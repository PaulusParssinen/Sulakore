using System.Net;
using System.Net.Sockets;
using System.IO.Pipelines;

using Sulakore.Network.Protocol.Http;
using Sulakore.Network.Transport.Sockets;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Transport.WebSockets;

public class WebSocketConnectionFactory : IConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpUpgradeProtocol _upgradeProtocol;

    public WebSocketConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint is not UriEndPoint uriEndpoint)
            throw new NotSupportedException($"{endpoint} is not supported");

        // TODO: Lightweight HTTP UPGRADE, or do we put it into middleware?

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

        var logger = _loggerFactory.CreateLogger<SocketConnection>();

        var connection = new SocketConnection(socket, logger, 
            PipeOptions.Default, PipeOptions.Default, 
            PipeScheduler.ThreadPool);

        return connection;
    }
}
