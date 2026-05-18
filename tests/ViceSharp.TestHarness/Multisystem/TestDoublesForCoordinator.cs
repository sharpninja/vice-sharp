namespace ViceSharp.TestHarness.Multisystem;

using ViceSharp.Abstractions;

/// <summary>
/// Counting test-double for IClock. Records Step() calls, Reset() calls,
/// and registered device counts. FrequencyHz is settable at construction
/// so coordinator tests can pin precise host:peripheral ratios.
/// </summary>
internal sealed class TestClock : IClock
{
    public TestClock(long frequencyHz)
    {
        FrequencyHz = frequencyHz;
    }

    public long FrequencyHz { get; }

    public long TotalCycles { get; private set; }

    public int ResetCount { get; private set; }

    public int RegisterCount { get; private set; }

    public int UnregisterCount { get; private set; }

    public void Step()
    {
        TotalCycles++;
    }

    public void Step(long cycles)
    {
        TotalCycles += cycles;
    }

    public void Register(IClockedDevice device) => RegisterCount++;

    public void Unregister(IClockedDevice device) => UnregisterCount++;

    public void Reset()
    {
        ResetCount++;
        TotalCycles = 0;
    }
}

/// <summary>
/// Minimal IMachine test-double for coordinator tests. Wraps a TestClock and
/// counts Reset() calls. Bus/Devices/Architecture throw - the coordinator only
/// touches Clock and Reset under test, so this surface is enough.
/// </summary>
internal sealed class TestMachine : IMachine
{
    public TestMachine(long frequencyHz)
    {
        Clock = new TestClock(frequencyHz);
    }

    public TestClock Clock { get; }

    IClock IMachine.Clock => Clock;

    public IBus Bus => throw new NotSupportedException();

    public IDeviceRegistry Devices => throw new NotSupportedException();

    public IArchitectureDescriptor Architecture => throw new NotSupportedException();

    public int ResetCount { get; private set; }

    public void RunFrame() => throw new NotSupportedException();

    public void StepInstruction() => throw new NotSupportedException();

    public MachineState GetState() => new();

    public void Reset()
    {
        ResetCount++;
        Clock.Reset();
    }
}
