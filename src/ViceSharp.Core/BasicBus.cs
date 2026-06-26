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
    private IPubSub? _pubSub;

    /// <summary>
    /// Connect the machine pub/sub so each write publishes a <see cref="MemoryWriteEvent"/>
    /// (gated on <see cref="IPubSub.SubscriptionCount"/>: zero cost when nobody listens) for
    /// the time-travel debugger's write-delta capture.
    /// </summary>
    public void ConnectPubSub(IPubSub pubSub) => _pubSub = pubSub;

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
        // Time-travel capture: record the pre-write byte so the write can be undone later.
        // Gated on a live subscriber so an unobserved run pays only a null/count check.
        if (_pubSub is { SubscriptionCount: > 0 })
            _pubSub.Publish(MemoryWriteEvent.Topic, new MemoryWriteEvent(address, Peek(address), value));

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
