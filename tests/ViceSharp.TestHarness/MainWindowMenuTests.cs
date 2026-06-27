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

    /// <summary>
    /// FR-CAPVID-001 / TR-UIHOTKEY-001 / TEST-UIHOTKEY-001.
    /// Use case: the default muxed video recording toggle must be reachable
    /// without opening the Snapshot menu.
    /// Acceptance: the MP4 + sound menu item exposes Ctrl+Shift+R and still
    /// routes to the existing video-record toggle handler.
    /// </summary>
    [Fact]
    public void SnapshotMenu_RecordVideoMp4_DeclaresToggleHotkey()
    {
        var axaml = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "MainWindow.axaml"));

        Assert.Contains("Header=\"MP4 + sound...\" HotKey=\"Ctrl+Shift+R\" Click=\"OnMenuRecordVideoMp4\"", axaml);
    }

    /// <summary>
    /// FR-PICKER-PAUSE-001 / TR-PICKER-PAUSE-001 / TEST-PICKER-PAUSE-001.
    /// Use case: OS file/folder pickers can block the UI while the emulator
    /// continues running behind the modal surface; every picker path must pause
    /// first through one shared code-behind boundary.
    /// Acceptance: all six picker call sites use pause-aware helper methods, and
    /// only the helpers call the StorageProvider picker APIs directly.
    /// </summary>
    [Fact]
    public void MainWindow_PickerCalls_RouteThroughPauseAwareHelpers()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "MainWindow.axaml.cs"));

        Assert.Equal(3, Count(code, "await ShowSaveFilePickerAsync("));
        Assert.Equal(2, Count(code, "await ShowOpenFilePickerAsync("));
        Assert.Equal(1, Count(code, "await ShowOpenFolderPickerAsync("));
        Assert.Equal(1, Count(code, ".StorageProvider.SaveFilePickerAsync("));
        Assert.Equal(1, Count(code, ".StorageProvider.OpenFilePickerAsync("));
        Assert.Equal(1, Count(code, ".StorageProvider.OpenFolderPickerAsync("));
        Assert.Contains("PauseBeforeSystemPickerAsync", code);
        Assert.Contains("var response = await _shell.PauseAsync()", code);
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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
