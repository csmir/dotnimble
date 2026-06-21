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
        => _options = options;

    public ILogger CreateLogger(string name)
        => _loggers.GetOrAdd(name, n => new FileLogger(n, _options));

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
        foreach (var logger in _loggers)
        {
            logger.Value.ScopeProvider = _scopeProvider;
        }
    }

    public void Dispose()
    {
    }
}
