namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using Xunit;

/// <summary>
/// FEAT-XMENUPAUSE-001 (PLAN-XBOXUWP, area XBOXUI). Operator 2026-07-14: "Emulator
/// needs to pause when opening the menu and unpause when done."
/// </summary>
/// <remarks>
/// FR: FR-XBOXUI-003 (context authority; the shell menu owns input while open).
/// Use case: opening the shell menu freezes the machine (no gameplay progresses
/// while navigating settings), and every dismissal path resumes it; the host's
/// Pause/Resume are session-locked and idempotent, so boot-time HideMenu calls and
/// the Home page's own Resume stay harmless. The virtual keyboard deliberately does
/// NOT pause: it types into the running machine.
/// Acceptance (structural pins for the #if HAS_UWP head):
///   TEST-XMENUPAUSE-001a: ShowMenu pauses the session; HideMenu resumes it; both
///     are guarded (never throw before the host is built).
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxMenuPauseTests
{
    [Fact]
    public void ShowMenu_Pauses_AndHideMenu_Resumes()
    {
        var app = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "App.xaml.cs"))
            .ToLowerInvariant();

        Assert.Contains("trypauseemulation", app);
        Assert.Contains("tryresumeemulation", app);
        Assert.Contains("menu open: emulation paused", app);
        Assert.Contains("menu closed: emulation resumed", app);
    }

    [Fact]
    public void Menu_SaveAndLoad_PersistSnapshotsToDisk()
    {
        // FEAT-XMENUSNAP-001 (operator 2026-07-14: "Add SAVE and LOAD buttons that can
        // save and load snapshots"): the menu captures the PAUSED machine to a durable
        // LocalState slot (AOT-safe source-generated JSON) and restores it on demand.
        var app = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "App.xaml.cs"))
            .ToLowerInvariant();

        Assert.Contains("savesnapshotasync", app);
        Assert.Contains("loadsnapshotasync", app);
        Assert.Contains("snapshot-slot1.json", app);

        // AOT/trim-safe serialization: the source-generated context, never the
        // reflection JsonSerializer overloads.
        var context = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox", "Platform", "SnapshotJsonContext.cs");
        Assert.True(File.Exists(context), $"Expected the snapshot JSON context at '{context}'.");
        Assert.Contains("snapshotjsoncontext.default.snapshotdto", app);
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
