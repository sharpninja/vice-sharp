using System.Globalization;
using ViceSharp.ViceCompatibility;
using ViceSharp.ViceCompatibility.SnapshotFixtures;
using static ViceSharp.VsfCompatibility.Tests.VsfSlice1ManifestAndResumeContractTests;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// TR-VSFLOCKSTEP-RESUME-001 AC1 as amended by R15 (external real inputs v1.2 and v1.4), real-bound form of the
/// Slice 1 resume boundary AC SnapshotResume_ObserverClockAndPcMatchVsfMainCpu (N02, N07, N10, N13, N18, N23) and
/// the v1.4 round trip, PLAN-NATIVERESTORE-001 D5. The mock-proven assertions of the contract class are bound to
/// the real x64sc observer of this host: the supplied v1.2 fixture is normalized by the external adapter, launched
/// with the R10 precondition last and the N07 budget, and the observed VICE_SNAP, VICE_OBS and VICE_GATE records are
/// handed to the identical assertion. Translation at the seam (documented, never weakening): VICE_BUS diagnostic
/// lines are not part of the resume record set and are dropped; VICE_OBS half-cycle indices are rebased to the first
/// post-load record (N10: raw indices stay in the retained stream); the pinned cycle-limit exit (mainc64cpu.c 813 to
/// 816: "cycle limit reached." then exit code 1) is reported as the trace terminal "VICE_EXIT,limit" with exit 0.
/// </summary>
[Trait("Category", "VsfSlice1Real")]
public sealed class NativeViceSnapshotResumeTests
{
    /// <summary>
    /// Cycles of the autostart prelude: measured, not derived. The pinned x64sc-observer loads an autostarted snapshot
    /// at the READY check (autostart.c 1269), observed at clock 3,028,945 in both retained runs on LAB-OMARCHY
    /// (artifacts/native-snapshot-resume/20261021T054138 and 20261021T064900, -seed 1); the earlier derivation from
    /// c64.c 1070 (three seconds at 985,248 Hz) gave 2,956,000 and ended the round trip before the load
    /// (20261021T064921). The margin covers the window and permit accounting.
    /// </summary>
    private const ulong PreludeCycles = 3_100_000;
    private const ulong WindowCycles = 4;

    internal sealed class RealResumeProbe : IResumeProbe
    {
        public string OutputDirectory { get; } = Path.Combine(VsfSlice1RealEvidence.RepoRoot, "artifacts", "native-snapshot-resume", DateTime.Now.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture));
        public string? NormalizedPath { get; private set; }
        public string? LastStderrPath { get; private set; }
        public CommandResult? LastResult { get; private set; }

        public ResumeTrace Run(ResumeInput input) => Run(input, VsfSlice1RealEvidence.SuppliedFixture, normalize: true, "v12-resume");

        public ResumeTrace Run(ResumeInput input, string fixturePath, bool normalize, string label)
        {
            var observer = VsfSlice1RealEvidence.ObserverPath("x64sc");
            Assert.True(File.Exists(observer), "x64sc-observer missing: " + observer);
            Assert.True(Directory.Exists(VsfSlice1RealEvidence.DataRoot), "VICE data root missing: " + VsfSlice1RealEvidence.DataRoot);
            Assert.True(File.Exists(fixturePath), "fixture missing: " + fixturePath);
            Directory.CreateDirectory(OutputDirectory);
            var bytes = File.ReadAllBytes(fixturePath);
            var launchBytes = normalize ? X64ScSnapshotCompatibility.Normalize(bytes) : bytes;
            NormalizedPath = Path.Combine(OutputDirectory, label + ".vsf");
            File.WriteAllBytes(NormalizedPath, launchBytes);
            var precondition = X64ScNativeLaunch.ComputePrecondition(launchBytes);
            Assert.Equal(input.Precondition, precondition);
            var budget = X64ScNativeLaunch.ComputeBudget(input.Clock, input.WindowCycles, input.PreludeCycles);
            var arguments = X64ScNativeLaunch.BuildArguments(NormalizedPath, precondition, budget.LimitCycles).ToList();
            // The headless stock binary needs the dummy sound device and the data root (the same two options the
            // equivalence runner passes); inserted before -limitcycles so the precondition flags stay last (N05).
            var limitIndex = arguments.IndexOf("-limitcycles");
            arguments.InsertRange(limitIndex, ["-sounddev", "dummy", "-directory", VsfSlice1RealEvidence.DataRoot]);
            LastStderrPath = Path.Combine(OutputDirectory, label + ".stderr");
            // Permits cover the prelude up to the load plus everything after it up to the limit (the restored clock
            // can be far below the limit when the fixture was written early), plus a margin; never the limiting factor.
            var request = new CommandRequest(observer, arguments, Path.GetDirectoryName(observer)!, new Dictionary<string, string>(StringComparer.Ordinal), LastStderrPath, input.PreludeCycles + budget.LimitCycles + 100_000, 1_800_000);
            LastResult = new ProcessCommandRunner().Run(request);
            File.WriteAllText(Path.Combine(OutputDirectory, label + ".command.txt"), observer + " " + string.Join(' ', arguments) + "\n");
            var lines = ExtractResumeLines(LastStderrPath, LastResult);
            File.WriteAllLines(Path.Combine(OutputDirectory, label + ".resume-window.txt"), lines);
            return new ResumeTrace(LastResult.ExitCode == 1 && LastResult.LimitReached ? 0 : LastResult.ExitCode, lines, budget.LimitCycles);
        }

