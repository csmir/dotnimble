using System.Runtime.Versioning;

namespace Nimble.Console;

/// <summary>
///     Provides Virtual Terminal sequences as defined by the VT100 standard.
/// </summary>
public static class VTSequences
{
    /// <summary>
    ///     Commands in this section are responsible for the console's buffer, and modifying its attributes.
    /// </summary>
    public static class Buffer
    {
        /// <summary>
        ///     <c>DECSTBM</c> - Sets the VT scrolling margins of the viewport.
        /// </summary>
        /// <remarks>
        ///     The scrolling region is the subset of rows adjusted when the screen would otherwise scroll, for example, on <c>'\n'</c> or <see cref="Cursor.Positioning.ReverseIndex"/>.
        ///     Margins can be especially useful for having a portions of the screen that don't scroll when the rest of the screen is filled, such as title or status bars.
        ///     These margins also affect the rows modified by the following commands:
        ///     <list type="bullet">
        ///         <item>
        ///             <see cref="Text.Modification.InsertLine"/>
        ///         </item>
        ///         <item>
        ///             <see cref="Text.Modification.DeleteLine"/>
        ///         </item>
        ///         <item>
        ///             <see cref="Viewport.ScrollUp"/>
        ///         </item>
        ///         <item>
        ///             <see cref="Viewport.ScrollDown"/>
        ///         </item>
        ///     </list>
        ///     For <see cref="SetScrollingRegion"/> there are two optional parameters, which specify the rows that represent the top and bottom lines of the scroll region, inclusive.
        ///     If the parameters are omitted, <c>{0}</c> defaults to 1, and <c>{1}</c> defaults to the current viewport height.
        ///     Scrolling margins are per-buffer, so importantly, the main and alternate buffers maintain separate scrolling regions.
        /// </remarks>
        public const string SetScrollingRegion = "\e[{0};{1}r";

        /// <summary>
        ///     Switches to a new alternate screen buffer.
        /// </summary>
        /// <remarks>
        ///     *nix style applications often utilize an alternate screen buffer, so that they can modify the entire contents of the buffer, without affecting the application that started them.
        ///     The alternate buffer is exactly the dimensions of the window, without any scrollback region.
        /// </remarks>
        public const string UseAlternateBuffer = "\e[?1049h";

        /// <summary>
        ///     Switches to the main screen buffer.
        /// </summary>
        /// <remarks>
        ///     *nix style applications often utilize an alternate screen buffer, so that they can modify the entire contents of the buffer, without affecting the application that started them.
        ///     The alternate buffer is exactly the dimensions of the window, without any scrollback region.
        /// </remarks>
        public const string UseMainScreenBuffer = "\e[?1049l";

        /// <summary>
        ///     <c>DECCOLM</c> - Sets the buffer width to 132 columns wide.
        /// </summary>
        public const string SetBufferWidth132 = "\e[?3h";

        /// <summary>
        ///     <c>DECCOLM</c> - Sets the buffer width to 80 columns wide.
        /// </summary>
        public const string SetBufferWidth80 = "\e[?3l";
    }

    /// <summary>
    ///     Commands in this section are responsible for the console's cursor, and its various operations.
    /// </summary>
    public static class Cursor
    {
        /// <summary>
        ///     Contains commands for controlling the cursor's position, within the current viewport.
        /// </summary>
        /// <remarks>
        ///     For all parameters, the following rules apply unless otherwise noted:
        ///     <list type="bullet">
        ///         <item>
        ///             <c>{0}</c> represents the distance to move the cursor, and is an optional parameter.
        ///         </item>
        ///         <item>
        ///             <c>{0}</c> will be treated as a 1, if it is omitted or equals 0.
        ///         </item>
        ///         <item>
        ///             <c>{0}</c> cannot be larger than <see cref="short.MaxValue"/>.
        ///         </item>
        ///         <item>
        ///             <c>{0}</c> cannot be negative.
        ///         </item>
        ///     </list>
        ///     Cursor movement is bounded by the buffer's current viewport. Scrolling (if available) will not occur.
        /// </remarks>
        public static class Positioning
        {
            /// <summary>
            ///     <c>DECXCPR</c> - Emits the cursor position as <c>"\e[{0};{1}R"</c> Where parameter <c>{0}</c> is the current row, and <c>{1}</c> is the current column.
            /// </summary>
            /// <remarks>
            ///     As a query, this command will emit its response directly into the console's input stream immediately upon recognition in the output stream.
            ///     Note however, that the flag responsible for respecting VT input does not apply, as query commands assume that the caller will always want the reply.
            /// </remarks>
            public const string ReportCursorPosition = "\e[6n";

