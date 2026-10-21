namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// One observer launch as the generator composes it: executable, ordered arguments, working directory, the
/// environment entries the launch adds, the file that receives the whole stderr stream and a wall-clock budget.
/// </summary>
/// <param name="FileName">Absolute path of the observer binary.</param>
/// <param name="Arguments">Ordered command line arguments (no shell quoting).</param>
/// <param name="WorkingDirectory">Working directory of the process.</param>
/// <param name="Environment">Environment variables set for the process (VICESHARP_SNAPSHOT_AT among them).</param>
/// <param name="StderrPath">File that receives every stderr line verbatim; stdout goes to the same path with ".stdout" appended.</param>
/// <param name="PermitCycles">
/// Number of gate permits to feed on stdin: the observer binaries are gated (gate-observer.c reads one 'c' byte per
/// cycle and exits with 66 on EOF), so a launch of N cycles needs at least N permits; the runner writes this many
/// plus slack and tolerates the process exiting before the slack is consumed. Zero feeds nothing.
/// </param>
/// <param name="TimeoutMilliseconds">Wall-clock budget after which the runner stops the process and reports it.</param>
public sealed record CommandRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment,
    string StderrPath,
    ulong PermitCycles,
    int TimeoutMilliseconds = 600_000);
