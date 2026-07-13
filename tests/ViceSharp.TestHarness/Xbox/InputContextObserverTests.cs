namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Xbox.Input;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S20 (IMPL-XBOXUWP-020). TEST-XBOXUI-003: the thin
/// <see cref="InputContextObserver"/> that DERIVES the effective UI input context
/// from <see cref="NavigationViewModel"/> state and REQUESTS it on the single
/// authority <see cref="XboxInputContext"/> (R5 / FR-XBOXUI-003).
/// </summary>
/// <remarks>
/// <para>
/// The observer is NOT a second state machine: its
/// <see cref="InputContextObserver.EffectiveContext"/> reflects
/// <see cref="XboxInputContext.Context"/> directly. On every navigation/overlay
/// change it computes the desired context (Gameplay iff the page stack is empty
/// AND no overlay is open; the virtual-keyboard overlay maps to
/// <see cref="InputContext.VirtualKeyboard"/>; anything else maps to a
/// non-Gameplay UI context) and calls
/// <see cref="XboxInputContext.RequestContext(InputContext)"/> so exactly one
/// context is authoritative at a time.
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class InputContextObserverTests
{
    /// <summary>
    /// FR-XBOXUI-003, TR-XBOXUI-003 (IMPL-XBOXUWP-020), TEST-XBOXUI-003 quick-menu
    /// context guard.
    /// Use case: UI-navigation and gameplay input must never be live at once;
    /// opening the quick menu over gameplay switches the single input authority off
    /// Gameplay, and closing it with nothing else open returns to Gameplay.
    /// Acceptance: with an empty page stack, setting IsQuickMenuOpen true drives
    /// both the observer's EffectiveContext and XboxInputContext.Context to a
    /// non-Gameplay context; setting it false returns both to Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void QuickMenu_TogglesContext_OffAndOnGameplay()
    {
        var machine = new XboxInputContext();
        var nav = new NavigationViewModel();
        using var observer = new InputContextObserver(nav, machine);

        Assert.Equal(InputContext.Gameplay, observer.EffectiveContext);
        Assert.Equal(InputContext.Gameplay, machine.Context);

        nav.IsQuickMenuOpen = true;
        Assert.NotEqual(InputContext.Gameplay, observer.EffectiveContext);
        Assert.NotEqual(InputContext.Gameplay, machine.Context);

        nav.IsQuickMenuOpen = false;
        Assert.Equal(InputContext.Gameplay, observer.EffectiveContext);
        Assert.Equal(InputContext.Gameplay, machine.Context);
    }

    /// <summary>
    /// FR-XBOXUI-003, TR-XBOXUI-003 (IMPL-XBOXUWP-020), TEST-XBOXUI-003
    /// page-navigation context guard.
    /// Use case: navigating into a full-screen page (e.g. Settings) must request a
    /// non-Gameplay context so the gamepad drives the UI, and popping back to the
    /// bare gameplay view must request Gameplay again.
    /// Acceptance: Push(Settings) drives both EffectiveContext and
    /// XboxInputContext.Context off Gameplay; GoBack() to the empty base returns
    /// both to Gameplay.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void NavigatingIntoPage_RequestsNonGameplay_BackRestoresGameplay()
    {
        var machine = new XboxInputContext();
        var nav = new NavigationViewModel();
        using var observer = new InputContextObserver(nav, machine);

        nav.Push(NavigationDestination.Settings);
        Assert.NotEqual(InputContext.Gameplay, observer.EffectiveContext);
        Assert.NotEqual(InputContext.Gameplay, machine.Context);

        Assert.True(nav.GoBack());
        Assert.Equal(InputContext.Gameplay, observer.EffectiveContext);
        Assert.Equal(InputContext.Gameplay, machine.Context);
    }

    /// <summary>
    /// FR-XBOXUI-003, TR-XBOXUI-003 (IMPL-XBOXUWP-020), TEST-XBOXUI-003
    /// virtual-keyboard mapping guard.
    /// Use case: the on-screen virtual keyboard overlay must map to the dedicated
    /// <see cref="InputContext.VirtualKeyboard"/> context (not the generic menu
    /// context) so the input machine routes keys correctly.
    /// Acceptance: setting IsVirtualKeyboardOpen true requests
    /// InputContext.VirtualKeyboard, and the observer reflects
    /// XboxInputContext.Context == VirtualKeyboard.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void VirtualKeyboardOverlay_RequestsVirtualKeyboardContext()
    {
        var machine = new XboxInputContext();
        var nav = new NavigationViewModel();
        using var observer = new InputContextObserver(nav, machine);

        nav.IsVirtualKeyboardOpen = true;

        Assert.Equal(InputContext.VirtualKeyboard, machine.Context);
        Assert.Equal(InputContext.VirtualKeyboard, observer.EffectiveContext);
    }

    /// <summary>
    /// FR-XBOXUI-003, TR-XBOXUI-003 (IMPL-XBOXUWP-020), TEST-XBOXUI-003
    /// single-authority guard.
    /// Use case: exactly one context must be authoritative at a time; the observer
    /// must never diverge from the single XboxInputContext authority, whatever the
    /// nav/overlay state.
    /// Acceptance: across a sequence of page pushes, overlay toggles, and GoBack
    /// pops, observer.EffectiveContext always equals XboxInputContext.Context, and
    /// the derived context stays non-Gameplay while any page OR overlay is active
    /// and returns to Gameplay only when the stack is empty and no overlay is open.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Observer_ReflectsSingleAuthority_NeverDiverges()
    {
        var machine = new XboxInputContext();
        var nav = new NavigationViewModel();
        using var observer = new InputContextObserver(nav, machine);

        Assert.Equal(machine.Context, observer.EffectiveContext);

        nav.Push(NavigationDestination.Settings);
        Assert.Equal(machine.Context, observer.EffectiveContext);
        Assert.NotEqual(InputContext.Gameplay, machine.Context);

        nav.IsQuickMenuOpen = true;
        Assert.Equal(machine.Context, observer.EffectiveContext);

        // Pop the page while the quick menu is still open: stack empty but an
        // overlay remains, so the context must stay non-Gameplay.
        Assert.True(nav.GoBack());
        Assert.Equal(machine.Context, observer.EffectiveContext);
        Assert.NotEqual(InputContext.Gameplay, machine.Context);

        // Close the last overlay with an empty stack: back to Gameplay.
        nav.IsQuickMenuOpen = false;
        Assert.Equal(machine.Context, observer.EffectiveContext);
        Assert.Equal(InputContext.Gameplay, machine.Context);
    }
}
