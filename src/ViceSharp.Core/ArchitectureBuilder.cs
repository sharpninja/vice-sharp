using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Chips.Tape;
using ViceSharp.Core.Wiring;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

/// <summary>
/// Default implementation of IArchitectureBuilder that constructs
/// a running IMachine instance from an IArchitectureDescriptor.
/// </summary>
public sealed class ArchitectureBuilder : IArchitectureBuilder
{
    private readonly IRomProvider? _romProvider;
    private readonly IAudioBackend? _audioBackend;

    public ArchitectureBuilder() { }

    public ArchitectureBuilder(IAudioBackend? audioBackend)
    {
        _audioBackend = audioBackend;
    }

    public ArchitectureBuilder(IRomProvider romProvider, IAudioBackend? audioBackend = null)
    {
        _romProvider = romProvider;
        _audioBackend = audioBackend;
    }

    /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
        if (IsC1541Machine(descriptor))
            return BuildC1541Machine(descriptor);

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

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        return machine;
    }

    // Wire one machine pub/sub: the CPU publishes instruction-completed events and the bus
    // publishes memory-write events on it, both gated on a live subscriber. The host's
    // tick-history recorder subscribes for the time-travel debugger.
    private static LockFreePubSub ConnectMachinePubSub(BasicBus bus, Mos6502 cpu)
    {
        var pubSub = new LockFreePubSub();
        cpu.ConnectPubSub(pubSub);
        bus.ConnectPubSub(pubSub);
        return pubSub;
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
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine, nmiLine);
        var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;
        var systemCore = profile is null ? null : new SystemCore(profile.SystemCore);
        var vic = CreateVicII(bus, irqLine, descriptor, profile);
        var cia1 = CreateC64Cia(bus, irqLine, 0xDC00, descriptor.MasterClockHz);
        var cia2Connected = profile?.SystemCore.Cia2Connected ?? true;
        var cia2 = cia2Connected ? CreateC64Cia(bus, nmiLine, 0xDD00, descriptor.MasterClockHz) : null;
        var pla = new Mos906114(bus);
        var sid = CreateSid(bus, profile, _audioBackend, descriptor.MasterClockHz);
        var defaultCartridgeMappingMode = ResolveDefaultCartridgeMappingMode(profile);
        var cia2PortAInputMask = ResolveCia2PortAInputMask(profile);
        var memory = new C64MemoryMap(
            vic,
            sid,
            cia1,
            cia2,
            pla,
            profile?.KeyboardEnabled ?? true,
            cia2PortAInputMask: cia2PortAInputMask,
            cia2Connected: cia2Connected,
            defaultCartridgeMappingMode: defaultCartridgeMappingMode,
            cpuPcReader: () => cpu.PC);
        cpu.ShouldDeferAbsoluteStore = memory.ShouldDeferCpuAbsoluteStore;
        cpu.ShouldDelayNextFetchAfterWrite = memory.ShouldDelayCpuFetchAfterWrite;
        var iecBusConnected = profile?.SystemCore.IecBusConnected ?? true;
        var cia2Interface = cia2Connected ? new C64Cia2InterfaceDevice() : null;
        var drive8 = iecBusConnected ? new IecDrive(8) : null;
        var drive9 = iecBusConnected ? new IecDrive(9) : null;

        // DD-IEC-1: the IEC serial bus is always present on a C64 (it hangs off
        // CIA2), independent of whether a drive is attached. Create it once and
        // attach the drives so the bus, spy and activity monitor observe the
        // real machine bus. CIA2 is intentionally NOT routed to the live bus
        // here - that read (with the faithful electrical model) is parity-gated
        // and lands separately; until then CIA2 keeps its native-matching mask.
        var iecBus = iecBusConnected ? IecInterSystemBus.Create() : null;
        if (iecBus is not null)
        {
            drive8?.ConnectIecBus(iecBus);
            drive9?.ConnectIecBus(iecBus);
        }
        var datasette = profile?.SystemCore.TapePortConnected == false ? null : new Datasette();
        var datasetteCia1FlagBinding = datasette is null
            ? null
            : new DatasetteCia1FlagBinding(datasette, cia1);
        bus.RegisterDevice(memory);

        var romLoader = new C64RomLoader(bus);
        var basicRomName = profile?.BasicRomName ?? "basic";
        var kernalRomName = profile?.KernalRomName ?? "kernal";
        var characterRomName = profile?.CharacterRomName ?? "characters";
        var basic = _romProvider.LoadRom(basicRomName, "C64").Span;
        var kernal = IsKernalRomRequired(kernalRomName)
            ? _romProvider.LoadRom(kernalRomName, "C64").Span
            : ReadOnlySpan<byte>.Empty;
        var character = _romProvider.LoadRom(characterRomName, "C64").Span;

        memory.BeginRomLoad();
        try
        {
            if (!romLoader.LoadAllRoms(basic, kernal, character, basicRomName, kernalRomName, characterRomName))
            {
                throw new InvalidOperationException($"{descriptor.MachineName} ROM set is invalid or missing expected checksum entries.");
            }
        }
        finally
        {
            memory.EndRomLoad();
        }

        clock.Register(cpu);
        clock.Register(vic);
        clock.Register(cia1);
        if (cia2 is not null)
            clock.Register(cia2);
        clock.Register(sid);
        clock.Register(pla);
        if (drive8 is not null)
            clock.Register(drive8);
        if (drive9 is not null)
            clock.Register(drive9);
        if (datasetteCia1FlagBinding is not null)
            clock.Register(datasetteCia1FlagBinding);

        if (systemCore is not null)
            deviceRegistry.Add(systemCore, DeviceRole.SystemCore);

        deviceRegistry.Add(memory, DeviceRole.SystemRam, DeviceRole.CartridgePort);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);
        deviceRegistry.Add(cia1, DeviceRole.Cia1);
        if (cia2 is not null)
            deviceRegistry.Add(cia2, DeviceRole.Cia2);
        if (cia2Interface is not null)
            deviceRegistry.Add(cia2Interface);
        if (iecBus is not null)
            deviceRegistry.Add(new IecBusDevice(iecBus));
        deviceRegistry.Add(pla, DeviceRole.Pla);
        deviceRegistry.Add(sid, DeviceRole.AudioChip);
        if (drive8 is not null)
            deviceRegistry.Add(drive8);
        if (drive9 is not null)
            deviceRegistry.Add(drive9);
        if (datasette is not null)
            deviceRegistry.Add(datasette);
        if (datasetteCia1FlagBinding is not null)
            deviceRegistry.Add(datasetteCia1FlagBinding);

        // KERNAL serial-bus traps (VICE virtual device traps): service LOAD/$ on
        // the simulated drive when True Drive is OFF. The hook declines unless the
        // addressed drive has a disk, so it is inert on parity rigs and on the
        // true-drive host C64 (whose disk lives in the emulated 1541, not here).
        KernalSerialTrap? serialTrap = null;
        if (drive8 is not null || drive9 is not null)
        {
            var virtualDrives = new VirtualDriveServer(device => device switch
            {
                8 => drive8?.DiskImage,
                9 => drive9?.DiskImage,
                _ => null
            });
            serialTrap = new KernalSerialTrap(cpu, bus, virtualDrives);
            cpu.SerialTrapHook = serialTrap.TryHandle;
        }

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        serialTrap?.Reset();
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

    private static bool IsC1541Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.DriveCpu) && roles.Contains(DeviceRole.DriveRom);
    }

    private IMachine BuildC1541Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine);

        // 2KB drive RAM at $0000-$07FF.
        var ramDescriptor = descriptor.Devices.First(d => d.Role == DeviceRole.DriveRam);
        var ramBytes = new byte[ramDescriptor.Size];
        var ram = new RamDevice(
            ramDescriptor.BaseAddress,
            (ushort)(ramDescriptor.BaseAddress + ramDescriptor.Size - 1),
            ramBytes);
        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.DriveRam);

        // Two VIAs at $1800 and $1C00 (each with 1KB mirror window).
        var viaDescriptors = descriptor.Devices.Where(d => d.Role == DeviceRole.DriveVia).ToArray();
        var vias = new List<ViceSharp.Chips.IEC.Via6522>(viaDescriptors.Length);
        foreach (var v in viaDescriptors)
        {
            var via = new ViceSharp.Chips.IEC.Via6522(bus, irqLine)
            {
                Id = v.Id,
                Name = v.Name,
                SourceId = v.Id,
                BaseAddress = v.BaseAddress,
                Size = (ushort)v.Size,
            };
            bus.RegisterDevice(via);
            clock.Register(via);
            deviceRegistry.Add(via, DeviceRole.DriveVia);
            vias.Add(via);
        }

        // 16KB drive DOS ROM at $C000-$FFFF.
        var romDescriptor = descriptor.Devices.First(d => d.Role == DeviceRole.DriveRom);
        var dosRomName = ResolveC1541DosRomName(descriptor);
        var romBytes = _romProvider.LoadRom(
            dosRomName,
            descriptor.RequiredRoms?.Architecture ?? "DRIVES").ToArray();
        var rom = new RomDevice(
            romDescriptor.BaseAddress,
            (ushort)(romDescriptor.BaseAddress + romDescriptor.Size - 1),
            romBytes);
        bus.RegisterDevice(rom);
        deviceRegistry.Add(rom, DeviceRole.DriveRom);

        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.DriveCpu);

        // Optional D64 disk image mount.
        D64DiskImageDevice? disk = null;
        if (descriptor is IDriveArchitectureDescriptor drive && !string.IsNullOrWhiteSpace(drive.DiskImagePath))
        {
            disk = D64DiskImageDevice.LoadFromFile(drive.DiskImagePath!);
            deviceRegistry.Add(disk, DeviceRole.DriveDisk);
        }

        var deviceNumber = descriptor is IDriveArchitectureDescriptor driveDescriptor
            ? driveDescriptor.DeviceNumber
            : 8;
        deviceRegistry.Add(new C1541IecInterfaceDevice(deviceNumber));

        var driveMechanism = new C1541DriveMechanismDevice(disk, cpu);
        clock.Register(driveMechanism);
        deviceRegistry.Add(driveMechanism);

        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu);
        machine.Reset();
        return machine;
    }

    private static string ResolveC1541DosRomName(IArchitectureDescriptor descriptor)
        => descriptor is IDriveArchitectureDescriptor d && !string.IsNullOrWhiteSpace(d.DosRomName)
            ? d.DosRomName
            : "dos1541-325302-01+901229-05.bin";

    private static bool IsKernalRomRequired(string kernalRomName)
        => !string.Equals(kernalRomName, "kernal-none.bin", StringComparison.OrdinalIgnoreCase);

    private static CartridgeMappingMode ResolveDefaultCartridgeMappingMode(IMachineProfile? profile)
    {
        if (profile is null)
            return CartridgeMappingMode.Auto;

        if (string.Equals(profile.SystemCore.AddressDecoderPolicy, "Ultimax", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.Ultimax;

        if (string.Equals(profile.BoardModel, "C64GS", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.GameSystem;

        return CartridgeMappingMode.Auto;
    }

    private static byte ResolveCia2PortAInputMask(IMachineProfile? profile)
    {
        if (profile is null)
            return 0x7F;

        return profile.SystemCore.BusPolicy switch
        {
            "GameSystem" or "Max" => 0x3F,
            _ => 0x7F
        };
    }

    private static Mos6526 CreateC64Cia(IBus bus, IInterruptLine line, ushort baseAddress, long masterClockHz)
    {
        return new Mos6526(bus, line)
        {
            BaseAddress = baseAddress,
            TodCyclesPer50HzTick = (int)(masterClockHz / 50),
            TodCyclesPer60HzTick = (int)(masterClockHz / 60)
        };
    }

    private static Mos6569 CreateVicII(
        IBus bus,
        IInterruptLine irqLine,
        IArchitectureDescriptor descriptor,
        IMachineProfile? profile)
    {
        var vic = profile?.VicIIModel switch
        {
            "Mos6569R1" => new Mos6569R1(bus, irqLine),
            "Mos6567R56A" => new Mos6567R56A(bus, irqLine),
            "Mos6567R8" => new Mos6567(bus, irqLine),
            "Mos6572" => new Mos6572(bus, irqLine),
            "Mos8562" => new Mos8562(bus, irqLine),
            "Mos8565" => new Mos8565(bus, irqLine),
            _ => descriptor.VideoStandard == VideoStandard.Ntsc
                ? new Mos6567(bus, irqLine)
                : new Mos6569(bus, irqLine)
        };

        if (profile is null)
            return vic;

        var system = profile.VideoStandard == VideoStandard.Ntsc
            ? Mos6569.TvSystem.NTSC
            : string.Equals(profile.VicIIModel, "Mos6572", StringComparison.OrdinalIgnoreCase)
                ? Mos6569.TvSystem.PALN
                : Mos6569.TvSystem.PAL;

        var visibleLines = profile.VideoStandard == VideoStandard.Ntsc
            ? Mos6569.NtscVisibleLines
            : Mos6569.PalVisibleLines;
        vic.ConfigureTiming(
            system,
            profile.CyclesPerLine,
            visibleLines,
            profile.RasterLines,
            profile.RefreshRateHz);
        return vic;
    }

    private static Sid6581 CreateSid(IBus bus, IMachineProfile? profile, IAudioBackend? audioBackend, double masterClockHz)
    {
        var sid = profile is not null &&
                  profile.SidModel.Contains("8580", StringComparison.OrdinalIgnoreCase)
            ? new Sid8580(bus, audioBackend) { BaseAddress = 0xD400 }
            : new Sid6581(bus, audioBackend) { BaseAddress = 0xD400 };

        // Drive live-audio emission at 44.1 kHz only when a backend is present;
        // otherwise the SID never touches the audio path (parity-preserving).
        if (audioBackend is not null)
            sid.ConfigureAudioClock(masterClockHz);

        return sid;
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
    private readonly IPubSub? _pubSub;
    private readonly int _frameCycles;

    public Machine(
        IArchitectureDescriptor architecture,
        IBus bus,
        IClock clock,
        IDeviceRegistry deviceRegistry,
        Mos6502 cpu,
        IPubSub? pubSub = null)
    {
        _architecture = architecture;
        _bus = bus;
        _clock = clock;
        _devices = deviceRegistry;
        _cpu = cpu;
        _pubSub = pubSub;
        _frameCycles = architecture is IProfiledArchitectureDescriptor profiled
            ? profiled.MachineProfile.CyclesPerLine * profiled.MachineProfile.RasterLines
            : architecture.VideoStandard == VideoStandard.Ntsc ? 263 * 65 : 312 * 63;
    }

    public IBus Bus => _bus;
    public IClock Clock => _clock;
    public IDeviceRegistry Devices => _devices;
    public IArchitectureDescriptor Architecture => _architecture;
    public IPubSub? PubSub => _pubSub;
    public ICpu? PrimaryCpu => _cpu;

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
        else if (device is ICartridgePort)
            _byRole[DeviceRole.CartridgePort] = device;
        else if (device is ICiaChip)
        {
            // Register CIA chips in order (CIA1 first, then CIA2)
            _byRole[_ciaIndex == 0 ? DeviceRole.Cia1 : DeviceRole.Cia2] = device;
            _ciaIndex++;
        }
    }
}

// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001: minimal debugcart device (or equivalent) for harness smoke.
// $D7FF write triggers exit signaling (pattern from debugcart.c:74). Registered on bus in launcher
// entry for C64 topology when -debugcart supplied. Enables process-level testbench exit codes.
public sealed class ExitState
{
    public bool Requested { get; set; }
    public int Code { get; set; }
}

public sealed class DebugCartDevice : IAddressSpace, IDevice
{
    private readonly ExitState _exit;

    public DebugCartDevice(ExitState exit)
    {
        _exit = exit;
        Id = new DeviceId(0xD7C0DE);
        Name = "DebugCart (ARCH-TESTBENCH-001)";
    }

    public DeviceId Id { get; }
    public string Name { get; }

    public void Reset() { }

    public bool HandlesAddress(ushort address) => address == 0xD7FF;

    public byte Read(ushort address) => 0xFF;
    public byte Peek(ushort address) => 0xFF;

    public void Write(ushort address, byte value)
    {
        if (address == 0xD7FF && !_exit.Requested)
        {
            _exit.Requested = true;
            _exit.Code = value;
        }
    }
}
