namespace Nimble.IO;

/// <summary>
///     Represents a megabyte (MB) of data, which is equal to 1024 kilobytes.
/// </summary>
[Serializable]
public struct MegaBytes
{
    private double _value;

    /// <summary>
    ///     Gets the size of a megabyte in bytes.
    /// </summary>
    public const long Size = KiloBytes.Size * KiloBytes.Size;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MegaBytes"/> struct with the specified value.
    /// </summary>
    /// <param name="value">The value of the megabyte, defining how many megabytes it represents.</param>
    public MegaBytes(double value) 
        => _value = value;

    /// <summary></summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary></summary>
    public static MegaBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary></summary>
    public static explicit operator MegaBytes(KiloBytes kiloByte) => new(kiloByte / KiloBytes.Size);

    /// <summary></summary>
    public static explicit operator MegaBytes(GigaBytes gigaByte) => new(gigaByte * KiloBytes.Size);

    /// <summary></summary>
    public static explicit operator MegaBytes(TeraBytes teraByte) => new(teraByte * Size);

    /// <summary></summary>
    public static implicit operator double(MegaBytes self) => self._value;

    /// <summary></summary>
    public readonly override string ToString() => $"{_value:F2} MB";

    /// <summary></summary>
    public readonly string ToString(IFormatProvider? formatProvider) => $"{_value.ToString("F2", formatProvider)} MB";
}
