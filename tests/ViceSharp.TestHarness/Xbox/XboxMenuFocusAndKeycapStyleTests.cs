namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XMENUFOCUS-001 + FEAT-XKEYCAPSTYLE-001 (operator 2026-07-14: "When opening the
/// menu, always set focus to the 'Close Menu' button (which should be at the top of the
/// menu)" and "Apply CBM button style to the virtual keyboad"). Structural pins of the
/// #if HAS_UWP head: menu order + programmatic focus on every open, and the shared
/// C64-keycap styling applied to the virtual keyboard tiles.
/// </summary>
/// <remarks>
/// FR: FR-XBOXUI-003 (10-foot menu shell), FR-XINPUT-005 (virtual keyboard).
/// TR: TR-XBOXUI-001.
/// Use case: the player opens the shell menu with Menu/ESC; focus lands on Close Menu
/// (the top button), so one press of A dismisses; the virtual keyboard reads like the
/// breadbin keycaps the menu buttons already use.
/// Acceptance:
///   TEST-XMENUFOCUS-001a: Close Menu is the FIRST button in HomePage.xaml (before
///     Save) and Restart stays LAST.
///   TEST-XMENUFOCUS-001b: HomePage exposes a programmatic Close-Menu focus and both
///     open paths drive it (navigation via OnNavigatedTo, re-show via App.ShowMenu).
///   TEST-XKEYCAPSTYLE-001a: the C64 keycap styles live in an app-level resource
///     dictionary merged by App.xaml (shared, not page-local).
///   TEST-XKEYCAPSTYLE-001b: the virtual keyboard tiles (both the rows and the
///     function column) use the shared keycap tile style.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxMenuFocusAndKeycapStyleTests
{
    [Fact]
    public void CloseMenu_IsTheFirstButton_AndRestartStaysLast()
    {
        var xaml = ReadSource("src", "ViceSharp.Xbox", "Views", "HomePage.xaml");

        var closeMenu = xaml.IndexOf("Close Menu", StringComparison.Ordinal);
        var save = xaml.IndexOf("\"Save\"", StringComparison.Ordinal);
        var restart = xaml.IndexOf("\"Restart\"", StringComparison.Ordinal);

        Assert.True(closeMenu >= 0, "HomePage.xaml must keep the Close Menu button.");
        Assert.True(save >= 0, "HomePage.xaml must keep the Save button.");
        Assert.True(restart >= 0, "HomePage.xaml must keep the Restart button.");

        Assert.True(closeMenu < save, "Close Menu must be the FIRST button (before Save).");
        Assert.True(restart > closeMenu && restart > save, "Restart must stay the LAST button.");
    }

    /// <summary>
    /// PLAN-ROMM-001: home rail must expose RomM entry points (lost in a merge, restored).
    /// Acceptance: Library / Lists / CSDb buttons exist, navigate handlers push the matching
    /// destinations, and Library sits after Load and before Settings.
    /// </summary>
    [Fact]
    public void HomeMenu_ExposesRomMLibraryListsAndCsdb()
    {
        var xaml = ReadSource("src", "ViceSharp.Xbox", "Views", "HomePage.xaml");
        var load = xaml.IndexOf("Content=\"Load\"", StringComparison.Ordinal);
        var library = xaml.IndexOf("Content=\"Library\"", StringComparison.Ordinal);
        var lists = xaml.IndexOf("Content=\"Lists\"", StringComparison.Ordinal);
        var csdb = xaml.IndexOf("Content=\"CSDb\"", StringComparison.Ordinal);
        var settings = xaml.IndexOf("Content=\"Settings\"", StringComparison.Ordinal);

        Assert.True(load >= 0 && library >= 0 && lists >= 0 && csdb >= 0 && settings >= 0);
        Assert.True(load < library && library < lists && lists < csdb && csdb < settings,
            "Expected order: Load, Library, Lists, CSDb, Settings.");

        var code = ReadSource("src", "ViceSharp.Xbox", "Views", "HomePage.xaml.cs");
        Assert.Contains("OnLibrary", code, StringComparison.Ordinal);
        Assert.Contains("NavigationDestination.Library", code, StringComparison.Ordinal);
        Assert.Contains("typeof(LibraryPage)", code, StringComparison.Ordinal);
        Assert.Contains("NavigationDestination.Lists", code, StringComparison.Ordinal);
        Assert.Contains("typeof(ListsPage)", code, StringComparison.Ordinal);
        Assert.Contains("NavigationDestination.Csdb", code, StringComparison.Ordinal);
        Assert.Contains("typeof(CsdbPage)", code, StringComparison.Ordinal);
    }

    [Fact]
    public void MenuOpen_FocusesCloseMenu_OnBothOpenPaths()
    {
        var page = ReadLower("src", "ViceSharp.Xbox", "Views", "HomePage.xaml.cs");
        Assert.Contains("focusclosemenu", page);
        Assert.Contains("focusstate.programmatic", page);
        Assert.Contains("onnavigatedto", page);

        // Re-shows flip Frame.Visibility without navigating, so ShowMenu must drive the
        // focus explicitly too.
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("focusclosemenu", app);
    }

    [Fact]
    public void KeycapStyles_AreAppLevel_SharedResources()
    {
        var styles = ReadSource("src", "ViceSharp.Xbox", "Styles", "C64Keycaps.xaml");
        Assert.Contains("C64KeyButtonStyle", styles);
        Assert.Contains("C64KeycapTileStyle", styles);
        // FEAT-XKEYCAPMODEL-001: the menu keycap colours are per-model brushes App repaints
        // (operator: "Menu colors should match virtual keyboard based on exact model"); the
        // breadbin default is the dark-brown function-key cap.
        Assert.Contains("C64MenuCapBrush", styles);
        Assert.Contains("#FF5A3B1E", styles);
        Assert.Contains("PetMe64.ttf#Pet Me 64", styles);
        Assert.Contains("2,2,4,5", styles);

        var app = ReadSource("src", "ViceSharp.Xbox", "App.xaml");
        Assert.Contains("Styles/C64Keycaps.xaml", app);

        // HomePage consumes the shared style rather than defining its own copy.
        var home = ReadSource("src", "ViceSharp.Xbox", "Views", "HomePage.xaml");
        Assert.Contains("C64KeyButtonStyle", home);
        Assert.DoesNotContain("<Style x:Key=\"C64KeyButtonStyle\"", home);
    }

    [Fact]
    public void Menu_DocksInARightColumn_AndShrinksTheEmulator()
    {
        // FEAT-XMENUSLIDE-001 (operator 2026-07-14: "INSTEAD of floating, ie. pinned
        // and consuming space like the keyboard"): the shell Frame DOCKS in a right
        // Auto column of the root grid, so showing the menu SHRINKS the emulator's
        // star column (never occludes it), exactly like the keyboard's bottom Auto
        // row. The page itself is the opaque panel (no full-screen scrim, no
        // click-outside backdrop) and slides in with the keyboard's edge transition.
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("columndefinitions.add", app);
        Assert.Contains("grid.setcolumn(frame, 1)", app);
        Assert.Contains("grid.setrowspan(frame, 2)", app);

        var xaml = ReadSource("src", "ViceSharp.Xbox", "Views", "HomePage.xaml");
        Assert.Contains("EdgeUIThemeTransition", xaml);
        Assert.Contains("Edge=\"Right\"", xaml);
        Assert.DoesNotContain("TvSafeAreaRootStyle", xaml);
        Assert.DoesNotContain("OnBackgroundDismiss", xaml);
    }

    [Fact]
    public void UiNavigation_IsThrottled_ToASingleNavigator()
    {
        // FIX-XDPADSKIP-002 (operator 2026-07-14: "dpad navigation is skipping buttons
        // again"): every directional focus move funnels through HandleUiNavigate; a
        // short wall-clock throttle there collapses any double-emission (polled
        // pipeline + a native XY leak, stick flicker re-arms) into one move, whatever
        // the source. The window sits below the repeater interval (220 ms) so held
        // auto-repeat still flows.
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("throttleuinav", app);
        Assert.Contains("uinavthrottlems", app);
    }

    [Fact]
    public void VirtualKeyboardTiles_UseTheKeycapTileStyle()
    {
        var overlay = ReadSource("src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml");

        // Both the five physical rows and the function column.
        var occurrences = 0;
        var index = 0;
        while ((index = overlay.IndexOf("C64KeycapTileStyle", index, StringComparison.Ordinal)) >= 0)
        {
            occurrences++;
            index += "C64KeycapTileStyle".Length;
        }

        Assert.True(occurrences >= 2, $"both tile templates must use the keycap style (found {occurrences} references).");
    }

    private static string ReadSource(params string[] parts)
    {
        var path = Path.Combine(RepoRoot, Path.Combine(parts));
        Assert.True(File.Exists(path), $"Expected source file at '{path}'.");
        return File.ReadAllText(path);
    }

    private static string ReadLower(params string[] parts)
        => ReadSource(parts).ToLowerInvariant();

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
