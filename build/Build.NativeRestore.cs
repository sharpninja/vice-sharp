using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using ViceSharp.Build.Testing;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2: the ordinary gate's load-log and TRX retention, the per-host fixture generation
/// target, the cross-host publication target and the cumulative real-bound VSF target (F05, R12, N11, N14).
/// </summary>
partial class Build
{
    [Parameter("Test: path of the VICESHARP_NATIVE_LOAD_LOG file. Created empty before the test process starts; the target fails when it is missing or not empty afterwards (PLAN-NATIVERESTORE-001 D2, N11). Default artifacts/test-results/<timestamp>/native-load.log.")]
    readonly string NativeLoadLog = null!;

    [Parameter("Test and NativeSnapshotCompatibilityTest: directory that receives one TRX per project. Default artifacts/test-results/<timestamp>.")]
    readonly string TestResultsDirectory = null!;

    [Parameter("NativeSnapshotCompatibilityTest: the slice through which the real-bound VSF cases run. Only 1 is defined.")]
    readonly int NativeSnapshotThroughSlice;

    [Parameter("GenerateNativeSnapshotFixtures: directory holding xvic-observer (and x64sc-observer). Default artifacts/immutable-vice/<commit>/bin-<host>.")]
    readonly string ObserverBinDirectory = null!;

    [Parameter("GenerateNativeSnapshotFixtures: the VICE data root passed as -directory. Default artifacts/immutable-vice/<commit>/work-<host>/vice/data.")]
    readonly string ViceDataDirectory = null!;

    [Parameter("GenerateNativeSnapshotFixtures: output directory for candidates, stderr streams and receipts. Default artifacts/native-snapshot-fixtures/<timestamp>.")]
    readonly string FixtureOutputDirectory = null!;

    [Parameter("PublishNativeSnapshotFixtures: comma-separated candidate receipt directories of every host (the GenerateNativeSnapshotFixtures output of each).")]
    readonly string FixtureReceiptDirectories = null!;

    const string PinnedViceCommit = "cc67418cba58e71d8e70fd353e9de47199ad2075";
    const ulong LiteralCheckpointClock = 100_000;

    static readonly string[] NativeSafeTestProjects =
    [
        "tests/ViceSharp.NativeClockProbe.Tests/ViceSharp.NativeClockProbe.Tests.csproj",
        "tests/ViceSharp.OracleContract.Tests/ViceSharp.OracleContract.Tests.csproj",
        "tests/ViceSharp.VsfCompatibility.Tests/ViceSharp.VsfCompatibility.Tests.csproj",
    ];

    /// <summary>The xvic fixtures of PLAN-VSFLOCKSTEP-001 Slice 1: id, model, published name, ROM files the launch loads.</summary>
    static readonly (string Id, string Model, string FileName, string[] Roms)[] XvicFixtures =
    [
        ("xvic-pal-unexpanded", "vic20", "ready-xvic-pal-unexpanded.vsf", ["VIC20/basic-901486-01.bin", "VIC20/kernal.901486-07.bin", "VIC20/chargen-901460-03.bin"]),
        ("xvic-ntsc-unexpanded", "vic20ntsc", "ready-xvic-ntsc-unexpanded.vsf", ["VIC20/basic-901486-01.bin", "VIC20/kernal.901486-06.bin", "VIC20/chargen-901460-03.bin"]),
    ];

    string HostTag => OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
    string RunStamp { get; } = DateTime.Now.ToString("yyyyMMddTHHmmss");

    AbsolutePath ResolvedTestResultsDirectory => TestResultsDirectory is { Length: > 0 }
        ? (AbsolutePath)Path.GetFullPath(TestResultsDirectory)
        : ArtifactsDirectory / "test-results" / RunStamp;

    AbsolutePath ResolvedNativeLoadLog => NativeLoadLog is { Length: > 0 }
        ? (AbsolutePath)Path.GetFullPath(NativeLoadLog)
        : ResolvedTestResultsDirectory / "native-load.log";

    AbsolutePath FixtureGeneratorProject => RootDirectory / "tools" / "ViceSharp.SnapshotFixtureGenerator" / "ViceSharp.SnapshotFixtureGenerator.csproj";

    /// <summary>Creates the empty load log before the test process starts (the audit interval begins before discovery, N16).</summary>
    AbsolutePath PrepareNativeLoadLog()
    {
        var log = ResolvedNativeLoadLog;
        Directory.CreateDirectory(Path.GetDirectoryName(log)!);
        File.WriteAllText(log, string.Empty);
        return log;
    }

