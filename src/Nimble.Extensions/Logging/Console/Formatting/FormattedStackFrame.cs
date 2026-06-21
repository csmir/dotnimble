using System.Diagnostics;
using System.Reflection;

namespace Nimble.Extensions.Logging.Console.Formatting;

internal readonly struct FormattedStackFrame
{
    public readonly int? Line;
    public readonly int? Column;

    public readonly string? File;
    public readonly string? Member;

    public FormattedStackFrame(MethodBase? method, string? fileName, int lineNumber, int columnNumber)
    {
        if (method != null)
            Member = method.DeclaringType?.FullName + "." + method.Name;

        File = fileName;
        Line = lineNumber > 0 
            ? lineNumber 
            : null;
        Column = columnNumber > 0 
            ? columnNumber 
            : null;
    }

    public static FormattedStackFrame[] GetFormattableStack(StackTrace stackTrace)
    {
        var frames = stackTrace.GetFrames();

        if (frames == null)
            return [];

        var result = new FormattedStackFrame[frames.Length];

        for (var i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            result[i] = new FormattedStackFrame(
                frame.GetMethod(), 
                frame.GetFileName(), 
                frame.GetFileLineNumber(), 
                frame.GetFileColumnNumber()
            );
        }

        return result;
    }
}
