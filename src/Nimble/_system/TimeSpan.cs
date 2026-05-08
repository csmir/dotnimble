using System.ComponentModel;
using System.Text.RegularExpressions;

namespace System;

/// <summary>
///     Extensions for <see cref="TimeSpan"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TimeSpanExtensions
{
    private static readonly Dictionary<string, Func<string, TimeSpan>> _callback;
    private static readonly Regex _timeRegex;

    static TimeSpanExtensions()
    {
        _timeRegex = new Regex(@"(\d+)\s*([a-zA-Z]+)\s*(?:and|,)?\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        _callback = new()
        {
            ["second"] = Seconds,
            ["seconds"] = Seconds,
            ["sec"] = Seconds,
            ["s"] = Seconds,
            ["minute"] = Minutes,
            ["minutes"] = Minutes,
            ["min"] = Minutes,
            ["m"] = Minutes,
            ["hour"] = Hours,
            ["hours"] = Hours,
            ["h"] = Hours,
            ["day"] = Days,
            ["days"] = Days,
            ["d"] = Days,
            ["week"] = Weeks,
            ["weeks"] = Weeks,
            ["w"] = Weeks,
            ["month"] = Months,
            ["months"] = Months
        };
    }

    private static TimeSpan Seconds(string match)
        => new(0, 0, int.Parse(match));

    private static TimeSpan Minutes(string match)
        => new(0, int.Parse(match), 0);

    private static TimeSpan Hours(string match)
        => new(int.Parse(match), 0, 0);

    private static TimeSpan Days(string match)
        => new(int.Parse(match), 0, 0, 0);

    private static TimeSpan Weeks(string match)
        => new(int.Parse(match) * 7, 0, 0, 0);

    private static TimeSpan Months(string match)
        => new((int)(int.Parse(match) * 30.437), 0, 0, 0);

    extension(TimeSpan span)
    {
        /// <summary>
        ///     Attempts to parse the specified input string into a <see cref="TimeSpan"/> value using a flexible parsing strategy.
        /// </summary>
        /// <remarks>
        ///     This method supports parsing time intervals from input strings that may not strictly
        ///     adhere to standard <see cref="TimeSpan"/> formats. It is useful when accepting user input or data from
        ///     sources with inconsistent formatting.
        /// </remarks>
        /// <param name="input">The input string to parse. This string can represent a time interval in various common formats.</param>
        /// <param name="result">When this method returns <see langword="true"/>, contains the parsed <see cref="TimeSpan"/> value that corresponds to the input string; otherwise, contains <see cref="TimeSpan.Zero"/>.</param>
        /// <returns><see langword="true"/> if the input string was successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseFuzzy(string input,
#if NET6_0_OR_GREATER 
            [NotNullWhen(true)]
#endif
            out TimeSpan result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!TimeSpan.TryParse(input, out result))
            {
                var matches = _timeRegex.Matches(input.ToLower().Trim());

                if (matches.Count != 0)
                {
                    foreach (Match match in matches)
                    {
                        if (_callback.TryGetValue(match.Groups[2].Value, out var callback))
                            result += callback(match.Groups[1].Value);
                    }

                    return true;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Formats the timespan into a human-readable string, such as "2 days, 3 hours, and 15 minutes".
        /// </summary>
        /// <returns>A new <see langword="string"/> containing the formatted span of time.</returns>
        public string ToFormattedString()
        {
#if NET6_0_OR_GREATER
            var sb = new Nimble.Text.ValueStringBuilder(stackalloc char[64]);
#else
            var sb = new System.Text.StringBuilder();
#endif

            if (span.Days > 0)
            {
                sb.Append($"{span.Days} day{(span.Days > 1 ? "s" : "")}");

                if (span.Hours > 0 && (span.Minutes > 0 || span.Seconds > 0))
                    sb.Append(", ");

                else if (span.Hours > 0)
                    sb.Append(", and ");

                else
                    return sb.ToString();
            }

            if (span.Hours > 0)
            {
                sb.Append($"{span.Hours} hour{(span.Hours > 1 ? "s" : "")}");

                if (span.Minutes > 0 && span.Seconds > 0)
                    sb.Append(", ");

                else if (span.Minutes > 0)
                    sb.Append(", and ");

                else
                    return sb.ToString();
            }

            if (span.Minutes > 0)
            {
                sb.Append($"{span.Minutes} minute{(span.Minutes > 1 ? "s" : "")}");

                if (span.Seconds > 0)
                    sb.Append(", and ");

                else
                    return sb.ToString();
            }

            if (span.Seconds > 0)
                sb.Append($"{span.Seconds} second{(span.Seconds > 1 ? "s" : "")}");

            return sb.ToString();
        }
    }
}
