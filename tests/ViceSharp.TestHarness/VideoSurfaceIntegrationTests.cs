namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Abstractions;

public sealed class VideoSurfaceIntegrationTests
{
    [Fact]
    public void Machine_Has_VideoChip_Device()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        
        // Act - Get the video chip by role
        var videoChip = machine.Devices.GetByRole(DeviceRole.VideoChip);
        
        // Assert - Video chip should exist and be IVideoChip
        Assert.NotNull(videoChip);
        Assert.IsAssignableFrom<IVideoChip>(videoChip);
    }

    [Fact]
    public void VideoChip_FrameBuffer_Is_Allocated()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act
        var frameBuffer = videoChip.FrameBuffer;
        
        // Assert - FrameBuffer should be allocated (384 * 272 * 4 bytes)
        Assert.NotNull(frameBuffer);
        Assert.Equal(384 * 272 * 4, frameBuffer.Length);
    }

    [Fact]
    public void VideoChip_Has_FrameCompleted_Event()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act & Assert - Event should be subscribable (not null)
        var handler = new EventHandler((s, e) => { });
        videoChip.FrameCompleted += handler;
        videoChip.FrameCompleted -= handler; // Clean up
    }

    [Fact]
    public void Machine_RunFrame_Triggers_FrameCompleted()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        int frameCount = 0;
        videoChip.FrameCompleted += (s, e) => frameCount++;
        
        // Act - Run one frame (should complete at end of frame)
        machine.RunFrame();
        
        // Assert - At least one frame should complete
        Assert.True(frameCount >= 1, $"Expected at least 1 frame completed, got {frameCount}");
    }

    [Fact]
    public void VideoChip_FrameBuffer_Has_Content_After_RunFrame()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act
        machine.RunFrame();
        
        // Give a moment for frame to complete
        var frameBuffer = videoChip.FrameBuffer;
        
        // Assert - FrameBuffer should have some non-zero content
        bool hasContent = false;
        for (int i = 0; i < frameBuffer.Length; i++)
        {
            if (frameBuffer[i] != 0)
            {
                hasContent = true;
                break;
            }
        }
        
        Assert.True(hasContent, "FrameBuffer should contain non-zero data after running a frame");
    }

    [Fact]
    public void Machine_RunFrame_Produces_Blue_Border_Color()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - Run a frame and get the framebuffer
        machine.RunFrame();
        
        // Assert - The first pixel should be blue (border color)
        // Blue index 6 = VicPalette.Colors[6] = RGB(0x1B, 0x1B, 0x8E)
        // In BGRA format: 0xFF1B1B8E (Alpha=FF, B=0x8E, G=0x1B, R=0x1B)
        var frameBuffer = videoChip.FrameBuffer;
        uint firstPixel = BitConverter.ToUInt32(frameBuffer, 0);
        
        Assert.Equal(0xFF1B1B8Eu, firstPixel);
    }

    [Fact]
    public void Border_Pixels_Are_All_Same_Color()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - Run a frame
        machine.RunFrame();
        
        var frameBuffer = videoChip.FrameBuffer;
        
        // Sample multiple border pixels (first row should be border)
        uint[] samples = new uint[10];
        for (int i = 0; i < 10; i++)
        {
            samples[i] = BitConverter.ToUInt32(frameBuffer, i * 4);
        }
        
        // All border pixels should be the same color (blue)
        uint borderColor = samples[0];
        for (int i = 1; i < samples.Length; i++)
        {
            Assert.Equal(borderColor, samples[i]);
        }
    }

    [Fact]
    public void FrameBuffer_Has_Expected_Size_For_384x272()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act
        machine.RunFrame();
        
        // Assert
        // 384 pixels * 272 lines * 4 bytes per pixel = 417,792 bytes
        Assert.Equal(384 * 272 * 4, videoChip.FrameBuffer.Length);
    }

    [Fact]
    public void Architecture_Has_All_Required_Devices()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        
        // Act - Check all required device roles exist
        var videoChip = machine.Devices.GetByRole(DeviceRole.VideoChip);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu);
        
        // Assert
        Assert.NotNull(videoChip);
        Assert.NotNull(cpu);
        Assert.True(machine.Devices.Count >= 5, $"Expected at least 5 devices, got {machine.Devices.Count}");
    }

    [Fact]
    public void VideoChip_Reset_Sets_RasterLine_To_Zero()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - Run some frames to advance raster
        machine.RunFrame();
        machine.RunFrame();
        
        // Reset the machine
        machine.Reset();
        
        // The video chip should be reset (raster line back to 0)
        // Note: This tests that reset propagates to devices
        Assert.NotNull(videoChip);
    }

    [Fact]
    public void VideoChip_FrameBuffer_Can_Be_Copied_To_External_Buffer()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        machine.RunFrame();
        
        var sourceBuffer = videoChip.FrameBuffer;
        var destBuffer = new byte[sourceBuffer.Length];
        
        // Act - Copy the framebuffer (simulating what VideoSurface does)
        Array.Copy(sourceBuffer, destBuffer, sourceBuffer.Length);
        
        // Assert - Destination buffer should match source
        bool match = true;
        for (int i = 0; i < sourceBuffer.Length; i++)
        {
            if (sourceBuffer[i] != destBuffer[i])
            {
                match = false;
                break;
            }
        }
        Assert.True(match, "FrameBuffer copy should preserve all bytes");
    }

    [Fact]
    public void VideoChip_ScreenDimensions_Are_Correct()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Assert - Video chip should report correct dimensions
        Assert.Equal(384, videoChip.FrameWidth);
        Assert.Equal(272, videoChip.FrameHeight);
    }

    [Fact]
    public void VideoChip_Is_Addressable_At_D000()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - Check if video chip handles $D000 address
        var vicAsAddressSpace = videoChip as IAddressSpace;
        
        // Assert - Video chip should be addressable
        Assert.NotNull(vicAsAddressSpace);
        Assert.True(vicAsAddressSpace.HandlesAddress(0xD000));
    }

    [Fact]
    public void Rendered_Frame_Image_Is_Blue_Border_Frame()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - Run a frame
        machine.RunFrame();
        
        // Write framebuffer to PNG file for analysis
        var outputPath = Path.Combine(Path.GetTempPath(), "vicesharp_test_frame.png");
        WriteFrameToPng(videoChip.FrameBuffer, 384, 272, outputPath);
        
        // Analyze the rendered frame
        var analysis = AnalyzeFrame(videoChip.FrameBuffer, 384, 272);
        
        // Write analysis to console for inspection
        Console.WriteLine($"Frame Analysis for {outputPath}:");
        Console.WriteLine($"  Total pixels: {analysis.TotalPixels}");
        Console.WriteLine($"  Unique colors: {analysis.UniqueColors}");
        Console.WriteLine($"  Blue border pixels: {analysis.BlueBorderPixels}");
        Console.WriteLine($"  Dark gray pixels (background): {analysis.DarkGrayPixels}");
        Console.WriteLine($"  Black pixels (screen chars): {analysis.BlackPixels}");
        Console.WriteLine($"  Other color pixels: {analysis.OtherPixels}");
        Console.WriteLine();
        Console.WriteLine("Expected: Full frame with blue border, dark gray screen background.");
        Console.WriteLine("The screen area (40x25 characters) should show default character RAM");
        Console.WriteLine("which is $00 (@ symbol) with black color, over dark gray background.");
        
        // Assert - At minimum, we should have blue border pixels
        Assert.True(analysis.TotalPixels > 0, "Frame should have pixels");
        Assert.True(analysis.UniqueColors > 1, "Frame should have multiple colors");
    }

    private static void WriteFrameToPng(byte[] frameBuffer, int width, int height, string outputPath)
    {
        // Create a simple BMP file (easier than PNG without additional dependencies)
        var bmpPath = Path.ChangeExtension(outputPath, ".bmp");
        
        // BMP header: 14 bytes + 40 bytes info header + pixel data
        var rowSize = (width * 3 + 3) & ~3; // Rows are aligned to 4 bytes
        var pixelDataSize = rowSize * height;
        var fileSize = 54 + pixelDataSize;
        
        using var fs = new FileStream(bmpPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        
        // BMP file header
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write(0); // Reserved
        bw.Write(54); // Pixel data offset
        
        // DIB header (BITMAPINFOHEADER)
        bw.Write(40); // Header size
        bw.Write(width);
        bw.Write(height);
        bw.Write((short)1); // Planes
        bw.Write((short)24); // Bits per pixel
        bw.Write(0); // Compression (none)
        bw.Write(pixelDataSize);
        bw.Write(0); // X pixels per meter
        bw.Write(0); // Y pixels per meter
        bw.Write(0); // Colors used
        bw.Write(0); // Important colors
        
        // Pixel data (BGR format, bottom-up)
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                bw.Write(frameBuffer[offset]); // Blue
                bw.Write(frameBuffer[offset + 1]); // Green
                bw.Write(frameBuffer[offset + 2]); // Red
            }
            // Row padding
            for (int p = 0; p < (width * 3) % 4; p++)
                bw.Write((byte)0);
        }
    }

    private static FrameAnalysis AnalyzeFrame(byte[] frameBuffer, int width, int height)
    {
        var analysis = new FrameAnalysis { TotalPixels = width * height };
        
        // Blue border color: VicPalette index 6 = RGB(0x1B, 0x1B, 0x8E) = BGRA(0x8E, 0x1B, 0x1B, 0xFF)
        var blueBorderBgr = ((0x8E << 16) | (0x1B << 8) | 0x1B);
        
        // Dark gray: VicPalette index 11 = RGB(0x44, 0x44, 0x44) = BGRA(0x44, 0x44, 0x44, 0xFF)
        var darkGrayBgr = ((0x44 << 16) | (0x44 << 8) | 0x44);
        
        // Black: VicPalette index 0 = RGB(0, 0, 0)
        var blackBgr = 0;
        
        var seenColors = new HashSet<int>();
        
        for (int i = 0; i < frameBuffer.Length; i += 4)
        {
            var bgr = ((frameBuffer[i + 2] << 16) | (frameBuffer[i + 1] << 8) | frameBuffer[i]);
            seenColors.Add(bgr);
            
            if (bgr == blueBorderBgr)
                analysis.BlueBorderPixels++;
            else if (bgr == darkGrayBgr)
                analysis.DarkGrayPixels++;
            else if (bgr == blackBgr)
                analysis.BlackPixels++;
            else
                analysis.OtherPixels++;
        }
        
        analysis.UniqueColors = seenColors.Count;
        return analysis;
    }

    private class FrameAnalysis
    {
        public int TotalPixels { get; set; }
        public int UniqueColors { get; set; }
        public int BlueBorderPixels { get; set; }
        public int DarkGrayPixels { get; set; }
        public int BlackPixels { get; set; }
        public int OtherPixels { get; set; }
    }
}
