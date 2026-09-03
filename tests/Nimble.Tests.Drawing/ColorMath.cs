using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

/// <summary>
///     A reference implementation of every formula <see cref="Composite"/> evaluates, written the naive way
///     and in double precision.
/// </summary>
/// <remarks>
///     Nothing here is shared with the implementation under test: the transfer functions call
///     <see cref="Math.Pow"/> rather than reading a table, the cube roots call <see cref="Math.Cbrt"/>
///     rather than refining a seed, and the bit tricks are written out a bit at a time. That is the
///     point. The library is free to reach the same numbers by whatever route is fastest, and these
///     tests say whether it still arrives at them.
/// </remarks>
internal static class ColorMath
{
    public const int HilbertBits = 10;

    public const float
        OklabLMin = 0f, OklabLMax = 1f,
        OklabAMin = -0.234f, OklabAMax = 0.277f,
        OklabBMin = -0.312f, OklabBMax = 0.199f;

    /// <summary>
    ///     Undoes the sRGB transfer function on an 8-bit channel.
    /// </summary>
    public static double Linearize(byte channel)
    {
        double value = channel / 255d;

        return value <= 0.04045d
            ? value / 12.92d
            : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
    }

    /// <summary>
    ///     Applies the sRGB transfer function to a linear intensity.
    /// </summary>
    public static double Encode(double linear)
    {
        return linear <= 0.0031308d
            ? linear * 12.92d
            : (1.055d * Math.Pow(linear, 1d / 2.4d)) - 0.055d;
    }

    public static double Luminosity(Composite color)
    {
        return (0.2126d * Linearize(color.R))
             + (0.7152d * Linearize(color.G))
             + (0.0722d * Linearize(color.B));
    }

    public static double RelativeLuminance(Composite color)
    {
        return (0.2126d * color.R)
             + (0.7152d * color.G)
             + (0.0722d * color.B);
    }

    public static double PerceivedLightness(Composite color)
        => LStar(Luminosity(color));

    public static double LStar(double y)
    {
        return y <= 216d / 24389d
            ? y * (24389d / 27d)
            : (Math.Cbrt(y) * 116d) - 16d;
    }

    public static double PerceivedBrightness(Composite color)
    {
        return Math.Sqrt((0.299d * color.R * color.R)
                       + (0.587d * color.G * color.G)
                       + (0.114d * color.B * color.B));
    }

    public static double TransferCurve(Composite color)
    {
        return Encode((0.299d * Linearize(color.R))
                    + (0.587d * Linearize(color.G))
                    + (0.114d * Linearize(color.B)));
    }

    public static double CombinedWavelength(Composite color)
        => 650d - ((650d - 400d) / 270d * Hue(color));

    public static double Hue(Composite color)
    {
        int max = Math.Max(Math.Max(color.R, color.G), color.B);
        int min = Math.Min(Math.Min(color.R, color.G), color.B);

        if (max == min)
            return 0d;

        double delta = max - min;
        double hue;

        if (color.R == max)
            hue = (color.G - color.B) / delta;
        else if (color.G == max)
            hue = ((color.B - color.R) / delta) + 2d;
        else
            hue = ((color.R - color.G) / delta) + 4d;

        hue *= 60d;

        return hue < 0d ? hue + 360d : hue;
    }

    public static double Saturation(Composite color)
    {
        int max = Math.Max(Math.Max(color.R, color.G), color.B);
        int min = Math.Min(Math.Min(color.R, color.G), color.B);

        if (max == min)
            return 0d;

        double lightness = (max + min) / 510d;

        return lightness <= 0.5d
            ? (max - min) / (double)(max + min)
            : (max - min) / (double)(510 - max - min);
    }

    public static double Lightness(Composite color)
    {
        int max = Math.Max(Math.Max(color.R, color.G), color.B);
        int min = Math.Min(Math.Min(color.R, color.G), color.B);

        return (max + min) / 510d;
    }

    public static (double H, double S, double V) HSV(Composite color)
    {
        int max = Math.Max(Math.Max(color.R, color.G), color.B);
        int min = Math.Min(Math.Min(color.R, color.G), color.B);

        double saturation = max == 0 ? 0d : 1d - (min / (double)max);

        return (Hue(color), saturation, max / 255d);
    }

    public static (double X, double Y, double Z) XYZ(Composite color)
    {
        double r = Linearize(color.R);
        double g = Linearize(color.G);
        double b = Linearize(color.B);

        return ((r * 0.4124564d) + (g * 0.3575761d) + (b * 0.1804375d),
                (r * 0.2126729d) + (g * 0.7151522d) + (b * 0.0721750d),
                (r * 0.0193339d) + (g * 0.1191920d) + (b * 0.9503041d));
    }

    public static (double L, double A, double B) CIELAB(Composite color)
    {
        (double x, double y, double z) = XYZ(color);

        double fx = LabF(x / 0.950489d);
        double fy = LabF(y / 1d);
        double fz = LabF(z / 1.088840d);

        return ((116d * fy) - 16d, 500d * (fx - fy), 200d * (fy - fz));
    }

