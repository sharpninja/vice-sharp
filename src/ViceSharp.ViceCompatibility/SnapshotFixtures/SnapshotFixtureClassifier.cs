using System.Security.Cryptography;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (R12, round 5 N14): classifies the candidates of one fixture across runs and hosts.
/// Accepted only when every required host contributed at least the required number of runs, every candidate is an
/// accepted candidate (write clock and MAINCPU clock equal to the trigger), and every candidate hash is the same.
/// A one-host mismatch, a both-host mismatch, a differing hash or a missing host yields BLOCKED with the measured
/// clocks of every host; never a pass and never a skip.
/// </summary>
public static class SnapshotFixtureClassifier
{
    /// <summary>The named blocker of a write clock that is not the literal trigger.</summary>
    public const string BlockerId = "BLOCKER-R12-XVIC-CHECKPOINT";

    /// <summary>Classifies <paramref name="candidates"/> for <paramref name="fixtureId"/> at <paramref name="trigger"/>.</summary>
    public static FixtureClassification Classify(string fixtureId, ulong trigger, IReadOnlyList<FixtureCandidate> candidates, IReadOnlyList<string> requiredHosts, int runsPerHost = 2)
        => Classify(fixtureId, trigger, candidates, requiredHosts, runsPerHost, null, null);

    /// <summary>
    /// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17. Verifies the retained raw xvic bytes and, only for the
    /// pinned PAL and NTSC cohorts at 100002, allows differences in VIA1 v2.2 ILA and ILB when both are disabled.
    /// Stock VICE at this pin omits MYVIA_NEED_LATCHING, yet its writer unconditionally saves both fields.
    /// </summary>
    public static FixtureClassification ClassifyPinnedXvic(string fixtureId, ulong trigger,
        IReadOnlyList<(FixtureCandidate Receipt, byte[]? Bytes)> runs, IReadOnlyList<string> requiredHosts, string viceCommit)
        => Classify(fixtureId, trigger, runs.Select(run => run.Receipt).ToArray(), requiredHosts, 2,
            runs.Select(run => run.Bytes).ToArray(), viceCommit);

