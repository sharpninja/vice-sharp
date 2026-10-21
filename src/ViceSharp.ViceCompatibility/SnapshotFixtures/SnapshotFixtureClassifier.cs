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
        foreach (var c in candidates)
        {
            if (c.FixtureId != fixtureId)
                reasons.Add($"host {c.Host}: candidate belongs to fixture '{c.FixtureId}', not '{fixtureId}'");
            if (c.Trigger != trigger)
                reasons.Add($"host {c.Host}: trigger {c.Trigger} is not the required {trigger}");
            if (c.ExitCode != FixtureCandidate.AcceptedExit)
                reasons.Add($"host {c.Host}: {c.Outcome}: {c.Reason}");
            else if (c.WriteClock != trigger || c.MainCpuClock != trigger)
                reasons.Add($"host {c.Host}: {BlockerId}: write clock {Format(c.WriteClock)}, MAINCPU clock {Format(c.MainCpuClock)}, trigger {trigger}");
        }
        var hashes = candidates.Select(c => c.CandidateSha256).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (hashes.Length > 1)
            reasons.Add("candidate bytes differ across runs or hosts: " + string.Join(", ", candidates.Select(c => c.Host + "=" + (c.CandidateSha256 ?? "absent"))));
        if (hashes.Length == 1 && hashes[0] is null)
            reasons.Add("no candidate bytes exist on any host");

        if (reasons.Count > 0)
            return new FixtureClassification(fixtureId, FixtureClassification.Blocked, null, null, reasons, clocks);
        var accepted = candidates[0];
        return new FixtureClassification(fixtureId, FixtureClassification.Accepted, accepted.CandidateSha256, accepted.CandidateLength, [], clocks);
    }

    private static string Format(ulong? clock) => clock?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none";
}
