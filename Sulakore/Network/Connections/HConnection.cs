using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Connections;

public sealed class HConnection : IAsyncDisposable
{
    private readonly ConnectionContext _connection;

    private readonly ILogger<HConnection> _logger;
    
    public HotelEndPoint EndPoint { get; }

    public HConnection(IConnectionFactory connectionFactory,
        /* IHProtocol protocol, */
        HotelEndPoint endPoint,
        ILogger<HConnection> logger)
    {
        _logger = logger;

        EndPoint = endPoint;
    }

    /*
    private static ReadOnlySpan<byte> OkBytes => "OK"u8;
    private static ReadOnlySpan<byte> StartTLSBytes => "StartTLS"u8;
    
    public async Task<bool> UpgradeWebSocketAsync(WebSocketProtocolType type, bool secure = true)
    {
        // .UseMiddleware<TlsClientMiddleware>();

        var upgradeProtocol = new HttpUpgradeProtocol(_connection.Transport);
        var response = await upgradeProtocol.UpgradeToWebSocketAsync().ConfigureAwait(false);

        var wsProtocol = new WebSocketProtocol(_connection.Transport, type);

        //TODO: Write "StartTLS"
        //TODO: Rcv && seq equal


        var something = await wsProtocol.ReadAsync();
        var something1 = await wsProtocol.ReadAsync();
        var something2 = await wsProtocol.ReadAsync();

        var (transport, application) = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
        // Initialize the second secure tunnel layer where ONLY the WebSocket payload data will be read/written from/to.

        //_securePayloadLayer = new SslStream(this, true, ValidateRemoteCertificate);
        //await _securePayloadLayer.AuthenticateAsClientAsync(RemoteEndPoint.Host, null, false).ConfigureAwait(false);
        //
        //return IsUpgraded;
        return true;
    }
    private static bool ValidateRemoteCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;
    */
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
