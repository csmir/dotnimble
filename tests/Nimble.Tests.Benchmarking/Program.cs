
using BenchmarkDotNet.Running;
using Nimble.Tests.Benchmarking.Scenarios;

// Test individual benchmarks using this root scenario.
BenchmarkRunner.Run<EnumerationData>();