namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// What one fixture generation run needs: the fixture identity, the observer binary, the machine arguments, the
/// VICE data root with the ROM files to verify, the literal trigger clock (R12), and where the candidate and the
/// stderr stream are written. The candidate path is never the published fixture path (N21).
/// </summary>
/// <param name="FixtureId">Stable identifier used in BLOCKED reports (for example "xvic-pal-unexpanded").</param>
/// <param name="Machine">"xvic" or "x64sc".</param>
/// <param name="Model">The VICE model name passed to -model (for example "vic20" or "vic20ntsc").</param>
/// <param name="MachineArguments">Extra machine arguments placed right after -model (for example -memory none).</param>
/// <param name="Trigger">The checkpoint trigger clock; acceptance requires the write clock to equal it.</param>
/// <param name="ObserverPath">Absolute path of the observer binary built by build-vice-observer.ps1.</param>
/// <param name="DataRoot">The VICE data root passed to -directory.</param>
/// <param name="RequiredRoms">ROM files (relative to the data root) the launch loads; each must pass the allowlist.</param>
/// <param name="CandidatePath">Where the checkpoint unit writes the candidate snapshot.</param>
/// <param name="StderrPath">Where the whole stderr stream is retained.</param>
/// <param name="Host">The host name recorded in the candidate.</param>
/// <param name="LimitMargin">Cycles added to the trigger for -limitcycles so the trap boundary is reached.</param>
public sealed record SnapshotFixtureSpec(
    string FixtureId,
    string Machine,
    string Model,
    IReadOnlyList<string> MachineArguments,
    ulong Trigger,
    string ObserverPath,
    string DataRoot,
    IReadOnlyList<string> RequiredRoms,
    string CandidatePath,
    string StderrPath,
    string Host,
    ulong LimitMargin = 50_000);
