namespace ViceSharp.Abstractions;

/// <summary>
/// Loads and caches ROM images. Validates integrity via CRC32/SHA256
/// checksums before returning ROM data.
/// </summary>
public interface IRomProvider
{
    /// <summary>Loads a ROM by name for the specified architecture.</summary>
    ReadOnlyMemory<byte> LoadRom(string romName, string architecture);

    /// <summary>Checks whether a ROM is available and valid.</summary>
    bool IsAvailable(string romName, string architecture);

    /// <summary>Base search path for ROM files.</summary>
    string RomBasePath { get; }
}