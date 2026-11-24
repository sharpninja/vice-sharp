namespace ViceSharp.TestHarness;

using System.Runtime.Versioning;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// Windows integration coverage for TR-HOST-AFFINITY-001 using real OS threads.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Host", "Windows")]
public sealed class ThreadAffinityIntegrationTests
{
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
        Assert.SkipUnless(Environment.ProcessorCount > 1, "Needs a multi-core host.");

        var ct = TestContext.Current.CancellationToken;

        var previous = Environment.GetEnvironmentVariable("VICESHARP_EMU_CPU");
        try
        {
            Environment.SetEnvironmentVariable("VICESHARP_EMU_CPU", "1");
            using var pump = new EmulationPumpService(new EmulatorRuntimeRegistry());
            await pump.StartAsync(ct);
            await Task.Delay(200, ct);
            Assert.Equal(1ul << 1, pump.AppliedWorkerAffinityMask);
            await pump.StopAsync(ct);

            Environment.SetEnvironmentVariable("VICESHARP_EMU_CPU", null);
            using var unpinned = new EmulationPumpService(new EmulatorRuntimeRegistry());
            await unpinned.StartAsync(ct);
            await Task.Delay(200, ct);
            Assert.Null(unpinned.AppliedWorkerAffinityMask);
            await unpinned.StopAsync(ct);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_EMU_CPU", previous);
        }
    }
}
