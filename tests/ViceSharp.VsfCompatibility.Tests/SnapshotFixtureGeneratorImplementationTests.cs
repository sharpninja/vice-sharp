using System.Security.Cryptography;
using System.Text.Json.Nodes;
using ViceSharp.ViceCompatibility.SnapshotFixtures;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (R12, N14, N21, N25): the fixture generator, classifier and publisher bound to a mock
/// command runner that writes a synthetic candidate and answers with the checkpoint records of each case. Covers
/// the exact invocation (arguments, VICESHARP_SNAPSHOT_AT, ROM allowlist before launch), the matching case, a
/// pending IRQ or NMI and a stolen-cycle boundary at the trigger (write clock later than the trigger: BLOCKED), a
/// nonzero writer result, an absent record, a MAINCPU clock that differs from the write clock, one-host and
/// both-host mismatches, differing bytes across hosts, publication of an accepted fixture only, and the manifest
/// upsert. The real binaries run in D5 through NativeViceSnapshotFixtureGeneratorTests.Regeneration_IsByteDeterministic.
/// </summary>
public sealed class SnapshotFixtureGeneratorImplementationTests : IDisposable
{
    private const ulong Trigger = 100_000;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vicesharp-fixturegen-" + Guid.NewGuid().ToString("N"));
    private readonly string _dataRoot;
    private readonly string _observer;
    private readonly Dictionary<string, string> _allowlist = new(StringComparer.Ordinal);

    private static readonly string[] Roms = ["VIC20/basic-901486-01.bin", "VIC20/kernal.901486-07.bin", "VIC20/chargen-901460-03.bin"];

