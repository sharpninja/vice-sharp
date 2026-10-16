using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ViceSharp.Avalonia.ViewModels;
// FlashCartBuilderView in same namespace

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-UISETTINGS-001: the settings panel as a reusable AXAML UserControl bound
/// to <see cref="AttachPanelViewModel"/>. Action buttons route to the view-model
/// (the host-backed apply/validate/revert pipeline).
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private AttachPanelViewModel? ViewModel => DataContext as AttachPanelViewModel;

    private void OnValidate(object? sender, RoutedEventArgs e)
        => _ = ViewModel?.ValidateSettingsAsync();

    private void OnApply(object? sender, RoutedEventArgs e)
        => _ = ViewModel?.ApplySettingsAsync(restartRequired: false);

    private void OnRevert(object? sender, RoutedEventArgs e)
        => ViewModel?.RevertSettings();

    private void OnApplyRestart(object? sender, RoutedEventArgs e)
        => _ = ViewModel?.ApplySettingsAsync(restartRequired: true);

    // Warp is a live control like Alt+W: the ToggleButton two-way binding has
    // already flipped IsWarpMode when Click fires; push it to the host now.
    private void OnWarpToggled(object? sender, RoutedEventArgs e)
        => _ = ViewModel?.ApplySettingsAsync(restartRequired: false);

    private void OnCycleSpeed(object? sender, RoutedEventArgs e)
        => _ = ViewModel?.CycleSpeedAsync();

    private async void OnOpenFlashCartBuilder(object? sender, RoutedEventArgs e)
    {
        var builder = new FlashCartBuilderView();
        var window = new Window
        {
            Title = "Flash cart image builder",
            Width = 560,
            Height = 520,
            Content = builder,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        builder.CloseRequested += (_, _) => window.Close();
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is not null)
            await window.ShowDialog(owner);
        else
            window.Show();
    }
}
