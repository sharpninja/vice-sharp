using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// Lockstep cycle accurate validator comparing ViceSharp against original VICE
/// </summary>
public sealed class LockstepValidator : IDisposable
{
    private readonly IMachine _machine;
    private readonly IViceNative _native;
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

        if (!ValidateState())
            return ValidationReport.Fail(0, 0, GetStateDiff());

        for (_cycleCount = 0; _cycleCount < maxCycles; _cycleCount++)
        {
            _machine.Clock.Step();
            _native.Step();

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
            (comparePc ? managedState.PC == nativeState.PC : true);
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

    private static ushort ReadPointer(byte lo, byte hi, byte y)
        => (ushort)((lo | (hi << 8)) + y);
}
