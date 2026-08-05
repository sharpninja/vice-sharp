using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Boundary;

/// <summary>
/// FR-ROMM-PKG-001 (AC-PKG-03), TR-ROMM-BOUNDARY-001. Use case: the portable VM library must depend
/// on exactly ViceSharp.Protocol and hold NO HTTP / RomM.Client / engine / host / desktop-UI / WinRT
/// coupling, so it packs clean for nuget.org and both heads can consume it.
/// </summary>
[Trait("Category", "Library")]
public sealed class LibraryViewModelsBoundaryTests
{
    private static string ProjectDir =>
        Path.Combine(RepoPaths.Root, "src", "ViceSharp.Library.ViewModels");

    private static string CsprojPath =>
        Path.Combine(ProjectDir, "ViceSharp.Library.ViewModels.csproj");

    /// <summary>Fragments that MUST NOT appear anywhere in the csproj (only Protocol is allowed).</summary>
    private static readonly string[] ForbiddenCsprojFragments =
    {
        "RomM.", "System.Net.Http", "Grpc.", "Microsoft.AspNetCore",
        "Avalonia", "Windows", "ViceSharp.Core", "ViceSharp.Chips",
        "ViceSharp.Architectures", "ViceSharp.Host", "ViceSharp.Hosting",
        "ViceSharp.Abstractions",
    };

    /// <summary>Identifiers that MUST NOT appear in the library source.</summary>
    private static readonly string[] ForbiddenSourceIdentifiers =
    {
        "RomM.", "HttpClient", "System.Net.Http", "Grpc.", "Avalonia",
        "Windows.UI", "using System.Windows", "ViceSharp.Core",
        "ViceSharp.Chips", "ViceSharp.Architectures", "ViceSharp.Host",
    };

    /// <summary>AC-PKG-03: the csproj references ViceSharp.Protocol and nothing forbidden.</summary>
    [Fact]
    [Trait("AC", "AC-PKG-03")]
    public void Csproj_ReferencesOnlyProtocol()
    {
        File.Exists(CsprojPath).Should().BeTrue($"csproj expected at {CsprojPath}");

        string project = File.ReadAllText(CsprojPath);
        project.Should().Contain("ViceSharp.Protocol.csproj");

        foreach (string forbidden in ForbiddenCsprojFragments)
        {
            project.Should().NotContain(forbidden, $"the VM library must not reference {forbidden}");
        }
    }

    /// <summary>AC-PKG-03: no forbidden engine/host/UI/HTTP identifiers appear in the library source.</summary>
    [Fact]
    [Trait("AC", "AC-PKG-03")]
    public void Sources_DoNotReferenceHttpEngineOrUi()
    {
        var files = Directory
            .EnumerateFiles(ProjectDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !IsBuildArtifact(p))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        files.Should().NotBeEmpty();

        string source = string.Join("\n", files.Select(File.ReadAllText));

        foreach (string forbidden in ForbiddenSourceIdentifiers)
        {
            source.Should().NotContain(forbidden, $"the VM library source must not mention {forbidden}");
        }
    }

    /// <summary>
    /// AC-PKG-03, TR-ROMM-BOUNDARY-001: the compiled assembly references ViceSharp.Protocol and
    /// carries no HTTP/RomM/engine/host/UI metadata reference.
    /// </summary>
    [Fact]
    [Trait("AC", "AC-PKG-03")]
    public void Assembly_ReferencesNoHttpEngineOrUi()
    {
        var referenced = typeof(MediaExtensionMap).Assembly
            .GetReferencedAssemblies()
            .Select(n => n.Name ?? string.Empty)
            .ToArray();

        referenced.Should().Contain("ViceSharp.Protocol");

        foreach (string forbidden in new[]
        {
            "ViceSharp.Core", "ViceSharp.Chips", "ViceSharp.Architectures",
            "ViceSharp.Host", "ViceSharp.Hosting", "ViceSharp.Avalonia", "System.Net.Http",
        })
        {
            referenced.Should().NotContain(forbidden);
        }

        referenced.Should().NotContain(n =>
            n.StartsWith("RomM", StringComparison.Ordinal)
            || n.StartsWith("Grpc", StringComparison.Ordinal)
            || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
            || n.StartsWith("Avalonia", StringComparison.Ordinal)
            || n.StartsWith("Microsoft.Windows", StringComparison.Ordinal)
            || n.StartsWith("Microsoft.UI", StringComparison.Ordinal));
    }

    private static bool IsBuildArtifact(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
