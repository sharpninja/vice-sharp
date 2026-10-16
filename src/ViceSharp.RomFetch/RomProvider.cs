using System.Security.Cryptography;
using ViceSharp.Abstractions;

namespace ViceSharp.RomFetch;

public class RomProvider : IRomProvider
{
    private readonly IReadOnlyList<string> _romBasePaths;
    private readonly HttpClient? _httpClient;

    public string RomBasePath { get; }

    public RomProvider(string romBasePath)
        : this(romBasePath, [])
    {
    }

    public RomProvider(string romBasePath, IEnumerable<string> fallbackRomBasePaths)
        : this(romBasePath, fallbackRomBasePaths, null)
    {
    }

    /// <summary>
    /// FR-CFG-002: resolves ROMs from configured roots and optionally uses a
    /// caller-owned HTTP client for verified downloads.
    /// </summary>
    /// <param name="romBasePath">Primary root and destination for downloaded ROMs.</param>
    /// <param name="fallbackRomBasePaths">Additional roots searched for existing ROMs.</param>
    /// <param name="httpClient">Optional download client; the caller retains ownership.</param>
    public RomProvider(string romBasePath, IEnumerable<string> fallbackRomBasePaths, HttpClient? httpClient)
    {
        RomBasePath = romBasePath;
        _httpClient = httpClient;
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
        cancellationToken.ThrowIfCancellationRequested();

        if (TryResolvePath(romName, architecture, out var availablePath) && VerifyHash(availablePath, romName))
        {
            return await File.ReadAllBytesAsync(availablePath, cancellationToken).ConfigureAwait(false);
        }

        if (!RomDatabase.TryGetValue(romName, out var entry))
        {
            throw new InvalidOperationException($"Unknown ROM: {romName}");
        }

        using var ownedClient = _httpClient is null ? new HttpClient() : null;
        var client = _httpClient ?? ownedClient!;
        using var request = new HttpRequestMessage(HttpMethod.Get, entry.Url);
        request.Headers.UserAgent.ParseAdd("ViceSharp/1.0");

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        
        if (!SHA256.HashData(data).AsSpan().SequenceEqual(entry.Sha256))
        {
            throw new InvalidOperationException($"ROM hash verification failed for {romName}");
        }

        var archPath = Path.Combine(RomBasePath, architecture);
        Directory.CreateDirectory(archPath);
        var path = Path.Combine(archPath, romName);
        await File.WriteAllBytesAsync(path, data, cancellationToken).ConfigureAwait(false);
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
            foreach (var fileName in EnumerateRomFileNames(romName))
            {
                var candidate = Path.Combine(basePath, architecture, fileName);
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }
        }

        path = string.Empty;
        return false;
    }

    private static IEnumerable<string> EnumerateRomFileNames(string romName)
    {
        yield return romName;

        if (!RomFileAliases.TryGetValue(romName, out var aliases))
            yield break;

        foreach (var alias in aliases)
        {
            if (!string.Equals(alias, romName, StringComparison.OrdinalIgnoreCase))
                yield return alias;
        }
    }

    private static readonly Dictionary<string, string[]> RomFileAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["basic"] = ["basic-901226-01.bin"],
        ["kernal"] = ["kernal-901227-03.bin"],
        ["characters"] = ["chargen-901225-01.bin"],
        // VIC-20: full filenames double as download keys (dots in kernal names).
        ["basic-901486-01.bin"] = ["basic-901486-01.bin"],
        ["kernal.901486-07.bin"] = ["kernal.901486-07.bin"],
        ["kernal.901486-06.bin"] = ["kernal.901486-06.bin"],
        ["chargen-901460-03.bin"] = ["chargen-901460-03.bin"],
    };

    private static readonly Dictionary<string, RomEntry> RomDatabase = new()
    {
        // VICE's GitHub mirror (svn-mirror) hosts the canonical ROM dumps; the sourceforge /roms/ paths
        // now 404. Same bytes, so the SHA256 checks are unchanged (verified against the mirror).
        ["basic"] = new RomEntry("https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/C64/basic-901226-01.bin", Convert.FromHexString("89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D")),
        ["kernal"] = new RomEntry("https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/C64/kernal-901227-03.bin", Convert.FromHexString("83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721")),
        ["characters"] = new RomEntry("https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/C64/chargen-901225-01.bin", Convert.FromHexString("FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420")),
        // VIC-20 (Iteration 2): download keys are the VICE data filenames under VIC20/.
        ["basic-901486-01.bin"] = new RomEntry(
            "https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/VIC20/basic-901486-01.bin",
            Convert.FromHexString("8BE2D882E0EEB1C6923A43660236EA69D43CA8A3BD0613B1B6C910601584FBED")),
        ["kernal.901486-07.bin"] = new RomEntry(
            "https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/VIC20/kernal.901486-07.bin",
            Convert.FromHexString("FF6D1416B91A71F5240A5BB8ADC9FDC1B955374C48322CA47B3E436334F168FB")),
        ["kernal.901486-06.bin"] = new RomEntry(
            "https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/VIC20/kernal.901486-06.bin",
            Convert.FromHexString("8FC94BD895F488AC0AE1A52ABDC32988CD63C777770B89A9D415513098535CC2")),
        ["chargen-901460-03.bin"] = new RomEntry(
            "https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/VIC20/chargen-901460-03.bin",
            Convert.FromHexString("BDA280726CFE069CA70EF5E2F2D95A29F69D3950C859A12E6979464963FCF502")),
    };

    static RomProvider()
    {
        // FR-CFG-002: desktop startup writes the same canonical names consumed by
        // C64RomSet. Share the pinned entries with the existing short download keys.
        RomDatabase["basic-901226-01.bin"] = RomDatabase["basic"];
        RomDatabase["kernal-901227-03.bin"] = RomDatabase["kernal"];
        RomDatabase["chargen-901225-01.bin"] = RomDatabase["characters"];
    }

    private sealed record RomEntry(string Url, byte[] Sha256);
}