    public SnapshotFixtureGeneratorImplementationTests()
    {
        _dataRoot = Path.Combine(_root, "data");
        foreach (var rom in Roms)
        {
            var path = Path.Combine(_dataRoot, rom.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var bytes = new byte[64];
            bytes[0] = (byte)rom.Length;
            File.WriteAllBytes(path, bytes);
            _allowlist[rom] = Convert.ToHexString(SHA256.HashData(bytes));
        }
        _observer = Path.Combine(_root, "bin", "xvic-observer");
        Directory.CreateDirectory(Path.GetDirectoryName(_observer)!);
        File.WriteAllText(_observer, "fake observer");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class FakeRunner(Func<CommandRequest, CommandResult> handler) : ICommandRunner
    {
        public List<CommandRequest> Requests { get; } = [];
        public CommandResult Run(CommandRequest request) { Requests.Add(request); return handler(request); }
    }

    private static string CandidatePathOf(CommandRequest request) => request.Environment[SnapshotFixtureGenerator.CheckpointVariable].Split(':', 2)[1];

    private static byte[] SyntheticSnapshot(ulong mainCpuClock) => VsfBuilder.Build(VsfBuilder.MainCpu(1, 4, mainCpuClock, 0xE000, 0x24));

    /// <summary>A runner that writes the candidate with the given MAINCPU clock and reports the given write clock.</summary>
    /// <summary>The pinned VICE ends a -limitcycles run with "cycle limit reached." and exit code 1 (mainc64cpu.c 813, maincpu.c 640); the fake does the same by default.</summary>
    private static FakeRunner Runner(ulong writeClock, ulong mainCpuClock, int writerResult = 0, bool writeBytes = true, int exitCode = 1, bool limitReached = true)
        => new(request =>
        {
            if (writeBytes) File.WriteAllBytes(CandidatePathOf(request), SyntheticSnapshot(mainCpuClock));
            var line = writerResult == 0
                ? $"VICE_SNAP,checkpoint,{Trigger},{writeClock}"
                : $"VICE_SNAP,checkpoint-fail,{Trigger},{writeClock},{writerResult}";
            return new CommandResult(exitCode, false, [line], limitReached);
        });

    [Fact]
    public void LimitExit_IsTheNormalEnd_AndAnyOtherNonzeroExitFails()
    {
        var limit = new SnapshotFixtureGenerator(Runner(Trigger, Trigger, exitCode: 1, limitReached: true), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.AcceptedExit, limit.ExitCode);
        Assert.True(limit.LimitReached);
        Assert.Equal(1, limit.ProcessExitCode);
        var clean = new SnapshotFixtureGenerator(Runner(Trigger, Trigger, exitCode: 0, limitReached: false), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.AcceptedExit, clean.ExitCode);
        var crash = new SnapshotFixtureGenerator(Runner(Trigger, Trigger, exitCode: 1, limitReached: false), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.FailedExit, crash.ExitCode);
        Assert.Contains("without reaching the cycle limit", crash.Reason);
        var other = new SnapshotFixtureGenerator(Runner(Trigger, Trigger, exitCode: 66, limitReached: true), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.FailedExit, other.ExitCode);
    }

    /// <summary>
    /// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17. Use case: VICE sends Windows errors to its file logger.
    /// Acceptance: a fresh run records its log path, accepts a proven cycle limit, and rejects an unproven exit code 1.
    /// </summary>
    [Fact]
    public void LimitExit_RequiresEvidenceFromThisObserverRun()
    {
        var spec = Spec();
        var simulatedLog = "Error - exit before the limit.";
        var runner = new FakeRunner(request =>
        {
            var logPath = Assert.IsType<string>(request.ViceLogPath);
            Assert.Equal(new[] { "-logfile", logPath }, request.Arguments.TakeLast(2));
            Assert.False(File.Exists(logPath));
            using (var log = new StreamWriter(File.Open(logPath, FileMode.CreateNew)))
                log.WriteLine(simulatedLog);
            var limitRecord = File.ReadAllText(logPath).Contains("cycle limit reached.", StringComparison.Ordinal);
            File.WriteAllBytes(CandidatePathOf(request), SyntheticSnapshot(Trigger));
            return new CommandResult(1, false, [$"VICE_SNAP,checkpoint,{Trigger},{Trigger}"], limitRecord);
        });
        var generator = new SnapshotFixtureGenerator(runner, _allowlist);
        var missing = generator.Generate(spec);
        Assert.Equal(FixtureCandidate.FailedExit, missing.ExitCode);

        simulatedLog = "Error - cycle limit reached.";
        var witnessed = generator.Generate(spec);
        Assert.Equal(FixtureCandidate.AcceptedExit, witnessed.ExitCode);
        Assert.NotEqual(runner.Requests[0].ViceLogPath, runner.Requests[1].ViceLogPath);
        Assert.Contains(runner.Requests[1].ViceLogPath!, witnessed.Command, StringComparison.Ordinal);
    }

    /// <summary>
    /// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17. Use case: compare stock VIC-20 snapshots.
    /// Acceptance: the two unused VIA1 v2.2 input-latch bytes may differ, and adjacent state must match.
    /// </summary>
    [Fact]
    public void PinnedXvicComparison_UsesOnlyTheTwoUnusedInputLatchBytes()
    {
        AssertPinnedViaComparison((left, right) =>
        {
            const string id = "xvic-pal-unexpanded";
            string[] hosts = ["LAB-OMARCHY", "PAYTON-LEGION2"];
            var runs = new[] { PinnedViaRun(left, hosts[0], 1), PinnedViaRun(left, hosts[0], 2),
                PinnedViaRun(right, hosts[1], 1), PinnedViaRun(right, hosts[1], 2) };
            var comparison = SnapshotFixtureClassifier.ClassifyPinnedXvic(id, 100_002, runs, hosts, FixtureManifest.PinnedViceCommit);
            if (comparison.IsAccepted)
            {
                Assert.Equal(runs[0].Receipt.CandidateSha256, comparison.AcceptedSha256);
                Assert.Equal(runs[2].Receipt.CandidateSha256, comparison.Clocks[2].CandidateSha256);
                Assert.NotEqual(comparison.AcceptedSha256, comparison.Clocks[2].CandidateSha256);
                Assert.Contains("23 (ILA) and 24 (ILB)", comparison.ByteComparisonException);
                Assert.False(SnapshotFixtureClassifier.ClassifyPinnedXvic(id, 100_002, runs, hosts, "different-pin").IsAccepted);
                runs[3] = (runs[3].Receipt, (byte[])left.Clone());
                Assert.False(SnapshotFixtureClassifier.ClassifyPinnedXvic(id, 100_002, runs, hosts, FixtureManifest.PinnedViceCommit).IsAccepted);
                runs[3] = runs[2];
                Assert.False(SnapshotFixtureClassifier.ClassifyPinnedXvic(id, 100_002, runs, hosts, FixtureManifest.PinnedViceCommit).IsAccepted);
            }
            return comparison.IsAccepted;
        });
    }

    private static (FixtureCandidate Receipt, byte[]? Bytes) PinnedViaRun(byte[] bytes, string host, int run) => (new()
    {
        FixtureId = "xvic-pal-unexpanded", Machine = "xvic", Model = "vic20", Host = host,
        Trigger = 100_002, WriteClock = 100_002, MainCpuClock = 100_002, WriterResult = 0,
        CandidatePath = $"{host}-{run}.vsf",
        CandidateSha256 = Convert.ToHexString(SHA256.HashData(bytes)), CandidateLength = bytes.LongLength,
        ExitCode = FixtureCandidate.AcceptedExit,
    }, bytes);

    private static void AssertPinnedViaComparison(Func<byte[], byte[], bool> compare)
    {
        const int payload = 58 + 22;
        var baseline = PinnedXvicSnapshot(new byte[28]);
        var sameState = (byte[])baseline.Clone();
        sameState[payload + 23] = 0x5c;
        sameState[payload + 24] = 0x61;
        Assert.True(compare(baseline, sameState));
        foreach (var offset in new[] { 21, payload + 16, payload + 22, payload + 25, sameState.Length - 1 })
        {
            var changed = (byte[])sameState.Clone();
            changed[offset] ^= 1;
            Assert.False(compare(baseline, changed));
        }
        var enabledLeft = (byte[])baseline.Clone();
        var enabledRight = (byte[])sameState.Clone();
        enabledLeft[payload + 16] = enabledRight[payload + 16] = 1;
        Assert.False(compare(enabledLeft, enabledRight));
        enabledLeft[payload + 16] = enabledRight[payload + 16] = 0;
        enabledLeft[58 + 5] = enabledRight[58 + 5] = 1;
        Assert.False(compare(enabledLeft, enabledRight));
        Assert.Equal(0, baseline[payload + 23]);
        Assert.Equal(0x5c, sameState[payload + 23]);
        Assert.Equal(0x61, sameState[payload + 24]);
    }

    private static byte[] PinnedXvicSnapshot(byte[] viaPayload)
    {
        var snapshot = VsfBuilder.Build(new("VIA1", 2, 2, viaPayload), VsfBuilder.MainCpu(1, 4, 100_002, 0xE000, 0x24));
        snapshot[19] = 3;
        snapshot[20] = 1;
        "VIC20"u8.CopyTo(snapshot.AsSpan(21));
        return snapshot;
    }

    private SnapshotFixtureSpec Spec(string host = "LAB-OMARCHY", string run = "1", ulong trigger = Trigger) => new(
        "xvic-pal-unexpanded", "xvic", "vic20", ["-memory", "none"], trigger, _observer, _dataRoot, Roms,
        Path.Combine(_root, "candidates", $"{host}-{run}.vsf"), Path.Combine(_root, "candidates", $"{host}-{run}.stderr"), host);

    /// <summary>
    /// 2026-11-23, PLAN-NATIVERESTORE-001 D5b/R17. Use case: launch a fixture observer with auditable inputs.
    /// Acceptance: the trace keeps the baseline machine options and includes the exact per-run VICE log path.
    /// </summary>
    [Fact]
    public void Launch_UsesTheExactArgumentsAndTheCheckpointVariable_AndVerifiesRomsFirst()
    {
        var runner = Runner(Trigger, Trigger);
        var candidate = new SnapshotFixtureGenerator(runner, _allowlist).Generate(Spec());
        var request = Assert.Single(runner.Requests);
        Assert.Equal(_observer, request.FileName);
        // +logcolorize first: an option that logs while parsed ("-model c64", "-memory none") crashes the pinned
        // binaries in log.c 615 when colors are still enabled and stdout is a pipe (cause pinned 2026-10-21T05:57).
        var logPath = Assert.IsType<string>(request.ViceLogPath);
        Assert.StartsWith(Spec().StderrPath + ".", logPath, StringComparison.Ordinal);
        Assert.EndsWith(".vice.log", logPath, StringComparison.Ordinal);
        Assert.Equal(["-default", "+logcolorize", "-seed", "1", "-sounddev", "dummy", "-directory", _dataRoot, "-limitcycles", "150000", "-model", "vic20", "-memory", "none", "-logfile", logPath], request.Arguments);
        Assert.Equal($"{Trigger}:{Spec().CandidatePath}", request.Environment[SnapshotFixtureGenerator.CheckpointVariable]);
        Assert.Equal(Path.GetDirectoryName(_observer), request.WorkingDirectory);
        Assert.Equal(150_000UL, request.PermitCycles);   // the gated observer needs one permit per cycle up to the limit
        Assert.All(candidate.Roms, r => Assert.True(r.Ok, r.Reason));
        Assert.Equal(3, candidate.Roms.Count);
        Assert.Contains("-limitcycles 150000", candidate.Command);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_observer))), candidate.ObserverSha256);
    }

    [Fact]
    public void MatchingWriteClockAndMainCpuClock_IsAnAcceptedCandidate_ThatPublishesNothing()
    {
        var candidate = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.AcceptedExit, candidate.ExitCode);
        Assert.Equal("accepted-candidate", candidate.Outcome);
        Assert.Equal(Trigger, candidate.WriteClock);
        Assert.Equal(Trigger, candidate.MainCpuClock);
        Assert.Equal(0, candidate.WriterResult);
        Assert.Empty(candidate.BlockedIds);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(SyntheticSnapshot(Trigger))), candidate.CandidateSha256);
        Assert.Equal(SyntheticSnapshot(Trigger).LongLength, candidate.CandidateLength);
        Assert.False(candidate.AcceptedFixtureChanged);
        Assert.False(candidate.ManifestChanged);
    }

    [Theory]
    [InlineData(7, "pending IRQ or NMI: the trap runs after the handler's first opcode")]
    [InlineData(3, "stolen cycles at the trigger: the boundary lands later")]
    [InlineData(1, "the trigger fell inside an instruction")]
    public void WriteClockLaterThanTheTrigger_IsBlocked_WithBothClocksAndTheRetainedCandidate(int delta, string context)
    {
        var writeClock = Trigger + (ulong)delta;
        var candidate = new SnapshotFixtureGenerator(Runner(writeClock, writeClock), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.BlockedExit, candidate.ExitCode);
        Assert.Equal("blocked", candidate.Outcome);
        Assert.Equal(Trigger, candidate.Trigger);
        Assert.Equal(writeClock, candidate.WriteClock);
        Assert.Contains(SnapshotFixtureClassifier.BlockerId, candidate.Reason);
        Assert.Contains($"measured write clock {writeClock}", candidate.Reason);
        Assert.Contains($"delta {delta}", candidate.Reason);
        Assert.Equal(["xvic-pal-unexpanded"], candidate.BlockedIds);
        Assert.True(File.Exists(candidate.CandidatePath), context);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(SyntheticSnapshot(writeClock))), candidate.CandidateSha256);
        Assert.Null(candidate.MainCpuClock);
    }

    [Fact]
    public void MainCpuClockDifferentFromTheWriteClock_IsBlocked()
    {
        var candidate = new SnapshotFixtureGenerator(Runner(Trigger, Trigger + 2), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.BlockedExit, candidate.ExitCode);
        Assert.Equal(Trigger + 2, candidate.MainCpuClock);
        Assert.Contains("MAINCPU clock 100002", candidate.Reason);
    }

    [Fact]
    public void NonzeroWriterResult_IsAFailure_NeverAccepted()
    {
        var candidate = new SnapshotFixtureGenerator(Runner(Trigger, Trigger, writerResult: 1), _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.FailedExit, candidate.ExitCode);
        Assert.Equal(1, candidate.WriterResult);
        Assert.Contains("machine_write_snapshot returned 1", candidate.Reason);
        Assert.Equal(["xvic-pal-unexpanded"], candidate.BlockedIds);
    }

    [Fact]
    public void NoCheckpointRecord_IsAFailure_WithNoInventedHash()
    {
        var runner = new FakeRunner(_ => new CommandResult(0, false, []));
        var candidate = new SnapshotFixtureGenerator(runner, _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.FailedExit, candidate.ExitCode);
        Assert.Equal(-1, candidate.WriterResult);
        Assert.Null(candidate.WriteClock);
        Assert.Null(candidate.CandidateSha256);
        Assert.Null(candidate.CandidateLength);
        Assert.Contains("no VICE_SNAP checkpoint record", candidate.Reason);
    }

    [Fact]
    public void TimedOutObserver_IsAFailure()
    {
        var runner = new FakeRunner(_ => new CommandResult(-1, true, []));
        var candidate = new SnapshotFixtureGenerator(runner, _allowlist).Generate(Spec());
        Assert.Equal(FixtureCandidate.FailedExit, candidate.ExitCode);
        Assert.True(candidate.TimedOut);
    }

    [Fact]
    public void RomOutsideTheAllowlist_RefusesTheLaunch()
    {
        var wrong = new Dictionary<string, string>(_allowlist, StringComparer.Ordinal) { [Roms[1]] = new string('0', 64) };
        var runner = Runner(Trigger, Trigger);
        var candidate = new SnapshotFixtureGenerator(runner, wrong).Generate(Spec());
        Assert.Empty(runner.Requests);
        Assert.Equal(FixtureCandidate.FailedExit, candidate.ExitCode);
        Assert.Equal(-3, candidate.WriterResult);
        Assert.Contains("ROM allowlist", candidate.Reason);
        Assert.Contains(Roms[1] + " hash differs from the allowlist", candidate.Reason);
        var missing = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), _allowlist).Generate(Spec() with { RequiredRoms = [.. Roms, "VIC20/absent.bin"] });
        Assert.Contains("VIC20/absent.bin file missing", missing.Reason);
        var unlisted = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), new Dictionary<string, string>(StringComparer.Ordinal)).Generate(Spec());
        Assert.Contains("path not allowlisted", unlisted.Reason);
    }

    [Fact]
    public void BaseAllowlist_IsTheNineEntriesOfThePlan()
    {
        Assert.Equal(9, RomAllowlist.BaseFixtureSubset.Count);
        Assert.Equal("FF6D1416B91A71F5240A5BB8ADC9FDC1B955374C48322CA47B3E436334F168FB", RomAllowlist.BaseFixtureSubset["VIC20/kernal.901486-07.bin"]);
        Assert.Equal("8FC94BD895F488AC0AE1A52ABDC32988CD63C777770B89A9D415513098535CC2", RomAllowlist.BaseFixtureSubset["VIC20/kernal.901486-06.bin"]);
        Assert.Equal("BDA280726CFE069CA70EF5E2F2D95A29F69D3950C859A12E6979464963FCF502", RomAllowlist.BaseFixtureSubset["VIC20/chargen-901460-03.bin"]);
        Assert.Equal("8BE2D882E0EEB1C6923A43660236EA69D43CA8A3BD0613B1B6C910601584FBED", RomAllowlist.BaseFixtureSubset["VIC20/basic-901486-01.bin"]);
        Assert.All(RomAllowlist.BaseFixtureSubset.Values, v => Assert.Matches("^[0-9A-F]{64}$", v));
        var checks = RomAllowlist.Verify(_dataRoot, Roms);
        Assert.All(checks, c => Assert.False(c.Ok));
        Assert.All(checks, c => Assert.Equal("hash differs from the allowlist", c.Reason));
    }

    [Fact]
    public void Classifier_AcceptsOnlyWhenEveryHostAndRunAgrees()
    {
        var generator = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), _allowlist);
        var runs = new[] { generator.Generate(Spec("LAB-OMARCHY", "1")), generator.Generate(Spec("LAB-OMARCHY", "2")), generator.Generate(Spec("PAYTON-LEGION2", "1")), generator.Generate(Spec("PAYTON-LEGION2", "2")) };
        var accepted = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, runs, ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        Assert.True(accepted.IsAccepted, string.Join("; ", accepted.Reasons));
        Assert.Equal(runs[0].CandidateSha256, accepted.AcceptedSha256);
        Assert.Equal(4, accepted.Clocks.Count);

        var oneHostLater = new SnapshotFixtureGenerator(Runner(Trigger + 7, Trigger + 7), _allowlist);
        var oneHost = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, [runs[0], runs[1], oneHostLater.Generate(Spec("PAYTON-LEGION2", "1")), oneHostLater.Generate(Spec("PAYTON-LEGION2", "2"))], ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        Assert.Equal(FixtureClassification.Blocked, oneHost.Outcome);
        Assert.Contains(oneHost.Reasons, r => r.Contains("PAYTON-LEGION2") && r.Contains(SnapshotFixtureClassifier.BlockerId));
        Assert.Contains(oneHost.Clocks, c => c.Host == "PAYTON-LEGION2" && c.WriteClock == Trigger + 7);
        Assert.Contains(oneHost.Clocks, c => c.Host == "LAB-OMARCHY" && c.WriteClock == Trigger);
        Assert.Null(oneHost.AcceptedSha256);

        var bothHosts = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, [oneHostLater.Generate(Spec("LAB-OMARCHY", "1")), oneHostLater.Generate(Spec("LAB-OMARCHY", "2")), oneHostLater.Generate(Spec("PAYTON-LEGION2", "1")), oneHostLater.Generate(Spec("PAYTON-LEGION2", "2"))], ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        Assert.Equal(FixtureClassification.Blocked, bothHosts.Outcome);
        Assert.Equal(2, bothHosts.Reasons.Count(r => r.Contains("LAB-OMARCHY")));
        Assert.Equal(2, bothHosts.Reasons.Count(r => r.Contains("PAYTON-LEGION2")));

        var missingHost = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, [runs[0], runs[1]], ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        Assert.Equal(FixtureClassification.Blocked, missingHost.Outcome);
        Assert.Contains(missingHost.Reasons, r => r.Contains("host PAYTON-LEGION2: 0 candidate(s)"));

        var oneRun = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, [runs[0], runs[2]], ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        Assert.Equal(FixtureClassification.Blocked, oneRun.Outcome);
    }

    [Fact]
    public void Classifier_BlocksDifferingBytesAcrossHosts()
    {
        var generator = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), _allowlist);
        var windowsRunner = new FakeRunner(request =>
        {
            var bytes = SyntheticSnapshot(Trigger);
            bytes[^1] ^= 0xFF;
            File.WriteAllBytes(CandidatePathOf(request), bytes);
            return new CommandResult(0, false, [$"VICE_SNAP,checkpoint,{Trigger},{Trigger}"]);
        });
        var windows = new SnapshotFixtureGenerator(windowsRunner, _allowlist);
        var runs = new[] { generator.Generate(Spec("LAB-OMARCHY", "1")), generator.Generate(Spec("LAB-OMARCHY", "2")), windows.Generate(Spec("PAYTON-LEGION2", "1")), windows.Generate(Spec("PAYTON-LEGION2", "2")) };
        Assert.All(runs, r => Assert.Equal(FixtureCandidate.AcceptedExit, r.ExitCode));
        var result = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, runs, ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        Assert.Equal(FixtureClassification.Blocked, result.Outcome);
        Assert.Contains(result.Reasons, r => r.StartsWith("candidate bytes differ", StringComparison.Ordinal));
    }

    [Fact]
    public void Publisher_WritesAcceptedBytesAndManifestEntry_RefusesBlocked_AndIsIdempotent()
    {
        var generator = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), _allowlist);
        var runs = new[] { generator.Generate(Spec("LAB-OMARCHY", "1")), generator.Generate(Spec("LAB-OMARCHY", "2")) };
        var classification = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, runs, ["LAB-OMARCHY"]);
        var fixturePath = Path.Combine(_root, "published", "ready-xvic-pal-unexpanded.vsf");
        var manifestPath = Path.Combine(_root, "published", "manifest.json");
        var entry = new FixtureManifestEntry
        {
            Path = "tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-xvic-pal-unexpanded.vsf", Sha256 = runs[0].CandidateSha256!, Length = runs[0].CandidateLength!.Value,
            Machine = "xvic", Model = "vic20", Command = runs[0].Command, TriggerClock = Trigger, WriteClock = Trigger,
            RomHashes = [.. runs[0].Roms.Select(r => r.Sha256!)], Hosts = ["LAB-OMARCHY"], ObserverSha256 = runs[0].ObserverSha256,
        };
        var first = FixturePublisher.Publish(classification, runs[0].CandidatePath!, fixturePath, manifestPath, entry);
        Assert.True(first.FixtureChanged);
        Assert.True(first.ManifestChanged);
        Assert.Equal(SyntheticSnapshot(Trigger), File.ReadAllBytes(fixturePath));
        var manifest = FixtureManifest.LoadOrCreate(manifestPath);
        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal(FixtureManifest.PinnedViceCommit, manifest.ViceCommit);
        Assert.Equal(9, manifest.RomAllowlist.Length);
        var saved = Assert.Single(manifest.Fixtures);
        Assert.Equal(entry.Sha256, saved.Sha256);
        Assert.Equal(Trigger, saved.WriteClock);

        var second = FixturePublisher.Publish(classification, runs[1].CandidatePath!, fixturePath, manifestPath, entry);
        Assert.False(second.FixtureChanged);
        Assert.False(second.ManifestChanged);

        var blocked = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, runs, ["LAB-OMARCHY", "PAYTON-LEGION2"]);
        var error = Assert.Throws<InvalidOperationException>(() => FixturePublisher.Publish(blocked, runs[0].CandidatePath!, fixturePath, manifestPath, entry));
        Assert.Contains("blocked", error.Message);

        var wrongEntry = new FixtureManifestEntry { Path = entry.Path, Sha256 = new string('A', 64), Length = entry.Length };
        Assert.Throws<InvalidOperationException>(() => FixturePublisher.Publish(classification, runs[0].CandidatePath!, fixturePath, manifestPath, wrongEntry));
    }

    /// <summary>
    /// Use case: add a native VIC-20 candidate without losing the native reader contract or supplied C64 fixture.
    /// Acceptance: R17 and D5b of PLAN-NATIVERESTORE-001 retain framing, read-order sources, modules and all
    /// supplied-fixture metadata supporting TR-VSFLOCKSTEP-RESUME-001 AC1 and TEST-VSFLOCKSTEP-001 AC1.
    /// 2026-11-23: prove the preservation check with independent good and lossy mock outputs before binding it.
    /// </summary>
    [Fact]
    public void Publisher_ManifestPreservationMock_AcceptsAppendAndRejectsMetadataLoss()
    {
        var original = ManifestWithSuppliedFixture();
        var mock = (JsonObject)original.DeepClone();
        mock["fixtures"]!.AsArray().Add(new JsonObject { ["path"] = "fixtures/vic20.vsf" });
        Assert.True(PreservesExistingManifest(original, mock));

        foreach (var member in new[] { "framing", "readOrderSources", "modules" })
        {
            var incomplete = (JsonObject)mock.DeepClone();
            incomplete.Remove(member);
            Assert.False(PreservesExistingManifest(original, incomplete), $"Dropping {member} must fail.");
        }
        var incompleteFixture = (JsonObject)mock.DeepClone();
        incompleteFixture["fixtures"]![0]!.AsObject().Remove("headerVariant");
        Assert.False(PreservesExistingManifest(original, incompleteFixture));
    }

    /// <summary>
    /// Use case: append an accepted VIC-20 fixture to the existing state-transfer manifest.
    /// Acceptance: the publisher preserves reader metadata and the supplied fixture under PLAN-NATIVERESTORE-001
    /// D5b/R17, supporting TR-VSFLOCKSTEP-RESUME-001 AC1 and TEST-VSFLOCKSTEP-001 AC1.
    /// 2026-11-23: bind the preservation assertion to the real manifest writer and verify repeat publication.
    /// </summary>
    [Fact]
    public void Publisher_PreservesUnknownManifestMembers()
    {
        const string fixtureId = "xvic-pal-unexpanded";
        const string path = "fixtures/vic20.vsf";
        string fixturePath = Path.Combine(_root, "vic20.vsf");
        string manifestPath = Path.Combine(_root, "manifest.json");
        var original = ManifestWithSuppliedFixture();
        File.WriteAllText(manifestPath, original.ToJsonString());
        var generator = new SnapshotFixtureGenerator(Runner(Trigger, Trigger), _allowlist);
        string[] hosts = ["LAB-OMARCHY", "PAYTON-LEGION2"];
        var runs = hosts.SelectMany(h => new[] { generator.Generate(Spec(h, "1")), generator.Generate(Spec(h, "2")) }).ToArray();
        var classification = SnapshotFixtureClassifier.Classify(fixtureId, Trigger, runs, hosts);
        var entry = new FixtureManifestEntry
        {
            Path = path, Sha256 = runs[0].CandidateSha256!, Length = runs[0].CandidateLength!.Value,
            TriggerClock = Trigger, WriteClock = Trigger, Hosts = hosts,
        };

        Assert.True(FixturePublisher.Publish(classification, runs[0].CandidatePath!, fixturePath, manifestPath, entry).ManifestChanged);
        string written = File.ReadAllText(manifestPath);
        var updated = JsonNode.Parse(written)!.AsObject();
        Assert.True(PreservesExistingManifest(original, updated), "Publication changed existing manifest metadata.");
        var fixtures = updated["fixtures"]!.AsArray();
        Assert.Equal(2, fixtures.Count);
        Assert.Equal(path, fixtures[1]!["path"]!.GetValue<string>());
        Assert.False(FixturePublisher.Publish(classification, runs[1].CandidatePath!, fixturePath, manifestPath, entry).ManifestChanged);
        Assert.Equal(written, File.ReadAllText(manifestPath));
    }

    private static JsonObject ManifestWithSuppliedFixture() => new()
    {
        ["schemaVersion"] = 1,
        ["framing"] = new JsonObject { ["currentHeaderBytes"] = 58 },
        ["readOrderSources"] = new JsonObject { ["xvic"] = "vic20-snapshot.c" },
        ["modules"] = new JsonArray(new JsonObject { ["name"] = "MAINCPU" }),
        ["fixtures"] = new JsonArray(new JsonObject
        {
            ["path"] = "fixtures/supplied-c64.vsf",
            ["headerVariant"] = "current",
            ["modules"] = new JsonArray(JsonValue.Create("MAINCPU")),
            ["romHashesSource"] = "supplied",
        }),
    };

    private static bool PreservesExistingManifest(JsonObject original, JsonObject updated)
        => original.Where(p => p.Key != "fixtures").All(p => JsonNode.DeepEquals(p.Value, updated[p.Key]))
            && original["fixtures"]!.AsArray().All(expected =>
                updated["fixtures"]!.AsArray().Any(actual => JsonNode.DeepEquals(expected, actual)));

    [Fact]
    public void CandidateReceipts_RoundTripThroughJson()
    {
        var candidate = new SnapshotFixtureGenerator(Runner(Trigger + 7, Trigger + 7), _allowlist).Generate(Spec());
        var path = Path.Combine(_root, "receipts", "c.json");
        FixturePublisher.SaveCandidate(candidate, path);
        var loaded = FixturePublisher.LoadCandidate(path);
        Assert.Equal(candidate.ExitCode, loaded.ExitCode);
        Assert.Equal(candidate.WriteClock, loaded.WriteClock);
        Assert.Equal(candidate.CandidateSha256, loaded.CandidateSha256);
        Assert.Equal(candidate.Reason, loaded.Reason);
        Assert.Equal(candidate.BlockedIds, loaded.BlockedIds);
        Assert.Equal(candidate.Roms.Count, loaded.Roms.Count);
        var classification = SnapshotFixtureClassifier.Classify("xvic-pal-unexpanded", Trigger, [loaded, loaded], ["LAB-OMARCHY"]);
        var cpath = Path.Combine(_root, "receipts", "class.json");
        FixturePublisher.SaveClassification(classification, cpath);
        Assert.Contains(SnapshotFixtureClassifier.BlockerId, File.ReadAllText(cpath));
    }
}
