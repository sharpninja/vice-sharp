// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): virtual-keyboard overlay code-behind. #if HAS_UWP.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The on-screen virtual keyboard overlay. Pressing a tile routes through the bound
/// <see cref="VirtualKeyboardViewModel"/>, which injects the keystroke (or RESTORE / SHIFT-LOCK)
/// on the machine keyboard seam.
/// </summary>
public sealed partial class VirtualKeyboardOverlay : UserControl
{
    /// <summary>Creates the overlay.</summary>
    public VirtualKeyboardOverlay() => InitializeComponent();

    private void OnKeyPressed(object sender, RoutedEventArgs e)
    {
        if (DataContext is VirtualKeyboardViewModel viewModel &&
            sender is FrameworkElement { DataContext: VirtualKeyEntry entry })
        {
            viewModel.Press(entry);
        }
    }
}
#endif
