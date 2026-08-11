using ViceSharp.Abstractions;

// Vic20VideoLockstepState lives in Abstractions for native/managed compare.

namespace ViceSharp.Chips.Vic;

/// <summary>
/// MOS 6560/6561 VIC-I: cycle fetch + line draw aligned to VICE
/// <c>vic-cycle.c</c> / <c>vic-draw.c</c> / <c>vic-mem.c</c> / <c>vic-timing.h</c>
/// for the normal-border READY path. Full Exact (interlace, lightpen, half-char
/// mid-line, sound) is not claimed — see <c>docs/audit-vic20-vs-vice-*.md</c>.
/// Machine-agnostic; the board supplies bus peeks for matrix/color/chargen.
/// </summary>
/// <remarks>FR-VIC20-001. $900F: bits 0-2 border, bit 3 reverse, bits 4-7 background.</remarks>
public sealed class Mos6561 : IVideoChip, IAddressSpace, IInterruptSource
{
    /// <summary>
    /// VIC-I 16-color palette as 0xAARRGGBB (B at low byte for framebuffer writers).
    /// Source: VICE <c>data/VIC20/PALette.vpl</c> (PALette_6561-101_v1 by Tobias).
    /// </summary>
    private static readonly uint[] Palette =
    [
        PackRgb(0x00, 0x00, 0x00), // 0 black
        PackRgb(0xFF, 0xFF, 0xFF), // 1 white
        PackRgb(0x97, 0x2A, 0x2E), // 2 red
        PackRgb(0x64, 0xE3, 0xDE), // 3 cyan
        PackRgb(0xAD, 0x3C, 0xBF), // 4 purple
        PackRgb(0x5C, 0xDC, 0x54), // 5 green
        PackRgb(0x40, 0x32, 0xB9), // 6 blue
        PackRgb(0xD7, 0xE7, 0x45), // 7 yellow
        PackRgb(0xBA, 0x6A, 0x24), // 8 orange
        PackRgb(0xE1, 0xB9, 0x96), // 9 light orange
        PackRgb(0xDA, 0xA3, 0xA5), // 10 light red
        PackRgb(0xB6, 0xF6, 0xF3), // 11 light cyan
        PackRgb(0xDE, 0xA6, 0xE8), // 12 light purple
        PackRgb(0xB2, 0xF3, 0xAF), // 13 light green
        PackRgb(0xA6, 0x9E, 0xE2), // 14 light blue
        PackRgb(0xF4, 0xFC, 0xAB), // 15 light yellow
    ];

    private static uint PackRgb(byte r, byte g, byte b)
        => 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;

    /// <summary>
    /// VICE <c>VIC_PIXEL_WIDTH</c> with <c>VIC_DUPLICATES_PIXELS</c> (victypes.h).
    /// </summary>
    public const int PixelWidth = 2;

    /// <summary>VICE <c>VIC_MAX_TEXT_COLS</c> / PAL max.</summary>
    public const int MaxTextCols = 32;

    // VICE vic-timing.h VIC_*_NORMAL_*
    private const int PalNormalDisplayWidth = 224;
    private const int PalNormalLeftBorderCycles = 12;
    private const int PalNormalFirstDisplayedLine = 28;
    private const int PalNormalLastDisplayedLine = 311;
    private const int NtscNormalDisplayWidth = 200;
    private const int NtscNormalLeftBorderCycles = 4;
    private const int NtscNormalFirstDisplayedLine = 28;
    private const int NtscNormalLastDisplayedLine = 261;
    // VICE vic.c geometry: typical KERNAL Y origin 38 * 2.
    private const int ViceTopBorderBase = 38 * 2;

    /// <summary>
    /// Full-line paper origin X for KERNAL PAL <c>$9000=12</c>
    /// (<c>12 * 4 * VIC_PIXEL_WIDTH</c>). Not the normal-border canvas origin.
    /// </summary>
    public const int FullLineOriginXKernalPal = PalNormalLeftBorderCycles * 4 * PixelWidth;

    /// <summary>
    /// Visible left border width / paper origin X in the <em>normal-border framebuffer</em>
    /// for KERNAL PAL origin 12 and 22 columns. VICE <c>video_viewport_resize</c>
    /// sets <c>first_x = 48</c>; paper at full-line 96 maps to canvas x 48.
    /// </summary>
    public const int BorderX = 48;

    /// <summary>Top border pixels for normal borders with KERNAL $9001=38.</summary>
    public const int BorderY = ViceTopBorderBase - PalNormalFirstDisplayedLine;

