using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;

namespace Gb64Import;

/// <summary>
/// Validates that VERSION.NFO Screenshot: and SID: relative paths resolve under
/// the attached library roots (screenshots + hvsc), matching Resolve-*.ps1 rules.
/// </summary>
public static class AssetPathValidator
{
    public sealed class Options
    {
        public required string GamesRoot { get; init; }
        public required string HvscRoot { get; init; }
        public required string ScreenshotsRoot { get; init; }
        public int Limit { get; init; }
        public int MaxDegreeOfParallelism { get; init; } = Math.Max(2, Environment.ProcessorCount / 2);
        public int MaxSamples { get; init; } = 30;
    }

    public sealed class Result
    {
        public int Zips;
        public int NfoPresent;
        public int NfoMissing;

        public int ShotDeclared;
        public int ShotBlank;
        public int ShotOk;
        public int ShotMissing;

        public int SidDeclared;
        public int SidBlank;
        public int SidOk;
        public int SidMissing;

        public List<string> ShotSamples { get; } = new();
        public List<string> SidSamples { get; } = new();
        public Dictionary<string, int> ShotMissingPrefixes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SidMissingPrefixes { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static Result Run(Options opt, TextWriter? log = null)
    {
        log ??= Console.Error;
        var result = new Result();
        var shotBag = new ConcurrentBag<string>();
        var sidBag = new ConcurrentBag<string>();
        var shotPref = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sidPref = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(opt.HvscRoot))
            throw new DirectoryNotFoundException("HVSC root missing: " + opt.HvscRoot);
        if (!Directory.Exists(opt.ScreenshotsRoot))
            throw new DirectoryNotFoundException("Screenshots root missing: " + opt.ScreenshotsRoot);

        var zips = Directory.EnumerateFiles(opt.GamesRoot, "*.zip", SearchOption.AllDirectories)
            .OrderBy(z => z, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (opt.Limit > 0)
            zips = zips.Take(opt.Limit).ToList();

        log.WriteLine($"Asset path validate: {zips.Count} zips");
        log.WriteLine($"  hvsc={opt.HvscRoot}");
        log.WriteLine($"  screenshots={opt.ScreenshotsRoot}");

        Parallel.ForEach(zips, new ParallelOptions { MaxDegreeOfParallelism = opt.MaxDegreeOfParallelism }, zipPath =>
        {
            Interlocked.Increment(ref result.Zips);
            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                var nfoEntry = zip.Entries.FirstOrDefault(e =>
                    e.Name.Equals("VERSION.NFO", StringComparison.OrdinalIgnoreCase));
                if (nfoEntry is null)
                {
                    Interlocked.Increment(ref result.NfoMissing);
                    return;
                }
                Interlocked.Increment(ref result.NfoPresent);

                using var sr = new StreamReader(nfoEntry.Open(), Encoding.Latin1, detectEncodingFromByteOrderMarks: true);
                var nfo = Gb64Nfo.Parse(sr.ReadToEnd());
                var zipName = Path.GetFileName(zipPath);

                // --- Screenshot ---
                if (IsBlankMeta(nfo.Screenshot))
                {
                    Interlocked.Increment(ref result.ShotBlank);
                }
                else
                {
                    Interlocked.Increment(ref result.ShotDeclared);
                    var resolved = ResolveUnderRoot(opt.ScreenshotsRoot, nfo.Screenshot!);
                    if (resolved is not null)
                    {
                        Interlocked.Increment(ref result.ShotOk);
                    }
                    else
                    {
                        Interlocked.Increment(ref result.ShotMissing);
                        var pref = PrefixOf(nfo.Screenshot!);
                        shotPref.AddOrUpdate(pref, 1, (_, n) => n + 1);
                        if (shotBag.Count < opt.MaxSamples)
                            shotBag.Add($"{zipName}: Screenshot={nfo.Screenshot}");
                    }
                }

                // --- SID ---
                if (IsBlankMeta(nfo.Sid))
                {
                    Interlocked.Increment(ref result.SidBlank);
                }
                else
                {
                    Interlocked.Increment(ref result.SidDeclared);
                    var resolved = ResolveUnderRoot(opt.HvscRoot, nfo.Sid!);
                    if (resolved is not null)
                    {
                        Interlocked.Increment(ref result.SidOk);
                    }
                    else
                    {
                        Interlocked.Increment(ref result.SidMissing);
                        var pref = PrefixOf(nfo.Sid!);
                        sidPref.AddOrUpdate(pref, 1, (_, n) => n + 1);
                        if (sidBag.Count < opt.MaxSamples)
                            sidBag.Add($"{zipName}: SID={nfo.Sid}");
                    }
                }
            }
            catch
            {
                Interlocked.Increment(ref result.NfoMissing);
            }
        });

        result.ShotSamples.AddRange(shotBag);
        result.SidSamples.AddRange(sidBag);
        foreach (var kv in shotPref.OrderByDescending(k => k.Value).Take(15))
            result.ShotMissingPrefixes[kv.Key] = kv.Value;
        foreach (var kv in sidPref.OrderByDescending(k => k.Value).Take(15))
            result.SidMissingPrefixes[kv.Key] = kv.Value;

        log.WriteLine(
            $"NFO present={result.NfoPresent} missing={result.NfoMissing}");
        log.WriteLine(
            $"Screenshot: declared={result.ShotDeclared} blank={result.ShotBlank} " +
            $"ok={result.ShotOk} missing={result.ShotMissing} " +
            $"hitRate={(result.ShotDeclared == 0 ? 100 : 100.0 * result.ShotOk / result.ShotDeclared):F2}%");
        log.WriteLine(
            $"SID:        declared={result.SidDeclared} blank={result.SidBlank} " +
            $"ok={result.SidOk} missing={result.SidMissing} " +
            $"hitRate={(result.SidDeclared == 0 ? 100 : 100.0 * result.SidOk / result.SidDeclared):F2}%");

        return result;
    }

    /// <summary>
    /// Same resolution as scripts/Resolve-SidPath.ps1 / Resolve-ScreenshotPath.ps1:
    /// normalize \ → separator, join under root, exact then case-insensitive filename match.
    /// </summary>
    public static string? ResolveUnderRoot(string root, string relativeFromNfo)
    {
        var rel = relativeFromNfo.Replace('\\', '/').Trim().TrimStart('/');
        if (rel.Length == 0) return null;
        var candidate = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(candidate))
            return candidate;

        var dir = Path.GetDirectoryName(candidate);
        var baseName = Path.GetFileName(candidate);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return null;

        try
        {
            foreach (var f in Directory.EnumerateFiles(dir))
            {
                if (string.Equals(Path.GetFileName(f), baseName, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
        }
        catch
        {
            // ignore access issues
        }
        return null;
    }

    private static bool IsBlankMeta(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        return v.Equals("(None)", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("None", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("-", StringComparison.OrdinalIgnoreCase);
    }

    private static string PrefixOf(string rel)
    {
        var n = rel.Replace('\\', '/');
        var i = n.IndexOf('/');
        return i > 0 ? n[..i] : n;
    }
}
