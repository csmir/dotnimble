namespace Nimble.IO;

/// <summary>
///     Represents a terabyte (TB) of data, which is equal to 1024 gigabytes.
/// </summary>
[Serializable]
public struct TeraBytes
{
    private double _value;

    /// <summary>
    ///     Gets the size of a terabyte in bytes.
    /// </summary>
    public const long Size = GigaBytes.Size * KiloBytes.Size;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TeraBytes"/> struct with the specified value.
    /// </summary>
    /// <param name="value">The value of the terabyte, defining how many terabytes it represents.</param>
    public TeraBytes(double value) 
        => _value = value;

    /// <summary></summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary></summary>
    public static TeraBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary></summary>
    public static explicit operator TeraBytes(KiloBytes kiloByte) => new(kiloByte / GigaBytes.Size);

    /// <summary></summary>
    public static explicit operator TeraBytes(MegaBytes gigaByte) => new(gigaByte / MegaBytes.Size);

    /// <summary></summary>
    public static explicit operator TeraBytes(GigaBytes teraByte) => new(teraByte / KiloBytes.Size);

    /// <summary></summary>
    public static implicit operator double(TeraBytes self) => self._value;

    /// <summary></summary>
    public readonly override string ToString() => $"{_value:F2} TB";

    /// <summary></summary>
    public readonly string ToString(IFormatProvider? formatProvider) => $"{_value.ToString("F2", formatProvider)} TB";
}
