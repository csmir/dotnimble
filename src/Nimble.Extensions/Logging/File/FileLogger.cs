namespace Nimble.Extensions.Logging.File;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class FileLogger : ILogger
{
    private readonly string categoryName;
    private readonly IOptions<FileLoggerOptions> _options;
    private static readonly Lock _lock = new();

    public FileLogger(string categoryName, IOptions<FileLoggerOptions> options)
    {
        _options = options;
        this.categoryName = categoryName;

        // Create the log folder, this optionally includes the datestamped rolling
        // log folders when rolling logging are enabled.
        options.Value.GetFilter(this.categoryName)?.CheckFileRolling();
    }

    internal IExternalScopeProvider? ScopeProvider { get; set; }

    public bool IsEnabled(LogLevel logLevel)
    {
        var filter = _options.Value.GetFilter(categoryName);
        return filter != null && logLevel >= filter.Value.MinLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (formatter != null)
        {
            if (_lock.TryEnter(Timeout.InfiniteTimeSpan))
            {
                // We know for sure that if IsEnabled returns true, then filter is not null,
                // but we need to get the filter again to get the file path.
                var filter = _options.Value.GetFilter(categoryName);
                var message = $"[{categoryName}] {logLevel}: {DateTime.UtcNow} {formatter(state, exception)}{Environment.NewLine}";

                // if file rolling is enabled this will update the file path in the filter to the new log file.
                filter!.Value.CheckFileRolling();
                try
                {
                    System.IO.File.AppendAllText(filter!.Value.FilePath, message);
                }
                finally
                {
                    _lock.Exit();
                }
            }
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => ScopeProvider?.Push(state);
}