    /// <summary>
    /// VICE <c>video_viewport_resize</c> horizontal window start into the full
    /// draw-buffer line (<c>video-viewport.c</c>). VIC-I sets
    /// <c>gfx_area_moves = 1</c> (<c>vic_set_geometry</c>), so the non-moving clamp
    /// is not applied.
    /// </summary>
    /// <param name="gfxX">Full-line gfx origin (<c>display_xstart</c> / geometry gfx_position.x).</param>
    /// <param name="gfxW">Full-line gfx width (<c>text_cols * 8 * PIXEL_WIDTH</c>).</param>
    /// <param name="canvasW">Visible canvas width (<c>display_width * PIXEL_WIDTH</c>).</param>
    /// <param name="screenW">Full line width (<c>screen_width * PIXEL_WIDTH</c>).</param>
    /// <param name="gfxAreaMoves">VICE geometry flag; VIC-I is true.</param>
    public static int ComputeViewportFirstX(
        int gfxX,
        int gfxW,
        int canvasW,
        int screenW,
        bool gfxAreaMoves = true)
    {
        if (canvasW <= 0 || screenW <= 0 || gfxW < 0)
            return 0;

        // small_x_border = min(left border, right border) on the full line
        var rightBorder = screenW - gfxX - gfxW;
        var smallXBorder = rightBorder;
        if (smallXBorder > gfxX)
            smallXBorder = gfxX;
        if (smallXBorder < 0)
            smallXBorder = 0;

        int firstX;
        // video-viewport.c: if gfx fits only with symmetric small borders, center paper
        if (gfxW + smallXBorder * 2 > canvasW)
            firstX = gfxX - (canvasW - gfxW) / 2;
        else if (gfxX > smallXBorder)
            firstX = screenW - canvasW;
        else
            firstX = 0;

        if (firstX < 0)
            firstX = 0;
        // Stop at the border unless gfx_area_moves (VIC-I: moves=1 → skip clamp)
        if (!gfxAreaMoves && firstX > gfxX)
            firstX = gfxX;
        if (firstX + canvasW > screenW)
            firstX = Math.Max(0, screenW - canvasW);
        return firstX;
    }

    /// <summary>VICE area state (<c>vic.area</c>).</summary>
    private enum VicArea : byte
    {
        Idle = 0,
        Pending = 1,
        Display = 2,
        Done = 3,
    }

    /// <summary>VICE <c>vic.fetch_state</c>.</summary>
    private enum FetchState : byte
    {
        Idle = 0,
        Start = 1,
        Matrix = 2,
        Chargen = 3,
        Done = 4,
    }

    private readonly IInterruptLine? _irqLine;
    private readonly byte[] _regs = new byte[16];
    /// <summary>VICE <c>vic.cbuf</c> color nybbles for the current line.</summary>
    private readonly byte[] _cbuf = new byte[MaxTextCols];
    /// <summary>VICE <c>vic.gbuf</c> glyph bytes for the current line.</summary>
    private readonly byte[] _gbuf = new byte[MaxTextCols];

    private int _cycleInLine;
    private int _cyclesPerLine = 71;
    private int _totalLines = 312;
    private int _visibleLines = 284;
    private int _columns = 22;
    private int _rows = 23;
    private int _frameWidth;
    private int _frameHeight;
    private byte[] _frameBuffer;
    /// <summary>Palette-index canvas (one byte per pixel), parallel to BGRA FrameBuffer.</summary>
    private byte[] _indexFrameBuffer = Array.Empty<byte>();
    private byte[] _indexLineBuffer = Array.Empty<byte>();
    /// <summary>
    /// VICE full raster line buffer (screen_width * VIC_PIXEL_WIDTH), before
    /// normal-border crop into <see cref="_frameBuffer"/>.
    /// </summary>
    private byte[] _lineBuffer = Array.Empty<byte>();
    private int _lineBufferWidth;
    private ushort _rasterLine;

    private VicArea _area = VicArea.Idle;
    private FetchState _fetchState = FetchState.Idle;
    private int _bufOffset;
    private byte _vbuf;
    private int _memptr;
    private int _memptrInc;
    private int _ycounter;
    private int _rowCounter;
    private int _textLines = 23;
    private int _textCols = 0;
    private int _pendingTextCols = 22;
    private int _charHeight = 8;
    private bool _lineWasBlank;
    private bool _blankThisLine;
    /// <summary>VICE <c>vic.raster.display_xstart</c> in full-line pixels.</summary>
    private int _displayXStart;
    /// <summary>VICE <c>vic.raster.display_xstop</c> in full-line pixels.</summary>
    private int _displayXStop;

    /// <summary>Bus peek for screen/color/chargen (board provides).</summary>
    public Func<ushort, byte>? MemoryPeek { get; set; }

    /// <summary>
    /// V-bus activity hook (VIC-20). Mirrors VICE <c>vic_cycle_do_fetch</c>
    /// updating <c>vic20_v_bus_last_data</c>.
    /// </summary>
    public Action<byte, byte>? VBusFetch { get; set; }

    /// <summary>Optional base of character ROM (default $8000).</summary>
    public ushort CharacterRomBase { get; set; } = 0x8000;

