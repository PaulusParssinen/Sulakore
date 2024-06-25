using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Connections;

public class HConnectionHandler : ConnectionHandler
{
    private readonly ILogger<HConnectionHandler> _logger;

    public HConnectionHandler(ILogger<HConnectionHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync(ConnectionContext connection)
    {
        throw new NotImplementedException();

        // Protocol here?
    }
}
