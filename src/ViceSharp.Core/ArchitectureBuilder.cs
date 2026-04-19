using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Default implementation of IArchitectureBuilder that constructs
/// a running IMachine instance from an IArchitectureDescriptor.
/// </summary>
public sealed class ArchitectureBuilder : IArchitectureBuilder
{
    /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
        // Create core system components
        var bus = new BasicBus();
        var clock = new SystemClock(descriptor.MasterClockHz);
        var deviceRegistry = new DeviceRegistry();
        
        // Create and return machine instance
        return new Machine(descriptor, bus, clock, deviceRegistry);
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
        // Reset all devices in the registry
        foreach (var device in Devices.All)
        {
            device.Reset();
        }
        
        // Reset clock
        Clock.Reset();
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