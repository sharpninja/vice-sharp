using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ViceSharp.Avalonia.ViewModels;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-TICKHIST-001: the "last 100 ticks" history panel as a reusable AXAML UserControl
/// bound to <see cref="TickHistoryViewModel"/>. Refresh reloads the tick list; selecting a
/// tick while paused opens the debug screen (registers + reconstructed memory dump).
/// </summary>
public partial class TickHistoryView : UserControl
{
    public TickHistoryView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private TickHistoryViewModel? ViewModel => DataContext as TickHistoryViewModel;

    private void OnRefresh(object? sender, RoutedEventArgs e)
        => _ = ViewModel?.RefreshAsync();

    private void OnBack(object? sender, RoutedEventArgs e)
        => ViewModel?.CloseDebug();

    private void OnTickSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is { } vm && e.AddedItems.Count > 0 && e.AddedItems[0] is TickRowViewModel tick)
            _ = vm.InspectAsync(tick);
    }
}
