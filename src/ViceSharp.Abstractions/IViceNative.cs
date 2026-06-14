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
    /// Attach a raw cartridge image to the native emulator instance.
    /// </summary>
    void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode);

    /// <summary>
    /// Attach a disk image path to the native emulator drive.
    /// </summary>
    void AttachDisk(uint unit, uint drive, string path);

    /// <summary>
    /// Detach the disk image from the native emulator drive.
    /// </summary>
    void DetachDisk(uint unit, uint drive);

    /// <summary>
    /// Apply a host keyboard matrix key state to the native emulator.
    /// </summary>
    void SetKeyboardMatrixKey(int row, int column, bool pressed);

    /// <summary>
    /// Read a byte from native physical C64 RAM without bus side effects.
    /// </summary>
    byte PeekRam(ushort address);

    /// <summary>
    /// Get current full machine state
    /// </summary>
    MachineState GetState();

    /// <summary>
    /// Get current native VIC-II timing state for lockstep diagnostics.
    /// </summary>
    NativeVicState GetVicState();
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

public readonly struct NativeVicState
{
    public uint Cycle { get; init; }
    public ushort RasterLine { get; init; }
    public byte RasterCycle { get; init; }
    public byte BadLine { get; init; }
    public byte DisplayState { get; init; }
    public byte SpriteDma { get; init; }
}
