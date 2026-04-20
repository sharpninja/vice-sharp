namespace ViceSharp.Abstractions;

/// <summary>
/// Immutable declarative description of a machine architecture.
/// Lists devices, memory map, clock configuration, and interrupt wiring.
/// Used by IArchitectureBuilder to construct a running IMachine.
/// </summary>
public interface IArchitectureDescriptor
{
    /// <summary>Machine name (e.g., "Commodore 64 PAL").</summary>
    string MachineName { get; }

    /// <summary>Master clock frequency in Hz.</summary>
    long MasterClockHz { get; }

    /// <summary>Video standard (PAL or NTSC).</summary>
    VideoStandard VideoStandard { get; }
    
    /// <summary>Device descriptors for this architecture.</summary>
    IReadOnlyList<DeviceDescriptor> Devices { get; }
    
    /// <summary>Required ROM set for this architecture.</summary>
    IRomSet? RequiredRoms { get; }
}

/// <summary>
/// Describes a device required by an architecture.
/// </summary>
public readonly record struct DeviceDescriptor(
    string Name,
    DeviceId Id,
    DeviceRole Role,
    ushort BaseAddress,
    int Size);

/// <summary>
/// Video output standard.
/// </summary>
public enum VideoStandard
{
    /// <summary>PAL (50Hz)</summary>
    Pal,
    
    /// <summary>NTSC (60Hz)</summary>
    Ntsc
}
