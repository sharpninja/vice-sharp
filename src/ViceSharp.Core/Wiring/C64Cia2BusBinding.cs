using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core.Wiring;

/// <summary>
/// Wires a C64 CIA2 chip to InterSystemBus endpoints so a running C64
/// drives real signal traffic on the user-port and IEC buses. Without
/// this, CIA2 register writes update internal state but never reach the
/// new substrate.
///
/// Pin mapping (C64 service manual + Commodore IEC docs):
///   CIA2 PA2        -> UserPort PA2 (RS232 TXD / general output)
///   CIA2 PB0..PB7   -> UserPort PB0..PB7 (RS232 data + parallel cable)
///   CIA2 PA3 out=1  -> assert IEC ATN  (line low on bus)
///   CIA2 PA4 out=1  -> assert IEC CLK  (line low on bus)
///   CIA2 PA5 out=1  -> assert IEC DATA (line low on bus)
///   CIA2 PA6 in     -> IEC CLK  inverted (bus low = bit 1)
///   CIA2 PA7 in     -> IEC DATA inverted (bus low = bit 1)
///
/// PA0/PA1 stay reserved for the VIC bank-select function and are not
/// touched here. The C64-side inverters are baked into the polarity rules
/// above so callers can program CIA2 with the natural "1 means assert"
/// convention.
/// </summary>
public static class C64Cia2BusBinding
{
    /// <summary>
    /// Bind CIA2 port input/output to the supplied bus endpoints. Pass null
    /// for either endpoint to skip that bus.
    /// </summary>
    public static void Bind(
        Mos6526 cia2,
        IBusEndpoint? userPort = null,
        IBusEndpoint? iec = null)
    {
        ArgumentNullException.ThrowIfNull(cia2);
        if (userPort is null && iec is null)
            throw new ArgumentException("At least one bus endpoint must be supplied.", nameof(userPort));

        // Chain with any callbacks already set by the C64 builder (VIC bank
        // selector on PortAOutputChanged, ReadCia2PortA on PortAInput, etc.)
        // rather than overwrite them.
        var prevPbOut = cia2.PortBOutputChanged;
        var prevPbIn = cia2.PortBInput;
        var prevPaOut = cia2.PortAOutputChanged;
        var prevPaIn = cia2.PortAInput;

        cia2.PortBOutputChanged = value =>
        {
            prevPbOut?.Invoke(value);
            if (userPort is not null)
                UserPortInterSystemBus.WritePortB(userPort, value);
        };

        cia2.PortBInput = () =>
        {
            byte composed = prevPbIn?.Invoke() ?? 0xFF;
            if (userPort is not null)
                composed = (byte)(composed & UserPortInterSystemBus.ReadPortB(userPort));
            return composed;
        };

        cia2.PortAOutputChanged = value =>
        {
            prevPaOut?.Invoke(value);
            if (userPort is not null)
                userPort.Pull(UserPortInterSystemBus.Pa2, low: (value & 0x04) == 0);
            if (iec is not null)
            {
                iec.Pull(IecInterSystemBus.Atn, low: (value & 0x08) != 0);
                iec.Pull(IecInterSystemBus.Clk, low: (value & 0x10) != 0);
                iec.Pull(IecInterSystemBus.Data, low: (value & 0x20) != 0);
            }
        };

        cia2.PortAInput = () =>
        {
            byte composed = prevPaIn?.Invoke() ?? 0;
            if (iec is not null)
            {
                if (!iec.ReadLine(IecInterSystemBus.Clk))
                    composed |= 0x40;
                if (!iec.ReadLine(IecInterSystemBus.Data))
                    composed |= 0x80;
            }
            return composed;
        };
    }
}
