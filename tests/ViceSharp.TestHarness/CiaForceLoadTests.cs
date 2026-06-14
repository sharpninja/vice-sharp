namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CIA-TIMER (BACKFILL-CIA force-load).
/// Use case: MOS 6526 CIA control register bit 4 (FORCE_LOAD) is the
/// "strobe" that demos use to retrigger timer countdown without
/// disturbing the rest of the control register. Writing CRA bit 4 = 1
/// reloads Timer A counter from its latch immediately; writing CRB
/// bit 4 = 1 does the same for Timer B. Bit 4 is one-shot: the next
/// read of CRA / CRB shows bit 4 = 0 (auto-cleared after the load
/// fires). Force-load works regardless of whether the timer is
/// currently running or stopped, and does not disturb other CRA / CRB
/// bits (run, TOD source, output mode, etc.).
/// Acceptance: each test below pins one facet of FORCE_LOAD: immediate
/// reload, auto-clear of bit 4, stopped-timer load, Timer B path, and
/// preservation of unrelated CRA / CRB bits.
/// </summary>
public sealed class CiaForceLoadTests
{
    private static (Mos6526 cia, InterruptLine irq) BuildCiaWithIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
        return (cia, irq);
    }

    private static ushort ReadTimerACounter(Mos6526 cia)
    {
        var low = cia.Read(0xDC04);
        var high = cia.Read(0xDC05);
        return (ushort)((high << 8) | low);
    }

    private static ushort ReadTimerBCounter(Mos6526 cia)
    {
        var low = cia.Read(0xDC06);
        var high = cia.Read(0xDC07);
        return (ushort)((high << 8) | low);
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA force-load).
    /// Use case: A demo programs Timer A latch and starts the timer; after
    /// the counter has decremented a few cycles, software writes CRA with
    /// bit 4 = 1 to retrigger the countdown without changing other CRA
    /// bits. The counter must reload from the latch immediately so the
    /// next tick observes the loaded-minus-one value.
    /// Acceptance: latch = 16; after 5 ticks of decrement, write CRA = 0x11
    /// (start + force-load). Counter reloads to 16 before any subsequent
    /// tick; one tick later the counter reads 15.
    /// </summary>
    [Fact]
    public void CraBit4_ReloadsTimerAFromLatchImmediately()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 16 (0x0010).
        cia.Write(0xDC04, 0x10);
        cia.Write(0xDC05, 0x00);

        // Start Timer A. CRA = 0x01: start only, no force-load.
        cia.Write(0xDC0E, 0x01);

        // Let the timer count down a few cycles.
        for (var i = 0; i < 5; i++)
            cia.Tick();

        // Force-load: CRA = 0x11 (start + force-load bit 4).
        cia.Write(0xDC0E, 0x11);

        ReadTimerACounter(cia).Should().Be((ushort)16,
            "force-load reloads counter from latch immediately on the CRA write");

        cia.Tick();

        ReadTimerACounter(cia).Should().Be((ushort)15,
            "after one tick post-load, counter has decremented by one");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA force-load).
    /// Use case: On real 6526 hardware, the FORCE_LOAD bit is a one-shot
    /// strobe: the chip clears bit 4 internally after the reload fires so
    /// subsequent reads of CRA never observe the strobe bit set. This lets
    /// software use read-modify-write on CRA without accidentally
    /// re-strobing on every write.
    /// Acceptance: after writing CRA = 0x11, reading CRA back returns a
    /// value with bit 4 = 0; the other written bits (bit 0 = start) remain
    /// intact.
    /// </summary>
    [Fact]
    public void CraBit4_AutoClearsAfterForceLoadFires()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 16.
        cia.Write(0xDC04, 0x10);
        cia.Write(0xDC05, 0x00);

        // CRA = 0x11: start + force-load.
        cia.Write(0xDC0E, 0x11);

        var cra = cia.Read(0xDC0E);
        (cra & 0x10).Should().Be(0x00,
            "force-load bit 4 is one-shot and must auto-clear after the load fires");
        (cra & 0x01).Should().Be(0x01,
            "start bit (bit 0) must remain set after force-load auto-clears");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA force-load).
    /// Use case: Force-load must work even when the timer is stopped. Real
    /// hardware reloads the counter from the latch in response to bit 4
    /// regardless of bit 0 (start). Demos sometimes pre-load a counter
    /// without starting it.
    /// Acceptance: Timer A is not started (bit 0 = 0); latch = $1234.
    /// Writing CRA = 0x10 (force-load only) reloads counter to $1234
    /// without starting the timer.
    /// </summary>
    [Fact]
    public void CraBit4_ReloadsStoppedTimerAFromLatch()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 0x1234.
        cia.Write(0xDC04, 0x34);
        cia.Write(0xDC05, 0x12);

        // CRA = 0x10: force-load only, NOT started.
        cia.Write(0xDC0E, 0x10);

        ReadTimerACounter(cia).Should().Be((ushort)0x1234,
            "force-load reloads stopped Timer A counter from latch");

        // Tick a few cycles; counter must NOT decrement (timer stopped).
        for (var i = 0; i < 4; i++)
            cia.Tick();

        ReadTimerACounter(cia).Should().Be((ushort)0x1234,
            "stopped timer must not decrement after force-load");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA force-load).
    /// Use case: The same FORCE_LOAD semantics apply to CRB bit 4 for
    /// Timer B: writing 1 reloads Timer B counter from the Timer B latch
    /// ($DC06 / $DC07) immediately.
    /// Acceptance: Timer B latch = 16; after 5 ticks of decrement,
    /// writing CRB = 0x11 reloads counter to 16 and a subsequent tick
    /// drops it to 15.
    /// </summary>
    [Fact]
    public void CrbBit4_ReloadsTimerBFromLatchImmediately()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer B latch = 16 (0x0010).
        cia.Write(0xDC06, 0x10);
        cia.Write(0xDC07, 0x00);

        // CRB = 0x01: start only.
        cia.Write(0xDC0F, 0x01);

        // Let Timer B count down (CRB INMODE=00 -> phi2).
        for (var i = 0; i < 5; i++)
            cia.Tick();

        // Force-load: CRB = 0x11.
        cia.Write(0xDC0F, 0x11);

        ReadTimerBCounter(cia).Should().Be((ushort)16,
            "force-load reloads Timer B counter from latch immediately on the CRB write");

        cia.Tick();

        ReadTimerBCounter(cia).Should().Be((ushort)15,
            "after one tick post-load, Timer B counter has decremented by one");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA force-load).
    /// Use case: FORCE_LOAD is a strobe, not a control: writing bit 4
    /// alongside other CRA bits (run, TOD source, etc.) must leave those
    /// other bits intact while only bit 4 auto-clears. Demos use
    /// read-modify-write to add force-load without disturbing the rest of
    /// CRA.
    /// Acceptance: with CRA = 0x81 (start + 50Hz TOD source), writing
    /// CRA = 0x91 reloads the counter and a subsequent read of CRA shows
    /// 0x81 (bit 4 cleared, bits 0 and 7 preserved).
    /// </summary>
    [Fact]
    public void CraBit4_DoesNotDisturbOtherCraBits()
    {
        var (cia, _) = BuildCiaWithIrq();

        // Timer A latch = 16.
        cia.Write(0xDC04, 0x10);
        cia.Write(0xDC05, 0x00);

        // CRA = 0x81: start (bit 0) + TOD source = 50Hz (bit 7).
        cia.Write(0xDC0E, 0x81);

        // Tick a few cycles to let the counter drift below latch.
        for (var i = 0; i < 3; i++)
            cia.Tick();

        // Add force-load by writing CRA = 0x91 (same flags + bit 4).
        cia.Write(0xDC0E, 0x91);

        ReadTimerACounter(cia).Should().Be((ushort)16,
            "force-load reloads the counter regardless of other CRA bits");

        var cra = cia.Read(0xDC0E);
        cra.Should().Be((byte)0x81,
            "after force-load auto-clears, CRA preserves bit 0 (run) and bit 7 (50Hz TOD)");
    }
}
