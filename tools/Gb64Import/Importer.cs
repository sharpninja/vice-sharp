using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace Gb64Import;

public sealed class ImportOptions
{
    public required string GamesRoot { get; init; }
    public required string RomsOut { get; init; }
    public string? HvscRoot { get; init; }
    public string? ScreenshotsRoot { get; init; }
    public int MaxDegreeOfParallelism { get; init; } = Math.Max(1, Environment.ProcessorCount / 2);
    public int Limit { get; init; } = 0; // 0 = all
    public bool Force { get; init; }
    public string? StatePath { get; init; }
}

public sealed class ImportResult
{
    public int Processed;
    public int Skipped;
    public int Failed;
    public int Written;
    public List<string> Errors { get; } = new();
}

public static class Importer
{
    public static ImportResult Run(ImportOptions opt, TextWriter? log = null)
    {
        log ??= Console.Error;
        Directory.CreateDirectory(opt.RomsOut);
        var statePath = opt.StatePath ?? Path.Combine(opt.RomsOut, ".gb64-import-state.txt");
        var done = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(statePath) && !opt.Force)
        {
            foreach (var line in File.ReadLines(statePath))
            {
                var id = line.Trim();
                if (id.Length > 0) done.TryAdd(id, 0);
            }
        }

        var zips = Directory.EnumerateFiles(opt.GamesRoot, "*.zip", SearchOption.AllDirectories)
            .OrderBy(z => z, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (opt.Limit > 0)
            zips = zips.Take(opt.Limit).ToList();

        log.WriteLine($"GB64 import: {zips.Count} zips -> {opt.RomsOut} (force={opt.Force}, already={done.Count})");

        var result = new ImportResult();
        var progressEvery = 500;
        var stateLock = new object();
        // Fresh state file when force; otherwise append-only for crash-safe resume.
        if (opt.Force)
            File.WriteAllText(statePath, string.Empty, Encoding.UTF8);

        try
        {
            Parallel.ForEach(zips, new ParallelOptions { MaxDegreeOfParallelism = opt.MaxDegreeOfParallelism }, zipPath =>
            {
                try
                {
                    var (idHint, revHint, shortName) = RomTagBuilder.ParseZipStem(Path.GetFileName(zipPath));
                    var key = idHint ?? Path.GetFileName(zipPath);
                    if (!opt.Force && done.ContainsKey(key))
                    {
                        Interlocked.Increment(ref result.Skipped);
                        return;
                    }

                    var outcome = ImportOne(zipPath, opt, shortName, revHint, idHint);
                    if (outcome.Ok)
                    {
                        var w = Interlocked.Increment(ref result.Written);
                        done.TryAdd(key, 0);
                        // Persist each success so a crash mid-run is resumable.
                        lock (stateLock)
                        {
                            File.AppendAllText(statePath, key + Environment.NewLine, Encoding.UTF8);
                        }
                        Interlocked.Increment(ref result.Processed);
                        if (w % progressEvery == 0)
                            log.WriteLine($"  progress written={result.Written} skipped={result.Skipped} failed={result.Failed}");
                    }
                    else
                    {
                        Interlocked.Increment(ref result.Failed);
                        lock (result.Errors)
                        {
                            if (result.Errors.Count < 50)
                                result.Errors.Add($"{Path.GetFileName(zipPath)}: {outcome.Error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref result.Failed);
                    lock (result.Errors)
                    {
                        if (result.Errors.Count < 50)
                            result.Errors.Add($"{Path.GetFileName(zipPath)}: {ex.Message}");
                    }
                }
            });
        }
        finally
        {
            // Marker for Prepare-RomMLibrary gate (roms/.gb64-library-built)
            var markerDir = Directory.GetParent(opt.RomsOut)!.FullName;
            File.WriteAllText(
                Path.Combine(markerDir, ".gb64-library-built"),
                string.Join('\n',
                [
                    $"builtUtc={DateTime.UtcNow:O}",
                    $"gamesRoot={opt.GamesRoot}",
                    $"romsOut={opt.RomsOut}",
                    $"written={result.Written}",
                    $"skipped={result.Skipped}",
                    $"failed={result.Failed}",
                    $"stateCount={done.Count}",
                ]));
        }

        log.WriteLine($"Done. written={result.Written} skipped={result.Skipped} failed={result.Failed}");
        return result;
    }

    private sealed class OneResult
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public static OneResult Success() => new() { Ok = true };
        public static OneResult Fail(string e) => new() { Ok = false, Error = e };
    }

    private static OneResult ImportOne(
        string zipPath,
        ImportOptions opt,
        string? shortName,
        string? revHint,
        string? idHint)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var nfoEntry = zip.Entries.FirstOrDefault(e =>
            e.Name.Equals("VERSION.NFO", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.EndsWith("/VERSION.NFO", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.EndsWith("\\VERSION.NFO", StringComparison.OrdinalIgnoreCase));

        Gb64Nfo nfo;
        if (nfoEntry is not null)
        {
            using var sr = new StreamReader(nfoEntry.Open(), Encoding.Latin1, detectEncodingFromByteOrderMarks: true);
            nfo = Gb64Nfo.Parse(sr.ReadToEnd());
        }
        else
        {
            nfo = new Gb64Nfo
            {
                UniqueId = idHint,
                GbVersion = revHint,
                Name = shortName,
            };
        }

        if (string.IsNullOrWhiteSpace(nfo.UniqueId) && idHint is not null)
            nfo.UniqueId = idHint;
        if (string.IsNullOrWhiteSpace(nfo.GbVersion) && revHint is not null)
            nfo.GbVersion = revHint;
        if (string.IsNullOrWhiteSpace(nfo.Name) && shortName is not null)
            nfo.Name = shortName;

        var media = zip.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name) && !e.FullName.EndsWith('/'))
            .Where(e => RomTagBuilder.IsMediaFile(e.Name))
            .Where(e => !e.Name.Equals("VERSION.NFO", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (media.Count == 0)
            return OneResult.Fail("no media files in zip");

        var stem = RomTagBuilder.BuildBaseStem(nfo, shortName);

        if (media.Count == 1)
        {
            var m = media[0];
            var ext = Path.GetExtension(m.Name);
            var outPath = Path.Combine(opt.RomsOut, stem + ext);
            outPath = EnsureUniquePath(outPath, isDir: false);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            ExtractEntry(m, outPath);
            // Do not hardlink SIDs into roms/c64 root (pollutes scan tree).
            // SID: paths resolve via library/hvsc.
            return OneResult.Success();
        }

        // multi-file game folder
        var folder = Path.Combine(opt.RomsOut, stem);
        folder = EnsureUniquePath(folder, isDir: true);
        Directory.CreateDirectory(folder);
        foreach (var m in media)
        {
            var name = Path.GetFileName(m.Name);
            // avoid nested paths from zip that recreate letter buckets
            var destFile = Path.Combine(folder, name);
            ExtractEntry(m, destFile);
        }
        MaybeLinkSid(nfo, opt, folder);
        return OneResult.Success();
    }

    private static void ExtractEntry(ZipArchiveEntry entry, string destPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        using var src = entry.Open();
        using var dst = File.Create(destPath);
        src.CopyTo(dst);
    }

    private static string EnsureUniquePath(string path, bool isDir)
    {
        if (isDir)
        {
            if (!Directory.Exists(path) && !File.Exists(path)) return path;
            var i = 2;
            while (Directory.Exists($"{path} ({i})") || File.Exists($"{path} ({i})"))
                i++;
            return $"{path} ({i})";
        }
        else
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path)!;
            var stem = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var i = 2;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
                i++;
            } while (File.Exists(candidate) || Directory.Exists(candidate));
            return candidate;
        }
    }

