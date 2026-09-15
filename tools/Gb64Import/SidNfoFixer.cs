using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Gb64Import;

/// <summary>
/// Fixes VERSION.NFO SID: fields in GB64 game ZIPs:
/// - miss-located paths (file exists under a different HVSC relative path) → rewrite to actual path
/// - unresolvable refs → set SID: to (None)
/// </summary>
public static class SidNfoFixer
{
    public sealed class Options
    {
        public required string GamesRoot { get; init; }
        public required string HvscRoot { get; init; }
        public string? RomsOut { get; init; }
        public bool DryRun { get; init; }
        public int Limit { get; init; }
        public string? LogPath { get; init; }
    }

    public sealed class Result
    {
        public int ZipsScanned;
        public int AlreadyOk;
        public int Blank;
        public int Remapped;
        public int Cleared;
        public int Failed;
        public int Relinked;
        public List<string> Samples { get; } = new();
    }

    public static Result Run(Options opt, TextWriter? log = null)
    {
        log ??= Console.Error;
        var result = new Result();
        var index = BuildHvscIndex(opt.HvscRoot);
        log.WriteLine($"HVSC index: {index.ByBaseName.Count} basenames, {index.ByNormStem.Count} unique norm-stems under {opt.HvscRoot}");

        var zips = Directory.EnumerateFiles(opt.GamesRoot, "*.zip", SearchOption.AllDirectories)
            .OrderBy(z => z, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (opt.Limit > 0)
            zips = zips.Take(opt.Limit).ToList();

        TextWriter? receipt = null;
        if (!string.IsNullOrWhiteSpace(opt.LogPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(opt.LogPath))!);
            receipt = new StreamWriter(opt.LogPath, append: false, Encoding.UTF8);
            receipt.WriteLine($"# SidNfoFixer {DateTime.UtcNow:O} dryRun={opt.DryRun}");
            receipt.WriteLine("# action\tzip\told\tnew");
        }

        try
        {
            foreach (var zipPath in zips)
            {
                result.ZipsScanned++;
                try
                {
                    var outcome = ProcessZip(zipPath, opt.HvscRoot, index, opt.DryRun);
                    switch (outcome.Kind)
                    {
                        case FixKind.Ok:
                            result.AlreadyOk++;
                            if (!string.IsNullOrWhiteSpace(outcome.NewSid) && !string.IsNullOrWhiteSpace(opt.RomsOut))
                            {
                                if (TryRelinkSid(zipPath, outcome.NewSid!, opt.HvscRoot, opt.RomsOut, opt.DryRun))
                                    result.Relinked++;
                            }
                            break;
                        case FixKind.Blank:
                            result.Blank++;
                            break;
                        case FixKind.Remap:
                            result.Remapped++;
                            receipt?.WriteLine($"remap\t{Path.GetFileName(zipPath)}\t{outcome.OldSid}\t{outcome.NewSid}");
                            if (result.Samples.Count < 40)
                                result.Samples.Add($"REMAP {Path.GetFileName(zipPath)}: {outcome.OldSid} -> {outcome.NewSid}");
                            if (!string.IsNullOrWhiteSpace(opt.RomsOut) && !string.IsNullOrWhiteSpace(outcome.NewSid))
                            {
                                if (TryRelinkSid(zipPath, outcome.NewSid!, opt.HvscRoot, opt.RomsOut, opt.DryRun))
                                    result.Relinked++;
                            }
                            break;
                        case FixKind.Clear:
                            result.Cleared++;
                            receipt?.WriteLine($"clear\t{Path.GetFileName(zipPath)}\t{outcome.OldSid}\t(None)");
                            if (result.Samples.Count < 40)
                                result.Samples.Add($"CLEAR {Path.GetFileName(zipPath)}: {outcome.OldSid}");
                            // drop any previously hardlinked sid beside multi-file game (best-effort)
                            if (!string.IsNullOrWhiteSpace(opt.RomsOut) && !string.IsNullOrWhiteSpace(outcome.OldSid))
                                TryUnlinkSid(zipPath, outcome.OldSid!, opt.RomsOut, opt.DryRun);
                            break;
                        case FixKind.Fail:
                            result.Failed++;
                            if (result.Samples.Count < 40)
                                result.Samples.Add($"FAIL {Path.GetFileName(zipPath)}: {outcome.Error}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    if (result.Samples.Count < 40)
                        result.Samples.Add($"FAIL {Path.GetFileName(zipPath)}: {ex.Message}");
                }
            }
        }
        finally
        {
            receipt?.Dispose();
        }

        log.WriteLine(
            $"SidNfoFixer done. scanned={result.ZipsScanned} ok={result.AlreadyOk} blank={result.Blank} " +
            $"remapped={result.Remapped} cleared={result.Cleared} relinked={result.Relinked} " +
            $"failed={result.Failed} dryRun={opt.DryRun}");
        if (!string.IsNullOrWhiteSpace(opt.LogPath))
            log.WriteLine($"receipt: {opt.LogPath}");
        return result;
    }

    private static bool TryRelinkSid(string zipPath, string sidRel, string hvscRoot, string romsOut, bool dryRun)
    {
        var id = RomTagBuilder.ParseZipStem(Path.GetFileName(zipPath)).Id;
        if (id is null) return false;
        var gameDir = FindMultiFileGameDir(romsOut, id);
        if (gameDir is null) return false;
        var src = Path.Combine(hvscRoot, sidRel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(src)) return false;
        var dest = Path.Combine(gameDir, Path.GetFileName(src));
        if (dryRun) return true;
        try
        {
            if (File.Exists(dest))
            {
                // replace wrong hardlink/copy if different file
                try { File.Delete(dest); } catch { return false; }
            }
            if (OperatingSystem.IsWindows())
            {
                if (!CreateHardLink(dest, src, IntPtr.Zero))
                    File.Copy(src, dest);
            }
            else File.Copy(src, dest);
            return true;
        }
        catch { return false; }
    }

    private static void TryUnlinkSid(string zipPath, string oldSid, string romsOut, bool dryRun)
    {
        var id = RomTagBuilder.ParseZipStem(Path.GetFileName(zipPath)).Id;
        if (id is null) return;
        var gameDir = FindMultiFileGameDir(romsOut, id);
        if (gameDir is null) return;
        var name = Path.GetFileName(oldSid.Replace('/', '\\'));
        if (string.IsNullOrEmpty(name)) return;
        var dest = Path.Combine(gameDir, name);
        if (!File.Exists(dest)) return;
        if (dryRun) return;
        try { File.Delete(dest); } catch { /* ignore */ }
    }

    private static string? FindMultiFileGameDir(string romsOut, string gb64Id)
    {
        foreach (var dir in Directory.EnumerateDirectories(romsOut))
        {
            var name = Path.GetFileName(dir);
            if (name.Contains($"(gb64-{gb64Id})", StringComparison.OrdinalIgnoreCase))
                return dir;
        }
        return null;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    private enum FixKind { Ok, Blank, Remap, Clear, Fail }

    private sealed class FixOutcome
    {
        public FixKind Kind { get; init; }
        public string? OldSid { get; init; }
        public string? NewSid { get; init; }
        public string? Error { get; init; }
    }

    public sealed class HvscIndex
    {
        public Dictionary<string, List<string>> ByBaseName { get; } = new(StringComparer.OrdinalIgnoreCase);
        // only stems that map to exactly one relative path
        public Dictionary<string, string> ByNormStem { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static HvscIndex BuildHvscIndex(string hvscRoot)
    {
        var idx = new HvscIndex();
        var stemBuckets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(hvscRoot, "*.sid", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(hvscRoot, file);
            // store with backslashes to match GB64 NFO style
            rel = rel.Replace(Path.AltDirectorySeparatorChar, '\\');
            var baseName = Path.GetFileName(file);
            if (!idx.ByBaseName.TryGetValue(baseName, out var list))
            {
                list = new List<string>();
                idx.ByBaseName[baseName] = list;
            }
            list.Add(rel);

            var stem = NormalizeStem(Path.GetFileNameWithoutExtension(baseName));
            if (!stemBuckets.TryGetValue(stem, out var sb))
            {
                sb = new List<string>();
                stemBuckets[stem] = sb;
            }
            sb.Add(rel);
        }

        foreach (var kv in stemBuckets)
        {
            var distinct = kv.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinct.Count == 1)
                idx.ByNormStem[kv.Key] = distinct[0];
        }
        return idx;
    }

    /// <summary>
    /// Resolve a declared SID relative path to an actual HVSC-relative path, or null if none.
    /// </summary>
    public static string? ResolveActualSidPath(string declared, string hvscRoot, HvscIndex index)
    {
        if (IsBlankMeta(declared)) return null;
        var rel = declared.Replace('/', '\\').Trim().TrimStart('\\');
        // strip leading junk like /CGSC/...
        while (rel.StartsWith('\\')) rel = rel[1..];

        var exact = Path.Combine(hvscRoot, rel);
        if (File.Exists(exact))
            return rel;

        // case-insensitive in directory
        var dir = Path.GetDirectoryName(exact);
        var leaf = Path.GetFileName(exact);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            foreach (var f in Directory.EnumerateFiles(dir))
            {
                if (string.Equals(Path.GetFileName(f), leaf, StringComparison.OrdinalIgnoreCase))
                    return Path.GetRelativePath(hvscRoot, f).Replace(Path.AltDirectorySeparatorChar, '\\');
            }
        }

        // basename match elsewhere
        if (index.ByBaseName.TryGetValue(leaf, out var hits) && hits.Count > 0)
            return PickBest(hits, rel);

        // unique normalized stem (e.g. Hotrod.sid → Hot_Rod.sid)
        if (!string.IsNullOrEmpty(leaf) &&
            leaf.EndsWith(".sid", StringComparison.OrdinalIgnoreCase))
        {
            var stem = NormalizeStem(Path.GetFileNameWithoutExtension(leaf));
            if (index.ByNormStem.TryGetValue(stem, out var only))
                return only;
        }

        return null;
    }

    private static string PickBest(List<string> hits, string preferredRel)
    {
        if (hits.Count == 1) return hits[0];
        var prefRoot = preferredRel.Split('\\', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var sameRoot = hits.FirstOrDefault(h =>
            h.StartsWith(prefRoot + "\\", StringComparison.OrdinalIgnoreCase));
        if (sameRoot is not null) return sameRoot;
        var musicians = hits.FirstOrDefault(h => h.StartsWith("MUSICIANS\\", StringComparison.OrdinalIgnoreCase));
        if (musicians is not null) return musicians;
        var games = hits.FirstOrDefault(h => h.StartsWith("GAMES\\", StringComparison.OrdinalIgnoreCase));
        if (games is not null) return games;
        return hits[0];
    }

    private static string NormalizeStem(string stem) =>
        Regex.Replace(stem, @"[_\-\s\.]+", "", RegexOptions.CultureInvariant).ToLowerInvariant();

    private static bool IsBlankMeta(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        return v.Equals("(None)", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("None", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("-", StringComparison.OrdinalIgnoreCase);
    }

    private static FixOutcome ProcessZip(string zipPath, string hvscRoot, HvscIndex index, bool dryRun)
    {
        using var zip = ZipFile.Open(zipPath, dryRun ? ZipArchiveMode.Read : ZipArchiveMode.Update);
        var entry = zip.Entries.FirstOrDefault(e =>
            e.Name.Equals("VERSION.NFO", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return new FixOutcome { Kind = FixKind.Ok };

        string text;
        using (var sr = new StreamReader(entry.Open(), Encoding.Latin1, detectEncodingFromByteOrderMarks: true))
            text = sr.ReadToEnd();

        var nfo = Gb64Nfo.Parse(text);
        var hasSecondaryPaths = HasSecondarySidPaths(text);

        if (IsBlankMeta(nfo.Sid))
        {
            if (hasSecondaryPaths)
            {
                // Keep primary blank-as-(None), scrub CGSC/secondary path lines.
                if (!dryRun)
                    RewriteSidLine(zip, entry, text, "(None)");
                return new FixOutcome { Kind = FixKind.Clear, OldSid = "secondary-path", NewSid = "(None)" };
            }
            return new FixOutcome { Kind = FixKind.Blank, OldSid = nfo.Sid };
        }

        var declared = nfo.Sid!.Trim();
        var actual = ResolveActualSidPath(declared, hvscRoot, index);
        var declaredNorm = declared.Replace('/', '\\').Trim().TrimStart('\\');

        if (actual is not null &&
            string.Equals(actual, declaredNorm, StringComparison.OrdinalIgnoreCase) &&
            !hasSecondaryPaths)
            return new FixOutcome { Kind = FixKind.Ok, OldSid = declared, NewSid = actual };

        if (actual is not null)
        {
            if (!dryRun)
                RewriteSidLine(zip, entry, text, actual);
            return new FixOutcome
            {
                Kind = string.Equals(actual, declaredNorm, StringComparison.OrdinalIgnoreCase)
                    ? FixKind.Ok
                    : FixKind.Remap,
                OldSid = declared,
                NewSid = actual,
            };
        }

        // Unresolvable primary → clear primary + secondary path lines.
        if (!dryRun)
            RewriteSidLine(zip, entry, text, "(None)");
        return new FixOutcome { Kind = FixKind.Clear, OldSid = declared, NewSid = "(None)" };
    }

    private static bool HasSecondarySidPaths(string nfoText)
    {
        var count = 0;
        foreach (var line in nfoText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var m = Regex.Match(line, @"^SID:\s*(.*)$", RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            count++;
            if (count > 1 && LooksLikeSidPath(m.Groups[1].Value.Trim()))
                return true;
        }
        return false;
    }

    public static string RewriteSidInText(string nfoText, string newSidValue)
    {
        var lines = nfoText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var foundPrimary = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"^(SID:)(\s*)(.*)$", RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            var existing = m.Groups[3].Value.Trim();

            if (!foundPrimary)
            {
                lines[i] = "SID:" + PadValue(m.Groups[2].Value, newSidValue);
                foundPrimary = true;
                continue;
            }

            // Always neutralize secondary path-like SID lines (CGSC notes, etc.).
            if (LooksLikeSidPath(existing))
                lines[i] = "SID:" + PadValue(m.Groups[2].Value, "(None)");
        }
        if (!foundPrimary)
        {
            var list = lines.ToList();
            list.Insert(0, "SID:" + PadValue("               ", newSidValue));
            lines = list.ToArray();
        }
        var nl = nfoText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return string.Join(nl, lines);
    }

    private static bool LooksLikeSidPath(string value) =>
        value.Contains('\\') || value.Contains('/') ||
        value.EndsWith(".sid", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith(".mus", StringComparison.OrdinalIgnoreCase);

    private static string PadValue(string existingSpaces, string value)
    {
        // Prefer fixed-width field like "SID:               path"
        const int targetKeyWidth = 19; // "SID:" + spaces to column ~19
        var pad = Math.Max(1, targetKeyWidth - 4);
        return new string(' ', pad) + value;
    }

    private static void RewriteSidLine(ZipArchive zip, ZipArchiveEntry entry, string originalText, string newSid)
    {
        var newText = RewriteSidInText(originalText, newSid);
        var fullName = entry.FullName;
        entry.Delete();
        var created = zip.CreateEntry(fullName, CompressionLevel.Optimal);
        using var sw = new StreamWriter(created.Open(), Encoding.Latin1);
        sw.Write(newText);
    }
}
