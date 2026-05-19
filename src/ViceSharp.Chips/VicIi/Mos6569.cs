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

    /// <summary>
    /// BACKFILL-VIDEO-001 / FR-VIC: count of bad lines that have fired
    /// during the current frame. Reset to zero when the raster wraps back
    /// to line 0. Increments at most once per raster line, on the first
    /// tick at which IsBadLine evaluates true for that line.
    /// </summary>
    public int BadLineCountThisFrame => _badLineCountThisFrame;
    
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

    public bool IsCpuCycleStolen => IsBadLine && RasterX >= 12 && RasterX < 55;

    public bool IsCpuCycleStealMandatory => IsBadLine && RasterX >= 13 && RasterX < 56;
    
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
        // VICE-style: 25-row mode boundaries
        if (CurrentRasterLine < 51 || CurrentRasterLine >= 251) return BorderSide.Extended;
        if (CurrentRasterLine < 55 || CurrentRasterLine >= 247) return BorderSide.Normal;
        return BorderSide.None;
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
    /// VICE-style: Y scroll value (bits 0-2 of $D011)
    /// </summary>
    public byte YScroll => (byte)(_registers[0x11] & 0x07);
    
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
        // BACKFILL-VIDEO-001 / FR-VIC: latch is independent of enable.
        if (RasterX == 58 && CurrentRasterLine == _rasterIrqLine)
        {
            _registers[0x19] |= 0x01;
            RefreshInterruptLine();
        }

        if (RasterX >= CyclesPerLine)
        {
            RasterX = 0;
            CurrentRasterLine++;

            if (CurrentRasterLine >= TotalLines)
            {
                CurrentRasterLine = 0;
                _refreshCounter = 0xFF;
                _allowBadLines = false;
                // BACKFILL-VIDEO-001: per-frame bad-line counter resets at the
                // frame boundary (raster wrap back to line 0).
                _badLineCountThisFrame = 0;
                _lastBadLineCounted = -1;
            }

            UpdateBadLineLatchForStartOfLine();

            // BACKFILL-VIDEO-001 / FR-VIC: count this scanline as a bad line
            // if it qualifies, exactly once per line.
            if (IsBadLine && _lastBadLineCounted != CurrentRasterLine)
            {
                _badLineCountThisFrame++;
                _lastBadLineCounted = CurrentRasterLine;
            }

            // BACKFILL-VIDEO-001: compute sprite collisions once per scanline.
            ProcessSpriteCollisionsForRasterLine(CurrentRasterLine);
        }

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
            return;
        }

        if (register == 0x11)
        {
            _rasterIrqLine = (ushort)((_rasterIrqLine & 0x0FF) | ((value & 0x80) << 1));
            _registers[register] = value;
            return;
        }

        if (register == 0x1A)
        {
            _registers[register] = (byte)(value & InterruptSourceMask);
            RefreshInterruptLine();
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

        if (yPos < upperBorder || yPos >= lowerBorder)
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
    // ordering does not affect collision detection (per FR-VIC: any two opaque
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

        // Visible screen vertical extent.
        int upperBorder = UpperBorderStart;
        int lowerBorder = LowerBorderStart;

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

        bool insideVisibleY = rasterLine >= upperBorder && rasterLine < lowerBorder;
        // The display window in VIC pixel space is [LeftBorderPixel, RightBorderEndPixel).
        int leftPixel = LeftBorderPixel;
        int rightPixel = RightBorderEndPixel;

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
            if (insideVisibleY && x >= leftPixel && x < rightPixel && IsBackgroundForegroundPixel(x, rasterLine))
            {
                spriteBackgroundAcc |= m;
            }
        }

        _spriteSpriteCollisionLatch |= spriteSpriteAcc;
        _spriteBackgroundCollisionLatch |= spriteBackgroundAcc;
    }

    // BACKFILL-VIDEO-001: Determine whether the background pixel at the given
    // VIC pixel coordinate is a foreground (non-transparent) pixel for the
    // purposes of sprite-background collision. Conservative: treats the bit
    // pattern from character/bitmap mode as foreground when the source bit
    // is set. Extended-bg / multicolor-text modes use the same simplification
    // (any set bit counts as foreground); this is a known approximation and
    // is documented in FR-VIC for the BACKFILL-VIDEO-001 slice.
    private bool IsBackgroundForegroundPixel(int xVicPixel, int rasterLine)
    {
        int leftBorderPixel = LeftBorderPixel;
        int upperBorder = UpperBorderStart;
        int lowerBorder = LowerBorderStart;

        if (rasterLine < upperBorder || rasterLine >= lowerBorder)
        {
            return false;
        }

        int screenX = xVicPixel - leftBorderPixel;
        if (screenX < 0)
        {
            return false;
        }
        int columns = Columns == ColumnMode.Wide40 ? 40 : 38;
        if (screenX >= columns * 8)
        {
            return false;
        }

        int visLine = rasterLine - upperBorder;
        int screenLine = visLine + YScroll;
        int screenRowCount = Math.Max((lowerBorder - upperBorder) / 8, 1);
        int row = Math.Max((screenLine / 8) % screenRowCount, 0);

        int col = screenX / 8;
        int charX = screenX % 8;
        int charOffset = row * columns + col;

        byte charCode = ReadVideoMemory((ushort)(ScreenMemoryBase + charOffset));

        switch (DisplayMode)
        {
            case VideoMode.StandardText:
            case VideoMode.MulticolorText:
            case VideoMode.ExtendedBackground:
            {
                byte charLine = ReadVideoMemory((ushort)(CharacterBase + charCode * 8 + (screenLine & 0x07)));
                int bitPos = 7 - charX;
                return ((charLine >> bitPos) & 0x01) != 0;
            }
            case VideoMode.Bitmap:
            {
                int bitmapOffset = (row * columns + col) * 8 + (screenLine & 0x07);
                byte bitmapByte = ReadVideoMemory((ushort)(BitmapPointerBase + bitmapOffset));
                int bitPos = 7 - charX;
                return ((bitmapByte >> bitPos) & 0x01) != 0;
            }
            default:
                return false;
        }
    }
}
