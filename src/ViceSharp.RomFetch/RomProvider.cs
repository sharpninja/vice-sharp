using System.Security.Cryptography;
using ViceSharp.Abstractions;

namespace ViceSharp.RomFetch;

public class RomProvider : IRomProvider
{
    private readonly IReadOnlyList<string> _romBasePaths;

    public string RomBasePath { get; }

    public RomProvider(string romBasePath)
        : this(romBasePath, [])
    {
    }

    public RomProvider(string romBasePath, IEnumerable<string> fallbackRomBasePaths)
    {
        RomBasePath = romBasePath;
        Directory.CreateDirectory(RomBasePath);
        _romBasePaths = new[] { RomBasePath }
            .Concat(fallbackRomBasePaths.Where(path => !string.IsNullOrWhiteSpace(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ReadOnlyMemory<byte> LoadRom(string romName, string architecture)
    {
        if (!TryResolvePath(romName, architecture, out var path))
        {
            throw new FileNotFoundException(
                $"ROM not found: {romName} for {architecture}",
                Path.Combine(RomBasePath, architecture, romName));
        }

        return File.ReadAllBytes(path);
    }

    public bool IsAvailable(string romName, string architecture)
    {
        return TryResolvePath(romName, architecture, out var path) && VerifyHash(path, romName);
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

    private bool TryResolvePath(string romName, string architecture, out string path)
    {
        foreach (var basePath in _romBasePaths)
        {
            var candidate = Path.Combine(basePath, architecture, romName);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    private static readonly Dictionary<string, RomEntry> RomDatabase = new()
    {
        ["basic"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/basic.901226-01.bin", Convert.FromHexString("89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D")),
        ["kernal"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/kernal.901227-03.bin", Convert.FromHexString("83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721")),
        ["characters"] = new RomEntry("https://vice-emu.sourceforge.io/roms/C64/characters.901225-01.bin", Convert.FromHexString("FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420")),
    };

    private sealed record RomEntry(string Url, byte[] Sha256);
}
