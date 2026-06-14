namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.Core.Snapshots;
using Xunit;

/// <summary>
/// Slice 4A/4B snapshot round-trip coverage for VIC-II registers,
/// CIA1 TOD registers, and SID ADSR registers.
/// FR-SNP-001 AC3/AC4/AC5, RUNTIME-SNAPSHOT-002, TR-STATE-001.
/// </summary>
public sealed class Slice4SnapshotRoundTripTests
{
    private sealed record TestRig(IMachine Machine, Mos6502 Cpu, Mos6526 Cia1, Sid6581 Sid);
    private sealed record TestRigWithVic(IMachine Machine, Mos6502 Cpu, Mos6526 Cia1, Sid6581 Sid, Mos6569 Vic);

    private static TestRig CreateRig()
    {
        var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());
        var cpu = (Mos6502)machine.Devices.GetByRole(DeviceRole.Cpu)!;
        var irq = new InterruptLine(InterruptType.Irq);
        var cia1 = new Mos6526(machine.Bus, irq) { BaseAddress = 0xDC00 };
        var sid = new Sid6581(machine.Bus) { BaseAddress = 0xD400 };
        machine.Bus.RegisterDevice(cia1);
        machine.Bus.RegisterDevice(sid);
        machine.Clock.Register(cia1);
        machine.Clock.Register(sid);
        return new TestRig(machine, cpu, cia1, sid);
    }

    private static TestRigWithVic CreateRigWithVic()
    {
        var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());
        var cpu = (Mos6502)machine.Devices.GetByRole(DeviceRole.Cpu)!;
        var irq = new InterruptLine(InterruptType.Irq);
        var cia1 = new Mos6526(machine.Bus, irq) { BaseAddress = 0xDC00 };
        var sid = new Sid6581(machine.Bus) { BaseAddress = 0xD400 };
        var vicIrq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(machine.Bus, vicIrq) { BaseAddress = 0xD000 };
        machine.Bus.RegisterDevice(cia1);
        machine.Bus.RegisterDevice(sid);
        machine.Bus.RegisterDevice(vic);
        machine.Clock.Register(cia1);
        machine.Clock.Register(sid);
        machine.Clock.Register(vic);
        return new TestRigWithVic(machine, cpu, cia1, sid, vic);
    }

    /// <summary>
    /// FR-SNP-001 AC3, TR-STATE-001.
    /// Use case: Write to VIC-II control registers $D011 (YSCROLL/DEN/RSEL),
    /// $D016 (CSEL/XSCROLL/MCM), and $D001 (sprite-0-Y), capture the snapshot,
    /// scramble those registers, then Restore. Bus.Peek must return the
    /// original values (modulo hardware-fixed bits).
    /// Acceptance: After Restore, Peek($D011) bits 6:0 match the written
    /// control bits; Peek($D016) bits 4:0 match the CSEL/MCM/XSCROLL bits;
    /// Peek($D001) matches exactly (sprite-Y is a plain R/W register).
    /// </summary>
    [Fact]
    public void Restore_RestoresVicIiRegisters_D011_D016_SpriteY()
    {
        var rig = CreateRigWithVic();
        var store = new RuntimeSnapshotStore();

        // Write VIC-II control register $D011: YSCROLL=3, DEN=1, RSEL=1
        rig.Machine.Bus.Write(0xD011, 0x1B);
        // Write VIC-II control register $D016: CSEL=1, XSCROLL=0, MCM=0
        rig.Machine.Bus.Write(0xD016, 0xC8);
        // Write sprite-0-Y register $D001
        rig.Machine.Bus.Write(0xD001, 0x42);

        var snapshot = store.Capture(rig.Machine);

        // Scramble: overwrite with distinct garbage values
        rig.Machine.Bus.Write(0xD011, 0x00);
        rig.Machine.Bus.Write(0xD016, 0x00);
        rig.Machine.Bus.Write(0xD001, 0x00);

        store.Restore(rig.Machine, snapshot);

        // $D011 bit 7 is the MSB of the current raster line (dynamic),
        // so we only assert bits 6:0 which are the static control bits.
        Assert.Equal(0x1B & 0x7F, rig.Machine.Bus.Peek(0xD011) & 0x7F);
        // $D016 bits 7:5 are open-collector/fixed; assert bits 4:0 only.
        Assert.Equal(0xC8 & 0x1F, rig.Machine.Bus.Peek(0xD016) & 0x1F);
        // Sprite-Y is a plain R/W byte with no side-effects.
        Assert.Equal(0x42, rig.Machine.Bus.Peek(0xD001));
    }

    /// <summary>
    /// FR-SNP-001 AC4, TR-STATE-001.
    /// Use case: Write CIA1 TOD registers ($DC08-$DC0B) to a known BCD time,
    /// capture the snapshot, overwrite with garbage, then Restore. The TOD
    /// registers must read back the original BCD values.
    /// Acceptance: After Restore, Bus.Peek for hours/mins/secs/tenths
    /// returns the programmed BCD bytes.
    /// </summary>
    [Fact]
    public void Restore_RestoresCia1TodRegisters()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();

        // Ensure timer-B control bit 7 is 0 so writes target the clock, not the alarm.
        rig.Machine.Bus.Write(0xDC0F, 0x00);

        // Write TOD: 01 hours, 23 mins, 45 secs, 06 tenths (all BCD)
        rig.Machine.Bus.Write(0xDC0B, 0x01); // hours
        rig.Machine.Bus.Write(0xDC0A, 0x23); // mins
        rig.Machine.Bus.Write(0xDC09, 0x45); // secs
        rig.Machine.Bus.Write(0xDC08, 0x06); // tenths

        var snapshot = store.Capture(rig.Machine);

        // Scramble: write zeroes to all TOD registers
        rig.Machine.Bus.Write(0xDC0F, 0x00);
        rig.Machine.Bus.Write(0xDC0B, 0x00);
        rig.Machine.Bus.Write(0xDC0A, 0x00);
        rig.Machine.Bus.Write(0xDC09, 0x00);
        rig.Machine.Bus.Write(0xDC08, 0x00);

        store.Restore(rig.Machine, snapshot);

        // Peek bypasses the latch-on-read behavior, reading internal state directly.
        var hours  = rig.Machine.Bus.Peek(0xDC0B);
        var mins   = rig.Machine.Bus.Peek(0xDC0A);
        var secs   = rig.Machine.Bus.Peek(0xDC09);
        var tenths = rig.Machine.Bus.Peek(0xDC08);

        Assert.Equal(0x01, hours);
        Assert.Equal(0x23, mins);
        Assert.Equal(0x45, secs);
        Assert.Equal(0x06, tenths);
    }

    /// <summary>
    /// FR-SNP-001 AC5, TR-STATE-001.
    /// Use case: Write SID voice-1 attack/decay ($D405) and sustain/release
    /// ($D406) ADSR registers, capture the snapshot, overwrite with garbage,
    /// then Restore. Bus.Peek must return the original bytes.
    /// Acceptance: After Restore, Bus.Peek($D405) and Peek($D406) return
    /// the snapshot values 0xA9 and 0xF3.
    /// </summary>
    [Fact]
    public void Restore_RestoresSidAdsr()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();

        // Voice 1 ADSR: attack/decay = 0xA9, sustain/release = 0xF3
        rig.Machine.Bus.Write(0xD405, 0xA9);
        rig.Machine.Bus.Write(0xD406, 0xF3);

        var snapshot = store.Capture(rig.Machine);

        rig.Machine.Bus.Write(0xD405, 0x00);
        rig.Machine.Bus.Write(0xD406, 0x00);

        store.Restore(rig.Machine, snapshot);

        Assert.Equal(0xA9, rig.Machine.Bus.Peek(0xD405));
        Assert.Equal(0xF3, rig.Machine.Bus.Peek(0xD406));
    }
}
