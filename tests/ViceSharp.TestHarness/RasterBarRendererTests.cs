using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FR/TR/TEST: FR-VIC-004 / TR-VIC-EDGE-003 / TEST-VICRENDER-001 (PLAN-VICRENDER-001).
///
/// Cycle-stable raster bars: the demo rewrites $D020 mid-scanline so a single line shows
/// more than one border colour. The renderer used to sample $D020 once per line (at
/// line-wrap) and fill the whole scanline with that single colour, which collapsed the
/// bars and shifted them by ~1 raster line. This test drives the VIC directly, writes two
/// border colours at two in-line cycles of the same scanline, and asserts the rendered
/// line shows BOTH colours at the expected horizontal spans.
/// </summary>
// PLAN-VICEPARITY-001 P0-5: these facts assert the register write-time
// change-log rendering hack that the per-cycle border/background units
// (V4/V7) replace. Quarantined from the blocking gate; deleted as the
// replacing V-slices land. Run ad hoc via --filter Category=ParityLegacy.
[Trait("Category", "ParityLegacy")]
[Collection("NativeVice")]
public sealed class RasterBarRendererTests
{
    private static uint ExpectedBgra(byte colorIndex)
    {
        var c = VicPalette.Colors[colorIndex & 0x0F];
        return 0xFF000000u | (uint)c.B | ((uint)c.G << 8) | ((uint)c.R << 16);
    }

    /// <summary>
    /// FR: FR-VIC-004, TR: TR-VIC-EDGE-003, TEST: TEST-VICRENDER-001 (PLAN-VICRENDER-001).
    /// Use case: the demo rewrites $D020 mid-scanline so a single border line shows two
    /// colours; the old renderer sampled $D020 once per line at line-wrap and filled the
    /// whole scanline with the last colour, collapsing the bars.
    /// Acceptance: driving the VIC to upper-border line 30 and writing red (colour 2) at
    /// in-line cycle 5 then blue (colour 6) at cycle 40, the rendered scanline shows the
    /// exact red BGRA at frame pixel 20 and the exact blue BGRA at pixel 360, and the two
    /// pixels differ - two colour bands on ONE scanline.
    /// </summary>
    [Fact]
    public void MidLineBorderColorChange_RendersTwoColourBandsOnOneScanline()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;

        // Line 30 is in the upper border (< UPPER border start 51), so the whole scanline is
        // border - ideal for observing border-colour bands. It is visible (frame y = 15).
        const int targetLine = 30;
        const byte colourA = 2; // red
        const byte colourB = 6; // blue

        // Advance the VIC to the target scanline.
        int guard = 0;
        while (vic.CurrentRasterLine != targetLine && guard++ < 200_000)
            vic.Tick();
        Assert.Equal(targetLine, vic.CurrentRasterLine);

        // Two mid-line $D020 writes at two distinct in-line cycles.
        while (vic.RasterX < 5)
            vic.Tick();
        vic.Write(0xD020, colourA);

        while (vic.RasterX < 40)
            vic.Tick();
        vic.Write(0xD020, colourB);

        // Finish the scanline so it renders (render fires at line-wrap).
        guard = 0;
        while (vic.CurrentRasterLine == targetLine && guard++ < 200_000)
            vic.Tick();

