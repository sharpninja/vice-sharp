using BenchmarkDotNet.Running;

namespace ViceSharp.Benchmarks;

/// <summary>
/// BenchmarkDotNet harness entry point.
/// Run via: dotnet run -c Release --project tests/ViceSharp.Benchmarks
///
/// Special arg: --perf-probe runs the quick stopwatch perf probe instead of
/// BenchmarkDotNet (for Phase 1 PERF-TUNING-001 cycle-per-second snapshot).
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--perf-probe")
        {
            long budget = args.Length > 1 && long.TryParse(args[1], out var b)
                ? b
                : PerfProbe.DefaultBudgetCycles;
            var (cycles, elapsed, cps) = PerfProbe.Run(budget);
            var realtimePct = cps / PerfProbe.PalCyclesPerSecond * 100.0;
            Console.WriteLine($"PerfProbe: cycles={cycles:N0} elapsed={elapsed.TotalMilliseconds:F2}ms cycles/sec={cps:N0} pct-realtime={realtimePct:F2}%");
            return 0;
        }

        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        switcher.Run(args);
        return 0;
    }
}
