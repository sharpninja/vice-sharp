using System.Security.Cryptography;
using System.Text.Json;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// Locates the real Slice 1 evidence on the running host (PLAN-NATIVERESTORE-001 D5): the repository root, the
/// pinned materialization tree with its builder receipts and observer binaries, the fixtures, the manifest and the
/// blockers file. Absence of any item is reported by the test that needs it as a failure, never a skip (Slice 1's
/// last item).
/// </summary>
internal static class VsfSlice1RealEvidence
{
    public const string PinnedSha = VsfSlice1PrerequisiteContractTests.PinnedSha;
    public static readonly string HostTag = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
    public static readonly string ExeSuffix = OperatingSystem.IsWindows() ? ".exe" : "";

    public static string RepoRoot { get; } = FindRepoRoot();
    public static string MaterializationRoot => Path.Combine(RepoRoot, "artifacts", "immutable-vice", PinnedSha);
    public static string ReceiptsDirectory => Path.Combine(MaterializationRoot, "receipts-" + HostTag);
    public static string BinDirectory => Path.Combine(MaterializationRoot, "bin-" + HostTag);
    public static string BuildDirectory => Path.Combine(MaterializationRoot, "build-" + HostTag);
    public static string WorkDirectory => Path.Combine(MaterializationRoot, "work-" + HostTag);
    public static string DataRoot => Path.Combine(WorkDirectory, "vice", "data");
    public static string ObserverPath(string machine) => Path.Combine(BinDirectory, machine + "-observer" + ExeSuffix);
    public static string SuppliedFixture => Path.Combine(RepoRoot, "tests", "ViceSharp.TestHarness", "Fixtures", "Vsf", "ready-c64sc-truedrive.vsf");
    public static string FixtureDirectory => Path.Combine(RepoRoot, "tests", "ViceSharp.TestHarness", "Fixtures", "Vsf");
    public static string ManifestPath => Path.Combine(RepoRoot, "docs", "requirements", "oracles", "vice-vsf-state-transfer-manifest.json");
    public static string BlockersPath => Path.Combine(RepoRoot, "docs", "requirements", "oracles", "vice-vsf-fixture-blockers.json");
    public static string FixtureGenerationDirectory => Path.Combine(RepoRoot, "docs", "requirements", "oracles", "fixture-generation");

    public static string Sha256Of(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    /// <summary>The newest receipt of a stage (file name &lt;stamp&gt;-&lt;stage&gt;.json), or null.</summary>
    public static string? LatestReceipt(string stage)
    {
        if (!Directory.Exists(ReceiptsDirectory)) return null;
        return Directory.EnumerateFiles(ReceiptsDirectory, "*-" + stage + ".json").OrderBy(p => p, StringComparer.Ordinal).LastOrDefault();
    }

    public static JsonDocument ReadJson(string path) => JsonDocument.Parse(File.ReadAllBytes(path));

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ViceSharp.slnx")))
                return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("ViceSharp.slnx not found above " + AppContext.BaseDirectory);
    }
}
