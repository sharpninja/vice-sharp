namespace ViceSharp.Abstractions;

/// <summary>
/// A device that occupies one or more address ranges on the system bus.
/// </summary>
public interface IAddressSpace : IDevice
{
    /// <summary>
    /// Read an 8-bit value from the specified address.
    /// </summary>
    byte Read(ushort address);

    /// <summary>
    /// Write an 8-bit value to the specified address.
    /// </summary>
    void Write(ushort address, byte value);

    /// <summary>
    /// Read without side effects (for debuggers, snapshots, disassemblers).
    /// </summary>
    byte Peek(ushort address);

    /// <summary>
    /// Check if this address space handles the given address.
    /// </summary>
    bool HandlesAddress(ushort address);
}