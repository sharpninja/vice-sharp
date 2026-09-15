using System.Text;
using System.Text.RegularExpressions;

namespace Gb64Import;

public static class RomTagBuilder
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

    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".d64", ".g64", ".d71", ".d81", ".d82", ".t64", ".tap", ".crt", ".prg", ".p00",
        ".bin", ".raw", ".vsf", ".reu", ".lnx", ".ark", ".sda", ".sfx", ".zip"
    };

    public static bool IsMediaFile(string name)
    {
        var ext = Path.GetExtension(name);
        return MediaExtensions.Contains(ext);
    }

    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name.Trim())
        {
            if (ch is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
                sb.Append('-');
            else if (char.IsControl(ch))
                continue;
            else
                sb.Append(ch);
        }
        var s = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        s = s.TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(s) ? "Unknown" : s;
    }

    public static string BuildBaseStem(Gb64Nfo nfo, string? zipStemFallback = null)
    {
        var name = !string.IsNullOrWhiteSpace(nfo.Name) ? nfo.Name! : (zipStemFallback ?? "Unknown");
        var tags = new List<string>();

        // Languages
        if (!string.IsNullOrWhiteSpace(nfo.Language))
        {
            foreach (var part in nfo.Language.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (LanguageMap.TryGetValue(part, out var code))
                    tags.Add($"({code})");
                else if (!string.IsNullOrWhiteSpace(part) &&
                         !part.Equals("(None)", StringComparison.OrdinalIgnoreCase))
                {
                    var token = Regex.Replace(part, @"\s+", "");
                    tags.Add($"(reg-{token})");
                }
            }
        }

        // Video / region
        if (!string.IsNullOrWhiteSpace(nfo.PalNtsc))
        {
            var p = nfo.PalNtsc.Trim();
            if (p.Equals("PAL", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("(E)"); tags.Add("(PAL)");
            }
            else if (p.Equals("NTSC", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("(U)"); tags.Add("(NTSC)");
            }
            else if (p.Contains("PAL", StringComparison.OrdinalIgnoreCase) &&
                     p.Contains("NTSC", StringComparison.OrdinalIgnoreCase))
            {
                if (p.Contains('?', StringComparison.Ordinal))
                {
                    tags.Add("(E)"); tags.Add("(PAL)"); tags.Add("(NTSC-maybe)");
                }
                else
                {
                    tags.Add("(W)"); tags.Add("(PAL)"); tags.Add("(NTSC)");
                }
            }
        }

        // Revision from GB-Version
        var rev = NormalizeRev(nfo.GbVersion);
        if (rev is not null)
            tags.Add($"(rev-{rev})");

        // Stable id
        var id = nfo.UniqueId;
        if (string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(nfo.Filename))
        {
            // Filename: 0\NAME_12345_01.zip
            var m = Regex.Match(nfo.Filename!, @"_(\d+)_(\d+)\.zip$", RegexOptions.IgnoreCase);
            if (m.Success) id = m.Groups[1].Value;
        }
        if (!string.IsNullOrWhiteSpace(id))
            tags.Add($"(gb64-{id.Trim()})");

        // Optional flags from VERSION INFO
        if (!string.IsNullOrWhiteSpace(nfo.Trainers) &&
            int.TryParse(nfo.Trainers.Trim(), out var t) && t > 0)
            tags.Add($"(Trainers-{t})");

        if (!string.IsNullOrWhiteSpace(nfo.TrueDriveEmul) &&
            nfo.TrueDriveEmul.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            tags.Add("(TrueDrive)");

        // Cracked/Crunched: first non-(None) group → (Cracked) + optional (CODE)
        if (!string.IsNullOrWhiteSpace(nfo.CrackedCrunched))
        {
            foreach (var part in nfo.CrackedCrunched.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Equals("(None)", StringComparison.OrdinalIgnoreCase))
                    continue;
                tags.Add("(Cracked)");
                var code = Regex.Match(part, @"\(([A-Za-z0-9]{2,6})\)\s*$");
                if (code.Success)
                    tags.Add($"({code.Groups[1].Value})");
                else
                {
                    var group = SanitizeName(part);
                    if (!string.IsNullOrWhiteSpace(group) && group.Length <= 24)
                        tags.Add($"({group.Replace(' ', '-')})");
                }
                break;
            }
        }

        var stem = SanitizeName(name);
        if (tags.Count == 0) return stem;
        return stem + " " + string.Join(" ", tags);
    }

    public static string? NormalizeRev(string? gbVersion)
    {
        if (string.IsNullOrWhiteSpace(gbVersion)) return null;
        var digits = Regex.Match(gbVersion.Trim(), @"\d+");
        if (!digits.Success) return null;
        if (!int.TryParse(digits.Value, out var n)) return null;
        return n.ToString("D2");
    }

    /// <summary>
    /// Parse ZIP stem NAME_ID_VER.zip for fallback id/rev.
    /// </summary>
    public static (string? Id, string? Rev, string? ShortName) ParseZipStem(string zipFileName)
    {
        var stem = Path.GetFileNameWithoutExtension(zipFileName);
        var m = Regex.Match(stem, @"^(.*)_(\d+)_(\d+)$");
        if (!m.Success) return (null, null, stem);
        return (m.Groups[2].Value, m.Groups[3].Value, m.Groups[1].Value);
    }
}
