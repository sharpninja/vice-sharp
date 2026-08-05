using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace ViceSharp.Library.Tests.Packaging;

/// <summary>
/// FR-ROMM-PKG-001, TR-ROMM-NUGET-001. Use case: ViceSharp.RomM consumes the prebuilt RomM client
/// packages from nuget.org rather than vendoring source, so their versions are centrally pinned.
/// Acceptance (AC-PKG-04): RomM.Client 1.0.0 and RomM.Client.Csdb 1.0.0 are pinned under CPM.
/// </summary>
[Trait("Category", "Library")]
public sealed class DirectoryPackagesTests
{
    private static readonly IReadOnlyDictionary<string, string> Versions = LoadVersions();

    /// <summary>AC-PKG-04: each RomM client package is pinned at exactly version 1.0.0.</summary>
    [Theory]
    [Trait("AC", "AC-PKG-04")]
    [InlineData("RomM.Client", "1.0.0")]
    [InlineData("RomM.Client.Csdb", "1.0.0")]
    public void RommPinned(string packageId, string expectedVersion)
    {
        Versions.Should().ContainKey(packageId, $"{packageId} must be pinned in Directory.Packages.props");
        Versions[packageId].Should().Be(expectedVersion);
    }

    private static Dictionary<string, string> LoadVersions()
    {
        string path = RepoPaths.File("Directory.Packages.props");
        return XDocument.Load(path)
            .Descendants("PackageVersion")
            .Where(e => e.Attribute("Include") is not null && e.Attribute("Version") is not null)
            .ToDictionary(
                e => e.Attribute("Include")!.Value,
                e => e.Attribute("Version")!.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
