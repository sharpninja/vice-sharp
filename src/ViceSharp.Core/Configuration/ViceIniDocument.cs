using System.Text;

namespace ViceSharp.Core.Configuration;

/// <summary>
/// Custom reader/writer for the Classic VICE INI format (vice.ini) and the
/// ViceSharp-only companion (vice-sharp.ini). The format is a sequence of
/// <c>[Section]</c> blocks (the <c>[Version]</c> header plus one per machine,
/// e.g. <c>[C64SC]</c>) whose entries are <c>Resource=value</c> lines: integer
/// resources are written bare (<c>VICIIModel=3</c>) and string resources are
/// double-quoted (<c>WIC64MACAddress="08:d1:f9:0a:0c:0e"</c>).
///
/// This is the storage primitive beneath the IConfiguration layer. Because
/// ViceSharp edits the user's <em>shared</em> VICE config, the document
/// preserves every section, resource, order, and quoting it parsed: a
/// read-modify-write round-trips losslessly and never drops resources ViceSharp
/// does not itself manage.
/// </summary>
public sealed class ViceIniDocument
{
    private sealed class IniEntry
    {
        public required string Key { get; init; }
        public string Value { get; set; } = string.Empty;
        public bool Quoted { get; set; }
    }

    private sealed class IniSection
    {
        public required string Name { get; init; }
        public List<IniEntry> Entries { get; } = [];
    }

    private readonly List<IniSection> _sections = [];

    /// <summary>Section names in document order.</summary>
    public IReadOnlyList<string> Sections => _sections.ConvertAll(s => s.Name);

    /// <summary>Parse VICE INI text into an editable, round-trippable document.</summary>
    public static ViceIniDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var document = new ViceIniDocument();
        IniSection? current = null;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
                continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                var name = line[1..^1].Trim();
                current = document.GetOrAddSection(name);
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0 || current is null)
                continue;

            var key = line[..eq].Trim();
            var rawValue = line[(eq + 1)..];
            var quoted = rawValue.Length >= 2 && rawValue[0] == '"' && rawValue[^1] == '"';
            var value = quoted ? rawValue[1..^1] : rawValue;

            current.Entries.Add(new IniEntry { Key = key, Value = value, Quoted = quoted });
        }

        return document;
    }

    /// <summary>Get a resource value (quotes stripped), or null if section/key is absent.</summary>
    public string? Get(string section, string key)
    {
        var entry = FindSection(section)?.Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal));
        return entry?.Value;
    }

    /// <summary>
    /// Set a resource value, updating an existing entry in place or appending a
    /// new one (creating the section if needed). <paramref name="quote"/>: true
    /// forces double-quoting (string resource), false forces bare; null (the
    /// default) preserves the existing entry's quoting on update and writes bare
    /// for a new entry. Preserving on update is what lets a read-modify-write
    /// round-trip a shared vice.ini without unquoting its string resources.
    /// </summary>
    public void Set(string section, string key, string value, bool? quote = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        var target = GetOrAddSection(section);
        var entry = target.Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal));
        if (entry is null)
        {
            target.Entries.Add(new IniEntry { Key = key, Value = value, Quoted = quote ?? false });
            return;
        }

        entry.Value = value;
        if (quote.HasValue)
            entry.Quoted = quote.Value;
    }

    /// <summary>Remove a resource. Returns true if it existed.</summary>
    public bool Remove(string section, string key)
    {
        var target = FindSection(section);
        return target is not null && target.Entries.RemoveAll(e => string.Equals(e.Key, key, StringComparison.Ordinal)) > 0;
    }

    /// <summary>Resources of a section in order, with quotes stripped.</summary>
    public IReadOnlyList<(string Key, string Value)> Entries(string section)
    {
        var target = FindSection(section);
        return target is null ? [] : target.Entries.ConvertAll(e => (e.Key, e.Value));
    }

    /// <summary>Serialize back to VICE INI text (sections in order, blank line between each).</summary>
    public string ToIniString()
    {
        var builder = new StringBuilder();
        foreach (var section in _sections)
        {
            builder.Append('[').Append(section.Name).Append(']').Append('\n');
            foreach (var entry in section.Entries)
            {
                builder.Append(entry.Key).Append('=');
                if (entry.Quoted)
                    builder.Append('"').Append(entry.Value).Append('"');
                else
                    builder.Append(entry.Value);
                builder.Append('\n');
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    public override string ToString() => ToIniString();

    private IniSection? FindSection(string section) =>
        _sections.FirstOrDefault(s => string.Equals(s.Name, section, StringComparison.Ordinal));

    private IniSection GetOrAddSection(string section)
    {
        var existing = FindSection(section);
        if (existing is not null)
            return existing;

        var created = new IniSection { Name = section };
        _sections.Add(created);
        return created;
    }
}
