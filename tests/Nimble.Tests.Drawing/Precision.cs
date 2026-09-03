using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

/// <summary>
///     How precisely a color's components survive a round trip through eight bits per channel.
/// </summary>
/// <remarks>
///     Hue and saturation are ratios of channel differences, so all of their precision comes from
///     the chroma present. A fully saturated mid-tone spreads its channels across the whole range
///     and pins both to a fraction of a step; a near-gray, or anything close to black, spreads them
///     across two or three steps, where rounding one channel swings the hue by tens of degrees. A
///     fixed tolerance would either pass everything or fail the dark and the pale for no reason, so
///     the round-trip tests scale it by the resolution actually available.
///     <para>
///         These are estimates of what the representation can carry, not properties of a color, which
///         is why they live with the tests rather than on <see cref="Composite"/>. The quantity they
///         are all derived from, the chroma itself, is a real measure and does live there.
///     </para>
/// </remarks>
internal static class Precision
{
    /// <summary>
    ///     Asserts that two hues name the same angle, to the precision the color's chroma supports.
    /// </summary>
    /// <remarks>
    ///     Compared the short way around the wheel, so that 357 and 0 read as three degrees apart
    ///     rather than as the whole circle.
    /// </remarks>
    public static void AssertSameHue(double expected, double actual, Composite color)
    {
        double tolerance = HueTolerance(color);
        double difference = Separation(expected, actual);

        Assert.True(difference <= tolerance,
            $"Expected hue {expected:F3} but found {actual:F3}, {difference:F3} degrees away, outside the {tolerance:F3} degree tolerance a chroma of {color.GetChroma()} supports.");
    }

    /// <summary>
    ///     Gets the angle between two hues, the short way around the wheel.
    /// </summary>
    /// <remarks>
    ///     Takes bare angles rather than colors, because the round-trip tests compare a hue that was
    ///     asked for against the one that came back. <see cref="Composite.GetHueDifference"/> is the
    ///     same measure between two colors, and is checked against this.
    /// </remarks>
    public static double Separation(double first, double second)
    {
        double difference = Math.Abs(first - second) % 360d;

        return difference > 180d ? 360d - difference : difference;
    }

    /// <summary>
    ///     Gets the hue tolerance for a color, in degrees.
    /// </summary>
    public static double HueTolerance(Composite color) 
        => 60d / Math.Max(1, color.GetChroma());

    /// <summary>
    ///     Gets the HSL saturation tolerance for a color.
    /// </summary>
    public static double LightnessSaturationTolerance(Composite color)
    {
        int max = color.Max();
        int min = color.Min();

        int divisor = max + min > byte.MaxValue
            ? (byte.MaxValue * 2) - max - min
            : max + min;

        return 4d / Math.Max(1, divisor);
    }

    /// <summary>
    ///     Gets the HSV saturation tolerance for a color.
    /// </summary>
    public static double ValueSaturationTolerance(Composite color) 
        => 3d / Math.Max(1, color.Max());
}
