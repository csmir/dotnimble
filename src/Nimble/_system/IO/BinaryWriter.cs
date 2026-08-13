using System.ComponentModel;

#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace System.IO;

/// <summary>
///     Extension methods for <see cref="BinaryWriter"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BinaryWriterExtensions
{
    extension(BinaryWriter writer)
    {
        /// <summary>
        ///     Writes a value of <see langword="unmanaged"/> type <typeparamref name="T"/> to the underlying stream.
        /// </summary>
        /// <param name="source">The <see langword="unmanaged"/> <see langword="struct"/> to write.</param>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public void WriteStruct<T>(T source) where T : unmanaged
        {
            unsafe
            {
#if NET6_0_OR_GREATER
                writer.Write(new ReadOnlySpan<byte>(&source, sizeof(T)));
#else
                byte[] bytes = new byte[sizeof(T)];

                fixed (byte* dst = bytes)
                {
                    unsafe
                    {
                        Buffer.MemoryCopy(&source, dst, sizeof(T), sizeof(T));
                    }
                }

                writer.Write(bytes);
#endif
            }
        }

        /// <summary>
        ///     Writes an array of <see langword="unmanaged"/> type <typeparamref name="T"/> to the underlying stream.
        /// </summary>
        /// <param name="values">An array containing the values to write.</param>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
#if NET6_0_OR_GREATER
        public void WriteStructs<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            writer.Write(MemoryMarshal.AsBytes(values));
        }
#else
        public void WriteStructs<T>(T[] values) where T : unmanaged
        {
            ArgumentNullException.ThrowIfNull(values);

            foreach (T value in values)
                writer.WriteStruct(value);
        }
#endif
    }
}
