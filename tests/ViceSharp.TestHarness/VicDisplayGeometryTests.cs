namespace ViceSharp.TestHarness;

using System;
using System.Collections.Generic;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC-002 / TR-VIC-EDGE-004: PAL vertical display geometry. The rendered
/// visible frame must band as top border / screen / bottom border so the
/// screen is framed (centered) rather than bleeding to a frame edge. Guards
/// against the "Y-offset" symptom where the top or bottom border fails to
/// render and the screen background runs off the top/bottom of the picture.
/// </summary>
public sealed class VicDisplayGeometryTests
{
    private const ushort ScreenControl1 = 0xD011;

    /// <summary>
    /// FR-VIC-002 / TR-VIC-EDGE-004.
    /// Use case: after the C64 boots to the steady READY screen the rendered PAL
    ///   frame shows the screen background framed by border on BOTH the top and
    ///   the bottom, i.e. the classic centered display.
    /// Acceptance: a row deep in the top border and a row deep in the bottom border
    ///   resolve to the same solid colour, and a mid-screen row resolves to a
    ///   different solid colour (the screen background).
    /// </summary>
    [Fact]
    public void C64_ReadyScreen_Frame_HasSymmetricTopAndBottomBorderBands()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        // Boot to the steady READY screen so the display window is open and stable.
        for (var frame = 0; frame < 400 && !ContainsReady(machine); frame++)
            machine.RunFrame();

        Assert.True(ContainsReady(machine), "C64 did not reach the READY screen within 400 frames.");

        // A few more frames so the framebuffer holds a fully-rendered steady frame.
        for (var i = 0; i < 3; i++)
            machine.RunFrame();

        var vic = machine.Devices.All.OfType<IVideoChip>().First();
        var fb = vic.FrameBuffer;
        int width = vic.FrameWidth;     // PAL visible: 384
        int height = vic.FrameHeight;   // PAL visible: 272

        // PAL visible window (DEN=1, RSEL=1): ~36 top-border rows, the 200-line
        // screen, then ~36 bottom-border rows. Sample well inside each band.
        uint topBorder = DominantRowColor(fb, width, 8);
        uint screen = DominantRowColor(fb, width, height / 2);
        uint bottomBorder = DominantRowColor(fb, width, height - 8);

        // The bottom border must render exactly like the top one. The reported
        // "Y-offset" symptom is the screen background bleeding to the bottom edge,
        // which makes bottomBorder == screen != topBorder.
        Assert.Equal(topBorder, bottomBorder);
        Assert.NotEqual(topBorder, screen);
    }

    /// <summary>
    /// FR-VIC-002 / TR-VIC-EDGE-004 / BUG-RENDER-YOFFSET-001.
    /// Use case: the normal C64 display mode ($D011=$1B: DEN/RSEL/YSCROLL=3)
    ///   opens the 25-row display on raster line 51, which is also the first
    ///   matching badline for YSCROLL=3.
    /// Acceptance: the first three display lines map to character rows 0, 1, and
    ///   2, not rows 3, 4, and 5. This pins the y-offset bug seen in the supplied
    ///   ViceSharp capture.
    /// </summary>
    [Fact]
    public void DisplayCellMapper_NormalC64Scroll_StartsAtFirstCharacterRow()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x1B);

        Assert.True(vic.TryMapRasterLineToDisplayCell(vic.UpperBorderStart, out var firstRow, out var firstCharRow));
        Assert.Equal(0, firstRow);
        Assert.Equal(0, firstCharRow);

        Assert.True(vic.TryMapRasterLineToDisplayCell(vic.UpperBorderStart + 7, out var seventhRow, out var seventhCharRow));
        Assert.Equal(0, seventhRow);
        Assert.Equal(7, seventhCharRow);

        Assert.True(vic.TryMapRasterLineToDisplayCell(vic.UpperBorderStart + 8, out var secondRow, out var secondCharRow));
        Assert.Equal(1, secondRow);
        Assert.Equal(0, secondCharRow);
    }

    /// <summary>
    /// FR-VIC-002 / TR-VIC-EDGE-004 / BUG-RENDER-YOFFSET-001.
    /// Use case: when YSCROLL does not match the line where the vertical border
    ///   opens, VICE waits for the first badline at or after the open line before
    ///   row 0 display data is available.
    /// Acceptance: with YSCROLL=0, raster 51 is not mapped to row 0; raster 56 is
    ///   the first mapped row-0/char-row-0 line.
    /// </summary>
    [Fact]
    public void DisplayCellMapper_WaitsForFirstVisibleBadlineForFineScroll()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x18);

        Assert.False(vic.TryMapRasterLineToDisplayCell(vic.UpperBorderStart, out _, out _));

        var firstBadline = vic.UpperBorderStart + 5;
        Assert.Equal(0, firstBadline & 0x07);
        Assert.True(vic.TryMapRasterLineToDisplayCell(firstBadline, out var row, out var charRow));
        Assert.Equal(0, row);
        Assert.Equal(0, charRow);
    }

    private static bool ContainsReady(IMachine machine)
    {
        var screen = new byte[1000];
        for (var i = 0; i < screen.Length; i++)
            screen[i] = machine.Bus.Peek((ushort)(0x0400 + i));

        ReadOnlySpan<byte> readyScreenCodes = [18, 5, 1, 4, 25]; // R E A D Y in screen codes
        return screen.AsSpan().IndexOf(readyScreenCodes) >= 0;
    }

    private static uint DominantRowColor(byte[] frameBuffer, int width, int y)
    {
        var counts = new Dictionary<uint, int>();
        var rowStart = y * width * 4;
        for (var x = 0; x < width; x++)
        {
            var pixel = BitConverter.ToUInt32(frameBuffer, rowStart + x * 4);
            counts[pixel] = counts.TryGetValue(pixel, out var c) ? c + 1 : 1;
        }

        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static Mos6569 BuildVic() => new(new BasicBus(), new InterruptLine(InterruptType.Irq));
}
