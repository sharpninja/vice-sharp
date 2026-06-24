namespace ViceSharp.TestHarness;

using System;
using ViceSharp.Core.Media;
using Xunit;

/// <summary>
/// FR-MEDOVL-001 / TEST-MEDOVL-001. The capture stats overlay draws legible text into a BGRA
/// frame: a translucent dark backing strip darkens the covered pixels and the glyph foreground
/// is opaque white, while pixels outside the overlay box are untouched. This lets a recorded
/// clip carry live emulation stats for debugging without any GDI/Skia dependency.
/// </summary>
public sealed class StatsOverlayRendererTests
{
    private const int Width = 64;
    private const int Height = 16;

    private static byte[] GrayFrame(byte level)
    {
        var buf = new byte[Width * Height * 4];
        Array.Fill(buf, level);
        return buf;
    }

    /// <summary>
    /// FR: FR-MEDOVL-001, TR: TR-MED-OVL-001, TEST-MEDOVL-001.
    /// Use case: the overlay must render a glyph as opaque white pixels so the burned-in stats
    ///   are readable on playback.
    /// Acceptance: drawing "1" sets a known glyph pixel to white (BGRA 255,255,255,255).
    /// </summary>
    [Fact]
    public void Draw_SetsGlyphForegroundWhite()
    {
        var frame = GrayFrame(100);

        StatsOverlayRenderer.Draw(frame, Width, Height, new[] { "1" }, scale: 1);

        // '1' row 0 is 00100 -> leftmost-set pixel at glyph column 2; origin pad = 2.
        var i = ((2 + 0) * Width + (2 + 2)) * 4;
        Assert.Equal(255, frame[i]);
        Assert.Equal(255, frame[i + 1]);
        Assert.Equal(255, frame[i + 2]);
        Assert.Equal(255, frame[i + 3]);
    }

    /// <summary>
    /// FR: FR-MEDOVL-001, TR: TR-MED-OVL-001, TEST-MEDOVL-001.
    /// Use case: the backing strip must darken its area (so text contrasts) but leave the rest of
    ///   the frame - the actual video - untouched.
    /// Acceptance: a covered background pixel is darkened toward black; a pixel outside the
    ///   overlay box keeps its original value.
    /// </summary>
    [Fact]
    public void Draw_DarkensBackingBox_LeavesRestUntouched()
    {
        var frame = GrayFrame(100);

        StatsOverlayRenderer.Draw(frame, Width, Height, new[] { "1" }, scale: 1);

        // Inside the box but off the glyph (top-left corner): darkened from 100.
        var inside = ((0 * Width) + 0) * 4;
        Assert.True(frame[inside] < 100, "backing box should darken covered pixels");

        // Far bottom-right is outside the small overlay box: unchanged.
        var outside = ((Height - 1) * Width + (Width - 1)) * 4;
        Assert.Equal(100, frame[outside]);
    }

    /// <summary>
    /// FR: FR-MEDOVL-001, TR: TR-MED-OVL-001, TEST-MEDOVL-001.
    /// Use case: the overlay is called from the hot capture path; bad inputs must be ignored, not
    ///   throw onto the emulation worker.
    /// Acceptance: empty lines, zero scale, or an undersized buffer are no-ops (no throw, no
    ///   pixel change).
    /// </summary>
    [Fact]
    public void Draw_GuardsBadInput()
    {
        var frame = GrayFrame(100);
        StatsOverlayRenderer.Draw(frame, Width, Height, Array.Empty<string>(), scale: 1);
        StatsOverlayRenderer.Draw(frame, Width, Height, new[] { "1" }, scale: 0);
        Assert.All(frame, b => Assert.Equal(100, b));

        var tooSmall = new byte[8];
        StatsOverlayRenderer.Draw(tooSmall, Width, Height, new[] { "1" }, scale: 1); // no throw
    }
}
