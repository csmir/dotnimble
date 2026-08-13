namespace Nimble.IO;

/// <summary>
///     Represents a gigabyte (GB) of data, which is equal to 1024 megabytes.
/// </summary>
[Serializable]
public readonly struct GigaBytes : IEquatable<GigaBytes>, IComparable<GigaBytes>, IFormattable
{
    private readonly double _value;

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

    /// <summary>
    ///     Converts the current <see cref="GigaBytes"/> instance to its equivalent size in bytes.
    /// </summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary>
    ///     Calculates how many gigabytes are represented by the specified size in bytes and returns a new <see cref="GigaBytes"/> instance.
    /// </summary>
    public static GigaBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary>
    ///     Converts the specified <see cref="KiloBytes"/> instance to its equivalent size in gigabytes and returns a new <see cref="GigaBytes"/> instance.
    /// </summary>
    public static explicit operator GigaBytes(KiloBytes kiloByte) => new(kiloByte / MegaBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="MegaBytes"/> instance to its equivalent size in gigabytes and returns a new <see cref="GigaBytes"/> instance.
    /// </summary>
    public static explicit operator GigaBytes(MegaBytes megaByte) => new(megaByte / KiloBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="TeraBytes"/> instance to its equivalent size in gigabytes and returns a new <see cref="GigaBytes"/> instance.
    /// </summary>
    public static explicit operator GigaBytes(TeraBytes teraByte) => new(teraByte * KiloBytes.Size);

    /// <summary>
    ///     Gets the underlying value of the <see cref="GigaBytes"/> instance as a double.
    /// </summary>
    public static implicit operator double(GigaBytes self) => self._value;

    /// <summary>
    ///     The string representation of the current <see cref="GigaBytes"/> instance, formatted as a string with two decimal places followed by " GB".
    /// </summary>
    public readonly override string ToString() => ToString(null, null);

    /// <summary>
    ///     The string representation of the current <see cref="GigaBytes"/> instance, formatted according to the specified format and format provider, followed by " GB".
    /// </summary>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) => $"{_value.ToString(format ?? "F2", formatProvider)} GB";

    /// <summary>
    ///     Determines whether the specified object is equal to the current <see cref="GigaBytes"/> instance.
    /// </summary>
    public readonly override bool Equals(object? obj) => obj is GigaBytes other && Equals(other);

    /// <summary>
    ///     Determines whether the specified <see cref="GigaBytes"/> instance is equal to the current instance.
    /// </summary>
    public readonly bool Equals(GigaBytes other) => _value.Equals(other._value);

    /// <summary>
    ///     Returns a hash code for the current <see cref="GigaBytes"/> instance.
    /// </summary>
    public readonly override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    ///     Compares the current <see cref="GigaBytes"/> instance with another <see cref="GigaBytes"/> instance and returns an integer that indicates whether the current instance precedes,
    ///     follows, or occurs in the same position in the sort order as the other instance.
    /// </summary>
    public readonly int CompareTo(GigaBytes other) => _value.CompareTo(other._value);

    /// <summary>
    ///     Determines whether two <see cref="GigaBytes"/> instances are equal.
    /// </summary>
    public static bool operator ==(GigaBytes left, GigaBytes right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two <see cref="GigaBytes"/> instances are not equal.
    /// </summary>
    public static bool operator !=(GigaBytes left, GigaBytes right) => !left.Equals(right);

    /// <summary>
    ///     Adds two <see cref="GigaBytes"/> instances.
    /// </summary>
    public static GigaBytes operator +(GigaBytes left, GigaBytes right) => new(left._value + right._value);

    /// <summary>
    ///     Subtracts one <see cref="GigaBytes"/> instance from another.
    /// </summary>
    public static GigaBytes operator -(GigaBytes left, GigaBytes right) => new(left._value - right._value);
}
