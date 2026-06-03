namespace ViceSharp.TestHarness.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ViceSharp.Core.Configuration;
using Xunit;

/// <summary>
/// SETTINGS-INI-001: the deployed appsettings.json carries exactly one setting -
/// the folder for the INI files (ViceSharp:ConfigDirectory) - and defaults to the
/// canonical VICE location when empty. ViceConfigLocator resolves it.
/// </summary>
public sealed class ViceConfigLocatorTests
{
    private static IConfiguration InMemory(params (string Key, string? Value)[] pairs)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();

    [Fact]
    public void ResolveConfigDirectory_Unset_ReturnsViceDefault()
        => Assert.Equal(ViceConfigLocator.GetViceDefaultDirectory(), ViceConfigLocator.ResolveConfigDirectory(InMemory()));

    [Fact]
    public void ResolveConfigDirectory_Blank_ReturnsViceDefault()
        => Assert.Equal(
            ViceConfigLocator.GetViceDefaultDirectory(),
            ViceConfigLocator.ResolveConfigDirectory(InMemory(("ViceSharp:ConfigDirectory", "   "))));

    [Fact]
    public void ResolveConfigDirectory_Set_ReturnsConfiguredAbsolutePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vicesharp-cfg-test");

        Assert.Equal(
            Path.GetFullPath(dir),
            ViceConfigLocator.ResolveConfigDirectory(InMemory(("ViceSharp:ConfigDirectory", dir))));
    }

    [Fact]
    public void ResolveConfigDirectory_ExpandsEnvironmentVariables()
    {
        var resolved = ViceConfigLocator.ResolveConfigDirectory(
            InMemory(("ViceSharp:ConfigDirectory", Path.Combine("%TEMP%", "vice"))));

        Assert.DoesNotContain("%", resolved);
        Assert.EndsWith("vice", resolved, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetViceDefaultDirectory_EndsWithViceFolder()
        => Assert.EndsWith("vice", ViceConfigLocator.GetViceDefaultDirectory(), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void IniPaths_AreUnderConfigDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vicesharp-cfg-test");
        var config = InMemory(("ViceSharp:ConfigDirectory", dir));

        Assert.Equal(Path.Combine(Path.GetFullPath(dir), "vice.ini"), ViceConfigLocator.ViceIniPath(config));
        Assert.Equal(Path.Combine(Path.GetFullPath(dir), "vice-sharp.ini"), ViceConfigLocator.ViceSharpIniPath(config));
    }

    [Fact]
    public void DeployedAppSettings_OnlyConfiguresIniFolder_DefaultingToViceDefault()
    {
        var appsettings = Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "appsettings.json");
        Assert.True(File.Exists(appsettings), $"the app must deploy appsettings.json (expected at {appsettings})");

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(appsettings)!)
            .AddJsonFile(Path.GetFileName(appsettings), optional: false)
            .Build();

        // The shipped file leaves ConfigDirectory empty, so it resolves to VICE's default.
        Assert.Equal(ViceConfigLocator.GetViceDefaultDirectory(), ViceConfigLocator.ResolveConfigDirectory(config));
    }

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir!.FullName;
        }
    }
}
