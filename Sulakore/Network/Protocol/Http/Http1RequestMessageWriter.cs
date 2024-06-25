using System.Buffers;
using System.Diagnostics;

namespace Sulakore.Network.Protocol.Http;

public sealed class Http1RequestMessageWriter : IMessageWriter<HttpRequestMessage>
{
    public void WriteMessage(HttpRequestMessage message, IBufferWriter<byte> output)
    {
        Debug.Assert(message.Method != null);
        Debug.Assert(message.RequestUri != null);

        var writer = new BufferWriter<IBufferWriter<byte>>(output);
        writer.WriteAscii(message.Method.Method);
        writer.Write(" "u8);
        writer.WriteAscii(message.RequestUri.ToString());
        writer.Write(" HTTP/1.1\r\n"u8);

        foreach (var header in message.Headers)
        {
            foreach (var value in header.Value)
            {
                writer.WriteAscii(header.Key);
                writer.Write(": "u8);
                writer.WriteAscii(value);
                writer.Write("\r\n"u8);
            }
        }

        if (message.Content != null)
        {
            foreach (var header in message.Content.Headers)
            {
                foreach (var value in header.Value)
                {
                    writer.WriteAscii(header.Key);
                    writer.Write(": "u8);
                    writer.WriteAscii(value);
                    writer.Write("\r\n"u8);
                }
            }
        }

        writer.Write("\r\n"u8);
        writer.Commit();
    }
}