            /// <summary>
            ///     <c>RI</c> – Performs the reverse operation of <c>\n</c>, moves cursor up one line, maintains horizontal position, scrolls buffer if necessary.
            /// </summary>
            /// <remarks>
            ///     If there are scroll margins set, RI inside the margins will scroll only the contents of the margins, and leave the viewport unchanged.
            /// </remarks>
            public const string ReverseIndex = "\eM";

            /// <summary>
            ///     <c>DECSC</c> - Saves the cursor's position in memory, to be restored via <see cref="RestoreCursor"/>.
            /// </summary>
            /// <remarks>
            ///     There will be no value saved in memory until the first use of the save command. The only way to access the saved value is with the restore command.
            /// </remarks>
            public const string SaveCursor = "\e7";

            /// <summary>
            ///     <c>DECSR</c> - Restores the cursor's position from memory (but does not clear the saved position).
            /// </summary>
            /// <remarks>
            ///     There will be no value saved in memory until the first use of the save command. The only way to access the saved value is with the restore command.
            /// </remarks>
            public const string RestoreCursor = "\e8";

            /// <summary>
            ///     <c>CUU</c> - Moves the cursor <c>{0}</c> rows up.
            /// </summary>
            public const string CursorUp = "\e[{0}A";

            /// <summary>
            ///     <c>CUD</c> - Moves the cursor <c>{0}</c> rows down.
            /// </summary>
            public const string CursorDown = "\e[{0}B";

            /// <summary>
            ///     <c>CUF</c> - Moves the cursor <c>{0}</c> columns forward.
            /// </summary>
            public const string CursorForward = "\e[{0}C";

            /// <summary>
            ///     <c>CUB</c> - Moves the cursor <c>{0}</c> columns backward.
            /// </summary>
            public const string CursorBackward = "\e[{0}D";

            /// <summary>
            ///     <c>CNL</c> - Moves the cursor <c>{0}</c> lines down from its current position.
            /// </summary>
            public const string CursorNextLine = "\e[{0}E";

            /// <summary>
            ///     <c>CPL</c> - Moves the cursor <c>{0}</c> lines up from its current position.
            /// </summary>
            public const string CursorPreviousLine = "\e[{0}F";

            /// <summary>
            ///     <c>CHA</c> - Moves the cursor to the <c>{0}</c>th position in the current line.
            /// </summary>
            public const string CursorHorizontalAbsolute = "\e[{0}G";

            /// <summary>
            ///     <c>VPA</c> - Moves the cursor to the <c>{0}</c>th row of the current column.
            /// </summary>
            public const string VerticalPositionAbsolute = "\e[{0}d";

            /// <summary>
            ///     <c>CUP</c> - Moves the cursor to coordinates (<c>{0}</c>, <c>{1}</c>) within the viewport.
            /// </summary>
            /// <remarks>
            ///     <c>{0}</c> is treated as the target column, and <c>{1}</c> as the target row.
            /// </remarks>
            public const string CursorPosition = "\e[{0};{1}H";

            /// <summary>
            ///     <c>HVP</c> - Moves the cursor to coordinates (<c>{0}</c>, <c>{1}</c>) within the viewport.
            /// </summary>
            /// <remarks>
            ///     <c>{0}</c> is treated as the target column, and <c>{1}</c> as the target row.
            /// </remarks>
            public const string HorizontalVerticalPosition = "\e[{0};{1}f";

            /// <summary>
            ///     <c>ANSISYSSC</c> - Ansi.sys emulation. With no parameters, performs a save cursor operation like <see cref="SaveCursor"/>.
            /// </summary>
            [Obsolete("ANSI.sys historical documentation can be found at https://msdn.microsoft.com/library/cc722862.aspx and is implemented for convenience/compatibility.")]
    #if NET6_0_OR_GREATER
            [SupportedOSPlatform("windows")]
    #endif
            public const string AnsiSaveCursor = "\e[s";

            /// <summary>
            ///     <c>ANSISYSRC</c> - Ansi.sys emulation. With no parameters, performs a restore cursor operation like <see cref="RestoreCursor"/>.
            /// </summary>
            [Obsolete("ANSI.sys historical documentation can be found at https://msdn.microsoft.com/library/cc722862.aspx and is implemented for convenience/compatibility.")]
    #if NET6_0_OR_GREATER
            [SupportedOSPlatform("windows")]
    #endif
            public const string AnsiRestoreCursor = "\e[u";
        }

