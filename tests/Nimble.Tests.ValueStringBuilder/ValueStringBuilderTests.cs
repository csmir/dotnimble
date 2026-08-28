#if NET6_0_OR_GREATER

using System.Globalization;
using System.Text;

using Vsb = Nimble.Text.ValueStringBuilder;

namespace Nimble.Tests.ValueStringBuilder;

public sealed class ValueStringBuilderTests
{
    [Fact]
    public void DefaultConstructor_MatchesEmptyStringBuilder()
    {
        Vsb actual = new();
        StringBuilder expected = new();

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("\0")]
    [InlineData("😀")]
    [InlineData("hello\0😀world")]
    public void StringConstructor_MatchesStringBuilder(string value)
    {
        Vsb actual = new(value);
        StringBuilder expected = new(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcdef", 0, 0)]
    [InlineData("abcdef", 0, 3)]
    [InlineData("abcdef", 2, 3)]
    [InlineData("abcdef", 6, 0)]
    [InlineData("", 0, 0)]
    public void StringConstructor_Range_MatchesStringBuilder(string value, int startIndex, int length)
    {
        Vsb actual = new(value, startIndex, length, 16);
        StringBuilder expected = new(value, startIndex, length, 16);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(128)]
    public void CapacityConstructor_StartsWithExpectedCapacity(int capacity)
    {
        Vsb actual = new(capacity);
        StringBuilder expected = new(capacity);

        Assert.Equal(expected.Length, actual.Length);
        Assert.True(actual.Capacity >= actual.Length);
        Assert.True(actual.Capacity >= capacity);
    }

    [Fact]
    public void SpanConstructor_UsesProvidedStore()
    {
        Span<char> storage = stackalloc char[64];

        Vsb actual = new(storage);

        Assert.Equal(64, actual.Capacity);
        Assert.Equal(0, actual.Length);

        actual.Append("hello");

        Assert.Equal("hello", actual.ToString());
    }

    [Fact]
    public void Length_Setter_Grow_MatchesStringBuilder()
    {
        Vsb actual = new(8);
        StringBuilder expected = new(8);

        actual.Append("abc");
        expected.Append("abc");

        actual.Length = 10;
        expected.Length = 10;

        AssertEquivalent(expected, actual);
        Assert.All(actual.ToString(), c => { });
    }

    [Fact]
    public void Length_Setter_Shrink_MatchesStringBuilder()
    {
        Vsb actual = new("abcdefgh");
        StringBuilder expected = new("abcdefgh");

        actual.Length = 3;
        expected.Length = 3;

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Length_Setter_Grow_ClearsNewCharacters()
    {
        Vsb actual = new("abc")
        {
            Length = 10
        };

        Assert.Equal(10, actual.Length);
        Assert.Equal("abc\0\0\0\0\0\0\0", actual.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(100)]
    public void EnsureCapacity_MaintainsContents(int capacity)
    {
        Vsb actual = new("hello");
        string expectedContents = "hello";

        int returned = actual.EnsureCapacity(capacity);

        Assert.Equal(expectedContents, actual.ToString());
        Assert.Equal(actual.Capacity, returned);
        Assert.True(actual.Capacity >= capacity);
        Assert.True(actual.Capacity >= actual.Length);
    }

    [Fact]
    public void Capacity_CanGrow()
    {
        Vsb actual = new("hello");

        int oldCapacity = actual.Capacity;

        actual.Capacity = oldCapacity + 50;

        Assert.True(actual.Capacity >= oldCapacity + 50);
        Assert.Equal("hello", actual.ToString());
    }

    [Fact]
    public void Capacity_CanShrink()
    {
        Vsb actual = new("hello");

        actual.Capacity = actual.Length;

        Assert.Equal(actual.Length, actual.Capacity);
        Assert.Equal("hello", actual.ToString());
    }

    [Theory]
    [InlineData('a')]
    [InlineData('\0')]
    public void Append_Char_MatchesStringBuilder(char value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData('a', 0)]
    [InlineData('a', 1)]
    [InlineData('a', 2)]
    [InlineData('x', 100)]
    [InlineData('\0', 10)]
    public void Append_Char_Repeat_MatchesStringBuilder(char value, int count)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value, count);
        expected.Append(value, count);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("\0")]
    [InlineData("😀")]
    public void Append_String_MatchesStringBuilder(string value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_NullString_MatchesStringBuilder()
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append((string?)null);
        expected.Append((string?)null);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcdef", 0, 6)]
    [InlineData("abcdef", 0, 3)]
    [InlineData("abcdef", 2, 3)]
    [InlineData("abcdef", 6, 0)]
    public void Append_StringRange_MatchesStringBuilder(string value, int startIndex, int count)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value, startIndex, count);
        expected.Append(value, startIndex, count);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcdef", 0, 6)]
    [InlineData("abcdef", 0, 3)]
    [InlineData("abcdef", 2, 3)]
    [InlineData("abcdef", 6, 0)]
    public void Append_CharArrayRange_MatchesStringBuilder(string value, int startIndex, int count)
    {
        char[] chars = value.ToCharArray();

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(chars, startIndex, count);
        expected.Append(chars, startIndex, count);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_CharArray_MatchesStringBuilder()
    {
        char[] chars = "hello".ToCharArray();

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(chars);
        expected.Append(chars);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_ReadOnlyMemory_MatchesStringBuilder()
    {
        ReadOnlyMemory<char> memory = "hello world".AsMemory();

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(memory);
        expected.Append(memory.Span);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_ReadOnlySpan_MatchesStringBuilder()
    {
        ReadOnlySpan<char> value = "hello world";

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Append_Bool_MatchesStringBuilder(bool value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(sbyte.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(sbyte.MaxValue)]
    public void Append_SByte_MatchesStringBuilder(sbyte value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData(1)]
    [InlineData(byte.MaxValue)]
    public void Append_Byte_MatchesStringBuilder(byte value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(short.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(short.MaxValue)]
    public void Append_Short_MatchesStringBuilder(short value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Append_Int_MatchesStringBuilder(int value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void Append_Long_MatchesStringBuilder(long value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(1.5)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void Append_Float_MatchesStringBuilder(float value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(1.5)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void Append_Double_MatchesStringBuilder(double value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1.25")]
    [InlineData("123456789.123456789")]
    public void Append_Decimal_MatchesStringBuilder(string value)
    {
        decimal number = decimal.Parse(value, CultureInfo.InvariantCulture);

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(number);
        expected.Append(number);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_Object_IFormattable_MatchesStringBuilder()
    {
        object value = 12345;

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_Object_NonFormattable_MatchesStringBuilder()
    {
        object value = new TestObject("hello");

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(value);
        expected.Append(value);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_NullObject_MatchesStringBuilder()
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append((object?)null);
        expected.Append((object?)null);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendLine_MatchesStringBuilder()
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendLine();
        expected.AppendLine();

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData("😀")]
    public void AppendLine_String_MatchesStringBuilder(string value)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendLine(value);
        expected.AppendLine(value);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData('a', 0)]
    [InlineData('a', 1)]
    [InlineData('a', 5)]
    public void AppendLine_CharRepeat_MatchesStringBuilder(char value, int count)
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendLine(value, count);
        expected.Append(value, count).AppendLine();

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendLine_Numeric_MatchesStringBuilder()
    {
        Vsb actual = new();
        StringBuilder expected = new();

        actual
            .AppendLine(123)
            .AppendLine(123456789L)
            .AppendLine(1.25f)
            .AppendLine(2.5d)
            .AppendLine(3.75m)
            .AppendLine(true)
            .AppendLine((object)"hello");

        expected
            .AppendLine((123).ToString())
            .AppendLine((123456789L).ToString())
            .AppendLine((1.25f).ToString())
            .AppendLine((2.5d).ToString())
            .AppendLine((3.75m).ToString())
            .AppendLine((true).ToString())
            .AppendLine(((object)"hello").ToString());

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Indexer_GetAndSet_MatchesStringBuilder()
    {
        Vsb actual = new("abcdef");
        StringBuilder expected = new("abcdef");

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }

        actual[2] = 'X';
        expected[2] = 'X';

        actual[5] = '\0';
        expected[5] = '\0';

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("hello world")]
    public void Clear_MatchesStringBuilder(string initial)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Clear();
        expected.Clear();

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcdef", 0, 1)]
    [InlineData("abcdef", 0, 6)]
    [InlineData("abcdef", 2, 1)]
    [InlineData("abcdef", 2, 3)]
    [InlineData("abcdef", 5, 1)]
    [InlineData("abcdef", 6, 0)]
    public void Remove_MatchesStringBuilder(string initial, int startIndex, int length)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Remove(startIndex, length);
        expected.Remove(startIndex, length);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Insert_Strings_MatchesStringBuilder()
    {
        Vsb actual = new("abcd");
        StringBuilder expected = new("abcd");

        actual.Insert(0, "X");
        expected.Insert(0, "X");

        actual.Insert(2, "YZ");
        expected.Insert(2, "YZ");

        actual.Insert(actual.Length, "END");
        expected.Insert(expected.Length, "END");

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcd", 0, "X", 1)]
    [InlineData("abcd", 2, "X", 1)]
    [InlineData("abcd", 4, "X", 1)]
    [InlineData("abcd", 1, "XY", 3)]
    [InlineData("abcd", 2, "", 10)]
    public void Insert_StringCount_MatchesStringBuilder(string initial, int index, string value, int count)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Insert(index, value, count);
        expected.Insert(index, value, count);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Insert_Char_MatchesStringBuilder()
    {
        Vsb actual = new("abcd");
        StringBuilder expected = new("abcd");

        actual.Insert(0, 'X');
        expected.Insert(0, 'X');

        actual.Insert(3, 'Y');
        expected.Insert(3, 'Y');

        actual.Insert(actual.Length, 'Z');
        expected.Insert(expected.Length, 'Z');

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Insert_Numeric_MatchesStringBuilder()
    {
        StringBuilder expected = new("abcdef");
        Vsb actual = new("abcdef");

        actual.Insert(0, 123);
        expected.Insert(0, 123);

        actual.Insert(2, 456L);
        expected.Insert(2, 456L);

        actual.Insert(3, 1.25);
        expected.Insert(3, 1.25);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Insert_CharArrayRange_MatchesStringBuilder()
    {
        char[] value = "XYZ123".ToCharArray();

        Vsb actual = new("abcdef");
        StringBuilder expected = new("abcdef");

        actual.Insert(2, value, 1, 3);
        expected.Insert(2, value, 1, 3);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcabc", "abc", "X")]
    [InlineData("aaaa", "a", "bb")]
    [InlineData("aaaa", "aa", "b")]
    [InlineData("abcabcabc", "abc", "")]
    [InlineData("abcabcabc", "abc", "XYZ")]
    [InlineData("abcdef", "x", "y")]
    [InlineData("abcdef", "abc", "abc")]
    [InlineData("aaaa", "aa", "aaa")]
    public void Replace_String_MatchesStringBuilder(string initial, string oldValue, string newValue)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Replace(oldValue, newValue);
        expected.Replace(oldValue, newValue);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcabcabc", "abc", "X", 0, 9)]
    [InlineData("abcabcabc", "abc", "X", 3, 6)]
    [InlineData("abcabcabc", "abc", "X", 0, 3)]
    [InlineData("abcabcabc", "abc", "X", 6, 3)]
    [InlineData("aaaaaa", "aa", "X", 1, 4)]
    [InlineData("abcdef", "x", "y", 0, 6)]
    public void Replace_StringRange_MatchesStringBuilder(string initial, string oldValue, string newValue, int startIndex, int count)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Replace(oldValue, newValue, startIndex, count);
        expected.Replace(oldValue, newValue, startIndex, count);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcabc", 'a', 'X')]
    [InlineData("aaaa", 'a', 'b')]
    [InlineData("abcdef", 'x', 'y')]
    [InlineData("a\0a", 'a', 'X')]
    public void Replace_Char_MatchesStringBuilder(string initial, char oldChar, char newChar)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Replace(oldChar, newChar);
        expected.Replace(oldChar, newChar);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcabcabc", 'a', 'X', 0, 9)]
    [InlineData("abcabcabc", 'a', 'X', 1, 8)]
    [InlineData("abcabcabc", 'a', 'X', 3, 6)]
    public void Replace_CharRange_MatchesStringBuilder(string initial, char oldChar, char newChar, int startIndex, int count)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        actual.Replace(oldChar, newChar, startIndex, count);
        expected.Replace(oldChar, newChar, startIndex, count);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void Append_SpanFromOwnContents_IsCorrect()
    {
        Vsb actual = new("abcdef");

        ReadOnlySpan<char> value = actual.ToString().AsSpan(1, 3);

        actual.Append(value);

        Assert.Equal("abcdefbcd", actual.ToString());
    }

    [Fact]
    public void Append_SpanFromOwnBackingStore_IsCorrect()
    {
        Span<char> storage = stackalloc char[64];

        Vsb actual = new(storage);
        actual.Append("abcdef");

        ReadOnlySpan<char> source = storage[1..4];

        actual.Append(source);

        Assert.Equal("abcdefbcd", actual.ToString());
    }

    [Fact]
    public void Insert_SpanFromOwnBackingStore_IsCorrect()
    {
        Span<char> storage = stackalloc char[64];

        Vsb actual = new(storage);
        actual.Append("abcdef");

        ReadOnlySpan<char> source = storage[1..4];

        actual.Insert(2, source);

        Assert.Equal("abbcdcdef", actual.ToString());
    }

    [Fact]
    public void Replace_SpanFromOwnBackingStore_IsCorrect()
    {
        Span<char> storage = stackalloc char[64];

        Vsb actual = new(storage);
        actual.Append("abcdefabcdef");

        ReadOnlySpan<char> oldValue = storage[0..3];
        ReadOnlySpan<char> newValue = storage[3..6];

        actual.Replace(oldValue, newValue);

        Assert.Equal("defdefdefdef", actual.ToString());
    }

    [Fact]
    public void AppendJoin_StringArray_MatchesStringBuilder()
    {
        string?[] values = ["a", "b", null, "d"];

        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendJoin(",", values);
        expected.AppendJoin(",", values);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendJoin_CharStringArray_MatchesStringBuilder()
    {
        string?[] values = ["a", "b", null, "d"];

        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendJoin(',', values);
        expected.AppendJoin(',', values);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendJoin_ObjectArray_MatchesStringBuilder()
    {
        object?[] values = [1, "hello", null, 2.5];

        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendJoin(",", values);
        expected.AppendJoin(",", values);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendJoin_Enumerable_MatchesStringBuilder()
    {
        IEnumerable<int> values = Enumerable.Range(1, 5);

        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendJoin("-", values);
        expected.AppendJoin("-", values);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendJoin_Empty_MatchesStringBuilder()
    {
        string[] values = [];

        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendJoin(",", values);
        expected.AppendJoin(",", values);

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void AppendJoin_NullSeparator_MatchesStringBuilder()
    {
        string?[] values = ["a", "b", "c"];

        Vsb actual = new();
        StringBuilder expected = new();

        actual.AppendJoin(null, values);
        expected.AppendJoin(null, values);

        AssertEquivalent(expected, actual);
    }

    [Theory]
    [InlineData("abcdef", 0, 3)]
    [InlineData("abcdef", 1, 3)]
    [InlineData("abcdef", 3, 2)]
    [InlineData("abcdef", 6, 0)]
    public void CopyTo_Array_MatchesStringBuilder(string initial, int sourceIndex, int count)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        char[] actualDestination = new char[10];
        char[] expectedDestination = new char[10];

        actual.CopyTo(sourceIndex, actualDestination, 2, count);
        expected.CopyTo(sourceIndex, expectedDestination, 2, count);

        Assert.Equal(expectedDestination, actualDestination);
    }

    [Theory]
    [InlineData("abcdef", 0, 3)]
    [InlineData("abcdef", 1, 3)]
    [InlineData("abcdef", 3, 2)]
    [InlineData("abcdef", 6, 0)]
    public void CopyTo_Span_MatchesStringBuilder(string initial, int sourceIndex, int count)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        Span<char> actualDestination = stackalloc char[10];
        Span<char> expectedDestination = stackalloc char[10];

        actual.CopyTo(sourceIndex, actualDestination, count);
        expected.CopyTo(sourceIndex, expectedDestination, count);

        Assert.Equal(expectedDestination.ToArray(), actualDestination.ToArray());
    }

    [Theory]
    [InlineData("abcdef", 0, 0)]
    [InlineData("abcdef", 0, 3)]
    [InlineData("abcdef", 2, 3)]
    [InlineData("abcdef", 6, 0)]
    public void ToString_Range_MatchesStringBuilder(string initial, int startIndex, int length)
    {
        Vsb actual = new(initial);
        StringBuilder expected = new(initial);

        Assert.Equal(expected.ToString(startIndex, length), actual.ToString(startIndex, length));
    }

    [Fact]
    public void Enumerator_MatchesStringBuilder()
    {
        Vsb actual = new("hello\0😀world");
        StringBuilder expected = new("hello\0😀world");

        Assert.Equal(expected.ToString().ToCharArray(), Enumerate(actual));
    }

    [Fact]
    public void CreateValueCopy_CopiesLogicalState()
    {
        Vsb original = new("hello");

        Vsb copy = original.CreateValueCopy();

        Assert.Equal(original.Length, copy.Length);
        Assert.Equal(original.ToString(), copy.ToString());
        Assert.Equal(original.Capacity, copy.Capacity);
        Assert.Equal(original.MaxCapacity, copy.MaxCapacity);

        copy.Append(" world");

        Assert.Equal("hello", original.ToString());
        Assert.Equal("hello world", copy.ToString());
    }

    [Fact]
    public void CreateValueCopy_CanBeDisposedIndependently()
    {
        Vsb original = new("hello");

        Vsb copy = original.CreateValueCopy();

        copy.Dispose();

        Assert.Equal("hello", original.ToString());

        original.Dispose();
    }

    [Fact]
    public void InterpolatedString_Basic_MatchesStringBuilder()
    {
        int number = 123;
        string text = "hello";

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append($"Text={text}, Number={number}");
        expected.Append($"Text={text}, Number={number}");

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void InterpolatedString_Alignment_MatchesStringBuilder()
    {
        int value = 42;

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append($"[{value,10}]");
        expected.Append($"[{value,10}]");

        actual.Append($"[{value,-10}]");
        expected.Append($"[{value,-10}]");

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void InterpolatedString_Format_MatchesStringBuilder()
    {
        DateTime value = new(2026, 8, 28, 13, 14, 15);

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append($"{value:yyyy-MM-dd}");
        expected.Append($"{value:yyyy-MM-dd}");

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void InterpolatedString_Provider_MatchesStringBuilder()
    {
        CultureInfo culture = CultureInfo.InvariantCulture;

        double value = 12345.6789;

        Vsb actual = new();
        StringBuilder expected = new();

        actual.Append(culture, $"{value:N2}");
        expected.Append(culture, $"{value:N2}");

        AssertEquivalent(expected, actual);
    }

    [Fact]
    public void RandomizedOperations_MatchStringBuilder()
    {
        const int Seed = 0x51A7BEEF;

        Random random = new(Seed);

        Vsb actual = new(0);
        StringBuilder expected = new();

        for (int operation = 0; operation < 10_000; operation++)
        {
            ExecuteRandomOperation(ref actual, expected, random);

            AssertEquivalent(expected, actual, $"Operation #{operation} failed.");
        }
    }

    private static void ExecuteRandomOperation(ref Vsb actual, StringBuilder expected, Random random)
    {
        switch (random.Next(8))
        {
            case 0:
                {
                    string value = RandomString(random);

                    actual.Append(value);
                    expected.Append(value);

                    break;
                }

            case 1:
                {
                    char value = RandomChar(random);
                    int count = random.Next(0, 8);

                    actual.Append(value, count);
                    expected.Append(value, count);

                    break;
                }

            case 2:
                {
                    char value = RandomChar(random);

                    if (expected.Length == 0)
                    {
                        actual.Append(value);
                        expected.Append(value);
                        break;
                    }

                    int index = random.Next(expected.Length + 1);

                    actual.Insert(index, value);
                    expected.Insert(index, value);

                    break;
                }

            case 3:
                {
                    if (expected.Length == 0)
                        break;

                    int start = random.Next(expected.Length);
                    int length = random.Next(expected.Length - start + 1);

                    actual.Remove(start, length);
                    expected.Remove(start, length);

                    break;
                }

            case 4:
                {
                    char oldChar = RandomChar(random);
                    char newChar = RandomChar(random);

                    actual.Replace(oldChar, newChar);
                    expected.Replace(oldChar, newChar);

                    break;
                }

            case 5:
                {
                    actual.AppendLine();
                    expected.AppendLine();

                    break;
                }

            case 6:
                {
                    int value = random.Next(int.MinValue, int.MaxValue);

                    actual.Append(value);
                    expected.Append(value);
                    break;
                }

            case 7:
                {
                    actual.Clear();
                    expected.Clear();

                    break;
                }
        }

        // The randomized numeric branch above needs to use the same generated
        // value on both sides. It is deliberately kept out of the switch
        // generation because Random.Next(int.MinValue, int.MaxValue) has
        // platform/version-dependent boundary behavior.
    }

    [Fact]
    public void Remove_InvalidArguments_MatchExceptionType()
    {
        AssertSameException(
            () =>
            {
                StringBuilder builder = new("abc");
                builder.Remove(-1, 1);
            },
            () =>
            {
                Vsb builder = new("abc");
                builder.Remove(-1, 1);
            });

        AssertSameException(
            () =>
            {
                StringBuilder builder = new("abc");
                builder.Remove(0, 4);
            },
            () =>
            {
                Vsb builder = new("abc");
                builder.Remove(0, 4);
            });
    }

    [Fact]
    public void Insert_InvalidArguments_MatchExceptionType()
    {
        AssertSameException(
            () =>
            {
                StringBuilder builder = new("abc");
                builder.Insert(-1, "x");
            },
            () =>
            {
                Vsb builder = new("abc");
                builder.Insert(-1, "x");
            });

        AssertSameException(
            () =>
            {
                StringBuilder builder = new("abc");
                builder.Insert(4, "x");
            },
            () =>
            {
                Vsb builder = new("abc");
                builder.Insert(4, "x");
            });
    }

    [Fact]
    public void Replace_InvalidRange_MatchesExceptionType()
    {
        AssertSameException(
            () =>
            {
                StringBuilder builder = new("abcdef");
                builder.Replace("a", "b", 4, 4);
            },
            () =>
            {
                Vsb builder = new("abcdef");
                builder.Replace("a", "b", 4, 4);
            });
    }

    [Fact]
    public void Append_StringRange_InvalidArguments_MatchExceptionType()
    {
        AssertSameException(
            () =>
            {
                StringBuilder builder = new();
                builder.Append("abc", 2, 5);
            },
            () =>
            {
                Vsb builder = new();
                builder.Append("abc", 2, 5);
            });
    }

    private static void AssertEquivalent(StringBuilder expected, Vsb actual, string? message = null)
    {
        Assert.Equal(expected.Length, actual.Length);

        Assert.Equal(expected.ToString(), actual.ToString());

        Assert.True(actual.Capacity >= actual.Length, message ?? "Vsb capacity is smaller than Length.");

        Assert.Equal(expected.ToString().ToCharArray(), Enumerate(actual));
    }

    private static char[] Enumerate(Vsb builder)
    {
        List<char> result = [];

        foreach (char c in builder)
            result.Add(c);

        return [.. result];
    }

    private static void AssertSameException(Action expectedAction, Action actualAction)
    {
        Exception? expected = Record.Exception(expectedAction);
        Exception? actual = Record.Exception(actualAction);

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.GetType(), actual.GetType());
    }

    private static string RandomString(Random random)
    {
        int length = random.Next(0, 20);

        char[] chars = new char[length];

        for (int i = 0; i < chars.Length; i++)
            chars[i] = RandomChar(random);

        return new string(chars);
    }

    private static char RandomChar(Random random)
    {
        return random.Next(8) switch
        {
            0 => '\0',
            1 => ' ',
            2 => 'a',
            3 => 'z',
            4 => 'A',
            5 => 'Z',
            6 => (char)random.Next(0x20, 0x7F),
            _ => (char)random.Next(0x80, 0x400),
        };
    }

    private sealed class TestObject(string value)
    {
        public override string ToString() => value;
    }
}

#endif