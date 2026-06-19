using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Nimble.Extensions.Logging.Console;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WrittenLogTracker
{
    private WrittenLog? _lastWrittenLog;

    public bool IsCategoricallyEqual(string category, LogLevel level)
    {
        if (_lastWrittenLog.HasValue && _lastWrittenLog.Value.IsSameLog(category, level))
            return true;

        _lastWrittenLog = new WrittenLog(category, level, DateTime.Now);

        return false;
    }

    public void Reset() => _lastWrittenLog = null;
}