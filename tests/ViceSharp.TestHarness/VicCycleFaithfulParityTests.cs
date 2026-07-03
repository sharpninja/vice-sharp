namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8 / TR-PARITY-GATE-001: FAITHFUL (green-now regression lock)
/// parity tests for FR-VIC-CYCLE, FR-VIC-FETCH and FR-VIC-MATRIX-ADDR from
/// artifacts/vice-parity-requirements/requirements.yaml. One test method per FAITHFUL
/// acceptance criterion; each method locks the CURRENT managed behavior that already
/// matches VICE (native/vice/vice/src/viciisc/vicii-cycle.c, vicii-fetch.c,
/// vicii-chip-model.c). DIVERGENT criteria are intentionally excluded here; they are
/// remediation targets, not regression locks.
///
/// Cycle numbering note: VICE raster cycles are 1-based; the managed VIC uses
/// RasterX = VICII_PAL_CYCLE(n) = n - 1. VICE "cycle 14" is managed RasterX 13,
/// VICE "cycle 58" is managed RasterX 57. Managed Tick() increments RasterX first,
/// so after a Tick() returns, the cycle equal to RasterX has just been processed
/// (including its Phi1 fetch, observable via LastReadPhi1).
/// </summary>
public sealed class VicCycleFaithfulParityTests
{
    private const ushort ScreenControl1 = 0xD011;
    private const ushort MemoryPointers = 0xD018;

    // ----------------------------------------------------------------
    // FR-VIC-CYCLE: per-cycle state engine (VC/RC/VMLI/idle/badline)
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-CYCLE AC-01.
    /// Use case: at VICE cycle 14 (managed RasterX 13) the video counter is reloaded
    /// from its base: vc = vcbase (VICE viciisc/vicii-cycle.c:543-544, managed
    /// Mos6569.cs:939-941).
    /// Acceptance: with vcbase captured as 3 (rc==7 capture at cycle 58) and vc then
    /// advanced to 7 by direct g-access consumption, the tick that processes
    /// RasterX 13 restores vc to exactly 3, equal to vcbase.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-01", ParityTag.Faithful)]
    public void VcRestoredFromVcBaseAtCycle14()
    {
        var vic = BuildVic();

        // Reach line 7 cycle 56: rc is 7 (incremented once per line at cycle 58
        // of lines 0..6), idle_state still false.
        AdvanceTo(vic, 0x07, 56);
        Assert.Equal(7, vic.CurrentRowCounter);

        // Advance vc to a nonzero value so the rc==7 capture at cycle 58 makes
        // vcbase nonzero and the later restore is observable.
        ConsumeGraphicsFetches(vic, 3);
        Assert.Equal(3, vic.CurrentVideoMatrixCounter);

        vic.Tick(); // RasterX 57: rc==7 -> idle_state=1, vcbase=vc=3.
        var afterRcUpdate = ReadInternals(vic);
        Assert.Equal(3, afterRcUpdate.VcBase);

        // Move into the next line, stopping just before the VC update cycle,
        // then push vc away from vcbase.
        AdvanceTo(vic, 0x08, 12);
        ConsumeGraphicsFetches(vic, 4);
        var before = ReadInternals(vic);
        Assert.Equal(7, before.Vc);
        Assert.Equal(3, before.VcBase);

        vic.Tick(); // Processes RasterX 13: vc = vcbase.
        Assert.Equal(13, vic.RasterX);
        var after = ReadInternals(vic);
        Assert.Equal(3, after.Vc);
        Assert.Equal(after.VcBase, after.Vc);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-02.
    /// Use case: at VICE cycle 14 (managed RasterX 13) the video matrix line index is
    /// cleared: vmli = 0 (VICE viciisc/vicii-cycle.c:545, managed Mos6569.cs:942).
    /// Acceptance: with vmli advanced to 5 by direct g-access consumption before the
    /// VC-update cycle, the tick that processes RasterX 13 resets the current video
    /// matrix slot to exactly 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-02", ParityTag.Faithful)]
    public void VmliClearsToZeroAtCycle14()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 0x00, 12);
        ConsumeGraphicsFetches(vic, 5);
        Assert.Equal(5, vic.CurrentVideoMatrixSlot);

