using System.Text.Json;
using System.Text.Json.Serialization;
using ViceSharp.ViceCompatibility.SnapshotFixtures;
using static ViceSharp.VsfCompatibility.Tests.VsfSlice1ManifestAndResumeContractTests;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 1 as amended (PLAN-NATIVERESTORE-001 D1 N12, D5): the real-bound form of the schema-1
/// per-line ledger assertion. The committed manifest docs/requirements/oracles/vice-vsf-state-transfer-manifest.json
/// (written by native/New-VsfStateTransferManifest.ps1 from the pinned sources) is handed to the identical contract
/// assertion AssertManifest. Translation at the seam (documented, never weakening): the manifest's module entries are
/// the reader units of the pinned machine read orders (c64-snapshot.c 121 to 136, vic20-snapshot.c 129 to 140), each
/// listing the VSF modules it consumes in "snapshotModules"; the coverage set handed to the assertion is computed from
/// the real fixture files by mapping every module of every listed fixture to its unit, and a module without a unit
/// fails here before the contract runs. TR-VSFLOCKSTEP-RESUME-001 AC1; TEST-VSFLOCKSTEP-001.
/// </summary>
[Trait("Category", "VsfSlice1Real")]
public sealed partial class ViceSnapshotStateTransferManifestTests
{
    /// <summary>One reader unit as written by the manifest script: the unit name plus the VSF modules it consumes ("NAME:major.minor[ (condition)]").</summary>
    public sealed record UnitEntry(string Machine, string Name, string[] SnapshotModules);
    /// <summary>The unit view of the manifest file (the fields the contract record does not carry).</summary>
    public sealed record UnitManifest(UnitEntry[] Modules);

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(Manifest))]
    [JsonSerializable(typeof(UnitManifest))]
    internal sealed partial class ManifestJsonContext : JsonSerializerContext;

    internal static Manifest LoadManifest(string path) =>
        JsonSerializer.Deserialize(File.ReadAllBytes(path), ManifestJsonContext.Default.Manifest) ?? throw new InvalidOperationException("manifest did not deserialize: " + path);

    internal static UnitManifest LoadUnits(string path) =>
        JsonSerializer.Deserialize(File.ReadAllBytes(path), ManifestJsonContext.Default.UnitManifest) ?? throw new InvalidOperationException("manifest did not deserialize: " + path);

    /// <summary>Module names of a VSF in file order (pinned snapshot.c framing: 58-byte current header, 22-byte module headers, inclusive little-endian lengths).</summary>
    internal static string[] ModuleNames(byte[] vsf)
    {
        // snapshot.c: 19-byte magic, two version bytes, 16-byte machine name, then the optional 21-byte VICE version block.
        var offset = vsf.Length >= 50 && System.Text.Encoding.ASCII.GetString(vsf, 37, 12) == "VICE Version" ? 58 : 37;
        var names = new List<string>();
        while (offset + 22 <= vsf.Length)
        {
            names.Add(System.Text.Encoding.ASCII.GetString(vsf, offset, 16).TrimEnd('\0'));
            var length = BitConverter.ToUInt32(vsf, offset + 18);
            if (length < 22) throw new InvalidOperationException("module length below the header size at offset " + offset);
            offset += (int)length;
        }
        return [.. names];
    }

    /// <summary>
    /// TR-VSFLOCKSTEP-RESUME-001 AC1 (real manifest); PLAN-NATIVERESTORE-001 D5.
    /// Use case: the committed manifest pins every deterministic module of both machines in the pinned restore order.
    /// Acceptance: the contract assertion passes on the file; every module of every listed fixture file maps to exactly
    /// one unit of its machine; the fixture hashes in the manifest equal the files on disk.
    /// </summary>
    [Fact]
    public void Manifest_CoversEveryDeterministicModule()
    {
        var path = VsfSlice1RealEvidence.ManifestPath;
        Assert.True(File.Exists(path), "manifest missing: " + path);
        var manifest = LoadManifest(path);
        var units = LoadUnits(path).Modules;
        var covered = new List<string>();
        foreach (var fixture in manifest.Fixtures)
        {
            var file = Path.Combine(VsfSlice1RealEvidence.RepoRoot, fixture.Path.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(file), "fixture file missing: " + file);
            Assert.Equal(fixture.Sha256, VsfSlice1RealEvidence.Sha256Of(file));
            Assert.Equal(fixture.Length, new FileInfo(file).Length);
            foreach (var module in ModuleNames(File.ReadAllBytes(file)))
            {
                var owners = units.Where(u => u.Machine == fixture.Machine && u.SnapshotModules.Any(s => s.StartsWith(module + ":", StringComparison.Ordinal))).ToArray();
                Assert.True(owners.Length == 1, $"module {module} of {fixture.Path} is covered by {owners.Length} units of {fixture.Machine}; exactly one is required");
                covered.Add(owners[0].Name);
            }
        }
        Assert.NotEmpty(covered);
        AssertManifest(manifest, [.. covered.Distinct()]);
        // Both machines are present and the xvic read order is pinned too (vic20-snapshot.c 129 to 140).
        var xvic = manifest.Modules.Where(e => e.Machine == "xvic").OrderBy(e => e.RestoreOrder).Select(e => e.Name).ToArray();
        Assert.Equal(["MAINCPU", "VIC20", "VIC-I", "VIA1", "VIA2", "DRIVE8", "DRIVE9", "DRIVE10", "DRIVE11", "FSDRIVE", "EVENT", "TAPEPORT", "KEYBOARD", "JOYPORT1", "USERPORT"], xvic);
    }
}

