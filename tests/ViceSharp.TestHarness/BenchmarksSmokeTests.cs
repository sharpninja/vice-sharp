namespace ViceSharp.TestHarness;

using ViceSharp.Benchmarks;
using Xunit;

/// <summary>
/// Smoke tests for the BenchmarkDotNet harness in
/// <c>tests/ViceSharp.Benchmarks</c>. We do not invoke BenchmarkDotNet here.
/// Instead we instantiate each benchmark class, run its setup, and execute
/// one iteration of the workload to prove the wiring is compilable and the
/// per-subsystem workloads still match the live chip surface area.
///
/// The benchmark binary itself is invoked manually via:
///   dotnet run -c Release --project tests/ViceSharp.Benchmarks
/// </summary>
public sealed class BenchmarksSmokeTests
{
    private const int SmokeCycles = 256;

    [Fact]
    public void CpuBenchmarks_OneIteration()
    {
        var bench = new CpuBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickLoop();
    }

    [Fact]
    public void VicIiBenchmarks_OneIteration()
    {
        var bench = new VicIiBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickFrame();
    }

    [Fact]
    public void SidBenchmarks_OneIteration()
    {
        var bench = new SidBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickLoop();
    }

    [Fact]
    public void CiaBenchmarks_OneIteration()
    {
        var bench = new CiaBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickLoop();
    }

    [Fact]
    public void FullSystemBenchmark_OneIteration()
    {
        var bench = new FullSystemBenchmark { Cycles = SmokeCycles };
        bench.Setup();
        bench.IterationSetup();
        bench.OneFrame();
    }
}
