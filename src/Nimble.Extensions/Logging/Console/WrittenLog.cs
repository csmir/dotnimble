using Microsoft.Extensions.Logging;

namespace Nimble.Extensions.Logging.Console;

/// <summary>
///     A log that has already been written using this formatter.
/// </summary>
/// <param name="Category">The category of the log entry.</param>
/// <param name="Level">The log level of the log entry.</param>
/// <param name="LastTimestamp">The timestamp of the last log entry.</param>
public record struct WrittenLog(string Category, LogLevel Level, DateTime LastTimestamp)
{
    /// <summary>
    ///     Checks whether or not the provided category and log level match this log's category and log level, and whether the last timestamp of this log is within 10 seconds of the current time. 
    ///     This is used to determine if a new log entry is part of the same log as this one, allowing for grouping of related log entries together in the console output.
    /// </summary>
    /// <param name="category">The category of the log entry to compare.</param>
    /// <param name="level">The log level of the log entry to compare.</param>
    /// <returns><see langword="true"/> if the log entry is part of the same log; otherwise, <see langword="false"/>.</returns>
    public readonly bool IsSameLog(string category, LogLevel level) =>
        Category == category && Level == level && (DateTime.Now - LastTimestamp).TotalSeconds < 10;
}