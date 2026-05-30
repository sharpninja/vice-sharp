using System.Collections.Immutable;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// High performance system bus implementation.
/// </summary>
public sealed class BasicBus : IBus
{
    private List<IAddressSpace> _devices = new();
    private readonly object _lock = new();

    // PERF-BUS-001: Read/Write/Peek run on the single emulation thread (SystemClock);
    // no lock needed here. RegisterDevice/UnregisterDevice retain their lock for
    // safe setup/teardown from any thread.
    public byte Read(ushort address)
    {
        foreach (var device in _devices)
        {
            if (device.HandlesAddress(address))
                return device.Read(address);
        }
        return 0xFF;
    }

    public void Write(ushort address, byte value)
    {
        foreach (var device in _devices)
        {
            if (device.HandlesAddress(address))
            {
                device.Write(address, value);
                return;
            }
        }
    }

    public byte Peek(ushort address)
    {
        foreach (var device in _devices)
        {
            if (device.HandlesAddress(address))
                return device.Peek(address);
        }
        return 0xFF;
    }

    public void RegisterDevice(IAddressSpace device)
    {
        lock (_lock)
        {
            _devices.Insert(0, device);
        }
    }

    public void UnregisterDevice(IAddressSpace device)
    {
        lock (_lock)
        {
            _devices.Remove(device);
        }
    }
}
