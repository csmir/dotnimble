namespace Nimble.IO;

/// <summary>
///     Represents a kilobyte (KB) of data, which is equal to 1024 bytes.
/// </summary>
[Serializable]
public readonly struct KiloBytes : IEquatable<KiloBytes>, IComparable<KiloBytes>, IFormattable
{
    private readonly double _value;

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

    /// <summary>
    ///     Converts the current <see cref="KiloBytes"/> instance to its equivalent size in bytes.
    /// </summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary>
    ///     Calculates how many kilobytes are represented by the specified size in bytes and returns a new <see cref="KiloBytes"/> instance.
    /// </summary>
    public static KiloBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary>
    ///     Converts the specified <see cref="MegaBytes"/> instance to its equivalent size in kilobytes and returns a new <see cref="KiloBytes"/> instance.
    /// </summary>
    public static explicit operator KiloBytes(MegaBytes megaByte) => new(megaByte * Size);

    /// <summary>
    ///     Converts the specified <see cref="GigaBytes"/> instance to its equivalent size in kilobytes and returns a new <see cref="KiloBytes"/> instance.
    /// </summary>
    public static explicit operator KiloBytes(GigaBytes gigaByte) => new(gigaByte * MegaBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="TeraBytes"/> instance to its equivalent size in kilobytes and returns a new <see cref="KiloBytes"/> instance.
    /// </summary>
    public static explicit operator KiloBytes(TeraBytes teraByte) => new(teraByte * GigaBytes.Size);

    /// <summary>
    ///     Gets the underlying value of the <see cref="KiloBytes"/> instance as a double.
    /// </summary>
    public static implicit operator double(KiloBytes self) => self._value;

    /// <summary>
    ///     The string representation of the current <see cref="KiloBytes"/> instance, formatted as a string with two decimal places followed by " KB".
    /// </summary>
    public readonly override string ToString() => ToString(null, null);

    /// <summary>
    ///     The string representation of the current <see cref="KiloBytes"/> instance, formatted according to the specified format and format provider, followed by " KB".
    /// </summary>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) => $"{_value.ToString(format ?? "F2", formatProvider)} KB";

    /// <summary>
    ///     Determines whether the specified object is equal to the current <see cref="KiloBytes"/> instance.
    /// </summary>
    public readonly override bool Equals(object? obj) => obj is KiloBytes other && Equals(other);

    /// <summary>
    ///     Determines whether the specified <see cref="KiloBytes"/> instance is equal to the current instance.
    /// </summary>
    public readonly bool Equals(KiloBytes other) => _value.Equals(other._value);

    /// <summary>
    ///     Returns a hash code for the current <see cref="KiloBytes"/> instance.
    /// </summary>
    public readonly override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    ///     Compares the current <see cref="KiloBytes"/> instance with another <see cref="KiloBytes"/> instance and returns an integer that indicates whether the current instance precedes,
    ///     follows, or occurs in the same position in the sort order as the other instance.
    /// </summary>
    public readonly int CompareTo(KiloBytes other) => _value.CompareTo(other._value);

    /// <summary>
    ///     Determines whether two <see cref="KiloBytes"/> instances are equal.
    /// </summary>
    public static bool operator ==(KiloBytes left, KiloBytes right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two <see cref="KiloBytes"/> instances are not equal.
    /// </summary>
    public static bool operator !=(KiloBytes left, KiloBytes right) => !left.Equals(right);

    /// <summary>
    ///     Adds two <see cref="KiloBytes"/> instances.
    /// </summary>
    public static KiloBytes operator +(KiloBytes left, KiloBytes right) => new(left._value + right._value);

    /// <summary>
    ///     Subtracts one <see cref="KiloBytes"/> instance from another.
    /// </summary>
    public static KiloBytes operator -(KiloBytes left, KiloBytes right) => new(left._value - right._value);
}
