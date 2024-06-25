// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.IO.Pipelines;

using Microsoft.AspNetCore.Connections;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sulakore.Network.Transport.Sockets;

internal class SocketConnectionFactory : IConnectionFactory
{
    private readonly ILogger<SocketConnection> _logger;
    
    private readonly PipeOptions _inputOptions;
    private readonly PipeOptions _outputOptions;
    private readonly SocketTransportOptions _options;
    
    public SocketConnectionFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options.Value;
        _logger = loggerFactory.CreateLogger<SocketConnection>();

        long maxReadBufferSize = _options.MaxReadBufferSize ?? 0;
        long maxWriteBufferSize = _options.MaxWriteBufferSize ?? 0;

        // These are the same, it's either the thread pool or inline.
        var scheduler = _options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;

        _inputOptions = new PipeOptions(null, scheduler, scheduler, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
        _outputOptions = new PipeOptions(null, scheduler, scheduler, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);
    }

    public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        // TODO: Investigate why Kestrel did not support UnixDomainSocket and others
        if (endpoint is not IPEndPoint ipEndPoint)
            throw new NotSupportedException($"The {nameof(SocketConnectionFactory)} only supports IPEndPoints for now.");

        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = _options.NoDelay
        };

        await socket.ConnectAsync(ipEndPoint);

        // TODO: _options.WaitForDataBeforeAllocatingBuffer. Idle-connections won't allocate until it needs to.
        var socketConnection = new SocketConnection(socket,
            _logger,
            _inputOptions, _outputOptions,
            _inputOptions.ReaderScheduler);

        socketConnection.Start();
        return socketConnection;
    }
}