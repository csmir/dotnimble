using System.Drawing;

namespace Nimble.Drawing;

public readonly partial struct Composite
{
    /// <summary>
    ///     Creates a new <see cref="Composite"/> from the specified hue, saturation, and value (brightness) components.
    /// </summary>
    /// <param name="h">The hue to create this color from, in a range between 0 and 360 degrees.</param>
    /// <param name="s">The saturation to create this color from, in a range between 0 and 1.</param>
    /// <param name="v">The value (brightness) to create this color from, in a range between 0 and 1.</param>
    /// <returns>A new <see cref="Composite"/> value from the provided values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any of the provided values are less than, or more than the accepted range.</exception>
    public static Composite FromHSV(float h, float s, float v)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(h, 0f, MAX_DEGREES, nameof(h));
        ArgumentOutOfRangeException.ThrowIfOutOfRange(s, 0f, 1f, nameof(s));
        ArgumentOutOfRangeException.ThrowIfOutOfRange(v, 0f, 1f, nameof(v));

        return new(h, s, v);
    }

    /// <summary>
    ///     Creates a new <see cref="Composite"/> from the specified hue, saturation, lightness, and alpha components.
    /// </summary>
    /// <param name="h">The hue to create this color from, in a range between 0 and 360 degrees.</param>
    /// <param name="s">The saturation to create this color from, in a range between 0 and 1.</param>
    /// <param name="l">The lightness to create this color from, in a range between 0 and 1.</param>
    /// <param name="a">The alpha to create this color from, in a range between 0 and 1. This parameter is optional and defaults to 1 (fully opaque) if not provided.</param>
    /// <returns>A new <see cref="Composite"/> value from the provided values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any of the provided values are less than, or more than the accepted range.</exception>
    public static Composite FromHSL(float h, float s, float l, float a = 1f)
    {
        ArgumentOutOfRangeException.ThrowIfOutOfRange(h, 0f, MAX_DEGREES, nameof(h));
        ArgumentOutOfRangeException.ThrowIfOutOfRange(s, 0f, 1f, nameof(s));
        ArgumentOutOfRangeException.ThrowIfOutOfRange(l, 0f, 1f, nameof(l));
        ArgumentOutOfRangeException.ThrowIfOutOfRange(a, 0f, 1f, nameof(a));

        return new(h, s, l, a);
    }

    /// <summary>
    ///     Creates a new <see cref="Composite"/> instance that represents the specified <see cref="Color"/> value.
    /// </summary>
    /// <param name="color">The <see cref="Color"/> to convert to a <see cref="Composite"/>.</param>
    /// <returns>A <see cref="Composite"/> instance that encapsulates the value of the specified <see cref="Color"/>.</returns>
    public static Composite FromColor(Color color)
        => new(unchecked((uint)color.ToArgb()));

#if NET6_0_OR_GREATER
    /// <summary>
    ///     Creates a new <see cref="Composite"/> instance with a random value. This method is only available on .NET 6.0 or later due to the use of <see cref="Random.Shared"/>.
    /// </summary>
    /// <param name="randomizeAlpha">
    ///     When <see langword="false"/> (the default) the resulting value is fully opaque and only the R, G and B channels are randomized.
    ///     When <see langword="true"/> the alpha channel is randomized as well.
    /// </param>
    /// <returns>A <see cref="Composite"/> instance with a random value.</returns>
    public static Composite FromRandom(bool randomizeAlpha = false)
    {
        // Random.Shared.Next() only spans [0, int.MaxValue), which would cap the most significant
        // byte at 127. NextInt64 is used to cover the full 32-bit range instead.
        var value = unchecked((uint)Random.Shared.NextInt64(uint.MinValue, uint.MaxValue + 1L));

        return new(randomizeAlpha ? value : value | 0xFF000000u);
    }
#endif
}
