using System.Buffers.Binary;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;

namespace ViceSharp.Host.Runtime;

/// <summary>
/// FIX-XSNAPWARP-001 (operator 2026-07-14: "After clicking LOAD, the emulator loads the
/// snapshot but restarts in Warp mode"). The v2 in-app machine snapshot: captures TRUE
/// machine state and restores it through the chip injectors the VSF lockstep rig proved
/// cycle-exact (TR-LOCKSTEP-VSF-001), replacing the v1 CPU-regs-plus-<c>Bus.Write</c>
/// replay that pushed capture-time register READ values back through live I/O registers
/// (scrambling the CIA ICR masks and timer latches, killing the game's IRQ pacing).
/// </summary>
/// <remarks>
/// Captured: the 64KB RAM array under any banking, color RAM, the 6510 port
/// (direction + data), the CPU register file, full VIC-II and CIA1/CIA2 snapshot state,
/// and the SID register file. Known limits: SID internals (envelopes/oscillators) are
/// restored by register replay, so held notes retrigger their attack; banked-cartridge
/// bank registers and drive state are not captured.
/// </remarks>
public sealed class MachineSnapshotStager
{
    /// <summary>The v2 machine snapshot format id carried in <see cref="ViceSharp.Protocol.SnapshotDto"/>.</summary>
    public const string FormatV2 = "vice-sharp.machine-snapshot.v2";

    private const byte PayloadVersion = 2;
    private const int RamBytes = 0x10000;
    private const int ColorBytes = 0x0400;
    private const int VicRegisterBytes = 64;
    private const int SidRegisterBytes = 32;
    private const int SidWritableRegisters = 0x19; // $D400-$D418; $D419+ are read-only.

    private const int PayloadBytes =
        1 + RamBytes + ColorBytes + 2 /* pport */ + 7 /* cpu */
        + VicRegisterBytes + 12 /* vic phase + counters */
        + 16 + 16 /* cia1 + cia2 */
        + SidRegisterBytes;

