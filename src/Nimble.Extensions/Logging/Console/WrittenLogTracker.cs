using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Nimble.Extensions.Logging.Console;

/// <summary>
///     A utility class that tracks the last written log entry, allowing for comparison of new log entries to determine if they are part of the same log. 
///     This is used to group related log entries together in the console output, improving readability and organization of logs.
/// </summary>
/// <remarks>
///     It is not recommended to use this class directly. 
///     It is intended for internal use by the <see cref="PrettierFormatter"/> and may be subject to change or removal in future versions of the library.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WrittenLogTracker
{
    private WrittenLog? _lastWrittenLog;

    /// <summary>
    ///     Determines if the provided log entry (category and level) is the same as the last written log entry. 
    ///     If it is the same, it returns true; otherwise, it updates the last written log entry and returns false.
    /// </summary>
    /// <param name="category">The category of the log entry to compare.</param>
    /// <param name="level">The log level of the log entry to compare.</param>
    /// <returns><see langword="true"/> if the log entry is part of the same log; otherwise, <see langword="false"/>.</returns>
    public bool IsCategoricallyEqual(string category, LogLevel level)
    {
        if (_lastWrittenLog.HasValue && _lastWrittenLog.Value.IsSameLog(category, level))
            return true;

        _lastWrittenLog = new WrittenLog(category, level, DateTime.Now);

        return false;
    }

    /// <summary>
    ///     Resets the last written log entry, clearing the tracking of the last log.
    /// </summary>
    public void Reset() => _lastWrittenLog = null;
}