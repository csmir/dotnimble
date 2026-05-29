using BenchmarkDotNet.Attributes;
using Nimble.ComponentModel;

namespace Nimble.Tests.Benchmarking.Scenarios;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class EnumerationData
{
    public enum TestEnum
    {
        [Obsolete("This value is obsolete.")]
        [Name("First Value")]
        Value1,

        [Name("Second Value")]
        Value2,

        [Name("Third Value")]
        Value3,

        [Name("Fourth Value")]
        Value4,

        [Name("Fifth Value")]
        [ModelBinding<string>]
        Value5
    }

    [Benchmark]
    public void GetAttributes()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var attribute = TestEnum.Value1.GetAttribute<ObsoleteAttribute>();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Benchmark]
    public void HasAttributes()
    {
        var hasAttribute = TestEnum.Value2.HasAttribute<ObsoleteAttribute>();
    }

    [Benchmark]
    public void ReadNames()
    {
        var names = Enum.GetAttributes<TestEnum, NameAttribute>();

        foreach (var kvp in names)
        {
            var enumValue = kvp.Key;
            var attributes = kvp.Value;

            foreach (var attribute in attributes)
            {
                var name = attribute.Name;
            }
        }
    }
}
