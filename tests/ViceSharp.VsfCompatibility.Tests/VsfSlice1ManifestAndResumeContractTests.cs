using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 1 as amended 2026-10-20 (PLAN-NATIVERESTORE-001 D1): the mock-bound form of
/// ViceSnapshotStateTransferManifestTests (schema-1 per-line ledger, round 4 N12), NativeViceSnapshotFixtureGeneratorTests
/// (regeneration determinism and the R12 literal checkpoint with the N21 retention contract) and the resume boundary
/// AC SnapshotResume_ObserverClockAndPcMatchVsfMainCpu (N02, N07, N10, N13, N18, N23). The real-bound form
/// (Category VsfSlice1Real) binds the same assertions in D5 to the manifest file, the generator and the observer
/// streams. TR-VSFLOCKSTEP-RESUME-001 AC1; TEST-VSFLOCKSTEP-001.
/// </summary>
[Trait("Category", "VsfSlice1Contract")]
public sealed class VsfSlice1ManifestAndResumeContractTests
{
    // Pinned c64-snapshot.c 121 to 137 read order; the manifest's restore order must match it.
    public static readonly string[] PinnedX64scOrder = ["MAINCPU", "C64", "CIA1", "CIA2", "SID", "DRIVE8", "DRIVE9", "DRIVE10", "DRIVE11", "VIC-II", "C64GLUE", "EVENT", "MEMHACKS", "TAPEPORT", "KEYBOARD", "JOYPORT1", "JOYPORT2", "USERPORT"];

    public sealed record ModuleEntry(string Machine, string Name, string Version, int RestoreOrder, string[] Categories, string NativeExport, string NativeImport, string ManagedRecord, string ManagedAdapter, bool Mandatory, string SourceAnchor);
    public sealed record FixtureEntry(string Path, string Sha256, long Length, string Machine, string Model, string Command, ulong TriggerClock, ulong WriteClock, string[] RomHashes, string[] MediaHashes);
    public sealed record ContainerFraming(int MagicBytes, int ContainerVersionBytes, int MachineFieldBytes, int CurrentHeaderBytes, int LegacyHeaderBytes);
    public sealed record Manifest(int SchemaVersion, string ViceCommit, string FixtureGeneratorVersion, ContainerFraming Framing, string[] RomAllowlist, FixtureEntry[] Fixtures, ModuleEntry[] Modules);

    public static Manifest ValidManifest() => new(1, VsfSlice1PrerequisiteContractTests.PinnedSha, "1.0.0", new(19, 2, 16, 58, 37),
        [new string('1', 64), new string('2', 64)],
        [new("fixtures/xvic-pal-100000.vsf", new string('C', 64), 1000, "xvic", "PAL", "cmd", 100000, 100000, [new string('2', 64)], [])],
        PinnedX64scOrder.Select((n, i) => new ModuleEntry("x64sc", n, "1.0", i, ["deterministic"], n.ToLowerInvariant() + "_snapshot_write_module", n.ToLowerInvariant() + "_snapshot_read_module", "Managed" + n, "Adapter" + n, true, "c64-snapshot.c:121")).ToArray());

    internal static void AssertManifest(Manifest m, string[] moduleNamesInFixtures)
    {
        Assert.NotNull(m);
        Assert.Equal(1, m.SchemaVersion);
        Assert.Equal(VsfSlice1PrerequisiteContractTests.PinnedSha, m.ViceCommit);
        Assert.False(string.IsNullOrWhiteSpace(m.FixtureGeneratorVersion));
        Assert.Equal(new ContainerFraming(19, 2, 16, 58, 37), m.Framing);
        Assert.NotEmpty(m.RomAllowlist);
        Assert.All(m.RomAllowlist, h => Assert.Matches("^[0-9A-F]{64}$", h));
        Assert.Equal(m.RomAllowlist.Length, m.RomAllowlist.Distinct().Count());
        Assert.NotEmpty(m.Fixtures);
        Assert.Equal(m.Fixtures.Length, m.Fixtures.Select(f => f.Path).Distinct().Count());
        foreach (var f in m.Fixtures)
        {
            Assert.Matches("^[0-9A-F]{64}$", f.Sha256);
            Assert.True(f.Length > 0);
            Assert.Equal(f.TriggerClock, f.WriteClock);
            Assert.False(string.IsNullOrWhiteSpace(f.Command));
            Assert.NotEmpty(f.RomHashes);
        }
        Assert.NotEmpty(m.Modules);
        Assert.Equal(m.Modules.Length, m.Modules.Select(e => e.Machine + "/" + e.Name).Distinct().Count());
        foreach (var e in m.Modules)
        {
            foreach (var field in new[] { e.Machine, e.Name, e.Version, e.NativeExport, e.NativeImport, e.ManagedRecord, e.ManagedAdapter, e.SourceAnchor })
                Assert.False(string.IsNullOrWhiteSpace(field), "every schema-1 module field is required");
            Assert.NotEmpty(e.Categories);
        }
        var x64sc = m.Modules.Where(e => e.Machine == "x64sc").OrderBy(e => e.RestoreOrder).Select(e => e.Name).ToArray();
        Assert.Equal(PinnedX64scOrder, x64sc);
        foreach (var name in moduleNamesInFixtures) Assert.Contains(m.Modules, e => e.Name == name);
    }

