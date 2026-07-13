namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S33 (IMPL-XBOXUWP-033), area XBOXCI. The Nuke build must
/// expose the three Xbox-head targets so the console head has a documented,
/// scripted build/deploy/validate path without pulling the manual DEVICE targets
/// into CI:
/// <list type="bullet">
///   <item><description><c>PublishXbox</c> - DEV-PC/MANUAL Native-AOT MSIX publish
///     of the <c>ViceSharp.Xbox</c> head (needs the windows-app / UWP workload).</description></item>
///   <item><description><c>DeployXbox</c> - DEV-PC/MANUAL <c>WinAppDeployCmd</c>
///     sideload of the packaged MSIX to a Dev-Mode console.</description></item>
///   <item><description><c>ValidateXbox</c> - OFF-CONSOLE, workload-free gate that
///     runs on a plain net10.0 agent (solution build + the <c>Category=Xbox</c>
///     tests + the trim/AOT link of the workload-free fallback head).</description></item>
/// </list>
///
/// <para>
/// This is a Tier H gate: it is a raw-text audit of <c>build/Build.cs</c> and the
/// on-console runbook, so it runs under <c>dotnet test</c> on any agent with no UWP
/// workload, no Nuke bootstrap, and no MSIX tooling.
/// </para>
///
/// <para>
/// FR: FR-TESTGATE (delivery-process / CI coexistence). TR: TR-XBOXCI-005.
/// TEST-XBOXCI-001.
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxNukeConfigTests
{
    /// <summary>
    /// TR-XBOXCI-005, TEST-XBOXCI-001.
    /// Use case: the Nuke build must declare the manual Xbox DEVICE publish target
    /// so the operator has a scripted Native-AOT MSIX build path.
    /// Acceptance: <c>build/Build.cs</c> declares a <c>Target PublishXbox</c>.
    /// </summary>
    [Fact]
    public void BuildCs_DeclaresPublishXboxTarget()
    {
        var build = ReadBuildCs();
        Assert.Contains("Target PublishXbox", build, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-XBOXCI-005, TEST-XBOXCI-001.
    /// Use case: the Nuke build must declare the manual Xbox DEVICE sideload target
    /// so the operator has a scripted <c>WinAppDeployCmd</c> deploy path.
    /// Acceptance: <c>build/Build.cs</c> declares a <c>Target DeployXbox</c> whose
    /// body invokes <c>WinAppDeployCmd</c>.
    /// </summary>
    [Fact]
    public void BuildCs_DeclaresDeployXboxTarget_UsingWinAppDeployCmd()
    {
        var build = ReadBuildCs();
        Assert.Contains("Target DeployXbox", build, StringComparison.Ordinal);

        var deployBlock = ExtractTargetBlock(build, "DeployXbox");
        Assert.Contains("WinAppDeployCmd", deployBlock, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TR-XBOXCI-005, TEST-XBOXCI-001.
    /// Use case: the Nuke build must declare the OFF-CONSOLE validation target that
    /// runs on a plain net10.0 CI agent (no UWP workload): it builds the solution,
    /// runs the <c>Category=Xbox</c> tests, and links the workload-free head under
    /// trim/AOT.
    /// Acceptance: <c>build/Build.cs</c> declares a <c>Target ValidateXbox</c> whose
    /// body references the <c>Category=Xbox</c> test filter and the AOT publish
    /// (<c>PublishAot</c> over the <c>ViceSharpXboxUwp</c>=false fallback).
    /// </summary>
    [Fact]
    public void BuildCs_DeclaresValidateXboxTarget_WithXboxFilterAndAotPublish()
    {
        var build = ReadBuildCs();
        Assert.Contains("Target ValidateXbox", build, StringComparison.Ordinal);

        var validateBlock = ExtractTargetBlock(build, "ValidateXbox");

        // The off-console gate runs the Category=Xbox tests ...
        Assert.Contains("Category=Xbox", validateBlock, StringComparison.Ordinal);
        // ... and links the workload-free fallback head under Native AOT.
        Assert.Contains("PublishAot", validateBlock, StringComparison.Ordinal);
        Assert.Contains("ViceSharpXboxUwp", validateBlock, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-XBOXCI-005, TEST-XBOXCI-001.
    /// Use case: a bad sideload must be recoverable, so S33 requires the on-console
    /// runbook to document the rollback/uninstall path (R8).
    /// Acceptance: <c>docs/xbox/on-console-setup-runbook.md</c> contains a rollback /
    /// uninstall section that names <c>WinAppDeployCmd uninstall</c> and the redeploy
    /// of the prior known-good MSIX.
    /// </summary>
    [Fact]
    public void OnConsoleRunbook_DocumentsRollbackUninstallAndRedeploy()
    {
        var runbookPath = Path.Combine(RepoRoot, "docs", "xbox", "on-console-setup-runbook.md");
        Assert.True(File.Exists(runbookPath), $"Expected the on-console runbook at '{runbookPath}'.");

        var runbook = File.ReadAllText(runbookPath);

        // A rollback / uninstall section heading.
        Assert.Matches(new Regex(@"(?im)^\s*#+.*\b(rollback|uninstall)\b"), runbook);

        // The uninstall command and the redeploy of the prior MSIX.
        Assert.Contains("WinAppDeployCmd uninstall", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redeploy", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".msix", runbook, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads <c>build/Build.cs</c> (asserts it exists first).</summary>
    private static string ReadBuildCs()
    {
        var path = Path.Combine(RepoRoot, "build", "Build.cs");
        Assert.True(File.Exists(path), $"Expected the Nuke build script at '{path}'.");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Returns the source text of a single Nuke target: from its
    /// <c>Target {name}</c> declaration up to the next 4-space-indented
    /// <c>Target </c> declaration (or the end of the file when it is the last target).
    /// </summary>
    private static string ExtractTargetBlock(string source, string targetName)
    {
        var marker = "Target " + targetName;
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Target '{targetName}' not found in build/Build.cs.");

        // The next target declaration is indented with four spaces at line start.
        var next = source.IndexOf("\n    Target ", start + marker.Length, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
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
