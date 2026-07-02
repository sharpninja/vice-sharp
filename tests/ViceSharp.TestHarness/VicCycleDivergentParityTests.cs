namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V1 / TR-PARITY-GATE-001: DIVERGENT (red-now remediation
/// target) parity tests for FR-VIC-CYCLE, FR-VIC-FETCH and FR-VIC-MATRIX-ADDR from
/// artifacts/vice-parity-requirements/requirements.yaml. One test method per DIVERGENT
/// acceptance criterion; each method asserts the exact VICE mechanism
/// (native/vice/vice/src/viciisc/vicii-cycle.c, vicii-fetch.c, vicii-chip-model.c) and
/// starts red against the managed implementation. FAITHFUL criteria live in
/// VicCycleFaithfulParityTests.
///
/// Cycle numbering note: VICE raster cycles are 1-based; the managed VIC uses
/// RasterX = VICII_PAL_CYCLE(n) = n - 1. VICE "cycle 15" is managed RasterX 14.
/// Managed Tick() increments RasterX first, so after a Tick() returns, the cycle
/// equal to RasterX has just been processed (including its Phi1 fetch, observable
/// via LastReadPhi1, and its Phi2 side effects such as the c-access).
/// </summary>
public sealed class VicCycleDivergentParityTests
{
    private const ushort SpriteY0 = 0xD001;
    private const ushort ScreenControl1 = 0xD011;
    private const ushort ScreenControl2 = 0xD016;
    private const ushort SpriteEnable = 0xD015;
    private const ushort MemoryPointers = 0xD018;

    // ----------------------------------------------------------------
    // FR-VIC-CYCLE: per-cycle state engine (VC/RC/VMLI/idle/badline)
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-CYCLE AC-04.
    /// Use case: check_badline runs every cycle while bad lines are allowed and forces
    /// idle_state = 0 on every cycle of a matching line, not just at the VC update
    /// (VICE viciisc/vicii-cycle.c:51-60 check_badline, called per cycle from
    /// vicii-cycle.c:529-531). The managed VIC only clears idle at RasterX 13
    /// (Mos6569.cs:945), so idle wrongly persists through cycles 0-12 of a bad line.
    /// Acceptance: with DEN and YSCROLL=0, the chip is idle at line $37 cycle 57
    /// (rc==7 idle entry) and must already be out of idle at cycle 5 of the next bad
    /// line $38, well before the VC-update cycle 13.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-04", ParityTag.Divergent, pending: false)]
    public void IdleStateForcedZeroPerCycleOnBadLine()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0 -> bad lines $30, $38, ...

        AdvanceTo(vic, 0x37, 57); // rc==7 at the RC update: idle_state = 1.
        Assert.True(vic.IsGraphicsIdle);

        AdvanceTo(vic, 0x38, 5); // Bad line $38, still before the VC update at 13.
        Assert.False(vic.IsGraphicsIdle); // VICE cleared idle at cycle 0 already.
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-07.
    /// Use case: at the start of every frame the video counter is reset: vc = 0
    /// (VICE viciisc/vicii-cycle.c:209 in vicii_cycle_start_of_frame). The managed
    /// frame wrap (Mos6569.cs:980-1002) never resets vc, so the boot-frame value
    /// (40 + 25 rows x 40, masked to 10 bits = 16) leaks into frame 2.
    /// Acceptance: after a full display frame (DEN, YSCROLL=0) the captured VC
    /// internal is exactly 0 when the raster has wrapped to line 0 cycle 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-07", ParityTag.Divergent, pending: false)]
    public void VcResetsToZeroAtFrameStart()
    {
        var (vic, _) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10); // DEN=1: full 25-row display frame.

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 0);
        AdvanceTo(vic, 0x00, 0); // Frame wrap into frame 2.

        var atFrameStart = ReadInternals(vic);
        Assert.Equal(0, atFrameStart.Vc);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-08.
    /// Use case: at the start of every frame the video base is reset: vcbase = 0
    /// (VICE viciisc/vicii-cycle.c:208 in vicii_cycle_start_of_frame). The managed
    /// frame wrap (Mos6569.cs:980-1002) never resets vcbase, so the end-of-frame
    /// capture ((40 + 1000) &amp; $3FF = 16) persists into the next frame.
    /// Acceptance: after a full display frame (DEN, YSCROLL=0) the captured VCBASE
    /// internal is exactly 0 when the raster has wrapped to line 0 cycle 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-08", ParityTag.Divergent, pending: false)]
    public void VcBaseResetsToZeroAtFrameStart()
    {
        var (vic, _) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10); // DEN=1: full 25-row display frame.

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 0);
        AdvanceTo(vic, 0x00, 0); // Frame wrap into frame 2.

        var atFrameStart = ReadInternals(vic);
        Assert.Equal(0, atFrameStart.VcBase);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-12.
    /// Use case: VICE arms start_of_frame at raster cycle 1 of the last line
    /// (vicii_cycle_end_of_line, viciisc/vicii-cycle.c:222-226) and applies the frame
    /// reset (raster_line = 0, refresh $FF, allow_bad_lines off, vc/vcbase 0) one
    /// cycle later at raster cycle 2 (vicii-cycle.c:453-456 with
    /// VICII_PAL_CYCLE(2) = managed RasterX 1). The managed VIC applies everything at
    /// the line wrap (RasterX 0), one cycle early, so raster_line never reads the
    /// last line during the wrapped line's first cycle.
    /// Acceptance: from line 311 cycle 62, the first tick leaves the raster line at
    /// 311 with RasterX 0; the second tick (RasterX 1) performs the frame reset:
    /// raster line 0, refresh counter $FF, allow_bad_lines false.
    /// SLICE V1 STOP: remediation conflicts with the FAITHFUL lock
    /// TEST-VIC-CYCLE-11 (one tick from (311,62) must yield (0,0)), which encodes
    /// the managed wrap timing. Kept pending until the lock conflict is resolved
    /// by the parity plan owner.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-12", ParityTag.Divergent, pending: true)]
    public void FrameResetAppliesAtRasterCycle1OfLastLine()
    {
        var vic = BuildVic();

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 62);
        Assert.Equal(311, vic.CurrentRasterLine);

        vic.Tick(); // VICE raster cycle 1: start_of_frame armed, line NOT yet reset.
        Assert.Equal(0, vic.RasterX);
        Assert.Equal(311, vic.CurrentRasterLine);

        vic.Tick(); // VICE raster cycle 2: vicii_cycle_start_of_frame applies.
        Assert.Equal(1, vic.RasterX);
        Assert.Equal(0, vic.CurrentRasterLine);
        var atFrameStart = ReadInternals(vic);
        Assert.Equal(0xFF, atFrameStart.Refresh);
        Assert.False(atFrameStart.AllowBadLines);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-14.
    /// Use case: bad lines are disallowed by an equality check on the just-completed
    /// line: allow_bad_lines = 0 only when raster_line == VICII_LAST_DMA_LINE ($F7)
    /// at the start-of-line point (VICE viciisc/vicii-cycle.c:236-238). The managed
    /// VIC instead clears the latch on every line whose number exceeds $F7
    /// (Mos6569.cs:2097-2098), which wrongly strips an allow_bad_lines state that was
    /// restored past the boundary (snapshot resume), where VICE keeps it.
    /// Acceptance: with allow_bad_lines armed and the raster phase snapshot-injected
    /// to line $FA, ticking across the next start of line ($FB) must leave
    /// allow_bad_lines true (no completed line equalled $F7); the natural boundary
    /// still turns the latch off at the first cycle of line $F8.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-14", ParityTag.Divergent, pending: false)]
    public void AllowBadLinesOffKeyedOnCompletedLineF7()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x12); // DEN=1, YSCROLL=2: latch arms at line $30.

        AdvanceTo(vic, 0x40, 10);
        Assert.True(ReadInternals(vic).AllowBadLines);

        // Snapshot-resume style teleport past the $F7 boundary; the injection seeds
        // registers and raster phase only, allow_bad_lines survives as-is.
        var registers = new byte[64];
        registers[0x11] = 0x12;
        vic.InjectSnapshotState(registers, rasterLine: 0xFA, inLineCycle: 5);

        Advance(vic, Mos6569.PalCyclesPerLine); // Cross the start of line $FB.
        Assert.Equal(0xFB, vic.CurrentRasterLine);
        Assert.Equal(5, vic.RasterX);
        Assert.True(ReadInternals(vic).AllowBadLines); // VICE: only ==$F7 clears it.

        // Natural boundary contract: completed line $F7 turns the latch off, so it
        // is false from the first cycle of line $F8 onward.
        var natural = BuildVic();
        natural.Write(ScreenControl1, 0x17); // DEN=1, YSCROLL=7: $F7 is a bad line.
        AdvanceTo(natural, 0xF7, 10);
        Assert.True(ReadInternals(natural).AllowBadLines);
        AdvanceTo(natural, 0xF8, 0);
        Assert.False(ReadInternals(natural).AllowBadLines);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-15.
    /// Use case: on the first DMA line ($30) the DEN bit is re-checked on EVERY cycle:
    /// if (raster_line == $30 &amp;&amp; !allow_bad_lines) allow_bad_lines = DEN (VICE
    /// viciisc/vicii-cycle.c:524-526). The managed VIC samples DEN only at the start
    /// of line $30 (Mos6569.cs:2094-2095), so setting DEN mid-line $30 never arms bad
    /// lines for the whole frame.
    /// Acceptance: with DEN off through line $30 cycle 10, writing DEN=1 (YSCROLL=0)
    /// arms allow_bad_lines on the very next cycle and line $30 immediately reports
    /// the bad-line condition; a control VIC with DEN still off stays disarmed.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-15", ParityTag.Divergent, pending: false)]
    public void DenRecheckedPerCycleOnLine30()
    {
        var vic = BuildVic();

        AdvanceTo(vic, 0x30, 10);
        Assert.False(ReadInternals(vic).AllowBadLines);

        vic.Write(ScreenControl1, 0x10); // DEN set mid-line $30.
        vic.Tick(); // Per-cycle DEN re-check at cycle 11.
        Assert.Equal(11, vic.RasterX);
        Assert.True(ReadInternals(vic).AllowBadLines);
        Assert.True(vic.IsBadLine); // (raster line low bits) == YSCROLL == 0.

        // Control: without DEN the per-cycle check keeps assigning 0.
        var noDen = BuildVic();
        AdvanceTo(noDen, 0x30, 11);
        Assert.False(ReadInternals(noDen).AllowBadLines);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-16.
    /// Use case: the per-cycle bad-line condition is only (raster_line &amp; 7) ==
    /// ysmooth under allow_bad_lines; check_badline has no raster-range clamp of its
    /// own (VICE viciisc/vicii-cycle.c:54,529-531). The managed IsBadLine adds a
    /// &lt;= $F7 clamp (Mos6569.cs:174,2085-2090), so a snapshot-resumed state with
    /// allow_bad_lines armed past the boundary never fires the bad-line machinery
    /// where VICE does.
    /// Acceptance: with allow_bad_lines armed, YSCROLL=2 and the raster phase
    /// injected to line $FA (low bits 2), the bad-line condition reports true and
    /// the VC-update cycle 13 resets rc to 0, exactly as on any matching line.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-16", ParityTag.Divergent, pending: false)]
    public void BadLineLatchHasNoUpperRasterClamp()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x12); // DEN=1, YSCROLL=2 -> bad lines $32, $3A, $42...

        AdvanceTo(vic, 0x40, 10);
        Assert.Equal(6, vic.CurrentRowCounter); // rc from rows $3A..$3F.
        Assert.True(ReadInternals(vic).AllowBadLines);

        var registers = new byte[64];
        registers[0x11] = 0x12;
        vic.InjectSnapshotState(registers, rasterLine: 0xFA, inLineCycle: 5);

        vic.Tick(); // Cycle 6 of line $FA: check_badline matches ($FA &amp; 7 == 2).
        Assert.True(vic.IsBadLine); // VICE has no upper clamp.

        AdvanceTo(vic, 0xFA, 13); // VC update on a bad line: rc = 0.
        Assert.Equal(0, vic.CurrentRowCounter);
    }

    /// <summary>
    /// FR-VIC-CYCLE AC-17.
    /// Use case: bad_line is a stored per-cycle latch: set/cleared by check_badline
    /// once per cycle and force-cleared at start-of-line (VICE
    /// viciisc/vicii-cycle.c:54-59,240); consumers such as the BA/stall logic read
    /// the latch, not a live formula. The managed VIC has no stored bad_line: a
    /// register write between cycles instantly flips the derived value, which VICE
    /// only reflects at the next cycle.
    /// Acceptance: on bad line $38 cycle 30 the CPU stall line is asserted; writing
    /// YSCROLL=5 (no longer matching) between ticks must leave the stall asserted
    /// (latch not yet re-evaluated); the next tick re-runs check_badline and clears
    /// it, and the following non-matching line starts with the latch cleared.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-CYCLE-17", ParityTag.Divergent, pending: false)]
    public void BadLineIsAStoredLatchClearedAtStartOfLine()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0 -> $38 is a bad line.

        AdvanceTo(vic, 0x38, 30); // Inside the bad-line BA window (12..54).
        Assert.True(vic.IsCpuCycleStolen);

        vic.Write(ScreenControl1, 0x15); // YSCROLL=5: no match, latch not yet updated.
        Assert.True(vic.IsCpuCycleStolen); // VICE: stall still driven by the latch.

        vic.Tick(); // check_badline re-evaluates: latch drops.
        Assert.Equal(31, vic.RasterX);
        Assert.False(vic.IsCpuCycleStolen);

        // Start-of-line clear: after matching bad line $3D (low bits 5), the
        // non-matching line $3E begins with the latch cleared, so no stall fires
        // inside the BA window.
        AdvanceTo(vic, 0x3D, 30);
        Assert.True(vic.IsCpuCycleStolen);
        AdvanceTo(vic, 0x3E, 12);
        Assert.False(vic.IsCpuCycleStolen);
    }

    // ----------------------------------------------------------------
    // FR-VIC-FETCH: Phi1/Phi2 fetch schedule
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-FETCH AC-03.
    /// Use case: the c-access (Phi2) leads its g-access by one slot: the first
    /// FetchC is Phi2 of VICE cycle 15 (managed RasterX 14) and the first FetchG is
    /// Phi1 of cycle 16 (RasterX 15) (VICE vicii-chip-model.c:139-141). The managed
    /// map fuses c+g on the same cycle starting at RasterX 15
    /// (C64MemoryMap.cs:717,784-799), so every matrix latch lands one cycle late.
    /// Acceptance: on bad line $30 (vcbase 40, screen $0400 seeded k xor $5C, colour
    /// RAM seeded k &amp; $0F), matrix slot 3 is already latched with real data
    /// (matrix byte (43) xor $5C, colour nibble 43 &amp; $0F) once RasterX 17 has been
    /// processed (VICE: c-access slot 3 at Phi2 of cycle 18 = RasterX 17).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-03", ParityTag.Divergent, pending: false)]
    public void CAccessLeadsGAccessByOneSlot()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10);  // DEN=1, YSCROLL=0 -> bad line $30.
        vic.Write(MemoryPointers, 0x14);  // Screen base $0400.
        for (var k = 0; k < 0x400; k++)
        {
            memory.Span[0x0400 + k] = (byte)(k ^ 0x5C);
            memory.Write((ushort)(0xD800 + k), (byte)(k & 0x0F));
        }

        AdvanceTo(vic, 0x30, 12);
        var entry = ReadInternals(vic);
        Assert.Equal(40, entry.VcBase); // Deterministic boot value.

        AdvanceTo(vic, 0x30, 17); // Phi2 c-access for slot 3 has just run in VICE.
        Assert.True(vic.TryReadVideoMatrixLatch(3, out var matrixByte, out var colorNibble));
        Assert.Equal((byte)(((entry.VcBase + 3) & 0x3FF) ^ 0x5C), matrixByte);
        Assert.Equal((byte)((entry.VcBase + 3) & 0x0F), colorNibble);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-06.
    /// Use case: the prefetch garbage path of the matrix fetch is gated by the
    /// prefetch_cycles counter, which counts 3+1 down while BA is low and reaches 0
    /// before the first c-access of a regular bad line because BA drops at cycle 12
    /// (VICE viciisc/vicii-fetch.c:194-196 with vicii-cycle.c:580-591). The managed
    /// map hardcodes the first three slots as garbage (Mos6569.cs:1724 slot &lt; 3),
    /// so regular bad lines wrongly latch $FF into vbuf slots 0-2.
    /// Acceptance: on bad line $30 with BA low from cycle 11, matrix slot 0 latches
    /// REAL data (matrix byte (40) xor $5C from screen $0400 + vc), not the $FF
    /// prefetch seed, by the time RasterX 17 has been processed.
    /// SLICE V1 STOP: remediation conflicts with the FAITHFUL lock
    /// TEST-VIC-FETCH-07, whose scenario asserts $FF prefetch seeds in slots 0-2 of
    /// a regular bad line (the divergent slot &lt; 3 gating). Kept pending until the
    /// lock is re-based on a VICE-valid mid-line bad-line scenario by the parity
    /// plan owner.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-06", ParityTag.Divergent, pending: true)]
    public void PrefetchGarbageGatedByPrefetchCounterNotFirstThreeSlots()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10);
        vic.Write(MemoryPointers, 0x14);
        for (var k = 0; k < 0x400; k++)
            memory.Span[0x0400 + k] = (byte)(k ^ 0x5C);

        AdvanceTo(vic, 0x30, 12);
        var entry = ReadInternals(vic);
        Assert.Equal(40, entry.VcBase);

        AdvanceTo(vic, 0x30, 17);
        Assert.True(vic.TryReadVideoMatrixLatch(0, out var matrixByte, out _));
        Assert.Equal((byte)((entry.VcBase & 0x3FF) ^ 0x5C), matrixByte); // Real, not $FF.
    }

    /// <summary>
    /// FR-VIC-FETCH AC-08.
    /// Use case: the g-access video mode is taken from the delayed $D011 copy: on the
    /// 6569 (color latency) the BMM bit is OR-ed in from reg11_delay so a BMM 1-to-0
    /// write only takes effect one fetch later (VICE viciisc/vicii-fetch.c:239-241,
    /// 261; reg11_delay latched at the end of every cycle, vicii-cycle.c:608). The
    /// managed g-fetch uses the live $D011 (Mos6569.cs:1665,1677), so the write
    /// retargets the very next fetch.
    /// Acceptance: in bitmap mode on bad line $30 (rc 0, vc 45 at slot 5, bitmap and
    /// char bases both in RAM bank 0), writing $D011 BMM off between cycles 19 and 20
    /// still fetches the bitmap address $0168 (seeded $AA) at cycle 20, and the text
    /// address $0100 (seeded $55) only at cycle 21.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-08", ParityTag.Divergent, pending: false)]
    public void GFetchModeUsesDelayedReg11()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x30);  // DEN=1, BMM=1, YSCROLL=0.
        vic.Write(MemoryPointers, 0x10);  // Screen $0400; char base $0000; bitmap $0000.
        for (var k = 0; k < 0x400; k++)
            memory.Span[0x0400 + k] = 0x20; // Every char code $20 -> text addr $0100.
        memory.Span[0x0100] = 0x55;         // Text-mode fetch target.
        memory.Span[0x0168] = 0xAA;         // Bitmap fetch target (vc 45 << 3 | rc 0).

        AdvanceTo(vic, 0x30, 19);
        vic.Write(ScreenControl1, 0x10); // BMM off between cycles.

        vic.Tick(); // Cycle 20: mode = live reg OR delayed BMM -> still bitmap.
        Assert.Equal(20, vic.RasterX);
        Assert.Equal(0xAA, vic.LastReadPhi1);

        vic.Tick(); // Cycle 21: delay caught up -> text fetch.
        Assert.Equal(21, vic.RasterX);
        Assert.Equal(0x55, vic.LastReadPhi1);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-09.
    /// Use case: 6569 fetch magic: when the mode change flips the g-access from a RAM
    /// address to a char-ROM address, the low byte is latched from the old-mode
    /// address and the upper bits from the new one: addr = (addr_from &amp; $FF) |
    /// (addr_to &amp; $3F00) (VICE viciisc/vicii-fetch.c:239-259). The managed VIC has
    /// no such latch and fetches the plain new-mode address.
    /// Acceptance: in bitmap mode ($0000 RAM) with char base $1000 (char ROM) and
    /// screen filled with char $01, a BMM 1-to-0 write between cycles 19 and 20 of
    /// line $33 (rc 3, vc 45) makes cycle 20 fetch char ROM at ($016B &amp; $FF) |
    /// ($100B &amp; $3F00) = $106B, i.e. the ROM byte at offset $6B, not the plain
    /// text byte at offset $0B.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-09", ParityTag.Divergent, pending: false)]
    public void GFetchRamToCharRomTransitionLatchesMixedAddress()
    {
        var (vic, memory) = CreatePalC64();
        var charRom = MachineTestFactory.LoadC64Rom("characters");
        byte mixedByte = charRom.Span[0x06B]; // (from &amp; $FF)=$6B | (to &amp; $3F00)=$1000.
        byte plainByte = charRom.Span[0x00B]; // Live-mode text address $100B.
        Assert.NotEqual(mixedByte, plainByte); // Test integrity: bytes must differ.

        vic.Write(ScreenControl1, 0x30);  // DEN=1, BMM=1, YSCROLL=0.
        vic.Write(MemoryPointers, 0x14);  // Screen $0400; char base $1000 (char ROM).
        for (var k = 0; k < 0x400; k++)
            memory.Span[0x0400 + k] = 0x01; // Char code $01 everywhere.

        AdvanceTo(vic, 0x33, 19); // rc == 3 on the fourth row line of the band.
        Assert.Equal(3, vic.CurrentRowCounter);
        vic.Write(ScreenControl1, 0x10); // BMM 1 -> 0: RAM -> char ROM transition.

        vic.Tick(); // Cycle 20: 6569 latch composes the mixed address.
        Assert.Equal(20, vic.RasterX);
        Assert.Equal(mixedByte, vic.LastReadPhi1);
    }

    /// <summary>
    /// FR-VIC-FETCH AC-14.
    /// Use case: sprite s-accesses are per-cycle bus fetches from
    /// (sprite pointer &lt;&lt; 6) + mc with mc incremented and masked to 6 bits on
    /// every access (VICE viciisc/vicii-fetch.c:110-154,282-299); the second s-access
    /// (dma1) is the Phi1 fetch of the cycle after the pointer fetch. The managed VIC
    /// reads sprite data at render time only (Mos6569.cs:2196-2201) and returns the
    /// $3FFF idle byte on those bus cycles.
    /// Acceptance: with sprite 0 enabled at Y=$60, pointer $80 at $07F8 and data
    /// seeded $A0+k at $2000, the Phi1 fetch at cycle 58 of line $60 returns exactly
    /// $A1 (mc 1 after the dma0 access) and at cycle 58 of line $61 exactly $A4
    /// (mc continued 3 per line).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-FETCH-14", ParityTag.Divergent, pending: false)]
    public void SpriteSDataFetchedPerCycleFromPointerTimes64PlusMc()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(MemoryPointers, 0x14);   // Screen base $0400 -> pointers at $07F8.
        vic.Write(SpriteEnable, 0x01);     // Sprite 0 enabled.
        vic.Write(SpriteY0, 0x60);         // Sprite 0 Y = line $60.
        memory.Span[0x07F8] = 0x80;        // Pointer $80 -> data at $2000.
        for (var k = 0; k < 9; k++)
            memory.Span[0x2000 + k] = (byte)(0xA0 + k);

        AdvanceTo(vic, 0x60, 58); // dma1 s-access: (ptr &lt;&lt; 6) + mc, mc == 1.
        Assert.Equal(0xA1, vic.LastReadPhi1);

        AdvanceTo(vic, 0x61, 58); // Next line: mc continued at 4.
        Assert.Equal(0xA4, vic.LastReadPhi1);
    }

    // ----------------------------------------------------------------
    // FR-VIC-MATRIX-ADDR: matrix/graphics addressing from VC
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-02.
    /// Use case: matrix cells are addressed from the video counter: the c-access
    /// reads v_fetch_addr(vc) (VICE viciisc/vicii-fetch.c:158-161,198), so the row
    /// content follows vc/vcbase, not screen geometry. The managed renderer capture
    /// computes the cell index geometrically from the raster line
    /// (Mos6569.cs:829-849,1224-1243), losing the deterministic boot offset
    /// (vcbase 40) that VICE displays on the first frame.
    /// Acceptance: with DEN, RSEL, YSCROLL=3 and CSEL=1, the row captured on bad line
    /// $33 (vc = vcbase = 40) holds the screen byte at index 40 in column 0, not the
    /// geometric index 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-02", ParityTag.Divergent, pending: false)]
    public void RenderMatrixRowContentComesFromVcNotGeometry()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x1B); // DEN=1, RSEL=1, YSCROLL=3 -> bad line $33.
        vic.Write(ScreenControl2, 0x08); // CSEL=1 (40 columns).
        vic.Write(MemoryPointers, 0x14); // Screen base $0400.
        // The renderer capture reads through the VIC render bank (bank 3 at
        // power-on), so VIC address $0400 resolves to CPU RAM $C400.
        for (var k = 0; k < 0x400; k++)
            memory.Span[0xC400 + k] = (byte)k;

        AdvanceTo(vic, 0x33, 13); // Bad-line capture point (VC update).
        Assert.Equal(40, vic.CurrentVideoMatrixCounter); // vc reloaded from vcbase 40.

        Assert.True(vic.TryReadRenderMatrixCell(0, 0, out var screenCode, out _));
        Assert.Equal(40, screenCode); // VICE: cell = screen[vc + col] = screen[40].
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-03.
    /// Use case: VC advances by 40 per display row regardless of CSEL: all 40
    /// c/g-accesses run even in 38-column mode (VICE viciisc/vicii-fetch.c:267-270;
    /// the cycle table always schedules 40 FetchC/FetchG slots). The managed renderer
    /// capture strides by Csel ? 40 : 38 (Mos6569.cs:1233,1237), shearing every row
    /// after the first and never filling columns 38-39.
    /// Acceptance: with CSEL=0, the row captured on bad line $3B (second row,
    /// vc = vcbase = 80) holds screen byte 80 in column 0 and screen byte 119 in
    /// column 39.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-03", ParityTag.Divergent, pending: false)]
    public void RenderMatrixRowStrideIsFortyRegardlessOfCsel()
    {
        var (vic, memory) = CreatePalC64();
        vic.Write(ScreenControl1, 0x1B); // DEN=1, RSEL=1, YSCROLL=3 -> rows $33, $3B...
        vic.Write(ScreenControl2, 0x00); // CSEL=0 (38 columns).
        vic.Write(MemoryPointers, 0x14); // Screen base $0400.
        // The renderer capture reads through the VIC render bank (bank 3 at
        // power-on), so VIC address $0400 resolves to CPU RAM $C400.
        for (var k = 0; k < 0x400; k++)
            memory.Span[0xC400 + k] = (byte)k;

        AdvanceTo(vic, 0x3B, 13); // Second bad line: vc = vcbase = 80.
        Assert.Equal(80, vic.CurrentVideoMatrixCounter);

        Assert.True(vic.TryReadRenderMatrixCell(1, 0, out var col0, out _));
        Assert.Equal(80, col0);   // 40-stride from VC, not 1 * 38 = 38.
        Assert.True(vic.TryReadRenderMatrixCell(1, 39, out var col39, out _));
        Assert.Equal(119, col39); // All 40 columns fetched even under CSEL=0.
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-08.
    /// Use case: g_fetch_addr takes its mode from the delayed $D011 copy: on the 6569
    /// the BMM bit is OR-ed from reg11_delay (VICE viciisc/vicii-fetch.c:240,261), so
    /// a BMM 1-to-0 write keeps the bitmap addressing for exactly one more fetch. The
    /// managed ConsumeGraphicsFetchAddress reads the live register
    /// (Mos6569.cs:1665,1677).
    /// Acceptance: with vc=5, rc=0, vbuf[5]=$23 and $D018=0, setting BMM then ticking
    /// once (delay latches) then clearing BMM yields a bitmap-mode fetch address of
    /// exactly $0028 ((5 &lt;&lt; 3) | 0), not the text address $0118.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-08", ParityTag.Divergent, pending: false)]
    public void GraphicsFetchAddressModeFromDelayedReg11()
    {
        var vic = BuildVic();
        ConsumeGraphicsFetches(vic, 5); // vc = vmli = 5.
        vic.LatchVideoMatrixFetch(5, 0x23, 0x01);

        vic.Write(ScreenControl1, 0x20); // BMM on.
        vic.Tick();                      // reg11_delay latches BMM (end of cycle).
        vic.Write(ScreenControl1, 0x00); // BMM off: live register only.

        var address = vic.ConsumeGraphicsFetchAddress();
        Assert.Equal((ushort)0x0028, address); // Delayed BMM still addresses bitmap.
    }

    /// <summary>
    /// FR-VIC-MATRIX-ADDR AC-09.
    /// Use case: because vc/vcbase reset at every frame start (VICE
    /// viciisc/vicii-cycle.c:208-209), the hot-path matrix address of the first bad
    /// line is v_fetch_addr(0) on every frame. The managed VIC never frame-resets VC,
    /// so from frame 2 the first row fetches drift to the leaked end-of-frame value
    /// (16), masked on screen only by the geometric renderer.
    /// Acceptance: at the VC-update cycle of the first bad line ($30) of frame 2 the
    /// video matrix counter is exactly 0 and the matrix fetch address is exactly the
    /// screen base $0400.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-MATRIX-ADDR-09", ParityTag.Divergent, pending: false)]
    public void HotPathMatrixAddressDoesNotDriftAcrossFrames()
    {
        var (vic, _) = CreatePalC64();
        vic.Write(ScreenControl1, 0x10); // DEN=1, YSCROLL=0.
        vic.Write(MemoryPointers, 0x14); // Screen base $0400.

        AdvanceTo(vic, (ushort)(vic.TotalLines - 1), 0);
        AdvanceTo(vic, 0x30, 13); // First bad line of frame 2, VC just reloaded.

        Assert.Equal(0, vic.CurrentVideoMatrixCounter);
        Assert.Equal((ushort)0x0400, vic.CurrentVideoMatrixFetchAddress);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds a bare PAL VIC-II (no memory map) reset to the canonical power-on
    /// phase: line 0, RasterX 6, pipeline counters zero, idle and allow_bad_lines
    /// false.
    /// </summary>
    private static Mos6569 BuildVic()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>
    /// Builds a full managed C64 (real C64MemoryMap wired as the VIC's Phi1 reader)
    /// and returns the VIC plus the memory map, freshly reset so vic.Tick() drives
    /// the product fetch dispatch deterministically.
    /// </summary>
    private static (Mos6569 Vic, IMemory Memory) CreatePalC64()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        var memory = (IMemory)machine.Devices.GetByRole(DeviceRole.SystemRam)!;
        Assert.Equal(Mos6569.PalCyclesPerLine, vic.CyclesPerLine);
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
    /// the public IStatefulDevice capture surface (Mos6569.State.cs).
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
