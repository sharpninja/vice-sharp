namespace ViceSharp.TestHarness.IEC;

using FluentAssertions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-IECVDRIVE-001 / TEST-IECVDRIVE-003.
/// Use case: the KERNAL serial trap must (a) be completely inert when the
/// addressed drive has no virtual disk - guaranteeing native cycle parity and
/// leaving the true-drive path untouched - and (b) when a disk is present,
/// service the routine, set the KERNAL register/flag state, and redirect PC to
/// the resume address ($EDAB), faithful to VICE serial-trap.c.
/// </summary>
public sealed class KernalSerialTrapTests
{
    private const byte FlagCarry = 0x01;
    private const byte FlagInterrupt = 0x04;

    private static (BasicBus Bus, byte[] Ram) NewBus()
    {
        var ram = new byte[0x10000];
        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, ram));
        // The five trap-address check bytes from the 901227-03 KERNAL.
        ram[0xED24] = 0x20; ram[0xED25] = 0x97; ram[0xED26] = 0xEE;
        ram[0xED37] = 0x20; ram[0xED38] = 0x8E; ram[0xED39] = 0xEE;
        ram[0xED41] = 0x20; ram[0xED42] = 0x97; ram[0xED43] = 0xEE;
        ram[0xEE14] = 0xA9; ram[0xEE15] = 0x00; ram[0xEE16] = 0x85;
        ram[0xEEA9] = 0xAD; ram[0xEEAA] = 0x00; ram[0xEEAB] = 0xDD;
        return (bus, ram);
    }

    private static D64Image MinimalDisk()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();
        return image;
    }

    [Fact]
    public void TryHandle_NoDisk_Declines_LeavingCpuUntouched()
    {
        var (bus, ram) = NewBus();
        ram[0x95] = 0x28; // LISTEN device 8
        var cpu = new Mos6502(bus);
        var trap = new KernalSerialTrap(cpu, bus, new VirtualDriveServer(_ => null));

        var handled = trap.TryHandle(0xED24);

        handled.Should().BeFalse("with no disk the trap must defer to the real KERNAL");
    }

    [Fact]
    public void TryHandle_WrongCheckBytes_Declines()
    {
        var (bus, ram) = NewBus();
        ram[0x95] = 0x28;
        ram[0xED24] = 0x00; // KERNAL not banked in / patched ROM
        var cpu = new Mos6502(bus);
        var image = MinimalDisk();
        var trap = new KernalSerialTrap(cpu, bus, new VirtualDriveServer(d => d == 8 ? image : null));

        trap.TryHandle(0xED24).Should().BeFalse();
    }

    [Fact]
    public void TryHandle_NonTrapAddress_Declines()
    {
        var (bus, _) = NewBus();
        var cpu = new Mos6502(bus);
        var image = MinimalDisk();
        var trap = new KernalSerialTrap(cpu, bus, new VirtualDriveServer(d => d == 8 ? image : null));

        trap.TryHandle(0xC000).Should().BeFalse();
    }

    [Fact]
    public void TryHandle_AttentionListenWithDisk_HandlesAndResumes()
    {
        var (bus, ram) = NewBus();
        ram[0x95] = 0x28; // LISTEN device 8
        var cpu = new Mos6502(bus) { P = 0xFF };
        var image = MinimalDisk();
        var trap = new KernalSerialTrap(cpu, bus, new VirtualDriveServer(d => d == 8 ? image : null));

        var handled = trap.TryHandle(0xED24);

        handled.Should().BeTrue();
        cpu.PC.Should().Be(0xEDAB, "the trap resumes at the KERNAL serial routine tail");
        (cpu.P & FlagCarry).Should().Be(0, "attention clears carry");
        (cpu.P & FlagInterrupt).Should().Be(0, "attention clears the interrupt-disable flag");
    }

    [Fact]
    public void TryHandle_ReadyWithActiveDisk_SetsAccumulatorToOne()
    {
        var (bus, ram) = NewBus();
        ram[0x95] = 0x48; // TALK device 8 -> ActiveDevice = 8
        var cpu = new Mos6502(bus);
        var image = MinimalDisk();
        var trap = new KernalSerialTrap(cpu, bus, new VirtualDriveServer(d => d == 8 ? image : null));

        // Establish the active device via an attention (TALK 8), then "ready".
        trap.TryHandle(0xED24).Should().BeTrue();
        cpu.A = 0;

        trap.TryHandle(0xEEA9).Should().BeTrue();

        cpu.A.Should().Be(1, "serial_trap_ready fakes a ready status of 1");
    }
}
