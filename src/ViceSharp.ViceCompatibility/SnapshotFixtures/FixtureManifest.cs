using System.Text.Json;
using System.Text.Json.Nodes;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The schema-1 state-transfer manifest view. The publisher owns fixture entries selected by path.
/// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17: retain the complete loaded document so publishing preserves
/// reader and supplied-fixture metadata supporting TR-VSFLOCKSTEP-RESUME-001 AC1 and TEST-VSFLOCKSTEP-001 AC1.
/// </summary>
public sealed class FixtureManifest
{
    /// <summary>The pinned VICE commit every fixture was generated from.</summary>
    public const string PinnedViceCommit = "cc67418cba58e71d8e70fd353e9de47199ad2075";
    /// <summary>Version of the generator that wrote the entries.</summary>
    public const string GeneratorVersion = "1.0.0";

    public int SchemaVersion { get; set; } = 1;
    public string ViceCommit { get; set; } = PinnedViceCommit;
    public string FixtureGeneratorVersion { get; set; } = GeneratorVersion;
    public ContainerFraming ContainerFraming { get; set; } = new();
    public string[] RomAllowlist { get; set; } = [.. SnapshotFixtures.RomAllowlist.BaseFixtureSubset.Values.Order(StringComparer.Ordinal)];
    public List<FixtureManifestEntry> Fixtures { get; set; } = [];
    public JsonElement? Machines { get; set; }

    private JsonObject? _loadedDocument;

    /// <summary>Loads the manifest at <paramref name="path"/>, or a new skeleton when the file does not exist.</summary>
    public static FixtureManifest LoadOrCreate(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            return new FixtureManifest();
        using var stream = File.OpenRead(path);
        if (JsonNode.Parse(stream) is not JsonObject document)
            throw new InvalidDataException($"Manifest '{path}' must contain a JSON object.");
        var manifest = document.Deserialize(FixtureManifestJsonContext.Default.FixtureManifest)
            ?? throw new InvalidDataException($"Manifest '{path}' is empty.");
        manifest._loadedDocument = document;
        return manifest;
    }

    /// <summary>Replaces the entry with the same path or appends; returns true when the manifest content changed.</summary>
    public bool Upsert(FixtureManifestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var index = Fixtures.FindIndex(f => string.Equals(f.Path, entry.Path, StringComparison.Ordinal));
        if (index < 0)
        {
            UpdateLoadedFixture(entry, index);
            Fixtures.Add(entry);
            return true;
        }
        var existing = Fixtures[index];
        if (Serialize(existing) == Serialize(entry))
            return false;
        UpdateLoadedFixture(entry, index);
        Fixtures[index] = entry;
        return true;
    }

    /// <summary>Writes the manifest as indented JSON, preserving the loaded document outside entries changed by <see cref="Upsert"/>.</summary>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var json = _loadedDocument?.ToJsonString(FixtureManifestJsonContext.Default.Options)
            ?? JsonSerializer.Serialize(this, FixtureManifestJsonContext.Default.FixtureManifest);
        File.WriteAllText(path, json + "\n");
    }

    /// <summary>Updates only publisher-owned fields, retaining any existing auxiliary fixture fields (D5b/R17).</summary>
    private void UpdateLoadedFixture(FixtureManifestEntry entry, int index)
    {
        if (_loadedDocument is null)
            return;
        var updated = JsonSerializer.SerializeToNode(entry, FixtureManifestJsonContext.Default.FixtureManifestEntry)!.AsObject();
        if (_loadedDocument["fixtures"] is not JsonArray rows)
            _loadedDocument["fixtures"] = rows = new JsonArray();
        if (index < 0)
        {
            rows.Add((JsonNode)updated);
            return;
        }
        var existing = rows[index]!.AsObject();
        foreach (var (name, value) in updated)
            existing[name] = value?.DeepClone();
    }

    private static string Serialize(FixtureManifestEntry entry) => JsonSerializer.Serialize(entry, FixtureManifestJsonContext.Default.FixtureManifestEntry);
}
