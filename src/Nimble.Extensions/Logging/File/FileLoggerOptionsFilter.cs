namespace Nimble.Extensions.Logging.File;

using Microsoft.Extensions.Logging;

internal struct FileLoggerOptionsFilter
{
    public FileLoggerOptionsFilter()
    {
    }

    public required bool CaptureScopes { get; set; } = true;

    public required LogLevel MinLevel { get; set; }

    public required string FilePath { get; set; } = string.Empty;

    public required FileLogRollingMethod RollingMethod { get; set; } = FileLogRollingMethod.None;

    private string? OriginalLogFolderName { get; set; }

    private DateOnly? CurrentRollingDate { get; set; }

    internal readonly string DebuggerToString()
        => $"CaptureScopes = {CaptureScopes}, {(
            MinLevel != LogLevel.None ? $"MinLevel = {MinLevel}" : "Enabled = false")}";

    internal void CheckFileRolling()
    {
        OriginalLogFolderName ??= Path.GetDirectoryName(FilePath) ?? string.Empty;
        var directoryName = OriginalLogFolderName;
        var fileName = Path.GetFileName(FilePath) ?? string.Empty;
        if (RollingMethod is not FileLogRollingMethod.None)
        {
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // if null or when the current date is greater than or equal to the current rolling date.
            if (CurrentRollingDate is null || currentDate >= CurrentRollingDate)
            {
                CurrentRollingDate = RollingMethod switch
                {
                    FileLogRollingMethod.Daily => currentDate.AddDays(1),
                    FileLogRollingMethod.Weekly => currentDate.AddDays(7),
                    FileLogRollingMethod.Monthly => currentDate.AddMonths(1),
                    FileLogRollingMethod.Yearly => currentDate.AddYears(1),
                    _ => throw new InvalidOperationException("bug by design.")
                };
            }

            // This should ideally compare the current date with a private stored date so that way there
            // are no attempts to roll the file early in the case of the Monthly/Yearly file rolling
            // options being used.
            directoryName = RollingMethod switch
            {
                FileLogRollingMethod.Daily or FileLogRollingMethod.Weekly => Path.Combine(OriginalLogFolderName, currentDate.ToString("yyyy-MM-dd")),
                FileLogRollingMethod.Monthly => Path.Combine(OriginalLogFolderName, currentDate.ToString("yyyy-MM")),
                FileLogRollingMethod.Yearly => Path.Combine(OriginalLogFolderName, currentDate.ToString("yyyy")),
                _ => throw new InvalidOperationException("bug by design.")
            };
        }

        CreateDirectory(directoryName);
        FilePath = Path.Combine(directoryName, fileName);
    }

    private static void CreateDirectory(string namePath)
    {
        if (!string.IsNullOrEmpty(namePath))
        {
            // If the directory containing the target file name to write to does not exist; create it.
            try
            {
                Directory.CreateDirectory(namePath);
            }
            catch
            {
            }
        }
    }
}
