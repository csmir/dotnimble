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
    /// <summary>
    ///     Resets the log level colors to their default values.
    /// </summary>
    /// <param name="options">The <see cref="PrettierFormatterOptions"/> instance to reset.</param>
    /// <returns>The <see cref="PrettierFormatterOptions"/> instance with reset colors.</returns>
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

    /// <summary>
    ///     Sets the color for a specific log level.
    /// </summary>
    /// <param name="options">The <see cref="PrettierFormatterOptions"/> instance to modify.</param>
    /// <param name="level">The log level to set the color for.</param>
    /// <param name="color">The color to set for the specified log level.</param>
    /// <returns>The <see cref="PrettierFormatterOptions"/> instance with the updated color.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static PrettierFormatterOptions SetColor(this PrettierFormatterOptions options, LogLevel level, ConsoleColor color)
    {
        if (level < LogLevel.Trace || level > LogLevel.Critical)
            throw new ArgumentOutOfRangeException(nameof(level), "LogLevel must be between Trace and Critical.");

        if (color < ConsoleColor.Black || color > ConsoleColor.White)
            throw new ArgumentOutOfRangeException(nameof(color), "ConsoleColor must be a valid console color.");

        options.LogLevelColors[level] = color;

        return options;
    }

    /// <summary>
    ///     Adds the <see cref="PrettierFormatter"/> to the logging builder with default options.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> instance to add the formatter to.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> instance with the formatter added.</returns>
    public static ILoggingBuilder AddPrettierConsole(this ILoggingBuilder builder)
        => AddPrettierConsole(builder, configure: (_) => { });

    /// <summary>
    ///     Adds the <see cref="PrettierFormatter"/> to the logging builder with custom options.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> instance to add the formatter to.</param>
    /// <param name="configure">An action to configure the <see cref="PrettierFormatterOptions"/>.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> instance with the formatter added.</returns>
    public static ILoggingBuilder AddPrettierConsole(this ILoggingBuilder builder, Action<PrettierFormatterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.AddConsoleFormatter<PrettierFormatter, PrettierFormatterOptions>();
        builder.AddConsole((options) => options.FormatterName = nameof(PrettierFormatter));
        builder.Services.AddSingleton<WrittenLogTracker>();
        builder.Services.Configure(configure);

        return builder;
    }

    /// <summary>
    ///     Adds a console listener to the logging builder, allowing for reading console input and triggering an action when input is completed.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> instance to add the console listener to.</param>
    /// <param name="configure">An action to configure the <see cref="ConsoleListenerOptions"/>.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> instance with the console listener added.</returns>
    public static ILoggingBuilder AddConsoleListener(this ILoggingBuilder builder, Action<ConsoleListenerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddOptionsWithValidateOnStart<ConsoleListenerOptions>()
            .Configure(configure);
        builder.Services.AddHostedService<ConsoleListener>();

        return builder;
    }
}
