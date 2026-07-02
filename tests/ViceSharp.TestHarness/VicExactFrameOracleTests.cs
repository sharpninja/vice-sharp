namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 Phase 0 (P0-2) / TR-VIC-ORACLE-001.
/// Self-proof suite for the per-pixel VIC frame oracle. VICE's viciisc core
/// draws 8 palette-indexed pixels per cycle into vicii.dbuf
/// (vicii-draw-cycle.c) and flushes each line into the raster draw buffer
/// (vicii_raster_draw_handler -> raster_line_emulate -> draw_dummy). The
/// oracle copies that draw buffer's visible window, so VIC pixel parity ACs
/// can assert index-exact colour identity against real VICE output instead of
/// the previous sentinel/synthetic capture path.
/// </summary>
[Collection("NativeVice")]
public sealed class VicExactFrameOracleTests
{
    private const int PalCyclesPerLine = 63;
    private const int PalLinesPerFrame = 312;
    private const int PalCyclesPerFrame = PalCyclesPerLine * PalLinesPerFrame;
    private const int VisibleWidth = 384;
    private const int VisibleHeight = 272;

    private static string? LocateFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Vsf", name);
        return File.Exists(path) ? path : null;
    }

    private static IntPtr CreateReadyMachine()
    {
        var vsfPath = LocateFixture("ready-c64sc-truedrive.vsf");
        if (vsfPath is null)
            Assert.Skip("External x64sc .vsf fixture not present.");

        var machine = ViceNativeBridge.CreateMachine("c64");
        var rc = ViceNative.ReadSnapshotNative(machine, vsfPath);
        if (rc != 0)
        {
            ViceNativeBridge.DestroyMachine(machine);
            Assert.Skip($"READY snapshot failed to resume (rc={rc}); frame oracle needs a booted machine.");
        }

        // Two full PAL frames so every visible line has been drawn by the
        // per-cycle pipeline since resume.
        for (int i = 0; i < 2 * PalCyclesPerFrame; i++)
            ViceNativeBridge.StepCycle(machine);

        return machine;
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-GFX, TR: TR-VIC-ORACLE-001, TEST: TEST-VIC-ORACLE-P0-01.
    /// Use case: VIC pixel parity ACs compare managed output against VICE's
    /// real rendered frame; the oracle must report the true visible geometry
    /// and raw palette indices from the raster draw buffer.
    /// Acceptance: after resuming the READY snapshot and drawing two full PAL
    /// frames, the index capture reports 384x272, every byte is a valid VIC
    /// palette index (&lt;= 0x0F), the top-left border pixel equals the live
    /// $D020 low nibble, and the frame contains more than one distinct index
    /// (border + screen background at READY).
    /// </summary>
    [ViceFact]
    public void CaptureIndices_ReadyScreen_GeometryAndIndexDomainExact()
    {
        var machine = CreateReadyMachine();
        try
        {
            var indices = new byte[VisibleWidth * VisibleHeight];
            Assert.True(
                ViceNativeBridge.TryCaptureVicFrameIndices(machine, indices, out var width, out var height),
                "index capture failed");
            Assert.Equal(VisibleWidth, width);
            Assert.Equal(VisibleHeight, height);

            Assert.All(indices, index => Assert.True(index <= 0x0F, $"index {index:X2} outside VIC palette"));

            var vicState = new ViceNative.ViceVicState();
            ViceNative.GetVicState(machine, ref vicState);
            byte borderIndex = (byte)(vicState.GetRegisters()[0x20] & 0x0F);
            Assert.Equal(borderIndex, indices[0]);

            Assert.True(indices.Distinct().Count() > 1, "frame is a single flat colour; draw buffer not populated");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(machine);
        }
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-GFX, TR: TR-VIC-ORACLE-001, TEST: TEST-VIC-ORACLE-P0-02.
    /// Use case: parity ACs treat the oracle as a pure function of machine
    /// state; identical resume + cycle programs must produce identical frames.
    /// Acceptance: two independent machines resumed from the same READY
    /// snapshot and stepped the same two PAL frames produce byte-identical
    /// index captures (delta 0 across all 104448 pixels).
    /// </summary>
    [ViceFact]
    public void CaptureIndices_Deterministic_AcrossInstances()
    {
        static byte[] Capture()
        {
            var machine = CreateReadyMachine();
            try
            {
                var indices = new byte[VisibleWidth * VisibleHeight];
                Assert.True(
                    ViceNativeBridge.TryCaptureVicFrameIndices(machine, indices, out _, out _),
                    "index capture failed");
                return indices;
            }
            finally
            {
                ViceNativeBridge.DestroyMachine(machine);
            }
        }

        var first = Capture();
        var second = Capture();
        Assert.True(first.AsSpan().SequenceEqual(second), "frame capture diverged between identical runs");
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-COLOR, TR: TR-VIC-ORACLE-001, TEST: TEST-VIC-ORACLE-P0-03.
    /// Use case: the BGRA visible-frame capture backs presentation-level
    /// comparisons and must now carry real rendered pixels instead of the
    /// previous 0xCC sentinel fill.
    /// Acceptance: the full 384x272 BGRA capture is not the sentinel pattern,
    /// every alpha byte is 0xFF, and colour identity follows the index
    /// capture: pixels with equal indices have equal BGRA values and pixels
    /// with different indices have different BGRA values.
    /// </summary>
    [ViceFact]
    public void CaptureVisibleFrame_RealPath_MatchesIndexIdentity()
    {
        var machine = CreateReadyMachine();
        try
        {
            var indices = new byte[VisibleWidth * VisibleHeight];
            Assert.True(
                ViceNativeBridge.TryCaptureVicFrameIndices(machine, indices, out _, out _),
                "index capture failed");

            var bgra = new byte[VisibleWidth * VisibleHeight * 4];
            Assert.True(
                ViceNativeBridge.TryCaptureVicVisibleFrame(machine, bgra, out var width, out var height),
                "BGRA capture failed");
            Assert.Equal(VisibleWidth, width);
            Assert.Equal(VisibleHeight, height);

            Assert.False(
                bgra[0] == 0xCC && bgra[1] == 0xCC && bgra[2] == 0xCC,
                "BGRA capture still returns the sentinel fill");

            var colourByIndex = new Dictionary<byte, (byte B, byte G, byte R)>();
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.Equal(0xFF, bgra[(i * 4) + 3]);
                var colour = (bgra[i * 4], bgra[(i * 4) + 1], bgra[(i * 4) + 2]);
                if (colourByIndex.TryGetValue(indices[i], out var seen))
                {
                    Assert.True(seen == colour, $"index {indices[i]:X2} mapped to two BGRA colours at pixel {i}");
                }
                else
                {
                    colourByIndex[indices[i]] = colour;
                }
            }

            Assert.Equal(colourByIndex.Count, colourByIndex.Values.Distinct().Count());
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(machine);
        }
    }
}