    private static void MaybeLinkSid(Gb64Nfo nfo, ImportOptions opt, string gameDir)
    {
        if (string.IsNullOrWhiteSpace(nfo.Sid) || string.IsNullOrWhiteSpace(opt.HvscRoot))
            return;
        // Prefer exact NFO path; if miss-located, resolve via HVSC index (basename / unique stem).
        var idx = SidIndexCache.Get(opt.HvscRoot);
        var rel = SidNfoFixer.ResolveActualSidPath(nfo.Sid, opt.HvscRoot, idx)
                  ?? nfo.Sid.Replace('/', '\\').Trim().TrimStart('\\');
        var src = Path.Combine(opt.HvscRoot, rel.Replace('\\', Path.DirectorySeparatorChar));
        if (!File.Exists(src)) return;
        var dest = Path.Combine(gameDir, Path.GetFileName(src));
        if (File.Exists(dest)) return;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (!CreateHardLink(dest, src, IntPtr.Zero))
                    File.Copy(src, dest);
            }
            else
            {
                File.Copy(src, dest);
            }
        }
        catch
        {
            try
            {
                if (!File.Exists(dest))
                    File.Copy(src, dest);
            }
            catch
            {
                // optional; ignore
            }
        }
    }

    /// <summary>Process-wide HVSC basename index for SID remapping during import.</summary>
    private static class SidIndexCache
    {
        private static readonly object Gate = new();
        private static string? _root;
        private static SidNfoFixer.HvscIndex? _index;

        // Expose BuildHvscIndex result type via reflection-free package-private accessor:
        // SidNfoFixer.BuildHvscIndex is internal — same assembly.
        public static SidNfoFixer.HvscIndex Get(string hvscRoot)
        {
            lock (Gate)
            {
                if (_index is not null &&
                    string.Equals(_root, hvscRoot, StringComparison.OrdinalIgnoreCase))
                    return _index;
                _root = hvscRoot;
                _index = SidNfoFixer.BuildHvscIndex(hvscRoot);
                return _index;
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
}
