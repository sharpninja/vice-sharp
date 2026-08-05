namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using global::Avalonia.Input;
using ViceSharp.Avalonia;
using Xunit;

/// <summary>
/// Emulator-only fullscreen hotkeys (F11, Alt+Enter): must be recognized by the head and
/// wired so chrome is hidden and the video surface is centered.
/// </summary>
public sealed class AvaloniaFullscreenHotkeyTests
{
    [Theory]
    [InlineData(Key.F11, KeyModifiers.None, true)]
    [InlineData(Key.F11, KeyModifiers.Control, false)]
    [InlineData(Key.F11, KeyModifiers.Alt, false)]
    [InlineData(Key.Enter, KeyModifiers.Alt, true)]
    [InlineData(Key.Enter, KeyModifiers.None, false)]
    [InlineData(Key.Enter, KeyModifiers.Alt | KeyModifiers.Control, false)]
    [InlineData(Key.R, KeyModifiers.None, false)]
    public void IsEmulatorFullscreenHotkey_MatchesF11AndAltEnter(Key key, KeyModifiers mods, bool expected)
    {
        Assert.Equal(expected, MainWindow.IsEmulatorFullscreenHotkey(key, mods));
    }

    [Fact]
    public void Head_WiresFullscreenToggleAndChrome()
    {
        var main = ReadLower("src", "ViceSharp.Avalonia", "MainWindow.axaml.cs");
        Assert.Contains("toggleemulatorfullscreen", main);
        Assert.Contains("enteremulatorfullscreen", main);
        Assert.Contains("exitemulatorfullscreen", main);
        Assert.Contains("isemulatorfullscreenhotkey", main);
        Assert.Contains("windowstate.fullscreen", main);
        Assert.Contains("horizontalalignment.center", main);

        var axaml = ReadLower("src", "ViceSharp.Avalonia", "MainWindow.axaml");
        Assert.Contains("part_statusbar", axaml);
        Assert.Contains("part_menu", axaml);
    }

    private static string ReadLower(params string[] parts)
    {
        var path = Path.Combine(RepoRoot, Path.Combine(parts));
        Assert.True(File.Exists(path), $"Expected source file at '{path}'.");
        return File.ReadAllText(path).ToLowerInvariant();
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
