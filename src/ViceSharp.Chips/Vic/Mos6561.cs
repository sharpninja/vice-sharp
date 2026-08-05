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

    /// <summary>Bus peek for screen/color/chargen data (board provides).</summary>
    public Func<ushort, byte>? MemoryPeek { get; set; }

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
        if (reg == 0x04)
            return (byte)(_rasterLine & 0xFF);
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
        _cycleInLine++;
        if (_cycleInLine < _cyclesPerLine)
            return;

        _cycleInLine = 0;
        _rasterLine++;
        if (_rasterLine < _totalLines)
            return;

        _rasterLine = 0;
        RenderCharacterFrame();
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        ResetRegisters();
        _cycleInLine = 0;
        _rasterLine = 0;
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
        Array.Clear(_regs);
        // Unexpanded VIC-20 KERNAL-like defaults so ResolveScreenBase => $1E00.
        _regs[0x00] = 5;
        _regs[0x01] = 25;
        _regs[0x02] = unchecked((byte)(22 | 0x80));
        _regs[0x03] = (23 << 1);
        _regs[0x05] = 0xF0;
        _regs[0x0E] = 0;
        // Typical READY look: cyan-ish border (3) + white bg (1) = $1B often after init;
        // power-on default before KERNAL is model-dependent; start with cyan border / light bg.
        _regs[0x0F] = 0x1B;
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
