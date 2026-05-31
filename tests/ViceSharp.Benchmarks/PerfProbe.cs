using System.Diagnostics;
using ViceSharp.Abstractions;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Quick stopwatch-based perf probe for Phase 1 PERF-TUNING-001. Drives a
/// rom-less C64 for a fixed cycle budget and reports cycles per second.
/// Not a BenchmarkDotNet benchmark; just a deterministic single-run number
/// for the handoff record.
/// </summary>
public static class PerfProbe
{
    public const int PalCyclesPerSecond = 985_248;
    public const long DefaultBudgetCycles = 10_000_000;

    public static (long cyclesExecuted, TimeSpan elapsed, double cyclesPerSecond) Run(long budgetCycles)
    {
        var machine = BenchmarkMachineFactory.CreateRomlessC64();
        // Warm-up: run one PAL frame to trigger JIT compilation on hot path.
        machine.Clock.Step(BenchmarkMachineFactory.PalFrameCycles);

        var sw = Stopwatch.StartNew();
        machine.Clock.Step((int)budgetCycles);
        sw.Stop();

        var cps = budgetCycles / sw.Elapsed.TotalSeconds;
        return (budgetCycles, sw.Elapsed, cps);
    }
}
