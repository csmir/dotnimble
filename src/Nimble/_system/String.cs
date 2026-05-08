using System.ComponentModel;
using System.Text;

namespace System;

/// <summary>
///     Provides extensions for the <see cref="string"/> primitive type.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringExtensions
{
    extension(string str)
    {
        /// <summary>
        ///     Reduces the string to a specified length, optionally killing at whitespace and adding a finalizer to the reduced string.
        /// </summary>
        /// <param name="maxLength">The maximum allowed length that the string can be.</param>
        /// <param name="killAtWhitespace">Sets whether the reduced string should kill at last encounter of whitespace, cutting off any trailing words that were likely cut off in the process.</param>
        /// <param name="finalizer">The finalizer to write to the end of the string. If null or empty, it is skipped. The finalizer will not exceed the maximum length of the string.</param>
        /// <returns>A copy of this string which has been reduced to be equal to or less than maximum length based on the provided settings.</returns>
        public string Reduce(int maxLength, bool killAtWhitespace = false, string finalizer = "...")
        {
            ArgumentNullException.ThrowIfNull(str, nameof(str));

            if (str.Length > maxLength)
            {
                maxLength -= finalizer.Length + 1; // reduce the length of the finalizer + a single integer to convert to valid range.

                ArgumentOutOfRangeException.ThrowIfNegative(maxLength, nameof(maxLength));

                if (killAtWhitespace)
                {
                    var range = str.Split(' ');

                    for (int i = 2; str.Length + finalizer.Length > maxLength; i++) // set i as 2, 1 for index reduction, 1 for initial word removal, then increment.
#if NET6_0_OR_GREATER
                        str = string.Join(' ', range[..(range.Length - i)]);
#else
                        str = string.Join(" ", range.Skip(range.Length - i));
#endif
                    str += finalizer;
                }

#if NET6_0_OR_GREATER
                return str[..maxLength] + finalizer;
#else
                return str.Substring(0, maxLength) + finalizer;
#endif
            }

            return str;
        }

        /// <summary>
        ///     Checks whether the string is numeric (contains only digit characters).
        /// </summary>
        /// <returns><see langword="true"/> if the current string contains only digits; otherwise <see langword="false"/>.</returns>
        public bool IsNumeric()
        {
            ArgumentException.ThrowIfNullOrEmpty(str, nameof(str));

            foreach (var c in str)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        /// <summary>
        ///     Checks whether the string is alphanumeric (contains only letter and digit characters).
        /// </summary>
        /// <returns><see langword="true"/> if the current string contains only digits or alphabetical characters; otherwise <see langword="false"/>.</returns>
        public bool IsAlpha()
        {
            ArgumentException.ThrowIfNullOrEmpty(str, nameof(str));

            foreach (var c in str)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Encodes the string into a byte array using the specified encoding (<see cref="Encoding.UTF8"/> by default).
        /// </summary>
        /// <param name="encoding">The encoding to apply to the conversion.</param>
        /// <returns>A new byte array containing the encoded string.</returns>
        public byte[] Encode(Encoding? encoding = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(str, nameof(str));

            return (encoding ?? Encoding.UTF8).GetBytes(str);
        }
    }
}