    [Fact]
    public void Manifest_CoversEveryDeterministicModule_and_every_schema_field_is_checked()
    {
        var names = PinnedX64scOrder;
        AssertManifest(ValidManifest(), names);
        void Reject(Func<Manifest, Manifest> mutate) => Assert.ThrowsAny<XunitException>(() => AssertManifest(mutate(ValidManifest()), names));
        Reject(m => m with { SchemaVersion = 2 });
        Reject(m => m with { ViceCommit = "abc" });
        Reject(m => m with { FixtureGeneratorVersion = "" });
        Reject(m => m with { Framing = m.Framing with { CurrentHeaderBytes = 57 } });
        Reject(m => m with { RomAllowlist = [] });
        Reject(m => m with { RomAllowlist = [m.RomAllowlist[0], m.RomAllowlist[0]] });               // duplicate
        Reject(m => m with { Fixtures = [] });
        Reject(m => m with { Fixtures = [m.Fixtures[0], m.Fixtures[0]] });                           // duplicate fixture
        Reject(m => m with { Fixtures = [m.Fixtures[0] with { WriteClock = 100003 }] });              // not the literal checkpoint
        Reject(m => m with { Fixtures = [m.Fixtures[0] with { RomHashes = [] }] });
        Reject(m => m with { Modules = m.Modules.Skip(1).ToArray() });                                // missing module (MAINCPU)
        Reject(m => m with { Modules = [.. m.Modules, m.Modules[0]] });                               // duplicate module
        Reject(m => m with { Modules = m.Modules.Select(e => e.Name == "SID" ? e with { NativeImport = "" } : e).ToArray() }); // blank edge
        Reject(m => m with { Modules = m.Modules.Select(e => e.Name == "SID" ? e with { RestoreOrder = 0 } : e).ToArray() }); // restore order drift
        Reject(m => m with { Modules = m.Modules.Select(e => e.Name == "CIA1" ? e with { Categories = [] } : e).ToArray() });
        Reject(m => m with { Modules = m.Modules.Select(e => e.Name == "CIA1" ? e with { SourceAnchor = " " } : e).ToArray() });
    }

    // Regeneration and the N21 retention contract: candidate outcomes the generator must classify.
    public sealed record GeneratorOutcome(string Host, ulong Trigger, ulong? WriteClock, int WriterResult, byte[]? CandidateBytes, string? CandidateSha256, bool AcceptedFixtureChanged, bool ManifestChanged, int ExitCode, string[] BlockedIds);

    public interface IFixtureGenerator
    {
        GeneratorOutcome[] Generate(string machine, string model, ulong trigger);
    }

    internal static void AssertRegeneration(GeneratorOutcome[] runs)
    {
        Assert.NotNull(runs);
        Assert.True(runs.Length >= 2, "two fresh-command runs per host are required");
        var accepted = runs.All(r => r.WriterResult == 0 && r.WriteClock == r.Trigger && r.CandidateBytes is { Length: > 0 });
        if (accepted)
        {
            Assert.Single(runs.Select(r => r.CandidateSha256).Distinct());
            Assert.All(runs, r => Assert.Equal(0, r.ExitCode));
            Assert.All(runs, r => Assert.Empty(r.BlockedIds));
            return;
        }
        // Any mismatch, failure or absent bytes: nothing published, candidates retained or honestly absent, nonzero exit, BLOCKED ids.
        Assert.All(runs, r => Assert.False(r.AcceptedFixtureChanged, "a mismatch must leave the accepted fixture unchanged"));
        Assert.All(runs, r => Assert.False(r.ManifestChanged, "a mismatch must leave the manifest unchanged"));
        Assert.Contains(runs, r => r.ExitCode != 0);
        Assert.Contains(runs, r => r.BlockedIds.Length > 0);
        foreach (var r in runs)
        {
            if (r.CandidateBytes is { Length: > 0 }) Assert.Matches("^[0-9A-F]{64}$", r.CandidateSha256!);
            else Assert.Null(r.CandidateSha256);   // never an invented hash (N25)
        }
    }

