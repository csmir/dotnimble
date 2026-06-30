namespace Nimble.IO;

/// <summary>
///     Represents a gigabyte (GB) of data, which is equal to 1024 megabytes.
/// </summary>
[Serializable]
public struct GigaBytes
{
    private double _value;

    /// <summary>
    ///     Gets the size of a gigabyte in bytes.
    /// </summary>
    public const long Size = MegaBytes.Size * KiloBytes.Size;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GigaBytes"/> struct with the specified value.
    /// </summary>
    /// <param name="value">The value of the gigabyte, defining how many gigabytes it represents.</param>
    public GigaBytes(double value) 
        => _value = value;

    /// <summary></summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary></summary>
    public static explicit operator GigaBytes(KiloBytes kiloByte) => new(kiloByte / MegaBytes.Size);

    /// <summary></summary>
    public static explicit operator GigaBytes(MegaBytes megaByte) => new(megaByte / KiloBytes.Size);

    /// <summary></summary>
    public static explicit operator GigaBytes(TeraBytes teraByte) => new(teraByte * KiloBytes.Size);

    /// <summary></summary>
    public static implicit operator double(GigaBytes self) => self._value;

    /// <summary></summary>
    public readonly override string ToString() => $"{_value:F2} GB";

    /// <summary></summary>
    public readonly string ToString(IFormatProvider? formatProvider) => $"{_value.ToString("F2", formatProvider)} GB";
}
