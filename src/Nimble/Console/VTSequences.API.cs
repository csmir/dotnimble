using System.Text;
using Nimble.Drawing;

namespace Nimble.Console;

public static partial class VTSequences
{
    public static partial class Text
    {
        public static partial class Formatting
        {
            // Console Color -> VT map
            private static readonly int[] _ccTranslationTable = [ 30, 34, 32, 36, 31, 35, 33, 37, 90, 94, 92, 96, 91, 95, 93, 97 ];

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

                return $"\e[{_ccTranslationTable[(int)color] + (asBackground ? 10 : 0)}m";
            }

            /// <summary>
            ///     Converts the specified <see cref="Composite"/> value to the equivalent virtual sequence.
            /// </summary>
            /// <param name="composite"> The <see cref="Composite"/> value to convert. </param>
            /// <param name="asBackground"> Whether to convert to the background-applied sequence. </param>
            public static string FromComposite(Composite composite, bool asBackground)
            {
                return string.Format(asBackground ? BackgroundExtendedRGB : ForegroundExtendedRGB, composite.R, composite.G, composite.B);
            }
        }
    }

#if NET8_0_OR_GREATER

    /// <summary>
    ///     Contains <see cref="CompositeFormat"/> instances for efficient sequence creation.
    /// </summary>
    public static partial class Buffer
    {
        /// <summary> Formatter for <see cref="SetScrollingRegion"/> </summary>
        public static readonly CompositeFormat _SetScrollingRegion = CompositeFormat.Parse(SetScrollingRegion);
    }

    public static partial class Cursor
    {
        public static partial class Positioning
        {
            /// <summary> Formatter for <see cref="CursorUp"/>. </summary>
            public static readonly CompositeFormat _CursorUp = CompositeFormat.Parse(CursorUp);

            /// <summary> Formatter for <see cref="CursorDown"/>. </summary>
            public static readonly CompositeFormat _CursorDown = CompositeFormat.Parse(CursorDown);

            /// <summary> Formatter for <see cref="CursorForward"/>. </summary>
            public static readonly CompositeFormat _CursorForward = CompositeFormat.Parse(CursorForward);

            /// <summary> Formatter for <see cref="CursorBackward"/>. </summary>
            public static readonly CompositeFormat _CursorBackward = CompositeFormat.Parse(CursorBackward);

            /// <summary> Formatter for <see cref="CursorNextLine"/>. </summary>
            public static readonly CompositeFormat _CursorNextLine = CompositeFormat.Parse(CursorNextLine);

            /// <summary> Formatter for <see cref="CursorPreviousLine"/>. </summary>
            public static readonly CompositeFormat _CursorPreviousLine = CompositeFormat.Parse(CursorPreviousLine);

            /// <summary> Formatter for <see cref="CursorHorizontalAbsolute"/>. </summary>
            public static readonly CompositeFormat _CursorHorizontalAbsolute = CompositeFormat.Parse(CursorHorizontalAbsolute);

            /// <summary> Formatter for <see cref="VerticalPositionAbsolute"/>. </summary>
            public static readonly CompositeFormat _VerticalPositionAbsolute = CompositeFormat.Parse(VerticalPositionAbsolute);

            /// <summary> Formatter for <see cref="CursorPosition"/>. </summary>
            public static readonly CompositeFormat _CursorPosition = CompositeFormat.Parse(CursorPosition);

            /// <summary> Formatter for <see cref="HorizontalVerticalPosition"/>.  </summary>
            public static readonly CompositeFormat _HorizontalVerticalPosition = CompositeFormat.Parse(HorizontalVerticalPosition);
        }
    }

    public static partial class Tabs
    {

        /// <summary> Formatter for <see cref="CursorHorizontalTab"/>. </summary>
        public static readonly CompositeFormat _CursorHorizontalTab = CompositeFormat.Parse(CursorHorizontalTab);
        
        /// <summary> Formatter for <see cref="CursorBackwardsTab"/>. </summary>
        public static readonly CompositeFormat _CursorBackwardsTab = CompositeFormat.Parse(CursorBackwardsTab);
    }

    public static partial class Text
    {
        public static partial class Formatting
        {
            
            /// <summary> Formatter for <see cref="SetGraphicsRendition"/>. </summary>
            public static readonly CompositeFormat _SetGraphicsRendition = CompositeFormat.Parse(SetGraphicsRendition);
            
            /// <summary> Formatter for <see cref="ForegroundExtendedRGB"/>. </summary>
            public static readonly CompositeFormat _ForegroundExtendedRGB = CompositeFormat.Parse(ForegroundExtendedRGB);
            
            /// <summary> Formatter for <see cref="ForegroundExtendedPalette"/>. </summary>
            public static readonly CompositeFormat _ForegroundExtendedPalette = CompositeFormat.Parse(ForegroundExtendedPalette);
            
            /// <summary> Formatter for <see cref="BackgroundExtendedRGB"/>. </summary>
            public static readonly CompositeFormat _BackgroundExtendedRGB = CompositeFormat.Parse(BackgroundExtendedRGB);
            
            /// <summary> Formatter for <see cref="BackgroundExtendedPalette"/>. </summary>
            public static readonly CompositeFormat _BackgroundExtendedPalette = CompositeFormat.Parse(BackgroundExtendedPalette);
            
            /// <summary> Formatter for <see cref="ModifyScreenColor"/>. </summary>
            public static readonly CompositeFormat _ModifyScreenColor = CompositeFormat.Parse(ModifyScreenColor);
        }

        public static partial class Modification
        {
            
            /// <summary> Formatter for <see cref="InsertCharacter"/>. </summary>
            public static readonly CompositeFormat _InsertCharacter = CompositeFormat.Parse(InsertCharacter);
            
            /// <summary> Formatter for <see cref="DeleteCharacter"/>. </summary>
            public static readonly CompositeFormat _DeleteCharacter = CompositeFormat.Parse(DeleteCharacter);
            
            /// <summary> Formatter for <see cref="EraseCharacter"/>. </summary>
            public static readonly CompositeFormat _EraseCharacter = CompositeFormat.Parse(EraseCharacter);
            
            /// <summary> Formatter for <see cref="InsertLine"/>. </summary>
            public static readonly CompositeFormat _InsertLine = CompositeFormat.Parse(InsertLine);
            
            /// <summary> Formatter for <see cref="DeleteLine"/>. </summary>
            public static readonly CompositeFormat _DeleteLine = CompositeFormat.Parse(DeleteLine);
            
            /// <summary> Formatter for <see cref="EraseInDisplay"/>. </summary>
            public static readonly CompositeFormat _EraseInDisplay = CompositeFormat.Parse(EraseInDisplay);
            
            /// <summary> Formatter for <see cref="EraseInLine"/>. </summary>
            public static readonly CompositeFormat _EraseInLine = CompositeFormat.Parse(EraseInLine);
        }
    }

    public static partial class Viewport
    {
        
        /// <summary> Formatter for <see cref="ScrollUp"/>. </summary>
        public static readonly CompositeFormat _ScrollUp = CompositeFormat.Parse(ScrollUp);
        
        /// <summary> Formatter for <see cref="ScrollDown"/>. </summary>
        public static readonly CompositeFormat _ScrollDown = CompositeFormat.Parse(ScrollDown);
    }

    public static partial class Window
    {
        
        /// <summary> Formatter for <see cref="SetWindowAndTabTitle"/>. </summary>
        public static readonly CompositeFormat _SetWindowAndTabTitle = CompositeFormat.Parse(SetWindowAndTabTitle);
        
        /// <summary> Formatter for <see cref="SetTabTitle"/>. </summary>
        public static readonly CompositeFormat _SetTabTitle = CompositeFormat.Parse(SetTabTitle);
        
        /// <summary> Formatter for <see cref="SetWindowTitle"/>. </summary>
        public static readonly CompositeFormat _SetWindowTitle = CompositeFormat.Parse(SetWindowTitle);
    }

#endif
}
