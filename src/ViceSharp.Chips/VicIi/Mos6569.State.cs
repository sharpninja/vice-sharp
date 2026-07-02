using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// FR-TICKHIST-CHIP-VIC: VIC-II full-state capture for the time-travel debugger. Serializes
/// the 64-byte register file plus the key live internals (current raster line/cycle, the VC
/// pipeline, refresh, sprite-DMA mask, border/idle/bad-line flags) so each captured tick can
/// show the VIC exactly as it was then.
/// </summary>
public partial class Mos6569 : IStatefulDevice
{
    private const int VicRegisterBytes = 64;
    private const int VicInternalBytes = 12; // raster(2)+irq(2)+vc(2)+rc(1)+vcbase(2)+refresh(1)+spriteDma(1)+flags(1)

    public string StateName => "VIC-II";

    public int StateSize => VicRegisterBytes + VicInternalBytes;

    /// <summary>
    /// Snapshot-resume injection for cross-emulator lockstep diagnostics (PLAN-VSFLOCKSTEP):
    /// seeds the 64-byte register file and the raster phase (line + in-line cycle) from an
    /// external VICE snapshot so the managed VIC starts at the same point. The bad-line
    /// allowance, video counters, and pipeline re-derive within a frame from the register
    /// state and raster position, so this is sufficient to drive a self-correcting raster
    /// engine. Not used on the per-cycle emulation hot path.
    /// </summary>
    public void InjectSnapshotState(ReadOnlySpan<byte> registers, ushort rasterLine, byte inLineCycle)
    {
        if (registers.Length < VicRegisterBytes)
            throw new ArgumentException($"Expected at least {VicRegisterBytes} register bytes.", nameof(registers));

        registers[..VicRegisterBytes].CopyTo(_registers);
        CurrentRasterLine = (ushort)(rasterLine & 0x01FF);
        RasterX = inLineCycle;
        // 9-bit raster compare = $D012 | ($D011 bit7 << 8).
        _rasterIrqLine = (ushort)(_registers[0x12] | ((_registers[0x11] & 0x80) << 1));
        // PLAN-VICRENDER-001: seed the border + background colour-change logs from the injected registers.
        _borderEntryColour = _registers[0x20];
        _borderChangeCount = 0;
        _bgEntryColour = _registers[0x21];
        _bgChangeCount = 0;
    }

    public void CaptureState(Span<byte> destination)
    {
        _registers.AsSpan(0, VicRegisterBytes).CopyTo(destination);

        var offset = VicRegisterBytes;
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), CurrentRasterLine); offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), _rasterIrqLine); offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), _videoCounter); offset += 2;
        destination[offset++] = _rowCounter;
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), _vcBase); offset += 2;
        destination[offset++] = _refreshCounter;
        destination[offset++] = _spriteDmaActiveMask;

        byte flags = 0;
        if (_verticalBorderActive) flags |= 0x01;
        if (_mainBorderActive) flags |= 0x02;
        if (_idleState) flags |= 0x04;
        if (_allowBadLines) flags |= 0x08;
        destination[offset] = flags;
    }

    public IReadOnlyList<ChipStateField> DecodeState(ReadOnlySpan<byte> state)
    {
        var fields = new List<ChipStateField>(24)
        {
            new("$D011 CTRL1", state[0x11]),
            new("$D016 CTRL2", state[0x16]),
            new("$D018 MEMPTR", state[0x18]),
            new("$D015 SPR-ENA", state[0x15]),
            new("$D019 IRQ", state[0x19]),
            new("$D01A IRQMASK", state[0x1A]),
            new("$D020 BORDER", state[0x20]),
            new("$D021 BG0", state[0x21]),
        };

        var offset = VicRegisterBytes;
        fields.Add(new("RASTER", BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(offset, 2)), 2)); offset += 2;
        fields.Add(new("RASTER-IRQ", BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(offset, 2)), 2)); offset += 2;
        fields.Add(new("VC", BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(offset, 2)), 2)); offset += 2;
        fields.Add(new("RC", state[offset++]));
        fields.Add(new("VCBASE", BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(offset, 2)), 2)); offset += 2;
        fields.Add(new("REFRESH", state[offset++]));
        fields.Add(new("SPR-DMA", state[offset++]));

        var flags = state[offset];
        fields.Add(new("V-BORDER", (flags & 0x01) != 0 ? 1 : 0));
        fields.Add(new("M-BORDER", (flags & 0x02) != 0 ? 1 : 0));
        fields.Add(new("IDLE", (flags & 0x04) != 0 ? 1 : 0));
        fields.Add(new("BAD-LINES", (flags & 0x08) != 0 ? 1 : 0));
        return fields;
    }
}
