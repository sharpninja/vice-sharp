using Avalonia.Controls;
using Avalonia.Input;
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

    private void OnSearch(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm)
            return;

        vm.SearchMemory();
        if (vm.MatchLineIndex >= 0 && this.FindControl<ListBox>("MemoryDumpList") is { } list)
            list.ScrollIntoView(vm.MatchLineIndex);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnSearch(sender, e);
            e.Handled = true;
        }
    }

    private void OnNavFirst(object? sender, RoutedEventArgs e) => _ = ViewModel?.NavigateFirstAsync();

    private void OnNavPrevious(object? sender, RoutedEventArgs e) => _ = ViewModel?.NavigatePreviousAsync();

    private void OnNavNext(object? sender, RoutedEventArgs e) => _ = ViewModel?.NavigateNextAsync();

    private void OnNavLast(object? sender, RoutedEventArgs e) => _ = ViewModel?.NavigateLastAsync();
}
