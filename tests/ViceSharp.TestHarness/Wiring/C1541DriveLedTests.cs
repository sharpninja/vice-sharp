namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-DRVLED-001 / TR-DRVLIFE-001 / TEST-DRVLED-001.
/// Use case: The drive activity LED is a function of the drive firmware - the
/// 1541 DOS ROM drives VIA2 ($1C00) port B bit 3 - exactly as VICE surfaces
/// led_status. It must reflect that bit and be independent of the motor bit
/// (PB2) and of IEC bus traffic.
/// </summary>
public sealed class C1541DriveLedTests
{
    private static Via6522 BuildIsolatedVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Via6522(bus, irq) { BaseAddress = 0x1C00, Size = 0x0400 };
    }

    /// <summary>
    /// Acceptance: Setting VIA2 PB3 lights the LED; clearing it turns it off.
    /// </summary>
    [Fact]
    public void Pb3_DrivesLed()
    {
        var via = BuildIsolatedVia();
        var mechanism = new C1541DriveMechanismDevice();
        mechanism.ConnectVia2(via);
        via.Write(0x1C02, 0xFF); // DDRB outputs

        via.Write(0x1C00, 0x08); // PB3 set
        mechanism.LedOn.Should().BeTrue("VIA2 PB3 high lights the drive LED");

        via.Write(0x1C00, 0x00); // PB3 clear
        mechanism.LedOn.Should().BeFalse("VIA2 PB3 low turns the drive LED off");
    }

    /// <summary>
    /// Acceptance: The LED bit is independent of the motor bit (PB2): motor on
    /// with LED off reads LedOn=false; motor on with LED on reads true.
    /// </summary>
    [Fact]
    public void Led_IsIndependentOfMotorBit()
    {
        var via = BuildIsolatedVia();
        var mechanism = new C1541DriveMechanismDevice();
        mechanism.ConnectVia2(via);
        via.Write(0x1C02, 0xFF);

        via.Write(0x1C00, 0x04); // motor on (PB2), LED off
        mechanism.LedOn.Should().BeFalse("the motor bit does not light the LED");

        via.Write(0x1C00, 0x0C); // motor on + LED on
        mechanism.LedOn.Should().BeTrue("PB3 lights the LED regardless of the motor bit");
    }
}
