namespace ViceSharp.Abstractions;

/// <summary>
/// Native VICE emulator binding interface
/// </summary>
public interface IViceNative : IDisposable
{
    /// <summary>
    /// Reset emulator to initial power on state
    /// </summary>
    void Reset();

    /// <summary>
    /// Advance emulator by one single master cycle
    /// </summary>
    void Step();

    /// <summary>
    /// Get current full machine state
    /// </summary>
    MachineState GetState();
}

/// <summary>
/// Serializable machine state snapshot
/// </summary>
public readonly struct MachineState
{
    // CPU Registers
    public byte A { get; init; }
    public byte X { get; init; }
    public byte Y { get; init; }
    public byte S { get; init; }
    public byte P { get; init; }
    public ushort PC { get; init; }

    // Cycle counter
    public long Cycle { get; init; }
}