    public Mos6561(IInterruptLine? irqLine = null)
    {
        _irqLine = irqLine;
        Id = new DeviceId(0x0003);
        SourceId = Id;
        Name = "MOS 6561 VIC-I";
        BaseAddress = 0x9000;
        Size = 0x0010;
        _frameBuffer = Array.Empty<byte>();
        EnsureFrameBufferSize();
        ResetRegisters();
    }

    public bool IsNtsc => _cyclesPerLine <= 65;

    /// <summary>
    /// VICE <c>vic_get_pixel_aspect</c>: PAL 1.66574035/2, NTSC 1.50411479/2.
    /// </summary>
    public static float GetPixelAspectRatio(bool ntsc)
        => ntsc ? 1.50411479f / 2.0f : 1.66574035f / 2.0f;

    public float PixelAspectRatio => GetPixelAspectRatio(IsNtsc);

    public DeviceId Id { get; init; }
    public string Name { get; init; }
    public DeviceId SourceId { get; init; }
    public ushort BaseAddress { get; init; }
    public ushort Size { get; init; }
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;
    public IReadOnlyList<IInterruptLine> ConnectedLines =>
        _irqLine is null ? Array.Empty<IInterruptLine>() : new[] { _irqLine };

    public ushort CurrentRasterLine => _rasterLine;
    public int CycleInLine => _cycleInLine;
    public int DebugVBusFetchState => (int)_fetchState;
    public int DebugVBusBufOffset => _bufOffset;
    /// <summary>VICE <c>vic.area</c> as byte (IDLE=0 .. DONE=3).</summary>
    public byte DebugArea => (byte)_area;
    /// <summary>VICE <c>vic.memptr</c>.</summary>
    public int DebugMemptr => _memptr;
    /// <summary>VICE <c>vic.raster.ycounter</c>.</summary>
    public int DebugYCounter => _ycounter;
    /// <summary>VICE <c>vic.row_counter</c>.</summary>
    public int DebugRowCounter => _rowCounter;
    public int CyclesPerLine => _cyclesPerLine;
    public int VisibleLines => _visibleLines;
    public int TotalLines => _totalLines;
    public bool IsVBlank => _rasterLine >= _visibleLines;
    public byte[] FrameBuffer => _frameBuffer;
    /// <summary>
    /// Palette-index framebuffer (one byte per pixel, same geometry as <see cref="FrameBuffer"/>).
    /// Primary Exact compare path vs xvic <c>vice_vic_capture_frame_indices</c> (FR-VIC20-001 / AC-PX-02).
    /// </summary>
    public byte[] IndexFrameBuffer => _indexFrameBuffer;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;
    public int TextColumns => _columns;
    public int TextRows => _rows;
    public event EventHandler? FrameCompleted;

    /// <summary>
    /// Capture VIC-I video pipeline state for every-cycle lockstep against
    /// xvic <c>vice_vic20_get_video_state</c> (same fields, same phase after Tick).
    /// </summary>
    public Vic20VideoLockstepState CaptureVideoLockstepState(uint cycle = 0)
    {
        var regs = new byte[16];
        Array.Copy(_regs, regs, 16);
        var cbuf = new byte[MaxTextCols];
        var gbuf = new byte[MaxTextCols];
        Array.Copy(_cbuf, cbuf, MaxTextCols);
        Array.Copy(_gbuf, gbuf, MaxTextCols);
        return new Vic20VideoLockstepState
        {
            Cycle = cycle,
            RasterLine = _rasterLine,
            RasterCycle = (byte)Math.Clamp(_cycleInLine, 0, 255),
            Area = (byte)_area,
            FetchState = (byte)_fetchState,
            TextCols = (byte)Math.Clamp(_textCols, 0, 255),
            TextLines = (byte)Math.Clamp(_textLines, 0, 255),
            YCounter = (byte)Math.Clamp(_ycounter, 0, 255),
            RowCounter = (byte)Math.Clamp(_rowCounter, 0, 255),
            BlankThisLine = (byte)(_blankThisLine ? 1 : 0),
            LineWasBlank = (byte)(_lineWasBlank ? 1 : 0),
            CharHeight = (byte)_charHeight,
            Memptr = (ushort)Math.Clamp(_memptr, 0, 0xFFFF),
            MemptrInc = (ushort)Math.Clamp(_memptrInc, 0, 0xFFFF),
            Regs = regs,
            Cbuf = cbuf,
            Gbuf = gbuf,
        };
    }

