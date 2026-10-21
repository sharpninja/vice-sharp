namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The cross-run, cross-host verdict for one fixture: accepted (every candidate accepted, byte-identical, every
/// required host present) or blocked (anything else), with the measured clocks of every candidate.
/// </summary>
public sealed record FixtureClassification(
    string FixtureId,
    string Outcome,
    string? AcceptedSha256,
    long? AcceptedLength,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<HostClock> Clocks)
{
    /// <summary>Outcome value for a fixture every host and run agree on.</summary>
    public const string Accepted = "accepted";
    /// <summary>Outcome value for a fixture that may not be published; the reasons name the blocker and the clocks.</summary>
    public const string Blocked = "blocked";

    /// <summary>True when the fixture may be published.</summary>
    public bool IsAccepted => Outcome == Accepted;
}
