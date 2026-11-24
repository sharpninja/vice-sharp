namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>One host's measured clocks and candidate hash, listed verbatim in every classification (R12: never approximated).</summary>
public sealed record HostClock(string Host, ulong Trigger, ulong? WriteClock, int WriterResult, ulong? MainCpuClock, string? CandidateSha256, string Outcome, string Reason);
