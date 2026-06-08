using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nimble.Extensions.Logging.Console;
using System.ComponentModel;

namespace Nimble.Extensions.Logging;

/// <summary>
///     A static class containing extension methods for the <see cref="PrettierFormatter"/> and related types.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class FormatterExtensions
{
    public static PrettierFormatterOptions ResetColors(this PrettierFormatterOptions options)
    {
        options.LogLevelColors[LogLevel.Trace] = ConsoleColor.Gray;
        options.LogLevelColors[LogLevel.Debug] = ConsoleColor.Cyan;
        options.LogLevelColors[LogLevel.Information] = ConsoleColor.Green;
        options.LogLevelColors[LogLevel.Warning] = ConsoleColor.Yellow;
        options.LogLevelColors[LogLevel.Error] = ConsoleColor.Red;
        options.LogLevelColors[LogLevel.Critical] = ConsoleColor.Magenta;

        return options;
    }

    public static PrettierFormatterOptions SetColor(this PrettierFormatterOptions options, LogLevel level, ConsoleColor color)
    {
        if (level < LogLevel.Trace || level > LogLevel.Critical)
            throw new ArgumentOutOfRangeException(nameof(level), "LogLevel must be between Trace and Critical.");

        if (color < ConsoleColor.Black || color > ConsoleColor.White)
            throw new ArgumentOutOfRangeException(nameof(color), "ConsoleColor must be a valid console color.");

        options.LogLevelColors[level] = color;

        return options;
    }

    public static ILoggingBuilder AddPrettierConsole(this ILoggingBuilder builder)
        => AddPrettierConsole(builder, configure: (_) => { });

    public static ILoggingBuilder AddPrettierConsole(this ILoggingBuilder builder, Action<PrettierFormatterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.AddConsoleFormatter<PrettierFormatter, PrettierFormatterOptions>();
        builder.AddConsole((options) => options.FormatterName = nameof(PrettierFormatter));
        builder.Services.Configure(configure);

        return builder;
    }
}
