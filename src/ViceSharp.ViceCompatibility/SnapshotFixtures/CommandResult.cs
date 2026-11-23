namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The outcome of one observer launch: the process exit code, whether the runner had to stop it, and the
/// <c>VICE_SNAP</c> records seen on stderr in order (the full stream is in the request's stderr file).
/// </summary>
/// <param name="ExitCode">Process exit code (-1 when the runner stopped the process).</param>
/// <param name="TimedOut">True when the wall-clock budget expired and the runner stopped the process.</param>
/// <param name="SnapshotLines">Every stderr line that starts with <c>VICE_SNAP,</c>, in order.</param>
/// <param name="LimitReached">
/// True when stdout carries the pinned core's "cycle limit reached." error (mainc64cpu.c 813 to 816, maincpu.c 640
/// to 643), the normal end of a -limitcycles run, which the pinned VICE reports with exit code 1.
/// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17. The same message in the run's fresh VICE log also proves the limit.
/// </param>
public sealed record CommandResult(int ExitCode, bool TimedOut, IReadOnlyList<string> SnapshotLines, bool LimitReached = false);
