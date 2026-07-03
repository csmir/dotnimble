using System.ComponentModel;

namespace System;

/// <summary>
///     Extension methods for <see cref="Exception"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExceptionExtensions
{
    extension(ArgumentOutOfRangeException ex)
    {
#if !NET6_0_OR_GREATER
        /// <summary>
        ///     Throws when the provided value is less than the specified minimum value.
        /// </summary>
        /// <typeparam name="T">The type of the value to compare.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="minValue">The minimum value to compare against.</param>
        /// <param name="argumentExpression">The name of the argument to include in the exception message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than the minimum value.</exception>
        public static void ThrowIfLessThan<T>(T value, T minValue, string? argumentExpression = null) 
            where T : IComparable<T>
        {
            if (value.CompareTo(minValue) < 0)
                throw new ArgumentOutOfRangeException(argumentExpression, value, $"Argument '{argumentExpression}' must be greater than or equal to {minValue}.");
        }

        /// <summary>
        ///     Throws when the provided value is greater than the specified maximum value.
        /// </summary>
        /// <typeparam name="T">The type of the value to compare.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="maxValue">The maximum value to compare against.</param>
        /// <param name="argumentExpression">The name of the argument to include in the exception message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is greater than the maximum value.</exception>
        public static void ThrowIfGreaterThan<T>(T value, T maxValue, string? argumentExpression = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(maxValue) > 0)
                throw new ArgumentOutOfRangeException(argumentExpression, value, $"Argument '{argumentExpression}' must be less than or equal to {maxValue}.");
        }
#endif
    }

    extension(ArgumentException ex)
    {
        /// <summary>
        ///     Throws when the provided value is <see langword="null"/> or empty. Empty represents a collection or enumerable with an element count of 0.
        /// </summary>
        /// <remarks>
        ///     This function retrieves the enumerator of non-collection enumerables to determine whether the enumerable is empty. If this enumerable can only be evaluated once, this method may cause side effects.
        /// </remarks>
        /// <param name="value">The collection to check on whether it is <see langword="null"/> or has no elements.</param>
        /// <param name="argumentExpression">The argument name to compare against. In newer .NET version this property is acknowledged using codeanalysis attributes.</param>
        /// <exception cref="ArgumentException">Thrown when the provided collection is <see langword="null"/> or has no elements.</exception>
        public static void ThrowIfNullOrEmpty(IEnumerable? value,
#if NET6_0_OR_GREATER
            [CallerArgumentExpression(nameof(value))]
#endif
            string? argumentExpression = null)
        {
            ArgumentNullException.ThrowIfNull(value, argumentExpression!);

            if (value is ICollection collection)
            {
                if (collection.Count == 0)
                    throw new ArgumentException($"Argument '{argumentExpression}' cannot be empty.", argumentExpression);
            }
            else if (value?.GetEnumerator()?.MoveNext() == false)
                throw new ArgumentException($"Argument '{argumentExpression}' cannot be empty.", argumentExpression);
        }

        // Polyfill for .NET versions prior to .NET 6.0
#if !NET6_0_OR_GREATER
        /// <summary>
        ///     Throws when the provided string argument is <see langword="null"/> or empty.
        /// </summary>
        /// <param name="value">The value to check on whether it is <see langword="null"/> or empty.</param>
        /// <param name="argumentExpression">The argument name to compare against. In newer .NET version this property is acknowledged using codeanalysis attributes.</param>
        /// <exception cref="ArgumentException">Thrown when the provided value is <see langword="null"/> or empty.</exception>
        public static void ThrowIfNullOrEmpty(string? value, string? argumentExpression = null)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Argument '{argumentExpression}' cannot be null or empty.", argumentExpression);
        }

        /// <summary>
        ///     Throws when the provided string argument is <see langword="null"/>, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The value to check whether it is <see langword="null"/>, empty or consists only of white-space characters.</param>
        /// <param name="argumentExpression">The argument name to compare against. In newer .NET version this property is acknowledged using codeanalysis attributes.</param>
        /// <exception cref="ArgumentException">Thrown when the provided value is <see langword="null"/>, empty or consists only of white-space characters.</exception>
        public static void ThrowIfNullOrWhiteSpace(string? value, string? argumentExpression = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Argument '{argumentExpression}' cannot be null, empty, or consist only of white-space characters.", argumentExpression);
        }
#endif
    }

    extension(ArgumentNullException ex)
    {
        // Polyfill for .NET versions prior to .NET 6.0
#if !NET6_0_OR_GREATER
        /// <summary>
        ///     Throws when the provided value is <see langword="null"/>.
        /// </summary>
        /// <param name="value">The value to compare against <see langword="null"/>.</param>
        /// <param name="argumentExpression">The argument name to compare against. In newer .NET version this property is acknowledged using codeanalysis attributes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> equals <see langword="null"/>.</exception>
        public static void ThrowIfNull(object? value, string? argumentExpression = null)
        {
            if (value == null)
                throw new ArgumentNullException(argumentExpression, $"Argument '{argumentExpression}' cannot be null.");
        }
#endif
    }

    extension(ArgumentOutOfRangeException ex)
    {
        /// <summary>
        ///     Throws when the provided value is less than the specified minimum value or greater than the specified maximum value.
        /// </summary>
        /// <param name="value">The value to compare against <paramref name="minValue"/> and <paramref name="maxValue"/>.</param>
        /// <param name="minValue">The exclusive minimum value to compare against.</param>
        /// <param name="maxValue">The exclusive maximum value to compare against.</param>
        /// <param name="argumentExpression">The argument name to compare against. In newer .NET version this property is acknowledged using codeanalysis attributes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided value is equal to/more than the maximum value, or equal to/less than the minimum value.</exception>
        public static void ThrowIfOutOfRange<T>(T value, T minValue, T maxValue,
#if NET6_0_OR_GREATER
            [CallerArgumentExpression(nameof(value))]
#endif
            string? argumentExpression = null)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(minValue) < 0 || value.CompareTo(maxValue) > 0)
                throw new ArgumentOutOfRangeException(argumentExpression, value, $"Argument '{argumentExpression}' must be between {minValue} and {maxValue}.");
        }

        // Polyfill for .NET versions prior to .NET 6.0
#if !NET6_0_OR_GREATER

        /// <summary>
        ///     Throws when the provided value is less than zero.
        /// </summary>
        /// <param name="value">The value to compare against zero.</param>
        /// <param name="argumentExpression">The argument name to compare against. In newer .NET version this property is acknowledged using codeanalysis attributes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided value is less than zero.</exception>
        public static void ThrowIfNegative<T>(T value, string? argumentExpression = null)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default) < 0)
                throw new ArgumentOutOfRangeException(argumentExpression, value, $"Argument '{argumentExpression}' cannot be negative.");
        }
#endif
    }
}
