using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Protocol;
using ViceSharp.RomFetch;

namespace ViceSharp.Host.Runtime;

public sealed class DefaultEmulatorRuntimeFactory : IEmulatorRuntimeFactory
{
    private readonly IArchitectureBuilder _architectureBuilder;
    private readonly IReadOnlyDictionary<string, IArchitectureDescriptor> _descriptors;
    private readonly string _defaultArchitectureId;

    public DefaultEmulatorRuntimeFactory()
        : this(CreateDefaultArchitectureBuilder(), CreateDefaultDescriptors(), CreateDefaultArchitectureId())
    {
    }

    public DefaultEmulatorRuntimeFactory(
        IArchitectureBuilder architectureBuilder,
        IEnumerable<IArchitectureDescriptor> descriptors)
        : this(architectureBuilder, descriptors, MinimalHostArchitectureDescriptor.ArchitectureId)
    {
    }

    public DefaultEmulatorRuntimeFactory(
        IArchitectureBuilder architectureBuilder,
        IEnumerable<IArchitectureDescriptor> descriptors,
        string defaultArchitectureId)
    {
        ArgumentNullException.ThrowIfNull(architectureBuilder);
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultArchitectureId);

        _architectureBuilder = architectureBuilder;
        _defaultArchitectureId = defaultArchitectureId;
        _descriptors = descriptors
            .SelectMany(CreateDescriptorKeys)
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Descriptor, StringComparer.OrdinalIgnoreCase);
    }

    public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var architectureId = string.IsNullOrWhiteSpace(request.ArchitectureId) ? _defaultArchitectureId : request.ArchitectureId;

        if (!_descriptors.TryGetValue(architectureId, out var descriptor))
            throw new InvalidOperationException($"Architecture '{architectureId}' is not registered.");

        var machine = _architectureBuilder.Build(descriptor);
        var sessionId = $"emulator-{Guid.NewGuid():N}";
        return new EmulatorRuntimeSession(sessionId, descriptor, machine, CreateIecBusActivityMonitor(machine));
    }

    private static IEnumerable<(string Key, IArchitectureDescriptor Descriptor)> CreateDescriptorKeys(
        IArchitectureDescriptor descriptor)
    {
        yield return (descriptor.MachineName, descriptor);

        if (ReferenceEquals(descriptor, MinimalHostArchitectureDescriptor.Instance))
            yield return (MinimalHostArchitectureDescriptor.ArchitectureId, descriptor);

        if (descriptor is C64Descriptor c64Descriptor)
        {
            yield return (c64Descriptor.Profile.Id, descriptor);
            foreach (var alias in c64Descriptor.Profile.Aliases)
                yield return (alias, descriptor);
        }
    }

    private static IArchitectureBuilder CreateDefaultArchitectureBuilder()
    {
        return TryFindC64RomBasePath(out var romBasePath)
            ? new ArchitectureBuilder(CreateC64RomProvider(romBasePath))
            : new ArchitectureBuilder();
    }

    private static IEnumerable<IArchitectureDescriptor> CreateDefaultDescriptors()
    {
        yield return MinimalHostArchitectureDescriptor.Instance;

        if (TryFindC64RomBasePath(out _))
        {
            foreach (var profile in C64MachineProfiles.All)
                yield return new C64Descriptor(profile);
        }
    }

    private static string CreateDefaultArchitectureId()
    {
        return TryFindC64RomBasePath(out _) ? "c64" : MinimalHostArchitectureDescriptor.ArchitectureId;
    }

    private static bool TryFindC64RomBasePath(out string romBasePath)
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = CreateC64RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new C64RomSet().IsComplete(provider))
            {
                romBasePath = dataRoot;
                return true;
            }
        }

        romBasePath = string.Empty;
        return false;
    }

    private static RomProvider CreateC64RomProvider(string romBasePath)
    {
        return new RomProvider(romBasePath, ViceDataPathResolver.FindDataRoots().Where(path => !string.Equals(path, romBasePath, StringComparison.OrdinalIgnoreCase)));
    }

    private static RomProvider CreateC64RomProvider(string romBasePath, IEnumerable<string> fallbackRomBasePaths)
    {
        return new RomProvider(romBasePath, fallbackRomBasePaths);
    }

    private static IecBusActivityMonitor? CreateIecBusActivityMonitor(IMachine machine)
    {
        var drives = machine.Devices.All.OfType<IecDrive>().ToArray();
        if (drives.Length == 0)
            return null;

        var bus = IecInterSystemBus.Create();
        foreach (var drive in drives)
            drive.ConnectIecBus(bus);

        return new IecBusActivityMonitor(bus);
    }
}
