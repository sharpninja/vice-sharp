namespace ViceSharp.TestHarness;

using Xunit;

public sealed class MainWindowMenuTests
{
    /// <summary>
    /// FR-HOST-DIAG-001 / TR-HOST-DIAG-002 / TR-HOST-DIAG-003 / TEST-UI-DIAG-001.
    /// Use case: a user or agent can copy deterministic attach details from
    /// the visible app without inspecting files manually.
    /// Acceptance: the Debug menu declares Copy Debug Attach Info and wires it
    /// to an explicit click handler.
    /// </summary>
    [Fact]
    public void DebugMenu_ContainsCopyDebugAttachInfoItem()
    {
        var axaml = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "MainWindow.axaml"));

        Assert.Contains("Copy Debug Attach Info", axaml);
        Assert.Contains("OnMenuCopyDebugAttachInfo", axaml);
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
