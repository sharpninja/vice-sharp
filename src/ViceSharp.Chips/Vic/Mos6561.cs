using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Vic;

/// <summary>
/// MOS 6560/6561 VIC-I video chip: character-mode framebuffer with border
/// strips and PAL/NTSC timing. Machine-agnostic; the board supplies bus peeks
/// for screen, color, and character data via callbacks.
/// </summary>
/// <remarks>FR-VIC20-001. $900F layout: bits 0-2 border, bit 3 invert screen, bits 4-7 background.</remarks>
public sealed class Mos6561 : IVideoChip, IAddressSpace, IInterruptSource
{
    // Approximate Pepto-style VIC-20 RGB as BGRA (matches common READY cyan/white look).
    private static readonly uint[] Palette =
    [
        0xFF000000, // 0 black
        0xFFFFFFFF, // 1 white
        0xFF2B3768, // 2 red (BGRA)
        0xFFB2A470, // 3 cyan
        0xFF863D6F, // 4 purple
        0xFF438D58, // 5 green
        0xFF9A4631, // 6 blue
        0xFF71C3C3, // 7 yellow (light)
        0xFF2B5E86, // 8 orange
        0xFF5988B9, // 9 light orange
        0xFF6B779A, // 10 pink / light red
        0xFFC8C8A0, // 11 light cyan
        0xFFA67BA9, // 12 light purple
        0xFF84D29A, // 13 light green
        0xFFB98D7A, // 14 light blue
        0xFFE0FFFF, // 15 light yellow
    ];

    /// <summary>Left/right border thickness in pixels (MVP fixed strip).</summary>
    public const int BorderX = 16;

    /// <summary>Top/bottom border thickness in pixels (MVP fixed strip).</summary>
    public const int BorderY = 16;

    private readonly IInterruptLine? _irqLine;
    private readonly byte[] _regs = new byte[16];
    private int _cycleInLine;
    private int _cyclesPerLine = 71;
    private int _totalLines = 312;
    private int _visibleLines = 284;
    private int _columns = 22;
    private int _rows = 23;
    private int _frameWidth;
    private int _frameHeight;
    private byte[] _frameBuffer;
    private ushort _rasterLine;
    /// <summary>Screen code latched by the last matrix fetch (for chargen row).</summary>
    private byte _vBusMatrixCode;
    /// <summary>VICE <c>vic.fetch_state</c> simplified for V-bus open-bus.</summary>
    private enum VBusFetchState : byte
    {
        Idle = 0,
        Start = 1,
        Matrix = 2,
        Chargen = 3,
        Done = 4,
    }

    private VBusFetchState _vBusFetchState = VBusFetchState.Idle;
    /// <summary>VICE <c>vic.buf_offset</c>: START delay, then column index.</summary>
    private int _vBusBufOffset;
    /// <summary>True while vertical display flipflop is open (VICE VIC_AREA_DISPLAY).</summary>
    private bool _vBusDisplayArea;
    /// <summary>VICE <c>vic.memptr</c> — video matrix row base (not row*cols).</summary>
    private int _memptr;
    private int _memptrInc;
    private int _ycounter;
    private int _rowCounter;
    private int _textLines = 23;
    private int _textCols = 22;
    private bool _lineWasBlank = true;

    /// <summary>Bus peek for screen/color/chargen data (board provides).</summary>
    public Func<ushort, byte>? MemoryPeek { get; set; }

