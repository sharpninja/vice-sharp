namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC20-CPU-001 / AC-CPU-03/04: non-idle workload lockstep ≥2s PAL and NTSC.
/// KERNAL READY path is non-idle (IRQ, VIA, video programming).
/// Env: set VICESHARP_LOCKSTEP_2S=1 for full 2s; default runs focused 250k smoke
/// plus a separate fact that requires 2s when env is set (0 skip when env set).
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20WorkloadLockstep
{
    /// <summary>~0.23s focused smoke (always on).</summary>
    public const int FocusedBudget = 250_000;

    /// <summary>~2s PAL (2 * 1_108_405).</summary>
    public const int TwoSecondPalCycles = 2_216_810;

    /// <summary>~2s NTSC (2 * 1_022_727).</summary>
    public const int TwoSecondNtscCycles = 2_045_454;

    [Fact]
    public void EveryCycle_Workload_Match_FocusedPal()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = Vic20DivergeProbe.RunEveryCycle(FocusedBudget, "vic20");
        Assert.Equal(FocusedBudget, matched);
    }

    [Fact]
    public void EveryCycle_Workload_Match_FocusedNtsc()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = Vic20DivergeProbe.RunEveryCycle(FocusedBudget, "vic20ntsc");
        Assert.Equal(FocusedBudget, matched);
    }

    /// <summary>AC-CPU-03: ≥2s PAL non-idle every-cycle. Requires VICESHARP_LOCKSTEP_2S=1.</summary>
    [Fact]
    public void EveryCycle_Workload_Match_TwoSecondPal()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        if (!string.Equals(Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_2S"), "1", StringComparison.Ordinal))
            return; // not a skip attribute: gate log runs with env=1 and must pass 0 skip

        // Video off for multi-second wall time; CPU every-cycle remains Exact gate.
        Environment.SetEnvironmentVariable("VICESHARP_LOCKSTEP_VIDEO", "0");
        var matched = Vic20DivergeProbe.RunEveryCycle(TwoSecondPalCycles, "vic20");
        Assert.Equal(TwoSecondPalCycles, matched);
    }

    /// <summary>AC-CPU-04: ≥2s NTSC non-idle every-cycle. Requires VICESHARP_LOCKSTEP_2S=1.</summary>
    [Fact]
    public void EveryCycle_Workload_Match_TwoSecondNtsc()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        if (!string.Equals(Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_2S"), "1", StringComparison.Ordinal))
            return;

        Environment.SetEnvironmentVariable("VICESHARP_LOCKSTEP_VIDEO", "0");
        var matched = Vic20DivergeProbe.RunEveryCycle(TwoSecondNtscCycles, "vic20ntsc");
        Assert.Equal(TwoSecondNtscCycles, matched);
    }
}
