using System.Text.Json;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The committed list of open fixture blockers (docs/requirements/oracles/vice-vsf-fixture-blockers.json): the
/// classify step records a blocked fixture here and removes it once accepted; the real-bound Slice 1 tests read
/// it to fail with the BLOCKED prefix and the measured clocks.
/// </summary>
public static class FixtureBlockers
{
    /// <summary>Loads the list, or an empty list when the file does not exist.</summary>
    public static List<FixtureBlockerRecord> Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            return [];
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, FixtureManifestJsonContext.Default.ListFixtureBlockerRecord) ?? [];
    }

    /// <summary>Records <paramref name="classification"/>: upserts a blocked fixture, removes an accepted one; writes the file.</summary>
    public static void Apply(string path, FixtureClassification classification, string blocker, string recordedAt)
    {
        ArgumentNullException.ThrowIfNull(classification);
        var list = Load(path);
        list.RemoveAll(b => string.Equals(b.FixtureId, classification.FixtureId, StringComparison.Ordinal));
        if (!classification.IsAccepted)
        {
            list.Add(new FixtureBlockerRecord
            {
                FixtureId = classification.FixtureId, Blocker = blocker, RecordedAt = recordedAt,
                Reasons = [.. classification.Reasons], Clocks = [.. classification.Clocks],
            });
        }
        list.Sort((a, b) => string.CompareOrdinal(a.FixtureId, b.FixtureId));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(list, FixtureManifestJsonContext.Default.ListFixtureBlockerRecord) + "\n");
    }

    /// <summary>The BLOCKED message a real-bound test fails with for <paramref name="record"/>.</summary>
    public static string Message(FixtureBlockerRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var clocks = string.Join("; ", record.Clocks.Select(c => $"{c.Host}: trigger {c.Trigger}, write clock {(c.WriteClock?.ToString() ?? "none")}, writer result {c.WriterResult}, MAINCPU clock {(c.MainCpuClock?.ToString() ?? "none")}"));
        return $"BLOCKED: {record.Blocker} ({record.FixtureId}, recorded {record.RecordedAt}): {string.Join("; ", record.Reasons)}; clocks: {clocks}";
    }
}
