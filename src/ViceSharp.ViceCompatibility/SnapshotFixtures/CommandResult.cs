namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// The outcome of one observer launch: the process exit code, whether the runner had to stop it, and the
/// <c>VICE_SNAP</c> records seen on stderr in order (the full stream is in the request's stderr file).
/// </summary>
/// <param name="ExitCode">Process exit code (-1 when the runner stopped the process).</param>
/// <param name="TimedOut">True when the wall-clock budget expired and the runner stopped the process.</param>
/// <param name="SnapshotLines">Every stderr line that starts with <c>VICE_SNAP,</c>, in order.</param>
public sealed record CommandResult(int ExitCode, bool TimedOut, IReadOnlyList<string> SnapshotLines);
