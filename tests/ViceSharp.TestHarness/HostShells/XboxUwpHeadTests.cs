namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S4 (IMPL-XBOXUWP-004): true UWP-on-Xbox-console head project
/// skeleton (src/ViceSharp.Xbox). This is a DIFFERENT project from the
/// ViceSharp.Host.Xbox desktop shell scaffold asserted by
/// <see cref="XboxHostShellTests"/>; that scaffold must stay untouched.
///
/// FR: FR-Host-UI-Boundary. TR: TR-Host-Status (cross-platform host parity).
/// Use case: ViceSharp must ship a UWP-on-Xbox-console head that reuses the
/// managed C64 core plus the in-process host facade
/// (ViceSharp.Host.InProcess) and takes no desktop-UI, gRPC, or web-host
/// dependency. A conditional target framework keeps the whole solution
/// buildable on agents WITHOUT the windows-app / UWP workload (mirrors the
/// Host.Android fallback precedent).
/// Acceptance:
///   TEST-XBOXTOPO-001a: csproj declares the conditional UWP/net10.0 target
///     framework, the ViceSharpXboxUwp switch, UseUwp, PublishAot, and the
///     five core ProjectReferences (Abstractions, Core, Chips, Architectures,
///     Host.InProcess).
///   TEST-XBOXTOPO-001b: csproj carries none of the banned direct references
///     (Host, desktop UI, Protocol, Monitor) nor any gRPC / web-host /
///     desktop-UI package.
///   TEST-XBOXTOPO-002: the head is registered in ViceSharp.slnx.
/// </summary>
public sealed class XboxUwpHeadTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void Csproj_HasConditionalTfm_AndCoreReferences()
    {
        var csproj = ReadCsproj();

        // TEST-XBOXTOPO-001a: conditional TFM + workload switch.
        Assert.Contains("net10.0-windows10.0.26100.0", csproj);
        Assert.Contains("net10.0", csproj);
        Assert.Contains("ViceSharpXboxUwp", csproj);

        // Core managed stack + the in-process host facade.
        Assert.Contains("ViceSharp.Abstractions.csproj", csproj);
        Assert.Contains("ViceSharp.Core.csproj", csproj);
        Assert.Contains("ViceSharp.Chips.csproj", csproj);
        Assert.Contains("ViceSharp.Architectures.csproj", csproj);
        Assert.Contains("ViceSharp.Host.InProcess.csproj", csproj);

        // UWP-only knobs that the workload path relies on.
        Assert.Contains("UseUwp", csproj);
        Assert.Contains("PublishAot", csproj);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Csproj_HasNoBannedReferences()
    {
        var csproj = ReadCsproj();

        // TEST-XBOXTOPO-001b: no direct references to the desktop host, the
        // desktop UI, the wire protocol, or the monitor. (Protocol/Monitor
        // arrive transitively through Host.InProcess, which is allowed.)
        Assert.DoesNotContain("ViceSharp.Host.csproj", csproj);
        Assert.DoesNotContain("ViceSharp.Avalonia.csproj", csproj);
        Assert.DoesNotContain("ViceSharp.Protocol.csproj", csproj);
        Assert.DoesNotContain("ViceSharp.Monitor.csproj", csproj);

        // No gRPC / web-host / desktop-UI package references.
        Assert.DoesNotContain("Grpc.", csproj);
        Assert.DoesNotContain("Microsoft.AspNetCore", csproj);
        Assert.DoesNotContain("Avalonia.", csproj);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Slnx_RegistersXboxHead()
    {
        var slnx = File.ReadAllText(Path.Combine(RepoRoot, "ViceSharp.slnx"));

        // TEST-XBOXTOPO-002: solution registers the UWP-on-Xbox-console head.
        Assert.Contains("src/ViceSharp.Xbox/ViceSharp.Xbox.csproj", slnx);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Csproj_FlipsUwpToTrue_UnderDebugUwpConfiguration()
    {
        var csproj = ReadCsproj();

        // PLAN-XBOXUWP: the VS "Debug-UWP" solution configuration must flip the
        // single ViceSharpXboxUwp chokepoint to true. A .slnx solution config
        // cannot inject an MSBuild property (vs-solutionpersistence#128), so the
        // head derives the boolean from the mapped $(Configuration) NAME.
        const string debugUwpDerivation =
            "<ViceSharpXboxUwp Condition=\"'$(ViceSharpXboxUwp)'=='' and '$(Configuration)'=='Debug-UWP'\">true</ViceSharpXboxUwp>";
        const string fallbackDefault =
            "<ViceSharpXboxUwp Condition=\"'$(ViceSharpXboxUwp)'==''\">false</ViceSharpXboxUwp>";

        Assert.Contains(debugUwpDerivation, csproj);

        // The workload-free net10.0 fallback default is preserved for
        // Debug / Release / Any CPU / the CI path.
        Assert.Contains(fallbackDefault, csproj);

        // MSBuild evaluates properties top-to-bottom: the Debug-UWP derivation
        // MUST precede the empty->false default, otherwise the property is
        // already false by the time the derivation would fire.
        Assert.True(
            csproj.IndexOf(debugUwpDerivation, StringComparison.Ordinal)
                < csproj.IndexOf(fallbackDefault, StringComparison.Ordinal),
            "The Debug-UWP derivation must appear before the empty->false default.");

        // The VS Configuration Manager validates the .slnx Debug-UWP|x64 mapping against the
        // project's DECLARED configurations/platforms; SDK defaults expose only Debug/Release
        // + AnyCPU, so the head must declare Debug-UWP + x64 or the IDE rejects the solution
        // mapping and keeps loading the net10.0 fallback (CLI MSBuild does not need this).
        Assert.Contains("<Configurations>Debug;Release;Debug-UWP</Configurations>", csproj);
        Assert.Contains("<Platforms>AnyCPU;x64</Platforms>", csproj);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Slnx_DeclaresDebugUwpConfiguration()
    {
        var slnx = File.ReadAllText(Path.Combine(RepoRoot, "ViceSharp.slnx"));

        // PLAN-XBOXUWP: a solution-level Debug-UWP BuildType + x64 Platform so the
        // operator picks "Debug-UWP | x64" in VS and F5 launches the real UWP head.
        Assert.Contains("<Configurations>", slnx);
        Assert.Contains("<BuildType Name=\"Debug-UWP\" />", slnx);

        // Re-listing the inferred configs is mandatory: an explicit <Configurations>
        // block OVERRIDES the auto-inferred Debug/Release/Any CPU, so they must be
        // restated or the CI Debug|Any CPU config disappears.
        Assert.Contains("<BuildType Name=\"Debug\" />", slnx);
        Assert.Contains("<BuildType Name=\"Release\" />", slnx);
        Assert.Contains("<Platform Name=\"Any CPU\" />", slnx);
        Assert.Contains("<Platform Name=\"x64\" />", slnx);

        // Xbox is x64-only (Native AOT has no AnyCPU image): the head is disabled
        // for Debug-UWP|Any CPU so a solution Platform=Any CPU cannot override the
        // project's <Platform>x64</Platform> and mislabel the win-x64 AOT/MSIX output.
        Assert.Contains("<Build Solution=\"Debug-UWP|Any CPU\" Project=\"false\" />", slnx);

        // A modern-.NET UWP app must run with MSIX package identity; without a <Deploy> rule
        // (Deploy defaults false) VS F5 launches the bare unpackaged exe, which fail-fasts
        // (0xC0000409) in the XAML/CoreApplication bootstrap before any window shows. (VS writes
        // this element without an explicit Project attribute, so match the prefix.)
        Assert.Contains("<Deploy Solution=\"Debug-UWP|x64\"", slnx);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void LaunchSettings_SelectsMsixPackageActivation()
    {
        var path = Path.Combine(
            RepoRoot, "src", "ViceSharp.Xbox", "Properties", "launchSettings.json");

        Assert.True(File.Exists(path), $"Expected the packaged-launch profile at '{path}'.");

        // commandName "MsixPackage" is what makes VS deploy + activate the REGISTERED package
        // WITH identity, instead of running the raw output exe (the default "Project" launch
        // that fail-fasts 0xC0000409 for lack of package identity).
        Assert.Contains("\"commandName\": \"MsixPackage\"", File.ReadAllText(path));
    }

    private static string ReadCsproj()
    {
        var csprojPath = Path.Combine(
            RepoRoot,
            "src",
            "ViceSharp.Xbox",
            "ViceSharp.Xbox.csproj");

        Assert.True(
            File.Exists(csprojPath),
            $"Expected Xbox UWP head csproj at '{csprojPath}'.");

        return File.ReadAllText(csprojPath);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
