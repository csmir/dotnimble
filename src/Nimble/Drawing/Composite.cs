using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Nimble.Drawing;

/// <summary>
///     An sRGB scientific representation that uses a 32-bit unsigned integer to store the RGBA (red, green, blue, alpha) colour channels.
/// </summary>
/// <remarks>
///     Create a new <see cref="Composite"/> from sRGB uint representation of the color. Other color formats are accepted using static FromX methods.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
[DebuggerDisplay("R = {R}, G = {G}, B = {B}, A = {A}")]
public readonly partial struct Composite : IEquatable<Color>, IEquatable<Composite>, IComparable<Composite>, ICloneable
{
    #region Constants

    // An 8-bit channel has only 256 possible values, so both sRGB transfer functions are tabulated
    // once at type-load instead of evaluating a Pow per channel on every single conversion.
    private static readonly float[] VLINEAR_LUT = BuildTransferTable(linearize: true);
    private static readonly float[] GAMMA_LUT = BuildTransferTable(linearize: false);


    const float
        REC_709_R = 0.2126f,
        REC_709_G = 0.7152f,
        REC_709_B = 0.0722f;

    const float
        BT_601_R = 0.299f,
        BT_601_G = 0.587f,
        BT_601_B = 0.114f;

    // The sRGB transfer function's parameters, at the precision the curve is defined in. The
    // transfer tables are built from these rather than from the single-precision forms below: an
    // 0.055f promoted back to double no longer cancels against a 1.055f, which leaves the top of the
    // linear curve at 1.0000001 instead of at one, and every value derived from it carrying that.
    const double
        EXACT_SRGBLINEAR_THRESHOLD = 0.04045d,
        EXACT_GAMMA_2_2_THRESHOLD = 0.0031308d,
        EXACT_UPPERFACTOR = 12.92d,
        EXACT_INNERCURVE = 0.055d,
        EXACT_LOWERFACTOR = 1.055d,
        EXACT_GAMMACOEFFICIENT = 2.4d;

    // The same parameters for the single-precision path, narrowed from the definitions above rather
    // than written out a second time, so that the two forms of each cannot drift apart.
    const float
        SRGBLINEAR_THRESHOLD = (float)EXACT_SRGBLINEAR_THRESHOLD,
        GAMMA_2_2_THRESHOLD = (float)EXACT_GAMMA_2_2_THRESHOLD;

    const float
        LINEAR_UPPERFACTOR = (float)EXACT_UPPERFACTOR,
        LINEAR_INNERCURVE = (float)EXACT_INNERCURVE,
        LINEAR_LOWERFACTOR = (float)EXACT_LOWERFACTOR,
        LINEAR_GAMMACOEFFICIENT = (float)EXACT_GAMMACOEFFICIENT;

    const float
        CIE_LSTAR_THRESHOLD = 216f / 24389f,
        CIE_LSTAR_UPPERMUL = 24389f / 27f,
        CIE_LSTAR_OFFSET = 16f;

    // Seed for the bit-level cube root approximation. Dividing a float's raw bits by three divides
    // its exponent by three; this constant re-biases the result and absorbs most of the error the
    // mantissa picks up on the way. Picked by sweeping candidates against the exact cube root over
    // the [0,1] domain that every call site works in.
    const int CBRT_SEED = 0x2A5137A0;

    // CIE D65 white point (2 degree standard observer), used to normalize XYZ before CIE-LAB.
    const float
        D65_XN = 0.950489f,
        D65_YN = 1f,
        D65_ZN = 1.088840f;

    // Morton (Z-order) bit-spreading masks for 10-bit lanes. C# has no octal literals, so these
    // are written in hexadecimal.
    const uint
        ZCURVE_SHIFT16 = 0x030000FF,
        ZCURVE_SHIFT08 = 0x0300F00F,
        ZCURVE_SHIFT04 = 0x030C30C3,
        ZCURVE_SHIFT02 = 0x09249249;

    // CFACTOR is the number of bands each component of a composite sort index is quantized into.
    const int
        CFACTOR = 8,
        MAX_DEGREES = 360;

    // 180 / pi, so that the polar color spaces convert their angle with a multiply.
    const float RADIANS_TO_DEGREES = 57.29577951308232f;

    // Bits per axis for the OKLAB Hilbert lattice. Three axes at 10 bits fill a 30-bit index.
    const int HILBERT_BITS = 10;

    // Extent of the sRGB gamut in OKLAB. Coordinates are clamped into this box before they are
    // quantized, so values marginally outside it are folded onto the edge rather than wrapping.
    const float
        OKLAB_L_MIN = 0f, OKLAB_L_MAX = 1f,
        OKLAB_A_MIN = -0.234f, OKLAB_A_MAX = 0.277f,
        OKLAB_B_MIN = -0.312f, OKLAB_B_MAX = 0.199f;

    // The visible spectrum, mapped so that hue 0 (red) sits at the long-wavelength end.
    const float
        WAVELENGTH_MAX_NM = 650f,
        WAVELENGTH_MIN_NM = 400f,
        WAVELENGTH_HUE_SPAN = 270f;

    #endregion

    /// <summary>
    ///     The 32-bit unsigned integer representation of the colour.
    /// </summary>
    [FieldOffset(0)]
    public readonly uint Value;

    /// <summary>
    ///     The B (blue) colour channel.
    /// </summary>
    /// <remarks>
    ///     A value of 0 results in no blue being present in the colour, while a value of 255 results in full blue intensity.
    /// </remarks>
    [FieldOffset(0)]
    public readonly byte B;

    /// <summary>
    ///     The G (green) colour channel.
    /// </summary>
    /// <remarks>
    ///     A value of 0 results in no green being present in the colour, while a value of 255 results in full green intensity.
    /// </remarks>
    [FieldOffset(1)]
    public readonly byte G;

    /// <summary>
    ///     The R (red) colour channel.
    /// </summary>
    /// <remarks>
    ///     A value of 0 results in no red being present in the colour, while a value of 255 results in full red intensity.
    /// </remarks>
    [FieldOffset(2)]
    public readonly byte R;

    /// <summary>
    ///     The A (alpha) colour channel, which controls the opacity of the colour. 
    /// </summary>
    /// <remarks>
    ///     A value of 0 is fully transparent, while a value of 255 is fully opaque.
    /// </remarks>
    [FieldOffset(3)]
    public readonly byte A;

    /// <summary>
    ///     Creates a new <see cref="Composite"/> value based on the provided 32-bit sRGB (A) representation.
    /// </summary>
    /// <param name="argb">A 32 bit representation of RGBA.</param>
    public Composite(uint argb)
        => Value = argb;

    /// <summary>
    ///     Creates a new <see cref="Composite"/> value based on the provided sRGB (A) channels.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The (optional) alpha channel.</param>
    public Composite(byte r, byte g, byte b, byte a = byte.MaxValue)
        => Value = Encode(r, g, b, a);

    #region Internal Constructors

    private Composite(float h, float s, float v)
    {
        var scaled = h / 60f;
        var sector = (int)scaled;
        var f = scaled - sector;

        // Wrap into [0,6) so that a hue at or beyond 360 degrees cannot index off the end. One
        // remainder and a conditional add, rather than the two remainders a symmetric wrap costs.
        var hi = sector % 6;

        if (hi < 0)
            hi += 6;

        v *= byte.MaxValue;

        // Convert.ToByte throws on any value that drifts a fraction outside [0,255] and rounds
        // through a call; ToByte clamps and rounds inline instead.
        var b = ToByte(v);
        var p = ToByte(v * (1 - s));
        var q = ToByte(v * (1 - (f * s)));
        var t = ToByte(v * (1 - ((1 - f) * s)));

        Value = hi switch
        {
            0 => Encode(b, t, p, byte.MaxValue),
            1 => Encode(q, b, p, byte.MaxValue),
            2 => Encode(p, b, t, byte.MaxValue),
            3 => Encode(p, q, b, byte.MaxValue),
            4 => Encode(t, p, b, byte.MaxValue),
            _ => Encode(b, p, q, byte.MaxValue),
        };
    }

    private Composite(float h, float s, float l, float a = 1f)
    {
        var c = (1 - Math.Abs((2 * l) - 1)) * s;
        var x = c * (1 - Math.Abs(((h / 60) % 2) - 1));
        var m = l - (c / 2);

        // The chroma components stay in the normalized [0,1] domain so that the lightness offset
        // is applied before the single rounding step, rather than truncating twice.
        float r, g, b;

        if (h < 60)
        {
            r = c;
            g = x;
            b = 0f;
        }
        else if (h < 120)
        {
            r = x;
            g = c;
            b = 0f;
        }
        else if (h < 180)
        {
            r = 0f;
            g = c;
            b = x;
        }
        else if (h < 240)
        {
            r = 0f;
            g = x;
            b = c;
        }
        else if (h < 300)
        {
            r = x;
            g = 0f;
            b = c;
        }
        else
        {
            r = c;
            g = 0f;
            b = x;
        }

        Value = Encode(ToChannel(r + m), ToChannel(g + m), ToChannel(b + m), ToChannel(a));
    }

    #endregion

    /// <summary>
    ///     Gets the luminosity of the color according to the Rec. 709 standard, 
    ///     implementing its Luma coefficients over linearized RGB values. (V-linear algorithm)
    /// </summary>
    /// <returns>True luminosity in accordance to Rec. 709 coefficients over V-linear.</returns>
    public float GetLuminosity()
        => (REC_709_R * VLinear(R))
         + (REC_709_G * VLinear(G))
         + (REC_709_B * VLinear(B));

    /// <summary>
    ///     Gets the Rec. 709 relative luminance for the current color, 
    ///     applying the Luma coefficient without linearization.
    /// </summary>
    /// <remarks>
    ///     The result spans the 0-255 range of the input channels and is gamma-encoded. 
    ///     For the linearized 0-1 luminance that WCAG and the CIE color spaces expect, use <see cref="GetLuminosity"/> instead.
    /// </remarks>
    /// <returns>Relative luminance in accordance to Rec. 709 coefficients, in the 0-255 range.</returns>
    public float GetRelativeLuminance()
        => (REC_709_R * R)
         + (REC_709_G * G)
         + (REC_709_B * B);

    /// <summary>
    ///     Gets the perceived lightness of the color according to the CIE L* color space. 
    ///     This takes the Rec. 709 luminosity and converts it to L*.
    /// </summary>
    /// <returns>Perceived lightness in accordance to Rec. 709 luminosity to L*.</returns>
    public float GetPerceivedLightness()
        => LStar(GetLuminosity());

    /// <summary>
    ///     Gets the perceived brightness of the current color according to the HSP color model using the BT.601 coefficients.
    /// </summary>
    /// <remarks>
    ///     The result spans the 0-255 range of the input channels, unlike <see cref="GetPerceivedLightness"/> which spans 0-100.
    /// </remarks>
    /// <returns>Perceived brightness in accordance to BT.601 coefficients, in the 0-255 range.</returns>
    public float GetPerceivedBrightness()
    {
        // Widened once per channel rather than once per occurrence, so the squares cost a multiply
        // each instead of a second integer-to-float conversion.
        float r = R, g = G, b = B;

        return Sqrt(
            (BT_601_R * r * r) +
            (BT_601_G * g * g) +
            (BT_601_B * b * b)
        );
    }

    /// <summary>
    ///     Gets the wavelength of the color based on its hue, 
    ///     mapping the hue range (0-270) onto the visible spectrum (650-400 nm), so that hue 0 (red) 
    ///     sits at the long-wavelength end and hue 270 (violet) at the short-wavelength end. 
    ///     Hues above 270 (the magentas) are extra-spectral and extrapolate below 400 nm.
    /// </summary>
    /// <returns>The combined wavelength of the color in the visible spectrum, in nanometres.</returns>
    public float GetCombinedWavelength()
        => WAVELENGTH_MAX_NM - ((WAVELENGTH_MAX_NM - WAVELENGTH_MIN_NM) / WAVELENGTH_HUE_SPAN * GetHue());

    /// <summary>
    ///     Gets the gamma-corrected (TRC) luminance of the color using BT.601 coefficients.
    /// </summary>
    /// <returns>The gamma-corrected luminance of the color.</returns>
    public float GetTransferCurve()
        => GammaCore((BT_601_R * VLinear(R))
                   + (BT_601_G * VLinear(G))
                   + (BT_601_B * VLinear(B)));

    /// <summary>
    ///     Gets a Z-order value for the color by interleaving the bits of the RGB channels, 
    ///     effectively creating a 30-bit integer that can be used for spatial sorting of colors in a 3D RGB space.
    /// </summary>
    /// <returns>The Z-order value for this color.</returns>
    public int GetZValue()
        => (int)(ZCurve(R)
              | (ZCurve(G) << 1)
              | (ZCurve(B) << 2));

    /// <summary>
    ///     Gets the hue of the color between 0 and 360 degrees.
    /// </summary>
    /// <returns>The hue of the current color.</returns>
    public float GetHue()
    {
        GetMinMax(out var min, out var max);

        return Hue(min, max);
    }

    /// <summary>
    ///     Gets the HSL accepted saturation of the color as a percentile between 0 and 1.
    /// </summary>
    /// <remarks>
    ///     Intensity is represented between 0% (grayscale) and 100% (full color).
    /// </remarks>
    /// <returns>The saturation of the current color.</returns>
    public float GetSaturation()
    {
        GetMinMax(out var min, out var max);

        return Saturation(min, max);
    }

    /// <summary>
    ///     Gets the HSL accepted brightness (lightness) of the color as a percentile between 0 and 1.
    /// </summary>
    /// <remarks>
    ///     Brightness is represented between 0% (black) and 100% (white), where 50% is normal.
    /// </remarks>
    /// <returns>The brightness of the current color.</returns>
    public float GetBrightness()
    {
        GetMinMax(out var min, out var max);

        return Brightness(min, max);
    }

    /// <summary>
    ///     Gets the chroma of the color: the spread between its largest and smallest channel.
    /// </summary>
    /// <remarks>
    ///     Chroma is what carries the hue, and how much of it is present sets how precisely a hue can be represented at all.
    ///     A fully saturated color spreads its channels across the whole range and pins its hue to a fraction of a degree, while an achromatic color has a chroma of zero and no hue at all.
    ///     The result spans the 0-255 range of the input channels. For how colorful the color is relative to its own lightness, normalized to 0-1, use <see cref="GetSaturation"/> instead.
    /// </remarks>
    /// <returns>The difference between the largest and the smallest of the R, G and B channels.</returns>
    public int GetChroma()
    {
        // Both extremes fall out of a single pass, where Max() - Min() would order the channels twice.
        GetMinMax(out var min, out var max);

        return max - min;
    }

    /// <summary>
    ///     Gets the angle between this color's hue and another color's hue, measured the short way around the color wheel.
    /// </summary>
    /// <remarks>
    ///     Hue is cyclic, so hues of 359 and 1 degrees are two degrees apart rather than 358.
    ///     A color with no chroma has no hue and reports zero, which is what the separation is then measured against; <see cref="GetChroma"/> tells the two cases apart.
    /// </remarks>
    /// <param name="o">The other color to measure the hue separation against.</param>
    /// <returns>The separation between the two hues, between 0 and 180 degrees.</returns>
    public float GetHueDifference(Composite o)
    {
        // Both hues arrive in [0,360), so their difference cannot exceed a full turn and folding it
        // back into a half turn takes a single comparison rather than a remainder.
        var difference = Math.Abs(GetHue() - o.GetHue());

        return difference > 180f ? MAX_DEGREES - difference : difference;
    }

    /// <summary>
    ///     Gets the contrast ratio between this color and another color according to the WCAG guidelines.
    /// </summary>
    /// <param name="o">The color to compare to to define the contrast.</param>
    /// <returns>The contrast ratio between the two colors, where a higher value indicates greater contrast.</returns>
    public double GetContrastRatio(Composite o)
    {
        // WCAG defines the ratio over linearized luminance in the [0,1] range, which is what
        // GetLuminosity produces. GetRelativeLuminance is gamma-encoded and spans [0,255].
        var l1 = GetLuminosity();

        var l2 = o.GetLuminosity();

        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    /// <summary>
    ///     Gets the Euclidean distance between this color and another color in RGB space, 
    ///     providing a simple measure of how different the two colors are based on their red, green, and blue channel values.
    /// </summary>
    /// <param name="o">The other color to calculate the Euclidian distance from.</param>
    /// <returns>The Euclidean distance between the two colors in RGB space, where a higher value indicates greater difference.</returns>
    public double GetEuclidian(Composite o)
    {
        var deltaR = R - o.R;
        var deltaG = G - o.G;
        var deltaB = B - o.B;

        // get euclidean distance between the two colors in RGB space
        // https://en.wikipedia.org/wiki/Color_difference#sRGB

        return Math.Sqrt((deltaR * deltaR) + (deltaG * deltaG) + (deltaB * deltaB));
    }

    /// <summary>
    ///     Gets the CIE Delta E 1976 color difference between this color and another color by converting both colors to the CIE-LAB color space and calculating the Euclidean distance between their L*, a*, and b* values, 
    ///     providing a more perceptually accurate measure of color difference that accounts for human visual sensitivity to different colors.
    /// </summary>
    /// <param name="o">The other color to calculate deltaE from.</param>
    /// <returns>The CIE Delta E 1976 color difference between the two colors, where a higher value indicates greater perceptual difference.</returns>
    public double GetDeltaE(Composite o)
    {
        // https://stackoverflow.com/questions/9018016/how-to-compare-two-colors-for-similarity-difference
        // use CIE-LAB color space for better perceptual distance measurement

        GetCIELAB(out var l1, out var a1, out var b1);
        o.GetCIELAB(out var l2, out var a2, out var b2);

        // get deltaE between the two colors using CIE76 formula
        // https://en.wikipedia.org/wiki/Color_difference#CIE76

        var deltaL = l1 - l2;
        var deltaA = a1 - a2;
        var deltaB = b1 - b2;

        return Math.Sqrt(((double)deltaL * deltaL)
                       + ((double)deltaA * deltaA)
                       + ((double)deltaB * deltaB));
    }

    /// <summary>
    ///     Gets the complementary color by rotating the hue by 180 degrees in the HSV color space while keeping the saturation and value (brightness) the same.
    /// </summary>
    /// <remarks>
    ///     This produces a color that is opposite on the color wheel and provides maximum contrast to the original color.
    /// </remarks>
    /// <returns>A new <see cref="Composite"/> value that is the complementary value of the current color.</returns>
    public Composite GetComplementaryColor()
    {
        GetHSV(out var h, out var s, out var v);

        var shiftH = Rotate(h, 180);

        return new(shiftH, s, v);
    }

    /// <summary>
    ///     Gets the gamma corrected color by applying a gamma function over R, G, B while retaining the alpha channel.
    /// </summary>
    /// <returns>A new <see cref="Composite"/> value that is the gamma-corrected value of the current color.</returns>
    public Composite GetGammaCorrectedColor()
    {
        // Gamma operates on the [0,1] range, so the channels are normalized before encoding.
        return new(
            ToChannel(Gamma(R)),
            ToChannel(Gamma(G)),
            ToChannel(Gamma(B)),
            A
        );
    }

    /// <summary>
    ///     Gets a composite index based on the provided index type for perceptual algorithmic sorting.
    /// </summary>
    /// <param name="indexType">The type of index to generate for this value.</param>
    /// <returns>A value representing a floating point (composite) index for the current value produced according to <paramref name="indexType"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided type is not a named value of <see cref="CompositeIndex"/>.</exception>
    public double GetIndex(CompositeIndex indexType)
    {
        return indexType switch
        {
            CompositeIndex.HLV1D or CompositeIndex.HLV2D
                => GetHLVIndex(indexType is CompositeIndex.HLV1D),
            CompositeIndex.HLV1DInverted or CompositeIndex.HLV2DInverted
                => GetHLVInvertedIndex(indexType is CompositeIndex.HLV1DInverted),
            CompositeIndex.HSV
                => GetHSVIndex(),
            CompositeIndex.HueHilbert
                => GetHueHilbertIndex(),
            _ => throw new ArgumentOutOfRangeException(nameof(indexType)),
        };
    }

    /// <summary>
    ///     Gets the HSV color space representation of the current value as H, S, V.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing H, S, V.</returns>
    public (float H, float S, float V) GetHSV()
    {
        GetHSV(out var h, out var s, out var v);

        return (h, s, v);
    }

    /// <summary>
    ///     Gets the linearized representation of the current value as R, G, B, undoing the sRGB transfer function on each channel.
    /// </summary>
    /// <remarks>
    ///     Linear light is what every physical operation on a color expects: blending, resizing and filtering all produce the wrong answer when applied to the gamma-encoded channels directly.
    ///     It is also the space the other color spaces on this type are derived from.
    ///     The alpha channel carries no transfer function and is not part of the result; read it from <see cref="A"/> where it is needed.
    /// </remarks>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing the linearized R, G, B, each in the 0-1 range.</returns>
    public (float R, float G, float B) GetLinear()
        => (VLinear(R), VLinear(G), VLinear(B));

    /// <summary>
    ///     Gets the CIE-XYZ color space representation of the current value as X, Y, Z.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing X, Y, Z.</returns>
    public (float X, float Y, float Z) GetXYZ()
    {
        GetXYZ(out var x, out var y, out var z);

        return (x, y, z);
    }

    /// <summary>
    ///     Gets the CIE-LAb color space representation of the current value as L*, A*, B*.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing L, A, B.</returns>
    public (float L, float A, float B) GetCIELAB()
    {
        GetCIELAB(out var l, out var a, out var b);

        return (l, a, b);
    }

    /// <summary>
    ///     Gets the OKLAB color space representation of the current value as L, A, B.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing L, A, B.</returns>
    public (float L, float A, float B) GetOKLAB()
    {
        GetOKLAB(out var l, out var a, out var b);

        return (l, a, b);
    }

    /// <summary>
    ///     Gets the OKLCH color space representation of the current value as L, C, h.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing L, C, h.</returns>
    public (float L, float C, float h) GetOKLCH()
    {
        GetOKLCH(out var l, out var c, out var h);

        return (l, c, h);
    }

    /// <summary>
    ///     Gets the HSL color space representation of the current value as H, S, L.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3}"/> containing H, S, L.</returns>
    public (float H, float S, float L) GetHSL()
    {
        GetMinMax(out var min, out var max);

        return (Hue(min, max), Saturation(min, max), Brightness(min, max));
    }

    /// <summary>
    ///     Gets the HSLA color space representation of the current value as H, S, L, A.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3, T4}"/> containing H, S, L, A.</returns>
    public (float H, float S, float L, float A) GetHSLA()
    {
        GetMinMax(out var min, out var max);

        return (Hue(min, max), Saturation(min, max), Brightness(min, max), A / 255f);
    }

    /// <summary>
    ///     Gets the minimum value among the RGB channels of the color.
    /// </summary>
    /// <returns>The smallest value in the set of R, G, B in this color.</returns>
    public int Min()
    {
        GetMinMax(out var min, out _);

        return min;
    }

    /// <summary>
    ///     Gets the maximum value among the RGB channels of the color. 
    /// </summary>
    /// <returns>The largest value in the set of R, G, B in this color.</returns>
    public int Max()
    {
        GetMinMax(out _, out var max);

        return max;
    }

    /// <summary>
    ///     Takes the current red value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified red value. The resulting value is clamped between 0 and 255.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite ShiftRed(int amount)
        => new(ShiftChannel(R, amount), G, B, A);

    /// <summary>
    ///     Takes the current red value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified red value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite SetRed(byte value)
        => new(value, G, B, A);

    /// <summary>
    ///     Takes the current green value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified green value. The resulting value is clamped between 0 and 255.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite ShiftGreen(int amount)
        => new(R, ShiftChannel(G, amount), B, A);

    /// <summary>
    ///     Takes the current green value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified green value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite SetGreen(byte value)
        => new(R, value, B, A);

    /// <summary>
    ///     Takes the current blue value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified blue value. The resulting value is clamped between 0 and 255.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite ShiftBlue(int amount)
        => new(R, G, ShiftChannel(B, amount), A);

    /// <summary>
    ///     Takes the current blue value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified blue value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite SetBlue(byte value)
        => new(R, G, value, A);

    /// <summary>
    ///     Takes the current alpha value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified alpha value. The resulting value is clamped between 0 and 255.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite ShiftAlpha(int amount)
        => new(R, G, B, ShiftChannel(A, amount));

    /// <summary>
    ///     Takes the current alpha value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified alpha value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    public Composite SetAlpha(byte value)
        => new(R, G, B, value);

    /// <summary>
    ///     Shifts the hue of the color by the specified amount, returning a new <see cref="Composite"/> with the modified hue value. The resulting hue value is wrapped around the 0-360 degree range.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is less than -360 or more than 360.</exception>
    public Composite ShiftHue(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(amount, -MAX_DEGREES, MAX_DEGREES, nameof(amount));

        var hsla = GetHSLA();

        hsla.H = (hsla.H + amount) % MAX_DEGREES;

        if (hsla.H < 0)
            hsla.H += MAX_DEGREES;
        else if (hsla.H > MAX_DEGREES)
            hsla.H -= MAX_DEGREES;

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current hue value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified hue value. The resulting hue value is wrapped around the 0-360 degree range.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than -360 or more than 360.</exception>
    public Composite SetHue(float value)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(value, -MAX_DEGREES, MAX_DEGREES, nameof(value));

        var hsla = GetHSLA();

        hsla.H = value % MAX_DEGREES;

        if (hsla.H < 0)
            hsla.H += MAX_DEGREES;
        else if (hsla.H > MAX_DEGREES)
            hsla.H -= MAX_DEGREES;

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current saturation value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified saturation value. The resulting saturation value is clamped to the 0-1 range.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is less than -1 or more than 1.</exception>
    public Composite ShiftSaturation(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(amount, -1f, 1f, nameof(amount));

        var hsla = GetHSLA();
        hsla.S = Clamp(hsla.S + amount, 0f, 1f);

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current saturation value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified saturation value. The resulting saturation value is clamped to the 0-1 range.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than -1 or more than 1.</exception>
    public Composite SetSaturation(float value)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(value, -1f, 1f, nameof(value));

        var hsla = GetHSLA();
        hsla.S = Clamp(value, 0f, 1f);

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current brightness value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified lightness value. The resulting lightness value is clamped to the 0-1 range.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is less than -1 or more than 1.</exception>
    public Composite ShiftBrightness(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(amount, -1f, 1f, nameof(amount));

        var hsla = GetHSLA();
        hsla.L = Clamp(hsla.L + amount, 0f, 1f);

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current brightness value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified lightness value. The resulting lightness value is clamped to the 0-1 range.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than -1 or more than 1.</exception>
    public Composite SetBrightness(float value)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(value, -1f, 1f, nameof(value));

        var hsla = GetHSLA();
        hsla.L = Clamp(value, 0f, 1f);

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Creates a <see cref="Color"/> from the current <see cref="Composite"/> instance.
    /// </summary>
    /// <returns>A new <see cref="Color"/> created from the sRGB value of this <see cref="Composite"/>.</returns>
    public Color ToColor()
        => Color.FromArgb(unchecked((int)Value));

    /// <summary>
    ///     Checks equality to another object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns><see langword="true"/> if the other object's value equals the current value; otherwise <see langword="false"/>.</returns>
    public override bool Equals(
#if NET6_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        object? obj)
        => obj is Composite other && Value == other.Value;

    /// <summary>
    ///     Checks equality to another <see cref="Composite"/> value by comparing their inner <see cref="Value"/>.
    /// </summary>
    /// <param name="other">The value to check equality for.</param>
    /// <returns><see langword="true"/> if the other value equals the current value; otherwise <see langword="false"/>.</returns>
    public bool Equals(Composite other)
        => other.Value == Value;

    /// <summary>
    ///     Checks equality to another <see cref="Color"/> value by comparing the R, G, B and A channels.
    /// </summary>
    /// <param name="other"></param>
    /// <returns><see langword="true"/> if the other value equals the current value; otherwise <see langword="false"/>.</returns>
    public bool Equals(Color other)
        => other.R == R && other.G == G && other.B == B && other.A == A;

    /// <summary>
    ///     Compares the current value to another <see cref="Composite"/> value by comparing their inner <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    ///     To sort colors based on perceptual attributes rather than their raw sRGB values, 
    ///     consider using <see cref="GetIndex(CompositeIndex)"/> with a suitable index type to generate a perceptually meaningful index for sorting.
    /// </remarks>
    /// <param name="other">The other value to compare to.</param>
    /// <returns>Less than zero if the current instance is less than <paramref name="other"/>. Zero if they are equal. More than zero if the current instance is more than <paramref name="other"/>.</returns>
    public int CompareTo(Composite other)
        => Value.CompareTo(other.Value);

    /// <summary>
    ///     Gets the hash code for the current value by returning the hash code of the inner <see cref="Value"/>.
    /// </summary>
    /// <returns>A hash code for the current value.</returns>
    public override int GetHashCode()
        => Value.GetHashCode();

    /// <summary>
    ///     Gets a web-format string representation of the current value in sRGB (A) color space.
    /// </summary>
    /// <returns>A string representing web-format sRGB (A) color space.</returns>
    public override string ToString()
        => ToString(CompositeFormat.RGBA);

    /// <summary>
    ///     Gets a web-format string representation of the current value in the chosen format.
    /// </summary>
    /// <param name="format">The target format for the current value.</param>
    /// <returns>A string representing web-format of the current value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided format is not a named value of <see cref="CompositeFormat"/>.</exception>
    public string ToString(CompositeFormat format)
    {
        switch (format)
        {
#if NET6_0_OR_GREATER
            case CompositeFormat.RGB:
                return ToChannelString(alpha: false);
            case CompositeFormat.RGBA:
                return ToChannelString(alpha: true);
#else
            case CompositeFormat.RGB:
                return $"rgb({R}, {G}, {B})";
            case CompositeFormat.RGBA:
                return $"rgba({R}, {G}, {B}, {A})";
#endif
            case CompositeFormat.HSL:
                {
                    var (h, s, l) = GetHSL();

                    return $"hsl({h}, {s}, {l})";
                }
            case CompositeFormat.HSLA:
                {
                    var (h, s, l, a) = GetHSLA();

                    return $"hsla({h}, {s}, {l}, {a})";
                }
            case CompositeFormat.HSV:
                {
                    var (h, s, v) = GetHSV();

                    return $"hsv({h}, {s}, {v})";
                }
            case CompositeFormat.CIEXYZ:
                {
                    var (x, y, z) = GetXYZ();

                    return $"xyz({x}, {y}, {z})";
                }
            case CompositeFormat.CIELAB:
                {
                    var (l, a, b) = GetCIELAB();

                    return $"cielab({l}, {a}, {b})";
                }
            case CompositeFormat.OKLAB:
                {
                    var (l, a, b) = GetOKLAB();
                    return $"oklab({l}, {a}, {b})";
                }
                case CompositeFormat.OKLCH:
                {
                    var (l, c, h) = GetOKLCH();
                    return $"oklch({l}, {c}, {h})";
                }
            case CompositeFormat.HEX:
                {
#if NET6_0_OR_GREATER
                    return ToHexString();
#else
                    return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
#endif
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    object ICloneable.Clone()
        => new Composite(Value);

    #region Formatting

#if NET6_0_OR_GREATER

    // The channel and hex writers exist because an interpolated string rents a buffer from the
    // array pool and formats each channel through a generic TryFormat that has to parse its format
    // string first. Both forms here have a known maximum length, so the whole result is built in a
    // stack buffer and the only allocation left is the string itself.

    // "rgba(255, 255, 255, 255)" is the longest form the channel writer produces.
    const int RGBA_MAX_LENGTH = 24;

    [SkipLocalsInit]
    private string ToChannelString(bool alpha)
    {
        Span<char> buffer = stackalloc char[RGBA_MAX_LENGTH];

        buffer[0] = 'r';
        buffer[1] = 'g';
        buffer[2] = 'b';

        var position = 3;

        if (alpha)
            buffer[position++] = 'a';

        buffer[position++] = '(';

        position = WriteChannel(buffer, position, R);
        position = WriteSeparator(buffer, position);
        position = WriteChannel(buffer, position, G);
        position = WriteSeparator(buffer, position);
        position = WriteChannel(buffer, position, B);

        if (alpha)
        {
            position = WriteSeparator(buffer, position);
            position = WriteChannel(buffer, position, A);
        }

        buffer[position++] = ')';

        return new string(buffer[..position]);
    }

    [SkipLocalsInit]
    private string ToHexString()
    {
        Span<char> buffer = stackalloc char[9];

        buffer[0] = '#';

        WriteHex(buffer, 1, R);
        WriteHex(buffer, 3, G);
        WriteHex(buffer, 5, B);
        WriteHex(buffer, 7, A);

        return new string(buffer);
    }

    // Writes a channel as decimal digits without a leading zero, and reports the position after it.
    private static int WriteChannel(Span<char> destination, int position, byte value)
    {
        if (value >= 100)
            destination[position++] = (char)('0' + (value / 100));

        if (value >= 10)
            destination[position++] = (char)('0' + ((value / 10) % 10));

        destination[position++] = (char)('0' + (value % 10));

        return position;
    }

    private static int WriteSeparator(Span<char> destination, int position)
    {
        destination[position] = ',';
        destination[position + 1] = ' ';

        return position + 2;
    }

    private static void WriteHex(Span<char> destination, int position, byte value)
    {
        const string DIGITS = "0123456789ABCDEF";

        destination[position] = DIGITS[value >> 4];
        destination[position + 1] = DIGITS[value & 0xF];
    }

#endif

    #endregion

    #region Optimization

    // Packs four channels into the single 32-bit field this type stores.
    //
    // Assigning the four byte fields at their own offsets instead leaves one-byte stores followed
    // by a four-byte read of the same stack slot, which stalls store forwarding on every
    // construction. Building the word in registers and storing it once avoids that entirely.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Encode(byte r, byte g, byte b, byte a)
        => ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

    // Kept as a branch chain rather than the four conditional moves Math.Min/Math.Max lower to.
    // Every caller immediately branches on min == max anyway, and measured against the accessors
    // that actually use it the conditional-move form was consistently a few percent slower.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetMinMax(out int min, out int max)
    {
        if (R > G)
        {
            max = R;
            min = G;
        }
        else
        {
            max = G;
            min = R;
        }

        if (B > max)
            max = B;
        else if (B < min)
            min = B;
    }

    // Hue from an already-computed min/max pair. min == max means R == G == B, which is achromatic.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Hue(int min, int max)
    {
        if (min == max)
            return 0f;

        float delta = max - min;
        float hue;

        if (R == max)
            hue = (G - B) / delta;
        else if (G == max)
            hue = ((B - R) / delta) + 2f;
        else
            hue = ((R - G) / delta) + 4f;

        hue *= 60f;

        if (hue < 0f)
            hue += MAX_DEGREES;

        return hue;
    }

    // HSL saturation from an already-computed min/max pair.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Saturation(int min, int max)
    {
        if (min == max)
            return 0f;

        var div = max + min;

        if (div > byte.MaxValue)
            div = (byte.MaxValue * 2) - max - min;

        return (max - min) / (float)div;
    }

    // HSL lightness from an already-computed min/max pair.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Brightness(int min, int max)
        => (max + min) / (byte.MaxValue * 2f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetXYZ(out float x, out float y, out float z)
        => CIEXYZ(VLinear(R), VLinear(G), VLinear(B), out x, out y, out z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetOKLCH(out float l, out float c, out float h)
    {
        GetOKLAB(out var lS, out var aS, out var bS);

        l = lS;
        c = Sqrt((aS * aS) + (bS * bS));
        h = Atan2(bS, aS) * RADIANS_TO_DEGREES;

        if (h < 0)
            h += MAX_DEGREES;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetOKLAB(out float l, out float a, out float b)
    {
        var rL = VLinear(R);
        var gL = VLinear(G);
        var bL = VLinear(B);

        // Linear sRGB straight to LMS, skipping the intermediate XYZ round trip.
        // https://bottosson.github.io/posts/oklab/#converting-from-linear-srgb-to-oklab
        var lS = (0.4122214708f * rL) + (0.5363325363f * gL) + (0.0514459929f * bL);
        var mS = (0.2119034982f * rL) + (0.6806995451f * gL) + (0.1073969566f * bL);
        var sS = (0.0883024619f * rL) + (0.2817188376f * gL) + (0.6299787005f * bL);

        // The non-linearity here is a cube root, not a cube.
        var lC = Cbrt(lS);
        var mC = Cbrt(mS);
        var sC = Cbrt(sS);

        l = (lC * 0.2104542553f) + (mC * 0.7936177850f) - (sC * 0.0040720468f);
        a = (lC * 1.9779984951f) - (mC * 2.4285922050f) + (sC * 0.4505937099f);
        b = (lC * 0.0259040371f) + (mC * 0.7827717662f) - (sC * 0.8086757660f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetCIELAB(out float l, out float a, out float b)
    {
        GetXYZ(out var x, out var y, out var z);

        // XYZ has to be normalized against the D65 white point before f() is applied.
        var xS = LabF(x / D65_XN);
        var yS = LabF(y / D65_YN);
        var zS = LabF(z / D65_ZN);

        l = (116f * yS) - CIE_LSTAR_OFFSET;
        a = 500f * (xS - yS);
        b = 200f * (yS - zS);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetHSV(out float h, out float s, out float v)
    {
        GetMinMax(out var min, out var max);

        h = Hue(min, max);
        s = (max == 0) ? 0f : 1f - (1f * min / max);
        v = max / 255f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ShiftChannel(byte oldValue, int shift)
    {
        var newRed = oldValue + shift;

        byte newValue;

        if (newRed > byte.MaxValue)
            newValue = byte.MaxValue;
        else if (newRed < byte.MinValue)
            newValue = byte.MinValue;
        else
            newValue = (byte)newRed;

        return newValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Clamp(float value, float minValue, float maxValue)
    {
        if (value < minValue)
            return minValue;
        else if (value > maxValue)
            return maxValue;
        
        return value;
    }

    // Converts a normalized [0,1] channel to its 8-bit representation, rounding rather than
    // truncating so that round trips through the float color spaces stay stable.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToChannel(float value)
        => ToByte(value * byte.MaxValue);

    // Rounds and clamps a [0,255] value to a channel. Unlike Convert.ToByte this never throws on
    // a value that drifts marginally outside the range through floating point error.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToByte(float value)
    {
        if (value <= 0f)
            return byte.MinValue;

        if (value >= byte.MaxValue)
            return byte.MaxValue;

        return (byte)(value + 0.5f);
    }

    #endregion

    #region Algorithmic

    // Gets CIE XYZ values from linear RGB channels, assuming sRGB color space and D65 illuminant.
    // Variables derived from the standard RGB to XYZ conversion matrix for sRGB with D65 white point
    // https://en.wikipedia.org/wiki/SRGB#Primaries
    // https://en.wikipedia.org/wiki/CIE_1931_color_space#From_RGB_to_CIE_XYZ
    // rL, gL, bL are in range [0,1].
    // Returns X, Y, Z in range [0,1].
    private static void CIEXYZ(float rL, float gL, float bL, out float x, out float y, out float z)
    {
        x = (rL * 0.4124564f) + (gL * 0.3575761f) + (bL * 0.1804375f);
        y = (rL * 0.2126729f) + (gL * 0.7151522f) + (bL * 0.0721750f);
        z = (rL * 0.0193339f) + (gL * 0.1191920f) + (bL * 0.9503041f);
    }

    // Gets gamma-corrected (sRGB encoded) value from a linear value.
    // L is in range [0,1].
    // Returns gamma-corrected value in range [0,1].
    private static float GammaCore(float L)
    {
        if (L <= GAMMA_2_2_THRESHOLD)
            return L * LINEAR_UPPERFACTOR;

        return (LINEAR_LOWERFACTOR * Pow(L, 1 / LINEAR_GAMMACOEFFICIENT)) - LINEAR_INNERCURVE;
    }

    // Runs exactly once per table, so the exact double-precision form of each curve is used here
    // rather than the single-precision one the hot path would otherwise have paid for.
    private static float[] BuildTransferTable(bool linearize)
    {
        var table = new float[256];

        for (var i = 0; i < table.Length; i++)
        {
            var v = i / 255d;
            double result;

            if (linearize)
            {
                result = v <= EXACT_SRGBLINEAR_THRESHOLD
                    ? v / EXACT_UPPERFACTOR
                    : Math.Pow((v + EXACT_INNERCURVE) / EXACT_LOWERFACTOR, EXACT_GAMMACOEFFICIENT);
            }
            else
            {
                result = v <= EXACT_GAMMA_2_2_THRESHOLD
                    ? v * EXACT_UPPERFACTOR
                    : (EXACT_LOWERFACTOR * Math.Pow(v, 1d / EXACT_GAMMACOEFFICIENT)) - EXACT_INNERCURVE;
            }

            table[i] = (float)result;
        }

        return table;
    }

    // Gets linearized value from an 8-bit sRGB channel.
    // Returns linear value in range [0,1].
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float VLinear(byte c)
        => VLINEAR_LUT[c];

    // Gets the sRGB encoded value for an 8-bit channel treated as linear intensity.
    // Returns gamma-corrected value in range [0,1].
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Gamma(byte c)
        => GAMMA_LUT[c];

    // Gets L* value from luminosity, assuming D65 illuminant and standard observer.
    // Y is in range [0,1].
    // Returns L* in range [0,100].
    private static float LStar(float Y)
    {
        if (Y <= CIE_LSTAR_THRESHOLD)
            return Y * CIE_LSTAR_UPPERMUL;

        return (Cbrt(Y) * 116f) - CIE_LSTAR_OFFSET;
    }

    // The CIE-LAB f(t) non-linearity. Note this is not L* itself: L* = 116 * f(Y/Yn) - 16, while
    // a* and b* are scaled differences of f() and must not carry the 116 factor.
    // t is the white-point-relative tristimulus value.
    private static float LabF(float t)
    {
        if (t > CIE_LSTAR_THRESHOLD)
            return Cbrt(t);

        return ((CIE_LSTAR_UPPERMUL * t) + CIE_LSTAR_OFFSET) / 116f;
    }

    // Cube root of a non-negative value.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Cbrt(float value)
    {
        // A zero or denormal input would decay towards zero under the refinement below without ever
        // reaching it, leaving black with a lightness a fraction above zero.
        if (value <= 0f)
            return 0f;

        var y = Int32BitsToSingle((SingleToInt32Bits(value) / 3) + CBRT_SEED);

        // Newton on y^3 - value, in the averaged form so the cube is never materialized. Each step
        // squares the relative error: 3e-2, then 1e-3, then 1e-6, then float's own floor.
        y = ((2f / 3f) * y) + (value / (3f * y * y));
        y = ((2f / 3f) * y) + (value / (3f * y * y));
        y = ((2f / 3f) * y) + (value / (3f * y * y));

        return y;
    }

    // Reinterprets a float as its raw bit pattern. BitConverter's single-precision overloads are
    // unavailable on netstandard2.0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int SingleToInt32Bits(float value)
    {
#if NET6_0_OR_GREATER
        return BitConverter.SingleToInt32Bits(value);
#else
        return *(int*)&value;
#endif
    }

    // Reinterprets a raw bit pattern as a float.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe float Int32BitsToSingle(int value)
    {
#if NET6_0_OR_GREATER
        return BitConverter.Int32BitsToSingle(value);
#else
        return *(float*)&value;
#endif
    }

    // Single-precision power. MathF is unavailable on netstandard2.0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Pow(float x, float y)
    {
#if NET6_0_OR_GREATER
        return MathF.Pow(x, y);
#else
        return (float)Math.Pow(x, y);
#endif
    }

    // Single-precision arc tangent of a quotient. Going through the double-precision Math.Atan2
    // pays for two conversions and a wider evaluation than a float result can carry anyway.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Atan2(float y, float x)
    {
#if NET6_0_OR_GREATER
        return MathF.Atan2(y, x);
#else
        return (float)Math.Atan2(y, x);
#endif
    }

    // Single-precision square root. MathF is unavailable on netstandard2.0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Sqrt(float value)
    {
#if NET6_0_OR_GREATER
        return MathF.Sqrt(value);
#else
        return (float)Math.Sqrt(value);
#endif
    }

    // Splits bits of a value into a 30-bit integer for Z-order curve calculation.
    // Only the lowest 10 bits are used, so input should be in range [0,1023].
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZCurve(uint a)
    {
        // split out the lowest 10 bits to lowest 30 bits
        a = (a | (a << 16)) & ZCURVE_SHIFT16;
        a = (a | (a << 08)) & ZCURVE_SHIFT08;
        a = (a | (a << 04)) & ZCURVE_SHIFT04;
        a = (a | (a << 02)) & ZCURVE_SHIFT02;

        return a;
    }

    // Rotates an angle by a certain degree amount, wrapping around at 360 degrees.
    // Returns the new angle in range [0,360].
    private static float Rotate(float angle, float degrees)
    {
        angle = (angle + degrees) % MAX_DEGREES;

        if (angle < 0)
            angle += MAX_DEGREES;

        return angle;
    }

    // Quantizes a normalized [0,1] component into one of CFACTOR bands.
    // Values at exactly 1 fold back into the last band so the result is always in [0,CFACTOR).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Band(float value)
    {
        var band = (int)(value * CFACTOR);

        if (band >= CFACTOR)
            return CFACTOR - 1;

        return band < 0 ? 0 : band;
    }

    // Packs three band indices into a single lexicographically ordered value, so that the primary
    // band dominates the ordering, then the secondary, then the tertiary. Summing the components
    // instead would let a large tertiary value outrank a whole primary band.
    // The trailing fraction orders colors that land in the same cell, keeping the index a total
    // order rather than one with large runs of identical keys.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Pack(int primary, int secondary, int tertiary, float tieBreaker)
    {
        var packed = ((((primary * CFACTOR) + secondary) * CFACTOR) + tertiary);

        // Halved so that the tie breaker can never reach the next cell.
        return packed + (Clamp(tieBreaker, 0f, 1f) * 0.5d);
    }

    // Combines hue, luminosity, and value into a single value for sorting. Hue is weighted most
    // heavily, followed by luminosity and then value.
    // If smooth is true, luminosity and value run backwards through every other hue band, so that
    // the ramp stays continuous where two bands meet instead of snapping back to the start.
    private double GetHLVIndex(bool smooth)
    {
        var lum = GetLuminosity();
        GetHSV(out var h, out _, out var v);

        // Hue arrives in degrees, luminosity and value are already normalized.
        var hBand = Band(h / MAX_DEGREES);

        if (smooth && (hBand & 1) is 1)
        {
            lum = 1f - lum;
            v = 1f - v;
        }

        return Pack(hBand, Band(lum), Band(v), lum);
    }

    // As GetHLVIndex, but walking the hue wheel from the opposite side: the hue is rotated half a
    // turn and reversed, so the bands run in the opposite order.
    private double GetHLVInvertedIndex(bool smooth)
    {
        var lum = GetLuminosity();
        GetHSV(out var h, out _, out var v);

        var hBand = Band(1f - (Rotate(h, 180) / MAX_DEGREES));

        if (smooth && (hBand & 1) is 1)
        {
            lum = 1f - lum;
            v = 1f - v;
        }

        return Pack(hBand, Band(lum), Band(v), lum);
    }

    // Combines hue, saturation, and value into a single value for sorting. Hue is weighted most
    // heavily, followed by saturation and then value.
    private double GetHSVIndex()
    {
        GetHSV(out var h, out var s, out var v);

        // Only the hue needs normalizing here: GetHSV already returns s and v in [0,1].
        // Luminosity breaks ties rather than v, which takes only 256 values and so would leave
        // most colors sharing a key.
        return Pack(Band(h / MAX_DEGREES), Band(s), Band(v), GetLuminosity());
    }

    // Places the color in a hue band, then orders it inside that band by its distance along a
    // Hilbert curve through OKLAB. The band keeps the overall sequence sweeping the hue wheel once,
    // which is what makes a sorted set readable, while the curve keeps successive colors within a
    // band close in lightness and chroma at the same time rather than in one component only.
    private double GetHueHilbertIndex()
    {
        GetMinMax(out var min, out var max);

        var band = Band(Hue(min, max) / MAX_DEGREES);

        GetOKLAB(out var l, out var a, out var b);

        var distance = Hilbert(
            QuantizeOKLAB(l, OKLAB_L_MIN, OKLAB_L_MAX),
            QuantizeOKLAB(a, OKLAB_A_MIN, OKLAB_A_MAX),
            QuantizeOKLAB(b, OKLAB_B_MIN, OKLAB_B_MAX));

        // The curve distance occupies the fraction below the band, so the band always dominates.
        var position = band + (distance / (double)(1u << (HILBERT_BITS * 3)));

        // Scaled by CFACTOR squared so the result spans the same range the other indexes produce.
        return position * CFACTOR * CFACTOR;
    }

    // Maps an OKLAB coordinate onto the lattice the Hilbert curve is defined over.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint QuantizeOKLAB(float value, float min, float max)
    {
        const uint STEPS = (1u << HILBERT_BITS) - 1;

        var scaled = (value - min) / (max - min) * STEPS;

        if (scaled <= 0f)
            return 0;

        if (scaled >= STEPS)
            return STEPS;

        return (uint)(scaled + 0.5f);
    }

    // Distance along a 3D Hilbert curve, by Skilling's transform.
    // https://doi.org/10.1063/1.1751381
    // The per-bit tests are folded into masks rather than branches: they depend on the color being
    // indexed, so as branches they mispredict on roughly every other iteration.
    private static uint Hilbert(uint x, uint y, uint z)
    {
        uint t;

        // Inverse undo.
        for (var k = HILBERT_BITS - 1; k >= 1; k--)
        {
            var p = (1u << k) - 1;

            // A mask of all ones when the bit is set, all zeros when it is not. Where the bit is
            // set the low axis is inverted, otherwise the two axes exchange their low bits.
            var mask = 0u - ((x >> k) & 1u);
            x ^= p & mask;

            mask = 0u - ((y >> k) & 1u);
            x ^= p & mask;
            t = (x ^ y) & p & ~mask;
            x ^= t;
            y ^= t;

            mask = 0u - ((z >> k) & 1u);
            x ^= p & mask;
            t = (x ^ z) & p & ~mask;
            x ^= t;
            z ^= t;
        }

        // Gray encode.
        y ^= x;
        z ^= y;

        // The correction the transform folds back into all three axes is the XOR of (2^k - 1) over
        // every set bit k of z, which sets bit j exactly when an odd number of bits above j are set.
        // That is a suffix parity, so the whole bit-serial accumulation collapses into a halving
        // chain of shifts. Valid while HILBERT_BITS stays within the 16 bits the last shift covers.
        t = z >> 1;
        t ^= t >> 1;
        t ^= t >> 2;
        t ^= t >> 4;
        t ^= t >> 8;

        x ^= t;
        y ^= t;
        z ^= t;

        // Interleave the transpose into a single distance along the curve. Bit j of each axis lands
        // at 3j (+1, +2), which is the same spread ZCurve performs, so the three axes are spread
        // independently and merged rather than shifted in one bit at a time down a serial chain.
        return ZCurve(z) | (ZCurve(y) << 1) | (ZCurve(x) << 2);
    }

    #endregion

    /// <summary>
    ///     Compares two <see cref="Composite"/> values for equality by comparing their inner <see cref="Value"/>.
    /// </summary>
    public static bool operator ==(Composite left, Composite right)
        => left.Equals(right);

    /// <summary>
    ///     Compares two <see cref="Composite"/> values for non-equality by comparing their inner <see cref="Value"/>.
    /// </summary>
    public static bool operator !=(Composite left, Composite right)
        => !left.Equals(right);

    /// <summary>
    ///     Converts a <see cref="Composite"/> value to its 32-bit unsigned sRGB (A) representation, identical to <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    ///     The most significant byte holds the A (alpha) channel, followed by the R (red), G (green) and B (blue) channels in that order.
    ///     This is the exact inverse of <see cref="op_Implicit(uint)"/>.
    /// </remarks>
    public static implicit operator uint(Composite color)
        => color.Value;

    /// <summary>
    ///     Converts a 32-bit unsigned integer representation of sRGB (A) to a <see cref="Composite"/> value by interpreting the most significant byte as the A (alpha) channel, followed by the R (red), G (green), and B (blue) channels in that order.
    /// </summary>
    public static implicit operator Composite(uint argb)
        => new(argb);
}