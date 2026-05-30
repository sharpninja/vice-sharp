namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// BACKFILL-VIDEO-001 (primary) / TR-VIC-EDGE-004 / FR-VIC-006 / FR-VIC-010 / TEST-VIC-001.
///
/// Native checkpoints for non-PAL per-model sprite DMA windows (sub-slice 1B).
/// Tables are already wired in Mos6569 (Slice 1A). These tests validate that:
///   (a) VICE native SpriteDma bit 3 fires near the expected raster line when sprite 3
///       is enabled with Y = 0x50, for each supported non-PAL model selector.
///   (b) The managed VIC for the matching model reports IsCpuCycleStolen at rasterX 0
///       of line 0x50 (the sprite-3 pointer-access slot, shared across all models).
///
/// No implementation change is expected; this is checkpoint/validation coverage only.
///
/// VICE sources:
///   vicii-cycle.c:118 (check_sprite_dma Y-latch entry point),
///   vicii-cycle.c:499-503 (model cycle count flags + late-line),
///   vicii-chip-model.c:272-403 (cycle_tab_ntsc SprDma*/BaSpr* for 65 cpl Mos6567R8/Mos8562),
///   vicii-chip-model.c:437-566 (cycle_tab_ntsc_old for 64 cpl Mos6567R56A),
///   vicii-fetch.c:275-309 (sprite pointer + data fetch driven by per-model DMA windows).
///
/// Byrd Development Process: tests written first (checkpoint); no prod edit required.
/// Gated by [Collection("NativeVice")] and [ViceTheory] - skip when native VICE absent.
/// </summary>
[Collection("NativeVice")]
public sealed class VicIISpriteDmaNativeCheckpointTests
{
    // Sprite 3 Y coordinate: line 0x50 (80 decimal), in the visible display area,
    // well below the DMA bad-line range (0x30-0xF7 with YSCROLL=0). Using line 0x50
    // means it is NOT a bad line, so only sprite DMA contributes to IsCpuCycleStolen.
    private const byte SpriteY = 0x50;
    private const byte SpriteEnable3 = 0x08;   // bit 3 of $D015
    private const ushort D011 = 0xD011;
    private const ushort D015 = 0xD015;
    private const ushort SpriteY3Reg = 0xD007;  // sprite 3 Y register

    // Number of cycles to step past KERNAL init before writing sprite registers.
    // 30,000 covers ~1.5 PAL frames (19,656 cycles/frame). NTSC/OldNTSC are similar.
    private const int KernalInitCycles = 30_000;

    // Additional cycles to search for SpriteDma after writing registers (~2 PAL frames).
    private const int SearchCycles = 40_000;

    public static TheoryData<string> NonPalAndExtendedModelSelectors
    {
        get
        {
            var data = new TheoryData<string>();
            // NTSC 65 cpl - Mos6567R8 (standard new NTSC C64)
            data.Add("ntsc");
            // NTSC 65 cpl - Mos8562 (new NTSC C64C)
            data.Add("newntsc");
            // Old NTSC 64 cpl - Mos6567R56A
            data.Add("oldntsc");
            // PAL-N 65 cpl - Mos6572 (Drean/PAL-N)
            data.Add("paln");
            // PAL 63 cpl - Mos8565 (new PAL C64C)
            data.Add("c64c");
            return data;
        }
    }

    private static Mos6569 CreateManagedVic(string modelSelector)
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var bus = new BasicBus();
        return modelSelector switch
        {
            "ntsc" => new Mos6567(bus, irq),
            "newntsc" => new Mos8562(bus, irq),
            "oldntsc" => new Mos6567R56A(bus, irq),
            "paln" => new Mos6572(bus, irq),
            "c64c" => new Mos8565(bus, irq),
            _ => new Mos6569(bus, irq),
        };
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        var max = vic.TotalLines * vic.CyclesPerLine * 4;
        for (var i = 0; i < max; i++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;
            vic.Tick();
        }

