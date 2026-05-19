namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA T2 PB6 pulse-count).
/// Use case: 1541 / 6522 VIA timer 2 supports a pulse-count mode (ACR bit 5 = 1)
/// where T2 decrements on negative transitions of the PB6 input pin instead of
/// on every phi2 tick. The previous slice landed the ACR bit 5 = 1 gate (T2 no
/// longer advances on phi2) but had no PB6 stimulus path. This slice plumbs
/// PB6 input through TriggerPb6(rising): negative edges decrement T2,
/// positive edges are ignored, and the original phi2 behaviour remains intact
/// when ACR bit 5 = 0.
/// </summary>
public sealed class Via6522T2Pb6Tests
{
    private const ushort Base = 0x1800;
    private const byte IfrT2 = 0x20;
    private const byte IfrAny = 0x80;

    private static (Via6522 via, InterruptLine irq) CreateVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via = new Via6522(bus, irq) { BaseAddress = Base, Size = 0x0400 };
        via.Reset();
        return (via, irq);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA T2 PB6 pulse-count).
    /// Use case: In pulse-count mode (ACR bit 5 = 1) T2 must ignore phi2 ticks;
    /// the only legal decrement source is a negative edge on PB6. Without any
    /// TriggerPb6 calls the counter holds and IFR bit 5 stays clear.
    /// Acceptance: ACR = 0x20, T2 loaded with 0x05, run 100 phi2 ticks - IFR
    /// bit 5 remains 0 and T2 counter (read via $08 lo) is still 0x05.
    /// </summary>
    [Fact]
    public void T2PulseCount_NoPb6Pulses_T2DoesNotAdvanceOnPhi2()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x20); // ACR bit 5 = 1: pulse-count mode
        via.Write(Base + 0x08, 0x05); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 100; i++) via.Tick();

        (via.Peek(Base + 0x0D) & IfrT2).Should().Be(0);
        via.Peek(Base + 0x08).Should().Be(0x05);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA T2 PB6 pulse-count).
    /// Use case: Each negative (high-to-low) edge on PB6 decrements T2 in
    /// pulse-count mode. After enough negative edges to drive the counter
    /// from its load value through zero, T2 underflows and latches IFR bit 5
    /// (gating IRQ when IER bit 5 is enabled, identical to phi2 underflow).
    /// Acceptance: ACR = 0x20, T2 loaded with 0x05, six TriggerPb6(rising: false)
    /// calls walk counter 5,4,3,2,1,0 then underflow on the sixth - IFR bit 5
    /// is set.
    /// </summary>
    [Fact]
    public void T2PulseCount_NegativeEdgeOnPb6_DecrementsAndUnderflowsT2()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x20); // ACR bit 5 = 1
        via.Write(Base + 0x08, 0x05); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        // Walk counter 5,4,3,2,1,0 then underflow on the 6th negative edge.
        for (int i = 0; i < 6; i++) via.TriggerPb6(rising: false);

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(IfrT2);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA T2 PB6 pulse-count).
    /// Use case: Only negative PB6 transitions decrement T2; rising edges (the
    /// inactive direction) must leave the counter alone. The 6522 spec
    /// specifies "negative transitions" only as the count source.
    /// Acceptance: ACR = 0x20, T2 loaded with 0x05, 10 TriggerPb6(rising: true)
    /// calls leave IFR bit 5 cleared and the counter unchanged at 0x05.
    /// </summary>
    [Fact]
    public void T2PulseCount_PositiveEdgesOnPb6_DoNotDecrementT2()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x20); // ACR bit 5 = 1
        via.Write(Base + 0x08, 0x05); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 10; i++) via.TriggerPb6(rising: true);

        (via.Peek(Base + 0x0D) & IfrT2).Should().Be(0);
        via.Peek(Base + 0x08).Should().Be(0x05);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA T2 PB6 pulse-count).
    /// Use case: Mode switching between PB6 pulse-count and phi2 countdown
    /// must take effect on the next tick. Software flips ACR bit 5 at runtime
    /// to alternate between byte-clock-driven counting (pulse mode) and
    /// time-based countdown (phi2 mode); the chip must not strand the counter
    /// in either path.
    /// Acceptance: Load T2 = 0x05, set ACR = 0x20 (pulse mode); 100 phi2 ticks
    /// leave T2 unchanged. Switch to ACR = 0x00 (phi2 mode); 6 more phi2 ticks
    /// drive counter 5,4,3,2,1,0 -> underflow and IFR bit 5 is set.
    /// </summary>
    [Fact]
    public void T2PulseCount_SwitchBackToPhi2Mode_ResumesPhi2Countdown()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x20); // ACR bit 5 = 1 (pulse-count)
        via.Write(Base + 0x08, 0x05); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 100; i++) via.Tick();
        (via.Peek(Base + 0x0D) & IfrT2).Should().Be(0);

        via.Write(Base + 0x0B, 0x00); // ACR bit 5 = 0 (phi2)

        for (int i = 0; i < 6; i++) via.Tick();

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(IfrT2);
    }
}
