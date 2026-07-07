namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 1+2 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md H1/H2/H3/M5/M6): full-frame
/// index-exact parity of the managed boot screen against the VICE x64sc
/// oracle. Both machines run from power-on for the same number of cycles, so
/// screen content and cursor phase are identical; the visible 384x272 frames
/// must match byte-for-byte as palette indices.
/// </summary>
[Collection("NativeVice")]
public sealed class VicIiBootFrameOracleParityTests
{
    private const int PalCyclesPerLine = 63;
    private const int PalLinesPerFrame = 312;
    private const int PalCyclesPerFrame = PalCyclesPerLine * PalLinesPerFrame;
    private const int VisibleWidth = 384;
    private const int VisibleHeight = 272;
    private const int MaxBootFrames = 250;

    /// <summary>
    /// FR: FR-VIC-DRAW-GFX, TR: TR-VIC-BOOTSTART-001, TEST: TEST-VIC-BOOTSTART-04.
    /// Use case: the per-cycle draw pipeline (cycle_flags_pipe one-cycle flag
    /// delay, vicii-draw-cycle.c:679/:687; dbuf_offset reset at raster_cycle 1,
    /// :675-677; prefetch_cycles BA countdown, vicii-cycle.c:580-591) must make
    /// the managed visible frame bit-identical to VICE's rendered frame on the
    /// boot screen, the baseline every C64 program starts from.
    /// Acceptance: after booting managed and native machines from power-on for
    /// the same frame count, all 384x272 visible palette indices are equal
    /// (zero mismatches).
    /// </summary>
    [ViceFact]
    public void Boot_Frame_Indices_Match_Vice_Oracle_Bit_Exact()
    {
        // Managed: boot to READY + 3 settle frames.
        var machine = MachineTestFactory.CreateC64Machine();
        var readyFrame = -1;
        for (var frame = 1; frame <= MaxBootFrames; frame++)
        {
            machine.RunFrame();
            if (ContainsReadyPrompt(machine))
            {
                readyFrame = frame;
                break;
            }
        }

        Assert.True(readyFrame > 0, $"managed machine did not reach READY within {MaxBootFrames} frames");

        var totalFrames = readyFrame + 3;
        for (var frame = readyFrame; frame < totalFrames; frame++)
            machine.RunFrame();

        var vic = Assert.IsAssignableFrom<Mos6569>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        var managedIndices = FrameToIndices(vic);

        // Native: same frame count from power-on, plus one line so the last
        // raster line has been flushed by vicii_raster_draw_handler.
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            var cycles = (totalFrames * PalCyclesPerFrame) + PalCyclesPerLine;
            for (var i = 0; i < cycles; i++)
                ViceNativeBridge.StepCycle(native);

            var nativeIndices = new byte[VisibleWidth * VisibleHeight];
            Assert.True(
                ViceNativeBridge.TryCaptureVicFrameIndices(native, nativeIndices, out var width, out var height),
                "native index capture failed");
            Assert.Equal(VisibleWidth, width);
            Assert.Equal(VisibleHeight, height);

            var mismatches = 0;
            var samples = new List<string>();
            for (var p = 0; p < nativeIndices.Length; p++)
            {
                if (managedIndices[p] != nativeIndices[p])
                {
                    mismatches++;
                    if (samples.Count < 24)
                        samples.Add($"({p % VisibleWidth},{p / VisibleWidth}) managed={managedIndices[p]} vice={nativeIndices[p]}");
                }
            }

            Assert.True(mismatches == 0,
                $"boot frame diverges from VICE oracle: {mismatches} mismatching pixels of {nativeIndices.Length}. First: {string.Join(" ", samples)}");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    private static bool ContainsReadyPrompt(IMachine machine)
    {
        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        var buffer = new byte[1000];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = machine.Bus.Peek((ushort)(0x0400 + i));
        return buffer.AsSpan().IndexOf(screenCodeReady) >= 0;
    }

    private static byte[] FrameToIndices(Mos6569 vic)
    {
        var frame = vic.FrameBuffer;
        var map = new Dictionary<uint, byte>(16);
        for (byte i = 0; i < 16; i++)
        {
            var c = VicPalette.Colors[i];
            map[0xFF000000u | c.B | ((uint)c.G << 8) | ((uint)c.R << 16)] = i;
        }

        var indices = new byte[vic.FrameWidth * vic.FrameHeight];
        for (var p = 0; p < indices.Length; p++)
        {
            uint bgra = frame[p * 4]
                | ((uint)frame[(p * 4) + 1] << 8)
                | ((uint)frame[(p * 4) + 2] << 16)
                | ((uint)frame[(p * 4) + 3] << 24);
            Assert.True(map.TryGetValue(bgra, out var index),
                $"pixel {p % vic.FrameWidth},{p / vic.FrameWidth} BGRA 0x{bgra:X8} is not a VIC palette colour");
            indices[p] = index;
        }

        return indices;
    }
}
