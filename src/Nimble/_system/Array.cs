using System.ComponentModel;

#if NET6_0_OR_GREATER
using Nimble.Collections.Generic;
#else
using System.Runtime.InteropServices;
#endif

namespace System;

/// <summary>
///     Extension methods for <see cref="Array"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArrayExtensions
{
    extension(Array array)
    {
        /// <summary>
        ///     Mutates the provided array by including the provided item at the end of the array. This function is a short-hand of <see cref="Array.Resize{T}(ref T[], int)"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array to push items to.</typeparam>
        /// <param name="arr">The target array for this mutation. If this addition causes the array to grow out of bounds, the method throws.</param>
        /// <param name="item">The item to be included into the array.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the array cannot resize to include the provided item.</exception>
        public static void Include<T>(ref T[] arr, T item)
        {
            var i = arr.Length;

            Array.Resize(ref arr, i + 1);

            arr[i] = item;
        }

        /// <summary>
        ///     Mutates the provided array by including the provided items at the end of the array. This function is a short-hand of <see cref="Array.Resize{T}(ref T[], int)"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array to push items to.</typeparam>
        /// <param name="arr">The target array for this mutation. If this addition causes the array to grow out of bounds, the method throws.</param>
        /// <param name="items">The items to be included into the array.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the array cannot resize to include the provided items.</exception>
        public static void Include<T>(ref T[] arr, params T[] items)
        {
            ArgumentNullException.ThrowIfNull(items, nameof(items));

            if (items.Length == 0)
                return;

            var i = arr.Length;

            Array.Resize(ref arr, arr.Length + items.Length);

            Array.Copy(items, 0, arr, i, items.Length);
        }

        #region Multidimension LINQ

        /// <inheritdoc cref="Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
        public bool MxAll<T>(Func<T, bool> predicate)
        {
            if (array is T[] flat) return flat.All(predicate);

            if (typeof(T).IsValueType)
            {
#if NET6_0_OR_GREATER
                foreach (T item in (ValueArray<T>)array) if (!predicate(item)) return false;
#else
                unsafe
                {
                    GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        int length = array.Length;
                        T* basePtr = (T*)handle.AddrOfPinnedObject();
                        for (int i = 0; i < length; i++) if (!predicate(basePtr[i])) return false;
                    }
                    finally { handle.Free(); }
                }
#endif
            }

            else foreach (T item in array) if (!predicate(item)) return false;

            return true;
        }

        /// <inheritdoc cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        public bool MxAny() => array.Length != 0;

        /// <inheritdoc cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
        public bool MxAny<T>(Func<T, bool> predicate)
        {
            if (array is T[] flat) return flat.Any(predicate);

            if (typeof(T).IsValueType)
            {
#if NET6_0_OR_GREATER
                foreach (T item in (ValueArray<T>)array) if (predicate(item)) return true;
#else
                unsafe
                {
                    GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        int length = array.Length;
                        T* basePtr = (T*)handle.AddrOfPinnedObject();
                        for (int i = 0; i < length; i++) if (predicate(basePtr[i])) return true;
                    }
                    finally { handle.Free(); }
                }
#endif
            }

            else foreach (T item in array) if (predicate(item)) return true;

            return false;
        }

        /// <inheritdoc cref="Enumerable.Count{TSource}(IEnumerable{TSource})"/>
        /// <remarks>
        ///     Because of high size probability, this method returns a <see langword="long"/> instead of an <see langword="int"/> to (try to) avoid overflow exceptions.
        /// </remarks>
        public long MxCount() => array.LongLength;

        /// <inheritdoc cref="Enumerable.Count{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
        public int MxCount<T>(Func<T, bool> predicate)
        {
            if (array is T[] flat) return flat.Count(predicate);

            int count = 0;
            if (typeof(T).IsValueType)
            {
#if NET6_0_OR_GREATER
                foreach (T item in (ValueArray<T>)array) if (predicate(item)) count++;
#else
                unsafe
                {
                    GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        int length = array.Length;
                        T* basePtr = (T*)handle.AddrOfPinnedObject();
                        for (int i = 0; i < length; i++) if (predicate(basePtr[i])) count++;
                    }
                    finally { handle.Free(); }
                }
#endif
            }

            else foreach (T item in array) if (predicate(item)) count++;

            return count;
        }

        #endregion
    }
}
