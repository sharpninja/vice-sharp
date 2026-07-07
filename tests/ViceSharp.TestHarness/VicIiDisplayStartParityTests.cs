namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 1+2 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md findings H1/H2/H3/M5/M6):
/// display-start draw/fetch alignment on the C64 boot screen.
/// VICE viciisc references: vicii-draw-cycle.c:672-688 (cycle_flags_pipe,
/// dbuf_offset reset at raster_cycle 1), vicii-cycle.c:572-591
/// (prefetch_cycles BA countdown), vicii-fetch.c:192-201 (matrix fetch
/// garbage only while prefetch_cycles != 0).
/// </summary>
public sealed class VicIiDisplayStartParityTests
{
    private const int MaxBootFrames = 250;
    private const byte BootBackgroundIndex = 6;  // $D021 = blue
    private const byte BootBorderIndex = 14;     // $D020 = light blue

    private static Mos6569 GetVic(IMachine machine)
        => Assert.IsAssignableFrom<Mos6569>(machine.Devices.GetByRole(DeviceRole.VideoChip));

    /// <summary>
    /// Boots a fresh C64 machine until the READY prompt appears in screen RAM,
    /// then runs two more frames so the frame buffer holds a settled boot frame.
    /// Returns the machine.
    /// </summary>
    private static IMachine BootToReadyPlusTwoFrames()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var booted = false;
        for (var frame = 0; frame < MaxBootFrames; frame++)
        {
            machine.RunFrame();
            if (ContainsReadyPrompt(machine))
            {
                booted = true;
                break;
            }
        }

