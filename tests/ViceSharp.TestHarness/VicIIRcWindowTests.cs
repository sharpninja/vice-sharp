namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR: FR-VIC-001 / FR-VIC-006 / FR-VIC-008, TR: TR-VIC-EDGE-003 / TR-CYCLE-001.
/// Use case: The VIC-II row counter (RC) and video counter (VC) state machine
/// must match the cycle-accurate behavior documented in VICE
/// viciisc/vicii-cycle.c lines 541-563.
/// - VC update at cycle 14 (managed: RasterX == 13): vc = vcbase; vmli = 0;
///   if bad_line: rc = 0, idle_state = false.
/// - RC update at cycle 58 (managed: RasterX == 57): if rc == 7 then
///   idle_state = 1 and vcbase = vc; if !idle_state || bad_line then
///   rc = (rc + 1) and 7 and idle_state = 0.
/// After eight consecutive display rows the chip enters idle state (rc
/// rolls to 7 and stays there). A bad line at the start of the next row
/// resets rc to 0 and clears idle state.
/// VICE source: viciisc/vicii-cycle.c:541-563 (cycle 14 VC update),
/// viciisc/vicii-cycle.c:551-563 (cycle 58 RC update).
/// </summary>
public sealed class VicIIRcWindowTests
{
    private const ushort ScreenControl1 = 0xD011;
    private const byte DenRselYscroll0 = 0x18; // DEN=1, RSEL=1, YSCROLL=0

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var maxCycles = vic.TotalLines * vic.CyclesPerLine * 2;
        for (var i = 0; i < maxCycles; i++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;
            vic.Tick();
        }
        throw new InvalidOperationException(
            $"VIC did not reach line ${rasterLine:X3}, cycle {rasterCycle}.");
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-003.
    /// Use case: The VIC-II row counter resets to 0 on the first bad line
    /// in the DMA range when a forced bad line condition is met.
    /// Acceptance: At cycle 14 (RasterX 13) of raster line $30 with
    /// YSCROLL=0 and DEN=1, CurrentRowCounter is 0 (the VC update resets
    /// it because bad_line is active).
    /// VICE viciisc/vicii-cycle.c:546-548 (if bad_line then rc = 0).
    /// </summary>
    [Fact]
    public void CurrentRowCounter_IsZeroAfterFirstBadLine_VcUpdate()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenRselYscroll0);

        // Advance to cycle 13 of line $30 (first DMA line, bad line with YSCROLL=0).
        // The VC update fires at RasterX=13 and resets rc to 0 because bad_line=true.
        AdvanceTo(vic, 0x30, 13);

        Assert.Equal(0, vic.CurrentRowCounter);
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-003.
    /// Use case: The RC update increments the row counter at cycle 58 (RasterX 57)
    /// of each non-bad display line. The RC update fires DURING the tick that
    /// brings RasterX to 57, so AdvanceTo(line, 57) returns with the updated value.
    /// After line $30 (bad, rc resets to 0 at VC then increments to 1 at RC),
    /// six consecutive non-bad lines (0x31-0x36) produce rc 2..7 at their RC points.
    /// Acceptance: At cycle 57 of $36, CurrentRowCounter == 7 (rc went 6->7 in
    /// that tick; idle_state is still false because rc was 6 when checked).
    /// VICE viciisc/vicii-cycle.c:560-562 (rc increment when !idle || bad).
    /// </summary>
    [Fact]
    public void CurrentRowCounter_IsSevenAtCycle57OfSixthNonBadLine()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenRselYscroll0);

        // Line $30: bad. VC update: rc=0. RC update: rc was 0 (!7), !idle=true -> rc=1.
        // Lines $31-$35: not bad. RC updates: rc 2, 3, 4, 5, 6.
        // Line $36: not bad. RC update: rc was 6 (!7), !idle=true -> rc=7.
        AdvanceTo(vic, 0x36, 57);

        Assert.Equal(7, vic.CurrentRowCounter);
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-003.
    /// Use case: When rc is already 7 going into the RC update, the VIC-II
    /// enters idle state. idle_state must be true at cycle 57 of the SECOND
    /// line that sees rc==7 (line $37), because $36's RC update raised rc
    /// from 6 to 7 without setting idle.
    /// Acceptance: After advancing to cycle 57 of line $37 with YSCROLL=0,
    /// IsGraphicsIdle returns true and CurrentRowCounter stays 7 (not incremented
    /// when idle and not bad_line).
    /// VICE viciisc/vicii-cycle.c:556-558 (if rc == 7: idle_state = 1).
    /// </summary>
    [Fact]
    public void IsGraphicsIdle_TrueAtCycle57WhenExistingRcIsAlreadySeven()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenRselYscroll0);

        // At cycle 57 of $36: rc goes 6->7 but idle NOT set (rc was 6 at the check).
        // At cycle 57 of $37: rc==7 at check -> idle_state=1. !idle||bad=false -> rc stays 7.
        AdvanceTo(vic, 0x37, 57);

        Assert.True(vic.IsGraphicsIdle);
        Assert.Equal(7, vic.CurrentRowCounter);
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-003.
    /// Use case: When the chip is in idle state (rc==7, idle_state=1), the
    /// next bad line must reset rc to 0 and clear idle_state via the VC
    /// update at cycle 14 of the bad line.
    /// Acceptance: After the idle transition at cycle 57 of $37, advancing
    /// to cycle 13 of $38 (the next bad line with YSCROLL=0) sets
    /// CurrentRowCounter to 0 and IsGraphicsIdle to false.
    /// VICE viciisc/vicii-cycle.c:546-548 (rc = 0 on bad line at VC update).
    /// </summary>
    [Fact]
    public void CurrentRowCounter_ResetsToZeroOnNextBadLine()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenRselYscroll0);

        // Advance past the idle transition at $37 cycle 57.
        AdvanceTo(vic, 0x37, 57);
        Assert.True(vic.IsGraphicsIdle);

        // Line $38 is a bad line (0x38 & 7 = 0 = YSCROLL). VC update at cycle 13
        // resets rc=0 and clears idle_state.
        AdvanceTo(vic, 0x38, 13);

        Assert.Equal(0, vic.CurrentRowCounter);
        Assert.False(vic.IsGraphicsIdle);
    }

    /// <summary>
    /// FR-VIC-001 / TR-VIC-EDGE-003.
    /// Use case: The row counter must increment continuously over multiple
    /// 8-row groups, proving the RC window repeats correctly across rows.
    /// Note: AdvanceTo returns AFTER the RC update fires at RasterX=57, so
    /// the value observed is the POST-update value (one higher than pre-update).
    /// Acceptance: Starting from line $30 (bad, rc=0 VC / rc=1 RC), the row
    /// counter at cycle 57 of $31 is 2 (1->2), at $32 is 3, ..., at $36 is 7 (6->7).
    /// VICE viciisc/vicii-cycle.c:560-562.
    /// </summary>
    [Theory]
    [InlineData(0x31, 2)] // rc was 1 pre-update, becomes 2
    [InlineData(0x32, 3)]
    [InlineData(0x33, 4)]
    [InlineData(0x34, 5)]
    [InlineData(0x35, 6)]
    [InlineData(0x36, 7)] // rc was 6 pre-update, becomes 7 (idle NOT set: rc was 6 at check)
    public void CurrentRowCounter_MatchesExpectedValueAtRcUpdateCycles(ushort line, int expectedRc)
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenRselYscroll0);

        AdvanceTo(vic, line, 57);

        Assert.Equal(expectedRc, vic.CurrentRowCounter);
    }

    /// <summary>
    /// FR-VIC-001 / FR-VIC-008 / TR-VIC-EDGE-003.
    /// Use case: On an FLI line (forced bad line), changing YSCROLL mid-way
    /// to match the current raster line's low 3 bits causes a new bad line
    /// condition. The VC update at cycle 14 of that line resets rc to 0 and
    /// clears idle_state, interrupting the idle window.
    /// Acceptance: After reaching idle state at $37 cycle 57 (rc==7,
    /// idle_state=1), writing YSCROLL=1 (to match $39 & 7 = 1) before line
    /// $39 makes $39 a forced bad line. Its VC update at cycle 13 resets
    /// rc=0 and clears idle_state. Lines $38 (YSCROLL=1: 0x38&7=0!=1) are
    /// not bad, so the idle window survives line $38 and is only interrupted
    /// at $39.
    /// VICE viciisc/vicii-cycle.c:51-60 (check_badline),
    /// viciisc/vicii-cycle.c:546-548 (rc = 0 on bad_line VC update).
    /// </summary>
    [Fact]
    public void ForcedBadLine_ResetsRcAndClearsIdleState()
    {
        var vic = BuildVic();
        vic.Write(ScreenControl1, DenRselYscroll0); // YSCROLL=0

        // Reach idle state at cycle 57 of $37.
        AdvanceTo(vic, 0x37, 57);
        Assert.True(vic.IsGraphicsIdle);

        // Change YSCROLL to 1 so line $39 ($39 & 7 = 1) becomes a forced bad line.
        // Line $38 ($38 & 7 = 0 != 1) is NOT a bad line with YSCROLL=1,
        // so idle survives through $38.
        vic.Write(ScreenControl1, 0x19); // DEN=1, RSEL=1, YSCROLL=1

        // VC update at cycle 13 of $39: bad_line=1 -> rc=0, idle_state=false.
        AdvanceTo(vic, 0x39, 13);

        Assert.Equal(0, vic.CurrentRowCounter);
        Assert.False(vic.IsGraphicsIdle);
    }
}
