using System.Text.Json;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The schema-1 state-transfer manifest skeleton the publisher maintains: fixtures are upserted by path; the
/// machines section is owned by D5 and is carried through untouched as raw JSON.
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

    /// <summary>Loads the manifest at <paramref name="path"/>, or a new skeleton when the file does not exist.</summary>
    public static FixtureManifest LoadOrCreate(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            return new FixtureManifest();
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, FixtureManifestJsonContext.Default.FixtureManifest)
            ?? throw new InvalidDataException($"Manifest '{path}' is empty.");
    }

    /// <summary>Replaces the entry with the same path or appends; returns true when the manifest content changed.</summary>
    public bool Upsert(FixtureManifestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var index = Fixtures.FindIndex(f => string.Equals(f.Path, entry.Path, StringComparison.Ordinal));
        if (index < 0)
        {
            Fixtures.Add(entry);
            return true;
        }
        var existing = Fixtures[index];
        if (Serialize(existing) == Serialize(entry))
            return false;
        Fixtures[index] = entry;
        return true;
    }

    /// <summary>Writes the manifest as indented JSON.</summary>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, FixtureManifestJsonContext.Default.FixtureManifest) + "\n");
    }

    private static string Serialize(FixtureManifestEntry entry) => JsonSerializer.Serialize(entry, FixtureManifestJsonContext.Default.FixtureManifestEntry);
}
