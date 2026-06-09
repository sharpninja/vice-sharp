namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Chips.VicIi;
using ViceSharp.Abstractions;
using ViceSharp.Core;

public sealed class VideoRendererTests
{
    private static (BasicBus bus, RamDevice ram, InterruptLine irq) CreateTestMachine()
    {
        var bus = new BasicBus();
        var memory = new byte[0x10000];
        var ram = new RamDevice(0x0000, 0xFFFF, memory);
        var irq = new InterruptLine(InterruptType.Irq);
        bus.RegisterDevice(ram);
        return (bus, ram, irq);
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Constructing a VideoRenderer must allocate the BGRA
    /// frame buffer at the canonical screen dimensions (384x272x4
    /// bytes).
    /// Acceptance: <c>renderer.FrameBuffer</c> is non-null and equals
    /// <c>ScreenWidth * ScreenHeight * 4</c> bytes in length.
    /// </summary>
    [Fact]
    public void Constructor_CreatesFrameBuffer()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        
        // Act
        var renderer = new VideoRenderer(vic, bus);
        
        // Assert
        Assert.NotNull(renderer.FrameBuffer);
        Assert.Equal(VideoRenderer.ScreenWidth * VideoRenderer.ScreenHeight * 4, renderer.FrameBuffer.Length);
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Calling <see cref="VideoRenderer.Reset"/> on a renderer
    /// must clear the entire frame buffer back to all zero bytes.
    /// Acceptance: After seeding the frame buffer with $FF and calling
    /// Reset, every byte in the buffer is 0.
    /// </summary>
    [Fact]
    public void Reset_ClearsFrameBuffer()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);
        
        // Fill with non-zero
        for (int i = 0; i < renderer.FrameBuffer.Length; i++)
            renderer.FrameBuffer[i] = 0xFF;
        
        // Act
        renderer.Reset();
        
        // Assert
        Assert.All(renderer.FrameBuffer, b => Assert.Equal(0, b));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: Rendering a full frame on a reset VIC-II must paint the
    /// border with palette index 0 (black) at the (0,0) pixel before
    /// any boot code reprograms the border colour.
    /// Acceptance: The BGRA value at pixel (0,0) of the rendered frame
    /// is 0xFF000000 (opaque black).
    /// </summary>
    [Fact]
    public void FrameBuffer_Contains_Black_For_ResetBorder()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        var renderer = new VideoRenderer(vic, bus);
        
        // Act - Render full frame with border
        renderer.RenderFullFrame();
        
        // Assert - reset border color is index 0 until the C64 boot path programs it.
        uint firstPixel = BitConverter.ToUInt32(renderer.FrameBuffer, 0);
        uint expectedBlack = 0xFF000000;
        
        Assert.Equal(expectedBlack, firstPixel);
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Running a full frame on a freshly reset machine must
    /// complete without throwing.
    /// Acceptance: <see cref="VideoRenderer.RenderFullFrame"/> returns
    /// normally (Record.Exception yields null).
    /// </summary>
    [Fact]
    public void RenderFullFrame_Completes_Without_Error()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        var renderer = new VideoRenderer(vic, bus);
        
