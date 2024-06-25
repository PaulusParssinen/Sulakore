using System.Buffers;

namespace Sulakore.Network.Protocol;

public interface IMessageWriter<TMessage>
{
    void WriteMessage(TMessage message, IBufferWriter<byte> output);
}
