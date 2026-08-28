namespace Nimble.Drawing;

/// <summary>
///     Generates an independent composite index based on the color's active space (sRGB value).
/// </summary>
/// <remarks>
///     These indexes can be used to sort colors by. 
///     Algorithms that back these composite indexes are specifically designed to produce accurate sort results.
/// </remarks>
public enum CompositeIndex
{
    /// <summary>
    ///     Produces a hue-first composite index that smoothens out across a single dimension.
    /// </summary>
    HLV1D = 0,

    /// <summary>
    ///     Produces a hue-first composite index that retains a sharp boundary for cutting off at multiple dimensions.
    /// </summary>
    HLV2D = 1,

    /// <summary>
    ///     Produces an inverted hue-first composite index that smoothens out across a single dimension.
    /// </summary>
    HLV1DInverted = 2,

    /// <summary>
    ///     Produces an inverted hue-first composite index that retains a sharp boundary for cutting off at multiple dimensions.
    /// </summary>
    HLV2DInverted = 3,

    /// <summary>
    ///     Produces a depth-first composite index that transitions across any dimension.
    /// </summary>
    HSV = 4,

    /// <summary>
    ///     Produces a hue-first composite index that orders each hue band along a Hilbert curve
    ///     through the OKLAB color space.
    /// </summary>
    /// <remarks>
    ///     The other indexes order each band by a single component, which keeps the sequence legible
    ///     but leaves large perceptual gaps between neighbouring colors. This one keeps the same
    ///     hue-first structure while filling each band with a space-filling curve, so consecutive
    ///     colors stay close in every dimension at once. It is roughly ten times smoother than
    ///     <see cref="HLV1D"/> by mean perceptual step, at no cost to the hue ordering, and is the
    ///     best default for sorting an arbitrary set of colors for display.
    /// </remarks>
    HueHilbert = 5,
}
