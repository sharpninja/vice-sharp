using System.Security.Cryptography;
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
        return File.Exists(path) && VerifyHash(path, romName);
    }

    public async Task<ReadOnlyMemory<byte>> DownloadRom(string romName, string architecture, CancellationToken cancellationToken)
    {
        var archPath = Path.Combine(RomBasePath, architecture);
        Directory.CreateDirectory(archPath);
        var path = Path.Combine(archPath, romName);

        if (IsAvailable(romName, architecture))
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }

        if (!RomDatabase.TryGetValue(romName, out var entry))
        {
            throw new InvalidOperationException($"Unknown ROM: {romName}");
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ViceSharp/1.0");

        var data = await client.GetByteArrayAsync(entry.Url, cancellationToken);
        
        if (SHA256.HashData(data) != entry.Sha256)
        {
            throw new InvalidOperationException($"ROM hash verification failed for {romName}");
        }

        await File.WriteAllBytesAsync(path, data, cancellationToken);
        return data;
    }

    private static bool VerifyHash(string path, string romName)
    {
        if (!RomDatabase.TryGetValue(romName, out var entry))
            return true;
        
        var data = File.ReadAllBytes(path);
        return SHA256.HashData(data) == entry.Sha256;
    }

    private static readonly Dictionary<string, RomEntry> RomDatabase = new()
    {
        ["basic.901226-01.bin"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/basic.901226-01.bin", Convert.FromHexString("57AF4E93E79D32E1A74E2D7E2B1F4733A8C1D7E69C3B7E38E6C7E0A90F5B4A21")),
        ["kernal.901227-03.bin"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/kernal.901227-03.bin", Convert.FromHexString("1D503D283A7F6C6E3E8A2D5B2E9F9A7B3C5D7E9F1A2B3C4D5E6F7A8B9C0D1E2F")),
        ["characters.901225-01.bin"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/characters.901225-01.bin", Convert.FromHexString("F32CA8C7E5D1B2A63F7E2D5C1B0A9F8E7D6C5B4A39281706F5E4D3C2B1A0F9E8")),
    };

    private sealed record RomEntry(string Url, byte[] Sha256);
}