        /// <summary>
        ///     Contains commands for controlling the cursor's visibility, and its blinking state.
        /// </summary>
        /// <remarks>
        ///     Enable sequences end in <c>h</c>, and the disable in <c>l</c>, representing high and low respectively.
        /// </remarks>
        public static class Visibility
        {
            /// <summary>
            ///     <c>ATT160</c> - Start blinking the cursor.
            /// </summary>
            public const string EnableBlinking = "\e[?12h";

            /// <summary>
            ///     <c>ATT160</c> - Stop blinking the cursor.
            /// </summary>
            public const string DisableBlinking = "\e[?12l";

            /// <summary>
            ///     <c>DECTCEM</c> - Show the cursor. Equivalent to setting <see cref="System.Console.CursorVisible"/> to <see langword="true"/>.
            /// </summary>
            public const string SetModeShow = "\e[?25h";

            /// <summary>
            ///     <c>DECTCEM</c> - Hide the cursor. Equivalent to setting <see cref="System.Console.CursorVisible"/> to <see langword="false"/>.
            /// </summary>
            public const string SetModeHide = "\e[?12l";
        }

        /// <summary>
        ///     Contains commands for controlling the cursor's shape.
        /// </summary>
        public static class Shape
        {
            /// <summary>
            ///     <c>DECSCUSR</c> - Default cursor shape configured by the user.
            /// </summary>
            public const string UserShape = "\e[0 q";

            /// <summary>
            ///     <c>DECSCUSR</c> - Sets the cursor shape to a block, with blinking enabled.
            /// </summary>
            public const string BlinkingBlock = "\e[1 q";

            /// <summary>
            ///     <c>DECSCUSR</c> - Sets the cursor shape to a block, with blinking disabled.
            /// </summary>
            public const string SteadyBlock = "\e[2 q";

            /// <summary>
            ///     <c>DECSCUSR</c> - Sets the cursor shape to an underline, with blinking enabled.
            /// </summary>
            public const string BlinkingUnderline = "\e[3 q";

            /// <summary>
            ///     <c>DECSCUSR</c> - Sets the cursor shape to an underline, with blinking disabled.
            /// </summary>
            public const string SteadyUnderline = "\e[4 q";

            /// <summary>
            ///     <c>DECSCUSR</c> - Sets the cursor shape to a bar, with blinking enabled.
            /// </summary>
            public const string BlinkingBar = "\e[5 q";

            /// <summary>
            ///     <c>DECSCUSR</c> - Sets the cursor shape to a bar, with blinking disabled.
            /// </summary>
            public const string SteadyBar = "\e[6 q";

        }
    }

    /// <summary>
    ///     Commands in this section are responsible for the console's behaviour, when receiving keyboard input.
    /// </summary>
    /// <remarks>
    ///     Each of these modes are simple boolean settings. The Cursor keys' mode is either Normal (default) or Application, and the Keypad keys' mode is either Numeric (default) or Application.
    /// </remarks>
    public static class Input
    {
        /// <summary>
        ///     <c>DECKPAM</c> - Keypad keys will emit their application mode sequences.
        /// </summary>
        /// <remarks>
        ///     The keypad keys mode primarily controls the sequences emitted by the numpad, as well as the function keys.
        /// </remarks>
        public const string SetKeypadApplication = "\e=";

        /// <summary>
        ///     <c>DECKPNM</c> - Keypad keys will emit their numeric mode sequences.
        /// </summary>
        /// <remarks>
        ///     The keypad keys mode controls the sequences emitted by the numpad, as well as the function keys.
        /// </remarks>
        public const string SetKeypadNumeric = "\e>";

        /// <summary>
        ///     <c>DECCKM</c> - Cursor keys will emit their application mode sequences.
        /// </summary>
        /// <remarks>
        ///     The cursor keys mode controls the sequences that are emitted by the arrow keys, as well as 'Home' and 'End'.
        /// </remarks>
        public const string SetCursorKeysApplication = "\e[?1h";

        /// <summary>
        ///     <c>DECCKM</c> - Cursor keys will emit their normal mode sequences.
        /// </summary>
        /// <remarks>
        ///     The cursor keys mode controls the sequences that are emitted by the arrow keys, as well as 'Home' and 'End'.
        /// </remarks>
        public const string SetCursorKeysDefault = "\e[?1l";
    }

