using System.Text;
using System.Buffers;
using System.Text.Unicode;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Sulakore.Network.Formats;

public readonly struct EvaWireFormat(bool isUnity) : IHFormat
{
    public bool IsUnity { get; } = isUnity;
    
    public static int MinBufferSize => sizeof(int) + MinPacketLength;
    public static int MinPacketLength => sizeof(short);
    public static bool HasLengthIndicator => true;

    public static bool TryReadLength(ReadOnlySpan<byte> source, out int length, out int bytesRead)
        => TryRead(source, out length, out bytesRead);
    public static bool TryWriteLength(Span<byte> destination, int length, out int bytesWritten)
        => TryWrite(destination, length, out bytesWritten);

    public static bool TryReadId(ReadOnlySpan<byte> source, out short id, out int bytesRead)
        => TryRead(source.Slice(4), out id, out bytesRead);
    public static bool TryWriteId(Span<byte> destination, short id, out int bytesWritten) 
        => TryWrite(destination.Slice(4), id, out bytesWritten);

    public static bool TryReadHeader(ReadOnlySpan<byte> source, out int length, out short id, out int bytesRead)
    {
        Unsafe.SkipInit(out id);
        if (TryReadLength(source, out length, out bytesRead) &&
            TryReadId(source, out id, out int idBytesRead))
        {
            bytesRead += idBytesRead;
            return true;
        }
        return false;
    }
    public static bool TryWriteHeader(Span<byte> destination, int length, short id, out int bytesWritten)
    {
        if (TryWriteLength(destination, length, out bytesWritten) &&
            TryWriteId(destination, id, out int idBytesWritten))
        {
            bytesWritten += idBytesWritten;
            return true;
        }
        return false;
    }

    public static unsafe int GetSize<T>(T value) where T : unmanaged => sizeof(T);
    public static int GetSize(ReadOnlySpan<char> value) => sizeof(short) + Encoding.UTF8.GetByteCount(value);

    public static unsafe bool TryRead<T>(ReadOnlySpan<byte> source, out T value, out int bytesRead) 
        where T : unmanaged
    {
        Unsafe.SkipInit(out value);
        Unsafe.SkipInit(out bytesRead);
        if (sizeof(T) <= (uint)source.Length)
        {
            bytesRead = sizeof(T);
            ref byte sourcePtr = ref MemoryMarshal.GetReference(source);
            if (BitConverter.IsLittleEndian)
            {
                if (sizeof(T) == sizeof(byte))
                {
                    value = Unsafe.As<byte, T>(ref sourcePtr);
                }
                else if (sizeof(T) == sizeof(uint))
                {
                    value = (T)(object)BinaryPrimitives.ReverseEndianness(Unsafe.As<byte, uint>(ref sourcePtr));
                }
                else if (sizeof(T) == sizeof(ushort))
                {
                    value = (T)(object)BinaryPrimitives.ReverseEndianness(Unsafe.As<byte, ushort>(ref sourcePtr));
                }
                else if (sizeof(T) == sizeof(ulong))
                {
                    value = (T)(object)BinaryPrimitives.ReverseEndianness(Unsafe.As<byte, ulong>(ref sourcePtr));
                }
                else if (typeof(T) == typeof(float))
                {
                    value = (T)(object)BitConverter.Int32BitsToSingle(
                        BinaryPrimitives.ReverseEndianness(Unsafe.As<byte, int>(ref sourcePtr)));
                }
                else if (typeof(T) == typeof(double))
                {
                    value = (T)(object)BitConverter.Int64BitsToDouble(
                        BinaryPrimitives.ReverseEndianness(Unsafe.As<byte, long>(ref sourcePtr)));
                }
            }
            else value = Unsafe.As<byte, T>(ref sourcePtr);
            return true;
        }
        return false;
    }

    //public static bool TryWriteInt(Span<byte> dest, int value, out int written) => TryWrite(dest, value, out written);

    public static unsafe bool TryWrite<T>(Span<byte> destination, T value, out int bytesWritten) 
        where T : unmanaged
    {
        Unsafe.SkipInit(out bytesWritten);

        ref byte destPtr = ref MemoryMarshal.GetReference(destination);
        if (sizeof(T) <= (uint)destination.Length)
        {
            bytesWritten = sizeof(T);
            if (BitConverter.IsLittleEndian)
            {
                if (sizeof(T) == sizeof(uint))
                {
                    value = (T)(object)BinaryPrimitives.ReverseEndianness((uint)(object)value);
                }
                else if (sizeof(T) == sizeof(ushort))
                {
                    value = (T)(object)BinaryPrimitives.ReverseEndianness((ushort)(object)value);
                }
                else if (sizeof(T) == sizeof(ulong))
                {
                    value = (T)(object)BinaryPrimitives.ReverseEndianness((ulong)(object)value);
                }
                else if (typeof(T) == typeof(float))
                {
                    Unsafe.WriteUnaligned(ref destPtr,
                        BinaryPrimitives.ReverseEndianness(
                            BitConverter.SingleToInt32Bits((float)(object)value)));
                    return true;
                }
                else if (typeof(T) == typeof(double))
                {
                    Unsafe.WriteUnaligned(ref destPtr, 
                        BinaryPrimitives.ReverseEndianness(
                            BitConverter.DoubleToInt64Bits((double)(object)value)));
                    return true;
                }
            }
            Unsafe.WriteUnaligned(ref destPtr, value);
            return true;
        }
        return false;
    }

    public static bool TryReadUTF8(ReadOnlySpan<byte> source, out string value, out int bytesRead)
    {
        Unsafe.SkipInit(out value);
        if (!TryRead(source, out short length, out bytesRead) ||
            source.Length < sizeof(short) + length) return false;

        bytesRead += length;
        value = Encoding.UTF8.GetString(source.Slice(sizeof(short), length));
        return true;
    }

    public static bool TryWriteUTF8(Span<byte> destination, ReadOnlySpan<char> value, out int bytesWritten)
    {
        Unsafe.SkipInit(out bytesWritten);
        if (destination.Length < sizeof(short))
            return false;

        if (Encoding.UTF8.TryGetBytes(value, destination.Slice(sizeof(short)), out bytesWritten))
            return false;

        if (!TryWrite(destination, (short)bytesWritten, out int written))
            return false;

        bytesWritten += written;
        return true;
    }


    public static bool TryReadUTF8(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten)
    {
        Unsafe.SkipInit(out charsWritten);
        if (!TryRead(source, out short length, out bytesRead) ||
            source.Length < sizeof(short) + length ||
            destination.Length < length) return false;

        OperationStatus status = Utf8.ToUtf16(source, destination.Slice(sizeof(short), length), out int actualLength, out charsWritten);
        bytesRead += actualLength;
        return status == OperationStatus.Done;
    }

    public static bool TryWriteUTF8(Span<byte> destination, string value, out int bytesWritten)
    {
        Unsafe.SkipInit(out bytesWritten);
        if (destination.Length < sizeof(short))
            return false;

        var utf8Handler = new Utf8.TryWriteInterpolatedStringHandler(
            value.Length, 0, destination.Slice(2), out _);

        if (utf8Handler.AppendLiteral(value))
        {
            bytesWritten = utf8Handler.GetPosition();
        }

        _ = TryWrite(destination, (ushort)bytesWritten, out _);
        bytesWritten += sizeof(ushort);

        return true;
    }
}

internal static class Utf8TryWriteInterpolatedStringHandler
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_pos")]
    internal static extern ref int GetPosition(this ref Utf8.TryWriteInterpolatedStringHandler instance);
}