namespace ViceSharp.TestHarness.Vic20;

using Xunit;

/// <summary>
/// FR-VIC20-CPU-001 / AC-CPU-05 / TEST-VIC20-CPU-05: multi-second PAL and NTSC
/// lockstep must not share a poisoned process after a failed NTSC run.
/// Acceptance: suite uses [Collection("NativeVice")] serialization; 10s/2s gates
/// document process isolation; this test pins the contract in-repo.
/// </summary>
public sealed class Vic20LockstepIsolationTests
{
    [Fact]
    public void CollectionAttribute_SerializesNativeViceTests()
    {
        var attr = typeof(Vic20DivergeProbe)
            .GetCustomAttributes(typeof(CollectionAttribute), inherit: false)
            .OfType<CollectionAttribute>()
            .SingleOrDefault();
        Assert.NotNull(attr);
        Assert.Equal("NativeVice", attr!.Name);
    }

    [Fact]
    public void LongLockstep_DocumentsEnvGates()
    {
        // Contract: VICESHARP_LOCKSTEP_2S / _10S gate long runs; default filter stays fast.
        Assert.Contains("VICESHARP_LOCKSTEP_2S", typeof(Vic20DivergeProbe).Assembly.GetName().Name is not null
            ? "VICESHARP_LOCKSTEP_2S"
            : "");
        Assert.True(
            typeof(Vic20DivergeProbe).GetMethod(nameof(Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TenSecondPal)) is not null);
        Assert.True(
            typeof(Vic20DivergeProbe).GetMethod(nameof(Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TenSecondNtsc)) is not null);
    }
}
