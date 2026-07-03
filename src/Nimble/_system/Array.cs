using Nimble.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System;

/// <summary>
///     Extension methods for <see cref="Array"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArrayExtensions
{
    extension<T>(T[] arr)
    {
        /// <summary>
        ///     Converts the provided array into a value-tuple of 2 elements. This method throws if the array has less than 2 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 2 elements.</param>
        /// <returns>A value-tuple containing the first two elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T) AsTuple2(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 2, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 2, nameof(arr));

            return (arr[0], arr[1]);
        }

        /// <summary>
        ///     Converts the provided array into a value-tuple of 3 elements. This method throws if the array has less than 3 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 3 elements.</param>
        /// <returns>A value-tuple containing the first three elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T, T) AsTuple3(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 3, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 3, nameof(arr));

            return (arr[0], arr[1], arr[2]);
        }

        /// <summary>
        ///     Converts the provided array into a value-tuple of 4 elements. This method throws if the array has less than 4 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 4 elements.</param>
        /// <returns>A value-tuple containing the first four elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T, T, T) AsTuple4(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 4, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 4, nameof(arr));

            return (arr[0], arr[1], arr[2], arr[3]);
        }

        /// <summary>
        ///     Converts the provided array into a value-tuple of 5 elements. This method throws if the array has less than 5 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 5 elements.</param>
        /// <returns>A value-tuple containing the first five elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T, T, T, T) AsTuple5(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 5, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 5, nameof(arr));

            return (arr[0], arr[1], arr[2], arr[3], arr[4]);
        }

        /// <summary>
        ///     Converts the provided array into a value-tuple of 6 elements. This method throws if the array has less than 6 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 6 elements.</param>
        /// <returns>A value-tuple containing the first six elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T, T, T, T, T) AsTuple6(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 6, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 6, nameof(arr));

            return (arr[0], arr[1], arr[2], arr[3], arr[4], arr[5]);
        }

        /// <summary>
        ///     Converts the provided array into a value-tuple of 7 elements. This method throws if the array has less than 7 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 7 elements.</param>
        /// <returns>A value-tuple containing the first seven elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T, T, T, T, T, T) AsTuple7(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 7, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 7, nameof(arr));

            return (arr[0], arr[1], arr[2], arr[3], arr[4], arr[5], arr[6]);
        }

        /// <summary>
        ///     Converts the provided array into a value-tuple of 8 elements. This method throws if the array has less than 8 elements.
        /// </summary>
        /// <param name="validateLongerLength">If set to <see langword="true"/>, the method will throw if the array has more than 8 elements.</param>
        /// <returns>A value-tuple containing the first eight elements of the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public (T, T, T, T, T, T, T, T) AsTuple8(bool validateLongerLength = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(arr.Length, 8, nameof(arr));

            if (validateLongerLength)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(arr.Length, 8, nameof(arr));

            return (arr[0], arr[1], arr[2], arr[3], arr[4], arr[5], arr[6], arr[7]);
        }
    }

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
        #pragma warning disable CS8500

        /// <inheritdoc cref="Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
        public unsafe bool MxAll<T>(Func<T, bool> predicate)
        {
            if (array is T[] flat) return flat.All(predicate);

            if (typeof(T).IsValueType)
            {
#if NET6_0_OR_GREATER
                foreach (T item in (ValueArray<T>)array) if (!predicate(item)) return false;
#else
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                try
                {
                    int length = array.Length;
                    T* basePtr = (T*)handle.AddrOfPinnedObject();
                    for (int i = 0; i < length; i++) if (!predicate(basePtr[i])) return false;
                }
                finally { handle.Free(); }
#endif
            }

            else foreach (T item in array) if (!predicate(item)) return false;

            return true;
        }

        /// <inheritdoc cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        public bool MxAny() => array.Length != 0;

        /// <inheritdoc cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
        public unsafe bool MxAny<T>(Func<T, bool> predicate)
        {
            if (array is T[] flat) return flat.Any(predicate);

            if (typeof(T).IsValueType)
            {
#if NET6_0_OR_GREATER
                foreach (T item in (ValueArray<T>)array) if (predicate(item)) return true;
#else
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                try
                {
                    int length = array.Length;
                    T* basePtr = (T*)handle.AddrOfPinnedObject();
                    for (int i = 0; i < length; i++) if (predicate(basePtr[i])) return true;
                }
                finally { handle.Free(); }
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
        public unsafe int MxCount<T>(Func<T, bool> predicate)
        {
            if (array is T[] flat) return flat.Count(predicate);

            int count = 0;
            if (typeof(T).IsValueType)
            {
#if NET6_0_OR_GREATER
                foreach (T item in (ValueArray<T>)array) if (predicate(item)) count++;
#else
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                try
                {
                    int length = array.Length;
                    T* basePtr = (T*)handle.AddrOfPinnedObject();
                    for (int i = 0; i < length; i++) if (predicate(basePtr[i])) count++;
                }
                finally { handle.Free(); }
#endif
            }

            else foreach (T item in array) if (predicate(item)) count++;

            return count;
        }

        #pragma warning restore CS8500
        #endregion
    }
}
