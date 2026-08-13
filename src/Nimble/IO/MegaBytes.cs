namespace Nimble.IO;

/// <summary>
///     Represents a megabyte (MB) of data, which is equal to 1024 kilobytes.
/// </summary>
[Serializable]
public readonly struct MegaBytes : IEquatable<MegaBytes>, IComparable<MegaBytes>, IFormattable
{
    private readonly double _value;

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

    /// <summary>
    ///     Converts the current <see cref="MegaBytes"/> instance to its equivalent size in bytes.
    /// </summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary>
    ///     Calculates how many megabytes are represented by the specified size in bytes and returns a new <see cref="MegaBytes"/> instance.
    /// </summary>
    public static MegaBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary>
    ///     Converts the specified <see cref="KiloBytes"/> instance to its equivalent size in megabytes and returns a new <see cref="MegaBytes"/> instance.
    /// </summary>
    public static explicit operator MegaBytes(KiloBytes kiloByte) => new(kiloByte / KiloBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="GigaBytes"/> instance to its equivalent size in megabytes and returns a new <see cref="MegaBytes"/> instance.
    /// </summary>
    public static explicit operator MegaBytes(GigaBytes gigaByte) => new(gigaByte * KiloBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="TeraBytes"/> instance to its equivalent size in megabytes and returns a new <see cref="MegaBytes"/> instance.
    /// </summary>
    public static explicit operator MegaBytes(TeraBytes teraByte) => new(teraByte * Size);

    /// <summary>
    ///     Gets the underlying value of the <see cref="MegaBytes"/> instance as a double.
    /// </summary>
    public static implicit operator double(MegaBytes self) => self._value;

    /// <summary>
    ///     The string representation of the current <see cref="MegaBytes"/> instance, formatted as a string with two decimal places followed by " MB".
    /// </summary>
    public readonly override string ToString() => ToString(null, null);

    /// <summary>
    ///     The string representation of the current <see cref="MegaBytes"/> instance, formatted according to the specified format and format provider, followed by " MB".
    /// </summary>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) => $"{_value.ToString(format ?? "F2", formatProvider)} MB";

    /// <summary>
    ///     Determines whether the specified object is equal to the current <see cref="MegaBytes"/> instance.
    /// </summary>
    public readonly override bool Equals(object? obj) => obj is MegaBytes other && Equals(other);

    /// <summary>
    ///     Determines whether the specified <see cref="MegaBytes"/> instance is equal to the current instance.
    /// </summary>
    public readonly bool Equals(MegaBytes other) => _value.Equals(other._value);

    /// <summary>
    ///     Returns a hash code for the current <see cref="MegaBytes"/> instance.
    /// </summary>
    public readonly override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    ///     Compares the current <see cref="MegaBytes"/> instance with another <see cref="MegaBytes"/> instance and returns an integer that indicates whether the current instance precedes,
    ///     follows, or occurs in the same position in the sort order as the other instance.
    /// </summary>
    public readonly int CompareTo(MegaBytes other) => _value.CompareTo(other._value);

    /// <summary>
    ///     Determines whether two <see cref="MegaBytes"/> instances are equal.
    /// </summary>
    public static bool operator ==(MegaBytes left, MegaBytes right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two <see cref="MegaBytes"/> instances are not equal.
    /// </summary>
    public static bool operator !=(MegaBytes left, MegaBytes right) => !left.Equals(right);

    /// <summary>
    ///     Adds two <see cref="MegaBytes"/> instances.
    /// </summary>
    public static MegaBytes operator +(MegaBytes left, MegaBytes right) => new(left._value + right._value);

    /// <summary>
    ///     Subtracts one <see cref="MegaBytes"/> instance from another.
    /// </summary>
    public static MegaBytes operator -(MegaBytes left, MegaBytes right) => new(left._value - right._value);
}
