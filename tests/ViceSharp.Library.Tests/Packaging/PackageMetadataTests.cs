using FluentAssertions;
using Xunit;

namespace ViceSharp.Library.Tests.Packaging;

/// <summary>
/// FR-ROMM-PKG-001 (AC-PKG-01/02). Use case: both RomM libraries ship as nuget.org packages with the
/// right identity, license, and dependencies, wired into the release pack list.
/// </summary>
[Trait("Category", "Library")]
public sealed class PackageMetadataTests
{
    private static string Csproj(string project) =>
        File.ReadAllText(Path.Combine(RepoPaths.Root, "src", project, project + ".csproj"));

    /// <summary>AC-PKG-01: both libraries are packable with a package id and GPL-2.0-or-later license.</summary>
    [Fact]
    [Trait("AC", "AC-PKG-01")]
    public void Ids_License()
    {
        foreach (string project in new[] { "ViceSharp.Library.ViewModels", "ViceSharp.RomM" })
        {
            string csproj = Csproj(project);
            csproj.Should().Contain("<IsPackable>true</IsPackable>");
            csproj.Should().Contain($"<PackageId>{project}</PackageId>");
        }

        string props = File.ReadAllText(Path.Combine(RepoPaths.Root, "Directory.Build.props"));
        props.Should().Contain("<PackageLicenseExpression>GPL-2.0-or-later</PackageLicenseExpression>");
    }

    /// <summary>AC-PKG-02: the packages declare the correct deps and are wired into the release pack list.</summary>
    [Fact]
    [Trait("AC", "AC-PKG-02")]
    public void Dependencies()
    {
        string viewModels = Csproj("ViceSharp.Library.ViewModels");
        viewModels.Should().Contain("ViceSharp.Protocol.csproj");

        string adapter = Csproj("ViceSharp.RomM");
        adapter.Should().Contain("ViceSharp.Library.ViewModels.csproj");
        adapter.Should().Contain("ViceSharp.Protocol.csproj");
        adapter.Should().Contain("Include=\"RomM.Client\"");
        adapter.Should().Contain("Include=\"RomM.Client.Csdb\"");

        string build = File.ReadAllText(Path.Combine(RepoPaths.Root, "build", "Build.cs"));
        build.Should().Contain("\"ViceSharp.Library.ViewModels\"");
        build.Should().Contain("\"ViceSharp.RomM\"");
    }
}
