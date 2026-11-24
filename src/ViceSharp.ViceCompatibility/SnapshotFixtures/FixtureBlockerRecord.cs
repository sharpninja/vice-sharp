namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// One open blocker of a fixture, kept in the committed blockers file so the real-bound tests report the case as
/// BLOCKED by name (round 5 N14) instead of as a plain failure or a skip.
/// </summary>
public sealed class FixtureBlockerRecord
{
    public string FixtureId { get; set; } = "";
    public string Blocker { get; set; } = "";
    public string RecordedAt { get; set; } = "";
    public string[] Reasons { get; set; } = [];
    public HostClock[] Clocks { get; set; } = [];
}
