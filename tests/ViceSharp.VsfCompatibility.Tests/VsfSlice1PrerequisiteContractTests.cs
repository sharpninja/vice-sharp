using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 1 as amended 2026-10-20 (PLAN-NATIVERESTORE-001 D1, R8, R12, R15, N08): the
/// mock-bound form of NativeViceSnapshotPrerequisiteTests and ImmutableViceBuildTests. A fake prerequisite provider
/// supplies a valid baseline, then each control omits or corrupts one input (library, MSYS2 tool, fixture, commit,
/// ROM hash, media hash, output hash, clean source materialization, legacy normalization capability) and the
/// assertions must reject it. The real-bound form (Category VsfSlice1Real) binds the same assertions in D5 to the
/// builder receipts, the generated fixtures and the materialization inventory. TR-VSFLOCKSTEP-RESUME-001 AC1, AC2;
/// TEST-VSFLOCKSTEP-001; TEST-VICE-001 AC1, AC2.
/// </summary>
[Trait("Category", "VsfSlice1Contract")]
public sealed class VsfSlice1PrerequisiteContractTests
{
    public const string PinnedSha = "cc67418cba58e71d8e70fd353e9de47199ad2075";
    public static readonly string[] RequiredWraps = ["vicii_cycle", "vic_cycle", "mem_initialize_memory", "mem_mmu_translate", "mem_toggle_watchpoints", "vice_sharp_observe_clock", "maincpu_snapshot_read_module", "machine_read_snapshot"];
    public static readonly string[] RequiredTools = ["gcc", "make", "autoconf", "automake", "libtool", "bash"];

    public sealed record Binary(string Machine, string Path, string Sha256, string[] WrapSymbols);
    public sealed record LibraryReceipt(string PinnedSha, Binary[] Binaries, bool MaterializationUnchanged);
    public sealed record FixtureRecord(string Path, string Machine, string Model, string Sha256, long Length, ulong TriggerClock, ulong WriteClock, string[] RomHashes, string[] MediaHashes, string Command, bool Present);
    public sealed record ToolRecord(string Name, string Path, bool Present);
    public sealed record MaterializationReceipt(int Before, int After, string[] Changed, string[] Removed, string[] Added, string[] GeneratedPaths);

    public interface IPrerequisiteProvider
    {
        LibraryReceipt GetLibraries();
        FixtureRecord[] GetFixtures();
        ToolRecord[] GetTools(bool isWindows);
        MaterializationReceipt GetMaterialization();
        bool HasLegacyNormalization();
    }

    public static LibraryReceipt ValidLibraries() => new(PinnedSha,
        [new("x64sc", "bin-linux/x64sc-observer", new string('A', 64), RequiredWraps.Where(w => w != "vic_cycle").ToArray()),
         new("xvic", "bin-linux/xvic-observer", new string('B', 64), RequiredWraps.Where(w => w != "vicii_cycle").ToArray())], true);

    public static FixtureRecord[] ValidFixtures() =>
    [
        new("tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf", "x64sc", "PAL", "5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19", 190854, 0, 0, [new string('1', 64)], [], "supplied", true),
        new("fixtures/xvic-pal-100000.vsf", "xvic", "PAL", new string('C', 64), 1000, 100000, 100000, [new string('2', 64)], [], "xvic-observer -seed 1 VICESHARP_SNAPSHOT_AT=100000:...", true),
        new("fixtures/xvic-ntsc-100000.vsf", "xvic", "NTSC", new string('D', 64), 1000, 100000, 100000, [new string('3', 64)], [], "xvic-observer -seed 1 VICESHARP_SNAPSHOT_AT=100000:...", true)
    ];

    public static ToolRecord[] ValidTools() => RequiredTools.Select(t => new ToolRecord(t, "C:/msys64/mingw64/bin/" + t, true)).ToArray();

    public static MaterializationReceipt ValidMaterialization() => new(4120, 4120, [], [], [], ["artifacts/immutable-vice/" + PinnedSha + "/build-linux/src/x64sc"]);

    internal static void AssertLibraries(LibraryReceipt r)
    {
        Assert.NotNull(r);
        Assert.Equal(PinnedSha, r.PinnedSha);
        Assert.True(r.MaterializationUnchanged, "the materialization must hash unchanged after the build");
        Assert.Equal(["x64sc", "xvic"], r.Binaries.Select(b => b.Machine).ToArray());
        foreach (var b in r.Binaries)
        {
            Assert.Matches("^[0-9A-F]{64}$", b.Sha256);
            var required = RequiredWraps.Where(w => b.Machine == "x64sc" ? w != "vic_cycle" : w != "vicii_cycle");
            foreach (var w in required) Assert.Contains(w, b.WrapSymbols);
        }
    }