    public void ConfigureTiming(int cyclesPerLine, int totalLines, int visibleLines, int columns = 22, int rows = 23)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cyclesPerLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLines, 1);
        _cyclesPerLine = cyclesPerLine;
        _totalLines = totalLines;
        _visibleLines = Math.Min(visibleLines, totalLines);
        _columns = Math.Clamp(columns, 1, MaxTextCols);
        _rows = Math.Clamp(rows, 1, 32);
        _textCols = _columns;
        _pendingTextCols = _columns;
        _textLines = _rows;
        EnsureFrameBufferSize();
    }

    public bool HandlesAddress(ushort address)
        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);

    public byte Read(ushort address)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        // VICE vic-mem.c: reg 3 bit7 = raster bit0; reg 4 = raster >> 1.
        if (reg == 0x03)
            return (byte)(((_rasterLine & 1) << 7) | (_regs[0x03] & ~0x80));
        if (reg == 0x04)
            return (byte)(_rasterLine >> 1);
        return _regs[reg];
    }

    public void Write(ushort address, byte value)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        _regs[reg] = value;
        // VICE vic-mem.c case 3: character height from bit 0.
        if (reg == 0x03)
            _charHeight = (value & 0x01) != 0 ? 16 : 8;
        // Sound $900A-$900E: register store only. VICE vic20sound.c is Explicit Missing
        // (not Exact). Do not claim audio parity until ported.
    }

    public byte Peek(ushort address) => Read(address);

    /// <summary>
    /// One phi2 of VIC-I. Control flow mirrors VICE <c>vic_cycle</c>
    /// (open_v, cycle++, end_of_line/draw, open_h, memptr, latch, fetch).
    /// Display truth is per-line <see cref="DrawRasterLine"/> (vic-draw), not an EOF invent grid.
    /// </summary>
    public void Tick()
    {
        // VICE: if area IDLE and regs[1] == (raster_line >> 1) -> open_v
        if (_area == VicArea.Idle && (_rasterLine >> 1) == _regs[0x01])
            OpenV();

        _cycleInLine++;

        if (_cycleInLine >= _cyclesPerLine)
            EndOfLine();

        // open_h when display/pending, fetch idle, origin == cycle
        if ((_area is VicArea.Display or VicArea.Pending)
            && _fetchState == FetchState.Idle
            && (_regs[0x00] & 0x7F) == _cycleInLine)
        {
            OpenH();
        }

        // memptr at cycle 0 while DISPLAY
        if (_area == VicArea.Display && _cycleInLine == 0)
            HandleMemptr();

        // Latch rows at raster 0 cycle 2; columns every line cycle 1
        if (_rasterLine == 0 && _cycleInLine == 2)
            LatchRows();
        if (_cycleInLine == 1)
            LatchColumns();

        PerformFetch();
    }

    /// <summary>VICE <c>vic_cycle_open_v</c>.</summary>
    private void OpenV()
    {
        _area = VicArea.Pending;
        if (_textLines == 0)
        {
            _area = VicArea.Done;
            return;
        }
    }

    /// <summary>VICE <c>vic_cycle_open_h</c>.</summary>
    private void OpenH()
    {
        // xstart = min(cycle*4, screen_width) * VIC_PIXEL_WIDTH (full line coords).
        var screenW = FullScreenWidthUnits;
        var xstart = Math.Min(_cycleInLine * 4, screenW) * PixelWidth;
        _displayXStart = xstart;
        _fetchState = FetchState.Start;
        _bufOffset = 4;
        if (_area == VicArea.Pending)
            _area = VicArea.Display;
        _memptrInc = 0;
        // VICE: text_cols = pending at open_h (before start_fetch may re-assign).
        _textCols = _pendingTextCols;
    }

    /// <summary>VICE <c>vic_cycle_start_fetch</c>.</summary>
    private void StartFetch()
    {
        _textCols = _pendingTextCols;
        // xstop = display_xstart/pw + text_cols*8, then * pw (vic-cycle.c).
        var xstopUnits = _displayXStart / PixelWidth + _textCols * 8;
        var screenW = FullScreenWidthUnits;
        if (xstopUnits >= screenW)
            xstopUnits = screenW - 1;
        _displayXStop = xstopUnits * PixelWidth;

        if (_textCols > 0)
        {
            _blankThisLine = false;
            _fetchState = FetchState.Matrix;
            _bufOffset = 0;
        }
        else
        {
            _fetchState = FetchState.Done;
        }
    }

    /// <summary>VICE <c>VIC_PAL_SCREEN_WIDTH</c> / <c>VIC_NTSC_SCREEN_WIDTH</c>.</summary>
    private int FullScreenWidthUnits => IsNtsc ? 260 : 284;

    /// <summary>VICE <c>vic_cycle_end_of_line</c> + <c>vic_raster_draw_handler</c> draw path.</summary>
    private void EndOfLine()
    {
        _lineWasBlank = _blankThisLine;
        // Draw the completed line into the normal-border framebuffer (vic-draw).
        DrawRasterLine(_rasterLine);
        _cycleInLine = 0;
        if (_area == VicArea.Display)
            _ycounter++;
        _fetchState = FetchState.Idle;
        _blankThisLine = true;
        _rasterLine++;
        if (_rasterLine >= _totalLines)
            EndOfFrame();
    }

    /// <summary>VICE <c>vic_cycle_end_of_frame</c>.</summary>
    private void EndOfFrame()
    {
        if (_area != VicArea.Done)
            _area = VicArea.Done;
        _rowCounter = 0;
        _rasterLine = 0;
        _area = VicArea.Idle;
        _ycounter = 0;
        _memptr = 0;
        _memptrInc = 0;
        _lineWasBlank = true;
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>VICE <c>vic_cycle_handle_memptr</c>.</summary>
    private void HandleMemptr()
    {
        var rowIncrease = _charHeight;
        if (_ycounter == rowIncrease || _ycounter == 2 * rowIncrease)
        {
            _ycounter = 0;
            _memptrInc = _lineWasBlank ? 0 : _textCols;
            _rowCounter++;
            if (_rowCounter == _textLines)
                _area = VicArea.Done;
        }

        _memptr += _memptrInc;
        _memptrInc = 0;
    }

    /// <summary>VICE <c>vic_cycle_latch_columns</c> — only pending; text_cols on open_h.</summary>
    private void LatchColumns()
    {
        var maxCols = IsNtsc ? 31 : 32;
        // VICE: MIN(regs[2] & 0x7f, max_text_cols) — no eager live text_cols.
        _pendingTextCols = Math.Min(_regs[0x02] & 0x7F, maxCols);
        if (_pendingTextCols > 0)
            _columns = _pendingTextCols;
    }

    /// <summary>VICE <c>vic_cycle_latch_rows</c>.</summary>
    private void LatchRows()
    {
        var lines = (_regs[0x03] & 0x7E) >> 1;
        _textLines = lines;
        if (lines > 0)
            _rows = lines;
    }

    /// <summary>VICE <c>vic_cycle_fix_addr</c>.</summary>
    private static int FixAddr(int addr)
    {
        var msb = (~((addr & 0x2000) << 2)) & 0x8000;
        return (addr & 0x1FFF) | msb;
    }

    /// <summary>VICE <c>vic_cycle_fetch</c> + store into cbuf/gbuf.</summary>
    private void PerformFetch()
    {
        switch (_fetchState)
        {
            case FetchState.Idle:
            case FetchState.Done:
            default:
                return;

            case FetchState.Start:
                _bufOffset--;
                if (_bufOffset == 0)
                    StartFetch();
                return;

            case FetchState.Matrix:
            {
                if (_bufOffset < 0 || _bufOffset >= MaxTextCols)
                    return;
                var reg2 = _regs[0x02];
                var reg5 = _regs[0x05];
                var matrixBase = ((reg5 & 0xF0) << 6) | ((reg2 & 0x80) << 2);
                var rawAddr = matrixBase + _memptr + _bufOffset;
                var screenAddr = (ushort)FixAddr(rawAddr);
                var colorAddr = (ushort)(ResolveColorBase() + ((_memptr + _bufOffset) & 0x03FF));
                var peek = MemoryPeek ?? (_ => 0x00);
                _vbuf = peek(screenAddr);
                var colorNibble = (byte)(peek(colorAddr) & 0x0F);
                _cbuf[_bufOffset] = colorNibble;
                VBusFetch?.Invoke(_vbuf, colorNibble);
                _fetchState = FetchState.Chargen;
                return;
            }

            case FetchState.Chargen:
            {
                if (_bufOffset < 0 || _bufOffset >= MaxTextCols)
                    return;
                var reg5 = _regs[0x05];
                var peek = MemoryPeek ?? (_ => 0x00);
                // VICE: ((regs[5]&0xf)<<10) + (vbuf * char_height + (ycounter & mask))
                var yMask = (_charHeight >> 1) | 7;
                var chargenRaw = ((reg5 & 0x0F) << 10)
                    + (_vbuf * _charHeight + (_ycounter & yMask));
                var chargenAddr = (ushort)FixAddr(chargenRaw);
                if ((reg5 & 0x0F) == 0)
                    chargenAddr = (ushort)(CharacterRomBase + (_vbuf * _charHeight) + (_ycounter & yMask));
                var colorAddr = (ushort)(ResolveColorBase() + ((_memptr + _bufOffset) & 0x03FF));
                var colorNibble = (byte)(peek(colorAddr) & 0x0F);
                var glyph = peek(chargenAddr);
                _gbuf[_bufOffset] = glyph;
                VBusFetch?.Invoke(glyph, colorNibble);
                if (_ycounter == _charHeight - 1)
                    _memptrInc = _bufOffset + 1;
                _bufOffset++;
                if (_bufOffset >= _textCols)
                    _fetchState = FetchState.Done;
                else
                    _fetchState = FetchState.Matrix;
                return;
            }
        }
    }

    /// <summary>
    /// Paint one raster line: VICE full-line buffer (screen_width * PIXEL_WIDTH),
    /// then crop into the normal-border <see cref="_frameBuffer"/> (display_width * PIXEL_WIDTH).
    /// Draw rules: <c>vic-draw.c</c> drawing_table (std/MC, reverse gate).
    /// </summary>
    private void DrawRasterLine(int rasterLine)
    {
        EnsureFrameBufferSize();
        EnsureLineBufferSize();

        var regF = _regs[0x0F];
        var regE = _regs[0x0E];
        var bgIdx = (byte)BackgroundColorIndex(regF);
        var borderIdx = (byte)BorderColorIndex(regF);
        var auxIdx = (byte)((regE >> 4) & 0x0F);
        var bg = Palette[bgIdx];
        var border = Palette[borderIdx];
        var reverseMode = InvertScreenMode(regF);
        var aux = Palette[auxIdx];
        var line = _lineBuffer;
        var indexLine = _indexLineBuffer;
        var lineW = _lineBufferWidth;

        // VICE: whole physical line is border/idle, then draw_line paints open region
        // (vic-draw.c), then raster_line_draw_borders blanks outside [xstart,xstop].
        FillRow(line, 0, lineW, border);
        FillIndexRow(indexLine, 0, lineW, borderIdx);

        // EndOfLine assigned _lineWasBlank = blank_this_line for THIS completed line.
        // Non-blank: open horizontal flipflop fetched cbuf/gbuf (vic-draw draw_line).
        // No invent paper strip: VICE paints all 8 bit slots (transparent=0), so space
        // cells become background via slot 0 (drawing_table / PUT_PIXEL).
        if (!_lineWasBlank)
        {
            var cols = Math.Clamp(_textCols, 0, MaxTextCols);
            if (cols > 0)
            {
                var originX = _displayXStart;
                // mc_border color is border (c[1] in vic-draw.c) for std/MC.
                var mcBorder = border;
                var mcBorderIdx = borderIdx;

                for (var col = 0; col < cols; col++)
                {
                    var b = _cbuf[col];
                    var d = _gbuf[col];
                    // VICE: dr = (reverse & !(b & 8)) ? ~d : d
                    var reverse = reverseMode && (b & 0x08) == 0;
                    var dr = reverse ? (byte)~d : d;
                    var c0 = bg;
                    var c1 = mcBorder;
                    var c2Idx = (byte)(b & 0x07);
                    var c2 = Palette[c2Idx];
                    var c3 = aux;
                    var multi = (b & 0x08) != 0;
                    var cellX = originX + col * 8 * PixelWidth;

                    for (var pos = 0; pos < 8; pos++)
                    {
                        int slot;
                        if (multi)
                            slot = (dr >> (6 - (pos & ~1))) & 0x3;
                        else
                            slot = ((dr >> (7 - pos)) & 0x1) * 2;

                        var color = slot switch
                        {
                            0 => c0,
                            1 => c1,
                            2 => c2,
                            _ => c3,
                        };
                        var idx = slot switch
                        {
                            0 => bgIdx,
                            1 => mcBorderIdx,
                            2 => c2Idx,
                            _ => auxIdx,
                        };
                        var xPix = pos * PixelWidth;
                        for (var sx = 0; sx < PixelWidth; sx++)
                        {
                            var px = cellX + xPix + sx;
                            if ((uint)px < (uint)lineW)
                            {
                                WritePixel(line, px * 4, color);
                                indexLine[px] = idx;
                            }
                        }
                    }
                }

                // VICE raster_line_draw_borders: blank left of display_xstart and
                // right of display_xstop on the full line (closed borders).
                if (_displayXStart > 0)
                {
                    FillRow(line, 0, Math.Min(_displayXStart, lineW), border);
                    FillIndexRow(indexLine, 0, Math.Min(_displayXStart, lineW), borderIdx);
                }
                if (_displayXStop > 0 && _displayXStop < lineW)
                {
                    var rightStart = _displayXStop;
                    // display_xstop is exclusive-ish end of gfx in VICE (blank from xstop to end).
                    FillRow(line, rightStart * 4, lineW - rightStart, border);
                    FillIndexRow(indexLine, rightStart, lineW - rightStart, borderIdx);
                }
            }
        }

        // Crop full line into normal-border output (vic_set_geometry display_width).
        // Horizontal start is VICE viewport first_x (video_viewport_resize + video_canvas_refresh),
        // not full-line x=0 (that invent wiped the right border on READY).
        var first = IsNtsc ? NtscNormalFirstDisplayedLine : PalNormalFirstDisplayedLine;
        var last = IsNtsc ? NtscNormalLastDisplayedLine : PalNormalLastDisplayedLine;
        if (rasterLine < first || rasterLine > last)
            return;

        var fbY = rasterLine - first;
        if ((uint)fbY >= (uint)_frameHeight)
            return;

        var gfxX = ResolveGfxXForViewport();
        var gfxW = ResolveGfxWForViewport();
        var firstX = ComputeViewportFirstX(gfxX, gfxW, _frameWidth, lineW, gfxAreaMoves: true);
        var copyW = Math.Min(_frameWidth, Math.Max(0, lineW - firstX));
        if (copyW <= 0)
            return;
        var src = firstX * 4;
        var dst = fbY * _frameWidth * 4;
        Buffer.BlockCopy(line, src, _frameBuffer, dst, copyW * 4);
        Buffer.BlockCopy(indexLine, firstX, _indexFrameBuffer, fbY * _frameWidth, copyW);
        // If the slice is short of canvas width, pad with border (should not happen for READY).
        if (copyW < _frameWidth)
        {
            FillRow(_frameBuffer, dst + copyW * 4, _frameWidth - copyW, border);
            FillIndexRow(_indexFrameBuffer, fbY * _frameWidth + copyW, _frameWidth - copyW, borderIdx);
        }
    }

    /// <summary>
    /// Full-line gfx X for viewport: live <c>display_xstart</c> after open_h, else
    /// <c>$9000</c> origin (VICE geometry default before first open).
    /// </summary>
    private int ResolveGfxXForViewport()
    {
        if (_displayXStart > 0)
            return _displayXStart;
        var originCycle = _regs[0] & 0x7F;
        var screenW = FullScreenWidthUnits;
        return Math.Min(originCycle * 4, screenW) * PixelWidth;
    }

    /// <summary>
    /// Full-line gfx width for viewport: live text cols, else pending / $9002.
    /// </summary>
    private int ResolveGfxWForViewport()
    {
        var cols = _textCols;
        if (cols <= 0)
            cols = _pendingTextCols;
        if (cols <= 0)
            cols = Math.Min(_regs[0x02] & 0x7F, MaxTextCols);
        if (cols <= 0)
            cols = 22;
        return cols * 8 * PixelWidth;
    }

    public void Reset()
    {
        ResetRegisters();
        // VICE vic_reset: raster_cycle = 6.
        _cycleInLine = 6;
        _rasterLine = 0;
        _fetchState = FetchState.Idle;
        _bufOffset = 0;
        _area = VicArea.Idle;
        _memptr = 0;
        _memptrInc = 0;
        _ycounter = 0;
        _rowCounter = 0;
        // VICE vic_init: pending_text_cols=22, text_lines=23; text_cols assigned on open_h.
        _textLines = 23;
        _textCols = 0;
        _pendingTextCols = 22;
        _charHeight = 8;
        // VICE raster_reset: blank_this_line = 0 (raster.c).
        _lineWasBlank = false;
        _blankThisLine = false;
        _displayXStart = 0;
        _displayXStop = 0;
        Array.Clear(_cbuf);
        Array.Clear(_gbuf);
        Array.Clear(_frameBuffer);
        Array.Clear(_indexFrameBuffer);
        if (_lineBuffer.Length > 0)
            Array.Clear(_lineBuffer);
        if (_indexLineBuffer.Length > 0)
            Array.Clear(_indexLineBuffer);
    }

    /// <summary>
    /// Drive one full raster through <see cref="Tick"/> so the cycle/draw path
    /// is the display truth (tests and hosts that do not wait for live frames).
    /// Program $9000/$9001 (origin) and geometry regs before calling when you
    /// need a specific text placement (KERNAL PAL uses $9000=12, $9001=38).
    /// </summary>
    public void RenderNow()
    {
        EnsureFrameBufferSize();
        EnsureLineBufferSize();
        // Re-arm frame state without clearing programmed registers.
        _cycleInLine = 6;
        _rasterLine = 0;
        _fetchState = FetchState.Idle;
        _bufOffset = 0;
        _area = VicArea.Idle;
        _memptr = 0;
        _memptrInc = 0;
        _ycounter = 0;
        _rowCounter = 0;
        // Match VICE raster_reset blank defaults (not "assume blank").
        _lineWasBlank = false;
        _blankThisLine = false;
        _displayXStart = 0;
        _displayXStop = 0;
        _pendingTextCols = Math.Min(_regs[0x02] & 0x7F, IsNtsc ? 31 : 32);
        if (_pendingTextCols == 0)
            _pendingTextCols = 22; // unprogrammed: VICE vic_init default
        var rows = (_regs[0x03] & 0x7E) >> 1;
        _textLines = rows > 0 ? rows : 23;
        _textCols = 0;
        _charHeight = (_regs[0x03] & 0x01) != 0 ? 16 : 8;
        Array.Clear(_cbuf);
        Array.Clear(_gbuf);
        FillSolid(_frameBuffer, Palette[BorderColorIndex(_regs[0x0F])]);

        var done = false;
        EventHandler handler = (_, _) => done = true;
        FrameCompleted += handler;
        try
        {
            var guard = _totalLines * _cyclesPerLine + 16;
            for (var i = 0; i < guard && !done; i++)
                Tick();
        }
        finally
        {
            FrameCompleted -= handler;
        }
    }

    public static int BorderColorIndex(byte reg900F) => reg900F & 0x07;
    public static int BackgroundColorIndex(byte reg900F) => (reg900F >> 4) & 0x0F;

    /// <summary>
    /// VICE <c>vic-mem.c</c> $900F reverse: <c>new_reverse = (value &amp; 0x8) ? 0 : 1</c>.
    /// Bit 3 SET means reverse OFF; bit 3 CLEAR means reverse ON.
    /// (Opposite of the naive "bit set = invert" reading of the data sheet.)
    /// </summary>
    public static bool InvertScreenMode(byte reg900F) => (reg900F & 0x08) == 0;

    private void ResetRegisters() => Array.Clear(_regs);

    private void EnsureFrameBufferSize()
    {
        var displayWidth = (IsNtsc ? NtscNormalDisplayWidth : PalNormalDisplayWidth) * PixelWidth;
        var first = IsNtsc ? NtscNormalFirstDisplayedLine : PalNormalFirstDisplayedLine;
        var last = IsNtsc ? NtscNormalLastDisplayedLine : PalNormalLastDisplayedLine;
        var h = last - first + 1;
        if (displayWidth == _frameWidth && h == _frameHeight
            && _frameBuffer is not null && _frameBuffer.Length == displayWidth * h * 4
            && _indexFrameBuffer.Length == displayWidth * h)
        {
            return;
        }

        _frameWidth = displayWidth;
        _frameHeight = h;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
        _indexFrameBuffer = new byte[_frameWidth * _frameHeight];
    }

    private void EnsureLineBufferSize()
    {
        var w = FullScreenWidthUnits * PixelWidth;
        if (w == _lineBufferWidth && _lineBuffer.Length == w * 4
            && _indexLineBuffer.Length == w)
            return;
        _lineBufferWidth = w;
        _lineBuffer = new byte[w * 4];
        _indexLineBuffer = new byte[w];
    }

    /// <summary>
    /// Color nybble base: VICE <c>vic.c</c>
    /// <c>(regs[0x02] & 0x80) ? 0x9600 : 0x9400</c>.
    /// </summary>
    public ushort ResolveColorBase()
        => (ushort)((_regs[0x02] & 0x80) != 0 ? 0x9600 : 0x9400);

    private static void WritePixel(byte[] fb, int i, uint color)
    {
        if ((uint)i + 3u >= (uint)fb.Length)
            return;
        fb[i] = (byte)(color & 0xFF);
        fb[i + 1] = (byte)((color >> 8) & 0xFF);
        fb[i + 2] = (byte)((color >> 16) & 0xFF);
        fb[i + 3] = 0xFF;
    }

    private static void FillRow(byte[] fb, int rowBase, int width, uint color)
    {
        var b = (byte)(color & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var r = (byte)((color >> 16) & 0xFF);
        var end = Math.Min(rowBase + width * 4, fb.Length);
        for (var i = rowBase; i < end; i += 4)
        {
            fb[i] = b;
            fb[i + 1] = g;
            fb[i + 2] = r;
            fb[i + 3] = 0xFF;
        }
    }

    private static void FillIndexRow(byte[] indices, int start, int width, byte colorIndex)
    {
        var end = Math.Min(start + width, indices.Length);
        for (var i = start; i < end; i++)
            indices[i] = colorIndex;
    }

    private static void FillSolid(byte[] fb, uint color)
    {
        var b = (byte)(color & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var r = (byte)((color >> 16) & 0xFF);
        for (var i = 0; i < fb.Length; i += 4)
        {
            fb[i] = b;
            fb[i + 1] = g;
            fb[i + 2] = r;
            fb[i + 3] = 0xFF;
        }
    }

    /// <summary>
    /// Decode video matrix base from $9005/$9002 (VICE matrix address formula).
    /// </summary>
    public ushort ResolveScreenBase()
    {
        var cr = _regs[0x02];
        var vm = _regs[0x05];
        var bank = (vm & 0x80) != 0 ? 0 : 0x8000;
        return (ushort)(bank + ((vm & 0x70) << 6) + ((cr & 0x80) << 2));
    }

    /// <summary>Character pointer base from $9005 bits 3..0.</summary>
    public ushort ResolveCharBase()
    {
        var vm = _regs[0x05];
        var addr = (ushort)((vm & 0x0F) << 10);
        if ((vm & 0x08) == 0)
            addr = (ushort)(addr + 0x8000);
        return addr == 0 ? CharacterRomBase : addr;
    }

    public ushort CurrentScreenBase => ResolveScreenBase();

    public const ushort Vic20DefaultScreenBase = 0x1E00;
}
