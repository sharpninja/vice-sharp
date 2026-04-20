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

    [Fact]
    public void FrameBuffer_Contains_Blue_For_Border()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        // Blue in BGRA format from VicPalette - Blue index 6 = RGB(0x1B, 0x1B, 0x8E)
        // BGRA: 0xFF1B1B8E (Alpha=FF, B=0x8E, G=0x1B, R=0x1B)
        var renderer = new VideoRenderer(vic, bus);
        
        // Act - Render full frame with border
        renderer.RenderFullFrame();
        
        // Assert - Check first pixel is blue
        uint firstPixel = BitConverter.ToUInt32(renderer.FrameBuffer, 0);
        uint expectedBlue = 0xFF1B1B8E;
        
        Assert.Equal(expectedBlue, firstPixel);
    }

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

    [Fact]
    public void ScreenDimensions_Are_Correct()
    {
        // Assert standard C64 screen dimensions (overscan)
        Assert.Equal(384, VideoRenderer.ScreenWidth);
        Assert.Equal(272, VideoRenderer.ScreenHeight);
    }

    [Fact]
    public void PAL_Timing_Is_Correct()
    {
        // Assert PAL timing values
        Assert.Equal(63, VideoRenderer.PalCyclesPerLine);
        Assert.Equal(312, VideoRenderer.PalTotalLines);
        Assert.Equal(272, VideoRenderer.PalVisibleLines);
    }

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

    [Fact]
    public void BorderColor_Is_Blue_After_Reset()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        // Assert - Default border color is index 6 (blue)
        Assert.Equal(6, vic.BorderColor);
    }

    [Fact]
    public void BackgroundColor_Is_DarkGray_After_Reset()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        // Assert - Default background color is index 11 (dark gray)
        Assert.Equal(11, vic.BackgroundColor);
    }

    [Fact]
    public void VIC_Raster_Position_Is_Zero_After_Reset()
    {
        // Arrange
        var (bus, _, irq) = CreateTestMachine();
        var vic = new Mos6569(bus, irq);
        vic.Reset();
        
        // Assert
        Assert.Equal(0, vic.CurrentRasterLine);
        Assert.Equal(0, vic.RasterX);
    }
}
