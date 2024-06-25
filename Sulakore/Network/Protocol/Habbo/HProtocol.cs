using System.Buffers;
using System.IO.Pipelines;

using Sulakore.Network.Formats;
using Sulakore.Cryptography.Ciphers;

namespace Sulakore.Network.Protocol.Habbo;

public class HProtocol<TInFormat, TOutFormat> : IMessageWriter<HPacket>, IMessageReader<HPacket>
    where TInFormat : struct, IHFormat
    where TOutFormat : struct, IHFormat
{
    public TInFormat ReceiveFormat { get; }
    public TOutFormat SendFormat { get; }

    public IStreamCipher Encrypter { get; set; }
    public IStreamCipher Decrypter { get; set; }

    private readonly ProtocolReader _protocolReader;
    private readonly ProtocolWriter _protocolWriter;

    public HProtocol(IDuplexPipe transport, IHFormat receiveFormat, IHFormat sendFormat)
    {
        _protocolReader = new ProtocolReader(transport.Input);
        _protocolWriter = new ProtocolWriter(transport.Output);
    }

    public async ValueTask<HPacket> ReadAsync(CancellationToken cancellationToken = default)
    {
        var result = await _protocolReader.ReadAsync(this, cancellationToken).ConfigureAwait(false);
        var message = result.Message;

        _protocolReader.Advance();
        return message;
    }
    public ValueTask WriteAsync(HPacket packet, CancellationToken cancellationToken = default)
    {
        return _protocolWriter.WriteAsync(this, packet, cancellationToken);
    }

    public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out HPacket message)
    {
        throw new NotImplementedException();
        message = default;
        return false;
    }
    public void WriteMessage(HPacket message, IBufferWriter<byte> output)
    {
        throw new NotImplementedException();
    }
}
