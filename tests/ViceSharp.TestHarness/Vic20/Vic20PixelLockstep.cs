namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC20-001 / TR-VIC20-PIXEL-001: palette-index then BGRA framebuffer
/// SequenceEqual managed vs xvic. Sync: identical step count after reset.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20PixelLockstep
{
    public const int PalNormalWidth = 448;
    public const int PalNormalHeight = 284;
    public const int BootCycles = 2_000_000;

    /// <summary>TEST-VIC20-001 / AC-PX-02: READY PAL index SequenceEqual.</summary>
    [Fact]
    public void Index_ReadyPal_SequenceEqual()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        AssertIndexEqual("vic20", PalNormalWidth, PalNormalHeight, BootCycles, busy: false);
    }

    /// <summary>AC-PX-03: READY NTSC index SequenceEqual.</summary>
    [Fact]
    public void Index_ReadyNtsc_SequenceEqual()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        // NTSC normal canvas from vic-timing (display_width*PIXEL_WIDTH x lines).
        using var native = ViceNative.CreateInstance("vic20ntsc");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20ntsc");
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));
        for (var i = 0; i < BootCycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var nIdx = new byte[vic.FrameWidth * vic.FrameHeight + 64];
        Assert.True(native.TryCaptureFrameIndices(nIdx, out var nW, out var nH));
        Assert.Equal(vic.FrameWidth, nW);
        Assert.Equal(vic.FrameHeight, nH);
        AssertEqualBuffers(nIdx, vic.IndexFrameBuffer, nW, nH, "NTSC READY index");
    }

    /// <summary>AC-PX-04: busy screen PAL — border color reg poke then index equal.</summary>
    [Fact]
    public void Index_BusyPal_SequenceEqual()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        AssertIndexEqual("vic20", PalNormalWidth, PalNormalHeight, BootCycles, busy: true);
    }

    /// <summary>
    /// AC-PX-05: BGRA READY PAL. Native
    /// <see cref="IViceNative.TryCaptureVisibleFrame"/> BGRA must SequenceEqual
    /// managed FrameBuffer after index SequenceEqual. Canvas RGB must match
    /// xvic (no tautology expand through the managed table).
    /// Use case: READY PAL pixel lockstep.
    /// Acceptance: Failed=0 for this fact when xvic is available.
    /// </summary>
    [Fact]
    public void Bgra_ReadyPal_SequenceEqual()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));
        for (var i = 0; i < BootCycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var nIdx = new byte[PalNormalWidth * PalNormalHeight];
        Assert.True(native.TryCaptureFrameIndices(nIdx, out var nW, out var nH));
        Assert.Equal(PalNormalWidth, nW);
        Assert.Equal(PalNormalHeight, nH);
        AssertEqualBuffers(nIdx, vic.IndexFrameBuffer, nW, nH, "PAL READY index before BGRA");

        // Real native BGRA path (not expand-indices tautology).
        var nBgra = new byte[PalNormalWidth * PalNormalHeight * 4];
        Assert.True(native.TryCaptureVisibleFrame(nBgra, out var bw, out var bh));
        Assert.Equal(PalNormalWidth, bw);
        Assert.Equal(PalNormalHeight, bh);

        var mBgra = vic.FrameBuffer;
        var len = bw * bh * 4;
        Assert.True(mBgra.Length >= len);

        // Alpha must be 0xFF on native capture.
        for (var i = 3; i < len; i += 4)
            Assert.Equal(0xFF, nBgra[i]);

        if (nBgra.AsSpan(0, len).SequenceEqual(mBgra.AsSpan(0, len)))
            return;

        var first = -1;
        for (var i = 0; i < len; i++)
        {
            if (nBgra[i] != mBgra[i])
            {
                first = i;
                break;
            }
        }

        var dump = new System.Text.StringBuilder();
        var seen = new bool[16];
        var pixels = nW * nH;
        for (var p = 0; p < pixels; p++)
        {
            var idx = nIdx[p] & 0x0F;
            if (seen[idx])
                continue;
            seen[idx] = true;
            var o = p * 4;
            dump.Append(
                $" idx{idx}: nB={nBgra[o]:X2} nG={nBgra[o + 1]:X2} nR={nBgra[o + 2]:X2}" +
                $" mB={mBgra[o]:X2} mG={mBgra[o + 1]:X2} mR={mBgra[o + 2]:X2}");
        }

        var px = first / 4;
        throw new Xunit.Sdk.XunitException(
            $"Native BGRA SequenceEqual failed first={first} (x={px % bw},y={px / bw}) " +
            $"n=0x{nBgra[first]:X2} m=0x{mBgra[first]:X2}.{dump}");
    }

    /// <summary>
    /// AC-PX-05 busy: native BGRA SequenceEqual after a deterministic $900F poke.
    /// Use case: busy PAL pixel lockstep.
    /// Acceptance: Failed=0 when xvic is available.
    /// </summary>
    [Fact]
    public void Bgra_BusyPal_SequenceEqual()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));
        for (var i = 0; i < BootCycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        const byte regF = 0x25; // bg=2, border=5
        native.WriteBus(0x900F, regF);
        managed.Bus.Write(0x900F, regF);
        for (var i = 0; i < 71 * 312; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var nIdx = new byte[PalNormalWidth * PalNormalHeight];
        Assert.True(native.TryCaptureFrameIndices(nIdx, out var nW, out var nH));
        AssertEqualBuffers(nIdx, vic.IndexFrameBuffer, nW, nH, "PAL busy index before BGRA");

        var nBgra = new byte[PalNormalWidth * PalNormalHeight * 4];
        Assert.True(native.TryCaptureVisibleFrame(nBgra, out var bw, out var bh));
        var mBgra = vic.FrameBuffer;
        var len = bw * bh * 4;
        if (nBgra.AsSpan(0, len).SequenceEqual(mBgra.AsSpan(0, len)))
            return;

        var first = -1;
        for (var i = 0; i < len; i++)
        {
            if (nBgra[i] != mBgra[i])
            {
                first = i;
                break;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"Busy native BGRA mismatch first={first} n=0x{nBgra[first]:X2} m=0x{mBgra[first]:X2}");
    }

    /// <summary>
    /// AC-PX-05 NTSC: native BGRA SequenceEqual on vic20ntsc READY canvas.
    /// Use case: READY NTSC pixel lockstep.
    /// Acceptance: Failed=0 when xvic is available.
    /// </summary>
    [Fact]
    public void Bgra_ReadyNtsc_SequenceEqual()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20ntsc");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20ntsc");
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));
        for (var i = 0; i < BootCycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var w = vic.FrameWidth;
        var h = vic.FrameHeight;
        var nIdx = new byte[w * h + 64];
        Assert.True(native.TryCaptureFrameIndices(nIdx, out var nW, out var nH));
        Assert.Equal(w, nW);
        Assert.Equal(h, nH);
        AssertEqualBuffers(nIdx, vic.IndexFrameBuffer, nW, nH, "NTSC READY index before BGRA");

        var nBgra = new byte[w * h * 4];
        Assert.True(native.TryCaptureVisibleFrame(nBgra, out var bw, out var bh));
        Assert.Equal(w, bw);
        Assert.Equal(h, bh);
        var mBgra = vic.FrameBuffer;
        var len = bw * bh * 4;
        for (var i = 3; i < len; i += 4)
            Assert.Equal(0xFF, nBgra[i]);
        if (nBgra.AsSpan(0, len).SequenceEqual(mBgra.AsSpan(0, len)))
            return;

        var first = -1;
        for (var i = 0; i < len; i++)
        {
            if (nBgra[i] != mBgra[i])
            {
                first = i;
                break;
            }
        }

        var dump = new System.Text.StringBuilder();
        var seen = new bool[16];
        for (var p = 0; p < nW * nH; p++)
        {
            var idx = nIdx[p] & 0x0F;
            if (seen[idx])
                continue;
            seen[idx] = true;
            var o = p * 4;
            dump.Append(
                $" idx{idx}: nRGB=({nBgra[o + 2]:X2},{nBgra[o + 1]:X2},{nBgra[o]:X2})" +
                $" mRGB=({mBgra[o + 2]:X2},{mBgra[o + 1]:X2},{mBgra[o]:X2})");
        }

        throw new Xunit.Sdk.XunitException(
            $"NTSC native BGRA mismatch first={first} n=0x{nBgra[first]:X2} m=0x{mBgra[first]:X2}.{dump}");
    }

    private static void AssertIndexEqual(string model, int expectW, int expectH, int bootCycles, bool busy)
    {
        using var native = ViceNative.CreateInstance(model);
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine(model);
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));

        for (var i = 0; i < bootCycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        if (busy)
        {
            // Deterministic busy: same $900F poke on both machines, then one PAL frame.
            const byte regF = 0x25; // bg=2, border=5
            native.WriteBus(0x900F, regF);
            managed.Bus.Write(0x900F, regF);
            for (var i = 0; i < 71 * 312; i++)
            {
                native.Step();
                managed.Clock.Step();
            }
        }

        var nIdx = new byte[expectW * expectH];
        Assert.True(native.TryCaptureFrameIndices(nIdx, out var nW, out var nH), "index capture failed");
        Assert.Equal(expectW, nW);
        Assert.Equal(expectH, nH);
        Assert.Equal(nW * nH, vic.IndexFrameBuffer.Length);
        AssertEqualBuffers(nIdx, vic.IndexFrameBuffer, nW, nH, busy ? "PAL busy index" : "PAL READY index");
    }

    private static void AssertEqualBuffers(byte[] native, byte[] managed, int w, int h, string label)
    {
        var len = w * h;
        if (native.AsSpan(0, len).SequenceEqual(managed.AsSpan(0, len)))
            return;

        var first = -1;
        for (var i = 0; i < len; i++)
        {
            if (native[i] != managed[i])
            {
                first = i;
                break;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"{label} mismatch first={first} (x={first % w},y={first / w}) " +
            $"n=0x{native[first]:X2} m=0x{managed[first]:X2} w={w} h={h}");
    }
}
