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
        bool? idleState = null,
        ushort? videoCounter = null,
        ushort? videoCounterBase = null,
        byte? rowCounter = null,
        byte? videoMatrixLineIndex = null,
        byte? refreshCounter = null,
        byte? spriteDmaActiveMask = null)
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
        _lightPenTriggerCountdown = 0;
        _irqAssertPending = false;
        // The replay image puts the rise back after this call. Leaving the
        // line released here keeps a pending rise from asserting one tick early.
        // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-08: seed the delayed $D011 copy so
        // the first post-injection g-access does not see a stale pre-injection mode.
        // A replay image puts the saved reg11_delay back after this call.
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
        // audit L10: restore the video counters and sprite DMA state exactly
        // like VICE's snapshot module (vicii-snapshot.c:105-108,131,223-227,
        // 250,270): vc/vcbase/rc/vmli/refresh_counter/sprite_dma load from
        // the snapshot; they only re-derive at frame top, so a mid-frame
        // resume without them diverges for the rest of the frame.
        if (videoCounter is { } vc)
            _videoCounter = (ushort)(vc & 0x03FF);
        if (videoCounterBase is { } vcBase)
            _vcBase = (ushort)(vcBase & 0x03FF);
        if (rowCounter is { } rc)
            _rowCounter = (byte)(rc & 0x07);
        if (videoMatrixLineIndex is { } vmli)
            _videoMatrixLineIndex = vmli % _videoBuffer.Length;
        if (refreshCounter is { } refresh)
            _refreshCounter = refresh;
        if (spriteDmaActiveMask is { } spriteDma)
            _spriteDmaActiveMask = spriteDma;
        // V4 FR-VIC-DRAW-COLOR: seed Cregs from injected registers so the first
        // rendered post-injection frame uses correct colour values without a CPU
        // write replay. Equivalent to vicii_draw_cycle_init seeding the identity
        // table plus a snapshot-restore step for 0x20-0x2E.
        _pixelSequencer.SeedCregsFromRegisters();
    }

    /// <summary>
    /// FR-SNP-003. VIC IRQ rise. InjectSnapshotState clears the pending flag
    /// and does not drive the line, so this state goes back afterwards.
    /// </summary>
    internal readonly struct IrqPipelineReplayState
    {
        public bool AssertPending { get; init; }
        public bool LineHeld { get; init; }
    }

    /// <summary>
    /// FR-SNP-003. Programmed raster compare. The register file mirrors the
    /// live raster, so InjectSnapshotState rebuilds the compare from that
    /// raster. VICE vicii-snapshot.c saves raster_irq_line and
    /// raster_irq_triggered separately. This state goes back afterwards.
    /// </summary>
    internal readonly struct RasterCompareReplayState
    {
        public ushort Line { get; init; }
        public bool Triggered { get; init; }
    }

    /// <summary>
    /// FR-SNP-003. Pending $D011. The register file already holds the new
    /// byte, and InjectSnapshotState copies that byte into the delay.
    /// VICE vicii-snapshot.c saves reg11_delay separately. This state goes
    /// back afterwards.
    /// </summary>
    internal readonly struct Reg11DelayReplayState
    {
        public byte Delay { get; init; }
    }

    /// <summary>Copies the delayed $D011 byte.</summary>
    internal Reg11DelayReplayState CaptureReg11Delay() => new()
    {
        Delay = _reg11Delay
    };

    /// <summary>
    /// Puts the delayed $D011 byte back after <see cref="InjectSnapshotState"/>.
    /// </summary>
    internal void InjectReg11Delay(Reg11DelayReplayState state)
    {
        _reg11Delay = state.Delay;
    }

    /// <summary>Copies the programmed compare and its once-per-line edge guard.</summary>
    internal RasterCompareReplayState CaptureRasterCompare() => new()
    {
        Line = _rasterIrqLine,
        Triggered = _rasterIrqTriggered
    };

    /// <summary>
    /// Puts the programmed compare back after <see cref="InjectSnapshotState"/>.
    /// </summary>
    internal void InjectRasterCompare(RasterCompareReplayState state)
    {
        _rasterIrqLine = (ushort)(state.Line & 0x01FF);
        _rasterIrqTriggered = state.Triggered;
    }

    /// <summary>Copies the one-cycle rise and whether the line is already up.</summary>
    internal IrqPipelineReplayState CaptureIrqPipeline() => new()
    {
        AssertPending = _irqAssertPending,
        LineHeld = _irqLineHeld
    };

    /// <summary>
    /// Puts the VIC IRQ rise back after <see cref="InjectSnapshotState"/>.
    /// A pending rise stays off the line until the next tick. A line that
    /// is already up matches vicii-snapshot.c interrupt_restore_irq.
    /// </summary>
    internal void InjectIrqPipeline(IrqPipelineReplayState state)
    {
        _irqAssertPending = state.AssertPending;
        if (state.LineHeld)
        {
            RefreshInterruptLine();
            _irqAssertPending = state.AssertPending;
            return;
        }

        if (_irqLineHeld)
        {
            _irqLine.Release(this);
            _irqLineHeld = false;
        }
    }

    /// <summary>
    /// FR-SNP-003. Pending frame start. InjectSnapshotState clears the latch,
    /// so this state goes back afterwards. VICE vicii-snapshot.c saves and
    /// restores start_of_frame.
    /// </summary>
    internal readonly struct FrameStartReplayState
    {
        public bool StartOfFrame { get; init; }
    }

    /// <summary>Copies the armed frame-start latch.</summary>
    internal FrameStartReplayState CaptureFrameStart() => new()
    {
        StartOfFrame = _startOfFrame
    };

    /// <summary>
    /// Puts the frame-start latch back after <see cref="InjectSnapshotState"/>.
    /// The next tick applies it when the raster cycle is 1.
    /// </summary>
    internal void InjectFrameStart(FrameStartReplayState state)
    {
        _startOfFrame = state.StartOfFrame;
    }

    /// <summary>
    /// FR-SNP-003. Light-pen and sprite-collision latches. The register file
    /// does not hold the live values $D013, $D014, $D01E, and $D01F return.
    /// VICE vicii-snapshot.c saves the light-pen block and the collision
    /// latches. InjectSnapshotState clears the pen countdown and the pending
    /// collision clear, so this state goes back afterwards.
    /// </summary>
    internal readonly struct EventLatchReplay
    {
        public byte LightPenX { get; init; }
        public byte LightPenY { get; init; }
        public bool LightPenTriggered { get; init; }
        public bool LightPenState { get; init; }
        public byte LightPenExtraBits { get; init; }
        public int LightPenCountdown { get; init; }
        public byte SpriteSprite { get; init; }
        public byte SpriteBackground { get; init; }
        public byte PendingCollisionClear { get; init; }
    }

    /// <summary>Copies the light-pen and sprite-collision latches.</summary>
    internal EventLatchReplay CaptureEventLatches() => new()
    {
        LightPenX = _lightPenLatchedX,
        LightPenY = _lightPenLatchedY,
        LightPenTriggered = _lightPenTriggeredThisFrame,
        LightPenState = _lightPenState,
        LightPenExtraBits = _lightPenXExtraBits,
        LightPenCountdown = _lightPenTriggerCountdown,
        SpriteSprite = _spriteSpriteCollisionLatch,
        SpriteBackground = _spriteBackgroundCollisionLatch,
        PendingCollisionClear = _pendingCollisionClear
    };

    /// <summary>
    /// Puts the light-pen and collision latches back after
    /// <see cref="InjectSnapshotState"/>.
    /// </summary>
    internal void InjectEventLatches(EventLatchReplay state)
    {
        _lightPenLatchedX = state.LightPenX;
        _lightPenLatchedY = state.LightPenY;
        _lightPenTriggeredThisFrame = state.LightPenTriggered;
        _lightPenState = state.LightPenState;
        _lightPenXExtraBits = state.LightPenExtraBits;
        _lightPenTriggerCountdown = state.LightPenCountdown;
        _spriteSpriteCollisionLatch = state.SpriteSprite;
        _spriteBackgroundCollisionLatch = state.SpriteBackground;
        _pendingCollisionClear = state.PendingCollisionClear;
    }

    /// <summary>
    /// FR-SNP-003. Live border flip-flops. VICE vicii-snapshot saves
    /// vborder, set_vborder, and main_border. InjectSnapshotState leaves
    /// them at the reset values.
    /// </summary>
    internal readonly struct BorderReplayState
    {
        public bool VerticalBorder { get; init; }
        public bool VerticalBorderNext { get; init; }
        public bool MainBorder { get; init; }
        public bool MainBorderOpenedThisLine { get; init; }
    }

    /// <summary>Copies the three border flip-flops VICE stores beside the registers.</summary>
    internal BorderReplayState CaptureBorder() => new()
    {
        VerticalBorder = _verticalBorderActive,
        VerticalBorderNext = _verticalBorderNextActive,
        MainBorder = _mainBorderActive,
        MainBorderOpenedThisLine = _mainBorderOpenedThisLine
    };

    /// <summary>
    /// Puts the border flip-flops back after <see cref="InjectSnapshotState"/>.
    /// Later cycles paint from these flags, so a copied frame is not enough.
    /// </summary>
    internal void InjectBorder(BorderReplayState state)
    {
        _verticalBorderActive = state.VerticalBorder;
        _verticalBorderNextActive = state.VerticalBorderNext;
        _mainBorderActive = state.MainBorder;
        _mainBorderOpenedThisLine = state.MainBorderOpenedThisLine;
    }

    /// <summary>
    /// Copies the matrix latches, the bad-line latch, and the draw pipeline.
    /// VICE stores vbuf, cbuf, bad_line, dbuf, and prefetch_cycles beside
    /// the border flip-flops.
    /// </summary>
    internal (PixelSequencer.DrawReplay Draw, byte[] Video, byte[] Color, bool BadLine, int RasterXPipe, byte SpriteDisplay, int PrefetchCycles) CaptureRendering()
    {
        return (
            _pixelSequencer.CaptureDraw(),
            _videoBuffer.ToArray(),
            _colorBuffer.ToArray(),
            _badLine,
            _rasterXPipe,
            _spriteDisplayBits,
            _prefetchCycles);
    }

    /// <summary>
    /// Puts the draw pipeline back after <see cref="InjectSnapshotState"/>.
    /// That call seeds colours from the registers and does not restore dbuf.
    /// </summary>
    internal void InjectRendering(PixelSequencer.DrawReplay draw, ReadOnlySpan<byte> video, ReadOnlySpan<byte> color, bool badLine, int rasterXPipe, byte spriteDisplay, int prefetchCycles)
    {
        _pixelSequencer.InjectDraw(draw);
        video.CopyTo(_videoBuffer.AsSpan());
        color.CopyTo(_colorBuffer.AsSpan());
        _badLine = badLine;
        _rasterXPipe = rasterXPipe;
        _spriteDisplayBits = spriteDisplay;
        _prefetchCycles = prefetchCycles;
    }

    /// <summary>
    /// Copies the per-line border record the renderer consults when a line
    /// completes. The live flip-flops do not recreate lines already started.
    /// </summary>
    internal (bool[] Vertical, bool[] VerticalCaptured, bool[] HorizontalOpen, bool[] LeftOpen, bool[] RightOpen, bool[] HorizontalCaptured) CaptureBorderLines()
    {
        return (
            _verticalBorderActiveByRasterLine.ToArray(),
            _verticalBorderLineCaptured.ToArray(),
            _horizontalDisplayOpenByRasterLine.ToArray(),
            _leftBorderOpenByRasterLine.ToArray(),
            _rightBorderOpenByRasterLine.ToArray(),
            _horizontalBorderLineCaptured.ToArray());
    }

    /// <summary>Puts the per-line border record back after injection.</summary>
    internal void InjectBorderLines(bool[] vertical, bool[] verticalCaptured, bool[] horizontalOpen, bool[] leftOpen, bool[] rightOpen, bool[] horizontalCaptured)
    {
        vertical.AsSpan().CopyTo(_verticalBorderActiveByRasterLine);
        verticalCaptured.AsSpan().CopyTo(_verticalBorderLineCaptured);
        horizontalOpen.AsSpan().CopyTo(_horizontalDisplayOpenByRasterLine);
        leftOpen.AsSpan().CopyTo(_leftBorderOpenByRasterLine);
        rightOpen.AsSpan().CopyTo(_rightBorderOpenByRasterLine);
        horizontalCaptured.AsSpan().CopyTo(_horizontalBorderLineCaptured);
    }

    /// <summary>
    /// FR-SNP-003. One sprite's execution image. VICE vicii-snapshot saves
    /// data, mc, mcbase, pointer, exp_flop, and x. Y is written only by
    /// the $D001 store, which replay does not repeat.
    /// </summary>
    internal struct SpriteReplay
    {
        public ushort X;
        public byte Y;
        public byte Pointer;
        public byte Mc;
        public byte McBase;
        public bool ExpFlop;
        public uint Data;
        public bool DataValid;
    }

    /// <summary>Copies all eight sprites' execution fields.</summary>
    internal SpriteReplay[] CaptureSprites()
    {
        var sprites = new SpriteReplay[_sprites.Length];
        for (var i = 0; i < _sprites.Length; i++)
        {
            ref readonly var sprite = ref _sprites[i];
            sprites[i] = new SpriteReplay
            {
                X = sprite.X,
                Y = sprite.Y,
                Pointer = sprite.Pointer,
                Mc = sprite.Mc,
                McBase = sprite.McBase,
                ExpFlop = sprite.ExpFlop,
                Data = sprite.Data,
                DataValid = sprite.DataValid
            };
        }

        return sprites;
    }

    /// <summary>Puts the sprite execution fields back after register injection.</summary>
    internal void InjectSprites(ReadOnlySpan<SpriteReplay> sprites)
    {
        for (var i = 0; i < sprites.Length && i < _sprites.Length; i++)
        {
            ref var sprite = ref _sprites[i];
            var state = sprites[i];
            sprite.X = state.X;
            sprite.Y = state.Y;
            sprite.Pointer = state.Pointer;
            sprite.Mc = state.Mc;
            sprite.McBase = state.McBase;
            sprite.ExpFlop = state.ExpFlop;
            sprite.Data = state.Data;
            sprite.DataValid = state.DataValid;
        }
    }

    /// <summary>
    /// Symmetric capture of the state <see cref="InjectSnapshotState"/> restores
    /// (FIX-XSNAPWARP-001: the in-app machine snapshot): the 64-byte register file,
    /// the raster phase (line + in-line cycle), the .vsf badline/display latches, and
    /// the mid-frame video counters (vc/vcbase/rc/vmli/refresh/sprite-DMA) that only
    /// re-derive at frame top.
    /// </summary>
    /// <returns>The VIC snapshot state, field-for-field what injection consumes.</returns>
    public VicSnapshotState CaptureSnapshotState() => new(
        _registers.AsSpan(0, VicRegisterBytes).ToArray(),
        CurrentRasterLine,
        (byte)RasterX,
        _allowBadLines,
        _idleState,
        _videoCounter,
        _vcBase,
        _rowCounter,
        (byte)_videoMatrixLineIndex,
        _refreshCounter,
        _spriteDmaActiveMask);

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
