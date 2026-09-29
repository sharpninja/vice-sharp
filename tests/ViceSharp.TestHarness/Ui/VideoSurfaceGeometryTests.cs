namespace ViceSharp.TestHarness.Ui;

using ViceSharp.Avalonia;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-HOST-003, FR: FR-1132, FR: FR-VIC20-001.
/// Use case: The Avalonia desktop VideoSurface must present the live video-chip
/// canvas (C64 384x272 or VIC-20 NTSC 400x234 / PAL 448x284), not a hardcoded
/// C64 stride. Copying a 400-pixel row into a 384-pixel bitmap shears the READY
/// screen into diagonal cyan bands.
/// Acceptance: Packed BGRA blit preserves unique per-pixel colors at VIC-20 NTSC
/// geometry; a 384-wide interpretation of the same buffer does not; SetFrame
/// accepts a VIC-20-sized DTO; ComputeDisplayAspect uses the live width.
/// </summary>
public sealed class VideoSurfaceGeometryTests
{
    // Mos6561 NTSC normal-border canvas: display_width 200 * VIC_PIXEL_WIDTH 2,
    // lines 28..261 inclusive.
    private const int Vic20NtscWidth = 400;
    private const int Vic20NtscHeight = 234;

    /// <summary>
    /// FR: FR-HOST-003, FR: FR-VIC20-001.
    /// Acceptance: Blitting a 400x234 packed BGRA buffer into a destination whose
    /// stride equals 400*4 leaves every unique pixel at (x,y) unchanged.
    /// </summary>
    [Fact]
    public void BlitPackedBgra_Vic20Ntsc_PreservesPixelsWhenDestStrideMatches()
    {
        var packed = BuildUniqueFrame(Vic20NtscWidth, Vic20NtscHeight);
        var dest = new byte[Vic20NtscWidth * Vic20NtscHeight * 4];

        VideoSurface.BlitPackedBgra(packed, Vic20NtscWidth, Vic20NtscHeight, dest, Vic20NtscWidth * 4);

        Assert.Equal(packed, dest);
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 0, 0), PixelAt(dest, Vic20NtscWidth, 0, 0));
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 399, 0), PixelAt(dest, Vic20NtscWidth, 399, 0));
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 0, 1), PixelAt(dest, Vic20NtscWidth, 0, 1));
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 16, 1), PixelAt(dest, Vic20NtscWidth, 16, 1));
    }

    /// <summary>
    /// FR: FR-HOST-003, FR: FR-VIC20-001.
    /// Acceptance: Blitting into a padded bitmap row (stride &gt; width*4) still
    /// places source (x,y) at dest (x,y); padding bytes are not consumed as pixels.
    /// </summary>
    [Fact]
    public void BlitPackedBgra_Vic20Ntsc_PreservesPixelsWhenDestStrideIsPadded()
    {
        const int destStridePixels = Vic20NtscWidth + 16;
        var packed = BuildUniqueFrame(Vic20NtscWidth, Vic20NtscHeight);
        var dest = new byte[destStridePixels * Vic20NtscHeight * 4];

        VideoSurface.BlitPackedBgra(packed, Vic20NtscWidth, Vic20NtscHeight, dest, destStridePixels * 4);

        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 0, 0), PixelAt(dest, destStridePixels, 0, 0));
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 399, 0), PixelAt(dest, destStridePixels, 399, 0));
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 0, 1), PixelAt(dest, destStridePixels, 0, 1));
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 16, 1), PixelAt(dest, destStridePixels, 16, 1));
    }

    /// <summary>
    /// FR: FR-HOST-003, FR: FR-VIC20-001.
    /// Use case: The pre-fix Avalonia path copied a 400-wide packed canvas into a
    /// 384-wide bitmap. That must NOT preserve (0,1) because 16 leftover pixels
    /// from row 0 spill into row 1.
    /// Acceptance: Reading the packed 400x234 buffer as 384-wide shears: dest(0,1)
    /// equals source(16,0), not source(0,1).
    /// </summary>
    [Fact]
    public void InterpretingVic20NtscAsC64Stride_ShearsRowStart()
    {
        var packed = BuildUniqueFrame(Vic20NtscWidth, Vic20NtscHeight);
        const int c64Width = VideoSurface.SourceWidth;
        var sheared = packed.AsSpan(0, c64Width * Vic20NtscHeight * 4);

        Assert.NotEqual(PixelAt(packed, Vic20NtscWidth, 0, 1), PixelAt(sheared, c64Width, 0, 1));
        // 400-wide row 0 leftover after 384 dest pixels starts dest row 1 at source x=384.
        Assert.Equal(PixelAt(packed, Vic20NtscWidth, 384, 0), PixelAt(sheared, c64Width, 0, 1));
    }

    /// <summary>
    /// FR: FR-HOST-003, FR: FR-VIC20-001.
    /// Acceptance: SetFrame's validation predicate accepts a VIC-20 NTSC DTO
    /// (400x234, exact BGRA length) instead of requiring C64 384x272.
    /// </summary>
    [Fact]
    public void IsValidFrame_AcceptsVic20NtscGeometry()
    {
        var frame = new VideoFrameDto(
            Vic20NtscWidth,
            Vic20NtscHeight,
            Cycle: 1,
            Bgra: new byte[Vic20NtscWidth * Vic20NtscHeight * 4]);

        Assert.True(VideoSurface.IsValidFrame(frame));
    }

    /// <summary>
    /// FR: FR-HOST-003.
    /// Acceptance: A DTO whose payload is shorter than Width*Height*4 is rejected
    /// even when Width/Height are the C64 defaults.
    /// </summary>
    [Fact]
    public void IsValidFrame_RejectsTruncatedPayloadForClaimedSize()
    {
        var frame = new VideoFrameDto(
            Vic20NtscWidth,
            Vic20NtscHeight,
            Cycle: 0,
            Bgra: new byte[Vic20NtscWidth * Vic20NtscHeight * 4 - 1]);

        Assert.False(VideoSurface.IsValidFrame(frame));
        Assert.False(VideoSurface.IsValidFrame(null));
        Assert.False(VideoSurface.IsValidFrame(new VideoFrameDto(0, Vic20NtscHeight, 0, new byte[4])));
        Assert.False(VideoSurface.IsValidFrame(new VideoFrameDto(Vic20NtscWidth, 0, 0, new byte[4])));
    }

    /// <summary>
    /// FR: FR-HOST-003, FR: FR-VIC20-001, FIX-XASPECT-002.
    /// Acceptance: Square-pixel aspect for a 400x234 canvas is 400/234, not 384/234.
    /// </summary>
    [Fact]
    public void ComputeDisplayAspect_Vic20NtscSquarePixels_UsesLiveWidth()
    {
        var aspect = VideoSurface.ComputeDisplayAspect("Square pixels", pixelAspect: 1.0, Vic20NtscHeight, Vic20NtscWidth);

        Assert.Equal((double)Vic20NtscWidth / Vic20NtscHeight, aspect, precision: 10);
        Assert.NotEqual((double)VideoSurface.SourceWidth / Vic20NtscHeight, aspect, precision: 10);
    }

    private static byte[] BuildUniqueFrame(int width, int height)
    {
        var buffer = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                buffer[i] = (byte)x;
                buffer[i + 1] = (byte)y;
                buffer[i + 2] = (byte)(x ^ y);
                buffer[i + 3] = 0xFF;
            }
        }

        return buffer;
    }

    private static uint PixelAt(ReadOnlySpan<byte> bgra, int stridePixels, int x, int y)
    {
        var i = (y * stridePixels + x) * 4;
        return (uint)(bgra[i] | (bgra[i + 1] << 8) | (bgra[i + 2] << 16) | (bgra[i + 3] << 24));
    }
}
