using BenchmarkDotNet.Attributes;
using ViceSharp.Abstractions;

namespace ViceSharp.Benchmarks;

/// <summary>
/// FR-PERF-RUNFRAME-001 benchmark for production C64 PAL RunFrame throughput.
/// </summary>
[MemoryDiagnoser]
public class C64PalRunFrameBenchmark
{
    private IMachine _machine = null!;

    /// <summary>Architecture name used by smoke tests to prove this is not the minimal fallback.</summary>
    public string ArchitectureName => _machine.Architecture.MachineName;

    [GlobalSetup]
    public void Setup()
    {
        _machine = BenchmarkMachineFactory.CreateC64Pal();
    }

    [Benchmark(Description = "C64 PAL IMachine.RunFrame()")]
    public void RunFrame()
    {
        _machine.RunFrame();
    }
}
