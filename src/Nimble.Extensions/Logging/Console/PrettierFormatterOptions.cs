using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Nimble.Extensions.Logging.Console;

/// <summary>
///     Represents the options for configuring the behavior of the <see cref="PrettierFormatter"/>.
/// </summary>
public class PrettierFormatterOptions : ConsoleFormatterOptions
{
    /// <inheritdoc cref="ConsoleFormatterOptions.IncludeScopes" />
    /// <remarks>
    ///     <b>NOTE:</b> This property is not supported in prettier console logging.
    /// </remarks>
    [Obsolete("This property is not supported in prettier console logging.")]
    public new bool IncludeScopes { get; } = false;

    /// <summary>
    ///     Gets or sets whether the logger should consider categories starting with this value as special case. If this property is set, the category will be compared against its value using <see cref="string.StartsWith(string)"/>.
    ///     When it matches, the category will be highlighted with a distinct color defined in <see cref="SpecialCategoryColor"/>.
    /// </summary>
    public string? SpecialCategoryPrefix { get; set; }

    /// <summary>
    ///     Gets or sets a key value collection of log levels correlating to different colors.
    /// </summary>
    /// <remarks>
    ///     Default:
    ///     <list type="bullet">
    ///         <item><see cref="LogLevel.Trace"/>: <see cref="ConsoleColor.DarkGray"/></item>
    ///         <item><see cref="LogLevel.Debug"/>: <see cref="ConsoleColor.DarkGray"/></item>
    ///         <item><see cref="LogLevel.Information"/>: <see cref="ConsoleColor.Green"/></item>
    ///         <item><see cref="LogLevel.Warning"/>: <see cref="ConsoleColor.Yellow"/></item>
    ///         <item><see cref="LogLevel.Error"/>: <see cref="ConsoleColor.DarkRed"/></item>
    ///         <item><see cref="LogLevel.Critical"/>: <see cref="ConsoleColor.DarkMagenta"/></item>
    ///     </list>
    /// </remarks>
    public Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.DarkGray,
        [LogLevel.Debug] = ConsoleColor.DarkGray,
        [LogLevel.Information] = ConsoleColor.Green,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.DarkRed,
        [LogLevel.Critical] = ConsoleColor.DarkMagenta
    };

    /// <summary>
    ///     Gets or sets the color of the timestamp in the console.
    /// </summary>
    /// <remarks>
    ///     Default: <see cref="ConsoleColor.DarkCyan"/>.
    /// </remarks>
    public ConsoleColor TimestampColor { get; set; } = ConsoleColor.DarkCyan;

    /// <summary>
    ///     Gets or sets the color of the remarked category matching <see cref="SpecialCategoryPrefix"/>.
    /// </summary>
    /// <remarks>
    ///     Default: <see cref="ConsoleColor.DarkYellow"/>.
    /// </remarks>
    public ConsoleColor SpecialCategoryColor { get; set; } = ConsoleColor.DarkYellow;

    // Gets or sets an internal flag that indicates whether the console listener is enabled.
    internal bool ConsoleListenerEnabled { get; set; }
}