    /// <summary>
    ///     Commands in this section are responsible for the console's tab stops.
    /// </summary>
    /// <remarks>
    ///     While legacy consoles traditionally expect eight-wide tabs, *nix applications can manipulate tab stops' locations, to optimize cursor movement.
    ///     The sequences contained allow an application to set, remove, and navigate the tab stop locations within the console windowhem.
    /// </remarks>
    public static class Tabs
    {
        /// <summary>
        ///     <c>HTS</c> - Sets a tab stop on the cursor's current column.
        /// </summary>
        /// <remarks>
        ///     <b>Side Effect:</b> Using <see cref="HorizontalTabSet"/> will cause the console to treat TAB characters similarly to <see cref="CursorHorizontalTab"/>.
        /// </remarks>
        public const string HorizontalTabSet = "\eH";

        /// <summary>
        ///     <c>CHT</c> - Advances the cursor forward by <c>{0}</c> tab stops.
        /// </summary>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item>
        ///             If parameter <c>{0}</c> is unspecified, the cursor will advance one tab stop.
        ///         </item>
        ///         <item>
        ///             If there are no tab stops set via <see cref="HorizontalTabSet"/>, the first and last columns will be treated as the only two tab stops.
        ///         </item>
        ///         <item>
        ///             If there are no more tab stops, the cursor will advance to the last column in the current row.
        ///         </item>
        ///         <item>
        ///             If the cursor is at the end of a line, it will advance to the first column of the next row.
        ///         </item>
        ///     </list>
        /// </remarks>
        public const string CursorHorizontalTab = "\e[{0}I";

        /// <summary>
        ///    <c>CBT</c> - Moves the cursor backward by <c>{0}</c> tab stops.
        /// </summary>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item>
        ///             If parameter <c>{0}</c> is unspecified, the cursor will advance one tab stop.
        ///         </item>
        ///         <item>
        ///             If there are no tab stops set via <see cref="HorizontalTabSet"/>, the first and last columns will be treated as the only two tab stops.
        ///         </item>
        ///         <item>
        ///             If there are no more tab stops, moves the cursor to the first column in the current row.
        ///         </item>
        ///         <item>
        ///             If the cursor is at the start of a line, does nothing.
        ///         </item>
        ///     </list>
        /// </remarks>
        public const string CursorBackwardsTab = "\e[{0}Z";

        /// <summary>
        ///     <c>TBC</c> - Clears the tab stop in the current column, if there is one. Does nothing otherwise.
        /// </summary>
        public const string TabClearCurrent = "\e[0g";

        /// <summary>
        ///     <c>TBC</c> - Clears all currently set tab stops.
        /// </summary>
        public const string TabClearAll = "\e[3g";
    }

    /// <summary>
    /// Commands in this section are responsible for text within the console, and various manipulations of it.
    /// </summary>
    public static class Text
    {
        /// <summary>
        ///     Contains commands for modifying the text buffer's contents.
        /// </summary>
        /// <remarks>
        ///     For each sequence, the default value for <c>{0}</c> if it is omitted is 0.
        /// </remarks>
        public static class Modification
        {
            /// <summary>
            ///     <c>ICH</c> - Inserts <c>{0}</c> spaces at the current cursor position, shifting all existing text to the right. Text exiting the screen to the right is removed.
            /// </summary>
            public const string InsertCharacter = "\e[{0}@";

            /// <summary>
            ///     <c>DCH</c> - Deletes <c>{0}</c> characters at the current cursor position, shifting all existing text to the left. Space characters are inserted from the right edge of the screen.
            /// </summary>
            public const string DeleteCharacter = "\e[{0}P";

            /// <summary>
            ///     <c>ECH</c> - Erases <c>{0}</c> characters from the current cursor position, overwriting them with space characters.
            /// </summary>
            public const string EraseCharacter = "\e[{0}X";

            /// <summary>
            ///     <c>IL</c> - Inserts <c>{0}</c> lines into the text buffer at the cursor's position, shifting down the line the cursor is on, and all lines below it.
            /// </summary>
            /// <remarks>
            ///     Only the lines in the current scrolling margins are affected. The following rules apply unless otherwise noted:
            ///     <list type="bullet">
            ///         <item>
            ///             If no margins are set, the default margin borders are the current viewport.
            ///         </item>
            ///         <item>
            ///             If a line were to be shifted below the margins, it is discarded entirely.
            ///         </item>
            ///         <item>
            ///             If a line is deleted, a blank line is inserted at the bottom of the margins.
            ///         </item>
            ///     </list>
            ///     Lines outside the current margins are never affected.
            /// </remarks>
            public const string InsertLine = "\e[{0}L";

