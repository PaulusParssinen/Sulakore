using System.Runtime.CompilerServices;

namespace Sulakore.Network.Formats;

/// <summary>
/// Provides useful helper methods for <see cref="IHFormat"/> implementations.
/// </summary>
/// <remarks>
/// Due to micro-optimizations, the <c>out</c> parameters must be considered to be trashed in <c>Try</c>-prefixed methods when the operation is unsuccessful (when the method returns <c>false</c>). 
/// The <c>out</c> parameters contain un-initialized values when the operation is unsuccessful - this is standard behaviour and can be also seen in runtime libraries.
/// </remarks>
public static class IHFormatExtensions
{
    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from <paramref name="source"/> and then 'advances'.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="source"/> does not have enough data to read a value of type <typeparamref name="T"/>.</exception>
    public static T Read<TFormat, T>(this TFormat format, ref ReadOnlySpan<byte> source)
        where TFormat : struct, IHFormat
        where T : unmanaged
    {
        T value = format.Read<TFormat, T>(source, out int bytesRead);
        source = source.Slice(bytesRead);
        return value;
    }

    /// <summary>
    /// Writes a <paramref name="value"/> of type <typeparamref name="T"/> into <paramref name="destination"/> and then 'advances'.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="value"/> does not fit into the <paramref name="destination"/>.</exception>
    public static void Write<TFormat, T>(this TFormat format, ref Span<byte> destination, T value)
        where TFormat : struct, IHFormat
        where T : unmanaged
    {
        format.Write(destination, value, out int bytesWritten);
        destination = destination.Slice(bytesWritten);
    }

    /// <summary>
    /// Reads a string from <paramref name="source"/> and then 'advances'.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="source"/> does not have enough data to read a string.</exception>
    public static string ReadUTF8<TFormat>(this TFormat format, ref ReadOnlySpan<byte> source)
        where TFormat : struct, IHFormat
    {
        string value = format.ReadUTF8(source, out int bytesRead);
        source = source.Slice(bytesRead);
        return value;
    }

