using System.Collections.Generic;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// FIX-ROMLESSVIC-001. The "ROM-less" display path: the host parks the 6510 in a RAM
/// <c>JMP *</c> loop (no KERNAL) and drives the VIC-II directly. The VIC-II is pure
/// managed C# and needs no ROMs to rasterise a bitmap, so a host that sets DEN and
/// programs bitmap mode must get real content.
/// </summary>
/// <remarks>
/// These tests assert the VICE-CORRECT, requirement-derived observables, established
/// against the native x64sc oracle (vice_x64.dll) for this exact scenario:
///   * Bad-line firing is bit-exact with VICE. Native x64sc (KERNAL text, yscroll 3)
///     and the managed parked-CPU machine (host-written DEN, yscroll 3) raise an
///     IDENTICAL bad-line set: 25 lines, rasters 51..243 step 8, allow_bad_lines armed
///     in both. The managed VIC does NOT diverge from VICE, so the allow_bad_lines
///     $30 latch is deliberately left unchanged (Mos6569.cs:1297-1299 / 3101-3102).
///   * <see cref="Mos6569.BadLineCountThisFrame"/> is a PER-FRAME counter reset at
///     start-of-frame (Mos6569.cs:1526). A full <see cref="IMachine.RunFrame"/> steps
///     exactly one PAL frame and lands on that reset, so reading the counter AFTER
///     RunFrame always yields 0 even though 25 bad lines fired during the frame. The
///     bad-line test therefore samples DURING the frame; the render test asserts the
///     real user-visible deliverable (content, not background-only), which is immune to
///     the frame boundary.
///   * A ROM-less machine also resets the VIC bank to 3; the bitmap at $2000 lives in
///     bank 0, so the bank must be programmed with a CPU store to $DD00 (a raw bus
///     write does not move it). The render test performs that companion setup exactly
///     as the real host (CbmEngine BuildRomless) does.
/// </remarks>
public sealed class RomlessBadLineTests
{
    /// <summary>
    /// FR: FR-VIC-CYCLE (allow_bad_lines armed by DEN at raster $30, CPU-independent;
    /// VICE viciisc/vicii-cycle.c:523-526). TR: TR-CYCLE-001.
    /// Use case: the host parks the CPU and sets DEN + bitmap mode by hand; the VIC must
    /// still raise bad lines so the c-access loads the video matrix / colour.
    /// Acceptance: sampled DURING the frame (not at the start-of-frame counter reset that
    /// a full RunFrame lands on), the parked-CPU machine has raised bad lines
    /// (BadLineCountThisFrame &gt; 0) - matching native x64sc, which raises 25 for yscroll 3.
    /// </summary>
    [Fact]
    public void ParkedCpu_HostDrivenDen_RaisesBadLines()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();

        var cpu = (Mos6502)machine.Devices.GetByRole(DeviceRole.Cpu)!;

        // Park the 6510 in a RAM JMP* loop so the KERNAL never runs and can't program the VIC.
        machine.Bus.Write(0xC000, 0x4C);
        machine.Bus.Write(0xC001, 0x00);
        machine.Bus.Write(0xC002, 0xC0);
        cpu.PC = 0xC000;

        // Host-driven full-screen multicolour bitmap with the display enabled.
        machine.Bus.Write(0xD011, 0x3B); // DEN + BMM, yscroll 3
        machine.Bus.Write(0xD016, 0x18); // MCM
        machine.Bus.Write(0xD018, 0x18); // screen $0400, bitmap $2000

