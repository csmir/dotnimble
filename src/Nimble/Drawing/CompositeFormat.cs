namespace Nimble.Drawing;

/// <summary>
///     Represents all color formats supported by Coloris.
/// </summary>
public enum CompositeFormat
{
    /// <summary>
    ///     sRGB format, standardized default web and UI color space. 
    ///     Uses 8 bits per channel (0-255) for red, green, and blue components.
    /// </summary>
    RGB = 1,

    /// <summary>
    ///     sRGB format with alpha channel, standardized default web and UI color space.
    ///     Uses 8 bits per channel (0-255) for red, green, blue, and alpha components.
    /// </summary>
    RGBA = 0,

    /// <summary>
    ///     HSL format, representing colors in terms of hue, saturation, and lightness.
    /// </summary>
    HSL = 3,

    /// <summary>
    ///     HSLA format, representing colors in terms of hue, saturation, lightness, and alpha (opacity).
    /// </summary>
    HSLA = 4,

    /// <summary>
    ///     HSV format, representing colors in terms of hue, saturation, and value (brightness).
    /// </summary>
    HSV = 2,

    /// <summary>
    ///     CIE-XYZ format, representing colors in a device-independent color space defined by the International Commission on Illumination (CIE).
    /// </summary>
    CIEXYZ = 5,

    /// <summary>
    ///     CIE-LAb format, representing colors in a perceptually uniform color space defined by the International Commission on Illumination (CIE).
    /// </summary>
    CIELAB = 6,

    /// <summary>
    ///     Ok-LAb format, representing colors in a perceptually uniform color space designed to be more accurate and consistent across different devices and lighting conditions compared to CIE-LAb.
    /// </summary>
    OKLAB = 7,

    /// <summary>
    ///     Ok-LCh format, representing colors in a cylindrical representation of the OK-LAb color space, where L is lightness, C is chroma, and h is hue.
    /// </summary>
    OKLCH = 8,

    /// <summary>
    ///     Hexadecimal format, representing colors as a string of hexadecimal digits (e.g., #RRGGBB or #RRGGBBAA).
    /// </summary>
    HEX = 9,

    // We can add more formats later if needed, but these should cover most use cases.
}
