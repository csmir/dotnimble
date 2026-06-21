#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

using System.ComponentModel;
using System.Text.RegularExpressions;

namespace System;

/// <summary>
///     Extensions for <see cref="TimeSpan"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class TimeSpanExtensions
{
#if NET7_0_OR_GREATER
    [StringSyntax("Regex")]
#endif
    private const string REGEX = @"(\d+)\s*([a-zA-Z]+)\s*(?:and|,)?\s*";

#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<string, Func<double, TimeSpan>> _callback = new Dictionary<string, Func<double, TimeSpan>>()
#else
    private static readonly Dictionary<string, Func<double, TimeSpan>> _callback = new()
#endif
    {
        ["second"]  = TimeSpan.FromSeconds,
        ["seconds"] = TimeSpan.FromSeconds,
        ["sec"]     = TimeSpan.FromSeconds,
        ["s"]       = TimeSpan.FromSeconds,
        ["minute"]  = TimeSpan.FromMinutes,
        ["minutes"] = TimeSpan.FromMinutes,
        ["min"]     = TimeSpan.FromMinutes,
        ["m"]       = TimeSpan.FromMinutes,
        ["hour"]    = TimeSpan.FromHours,
        ["hours"]   = TimeSpan.FromHours,
        ["h"]       = TimeSpan.FromHours,
        ["day"]     = TimeSpan.FromDays,
        ["days"]    = TimeSpan.FromDays,
        ["d"]       = TimeSpan.FromDays,
        ["week"]    = TimeSpan.FromWeeks,
        ["weeks"]   = TimeSpan.FromWeeks,
        ["w"]       = TimeSpan.FromWeeks,
        ["month"]   = TimeSpan.FromMonths,
        ["months"]  = TimeSpan.FromMonths
#if NET8_0_OR_GREATER
    }.ToFrozenDictionary();
#else
    };
#endif

#if NET7_0_OR_GREATER
    [GeneratedRegex(REGEX, RegexOptions.IgnoreCase)]
    private static partial Regex TimeRegex { get; }
#else
    private static Regex TimeRegex { get; } = new Regex(REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);
#endif

    extension(TimeSpan span)
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TimeSpan"/> structure to the specified number of weeks.
        /// </summary>
        /// <param name="value">Number of weeks.</param>
        /// <returns>Returns a <see cref="TimeSpan"/> that represents a specified number of weeks.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The parameters specify a <see cref="TimeSpan"/> value less than <see cref="TimeSpan.MinValue"/> or greater than <see cref="TimeSpan.MaxValue"/>.
        /// </exception>
        public static TimeSpan FromWeeks(int value) => TimeSpan.FromWeeks((double)value);

        /// <summary>
        ///     Returns a <see cref="TimeSpan"/> that represents a specified number of weeks, where the specification is accurate to the nearest millisecond.
        /// </summary>
        /// <param name="value">A number of weeks, accurate to the nearest millisecond.</param>
        /// <returns>An object that represents <paramref name="value"/>.</returns>
        /// <exception cref="OverflowException">
        ///     <paramref name="value"/> is less than <see cref="TimeSpan.MinValue"/> or greater than <see cref="TimeSpan.MaxValue"/>. -or-
        ///     <paramref name="value"/> is <see cref="double.PositiveInfinity"/>. -or-
        ///     <paramref name="value"/> is <see cref="double.NegativeInfinity"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="value"/> is equal to <see cref="double.NaN"/>.
        /// </exception>
        public static TimeSpan FromWeeks(double value) => TimeSpan.FromDays(value * 7);

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimeSpan"/> structure to the specified number of months.
        /// </summary>
        /// <param name="value">Number of months.</param>
        /// <returns>Returns a <see cref="TimeSpan"/> that represents a specified number of months.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The parameters specify a <see cref="TimeSpan"/> value less than <see cref="TimeSpan.MinValue"/> or greater than <see cref="TimeSpan.MaxValue"/>.
        /// </exception>
        public static TimeSpan FromMonths(int value) => TimeSpan.FromMonths((double)value);

        /// <summary>
        ///     Returns a <see cref="TimeSpan"/> that represents a specified number of weeks, where the specification is accurate to the nearest millisecond.
        /// </summary>
        /// <param name="value">A number of weeks, accurate to the nearest millisecond.</param>
        /// <returns>An object that represents <paramref name="value"/>.</returns>
        /// <exception cref="OverflowException">
        ///     <paramref name="value"/> is less than <see cref="TimeSpan.MinValue"/> or greater than <see cref="TimeSpan.MaxValue"/>. -or-
        ///     <paramref name="value"/> is <see cref="double.PositiveInfinity"/>. -or-
        ///     <paramref name="value"/> is <see cref="double.NegativeInfinity"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="value"/> is equal to <see cref="double.NaN"/>.
        /// </exception>
        public static TimeSpan FromMonths(double value) => TimeSpan.FromDays((int)(value * 30.4375));

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

            if (string.IsNullOrWhiteSpace(input)) return false;

            if (TimeSpan.TryParse(input, out result)) return true;

            var matches = TimeRegex.Matches(input);

            bool parsed = false;

            foreach (Match match in matches)
            {
                if (!_callback.TryGetValue(match.Groups[2].Value, out var callback)) continue;

                parsed = true;
                result += callback(int.Parse(match.Groups[1].Value));
            }

            return parsed;
        }

        /// <summary>
        ///     Formats the timespan into a human-readable string, such as "2 days, 3 hours, and 15 minutes".
        /// </summary>
        /// <returns>A new <see langword="string"/> containing the formatted span of time.</returns>
        public string ToFormattedString()
        {
            if (span == TimeSpan.Zero) return "0 seconds";

#if NET6_0_OR_GREATER
            var sb = new Nimble.Text.ValueStringBuilder(stackalloc char[64]);
#else
            var sb = new Text.StringBuilder();
#endif

            int count = 0;

            if (span.Days    > 0) count++;
            if (span.Hours   > 0) count++;
            if (span.Minutes > 0) count++;
            if (span.Seconds > 0) count++;

            int current = 0;

#if NET6_0_OR_GREATER
            void AppendPart(ref Nimble.Text.ValueStringBuilder sb, int value, string unit)
#else
    void AppendPart(Text.StringBuilder sb, int value, string unit)
#endif
            {
                if (value <= 0) return;

                current++;

                if (current > 1)
                {
                    if (current == count)
                    {
                        sb.Append(count == 2 ? " and " : ", and ");
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(value).Append(' ').Append(unit);

                if (value != 1) sb.Append('s');
            }

#if NET6_0_OR_GREATER
            AppendPart(ref sb, span.Days,    "day");
            AppendPart(ref sb, span.Hours,   "hour");
            AppendPart(ref sb, span.Minutes, "minute");
            AppendPart(ref sb, span.Seconds, "second");
#else
            AppendPart(sb, span.Days, "day");
            AppendPart(sb, span.Hours, "hour");
            AppendPart(sb, span.Minutes, "minute");
            AppendPart(sb, span.Seconds, "second");
#endif

            return sb.ToString();
        }
    }
}
