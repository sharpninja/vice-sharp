using ViceSharp.Abstractions;
using ViceSharp.Architectures.Adhoc;
using ViceSharp.Core;

namespace ViceSharp.Architectures.Multisystem;

/// <summary>
/// A validated multi-system topology blueprint. BuildCoordinator constructs
/// the ISystemCoordinator + all attached machines + buses + recorded endpoint
/// attachments, ready for the console host (or a test) to drive.
/// </summary>
public sealed class MultiSystemBlueprint
{
    private readonly MultiSystemMachinePlan _host;
    private readonly IReadOnlyList<MultiSystemPeripheralPlan> _peripherals;
    private readonly IReadOnlyList<MultiSystemCartExtensionPlan> _cartExtensions;
    private readonly IReadOnlyList<MultiSystemBusPlan> _buses;

    internal MultiSystemBlueprint(
        MultiSystemMachinePlan host,
        IReadOnlyList<MultiSystemPeripheralPlan> peripherals,
        IReadOnlyList<MultiSystemCartExtensionPlan> cartExtensions,
        IReadOnlyList<MultiSystemBusPlan> buses)
    {
        _host = host;
        _peripherals = peripherals;
        _cartExtensions = cartExtensions;
        _buses = buses;
    }

    /// <summary>Host system id (declared in coordinator.host.id).</summary>
    public string HostId => _host.Id;

    /// <summary>Peripheral ids in attach order.</summary>
    public IReadOnlyList<string> PeripheralIds =>
        _peripherals.Select(p => p.Id).ToArray();

    /// <summary>Cart-extension ids in attach order.</summary>
    public IReadOnlyList<string> CartExtensionIds =>
        _cartExtensions.Select(c => c.Id).ToArray();

    /// <summary>Bus ids declared by this topology.</summary>
    public IReadOnlyList<string> BusIds =>
        _buses.Select(b => b.Id).ToArray();

    /// <summary>
    /// Resolve the declared fidelity for an attached system id (peripheral
    /// or cart extension). Host systems always run as <see cref="Fidelity.TrueDevice"/>.
    /// Throws if <paramref name="systemId"/> is unknown.
    /// </summary>
    public Fidelity GetFidelity(string systemId)
    {
        if (systemId == _host.Id) return Fidelity.TrueDevice;
        foreach (var p in _peripherals)
            if (p.Id == systemId) return p.Fidelity;
        foreach (var c in _cartExtensions)
            if (c.Id == systemId) return c.Fidelity;
        throw new KeyNotFoundException($"System '{systemId}' is not part of this blueprint.");
    }

    /// <summary>
    /// Build the coordinator with all systems + buses attached and all
    /// declared endpoints registered. The returned <see cref="MultiSystemBuildResult"/>
    /// exposes the coordinator plus endpoint lookups so device implementations
    /// (Phase B+) can retrieve their IBusEndpoint by (systemId, busId).
    /// </summary>
    public MultiSystemBuildResult BuildCoordinator(
        IArchitectureBuilder builder,
        Func<string, string?, IMachine> machineFactory)
    {
        var coord = new SystemCoordinator();
        var systemsById = new Dictionary<string, IMachine>(StringComparer.Ordinal);
        var endpoints = new Dictionary<(string SystemId, string BusId), IBusEndpoint>();

        var hostMachine = machineFactory(_host.Id, _host.YamlText);
        coord.AttachSystem(hostMachine);
        systemsById[_host.Id] = hostMachine;

        foreach (var peripheral in _peripherals)
        {
            var machine = machineFactory(peripheral.Id, peripheral.YamlText);
            coord.AttachSystem(machine);
            systemsById[peripheral.Id] = machine;
        }

        foreach (var ext in _cartExtensions)
        {
            var machine = machineFactory(ext.Id, ext.YamlText);
            coord.AttachCartExtension(machine, hostMachine);
            systemsById[ext.Id] = machine;
        }

        var busesById = new Dictionary<string, IInterSystemBus>(StringComparer.Ordinal);
        foreach (var bus in _buses)
        {
            var inst = new InterSystemBus(bus.Id, bus.Signals);
            coord.AttachBus(inst);
            busesById[bus.Id] = inst;
        }

        AttachEndpoints(_host.BusAttachments, _host.Id, busesById, endpoints);
        foreach (var peripheral in _peripherals)
            AttachEndpoints(peripheral.BusAttachments, peripheral.Id, busesById, endpoints);
        foreach (var ext in _cartExtensions)
            AttachEndpoints(ext.BusAttachments, ext.Id, busesById, endpoints);

        AutoBindChipsToBuses(systemsById, busesById, endpoints);

        return new MultiSystemBuildResult(coord, systemsById, busesById, endpoints);
    }

