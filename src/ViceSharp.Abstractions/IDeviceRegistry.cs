namespace ViceSharp.Abstractions;

/// <summary>
/// Registry of all devices in a machine. Supports lookup by DeviceId,
/// interface type, or device role. Populated during machine construction.
/// </summary>
public interface IDeviceRegistry
{
    /// <summary>Returns the device with the given ID, or null if not found.</summary>
    IDevice? GetById(DeviceId id);

    /// <summary>Returns all devices implementing the specified interface.</summary>
    IReadOnlyList<T> GetAll<T>() where T : IDevice;

    /// <summary>Returns all registered devices.</summary>
    IReadOnlyList<IDevice> All { get; }

    /// <summary>Returns the single device with the given role, or null.</summary>
    IDevice? GetByRole(DeviceRole role);

    /// <summary>Total number of registered devices.</summary>
    int Count { get; }
}

/// <summary>
/// Standard device roles for well-known machine components.
/// </summary>
public enum DeviceRole
{
    /// <summary>Board-level system core policy</summary>
    SystemCore,

    /// <summary>Main system CPU</summary>
    Cpu,
    
    /// <summary>Video display controller</summary>
    VideoChip,
    
    /// <summary>Audio sound generator</summary>
    AudioChip,
    
    /// <summary>Complex Interface Adapter 1</summary>
    Cia1,
    
    /// <summary>Complex Interface Adapter 2</summary>
    Cia2,
    
    /// <summary>Programmable Logic Array</summary>
    Pla,
    
    /// <summary>System RAM</summary>
    SystemRam,
    
    /// <summary>KERNAL ROM</summary>
    KernalRom,
    
    /// <summary>BASIC ROM</summary>
    BasicRom,
    
    /// <summary>Character Generator ROM</summary>
    ChargenRom,

    /// <summary>Expansion cartridge port</summary>
    CartridgePort,

    /// <summary>6502 CPU of an attached drive (1541 family).</summary>
    DriveCpu,

    /// <summary>VIA 6522 on a 1541-family drive.</summary>
    DriveVia,

    /// <summary>RAM on a 1541-family drive.</summary>
    DriveRam,

    /// <summary>DOS ROM on a 1541-family drive.</summary>
    DriveRom,

    /// <summary>Mounted disk image (D64 / G64 / D81 etc.) on a 1541-family drive.</summary>
    DriveDisk,
}
