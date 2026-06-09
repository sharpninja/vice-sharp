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
    private C64MemoryMap? _singleC64MemoryMap;

    // PERF-BUS-001: Read/Write/Peek run on the single emulation thread (SystemClock);
    // no lock needed here. RegisterDevice/UnregisterDevice retain their lock for
    // safe setup/teardown from any thread.
    public byte Read(ushort address)
    {
        if (_singleC64MemoryMap is { } c64MemoryMap)
        {
            return c64MemoryMap.Read(address);
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            if (device.HandlesAddress(address))
                return device.Read(address);
        }
        return 0xFF;
    }

    public void Write(ushort address, byte value)
    {
        if (_singleC64MemoryMap is { } c64MemoryMap)
        {
            c64MemoryMap.Write(address, value);
            return;
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            if (device.HandlesAddress(address))
            {
                device.Write(address, value);
                return;
            }
        }
    }

    public byte Peek(ushort address)
    {
        if (_singleC64MemoryMap is { } c64MemoryMap)
        {
            return c64MemoryMap.Peek(address);
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
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
            RefreshFastPath();
        }
    }

    public void UnregisterDevice(IAddressSpace device)
    {
        lock (_lock)
        {
            _devices.Remove(device);
            RefreshFastPath();
        }
    }

    private void RefreshFastPath()
    {
        _singleC64MemoryMap = _devices.Count == 1 && _devices[0] is C64MemoryMap memoryMap
            ? memoryMap
            : null;
    }
}
