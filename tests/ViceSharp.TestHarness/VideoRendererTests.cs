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

    private static uint ToBgra(int paletteIndex)
    {
        var color = VicPalette.Colors[paletteIndex & 0x0F];
        return 0xFF000000u | color.B | ((uint)color.G << 8) | ((uint)color.R << 16);
    }
}
