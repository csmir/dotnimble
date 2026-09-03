using System.Drawing;

using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

public sealed class CompositeConversionTests
{
    #region Against System.Drawing.Color

    [Fact]
    public void GetHue_MatchesColor()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(color.ToColor().GetHue(), color.GetHue());
    }

    [Fact]
    public void GetSaturation_MatchesColor()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(color.ToColor().GetSaturation(), color.GetSaturation());
    }

    [Fact]
    public void GetBrightness_MatchesColor()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(color.ToColor().GetBrightness(), color.GetBrightness());
    }

    #endregion

    #region Against the reference implementation

    [Fact]
    public void GetHue_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(ColorMath.Hue(color), color.GetHue(), HueTolerance);
    }

    [Fact]
    public void GetLuminosity_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            Assert.Equal(ColorMath.Luminosity(color), color.GetLuminosity(), 1e-6d);
            Assert.InRange(color.GetLuminosity(), 0f, 1f);
        }
    }

    [Fact]
    public void GetRelativeLuminance_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            Assert.Equal(ColorMath.RelativeLuminance(color), color.GetRelativeLuminance(), 1e-3d);
            Assert.InRange(color.GetRelativeLuminance(), 0f, 255f);
        }
    }

    [Fact]
    public void GetPerceivedLightness_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            Assert.Equal(ColorMath.PerceivedLightness(color), color.GetPerceivedLightness(), 1e-3d);
            Assert.InRange(color.GetPerceivedLightness(), 0f, 100f);
        }
    }

    [Fact]
    public void GetPerceivedBrightness_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            Assert.Equal(ColorMath.PerceivedBrightness(color), color.GetPerceivedBrightness(), 1e-2d);
            Assert.InRange(color.GetPerceivedBrightness(), 0f, 255f);
        }
    }

    [Fact]
    public void GetTransferCurve_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            Assert.Equal(ColorMath.TransferCurve(color), color.GetTransferCurve(), 1e-5d);
            Assert.InRange(color.GetTransferCurve(), 0f, 1f);
        }
    }

    [Fact]
    public void GetCombinedWavelength_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
            Assert.Equal(ColorMath.CombinedWavelength(color), color.GetCombinedWavelength(), 1e-3d);
    }

    [Fact]
    public void GetHSL_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float h, float s, float l) = color.GetHSL();

            Assert.Equal(ColorMath.Hue(color), h, HueTolerance);
            Assert.Equal(ColorMath.Saturation(color), s, 1e-6d);
            Assert.Equal(ColorMath.Lightness(color), l, 1e-6d);
        }
    }

    [Fact]
    public void GetHSLA_MatchesGetHSL_AndNormalizesAlpha()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float h, float s, float l) = color.GetHSL();
            (float ah, float @as, float al, float alpha) = color.GetHSLA();

            Assert.Equal(h, ah);
            Assert.Equal(s, @as);
            Assert.Equal(l, al);
            Assert.Equal(color.A / 255f, alpha);
        }
    }

    [Fact]
    public void GetHSV_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float h, float s, float v) = color.GetHSV();
            (double eh, double es, double ev) = ColorMath.HSV(color);

            Assert.Equal(eh, h, HueTolerance);
            Assert.Equal(es, s, 1e-6d);
            Assert.Equal(ev, v, 1e-6d);
        }
    }

    [Fact]
    public void GetLinear_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float r, float g, float b) = color.GetLinear();

            Assert.Equal(ColorMath.Linearize(color.R), r, 1e-6d);
            Assert.Equal(ColorMath.Linearize(color.G), g, 1e-6d);
            Assert.Equal(ColorMath.Linearize(color.B), b, 1e-6d);

            Assert.InRange(r, 0f, 1f);
            Assert.InRange(g, 0f, 1f);
            Assert.InRange(b, 0f, 1f);
        }
    }

    [Fact]
    public void GetLinear_IsWhatTheLuminosityWeightsAreAppliedTo()
    {
        // Luminosity is the Rec. 709 weighting of exactly these three values, so the two accessors
        // have to agree or one of them is linearizing differently from the other.
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float r, float g, float b) = color.GetLinear();

            double expected = (0.2126d * r) + (0.7152d * g) + (0.0722d * b);

            Assert.Equal(expected, color.GetLuminosity(), 1e-6d);
        }
    }

    [Fact]
    public void TransferTable_ResolvesItsEndpointsExactly()
    {
        // The transfer tables are built in double precision, from constants declared at that
        // precision. Building them from the single-precision forms instead leaves 1 + 0.055f
        // failing to cancel against 1.055f, which puts the top of the linear curve at 1.0000001 and
        // hands that overshoot to everything derived from it: white stops being fully luminous and
        // black on white stops hitting the WCAG maximum exactly.
        Composite white = new(255, 255, 255);
        Composite black = new(0, 0, 0);

        Assert.Equal(0f, black.GetLinear().R);
        Assert.Equal(1f, white.GetLinear().R);

        Assert.Equal(0f, black.GetLuminosity());
        Assert.Equal(1f, white.GetLuminosity());

        Assert.Equal(0f, black.GetPerceivedLightness());
        Assert.Equal(100f, white.GetPerceivedLightness());

        Assert.Equal(0f, black.GetOKLAB().L);
        Assert.Equal(1f, white.GetOKLAB().L);

        Assert.Equal(0f, black.GetCIELAB().L);
        Assert.Equal(100f, white.GetCIELAB().L);

        Assert.Equal(21d, black.GetContrastRatio(white));
    }

    [Fact]
    public void GetLinear_IsMonotonicInEachChannel()
    {
        // The transfer function is strictly increasing, so a brighter channel can never linearize
        // to a darker value. This holds regardless of how the curve is evaluated.
        for (int channel = 1; channel <= byte.MaxValue; channel++)
        {
            (float previous, float _, float _) = new Composite((byte)(channel - 1), 0, 0).GetLinear();
            (float current, float _, float _) = new Composite((byte)channel, 0, 0).GetLinear();

            Assert.True(current > previous, $"Channel {channel} linearized to {current}, not above {previous}.");
        }
    }

    [Fact]
    public void GetXYZ_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float x, float y, float z) = color.GetXYZ();
            (double ex, double ey, double ez) = ColorMath.XYZ(color);

            Assert.Equal(ex, x, 1e-6d);
            Assert.Equal(ey, y, 1e-6d);
            Assert.Equal(ez, z, 1e-6d);
        }
    }

    [Fact]
    public void GetCIELAB_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float l, float a, float b) = color.GetCIELAB();
            (double el, double ea, double eb) = ColorMath.CIELAB(color);

            Assert.Equal(el, l, 1e-3d);
            Assert.Equal(ea, a, 1e-3d);
            Assert.Equal(eb, b, 1e-3d);
        }
    }

    [Fact]
    public void GetOKLAB_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float l, float a, float b) = color.GetOKLAB();
            (double el, double ea, double eb) = ColorMath.OKLAB(color);

            Assert.Equal(el, l, 1e-5d);
            Assert.Equal(ea, a, 1e-5d);
            Assert.Equal(eb, b, 1e-5d);
        }
    }

    [Fact]
    public void GetOKLAB_StaysWithinTheGamutBoxTheHilbertLatticeAssumes()
    {
        // The Hilbert index clamps into this box before it quantizes. A color that falls outside it
        // is folded onto an edge and stops sorting where it belongs, so the box has to actually
        // contain the sRGB gamut.
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float l, float a, float b) = color.GetOKLAB();

            Assert.InRange(l, ColorMath.OklabLMin, ColorMath.OklabLMax);
            Assert.InRange(a, ColorMath.OklabAMin, ColorMath.OklabAMax);
            Assert.InRange(b, ColorMath.OklabBMin, ColorMath.OklabBMax);
        }
    }

    [Fact]
    public void GetOKLCH_MatchesReference()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            (float l, float c, float h) = color.GetOKLCH();
            (double el, double ec, double eh) = ColorMath.OKLCH(color);

            Assert.Equal(el, l, 1e-5d);
            Assert.Equal(ec, c, 1e-5d);

            // Hue is meaningless as chroma approaches zero, where a change of a few ulps in a or b
            // swings the angle arbitrarily far.
            if (c > 1e-3f)
                Assert.Equal(eh, h, 1e-2d);

            Assert.InRange(h, 0f, 360f);
        }
    }

    [Fact]
    public void GetOKLCH_MatchesGetOKLABInPolarForm()
    {
        foreach (Composite color in Colors.Sample(20_000))
        {
            (float l, float a, float b) = color.GetOKLAB();
            (float pl, float c, float _) = color.GetOKLCH();

            Assert.Equal(l, pl);
            Assert.Equal(Math.Sqrt((a * a) + (b * b)), c, 1e-6d);
        }
    }

    #endregion

    #region Differences between colors

    [Fact]
    public void GetContrastRatio_MatchesReference()
    {
        Composite[] colors = [.. Colors.Sample(200), .. Colors.Block(200)];

        foreach (Composite left in colors)
        {
            foreach (Composite right in colors)
            {
                double first = ColorMath.Luminosity(left);
                double second = ColorMath.Luminosity(right);

                double expected = (Math.Max(first, second) + 0.05d) / (Math.Min(first, second) + 0.05d);

                Assert.Equal(expected, left.GetContrastRatio(right), 1e-5d);
            }
        }
    }

    [Fact]
    public void GetContrastRatio_IsSymmetricAndBounded()
    {
        Composite[] colors = [.. Colors.Sample(200), .. Colors.Block(200)];

        foreach (Composite left in colors)
        {
            Assert.Equal(1d, left.GetContrastRatio(left), 1e-9d);

            foreach (Composite right in colors)
            {
                Assert.Equal(left.GetContrastRatio(right), right.GetContrastRatio(left), 1e-9d);
                Assert.InRange(left.GetContrastRatio(right), 1d, 21d);
            }
        }
    }

    [Fact]
    public void GetContrastRatio_BlackOnWhite_IsTheWcagMaximum()
    {
        Composite black = new(0, 0, 0);
        Composite white = new(255, 255, 255);

        Assert.Equal(21d, black.GetContrastRatio(white), 0.01d);
    }

    [Fact]
    public void GetChroma_MatchesTheChannelSpread()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            Assert.Equal(color.Max() - color.Min(), color.GetChroma());
            Assert.InRange(color.GetChroma(), 0, byte.MaxValue);
        }
    }

    [Fact]
    public void GetChroma_IsZeroExactlyWhenTheColorIsAchromatic()
    {
        foreach (Composite color in Colors.Sample(50_000))
        {
            bool achromatic = color.R == color.G && color.G == color.B;

            Assert.Equal(achromatic, color.GetChroma() == 0);

            // A color with no chroma has no hue and no saturation to speak of either.
            if (achromatic)
            {
                Assert.Equal(0f, color.GetHue());
                Assert.Equal(0f, color.GetSaturation());
            }
        }
    }

    [Fact]
    public void GetChroma_IgnoresAlpha()
    {
        foreach (Composite color in Colors.Sample(20_000))
            Assert.Equal(color.SetAlpha(byte.MaxValue).GetChroma(), color.SetAlpha(0).GetChroma());
    }

    [Fact]
    public void GetHueDifference_MatchesTheSeparationBetweenTheTwoHues()
    {
        Composite[] colors = [.. Colors.Sample(200), .. Colors.Block(200)];

        foreach (Composite left in colors)
        {
            foreach (Composite right in colors)
            {
                double expected = Precision.Separation(left.GetHue(), right.GetHue());

                Assert.Equal(expected, left.GetHueDifference(right), 1e-3d);
            }
        }
    }

    [Fact]
    public void GetHueDifference_IsSymmetricAndBounded()
    {
        Composite[] colors = [.. Colors.Sample(200), .. Colors.Block(200)];

        foreach (Composite left in colors)
        {
            Assert.Equal(0f, left.GetHueDifference(left));

            foreach (Composite right in colors)
            {
                Assert.Equal(left.GetHueDifference(right), right.GetHueDifference(left));
                Assert.InRange(left.GetHueDifference(right), 0f, 180f);
            }
        }
    }

    [Fact]
    public void GetHueDifference_TakesTheShortWayAroundTheWheel()
    {
        // Hues either side of the wrap are close together, not almost a full turn apart.
        Composite first = Composite.FromHSL(359f, 1f, 0.5f);
        Composite second = Composite.FromHSL(1f, 1f, 0.5f);

        Assert.Equal(2d, first.GetHueDifference(second), 1d);

        // Complementary colors sit half a turn apart, which is the furthest two hues can be.
        Composite red = new(255, 0, 0);

        Assert.Equal(180f, red.GetHueDifference(red.GetComplementaryColor()), 1d);
    }

    [Fact]
    public void GetEuclidian_MatchesReference()
    {
        Composite[] colors = [.. Colors.Sample(200), .. Colors.Block(200)];

        foreach (Composite left in colors)
        {
            foreach (Composite right in colors)
            {
                double dr = left.R - right.R;
                double dg = left.G - right.G;
                double db = left.B - right.B;

                Assert.Equal(Math.Sqrt((dr * dr) + (dg * dg) + (db * db)), left.GetEuclidian(right), 1e-9d);
            }
        }
    }

    [Fact]
    public void GetDeltaE_MatchesReference()
    {
        Composite[] colors = [.. Colors.Sample(200), .. Colors.Block(200)];

        foreach (Composite left in colors)
        {
            Assert.Equal(0d, left.GetDeltaE(left), 1e-9d);

            foreach (Composite right in colors)
            {
                (double l1, double a1, double b1) = ColorMath.CIELAB(left);
                (double l2, double a2, double b2) = ColorMath.CIELAB(right);

                double expected = Math.Sqrt(((l1 - l2) * (l1 - l2))
                                          + ((a1 - a2) * (a1 - a2))
                                          + ((b1 - b2) * (b1 - b2)));

                Assert.Equal(expected, left.GetDeltaE(right), 1e-2d);
                Assert.Equal(left.GetDeltaE(right), right.GetDeltaE(left), 1e-9d);
            }
        }
    }

    #endregion

    #region Round trips

    [Fact]
    public void FromHSV_RoundTripsThroughGetHSV()
    {
        for (float h = 0f; h < 360f; h += 3f)
        {
            for (float s = 0.1f; s <= 1f; s += 0.1f)
            {
                for (float v = 0.1f; v <= 1f; v += 0.1f)
                {
                    Composite color = Composite.FromHSV(h, s, v);

                    (float ah, float @as, float av) = color.GetHSV();

                    Precision.AssertSameHue(h, ah, color);

                    Assert.Equal(s, @as, Precision.ValueSaturationTolerance(color));
                    Assert.Equal(v, av, 0.01d);
                }
            }
        }
    }

    [Fact]
    public void FromHSL_RoundTripsThroughGetHSL()
    {
        for (float h = 0f; h < 360f; h += 3f)
        {
            for (float s = 0.1f; s <= 1f; s += 0.1f)
            {
                for (float l = 0.15f; l <= 0.85f; l += 0.1f)
                {
                    Composite color = Composite.FromHSL(h, s, l);

                    (float ah, float @as, float al) = color.GetHSL();

                    Precision.AssertSameHue(h, ah, color);

                    Assert.Equal(s, @as, Precision.LightnessSaturationTolerance(color));
                    Assert.Equal(l, al, 0.01d);
                }
            }
        }
    }

    [Fact]
    public void FromHSL_CarriesAlphaThrough()
    {
        for (int alpha = 0; alpha <= byte.MaxValue; alpha++)
            Assert.Equal((byte)alpha, Composite.FromHSL(200f, 0.5f, 0.5f, alpha / 255f).A);
    }

    [Fact]
    public void FromHSV_IsAlwaysOpaque()
    {
        for (float h = 0f; h <= 360f; h += 7f)
            Assert.Equal(byte.MaxValue, Composite.FromHSV(h, 0.5f, 0.5f).A);
    }

    [Theory]
    [InlineData(0f, 255, 0, 0)]
    [InlineData(60f, 255, 255, 0)]
    [InlineData(120f, 0, 255, 0)]
    [InlineData(180f, 0, 255, 255)]
    [InlineData(240f, 0, 0, 255)]
    [InlineData(300f, 255, 0, 255)]
    [InlineData(360f, 255, 0, 0)]
    public void FromHSV_AtSectorBoundaries_ProducesThePrimaries(float hue, byte r, byte g, byte b)
    {
        Composite actual = Composite.FromHSV(hue, 1f, 1f);

        Assert.Equal(r, actual.R);
        Assert.Equal(g, actual.G);
        Assert.Equal(b, actual.B);
    }

    [Theory]
    [InlineData(0f, 255, 0, 0)]
    [InlineData(60f, 255, 255, 0)]
    [InlineData(120f, 0, 255, 0)]
    [InlineData(180f, 0, 255, 255)]
    [InlineData(240f, 0, 0, 255)]
    [InlineData(300f, 255, 0, 255)]
    [InlineData(360f, 255, 0, 0)]
    public void FromHSL_AtSectorBoundaries_ProducesThePrimaries(float hue, byte r, byte g, byte b)
    {
        Composite actual = Composite.FromHSL(hue, 1f, 0.5f);

        Assert.Equal(r, actual.R);
        Assert.Equal(g, actual.G);
        Assert.Equal(b, actual.B);
    }

    [Fact]
    public void FromHSV_AndFromHSL_AgreeWhereTheModelsMeet()
    {
        // Full saturation at half lightness in HSL is the same color as full saturation and full
        // value in HSV, which is the one place the two models are required to coincide.
        for (float h = 0f; h < 360f; h += 1f)
            Assert.Equal(Composite.FromHSV(h, 1f, 1f).Value, Composite.FromHSL(h, 1f, 0.5f).Value);
    }

    [Fact]
    public void FromHSV_ZeroSaturation_IsAchromatic()
    {
        for (float v = 0f; v <= 1f; v += 0.05f)
        {
            Composite actual = Composite.FromHSV(123f, 0f, v);

            Assert.Equal(actual.R, actual.G);
            Assert.Equal(actual.G, actual.B);
        }
    }

    #endregion

    #region Argument validation

    [Theory]
    [InlineData(-1f, 0.5f, 0.5f)]
    [InlineData(361f, 0.5f, 0.5f)]
    [InlineData(180f, -0.1f, 0.5f)]
    [InlineData(180f, 1.1f, 0.5f)]
    [InlineData(180f, 0.5f, -0.1f)]
    [InlineData(180f, 0.5f, 1.1f)]
    public void FromHSV_OutOfRange_Throws(float h, float s, float v)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Composite.FromHSV(h, s, v));

    [Theory]
    [InlineData(-1f, 0.5f, 0.5f, 1f)]
    [InlineData(361f, 0.5f, 0.5f, 1f)]
    [InlineData(180f, -0.1f, 0.5f, 1f)]
    [InlineData(180f, 0.5f, 1.1f, 1f)]
    [InlineData(180f, 0.5f, 0.5f, -0.1f)]
    [InlineData(180f, 0.5f, 0.5f, 1.1f)]
    public void FromHSL_OutOfRange_Throws(float h, float s, float l, float a)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Composite.FromHSL(h, s, l, a));

    #endregion

    // A hue is a ratio of small integer differences scaled by 60, so the last place of the float it
    // is returned in is worth roughly this much.
    private const double HueTolerance = 1e-3d;
}
