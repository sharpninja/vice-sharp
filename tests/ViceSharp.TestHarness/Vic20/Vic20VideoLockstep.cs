namespace ViceSharp.TestHarness.Vic20;

using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Every-cycle VIC-I video pipeline lockstep vs xvic (same discipline as CPU
/// A/X/Y/S/P/PC): after each phi2, compare raster, area, fetch, memptr, cbuf/gbuf,
/// and raw $9000-$900F stores. CPU lockstep is not video proof; this is.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20VideoLockstep
{
    /// <summary>~0.45s PAL; past power-on into KERNAL video programming.</summary>
    public const int FocusedBudgetCycles = 500_000;

    [Fact]
    public void EveryCycle_VicI_VideoState_Match_FocusedWindow()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = RunEveryCycleVideo(FocusedBudgetCycles);
        Assert.True(matched == FocusedBudgetCycles,
            $"expected full video match for {FocusedBudgetCycles} cycles, matched={matched}");
    }

    /// <summary>
    /// Short smoke: video state advances and stays bit-equal for 2k cycles.
    /// </summary>
    [Fact]
    public void EveryCycle_VicI_VideoState_Match_2k()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = RunEveryCycleVideo(2000);
        Assert.Equal(2000, matched);
    }

    /// <summary>TEST-VIC20-VS-04 / AC-VS-04: NTSC every-cycle video state 2k.</summary>
    [Fact]
    public void EveryCycle_VicI_VideoState_Match_2k_Ntsc()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = RunEveryCycleVideo(2000, "vic20ntsc");
        Assert.Equal(2000, matched);
    }

    /// <summary>AC-VS-04: NTSC focused window (500k) every-cycle video state.</summary>
    [Fact]
    public void EveryCycle_VicI_VideoState_Match_FocusedWindow_Ntsc()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = RunEveryCycleVideo(FocusedBudgetCycles, "vic20ntsc");
        Assert.True(matched == FocusedBudgetCycles,
            $"expected full NTSC video match for {FocusedBudgetCycles} cycles, matched={matched}");
    }

    /// <summary>
    /// Steps managed + xvic; returns matched cycles. Throws on first video mismatch.
    /// </summary>
    public static int RunEveryCycleVideo(int budget, string modelSelector = "vic20")
    {
        using var native = ViceNative.CreateInstance(modelSelector);
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine(modelSelector);
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));

        var log = new StringBuilder();
        const int pre = 8;
        var ring = new string[pre];
        var ringAt = 0;
        var ringCount = 0;

        for (var i = 1; i <= budget; i++)
        {
            native.Step();
            managed.Clock.Step();

            var n = native.GetVic20VideoState();
            var m = vic.CaptureVideoLockstepState((uint)i);
            var detail = DescribeMismatch(n, m);
            var line =
                $"c={i} nR={n.RasterLine} mR={m.RasterLine} nCyc={n.RasterCycle} mCyc={m.RasterCycle} " +
                $"nA={n.Area} mA={m.Area} nF={n.FetchState} mF={m.FetchState} " +
                $"nMem={n.Memptr} mMem={m.Memptr} nY={n.YCounter} mY={m.YCounter} " +
                $"nRow={n.RowCounter} mRow={m.RowCounter} nCols={n.TextCols} mCols={m.TextCols} " +
                $"nBlank={n.BlankThisLine} mBlank={m.BlankThisLine} mis={detail is not null}";

            ring[ringAt] = line;
            ringAt = (ringAt + 1) % pre;
            if (ringCount < pre)
                ringCount++;

            if (detail is null)
                continue;

            var start = (ringAt - ringCount + pre) % pre;
            for (var k = 0; k < ringCount; k++)
                log.AppendLine(ring[(start + k) % pre]);
            log.AppendLine(detail);
            log.AppendLine(FormatRegs("nRegs", n.Regs));
            log.AppendLine(FormatRegs("mRegs", m.Regs));
            // Bus-visible $9003/$9004 (raster encode) — part of video output surface.
            log.AppendLine(
                $"peek $9003 n=${native.PeekBus(0x9003):X2} m=${managed.Bus.Peek(0x9003):X2} " +
                $"$9004 n=${native.PeekBus(0x9004):X2} m=${managed.Bus.Peek(0x9004):X2}");

            throw new Xunit.Sdk.XunitException(
                $"VIDEO DIV first={i} matchedCycles={i - 1} budget={budget}\n{log}");
        }

        return budget;
    }

    /// <summary>
    /// Compare native vs managed VIC-I pipeline. Returns null if equal.
    /// </summary>
    public static string? DescribeMismatch(Vic20VideoLockstepState n, Vic20VideoLockstepState m)
    {
        if (n.RasterLine != m.RasterLine)
            return $"RasterLine n={n.RasterLine} m={m.RasterLine}";
        if (n.RasterCycle != m.RasterCycle)
            return $"RasterCycle n={n.RasterCycle} m={m.RasterCycle}";
        if (n.Area != m.Area)
            return $"Area n={n.Area} m={m.Area}";
        if (n.FetchState != m.FetchState)
            return $"FetchState n={n.FetchState} m={m.FetchState}";
        if (n.TextCols != m.TextCols)
            return $"TextCols n={n.TextCols} m={m.TextCols}";
        if (n.TextLines != m.TextLines)
            return $"TextLines n={n.TextLines} m={m.TextLines}";
        if (n.YCounter != m.YCounter)
            return $"YCounter n={n.YCounter} m={m.YCounter}";
        if (n.RowCounter != m.RowCounter)
            return $"RowCounter n={n.RowCounter} m={m.RowCounter}";
        if (n.BlankThisLine != m.BlankThisLine)
            return $"BlankThisLine n={n.BlankThisLine} m={m.BlankThisLine}";
        if (n.LineWasBlank != m.LineWasBlank)
            return $"LineWasBlank n={n.LineWasBlank} m={m.LineWasBlank}";
        if (n.CharHeight != m.CharHeight && n.CharHeight != 0)
            return $"CharHeight n={n.CharHeight} m={m.CharHeight}";
        if (n.Memptr != m.Memptr)
            return $"Memptr n={n.Memptr} m={m.Memptr}";
        if (n.MemptrInc != m.MemptrInc)
            return $"MemptrInc n={n.MemptrInc} m={m.MemptrInc}";

        if (n.Regs is not null && m.Regs is not null)
        {
            for (var i = 0; i < 16; i++)
            {
                if (n.Regs[i] != m.Regs[i])
                    return $"Regs[{i:X}] n=${n.Regs[i]:X2} m=${m.Regs[i]:X2}";
            }
        }

        // Only compare active columns when text is on; stale tail bytes may differ.
        var cols = Math.Min(n.TextCols, (byte)32);
        if (n.Cbuf is not null && m.Cbuf is not null && cols > 0 && n.BlankThisLine == 0)
        {
            for (var i = 0; i < cols; i++)
            {
                if (n.Cbuf[i] != m.Cbuf[i])
                    return $"Cbuf[{i}] n=${n.Cbuf[i]:X2} m=${m.Cbuf[i]:X2}";
            }
        }

        if (n.Gbuf is not null && m.Gbuf is not null && cols > 0 && n.BlankThisLine == 0)
        {
            for (var i = 0; i < cols; i++)
            {
                if (n.Gbuf[i] != m.Gbuf[i])
                    return $"Gbuf[{i}] n=${n.Gbuf[i]:X2} m=${m.Gbuf[i]:X2}";
            }
        }

        return null;
    }

    private static string FormatRegs(string label, byte[]? regs)
    {
        if (regs is null || regs.Length < 16)
            return $"{label}=null";
        var sb = new StringBuilder(label);
        sb.Append('=');
        for (var i = 0; i < 16; i++)
            sb.Append($" {i:X}={regs[i]:X2}");
        return sb.ToString();
    }
}
