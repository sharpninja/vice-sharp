using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class SystemClock : IClock
{
    private readonly List<IClockedDevice> _devices = new();
    private long _cycle;

    public long TotalCycles => _cycle;
    public long FrequencyHz { get; }

    /// <summary>
    /// Creates a new SystemClock with default C64 PAL frequency (985248 Hz).
    /// </summary>
    public SystemClock() : this(985248)
    {
    }

    /// <summary>
    /// Creates a new SystemClock with the specified frequency.
    /// </summary>
    public SystemClock(long frequencyHz)
    {
        FrequencyHz = frequencyHz;
    }

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