namespace Nimble.Extensions.Logging.File;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly IOptions<FileLoggerOptions> _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private IExternalScopeProvider _scopeProvider = null!;

    public FileLoggerProvider(IOptions<FileLoggerOptions> options)
        => this._options = options;

    public ILogger CreateLogger(string name)
        => this._loggers.GetOrAdd(name, n => new FileLogger(n, this._options));

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        this._scopeProvider = scopeProvider;
        foreach (var logger in this._loggers)
        {
            logger.Value.ScopeProvider = this._scopeProvider;
        }
    }

    public void Dispose()
    {
    }
}
