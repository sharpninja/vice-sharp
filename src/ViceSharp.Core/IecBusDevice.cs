using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Carries the machine's always-on IEC <see cref="IInterSystemBus"/> as a
/// registered device so consumers (host activity monitor, the IEC spy/scope,
/// dynamic drive attach) can retrieve the single canonical bus instance.
///
/// The IEC bus is open-collector and present on every C64 regardless of whether
/// a drive is currently attached, mirroring real hardware where the serial port
/// is always available; drives attach/detach against it at runtime.
///
/// Note: registering this device only makes the bus discoverable - it does NOT
/// by itself route CIA2's serial reads to the live bus. That live read (with
/// the faithful electrical model) is a separate, parity-gated step.
/// </summary>
public sealed class IecBusDevice : IDevice
{
    public IecBusDevice(IInterSystemBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        Bus = bus;
    }

    /// <summary>The shared IEC serial bus for this machine.</summary>
    public IInterSystemBus Bus { get; }

    public DeviceId Id => new(0x6403);

    public string Name => "IEC Bus";

    public void Reset()
    {
    }
}
