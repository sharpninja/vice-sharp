namespace ViceSharp.TestHarness;

using System.Runtime.Versioning;
using ViceSharp.Host.Runtime;
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

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-HOST-AFFINITY-001, TEST: TEST-HOST-AFFINITY-02.
    /// Use case: pinning the current thread applies the requested mask to the
    /// OS thread. Acceptance: TryPinCurrentThread succeeds on Windows and a
    /// second pin returns the first mask as the previous value; the original
    /// mask is restored afterwards.
    /// </summary>
    [Fact]
    [SupportedOSPlatform("windows")]
    public void TryPinCurrentThread_Applies_The_Mask()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Thread affinity is Windows-only.");
        Assert.SkipUnless(Environment.ProcessorCount > 1, "Needs a multi-core host.");

        var mask = 1ul << 1;
        Assert.True(ThreadAffinity.TryPinCurrentThread(mask, out var original));
        try
        {
            Assert.True(ThreadAffinity.TryPinCurrentThread(mask, out var previous));
            Assert.Equal(mask, previous);
        }
        finally
        {
            ThreadAffinity.TryPinCurrentThread(original, out _);
        }
    }

    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-HOST-AFFINITY-001, TEST: TEST-HOST-AFFINITY-03.
    /// Use case: VICESHARP_EMU_CPU pins the pump's worker thread at start.
    /// Acceptance: with the variable set to core 1 the started pump reports
    /// the applied mask; with it unset the pump reports none.
    /// </summary>
    [Fact]
    public async Task Pump_Worker_Pins_From_Environment()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Thread affinity is Windows-only.");
        Assert.SkipUnless(Environment.ProcessorCount > 1, "Needs a multi-core host.");

        var ct = TestContext.Current.CancellationToken;

        Environment.SetEnvironmentVariable("VICESHARP_EMU_CPU", "1");
        try
        {
            using var pump = new EmulationPumpService(new EmulatorRuntimeRegistry());
            await pump.StartAsync(ct);
            await Task.Delay(200, ct);
            Assert.Equal(1ul << 1, pump.AppliedWorkerAffinityMask);
            await pump.StopAsync(ct);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_EMU_CPU", null);
        }

        using var unpinned = new EmulationPumpService(new EmulatorRuntimeRegistry());
        await unpinned.StartAsync(ct);
        await Task.Delay(200, ct);
        Assert.Null(unpinned.AppliedWorkerAffinityMask);
        await unpinned.StopAsync(ct);
    }
}
