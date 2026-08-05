namespace ViceSharp.Xbox.ViewModels;

using System;
using ViceSharp.Xbox.Input;

/// <summary>
/// A thin observer that DERIVES the effective UI input context from
/// <see cref="NavigationViewModel"/> state and REQUESTS it on the single authority
/// <see cref="XboxInputContext"/>. It is NOT a second state machine: its
/// <see cref="EffectiveContext"/> reflects <see cref="XboxInputContext.Context"/>
/// directly.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S20 (IMPL-XBOXUWP-020), FR-XBOXUI-003 / TR-XBOXUI-003, R5. UI
/// navigation and gameplay input must never be simultaneously live; exactly one
/// context is authoritative. The observer subscribes to
/// <see cref="NavigationViewModel.StateChanged"/> and, on every change (and once at
/// construction), computes the desired context and calls
/// <see cref="XboxInputContext.RequestContext(InputContext)"/>:
/// </para>
/// <list type="bullet">
///   <item><description>
///   The virtual-keyboard overlay maps to
///   <see cref="InputContext.VirtualKeyboard"/> (it takes precedence, being the most
///   specific non-Gameplay surface).
///   </description></item>
///   <item><description>
///   Otherwise the context is <see cref="InputContext.Gameplay"/> iff the page stack
///   is empty AND the quick menu is closed (the bare In-emulator view); any pushed
///   page or an open quick menu yields <see cref="InputContext.MainMenu"/>.
///   </description></item>
/// </list>
/// <para>
/// The gamepad Menu/View/Y button edges resolved by
/// <see cref="XboxInputContext.Tick(long, in GamepadSnapshot)"/> and this UI-driven
/// path both write the one <see cref="XboxInputContext.Context"/> field, so they can
/// never disagree. This type holds no engine, host, or XAML reference
/// (TR-MVVM-001).
/// </para>
/// </remarks>
public sealed class InputContextObserver : IDisposable
{
    private readonly NavigationViewModel _navigation;
    private readonly XboxInputContext _inputContext;
    private bool _disposed;

    /// <summary>
    /// Creates the observer over a navigation view-model and the single input-context
    /// authority, subscribes to navigation changes, and immediately requests the
    /// context derived from the current navigation state.
    /// </summary>
    /// <param name="navigation">The navigation view-model to observe.</param>
    /// <param name="inputContext">
    /// The single input-context authority to request context changes on.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="navigation"/> or <paramref name="inputContext"/> is
    /// <c>null</c>.
    /// </exception>
    public InputContextObserver(NavigationViewModel navigation, XboxInputContext inputContext)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _inputContext = inputContext ?? throw new ArgumentNullException(nameof(inputContext));

        _navigation.StateChanged += OnNavigationStateChanged;
        Sync();
    }

    /// <summary>
    /// The effective UI input context, read straight from the single authority
    /// <see cref="XboxInputContext.Context"/>. The observer never stores its own copy,
    /// so it cannot diverge from the machine.
    /// </summary>
    public InputContext EffectiveContext => _inputContext.Context;

    /// <summary>
    /// Recomputes the desired context from the current navigation state and requests
    /// it on the single authority. Called automatically on every
    /// <see cref="NavigationViewModel.StateChanged"/>; exposed so the wiring can force
    /// an initial reconcile.
    /// </summary>
    public void Sync() => _inputContext.RequestContext(DeriveContext());

    private InputContext DeriveContext()
    {
        if (_navigation.IsVirtualKeyboardOpen)
        {
            return InputContext.VirtualKeyboard;
        }

        bool bareGameplay = _navigation.Current is null && !_navigation.IsQuickMenuOpen;
        return bareGameplay ? InputContext.Gameplay : InputContext.MainMenu;
    }

    private void OnNavigationStateChanged(object? sender, EventArgs e) => Sync();

    /// <summary>
    /// Unsubscribes from the navigation view-model so the observer stops requesting
    /// context changes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _navigation.StateChanged -= OnNavigationStateChanged;
        _disposed = true;
    }
}
