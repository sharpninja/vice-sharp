namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FIX-XNTSCFPS-001 (PLAN-XBOXUWP, area XVIDEO). Operator 2026-07-14, from the on-device
/// HUD (FPS 22.3 / EMU 22.3 / SPD 98.6% on NTSC): "For NTSC, should be targeting 60 FPS.
/// Also, the FPS is half what would be expected at 100%." SPD ~100% proves the emulation
/// holds real time via the gate's deficit catch-up; the RENDER loop was the bottleneck:
/// a fixed 20 ms timer plus a per-tick full-target clear and a per-pixel division blit at
/// panel resolution (~7.6M cleared + 2.9M divided pixels per tick on the UI thread).
/// </summary>
/// <remarks>
/// Acceptance:
///   TEST-XFPS-001a: the render cadence derives from the ACTIVE machine's refresh rate
///     (NTSC ~59.826 Hz -> ~16.7 ms, PAL ~50.125 Hz -> ~19.95 ms), clamped to sane bounds,
///     defaulting to 20 ms when unknown.
///   TEST-XFPS-001b: the nearest-neighbor coordinate maps are precomputed (no per-pixel
///     division on the hot path): exact mapping, end clamped inside the source, degenerate
///     inputs yield empty maps.
///   TEST-XFPS-001c (structural): the surface consumes the cadence + map helpers, clears
///     the target only when geometry changes, and the head feeds the cadence from the live
///     session's refresh rate at boot and on model change.
/// </remarks>
public sealed class XboxVideoCadenceTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void IntervalMs_TracksTheMachineRefreshRate_AtHalfPeriod()
    {
        // TEST-XFPS-001a: VICE-true refresh rates -> HALF-period render intervals (the
        // dispatcher timer quantizes up to the ~15.6 ms system tick; a full-period NTSC
        // interval measured ~32 fps on-device, the half-period lands on one tick).
        Assert.InRange(VideoCadence.IntervalMsFor(59.826), 8.35, 8.37);
        Assert.InRange(VideoCadence.IntervalMsFor(50.125), 9.97, 9.99);

        // Unknown machine -> the historical 20 ms default; absurd rates clamp.
        Assert.Equal(20.0, VideoCadence.IntervalMsFor(0));
        Assert.Equal(20.0, VideoCadence.IntervalMsFor(-1));
        Assert.Equal(4.0, VideoCadence.IntervalMsFor(500));
        Assert.Equal(40.0, VideoCadence.IntervalMsFor(5));
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void NearestNeighborMap_IsExact_AndClamped()
    {
        // TEST-XFPS-001b: 4 source pixels across 8 destination pixels.
        Assert.Equal(new[] { 0, 0, 1, 1, 2, 2, 3, 3 }, NearestNeighborMap.Build(4, 8));

        // Real geometry: 384 source columns across 1470 destination columns.
        var map = NearestNeighborMap.Build(384, 1470);
        Assert.Equal(1470, map.Length);
        Assert.Equal(0, map[0]);
        Assert.Equal(383, map[^1]);
        for (var i = 1; i < map.Length; i++)
        {
            Assert.True(map[i] >= map[i - 1], "map must be monotonic");
            Assert.InRange(map[i], 0, 383);
        }

        // Degenerate inputs never explode.
        Assert.Empty(NearestNeighborMap.Build(0, 10));
        Assert.Empty(NearestNeighborMap.Build(10, 0));
        Assert.Empty(NearestNeighborMap.Build(-1, -1));
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Head_WiresCadenceAndFastBlit()
    {
        // TEST-XFPS-001c: structural wiring of the #if HAS_UWP files.
        var surface = ReadLower("src", "ViceSharp.Xbox", "Controls", "VideoSurfaceHost.cs");
        Assert.Contains("settargetrefreshrate", surface);
        Assert.Contains("videocadence.intervalmsfor", surface);
        Assert.Contains("nearestneighbormap.build", surface);
        Assert.Contains("_clearpending", surface);

        var app = ReadLower("src", "ViceSharp.Xbox", "App.xaml.cs");
        Assert.Contains("settargetrefreshrate", app);
        Assert.Contains("getrefreshratehz", app);
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
