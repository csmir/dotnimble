#if NET6_0_OR_GREATER

using System.Runtime.InteropServices;

namespace Nimble.Collections.Generic;

/// <summary>
///     An array that represents <typeparamref name="T"/> as values inside the array.
/// </summary>
/// <typeparam name="T">The type of value the array.</typeparam>
/// <param name="array">The array to reinterpret as a value-bound array.</param>
public class ValueArray<T>(Array array) : IEnumerable<T>
{
    /// <summary>
    ///     Gets the length of the array. This is a short-hand for <see cref="Array.Length"/> on the underlying array.
    /// </summary>
    public int Length => array.Length;

    /// <summary>
    ///     Accesses the value at the provided index in the array. This indexer is a short-hand for <see cref="MemoryMarshal.CreateSpan{T}(ref T, int)"/> with the array's length as the span length.
    /// </summary>
    /// <param name="index">The index of the element to access inside the array.</param>
    /// <returns>The element at the provided <paramref name="index"/> as <typeparamref name="T"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is out of bounds of the array.</exception>
    public T this[int index] 
        => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length)[index];

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() 
        => new ValueArrayEnumerator(array);

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    /// <summary>
    ///     Reinterprets the provided <see cref="Array"/> as a <see cref="ValueArray{T}"/>.
    /// </summary>
    /// <param name="array">The array to reinterpret as a <see cref="ValueArray{T}"/>.</param>
    public static explicit operator ValueArray<T>(Array array) => new(array);

    internal sealed class ValueArrayEnumerator(Array array) : IEnumerator<T>
    {
        private readonly int _length = array.Length;
        private int i = -1;

        public T Current 
            => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), _length)[i];
        
        object IEnumerator.Current => Current!;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => i++ != _length;

        public void Reset() => i = -1;
        
        public void Dispose() { }
    }
}

#endif