namespace ViceSharp.TestHarness.Multisystem;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// Contract tests for SystemCoordinator: per-machine clock advancement, rate
/// proportionality, cart-extension phi2 lock, and Reset propagation.
///
/// FR/TR: ARCH-MULTISYSTEM-001 (multi-system coordinator + per-machine clocks).
/// Use case: A C64 (host) and a 1541 (independent peripheral) advance at their
/// own clock rates while sharing a coordinator that pumps both per host cycle.
/// Acceptance: A coordinator.Step() advances the host by 1, advances each
/// independent machine by (machineHz / hostHz) cycles using a fractional
/// accumulator that drifts by at most 1 cycle, and advances each cart-extension
/// by exactly 1 (phi2-locked to host).
/// </summary>
public sealed class SystemCoordinatorTests
{
    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: One attached machine, coordinator advances it once per Step.
    /// Acceptance: After N coordinator.Step() calls, the machine's clock has
    /// stepped N times.
    /// </summary>
    [Fact]
    public void SingleAttachedMachine_AdvancesOncePerCoordinatorStep()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(frequencyHz: 1_000_000);
        coord.AttachSystem(host);

        coord.Step(100);

        Assert.Equal(100, host.Clock.TotalCycles);
        Assert.Equal(100, coord.TotalHostCycles);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Host at 2 MHz, peripheral at 3 MHz - exact 3:2 ratio.
    /// Acceptance: After 2 coordinator steps, peripheral has stepped 3 times.
    /// After 200 coordinator steps, peripheral at 300, host at 200.
    /// </summary>
    [Fact]
    public void TwoMachines_AdvanceAtIndependentRates_CleanRatio()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(frequencyHz: 2_000_000);
        var drive = new TestMachine(frequencyHz: 3_000_000);
        coord.AttachSystem(host);
        coord.AttachSystem(drive);

        coord.Step(200);

        Assert.Equal(200, host.Clock.TotalCycles);
        Assert.Equal(300, drive.Clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Realistic PAL C64 (985248 Hz) + 1541 (1000000 Hz).
    /// Acceptance: After 985248 host steps (one second of host time), drive
    /// has advanced 1000000 cycles +/-1 (fractional-accumulator drift bound).
    /// </summary>
    [Fact]
    public void TwoMachines_PalC64Plus1541_DriftsByAtMostOneCycle()
    {
        var coord = new SystemCoordinator();
        var c64 = new TestMachine(frequencyHz: 985248);
        var d1541 = new TestMachine(frequencyHz: 1_000_000);
        coord.AttachSystem(c64);
        coord.AttachSystem(d1541);

        coord.Step(985248);

        Assert.Equal(985248, c64.Clock.TotalCycles);
        var drift = Math.Abs(d1541.Clock.TotalCycles - 1_000_000);
        Assert.True(drift <= 1, $"1541 drift {drift} > 1 cycle");
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001 (cart-port phi2 lock)
    /// Use case: SuperCPU-class cart extension attached to a C64 host.
    /// Acceptance: Regardless of the extension's declared FrequencyHz, it
    /// advances exactly once per host cycle (phi2-locked).
    /// </summary>
    [Fact]
    public void CartExtension_IsPhi2LockedToHost_Regardless_OfDeclaredHz()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(frequencyHz: 985248);
        var ext = new TestMachine(frequencyHz: 20_000_000);
        coord.AttachSystem(host);
        coord.AttachCartExtension(ext, host);

        coord.Step(1000);

        Assert.Equal(1000, host.Clock.TotalCycles);
        Assert.Equal(1000, ext.Clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Coordinator must reset cycle counts and forward Reset to
    /// every attached machine.
    /// Acceptance: After Reset, host cycles = 0; each attached machine's
    /// ResetCount has incremented and its clock cycles are 0.
    /// </summary>
    [Fact]
    public void Reset_ZeroesHostCycles_AndResetsAllMachines()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(frequencyHz: 1_000_000);
        var drive = new TestMachine(frequencyHz: 1_000_000);
        coord.AttachSystem(host);
        coord.AttachSystem(drive);
        coord.Step(50);

        coord.Reset();

        Assert.Equal(0, coord.TotalHostCycles);
        Assert.Equal(0, host.Clock.TotalCycles);
        Assert.Equal(0, drive.Clock.TotalCycles);
        Assert.Equal(1, host.ResetCount);
        Assert.Equal(1, drive.ResetCount);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Detaching a machine mid-run must stop further advancement
    /// of that machine but not affect others.
    /// Acceptance: Host continues advancing after drive detach; drive cycles
    /// frozen at the detach point.
    /// </summary>
    [Fact]
    public void Detach_StopsAdvancing_DetachedMachineOnly()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(frequencyHz: 1_000_000);
        var drive = new TestMachine(frequencyHz: 1_000_000);
        coord.AttachSystem(host);
        coord.AttachSystem(drive);
        coord.Step(50);

        coord.DetachSystem(drive);
        coord.Step(50);

        Assert.Equal(100, host.Clock.TotalCycles);
        Assert.Equal(50, drive.Clock.TotalCycles);
    }
}
