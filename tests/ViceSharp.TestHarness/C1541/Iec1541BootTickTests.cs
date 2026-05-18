namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-001 (Phase B1d-1).
/// Use case: A built 1541 drive machine is reset and ticked under its own
/// clock; the drive 6502 fetches instructions from the DOS ROM and the
/// program counter advances. Confirms ROM is wired into the bus AND the
/// CPU is executing real code (not stuck on a reset vector or in a
/// 1-instruction loop because of an unwired peripheral).
///
/// True drive-CPU lockstep against upstream VICE drivecpu is Phase B1d-4
/// and depends on a ViceNative extension; this slice is the cheap-path
/// gate that proves the drive machine boots in our emulator.
/// </summary>
public sealed class Iec1541BootTickTests
{
    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Drive reset latches PC from the ROM reset vector; ticking
    /// the clock advances both the cycle counter and the PC into the boot
    /// sequence.
    /// Acceptance: After Step(1000) the PC has changed from the reset value
    /// AND the clock has counted 1000 cycles.
    /// </summary>
    [Fact]
    public void DriveCpu_ExecutesRomFromResetVector()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = new ArchitectureBuilder(provider).Build(new C1541Descriptor());
        machine.Reset();
        var resetPc = machine.GetState().PC;

        machine.Clock.Step(1000);

        machine.Clock.TotalCycles.Should().Be(1000);
        machine.GetState().PC.Should().NotBe(resetPc);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: A 50k-cycle drive run never crashes the CPU - the drive
    /// stays in some valid PC region. The Mos6502 implementation does not
    /// throw or wedge on the DOS ROM's RAM/VIA accesses.
    /// Acceptance: After Step(50_000) the machine is still operable and PC
    /// is inside the ROM region (or RAM via vectored jump) - i.e. not stuck
    /// on the reset vector and not in a corrupt range.
    /// </summary>
    [Fact]
    public void DriveCpu_Runs_50kCycles_WithoutCrash()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = new ArchitectureBuilder(provider).Build(new C1541Descriptor());
        machine.Reset();
        var resetPc = machine.GetState().PC;

        machine.Clock.Step(50_000);

        machine.Clock.TotalCycles.Should().Be(50_000);
        var pc = machine.GetState().PC;
        pc.Should().NotBe(resetPc);
        // PC should be inside either ROM ($C000-$FFFF), RAM ($0000-$07FF), or
        // a VIA address used for code (rare but possible for self-modifying
        // boot sequences). We accept any of these as "not crashed".
        var inRom = pc >= 0xC000;
        var inRam = pc < 0x0800;
        var inVia = pc >= 0x1800 && pc < 0x2000;
        (inRom || inRam || inVia).Should().BeTrue(
            $"drive PC {pc:X4} is outside the canonical 1541 address regions");
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Drive machine reset is idempotent - calling Reset twice
    /// produces the same state as one reset.
    /// Acceptance: After Reset() / Step(100) / Reset(), PC + cycle counter
    /// match a fresh Reset().
    /// </summary>
    [Fact]
    public void Reset_IsIdempotent_AfterPartialRun()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = new ArchitectureBuilder(provider).Build(new C1541Descriptor());

        machine.Reset();
        var freshPc = machine.GetState().PC;
        machine.Clock.Step(100);
        machine.Reset();

        machine.GetState().PC.Should().Be(freshPc);
        machine.Clock.TotalCycles.Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: SystemCoordinator drives a single 1541 machine at its own
    /// 1MHz clock; per-cycle ticks land on the drive's clock one-to-one.
    /// Acceptance: After coordinator.Step(N) the drive clock has N cycles
    /// and PC has advanced beyond the reset vector.
    /// </summary>
    [Fact]
    public void Coordinator_DrivingDriveMachine_AdvancesItOneToOne()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = new ArchitectureBuilder(provider).Build(new C1541Descriptor());
        machine.Reset();
        var resetPc = machine.GetState().PC;
        var coord = new SystemCoordinator();
        coord.AttachSystem(machine);

        coord.Step(10_000);

        machine.Clock.TotalCycles.Should().Be(10_000);
        coord.TotalHostCycles.Should().Be(10_000);
        machine.GetState().PC.Should().NotBe(resetPc);
    }
}
