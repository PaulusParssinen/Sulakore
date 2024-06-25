using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;


namespace Sulakore.Network.Middleware;

internal sealed class WebSocketClientMiddleware(ConnectionDelegate next, ILogger<WebSocketClientMiddleware> logger)
{
    private readonly ConnectionDelegate _next = next;
    private readonly ILogger<WebSocketClientMiddleware> _logger = logger;

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        var originalTransport = context.Transport;
        try
        {

            var wsProtocol = new WebSocketProtocol(context.Transport, WebSocketProtocolType.Client);
            
            

            //await using var loggingDuplexPipe = new LoggingDuplexPipe(context.Transport, _logger);
            //context.Transport = loggingDuplexPipe;

            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Transport = originalTransport;
        }
    }
}
