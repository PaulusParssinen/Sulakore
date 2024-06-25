using System.Net;

using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Transport.WebSockets;

public class WebSocketConnectionListener : IConnectionListener
{
    public EndPoint EndPoint { get; }

    public WebSocketConnectionListener()
    {

    }

    public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        await UnbindAsync().ConfigureAwait(false);
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
