#pragma warning disable IDE0007 // Explicit typing is important for unsafe code.

using System.Runtime.InteropServices;

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

namespace Nimble.Runtime;

/// <summary>
/// This class contains methods for native memory management, in a framework-agnostic manner.
/// </summary>
#if NET8_0_OR_GREATER
[SkipLocalsInit]
#endif
public static class Memory
{
#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    private static void VerifyAlignment(nint alignment)
    {
#if NET6_0_OR_GREATER
        if (alignment < 0 || !BitOperations.IsPow2(alignment))
#else
        if (alignment < 0 || (alignment & (alignment - 1)) != 0)
#endif
            throw new ArgumentOutOfRangeException(nameof(alignment), $"Memory alignments must be a non-negative power of two. Actual value: '{alignment}'.");
    }

    /// <summary>
    ///     Allocates a block of memory of the specified size, in bytes.
    /// </summary>
    /// <param name="length"> The size, in bytes, of the block to allocate. </param>
    /// <param name="alignment"> The alignment, in bytes, of the block to allocate. This must be a power of 2. </param>
    /// <param name="zeroed"> Whether the allocated block should be zeroed. </param>
    /// <returns> A pointer to the allocated block of memory. </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="OutOfMemoryException"/>
    public static unsafe nint Allocate(nuint length, nint alignment = 1, bool zeroed = false)
    {
#if NET6_0_OR_GREATER
        if (alignment == 1)
            return (nint)(zeroed
                ? NativeMemory.AllocZeroed(length)
                : NativeMemory.Alloc(length));

        VerifyAlignment(alignment);

        void* ptr = NativeMemory.AlignedAlloc(length, (nuint)alignment);

        if (zeroed)
            NativeMemory.Clear(ptr, length);

        return (nint)ptr;
#else
        nint allocated;

        if (alignment == 1)
        {
            allocated = Marshal.AllocHGlobal((nint)length);

            if (zeroed)
                Clear(allocated, length);

            return allocated;
        }
        else
        {
            VerifyAlignment(alignment);

            allocated = Marshal.AllocHGlobal((nint)length + alignment + sizeof(nint));

            nint aligned = (allocated + alignment + sizeof(nint) - 1) & ~(alignment - 1);

            if (zeroed)
                Clear(aligned, length);

            ((nint*)aligned)[-1] = allocated;

            return aligned;
        }
#endif
    }

    /// <summary>
    ///     Clears a block of memory.
    /// </summary>
    /// <param name="ptr"> A pointer to the block of memory that should be cleared. </param>
    /// <param name="length"> The size, in bytes, of the block to clear. </param>
    /// <remarks>
    ///     If this method is called with a <see langword="null"/> <paramref name="ptr" /> and <paramref name="length" /> of 0, it is equivalent to a no-op.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Clear(nint ptr, nuint length)
    {
#if NET6_0_OR_GREATER
        NativeMemory.Clear((void*)ptr, length);
#else
        while (length >= sizeof(long))
        {
            *(long*)ptr = 0;

            ptr += sizeof(long);

            length -= sizeof(long);
        }

        while (length > 0)
        {
            *(byte*)ptr = 0;

            ptr++;

            length--;
        }
#endif
    }

    /// <summary>
    ///     Copies a block of memory from memory location <paramref name="source"/>, to memory location <paramref name="destination"/>.
    /// </summary>
    /// <param name="source"> A pointer to the source of data to be copied. </param>
    /// <param name="destination"> A pointer to the destination memory block, where the data is to be copied. </param>
    /// <param name="length"> The size, in bytes, to be copied from the source location to the destination. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Copy(nint source, nint destination, nuint length)
    {
#if NET6_0_OR_GREATER
        NativeMemory.Copy((void*)source, (void*)destination, length);
#else
        Buffer.MemoryCopy((void*)source, (void*)destination, length, length);
#endif
    }

