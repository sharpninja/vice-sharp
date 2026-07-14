// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): virtual-keyboard overlay code-behind. #if HAS_UWP.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The on-screen virtual keyboard dock. Pressing a tile routes through the bound
/// <see cref="VirtualKeyboardViewModel"/>, which injects the keystroke (or RESTORE /
/// SHIFT-LOCK) on the machine keyboard seam. FEAT-XKEYCAPSHIFT-001: while SHIFT is
/// effective (trigger hold, SHIFT-LOCK latch, or a momentary one-shot arm) each keycap
/// swaps to its printable shifted legend (<see cref="VirtualKeyEntry.ShiftedLabel"/>)
/// and back when shift clears.
/// </summary>
public sealed partial class VirtualKeyboardOverlay : UserControl
{
    private bool _externalShift;
    private bool _appliedShift;

    /// <summary>Creates the overlay.</summary>
    public VirtualKeyboardOverlay() => InitializeComponent();

    /// <summary>
    /// Sets the EXTERNAL shift state (the head's RT trigger-modifier hold,
    /// FEAT-XKEYCAPSHIFT-001) and refreshes the keycaps. The effective shift visual is
    /// this flag OR the ViewModel's SHIFT-LOCK latch OR its momentary one-shot arm.
    /// </summary>
    /// <param name="held"><c>true</c> while the SHIFT trigger modifier is held.</param>
    public void SetExternalShift(bool held)
    {
        if (_externalShift == held)
            return;

        _externalShift = held;
        RefreshKeycaps();
    }

    /// <summary>
    /// Re-evaluates the effective shift state and swaps every realized keycap between
    /// its base and shifted legend. Called on external-shift edges and after every tile
    /// press (the press may toggle the latch, arm/consume the one-shot, or leave both).
    /// </summary>
    public void RefreshKeycaps()
    {
        var vm = DataContext as VirtualKeyboardViewModel;
        var shifted = _externalShift || vm is { ShiftLatched: true } || vm is { ShiftArmed: true };
        if (shifted == _appliedShift)
            return;

        _appliedShift = shifted;
        ApplyKeycaps(this, shifted);
    }

    private static void ApplyKeycaps(DependencyObject root, bool shifted)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button { DataContext: VirtualKeyEntry entry } button)
            {
                button.Content = shifted && entry.ShiftedLabel is not null
                    ? entry.ShiftedLabel
                    : entry.DisplayLabel;
            }

            ApplyKeycaps(child, shifted);
        }
    }

    private void OnKeyPressed(object sender, RoutedEventArgs e)
    {
        if (DataContext is VirtualKeyboardViewModel viewModel &&
            sender is FrameworkElement { DataContext: VirtualKeyEntry entry })
        {
            viewModel.Press(entry);

            // The press may have toggled SHIFT-LOCK, armed a momentary shift, or consumed
            // a one-shot: re-sync the keycaps to the new effective state.
            RefreshKeycaps();
        }
    }
}
#endif
