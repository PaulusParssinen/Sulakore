﻿namespace Sulakore.Network.Formats.Wedgie;

public sealed class WedgieInFormat : IHFormat
{
    public int MinBufferSize => MinPacketLength;
    public int MinPacketLength => sizeof(ushort);
    public bool HasLengthIndicator => false;

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
}