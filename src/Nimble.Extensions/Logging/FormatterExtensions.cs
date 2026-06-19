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
        _ = builder.AddConsoleFormatter<PrettierFormatter, PrettierFormatterOptions>()
            .AddConsole(static (options) => options.FormatterName = nameof(PrettierFormatter));
        _ = builder.Services.AddSingleton<WrittenLogTracker>()
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
            _ = builder.ClearProviders();
        }

        _ = builder.Services
            .Configure(configureOptions)
            .AddSingleton<ILoggerProvider, FileLoggerProvider>();

        // here in my own original code I would register a "static logger" to use for general
        // logs in my discord bot to possibly find bugs/issues in my code, but left it out here
        // as not everyone happens to need said "logger".
        return builder;
    }

    public static ILoggingBuilder AddConsoleListener(this ILoggingBuilder builder, Action<ConsoleListenerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddOptionsWithValidateOnStart<ConsoleListenerOptions>()
            .Configure(configure);
        builder.Services.AddHostedService<ConsoleListener>();

        return builder;
    }
}
