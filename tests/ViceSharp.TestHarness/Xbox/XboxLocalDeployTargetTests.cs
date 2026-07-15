namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XLOCALDEPLOY-001 (PLAN-XBOXUWP, area XBOXPKG). Operator 2026-07-14: "add nuke
/// target to build and deploy appx locally." The DeployXboxLocal target packages the
/// session-proven local loop: stop a running instance, build the UWP head with the
/// vswhere-located VS MSBuild (Restore heals the fallback-TFM assets flip), refresh the
/// REGISTERED loose-file AppX layout in place (robocopy, manifest excluded), and
/// optionally relaunch via the shell activation URI.
/// </summary>
/// <remarks>
/// Acceptance (structural pins; the executable receipt is running the target):
///   TEST-XDEPLOY-001a: the target exists with the deploy-configuration and launch
///     parameters, builds via vswhere-located MSBuild with Restore, and refreshes the
///     layout with the proven robocopy shape (manifest excluded).
///   TEST-XDEPLOY-001b: the loop is safe: a running instance is stopped before the
///     copy (locked files), the registered location comes from Get-AppxPackage, and
///     relaunch uses shell:AppsFolder activation.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxLocalDeployTargetTests
{
    [Fact]
    public void DeployXboxLocal_BuildsAndRefreshesTheRegisteredLayout()
    {
        var build = File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));

        // TEST-XDEPLOY-001a.
        Assert.Contains("Target DeployXboxLocal", build);
        Assert.Contains("XboxDeployConfiguration", build);
        Assert.Contains("XboxLaunch", build);

        // GenerateProjectPriFile is mandatory: incremental Build leaves resources.pri
        // stale and UWP loads page XAML from the pri, so a XAML-only change would
        // deploy the PREVIOUS UI (operator-hit 2026-07-14).
        Assert.Contains("/t:Restore,Build,GenerateProjectPriFile", build);
        Assert.Contains("robocopy", build);
        Assert.Contains("/XF AppxManifest.xml", build);

        // TEST-XDEPLOY-001b.
        Assert.Contains("Get-AppxPackage", build);
        Assert.Contains("Stop-Process", build);
        Assert.Contains("shell:AppsFolder", build);
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
