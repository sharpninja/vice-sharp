namespace ViceSharp.Abstractions;

/// <summary>
/// Provider for loading ROM images into the emulator.
/// </summary>
public interface IRomProvider
{
    /// <summary>Loads a ROM file for the specified architecture.</summary>
    ReadOnlyMemory<byte> LoadRom(string romName, string architecture);

    /// <summary>Checks if a ROM is available.</summary>
    bool IsAvailable(string romName, string architecture);
}
