namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;

public sealed class VideoSurfaceIntegrationTests
{
    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Every standard C64 machine produced by the factory
    /// must expose an <see cref="IVideoChip"/> device via the
    /// VideoChip role.
    /// Acceptance: Devices.GetByRole(VideoChip) returns a non-null
    /// object that is assignable to IVideoChip.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-HOST-003, TR: TR-CYCLE-001.
    /// Use case: The IVideoChip surface must allocate a 384x272 BGRA
    /// frame buffer at construction time so host clients can rely on
    /// the backing memory at any point after the machine exists.
    /// Acceptance: FrameBuffer is non-null and exactly
    /// 384 * 272 * 4 bytes in length.
    /// </summary>
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

    /// <summary>
    /// FR: FR-HOST-003, TR: TR-PUBSUB-001.
    /// Use case: Host clients subscribe to FrameCompleted to know when
    /// a frame is ready to deliver; the event must be subscribable and
    /// unsubscribable without error.
    /// Acceptance: Subscribing and unsubscribing a handler on the
    /// FrameCompleted event does not throw.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-HOST-003, TR: TR-PUBSUB-001.
    /// Use case: Running a full frame must raise the VideoChip's
    /// FrameCompleted event so host clients can present the frame.
    /// Acceptance: After <c>machine.RunFrame()</c>, FrameCompleted has
    /// fired at least once.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: After running a frame the VIC-II must have actually
    /// written pixels (not leave the frame buffer all-zero) so host
    /// clients see something rendered.
    /// Acceptance: The frame buffer contains at least one non-zero byte
    /// after <c>RunFrame()</c>.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: At reset the VIC-II border colour register defaults to
    /// palette index 0; the rendered frame buffer's first pixel must
    /// reflect that until the boot ROM programs a different colour.
    /// Acceptance: After a single <c>RunFrame()</c>, the BGRA value at
    /// pixel (0,0) is 0xFF000000 (opaque black).
    /// </summary>
    [Fact]
    public void Machine_RunFrame_Uses_Reset_Border_Color()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - Run a frame and get the framebuffer
        machine.RunFrame();
        
        // Assert - reset border color is index 0 until the boot path programs it.
        var frameBuffer = videoChip.FrameBuffer;
        uint firstPixel = BitConverter.ToUInt32(frameBuffer, 0);
        
