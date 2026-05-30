namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
/// Use case: $D01E (sprite-sprite) and $D01F (sprite-background) collision
/// registers accumulate per-frame based on opaque pixel overlap between
/// enabled sprites or between sprites and non-transparent foreground
/// background pixels. Bits stay set until READ, which atomically clears
/// the latch. Peek must be non-destructive.
/// </summary>
public sealed class SpriteCollisionTests
{
    private const ushort SpriteX0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteX1 = 0xD002;
    private const ushort SpriteY1 = 0xD003;
    private const ushort SpriteEnable = 0xD015;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort MemoryPointers = 0xD018;
    private const ushort SpriteSpriteCollision = 0xD01E;
    private const ushort SpriteBackgroundCollision = 0xD01F;

    private const int CyclesPerLine = Mos6569.PalCyclesPerLine;

    /// <summary>
    /// Build a minimal VIC-II with a memory reader that returns:
    /// - 0xFF for sprite data block (giving fully opaque sprites)
    /// - The provided <paramref name="bgPattern"/> for the character bitmap fetch
    ///   so we can control whether the background has foreground pixels.
    /// Sprite data pointer (screen RAM $07F8+n) returns <paramref name="spriteDataBlock"/>.
    /// </summary>
    private static Mos6569 BuildVic(byte spriteDataBlock = 0x0D, byte bgPattern = 0x00)
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);

            // Sprite data pointers live at ScreenMemoryBase + $03F8..$03FF.
            // Default screen base is 0 (register $18 = 0) so pointers are at $03F8-$03FF.
            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                return spriteDataBlock;
            }

            // Sprite data block = pointer * 64. With pointer=0x0D, data sits at $0340-$037F.
            ushort spriteBase = (ushort)(spriteDataBlock * 64);
            if (masked >= spriteBase && masked < spriteBase + 64)
            {
                return 0xFF; // Fully opaque sprite row
            }

            // Character bitmap area: with $D018 = 0, character base = 0.
            // Return a configurable foreground bitmap pattern for background pixels.
            return bgPattern;
        };
        // Set DEN (display enable) for badline behaviour and standard text mode.
        vic.Write(ScreenControl1, 0x1B); // DEN=1, RSEL=1, YSCROLL=3
        vic.Write(ScreenControl2, 0x08); // CSEL=1 (40 cols)
        return vic;
    }

    /// <summary>
    /// Advance VIC ticks until the given raster line is reached at cycle 0,
    /// then step <paramref name="extra"/> more cycles. Used to ensure a full
    /// scanline of sprite raster passes through the collision detector.
    /// </summary>
    private static void AdvanceTo(Mos6569 vic, int line, int extra = CyclesPerLine)
    {
        int budget = vic.TotalLines * CyclesPerLine * 3;
        while (budget-- > 0 && !(vic.CurrentRasterLine == line && vic.RasterX == 0))
        {
            vic.Tick();
        }
        for (int i = 0; i < extra; i++)
        {
            vic.Tick();
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: Two enabled sprites placed at identical coordinates with
    /// fully opaque shape data must latch bits 0 and 1 in $D01E once their
    /// raster line is processed.
    /// Acceptance: Reading $D01E returns 0x03 after one full sprite-height
    /// raster pass with both sprites overlapping.
    /// </summary>
    [Fact]
    public void SpriteSprite_Overlap_LatchesBothBitsInD01E()
    {
        var vic = BuildVic();
        // Place both sprites at the same on-screen location (well inside the visible area).
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        // Enable sprites 0 and 1.
        vic.Write(SpriteEnable, 0x03);

        // Advance far enough to cover all 21 sprite raster lines plus a margin.
        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte collision = vic.Read(SpriteSpriteCollision);
        Assert.Equal(0x03, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: $D01E read-clear semantics: after reading the latch, a
    /// subsequent read must return 0 until new collisions accumulate.
    /// Acceptance: First read returns 0x03; immediate second read returns 0x00.
    /// </summary>
    [Fact]
    public void SpriteSprite_LatchClearsOnRead()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        Assert.Equal(0x03, vic.Read(SpriteSpriteCollision));
        Assert.Equal(0x00, vic.Read(SpriteSpriteCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: Non-overlapping enabled sprites must not produce false
    /// collisions; $D01E must remain 0.
    /// Acceptance: With sprite 0 and sprite 1 at well-separated X positions
    /// on different scanlines, $D01E reads 0x00 after a full frame's worth
    /// of raster processing.
    /// </summary>
    [Fact]
    public void SpriteSprite_Separated_NoFalseCollision()
    {
        var vic = BuildVic();
        // Sprite 0 at (50, 80); sprite 1 at (200, 150) - non-overlapping bboxes.
        vic.Write(SpriteX0, 50);
        vic.Write(SpriteY0, 80);
        vic.Write(SpriteX1, 200);
        vic.Write(SpriteY1, 150);
        vic.Write(SpriteEnable, 0x03);

        AdvanceTo(vic, line: 70, extra: 0);
        AdvanceTo(vic, line: 180, extra: 0);

        byte collision = vic.Read(SpriteSpriteCollision);
        Assert.Equal(0x00, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: A single enabled sprite drawn over a region whose
    /// underlying character bitmap has foreground pixels must latch bit 0
    /// of $D01F (sprite-background collision register).
    /// Acceptance: Reading $D01F returns 0x01 after the sprite's raster
    /// rows pass over the foreground-heavy character area.
    /// </summary>
    [Fact]
    public void SpriteBackground_OverForeground_LatchesBitInD01F()
    {
        // 0xFF background pattern -> every char pixel is foreground (opaque).
        var vic = BuildVic(bgPattern: 0xFF);
        // Place sprite well inside the visible character area (lines 51-250, x 24-343).
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        byte collision = vic.Read(SpriteBackgroundCollision);
        Assert.Equal(0x01, collision);
    }

    /// <summary>
    /// FR/TR: FR-VIC-002 / FR-VIC-003 / FR-VIC-005 / FR-VIC-008 /
    /// TEST-VIC-001 (BACKFILL-VIDEO-002 invalid ECM priority).
    /// Use case: x64sc maps invalid ECM display-mode colors to COL_NONE
    /// while preserving the hidden graphics foreground bit used by
    /// sprite-background collision detection.
    /// Acceptance: A sprite over an invalid ECM foreground pixel still
    /// latches $D01F even though the visible graphics output is black, while
    /// adjacent hires zero-bits or multicolor %01 pairs remain non-foreground.
    /// </summary>
    [Theory]
    [InlineData(0x58, 0x18, 0, 1)]
    [InlineData(0x58, 0x18, 1, 2)]
    [InlineData(0x78, 0x08, 2, 1)]
    [InlineData(0x78, 0x18, 3, 2)]
    public void SpriteBackground_InvalidEcmForeground_LatchesOnlyForHiddenPriorityBits(
        byte d011,
        byte d016,
        int sourceKind,
        int firstBackgroundOffset)
    {
        var foregroundVic = BuildInvalidEcmCollisionVic(d011, d016, sourceKind, spriteX: 96);
        AdvanceTo(foregroundVic, line: 90, extra: 0);
        AdvanceTo(foregroundVic, line: 130, extra: 0);
        Assert.Equal(0x01, foregroundVic.Read(SpriteBackgroundCollision));

        var backgroundVic = BuildInvalidEcmCollisionVic(d011, d016, sourceKind, spriteX: 96 + firstBackgroundOffset);
        AdvanceTo(backgroundVic, line: 90, extra: 0);
        AdvanceTo(backgroundVic, line: 130, extra: 0);
        Assert.Equal(0x00, backgroundVic.Read(SpriteBackgroundCollision));
    }

    /// <summary>
    /// FR/TR: FR-VIC-005 / TEST-VIC-001 (BACKFILL-VIDEO-001 sprite collision).
    /// Use case: $D01F read-clear semantics: after reading the latch, a
    /// subsequent read must return 0 until new collisions accumulate.
    /// Acceptance: First read returns 0x01; immediate second read returns 0x00.
    /// </summary>
    [Fact]
    public void SpriteBackground_LatchClearsOnRead()
    {
        var vic = BuildVic(bgPattern: 0xFF);
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, line: 90, extra: 0);
        AdvanceTo(vic, line: 130, extra: 0);

        Assert.Equal(0x01, vic.Read(SpriteBackgroundCollision));
        Assert.Equal(0x00, vic.Read(SpriteBackgroundCollision));
    }

    private static void ConfigureSprite(RamDevice ram, Mos6569 vic, int sprite, int x, byte y, byte pointer)
    {
        ram.Write((ushort)(vic.ScreenMemoryBase + 0x03F8 + sprite), pointer);
        ram.Write((ushort)(pointer * 64), 0x80);
        ram.Write((ushort)(pointer * 64 + 1), 0x00);
        ram.Write((ushort)(pointer * 64 + 2), 0x00);
        vic.Write((ushort)(0xD000 + sprite * 2), (byte)(x & 0xFF));
        byte xMsb = vic.Peek(0xD010);
        xMsb = x >= 0x100
            ? (byte)(xMsb | (1 << sprite))
            : (byte)(xMsb & ~(1 << sprite));
        vic.Write(0xD010, xMsb);
        vic.Write((ushort)(0xD001 + sprite * 2), y);
    }

    private static Mos6569 BuildInvalidEcmCollisionVic(byte d011, byte d016, int sourceKind, int spriteX)
    {
        var bus = new BasicBus();
        var memory = new byte[0x10000];
        var ram = new RamDevice(0x0000, 0xFFFF, memory);
        bus.RegisterDevice(ram);
        var vic = new Mos6569(bus, new InterruptLine(InterruptType.Irq));

        vic.Write(ScreenControl1, d011);
        vic.Write(ScreenControl2, d016);
        vic.Write(MemoryPointers, sourceKind <= 1 ? (byte)0x15 : (byte)0x18);
        ConfigureInvalidEcmForegroundSource(ram, vic, x: 96, rasterLine: 100, sourceKind);
        ConfigureSprite(ram, vic, sprite: 0, x: spriteX, y: 100, pointer: 0x20);
        vic.Write(SpriteEnable, 0x01);
        return vic;
    }

    private static void ConfigureInvalidEcmForegroundSource(RamDevice ram, Mos6569 vic, int x, int rasterLine, int sourceKind)
    {
        switch (sourceKind)
        {
            case 0:
                ConfigureCharacterByte(ram, vic, x, rasterLine, color: 0x07, charByte: 0x80);
                break;
            case 1:
                ConfigureCharacterByte(ram, vic, x, rasterLine, color: 0x0B, charByte: 0x90);
                break;
            case 2:
                ConfigureBitmapByte(ram, vic, x, rasterLine, bitmapByte: 0x80);
                break;
            case 3:
                ConfigureBitmapByte(ram, vic, x, rasterLine, bitmapByte: 0x90);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sourceKind));
        }
    }

    private static void ConfigureCharacterByte(RamDevice ram, Mos6569 vic, int x, int rasterLine, byte color, byte charByte)
    {
        int columns = vic.Columns == Mos6569.ColumnMode.Wide40 ? 40 : 38;
        int screenX = x - vic.LeftBorderPixel;
        int visLine = rasterLine - vic.UpperBorderStart;
        int screenLine = visLine + vic.YScroll;
        int screenRowCount = Math.Max((vic.LowerBorderStart - vic.UpperBorderStart) / 8, 1);
        int screenRow = Math.Max((screenLine / 8) % screenRowCount, 0);
        int col = screenX / 8;
        int charRow = screenLine & 7;
        int screenIndex = screenRow * columns + col;
        byte charCode = 1;

        ram.Write((ushort)(vic.ScreenMemoryBase + screenIndex), charCode);
        ram.Write((ushort)(0xD800 + screenIndex), color);
        ram.Write((ushort)(vic.CharacterBase + charCode * 8 + charRow), charByte);
    }

    private static void ConfigureBitmapByte(RamDevice ram, Mos6569 vic, int x, int rasterLine, byte bitmapByte)
    {
        int columns = vic.Columns == Mos6569.ColumnMode.Wide40 ? 40 : 38;
        int screenX = x - vic.LeftBorderPixel;
        int visLine = rasterLine - vic.UpperBorderStart;
        int screenLine = visLine + vic.YScroll;
        int screenRowCount = Math.Max((vic.LowerBorderStart - vic.UpperBorderStart) / 8, 1);
        int screenRow = Math.Max((screenLine / 8) % screenRowCount, 0);
        int col = screenX / 8;
        int charRow = screenLine & 7;
        int screenIndex = screenRow * columns + col;

        ram.Write((ushort)(vic.BitmapPointerBase + screenIndex * 8 + charRow), bitmapByte);
    }

    // =====================================================================
    // BDP-gated addition for BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 (narrow slice)
    // Tests written FIRST per Byrd Development Process (requirements-driven,
    // full acceptance criteria in XMLDOC, mocks/stubs via controlled
    // VideoMemoryReader + register state, validated green before any real
    // logic changes in Mos6569).
    // =====================================================================

    /// <summary>
    /// BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 / FR-VIC-002 / FR-VIC-003 /
    /// FR-VIC-005 / FR-VIC-008 / TEST-VIC-001:
    /// Invalid ECM display mode combinations (ECM bit set with BMM or MCM)
    /// must preserve the hidden graphics priority bit (VICE "px &amp; 0x2")
    /// for sprite priority decisions and sprite-background collision
    /// detection, even though the color path forces COL_NONE (rendered pixel 0).
    /// 
    /// VICE sources (from explore subagent gaps report 019e6acc-29b8-77f1-a9cc-56499af366f9):
    /// - viciisc/vicii-draw-cycle.c:146-188 (gbuf_pixel_reg / px construction
    ///   for the 8 vmode combos, including the MCM/BMM/ECM kludge paths)
    /// - viciisc/vicii-draw-cycle.c:194-196: "pixel_pri = (px &amp; 0x2);"
    ///   and "cc = colors[vmode | px];"
    /// - viciisc/vicii-draw-cycle.c:224: "pri_buffer[i] = pixel_pri;"
    /// - viciisc/vicii-draw-cycle.c:402: "uint8_t pixel_pri = pri_buffer[i];"
    ///   then "if (!(pixel_pri &amp;&amp; spri))" for render/collision choice
    /// - viciisc/vicii-draw-cycle.c:134-141 (colors[] table: COL_D02X_EXT for
    ///   valid ECM, COL_NONE for the three ECM=1 invalid rows)
    /// 
    /// Use case: With VideoMemoryReader stub + forced D011/D016 producing
    /// DisplayModeSelection == Invalid with ECM contributing, and character/
    /// bitmap data bytes producing 2-bit "px" values with bit 1 set vs clear
    /// (at specific charX), IsGraphicsPixelForegroundForSpritePriority must
    /// return exactly the (px &amp; 0x2) != 0 result for all three invalid ECM combos
    /// (mocks/stubs first per BDP).
    /// Acceptance: IsGraphicsPixelForegroundForSpritePriority returns true when px bit 1
    /// is set and false otherwise for all invalid ECM combos, driving correct
    /// TryGetSpritePixel priority skip and ProcessSpriteCollisionsForRasterLine
    /// sb-collision latch; visible render path yields color 0 for these pixels.
    /// </summary>
    [Theory]
    [InlineData(0x58, 0x18, 0x80, 0, true)]   // ECM1 BMM0 MCM1 (invalid), char high bit -> px=3 or equiv, pri=1
    [InlineData(0x58, 0x18, 0x40, 0, false)]  // same mode, data bit producing px bit1=0
    [InlineData(0x78, 0x08, 0x80, 2, true)]   // ECM1 BMM1 MCM0 (invalid), bitmap high -> pri
    [InlineData(0x78, 0x08, 0x20, 2, false)]
    [InlineData(0x78, 0x18, 0xC0, 3, true)]   // ECM1 BMM1 MCM1 (invalid), mc bitmap pair high bit
    [InlineData(0x78, 0x18, 0x30, 3, false)]
    public void IsGraphicsPixelForegroundForSpritePriority_InvalidEcm_PreservesVicePxBit1(
        byte d011, byte d016, byte dataByte, int sourceKind, bool expectedPriBit)
    {
        // Stub memory reader (mocks the gbuf fetch for the target column/char)
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Write(ScreenControl1, d011);
        vic.Write(ScreenControl2, d016);

        // Compute a screen location inside a visible char cell (col 9, charX=0 for simplicity)
        const int TestRaster = 100;
        const int TestVicX = 96; // inside left border + some chars
        int columns = vic.Columns == Mos6569.ColumnMode.Wide40 ? 40 : 38;
        int screenX = TestVicX - vic.LeftBorderPixel;
        int visLine = TestRaster - vic.UpperBorderStart;
        int screenLine = visLine + vic.YScroll;
        int screenRowCount = Math.Max((vic.LowerBorderStart - vic.UpperBorderStart) / 8, 1);
        int screenRow = Math.Max((screenLine / 8) % screenRowCount, 0);
        int col = screenX / 8;
        int charX = screenX % 8; // 0
        int charRow = screenLine & 7;
        int screenIndex = screenRow * columns + col;

        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);
            if (sourceKind <= 1)
            {
                // Character screen + color + glyph data (for BMM=0 cases)
                if (masked == (ushort)(vic.ScreenMemoryBase + screenIndex)) return 0x01; // charCode
                if (masked == (ushort)(0xD800 + screenIndex)) return (byte)(sourceKind == 0 ? 0x07 : 0x0B);
                ushort glyphAddr = (ushort)(vic.CharacterBase + 0x01 * 8 + charRow);
                if (masked == glyphAddr) return dataByte;
            }
            else
            {
                // Bitmap data (for BMM=1 cases)
                ushort bmpAddr = (ushort)(vic.BitmapPointerBase + screenIndex * 8 + charRow);
                if (masked == bmpAddr) return dataByte;
                // Also satisfy screen read in IsGraphics path
                if (masked == (ushort)(vic.ScreenMemoryBase + screenIndex)) return 0x00;
                if (masked == (ushort)(0xD800 + screenIndex)) return 0x00;
            }
            return 0x00;
        };

        // Exercise the priority/collision query path (this is the contract under test)
        bool actual = vic.IsGraphicsPixelForegroundForSpritePriority(TestVicX, TestRaster);

        // Acceptance: must match VICE (px & 0x2) semantics for the invalid ECM case
        Assert.Equal(expectedPriBit, actual);
    }
}