    /// <summary>Captures the machine's snapshot payload (side-effect free).</summary>
    /// <param name="machine">The C64 machine to capture.</param>
    /// <returns>The v2 payload.</returns>
    public byte[] Capture(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        var (ram, cpu, vic, cia1, cia2) = ResolveDevices(machine);

        var payload = new byte[PayloadBytes];
        var span = payload.AsSpan();
        var offset = 0;

        span[offset++] = PayloadVersion;

        ram.Span.CopyTo(span.Slice(offset, RamBytes));
        offset += RamBytes;

        // The board wires the VIC's video-memory reader to the memory map, which
        // resolves $D800-$DBFF to the color nybbles regardless of CPU banking.
        var readVideoMemory = vic.VideoMemoryReader;
        for (var i = 0; i < ColorBytes; i++)
            span[offset + i] = readVideoMemory((ushort)(0xD800 + i));
        offset += ColorBytes;

        span[offset++] = machine.Bus.Peek(0x0000);
        span[offset++] = machine.Bus.Peek(0x0001);

        var cpuState = machine.GetState();
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), cpuState.PC);
        offset += 2;
        span[offset++] = cpuState.A;
        span[offset++] = cpuState.X;
        span[offset++] = cpuState.Y;
        span[offset++] = cpuState.S;
        span[offset++] = cpuState.P;

        var vicState = vic.CaptureSnapshotState();
        vicState.Registers.AsSpan(0, VicRegisterBytes).CopyTo(span.Slice(offset, VicRegisterBytes));
        offset += VicRegisterBytes;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), vicState.RasterLine);
        offset += 2;
        span[offset++] = vicState.InLineCycle;
        span[offset++] = (byte)((vicState.AllowBadLines ? 0x01 : 0) | (vicState.IdleState ? 0x02 : 0));
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), vicState.VideoCounter);
        offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), vicState.VideoCounterBase);
        offset += 2;
        span[offset++] = vicState.RowCounter;
        span[offset++] = vicState.VideoMatrixLineIndex;
        span[offset++] = vicState.RefreshCounter;
        span[offset++] = vicState.SpriteDmaActiveMask;

        offset = WriteCia(span, offset, cia1.CaptureSnapshotState());
        offset = WriteCia(span, offset, cia2.CaptureSnapshotState());

        if (machine.Devices.GetByRole(DeviceRole.AudioChip) is IAddressSpace sid)
        {
            for (var r = 0; r < SidRegisterBytes; r++)
                span[offset + r] = sid.Peek((ushort)(0xD400 + r));
        }

        offset += SidRegisterBytes;
        if (offset != PayloadBytes)
            throw new InvalidOperationException($"Snapshot payload layout drifted: wrote {offset} of {PayloadBytes} bytes.");

        return payload;
    }

    /// <summary>
    /// Restores a v2 payload into the machine: RAM and color RAM first, the SID
    /// register file by replay (with I/O banked in), then the VIC/CIA/CPU injections,
    /// and the 6510 port LAST so the snapshot's banking is final.
    /// </summary>
    /// <param name="machine">The C64 machine to restore into.</param>
    /// <param name="payload">A payload produced by <see cref="Capture"/>.</param>
    public void Restore(IMachine machine, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(machine);

        if (payload.Length != PayloadBytes || payload[0] != PayloadVersion)
            throw new ArgumentException(
                $"Expected a v{PayloadVersion} machine snapshot payload of {PayloadBytes} bytes.", nameof(payload));

        var (ram, cpu, vic, cia1, cia2) = ResolveDevices(machine);
        var offset = 1;

        payload.Slice(offset, RamBytes).CopyTo(ram.Span);
        offset += RamBytes;

        var color = payload.Slice(offset, ColorBytes);
        offset += ColorBytes;

        var pportDir = payload[offset++];
        var pportData = payload[offset++];

        var pc = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var a = payload[offset++];
        var x = payload[offset++];
        var y = payload[offset++];
        var s = payload[offset++];
        var p = payload[offset++];

        // Bank I/O in (default map) for the color RAM and SID register replay; the
        // snapshot's own port values land last.
        machine.Bus.Write(0x0000, 0x2F);
        machine.Bus.Write(0x0001, 0x37);

        for (var i = 0; i < ColorBytes; i++)
            machine.Bus.Write((ushort)(0xD800 + i), color[i]);

        var vicRegisters = payload.Slice(offset, VicRegisterBytes);
        offset += VicRegisterBytes;
        var rasterLine = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var inLineCycle = payload[offset++];
        var vicFlags = payload[offset++];
        var vc = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var vcBase = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var rc = payload[offset++];
        var vmli = payload[offset++];
        var refresh = payload[offset++];
        var spriteDma = payload[offset++];

        var cia1State = ReadCia(payload, ref offset);
        var cia2State = ReadCia(payload, ref offset);

        for (var r = 0; r < SidWritableRegisters; r++)
            machine.Bus.Write((ushort)(0xD400 + r), payload[offset + r]);
        offset += SidRegisterBytes;

        vic.InjectSnapshotState(
            vicRegisters,
            rasterLine,
            inLineCycle,
            allowBadLines: (vicFlags & 0x01) != 0,
            idleState: (vicFlags & 0x02) != 0,
            videoCounter: vc,
            videoCounterBase: vcBase,
            rowCounter: rc,
            videoMatrixLineIndex: vmli,
            refreshCounter: refresh,
            spriteDmaActiveMask: spriteDma);

        InjectCia(cia1, cia1State);
        InjectCia(cia2, cia2State);

        machine.Bus.Write(0x0000, pportDir);
        machine.Bus.Write(0x0001, pportData);

        cpu.InjectSnapshotResumeState(a, x, y, s, p, pc);
    }

    /// <summary>Whether the machine has the C64 device shape this stager snapshots.</summary>
    /// <param name="machine">The machine to test.</param>
    /// <returns><c>true</c> for a C64-shaped machine.</returns>
    public static bool CanSnapshot(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        return machine.Devices.GetByRole(DeviceRole.SystemRam) is IMemory
            && machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502
            && machine.Devices.GetByRole(DeviceRole.VideoChip) is Mos6569
            && machine.Devices.GetByRole(DeviceRole.Cia1) is Mos6526
            && machine.Devices.GetByRole(DeviceRole.Cia2) is Mos6526;
    }

    private static (IMemory Ram, Mos6502 Cpu, Mos6569 Vic, Mos6526 Cia1, Mos6526 Cia2)
        ResolveDevices(IMachine machine)
    {
        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory ram)
            throw new InvalidOperationException("Machine snapshots require system RAM exposed as IMemory.");
        if (machine.Devices.GetByRole(DeviceRole.Cpu) is not Mos6502 cpu)
            throw new InvalidOperationException("Machine snapshots require an Mos6502-family CPU.");
        if (machine.Devices.GetByRole(DeviceRole.VideoChip) is not Mos6569 vic)
            throw new InvalidOperationException("Machine snapshots require an Mos6569-family VIC.");
        if (machine.Devices.GetByRole(DeviceRole.Cia1) is not Mos6526 cia1)
            throw new InvalidOperationException("Machine snapshots require CIA1.");
        if (machine.Devices.GetByRole(DeviceRole.Cia2) is not Mos6526 cia2)
            throw new InvalidOperationException("Machine snapshots require CIA2.");

        return (ram, cpu, vic, cia1, cia2);
    }

    private static int WriteCia(Span<byte> span, int offset, CiaSnapshotState state)
    {
        span[offset++] = state.PortA;
        span[offset++] = state.PortB;
        span[offset++] = state.DdrA;
        span[offset++] = state.DdrB;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), state.TimerACounter);
        offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), state.TimerALatch);
        offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), state.TimerBCounter);
        offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), state.TimerBLatch);
        offset += 2;
        span[offset++] = state.Cra;
        span[offset++] = state.Crb;
        span[offset++] = state.InterruptFlags;
        span[offset++] = state.IrqMask;
        return offset;
    }

    private static CiaSnapshotState ReadCia(ReadOnlySpan<byte> payload, ref int offset)
    {
        var portA = payload[offset++];
        var portB = payload[offset++];
        var ddrA = payload[offset++];
        var ddrB = payload[offset++];
        var timerACounter = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var timerALatch = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var timerBCounter = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var timerBLatch = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2));
        offset += 2;
        var cra = payload[offset++];
        var crb = payload[offset++];
        var interruptFlags = payload[offset++];
        var irqMask = payload[offset++];
        return new CiaSnapshotState(
            portA, portB, ddrA, ddrB,
            timerACounter, timerALatch, timerBCounter, timerBLatch,
            cra, crb, interruptFlags, irqMask);
    }

    private static void InjectCia(Mos6526 cia, CiaSnapshotState state) => cia.InjectSnapshotState(
        state.PortA,
        state.PortB,
        state.DdrA,
        state.DdrB,
        state.TimerACounter,
        state.TimerALatch,
        state.TimerBCounter,
        state.TimerBLatch,
        state.Cra,
        state.Crb,
        state.InterruptFlags,
        state.IrqMask);
}
