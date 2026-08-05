namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S20 (IMPL-XBOXUWP-020). TEST-XBOXUI-001: the explicit,
/// unit-testable navigation back-stack in <see cref="NavigationViewModel"/>
/// (<c>ViceSharp.Xbox.ViewModels</c>).
/// </summary>
/// <remarks>
/// <para>
/// The 10-foot UI navigates a set of PUSHABLE pages
/// (<see cref="NavigationDestination"/>: Home, Settings, DeviceSetup,
/// InputMapping, About) over an always-present base In-emulator (gameplay) view.
/// The base view is modeled as <c>Current == null</c> (an empty stack), not a
/// stack entry, so returning to the bare gameplay surface simply empties the
/// stack. The quick menu and the on-screen virtual keyboard are OVERLAY FLAGS
/// (<see cref="NavigationViewModel.IsQuickMenuOpen"/> /
/// <see cref="NavigationViewModel.IsVirtualKeyboardOpen"/>), never stack entries.
/// </para>
/// <para>
/// The stack is EXPLICIT (a private list), independent of any XAML Frame journal,
/// so it is fully testable off-console.
/// </para>
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class NavigationViewModelTests
{
    /// <summary>
    /// FR-XBOXUI-001, TR-XBOXUI-001 (IMPL-XBOXUWP-020), TEST-XBOXUI-001 back-stack
    /// order guard.
    /// Use case: the couch UI pushes pages onto an explicit stack and the Back
    /// control pops exactly one level at a time, eventually returning to the bare
    /// In-emulator base view without recreating it.
    /// Acceptance: from the empty base (<c>Current == null</c>, CanGoBack false),
    /// Push(Settings) then Push(DeviceSetup) makes Current follow
    /// null -> Settings -> DeviceSetup with Depth 0 -> 1 -> 2; GoBack() twice pops
    /// DeviceSetup -> Settings -> null in order, each returning true and
    /// decrementing Depth, and CanGoBack reflects the depth (true while pages
    /// remain, false at the base).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void PushThenGoBack_ReturnsToBase_InOrder()
    {
        var nav = new NavigationViewModel();

        Assert.Null(nav.Current);
        Assert.False(nav.CanGoBack);
        Assert.Equal(0, nav.Depth);

        nav.Push(NavigationDestination.Settings);
        Assert.Equal(NavigationDestination.Settings, nav.Current);
        Assert.True(nav.CanGoBack);
        Assert.Equal(1, nav.Depth);

        nav.Push(NavigationDestination.DeviceSetup);
        Assert.Equal(NavigationDestination.DeviceSetup, nav.Current);
        Assert.True(nav.CanGoBack);
        Assert.Equal(2, nav.Depth);

        Assert.True(nav.GoBack());
        Assert.Equal(NavigationDestination.Settings, nav.Current);
        Assert.True(nav.CanGoBack);
        Assert.Equal(1, nav.Depth);

        Assert.True(nav.GoBack());
        Assert.Null(nav.Current);
        Assert.False(nav.CanGoBack);
        Assert.Equal(0, nav.Depth);
    }

    /// <summary>
    /// FR-XBOXUI-001, TR-XBOXUI-001 (IMPL-XBOXUWP-020), TEST-XBOXUI-001 overlay
    /// non-push guard.
    /// Use case: opening the quick menu or the virtual keyboard is an overlay over
    /// the current surface, not a navigation, so it must not grow the back stack or
    /// change the current page (otherwise Back would dismiss the overlay by popping
    /// a page).
    /// Acceptance: with Settings pushed, setting IsQuickMenuOpen and then
    /// IsVirtualKeyboardOpen true leaves Depth and Current unchanged while the
    /// respective overlay flag reads true.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Overlays_DoNotPushStackEntries()
    {
        var nav = new NavigationViewModel();
        nav.Push(NavigationDestination.Settings);

        int depth = nav.Depth;
        NavigationDestination? current = nav.Current;

        nav.IsQuickMenuOpen = true;
        Assert.True(nav.IsQuickMenuOpen);
        Assert.Equal(depth, nav.Depth);
        Assert.Equal(current, nav.Current);

        nav.IsVirtualKeyboardOpen = true;
        Assert.True(nav.IsVirtualKeyboardOpen);
        Assert.Equal(depth, nav.Depth);
        Assert.Equal(current, nav.Current);
    }

    /// <summary>
    /// FR-XBOXUI-001, TR-XBOXUI-001 (IMPL-XBOXUWP-020), TEST-XBOXUI-001 empty-stack
    /// guard.
    /// Use case: pressing Back at the bare In-emulator base view (nothing pushed)
    /// must be a no-op, not throw or underflow the stack.
    /// Acceptance: GoBack() on a fresh view-model returns false and leaves
    /// Current null and CanGoBack false.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void GoBack_EmptyStack_ReturnsFalse()
    {
        var nav = new NavigationViewModel();

        Assert.False(nav.GoBack());
        Assert.Null(nav.Current);
        Assert.False(nav.CanGoBack);
        Assert.Equal(0, nav.Depth);
    }
}
