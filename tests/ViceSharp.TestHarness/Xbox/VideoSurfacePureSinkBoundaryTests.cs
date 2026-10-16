namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S23 (IMPL-XBOXUWP-023), area XVIDEO. FR-XVIDEO-002 / TR-MVVM-001.
/// A raw-source forbidden-identifier scan proving the video frame-pull adapter is a
/// PURE SINK: its source may name none of the core-advancing / input-mutating members
/// (<c>RunFrame</c>, <c>StepInstruction</c>, <c>Reset(</c>, <c>SetJoystickState</c>,
/// <c>SetKeyState</c>) nor the broad machine handle <c>IMachine</c>. Because the
/// adapter's only dependency is <see cref="ViceSharp.Xbox.ViewModels.ILocalVideoFramePull"/>
/// (which exposes only frame-copy and geometry members), it structurally cannot
/// advance or perturb the emulator core - this scan enforces that at the source level.
///
/// <para>
/// Modeled on <c>AvaloniaBoundaryTests.AvaloniaSources_DoNotReferenceRuntimeInternals</c>
/// (concatenated raw-source forbidden-identifier assertion): the guard needs no UWP
/// workload and no linker, so it runs under plain <c>dotnet test</c> on any agent.
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class VideoSurfacePureSinkBoundaryTests
{
    /// <summary>
    /// Core-advancing / input-mutating identifiers that MUST NOT appear anywhere in the
    /// pure video-sink source. <c>IMachine</c> is checked separately as a whole word so
    /// the narrow input contracts <c>IMachineJoystickInput</c>/<c>IMachineKeyboardInput</c>
    /// (which legitimately exist elsewhere in the ViewModels) are not false positives.
    /// </summary>
    private static readonly string[] ForbiddenIdentifiers =
    {
        "RunFrame",
        "StepInstruction",
        "Reset(",
        "SetJoystickState",
        "SetKeyState",
    };

    /// <summary>The ViewModels source files that make up the pure video-pull sink.</summary>
    private static readonly string[] PureSinkSourceFiles =
    {
        "VideoFramePullViewModel.cs",
        "FrameGeometry.cs",
    };

    /// <summary>
    /// FR-XVIDEO-002, TR-MVVM-001. TEST-XBOXUI-005b.
    /// Use case: even if the adapter compiles against only the pull seam, a textual
    /// reference to a core-advancing member would signal that the render pull could
    /// perturb determinism or stall the worker.
    /// Acceptance: the concatenated source of the pure-sink files
    /// (<c>VideoFramePullViewModel.cs</c>, <c>FrameGeometry.cs</c>) contains none of the
    /// forbidden core-advancing identifiers and no whole-word <c>IMachine</c>.
    /// </summary>
    [Fact]
    public void PureSinkAdapterSource_ContainsNoCoreAdvancingIdentifiers()
    {
        var sourceRoot = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox.ViewModels");

        foreach (var fileName in PureSinkSourceFiles)
        {
            var fullPath = Path.Combine(sourceRoot, fileName);
            Assert.True(File.Exists(fullPath), $"Expected the pure-sink source at '{fullPath}'.");

            var source = File.ReadAllText(fullPath);

            foreach (var forbidden in ForbiddenIdentifiers)
                Assert.DoesNotContain(forbidden, source);

            Assert.DoesNotMatch(@"\bIMachine\b", source);
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
