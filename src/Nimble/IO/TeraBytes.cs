namespace Nimble.IO;

/// <summary>
///     Represents a terabyte (TB) of data, which is equal to 1024 gigabytes.
/// </summary>
[Serializable]
public readonly struct TeraBytes : IEquatable<TeraBytes>, IComparable<TeraBytes>, IFormattable
{
    private readonly double _value;

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

    /// <summary>
    ///     Converts the current <see cref="TeraBytes"/> instance to its equivalent size in bytes.
    /// </summary>
    public readonly long ToBytes() => checked((long)(_value * Size));

    /// <summary>
    ///     Calculates how many terabytes are represented by the specified size in bytes and returns a new <see cref="TeraBytes"/> instance.
    /// </summary>
    public static TeraBytes FromBytes(long sizeInBytes) => new((double)sizeInBytes / Size);

    /// <summary>
    ///     Converts the specified <see cref="KiloBytes"/> instance to its equivalent size in terabytes and returns a new <see cref="TeraBytes"/> instance.
    /// </summary>
    public static explicit operator TeraBytes(KiloBytes kiloByte) => new(kiloByte / GigaBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="MegaBytes"/> instance to its equivalent size in terabytes and returns a new <see cref="TeraBytes"/> instance.
    /// </summary>
    public static explicit operator TeraBytes(MegaBytes megaByte) => new(megaByte / MegaBytes.Size);

    /// <summary>
    ///     Converts the specified <see cref="GigaBytes"/> instance to its equivalent size in terabytes and returns a new <see cref="TeraBytes"/> instance.
    /// </summary>
    public static explicit operator TeraBytes(GigaBytes gigaByte) => new(gigaByte / KiloBytes.Size);

    /// <summary>
    ///     Gets the underlying value of the <see cref="TeraBytes"/> instance as a double.
    /// </summary>
    public static implicit operator double(TeraBytes self) => self._value;

    /// <summary>
    ///     The string representation of the current <see cref="TeraBytes"/> instance, formatted as a string with two decimal places followed by " TB".
    /// </summary>
    public readonly override string ToString() => ToString(null, null);

    /// <summary>
    ///     The string representation of the current <see cref="TeraBytes"/> instance, formatted according to the specified format and format provider, followed by " TB".
    /// </summary>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) => $"{_value.ToString(format ?? "F2", formatProvider)} TB";

    /// <summary>
    ///     Determines whether the specified object is equal to the current <see cref="TeraBytes"/> instance.
    /// </summary>
    public readonly override bool Equals(object? obj) => obj is TeraBytes other && Equals(other);

    /// <summary>
    ///     Determines whether the specified <see cref="TeraBytes"/> instance is equal to the current instance.
    /// </summary>
    public readonly bool Equals(TeraBytes other) => _value.Equals(other._value);

    /// <summary>
    ///     Returns a hash code for the current <see cref="TeraBytes"/> instance.
    /// </summary>
    public readonly override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    ///     Compares the current <see cref="TeraBytes"/> instance with another <see cref="TeraBytes"/> instance and returns an integer that indicates whether the current instance precedes, 
    ///     follows, or occurs in the same position in the sort order as the other instance.
    /// </summary>
    public readonly int CompareTo(TeraBytes other) => _value.CompareTo(other._value);

    /// <summary>
    ///     Determines whether two <see cref="TeraBytes"/> instances are equal.
    /// </summary>
    public static bool operator ==(TeraBytes left, TeraBytes right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two <see cref="TeraBytes"/> instances are not equal.
    /// </summary>
    public static bool operator !=(TeraBytes left, TeraBytes right) => !left.Equals(right);

    /// <summary>
    ///     Adds two <see cref="TeraBytes"/> instances.
    /// </summary>
    public static TeraBytes operator +(TeraBytes left, TeraBytes right) => new(left._value + right._value);

    /// <summary>
    ///     Subtracts one <see cref="TeraBytes"/> instance from another.
    /// </summary>
    public static TeraBytes operator -(TeraBytes left, TeraBytes right) => new(left._value - right._value);
}
