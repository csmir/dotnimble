using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Nimble.Console;
using Nimble.Text;

namespace Nimble.Extensions.Logging.Console;

/// <summary>
///     A custom console formatter that enhances the readability of log messages by adding structured formatting, color-coding based on log levels, and dynamic wrapping of long messages.
/// </summary>
public sealed class PrettierFormatter : ConsoleFormatter, IDisposable
{

    private const string LOG_PREFIX = " │   ", LOG_SUFFIX = " ├── ";

    private readonly IDisposable? _optionsReloadToken;
    private PrettierFormatterOptions _options;
    private WrittenLog? _lastWrittenLog;
    private bool _hasWrittenBottom;

    /// <summary>
    ///     Creates a new instance of the <see cref="PrettierFormatter"/> class with the specified options. 
    ///     The formatter will listen for changes in the options and reload them when they change, allowing for dynamic updates to the logging behavior without needing to restart the application.
    /// </summary>
    /// <param name="options">The options monitor for <see cref="PrettierFormatterOptions"/>.</param>
    public PrettierFormatter(IOptionsMonitor<PrettierFormatterOptions> options)
        : base(nameof(PrettierFormatter))
    {
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _options = options.CurrentValue;
    }

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var content = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (content != null)
        {
            string message;
            if (!_lastWrittenLog.HasValue || !_lastWrittenLog.Value.IsSameLog(logEntry.Category, logEntry.LogLevel))
            {
                message = $"{GetLevelText(logEntry.LogLevel)} {GetTimestampText(DateTime.Now)} {GetCategoryText(logEntry.Category)}";
                _lastWrittenLog = new(logEntry.Category, logEntry.LogLevel, DateTime.Now);
            }
            else
                message = LOG_PREFIX;

            var logTextWidth = _options.MaxLogWidth - LOG_PREFIX.Length;

            if (content.Length > logTextWidth)
            {
                var wrappedMessage = new ValueStringBuilder();
                int currentIndex = 0;

                while (currentIndex < content.Length)
                {
                    int length = Math.Min(logTextWidth, content.Length - currentIndex);

                    if (length == logTextWidth && currentIndex + length < content.Length)
                        wrappedMessage.AppendLine(string.Concat(content.AsSpan(currentIndex, length), "-"));
                    else
                        wrappedMessage.AppendLine(content.Substring(currentIndex, length));

                    currentIndex += length;
                }

                content = wrappedMessage.ToString();
            }

            if (logEntry.Exception != null)
                content += $"{Environment.NewLine}\t  {logEntry.Exception}";

            message += content.Contains(Environment.NewLine)
                ? string.Join($"{Environment.NewLine}{LOG_PREFIX}", content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                : content;

            textWriter.WriteLine(message);
        }
    }

    #region Text Formatting

    private string GetTimestampText(DateTime timestamp)
    {
        var timestmp = _options.UseUtcTimestamp
            ? timestamp.ToUniversalTime().ToString(_options.TimestampFormat)
            : timestamp.ToString(_options.TimestampFormat);

        return $"{VTSequences.Text.Formatting.FromConsoleColor(_options.TimestampColor)}{timestmp}{VTSequences.Text.Formatting.Default}";
    }

    private string GetCategoryText(string category)
    {
        var categoryColor = !string.IsNullOrEmpty(_options.SpecialCategoryPrefix) && category.StartsWith(_options.SpecialCategoryPrefix, StringComparison.Ordinal)
            ? VTSequences.Text.Formatting.FromConsoleColor(_options.SpecialCategoryColor)
            : VTSequences.Text.Formatting.ForegroundBrightBlack;

        var allowedCategoryWidth = _options.MaxLogWidth - (_options.TimestampFormat?.Length ?? 0) - 5;

        if (category.Length > allowedCategoryWidth)
            category = string.Concat(category.AsSpan(0, allowedCategoryWidth - 3), "...");

        return $"{categoryColor}{category}{VTSequences.Text.Formatting.Default}{Environment.NewLine}{LOG_SUFFIX}";
    }

    private string GetLevelText(LogLevel lvl)
    {
        static string GetLevelPhrase(LogLevel lvl) =>
            lvl switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",

                _ => "???"
            };

        var color = _options.LogLevelColors.TryGetValue(lvl, out var c) ? c : ConsoleColor.White;

        return $"{LOG_PREFIX}{Environment.NewLine}{VTSequences.Text.Formatting.FromConsoleColor(color)}{GetLevelPhrase(lvl)}:";
    }

    #endregion

    private void ReloadLoggerOptions(PrettierFormatterOptions options) =>
        _options = options;

    public void Dispose() => _optionsReloadToken?.Dispose();
}
