using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
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
        if (IsC64Machine(descriptor))
            return BuildC64Machine(descriptor);

        return BuildMinimalMachine(descriptor);
    }

    private IMachine BuildMinimalMachine(IArchitectureDescriptor descriptor)
    {
        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var ram = new SimpleRam();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine);

        ram.InitializeC64();
        bus.RegisterDevice(ram);
        clock.Register(cpu);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);

        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu);
        machine.Reset();
        return machine;
    }

    private IMachine BuildC64Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException($"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine);
        var vic = descriptor.VideoStandard == VideoStandard.Ntsc
            ? new Mos6567(bus, irqLine)
            : new Mos6569(bus, irqLine);
        var cia1 = new Mos6526(bus, irqLine) { BaseAddress = 0xDC00 };
        var cia2 = new Mos6526(bus, nmiLine) { BaseAddress = 0xDD00 };
        var pla = new Mos906114(bus);
        var sid = new Sid6581(bus);
        var memory = new C64MemoryMap(vic, sid, cia1, cia2, pla);

        memory.LoadBasicRom(_romProvider.LoadRom("basic", "C64").Span);
        memory.LoadKernalRom(_romProvider.LoadRom("kernal", "C64").Span);
        memory.LoadCharacterRom(_romProvider.LoadRom("characters", "C64").Span);

        bus.RegisterDevice(memory);

        clock.Register(cpu);
        clock.Register(vic);
        clock.Register(cia1);
        clock.Register(cia2);
        clock.Register(sid);
        clock.Register(pla);

        deviceRegistry.Add(memory, DeviceRole.SystemRam);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);
        deviceRegistry.Add(cia1, DeviceRole.Cia1);
        deviceRegistry.Add(cia2, DeviceRole.Cia2);
        deviceRegistry.Add(pla, DeviceRole.Pla);
        deviceRegistry.Add(sid, DeviceRole.AudioChip);

        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu);
        machine.Reset();
        return machine;
    }

    private static bool IsC64Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.VideoChip)
            || roles.Contains(DeviceRole.Cia1)
            || roles.Contains(DeviceRole.Cia2)
            || roles.Contains(DeviceRole.Pla)
            || roles.Contains(DeviceRole.AudioChip);
    }
}

/// <summary>
/// Default IMachine implementation.
/// </summary>
internal sealed class Machine : IMachine
{
    private readonly IBus _bus;
    private readonly IClock _clock;
    private readonly IDeviceRegistry _devices;
    private readonly IArchitectureDescriptor _architecture;
    private readonly Mos6502 _cpu;
    private readonly int _frameCycles;

    public Machine(
        IArchitectureDescriptor architecture,
        IBus bus,
        IClock clock,
        IDeviceRegistry deviceRegistry,
        Mos6502 cpu)
    {
        _architecture = architecture;
        _bus = bus;
        _clock = clock;
        _devices = deviceRegistry;
        _cpu = cpu;
        _frameCycles = architecture.VideoStandard == VideoStandard.Ntsc ? 263 * 64 : 312 * 63;
    }

    public IBus Bus => _bus;
    public IClock Clock => _clock;
    public IDeviceRegistry Devices => _devices;
    public IArchitectureDescriptor Architecture => _architecture;

    public void RunFrame() => _clock.Step(_frameCycles);

    public void StepInstruction()
    {
        do
        {
            _clock.Step();
        }
        while (!_cpu.IsInstructionBoundary);
    }

    public MachineState GetState()
    {
        return new MachineState
        {
            A = _cpu.A,
            X = _cpu.X,
            Y = _cpu.Y,
            S = _cpu.S,
            P = _cpu.P,
            PC = _cpu.PC,
            Cycle = _clock.TotalCycles
        };
    }

    public void Reset()
    {
        _clock.Reset();
        foreach (var device in _devices.All.Where(device => device is not IClockedDevice))
        {
            device.Reset();
        }
        _cpu.Reset();
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
    private int _ciaIndex;

    public IDevice? GetById(DeviceId id) => _byId.TryGetValue(id, out var device) ? device : null;
    public IReadOnlyList<T> GetAll<T>() where T : IDevice => _devices.OfType<T>().ToList().AsReadOnly();
    public IReadOnlyList<IDevice> All => _devices.AsReadOnly();
    public IDevice? GetByRole(DeviceRole role) => _byRole.TryGetValue(role, out var device) ? device : null;
    public int Count => _devices.Count;

    public void Add(IDevice device, params DeviceRole[] roles)
    {
        _devices.Add(device);
        _byId[device.Id] = device;

        foreach (var role in roles)
        {
            _byRole[role] = device;
        }

        if (roles.Length != 0)
            return;

        // Register devices by their role for lookup
        if (device is IVideoChip)
            _byRole[DeviceRole.VideoChip] = device;
        else if (device is IAudioChip)
            _byRole[DeviceRole.AudioChip] = device;
        else if (device is ICpu)
            _byRole[DeviceRole.Cpu] = device;
        else if (device is Mos906114)
            _byRole[DeviceRole.Pla] = device;
        else if (device is ICiaChip)
        {
            // Register CIA chips in order (CIA1 first, then CIA2)
            _byRole[_ciaIndex == 0 ? DeviceRole.Cia1 : DeviceRole.Cia2] = device;
            _ciaIndex++;
        }
    }
}
