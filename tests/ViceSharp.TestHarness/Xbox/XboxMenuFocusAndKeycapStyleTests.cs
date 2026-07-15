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
        // The breadbin identity: warm brown cap + PetMe64 face + raised-key bevel.
        Assert.Contains("#FF40352C", styles);
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
