using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 load detector, fresh-process controls (round 4 N11; round 5 N16): each scenario runs
/// the probe console in a new process with VICESHARP_NATIVE_LOAD_LOG pointing at a fresh file, so the once-per-process
/// resolvers are observed cold. Positive controls (xvic first, x64 first, alias and fallback probing, a failing load)
/// must show their attempts in their own logs; the managed fake and the empty scenario must leave the log empty; an
/// unwritable log fails the process loudly. These assertions are host independent: a host that carries a real
/// library records resolved and loaded or failed lines, a host without one records unresolved lines.
/// </summary>
public sealed class NativeLoadLogProcessControlTests
{
    private static string ProbePath()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var sourceDirectory = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "TestHarnessSourceDirectory").Value!;
        var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
        var path = Path.GetFullPath(Path.Combine(sourceDirectory, "..", "..", "tools", "ViceSharp.NativeLoadProbe", "bin", configuration, "net10.0", "ViceSharp.NativeLoadProbe.dll"));
        Assert.True(File.Exists(path), $"probe not built: {path}");
        return path;
    }

    private static (int ExitCode, string[] Log, string StdOut, string StdErr) RunProbe(string scenario, string? logPath = null)
    {
        logPath ??= Path.Combine(Path.GetTempPath(), "vicesharp-load-log-" + Guid.NewGuid().ToString("N") + ".log");
        // Like the Nuke Test target (round 4 N11), the launcher creates the empty log before the process starts, so
        // "exists and is empty" is a statement about the process, never about whether the audit type was touched.
        File.WriteAllText(logPath, string.Empty);
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(ProbePath());
        start.ArgumentList.Add(scenario);
        start.Environment["VICESHARP_NATIVE_LOAD_LOG"] = logPath;
        start.Environment.Remove("VICESHARP_ROM_PATH");
        start.Environment.Remove("VICE_DATA_PATH");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(120_000), "probe did not exit");
        var log = File.Exists(logPath) ? File.ReadAllLines(logPath) : null;
        if (File.Exists(logPath))
            File.Delete(logPath);
        Assert.NotNull(log);
        return (process.ExitCode, log, stdout, stderr);
    }

    private static readonly string[] ProbeOutcomes = ["resolved", "unresolved"];
    private static readonly string[] LoadOutcomes = ["loaded", "failed"];

    private static (string Library, string Path, string Outcome) Parse(string line)
    {
        var parts = line.Split(',');
        Assert.Equal(3, parts.Length);
        return (parts[0], parts[1], parts[2]);
    }

    [Fact]
    public void EmptyScenario_LeavesAnEmptyLog()
    {
        var (exit, log, _, _) = RunProbe("none");
        Assert.Equal(0, exit);
        Assert.Empty(log);
    }

    [Fact]
    public void ManagedFake_StaysSilent()
    {
        var (exit, log, stdout, _) = RunProbe("managed-fake");
        Assert.Equal(0, exit);
        Assert.Contains("managed fake stepped 1000", stdout);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("x64-first", "vice_x64", "vice_xvic")]
    [InlineData("xvic-first", "vice_xvic", "vice_x64")]
    public void AvailabilityProbes_AreRecordedOncePerLibraryInCallOrder(string scenario, string first, string second)
    {
        var (exit, log, _, stderr) = RunProbe(scenario);
        Assert.True(exit == 0, stderr);
        Assert.Equal(2, log.Length);
        var a = Parse(log[0]);
        var b = Parse(log[1]);
        Assert.Equal(first, a.Library);
        Assert.Equal(second, b.Library);
        Assert.Contains(a.Outcome, ProbeOutcomes);
        Assert.Contains(b.Outcome, ProbeOutcomes);
        Assert.Equal(a.Outcome == "resolved", a.Path != "none");
        Assert.Equal(b.Outcome == "resolved", b.Path != "none");
    }

    [Theory]
    [InlineData("failing-load", "vice_x64", "vice_x64")]
    [InlineData("failing-load-xvic", "vice_xvic", "vice_xvic")]
    [InlineData("alias", "vice_x64", "vice-shim")]
    public void FailingLoad_RecordsTheResolvedPathThenTheFailure(string scenario, string library, string fileStem)
    {
        var (exit, log, stdout, stderr) = RunProbe(scenario);
        Assert.True(exit == 2, $"exit {exit}; stdout {stdout}; stderr {stderr}");
        var probe = log.Select(Parse).First(l => l.Library == library && ProbeOutcomes.Contains(l.Outcome));
        Assert.Equal("resolved", probe.Outcome);
        Assert.Contains(fileStem, probe.Path);
        var load = log.Select(Parse).Single(l => l.Library == library && LoadOutcomes.Contains(l.Outcome));
        Assert.Equal("failed", load.Outcome);
        Assert.Equal(probe.Path, load.Path);
    }

    [Fact]
    public void UnwritableLog_FailsTheProcessLoudly()
    {
        var unwritable = Path.Combine(Path.GetTempPath(), "vicesharp-missing-" + Guid.NewGuid().ToString("N"), "sub", "audit.log");
        var (exit, _, stderr) = RunProbeExpectingNoLog("x64-first", unwritable);
        Assert.NotEqual(0, exit);
        Assert.Contains("VICESHARP_NATIVE_LOAD_LOG", stderr);
    }

    private static (int ExitCode, string StdOut, string StdErr) RunProbeExpectingNoLog(string scenario, string logPath)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(ProbePath());
        start.ArgumentList.Add(scenario);
        start.Environment["VICESHARP_NATIVE_LOAD_LOG"] = logPath;
        start.Environment.Remove("VICESHARP_ROM_PATH");
        start.Environment.Remove("VICE_DATA_PATH");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(120_000), "probe did not exit");
        Assert.False(File.Exists(logPath));
        return (process.ExitCode, stdout, stderr);
    }
}
