namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using ViceSharp.Core.Snapshots;
using Xunit;

/// <summary>
/// RUNTIME-SNAPSHOT-002 extended round-trip coverage. The baseline tests
/// in RuntimeSnapshotTests.cs only verify RAM and the public PC/P registers
/// survive a Capture/Restore cycle. These tests close the gap for the rest
/// of the CPU register file (A/X/Y/S), for chip-visible state that lives
/// behind the I/O bank (CIA1 timer latch/counter, SID voice 1 register
/// state), and for byte-exact determinism of a load-and-replay sequence.
///
/// The fixture uses the EmptyMachine architecture (no ROM artefacts needed)
/// and layers a Mos6526 at $DC00 plus a Sid6581 at $D400 on top of the
/// base BasicBus. Because BasicBus dispatches to the most recently
/// registered IAddressSpace first, the chip writes/reads intercept the
/// SimpleRam fallback for their respective I/O windows.
/// </summary>
public sealed class SnapshotRoundTripTests
{
    private sealed record TestRig(IMachine Machine, Mos6502 Cpu, Mos6526 Cia1, Sid6581 Sid);

    private static TestRig CreateRig()
    {
        var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());
        var cpu = (Mos6502)machine.Devices.GetByRole(DeviceRole.Cpu)!;
        var irq = new InterruptLine(InterruptType.Irq);
        var cia1 = new Mos6526(machine.Bus, irq) { BaseAddress = 0xDC00 };
        var sid = new Sid6581(machine.Bus);
        machine.Bus.RegisterDevice(cia1);
        machine.Bus.RegisterDevice(sid);
        machine.Clock.Register(cia1);
        machine.Clock.Register(sid);
        return new TestRig(machine, cpu, cia1, sid);
    }

    /// <summary>
    /// FR: FR-SNAPSHOT (RUNTIME-SNAPSHOT-002), TR: TR-STATE-001.
    /// Use case: Mutate the CPU A/X/Y/S registers directly, capture the
    /// snapshot, then scramble the live CPU register file and Restore.
    /// MachineState.A/X/Y/S after Restore must match the snapshot values
    /// so that continued execution sees the captured register file rather
    /// than the scrambled one.
    /// Acceptance: After Restore, MachineState.A/X/Y/S equal the values
    /// programmed before Capture (0xAB, 0xCD, 0xEF, 0x42).
    /// </summary>
    [Fact]
    public void Restore_RestoresFullCpuRegisterFile_AXYS()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();

        rig.Cpu.A = 0xAB;
        rig.Cpu.X = 0xCD;
        rig.Cpu.Y = 0xEF;
        rig.Cpu.S = 0x42;

        var snapshot = store.Capture(rig.Machine);

        rig.Cpu.A = 0x11;
        rig.Cpu.X = 0x22;
        rig.Cpu.Y = 0x33;
        rig.Cpu.S = 0x44;

        store.Restore(rig.Machine, snapshot);

        var state = rig.Machine.GetState();
        Assert.Equal(0xAB, state.A);
        Assert.Equal(0xCD, state.X);
        Assert.Equal(0xEF, state.Y);
        Assert.Equal(0x42, state.S);
    }

    /// <summary>
    /// FR: FR-SNAPSHOT (RUNTIME-SNAPSHOT-002), TR: TR-STATE-001.
    /// Use case: Write known bytes across the CPU-visible memory map
    /// (zero page, stack, BASIC RAM, free RAM near $C000), snapshot,
    /// scribble different bytes into the same locations, then Restore.
    /// The probed addresses must read back the snapshot values via the
    /// CPU bus.
    /// Acceptance: For each probed address, Bus.Peek returns the snapshot
    /// value after Restore (not the scribbled value).
    /// </summary>
    [Fact]
    public void Restore_RestoresFull64kRamImage()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();
        var probes = new (ushort Address, byte Value)[]
        {
            (0x0002, 0xA0),
            (0x0801, 0x42),
            (0x1FFF, 0x55),
            (0xC000, 0x99),
            (0xCFFF, 0x77),
        };

        foreach (var (address, value) in probes)
            rig.Machine.Bus.Write(address, value);

        var snapshot = store.Capture(rig.Machine);

        foreach (var (address, _) in probes)
            rig.Machine.Bus.Write(address, 0x00);

        store.Restore(rig.Machine, snapshot);

        foreach (var (address, value) in probes)
            Assert.Equal(value, rig.Machine.Bus.Peek(address));
    }

    /// <summary>
    /// FR: FR-SNAPSHOT (RUNTIME-SNAPSHOT-002), TR: TR-STATE-001.
    /// Use case: Program CIA1 Timer A latch/counter via $DC04/$DC05 with
    /// the timer stopped, capture the snapshot, write garbage to the same
    /// registers, then Restore. Reading $DC04/$DC05 via the CPU bus must
    /// return the snapshot low/high counter bytes - the round-trip works
    /// because the snapshot replays the $DC05 high-byte write while the
    /// timer is stopped, which reloads counter from the latch per 6526
    /// spec.
    /// Acceptance: After Restore, Bus.Peek($DC04) and Peek($DC05) match
    /// the values programmed before Capture; the CIA1 device exposes
    /// the same counter low/high pair through its direct read API.
    /// </summary>
    [Fact]
    public void Restore_RestoresCia1TimerACounterAndLatch()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();

        rig.Machine.Bus.Write(0xDC0E, 0x00);
        rig.Machine.Bus.Write(0xDC04, 0x34);
        rig.Machine.Bus.Write(0xDC05, 0x12);

        Assert.Equal(0x34, rig.Machine.Bus.Peek(0xDC04));
        Assert.Equal(0x12, rig.Machine.Bus.Peek(0xDC05));

        var snapshot = store.Capture(rig.Machine);

        rig.Machine.Bus.Write(0xDC04, 0xFF);
        rig.Machine.Bus.Write(0xDC05, 0xFF);

        store.Restore(rig.Machine, snapshot);

        Assert.Equal(0x34, rig.Machine.Bus.Peek(0xDC04));
        Assert.Equal(0x12, rig.Machine.Bus.Peek(0xDC05));

        var counter = (rig.Cia1.Read(0xDC05) << 8) | rig.Cia1.Read(0xDC04);
        Assert.Equal(0x1234, counter);
    }

    /// <summary>
    /// FR: FR-SNAPSHOT (RUNTIME-SNAPSHOT-002), TR: TR-STATE-001.
    /// Use case: Program SID voice 1 frequency ($D400/$D401) and control
    /// ($D404) registers, capture the snapshot, overwrite those registers
    /// with different bytes, then Restore. The bus must read back the
    /// snapshot values, and the SID device's own register shadow must
    /// reflect them (proving the restore replayed via Sid6581.Write, not
    /// just the RAM image).
    /// Acceptance: After Restore, Bus.Peek and Sid6581.Peek for
    /// $D400/$D401/$D404 each return the snapshot bytes
    /// (0x37, 0x10, 0x41).
    /// </summary>
    [Fact]
    public void Restore_RestoresSidVoice1FrequencyAndControl()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();

        rig.Machine.Bus.Write(0xD400, 0x37);
        rig.Machine.Bus.Write(0xD401, 0x10);
        rig.Machine.Bus.Write(0xD404, 0x41);

        var snapshot = store.Capture(rig.Machine);

        rig.Machine.Bus.Write(0xD400, 0x00);
        rig.Machine.Bus.Write(0xD401, 0x00);
        rig.Machine.Bus.Write(0xD404, 0x00);

        store.Restore(rig.Machine, snapshot);

        Assert.Equal(0x37, rig.Machine.Bus.Peek(0xD400));
        Assert.Equal(0x10, rig.Machine.Bus.Peek(0xD401));
        Assert.Equal(0x41, rig.Machine.Bus.Peek(0xD404));

        Assert.Equal(0x37, rig.Sid.Peek(0xD400));
        Assert.Equal(0x10, rig.Sid.Peek(0xD401));
        Assert.Equal(0x41, rig.Sid.Peek(0xD404));
    }

    /// <summary>
    /// FR: FR-SNAPSHOT (RUNTIME-SNAPSHOT-002), TR: TR-STATE-001.
    /// Use case: Capture a baseline snapshot, write a fixed pattern into
    /// the memory map (acting as the "forward" mutation in lieu of a
    /// cycle-stepping replay, since the chips' free-running counter state
    /// is not captured by this slice), Restore to baseline, then write
    /// the same pattern again. Snapshot the result. The post-replay RAM
    /// image must be byte-identical to the forward snapshot.
    /// Acceptance: The 64K memory image returned by Capture is byte-for-
    /// byte identical between the forward path and the restore-and-replay
    /// path, proving the snapshot + replay loop is deterministic for the
    /// captured memory surface.
    /// </summary>
    [Fact]
    public void Snapshot_Replay_IsByteExactForMemoryImage()
    {
        var rig = CreateRig();
        var store = new RuntimeSnapshotStore();

        var baseline = (RuntimeSnapshot)store.Capture(rig.Machine);

        ApplyForwardPattern(rig.Machine.Bus);
        var forward = (RuntimeSnapshot)store.Capture(rig.Machine);

        store.Restore(rig.Machine, baseline);
        ApplyForwardPattern(rig.Machine.Bus);
        var replay = (RuntimeSnapshot)store.Capture(rig.Machine);

        Assert.Equal(forward.Memory.ToArray(), replay.Memory.ToArray());
    }

    private static void ApplyForwardPattern(IBus bus)
    {
        for (var i = 0; i < 64; i++)
        {
            bus.Write((ushort)(0x4000 + i), (byte)(i * 7 ^ 0x5A));
        }

        bus.Write(0xDC0E, 0x00);
        bus.Write(0xDC04, 0x78);
        bus.Write(0xDC05, 0x56);

        bus.Write(0xD400, 0xAB);
        bus.Write(0xD401, 0x07);
        bus.Write(0xD404, 0x11);
    }
}
