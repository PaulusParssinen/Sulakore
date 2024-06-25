using System.Net;
using System.Text;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace Sulakore.Network.Protocol.Http;

public sealed class Http1ResponseMessageReader : IMessageReader<HttpResponseMessage>
{
    private static ReadOnlySpan<byte> NewLine => "\r\n"u8;
    private static ReadOnlySpan<byte> TrimChars => " \t"u8;

    private HttpResponseMessage _httpResponseMessage = new();

    private State _state;

    public Http1ResponseMessageReader()
    { }

    public bool TryParseMessage(in ReadOnlySequence<byte> input, 
        ref SequencePosition consumed, ref SequencePosition examined, 
        [NotNullWhen(true)] out HttpResponseMessage? message)
    {
        var sequenceReader = new SequenceReader<byte>(input);
        message = default;

        switch (_state)
        {
            case State.StartLine:
                if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> version, (byte)' ')) return false;
                if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> statusCodeText, (byte)' ')) return false;
                if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> statusText, NewLine)) return false;
                if (!Utf8Parser.TryParse(statusCodeText, out int statusCode, out _)) return false;

                _httpResponseMessage.StatusCode = (HttpStatusCode)statusCode;
                _httpResponseMessage.ReasonPhrase = Encoding.ASCII.GetString(statusText);
                _httpResponseMessage.Version = HttpVersion.Version11;

                _state = State.Headers;

                consumed = sequenceReader.Position;

                goto case State.Headers;

            case State.Headers:
                while (sequenceReader.TryReadTo(out ReadOnlySequence<byte> headerLine, NewLine))
                {
                    if (headerLine.Length == 0)
                    {
                        consumed = sequenceReader.Position;
                        examined = consumed;

                        message = _httpResponseMessage;

                        _state = State.Body;
                        break;
                    }

                    Http1RequestMessageReader.ParseHeader(headerLine, out var headerName, out var headerValue);

                    var key = Encoding.ASCII.GetString(headerName.Trim(TrimChars));
                    var value = Encoding.ASCII.GetString(headerValue.Trim(TrimChars));

                    _httpResponseMessage.Headers.TryAddWithoutValidation(key, value);

                    consumed = sequenceReader.Position;
                }
                break;
            default:
                break;
        }

        return _state == State.Body;
    }

    private enum State
    {
        StartLine,
        Headers,
        Body
    }
}
