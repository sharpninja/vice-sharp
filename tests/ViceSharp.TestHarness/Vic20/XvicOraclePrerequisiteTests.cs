namespace ViceSharp.TestHarness.Vic20;

using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

/// <summary>
/// FR-SNP-005 AC7 and TEST-SNP-002 AC6.
/// Acceptance: the pinned xvic executable, vice_xvic native library, VIC-20 ROM
/// allowlist, manifest hashes, and named workload are mandatory. A missing or
/// stale prerequisite fails the gate and is never converted into a skipped pass.
/// </summary>
public sealed class XvicOraclePrerequisiteTests
{
    [Fact]
    public void Validate_FailsWhenXvicBinaryIsMissing()
    {
        using var fixture = OracleFixture.Create();
        File.Delete(fixture.XvicPath);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Validate());

        Assert.Contains("xvic executable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FailsWhenViceXvicLibraryIsMissing()
    {
        using var fixture = OracleFixture.Create();
        File.Delete(fixture.NativeLibraryPath);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Validate());

        Assert.Contains("vice_xvic native library", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FailsWhenVic20RomSetIsMissingFromConfiguredRoot()
    {
        using var fixture = OracleFixture.Create();
        File.Delete(Path.Combine(fixture.RomRoot, "VIC20", "basic-901486-01.bin"));

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Validate());

        Assert.Contains("ROM", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FailsWhenManifestIsMissing()
    {
        using var fixture = OracleFixture.Create();
        File.Delete(fixture.ManifestPath);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Validate());

        Assert.Contains("manifest", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FailsWhenManifestHashDoesNotMatchFile()
    {
        using var fixture = OracleFixture.Create();
        File.WriteAllBytes(fixture.NativeLibraryPath, [0xFF, 0xEE, 0xDD]);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Validate());

        Assert.Contains("hash", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_FailsWhenNamedWorkloadIsMissing()
    {
        using var fixture = OracleFixture.Create();
        File.Delete(fixture.WorkloadPath);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Validate());

        Assert.Contains(Path.GetFileName(fixture.WorkloadPath), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AcceptsCompleteOracleFixture()
    {
        using var fixture = OracleFixture.Create();

        fixture.Validate();
    }

    private static void InvokeGate(
        string manifestPath,
        string nativeLibraryPath,
        string romRoot,
        string workloadProgram)
        => XvicOraclePrerequisites.Validate(
            manifestPath,
            nativeLibraryPath,
            romRoot,
            workloadProgram);

    private sealed class OracleFixture : IDisposable
    {
        private const string DefaultWorkloadProgram = "power-on-pal.prg";

        private OracleFixture(string root)
        {
            Root = root;
            ManifestPath = Path.Combine(root, "vic20-xvic-manifest.json");
            XvicPath = Path.Combine(root, "xvic.exe");
            NativeLibraryPath = Path.Combine(root, "vice_xvic.dll");
            RomRoot = Path.Combine(root, "data");
            WorkloadPath = Path.Combine(RomRoot, "VIC20", DefaultWorkloadProgram);
        }

        public string Root { get; }
        public string ManifestPath { get; }
        public string XvicPath { get; }
        public string NativeLibraryPath { get; }
        public string RomRoot { get; }
        public string WorkloadPath { get; }

        public static OracleFixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), $"vice-sharp-xvic-prerequisites-{Guid.NewGuid():N}");
            var fixture = new OracleFixture(root);
            Directory.CreateDirectory(Path.Combine(fixture.RomRoot, "VIC20"));

            File.WriteAllBytes(fixture.XvicPath, [0x58, 0x56, 0x49, 0x43]);
            File.WriteAllBytes(fixture.NativeLibraryPath, [0x56, 0x49, 0x43, 0x45]);
            File.WriteAllBytes(fixture.WorkloadPath, [0x01, 0x08, 0x00, 0x00]);

            var roms = new[]
            {
                fixture.WriteRom("basic-901486-01.bin", 0x2000, 0x01),
                fixture.WriteRom("kernal.901486-02.bin", 0x2000, 0x02),
                fixture.WriteRom("kernal.901486-06.bin", 0x2000, 0x06),
                fixture.WriteRom("kernal.901486-07.bin", 0x2000, 0x07),
                fixture.WriteRom("chargen-901460-02.bin", 0x1000, 0x02),
                fixture.WriteRom("chargen-901460-03.bin", 0x1000, 0x03),
            };

            var manifest = new
            {
                viceCommit = "cc67418cba58e71d8e70fd353e9de47199ad2075",
                viceBuildHash = HashFile(fixture.XvicPath),
                xvicPath = fixture.XvicPath,
                viceXvicDllSha256 = HashFile(fixture.NativeLibraryPath),
                machine = new[] { "vic20", "vic20ntsc" },
                media = Array.Empty<object>(),
                romHashes = roms,
                workloads = new[]
                {
                    new
                    {
                        program = DefaultWorkloadProgram,
                        machine = "vic20",
                        cycleStart = 0,
                        cycleCount = 64,
                        source = "XvicOraclePrerequisiteTests",
                    },
                },
            };

            File.WriteAllText(
                fixture.ManifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            return fixture;
        }

        public void Validate(string workloadProgram = DefaultWorkloadProgram)
            => InvokeGate(ManifestPath, NativeLibraryPath, RomRoot, workloadProgram);

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }

        private object WriteRom(string name, int length, byte value)
        {
            var path = Path.Combine(RomRoot, "VIC20", name);
            File.WriteAllBytes(path, Enumerable.Repeat(value, length).ToArray());
            return new { name, path, sha256 = HashFile(path) };
        }

        private static string HashFile(string path)
            => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }
}
