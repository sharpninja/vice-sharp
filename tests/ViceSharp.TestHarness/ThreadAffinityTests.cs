namespace ViceSharp.TestHarness;

using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// FR: FR-C64-Boot, TR: TR-HOST-AFFINITY-001. Opt-in CPU pinning for the
/// emulation worker (VICESHARP_EMU_CPU) and the UI thread (VICESHARP_UI_CPU):
/// the user can dedicate one core to the pump worker and another to the UI so
/// a busy desktop cannot bounce the emulator between cores. Off by default;
/// invalid values are ignored (no pin).
/// </summary>
public sealed class ThreadAffinityTests
{
    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-HOST-AFFINITY-001, TEST: TEST-HOST-AFFINITY-01.
    /// Use case: parse the env-var CPU index into an affinity mask.
    /// Acceptance: "13" maps to bit 13; null, blanks, non-numeric, negative,
    /// and indexes above 63 map to no mask.
    /// </summary>
    [Fact]
    public void ParseCpuIndex_Maps_Valid_Index_And_Rejects_Garbage()
    {
        Assert.Equal(1ul << 13, ThreadAffinity.ParseCpuIndex("13"));
        Assert.Equal(1ul << 0, ThreadAffinity.ParseCpuIndex("0"));
        Assert.Equal(1ul << 63, ThreadAffinity.ParseCpuIndex("63"));
        Assert.Null(ThreadAffinity.ParseCpuIndex(null));
        Assert.Null(ThreadAffinity.ParseCpuIndex(""));
        Assert.Null(ThreadAffinity.ParseCpuIndex("  "));
        Assert.Null(ThreadAffinity.ParseCpuIndex("core13"));
        Assert.Null(ThreadAffinity.ParseCpuIndex("-1"));
        Assert.Null(ThreadAffinity.ParseCpuIndex("64"));
    }
}
