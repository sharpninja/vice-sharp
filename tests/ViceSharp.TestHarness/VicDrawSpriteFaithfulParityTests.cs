namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8 / TR-PARITY-GATE-001: FAITHFUL (green-now regression
/// lock) parity tests for the VIC-II draw and sprite FRs in
/// artifacts/vice-parity-requirements/requirements.yaml:
/// FR-VIC-DRAW-GFX (AC-05, AC-07..AC-13), FR-VIC-DISPLAYMODE (AC-01, AC-02,
/// AC-07, AC-08), FR-VIC-SPRITE-RENDER (AC-05..AC-08), FR-VIC-SPRITE-COLLISION
/// (AC-02..AC-05, AC-10, AC-11), FR-VIC-SPRITE-PRIORITY (AC-01..AC-04, AC-06).
/// FR-VIC-DRAW-COLOR, FR-VIC-XSCROLL, and FR-VIC-SPRITE-DMA carry no FAITHFUL
/// acceptance criteria (every AC on them is a DIVERGENT remediation target), so
/// they contribute no methods here.
///
/// These locks assert the durable observable level of each statement (register
/// decode, collision-latch semantics, priority winner rules, mode colour
/// routing, per-line pixel output) rather than geometric implementation details
/// of the current per-line renderer, so they are chosen to survive the V3-V7
/// per-cycle PixelSequencer rewrite. Deliberately NOT tagged ParityLegacy.
/// VICE citations refer to native/vice/vice/src/viciisc/.
/// </summary>
public sealed class VicDrawSpriteFaithfulParityTests
{
    private const byte BorderBlue = 0x06;

    // =====================================================================
    // Shared machine wiring (pattern copied from VideoRendererTests /
    // SpriteCollisionTests).
    // =====================================================================

    private static (BasicBus Bus, RamDevice Ram, InterruptLine Irq) CreateTestMachine()
    {
        var bus = new BasicBus();
        var memory = new byte[0x10000];
        var ram = new RamDevice(0x0000, 0xFFFF, memory);
        var irq = new InterruptLine(InterruptType.Irq);
        bus.RegisterDevice(ram);
        return (bus, ram, irq);
    }

    private static (Mos6569 Vic, RamDevice Ram, VideoRenderer Renderer) CreateRenderMachine(
        byte d011, byte d016, byte d018)
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);
        vic.Write(0xD011, d011);
        vic.Write(0xD016, d016);
        vic.Write(0xD018, d018);
        return (vic, ram, renderer);
    }

    private static uint ReadPixel(byte[] frameBuffer, int x, int y)
    {
        var offset = ((y * VideoRenderer.ScreenWidth) + x) * 4;
        return BitConverter.ToUInt32(frameBuffer, offset);
    }

    private static uint ToBgra(int paletteIndex)
    {
        var color = VicPalette.Colors[paletteIndex & 0x0F];
        return 0xFF000000u | color.B | ((uint)color.G << 8) | ((uint)color.R << 16);
    }

    private static int FirstVisibleBadLine(Mos6569 vic)
        => vic.UpperBorderStart + ((vic.YScroll - (vic.UpperBorderStart & 0x07) + 8) & 0x07);

    private static void AdvanceToLineStart(Mos6569 vic, int line)
    {
        int budget = vic.TotalLines * Mos6569.PalCyclesPerLine * 3;
        while (budget-- > 0)
        {
            if (vic.CurrentRasterLine == line && vic.RasterX == 0)
            {
                return;
            }

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line {line}, cycle 0.");
    }

    private static void ConfigureCharacterCell(
        RamDevice ram,
        Mos6569 vic,
        int x,
        int rasterLine,
        byte charCode,
        byte color,
        byte charByte)
    {
        int columns = vic.Columns == Mos6569.ColumnMode.Wide40 ? 40 : 38;
        int screenX = x - vic.LeftBorderPixel;
        int screenLine = rasterLine - FirstVisibleBadLine(vic);
        int screenRow = screenLine / 8;
        int col = screenX / 8;
        int charRow = screenLine & 7;
        int screenIndex = screenRow * columns + col;

        ram.Write((ushort)(vic.ScreenMemoryBase + screenIndex), charCode);
        ram.Write((ushort)(0xD800 + screenIndex), color);
        ram.Write((ushort)(vic.CharacterBase + charCode * 8 + charRow), charByte);
    }

    private static void ConfigureBitmapCell(
        RamDevice ram,
        Mos6569 vic,
        int x,
        int rasterLine,
        byte screenByte,
        byte colorRam,
        byte bitmapByte)
    {
        int columns = vic.Columns == Mos6569.ColumnMode.Wide40 ? 40 : 38;
        int screenX = x - vic.LeftBorderPixel;
        int screenLine = rasterLine - FirstVisibleBadLine(vic);
        int screenRow = screenLine / 8;
        int col = screenX / 8;
        int charRow = screenLine & 7;
        int screenIndex = screenRow * columns + col;

        ram.Write((ushort)(vic.ScreenMemoryBase + screenIndex), screenByte);
        ram.Write((ushort)(0xD800 + screenIndex), colorRam);
        ram.Write((ushort)(vic.BitmapPointerBase + screenIndex * 8 + charRow), bitmapByte);
    }

    private static void ConfigureSprite(
        RamDevice ram,
        Mos6569 vic,
        int sprite,
        int x,
        byte y,
        byte pointer,
        byte color,
        byte row0 = 0x80,
        byte row1 = 0x00,
        byte row2 = 0x00)
    {
        ram.Write((ushort)(vic.ScreenMemoryBase + 0x03F8 + sprite), pointer);
        ram.Write((ushort)(pointer * 64), row0);
        ram.Write((ushort)(pointer * 64 + 1), row1);
        ram.Write((ushort)(pointer * 64 + 2), row2);
        vic.Write((ushort)(0xD000 + sprite * 2), (byte)(x & 0xFF));
        byte xMsb = vic.Peek(0xD010);
        xMsb = x >= 0x100
            ? (byte)(xMsb | (1 << sprite))
            : (byte)(xMsb & ~(1 << sprite));
        vic.Write(0xD010, xMsb);
        vic.Write((ushort)(0xD001 + sprite * 2), y);
        vic.Write((ushort)(0xD027 + sprite), color);
    }

    /// <summary>
    /// Stub-reader VIC used by the border collision locks, mirroring
    /// SpriteCollisionTests.BuildVic: the sprite data block reads fully opaque
    /// (0xFF) and every other video fetch returns <paramref name="bgPattern"/>,
    /// so the whole character window is foreground when bgPattern is 0xFF.
    /// </summary>
    private static Mos6569 BuildStubVic(byte bgPattern)
    {
        const byte SpriteDataBlock = 0x0D;
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);
            if (masked >= 0x03F8 && masked <= 0x03FF)
            {
                return SpriteDataBlock;
            }

            ushort spriteBase = SpriteDataBlock * 64;
            if (masked >= spriteBase && masked < spriteBase + 64)
            {
                return 0xFF;
            }

            return bgPattern;
        };
        vic.Phi1MemoryReader = _ => bgPattern;
        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        return vic;
    }

    // =====================================================================
    // FR-VIC-DRAW-GFX (FAITHFUL: AC-05, AC-07, AC-08, AC-09, AC-10, AC-11,
    // AC-12, AC-13)
    // =====================================================================

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-05 / TEST-VIC-DRAW-GFX-05: mc-vs-hires colour
    /// selection (result faithful; per-pixel flop timing divergent).
    /// VICE viciisc/vicii-draw-cycle.c:164-173: with MCM active, mc pixels are
    /// used when BMM is set or cbuf bit 3 is set; otherwise the cell renders
    /// hires pixels. Managed: VideoRenderer.cs:308-323 (colour-RAM bit 3
    /// branch) and RenderMulticolorBitmapPixel (always pairs under BMM+MCM).
    /// Use case: A multicolour-text raster mixes true multicolour cells
    /// (colour RAM bit 3 set) with hires cells (bit 3 clear) on one line; a
    /// multicolour bitmap ignores colour RAM bit 3 entirely.
    /// Acceptance: The same glyph byte 0xAA renders per-bit (fg colour RAM
    /// low 3 bits, bg $D021) when colour RAM bit 3 is clear and per-pair
    /// ($D023 for pair 10) when bit 3 is set; a multicolour-bitmap cell with
    /// colour RAM bit 3 clear still renders pairs, not hires bits.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-05", ParityTag.Faithful)]
    public void McTextColourRamBit3SelectsMcVersusHiresAndMcBitmapAlwaysUsesPairs()
    {
        // Multicolour text: cbuf bit 3 selects hires vs mc for the same glyph.
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x1B, d016: 0x18, d018: 0x15);
        vic.Write(0xD021, BorderBlue);
        vic.Write(0xD023, 0x04);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        // Column 0: colour RAM 0x05 (bit 3 clear) -> hires rendering of 0xAA.
        ConfigureCharacterCell(ram, vic, x, rasterLine, charCode: 0x01, color: 0x05, charByte: 0xAA);
        // Column 1: colour RAM 0x0D (bit 3 set) -> mc pairs of 0xAA (all pair 10).
        ConfigureCharacterCell(ram, vic, x + 8, rasterLine, charCode: 0x01, color: 0x0D, charByte: 0xAA);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        // Hires cell: 0xAA = 10101010 renders bit 7 as fg (colour RAM & 7 = 5)
        // and bit 6 as bg ($D021).
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x + 1, y));
        // Mc cell: 0xAA = pairs 10,10,10,10 -> every pixel $D023, including the
        // odd pixel that the hires cell rendered as background.
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x + 8, y));
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x + 9, y));

        // Multicolour bitmap: BMM forces mc pixels even with colour RAM bit 3
        // clear (vicii-draw-cycle.c:165 "(vmode11_pipe & 0x08) || ...").
        var (bmVic, bmRam, bmRenderer) = CreateRenderMachine(d011: 0x1B | 0x20, d016: 0x18, d018: 0x18);
        bmVic.Write(0xD021, BorderBlue);
        int bmLine = FirstVisibleBadLine(bmVic);
        // 0x1B = pairs 00,01,10,11; colour RAM 0x03 has bit 3 clear.
        ConfigureBitmapCell(bmRam, bmVic, x, bmLine, screenByte: 0xA5, colorRam: 0x03, bitmapByte: 0x1B);

        bmRenderer.RenderFullFrame();

        int bmY = VideoRenderer.RasterLineToFrameY(bmLine);
        // Pair 01 -> vbuf high nibble (0x0A), proving pair decoding despite
        // colour RAM bit 3 being clear (a hires read of bit 5 would be 0 -> bg).
        Assert.Equal(ToBgra(0x0A), ReadPixel(bmRenderer.FrameBuffer, x + 2, bmY));
        Assert.Equal(ToBgra(0x03), ReadPixel(bmRenderer.FrameBuffer, x + 6, bmY));
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-07 / TEST-VIC-DRAW-GFX-07: standard text renders
    /// bg=$D021 and fg=CBUF.
    /// VICE viciisc/vicii-draw-cycle.c:134 (colors row ECM=0 BMM=0 MCM=0:
    /// COL_D021, COL_D021, COL_CBUF, COL_CBUF). Managed: VideoRenderer.cs
    /// RenderStandardTextPixel (296-303).
    /// Use case: A standard-text cell draws its glyph one-bits in the Color
    /// RAM colour and zero-bits in the $D021 background colour.
    /// Acceptance: Glyph byte 0x80 renders pixel 0 in the full 4-bit Color
    /// RAM value (0x0E) and pixel 1 in the $D021 colour.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-07", ParityTag.Faithful)]
    public void StandardTextRendersBackgroundFromD021AndForegroundFromColourRam()
    {
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x1B, d016: 0x08, d018: 0x15);
        vic.Write(0xD021, BorderBlue);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        // Colour RAM 0x0E locks the full 4-bit CBUF path (a 3-bit read would give 6).
        ConfigureCharacterCell(ram, vic, x, rasterLine, charCode: 0x01, color: 0x0E, charByte: 0x80);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        Assert.Equal(ToBgra(0x0E), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x + 1, y));
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-08 / TEST-VIC-DRAW-GFX-08: mc text renders pairs
    /// from $D021/$D022/$D023/CBUF_MC.
    /// VICE viciisc/vicii-draw-cycle.c:135 (colors row ECM=0 BMM=0 MCM=1:
    /// COL_D021, COL_D022, COL_D023, COL_CBUF_MC where CBUF_MC = cbuf &amp; 0x07).
    /// Managed: VideoRenderer.cs RenderMulticolorTextPixel (305-324).
    /// Use case: A multicolour-text cell with colour RAM bit 3 set routes its
    /// four two-bit pair values through the VICE colour sources.
    /// Acceptance: Glyph 0x1B (pairs 00,01,10,11) renders $D021, $D022, $D023,
    /// and colour RAM &amp; 0x07 at the pair anchors; colour RAM 0x0F must
    /// render as palette 7 (low three bits only), not 15.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-08", ParityTag.Faithful)]
    public void McTextRendersPairsFromD021D022D023AndColourRamLowThreeBits()
    {
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x1B, d016: 0x18, d018: 0x15);
        vic.Write(0xD021, BorderBlue);
        vic.Write(0xD022, 0x02);
        vic.Write(0xD023, 0x04);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        // Colour RAM 0x0F: bit 3 set selects mc pairs; CBUF_MC low bits = 7.
        ConfigureCharacterCell(ram, vic, x, rasterLine, charCode: 0x01, color: 0x0F, charByte: 0x1B);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, x + 2, y));
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x + 4, y));
        Assert.Equal(ToBgra(0x07), ReadPixel(renderer.FrameBuffer, x + 6, y));
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-09 / TEST-VIC-DRAW-GFX-09: standard bitmap renders
    /// fg=vbuf&gt;&gt;4 and bg=vbuf&amp;0x0f.
    /// VICE viciisc/vicii-draw-cycle.c:136 (colors row ECM=0 BMM=1 MCM=0:
    /// COL_VBUF_L, COL_VBUF_L, COL_VBUF_H, COL_VBUF_H). Managed:
    /// VideoRenderer.cs RenderStandardBitmapPixel (346-354).
    /// Use case: A hires bitmap cell colours one-bits from the video-matrix
    /// byte's high nibble and zero-bits from its low nibble.
    /// Acceptance: Video-matrix byte 0xA5 with bitmap byte 0x80 renders pixel
    /// 0 as palette 0x0A and pixel 1 as palette 0x05.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-09", ParityTag.Faithful)]
    public void StandardBitmapRendersForegroundFromVbufHighNibbleAndBackgroundFromLowNibble()
    {
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x3B, d016: 0x08, d018: 0x18);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        ConfigureBitmapCell(ram, vic, x, rasterLine, screenByte: 0xA5, colorRam: 0x00, bitmapByte: 0x80);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        Assert.Equal(ToBgra(0x0A), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 1, y));
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-10 / TEST-VIC-DRAW-GFX-10: mc bitmap renders pairs
    /// from $D021/vbuf&gt;&gt;4/vbuf&amp;0x0f/cbuf.
    /// VICE viciisc/vicii-draw-cycle.c:137 (colors row ECM=0 BMM=1 MCM=1:
    /// COL_D021, COL_VBUF_H, COL_VBUF_L, COL_CBUF). Managed: VideoRenderer.cs
    /// RenderMulticolorBitmapPixel (356-367).
    /// Use case: A multicolour bitmap cell routes its four pair values through
    /// the background register, both video-matrix nibbles, and Color RAM.
    /// Acceptance: Bitmap byte 0x1B (pairs 00,01,10,11) with matrix byte 0xA5
    /// and Color RAM 0x0E renders $D021, 0x0A, 0x05, and 0x0E (full 4-bit
    /// cbuf) at the pair anchors.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-10", ParityTag.Faithful)]
    public void McBitmapRendersPairsFromD021VbufHighVbufLowAndColourRam()
    {
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x3B, d016: 0x18, d018: 0x18);
        vic.Write(0xD021, BorderBlue);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        ConfigureBitmapCell(ram, vic, x, rasterLine, screenByte: 0xA5, colorRam: 0x0E, bitmapByte: 0x1B);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x0A), ReadPixel(renderer.FrameBuffer, x + 2, y));
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 4, y));
        Assert.Equal(ToBgra(0x0E), ReadPixel(renderer.FrameBuffer, x + 6, y));
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-11 / TEST-VIC-DRAW-GFX-11: extended colour selects
    /// the background register from screen-code bits 6-7 and masks the glyph
    /// index to 0x3F.
    /// VICE viciisc/vicii-draw-cycle.c:138,216-218 (COL_D02X_EXT resolves to
    /// COL_D021 + (vbuf &gt;&gt; 6)). Managed: VideoRenderer.cs
    /// RenderExtendedColorPixel (326-344).
    /// Use case: Four ECM cells whose screen codes differ only in bits 6-7
    /// select $D021, $D022, $D023, and $D024 for their background pixels while
    /// all rendering the glyph of character 1.
    /// Acceptance: Screen codes 0x01, 0x41, 0x81, 0xC1 with glyph 0x40 render
    /// the zero bit from the indexed background register and the one bit from
    /// Color RAM; the one bit renders at all four codes, proving the 0x3F
    /// glyph mask (only character 1's glyph data exists in RAM).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-11", ParityTag.Faithful)]
    public void ExtendedColorSelectsBackgroundRegisterFromScreenCodeUpperBitsAndMasksGlyph()
    {
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x5B, d016: 0x08, d018: 0x15);
        vic.Write(0xD021, BorderBlue);
        vic.Write(0xD022, 0x02);
        vic.Write(0xD023, 0x04);
        vic.Write(0xD024, 0x05);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        byte[] screenCodes = { 0x01, 0x41, 0x81, 0xC1 };
        byte[] cellColors = { 0x03, 0x07, 0x09, 0x0C };
        for (int cell = 0; cell < 4; cell++)
        {
            ConfigureCharacterCell(
                ram,
                vic,
                x + cell * 8,
                rasterLine,
                charCode: screenCodes[cell],
                color: cellColors[cell],
                charByte: 0x40);
        }

        // ConfigureCharacterCell wrote the glyph at CharacterBase + code * 8; for
        // the codes with upper bits set that lands outside character 1's slot, so
        // rewrite the single glyph the 0x3F mask must select for every cell.
        ram.Write((ushort)(vic.CharacterBase + 0x01 * 8), 0x40);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        byte[] backgroundPalette = { BorderBlue, 0x02, 0x04, 0x05 };
        for (int cell = 0; cell < 4; cell++)
        {
            // Bit 7 of glyph 0x40 is 0 -> background register chosen by code bits 6-7.
            Assert.Equal(ToBgra(backgroundPalette[cell]), ReadPixel(renderer.FrameBuffer, x + cell * 8, y));
            // Bit 6 of glyph 0x40 is 1 -> Color RAM foreground (glyph found via the 0x3F mask).
            Assert.Equal(ToBgra(cellColors[cell]), ReadPixel(renderer.FrameBuffer, x + cell * 8 + 1, y));
        }
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-12 / TEST-VIC-DRAW-GFX-12: invalid modes render
    /// black (cc=0) while preserving pri_buffer = px &amp; 0x2.
    /// VICE viciisc/vicii-draw-cycle.c:139-141 (COL_NONE rows), 196
    /// (pixel_pri = px &amp; 0x2), 224 (pri_buffer). Managed: VideoRenderer.cs
    /// Invalid arm (280) and Mos6569.cs IsInvalidDisplayModeForeground
    /// (2314-2344).
    /// Use case: An ECM+BMM raster shows a black screen, but sprite priority
    /// and sprite-background collisions still see the hidden graphics
    /// foreground bit of the underlying data.
    /// Acceptance: With bitmap byte 0x80 in ECM+BMM, both the set-bit and
    /// clear-bit pixels render palette 0, while
    /// IsGraphicsPixelForegroundForSpritePriority reports true at the set bit
    /// and false at the clear bit.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-12", ParityTag.Faithful)]
    public void InvalidModesRenderBlackWhilePreservingGraphicsPriorityBit()
    {
        var (vic, ram, renderer) = CreateRenderMachine(d011: 0x78, d016: 0x08, d018: 0x18);
        vic.Write(0xD021, BorderBlue);

        int rasterLine = FirstVisibleBadLine(vic);
        int x = vic.LeftBorderPixel;
        ConfigureBitmapCell(ram, vic, x, rasterLine, screenByte: 0xA5, colorRam: 0x03, bitmapByte: 0x80);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(rasterLine);
        // Visible output is COL_NONE (palette 0) for every px value.
        Assert.Equal(ToBgra(0x00), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x00), ReadPixel(renderer.FrameBuffer, x + 1, y));
        // The hidden px & 0x2 priority bit survives the COL_NONE colour path.
        Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(x, rasterLine));
        Assert.False(vic.IsGraphicsPixelForegroundForSpritePriority(x + 1, rasterLine));
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-13 / TEST-VIC-DRAW-GFX-13: colour code =
    /// colors[(vmode11|vmode16)|px] (steady-state decode).
    /// VICE viciisc/vicii-draw-cycle.c:195-197 (vmode = vmode11_pipe |
    /// vmode16_pipe; cc = colors[vmode | px]). Managed: Mos6569.cs
    /// DisplayModeSelection (387-401) feeding the VideoRenderer mode switch.
    /// Use case: With one identical memory image, changing only the mode
    /// selector registers must swap the colors[] row used for the same pixel
    /// data, changing which colour source each px value resolves to.
    /// Acceptance: The same seeded RAM renders (charX0, charX2) as
    /// (CBUF 3, $D021) in standard text, (CBUF 3, $D023) in extended colour,
    /// (0x0A, 0x05) in standard bitmap, and (0x05, $D021) in multicolour
    /// bitmap, exactly per the four colors[] rows.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-13", ParityTag.Faithful)]
    public void ColourCodeDecodeRoutesThroughModeSelectorRowForSamePixelData()
    {
        // One memory image, four register-only mode selections.
        static (Mos6569 Vic, VideoRenderer Renderer) BuildMode(byte d011, byte d016, byte d018)
        {
            var (vic, ram, renderer) = CreateRenderMachine(d011, d016, d018);
            vic.Write(0xD021, BorderBlue);
            vic.Write(0xD023, 0x04);
            // Screen matrix byte (vbuf) at row 0, column 0 of base $0400.
            ram.Write(0x0400, 0xA5);
            // Color RAM (cbuf).
            ram.Write(0xD800, 0x03);
            // Glyph for text mode (char 0xA5) and ECM (char 0xA5 & 0x3F = 0x25), row 0.
            ram.Write((ushort)(0x1000 + 0xA5 * 8), 0x80);
            ram.Write((ushort)(0x1000 + 0x25 * 8), 0x80);
            // Bitmap byte for cell 0, row 0 at base $2000.
            ram.Write(0x2000, 0x80);
            return (vic, renderer);
        }

        static (uint P0, uint P2) RenderAndSample((Mos6569 Vic, VideoRenderer Renderer) machine)
        {
            machine.Renderer.RenderFullFrame();
            int rasterLine = FirstVisibleBadLine(machine.Vic);
            int y = VideoRenderer.RasterLineToFrameY(rasterLine);
            int x = machine.Vic.LeftBorderPixel;
            return (ReadPixel(machine.Renderer.FrameBuffer, x, y),
                    ReadPixel(machine.Renderer.FrameBuffer, x + 2, y));
        }

        // Row ECM=0 BMM=0 MCM=0: {D021, D021, CBUF, CBUF}.
        var text = RenderAndSample(BuildMode(0x1B, 0x08, 0x15));
        Assert.Equal(ToBgra(0x03), text.P0);
        Assert.Equal(ToBgra(BorderBlue), text.P2);

        // Row ECM=1 BMM=0 MCM=0: {D02X_EXT, D02X_EXT, CBUF, CBUF}; vbuf>>6 = 2 -> $D023.
        var ecm = RenderAndSample(BuildMode(0x5B, 0x08, 0x15));
        Assert.Equal(ToBgra(0x03), ecm.P0);
        Assert.Equal(ToBgra(0x04), ecm.P2);

        // Row ECM=0 BMM=1 MCM=0: {VBUF_L, VBUF_L, VBUF_H, VBUF_H}.
        var bitmap = RenderAndSample(BuildMode(0x3B, 0x08, 0x18));
        Assert.Equal(ToBgra(0x0A), bitmap.P0);
        Assert.Equal(ToBgra(0x05), bitmap.P2);

        // Row ECM=0 BMM=1 MCM=1: {D021, VBUF_H, VBUF_L, CBUF}; 0x80 = pairs 10,00.
        var mcBitmap = RenderAndSample(BuildMode(0x3B, 0x18, 0x18));
        Assert.Equal(ToBgra(0x05), mcBitmap.P0);
        Assert.Equal(ToBgra(BorderBlue), mcBitmap.P2);
    }

    // =====================================================================
    // FR-VIC-DISPLAYMODE (FAITHFUL: AC-01, AC-02, AC-07, AC-08)
    // =====================================================================

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-01 / TEST-VIC-DISPLAYMODE-01: vmode11_pipe =
    /// (regs[$11] &amp; 0x60) &gt;&gt; 2 (decode value; timing divergent).
    /// VICE viciisc/vicii-draw-cycle.c:246. Managed: Mos6569.cs 391-392 (ECM =
    /// $D011 bit 6, BMM = $D011 bit 5 in DisplayModeSelection).
    /// Use case: Only bits 5 (BMM) and 6 (ECM) of $D011 feed the display-mode
    /// decode; RSEL, DEN, YSCROLL, and the raster MSB never leak into it.
    /// Acceptance: With MCM held 0, the four $D011 bit-5/6 combinations decode
    /// to StandardText, StandardBitmap, ExtendedColor, and Invalid, with and
    /// without all other $D011 bits set.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DISPLAYMODE-01", ParityTag.Faithful)]
    public void D011EcmBmmBitsDriveModeDecodeIndependentOfOtherD011Bits()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Write(0xD016, 0x08); // MCM = 0.

        var expected = new (byte SelectorBits, Mos6569.VicIIDisplayMode Mode)[]
        {
            (0x00, Mos6569.VicIIDisplayMode.StandardText),
            (0x20, Mos6569.VicIIDisplayMode.StandardBitmap),
            (0x40, Mos6569.VicIIDisplayMode.ExtendedColor),
            (0x60, Mos6569.VicIIDisplayMode.Invalid),
        };

        foreach (var (selectorBits, mode) in expected)
        {
            // Clean selector bits only.
            vic.Write(0xD011, selectorBits);
            Assert.Equal(mode, vic.DisplayModeSelection);

            // Same selector with every non-selector $D011 bit set
            // (raster MSB, DEN, RSEL, YSCROLL = 0x9F noise).
            vic.Write(0xD011, (byte)(selectorBits | 0x9F));
            Assert.Equal(mode, vic.DisplayModeSelection);
        }
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-02 / TEST-VIC-DISPLAYMODE-02: vmode16_pipe =
    /// (regs[$16] &amp; 0x10) &gt;&gt; 2 (decode value; timing divergent).
    /// VICE viciisc/vicii-draw-cycle.c:243. Managed: Mos6569.cs 393 (MCM =
    /// $D016 bit 4 in DisplayModeSelection).
    /// Use case: Only bit 4 (MCM) of $D016 feeds the display-mode decode;
    /// CSEL, XSCROLL, and the RES bit never leak into it.
    /// Acceptance: Toggling $D016 bit 4 switches StandardText to
    /// MulticolorText and StandardBitmap to MulticolorBitmap, with and without
    /// all other $D016 low bits set (0x2F noise).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DISPLAYMODE-02", ParityTag.Faithful)]
    public void D016McmBitDrivesModeDecodeIndependentOfOtherD016Bits()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);

        // Text base: ECM=0 BMM=0.
        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x00);
        Assert.Equal(Mos6569.VicIIDisplayMode.StandardText, vic.DisplayModeSelection);
        vic.Write(0xD016, 0x10);
        Assert.Equal(Mos6569.VicIIDisplayMode.MulticolorText, vic.DisplayModeSelection);
        // Noise bits (RES, CSEL, XSCROLL) must not perturb the decode.
        vic.Write(0xD016, 0x2F);
        Assert.Equal(Mos6569.VicIIDisplayMode.StandardText, vic.DisplayModeSelection);
        vic.Write(0xD016, 0x3F);
        Assert.Equal(Mos6569.VicIIDisplayMode.MulticolorText, vic.DisplayModeSelection);

        // Bitmap base: BMM=1.
        vic.Write(0xD011, 0x3B);
        vic.Write(0xD016, 0x2F);
        Assert.Equal(Mos6569.VicIIDisplayMode.StandardBitmap, vic.DisplayModeSelection);
        vic.Write(0xD016, 0x3F);
        Assert.Equal(Mos6569.VicIIDisplayMode.MulticolorBitmap, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-07 / TEST-VIC-DISPLAYMODE-07: steady-state 5-mode
    /// decode plus ECM &amp; (BMM || MCM) -&gt; Invalid.
    /// VICE viciisc/vicii-draw-cycle.c:133-142 (the five valid colors[] rows
    /// and the three COL_NONE rows). Managed: Mos6569.cs DisplayModeSelection
    /// (394-399).
    /// Use case: All eight ECM/BMM/MCM selector combinations decode to the
    /// canonical display mode used by the pixel pipeline.
    /// Acceptance: 000, 001, 010, 011, 100 decode to StandardText,
    /// MulticolorText, StandardBitmap, MulticolorBitmap, ExtendedColor; 101,
    /// 110, 111 decode to Invalid.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DISPLAYMODE-07", ParityTag.Faithful)]
    public void SteadyStateDecodeYieldsFiveValidModesAndInvalidForEcmWithBmmOrMcm()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);

        var combos = new (byte D011, byte D016, Mos6569.VicIIDisplayMode Mode)[]
        {
            (0x1B, 0x08, Mos6569.VicIIDisplayMode.StandardText),     // ECM=0 BMM=0 MCM=0
            (0x1B, 0x18, Mos6569.VicIIDisplayMode.MulticolorText),   // ECM=0 BMM=0 MCM=1
            (0x3B, 0x08, Mos6569.VicIIDisplayMode.StandardBitmap),   // ECM=0 BMM=1 MCM=0
            (0x3B, 0x18, Mos6569.VicIIDisplayMode.MulticolorBitmap), // ECM=0 BMM=1 MCM=1
            (0x5B, 0x08, Mos6569.VicIIDisplayMode.ExtendedColor),    // ECM=1 BMM=0 MCM=0
            (0x5B, 0x18, Mos6569.VicIIDisplayMode.Invalid),          // ECM=1 BMM=0 MCM=1
            (0x7B, 0x08, Mos6569.VicIIDisplayMode.Invalid),          // ECM=1 BMM=1 MCM=0
            (0x7B, 0x18, Mos6569.VicIIDisplayMode.Invalid),          // ECM=1 BMM=1 MCM=1
        };

        foreach (var (d011, d016, mode) in combos)
        {
            vic.Write(0xD011, d011);
            vic.Write(0xD016, d016);
            Assert.Equal(mode, vic.DisplayModeSelection);
        }
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-08 / TEST-VIC-DISPLAYMODE-08: colors[] 32-entry
    /// table contents.
    /// VICE viciisc/vicii-draw-cycle.c:133-142. Managed: VideoRenderer.cs mode
    /// switch (273-281) and the per-mode pixel functions. The per-line
    /// renderer reaches every steady-state entry of the table: hires modes
    /// produce px 0/2-equivalent output and mc modes all four pairs; the
    /// transition-kludge entries (px 1/2 in hires rows) are only reachable via
    /// mid-cell mode changes, which are DIVERGENT (FR-VIC-DISPLAYMODE AC-03..
    /// AC-06) and excluded here.
    /// Use case: One render per colors[] row proves the full colour-source
    /// table: text (D021/CBUF), mc text (D021/D022/D023/CBUF_MC plus the
    /// hires-in-mc CBUF_MC path), bitmap (VBUF_L/VBUF_H), mc bitmap
    /// (D021/VBUF_H/VBUF_L/CBUF), ECM (D02X_EXT/CBUF), and all three invalid
    /// rows as COL_NONE black.
    /// Acceptance: Every sampled pixel matches the exact palette entry of its
    /// colors[] table cell as listed in the asserts below.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DISPLAYMODE-08", ParityTag.Faithful)]
    public void ColorsTableContentsPerModeRowMatchViceDrawCycleTable()
    {
        // Row ECM=0 BMM=0 MCM=0: {D021, D021, CBUF, CBUF}.
        {
            var (vic, ram, renderer) = CreateRenderMachine(0x1B, 0x08, 0x15);
            vic.Write(0xD021, BorderBlue);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x0E, charByte: 0x80);
            renderer.RenderFullFrame();
            int y = VideoRenderer.RasterLineToFrameY(line);
            Assert.Equal(ToBgra(0x0E), ReadPixel(renderer.FrameBuffer, x, y));
            Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x + 1, y));
        }

        // Row ECM=0 BMM=0 MCM=1: {D021, D022, D023, CBUF_MC} plus the
        // hires-in-mc cell (cbuf bit 3 clear) whose set bit is CBUF_MC.
        {
            var (vic, ram, renderer) = CreateRenderMachine(0x1B, 0x18, 0x15);
            vic.Write(0xD021, BorderBlue);
            vic.Write(0xD022, 0x02);
            vic.Write(0xD023, 0x04);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x0F, charByte: 0x1B);
            ConfigureCharacterCell(ram, vic, x + 8, line, charCode: 0x02, color: 0x05, charByte: 0x80);
            renderer.RenderFullFrame();
            int y = VideoRenderer.RasterLineToFrameY(line);
            Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x, y));
            Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, x + 2, y));
            Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x + 4, y));
            Assert.Equal(ToBgra(0x07), ReadPixel(renderer.FrameBuffer, x + 6, y));
            Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 8, y));
            Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x + 9, y));
        }

        // Row ECM=0 BMM=1 MCM=0: {VBUF_L, VBUF_L, VBUF_H, VBUF_H}.
        {
            var (vic, ram, renderer) = CreateRenderMachine(0x3B, 0x08, 0x18);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureBitmapCell(ram, vic, x, line, screenByte: 0xA5, colorRam: 0x00, bitmapByte: 0x80);
            renderer.RenderFullFrame();
            int y = VideoRenderer.RasterLineToFrameY(line);
            Assert.Equal(ToBgra(0x0A), ReadPixel(renderer.FrameBuffer, x, y));
            Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 1, y));
        }

        // Row ECM=0 BMM=1 MCM=1: {D021, VBUF_H, VBUF_L, CBUF}.
        {
            var (vic, ram, renderer) = CreateRenderMachine(0x3B, 0x18, 0x18);
            vic.Write(0xD021, BorderBlue);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureBitmapCell(ram, vic, x, line, screenByte: 0xA5, colorRam: 0x0E, bitmapByte: 0x1B);
            renderer.RenderFullFrame();
            int y = VideoRenderer.RasterLineToFrameY(line);
            Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x, y));
            Assert.Equal(ToBgra(0x0A), ReadPixel(renderer.FrameBuffer, x + 2, y));
            Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 4, y));
            Assert.Equal(ToBgra(0x0E), ReadPixel(renderer.FrameBuffer, x + 6, y));
        }

        // Row ECM=1 BMM=0 MCM=0: {D02X_EXT, D02X_EXT, CBUF, CBUF}.
        {
            var (vic, ram, renderer) = CreateRenderMachine(0x5B, 0x08, 0x15);
            vic.Write(0xD021, BorderBlue);
            vic.Write(0xD024, 0x05);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x03, charByte: 0x40);
            ConfigureCharacterCell(ram, vic, x + 8, line, charCode: 0xC1, color: 0x0C, charByte: 0x40);
            ram.Write((ushort)(vic.CharacterBase + 0x01 * 8), 0x40);
            renderer.RenderFullFrame();
            int y = VideoRenderer.RasterLineToFrameY(line);
            Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, x, y));
            Assert.Equal(ToBgra(0x03), ReadPixel(renderer.FrameBuffer, x + 1, y));
            Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 8, y));
            Assert.Equal(ToBgra(0x0C), ReadPixel(renderer.FrameBuffer, x + 9, y));
        }

        // Rows ECM=1 with BMM or MCM: all COL_NONE (palette 0).
        var invalidCombos = new (byte D011, byte D016, byte D018)[]
        {
            (0x5B, 0x18, 0x15),
            (0x7B, 0x08, 0x18),
            (0x7B, 0x18, 0x18),
        };
        foreach (var (d011, d016, d018) in invalidCombos)
        {
            var (vic, ram, renderer) = CreateRenderMachine(d011, d016, d018);
            vic.Write(0xD021, BorderBlue);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x0F, charByte: 0xFF);
            ConfigureBitmapCell(ram, vic, x, line, screenByte: 0xA5, colorRam: 0x0F, bitmapByte: 0xFF);
            renderer.RenderFullFrame();
            int y = VideoRenderer.RasterLineToFrameY(line);
            Assert.Equal(ToBgra(0x00), ReadPixel(renderer.FrameBuffer, x, y));
            Assert.Equal(ToBgra(0x00), ReadPixel(renderer.FrameBuffer, x + 1, y));
        }
    }

    // =====================================================================
    // FR-VIC-SPRITE-RENDER (FAITHFUL: AC-05, AC-06, AC-07, AC-08)
    // =====================================================================

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-05 / TEST-VIC-SPRITE-RENDER-05: MC pixel
    /// px = (sbuf &gt;&gt; 22) &amp; 3 held 2 pixels via sbuf_mc_flops (pairing
    /// rule).
    /// VICE viciisc/vicii-draw-cycle.c:363-369 (pair fetched when the mc flop
    /// is set, held while it toggles). Managed: VideoRenderer.cs
    /// TryGetSpritePaletteIndex mc branch (456-467, even-source anchoring).
    /// Use case: A multicolour sprite renders each two-bit pair as a two-pixel
    /// block anchored at even sprite-local columns.
    /// Acceptance: Row byte 0x6C (pairs 01,10,11,00) renders pixel pairs
    /// (0,1)=(1,1)($D025), (2,3)=(2,2)(sprite colour), (4,5)=(3,3)($D026),
    /// and (6,7) background; each odd pixel equals its even partner.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-05", ParityTag.Faithful)]
    public void McSpritePixelsRenderAsTwoPixelPairs()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        vic.Write(0xD025, 0x07);
        vic.Write(0xD026, 0x05);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0x6C);
        vic.Write(0xD01C, 0x01);
        vic.Write(0xD015, 0x01);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        // Pair 01 -> $D025, held for two pixels.
        Assert.Equal(ToBgra(0x07), ReadPixel(renderer.FrameBuffer, 96, y));
        Assert.Equal(ToBgra(0x07), ReadPixel(renderer.FrameBuffer, 97, y));
        // Pair 10 -> sprite colour, held for two pixels.
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 98, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 99, y));
        // Pair 11 -> $D026, held for two pixels.
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, 100, y));
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, 101, y));
        // Pair 00 -> transparent for both pixels (background shows).
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, 102, y));
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, 103, y));
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-06 / TEST-VIC-SPRITE-RENDER-06: MC pair values
    /// map 1 to $D025, 2 to $D027+n, 3 to $D026, and 0 to transparent.
    /// VICE viciisc/vicii-draw-cycle.c:406-418 (switch on sbuf_pixel_reg).
    /// Managed: VideoRenderer.cs TryGetSpritePaletteIndex mc colour switch
    /// (467-479).
    /// Use case: A multicolour sprite other than sprite 0 routes each pair
    /// value to its VICE colour register, including the per-sprite $D027+n
    /// register for pair 2.
    /// Acceptance: Sprite 1 with row byte 0x6C renders pair 01 as $D025, pair
    /// 10 as $D028 (n=1), pair 11 as $D026, and pair 00 as the background.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-06", ParityTag.Faithful)]
    public void McSpritePairValuesMapToD025PerSpriteColourD026AndTransparent()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        vic.Write(0xD025, 0x07);
        vic.Write(0xD026, 0x0E);
        // Sprite 1: its pair-10 colour must come from $D028 = $D027 + 1.
        ConfigureSprite(ram, vic, sprite: 1, x: 96, y: 60, pointer: 0x21, color: 0x09, row0: 0x6C);
        vic.Write(0xD01C, 0x02);
        vic.Write(0xD015, 0x02);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x07), ReadPixel(renderer.FrameBuffer, 96, y));  // pair 01 -> $D025
        Assert.Equal(ToBgra(0x09), ReadPixel(renderer.FrameBuffer, 98, y));  // pair 10 -> $D028
        Assert.Equal(ToBgra(0x0E), ReadPixel(renderer.FrameBuffer, 100, y)); // pair 11 -> $D026
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, 102, y)); // pair 00 -> transparent
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-07 / TEST-VIC-SPRITE-RENDER-07: hires opaque
    /// pixel renders $D027+n.
    /// VICE viciisc/vicii-draw-cycle.c:411 (case 2: COL_D027 + as; hires bits
    /// become px value 2). Managed: VideoRenderer.cs TryGetSpritePaletteIndex
    /// hires tail (495).
    /// Use case: A hires sprite's set bits use that sprite's own colour
    /// register, indexed by sprite number from $D027.
    /// Acceptance: Sprite 2 with row byte 0x80 and $D029 = 0x0B renders the
    /// opaque pixel as palette 0x0B and the adjacent clear bit as background.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-07", ParityTag.Faithful)]
    public void HiresSpriteOpaquePixelUsesPerSpriteColourRegister()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        // Sprite 2: colour register $D029 = $D027 + 2.
        ConfigureSprite(ram, vic, sprite: 2, x: 96, y: 60, pointer: 0x22, color: 0x0B, row0: 0x80);
        vic.Write(0xD015, 0x04);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x0B), ReadPixel(renderer.FrameBuffer, 96, y));
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, 97, y));
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-08 / TEST-VIC-SPRITE-RENDER-08: X-expansion
    /// doubling (result).
    /// VICE viciisc/vicii-draw-cycle.c:377-384 (sbuf shifts every second pixel
    /// while sprite_expx_bits toggles the expansion flop). Managed:
    /// VideoRenderer.cs TryGetSpritePaletteIndex (448, sourceX = localX / 2
    /// with width 48).
    /// Use case: An X-expanded sprite renders every source bit as two adjacent
    /// screen pixels.
    /// Acceptance: Row byte 0xC0 with $D01D bit 0 set renders four opaque
    /// pixels (two source bits doubled) followed by background.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-08", ParityTag.Faithful)]
    public void XExpandedSpriteDoublesEachSourcePixel()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0xC0);
        vic.Write(0xD01D, 0x01);
        vic.Write(0xD015, 0x01);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 96, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 97, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 98, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 99, y));
        Assert.Equal(ToBgra(BorderBlue), ReadPixel(renderer.FrameBuffer, 100, y));
    }

    // =====================================================================
    // FR-VIC-SPRITE-COLLISION (FAITHFUL: AC-02, AC-03, AC-04, AC-05, AC-10,
    // AC-11)
    // =====================================================================

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-02 / TEST-VIC-SPRITE-COLLISION-02:
    /// sprite-sprite collision follows (mask &amp; (mask - 1)): the $D01E latch
    /// accumulates all mask bits when two or more sprites are opaque at the
    /// same pixel, and never for a lone opaque sprite (rule; per-line timing
    /// divergent).
    /// VICE viciisc/vicii-draw-cycle.c:427-429. Managed: Mos6569.cs
    /// ProcessSpriteCollisionsForRasterLine (2276-2280).
    /// Use case: Three sprites share one opaque pixel while a fourth enabled
    /// sprite sits alone elsewhere on the same line.
    /// Acceptance: $D01E reads exactly 0x07 (bits 0-2 latched, bit 3 clear).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-02", ParityTag.Faithful)]
    public void SpriteSpriteCollisionRequiresTwoOrMoreOverlappingSpritesAndLatchesAllMaskBits()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);

        ConfigureSprite(ram, vic, sprite: 0, x: 100, y: 100, pointer: 0x20, color: 0x02, row0: 0x80);
        ConfigureSprite(ram, vic, sprite: 1, x: 100, y: 100, pointer: 0x21, color: 0x03, row0: 0x80);
        ConfigureSprite(ram, vic, sprite: 2, x: 100, y: 100, pointer: 0x22, color: 0x04, row0: 0x80);
        // Sprite 3 is opaque but alone: a single mask bit never latches.
        ConfigureSprite(ram, vic, sprite: 3, x: 200, y: 100, pointer: 0x23, color: 0x05, row0: 0x80);
        vic.Write(0xD015, 0x0F);

        AdvanceToLineStart(vic, 103);

        Assert.Equal(0x07, vic.Read(0xD01E));
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-03 / TEST-VIC-SPRITE-COLLISION-03:
    /// sprite-background collision is gated on the foreground pixel_pri bit
    /// (rule; per-line timing divergent).
    /// VICE viciisc/vicii-draw-cycle.c:401-424 (collision only when
    /// pixel_pri, i.e. graphics px &amp; 0x2). Managed: Mos6569.cs
    /// ProcessSpriteCollisionsForRasterLine (2281-2286).
    /// Use case: A sprite pixel over hires foreground collides; over hires
    /// background it does not; over an mc-text 01 pair (coloured but px bit 1
    /// clear) it does not; over an mc-text 10 pair it does.
    /// Acceptance: $D01F reads 0x01 for the hires-foreground and mc-pair-10
    /// scenarios and 0x00 for the hires-background and mc-pair-01 scenarios.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-03", ParityTag.Faithful)]
    public void SpriteBackgroundCollisionIsGatedOnForegroundPriorityPixels()
    {
        static byte RunScenario(byte d016, byte colorRam, byte glyphByte)
        {
            var (bus, ram, irq) = CreateTestMachine();
            var vic = new Mos6569(bus, irq);
            vic.Write(0xD011, 0x1B);
            vic.Write(0xD016, d016);
            vic.Write(0xD018, 0x15);
            // Supply character data so DrawGraphics8 fills PriBuffer for the
            // SB-collision gate check (phi1 cycle -> GbufPipe0 -> PriBuffer).
            int fbl = vic.UpperBorderStart + ((vic.YScroll - (vic.UpperBorderStart & 7) + 8) & 7);
            vic.Phi1MemoryReader = cycle =>
            {
                if (cycle < 14 || cycle >= 54) return 0;
                int col = cycle - 14;
                int screenRow = (vic.CurrentRasterLine - fbl) / 8;
                if (screenRow < 0 || screenRow >= 25) return 0;
                int rowCounter = (vic.CurrentRasterLine - fbl) & 7;
                int screenIndex = screenRow * 40 + col;
                byte screenCode = bus.Read((ushort)(vic.ScreenMemoryBase + screenIndex));
                return bus.Read((ushort)(vic.CharacterBase + screenCode * 8 + rowCounter));
            };

            int line = FirstVisibleBadLine(vic);
            // rasterLine: line+1 because DrawSprites8 detects collision on the first
            // rendering line (sprite.Y+1), so the foreground source must be at
            // the char row for line+1.
            ConfigureCharacterCell(ram, vic, x: 96, rasterLine: line + 1, charCode: 0x01, color: colorRam, charByte: glyphByte);
            ConfigureSprite(ram, vic, sprite: 0, x: 96, y: (byte)line, pointer: 0x20, color: 0x02, row0: 0x80);
            vic.Write(0xD015, 0x01);

            AdvanceToLineStart(vic, line + 3);
            return vic.Read(0xD01F);
        }

        // Hires glyph bit set under the sprite pixel: foreground -> collision.
        Assert.Equal(0x01, RunScenario(d016: 0x08, colorRam: 0x0E, glyphByte: 0x80));
        // Hires glyph bit clear: background -> no collision.
        Assert.Equal(0x00, RunScenario(d016: 0x08, colorRam: 0x0E, glyphByte: 0x00));
        // Mc text pair 01 ($D022 colour, px bit 1 clear): coloured but NOT
        // foreground -> no collision.
        Assert.Equal(0x00, RunScenario(d016: 0x18, colorRam: 0x0B, glyphByte: 0x40));
        // Mc text pair 10 (px bit 1 set): foreground -> collision.
        Assert.Equal(0x01, RunScenario(d016: 0x18, colorRam: 0x0B, glyphByte: 0x80));
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-04 / TEST-VIC-SPRITE-COLLISION-04:
    /// collisions are recorded independent of $D01B priority.
    /// VICE viciisc/vicii-draw-cycle.c:391-429 (collision_mask and the
    /// pixel_pri gate never consult sprite_pri_bits). Managed: Mos6569.cs
    /// ProcessSpriteCollisionsForRasterLine (2168-2290, no IsPriority use).
    /// Use case: Two overlapping sprites over a foreground graphics pixel are
    /// both marked behind-background via $D01B; they are visually hidden but
    /// their collisions still latch.
    /// Acceptance: With $D01B = 0x03, $D01E reads 0x03 and $D01F reads 0x03.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-04", ParityTag.Faithful)]
    public void CollisionsAreRecordedIndependentOfD01BSpritePriority()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD018, 0x15);
        // Supply character data so DrawGraphics8 fills PriBuffer for the
        // SB-collision gate check (phi1 cycle -> GbufPipe0 -> PriBuffer).
        {
            int fbl = vic.UpperBorderStart + ((vic.YScroll - (vic.UpperBorderStart & 7) + 8) & 7);
            vic.Phi1MemoryReader = cycle =>
            {
                if (cycle < 14 || cycle >= 54) return 0;
                int col = cycle - 14;
                int screenRow = (vic.CurrentRasterLine - fbl) / 8;
                if (screenRow < 0 || screenRow >= 25) return 0;
                int rowCounter = (vic.CurrentRasterLine - fbl) & 7;
                int screenIndex = screenRow * 40 + col;
                byte screenCode = bus.Read((ushort)(vic.ScreenMemoryBase + screenIndex));
                return bus.Read((ushort)(vic.CharacterBase + screenCode * 8 + rowCounter));
            };
        }

        int line = FirstVisibleBadLine(vic);
        // rasterLine: line+1 because DrawSprites8 detects collision on the first
        // rendering line (sprite.Y+1), so the foreground source must be at
        // the char row for line+1.
        ConfigureCharacterCell(ram, vic, x: 96, rasterLine: line + 1, charCode: 0x01, color: 0x0E, charByte: 0x80);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: (byte)line, pointer: 0x20, color: 0x02, row0: 0x80);
        ConfigureSprite(ram, vic, sprite: 1, x: 96, y: (byte)line, pointer: 0x21, color: 0x03, row0: 0x80);
        vic.Write(0xD01B, 0x03);
        vic.Write(0xD015, 0x03);

        AdvanceToLineStart(vic, line + 3);

        Assert.Equal(0x03, vic.Read(0xD01E));
        Assert.Equal(0x03, vic.Read(0xD01F));
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-05 / TEST-VIC-SPRITE-COLLISION-05:
    /// transparent pixels (MC pair 00, hires 0 bits) never collide.
    /// VICE viciisc/vicii-draw-cycle.c:391 (only sbuf_pixel_reg != 0 joins the
    /// collision mask). Managed: Mos6569.cs ProcessSpriteCollisionsForRasterLine
    /// opacity tests (2235, 2239).
    /// Use case: Sprites whose bounding boxes overlap but whose opaque pixels
    /// never share a coordinate must not latch $D01E; shifting one sprite so
    /// opaque pixels meet must latch it.
    /// Acceptance: Hires 0-bit overlap reads 0x00; MC pair-00 overlap reads
    /// 0x00; the control with coinciding opaque pixels reads 0x03.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-05", ParityTag.Faithful)]
    public void TransparentSpritePixelsNeverCollide()
    {
        static byte RunScenario(byte sprite0Row0, bool sprite0Multicolor, int sprite1X)
        {
            var (bus, ram, irq) = CreateTestMachine();
            var vic = new Mos6569(bus, irq);
            vic.Write(0xD011, 0x1B);
            vic.Write(0xD016, 0x08);

            ConfigureSprite(ram, vic, sprite: 0, x: 100, y: 100, pointer: 0x20, color: 0x02, row0: sprite0Row0);
            ConfigureSprite(ram, vic, sprite: 1, x: sprite1X, y: 100, pointer: 0x21, color: 0x03, row0: 0x80);
            if (sprite0Multicolor)
            {
                vic.Write(0xD01C, 0x01);
            }

            vic.Write(0xD015, 0x03);
            AdvanceToLineStart(vic, 103);
            return vic.Read(0xD01E);
        }

        // Hires: sprite 0 opaque only at x=100, sprite 1 opaque only at x=101.
        // Bounding boxes overlap 23 pixels but no opaque pixel coincides.
        Assert.Equal(0x00, RunScenario(sprite0Row0: 0x80, sprite0Multicolor: false, sprite1X: 101));
        // MC pair 00: sprite 0 (0x30 = pairs 00,11,00,00) is transparent at
        // x=100..101 where sprite 1 is opaque at x=100.
        Assert.Equal(0x00, RunScenario(sprite0Row0: 0x30, sprite0Multicolor: true, sprite1X: 100));
        // Control: sprite 1 moved onto sprite 0's opaque MC pair at x=102.
        Assert.Equal(0x03, RunScenario(sprite0Row0: 0x30, sprite0Multicolor: true, sprite1X: 102));
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-10 / TEST-VIC-SPRITE-COLLISION-10:
    /// sprite-sprite collisions are recorded even under the border.
    /// VICE viciisc/vicii-draw-cycle.c:679-683 (draw_sprites8 runs before
    /// draw_border8, so the border only overdraws pixels; collisions latched
    /// first). Managed: Mos6569.cs ProcessSpriteCollisionsForRasterLine
    /// sprite-sprite sweep (2269-2280) has no border gate.
    /// Use case: Two overlapping opaque sprites sit entirely inside the top
    /// vertical border where no display window exists.
    /// Acceptance: After the border lines pass, $D01E reads 0x03 while the
    /// premise IsRasterLineVerticalBorderActive(30) holds.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-10", ParityTag.Faithful)]
    public void SpriteSpriteCollisionsAreRecordedUnderTheVerticalBorder()
    {
        var vic = BuildStubVic(bgPattern: 0x00);
        // Sprite rows 30..50 sit fully above UpperBorderStart (51): all border.
        vic.Write(0xD000, 100);
        vic.Write(0xD001, 30);
        vic.Write(0xD002, 100);
        vic.Write(0xD003, 30);
        vic.Write(0xD015, 0x03);

        AdvanceToLineStart(vic, 55);

        Assert.True(vic.IsRasterLineVerticalBorderActive(30));
        Assert.Equal(0x03, vic.Read(0xD01E));
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-11 / TEST-VIC-SPRITE-COLLISION-11:
    /// sprite-background collision is suppressed where the graphics foreground
    /// bit is 0 (vertical border).
    /// VICE viciisc/vicii-draw-cycle.c:275-280 (gbuf pipe forced 0 outside the
    /// visible display, so pixel_pri is 0 in the vertical border). Managed:
    /// Mos6569.cs CanRenderSpritePixelAt vertical-border gate (731-734) and
    /// the sprite-background sweep gate (2283).
    /// Use case: The whole character window is foreground (0xFF glyph
    /// pattern); a sprite fully inside the top vertical border must not latch
    /// $D01F, while the identical sprite inside the display window must.
    /// Acceptance: Border sprite reads $D01F = 0x00; in-window control sprite
    /// reads $D01F = 0x01.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-11", ParityTag.Faithful)]
    public void SpriteBackgroundCollisionIsSuppressedInTheVerticalBorder()
    {
        // Sprite entirely inside the vertical border (rows 21..41 < 51).
        var borderVic = BuildStubVic(bgPattern: 0xFF);
        borderVic.Write(0xD000, 100);
        borderVic.Write(0xD001, 20);
        borderVic.Write(0xD015, 0x01);
        AdvanceToLineStart(borderVic, 55);
        Assert.True(borderVic.IsRasterLineVerticalBorderActive(20));
        Assert.Equal(0x00, borderVic.Read(0xD01F));

        // Control: identical sprite inside the display window latches.
        var windowVic = BuildStubVic(bgPattern: 0xFF);
        windowVic.Write(0xD000, 100);
        windowVic.Write(0xD001, 100);
        windowVic.Write(0xD015, 0x01);
        AdvanceToLineStart(windowVic, 125);
        Assert.False(windowVic.IsRasterLineVerticalBorderActive(100));
        Assert.Equal(0x01, windowVic.Read(0xD01F));
    }

    // =====================================================================
    // FR-VIC-SPRITE-PRIORITY (FAITHFUL: AC-01, AC-02, AC-03, AC-04, AC-06)
    // =====================================================================

    /// <summary>
    /// FR-VIC-SPRITE-PRIORITY AC-01 / TEST-VIC-SPRITE-PRIORITY-01: winner =
    /// lowest-numbered opaque sprite (for the all-in-front case).
    /// VICE viciisc/vicii-draw-cycle.c:356,391-393 (descending loop leaves the
    /// lowest opaque sprite as active_sprite). Managed: VideoRenderer.cs
    /// TryGetSpritePixel ascending first-opaque loop (405-424).
    /// Use case: Two in-front sprites overlap; where both are opaque the lower
    /// number wins, and where the lower number is transparent the higher
    /// number's pixel shows (opacity, not enable order, selects the winner).
    /// Acceptance: Sprite 0 (row 0x40) and sprite 1 (row 0xC0) at the same
    /// coordinate render sprite 1's colour at pixel 0 and sprite 0's colour at
    /// pixel 1.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-PRIORITY-01", ParityTag.Faithful)]
    public void LowestNumberedOpaqueSpriteWinsThePixel()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        // Sprite 0 transparent at local pixel 0, opaque at local pixel 1.
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0x40);
        // Sprite 1 opaque at local pixels 0 and 1.
        ConfigureSprite(ram, vic, sprite: 1, x: 96, y: 60, pointer: 0x21, color: 0x04, row0: 0xC0);
        vic.Write(0xD015, 0x03);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        // Pixel 96: only sprite 1 is opaque -> sprite 1 wins.
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, 96, y));
        // Pixel 97: both opaque -> lowest number (sprite 0) wins.
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 97, y));
    }

    /// <summary>
    /// FR-VIC-SPRITE-PRIORITY AC-02 / TEST-VIC-SPRITE-PRIORITY-02: the behind
    /// test is applied only to the winner.
    /// VICE viciisc/vicii-draw-cycle.c:401-419 (spri = sprite_pri_bits &amp;
    /// (1 &lt;&lt; active_sprite); only the winner's $D01B bit is consulted).
    /// Managed: VideoRenderer.cs TryGetSpritePixel (417-423).
    /// Use case: A non-winner sprite's $D01B bit must never affect the
    /// composite: an in-front winner shows over foreground graphics although a
    /// behind sprite overlaps the same pixel, and a behind winner shows over
    /// background graphics although an in-front sprite overlaps it.
    /// Acceptance: With sprite 0 in front and sprite 1 behind over a
    /// foreground pixel, the pixel is sprite 0's colour; with sprite 0 behind
    /// and sprite 1 in front over a background pixel, the pixel is sprite 0's
    /// colour.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-PRIORITY-02", ParityTag.Faithful)]
    public void BehindTestUsesOnlyTheWinnersPriorityBit()
    {
        // Scenario A: winner (sprite 0) in front, non-winner (sprite 1) behind,
        // over a foreground graphics pixel.
        {
            var (bus, ram, irq) = CreateTestMachine();
            var vic = new Mos6569(bus, irq);
            var renderer = new VideoRenderer(vic, bus);
            vic.Write(0xD011, 0x1B);
            vic.Write(0xD016, 0x08);
            vic.Write(0xD021, BorderBlue);
            ConfigureCharacterCell(ram, vic, x: 96, rasterLine: 60, charCode: 0x01, color: 0x04, charByte: 0x80);
            ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0x80);
            ConfigureSprite(ram, vic, sprite: 1, x: 96, y: 60, pointer: 0x21, color: 0x03, row0: 0x80);
            vic.Write(0xD01B, 0x02); // Only the NON-winner is behind.
            vic.Write(0xD015, 0x03);

            renderer.RenderFullFrame();

            int y = VideoRenderer.RasterLineToFrameY(60);
            Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 96, y));
        }

        // Scenario B: winner (sprite 0) behind, non-winner (sprite 1) in front,
        // over a background (non-foreground) graphics pixel: the winner's own
        // behind test passes, so the winner draws.
        {
            var (bus, ram, irq) = CreateTestMachine();
            var vic = new Mos6569(bus, irq);
            var renderer = new VideoRenderer(vic, bus);
            vic.Write(0xD011, 0x1B);
            vic.Write(0xD016, 0x08);
            vic.Write(0xD021, BorderBlue);
            // Foreground exists only at pixel 96; pixel 97 is background.
            ConfigureCharacterCell(ram, vic, x: 96, rasterLine: 60, charCode: 0x01, color: 0x04, charByte: 0x80);
            ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0xC0);
            ConfigureSprite(ram, vic, sprite: 1, x: 96, y: 60, pointer: 0x21, color: 0x03, row0: 0xC0);
            vic.Write(0xD01B, 0x01); // Only the winner is behind.
            vic.Write(0xD015, 0x03);

            renderer.RenderFullFrame();

            int y = VideoRenderer.RasterLineToFrameY(60);
            Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 97, y));
        }
    }

    /// <summary>
    /// FR-VIC-SPRITE-PRIORITY AC-03 / TEST-VIC-SPRITE-PRIORITY-03: an in-front
    /// winner draws over a foreground background pixel.
    /// VICE viciisc/vicii-draw-cycle.c:405 (!(pixel_pri &amp;&amp; spri) is
    /// true when spri is 0, so the sprite colour replaces the foreground).
    /// Managed: VideoRenderer.cs TryGetSpritePixel (417).
    /// Use case: A default-priority sprite covers a character's foreground
    /// pixel and must appear in front of it.
    /// Acceptance: The overlap pixel renders the sprite colour, not the
    /// Color RAM glyph colour.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-PRIORITY-03", ParityTag.Faithful)]
    public void InFrontWinnerDrawsOverForegroundGraphicsPixel()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        ConfigureCharacterCell(ram, vic, x: 96, rasterLine: 60, charCode: 0x01, color: 0x04, charByte: 0x80);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0x80);
        vic.Write(0xD01B, 0x00);
        vic.Write(0xD015, 0x01);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 96, y));
    }

    /// <summary>
    /// FR-VIC-SPRITE-PRIORITY AC-04 / TEST-VIC-SPRITE-PRIORITY-04: a behind
    /// winner shows over non-foreground background and is hidden by
    /// foreground.
    /// VICE viciisc/vicii-draw-cycle.c:405 (pixel_pri &amp;&amp; spri hides
    /// the winner; otherwise it draws). Managed: VideoRenderer.cs
    /// TryGetSpritePixel behind gate (417-420).
    /// Use case: A $D01B-behind sprite crosses a cell whose glyph has a single
    /// foreground bit: the sprite hides under that bit and shows over the
    /// adjacent background bit.
    /// Acceptance: The foreground pixel keeps the Color RAM colour and the
    /// adjacent background pixel shows the sprite colour.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-PRIORITY-04", ParityTag.Faithful)]
    public void BehindWinnerShowsOverBackgroundAndHidesUnderForeground()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, BorderBlue);
        // Foreground bit only at pixel 96; sprite opaque at pixels 96 and 97.
        ConfigureCharacterCell(ram, vic, x: 96, rasterLine: 60, charCode: 0x01, color: 0x04, charByte: 0x80);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, row0: 0xC0);
        vic.Write(0xD01B, 0x01);
        vic.Write(0xD015, 0x01);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, 96, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 97, y));
    }

    /// <summary>
    /// FR-VIC-SPRITE-PRIORITY AC-06 / TEST-VIC-SPRITE-PRIORITY-06: pixel_pri
    /// comes from the graphics px &amp; 0x02 bit in all modes including
    /// invalid-ECM.
    /// VICE viciisc/vicii-draw-cycle.c:196,224,402 (pixel_pri = px &amp; 0x2
    /// stored in pri_buffer and consumed by the sprite priority test).
    /// Managed: Mos6569.cs IsGraphicsPixelForegroundForSpritePriority
    /// (760-806).
    /// Use case: The priority bit must be the pair/bit high bit, not "any
    /// non-background colour": mc 01 pairs are coloured yet non-foreground,
    /// mc 10 pairs are foreground, hires follows the data bit, and invalid
    /// ECM+MCM keeps the hidden px bit 1 despite rendering black.
    /// Acceptance: The foreground query returns exactly the px &amp; 0x2
    /// values asserted per mode and pixel below.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-PRIORITY-06", ParityTag.Faithful)]
    public void GraphicsPriorityBitFollowsVicePxBit1AcrossAllModes()
    {
        // Standard text: bit 7 set -> foreground; bit 6 clear -> background.
        {
            var (vic, ram, _) = CreateRenderMachine(0x1B, 0x08, 0x15);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x0E, charByte: 0x80);
            Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(x, line));
            Assert.False(vic.IsGraphicsPixelForegroundForSpritePriority(x + 1, line));
        }

        // Mc text (cbuf bit 3 set), glyph 0x60 = pairs 01,10: pair 01 is
        // coloured but px bit 1 is clear; pair 10 has px bit 1 set.
        {
            var (vic, ram, _) = CreateRenderMachine(0x1B, 0x18, 0x15);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x0B, charByte: 0x60);
            Assert.False(vic.IsGraphicsPixelForegroundForSpritePriority(x, line));
            Assert.False(vic.IsGraphicsPixelForegroundForSpritePriority(x + 1, line));
            Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(x + 2, line));
            Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(x + 3, line));
        }

        // Mc bitmap, bitmap byte 0x60 = pairs 01,10: same px bit 1 rule.
        {
            var (vic, ram, _) = CreateRenderMachine(0x3B, 0x18, 0x18);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureBitmapCell(ram, vic, x, line, screenByte: 0xA5, colorRam: 0x03, bitmapByte: 0x60);
            Assert.False(vic.IsGraphicsPixelForegroundForSpritePriority(x, line));
            Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(x + 2, line));
        }

        // Invalid ECM+MCM: renders black but preserves the mc pair's px bit 1.
        {
            var (vic, ram, _) = CreateRenderMachine(0x58, 0x18, 0x15);
            int line = FirstVisibleBadLine(vic);
            int x = vic.LeftBorderPixel;
            ConfigureCharacterCell(ram, vic, x, line, charCode: 0x01, color: 0x0B, charByte: 0x60);
            Assert.False(vic.IsGraphicsPixelForegroundForSpritePriority(x, line));
            Assert.True(vic.IsGraphicsPixelForegroundForSpritePriority(x + 2, line));
        }
    }
}