    internal static void AssertFixtures(FixtureRecord[] fixtures)
    {
        Assert.NotNull(fixtures);
        var supplied = Assert.Single(fixtures, f => f.Path.EndsWith("ready-c64sc-truedrive.vsf", StringComparison.Ordinal));
        Assert.True(supplied.Present);
        Assert.Equal("5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19", supplied.Sha256);
        Assert.Equal(190854, supplied.Length);
        foreach (var model in new[] { "PAL", "NTSC" })
        {
            var f = Assert.Single(fixtures, x => x.Machine == "xvic" && x.Model == model);
            Assert.True(f.Present, "an xvic fixture that was not written is BLOCKED, never passed");
            Assert.Equal(100000UL, f.TriggerClock);
            Assert.Equal(100000UL, f.WriteClock);
            Assert.Matches("^[0-9A-F]{64}$", f.Sha256);
            Assert.NotEmpty(f.RomHashes);
            Assert.All(f.RomHashes, h => Assert.Matches("^[0-9A-F]{64}$", h));
            Assert.All(f.MediaHashes, h => Assert.Matches("^[0-9A-F]{64}$", h));
            Assert.False(string.IsNullOrWhiteSpace(f.Command));
        }
    }

    internal static void AssertTools(ToolRecord[] tools, bool isWindows)
    {
        if (!isWindows) { Assert.Empty(tools); return; }
        foreach (var t in RequiredTools) Assert.True(Assert.Single(tools, x => x.Name == t).Present, t + " must be present under the MSYS2 root");
    }

    internal static void AssertMaterialization(MaterializationReceipt m)
    {
        Assert.NotNull(m);
        Assert.Empty(m.Changed);
        Assert.Empty(m.Removed);
        Assert.Equal(m.Before, m.After - m.Added.Length);
        Assert.DoesNotContain(m.GeneratedPaths, p => p.Replace('\\', '/').Contains("/native/vice/", StringComparison.Ordinal) || p.Replace('\\', '/').StartsWith("native/vice/", StringComparison.Ordinal));
        Assert.DoesNotContain(m.Added, p => p.Replace('\\', '/').Contains("native/vice/", StringComparison.Ordinal));
    }

    private static IPrerequisiteProvider Valid()
    {
        var p = Substitute.For<IPrerequisiteProvider>();
        p.GetLibraries().Returns(ValidLibraries());
        p.GetFixtures().Returns(ValidFixtures());
        p.GetTools(true).Returns(ValidTools());
        p.GetTools(false).Returns([]);
        p.GetMaterialization().Returns(ValidMaterialization());
        p.HasLegacyNormalization().Returns(true);
        return p;
    }

    [Fact]
    public void C64AndVic20Libraries_AreBuiltFromPinnedViceCommit()
    {
        var p = Valid();
        AssertLibraries(p.GetLibraries());
        var wrongCommit = Valid(); wrongCommit.GetLibraries().Returns(ValidLibraries() with { PinnedSha = new string('0', 40) });
        Assert.ThrowsAny<XunitException>(() => AssertLibraries(wrongCommit.GetLibraries()));
        var missingBinary = Valid(); missingBinary.GetLibraries().Returns(ValidLibraries() with { Binaries = [ValidLibraries().Binaries[0]] });
        Assert.ThrowsAny<XunitException>(() => AssertLibraries(missingBinary.GetLibraries()));
        var missingWrap = Valid(); missingWrap.GetLibraries().Returns(ValidLibraries() with { Binaries = ValidLibraries().Binaries.Select(b => b with { WrapSymbols = b.WrapSymbols.Where(w => w != "machine_read_snapshot").ToArray() }).ToArray() });
        Assert.ThrowsAny<XunitException>(() => AssertLibraries(missingWrap.GetLibraries()));
        var dirtyTree = Valid(); dirtyTree.GetLibraries().Returns(ValidLibraries() with { MaterializationUnchanged = false });
        Assert.ThrowsAny<XunitException>(() => AssertLibraries(dirtyTree.GetLibraries()));
        var badHash = Valid(); badHash.GetLibraries().Returns(ValidLibraries() with { Binaries = ValidLibraries().Binaries.Select(b => b with { Sha256 = "zz" }).ToArray() });
        Assert.ThrowsAny<XunitException>(() => AssertLibraries(badHash.GetLibraries()));
    }