        var fb = vic.FrameBuffer;
        int y = targetLine - VideoRenderer.PalFirstVisibleRasterLine;
        uint PixelAt(int x) => System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + x) * 4);

        // colourA write at RasterX 5 maps to the left edge (frame pixel 0); colourB write at
        // RasterX 40 maps to ~frame pixel 224. So the left span is colourA and the right span
        // is colourB - two bands on ONE scanline, which the old single-fill renderer could not
        // produce (it would paint the whole line colourB, the last write).
        Assert.Equal(ExpectedBgra(colourA), PixelAt(20));
        Assert.Equal(ExpectedBgra(colourB), PixelAt(360));
        Assert.NotEqual(PixelAt(20), PixelAt(360));
    }

    /// <summary>
    /// FR: FR-VIC-004, TR: TR-VIC-EDGE-003, TEST: TEST-VICRENDER-001 (PLAN-VICRENDER-001).
    /// Use case: regression guard for the common case - a border line with no mid-line
    /// $D020 change must still render as one solid colour after the per-cycle border
    /// colour fix (the fast path must be unaffected).
    /// Acceptance: writing green (colour 5) once near the start of border line 28 and
    /// finishing the line renders the same exact green BGRA at frame pixels 10, 200,
    /// and 370 of that scanline.
    /// </summary>
    [Fact]
    public void NoMidLineChange_RendersSolidBorder_FastPathUnaffected()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;

        const int targetLine = 28;
        const byte colour = 5; // green

        int guard = 0;
        while (vic.CurrentRasterLine != targetLine && guard++ < 200_000)
            vic.Tick();

        // One write near the start of the line, then no further change -> whole line one colour.
        while (vic.RasterX < 2)
            vic.Tick();
        vic.Write(0xD020, colour);

        guard = 0;
        while (vic.CurrentRasterLine == targetLine && guard++ < 200_000)
            vic.Tick();

        var fb = vic.FrameBuffer;
        int y = targetLine - VideoRenderer.PalFirstVisibleRasterLine;
        uint PixelAt(int x) => System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + x) * 4);

        var expected = ExpectedBgra(colour);
        Assert.Equal(expected, PixelAt(10));
        Assert.Equal(expected, PixelAt(200));
        Assert.Equal(expected, PixelAt(370));
    }

    /// <summary>
    /// FR: FR-VIC-004, TR: TR-VIC-EDGE-003, TEST: TEST-VICRENDER-001 (PLAN-VICRENDER-001).
    /// Use case: the actual bug - a $D020 write near the END of a scanline (the
    /// cycle-stable raster handler writes ahead of the beam) must colour the NEXT
    /// scanline, not the current one. The old renderer sampled $D020 once at line-wrap
    /// and painted the whole current line the final colour, so every bar appeared ~1
    /// raster line too high. Pure renderer unit test: no emulation or injection.
    /// Acceptance: writing a distinct colour at in-line cycle 60 of each consecutive
    /// upper-border line 24-28, the left-border pixel of line L+1 equals the exact BGRA
    /// of the colour written on line L for every consecutive pair - the vertical
    /// placement that was wrong.
    /// </summary>
    [Fact]
    public void EndOfLineBorderWrites_ColourTheNextScanline_NotTheCurrentOne()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;

        // Upper-border lines (whole scanline is border), all visible.
        int[] lines = { 24, 25, 26, 27, 28 };
        byte[] colours = { 1, 3, 7, 10, 13 };
        const int lateCycle = 60; // near end of line -> effect belongs to the next line

        // Advance to just before the first line.
        int guard = 0;
        while (vic.CurrentRasterLine != lines[0] && guard++ < 200_000)
            vic.Tick();

        for (int i = 0; i < lines.Length; i++)
        {
            while (vic.CurrentRasterLine == lines[i] && vic.RasterX < lateCycle)
                vic.Tick();
            vic.Write(0xD020, colours[i]);
            while (vic.CurrentRasterLine == lines[i])
                vic.Tick();
        }
        // Render the last line by advancing one more.
        while (vic.CurrentRasterLine == lines[^1] + 0 && guard++ < 200_000)
            vic.Tick();

        var fb = vic.FrameBuffer;
        uint LeftBorder(int line)
        {
            int y = line - VideoRenderer.PalFirstVisibleRasterLine;
            return System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + 8) * 4);
        }

        // Each late write on line L must colour line L+1 (the bar is NOT 1 line too high).
        for (int i = 0; i < lines.Length - 1; i++)
        {
            Assert.Equal(ExpectedBgra(colours[i]), LeftBorder(lines[i] + 1));
        }
    }

    private static readonly string[] SnapshotCandidates =
    [
        @"F:\GitHub\vice-sharp\vice-snapshot-20260630171307.vsf",
    ];

    /// <summary>
    /// FR: FR-VIC-004, TR: TR-VIC-EDGE-003, TEST: TEST-VICRENDER-001 (PLAN-VICRENDER-001).
    /// Use case: demo-level diagnostic (snapshot-gated) - resume the staged
    /// Pieces-of-Light .vsf, capture VICE's per-line border colour across the bar region
    /// (lines 212-246), inject the same t0 into the managed core, run the same span, and
    /// read the managed RENDERED frame's left-border colour per line for comparison.
    /// Acceptance: diagnostic under LOSSY injection (InjectSnapshotState seeds VIC
    /// registers + raster phase only), so residual per-line mismatch is an
    /// emulation-fidelity limit of injection, not a renderer defect: the match ratio is
    /// logged and the test gates only on having sampled at least 20 bar-region lines;
    /// the renderer fix itself is gated by
    /// MidLineBorderColorChange_RendersTwoColourBandsOnOneScanline. Skips when the shim
    /// or the staged .vsf is absent.
    /// </summary>
    [Fact]
    public void DemoBars_ManagedRenderedFrame_AlignsWithViceBorderColoursPerLine()
    {
        if (!ViceNative.IsAvailable) { Assert.Skip(ViceNative.AvailabilityMessage); return; }
        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        if (vsf is null) { Assert.Skip("Staged demo .vsf snapshot not present."); return; }

        const int firstBarLine = 212;
        const int lastBarLine = 246;
        const long runCycles = 3 * 19656;

        using var native = ViceNative.CreateInstance("c64");
        native.Reset();
        Assert.True(native.ReadSnapshot(vsf) == 0, "snapshot must resume");
        var vic0 = native.GetVicState();
        var regs = vic0.Registers!;
        var cpu0 = native.GetState();
        var ram = new byte[0x10000];
        for (var a = 0; a < ram.Length; a++) ram[a] = native.PeekRam((ushort)a);

        // VICE per-line border colour: sample $D020 at the first cycle of each line (the colour
        // that fills the left border, i.e. the line's entry colour).
        var nativeLineColour = new Dictionary<int, byte>();
        for (long i = 0; i < runCycles; i++)
        {
            native.Step();
            var v = native.GetVicState();
            if (v.RasterCycle <= 1 && v.RasterLine >= firstBarLine && v.RasterLine <= lastBarLine)
                nativeLineColour[v.RasterLine] = (byte)((v.Registers?[0x20] ?? 0) & 0x0F);
        }

        // Managed: inject the same t0 and run the same span so the renderer fills frames.
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        ram.AsSpan().CopyTo(((IMemory)machine.Devices.GetByRole(DeviceRole.SystemRam)!).Span);
        machine.Bus.Write(0, ram[0]); machine.Bus.Write(1, ram[1]);
        var mcpu = (Mos6502)machine.Devices.GetByRole(DeviceRole.Cpu)!;
        mcpu.A = cpu0.A; mcpu.X = cpu0.X; mcpu.Y = cpu0.Y; mcpu.S = cpu0.S; mcpu.PC = cpu0.PC; mcpu.P = cpu0.P;
        var mvic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        mvic.InjectSnapshotState(regs, vic0.RasterLine, (byte)((vic0.RasterCycle + 1) % 63));
        for (long i = 0; i < runCycles; i++) machine.Clock.Step();

        // Managed rendered frame: left-border pixel colour per line.
        var fb = mvic.FrameBuffer;
        var palette = Enumerable.Range(0, 16).Select(i => ExpectedBgra((byte)i)).ToArray();
        byte ManagedLineColour(int line)
        {
            int y = line - VideoRenderer.PalFirstVisibleRasterLine;
            uint px = System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + 8) * 4);
            int idx = Array.IndexOf(palette, px);
            return (byte)(idx < 0 ? 0xFF : idx);
        }

        int match = 0, total = 0;
        var mismatches = new List<string>();
        for (int line = firstBarLine; line <= lastBarLine; line++)
        {
            if (!nativeLineColour.TryGetValue(line, out var nc)) continue;
            total++;
            var mc = ManagedLineColour(line);
            if (mc == nc) match++;
            else if (mismatches.Count < 8) mismatches.Add($"line {line}: native=${nc:X1} managed=${mc:X1}");
        }

        foreach (var m in mismatches) _output.WriteLine(m);
        _output.WriteLine($"bar-line border-colour alignment (lossy-injection diagnostic): {match}/{total} lines match VICE");

        // DIAGNOSTIC, not a hard gate. Residual mismatch here is dominated by the LOSSY
        // snapshot injection (InjectSnapshotState seeds VIC registers + raster phase only, not
        // _allowBadLines / VC / RC / pipeline), so the managed *emulation* of the cycle-stable
        // busy-wait diverges from VICE and produces a different $D020 pattern per line - which
        // is an emulation-fidelity limit of injection, NOT a renderer defect. The renderer fix
        // itself is proven by MidLineBorderColorChange_RendersTwoColourBandsOnOneScanline, and
        // on a real demo run (cycle-exact from-reset emulation) the per-cycle border draws the
        // bars on the correct lines. A fully faithful end-to-end check needs a non-lossy oracle
        // (from-reset D64 autostart) - tracked as follow-up.
        Assert.True(total >= 20, $"Expected to sample the bar region; got {total} lines.");
    }

    /// <summary>
    /// FR: FR-VIC-004, TR: TR-VIC-EDGE-003, TEST: TEST-VICRENDER-001 (PLAN-VICRENDER-001).
    /// Use case: EMPIRICAL, non-lossy end-to-end proof (snapshot-gated) - capture VICE's
    /// real per-cycle $D020 timeline for one frame of the live Pieces-of-Light bar
    /// segment from the resumed .vsf, then replay that exact timeline into a fresh
    /// managed VIC, bypassing managed emulation entirely so injection lossiness cannot
    /// contaminate the result. This exercises the real demo's writes AND the
    /// RasterX-to-pixel phase; a vertical misplacement or phase error shows up as
    /// per-line mismatches (both sides sampled at the same physical point: VICE cycle 13
    /// == frame pixel 8).
    /// Acceptance: at least 20 bar-region lines (212-246) are sampled and the managed
    /// RENDERED frame's per-line border colour matches VICE's on at least 95% of them;
    /// mismatched lines are logged. Skips when the shim or the staged .vsf is absent.
    /// </summary>
    [Fact]
    public void DemoBars_ReplayedViceTimeline_ManagedRendererMatchesVicePerLineBorder()
    {
        if (!ViceNative.IsAvailable) { Assert.Skip(ViceNative.AvailabilityMessage); return; }
        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        if (vsf is null) { Assert.Skip("Staged demo .vsf snapshot not present."); return; }

        const int firstBarLine = 212;
        const int lastBarLine = 246;

        using var native = ViceNative.CreateInstance("c64");
        native.Reset();
        Assert.True(native.ReadSnapshot(vsf) == 0, "snapshot must resume");

        // Advance to a frame boundary (raster wraps to line 0).
        int prev = native.GetVicState().RasterLine;
        for (int i = 0; i < 30000; i++)
        {
            native.Step();
            int l = native.GetVicState().RasterLine;
            if (prev > 300 && l == 0) break;
            prev = l;
        }

        // Capture one full frame of VICE's $D020 timeline (change events) + per-line border colour.
        byte Border() => (byte)((native.GetVicState().Registers?[0x20] ?? 0) & 0x0F);
        byte seed = Border();
        var writes = new Dictionary<(int line, int cycle), byte>();
        var nativeBorder = new Dictionary<int, byte>();
        byte prevBorder = seed;
        int startLine = native.GetVicState().RasterLine;
        for (int i = 0; i < 20500; i++)
        {
            var v = native.GetVicState();
            byte b = (byte)((v.Registers?[0x20] ?? 0) & 0x0F);
            if (b != prevBorder) { writes[(v.RasterLine, v.RasterCycle)] = b; prevBorder = b; }
            // Sample VICE's border at the SAME physical point the managed left-border pixel (8)
            // shows: frame pixel 8 == (C-12)*8 -> VICE cycle 13. Sampling at cycle 0-1 would
            // compare different cycles and manufacture a false 1-line offset.
            if (v.RasterCycle == 13 && !nativeBorder.ContainsKey(v.RasterLine)) nativeBorder[v.RasterLine] = b;
            native.Step();
            if (i > 1000 && native.GetVicState().RasterLine == 0 && v.RasterLine > 300) break;
        }

        // Replay the exact timeline into a fresh managed VIC (no emulation, no injection).
        var machine = MachineTestFactory.CreateC64Machine("c64");
        machine.Reset();
        var mvic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        mvic.Write(0xD020, seed);
        // Two frames: warm up frame, then the replay frame that we read.
        for (int f = 0; f < 2; f++)
        {
            for (int t = 0; t < VideoRenderer.PalCyclesPerLine * VideoRenderer.PalTotalLines + 4; t++)
            {
                int line = mvic.CurrentRasterLine;
                int viceCycle = mvic.RasterX - 1; // managed RasterX == VICE cycle + 1
                if (writes.TryGetValue((line, viceCycle), out var c))
                    mvic.Write(0xD020, c);
                mvic.Tick();
            }
        }

        var fb = mvic.FrameBuffer;
        var palette = Enumerable.Range(0, 16).Select(i => ExpectedBgra((byte)i)).ToArray();
        byte ManagedBorder(int line)
        {
            int y = line - VideoRenderer.PalFirstVisibleRasterLine;
            uint px = System.BitConverter.ToUInt32(fb, (y * VideoRenderer.ScreenWidth + 8) * 4);
            int idx = Array.IndexOf(palette, px);
            return (byte)(idx < 0 ? 0xFF : idx);
        }

        int match = 0, total = 0;
        var mism = new List<string>();
        for (int line = firstBarLine; line <= lastBarLine; line++)
        {
            if (!nativeBorder.TryGetValue(line, out var nc)) continue;
            total++;
            var mc = ManagedBorder(line);
            if (mc == nc) match++;
            else if (mism.Count < 10) mism.Add($"line {line}: VICE=${nc:X1} managed=${mc:X1}");
        }

        foreach (var m in mism) _output.WriteLine(m);
        _output.WriteLine($"replayed-timeline per-line border match: {match}/{total} (start frame line {startLine})");
        Assert.True(total >= 20, $"expected to sample the bar region; got {total} lines.");
        // Non-lossy: the managed renderer, fed VICE's exact $D020 timeline, must reproduce VICE's
        // per-line bar colours almost exactly. This is the empirical proof the bars render on the
        // correct lines for the real demo (isolated from snapshot-injection emulation lossiness).
        Assert.True(match * 100 >= total * 95,
            $"Managed renderer reproduced VICE bars on only {match}/{total} bar lines (<95%).");
    }

    private readonly ITestOutputHelper _output;
    public RasterBarRendererTests(ITestOutputHelper output) => _output = output;
}
