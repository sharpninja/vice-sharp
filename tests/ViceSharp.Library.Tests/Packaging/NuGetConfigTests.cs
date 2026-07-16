using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace ViceSharp.Library.Tests.Packaging;

/// <summary>
/// FR-ROMM-PKG-001, TR-ROMM-NUGET-001. Use case: the RomM client packages must resolve from
/// nuget.org and nothing else, so the build stays reproducible on a clean machine.
/// Acceptance (AC-PKG-04): NuGet.config clears inherited feeds and lists nuget.org as the sole source.
/// </summary>
[Trait("Category", "Library")]
public sealed class NuGetConfigTests
{
    /// <summary>AC-PKG-04: the repo NuGet.config is a single-source, nuget.org-only feed list.</summary>
    [Fact]
    [Trait("AC", "AC-PKG-04")]
    public void NugetOrgOnly()
    {
        string path = RepoPaths.File("NuGet.config");
        System.IO.File.Exists(path).Should().BeTrue($"NuGet.config expected at {path}");

        XElement sources = XDocument.Load(path).Root!.Element("packageSources")!;

        sources.Elements("clear").Should().ContainSingle("inherited feeds must be cleared");

        var adds = sources.Elements("add").ToList();
        adds.Should().ContainSingle("nuget.org must be the only package source");
        adds[0].Attribute("value")!.Value.Should().Be("https://api.nuget.org/v3/index.json");
    }
}
