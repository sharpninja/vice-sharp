namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA timer-1 PB7).
/// Use case: The 1541 drives VIA1 PB7 as a timer-1 output to produce a stable
/// head-step clock; demos use the same path for stable square-wave output.
/// ACR bit 7 = 1 routes the timer-1 underflow event onto PB7. ACR bit 6
/// then selects toggle-on-underflow (continuous, free-run square wave) vs.
/// one-shot (low at T1 start, high at first underflow).
/// </summary>
public sealed class Via6522Timer1Pb7Tests
{
    private const ushort Base = 0x1800;
    private const byte Pb7 = 0x80;

    private static (Via6522 via, InterruptLine irq) CreateVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via = new Via6522(bus, irq) { BaseAddress = Base, Size = 0x0400 };
        via.Reset();
        return (via, irq);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-1 PB7).
    /// Use case: With ACR bit 7 = 0 the chip leaves PB7 as a normal DDR-gated
    /// output pin; software-driven ORB writes propagate directly and the
    /// timer must not perturb the bit.
    /// Acceptance: ORB = $80, DDRB = $80, ACR = $00 -> port B reads $80; running
    /// T1 to several underflows leaves PB7 = 1 unchanged.
    /// </summary>
    [Fact]
    public void Acr7Clear_Pb7IsRegularDdrPin_NotAffectedByTimer1()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x02, 0x80); // DDRB bit 7 = output
        via.Write(Base + 0x00, 0x80); // ORB bit 7 = 1
        via.Write(Base + 0x0B, 0x00); // ACR: PB7 timer routing OFF
        via.Write(Base + 0x04, 0x02); // T1L-L = 2
        via.Write(Base + 0x05, 0x00); // T1C-H = 0 (start)

        for (int i = 0; i < 12; i++) via.Tick();

        (via.Read(Base + 0x00) & Pb7).Should().Be(Pb7);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-1 PB7).
    /// Use case: ACR bit 7 = 1 + bit 6 = 1 puts T1 into continuous free-run
    /// mode with PB7 toggling on every underflow. With latch = N and DDRB bit
    /// 7 = 1 the port-B PB7 bit must invert once per underflow.
    /// Acceptance: Latch = 5; after N+1 ticks PB7 has toggled exactly once;
    /// after another N+1 ticks (10 total) PB7 has toggled a second time.
    /// </summary>
    [Fact]
    public void Acr7And6Set_Pb7TogglesOnEachT1Underflow()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x02, 0x80); // DDRB bit 7 = output
        via.Write(Base + 0x0B, 0xC0); // ACR bits 7+6: PB7 + continuous
        via.Write(Base + 0x04, 0x05); // T1L-L = 5
        via.Write(Base + 0x05, 0x00); // T1C-H = 0 (start)

        var initial = (byte)(via.Read(Base + 0x00) & Pb7);

        for (int i = 0; i < 6; i++) via.Tick(); // first underflow at tick 6

        var afterFirst = (byte)(via.Read(Base + 0x00) & Pb7);
        afterFirst.Should().NotBe(initial);

        for (int i = 0; i < 6; i++) via.Tick(); // second underflow

        var afterSecond = (byte)(via.Read(Base + 0x00) & Pb7);
        afterSecond.Should().NotBe(afterFirst);
        afterSecond.Should().Be(initial);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-1 PB7).
    /// Use case: PB7 toggling must continue indefinitely while the chip is in
    /// continuous mode. After an even number of underflows the bit is back to
    /// its initial value; after an odd number it is inverted.
    /// Acceptance: 10 underflows leave PB7 at its initial level; 11 underflows
    /// leave it inverted.
    /// </summary>
    [Fact]
    public void ContinuousMode_PiB7TogglesAcrossManyUnderflows()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x02, 0x80); // DDRB bit 7 = output
        via.Write(Base + 0x0B, 0xC0); // ACR: PB7 + continuous
        via.Write(Base + 0x04, 0x02); // T1L-L = 2
        via.Write(Base + 0x05, 0x00); // T1C-H = 0 (start)

        var initial = (byte)(via.Read(Base + 0x00) & Pb7);

        // 10 underflows: first at tick 3, then each subsequent on cycle latch+1
        // After T1C-H write the counter is reloaded from latch (2). Tick 1->1,
        // tick 2->0, tick 3->underflow + reload to 2. So underflows happen at
        // ticks 3, 6, 9, ... -> 3 * N ticks for N underflows.
        for (int i = 0; i < 30; i++) via.Tick();

        var afterEven = (byte)(via.Read(Base + 0x00) & Pb7);
        afterEven.Should().Be(initial); // 10 toggles == back to start

        for (int i = 0; i < 3; i++) via.Tick(); // one more underflow

        var afterOdd = (byte)(via.Read(Base + 0x00) & Pb7);
        afterOdd.Should().NotBe(initial);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-1 PB7).
    /// Use case: With DDRB bit 7 = 0 (PB7 = input) the timer-driven PB7 state
    /// must not appear in port-B reads; the external input drives bit 7
    /// instead, matching the 6522 pin-direction rule.
    /// Acceptance: DDRB bit 7 = 0, ACR = $C0, PortBInput supplies 0 in bit 7;
    /// running several T1 underflows leaves the port-B read with bit 7 = 0.
    /// </summary>
    [Fact]
    public void DdrbBit7Input_Pb7TimerToggleDoesNotAppearInPortBRead()
    {
        var (via, _) = CreateVia();
        via.PortBInput = () => 0x00; // external bit 7 = 0
        via.Write(Base + 0x02, 0x00); // DDRB: all inputs
        via.Write(Base + 0x0B, 0xC0); // ACR: PB7 + continuous (internal toggle armed)
        via.Write(Base + 0x04, 0x02); // T1L-L = 2
        via.Write(Base + 0x05, 0x00); // T1C-H = 0 (start)

        for (int i = 0; i < 15; i++) via.Tick(); // several underflows

        (via.Read(Base + 0x00) & Pb7).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-1 PB7).
    /// Use case: ACR bit 7 = 1 + bit 6 = 0 selects one-shot PB7 mode: PB7 goes
    /// low when T1 is started (T1C-H write) and goes high on the first T1
    /// underflow. Subsequent underflows do not re-toggle.
    /// Acceptance: With DDRB bit 7 = 1 and ACR = $80, PB7 reads 0 between
    /// T1 start and first underflow; PB7 reads 1 after first underflow and
    /// stays 1 across a second underflow.
    /// </summary>
    [Fact]
    public void Acr7SetAcr6Clear_Pb7OneShotMode_GoesHighOnFirstUnderflowAndStays()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x02, 0x80); // DDRB bit 7 = output
        via.Write(Base + 0x0B, 0x80); // ACR bit 7 only: PB7 one-shot
        via.Write(Base + 0x04, 0x03); // T1L-L = 3
        via.Write(Base + 0x05, 0x00); // T1C-H = 0 (start: PB7 -> low)

        // Tick a few cycles short of underflow: PB7 must still be low.
        for (int i = 0; i < 3; i++) via.Tick();
        (via.Read(Base + 0x00) & Pb7).Should().Be(0);

        // One more tick brings the underflow; PB7 -> high.
        via.Tick();
        (via.Read(Base + 0x00) & Pb7).Should().Be(Pb7);

        // In one-shot mode T1 stops, so additional ticks must not re-toggle
        // PB7. Even if T1 were re-triggered the spec says one-shot PB7 stays
        // high after the first underflow; we check the simpler post-underflow
        // case here.
        for (int i = 0; i < 20; i++) via.Tick();
        (via.Read(Base + 0x00) & Pb7).Should().Be(Pb7);
    }
}