    /// <summary>
    /// Optional V-bus activity hook (VIC-20). Invoked when VIC would drive
    /// VD0-VD7 / color high during a display fetch. Args: (vBusData, colorHighNibble0_15).
    /// Mirrors VICE <c>vic_cycle_fetch</c> updating <c>vic20_v_bus_last_data</c>.
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
        _frameWidth = BorderX * 2 + _columns * 8;
        _frameHeight = BorderY * 2 + _rows * 8;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
        ResetRegisters();
    }

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
    /// <summary>Phi2 index within the current raster line (0 .. CyclesPerLine-1).</summary>
    public int CycleInLine => _cycleInLine;
    /// <summary>Diagnostic: V-bus fetch FSM state (0=Idle..4=Done).</summary>
    public int DebugVBusFetchState => (int)_vBusFetchState;
    /// <summary>Diagnostic: V-bus column/delay counter.</summary>
    public int DebugVBusBufOffset => _vBusBufOffset;
    public int CyclesPerLine => _cyclesPerLine;
    public int VisibleLines => _visibleLines;
    public int TotalLines => _totalLines;
    public bool IsVBlank => _rasterLine >= _visibleLines;
    public byte[] FrameBuffer => _frameBuffer;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;
    public int TextColumns => _columns;
    public int TextRows => _rows;
    public event EventHandler? FrameCompleted;

    public void ConfigureTiming(int cyclesPerLine, int totalLines, int visibleLines, int columns = 22, int rows = 23)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cyclesPerLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLines, 1);
        _cyclesPerLine = cyclesPerLine;
        _totalLines = totalLines;
        _visibleLines = Math.Min(visibleLines, totalLines);
        _columns = Math.Clamp(columns, 1, 32);
        _rows = Math.Clamp(rows, 1, 32);
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
    }

    public byte Peek(ushort address) => Read(address);

    public void Tick()
    {
        // VICE vic20cpu.c CLK_INC: CPU bus access first, then vic_cycle().
        // SystemClock registers CPU before VIC so this Tick runs after the CPU.
        //
        // VICE vic_cycle order: open_v → raster_cycle++ → end_of_line →
        // open_h / memptr → fetch. Matrix addr = base+memptr+buf_offset.
        // VICE open_v: area IDLE and regs[1]==(raster_line>>1). reg1==0 on
        // line 0 opens immediately (do not require reg1!=0).
        var reg1 = _regs[0x01];
        if (!_vBusDisplayArea && (_rasterLine >> 1) == reg1)
            _vBusDisplayArea = true;

        _cycleInLine++;
        if (_cycleInLine >= _cyclesPerLine)
        {
            // VICE end_of_line: capture blank_this_line into line_was_blank,
            // then default blank_this_line=1 for the next line (after memptr).
            if (_vBusDisplayArea)
                _ycounter++;
            _cycleInLine = 0;
            _vBusFetchState = VBusFetchState.Idle;
            _vBusBufOffset = 0;
            // _lineWasBlank still holds prior line until handle_memptr below.
            _rasterLine++;
            if (_rasterLine >= _totalLines)
            {
                _rasterLine = 0;
                _vBusDisplayArea = false;
                _memptr = 0;
                _memptrInc = 0;
                _ycounter = 0;
                _rowCounter = 0;
                _lineWasBlank = true;
                RenderCharacterFrame();
                FrameCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        // Latch text geometry at VICE raster_cycle==1.
        if (_cycleInLine == 1)
        {
            var cols = _regs[0x02] & 0x7F;
            _textCols = cols > 0 ? cols : _columns;
            var lines = (_regs[0x03] & 0x7E) >> 1;
            if (lines > 0)
                _textLines = lines;
        }

        // VICE open_h when origin==raster_cycle.
        var origin = _regs[0x00] & 0x7F;
        if (_vBusDisplayArea
            && _vBusFetchState is VBusFetchState.Idle or VBusFetchState.Done
            && origin == _cycleInLine
            && _textCols > 0)
        {
            _vBusFetchState = VBusFetchState.Start;
            _vBusBufOffset = 4;
            _memptrInc = 0;
        }

        // VICE: handle_memptr when DISPLAY and raster_cycle==0 (uses prior
        // line's blank flag), then arm blank for the new line.
        if (_vBusDisplayArea && _cycleInLine == 0)
        {
            HandleMemptr();
            _lineWasBlank = true;
        }

        PerformDisplayVBusFetch();
    }

    /// <summary>VICE <c>vic_cycle_handle_memptr</c>.</summary>
    private void HandleMemptr()
    {
        // Character height 8: step row when ycounter hits 8 (or 16 for double).
        const int rowIncreaseLine = 8;
        if (_ycounter == rowIncreaseLine || _ycounter == 2 * rowIncreaseLine)
        {
            _ycounter = 0;
            _memptrInc = _lineWasBlank ? 0 : _textCols;
            _rowCounter++;
            if (_rowCounter >= _textLines)
                _vBusDisplayArea = false;
        }

        _memptr += _memptrInc;
        _memptrInc = 0;
    }

    /// <summary>
    /// VICE <c>vic_cycle_fetch</c>: START is no_fetch (even when entering MATRIX);
    /// matrix addr = base + memptr + buf_offset.
    /// </summary>
    private void PerformDisplayVBusFetch()
    {
        if (VBusFetch is null || MemoryPeek is null)
            return;

        // VICE VIC_FETCH_START: --buf; on zero enter MATRIX; always no_fetch.
        if (_vBusFetchState == VBusFetchState.Start)
        {
            _vBusBufOffset--;
            if (_vBusBufOffset == 0)
            {
                _vBusFetchState = _textCols > 0 ? VBusFetchState.Matrix : VBusFetchState.Done;
                _vBusBufOffset = 0;
            }
            return;
        }

        if (_vBusFetchState is not (VBusFetchState.Matrix or VBusFetchState.Chargen))
            return;

        var reg2 = _regs[0x02];
        var reg5 = _regs[0x05];
        // VICE: (((r5&0xf0)<<6)|((r2&0x80)<<2)) + memptr + buf_offset, then
        // 14→16 bit fix: msb = ~((addr&0x2000)<<2) & 0x8000.
        var matrixBase = ((reg5 & 0xF0) << 6) | ((reg2 & 0x80) << 2);
        var slot = _vBusBufOffset;
        if (slot < 0 || slot >= _textCols)
            return;

        var rawAddr = matrixBase + _memptr + slot;
        var msb = (~((rawAddr & 0x2000) << 2)) & 0x8000;
        var screenAddr = (ushort)((rawAddr & 0x1FFF) | msb);
        var colorAddr = (ushort)(0x9400 + ((_memptr + slot) & 0x03FF));
        var colorNibble = (byte)(MemoryPeek(colorAddr) & 0x0F);

        if (_vBusFetchState == VBusFetchState.Matrix)
        {
            _vBusMatrixCode = MemoryPeek(screenAddr);
            VBusFetch(_vBusMatrixCode, colorNibble);
            _vBusFetchState = VBusFetchState.Chargen;
            // Mark line as having displayed content.
            _lineWasBlank = false;
            return;
        }

        // Chargen: VICE addr from regs[5] low + char*height + ycounter.
        var charHeight = 8;
        var chargenBase = (reg5 & 0x0F) << 10;
        var chargenRaw = chargenBase + (_vBusMatrixCode * charHeight) + (_ycounter & 7);
        var cMsb = (~((chargenRaw & 0x2000) << 2)) & 0x8000;
        var chargenAddr = (ushort)((chargenRaw & 0x1FFF) | cMsb);
        // Unexpanded chargen often at $8000; fall back if base 0.
        if ((reg5 & 0x0F) == 0)
            chargenAddr = (ushort)(CharacterRomBase + (_vBusMatrixCode * 8) + (_ycounter & 7));
        var chargenByte = MemoryPeek(chargenAddr);
        VBusFetch(chargenByte, colorNibble);
        _vBusBufOffset++;
        if (_vBusBufOffset >= _textCols)
            _vBusFetchState = VBusFetchState.Done;
        else
            _vBusFetchState = VBusFetchState.Matrix;
    }

    public void Reset()
    {
        ResetRegisters();
        // VICE vic_reset: raster_cycle = 6 (maincpu_clk = 6). Start 0 left a
        // permanent +6 nCyc lead so native was FETCH_DONE while we still
        // chargen'd (c=3533371 nA=$91 mA=$01, nCyc=61 mCyc=55).
        _cycleInLine = 6;
        _rasterLine = 0;
        _vBusMatrixCode = 0;
        _vBusFetchState = VBusFetchState.Idle;
        _vBusBufOffset = 0;
        _vBusDisplayArea = false;
        _memptr = 0;
        _memptrInc = 0;
        _ycounter = 0;
        _rowCounter = 0;
        _textLines = 23;
        _textCols = 22;
        _lineWasBlank = true;
        Array.Clear(_frameBuffer);
    }

    /// <summary>
    /// Force an immediate character-mode render (tests / hosts that pull frames
    /// without waiting for a full raster).
    /// </summary>
    public void RenderNow() => RenderCharacterFrame();

    /// <summary>Decode $900F border color index (bits 0-2).</summary>
    public static int BorderColorIndex(byte reg900F) => reg900F & 0x07;

    /// <summary>Decode $900F background color index (bits 4-7).</summary>
    public static int BackgroundColorIndex(byte reg900F) => (reg900F >> 4) & 0x0F;

    /// <summary>True when $900F bit 3 requests inverted (reverse) screen mode.</summary>
    public static bool InvertScreenMode(byte reg900F) => (reg900F & 0x08) != 0;

    private void ResetRegisters()
    {
        // Power-on zeros match VICE vic_reset / unprogrammed chip state. KERNAL
        // programs geometry ($9000-$9005) and colors ($900F) during boot; lockstep
        // against xvic requires the same starting point (not pre-seeded READY defaults).
        Array.Clear(_regs);
    }

    private void EnsureFrameBufferSize()
    {
        var w = BorderX * 2 + _columns * 8;
        var h = BorderY * 2 + _rows * 8;
        if (w == _frameWidth && h == _frameHeight && _frameBuffer.Length == w * h * 4)
            return;
        _frameWidth = w;
        _frameHeight = h;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
    }

    private void RenderCharacterFrame()
    {
        _columns = Math.Clamp(_regs[0x02] & 0x7F, 1, 32);
        _rows = Math.Clamp((_regs[0x03] >> 1) & 0x3F, 1, 32);
        EnsureFrameBufferSize();

        var screenBase = ResolveScreenBase();
        var charBase = ResolveCharBase();
        var regF = _regs[0x0F];
        var bg = Palette[BackgroundColorIndex(regF)];
        var border = Palette[BorderColorIndex(regF)];
        var invertScreen = InvertScreenMode(regF);

        var peek = MemoryPeek ?? (_ => 0x00);
        var fb = _frameBuffer;
        var stride = _frameWidth * 4;

        // Fill entire frame with border color, then paint character area over the center.
        FillSolid(fb, border);

        var originX = BorderX;
        var originY = BorderY;

        for (var row = 0; row < _rows; row++)
        {
            for (var col = 0; col < _columns; col++)
            {
                var offset = row * _columns + col;
                var screenCode = peek((ushort)(screenBase + offset));
                var colorNibble = (byte)(peek((ushort)(0x9400 + (offset & 0x03FF))) & 0x0F);
                var fg = Palette[colorNibble & 0x0F];
                var reverse = ((screenCode & 0x80) != 0) ^ invertScreen;
                var charIndex = screenCode & 0x7F;
                var glyphAddr = (ushort)(charBase + charIndex * 8);

                for (var py = 0; py < 8; py++)
                {
                    var bits = peek((ushort)(glyphAddr + py));
                    if (reverse)
                        bits = (byte)~bits;
                    var y = originY + row * 8 + py;
                    if ((uint)y >= (uint)_frameHeight)
                        break;
                    var rowBase = y * stride + (originX + col * 8) * 4;
                    for (var px = 0; px < 8; px++)
                    {
                        var on = (bits & (0x80 >> px)) != 0;
                        var color = on ? fg : bg;
                        var i = rowBase + px * 4;
                        fb[i] = (byte)(color & 0xFF);
                        fb[i + 1] = (byte)((color >> 8) & 0xFF);
                        fb[i + 2] = (byte)((color >> 16) & 0xFF);
                        fb[i + 3] = 0xFF;
                    }
                }
            }
        }
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
    /// Decode video matrix base from $9005/$9002 (VICE <c>mem_get_screen_parameter</c>).
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
