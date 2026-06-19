namespace ViceSharp.TestHarness;

using System;
using System.Collections.Generic;
using System.Linq;
using ViceSharp.Abstractions;
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
}
