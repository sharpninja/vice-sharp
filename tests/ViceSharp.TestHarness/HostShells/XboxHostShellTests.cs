namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using Xunit;

/// <summary>
/// PLATFORM-CROSS-001 Phase 1: UWP Xbox host shell scaffold.
///
/// FR: FR-Host-UI-Boundary, TR: TR-Host-Status (cross-platform host parity).
/// Use case: ViceSharp must be able to ship a UWP-style Xbox host shell that
/// reuses the existing Avalonia host plus the in-process gRPC host
/// composition. The shell project itself must not duplicate UI or runtime
/// logic; it only wires the existing App entry point for the Xbox target.
/// Acceptance:
///   - src/ViceSharp.Host.Xbox/ViceSharp.Host.Xbox.csproj exists.
///   - Its &lt;TargetFramework&gt; (or &lt;TargetFrameworks&gt;) contains
///     "net10.0" (the workload-available fallback when the
///     net10.0-windows10.0.x UWP workload is missing).
///   - It carries ProjectReferences to both ViceSharp.Avalonia and
///     ViceSharp.Host so the Xbox shell inherits the host boundary.
/// </summary>
public sealed class XboxHostShellTests
{
    [Fact]
    public void XboxHostShellCsproj_TargetsNet10AndReferencesAvaloniaAndHost()
    {
        var csprojPath = Path.Combine(
            RepoRoot,
            "src",
            "ViceSharp.Host.Xbox",
            "ViceSharp.Host.Xbox.csproj");

        Assert.True(
            File.Exists(csprojPath),
            $"Expected Xbox host shell csproj at '{csprojPath}'.");

        var project = File.ReadAllText(csprojPath);

        Assert.Contains("net10.0", project);
        Assert.Contains("ViceSharp.Avalonia.csproj", project);
        Assert.Contains("ViceSharp.Host.csproj", project);
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
