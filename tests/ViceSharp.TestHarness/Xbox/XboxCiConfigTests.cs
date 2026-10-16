namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S33 (IMPL-XBOXUWP-033), area XBOXCI. The Xbox DEVICE targets
/// (<c>PublishXbox</c>, <c>DeployXbox</c>) are MANUAL / dev-PC only: they need the
/// windows-app / UWP workload and a physical Dev-Mode console, so they must never
/// be wired into a CI or release pipeline (per the plan failure-mode: "device
/// targets wired into CI -> XboxCiConfigTests catches"). Only the workload-free,
/// off-console <c>ValidateXbox</c> gate is safe for a plain net10.0 agent, and it is
/// optional to wire into CI.
///
/// <para>
/// This Tier H gate is a raw-text audit of the Nuke-generated Azure pipeline YAML,
/// so it runs under <c>dotnet test</c> on any agent.
/// </para>
///
/// <para>
/// FR: FR-TESTGATE (delivery-process / CI coexistence). TR: TR-XBOXCI-005.
/// TEST-XBOXCI-001.
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxCiConfigTests
{
    /// <summary>The CI / release pipeline YAML files that must not name the device targets.</summary>
    private static readonly string[] PipelineFiles =
    {
        "azure-pipelines.ci.yml",
        "azure-pipelines.release.yml",
    };

    /// <summary>The manual Xbox DEVICE targets that must stay out of every pipeline.</summary>
    private static readonly string[] DeviceTargets =
    {
        "PublishXbox",
        "DeployXbox",
    };

    /// <summary>
    /// TR-XBOXCI-005, TEST-XBOXCI-001.
    /// Use case: the manual, workload-requiring Xbox DEVICE targets must never be
    /// invoked by an unattended CI/release agent.
    /// Acceptance: neither Nuke-generated pipeline YAML (<c>azure-pipelines.ci.yml</c>,
    /// <c>azure-pipelines.release.yml</c>) names <c>PublishXbox</c> or <c>DeployXbox</c>.
    /// (The off-console <c>ValidateXbox</c> gate is allowed but not required.)
    /// </summary>
    [Fact]
    public void CiPipelines_DoNotNameTheXboxDeviceTargets()
    {
        foreach (var relative in PipelineFiles)
        {
            var path = Path.Combine(RepoRoot, relative);
            Assert.True(File.Exists(path), $"Expected the CI pipeline file at '{path}'.");

            var yaml = File.ReadAllText(path);
            foreach (var target in DeviceTargets)
            {
                Assert.DoesNotContain(
                    target,
                    yaml,
                    StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// FEAT-XOCTOPUS-001 (operator 2026-08-05: finish LEGION2 Octopus release steps).
    /// Use case: after a green CI/release job, optionally create and deploy an Octopus
    /// release on PAYTON-LEGION2 when <c>OCTOPUS_API_KEY</c> is present.
    /// Acceptance: both pipeline YAMLs declare an "Octopus LEGION2 release" step that
    /// no-ops without the key, invokes <c>octopus.exe</c> release create/deploy when
    /// set, and respects <c>SkipOctopus</c>.
    /// </summary>
    [Fact]
    public void CiPipelines_DeclareOptionalOctopusLegion2ReleaseStep()
    {
        foreach (var relative in PipelineFiles)
        {
            var path = Path.Combine(RepoRoot, relative);
            Assert.True(File.Exists(path), $"Expected the CI pipeline file at '{path}'.");

            var yaml = File.ReadAllText(path);
            Assert.Contains("Octopus LEGION2 release", yaml, StringComparison.Ordinal);
            Assert.Contains("OCTOPUS_API_KEY", yaml, StringComparison.Ordinal);
            Assert.Contains("octopus.exe", yaml, StringComparison.Ordinal);
            Assert.Contains("release create", yaml, StringComparison.Ordinal);
            Assert.Contains("release deploy", yaml, StringComparison.Ordinal);
            Assert.Contains("SkipOctopus", yaml, StringComparison.Ordinal);
            Assert.Contains("PAYTON-LEGION2:8066", yaml, StringComparison.Ordinal);
        }
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
