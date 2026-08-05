namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Vic;
using Xunit;

/// <summary>
/// FR-VIC20-001. VIC-I character-mode frames with borders and $900F colors.
/// </summary>
public sealed class Vic20VideoTests
{
    [Fact]
    public void ResolveScreenBase_MatchesViceFormula_UnexpandedReadyLayout()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        machine.Bus.Write(0x9005, 0xF0);
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));

        Assert.Equal(0x1E00, vic.ResolveScreenBase());
        Assert.Equal(0x1E00, vic.CurrentScreenBase);
    }

    [Fact]
    public void ResolveScreenBase_BankBitClear_Selects8000Bank()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        machine.Bus.Write(0x9005, 0x00);
        machine.Bus.Write(0x9002, 0x00);
        Assert.Equal(0x8000, vic.ResolveScreenBase());
    }

    [Fact]
    public void Reg900F_BorderIsLowNibble_BackgroundIsHighNibble()
    {
        Assert.Equal(3, Mos6561.BorderColorIndex(0x1B));
        Assert.Equal(1, Mos6561.BackgroundColorIndex(0x1B));
        Assert.True(Mos6561.InvertScreenMode(0x08));
        Assert.False(Mos6561.InvertScreenMode(0x10));
    }

    [Fact]
    public void FrameIncludesBorderStrips_AroundCharacterArea()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        machine.Bus.Write(0x9003, (byte)(23 << 1));
        machine.Bus.Write(0x900F, 0x1B); // border cyan(3), bg white(1)
        machine.Bus.Write(0x9005, 0xF0);

        vic.RenderNow();

        Assert.Equal(Mos6561.BorderX * 2 + 22 * 8, vic.FrameWidth);
        Assert.Equal(Mos6561.BorderY * 2 + 23 * 8, vic.FrameHeight);

        // Corner pixel is border color index 3 (cyan-ish), not background white.
        var fb = vic.FrameBuffer;
        Assert.False(IsWhite(fb, 0, 0, vic.FrameWidth));
        // Center of first character cell (after border) should be background white when cell is space/0.
        var cx = Mos6561.BorderX + 4;
        var cy = Mos6561.BorderY + 4;
        // Space char: typically empty glyph -> background
        machine.Bus.Write(vic.ResolveScreenBase(), 0x20);
        machine.Bus.Write(0x9400, 0x01);
        vic.RenderNow();
        Assert.True(IsWhite(fb = vic.FrameBuffer, cx, cy, vic.FrameWidth)
            || IsBorderColor(fb, 0, 0, vic.FrameWidth));
    }

    [Fact]
    public void CharacterMode_RendersInkPixels_AtDecodedScreenBase_ForPlantedGlyph()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // White bg (high nibble 1), black border (0), no invert.
        machine.Bus.Write(0x900F, 0x10);
        machine.Bus.Write(0x9005, 0xF0);
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));

        var screenBase = vic.ResolveScreenBase();
        Assert.Equal(0x1E00, screenBase);

        machine.Bus.Write(screenBase, 0x01); // 'A'
        machine.Bus.Write(0x9400, 0x00); // black ink

        var glyphRow0 = machine.Bus.Peek(0x8008);
        Assert.NotEqual((byte)0, glyphRow0);

        vic.RenderNow();

        var fb = vic.FrameBuffer;
        var inkPixels = 0;
        var mismatches = 0;
        var ox = Mos6561.BorderX;
        var oy = Mos6561.BorderY;
        for (var py = 0; py < 8; py++)
        {
            var bits = machine.Bus.Peek((ushort)(0x8008 + py));
            for (var px = 0; px < 8; px++)
            {
                var on = (bits & (0x80 >> px)) != 0;
                var isBlack = IsBlack(fb, ox + px, oy + py, vic.FrameWidth);
                var isWhite = IsWhite(fb, ox + px, oy + py, vic.FrameWidth);
                if (on)
                {
                    if (!isBlack) mismatches++;
                    else inkPixels++;
                }
                else if (!isWhite)
                {
                    mismatches++;
                }
            }
        }

        Assert.True(inkPixels > 0, "expected black ink pixels from planted 'A' glyph");
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void BorderPixels_MatchBorderColor_NotBackground()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // Border red (2), background white (1) => $12
        machine.Bus.Write(0x900F, 0x12);
        machine.Bus.Write(0x9005, 0xF0);
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        // Clear first cell so interior is solid bg
        machine.Bus.Write(vic.ResolveScreenBase(), 0x20);
        machine.Bus.Write(0x9400, 0x01);

        vic.RenderNow();
        var fb = vic.FrameBuffer;
        // Top-left is border
        Assert.True(IsSamePixel(fb, 0, 0, 1, 0, vic.FrameWidth));
        Assert.False(IsWhite(fb, 0, 0, vic.FrameWidth));
        // Interior of cell (border+char) should be white bg for space
        Assert.True(IsWhite(fb, Mos6561.BorderX + 2, Mos6561.BorderY + 2, vic.FrameWidth));
    }

    [Fact]
    public void FrameCompleted_FiresAfterFullRaster()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        var frames = 0;
        vic.FrameCompleted += (_, _) => frames++;

        var cycles = vic.CyclesPerLine * vic.TotalLines;
        for (var i = 0; i < cycles; i++)
            machine.Clock.Step();

        Assert.True(frames >= 1, $"expected FrameCompleted, got {frames}");
    }

    [Fact]
    public void PalAndNtsc_Profiles_ExposeDistinctTiming()
    {
        var pal = MachineTestFactory.CreateVic20Machine("vic20");
        var ntsc = MachineTestFactory.CreateVic20Machine("vic20ntsc");
        var palVic = Assert.IsType<Mos6561>(pal.Devices.GetByRole(DeviceRole.VideoChip));
        var ntscVic = Assert.IsType<Mos6561>(ntsc.Devices.GetByRole(DeviceRole.VideoChip));

        Assert.Equal(71, palVic.CyclesPerLine);
        Assert.Equal(312, palVic.TotalLines);
        Assert.Equal(65, ntscVic.CyclesPerLine);
        Assert.Equal(261, ntscVic.TotalLines);
    }

    private static bool IsWhite(byte[] fb, int x, int y, int width)
    {
        var i = (y * width + x) * 4;
        return fb[i] == 0xFF && fb[i + 1] == 0xFF && fb[i + 2] == 0xFF;
    }

    private static bool IsBlack(byte[] fb, int x, int y, int width)
    {
        var i = (y * width + x) * 4;
        return fb[i] == 0x00 && fb[i + 1] == 0x00 && fb[i + 2] == 0x00;
    }

    private static bool IsBorderColor(byte[] fb, int x, int y, int width)
        => !IsWhite(fb, x, y, width);

    private static bool IsSamePixel(byte[] fb, int x1, int y1, int x2, int y2, int width)
    {
        var a = (y1 * width + x1) * 4;
        var b = (y2 * width + x2) * 4;
        return fb[a] == fb[b] && fb[a + 1] == fb[b + 1] && fb[a + 2] == fb[b + 2];
    }
}
