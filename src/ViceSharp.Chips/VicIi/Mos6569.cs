using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// MOS 6569 VIC-II Video Interface Controller implementation.
/// </summary>
public partial class Mos6569 : IVideoChip, IAddressSpace, IInterruptSource, ICpuCycleStealer
{
    public virtual DeviceId Id => new DeviceId(0x0003);
    public DeviceId SourceId => Id;
    public virtual string Name => "MOS 6569 VIC-II";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi1;

    public ushort BaseAddress { get; init; } = 0xD000;
    public ushort Size => 64;
    public bool IsReadOnly => false;

    // VIC-II registers
    private readonly byte[] _registers = new byte[64];
    private const byte InterruptSourceMask = 0x0F;
    private ushort _rasterIrqLine;
    private bool _rasterIrqCompareArmed;

    public ushort CurrentRasterLine { get; private set; }
    
    private readonly VideoRenderer _renderer;
    
    /// <inheritdoc />
    public byte[] FrameBuffer => _renderer.FrameBuffer;
    
    /// <inheritdoc />
    public int FrameWidth => VideoRenderer.ScreenWidth;
    
    /// <inheritdoc />
    public int FrameHeight => VideoRenderer.ScreenHeight;
    
    /// <inheritdoc />
    public event EventHandler? FrameCompleted
    {
        add => _renderer.FrameCompleted += value;
        remove => _renderer.FrameCompleted -= value;
    }
    
    // VICE-style: PAL timing (6567/6569)
    public const int PalCyclesPerLine = 63;
    public const int PalVisibleLines = 312;
    public const int PalTotalLines = 312;
    public const int NtscCyclesPerLine = 65;
    public const int NtscOldCyclesPerLine = 64;
    public const int NtscVisibleLines = 262;
    public const int NtscTotalLines = 263;
    public const int NtscOldTotalLines = 262;
    public const byte ResetRasterCycle = 6;
    private const ushort FirstDmaLine = 0x30;
    private const ushort LastDmaLine = 0xF7;

    private int _cyclesPerLine = PalCyclesPerLine;
    private int _visibleLines = PalVisibleLines;
    private int _totalLines = PalTotalLines;
    private double _frameRate = 50.0;
    private bool _allowBadLines;
    private byte _refreshCounter;
    private readonly byte[] _videoBuffer = new byte[40];
    private ushort _videoCounter;
    private byte _rowCounter;
    private int _videoMatrixLineIndex;
    private bool _idleState;

    // BACKFILL-VIDEO-001: Track which raster line last contributed to the
    // per-frame bad-line count so each bad line is counted exactly once even
    // if IsBadLine is sampled multiple times during the line.
    private int _lastBadLineCounted = -1;
    private int _badLineCountThisFrame;

    // BACKFILL-VIDEO-001 / FR-VIC-007: VICE-style border flip-flops.
    // _verticalBorderActive mirrors vborder, _verticalBorderNextActive mirrors
    // set_vborder, and _mainBorderActive mirrors the horizontal main-border
    // state that is cleared at the left display check and set at the right
    // display check. Per-line snapshots let whole-line renderers consume the
    // cycle-driven vertical border state without changing static render-only
    // fallback behavior for lines that were never ticked.
    private readonly bool[] _verticalBorderActiveByRasterLine = new bool[512];
    private readonly bool[] _verticalBorderLineCaptured = new bool[512];
    private readonly bool[] _rightBorderOpenByRasterLine = new bool[512];
    private readonly bool[] _rightBorderLineCaptured = new bool[512];
    private bool _verticalBorderActive = true;
    private bool _verticalBorderNextActive = true;
    private bool _mainBorderActive = true;

    // BACKFILL-VIDEO-001: Sprite DMA cycle stealing accounting.
    // Each enabled sprite that intersects the current raster line steals
    // two CPU cycles for s-data fetches. _spriteDmaCyclesThisFrame
    // accumulates across the frame; _lastSpriteDmaLineCounted ensures
    // each line is counted at most once.
    private int _spriteDmaCyclesThisFrame;
    private int _lastSpriteDmaLineCounted = -1;
    private byte _spriteDmaActiveMask;
    private readonly ushort[] _spriteDmaStartLines = new ushort[8];
    private readonly byte[] _spriteDmaHeights = new byte[8];

    // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: Light pen latch state.
    // On a high-to-low transition of the LP pin the VIC-II latches
    // RasterX >> 1 into $D013 and the low 8 bits of the current raster
    // line into $D014, sets $D019 bit 3 (LP IRQ latch), and asserts the
    // IRQ output if $D01A bit 3 is enabled. The latch fires at most once
    // per frame; the "already triggered this frame" flag clears at the
    // frame boundary (raster wrap to line 0).
    private byte _lightPenLatchedX;
    private byte _lightPenLatchedY;
    private bool _lightPenTriggeredThisFrame;

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001:
    /// count of bad lines that have fired
    /// during the current frame. Reset to zero when the raster wraps back
    /// to line 0. Increments at most once per raster line, on the first
    /// tick at which IsBadLine evaluates true for that line.
    /// </summary>
    public int BadLineCountThisFrame => _badLineCountThisFrame;

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 /
    /// TEST-VIC-001: cumulative count of CPU cycles
    /// stolen by sprite DMA in the current frame. Each enabled sprite
    /// intersecting a raster line steals two cycles for s-data fetches.
    /// Reset when the raster wraps to line 0. Composes additively with
    /// BadLineCountThisFrame (bad-line cycle theft is a separate
    /// counter).
    /// </summary>
    public int SpriteDmaCyclesThisFrame => _spriteDmaCyclesThisFrame;
    
    /// <summary>
    /// TV system type for VIC-II timing
    /// </summary>
    public enum TvSystem { PAL, NTSC, PALN, SECAM }
    
    /// <summary>
    /// Current TV system
    /// </summary>
    public virtual TvSystem System { get; protected set; } = TvSystem.PAL;
    
    /// <summary>
    /// Is this PAL machine (6569) vs NTSC (6567)
    /// </summary>
    public bool IsPal => System == TvSystem.PAL;
    
    public bool IsVBlank => CurrentRasterLine >= VisibleLines;
    
    /// <summary>
    /// VICE-style: Check if current raster line is a badline.
    /// Badlines occur on raster lines $30-$F7 when DEN is set and the raster low bits match YSCROLL.
    /// </summary>
    public bool IsBadLine => _allowBadLines && IsBadLineRaster(CurrentRasterLine);
    
    /// <summary>
    /// Check if display is enabled (DEN bit in register $11)
    /// </summary>
    public bool IsDisplayEnabled => (_registers[0x11] & 0x10) != 0;
    
    /// <summary>
    /// Check if current position is in vertical blank area
    /// </summary>
    public bool IsVerticalBlankArea => CurrentRasterLine < 51 || CurrentRasterLine >= 251;
    
    /// <summary>
    /// VICE-style: DMA stealing state
    /// On badlines, VIC-II steals 40-63 cycles during display window for character data fetch
    /// </summary>
    public bool IsDmaStealing => IsBadLine && RasterX >= 14 && RasterX < 54;

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001:
    /// CPU cycle is stolen when either the bad-line c-access window is
    /// active (RasterX 12..54 on a bad line) or the active VIC-II model's
    /// sprite BA/DMA mask requests the bus for an enabled sprite.
    /// </summary>
    public bool IsCpuCycleStolen =>
        (IsBadLine && RasterX >= 12 && RasterX < 55)
        || IsInSpriteDmaStallWindow(leadingEdgeOffset: 0);

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001:
    /// Mandatory cycle steal mirrors IsCpuCycleStolen but lags by one
    /// cycle, matching the existing bad-line semantics.
    /// </summary>
    public bool IsCpuCycleStealMandatory =>
        (IsBadLine && RasterX >= 13 && RasterX < 56)
        || IsInSpriteDmaStallWindow(leadingEdgeOffset: 1);
    
    /// <summary>
    /// Check if VIC-II is currently accessing video matrix (cycle 14-54 of badline)
    /// </summary>
    public bool IsVideoMatrixAccess => IsBadLine && RasterX >= 14 && RasterX < 54;
    
    /// <summary>
    /// Check if VIC-II is currently accessing character generator (cycle 54-64 of badline)
    /// </summary>
    public bool IsCharacterAccess => IsBadLine && RasterX >= 54 && RasterX < CyclesPerLine;
    