    private static FixtureClassification Classify(string fixtureId, ulong trigger, IReadOnlyList<FixtureCandidate> candidates,
        IReadOnlyList<string> requiredHosts, int runsPerHost, IReadOnlyList<byte[]?>? retained, string? viceCommit)
    {
        ArgumentException.ThrowIfNullOrEmpty(fixtureId);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(requiredHosts);
        var reasons = new List<string>();
        var clocks = candidates.Select(c => new HostClock(c.Host, c.Trigger, c.WriteClock, c.WriterResult, c.MainCpuClock, c.CandidateSha256, c.Outcome, c.Reason)).ToArray();

        foreach (var host in requiredHosts)
        {
            var count = candidates.Count(c => string.Equals(c.Host, host, StringComparison.OrdinalIgnoreCase));
            if (count < runsPerHost)
                reasons.Add($"host {host}: {count} candidate(s), {runsPerHost} fresh-command runs required");
        }
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c.FixtureId != fixtureId)
                reasons.Add($"host {c.Host}: candidate belongs to fixture '{c.FixtureId}', not '{fixtureId}'");
            if (c.Trigger != trigger)
                reasons.Add($"host {c.Host}: trigger {c.Trigger} is not the required {trigger}");
            if (c.ExitCode != FixtureCandidate.AcceptedExit)
                reasons.Add($"host {c.Host}: {c.Outcome}: {c.Reason}");
            else if (c.WriteClock != trigger || c.MainCpuClock != trigger)
                reasons.Add($"host {c.Host}: {BlockerId}: write clock {Format(c.WriteClock)}, MAINCPU clock {Format(c.MainCpuClock)}, trigger {trigger}");
            if (retained is not null && c.ExitCode == FixtureCandidate.AcceptedExit)
            {
                var bytes = retained[i];
                if (bytes is null || bytes.LongLength != c.CandidateLength ||
                    !string.Equals(c.CandidateSha256, Convert.ToHexString(SHA256.HashData(bytes)), StringComparison.OrdinalIgnoreCase))
                    reasons.Add($"host {c.Host}: retained bytes do not match the raw candidate hash and length");
                else if (c.WriterResult != 0 || !HasMainCpuClock(bytes, trigger))
                    reasons.Add($"host {c.Host}: retained candidate does not prove a successful writer and MAINCPU at {trigger}");
            }
        }
        var hashes = candidates.Select(c => c.CandidateSha256).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var model = fixtureId switch
        {
            "xvic-pal-unexpanded" => "vic20",
            "xvic-ntsc-unexpanded" => "vic20ntsc",
            _ => null,
        };
        var latchException = hashes.Length > 1 && reasons.Count == 0 && retained is not null &&
            viceCommit == FixtureManifest.PinnedViceCommit && trigger == 100_002 && model is not null &&
            candidates.Count == 4 && candidates.Count(c => c.Host == "LAB-OMARCHY") == 2 &&
            candidates.Count(c => c.Host == "PAYTON-LEGION2") == 2 &&
            candidates.All(c => !string.IsNullOrEmpty(c.CandidatePath)) &&
            candidates.Select(c => c.CandidatePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 4 &&
            candidates.All(c => c.Machine == "xvic" && c.Model == model) && MatchExceptUnusedVia1Latches(retained);
        if (hashes.Length > 1 && !latchException)
            reasons.Add("candidate bytes differ across runs or hosts: " + string.Join(", ", candidates.Select(c => c.Host + "=" + (c.CandidateSha256 ?? "absent"))));
        if (hashes.Length == 0 || (hashes.Length == 1 && hashes[0] is null))
            reasons.Add("no candidate bytes exist on any host");

        if (reasons.Count > 0)
            return new FixtureClassification(fixtureId, FixtureClassification.Blocked, null, null, reasons, clocks);
        var accepted = candidates[0];
        return new FixtureClassification(fixtureId, FixtureClassification.Accepted, accepted.CandidateSha256, accepted.CandidateLength, [], clocks)
        {
            ByteComparisonException = latchException ? "VIA1 v2.2 payload offsets 23 (ILA) and 24 (ILB)" : null,
        };
    }

    /// <summary>2026-11-23, D5b/R17. Verify the receipt's clock directly in the original snapshot.</summary>
    private static bool HasMainCpuClock(ReadOnlySpan<byte> snapshot, ulong trigger)
    {
        try { return X64ScNativeLaunch.ReadMainCpu(snapshot).Clock == trigger; }
        catch (InvalidDataException) { return false; }
    }

    /// <summary>
    /// 2026-11-23, D5b/R17. Compare the original bytes on each side of ILA and ILB without rewriting either file.
    /// </summary>
    private static bool MatchExceptUnusedVia1Latches(IReadOnlyList<byte[]?> snapshots)
    {
        try
        {
            var first = snapshots[0].AsSpan();
            var offset = UnusedVia1LatchOffset(first);
            if (offset < 0) return false;
            for (var i = 1; i < snapshots.Count; i++)
            {
                var next = snapshots[i].AsSpan();
                if (next.Length != first.Length || UnusedVia1LatchOffset(next) != offset ||
                    !first[..offset].SequenceEqual(next[..offset]) ||
                    !first[(offset + 2)..].SequenceEqual(next[(offset + 2)..]))
                    return false;
            }
            return true;
        }
        catch (InvalidDataException) { return false; }
    }

    /// <summary>2026-11-23, D5b/R17. Locate the two fields only in the expected pinned VIC-20 framing.</summary>
    private static int UnusedVia1LatchOffset(ReadOnlySpan<byte> snapshot)
    {
        const int moduleHeader = 22;
        const int acr = 16;
        const int ila = 23;
        if (snapshot.Length < 58 || snapshot[19] != 3 || snapshot[20] != 1 ||
            !snapshot.Slice(21, 16).SequenceEqual("VIC20\0\0\0\0\0\0\0\0\0\0\0"u8) ||
            !snapshot.Slice(37, 13).SequenceEqual("VICE Version\x1a"u8))
            return -1;
        var latchOffset = -1;
        foreach (var (name, offset, size) in X64ScNativeLaunch.Modules(snapshot))
        {
            if (name != "VIA1") continue;
            if (latchOffset >= 0 || size != moduleHeader + 28 || snapshot[offset + 16] != 2 ||
                snapshot[offset + 17] != 2 || !snapshot.Slice(offset, 16).SequenceEqual("VIA1\0\0\0\0\0\0\0\0\0\0\0\0"u8) ||
                (snapshot[offset + moduleHeader + acr] & 0x03) != 0)
                return -1;
            latchOffset = offset + moduleHeader + ila;
        }
        return latchOffset;
    }

    private static string Format(ulong? clock) => clock?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none";
}