    /// <summary>
    /// The ROM root for a local Test run: null when VICESHARP_ROM_PATH or VICE_DATA_PATH is already set (the machine
    /// decides), otherwise the submodule's vendored data tree when it carries a C64 directory, otherwise null.
    /// </summary>
    string? ResolveLocalRomRoot()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VICESHARP_ROM_PATH")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VICE_DATA_PATH")))
            return null;
        var vendored = RootDirectory / "native" / "vice" / "vice" / "data";
        return Directory.Exists(vendored / "C64") ? vendored.ToString() : null;
    }

    /// <summary>Fails when the load log is missing or holds any attempt (N11).</summary>
    static void AssertNativeLoadLogEmpty(AbsolutePath log)
    {
        if (!File.Exists(log))
            throw new InvalidOperationException($"Native load log missing after the run: {log}");
        var lines = File.ReadAllLines(log).Where(l => l.Length > 0).ToArray();
        if (lines.Length > 0)
            throw new InvalidOperationException($"Native load log is not empty ({lines.Length} attempt(s)): {string.Join(" | ", lines.Take(10))}");
        Serilog.Log.Information("Native load log {Log} exists and is empty", log);
    }

    Target GenerateNativeSnapshotFixtures => _ => _
        .Description("Per host: run xvic-observer twice per Slice 1 fixture with VICESHARP_SNAPSHOT_AT=100000:<candidate>, the ROM allowlist and -seed 1; write candidate receipts; BLOCKED when the write clock is not the literal 100,000 (R12)")
        .DependsOn(Compile)
        .Executes(() =>
        {
            var bin = ObserverBinDirectory is { Length: > 0 } ? (AbsolutePath)Path.GetFullPath(ObserverBinDirectory) : ArtifactsDirectory / "immutable-vice" / PinnedViceCommit / ("bin-" + HostTag);
            var data = ViceDataDirectory is { Length: > 0 } ? (AbsolutePath)Path.GetFullPath(ViceDataDirectory) : ArtifactsDirectory / "immutable-vice" / PinnedViceCommit / ("work-" + HostTag) / "vice" / "data";
            var output = FixtureOutputDirectory is { Length: > 0 } ? (AbsolutePath)Path.GetFullPath(FixtureOutputDirectory) : ArtifactsDirectory / "native-snapshot-fixtures" / RunStamp;
            var observer = bin / (OperatingSystem.IsWindows() ? "xvic-observer.exe" : "xvic-observer");
            var host = Environment.MachineName;
            Directory.CreateDirectory(output);
            var blocked = new List<string>();
            var failed = new List<string>();
            foreach (var (id, model, _, roms) in XvicFixtures)
            {
                for (var run = 1; run <= 2; run++)
                {
                    var receipt = output / $"{id}-{host}-{run}.candidate.json";
                    var arguments = new List<string>
                    {
                        "generate", "--fixture-id", id, "--machine", "xvic", "--model", model, "--machine-arg", "-memory", "--machine-arg", "none",
                        "--trigger", LiteralCheckpointClock.ToString(), "--observer", observer, "--data", data,
                        "--candidate", output / $"{id}-{host}-{run}.vsf", "--stderr", output / $"{id}-{host}-{run}.stderr", "--host", host, "--receipt", receipt,
                    };
                    foreach (var rom in roms) { arguments.Add("--rom"); arguments.Add(rom); }
                    var exit = RunFixtureGenerator(arguments);
                    Serilog.Log.Information("{Fixture} run {Run}: exit {Exit} (0 accepted candidate, 3 blocked, 1 failed); receipt {Receipt}", id, run, exit, receipt);
                    if (exit == 3) blocked.Add($"{id} run {run}");
                    else if (exit != 0) failed.Add($"{id} run {run}");
                }
            }
            Serilog.Log.Information("Candidate receipts in {Output}; classify across hosts with PublishNativeSnapshotFixtures --fixture-receipt-directories <this>,<other host>", output);
            if (blocked.Count > 0)
                throw new InvalidOperationException($"BLOCKER-R12-XVIC-CHECKPOINT on {host}: {string.Join(", ", blocked)}; no fixture written, candidates retained in {output}");
            if (failed.Count > 0)
                throw new InvalidOperationException($"fixture generation failed on {host}: {string.Join(", ", failed)}; see the receipts in {output}");
        });

    Target PublishNativeSnapshotFixtures => _ => _
        .Description("Cross-host: classify the candidate receipts of every host for each Slice 1 fixture; publish only an accepted fixture and its manifest entry; BLOCKED otherwise")
        .DependsOn(Compile)
        .Requires(() => FixtureReceiptDirectories)
        .Executes(() =>
        {
            var directories = FixtureReceiptDirectories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Path.GetFullPath).ToArray();
            var manifest = RootDirectory / "docs" / "requirements" / "oracles" / "vice-vsf-state-transfer-manifest.json";
            var fixtureDirectory = RootDirectory / "tests" / "ViceSharp.TestHarness" / "Fixtures" / "Vsf";
            var hosts = new List<string>();
            var blocked = new List<string>();
            foreach (var (id, _, fileName, _) in XvicFixtures)
            {
                var receipts = directories.SelectMany(d => Directory.EnumerateFiles(d, $"{id}-*.candidate.json")).OrderBy(p => p, StringComparer.Ordinal).ToArray();
                foreach (var receipt in receipts)
                {
                    // The host comes from the receipt itself (host names carry hyphens: LAB-OMARCHY, PAYTON-LEGION2).
                    using var document = JsonDocument.Parse(File.ReadAllBytes(receipt));
                    var hostName = document.RootElement.GetProperty("host").GetString()!;
                    if (!hosts.Contains(hostName)) hosts.Add(hostName);
                }
                var classification = ArtifactsDirectory / "native-snapshot-fixtures" / RunStamp / $"{id}.classification.json";
                var blockers = RootDirectory / "docs" / "requirements" / "oracles" / "vice-vsf-fixture-blockers.json";
                var arguments = new List<string> { "classify", "--fixture-id", id, "--trigger", LiteralCheckpointClock.ToString(), "--out", classification, "--blockers", blockers };
                foreach (var r in receipts) { arguments.Add("--receipt"); arguments.Add(r); }
                foreach (var h in hosts) { arguments.Add("--host"); arguments.Add(h); }
                var exit = RunFixtureGenerator(arguments);
                if (exit != 0) { blocked.Add(id); continue; }
                var publish = new List<string> { "publish", "--classification", classification, "--receipt", receipts[0], "--fixture", fixtureDirectory / fileName, "--manifest", manifest, "--manifest-path", $"tests/ViceSharp.TestHarness/Fixtures/Vsf/{fileName}" };
                foreach (var h in hosts) { publish.Add("--host"); publish.Add(h); }
                if (RunFixtureGenerator(publish) != 0) blocked.Add(id + " (publish refused)");
            }
            if (blocked.Count > 0)
                throw new InvalidOperationException($"BLOCKER-R12-XVIC-CHECKPOINT: not published: {string.Join(", ", blocked)} (hosts {string.Join(", ", hosts)})");
        });

    int RunFixtureGenerator(IReadOnlyList<string> arguments)
    {
        var process = ProcessTasks.StartProcess(
            "dotnet",
            string.Join(' ', new[] { "run", "--project", FixtureGeneratorProject.ToString(), "--configuration", Configuration, "--no-build", "--" }.Concat(arguments.Select(a => a.Contains(' ') ? "\"" + a + "\"" : a))),
            RootDirectory,
            logOutput: true);
        process.WaitForExit();
        return process.ExitCode;
    }

    Target NativeSnapshotCompatibilityTest => _ => _
        .Description("Cumulative real-bound VSF gate (Category=VsfSlice1Real over the three native-safe projects): BLOCKED cases are reported by identifier with the blocker, every outcome is listed, and anything but all-pass exits nonzero")
        .DependsOn(Compile)
        .Requires(() => NativeSnapshotThroughSlice == 1)
        .Executes(() =>
        {
            var results = ResolvedTestResultsDirectory / "native-snapshot";
            Directory.CreateDirectory(results);
            var trx = new List<string>();
            foreach (var project in NativeSafeTestProjects)
            {
                var name = Path.GetFileNameWithoutExtension(project);
                var file = results / (name + ".trx");
                DotNetTest(s => s
                    .SetProjectFile(RootDirectory / project)
                    .SetConfiguration(Configuration)
                    .SetNoRestore(true)
                    .SetNoBuild(true)
                    .SetFilter("Category=VsfSlice1Real")
                    .SetResultsDirectory(results)
                    .SetLoggers($"trx;LogFileName={name}.trx")
                    .SetProcessExitHandler(_ => { }));
                if (File.Exists(file)) trx.Add(file);
            }
            var summary = TrxOutcomes.Summarize(trx);
            foreach (var c in summary.Cases)
                Serilog.Log.Information("{Outcome,-11} {Project} {Case} {Message}", c.Outcome, c.Project, c.FullName, c.Outcome == "Blocked" ? c.Message : "");
            var (passing, reason) = TrxOutcomes.Evaluate(summary);
            Serilog.Log.Information("NativeSnapshotCompatibilityTest through slice {Slice}: total {Total}, passed {Passed}, failed {Failed}, BLOCKED {Blocked}, skipped {Skipped}, not executed {NotExecuted}; {Reason}",
                NativeSnapshotThroughSlice, summary.Total, summary.Passed, summary.Failed, summary.Blocked, summary.Skipped, summary.NotExecuted, reason);
            if (!passing)
                throw new InvalidOperationException("NativeSnapshotCompatibilityTest is not passing: " + reason);
        });
}
