using System.Diagnostics;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Native VICE comparison probe (PERF-BENCHMARK-001). Drives the native
/// x64sc machine through <see cref="ViceNative"/> via the shim's
/// <c>vice_machine_step_cycle</c> entry point and reports cycles per second.
/// Used alongside <see cref="PerfProbe"/> to compute a managed-vs-native
/// performance ratio for the Phase 1 closeout dashboard.
///
/// Note: the shim's step_cycle path mimics warp-mode (raw cycle pumping with
/// no host-clock pacing) so this number is comparable to the managed
/// <c>SystemClock.Step</c> hot path under release JIT.
/// </summary>
public static class NativeViceBaseline
{
    /// <summary>
    /// Returns the cycles per second the native shim can pump on this host.
    /// Throws <see cref="NotSupportedException"/> when the native shim is not
    /// available (no built native library, no VICE source).
    /// </summary>
    public static (long cyclesExecuted, TimeSpan elapsed, double cyclesPerSecond) Run(
        long budgetCycles,
        string? modelSelector = null)
    {
        if (!ViceNative.IsAvailable)
            throw new NotSupportedException(
                $"Native VICE shim is not available: {ViceNative.AvailabilityMessage}");

        var machine = string.IsNullOrWhiteSpace(modelSelector)
            ? ViceNative.Create()
            : ViceNative.CreateModel(modelSelector);
        if (machine == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        try
        {
            ViceNative.ResetNative(machine);

            // Warm-up: pump one PAL frame so any first-call paths settle.
            for (var i = 0; i < BenchmarkMachineFactory.PalFrameCycles; i++)
                ViceNative.StepNative(machine);

            var sw = Stopwatch.StartNew();
            for (var i = 0L; i < budgetCycles; i++)
                ViceNative.StepNative(machine);
            sw.Stop();

            var cps = budgetCycles / sw.Elapsed.TotalSeconds;
            return (budgetCycles, sw.Elapsed, cps);
        }
        finally
        {
            ViceNative.Destroy(machine);
        }
    }
}
