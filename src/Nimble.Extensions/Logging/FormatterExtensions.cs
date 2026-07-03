using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nimble.Extensions.Logging.Console;
using Nimble.Extensions.Logging.File;
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
        => options.SetColor(LogLevel.Trace, ConsoleColor.Gray)
        .SetColor(LogLevel.Debug, ConsoleColor.Cyan)
        .SetColor(LogLevel.Information, ConsoleColor.Green)
        .SetColor(LogLevel.Warning, ConsoleColor.Yellow)
        .SetColor(LogLevel.Error, ConsoleColor.Red)
        .SetColor(LogLevel.Critical, ConsoleColor.Magenta);

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
        builder.AddConsoleFormatter<PrettierFormatter, PrettierFormatterOptions>()
            .AddConsole(static (options) => options.FormatterName = nameof(PrettierFormatter));
        builder.Services.AddSingleton<WrittenLogTracker>()
            .Configure(configure);
        return builder;
    }

    /// <summary>
    /// Adds a file logging provider to the <see cref="ILoggingBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" /> to add the provider to.</param>
    /// <param name="configureOptions">The action used to configure the logger.</param>
    /// <param name="clearExistingProviders">If the existing providers added to the <see cref="ILoggingBuilder" /> should be removed.</param>
    /// <returns>The <see cref="ILoggingBuilder" /> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configureOptions, bool clearExistingProviders = false)
        => AddFileLoggerProvider(builder, configureOptions, clearExistingProviders);

    /// <summary>
    /// Adds a file logging provider to the <see cref="ILoggingBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder" /> to add the provider to.</param>
    /// <param name="configureOptions">The action used to configure the logger.</param>
    /// <param name="clearExistingProviders">If the existing providers added to the <see cref="ILoggingBuilder" /> should be removed.</param>
    /// <returns>The <see cref="ILoggingBuilder" /> so that additional calls can be chained.</returns>
    public static ILoggingBuilder AddFileLoggerProvider(this ILoggingBuilder builder, Action<FileLoggerOptions> configureOptions, bool clearExistingProviders = false)
    {
        if (clearExistingProviders)
        {
            builder.ClearProviders();
        }

        builder.Services
            .Configure(configureOptions)
            .AddSingleton<ILoggerProvider, FileLoggerProvider>();

        // here in my own original code I would register a "static logger" to use for general
        // logs in my discord bot to possibly find bugs/issues in my code, but left it out here
        // as not everyone happens to need said "logger".
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

        builder.Services
            .AddOptionsWithValidateOnStart<ConsoleListenerOptions>()
            .Configure(configure);
        builder.Services.AddHostedService<ConsoleListener>();
        builder.Services.Configure<PrettierFormatterOptions>(configure => configure.ConsoleListenerEnabled = true);

        return builder;
    }
}
