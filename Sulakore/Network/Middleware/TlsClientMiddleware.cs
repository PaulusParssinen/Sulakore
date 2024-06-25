using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Sulakore.Network.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Middleware;

internal sealed class TlsClientMiddleware(ConnectionDelegate next, string hostname, ILogger<TlsClientMiddleware> logger)
{
    // TODO: TlsOptions { Timeout, RemoteEndpoint, Cert?, ValidateRemote? }
    private readonly string _hostname = hostname;

    private readonly ConnectionDelegate _next = next;
    private readonly ILogger<TlsClientMiddleware> _logger = logger;

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        var sslDuplexPipe = new DuplexPipeStreamAdapter<SslStream>(context.Transport,
            stream => new SslStream(stream, leaveInnerStreamOpen: false, ValidateRemoteCertificate));

        SslStream sslStream = sslDuplexPipe.Stream;

        await sslStream.AuthenticateAsClientAsync(_hostname, null, false).ConfigureAwait(false);
        if (!sslStream.IsAuthenticated)
        {
            _logger.LogError(1, "Authentication failed");
            await sslStream.DisposeAsync().ConfigureAwait(false);
            return;
        }

        var originalTransport = context.Transport;
        try
        {
            context.Transport = sslDuplexPipe;

            await using (sslStream)
            await using (sslDuplexPipe)
            {
                await _next(context).ConfigureAwait(false);
            }
        }
        finally
        {
            context.Transport = originalTransport;
        }
    }
    private static bool ValidateRemoteCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;
}
