namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8 / TR-PARITY-GATE-001: FAITHFUL (green-now) parity
/// regression locks for FR-VIC-BORDER, FR-VIC-RASTER-IRQ, FR-VIC-REGISTERS,
/// and FR-VIC-LIGHTPEN from artifacts/vice-parity-requirements/requirements.yaml.
/// One test method per FAITHFUL acceptance criterion; each drives the managed
/// Mos6569 alone (deterministic, managed-only) and asserts the exact register,
/// flip-flop, and IRQ-line behavior that already matches VICE
/// (native/vice/vice/src/viciisc). DIVERGENT criteria are remediation targets
/// covered by separate red-first tests, not by this file.
/// Use case: guard the already-VICE-faithful VIC-II border flip-flop,
/// raster-IRQ, register-mask, and light-pen behaviors against regression.
/// Acceptance: every [ParityAc]-tagged FAITHFUL test in this file passes
/// against the current managed implementation with exact-value asserts.
/// </summary>
public sealed class VicBorderIrqFaithfulParityTests
{
    private const ushort SpriteXLow0 = 0xD000;
    private const ushort SpriteY0 = 0xD001;
    private const ushort SpriteXMsb = 0xD010;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort RasterCompareLow = 0xD012;
    private const ushort LightPenX = 0xD013;
    private const ushort LightPenY = 0xD014;
    private const ushort SpriteEnable = 0xD015;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort SpriteExpandY = 0xD017;
    private const ushort MemoryPointers = 0xD018;
    private const ushort InterruptLatch = 0xD019;
    private const ushort InterruptEnable = 0xD01A;
    private const ushort SpritePriorityRegister = 0xD01B;
    private const ushort SpriteMulticolorRegister = 0xD01C;
    private const ushort SpriteExpandX = 0xD01D;
    private const ushort SpriteSpriteCollision = 0xD01E;
    private const ushort SpriteBackgroundCollision = 0xD01F;
    private const ushort UnusedD02F = 0xD02F;
    private const ushort UnusedD03F = 0xD03F;