    [Fact]
    public void Regeneration_IsByteDeterministic_and_mismatch_retains_candidates_without_publishing()
    {
        static GeneratorOutcome Ok(string host, string sha) => new(host, 100000, 100000, 0, [1, 2, 3], sha, true, true, 0, []);
        var ok = Substitute.For<IFixtureGenerator>();
        ok.Generate("xvic", "PAL", 100000).Returns([Ok("linux", new string('C', 64)), Ok("windows", new string('C', 64))]);
        AssertRegeneration(ok.Generate("xvic", "PAL", 100000));

        void Reject(GeneratorOutcome[] runs)
        {
            var g = Substitute.For<IFixtureGenerator>();
            g.Generate("xvic", "PAL", 100000).Returns(runs);
            Assert.ThrowsAny<XunitException>(() => AssertRegeneration(g.Generate("xvic", "PAL", 100000)));
        }
        Reject([Ok("linux", new string('C', 64)), Ok("windows", new string('D', 64))]);                                     // hosts differ
        Reject([Ok("linux", new string('C', 64))]);                                                                           // one run only
        Reject([new("linux", 100000, 100007, 0, [1], new string('E', 64), true, false, 0, []), Ok("windows", new string('C', 64))]); // later boundary accepted as success
        Reject([new("linux", 100000, 100000, 1, [1], new string('E', 64), false, false, 1, ["xvic-pal"]), new("windows", 100000, 100000, 1, [1], new string('E', 64), true, false, 1, ["xvic-pal"])]); // +1 writer result published on one host
        Reject([new("linux", 100000, null, -1, null, new string('F', 64), false, false, 1, ["xvic-pal"]), new("windows", 100000, 100000, 0, [1], new string('C', 64), false, false, 1, ["xvic-pal"])]); // invented hash for absent bytes

        // A correct blocked classification passes: one-host mismatch, retained candidate, nothing published, nonzero, ids.
        var blocked = Substitute.For<IFixtureGenerator>();
        blocked.Generate("xvic", "PAL", 100000).Returns([new("linux", 100000, 100007, 0, [1], new string('E', 64), false, false, 1, ["xvic-pal"]), new("windows", 100000, 100000, 0, [1], new string('C', 64), false, false, 1, ["xvic-pal"])]);
        AssertRegeneration(blocked.Generate("xvic", "PAL", 100000));
    }

    // Resume boundary (N02, N07, N10, N13, N18, N23). Lines are the observed records in order.
    public sealed record ResumeInput(ulong Clock, int A, int X, int Y, int Sp, int Pc, int P, string[] Precondition, ulong WindowCycles, ulong PreludeCycles);
    public sealed record ResumeTrace(int ExitCode, string[] Lines, ulong LimitCycles);

    public interface IResumeProbe
    {
        ResumeTrace Run(ResumeInput input);
    }

    public static readonly ResumeInput Approved = new(109163946, 0x11, 0x22, 0x33, 0xF8, 22377, 0xA0, ["-drive8type", "0"], 4, 2956000);

    /// <summary>The constrained positive trace: resume and load records, then the post-load gate at clk+1 with one stolen cycle, the first CPU access reading at PC, then the rest of the window.</summary>
    public static string[] PositiveLines(ResumeInput i)
    {
        var c = i.Clock;
        return
        [
            $"VICE_SNAP,resume,{c},{i.A},{i.X},{i.Y},{i.Sp},{i.Pc},{i.P}",
            "VICE_SNAP,load,0",
            "VICE_SNAP,drives,0,0,none,1541,1,none,1541,1,none,1541,1,none",
            $"VICE_OBS,1,0,PHI1,0,0,idle,0,17,0,1,{c + 1}", $"VICE_OBS,1,1,PHI2,1,0,idle,0,,0,1,{c + 1}", $"VICE_GATE,{c + 1}",
            $"VICE_OBS,1,2,PHI1,0,1,idle,0,17,0,0,{c + 2}", $"VICE_OBS,1,3,PHI2,1,1,read,{i.Pc},169,0,0,{c + 2}", $"VICE_GATE,{c + 2}",
            $"VICE_OBS,1,4,PHI1,0,1,idle,0,17,0,0,{c + 3}", $"VICE_OBS,1,5,PHI2,1,1,read,{i.Pc + 1},32,0,0,{c + 3}", $"VICE_GATE,{c + 3}",
            $"VICE_OBS,1,6,PHI1,0,1,idle,0,17,0,0,{c + 4}", $"VICE_OBS,1,7,PHI2,1,1,write,1024,1,0,0,{c + 4}", $"VICE_GATE,{c + 4}",
            "VICE_EXIT,limit"
        ];
    }

