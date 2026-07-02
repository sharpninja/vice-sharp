namespace ViceSharp.TestHarness;

using ViceSharp.Avalonia;
using Xunit;

/// <summary>
/// FR: FR-UI-001, TR: TR-MVVM-001.
/// Use case: A screen recording of the desktop app must identify which deployed
/// build produced it, so the window title shows the running build's semantic
/// version. GitVersion / SourceLink stamp the assembly informational version with
/// a trailing "+&lt;buildmetadata&gt;" that must not leak into the title.
/// Acceptance: <see cref="MainWindow.FormatSemVer"/> reduces a raw informational
/// version to its semantic-version core - build metadata (after '+') dropped, any
/// pre-release tag retained, and blank input falling back to "0.0.0".
/// </summary>
public sealed class AppTitleVersionTests
{
    [Theory]
    [InlineData("0.1.419+7a55b6a6dd1e1aa647215e98b76df46626883e00", "0.1.419")]
    [InlineData("1.0.0+sha.abc123", "1.0.0")]
    [InlineData("0.1.419", "0.1.419")]
    [InlineData("2.3.0-beta.4+Branch.main.Sha.deadbeef", "2.3.0-beta.4")]
    [InlineData("2.3.0-rc.1", "2.3.0-rc.1")]
    public void FormatSemVer_DropsBuildMetadata_KeepsSemverCore(string raw, string expected)
    {
        Assert.Equal(expected, MainWindow.FormatSemVer(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FormatSemVer_BlankInput_FallsBackToZeroVersion(string? raw)
    {
        Assert.Equal("0.0.0", MainWindow.FormatSemVer(raw!));
    }
}
