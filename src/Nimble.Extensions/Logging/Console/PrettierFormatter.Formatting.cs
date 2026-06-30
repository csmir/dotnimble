using Microsoft.Extensions.Logging;
using Nimble.Extensions.Logging.Console.Formatting;
using Nimble.Text;
using System.Diagnostics;
using ANSI = Nimble.Console.VTSequences.Text.Formatting;

namespace Nimble.Extensions.Logging.Console;

public sealed partial class PrettierFormatter
{
    private void AppendQ(ref ValueStringBuilder stringBuilder)
    {
        stringBuilder
            .AppendLine(LOG_PREFIX)
            .Append(ANSI.ForegroundBrightBlack)
            .Append("CMD: ")
            .Append(ANSI.Default);
    }

    private void AppendContent(ref ValueStringBuilder stringBuilder, string message)
    {
        var contentByLine = message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var isFirstLine = true;
        foreach (var l in contentByLine)
        {
            if (!isFirstLine)
                stringBuilder.Append(LOG_PREFIX);

            stringBuilder.AppendLine(l);
            isFirstLine = false;
        }
    }

    private void AppendException(ref ValueStringBuilder stringBuilder, Exception? exception, int depth = 1)
    {
        if (exception == null)
            return;

        void AppendHead(ref ValueStringBuilder stringBuilder)
        {
            stringBuilder.Append(LOG_PREFIX);

            for (var i = 0; i < depth; i++)
                stringBuilder.Append("  ");
        }

        AppendHead(ref stringBuilder);

        stringBuilder
            .Append(ANSI.ForegroundBrightBlack)
            .Append('(')
            .Append(depth)
            .Append(") ")
            .Append(ANSI.ForegroundBrightRed)
            .Append(exception.GetType().Name)
            .Append(": ")
            .Append(ANSI.Default)
            .AppendLine(exception.Message);

        AppendException(ref stringBuilder, exception.InnerException, depth + 1);

        var trace = new StackTrace(exception, true);

        foreach (var frame in FormattedStackFrame.GetFormattableStack(trace))
        {
            AppendHead(ref stringBuilder);

            stringBuilder
                .Append(ANSI.ForegroundBrightBlack)
                .Append("at ")
                .Append(ANSI.Default)
                .Append(ANSI.Underline)
                .Append(frame.Member ?? "<unknown>")
                .Append(ANSI.NoUnderline)
                .AppendLine(ANSI.Default);

            if (string.IsNullOrEmpty(frame.File))
                continue;

            AppendHead(ref stringBuilder);

            stringBuilder
                .Append(ANSI.ForegroundBrightBlack)
                .Append("in ")
                .Append(ANSI.ForegroundYellow)
                .Append(frame.File ?? "<unknown>")
                .Append(ANSI.ForegroundBrightYellow)
                .Append(" line ")
                .Append(ANSI.Underline)
                .Append(frame.Line?.ToString() ?? "<unknown>")
                .Append(':')
                .Append(frame.Column?.ToString() ?? "<unknown>")
                .Append(ANSI.NoUnderline)
                .AppendLine(ANSI.Default);
        }
    }

    private void AppendTimestamp(ref ValueStringBuilder stringBuilder, DateTime logTime)
    {
        var timeStamp = _options.UseUtcTimestamp
            ? logTime.ToUniversalTime().ToString(_options.TimestampFormat)
            : logTime.ToString(_options.TimestampFormat);

        stringBuilder
            .Append(ANSI.FromConsoleColor(_options.TimestampColor, false))
            .Append(timeStamp)
            .Append(ANSI.Default)
            .Append(' ');
    }

    private void AppendCategory(ref ValueStringBuilder stringBuilder, string category)
    {
        var categoryColor = !string.IsNullOrEmpty(_options.SpecialCategoryPrefix) && category.StartsWith(_options.SpecialCategoryPrefix, StringComparison.Ordinal)
            ? ANSI.FromConsoleColor(_options.SpecialCategoryColor, false)
            : ANSI.ForegroundBrightBlack;

        stringBuilder
            .Append(categoryColor)
            .Append(category)
            .Append(ANSI.Default)
            .AppendLine()
            .Append(LOG_SUFFIX);
    }

    private void AppendLevel(ref ValueStringBuilder stringBuilder, LogLevel lvl, bool fromEmptyTracker)
    {
        static string GetLevelPhrase(LogLevel lvl) =>
            lvl switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",

                _ => "???"
            };

        var color = _options.LogLevelColors.TryGetValue(lvl, out var c) ? c : ConsoleColor.White;

        if (!fromEmptyTracker)
            stringBuilder.AppendLine(LOG_PREFIX);

        stringBuilder
            .Append(ANSI.FromConsoleColor(color, false))
            .Append(GetLevelPhrase(lvl))
            .Append(':')
            .Append(ANSI.Default)
            .Append(' ');
    }
}
