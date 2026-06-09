using System.Diagnostics;
using ViceSharp.Architectures.C64;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Stopwatch probe for FR-PERF-RUNFRAME-001 60-warmup / 600-frame measurement.
/// </summary>
public static class RunFramePerfProbe
{
    public const int DefaultWarmupFrames = 60;
    public const int DefaultMeasuredFrames = 600;
    public const double TargetMedianMilliseconds = 18.0;
    public const double TargetP95Milliseconds = 22.0;

    public static Result Run(
        int warmupFrames = DefaultWarmupFrames,
        int measuredFrames = DefaultMeasuredFrames)
    {
        if (warmupFrames < 0)
            throw new ArgumentOutOfRangeException(nameof(warmupFrames));
        if (measuredFrames <= 0)
            throw new ArgumentOutOfRangeException(nameof(measuredFrames));

        var machine = BenchmarkMachineFactory.CreateC64Pal();
        if (!string.Equals(machine.Architecture.MachineName, C64MachineProfiles.C64Pal.DisplayName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"RunFrame probe requires {C64MachineProfiles.C64Pal.DisplayName}; got {machine.Architecture.MachineName}.");
        }

        for (var i = 0; i < warmupFrames; i++)
            machine.RunFrame();

        var frameMilliseconds = new double[measuredFrames];
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        var frequency = Stopwatch.Frequency;
        for (var i = 0; i < measuredFrames; i++)
        {
            var start = Stopwatch.GetTimestamp();
            machine.RunFrame();
            var end = Stopwatch.GetTimestamp();
            frameMilliseconds[i] = (end - start) * 1000.0 / frequency;
        }
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Array.Sort(frameMilliseconds);
        var min = frameMilliseconds[0];
        var max = frameMilliseconds[^1];
        var median = Median(frameMilliseconds);
        var p95 = PercentileNearestRank(frameMilliseconds, 0.95);

        return new Result(
            machine.Architecture.MachineName,
            warmupFrames,
            measuredFrames,
            median,
            p95,
            min,
            max,
            allocatedBytes);
    }

    private static double Median(double[] sorted)
    {
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private static double PercentileNearestRank(double[] sorted, double percentile)
    {
        var rank = (int)Math.Ceiling(percentile * sorted.Length);
        var index = Math.Clamp(rank - 1, 0, sorted.Length - 1);
        return sorted[index];
    }

    public readonly record struct Result(
        string ArchitectureName,
        int WarmupFrames,
        int MeasuredFrames,
        double MedianMilliseconds,
        double P95Milliseconds,
        double MinMilliseconds,
        double MaxMilliseconds,
        long AllocatedBytes)
    {
        public bool MeetsTarget =>
            MedianMilliseconds <= TargetMedianMilliseconds &&
            P95Milliseconds <= TargetP95Milliseconds &&
            AllocatedBytes == 0;
    }
}
