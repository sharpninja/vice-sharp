namespace ViceSharp.TestHarness.Multisystem;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Integration tests: SystemCoordinator wrapping a real C64 IMachine. Proves
/// the coordinator-driven path produces the same observable behavior as the
/// previous direct machine.Clock.Step() loop in Program.cs.
///
/// FR/TR: ARCH-MULTISYSTEM-001 (Phase A3 - console host coordinator-driven loop).
/// Use case: Console host wraps a single C64 in a coordinator (backward-compat
/// for --machine-yaml single-machine YAML).
/// Acceptance: After N coordinator.Step() calls, machine.Clock.TotalCycles == N
/// and final CPU PC matches a direct-step baseline.
/// </summary>
public sealed class CoordinatorRealMachineIntegrationTests
{
    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Coordinator-driven path advances a real C64 machine exactly
    /// as the legacy direct-step loop did.
    /// Acceptance: 50_000 coordinator.Step() calls leave machine.Clock.TotalCycles
    /// at 50_000 and host cycles also at 50_000.
    /// </summary>
    [Fact]
    public void Coordinator_DrivingRealC64_AdvancesMachineCycles_OneToOne()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();
        var coord = new SystemCoordinator();
        coord.AttachSystem(machine);

        coord.Step(50_000);

        Assert.Equal(50_000, coord.TotalHostCycles);
        Assert.Equal(50_000, machine.Clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Coordinator-driven loop and legacy direct-step loop must
    /// produce identical CPU end state for the same cycle count.
    /// Acceptance: Final PC after 50_000 cycles via coordinator matches final
    /// PC via direct Clock.Step() loop.
    /// </summary>
    [Fact]
    public void Coordinator_DrivingRealC64_ProducesSameCpuState_AsDirectStep()
    {
        var directMachine = MachineTestFactory.CreateC64Machine();
        directMachine.Reset();
        for (int i = 0; i < 50_000; i++)
            directMachine.Clock.Step();
        var directState = directMachine.GetState();

        var coordMachine = MachineTestFactory.CreateC64Machine();
        coordMachine.Reset();
        var coord = new SystemCoordinator();
        coord.AttachSystem(coordMachine);
        coord.Step(50_000);
        var coordState = coordMachine.GetState();

        Assert.Equal(directState.PC, coordState.PC);
        Assert.Equal(directState.A, coordState.A);
        Assert.Equal(directState.X, coordState.X);
        Assert.Equal(directState.Y, coordState.Y);
        Assert.Equal(directState.S, coordState.S);
        Assert.Equal(directState.P, coordState.P);
        Assert.Equal(directState.Cycle, coordState.Cycle);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Coordinator.Reset must forward to a real C64 - cycle count
    /// returns to zero, machine returns to power-on PC.
    /// Acceptance: After Step(1000) then Reset(), machine cycles are 0 and
    /// PC matches the reset vector.
    /// </summary>
    [Fact]
    public void Coordinator_Reset_ForwardsToRealMachine()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();
        var resetPc = machine.GetState().PC;
        var coord = new SystemCoordinator();
        coord.AttachSystem(machine);
        coord.Step(1000);

        coord.Reset();

        Assert.Equal(0, coord.TotalHostCycles);
        Assert.Equal(0, machine.Clock.TotalCycles);
        Assert.Equal(resetPc, machine.GetState().PC);
    }
}
