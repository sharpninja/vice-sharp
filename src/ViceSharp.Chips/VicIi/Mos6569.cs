using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// MOS 6569 VIC-II Video Interface Controller implementation.
/// </summary>
public partial class Mos6569 : IVideoChip, IAddressSpace, IInterruptSource
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

    public ushort CurrentRasterLine { get; private set; }
    
    // VICE-style: PAL timing (6567/6569)
    public const int PalCyclesPerLine = 63;
    public const int PalVisibleLines = 312;
    public const int PalTotalLines = 312;
    public const int NtscCyclesPerLine = 64;
    public const int NtscVisibleLines = 262;
    public const int NtscTotalLines = 263;
    
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
    /// Badlines occur on raster lines 30-49 when display is enabled (DEN bit in $11).
    /// </summary>
    public bool IsBadLine => CurrentRasterLine >= 30 && CurrentRasterLine <= 49 && IsDisplayEnabled;
    
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
    public bool IsDmaStealing => IsBadLine && !IsVerticalBlankArea && RasterX >= 40 && RasterX < CyclesPerLine;
    
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
    public ushort ScreenMemoryBase => (ushort)(((int)_registers[0x18] << 6) | (((int)_registers[0x11] & 0x0F) << 10));
    
    /// <summary>
    /// VICE-style: Character generator base address
    /// </summary>
    public ushort CharacterBase => (ushort)((_registers[0x18] & 0x0E) << 10);
    
    /// <summary>
    /// VICE-style: Get current cycle within line
    /// </summary>
    public byte CurrentCycle => (byte)(RasterX % CyclesPerLine);
    
    /// <summary>
    /// VICE-style: Is this a badline (raster line 30-49)
    /// </summary>
    public bool IsCurrentLineBad => CurrentRasterLine >= 30 && CurrentRasterLine <= 49;
    
    /// <summary>
    /// VICE-style: Frame rate based on TV system
    /// </summary>
    public double FrameRate => System switch
    {
        TvSystem.PAL => 50.0,
        TvSystem.NTSC => 60.0,
        TvSystem.PALN => 50.0,
        _ => 50.0
    };
    
    /// <summary>
    /// VICE-style: Cycles per line based on TV system
    /// </summary>
    public int CyclesPerLine => System switch
    {
        TvSystem.PAL or TvSystem.PALN => PalCyclesPerLine,
        TvSystem.NTSC => NtscCyclesPerLine,
        _ => PalCyclesPerLine
    };
    
    /// <summary>
    /// VICE-style: Visible lines based on TV system
    /// </summary>
    public int VisibleLines => System switch
    {
        TvSystem.PAL or TvSystem.PALN => PalVisibleLines,
        TvSystem.NTSC => NtscVisibleLines,
        _ => PalVisibleLines
    };
    
    /// <summary>
    /// VICE-style: Total lines based on TV system
    /// </summary>
    public int TotalLines => System switch
    {
        TvSystem.PAL or TvSystem.PALN => PalTotalLines,
        TvSystem.NTSC => NtscTotalLines,
        _ => PalTotalLines
    };
    
    public byte RasterX;
    public uint CycleCounter;
    
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
    public VideoMode DisplayMode => (VideoMode)((_registers[0x11] >> 4) & 0x07);
    
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
    public ushort ScreenMemoryAddress => (ushort)(((int)_registers[0x18] << 6) | (((int)_registers[0x11] & 0x0F) << 10));
    
    // VICE-style: Sprite state
    private readonly SpriteState[] _sprites = new SpriteState[8];
    
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
    /// Get sprite-sprite collision mask from register $1E
    /// </summary>
    public byte SpriteSpriteCollision => _registers[0x1E];
    
    /// <summary>
    /// Get sprite-background collision mask from register $1F
    /// </summary>
    public byte SpriteBackgroundCollision => _registers[0x1F];
    
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
    public ushort RasterIrqLine => (ushort)(((_registers[0x11] & 0x80) << 1) | _registers[0x12]);
    
    /// <summary>
    /// Set raster IRQ line
    /// </summary>
    public void SetRasterIrqLine(ushort line)
    {
        _registers[0x12] = (byte)line;
        _registers[0x11] = (byte)((_registers[0x11] & 0x7F) | ((line >> 1) & 0x80));
    }
    
    /// <summary>
    /// Clear sprite collision flags
    /// </summary>
    public void ClearCollisionFlags()
    {
        _registers[0x1E] = 0;
        _registers[0x1F] = 0;
    }
    
    /// <summary>
    /// Check if two sprites collide (VICE-style detection)
    /// </summary>
    public bool CheckSpriteCollision(int sprite1, int sprite2) => 
        (_registers[0x1E] & (1 << sprite1)) != 0 && (_registers[0x1E] & (1 << sprite2)) != 0;
    
    /// <summary>
    /// Check if sprite collides with background data
    /// </summary>
    public bool CheckSpriteBackgroundCollision(int spriteNum) => 
        (_registers[0x1F] & (1 << spriteNum)) != 0;
    
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

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

    public Mos6569(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
    }

    /// <inheritdoc />
    public void Tick()
    {
        CycleCounter++;
        RasterX++;

        // VICE-style raster interrupt (at cycle 58 of line)
        if (RasterX == 58)
        {
            ushort rasterIrq = (ushort)(((_registers[0x11] & 0x80) << 1) | _registers[0x12]);
            if (CurrentRasterLine == rasterIrq && (_registers[0x1A] & 0x01) != 0)
            {
                _registers[0x19] |= 0x01;
                if ((_registers[0x1A] & 0x80) != 0)
                    _irqLine.Assert(this);
            }
        }

        if (RasterX >= CyclesPerLine)
        {
            RasterX = 0;
            CurrentRasterLine++;

            if (CurrentRasterLine >= TotalLines)
            {
                CurrentRasterLine = 0;
            }
        }

        // Update raster register
        _registers[0x12] = (byte)CurrentRasterLine;
        if ((CurrentRasterLine & 0x100) != 0)
        {
            _registers[0x11] |= 0x80;
        }
        else
        {
            _registers[0x11] &= 0x7F;
        }
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
        RasterX = 0;
        CycleCounter = 0;
    }

    /// <inheritdoc />
    public byte Peek(ushort offset)
    {
        return Read(offset);
    }

    /// <inheritdoc />
    public byte Read(ushort offset)
    {
        if (offset >= Size) return 0xFF;

        if (offset == 0x19)
        {
            byte value = _registers[0x19];
            _registers[0x19] &= 0x7F;
            return value;
        }

        return _registers[offset];
    }

    /// <inheritdoc />
    public void Write(ushort offset, byte value)
    {
        if (offset >= Size) return;

        if (offset == 0x19)
        {
            _registers[offset] &= (byte)~value;
            return;
        }

        _registers[offset] = value;
        
        // VICE-style: Update sprite registers
        UpdateSpriteRegisters(offset, value);
    }
    
    private void UpdateSpriteRegisters(ushort offset, byte value)
    {
        // Sprite X position low (0x00-0x0F)
        if (offset < 0x10)
        {
            int sprite = offset / 2;
            if ((offset & 0x01) == 0)
                _sprites[sprite].X = (ushort)((_sprites[sprite].X & 0xFF00) | value);
            else
                _sprites[sprite].X = (ushort)((_sprites[sprite].X & 0x00FF) | ((value & 0x07) << 8));
        }
        // Sprite Y position (0x01, 0x03, ..., 0x0F)
        else if (offset >= 0x01 && offset < 0x10 && (offset & 0x01) != 0)
        {
            int sprite = (offset - 1) / 2;
            _sprites[sprite].Y = value;
        }
        // Sprite X MSB (0x10)
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
        // Sprite control (0x1C, 0x1D, 0x1E, 0x1F)
        else if (offset >= 0x1C && offset <= 0x1F)
        {
            int sprite = offset - 0x1C;
            ref SpriteState s = ref _sprites[sprite];
            s.IsExpandedX = (value & 0x08) != 0;
            s.IsExpandedY = (value & 0x04) != 0;
            s.IsMulticolor = (value & 0x02) != 0;
            s.IsPriority = (value & 0x01) == 0;
        }
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
    {
        return address >= BaseAddress && address < BaseAddress + Size;
    }
    
    /// <summary>
    /// VICE-style: Get pixel color at given raster position
    /// </summary>
    public byte GetPixelColor(byte x, byte y)
    {
        // Border takes priority
        if (GetHorizontalBorder() != BorderSide.None || GetVerticalBorder() != BorderSide.None)
            return BorderColor;
        
        // Outside visible area returns background
        if (y >= 200 || x >= 160)
            return BackgroundColor;
        
        // TODO: Implement actual character/bitmap rendering based on DisplayMode
        return BackgroundColor;
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
}
