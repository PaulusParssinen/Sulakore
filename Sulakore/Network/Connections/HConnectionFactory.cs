using Microsoft.AspNetCore.Connections;
using System.Net;

namespace Sulakore.Network.Connections;

internal class HConnectionFactory : IConnectionFactory
{
    public HConnectionFactory()
    {

    }

    public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
