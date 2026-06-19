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

    internal readonly string DebuggerToString()
        => $"CaptureScopes = {this.CaptureScopes}, {(
            this.MinLevel != LogLevel.None ? $"MinLevel = {this.MinLevel}" : "Enabled = false")}";
}