        // Advance ~half a frame so the raster is mid-screen and several bad lines have
        // fired, WITHOUT crossing the frame boundary (which would reset the per-frame
        // counter). 10000 host cycles from the reset phase lands around raster 158.
        machine.Clock.Step(10000);

        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        Assert.True(
            vic.BadLineCountThisFrame > 0,
            $"No bad lines fired with a parked CPU and DEN set: badLines={vic.BadLineCountThisFrame}, " +
            $"displayEnabled={vic.IsDisplayEnabled}, raster={vic.CurrentRasterLine}, yscroll={vic.YScroll}.");
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-GFX (multicolour-bitmap g-access rendering from the bad-line
    /// c-access matrix/colour load). TR: TR-CYCLE-001.
    /// Use case: a ROM-less host programs the VIC bank, DEN + multicolour bitmap mode,
    /// and fills screen/bitmap/colour RAM; the emulated frame must show the real
    /// 00/01/10/11 multicolour pairs as distinct colours, not background-only (black).
    /// Acceptance: after the frame renders, the visible framebuffer contains at least 3
    /// distinct colours (the host cell resolves to background 0 plus colours 6, 7, 10).
    /// </summary>
    [Fact]
    public void RomlessMulticolourBitmap_RendersDistinctContent_NotJustBackground()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Reset();

        var cpu = (Mos6502)machine.Devices.GetByRole(DeviceRole.Cpu)!;

        // Processor port: BASIC/KERNAL blank (ROM-less), I/O banked in.
        machine.Bus.Write(0x0000, 0x2F);
        machine.Bus.Write(0x0001, 0x37);

        // Multicolour bitmap content: screen nibbles 6/7 (pairs 01/10), colour RAM 10
        // (pair 11), background 0 (pair 00). Bitmap 0x1B = %00 01 10 11 across the cell.
        for (ushort a = 0x0400; a < 0x07E8; a++) machine.Bus.Write(a, 0x67);
        for (ushort a = 0xD800; a < 0xDBE8; a++) machine.Bus.Write(a, 0x0A);
        for (int a = 0x2000; a < 0x3F40; a++) machine.Bus.Write((ushort)a, 0x1B);
        machine.Bus.Write(0xD021, 0x00);

        // CPU stub at $C000: set the VIC bank to 0 (a raw bus write does NOT move it -
        // the CIA2 port output only changes on a CPU store), program DEN + multicolour
        // bitmap and the memory pointers, then park. Mirrors CbmEngine BuildRomless.
        byte[] stub =
        [
            0xA9, 0x3F, 0x8D, 0x02, 0xDD, // LDA #$3F ; STA $DD02  (CIA2 port A outputs)
            0xA9, 0x03, 0x8D, 0x00, 0xDD, // LDA #$03 ; STA $DD00  (VIC bank 0)
            0xA9, 0x3B, 0x8D, 0x11, 0xD0, // LDA #$3B ; STA $D011  (DEN + BMM, yscroll 3)
            0xA9, 0x18, 0x8D, 0x16, 0xD0, // LDA #$18 ; STA $D016  (MCM)
            0xA9, 0x18, 0x8D, 0x18, 0xD0, // LDA #$18 ; STA $D018  (screen $0400, bitmap $2000)
            0x4C, 0x19, 0xC0,             // JMP $C019             (park on self)
        ];
        for (int i = 0; i < stub.Length; i++) machine.Bus.Write((ushort)(0xC000 + i), stub[i]);
        cpu.PC = 0xC000;

        for (int frame = 0; frame < 6; frame++) machine.RunFrame();

        var vic = (Mos6569)machine.Devices.GetByRole(DeviceRole.VideoChip)!;
        var frameBuffer = vic.FrameBuffer;
        var distinctColours = new HashSet<uint>();
        for (int p = 0; p < frameBuffer.Length; p += 4)
        {
            uint bgra = frameBuffer[p]
                | ((uint)frameBuffer[p + 1] << 8)
                | ((uint)frameBuffer[p + 2] << 16)
                | ((uint)frameBuffer[p + 3] << 24);
            distinctColours.Add(bgra);
        }

        Assert.True(
            distinctColours.Count >= 3,
            $"ROM-less multicolour bitmap rendered background-only: distinctColours={distinctColours.Count} " +
            $"({string.Join(",", distinctColours.OrderBy(c => c).Select(c => "0x" + c.ToString("X8")))}).");
    }
}
