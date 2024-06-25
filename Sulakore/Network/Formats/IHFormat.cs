using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sulakore.Network.Formats;

/// <summary>
/// Provides a high-performance low-level APIs for reading and writing byte buffers.
/// </summary>
/// <remarks>
/// Due to micro-optimizations, the <c>out</c> parameters must be considered to be trashed in <c>Try</c>-prefixed methods when the operation is unsuccessful (when the method returns <c>false</c>). 
/// The <c>out</c> parameters contain un-initialized values when the operation is unsuccessful - this is standard behaviour and can be also seen in runtime libraries.
/// </remarks>
public interface IHFormat
{
    public static readonly EvaWireFormat EvaWire = new(isUnity: false);
    public static readonly EvaWireFormat EvaWireUnity = new(isUnity: true);

    /// <summary>
    /// Minimum buffer size required from a packet in bytes.
    /// </summary>
    abstract static int MinBufferSize { get; }
    /// <summary>
    /// Minimum length required from a packet in bytes.
    /// </summary>
    abstract static int MinPacketLength { get; }
    /// <summary>
    /// Indicates whether the format has a length-prefix or not.
    /// </summary>
    abstract static bool HasLengthIndicator { get; }

    /// <summary>
    /// Returns the amount of bytes it takes to write <paramref name="value"/> of type <typeparamref name="T"/>.
    /// </summary>
    abstract static int GetSize<T>(T value) where T : unmanaged;
    /// <summary>
    /// Returns the amount of bytes it takes to write <paramref name="value"/> string.
    /// </summary>
    abstract static int GetSize(ReadOnlySpan<char> value);

    abstract static bool TryReadLength(ReadOnlySpan<byte> source, out int length, out int bytesRead);
    abstract static bool TryWriteLength(Span<byte> source, int length, out int bytesWritten);

    abstract static bool TryReadId(ReadOnlySpan<byte> source, out short id, out int bytesRead);
    abstract static bool TryWriteId(Span<byte> source, short id, out int bytesWritten);

    abstract static bool TryReadHeader(ReadOnlySpan<byte> source, out int length, out short id, out int bytesRead);
    abstract static bool TryWriteHeader(Span<byte> destination, int length, short id, out int bytesWritten);

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source span from which <paramref name="value"/> is read.</param>
    /// <param name="value">The value upon returning <c>true</c>.</param>
    /// <param name="bytesRead">The amount of bytes read from <paramref name="source"/>.</param>
    /// <returns>true if the value was read successfully; otherwise, false.</returns>
    abstract static bool TryRead<T>(ReadOnlySpan<byte> source, out T value, out int bytesRead) where T : unmanaged;
    
    /// <summary>
    /// Writes a <paramref name="value"/> of type <typeparamref name="T"/> into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The destination span where <paramref name="value"/> is written.</param>
    /// <param name="value">The value to write.</param>    
    /// <param name="bytesWritten">The amount of bytes written into <paramref name="destination"/> span.</param>
    /// <returns>true if the value was written successfully; otherwise, false.</returns>
    abstract static bool TryWrite<T>(Span<byte> destination, T value, out int bytesWritten) where T : unmanaged;

    abstract static bool TryReadUTF8(ReadOnlySpan<byte> source, out string value, out int bytesRead);
    abstract static bool TryReadUTF8(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten);

    abstract static bool TryWriteUTF8(Span<byte> destination, string value, out int bytesWritten);
    abstract static bool TryWriteUTF8(Span<byte> destination, ReadOnlySpan<char> value, out int bytesWritten);

    /// <summary>
    /// Provides a handler used by the language compiler to format interpolated string into binary representation using specified <see cref="IHFormat"/>.
    /// </summary>
    [InterpolatedStringHandler]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref struct TryWriteInterpolatedStringHandler<TFormat>
        where TFormat : struct, IHFormat
    {
        /// <summary>
        /// The destination buffer.
        /// </summary>
        private readonly Span<byte> _destination;
        
        /// <summary>
        /// The number of bytes written to <see cref="_destination"/>.
        /// </summary>
        internal int _position;

        /// <summary>
        /// true if all formatting operations have succeeded; otherwise, false.
        /// </summary>
        internal bool _success;

        /// <summary>
        /// Creates a handler used to write an interpolated string into a <paramref name="destination"/> buffer in specified <paramref name="format"/>.
        /// </summary>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <param name="shouldAppend">Upon return, true if the destination may be long enough to support the formatting, or false if it won't be.</param>
        /// <remarks>
        /// This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.
        /// </remarks>
        public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<byte> destination)
        {
            _destination = destination;
            _success = false;
            _position = 0;
        }

        /// <summary>
        /// Writes the specified value type to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public bool AppendFormatted<T>(T value)
            where T : unmanaged
        {
            if (TFormat.TryWrite(_destination.Slice(_position), value, out int bytesWritten))
            {
                _position += bytesWritten;
                return true;
            }
            return Fail();
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public bool AppendFormatted<T>(T value, string? format = default)
            where T : IHFormattable
        {
            // TODO: Handling of Enum.
            // constrained call avoiding boxing for value types
            if (((IHFormattable)value).TryFormat<TFormat>(_destination.Slice(_position), default, out int bytesWritten, format))
            {
                _position += bytesWritten;
                return true;
            }
            return Fail();
        }

        /// <summary>
        /// Writes the specified character span to the handler.
        /// </summary>
        /// <param name="value">The span to write.</param>
        public bool AppendFormatted(ReadOnlySpan<char> value)
        {
            if (TFormat.TryWriteUTF8(_destination.Slice(_position), value, out int bytesWritten))
            {
                _position += bytesWritten;
                return true;
            }
            return Fail();
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public bool AppendFormatted(string value)
        {
            if (TFormat.TryWriteUTF8(_destination.Slice(_position), value, out int bytesWritten))
            {
                _position += bytesWritten;
                return true;
            }
            return Fail();
        }

        /// <summary>
        /// Marks formatting as having failed and returns false.
        /// </summary>
        private bool Fail()
        {
            _success = false;
            return false;
        }
    }
}