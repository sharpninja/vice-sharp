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

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PERF-HARNESS.
    /// Use case: CI must catch breakage in the CPU benchmark wiring without
    /// invoking BenchmarkDotNet itself.
    /// Acceptance: CpuBenchmarks.Setup + one TickLoop iteration run without
    /// throwing against the live Mos6502 chip surface.
    /// </summary>
    [Fact]
    public void CpuBenchmarks_OneIteration()
    {
        var bench = new CpuBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickLoop();
    }

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PERF-HARNESS.
    /// Use case: CI must catch breakage in the VIC-II benchmark wiring.
    /// Acceptance: VicIiBenchmarks.Setup + one TickFrame iteration run without
    /// throwing against the live Mos6569 chip surface.
    /// </summary>
    [Fact]
    public void VicIiBenchmarks_OneIteration()
    {
        var bench = new VicIiBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickFrame();
    }

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PERF-HARNESS.
    /// Use case: CI must catch breakage in the SID benchmark wiring.
    /// Acceptance: SidBenchmarks.Setup + one TickLoop iteration run without
    /// throwing against the live Sid6581 chip surface.
    /// </summary>
    [Fact]
    public void SidBenchmarks_OneIteration()
    {
        var bench = new SidBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickLoop();
    }

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PERF-HARNESS.
    /// Use case: CI must catch breakage in the CIA benchmark wiring.
    /// Acceptance: CiaBenchmarks.Setup + one TickLoop iteration run without
    /// throwing against the live Mos6526 chip surface.
    /// </summary>
    [Fact]
    public void CiaBenchmarks_OneIteration()
    {
        var bench = new CiaBenchmarks { Cycles = SmokeCycles };
        bench.Setup();
        bench.TickLoop();
    }

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PERF-HARNESS.
    /// Use case: CI must catch breakage in the full-system benchmark wiring.
    /// Acceptance: FullSystemBenchmark.Setup + IterationSetup + one OneFrame
    /// iteration run without throwing against the live Commodore64 boot path.
    /// </summary>
    [Fact]
    public void FullSystemBenchmark_OneIteration()
    {
        var bench = new FullSystemBenchmark { Cycles = SmokeCycles };
        bench.Setup();
        bench.IterationSetup();
        bench.OneFrame();
    }
}