            /// <summary>
            ///     <c>DL</c> - Deletes <c>{0}</c> lines from the text buffer, starting with the row the cursor is on.
            /// </summary>
            /// <remarks>
            ///     Only the lines in the current scrolling margins are affected. The following rules apply unless otherwise noted:
            ///     <list type="bullet">
            ///         <item>
            ///             If no margins are set, the default margin borders are the current viewport.
            ///         </item>
            ///         <item>
            ///             If a line were to be shifted below the margins, it is discarded entirely.
            ///         </item>
            ///         <item>
            ///             If a line is deleted, a blank line is inserted at the bottom of the margins.
            ///         </item>
            ///     </list>
            ///     Lines outside the current margins are never affected.
            /// </remarks>
            public const string DeleteLine = "\e[{0}M";

            /// <summary>
            ///     <c>ED</c> - Replace all text in the current viewport/screen with space characters, within the bounds specified by <c>{0}</c>.
            /// </summary>
            /// <remarks>
            ///     The parameter <c>{0}</c> has 3 valid values:
            ///     <list type="bullet">
            ///         <item>
            ///             <c>0</c> erases from the current cursor position (inclusive), to the end of the line/display.
            ///         </item>
            ///         <item>
            ///             <c>1</c> erases from the beginning of the line/display, up to and including, the current cursor position
            ///         </item>
            ///         <item>
            ///             <c>2</c> erases the entire line/display.
            ///         </item>
            ///     </list>
            /// </remarks>
            public const string EraseInDisplay = "\e[{0}J";

            /// <summary>
            ///     <c>EL</c> - Replace all text on the cursor's current line with with space characters, within the bounds specified by <c>{0}</c>.
            /// </summary>
            /// <remarks>
            ///     The parameter <c>{0}</c> has 3 valid values:
            ///     <list type="bullet">
            ///         <item>
            ///             <c>0</c> erases from the current cursor position (inclusive), to the end of the line/display.
            ///         </item>
            ///         <item>
            ///             <c>1</c> erases from the beginning of the line/display, up to and including, the current cursor position
            ///         </item>
            ///         <item>
            ///             <c>2</c> erases the entire line/display.
            ///         </item>
            ///     </list>
            /// </remarks>
            public const string EraseInLine = "\e[{0}K";
        }

        /// <summary>
        ///     Contains commands for adjusting the format of all future writes to the console's text buffer.
        /// </summary>
        /// <remarks>
        ///     Some virtual terminal emulators support a palette of colors greater than the 16 colors provided by legacy implementations.
        ///     For these extended colors, consoles without support (such as the legacy Windows console) will approximate the nearest color from their table.
        /// </remarks>
        public static class Formatting
        {
            /// <summary>
            ///     <c>SGR</c> - Set the format of the screen and text, as specified by <c>{0}</c>.
            /// </summary>
            /// <remarks>
            ///     This command is special in that the <c>{0}</c> parameter is variable, and can accept 0 to 16 parameters, separated by semicolons. When no parameters are specified, it is treated the same as a single 0 parameter.<br/>
            ///     Formatting modes are applied from left to right. Applying competing formatting options will result in the right-most option taking precedence.<br/>
            ///     For options that specify colors, the colors will be used as defined in the console's current color table, and will continue to use those definitions until changed.
            /// </remarks>
            public const string SetGraphicsRendition = "\e[{0}m";

            /// <summary>
            ///     Returns all attributes to the default state prior to modification.
            /// </summary>
            public const string Default = "\e[0m";

            /// <summary>
            ///     Applies intensity flag to the console's foreground color.
            /// </summary>
            public const string Bright = "\e[1m";

            /// <summary>
            ///     Removes intensity flag from foreground color.
            /// </summary>
            public const string NoBright = "\e[22m";

            /// <summary>
            ///     Adds underline.
            /// </summary>
            public const string Underline = "\e[4m";

            /// <summary>
            ///     Removes underline.
            /// </summary>
            public const string NoUnderline = "\e[24m";

            /// <summary>
            ///     Swaps foreground and background colors.
            /// </summary>
            public const string Negative = "\e[7m";

            /// <summary>
            ///     Returns foreground/background to normal.
            /// </summary>
            public const string Positive = "\e[27m";

            /// <summary>
            ///     Applies black to the console's foreground.
            /// </summary>
            public const string ForegroundBlack = "\e[30m";

