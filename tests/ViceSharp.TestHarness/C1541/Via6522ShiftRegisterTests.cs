namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA shift register).
/// Use case: 1541 drive uses the VIA 6522 shift register to clock bytes
/// to / from CB1 + CB2. ACR bits 4-2 select among 8 shift modes; after
/// 8 shifts the SR IFR bit (bit 2) latches and (with IER bit 2 set) gates
/// the IRQ line. This slice covers mode 110 (shift out under phi2) and
/// mode 010 (shift in under phi2); T2- and CB1-clocked modes remain
/// stubs documented inline.
/// </summary>
public sealed class Via6522ShiftRegisterTests
{
    private const ushort Base = 0x1800;
    private const byte IfrSr = 0x04;
    private const byte IerEnable = 0x80;

    private static (Via6522 via, InterruptLine irq) CreateVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via = new Via6522(bus, irq) { BaseAddress = Base, Size = 0x0400 };
        via.Reset();
        return (via, irq);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA shift register).
    /// Use case: ACR shift mode 000 (disabled) is a transparent latch:
    /// writes round-trip through SR with no shift activity and no SR
    /// interrupt regardless of how many cycles tick.
    /// Acceptance: After ACR = 0, SR write $5A reads back $5A; ticking
    /// 64 cycles leaves IFR bit 2 cleared.
    /// </summary>
    [Fact]
    public void Mode000_Disabled_SrRoundTrips_NoShiftActivity()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x00); // ACR: shift mode 000 (disabled)

        via.Write(Base + 0x0A, 0x5A); // SR = $5A
        via.Read(Base + 0x0A).Should().Be(0x5A);

        for (var i = 0; i < 64; i++)
        {
            via.Tick();
        }

        (via.Read(Base + 0x0D) & IfrSr).Should().Be(0, "shift register should be idle when mode is disabled");
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA shift register).
    /// Use case: ACR shift mode 110 (shift out under phi2) shifts SR one
    /// bit per phi2 tick; after 8 ticks the SR IFR bit latches.
    /// Acceptance: With ACR = $18 (bits 4-2 = 110) and SR = $C3, ticking
    /// 8 phi2 cycles sets IFR bit 2.
    /// </summary>
    [Fact]
    public void Mode110_ShiftOutPhi2_LatchesSrIfrAfterEightTicks()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x18); // ACR: bits 4-2 = 110 (shift out under phi2)
        via.Write(Base + 0x0A, 0xC3);

        for (var i = 0; i < 8; i++)
        {
            via.Tick();
        }

        (via.Read(Base + 0x0D) & IfrSr).Should().Be(IfrSr, "8 shift-out ticks should latch the SR IFR bit");
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA shift register).
    /// Use case: With IER bit 2 set, the SR IFR latch gates an IRQ; writing
    /// $04 to IFR clears the SR flag and releases the IRQ line.
    /// Acceptance: After 8 shift-out ticks (mode 110), IRQ asserted;
    /// clearing the SR flag releases the line.
    /// </summary>
    [Fact]
    public void Mode110_ShiftOutPhi2_AssertsIrq_WithIerEnabled()
    {
        var (via, irq) = CreateVia();
        via.Write(Base + 0x0E, IerEnable | IfrSr); // IER bit 7 set + bit 2 (SR int enable)
        via.Write(Base + 0x0B, 0x18);
        via.Write(Base + 0x0A, 0xC3);

        for (var i = 0; i < 8; i++)
        {
            via.Tick();
        }

        irq.IsAsserted.Should().BeTrue("SR IFR + IER bit 2 should gate the VIA IRQ output");

        via.Write(Base + 0x0D, IfrSr); // clear SR IFR flag

        (via.Read(Base + 0x0D) & IfrSr).Should().Be(0);
        irq.IsAsserted.Should().BeFalse("clearing the SR flag should release the IRQ");
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA shift register).
    /// Use case: ACR shift mode 010 (shift in under phi2) shifts CB2 data
    /// into SR one bit per phi2 tick; after 8 ticks the SR IFR bit latches.
    /// CB2 is not yet plumbed, so the test verifies only the IFR latch
    /// (incoming bit value can be 0 or 1).
    /// Acceptance: With ACR = $08 (bits 4-2 = 010), ticking 8 phi2 cycles
    /// sets IFR bit 2.
    /// </summary>
    [Fact]
    public void Mode010_ShiftInPhi2_LatchesSrIfrAfterEightTicks()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x08); // ACR: bits 4-2 = 010 (shift in under phi2)

        for (var i = 0; i < 8; i++)
        {
            via.Tick();
        }

        (via.Read(Base + 0x0D) & IfrSr).Should().Be(IfrSr, "8 shift-in ticks should latch the SR IFR bit");
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA shift register).
    /// Use case: Fewer than 8 shift ticks leave the SR IFR bit unset.
    /// Acceptance: With ACR mode 110 + SR write, ticking only 7 phi2 cycles
    /// keeps IFR bit 2 cleared.
    /// </summary>
    [Fact]
    public void Mode110_PartialShift_DoesNotLatchSrIfr()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0B, 0x18);
        via.Write(Base + 0x0A, 0xC3);

        for (var i = 0; i < 7; i++)
        {
            via.Tick();
        }

        (via.Read(Base + 0x0D) & IfrSr).Should().Be(0, "7 ticks is one short of an SR latch");
    }
}
