namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C64;
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

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PUBSUB-001.
    /// Use case: CI must catch breakage in the PubSub microbenchmark wiring.
    /// Acceptance: setup plus each PubSub benchmark workload runs without
    /// throwing against the live LockFreePubSub surface.
    /// </summary>
    [Fact]
    public void PubSubBenchmarks_OneIteration()
    {
        var bench = new PubSubBenchmarks();
        bench.Setup();

        bench.PublishTypedOneSubscriber();
        bench.PublishTypedThreeSubscribers();
        bench.PublishPackedPayload();
        bench.MessagePoolRentReturn();
        bench.PayloadArenaAllocate();
    }

    /// <summary>
    /// FR: FR-PERF-BENCHMARK, TR: TR-PUBSUB-001.
    /// Use case: the quick PubSub stopwatch probe must remain runnable for
    /// local requirement validation without invoking BenchmarkDotNet.
    /// Acceptance: a small probe run reports positive timing values and no
    /// steady-state allocations.
    /// </summary>
    [Fact]
    public void PubSubPerfProbe_SmallRun()
    {
        var result = PubSubPerfProbe.Run(1024);

        Assert.True(result.PublishOneSubscriberNs > 0);
        Assert.True(result.PublishThreeSubscribersNs > 0);
        Assert.True(result.PublishPackedPayloadNs > 0);
        Assert.True(result.MessagePoolRentReturnNs > 0);
        Assert.True(result.PayloadArenaAllocateNs > 0);
        Assert.Equal(0, result.AllocatedBytes);
    }

    /// <summary>
    /// FR: FR-PERF-RUNFRAME-001, TR: TR-PERF-HARNESS.
    /// Use case: CI must catch benchmark regressions that accidentally use the
    /// minimal host or romless helper instead of the production C64 PAL path.
    /// Acceptance: C64PalRunFrameBenchmark.Setup builds the Commodore 64 PAL
    /// architecture and one RunFrame iteration completes without throwing.
    /// </summary>
    [Fact]
    public void C64PalRunFrameBenchmark_UsesRealC64Pal()
    {
        var bench = new C64PalRunFrameBenchmark();
        bench.Setup();

        Assert.Equal(C64MachineProfiles.C64Pal.DisplayName, bench.ArchitectureName);
        bench.RunFrame();
    }

    /// <summary>
    /// FR: FR-PERF-RUNFRAME-001, TR: TR-PERF-HARNESS.
    /// Use case: The stopwatch probe must remain runnable for the required
    /// warmup/measurement workflow without invoking BenchmarkDotNet.
    /// Acceptance: A small probe reports positive median/p95 timings for the
    /// real C64 PAL architecture and records zero RunFrame hot-path allocations.
    /// </summary>
    [Fact]
    public void RunFramePerfProbe_SmallRun_ReportsC64PalAndNoAllocations()
    {
        var result = RunFramePerfProbe.Run(warmupFrames: 1, measuredFrames: 2);

        Assert.Equal(C64MachineProfiles.C64Pal.DisplayName, result.ArchitectureName);
        Assert.True(result.MedianMilliseconds > 0);
        Assert.True(result.P95Milliseconds > 0);
        Assert.Equal(0, result.AllocatedBytes);
    }
}
