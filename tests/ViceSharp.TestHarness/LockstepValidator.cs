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

    public LockstepValidator()
    {
        _machine = MachineTestFactory.CreateC64Machine();
        _native = ViceNative.CreateInstance();
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

    private bool ValidateState()
    {
        var managedState = _machine.GetState();
        var nativeState = _native.GetState();

        return 
            managedState.A == nativeState.A &&
            managedState.X == nativeState.X &&
            managedState.Y == nativeState.Y &&
            managedState.S == nativeState.S &&
            managedState.P == nativeState.P &&
            managedState.PC == nativeState.PC;
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
}


