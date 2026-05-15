using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// Lockstep cycle accurate validator comparing ViceSharp against original VICE
/// </summary>
public sealed class LockstepValidator : IDisposable
{
    private readonly IMachine _machine;
    private readonly IViceNative _native;
    private readonly Queue<string> _recentTrace = new();
    private long _cycleCount;

    public LockstepValidator(
        string modelSelector = "c64",
        byte[]? cartridgeImage = null,
        CartridgeMappingMode cartridgeMappingMode = CartridgeMappingMode.Auto)
    {
        _machine = MachineTestFactory.CreateC64Machine(modelSelector);
        _native = ViceNative.CreateInstance(modelSelector);

        if (cartridgeImage is not null)
        {
            var cartridgePort = _machine.Devices.GetAll<ICartridgePort>().SingleOrDefault()
                ?? throw new InvalidOperationException($"Machine '{modelSelector}' does not expose a cartridge port.");

            cartridgePort.AttachCartridge(cartridgeImage, cartridgeMappingMode);
            _native.AttachCartridge(cartridgeImage, cartridgeMappingMode);
        }
    }

    /// <summary>
    /// Run lockstep comparison for specified number of cycles
    /// </summary>
    public ValidationReport Run(long maxCycles)
    {
        _machine.Reset();
        _native.Reset();
        _recentTrace.Clear();
        RecordTrace(0);

        if (!ValidateState())
            return ValidationReport.Fail(0, 0, GetStateDiff());

        for (_cycleCount = 0; _cycleCount < maxCycles; _cycleCount++)
        {
            _machine.Clock.Step();
            _native.Step();
            RecordTrace(_cycleCount + 1);

            if (!ValidateState())
            {
                var mismatchCycle = _cycleCount + 1;
                return ValidationReport.Fail(mismatchCycle, mismatchCycle, GetStateDiff());
            }
        }

        return ValidationReport.Pass(maxCycles);
    }

    public string FormatRamtasDiagnostic()
    {
        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory memory)
            return "RAMTAS diagnostic unavailable: managed system RAM is not exposed as IMemory.";

        var managedState = _machine.GetState();
        var nativeState = _native.GetState();
        var ram = memory.Span;
        var managedPointer = ReadPointer(ram[0x00C1], ram[0x00C2], managedState.Y);
        var nativePointer = ReadPointer(_native.PeekRam(0x00C1), _native.PeekRam(0x00C2), nativeState.Y);

        return
            $"RAMTAS managed C1=${ram[0x00C1]:X2}, C2=${ram[0x00C2]:X2}, Y=${managedState.Y:X2}, " +
            $"EA=${managedPointer:X4}, RAM=${ram[managedPointer]:X2}; " +
            $"native C1=${_native.PeekRam(0x00C1):X2}, C2=${_native.PeekRam(0x00C2):X2}, Y=${nativeState.Y:X2}, " +
            $"EA=${nativePointer:X4}, RAM=${_native.PeekRam(nativePointer):X2}.";
    }

    public string FormatRecentTrace()
    {
        return _recentTrace.Count == 0
            ? "No recent trace captured."
            : string.Join(Environment.NewLine, _recentTrace);
    }

    private bool ValidateState()
    {
        var managedState = _machine.GetState();
        var nativeState = _native.GetState();
        var comparePc = _cycleCount > 1;

        return
            managedState.A == nativeState.A &&
            managedState.X == nativeState.X &&
            managedState.Y == nativeState.Y &&
            managedState.S == nativeState.S &&
            managedState.P == nativeState.P &&
            managedState.Cycle == nativeState.Cycle &&
            (!comparePc || managedState.PC == nativeState.PC);
    }

    private StateDiff GetStateDiff()
    {
        return new StateDiff
        {
            Cycle = _cycleCount,
            Expected = _native.GetState(),
            Actual = _machine.GetState()
        };
    }

    public void Dispose()
    {
        _native.Dispose();
    }

    private void RecordTrace(long cycle)
    {
        var managedState = _machine.GetState();
        var nativeState = _native.GetState();
        var vicTrace = FormatVicTrace();
        var cpuTrace = FormatCpuTrace();
        if (_recentTrace.Count == 16)
            _recentTrace.Dequeue();

        _recentTrace.Enqueue(
            $"cycle {cycle}: managed PC=${managedState.PC:X4} A=${managedState.A:X2} X=${managedState.X:X2} Y=${managedState.Y:X2} S=${managedState.S:X2} P=${managedState.P:X2}; " +
            $"native PC=${nativeState.PC:X4} A=${nativeState.A:X2} X=${nativeState.X:X2} Y=${nativeState.Y:X2} S=${nativeState.S:X2} P=${nativeState.P:X2}; " +
            $"{vicTrace}; {cpuTrace}");
    }

    private string FormatVicTrace()
    {
        if (_machine.Devices.GetByRole(DeviceRole.VideoChip) is not Mos6569 vic)
            return "managed VIC unavailable";

        var nativeVic = _native.GetVicState();
        return
            $"managed VIC line=${vic.CurrentRasterLine:X3} x={vic.RasterX} bad={vic.IsBadLine} hold={vic.IsCpuCycleStolen}; " +
            $"native VIC line=${nativeVic.RasterLine:X3} x={nativeVic.RasterCycle} bad={nativeVic.BadLine != 0} spriteDma=${nativeVic.SpriteDma:X2}";
    }

    private string FormatCpuTrace()
    {
        return _machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu
            ? $"cpuStealEligible={cpu.CanStealCurrentCycle} cpuCycle={cpu.DebugCycle} opcode=${cpu.DebugOpcode:X2} delay={cpu.DebugDelayNextFetch}"
            : "cpuStealEligible=unavailable";
    }

    private static ushort ReadPointer(byte lo, byte hi, byte y)
        => (ushort)((lo | (hi << 8)) + y);
}
