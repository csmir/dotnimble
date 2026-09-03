#if !NETSTANDARD2_0

using System.Buffers;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Nimble.Buffers;
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

    private SafeRentedArray<char>? _rentedArray;
    private Span<char> _span = new();
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
        int valueLength = value?.Length ?? 0;

        ArgumentOutOfRangeException.ThrowIfOutOfRange(startIndex, 0, valueLength);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)(valueLength - startIndex));

        MaxCapacity = int.MaxValue;

        int initialCapacity = capacity > length ? capacity : length;

        if (initialCapacity > MaxCapacity) ThrowCapacityTooHigh();

        _span = initialCapacity > 0 ? (_rentedArray = SafeArrayPool<char>.Shared.Rent(initialCapacity)).Array.AsSpan(0, Math.Min(initialCapacity, MaxCapacity)) : [];

        if (length != 0)
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
        ArgumentOutOfRangeException.ThrowIfNegative(maxCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        MaxCapacity = maxCapacity;

        if (capacity > maxCapacity)
            ThrowCapacityTooHigh();

        _span = capacity > 0 ? (_rentedArray = SafeArrayPool<char>.Shared.Rent(capacity)).Array.AsSpan(0, Math.Min(capacity, maxCapacity)) : [];
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Vsb"/> class.
    /// </summary>
    /// <param name="initialStore"> The backing store of memory to use, before renting arrays. </param>
    /// <param name="maxCapacity"> The maximum capacity of this builder. </param>
    public ValueStringBuilder(Span<char> initialStore, int maxCapacity = int.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxCapacity);

        MaxCapacity = maxCapacity;

        _span = initialStore[..Math.Min(initialStore.Length, maxCapacity)];
    }

    #endregion

    #region Throw Helpers

    [DoesNotReturn]
    private static void ThrowCapacityTooHigh()
    {
        throw new InvalidOperationException("The requested operation would exceed the maximum capacity of the current ValueStringBuilder instance.");
    }

    [DoesNotReturn]
    private static void ThrowCapacityTooLow()
    {
        throw new InvalidOperationException("The requested operation would reduce capacity below the length of the current ValueStringBuilder instance.");
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
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            if (value > MaxCapacity) ThrowCapacityTooHigh();

            if (value < _position) ThrowCapacityTooLow();

            if (value > _span.Length)
            {
                GrowStorage(value);
            }
            else if (value < _span.Length)
            {
                ShrinkStorage(value);
            }
        }
    }

    /// <summary>
    ///     Gets the maximum capacity this builder is allowed to have.
    /// </summary>
    public int MaxCapacity { get; private init; }

    /// <summary>
    ///     Gets or sets the length of this builder.
    /// </summary>
    public int Length
    {
        readonly get => _position;

        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            if (value < _position)
            {
                _position = value;
                return;
            }

            EnsureCapacity(value);

            unsafe
            {
                fixed (char* c = _span)
                {
                    byte* b = (byte*)(c + _position);
                    NativeMemory.Clear(b, (nuint)(value - _position) * sizeof(char));
                }
            }

            _position = value;
        }
    }

    #endregion

    #region Uncategorized APIs

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool CheckCapacity(int requestedCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(requestedCapacity);

        return requestedCapacity < Capacity;
    }

    /// <summary>
    ///     Ensures that the capacity of this builder is at least the specified value.
    /// </summary>
    /// <param name="requestedCapacity"> The new capacity for this builder. </param>
    /// <remarks>
    ///     If <paramref name="requestedCapacity"/> is less than or equal to the current capacity of this builder, the capacity remains unchanged.
    /// </remarks>
    /// <returns> The builder's new capacity. </returns>
    public int EnsureCapacity(int requestedCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(requestedCapacity);

        if (requestedCapacity <= _span.Length)
            return _span.Length;

        Capacity = _span.Length <= int.MaxValue / 2 ? Math.Max(_span.Length * 2, requestedCapacity) : requestedCapacity;

        return Capacity;
    }

    /// <summary>
    ///     Attempts to grow the builder by an arbitrary amount.
    /// </summary>
    private bool GrowCapacity()
    {
        if (_span.Length == MaxCapacity) return false;

        int current = _span.Length;

        int next = current == 0 ? 16 : current <= int.MaxValue / 2 ? current * 2 : int.MaxValue;

        EnsureCapacity(Math.Min(next, MaxCapacity));

        return true;
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
        readonly get
        {
            ArgumentOutOfRangeException.ThrowIfOutOfRange(index, 0, _position);

            return _span[index];
        }

        set
        {
            ArgumentOutOfRangeException.ThrowIfOutOfRange(index, 0, _position);

            _span[index] = value;
        }
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
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        int endIndex = startIndex + length;

        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)endIndex, (uint)_position);

        FastCopy(_span[endIndex.._position], _span[startIndex..], true);

        _position -= length;

        return ref this;
    }

    /// <summary>
    ///     Returns an enumerator for this <see cref="Vsb"/>.
    /// </summary>
    /// <returns> An enumerator for this builder. </returns>
    public readonly Span<char>.Enumerator GetEnumerator() => _span[.._position].GetEnumerator();

    /// <summary>
    ///     Converts the value of this instance to a <see cref="string"/>.
    /// </summary>
    /// <returns> A string whose value is the same as this instance. </returns>
    public override readonly string ToString()
    {
        return new(_span[.._position]); // This moves directly to a runtime-internal call
    }

    /// <summary>
    ///     Converts the value of a substring of this instance to a <see cref="string"/>.
    /// </summary>
    /// <param name="startIndex"> The starting position of the substring in this instance. </param>
    /// <param name="length"> The length of the substring. </param>
    /// <returns> A string whose value is the same as the specified substring of this instance. </returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public readonly string ToString(int startIndex, int length) => _span[.._position].Slice(startIndex, length).ToString();

    /// <summary>
    ///     Creates a copy of the current <see cref="Vsb"/> instance that is safe to dispose. Standard copies may result in undefined behaviour.
    /// </summary>
    public readonly Vsb CreateValueCopy()
    {
        _rentedArray?.AddReference();

        Vsb copy = new()
        {
            MaxCapacity = MaxCapacity,

            _rentedArray = _rentedArray,
            _position = _position,
            _span = _span,
        };


        return copy;
    }

    /// <inheritdoc />
    public readonly void Dispose()
    {
        _rentedArray?.Dispose();
    }

    #endregion

    #region Internal Helpers

    private readonly void FastCopy(scoped ReadOnlySpan<char> source, scoped Span<char> destination, bool overlap = false)
    {
        if (overlap)
            source.CopyTo(destination);
        else
            Unsafe.CopyBlock(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(destination)), ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(source)), (uint)source.Length * sizeof(char));
    }

    /// <summary>
    ///     Grows the internal buffer to accommodate additional characters.
    /// </summary>
    /// <param name="requestedSize"> The minimum storage size to accomodate. </param>
    private void GrowStorage(int requestedSize)
    {
        SafeRentedArray<char> newArray = SafeArrayPool<char>.Shared.Rent(requestedSize);

        FastCopy(_span[.._position], newArray.Array);

        _rentedArray?.Dispose();

        _span = (_rentedArray = newArray).Array.AsSpan(0, Math.Min(newArray.Array.Length, MaxCapacity));
    }

    /// <summary>
    ///     Shrinks the internal buffer destructively.
    /// </summary>
    /// <param name="requestedSize"> The storage size to attempt to shrink towards. </param>
    private void ShrinkStorage(int requestedSize)
    {
        // Quick path for ShrinkStorage(0)
        if (requestedSize == 0)
        {
            _rentedArray?.Dispose();
            _rentedArray = null;
            _span = [];
            return;
        }

        SafeRentedArray<char> newArray = SafeArrayPool<char>.Shared.Rent(requestedSize);

        FastCopy(_span[..requestedSize], newArray.Array.AsSpan(0, requestedSize));

        _rentedArray?.Dispose();

        _span = (_rentedArray = newArray).Array.AsSpan(0, requestedSize);
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

    public readonly void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        FastCopy(_span.Slice(sourceIndex, count), destination.AsSpan(destinationIndex, count));
    }

    ///  <summary>
    ///     Copies the characters from a specified segment of this instance to a destination <see cref="char"/> span.
    ///  </summary>
    ///  <param name="sourceIndex"> The starting position in this instance where characters will be copied from. The index is zero-based. </param>
    ///  <param name="destination"> The writable span where characters will be copied. </param>
    ///  <param name="count"> The number of characters to be copied. </param>

    public readonly void CopyTo(int sourceIndex, scoped Span<char> destination, int count)
    {
        FastCopy(_span.Slice(sourceIndex, count), destination);
    }

    #endregion

    #region Core Append(...)

    /// <summary>
    ///     Appends a character 0 or more times to the end of this builder.
    /// </summary>
    /// <param name="value"> The character to append. </param>
    /// <param name="repeatCount"> The number of times to append <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(char value, int repeatCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(repeatCount);

        if (repeatCount != 0)
        {
            EnsureCapacity(checked(_position + repeatCount));

            _span[_position..(_position + repeatCount)].Fill(value);

            _position += repeatCount;
        }

        return ref this;
    }

    /// <summary>
    ///     Appends the string representation of a specified <see cref="char"/> object to this instance.
    /// </summary>
    /// <param name="value"> The UTF-16-encoded code unit to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(char value)
    {
        EnsureCapacity(checked(_position + 1));

        // Skip bounds check.
        Unsafe.Add(ref MemoryMarshal.GetReference(_span), _position++) = value;

        return ref this;
    }

    /// <summary>
    ///     Appends the string representation of a specified read-only character span to this instance.
    /// </summary>
    /// <param name="value"> The read-only character span to append. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(scoped ReadOnlySpan<char> value)
    {
        int required = _position + value.Length;

        #region Span Alias Protection

        // Fast paths:
        // 1. If the span doesn't overlap, this is irrelevant.
        // 2. If we have enough capacity, there's no situation where the write could be a problem.
        // *. We don't attempt to use scratch space, as condition 2 is required first.
        if (CheckCapacity(required))
            goto writeFast;

        if (!_span.Overlaps(value))
            goto write;

        scoped Span<char> copy;

        if (value.Length < 2048)
            copy = stackalloc char[value.Length];
        else
            copy = GC.AllocateUninitializedArray<char>(value.Length);

        FastCopy(value, copy);

        value = copy;

        #endregion

    write:
        EnsureCapacity(required);

    writeFast:
        FastCopy(value, _span[_position..]);

        _position += value.Length;

        return ref this;
    }

    #endregion

    #region Type Append(...)

    [UnscopedRef]
    private ref Vsb AppendSpanFormattable<T>(T value) where T : ISpanFormattable => ref AppendSpanFormattable(value, default, null);

    [UnscopedRef]
    private ref Vsb AppendSpanFormattable<T>(T value, string? format, IFormatProvider? provider) where T : ISpanFormattable
    {
        if (!value.TryFormat(_span[_position..], out int charsWritten, format, provider))
        {
            while (!value.TryFormat(_span[_position..], out charsWritten, format, provider))
            {
                if (!GrowCapacity()) ThrowCapacityTooHigh();
            }
        }

        _position += charsWritten;

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
        if (value == null)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(startIndex, 0);
            ArgumentOutOfRangeException.ThrowIfNotEqual(charCount, -1);

            return ref this;
        }

        if (charCount == -1) charCount = value.Length;

        ArgumentOutOfRangeException.ThrowIfOutOfRange(startIndex, 0, value.Length);

        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)(startIndex + charCount), (uint)value.Length);

        if (charCount == 0) return ref this;

        Append(value.AsSpan(startIndex, charCount));

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
        if (value != null) Append(value.AsSpan());

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
    public ref Vsb Append(scoped Vsb value) => ref Append(value._span[..value._position]);

    /// <summary>
    ///     Appends a copy of a substring within a specified builder to this instance.
    /// </summary>
    /// <param name="value"> The builder to append. </param>
    /// <param name="startIndex"> The starting position of the substring within value. </param>
    /// <param name="count"> The number of characters in <paramref name="value"/> to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(scoped Vsb value, int startIndex, int count) => ref Append(value._span.Slice(startIndex, count));

    /// <summary>
    ///     Appends the string representation of a specified Boolean value to this instance.
    /// </summary>
    /// <param name="value"> The Boolean value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(bool value) => ref Append(value.ToString());

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
    public ref Vsb Append(char[]? value)
    {
        if (value != null) Append(value.AsSpan());

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

    #region AppendLine(...)

    /// <summary>
    ///     Appends the default line terminator to the end of the current <see cref="Vsb"/>.
    /// </summary>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine()
    {
        if (!OperatingSystem.IsWindows())
            return ref Append('\n');

        EnsureCapacity(checked(_position + 2));

        Unsafe.As<char, int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), _position)) = 0x000A000D;

        _position += 2;

        return ref this;
    }

    /// <summary>
    ///     Appends a character 0 or more times to the end of this buillder, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The character to append. </param>
    /// <param name="repeatCount"> The number of times to append <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(char value, int repeatCount) => ref Append(value, repeatCount).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified <see cref="char"/> object to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The UTF-16-encoded code unit to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(char value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified read-only character span to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The read-only character span to append. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(scoped ReadOnlySpan<char> value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends a range of characters to the end of this builder, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The characters to append. </param>
    /// <param name="startIndex"> The index to start in <paramref name="value"/>. </param>
    /// <param name="charCount"> The number of characters to read in <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(char[]? value, int startIndex = 0, int charCount = -1) => ref Append(value, startIndex, charCount).AppendLine();

    /// <summary>
    ///     Appends a copy of the specified string followed by the default line terminator to the end of the current <see cref="Vsb"/> object.
    /// </summary>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(string? value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends part of a string to the end of this builder, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    /// <param name="startIndex"> The index to start in <paramref name="value"/>. </param>
    /// <param name="count"> The number of characters to read in <paramref name="value"/>. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(string? value, int startIndex, int count) => ref Append(value, startIndex, count).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified builder to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The builder to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(scoped Vsb value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends a copy of a substring within a specified builder to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The builder to append. </param>
    /// <param name="startIndex"> The starting position of the substring within value. </param>
    /// <param name="count"> The number of characters in <paramref name="value"/> to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(scoped Vsb value, int startIndex, int count) => ref Append(value, startIndex, count).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified Boolean value to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The Boolean value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(bool value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 8-bit signed integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(sbyte value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 8-bit unsigned integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(byte value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 16-bit signed integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(short value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 32-bit signed integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(int value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 64-bit signed integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(long value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified single-precision floating-point number to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(float value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified double-precision floating-point number to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(double value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified decimal number to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(decimal value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 16-bit unsigned integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(ushort value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 32-bit unsigned integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(uint value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified 64-bit unsigned integer to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(ulong value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified object to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The object to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(object? value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of the Unicode characters in a specified array to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The array of characters to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(char[]? value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends the string representation of a specified read-only character memory region to this instance, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The read-only character memory region to append. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(ReadOnlyMemory<char> value) => ref Append(value).AppendLine();

    /// <summary>
    ///     Appends a character buffer to this builder, followed by the default line terminator.
    /// </summary>
    /// <param name="value"> The pointer to the start of the buffer. </param>
    /// <param name="valueCount"> The number of characters in the buffer. </param>
    /// <returns> A reference to this instance after the append operation is completed. </returns>
    [UnscopedRef]
    public unsafe ref Vsb AppendLine(char* value, int valueCount) => ref Append(value, valueCount).AppendLine();

    #endregion

    #region AppendJoin(...)

    #region AppendJoinCore<T>(...)

    [UnscopedRef]
    private ref Vsb AppendJoinCore<T>(ref readonly char separator, int separatorLength, IEnumerable<T> values)
    {
        // Typed hotpaths

        if (values is T[] array) return ref AppendJoinCore(in separator, separatorLength, array);

        if (values is List<T> list) return ref AppendJoinCore(in separator, separatorLength, CollectionsMarshal.AsSpan(list));

        ReadOnlySpan<char> separatorSpan = default; bool useSpan = separatorLength > 1;
        
        if (useSpan) separatorSpan = MemoryMarshal.CreateReadOnlySpan(in separator, separatorLength);

        using IEnumerator<T> enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext()) return ref this;

        Append(enumerator.Current);

        if (useSpan)
        {
            while (enumerator.MoveNext()) Append(separatorSpan).Append(enumerator.Current);
        }
        else if (separatorLength == 1)
        {
            while (enumerator.MoveNext()) Append(separator).Append(enumerator.Current);
        }
        else
        {
            while (enumerator.MoveNext()) Append(enumerator.Current);
        }

        return ref this;
    }

    [UnscopedRef]
    private ref Vsb AppendJoinCore<T>(ref readonly char separator, int separatorLength, scoped ReadOnlySpan<T> values)
    {
        ReadOnlySpan<char> separatorSpan = default; bool useSpan = separatorLength > 1;

        if (useSpan) separatorSpan = MemoryMarshal.CreateReadOnlySpan(in separator, separatorLength);

        Append(values[0]);

        // No alias verification needed, append handles it.
        if (useSpan)
        {
            for (int i = 1; i < values.Length; i++) Append(separatorSpan).Append(values[i]);
        }
        else if (separatorLength == 1)
        {
            for (int i = 1; i < values.Length; i++) Append(separator).Append(values[i]);
        }
        else
        {
            for (int i = 1; i < values.Length; i++) Append(values[i]);
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
        if (values is null || values.Length == 0) return ref this;

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
    public ref Vsb AppendJoin(string? separator, scoped ReadOnlySpan<object?> values)
    {
        if (values.IsEmpty) return ref this;

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
        if (values is null || values.Length == 0) return ref this;

        separator ??= string.Empty;

        int expansionHint = separator.Length * (values.Length - 2);

        foreach (string? value in values) expansionHint += value?.Length ?? 0;

        EnsureCapacity(expansionHint);

        return ref AppendJoinCore(ref GetRawStringData(separator), separator.Length, values);
    }

    /// <summary>
    ///     Concatenates the strings of the provided span, using the specified separator between each string, then appends the result to the current instance of the string builder.
    /// </summary>
    /// <param name="separator"> The character to use as a separator. <paramref name="separator" /> is included in the joined strings only if <paramref name="values" /> has more than one element. </param>
    /// <param name="values"> A span that contains the strings to concatenate and append to the current instance of the string builder. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendJoin(string? separator, scoped ReadOnlySpan<string?> values)
    {
        if (values.IsEmpty) return ref this;

        separator ??= string.Empty;

        int expansionHint = separator.Length * (values.Length - 2);

        foreach (string? value in values) expansionHint += value?.Length ?? 0;

        EnsureCapacity(expansionHint);

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
    public ref Vsb AppendJoin(char separator, scoped ReadOnlySpan<object?> values) => ref AppendJoinCore(ref separator, 1, values);

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
    public ref Vsb AppendJoin(char separator, scoped ReadOnlySpan<string?> values) => ref AppendJoinCore(ref separator, 1, values);

    #endregion

    #region Insert(...)

    private void GrowAndShift(int index, int count, bool skipExpansionCheck = false)
    {
        if (!skipExpansionCheck)
            EnsureCapacity(checked(_position + count));

        // We could be shifting so far that it doesn't overlap.
        Span<char> source = _span[index.._position], destination = _span[(index + count)..];

        FastCopy(source, destination, source.Overlaps(destination));

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
    public ref Vsb Insert(int index, scoped ReadOnlySpan<char> value, int count)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(index, 0, _position);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        bool skipExpansionCheck = false;
        scoped Span<char> copy;

        if (count != 0 && value.Length != 0)
        {
            if (count > int.MaxValue / value.Length)
                ThrowCapacityTooHigh();

            int expansion = value.Length * count;

            #region Span Alias Protection

            if (!_span.Overlaps(value, out int offset))
                goto noOverlap;

            // If the expansion takes place after our alias, we don't need to worry about the alias being overwritten.
            if (index >= (offset + value.Length))
            {
                // If the current capacity is enough, we don't need to worry about the alias being garbage collected.
                if (skipExpansionCheck = CheckCapacity(_position + expansion))
                {
                    // If all of the above is true, there may be space to bypass the allocation.
                    if (TryScratchBuffer(ref value, expansion))
                    {
                        // JIT is happy
                        goto noOverlap;
                    }
                }
            }

            if (value.Length < 2048)
                copy = stackalloc char[value.Length];
            else
                copy = GC.AllocateUninitializedArray<char>(value.Length);

            FastCopy(value, copy);

            value = copy;

            #endregion

        noOverlap:
            GrowAndShift(index, expansion, skipExpansionCheck);

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
        ArgumentOutOfRangeException.ThrowIfOutOfRange(index, 0, _position);

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
    public ref Vsb Insert(int index, scoped ReadOnlySpan<char> value) => ref Insert(index, value, 1);

    [UnscopedRef]
    private ref Vsb InsertSpanFormattable<T>(int index, T value)
    where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[512];

        if (value.TryFormat(buffer, out int charsWritten, default, null))
        {
            GrowAndShift(index, charsWritten);

            FastCopy(buffer[..charsWritten], _span[index..]);

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
    public ref Vsb Replace(scoped ReadOnlySpan<char> oldValue, scoped ReadOnlySpan<char> newValue) => ref Replace(oldValue, newValue, 0, Length);

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
    public ref Vsb Replace(scoped ReadOnlySpan<char> oldValue, scoped ReadOnlySpan<char> newValue, int startIndex, int count)
    {
        if (oldValue.Length == 0)
            return ref this;

        int difference = newValue.Length - oldValue.Length;

        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)(startIndex + count), (uint)_position);

        if (difference == 0)
        {
            if (oldValue.Equals(newValue, StringComparison.Ordinal))
                return ref this;

            return ref ReplaceEqualSpan(_span.Slice(startIndex, count), oldValue, newValue);
        }

        if (difference < 0)
            return ref ReplaceShorterSpan(oldValue, newValue, startIndex, count);

        return ref ReplaceLongerSpan(oldValue, newValue, startIndex, count);
    }

    #region Replace Helpers

    private readonly bool TryScratchBuffer(ref ReadOnlySpan<char> original, int accumulatedOffset = 0)
    {
        // Span doesn't need to be aliased.
        if (!original.Overlaps(_span))
            return true;

        int writeTarget = _position + accumulatedOffset;

        /* At runtime, the builder's full capacity is not always in use, leaving volatile scratch space available to us.
         * Here we check if enough scratch space exists to store the alias, without comitting to further allocations later.
         * The optional parameter 'accumulatedOffset' can be used to indicate an amount of scratch space is already used.
         * This allows us to store multiple things in scratch, assuming the caller tracks the offsets required. */
        if ((Capacity - writeTarget) >= original.Length)
        {
            Span<char> span = _span.Slice(writeTarget, original.Length);

            FastCopy(original, span);

            original = span;

            return true;
        }

        return false;
    }

    [UnscopedRef]
    private ref Vsb ReplaceEqualSpan(scoped Span<char> span, scoped ReadOnlySpan<char> oldValue, scoped ReadOnlySpan<char> newValue)
    {
        #region Span Alias Protection

        int accumulatedOffset = 0;
        scoped Span<char> copy;

        if (!TryScratchBuffer(ref oldValue))
        {
            if (oldValue.Length < 2048)
                copy = stackalloc char[oldValue.Length];
            else
                copy = GC.AllocateUninitializedArray<char>(oldValue.Length);

            FastCopy(oldValue, copy);

            oldValue = copy;
        }
        else
        {
            accumulatedOffset = newValue.Length;
        }

        if (!TryScratchBuffer(ref newValue, accumulatedOffset))
        {
            if (newValue.Length < 2048)
                copy = stackalloc char[newValue.Length];
            else
                copy = GC.AllocateUninitializedArray<char>(newValue.Length);

            FastCopy(newValue, copy);

            newValue = copy;
        }

        #endregion

        #region Replacement

        int offset = 0;

        while (true)
        {
            int index = span[offset..].IndexOf(oldValue);

            if (index < 0)
                break;

            index += offset;

            FastCopy(newValue, span[index..]);

            offset = index + oldValue.Length;
        }

        #endregion

        return ref this;
    }

    [UnscopedRef]
    private ref Vsb ReplaceShorterSpan(scoped ReadOnlySpan<char> oldValue, scoped ReadOnlySpan<char> newValue, int startIndex, int count)
    {
        #region Span Alias Protection

        int accumulatedOffset = 0;
        scoped Span<char> copy;

        if (!TryScratchBuffer(ref oldValue))
        {

            if (oldValue.Length < 2048)
                copy = stackalloc char[oldValue.Length];
            else
                copy = GC.AllocateUninitializedArray<char>(oldValue.Length);

            FastCopy(oldValue, copy);

            oldValue = copy;
        }
        else
        {
            accumulatedOffset = newValue.Length;
        }

        if (!TryScratchBuffer(ref newValue, accumulatedOffset))
        {

            if (newValue.Length < 2048)
                copy = stackalloc char[newValue.Length];
            else
                copy = GC.AllocateUninitializedArray<char>(newValue.Length);

            FastCopy(newValue, copy);

            newValue = copy;
        }

        #endregion

        #region Replacement

        Span<char> span = _span.Slice(startIndex, count);

        int read = 0, write = 0;

        while (true)
        {
            int match = span[read..].IndexOf(oldValue);

            if (match < 0)
            {
                // Copy the final unchanged region.
                int remaining = count - read;

                if (remaining != 0 && read != write)
                    span.Slice(read, remaining).CopyTo(span.Slice(write, remaining));

                write += remaining;
                break;
            }

            match += read;

            // Copy the unchanged region before the match.
            int unchanged = match - read;

            if (unchanged != 0 && read != write)
                span.Slice(read, unchanged).CopyTo(span.Slice(write, unchanged));

            write += unchanged;

            FastCopy(newValue, span.Slice(write, newValue.Length));

            read = match + oldValue.Length;
            write += newValue.Length;
        }

        int removed = count - write;

        if (removed == 0)
            return ref this;

        #endregion

        #region Shift Tail

        // The range after the replacement region must move left once.
        int tailStart = startIndex + count, tailLength = _position - tailStart;

        if (tailLength != 0)
            _span.Slice(tailStart, tailLength).CopyTo(_span.Slice(startIndex + write, tailLength));

        _position -= removed;

        #endregion

        return ref this;
    }

    [UnscopedRef]
    private ref Vsb ReplaceLongerSpan(scoped ReadOnlySpan<char> oldValue, scoped ReadOnlySpan<char> newValue, int startIndex, int count)
    {
        #region Compute Matches

        int difference = newValue.Length - oldValue.Length, end = startIndex + count;

        // First pass: count matches.
        int matches = 0, read = startIndex;

        while (read < end)
        {
            int match = _span[read..end].IndexOf(oldValue);

            if (match < 0)
                break;

            read += match + oldValue.Length;
            matches++;
        }

        if (matches == 0)
            return ref this;

        #endregion

        #region Handle Expansion

        int expansion = checked(matches * difference), writeOffset = expansion;

        bool willGrow = CheckCapacity(_position + expansion);

        scoped Span<char> copy;

        if (!_span.Overlaps(oldValue))
            goto skipOldAlloc;

        if (!willGrow && TryScratchBuffer(ref oldValue, writeOffset))
        {
            writeOffset += oldValue.Length;
            goto skipOldAlloc;
        }

        if (oldValue.Length < 2048)
            copy = stackalloc char[oldValue.Length];
        else
            copy = GC.AllocateUninitializedArray<char>(oldValue.Length);

        FastCopy(oldValue, copy);

        oldValue = copy;

    skipOldAlloc:

        if (!_span.Overlaps(newValue))
            goto skipNewAlloc;

        if (!willGrow && TryScratchBuffer(ref newValue, writeOffset))
        {
            writeOffset += newValue.Length;
            goto skipNewAlloc;
        }

        if (newValue.Length < 2048)
            copy = stackalloc char[newValue.Length];
        else
            copy = GC.AllocateUninitializedArray<char>(newValue.Length);

        FastCopy(newValue, copy);

        newValue = copy;

    skipNewAlloc:
        if (willGrow)
            EnsureCapacity(checked(_position + expansion));

        #endregion

        #region Replacement

        int tailLength = _position - end;

        if (tailLength != 0)
            _span.Slice(end, tailLength).CopyTo(_span.Slice(end + expansion, tailLength));

        _position += expansion;

        Span<char> span = _span.Slice(startIndex, count + expansion);

        // Rewrite from the end so that the expanded destination never overwrites source data that has not yet been consumed.
        int sourceEnd = count, destinationEnd = sourceEnd + expansion;

        while (sourceEnd != 0)
        {
            int match = span[..sourceEnd].LastIndexOf(oldValue);

            if (match < 0)
            {
                // Everything before the first match.
                span[..sourceEnd].CopyTo(span.Slice(destinationEnd - sourceEnd, sourceEnd));

                break;
            }

            int matchEnd = match + oldValue.Length;

            // Move the unchanged suffix preceding our already-written destination.
            int suffixLength = sourceEnd - matchEnd;

            destinationEnd -= suffixLength;

            if (suffixLength != 0)
                span.Slice(matchEnd, suffixLength).CopyTo(span.Slice(destinationEnd, suffixLength));

            destinationEnd -= newValue.Length;

            FastCopy(newValue, span.Slice(destinationEnd, newValue.Length));

            sourceEnd = match;
        }

        #endregion

        return ref this;
    }

    #endregion

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
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)(startIndex + count), (uint)_position);

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
    public ref Vsb Append([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
    {
        this = handler._stringBuilder;

        return ref this;
    }

    /// <summary>
    ///     Appends the specified interpolated string to this instance.
    /// </summary>
    /// <param name="provider"> An object that supplies culture-specific formatting information. </param>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb Append(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref AppendInterpolatedStringHandler handler)
    {
        this = handler._stringBuilder;

        return ref this;
    }

    /// <summary>
    ///     Appends the specified interpolated string followed by the default line terminator to the end of the current <see cref="Vsb"/>.
    /// </summary>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
    {
        this = handler._stringBuilder;

        return ref AppendLine();
    }

    /// <summary>
    ///     Appends the specified interpolated string using the specified format, followed by the default line terminator, to the end of the current <see cref="Vsb"/>.
    /// </summary>
    /// <param name="provider"> An object that supplies culture-specific formatting information. </param>
    /// <param name="handler"> The interpolated string to append. </param>
    /// <returns> A reference to this instance after the append operation has completed. </returns>
    [UnscopedRef]
    public ref Vsb AppendLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref AppendInterpolatedStringHandler handler)
    {
        this = handler._stringBuilder;

        return ref AppendLine();
    }

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
        internal static extern bool TryFormatUnconstrained<T>(Enum _, T value, Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.EnumFormat)] ReadOnlySpan<char> format = default);

        /// <summary>
        ///     The associated builder to append strings to.
        /// </summary>
        internal Vsb _stringBuilder;

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
                    if (TryFormatUnconstrained(null!, value, _stringBuilder.AppendTarget, out int charsWritten))
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
                    if (!spanFormattable.TryFormat(_stringBuilder.AppendTarget, out int charsWritten, format, _provider))
                    {
                        while (!spanFormattable.TryFormat(_stringBuilder.AppendTarget, out charsWritten, format, _provider))
                        {
                            if (!_stringBuilder.GrowCapacity()) ThrowCapacityTooHigh();
                        }
                    }

                    _stringBuilder._position += charsWritten;
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
                AppendFormatted(handler.Text, alignment);
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
        public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
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