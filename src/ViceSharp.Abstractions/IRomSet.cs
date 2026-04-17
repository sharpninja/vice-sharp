namespace ViceSharp.Abstractions;

/// <summary>
/// Describes the complete set of ROMs required by a specific architecture.
/// Each entry specifies the ROM name, expected size, and known-good checksums.
/// </summary>
public interface IRomSet
{
    /// <summary>Architecture this ROM set belongs to (e.g., "c64").</summary>
    string Architecture { get; }

    /// <summary>Checks if all required ROMs are present and valid.</summary>
    bool IsComplete(IRomProvider provider);
}