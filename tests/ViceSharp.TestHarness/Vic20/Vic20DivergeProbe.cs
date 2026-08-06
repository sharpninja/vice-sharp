namespace ViceSharp.TestHarness.Vic20;

using System.Text;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Every-cycle CPU lockstep vs xvic (A/X/Y/S/P/PC) on the production create/step path.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20DivergeProbe
{
    /// <summary>~0.45s PAL machine time; keeps CI focused while proving past 5005/5023.</summary>
    public const int FocusedBudgetCycles = 500_000;

    /// <summary>~2s PAL machine time (2 * 1_108_405).</summary>
    public const int TwoSecondPalCycles = 2_216_810;

    /// <summary>~10s PAL machine time (10 * 1_108_405).</summary>
    public const int TenSecondPalCycles = 11_084_050;

    /// <summary>~10s NTSC machine time (10 * 1_022_727).</summary>
    public const int TenSecondNtscCycles = 10_227_270;

    /// <summary>
    /// Every-cycle CPU match for a focused multi-hundred-k window (past former 5005/5023 fails).
    /// Skips when oracle absent.
    /// </summary>
    [Fact]
    public void EveryCycle_CpuRegs_Match_FocusedWindow()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = RunEveryCycle(FocusedBudgetCycles);
        Assert.True(matched == FocusedBudgetCycles,
            $"expected full match for {FocusedBudgetCycles} cycles, matched={matched}");
    }

    /// <summary>
    /// Long every-cycle probe (~2s PAL). Skips unless VICESHARP_LOCKSTEP_2S=1
    /// so the default Vic20 filter stays fast.
    /// </summary>
    [Fact]
    public void EveryCycle_CpuRegs_Match_TwoSecondPal()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        if (!string.Equals(Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_2S"), "1", StringComparison.Ordinal))
            return;

        var matched = RunEveryCycle(TwoSecondPalCycles);
        Assert.True(matched == TwoSecondPalCycles,
            $"expected full match for {TwoSecondPalCycles} cycles, matched={matched}");
    }

    /// <summary>
    /// Long every-cycle probe (~10s PAL). Skips unless VICESHARP_LOCKSTEP_10S=1
    /// (wall ~30-90s; keep off default filters).
    /// </summary>
    [Fact]
    public void EveryCycle_CpuRegs_Match_TenSecondPal()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        if (!string.Equals(Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_10S"), "1", StringComparison.Ordinal))
            return;

        var matched = RunEveryCycle(TenSecondPalCycles, "vic20");
        Assert.True(matched == TenSecondPalCycles,
            $"expected full match for {TenSecondPalCycles} cycles, matched={matched}");
    }

    /// <summary>
    /// Long every-cycle probe (~10s NTSC). Skips unless VICESHARP_LOCKSTEP_10S=1.
    /// </summary>
    [Fact]
    public void EveryCycle_CpuRegs_Match_TenSecondNtsc()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;
        if (!string.Equals(Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_10S"), "1", StringComparison.Ordinal))
            return;

        var matched = RunEveryCycle(TenSecondNtscCycles, "vic20ntsc");
        Assert.True(matched == TenSecondNtscCycles,
            $"expected full match for {TenSecondNtscCycles} cycles (vic20ntsc), matched={matched}");
    }

    /// <summary>
    /// Steps managed Vic20 and native xvic together; returns matched cycle count.
    /// Throws with a ring dump on first A/X/Y/S/P/PC mismatch.
    /// </summary>
    public static int RunEveryCycle(int budget, string modelSelector = "vic20")
    {
        using var native = ViceNative.CreateInstance(modelSelector);
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine(modelSelector);
        managed.Reset();
        var cpu = managed.Devices.GetAll<Mos6502>().First();

        var log = new StringBuilder();
        const int pre = 12;
        var ring = new string[pre];
        var ringAt = 0;
        var ringCount = 0;

        for (var i = 1; i <= budget; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            var mismatch = n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P;
            var line =
                $"c={i} nPC=${n.PC:X4} mPC=${m.PC:X4} nA=${n.A:X2} mA=${m.A:X2} nX=${n.X:X2} mX=${m.X:X2} " +
                $"nY=${n.Y:X2} mY=${m.Y:X2} nS=${n.S:X2} mS=${m.S:X2} nP=${n.P:X2} mP=${m.P:X2} " +
                $"dbgCyc={cpu.DebugCycle} op=${cpu.DebugOpcode:X2} trail={cpu.DebugPriorTrailingAtNextPc} " +
                $"nonOvlR={cpu.DebugNonOverlappedRegion} nonOvlF={cpu.DebugNonOverlappedFetchPhase} " +
                $"dly={cpu.DebugDelayNextFetch} stg={cpu.DebugStagedMemoryReadCompleted} " +
                $"irq={cpu.DebugInterruptSequenceRemaining} mis={mismatch}";

            ring[ringAt] = line;
            ringAt = (ringAt + 1) % pre;
            if (ringCount < pre)
                ringCount++;

            // Optional checkpoint dump (VICESHARP_LOCKSTEP_DUMP=1316731,2055609;
            // VICESHARP_LOCKSTEP_DUMP_FILE=path).
            var dumpSpec = Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_DUMP");
            var dumpFile = Environment.GetEnvironmentVariable("VICESHARP_LOCKSTEP_DUMP_FILE");
            if (!string.IsNullOrEmpty(dumpSpec) && !string.IsNullOrEmpty(dumpFile))
            {
                foreach (var part in dumpSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(part, out var dumpAt) && dumpAt == i)
                    {
                        var bb = managed.Bus as BasicBus;
                        var vicChip = managed.Devices.GetAll<Mos6561>().FirstOrDefault();
                        var nv = native.GetVicState();
                        File.AppendAllText(dumpFile,
                            $"DUMP c={i} nA=${n.A:X2} mA=${m.A:X2} nP=${n.P:X2} mP=${m.P:X2} " +
                            $"mRast={vicChip?.CurrentRasterLine} nRast={nv.RasterLine} " +
                            $"mCyc={vicChip?.CycleInLine} nCyc={nv.RasterCycle} " +
                            $"mFetch={vicChip?.DebugVBusFetchState} nFetch={nv.IdleState} " +
                            $"m$9124=${managed.Bus.Peek(0x9124):X2} n$9124=${native.PeekBus(0x9124):X2} " +
                            $"m$9125=${managed.Bus.Peek(0x9125):X2} n$9125=${native.PeekBus(0x9125):X2} " +
                            $"m$912B=${managed.Bus.Peek(0x912B):X2} n$912B=${native.PeekBus(0x912B):X2} " +
                            $"m$912D=${managed.Bus.Peek(0x912D):X2} n$912D=${native.PeekBus(0x912D):X2} " +
                            $"m$912E=${managed.Bus.Peek(0x912E):X2} n$912E=${native.PeekBus(0x912E):X2} " +
                            $"mis={mismatch}{Environment.NewLine}");
                    }
                }
            }

            if (!mismatch)
                continue;

            var start = (ringAt - ringCount + pre) % pre;
            for (var k = 0; k < ringCount; k++)
                log.AppendLine(ring[(start + k) % pre]);

            // Extra bus evidence for load/store diverges.
            var op = cpu.DebugOpcode;
            var opAddr = cpu.DebugOpcodeAddress;
            ushort absAddr = 0;
            if (op is 0xAD or 0xAE or 0xAC or 0x8D or 0x8E or 0x8C or 0xEE or 0xCE
                or 0xBD or 0xB9 or 0xBC or 0xBE)
            {
                var lo = managed.Bus.Peek((ushort)(opAddr + 1));
                var hi = managed.Bus.Peek((ushort)(opAddr + 2));
                absAddr = (ushort)(lo | (hi << 8));
                if (op is 0xBD or 0xBC)
                    absAddr = (ushort)(absAddr + m.X);
                else if (op is 0xB9 or 0xBE)
                    absAddr = (ushort)(absAddr + m.Y);
            }

            log.AppendLine(
                $"bus: m$C1=${managed.Bus.Peek(0xC1):X2} m$C2=${managed.Bus.Peek(0xC2):X2} " +
                $"n$C1=${native.PeekBus(0xC1):X2} n$C2=${native.PeekBus(0xC2):X2} " +
                $"m$C3=${managed.Bus.Peek(0xC3):X2} m$C4=${managed.Bus.Peek(0xC4):X2} " +
                $"n$C3=${native.PeekBus(0xC3):X2} n$C4=${native.PeekBus(0xC4):X2} " +
                $"m$0314=${managed.Bus.Peek(0x0314):X2} n$0314=${native.PeekBus(0x0314):X2}" +
                (absAddr != 0
                    ? $" opAddr=${opAddr:X4} abs=${absAddr:X4} m[${absAddr:X4}]=${managed.Bus.Peek(absAddr):X2} n[${absAddr:X4}]=${native.PeekBus(absAddr):X2}"
                    : string.Empty));

            // VICE last_opcode_info / IRQ delay evidence (c=577679 early IRQ).
            var pipe = native.GetCpuPipelineState();
            const uint delaysInterruptMsk = 1u << 8;
            log.AppendLine(
                $"irqdiag: nLastOp=0x{pipe.LastOpcodeInfo:X} nDelays={((pipe.LastOpcodeInfo & delaysInterruptMsk) != 0)} " +
                $"nPend=0x{pipe.GlobalPendingInt:X} nIrqClk={pipe.IrqClk} nClk={pipe.Clk} " +
                $"mDelays={cpu.LastOpcodeDelaysInterrupt} mBound={cpu.IsInstructionBoundary} " +
                $"mIrqSeq={cpu.DebugInterruptSequenceRemaining}");

            // LDA (zp),Y effective-address evidence when A mismatches.
            if (op == 0xB1)
            {
                var zp = managed.Bus.Peek((ushort)(opAddr + 1));
                var ptrLo = managed.Bus.Peek(zp);
                var ptrHi = managed.Bus.Peek((byte)(zp + 1));
                var eff = (ushort)((ptrLo | (ptrHi << 8)) + m.Y);
                var nPtrLo = native.PeekBus(zp);
                var nPtrHi = native.PeekBus((byte)(zp + 1));
                var nEff = (ushort)((nPtrLo | (nPtrHi << 8)) + n.Y);
                var mVBus = managed.Bus is BasicBus bb ? bb.VBusLastData : (byte)0;
                var vic = managed.Devices.GetAll<Mos6561>().FirstOrDefault();
                log.AppendLine(
                    $"indy: zp=${zp:X2} mPtr=${ptrHi:X2}{ptrLo:X2} nPtr=${nPtrHi:X2}{nPtrLo:X2} " +
                    $"mEff=${eff:X4}=${managed.Bus.Peek(eff):X2} nEff=${nEff:X4}=${native.PeekBus(nEff):X2} " +
                    $"mVBus=${mVBus:X2} mRast={vic?.CurrentRasterLine} mCycLine={vic?.CycleInLine} " +
                    $"mFetch={vic?.DebugVBusFetchState} mBuf={vic?.DebugVBusBufOffset} " +
                    $"n$9000=${native.PeekBus(0x9000):X2} m$9000=${managed.Bus.Peek(0x9000):X2} " +
                    $"n$9001=${native.PeekBus(0x9001):X2} m$9001=${managed.Bus.Peek(0x9001):X2} " +
                    $"n$9002=${native.PeekBus(0x9002):X2} m$9002=${managed.Bus.Peek(0x9002):X2} " +
                    $"n$9003=${native.PeekBus(0x9003):X2} n$9004=${native.PeekBus(0x9004):X2} " +
                    $"m$9003=${managed.Bus.Peek(0x9003):X2} m$9004=${managed.Bus.Peek(0x9004):X2}");
            }

            throw new Xunit.Sdk.XunitException(
                $"DIV first={i} matchedCycles={i - 1} budget={budget}\n{log}");
        }

        return budget;
    }
}
