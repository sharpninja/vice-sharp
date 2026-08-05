namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XMENUSUBPAGE-001 (operator 2026-07-14: "Whenever there is a back button that
/// can be clicked in the UI, the B button should go back, not close the menu panel.
/// Also, subpages from the menu need to expand fully."). Structural wiring of the
/// #if HAS_UWP head:
/// B (UiBack) is PAGE-TYPE-driven - on a subpage it always returns toward the home
/// rail (GoBack, or a direct Home navigate when the stack is empty); only on the home
/// rail does it dismiss the menu. Menu subpages expand to the FULL window (the frame
/// spans both root-grid columns) and contract back to the docked right rail on
/// HomePage. Show/hide also push the gamepad context machine explicitly, so a mouse
/// click or B-dismiss can no longer strand the controller in the MainMenu context
/// with an inert joystick.
/// </summary>
/// <remarks>
/// FR: FR-XBOXUI-002 (10-foot shell navigation), FR-CTX-002 (context machine owns
/// per-context input routing). TR: TR-XBOXUI-001.
/// Use case: the player opens the docked menu, enters Settings/Devices/Controls/About
/// (page fills the window), presses B to step back to the rail, and B again (or Close
/// Menu) to dismiss; gameplay input resumes immediately after any dismissal path.
/// Acceptance:
///   TEST-XMENUSUB-001a: UiBack is page-type-driven: HomePage (or empty) dismisses;
///     a subpage GoBacks, with a Navigate-to-Home fallback for an empty back stack.
///   TEST-XMENUSUB-001b: a Navigated handler expands non-Home pages across both
///     columns and restores the Home rail to the docked right column.
///   TEST-XMENUSUB-001c: ShowMenu requests the MainMenu input context and HideMenu
///     requests Gameplay (or VirtualKeyboard while the dock is open), for every
///     dismissal path.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxMenuSubpageTests
{
    [Fact]
    public void UiBack_IsPageTypeDriven()
    {
        var app = ReadApp();

        // TEST-XMENUSUB-001a: root dismisses; subpage goes back; stackless subpage
        // still lands on the rail instead of closing.
        Assert.Contains("Content is Views.HomePage", app);
        Assert.Contains("_frame.GoBack()", app);
        Assert.Contains("_frame.Navigate(typeof(Views.HomePage))", app);
    }

    [Fact]
    public void Subpages_ExpandAcrossBothColumns_AndTheRailContracts()
    {
        var app = ReadApp();

        // TEST-XMENUSUB-001b: the frame's Navigated handler drives the layout.
        Assert.Contains("_frame.Navigated", app);
        Assert.Contains("Grid.SetColumnSpan(_frame, 2)", app);
        Assert.Contains("Grid.SetColumn(_frame, 0)", app);
        Assert.Contains("Grid.SetColumnSpan(_frame, 1)", app);
        Assert.Contains("Grid.SetColumn(_frame, 1)", app);

        // Expanded pages sit OVER the paused emulator: the frame supplies the opaque
        // backdrop (the pages' own translucent washes were tuned for the empty right
        // column), and the rail restores the see-through dock.
        Assert.Contains("_frame.Background = ", app);

        // Navigation failures must land in the log: a page whose construction or
        // OnNavigatedTo throws otherwise just silently refuses to open.
        Assert.Contains("NavigationFailed", app);
    }

    [Fact]
    public void ShowAndHide_PushTheGamepadContext()
    {
        var app = ReadApp();

        // TEST-XMENUSUB-001c: no dismissal path may strand the controller in the
        // MainMenu context (mouse Close Menu / background click / B on the rail).
        Assert.Contains("RequestContext(ViceSharp.Xbox.Input.InputContext.MainMenu)", app);
        Assert.Contains("RequestContext(ViceSharp.Xbox.Input.InputContext.VirtualKeyboard)", app);
        Assert.Contains("RequestContext(ViceSharp.Xbox.Input.InputContext.Gameplay)", app);
    }

    private static string ReadApp()
    {
        var path = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.True(File.Exists(path), $"Expected source file at '{path}'.");
        return File.ReadAllText(path);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
