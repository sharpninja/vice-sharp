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
    /// Snapshot-resume injection for cross-emulator lockstep diagnostics (PLAN-VSFLOCKSTEP /
    /// TR-LOCKSTEP-VSF-001): seeds the 64-byte register file and the raster phase (line +
    /// in-line cycle) from an external VICE snapshot so the managed VIC starts at the same
    /// point. The video counters and pipeline re-derive within a frame from the register
    /// state and raster position. The bad-line allowance does NOT re-derive mid-frame (it
    /// only arms on line $30 with DEN set; VICE viciisc/vicii-cycle.c:523-526) and the .vsf
    /// VIC-II module carries it explicitly (viciisc/vicii-snapshot.c allow_bad_lines), so
    /// resuming mid-frame must seed it or every remaining badline BA stall is lost;
    /// <paramref name="allowBadLines"/>/<paramref name="idleState"/> seed those latches
    /// (null leaves the reset-state values untouched for legacy register-only staging).
    /// Not used on the per-cycle emulation hot path.
    /// </summary>
    public void InjectSnapshotState(
        ReadOnlySpan<byte> registers,
        ushort rasterLine,
        byte inLineCycle,
        bool? allowBadLines = null,
        bool? idleState = null)
    {
        if (registers.Length < VicRegisterBytes)
            throw new ArgumentException($"Expected at least {VicRegisterBytes} register bytes.", nameof(registers));

        registers[..VicRegisterBytes].CopyTo(_registers);
        CurrentRasterLine = (ushort)(rasterLine & 0x01FF);
        RasterX = inLineCycle;
        // audit H1: seed the cycle_flags_pipe equivalent with the previous
        // cycle so the first post-injection draw consumes the correct piped
        // flags (vicii-draw-cycle.c:687). audit L10 (Phase 6) will carry the
        // real piped value through the snapshot format.
        _rasterXPipe = inLineCycle > 0 ? inLineCycle - 1 : CyclesPerLine - 1;
        // 9-bit raster compare = $D012 | ($D011 bit7 << 8).
        _rasterIrqLine = (ushort)(_registers[0x12] | ((_registers[0x11] & 0x80) << 1));
        // PLAN-VICEPARITY-001 FR-VIC-RASTER-IRQ AC-02/AC-11: seed the
        // raster_irq_triggered edge guard. Resuming mid-line ON the compare
        // line means VICE fired its once-per-line latch at that line's entry
        // already, so treat it as consumed; off the line the per-cycle
        // comparison holds it clear anyway (viciisc/vicii-cycle.c:466-474).
        _rasterIrqTriggered = CurrentRasterLine == _rasterIrqLine;
        // PLAN-VICEPARITY-001 FR-VIC-CYCLE AC-12 / FR-VIC-REGISTERS AC-14 /
        // FR-VIC-LIGHTPEN AC-06 / FR-VIC-RASTER-IRQ AC-02: injection
        // re-derives frame phase from the raster position; no armed frame
        // reset, pending collision clear, scheduled pen trigger or pending
        // IRQ-line rise survives from before the injection.
        _startOfFrame = false;
        _pendingCollisionClear = 0;
        _lightPenTriggerPending = false;
        _irqAssertPending = false;
        // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-08: seed the delayed $D011 copy so
        // the first post-injection g-access does not see a stale pre-injection mode.
        _reg11Delay = _registers[0x11];
        // PLAN-VICRENDER-001: seed the border + background colour-change logs from the injected registers.
        _borderEntryColour = _registers[0x20];
        _borderChangeCount = 0;
        _bgEntryColour = _registers[0x21];
        _bgChangeCount = 0;
        // TR-LOCKSTEP-VSF-001: seed the .vsf badline/display latches so mid-frame
        // resume reproduces the remaining badline BA stalls (the per-cycle
        // check_badline latch gates on _allowBadLines exactly like VICE).
        if (allowBadLines is { } allow)
            _allowBadLines = allow;
        if (idleState is { } idle)
            _idleState = idle;
        // V4 FR-VIC-DRAW-COLOR: seed Cregs from injected registers so the first
        // rendered post-injection frame uses correct colour values without a CPU
        // write replay. Equivalent to vicii_draw_cycle_init seeding the identity
        // table plus a snapshot-restore step for 0x20-0x2E.
        _pixelSequencer.SeedCregsFromRegisters();
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
