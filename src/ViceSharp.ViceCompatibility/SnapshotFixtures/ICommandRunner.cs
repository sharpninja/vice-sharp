namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// Runs one observer launch. The production implementation starts a process; tests substitute a double that
/// writes a candidate file and answers with the checkpoint records of the case under test (PLAN-NATIVERESTORE-001
/// D2: the exact invocation is proven with a mock runner before the real binaries).
/// </summary>
public interface ICommandRunner
{
    /// <summary>Runs the request to completion and returns its result.</summary>
    CommandResult Run(CommandRequest request);
}