    /// <summary>
    ///     Copies the byte <paramref name="value"/> to the first <paramref name="count"/> bytes of the memory located at <paramref name="ptr"/>.
    /// </summary>
    /// <param name="ptr"> A pointer to the block of memory to fill. </param>
    /// <param name="count"> The number of bytes to be set to <paramref name="value"/>. </param>
    /// <param name="value"> The value to be set. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Fill(nint ptr, nuint count, byte value)
    {
        if (count == 0) return;

#if NET6_0_OR_GREATER
        NativeMemory.Fill((void*)ptr, count, value);
#else
        nuint words = count / (nuint)sizeof(nuint), wValue = 0;
        
        nuint* wp = (nuint*)ptr;

        if (sizeof(nuint) == 8)
        {
            ((byte*)&wValue)[0] = value; ((byte*)&wValue)[1] = value; ((byte*)&wValue)[2] = value; ((byte*)&wValue)[3] = value;
            ((byte*)&wValue)[4] = value; ((byte*)&wValue)[5] = value; ((byte*)&wValue)[6] = value; ((byte*)&wValue)[7] = value;
        }
        else
        {
            ((byte*)&wValue)[0] = value; ((byte*)&wValue)[1] = value; ((byte*)&wValue)[2] = value; ((byte*)&wValue)[3] = value;
        }

        for (nuint i = 0; i < words; i++)
            *wp++ = wValue;

        byte* bp = (byte*)wp;

        for (nuint i = count % (nuint)sizeof(nuint); i > 0; i--)
            *bp++ = value;
#endif
    }

    /// <summary>
    ///     Frees an (optionally aligned) block of memory.
    /// </summary>
    /// <param name="ptr"> A pointer to the block of memory that should be freed. </param>
    /// <param name="aligned"> Whether an aligned free should be used. Must be set when clearing aligned blocks. </param>
    /// <remarks>
    ///    This method does nothing if <paramref name="ptr"/> is <see langword="null"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free(nint ptr, bool aligned = false)
    {
        if (ptr == 0)
            return;

#if NET6_0_OR_GREATER
        if (aligned)
            NativeMemory.AlignedFree((void*)ptr);
        else
            NativeMemory.Free((void*)ptr);
#else
        Marshal.FreeHGlobal(aligned ? ((nint*)ptr)[-1] : ptr);
#endif
    }

    /// <summary>
    ///     Reallocates a block of memory of the specified size, in bytes.
    /// </summary>
    /// <param name="ptr"> The previously allocated block of memory. </param>
    /// <param name="length"> The size, in bytes, of the original memory block. </param>
    /// <param name="newLength"> The size, in bytes, of the reallocated memory block. </param>
    /// <param name="alignment"> The alignment, in bytes, of the block to allocate. This must be a power of 2. </param>
    /// <param name="zeroed"> Whether to zero the newly allocated extension. </param>
    /// <returns> A pointer to the reallocated block of memory. </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="OutOfMemoryException"/>
    /// <remarks>
    ///     <para>
    ///         If <paramref name="ptr"/> is <see langword="null"/>, this method is equivalent to <see cref="Allocate(nuint, nint, bool)"/>.<br/>
    ///         If <paramref name="zeroed"/> is <see langword="false"/> on .NET 6+, <paramref name="length"/> is not required, and is ignored.<br/>
    ///         If <paramref name="newLength"/> is 0, the returned pointer must not be dereferenced, and must be freed.
    ///     </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint Reallocate(nint ptr, nuint length, nuint newLength, nint alignment = 1, bool zeroed = false)
    {
#if NET6_0_OR_GREATER

        if (alignment == 1)
        {
            ptr = (nint)NativeMemory.Realloc((void*)ptr, newLength);
        }
        else
        {
            VerifyAlignment(alignment);
            ptr = (nint)NativeMemory.AlignedRealloc((void*)ptr, newLength, (nuint)alignment);
        }
#else
        nint oldPtr = ptr;

        ptr = Allocate(newLength, alignment);

        if (oldPtr != 0)
        {
            Copy(oldPtr, ptr, newLength > length ? length : newLength);
            Free(oldPtr, alignment != 1);
        }
#endif

        if (zeroed && newLength > length)
            Clear(ptr + (nint)length, newLength - length);

        return ptr;
    }
}