            /// <summary>
            ///     Applies red to the console's foreground.
            /// </summary>
            public const string ForegroundRed = "\e[31m";

            /// <summary>
            ///     Applies green to the console's foreground.
            /// </summary>
            public const string ForegroundGreen = "\e[32m";

            /// <summary>
            ///     Applies yellow to the console's foreground.
            /// </summary>
            public const string ForegroundYellow = "\e[33m";

            /// <summary>
            ///     Applies blue to the console's foreground.
            /// </summary>
            public const string ForegroundBlue = "\e[34m";

            /// <summary>
            ///     Applies magenta to the console's foreground.
            /// </summary>
            public const string ForegroundMagenta = "\e[35m";

            /// <summary>
            ///     Applies cyan to the console's foreground.
            /// </summary>
            public const string ForegroundCyan = "\e[36m";

            /// <summary>
            ///     Applies white to the console's foreground.
            /// </summary>
            public const string ForegroundWhite = "\e[37m";

            /// <summary>
            ///     Sets the foreground color to the RGB value specified.
            /// </summary>
            /// <remarks>
            ///     The RGB value consists of three components, each within the range of a <see cref="byte"/>. <c>{0}</c> represents the R component, <c>{1}</c> the G component, and <c>{2}</c> the B component.
            /// </remarks>
            public const string ForegroundExtendedRGB = "\e[38;2;{0};{1};{2}m";

            /// <summary>
            ///     Sets the foreground color to index <c>{0}</c>, in the console's color table.
            /// </summary>
            /// <remarks>
            ///     The color table's entries depends on the terminal in use, typically derived from the xterm 256 color table (88, for memory-limited terminals).
            ///     The full xterm table can be seen here: <see href="https://upload.wikimedia.org/wikipedia/commons/1/15/Xterm_256color_chart.svg"/>.
            /// </remarks>
            public const string ForegroundExtendedPalette = "\e[38;5;{0}m";

            /// <summary>
            ///     Applies only the foreground portion of the defaults.
            /// </summary>
            public const string ForegroundDefault = "\e[39m";

            /// <summary>
            ///     Applies black to the console's background.
            /// </summary>
            public const string BackgroundBlack = "\e[40m";

            /// <summary>
            ///     Applies red to the console's background.
            /// </summary>
            public const string BackgroundRed = "\e[41m";

            /// <summary>
            ///     Applies green to the console's background.
            /// </summary>
            public const string BackgroundGreen = "\e[42m";

            /// <summary>
            ///     Applies yellow to the console's background.
            /// </summary>
            public const string BackgroundYellow = "\e[43m";

            /// <summary>
            ///     Applies blue to the console's background.
            /// </summary>
            public const string BackgroundBlue = "\e[44m";

            /// <summary>
            ///     Applies magenta to the console's background.
            /// </summary>
            public const string BackgroundMagenta = "\e[45m";

            /// <summary>
            ///     Applies cyan to the console's background.
            /// </summary>
            public const string BackgroundCyan = "\e[46m";

            /// <summary>
            ///     Applies white to the console's background.
            /// </summary>
            public const string BackgroundWhite = "\e[47m";

            /// <summary>
            ///     Sets the background color to the RGB value specified.
            /// </summary>
            /// <remarks>
            ///     The RGB value consists of three components, each within the range of a <see cref="byte"/>. <c>{0}</c> represents the R component, <c>{1}</c> the G component, and <c>{2}</c> the B component.
            /// </remarks>
            public const string BackgroundExtendedRGB = "\e[48;2;{0};{1};{2}m";

            /// <summary>
            ///     Sets the background color to index <c>{0}</c>, in the console's color table.
            /// </summary>
            /// <remarks>
            ///     The color table's entries depends on the terminal in use, typically derived from the xterm 256 color table (88, for memory-limited terminals).<br/>
            ///     The full xterm table can be seen here: <see href="https://upload.wikimedia.org/wikipedia/commons/1/15/Xterm_256color_chart.svg"/>.
            /// </remarks>
            public const string BackgroundExtendedPalette = "\e[48;5;{0}m";

            /// <summary>
            ///     Applies only the background portion of the defaults.
            /// </summary>
            public const string BackgroundDefault = "\e[49m";

            /// <summary>
            ///     Applies bold/bright black to the console's foreground.
            /// </summary>
            public const string ForegroundBrightBlack = "\e[90m";

            /// <summary>
            ///     Applies bold/bright red to the console's foreground.
            /// </summary>
            public const string ForegroundBrightRed = "\e[91m";

