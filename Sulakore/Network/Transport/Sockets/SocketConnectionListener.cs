// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO.Pipelines;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Transport.Sockets;

#nullable enable
internal sealed class SocketConnectionListener : IConnectionListener
{
    private readonly SocketTransportOptions _options;
    private readonly ILogger<SocketConnection> _logger;
    
    private Socket? _listenSocket;

    public EndPoint EndPoint { get; private set; }

    internal SocketConnectionListener(
        EndPoint endpoint,
        SocketTransportOptions options,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<SocketConnection>();
        
        EndPoint = endpoint;
    }

    internal void Bind()
    {
        if (_listenSocket != null) throw new InvalidOperationException("Socket is already bound.");
    
        Socket listenSocket;
        try
        {
            listenSocket = _options.CreateBoundListenSocket(EndPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new AddressInUseException(e.Message, e);
        }

        Debug.Assert(listenSocket.LocalEndPoint != null);
        EndPoint = listenSocket.LocalEndPoint;

        listenSocket.Listen(_options.Backlog);

        _listenSocket = listenSocket;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                Debug.Assert(_listenSocket != null, "Bind must be called first.");

                var acceptSocket = await _listenSocket.AcceptAsync(cancellationToken);

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                {
                    acceptSocket.NoDelay = _options.NoDelay;
                }

                long maxReadBufferSize = _options.MaxReadBufferSize ?? 0;
                long maxWriteBufferSize = _options.MaxWriteBufferSize ?? 0;

                // These are the same, it's either the thread pool or inline.
                var scheduler = _options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;

                var pipeOptions = new PipeOptions(null, scheduler, scheduler, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);

                var socketconnection = new SocketConnection(
                    acceptSocket,
                    _logger,
                    pipeOptions, pipeOptions,
                    scheduler);

                return socketconnection;
            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                SocketLog.ConnectionReset(_logger, connectionId: "(null)");
            }
        }
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _listenSocket?.Dispose();
        return default;
    }

    public ValueTask DisposeAsync()
    {
        _listenSocket?.Dispose();
        return default;
    }
}