    /// <summary>
    /// VICE-style: Screen memory address from registers
    /// </summary>
    public ushort ScreenMemoryBase => (ushort)((_registers[0x18] & 0xF0) << 6);
    
    /// <summary>
    /// VICE-style: Character generator base address
    /// </summary>
    public ushort CharacterBase => (ushort)((_registers[0x18] & 0x0E) << 10);
    
    /// <summary>
    /// VICE-style: Get current cycle within line
    /// </summary>
    public byte CurrentCycle => (byte)(RasterX % CyclesPerLine);
    
    /// <summary>
    /// VICE-style: Check if this a badline (raster line 30-49)
    /// </summary>
    public bool IsCurrentLineBad => IsBadLine;
    
    /// <summary>
    /// VICE-style: Frame rate based on TV system
    /// </summary>
    public double FrameRate => _frameRate;
    
    /// <summary>
    /// VICE-style: Cycles per line based on TV system
    /// </summary>
    public int CyclesPerLine => _cyclesPerLine;
    
    /// <summary>
    /// VICE-style: Visible lines based on TV system
    /// </summary>
    public int VisibleLines => _visibleLines;
    
    /// <summary>
    /// VICE-style: Total lines based on TV system
    /// </summary>
    public int TotalLines => _totalLines;
    
    public byte RasterX;
    public uint CycleCounter;
    public Func<ushort, byte>? VideoMemoryReader { get; set; }
    public Func<byte, byte>? Phi1MemoryReader { get; set; }
    public byte LastReadPhi1 { get; private set; }
    
    // VICE-style: Border configuration
    public enum BorderSide { None, Normal, Extended }
    
    /// <summary>
    /// Check if current position is in border area (VICE-style)
    /// </summary>
    public BorderSide GetHorizontalBorder()
    {
        // VICE-style: 40-column mode boundaries
        if (RasterX < 24 || RasterX >= 56 + 40) return BorderSide.Extended;
        if (RasterX < 31 || RasterX >= 56 + 31) return BorderSide.Normal;
        return BorderSide.None;
    }
    
    /// <summary>
    /// Check if current position is in vertical border area
    /// </summary>
    public BorderSide GetVerticalBorder()
    {
        if (!_verticalBorderActive)
            return BorderSide.None;

        return CurrentRasterLine < 51 || CurrentRasterLine >= 251
            ? BorderSide.Extended
            : BorderSide.Normal;
    }
    
    /// <summary>
    /// Get border color from register $20
    /// </summary>
    public byte BorderColor => _registers[0x20];
    
    /// <summary>
    /// Get background color from register $21
    /// </summary>
    public byte BackgroundColor => _registers[0x21];
    
    /// <summary>
    /// Get auxiliary color from register $22
    /// </summary>
    public byte AuxiliaryColor => _registers[0x22];
    
    // VICE-style: Video mode configuration
    public enum VideoMode { StandardText, MulticolorText, Bitmap, ExtendedBackground }

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-008.
    /// Abstract VIC-II display mode encoded by the three selector bits
    /// $D011 bit 5 (BMM), $D011 bit 6 (ECM), and $D016 bit 4 (MCM).
    /// Five valid combinations plus a single Invalid bucket for the
    /// ECM-with-BMM-or-MCM cases that the real chip renders as a
    /// black screen / garbage. Distinct from VideoMode (which collapses
    /// hi-res and multicolor bitmap into a single Bitmap entry and has
    /// no Invalid state): VicIIDisplayMode is the canonical mode for
    /// downstream pixel-pipeline routing.
    /// </summary>
    public enum VicIIDisplayMode
    {
        StandardText,
        MulticolorText,
        StandardBitmap,
        MulticolorBitmap,
        ExtendedColor,
        Invalid
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-008.
    /// Decode the three selector bits ($D011 bit 5 = BMM, $D011 bit 6
    /// = ECM, $D016 bit 4 = MCM) into one of the five valid display
    /// modes or Invalid. VideoRenderer routes visible pixels through this
    /// selector using the VICE display-mode color table.
    /// </summary>
    public VicIIDisplayMode DisplayModeSelection
    {
        get
        {
            bool ecm = (_registers[0x11] & 0x40) != 0;
            bool bmm = (_registers[0x11] & 0x20) != 0;
            bool mcm = (_registers[0x16] & 0x10) != 0;
            if (ecm && (bmm || mcm)) return VicIIDisplayMode.Invalid;
            if (ecm) return VicIIDisplayMode.ExtendedColor;
            if (bmm && mcm) return VicIIDisplayMode.MulticolorBitmap;
            if (bmm) return VicIIDisplayMode.StandardBitmap;
            if (mcm) return VicIIDisplayMode.MulticolorText;
            return VicIIDisplayMode.StandardText;
        }
    }
    
    /// <summary>
    /// Column mode (40 columns vs 38 columns)
    /// </summary>
    public enum ColumnMode { Normal38, Wide40 }
    
    /// <summary>
    /// Color mode (single or multicolor)
    /// </summary>
    public enum ColorMode { Single, Multi }
    
    /// <summary>
    /// Sprite expansion mode (normal or double)
    /// </summary>
    public enum SpriteExpansion { Normal, Double }
    
    /// <summary>
    /// Sprite display mode (normal or multicolor)
    /// </summary>
    public enum SpriteColorMode { Normal, Multi }
    
    /// <summary>
    /// Sprite priority relative to background
    /// </summary>
    public enum SpritePriority { InFront, Behind }
    
    /// <summary>
    /// Get current video mode from register $11 bit 4-6
    /// </summary>
    public VideoMode DisplayMode
    {
        get
        {
            var extendedBackground = (_registers[0x11] & 0x40) != 0;
            var bitmap = (_registers[0x11] & 0x20) != 0;
            var multicolor = (_registers[0x16] & 0x10) != 0;

            return (extendedBackground, bitmap, multicolor) switch
            {
                (false, false, false) => VideoMode.StandardText,
                (false, false, true) => VideoMode.MulticolorText,
                (false, true, _) => VideoMode.Bitmap,
                (true, false, false) => VideoMode.ExtendedBackground,
                _ => VideoMode.StandardText
            };
        }
    }
    
    /// <summary>
    /// Get character/color data pointer base (bits 0-3 of register $18)
    /// </summary>
    public ushort CharacterPointerBase => (ushort)((_registers[0x18] & 0x0F) << 10);
    
    /// <summary>
    /// Get bitmap pointer base (bit 3 of register $18)
    /// </summary>
    public ushort BitmapPointerBase => (ushort)((_registers[0x18] & 0x08) << 10);
    
    /// <summary>
    /// Column mode (40 columns vs 38 columns)
    /// </summary>
    public ColumnMode Columns => (_registers[0x16] & 0x08) switch
    {
        0 => ColumnMode.Normal38,
        _ => ColumnMode.Wide40
    };
    
    /// <summary>
    /// Color mode (single or multicolor)
    /// </summary>
    public ColorMode Color => (_registers[0x16] & 0x10) switch
    {
        0 => ColorMode.Single,
        _ => ColorMode.Multi
    };
    
    /// <summary>
    /// Get screen memory address (10-bit from registers)
    /// </summary>
    public ushort ScreenMemoryAddress => ScreenMemoryBase;
    
    // VICE-style: Sprite state
    private readonly SpriteState[] _sprites = new SpriteState[8];

    // BACKFILL-VIDEO-001: Sprite collision latches.
    // Accumulate per-scanline; cleared on Read of $D01E / $D01F.
    private byte _spriteSpriteCollisionLatch;
    private byte _spriteBackgroundCollisionLatch;
    // Track the last raster line at which we ran the collision raster so we
    // do it exactly once per scanline (at cycle 0).
    private int _lastCollisionRasterLine = -1;

    private struct SpriteState
    {
        public ushort X;
        public byte Y;
        public byte Control;
        public ushort DataPtr;
        public byte Color;
        public bool IsExpandedX;
        public bool IsExpandedY;
        public bool IsMulticolor;
        public bool IsPriority;
    }
    
    /// <summary>
    /// Get sprite X position (11-bit, VICE-style)
    /// </summary>
    public ushort GetSpriteX(int spriteNum) => _sprites[spriteNum].X;
    
