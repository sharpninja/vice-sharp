using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Pla;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

/// <summary>
/// Default implementation of IArchitectureBuilder that constructs
/// a running IMachine instance from an IArchitectureDescriptor.
/// </summary>
public sealed class ArchitectureBuilder : IArchitectureBuilder
{
    private readonly IRomProvider? _romProvider;

    public ArchitectureBuilder() { }

    public ArchitectureBuilder(IRomProvider romProvider)
    {
        _romProvider = romProvider;
    }

    /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
        // Create core system components
        var bus = new BasicBus();
        var clock = new SystemClock(descriptor.MasterClockHz);
        var deviceRegistry = new DeviceRegistry();
        
        // Create shared interrupt line
        var irqLine = new InterruptLine(InterruptType.Irq);
        
        // Create VIC-II at 0xD000
        var vic = new Mos6569(bus, irqLine);
        bus.RegisterDevice(vic);
        deviceRegistry.Add(vic);
        
        // Create CIA #1 at 0xDC00
        var cia1 = new Mos6526(bus, irqLine);
        deviceRegistry.Add(cia1);
        
        // Create CIA #2 at 0xDD00
        var cia2 = new Mos6526(bus, irqLine);
        deviceRegistry.Add(cia2);
        
        // Create PLA for memory banking
        var pla = new Mos906114(bus);
        deviceRegistry.Add(pla);
        
        // Create SID at 0xD400
        var sid = new Sid6581(bus);
        bus.RegisterDevice(sid);
        deviceRegistry.Add(sid);
        
        // Create machine instance
        var machine = new Machine(descriptor, bus, clock, deviceRegistry);

        // Load ROMs if provider is available
        if (_romProvider != null)
        {
            var loader = new C64RomLoader(bus);
            try
            {
                var basic = _romProvider.LoadRom("basic", "C64");
                var kernal = _romProvider.LoadRom("kernal", "C64");
                var character = _romProvider.LoadRom("characters", "C64");
                loader.LoadAllRoms(basic.Span, kernal.Span, character.Span);
            }
            catch { /* ROM loading failed - continue without ROMs */ }
        }

        return machine;
    }
}

/// <summary>
/// Default IMachine implementation holding all system components.
/// </summary>
internal sealed class Machine : IMachine
{
    /// <inheritdoc />
    public IBus Bus { get; }

    /// <inheritdoc />
    public IClock Clock { get; }

    /// <inheritdoc />
    public IDeviceRegistry Devices { get; }

    /// <inheritdoc />
    public IArchitectureDescriptor Architecture { get; }

    public Machine(
        IArchitectureDescriptor architecture,
        IBus bus,
        IClock clock,
        IDeviceRegistry deviceRegistry)
    {
        Architecture = architecture;
        Bus = bus;
        Clock = clock;
        Devices = deviceRegistry;
    }

    /// <inheritdoc />
    public void RunFrame()
    {
        // Execute one full frame of cycles (placeholder implementation)
        // Will be properly implemented when video timing is defined
    }

    /// <inheritdoc />
    public void StepInstruction()
    {
        // Step single CPU instruction (placeholder implementation)
        // Will be properly implemented when CPU core is added
    }

    /// <inheritdoc />
    public MachineState GetState()
    {
        return default;
    }

    /// <inheritdoc />
    public void Reset()
    {
        // VICE-style reset sequence (per C64 original hw):
        // 1. Reset clock first
        // 2. CPU reset ($FCE2)
        // 3. CIA reset
        // 4. VIC reset
        // 5. SID reset
        
        Clock.Reset();
        
        // Reset all devices in order
        foreach (var device in Devices.All)
        {
            device.Reset();
        }
    }
}

/// <summary>
/// Default IDeviceRegistry implementation.
/// </summary>
internal sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly List<IDevice> _devices = new();
    private readonly Dictionary<DeviceId, IDevice> _byId = new();
    private readonly Dictionary<DeviceRole, IDevice> _byRole = new();

    /// <inheritdoc />
    public IDevice? GetById(DeviceId id) => _byId.TryGetValue(id, out var device) ? device : null;

    /// <inheritdoc />
    public IReadOnlyList<T> GetAll<T>() where T : IDevice
    {
        return _devices.OfType<T>().ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<IDevice> All => _devices.AsReadOnly();

    /// <inheritdoc />
    public IDevice? GetByRole(DeviceRole role) => _byRole.TryGetValue(role, out var device) ? device : null;

    /// <inheritdoc />
    public int Count => _devices.Count;

    /// <summary>
    /// Adds a device to the registry.
    /// </summary>
    public void Add(IDevice device)
    {
        _devices.Add(device);
        _byId[device.Id] = device;
    }
}
