namespace Nimble.Extensions.Logging.File;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class FileLogger : ILogger
{
    private readonly string _directoryName;
    private readonly string categoryName;
    private readonly IOptions<FileLoggerOptions> _options;
    private static readonly Lock _lock = new();

    public FileLogger(string categoryName, IOptions<FileLoggerOptions> options)
    {
        this._options = options;
        this.categoryName = categoryName;
        this._directoryName = Path.GetDirectoryName(options.Value.GetFilter(this.categoryName)?.FilePath) ?? string.Empty;
        if (!string.IsNullOrEmpty(this._directoryName))
        {
            // If the directory containing the target file name to write to does not exist; create it.
            if (!Directory.Exists(this._directoryName))
            {
                _ = Directory.CreateDirectory(this._directoryName);
            }
        }
    }

    internal IExternalScopeProvider? ScopeProvider { get; set; }

    public bool IsEnabled(LogLevel logLevel)
    {
        var filter = this._options.Value.GetFilter(this.categoryName);
        return filter != null && logLevel >= filter.Value.MinLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
        {
            return;
        }

        if (formatter != null)
        {
            if (_lock.TryEnter(Timeout.InfiniteTimeSpan))
            {
                // We know for sure that if IsEnabled returns true, then filter is not null,
                // but we need to get the filter again to get the file path.
                var filter = this._options.Value.GetFilter(this.categoryName);
                var message = $"[{this.categoryName}] {logLevel}: {DateTime.UtcNow} {formatter(state, exception)}{Environment.NewLine}";
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
        => this.ScopeProvider?.Push(state);
}
