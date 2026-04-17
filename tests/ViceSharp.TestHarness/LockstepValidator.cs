using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Architectures.C64;

namespace ViceSharp.TestHarness;

/// <summary>
/// Lockstep cycle accurate validator comparing ViceSharp against original VICE
/// </summary>
public sealed class LockstepValidator : IDisposable
{
    private readonly Commodore64 _machine;
    private readonly IViceNative _native;
    private long _cycleCount;

    public LockstepValidator()
    {
        IBus bus = new BasicBus();
        IClock clock = new SystemClock();
        IInterruptLine irqLine = new InterruptLine(InterruptType.Irq);
        IInterruptLine nmiLine = new InterruptLine(InterruptType.Nmi);
        
        _machine = new Commodore64(bus, clock, irqLine, nmiLine);
        _native = ViceNative.CreateInstance();
    }

    /// <summary>
    /// Run lockstep comparison for specified number of cycles
    /// </summary>
    public ValidationReport Run(long maxCycles)
    {
        _machine.Reset();
        _native.Reset();
        
        for (_cycleCount = 0; _cycleCount < maxCycles; _cycleCount++)
        {
            _machine.Clock.Step();
            _native.Step();

            if (!ValidateState())
            {
                return ValidationReport.Fail(_cycleCount, _cycleCount, GetStateDiff());
            }
        }

        return ValidationReport.Pass(_cycleCount);
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
        return new StateDiff();
    }

    public void Dispose()
    {
        _native.Dispose();
    }
}