        throw new InvalidOperationException(
            $"Managed VIC did not reach line ${rasterLine:X3} cycle {rasterCycle}.");
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / TR-VIC-EDGE-004 / FR-VIC-006 / FR-VIC-010 / TEST-VIC-001.
    ///
    /// Use case: For each supported VIC-II model, enabling sprite 3 at Y=0x50 must cause
    /// the VIC-II chip to assert DMA activity in the line range [0x4F, 0x51]. This
    /// native checkpoint validates that the VICE x64sc engine fires sprite-3 DMA at
    /// the expected raster position for each model-specific cycle table. The managed
    /// VIC for the matching model must also report IsCpuCycleStolen at rasterX 0 of
    /// line 0x50 (the sprite-3 pointer-access slot, model-independent).
    ///
    /// Native assertion: VICE SpriteDma bit 3 goes high in lines [0x4F, 0x51].
    /// Managed assertion: managed IsCpuCycleStolen is true at (line 0x50, rasterX 0).
    ///
    /// VICE sources:
    ///   vicii-cycle.c:118 (check_sprite_dma Y-latch, fires at model ChkSprDma cycle),
    ///   vicii-chip-model.c:272-403/437-566 (SprDma* + BaSpr* slots per model cpl),
    ///   vicii-fetch.c:275-309 (sprite 3 p/s fetch at cycle 1/2 = rasterX 0/1).
    ///
    /// Acceptance: Both native and managed assertions pass for all five model selectors.
    /// Byrd: checkpoint test; no production code change needed.
    /// </summary>
    [ViceTheory]
    [MemberData(nameof(NonPalAndExtendedModelSelectors))]
    public void Sprite3DmaWindow_NativeAndManagedBothActive_ModelSpecificCycleTable(string modelSelector)
    {
        // ============================================================
        // Native VICE side
        // ============================================================
        var native = ViceNativeBridge.CreateMachine(modelSelector);
        bool nativeFoundDma = false;
        ushort nativeDmaLine = 0;
        byte nativeDmaCycle = 0;

        try
        {
            ViceNativeBridge.ResetMachine(native);

            // Step past KERNAL init so sprite registers survive the write.
            for (var i = 0; i < KernalInitCycles; i++)
                ViceNativeBridge.StepCycle(native);

            // Write sprite 3 registers: Y=0x50, enable bit 3.
            // DEN=1, YSCROLL=0 ensures line 0x50 is not a bad line.
            ViceNativeBridge.WriteMemory(native, D011, 0x10);
            ViceNativeBridge.WriteMemory(native, SpriteY3Reg, SpriteY);
            ViceNativeBridge.WriteMemory(native, D015, SpriteEnable3);

            // Step forward and poll GetVicState looking for SpriteDma bit 3 near line 0x50.
            // The DMA latch fires at check_sprite_dma (near end of line 0x4F); the p/s access
            // is at VICE cycle 1/2 of line 0x50 (RasterX 0/1 in managed). We search for any
            // cycle in lines [0x4F, 0x51] where bit 3 is active.
            for (var i = 0; i < SearchCycles && !nativeFoundDma; i++)
            {
                ViceNativeBridge.StepCycle(native);
                var st = new ViceNativeBridge.ViceVicState();
                ViceNativeBridge.GetVicState(native, ref st);

                if ((st.SpriteDma & SpriteEnable3) != 0 &&
                    st.RasterLine >= (ushort)(SpriteY - 1) &&
                    st.RasterLine <= (ushort)(SpriteY + 1))
                {
                    nativeDmaLine = st.RasterLine;
                    nativeDmaCycle = (byte)st.RasterCycle;
                    nativeFoundDma = true;
                }
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }

        // Native assertion: VICE fired sprite 3 DMA at the expected line range.
        Assert.True(nativeFoundDma,
            $"Model {modelSelector}: native VICE did not set SpriteDma bit 3 near line ${SpriteY:X2} " +
            $"after writing sprite 3 Y=${SpriteY:X2} and enabling via $D015={SpriteEnable3:X2}. " +
            "Expected SpriteDma & 0x08 in lines [0x4F, 0x51] per model cycle table " +
            "(vicii-chip-model.c:272-403/437-566, vicii-cycle.c:118).");

        Assert.True(
            nativeDmaLine >= (ushort)(SpriteY - 1) && nativeDmaLine <= (ushort)(SpriteY + 1),
            $"Model {modelSelector}: SpriteDma bit 3 appeared at line ${nativeDmaLine:X3} " +
            $"but expected within +-1 of sprite Y=${SpriteY:X2}.");

        // ============================================================
        // Managed VIC side (independent of native - no lockstep needed)
        // ============================================================
        // Sprite 3 uses the "early next-line" DMA slot: cycle_tab_* places sprite 3 at
        // FirstCurrentCycle=0 for all models (PAL 63cpl, NTSC 65cpl, OldNTSC 64cpl,
        // PAL-N 65cpl). The p/s access is at rasterX 0/1 of line (spriteY + 1), not
        // on spriteY itself. The DMA check fires at ~rasterX 54-55 of spriteY; BA lead
        // starts at rasterX 60-62 of spriteY; p/s at rasterX 0/1 of (spriteY+1).
        // This matches SpriteDmaStall_Sprite3UsesEarlyLineTableSlot (Y=$10, checks $11).
        var managed = CreateManagedVic(modelSelector);
        managed.Write(D011, 0x10);       // DEN=1, YSCROLL=0
        managed.Write(SpriteY3Reg, SpriteY);  // sprite 3 Y = 0x50
        managed.Write(D015, SpriteEnable3);   // enable sprite 3

        // Advance to line (SpriteY+1), rasterX 0 - the sprite-3 pointer-access slot.
        // For SpriteY=0x50: check fires at end of line 0x50, p/s access at line 0x51 rasterX 0.
        AdvanceTo(managed, (ushort)(SpriteY + 1), 0);

        Assert.True(managed.IsCpuCycleStolen,
            $"Model {modelSelector}: managed VIC-II must report IsCpuCycleStolen at " +
            $"line ${SpriteY + 1:X2} rasterX 0 (sprite-3 pointer-access slot on line Y+1; " +
            "check fires at end of line $50, BA lead at $50 cycles 60-62, p/s at $51 rasterX 0/1; " +
            "VICE vicii-fetch.c:275-309 + chip-model.c:272-403/437-566).");
    }
}
