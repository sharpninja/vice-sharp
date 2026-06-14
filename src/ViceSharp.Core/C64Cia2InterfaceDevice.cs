using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core;

/// <summary>
/// C64 CIA2-facing interface electronics for the user-port and IEC bus.
/// </summary>
public sealed class C64Cia2InterfaceDevice : IDevice
{
    public DeviceId Id => new(0x6402);
    public string Name => "C64 CIA2 Interface";

    /// <summary>
    /// Connects the C64 CIA2 pins to the supplied bus endpoints.
    /// </summary>
    public void ConnectCia2(
        Mos6526 cia2,
        IBusEndpoint? userPort = null,
        IBusEndpoint? iec = null,
        Action? synchronizeIec = null)
    {
        ArgumentNullException.ThrowIfNull(cia2);
        if (userPort is null && iec is null)
            throw new ArgumentException("At least one bus endpoint must be supplied.", nameof(userPort));

        // Chain with callbacks already set by the C64 builder, including VIC
        // bank selection on PA0/PA1 and profile-specific floating input masks.
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
                synchronizeIec?.Invoke();
                var latch = cia2.PortAOutputLatch;
                var direction = cia2.PortADataDirection;
                iec.Pull(IecInterSystemBus.Atn, low: IsOutputAsserted(latch, direction, 0x08));
                iec.Pull(IecInterSystemBus.Clk, low: IsOutputAsserted(latch, direction, 0x10));
                iec.Pull(IecInterSystemBus.Data, low: IsOutputAsserted(latch, direction, 0x20));
            }
        };

        cia2.PortAInput = () =>
        {
            byte composed = prevPaIn?.Invoke() ?? 0;
            if (iec is not null)
            {
                synchronizeIec?.Invoke();
                composed &= 0x3F;
                if (iec.ReadLine(IecInterSystemBus.Clk))
                    composed |= 0x40;
                if (iec.ReadLine(IecInterSystemBus.Data))
                    composed |= 0x80;
            }
            return composed;
        };
    }

    public void Reset()
    {
    }

    private static bool IsOutputAsserted(byte latch, byte direction, byte mask)
    {
        return (direction & mask) != 0 && (latch & mask) != 0;
    }
}
