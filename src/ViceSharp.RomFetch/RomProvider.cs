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
        
        if (!SHA256.HashData(data).AsSpan().SequenceEqual(entry.Sha256))
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
        return SHA256.HashData(data).AsSpan().SequenceEqual(entry.Sha256);
    }

    private static readonly Dictionary<string, RomEntry> RomDatabase = new()
    {
        ["basic"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/basic.901226-01.bin", Convert.FromHexString("89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D")),
        ["kernal"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/kernal.901227-03.bin", Convert.FromHexString("83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721")),
        ["characters"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/characters.901225-01.bin", Convert.FromHexString("FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420")),
    };

    private sealed record RomEntry(string Url, byte[] Sha256);
}
