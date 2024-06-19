namespace Tanji.Core.Net.Formats;

// TODO: Optimize :)
// TODO: maybe bring in static abstract interface versions of IHFormat
public sealed class WedgieFormat : IHFormat
{
    public bool IsOutgoing { get; }
    public int MinBufferSize => throw new NotImplementedException();
    public int MinPacketLength => throw new NotImplementedException();
    public bool HasLengthIndicator => IsOutgoing;

    public WedgieFormat(bool isOutgoing) => IsOutgoing = isOutgoing;

    public int GetSize<T>(T value) where T : struct => throw new NotImplementedException();
    public int GetSize(ReadOnlySpan<char> value) => throw new NotImplementedException();
    
    public bool TryRead<T>(ReadOnlySpan<byte> source, out T value, out int bytesRead) where T : struct => throw new NotImplementedException();
    public bool TryReadHeader(ReadOnlySpan<byte> source, out int length, out short id, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadId(ReadOnlySpan<byte> source, out short id, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadLength(ReadOnlySpan<byte> source, out int length, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadUTF8(ReadOnlySpan<byte> source, out string value, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadUTF8(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten) => throw new NotImplementedException();
    
    public bool TryWrite<T>(Span<byte> destination, T value, out int bytesWritten) where T : struct => throw new NotImplementedException();
    public bool TryWriteHeader(Span<byte> destination, int length, short id, out int bytesWritten) => throw new NotImplementedException();
    public bool TryWriteId(Span<byte> source, short id, out int bytesWritten) => throw new NotImplementedException();
    public bool TryWriteLength(Span<byte> source, int length, out int bytesWritten) => throw new NotImplementedException();
    public bool TryWriteUTF8(Span<byte> destination, ReadOnlySpan<char> value, out int bytesWritten) => throw new NotImplementedException();

    public static bool TryReadVL64Int32(ReadOnlySpan<byte> source, out int value, out int bytesRead)
    {
        value = bytesRead = 0;
        if (source.Length == 0) return false;

        byte header = source[0];

        int count = (header >> 3) & 0x7;

        value = header & 0x3;
        for (int i = 1; i < count; i++)
        {
            // TODO: Proper length check instead of exception for Try overload
            value |= (source[i] - 0x40) << (2 + (6 * (i - 1)));
        }

        if ((header & 0x4) == 0x4)
        {
            value = -value;
        }

        return true;
    }

    public static bool TryWriteVL64Int32(Span<byte> destination, int value, out int bytesWritten)
    {
        value = bytesWritten = 0;
        if (destination.Length == 0)
            return false;

        int left = Math.Abs(value);
        int header = left & 3;

        bytesWritten = 1;
        for (left >>= 2; left > 0; left >>= 6)
        {
            bytesWritten++;

            // TODO: Proper length check instead of exception for Try overload
            destination[bytesWritten] = (byte)(0x40 + (left & 0x3F));
        }

        destination[0] = (byte)((bytesWritten << 3) | (value < 0 ? 0x4 : 0) | header);
        return true;
    }
}