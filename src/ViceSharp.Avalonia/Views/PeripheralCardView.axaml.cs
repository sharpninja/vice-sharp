using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ViceSharp.Avalonia.ViewModels;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-UIPERIPHERAL-001: reusable per-peripheral card. Its DataContext is an
/// <see cref="AttachSlotViewModel"/>; Attach/Eject/Recent actions are routed to
/// the owning <see cref="AttachPanelViewModel"/> supplied via <see cref="Panel"/>.
/// </summary>
public partial class PeripheralCardView : UserControl
{
    /// <summary>The owning panel view-model that performs attach/eject/host work.</summary>
    public static readonly StyledProperty<AttachPanelViewModel?> PanelProperty =
        AvaloniaProperty.Register<PeripheralCardView, AttachPanelViewModel?>(nameof(Panel));

    public PeripheralCardView()
    {
        InitializeComponent();
    }

    /// <summary>The owning panel view-model (routes attach/eject through the host).</summary>
    public AttachPanelViewModel? Panel
    {
        get => GetValue(PanelProperty);
        set => SetValue(PanelProperty, value);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnAttach(object? sender, RoutedEventArgs e)
    {
        if (Panel is { } panel && DataContext is AttachSlotViewModel slot)
            _ = panel.AttachFromPickerAsync(slot);
    }

    private void OnEject(object? sender, RoutedEventArgs e)
    {
        if (Panel is { } panel && DataContext is AttachSlotViewModel slot)
            _ = panel.EjectAsync(slot);
    }

    private void OnRecentSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: string filePath } combo
            && Panel is { } panel
            && DataContext is AttachSlotViewModel slot)
        {
            combo.SelectedItem = null;
            _ = panel.AttachAsync(slot, filePath);
        }
    }
}
