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
    /// Last data on the VIC-20 V-bus (VD0-VD7). VICE <c>vic20_v_bus_last_data</c>:
    /// updated after V-bus region CPU access (<c>vic20_mem_v_bus_read/store</c>)
    /// and color RAM r/w. Color RAM high nibble is open-bus from this latch.
    /// </summary>
    private byte _vBusLastData = 0xFF;

    /// <summary>Last data byte on the multi-device C-bus (open-bus).</summary>
    public byte LastBusValue => _lastBusValue;

    /// <summary>Last data byte on the VIC-20 V-bus (color RAM open-bus high nibble).</summary>
    public byte VBusLastData => _vBusLastData;

    /// <summary>
    /// Record a V-bus sample after a CPU access to V-bus space ($0000-$1FFF,
    /// $8000-$9FFF) or color RAM. Mirrors VICE <c>vic20_mem_v_bus_read</c> /
    /// colorram_read assignment of <c>vic20_v_bus_last_data</c>.
    /// </summary>
    public void NoteVBusData(byte value) => _vBusLastData = value;

    /// <summary>
    /// VIC display fetch (VICE <c>vic_cycle_fetch</c>): screen byte becomes
    /// <c>vic20_v_bus_last_data</c>. Color high nibble for a following CPU
    /// color RAM read is taken from this value's high nibble.
    /// </summary>
    public void NoteVicDisplayFetch(byte screenData, byte colorNibble)
    {
        // VICE: v_bus_last_data = screen (b); v_bus_last_high = color (c).
        // colorram_read uses (v_bus_last_data & 0xf0) for the open-bus high
        // nibble. Keep screen as the data latch.
        _vBusLastData = screenData;
    }

    /// <summary>V-bus region for VIC-20 CPU map (low RAM + $8000-$9FFF I/O/ROM).</summary>
    public static bool IsVic20VBusAddress(ushort address)
        => address <= 0x1FFF || address is >= 0x8000 and <= 0x9FFF;

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
                // VICE zero_read / ram_read_v_bus / chargen: after C-bus last
                // data, refresh V-bus latch (vic20_mem_v_bus_read). Color RAM
                // updates V-bus inside its own Read (colorram_read).
                if (device is not Vic20.Vic20ColorRam
                    && IsVic20VBusAddress(address)
                    && DeviceReadUpdatesOpenBusLastData(device, address))
                    _vBusLastData = _lastBusValue;
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
                // VICE zero_store / ram_store_v_bus / colorram_store.
                if (device is not Vic20.Vic20ColorRam && IsVic20VBusAddress(address))
                    _vBusLastData = value;
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
