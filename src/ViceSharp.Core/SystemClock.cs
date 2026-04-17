using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class SystemClock
{
    private readonly List<IClockedDevice> _devices = new();
    private ulong _cycle;

    public ulong CurrentCycle => _cycle;

    public void RegisterDevice(IClockedDevice device)
    {
        _devices.Add(device);
    }

    public void Tick()
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

    public void Reset()
    {
        _cycle = 0;
        foreach (var device in _devices)
        {
            device.Reset();
        }
    }
}