        // Act & Assert
        var exception = Record.Exception(() => renderer.RenderFullFrame());
        Assert.Null(exception);
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: The renderer exposes the canonical C64 overscan screen
    /// dimensions (384x272) as constants so host code can size buffers
    /// before constructing a renderer.
    /// Acceptance: <c>ScreenWidth == 384</c> and
    /// <c>ScreenHeight == 272</c>.
    /// </summary>
    [Fact]
    public void ScreenDimensions_Are_Correct()
    {
        // Assert standard C64 screen dimensions (overscan)
        Assert.Equal(384, VideoRenderer.ScreenWidth);
        Assert.Equal(272, VideoRenderer.ScreenHeight);
    }

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: With 25-row mode active and the border/background
    /// colours programmed (Light Blue / Blue), the renderer must crop
    /// the PAL raster such that the active screen area sits centred in
    /// the 272-line frame buffer.
    /// Acceptance: Pixel (24,35) is border colour; pixel (24,36) is the
    /// background; the lower transition mirrors the upper one at rows
    /// 235/236, proving symmetrical vertical centring.
    /// </summary>
    [Fact]
    public void RenderFullFrame_CropsPalRasterSoActiveScreenIsVerticallyCentered()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD020, 0x0E);
        vic.Write(0xD021, 0x06);

        renderer.RenderFullFrame();

        var border = ToBgra(0x0E);
        var background = ToBgra(0x06);

        Assert.Equal(border, ReadPixel(renderer.FrameBuffer, 24, 35));
        Assert.Equal(background, ReadPixel(renderer.FrameBuffer, 24, 36));
        Assert.Equal(background, ReadPixel(renderer.FrameBuffer, 343, 235));
        Assert.Equal(border, ReadPixel(renderer.FrameBuffer, 343, 236));
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: The renderer's static PAL timing constants must equal
    /// the documented PAL VIC-II values (63 cycles per line, 312 total
    /// lines, 272 visible) so callers can compute frame budgets.
    /// Acceptance: <c>PalCyclesPerLine == 63</c>,
    /// <c>PalTotalLines == 312</c>, <c>PalVisibleLines == 272</c>.
    /// </summary>
    [Fact]
    public void PAL_Timing_Is_Correct()
    {
        // Assert PAL timing values
        Assert.Equal(63, VideoRenderer.PalCyclesPerLine);
        Assert.Equal(312, VideoRenderer.PalTotalLines);
        Assert.Equal(272, VideoRenderer.PalVisibleLines);
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: FrameBuffer.Length must be measured in bytes (4 per
    /// pixel for BGRA), not pixels; a mismatch would cause buffer
    /// over/underruns in host blit code.
    /// Acceptance: <c>FrameBuffer.Length</c> equals
    /// <c>ScreenWidth * ScreenHeight * 4</c>.
    /// </summary>
    [Fact]
    public void FrameBuffer_Size_Is_Bytes_Not_Pixels()
    {
        // FrameBuffer is 384 * 272 * 4 bytes (BGRA)
        int expectedBytes = VideoRenderer.ScreenWidth * VideoRenderer.ScreenHeight * 4;
        
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);
        
        Assert.Equal(expectedBytes, renderer.FrameBuffer.Length);
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: A freshly reset VIC-II's BorderColor register must be
    /// palette index 0 (black) until the boot ROM writes a different
    /// value.
    /// Acceptance: <c>vic.BorderColor == 0</c> after Reset.
    /// </summary>
    [Fact]
    public void BorderColor_Is_Black_After_Reset()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        Assert.Equal(0, vic.BorderColor);
    }

    /// <summary>
    /// FR: FR-VIC-002, TR: TR-CYCLE-001.
    /// Use case: A freshly reset VIC-II's BackgroundColor register must
    /// also be palette index 0 (black) until configured by the boot ROM.
    /// Acceptance: <c>vic.BackgroundColor == 0</c> after Reset.
    /// </summary>
    [Fact]
    public void BackgroundColor_Is_Black_After_Reset()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        Assert.Equal(0, vic.BackgroundColor);
    }

    /// <summary>
    /// FR: FR-VIC-002, FR: FR-VIC-003, TR: TR-CYCLE-001.
    /// Use case: $D018 controls the screen memory and character base
    /// pointers; $D011/$D016 control the display mode. The VIC-II must
    /// decode these registers to the documented base addresses and
    /// modes (StandardText, MulticolorText, Bitmap).
    /// Acceptance: With $D018=$15, screen base resolves to $0400 and
    /// character base to $1000; setting MCM bit 4 in $D016 transitions
    /// to MulticolorText; setting BMM bit 5 in $D011 transitions to
    /// Bitmap.
    /// </summary>
    [Fact]
    public void RegisterPointers_UseD018ForScreenAndCharacterBases()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);

        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD018, 0x15);

        Assert.Equal(0x0400, vic.ScreenMemoryBase);
        Assert.Equal(0x0400, vic.ScreenMemoryAddress);
        Assert.Equal(0x1000, vic.CharacterBase);
        Assert.Equal(Mos6569.VideoMode.StandardText, vic.DisplayMode);

        vic.Write(0xD016, 0x18);
        Assert.Equal(Mos6569.VideoMode.MulticolorText, vic.DisplayMode);

        vic.Write(0xD011, 0x3B);
        Assert.Equal(Mos6569.VideoMode.Bitmap, vic.DisplayMode);
    }

    /// <summary>
    /// FR: FR-PERF-RUNFRAME-001, FR-VIC-002, FR-VIC-007,
    /// TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: The common C64 PAL frame path renders standard text
    /// without enabled sprites. Its optimized scanline renderer must
    /// remain pixel-exact for the side border, background pixels, and
    /// character foreground bits.
    /// Acceptance: A closed left-border pixel uses $D020, the first
    /// glyph bit uses Color RAM, and the adjacent zero bit uses $D021.
    /// </summary>
    [Fact]
    public void RenderFullFrame_StandardTextNoSprites_RendersBorderBackgroundAndGlyphPixels()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        ConfigureCharacterByte(ram, vic, x: vic.LeftBorderPixel, rasterLine: vic.UpperBorderStart, color: 0x04, charByte: 0x80);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        int x = vic.LeftBorderPixel;
        Assert.Equal(ToBgra(0x03), ReadPixel(renderer.FrameBuffer, x - 1, y));
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x06), ReadPixel(renderer.FrameBuffer, x + 1, y));
    }

    /// <summary>
    /// FR: FR-VIC-002, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Multicolor text cells with Color RAM bit 3 set must
    /// route two-bit character pairs through the VICE color table.
    /// Acceptance: Pair values 00, 01, 10, and 11 render as $D021,
    /// $D022, $D023, and the Color RAM low three bits respectively.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsMulticolorTextPairsFromViceColorTable()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigurePixelModeScreen(vic, d011: 0x18, d016: 0x18, d018: 0x15);
        vic.Write(0xD021, 0x06);
        vic.Write(0xD022, 0x02);
        vic.Write(0xD023, 0x04);
        ram.Write(vic.ScreenMemoryBase, 0x01);
        ram.Write(0xD800, 0x0B);
        ram.Write((ushort)(vic.CharacterBase + 0x08), 0x1B);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        int x = vic.LeftBorderPixel;
        Assert.Equal(ToBgra(0x06), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, x + 2, y));
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x + 4, y));
        Assert.Equal(ToBgra(0x03), ReadPixel(renderer.FrameBuffer, x + 6, y));
    }

    /// <summary>
    /// FR: FR-VIC-002, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Extended Color Mode uses the screen-code upper bits
    /// to pick one of the four background registers while the lower six
    /// bits still select the character glyph.
    /// Acceptance: A screen code with upper bits %10 renders a zero bit
    /// from $D023 and a one bit from Color RAM.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsExtendedColorBackgroundFromScreenCode()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigurePixelModeScreen(vic, d011: 0x58, d016: 0x08, d018: 0x15);
        vic.Write(0xD023, 0x04);
        ram.Write(vic.ScreenMemoryBase, 0x81);
        ram.Write(0xD800, 0x07);
        ram.Write((ushort)(vic.CharacterBase + 0x08), 0x40);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        int x = vic.LeftBorderPixel;
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x07), ReadPixel(renderer.FrameBuffer, x + 1, y));
    }

    /// <summary>
    /// FR: FR-VIC-003, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Standard bitmap mode reads bitmap bytes from the
    /// $D018-selected bitmap base and color pairs from the screen matrix.
    /// Acceptance: A one bit renders the screen byte high nibble; a zero
    /// bit renders the low nibble.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsStandardBitmapFromScreenMatrixNibbles()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigurePixelModeScreen(vic, d011: 0x38, d016: 0x08, d018: 0x18);
        ram.Write(vic.ScreenMemoryBase, 0xA5);
        ram.Write(vic.BitmapPointerBase, 0x80);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        int x = vic.LeftBorderPixel;
        Assert.Equal(ToBgra(0x0A), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 1, y));
    }

    /// <summary>
    /// FR: FR-VIC-003, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Multicolor bitmap mode routes two-bit bitmap pairs
    /// through $D021, screen matrix high nibble, screen matrix low
    /// nibble, and Color RAM.
    /// Acceptance: Pair values 00, 01, 10, and 11 render those four
    /// palette sources in VICE table order.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsMulticolorBitmapFromViceColorTable()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigurePixelModeScreen(vic, d011: 0x38, d016: 0x18, d018: 0x18);
        vic.Write(0xD021, 0x06);
        ram.Write(vic.ScreenMemoryBase, 0xA5);
        ram.Write(0xD800, 0x03);
        ram.Write(vic.BitmapPointerBase, 0x1B);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        int x = vic.LeftBorderPixel;
        Assert.Equal(ToBgra(0x06), ReadPixel(renderer.FrameBuffer, x, y));
        Assert.Equal(ToBgra(0x0A), ReadPixel(renderer.FrameBuffer, x + 2, y));
        Assert.Equal(ToBgra(0x05), ReadPixel(renderer.FrameBuffer, x + 4, y));
        Assert.Equal(ToBgra(0x03), ReadPixel(renderer.FrameBuffer, x + 6, y));
    }

    /// <summary>
    /// FR: FR-VIC-002, FR: FR-VIC-003, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: VICE marks ECM combined with BMM or MCM as COL_NONE
    /// for display-mode output.
    /// Acceptance: A visible-area pixel in an invalid mode renders as
    /// palette index 0 instead of background, screen, bitmap, or Color RAM.
    /// </summary>
    [Theory]
    [InlineData(0x58, 0x18)]
    [InlineData(0x78, 0x08)]
    [InlineData(0x78, 0x18)]
    public void RenderFullFrame_DrawsInvalidDisplayModeAsBlack(byte d011, byte d016)
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigurePixelModeScreen(vic, d011, d016, d018: 0x18);
        vic.Write(0xD021, 0x06);
        ram.Write(vic.ScreenMemoryBase, 0xA5);
        ram.Write(vic.BitmapPointerBase, 0xFF);

        renderer.RenderFullFrame();

        int y = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        Assert.Equal(ToBgra(0x00), ReadPixel(renderer.FrameBuffer, vic.LeftBorderPixel, y));
    }

    /// <summary>
    /// FR: FR-VIC-002, FR: FR-VIC-003, FR: FR-VIC-004, FR: FR-VIC-005,
    /// FR: FR-VIC-008, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: x64sc renders invalid ECM selector colors as COL_NONE
    /// (palette 0) but still uses the hidden graphics pixel priority bit
    /// for $D01B sprite/background priority.
    /// Acceptance: A behind-background sprite stays behind an invalid-mode
    /// foreground pixel even though that pixel is black, and appears over the
    /// adjacent invalid-mode background pixel.
    /// </summary>
    [Theory]
    [InlineData(0x58, 0x18, 0, 1)]
    [InlineData(0x58, 0x18, 1, 2)]
    [InlineData(0x78, 0x08, 2, 1)]
    [InlineData(0x78, 0x18, 3, 2)]
    public void RenderFullFrame_InvalidEcmBlackPixelStillBlocksBehindSpriteWhenForeground(
        byte d011,
        byte d016,
        int sourceKind,
        int firstBackgroundOffset)
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigurePixelModeScreen(vic, d011, d016, d018: sourceKind <= 1 ? (byte)0x15 : (byte)0x18);
        ConfigureInvalidEcmForegroundSource(ram, vic, x: 96, rasterLine: 60, sourceKind);
        ConfigureSprite(
            ram,
            vic,
            sprite: 0,
            x: 96,
            y: 60,
            pointer: 0x20,
            color: 0x02,
            firstRowByte: firstBackgroundOffset == 1 ? (byte)0xC0 : (byte)0xE0);
        vic.Write(0xD015, 0x01);
        vic.Write(0xD01B, 0x01);

        renderer.RenderFullFrame();

        int frameY = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x00), ReadPixel(renderer.FrameBuffer, 96, frameY));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 96 + firstBackgroundOffset, frameY));
    }

    /// <summary>
    /// FR: FR-PERF-RUNFRAME-001, FR-VIC-004, FR-VIC-007,
    /// TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: An enabled opaque sprite must become visible in the
    /// rendered BGRA framebuffer, not only in collision latches. This
    /// also proves standard-text rendering falls back to the sprite
    /// compositor when $D015 enables any sprite.
    /// Acceptance: With sprite 0 enabled at a visible raster coordinate
    /// and its first source bit set, the matching framebuffer pixel uses
    /// sprite 0's colour register instead of the background colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsEnabledSpriteOverBackground()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02);
        vic.Write(0xD015, 0x01);

        renderer.RenderFullFrame();

        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 96, VideoRenderer.RasterLineToFrameY(60)));
    }

    /// <summary>
    /// FR: FR-VIC-004, FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Closed side borders mask sprite output even when an enabled
    /// sprite source bit overlaps the framebuffer pixel.
    /// Acceptance: Sprite 0 placed at x=0 on a visible raster line leaves the
    /// side-border pixel at x=0 in the border colour instead of the sprite
    /// colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_KeepsSpriteBehindClosedSideBorder()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        ConfigureSprite(ram, vic, sprite: 0, x: 0, y: 60, pointer: 0x20, color: 0x02);
        vic.Write(0xD015, 0x01);

        renderer.RenderFullFrame();

        Assert.Equal(ToBgra(0x03), ReadPixel(renderer.FrameBuffer, 0, VideoRenderer.RasterLineToFrameY(60)));
    }

    /// <summary>
    /// FR: FR-VIC-004, FR: FR-VIC-007, TR: TR-VIC-EDGE-002,
    /// TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: VICE x64sc keeps the right side border open when CSEL
    /// changes from 40-column to 38-column mode at PAL cycle 56, so the
    /// framebuffer compositor must allow sprite pixels in that opened border.
    /// Acceptance: Sprite 0 at x=340 renders with its sprite colour after
    /// the cycle-56 CSEL switch instead of remaining masked by border colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsSpriteInOpenedRightSideBorder()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        ConfigureSprite(ram, vic, sprite: 0, x: 340, y: 100, pointer: 0x20, color: 0x02);
        vic.Write(0xD015, 0x01);

        AdvanceTo(vic, 100, 56);
        vic.Write(0xD016, 0x00);
        AdvanceTo(vic, 101, 0);

        renderer.RenderFullFrame();

        Assert.True(vic.IsRasterLineRightBorderOpen(100));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 340, VideoRenderer.RasterLineToFrameY(100)));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: Opening the right side border in VICE suppresses border
    /// drawing; pixels without sprite coverage show the background/idle fill
    /// instead of the border colour.
    /// Acceptance: A right-side pixel opened by the cycle-56 CSEL switch
    /// renders background colour, not border colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsBackgroundInOpenedRightSideBorder()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        vic.Write(0xD021, 0x06);

        AdvanceTo(vic, 100, 56);
        vic.Write(0xD016, 0x00);
        AdvanceTo(vic, 101, 0);

        renderer.RenderFullFrame();

        Assert.True(vic.IsRasterLineRightBorderOpen(100));
        Assert.Equal(ToBgra(0x06), ReadPixel(renderer.FrameBuffer, 340, VideoRenderer.RasterLineToFrameY(100)));
    }

    /// <summary>
    /// FR: FR-VIC-004, FR: FR-VIC-007, TR: TR-VIC-EDGE-002,
    /// TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: A right side-border open on one line carries into the next
    /// line's left border in VICE x64sc, allowing continuous side-border
    /// effects to show sprites at the far left edge.
    /// Acceptance: Sprite 0 at x=0 on the following line renders with sprite
    /// colour instead of border colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsSpriteInCarriedOpenLeftSideBorder()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        ConfigureSprite(ram, vic, sprite: 0, x: 0, y: 101, pointer: 0x20, color: 0x02);
        vic.Write(0xD015, 0x01);

        AdvanceTo(vic, 100, 56);
        vic.Write(0xD016, 0x00);
        AdvanceTo(vic, 102, 0);

        renderer.RenderFullFrame();

        Assert.True(vic.IsRasterLineLeftBorderOpen(101));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 0, VideoRenderer.RasterLineToFrameY(101)));
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-VIC-EDGE-002, TR: TR-CYCLE-001,
    /// TEST: TEST-VIC-001, TODO: BACKFILL-VIDEO-001.
    /// Use case: A right-open side border carries into the next line's left
    /// side border, where VICE suppresses border drawing even without sprite
    /// coverage.
    /// Acceptance: The carried-open left edge renders background colour, not
    /// border colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsBackgroundInCarriedOpenLeftSideBorder()
    {
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        vic.Write(0xD021, 0x06);

        AdvanceTo(vic, 100, 56);
        vic.Write(0xD016, 0x00);
        AdvanceTo(vic, 102, 0);

        renderer.RenderFullFrame();

        Assert.True(vic.IsRasterLineLeftBorderOpen(101));
        Assert.Equal(ToBgra(0x06), ReadPixel(renderer.FrameBuffer, 0, VideoRenderer.RasterLineToFrameY(101)));
    }

    /// <summary>
    /// FR: FR-VIC-004, FR: FR-VIC-007, TR: TR-VIC-EDGE-002,
    /// TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Continuous side-border effects repeat the CSEL timing on
    /// adjacent lines, so the renderer must keep admitting sprite pixels at
    /// the far left edge after multiple carried-open lines.
    /// Acceptance: Sprite 0 at x=0 on the second carried line renders with
    /// sprite colour instead of border colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_DrawsSpriteInContinuousOpenLeftSideBorder()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        ConfigureSprite(ram, vic, sprite: 0, x: 0, y: 102, pointer: 0x20, color: 0x02);
        vic.Write(0xD015, 0x01);

        AdvanceTo(vic, 100, 56);
        vic.Write(0xD016, 0x00);

        AdvanceTo(vic, 101, 55);
        vic.Write(0xD016, 0x08);
        AdvanceTo(vic, 101, 56);
        vic.Write(0xD016, 0x00);

        AdvanceTo(vic, 103, 0);

        renderer.RenderFullFrame();

        Assert.True(vic.IsRasterLineLeftBorderOpen(102));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 0, VideoRenderer.RasterLineToFrameY(102)));
    }

    /// <summary>
    /// FR: FR-VIC-004, FR: FR-VIC-007, TR: TR-VIC-EDGE-002,
    /// TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: Switching CSEL from 38-column to 40-column mode at PAL
    /// cycle 17 leaves the whole line blank in VICE x64sc.
    /// Acceptance: The framebuffer keeps the border colour at a normally
    /// visible sprite coordinate on the blanked line.
    /// </summary>
    [Fact]
    public void RenderFullFrame_KeepsSpriteBehindCycle17CselBlankedLine()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        vic.Write(0xD020, 0x03);
        vic.Write(0xD016, 0x00);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 100, pointer: 0x20, color: 0x02);
        vic.Write(0xD015, 0x01);

        AdvanceTo(vic, 100, 17);
        vic.Write(0xD016, 0x08);
        AdvanceTo(vic, 101, 0);

        renderer.RenderFullFrame();

        Assert.False(vic.IsRasterLineHorizontalDisplayOpen(100));
        Assert.Equal(ToBgra(0x03), ReadPixel(renderer.FrameBuffer, 96, VideoRenderer.RasterLineToFrameY(100)));
    }

    /// <summary>
    /// FR: FR-VIC-004, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: When two opaque sprites overlap, the lower-numbered
    /// sprite has display priority for the visible framebuffer pixel.
    /// Acceptance: Sprites 0 and 1 placed at the same coordinate render
    /// sprite 0's colour at the overlap pixel.
    /// </summary>
    [Fact]
    public void RenderFullFrame_LowerNumberedSpriteWinsWhenSpritesOverlap()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02);
        ConfigureSprite(ram, vic, sprite: 1, x: 96, y: 60, pointer: 0x21, color: 0x04);
        vic.Write(0xD015, 0x03);

        renderer.RenderFullFrame();

        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 96, VideoRenderer.RasterLineToFrameY(60)));
    }

    /// <summary>
    /// FR: FR-VIC-004, FR: FR-VIC-007, TR: TR-CYCLE-001, TEST: TEST-VIC-001.
    /// Use case: $D01B sprite priority places a sprite behind foreground
    /// character pixels while still allowing it to appear over background
    /// pixels.
    /// Acceptance: A behind-background sprite over a foreground character
    /// pixel keeps the character colour; the adjacent background pixel uses
    /// the sprite colour.
    /// </summary>
    [Fact]
    public void RenderFullFrame_SpritePriorityBehindForegroundKeepsCharacterPixel()
    {
        var (bus, ram, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        var renderer = new VideoRenderer(vic, bus);

        ConfigureStandardScreen(vic);
        ConfigureSprite(ram, vic, sprite: 0, x: 96, y: 60, pointer: 0x20, color: 0x02, firstRowByte: 0xC0);
        ConfigureForegroundCharacterPixel(ram, vic, x: 96, rasterLine: 60, color: 0x04);
        vic.Write(0xD015, 0x01);
        vic.Write(0xD01B, 0x01);

        renderer.RenderFullFrame();

        int frameY = VideoRenderer.RasterLineToFrameY(60);
        Assert.Equal(ToBgra(0x04), ReadPixel(renderer.FrameBuffer, 96, frameY));
        Assert.Equal(ToBgra(0x02), ReadPixel(renderer.FrameBuffer, 97, frameY));
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: After Reset, the VIC-II's externally visible raster
    /// position must be at line 0, cycle <c>ResetRasterCycle</c> so
    /// timing-sensitive code starts from a known phase.
    /// Acceptance: <c>CurrentRasterLine == 0</c> and
    /// <c>RasterX == ResetRasterCycle</c>.
    /// </summary>
    [Fact]
    public void VIC_Raster_Position_Is_Zero_After_Reset()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        // Assert
        Assert.Equal(0, vic.CurrentRasterLine);
        Assert.Equal(Mos6569.ResetRasterCycle, vic.RasterX);
    }

    private static uint ReadPixel(byte[] frameBuffer, int x, int y)
    {
        var offset = ((y * VideoRenderer.ScreenWidth) + x) * 4;
        return BitConverter.ToUInt32(frameBuffer, offset);
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    private static void ConfigureStandardScreen(Mos6569 vic)
    {
        vic.Write(0xD011, 0x1B);
        vic.Write(0xD016, 0x08);
        vic.Write(0xD021, 0x06);
    }

    private static void ConfigurePixelModeScreen(Mos6569 vic, byte d011, byte d016, byte d018)
    {
        vic.Write(0xD011, d011);
        vic.Write(0xD016, d016);
        vic.Write(0xD018, d018);
    }

    private static void ConfigureSprite(
        RamDevice ram,
        Mos6569 vic,
        int sprite,
        int x,
        byte y,
        byte pointer,
        byte color,
        byte firstRowByte = 0x80)
    {
        ram.Write((ushort)(vic.ScreenMemoryBase + 0x03F8 + sprite), pointer);
        ram.Write((ushort)(pointer * 64), firstRowByte);
        ram.Write((ushort)(pointer * 64 + 1), 0x00);
        ram.Write((ushort)(pointer * 64 + 2), 0x00);
        vic.Write((ushort)(0xD000 + sprite * 2), (byte)(x & 0xFF));
        byte xMsb = vic.Peek(0xD010);
        xMsb = x >= 0x100
            ? (byte)(xMsb | (1 << sprite))
            : (byte)(xMsb & ~(1 << sprite));
        vic.Write(0xD010, xMsb);
        vic.Write((ushort)(0xD001 + sprite * 2), y);
        vic.Write((ushort)(0xD027 + sprite), color);
    }

    private static void ConfigureForegroundCharacterPixel(RamDevice ram, Mos6569 vic, int x, int rasterLine, byte color)
    {
        ConfigureCharacterByte(ram, vic, x, rasterLine, color, (byte)(1 << (7 - ((x - vic.LeftBorderPixel) % 8))));
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
        int charX = screenX % 8;
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

    private static uint ToBgra(int paletteIndex)
    {
        var color = VicPalette.Colors[paletteIndex & 0x0F];
        return 0xFF000000u | color.B | ((uint)color.G << 8) | ((uint)color.R << 16);
    }
}
