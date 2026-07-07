namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V7 / TR-PARITY-GATE-001: DIVERGENT (red-now
/// remediation target) parity tests for FR-VIC-BORDER (AC-01..07, AC-11, AC-12)
/// from artifacts/vice-parity-requirements/requirements.yaml.
///
/// These tests verify the VICE per-cycle draw_border8 behavior
/// (native/vice/vice/src/viciisc/vicii-draw-cycle.c lines 541-575,
/// vicii-cycle.c check_hborder lines 184-200,
/// vicii-chip-model.c PAL table ChkBrdL1/L0/R0/R1 entries lines 145-225)
/// against the managed PixelSequencer.DrawBorder8 in VicIi/PixelSequencer.cs
/// wired into Mos6569.Tick(), and the correct left/right border flip-flop
/// check cycles in Mos6569.cs.
///
/// COL_D020 = 0x20 (vicii-draw-cycle.c line 47). DrawBorder8 fills
/// RenderBuffer[0..7] with 0x20 for border pixels; DrawColors8 resolves
/// 0x20 via Cregs[0x20] to the live $D020 palette index.
///
/// Cycle numbering: VICE is 1-based; managed RasterX = VICE cycle - 1.
/// VICE L1 at Phi2(17) = managed RasterX 16 (CSEL=1 left check).
/// VICE L0 at Phi2(18) = managed RasterX 17 (CSEL=0 left check).
/// VICE R0 at Phi2(56) = managed RasterX 55 (CSEL=0 right check).
/// VICE R1 at Phi2(57) = managed RasterX 56 (CSEL=1 right check).
/// </summary>
[Collection("NativeVice")]
public sealed class VicBorderDivergentParityTests
{
    private const byte ColD020 = 0x20; // vicii-draw-cycle.c line 47

    private const ushort ScreenControl1 = 0xD011;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort BorderColorReg = 0xD020;

    // Standard text, DEN=1, RSEL=1 (25 rows). 0x1B = DEN | RSEL | YSCROLL(3).
    private const byte DenStandardText = 0x1B;

    // A mid-screen display line well inside the 25-row window.
    private const ushort DisplayLine = 100;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mos6569 BuildVic()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        vic.Phi1MemoryReader = _ => 0x00;
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

    // -----------------------------------------------------------------------
    // AC-01: draw_border8 reads csel=regs[$16]&0x8 each cycle
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-01 (TEST-VIC-BORDER-01, DIVERGENT).
    /// Use case: draw_border8 reads csel=vicii.regs[$16]&amp;0x8 each cycle to
    /// select whether the CSEL=1 or CSEL=0 edge handling runs
    /// (vicii-draw-cycle.c:543). The managed code had no per-cycle border
    /// pixel consumer at all (finding 42).
    /// Acceptance: with border_state=1, main_border=0 (transition cycle), CSEL=1
    /// fills all 8 RenderBuffer entries with COL_D020 (0x20); CSEL=0 fills only
    /// the first 7. This proves csel is read and drives the edge logic per cycle.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-01", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_ReadsCselEachCycle_DifferentEdgeForCsel0And1()
    {
        // CSEL=1: transition border_state=1, main_border=0 -> memset 8
        var seq1 = BuildVic().PixelSequencer;
        seq1.BorderState = 1;
        // Fill RenderBuffer with sentinel to detect partial fill.
        for (int i = 0; i < 8; i++) seq1.RenderBuffer[i] = 0xFF;
        // Register byte 0x16: CSEL=1 is bit 3 (0x08).
        // PixelSequencer reads _regs[0x16] directly; we need registers[0x16] = 0x08.
        // Since PixelSequencer._regs is the same array as Mos6569._registers, use BuildVic
        // and set via Write.
        var vic1 = BuildVic();
        vic1.Write(ScreenControl2, 0x08); // CSEL=1
        for (int i = 0; i < 8; i++) vic1.PixelSequencer.RenderBuffer[i] = 0xFF;
        vic1.PixelSequencer.BorderState = 1;
        vic1.PixelSequencer.DrawBorder8(mainBorder: false);
        // All 8 must be COL_D020: csel=1, border_state=1 -> memset(render_buffer,COL_D020,8)
        for (int i = 0; i < 8; i++)
            Assert.Equal(ColD020, vic1.PixelSequencer.RenderBuffer[i]);

        // CSEL=0: transition border_state=1, main_border=0 -> memset 7
        var vic0 = BuildVic();
        vic0.Write(ScreenControl2, 0x00); // CSEL=0
        for (int i = 0; i < 8; i++) vic0.PixelSequencer.RenderBuffer[i] = 0xFF;
        vic0.PixelSequencer.BorderState = 1;
        vic0.PixelSequencer.DrawBorder8(mainBorder: false);
        // Pixels 0..6 must be COL_D020; pixel 7 must NOT be (not the csel=0 open-border path)
        for (int i = 0; i < 7; i++)
            Assert.Equal(ColD020, vic0.PixelSequencer.RenderBuffer[i]);
        Assert.NotEqual(ColD020, vic0.PixelSequencer.RenderBuffer[7]);
    }

    // -----------------------------------------------------------------------
    // AC-02: no-border early exit leaves render_buffer untouched
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-02 (TEST-VIC-BORDER-02, DIVERGENT).
    /// Use case: draw_border8 early-exits without touching render_buffer when
    /// both border_state and main_border are zero (vicii-draw-cycle.c:547-549).
    /// The managed code produced a whole-line border fill in VideoRenderer
    /// instead of a per-cycle no-op (finding 42).
    /// Acceptance: with border_state=0 and main_border=false, all 8 RenderBuffer
    /// entries retain their pre-call sentinel value after DrawBorder8.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-02", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_NoOpWhenBothFlagsZero_RenderBufferUnchanged()
    {
        var vic = BuildVic();
        const byte Sentinel = 0xAA;
        for (int i = 0; i < 8; i++) vic.PixelSequencer.RenderBuffer[i] = Sentinel;
        vic.PixelSequencer.BorderState = 0;

        vic.PixelSequencer.DrawBorder8(mainBorder: false);

        for (int i = 0; i < 8; i++)
            Assert.Equal(Sentinel, vic.PixelSequencer.RenderBuffer[i]);
    }

    // -----------------------------------------------------------------------
    // AC-03: continuous border fills all 8 with COL_D020
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-03 (TEST-VIC-BORDER-03, DIVERGENT).
    /// Use case: when border_state and main_border are both non-zero (continuous
    /// border), draw_border8 does memset(render_buffer, COL_D020, 8) and returns
    /// (vicii-draw-cycle.c:551-554). The managed code called FillBorderSegmented
    /// over the whole line via the change-log hack (VideoRenderer.cs:538, finding 42).
    /// Acceptance: with border_state=1 and main_border=true, all 8 RenderBuffer
    /// entries equal COL_D020 (0x20) after DrawBorder8, and border_state remains 1.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-03", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_ContinuousBorder_FillsAllEightWithColD020()
    {
        var vic = BuildVic();
        for (int i = 0; i < 8; i++) vic.PixelSequencer.RenderBuffer[i] = 0x00;
        vic.PixelSequencer.BorderState = 1;

        vic.PixelSequencer.DrawBorder8(mainBorder: true);

        for (int i = 0; i < 8; i++)
            Assert.Equal(ColD020, vic.PixelSequencer.RenderBuffer[i]);
        // border_state is NOT updated by the early-exit path (early return before the
        // CSEL else-if that updates it), so border_state remains 1 (continuous).
        Assert.Equal(1, vic.PixelSequencer.BorderState);
    }

    // -----------------------------------------------------------------------
    // AC-04: CSEL=1 transition: memset 8, border_state=main_border
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-04 (TEST-VIC-BORDER-04, DIVERGENT).
    /// Use case: CSEL=1 transition case (vicii-draw-cycle.c:561-565): if
    /// border_state is set, memset render_buffer COL_D020 8 bytes; then
    /// border_state = main_border. The managed code had no per-cycle CSEL-aware
    /// border_state update (finding 42).
    /// Acceptance: (border_state=1, main_border=false, CSEL=1): all 8 RenderBuffer
    /// entries = COL_D020 and border_state is updated to 0 (= main_border).
    /// Acceptance (border_state=0, main_border=true, CSEL=1): RenderBuffer unchanged
    /// (no memset since border_state=0) and border_state is updated to 1.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-04", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_Csel1Transition_MemsetAndUpdatesBorderState()
    {
        // border_state=1, main_border=false: memset 8, then border_state=0
        var vic = BuildVic();
        vic.Write(ScreenControl2, 0x08); // CSEL=1
        for (int i = 0; i < 8; i++) vic.PixelSequencer.RenderBuffer[i] = 0x00;
        vic.PixelSequencer.BorderState = 1;
        vic.PixelSequencer.DrawBorder8(mainBorder: false);
        for (int i = 0; i < 8; i++)
            Assert.Equal(ColD020, vic.PixelSequencer.RenderBuffer[i]);
        Assert.Equal(0, vic.PixelSequencer.BorderState);

        // border_state=0, main_border=true: no memset, border_state=1
        var vic2 = BuildVic();
        vic2.Write(ScreenControl2, 0x08); // CSEL=1
        const byte Sentinel = 0xBB;
        for (int i = 0; i < 8; i++) vic2.PixelSequencer.RenderBuffer[i] = Sentinel;
        vic2.PixelSequencer.BorderState = 0;
        vic2.PixelSequencer.DrawBorder8(mainBorder: true);
        for (int i = 0; i < 8; i++)
            Assert.Equal(Sentinel, vic2.PixelSequencer.RenderBuffer[i]);
        Assert.Equal(1, vic2.PixelSequencer.BorderState);
    }

    // -----------------------------------------------------------------------
    // AC-05: CSEL=0 transition: memset 7 then render_buffer[7]=COL_D020 if border_state
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-05 (TEST-VIC-BORDER-05, DIVERGENT).
    /// Use case: CSEL=0 transition case (vicii-draw-cycle.c:566-574): if
    /// border_state is set, memset render_buffer 7 bytes; then border_state =
    /// main_border; if the NEW border_state is set, render_buffer[7] = COL_D020
    /// (38-column right edge pixel). The managed code had no per-cycle CSEL=0
    /// edge handling (finding 42).
    /// Acceptance: (border_state=1, main_border=false, CSEL=0): pixels 0..6 =
    /// COL_D020, pixel 7 = sentinel (no border), border_state = 0.
    /// Acceptance (border_state=0, main_border=true, CSEL=0): pixels 0..6 =
    /// sentinel, pixel 7 = COL_D020 (open-border entering pixel), border_state=1.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-05", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_Csel0Transition_Memset7AndPixel7Rule()
    {
        // border_state=1, main_border=false, CSEL=0: memset 7, pixel 7 untouched
        var vic = BuildVic();
        vic.Write(ScreenControl2, 0x00); // CSEL=0
        const byte Sentinel = 0xCC;
        for (int i = 0; i < 8; i++) vic.PixelSequencer.RenderBuffer[i] = Sentinel;
        vic.PixelSequencer.BorderState = 1;
        vic.PixelSequencer.DrawBorder8(mainBorder: false);
        for (int i = 0; i < 7; i++)
            Assert.Equal(ColD020, vic.PixelSequencer.RenderBuffer[i]);
        Assert.Equal(Sentinel, vic.PixelSequencer.RenderBuffer[7]);
        Assert.Equal(0, vic.PixelSequencer.BorderState);

        // border_state=0, main_border=true, CSEL=0: no memset, pixel 7 = COL_D020
        var vic2 = BuildVic();
        vic2.Write(ScreenControl2, 0x00); // CSEL=0
        for (int i = 0; i < 8; i++) vic2.PixelSequencer.RenderBuffer[i] = Sentinel;
        vic2.PixelSequencer.BorderState = 0;
        vic2.PixelSequencer.DrawBorder8(mainBorder: true);
        for (int i = 0; i < 7; i++)
            Assert.Equal(Sentinel, vic2.PixelSequencer.RenderBuffer[i]);
        Assert.Equal(ColD020, vic2.PixelSequencer.RenderBuffer[7]);
        Assert.Equal(1, vic2.PixelSequencer.BorderState);
    }

    // -----------------------------------------------------------------------
    // AC-06: border_state is a one-cycle-lagged copy of main_border
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-06 (TEST-VIC-BORDER-06, DIVERGENT).
    /// Use case: border_state is VICE's one-cycle-lagged shadow of main_border
    /// (vicii-draw-cycle.c:105,565,570). In VICE each DrawBorder8 call updates
    /// border_state from the current main_border at the END of the call, so the
    /// next cycle's DrawBorder8 uses the previous cycle's main_border value.
    /// The managed VideoRenderer kept no lagged FF at all (finding 43).
    /// Acceptance: two consecutive DrawBorder8 calls with main_border toggling
    /// from false to true: after cycle 1 (main_border=false) border_state=0;
    /// after cycle 2 (main_border=true) border_state=1.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-06", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_BorderState_IsOneCycleLaggedMainBorder()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl2, 0x08); // CSEL=1

        // Cycle 1: border_state=0, main_border=false -> early exit; border_state unchanged at 0
        vic.PixelSequencer.BorderState = 0;
        vic.PixelSequencer.DrawBorder8(mainBorder: false);
        Assert.Equal(0, vic.PixelSequencer.BorderState);

        // Cycle 2: border_state=0, main_border=true -> CSEL=1 transition;
        // no memset (border_state=0), border_state = main_border = 1
        vic.PixelSequencer.DrawBorder8(mainBorder: true);
        Assert.Equal(1, vic.PixelSequencer.BorderState);

        // Cycle 3: border_state=1, main_border=true -> continuous border early exit;
        // border_state NOT updated by early-exit path (remains 1 = already set).
        vic.PixelSequencer.DrawBorder8(mainBorder: true);
        Assert.Equal(1, vic.PixelSequencer.BorderState);

        // Cycle 4: border_state=1, main_border=false -> CSEL=1 transition;
        // memset 8, border_state = main_border = 0
        vic.PixelSequencer.DrawBorder8(mainBorder: false);
        Assert.Equal(0, vic.PixelSequencer.BorderState);
    }

    // -----------------------------------------------------------------------
    // AC-07: COL_D020 resolves through Cregs to live $D020
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-07 (TEST-VIC-BORDER-07, DIVERGENT).
    /// Use case: draw_border8 writes the symbolic code COL_D020 (0x20) into
    /// render_buffer; draw_colors8 resolves it through cregs[0x20] = live $D020
    /// one cycle later (vicii-draw-cycle.c:592-604 color_latency path,
    /// cregs updated via vicii_monitor_colreg_store/update_cregs). The managed
    /// VideoRenderer sampled BorderColor once per scanline instead of running
    /// the cregs pipeline per pixel (Mos6569.cs:344, finding 42).
    /// Acceptance: after at least two border cycles with $D020 = 14 (light blue),
    /// the resolved LineIndices entries for those cycles equal 14.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-07", ParityTag.Divergent, pending: false)]
    public void DrawBorder8_ColD020_ResolvesToLiveD020ViaCregs()
    {
        var vic = BuildVic();
        // No DEN: vertical border stays active, main_border=true all cycles.
        vic.Write(ScreenControl1, 0x13); // YSCROLL=3, no DEN -> vborder active
        const byte BorderPaletteIdx = 14; // light blue (palette index 14)
        vic.Write(BorderColorReg, BorderPaletteIdx);

        // Advance deep into the frame so vertical border is established and
        // border_state has had time to latch to 1 (2+ border cycles at a deep cycle).
        AdvanceTo(vic, 50, 30);
        // At this point DrawBorder8 has run per cycle. Two more ticks to flush ring.
        vic.Tick(); // cycle 31
        vic.Tick(); // cycle 32

        // RasterX is now 32 (after tick) -> line index offset = 32*8 = 256.
        // With color_latency=1 (6569), there's a one-pixel ring delay; pick cycle 30
        // which is well past the warm-up cycles.
        int rasterX = 30;
        int baseOffset = rasterX * 8;
        for (int i = 0; i < 8; i++)
        {
            byte idx = vic.PixelSequencer.LineIndices[baseOffset + i];
            Assert.Equal(BorderPaletteIdx, idx);
        }
    }

    // -----------------------------------------------------------------------
    // AC-11: left check cycle 16 (CSEL=1) / 17 (CSEL=0); managed was 17/18 (+1)
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-11 (TEST-VIC-BORDER-11, DIVERGENT).
    /// Use case: VICE's left border check fires at VICE PAL cycle 17 (1-based) =
    /// managed RasterX 16 for CSEL=1 (L1: ChkBrdL1 at Phi2(17)), and VICE PAL
    /// cycle 18 = managed RasterX 17 for CSEL=0 (L0: ChkBrdL0 at Phi2(18))
    /// (vicii-chip-model.c:145,147; check_hborder vicii-cycle.c:189-195).
    /// The managed LeftBorderCheckCycle was Csel?17:18 (off by +1 from VICE,
    /// finding 43, Mos6569.cs:1193,1245).
    /// Acceptance: on a display line (100) with CSEL=1, IsMainBorderActive is
    /// false (border opened) AT managed RasterX 16 and not 17. With CSEL=0,
    /// it is false at managed RasterX 17 and not 18.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-11", ParityTag.Divergent, pending: false)]
    public void LeftBorderCheck_FiresAtCycle16ForCsel1_AndCycle17ForCsel0()
    {
        // CSEL=1: left check at managed RasterX 16 (VICE PAL 17)
        var vic1 = BuildVic();
        vic1.Write(ScreenControl1, DenStandardText);
        vic1.Write(ScreenControl2, 0x08); // CSEL=1

        // Advance to a display line at cycle 16: border should have just opened.
        AdvanceTo(vic1, DisplayLine, 16);
        Assert.False(vic1.IsMainBorderActive); // opened at RasterX 16

        // CSEL=0: left check at managed RasterX 17 (VICE PAL 18)
        var vic0 = BuildVic();
        vic0.Write(ScreenControl1, DenStandardText);
        vic0.Write(ScreenControl2, 0x00); // CSEL=0

        // At cycle 16 with CSEL=0: check has not fired yet (fires at 17).
        AdvanceTo(vic0, DisplayLine, 16);
        Assert.True(vic0.IsMainBorderActive); // still in border at 16

        AdvanceTo(vic0, DisplayLine, 17);
        Assert.False(vic0.IsMainBorderActive); // opened at RasterX 17
    }

    // -----------------------------------------------------------------------
    // AC-12: right check cycle 55 (CSEL=0) / 56 (CSEL=1); managed was 57/56 (+1)
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-12 (TEST-VIC-BORDER-12, DIVERGENT).
    /// Use case: VICE's right border check fires at VICE PAL cycle 56 (1-based) =
    /// managed RasterX 55 for CSEL=0 (R0: ChkBrdR0 at Phi2(56)), and VICE PAL
    /// cycle 57 = managed RasterX 56 for CSEL=1 (R1: ChkBrdR1 at Phi2(57))
    /// (vicii-chip-model.c:223,225; check_hborder vicii-cycle.c:196-199).
    /// The managed RightBorderCheckCycle was Csel?57:56 (off by +1 for CSEL=1,
    /// finding 43, Mos6569.cs:1205-1206,1247).
    /// Acceptance: on a display line (100) after the border has opened, with
    /// CSEL=0 IsMainBorderActive is true (border closed) AT managed RasterX 55.
    /// With CSEL=1 it is true at managed RasterX 56.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-12", ParityTag.Divergent, pending: false)]
    public void RightBorderCheck_FiresAtCycle55ForCsel0_AndCycle56ForCsel1()
    {
        // CSEL=0: right check at managed RasterX 55 (VICE PAL 56)
        var vic0 = BuildVic();
        vic0.Write(ScreenControl1, DenStandardText);
        vic0.Write(ScreenControl2, 0x00); // CSEL=0

        // Advance to display line, just before right check.
        AdvanceTo(vic0, DisplayLine, 17); // left check fires (CSEL=0), border opens
        AdvanceTo(vic0, DisplayLine, 54); // still in display window
        Assert.False(vic0.IsMainBorderActive); // display area

        AdvanceTo(vic0, DisplayLine, 55);
        Assert.True(vic0.IsMainBorderActive); // right check fires at 55 with CSEL=0

        // CSEL=1: right check at managed RasterX 56 (VICE PAL 57)
        var vic1 = BuildVic();
        vic1.Write(ScreenControl1, DenStandardText);
        vic1.Write(ScreenControl2, 0x08); // CSEL=1

        AdvanceTo(vic1, DisplayLine, 16); // left check fires (CSEL=1), border opens
        AdvanceTo(vic1, DisplayLine, 55); // just before right check
        Assert.False(vic1.IsMainBorderActive); // display area

        AdvanceTo(vic1, DisplayLine, 56);
        Assert.True(vic1.IsMainBorderActive); // right check fires at 56 with CSEL=1
    }
}
