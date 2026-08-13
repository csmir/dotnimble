using BenchmarkDotNet.Attributes;
using Nimble.Drawing;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Nimble.Tests.Benchmarking.Scenarios;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class ColorVsComposite
{
    [Benchmark]
    [SkipLocalsInit]
    public Color CreateColorFromARGB()
    {
        return Color.FromArgb(0, 0, 0, 0);
    }

    [Benchmark]
    [SkipLocalsInit]
    public Composite CreateComposite()
    {
        return new Composite(0);
    }

    [Benchmark]
    [SkipLocalsInit]
    public Color CreateColorFromComposite()
    {
        return new Composite(0).ToColor();
    }
}