// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// From Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.SocketConnection

using System.Net;
using System.Net.Sockets;
using System.IO.Pipelines;

using Sulakore.Network.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Sulakore.Network.Transport.Sockets;

/// <summary>
/// Represents a socket transport connection. This connection will keep pumping underlying socket to/from <see cref="Transport"/> duplex pipe.
/// </summary>
internal sealed class SocketConnection : ConnectionContext
{
    private volatile bool _socketDisposed;

    private bool _connectionClosed;
    private Exception? _shutdownReason;
    private readonly object _shutdownLock = new object();

    private readonly CancellationTokenSource _connectionClosedCts;
    private readonly TaskCompletionSource _waitForConnectionClosedTcs; // TODO: Necessary? 

    private Task? _sendingTask;
    private Task? _receivingTask;

    private readonly Socket _socket;

    // TODO: Measure whether Socket ValueTask APIs are worth to switch to. Would simplify code.
    private readonly SocketSender _sender;
    private readonly SocketReceiver _receiver;

    private readonly IDuplexPipe _application;
    private readonly IDuplexPipe _originalTransport;

    private readonly ILogger<SocketConnection> _logger;

    /// <summary>
    /// Provides a <see cref="IDuplexPipe"/> for reading and writing data.
    /// </summary>
    public override IDuplexPipe Transport { get; set; }

    public override EndPoint? LocalEndPoint  { get; set; }
    public override EndPoint? RemoteEndPoint { get; set; }

    public override CancellationToken ConnectionClosed { get; set; }
    public override string ConnectionId { get; set; } = Guid.NewGuid().ToString();

    // TODO: Features.. ASP.NET bloat or do we have use for them..?
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();

    public SocketConnection(
        Socket socket, 
        ILogger<SocketConnection> logger,
        PipeOptions inputOptions,
        PipeOptions outputOptions,
        PipeScheduler transportScheduler)
    {
        _logger = logger;
        _socket = socket;

        _sender = new SocketSender(transportScheduler);
        _receiver = new SocketReceiver(transportScheduler);

        (_originalTransport, _application) = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

        _connectionClosedCts = new CancellationTokenSource();
        _waitForConnectionClosedTcs = new TaskCompletionSource();

        Transport = _originalTransport;
        ConnectionClosed = _connectionClosedCts.Token;
    }

    /// <summary>
    /// Starts receiving and sending data.
    /// </summary>
    public void Start()
    {
        try
        {
            _receivingTask = DoReceive();
            _sendingTask = DoSend();
        }
        catch (Exception ex)
        {
            _logger.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(Start)}.");
        }
    }

    public override void Abort(ConnectionAbortedException abortReason)
    {
        // Try to gracefully close the socket to match libuv behavior.
        Shutdown(abortReason);

        // Cancel DoSend loop after calling shutdown to ensure the correct _shutdownReason gets set.
        _application.Input.CancelPendingRead();
    }

    private async Task DoReceive()
    {
        Exception? error = null;
        try
        {
            while (true)
            {
                var buffer = _application.Output.GetMemory();

                int bytesReceived = await _receiver.ReceiveAsync(_socket, buffer);
                if (bytesReceived == 0) break;

                _application.Output.Advance(bytesReceived);

                var flushTask = _application.Output.FlushAsync();

                FlushResult result;
                if (flushTask.IsCompletedSuccessfully)
                {
                    result = flushTask.Result;
                }
                else result = await flushTask.ConfigureAwait(false);

                // Pipe consumer is shut down, do we stop writing
                if (result.IsCompleted || result.IsCanceled) break;
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset) { error = new ConnectionResetException(ex.Message, ex); }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.OperationAborted or
            SocketError.ConnectionAborted or SocketError.Interrupted or SocketError.InvalidArgument)
        {
            if (!_socketDisposed) error = new Exception(ex.Message);
        }
        catch (ObjectDisposedException)
        {
            if (!_socketDisposed) error = new Exception("Disposed");
        }
        catch (IOException ex) { error = ex; }
        catch (Exception ex) { error = new IOException(ex.Message, ex); }
        finally
        {
            if (_socketDisposed) error ??= new Exception("Successful abort.");

            await _application.Output.CompleteAsync(error).ConfigureAwait(false);
        }
    }
    private async Task DoSend()
    {
        Exception? error = null;
        try
        {
            while (true)
            {
                // Wait for data to write from the pipe producer
                var result = await _application.Input.ReadAsync().ConfigureAwait(false);
                if (result.IsCanceled) break;

                var buffer = result.Buffer;
                if (!buffer.IsEmpty)
                {
                    await _sender.SendAsync(_socket, buffer);
                }

                _application.Input.AdvanceTo(buffer.End);

                if (result.IsCompleted) break;
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted) { error = null; }
        catch (ObjectDisposedException) { error = null; }
        catch (IOException ex) { error = ex; }
        catch (Exception ex) { error = new IOException(ex.Message, ex); }
        finally
        {
            Shutdown(error);

            // Mark the application input to the socket complete
            _application.Input.Complete();

            // Cancel any pending flush 
            _application.Output.CancelPendingFlush();
        }
    }

    // TODO: necessary?
    private void FireConnectionClosed()
    {
        if (_connectionClosed) return;
        _connectionClosed = true;

        ThreadPool.UnsafeQueueUserWorkItem(state =>
        {
            state.CancelConnectionClosedToken();

            state._waitForConnectionClosedTcs.TrySetResult();
        }, this, preferLocal: false);
    }

    private void Shutdown(Exception? shutdownReason)
    {
        lock (_shutdownLock)
        {
            if (_socketDisposed) return;

            // Make sure to close the connection only after the _socketDisposed flag is set.
            _socketDisposed = true;

            // shutdownReason should only be null if the output was completed gracefully, so no one should ever
            // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
            // to half close the connection which is currently unsupported.
            _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The Socket transport's send loop completed gracefully.");
            SocketLog.ConnectionClosed(_logger, ConnectionId, _shutdownReason.Message);

            try
            {
                // Try to gracefully close the socket even for aborts to match libuv behavior.
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { } // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.

            _socket.Dispose();
        }
    }

    private void CancelConnectionClosedToken()
    {
        try
        {
            _connectionClosedCts.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(CancelConnectionClosedToken)}.");
        }
    }

    public override async ValueTask DisposeAsync()
    {
        // Mark transport complete and wait for both loops to complete.
        _originalTransport.Input.Complete();
        _originalTransport.Output.Complete();
        
        try
        {
            if (_receivingTask != null) await _receivingTask;
            if (_sendingTask != null) await _sendingTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(Start)}.");
        }
        finally
        {
            _receiver.Dispose();
            _sender?.Dispose();
        }

        _connectionClosedCts.Dispose();
    }
}

internal static partial class SocketLog
{
    [LoggerMessage(1, LogLevel.Debug, "Connection id \"{ConnectionId}\" closed.")]
    public static partial void ConnectionClosed(ILogger logger, string connectionId, string message);

    [LoggerMessage(2, LogLevel.Debug, "Connection id \"{ConnectionId}\" was reset.")]
    public static partial void ConnectionReset(ILogger logger, string connectionId);
}