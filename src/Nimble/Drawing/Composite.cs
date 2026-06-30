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
public unsafe readonly partial struct Composite : IEquatable<Color>, IEquatable<Composite>, IComparable<Composite>, ICloneable
{
    #region Constants
    
    const float
        REC_709_R = 0.2126f,
        REC_709_G = 0.7152f,
        REC_709_B = 0.0722f;

    const float
        BT_601_R = 0.299f,
        BT_601_G = 0.587f,
        BT_601_B = 0.114f;

    const float
        SRGBLINEAR_THRESHOLD = 0.04045f,
        GAMMA_2_2_THRESHOLD = 0.0031308f;

    const float
        LINEAR_UPPERFACTOR = 12.92f,
        LINEAR_INNERCURVE = 0.055f,
        LINEAR_LOWERFACTOR = 1.055f,
        LINEAR_GAMMACOEFFICIENT = 2.4f;

    const float
        CIE_LSTAR_THRESHOLD = 216f / 24389f,
        CIE_LSTAR_UPPERMUL = 24389f / 27f,
        CIE_LSTAR_CUBEROOT_FACTOR = 1f / 3f,
        CIE_LSTAR_OFFSET = 16f;

    const int
        ZCURVE_SHIFT12 = 00014000377,
        ZCURVE_SHIFT08 = 00014170017,
        ZCURVE_SHIFT04 = 00303030303,
        ZCURVE_SHIFT02 = 01111111111;

    const int
        CFACTOR = 8,
        MAX_DEGREES = 360;

    #endregion

#if NET6_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Value")]
    private static extern ref long GetValue(Color @this);
#endif

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemColor
    {
        /// <summary>
        ///     string? System.Drawing.Color.name. Because its a string, it can be set to 0 in an nint to fit in the same memory.
        /// </summary>
        //[FieldOffset(0)]
        public nint Name;

        /// <summary>
        ///     long? System.Drawing.Color.value. This is the value that has equality to Colr.Value.
        /// </summary>
        //[FieldOffset(8)]
        public long Value;

        /// <summary>
        ///     short System.Drawing.Color.knownColor, but it can be set to 0 in a short to fit in the same memory.
        /// </summary>
        //[FieldOffset(16)]
        public short KnownColor;

        /// <summary>
        ///     short System.Drawing.Color.state. This is the value that has to be set to 0x0002 for the Color to be valid when using the Value field.
        /// </summary>
        //[FieldOffset(18)]
        public short State;
    }

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
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    #region Internal Constructors

    private Composite(float h, float s, float v)
    {
        A = byte.MaxValue;

        var hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
        var f = h / 60 - Math.Floor(h / 60);

        v *= byte.MaxValue;

        var b = Convert.ToByte(v);
        var p = Convert.ToByte(v * (1 - s));
        var q = Convert.ToByte(v * (1 - f * s));
        var t = Convert.ToByte(v * (1 - (1 - f) * s));

        switch (hi)
        {
            case 0:
                R = b; G = t; B = p;
                break;
            case 1:
                R = q; G = b; B = p;
                break;
            case 2:
                R = p; G = b; B = t;
                break;
            case 3:
                R = p; G = q; B = b;
                break;
            case 4:
                R = t; G = p; B = b;
                break;
            default:
                R = b; G = p; B = q;
                break;
        }
    }

    private Composite(float h, float s, float l, float a = 1f)
    {
        A = (byte)(a * byte.MaxValue);

        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = l - c / 2;

        byte r, g, b;

        if (h < 60)
        {
            r = (byte)(c * byte.MaxValue);
            g = (byte)(x * byte.MaxValue);
            b = 0;
        }
        else if (h < 120)
        {
            r = (byte)(x * byte.MaxValue);
            g = (byte)(c * byte.MaxValue);
            b = 0;
        }
        else if (h < 180)
        {
            r = 0;
            g = (byte)(c * byte.MaxValue);
            b = (byte)(x * byte.MaxValue);
        }
        else if (h < 240)
        {
            r = 0;
            g = (byte)(x * byte.MaxValue);
            b = (byte)(c * byte.MaxValue);
        }
        else if (h < 300)
        {
            r = (byte)(x * byte.MaxValue);
            g = 0;
            b = (byte)(c * byte.MaxValue);
        }
        else
        {
            r = (byte)(c * byte.MaxValue);
            g = 0;
            b = (byte)(x * byte.MaxValue);
        }

        R = (byte)(r + m * byte.MaxValue);
        G = (byte)(g + m * byte.MaxValue);
        B = (byte)(b + m * byte.MaxValue);
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
    /// <returns>Relative luminance in accordance to Rec. 709 coefficients.</returns>
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
    /// <returns>Perceived brightness in accordance to BT.601 coefficients.</returns>
    public float GetPerceivedBrightness()
        => (float)Math.Sqrt(
            BT_601_R * Math.Pow(R, 2) +
            BT_601_G * Math.Pow(G, 2) +
            BT_601_B * Math.Pow(B, 2)
        );

    /// <summary>
    ///     Gets the wavelength of the color based on its hue, 
    ///     mapping the hue range (0-360) to a wavelength range (400-700 nm) in the visible spectrum.
    /// </summary>
    /// <returns>The combined wavelength of the color in the visible spectrum.</returns>
    public float GetCombinedWavelength()
        => 400 / 270 * GetHue();

    /// <summary>
    ///     Gets the gamma-corrected (TRC) luminance of the color using BT.601 coefficients.
    /// </summary>
    /// <returns>The gamma-corrected luminance of the color.</returns>
    public float GetTransferCurve()
        => BT_601_R * Gamma(R)
         + BT_601_G * Gamma(G)
         + BT_601_B * Gamma(B);

    /// <summary>
    ///     Gets a Z-order value for the color by interleaving the bits of the RGB channels, 
    ///     effectively creating a 30-bit integer that can be used for spatial sorting of colors in a 3D RGB space.
    /// </summary>
    /// <returns>The Z-order value for this color.</returns>
    public int GetZValue()
        => ZCurve(R)
         + (ZCurve(G) << 1)
         + (ZCurve(B) << 2);

    /// <summary>
    ///     Gets the hue of the color between 0 and 360 degrees.
    /// </summary>
    /// <returns>The hue of the current color.</returns>
    public float GetHue()
    {
        if (IsRGBEquals())
            return 0f;

        GetMinMax(out var min, out var max);

        float delta = max - min;
        float hue;

        int r, g, b;

        r = R;
        g = G;
        b = B;

        if (r == max)
            hue = (g - b) / delta;
        else if (g == max)
            hue = (b - r) / delta + 2f;
        else
            hue = (r - g) / delta + 4f;

        hue *= 60f;
        if (hue < 0f)
            hue += 360f;

        return hue;
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
        if (IsRGBEquals())
            return 0f;

        GetMinMax(out var min, out var max);

        var div = max + min;

        if (div > byte.MaxValue)
            div = byte.MaxValue * 2 - max - min;

        return (max - min) / (float)div;
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

        return (max + min) / (byte.MaxValue * 2f);
    }

    /// <summary>
    ///     Gets the contrast ratio between this color and another color according to the WCAG guidelines.
    /// </summary>
    /// <param name="o">The color to compare to to define the contrast.</param>
    /// <returns>The contrast ratio between the two colors, where a higher value indicates greater contrast.</returns>
    public double GetContrastRatio(Composite o)
    {
        var l1 = GetRelativeLuminance();

        var l2 = o.GetRelativeLuminance();

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

        return Math.Sqrt(Math.Pow(deltaR, 2) + Math.Pow(deltaG, 2) + Math.Pow(deltaB, 2));
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

        return Math.Sqrt(Math.Pow(deltaL, 2) + Math.Pow(deltaA, 2) + Math.Pow(deltaB, 2));
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
        return new(
            (byte)(Clamp(Gamma(R), 0, 1) * byte.MaxValue),
            (byte)(Clamp(Gamma(G), 0, 1) * byte.MaxValue),
            (byte)(Clamp(Gamma(B), 0, 1) * byte.MaxValue),
            A
        );
    }

    /// <summary>
    ///     Gets a composite index based on the provided index type for perceptual algorithmic sorting.
    /// </summary>
    /// <param name="indexType">The type of index to generate for this value.</param>
    /// <returns>A value representing a floating point (composite) index for the current value produced according to <paramref name="indexType"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided type is not a named value of <see cref="CompositeIndex"/>.</exception>
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
            _ => throw new ArgumentException("Invalid index type.", nameof(indexType)),
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
        => (GetHue(), GetSaturation(), GetBrightness());

    /// <summary>
    ///     Gets the HSLA color space representation of the current value as H, S, L, A.
    /// </summary>
    /// <returns>A <see cref="ValueTuple{T1, T2, T3, T4}"/> containing H, S, L, A.</returns>
    public (float H, float S, float L, float A) GetHSLA()
        => (GetHue(), GetSaturation(), GetBrightness(), A / 255f);

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
    ///     Takes the current saturation value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified saturation value. The resulting saturation value is wrapped around the 0-1 range.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is less than -1 or more than 1.</exception>
    public Composite ShiftSaturation(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(amount, -1f, 1f, nameof(amount));

        var hsla = GetHSLA();
        hsla.S = (hsla.S + amount) % 1f;

        if (hsla.S < 0)
            hsla.S += 1f;
        else if (hsla.S > 1f)
            hsla.S -= 1f;

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current saturation value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified saturation value. The resulting saturation value is wrapped around the 0-1 range.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than -1 or more than 1.</exception>
    public Composite SetSaturation(float value)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(value, -1f, 1f, nameof(value));

        var hsla = GetHSLA();
        hsla.S = value % 1f;

        if (hsla.S < 0)
            hsla.S += 1f;
        else if (hsla.S > 1f)
            hsla.S -= 1f;

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current brightness value and adds or removes the specified amount to it, returning a new <see cref="Composite"/> with the modified lightness value. The resulting lightness value is wrapped around the 0-1 range.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is less than -1 or more than 1.</exception>
    public Composite ShiftBrightness(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(amount, -1f, 1f, nameof(amount));

        var hsla = GetHSLA();
        hsla.L = (hsla.L + amount) % 1f;

        if (hsla.L < 0)
            hsla.L += 1f;
        else if (hsla.L > 1f)
            hsla.L -= 1f;

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Takes the current brightness value and sets it to the specified value, returning a new <see cref="Composite"/> with the modified lightness value. The resulting lightness value is wrapped around the 0-1 range.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A new <see cref="Composite"/> value with the included mutation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than -1 or more than 1.</exception>
    public Composite SetBrightness(float value)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(value, -1f, 1f, nameof(value));

        var hsla = GetHSLA();
        hsla.L = value % 1f;

        if (hsla.L < 0)
            hsla.L += 1f;
        else if (hsla.L > 1f)
            hsla.L -= 1f;

        return new Composite(hsla.H, hsla.S, hsla.L, hsla.A);
    }

    /// <summary>
    ///     Creates a <see cref="Color"/> from the current <see cref="Composite"/> instance.
    /// </summary>
    /// <returns>A new <see cref="Color"/> created from the sRGB value of this <see cref="Composite"/>.</returns>
    public Color ToColor()
    {
        var systemColor = new SystemColor
        {
            Value = Value,
            State = 0x0002 // Refer to System.Drawing.Color.StateARGBValueValid.
        };

        return *(Color*)&systemColor;
    }

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
        => ToString(ColorFormat.RGBA);

    /// <summary>
    ///     Gets a web-format string representation of the current value in the chosen format.
    /// </summary>
    /// <param name="format">The target format for the current value.</param>
    /// <returns>A string representing web-format of the current value.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided format is not a named value of <see cref="ColorFormat"/>.</exception>
    public string ToString(ColorFormat format)
    {
        switch (format)
        {
            case ColorFormat.RGB:
                return $"rgb({R}, {G}, {B})";
            case ColorFormat.RGBA:
                return $"rgba({R}, {G}, {B}, {A})";
            case ColorFormat.HSL:
                {
                    var (h, s, l) = GetHSL();

                    return $"hsl({h}, {s}, {l})";
                }
            case ColorFormat.HSLA:
                {
                    var (h, s, l, a) = GetHSLA();

                    return $"hsla({h}, {s}, {l}, {a}";
                }
            case ColorFormat.HSV:
                {
                    var (h, s, v) = GetHSV();

                    return $"hsv({h}, {s}, {v})";
                }
            case ColorFormat.CIEXYZ:
                {
                    var (x, y, z) = GetXYZ();

                    return $"xyz({x}, {y}, {z})";
                }
            case ColorFormat.CIELAB:
                {
                    var (l, a, b) = GetCIELAB();

                    return $"cielab({l}, {a}, {b})";
                }
            case ColorFormat.OKLAB:
                {
                    var (l, a, b) = GetOKLAB();
                    return $"oklab({l}, {a}, {b})";
                }
                case ColorFormat.OKLCH:
                {
                    var (l, c, h) = GetOKLCH();
                    return $"oklch({l}, {c}, {h})";
                }
            case ColorFormat.HEX:
                {
                    return $"#{GetOrderedRGBA():X8}";
                }
            default:
                throw new ArgumentException("Invalid color format", nameof(format));
        }
    }

    object ICloneable.Clone()
        => new Composite(Value);
    
    #region Optimization

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsRGBEquals()
        => R == G && G == B;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetXYZ(out float x, out float y, out float z)
        => CIEXYZ(VLinear(R), VLinear(G), VLinear(B), out x, out y, out z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetOKLCH(out float l, out float c, out float h)
    {
        GetOKLAB(out var lS, out var aS, out var bS);

        l = lS;
        c = (float)Math.Sqrt(aS * aS + bS * bS);
        h = (float)(Math.Atan2(bS, aS) * (180 / Math.PI));

        if (h < 0)
            h += 360f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetOKLAB(out float l, out float a, out float b)
    {
        GetXYZ(out var x, out var y, out var z);

        // https://bottosson.github.io/posts/oklab/#converting-from-xyz-to-oklab
        var lS = 0.8189330101f * x + 0.3618667424f * y - 0.1288597137f * z;
        var mS = 0.0329845436f * x + 0.9293118715f * y + 0.0361456387f * z;
        var sS = 0.0482003018f * x + 0.2643662691f * y + 0.6338517070f * z;

        var lS3 = lS * lS * lS;
        var mS3 = mS * mS * mS;
        var sS3 = sS * sS * sS;

        l = (lS3 * 0.2104542553f) + (mS3 * 0.7936177850f) - (sS3 * 0.0040720468f);
        a = (lS3 * 1.9779984951f) - (mS3 * 2.4285922050f) + (sS3 * 0.4505937099f);
        b = (lS3 * 0.0259040371f) + (mS3 * 0.7827717662f) - (sS3 * 0.8086757660f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetCIELAB(out float l, out float a, out float b)
    {
        GetXYZ(out var x, out var y, out var z);

        var xS = LStar(x);
        var yS = LStar(y);
        var zS = LStar(z);

        l = yS;
        a = 500f * (xS - yS);
        b = 200f * (yS - zS);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetHSV(out float h, out float s, out float v)
    {
        GetMinMax(out var min, out var max);

        h = GetHue();
        s = (max == 0) ? 0f : 1f - (1f * min / max);
        v = max / 255f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetOrderedRGBA()
    {
        var r = (uint)R;
        var g = (uint)G << 8;
        var b = (uint)B << 16;
        var a = (uint)A << 24;

        return r | g | b | a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ShiftChannel(byte oldValue, int shift)
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
    private float Clamp(float value, float minValue, float maxValue)
    {
        if (value < minValue)
            return minValue;
        else if (value > maxValue)
            return maxValue;
        
        return value;
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
        x = (rL * 0.4124564f) + (gL * 0.2126729f) + (bL * 0.0193339f);
        y = (rL * 0.2126729f) + (gL * 0.7151522f) + (bL * 0.0721750f);
        z = (rL * 0.0193339f) + (gL * 0.0721750f) + (bL * 0.9503041f);
    }

    // Gets gamma-corrected value from linear RGB channel.
    // C is in range [0,1].
    // Returns gamma-corrected value in range [0,1].
    private static float Gamma(float L)
    {
        if (L <= GAMMA_2_2_THRESHOLD)
            return L * LINEAR_UPPERFACTOR;

        return (LINEAR_LOWERFACTOR * (float)Math.Pow(L, 1 / LINEAR_GAMMACOEFFICIENT)) - LINEAR_INNERCURVE;
    }

    // Gets linearized value from sRGB channel.
    // C is in range [0,255]
    // Returns linear value in range [0,1]S
    private static float VLinear(float C)
    {
        var v = C / 255f;

        if (v <= SRGBLINEAR_THRESHOLD)
            return v / LINEAR_UPPERFACTOR;

        return (float)Math.Pow((v + LINEAR_INNERCURVE) / LINEAR_LOWERFACTOR, LINEAR_GAMMACOEFFICIENT);
    }

    // Gets L* value from luminosity, assuming D65 illuminant and standard observer.
    // Y is in range [0,1].
    // Returns L* in range [0,100].
    private static float LStar(float Y)
    {
        if (Y <= CIE_LSTAR_THRESHOLD)
            return Y * CIE_LSTAR_UPPERMUL;

        return ((float)Math.Pow(Y, CIE_LSTAR_CUBEROOT_FACTOR) * 116f) - CIE_LSTAR_OFFSET;
    }

    // Splits bits of a byte into a 30-bit integer for Z-order curve calculation.
    // Only the lowest 10 bits are used, so input should be in range [0,255].
    private static int ZCurve(int a)
    {
        // split out the lowest 10 bits to lowest 30 bits
        a = (a | (a << 12)) & ZCURVE_SHIFT12;
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

    // Combines hue, luminosity, and brightness into a single value for sorting. Hue is weighted most heavily, followed by luminosity and then brightness.
    // If smooth is true, the luminosity and brightness values are inverted for odd hue values to create a smoother gradient when sorting.
    private double GetHLVIndex(bool smooth)
    {
        var lum = GetLuminosity();
        GetHSV(out var h, out _, out var v);

        h *= CFACTOR;
        lum *= CFACTOR;
        v *= CFACTOR;

        if (smooth && h % 2 is 1)
        {
            v = CFACTOR - v;
            lum = CFACTOR - lum;
        }

        return h + lum + v;
    }

    // Combines inverted hue, luminosity, and brightness into a single value for sorting. Inverted hue is weighted most heavily, followed by luminosity and then brightness.
    private double GetHLVInvertedIndex(bool smooth)
    {
        var lum = GetLuminosity();
        var hue = 1 - Rotate(GetHue(), 180) / 360;
        var brightness = GetBrightness();

        var h2 = hue * CFACTOR;
        var v2 = brightness * CFACTOR;

        if (smooth)
        {
            if (h2 % 2 is 0)
                v2 = CFACTOR - v2;
            else
                lum = CFACTOR - lum;
        }

        return h2 + v2 + lum;
    }

    // Combines hue, saturation, and value into a single value for sorting. Hue is weighted most heavily, followed by saturation and then value.
    private double GetHSVIndex()
    {
        GetHSV(out var h, out var s, out var v);

        h /= 360f;
        v /= 255f;

        // combine the HSV values into a single value for sorting
        return (h * CFACTOR) + (s * CFACTOR) + (v * CFACTOR);
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
    ///     Converts a <see cref="Composite"/> value to a 32-bit unsigned integer representation of RGBA.
    /// </summary>
    public static implicit operator uint(Composite color)
        => (uint)(color.R | (color.G << 8) | (color.B << 16) | (color.A << 24));

    /// <summary>
    ///     Converts a 32-bit unsigned integer representation of RGBA to a <see cref="Composite"/> value by interpreting the least significant byte as the A (alpha) channel, followed by the B (blue), G (green), and R (red) channels in that order.
    /// </summary>
    public static implicit operator Composite(uint rgba)
        => new(rgba);
}