using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Nimble.Text;

namespace Nimble.Extensions.Logging.Console;

/// <summary>
///     A custom console formatter that enhances the readability of log messages by adding structured formatting, color-coding based on log levels, and dynamic wrapping of long messages.
/// </summary>
public sealed partial class PrettierFormatter : ConsoleFormatter, IDisposable
{
    private const string LOG_PREFIX = " │   ", LOG_SUFFIX = " ├── ", LOG_STUB = " : ";

    private readonly WrittenLogTracker _logTracker;
    private readonly IDisposable? _optionsReloadToken;
    private PrettierFormatterOptions _options;

    /// <summary>
    ///     Creates a new instance of the <see cref="PrettierFormatter"/> class with the specified options. 
    ///     The formatter will listen for changes in the options and reload them when they change, allowing for dynamic updates to the logging behavior without needing to restart the application.
    /// </summary>
    /// <param name="options">The options monitor for <see cref="PrettierFormatterOptions"/>.</param>
    /// <param name="logTracker">The log tracker, used to track written logs.</param>
    public PrettierFormatter(IOptionsMonitor<PrettierFormatterOptions> options, WrittenLogTracker logTracker)
        : base(nameof(PrettierFormatter))
    {
        _optionsReloadToken = options.OnChange(o => _options = o);
        _options = options.CurrentValue;
        _logTracker = logTracker;
    }

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var content = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        var stringBuilder = new ValueStringBuilder(stackalloc char[256]);

        if (!string.IsNullOrEmpty(content))
        {
            if (!_logTracker.IsCategoricallyEqual(logEntry.Category, logEntry.LogLevel, out var fromEmptyTracker))
            {
                AppendLevel(ref stringBuilder, logEntry.LogLevel, fromEmptyTracker);
                AppendTimestamp(ref stringBuilder, DateTime.Now);
                AppendCategory(ref stringBuilder, logEntry.Category);
            }
            else
                stringBuilder.Append(LOG_PREFIX);

            AppendContent(ref stringBuilder, content);
            AppendException(ref stringBuilder, logEntry.Exception);

            textWriter.Write(stringBuilder.ToString());
        }
    }

    /// <inheritdoc />
    public void Dispose() 
        => _optionsReloadToken?.Dispose();
}