    [Fact]
    public void RequiredFixtures_ArePresentAndHashPinned()
    {
        var p = Valid();
        AssertFixtures(p.GetFixtures());
        void Reject(Func<FixtureRecord[], FixtureRecord[]> mutate)
        {
            var wrong = Valid(); wrong.GetFixtures().Returns(mutate(ValidFixtures()));
            Assert.ThrowsAny<XunitException>(() => AssertFixtures(wrong.GetFixtures()));
        }
        Reject(f => f.Where(x => x.Model != "NTSC").ToArray());                                            // omitted fixture
        Reject(f => f.Select(x => x.Path.EndsWith("truedrive.vsf", StringComparison.Ordinal) ? x with { Sha256 = new string('9', 64) } : x).ToArray()); // wrong hash
        Reject(f => f.Select(x => x.Path.EndsWith("truedrive.vsf", StringComparison.Ordinal) ? x with { Length = 1 } : x).ToArray());                // wrong length
        Reject(f => f.Select(x => x.Machine == "xvic" ? x with { WriteClock = 100007 } : x).ToArray());    // write clock not the literal trigger (R12)
        Reject(f => f.Select(x => x.Machine == "xvic" ? x with { Present = false } : x).ToArray());        // BLOCKED fixture never passes
        Reject(f => f.Select(x => x.Machine == "xvic" ? x with { RomHashes = [] } : x).ToArray());         // ROM hash missing
        Reject(f => f.Select(x => x.Machine == "xvic" ? x with { MediaHashes = ["bad"] } : x).ToArray());  // media hash malformed
        Reject(f => f.Select(x => x.Machine == "xvic" ? x with { Command = "" } : x).ToArray());           // generation command missing
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Msys2Toolchain_HasEveryRequiredCommand(bool isWindows)
    {
        var p = Valid();
        AssertTools(p.GetTools(isWindows), isWindows);
        if (isWindows)
        {
            var missing = Valid(); missing.GetTools(true).Returns(ValidTools().Where(t => t.Name != "make").ToArray());
            Assert.ThrowsAny<XunitException>(() => AssertTools(missing.GetTools(true), true));
            var absent = Valid(); absent.GetTools(true).Returns(ValidTools().Select(t => t.Name == "gcc" ? t with { Present = false } : t).ToArray());
            Assert.ThrowsAny<XunitException>(() => AssertTools(absent.GetTools(true), true));
        }
    }

    [Fact]
    public void BuildInputsOutputsAndGeneratedFiles_StayOutsideNativeVice()
    {
        var p = Valid();
        AssertMaterialization(p.GetMaterialization());
        var inside = Valid(); inside.GetMaterialization().Returns(ValidMaterialization() with { GeneratedPaths = ["native/vice/vice/src/version.h"] });
        Assert.ThrowsAny<XunitException>(() => AssertMaterialization(inside.GetMaterialization()));
        var addedInside = Valid(); addedInside.GetMaterialization().Returns(ValidMaterialization() with { Added = ["x/native/vice/vice/configure"], After = 4121 });
        Assert.ThrowsAny<XunitException>(() => AssertMaterialization(addedInside.GetMaterialization()));
    }

    [Fact]
    public void PinnedSourceTree_IsIdenticalBeforeAndAfterBuild()
    {
        var p = Valid();
        AssertMaterialization(p.GetMaterialization());
        var changed = Valid(); changed.GetMaterialization().Returns(ValidMaterialization() with { Changed = ["vice/src/mainc64cpu.c"] });
        Assert.ThrowsAny<XunitException>(() => AssertMaterialization(changed.GetMaterialization()));
        var removed = Valid(); removed.GetMaterialization().Returns(ValidMaterialization() with { Removed = ["vice/src/vice.h"], After = 4119 });
        Assert.ThrowsAny<XunitException>(() => AssertMaterialization(removed.GetMaterialization()));
        var miscounted = Valid(); miscounted.GetMaterialization().Returns(ValidMaterialization() with { After = 4130 });
        Assert.ThrowsAny<XunitException>(() => AssertMaterialization(miscounted.GetMaterialization()));
    }

    [Fact]
    public void LegacyNormalizationCapability_IsRequired()
    {
        Assert.True(Valid().HasLegacyNormalization());
        var missing = Valid(); missing.HasLegacyNormalization().Returns(false);
        Assert.ThrowsAny<XunitException>(() => Assert.True(missing.HasLegacyNormalization()));
    }
}