    /// <summary>
    /// Get sprite Y position
    /// </summary>
    public byte GetSpriteY(int spriteNum) => _sprites[spriteNum].Y;
    
    /// <summary>
    /// Get sprite X expansion mode
    /// </summary>
    public SpriteExpansion GetSpriteExpansionX(int spriteNum) => _sprites[spriteNum].IsExpandedX 
        ? SpriteExpansion.Double 
        : SpriteExpansion.Normal;
    
    /// <summary>
    /// Get sprite Y expansion mode
    /// </summary>
    public SpriteExpansion GetSpriteExpansionY(int spriteNum) => _sprites[spriteNum].IsExpandedY 
        ? SpriteExpansion.Double 
        : SpriteExpansion.Normal;
    
    /// <summary>
    /// Get sprite color mode
    /// </summary>
    public SpriteColorMode GetSpriteColorMode(int spriteNum) => _sprites[spriteNum].IsMulticolor 
        ? SpriteColorMode.Multi 
        : SpriteColorMode.Normal;
    
    /// <summary>
    /// Get sprite priority
    /// </summary>
    public SpritePriority GetSpritePriority(int spriteNum) => _sprites[spriteNum].IsPriority 
        ? SpritePriority.Behind 
        : SpritePriority.InFront;
    
    /// <summary>
    /// Get sprite color
    /// </summary>
    public byte GetSpriteColor(int spriteNum) => _sprites[spriteNum].Color;
    
    /// <summary>
    /// Sprite collision types (VICE-style)
    /// </summary>
    public enum SpriteCollisionType { None, SpriteSprite, SpriteBackground }
    
    /// <summary>
    /// Get sprite-sprite collision mask from register $1E (non-clearing peek into the latch).
    /// </summary>
    public byte SpriteSpriteCollision => _spriteSpriteCollisionLatch;

    /// <summary>
    /// Get sprite-background collision mask from register $1F (non-clearing peek into the latch).
    /// </summary>
    public byte SpriteBackgroundCollision => _spriteBackgroundCollisionLatch;
    
    // VICE-style: Interrupt sources
    public enum InterruptSource { None, Raster, SpriteSprite, SpriteBackground, LightPen, Timer }
    
    /// <summary>
    /// Get interrupt flags from register $19
    /// </summary>
    public byte InterruptFlags => _registers[0x19];
    
    /// <summary>
    /// Get interrupt mask from register $1A
    /// </summary>
    public byte InterruptMask => _registers[0x1A];
    
    /// <summary>
    /// Check if raster interrupt is enabled
    /// </summary>
    public bool IsRasterInterruptEnabled => (_registers[0x1A] & 0x01) != 0;
    
    /// <summary>
    /// Get current raster IRQ line
    /// </summary>
    public ushort RasterIrqLine => _rasterIrqLine;
    
    /// <summary>
    /// Set raster IRQ line
    /// </summary>
    public void SetRasterIrqLine(ushort line)
    {
        _rasterIrqLine = (ushort)(line & 0x01FF);
        _rasterIrqCompareArmed = true;
    }
    
    /// <summary>
    /// Clear sprite collision flags
    /// </summary>
    public void ClearCollisionFlags()
    {
        _registers[0x1E] = 0;
        _registers[0x1F] = 0;
        _spriteSpriteCollisionLatch = 0;
        _spriteBackgroundCollisionLatch = 0;
    }

    /// <summary>
    /// Check if two sprites collide (VICE-style detection)
    /// </summary>
    public bool CheckSpriteCollision(int sprite1, int sprite2) =>
        (_spriteSpriteCollisionLatch & (1 << sprite1)) != 0 && (_spriteSpriteCollisionLatch & (1 << sprite2)) != 0;

    /// <summary>
    /// Check if sprite collides with background data
    /// </summary>
    public bool CheckSpriteBackgroundCollision(int spriteNum) =>
        (_spriteBackgroundCollisionLatch & (1 << spriteNum)) != 0;
    
    /// <summary>
    /// Check if sprite is visible at current raster position
    /// </summary>
    public bool IsSpriteVisible(int spriteNum)
    {
        ref SpriteState s = ref _sprites[spriteNum];
        uint width = s.IsExpandedX ? 48u : 24u;
        uint height = s.IsExpandedY ? 42u : 21u;
        return RasterX >= s.X && RasterX < s.X + width &&
               CurrentRasterLine >= s.Y && CurrentRasterLine < s.Y + height;
    }
    
    // VICE-style: RSEL/CSEL for border control
    private bool Rsel => (_registers[0x11] & 0x08) != 0;  // Row select (25 vs 24 rows)
    private bool Csel => (_registers[0x16] & 0x08) != 0;  // Column select (40 vs 38 cols)
    
    /// <summary>
    /// VICE-style: Upper border start line
    /// </summary>
    public int UpperBorderStart => Rsel ? 51 : 55;
    
    /// <summary>
    /// VICE-style: Lower border start line
    /// </summary>
    public int LowerBorderStart => Rsel ? 251 : 247;
    
    /// <summary>
    /// VICE-style: Left border pixel position
    /// </summary>
    public int LeftBorderPixel => Csel ? 24 : 31;
    
    /// <summary>
    /// VICE-style: Right border end pixel position
    /// </summary>
    public int RightBorderEndPixel => Csel ? 344 : 335;

    /// <summary>
    /// FR-VIC-007: current vertical border flip-flop state.
    /// </summary>
    public bool IsVerticalBorderActive => _verticalBorderActive;

    /// <summary>
    /// FR-VIC-007: current main horizontal border flip-flop state.
    /// </summary>
    public bool IsMainBorderActive => _mainBorderActive;

    /// <summary>
    /// FR-VIC-007: returns the captured vertical border state for a ticked
    /// raster line, or the static RSEL boundary result for render-only lines.
    /// </summary>
    public bool IsRasterLineVerticalBorderActive(int rasterLine)
    {
        if ((uint)rasterLine < (uint)_verticalBorderActiveByRasterLine.Length &&
            _verticalBorderLineCaptured[rasterLine])
        {
            return _verticalBorderActiveByRasterLine[rasterLine];
        }

        return rasterLine < UpperBorderStart || rasterLine >= LowerBorderStart;
    }

