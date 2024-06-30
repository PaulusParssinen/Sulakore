using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sulakore.Network.Formats.Wedgie;

public sealed class WedgieOutFormat : IHFormat
{
    public int MinBufferSize => 3 + MinPacketLength;
    public int MinPacketLength => sizeof(ushort);
    public bool HasLengthIndicator => true;

    public int GetSize<T>(T value) where T : unmanaged => throw new NotImplementedException();
    public int GetSize(ReadOnlySpan<char> value) => throw new NotImplementedException();

    public bool TryRead<T>(ReadOnlySpan<byte> source, out T value, out int bytesRead) where T : unmanaged => throw new NotImplementedException();
    public bool TryReadHeader(ReadOnlySpan<byte> source, out int length, out short id, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadId(ReadOnlySpan<byte> source, out short id, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadLength(ReadOnlySpan<byte> source, out int length, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadUTF8(ReadOnlySpan<byte> source, out string value, out int bytesRead) => throw new NotImplementedException();
    public bool TryReadUTF8(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten) => throw new NotImplementedException();

    public bool TryWrite<T>(Span<byte> destination, T value, out int bytesWritten) where T : unmanaged => throw new NotImplementedException();
    public bool TryWriteHeader(Span<byte> destination, int length, short id, out int bytesWritten) => throw new NotImplementedException();
    public bool TryWriteId(Span<byte> source, short id, out int bytesWritten) => throw new NotImplementedException();
    public bool TryWriteLength(Span<byte> source, int length, out int bytesWritten) => throw new NotImplementedException();
    public bool TryWriteUTF8(Span<byte> destination, ReadOnlySpan<char> value, out int bytesWritten) => throw new NotImplementedException();

    public static int GetEncodedVL64Int32Length(int value) => GetEncodedVL64UInt32Length((uint)Math.Abs(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEncodedVL64UInt32Length(uint value)
    {
        return ((32 - BitOperations.LeadingZeroCount(value) + 9) * 43) >> 8;
    }

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
            value |= (source[i] & 0x3F) << (2 + (6 * (i - 1)));
        }

        if ((header & 0x4) == 0x4)
        {
            value = -value;
        }

        return true;
    }

    public static bool TryReadBase64UInt32(ReadOnlySpan<byte> source, out uint value)
    {
        value = 0;
        for (int i = source.Length - 1; i >= 0; i--)
        {
            value |= (source[source.Length - i - 1] & 0x3Fu) << i * 6;
        }
        return true;
    }

    public static bool TryWriteBase64UInt32(Span<byte> destination, uint value)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] = (byte)(0x40 | ((value >> ((destination.Length - 1 - i) * 6)) & 0x3F));
        }
        return true;
    }

    public static bool TryWriteVL64Int32(Span<byte> destination, int value, out int bytesWritten)
    {
        bytesWritten = 0;
        if (destination.Length == 0)
            return false;

        int left = Math.Abs(value);
        int header = 0x40 | (left & 3);

        bytesWritten = 1;
        for (left >>= 2; left > 0; left >>= 6)
        {
            // TODO: Proper length check instead of exception for Try overload
            destination[bytesWritten] = (byte)(0x40 | (left & 0x3F));

            bytesWritten++;
        }

        destination[0] = (byte)((bytesWritten << 3) | (value < 0 ? 0x4 : 0) | header);
        return true;
    }
}