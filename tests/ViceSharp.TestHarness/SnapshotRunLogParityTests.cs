using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Core.Media;
using ViceSharp.Monitor;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-VIC-CYCLE / TR-LOCKSTEP-VSF-001 / TEST-RUNLOG-PARITY-001.
///
/// Snapshot run-log parity: the offline flavour of the lockstep analyzer. A per-cycle
/// CPU-state log (cycle, PC, A, X, Y, S, P) is SAVED from a native VICE run resumed
/// from a .vsf (<see cref="LockstepValidator.SaveNativeRunLog"/>), and the managed C64
/// - resumed from the same .vsf with the staging RasterBarLockstepTests proves works
/// (CPU registers, 64K RAM, CPU port $00/$01, VIC register file + raster phase) - must
/// reproduce every per-cycle record. On the first divergence the test emits a failure
/// analysis that disassembles the code executing at the divergence point.
///
/// Contains the infra self-proofs (run-log round trip, analysis builder, native-log
/// capture + comparison, RunFromSnapshot with and without AV capture) so the slice is
/// green without committed fixtures; the theory itself discovers committed or ad hoc
/// (snapshot, run log) pairs and gates them exactly.
/// </summary>
[Collection("NativeVice")]
public sealed class SnapshotRunLogParityTests
{
    private const string ModelSelector = "c64";
    private const string ReadyFixtureName = "ready-c64sc-truedrive.vsf";

    /// <summary>Environment variable holding ad hoc cases: semicolon-separated "vsf|log" pairs.</summary>
    public const string AdHocCasesEnvironmentVariable = "VICESHARP_RUNLOG_CASES";

    private readonly ITestOutputHelper _output;

    public SnapshotRunLogParityTests(ITestOutputHelper output) => _output = output;

    // ------------------------------------------------------------------
    // Case discovery
    // ------------------------------------------------------------------

    /// <summary>
    /// Discovers (snapshotPath, runLogPath) theory cases: every
    /// tests/ViceSharp.TestHarness/Fixtures/RunLogs/*.cpulog with a sibling
    /// same-basename .vsf, plus ad hoc pairs from
    /// <see cref="AdHocCasesEnvironmentVariable"/> ("vsf|log;vsf|log"). Yields
    /// nothing when no cases are staged (the theory then skips via
    /// SkipTestWithoutData - verified because DisableDiscoveryEnumeration alone
    /// does not cover the zero-row case in xUnit v3).
    /// </summary>
    public static IEnumerable<object[]> DiscoverCases()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var runLogDirectory = LocateFixturePath("RunLogs", fileName: null);
        if (runLogDirectory is not null)
        {
            foreach (var logPath in Directory.EnumerateFiles(runLogDirectory, "*.cpulog")
                         .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var vsfPath = Path.ChangeExtension(logPath, ".vsf");
                if (File.Exists(vsfPath) && seen.Add(logPath))
                    yield return new object[] { vsfPath, logPath };
            }
        }

