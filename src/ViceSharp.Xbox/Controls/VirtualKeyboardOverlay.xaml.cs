// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): virtual-keyboard overlay code-behind. #if HAS_UWP.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The on-screen virtual keyboard dock. Pressing a tile routes through the bound
/// <see cref="VirtualKeyboardViewModel"/>, which injects the keystroke (or RESTORE /
/// SHIFT-LOCK) on the machine keyboard seam. Keycap glyphs are LIVE
/// (<see cref="VirtualKeycapGlyphs"/>): FEAT-XKEYCAPSHIFT-001 swaps the printable
/// shifted legends while SHIFT is effective, and FEAT-XKEYCAPCASE-001 follows the
/// machine's charset case (uppercase/graphics vs lowercase/uppercase, polled from the
/// live VIC while the dock is visible) so letters read exactly as they will insert.
/// </summary>
public sealed partial class VirtualKeyboardOverlay : UserControl
{
    private readonly DispatcherQueueTimer _caseTimer;
    private readonly DispatcherQueueTimer _strokeTimer;

    private bool _externalShift;
    private bool _externalCommodore;
    private bool _appliedShift;
    private bool _appliedCommodore;
    private bool _appliedLowercase;

    /// <summary>The bound view-model, projected from the externally-set DataContext (for {x:Bind}).</summary>
    private VirtualKeyboardViewModel? ViewModel => DataContext as VirtualKeyboardViewModel;

    /// <summary>Creates the overlay, its charset-case poll, and the stroke-hold timer.</summary>
    public VirtualKeyboardOverlay()
    {
        InitializeComponent();

        // DataContext (the VirtualKeyboardViewModel) is assigned externally by App, so refresh the
        // compiled {x:Bind} bindings whenever it changes.
        DataContextChanged += (_, _) => Bindings.Update();

        // The charset mode flips at RUNTIME (SHIFT+C= / POKE 53272), so poll the live
        // VIC at ~4 Hz while the dock is on screen; the apply path no-ops when nothing
        // changed, so the steady-state cost is one facade read.
        _caseTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _caseTimer.Interval = TimeSpan.FromMilliseconds(250);
        _caseTimer.IsRepeating = true;
        _caseTimer.Tick += (_, _) => RefreshKeycaps();

        // FEAT-XKBDSTICKY-001 (operator: "The virtual keyboard should be scanned in
        // real time"): a clicked key stays DOWN in the machine matrix for a real scan
        // window (~5 frames) before CompletePress releases it (and any armed sticky
        // modifiers); the old instant down+up raced the KERNAL's ~60 Hz scan.
        _strokeTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _strokeTimer.Interval = TimeSpan.FromMilliseconds(80);
        _strokeTimer.IsRepeating = false;
        _strokeTimer.Tick += (_, _) =>
        {
            (DataContext as VirtualKeyboardViewModel)?.CompletePress();
            RefreshKeycaps();
        };

        Loaded += (_, _) => _caseTimer.Start();
        Unloaded += (_, _) => _caseTimer.Stop();
    }

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
    /// Sets the EXTERNAL C= state (the head's LT trigger-modifier hold,
    /// FEAT-XKEYCAPPETSCII-001) and refreshes the keycaps to the PETSCII left-keycap
    /// graphics while held. C= has no latch or one-shot; the trigger is its only source.
    /// </summary>
    /// <param name="held"><c>true</c> while the C= trigger modifier is held.</param>
    public void SetExternalCommodore(bool held)
    {
        if (_externalCommodore == held)
            return;

        _externalCommodore = held;
        RefreshKeycaps();
    }

    /// <summary>
    /// Re-evaluates the effective shift + charset-case state and swaps every realized
    /// keycap glyph. Called on external-shift edges, after every tile press (the press
    /// may toggle the latch or arm/consume the one-shot), and by the case poll.
    /// </summary>
    public void RefreshKeycaps()
    {
        var vm = DataContext as VirtualKeyboardViewModel;
        var shifted = _externalShift || vm is { ShiftLatched: true } || vm is { ShiftArmed: true };
        var commodore = _externalCommodore || vm is { CommodoreArmed: true };
        var lowercase = App.Instance.IsCharsetLowercase();

        if (shifted == _appliedShift && commodore == _appliedCommodore && lowercase == _appliedLowercase)
            return;

        _appliedShift = shifted;
        _appliedCommodore = commodore;
        _appliedLowercase = lowercase;
        ApplyKeycaps(this, shifted, commodore, lowercase);
    }

    private static void ApplyKeycaps(DependencyObject root, bool shifted, bool commodore, bool lowercase)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button { DataContext: VirtualKeyEntry entry } button)
            {
                button.Content = VirtualKeycapGlyphs.For(entry, shifted, commodore, lowercase);
            }

            ApplyKeycaps(child, shifted, commodore, lowercase);
        }
    }

    private void OnKeyPressed(object sender, RoutedEventArgs e)
    {
        if (DataContext is VirtualKeyboardViewModel viewModel &&
            sender is FrameworkElement { DataContext: VirtualKeyEntry entry })
        {
            viewModel.Press(entry);

            // An ordinary key is now HELD in the matrix; schedule the stroke completion
            // one real scan window out (restarting on fast typing is safe: Press already
            // finished the previous stroke).
            if (entry.Kind == AppKeyKind.Key)
            {
                _strokeTimer.Stop();
                _strokeTimer.Start();
            }

            // The press may have toggled SHIFT-LOCK or a sticky modifier: re-sync the
            // keycaps to the new effective state.
            RefreshKeycaps();
        }
    }
}
#endif