    /// <summary>
    /// FR-VIC-007: returns whether the right side border remained open on a
    /// ticked raster line because the horizontal border set check was skipped.
    /// </summary>
    public bool IsRasterLineRightBorderOpen(int rasterLine) =>
        (uint)rasterLine < (uint)_rightBorderOpenByRasterLine.Length &&
        _rightBorderLineCaptured[rasterLine] &&
        _rightBorderOpenByRasterLine[rasterLine];

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-004 / FR-VIC-007 / TEST-VIC-001:
    /// returns whether a sprite pixel is visible at the supplied VIC
    /// pixel coordinate once the closed border window is applied.
    /// </summary>
    public bool CanRenderSpritePixelAt(int xVicPixel, int rasterLine)
    {
        if (IsRasterLineVerticalBorderActive(rasterLine))
        {
            return false;
        }

        int rightBorderEndPixel = IsRasterLineRightBorderOpen(rasterLine)
            ? VideoRenderer.ScreenWidth
            : RightBorderEndPixel;

        return xVicPixel >= LeftBorderPixel && xVicPixel < rightBorderEndPixel;
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / BACKFILL-VIDEO-002 / FR-VIC-002 / FR-VIC-003 /
    /// FR-VIC-005 / FR-VIC-008 / TEST-VIC-001: returns the x64sc-style
    /// foreground/priority bit for a graphics pixel. Invalid ECM selector
    /// combinations still render as color 0, but VICE keeps the hidden
    /// <c>px &amp; 0x02</c> priority bit for sprite priority and
    /// sprite-background collision logic.
    /// </summary>
    public bool IsGraphicsPixelForegroundForSpritePriority(int xVicPixel, int rasterLine)
    {
        if (IsRasterLineVerticalBorderActive(rasterLine))
        {
            return false;
        }

        int screenX = xVicPixel - LeftBorderPixel;
        if (screenX < 0)
        {
            return false;
        }

        int columns = Columns == ColumnMode.Wide40 ? 40 : 38;
        if (screenX >= columns * 8)
        {
            return false;
        }

        int visLine = rasterLine - UpperBorderStart;
        if (visLine < 0 || rasterLine >= LowerBorderStart)
        {
            return false;
        }

        int screenLine = visLine + YScroll;
        int screenRowCount = Math.Max((LowerBorderStart - UpperBorderStart) / 8, 1);
        int row = Math.Max((screenLine / 8) % screenRowCount, 0);
        int col = screenX / 8;
        int charX = screenX % 8;
        int charRow = screenLine & 7;
        int screenIndex = row * columns + col;
        byte screenCode = ReadVideoMemory((ushort)(ScreenMemoryBase + screenIndex));
        byte colorCode = ReadVideoMemory((ushort)(0xD800 + screenIndex));

        return DisplayModeSelection switch
        {
            VicIIDisplayMode.StandardText => IsStandardTextForeground(screenCode, charRow, charX, extendedColor: false),
            VicIIDisplayMode.MulticolorText => IsMulticolorTextForeground(screenCode, colorCode, charRow, charX),
            VicIIDisplayMode.StandardBitmap => IsStandardBitmapForeground(screenIndex, charRow, charX),
            VicIIDisplayMode.MulticolorBitmap => IsMulticolorBitmapForeground(screenIndex, charRow, charX),
            VicIIDisplayMode.ExtendedColor => IsStandardTextForeground(screenCode, charRow, charX, extendedColor: true),
            VicIIDisplayMode.Invalid => IsInvalidDisplayModeForeground(screenCode, colorCode, screenIndex, charRow, charX),
            _ => false,
        };
    }
    
    /// <summary>
    /// VICE-style: Y scroll value (bits 0-2 of $D011)
    /// </summary>
    public byte YScroll => (byte)(_registers[0x11] & 0x07);

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-007 /
    /// TEST-VIC-001: X scroll value
    /// (bits 0-2 of $D016). Consumed by the pixel sequencer to shift
    /// rendered pixels 0-7 to the right; mode-specific color routing is
    /// covered by VideoRenderer.
    /// </summary>
    public byte XScroll => (byte)(_registers[0x16] & 0x07);
    
    /// <summary>
    /// VICE-style: Is this a forced badline (FLI support)
    /// </summary>
    public bool IsForcedBadline => IsDisplayEnabled && (CurrentRasterLine & 7) == YScroll;
    
    /// <summary>
    /// VICE-style: Get VIC bank from CIA2 port A (bank selection)
    /// </summary>
    public int VicBank { get; set; } = 3;  // Default bank 3 ($C000-$FFFF)
    
    /// <summary>
    /// VICE-style: Translate VIC address to system address
    /// </summary>
    public ushort TranslateVicAddress(ushort vicAddr)
    {
        // VIC addresses 14 bits, translate to 16-bit with bank
        return (ushort)((VicBank << 14) | (vicAddr & 0x3FFF));
    }

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

    public Mos6569(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
        _renderer = new VideoRenderer(this);
    }

    public void ConfigureTiming(TvSystem system, int cyclesPerLine, int visibleLines, int totalLines, double frameRate)
    {
        if (cyclesPerLine <= 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerLine));
        if (visibleLines <= 0)
            throw new ArgumentOutOfRangeException(nameof(visibleLines));
        if (totalLines < visibleLines)
            throw new ArgumentOutOfRangeException(nameof(totalLines));
        if (!double.IsFinite(frameRate) || frameRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameRate));

        System = system;
        _cyclesPerLine = cyclesPerLine;
        _visibleLines = visibleLines;
        _totalLines = totalLines;
        _frameRate = frameRate;
    }

