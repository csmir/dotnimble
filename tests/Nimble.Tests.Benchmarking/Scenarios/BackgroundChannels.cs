using BenchmarkDotNet.Attributes;
using Nimble.Threading.Channels;

namespace Nimble.Tests.Benchmarking.Scenarios;

[MemoryDiagnoser]
public class BackgroundChannels
{
    public IBackgroundChannel<int> Channel { get; set; } = BackgroundChannel.Create<int>(async (value) =>
    {
        value *= 10;
    });

    public BackgroundChannels()
    {
        Channel.StartReading();
    }

    [Benchmark]
    public void CreateBackgroundChannel()
    {
        var channel = BackgroundChannel.Create<int>(async (value) =>
        {
            value *= 10;
        });
    }

    [Benchmark]
    public void WorkBackgroundChannel()
    {
        Channel.TryWrite(5);
    }
}