        var adHoc = Environment.GetEnvironmentVariable(AdHocCasesEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(adHoc))
        {
            foreach (var pair in adHoc.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = pair.Split('|', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && File.Exists(parts[0]) && File.Exists(parts[1]) && seen.Add(parts[1]))
                    yield return new object[] { parts[0], parts[1] };
            }
        }
    }

    // ------------------------------------------------------------------
    // The parity theory
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-VIC-CYCLE, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-RUNLOG-PARITY-001.
    /// Use case: replay a saved native VICE per-cycle CPU run log against the managed
    /// C64 resumed from the same .vsf - VICE runs once offline (SaveNativeRunLog), the
    /// log ships as a fixture, and the managed core must reproduce every per-cycle
    /// cycle/PC/A/X/Y/S/P record without native VICE in the loop per compare.
    /// Acceptance: for each discovered (snapshot, run log) pair the snapshot resumes in
    /// the shim (rc 0, used only to stage the managed machine), and every log entry
    /// equals the managed CPU state at that cycle (exact record equality). On the first
    /// divergence the test fails with the full analysis: divergence cycle, per-field
    /// expected/actual diff, the previous 8 entries from both sides, and a disassembly
    /// of the instruction stream around the divergence PC with a DIVERGES-HERE marker.
    /// Skips when no fixture pairs are staged or the native shim is absent.
    /// </summary>
    [ViceTheory(SkipTestWithoutData = true)]
    [MemberData(nameof(DiscoverCases))]
    public void SnapshotRun_MatchesViceRunLog(string snapshotPath, string runLogPath)
    {
        var expected = CpuRunLog.Load(runLogPath);
        Assert.True(expected.Count > 0, $"Run log '{runLogPath}' contains no entries.");

        using var native = ViceNative.CreateInstance(ModelSelector);
        native.Reset();
        var rc = native.ReadSnapshot(snapshotPath);
        Assert.True(rc == 0,
            $"Snapshot '{snapshotPath}' must resume in the shim to stage the managed machine; " +
            $"rc={rc}, snapshot_last_error={ViceNative.SnapshotLastError()}.");

        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);
        LockstepValidator.StageManagedFromNative(machine, native);

        var comparison = CompareManagedAgainstLog(machine, expected);
        if (comparison.DivergenceIndex >= 0)
            Assert.Fail(comparison.Analysis!);

        Assert.Equal(expected.Count, comparison.ManagedLog.Count);
        _output.WriteLine(
            $"managed matched all {expected.Count} run-log entries from {Path.GetFileName(runLogPath)} " +
            $"(snapshot {Path.GetFileName(snapshotPath)}).");
    }

    // ------------------------------------------------------------------
    // Self-proof 1: run-log persistence round trip
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: n/a (test infrastructure), TR: TR-DET-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: run logs captured on one machine are replayed on another, so the
    /// text format must round-trip losslessly and serialize byte-identically
    /// regardless of host locale or platform newline convention.
    /// Acceptance: 1000 synthetic entries (seeded RNG, full value ranges) survive
    /// Save then Load with exact record equality for every entry, and re-saving the
    /// loaded list produces a byte-identical file.
    /// </summary>
    [Fact]
    public void CpuRunLog_RoundTrip_1000SyntheticEntries_IsByteExact()
    {
        var rng = new Random(0x5EED);
        var entries = new List<CpuRunLogEntry>(1000);
        long cycle = 0;
        for (var i = 0; i < 1000; i++)
        {
            cycle += rng.Next(1, 4); // includes >1 gaps like a real native log
            entries.Add(new CpuRunLogEntry(
                cycle,
                (ushort)rng.Next(0x10000),
                (byte)rng.Next(0x100),
                (byte)rng.Next(0x100),
                (byte)rng.Next(0x100),
                (byte)rng.Next(0x100),
                (byte)rng.Next(0x100)));
        }

        var firstPath = Path.Combine(Path.GetTempPath(), $"vicesharp-runlog-{Guid.NewGuid():N}.cpulog");
        var secondPath = Path.Combine(Path.GetTempPath(), $"vicesharp-runlog-{Guid.NewGuid():N}.cpulog");
        try
        {
            CpuRunLog.Save(firstPath, entries, "self-proof synthetic");
            var loaded = CpuRunLog.Load(firstPath);

            Assert.Equal(entries.Count, loaded.Count);
            for (var i = 0; i < entries.Count; i++)
                Assert.Equal(entries[i], loaded[i]);

            CpuRunLog.Save(secondPath, loaded, "self-proof synthetic");
            Assert.Equal(File.ReadAllBytes(firstPath), File.ReadAllBytes(secondPath));
        }
        finally
        {
            if (File.Exists(firstPath)) File.Delete(firstPath);
            if (File.Exists(secondPath)) File.Delete(secondPath);
        }
    }

    // ------------------------------------------------------------------
    // Self-proof 2: SaveNativeRunLog + the theory's comparison routine
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-VIC-CYCLE, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-RUNLOG-PARITY-001.
    /// Use case: end-to-end infra proof of the capture half - save a 2000-cycle
    /// per-cycle CPU run log from native VICE resumed from the READY fixture .vsf,
    /// then run the SAME comparison routine the theory uses against a managed
    /// machine resumed from that snapshot.
    /// Acceptance: infra proof, honest about BUG-LOCKSTEP-001 - either the managed
    /// machine matches all 2000 entries (zero divergence), or the comparison
    /// produces a well-formed analysis containing "DIVERGES HERE", the divergence
    /// cycle number, and at least 5 disassembly lines; the actual divergence
    /// content is logged via ITestOutputHelper as real signal, never masked.
    /// Skips when the shim or the READY fixture is absent.
    /// </summary>
    [ViceFact]
    public void SaveNativeRunLog_ReadyFixture_TheoryComparisonIsWellFormed()
    {
        var vsf = LocateFixturePath("Vsf", ReadyFixtureName);
        if (vsf is null)
        {
            Assert.Skip("READY fixture .vsf not present.");
            return;
        }

        const int Cycles = 2000;
        var logPath = Path.Combine(Path.GetTempPath(), $"vicesharp-nativelog-{Guid.NewGuid():N}.cpulog");
        try
        {
            LockstepValidator.SaveNativeRunLog(vsf, Cycles, logPath);
            var expected = CpuRunLog.Load(logPath);
            Assert.Equal(Cycles, expected.Count);

            using var native = ViceNative.CreateInstance(ModelSelector);
            native.Reset();
            var rc = native.ReadSnapshot(vsf);
            Assert.True(rc == 0, $"snapshot must resume; rc={rc}, err={ViceNative.SnapshotLastError()}");

            var machine = MachineTestFactory.CreateC64Machine(ModelSelector);
            LockstepValidator.StageManagedFromNative(machine, native);

            var comparison = CompareManagedAgainstLog(machine, expected);
            if (comparison.DivergenceIndex < 0)
            {
                _output.WriteLine($"managed stayed in lockstep with the native run log for all {expected.Count} entries.");
                return;
            }

            _output.WriteLine($"REAL DIVERGENCE (BUG-LOCKSTEP-001 signal) at log index {comparison.DivergenceIndex}:");
            _output.WriteLine(comparison.Analysis!);

            Assert.Contains("DIVERGES HERE", comparison.Analysis!, StringComparison.Ordinal);
            Assert.Contains($"cycle {expected[comparison.DivergenceIndex].Cycle}", comparison.Analysis!, StringComparison.Ordinal);
            var disassemblyLines = comparison.Analysis!
                .Split(Environment.NewLine)
                .Count(line => line.TrimStart().StartsWith('$'));
            Assert.True(disassemblyLines >= 5,
                $"analysis must contain at least 5 disassembly lines; found {disassemblyLines}.");
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    // ------------------------------------------------------------------
    // Self-proof 3: analysis builder over synthetic logs + memory
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: n/a (test infrastructure), TR: TR-LOCKSTEP-VSF-001 (failure-analysis surface).
    /// Use case: when the parity theory diverges, the developer needs the divergence
    /// localized without re-running anything - the analysis must name the cycle, diff
    /// the register fields, and disassemble the instruction stream at the divergence.
    /// Acceptance: feeding two synthetic 7-entry logs that diverge at index 6 (A: $1B
    /// vs $00 at PC $1005) over a synthetic memory image holding a known instruction
    /// sequence yields an analysis containing "cycle 6", the exact field-diff line
    /// "A: expected=$1B actual=$00 &lt;-- differs", and a single marker line carrying
    /// both ">>> DIVERGES HERE" and the mnemonic "LDX #$01" decoded at $1005.
    /// </summary>
    [Fact]
    public void BuildDivergenceAnalysis_SyntheticDivergence_ReportsCycleFieldDiffAndMnemonic()
    {
        var memory = new byte[0x10000];
        // $1000: LDA #$1B / STA $0314 / LDX #$01 / STX $D01A / NOP x10
        var program = new byte[]
        {
            0xA9, 0x1B, 0x8D, 0x14, 0x03, 0xA2, 0x01, 0x8E, 0x1A, 0xD0,
            0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA,
        };
        Array.Copy(program, 0, memory, 0x1000, program.Length);

        ushort[] pcs = [0x1000, 0x1000, 0x1002, 0x1002, 0x1002, 0x1002, 0x1005];
        var expected = new List<CpuRunLogEntry>(pcs.Length);
        var actual = new List<CpuRunLogEntry>(pcs.Length);
        for (var i = 0; i < pcs.Length; i++)
        {
            var a = i < 2 ? (byte)0x00 : (byte)0x1B;
            expected.Add(new CpuRunLogEntry(i, pcs[i], a, 0x00, 0x00, 0xFD, 0x24));
            // Managed lost the accumulator load: A stays $00, diverging at index 6.
            actual.Add(new CpuRunLogEntry(i, pcs[i], i == 6 ? (byte)0x00 : a, 0x00, 0x00, 0xFD, 0x24));
        }

        var analysis = BuildDivergenceAnalysis(address => memory[address], expected, actual, 6);
        _output.WriteLine(analysis);

        Assert.Contains("cycle 6", analysis, StringComparison.Ordinal);
        Assert.Contains("A: expected=$1B actual=$00 <-- differs", analysis, StringComparison.Ordinal);

        var markerLines = analysis
            .Split(Environment.NewLine)
            .Where(line => line.Contains(">>> DIVERGES HERE", StringComparison.Ordinal))
            .ToArray();
        var markerLine = Assert.Single(markerLines);
        Assert.Contains("LDX #$01", markerLine, StringComparison.Ordinal);
        Assert.Contains("$1005", markerLine, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------
    // Self-proof 4 (scope addendum 1): snapshot-staged lockstep run
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-VIC-CYCLE, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-RUNLOG-PARITY-001.
    /// Use case: the lockstep analyzer itself must support starting from a VICE
    /// snapshot, not only from reset - resume the READY fixture .vsf on BOTH sides
    /// (native via ReadSnapshot, managed via the proven .vsf staging) and run the
    /// existing per-cycle compare loop for 5000 cycles.
    /// Acceptance: infra proof, honest about BUG-LOCKSTEP-001 - either
    /// <see cref="LockstepValidator.RunFromSnapshot"/> reports Success with exactly
    /// 5000 cycles executed, or the divergence report is well-formed (a non-negative
    /// mismatch cycle plus a register/cycle field difference between expected and
    /// actual states); the actual divergence content and recent trace are logged via
    /// ITestOutputHelper as real signal, never masked. Skips when the shim or the
    /// READY fixture is absent.
    /// </summary>
    [ViceFact]
    public void RunFromSnapshot_ReadyFixture_PassesOrReportsWellFormedDivergence()
    {
        var vsf = LocateFixturePath("Vsf", ReadyFixtureName);
        if (vsf is null)
        {
            Assert.Skip("READY fixture .vsf not present.");
            return;
        }

        const long Cycles = 5000;
        using var validator = new LockstepValidator(ModelSelector);
        var report = validator.RunFromSnapshot(vsf, Cycles);

        if (report.Success)
        {
            Assert.Equal(Cycles, report.TotalCyclesExecuted);
            _output.WriteLine($"RunFromSnapshot held lockstep for all {Cycles} cycles from {Path.GetFileName(vsf)}.");
            return;
        }

        Assert.NotNull(report.Mismatch);
        var mismatch = report.Mismatch!.Value;
        _output.WriteLine($"REAL DIVERGENCE (BUG-LOCKSTEP-001 signal) at cycle {report.FirstMismatchCycle}:");
        _output.WriteLine(
            $"expected (VICE)   PC=${mismatch.Expected.PC:X4} A=${mismatch.Expected.A:X2} X=${mismatch.Expected.X:X2} " +
            $"Y=${mismatch.Expected.Y:X2} S=${mismatch.Expected.S:X2} P=${mismatch.Expected.P:X2} cyc={mismatch.Expected.Cycle}");
        _output.WriteLine(
            $"actual (managed)  PC=${mismatch.Actual.PC:X4} A=${mismatch.Actual.A:X2} X=${mismatch.Actual.X:X2} " +
            $"Y=${mismatch.Actual.Y:X2} S=${mismatch.Actual.S:X2} P=${mismatch.Actual.P:X2} cyc={mismatch.Actual.Cycle}");
        _output.WriteLine("recent trace:");
        _output.WriteLine(validator.FormatRecentTrace());

        Assert.True(report.FirstMismatchCycle >= 0, "divergence report must carry the mismatch cycle number.");
        var hasFieldDifference =
            mismatch.Expected.A != mismatch.Actual.A ||
            mismatch.Expected.X != mismatch.Actual.X ||
            mismatch.Expected.Y != mismatch.Actual.Y ||
            mismatch.Expected.S != mismatch.Actual.S ||
            mismatch.Expected.P != mismatch.Actual.P ||
            mismatch.Expected.PC != mismatch.Actual.PC ||
            mismatch.Expected.Cycle != mismatch.Actual.Cycle;
        Assert.True(hasFieldDifference, "divergence report must show a register or cycle field difference.");
    }

    // ------------------------------------------------------------------
    // Self-proof 5 (scope addendum 2): AV capture of the snapshot run
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-MED-004, TR: TR-LOCKSTEP-VSF-001 / TR-MEDIA-VIDEO-FFMPEG-001.
    /// Use case: the lockstep analyzer must be able to save video WITH audio of a
    /// snapshot-staged run - resume the READY fixture, run ~3 PAL frames through
    /// <see cref="LockstepValidator.RunFromSnapshot"/> with a temp .mp4 path so the
    /// managed VIC frames (per PAL frame boundary) and managed SID samples (44.1 kHz
    /// mono) are muxed through the external-ffmpeg Media recorder.
    /// Acceptance: the output file exists and is non-trivially sized (&gt; 1024
    /// bytes), and - when ffprobe is available alongside ffmpeg - the container
    /// holds exactly the expected stream kinds: one video and one audio stream.
    /// Lockstep divergence during the run does not fail this proof (the capture
    /// covers the full run regardless); the report outcome is logged. Skips when
    /// ffmpeg, the shim, or the READY fixture is absent.
    /// </summary>
    [ViceFact]
    public void RunFromSnapshot_WithVideoCapture_ProducesMuxedVideoAndAudioStreams()
    {
        var ffmpeg = FfmpegLocator.Locate();
        if (ffmpeg is null)
        {
            Assert.Skip("ffmpeg not installed - skipping lockstep AV capture self-proof.");
            return;
        }

        var vsf = LocateFixturePath("Vsf", ReadyFixtureName);
        if (vsf is null)
        {
            Assert.Skip("READY fixture .vsf not present.");
            return;
        }

        const long Cycles = 3 * 19656; // ~3 PAL frames
        var outPath = Path.Combine(Path.GetTempPath(), $"vicesharp-lockstep-av-{Guid.NewGuid():N}.mp4");
        try
        {
            using var validator = new LockstepValidator(ModelSelector);
            var report = validator.RunFromSnapshot(vsf, Cycles, videoOutPath: outPath);
            _output.WriteLine(report.Success
                ? $"lockstep held for all {Cycles} captured cycles."
                : $"lockstep diverged at cycle {report.FirstMismatchCycle}; capture still covers the full run.");

            Assert.True(File.Exists(outPath), "AV capture must produce the requested video file.");
            var size = new FileInfo(outPath).Length;
            Assert.True(size > 1024, $"AV capture output is suspiciously small ({size} bytes).");

            var streams = ProbeStreamTypes(ffmpeg, outPath);
            if (streams is not null)
            {
                _output.WriteLine($"ffprobe stream kinds: {streams.Replace("\n", ",").TrimEnd(',')}");
                Assert.Contains("video", streams, StringComparison.Ordinal);
                Assert.Contains("audio", streams, StringComparison.Ordinal);
            }
            else
            {
                _output.WriteLine("ffprobe not found alongside ffmpeg; stream-kind assertion skipped (file size + existence proven).");
            }
        }
        finally
        {
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    // ------------------------------------------------------------------
    // Shared comparison + analysis (used by the theory and the self-proofs)
    // ------------------------------------------------------------------

    private readonly record struct RunLogComparison(
        int DivergenceIndex,
        string? Analysis,
        IReadOnlyList<CpuRunLogEntry> ManagedLog);

    /// <summary>
    /// The comparison routine the theory uses: for each expected entry, advance the
    /// managed machine to the entry's cycle (a native master step can advance more
    /// than one cycle, so the log may carry gaps), record the managed state, and
    /// compare the full record exactly. Returns the first divergence index with the
    /// built analysis, or -1 when every entry matched.
    /// </summary>
    private static RunLogComparison CompareManagedAgainstLog(IMachine machine, IReadOnlyList<CpuRunLogEntry> expected)
    {
        var managedLog = new List<CpuRunLogEntry>(expected.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            var want = expected[i];
            var state = machine.GetState();
            var guard = 0L;
            while (state.Cycle < want.Cycle)
            {
                machine.Clock.Step();
                state = machine.GetState();
                if (++guard > 1_000_000)
                {
                    throw new InvalidOperationException(
                        $"Managed machine did not reach log cycle {want.Cycle} within 1,000,000 steps (stuck at {state.Cycle}).");
                }
            }

            var got = new CpuRunLogEntry(state.Cycle, state.PC, state.A, state.X, state.Y, state.S, state.P);
            managedLog.Add(got);

            if (got != want)
            {
                var analysis = BuildDivergenceAnalysis(machine.Bus.Peek, expected, managedLog, i);
                return new RunLogComparison(i, analysis, managedLog);
            }
        }

        return new RunLogComparison(-1, null, managedLog);
    }

    /// <summary>
    /// Builds the failure analysis for a run-log divergence: (1) divergence cycle +
    /// field-by-field expected/actual diff, (2) the previous 8 log entries from both
    /// sides, (3) a disassembly of the instruction stream from up to 24 bytes before
    /// the divergence PC through 8 instructions after it (stream-walked with
    /// <see cref="Mos6502Disassembler.OpcodeLength"/> so absolute-mode stores never
    /// desync), with a ">>> DIVERGES HERE" marker on the instruction whose fetch PC
    /// equals the divergence entry's PC, and (4) both sides' registers in hex.
    /// </summary>
    private static string BuildDivergenceAnalysis(
        Func<ushort, byte> peek,
        IReadOnlyList<CpuRunLogEntry> expected,
        IReadOnlyList<CpuRunLogEntry> actual,
        int divergenceIndex)
    {
        var nl = Environment.NewLine;
        var want = expected[divergenceIndex];
        var got = actual[divergenceIndex];
        var sb = new StringBuilder(4096);

        sb.Append("==== RUN-LOG DIVERGENCE ANALYSIS ====").Append(nl);
        sb.Append($"divergence at log index {divergenceIndex}, cycle {want.Cycle}").Append(nl);
        sb.Append(nl);

        sb.Append("field diff (expected = VICE run log, actual = managed):").Append(nl);
        AppendFieldDiff(sb, "Cycle", want.Cycle.ToString(), got.Cycle.ToString(), nl);
        AppendFieldDiff(sb, "PC", $"${want.PC:X4}", $"${got.PC:X4}", nl);
        AppendFieldDiff(sb, "A", $"${want.A:X2}", $"${got.A:X2}", nl);
        AppendFieldDiff(sb, "X", $"${want.X:X2}", $"${got.X:X2}", nl);
        AppendFieldDiff(sb, "Y", $"${want.Y:X2}", $"${got.Y:X2}", nl);
        AppendFieldDiff(sb, "S", $"${want.S:X2}", $"${got.S:X2}", nl);
        AppendFieldDiff(sb, "P", $"${want.P:X2}", $"${got.P:X2}", nl);
        sb.Append(nl);

        sb.Append("last entries up to the divergence (expected | actual):").Append(nl);
        for (var i = Math.Max(0, divergenceIndex - 8); i <= divergenceIndex; i++)
        {
            var e = expected[i];
            var marker = i == divergenceIndex ? "  <-- first divergence" : string.Empty;
            if (i < actual.Count)
            {
                var a = actual[i];
                sb.Append(
                    $"  [{i}] cyc {e.Cycle}: vice PC=${e.PC:X4} A=${e.A:X2} X=${e.X:X2} Y=${e.Y:X2} S=${e.S:X2} P=${e.P:X2}" +
                    $" | managed PC=${a.PC:X4} A=${a.A:X2} X=${a.X:X2} Y=${a.Y:X2} S=${a.S:X2} P=${a.P:X2}{marker}").Append(nl);
            }
            else
            {
                sb.Append(
                    $"  [{i}] cyc {e.Cycle}: vice PC=${e.PC:X4} A=${e.A:X2} X=${e.X:X2} Y=${e.Y:X2} S=${e.S:X2} P=${e.P:X2}" +
                    $" | managed (not captured){marker}").Append(nl);
            }
        }

        sb.Append(nl).Append("disassembly around the divergence PC (managed memory):").Append(nl);
        AppendDisassembly(sb, peek, want.PC, nl);

        sb.Append(nl).Append("state at divergence:").Append(nl);
        sb.Append(
            $"  expected (VICE):  PC=${want.PC:X4} A=${want.A:X2} X=${want.X:X2} Y=${want.Y:X2} S=${want.S:X2} P=${want.P:X2}").Append(nl);
        sb.Append(
            $"  actual (managed): PC=${got.PC:X4} A=${got.A:X2} X=${got.X:X2} Y=${got.Y:X2} S=${got.S:X2} P=${got.P:X2}").Append(nl);

        return sb.ToString();
    }

    private static void AppendFieldDiff(StringBuilder sb, string name, string expectedText, string actualText, string nl)
    {
        var suffix = string.Equals(expectedText, actualText, StringComparison.Ordinal)
            ? string.Empty
            : " <-- differs";
        sb.Append($"  {name}: expected={expectedText} actual={actualText}{suffix}").Append(nl);
    }

    private static void AppendDisassembly(StringBuilder sb, Func<ushort, byte> peek, ushort divergencePc, string nl)
    {
        const int MaxLookBehindBytes = 24;
        const int InstructionsAfterMarker = 8;

        var pc = FindAlignedDisassemblyStart(peek, divergencePc, MaxLookBehindBytes);
        var printedAfterMarker = 0;
        var sawMarker = false;
        while (true)
        {
            if (!sawMarker && pc > divergencePc)
            {
                // The backward walk desynced past the target (self-modifying or data
                // bytes); snap to the divergence PC so the marker line always appears.
                pc = divergencePc;
            }

            var opcode = peek((ushort)pc);
            var length = Mos6502Disassembler.OpcodeLength(opcode);
            var byteText = new StringBuilder(9);
            for (var k = 0; k < length; k++)
            {
                if (k > 0) byteText.Append(' ');
                byteText.Append(peek((ushort)(pc + k)).ToString("X2"));
            }

            var marker = pc == divergencePc ? "   >>> DIVERGES HERE" : string.Empty;
            sb.Append($"  ${(ushort)pc:X4}: {byteText,-9} {Mos6502Disassembler.Decode((ushort)pc, peek)}{marker}").Append(nl);

            if (pc == divergencePc)
                sawMarker = true;
            else if (sawMarker && ++printedAfterMarker >= InstructionsAfterMarker)
                break;

            pc += length;
            if (pc > 0xFFFF)
                break;
        }
    }

    /// <summary>
    /// Finds the farthest instruction-aligned start within <paramref name="maxLookBehind"/>
    /// bytes before <paramref name="targetPc"/>: a start is aligned when stream-walking
    /// forward by decoded opcode lengths lands exactly on the target (the same walk
    /// idiom Mos6502DisassemblerTests proves never desyncs). Falls back to the target
    /// itself when no aligned start exists.
    /// </summary>
    private static int FindAlignedDisassemblyStart(Func<ushort, byte> peek, ushort targetPc, int maxLookBehind)
    {
        for (var back = maxLookBehind; back >= 1; back--)
        {
            var start = targetPc - back;
            if (start < 0)
                continue;

            var pc = start;
            while (pc < targetPc)
                pc += Mos6502Disassembler.OpcodeLength(peek((ushort)pc));

            if (pc == targetPc)
                return start;
        }

        return targetPc;
    }

    // ------------------------------------------------------------------
    // Fixture + probe helpers
    // ------------------------------------------------------------------

    private static string? LocateFixturePath(string subdirectory, string? fileName)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 12 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "tests", "ViceSharp.TestHarness", "Fixtures", subdirectory);
            if (fileName is not null)
                candidate = Path.Combine(candidate, fileName);

            if (fileName is null ? Directory.Exists(candidate) : File.Exists(candidate))
                return candidate;

            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }

        return null;
    }

    // Returns the newline-joined codec_type list (e.g. "video\naudio") via ffprobe,
    // or null when ffprobe is not alongside ffmpeg (same idiom as FfmpegVideoRecorderTests).
    private static string? ProbeStreamTypes(string ffmpegPath, string mediaPath)
    {
        var dir = Path.GetDirectoryName(ffmpegPath)!;
        var probe = Path.Combine(dir, OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");
        if (!File.Exists(probe))
            return null;

        var psi = new ProcessStartInfo
        {
            FileName = probe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var arg in new[]
                 {
                     "-v", "error",
                     "-show_entries", "stream=codec_type",
                     "-of", "csv=p=0",
                     mediaPath,
                 })
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(10_000);
        return output;
    }
}
