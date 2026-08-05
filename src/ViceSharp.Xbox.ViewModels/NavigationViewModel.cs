namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;

/// <summary>
/// The explicit navigation back-stack for the 10-foot (couch) UI: an ordered stack
/// of pushable <see cref="NavigationDestination"/> pages over an always-present base
/// In-emulator (gameplay) view, plus the two overlay flags (quick menu and virtual
/// keyboard).
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S20 (IMPL-XBOXUWP-020), FR-XBOXUI-001 / TR-XBOXUI-001. The stack is
/// EXPLICIT (a private <see cref="List{T}"/>), independent of any XAML Frame journal,
/// so all navigation logic is unit-testable off-console. The base view is modeled as
/// an EMPTY stack (<see cref="Current"/> == <c>null</c>): it is the permanent video
/// surface, never a stack entry, so returning to gameplay simply empties the stack
/// and never recreates or pauses the running surface.
/// </para>
/// <para>
/// The quick menu and the on-screen virtual keyboard are OVERLAYS
/// (<see cref="IsQuickMenuOpen"/> / <see cref="IsVirtualKeyboardOpen"/>): they are
/// boolean flags, NOT stack entries, so opening or closing them never changes the
/// page stack and the Back control never dismisses an overlay by popping a page.
/// </para>
/// <para>
/// Any change to the current page or an overlay flag raises <see cref="StateChanged"/>
/// so a thin observer (e.g. <see cref="InputContextObserver"/>) can request the
/// matching input context on the single authority. This type holds no engine, host,
/// or XAML reference (TR-MVVM-001).
/// </para>
/// </remarks>
public sealed class NavigationViewModel
{
    private readonly List<NavigationDestination> _stack = new();
    private bool _isQuickMenuOpen;
    private bool _isVirtualKeyboardOpen;

    /// <summary>
    /// Raised whenever the navigation state changes: a page is pushed or popped, or
    /// either overlay flag toggles. Carries no payload; observers read the current
    /// state from this instance.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// The page currently on top of the back stack, or <c>null</c> when the stack is
    /// empty (the base In-emulator / gameplay view). Overlays do not affect this
    /// value.
    /// </summary>
    public NavigationDestination? Current => _stack.Count == 0 ? null : _stack[^1];

    /// <summary>
    /// The number of pages currently on the back stack (0 at the base view). Overlays
    /// never contribute to the depth.
    /// </summary>
    public int Depth => _stack.Count;

    /// <summary>
    /// True when at least one page is on the stack (so <see cref="GoBack"/> would pop
    /// one); false at the base In-emulator view.
    /// </summary>
    public bool CanGoBack => _stack.Count > 0;

    /// <summary>
    /// Whether the quick-menu overlay is open. Setting this is an overlay toggle: it
    /// does NOT push or pop the page stack. Changing the value raises
    /// <see cref="StateChanged"/>.
    /// </summary>
    public bool IsQuickMenuOpen
    {
        get => _isQuickMenuOpen;
        set
        {
            if (_isQuickMenuOpen == value)
            {
                return;
            }

            _isQuickMenuOpen = value;
            RaiseStateChanged();
        }
    }

    /// <summary>
    /// Whether the on-screen virtual-keyboard overlay is open. Setting this is an
    /// overlay toggle: it does NOT push or pop the page stack. Changing the value
    /// raises <see cref="StateChanged"/>.
    /// </summary>
    public bool IsVirtualKeyboardOpen
    {
        get => _isVirtualKeyboardOpen;
        set
        {
            if (_isVirtualKeyboardOpen == value)
            {
                return;
            }

            _isVirtualKeyboardOpen = value;
            RaiseStateChanged();
        }
    }

    /// <summary>
    /// Pushes a page onto the back stack, making it the new <see cref="Current"/>, and
    /// raises <see cref="StateChanged"/>.
    /// </summary>
    /// <param name="destination">The page to navigate to.</param>
    public void Push(NavigationDestination destination)
    {
        _stack.Add(destination);
        RaiseStateChanged();
    }

    /// <summary>
    /// Pops the top page off the back stack, returning to the previous page (or the
    /// base In-emulator view when the last page is popped).
    /// </summary>
    /// <returns>
    /// <c>true</c> if a page was popped (and <see cref="StateChanged"/> raised);
    /// <c>false</c> when the stack was already empty (a no-op at the base view).
    /// </returns>
    public bool GoBack()
    {
        if (_stack.Count == 0)
        {
            return false;
        }

        _stack.RemoveAt(_stack.Count - 1);
        RaiseStateChanged();
        return true;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
