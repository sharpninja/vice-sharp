using System.Security.Cryptography;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The exact base-fixture ROM subset of PLAN-VSFLOCKSTEP-001 (fixture and toolchain contract): a launch may use
/// only ROM files whose relative path and SHA-256 both appear here. ROMs are never committed; the generator hashes
/// the files of the VICE data root it launches with and rejects any other input before the launch.
/// </summary>
public static class RomAllowlist
{
    /// <summary>Path (relative to the VICE data root) to upper-case SHA-256, exactly as the plan lists them.</summary>
    public static readonly IReadOnlyDictionary<string, string> BaseFixtureSubset = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["C64/basic-901226-01.bin"] = "89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D",
        ["C64/kernal-901227-03.bin"] = "83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721",
        ["C64/chargen-901225-01.bin"] = "FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420",
        ["DRIVES/dos1541-325302-01+901229-05.bin"] = "D1D45AFB46FD4E2B48D93CA367B889D75654A3B7ACF73044E51F6D880C09369E",
        ["VIC20/basic-901486-01.bin"] = "8BE2D882E0EEB1C6923A43660236EA69D43CA8A3BD0613B1B6C910601584FBED",
        ["VIC20/kernal.901486-07.bin"] = "FF6D1416B91A71F5240A5BB8ADC9FDC1B955374C48322CA47B3E436334F168FB",
        ["VIC20/kernal.901486-06.bin"] = "8FC94BD895F488AC0AE1A52ABDC32988CD63C777770B89A9D415513098535CC2",
        ["VIC20/chargen-901460-03.bin"] = "BDA280726CFE069CA70EF5E2F2D95A29F69D3950C859A12E6979464963FCF502",
        ["DRIVES/dos1540-325302+3-01.bin"] = "A966E3FF6FD2DB7B8F6D57E4D105C589A92E88A6705201215604AC864CC3C27F",
    };

    /// <summary>
    /// Hashes every listed file under <paramref name="dataRoot"/> and checks it against <paramref name="allowlist"/>
    /// (the base subset by default). A missing file, an unlisted path or a differing hash each fail that entry.
    /// </summary>
    public static IReadOnlyList<RomCheck> Verify(string dataRoot, IEnumerable<string> relativePaths, IReadOnlyDictionary<string, string>? allowlist = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataRoot);
        ArgumentNullException.ThrowIfNull(relativePaths);
        allowlist ??= BaseFixtureSubset;
        var checks = new List<RomCheck>();
        foreach (var relative in relativePaths)
        {
            var key = relative.Replace('\\', '/');
            allowlist.TryGetValue(key, out var expected);
            var path = Path.Combine(dataRoot, key.Replace('/', Path.DirectorySeparatorChar));
            string? actual = null;
            if (File.Exists(path))
            {
                using var stream = File.OpenRead(path);
                actual = Convert.ToHexString(SHA256.HashData(stream));
            }
            var ok = actual is not null && expected is not null && string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            var reason = actual is null ? "file missing" : expected is null ? "path not allowlisted" : ok ? "ok" : "hash differs from the allowlist";
            checks.Add(new RomCheck(key, actual, expected, ok, reason));
        }
        return checks;
    }
}
