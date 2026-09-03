using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

public sealed class CompositeIndexTests
{
    public static TheoryData<CompositeIndex> IndexTypes()
    {
        TheoryData<CompositeIndex> types = [];

        foreach (CompositeIndex indexType in Enum.GetValues<CompositeIndex>())
            types.Add(indexType);

        return types;
    }

    #region Z-order

    [Fact]
    public void GetZValue_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(ColorMath.ZValue(color), color.GetZValue());
    }

    [Fact]
    public void GetZValue_InterleavesTheChannelsIntoThirtyBits()
    {
        foreach (Composite color in Colors.Sample(20_000))
        {
            int actual = color.GetZValue();

            Assert.InRange(actual, 0, (1 << 24) - 1);

            // Bit j of red lands at 3j, green at 3j+1 and blue at 3j+2.
            for (int bit = 0; bit < 8; bit++)
            {
                Assert.Equal((color.R >> bit) & 1, (actual >> (bit * 3)) & 1);
                Assert.Equal((color.G >> bit) & 1, (actual >> ((bit * 3) + 1)) & 1);
                Assert.Equal((color.B >> bit) & 1, (actual >> ((bit * 3) + 2)) & 1);
            }
        }
    }

    [Fact]
    public void GetZValue_IsInjectiveOverRGB()
    {
        // Alpha is not part of the Z-order, so two colors differing only in alpha collide by design.
        Dictionary<int, Composite> seen = [];

        foreach (Composite color in Colors.Sample(50_000))
        {
            Composite opaque = new(color.R, color.G, color.B);

            if (seen.TryGetValue(opaque.GetZValue(), out Composite existing))
                Assert.Equal(existing.Value, opaque.Value);
            else
                seen[opaque.GetZValue()] = opaque;
        }
    }

    [Fact]
    public void GetZValue_IgnoresAlpha()
    {
        foreach (Composite color in Colors.Sample(20_000))
            Assert.Equal(color.SetAlpha(byte.MaxValue).GetZValue(), color.SetAlpha(0).GetZValue());
    }

    #endregion

    #region Hilbert

    [Fact]
    public void HueHilbertIndex_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(HueHilbertReference(color), color.GetIndex(CompositeIndex.HueHilbert));
    }

    [Fact]
    public void HueHilbertIndex_KeepsTheHueBandAsTheLeadingTerm()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            int band = ColorMath.Band(color.GetHue() / 360f);

            double index = color.GetIndex(CompositeIndex.HueHilbert);

            // The curve distance occupies the range below one band, scaled by CFACTOR squared.
            Assert.InRange(index, band * 64d, (band + 1) * 64d);
        }
    }

    #endregion

    #region The banded indexes

    [Theory]
    [InlineData(CompositeIndex.HLV1D, true)]
    [InlineData(CompositeIndex.HLV2D, false)]
    public void HLVIndex_MatchesReference(CompositeIndex indexType, bool smooth)
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            float lum = color.GetLuminosity();

            (float h, float _, float v) = color.GetHSV();

            int band = ColorMath.Band(h / 360f);

            if (smooth && (band & 1) is 1)
            {
                lum = 1f - lum;
                v = 1f - v;
            }

            // The tie breaker is the luminosity after the inversion, not before it, so that colors
            // sharing a cell in a reversed band are ordered the same way round as the band itself.
            double expected = ColorMath.Pack(band, ColorMath.Band(lum), ColorMath.Band(v), lum);

            Assert.Equal(expected, color.GetIndex(indexType));
        }
    }

    [Theory]
    [InlineData(CompositeIndex.HLV1DInverted, true)]
    [InlineData(CompositeIndex.HLV2DInverted, false)]
    public void HLVInvertedIndex_MatchesReference(CompositeIndex indexType, bool smooth)
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            float lum = color.GetLuminosity();

            (float h, float _, float v) = color.GetHSV();

            // Rotated in single precision, as the implementation does. Half a turn is exact in
            // binary, but the remainder that follows it is not, and a band boundary is close enough
            // to notice the difference.
            float rotated = (h + 180f) % 360f;

            if (rotated < 0f)
                rotated += 360f;

            int band = ColorMath.Band(1f - (rotated / 360f));

            if (smooth && (band & 1) is 1)
            {
                lum = 1f - lum;
                v = 1f - v;
            }

            double expected = ColorMath.Pack(band, ColorMath.Band(lum), ColorMath.Band(v), lum);

            Assert.Equal(expected, color.GetIndex(indexType));
        }
    }

    [Fact]
    public void HSVIndex_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float h, float s, float v) = color.GetHSV();

            double expected = ColorMath.Pack(
                ColorMath.Band(h / 360f),
                ColorMath.Band(s),
                ColorMath.Band(v),
                color.GetLuminosity());

            Assert.Equal(expected, color.GetIndex(CompositeIndex.HSV));
        }
    }

    #endregion

    #region Shared behaviour

    [Theory]
    [MemberData(nameof(IndexTypes))]
    public void GetIndex_StaysWithinTheSharedRange(CompositeIndex indexType)
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            double index = color.GetIndex(indexType);

            Assert.False(double.IsNaN(index), $"{indexType} produced NaN for {color}.");
            Assert.InRange(index, 0d, 512d);
        }
    }

    [Theory]
    [MemberData(nameof(IndexTypes))]
    public void GetIndex_IsStableForTheSameColor(CompositeIndex indexType)
    {
        foreach (Composite color in Colors.Sample(20_000))
            Assert.Equal(color.GetIndex(indexType), color.GetIndex(indexType));
    }

    [Theory]
    [MemberData(nameof(IndexTypes))]
    public void GetIndex_IgnoresAlpha(CompositeIndex indexType)
    {
        foreach (Composite color in Colors.Sample(5_000))
        {
            Composite opaque = color.SetAlpha(byte.MaxValue);
            Composite transparent = color.SetAlpha(0);

            Assert.Equal(opaque.GetIndex(indexType), transparent.GetIndex(indexType));
        }
    }

    [Fact]
    public void GetIndex_UnknownType_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Composite(0u).GetIndex((CompositeIndex)9999));

    #endregion

    // The Hilbert index is built from the library's own hue and OKLAB, so that this checks the
    // transform and the way its distance is folded into the index rather than re-testing the color
    // space conversions the other file already covers.
    private static double HueHilbertReference(Composite color)
    {
        int band = ColorMath.Band(color.GetHue() / 360f);

        (float l, float a, float b) = color.GetOKLAB();

        uint distance = ColorMath.Hilbert(
            ColorMath.Quantize(l, ColorMath.OklabLMin, ColorMath.OklabLMax),
            ColorMath.Quantize(a, ColorMath.OklabAMin, ColorMath.OklabAMax),
            ColorMath.Quantize(b, ColorMath.OklabBMin, ColorMath.OklabBMax));

        double position = band + (distance / (double)(1u << (ColorMath.HilbertBits * 3)));

        return position * 8 * 8;
    }
}
