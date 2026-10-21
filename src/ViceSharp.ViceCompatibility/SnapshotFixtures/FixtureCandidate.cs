namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The result of one generation run on one host, in the shape of the Slice 1 contract's generator outcome
/// (PLAN-NATIVERESTORE-001 D1 VsfSlice1ManifestAndResumeContractTests.GeneratorOutcome): trigger and measured write
/// clock are always distinguished, a hash exists only for bytes that exist (N25), nothing is published by a run
/// (AcceptedFixtureChanged and ManifestChanged are false), and a non-accepted run carries a nonzero exit code and
/// the blocked fixture identifier.
/// </summary>
public sealed class FixtureCandidate
{
    /// <summary>Exit code of an accepted candidate.</summary>
    public const int AcceptedExit = 0;
    /// <summary>Exit code of a run that failed before or during the launch (ROMs, observer, writer error, no record).</summary>
    public const int FailedExit = 1;
    /// <summary>Exit code of a run whose write clock or MAINCPU clock differs from the trigger (BLOCKER-R12-XVIC-CHECKPOINT).</summary>
    public const int BlockedExit = 3;

    public string FixtureId { get; init; } = "";
    public string Host { get; init; } = "";
    public string Machine { get; init; } = "";
    public string Model { get; init; } = "";
    public ulong Trigger { get; init; }
    /// <summary>The clock measured inside the trap callback, or null when no checkpoint record was observed.</summary>
    public ulong? WriteClock { get; init; }
    /// <summary>machine_write_snapshot result (0 success); -1 when no record was observed; -2 malformed spec; -3 not launched.</summary>
    public int WriterResult { get; init; }
    /// <summary>The MAINCPU clock read back from the candidate bytes, when they exist and parse.</summary>
    public ulong? MainCpuClock { get; init; }
    public string? CandidatePath { get; init; }
    public string? CandidateSha256 { get; init; }
    public long? CandidateLength { get; init; }
    public string Command { get; init; } = "";
    public IReadOnlyList<RomCheck> Roms { get; init; } = [];
    public string? ObserverSha256 { get; init; }
    public int ProcessExitCode { get; init; }
    public bool TimedOut { get; init; }
    public IReadOnlyList<string> SnapshotLines { get; init; } = [];
    public int ExitCode { get; init; }
    public IReadOnlyList<string> BlockedIds { get; init; } = [];
    public string Reason { get; init; } = "";
    public bool AcceptedFixtureChanged { get; init; }
    public bool ManifestChanged { get; init; }

    /// <summary>"accepted-candidate", "blocked" or "failed".</summary>
    public string Outcome => ExitCode switch { AcceptedExit => "accepted-candidate", BlockedExit => "blocked", _ => "failed" };
}
