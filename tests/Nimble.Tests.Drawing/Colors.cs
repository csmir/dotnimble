using Nimble.Drawing;

namespace Nimble.Tests.Drawing;

/// <summary>
///     The color sets the tests run over.
/// </summary>
/// <remarks>
///     Every sequence here is deterministic, so a failure reproduces from the test name alone.
///     Each one leads with the values that sit on a boundary in the code under test, the achromatic
///     ramp, the primaries and the fully transparent and fully opaque extremes, before it moves on
///     to pseudo-random coverage of the rest of the space.
/// </remarks>
internal static class Colors
{
    /// <summary>
    ///     The colors that sit on a boundary of one of the conversions, and so are the ones most
    ///     likely to fall on the wrong side of a clamp, a wrap or a piecewise threshold.
    /// </summary>
    public static IEnumerable<Composite> Edges()
    {
        yield return new Composite(0x00000000u);
        yield return new Composite(0xFFFFFFFFu);
        yield return new Composite(0xFF000000u);
        yield return new Composite(0x00FFFFFFu);

        // The primaries and secondaries land exactly on a hue sector boundary.
        yield return new Composite(255, 0, 0);
        yield return new Composite(0, 255, 0);
        yield return new Composite(0, 0, 255);
        yield return new Composite(255, 255, 0);
        yield return new Composite(0, 255, 255);
        yield return new Composite(255, 0, 255);

        // Either side of the sRGB transfer function's linear segment, which ends at 0.04045.
        yield return new Composite(10, 10, 10);
        yield return new Composite(11, 11, 11);

        // Either side of the CIE L* threshold.
        yield return new Composite(20, 20, 20);
        yield return new Composite(21, 21, 21);

        // The full achromatic ramp, where every hue is undefined and every saturation is zero.
        for (int channel = 0; channel <= byte.MaxValue; channel++)
            yield return new Composite((byte)channel, (byte)channel, (byte)channel);

        // One channel at a time across its whole range, against a fixed remainder.
        for (int channel = 0; channel <= byte.MaxValue; channel++)
        {
            yield return new Composite((byte)channel, 64, 192);
            yield return new Composite(64, (byte)channel, 192);
            yield return new Composite(64, 192, (byte)channel);
            yield return new Composite(64, 192, 32, (byte)channel);
        }
    }

    /// <summary>
    ///     The edge cases, followed by pseudo-random colors up to <paramref name="count"/> in total.
    /// </summary>
    public static IEnumerable<Composite> Sample(int count)
    {
        int produced = 0;

        foreach (Composite color in Edges())
        {
            if (produced++ == count)
                yield break;

            yield return color;
        }

        Random random = new(Seed);

        for (; produced < count; produced++)
            yield return new Composite((uint)random.NextInt64(uint.MinValue, uint.MaxValue + 1L));
    }

    /// <summary>
    ///     A block of pseudo-random colors, for the bulk conversions that take a span.
    /// </summary>
    public static Composite[] Block(int count, int seed = Seed)
    {
        Random random = new(seed);

        Composite[] colors = new Composite[count];

        for (int i = 0; i < colors.Length; i++)
            colors[i] = new Composite((uint)random.NextInt64(uint.MinValue, uint.MaxValue + 1L));

        return colors;
    }

    /// <summary>
    ///     Span lengths that straddle the width of a hardware vector, so that a bulk conversion is
    ///     exercised below its vectorized path, exactly on it, and with every size of tail.
    /// </summary>
    public static TheoryData<int> Lengths()
    {
        TheoryData<int> lengths = [];

        foreach (int length in new[] { 0, 1, 2, 3, 5, 7, 8, 9, 15, 16, 17, 31, 32, 33, 63, 64, 65, 127, 256, 1000, 4097 })
            lengths.Add(length);

        return lengths;
    }

    private const int Seed = 20260903;
}
