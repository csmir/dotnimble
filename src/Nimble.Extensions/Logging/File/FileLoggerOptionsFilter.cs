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

    internal readonly string DebuggerToString()
        => $"CaptureScopes = {this.CaptureScopes}, {(
            this.MinLevel != LogLevel.None ? $"MinLevel = {this.MinLevel}" : "Enabled = false")}";

    internal void CheckFileRolling()
    {
        this.OriginalLogFolderName ??= Path.GetDirectoryName(this.FilePath) ?? string.Empty;
        var directoryName = this.OriginalLogFolderName;
        var fileName = Path.GetFileName(this.FilePath) ?? string.Empty;
        if (this.RollingMethod is not FileLogRollingMethod.None)
        {
            // This should ideally compare the current date with a private stored date so that way there
            // are no attempts to roll the file early in the case of the Monthly/Yearly file rolling
            // options being used.
            directoryName = Path.Combine(this.OriginalLogFolderName, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        CreateDirectory(directoryName);
        this.FilePath = Path.Combine(directoryName, fileName);
    }

    private static void CreateDirectory(string namePath)
    {
        if (!string.IsNullOrEmpty(namePath))
        {
            // If the directory containing the target file name to write to does not exist; create it.
            try
            {
                _ = Directory.CreateDirectory(namePath);
            }
            catch
            {
            }
        }
    }
}