/// <summary>
/// PLAN-VSFLOCKSTEP-001 Slice 1 as amended (PLAN-NATIVERESTORE-001 D1 N21, N25; D5): the real-bound form of the
/// regeneration assertion. The committed candidate receipts (docs/receipts/native-vice/*/fixture-candidates, two
/// fresh-command runs per host written by GenerateNativeSnapshotFixtures) are handed to the identical contract
/// assertion AssertRegeneration; a blocked classification that is correct passes the contract and is then reported
/// here as BLOCKED by name (round 5 N14), never as a plain failure or a skip.
/// </summary>
[Trait("Category", "VsfSlice1Real")]
public sealed class NativeViceSnapshotFixtureGeneratorTests
{
    internal static IEnumerable<string> CommittedCandidateReceipts()
    {
        var root = Path.Combine(VsfSlice1RealEvidence.RepoRoot, "docs", "receipts", "native-vice");
        if (!Directory.Exists(root)) return [];
        return Directory.EnumerateDirectories(root)
            .Select(d => Path.Combine(d, "fixture-candidates"))
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.candidate.json"))
            .OrderBy(p => p, StringComparer.Ordinal);
    }

    internal static GeneratorOutcome ToOutcome(FixtureCandidate c, string receiptPath, Manifest manifest)
    {
        var retained = c.CandidatePath is null ? null : Path.Combine(Path.GetDirectoryName(receiptPath)!, Path.GetFileName(c.CandidatePath));
        var bytes = retained is not null && File.Exists(retained) ? File.ReadAllBytes(retained) : null;
        if (bytes is not null) Assert.Equal(c.CandidateSha256, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        var accepted = Path.Combine(VsfSlice1RealEvidence.FixtureDirectory, c.FixtureId + ".vsf");
        var acceptedChanged = File.Exists(accepted) && VsfSlice1RealEvidence.Sha256Of(accepted) != c.CandidateSha256;
        var manifestChanged = manifest.Fixtures.Any(f => f.Path.EndsWith(c.FixtureId + ".vsf", StringComparison.Ordinal) && f.Sha256 != c.CandidateSha256);
        return new GeneratorOutcome(c.Host, c.Trigger, c.WriteClock, c.WriterResult, bytes, bytes is null ? null : c.CandidateSha256, acceptedChanged, manifestChanged, c.ExitCode, [.. c.BlockedIds]);
    }

    /// <summary>
    /// TR-VSFLOCKSTEP-RESUME-001 AC1 (real receipts); PLAN-NATIVERESTORE-001 D5, R12 B.
    /// Use case: every fixture id with committed candidate receipts has at least two fresh-command runs whose
    /// classification is correct: accepted runs are byte identical and published, anything else is retained,
    /// unpublished, nonzero and BLOCKED by id.
    /// Acceptance: the contract assertion passes per fixture id; a blocked id is then reported as BLOCKED with the
    /// measured trigger and write clocks of every host (never approximated).
    /// </summary>
    [Fact]
    public void Regeneration_IsByteDeterministic()
    {
        var receipts = CommittedCandidateReceipts().ToArray();
        Assert.True(receipts.Length > 0, "no committed candidate receipts under docs/receipts/native-vice/*/fixture-candidates");
        var manifest = ViceSnapshotStateTransferManifestTests.LoadManifest(VsfSlice1RealEvidence.ManifestPath);
        var byFixture = receipts.Select(p => (Path: p, Candidate: FixturePublisher.LoadCandidate(p))).GroupBy(x => x.Candidate.FixtureId, StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(byFixture);
        var blocked = new List<string>();
        foreach (var group in byFixture)
        {
            var runs = group.Select(x => ToOutcome(x.Candidate, x.Path, manifest)).ToArray();
            AssertRegeneration(runs);
            if (runs.Any(r => r.BlockedIds.Length > 0))
            {
                var clocks = string.Join("; ", group.Select(x => $"{x.Candidate.Host}: trigger {x.Candidate.Trigger}, write clock {(x.Candidate.WriteClock?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none")}, writer result {x.Candidate.WriterResult}, sha256 {x.Candidate.CandidateSha256 ?? "none"}"));
                blocked.Add($"{group.Key}: {clocks}");
            }
        }
        if (blocked.Count > 0)
            Assert.Fail("BLOCKED: " + SnapshotFixtureClassifier.BlockerId + " " + string.Join(" | ", blocked));
    }
}
