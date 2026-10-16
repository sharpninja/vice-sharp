namespace ViceSharp.TestHarness;

using ViceSharp.RomFetch;
using Xunit;

public sealed class ViceDataPathResolverTests
{
    /// <summary>
    /// FR-CFG-002. Use case: Linux first run before XDG_DATA_HOME exists.
    /// Acceptance: The cache resolves to the absolute user data path without creating it.
    /// </summary>
    [Fact]
    public void DefaultDownloadDirectory_LinuxMissingDataHome_IsAbsolute()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "XDG data roots apply to Linux.");
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
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Uses an isolated XDG data root.");
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

    /// <summary>
    /// FR: FR-CFG-001.
    /// Use case: A user points ViceSharp at the root of a native VICE
    /// install, which contains machine folders such as C64 and DRIVES.
    /// Acceptance: The resolver keeps the data root unchanged.
    /// </summary>
    [Fact]
    public void NormalizeDataRootOrDefault_AcceptsViceDataRoot()
    {
        var dataRoot = CreateViceDataRoot();

        try
        {
            Assert.Equal(dataRoot, ViceDataPathResolver.NormalizeDataRootOrDefault(dataRoot));
        }
        finally
        {
            Directory.Delete(dataRoot, recursive: true);
        }
    }

    /// <summary>
    /// FR: FR-CFG-001.
    /// Use case: Existing setup documentation may point directly at the C64
    /// resource folder from a VICE install.
    /// Acceptance: The resolver normalizes the C64 path back to the parent
    /// data root so other architecture folders remain available.
    /// </summary>
    [Fact]
    public void NormalizeDataRootOrDefault_NormalizesC64DirectoryToDataRoot()
    {
        var dataRoot = CreateViceDataRoot();

        try
        {
            Assert.Equal(dataRoot, ViceDataPathResolver.NormalizeDataRootOrDefault(Path.Combine(dataRoot, "C64")));
        }
        finally
        {
            Directory.Delete(dataRoot, recursive: true);
        }
    }

    /// <summary>
    /// FR: FR-CFG-001.
    /// Use case: The local machine exposes x64sc.exe from inside a native
    /// VICE install.
    /// Acceptance: The resolver derives the parent VICE data root from the
    /// executable path.
    /// </summary>
    [Fact]
    public void NormalizeDataRootOrDefault_NormalizesX64ScPathToDataRoot()
    {
        var dataRoot = CreateViceDataRoot();

        try
        {
            var binDirectory = Path.Combine(dataRoot, "bin");
            Directory.CreateDirectory(binDirectory);
            var executablePath = Path.Combine(binDirectory, OperatingSystem.IsWindows() ? "x64sc.exe" : "x64sc");
            File.WriteAllBytes(executablePath, []);

            Assert.Equal(dataRoot, ViceDataPathResolver.NormalizeDataRootOrDefault(executablePath));
        }
        finally
        {
            Directory.Delete(dataRoot, recursive: true);
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