    private static Mos6569 BuildVic(out IInterruptLine irq)
    {
        irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    private static void Advance(Mos6569 vic, int cycles)
    {
        for (var i = 0; i < cycles; i++) vic.Tick();
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;
            vic.Tick();
        }
        throw new InvalidOperationException($"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    // ------------------------------------------------------------------
    // FR-VIC-BORDER
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-BORDER AC-08 (TEST-VIC-BORDER-08, FAITHFUL): check_vborder_top
    /// at line 51 (RSEL=1) / 55 (RSEL=0) with DEN clears vborder and set_vborder.
    /// VICE native/vice/vice/src/viciisc/vicii-cycle.c:165-173; managed
    /// Mos6569.CheckVerticalBorderTopForCurrentLine (Mos6569.cs:1209-1216).
    /// Use case: the vertical border flip-flop opens at the RSEL-selected top
    /// display line only when the display-enable bit ($D011 bit 4) is set.
    /// Acceptance: with DEN+RSEL the flip-flop clears entering line 51; with
    /// DEN only (RSEL=0) it stays set through line 51 and clears entering
    /// line 55; with DEN off it never clears on either boundary line.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-08", ParityTag.Faithful)]
    public void Border_TopCheck_ClearsVerticalBorderAtDenGatedStartLine()
    {
        var rsel1 = BuildVic(out _);
        rsel1.Write(ScreenControl1, 0x18); // DEN + RSEL
        AdvanceTo(rsel1, 50, 62);
        Assert.True(rsel1.IsVerticalBorderActive);
        AdvanceTo(rsel1, 51, 0);
        Assert.False(rsel1.IsVerticalBorderActive);

        var rsel0 = BuildVic(out _);
        rsel0.Write(ScreenControl1, 0x10); // DEN, RSEL=0
        AdvanceTo(rsel0, 51, 30);
        Assert.True(rsel0.IsVerticalBorderActive);
        AdvanceTo(rsel0, 55, 0);
        Assert.False(rsel0.IsVerticalBorderActive);

        var denOff = BuildVic(out _);
        AdvanceTo(denOff, 51, 0);
        Assert.True(denOff.IsVerticalBorderActive);
        AdvanceTo(denOff, 55, 0);
        Assert.True(denOff.IsVerticalBorderActive);
        AdvanceTo(denOff, 100, 0);
        Assert.True(denOff.IsVerticalBorderActive);
    }

    /// <summary>
    /// FR-VIC-BORDER AC-09 (TEST-VIC-BORDER-09, FAITHFUL): check_vborder_bottom
    /// at line 251 (RSEL=1) / 247 (RSEL=0) sets set_vborder=1.
    /// VICE native/vice/vice/src/viciisc/vicii-cycle.c:175-182; managed
    /// Mos6569.CheckVerticalBorderBottomForCurrentLine (Mos6569.cs:1218-1222).
    /// Use case: the bottom compare raises the pending vertical border state at
    /// the RSEL-selected stop line; the flip-flop takes the value at the left
    /// border check and the line renders as border.
    /// Acceptance: with DEN+RSEL the flip-flop is clear on line 250 and set
    /// from the left check of line 251; per-line captures report 250 open and
    /// 251 bordered. With RSEL=0 the captured boundary moves to 246/247.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-09", ParityTag.Faithful)]
    public void Border_BottomCheck_RaisesSetVerticalBorderAtRselStopLine()
    {
        var rsel1 = BuildVic(out _);
        rsel1.Write(ScreenControl1, 0x18); // DEN + RSEL
        rsel1.Write(ScreenControl2, 0x08); // CSEL=1: managed left check at cycle 17
        AdvanceTo(rsel1, 250, 30);
        Assert.False(rsel1.IsVerticalBorderActive);
        AdvanceTo(rsel1, 251, 17);
        Assert.True(rsel1.IsVerticalBorderActive);
        AdvanceTo(rsel1, 252, 5);
        Assert.False(rsel1.IsRasterLineVerticalBorderActive(250));
        Assert.True(rsel1.IsRasterLineVerticalBorderActive(251));

        var rsel0 = BuildVic(out _);
        rsel0.Write(ScreenControl1, 0x10); // DEN, RSEL=0
        AdvanceTo(rsel0, 248, 5);
        Assert.False(rsel0.IsRasterLineVerticalBorderActive(246));
        Assert.True(rsel0.IsRasterLineVerticalBorderActive(247));
    }

    /// <summary>
    /// FR-VIC-BORDER AC-10 (TEST-VIC-BORDER-10, FAITHFUL): the left border
    /// check re-runs the bottom compare, copies set_vborder into vborder, and
    /// clears main_border only when vborder is 0.
    /// VICE native/vice/vice/src/viciisc/vicii-cycle.c:189-195; managed
    /// Mos6569.UpdateBorderFlipFlopsForCurrentCycle (Mos6569.cs:1193-1203).
    /// Use case: on display lines the main border opens at the left check; on
    /// the bottom stop line the freshly copied vertical border keeps the main
    /// border closed for the whole line.
    /// Acceptance: on line 100 the main border flip-flop is clear after the
    /// left check; on line 251 the vertical border is set after the left check
    /// and the main border flip-flop stays set through the display window.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-10", ParityTag.Faithful)]
    public void Border_LeftCheck_CopiesSetVBorderAndOpensMainBorderOnlyWhenClear()
    {
        var vic = BuildVic(out _);
        vic.Write(ScreenControl1, 0x18); // DEN + RSEL
        vic.Write(ScreenControl2, 0x08); // CSEL=1: managed left check at cycle 17

        AdvanceTo(vic, 100, 17);
        Assert.False(vic.IsVerticalBorderActive);
        Assert.False(vic.IsMainBorderActive);

        AdvanceTo(vic, 251, 16);
        Assert.True(vic.IsMainBorderActive);
        AdvanceTo(vic, 251, 17);
        Assert.True(vic.IsVerticalBorderActive);
        Assert.True(vic.IsMainBorderActive);
        AdvanceTo(vic, 251, 40);
        Assert.True(vic.IsMainBorderActive);
    }

    /// <summary>
    /// FR-VIC-BORDER AC-13 (TEST-VIC-BORDER-13, FAITHFUL): CSEL selects which
    /// left/right border check cycle pair is armed (L1/R1 for CSEL=1, L0/R0 for
    /// CSEL=0). VICE PAL table: L1 at Phi2(17) = managed RasterX 16 (CSEL=1),
    /// L0 at Phi2(18) = managed RasterX 17 (CSEL=0), R0 at Phi2(56) = managed
    /// RasterX 55 (CSEL=0), R1 at Phi2(57) = managed RasterX 56 (CSEL=1).
    /// vicii-chip-model.c PAL table lines 145,147,223,225; managed
    /// Mos6569.LeftBorderCheckCycle / RightBorderCheckCycle (Mos6569.cs).
    /// Use case: CSEL=1 (40 columns) opens the main border one cycle earlier
    /// and closes it one cycle later than CSEL=0 (38 columns).
    /// Acceptance: with CSEL=1 the main border flip-flop clears at RasterX 16
    /// and sets at 56; with CSEL=0 it clears at 17 and sets at 55 (VICE-correct
    /// cycles aligned to TEST-VIC-BORDER-11/12 in V7 of PLAN-VICEPARITY-001).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-13", ParityTag.Faithful)]
    public void Border_CselSelectsLeftRightBorderCheckCyclePair()
    {
        var vic = BuildVic(out _);
        vic.Write(ScreenControl1, 0x18); // DEN + RSEL
        vic.Write(ScreenControl2, 0x08); // CSEL=1

        // CSEL=1: left check fires at managed RasterX 16 (VICE PAL Phi2(17)).
        AdvanceTo(vic, 100, 15);
        Assert.True(vic.IsMainBorderActive);   // still in border before check
        AdvanceTo(vic, 100, 16);
        Assert.False(vic.IsMainBorderActive);  // border opens at 16
        // CSEL=1: right check fires at managed RasterX 56 (VICE PAL Phi2(57)).
        AdvanceTo(vic, 100, 55);
        Assert.False(vic.IsMainBorderActive);  // still in display before check
        AdvanceTo(vic, 100, 56);
        Assert.True(vic.IsMainBorderActive);   // border closes at 56

        AdvanceTo(vic, 101, 0);
        vic.Write(ScreenControl2, 0x00); // CSEL=0
        // CSEL=0: left check fires at managed RasterX 17 (VICE PAL Phi2(18)).
        AdvanceTo(vic, 101, 16);
        Assert.True(vic.IsMainBorderActive);   // still in border before check
        AdvanceTo(vic, 101, 17);
        Assert.False(vic.IsMainBorderActive);  // border opens at 17
        // CSEL=0: right check fires at managed RasterX 55 (VICE PAL Phi2(56)).
        AdvanceTo(vic, 101, 54);
        Assert.False(vic.IsMainBorderActive);  // still in display before check
        AdvanceTo(vic, 101, 55);
        Assert.True(vic.IsMainBorderActive);   // border closes at 55
    }

    /// <summary>
    /// FR-VIC-BORDER AC-14 (TEST-VIC-BORDER-14, FAITHFUL): vborder reflects
    /// set_vborder at raster cycle 0 (start-of-line processing).
    /// VICE native/vice/vice/src/viciisc/vicii-cycle.c:480-482; managed
    /// start-of-line update Mos6569.UpdateVerticalBorderForLineStart
    /// (Mos6569.cs:1005,1184-1189). This lock scopes to the DEN top edge,
    /// where the reflection is observable at cycle 0; the bottom-edge copy in
    /// managed lands at the left check (TEST-VIC-BORDER-09/10 mechanism) with
    /// border output identical to VICE because main_border covers the
    /// intervening cycles.
    /// Use case: the display window opens at the very start of the top display
    /// line rather than waiting for the left border check.
    /// Acceptance: the vertical border flip-flop is set on the last cycle of
    /// line 50 and clear at cycle 0 of line 51 (DEN+RSEL); per-line captures
    /// report 50/251 bordered and 51/250 open.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-BORDER-14", ParityTag.Faithful)]
    public void Border_StartOfLine_ReflectsSetVBorderIntoVerticalBorder()
    {
        var vic = BuildVic(out _);
        vic.Write(ScreenControl1, 0x18); // DEN + RSEL
        vic.Write(ScreenControl2, 0x08); // CSEL=1

        AdvanceTo(vic, 50, 62);
        Assert.True(vic.IsVerticalBorderActive);
        AdvanceTo(vic, 51, 0);
        Assert.False(vic.IsVerticalBorderActive);
        AdvanceTo(vic, 51, 16);
        Assert.False(vic.IsVerticalBorderActive);

        AdvanceTo(vic, 252, 5);
        Assert.True(vic.IsRasterLineVerticalBorderActive(50));
        Assert.False(vic.IsRasterLineVerticalBorderActive(51));
        Assert.False(vic.IsRasterLineVerticalBorderActive(250));
        Assert.True(vic.IsRasterLineVerticalBorderActive(251));
    }

    // ------------------------------------------------------------------
    // FR-VIC-RASTER-IRQ
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-01 (TEST-VIC-RASTER-IRQ-01, FAITHFUL): the raster
    /// compare line is the 9-bit value $D012 | ($D011 bit 7 shifted to bit 8).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:132-143 (update_raster_line);
    /// managed Mos6569.Write $D012/$D011 (Mos6569.cs:1925,1932).
    /// Use case: software programs raster splits above line 255 by combining
    /// the $D012 low byte with the $D011 bit 7 high bit.
    /// Acceptance: writes to $D012 replace the low 8 bits preserving bit 8,
    /// and writes to $D011 replace bit 8 preserving the low 8 bits, across
    /// set and clear transitions up to compare value $1FF.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-01", ParityTag.Faithful)]
    public void RasterIrq_CompareLine_IsNineBitD012PlusD011Bit7()
    {
        var vic = BuildVic(out _);

        vic.Write(RasterCompareLow, 0x42);
        Assert.Equal(0x0042, vic.RasterIrqLine);

        vic.Write(ScreenControl1, 0x80);
        Assert.Equal(0x0142, vic.RasterIrqLine);

        vic.Write(RasterCompareLow, 0x10);
        Assert.Equal(0x0110, vic.RasterIrqLine);

        vic.Write(ScreenControl1, 0x00);
        Assert.Equal(0x0010, vic.RasterIrqLine);

        vic.Write(RasterCompareLow, 0xFF);
        vic.Write(ScreenControl1, 0x80);
        Assert.Equal(0x01FF, vic.RasterIrqLine);
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-03 (TEST-VIC-RASTER-IRQ-03, FAITHFUL): the
    /// raster_irq_triggered guard holds for the whole matching line (no
    /// re-latch after acknowledge) and is reset off the matching line so the
    /// next traversal fires again.
    /// VICE native/vice/vice/src/viciisc/vicii-cycle.c:467-474; managed
    /// Mos6569 compare-armed flag (Mos6569.cs:930,978).
    /// Use case: an IRQ handler that acknowledges $D019 while still on the
    /// compare line must not be re-interrupted until the raster returns to
    /// the compare line.
    /// Acceptance: after the latch fires on line 5 and is acknowledged
    /// mid-line, $D019 stays $70 through the rest of line 5 and line 6, then
    /// reads $71 again on the next frame's line 5.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-03", ParityTag.Faithful)]
    public void RasterIrq_TriggeredGuard_HoldsWholeLineAndRefiresOnNextTraversal()
    {
        var vic = BuildVic(out _);
        vic.Write(RasterCompareLow, 0x05);

        AdvanceTo(vic, 5, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));

        vic.Write(InterruptLatch, 0x01);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        AdvanceTo(vic, 5, 62);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        AdvanceTo(vic, 6, 30);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        AdvanceTo(vic, 5, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-04 (TEST-VIC-RASTER-IRQ-04, FAITHFUL): the raster
    /// trigger sets irq_status bit 0 idempotently; re-firing with the bit
    /// already latched leaves the status unchanged.
    /// VICE native/vice/vice/src/viciisc/vicii-irq.c:116-121
    /// (vicii_irq_raster_trigger); managed Mos6569.Tick (Mos6569.cs:931).
    /// Use case: an unacknowledged raster latch survives repeated compare
    /// matches across frames without disturbing other status bits.
    /// Acceptance: with the compare on line 7 and no acknowledge, $D019 reads
    /// exactly $71 after the first fire and still exactly $71 after the next
    /// frame's fire, with the IRQ line never asserted (enable mask 0).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-04", ParityTag.Faithful)]
    public void RasterIrq_Trigger_IsIdempotentOnAlreadyLatchedBit0()
    {
        var vic = BuildVic(out var irq);
        vic.Write(RasterCompareLow, 0x07);

        AdvanceTo(vic, 7, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
        Assert.False(irq.IsAsserted);

        Advance(vic, 5);
        AdvanceTo(vic, 7, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
        Assert.False(irq.IsAsserted);
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-05 (TEST-VIC-RASTER-IRQ-05, FAITHFUL): the raster
    /// latch sets irq_status bit 0 (irq_status |= 0x1) when the raster line
    /// reaches the compare line.
    /// VICE native/vice/vice/src/viciisc/vicii-irq.c:58-62 (vicii_irq_raster_set);
    /// managed Mos6569.Tick (Mos6569.cs:931).
    /// Use case: the raster IRQ source latches independently of the enable
    /// mask so software can poll $D019 bit 0.
    /// Acceptance: with compare line 3 and enable mask 0, $D019 reads $70 on
    /// line 2 and $71 once line 3 has been entered.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-05", ParityTag.Faithful)]
    public void RasterIrq_RasterLatch_SetsIrqStatusBit0()
    {
        var vic = BuildVic(out _);
        vic.Write(RasterCompareLow, 0x03);

        AdvanceTo(vic, 2, 30);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        AdvanceTo(vic, 3, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-06 (TEST-VIC-RASTER-IRQ-06, FAITHFUL): $D019 is
    /// write-1-to-clear per source (irq_status &amp;= ~((value &amp; $0F) | $80))
    /// followed by an IRQ-line recompute.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:227-233 (d019_store);
    /// managed Mos6569.Write $D019 (Mos6569.cs:1916-1921).
    /// Use case: acknowledging one source keeps other pending sources latched
    /// and asserted; acknowledging all sources releases the IRQ line.
    /// Acceptance: with raster+LP latched and enabled ($D019 = $F9), writing
    /// $01 leaves $F8 asserted; writing $0F leaves $70 released.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-06", ParityTag.Faithful)]
    public void RasterIrq_D019WriteOneToClear_AcksPerSourceAndRecomputesLine()
    {
        var vic = BuildVic(out var irq);
        vic.Write(RasterCompareLow, 0x02);
        vic.Write(InterruptEnable, 0x09); // raster + light pen enabled

        AdvanceTo(vic, 2, 1);
        Assert.Equal(0xF1, vic.Read(InterruptLatch));
        Assert.True(irq.IsAsserted);

        vic.TriggerLightPen();
        Assert.Equal(0xF9, vic.Read(InterruptLatch));

        vic.Write(InterruptLatch, 0x01);
        Assert.Equal(0xF8, vic.Read(InterruptLatch));
        Assert.True(irq.IsAsserted);

        vic.Write(InterruptLatch, 0x0F);
        Assert.Equal(0x70, vic.Read(InterruptLatch));
        Assert.False(irq.IsAsserted);
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-07 (TEST-VIC-RASTER-IRQ-07, FAITHFUL): a $D01A
    /// store keeps only the low nibble (regs[$1A] = value &amp; $0F) and then
    /// recomputes the IRQ output against the pending latch.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:235-242 (d01a_store);
    /// managed Mos6569.Write $D01A (Mos6569.cs:1938-1943).
    /// Use case: enabling a source whose latch is already pending asserts the
    /// IRQ immediately; writing only high-nibble garbage disables everything.
    /// Acceptance: with the raster latch pending, writing $01 asserts the IRQ
    /// ($D019 = $F1) and writing $F0 releases it ($D019 = $71, $D01A reads $F0).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-07", ParityTag.Faithful)]
    public void RasterIrq_D01AStore_KeepsLowNibbleAndRecomputesIrqLine()
    {
        var vic = BuildVic(out var irq);
        vic.Write(RasterCompareLow, 0x02);

        AdvanceTo(vic, 2, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
        Assert.False(irq.IsAsserted);

        vic.Write(InterruptEnable, 0x01);
        Assert.True(irq.IsAsserted);
        Assert.Equal(0xF1, vic.Read(InterruptLatch));

        vic.Write(InterruptEnable, 0xF0);
        Assert.False(irq.IsAsserted);
        Assert.Equal(0x71, vic.Read(InterruptLatch));
        Assert.Equal(0xF0, vic.Read(InterruptEnable));
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-08 (TEST-VIC-RASTER-IRQ-08, FAITHFUL): the IRQ
    /// output rule: a nonzero (irq_status AND $D01A) sets $D019 bit 7 and
    /// asserts the CPU IRQ line; zero clears bit 7 and releases the line.
    /// VICE native/vice/vice/src/viciisc/vicii-irq.c:36-45 (vicii_irq_set_line);
    /// managed Mos6569.RefreshInterruptLine (Mos6569.cs:2101-2114).
    /// Use case: $D019 bit 7 mirrors the physical IRQ output so handlers can
    /// identify the VIC as the interrupt source.
    /// Acceptance: latch-only state reads bit 0 set with bit 7 clear and the
    /// line released; enabling the source sets bit 7 ($F1) and asserts;
    /// acknowledging clears to $70 and releases.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-08", ParityTag.Faithful)]
    public void RasterIrq_IrqOutput_Bit7AndLineFollowLatchAndMaskOverlap()
    {
        var vic = BuildVic(out var irq);
        vic.Write(RasterCompareLow, 0x04);

        AdvanceTo(vic, 4, 1);
        byte latchOnly = vic.Read(InterruptLatch);
        Assert.Equal(0x01, latchOnly & 0x0F);
        Assert.Equal(0x00, latchOnly & 0x80);
        Assert.False(irq.IsAsserted);

        vic.Write(InterruptEnable, 0x01);
        Assert.Equal(0xF1, vic.Read(InterruptLatch));
        Assert.True(irq.IsAsserted);

        vic.Write(InterruptLatch, 0x01);
        Assert.Equal(0x70, vic.Read(InterruptLatch));
        Assert.False(irq.IsAsserted);
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-10 (TEST-VIC-RASTER-IRQ-10, FAITHFUL): a $D011
    /// store always recomputes the compare line (no unchanged-value early
    /// exit, unlike $D012 in VICE).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:145-156 (d011_store);
    /// managed Mos6569.Write $D011 (Mos6569.cs:1930-1936).
    /// Use case: rewriting $D011 with the same value keeps the merged 9-bit
    /// compare stable, and toggling bit 7 moves the compare across the
    /// 256-line boundary in both directions.
    /// Acceptance: with $D012 = $42, writes of $80/$80/$00/$80 to $D011 yield
    /// compare values $142/$142/$042/$142.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-10", ParityTag.Faithful)]
    public void RasterIrq_D011Store_AlwaysRecomputesCompareLine()
    {
        var vic = BuildVic(out _);
        vic.Write(RasterCompareLow, 0x42);

        vic.Write(ScreenControl1, 0x80);
        Assert.Equal(0x0142, vic.RasterIrqLine);

        vic.Write(ScreenControl1, 0x80);
        Assert.Equal(0x0142, vic.RasterIrqLine);

        vic.Write(ScreenControl1, 0x00);
        Assert.Equal(0x0042, vic.RasterIrqLine);

        vic.Write(ScreenControl1, 0x80);
        Assert.Equal(0x0142, vic.RasterIrqLine);
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-12 (TEST-VIC-RASTER-IRQ-12, FAITHFUL): reading
    /// $D019 returns irq_status | $70 (unconnected bits 6-4 float high).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:515-518 (d019_read);
    /// managed Mos6569.Read $D019 (Mos6569.cs:1819-1822).
    /// Use case: software masks $D019 reads knowing bits 6-4 always read 1.
    /// Acceptance: $D019 reads exactly $70 with nothing latched, $71 with the
    /// raster source latched, and $F1 once the source is enabled (bit 7 joins
    /// the status).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-12", ParityTag.Faithful)]
    public void RasterIrq_D019Read_ReturnsIrqStatusWithBits4To6ForcedHigh()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        vic.Write(RasterCompareLow, 0x02);
        AdvanceTo(vic, 2, 1);
        Assert.Equal(0x71, vic.Read(InterruptLatch));

        vic.Write(InterruptEnable, 0x01);
        Assert.Equal(0xF1, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR-VIC-RASTER-IRQ AC-13 (TEST-VIC-RASTER-IRQ-13, FAITHFUL): reading
    /// $D01A returns the stored enable mask with the high nibble forced high
    /// (regs | $F0).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:650-653; managed
    /// Mos6569.Read $D01A (Mos6569.cs:1895-1898).
    /// Use case: only the four source-enable bits of $D01A are readable; the
    /// upper nibble always reads 1.
    /// Acceptance: reads return $F0 after reset, $F5 after writing $05, and
    /// $FF after writing $FF (stored as $0F).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-RASTER-IRQ-13", ParityTag.Faithful)]
    public void RasterIrq_D01ARead_ReturnsRegsWithHighNibbleForcedHigh()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0xF0, vic.Read(InterruptEnable));

        vic.Write(InterruptEnable, 0x05);
        Assert.Equal(0xF5, vic.Read(InterruptEnable));

        vic.Write(InterruptEnable, 0xFF);
        Assert.Equal(0xFF, vic.Read(InterruptEnable));
    }

    // ------------------------------------------------------------------
    // FR-VIC-REGISTERS
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-REGISTERS AC-01 (TEST-VIC-REGISTERS-01, FAITHFUL): reading
    /// $D016 returns regs | $C0 (unconnected bits 7-6 float high).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:627-631; managed
    /// Mos6569.Read $D016 (Mos6569.cs:1877-1880).
    /// Use case: control register 2 exposes MCM/CSEL/XSCROLL with the top two
    /// bits always reading 1.
    /// Acceptance: reads return $C0 after reset, $D8 after writing $18, and
    /// $C0 after writing $00.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-01", ParityTag.Faithful)]
    public void Registers_D016Read_ForcesBits6And7High()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0xC0, vic.Read(ScreenControl2));

        vic.Write(ScreenControl2, 0x18);
        Assert.Equal(0xD8, vic.Read(ScreenControl2));

        vic.Write(ScreenControl2, 0x00);
        Assert.Equal(0xC0, vic.Read(ScreenControl2));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-02 (TEST-VIC-REGISTERS-02, FAITHFUL): reading
    /// $D018 returns regs | $01 (unconnected bit 0 floats high).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:639-643; managed
    /// Mos6569.Read $D018 (Mos6569.cs:1887-1890).
    /// Use case: the memory-pointers register reads back with bit 0 always 1.
    /// Acceptance: reads return $01 after reset, $15 after writing $14, and
    /// $F1 after writing $F0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-02", ParityTag.Faithful)]
    public void Registers_D018Read_ForcesBit0High()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0x01, vic.Read(MemoryPointers));

        vic.Write(MemoryPointers, 0x14);
        Assert.Equal(0x15, vic.Read(MemoryPointers));

        vic.Write(MemoryPointers, 0xF0);
        Assert.Equal(0xF1, vic.Read(MemoryPointers));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-03 (TEST-VIC-REGISTERS-03, FAITHFUL): reading
    /// $D019 returns irq_status | $70.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:515-518 (d019_read);
    /// managed Mos6569.Read $D019 (Mos6569.cs:1819-1822).
    /// Use case: the IRQ status register carries the $70 floor on every read
    /// regardless of which source is latched.
    /// Acceptance: $D019 reads $70 with no latch pending and $78 after the
    /// light-pen source latches with the enable mask still 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-03", ParityTag.Faithful)]
    public void Registers_D019Read_ReturnsIrqStatusOr70()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0x70, vic.Read(InterruptLatch));

        vic.TriggerLightPen();
        Assert.Equal(0x78, vic.Read(InterruptLatch));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-04 (TEST-VIC-REGISTERS-04, FAITHFUL): reading
    /// $D01A returns regs | $F0.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:650-653; managed
    /// Mos6569.Read $D01A (Mos6569.cs:1895-1898).
    /// Use case: the enable-mask register reads back with the high nibble
    /// always 1.
    /// Acceptance: reads return $F0 after reset and $FA after writing $0A.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-04", ParityTag.Faithful)]
    public void Registers_D01ARead_ReturnsRegsOrF0()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0xF0, vic.Read(InterruptEnable));

        vic.Write(InterruptEnable, 0x0A);
        Assert.Equal(0xFA, vic.Read(InterruptEnable));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-05 (TEST-VIC-REGISTERS-05, FAITHFUL): reading any
    /// color register $D020-$D02E returns regs | $F0 (upper nibble floats high).
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:681-714; managed
    /// Mos6569.Read color range (Mos6569.cs:1867-1870).
    /// Use case: writing color $05 to a color register reads back as $F5 on
    /// real hardware and in VICE.
    /// Acceptance: for every register $D020-$D02E, writing a low-nibble value
    /// reads back as $F0 | value.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-05", ParityTag.Faithful)]
    public void Registers_ColorRegisterReads_ForceHighNibbleHigh()
    {
        var vic = BuildVic(out _);
        for (var reg = 0x20; reg <= 0x2E; reg++)
        {
            var address = (ushort)(0xD000 + reg);
            byte nibble = (byte)((reg - 0x20 + 3) & 0x0F);
            vic.Write(address, nibble);
            Assert.Equal(0xF0 | nibble, vic.Read(address));
        }
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-06 (TEST-VIC-REGISTERS-06, FAITHFUL): unused
    /// registers $D02F-$D03F read as $FF.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:716-735; managed
    /// Mos6569.Read unused range (Mos6569.cs:1903-1906).
    /// Use case: the VIC-II decodes only 47 registers; the rest return open
    /// bus $FF.
    /// Acceptance: every read in $D02F-$D03F returns exactly $FF.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-06", ParityTag.Faithful)]
    public void Registers_UnusedD02FToD03F_ReadAsFF()
    {
        var vic = BuildVic(out _);
        for (var reg = 0x2F; reg <= 0x3F; reg++)
        {
            Assert.Equal(0xFF, vic.Read((ushort)(0xD000 + reg)));
        }
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-07 (TEST-VIC-REGISTERS-07, FAITHFUL): reading
    /// $D011 returns (regs &amp; $7F) | ((raster_y &amp; $100) shifted right 1);
    /// bit 7 is the live raster bit 8, not the stored compare bit.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:501-512 (d01112_read);
    /// managed Mos6569.Read $D011 (Mos6569.cs:1824-1827).
    /// Use case: software polls $D011 bit 7 to detect raster lines at or above
    /// 256.
    /// Acceptance: with $D011 = $9B stored, reads return $1B on line 0 and
    /// $9B on line 304; storing $0B on line 304 reads $8B (live raster bit).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-07", ParityTag.Faithful)]
    public void Registers_D011Read_MergesRegsWithLiveRasterBit8()
    {
        var vic = BuildVic(out _);
        vic.Write(ScreenControl1, 0x9B);
        Assert.Equal(0x1B, vic.Read(ScreenControl1));

        AdvanceTo(vic, 304, 0);
        Assert.Equal(0x9B, vic.Read(ScreenControl1));

        vic.Write(ScreenControl1, 0x0B);
        Assert.Equal(0x8B, vic.Read(ScreenControl1));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-08 (TEST-VIC-REGISTERS-08, FAITHFUL): reading
    /// $D012 returns the live raster line low byte, never the stored compare
    /// value.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:501-512 (d01112_read);
    /// managed live $D012 mirror + Read (Mos6569.cs:1062,1908).
    /// Use case: busy-wait raster polling loops read the current raster
    /// position from $D012.
    /// Acceptance: after writing compare value $40, $D012 reads $00 on line 0,
    /// $81 on line $081, and $34 on line $134 (low byte of 308).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-08", ParityTag.Faithful)]
    public void Registers_D012Read_ReturnsLiveRasterLowByte()
    {
        var vic = BuildVic(out _);
        vic.Write(RasterCompareLow, 0x40);
        Assert.Equal(0x00, vic.Read(RasterCompareLow));

        AdvanceTo(vic, 0x81, 0);
        Assert.Equal(0x81, vic.Read(RasterCompareLow));

        AdvanceTo(vic, 308, 0);
        Assert.Equal(0x34, vic.Read(RasterCompareLow));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-09 (TEST-VIC-REGISTERS-09, FAITHFUL): raw register
    /// reads (sprite X/Y positions, $D010, $D015, $D017, $D01B-$D01D) return
    /// the stored register value unmasked.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:573-671; managed
    /// Mos6569.Read default path (Mos6569.cs:1908).
    /// Use case: all eight bits of the sprite position and control registers
    /// are readable exactly as written.
    /// Acceptance: each listed register reads back the exact byte written,
    /// including high bits.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-09", ParityTag.Faithful)]
    public void Registers_RawRegisters_ReadBackUnmasked()
    {
        var vic = BuildVic(out _);

        vic.Write(SpriteXLow0, 0xAB);
        Assert.Equal(0xAB, vic.Read(SpriteXLow0));

        vic.Write(SpriteY0, 0x33);
        Assert.Equal(0x33, vic.Read(SpriteY0));

        vic.Write(SpriteXMsb, 0x5A);
        Assert.Equal(0x5A, vic.Read(SpriteXMsb));

        vic.Write(SpriteEnable, 0xC3);
        Assert.Equal(0xC3, vic.Read(SpriteEnable));

        vic.Write(SpriteExpandY, 0x81);
        Assert.Equal(0x81, vic.Read(SpriteExpandY));

        vic.Write(SpritePriorityRegister, 0x7E);
        Assert.Equal(0x7E, vic.Read(SpritePriorityRegister));

        vic.Write(SpriteMulticolorRegister, 0x99);
        Assert.Equal(0x99, vic.Read(SpriteMulticolorRegister));

        vic.Write(SpriteExpandX, 0x42);
        Assert.Equal(0x42, vic.Read(SpriteExpandX));
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-10 (TEST-VIC-REGISTERS-10, FAITHFUL): color
    /// register writes $D020-$D02E store only value &amp; $0F.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:277-331 (d020_store and
    /// friends); managed Mos6569.Write color range (Mos6569.cs:1964-1985).
    /// Use case: the four upper bits of a color write are physically
    /// unconnected and never reach the register backing store.
    /// Acceptance: for every register $D020-$D02E, writing a value with a
    /// dirty high nibble leaves the low nibble in the backing store, and
    /// Peek returns regs|unused_bits = nibble|$F0 (vicii_peek semantics per
    /// vicii-mem.c:767-768, unused_bits_in_registers[0x20..0x2E]=0xF0).
    /// Rebased from V2 (old lock asserted Peek returns raw nibble via
    /// "non-masking debug Peek"); superseded by FR-VIC-REGISTERS AC-15 fix.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-10", ParityTag.Faithful)]
    public void Registers_ColorRegisterWrites_StoreLowNibbleOnly()
    {
        var vic = BuildVic(out _);
        for (var reg = 0x20; reg <= 0x2E; reg++)
        {
            var address = (ushort)(0xD000 + reg);
            byte nibble = (byte)((reg - 0x20 + 5) & 0x0F);
            vic.Write(address, (byte)(0xB0 | nibble));
            // vicii_peek returns regs[addr] | unused_bits[addr] = nibble | 0xF0.
            Assert.Equal((byte)(0xF0 | nibble), vic.Peek(address));
        }
    }

    /// <summary>
    /// FR-VIC-REGISTERS AC-11 (TEST-VIC-REGISTERS-11, FAITHFUL): writes to
    /// the collision registers $D01E/$D01F and the unused range $D02F-$D03F
    /// are ignored.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:265-268 (collision_store
    /// no-op); managed Mos6569.Write early returns (Mos6569.cs:1948-1958).
    /// Use case: collision registers are read-only latches and the unused
    /// range has no backing hardware; stores must not disturb any state.
    /// Acceptance: after writing $FF to $D01E/$D01F both collision accumulators
    /// still peek and read as $00 (vicii_peek returns raw accumulator per
    /// vicii-mem.c:763-766); after writing to $D02F/$D03F Peek returns $FF
    /// (regs[0]=0x00 | unused_bits=0xFF per vicii-mem.c:767-768) and Read
    /// also returns $FF. Rebased from V2 (old lock asserted Peek($D02F/$D03F)
    /// returns $00 via "non-masking debug Peek"); superseded by
    /// FR-VIC-REGISTERS AC-15 fix.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-REGISTERS-11", ParityTag.Faithful)]
    public void Registers_CollisionAndUnusedRegisterWrites_AreIgnored()
    {
        var vic = BuildVic(out _);

        vic.Write(SpriteSpriteCollision, 0xFF);
        vic.Write(SpriteBackgroundCollision, 0xFF);
        // vicii_peek($D01E/$D01F): raw collision accumulators (0x00 - no collision accumulated).
        Assert.Equal(0x00, vic.Peek(SpriteSpriteCollision));
        Assert.Equal(0x00, vic.Peek(SpriteBackgroundCollision));
        Assert.Equal(0x00, vic.Read(SpriteSpriteCollision));
        Assert.Equal(0x00, vic.Read(SpriteBackgroundCollision));

        vic.Write(UnusedD02F, 0xAA);
        vic.Write(UnusedD03F, 0x55);
        // vicii_peek($D02F/$D03F): regs[addr]=0x00 | unused_bits=0xFF = 0xFF.
        Assert.Equal(0xFF, vic.Peek(UnusedD02F));
        Assert.Equal(0xFF, vic.Peek(UnusedD03F));
        Assert.Equal(0xFF, vic.Read(UnusedD02F));
        Assert.Equal(0xFF, vic.Read(UnusedD03F));
    }

    // ------------------------------------------------------------------
    // FR-VIC-LIGHTPEN
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-02 (TEST-VIC-LIGHTPEN-02, FAITHFUL): the light-pen
    /// trigger latches the current raster line low byte into $D014.
    /// VICE native/vice/vice/src/viciisc/vicii-lightpen.c:68,101; managed
    /// Mos6569.TriggerLightPen (Mos6569.cs:2135).
    /// Use case: light-pen software reads the trigger raster line from $D014;
    /// lines at or above 256 wrap to their low byte.
    /// Acceptance: a trigger on line 308 ($134) latches $34, and a trigger on
    /// line $42 of the next frame latches $42.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-02", ParityTag.Faithful)]
    public void LightPen_D014_LatchesRasterLineLowByte()
    {
        var vic = BuildVic(out _);

        AdvanceTo(vic, 308, 20);
        vic.TriggerLightPen();
        Assert.Equal(0x34, vic.Read(LightPenY));

        AdvanceTo(vic, 0x42, 10);
        vic.TriggerLightPen();
        Assert.Equal(0x42, vic.Read(LightPenY));
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-03 (TEST-VIC-LIGHTPEN-03, FAITHFUL): the
    /// once-per-frame guard makes a second trigger in the same frame return
    /// without latching.
    /// VICE native/vice/vice/src/viciisc/vicii-lightpen.c:62-64; managed
    /// Mos6569.TriggerLightPen guard (Mos6569.cs:2128-2131).
    /// Use case: only the first light-pen pulse of a frame is captured; later
    /// pulses in the same frame must not disturb the latched coordinates.
    /// Acceptance: after a trigger at line 10 cycle 5 latches
    /// X=$DC (VICE xpos: Phi1(6) 0x1bc, cycle_get_xpos=0x1b8=440, /2=220=$DC)
    /// and Y=$0A, a second trigger at line 20 cycle 8 leaves both reads
    /// unchanged. Old managed value was $02 (RasterX>>1=5>>1=2; superseded by
    /// FR-VIC-LIGHTPEN AC-01 fix; cites vicii-chip-model.h:164-167 +
    /// vicii-lightpen.c:75).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-03", ParityTag.Faithful)]
    public void LightPen_SecondTriggerInSameFrame_IsIgnored()
    {
        var vic = BuildVic(out _);

        AdvanceTo(vic, 10, 5);
        vic.TriggerLightPen();
        Assert.Equal(0xDC, vic.Read(LightPenX)); // VICE-exact: Phi1(6) xpos 0x1bc -> 0x1b8/2 = 0xDC.
        Assert.Equal(0x0A, vic.Read(LightPenY));

        AdvanceTo(vic, 20, 8);
        vic.TriggerLightPen();
        Assert.Equal(0xDC, vic.Read(LightPenX)); // Second trigger ignored; latch unchanged.
        Assert.Equal(0x0A, vic.Read(LightPenY));
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-04 (TEST-VIC-LIGHTPEN-04, FAITHFUL): an accepted
    /// trigger sets the triggered flag; the flag clears at the frame boundary
    /// so the next frame's first trigger is accepted again.
    /// VICE native/vice/vice/src/viciisc/vicii-lightpen.c:66 (triggered = 1)
    /// with vicii-cycle.c:210 (start-of-frame clear); managed
    /// Mos6569.TriggerLightPen (Mos6569.cs:2133) + frame wrap (Mos6569.cs:1001).
    /// Use case: the light pen latches exactly once per frame and re-arms
    /// automatically when the raster wraps to line 0.
    /// Acceptance: a trigger at line 5 latches Y=$05; after the frame wrap a
    /// trigger at line 2 cycle 4 is accepted and overwrites the latch with
    /// Y=$02 and X=$D8 (VICE-exact: Phi1(5) xpos 0x1b4, cycle_get_xpos=0x1b0=432,
    /// /2=216=$D8). Old managed X was $02 (RasterX>>1=4>>1=2; superseded by
    /// FR-VIC-LIGHTPEN AC-01 fix; cites vicii-chip-model.h:164-167 +
    /// vicii-lightpen.c:75).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-04", ParityTag.Faithful)]
    public void LightPen_TriggeredFlag_SetOnAccept_RearmsAtFrameWrap()
    {
        var vic = BuildVic(out _);

        AdvanceTo(vic, 5, 4);
        vic.TriggerLightPen();
        Assert.Equal(0x05, vic.Read(LightPenY));

        AdvanceTo(vic, 2, 4);
        vic.TriggerLightPen();
        Assert.Equal(0x02, vic.Read(LightPenY));
        Assert.Equal(0xD8, vic.Read(LightPenX)); // VICE-exact: Phi1(5) xpos 0x1b4 -> 0x1b0/2 = 0xD8.
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-11 (TEST-VIC-LIGHTPEN-11, FAITHFUL): a normal-mode
    /// (non-6569R1) accepted trigger fires the light-pen IRQ
    /// (vicii_irq_lightpen_set).
    /// VICE native/vice/vice/src/viciisc/vicii-lightpen.c:105-107 with
    /// vicii-irq.c:94-98; managed Mos6569.TriggerLightPen (Mos6569.cs:2136-2137).
    /// Use case: with $D01A bit 3 enabled, the light-pen latch immediately
    /// asserts the CPU IRQ line.
    /// Acceptance: with enable $08, a trigger reads $D019 = $F8 (LP latch +
    /// bit 7) and the IRQ line is asserted.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-11", ParityTag.Faithful)]
    public void LightPen_Trigger_FiresIrqInNormalMode()
    {
        var vic = BuildVic(out var irq);
        vic.Write(InterruptEnable, 0x08);

        AdvanceTo(vic, 7, 10);
        vic.TriggerLightPen();

        Assert.Equal(0xF8, vic.Read(InterruptLatch));
        Assert.True(irq.IsAsserted);
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-12 (TEST-VIC-LIGHTPEN-12, FAITHFUL): the light-pen
    /// latch sets $D019 bit 3 unconditionally while $D01A bit 3 only gates
    /// the IRQ output.
    /// VICE native/vice/vice/src/viciisc/vicii-irq.c:94-98
    /// (vicii_irq_lightpen_set + vicii_irq_set_line); managed
    /// Mos6569.TriggerLightPen + RefreshInterruptLine (Mos6569.cs:2136-2137).
    /// Use case: software can poll the LP latch with interrupts disabled, and
    /// enabling the source later releases the pending latch as an IRQ.
    /// Acceptance: with enable 0 a trigger reads $D019 = $78 and the line
    /// stays released; writing $08 to $D01A then asserts and reads $F8.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-12", ParityTag.Faithful)]
    public void LightPen_LatchSetsD019Bit3_D01ABit3GatesIrqOutput()
    {
        var vic = BuildVic(out var irq);

        AdvanceTo(vic, 7, 10);
        vic.TriggerLightPen();
        Assert.Equal(0x78, vic.Read(InterruptLatch));
        Assert.False(irq.IsAsserted);

        vic.Write(InterruptEnable, 0x08);
        Assert.Equal(0xF8, vic.Read(InterruptLatch));
        Assert.True(irq.IsAsserted);
    }

    /// <summary>
    /// FR-VIC-LIGHTPEN AC-13 (TEST-VIC-LIGHTPEN-13, FAITHFUL): $D013/$D014
    /// reads return the latched X/Y values non-destructively.
    /// VICE native/vice/vice/src/viciisc/vicii-mem.c:611-619; managed
    /// Mos6569.Read $D013/$D014.
    /// Use case: light-pen coordinates read back from the latch registers, not
    /// from live raster state, and repeated reads are stable.
    /// Acceptance: both registers read $00 before any trigger; after a trigger
    /// at line 100 cycle 40 they read X=$6C (VICE-exact: Phi1(41) xpos 0x0dc,
    /// cycle_get_xpos=0x0d8=216, /2=108=$6C per vicii-chip-model.h:164-167 +
    /// vicii-lightpen.c:75) and Y=$64, stable across a second read. Rebased
    /// from V2 (old acceptance pinned X=$14 = managed RasterX>>1; superseded
    /// by FR-VIC-LIGHTPEN AC-01 fix, owned by TEST-VIC-LIGHTPEN-01).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-LIGHTPEN-13", ParityTag.Faithful)]
    public void LightPen_D013D014Reads_ReturnLatchedValues()
    {
        var vic = BuildVic(out _);
        Assert.Equal(0x00, vic.Read(LightPenX));
        Assert.Equal(0x00, vic.Read(LightPenY));

        AdvanceTo(vic, 100, 40);
        vic.TriggerLightPen();
        // VICE-exact: Phi1(41) xpos 0x0dc, cycle_get_xpos=0x0d8, /2=0x6C.
        Assert.Equal(0x6C, vic.Read(LightPenX));
        Assert.Equal(0x64, vic.Read(LightPenY));
        Assert.Equal(0x6C, vic.Read(LightPenX));
        Assert.Equal(0x64, vic.Read(LightPenY));
    }
}
