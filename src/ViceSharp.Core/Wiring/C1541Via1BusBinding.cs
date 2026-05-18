using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core.Wiring;

/// <summary>
/// Wires a 1541 drive's VIA1 chip to an InterSystemBus endpoint on the IEC
/// serial bus, completing the C64 - 1541 signal loop (CIA2 binding handles
/// the host side; this one handles the drive side).
///
/// Pin mapping (1541 service manual + VICE drive sources):
///   VIA1 PB0 in   - IEC DATA inverted (bus low = bit 1)
///   VIA1 PB1 out  - drive's DATA output (1 = assert = bus low)
///   VIA1 PB2 in   - IEC CLK inverted
///   VIA1 PB3 out  - drive's CLK output (1 = assert = bus low)
///   VIA1 PB4 out  - ATNA (ATN acknowledge, hardware-tied also)
///   VIA1 PB5..PB6 - device-address jumpers (hardwired per drive number)
///   VIA1 PB7 in   - IEC ATN inverted
///
/// Polarity matches the C64 side - the binding takes care of the on-board
/// inverters so the drive firmware sees "1 = asserted" at the register
/// level.
/// </summary>
public static class C1541Via1BusBinding
{
    /// <summary>
    /// Bind a 1541 VIA1 to the given IEC bus endpoint.
    /// </summary>
    /// <param name="via1">The drive's VIA1 instance.</param>
    /// <param name="iec">IEC bus endpoint for this drive.</param>
    /// <param name="deviceNumber">Drive device number (8, 9, 10, 11). Bits
    /// 5..6 of PB encode this minus 8 (so device 8 = 00, device 9 = 01).</param>
    public static void Bind(Via6522 via1, IBusEndpoint iec, int deviceNumber = 8)
    {
        ArgumentNullException.ThrowIfNull(via1);
        ArgumentNullException.ThrowIfNull(iec);
        if (deviceNumber < 8 || deviceNumber > 11)
            throw new ArgumentOutOfRangeException(nameof(deviceNumber), "Device number must be 8..11.");

        var addressBits = (byte)(((deviceNumber - 8) & 0x03) << 5);

        var prevPbOut = via1.PortBOutputChanged;
        var prevPbIn = via1.PortBInput;

        via1.PortBOutputChanged = value =>
        {
            prevPbOut?.Invoke(value);
            iec.Pull(IecInterSystemBus.Data, low: (value & 0x02) != 0);
            iec.Pull(IecInterSystemBus.Clk, low: (value & 0x08) != 0);
            // PB4 is ATNA - not driven onto a bus signal directly; the
            // drive's internal hardware AND-gates it with ATN to control
            // the DATA pull. Modeled by reading the line state below.
        };

        via1.PortBInput = () =>
        {
            byte composed = prevPbIn?.Invoke() ?? 0;
            if (!iec.ReadLine(IecInterSystemBus.Data))
                composed |= 0x01;
            if (!iec.ReadLine(IecInterSystemBus.Clk))
                composed |= 0x04;
            if (!iec.ReadLine(IecInterSystemBus.Atn))
                composed |= 0x80;
            composed |= addressBits;
            return composed;
        };
    }
}
