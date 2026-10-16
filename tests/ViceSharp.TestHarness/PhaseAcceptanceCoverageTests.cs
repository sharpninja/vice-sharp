namespace ViceSharp.TestHarness;

using System.Diagnostics;
using Xunit;

/// <summary>
/// Fails the build when the phase being exited, or any earlier phase, has a
/// requirement with acceptance criteria that no test names. Iteration is the
/// phase number. Priority is severity and is not consulted.
/// </summary>
public sealed class PhaseAcceptanceCoverageTests
{
    /// <summary>
    /// TR: TR-QA-XMLDOCS-CONVENTION.
    /// Use case: Exit of any phase requires tests for that phase and every
    /// prior phase. This run exits phase 2, so phases 0, 1, and 2 are in scope.
    /// Acceptance: tools/check_phase_acceptance_coverage.ps1 -ThroughPhase 2
    /// exits 0. A requirement in a later iteration is outside this exit.
    /// </summary>
    [Fact]
    public void ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases()
    {
        const int throughPhase = 2;
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, "tools", "check_phase_acceptance_coverage.ps1");
        var start = new ProcessStartInfo
        {
            FileName = "pwsh",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("-ThroughPhase");
        start.ArgumentList.Add(throughPhase.ToString());

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            "Phase " + throughPhase + " cannot exit. Requirements in this phase or an earlier phase have acceptance criteria and are not named under tests/."
            + Environment.NewLine
            + stdout
            + stderr);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ViceSharp.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate ViceSharp.slnx from the test output directory.");
    }
}
