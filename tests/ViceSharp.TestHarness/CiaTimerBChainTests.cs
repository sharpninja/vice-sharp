namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CIA-TIMER (BACKFILL-CIA).
/// Use case: MOS 6526 CIA Timer B input-source select (CRB bits 5-6).
/// Behaviour verified: mode 00 (phi2) baseline, mode 10 (Timer A
/// underflow chain) counting + IRQ latch, and mode 10 with Timer A
/// stopped (B must not advance). Modes 01 / 11 (CNT-gated) remain
/// deferred pending CNT pin plumbing and are not covered here.
/// </summary>
public sealed class CiaTimerBChainTests
{
    private static (Mos6526 cia, InterruptLine irq) BuildCiaWithIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
        return (cia, irq);
    }

    private static ushort ReadTimerBCounter(Mos6526 cia)
    {
        var low = cia.Read(0xDC06);
        var high = cia.Read(0xDC07);
        return (ushort)((high << 8) | low);
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA).
    /// Use case: Timer B in default INMODE=00 must count phi2 host cycles
    /// just like Timer A; this is the baseline that demos use when they do
    /// not chain Timer B to Timer A.
    /// Acceptance: With Timer B latch=$0010 and CRB=0x11 (start + force
    /// load, INMODE=00), Timer B underflows after sixteen counting cycles
    /// (load + count pipeline accounted for) and ICR bit 1 latches.
    /// </summary>
    [Fact]
    public void TimerB_Mode00_Phi2_Baseline_UnderflowsAfterLatchCycles()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer B latch = 0x0010 (16 counts).
        cia.Write(0xDC06, 0x10);
        cia.Write(0xDC07, 0x00);

        // Enable ICR mask bit 1 (Timer B underflow IRQ).
        cia.Write(0xDC0D, 0x82);

        // CRB = 0x11: start + force-load, INMODE bits 5-6 = 00 -> phi2.
        cia.Write(0xDC0F, 0x11);

        // The force-load pipeline burns 2 cycles loading, 1 cycle of
        // count delay, then 16 counts to reach 0. Tick well past that to
        // be sure the underflow latched.
        for (var i = 0; i < 64; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeTrue("Timer B in phi2 mode should underflow within 64 cycles");
        cia.Read(0xDC0D).Should().Be(0x82, "ICR returns Timer B underflow bit (0x02) + master IR (0x80)");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA).
    /// Use case: CRB INMODE=10 chains Timer B to Timer A underflow events.
    /// Timer B must advance by exactly one count per Timer A underflow,
    /// regardless of host phi2 cycles between events. This is the
    /// classic 32-bit composite timer demos use.
    /// Acceptance: With Timer A latch=10 and Timer B latch=5 (both
    /// configured before starting), starting both timers leads to Timer A
    /// underflowing roughly every 10 cycles; Timer B underflows after 5
    /// of those A underflows. After enough ticks for 5 A underflows plus
    /// pipeline overhead, IRQ asserts with ICR bit 1 (B underflow) and
    /// bit 0 (A underflow, latched from repeated A underflows during the
    /// chain countdown).
    /// </summary>
    [Fact]
    public void TimerB_Mode10_AdvancesOncePerTimerAUnderflow()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer A latch = 10.
        cia.Write(0xDC04, 0x0A);
        cia.Write(0xDC05, 0x00);

        // Timer B latch = 5.
        cia.Write(0xDC06, 0x05);
        cia.Write(0xDC07, 0x00);

        // Enable ICR mask bit 1 (Timer B underflow IRQ) only - we want
        // to detect B underflow, not A.
        cia.Write(0xDC0D, 0x82);

        // CRB = 0x51: start + force-load, INMODE bits 5-6 = 10 -> Timer A
        // underflow source.
        cia.Write(0xDC0F, 0x51);

        // CRA = 0x11: start + force-load, INMODE bit 5 = 0 -> phi2.
        cia.Write(0xDC0E, 0x11);

        // Verify Timer B does not assert IRQ before any A underflow could
        // have happened: at minimum, A needs latch+load-delay cycles for
        // its first underflow (~12 cycles), and B needs 5 A underflows.
        for (var i = 0; i < 10; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeFalse("Timer B should not have underflowed within the first A underflow window");

        // Now tick enough cycles for at least 5 A underflows (>50 cycles)
        // plus the B count pipeline overhead.
        for (var i = 0; i < 150; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeTrue("Timer B should underflow after 5 Timer A underflows");
        // Both Timer A and Timer B underflow bits will be latched because
        // A underflows repeatedly during the chain. Only B's IRQ is
        // masked-in (bit 1), so the master IR (bit 7) is set.
        cia.Read(0xDC0D).Should().Be(0x83, "ICR returns Timer A+B underflow flags + master IR");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA).
    /// Use case: With CRB INMODE=10, Timer B must only advance when Timer
    /// A actually underflows. If Timer A is stopped (CRA bit 0 = 0), no
    /// underflow events occur, so Timer B must stay parked at its load
    /// value even while the CIA is ticking.
    /// Acceptance: After force-loading B = 0x0042 with CRB=0x51 (start +
    /// force-load + INMODE=10) and leaving CRA stopped, ticking 200
    /// cycles leaves Timer B counter unchanged at 0x0042 and ICR Timer B
    /// flag clear.
    /// </summary>
    [Fact]
    public void TimerB_Mode10_DoesNotAdvanceWhenTimerAStopped()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer A latch = 10 but we will NOT start it.
        cia.Write(0xDC04, 0x0A);
        cia.Write(0xDC05, 0x00);

        // Timer B latch = 0x0042 - distinctive value to detect drift.
        cia.Write(0xDC06, 0x42);
        cia.Write(0xDC07, 0x00);

        // Enable ICR mask bit 1; we want to detect any unexpected B IRQ.
        cia.Write(0xDC0D, 0x82);

        // CRB = 0x51: start + force-load, INMODE=10 (Timer A underflow).
        cia.Write(0xDC0F, 0x51);

        // CRA NOT started: leave at default (0x00). Timer A will never
        // underflow, so Timer B must not advance.

        for (var i = 0; i < 200; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeFalse("Timer B must not underflow when Timer A is stopped");
        ReadTimerBCounter(cia).Should().Be((ushort)0x0042, "Timer B counter must stay at its load value");
        cia.Read(0xDC0D).Should().Be(0x00, "no IRQ flags should latch when Timer A is stopped");
    }

    /// <summary>
    /// FR/TR: FR-CIA-TIMER (BACKFILL-CIA).
    /// Use case: When CRB INMODE=10 causes Timer B to underflow on the
    /// fifth Timer A underflow, the ICR Timer B latch bit (bit 1) must
    /// be set and, with the IMR mask bit 1 enabled, the IRQ line must
    /// assert. Reading the ICR must return both the underflow bit and
    /// the master IR bit, then clear them.
    /// Acceptance: After enough ticks for B to underflow, IRQ is high;
    /// reading $DC0D returns 0x82 and IRQ drops back to deasserted.
    /// </summary>
    [Fact]
    public void TimerB_Mode10_LatchesIcrBit1AndAssertsIrqOnUnderflow()
    {
        var (cia, irq) = BuildCiaWithIrq();

        // Timer A latch = 2 (fast underflows for short test).
        cia.Write(0xDC04, 0x02);
        cia.Write(0xDC05, 0x00);

        // Timer B latch = 3.
        cia.Write(0xDC06, 0x03);
        cia.Write(0xDC07, 0x00);

        // Enable ICR mask bit 1 (Timer B underflow).
        cia.Write(0xDC0D, 0x82);

        // Start Timer B in chain mode first, then start Timer A.
        cia.Write(0xDC0F, 0x51);
        cia.Write(0xDC0E, 0x11);

        // 2 cycles per A underflow * 3 needed * ~2x for pipeline = ~32.
        // Tick 64 to be safe.
        for (var i = 0; i < 64; i++)
            cia.Tick();

        irq.IsAsserted.Should().BeTrue("chained Timer B underflow must assert IRQ when IMR bit 1 is set");

        var icr = cia.Read(0xDC0D);
        icr.Should().Be(0x83, "ICR must report Timer A+B underflow flags (0x03) plus master IR (0x80)");
        irq.IsAsserted.Should().BeFalse("reading ICR must clear pending flags and release IRQ");
    }
}
