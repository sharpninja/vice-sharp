namespace ViceSharp.TestHarness.Ui;

using Xunit;

/// <summary>
/// Structural proof that Avalonia and Xbox settings surfaces bind VIC-20 RAM,
/// expansion cart, and uIEC path controls.
/// </summary>
public sealed class Vic20SettingsUiSurfaceTests
{
    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
        }
    }

    [Fact]
    public void Avalonia_SettingsView_ContainsVic20MemoryAndUiecBindings()
    {
        var path = Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "Views", "SettingsView.axaml");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.Contains("IsVic20Selected", text, StringComparison.Ordinal);
        Assert.Contains("SelectedVic20MemoryPreset", text, StringComparison.Ordinal);
        Assert.Contains("Vic20Blk0", text, StringComparison.Ordinal);
        Assert.Contains("Vic20Blk5", text, StringComparison.Ordinal);
        Assert.Contains("FileSystemIecRootPath", text, StringComparison.Ordinal);
        Assert.Contains("SelectedExpansionCartKind", text, StringComparison.Ordinal);
        Assert.Contains("ExpansionCartWriteBack", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Xbox_SettingsPage_ContainsVic20MemoryAndUiecBindings()
    {
        var path = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "Views", "SettingsPage.xaml");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.Contains("IsVic20Selected", text, StringComparison.Ordinal);
        Assert.Contains("SelectedVic20MemoryPreset", text, StringComparison.Ordinal);
        Assert.Contains("Vic20Blk0", text, StringComparison.Ordinal);
        Assert.Contains("FileSystemIecRootPath", text, StringComparison.Ordinal);
        Assert.Contains("SelectedExpansionCartKind", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AttachPanelViewModel_ExposesVic20MemoryProperties()
    {
        var type = typeof(ViceSharp.Avalonia.ViewModels.AttachPanelViewModel);
        Assert.NotNull(type.GetProperty("IsVic20Selected"));
        Assert.NotNull(type.GetProperty("SelectedVic20MemoryPreset"));
        Assert.NotNull(type.GetProperty("Vic20Blk0"));
        Assert.NotNull(type.GetProperty("FileSystemIecRootPath"));
        Assert.NotNull(type.GetProperty("SelectedExpansionCartKind"));
    }

    [Fact]
    public void XboxSettingsViewModel_ExposesVic20MemoryProperties()
    {
        var type = typeof(ViceSharp.Xbox.ViewModels.XboxSettingsViewModel);
        Assert.NotNull(type.GetProperty("IsVic20Selected"));
        Assert.NotNull(type.GetProperty("SelectedVic20MemoryPreset"));
        Assert.NotNull(type.GetProperty("Vic20Blk5"));
        Assert.NotNull(type.GetProperty("FileSystemIecRootPath"));
        Assert.NotNull(type.GetProperty("SelectedExpansionCartPreset"));
    }
}
