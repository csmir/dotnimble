namespace Nimble.Extensions.Logging.File;

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// The options for the FileLogger.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public sealed class FileLoggerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerOptions" /> class.
    /// </summary>
    public FileLoggerOptions()
    {
    }

    private ConcurrentDictionary<string, FileLoggerOptionsFilter> Filters { get; } = new();

    /// <summary>
    /// Adds a new logging filter to this options instance.
    /// </summary>
    /// <param name="categoryName">The category for the filter.</param>
    /// <param name="filePath">The file path (including file name) for the log file to output to.</param>
    /// <param name="minLevel">The minimum level of log messages for the filter.</param>
    /// <param name="captureScopes">To enable capturing of logging scopes in the filter.</param>
    /// <returns>The same options so that multiple calls can be chained.</returns>
    public FileLoggerOptions AddFilter(
        string categoryName,
        string filePath,
        LogLevel minLevel = LogLevel.Debug,
        bool captureScopes = true,
        FileLogRollingMethod rollingMethod = FileLogRollingMethod.None)
    {
        Filters.TryAdd(categoryName, new FileLoggerOptionsFilter
        {
            CaptureScopes = captureScopes,
            MinLevel = minLevel,
            FilePath = filePath,
            RollingMethod = rollingMethod,
        });
        return this;
    }

    // TODO: Add the ability to configure the formatter in this Options instance for the file logger.
    public FileLoggerOptions AddFormatter()
    {
        return this;
    }

    internal FileLoggerOptionsFilter? GetFilter(string categoryName)
    {
        foreach (var filter in Filters)
        {
            if (categoryName.StartsWith(filter.Key, StringComparison.Ordinal))
            {
                return filter.Value;
            }
        }

        // in the code that uses this method, if the return value from this is null, then it will be logged anyways.
        return null;
    }

    internal string DebuggerToString()
        => string.Join(", ", Filters.Values.Select(static flof => flof.DebuggerToString()));
}
