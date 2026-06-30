namespace Nimble.IO;

/// <summary>
///     Represents a kilobyte (KB) of data, which is equal to 1024 bytes.
/// </summary>
[Serializable]
public struct KiloBytes
{
    private double _value;

    /// <summary>
    ///     Gets the size of a kilobyte in bytes.
    /// </summary>
    public const long Size = 1024;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KiloBytes"/> struct with the specified value.
    /// </summary>
    /// <param name="value">The value of the kilobyte, defining how many kilobytes it represents.</param>
    public KiloBytes(double value) 
        => _value = value;

    /// <summary></summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary></summary>
    public static KiloBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary></summary>
    public static explicit operator KiloBytes(MegaBytes megaByte) => new(megaByte * Size);

    /// <summary></summary>
    public static explicit operator KiloBytes(GigaBytes gigaByte) => new(gigaByte * MegaBytes.Size);

    /// <summary></summary>
    public static explicit operator KiloBytes(TeraBytes teraByte) => new(teraByte * GigaBytes.Size);

    /// <summary></summary>
    public static implicit operator double(KiloBytes self) => self._value;

    /// <summary></summary>
    public readonly override string ToString() => $"{_value:F2} KB";

    /// <summary></summary>
    public readonly string ToString(IFormatProvider? formatProvider) => $"{_value.ToString("F2", formatProvider)} KB";
}
