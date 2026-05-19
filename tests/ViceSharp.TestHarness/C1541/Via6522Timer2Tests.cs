namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA timer-2).
/// Use case: 1541 firmware uses VIA timer 2 in one-shot phi2 mode to time
/// disk-controller delays. ACR bit 5 = 0 selects phi2 countdown; underflow
/// latches IFR bit 5 and (gated by IER bit 5) asserts IRQ. T2 is one-shot:
/// after underflow it does not re-fire IFR until reload. Reading T2 lo ($08)
/// clears IFR bit 5; writing T2 hi ($09) reloads counter from latch + low
/// counter byte and clears IFR bit 5. ACR bit 5 = 1 selects pulse-count on
/// PB6 (PB6 input plumbing deferred).
/// </summary>
public sealed class Via6522Timer2Tests
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
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-2).
    /// Use case: T2 in phi2 mode (ACR bit 5 = 0) must count down on every phi2
    /// tick after the high byte write loads the counter, and underflow must
    /// latch IFR bit 5.
    /// Acceptance: Latch lo = 0x05, write hi = 0x00 (loads counter to 0x0005
    /// and starts T2). After 6 ticks (counter walks 5,4,3,2,1,0 then underflows
    /// on the next zero-state tick) IFR bit 5 is set.
    /// </summary>
    [Fact]
    public void T2Phi2_CountdownToUnderflow_SetsIfrBit5()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x00); // ACR: T2 phi2 mode (bit 5 = 0)
        via.Write(Base + 0x08, 0x05); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // T2 hi: load counter to 0x0005 + start

        for (int i = 0; i < 6; i++) via.Tick();

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(IfrT2);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-2).
    /// Use case: With IER bit 5 enabled, T2 underflow must assert the drive IRQ
    /// line. Reading T2 lo ($08) clears IFR bit 5 and (with no other pending
    /// flags) releases the IRQ line.
    /// Acceptance: After T2 underflow with IER bit 5 set, IRQ is asserted; after
    /// reading $08, IFR bit 5 is cleared and IRQ is released.
    /// </summary>
    [Fact]
    public void T2Phi2_Underflow_AssertsIrq_WhenIerBit5Enabled()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0E, (byte)(IfrAny | IfrT2)); // IER: enable T2
        via.Write(Base + 0x0B, 0x00); // ACR: T2 phi2 mode
        via.Write(Base + 0x08, 0x02); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 3; i++) via.Tick();

        irq.IsAsserted.Should().BeTrue();
        var ifrBeforeRead = via.Peek(Base + 0x0D);
        (ifrBeforeRead & IfrT2).Should().Be(IfrT2);

        // Read T2 lo: clears IFR bit 5 and releases IRQ.
        _ = via.Read(Base + 0x08);

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-2).
    /// Use case: T2 is one-shot: after the first underflow + IFR-bit-5 clear,
    /// the chip must NOT re-latch IFR bit 5 from continued phi2 ticks until the
    /// program reloads T2 by writing $09 again. The counter is allowed to keep
    /// wrapping (NMOS behaviour) but the flag stays clear.
    /// Acceptance: Drive T2 to underflow, clear IFR via $08 read, tick another
    /// 128 phi2 cycles - IFR bit 5 remains clear and IRQ stays released.
    /// </summary>
    [Fact]
    public void T2Phi2_OneShot_DoesNotReFireAfterUnderflowAndClear()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0E, (byte)(IfrAny | IfrT2)); // IER bit 5
        via.Write(Base + 0x0B, 0x00); // ACR: T2 phi2
        via.Write(Base + 0x08, 0x01); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 2; i++) via.Tick();
        (via.Read(Base + 0x0D) & IfrT2).Should().Be(IfrT2);

        _ = via.Read(Base + 0x08); // clear IFR bit 5
        (via.Read(Base + 0x0D) & IfrT2).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();

        for (int i = 0; i < 128; i++) via.Tick();

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(0);
        irq.IsAsserted.Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-2).
    /// Use case: Writing $09 reloads the T2 counter from the high byte + the
    /// existing low latch and clears IFR bit 5 (write-clear semantics matching
    /// the NMOS 6522).
    /// Acceptance: After T2 underflow leaves IFR bit 5 set, writing 0x00 to
    /// $09 clears IFR bit 5.
    /// </summary>
    [Fact]
    public void T2Phi2_WriteHi_Reloads_AndClearsIfrBit5()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x00); // ACR: T2 phi2
        via.Write(Base + 0x08, 0x01); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 2; i++) via.Tick();
        (via.Read(Base + 0x0D) & IfrT2).Should().Be(IfrT2);

        // Reload + clear: write hi with a new value.
        via.Write(Base + 0x09, 0x00);

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA timer-2).
    /// Use case: ACR bit 5 = 1 selects pulse-count mode on PB6 (count negative
    /// edges of PB6). The current build recognises the mode but has no PB6 pin
    /// input plumbed, so the T2 counter must NOT advance on phi2 ticks. PB6
    /// pulse-count wiring is deferred to a later slice.
    /// Acceptance: With ACR bit 5 = 1 and T2 loaded with 0x05, ticking 32 phi2
    /// cycles leaves IFR bit 5 cleared (no underflow) - phi2 countdown is gated
    /// off and no PB6 stimulus exists in this build.
    /// </summary>
    [Fact]
    public void T2PulseCountMode_PhiTicksDoNotAdvanceCounter_AndDoNotUnderflow()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x20); // ACR bit 5 = 1: pulse-count mode
        via.Write(Base + 0x08, 0x05); // T2 latch lo
        via.Write(Base + 0x09, 0x00); // load + start

        for (int i = 0; i < 32; i++) via.Tick();

        (via.Read(Base + 0x0D) & IfrT2).Should().Be(0);
    }
}
