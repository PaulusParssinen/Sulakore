using System.Text;
using System.Buffers.Text;
using System.IO.Pipelines;

using Sulakore.Network.Transport;
using Sulakore.Network.Transport.Sockets;

using System.Security.Cryptography;

namespace Sulakore.Network.Protocol.Http;

public sealed class HttpUpgradeProtocol
{
    private readonly SocketConnection _node;
    
    private readonly ProtocolReader _protocolReader;
    private readonly ProtocolWriter _protocolWriter;
    
    private readonly IMessageReader<HttpRequestMessage> _requestReader;
    private readonly IMessageWriter<HttpRequestMessage> _requestWriter;

    private readonly IMessageReader<HttpResponseMessage> _responseReader;
    // TODO: private readonly IMessageWriter<HttpResponseMessage> _responseWriter;

    private readonly IDuplexPipe _transport;

    public HttpUpgradeProtocol(IDuplexPipe transport)
    {
        _transport = transport;

        _protocolReader = new ProtocolReader(transport.Input);
        _protocolWriter = new ProtocolWriter(transport.Output);

        _requestReader = new Http1RequestMessageReader();
        _requestWriter = new Http1RequestMessageWriter();

        _responseReader = new Http1ResponseMessageReader();
        // TODO: _responseWriter = new Http1ResponseMessageWriter();
    }

    public ValueTask<FlushResult> WriteUpgradeRequestAsync(CancellationToken cancellationToken = default)
    {
        static string GenerateWebSocketKey()
        {
            Span<byte> keyGenerationBuffer = stackalloc byte[24];
            RandomNumberGenerator.Fill(keyGenerationBuffer.Slice(0, 16));

            Base64.EncodeToUtf8InPlace(keyGenerationBuffer, 16, out int encodedSize);
            return Encoding.UTF8.GetString(keyGenerationBuffer.Slice(0, encodedSize));
        }
        
        // TODO: Cache and reuse the HttpRequestMessage instance.
        HttpRequestMessage upgradeMessage = new(HttpMethod.Get, "/websocket");
        //upgradeMessage.Headers.TryAddWithoutValidation("Host", _node.Host);
        upgradeMessage.Headers.TryAddWithoutValidation("Connection", "Upgrade");
        upgradeMessage.Headers.TryAddWithoutValidation("User-Agent", "");
        upgradeMessage.Headers.TryAddWithoutValidation("Sec-WebSocket-Version", "13");
        upgradeMessage.Headers.TryAddWithoutValidation("Sec-WebSocket-Key", GenerateWebSocketKey());
        
        _requestWriter.WriteMessage(upgradeMessage, _transport.Output);

        return _transport.Output.FlushAsync(cancellationToken);
    }

    public async Task<HttpResponseMessage> UpgradeToWebSocketAsync(CancellationToken cancellationToken = default)
    {
        var result = await WriteUpgradeRequestAsync(cancellationToken);
        return await ReadResponseAsync(cancellationToken);
    }

    public async ValueTask<HttpRequestMessage> ReadRequestAsync(CancellationToken cancellationToken = default)
    {
        var result = await _protocolReader.ReadAsync(_requestReader, cancellationToken).ConfigureAwait(false);
        
        _protocolReader.Advance();
        return result.Message;
    }
    public async ValueTask<HttpResponseMessage> ReadResponseAsync(CancellationToken cancellationToken = default)
    {
        var result = await _protocolReader.ReadAsync(_responseReader, cancellationToken).ConfigureAwait(false);
        
        _protocolReader.Advance();
        return result.Message;
    }
}