    public static double LabF(double t)
    {
        return t > 216d / 24389d
            ? Math.Cbrt(t)
            : (((24389d / 27d) * t) + 16d) / 116d;
    }

    public static (double L, double A, double B) OKLAB(Composite color)
    {
        double r = Linearize(color.R);
        double g = Linearize(color.G);
        double b = Linearize(color.B);

        double l = (0.4122214708d * r) + (0.5363325363d * g) + (0.0514459929d * b);
        double m = (0.2119034982d * r) + (0.6806995451d * g) + (0.1073969566d * b);
        double s = (0.0883024619d * r) + (0.2817188376d * g) + (0.6299787005d * b);

        double lc = Math.Cbrt(l);
        double mc = Math.Cbrt(m);
        double sc = Math.Cbrt(s);

        return ((lc * 0.2104542553d) + (mc * 0.7936177850d) - (sc * 0.0040720468d),
                (lc * 1.9779984951d) - (mc * 2.4285922050d) + (sc * 0.4505937099d),
                (lc * 0.0259040371d) + (mc * 0.7827717662d) - (sc * 0.8086757660d));
    }

    public static (double L, double C, double H) OKLCH(Composite color)
    {
        (double l, double a, double b) = OKLAB(color);

        double chroma = Math.Sqrt((a * a) + (b * b));
        double hue = Math.Atan2(b, a) * (180d / Math.PI);

        return (l, chroma, hue < 0d ? hue + 360d : hue);
    }

    /// <summary>
    ///     Spreads the low ten bits of a value so that bit j lands at 3j, one bit at a time.
    /// </summary>
    public static uint Spread(uint value)
    {
        uint result = 0;

        for (int bit = 0; bit < HilbertBits; bit++)
            result |= ((value >> bit) & 1u) << (bit * 3);

        return result;
    }

    public static int ZValue(Composite color)
        => (int)(Spread(color.R) | (Spread(color.G) << 1) | (Spread(color.B) << 2));

    /// <summary>
    ///     Distance along a 3D Hilbert curve by Skilling's transform, written out bit by bit.
    /// </summary>
    public static uint Hilbert(uint x, uint y, uint z)
    {
        uint t;

        for (int k = HilbertBits - 1; k >= 1; k--)
        {
            uint p = (1u << k) - 1;

            if (((x >> k) & 1u) != 0)
            {
                x ^= p;
            }

            if (((y >> k) & 1u) != 0)
            {
                x ^= p;
            }
            else
            {
                t = (x ^ y) & p;
                x ^= t;
                y ^= t;
            }

            if (((z >> k) & 1u) != 0)
            {
                x ^= p;
            }
            else
            {
                t = (x ^ z) & p;
                x ^= t;
                z ^= t;
            }
        }

        y ^= x;
        z ^= y;

        t = 0;

        for (int k = HilbertBits - 1; k >= 1; k--)
        {
            if (((z >> k) & 1u) != 0)
                t ^= (1u << k) - 1;
        }

        x ^= t;
        y ^= t;
        z ^= t;

        uint distance = 0;

        for (int bit = HilbertBits - 1; bit >= 0; bit--)
        {
            distance = (distance << 1) | ((x >> bit) & 1u);
            distance = (distance << 1) | ((y >> bit) & 1u);
            distance = (distance << 1) | ((z >> bit) & 1u);
        }

        return distance;
    }

    // The three helpers below stay in single precision on purpose.
    // They are the quantization the index builds on rather than a formula worth re-deriving, and a
    // double-precision copy of them would land on the far side of a lattice or band boundary often
    // enough to make the index tests flaky for reasons that say nothing about the code under test.

    /// <summary>
    ///     Maps an OKLAB coordinate onto the lattice the Hilbert curve is defined over.
    /// </summary>
    public static uint Quantize(float value, float min, float max)
    {
        const uint steps = (1u << HilbertBits) - 1;

        float scaled = (value - min) / (max - min) * steps;

        if (scaled <= 0f)
            return 0;

        if (scaled >= steps)
            return steps;

        return (uint)(scaled + 0.5f);
    }

    /// <summary>
    ///     Quantizes a normalized component into one of eight bands.
    /// </summary>
    public static int Band(float value)
    {
        int band = (int)(value * 8);

        if (band >= 8)
            return 7;

        return band < 0 ? 0 : band;
    }

    /// <summary>
    ///     Packs three band indices into a single lexicographically ordered value.
    /// </summary>
    public static double Pack(int primary, int secondary, int tertiary, float tieBreaker)
    {
        int packed = (((primary * 8) + secondary) * 8) + tertiary;

        return packed + (Math.Clamp(tieBreaker, 0f, 1f) * 0.5d);
    }

    public static double Rotate(double angle, double degrees)
    {
        angle = (angle + degrees) % 360d;

        return angle < 0d ? angle + 360d : angle;
    }
}