            /// <summary>
            ///     Applies bold/bright green to the console's foreground.
            /// </summary>
            public const string ForegroundBrightGreen = "\e[92m";

            /// <summary>
            ///     Applies bold/bright yellow to the console's foreground.
            /// </summary>
            public const string ForegroundBrightYellow = "\e[93m";

            /// <summary>
            ///     Applies bold/bright blue to the console's foreground.
            /// </summary>
            public const string ForegroundBrightBlue = "\e[94m";

            /// <summary>
            ///     Applies bold/bright magenta to the console's foreground.
            /// </summary>
            public const string ForegroundBrightMagenta = "\e[95m";

            /// <summary>
            ///     Applies bold/bright cyan to the console's foreground.
            /// </summary>
            public const string ForegroundBrightCyan = "\e[96m";

            /// <summary>
            ///     Applies bold/bright white to the console's foreground.
            /// </summary>
            public const string ForegroundBrightWhite = "\e[97m";

            /// <summary>
            ///     Applies bold/bright black to the console's background.
            /// </summary>
            public const string BackgroundBrightBlack = "\e[100m";

            /// <summary>
            ///     Applies bold/bright red to the console's background.
            /// </summary>
            public const string BackgroundBrightRed = "\e[101m";

            /// <summary>
            ///     Applies bold/bright green to the console's background.
            /// </summary>
            public const string BackgroundBrightGreen = "\e[102m";

            /// <summary>
            ///     Applies bold/bright yellow to the console's background.
            /// </summary>
            public const string BackgroundBrightYellow = "\e[103m";

            /// <summary>
            ///     Applies bold/bright blue to the console's background.
            /// </summary>
            public const string BackgroundBrightBlue = "\e[104m";

            /// <summary>
            ///     Applies bold/bright magenta to the console's background.
            /// </summary>
            public const string BackgroundBrightMagenta = "\e[105m";

            /// <summary>
            ///     Applies bold/bright cyan to the console's background.
            /// </summary>
            public const string BackgroundBrightCyan = "\e[106m";

            /// <summary>
            ///     Applies bold/bright white to the console's background.
            /// </summary>
            public const string BackgroundBrightWhite = "\e[107m";

            /// <summary>
            ///     Sets index <c>{0}</c> in the console's color palette to the RGB value specified.
            /// </summary>
            /// <remarks>
            ///     The RGB value consists of three hexadecimal components, each within the range of a <see cref="byte"/>. <c>{1}</c> represents the R component, <c>{2}</c> the G component, and <c>{3}</c> the B component.<br/><br/>
            ///     Note that this sequence is an OSC sequence, and not a CSI like many of the other sequences listed. As such, it starts with <c>"\e]"</c>, not <c>"\e["</c>.
            ///     Additionally, it must end with the terminator sequence <c>"\e\x5C"</c>. <c>"\x7"</c> may be used instead, but the longer form is preferred.
            /// </remarks>
            public const string ModifyScreenColor = "\e]4;{0};{1}/{2}/{3}\e\x5C";

