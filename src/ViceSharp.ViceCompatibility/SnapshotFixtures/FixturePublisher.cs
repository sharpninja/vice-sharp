using System.Security.Cryptography;
using System.Text.Json;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// Publishes an accepted fixture: copies the accepted candidate bytes to the fixture path (only when they differ)
/// and upserts the manifest entry. Refuses a blocked classification and a candidate whose bytes do not hash to the
/// accepted hash, so nothing blocked or drifting ever reaches the repository (N21).
/// </summary>
public static class FixturePublisher
{
    /// <summary>What the publish changed.</summary>
    public sealed record PublishResult(bool FixtureChanged, bool ManifestChanged, string FixturePath, string ManifestPath);

    /// <summary>Publishes <paramref name="candidatePath"/> as <paramref name="fixturePath"/> and records <paramref name="entry"/> in the manifest at <paramref name="manifestPath"/>.</summary>
    public static PublishResult Publish(FixtureClassification classification, string candidatePath, string fixturePath, string manifestPath, FixtureManifestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(classification);
        ArgumentException.ThrowIfNullOrEmpty(candidatePath);
        ArgumentException.ThrowIfNullOrEmpty(fixturePath);
        ArgumentException.ThrowIfNullOrEmpty(manifestPath);
        ArgumentNullException.ThrowIfNull(entry);
        if (!classification.IsAccepted)
            throw new InvalidOperationException($"Fixture '{classification.FixtureId}' is {classification.Outcome}; nothing is published: {string.Join("; ", classification.Reasons)}");
        var bytes = File.ReadAllBytes(candidatePath);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(hash, classification.AcceptedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Candidate '{candidatePath}' hashes to {hash}, not the accepted {classification.AcceptedSha256}.");
        if (!string.Equals(entry.Sha256, hash, StringComparison.OrdinalIgnoreCase) || entry.Length != bytes.LongLength)
            throw new InvalidOperationException("The manifest entry's hash or length does not describe the accepted bytes.");

        var fixtureChanged = false;
        if (!File.Exists(fixturePath) || !bytes.AsSpan().SequenceEqual(File.ReadAllBytes(fixturePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fixturePath))!);
            File.WriteAllBytes(fixturePath, bytes);
            fixtureChanged = true;
        }
        var manifest = FixtureManifest.LoadOrCreate(manifestPath);
        var manifestChanged = manifest.Upsert(entry) || !File.Exists(manifestPath);
        if (manifestChanged)
            manifest.Save(manifestPath);
        return new PublishResult(fixtureChanged, manifestChanged, fixturePath, manifestPath);
    }

    /// <summary>Writes a candidate as JSON (the per-host receipt the classifier reads back).</summary>
    public static void SaveCandidate(FixtureCandidate candidate, string path)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(candidate, FixtureManifestJsonContext.Default.FixtureCandidate) + "\n");
    }

    /// <summary>Reads a candidate receipt written by <see cref="SaveCandidate"/>.</summary>
    public static FixtureCandidate LoadCandidate(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, FixtureManifestJsonContext.Default.FixtureCandidate)
            ?? throw new InvalidDataException($"Candidate receipt '{path}' is empty.");
    }

    /// <summary>Writes a classification as JSON.</summary>
    public static void SaveClassification(FixtureClassification classification, string path)
    {
        ArgumentNullException.ThrowIfNull(classification);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(classification, FixtureManifestJsonContext.Default.FixtureClassification) + "\n");
    }
}