    private static void AutoBindChipsToBuses(
        IReadOnlyDictionary<string, IMachine> systemsById,
        IReadOnlyDictionary<string, IInterSystemBus> busesById,
        IReadOnlyDictionary<(string SystemId, string BusId), IBusEndpoint> endpoints)
    {
        foreach (var (systemId, machine) in systemsById)
        {
            IBusEndpoint? iecEp = null;
            IBusEndpoint? userPortEp = null;
            foreach (var (key, ep) in endpoints)
            {
                if (key.SystemId != systemId) continue;
                if (!busesById.TryGetValue(key.BusId, out var bus)) continue;
                if (bus.Name == ViceSharp.Chips.IEC.IecInterSystemBus.BusName) iecEp = ep;
                else if (bus.Name == ViceSharp.Core.UserPortInterSystemBus.BusName) userPortEp = ep;
            }

            if (iecEp is null && userPortEp is null) continue;

            var cia2 = machine.Devices.GetByRole(DeviceRole.Cia2) as ViceSharp.Chips.Cia.Mos6526;
            if (cia2 is not null)
                ViceSharp.Core.Wiring.C64Cia2BusBinding.Bind(cia2, userPort: userPortEp, iec: iecEp);

            if (iecEp is not null)
            {
                var driveVia1 = FindDriveVia1(machine);
                if (driveVia1 is not null)
                {
                    var deviceNumber = (machine.Architecture as IDriveArchitectureDescriptor)?.DeviceNumber ?? 8;
                    ViceSharp.Core.Wiring.C1541Via1BusBinding.Bind(driveVia1, iecEp, deviceNumber);
                }
            }

            // VIA2 (head/motor/WPRT) - bind to the mounted disk if present.
            var driveVia2 = FindDriveVia2(machine);
            var disk = machine.Devices.GetByRole(DeviceRole.DriveDisk) as D64DiskImageDevice;
            if (driveVia2 is not null)
                ViceSharp.Core.Wiring.C1541Via2BusBinding.Bind(driveVia2, disk);
        }
    }

    private static ViceSharp.Chips.IEC.Via6522? FindDriveVia1(IMachine machine)
    {
        ViceSharp.Chips.IEC.Via6522? lowest = null;
        foreach (var v in machine.Devices.GetAll<ViceSharp.Chips.IEC.Via6522>())
        {
            if (lowest is null || v.BaseAddress < lowest.BaseAddress)
                lowest = v;
        }
        return lowest;
    }

    private static ViceSharp.Chips.IEC.Via6522? FindDriveVia2(IMachine machine)
    {
        ViceSharp.Chips.IEC.Via6522? highest = null;
        foreach (var v in machine.Devices.GetAll<ViceSharp.Chips.IEC.Via6522>())
        {
            if (highest is null || v.BaseAddress > highest.BaseAddress)
                highest = v;
        }
        return highest;
    }

    private static void AttachEndpoints(
        IReadOnlyList<MultiSystemBusAttachmentPlan> attachments,
        string systemId,
        Dictionary<string, IInterSystemBus> busesById,
        Dictionary<(string, string), IBusEndpoint> endpoints)
    {
        foreach (var att in attachments)
        {
            var bus = busesById[att.BusId];
            var endpoint = bus.AttachEndpoint(att.EndpointName);
            endpoints[(systemId, att.BusId)] = endpoint;
        }
    }

    /// <summary>
    /// Convenience: build using AdhocMachineYamlLoader for every machine
    /// (host, peripherals, cart-extensions). Caller supplies the IArchitectureBuilder.
    /// </summary>
    /// <summary>
    /// Build the coordinator using each system's declared <c>kind:</c> field
    /// to dispatch to a known architecture descriptor. Recognised kinds:
    /// "C64" -> C64Descriptor; "C1541" -> C1541Descriptor with the
    /// per-peripheral deviceNumber. Systems with neither a kind nor a yaml
    /// spec are routed through the adhoc YAML loader fallback.
    /// </summary>
    public MultiSystemBuildResult BuildCoordinatorAuto(IArchitectureBuilder builder)
    {
        var adhoc = new AdhocMachineYamlLoader();
        return BuildCoordinator(builder, (systemId, yamlText) =>
        {
            var kind = ResolveKind(systemId);
            if (!string.IsNullOrWhiteSpace(kind))
                return BuildKind(builder, systemId, kind, yamlText, adhoc);
            if (yamlText is null)
                throw new MultiSystemValidationException(
                    $"system '{systemId}' has no kind and no yaml; cannot build.");
            var bp = adhoc.LoadFromString(yamlText);
            return bp.BuildMachine(builder);
        });
    }