    /// <summary>
    /// Reads a string of chars from <paramref name="source"/> into <paramref name="destination"/> and then 'advances'.
    /// </summary>
    /// <param name="charsWritten">The amount of characters written into destination span.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="source"/> does not have enough data to read a string.</exception>
    public static void ReadUTF8<TFormat>(this TFormat format, ref ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        where TFormat : struct, IHFormat
    {
        format.ReadUTF8(source, destination, out int bytesRead, out charsWritten);
        source = source.Slice(bytesRead);
    }
    /// <summary>
    /// Writes a <paramref name="value"/> into <paramref name="destination"/> and then 'advances'.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="value"/> does not fit into the <paramref name="destination"/>.</exception>
    public static void WriteUTF8<TFormat>(this TFormat format, ref Span<byte> destination, ReadOnlySpan<char> value)
        where TFormat : struct, IHFormat
    {
        format.WriteUTF8(destination, value, out int bytesWritten);
        destination = destination.Slice(bytesWritten);
    }

    /// <summary>
    /// Reads a unique identifier from <paramref name="source"/> and then 'advances'.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="source"/> does not have enough data to read a value of type <typeparamref name="T"/>.</exception>
    public static long ReadUniqueId<TFormat, T>(this TFormat format, ref ReadOnlySpan<byte> source)
        where TFormat : struct, IHFormat
        where T : struct
    {
        long uniqueId = format.ReadUniqueId(source, out int bytesRead);
        source = source.Slice(bytesRead);
        return uniqueId;
    }
    /// <summary>
    /// Writes a unique identifier into <paramref name="destination"/> and then 'advancess'.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="uniqueId"/> does not fit into the <paramref name="destination"/>.</exception>
    public static void WriteUniqueId<TFormat>(this TFormat format, ref Span<byte> destination, long uniqueId)
        where TFormat : struct, IHFormat
    {
        format.WriteUniqueId(destination, uniqueId, out int bytesWritten);
        destination = destination.Slice(bytesWritten);
    }

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from <paramref name="source"/>.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="source"/> does not have enough data to read a value of type <typeparamref name="T"/>.</exception>
    public static T Read<TFormat, T>(this TFormat format, ReadOnlySpan<byte> source, out int bytesRead)
        where TFormat : struct, IHFormat
        where T : unmanaged
    {
        if (!TFormat.TryRead(source, out T value, out bytesRead))
            ThrowHelper.ThrowIndexOutOfRangeException();
        return value;
    }
    /// <summary>
    /// Writes a <paramref name="value"/> of type <typeparamref name="T"/> into <paramref name="destination"/>.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="value"/> does not fit into the <paramref name="destination"/>.</exception>
    public static void Write<TFormat, T>(this TFormat format, Span<byte> destination, T value, out int bytesWritten)
        where TFormat : struct, IHFormat
        where T : unmanaged
    {
        if (!TFormat.TryWrite(destination, value, out bytesWritten))
            ThrowHelper.ThrowIndexOutOfRangeException();
    }

    public static string ReadUTF8<TFormat>(this TFormat format, ReadOnlySpan<byte> source, out int bytesRead)
        where TFormat : struct, IHFormat
    {
        if (!TFormat.TryReadUTF8(source, out string value, out bytesRead))
            ThrowHelper.ThrowIndexOutOfRangeException();
        return value;
    }

    public static void ReadUTF8<TFormat>(this TFormat format, ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten)
        where TFormat : struct, IHFormat
    {
        if (!TFormat.TryReadUTF8(source, destination, out bytesRead, out charsWritten))
            ThrowHelper.ThrowIndexOutOfRangeException();
    }

    public static void WriteUTF8<TFormat>(this TFormat format, Span<byte> destination, ReadOnlySpan<char> value, out int bytesWritten)
        where TFormat : struct, IHFormat
    {
        if (!TFormat.TryWriteUTF8(destination, value, out bytesWritten))
            ThrowHelper.ThrowIndexOutOfRangeException();
    }

    /// <summary>
    /// Reads a unique identifier from <paramref name="source"/>.
    /// </summary>
    /// <param name="format">The <see cref="IHFormat">format</see> used to read the unique identifier from <paramref name="source"/>.</param>
    /// <param name="source">The source span from which the unique identifier is read.</param>
    /// <param name="bytesRead">The amount of bytes read from <paramref name="source"/>.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="source"/> does not have enough data to read array length.</exception>
    public static long ReadUniqueId<TFormat>(this TFormat format, ReadOnlySpan<byte> source, out int bytesRead)
        where TFormat : struct, IHFormat
    {
        if (!format.TryReadUniqueId(source, out long uniqueId, out bytesRead))
            ThrowHelper.ThrowIndexOutOfRangeException();
        return uniqueId;
    }

    /// <summary>
    /// Writes <paramref name="uniqueId"/> into <paramref name="destination"/>.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="uniqueId"/> does not fit into the <paramref name="destination"/>.</exception>
    public static void WriteUniqueId<TFormat>(this TFormat format, Span<byte> destination, long uniqueId, out int bytesWritten)
        where TFormat : struct, IHFormat
    {
        if (!format.TryWriteUniqueId(destination, uniqueId, out bytesWritten))
            ThrowHelper.ThrowIndexOutOfRangeException();
    }

    /// <summary>
    /// Reads <paramref name="uniqueId"/> from <paramref name="source"/>.
    /// </summary>
    /// <param name="format">The <see cref="IHFormat">format</see> used to read the <paramref name="uniqueId"/> from <paramref name="source"/>.</param>
    /// <param name="source">The source span from which <paramref name="uniqueId"/> is read.</param>
    /// <param name="uniqueId">The unique identifier.</param>
    /// <param name="bytesRead">The amount of bytes read from <paramref name="source"/>.</param>
    /// <returns>true if the unique identifier was read successfully; otherwise, false.</returns>
    public static bool TryReadUniqueId<TFormat>(this TFormat format, ReadOnlySpan<byte> source, out long uniqueId, out int bytesRead)
        where TFormat : struct, IHFormat
    {
        // TODO: Check the if the type check is elided by JIT or do we have to do it all manually
        if (format is EvaWireFormat wireFormat && wireFormat.IsUnity)
        {
            return TFormat.TryRead(source, out uniqueId, out bytesRead);
        }
        bool success = TFormat.TryRead(source, out int uniqueIdInt, out bytesRead);
        uniqueId = uniqueIdInt;
        return success;
    }

    /// <summary>
    /// Writes the specified <paramref name="uniqueId"/> into <paramref name="destination"/>.
    /// </summary>
    /// <param name="format">The <see cref="IHFormat">format</see> used to write the <paramref name="uniqueId"/> into <paramref name="destination"/>.</param>
    /// <param name="destination">The destination span where <paramref name="uniqueId"/> is written.</param>
    /// <param name="uniqueId">The unique identifier.</param>
    /// <param name="bytesWritten">The amount of bytes written into <paramref name="destination"/>.</param>
    /// <returns>true if the unique identifier was written successfully; otherwise, false.</returns>
    public static bool TryWriteUniqueId<TFormat>(this TFormat format, Span<byte> destination, long uniqueId, out int bytesWritten) 
        where TFormat : struct, IHFormat
    {
        // TODO: Check the if the type check is elided by JIT or do we have to do it all manually
        if (format is EvaWireFormat wireFormat && wireFormat.IsUnity)
        {
            return TFormat.TryWrite(destination, uniqueId, out bytesWritten);
        }
        return TFormat.TryWrite(destination, (int)uniqueId, out bytesWritten);
    }

    /// <summary>
    /// Writes the specified array <paramref name="values"/> into <paramref name="destination"/>.
    /// </summary>
    /// <param name="format">The <see cref="IHFormat">format</see> used to write the <paramref name="values"/> into <paramref name="destination"/>.</param>
    /// <param name="destination">The destination span where <paramref name="values"/> are written.</param>
    /// <param name="values">The array values.</param>
    /// <param name="bytesWritten">The amount of bytes written into <paramref name="destination"/>.</param>
    /// <returns>true if the entire array was written successfully; otherwise, false.</returns>
    public static bool TryWriteArray<TFormat, TFormattable>(this TFormat format, Span<byte> destination, TFormattable[] values, out int bytesWritten)
        where TFormat : struct, IHFormat
        where TFormattable : IHFormattable
    {
        return TryWriteArray(format, destination, new ReadOnlySpan<TFormattable>(values), out bytesWritten);
    }
    
    /// <inheritdoc cref="TryWriteArray{TFormat, TFormattable}(TFormat, Span{byte}, TFormattable[], out int)"/>
    public static bool TryWriteArray<TFormat, TFormattable>(this TFormat format, Span<byte> destination, ReadOnlySpan<TFormattable> values, out int bytesWritten) 
        where TFormat : struct, IHFormat
        where TFormattable : IHFormattable
    {
        if (!format.TryWriteArrayLength(destination, destination.Length, out bytesWritten))
            return false;

        for (int i = 0; i < values.Length; i++)
        {
            if (!values[i].TryFormat(destination.Slice(bytesWritten), format, out int written, default))
                return false;

            bytesWritten += written;
        }
        return true;
    }

    /// <summary>
    /// Reads the <paramref name="arrayLength"/> from <paramref name="source"/>.
    /// </summary>
    /// <param name="format">The <see cref="IHFormat">format</see> used to read the <paramref name="arrayLength"/> from <paramref name="source"/>.</param>
    /// <param name="source">The source span from which <paramref name="arrayLength"/> is read.</param>
    /// <param name="arrayLength">The array length.</param>
    /// <param name="bytesRead">The amount of bytes read from <paramref name="source"/>.</param>
    /// <returns>true if the array length was read successfully; otherwise, false.</returns>
    public static bool TryReadArrayLength<TFormat>(this TFormat format, ReadOnlySpan<byte> source, out int arrayLength, out int bytesRead)
        where TFormat : struct, IHFormat 
    {
        // TODO: Check the if the type check is elided by JIT or do we have to do it all manually
        if (format is EvaWireFormat evaWire && evaWire.IsUnity)
        {
            bool success = TFormat.TryRead(source, out ushort length, out bytesRead);
            arrayLength = length;
            return success;
        }
        return TFormat.TryRead(source, out arrayLength, out bytesRead);
    }

    /// <summary>
    /// Writes the specified <paramref name="arrayLength"/> into <paramref name="destination"/>.
    /// </summary>
    /// <param name="format">The <see cref="IHFormat">format</see> used to write the <paramref name="arrayLength"/> into <paramref name="destination"/>.</param>
    /// <param name="destination">The destination span where <paramref name="arrayLength"/> is written.</param>
    /// <param name="arrayLength">The array length.</param>
    /// <param name="bytesWritten">The amount of bytes written into <paramref name="destination"/>.</param>
    /// <returns>true if the array length was written successfully; otherwise, false.</returns>
    public static bool TryWriteArrayLength<TFormat>(this TFormat format, Span<byte> destination, int arrayLength, out int bytesWritten)
        where TFormat : struct, IHFormat
    {
        // TODO: Check the if the "is" cast is elided by JIT or do we have to do it all manually
        if (typeof(TFormat) != typeof(EvaWireFormat))
        {
            return TFormat.TryWrite(destination, (ushort)arrayLength, out bytesWritten);
        }
        return TFormat.TryWrite(destination, arrayLength, out bytesWritten);
    }

    public static bool TryWrite<TFormat>(
        this Span<byte> destination, TFormat format,
        [InterpolatedStringHandlerArgument(nameof(destination))]
        ref IHFormat.TryWriteInterpolatedStringHandler<TFormat> handler, out int bytesWritten)
        where TFormat : struct, IHFormat
    {
        return TryWrite(format, destination, ref handler, out bytesWritten);
    }
    public static bool TryWrite<TFormat>(
        this TFormat format, Span<byte> destination,
        [InterpolatedStringHandlerArgument(nameof(destination))] ref IHFormat.TryWriteInterpolatedStringHandler<TFormat> handler, out int bytesWritten) 
        where TFormat : struct, IHFormat
    {
        if (handler._success)
        {
            bytesWritten = handler._position;
            return true;
        }

        bytesWritten = 0;
        return false;
    }
}