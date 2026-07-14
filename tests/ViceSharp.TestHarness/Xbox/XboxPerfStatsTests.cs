namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FEAT-XPERFHUD-001 (PLAN-XBOXUWP, area XVIDEO): performance stats rendered in the
/// letterbox area beside the emulator display.
///
/// FR: FR-XVIDEO-002 (render pipeline; the HUD is a pure sink over data the render loop
/// already has). Use case: the operator wants live performance stats on the LEFT
/// letterbox bar: presented FPS, emulated FPS, emulation speed percent (cycle rate vs
/// the machine's nominal clock), and the active standard + pixel aspect. The rate math
/// lives in the portable VideoPerfStatsViewModel (System only, TR-MVVM-001) driven by
/// explicit timestamps so it is fully unit-testable headless; the #if HAS_UWP surface
/// only records samples and displays the formatted text.
/// Acceptance:
///   TEST-XPERFHUD-001a: present/emulated FPS and speed percent computed from recorded
///     samples over an explicit clock (50 presents + 50 frames advancing one PAL second
///     of cycles over one second reads 50.0 / 50.0 / 100.0%).
///   TEST-XPERFHUD-001b: compute throttles (no text before the minimum window),
///     repeated cycle stamps do not count as emulated frames, and an unknown machine
///     clock omits the speed line rather than fabricating one.
///   TEST-XPERFHUD-001c: the formatted overlay carries the standard label and pixel
///     aspect.
///   TEST-XPERFHUD-001d: structural head wiring: the surface records + raises the HUD
///     text, EmulatorView hosts the left-letterbox TextBlock, the App feeds the machine
///     clock/standard at boot and on model change.
/// </summary>
public sealed class XboxPerfStatsTests
{
    private const double PalClockHz = 985248d;

    [Fact]
    [Trait("Category", "Xbox")]
    public void Compute_ReportsPresentFps_EmuFps_AndSpeedPercent()
    {
        // TEST-XPERFHUD-001a: fake clock at 1000 ticks/second.
        var stats = new VideoPerfStatsViewModel();
        stats.SetMachine(PalClockHz, "PAL", 0.93650794f);

        // 50 presents + 50 frames, cycles advancing exactly one PAL second in total.
        for (var i = 0; i < 50; i++)
        {
            long ts = i * 20;
            stats.RecordPresent(ts);
            stats.RecordFrame((i + 1) * 19704L, ts);
        }

        Assert.True(stats.TryComputeText(now: 1000, frequency: 1000, out var text));

        Assert.Contains("FPS 50.0", text);
        Assert.Contains("EMU 50.0", text);

        // 50 deltas of 19704 cycles = 985200 cycles over 1s = 99.995% -> 100.0%.
        Assert.Contains("SPD 100.0%", text);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Compute_Throttles_IgnoresRepeatedCycles_AndOmitsSpeedWithoutClock()
    {
        var stats = new VideoPerfStatsViewModel();
        stats.SetMachine(0d, "PAL", 0.93650794f);

        stats.RecordPresent(0);
        stats.RecordFrame(1000, 0);
        stats.RecordFrame(1000, 10); // same cycle stamp: NOT a new emulated frame.
        stats.RecordFrame(2000, 20);

        // TEST-XPERFHUD-001b: below the minimum window -> no text yet.
        Assert.False(stats.TryComputeText(now: 100, frequency: 1000, out _));

        Assert.True(stats.TryComputeText(now: 1000, frequency: 1000, out var text));
        Assert.Contains("EMU 2.0", text);

        // Unknown machine clock: the speed line is omitted, never fabricated.
        Assert.DoesNotContain("SPD", text);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Compute_CarriesStandardLabel_ClockAndPixelAspect()
    {
        var stats = new VideoPerfStatsViewModel();
        stats.SetMachine(1022727d, "NTSC", 0.75f);

        stats.RecordPresent(0);
        Assert.True(stats.TryComputeText(now: 1000, frequency: 1000, out var text));

        // TEST-XPERFHUD-001c (operator 2026-07-14: "What does 'NTSC 0.75' mean? NTSC is
        // over 1Mhz" - the bare number read as a clock). The machine line spells out BOTH:
        // the nominal clock in MHz and the labeled composite Pixel Aspect Ratio.
        Assert.Contains("NTSC 1.02MHz PAR 0.75", text);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Compute_OmitsClock_WhenUnknown_ButKeepsLabeledPar()
    {
        var stats = new VideoPerfStatsViewModel();
        stats.SetMachine(0d, "PAL", 0.93650794f);

        stats.RecordPresent(0);
        Assert.True(stats.TryComputeText(now: 1000, frequency: 1000, out var text));

        // No clock -> no MHz claim (never fabricate), but the PAR stays labeled.
        Assert.Contains("PAL PAR 0.94", text);
        Assert.DoesNotContain("MHz", text);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Head_WiresHud_SurfaceEmulatorViewAndApp()
    {
        // TEST-XPERFHUD-001d: structural wiring of the #if HAS_UWP files the headless
        // fallback cannot execute.
        var surface = ReadLower("src", "ViceSharp.Xbox", "Controls", "VideoSurfaceHost.cs");
        Assert.Contains("attachstats", surface);
        Assert.Contains("recordpresent", surface);
        Assert.Contains("recordframe", surface);
        Assert.Contains("statstextupdated", surface);

        var view = ReadLower("src", "ViceSharp.Xbox", "Views", "EmulatorView.xaml");
        Assert.Contains("perfstats", view);
        Assert.Contains("horizontalalignment=\"left\"", view);

        var viewCode = ReadLower("src", "ViceSharp.Xbox", "Views", "EmulatorView.xaml.cs");
        Assert.Contains("statstextupdated", viewCode);

        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("attachstats", app);
        Assert.Contains("setmachine", app);
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
