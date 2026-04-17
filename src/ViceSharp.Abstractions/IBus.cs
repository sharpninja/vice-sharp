namespace ViceSharp.Abstractions;

/// <summary>
/// System bus that connects all devices together.
/// </summary>
public interface IBus
{
    /// <summary>
    /// Read a byte from the specified address.
    /// </summary>
    byte Read(ushort address);

    /// <summary>
    /// Write a byte to the specified address.
    /// </summary>
    void Write(ushort address, byte value);

    /// <summary>
    /// Read without side effects.
    /// </summary>
    byte Peek(ushort address);

    /// <summary>
    /// Register a device on the bus.
    /// </summary>
    void RegisterDevice(IAddressSpace device);

    /// <summary>
    /// Unregister a device from the bus.
    /// </summary>
    void UnregisterDevice(IAddressSpace device);
}