using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Connections;

public class HConnectionListener : IConnectionListener
{
    public EndPoint EndPoint { get; }

    private readonly ILogger<HConnectionListener> _logger;

    public HConnectionListener(
        EndPoint endpoint,
        ILogger<HConnectionListener> logger)
    {
        _logger = logger;

        EndPoint = endpoint;
    }

    public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public static class HConnectionListenerBuilder
{
    public static HConnectionListener UseProtocol(this HConnectionListener listener)
    {
        return listener;
    }
}