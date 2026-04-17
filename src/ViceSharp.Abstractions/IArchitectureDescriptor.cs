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
}

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