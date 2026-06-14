using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core;

/// <summary>
/// 1541 serial IEC interface electronics connected to VIA1.
/// </summary>
public sealed class C1541IecInterfaceDevice : IDevice
{
    private readonly byte _addressBits;
    private byte _portBOutput;

    public C1541IecInterfaceDevice(int deviceNumber = 8)
    {
        if (deviceNumber is < 8 or > 11)
            throw new ArgumentOutOfRangeException(nameof(deviceNumber), "Device number must be 8..11.");

        DeviceNumber = deviceNumber;
        _addressBits = (byte)(((deviceNumber - 8) & 0x03) << 5);
    }

    public DeviceId Id => new(0x1400);
    public string Name => $"1541 IEC Interface #{DeviceNumber}";
    public int DeviceNumber { get; }

    /// <summary>
    /// Connects this 1541 IEC interface to the drive's VIA1 and IEC endpoint.
    /// </summary>
    public void ConnectVia1(Via6522 via1, IBusEndpoint iec, IInterSystemBus? bus = null)
    {
        ArgumentNullException.ThrowIfNull(via1);
        ArgumentNullException.ThrowIfNull(iec);

        var prevPbOut = via1.PortBOutputChanged;
        var prevPbIn = via1.PortBInput;

        via1.PortBOutputChanged = value =>
        {
            prevPbOut?.Invoke(value);
            _portBOutput = value;
            UpdateIecOutputs(iec);
            iec.Pull(IecInterSystemBus.Clk, low: (value & 0x08) != 0);
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
            composed |= _addressBits;
            return composed;
        };

        if (bus is not null)
        {
            bus.LineChanged += (_, e) =>
            {
                if (!string.Equals(e.Signal, IecInterSystemBus.Atn, StringComparison.Ordinal))
                    return;

                via1.TriggerCa1(rising: !e.NewState);
                UpdateIecOutputs(iec);
            };
        }
    }

    public void Reset()
    {
        _portBOutput = 0;
    }

    private void UpdateIecOutputs(IBusEndpoint iec)
    {
        var dataOut = (_portBOutput & 0x02) != 0;
        var atna = (_portBOutput & 0x10) != 0;
        var atnActive = !iec.ReadLine(IecInterSystemBus.Atn);
        iec.Pull(IecInterSystemBus.Data, low: dataOut || atna != atnActive);
    }
}
