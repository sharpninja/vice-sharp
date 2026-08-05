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
    /// Last data byte driven on the multi-device bus (VIC-20 open-bus / VICE
    /// <c>vic20_cpu_last_data</c>). C64 uses <see cref="C64MemoryMap"/>'s own latch.
    /// VICE does not update last-data on BASIC/KERNAL ROM reads
    /// (<c>vic20memrom_*_read</c>), only on RAM / I/O / chargen / dummy stores.
    /// </summary>
    private byte _lastBusValue = 0xFF;

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
            {
                var value = device.Read(address);
                // VICE Vic20: BASIC/KERNAL ROM reads leave vic20_cpu_last_data
                // unchanged; chargen and non-ROM devices refresh it.
                if (DeviceReadUpdatesOpenBusLastData(device, address))
                    _lastBusValue = value;
                return value;
            }
        }

        // Unmapped: VICE-style open bus returns the last driven data byte
        // (vic20 read_unconnected_c_bus / store_dummy_c_bus pair).
        return _lastBusValue;
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
                // Dummy store to ROM still updates last-data in VICE (store_dummy_c_bus).
                _lastBusValue = value;
                return;
            }
        }

        // Open-bus write: update last data, no backing store (VICE store_dummy_c_bus).
        _lastBusValue = value;
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

        // Side-effect-free open-bus view (VICE peek_unconnected_c_bus).
        return _lastBusValue;
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

    /// <summary>
    /// VICE <c>vic20memrom_kernal_read</c> / <c>basic_read</c> do not assign
    /// <c>vic20_cpu_last_data</c>; chargen_read does. Non-ROM devices always do.
    /// </summary>
    private static bool DeviceReadUpdatesOpenBusLastData(IAddressSpace device, ushort address)
    {
        if (device is not RomDevice)
            return true;

        // Chargen window $8000-$8FFF (VIC-20) updates last-data; BASIC/KERNAL do not.
        return address is >= 0x8000 and <= 0x8FFF;
    }
}
