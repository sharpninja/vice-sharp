using System.Text.Json;
using ViceSharp.ViceCompatibility;
using ViceSharp.ViceCompatibility.SnapshotFixtures;
using static ViceSharp.VsfCompatibility.Tests.VsfSlice1PrerequisiteContractTests;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 1 (as amended 2026-10-20) real-bound prerequisite tests: the mock-proven assertions
/// of VsfSlice1PrerequisiteContractTests bound to the builder receipts, the observer binaries, the fixtures and the
/// manifest on this host (PLAN-NATIVERESTORE-001 D5). TR-VSFLOCKSTEP-RESUME-001 AC1, AC2; TEST-VSFLOCKSTEP-001;
/// TEST-VICE-001 AC1, AC2. Absence is an assertion failure; a blocked xvic fixture fails with the BLOCKED prefix.
/// </summary>
[Trait("Category", "VsfSlice1Real")]
public sealed class NativeViceSnapshotPrerequisiteTests
{
    /// <summary>The real prerequisite provider over the receipts and files of this host.</summary>
    internal sealed class RealPrerequisiteProvider : IPrerequisiteProvider
    {
        public LibraryReceipt GetLibraries()
        {
            var link = VsfSlice1RealEvidence.LatestReceipt("link") ?? throw new InvalidOperationException("no link receipt under " + VsfSlice1RealEvidence.ReceiptsDirectory);
            using var doc = VsfSlice1RealEvidence.ReadJson(link);
            var binaries = new List<Binary>();
            foreach (var result in doc.RootElement.GetProperty("Results").EnumerateArray())
            {
                var machine = result.GetProperty("Machine").GetString()!;
                var output = result.GetProperty("Output").GetString()!;
                var recorded = result.GetProperty("Sha256").GetString()!;
                var onDisk = File.Exists(output) ? VsfSlice1RealEvidence.Sha256Of(output) : "missing";
                var wraps = result.GetProperty("WrapSymbols").EnumerateArray().Select(e => e.GetString()!)
                    .Select(line => line.Split(' ').Last())
                    .Where(symbol => symbol.StartsWith("__wrap_", StringComparison.Ordinal))
                    .Select(symbol => symbol["__wrap_".Length..])
                    .Concat(result.GetProperty("Wraps").EnumerateArray().Select(e => e.GetString()!.Replace("-Wl,--wrap=", "", StringComparison.Ordinal)))
                    .Distinct(StringComparer.Ordinal).ToArray();
                // The receipt's hash must describe the binary on disk; a stale receipt yields "missing" or a different hash.
                binaries.Add(new Binary(machine, output, string.Equals(onDisk, recorded, StringComparison.OrdinalIgnoreCase) ? recorded : "RECEIPT-" + recorded + "-DISK-" + onDisk, wraps));
            }
            return new LibraryReceipt(VsfSlice1RealEvidence.PinnedSha, binaries.OrderBy(b => b.Machine, StringComparer.Ordinal).ToArray(), GetMaterialization() is { Changed.Length: 0, Removed.Length: 0 });
        }

        public FixtureRecord[] GetFixtures()
        {
            var records = new List<FixtureRecord>();
            var supplied = VsfSlice1RealEvidence.SuppliedFixture;
            var present = File.Exists(supplied);
            records.Add(new FixtureRecord(supplied, "x64sc", "PAL", present ? VsfSlice1RealEvidence.Sha256Of(supplied) : "", present ? new FileInfo(supplied).Length : 0, 0, 0, [.. RomAllowlist.BaseFixtureSubset.Where(kv => kv.Key.StartsWith("C64/", StringComparison.Ordinal)).Select(kv => kv.Value)], [], "supplied", present));
            var manifest = File.Exists(VsfSlice1RealEvidence.ManifestPath) ? FixtureManifest.LoadOrCreate(VsfSlice1RealEvidence.ManifestPath) : new FixtureManifest();
            foreach (var (model, label) in new[] { ("vic20", "PAL"), ("vic20ntsc", "NTSC") })
            {
                var entry = manifest.Fixtures.FirstOrDefault(f => f.Machine == "xvic" && f.Model == model);
                if (entry is null)
                {
                    records.Add(new FixtureRecord("(not in the manifest)", "xvic", label, "", 0, 100000, 0, [], [], "", false));
                    continue;
                }
                var path = Path.Combine(VsfSlice1RealEvidence.RepoRoot, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                var exists = File.Exists(path) && string.Equals(VsfSlice1RealEvidence.Sha256Of(path), entry.Sha256, StringComparison.OrdinalIgnoreCase) && new FileInfo(path).Length == entry.Length;
                records.Add(new FixtureRecord(entry.Path, "xvic", label, entry.Sha256, entry.Length, entry.TriggerClock, entry.WriteClock, entry.RomHashes, entry.MediaHashes, entry.Command, exists));
            }
            return [.. records];
        }

        public ToolRecord[] GetTools(bool isWindows)
        {
            if (!isWindows) return [];
            var root = @"C:\msys64";
            var paths = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["gcc"] = Path.Combine(root, "mingw64", "bin", "gcc.exe"), ["make"] = Path.Combine(root, "usr", "bin", "make.exe"),
                ["autoconf"] = Path.Combine(root, "usr", "bin", "autoconf"), ["automake"] = Path.Combine(root, "usr", "bin", "automake"),
                ["libtool"] = Path.Combine(root, "usr", "bin", "libtool"), ["bash"] = Path.Combine(root, "usr", "bin", "bash.exe"),
            };
            return RequiredTools.Select(t => new ToolRecord(t, paths[t], File.Exists(paths[t]))).ToArray();
        }

