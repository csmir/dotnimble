#if !NETSTANDARD2_0

using System.Numerics;
using System.Runtime.InteropServices;
using Nimble.Buffers;

namespace Nimble.Drawing;

public readonly partial struct Composite
{
    #region Conversions

    /// <summary>
    ///     Linearizes a span of colors, undoing the sRGB transfer function on each channel.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays: one span per channel rather than one interleaved span.
    ///     This is the layout every other bulk conversion on this type produces, and the layout a vectorized implementation can fill a whole register from at a time.
    ///     The alpha channel is not linearized and is not written; read it from <paramref name="source"/> where it is needed.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="r">Receives the linearized red channel, in the 0-1 range.</param>
    /// <param name="g">Receives the linearized green channel, in the 0-1 range.</param>
    /// <param name="b">Receives the linearized blue channel, in the 0-1 range.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToLinear(ReadOnlySpan<Composite> source, Span<float> r, Span<float> g, Span<float> b)
    {
        Fit(source.Length, r, nameof(r));
        Fit(source.Length, g, nameof(g));
        Fit(source.Length, b, nameof(b));

        // Slicing every destination to the source length up front lets the bounds checks fall out of
        // the loop, rather than being re-proven against three separate lengths on every element.
        r = r[..source.Length];
        g = g[..source.Length];
        b = b[..source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];

            r[i] = VLinear(c.R);
            g[i] = VLinear(c.G);
            b[i] = VLinear(c.B);
        }
    }

    /// <summary>
    ///     Gets the Rec. 709 luminosity of a span of colors over linearized RGB values, as <see cref="GetLuminosity"/> does for a single color.
    /// </summary>
    /// <param name="source">The colors to measure.</param>
    /// <param name="destination">Receives the luminosity of each color, in the 0-1 range.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is shorter than <paramref name="source"/>.</exception>
    public static void ToLuminosity(ReadOnlySpan<Composite> source, Span<float> destination)
    {
        Fit(source.Length, destination, nameof(destination));

        destination = destination[..source.Length];

        for (var i = 0; i < source.Length; i++)
            destination[i] = source[i].GetLuminosity();
    }

    /// <summary>
    ///     Converts a span of colors to the CIE-XYZ color space, as <see cref="GetXYZ()"/> does for a single color.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays, one span per component.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="x">Receives the X component of each color.</param>
    /// <param name="y">Receives the Y component of each color.</param>
    /// <param name="z">Receives the Z component of each color.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToXYZ(ReadOnlySpan<Composite> source, Span<float> x, Span<float> y, Span<float> z)
    {
        Fit(source.Length, x, nameof(x));
        Fit(source.Length, y, nameof(y));
        Fit(source.Length, z, nameof(z));

        x = x[..source.Length];
        y = y[..source.Length];
        z = z[..source.Length];

        var i = 0;

        if (Vector.IsHardwareAccelerated && source.Length >= Vector<float>.Count)
            i = XYZCore(source, x, y, z);

        for (; i < source.Length; i++)
        {
            source[i].GetXYZ(out var cx, out var cy, out var cz);

            x[i] = cx;
            y[i] = cy;
            z[i] = cz;
        }
    }

    /// <summary>
    ///     Converts a span of colors to the CIE-LAB color space, as <see cref="GetCIELAB()"/> does for a single color.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays, one span per component.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="l">Receives the L* component of each color, in the 0-100 range.</param>
    /// <param name="a">Receives the a* component of each color.</param>
    /// <param name="b">Receives the b* component of each color.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToCIELAB(ReadOnlySpan<Composite> source, Span<float> l, Span<float> a, Span<float> b)
    {
        Fit(source.Length, l, nameof(l));
        Fit(source.Length, a, nameof(a));
        Fit(source.Length, b, nameof(b));

        l = l[..source.Length];
        a = a[..source.Length];
        b = b[..source.Length];

        var i = 0;

        if (Vector.IsHardwareAccelerated && source.Length >= Vector<float>.Count)
            i = CIELABCore(source, l, a, b);

        for (; i < source.Length; i++)
        {
            source[i].GetCIELAB(out var cl, out var ca, out var cb);

            l[i] = cl;
            a[i] = ca;
            b[i] = cb;
        }
    }

    /// <summary>
    ///     Converts a span of colors to the OKLAB color space, as <see cref="GetOKLAB()"/> does for a single color.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays, one span per component.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="l">Receives the L component of each color, in the 0-1 range.</param>
    /// <param name="a">Receives the a component of each color.</param>
    /// <param name="b">Receives the b component of each color.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToOKLAB(ReadOnlySpan<Composite> source, Span<float> l, Span<float> a, Span<float> b)
    {
        Fit(source.Length, l, nameof(l));
        Fit(source.Length, a, nameof(a));
        Fit(source.Length, b, nameof(b));

        l = l[..source.Length];
        a = a[..source.Length];
        b = b[..source.Length];

        var i = 0;

        if (Vector.IsHardwareAccelerated && source.Length >= Vector<float>.Count)
            i = OKLABCore(source, l, a, b);

        // The tail, and every element when the platform has no vector unit at all.
        for (; i < source.Length; i++)
        {
            source[i].GetOKLAB(out var cl, out var ca, out var cb);

            l[i] = cl;
            a[i] = ca;
            b[i] = cb;
        }
    }

    /// <summary>
    ///     Converts a span of colors to the OKLCH color space, as <see cref="GetOKLCH()"/> does for a single color.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays, one span per component.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="l">Receives the lightness of each color, in the 0-1 range.</param>
    /// <param name="c">Receives the chroma of each color.</param>
    /// <param name="h">Receives the hue of each color, in the 0-360 degree range.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToOKLCH(ReadOnlySpan<Composite> source, Span<float> l, Span<float> c, Span<float> h)
    {
        Fit(source.Length, l, nameof(l));
        Fit(source.Length, c, nameof(c));
        Fit(source.Length, h, nameof(h));

        l = l[..source.Length];
        c = c[..source.Length];
        h = h[..source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            source[i].GetOKLCH(out var cl, out var cc, out var ch);

            l[i] = cl;
            c[i] = cc;
            h[i] = ch;
        }
    }

    /// <summary>
    ///     Converts a span of colors to the HSL color space, as <see cref="GetHSL"/> does for a single color.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays, one span per component.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="h">Receives the hue of each color, in the 0-360 degree range.</param>
    /// <param name="s">Receives the saturation of each color, in the 0-1 range.</param>
    /// <param name="l">Receives the lightness of each color, in the 0-1 range.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToHSL(ReadOnlySpan<Composite> source, Span<float> h, Span<float> s, Span<float> l)
    {
        Fit(source.Length, h, nameof(h));
        Fit(source.Length, s, nameof(s));
        Fit(source.Length, l, nameof(l));

        h = h[..source.Length];
        s = s[..source.Length];
        l = l[..source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            var color = source[i];

            // All three components fall out of the same min/max pair, so it is resolved once here
            // instead of three times over through the single-color accessors.
            color.GetMinMax(out var min, out var max);

            h[i] = color.Hue(min, max);
            s[i] = Saturation(min, max);
            l[i] = Brightness(min, max);
        }
    }

    /// <summary>
    ///     Converts a span of colors to the HSV color space, as <see cref="GetHSV()"/> does for a single color.
    /// </summary>
    /// <remarks>
    ///     Results are written as a structure of arrays, one span per component.
    ///     Note that <paramref name="s"/> is HSV saturation, which is not the same measure as the HSL saturation <see cref="ToHSL"/> produces.
    /// </remarks>
    /// <param name="source">The colors to convert.</param>
    /// <param name="h">Receives the hue of each color, in the 0-360 degree range.</param>
    /// <param name="s">Receives the saturation of each color, in the 0-1 range.</param>
    /// <param name="v">Receives the value (brightness) of each color, in the 0-1 range.</param>
    /// <exception cref="ArgumentException">Thrown when any destination is shorter than <paramref name="source"/>.</exception>
    public static void ToHSV(ReadOnlySpan<Composite> source, Span<float> h, Span<float> s, Span<float> v)
    {
        Fit(source.Length, h, nameof(h));
        Fit(source.Length, s, nameof(s));
        Fit(source.Length, v, nameof(v));

        h = h[..source.Length];
        s = s[..source.Length];
        v = v[..source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            source[i].GetHSV(out var ch, out var cs, out var cv);

            h[i] = ch;
            s[i] = cs;
            v[i] = cv;
        }
    }

    #endregion

    #region Sorting

    /// <summary>
    ///     Produces a composite sort index for a span of colors, as <see cref="GetIndex(CompositeIndex)"/> does for a single color.
    /// </summary>
    /// <param name="source">The colors to index.</param>
    /// <param name="indexType">The type of index to generate.</param>
    /// <param name="destination">Receives the index of each color.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is shorter than <paramref name="source"/>, or when <paramref name="indexType"/> is not a named value of <see cref="CompositeIndex"/>.</exception>
    public static void ToIndex(ReadOnlySpan<Composite> source, CompositeIndex indexType, Span<double> destination)
    {
        Fit(source.Length, destination, nameof(destination));

        destination = destination[..source.Length];

        // The index type is resolved once for the whole span. Dispatching per element would put a
        // branch on a value that cannot change between iterations into the middle of the loop.
        switch (indexType)
        {
            case CompositeIndex.HLV1D:
            case CompositeIndex.HLV2D:
                {
                    var smooth = indexType is CompositeIndex.HLV1D;

                    for (var i = 0; i < source.Length; i++)
                        destination[i] = source[i].GetHLVIndex(smooth);

                    return;
                }
            case CompositeIndex.HLV1DInverted:
            case CompositeIndex.HLV2DInverted:
                {
                    var smooth = indexType is CompositeIndex.HLV1DInverted;

                    for (var i = 0; i < source.Length; i++)
                        destination[i] = source[i].GetHLVInvertedIndex(smooth);

                    return;
                }
            case CompositeIndex.HSV:
                {
                    for (var i = 0; i < source.Length; i++)
                        destination[i] = source[i].GetHSVIndex();

                    return;
                }
            case CompositeIndex.HueHilbert:
                {
                    var i = 0;

                    if (Vector.IsHardwareAccelerated && source.Length >= Vector<float>.Count)
                        i = HueHilbertCore(source, destination);

                    for (; i < source.Length; i++)
                        destination[i] = source[i].GetHueHilbertIndex();

                    return;
                }
            default:
                throw new ArgumentException("Invalid index type.", nameof(indexType));
        }
    }

    /// <summary>
    ///     Sorts a span of colors in place by the perceptual index named by <paramref name="indexType"/>.
    /// </summary>
    /// <remarks>
    ///     Each index is computed exactly once and the span is then sorted against those keys.
    ///     Sorting through a comparer that calls <see cref="GetIndex(CompositeIndex)"/> would instead recompute an index on every comparison, which for a set of any size is the dominant cost.
    ///     The sort is not stable: colors sharing an index may be reordered relative to each other.
    /// </remarks>
    /// <param name="colors">The colors to sort in place.</param>
    /// <param name="indexType">The index to sort by. <see cref="CompositeIndex.HueHilbert"/> is the best default for an arbitrary set.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="indexType"/> is not a named value of <see cref="CompositeIndex"/>.</exception>
    public static void Sort(Span<Composite> colors, CompositeIndex indexType)
    {
        if (colors.Length < 2)
            return;

        using var rented = SafeArrayPool<double>.Shared.Rent(colors.Length);

        var keys = rented.Array.AsSpan(0, colors.Length);

        ToIndex(colors, indexType, keys);

        keys.Sort(colors);
    }

    #endregion

    #region Vectorization

    // Converts whole vectors of colors to OKLAB, and reports the index the caller has to resume the
    // scalar tail from.
    //
    // Vector<T> sizes itself to the widest unit the platform actually has, so this is eight colors
    // at a time on AVX2, four on NEON, and sixteen where AVX-512 is enabled, without a separate code
    // path per width. Measured within two percent of a hand written Vector256 version on AVX2.
    private static int OKLABCore(ReadOnlySpan<Composite> source, Span<float> l, Span<float> a, Span<float> b)
    {
        var width = Vector<float>.Count;

        Span<float> sr = stackalloc float[width];
        Span<float> sg = stackalloc float[width];
        Span<float> sb = stackalloc float[width];

        var i = 0;

        for (; i + width <= source.Length; i += width)
        {
            Linearize(source, i, sr, sg, sb, out var rL, out var gL, out var bL);

            OKLAB(rL, gL, bL, out var cl, out var ca, out var cb);

            cl.CopyTo(l[i..]);
            ca.CopyTo(a[i..]);
            cb.CopyTo(b[i..]);
        }

        return i;
    }

    // Converts whole vectors of colors to CIE-XYZ, and reports the index to resume the tail from.
    private static int XYZCore(ReadOnlySpan<Composite> source, Span<float> x, Span<float> y, Span<float> z)
    {
        var width = Vector<float>.Count;

        Span<float> sr = stackalloc float[width];
        Span<float> sg = stackalloc float[width];
        Span<float> sb = stackalloc float[width];

        var i = 0;

        for (; i + width <= source.Length; i += width)
        {
            Linearize(source, i, sr, sg, sb, out var rL, out var gL, out var bL);

            XYZ(rL, gL, bL, out var cx, out var cy, out var cz);

            cx.CopyTo(x[i..]);
            cy.CopyTo(y[i..]);
            cz.CopyTo(z[i..]);
        }

        return i;
    }

    // Converts whole vectors of colors to CIE-LAB, and reports the index to resume the tail from.
    private static int CIELABCore(ReadOnlySpan<Composite> source, Span<float> l, Span<float> a, Span<float> b)
    {
        var width = Vector<float>.Count;

        Span<float> sr = stackalloc float[width];
        Span<float> sg = stackalloc float[width];
        Span<float> sb = stackalloc float[width];

        var i = 0;

        for (; i + width <= source.Length; i += width)
        {
            Linearize(source, i, sr, sg, sb, out var rL, out var gL, out var bL);

            XYZ(rL, gL, bL, out var cx, out var cy, out var cz);

            var xS = LabF(cx / new Vector<float>(D65_XN));
            var yS = LabF(cy / new Vector<float>(D65_YN));
            var zS = LabF(cz / new Vector<float>(D65_ZN));

            ((new Vector<float>(116f) * yS) - new Vector<float>(CIE_LSTAR_OFFSET)).CopyTo(l[i..]);
            (new Vector<float>(500f) * (xS - yS)).CopyTo(a[i..]);
            (new Vector<float>(200f) * (yS - zS)).CopyTo(b[i..]);
        }

        return i;
    }

    // Produces the hue-banded Hilbert index for whole vectors of colors, and reports the index to
    // resume the tail from.
    //
    // This is the slowest of the indexes by a wide margin and the one worth sorting a palette by, so
    // it carries the most detail: a hue band from the raw channels, an OKLAB conversion, and the
    // Skilling transform over the quantized result. Only the final combine stays scalar, because the
    // index is a double and widening to half as many lanes for three arithmetic operations costs
    // more than it saves.
    private static int HueHilbertCore(ReadOnlySpan<Composite> source, Span<double> destination)
    {
        var width = Vector<float>.Count;

        Span<float> rawR = stackalloc float[width];
        Span<float> rawG = stackalloc float[width];
        Span<float> rawB = stackalloc float[width];

        Span<float> linR = stackalloc float[width];
        Span<float> linG = stackalloc float[width];
        Span<float> linB = stackalloc float[width];

        Span<int> bands = stackalloc int[width];
        Span<uint> distances = stackalloc uint[width];

        ref var lut = ref MemoryMarshal.GetArrayDataReference(VLINEAR_LUT);

        var i = 0;

        for (; i + width <= source.Length; i += width)
        {
            for (var k = 0; k < width; k++)
            {
                var c = source[i + k];

                rawR[k] = c.R;
                rawG[k] = c.G;
                rawB[k] = c.B;

                linR[k] = Unsafe.Add(ref lut, c.R);
                linG[k] = Unsafe.Add(ref lut, c.G);
                linB[k] = Unsafe.Add(ref lut, c.B);
            }

            var r = new Vector<float>(rawR);
            var g = new Vector<float>(rawG);
            var b = new Vector<float>(rawB);

            Band(Hue(r, g, b) / new Vector<float>(MAX_DEGREES)).CopyTo(bands);

            OKLAB(new Vector<float>(linR), new Vector<float>(linG), new Vector<float>(linB), out var l, out var a, out var bb);

            Hilbert(
                Quantize(l, OKLAB_L_MIN, OKLAB_L_MAX),
                Quantize(a, OKLAB_A_MIN, OKLAB_A_MAX),
                Quantize(bb, OKLAB_B_MIN, OKLAB_B_MAX)).CopyTo(distances);

            for (var k = 0; k < width; k++)
            {
                var position = bands[k] + (distances[k] / (double)(1u << (HILBERT_BITS * 3)));

                destination[i + k] = position * CFACTOR * CFACTOR;
            }
        }

        return i;
    }

    // Vectorized counterpart to GetMinMax followed by Hue, over raw channel values.
    //
    // The scalar version returns early for an achromatic color rather than dividing by a zero delta.
    // A lane cannot return early, so the division runs regardless and the infinity it produces is
    // discarded by the final select, which is bitwise and so is unbothered by it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Hue(Vector<float> r, Vector<float> g, Vector<float> b)
    {
        var max = Vector.Max(Vector.Max(r, g), b);
        var min = Vector.Min(Vector.Min(r, g), b);

        var delta = max - min;

        var hue = Vector.ConditionalSelect(
            Vector.Equals(r, max),
            (g - b) / delta,
            Vector.ConditionalSelect(
                Vector.Equals(g, max),
                ((b - r) / delta) + new Vector<float>(2f),
                ((r - g) / delta) + new Vector<float>(4f)));

        hue *= new Vector<float>(60f);

        hue = Vector.ConditionalSelect(Vector.LessThan(hue, Vector<float>.Zero), hue + new Vector<float>(MAX_DEGREES), hue);

        return Vector.ConditionalSelect(Vector.Equals(min, max), Vector<float>.Zero, hue);
    }

    // Vectorized counterpart to the scalar Band.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<int> Band(Vector<float> value)
    {
        var band = Vector.ConvertToInt32(value * new Vector<float>(CFACTOR));

        return Vector.Min(Vector.Max(band, Vector<int>.Zero), new Vector<int>(CFACTOR - 1));
    }

    // Vectorized counterpart to QuantizeOKLAB. Clamping in floating point before the conversion
    // reproduces the scalar guards exactly, and keeps a negative value away from the unsigned
    // conversion, which has no defined result for one.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<uint> Quantize(Vector<float> value, float min, float max)
    {
        const uint STEPS = (1u << HILBERT_BITS) - 1;

        var scaled = (value - new Vector<float>(min)) / new Vector<float>(max - min) * new Vector<float>(STEPS);

        scaled = Vector.Min(Vector.Max(scaled, Vector<float>.Zero), new Vector<float>(STEPS));

        return Vector.ConvertToUInt32(scaled + new Vector<float>(0.5f));
    }

    // Vectorized counterpart to the scalar Hilbert. Pure integer work, so given the same lattice
    // coordinates this produces bit-identical distances.
    private static Vector<uint> Hilbert(Vector<uint> x, Vector<uint> y, Vector<uint> z)
    {
        Vector<uint> t;

        for (var k = HILBERT_BITS - 1; k >= 1; k--)
        {
            var p = new Vector<uint>((1u << k) - 1);

            var mask = Ones(x, k);
            x ^= p & mask;

            mask = Ones(y, k);
            x ^= p & mask;
            t = (x ^ y) & p & Vector.OnesComplement(mask);
            x ^= t;
            y ^= t;

            mask = Ones(z, k);
            x ^= p & mask;
            t = (x ^ z) & p & Vector.OnesComplement(mask);
            x ^= t;
            z ^= t;
        }

        y ^= x;
        z ^= y;

        t = Vector<uint>.Zero;

        for (var k = HILBERT_BITS - 1; k >= 1; k--)
            t ^= new Vector<uint>((1u << k) - 1) & Ones(z, k);

        x ^= t;
        y ^= t;
        z ^= t;

        var distance = Vector<uint>.Zero;

        for (var bit = HILBERT_BITS - 1; bit >= 0; bit--)
        {
            distance = Vector.ShiftLeft(distance, 1) | (Vector.ShiftRightLogical(x, bit) & Vector<uint>.One);
            distance = Vector.ShiftLeft(distance, 1) | (Vector.ShiftRightLogical(y, bit) & Vector<uint>.One);
            distance = Vector.ShiftLeft(distance, 1) | (Vector.ShiftRightLogical(z, bit) & Vector<uint>.One);
        }

        return distance;
    }

    // All ones where the bit at k is set, all zeros where it is not.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<uint> Ones(Vector<uint> value, int k)
        => Vector<uint>.Zero - (Vector.ShiftRightLogical(value, k) & Vector<uint>.One);

    // Linear sRGB to OKLAB over whole vectors, in the order GetOKLAB applies it per color.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void OKLAB(Vector<float> rL, Vector<float> gL, Vector<float> bL, out Vector<float> l, out Vector<float> a, out Vector<float> b)
    {
        var lS = (new Vector<float>(0.4122214708f) * rL) + (new Vector<float>(0.5363325363f) * gL) + (new Vector<float>(0.0514459929f) * bL);
        var mS = (new Vector<float>(0.2119034982f) * rL) + (new Vector<float>(0.6806995451f) * gL) + (new Vector<float>(0.1073969566f) * bL);
        var sS = (new Vector<float>(0.0883024619f) * rL) + (new Vector<float>(0.2817188376f) * gL) + (new Vector<float>(0.6299787005f) * bL);

        var lC = Cbrt(lS);
        var mC = Cbrt(mS);
        var sC = Cbrt(sS);

        l = (lC * new Vector<float>(0.2104542553f)) + (mC * new Vector<float>(0.7936177850f)) - (sC * new Vector<float>(0.0040720468f));
        a = (lC * new Vector<float>(1.9779984951f)) - (mC * new Vector<float>(2.4285922050f)) + (sC * new Vector<float>(0.4505937099f));
        b = (lC * new Vector<float>(0.0259040371f)) + (mC * new Vector<float>(0.7827717662f)) - (sC * new Vector<float>(0.8086757660f));
    }

    // Linearizes one vector's worth of colors into three separate channel vectors.
    //
    // Pulling three channels out of packed BGRA has no single instruction form worth the shuffle
    // chain at these widths, so the transfer table is applied per lane into a scratch buffer that is
    // then loaded as a vector. The table is a kilobyte, so every one of those reads is an L1 hit.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Linearize(ReadOnlySpan<Composite> source, int offset, Span<float> sr, Span<float> sg, Span<float> sb, out Vector<float> r, out Vector<float> g, out Vector<float> b)
    {
        // The table is indexed by a byte against 256 entries, so the bound holds by construction;
        // taking a reference to it drops the check the indexer would otherwise emit per lane.
        ref var lut = ref MemoryMarshal.GetArrayDataReference(VLINEAR_LUT);

        for (var k = 0; k < sr.Length; k++)
        {
            var c = source[offset + k];

            sr[k] = Unsafe.Add(ref lut, c.R);
            sg[k] = Unsafe.Add(ref lut, c.G);
            sb[k] = Unsafe.Add(ref lut, c.B);
        }

        r = new Vector<float>(sr);
        g = new Vector<float>(sg);
        b = new Vector<float>(sb);
    }

    // Vectorized counterpart to CIEXYZ, over the same matrix in the same order.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void XYZ(Vector<float> rL, Vector<float> gL, Vector<float> bL, out Vector<float> x, out Vector<float> y, out Vector<float> z)
    {
        x = (rL * new Vector<float>(0.4124564f)) + (gL * new Vector<float>(0.3575761f)) + (bL * new Vector<float>(0.1804375f));
        y = (rL * new Vector<float>(0.2126729f)) + (gL * new Vector<float>(0.7151522f)) + (bL * new Vector<float>(0.0721750f));
        z = (rL * new Vector<float>(0.0193339f)) + (gL * new Vector<float>(0.1191920f)) + (bL * new Vector<float>(0.9503041f));
    }

    // Vectorized counterpart to the scalar LabF. Both sides of the piecewise curve are evaluated and
    // then selected between, since a branch cannot be taken per lane.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> LabF(Vector<float> t)
    {
        var upper = Cbrt(t);
        var lower = ((new Vector<float>(CIE_LSTAR_UPPERMUL) * t) + new Vector<float>(CIE_LSTAR_OFFSET)) / new Vector<float>(116f);

        return Vector.ConditionalSelect(Vector.GreaterThan(t, new Vector<float>(CIE_LSTAR_THRESHOLD)), upper, lower);
    }

    // Vectorized counterpart to the scalar Cbrt, from the same seed and the same three Newton steps.
    //
    // The seed's division by three runs in floating point rather than over the integer bit pattern:
    // no hardware has a SIMD integer divide, so Vector<int> division is emulated a lane at a time and
    // measured barely faster than not vectorizing at all. Rounding the exponent through a float costs
    // the seed a few ulps, which Newton reconverges from within the same three steps.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Cbrt(Vector<float> value)
    {
        var third = Vector.ConvertToInt32(Vector.ConvertToSingle(Vector.AsVectorInt32(value)) * new Vector<float>(1f / 3f));

        var y = Vector.AsVectorSingle(third + new Vector<int>(CBRT_SEED));

        var twoThirds = new Vector<float>(2f / 3f);
        var three = new Vector<float>(3f);

        y = (twoThirds * y) + (value / (three * y * y));
        y = (twoThirds * y) + (value / (three * y * y));
        y = (twoThirds * y) + (value / (three * y * y));

        // Mirrors the scalar guard: a zero or denormal input has to come back out as zero, rather
        // than as the small positive value the refinement would otherwise settle towards.
        return Vector.ConditionalSelect(Vector.LessThanOrEqual(value, Vector<float>.Zero), Vector<float>.Zero, y);
    }

    #endregion

    // A destination is allowed to be longer than the source, so that one set of scratch buffers can
    // be reused across batches of differing sizes without being reallocated for each.
    private static void Fit<T>(int length, Span<T> destination, string parameterName)
    {
        if (destination.Length < length)
            throw new ArgumentException($"Destination must be at least {length} elements long to receive the result, but was {destination.Length}.", parameterName);
    }
}

#endif