    internal static void AssertResume(ResumeTrace t, ResumeInput i)
    {
        Assert.NotNull(t);
        Assert.Equal(0, t.ExitCode);
        Assert.Equal(i.WindowCycles > 0 && i.Clock + i.WindowCycles > i.PreludeCycles ? i.Clock + i.WindowCycles : i.PreludeCycles + i.WindowCycles, t.LimitCycles);
        var lines = t.Lines;
        Assert.True(lines.Length > 0, "no records: prelude-only exit or absent boundary");
        var resumeIndex = Array.FindIndex(lines, l => l.StartsWith("VICE_SNAP,resume,", StringComparison.Ordinal));
        Assert.True(resumeIndex >= 0, "the resume record is required");
        Assert.Equal($"VICE_SNAP,resume,{i.Clock},{i.A},{i.X},{i.Y},{i.Sp},{i.Pc},{i.P}", lines[resumeIndex]);
        Assert.DoesNotContain(lines, l => l.StartsWith("VICE_SNAP,resume-fail", StringComparison.Ordinal));
        var loadIndex = Array.FindIndex(lines, l => l.StartsWith("VICE_SNAP,load,", StringComparison.Ordinal));
        Assert.True(loadIndex == resumeIndex + 1, "load must follow resume before the next gate line");
        Assert.Equal("VICE_SNAP,load,0", lines[loadIndex]);
        Assert.DoesNotContain(lines.Skip(loadIndex), l => l.StartsWith("VICE_RESET", StringComparison.Ordinal));
        var gates = lines.Where(l => l.StartsWith("VICE_GATE,", StringComparison.Ordinal)).Select(l => ulong.Parse(l.Split(',')[1], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.True(gates.Length >= (int)i.WindowCycles, "the whole window must be observed");
        Assert.Equal(i.Clock + 1, gates[0]);
        for (var k = 1; k < gates.Length; k++) Assert.Equal(gates[k - 1] + 1, gates[k]);
        var obs = lines.Where(l => l.StartsWith("VICE_OBS,1,", StringComparison.Ordinal)).Select(l => l.Split(',')).ToArray();
        Assert.Equal(2 * gates.Length, obs.Length);
        for (var k = 0; k < obs.Length; k++) Assert.Equal(k.ToString(System.Globalization.CultureInfo.InvariantCulture), obs[k][2]);
        var firstAccess = obs.First(o => o[6] is "read" or "write" or "dummy");
        foreach (var o in obs.TakeWhile(o => o != firstAccess)) Assert.True(o[6] == "idle" && (o[10] == "1" || o[3] == "PHI1"), "only idle or stolen half-cycles may precede the first CPU access");
        Assert.True(firstAccess[6] is "read" or "dummy", "the first CPU access is a read or dummy at PC");
        Assert.Equal(i.Pc.ToString(System.Globalization.CultureInfo.InvariantCulture), firstAccess[7]);
        Assert.Equal("VICE_EXIT,limit", lines[^1]);
        Assert.DoesNotContain(lines, l => l.StartsWith("VICE_GATE_STOP", StringComparison.Ordinal));
    }

    [Fact]
    public void Resume_boundary_contract_accepts_the_constrained_positive_case_and_rejects_each_named_case()
    {
        var input = Approved;
        var correct = Substitute.For<IResumeProbe>();
        correct.Run(input).Returns(new ResumeTrace(0, PositiveLines(input), input.Clock + input.WindowCycles));
        AssertResume(correct.Run(input), input);

        void Reject(Func<string[], string[]> mutate, ulong? limit = null)
        {
            var wrong = Substitute.For<IResumeProbe>();
            wrong.Run(input).Returns(new ResumeTrace(0, mutate(PositiveLines(input)), limit ?? input.Clock + input.WindowCycles));
            Assert.ThrowsAny<XunitException>(() => AssertResume(wrong.Run(input), input));
        }
        Reject(l => l.Where(x => !x.StartsWith("VICE_SNAP,resume,", StringComparison.Ordinal)).ToArray());                   // absent boundary
        Reject(l => l.Select(x => x.StartsWith("VICE_SNAP,resume,", StringComparison.Ordinal) ? x.Replace(",160", ",161") : x).ToArray()); // register mismatch
        Reject(l => l.Select(x => x.StartsWith("VICE_SNAP,resume,", StringComparison.Ordinal) ? "VICE_SNAP,resume-fail,-1" : x).ToArray()); // resume-fail
        Reject(l => l.Select(x => x == "VICE_SNAP,load,0" ? "VICE_SNAP,load,-1" : x).Concat(["VICE_RESET"]).ToArray());         // later module fails after MAINCPU
        Reject(l => [.. l[..3], "VICE_RESET", .. l[3..]]);                                                                   // unrelated reset after load
        Reject(l => l.Select(x => x.Contains(",read,22377,", StringComparison.Ordinal) ? x.Replace(",read,22377,", ",read,22378,") : x).ToArray()); // first access not at PC
        Reject(l => l.Select(x => x == $"VICE_GATE,{input.Clock + 1}" ? $"VICE_GATE,{input.Clock + 2}" : x).ToArray());       // post-load clock not clk+1
        Reject(l => ["VICE_EXIT,limit"]);                                                                                     // prelude-only exit
        Reject(l => l[..9].Concat(["VICE_EXIT,limit"]).ToArray());                                                            // early termination (window not complete)
        Reject(l => l.Where(x => !x.StartsWith("VICE_OBS,1,5,", StringComparison.Ordinal)).ToArray());                       // missing half-cycle
        Reject(l => [.. l[..5], l[4], .. l[5..]]);                                                                            // extra half-cycle
        Reject(l => { var c = l.ToArray(); (c[3], c[4]) = (c[4], c[3]); return c; });                                          // reordered half-cycles
        Reject(l => l, input.Clock);                                                                                           // budget below the window (N07)
        Reject(l => l.Select(x => x.StartsWith("VICE_SNAP,drives,", StringComparison.Ordinal) ? x : x).Concat(["VICE_GATE_STOP,65"]).ToArray()); // gate stop

        // Low-clock positive (N23): the limit is raised to the prelude plus window and the whole window still follows the load.
        var low = Approved with { Clock = 5000, WindowCycles = 4 };
        var lowProbe = Substitute.For<IResumeProbe>();
        lowProbe.Run(low).Returns(new ResumeTrace(0, PositiveLines(low), low.PreludeCycles + low.WindowCycles));
        AssertResume(lowProbe.Run(low), low);
        var lowWrong = Substitute.For<IResumeProbe>();
        lowWrong.Run(low).Returns(new ResumeTrace(0, PositiveLines(low), low.Clock + low.WindowCycles));
        Assert.ThrowsAny<XunitException>(() => AssertResume(lowWrong.Run(low), low));
    }

    /// <summary>N18: the window is specified in whole CPU cycles; two VICE_OBS records per cycle; an odd half-cycle request is rejected.</summary>
    public static long HalfCycles(ulong windowCycles) => windowCycles == 0 ? throw new ArgumentOutOfRangeException(nameof(windowCycles)) : checked((long)(windowCycles * 2));
    public static ulong WindowFromHalfCycles(long halfCycles) => halfCycles <= 0 || halfCycles % 2 == 1 ? throw new ArgumentException("windows are whole cycles") : (ulong)(halfCycles / 2);

    [Theory]
    [InlineData(4UL, 8L)]
    [InlineData(100000UL, 200000L)]
    public void Window_in_cpu_cycles_is_twice_as_many_half_cycle_records(ulong windowCycles, long expectedHalfCycles)
    {
        Assert.Equal(expectedHalfCycles, HalfCycles(windowCycles));
        Assert.Equal(windowCycles, WindowFromHalfCycles(expectedHalfCycles));
    }

    [Fact]
    public void Empty_window_and_odd_half_cycle_requests_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HalfCycles(0));
        Assert.Throws<ArgumentException>(() => WindowFromHalfCycles(7));
        Assert.Throws<ArgumentException>(() => WindowFromHalfCycles(0));
    }
}
