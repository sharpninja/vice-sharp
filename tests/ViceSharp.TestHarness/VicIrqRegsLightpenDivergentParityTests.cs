namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V2 / TR-PARITY-GATE-001: DIVERGENT (red-now
/// remediation target) parity tests for FR-VIC-RASTER-IRQ, FR-VIC-REGISTERS and
/// FR-VIC-LIGHTPEN from artifacts/vice-parity-requirements/requirements.yaml.
/// One test method per DIVERGENT acceptance criterion; each method asserts the
/// exact VICE mechanism (native/vice/vice/src/viciisc/vicii-cycle.c,
/// vicii-mem.c, vicii-irq.c, vicii-lightpen.c) and starts red against the
/// managed implementation. FAITHFUL criteria live in
/// VicBorderIrqFaithfulParityTests.
///
/// Cycle numbering note: VICE raster cycles are 1-based; the managed VIC uses
/// RasterX = VICII_PAL_CYCLE(n) = n - 1. Managed Tick() increments RasterX
/// first, so after a Tick() returns, the cycle equal to RasterX has just been
/// processed (including its raster-line update and raster-IRQ compare).
///
/// SLICE V2 STOPS (lock-conflict protocol):
/// - TEST-VIC-LIGHTPEN-01 stays pending; remediation conflicts with the
///   FAITHFUL locks TEST-VIC-LIGHTPEN-03/04, whose acceptance pins the
///   current RasterX &gt;&gt; 1 $D013 values without a divergence-ownership
///   note (only TEST-VIC-LIGHTPEN-13 carries one).
/// - TEST-VIC-REGISTERS-15 stays pending; remediation conflicts with the
///   FAITHFUL locks TEST-VIC-REGISTERS-10/11, whose acceptance pins the
///   current raw non-masking Peek without a divergence-ownership note.
/// </summary>
public sealed class VicIrqRegsLightpenDivergentParityTests
{
    private const ushort SpriteX0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteX1 = 0xD002;
    private const ushort SpriteY1 = 0xD003;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort RasterCompareLow = 0xD012;
    private const ushort LightPenX = 0xD013;
    private const ushort LightPenY = 0xD014;
    private const ushort SpriteEnable = 0xD015;
    private const ushort InterruptLatch = 0xD019;
    private const ushort SpriteSpriteCollision = 0xD01E;
    private const ushort SpriteBackgroundCollision = 0xD01F;

    // ----------------------------------------------------------------
    // FR-VIC-RASTER-IRQ: raster-compare interrupt timing and re-arm
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-02.
    /// Use case: the raster IRQ latches at raster_cycle 0, in the same cycle
    /// that raster_line increments: vicii_cycle() bumps the line at
    /// VICII_PAL_CYCLE(1) and then immediately runs the compare
    /// (VICE viciisc/vicii-cycle.c:457-474). The managed VIC checked the
    /// compare before the line-wrap update, so the latch only appeared one
    /// cycle later, at RasterX 1 (finding 44).
    /// Acceptance: with the compare line set to 100, $D019 reads $70 on line
    /// 99 cycle 62, and exactly one tick later, at line 100 cycle 0, it
    /// already reads $71.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-02", ParityTag.Divergent, pending: false)]
    public void RasterIrq_LatchesAtRasterCycle0OfMatchingLine()
    {
        var vic = BuildVic();
        vic.Write(RasterCompareLow, 100);
        vic.Write(InterruptLatch, 0x0F); // Ack the boot-time line-0 latch, if any.

        AdvanceTo(vic, 99, 62);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        vic.Tick(); // Processes raster cycle 0 of line 100: line++ then compare.
        Assert.Equal(100, vic.CurrentRasterLine);
        Assert.Equal(0, vic.RasterX);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-09.
    /// Use case: a $D012 store with an unchanged value returns early: no
    /// register update, no compare-line recompute, and (because VICE writes
    /// never touch raster_irq_triggered) no way to re-fire on the current
    /// line (VICE viciisc/vicii-mem.c:158-169 d012_store). The managed VIC
    /// always updated and re-armed the compare on every $D012 write, so an
    /// unchanged-value rewrite on the matching line double-fired
    /// (finding 45).
    /// Acceptance: with compare line 50 latched at cycle entry and
    /// acknowledged mid-line, rewriting $D012 = 50 (unchanged) leaves $D019
    /// at exactly $70 through the rest of line 50, and the stored compare
    /// value remains 50.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-09", ParityTag.Divergent, pending: false)]
    public void RasterIrq_UnchangedD012Write_ReturnsEarlyWithoutRefire()
    {
        var vic = BuildVic();
        vic.Write(RasterCompareLow, 50);

        AdvanceTo(vic, 50, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));

        vic.Write(InterruptLatch, 0x01); // Acknowledge.
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        vic.Write(RasterCompareLow, 50); // Unchanged value: d012_store early-returns.
        Assert.Equal(0x0032, vic.RasterIrqLine);

        AdvanceTo(vic, 50, 62);
        Assert.Equal(0x70, vic.Read(InterruptLatch)); // VICE: no re-fire on this line.
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-11.
    /// Use case: $D011/$D012 stores never re-arm the raster IRQ: VICE keeps
    /// raster_irq_triggered set for the whole matching line and only the
    /// per-cycle comparison resets it when the line stops matching
    /// (VICE viciisc/vicii-cycle.c:467-474; the stores in vicii-mem.c:145-169
    /// never touch the flag). The managed VIC set its compare-armed flag on
    /// every $D011/$D012 write, so a rewrite on the matching line
    /// double-fired (finding 45). The legitimate VICE re-fire (move the
    /// compare off the line, then back while still on it) must keep working.
    /// Acceptance: after the line-60 latch is acknowledged, writing $D011
    /// with the same bit 7 (compare unchanged at 60) produces no second
    /// latch through cycle 62; on a second chip, moving $D012 to 71 and back
    /// to 70 while on line 70 re-fires exactly once via the non-match to
    /// match edge.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-11", ParityTag.Divergent, pending: false)]
    public void RasterIrq_D011D012Writes_DoNotRearmOnMatchingLine()
    {
        var vic = BuildVic();
        vic.Write(RasterCompareLow, 60);

        AdvanceTo(vic, 60, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
        vic.Write(InterruptLatch, 0x01);

        vic.Write(ScreenControl1, 0x02); // Bit 7 unchanged: compare stays 60.
        Assert.Equal(0x003C, vic.RasterIrqLine);

        AdvanceTo(vic, 60, 62);
        Assert.Equal(0x70, vic.Read(InterruptLatch)); // VICE: the store never re-arms.

        // Positive control: the per-cycle edge (non-match -> match) is the ONLY
        // legitimate re-fire path (vicii-cycle.c:467-474).
        var edge = BuildVic();
        edge.Write(RasterCompareLow, 70);
        AdvanceTo(edge, 70, 1);
        edge.Write(InterruptLatch, 0x01);
        Assert.Equal(0x70, edge.Read(InterruptLatch));

        edge.Write(RasterCompareLow, 71); // Leave the matching line...
        AdvanceTo(edge, 70, 10);          // ...the per-cycle check resets triggered...
        edge.Write(RasterCompareLow, 70); // ...and return to it while still on line 70.
        AdvanceTo(edge, 70, 12);
        Assert.Equal(0x71, edge.Read(InterruptLatch)); // Match edge fires again.
    }

    // ----------------------------------------------------------------
    // FR-VIC-REGISTERS: collision-register read side effects
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-REGISTERS AC-12.
    /// Use case: a $D01E read copies the sprite-sprite collision accumulator
    /// into regs[$1E], schedules clear_collisions = $1E, and returns the
    /// copy WITHOUT clearing the accumulator in the read itself
    /// (VICE viciisc/vicii-mem.c:520-535 d01e_read). The managed VIC zeroed
    /// the accumulator inside Read (finding 46).
    /// Acceptance: with sprites 0 and 1 overlapped, reading $D01E returns
    /// $03 and the non-destructive Peek still reports the accumulator as
    /// $03 immediately after the read (no in-read clear).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-12", ParityTag.Divergent, pending: false)]
    public void Registers_D01ERead_CopiesMaskAndDoesNotClearInRead()
    {
        var vic = BuildCollisionVic();
        ConfigureOverlappingSpritePair(vic);

        AdvanceTo(vic, 130, 10);
        Assert.Equal(0x03, vic.Peek(SpriteSpriteCollision));

        Assert.Equal(0x03, vic.Read(SpriteSpriteCollision));
        Assert.Equal(0x03, vic.Peek(SpriteSpriteCollision)); // VICE: still latched.
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-13.
    /// Use case: a $D01F read has the same deferred-clear contract as $D01E:
    /// copy the sprite-background accumulator into regs[$1F], schedule
    /// clear_collisions = $1F, return the copy, and leave the accumulator
    /// untouched during the read (VICE viciisc/vicii-mem.c:537-559
    /// d01f_read). The managed VIC cleared it inside Read (finding 46).
    /// Acceptance: with sprite 0 over an all-foreground character area,
    /// reading $D01F returns $01 and Peek still reports the accumulator as
    /// $01 immediately after the read.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-13", ParityTag.Divergent, pending: false)]
    public void Registers_D01FRead_CopiesMaskAndDoesNotClearInRead()
    {
        var vic = BuildCollisionVic(bgPattern: 0xFF);
        ConfigureSingleSpriteOverForeground(vic);

        AdvanceTo(vic, 130, 10);
        Assert.Equal(0x01, vic.Peek(SpriteBackgroundCollision));

        Assert.Equal(0x01, vic.Read(SpriteBackgroundCollision));
        Assert.Equal(0x01, vic.Peek(SpriteBackgroundCollision)); // VICE: still latched.
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-14.
    /// Use case: the scheduled clear_collisions zeroes the collision
    /// accumulator in the NEXT vicii_cycle(), not in the read: the cycle
    /// after a $D01E/$D01F read wipes sprite_sprite_collisions or
    /// sprite_background_collisions and resets clear_collisions
    /// (VICE viciisc/vicii-cycle.c:413-425). The managed VIC had no deferral
    /// at all (finding 46).
    /// Acceptance: after a mid-line $D01E read returns $03 the accumulator
    /// survives until the next Tick and reads $00 (Peek and Read) right
    /// after it; the same single-cycle deferral holds for $D01F.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-14", ParityTag.Divergent, pending: false)]
    public void Registers_CollisionClear_LandsOnTheNextCycle()
    {
        var ss = BuildCollisionVic();
        ConfigureOverlappingSpritePair(ss);
        AdvanceTo(ss, 130, 10);

        Assert.Equal(0x03, ss.Read(SpriteSpriteCollision));
        Assert.Equal(0x03, ss.Peek(SpriteSpriteCollision)); // Deferred: not yet cleared.
        ss.Tick();                                          // vicii-cycle.c:413-425 clears here.
        Assert.Equal(0x00, ss.Peek(SpriteSpriteCollision));
        Assert.Equal(0x00, ss.Read(SpriteSpriteCollision));

        var sb = BuildCollisionVic(bgPattern: 0xFF);
        ConfigureSingleSpriteOverForeground(sb);
        AdvanceTo(sb, 130, 10);

        Assert.Equal(0x01, sb.Read(SpriteBackgroundCollision));
        Assert.Equal(0x01, sb.Peek(SpriteBackgroundCollision));
        sb.Tick();
        Assert.Equal(0x00, sb.Peek(SpriteBackgroundCollision));
        Assert.Equal(0x00, sb.Read(SpriteBackgroundCollision));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-15.
    /// Use case: the monitor peek returns regs | unused_bits for the default
    /// registers plus the live views: $D011 = (regs &amp; $7F) | ((raster_y
    /// &amp; $100) &gt;&gt; 1), $D012 = live raster low byte, $D019 =
    /// irq_status | $70, $D01E/$D01F = the raw collision accumulators
    /// (VICE viciisc/vicii-mem.c:742-770 vicii_peek with the
    /// unused_bits_in_registers table at :48-63). The managed Peek returns
    /// the raw backing store (finding 46).
    /// Acceptance: Peek($D020) reads $F5 after writing colour 5, Peek($D02F)
    /// reads $FF, Peek($D011) merges the live raster bit 8 instead of the
    /// stored compare bit, and Peek($D019) carries the $70 floor.
    /// SLICE V2 STOP: remediation conflicts with the FAITHFUL locks
    /// TEST-VIC-REGISTERS-10 (Peek($D020-$D02E) returns the raw low nibble
    /// through "the non-masking debug Peek") and TEST-VIC-REGISTERS-11
    /// (Peek($D02F/$D03F) returns $00), neither of which carries a
    /// divergence-ownership note for this AC. Kept pending until the parity
    /// plan owner re-bases those locks.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-15", ParityTag.Divergent, pending: false)]
    public void Registers_Peek_ReturnsRegsWithUnusedBitsAndLiveViews()
    {
        var vic = BuildVic();

        vic.Write(0xD020, 0x05);
        Assert.Equal(0xF5, vic.Peek(0xD020)); // regs | 0xF0 (vicii-mem.c:768).

        Assert.Equal(0xFF, vic.Peek(0xD02F)); // unused_bits_in_registers = 0xFF.

        vic.Write(ScreenControl1, 0x9B);      // Stored bit 7 = 1, raster on line < 256.
        Assert.Equal(0x1B, vic.Peek(ScreenControl1)); // Live raster bit 8 = 0 (vicii-mem.c:754).

        AdvanceTo(vic, 0x134, 5);
        Assert.Equal(0x9B, vic.Peek(ScreenControl1)); // Live raster bit 8 = 1.
        Assert.Equal(0x34, vic.Peek(RasterCompareLow)); // Live raster low byte (vicii-mem.c:756).

        Assert.Equal(0x70, (byte)(vic.Peek(InterruptLatch) & 0x70)); // d019_peek | 0x70 (vicii-mem.c:742-745).
    }

    // ----------------------------------------------------------------
    // FR-VIC-LIGHTPEN: trigger pipeline ($D013/$D014)
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-01.
    /// Use case: an accepted light-pen trigger latches $D013 from the
    /// chip-model xpos of the current cycle: x =
    /// cycle_get_xpos(cycle_table[raster_cycle]) / 2 + x_extra_bits, where
    /// the PAL Phi1 xpos is ((0x194 + 8 * raster_cycle) mod 0x1F8) with the
    /// low three bits cleared by cycle_get_xpos
    /// (VICE viciisc/vicii-lightpen.c:75,78,100 with
    /// vicii-chip-model.h:164-167 and the pack at vicii-chip-model.c:766-767).
    /// The managed VIC latches RasterX &gt;&gt; 1 with no extra bits
    /// (finding 47).
    /// Acceptance: a trigger at line 100 cycle 40 latches $D013 =
    /// ((0x194 + 8 * 40) mod 0x1F8 &amp; ~7) / 2 = $6C (and $D014 = $64).
    /// SLICE V2 STOP: remediation conflicts with the FAITHFUL locks
    /// TEST-VIC-LIGHTPEN-03/04, whose acceptance pins RasterX &gt;&gt; 1
    /// $D013 values ($02) without a divergence-ownership note; only
    /// TEST-VIC-LIGHTPEN-13 acknowledges this AC as the owner. Kept pending
    /// until the parity plan owner re-bases those locks.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-01", ParityTag.Divergent, pending: false)]
    public void LightPen_D013_LatchesCycleXposOverTwo()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 100, 40);
        vic.TriggerLightPen();

        Assert.Equal(0x6C, vic.Read(LightPenX)); // cycle_get_xpos(40)/2 = 0xD8/2.
        Assert.Equal(0x64, vic.Read(LightPenY));
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-05.
    /// Use case: a trigger on the last raster line is swallowed except on
    /// the line's first cycle: the internal trigger sets the once-per-frame
    /// flag, then returns WITHOUT latching x/y or firing the IRQ when
    /// y == screen_height - 1 and raster_cycle &gt; 0
    /// (VICE viciisc/vicii-lightpen.c:66-73). The managed VIC had no such
    /// guard and latched normally on the last line (finding 47).
    /// Acceptance: a trigger at line 311 cycle 10 leaves $D013/$D014 at $00
    /// and $D019 bit 3 clear, consumes the frame's trigger (a second
    /// same-frame trigger also does nothing), and the next frame's trigger
    /// latches normally ($D014 = 5).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-05", ParityTag.Divergent, pending: false)]
    public void LightPen_LastLineGuard_SwallowsTriggerAfterCycle0()
    {
        var vic = BuildVic();

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 10);
        vic.TriggerLightPen();
        Assert.Equal(0x00, vic.Read(LightPenX));
        Assert.Equal(0x00, vic.Read(LightPenY));
        Assert.Equal(0x00, vic.Read(InterruptLatch) & 0x08);

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 40);
        vic.TriggerLightPen(); // Frame guard already consumed: still swallowed.
        Assert.Equal(0x00, vic.Read(LightPenY));
        Assert.Equal(0x00, vic.Read(InterruptLatch) & 0x08);

        AdvanceTo(vic, 5, 4); // Next frame re-arms (vicii-cycle.c:210).
        vic.TriggerLightPen();
        Assert.Equal(0x05, vic.Read(LightPenY));
        Assert.Equal(0x08, vic.Read(InterruptLatch) & 0x08);
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-06.
    /// Use case: a light-pen line edge does not latch immediately: the pen
    /// input schedules the trigger one clock later (trigger_cycle = mclk + 1
    /// in vicii_set_light_pen, fired at the end of the vicii_cycle whose
    /// clock matches: VICE viciisc/vicii-lightpen.c:38-47 with
    /// vicii-cycle.c:610-613). The managed pen path latched in the same
    /// cycle as the edge (finding 47).
    /// Acceptance: a pen edge raised at line 120 cycle 62 has latched
    /// nothing before the next tick ($D019 bit 3 clear), and after exactly
    /// one tick the latch carries the NEXT cycle's coordinates:
    /// $D014 = 121 (the wrapped line) with $D019 bit 3 set.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-06", ParityTag.Divergent, pending: false)]
    public void LightPen_PenEdge_LatchesOneClockLater()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 120, 62);
        vic.SetLightPen(true);
        Assert.Equal(0x00, vic.Read(InterruptLatch) & 0x08); // Not latched yet.
        Assert.Equal(0x00, vic.Read(LightPenY));

        vic.Tick(); // trigger_cycle == mclk: latch at the end of this cycle.
        Assert.Equal(0x08, vic.Read(InterruptLatch) & 0x08);
        Assert.Equal(121, vic.Read(LightPenY)); // Line already advanced by the delay.
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-07.
    /// Use case: the pen path adds the chip-model x offset into $D013:
    /// x_extra_bits = color_latency ? 2 : 1 is set on the pen edge and added
    /// to the latched x (VICE viciisc/vicii-lightpen.c:42,78 with the
    /// chip-model color latency: 6569 = 1, 8565 = 0 per
    /// vicii-chip-model.c:240-268). The managed VIC had no x_extra_bits at
    /// all (finding 47).
    /// Acceptance: a pen-edge latch on the 6569 reads exactly 2 higher in
    /// $D013 than a direct internal trigger at the same raster cycle, and
    /// exactly 1 higher on the 8565 (color latency off).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-07", ParityTag.Divergent, pending: false)]
    public void LightPen_XExtraBits_AddColorLatencyOffsetIntoD013()
    {
        var pen6569 = BuildVic();
        AdvanceTo(pen6569, 50, 21);
        pen6569.SetLightPen(true);
        pen6569.Tick(); // Latch lands at cycle 22 with x_extra_bits = 2.

        var direct6569 = BuildVic();
        AdvanceTo(direct6569, 50, 22);
        direct6569.TriggerLightPen(); // Internal trigger: no extra bits.

        Assert.Equal(50, pen6569.Read(LightPenY));
        Assert.Equal(direct6569.Read(LightPenX) + 2, pen6569.Read(LightPenX));

        var pen8565 = Build8565();
        AdvanceTo(pen8565, 50, 21);
        pen8565.SetLightPen(true);
        pen8565.Tick();

        var direct8565 = Build8565();
        AdvanceTo(direct8565, 50, 22);
        direct8565.TriggerLightPen();

        Assert.Equal(direct8565.Read(LightPenX) + 1, pen8565.Read(LightPenX));
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-08.
    /// Use case: when the light-pen line is still held low at the start of a
    /// frame, the frame start clears the once-per-frame flag, reloads
    /// x_extra_bits, and immediately retriggers
    /// (vicii_trigger_light_pen_internal(1)); the managed VIC only cleared
    /// the triggered flag at the wrap (VICE viciisc/vicii-cycle.c:202-218,
    /// finding 47).
    /// Acceptance: with the pen held low across the frame boundary, the
    /// acknowledged $D019 bit 3 latches again at the frame start with
    /// $D014 = 0, and the retrigger consumes the new frame's single shot
    /// (a later same-frame trigger does not overwrite the latch).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-08", ParityTag.Divergent, pending: false)]
    public void LightPen_HeldLow_RetriggersAtFrameStart()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 200, 10);
        vic.SetLightPen(true);
        vic.Tick();
        Assert.Equal(0x08, vic.Read(InterruptLatch) & 0x08);
        vic.Write(InterruptLatch, 0x08); // Acknowledge; pen stays low.
        Assert.Equal(0x00, vic.Read(InterruptLatch) & 0x08);

        AdvanceTo(vic, 1, 5); // Crosses the frame start with the line still low.
        Assert.Equal(0x08, vic.Read(InterruptLatch) & 0x08); // Retriggered.
        Assert.Equal(0x00, vic.Read(LightPenY));             // y = raster_line = 0.

        vic.TriggerLightPen(); // Single shot consumed by the retrigger.
        Assert.Equal(0x00, vic.Read(LightPenY));
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-09.
    /// Use case: the frame-start retrigger does not use the xpos formula:
    /// it forces $D013 to $D1 on 63-cycle lines (PAL default) and $D5 on
    /// 65-cycle lines (VICE viciisc/vicii-lightpen.c:81-92). The managed VIC
    /// had no retrigger at all (finding 47).
    /// Acceptance: holding the pen low across a frame boundary latches
    /// $D013 = $D1 on the 63-cycle PAL chip and $D013 = $D5 on a 65-cycle
    /// NTSC-timed chip.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-09", ParityTag.Divergent, pending: false)]
    public void LightPen_FrameStartRetrigger_ForcesModelXValue()
    {
        var pal = BuildVic();
        AdvanceTo(pal, 200, 10);
        pal.SetLightPen(true);
        pal.Tick();
        AdvanceTo(pal, 1, 5);
        Assert.Equal(0xD1, pal.Read(LightPenX)); // 63 cycles per line.

        var ntsc = BuildVic();
        ntsc.ConfigureTiming(
            Mos6569.TvSystem.NTSC,
            Mos6569.NtscCyclesPerLine,
            Mos6569.NtscVisibleLines,
            Mos6569.NtscTotalLines,
            1_022_730d / (Mos6569.NtscCyclesPerLine * Mos6569.NtscTotalLines));
        AdvanceTo(ntsc, 200, 10);
        ntsc.SetLightPen(true);
        ntsc.Tick();
        AdvanceTo(ntsc, 1, 5);
        Assert.Equal(0xD5, ntsc.Read(LightPenX)); // 65 cycles per line.
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-10.
    /// Use case: the 6569R1 uses the old light-pen IRQ mode: a normal
    /// trigger latches x/y but never fires the IRQ; only the frame-start
    /// retrigger (line low on the first cycle of the frame) raises $D019
    /// bit 3 (VICE viciisc/vicii-lightpen.c:93-98,105-107 with
    /// lightpen_old_irq_mode = 1 in vicii-chip-model.c:240-248). The managed
    /// R1 fired on every normal trigger like the R3 (finding 47).
    /// Acceptance: on the 6569R1 a direct trigger latches $D014 = 100 with
    /// $D019 bit 3 still clear, and holding the pen low across the next
    /// frame boundary fires bit 3 via the retrigger.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-10", ParityTag.Divergent, pending: false)]
    public void LightPen_6569R1_FiresIrqOnlyOnFrameStartRetrigger()
    {
        var r1 = BuildR1();

        AdvanceTo(r1, 100, 10);
        r1.TriggerLightPen();
        Assert.Equal(100, r1.Read(LightPenY));               // Latch still happens...
        Assert.Equal(0x00, r1.Read(InterruptLatch) & 0x08);  // ...but no IRQ (old mode).

        r1.SetLightPen(true); // Hold the line low into the next frame.
        AdvanceTo(r1, 1, 5);
        Assert.Equal(0x08, r1.Read(InterruptLatch) & 0x08);  // Retrigger fires (:96).
        Assert.Equal(0xD1, r1.Read(LightPenX));
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-14.
    /// Use case: x_extra_bits is consumed by the latch: after writing
    /// light_pen.x the trigger resets x_extra_bits to 0, so a later trigger
    /// without a fresh pen edge carries no stale offset
    /// (VICE viciisc/vicii-lightpen.c:100-103). The managed VIC had no such
    /// state at all (finding 47).
    /// Acceptance: a pen-edge latch on the 6569 carries the +2 offset
    /// exactly once: the next frame's direct trigger at the same raster
    /// cycle latches the same $D013 as a control chip that never saw a pen
    /// edge.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-14", ParityTag.Divergent, pending: false)]
    public void LightPen_XExtraBits_ResetToZeroAfterEachLatch()
    {
        var pen = BuildVic();
        AdvanceTo(pen, 50, 21);
        pen.SetLightPen(true);
        pen.Tick(); // Latch at cycle 22 consumes x_extra_bits (+2, then reset).
        byte penEdgeX = pen.Read(LightPenX);
        pen.SetLightPen(false); // Release before the frame start: no retrigger.

        var control = BuildVic();
        AdvanceTo(control, 60, 22);
        control.TriggerLightPen(); // Never saw a pen edge: extra bits 0.
        byte controlX = control.Read(LightPenX);

        Assert.Equal(controlX + 2, penEdgeX); // The offset was applied once...

        AdvanceTo(pen, 0, 5);   // Cross the frame boundary (re-arms the single shot)...
        AdvanceTo(pen, 60, 22); // ...and the next frame's direct trigger has none.
        pen.TriggerLightPen();
        Assert.Equal(controlX, pen.Read(LightPenX));
        Assert.Equal(60, pen.Read(LightPenY));
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds a bare PAL 6569 reset to the canonical power-on phase
    /// (line 0, RasterX 6).
    /// </summary>
    private static Mos6569 BuildVic()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>Builds a bare 6569R1 (old light-pen IRQ mode), reset.</summary>
    private static Mos6569R1 BuildR1()
    {
        var vic = new Mos6569R1(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>Builds a bare 8565 (color latency off), reset.</summary>
    private static Mos8565 Build8565()
    {
        var vic = new Mos8565(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>
    /// Builds a VIC whose memory reader yields fully opaque sprite rows and a
    /// configurable character bitmap, mirroring the deterministic collision
    /// scenario of SpriteCollisionTests (sprite pointers at $03F8+, data
    /// $FF, background pattern selectable).
    /// </summary>
    private static Mos6569 BuildCollisionVic(byte bgPattern = 0x00)
    {
        const byte spriteDataBlock = 0x0D;
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        vic.VideoMemoryReader = addr =>
        {
            ushort masked = (ushort)(addr & 0x3FFF);
            if (masked >= 0x03F8 && masked <= 0x03FF)
                return spriteDataBlock;

            ushort spriteBase = spriteDataBlock * 64;
            if (masked >= spriteBase && masked < spriteBase + 64)
                return 0xFF;

            return bgPattern;
        };
        // VICE draw_sprites (viciisc/vicii-draw-cycle.c) derives the
        // sprite-background collision foreground bit from the g-access byte, not
        // the video-II p/c-access. VideoMemoryReader covers the p/c-accesses;
        // Phi1MemoryReader supplies the g-access character/bitmap row data the
        // V6 per-pixel PriBuffer path reads. Mirror the wiring already used by
        // SpriteCollisionTests / VicIISpriteCollisionIrqTests so $D01F latches.
        vic.Phi1MemoryReader = _ => bgPattern;
        vic.Write(ScreenControl1, 0x1B); // DEN=1, RSEL=1, YSCROLL=3.
        vic.Write(0xD016, 0x08);         // CSEL=1.
        return vic;
    }

    private static void ConfigureOverlappingSpritePair(Mos6569 vic)
    {
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1, 100);
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);
    }

    private static void ConfigureSingleSpriteOverForeground(Mos6569 vic)
    {
        vic.Write(SpriteX0, 100);
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteEnable, 0x01);
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 3;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;

            vic.Tick();
        }

        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }
}
