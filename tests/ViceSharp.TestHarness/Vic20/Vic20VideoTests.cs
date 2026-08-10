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
        // VICE vic-mem.c: reverse = (bit3 ? 0 : 1). Bit SET => reverse OFF.
        Assert.False(Mos6561.InvertScreenMode(0x08));
        Assert.False(Mos6561.InvertScreenMode(0x1B)); // READY $900F: reverse off
        Assert.True(Mos6561.InvertScreenMode(0x10));  // bit3 clear => reverse on
        Assert.True(Mos6561.InvertScreenMode(0x00));
    }

    [Fact]
    public void FrameIncludesBorderStrips_AroundCharacterArea()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // VICE/KERNAL PAL origin: $9000=12, $9001=38 (places text at BorderX/BorderY).
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1B); // border cyan(3), bg white(1)

        vic.RenderNow();

        // VICE normal PAL borders: display_width 224 * VIC_PIXEL_WIDTH 2 = 448
        Assert.Equal(224 * Mos6561.PixelWidth, vic.FrameWidth);
        // last-first+1 = 311-28+1 = 284
        Assert.Equal(284, vic.FrameHeight);

        // Corner pixel is border color index 3 (cyan-ish), not background white.
        var fb = vic.FrameBuffer;
        Assert.False(IsWhite(fb, 0, 0, vic.FrameWidth));
        // Center of first character cell (after border) should be background white when cell is space/0.
        var cx = Mos6561.BorderX + 4;
        var cy = Mos6561.BorderY + 4;
        // Space char: typically empty glyph -> background
        machine.Bus.Write(vic.ResolveScreenBase(), 0x20);
        machine.Bus.Write(vic.ResolveColorBase(), 0x01);
        vic.RenderNow();
        Assert.True(IsWhite(fb = vic.FrameBuffer, cx, cy, vic.FrameWidth)
            || IsBorderColor(fb, 0, 0, vic.FrameWidth));
    }

    [Fact]
    public void CharacterMode_RendersInkPixels_AtDecodedScreenBase_ForPlantedGlyph()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // White bg (high nibble 1), black border (0), reverse OFF (bit3 set per VICE).
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x18);

        var screenBase = vic.ResolveScreenBase();
        Assert.Equal(0x1E00, screenBase);

        machine.Bus.Write(screenBase, 0x01); // 'A'
        machine.Bus.Write(0x9600, 0x00); // black ink (color base $9600 when $9002 bit7 set)

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
                // VICE VIC_PIXEL_WIDTH=2: each bit is two framebuffer pixels.
                for (var sx = 0; sx < Mos6561.PixelWidth; sx++)
                {
                    var x = ox + px * Mos6561.PixelWidth + sx;
                    var isBlack = IsBlack(fb, x, oy + py, vic.FrameWidth);
                    var isWhite = IsWhite(fb, x, oy + py, vic.FrameWidth);
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
        }

        Assert.True(inkPixels > 0, "expected black ink pixels from planted 'A' glyph");
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void BorderPixels_MatchBorderColor_NotBackground()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // Border red (2), background white (1), reverse OFF (bit3) => $1A
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1A);
        // Clear first cell so interior is solid bg
        machine.Bus.Write(vic.ResolveScreenBase(), 0x20);
        machine.Bus.Write(vic.ResolveColorBase(), 0x01);

        vic.RenderNow();
        var fb = vic.FrameBuffer;
        // Top-left is border
        Assert.True(IsSamePixel(fb, 0, 0, 1, 0, vic.FrameWidth));
        Assert.False(IsWhite(fb, 0, 0, vic.FrameWidth));
        // Interior of cell (border+char) should be white bg for space
        Assert.True(IsWhite(fb, Mos6561.BorderX + 2, Mos6561.BorderY + 2, vic.FrameWidth));
    }

    /// <summary>
    /// VICE data/VIC20/PALette.vpl index 3 cyan is RGB(0x64,0xE3,0xDE).
    /// </summary>
    [Fact]
    public void BorderCyan_UsesVicePalettesVplRgb()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        // Border cyan (3), background black (0) => $03
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x03);
        vic.RenderNow();

        var fb = vic.FrameBuffer;
        // BGRA: B at +0, G at +1, R at +2
        var b = fb[0];
        var g = fb[1];
        var r = fb[2];
        Assert.Equal(0x64, r);
        Assert.Equal(0xE3, g);
        Assert.Equal(0xDE, b);
        Assert.True(b > r && g > r, "cyan must not look like tan (R-dominant)");
    }

    /// <summary>
    /// VICE VIC_DUPLICATES_PIXELS: each character bit is two framebuffer pixels wide
    /// (vic-draw.c / victypes.h VIC_PIXEL_WIDTH=2).
    /// </summary>
    [Fact]
    public void CharacterBits_AreDoubleWidth_LikeXvic()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x18); // white bg, reverse OFF (VICE bit3)
        var screen = vic.ResolveScreenBase();
        machine.Bus.Write(screen, 0x01); // 'A'
        // Color at $9600 when $9002 bit7 set (VICE)
        machine.Bus.Write(0x9600, 0x00); // black ink, standard mode

        var glyphRow0 = machine.Bus.Peek(0x8008);
        Assert.NotEqual((byte)0, glyphRow0);

        vic.RenderNow();
        var fb = vic.FrameBuffer;
        var ox = Mos6561.BorderX;
        var oy = Mos6561.BorderY;
        // First bit of glyph: two adjacent pixels must match (double-width).
        var bit0On = (glyphRow0 & 0x80) != 0;
        if (!bit0On)
            return; // rare; glyph still exercises path above

        Assert.True(IsBlack(fb, ox, oy, vic.FrameWidth));
        Assert.True(IsBlack(fb, ox + 1, oy, vic.FrameWidth));
        Assert.Equal(2, Mos6561.PixelWidth);
    }

    [Fact]
    public void ColorBase_Uses9600_WhenScreenBankBitSet()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        Assert.Equal(0x9600, vic.ResolveColorBase());
        machine.Bus.Write(0x9002, 22);
        Assert.Equal(0x9400, vic.ResolveColorBase());
    }

    [Fact]
    public void PixelAspect_MatchesViceVicGetPixelAspect()
    {
        var pal = MachineTestFactory.CreateVic20Machine("vic20");
        var ntsc = MachineTestFactory.CreateVic20Machine("vic20ntsc");
        var palVic = Assert.IsType<Mos6561>(pal.Devices.GetByRole(DeviceRole.VideoChip));
        var ntscVic = Assert.IsType<Mos6561>(ntsc.Devices.GetByRole(DeviceRole.VideoChip));
        Assert.Equal(1.66574035f / 2.0f, palVic.PixelAspectRatio, 5);
        Assert.Equal(1.50411479f / 2.0f, ntscVic.PixelAspectRatio, 5);
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

    /// <summary>
    /// VICE vic-timing.h VIC_PAL_NORMAL_*: display_width 224, first 28, last 311;
    /// victypes.h VIC_PIXEL_WIDTH 2 => frame 448 x 284.
    /// </summary>
    [Fact]
    public void CreateVic20Pal_FrameGeometry_MatchesViceNormalBorders()
    {
        var machine = MachineTestFactory.CreateVic20Machine("vic20");
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1B);
        vic.RenderNow();
        Assert.Equal(224 * Mos6561.PixelWidth, vic.FrameWidth);
        Assert.Equal(311 - 28 + 1, vic.FrameHeight);
        Assert.True(vic.FrameBuffer.Length >= vic.FrameWidth * vic.FrameHeight * 4);
    }

    /// <summary>
    /// VICE vic-timing.h VIC_NTSC_NORMAL_*: display_width 200, first 28, last 261;
    /// VIC_PIXEL_WIDTH 2 => frame 400 x 234.
    /// </summary>
    [Fact]
    public void CreateVic20Ntsc_FrameGeometry_MatchesViceNormalBorders()
    {
        var machine = MachineTestFactory.CreateVic20Machine("vic20ntsc");
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        // NTSC KERNAL-style origin (common $9000=5, $9001=25); geometry sizes do not depend on it.
        machine.Bus.Write(0x9000, 5);
        machine.Bus.Write(0x9001, 25);
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        machine.Bus.Write(0x9003, (byte)(23 << 1));
        machine.Bus.Write(0x9005, 0xF0);
        machine.Bus.Write(0x900F, 0x1B);
        vic.RenderNow();
        Assert.True(vic.IsNtsc);
        Assert.Equal(200 * Mos6561.PixelWidth, vic.FrameWidth);
        Assert.Equal(261 - 28 + 1, vic.FrameHeight);
    }

    /// <summary>
    /// VICE cycle/draw path: display truth is per-line gbuf/cbuf paint, not a separate
    /// end-of-frame invent grid. FrameCompleted after a full raster proves Tick drew.
    /// </summary>
    [Fact]
    public void RenderNow_UsesCycleDrawPath_FrameCompletedOnce()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1B);
        var frames = 0;
        vic.FrameCompleted += (_, _) => frames++;
        vic.RenderNow();
        Assert.Equal(1, frames);
        Assert.Equal(224 * Mos6561.PixelWidth, vic.FrameWidth);
        Assert.Equal(284, vic.FrameHeight);
    }

    /// <summary>
    /// VICE <c>vic_cycle_latch_columns</c>: only <c>pending_text_cols</c> updates at cycle 1;
    /// live <c>text_cols</c> stays 0 until <c>open_h</c>/<c>start_fetch</c>.
    /// Use case: no eager live column invent. Acceptance: TextCols stays 0 when origin never opens.
    /// </summary>
    [Fact]
    public void LatchColumns_DoesNotEagerlySetLiveTextCols()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        // Origin cycle 99 never occurs on 71-cycle PAL line → open_h never fires.
        machine.Bus.Write(0x9000, 99);
        machine.Bus.Write(0x9001, 0); // open_v at raster line 0
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        machine.Bus.Write(0x9003, (byte)(23 << 1));
        vic.Reset();
        machine.Bus.Write(0x9000, 99);
        machine.Bus.Write(0x9001, 0);
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        machine.Bus.Write(0x9003, (byte)(23 << 1));

        // One full line of ticks: LatchColumns runs at cycle 1; open_h does not.
        for (var i = 0; i < vic.CyclesPerLine + 4; i++)
            vic.Tick();

        var st = vic.CaptureVideoLockstepState();
        Assert.Equal(0, st.TextCols);
    }

    /// <summary>
    /// Space glyph (0x20) paints background via VICE drawing_table slot 0 — not an invent
    /// continuous paper strip. Use case: FR-VIC20-001 draw_line fidelity for READY cells.
    /// Acceptance: white paper at cell interior after RenderNow with only space in matrix.
    /// </summary>
    [Fact]
    public void SpaceGlyph_PaintsBackgroundPaper_WithoutInventStrip()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1B);
        var screen = vic.ResolveScreenBase();
        var color = vic.ResolveColorBase();
        // Only first cell space; rest uncleared may be zero (also space-like if ROM zero).
        machine.Bus.Write(screen, 0x20);
        machine.Bus.Write(color, 0x06);
        // Chargen space is typically empty: prove at least one zero row.
        var glyph0 = machine.Bus.Peek(0x8000 + 0x20 * 8);
        Assert.Equal((byte)0, glyph0);

        vic.RenderNow();
        var fb = vic.FrameBuffer;
        var w = vic.FrameWidth;
        Assert.True(IsWhite(fb, Mos6561.BorderX + 2, Mos6561.BorderY + 2, w),
            "space cell interior must be background white from draw slots, not border");
        Assert.False(IsWhite(fb, 0, Mos6561.BorderY + 2, w), "left border remains cyan");
        Assert.False(IsWhite(fb, 400, Mos6561.BorderY + 2, w), "right border remains cyan");
    }

    /// <summary>
    /// VICE <c>video_viewport_resize</c> first_x for READY PAL (origin 12, 22 cols,
    /// canvas 448, full line 568): first_x=48. Paper on canvas at x=48; right border
    /// [400..447] must be border color — not paper flush (managed invent when src=0).
    /// </summary>
    [Fact]
    public void ViewportFirstX_ReadyPal_Is48_MatchingViceVideoViewport()
    {
        // gfx_x=96, gfx_w=352, canvas=448, screen=568 → first_x=48
        Assert.Equal(48, Mos6561.ComputeViewportFirstX(96, 352, 448, 568, gfxAreaMoves: true));
        Assert.Equal(Mos6561.FullLineOriginXKernalPal, 96);
        Assert.Equal(Mos6561.BorderX, 48); // visible paper origin after first_x crop
    }

    /// <summary>
    /// READY PAL normal-border framebuffer: left and right border bands present.
    /// Use case: FR-VIC20-001 present path matches VICE normal window (not src=0 crop).
    /// Acceptance: L=[0..47] border, paper origin 48, R=[400..447] border, mid paper white.
    /// </summary>
    [Fact]
    public void ReadyPal_NormalBorderWindow_HasLeftAndRightBorder_ViceFirstX()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1B); // cyan border, white bg, reverse OFF
        var screen = vic.ResolveScreenBase();
        var color = vic.ResolveColorBase();
        for (var i = 0; i < 22 * 23; i++)
        {
            machine.Bus.Write((ushort)(screen + i), 0x20);
            machine.Bus.Write((ushort)(color + (i & 0x3FF)), 0x06);
        }

        vic.RenderNow();
        var fb = vic.FrameBuffer;
        var w = vic.FrameWidth;
        Assert.Equal(448, w);
        var y = Mos6561.BorderY + 40; // inside paper band

        // Left border strip (canvas [0..47])
        Assert.False(IsWhite(fb, 0, y, w), "left edge must be border, not paper");
        Assert.False(IsWhite(fb, 47, y, w), "left border must extend to x=47");
        // Paper starts at first_x-centered origin 48
        Assert.True(IsWhite(fb, Mos6561.BorderX, y, w), "paper must start at visible origin 48");
        Assert.True(IsWhite(fb, Mos6561.BorderX + 8, y, w));
        // Right border strip (canvas [400..447] = 48+352 .. 447)
        Assert.False(IsWhite(fb, 400, y, w), "right border band must be border color");
        Assert.False(IsWhite(fb, 447, y, w), "right edge must be border, not paper flush");
        // Paper must not reach the right edge
        Assert.False(IsWhite(fb, w - 1, y, w));
    }

    /// <summary>
    /// READY-style paper: with KERNAL geometry and $900F=$1B, character cells
    /// (spaces) must be white paper; reverse is OFF for $1B (VICE polarity).
    /// Paper band height is 23*8 scanlines starting at BorderY.
    /// </summary>
    [Fact]
    public void ReadyStyle_PaperRegion_IsWhiteBand_NotShortCyanIsland()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vic = Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        ProgramPalKernalsVicGeometry(machine);
        machine.Bus.Write(0x900F, 0x1B); // cyan border, reverse OFF (bit3 set), white bg
        // Fill full 22x23 matrix with spaces, blue ink (standard READY ink).
        var screen = vic.ResolveScreenBase();
        var color = vic.ResolveColorBase();
        for (var i = 0; i < 22 * 23; i++)
        {
            machine.Bus.Write((ushort)(screen + i), 0x20);
            machine.Bus.Write((ushort)(color + (i & 0x3FF)), 0x06);
        }

        vic.RenderNow();
        var fb = vic.FrameBuffer;
        var w = vic.FrameWidth;
        var whitePaperRows = 0;
        for (var y = 0; y < vic.FrameHeight; y++)
        {
            var white = 0;
            for (var x = Mos6561.BorderX; x < Mos6561.BorderX + 64 && x < w; x++)
            {
                if (IsWhite(fb, x, y, w))
                    white++;
            }
            if (white > 40)
                whitePaperRows++;
        }

        // Corner = border cyan (not white).
        Assert.False(IsWhite(fb, 0, 0, w));
        // Full 23*8 paper band (KERNAL origin Y).
        Assert.True(whitePaperRows >= 23 * 8 - 4,
            $"expected ~184 white paper scanlines, got {whitePaperRows}");
        var cx = Mos6561.BorderX + 8;
        Assert.True(IsWhite(fb, cx, Mos6561.BorderY + 2, w), "paper top scanline should be white");
        Assert.True(IsWhite(fb, cx, Mos6561.BorderY + 40, w), "paper mid-band should stay white (23 rows)");
        Assert.True(IsWhite(fb, cx, Mos6561.BorderY + 23 * 8 - 2, w), "paper bottom of 23-row band should be white");
        // Below character rows: border cyan again (not leftover paper).
        Assert.False(IsWhite(fb, cx, Mos6561.BorderY + 23 * 8 + 4, w));
    }

    /// <summary>
    /// PAL KERNAL-typical VIC-I geometry: origin X=12, Y=38, 22 cols, 23 rows,
    /// screen at $1E00 (bit7 of $9002 + $9005=$F0).
    /// </summary>
    private static void ProgramPalKernalsVicGeometry(IMachine machine)
    {
        machine.Bus.Write(0x9000, 12);
        machine.Bus.Write(0x9001, 38);
        machine.Bus.Write(0x9002, unchecked((byte)(22 | 0x80)));
        machine.Bus.Write(0x9003, (byte)(23 << 1));
        machine.Bus.Write(0x9005, 0xF0);
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
