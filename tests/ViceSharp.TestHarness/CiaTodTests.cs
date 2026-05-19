namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CIA-TOD (BACKFILL-CIA).
/// Use case: MOS 6526 CIA Time-Of-Day clock at $D008-$D00B (CIA1) /
/// $DD08-$DD0B (CIA2). Behaviour verified: 50/60Hz cycle-driven tick from
/// CRA bit 7, HOUR-read latch + TENTHS-read unlatch, AM/PM bit 7 in HOUR,
/// CRB bit 7 routes writes to ALARM, alarm match ORs bit 2 into ICR.
/// Note: native VICE shim does not export TOD state in
/// <c>vice_cia_state</c>, so these tests verify ViceSharp behaviour
/// against the 6526 spec rather than VICE parity.
/// </summary>
public sealed class CiaTodTests
{
    private const int PalClockHz = 985_248;
    private const int Pal50HzCyclesPerTick = PalClockHz / 50;
    private const int Pal60HzCyclesPerTick = PalClockHz / 60;

    private static Mos6526 BuildCia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6526(bus, irq);
    }

    private static (Mos6526 cia, InterruptLine irq) BuildCiaWithIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);
        return (cia, irq);
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA).
    /// Use case: CIA must clock its TOD from the configured 50/60Hz source
    /// (CRA bit 7). After ticking enough host phi2 cycles to span at least
    /// one TOD tenths-of-second period at 50Hz, the TENTHS register must
    /// have advanced from 0.
    /// Acceptance: With CRA bit 7 = 1 (50Hz), after PAL/50 cycles the
    /// TENTHS register reads non-zero.
    /// </summary>
    [Fact]
    public void Tod_TicksAtConfiguredRate_50Hz()
    {
        var cia = BuildCia();

        // CRA bit 7 = 1 selects 50Hz source.
        cia.Write(0xDC0E, 0x80);

        for (var i = 0; i < Pal50HzCyclesPerTick + 4; i++)
            cia.Tick();

        cia.Read(0xDC08).Should().NotBe((byte)0x00, "TOD tenths should have advanced after one 50Hz period");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA).
    /// Use case: Reading the HOUR register latches the current TOD into a
    /// hidden buffer; subsequent reads of MIN/SEC/TENTHS return the latched
    /// value, not the live counter. Reading TENTHS releases the latch and
    /// future reads return live values again.
    /// Acceptance: After latching at 12:34:56.7, manually clocking TOD via
    /// <see cref="Mos6526.ClockTod"/> does not change MIN/SEC reads (still
    /// 34/56) until TENTHS is read and a new TOD tick lands; then MIN
    /// reflects the advanced state.
    /// </summary>
    [Fact]
    public void Tod_HourReadLatches_TenthsReadUnlatches()
    {
        var cia = BuildCia();

        // Stop TOD writing to alarm by clearing CRB bit 7 (default).
        cia.Write(0xDC0F, 0x00);

        // Set TOD to 12:34:56.7.
        cia.Write(0xDC0B, 0x12); // HOUR = 12 (AM)
        cia.Write(0xDC0A, 0x34); // MIN = 34
        cia.Write(0xDC09, 0x56); // SEC = 56
        cia.Write(0xDC08, 0x07); // TENTHS = 7

        // Read HOUR -> latches the current TOD as 12:34:56.7.
        var latchedHour = cia.Read(0xDC0B);
        latchedHour.Should().Be(0x12);

        // Advance live TOD past the latch (3 tenths -> 12:34:57.0).
        cia.ClockTod();
        cia.ClockTod();
        cia.ClockTod();

        // While latched, MIN/SEC return the latched values, NOT the live
        // values that have just advanced past 56.
        cia.Read(0xDC0A).Should().Be(0x34, "MIN should be latched at 0x34");
        cia.Read(0xDC09).Should().Be(0x56, "SEC should be latched at 0x56");

        // Reading TENTHS returns latched 7 and unlatches.
        cia.Read(0xDC08).Should().Be(0x07, "TENTHS read returns latched value and then unlatches");

        // Now reads of MIN/SEC return live state, which is 12:34:57.0.
        cia.Read(0xDC09).Should().Be(0x57, "after unlatch, SEC reads live (advanced past 56)");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA).
    /// Use case: Bit 7 of the HOUR register encodes AM/PM (1 = PM); the
    /// CIA must preserve this bit across writes and reads of HOUR.
    /// Acceptance: Writing 0x92 (12 PM BCD) reads back 0x92; writing 0x12
    /// (12 AM BCD) reads back 0x12.
    /// </summary>
    [Fact]
    public void Tod_HourAmPmBit_IsPreservedOnWriteAndRead()
    {
        var cia = BuildCia();
        cia.Write(0xDC0F, 0x00); // CRB bit 7 = 0 -> write goes to TOD

        // Write 12 PM (BCD 12 with AM/PM bit set).
        cia.Write(0xDC0B, 0x92);
        // Reads of MIN/SEC/TENTHS unlatch after TENTHS; read TENTHS to
        // ensure latch is clear before next assertion.
        cia.Read(0xDC0B).Should().Be(0x92, "12 PM = $92");
        cia.Read(0xDC08); // unlatch

        // Write 12 AM (BCD 12 with AM/PM bit clear).
        cia.Write(0xDC0B, 0x12);
        cia.Read(0xDC0B).Should().Be(0x12, "12 AM = $12");
        cia.Read(0xDC08); // unlatch
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA).
    /// Use case: With CRB bit 7 = 1, writes to $DC08-$DC0B target the
    /// ALARM latch rather than the live TOD; with CRB bit 7 = 0, writes
    /// target the live TOD.
    /// Acceptance: After loading TOD HOUR with 0x03 (CRB.7 = 0), then
    /// writing 0x05 to HOUR with CRB.7 = 1, the TOD HOUR remains 0x03 (the
    /// 0x05 went to the alarm). Clearing CRB.7 then writing 0x07 updates
    /// the live HOUR to 0x07.
    /// </summary>
    [Fact]
    public void Tod_CrbBit7_RoutesHourWritesToAlarmOrClock()
    {
        var cia = BuildCia();

        // CRB.7 = 0: write to live TOD.
        cia.Write(0xDC0F, 0x00);
        cia.Write(0xDC0B, 0x03);
        cia.Read(0xDC0B).Should().Be(0x03);
        cia.Read(0xDC08); // unlatch

        // CRB.7 = 1: write to alarm; live HOUR unchanged.
        cia.Write(0xDC0F, 0x80);
        cia.Write(0xDC0B, 0x05);
        cia.Read(0xDC0B).Should().Be(0x03, "alarm got the 0x05 write; live TOD HOUR still 0x03");
        cia.Read(0xDC08); // unlatch

        // Back to CRB.7 = 0: write to live TOD.
        cia.Write(0xDC0F, 0x00);
        cia.Write(0xDC0B, 0x07);
        cia.Read(0xDC0B).Should().Be(0x07);
        cia.Read(0xDC08); // unlatch
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA).
    /// Use case: When TOD matches ALARM exactly across all four BCD
    /// fields, the CIA must set bit 2 of the ICR latch. With the ICR mask
    /// bit 2 set, the IRQ line must also assert.
    /// Acceptance: Loading TOD = ALARM (12:00:00.0) sets ICR bit 2 on the
    /// next TOD tick (and bit 7 master IR flag with the mask enabled).
    /// </summary>
    [Fact]
    public void Tod_AlarmMatch_SetsIcrBit2()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Configure CRA bit 7 = 1 (50Hz).
        cia.Write(0xDC0E, 0x80);

        // CRB.7 = 1 -> writes target alarm. Set alarm = 12:00:00.1.
        cia.Write(0xDC0F, 0x80);
        cia.Write(0xDC0B, 0x12);
        cia.Write(0xDC0A, 0x00);
        cia.Write(0xDC09, 0x00);
        cia.Write(0xDC08, 0x01);

        // CRB.7 = 0 -> writes target TOD. Set TOD = 12:00:00.0.
        cia.Write(0xDC0F, 0x00);
        cia.Write(0xDC0B, 0x12);
        cia.Write(0xDC0A, 0x00);
        cia.Write(0xDC09, 0x00);
        cia.Write(0xDC08, 0x00);

        // Enable ICR mask bit 2 (alarm).
        cia.Write(0xDC0D, 0x84);

        // Tick one TOD period at 50Hz -> TOD becomes 12:00:00.1 which
        // matches the alarm.
        for (var i = 0; i < Pal50HzCyclesPerTick + 4; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeTrue("alarm match should assert the IRQ line");
        var icr = cia.Read(0xDC0D);
        (icr & 0x04).Should().Be(0x04, "ICR bit 2 (alarm) must be set");
        (icr & 0x80).Should().Be(0x80, "ICR master IR flag (bit 7) must be set when masked source fires");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA #45 followup, 12-hour mode).
    /// Use case: Real 6526 TOD HOUR is 12-hour BCD (01..12) plus AM/PM in
    /// bit 7. The 11 -> 12 rollover stays within the same half-day and
    /// must NOT toggle bit 7.
    /// Acceptance: With TOD = 11:59:59.9 AM (HOUR = 0x11), one ClockTod()
    /// advances HOUR to 0x12 (12 AM = noon-ish in 6526 convention) with
    /// bit 7 still clear.
    /// </summary>
    [Fact]
    public void Tod_HourRollover_11To12_DoesNotTogglePm()
    {
        var cia = BuildCia();
        cia.Write(0xDC0F, 0x00); // CRB.7 = 0 -> writes target live TOD

        cia.Write(0xDC0B, 0x11); // HOUR = 11 AM
        cia.Write(0xDC0A, 0x59); // MIN = 59
        cia.Write(0xDC09, 0x59); // SEC = 59
        cia.Write(0xDC08, 0x09); // TENTHS = 9

        cia.ClockTod();

        cia.Read(0xDC0B).Should().Be(0x12, "11 + 1 = 12 within same half-day; AM bit stays clear");
        cia.Read(0xDC08); // unlatch
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA #45 followup, 12-hour mode).
    /// Use case: The 12 -> 1 rollover toggles AM/PM bit 7 on real 6526
    /// hardware. Starting from 12:59:59.9 AM (HOUR = 0x12), one tenth
    /// advances HOUR to 1 PM (0x81).
    /// Acceptance: HOUR reads 0x81 (1 PM = 0x01 with bit 7 set).
    /// </summary>
    [Fact]
    public void Tod_HourRollover_12AmTo1Pm_TogglesAmPmBit()
    {
        var cia = BuildCia();
        cia.Write(0xDC0F, 0x00);

        cia.Write(0xDC0B, 0x12); // HOUR = 12 AM
        cia.Write(0xDC0A, 0x59);
        cia.Write(0xDC09, 0x59);
        cia.Write(0xDC08, 0x09);

        cia.ClockTod();

        cia.Read(0xDC0B).Should().Be(0x81, "12 AM rolls to 1 PM; bit 7 set, BCD = 0x01");
        cia.Read(0xDC08); // unlatch
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA #45 followup, 12-hour mode).
    /// Use case: The 12 -> 1 rollover toggles AM/PM in the opposite
    /// direction too. Starting from 12:59:59.9 PM (HOUR = 0x92), one
    /// tenth advances HOUR to 1 AM (0x01).
    /// Acceptance: HOUR reads 0x01 (1 AM = 0x01 with bit 7 clear).
    /// </summary>
    [Fact]
    public void Tod_HourRollover_12PmTo1Am_TogglesAmPmBit()
    {
        var cia = BuildCia();
        cia.Write(0xDC0F, 0x00);

        cia.Write(0xDC0B, 0x92); // HOUR = 12 PM
        cia.Write(0xDC0A, 0x59);
        cia.Write(0xDC09, 0x59);
        cia.Write(0xDC08, 0x09);

        cia.ClockTod();

        cia.Read(0xDC0B).Should().Be(0x01, "12 PM rolls to 1 AM; bit 7 clear, BCD = 0x01");
        cia.Read(0xDC08); // unlatch
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA #45 followup, 12-hour mode).
    /// Use case: The HOUR BCD ring is 1..12 only; the live HOUR low
    /// nibble + bit 4 must never read 0x00 or 0x13+ after a rollover.
    /// Walking 12 hours from 12 AM verifies the full ring.
    /// Acceptance: After 12 hour-rollovers starting at 11 AM, the BCD
    /// portion (mask 0x1F) walks 12, 1, 2, ..., 11 staying inside
    /// [0x01, 0x12].
    /// </summary>
    [Fact]
    public void Tod_HourRollover_StaysInOneToTwelveBcdRing()
    {
        var cia = BuildCia();
        cia.Write(0xDC0F, 0x00);

        // Start at 11:59:59.9 AM so the very next tenth ticks HOUR.
        cia.Write(0xDC0B, 0x11);
        cia.Write(0xDC0A, 0x59);
        cia.Write(0xDC09, 0x59);
        cia.Write(0xDC08, 0x09);

        byte[] expectedBcd = { 0x12, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11 };
        foreach (var expected in expectedBcd)
        {
            // One tenth ticks the hour because MIN:SEC.TENTHS = 59:59.9.
            cia.ClockTod();
            // Force the next iteration to land on another hour rollover.
            // Reading HOUR latches, so unlatch via TENTHS to keep live.
            var hour = cia.Read(0xDC0B);
            cia.Read(0xDC08);
            (hour & 0x1F).Should().Be(expected, $"BCD ring must stay 1..12; saw {hour:X2}");
            (hour & 0x1F).Should().BeInRange(0x01, 0x12, "BCD never 0x00 or 0x13+");

            // Reset MIN/SEC/TENTHS to 59:59.9 so the next tick hits another
            // hour rollover.
            cia.Write(0xDC0A, 0x59);
            cia.Write(0xDC09, 0x59);
            cia.Write(0xDC08, 0x09);
        }
    }

    /// <summary>
    /// FR/TR: FR-CIA-TOD (BACKFILL-CIA #45 followup, 12-hour mode).
    /// Use case: A write to HOUR must preserve BCD 1..12 in the low five
    /// bits and the AM/PM bit (bit 7); other bits stay as written so the
    /// CPU sees what it wrote.
    /// Acceptance: Writing 0x95 (5 PM) reads back 0x95 unchanged.
    /// </summary>
    [Fact]
    public void Tod_HourWrite_Preserves5PmBcdAndAmPmBit()
    {
        var cia = BuildCia();
        cia.Write(0xDC0F, 0x00);

        cia.Write(0xDC0B, 0x95); // 5 PM = BCD 0x05 + bit 7
        cia.Read(0xDC0B).Should().Be(0x95, "write/read preserves 5 PM = 0x95");
        cia.Read(0xDC08); // unlatch
    }
}