        vic.Tick(); // Processes RasterX 13: vmli = 0.
        Assert.Equal(13, vic.RasterX);
        Assert.Equal(0, vic.CurrentVideoMatrixSlot);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-03.
    /// Use case: at VICE cycle 14 (managed RasterX 13) on a bad line the row counter
    /// is reset: rc = 0 (VICE viciisc/vicii-cycle.c:546-548, managed
    /// Mos6569.cs:943-944).
    /// Acceptance: with DEN set and YSCROLL=3, line $33 is a bad line; rc is 7 when
    /// entering that line (idle sequence from lines 0..7) and the tick that processes
    /// RasterX 13 forces rc to exactly 0 and leaves the chip in display state.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-03", ParityTag.Faithful)]
    public void RcResetsToZeroAtCycle14OnBadLine()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x13); // DEN=1, YSCROLL=3 -> first bad line $33.

        AdvanceTo(vic, 0x33, 12);
        Assert.True(vic.IsBadLine);
        Assert.Equal(7, vic.CurrentRowCounter);

        vic.Tick(); // Processes RasterX 13 on a bad line: rc = 0, idle cleared.
        Assert.Equal(13, vic.RasterX);
        Assert.Equal(0, vic.CurrentRowCounter);
        var after = ReadInternals(vic);
        Assert.False(after.Idle);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-05.
    /// Use case: at VICE cycle 58 (managed RasterX 57) with rc == 7 the chip enters
    /// idle state and captures the video base: idle_state = 1, vcbase = vc
    /// (VICE viciisc/vicii-cycle.c:556-559, managed Mos6569.cs:960-964).
    /// Acceptance: on line 7 (DEN off, no bad line) rc reaches 7 with idle still
    /// false; after advancing vc to 3, the tick that processes RasterX 57 sets
    /// idle_state true and vcbase to exactly 3 while rc stays 7 (second branch
    /// skipped because idle is now set and the line is not bad).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-05", ParityTag.Faithful)]
    public void RcSevenAtCycle58EntersIdleAndCapturesVcBase()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 0x07, 56);
        var before = ReadInternals(vic);
        Assert.Equal(7, before.Rc);
        Assert.False(before.Idle);
        Assert.Equal(0, before.VcBase);

        ConsumeGraphicsFetches(vic, 3);
        Assert.Equal(3, vic.CurrentVideoMatrixCounter);

        vic.Tick(); // Processes RasterX 57.
        Assert.Equal(57, vic.RasterX);
        var after = ReadInternals(vic);
        Assert.True(after.Idle);
        Assert.Equal(3, after.VcBase);
        Assert.Equal(7, after.Rc);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-06.
    /// Use case: at VICE cycle 58 the rc increment branch evaluates the UPDATED
    /// idle_state from the preceding rc==7 branch sequentially: if (!idle_state ||
    /// bad_line) rc = (rc + 1) &amp; 7, idle_state = 0 (VICE
    /// viciisc/vicii-cycle.c:560-563, managed Mos6569.cs:965-969).
    /// Acceptance: on a line made bad after cycle 14 (FLI-style late YSCROLL write)
    /// with rc == 7 entering cycle 58, the first branch captures vcbase = vc = 5 and
    /// sets idle, then the second branch still runs because bad_line is true: rc
    /// wraps to exactly 0 and idle_state ends false.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-06", ParityTag.Faithful)]
    public void RcIncrementAtCycle58UsesUpdatedIdleState()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0 -> bad lines $30, $38, ...

        // Line $37 is not bad under YSCROLL=0, so rc (incremented on $30..$36)
        // is 7 when the line starts and survives cycle 14.
        AdvanceTo(vic, 0x37, 20);
        Assert.Equal(7, vic.CurrentRowCounter);
        Assert.False(vic.IsBadLine);

        // Late YSCROLL rewrite makes line $37 bad after its VC-update cycle.
        vic.Write(ScreenControl1, 0x17); // DEN=1, YSCROLL=7 matches $37 AND 7.
        Assert.True(vic.IsBadLine);

        AdvanceTo(vic, 0x37, 56);
        ConsumeGraphicsFetches(vic, 5);
        var before = ReadInternals(vic);
        Assert.Equal(7, before.Rc);
        Assert.False(before.Idle);
        Assert.Equal(5, before.Vc);
        Assert.Equal(0, before.VcBase);

        vic.Tick(); // Processes RasterX 57: branch 1 then branch 2, sequentially.
        Assert.Equal(57, vic.RasterX);
        var after = ReadInternals(vic);
        Assert.Equal(5, after.VcBase); // Branch 1 ran: vcbase captured from vc.
        Assert.Equal(0, after.Rc);     // Branch 2 ran on updated idle: rc 7 -> 0.
        Assert.False(after.Idle);      // Branch 2 cleared idle again (display continues).
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-09.
    /// Use case: at frame start the DRAM refresh counter is reloaded:
    /// refresh_counter = 0xff (VICE viciisc/vicii-cycle.c:206, managed
    /// Mos6569.cs:984).
    /// Acceptance: after disturbing the refresh counter to $FD via three refresh
    /// consumptions, advancing through the frame wrap back to line 0 restores the
    /// refresh counter to exactly $FF.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-09", ParityTag.Faithful)]
    public void RefreshCounterResetsToFfAtFrameStart()
    {
        var vic = BuildVic();

        // Reset leaves the counter at 0; three post-decrement reads leave $FD.
        vic.ConsumeRefreshCounter();
        vic.ConsumeRefreshCounter();
        vic.ConsumeRefreshCounter();
        var before = ReadInternals(vic);
        Assert.Equal(0xFD, before.Refresh);

        // Through the full frame to the frame start (applied at raster cycle 1
        // per VICE viciisc/vicii-cycle.c:453-456; line 0 cycle 0 does not
        // exist, PLAN-VICEPARITY-001 slice V2 / TEST-VIC-CYCLE-12).
        AdvanceTo(vic, 0x00, 1);
        var after = ReadInternals(vic);
        Assert.Equal(0xFF, after.Refresh);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-10.
    /// Use case: at frame start bad lines are disallowed: allow_bad_lines = 0
    /// (VICE viciisc/vicii-cycle.c:207, managed Mos6569.cs:985).
    /// Acceptance: with DEN set, allow_bad_lines is true inside the display window
    /// (line $31) and is false again at the next frame start (line 0 cycle 1,
    /// where vicii_cycle_start_of_frame applies per vicii-cycle.c:453-456).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-10", ParityTag.Faithful)]
    public void AllowBadLinesClearsAtFrameStart()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x10); // DEN=1.

        AdvanceTo(vic, 0x31, 0);
        var during = ReadInternals(vic);
        Assert.True(during.AllowBadLines);

        // Frame start (applied at raster cycle 1 per VICE
        // viciisc/vicii-cycle.c:453-456; line 0 cycle 0 does not exist,
        // PLAN-VICEPARITY-001 slice V2 / TEST-VIC-CYCLE-12).
        AdvanceTo(vic, 0x00, 1);
        var atFrameStart = ReadInternals(vic);
        Assert.False(atFrameStart.AllowBadLines);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-11.
    /// Use case: at frame start the raster line is reset: raster_line = 0
    /// (VICE viciisc/vicii-cycle.c:205 in vicii_cycle_start_of_frame, applied
    /// at raster cycle 1 via :453-456 after vicii_cycle_end_of_line armed
    /// start_of_frame at cycle 0 of the wrap, :220-226).
    /// Acceptance: from the last PAL line (311, TotalLines - 1) cycle 62, one
    /// tick reaches cycle 0 with the raster line still reading 311 (VICE
    /// line-wrap visibility), and the next tick applies the reset: line 0 at
    /// cycle 1.
    /// REBASED (PLAN-VICEPARITY-001 slice V2): the previous expectation
    /// ((311,62) -> (0,0) in one tick) encoded the managed wrap timing that
    /// DIVERGENT TEST-VIC-CYCLE-12 owned; with the frame reset now applied at
    /// raster cycle 1 per VICE viciisc/vicii-cycle.c:202-218,453-456, the lock
    /// pins the VICE-exact wrap visibility instead.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-11", ParityTag.Faithful)]
    public void RasterLineResetsToZeroAtFrameStart()
    {
        var vic = BuildVic();
        Assert.Equal(Mos6569.PalTotalLines, vic.TotalLines);

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 62);
        Assert.Equal(311, vic.CurrentRasterLine);

        vic.Tick(); // VICE raster cycle 1: start_of_frame armed, line NOT yet reset.
        Assert.Equal(311, vic.CurrentRasterLine);
        Assert.Equal(0, vic.RasterX);

        vic.Tick(); // VICE raster cycle 2: vicii_cycle_start_of_frame applies.
        Assert.Equal(0, vic.CurrentRasterLine);
        Assert.Equal(1, vic.RasterX);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-13.
    /// Use case: allow_bad_lines turns on at the first DMA line ($30) when DEN is
    /// set (VICE viciisc/vicii-cycle.c:231-233, managed Mos6569.cs:2094-2095).
    /// Acceptance: with DEN set before line $30, allow_bad_lines is false at line
    /// $2F cycle 62 and true immediately after the wrap into line $30; a control VIC
    /// without DEN keeps allow_bad_lines false at line $30.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-13", ParityTag.Faithful)]
    public void AllowBadLinesLatchesOnAtFirstDmaLineUnderDen()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0.

        AdvanceTo(vic, 0x2F, 62);
        var before = ReadInternals(vic);
        Assert.False(before.AllowBadLines);

        vic.Tick(); // Wrap into line $30 cycle 0: start-of-line latch runs.
        Assert.Equal(0x30, vic.CurrentRasterLine);
        Assert.Equal(0, vic.RasterX);
        var after = ReadInternals(vic);
        Assert.True(after.AllowBadLines);
        Assert.True(vic.IsBadLine); // YSCROLL=0 matches the low bits of $30.

        // Control: without DEN the latch never arms.
        var noDen = BuildVic();
        AdvanceTo(noDen, 0x30, 0);
        var noDenState = ReadInternals(noDen);
        Assert.False(noDenState.AllowBadLines);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-18.
    /// Use case: the raster cycle counter wraps within 0..cycles_per_line-1
    /// (VICE viciisc/vicii-cycle.c:244-253, managed Mos6569.cs:972-977).
    /// Acceptance: on PAL (63 cycles per line) RasterX advances 1..62 across a full
    /// line and the next tick wraps it to exactly 0 while the raster line increments;
    /// the value 63 is never observable.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-18", ParityTag.Faithful)]
    public void RasterCycleWrapsWithinCyclesPerLine()
    {
        var vic = BuildVic();
        Assert.Equal(Mos6569.PalCyclesPerLine, vic.CyclesPerLine);

        AdvanceTo(vic, 0x05, 0);
        for (var expected = 1; expected < Mos6569.PalCyclesPerLine; expected++)
        {
            vic.Tick();
            Assert.Equal(expected, vic.RasterX);
        }

        vic.Tick(); // 63rd tick of the line wraps.
        Assert.Equal(0, vic.RasterX);
        Assert.Equal(0x06, vic.CurrentRasterLine);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-19.
    /// Use case: the raster line increments by exactly one per line wrap; managed
    /// increments at the wrap, which is net-numbering equivalent to VICE
    /// incrementing at cycle 1 (VICE viciisc/vicii-cycle.c:458-461, managed
    /// Mos6569.cs:977).
    /// Acceptance: two consecutive line wraps from line 10 produce lines exactly
    /// 11 and 12, each entered at RasterX 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-19", ParityTag.Faithful)]
    public void RasterLineIncrementsByOneAtLineWrap()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 0x0A, 62);
        vic.Tick();
        Assert.Equal(0x0B, vic.CurrentRasterLine);
        Assert.Equal(0, vic.RasterX);

        Advance(vic, 62);
        Assert.Equal(62, vic.RasterX);
        vic.Tick();
        Assert.Equal(0x0C, vic.CurrentRasterLine);
        Assert.Equal(0, vic.RasterX);
    }

    // ----------------------------------------------------------------
    // FR-VIC-FETCH: Phi1/Phi2 fetch schedule
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-FETCH AC-01.
    /// Use case: DRAM refresh fetches occupy managed raster cycles 10-14 (VICE PAL
    /// Phi1 cycles 11-15; vicii-chip-model.c:132-140, managed C64MemoryMap.cs:709).
    /// Acceptance: with $3F00-$3FFF seeded so each byte equals its low address byte,
    /// cycle 9 reads the $3FFF idle gap, cycles 10-14 read five consecutive
    /// decrementing refresh-counter values, and the refresh counter does not move
    /// again for the rest of the line.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-01", ParityTag.Faithful)]
    public void RefreshFetchOccupiesCycles10Through14()
    {
        var (vic, memory) = CreatePalC64();
        for (var k = 0; k < 0x100; k++)
            memory.Span[0x3F00 + k] = (byte)k;
        memory.Span[0x3FFF] = 0x6E; // Idle gap marker (also refresh counter $FF slot).

        AdvanceTo(vic, 0x01, 8);
        var beforeGap = ReadInternals(vic);

        vic.Tick(); // Cycle 9: idle gap, not refresh.
        Assert.Equal(9, vic.RasterX);
        Assert.Equal(0x6E, vic.LastReadPhi1);
        var afterGap = ReadInternals(vic);
        Assert.Equal(beforeGap.Refresh, afterGap.Refresh);

        for (var i = 0; i < 5; i++)
        {
            var expected = (byte)(afterGap.Refresh - i);
            vic.Tick(); // Cycles 10..14: refresh reads $3F00 + counter.
            Assert.Equal(10 + i, vic.RasterX);
            Assert.Equal(expected, vic.LastReadPhi1);
        }

        var afterWindow = ReadInternals(vic);
        Assert.Equal((byte)(afterGap.Refresh - 5), (byte)afterWindow.Refresh);

        AdvanceTo(vic, 0x01, 62); // No further refresh fetches on this line.
        var atLineEnd = ReadInternals(vic);
        Assert.Equal(afterWindow.Refresh, atLineEnd.Refresh);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-02.
    /// Use case: a refresh fetch reads $3F00 + refresh_counter with post-decrement
    /// semantics: the pre-decrement counter forms the address, then the counter
    /// drops by one (VICE viciisc/vicii-fetch.c:203-206, managed
    /// C64MemoryMap.cs:773-777).
    /// Acceptance: with $3F00-$3FFF seeded so each byte equals its low address byte,
    /// the cycle-10 fetch returns exactly the counter value sampled before the tick,
    /// and the counter afterwards is exactly that value minus one.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-02", ParityTag.Faithful)]
    public void RefreshFetchReads3F00PlusCounterWithPostDecrement()
    {
        var (vic, memory) = CreatePalC64();
        for (var k = 0; k < 0x100; k++)
            memory.Span[0x3F00 + k] = (byte)k;

        AdvanceTo(vic, 0x02, 9);
        var before = ReadInternals(vic);

        vic.Tick(); // Cycle 10: first refresh slot of the line.
        Assert.Equal(10, vic.RasterX);
        Assert.Equal((byte)before.Refresh, vic.LastReadPhi1);

        var after = ReadInternals(vic);
        Assert.Equal((byte)(before.Refresh - 1), (byte)after.Refresh);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-04.
    /// Use case: the c-access reads the video matrix at v_fetch_addr(vc) =
    /// ((reg $D018 &amp; $F0) &lt;&lt; 6) + vc (VICE viciisc/vicii-fetch.c:198, managed
    /// C64MemoryMap.cs:793-795); managed performs it fused on the g-access cycle
    /// (phase difference is DIVERGENT AC-03, not asserted here).
    /// Acceptance: on bad line $30 with screen base $0400 and vcbase 40 (the
    /// deterministic boot value), matrix slots 3, 20 and 39 latch exactly the bytes
    /// seeded at $0400 + vcbase + slot.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-04", ParityTag.Faithful)]
    public void CAccessMatrixAddressIsScreenBasePlusVc()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10);  // DEN=1, YSCROLL=0 -> bad line $30.
        vic.Write(MemoryPointers, 0x14);  // Screen base $0400, char base $1000.
        for (var k = 0; k < 0x400; k++)
            memory.Span[0x0400 + k] = (byte)(k ^ 0x5C);

        AdvanceTo(vic, 0x30, 12);
        var entry = ReadInternals(vic);
        // Deterministic boot trace: 40 g-accesses per display line on lines 0..7,
        // vcbase captured as 40 at line 7 cycle 58, unchanged through idle lines.
        Assert.Equal(40, entry.VcBase);
        Assert.True(vic.IsBadLine);

        AdvanceTo(vic, 0x30, 54); // All 40 matrix slots latched (cycles 15..54).
        Assert.Equal((byte)(((entry.VcBase + 3) & 0x3FF) ^ 0x5C), vic.PeekVideoMatrixLatch(3));
        Assert.Equal((byte)(((entry.VcBase + 20) & 0x3FF) ^ 0x5C), vic.PeekVideoMatrixLatch(20));
        Assert.Equal((byte)(((entry.VcBase + 39) & 0x3FF) ^ 0x5C), vic.PeekVideoMatrixLatch(39));
    }

    /// <summary>
    /// FR-VIC-FETCH AC-05.
    /// Use case: the c-access latches colour data as the low nibble of colour RAM
    /// (VICE viciisc/vicii-fetch.c:199, managed C64MemoryMap.cs:795 plus the
    /// Mos6569.LatchVideoMatrixFetch nibble mask).
    /// Acceptance: on bad line $30, matrix slots latch exactly the seeded colour
    /// nibble (index low nibble) for the fetched cell, and the latch itself
    /// truncates a wide colour byte $AB to exactly $0B.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-05", ParityTag.Faithful)]
    public void CAccessLatchesColorRamLowNibble()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10);
        vic.Write(MemoryPointers, 0x14);
        for (var k = 0; k < 0x400; k++)
            memory.Write((ushort)(0xD800 + k), (byte)(k & 0x0F));

        AdvanceTo(vic, 0x30, 12);
        var entry = ReadInternals(vic);
        Assert.Equal(40, entry.VcBase);

        AdvanceTo(vic, 0x30, 54);
        Assert.Equal((byte)((entry.VcBase + 3) & 0x0F), vic.PeekColorMatrixLatch(3));
        Assert.Equal((byte)((entry.VcBase + 39) & 0x0F), vic.PeekColorMatrixLatch(39));

        // The latch masks to the low nibble on the chip side as well.
        vic.LatchVideoMatrixFetch(5, 0x42, 0xAB);
        Assert.Equal(0x0B, vic.PeekColorMatrixLatch(5));
    }

    /// <summary>
    /// FR-VIC-FETCH AC-07.
    /// Use case: bad-line prefetch slots seed vbuf with $FF and cbuf with the low
    /// nibble of the CPU program RAM byte (VICE viciisc/vicii-fetch.c:195-196,
    /// managed C64MemoryMap.cs:789 via Mos6569.LatchVideoMatrixPrefetch).
    /// Acceptance: with RAM at the CPU PC seeded to $AB, the first three matrix
    /// slots of bad line $30 latch matrix byte exactly $FF and colour nibble
    /// exactly $0B.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-07", ParityTag.Faithful)]
    public void PrefetchLatchesVbufFfAndCbufFromCpuProgramRam()
    {
        var (machine, vic, memory) = CreatePalC64Machine();
        var cpu = (ICpu)machine.Devices.GetByRole(DeviceRole.Cpu)!;
        vic.Write(ScreenControl1, 0x10);
        vic.Write(MemoryPointers, 0x14);
        memory.Span[cpu.PC] = 0xAB; // Raw RAM under the KERNAL reset vector target.

        AdvanceTo(vic, 0x30, 17); // Prefetch slots 0..2 latched at cycles 15..17.
        Assert.True(vic.IsBadLine);
        Assert.Equal(0xFF, vic.PeekVideoMatrixLatch(0));
        Assert.Equal(0xFF, vic.PeekVideoMatrixLatch(1));
        Assert.Equal(0xFF, vic.PeekVideoMatrixLatch(2));
        Assert.Equal(0x0B, vic.PeekColorMatrixLatch(0));
        Assert.Equal(0x0B, vic.PeekColorMatrixLatch(1));
        Assert.Equal(0x0B, vic.PeekColorMatrixLatch(2));
    }

    /// <summary>
    /// FR-VIC-FETCH AC-10.
    /// Use case: the idle-state graphics fetch reads $39FF under ECM and $3FFF
    /// otherwise (VICE viciisc/vicii-fetch.c:213-232, managed Mos6569.cs:1700-1702;
    /// the reg11-delay source aspect is DIVERGENT AC-08, not asserted here).
    /// Acceptance: the idle fetch address is exactly $3FFF with ECM clear, $39FF
    /// with ECM set, and back to $3FFF when cleared; end to end in idle state the
    /// Phi1 data equals the byte seeded at $3FFF, then at $39FF once ECM is set.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-10", ParityTag.Faithful)]
    public void IdleGraphicsFetchReads39FFUnderEcmElse3FFF()
    {
        var (vic, memory) = CreatePalC64();
        memory.Span[0x3FFF] = 0x91;
        memory.Span[0x39FF] = 0x64;

        Assert.Equal(0x3FFF, vic.IdleGraphicsFetchAddress);

        // Lines past 7 are idle (rc==7 idle entry at line 7 cycle 58, DEN off).
        AdvanceTo(vic, 0x09, 19); // A graphics slot deep inside the idle line.
        Assert.True(vic.IsGraphicsIdle);
        Assert.Equal(0x91, vic.LastReadPhi1);

        vic.Write(ScreenControl1, 0x40); // ECM on (DEN off keeps the line idle).
        Assert.Equal(0x39FF, vic.IdleGraphicsFetchAddress);
        vic.Tick(); // Cycle 20: idle graphics fetch under ECM.
        Assert.Equal(20, vic.RasterX);
        Assert.Equal(0x64, vic.LastReadPhi1);

        vic.Write(ScreenControl1, 0x00);
        Assert.Equal(0x3FFF, vic.IdleGraphicsFetchAddress);
        vic.Tick();
        Assert.Equal(0x91, vic.LastReadPhi1);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-11.
    /// Use case: idle non-graphics gap fetches read $3FFF (VICE
    /// viciisc/vicii-fetch.c:208-211, managed C64MemoryMap.cs:802).
    /// Acceptance: with $3FFF seeded to $6E, the PAL gap cycles 1, 3 and 55 all
    /// return exactly $6E on a display-window line.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-11", ParityTag.Faithful)]
    public void IdleGapFetchReads3FFF()
    {
        var (vic, memory) = CreatePalC64();
        memory.Span[0x3FFF] = 0x6E;

        AdvanceTo(vic, 0x01, 0);
        vic.Tick(); // Cycle 1: gap between sprite pointer fetches.
        Assert.Equal(1, vic.RasterX);
        Assert.Equal(0x6E, vic.LastReadPhi1);

        vic.Tick();
        vic.Tick(); // Cycle 3: gap.
        Assert.Equal(3, vic.RasterX);
        Assert.Equal(0x6E, vic.LastReadPhi1);

        AdvanceTo(vic, 0x01, 55); // Cycle 55: post-display gap.
        Assert.Equal(0x6E, vic.LastReadPhi1);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-12.
    /// Use case: the sprite p-access reads v_fetch_addr($3F8 + n) =
    /// ((reg $D018 &amp; $F0) &lt;&lt; 6) + $3F8 + n (VICE viciisc/vicii-fetch.c:275-280,
    /// managed C64MemoryMap.cs:767-771).
    /// Acceptance: with screen base $0400 the sprite 0 pointer slot (cycle 57)
    /// returns exactly the byte at $07F8; after switching the base to $0800 the
    /// sprite 1 pointer slot (cycle 59) returns exactly the byte at $0BF9.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-12", ParityTag.Faithful)]
    public void SpritePointerFetchAddressIsScreenBasePlus3F8PlusIndex()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(MemoryPointers, 0x14);   // Screen base $0400.
        memory.Span[0x07F8] = 0x90;        // $0400 + $3F8 + 0.
        memory.Span[0x0BF9] = 0xA1;        // $0800 + $3F8 + 1.

        AdvanceTo(vic, 0x03, 56);
        vic.Tick(); // Cycle 57: sprite 0 pointer fetch.
        Assert.Equal(57, vic.RasterX);
        Assert.Equal(0x90, vic.LastReadPhi1);

        vic.Write(MemoryPointers, 0x24);   // Screen base $0800.
        vic.Tick(); // Cycle 58: gap.
        vic.Tick(); // Cycle 59: sprite 1 pointer fetch from the new base.
        Assert.Equal(59, vic.RasterX);
        Assert.Equal(0xA1, vic.LastReadPhi1);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-13.
    /// Use case: PAL sprite p-access schedule: sprites 3..7 at managed cycles
    /// 0, 2, 4, 6, 8 and sprites 0..2 at cycles 57, 59, 61 (VICE Phi1 cycles
    /// 1, 3, 5, 7, 9 and 58, 60, 62; vicii-chip-model.c:112-236, managed
    /// C64MemoryMap.cs:697-718).
    /// Acceptance: with each sprite pointer cell seeded as $90 + n and the gap at
    /// $3FFF seeded as $6E, a full idle line returns exactly the matching pointer
    /// byte at every p-access cycle and exactly $6E at the interleaved gap cycles.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-13", ParityTag.Faithful)]
    public void SpritePointerFetchesFollowPalScheduleSlots()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(MemoryPointers, 0x14); // Screen base $0400 -> pointers at $07F8.
        for (var n = 0; n < 8; n++)
            memory.Span[0x07F8 + n] = (byte)(0x90 + n);
        memory.Span[0x3FFF] = 0x6E;

        AdvanceTo(vic, 0x08, 62);
        var seen = new byte[Mos6569.PalCyclesPerLine];
        for (var cycle = 0; cycle < Mos6569.PalCyclesPerLine; cycle++)
        {
            vic.Tick();
            Assert.Equal(cycle, vic.RasterX);
            seen[cycle] = vic.LastReadPhi1;
        }

        Assert.Equal(0x93, seen[0]);  // Sprite 3.
        Assert.Equal(0x94, seen[2]);  // Sprite 4.
        Assert.Equal(0x95, seen[4]);  // Sprite 5.
        Assert.Equal(0x96, seen[6]);  // Sprite 6.
        Assert.Equal(0x97, seen[8]);  // Sprite 7.
        Assert.Equal(0x90, seen[57]); // Sprite 0.
        Assert.Equal(0x91, seen[59]); // Sprite 1.
        Assert.Equal(0x92, seen[61]); // Sprite 2.

        Assert.Equal(0x6E, seen[1]);
        Assert.Equal(0x6E, seen[3]);
        Assert.Equal(0x6E, seen[5]);
        Assert.Equal(0x6E, seen[7]);
        Assert.Equal(0x6E, seen[9]);
        Assert.Equal(0x6E, seen[55]);
        Assert.Equal(0x6E, seen[56]);
        Assert.Equal(0x6E, seen[58]);
        Assert.Equal(0x6E, seen[60]);
        Assert.Equal(0x6E, seen[62]);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-15.
    /// Use case: VIC Phi1 addresses in [$1000, $2000) resolve to character ROM in
    /// banks 0 and 2, and to RAM in bank 1 (VICE viciisc/vicii-fetch.c:50-74,
    /// managed C64MemoryMap.cs:818-830).
    /// Acceptance: a text g-access at char address $1040 returns exactly the
    /// character ROM byte in bank 0, exactly the planted RAM byte at $5040 in
    /// bank 1, and exactly the character ROM byte again in bank 2, with RAM
    /// underlays planted differently at $1040 and $9040.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-15", ParityTag.Faithful)]
    public void Phi1CharRomWindowMapsToCharRomInBanks0And2()
    {
        var (vic, memory) = CreatePalC64();
        var charRomByte = MachineTestFactory.LoadC64Rom("characters").Span[0x40];
        var bank0Underlay = (byte)(charRomByte ^ 0xFF);
        var bank1Plant = (byte)(charRomByte ^ 0x55);
        var bank2Underlay = (byte)(charRomByte ^ 0xAA);
        memory.Span[0x1040] = bank0Underlay; // Bank 0 RAM under the char ROM window.
        memory.Span[0x5040] = bank1Plant;    // Bank 1: same VIC address, real RAM.
        memory.Span[0x9040] = bank2Underlay; // Bank 2 RAM under the char ROM window.

        // Text mode g-access address (char code 8, rc 0, char base $1000) = $1040.
        vic.Write(MemoryPointers, 0x04);
        for (var slot = 0; slot < 40; slot++)
            vic.LatchVideoMatrixFetch(slot, 0x08, 0x01);

        AdvanceTo(vic, 0x00, 14);
        vic.Tick(); // Cycle 15: g-access, phi1 bank 0 -> char ROM.
        Assert.Equal(15, vic.RasterX);
        Assert.Equal(charRomByte, vic.LastReadPhi1);

        // CIA2 port A bits 0-1 select the VIC bank (inverted): pins %10 -> bank 1.
        memory.Write(0xDD02, 0x03);
        memory.Write(0xDD00, 0x02);
        vic.Tick(); // Cycle 16: same VIC address, bank 1 -> RAM at $5040.
        Assert.Equal(16, vic.RasterX);
        Assert.Equal(bank1Plant, vic.LastReadPhi1);

        memory.Write(0xDD00, 0x01); // Pins %01 -> bank 2.
        vic.Tick(); // Cycle 17: bank 2 -> char ROM again.
        Assert.Equal(17, vic.RasterX);
        Assert.Equal(charRomByte, vic.LastReadPhi1);
    }

    // ----------------------------------------------------------------
    // FR-VIC-MATRIX-ADDR: matrix/graphics addressing from VC
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-01.
    /// Use case: the hot-path video matrix address is v_fetch_addr(vc) =
    /// ((reg $D018 &amp; $F0) &lt;&lt; 6) + (vc &amp; $3FF) (VICE
    /// viciisc/vicii-fetch.c:158-161,198, managed Mos6569.cs:1714,1720).
    /// Acceptance: with screen base $2000 and vc advanced to 7, the matrix fetch
    /// address is exactly $2007; at vc $3FF it is exactly $23FF and one further
    /// g-access wraps it to exactly $2000.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-01", ParityTag.Faithful)]
    public void HotPathMatrixFetchAddressIsScreenBasePlusVc()
    {
        var vic = BuildVic();
        vic.Write(MemoryPointers, 0x84); // Screen base ($80 shifted left 6) = $2000.

        ConsumeGraphicsFetches(vic, 7);
        Assert.Equal(7, vic.CurrentVideoMatrixCounter);
        Assert.Equal((ushort)0x2007, vic.CurrentVideoMatrixFetchAddress);

        ConsumeGraphicsFetches(vic, 0x3FF - 7);
        Assert.Equal(0x3FF, vic.CurrentVideoMatrixCounter);
        Assert.Equal((ushort)0x23FF, vic.CurrentVideoMatrixFetchAddress);

        ConsumeGraphicsFetches(vic, 1);
        Assert.Equal(0, vic.CurrentVideoMatrixCounter);
        Assert.Equal((ushort)0x2000, vic.CurrentVideoMatrixFetchAddress);
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-04.
    /// Use case: each g-access advances the pipeline: vmli++ and vc = (vc + 1) &amp;
    /// $3FF (VICE viciisc/vicii-fetch.c:267-270, managed Mos6569.cs:1680-1681).
    /// Acceptance: one g-access moves vmli 0 to 1 and vc 0 to 1; four more move
    /// them to exactly 5; driving vc to $3FF and consuming once wraps vc to
    /// exactly 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-04", ParityTag.Faithful)]
    public void GraphicsAccessIncrementsVmliAndWrapsVcAt3FF()
    {
        var vic = BuildVic();
        Assert.Equal(0, vic.CurrentVideoMatrixSlot);
        Assert.Equal(0, vic.CurrentVideoMatrixCounter);

        vic.ConsumeGraphicsFetchAddress();
        Assert.Equal(1, vic.CurrentVideoMatrixSlot);
        Assert.Equal(1, vic.CurrentVideoMatrixCounter);

        ConsumeGraphicsFetches(vic, 4);
        Assert.Equal(5, vic.CurrentVideoMatrixSlot);
        Assert.Equal(5, vic.CurrentVideoMatrixCounter);

        ConsumeGraphicsFetches(vic, 0x3FF - 5);
        Assert.Equal(0x3FF, vic.CurrentVideoMatrixCounter);
        vic.ConsumeGraphicsFetchAddress();
        Assert.Equal(0, vic.CurrentVideoMatrixCounter);
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-05.
    /// Use case: the bitmap-mode g-access address is (vc &lt;&lt; 3) | rc with the
    /// bitmap bank bit (reg $D018 &amp; 8) &lt;&lt; 10 (VICE
    /// viciisc/vicii-fetch.c:168-170, managed Mos6569.cs:1665-1668).
    /// Acceptance: with rc = 2 (line 2), vc = 5, BMM set and $D018 = $08 the
    /// g-access address is exactly $202A; with $D018 = $00 the next g-access
    /// (vc = 6) is exactly $0032.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-05", ParityTag.Faithful)]
    public void GraphicsFetchAddressBitmapModeUsesVcRcAndD018Bit3()
    {
        var vic = BuildVic();
        AdvanceTo(vic, 0x02, 20); // rc incremented at cycle 58 of lines 0 and 1.
        Assert.Equal(2, vic.CurrentRowCounter);

        ConsumeGraphicsFetches(vic, 5); // vc = 5 (reset to vcbase 0 at cycle 14).
        Assert.Equal(5, vic.CurrentVideoMatrixCounter);

        vic.Write(ScreenControl1, 0x20); // BMM on.
        vic.Write(MemoryPointers, 0x08); // Bitmap bank bit set -> +$2000.
        Assert.Equal((ushort)0x202A, vic.ConsumeGraphicsFetchAddress());

        vic.Write(MemoryPointers, 0x00); // Bitmap bank bit clear.
        Assert.Equal((ushort)0x0032, vic.ConsumeGraphicsFetchAddress()); // (vc 6 * 8) OR rc 2.
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-06.
    /// Use case: the text-mode g-access address is (vbuf[vmli] &lt;&lt; 3) | rc with the
    /// character base (reg $D018 &amp; $0E) &lt;&lt; 10 (VICE
    /// viciisc/vicii-fetch.c:171-174, managed Mos6569.cs:1670-1674).
    /// Acceptance: with rc = 1 (line 1), latched character $23 in slot 0 and
    /// $D018 = $05 (bit 0 excluded by the $0E mask, char base $1000) the g-access
    /// address is exactly $1119.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-06", ParityTag.Faithful)]
    public void GraphicsFetchAddressTextModeUsesVbufRcAndD018CharBase()
    {
        var vic = BuildVic();
        AdvanceTo(vic, 0x01, 20); // rc = 1; vmli reset to 0 at cycle 14.
        Assert.Equal(1, vic.CurrentRowCounter);
        Assert.Equal(0, vic.CurrentVideoMatrixSlot);

        vic.LatchVideoMatrixFetch(0, 0x23, 0x05);
        vic.Write(MemoryPointers, 0x05); // Char base from $0E mask = $1000; bit 0 ignored.

        Assert.Equal((ushort)0x1119, vic.ConsumeGraphicsFetchAddress()); // ($23 * 8) OR rc 1 OR $1000.
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-07.
    /// Use case: under ECM the g-access address is masked with $39FF, forcing
    /// address bits 9 and 10 low (VICE viciisc/vicii-fetch.c:177-179, managed
    /// Mos6569.cs:1677-1678).
    /// Acceptance: a text g-access of character $FF at char base $1800 raises raw
    /// address $1FF8; with ECM set the fetched address is exactly $19F8, and with
    /// ECM clear the next g-access is exactly the unmasked $1FF8.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-07", ParityTag.Faithful)]
    public void GraphicsFetchAddressEcmMasksTo39FF()
    {
        var vic = BuildVic();
        vic.LatchVideoMatrixFetch(0, 0xFF, 0x01);
        vic.LatchVideoMatrixFetch(1, 0xFF, 0x01);
        vic.Write(MemoryPointers, 0x06); // Char base ($06 shifted left 10) = $1800.

        vic.Write(ScreenControl1, 0x40); // ECM on.
        Assert.Equal((ushort)0x19F8, vic.ConsumeGraphicsFetchAddress()); // $1FF8 masked with $39FF.

        vic.Write(ScreenControl1, 0x00); // ECM off.
        Assert.Equal((ushort)0x1FF8, vic.ConsumeGraphicsFetchAddress());
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-10.
    /// Use case: the hot-path colour RAM index for the c-access is vc itself (VICE
    /// viciisc/vicii-fetch.c:199, managed C64MemoryMap.cs:793-795).
    /// Acceptance: on bad line $30 with vcbase 40 and colour RAM seeded with each
    /// cell's index low nibble, slots 3, 17 and 39 latch exactly the nibble at
    /// index vcbase + slot (which differs from a slot-indexed or geometric lookup
    /// because vcbase mod 16 is 8).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-10", ParityTag.Faithful)]
    public void ColorRamFetchIndexIsVc()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10);
        vic.Write(MemoryPointers, 0x14);
        for (var k = 0; k < 0x400; k++)
            memory.Write((ushort)(0xD800 + k), (byte)(k & 0x0F));

        AdvanceTo(vic, 0x30, 12);
        var entry = ReadInternals(vic);
        Assert.Equal(40, entry.VcBase); // vcbase mod 16 = 8 discriminates vc from slot.

        AdvanceTo(vic, 0x30, 54);
        Assert.Equal((byte)((entry.VcBase + 3) & 0x0F), vic.PeekColorMatrixLatch(3));
        Assert.Equal((byte)((entry.VcBase + 17) & 0x0F), vic.PeekColorMatrixLatch(17));
        Assert.Equal((byte)((entry.VcBase + 39) & 0x0F), vic.PeekColorMatrixLatch(39));
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds a bare PAL VIC-II (no memory map) and resets it to the canonical
    /// power-on phase: line 0, RasterX 6 (Mos6569.ResetRasterCycle), all pipeline
    /// counters zero, idle and allow_bad_lines false.
    /// </summary>
    private static Mos6569 BuildVic()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>
    /// Builds a full managed C64 (real C64MemoryMap wired as the VIC's Phi1 reader)
    /// and returns the VIC plus the memory map. The machine is freshly reset: the
    /// VIC sits at line 0, RasterX 6, phi1 bank 0, and no CPU cycles have run, so
    /// driving vic.Tick() directly exercises the product fetch dispatch
    /// deterministically.
    /// </summary>
    private static (IMachine Machine, Mos6569 Vic, IMemory Memory) CreatePalC64Machine()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        var memory = (IMemory)machine.Devices.GetByRole(DeviceRole.SystemRam)!;
        Assert.Equal(Mos6569.PalCyclesPerLine, vic.CyclesPerLine);
        return (machine, vic, memory);
    }

    private static (Mos6569 Vic, IMemory Memory) CreatePalC64()
    {
        var (_, vic, memory) = CreatePalC64Machine();
        return (vic, memory);
    }

    private static void Advance(Mos6569 vic, int cycles)
    {
        for (var cycle = 0; cycle < cycles; cycle++)
            vic.Tick();
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

    private static void ConsumeGraphicsFetches(Mos6569 vic, int count)
    {
        for (var i = 0; i < count; i++)
            vic.ConsumeGraphicsFetchAddress();
    }

    /// <summary>
    /// Reads the live VC/RC/VCBASE/refresh/idle/allow_bad_lines internals through
    /// the public IStatefulDevice capture surface (Mos6569.State.cs), avoiding any
    /// reliance on private fields.
    /// </summary>
    private static VicInternals ReadInternals(Mos6569 vic)
    {
        var state = new byte[vic.StateSize];
        vic.CaptureState(state);
        var fields = vic.DecodeState(state);
        int Field(string name) => fields.First(f => f.Name == name).Value;
        return new VicInternals(
            Vc: Field("VC"),
            Rc: Field("RC"),
            VcBase: Field("VCBASE"),
            Refresh: Field("REFRESH"),
            Idle: Field("IDLE") != 0,
            AllowBadLines: Field("BAD-LINES") != 0);
    }

    private readonly record struct VicInternals(int Vc, int Rc, int VcBase, int Refresh, bool Idle, bool AllowBadLines);
}
