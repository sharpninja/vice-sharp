using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class SystemClock : IClock
{
    private readonly List<IClockedDevice> _devices = new();
    private long _cycle;

    public long TotalCycles => _cycle;
    public long FrequencyHz { get; } = 985248;

    public void Step()
    {
        _cycle++;

        foreach (var device in _devices)
        {
            if (_cycle % device.ClockDivisor == 0)
            {
                device.Tick();
            }
        }
    }

    public void Step(long cycles)
    {
        for (long i = 0; i < cycles; i++)
        {
            Step();
        }
    }

    public void Register(IClockedDevice device)
    {
        _devices.Add(device);
    }

    public void Unregister(IClockedDevice device)
    {
        _devices.Remove(device);
    }

    public void Reset()
    {
        _cycle = 0;
        foreach (var device in _devices)
        {
            device.Reset();
        }
    }
}