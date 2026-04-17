using ViceSharp.Abstractions;

namespace ViceSharp.RomFetch;

public class RomProvider : IRomProvider
{
    public string RomBasePath { get; }

    public RomProvider(string romBasePath)
    {
        RomBasePath = romBasePath;
        Directory.CreateDirectory(RomBasePath);
    }

    public ReadOnlyMemory<byte> LoadRom(string romName, string architecture)
    {
        var path = Path.Combine(RomBasePath, architecture, romName);
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"ROM not found: {romName} for {architecture}", path);
        }

        return File.ReadAllBytes(path);
    }

    public bool IsAvailable(string romName, string architecture)
    {
        var path = Path.Combine(RomBasePath, architecture, romName);
        return File.Exists(path);
    }
}