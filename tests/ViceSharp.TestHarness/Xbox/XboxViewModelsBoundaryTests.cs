namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Linq;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S19 (IMPL-XBOXUWP-019). FR-XBOXTOPO-001 / TR-MVVM-001,
/// TR-GRPC-BOUNDARY-001: the portable 10-foot-UI ViewModels library
/// (<c>ViceSharp.Xbox.ViewModels</c>) is created as a plain net10.0 assembly that
/// holds all navigation / focus-graph / virtual-keyboard / video-pull /
/// settings-device-ROM logic in later slices. This slice ships only the project
/// plus the boundary guard: the ViewModels layer may depend on exactly the three
/// portable contract projects (Abstractions, Protocol, Xbox.Input) and MUST NOT
/// reach the emulation engine (Core/Chips/Architectures), the host composition
/// (Host/Host.InProcess), the desktop UI (Avalonia), the monitor, or any
/// gRPC-server / ASP.NET / XAML / WinRT stack.
///
/// <para>
/// Modeled on <c>AvaloniaBoundaryTests</c> (csproj reference assertion + raw-source
/// forbidden-identifier scan + runtime referenced-assembly assertion). These guards
/// need no UWP workload and no linker; they run under plain <c>dotnet test</c> on any
/// agent.
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxViewModelsBoundaryTests
{
    /// <summary>Project reference substrings that MUST be present in the csproj.</summary>
    private static readonly string[] RequiredProjectReferences =
    {
        "ViceSharp.Abstractions.csproj",
        "ViceSharp.Protocol.csproj",
        "ViceSharp.Xbox.Input.csproj",
    };

    /// <summary>
    /// Reference substrings that MUST NOT appear anywhere in the csproj (engine,
    /// host composition, desktop UI, monitor, and the gRPC/ASP.NET/XAML/WinRT
    /// package families).
    /// </summary>
    private static readonly string[] ForbiddenCsprojFragments =
    {
        "ViceSharp.Core.csproj",
        "ViceSharp.Chips.csproj",
        "ViceSharp.Architectures.csproj",
        "ViceSharp.Host.csproj",
        "ViceSharp.Host.InProcess.csproj",
        "ViceSharp.Avalonia.csproj",
        "ViceSharp.Monitor.csproj",
        "Grpc.",
        "Microsoft.AspNetCore",
        "Avalonia.",
        "Windows.",
    };

    /// <summary>
    /// Engine / host / XAML identifiers that MUST NOT appear in ViewModels source.
    /// ViceSharp.Abstractions, ViceSharp.Protocol and ViceSharp.Xbox.Input are
    /// deliberately NOT forbidden: they are the portable contracts the ViewModels
    /// are allowed to consume (TR-MVVM-001).
    /// </summary>
    private static readonly string[] ForbiddenSourceIdentifiers =
    {
        "ViceSharp.Core",
        "ViceSharp.Chips",
        "ViceSharp.Architectures",
        "ViceSharp.Host",
        "IArchitectureBuilder",
        "IMachine",
        "IVideoChip",
        "Grpc.",
        "Avalonia",
        "Windows.UI",
        "using System.Windows",
    };

    /// <summary>
    /// FR-XBOXTOPO-001, TR-MVVM-001, TR-GRPC-BOUNDARY-001.
    /// Use case: the portable ViewModels library must depend on only the three
    /// portable contract projects; reaching into Core/Chips/Architectures, the
    /// host composition, the desktop UI, the monitor, or any gRPC/ASP.NET/XAML/WinRT
    /// package would break the UWP-on-console AOT/AppContainer topology.
    /// Acceptance: <c>ViceSharp.Xbox.ViewModels.csproj</c> contains ProjectReferences
    /// to Abstractions, Protocol and Xbox.Input and NONE of the forbidden project or
    /// package fragments.
    /// </summary>
    [Fact]
    public void ViewModelsProject_ReferencesOnlyPortableContractProjects()
    {
        var csprojPath = Path.Combine(
            RepoRoot, "src", "ViceSharp.Xbox.ViewModels", "ViceSharp.Xbox.ViewModels.csproj");

        Assert.True(File.Exists(csprojPath), $"Expected the ViewModels csproj at '{csprojPath}'.");

        var project = File.ReadAllText(csprojPath);

        foreach (var required in RequiredProjectReferences)
            Assert.Contains(required, project);

        foreach (var forbidden in ForbiddenCsprojFragments)
            Assert.DoesNotContain(forbidden, project);
    }

    /// <summary>
    /// FR-XBOXTOPO-001, TR-MVVM-001.
    /// Use case: even with clean project references, ViewModels source must not carry
    /// textual references to the engine, host, monitor, or XAML/WinRT namespaces - a
    /// textual usage would imply reflective or leaked coupling.
    /// Acceptance: the concatenated ViewModels source (all <c>*.cs</c>, excluding
    /// <c>obj</c>/<c>bin</c>) contains none of the forbidden engine/host/XAML
    /// identifiers.
    /// </summary>
    [Fact]
    public void ViewModelsSources_DoNotReferenceEngineHostOrXamlInternals()
    {
        var sourceRoot = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox.ViewModels");
        var files = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(files);

        var source = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        foreach (var forbidden in ForbiddenSourceIdentifiers)
            Assert.DoesNotContain(forbidden, source);
    }

    /// <summary>
    /// FR-XBOXTOPO-001, TR-MVVM-001, TR-GRPC-BOUNDARY-001.
    /// Use case: the compiled ViewModels assembly must not carry a metadata reference
    /// to any engine, host, desktop-UI, gRPC, ASP.NET, or Windows/WinRT assembly.
    /// Acceptance: <c>typeof(NavigationDestination).Assembly.GetReferencedAssemblies()</c>
    /// contains no ViceSharp engine/host/UI assembly and no
    /// Grpc/ASP.NET/Windows/Avalonia assembly.
    /// </summary>
    [Fact]
    public void ViewModelsAssembly_ReferencesNoEngineHostOrUiAssemblies()
    {
        var referenced = typeof(NavigationDestination).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();

        foreach (var forbidden in new[]
        {
            "ViceSharp.Core",
            "ViceSharp.Chips",
            "ViceSharp.Architectures",
            "ViceSharp.Host",
            "ViceSharp.Host.InProcess",
            "ViceSharp.Avalonia",
            "ViceSharp.Monitor",
        })
        {
            Assert.DoesNotContain(forbidden, referenced);
        }

        Assert.DoesNotContain(
            referenced,
            name =>
                name.StartsWith("Grpc", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.StartsWith("Avalonia", StringComparison.Ordinal)
                || name.StartsWith("Windows", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.Windows", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.UI", StringComparison.Ordinal));
    }

    private static bool IsBuildArtifact(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root (ViceSharp.slnx).");

            return directory.FullName;
        }
    }
}
