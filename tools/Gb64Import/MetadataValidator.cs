using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Gb64Import;

/// <summary>
/// Validates that on-disk RomM names match GB64 VERSION.NFO fields per
/// docs/gb64-romm-tag-mapping.md default emit profile.
/// </summary>
public static class MetadataValidator
{
    private static readonly Dictionary<string, string> LanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["English"] = "En",
        ["German"] = "De",
        ["Italian"] = "It",
        ["Spanish"] = "Es",
        ["Dutch"] = "Nl",
        ["French"] = "Fr",
        ["Finnish"] = "Fi",
        ["Swedish"] = "Sv",
        ["Norwegian"] = "No",
        ["Polish"] = "Pl",
        ["Danish"] = "Da",
        ["Arabic"] = "Ar",
        ["Hungarian"] = "reg-Hungarian",
        ["Czech"] = "reg-Czech",
        ["Slovenian"] = "reg-Slovenian",
        ["Serbo-Croatian"] = "Sr",
        ["(No Text)"] = "nolang",
        ["No Text"] = "nolang",
    };

    public sealed class ValidateOptions
    {
        public required string GamesRoot { get; init; }
        public required string RomsOut { get; init; }
        public int Limit { get; init; }
        public int MaxDegreeOfParallelism { get; init; } = Math.Max(2, Environment.ProcessorCount / 2);
        public int MaxMismatchesToReport { get; init; } = 40;
    }

    public sealed class ValidateResult
    {
        public int Checked;
        public int Ok;
        public int Mismatch;
        public int MissingOnDisk;
        public int NoNfo;
        public int NoMedia;
        public List<string> Samples { get; } = new();
        public Dictionary<string, int> FailReasons { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static ValidateResult Run(ValidateOptions opt, TextWriter? log = null)
    {
        log ??= Console.Error;
        var result = new ValidateResult();

        // Index on-disk game units by gb64 id.
        // Do NOT skip names that merely start with '.' (legitimate titles like "...sein letzter Trick").
        var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Directory.EnumerateFileSystemEntries(opt.RomsOut))
        {
            var name = Path.GetFileName(entry);
            if (name.Equals(".gb64-import-state.txt", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith(".gb64", StringComparison.OrdinalIgnoreCase))
                continue;
            var m = Regex.Match(name, @"\(gb64-(\d+)\)", RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            byId[m.Groups[1].Value] = entry;
        }
        log.WriteLine($"Indexed {byId.Count} on-disk game units under {opt.RomsOut}");

        var zips = Directory.EnumerateFiles(opt.GamesRoot, "*.zip", SearchOption.AllDirectories)
            .OrderBy(z => z, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (opt.Limit > 0)
            zips = zips.Take(opt.Limit).ToList();

        log.WriteLine($"Validating metadata for {zips.Count} zips...");

        var mismatches = new ConcurrentBag<string>();
        var reasons = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(zips, new ParallelOptions { MaxDegreeOfParallelism = opt.MaxDegreeOfParallelism }, zipPath =>
        {
            Interlocked.Increment(ref result.Checked);
            try
            {
                var (idHint, revHint, shortName) = RomTagBuilder.ParseZipStem(Path.GetFileName(zipPath));
                using var zip = ZipFile.OpenRead(zipPath);
                var nfoEntry = zip.Entries.FirstOrDefault(e =>
                    e.Name.Equals("VERSION.NFO", StringComparison.OrdinalIgnoreCase));

                Gb64Nfo nfo;
                if (nfoEntry is not null)
                {
                    using var sr = new StreamReader(nfoEntry.Open(), Encoding.Latin1, detectEncodingFromByteOrderMarks: true);
                    nfo = Gb64Nfo.Parse(sr.ReadToEnd());
                }
                else
                {
                    Interlocked.Increment(ref result.NoNfo);
                    nfo = new Gb64Nfo();
                }

                if (string.IsNullOrWhiteSpace(nfo.UniqueId) && idHint is not null)
                    nfo.UniqueId = idHint;
                if (string.IsNullOrWhiteSpace(nfo.GbVersion) && revHint is not null)
                    nfo.GbVersion = revHint;
                if (string.IsNullOrWhiteSpace(nfo.Name) && shortName is not null)
                    nfo.Name = shortName;

                var mediaCount = zip.Entries.Count(e =>
                    !string.IsNullOrEmpty(e.Name) &&
                    !e.FullName.EndsWith('/') &&
                    RomTagBuilder.IsMediaFile(e.Name) &&
                    !e.Name.Equals("VERSION.NFO", StringComparison.OrdinalIgnoreCase));

                if (mediaCount == 0)
                {
                    Interlocked.Increment(ref result.NoMedia);
                    Bump(reasons, "no-media-in-zip");
                    return;
                }

                var id = nfo.UniqueId?.Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    Record(mismatches, reasons, result, opt, $"{Path.GetFileName(zipPath)}: no Unique-ID");
                    Bump(reasons, "no-unique-id");
                    return;
                }

                // Strip leading zeros for lookup flexibility (disk uses NFO id as-is)
                if (!byId.TryGetValue(id, out var diskPath) &&
                    !byId.TryGetValue(id.TrimStart('0'), out diskPath))
                {
                    Interlocked.Increment(ref result.MissingOnDisk);
                    Record(mismatches, reasons, result, opt,
                        $"{Path.GetFileName(zipPath)}: missing on disk (gb64-{id})");
                    Bump(reasons, "missing-on-disk");
                    return;
                }

                var diskName = Path.GetFileName(diskPath);
                // Folder name is the full stem; file name is stem + extension.
                // Do NOT strip trailing " (NNNN)" — crack group codes are often pure digits
                // (e.g. (3532), (5211)) and must remain for mapping fidelity.
                var diskStem = Directory.Exists(diskPath)
                    ? diskName
                    : Path.GetFileNameWithoutExtension(diskName);

                var expectedStem = RomTagBuilder.BuildBaseStem(nfo, shortName);
                // Allow rare EnsureUniquePath collisions: "stem (2)" only when expected itself
                // does not already end with that form as a crack code we intentionally emit.
                if (!string.Equals(diskStem, expectedStem, StringComparison.Ordinal) &&
                    Regex.IsMatch(diskStem, @" \(\d\)$") &&
                    diskStem.StartsWith(expectedStem + " (", StringComparison.Ordinal))
                {
                    diskStem = expectedStem; // treat "stem (2)" collision as matching expected
                }

                var fieldErrors = new List<string>();

                // --- Mapping-rule field checks (independent of full-string equality) ---
                // Language
                if (!string.IsNullOrWhiteSpace(nfo.Language))
                {
                    foreach (var part in nfo.Language.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (part.Equals("(None)", StringComparison.OrdinalIgnoreCase)) continue;
                        string tag;
                        if (LanguageMap.TryGetValue(part, out var code))
                            tag = $"({code})";
                        else
                            tag = $"(reg-{Regex.Replace(part, @"\s+", "")})";
                        if (!diskStem.Contains(tag, StringComparison.Ordinal))
                            fieldErrors.Add($"lang missing {tag}");
                    }
                }

                // Pal/NTSC
                if (!string.IsNullOrWhiteSpace(nfo.PalNtsc))
                {
                    var p = nfo.PalNtsc.Trim();
                    if (p.Equals("PAL", StringComparison.OrdinalIgnoreCase))
                    {
                        Require(diskStem, "(E)", fieldErrors, "PAL");
                        Require(diskStem, "(PAL)", fieldErrors, "PAL");
                    }
                    else if (p.Equals("NTSC", StringComparison.OrdinalIgnoreCase))
                    {
                        Require(diskStem, "(U)", fieldErrors, "NTSC");
                        Require(diskStem, "(NTSC)", fieldErrors, "NTSC");
                    }
                    else if (p.Contains("PAL", StringComparison.OrdinalIgnoreCase) &&
                             p.Contains("NTSC", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Contains('?'))
                        {
                            Require(diskStem, "(E)", fieldErrors, "PAL(+NTSC?)");
                            Require(diskStem, "(PAL)", fieldErrors, "PAL(+NTSC?)");
                            Require(diskStem, "(NTSC-maybe)", fieldErrors, "PAL(+NTSC?)");
                        }
                        else
                        {
                            Require(diskStem, "(W)", fieldErrors, "PAL+NTSC");
                            Require(diskStem, "(PAL)", fieldErrors, "PAL+NTSC");
                            Require(diskStem, "(NTSC)", fieldErrors, "PAL+NTSC");
                        }
                    }
                }

                // rev
                var rev = RomTagBuilder.NormalizeRev(nfo.GbVersion);
                if (rev is not null)
                    Require(diskStem, $"(rev-{rev})", fieldErrors, "rev");

                // gb64 id
                Require(diskStem, $"(gb64-{id})", fieldErrors, "gb64-id");

                // TrueDrive
                if (string.Equals(nfo.TrueDriveEmul, "Yes", StringComparison.OrdinalIgnoreCase))
                    Require(diskStem, "(TrueDrive)", fieldErrors, "TrueDrive");
                else if (!string.IsNullOrWhiteSpace(nfo.TrueDriveEmul) &&
                         nfo.TrueDriveEmul.Equals("No", StringComparison.OrdinalIgnoreCase) &&
                         diskStem.Contains("(TrueDrive)", StringComparison.Ordinal))
                    fieldErrors.Add("TrueDrive present but NFO=No");

                // Trainers
                if (int.TryParse(nfo.Trainers?.Trim(), out var trainers))
                {
                    if (trainers > 0)
                        Require(diskStem, $"(Trainers-{trainers})", fieldErrors, "Trainers");
                    else if (Regex.IsMatch(diskStem, @"\(Trainers-\d+\)"))
                        fieldErrors.Add("Trainers tag present but NFO=0");
                }

                // Cracked
                var crackedActive = !string.IsNullOrWhiteSpace(nfo.CrackedCrunched) &&
                    nfo.CrackedCrunched.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(p => !p.Equals("(None)", StringComparison.OrdinalIgnoreCase));
                if (crackedActive)
                    Require(diskStem, "(Cracked)", fieldErrors, "Cracked");
                else if (diskStem.Contains("(Cracked)", StringComparison.Ordinal) &&
                         (string.IsNullOrWhiteSpace(nfo.CrackedCrunched) ||
                          nfo.CrackedCrunched.Trim().Equals("(None)", StringComparison.OrdinalIgnoreCase)))
                    fieldErrors.Add("Cracked tag present but NFO=(None)");

                // Name (sanitized) should be the base before first " ("
                var expectedName = RomTagBuilder.SanitizeName(
                    !string.IsNullOrWhiteSpace(nfo.Name) ? nfo.Name! : (shortName ?? "Unknown"));
                if (!diskStem.StartsWith(expectedName, StringComparison.Ordinal) &&
                    !diskStem.StartsWith(expectedName.TrimEnd('.'), StringComparison.Ordinal))
                {
                    // Allow if expected name was only punctuation and became Unknown
                    fieldErrors.Add($"name base want '{expectedName}'");
                }

                // Full stem equality (primary fidelity check)
                var stemOk = string.Equals(diskStem, expectedStem, StringComparison.Ordinal);
                // Also accept diskStem with trailing " (N)" collision already stripped

                if (!stemOk || fieldErrors.Count > 0)
                {
                    var detail = stemOk
                        ? string.Join("; ", fieldErrors)
                        : $"stem mismatch want=[{expectedStem}] got=[{diskStem}]" +
                          (fieldErrors.Count > 0 ? " | " + string.Join("; ", fieldErrors) : "");
                    if (!stemOk) Bump(reasons, "stem-mismatch");
                    foreach (var fe in fieldErrors)
                    {
                        var key = fe.Split(' ')[0];
                        Bump(reasons, "field:" + key);
                    }
                    Record(mismatches, reasons, result, opt,
                        $"{Path.GetFileName(zipPath)} / gb64-{id}: {detail}");
                }
                else
                {
                    Interlocked.Increment(ref result.Ok);
                }
            }
            catch (Exception ex)
            {
                Bump(reasons, "exception");
                Record(mismatches, reasons, result, opt, $"{Path.GetFileName(zipPath)}: {ex.Message}");
            }
        });

        result.Samples.AddRange(mismatches.Take(opt.MaxMismatchesToReport));
        foreach (var kv in reasons.OrderByDescending(k => k.Value))
            result.FailReasons[kv.Key] = kv.Value;

        log.WriteLine(
            $"Done. checked={result.Checked} ok={result.Ok} mismatch={result.Mismatch} " +
            $"missing={result.MissingOnDisk} noNfo={result.NoNfo} noMedia={result.NoMedia}");
        if (result.FailReasons.Count > 0)
        {
            log.WriteLine("Fail reason tallies:");
            foreach (var kv in result.FailReasons.OrderByDescending(k => k.Value).Take(20))
                log.WriteLine($"  {kv.Value,6}  {kv.Key}");
        }
        return result;
    }

    private static void Require(string diskStem, string tag, List<string> errors, string label)
    {
        if (!diskStem.Contains(tag, StringComparison.Ordinal))
            errors.Add($"{label} missing {tag}");
    }

    private static void Bump(ConcurrentDictionary<string, int> reasons, string key) =>
        reasons.AddOrUpdate(key, 1, (_, n) => n + 1);

    private static void Record(
        ConcurrentBag<string> bag,
        ConcurrentDictionary<string, int> reasons,
        ValidateResult result,
        ValidateOptions opt,
        string message)
    {
        Interlocked.Increment(ref result.Mismatch);
        if (bag.Count < opt.MaxMismatchesToReport)
            bag.Add(message);
    }
}
