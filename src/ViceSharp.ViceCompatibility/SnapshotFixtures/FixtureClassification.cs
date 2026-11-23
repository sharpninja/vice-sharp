namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The cross-run, cross-host verdict for one fixture: accepted (every candidate accepted, byte-identical, every
/// required host present) or blocked (anything else), with the measured clocks of every candidate.
/// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17. A scoped unused-latch comparison still selects a raw candidate
/// and records that candidate's original hash and length.
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

    /// <summary>2026-11-23, D5b/R17. Identifies the pinned VIA1 exception when it was needed to accept a fixture.</summary>
    public string? ByteComparisonException { get; init; }
}