    private IMachine BuildKind(
        IArchitectureBuilder builder,
        string systemId,
        string kind,
        string? yamlText,
        AdhocMachineYamlLoader adhoc)
    {
        if (string.Equals(kind, "C64", StringComparison.OrdinalIgnoreCase))
            return builder.Build(new Architectures.C64.C64Descriptor());
        if (string.Equals(kind, "C1541", StringComparison.OrdinalIgnoreCase))
        {
            var deviceNumber = ResolvePeripheralDeviceNumber(systemId) ?? 8;
            var diskImagePath = ResolvePeripheralDiskImagePath(systemId);
            return builder.Build(new Architectures.C1541.C1541Descriptor(deviceNumber, diskImagePath));
        }
        if (yamlText is not null)
        {
            var bp = adhoc.LoadFromString(yamlText);
            return bp.BuildMachine(builder);
        }
        throw new MultiSystemValidationException(
            $"system '{systemId}' kind '{kind}' is not recognised and no yaml was supplied.");
    }

    private string? ResolveKind(string systemId)
    {
        if (systemId == _host.Id) return _host.Kind;
        foreach (var p in _peripherals)
            if (p.Id == systemId) return p.Kind;
        foreach (var c in _cartExtensions)
            if (c.Id == systemId) return null;
        return null;
    }

    private int? ResolvePeripheralDeviceNumber(string systemId)
    {
        foreach (var p in _peripherals)
            if (p.Id == systemId) return p.DeviceNumber;
        return null;
    }

    private string? ResolvePeripheralDiskImagePath(string systemId)
    {
        foreach (var p in _peripherals)
            if (p.Id == systemId) return p.DiskImagePath;
        return null;
    }

    public MultiSystemBuildResult BuildCoordinatorWithAdhocLoader(IArchitectureBuilder builder)
    {
        var adhoc = new AdhocMachineYamlLoader();
        return BuildCoordinator(builder, (id, yamlText) =>
        {
            if (yamlText is null)
                throw new MultiSystemValidationException(
                    $"system '{id}' has no yaml; use BuildCoordinatorAuto for kind-based dispatch.");
            var bp = adhoc.LoadFromString(yamlText);
            return bp.BuildMachine(builder);
        });
    }
}

internal sealed record MultiSystemMachinePlan(
    string Id,
    string? Kind,
    string? YamlText,
    IReadOnlyList<MultiSystemBusAttachmentPlan> BusAttachments);

internal sealed record MultiSystemPeripheralPlan(
    string Id,
    string Role,
    Fidelity Fidelity,
    string? Kind,
    int? DeviceNumber,
    string? DiskImagePath,
    string? YamlText,
    IReadOnlyList<MultiSystemBusAttachmentPlan> BusAttachments);

internal sealed record MultiSystemCartExtensionPlan(
    string Id,
    Fidelity Fidelity,
    string YamlText,
    IReadOnlyList<MultiSystemBusAttachmentPlan> BusAttachments);

internal sealed record MultiSystemBusPlan(
    string Id,
    IReadOnlyList<string> Signals);

internal sealed record MultiSystemBusAttachmentPlan(
    string BusId,
    string EndpointName);

/// <summary>
/// Result of building a multi-system topology: the coordinator plus lookups
/// for machines, buses, and endpoint handles.
/// </summary>
public sealed class MultiSystemBuildResult
{
    internal MultiSystemBuildResult(
        ISystemCoordinator coordinator,
        IReadOnlyDictionary<string, IMachine> systemsById,
        IReadOnlyDictionary<string, IInterSystemBus> busesById,
        IReadOnlyDictionary<(string SystemId, string BusId), IBusEndpoint> endpoints)
    {
        Coordinator = coordinator;
        SystemsById = systemsById;
        BusesById = busesById;
        Endpoints = endpoints;
    }

    public ISystemCoordinator Coordinator { get; }
    public IReadOnlyDictionary<string, IMachine> SystemsById { get; }
    public IReadOnlyDictionary<string, IInterSystemBus> BusesById { get; }
    public IReadOnlyDictionary<(string SystemId, string BusId), IBusEndpoint> Endpoints { get; }
}
