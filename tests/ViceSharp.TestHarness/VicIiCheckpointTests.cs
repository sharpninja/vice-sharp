namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// BACKFILL-VIDEO-001 (primary) / TR-VIC-EDGE-001 / TR-VIC-EDGE-002 / TR-VIC-EDGE-005 / TR-VIC-EDGE-006 /
/// FR-VIC-001 / TEST-VIC-001.
///
/// Native visible-frame checkpoints (sub-slice 1C). Two checkpoint categories:
///
///   1. Managed-only: the managed PAL VIC-II raster engine advances exactly one full frame
///      (PalTotalLines * PalCyclesPerLine = 19,656 ticks) and returns to the same raster
///      position, proving the line-wrap and cycle counter are correct.
///
///   2. Native (gated): write a known byte pattern to screen-RAM rows 20-24 on a live x64sc
///      native machine, step one full PAL frame (19,656 cycles), and verify the pattern
///      is preserved. VIC-II matrix DMA (c-access) is read-only; it must never write back
///      to screen RAM. This validates TR-VIC-EDGE-005 (DMA read-only semantics).
///
/// VICE sources:
///   vicii-fetch.c:135-166 (matrix character-fetch c-access reads screen RAM, read-only),
///   vicii-cycle.c:541-548 (VC update, matrix DMA window 12..54 per bad line),
///   vicii-mem.c:48-63 (unused_bits table, register space),
///   vicii-draw-cycle.c:133-141 (display mode color table).
///
/// Safe region for screen RAM: rows 20-24 (bytes $07A0-$07E7 = last 200 bytes of screen RAM).
/// KERNAL places the READY cursor at row 5-6 and does not write to rows 20-24 within one frame.
///
/// Byrd Development Process: managed-only leg written first (always green, no native required).
/// Native legs are gated by [ViceFact] and skip when native VICE is absent.
/// </summary>
[Collection("NativeVice")]
public sealed class VicIiCheckpointTests
{
    // PAL frame constants.
    // One PAL frame = 312 lines x 63 cycles = 19,656 total cycles.
    private const int PalCyclesPerLine = 63;
    private const int PalTotalLines = 312;
    private const int PalCyclesPerFrame = PalCyclesPerLine * PalTotalLines; // 19 656

    // Screen RAM layout: $0400..$07E7 = 40 columns x 25 rows = 1000 bytes.
    private const ushort ScreenRamBase = 0x0400;
    private const int ScreenRamCols = 40;

    // Safe test region: rows 20-24 (offset 800..999) = $07A0..$07E7.
    // KERNAL READY cursor is at row 5-6 after init; rows 20-24 are left as spaces and
    // are not touched by the cursor-blink interrupt within one 19,656-cycle frame.
    private const int SafeRowStart = 20;
    private const int SafeRowCount = 5;
    private const ushort SafeRegionBase = (ushort)(ScreenRamBase + SafeRowStart * ScreenRamCols); // $07A0
    private const int SafeRegionSize = SafeRowCount * ScreenRamCols; // 200

    // Cycles to step past KERNAL init before writing test registers.
    // 100,000 cycles ~= 5 PAL frames; BASIC READY appears within ~3 frames.
    private const int KernalSettleCycles = 100_000;

    // -------------------------------------------------------------------------
    // Managed-only checkpoint (always runs; no native VICE required)
    // -------------------------------------------------------------------------