            /// <summary>
            ///     Converts a <see cref="ConsoleColor"/> to its corresponding ANSI color code sequence. The <paramref name="isBackground"/> parameter controls whether the returned sequence is for the foreground or background color.
            /// </summary>
            /// <param name="color">The <see cref="ConsoleColor"/> to convert.</param>
            /// <param name="isBackground">Indicates whether the color is for the background.</param>
            /// <returns>The ANSI color code sequence.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="color"/> is not a valid <see cref="ConsoleColor"/> value.</exception>
            public static string FromConsoleColor(ConsoleColor color, bool isBackground = false)
            {
                if (color < ConsoleColor.Black || color > ConsoleColor.White)
                    throw new ArgumentOutOfRangeException(nameof(color), $"Invalid console color: {color}");

                if (isBackground)
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
                    return color switch
                    {
                        ConsoleColor.Black => BackgroundBlack,
                        ConsoleColor.DarkRed => BackgroundRed,
                        ConsoleColor.DarkGreen => BackgroundGreen,
                        ConsoleColor.DarkYellow => BackgroundYellow,
                        ConsoleColor.DarkBlue => BackgroundBlue,
                        ConsoleColor.DarkMagenta => BackgroundMagenta,
                        ConsoleColor.DarkCyan => BackgroundCyan,
                        ConsoleColor.Gray => BackgroundWhite,
                        ConsoleColor.DarkGray => BackgroundBrightBlack,
                        ConsoleColor.Red => BackgroundBrightRed,
                        ConsoleColor.Green => BackgroundBrightGreen,
                        ConsoleColor.Yellow => BackgroundBrightYellow,
                        ConsoleColor.Blue => BackgroundBrightBlue,
                        ConsoleColor.Magenta => BackgroundBrightMagenta,
                        ConsoleColor.Cyan => BackgroundBrightCyan,
                        ConsoleColor.White => BackgroundBrightWhite
                    };
                else
                    return color switch
                    {
                        ConsoleColor.Black => ForegroundBlack,
                        ConsoleColor.DarkRed => ForegroundRed,
                        ConsoleColor.DarkGreen => ForegroundGreen,
                        ConsoleColor.DarkYellow => ForegroundYellow,
                        ConsoleColor.DarkBlue => ForegroundBlue,
                        ConsoleColor.DarkMagenta => ForegroundMagenta,
                        ConsoleColor.DarkCyan => ForegroundCyan,
                        ConsoleColor.Gray => ForegroundWhite,
                        ConsoleColor.DarkGray => ForegroundBrightBlack,
                        ConsoleColor.Red => ForegroundBrightRed,
                        ConsoleColor.Green => ForegroundBrightGreen,
                        ConsoleColor.Yellow => ForegroundBrightYellow,
                        ConsoleColor.Blue => ForegroundBrightBlue,
                        ConsoleColor.Magenta => ForegroundBrightMagenta,
                        ConsoleColor.Cyan => ForegroundBrightCyan,
                        ConsoleColor.White => ForegroundBrightWhite
                    };
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            }
        }
    }

    /// <summary>
    ///     Commands in this section are responsible for the console's viewport, and its contents.
    /// </summary>
    /// <remarks>
    ///     The contents are moved starting with the cursor's current line, as such, if the cursor is on the viewport's middle row, then <see cref="ScrollUp"/> would move the bottom half, and insert blank lines at the bottom.
    ///     <see cref="ScrollDown"/> would move the top half, and insert new lines at the top. It is also important to note that <see cref="ScrollUp"/> and <see cref="ScrollDown"/> are also affected by the current scrolling margins.
    ///     Lines outside the scrolling margins will not be affected.<br/><br/>
    ///     For each sequence, the default value for <c>{0}</c> if it is omitted is 1.
    /// </remarks>
    public static class Viewport
    {
        /// <summary>
        ///     <c>SU</c> - Scrolls the console's contents <c>{0}</c> columns up.
        /// </summary>
        /// <remarks>
        ///     Also known as 'pan down'. When moving, new lines will fill in from below.
        /// </remarks>
        public const string ScrollUp = "\e[{0}S";

        /// <summary>
        ///     <c>SD</c> - Scrolls the console's contents <c>{0}</c> columns down.
        /// </summary>
        /// <remarks>
        ///     Also known as 'pan up'. When moving, new lines will fill in from above.
        /// </remarks>
        public const string ScrollDown = "\e[{0}T";
    }

    /// <summary>
    /// Commands in this section are responsible for the console's window, and output controls.
    /// </summary>
    public static class Window
    {
        /// <summary>
        ///     Sets both the console window's title, and its tab information, to the string passed in <c>{0}</c>.
        /// </summary>
        /// <remarks>
        ///     The string must be less than 255 characters to be accepted.
        /// </remarks>
        public const string SetWindowAndTabTitle = "\e]0;{0}\e\x5C";

        /// <summary>
        ///     Sets only the console tab's information to the string passed in <c>{0}</c>, but does not update its window title.
        /// </summary>
        /// <remarks>
        ///     The string must be less than 255 characters to be accepted.
        /// </remarks>
        public const string SetTabTitle = "\e]1;{0}\e\x5C";

        /// <summary>
        ///     Sets only the console window's title to the string passed in <c>{0}</c>, but does not update its tab information.
        /// </summary>
        /// <remarks>
        ///     The string must be less than 255 characters to be accepted.
        /// </remarks>
        public const string SetWindowTitle = "\e]2;{0}\e\x5C";

        /// <summary>
        ///     Sets the console input mode to 'DEC line drawing'. See <see href="https://vt100.net/docs/vt220-rm/chapter2.html#T2-4"/> for a table representing the input mappings.
        /// </summary>
        public const string DesignateCharacterSetLine = "\e(0";

        /// <summary>
        ///     Sets the console input mode to standard ASCII (default).
        /// </summary>
        public const string DesignateCharacterSetASCII = "\e(B";
    }
}
