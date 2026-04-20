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
        var bus = new BasicBus();
        var clock = new SystemClock(descriptor.MasterClockHz);
        var deviceRegistry = new DeviceRegistry();
        
        var ram = new SimpleRam();
        ram.InitializeC64();
        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram);
        
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        
        var cpu = new Mos6502(bus);
        clock.Register(cpu);
        deviceRegistry.Add(cpu);
        
        var vic = new Mos6569(bus, irqLine);
        bus.RegisterDevice(vic);
        clock.Register(vic);
        deviceRegistry.Add(vic);
        
        var cia1 = new Mos6526(bus, irqLine) { BaseAddress = 0xDC00 };
        bus.RegisterDevice(cia1);
        clock.Register(cia1);
        deviceRegistry.Add(cia1);
        
        var cia2 = new Mos6526(bus, nmiLine) { BaseAddress = 0xDD00 };
        bus.RegisterDevice(cia2);
        clock.Register(cia2);
        deviceRegistry.Add(cia2);
        
        var pla = new Mos906114(bus);
        bus.RegisterDevice(pla);
        deviceRegistry.Add(pla);
        
        var sid = new Sid6581(bus);
        bus.RegisterDevice(sid);
        clock.Register(sid);
        deviceRegistry.Add(sid);
        
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, irqLine);

        if (_romProvider != null)
        {
            try
            {
                var basic = _romProvider.LoadRom("basic", "C64");
                for (int i = 0; i < basic.Length; i++)
                {
                    bus.Write((ushort)(0xA000 + i), basic.Span[i]);
                }
                
                var kernal = _romProvider.LoadRom("kernal", "C64");
                for (int i = 0; i < kernal.Length; i++)
                {
                    bus.Write((ushort)(0xE000 + i), kernal.Span[i]);
                }
                
                var character = _romProvider.LoadRom("characters", "C64");
                for (int i = 0; i < character.Length; i++)
                {
                    bus.Write((ushort)(0xD000 + i), character.Span[i]);
                }
                
                // Initialize color RAM $D800 with default color (light blue for chars)
                for (int i = 0; i < 1000; i++)
                {
                    bus.Write((ushort)(0xD800 + i), 14); // Color index 14 = light blue
                }
            }
            catch { /* ROM loading failed */ }
        }

        return machine;
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

    public Machine(
        IArchitectureDescriptor architecture,
        IBus bus,
        IClock clock,
        IDeviceRegistry deviceRegistry,
        Mos6502 cpu,
        IInterruptLine irqLine)
    {
        _architecture = architecture;
        _bus = bus;
        _clock = clock;
        _devices = deviceRegistry;
        _cpu = cpu;
    }

    public IBus Bus => _bus;
    public IClock Clock => _clock;
    public IDeviceRegistry Devices => _devices;
    public IArchitectureDescriptor Architecture => _architecture;

    public void RunFrame() => _clock.Step(19656);
    public void StepInstruction() => _clock.Step();

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
        _cpu.Reset();
        foreach (var device in _devices.All)
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

    public IDevice? GetById(DeviceId id) => _byId.TryGetValue(id, out var device) ? device : null;
    public IReadOnlyList<T> GetAll<T>() where T : IDevice => _devices.OfType<T>().ToList().AsReadOnly();
    public IReadOnlyList<IDevice> All => _devices.AsReadOnly();
    public IDevice? GetByRole(DeviceRole role) => _byRole.TryGetValue(role, out var device) ? device : null;
    public int Count => _devices.Count;

    public void Add(IDevice device)
    {
        _devices.Add(device);
        _byId[device.Id] = device;
    }
}