    /// <summary>
    /// FR-VIC-001 / TR-CYCLE-001 / BACKFILL-VIDEO-001.
    /// Use case: The managed PAL VIC-II raster engine must be frame-periodic.
    /// After advancing exactly one full PAL frame (19,656 ticks) from any starting
    /// raster position, the chip returns to the identical (rasterLine, rasterX) pair.
    /// Acceptance: rasterLine and rasterX after two-frame advance equal those after
    /// one-frame advance - the frame is a fixed period of 312 x 63 = 19,656 cycles.
    /// VICE vicii-cycle.c:576-598 (line-wrap and raster-line increment timing).
    /// </summary>
    [Fact]
    public void ManagedVic_OneFullPalFrame_RasterPositionIsFramePeriodic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);

        // Advance through one complete frame from reset to establish a baseline.
        for (var i = 0; i < PalCyclesPerFrame; i++)
            vic.Tick();

        ushort baselineLine = vic.CurrentRasterLine;
        byte baselineRasterX = vic.RasterX;

        // Advance exactly one more frame.
        for (var i = 0; i < PalCyclesPerFrame; i++)
            vic.Tick();

        Assert.Equal(baselineLine, vic.CurrentRasterLine);
        Assert.Equal(baselineRasterX, vic.RasterX);
    }

    /// <summary>
    /// FR-VIC-001 / TR-CYCLE-001 / BACKFILL-VIDEO-001.
    /// Use case: The managed PAL VIC-II cycle counter must advance monotonically
    /// by exactly PalCyclesPerFrame per frame.
    /// Acceptance: After one frame, CycleCounter increases by exactly 19,656.
    /// </summary>
    [Fact]
    public void ManagedVic_OneFullPalFrame_CycleCounterAdvancesByExactlyOnePalFrame()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);

        var before = vic.CycleCounter;
        for (var i = 0; i < PalCyclesPerFrame; i++)
            vic.Tick();

        Assert.Equal((ulong)(before + PalCyclesPerFrame), vic.CycleCounter);
    }

    // -------------------------------------------------------------------------
    // Native VICE checkpoints (gated - skip when native VICE absent)
    // -------------------------------------------------------------------------

    /// <summary>
    /// BACKFILL-VIDEO-001 / TR-VIC-EDGE-005 / FR-VIC-001 / TEST-VIC-001.
    /// Use case: The ViceNativeBridge ReadMemory/WriteMemory path for screen-RAM
    /// addresses ($0400-$07E7) must correctly round-trip byte values immediately
    /// after a write (no VIC or CPU side-effects in zero elapsed cycles).
    /// Acceptance: Writing a distinctive pattern to the safe region ($07A0-$07E7)
    /// and immediately reading it back yields the same 200 byte values.
    /// VICE vicii-fetch.c:135-166 (c-access reads, no write side-effect).
    /// </summary>
    [ViceFact]
    public void Native_ScreenRam_ImmediateReadBackMatchesWrite()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(native);

            // Step past KERNAL init.
            for (var i = 0; i < KernalSettleCycles; i++)
                ViceNativeBridge.StepCycle(native);

            // Write a distinctive non-space pattern to the safe screen-RAM region.
            var pattern = new byte[SafeRegionSize];
            for (var i = 0; i < SafeRegionSize; i++)
                pattern[i] = (byte)((i + 0xAA) & 0xFF);

            for (var i = 0; i < SafeRegionSize; i++)
                ViceNativeBridge.WriteMemory(native, (ushort)(SafeRegionBase + i), pattern[i]);

            // Read back immediately (zero elapsed cycles - only testing R/W path).
            for (var i = 0; i < SafeRegionSize; i++)
            {
                var actual = ViceNativeBridge.ReadMemory(native, (ushort)(SafeRegionBase + i));
                Assert.True(actual == pattern[i],
                    $"Screen RAM at ${SafeRegionBase + i:X4}: immediate read must return written value " +
                    $"0x{pattern[i]:X2} (ViceNativeBridge R/W path for address range $07A0-$07E7), " +
                    $"got 0x{actual:X2}.");
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 / TR-VIC-EDGE-005 / FR-VIC-001 / TEST-VIC-001.
    /// Use case: VIC-II matrix DMA (c-access, vicii-fetch.c:135-166) is read-only.
    /// The chip fetches character codes from screen RAM during each bad line but must
    /// not write back to it. After exactly one PAL frame (19,656 cycles = 312 x 63),
    /// screen-RAM bytes that the CPU did not touch must be unchanged.
    /// Acceptance: A 200-byte test pattern written to rows 20-24 ($07A0-$07E7) before
    /// the frame survives the frame unchanged. KERNAL cursor is at row 5-6 during
    /// BASIC READY and does not reach rows 20-24 within a single 19,656-cycle frame.
    /// VICE vicii-fetch.c:135-166, vicii-cycle.c:541-548.
    /// </summary>
    [ViceFact]
    public void Native_ScreenRamPattern_SurvivesOneFullPalFrame_VicDmaIsReadOnly()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(native);

            // Step past KERNAL init.
            for (var i = 0; i < KernalSettleCycles; i++)
                ViceNativeBridge.StepCycle(native);

            // Write a distinctive test pattern to the safe screen-RAM region.
            // The pattern uses i*3+0x55 mod 256 to differentiate all 200 positions.
            var pattern = new byte[SafeRegionSize];
            for (var i = 0; i < SafeRegionSize; i++)
                pattern[i] = (byte)((i * 3 + 0x55) & 0xFF);

            for (var i = 0; i < SafeRegionSize; i++)
                ViceNativeBridge.WriteMemory(native, (ushort)(SafeRegionBase + i), pattern[i]);

            // Step exactly one full PAL frame (19,656 cycles).
            // This exercises all 25 bad-line DMA windows (lines $30-$F7 with YSCROLL=0 default),
            // each reading 40 character codes from screen RAM via c-access.
            for (var i = 0; i < PalCyclesPerFrame; i++)
                ViceNativeBridge.StepCycle(native);

            // Read back and verify the pattern is unchanged.
            // Any mismatch proves a write to screen RAM by VIC DMA (which must not happen)
            // or unexpected CPU activity in rows 20-24 during BASIC idle state.
            for (var i = 0; i < SafeRegionSize; i++)
            {
                var actual = ViceNativeBridge.ReadMemory(native, (ushort)(SafeRegionBase + i));
                Assert.True(actual == pattern[i],
                    $"Screen RAM at ${SafeRegionBase + i:X4} must survive one PAL frame unchanged. " +
                    $"VIC-II matrix DMA is read-only (vicii-fetch.c:135-166); CPU cursor is at row 5-6 and " +
                    $"does not reach rows 20-24 in 19,656 cycles. Expected 0x{pattern[i]:X2}, got 0x{actual:X2}.");
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }
}
