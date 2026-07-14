namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XKEYCAPSHIFT-001 (PLAN-XKEYBOARD-001 follow-up). Operator 2026-07-14: "When
/// holding SHIFT or C= modifiers, change the keycap to match the character to be
/// inserted." Structural wiring of the #if HAS_UWP head: the overlay swaps each keycap
/// to its ShiftedLabel while SHIFT is effective (trigger hold, SHIFT-LOCK latch, or a
/// momentary one-shot arm) and back when it clears.
/// </summary>
/// <remarks>
/// Acceptance:
///   TEST-XKEYCAP-001a (in XboxAuthenticKeyboardTests): the layout carries the exact
///     printable shifted legends of the physical keycap tops; graphics-producing keys
///     stay null (never a wrong glyph).
///   TEST-XKEYCAP-001b: the overlay exposes the shift-visual refresh (RefreshKeycaps /
///     SetExternalShift) and applies ShiftedLabel over DisplayLabel.
///   TEST-XKEYCAP-001c: the head drives the overlay's external-shift flag from the
///     trigger-modifier commands, and the overlay re-syncs after tile presses (latch /
///     momentary arm changes).
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxKeycapShiftTests
{
    [Fact]
    public void Overlay_SwapsKeycaps_WithTheShiftVisual()
    {
        var overlay = ReadLower("src", "ViceSharp.Xbox", "Controls", "VirtualKeyboardOverlay.xaml.cs");
        Assert.Contains("refreshkeycaps", overlay);
        Assert.Contains("setexternalshift", overlay);
        Assert.Contains("shiftedlabel", overlay);
    }

    [Fact]
    public void Head_DrivesTheOverlayShiftVisual_FromTheTriggerModifiers()
    {
        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("setexternalshift(true)", app);
        Assert.Contains("setexternalshift(false)", app);
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
