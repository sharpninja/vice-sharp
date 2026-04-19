using System;
using System.Diagnostics;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// Measures wall-clock time for a fixed number of emulated cycles.
/// Runs multiple iterations and reports average host cycles per emulated cycle.
/// </summary>
public class PerformanceBenchmark
{
    public record BenchmarkResult(
        long TotalEmulatedCycles,
        long TotalHostTicks,
        double HostCyclesPerEmulatedCycle,
        double EmulatedCyclesPerSecond,
        TimeSpan WallClockTime,
        int Iterations
    );

    private readonly IMachine _machine;
    private readonly int _iterations;
    private readonly long _cyclesPerIteration;

    public PerformanceBenchmark(IMachine machine, int iterations = 10, long cyclesPerIteration = 100000)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _iterations = iterations;
        _cyclesPerIteration = cyclesPerIteration;
    }

    public BenchmarkResult Run()
    {
        var totalHostTicks = 0L;
        var frequency = Stopwatch.Frequency;

        // Warm-up run (discarded)
        _machine.Reset();
        _machine.Clock.Step(_cyclesPerIteration);

        // Actual benchmark runs
        for (int i = 0; i < _iterations; i++)
        {
            _machine.Reset();

            var sw = Stopwatch.StartNew();
            _machine.Clock.Step(_cyclesPerIteration);
            sw.Stop();

            totalHostTicks += sw.ElapsedTicks;
        }

        var totalEmulatedCycles = _cyclesPerIteration * _iterations;
        var hostCyclesPerSecond = frequency; // Assuming 1 tick = 1 cycle for simplicity
        var hostCyclesPerEmulatedCycle = (double)totalHostTicks / totalEmulatedCycles;
        var emulatedCyclesPerSecond = totalEmulatedCycles / ((double)totalHostTicks / frequency);

        return new BenchmarkResult(
            TotalEmulatedCycles: totalEmulatedCycles,
            TotalHostTicks: totalHostTicks,
            HostCyclesPerEmulatedCycle: hostCyclesPerEmulatedCycle,
            EmulatedCyclesPerSecond: emulatedCyclesPerSecond,
            WallClockTime: TimeSpan.FromTicks(totalHostTicks),
            Iterations: _iterations
        );
    }

    public string GenerateReport(BenchmarkResult result)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== VICE-Sharp Performance Benchmark ===");
        sb.AppendLine();
        sb.AppendLine($"Total emulated cycles: {result.TotalEmulatedCycles:N0}");
        sb.AppendLine($"Iterations:            {result.Iterations}");
        sb.AppendLine($"Cycles per iteration:  {result.TotalEmulatedCycles / result.Iterations:N0}");
        sb.AppendLine();
        sb.AppendLine($"Wall-clock time:       {result.WallClockTime.TotalSeconds:F3}s");
        sb.AppendLine($"Host cycles/emulated:  {result.HostCyclesPerEmulatedCycle:F2}");
        sb.AppendLine($"Emulated cycles/sec:   {result.EmulatedCyclesPerSecond:N0}");
        sb.AppendLine();

        // Performance rating
        var rating = result.EmulatedCyclesPerSecond switch
        {
            >= 10_000_000 => "Excellent (>10 MHz)",
            >= 5_000_000 => "Good (>5 MHz)",
            >= 1_000_000 => "Acceptable (>1 MHz)",
            >= 500_000 => "Slow (>500 kHz)",
            _ => "Very slow (<500 kHz)"
        };

        sb.AppendLine($"Performance rating: {rating}");

        return sb.ToString();
    }
}