        /// <summary>From the resume record onward: VICE_SNAP, VICE_OBS (index rebased) and VICE_GATE records, then the terminal.</summary>
        internal static string[] ExtractResumeLines(string stderrPath, CommandResult result)
        {
            var kept = new List<string>();
            var started = false;
            long? firstIndex = null;
            foreach (var line in File.ReadLines(stderrPath))
            {
                if (!started)
                {
                    if (line.StartsWith("VICE_SNAP,resume", StringComparison.Ordinal)) started = true;
                    else continue;
                }
                if (line.StartsWith("VICE_BUS,", StringComparison.Ordinal)) continue;
                if (line.StartsWith("VICE_OBS,1,", StringComparison.Ordinal))
                {
                    var columns = line.Split(',');
                    var raw = long.Parse(columns[2], NumberStyles.None, CultureInfo.InvariantCulture);
                    firstIndex ??= raw;
                    columns[2] = (raw - firstIndex.Value).ToString(CultureInfo.InvariantCulture);
                    kept.Add(string.Join(',', columns));
                    continue;
                }
                kept.Add(line);
            }
            kept.Add(result.ExitCode == 1 && result.LimitReached ? "VICE_EXIT,limit" : "VICE_EXIT," + result.ExitCode.ToString(CultureInfo.InvariantCulture));
            return [.. kept];
        }
    }

    private static ResumeInput InputFor(byte[] fixtureBytes, bool normalize, ulong window, ulong prelude)
    {
        var cpu = X64ScNativeLaunch.ReadMainCpu(normalize ? X64ScSnapshotCompatibility.Normalize(fixtureBytes) : fixtureBytes);
        var precondition = X64ScNativeLaunch.ComputePrecondition(fixtureBytes);
        return new ResumeInput(cpu.Clock, cpu.A, cpu.X, cpu.Y, cpu.Sp, cpu.Pc, cpu.Status, precondition, window, prelude);
    }

    /// <summary>
    /// TR-VSFLOCKSTEP-RESUME-001 AC1 (v1.2 external real input); PLAN-NATIVERESTORE-001 D5.
    /// Use case: the approved v1.2 fixture resumes on the pinned, unmodified x64sc through the observer wrappers.
    /// Acceptance: the resume record carries the fixture's MAINCPU clock and registers, load,0 follows it, the first
    /// gate after the load is the fixture clock plus one, the first CPU access reads at the fixture's PC, the whole
    /// window is observed and the run ends at the cycle limit.
    /// </summary>
    [Fact]
    public void SnapshotResume_ObserverClockAndPcMatchVsfMainCpu()
    {
        var bytes = File.ReadAllBytes(VsfSlice1RealEvidence.SuppliedFixture);
        var input = InputFor(bytes, normalize: true, WindowCycles, PreludeCycles);
        Assert.Equal(Approved.Clock, input.Clock);
        Assert.Equal(Approved.Precondition, input.Precondition);
        var probe = new RealResumeProbe();
        var trace = probe.Run(input);
        AssertResume(trace, input);
    }

    /// <summary>
    /// TR-VSFLOCKSTEP-RESUME-001 AC1 (v1.4 external real input, R15); PLAN-NATIVERESTORE-001 D5 round trip.
    /// Use case: the pinned x64sc writes a snapshot through the checkpoint unit during a reset workload and resumes
    /// it; the post-resume sequence must equal the writer's post-checkpoint sequence over the window.
    /// Acceptance: the written candidate is a v1.4 MAINCPU snapshot whose clock equals the measured write clock; the
    /// resumed run passes the resume boundary assertion against it; and the VICE_OBS records of the window, compared
    /// relative to each origin's marker, are identical field by field (N10).
    /// </summary>
    [Fact]
    public void RoundTrip_PinnedWriterThenResume_PostResumeSequenceMatchesWriter()
    {
        const ulong trigger = 300_000;
        var observer = VsfSlice1RealEvidence.ObserverPath("x64sc");
        Assert.True(File.Exists(observer), "x64sc-observer missing: " + observer);
        var probe = new RealResumeProbe();
        Directory.CreateDirectory(probe.OutputDirectory);
        var candidate = Path.Combine(probe.OutputDirectory, "roundtrip-candidate.vsf");
        var stderr = Path.Combine(probe.OutputDirectory, "roundtrip-writer.stderr");
        // The resume launch carries no model option, so the writer must use the resume process's default model:
        // the factory VICIIModel is 8565 (viciisc/vicii-resources.c 202), the C64C ("c64c", c64-cmdline-options.c 64).
        // "-model c64" selects the 6569 (line 134) and the pinned reader refuses the snapshot ("Snapshot was made with
        // model 0 while the current model is 1", vicii-snapshot.c 193 to 197; run 20261021T065157).
        var spec = new SnapshotFixtureSpec("x64sc-roundtrip", "x64sc", "c64c", [], trigger, observer, VsfSlice1RealEvidence.DataRoot,
            ["C64/basic-901226-01.bin", "C64/kernal-901227-03.bin", "C64/chargen-901225-01.bin", "DRIVES/dos1541-325302-01+901229-05.bin"],
            candidate, stderr, Environment.MachineName, LimitMargin: WindowCycles + 50);
        var written = new SnapshotFixtureGenerator(new ProcessCommandRunner()).Generate(spec);
        FixturePublisher.SaveCandidate(written, Path.Combine(probe.OutputDirectory, "roundtrip-writer.candidate.json"));
        Assert.True(written.WriterResult == 0 && written.CandidateSha256 is not null, "writer: " + written.Reason);
        var writeClock = written.WriteClock!.Value;
        var candidateBytes = File.ReadAllBytes(candidate);
        var cpu = X64ScNativeLaunch.ReadMainCpu(candidateBytes);
        Assert.Equal(4, cpu.Minor);
        Assert.Equal(writeClock, cpu.Clock);

        // Writer's post-checkpoint window: the OBS records after the checkpoint record, indices rebased.
        var writerWindow = WindowAfterMarker(stderr, "VICE_SNAP,checkpoint,", (int)(WindowCycles * 2));

        var input = InputFor(candidateBytes, normalize: false, WindowCycles, PreludeCycles);
        Assert.Empty(input.Precondition);
        var trace = probe.Run(input, candidate, normalize: false, "v14-roundtrip-resume");
        AssertResume(trace, input);
        var resumedWindow = trace.Lines.Where(l => l.StartsWith("VICE_OBS,1,", StringComparison.Ordinal)).Take((int)(WindowCycles * 2)).ToArray();
        Assert.Equal(writerWindow.Length, resumedWindow.Length);
        for (var i = 0; i < writerWindow.Length; i++)
            Assert.True(writerWindow[i] == resumedWindow[i], $"half-cycle {i} differs:\n writer  {writerWindow[i]}\n resumed {resumedWindow[i]}");
    }

    private static string[] WindowAfterMarker(string stderrPath, string markerPrefix, int halfCycles)
    {
        var kept = new List<string>();
        var started = false;
        long? first = null;
        foreach (var line in File.ReadLines(stderrPath))
        {
            if (!started) { if (line.StartsWith(markerPrefix, StringComparison.Ordinal)) started = true; continue; }
            if (!line.StartsWith("VICE_OBS,1,", StringComparison.Ordinal)) continue;
            var columns = line.Split(',');
            var raw = long.Parse(columns[2], NumberStyles.None, CultureInfo.InvariantCulture);
            first ??= raw;
            columns[2] = (raw - first.Value).ToString(CultureInfo.InvariantCulture);
            kept.Add(string.Join(',', columns));
            if (kept.Count == halfCycles) break;
        }
        return [.. kept];
    }
}
