#if NET6_0_OR_GREATER

using System.Buffers;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Vsb = Nimble.Text.ValueStringBuilder;

namespace Nimble.Text;

/// <summary>
///     A cheaper alternative to <see cref="System.Text.StringBuilder"/>, with a <see langword="stackalloc"/>-compatible backing store.
///     The provided store (if any) will be used until it overflows, at which point a larger array will be rented from <see cref="ArrayPool{T}.Shared"/> to minimize allocation.
/// </summary>
public ref struct ValueStringBuilder : IDisposable
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetRawStringData")]
    private static extern ref char GetRawStringData(string @this);

    #region Fields

    private Span<char> _span = new();
    private char[]? _rentedArray;
    private int _position;

    #endregion

    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vsb"/> class.
    /// </summary>
    public ValueStringBuilder()
    {
        MaxCapacity = int.MaxValue;
        _span = [];
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vsb"/> class.
    /// </summary>
    /// <param name="value"> The initial contents of this builder. </param>
    /// <param name="capacity"> The initial capacity of this builder. </param>
    public ValueStringBuilder(string? value, int capacity = 16) : this(value, 0, value?.Length ?? 0, capacity) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vsb"/> class.
    /// </summary>
    /// <param name="value"> The initial contents of this builder. </param>
    /// <param name="startIndex"> The index to start in <paramref name="value"/>. </param>
    /// <param name="length"> The number of characters to read in <paramref name="value"/>. </param>
    /// <param name="capacity"> The initial capacity of this builder. </param>
    public ValueStringBuilder(string? value, int startIndex, int length, int capacity)
    {
        int initialCapacity = capacity > length ? capacity : length;

        _span = (initialCapacity > 0) ? (_rentedArray = ArrayPool<char>.Shared.Rent(initialCapacity)) : [];

        if (length > 0)
        {
            FastCopy(value.AsSpan(startIndex, length), _span);
            _position = length;
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vsb"/> class.
    /// </summary>
    /// <param name="capacity"> The initial capacity of this builder. </param>
    /// <param name="maxCapacity"> The maximum capacity of this builder. </param>
    public ValueStringBuilder(int capacity, int maxCapacity = int.MaxValue)
    {
        _span = (capacity > 0) ? (_rentedArray = ArrayPool<char>.Shared.Rent(capacity)) : [];

        MaxCapacity = maxCapacity;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vsb"/> class.
    /// </summary>
    /// <param name="initialStore"> The backing store of memory to use, before renting arrays. </param>
    /// <param name="maxCapacity"> The maximum capacity of this builder. </param>
    public ValueStringBuilder(Span<char> initialStore, int maxCapacity = int.MaxValue)
    {
        MaxCapacity = maxCapacity;
        _span = initialStore;
    }

    #endregion

    #region Properties

    private readonly Span<char> AppendTarget => _span[_position..];

    /// <summary>
    ///     Gets or sets the maximum amount of characters that can be contained in the memory held by the current instance.
    /// </summary>
    public int Capacity
    {
        readonly get => _span.Length;

        set
        {
            if (value > _span.Length)
            {
                Grow(value - _span.Length);
            }
            else if (value < _position)
            {
                _position = value;
            }
        }
    }

    /// <summary>
    ///     Gets the maximum capacity this builder is allowed to have.
    /// </summary>
    public readonly int MaxCapacity { get; }

    /// <summary>
    ///     Gets or sets the length of this builder.
    /// </summary>
    public int Length
    {
        readonly get => _position;

        set => _position = value;
    }

    #endregion

    #region Uncategorized APIs

    /// <summary>
    ///     Ensures that the capacity of this builder is at least the specified value.
    /// </summary>
    /// <param name="capacity"> The new capacity for this builder. </param>
    /// <remarks>
    ///     If <paramref name="capacity"/> is less than or equal to the current capacity of this builder, the capacity remains unchanged.
    /// </remarks>
    /// <returns> The builder's new capacity. </returns>
    public int EnsureCapacity(int capacity)
    {
        if (Capacity < capacity) Capacity = capacity;

        return Capacity;
    }

    /// <summary>
    ///     Removes all characters from the current <see cref="Vsb"/> instance.
    /// </summary>
    /// <returns> A cleared reference to this instance. </returns>
    [UnscopedRef]
    public ref Vsb Clear()
    {
        _position = 0;

        return ref this;
    }

    /// <summary>
    ///     Gets or sets the character at the specified position in this instance.
    /// </summary>
    /// <param name="index"> The position of the character. </param>
    public char this[int index]
    {
        readonly get => _span[index];
        set => _span[index] = value;
    }

    /// <summary>
    ///     Removes a range of characters from this builder.
    /// </summary>
    /// <remarks>
    ///     This method does not reduce the capacity of this builder.
    /// </remarks>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Remove(int startIndex, int length)
    {
        FastCopy(_span[(startIndex + length).._position], _span[startIndex..]);

        _position -= length;

        return ref this;
    }

    /// <summary>
    ///     Returns an enumerator for this <see cref="Vsb"/>.
    /// </summary>
    /// <returns> An enumerator for this builder. </returns>
    public readonly Span<char>.Enumerator GetEnumerator() => _span[.._position].GetEnumerator();

    /// <summary>
    ///     Converts the current contents of the builder to a string, and destroys the builder by returning the rented array to the pool.<br/>
    ///     Only call at the end of the builder's lifetime, as it will no longer be usable after this call.
    /// </summary>
    /// <returns> The string representation of the builder's contents. </returns>
    public override readonly string ToString() => new(_span[.._position]); // This moves directly to a runtime-internal call

    /// <summary>
    ///     Creates a string from a substring of this builder.
    /// </summary>
    /// <param name="startIndex"> The index to start in this builder. </param>
    /// <param name="length"> The number of characters to read in this builder. </param>
    public readonly string ToString(int startIndex, int length) => _span.Slice(startIndex, length).ToString();

    /// <inheritdoc />
    public readonly void Dispose()
    {
        if (_rentedArray != null)
            ArrayPool<char>.Shared.Return(_rentedArray);
    }

    #endregion

    #region Internal Helpers

    private readonly unsafe void FastCopy(ReadOnlySpan<char> source, Span<char> destination)
    {
        fixed (char* s = source, d = destination) Unsafe.CopyBlock(d, s, (uint)source.Length * 2);
    }

    /// <summary>
    ///     Grows the internal buffer to accommodate additional characters.
    /// </summary>
    /// <param name="requested">The minimum number of additional characters required.</param>
    private void Grow(int requested)
    {
        int newCapacity = _span.Length * 2;

        if (newCapacity < _span.Length + requested)
            newCapacity = _span.Length + requested;

        char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);

        FastCopy(_span, newArray.AsSpan(0, _position));

        if (_rentedArray != null)
            ArrayPool<char>.Shared.Return(_rentedArray);

        _span = _rentedArray = newArray;
    }

    [UnscopedRef]
    private ref Vsb AppendSpanFormattable<T>(T value) where T : ISpanFormattable => ref AppendSpanFormattable(value, default, null);

    [UnscopedRef]
    private ref Vsb AppendSpanFormattable<T>(T value, string? format, IFormatProvider? provider) where T : ISpanFormattable
    {
        if (value.TryFormat(_span[_position..], out int charsWritten, format, provider))
        {
            _position += charsWritten;
            return ref this;
        }

        return ref Append(value.ToString());
    }

    #endregion

    #region CopyTo(...)

    /// <summary>
    ///     Copies the characters from a specified segment of this instance to a specified segment of a destination <see cref="char"/> array.
    /// </summary>
    /// <param name="sourceIndex"> The starting position in this instance where characters will be copied from.The index is zero-based. </param>
    /// <param name="destination"> The array where characters will be copied. </param>
    /// <param name="destinationIndex"> The starting position in <paramref name="destination"/> where characters will be copied. The index is zero-based. </param>
    /// <param name="count"> The number of characters to be copied. </param>

    public readonly void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => FastCopy(_span[sourceIndex..], new(destination, destinationIndex, count));

    ///  <summary>
    ///     Copies the characters from a specified segment of this instance to a destination <see cref="char"/> span.
    ///  </summary>
    ///  <param name="sourceIndex"> The starting position in this instance where characters will be copied from. The index is zero-based. </param>
    ///  <param name="destination"> The writable span where characters will be copied. </param>
    ///  <param name="count"> The number of characters to be copied. </param>

    public readonly void CopyTo(int sourceIndex, Span<char> destination, int count) => FastCopy(_span.Slice(sourceIndex, count), destination);

    #endregion

    #region Append(...)

    /// <summary>
    ///     Appends a character 0 or more times to the end of this builder.
    /// </summary>
    /// <param name="value"> The character to append. </param>
    /// <param name="repeatCount"> The number of times to append <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(char value, int repeatCount)
    {
        if (repeatCount != 0)
        {
            EnsureCapacity(_position + repeatCount);

            _span[_position..(_position + repeatCount)].Fill(value);

            _position += repeatCount;
        }

        return ref this;
    }

    /// <summary>
    ///     Appends a range of characters to the end of this builder.
    /// </summary>
    /// <param name="value"> The characters to append. </param>
    /// <param name="startIndex"> The index to start in <paramref name="value"/>. </param>
    /// <param name="charCount"> The number of characters to read in <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(char[]? value, int startIndex = 0, int charCount = -1)
    {
        if (value != null && charCount != 0)
        {
            if (charCount == -1) charCount = value.Length;

            Append(MemoryMarshal.CreateReadOnlySpan(ref value[startIndex], charCount));
        }

        return ref this;
    }

    /// <summary>
    ///     Appends a copy of the specified string to this instance.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(string? value)
    {
        if (value is not null) Append(value.AsSpan());

        return ref this;
    }

    /// <summary>
    ///     Appends part of a string to the end of this builder.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    /// <param name="startIndex"> The index to start in <paramref name="value"/>. </param>
    /// <param name="count"> The number of characters to read in <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(string? value, int startIndex, int count)
    {
        if (value is not null) Append(value.AsSpan(startIndex, count));

        return ref this;
    }

    /// <summary>
    ///     Appends the string representation of a specified builder to this instance.
    /// </summary>
    /// <param name="value"> The builder to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(Vsb value) => ref Append(value._span[..value._position]);

    /// <summary>
    ///     Appends a copy of a substring within a specified builder to this instance.
    /// </summary>
    /// <param name="value"> The builder to append. </param>
    /// <param name="startIndex"> The starting position of the substring within value. </param>
    /// <param name="count"> The number of characters in <paramref name="value"/> to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(Vsb value, int startIndex, int count) => ref Append(value._span.Slice(startIndex, count));

    /// <summary>
    ///     Appends the default line terminator to the end of the current <see cref="Vsb"/>.
    /// </summary>
    /// <returns> A reference to this instance after the append operation has completed. </returns>s
    [UnscopedRef]
    public ref Vsb AppendLine() => ref Append(Environment.NewLine);

    /// <summary>
    ///     Appends a copy of the specified string followed by the default line terminator to the end of the current <see cref="Vsb"/> object.
    /// </summary>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(string? value) => ref Append(value).Append(Environment.NewLine);

    /// <summary>
    ///     Appends the string representation of a specified Boolean value to this instance.
    /// </summary>
    /// <param name="value"> The Boolean value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(bool value) => ref Append(value.ToString());

    /// <summary>
    ///     Appends the string representation of a specified <see cref="char"/> object to this instance.
    /// </summary>
    /// <param name="value"> The UTF-16-encoded code unit to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(char value)
    {
        if (_position >= _span.Length)
            Grow(1);

        // Skip bounds check, Grow(1) will always succeed.
        Unsafe.Add(ref MemoryMarshal.GetReference(_span), _position++) = value;

        return ref this;
    }

    /// <summary>
    ///     Appends the string representation of a specified 8-bit signed integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(sbyte value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 8-bit unsigned integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(byte value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 16-bit signed integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(short value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 32-bit signed integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(int value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 64-bit signed integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(long value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified single-precision floating-point number to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(float value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified double-precision floating-point number to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(double value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified decimal number to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(decimal value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 16-bit unsigned integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(ushort value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 32-bit unsigned integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(uint value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified 64-bit unsigned integer to this instance.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(ulong value) => ref AppendSpanFormattable(value);

    /// <summary>
    ///     Appends the string representation of a specified object to this instance.
    /// </summary>
    /// <param name="value"> The object to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(object? value)
    {
        if (value != null)
        {
            if (value is IFormattable formattable)
            {
                if (value is ISpanFormattable spanFormattable)
                {
                    return ref AppendSpanFormattable(spanFormattable);
                }

                Append(formattable.ToString());
            }
            else
            {
                Append(value.ToString());
            }
        }

        return ref this;
    }

    /// <summary>
    ///     Appends the string representation of the Unicode characters in a specified array to this instance.
    /// </summary>
    /// <param name="value"> The array of characters to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(char[]? value) => ref Append(new Span<char>(value));

    /// <summary>
    ///     Appends the string representation of a specified read-only character span to this instance.
    /// </summary>
    /// <param name="value"> The read-only character span to append. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(ReadOnlySpan<char> value)
    {
        int length = value.Length;

        if (_position + length > _span.Length) Grow(length);

        FastCopy(value, _span[_position..]);

        _position += length;

        return ref this;
    }

    /// <summary>
    ///     Appends the string representation of a specified read-only character memory region to this instance.
    /// </summary>
    /// <param name="value"> The read-only character memory region to append. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(ReadOnlyMemory<char> value) => ref Append(value.Span);

    /// <summary>
    ///     Appends a character buffer to this builder.
    /// </summary>
    /// <param name="value"> The pointer to the start of the buffer. </param>
    /// <param name="valueCount"> The number of characters in the buffer. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public unsafe ref Vsb Append(char* value, int valueCount) => ref Append(new ReadOnlySpan<char>(value, valueCount));

    #endregion

    #region AppendJoin(...)

    #region AppendJoinCore<T>(...)

    [UnscopedRef]
    private ref Vsb AppendJoinCore<T>(ref readonly char separator, int separatorLength, IEnumerable<T> values)
    {
        // Typed hotpaths

        if (values is T[] array) return ref AppendJoinCore<T>(in separator, separatorLength, array);

        if (values is List<T> list) return ref AppendJoinCore<T>(in separator, separatorLength, CollectionsMarshal.AsSpan(list));

        ReadOnlySpan<char> separatorSpan = default; bool useSpan = separatorLength > 1;
        
        if (useSpan) separatorSpan = MemoryMarshal.CreateReadOnlySpan(in separator, separatorLength);

        using IEnumerator<T> enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext()) return ref this;

        Append(enumerator.Current);

        if (useSpan)
        {
            while (enumerator.MoveNext()) Append(separatorSpan).Append(enumerator.Current);
        }
        else
        {
            while (enumerator.MoveNext()) Append(separator).Append(enumerator.Current);
        }


        return ref this;
    }

    [UnscopedRef]
    private ref Vsb AppendJoinCore<T>(ref readonly char separator, int separatorLength, ReadOnlySpan<T> values)
    {
        ReadOnlySpan<char> separatorSpan = default; bool useSpan = separatorLength > 1;

        if (useSpan) separatorSpan = MemoryMarshal.CreateReadOnlySpan(in separator, separatorLength);

        if (!values.IsEmpty)
        {
            Append(values[0]);

            if (useSpan)
            {
                for (int i = 1; i < values.Length; i++) Append(separatorSpan).Append(values[i]);
            }
            else
            {
                for (int i = 1; i < values.Length; i++) Append(separator).Append(values[i]);
            }
        }

        return ref this;
    }

    #endregion

    /// <summary>
    ///     Concatenates the string representations of the elements in the provided array of objects, using the specified separator between each member, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> An array that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(string? separator, params object?[] values)
    {
        separator ??= string.Empty;

        return ref AppendJoinCore(ref GetRawStringData(separator), separator.Length, values);
    }

    /// <summary>
    ///     Concatenates the string representations of the elements in the provided array of objects, using the specified separator between each member, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A span that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(string? separator, ReadOnlySpan<object?> values)
    {
        separator ??= string.Empty;

        return ref AppendJoinCore(ref GetRawStringData(separator), separator.Length, values);
    }

    /// <summary>
    ///     Concatenates and appends the members of a collection, using the specified separator between each member.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the concatenated and appended strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A collection that contains the objects to concatenate and append to the current instance of the string builder. </param>
    /// <typeparam name="T"> The type of the members of <paramref name="values" />. </typeparam>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin<T>(string? separator, IEnumerable<T> values)
    {
        separator ??= string.Empty;

        return ref AppendJoinCore(ref GetRawStringData(separator), separator.Length, values);
    }

    /// <summary>
    ///     Concatenates the strings of the provided span, using the specified separator between each string, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> An array that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(string? separator, params string?[] values)
    {
        separator ??= string.Empty;

        return ref AppendJoinCore(ref GetRawStringData(separator), separator.Length, values);
    }

    /// <summary>
    ///     Concatenates the strings of the provided span, using the specified separator between each string, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A span that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(string? separator, ReadOnlySpan<string?> values)
    {
        separator ??= string.Empty;

        return ref AppendJoinCore(ref GetRawStringData(separator), separator.Length, values);
    }

    /// <summary>
    ///     Concatenates the string representations of the elements in the provided array of objects, using the specified char separator between each member, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> An array that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(char separator, params object?[] values) => ref AppendJoinCore(ref separator, 1, values);

    /// <summary>
    ///     Concatenates the string representations of the elements in the provided array of objects, using the specified char separator between each member, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A span that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(char separator, ReadOnlySpan<object?> values) => ref AppendJoinCore(ref separator, 1, values);

    /// <summary>
    ///     Concatenates and appends the members of a collection, using the specified char separator between each member.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the concatenated and appended strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A collection that contains the objects to concatenate and append to the current instance of the string builder. </param>
    /// <typeparam name="T"> The type of the members of <paramref name="values" />. </typeparam>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin<T>(char separator, IEnumerable<T> values) => ref AppendJoinCore(ref separator, 1, values);

    /// <summary>
    ///     Concatenates the strings of the provided span, using the specified char separator between each string, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> An array that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(char separator, params string?[] values) => ref AppendJoinCore(ref separator, 1, values);

    /// <summary>
    ///     Concatenates the strings of the provided span, using the specified char separator between each string, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A span that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(char separator, ReadOnlySpan<string?> values) => ref AppendJoinCore(ref separator, 1, values);

    #endregion

    #region Insert(...)

    private void GrowAndShift(int index, int count)
    {
        EnsureCapacity(_position + count);

        FastCopy(_span[index.._position], _span[(index + count)..]);

        _position += count;
    }

    /// <summary>
    ///     Inserts a string 0 or more times into this builder at the specified position.
    /// </summary>
    /// <param name="index"> The index to insert in this builder. </param>
    /// <param name="value"> The string to insert. </param>
    /// <param name="count"> The number of times to insert the string. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, string? value, int count) => ref Insert(index, value.AsSpan(), count);

    /// <summary>
    ///     Inserts a sequence of characters 0 or more times into this builder at the specified position.
    /// </summary>
    /// <param name="index"> The index to insert in this builder. </param>
    /// <param name="value"> The string to insert. </param>
    /// <param name="count"> The number of times to insert the string. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, ReadOnlySpan<char> value, int count)
    {
        if (count != 0 && value.Length != 0)
        {
            int expansion = value.Length * count;

            GrowAndShift(index, expansion);

            Span<char> destination = _span.Slice(index, expansion);

            for (int i = 0; i < count; i++) FastCopy(value, destination[(i * value.Length)..]);
        }

        return ref this;
    }

    /// <summary>
    ///     Inserts a string into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, string? value) => ref Insert(index, value.AsSpan());

    /// <summary>
    ///     Inserts the string representation of a Boolean value into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, bool value) => ref Insert(index, value.ToString().AsSpan());

    /// <summary>
    ///     Inserts the string representation of a specified 8-bit signed integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, sbyte value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified 8-bit unsigned integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, byte value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified 16-bit signed integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, short value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified Unicode character into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, char value)
    {
        GrowAndShift(index, 1);

        _span[index] = value;

        return ref this;
    }

    /// <summary>
    ///     Inserts the string representation of a specified array of Unicode characters into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> A character array. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, char[]? value) => ref Insert(index, value.AsSpan());

    /// <summary>
    ///     Inserts the string representation of a specified subarray of Unicode characters into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> A character array. </param>
    /// <param name="startIndex"> The starting index within <paramref name="value" />. </param>
    /// <param name="charCount"> The number of characters to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, char[]? value, int startIndex, int charCount) => ref Insert(index, value.AsSpan(startIndex, charCount));

    /// <summary>
    ///     Inserts the string representation of a specified 32-bit signed integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, int value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified 64-bit signed integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, long value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a single-precision floating-point number into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, float value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a double-precision floating-point number into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, double value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a decimal number into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, decimal value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified 16-bit unsigned integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, ushort value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified 32-bit unsigned integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, uint value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of a specified 64-bit unsigned integer into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, ulong value) => ref InsertSpanFormattable(index, value);

    /// <summary>
    ///     Inserts the string representation of an object into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, object? value) => ref (value == null) ? ref this : ref Insert(index, value.ToString().AsSpan());

    /// <summary>
    ///     Inserts the sequence of characters into this instance at the specified character position.
    /// </summary>
    /// <param name="index"> The position in this instance where insertion begins. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> A reference to this instance after the insert operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Insert(int index, ReadOnlySpan<char> value)
    {
        if (value.Length != 0)
        {
            GrowAndShift(index, value.Length);

            FastCopy(value, _span[index..]);
        }

        return ref this;
    }

    [UnscopedRef]
    private ref Vsb InsertSpanFormattable<T>(int index, T value) where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[512];

        if (value.TryFormat(buffer, out int charsWritten, default, null))
        {
            GrowAndShift(index, charsWritten);

            FastCopy(buffer, _span[index..]);

            return ref this;
        }

        return ref Insert(index, value.ToString().AsSpan());
    }

    #endregion

    #region Replace(...)

    /// <summary>
    ///     Replaces all occurrences of a specified string in this instance with another specified string.
    /// </summary>
    /// <param name="oldValue"> The string to replace. </param>
    /// <param name="newValue"> The string that replaces <paramref name="oldValue" />, or <see langword="null"/>. </param>
    /// <returns> A reference to this instance with <paramref name="oldValue" /> replaced by <paramref name="newValue" />. </returns>
    [UnscopedRef]
    public ref Vsb Replace(string? oldValue, string? newValue) => ref Replace(oldValue.AsSpan(), newValue.AsSpan(), 0, Length);

    /// <summary>
    ///     Replaces all instances of one read-only character span with another in this builder.
    /// </summary>
    /// <param name="oldValue"> The read-only character span to replace. </param>
    /// <param name="newValue"> The read-only character span to replace <paramref name="oldValue" /> with. </param>
    /// <returns> A reference to this instance with <paramref name="oldValue" /> replaced by <paramref name="newValue" />. </returns>
    [UnscopedRef]
    public ref Vsb Replace(ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue) => ref Replace(oldValue, newValue, 0, Length);

    /// <summary>
    ///     Replaces, within a substring of this instance, of a specified string in this instance with another specified string.
    /// </summary>
    /// <param name="oldValue"> The string to replace. </param>
    /// <param name="newValue"> The string that replaces <paramref name="oldValue" />, or <see langword="null"/>. </param>
    /// <param name="startIndex"> The position in this instance where the substring begins. </param>
    /// <param name="count"> The length of the substring. </param>
    /// <returns> A reference to this instance with <paramref name="oldValue" /> replaced by <paramref name="newValue" /> in the range from <paramref name="startIndex" /> to <paramref name="startIndex" /> + <paramref name="count" /> -1. </returns>
    [UnscopedRef]
    public ref Vsb Replace(string? oldValue, string? newValue, int startIndex, int count) => ref Replace(oldValue.AsSpan(), newValue.AsSpan(), startIndex, count);

    /// <summary>
    ///     Replaces all instances of one read-only character span with another in part of this builder.
    /// </summary>
    /// <param name="oldValue"> The read-only character span to replace. </param>
    /// <param name="newValue"> The read-only character span to replace <paramref name="oldValue" /> with. </param>
    /// <param name="startIndex"> The index to start in this builder. </param>
    /// <param name="count"> The number of characters to read in this builder. </param>
    /// <returns> A reference to this instance with <paramref name="oldValue" /> replaced by <paramref name="newValue" /> in the range from <paramref name="startIndex" /> to <paramref name="startIndex" /> + <paramref name="count" /> -1. </returns>
    [UnscopedRef]
    public ref Vsb Replace(ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue, int startIndex, int count)
    {
        int difference = newValue.Length - oldValue.Length, position = startIndex, end = startIndex + count;

        while (position < end)
        {
            int searchLength = end - position, relativeIndex = _span.Slice(position, searchLength).IndexOf(oldValue, StringComparison.Ordinal), absoluteIndex = position + relativeIndex;

            if (relativeIndex == -1) break;

            if (difference > 0)
            {
                GrowAndShift(absoluteIndex + oldValue.Length, difference);
                end += difference;
            }
            else if (difference < 0)
            {
                Remove(absoluteIndex + newValue.Length, -difference);
                end += difference;
            }

            FastCopy(newValue, _span[absoluteIndex..]);

            position = absoluteIndex + newValue.Length;
        }

        return ref this;
    }

    /// <summary>
    ///     Replaces all occurrences of a specified character in this instance with another specified character.
    /// </summary>
    /// <param name="oldChar"> The character to replace. </param>
    /// <param name="newChar"> The character that replaces <paramref name="oldChar" />. </param>
    /// <returns>A reference to this instance with <paramref name="oldChar" /> replaced by <paramref name="newChar" />.</returns>
    [UnscopedRef]
    public ref Vsb Replace(char oldChar, char newChar) => ref Replace(oldChar, newChar, 0, Length);

    /// <summary>
    ///     Replaces, within a substring of this instance, all occurrences of a specified character with another specified character.
    /// </summary>
    /// <param name="oldChar"> The character to replace. </param>
    /// <param name="newChar"> The character that replaces <paramref name="oldChar" />. </param>
    /// <param name="startIndex"> The position in this instance where the substring begins. </param>
    /// <param name="count"> The length of the substring. </param>
    /// <returns> A reference to this instance with <paramref name="oldChar" /> replaced by <paramref name="newChar" /> in the range from <paramref name="startIndex" /> to <paramref name="startIndex" /> + <paramref name="count" /> -1. </returns>
    [UnscopedRef]
    public ref Vsb Replace(char oldChar, char newChar, int startIndex, int count)
    {
        _span.Slice(startIndex, count).Replace(oldChar, newChar);

        return ref this;
    }

    #endregion

    #region Interpolated String Handling

#pragma warning disable IDE0060

    /// <summary>
    ///     Appends the specified interpolated string to this instance.
    /// </summary>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler) => ref this;

    /// <summary>
    ///     Appends the specified interpolated string to this instance.
    /// </summary>
    /// <param name="provider"> An object that supplies culture-specific formatting information. </param>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref AppendInterpolatedStringHandler handler) => ref this;

    /// <summary>
    ///     Appends the specified interpolated string followed by the default line terminator to the end of the current <see cref="Vsb"/>.
    /// </summary>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler) => ref AppendLine();

    /// <summary>
    ///     Appends the specified interpolated string using the specified format, followed by the default line terminator, to the end of the current <see cref="Vsb"/>.
    /// </summary>
    /// <param name="provider"> An object that supplies culture-specific formatting information. </param>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref AppendInterpolatedStringHandler handler) => ref AppendLine();

    #pragma warning restore IDE0060

    #endregion

    #region Interpolation Handler

    /// <summary>
    ///     Provides a handler used by the language compiler to append interpolated strings into <see cref="Vsb"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public ref struct AppendInterpolatedStringHandler
    {
        /// <summary>
        ///     [<see cref="UnsafeAccessorAttribute"/>] Tries to format the value of the enumerated type instance into the provided span of characters.
        /// </summary>
        /// <remarks>
        ///     This is same as the implementation for <see cref="Enum.TryFormat"/>.
        ///     It is separated out as TryFormat has constrains on the TEnum, and we internally want to use this method in cases where we dynamically validate a generic T is an enum.
        ///     It's a manual copy/paste right now to avoid pressure on the JIT's inlining mechanisms.
        /// </remarks>
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "TryFormatUnconstrained")]
        internal static extern bool TryFormatUnconstrained<T>(T value, Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.EnumFormat)] ReadOnlySpan<char> format = default);

        /// <summary>
        ///     The associated builder to append strings to.
        /// </summary>
        private Vsb _stringBuilder;

        /// <summary>
        ///     Optional provider to pass to <see cref="IFormattable.ToString"/> or <see cref="ISpanFormattable.TryFormat"/> calls.
        /// </summary>
        private readonly IFormatProvider? _provider;

        /// <summary>
        ///     Whether <see cref="_provider"/> provides an <see cref="ICustomFormatter"/>.
        /// </summary>
        private readonly bool _hasCustomFormatter;

        /// <summary>
        ///     Creates a handler used to append an interpolated string into a <see cref="Vsb"/>.
        /// </summary>
        /// <param name="literalLength"> The number of constant characters outside of interpolation expressions in the interpolated string. </param>
        /// <param name="formattedCount"> The number of interpolation expressions in the interpolated string. </param>
        /// <param name="stringBuilder"> The associated <see cref="Vsb"/> to which to append. </param>
        /// <remarks>
        ///     This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.
        /// </remarks>
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, Vsb stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _provider = null;
            _hasCustomFormatter = false;
        }

        /// <summary>
        ///     Creates a handler used to translate an interpolated string into a <see cref="string"/>.
        /// </summary>
        /// <param name="literalLength"> The number of constant characters outside of interpolation expressions in the interpolated string. </param>
        /// <param name="formattedCount"> The number of interpolation expressions in the interpolated string. </param>
        /// <param name="stringBuilder"> The associated <see cref="Vsb"/> to which to append. </param>
        /// <param name="provider"> An object that supplies culture-specific formatting information. </param>
        /// <remarks>
        ///     This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.
        /// </remarks>
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, Vsb stringBuilder, IFormatProvider? provider)
        {
            _stringBuilder = stringBuilder;
            _provider = provider;
            _hasCustomFormatter = provider != null && provider.GetType() != typeof(CultureInfo) && provider.GetFormat(typeof(ICustomFormatter)) != null;
        }

        /// <summary>
        ///     Writes the specified string to the handler.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public void AppendLiteral(string value) => _stringBuilder.Append(value);

        #region AppendFormatted

        #region AppendFormatted T

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <typeparam name="T"> The type of the value to write. </typeparam>
        public void AppendFormatted<T>(T value) => AppendFormatted(value, null);

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <param name="format"> The format string. </param>
        /// <typeparam name="T"> The type of the value to write. </typeparam>
        public void AppendFormatted<T>(T value, string? format)
        {
            if (_hasCustomFormatter)
            {
                // If there's a custom formatter, always use it.
                AppendCustomFormatter(value, format);
                return;
            }

            if (value is null) return;

            // Check first for IFormattable, even though we'll prefer to use ISpanFormattable, as the latter requires the former.
            // For value types, it won't matter as the type checks devolve into JIT-time constants.
            // For reference types, they're more likely to implement IFormattable than they are to implement ISpanFormattable.
            // If they don't implement either, we save an interface check over first checking for ISpanFormattable and then for IFormattable, and if it only implements IFormattable, we come out even.
            // Only if it implements both do we end up paying for an extra interface check.

            if (value is IFormattable formattable)
            {
                if (typeof(T).IsEnum)
                {
                    if (TryFormatUnconstrained(value, _stringBuilder.AppendTarget, out int charsWritten))
                    {
                        _stringBuilder.Length += charsWritten;
                    }
                    else
                    {
                        _stringBuilder.Append(formattable.ToString(format, _provider));
                    }
                }
                else if (value is ISpanFormattable spanFormattable)
                {
                    if (spanFormattable.TryFormat(_stringBuilder.AppendTarget, out int charsWritten, format, _provider)) // constrained call avoiding boxing for value types
                    {
                        _stringBuilder.Length += charsWritten;
                    }
                    else
                    {
                        _stringBuilder.Append(spanFormattable.ToString(format, _provider));
                    }
                }
                else
                {
                    _stringBuilder.Append(formattable.ToString(format, _provider)); // constrained call avoiding boxing for value types
                }
            }
            else
            {
                _stringBuilder.Append(value.ToString());
            }
        }

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <param name="alignment"> Minimum number of characters that should be written for this value. If negative, it indicates left-aligned and the required minimum is the absolute value. </param>
        /// <typeparam name="T"> The type of the value to write. </typeparam>
        public void AppendFormatted<T>(T value, int alignment) => AppendFormatted(value, alignment, null);

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <param name="alignment"> Minimum number of characters that should be written for this value. If negative, it indicates left-aligned and the required minimum is the absolute value. </param>
        /// <param name="format"> The format string. </param>
        /// <typeparam name="T"> The type of the value to write. </typeparam>
        public void AppendFormatted<T>(T value, int alignment, string? format)
        {
            if (alignment == 0)
            {
                // This overload is used as a fallback from several disambiguation overloads, so special-case 0.
                AppendFormatted(value, format);
            }
            else if (alignment < 0)
            {
                // Left aligned: format into the handler, then append any additional padding required.
                int start = _stringBuilder.Length;

                AppendFormatted(value, format);

                int paddingRequired = -alignment - (_stringBuilder.Length - start);

                if (paddingRequired > 0) _stringBuilder.Append(' ', paddingRequired);
            }
            else
            {
                DefaultInterpolatedStringHandler handler = new(0, 0, _provider, stackalloc char[512]);
                handler.AppendFormatted(value, format);
                AppendFormatted(handler.Text.ToString(), alignment);
                handler.Clear();
            }
        }

        #endregion

        #region AppendFormatted ReadOnlySpan<char>

        /// <summary>
        ///     Writes the specified character span to the handler.
        /// </summary>
        /// <param name="value"> The span to write. </param>
        public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilder.Append(value);

        /// <summary>
        ///     Writes the specified string of chars to the handler.
        /// </summary>
        /// <param name="value"> The span to write. </param>
        /// <param name="alignment"> Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value. </param>
        /// <param name="format"> The format string. </param>
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        {
            if (alignment == 0)
            {
                _stringBuilder.Append(value);
            }
            else
            {
                bool leftAlign = false;
                if (alignment < 0)
                {
                    leftAlign = true;
                    alignment = -alignment;
                }

                int paddingRequired = alignment - value.Length;
                if (paddingRequired <= 0)
                {
                    _stringBuilder.Append(value);
                }
                else if (leftAlign)
                {
                    _stringBuilder.Append(value);
                    _stringBuilder.Append(' ', paddingRequired);
                }
                else
                {
                    _stringBuilder.Append(' ', paddingRequired);
                    _stringBuilder.Append(value);
                }
            }
        }

        #endregion

        #region AppendFormatted string

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        public void AppendFormatted(string? value)
        {
            if (!_hasCustomFormatter)
            {
                _stringBuilder.Append(value);
            }
            else
            {
                AppendFormatted<string?>(value);
            }
        }

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <param name="alignment"> Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value. </param>
        /// <param name="format"> The format string. </param>
        /// <remarks>
        ///     Format is meaningless for strings and doesn't make sense for someone to specify.
        ///     We have the overload simply to disambiguate between <see cref="ReadOnlySpan{T}"/> and object, just in case someone does specify a format, as string is implicitly convertible to both.
        ///     Just delegate to the T-based implementation.
        /// </remarks>
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => AppendFormatted<string?>(value, alignment, format);

        #endregion

        #region AppendFormatted object

        /// <summary>
        ///     Writes the specified value to the handler.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <param name="alignment"> Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value. </param>
        /// <param name="format"> The format string. </param>
        /// <remarks>
        ///     This overload is expected to be used rarely, only if either:
        ///     <list type="bullet">
        ///         <item> Something strongly typed as object is formatted with both an alignment and a format. </item>
        ///         <item> The compiler is unable to target type to T. </item>
        ///     </list>
        ///     It exists purely to help make the second case compile. Just delegate to the T-based implementation.
        /// </remarks>
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => AppendFormatted<object?>(value, alignment, format);

        #endregion

        #endregion

        /// <summary>
        ///     Formats the value using the custom formatter from the provider.
        /// </summary>
        /// <param name="value"> The value to write. </param>
        /// <param name="format"> The format string. </param>
        /// <typeparam name="T"> The type of the value to write. </typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendCustomFormatter<T>(T value, string? format)
        {
            // This case is very rare, but we need to handle it prior to the other checks in case a provider was used that supplied an ICustomFormatter which wanted to intercept the particular value.
            // We do the cast here rather than in the ctor, even though this could be executed multiple times per formatting, to make the cast pay for play.

            ICustomFormatter? formatter = (ICustomFormatter?)_provider!.GetFormat(typeof(ICustomFormatter));

            if (formatter is not null) _stringBuilder.Append(formatter.Format(format, value, _provider));
        }
    }

    #endregion
}

#endif