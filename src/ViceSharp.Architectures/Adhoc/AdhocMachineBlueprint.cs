using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;

namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// A validated, ready-to-build machine description produced by
/// <see cref="AdhocMachineYamlLoader"/>. Holds the descriptor and the
/// resolved chip / memory / interrupt plan so the actual <see cref="IMachine"/>
/// can be constructed lazily.
/// </summary>
public sealed class AdhocMachineBlueprint
{
    private readonly AdhocMachinePlan _plan;

    internal AdhocMachineBlueprint(AdhocArchitectureDescriptor descriptor, AdhocMachinePlan plan)
    {
        Descriptor = descriptor;
        _plan = plan;
    }

    /// <summary>The architecture descriptor for this machine.</summary>
    public AdhocArchitectureDescriptor Descriptor { get; }

    /// <summary>
    /// Materialises the blueprint into a runnable <see cref="IMachine"/>.
    /// The supplied <paramref name="builder"/> is used to allocate the base
    /// machine (bus, clock, registry); the loader then registers the chips and
    /// memory regions declared in the YAML on top of it.
    /// </summary>
    public IMachine BuildMachine(IArchitectureBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Allocate a vanilla machine via the supplied builder so callers that
        // pass a customised IArchitectureBuilder keep their behaviour. We then
        // attach interrupt lines, chips, RAM and ROM regions, all of which
        // live on the bus / clock that the builder produced.
        var baseMachine = builder.Build(Descriptor);

        var lines = BuildInterruptLines();
        var devices = BuildAndRegisterChips(baseMachine, lines);
        RegisterMemoryRegions(baseMachine);

        return new AdhocMachine(Descriptor, baseMachine.Bus, baseMachine.Clock, devices);
    }

    private Dictionary<string, InterruptLine> BuildInterruptLines()
    {
        var map = new Dictionary<string, InterruptLine>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in _plan.InterruptLines)
        {
            map[line.Id] = new InterruptLine(line.Type);
        }
        return map;
    }

    private List<IDevice> BuildAndRegisterChips(IMachine baseMachine, Dictionary<string, InterruptLine> lines)
    {
        var devices = new List<IDevice>();
        foreach (var chip in _plan.Chips)
        {
            IDevice device = chip.Type switch
            {
                AdhocChipType.Mos6502 => CreateCpu(baseMachine.Bus),
                AdhocChipType.Mos6526 => CreateCia(baseMachine.Bus, chip, lines),
                AdhocChipType.Mos6569 => CreateVic(baseMachine.Bus, chip, lines),
                AdhocChipType.Sid6581 => CreateSid(baseMachine.Bus),
                _ => throw new AdhocMachineValidationException(
                    $"chips[{chip.Index}].type '{chip.Type}' is not implemented in the loader."),
            };

            if (device is IAddressSpace addressSpace)
            {
                baseMachine.Bus.RegisterDevice(addressSpace);
            }

            if (device is IClockedDevice clocked)
            {
                baseMachine.Clock.Register(clocked);
            }

            devices.Add(device);
        }
        return devices;
    }

    private static Mos6502 CreateCpu(IBus bus) => new(bus);

    private static Mos6526 CreateCia(IBus bus, AdhocChipPlan chip, Dictionary<string, InterruptLine> lines)
    {
        var line = ResolveInterruptLine(chip.IrqLineId ?? chip.NmiLineId, lines, chip.Index);
        return new Mos6526(bus, line) { BaseAddress = chip.BaseAddress!.Value };
    }

    private static Mos6569 CreateVic(IBus bus, AdhocChipPlan chip, Dictionary<string, InterruptLine> lines)
    {
        var line = ResolveInterruptLine(chip.IrqLineId, lines, chip.Index);
        return new Mos6569(bus, line) { BaseAddress = chip.BaseAddress!.Value };
    }

    private static Sid6581 CreateSid(IBus bus) => new(bus);

    private static IInterruptLine ResolveInterruptLine(string? id, Dictionary<string, InterruptLine> lines, int chipIndex)
    {
        if (id is null)
        {
            // Provide an unconnected line so the chip can still be wired. In a
            // real system the chip's driver pin would float; for now we just
            // hand it an IRQ line nobody listens to.
            return new InterruptLine(InterruptType.Irq);
        }
        if (!lines.TryGetValue(id, out var line))
        {
            throw new AdhocMachineValidationException(
                $"chips[{chipIndex}] references undefined interrupt line '{id}'.");
        }
        return line;
    }

    private void RegisterMemoryRegions(IMachine baseMachine)
    {
        foreach (var region in _plan.Regions)
        {
            var size = region.End - region.Start + 1;
            var backing = new byte[size];
            IAddressSpace device = region.Kind switch
            {
                AdhocMemoryKind.Ram => new RamDevice(region.Start, region.End, backing),
                AdhocMemoryKind.Rom => new RomDevice(region.Start, region.End, backing),
                _ => throw new AdhocMachineValidationException(
                    $"memory.regions[{region.Index}].kind '{region.Kind}' is not implemented."),
            };
            baseMachine.Bus.RegisterDevice(device);
        }
    }
}

/// <summary>
/// <see cref="IMachine"/> implementation produced by an <see cref="AdhocMachineBlueprint"/>.
/// Wraps the bus and clock allocated by the underlying <see cref="IArchitectureBuilder"/>
/// and exposes the registered chip devices through an <see cref="IDeviceRegistry"/>.
/// </summary>
internal sealed class AdhocMachine : IMachine
{
    public AdhocMachine(IArchitectureDescriptor architecture, IBus bus, IClock clock, IReadOnlyList<IDevice> devices)
    {
        Architecture = architecture;
        Bus = bus;
        Clock = clock;
        Devices = new AdhocDeviceRegistry(devices);
    }

    public IBus Bus { get; }
    public IClock Clock { get; }
    public IDeviceRegistry Devices { get; }
    public IArchitectureDescriptor Architecture { get; }

    public void RunFrame()
    {
        // Run a generic PAL frame so callers driving the adhoc machine from a
        // host get useful default behaviour. Frame size is hard coded for now;
        // a future schema revision can make this configurable.
        Clock.Step(19656);
    }

    public void StepInstruction()
    {
        // Step a single cycle. The ad-hoc loader does not yet know how to ask
        // a specific CPU to advance one instruction in a chip-agnostic way.
        Clock.Step();
    }

    public void Reset()
    {
        Clock.Reset();
        foreach (var device in Devices.All)
        {
            device.Reset();
        }
    }

    public MachineState GetState()
    {
        // Ad-hoc machines are chip-agnostic. Surface a default snapshot until
        // a future schema revision picks a designated CPU role to read from.
        return default;
    }
}

internal sealed class AdhocDeviceRegistry : IDeviceRegistry
{
    private readonly List<IDevice> _devices;
    private readonly Dictionary<DeviceId, IDevice> _byId = new();

    public AdhocDeviceRegistry(IReadOnlyList<IDevice> devices)
    {
        _devices = devices.ToList();
        foreach (var device in _devices)
        {
            _byId[device.Id] = device;
        }
    }

    public IDevice? GetById(DeviceId id) =>
        _byId.TryGetValue(id, out var device) ? device : null;

    public IReadOnlyList<T> GetAll<T>() where T : IDevice =>
        _devices.OfType<T>().ToList().AsReadOnly();

    public IReadOnlyList<IDevice> All => _devices.AsReadOnly();

    public IDevice? GetByRole(DeviceRole role) => null;

    public int Count => _devices.Count;
}
