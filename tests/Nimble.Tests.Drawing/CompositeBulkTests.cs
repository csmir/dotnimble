using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

/// <summary>
///     The span conversions against the single-color accessors they are documented to reproduce.
/// </summary>
/// <remarks>
///     Several of these run a vectorized core over whole hardware vectors and a scalar loop over
///     whatever is left, so every case is repeated across lengths that fall short of one vector,
///     land exactly on one, and leave every size of tail. A result that disagrees with the scalar
///     accessor for one length and not another is the signature of a broken tail or a core that
///     reports the wrong resume index, which is why these compare per element rather than in bulk.
/// </remarks>
public sealed class CompositeBulkTests
{
    public static TheoryData<int> Lengths()
        => Colors.Lengths();

    #region Conversions

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToLinear_MatchesGetLinear(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] r = new float[length];
        float[] g = new float[length];
        float[] b = new float[length];

        Composite.ToLinear(colors, r, g, b);

        for (int i = 0; i < length; i++)
        {
            (float er, float eg, float eb) = colors[i].GetLinear();

            Assert.Equal(er, r[i]);
            Assert.Equal(eg, g[i]);
            Assert.Equal(eb, b[i]);
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToLuminosity_MatchesGetLuminosity(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] actual = new float[length];

        Composite.ToLuminosity(colors, actual);

        for (int i = 0; i < length; i++)
            Assert.Equal(colors[i].GetLuminosity(), actual[i]);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToXYZ_MatchesGetXYZ(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] x = new float[length];
        float[] y = new float[length];
        float[] z = new float[length];

        Composite.ToXYZ(colors, x, y, z);

        for (int i = 0; i < length; i++)
        {
            (float ex, float ey, float ez) = colors[i].GetXYZ();

            Assert.Equal(ex, x[i], VectorTolerance);
            Assert.Equal(ey, y[i], VectorTolerance);
            Assert.Equal(ez, z[i], VectorTolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToCIELAB_MatchesGetCIELAB(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] l = new float[length];
        float[] a = new float[length];
        float[] b = new float[length];

        Composite.ToCIELAB(colors, l, a, b);

        for (int i = 0; i < length; i++)
        {
            (float el, float ea, float eb) = colors[i].GetCIELAB();

            Assert.Equal(el, l[i], LabVectorTolerance);
            Assert.Equal(ea, a[i], LabVectorTolerance);
            Assert.Equal(eb, b[i], LabVectorTolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToOKLAB_MatchesGetOKLAB(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] l = new float[length];
        float[] a = new float[length];
        float[] b = new float[length];

        Composite.ToOKLAB(colors, l, a, b);

        for (int i = 0; i < length; i++)
        {
            (float el, float ea, float eb) = colors[i].GetOKLAB();

            Assert.Equal(el, l[i], VectorTolerance);
            Assert.Equal(ea, a[i], VectorTolerance);
            Assert.Equal(eb, b[i], VectorTolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToOKLCH_MatchesGetOKLCH(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] l = new float[length];
        float[] c = new float[length];
        float[] h = new float[length];

        Composite.ToOKLCH(colors, l, c, h);

        for (int i = 0; i < length; i++)
        {
            (float el, float ec, float eh) = colors[i].GetOKLCH();

            Assert.Equal(el, l[i]);
            Assert.Equal(ec, c[i]);
            Assert.Equal(eh, h[i]);
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToHSL_MatchesGetHSL(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] h = new float[length];
        float[] s = new float[length];
        float[] l = new float[length];

        Composite.ToHSL(colors, h, s, l);

        for (int i = 0; i < length; i++)
        {
            (float eh, float es, float el) = colors[i].GetHSL();

            Assert.Equal(eh, h[i]);
            Assert.Equal(es, s[i]);
            Assert.Equal(el, l[i]);
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void ToHSV_MatchesGetHSV(int length)
    {
        Composite[] colors = Colors.Block(length);

        float[] h = new float[length];
        float[] s = new float[length];
        float[] v = new float[length];

        Composite.ToHSV(colors, h, s, v);

        for (int i = 0; i < length; i++)
        {
            (float eh, float es, float ev) = colors[i].GetHSV();

            Assert.Equal(eh, h[i]);
            Assert.Equal(es, s[i]);
            Assert.Equal(ev, v[i]);
        }
    }

    #endregion

    #region Indexing and sorting

    [Theory]
    [MemberData(nameof(IndexTypesAndLengths))]
    public void ToIndex_MatchesGetIndex(CompositeIndex indexType, int length)
    {
        Composite[] colors = Colors.Block(length);

        double[] actual = new double[length];

        Composite.ToIndex(colors, indexType, actual);

        for (int i = 0; i < length; i++)
            Assert.Equal(colors[i].GetIndex(indexType), actual[i]);
    }

    [Theory]
    [MemberData(nameof(IndexTypesAndLengths))]
    public void Sort_OrdersByIndex(CompositeIndex indexType, int length)
    {
        Composite[] actual = Colors.Block(length);

        Composite.Sort(actual, indexType);

        for (int i = 1; i < actual.Length; i++)
            Assert.True(actual[i - 1].GetIndex(indexType) <= actual[i].GetIndex(indexType),
                $"Element {i} of {length} came out of order for {indexType}.");
    }

    [Theory]
    [MemberData(nameof(IndexTypesAndLengths))]
    public void Sort_IsAPermutationOfItsInput(CompositeIndex indexType, int length)
    {
        Composite[] expected = Colors.Block(length);
        Composite[] actual = [.. expected];

        Composite.Sort(actual, indexType);

        Assert.Equal(
            expected.Select(color => color.Value).Order(),
            actual.Select(color => color.Value).Order());
    }

    [Fact]
    public void Sort_LeavesShortSpansAlone()
    {
        Composite[] single = [new(0x12345678u)];

        Composite.Sort(single, CompositeIndex.HueHilbert);

        Assert.Equal(0x12345678u, single[0].Value);
    }

    #endregion

    #region Argument validation

    [Fact]
    public void Conversions_ShortDestination_Throws()
    {
        Composite[] colors = Colors.Block(16);

        float[] fitting = new float[16];
        float[] tooShort = new float[15];

        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToLinear(colors, tooShort, fitting, fitting));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToLinear(colors, fitting, tooShort, fitting));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToLinear(colors, fitting, fitting, tooShort));

        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToLuminosity(colors, tooShort));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToXYZ(colors, tooShort, fitting, fitting));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToCIELAB(colors, fitting, tooShort, fitting));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToOKLAB(colors, fitting, fitting, tooShort));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToOKLCH(colors, tooShort, fitting, fitting));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToHSL(colors, fitting, tooShort, fitting));
        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToHSV(colors, fitting, fitting, tooShort));

        Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToIndex(colors, CompositeIndex.HueHilbert, new double[15]));
    }

    [Fact]
    public void Conversions_AcceptALongerDestination()
    {
        // A destination may be longer than the source, so that one set of scratch buffers can be
        // reused across batches of differing sizes.
        Composite[] colors = Colors.Block(16);

        float[] roomy = new float[64];
        float[] second = new float[64];
        float[] third = new float[64];

        Composite.ToOKLAB(colors, roomy, second, third);

        for (int i = 0; i < colors.Length; i++)
            Assert.Equal(colors[i].GetOKLAB().L, roomy[i], VectorTolerance);

        // Nothing past the source length may be written.
        for (int i = colors.Length; i < roomy.Length; i++)
            Assert.Equal(0f, roomy[i]);
    }

    [Fact]
    public void ToIndex_UnknownType_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Composite.ToIndex(Colors.Block(4), (CompositeIndex)9999, new double[4]));

    [Fact]
    public void Sort_UnknownType_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Composite.Sort(Colors.Block(4), (CompositeIndex)9999));

    #endregion

    public static TheoryData<CompositeIndex, int> IndexTypesAndLengths()
    {
        TheoryData<CompositeIndex, int> data = [];

        foreach (CompositeIndex indexType in Enum.GetValues<CompositeIndex>())
        {
            foreach (int length in new[] { 0, 1, 3, 7, 8, 9, 15, 16, 17, 31, 33, 64, 1000 })
                data.Add(indexType, length);
        }

        return data;
    }

    // The vectorized cores evaluate the same expressions in the same order as the scalar accessors,
    // but the compiler is free to contract a multiply and an add into a single fused instruction on
    // one path and not the other, which moves the last place of the result.
    private const double VectorTolerance = 1e-6d;

    // CIE-LAB carries the same difference through a factor of up to 500.
    private const double LabVectorTolerance = 1e-3d;
}
