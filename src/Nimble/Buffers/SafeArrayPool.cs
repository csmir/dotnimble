#if !NETSTANDARD2_0

using System.Buffers;

namespace Nimble.Buffers;

/// <summary>
///     Provides a reference-counted wrapper for an <see cref="ArrayPool{T}"/> object.
/// </summary>
public class SafeArrayPool<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SafeArrayPool{T}"/> class.
    /// </summary>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> to wrap.</param>
    protected SafeArrayPool(ArrayPool<T> pool)
    {
        _pool = pool;
    }

    internal readonly ArrayPool<T> _pool;

    /// <summary>
    ///     Gets a wrapper over a shared <see cref="ArrayPool{T}"/> instance.
    /// </summary>
    public static SafeArrayPool<T> Shared { get; } = new(ArrayPool<T>.Shared);

    /// <summary>
    ///     Creates a wrapper over a new <see cref="ArrayPool{T}"/> instance.
    /// </summary>
    public static SafeArrayPool<T> Create()
    {
        return new(ArrayPool<T>.Create());
    }

    /// <summary>
    ///     Creates a wrapper over a new <see cref="ArrayPool{T}"/> instance with the specified configuration.
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of an array instance that may be stored in the pool.</param>
    /// <param name="maxArraysPerBucket">
    ///     The maximum number of array instances that may be stored in each bucket in the pool.
    /// </param>
    public static SafeArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket)
    {
        return new(ArrayPool<T>.Create(maxArrayLength, maxArraysPerBucket));
    }

    /// <summary>
    ///     Retrieves a buffer that is at least the specified length, with a reference count of 1.
    /// </summary>
    /// <param name="minimumLength">The minimum length of the underlying array.</param>
    /// <returns>A reference-counted array of type <typeparamref name="T"/>.</returns>
    public SafeRentedArray<T> Rent(int minimumLength)
    {
        return new(new SafeRentedArrayState<T>(_pool.Rent(minimumLength), _pool));
    }
}

/// <summary>
///     Shared state for a reference-counted rented array.
/// </summary>
internal sealed class SafeRentedArrayState<T>
{
    internal SafeRentedArrayState(T[] array, ArrayPool<T> pool)
    {
        Array = array;
        Pool = pool;
        ReferenceCount = 1;
    }

    internal readonly T[] Array;
    internal readonly ArrayPool<T> Pool;

    private int ReferenceCount;

    internal void AddReference()
    {
        while (true)
        {
            int count = Volatile.Read(ref ReferenceCount);

            ObjectDisposedException.ThrowIf(count == 0, this);

            if (Interlocked.CompareExchange(ref ReferenceCount, count + 1, count) == count)
                return;
        }
    }

    internal void Release()
    {
        if (Interlocked.Decrement(ref ReferenceCount) == 0)
            Pool.Return(Array);
    }
}

/// <summary>
///     A reference-counted array, rented from a <see cref="SafeArrayPool{T}"/>.
/// </summary>
public sealed class SafeRentedArray<T> : IDisposable
{
    private readonly SafeRentedArrayState<T> _state;
    private int _disposed;

    internal SafeRentedArray(SafeRentedArrayState<T> state)
    {
        _state = state;
    }

    /// <summary>
    ///     The underlying rented array.
    /// </summary>
    public T[] Array => _state.Array;

    /// <summary>
    ///     Creates another reference to the underlying array.
    /// </summary>
    /// <returns>A new reference to the same rented array.</returns>
    /// <exception cref="ObjectDisposedException">
    ///     The current reference has already been disposed.
    /// </exception>
    public SafeRentedArray<T> AddReference()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        _state.AddReference();

        return new SafeRentedArray<T>(_state);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _state.Release();
    }
}

#endif