        Assert.True(booted, $"READY prompt not found within {MaxBootFrames} frames.");
        machine.RunFrame();
        machine.RunFrame();
        return machine;
    }

    private static bool ContainsReadyPrompt(IMachine machine)
    {
        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        var buffer = new byte[1000];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = machine.Bus.Peek((ushort)(0x0400 + i));
        return buffer.AsSpan().IndexOf(screenCodeReady) >= 0;
    }

    /// <summary>
    /// Reverse-maps the BGRA frame buffer to VIC palette indices using the
    /// same packing as VideoRenderer (B,G,R,A byte order, A=0xFF).
    /// </summary>
    private static byte[] FrameToIndices(Mos6569 vic)
    {
        var frame = vic.FrameBuffer;
        var width = vic.FrameWidth;
        var height = vic.FrameHeight;
        var map = new Dictionary<uint, byte>(16);
        for (byte i = 0; i < 16; i++)
        {
            var c = VicPalette.Colors[i];
            map[0xFF000000u | c.B | ((uint)c.G << 8) | ((uint)c.R << 16)] = i;
        }

        var indices = new byte[width * height];
        for (var p = 0; p < indices.Length; p++)
        {
            uint bgra = frame[p * 4]
                | ((uint)frame[(p * 4) + 1] << 8)
                | ((uint)frame[(p * 4) + 2] << 16)
                | ((uint)frame[(p * 4) + 3] << 24);
            Assert.True(map.TryGetValue(bgra, out var index),
                $"pixel {p % width},{p / width} BGRA 0x{bgra:X8} is not a VIC palette colour");
            indices[p] = index;
        }

        return indices;
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-GFX, TR: TR-VIC-BOOTSTART-001, TEST: TEST-VIC-BOOTSTART-01.
    /// Use case: the C64 boot screen uses only $D021 blue (index 6) and
    /// $D020/text light blue (index 14); the display-start defect (missing
    /// cycle_flags_pipe delay, vicii-draw-cycle.c:679/:687, plus the
    /// prefetch_cycles c-access garbage, vicii-fetch.c:194-200) injects a
    /// black/light-grey checkerboard at the first display columns.
    /// Acceptance: every pixel of a settled boot frame decodes to palette
    /// index 6 or 14; any other index is display-start garbage.
    /// </summary>
    [Fact]
    public void C64_Boot_Frame_Uses_Only_Boot_Palette_Indices()
    {
        var machine = BootToReadyPlusTwoFrames();
        var vic = GetVic(machine);
        var indices = FrameToIndices(vic);
        var width = vic.FrameWidth;

        var offenders = new List<string>();
        for (var p = 0; p < indices.Length && offenders.Count < 24; p++)
        {
            if (indices[p] != BootBackgroundIndex && indices[p] != BootBorderIndex)
                offenders.Add($"({p % width},{p / width})={indices[p]}");
        }

        Assert.True(offenders.Count == 0,
            $"boot frame contains non-boot palette indices (first {offenders.Count}): {string.Join(" ", offenders)}");
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-GFX, TR: TR-VIC-BOOTSTART-001, TEST: TEST-VIC-BOOTSTART-02.
    /// Use case: with CSEL=1 the PAL left border in the 384px visible window is
    /// exactly 32 pixels (visible cycles 12-15; display render starts at
    /// cycle 16 via the one-cycle-delayed pipe0 load, vicii-draw-cycle.c:275-294
    /// with cycle_flags_pipe :679/:687 and the dbuf_offset reset at
    /// raster_cycle 1 :675-677). The managed defect renders a 48px border.
    /// Acceptance: on the first visible row containing display pixels, columns
    /// 0-31 are border colour and column 32 is the display background.
    /// </summary>
    [Fact]
    public void C64_Boot_Frame_Left_Border_Is_Exactly_32_Pixels()
    {
        var machine = BootToReadyPlusTwoFrames();
        var vic = GetVic(machine);
        var indices = FrameToIndices(vic);
        var width = vic.FrameWidth;
        var height = vic.FrameHeight;

        var displayRow = -1;
        for (var y = 0; y < height && displayRow < 0; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (indices[(y * width) + x] != BootBorderIndex)
                {
                    displayRow = y;
                    break;
                }
            }
        }

        Assert.True(displayRow >= 0, "no display row found; frame is all border");

        var row = indices.AsSpan(displayRow * width, width);
        for (var x = 0; x < 32; x++)
        {
            Assert.True(row[x] == BootBorderIndex,
                $"row {displayRow} x={x}: expected border index {BootBorderIndex}, got {row[x]}");
        }

        Assert.True(row[32] == BootBackgroundIndex,
            $"row {displayRow} x=32: expected background index {BootBackgroundIndex} (display start), got {row[32]}");
    }

    /// <summary>
    /// FR: FR-VIC-FETCH, TR: TR-VIC-BOOTSTART-002, TEST: TEST-VIC-BOOTSTART-03.
    /// Use case: on a standard bad line BA falls at cycle 12 and the first
    /// c-access runs at cycle 15, so VICE's prefetch_cycles counter
    /// (vicii-cycle.c:580-591, reset to 3+1 while BA is high, decremented each
    /// BA-low cycle) reaches zero exactly at the first c-access and ALL 40
    /// matrix fetches read real screen data (vicii-fetch.c:192-201). The
    /// managed slot&lt;3 heuristic corrupts vbuf[0..2] with 0xFF on every bad line.
    /// Acceptance: after a settled boot frame the last fetched matrix row
    /// (a blank screen row) holds screen code 0x20 in slots 0-2, not 0xFF.
    /// </summary>
    [Fact]
    public void C64_Boot_Badline_CAccess_Fetches_Real_Matrix_Data_In_First_Three_Slots()
    {
        var machine = BootToReadyPlusTwoFrames();
        var vic = GetVic(machine);

        for (var slot = 0; slot < 3; slot++)
        {
            var value = vic.PeekVideoMatrixLatch(slot);
            Assert.True(value == 0x20,
                $"vbuf[{slot}] = 0x{value:X2}; expected screen code 0x20 (blank) - 0xFF means the prefetch garbage heuristic fired on a standard bad line");
        }
    }
}
