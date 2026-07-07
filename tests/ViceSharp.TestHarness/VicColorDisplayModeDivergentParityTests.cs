namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V4 / TR-PARITY-GATE-001: DIVERGENT (red-now
/// remediation target) parity tests for FR-VIC-DRAW-COLOR (all 10 ACs) and
/// FR-VIC-DISPLAYMODE (AC-03/04/05/06/09) from
/// artifacts/vice-parity-requirements/requirements.yaml.
///
/// FR-VIC-DRAW-COLOR covers the colour resolution pipeline (draw_colors8):
/// cregs[] register file, pixel_buffer ring delay, color_latency 6569/8565
/// grey-dot, pending colour-write pipeline, monitor-store path, and per-pixel
/// dbuf-offset tracking. The managed VideoRenderer resolved colour registers
/// once per line from live _regs[] (VideoRenderer.cs:538-605), losing both the
/// cregs snapshot and the one-pixel 6569 ring delay.
///
/// FR-VIC-DISPLAYMODE AC-03/04/05/06/09 cover per-cycle ECM/BMM/MCM selection
/// timing: 6569 rising-edge OR-in at pixel 4, falling-edge AND-in at pixel 6,
/// 8565 whole-cell update, two-stage MCM pipe, and mid-line mode change. The
/// V3 PixelSequencer already implements these; these tests admit them to the
/// parity coverage manifest.
///
/// VICE sources: native/vice/vice/src/viciisc/vicii-draw-cycle.c.
/// draw_colors8 :627-663, draw_colors_6569 :592-604, draw_colors_8565 :606-624,
/// update_cregs :585-590, vicii_monitor_colreg_store :120-125,
/// vicii_draw_cycle_init :702-706. Mode-edge updates :244-266.
/// </summary>
[Collection("NativeVice")]
public sealed class VicColorDisplayModeDivergentParityTests
{
    private const ushort ScreenControl1  = 0xD011;
    private const ushort ScreenControl2  = 0xD016;
    private const ushort BackgroundColor = 0xD021;

    // $D011 = DEN | RSEL | YSCROLL(3): standard 25-row text mode.
    private const byte DenStandardText = 0x1B;

    // Display cycle inside the 40-col window (RasterX 14-53) on a line
    // safely inside the 25-row vertical window (0x33-0xFB).
    private const ushort DisplayLine  = 100;
    private const byte   DisplayCycle = 30;

    // ----------------------------------------------------------------
    // FR-VIC-DRAW-COLOR: colour resolution pipeline (draw_colors8)
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-01.
    /// Use case: vicii_draw_cycle_init seeds cregs[0x00..0x0F] to identity
    /// and zeros cregs[0x10..0x2E] (vicii-draw-cycle.c:702-706). Managed code
    /// had no cregs array; colour registers were read live from _registers[]
    /// once per rendered line (VideoRenderer.cs:538-605).
    /// Acceptance: after Reset(), PixelSequencer.Cregs[0x00..0x0F] equal their
    /// own index (identity mapping), and Cregs[0x10..0x2E] equal 0x00.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-01", ParityTag.Divergent, pending: false)]
    public void Cregs_InitializedToIdentityLower16_ZeroForRest_AfterReset()
    {
        var vic = BuildVic();

        // cregs[0x00..0x0F] = identity (vicii-draw-cycle.c:702-705)
        for (int i = 0; i < 0x10; i++)
            Assert.Equal((byte)i, vic.PixelSequencer.Cregs[i]);

        // cregs[0x10..0x2E] = 0 (vicii-draw-cycle.c:706)
        for (int i = 0x10; i < 0x2F; i++)
            Assert.Equal(0, vic.PixelSequencer.Cregs[i]);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-02.
    /// Use case: symbolic codes 0x21-0x2E produced by draw_graphics are
    /// resolved through cregs[] in draw_colors8, NOT through the live _regs[]
    /// (vicii-draw-cycle.c:592-623). Managed VideoRenderer resolved via live
    /// _registers[] each line (VideoRenderer.cs:538-605), so an independently
    /// set cregs entry was ignored.
    /// Acceptance: with _regs[$D021]=0 (default) and Cregs[0x21] manually set
    /// to 3, the resolved background pixel in LineIndices equals 3 (cregs
    /// path), not 0 (live _regs path).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-02", ParityTag.Divergent, pending: false)]
    public void DrawColors8_ResolvesSymbolicCode_ViaCregs_NotLiveRegister()
    {
        var vic = BuildVic(gbuf: 0x00); // all background (hires, px=0 -> code 0x21)
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08); // CSEL=1, xscroll=0

        // Live _regs[$D021] stays 0 from Reset (no Write to BackgroundColor).
        // Set Cregs[0x21] = 3 independently - the pipeline must use this, not the live register.
        vic.PixelSequencer.Cregs[0x21] = 3;

        // audit H2: LineIndices now uses VICE's dbuf indexing (dbuf_offset reset
        // at raster cycle 1, vicii-draw-cycle.c:674-677): cycle k's ring-delayed
        // pixels are flushed during cycle k+1 into LineIndices[8k], so advance
        // one cycle past DisplayCycle before reading its pixels.
        AdvanceTo(vic, DisplayLine, (byte)(DisplayCycle + 1));

        int offset = DisplayCycle * 8;
        // draw_colors8 must resolve symbolic code 0x21 via Cregs[0x21]=3, not _regs[0x21]=0.
        Assert.Equal(3, vic.PixelSequencer.LineIndices[offset]);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-03.
    /// Use case: draw_colors8 applies a pending colour-register write to cregs
    /// at the START of each cycle (vicii-draw-cycle.c:636-638). The managed
    /// Mos6569.Write updated _registers immediately with no pipeline delay.
    /// Acceptance: injecting a pending write via VicLastColorRegWrite=0x21/
    /// VicLastColorValueWrite=7 and ticking one cycle results in
    /// Cregs[0x21]==7 (the pending write was consumed by draw_colors8).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-03", ParityTag.Divergent, pending: false)]
    public void DrawColors8_AppliesPendingColorWrite_ToCregs_AtCycleStart()
    {
        var vic = BuildVic();
        // Inject a pending colour write (simulates CPU write to $D021 between cycles).
        vic.VicLastColorRegWrite   = 0x21;
        vic.VicLastColorValueWrite = 7;

        // One tick: draw_colors8 must apply the pending write at the start.
        vic.Tick();

        // cregs[0x21] must be 7 (pending write consumed).
        Assert.Equal(7, vic.PixelSequencer.Cregs[0x21]);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-04.
    /// Use case: 6569 color_latency=1 uses draw_colors_6569 with a one-pixel
    /// pixel_buffer ring delay: lookup_index=(i+1)&amp;7 means the pending
    /// write's effect appears from pixel 1 onwards; pixel 0 outputs the
    /// previous cycle's resolved value (vicii-draw-cycle.c:592-604). Managed
    /// VideoRenderer had no ring buffer (VideoRenderer.cs:131-156).
    /// Acceptance: after a pending write of Cregs[0x21]=5 (old=0), the
    /// background pixel at column 0 of the affected cycle outputs 0 (old
    /// resolved value), while pixel 1 outputs 5 (new cregs value).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-04", ParityTag.Divergent, pending: false)]
    public void DrawColors6569_OnePixelRingDelay_PendingWrite_AffectsPixel1Not0()
    {
        var vic = BuildVic(gbuf: 0x00); // all background pixels
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);

        // Advance to DisplayCycle to seed pixel_buffer. After this cycle,
        // pixel_buffer[0] holds the resolved value of Cregs[0x21]=0 (initial).
        // audit H2: cycle k's pixels are flushed during cycle k+1 into
        // LineIndices[8k] (dbuf_offset reset at raster cycle 1,
        // vicii-draw-cycle.c:674-677), so the batch under test is DisplayCycle's
        // render, flushed during DisplayCycle+1.
        AdvanceTo(vic, DisplayLine, DisplayCycle);

        // Inject pending write between cycles (simulates CPU write during cycle DisplayCycle).
        vic.VicLastColorRegWrite   = 0x21;
        vic.VicLastColorValueWrite = 5; // new bg = 5; old Cregs[0x21] = 0

        // Tick into DisplayCycle+1. draw_colors8 applies pending at start (Cregs[0x21]=5)
        // but pixel_buffer[0] was already resolved to old Cregs[0x21]=0 in previous cycle.
        vic.Tick();

        int offset = DisplayCycle * 8;
        // 6569 ring delay: pixel 0 uses old pixel_buffer value (0, resolved before pending).
        Assert.Equal(0, vic.PixelSequencer.LineIndices[offset + 0]);
        // Pixel 1: resolved with new Cregs[0x21]=5 in current cycle's i=0 step.
        Assert.Equal(5, vic.PixelSequencer.LineIndices[offset + 1]);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-05.
    /// Use case: 8565 color_latency=0 uses draw_colors_8565 with lookup_index=i
    /// (no ring delay), so all 8 pixels in the affected cycle immediately use
    /// the new cregs value (vicii-draw-cycle.c:606-624). Managed code had no
    /// color_latency distinction (VideoRenderer.cs).
    /// Acceptance: with Mos8565 and pending write Cregs[0x21]=5, pixel 0 of
    /// the affected cycle outputs 5 (no delay), contrasting with the 6569
    /// result of 0 from AC-04.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-05", ParityTag.Divergent, pending: false)]
    public void DrawColors8565_NoRingDelay_PendingWrite_AffectsPixel0Immediately()
    {
        var vic = BuildVic8565(gbuf: 0x00); // 8565, all background
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);

        // audit H2: DisplayCycle's pixels are flushed during DisplayCycle+1
        // into LineIndices[DisplayCycle*8] (VICE dbuf indexing).
        AdvanceTo(vic, DisplayLine, DisplayCycle);

        vic.VicLastColorRegWrite   = 0x21;
        vic.VicLastColorValueWrite = 5; // new bg = 5

        vic.Tick(); // DisplayCycle+1 flushes DisplayCycle's batch

        int offset = DisplayCycle * 8;
        // 8565: NO ring delay; pixel 0 uses new Cregs[0x21]=5.
        Assert.Equal(5, vic.PixelSequencer.LineIndices[offset + 0]);
        Assert.Equal(5, vic.PixelSequencer.LineIndices[offset + 1]);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-06.
    /// Use case: 8565 grey-dot: if pixel_buffer[0]==last_color_reg at the
    /// start of draw_colors_8565 for i==0, pixel_buffer[0] is forced to 0x0F
    /// (light-grey) instead of going through cregs lookup
    /// (vicii-draw-cycle.c:613-618). Managed code had no grey-dot logic.
    /// Acceptance: after a CPU write to $D021 during the cycle just before
    /// DisplayCycle (so last_color_reg=0x21 and pixel_buffer[0]=0x21), the
    /// 8565 grey-dot fires and LineIndices[DisplayCycle*8+0] equals 0x0F.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-06", ParityTag.Divergent, pending: false)]
    public void DrawColors8565_GreyDot_Pixel0Forced0x0F_WhenPixelBuffer0EqualsLastColorReg()
    {
        // 8565: after a vis_en cycle, pixel_buffer[i] = render_buffer[i] for each i.
        // For background pixels, render_buffer[0] = 0x21 (bg symbolic code).
        // So pixel_buffer[0] = 0x21 after every vis_en cycle in this mode.
        var vic = BuildVic8565(gbuf: 0x00);
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);

        // Advance to DisplayCycle-1 so there is a vis_en cycle between setup and
        // target (audit H2: cycle k's batch is flushed during cycle k+1 into
        // LineIndices[8k], so the asserted batch is DisplayCycle's render,
        // flushed and grey-dot-checked during DisplayCycle+1).
        AdvanceTo(vic, DisplayLine, (byte)(DisplayCycle - 1));

        // Inject pending write for cycle DisplayCycle: this gets consumed by
        // update_cregs at the end of DisplayCycle, making LastColorReg=0x21
        // for DisplayCycle+1's draw_colors8 check.
        vic.VicLastColorRegWrite   = 0x21;
        vic.VicLastColorValueWrite = 3;

        // Tick to DisplayCycle: update_cregs at end transfers VicLastColorRegWrite
        // to LastColorReg=0x21; pixel_buffer[0] gets render_buffer[0]=0x21.
        vic.Tick();

        // Tick to DisplayCycle+1: grey-dot condition pixel_buffer[0]==last_color_reg
        // (both 0x21) fires; pixel 0 forced to 0x0F.
        vic.Tick();

        int offset = DisplayCycle * 8;
        Assert.Equal(0x0F, vic.PixelSequencer.LineIndices[offset + 0]);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-07.
    /// Use case: update_cregs at the end of draw_colors8 transfers
    /// vicii.last_color_reg into the local pipeline last_color_reg and resets
    /// vicii.last_color_reg to 0xFF (vicii-draw-cycle.c:585-590). Managed code
    /// had no cregs pipeline (VideoRenderer.cs).
    /// Acceptance: after injecting VicLastColorRegWrite=0x21 and ticking one
    /// cycle, VicLastColorRegWrite is reset to 0xFF and
    /// PixelSequencer.LastColorReg equals 0x21 (the transferred value).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-07", ParityTag.Divergent, pending: false)]
    public void UpdateCregs_TransfersLastColorReg_AndResetsChipPending_AfterCycle()
    {
        var vic = BuildVic();
        vic.VicLastColorRegWrite   = 0x21;
        vic.VicLastColorValueWrite = 9;

        vic.Tick(); // draw_colors8 runs; update_cregs at end consumes the pending

        // VicLastColorRegWrite (vicii.last_color_reg) must be reset to 0xFF.
        Assert.Equal(0xFF, vic.VicLastColorRegWrite);
        // LastColorReg (local last_color_reg) must be 0x21 (transferred from chip).
        Assert.Equal(0x21, vic.PixelSequencer.LastColorReg);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-08.
    /// Use case: draw_colors8 maintains vicii.dbuf_offset (+=8/call) and
    /// resets it at the start of each raster line (vicii-draw-cycle.c:626-633).
    /// Managed VideoRenderer used a line-relative pixel index
    /// (VideoRenderer.cs:80,131) with no equivalent global offset.
    /// Acceptance: after advancing to DisplayCycle of a display line,
    /// DbufOffset equals DisplayCycle*8; after advancing to cycle 1 of the
    /// following line (BeginLine reset), DbufOffset equals 8.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-08", ParityTag.Divergent, pending: false)]
    public void DbufOffset_IncrementsBy8PerCycle_ResetsAtLineStart()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        // audit H2: DbufOffset resets at raster cycle 1 exactly like VICE
        // (vicii_draw_cycle, vicii-draw-cycle.c:674-677), executed by
        // Mos6569.Tick before the draws. Cycles 1..DisplayCycle each ran
        // DrawColors8 (+8), so after cycle RasterX=N: DbufOffset = N*8.
        Assert.Equal(DisplayCycle * 8, vic.PixelSequencer.DbufOffset);

        // Advance to cycle 1 of the next line: the reset fires at cycle 1
        // (not at the line wrap), then cycle 1's DrawColors8 increments once:
        // DbufOffset = 8.
        AdvanceTo(vic, (ushort)(DisplayLine + 1), 1);
        Assert.Equal(8, vic.PixelSequencer.DbufOffset);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-09.
    /// Use case: vicii_monitor_colreg_store provides an IMMEDIATE path that
    /// updates cregs[reg] in the same cycle as the monitor write, also setting
    /// last_color_reg and last_color_value for pipeline consistency
    /// (vicii-draw-cycle.c:120-125). Managed code had no cregs mirror.
    /// Acceptance: MonitorColorStore(0x21, 9) immediately sets Cregs[0x21]=9,
    /// LastColorReg=0x21, LastColorValue=9 without waiting for a Tick.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-09", ParityTag.Divergent, pending: false)]
    public void MonitorColorStore_ImmediatelyUpdatesCregs_AndLastColorReg()
    {
        var vic = BuildVic();
        vic.PixelSequencer.MonitorColorStore(0x21, 9);

        Assert.Equal(9,    vic.PixelSequencer.Cregs[0x21]);
        Assert.Equal(0x21, vic.PixelSequencer.LastColorReg);
        Assert.Equal(9,    vic.PixelSequencer.LastColorValue);
    }

    /// <summary>
    /// FR-VIC-DRAW-COLOR AC-10.
    /// Use case: the full-line draw_colors8 stream must match the VICE oracle
    /// including mid-line $D021 writes, because each write goes through the
    /// pending+cregs pipeline (vicii-draw-cycle.c:626-663). The managed
    /// VideoRenderer's geometric path applied the FINAL register value to the
    /// whole line (VideoRenderer.cs:75-157), losing mid-line colour changes.
    /// Acceptance: with initial background=3, a mid-line write of background=7
    /// (between display cycles 14 and 15), the 6569 ring delay means pixel 0
    /// of cycle 15 still outputs the OLD colour (3), while pixel 1 outputs the
    /// NEW colour (7). The managed V3 path (resolving via live _regs) gives 7
    /// for pixel 0, proving the divergence.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DRAW-COLOR-10", ParityTag.Divergent, pending: false)]
    public void DrawColors8_FullLine_MidLineBgChange_RingDelay_OldValueAtPixel0()
    {
        var vic = BuildVic(gbuf: 0x00); // all background pixels
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08);
        // Initial background colour 3. V4: Write() calls MonitorColorStore ->
        // Cregs[0x21]=3; pixel_buffer seeded with 3 after vis_en cycles.
        vic.Write(BackgroundColor, 0x03);

        // Advance to display cycle 18. Display rendering starts at cycle 17
        // (audit H1: the pipe0 load is gated by the PREVIOUS cycle's VISIBLE
        // flag, so the first g-access of cycle 15 renders at cycle 17;
        // DrawBorder8 also early-exits from cycle 17). Cycle 18 is therefore a
        // pure display cycle whose batch (audit H2) is flushed during cycle 19
        // into LineIndices[18*8], matching VICE dbuf[8k] = render(k).
        // After cycle 18, pixel_buffer[0] = Cregs[0x21] = 3 (resolved at i=7),
        // pixel_buffer[1..7] = 0x21 (loaded from RenderBuffer, not yet resolved).
        AdvanceTo(vic, DisplayLine, 18);

        // Mid-line write between cycles 18 and 19.
        // V4: MonitorColorStore -> Cregs[0x21]=7 immediately; VicLastColorReg=0x21.
        // V3: _regs[0x21]=7 immediately; DrawGraphics resolves it next cycle.
        vic.Write(BackgroundColor, 0x07);

        // Tick into cycle 19, which flushes cycle 18's batch:
        // V4: apply pending (VicLastColorRegWrite=0x21 -> Cregs[0x21]=7);
        //     ring delay: pixel_buffer[0] was resolved to OLD Cregs[0x21]=3 in
        //     cycle 18's i=7 step -> LineIndices[18*8+0] = 3 (old colour).
        //     pixel_buffer[1] = Cregs[0x21]=7 (resolved at cycle 19's i=0 step)
        //     -> LineIndices[18*8+1] = 7 (new colour).
        // V3: DrawGraphics resolves 0x21 -> _regs[0x21]=7 for ALL pixels
        //     -> LineIndices[18*8+0] = 7 (fails assertion below).
        vic.Tick();

        int offset18 = 18 * 8;
        // 6569 ring delay: pixel 0 still shows old colour 3 (diverges from V3).
        Assert.Equal(3, vic.PixelSequencer.LineIndices[offset18 + 0]);
        // Pixel 1 already shows new colour 7.
        Assert.Equal(7, vic.PixelSequencer.LineIndices[offset18 + 1]);
    }

    // ----------------------------------------------------------------
    // FR-VIC-DISPLAYMODE: per-cycle ECM/BMM/MCM selection timing
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-03.
    /// Use case: 6569 color_latency=1 applies a rising-edge mode change as an
    /// OR-in to Vmode11Pipe at pixel 4 of each display cycle
    /// (vicii-draw-cycle.c:244-247). The retired managed VideoRenderer sampled
    /// DisplayModeSelection once per line (VideoRenderer.cs:110), losing the
    /// pixel-4 boundary.
    /// Acceptance: advancing a 6569 VIC with ECM=1 ($D011 bit 6) set to a
    /// display cycle results in Vmode11Pipe==0x10 ((ECM bit 6 of $D011)&gt;&gt;2),
    /// proving the OR-in occurred at pixel 4 within the cycle.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DISPLAYMODE-03", ParityTag.Divergent, pending: false)]
    public void Vmode11Pipe_6569_RisingEdgeEcm_OredInAtPixel4()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, (byte)(DenStandardText | 0x40)); // + ECM (bit 6)
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, DisplayLine, DisplayCycle);

        // ECM=1: (D011 & 0x60) >> 2 = 0x40 >> 2 = 0x10. OR-in at pixel 4.
        Assert.Equal(0x10, vic.PixelSequencer.Vmode11Pipe);
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-04.
    /// Use case: 6569 falling-edge mode change uses AND semantics at pixel 6:
    /// Vmode11Pipe &amp;= (regs[$D011]&amp;0x60)&gt;&gt;2 (vicii-draw-cycle.c:252-255),
    /// clearing any ECM/BMM bits that are no longer active. The managed renderer
    /// sampled the mode once per line (VideoRenderer.cs).
    /// Acceptance: after one cycle with ECM=1 (Vmode11Pipe=0x10), clearing
    /// ECM ($D011 bit 6) and ticking one more cycle results in Vmode11Pipe==0
    /// (falling-edge AND-in at pixel 6 cleared the ECM bit).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DISPLAYMODE-04", ParityTag.Divergent, pending: false)]
    public void Vmode11Pipe_6569_FallingEdgeEcm_AndedAtPixel6_ClearsEcm()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, (byte)(DenStandardText | 0x40)); // ECM=1
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(0x10, vic.PixelSequencer.Vmode11Pipe); // ECM active

        // Clear ECM between cycles; falling-edge AND-in at pixel 6 will clear the bit.
        vic.Write(ScreenControl1, DenStandardText); // ECM=0
        vic.Tick();

        // After AND-in: Vmode11Pipe cleared to 0 ((D011 & 0x60) >> 2 = 0).
        Assert.Equal(0, vic.PixelSequencer.Vmode11Pipe);
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-05.
    /// Use case: 8565 (color_latency=0) updates Vmode11Pipe after pixel 7
    /// with the full live register value (vicii-draw-cycle.c:264-266), i.e. a
    /// whole-cell replace rather than OR/AND edges. Managed VideoRenderer
    /// sampled once per line (VideoRenderer.cs).
    /// Acceptance: with Mos8565 and ECM=1 written between two cycles,
    /// Vmode11Pipe equals 0x10 after the following cycle (whole-cell update
    /// vs. 6569 which requires two separate edge events).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DISPLAYMODE-05", ParityTag.Divergent, pending: false)]
    public void Vmode11Pipe_8565_WholeCellUpdate_AfterPixel7()
    {
        var vic = BuildVic8565();
        vic.Write(ScreenControl1, DenStandardText); // ECM=0 initially
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(0, vic.PixelSequencer.Vmode11Pipe);

        // Enable ECM between cycles.
        vic.Write(ScreenControl1, (byte)(DenStandardText | 0x40)); // ECM=1
        vic.Tick();

        // 8565 whole-cell: Vmode11Pipe = (D011 & 0x60) >> 2 = 0x10.
        Assert.Equal(0x10, vic.PixelSequencer.Vmode11Pipe);
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-06.
    /// Use case: MCM activation uses a two-stage pipe: Vmode16Pipe captures the
    /// raw $D016 MCM bit each cycle, Vmode16Pipe2 follows Vmode16Pipe one cycle
    /// later; on the 0-to-1 transition GbufMcFlop is also reset to 0
    /// (vicii-draw-cycle.c:258-261). Managed VideoRenderer applied MCM to the
    /// whole line (VideoRenderer.cs).
    /// Acceptance: with MCM=0 settled, Vmode16Pipe2 is 0; after enabling MCM
    /// and ticking one cycle, Vmode16Pipe2 equals 4 (($D016 &amp; 0x10) &gt;&gt; 2).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DISPLAYMODE-06", ParityTag.Divergent, pending: false)]
    public void Vmode16Pipe2_McmTwoStagePipe_UpdatesOneCycleLate()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenStandardText);
        vic.Write(ScreenControl2, 0x08); // MCM=0

        AdvanceTo(vic, DisplayLine, DisplayCycle);
        Assert.Equal(0, vic.PixelSequencer.Vmode16Pipe2); // settled, MCM off

        vic.Write(ScreenControl2, 0x08 | 0x10); // enable MCM
        vic.Tick();

        // One cycle later: Vmode16Pipe2 reflects MCM=1 -> (0x10 >> 2) = 4.
        Assert.Equal(4, vic.PixelSequencer.Vmode16Pipe2);
    }

    /// <summary>
    /// FR-VIC-DISPLAYMODE AC-09.
    /// Use case: a mid-line mode change (e.g. ECM=0 -> ECM=1) takes effect at
    /// the per-cycle pixel-4/6 boundaries of the FIRST affected cycle; it does
    /// NOT apply retroactively to earlier cells on the same line
    /// (vicii-draw-cycle.c:243-266). The retired managed VideoRenderer applied
    /// the final-register mode to the whole line (VideoRenderer.cs:110).
    /// Acceptance: with ECM=0 for DisplayCycle (Vmode11Pipe=0 after the cycle),
    /// writing ECM=1 and ticking one more cycle yields Vmode11Pipe=0x10,
    /// proving mode change is per-cell and was absent from DisplayCycle output.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-VIC-DISPLAYMODE-09", ParityTag.Divergent, pending: false)]
    public void Vmode11Pipe_MidLineEcmChange_IsPerCell_NotWholeLine()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenStandardText); // ECM=0
        vic.Write(ScreenControl2, 0x08);

        AdvanceTo(vic, DisplayLine, DisplayCycle);

        // ECM was off for DisplayCycle; Vmode11Pipe reflects post-pixel-6 state.
        Assert.Equal(0, vic.PixelSequencer.Vmode11Pipe);

        // Mid-line write ECM=1 (between cells DisplayCycle and DisplayCycle+1).
        vic.Write(ScreenControl1, (byte)(DenStandardText | 0x40)); // ECM=1
        vic.Tick(); // DisplayCycle+1

        // Per-cycle: only DisplayCycle+1 onwards sees ECM mode (from pixel 4 onward).
        // Vmode11Pipe = 0x10 after DisplayCycle+1.
        Assert.Equal(0x10, vic.PixelSequencer.Vmode11Pipe);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds a bare PAL VIC-II 6569 (no memory map), reset to power-on,
    /// with the Phi1 g-access reader fixed to <paramref name="gbuf"/>.
    /// </summary>
    private static Mos6569 BuildVic(byte gbuf = 0x00)
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        vic.Phi1MemoryReader = _ => gbuf;
        return vic;
    }

    /// <summary>
    /// Builds a bare PAL VIC-II 8565 (color_latency=0), reset to power-on,
    /// with the Phi1 g-access reader fixed to <paramref name="gbuf"/>.
    /// </summary>
    private static Mos8565 BuildVic8565(byte gbuf = 0x00)
    {
        var vic = new Mos8565(new BasicBus(), new InterruptLine(InterruptType.Irq));
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
