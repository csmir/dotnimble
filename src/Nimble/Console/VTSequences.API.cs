namespace Nimble.Console;

public static partial class VTSequences
{
    public static partial class Text
    {
        public static partial class Formatting
        {
            private static readonly int[] _translationTable = [ 30, 34, 32, 36, 31, 35, 33, 37, 90, 94, 92, 96, 91, 95, 93, 97 ];

            /// <summary>
            ///     Converts the specified <see cref="ConsoleColor"/> value to the equivalent virtual sequence.
            /// </summary>
            /// <param name="color"> The <see cref="ConsoleColor"/> value to convert. </param>
            /// <param name="asBackground"> Whether to convert to the background-applied sequence. </param>
            /// <exception cref="ArgumentOutOfRangeException"/>
            public static string FromConsoleColor(ConsoleColor color, bool asBackground)
            {
                if (color < ConsoleColor.Black || color > ConsoleColor.White)
                    throw new ArgumentOutOfRangeException(nameof(color), $"Expected a valid ConsoleColor value, actual value was '{color}'.");

                return $"\e[{_translationTable[(int)color] + (asBackground ? 10 : 0)}m";
            }
        }
    }
}