    /// <inheritdoc />
    public void Tick()
    {
        CycleCounter++;
        RasterX++;

        // VICE-style raster interrupt (at cycle 58 of line):
        // the latch in $D019 bit 0 is set unconditionally on compare match;
        // RefreshInterruptLine then gates the IRQ output by $D01A enable.
        // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: latch is independent of enable.
        if (_rasterIrqCompareArmed && RasterX == 58 && CurrentRasterLine == _rasterIrqLine)
        {
            _registers[0x19] |= 0x01;
            RefreshInterruptLine();
        }

        if (RasterX >= CyclesPerLine)
        {
            CaptureHorizontalBorderForCompletedLine(CurrentRasterLine);
            RasterX = 0;
            CurrentRasterLine++;
            _rasterIrqCompareArmed = true;

            if (CurrentRasterLine >= TotalLines)
            {
                CurrentRasterLine = 0;
                _refreshCounter = 0xFF;
                _allowBadLines = false;
                // BACKFILL-VIDEO-001: per-frame bad-line counter resets at the
                // frame boundary (raster wrap back to line 0).
                _badLineCountThisFrame = 0;
                _lastBadLineCounted = -1;
                // BACKFILL-VIDEO-001: per-frame sprite DMA counter also
                // resets on the frame wrap so each frame is independent.
                _spriteDmaCyclesThisFrame = 0;
                _lastSpriteDmaLineCounted = -1;
                _spriteDmaActiveMask = 0;
                Array.Clear(_spriteDmaStartLines, 0, _spriteDmaStartLines.Length);
                Array.Clear(_spriteDmaHeights, 0, _spriteDmaHeights.Length);
                // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: clear the LP "already latched
                // this frame" flag so the next LP trigger can re-arm. The
                // last latched X/Y values are kept (consistent with reading
                // $D013/$D014 between frames).
                _lightPenTriggeredThisFrame = false;
            }

            UpdateVerticalBorderForLineStart();
            UpdateBadLineLatchForStartOfLine();

            // BACKFILL-VIDEO-001 / FR-VIC-006 / TR-CYCLE-001 / TEST-VIC-001:
            // count this scanline as a bad line
            // if it qualifies, exactly once per line.
            if (IsBadLine && _lastBadLineCounted != CurrentRasterLine)
            {
                _badLineCountThisFrame++;
                _lastBadLineCounted = CurrentRasterLine;
            }

            // BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 /
            // TEST-VIC-001: account sprite DMA cycle theft
            // for this scanline, exactly once per line.
            AccountSpriteDmaForRasterLine(CurrentRasterLine);

            // BACKFILL-VIDEO-001: compute sprite collisions once per scanline.
            ProcessSpriteCollisionsForRasterLine(CurrentRasterLine);
        }
        else
        {
            UpdateBorderFlipFlopsForCurrentCycle();
        }

        UpdatePalSpriteDmaLatchForCurrentCycle();

        LastReadPhi1 = Phi1MemoryReader?.Invoke(CurrentCycle) ?? 0;

        // Update raster register
        _renderer.Tick();
        _registers[0x12] = (byte)CurrentRasterLine;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_registers, 0, _registers.Length);
        CurrentRasterLine = 0;
        RasterX = ResetRasterCycle;
        CycleCounter = 0;
        _rasterIrqLine = 0;
        _rasterIrqCompareArmed = false;
        _verticalBorderActive = true;
        _verticalBorderNextActive = true;
        _mainBorderActive = true;
        Array.Fill(_verticalBorderActiveByRasterLine, true);
        Array.Clear(_verticalBorderLineCaptured, 0, _verticalBorderLineCaptured.Length);
        Array.Clear(_rightBorderOpenByRasterLine, 0, _rightBorderOpenByRasterLine.Length);
        Array.Clear(_rightBorderLineCaptured, 0, _rightBorderLineCaptured.Length);
        CaptureVerticalBorderForCurrentLine();
        _allowBadLines = false;
        _refreshCounter = 0;
        Array.Clear(_videoBuffer, 0, _videoBuffer.Length);
        _videoCounter = 0;
        _rowCounter = 0;
        _videoMatrixLineIndex = 0;
        _idleState = false;
        LastReadPhi1 = 0;
        // BACKFILL-VIDEO-001: clear collision accumulators on reset.
        _spriteSpriteCollisionLatch = 0;
        _spriteBackgroundCollisionLatch = 0;
        _lastCollisionRasterLine = -1;
        // BACKFILL-VIDEO-001: clear per-frame bad-line counter on reset.
        _badLineCountThisFrame = 0;
        _lastBadLineCounted = -1;
        // BACKFILL-VIDEO-001: clear per-frame sprite-DMA counter on reset.
        _spriteDmaCyclesThisFrame = 0;
        _lastSpriteDmaLineCounted = -1;
        _spriteDmaActiveMask = 0;
        Array.Clear(_spriteDmaStartLines, 0, _spriteDmaStartLines.Length);
        Array.Clear(_spriteDmaHeights, 0, _spriteDmaHeights.Length);
        // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: clear light-pen latch state on reset.
        _lightPenLatchedX = 0;
        _lightPenLatchedY = 0;
        _lightPenTriggeredThisFrame = false;
    }

    // BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001 /
    // TEST-VIC-001: sprite DMA cycle accounting.
    // For each enabled sprite n that has its vertical extent intersect
    // the supplied raster line, the chip steals two CPU cycles on that
    // line for s-data fetches. A normal sprite spans 21 raster lines
    // (Y..Y+20); a Y-expanded sprite spans 42 raster lines. This routine
    // runs exactly once per scanline (gated by _lastSpriteDmaLineCounted)
    // and accumulates into _spriteDmaCyclesThisFrame. Composes additively
    // with bad-line cycle theft - the two are tracked independently.
    private void AccountSpriteDmaForRasterLine(int rasterLine)
    {
        if (_lastSpriteDmaLineCounted == rasterLine)
        {
            return;
        }
        _lastSpriteDmaLineCounted = rasterLine;

        byte enabled = _registers[0x15];
        if (enabled == 0)
        {
            return;
        }

        int intersectingSprites = 0;
        for (int n = 0; n < 8; n++)
        {
            if ((enabled & (1 << n)) == 0)
            {
                continue;
            }

            ref SpriteState s = ref _sprites[n];
            int spriteY = s.Y;
            int height = s.IsExpandedY ? 42 : 21;
            int row = rasterLine - spriteY;
            if (row < 0 || row >= height)
            {
                continue;
            }
            intersectingSprites++;
        }

        // Two CPU cycles stolen per sprite that intersects this scanline.
        _spriteDmaCyclesThisFrame += intersectingSprites * 2;
    }

    private void UpdateVerticalBorderForLineStart()
    {
        CheckVerticalBorderTopForCurrentLine();
        CheckVerticalBorderBottomForCurrentLine();
        CaptureVerticalBorderForCurrentLine();
    }

    private void UpdateBorderFlipFlopsForCurrentCycle()
    {
        if (RasterX == LeftBorderCheckCycle)
        {
            CheckVerticalBorderBottomForCurrentLine();
            _verticalBorderActive = _verticalBorderNextActive;
            CaptureVerticalBorderForCurrentLine();
            if (!_verticalBorderActive)
                _mainBorderActive = false;
        }

        if (RasterX == RightBorderCheckCycle)
            _mainBorderActive = true;
    }

    private void CheckVerticalBorderTopForCurrentLine()
    {
        if (CurrentRasterLine == UpperBorderStart && IsDisplayEnabled)
        {
            _verticalBorderActive = false;
            _verticalBorderNextActive = false;
        }
    }

    private void CheckVerticalBorderBottomForCurrentLine()
    {
        if (CurrentRasterLine == LowerBorderStart)
            _verticalBorderNextActive = true;
    }

    private int LeftBorderCheckCycle => Csel ? 17 : 18;

    private int RightBorderCheckCycle => Csel ? 57 : 56;

    private void CaptureVerticalBorderForCurrentLine()
    {
        if ((uint)CurrentRasterLine >= (uint)_verticalBorderActiveByRasterLine.Length)
            return;

        _verticalBorderActiveByRasterLine[CurrentRasterLine] = _verticalBorderActive;
        _verticalBorderLineCaptured[CurrentRasterLine] = true;
    }

    private void CaptureHorizontalBorderForCompletedLine(int rasterLine)
    {
        if ((uint)rasterLine >= (uint)_rightBorderOpenByRasterLine.Length)
            return;

        _rightBorderOpenByRasterLine[rasterLine] = !_verticalBorderActive && !_mainBorderActive;
        _rightBorderLineCaptured[rasterLine] = true;
    }

    // BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001:
    // PAL x64sc sprite p-/s-access pairs from VICE cycle_tab_pal in
    // native/vice/vice/src/viciisc/vicii-chip-model.c.
    private static readonly SpriteDmaAccess[] PalSpriteDmaAccesses =
    [
        new(3, 0),
        new(4, 2),
        new(5, 4),
        new(6, 6),
        new(7, 8),
        new(0, 57),
        new(1, 59),
        new(2, 61),
    ];

    private readonly record struct SpriteDmaAccess(int SpriteNumber, int FirstCurrentCycle);

    private void UpdatePalSpriteDmaLatchForCurrentCycle()
    {
        if (CyclesPerLine != PalCyclesPerLine)
        {
            return;
        }

        if (RasterX == 0)
        {
            ClearExpiredSpriteDmaLatches();
            return;
        }

        // VICE x64sc checks sprite DMA at PAL public cycles 55 and 56
        // (zero-based CurrentCycle/RasterX 54 and 55). $D015 and sprite Y
        // are sampled here; later BA/data slots use the sprite_dma latch.
        if (RasterX is 54 or 55)
        {
            LatchSpriteDmaForCurrentLine();
        }
    }

    private void LatchSpriteDmaForCurrentLine()
    {
        byte enabled = _registers[0x15];
        if (enabled == 0)
        {
            return;
        }

        int rasterLow = CurrentRasterLine & 0xFF;
        for (int spriteNumber = 0; spriteNumber < 8; spriteNumber++)
        {
            byte bit = (byte)(1 << spriteNumber);
            if ((enabled & bit) == 0 || (_spriteDmaActiveMask & bit) != 0)
            {
                continue;
            }

            ref SpriteState sprite = ref _sprites[spriteNumber];
            if (sprite.Y != rasterLow)
            {
                continue;
            }

            _spriteDmaActiveMask |= bit;
            _spriteDmaStartLines[spriteNumber] = CurrentRasterLine;
            _spriteDmaHeights[spriteNumber] = (byte)(sprite.IsExpandedY ? 42 : 21);
        }
    }

    private void ClearExpiredSpriteDmaLatches()
    {
        for (int spriteNumber = 0; spriteNumber < 8; spriteNumber++)
        {
            byte bit = (byte)(1 << spriteNumber);
            if ((_spriteDmaActiveMask & bit) == 0)
            {
                continue;
            }

            int height = _spriteDmaHeights[spriteNumber] == 0 ? 21 : _spriteDmaHeights[spriteNumber];
            int elapsedLines = NormalizeRasterLine(CurrentRasterLine - _spriteDmaStartLines[spriteNumber]);
            if (elapsedLines < height)
            {
                continue;
            }

            _spriteDmaActiveMask = (byte)(_spriteDmaActiveMask & ~bit);
            _spriteDmaStartLines[spriteNumber] = 0;
            _spriteDmaHeights[spriteNumber] = 0;
        }
    }

    private bool IsSpriteEnabledAndIntersectingLine(int spriteNumber, int rasterLine)
    {
        byte enabled = _registers[0x15];
        if (enabled == 0)
        {
            return false;
        }

        if ((enabled & (1 << spriteNumber)) == 0)
        {
            return false;
        }

        ref SpriteState s = ref _sprites[spriteNumber];
        int spriteY = s.Y;
        int height = s.IsExpandedY ? 42 : 21;
        int row = rasterLine - spriteY;
        return row >= 0 && row < height;
    }

    // BACKFILL-VIDEO-001 / FR-VIC-006 / FR-VIC-010 / TR-CYCLE-001:
    // sprite-DMA cycle-steal window.
    //
    // VICE's PAL table uses one-based raster cycles 1..63 and then stores
    // them in cycle_table[cycle - 1]. The values above are already
    // normalized to vice-sharp CurrentCycle/RasterX terms. Each sprite's
    // BA mask starts three cycles before its first p-access and remains
    // asserted through the second s-access cycle.
    //
    // leadingEdgeOffset = 0 -&gt; matches IsCpuCycleStolen window.
    // leadingEdgeOffset = 1 -&gt; mandatory window (one cycle later on the
    // leading edges, mirroring bad-line semantics: stolen is 12..54,
    // mandatory is 13..55).
    //
    // Non-PAL tables are still a backfill target; until they are imported,
    // this helper preserves the prior coarse behavior for non-63-cycle
    // models instead of pretending the PAL table applies to them.
    private bool IsInSpriteDmaStallWindow(int leadingEdgeOffset)
    {
        if (CyclesPerLine != PalCyclesPerLine)
        {
            return IsInCoarseSpriteDmaStallWindow(leadingEdgeOffset);
        }

        foreach (SpriteDmaAccess access in PalSpriteDmaAccesses)
        {
            if (IsSpriteDmaBaSlotActive(access, leadingEdgeOffset))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSpriteDmaBaSlotActive(SpriteDmaAccess access, int leadingEdgeOffset)
    {
        for (int delta = -3; delta <= 1; delta++)
        {
            int cycle = access.FirstCurrentCycle + delta + leadingEdgeOffset;
            MapCurrentCycleToRasterX(cycle, out int rasterLineOffset, out int rasterX);
            if (RasterX != rasterX)
            {
                continue;
            }

            int accessLine = NormalizeRasterLine(CurrentRasterLine - rasterLineOffset);
            if (IsSpriteDmaActiveForAccessLine(access.SpriteNumber, accessLine))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSpriteDmaActiveForAccessLine(int spriteNumber, int accessLine)
    {
        byte bit = (byte)(1 << spriteNumber);
        if ((_spriteDmaActiveMask & bit) == 0)
        {
            return false;
        }

        int height = _spriteDmaHeights[spriteNumber] == 0 ? 21 : _spriteDmaHeights[spriteNumber];
        int elapsedLines = NormalizeRasterLine(accessLine - _spriteDmaStartLines[spriteNumber]);
        return elapsedLines >= 0 && elapsedLines < height;
    }

    private void MapCurrentCycleToRasterX(int cycle, out int rasterLineOffset, out int rasterX)
    {
        rasterLineOffset = Math.DivRem(cycle, PalCyclesPerLine, out rasterX);
        if (rasterX < 0)
        {
            rasterLineOffset--;
            rasterX += PalCyclesPerLine;
        }
    }

    private int NormalizeRasterLine(int rasterLine)
    {
        int normalized = rasterLine % TotalLines;
        return normalized < 0 ? normalized + TotalLines : normalized;
    }

    private bool IsInCoarseSpriteDmaStallWindow(int leadingEdgeOffset)
    {
        int trailingEnd = 8 + leadingEdgeOffset;
        if (RasterX < trailingEnd)
        {
            return IsAnySpriteEnabledAndIntersectingLine(CurrentRasterLine);
        }

        int leadingStart = 55 + leadingEdgeOffset;
        if (RasterX >= leadingStart && RasterX < CyclesPerLine)
        {
            if (IsAnySpriteEnabledAndIntersectingLine(CurrentRasterLine))
            {
                return true;
            }

            int nextLine = NormalizeRasterLine(CurrentRasterLine + 1);
            return IsAnySpriteEnabledAndIntersectingLine(nextLine);
        }

        return false;
    }

    private bool IsAnySpriteEnabledAndIntersectingLine(int rasterLine)
    {
        byte enabled = _registers[0x15];
        if (enabled == 0)
        {
            return false;
        }

        for (int n = 0; n < 8; n++)
        {
            if (IsSpriteEnabledAndIntersectingLine(n, rasterLine))
            {
                return true;
            }
        }

        return false;
    }

    public byte ConsumeRefreshCounter()
    {
        var value = _refreshCounter;
        _refreshCounter--;
        return value;
    }

    public ushort ConsumeGraphicsFetchAddress()
    {
        ushort address;

        if ((_registers[0x11] & 0x20) != 0)
        {
            address = (ushort)((_videoCounter << 3) | _rowCounter);
            address |= (ushort)((_registers[0x18] & 0x08) << 10);
        }
        else
        {
            var character = _videoBuffer[_videoMatrixLineIndex % _videoBuffer.Length];
            address = (ushort)((character << 3) | _rowCounter);
            address |= (ushort)((_registers[0x18] & 0x0E) << 10);
        }

        if ((_registers[0x11] & 0x40) != 0)
            address &= 0x39FF;

        _videoMatrixLineIndex = (_videoMatrixLineIndex + 1) % _videoBuffer.Length;
        _videoCounter = (ushort)((_videoCounter + 1) & 0x03FF);
        return address;
    }

    public bool IsGraphicsIdle => _idleState;

    /// <inheritdoc />
    public byte Peek(ushort offset)
    {
        int register = (offset - BaseAddress) & 0x3F;

        // BACKFILL-VIDEO-001: Peek is debug-only; never disturb collision latches.
        if (register == 0x1E)
        {
            return _spriteSpriteCollisionLatch;
        }

        if (register == 0x1F)
        {
            return _spriteBackgroundCollisionLatch;
        }

        // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: Peek $D013 / $D014 returns the LP
        // latched values rather than the raw _registers backing store.
        if (register == 0x13)
        {
            return _lightPenLatchedX;
        }

        if (register == 0x14)
        {
            return _lightPenLatchedY;
        }

        return _registers[register];
    }

    /// <inheritdoc />
    public byte Read(ushort offset)
    {
        int register = (offset - BaseAddress) & 0x3F;

        if (register == 0x19)
        {
            return _registers[0x19];
        }

        if (register == 0x11)
        {
            return (byte)((_registers[0x11] & 0x7F) | ((CurrentRasterLine & 0x100) >> 1));
        }

        // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: $D013 returns the latched RasterX >> 1
        // (or 0 if no LP trigger has fired yet this frame).
        if (register == 0x13)
        {
            return _lightPenLatchedX;
        }

        // BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: $D014 returns the latched raster line
        // low byte (or 0 if no LP trigger has fired yet this frame).
        if (register == 0x14)
        {
            return _lightPenLatchedY;
        }

        // BACKFILL-VIDEO-001: $D01E sprite-sprite collision register.
        // Read-and-clear semantics: return the accumulated mask, then clear.
        if (register == 0x1E)
        {
            byte value = _spriteSpriteCollisionLatch;
            _spriteSpriteCollisionLatch = 0;
            return value;
        }

        // BACKFILL-VIDEO-001: $D01F sprite-background collision register.
        // Read-and-clear semantics: return the accumulated mask, then clear.
        if (register == 0x1F)
        {
            byte value = _spriteBackgroundCollisionLatch;
            _spriteBackgroundCollisionLatch = 0;
            return value;
        }

        // BACKFILL-VIDEO-001 / FR-VIC-004 / FR-VIC-007 / TEST-VIC-001:
        // Color registers $D020-$D02E use only the
        // low 4 bits to encode a 16-color palette index. The upper 4 bits are
        // unconnected on the chip and float high, so reads always report them
        // as 1. This matches real-hardware behavior (writing $05 reads back as
        // $F5).
        if (register >= 0x20 && register <= 0x2E)
        {
            return (byte)(_registers[register] | 0xF0);
        }

        // BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-007 /
        // TEST-VIC-001: $D016 control register 2. Bits 7-6 are
        // unconnected on the real chip and float high; bit 5 is reserved
        // (RES). Bits 4-0 (MCM, CSEL, XSCROLL2..0) carry real state. Force
        // bits 7-6 to 1 on read.
        if (register == 0x16)
        {
            return (byte)(_registers[register] | 0xC0);
        }

        // BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-008 /
        // FR-VIC-009 / TEST-VIC-001: $D018 memory pointers. Bit 0 is
        // unconnected on the real chip and floats high; bits 7-4 are the
        // video matrix base (VM13..VM10) and bits 3-1 are the character
        // bitmap base (CB13..CB11). Force bit 0 to 1 on read.
        if (register == 0x18)
        {
            return (byte)(_registers[register] | 0x01);
        }

        return _registers[register];
    }

    /// <inheritdoc />
    public void Write(ushort offset, byte value)
    {
        int register = (offset - BaseAddress) & 0x3F;

        if (register == 0x19)
        {
            _registers[register] &= (byte)~(value & InterruptSourceMask);
            RefreshInterruptLine();
            return;
        }

        if (register == 0x12)
        {
            _rasterIrqLine = (ushort)((_rasterIrqLine & 0x100) | value);
            _rasterIrqCompareArmed = true;
            return;
        }

        if (register == 0x11)
        {
            _rasterIrqLine = (ushort)((_rasterIrqLine & 0x0FF) | ((value & 0x80) << 1));
            _rasterIrqCompareArmed = true;
            _registers[register] = value;
            return;
        }

        if (register == 0x1A)
        {
            _registers[register] = (byte)(value & InterruptSourceMask);
            RefreshInterruptLine();
            return;
        }

        // BACKFILL-VIDEO-001 / FR-VIC-004 / FR-VIC-007 / TEST-VIC-001:
        // Color registers $D020-$D02E only latch
        // the low 4 bits on write. Upper 4 bits are unconnected on the chip
        // and are ignored on write (and float high on read, applied above).
        if (register >= 0x20 && register <= 0x2E)
        {
            byte masked = (byte)(value & 0x0F);
            _registers[register] = masked;
            UpdateSpriteRegisters((ushort)register, masked);
            return;
        }

        _registers[register] = value;

        // VICE-style: Update sprite registers
        UpdateSpriteRegisters((ushort)register, value);
    }
    
    private void UpdateSpriteRegisters(ushort offset, byte value)
    {
        // Sprite X low / sprite Y pairs (0x00..0x0F):
        // even offsets ($00, $02, ..., $0E) = sprite X low byte
        // odd  offsets ($01, $03, ..., $0F) = sprite Y position
        if (offset < 0x10)
        {
            int sprite = offset / 2;
            if ((offset & 0x01) == 0)
            {
                _sprites[sprite].X = (ushort)((_sprites[sprite].X & 0xFF00) | value);
            }
            else
            {
                _sprites[sprite].Y = value;
            }
        }
        // Sprite X MSB (0x10): bit n = sprite n high X bit.
        else if (offset == 0x10)
        {
            for (int i = 0; i < 8; i++)
            {
                if ((value & (1 << i)) != 0)
                    _sprites[i].X |= 0x100;
                else
                    _sprites[i].X &= 0xFF;
            }
        }
        // $D017 Sprite Y Expansion: bit n = sprite n is Y-expanded.
        else if (offset == 0x17)
        {
            for (int i = 0; i < 8; i++)
            {
                _sprites[i].IsExpandedY = (value & (1 << i)) != 0;
            }
        }
        // $D01B Sprite-data priority (bit n = sprite n behind background data).
        else if (offset == 0x1B)
        {
            for (int i = 0; i < 8; i++)
            {
                // IsPriority == true means sprite behind background (matches existing
                // semantic in GetSpritePriority returning SpritePriority.Behind).
                _sprites[i].IsPriority = (value & (1 << i)) != 0;
            }
        }
        // $D01C Sprite Multicolor: bit n = sprite n in multicolor mode.
        else if (offset == 0x1C)
        {
            for (int i = 0; i < 8; i++)
            {
                _sprites[i].IsMulticolor = (value & (1 << i)) != 0;
            }
        }
        // $D01D Sprite X Expansion: bit n = sprite n is X-expanded.
        else if (offset == 0x1D)
        {
            for (int i = 0; i < 8; i++)
            {
                _sprites[i].IsExpandedX = (value & (1 << i)) != 0;
            }
        }
        // Sprite colors $D027-$D02E (per-sprite individual color).
        else if (offset >= 0x27 && offset <= 0x2E)
        {
            int sprite = offset - 0x27;
            _sprites[sprite].Color = (byte)(value & 0x0F);
        }
        // $D01E (sprite-sprite collision) and $D01F (sprite-background collision)
        // are read-only collision latches; writes do not configure sprite state.
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
    {
        return address >= BaseAddress && address < BaseAddress + 0x0400;
    }

    public byte ReadVideoMemory(ushort address)
    {
        return VideoMemoryReader?.Invoke(address) ?? _bus.Read(address);
    }

    private bool IsBadLineRaster(ushort rasterLine)
    {
        return rasterLine >= FirstDmaLine
            && rasterLine <= LastDmaLine
            && (rasterLine & 0x07) == YScroll;
    }

    private void UpdateBadLineLatchForStartOfLine()
    {
        if (CurrentRasterLine == FirstDmaLine && !_allowBadLines && IsDisplayEnabled)
            _allowBadLines = true;

        if (CurrentRasterLine > LastDmaLine)
            _allowBadLines = false;
    }

    private void RefreshInterruptLine()
    {
        var activeEnabled = (_registers[0x19] & _registers[0x1A] & InterruptSourceMask) != 0;
        if (activeEnabled)
        {
            _registers[0x19] |= 0x80;
            _irqLine.Assert(this);
        }
        else
        {
            _registers[0x19] &= 0x7F;
            _irqLine.Release(this);
        }
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC-001 / TEST-VIC-001: Simulate a high-to-low transition on the
    /// VIC-II LP (light pen) pin. On the first trigger of the current frame
    /// the chip latches the current RasterX shifted right by one into $D013
    /// and the low 8 bits of the current raster line into $D014, sets the
    /// LP IRQ latch ($D019 bit 3), and asserts the IRQ output if the LP
    /// enable bit in $D01A is set. Second and later triggers within the
    /// same frame are ignored. The latch re-arms when the raster wraps
    /// back to line 0 (frame boundary).
    /// </summary>
    public void TriggerLightPen()
    {
        if (_lightPenTriggeredThisFrame)
        {
            return;
        }

        _lightPenTriggeredThisFrame = true;
        _lightPenLatchedX = (byte)(RasterX >> 1);
        _lightPenLatchedY = (byte)(CurrentRasterLine & 0xFF);
        _registers[0x19] |= 0x08;
        RefreshInterruptLine();
    }
    
    /// <summary>
    /// VICE-style: Get pixel color at given raster position
    /// </summary>
    public byte GetPixelColor(byte x, byte y)
    {
        int xPos = x;
        int yPos = y;
        int leftBorderPixel = LeftBorderPixel;
        int rightBorderEndPixel = RightBorderEndPixel;
        int upperBorder = UpperBorderStart;
        int lowerBorder = LowerBorderStart;

        if (IsRasterLineVerticalBorderActive(yPos))
            return BorderColor;

        if (xPos < leftBorderPixel || xPos >= rightBorderEndPixel || xPos >= 320)
            return BorderColor;

        int columns = Columns == ColumnMode.Wide40 ? 40 : 38;
        int visibleLine = yPos - upperBorder;
        int screenLine = visibleLine + YScroll;
        int rowCount = Math.Max((lowerBorder - upperBorder) / 8, 1);
        int row = Math.Max((screenLine / 8) % rowCount, 0);

        int screenX = x - leftBorderPixel;
        if (screenX >= columns * 8)
            return BorderColor;

        int col = screenX / 8;
        int charX = screenX % 8;
        int charOffset = row * columns + col;
        
        // Get character from screen memory
        byte charCode = ReadVideoMemory((ushort)(ScreenMemoryBase + charOffset));
        
        // Get bitmap data based on display mode
        switch (DisplayMode)
        {
            case VideoMode.StandardText:
            case VideoMode.MulticolorText:
                // Fetch character line from ROM
                byte charLine = ReadVideoMemory((ushort)(CharacterBase + charCode * 8 + (screenLine & 0x07)));
                // Get bit within byte (x % 8, leftmost bit at position 7)
                int bitPos = 7 - charX;
                byte colorIndex = (byte)((charLine >> bitPos) & 0x01);
                return colorIndex != 0 ? (byte)0x0E : BackgroundColor; // White or background
                
            case VideoMode.Bitmap:
                // Direct bitmap mode
                int bitmapOffset = charCode * 64 + (screenLine & 0x07) * 8 + charX;
                byte bitmapByte = ReadVideoMemory((ushort)(BitmapPointerBase + bitmapOffset));
                return (byte)(bitmapByte & 0x0F);
                
            case VideoMode.ExtendedBackground:
            default:
                return BackgroundColor;
        }
    }
    
    /// <summary>
    /// VICE-style: Generate a complete frame of pixel data (320x200 visible area)
    /// </summary>
    public void GenerateFrame(Span<byte> frameBuffer)
    {
        if (frameBuffer.Length < 320 * 200)
            return;

        for (int y = 0; y < 200; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                frameBuffer[y * 320 + x] = GetPixelColor((byte)x, (byte)y);
            }
        }
    }

    // BACKFILL-VIDEO-001: minimal sprite collision raster.
    //
    // Approximation note: production Mos6569 has no per-pixel sprite composition
    // pipeline yet, so this routine performs a stand-alone per-scanline raster
    // that builds an 8-sprite opacity mask across the 320-pixel visible main
    // screen area plus a background foreground mask, then ORs the detected
    // collisions into the per-frame latches that are returned (and cleared) on
    // Read of $D01E / $D01F. Multicolor sprites and Y/X expansion are honoured
    // for the shape mask (each pixel is treated as one source bit; multicolor
    // pairs are flattened to "opaque if pair != 00"). Sprite-sprite priority
    // ordering does not affect collision detection (per FR-VIC-005: any two opaque
    // sprite pixels at the same coord collide regardless of priority).
    private void ProcessSpriteCollisionsForRasterLine(int rasterLine)
    {
        // Avoid double-processing if Tick is called in unusual orders.
        if (_lastCollisionRasterLine == rasterLine)
        {
            return;
        }
        _lastCollisionRasterLine = rasterLine;

        byte enabled = _registers[0x15];
        // Need at least one sprite enabled for any collision to occur.
        if (enabled == 0)
        {
            return;
        }

        // Per-pixel sprite opacity bitmask across the VIC pixel x range 0..503.
        // Backed by a stackalloc to keep this allocation-free on the hot path.
        const int PixelSpan = 504;
        Span<byte> spriteMask = stackalloc byte[PixelSpan];
        spriteMask.Clear();

        bool anyHit = false;
        for (int n = 0; n < 8; n++)
        {
            if ((enabled & (1 << n)) == 0)
            {
                continue;
            }

            ref SpriteState s = ref _sprites[n];
            int height = s.IsExpandedY ? 42 : 21;
            // Sprite Y is in raster-line coordinates.
            int spriteY = s.Y;
            int row = rasterLine - spriteY;
            if (row < 0 || row >= height)
            {
                continue;
            }

            // Source row inside the 21-row sprite shape after un-expanding.
            int sourceRow = s.IsExpandedY ? row / 2 : row;

            // Sprite data pointer lives at screen RAM + $03F8 + n.
            byte ptr = ReadVideoMemory((ushort)(ScreenMemoryBase + 0x03F8 + n));
            // Sprite data block is ptr * 64; each row is 3 bytes.
            ushort baseAddr = (ushort)(ptr * 64 + sourceRow * 3);
            byte b0 = ReadVideoMemory(baseAddr);
            byte b1 = ReadVideoMemory((ushort)(baseAddr + 1));
            byte b2 = ReadVideoMemory((ushort)(baseAddr + 2));

            // Sprite X is a 9-bit value in VIC pixel space (24 = leftmost main screen pixel).
            int spriteX = s.X;
            int width = s.IsExpandedX ? 48 : 24;
            byte bit = (byte)(1 << n);

            for (int px = 0; px < width; px++)
            {
                int sourceCol = s.IsExpandedX ? px / 2 : px;
                int byteIdx = sourceCol / 8;
                int bitIdx = 7 - (sourceCol % 8);
                byte src = byteIdx switch
                {
                    0 => b0,
                    1 => b1,
                    _ => b2,
                };

                bool opaque;
                if (s.IsMulticolor)
                {
                    // Multicolor: pairs of bits encode 4 colors; 00 is transparent.
                    // Sample the pair anchored at the even source column.
                    int evenSource = sourceCol & ~1;
                    int evenByte = evenSource / 8;
                    int evenBit = 7 - (evenSource % 8);
                    byte mcSrc = evenByte switch
                    {
                        0 => b0,
                        1 => b1,
                        _ => b2,
                    };
                    int pair = ((mcSrc >> (evenBit - 1)) & 0x03);
                    opaque = pair != 0;
                }
                else
                {
                    opaque = ((src >> bitIdx) & 0x01) != 0;
                }

                if (!opaque)
                {
                    continue;
                }

                int x = spriteX + px;
                if ((uint)x >= (uint)PixelSpan)
                {
                    continue;
                }
                spriteMask[x] |= bit;
                anyHit = true;
            }
        }

        if (!anyHit)
        {
            return;
        }

        // Sweep the mask to detect sprite-sprite overlaps and to AND against
        // the background foreground bitmask for sprite-background collisions.
        // Only compute the background bitmask for x positions where a sprite
        // actually contributes opacity (skip when none).
        byte spriteSpriteAcc = 0;
        byte spriteBackgroundAcc = 0;

        for (int x = 0; x < PixelSpan; x++)
        {
            byte m = spriteMask[x];
            if (m == 0)
            {
                continue;
            }
            // Sprite-sprite: any byte with two-or-more bits set means a multi-sprite overlap.
            if ((m & (m - 1)) != 0)
            {
                spriteSpriteAcc |= m;
            }
            // Sprite-background: only if we are within the visible character window AND
            // the underlying character/bitmap pixel is a foreground pixel.
            if (CanRenderSpritePixelAt(x, rasterLine) && IsGraphicsPixelForegroundForSpritePriority(x, rasterLine))
            {
                spriteBackgroundAcc |= m;
            }
        }

        _spriteSpriteCollisionLatch |= spriteSpriteAcc;
        _spriteBackgroundCollisionLatch |= spriteBackgroundAcc;

        // BACKFILL-VIDEO-001 / FR-VIC-005 / TEST-VIC-001: sprite collision IRQ wiring.
        // $D019 bit 1 latches a sprite-background collision; bit 2 latches
        // a sprite-sprite collision. Both bits set unconditionally when the
        // corresponding latch ($D01F / $D01E) sees a new event; the $D01A
        // enable mask only gates the IRQ output, not the latch. Same
        // write-1-to-clear semantics as bit 0 (raster) and bit 3 (LP).
        byte irqLatchBits = 0;
        if (spriteBackgroundAcc != 0)
        {
            irqLatchBits |= 0x02;
        }
        if (spriteSpriteAcc != 0)
        {
            irqLatchBits |= 0x04;
        }
        if (irqLatchBits != 0)
        {
            _registers[0x19] |= irqLatchBits;
            RefreshInterruptLine();
        }
    }

    private bool IsInvalidDisplayModeForeground(byte screenCode, byte colorCode, int screenIndex, int charRow, int charX)
    {
        bool bmm = (_registers[0x11] & 0x20) != 0;
        bool mcm = (_registers[0x16] & 0x10) != 0;

        if (bmm)
        {
            return mcm
                ? IsMulticolorBitmapForeground(screenIndex, charRow, charX)
                : IsStandardBitmapForeground(screenIndex, charRow, charX);
        }

        return mcm && IsMulticolorTextForeground(screenCode, colorCode, charRow, charX);
    }

    private bool IsStandardTextForeground(byte screenCode, int charRow, int charX, bool extendedColor)
    {
        byte glyph = extendedColor ? (byte)(screenCode & 0x3F) : screenCode;
        byte charLine = ReadVideoMemory((ushort)(CharacterBase + glyph * 8 + charRow));
        int bitPos = 7 - charX;
        return ((charLine >> bitPos) & 0x01) != 0;
    }

    private bool IsMulticolorTextForeground(byte screenCode, byte colorCode, int charRow, int charX)
    {
        byte charLine = ReadVideoMemory((ushort)(CharacterBase + screenCode * 8 + charRow));
        if ((colorCode & 0x08) == 0)
        {
            int bitPos = 7 - charX;
            return ((charLine >> bitPos) & 0x01) != 0;
        }

        return (ReadMulticolorPair(charLine, charX) & 0x02) != 0;
    }

    private bool IsStandardBitmapForeground(int screenIndex, int charRow, int charX)
    {
        byte bitmapByte = ReadVideoMemory((ushort)(BitmapPointerBase + screenIndex * 8 + charRow));
        int bitPos = 7 - charX;
        return ((bitmapByte >> bitPos) & 0x01) != 0;
    }

    private bool IsMulticolorBitmapForeground(int screenIndex, int charRow, int charX)
    {
        byte bitmapByte = ReadVideoMemory((ushort)(BitmapPointerBase + screenIndex * 8 + charRow));
        return (ReadMulticolorPair(bitmapByte, charX) & 0x02) != 0;
    }

    private static int ReadMulticolorPair(byte source, int charX)
    {
        int pairShift = 6 - ((charX / 2) * 2);
        return (source >> pairShift) & 0x03;
    }
}