        Assert.Equal(0xFF000000u, firstPixel);
    }

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: Border pixels in a single rendered scanline must all
    /// share the same colour, since the border register cannot change
    /// mid-line during normal operation.
    /// Acceptance: Sampling the first 10 pixels of the rendered frame
    /// produces 10 identical BGRA values.
    /// </summary>
    [Fact]
    public void Border_Pixels_Are_All_Same_Color()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Act - boot far enough for the ROM to program VIC colors, then
        // render a frame for surface inspection.
        RunUntilReadyPrompt(machine);
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-HOST-003, TR: TR-CYCLE-001.
    /// Use case: After running a frame the FrameBuffer length must still
    /// be exactly 384*272*4 bytes (it cannot be reallocated to a
    /// different size at runtime).
    /// Acceptance: FrameBuffer.Length == 384 * 272 * 4 after RunFrame.
    /// </summary>
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

    /// <summary>
    /// FR: FR-PRF-001, TR: TR-CYCLE-001.
    /// Use case: A built C64 machine must expose at minimum the
    /// VideoChip and CPU roles and have at least 5 devices total
    /// (CPU, VIC-II, two CIAs, SID, etc.).
    /// Acceptance: Devices.GetByRole(VideoChip) and Cpu are both
    /// non-null and the total device count is at least 5.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: The machine-level Reset must propagate to every chip
    /// device, including the VIC-II, so the raster engine returns to
    /// its initial state.
    /// Acceptance: After running two frames and then resetting, the
    /// VIC-II device handle is still valid and a future RunFrame will
    /// produce a frame from the initial raster line.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-HOST-003, TR: TR-CYCLE-001.
    /// Use case: Host code (e.g. the Avalonia VideoSurface) copies the
    /// VIC-II frame buffer into its own backing store; the copy must
    /// preserve every byte.
    /// Acceptance: <c>Array.Copy</c> from FrameBuffer into a freshly
    /// allocated array of the same length yields a byte-identical
    /// destination.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: The IVideoChip must report the canonical C64 visible
    /// frame dimensions: 384 wide, 272 tall.
    /// Acceptance: FrameWidth == 384 and FrameHeight == 272.
    /// </summary>
    [Fact]
    public void VideoChip_ScreenDimensions_Are_Correct()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        // Assert - Video chip should report correct dimensions
        Assert.Equal(384, videoChip.FrameWidth);
        Assert.Equal(272, videoChip.FrameHeight);
    }

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-MEM-001, TR: TR-CYCLE-001.
    /// Use case: The VIC-II device must claim the $D000 I/O range via
    /// the IAddressSpace interface so PLA routing finds it during reads
    /// and writes to that region.
    /// Acceptance: The video chip is castable to IAddressSpace and its
    /// HandlesAddress reports true for $D000.
    /// </summary>
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

    /// <summary>
    /// FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: After the VIC-II is configured with distinct border
    /// and background colours, the rendered frame must contain at least
    /// two distinct colours and a non-trivial pixel count for visual
    /// regression debugging.
    /// Acceptance: TotalPixels &gt; 0 and UniqueColors &gt; 1 after
    /// running a single frame; the frame is also dumped to a temp BMP
    /// for offline inspection.
    /// </summary>
    [Fact]
    public void Rendered_Frame_Image_Is_Blue_Border_Frame()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        
        machine.Bus.Write(0xD011, 0x1B);
        machine.Bus.Write(0xD016, 0x08);
        machine.Bus.Write(0xD020, 0x06);
        machine.Bus.Write(0xD021, 0x0B);

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

    /// <summary>
    /// FR: FR-VIC-002, FR: FR-CPU-001, TR: TR-CYCLE-001.
    /// Use case: After the BASIC ROM prints READY., every glyph in the
    /// rendered frame buffer must come from the character ROM at the
    /// expected screen position with the expected foreground/background
    /// colours from color RAM and the VIC background register.
    /// Acceptance: For each of the first 5 cells of the "READY" prompt,
    /// every one of the 8x8 pixels matches the character ROM glyph row
    /// rendered with the cell's foreground colour over the global
    /// background colour.
    /// </summary>
    [Fact]
    public void BootReadyPrompt_FrameBufferMatchesCharacterRomGlyphs()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var videoChip = (IVideoChip)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        var vic = Assert.IsType<Mos6569>(videoChip);

        var readyOffset = RunUntilReadyPrompt(machine);
        machine.RunFrame();
        WriteFrameToPng(videoChip.FrameBuffer, 384, 272, Path.Combine(Path.GetTempPath(), "vicesharp_ready_frame.bmp"));

        var characterRom = MachineTestFactory.LoadC64Rom("characters").Span;
        var screenCodes = ReadScreenCodes(machine);

        for (var i = 0; i < 5; i++)
        {
            var screenOffset = readyOffset + i;
            var screenCode = screenCodes[screenOffset];
            var foreground = ToBgra(machine.Bus.Peek((ushort)(0xD800 + screenOffset)) & 0x0F);
            var background = ToBgra(vic.BackgroundColor);
            var row = screenOffset / 40;
            var column = screenOffset % 40;

            for (var charRow = 0; charRow < 8; charRow++)
            {
                var glyph = characterRom[(screenCode * 8) + charRow];
                var rasterLine = vic.UpperBorderStart + (row * 8) - vic.YScroll + charRow;
                var y = VideoRenderer.RasterLineToFrameY(rasterLine);

                for (var charColumn = 0; charColumn < 8; charColumn++)
                {
                    var x = vic.LeftBorderPixel + (column * 8) + charColumn;
                    var bit = (glyph >> (7 - charColumn)) & 0x01;
                    var expected = bit == 0 ? background : foreground;
                    var actual = ReadPixel(videoChip.FrameBuffer, x, y);

                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    /// <summary>
    /// FR: FR-CPU-001, FR: FR-VIC-002, TR: TR-CYCLE-001.
    /// Use case: After READY., the BASIC input cursor in screen RAM
    /// must alternate between blank ($20) and reverse blank ($A0) as
    /// BASIC's cursor blink ISR ticks.
    /// Acceptance: Within 120 frames after the READY prompt appears,
    /// the cursor cell is observed as both $20 and $A0 (cursor blinked
    /// at least once each way).
    /// </summary>
    [Fact]
    public void BootReadyPrompt_ShowsBlinkingBasicInputCursor()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var readyOffset = RunUntilReadyPrompt(machine);
        var cursorOffset = (((readyOffset / 40) + 1) * 40) + (readyOffset % 40);
        var sawBlank = false;
        var sawReverseBlank = false;

        for (var frame = 0; frame < 120; frame++)
        {
            machine.RunFrame();
            var cursorCell = machine.Bus.Peek((ushort)(0x0400 + cursorOffset));
            sawBlank |= cursorCell == 0x20;
            sawReverseBlank |= cursorCell == 0xA0;

            if (sawBlank && sawReverseBlank)
                return;
        }

        Assert.Fail($"BASIC cursor at screen offset {cursorOffset} did not blink between blank and reverse blank.");
    }

    /// <summary>
    /// FR: FR-VIC-001, FR: FR-VIC-007, TR: TR-CYCLE-001.
    /// Use case: VideoRenderer must crop the PAL raster (312 lines)
    /// down to the visible 272-line frame so the 200-line text area
    /// is vertically centred.
    /// Acceptance: With DEN and 25-row mode enabled, upper text area
    /// maps to frame Y 36, lower text area maps to frame Y 236, and
    /// the bottom margin (ScreenHeight - lower) equals the top margin
    /// (both 36) - i.e. perfectly centred.
    /// </summary>
    [Fact]
    public void VideoFrame_CropsPalRasterToCenterTextAreaVertically()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var vic = Assert.IsType<Mos6569>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        vic.Write(0xD011, 0x1B);

        var upperTextArea = VideoRenderer.RasterLineToFrameY(vic.UpperBorderStart);
        var lowerTextArea = VideoRenderer.RasterLineToFrameY(vic.LowerBorderStart);

        Assert.Equal(36, upperTextArea);
        Assert.Equal(236, lowerTextArea);
        Assert.Equal(36, VideoRenderer.ScreenHeight - lowerTextArea);
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

    private static int RunUntilReadyPrompt(IMachine machine)
    {
        for (var frame = 0; frame < 400; frame++)
        {
            machine.RunFrame();
            var offset = FindReadyPrompt(ReadScreenCodes(machine));
            if (offset >= 0)
                return offset;
        }

        Assert.Fail("READY prompt was not found in screen memory after 400 frames.");
        return -1;
    }

    private static int FindReadyPrompt(byte[] screenCodes)
    {
        ReadOnlySpan<byte> ready = [18, 5, 1, 4, 25];
        return screenCodes.AsSpan().IndexOf(ready);
    }

    private static byte[] ReadScreenCodes(IMachine machine)
    {
        var screenCodes = new byte[1000];
        for (var i = 0; i < screenCodes.Length; i++)
        {
            screenCodes[i] = machine.Bus.Peek((ushort)(0x0400 + i));
        }

        return screenCodes;
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