        public MaterializationReceipt GetMaterialization()
        {
            var inventory = VsfSlice1RealEvidence.LatestReceipt("inventory") ?? throw new InvalidOperationException("no inventory receipt under " + VsfSlice1RealEvidence.ReceiptsDirectory);
            using var doc = VsfSlice1RealEvidence.ReadJson(inventory);
            var root = doc.RootElement;
            var knownAdded = root.GetProperty("KnownAddedList").GetString()!;
            var added = File.Exists(knownAdded) ? File.ReadAllLines(knownAdded).Where(l => l.Length > 0).ToArray() : [];
            return new MaterializationReceipt(
                root.GetProperty("Before").GetInt32(), root.GetProperty("After").GetInt32(),
                root.GetProperty("Changed").EnumerateArray().Select(e => e.GetString()!).ToArray(),
                root.GetProperty("Removed").EnumerateArray().Select(e => e.GetString()!).ToArray(),
                added,
                [VsfSlice1RealEvidence.BuildDirectory, VsfSlice1RealEvidence.BinDirectory, VsfSlice1RealEvidence.WorkDirectory, VsfSlice1RealEvidence.ReceiptsDirectory]);
        }

        public bool HasLegacyNormalization()
        {
            var supplied = VsfSlice1RealEvidence.SuppliedFixture;
            if (!File.Exists(supplied)) return false;
            var normalized = X64ScSnapshotCompatibility.Normalize(File.ReadAllBytes(supplied));
            return X64ScNativeLaunch.ReadMainCpu(normalized).Minor == 4;
        }
    }

    private static readonly RealPrerequisiteProvider Real = new();

    private static void FailIfBlocked(string model)
    {
        var fixtureId = model == "PAL" ? "xvic-pal-unexpanded" : "xvic-ntsc-unexpanded";
        var blocker = FixtureBlockers.Load(VsfSlice1RealEvidence.BlockersPath).FirstOrDefault(b => b.FixtureId == fixtureId);
        if (blocker is not null)
            Assert.Fail(FixtureBlockers.Message(blocker));
    }

    [Fact]
    public void C64AndVic20Libraries_AreBuiltFromPinnedViceCommit() => AssertLibraries(Real.GetLibraries());

    [Fact]
    public void RequiredFixtures_ArePresentAndHashPinned()
    {
        FailIfBlocked("PAL");
        FailIfBlocked("NTSC");
        AssertFixtures(Real.GetFixtures());
    }

    [Fact]
    public void Msys2Toolchain_HasEveryRequiredCommand() => AssertTools(Real.GetTools(OperatingSystem.IsWindows()), OperatingSystem.IsWindows());

    [Fact]
    public void LegacyNormalizationCapability_IsRequired() => Assert.True(Real.HasLegacyNormalization(), "the external v1.2 to v1.4 MAINCPU normalization must exist and apply to the supplied fixture");
}

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 1 real-bound build tests: the builder's inventory receipt proves generated files
/// stay outside native/vice and the pinned tree hashes unchanged after the build. TEST-VICE-001 AC1, AC2;
/// TR-VICE-ORACLE-001 AC1, AC2.
/// </summary>
[Trait("Category", "VsfSlice1Real")]
public sealed class ImmutableViceBuildTests
{
    private static readonly NativeViceSnapshotPrerequisiteTests.RealPrerequisiteProvider Real = new();

    [Fact]
    public void BuildInputsOutputsAndGeneratedFiles_StayOutsideNativeVice() => AssertMaterialization(Real.GetMaterialization());

    [Fact]
    public void PinnedSourceTree_IsIdenticalBeforeAndAfterBuild()
    {
        var m = Real.GetMaterialization();
        AssertMaterialization(m);
        var inventory = VsfSlice1RealEvidence.LatestReceipt("inventory")!;
        using var doc = VsfSlice1RealEvidence.ReadJson(inventory);
        Assert.True(doc.RootElement.GetProperty("OriginalFilesUnchanged").GetBoolean(), "the Inventory stage must report the original files unchanged");
    }
}
