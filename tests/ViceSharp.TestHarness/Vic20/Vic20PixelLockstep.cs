namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC20-001 / TR-VIC20-PIXEL-001 / TEST-VIC20-001: palette-index framebuffer
/// SequenceEqual managed vs xvic (primary Exact path before BGRA).
/// Use case: READY idle PAL full-frame index lockstep after synchronized boot.
/// Acceptance: AC-PX-02 — one full frame after READY-stable cycles, indices equal.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20PixelLockstep
{
    public const int PalNormalWidth = 448;
    public const int PalNormalHeight = 284;
    public const int BootCycles = 2_000_000;

    /// <summary>
    /// TEST-VIC20-001 / AC-PX-02: READY PAL index SequenceEqual.
    /// Sync: identical step count after reset on both machines.
    /// </summary>
    [Fact]
    public void Index_ReadyPal_SequenceEqual()
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
        Assert.True(
            native.TryCaptureFrameIndices(nIdx, out var nW, out var nH),
            "xvic index capture failed");
        Assert.Equal(PalNormalWidth, nW);
        Assert.Equal(PalNormalHeight, nH);
        Assert.Equal(nW, vic.FrameWidth);
        Assert.Equal(nH, vic.FrameHeight);
        Assert.Equal(nW * nH, vic.IndexFrameBuffer.Length);

        var mIdx = vic.IndexFrameBuffer;
        if (!nIdx.AsSpan(0, nW * nH).SequenceEqual(mIdx.AsSpan(0, nW * nH)))
        {
            var first = -1;
            for (var i = 0; i < nW * nH; i++)
            {
                if (nIdx[i] != mIdx[i])
                {
                    first = i;
                    break;
                }
            }

            var y = first / nW;
            var x = first % nW;
            throw new Xunit.Sdk.XunitException(
                $"Index mismatch first={first} (x={x},y={y}) n=0x{nIdx[first]:X2} m=0x{mIdx[first]:X2} " +
                $"w={nW} h={nH} bootCycles={BootCycles}");
        }
    }
}
