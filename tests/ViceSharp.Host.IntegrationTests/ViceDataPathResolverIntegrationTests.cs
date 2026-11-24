namespace ViceSharp.TestHarness;

using ViceSharp.RomFetch;
using Xunit;

/// <summary>
/// Linux integration coverage for FR-CFG-002 using the process XDG environment.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Host", "Linux")]
public sealed class ViceDataPathResolverIntegrationTests
{
    /// <summary>
    /// FR-CFG-002. Use case: Linux first run before XDG_DATA_HOME exists.
    /// Acceptance: The cache resolves to the absolute user data path without creating it.
    /// </summary>
    [Fact]
    public void DefaultDownloadDirectory_LinuxMissingDataHome_IsAbsolute()
    {
        var dataHome = Path.Combine(Path.GetTempPath(), "ViceSharpDataResolverTests", Guid.NewGuid().ToString("N"));
        var previous = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        try
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", dataHome);
            Assert.Equal(Path.Combine(dataHome, "ViceSharp", "roms"), ViceDataPathResolver.DefaultDownloadDirectory);
            Assert.False(Directory.Exists(dataHome));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previous);
        }
    }

    /// <summary>
    /// FR-CFG-002. Use case: A configured VICE installation and downloaded cache both exist.
    /// Acceptance: Discovery puts the configured root before the per-user cache.
    /// </summary>
    [Fact]
    public void FindDataRoots_ConfiguredRootPrecedesDownloadedCache()
    {
        var root = CreateViceDataRoot();
        var previousHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var previousRoms = Environment.GetEnvironmentVariable("VICESHARP_ROM_PATH");
        try
        {
            var dataHome = Path.Combine(root, "user-data");
            var cache = Path.Combine(dataHome, "ViceSharp", "roms");
            Directory.CreateDirectory(Path.Combine(cache, "C64"));
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", dataHome);
            Environment.SetEnvironmentVariable("VICESHARP_ROM_PATH", root);

            var roots = ViceDataPathResolver.FindDataRoots();
            Assert.Equal(root, roots[0]);
            Assert.True(Array.IndexOf(roots.ToArray(), cache) > 0);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previousHome);
            Environment.SetEnvironmentVariable("VICESHARP_ROM_PATH", previousRoms);
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateViceDataRoot()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "ViceSharpDataResolverTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "C64"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "DRIVES"));
        return dataRoot;
    }
}
