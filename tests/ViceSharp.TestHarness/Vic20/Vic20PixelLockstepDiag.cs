namespace ViceSharp.TestHarness.Vic20;

using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>Diagnostic-only: dump index histograms for pixel lockstep debug. Not a gate.</summary>
[Collection("NativeVice")]
public sealed class Vic20PixelLockstepDiag
{
    [Fact]
    public void Dump_ReadyPal_IndexHistogram()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));

        const int boot = 2_000_000;
        var videoDiv = -1;
        for (var i = 1; i <= boot; i++)
        {
            native.Step();
            managed.Clock.Step();
            if (videoDiv < 0 && i % 10000 == 0)
            {
                var n = native.GetVic20VideoState();
                var m = vic.CaptureVideoLockstepState((uint)i);
                if (Vic20VideoLockstep.DescribeMismatch(n, m) is not null)
                    videoDiv = i;
            }
        }

        var nIdx = new byte[Vic20PixelLockstep.PalNormalWidth * Vic20PixelLockstep.PalNormalHeight];
        Assert.True(native.TryCaptureFrameIndices(nIdx, out var nW, out var nH));
        var mIdx = vic.IndexFrameBuffer;
        Assert.Equal(nW * nH, mIdx.Length);

        var nh = new int[16];
        var mh = new int[16];
        var mismatches = 0;
        var first = -1;
        for (var i = 0; i < nW * nH; i++)
        {
            if (nIdx[i] < 16) nh[nIdx[i]]++;
            if (mIdx[i] < 16) mh[mIdx[i]]++;
            if (nIdx[i] != mIdx[i])
            {
                mismatches++;
                if (first < 0) first = i;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"videoDivFirst={videoDiv} w={nW} h={nH} mismatches={mismatches} first={first}");
        if (first >= 0)
            sb.AppendLine($"first x={first % nW} y={first / nW} n=0x{nIdx[first]:X2} m=0x{mIdx[first]:X2}");
        sb.Append("nHist:");
        for (var c = 0; c < 16; c++)
            if (nh[c] > 0) sb.Append($" {c}={nh[c]}");
        sb.AppendLine();
        sb.Append("mHist:");
        for (var c = 0; c < 16; c++)
            if (mh[c] > 0) sb.Append($" {c}={mh[c]}");
        sb.AppendLine();
        // Sample mid row
        var mid = (nH / 2) * nW;
        sb.Append("midRow n:");
        for (var x = 0; x < nW; x += 32)
            sb.Append($" {nIdx[mid + x]:X1}");
        sb.AppendLine();
        sb.Append("midRow m:");
        for (var x = 0; x < nW; x += 32)
            sb.Append($" {mIdx[mid + x]:X1}");
        sb.AppendLine();
        // Top row
        sb.Append("topRow n:");
        for (var x = 0; x < nW; x += 32)
            sb.Append($" {nIdx[x]:X1}");
        sb.AppendLine();
        sb.Append("topRow m:");
        for (var x = 0; x < nW; x += 32)
            sb.Append($" {mIdx[x]:X1}");

        // Force visible failure with dump for logs
        Assert.True(mismatches == 0, sb.ToString());
    }
}
