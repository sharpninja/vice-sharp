namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V3 / TR-PARITY-GATE-001: DIVERGENT (red-now
/// remediation target) parity tests for FR-VIC-DRAW-GFX
/// (AC-01/02/03/04/06/14/15) and FR-VIC-XSCROLL (AC-01/02/03/04) from
/// artifacts/vice-parity-requirements/requirements.yaml.
///
/// Each test asserts the VICE per-cycle pixel sequencer behavior
/// (native/vice/vice/src/viciisc/vicii-draw-cycle.c) against the managed
/// PixelSequencer in VicIi/PixelSequencer.cs, wired into Mos6569.Tick() and
/// feeding VideoRenderer.cs via PixelSequencer.LineIndices[504].
///
/// Method: the tests inject a known g-access byte through the VIC's
/// Phi1MemoryReader (the same channel the real g-access uses to fill gbuf) and
/// then read the per-cycle RenderBuffer/PriBuffer and the per-line
/// LineIndices/LinePriority. Because the priority bit is px &amp; 0x2, a
/// foreground graphics pixel is directly observable as PriBuffer[i]==2
/// regardless of palette colour, so the shift-register / mc-flop / xscroll-latch
/// mechanisms can be asserted against exact expected patterns. This is a real
/// per-cycle divergence oracle: the retired geometric path (VideoRenderer read
/// the char byte as (charData &gt;&gt; (7 - charX)) once per line) cannot
/// reproduce these per-cycle shift patterns.
///
/// FR-VIC-DRAW-COLOR (cregs[] snapshot pipeline, finding 28) is out of scope for
/// V3 and has no tests here.
///
/// Cycle numbering: VICE is 1-based (cycle 15 = RasterX 14 managed, where
/// RasterX = VICII_PAL_CYCLE(n) = n - 1). Tick() increments RasterX first, so
/// after AdvanceTo(line, rx) the chip is at (line, rx) and PixelSequencer holds
/// the result of that cycle's DrawGraphics8.
///
/// VICE sources: native/vice/vice/src/viciisc/vicii-draw-cycle.c (draw_graphics
/// lines 144-225, draw_graphics8 lines 227-295).
/// </summary>
public sealed class VicGraphicsPipelineDivergentParityTests
{
    private const ushort ScreenControl1 = 0xD011; // $D011: RST8/ECM/BMM/DEN/RSEL/YSCROLL
    private const ushort ScreenControl2 = 0xD016; // $D016: -/-/RES/MCM/CSEL/XSCROLL
    private const ushort BorderColor = 0xD020;
    private const ushort BackgroundColor = 0xD021;

    // $D011 = DEN | RSEL | YSCROLL(3): standard text, 25-row window open.
    private const byte DenStandardText = 0x1B;
    private const byte Foreground = 2; // px & 0x2 priority bit for a set graphics pixel.

    // A display cycle deep enough that the 2-stage gbuf/vbuf/cbuf pipe has fully
    // filled and the vertical border is open (line 100 is inside the RSEL=1
    // 25-row window 0x33-0xFB, RasterX 30 is inside the 40-col window 14-53).
    private const ushort DisplayLine = 100;
    private const byte DisplayCycle = 30;

    // ----------------------------------------------------------------
    // FR-VIC-DRAW-GFX: per-cycle graphics pixel generation (draw_graphics8)
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-01.
    /// Use case: draw_graphics8 emits exactly 8 render_buffer/pri_buffer entries
    /// per cycle from the 8-bit gbuf shift register (vicii-draw-cycle.c:227-262).
    /// The managed VideoRenderer reconstructed the whole line at line-wrap with
    /// geometric character decoding (VideoRenderer.cs:75-157) and produced no
    /// per-cycle buffers.
    /// Acceptance: with an all-ones g-access byte (0xFF) latched at xscroll 0,
    /// every one of the 8 PriBuffer entries and the 8 matching LinePriority slots
    /// for a display cycle are foreground (px &amp; 2 == 2); the per-cycle line
    /// buffer is populated at RasterX*8 exactly.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-01", ParityTag.Divergent, pending: false)]
    public void DrawGraphics8_EmitsEightForegroundPixelsPerCycle()
    {
        var vic = BuildVic(gbuf: 0xFF);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08); // CSEL=1, xscroll=0

        AdvanceTo(vic, DisplayLine, DisplayCycle);

        var pri = vic.PixelSequencer.PriBuffer;
        Assert.Equal(8, pri.Length);
        for (int i = 0; i < 8; i++)
            Assert.Equal(Foreground, pri[i]);

        int offset = DisplayCycle * 8;
        for (int i = 0; i < 8; i++)
            Assert.Equal(Foreground, vic.PixelSequencer.LinePriority[offset + i]);
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-02.
    /// Use case: draw_graphics latches vbuf/cbuf/gbuf and sets gbuf_mc_flop=1 at
    /// the pixel index i==xscroll_pipe (vicii-draw-cycle.c:151-158), so the fresh
    /// cell's first pixel appears at column i==xscroll_pipe, not at pixel 0. The
    /// managed VideoRenderer used a fixed screenX offset (VideoRenderer.cs:253,
    /// 264-265) with no per-cycle latch.
    /// Acceptance: with a single-MSB g-access byte (0x80) and xscroll 3, the sole
    /// foreground pixel of the freshly latched cell appears at PriBuffer[3]
    /// (the latch column), with PriBuffer[0] and PriBuffer[4] background.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-02", ParityTag.Divergent, pending: false)]
    public void DrawGraphics_LatchesAtXscrollPipeColumn()
    {
        var vic = BuildVic(gbuf: 0x80);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08 | 0x03); // CSEL=1, xscroll=3

        AdvanceTo(vic, DisplayLine, DisplayCycle);

        Assert.Equal(3, vic.PixelSequencer.XscrollPipe);

        var pri = vic.PixelSequencer.PriBuffer;
        Assert.Equal(Foreground, pri[3]); // fresh cell's MSB pixel at the latch column
        Assert.Equal(0, pri[0]);          // before the latch: previous cell already shifted out
        Assert.Equal(0, pri[4]);          // after the MSB: shifted to background
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-03.
    /// Use case: draw_graphics shifts the gbuf register MSB-first
    /// (vicii-draw-cycle.c:191 gbuf_reg &lt;&lt;= 1 after each pixel, reading bit 7
    /// at lines 172/182). The managed VideoRenderer read each bit directly via
    /// (charData &gt;&gt; (7 - charX)) &amp; 1 (VideoRenderer.cs:299) from the
    /// original byte, not a shift register.
    /// Acceptance: an MSB-only byte (0x80) puts the single foreground pixel at
    /// column 0; an LSB-only byte (0x01) puts it at column 7. Only a left-shifting
    /// MSB-first register produces both patterns.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-03", ParityTag.Divergent, pending: false)]
    public void GbufReg_ShiftsMsbFirst()
    {
        var msb = BuildVic(gbuf: 0x80);
        msb.Write(ScreenControl1, DenStandardText);
        msb.Write(ScreenControl2, 0x08); // xscroll=0
        AdvanceTo(msb, DisplayLine, DisplayCycle);
        Assert.Equal(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0 }, msb.PixelSequencer.PriBuffer);

        var lsb = BuildVic(gbuf: 0x01);
        lsb.Write(ScreenControl1, DenStandardText);
        lsb.Write(ScreenControl2, 0x08); // xscroll=0
        AdvanceTo(lsb, DisplayLine, DisplayCycle);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 }, lsb.PixelSequencer.PriBuffer);
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-04.
    /// Use case: in multicolour mode the 2-bit pixel is held across a pair of dot
    /// positions by gbuf_mc_flop, and the shift-register consumes 2 bits per pair
    /// (vicii-draw-cycle.c:166-169). The managed renderer grouped multicolour
    /// pairs by charX position (VideoRenderer.cs:381-385), not by a per-cycle flop.
    /// Acceptance: an MSB-only byte (0x80) in multicolour bitmap mode yields the
    /// 2-bit value 0b10 (=2) held across BOTH pixels 0 and 1
    /// (PriBuffer = [2,2,0,...]); the same byte in hires yields a single-pixel
    /// [2,0,0,...]. The doubled foreground column proves the flop-held pair.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-04", ParityTag.Divergent, pending: false)]
    public void GbufMcFlop_HoldsMulticolourPairAcrossTwoPixels()
    {
        // Multicolour bitmap: BMM=1 ($D011 bit 5) so the mc-pixel branch is taken
        // via (vmode11_pipe & 0x08); MCM=1 ($D016 bit 4) so vmode16_pipe2 gates it.
        var mc = BuildVic(gbuf: 0x80);
        mc.Write(ScreenControl1, (byte)(DenStandardText | 0x20)); // + BMM
        mc.Write(ScreenControl2, 0x08 | 0x10);                    // CSEL + MCM
        AdvanceTo(mc, DisplayLine, DisplayCycle);
        Assert.Equal(new byte[] { 2, 2, 0, 0, 0, 0, 0, 0 }, mc.PixelSequencer.PriBuffer);

        var hires = BuildVic(gbuf: 0x80);
        hires.Write(ScreenControl1, DenStandardText);
        hires.Write(ScreenControl2, 0x08); // MCM=0
        AdvanceTo(hires, DisplayLine, DisplayCycle);
        Assert.Equal(new byte[] { 2, 0, 0, 0, 0, 0, 0, 0 }, hires.PixelSequencer.PriBuffer);
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-06.
    /// Use case: the video mode is carried as a per-cycle pipe (vmode16_pipe2,
    /// vicii-draw-cycle.c:243,258-261), which gates the MCM-transition $D023
    /// kludge; a mid-screen MCM change is visible one cycle later. The managed
    /// VideoRenderer sampled DisplayModeSelection once per line
    /// (VideoRenderer.cs:110), so a mid-line MCM change was lost.
    /// Acceptance: with MCM=0 settled, vmode16_pipe2 is 0; after a single cycle
    /// with MCM newly enabled, vmode16_pipe2 becomes 4 (($D016 &amp; 0x10) &gt;&gt; 2).
    /// A once-per-line sample could never expose this per-cycle pipe value.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-06", ParityTag.Divergent, pending: false)]
    public void Vmode16Pipe2_TracksMcmTransitionPerCycle()
    {
        var vic = BuildVic(gbuf: 0x80);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08); // MCM=0

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(0, vic.PixelSequencer.Vmode16Pipe2); // settled, MCM off

        vic.Write(ScreenControl2, 0x08 | 0x10); // enable MCM
        vic.Tick();                             // one more display cycle (RasterX 31)
        Assert.Equal(4, vic.PixelSequencer.Vmode16Pipe2); // per-cycle pipe now reflects MCM
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-14.
    /// Use case: gbuf/vbuf/cbuf are double-buffered: pipe0 is loaded from the
    /// current cycle's g-access, then promoted to pipe1 at the end of the cycle
    /// (vicii-draw-cycle.c:269-294), so the data reaching the shift register lags
    /// the g-access by one cycle. The managed VideoRenderer read the matrix cell
    /// per pixel with no pipeline (VideoRenderer.cs:284-294).
    /// Acceptance: a g-access byte present only on RasterX 30 loads GbufPipe0Reg
    /// on cycle 30, and exactly one cycle later (RasterX 31) that value has been
    /// promoted into GbufPipe1Reg while GbufPipe0Reg has reloaded from the
    /// now-zero g-access.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-14", ParityTag.Divergent, pending: false)]
    public void GbufPipe_DoubleBuffered_OneCycleDelay()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);
        // g-access delivers 0x80 only on cycle 30 (CurrentCycle == RasterX here).
        vic.Phi1MemoryReader = c => c == DisplayCycle ? (byte)0x80 : (byte)0x00;

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(0x80, vic.PixelSequencer.GbufPipe0Reg); // loaded this cycle

        AdvanceTo(vic, DisplayLine, (byte)(DisplayCycle + 1));
        Assert.Equal(0x80, vic.PixelSequencer.GbufPipe1Reg); // promoted pipe0 -> pipe1
        Assert.Equal(0x00, vic.PixelSequencer.GbufPipe0Reg); // reloaded from zero g-access
    }

    /// <summary>
    /// FR-VIC-DRAW-GFX AC-15.
    /// Use case: gbuf is forced to 0 when the cycle is outside the visible display
    /// window (vis_en false / vertical border) and dmli is reset there
    /// (vicii-draw-cycle.c:275-294). The managed VideoRenderer used a background
    /// colour path (VideoRenderer.cs:106,259-262) instead of a force-to-zero gate.
    /// Acceptance: with a constant 0xFF g-access, GbufPipe0Reg is 0 and Dmli is 0
    /// at a left-border cycle (RasterX 10, outside 14-53), but GbufPipe0Reg is
    /// 0xFF inside the display window (RasterX 30).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-DRAW-GFX-15", ParityTag.Divergent, pending: false)]
    public void GbufPipe0_ForcedZeroOutsideVisibleWindow()
    {
        var vic = BuildVic(gbuf: 0xFF);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, DisplayLine, 10); // left border, before the 40-col window
        Assert.Equal(0, vic.PixelSequencer.GbufPipe0Reg);
        Assert.Equal(0, vic.PixelSequencer.Dmli);

        AdvanceTo(vic, DisplayLine, DisplayCycle); // inside the window
        Assert.Equal(0xFF, vic.PixelSequencer.GbufPipe0Reg);
    }

    // ----------------------------------------------------------------
    // FR-VIC-XSCROLL: $D016 fine horizontal scroll pipe
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-XSCROLL AC-01.
    /// Use case: xscroll_pipe is sampled from regs[$16] &amp; 0x07 at the end of
    /// each display cycle (vicii-draw-cycle.c:276-280). The managed Mos6569 had an
    /// XScroll property (Mos6569.cs:858) that the render pipeline never consumed.
    /// Acceptance: with $D016 xscroll=5, PixelSequencer.XscrollPipe equals 5 after
    /// a display cycle where vis_en is true.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-XSCROLL-01", ParityTag.Divergent, pending: false)]
    public void XscrollPipe_SampledFromRegisterEachDisplayCycle()
    {
        var vic = BuildVic(gbuf: 0x80);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08 | 0x05); // CSEL=1, xscroll=5

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(5, vic.PixelSequencer.XscrollPipe);
    }

    /// <summary>
    /// FR-VIC-XSCROLL AC-02.
    /// Use case: the display cell becomes visible from column i==xscroll_pipe,
    /// shifting the whole image right by xscroll pixels
    /// (vicii-draw-cycle.c:151-158). The managed VideoRenderer started the cell at
    /// a fixed leftBorderPixel (VideoRenderer.cs:253,264-265) with no fine shift.
    /// Acceptance: a single-MSB cell (0x80) renders its foreground column at
    /// PriBuffer[0] with xscroll 0, and shifts right to PriBuffer[2] with
    /// xscroll 2, proving the image moves right by exactly xscroll pixels.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-XSCROLL-02", ParityTag.Divergent, pending: false)]
    public void XscrollPipe_ShiftsCellRightByScrollPixels()
    {
        var scroll0 = BuildVic(gbuf: 0x80);
        scroll0.Write(ScreenControl1, DenStandardText);
        scroll0.Write(ScreenControl2, 0x08 | 0x00); // xscroll=0
        AdvanceTo(scroll0, DisplayLine, DisplayCycle);
        Assert.Equal(Foreground, scroll0.PixelSequencer.PriBuffer[0]);

        var scroll2 = BuildVic(gbuf: 0x80);
        scroll2.Write(ScreenControl1, DenStandardText);
        scroll2.Write(ScreenControl2, 0x08 | 0x02); // xscroll=2
        AdvanceTo(scroll2, DisplayLine, DisplayCycle);
        Assert.Equal(0, scroll2.PixelSequencer.PriBuffer[0]);
        Assert.Equal(Foreground, scroll2.PixelSequencer.PriBuffer[2]);
    }

    /// <summary>
    /// FR-VIC-XSCROLL AC-03.
    /// Use case: a write to $D016 bits 0-2 takes effect only in the NEXT display
    /// cycle because xscroll_pipe is sampled at the END of the current vis_en
    /// cycle (vicii-draw-cycle.c:276-280). The managed renderer had no pipe delay.
    /// Acceptance: after a display cycle with xscroll 0, XscrollPipe is 0; writing
    /// xscroll 7 does not change XscrollPipe until the following display cycle's
    /// DrawGraphics8 has run, at which point it is 7.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-XSCROLL-03", ParityTag.Divergent, pending: false)]
    public void XscrollPipe_WriteTakesEffectNextCycle()
    {
        var vic = BuildVic(gbuf: 0x80);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08 | 0x00); // xscroll=0

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(0, vic.PixelSequencer.XscrollPipe);

        vic.Write(ScreenControl2, 0x08 | 0x07); // xscroll=7, between cycles
        // The value written mid-cycle is not sampled until the next DrawGraphics8.
        AdvanceTo(vic, DisplayLine, (byte)(DisplayCycle + 1));
        Assert.Equal(7, vic.PixelSequencer.XscrollPipe);
    }

    /// <summary>
    /// FR-VIC-XSCROLL AC-04.
    /// Use case: the xscroll latch offset composes with the absolute line-buffer
    /// position (the fractional cell that CSEL border-clips): the latch at column
    /// i==xscroll_pipe places the cell's first pixel at line-buffer index
    /// RasterX*8 + xscroll (vicii-draw-cycle.c:151-158). The managed renderer used
    /// LeftBorderPixel directly (Mos6569.cs:649,654) with no fractional offset.
    /// Acceptance: with CSEL=1 and xscroll 3, the freshly latched single-MSB cell
    /// lands at LinePriority[RasterX*8 + 3] and not at LinePriority[RasterX*8].
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-XSCROLL-04", ParityTag.Divergent, pending: false)]
    public void XscrollPipe_LatchOffsetPlacesCellInLineBuffer()
    {
        var vic = BuildVic(gbuf: 0x80);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08 | 0x03); // CSEL=1, xscroll=3

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(3, vic.PixelSequencer.XscrollPipe);

        int baseIndex = DisplayCycle * 8;
        Assert.Equal(Foreground, vic.PixelSequencer.LinePriority[baseIndex + 3]);
        Assert.Equal(0, vic.PixelSequencer.LinePriority[baseIndex + 0]);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds a bare PAL VIC-II (no memory map), reset to power-on phase, with the
    /// Phi1 g-access reader forced to a constant byte so the gbuf shift register
    /// receives a known pattern every cycle.
    /// </summary>
    private static Mos6569 BuildVic(byte gbuf = 0x00)
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        vic.Phi1MemoryReader = _ => gbuf;
        return vic;
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        int maxCycles = vic.TotalLines * vic.CyclesPerLine * 3;
        for (int cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;
            vic.Tick();
        }

        throw new InvalidOperationException(
            $"Could not reach raster line {rasterLine} cycle {rasterCycle} within {maxCycles} cycles.");
    }
}
