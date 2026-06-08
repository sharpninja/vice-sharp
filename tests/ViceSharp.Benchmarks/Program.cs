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

        if (args.Length > 0 && args[0] == "--native-probe")
        {
            long budget = args.Length > 1 && long.TryParse(args[1], out var nb)
                ? nb
                : PerfProbe.DefaultBudgetCycles;
            string? model = args.Length > 2 ? args[2] : null;
            try
            {
                var (cycles, elapsed, cps) = NativeViceBaseline.Run(budget, model);
                var realtimePct = cps / PerfProbe.PalCyclesPerSecond * 100.0;
                Console.WriteLine($"NativeViceBaseline: model={model ?? "default"} cycles={cycles:N0} elapsed={elapsed.TotalMilliseconds:F2}ms cycles/sec={cps:N0} pct-realtime={realtimePct:F2}%");
                return 0;
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"NativeViceBaseline: SKIPPED - {ex.Message}");
                return 0;
            }
        }

        if (args.Length > 0 && args[0] == "--pubsub-probe")
        {
            var messageCount = args.Length > 1 && int.TryParse(args[1], out var pc)
                ? pc
                : PubSubPerfProbe.DefaultMessageCount;
            var result = PubSubPerfProbe.Run(messageCount);
            Console.WriteLine($"PubSubPerfProbe: messages={result.MessageCount:N0} publish-one={result.PublishOneSubscriberNs:F2}ns publish-three={result.PublishThreeSubscribersNs:F2}ns publish-packed={result.PublishPackedPayloadNs:F2}ns pool-rent-return={result.MessagePoolRentReturnNs:F2}ns arena-alloc={result.PayloadArenaAllocateNs:F2}ns allocated={result.AllocatedBytes:N0} bytes");
            return 0;
        }

        if (args.Length > 0 && args[0] == "--perf-compare")
        {
            long budget = args.Length > 1 && long.TryParse(args[1], out var cb)
                ? cb
                : PerfProbe.DefaultBudgetCycles;
            var (_, mElapsed, mCps) = PerfProbe.Run(budget);
            var mPct = mCps / PerfProbe.PalCyclesPerSecond * 100.0;
            Console.WriteLine($"managed: cycles/sec={mCps:N0} pct-realtime={mPct:F2}% elapsed={mElapsed.TotalMilliseconds:F2}ms");
            try
            {
                var (_, nElapsed, nCps) = NativeViceBaseline.Run(budget);
                var nPct = nCps / PerfProbe.PalCyclesPerSecond * 100.0;
                Console.WriteLine($"native:  cycles/sec={nCps:N0} pct-realtime={nPct:F2}% elapsed={nElapsed.TotalMilliseconds:F2}ms");
                Console.WriteLine($"ratio managed/native: {mCps / nCps:F3}x");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"native:  SKIPPED - {ex.Message}");
            }
            return 0;
        }

        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        switcher.Run(args);
        return 0;
    }
}
