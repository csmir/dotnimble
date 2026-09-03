using System.Drawing;

using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

public sealed class CompositeTests
{
    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(0xFF000000u)]
    [InlineData(0x00FFFFFFu)]
    [InlineData(0x12345678u)]
    [InlineData(0xDEADBEEFu)]
    public void ValueConstructor_PreservesValue(uint value)
    {
        Composite actual = new(value);

        Assert.Equal(value, actual.Value);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255)]
    [InlineData(1, 2, 3, 4)]
    [InlineData(18, 52, 86, 120)]
    [InlineData(255, 0, 0, 255)]
    [InlineData(0, 255, 0, 128)]
    public void ChannelConstructor_PacksChannelsIntoValue(byte r, byte g, byte b, byte a)
    {
        Composite actual = new(r, g, b, a);

        Assert.Equal(r, actual.R);
        Assert.Equal(g, actual.G);
        Assert.Equal(b, actual.B);
        Assert.Equal(a, actual.A);

        Assert.Equal(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b, actual.Value);
    }

    [Fact]
    public void ChannelConstructor_DefaultsToOpaque()
    {
        Composite actual = new(10, 20, 30);

        Assert.Equal(byte.MaxValue, actual.A);
    }

    [Fact]
    public void Channels_MatchValueLayout_ForEveryChannelValue()
    {
        for (int channel = 0; channel <= byte.MaxValue; channel++)
        {
            byte value = (byte)channel;

            Assert.Equal(value, new Composite(value, 0, 0, 0).R);
            Assert.Equal(value, new Composite(0, value, 0, 0).G);
            Assert.Equal(value, new Composite(0, 0, value, 0).B);
            Assert.Equal(value, new Composite(0, 0, 0, value).A);
        }
    }

    [Fact]
    public void ValueConstructor_AndChannelConstructor_RoundTrip()
    {
        foreach (Composite expected in Colors.Sample(20_000))
        {
            Composite actual = new(expected.R, expected.G, expected.B, expected.A);

            Assert.Equal(expected.Value, actual.Value);
        }
    }

    [Fact]
    public void MinAndMax_MatchChannelExtremes()
    {
        foreach (Composite color in Colors.Sample(20_000))
        {
            Assert.Equal(Math.Min(Math.Min(color.R, color.G), color.B), color.Min());
            Assert.Equal(Math.Max(Math.Max(color.R, color.G), color.B), color.Max());
        }
    }

    #region Mutators

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(250, 10, 255)]
    [InlineData(10, -20, 0)]
    [InlineData(128, 0, 128)]
    [InlineData(0, -1, 0)]
    [InlineData(255, 1, 255)]
    public void ShiftChannel_ClampsToChannelRange(byte start, int amount, byte expected)
    {
        Composite color = new(start, start, start, start);

        Assert.Equal(expected, color.ShiftRed(amount).R);
        Assert.Equal(expected, color.ShiftGreen(amount).G);
        Assert.Equal(expected, color.ShiftBlue(amount).B);
        Assert.Equal(expected, color.ShiftAlpha(amount).A);
    }

    [Fact]
    public void ShiftChannel_LeavesOtherChannelsAlone()
    {
        Composite color = new(0x12345678u);

        Composite red = color.ShiftRed(5);

        Assert.Equal(color.G, red.G);
        Assert.Equal(color.B, red.B);
        Assert.Equal(color.A, red.A);

        Composite alpha = color.ShiftAlpha(5);

        Assert.Equal(color.R, alpha.R);
        Assert.Equal(color.G, alpha.G);
        Assert.Equal(color.B, alpha.B);
    }

    [Fact]
    public void SetChannel_ReplacesOnlyThatChannel()
    {
        Composite color = new(0x12345678u);

        Assert.Equal(new Composite(99, color.G, color.B, color.A).Value, color.SetRed(99).Value);
        Assert.Equal(new Composite(color.R, 99, color.B, color.A).Value, color.SetGreen(99).Value);
        Assert.Equal(new Composite(color.R, color.G, 99, color.A).Value, color.SetBlue(99).Value);
        Assert.Equal(new Composite(color.R, color.G, color.B, 99).Value, color.SetAlpha(99).Value);
    }

    [Fact]
    public void SetHue_ProducesTheRequestedHue()
    {
        foreach (Composite color in Colors.Sample(2_000))
        {
            // A color with no chroma has no hue to set: every hue collapses back to the same gray.
            if (color.GetSaturation() == 0f)
                continue;

            for (float hue = 0f; hue < 360f; hue += 17f)
            {
                Composite actual = color.SetHue(hue);

                Precision.AssertSameHue(hue, actual.GetHue(), actual);
            }
        }
    }

    [Fact]
    public void ShiftHue_WrapsAroundTheWheel()
    {
        Composite color = Composite.FromHSL(10f, 1f, 0.5f);

        Precision.AssertSameHue(30d, color.ShiftHue(20f).GetHue(), color);
        Precision.AssertSameHue(350d, color.ShiftHue(-20f).GetHue(), color);
        Precision.AssertSameHue(10d, color.ShiftHue(360f).GetHue(), color);
        Precision.AssertSameHue(10d, color.ShiftHue(-360f).GetHue(), color);
    }

    [Fact]
    public void SetSaturation_And_SetBrightness_ProduceTheRequestedComponent()
    {
        foreach (Composite color in Colors.Sample(2_000))
        {
            // Saturation keeps the color's own lightness, and at either extreme of lightness there
            // is no room for any: black and white stay black and white however saturated they are.
            if (color.GetBrightness() is > 0.05f and < 0.95f)
            {
                Composite saturated = color.SetSaturation(0.25f);

                Assert.Equal(0.25d, saturated.GetSaturation(), Precision.LightnessSaturationTolerance(saturated));
            }

            Assert.Equal(0.75d, color.SetBrightness(0.75f).GetBrightness(), 0.02d);
        }
    }

    [Theory]
    [InlineData(0.5f, 0.5f, 1f)]
    [InlineData(0.5f, -0.9f, 0f)]
    [InlineData(0.5f, 0.9f, 1f)]
    public void ShiftSaturation_ClampsToUnitRange(float start, float amount, float expected)
    {
        Composite color = Composite.FromHSL(120f, start, 0.5f);

        Assert.Equal(expected, color.ShiftSaturation(amount).GetSaturation(), 0.02d);
    }

    [Fact]
    public void Mutators_PreserveAlpha()
    {
        Composite color = new(0x80123456u);

        Assert.Equal(color.A, color.ShiftHue(45f).A);
        Assert.Equal(color.A, color.SetHue(45f).A);
        Assert.Equal(color.A, color.ShiftSaturation(0.1f).A);
        Assert.Equal(color.A, color.SetSaturation(0.1f).A);
        Assert.Equal(color.A, color.ShiftBrightness(0.1f).A);
        Assert.Equal(color.A, color.SetBrightness(0.1f).A);
        Assert.Equal(color.A, color.GetGammaCorrectedColor().A);
    }

    [Theory]
    [InlineData(-360.1f)]
    [InlineData(360.1f)]
    [InlineData(float.NaN)]
    public void Hue_OutOfRange_Throws(float value)
    {
        Composite color = new(0xFF112233u);

        Assert.Throws<ArgumentOutOfRangeException>(() => color.ShiftHue(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => color.SetHue(value));
    }

    [Theory]
    [InlineData(-1.1f)]
    [InlineData(1.1f)]
    [InlineData(float.NaN)]
    public void UnitComponent_OutOfRange_Throws(float value)
    {
        Composite color = new(0xFF112233u);

        Assert.Throws<ArgumentOutOfRangeException>(() => color.ShiftSaturation(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => color.SetSaturation(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => color.ShiftBrightness(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => color.SetBrightness(value));
    }

    #endregion

    #region Derived colors

    [Fact]
    public void GetComplementaryColor_RotatesHueByHalfATurn()
    {
        foreach (Composite color in Colors.Sample(2_000))
        {
            if (color.GetSaturation() == 0f)
                continue;

            Composite complement = color.GetComplementaryColor();

            double expected = ColorMath.Rotate(ColorMath.Hue(color), 180d);

            Precision.AssertSameHue(expected, complement.GetHue(), complement);
        }
    }

    [Fact]
    public void GetComplementaryColor_IsItsOwnInverse()
    {
        foreach (Composite color in Colors.Sample(2_000))
        {
            if (color.GetSaturation() == 0f)
                continue;

            Composite roundTrip = color.GetComplementaryColor().GetComplementaryColor();

            Precision.AssertSameHue(ColorMath.Hue(color), roundTrip.GetHue(), roundTrip);
        }
    }

    [Fact]
    public void GetGammaCorrectedColor_MatchesReference()
    {
        for (int channel = 0; channel <= byte.MaxValue; channel++)
        {
            byte value = (byte)channel;

            Composite actual = new Composite(value, value, value).GetGammaCorrectedColor();
             
            int expected = (int)Math.Round(ColorMath.Encode(value / 255d) * 255d, MidpointRounding.AwayFromZero);

            // Permit tolerance for a rounding boundary up or down, simply because we cannot be sure.
            Assert.InRange(actual.R, expected - 1, expected + 1);
            Assert.Equal(actual.R, actual.G);
            Assert.Equal(actual.R, actual.B);
        }
    }

    #endregion

    #region Equality and ordering

    [Fact]
    public void Equals_ComparesByValue()
    {
        Composite left = new(0x12345678u);
        Composite right = new(0x12345678u);
        Composite other = new(0x12345679u);

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.False(left != right);

        Assert.False(left.Equals(other));
        Assert.False(left == other);
        Assert.True(left != other);
    }

    [Fact]
    public void Equals_Object_IsFalseForOtherTypes()
    {
        Composite color = new(0x12345678u);

        Assert.True(color.Equals((object)new Composite(0x12345678u)));
        Assert.False(color.Equals((object?)null));
        Assert.False(color.Equals("not a color"));
    }

    [Fact]
    public void Equals_Color_ComparesChannels()
    {
        foreach (Composite color in Colors.Sample(5_000))
        {
            Assert.True(color.Equals(color.ToColor()));
            Assert.False(color.Equals(Color.FromArgb(unchecked((int)(color.Value ^ 1u)))));
        }
    }

    [Fact]
    public void GetHashCode_MatchesValueHashCode()
    {
        foreach (Composite color in Colors.Sample(5_000))
            Assert.Equal(color.Value.GetHashCode(), color.GetHashCode());
    }

    [Fact]
    public void CompareTo_OrdersByValue()
    {
        Assert.True(new Composite(1u).CompareTo(new Composite(2u)) < 0);
        Assert.True(new Composite(2u).CompareTo(new Composite(1u)) > 0);
        Assert.Equal(0, new Composite(2u).CompareTo(new Composite(2u)));

        Assert.True(new Composite(0xFFFFFFFFu).CompareTo(new Composite(0u)) > 0);
    }

    [Fact]
    public void Clone_ProducesAnEqualValue()
    {
        Composite color = new(0x12345678u);

        object clone = ((ICloneable)color).Clone();

        Assert.Equal(color, Assert.IsType<Composite>(clone));
    }

    #endregion

    #region Conversions to and from other representations

    [Fact]
    public void ToColor_And_FromColor_RoundTrip()
    {
        foreach (Composite expected in Colors.Sample(10_000))
        {
            Color color = expected.ToColor();

            Assert.Equal(expected.R, color.R);
            Assert.Equal(expected.G, color.G);
            Assert.Equal(expected.B, color.B);
            Assert.Equal(expected.A, color.A);

            Assert.Equal(expected.Value, Composite.FromColor(color).Value);
        }
    }

    [Fact]
    public void ImplicitOperators_RoundTripThroughUInt32()
    {
        foreach (Composite expected in Colors.Sample(10_000))
        {
            uint value = expected;
            Composite actual = value;

            Assert.Equal(expected.Value, value);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void FromRandom_IsOpaqueUnlessAlphaIsRandomized()
    {
        for (int i = 0; i < 1_000; i++)
            Assert.Equal(byte.MaxValue, Composite.FromRandom().A);

        bool sawOther = false;

        for (int i = 0; i < 1_000 && !sawOther; i++)
            sawOther = Composite.FromRandom(randomizeAlpha: true).A != byte.MaxValue;

        Assert.True(sawOther, "Randomizing the alpha channel never produced a value other than 255.");
    }

    #endregion

    #region Formatting

    [Theory]
    [InlineData(0x00000000u, "rgb(0, 0, 0)")]
    [InlineData(0xFFFFFFFFu, "rgb(255, 255, 255)")]
    [InlineData(0xFF010A64u, "rgb(1, 10, 100)")]
    public void ToString_RGB_MatchesExpectedText(uint value, string expected)
        => Assert.Equal(expected, new Composite(value).ToString(CompositeFormat.RGB));

    [Theory]
    [InlineData(0x00000000u, "rgba(0, 0, 0, 0)")]
    [InlineData(0xFFFFFFFFu, "rgba(255, 255, 255, 255)")]
    [InlineData(0x0A010A64u, "rgba(1, 10, 100, 10)")]
    public void ToString_RGBA_MatchesExpectedText(uint value, string expected)
        => Assert.Equal(expected, new Composite(value).ToString(CompositeFormat.RGBA));

    [Theory]
    [InlineData(0x00000000u, "#00000000")]
    [InlineData(0xFFFFFFFFu, "#FFFFFFFF")]
    [InlineData(0x0A010A64u, "#010A640A")]
    [InlineData(0xDEADBEEFu, "#ADBEEFDE")]
    public void ToString_HEX_MatchesExpectedText(uint value, string expected)
        => Assert.Equal(expected, new Composite(value).ToString(CompositeFormat.HEX));

    [Fact]
    public void ToString_DefaultsToRGBA()
    {
        foreach (Composite color in Colors.Sample(5_000))
            Assert.Equal(color.ToString(CompositeFormat.RGBA), color.ToString());
    }

    [Fact]
    public void ToString_RGB_And_RGBA_MatchInterpolatedForm()
    {
        foreach (Composite color in Colors.Sample(20_000))
        {
            Assert.Equal($"rgb({color.R}, {color.G}, {color.B})", color.ToString(CompositeFormat.RGB));
            Assert.Equal($"rgba({color.R}, {color.G}, {color.B}, {color.A})", color.ToString(CompositeFormat.RGBA));
        }
    }

    [Fact]
    public void ToString_HEX_MatchesInterpolatedForm()
    {
        foreach (Composite color in Colors.Sample(20_000))
            Assert.Equal($"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}", color.ToString(CompositeFormat.HEX));
    }

    [Fact]
    public void ToString_HEX_CoversEveryChannelValue()
    {
        for (int channel = 0; channel <= byte.MaxValue; channel++)
        {
            byte value = (byte)channel;

            Assert.Equal($"#{value:X2}{value:X2}{value:X2}{value:X2}", new Composite(value, value, value, value).ToString(CompositeFormat.HEX));
            Assert.Equal($"rgba({value}, {value}, {value}, {value})", new Composite(value, value, value, value).ToString(CompositeFormat.RGBA));
        }
    }

    [Theory]
    [InlineData(CompositeFormat.HSL, "hsl(")]
    [InlineData(CompositeFormat.HSLA, "hsla(")]
    [InlineData(CompositeFormat.HSV, "hsv(")]
    [InlineData(CompositeFormat.CIEXYZ, "xyz(")]
    [InlineData(CompositeFormat.CIELAB, "cielab(")]
    [InlineData(CompositeFormat.OKLAB, "oklab(")]
    [InlineData(CompositeFormat.OKLCH, "oklch(")]
    public void ToString_ColorSpaceFormats_AreTagged(CompositeFormat format, string prefix)
    {
        string actual = new Composite(0xFF336699u).ToString(format);

        Assert.StartsWith(prefix, actual);
        Assert.EndsWith(")", actual);
    }

    [Fact]
    public void ToString_UnknownFormat_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Composite(0u).ToString((CompositeFormat)9999));

    #endregion
}
