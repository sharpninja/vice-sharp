namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-LOCKSTEP-VSF-001. Operator utility: capture a baseline native (x64sc)
/// CPU run log from a VICE snapshot, driven entirely by environment variables
/// so baselines can be (re)captured on demand without code edits:
/// VICESHARP_CAPTURE_VSF (snapshot path), VICESHARP_CAPTURE_SECONDS (PAL
/// seconds; or VICESHARP_CAPTURE_CYCLES for an exact cycle count),
/// VICESHARP_CAPTURE_OUT (output .cpulog path). Skips cleanly when unset, so
/// the suite carries no hidden work.
/// </summary>
[Collection("NativeVice")]
public sealed class CaptureRunLogUtilityTests
{
    private const double PalClockHz = 985248.0;

    /// <summary>
    /// FR: n/a (operator tooling), TR: TR-LOCKSTEP-VSF-001.
    /// Use case: produce the baseline VICE run log for a demo snapshot (e.g.
    /// Pieces of Light intro) that SnapshotRunLogParityTests replays managed
    /// runs against.
    /// Acceptance: with the VICESHARP_CAPTURE_* variables set, the utility
    /// resumes the snapshot on the native shim, records exactly the requested
    /// number of per-cycle entries, saves a v1 cpulog to the requested path
    /// and reports its size; without them it skips.
    /// </summary>
    [ViceFact]
    public void CaptureBaselineRunLog_FromEnvironment()
    {
        var vsf = Environment.GetEnvironmentVariable("VICESHARP_CAPTURE_VSF");
        var outPath = Environment.GetEnvironmentVariable("VICESHARP_CAPTURE_OUT");
        var seconds = Environment.GetEnvironmentVariable("VICESHARP_CAPTURE_SECONDS");
        var cyclesRaw = Environment.GetEnvironmentVariable("VICESHARP_CAPTURE_CYCLES");

        if (string.IsNullOrWhiteSpace(vsf) || string.IsNullOrWhiteSpace(outPath)
            || (string.IsNullOrWhiteSpace(seconds) && string.IsNullOrWhiteSpace(cyclesRaw)))
        {
            Assert.Skip("Capture utility idle: set VICESHARP_CAPTURE_VSF, VICESHARP_CAPTURE_OUT and VICESHARP_CAPTURE_SECONDS (or _CYCLES).");
        }

        Assert.True(File.Exists(vsf), $"snapshot not found: {vsf}");

        var cycles = !string.IsNullOrWhiteSpace(cyclesRaw)
            ? int.Parse(cyclesRaw)
            : checked((int)Math.Round(double.Parse(seconds!) * PalClockHz));

        LockstepValidator.SaveNativeRunLog(vsf!, cycles, outPath!);

        Assert.True(File.Exists(outPath), $"capture did not produce {outPath}");
        var info = new FileInfo(outPath!);
        Assert.True(info.Length > 0, "capture produced an empty log");
    }
}
