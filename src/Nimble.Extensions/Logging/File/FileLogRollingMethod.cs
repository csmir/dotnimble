namespace Nimble.Extensions.Logging.File;

/// <summary>
/// This determines the type of rolling for file logging.
/// </summary>
public enum FileLogRollingMethod
{
    /// <summary>
    /// No file rolling is performed.
    /// </summary>
    None = 0,

    /// <summary>
    /// File rolling is performed Daily.
    /// </summary>
    Daily = 1,

    /// <summary>
    /// File rolling is performed Weekly.
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// File rolling is performed Monthly.
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// File rolling is performed Yearly.
    /// </summary>
    Yearly = 4
}
