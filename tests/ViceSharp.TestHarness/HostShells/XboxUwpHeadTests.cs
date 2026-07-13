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
