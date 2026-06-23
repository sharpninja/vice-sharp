using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// FR-IECMON-001 / FR-IECSPY-001: the real-time IEC bus monitor as a reusable AXAML UserControl
/// bound to <see cref="ViewModels.IecMonitorViewModel"/>. Shows each IEC line's live level and
/// pullers plus the activity summary; collapses when the session has no IEC bus.
/// </summary>
public partial class IecMonitorView : UserControl
{
    public IecMonitorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
