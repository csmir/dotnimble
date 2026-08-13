using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.IO;

/// <summary>
///     Extension methods for <see cref="BinaryReader"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BinaryReaderExtensions
{
    extension(BinaryReader reader)
    {
        /// <summary>
        ///     Reads a value of <see langword="unmanaged"/> type <typeparamref name="T"/> from the current stream and advances the current position by that number of bytes.
        /// </summary>
        /// <returns>A value of <see langword="unmanaged"/> type <typeparamref name="T"/> read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public T ReadStruct<T>() where T : unmanaged
        {
#if NET6_0_OR_GREATER
            T value = new();

            unsafe
            {
                reader.ReadExactly(new(&value, sizeof(T)));
            }

            return value;
#else
            byte[] bytes = reader.ReadBytes(sizeof(T));

            if (bytes.Length != sizeof(T))
                throw new EndOfStreamException("Unable to read beyond the end of the stream.");

            fixed (byte* ptr = bytes)
                return Marshal.PtrToStructure<T>((IntPtr)ptr);
#endif
        }

        /// <summary>
        ///     Reads the specified number of elements from the current stream and advances the current position by the read number of bytes.
        /// </summary>
        /// <param name="count">The number of bytes to read. This value must be 0 or a non-negative number or an exception will occur.</param>
        /// <returns>An array containing data read from the underlying stream.</returns>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public T[] ReadStructs<T>(int count) where T : unmanaged
        {
#if NET6_0_OR_GREATER
            Span<T> store = stackalloc T[count];

            reader.ReadExactly(MemoryMarshal.AsBytes(store));

            return store.ToArray();
#else
            int readLength = checked(sizeof(T) * count);

            byte[] bytes = reader.ReadBytes(readLength);

            if (bytes.Length != readLength)
                throw new EndOfStreamException("Unable to read beyond the end of the stream.");

            T[] elements = new T[count];

            fixed (byte* b = bytes)
            fixed (T* t = elements)
            {
                unsafe
                {
                    Buffer.MemoryCopy(b, t, readLength, readLength);
                }
            }

            return elements;
#endif
        }
    }
}
