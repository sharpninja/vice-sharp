using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Host.Audio;
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
        // Honor the session request's True Drive selection (default false keeps
        // the simulated-drive path byte-identical, so native parity is safe).
        return Create(
            request,
            request.TrueDrive,
            request.TrueDriveDevice,
            string.IsNullOrWhiteSpace(request.TrueDriveDiskImagePath) ? null : request.TrueDriveDiskImagePath);
    }

    /// <summary>
    /// FR-DRVTRUE-001: create a session, optionally as a cycle-accurate
    /// true-drive rig. With <paramref name="trueDrive"/> false (the default) the
    /// machine is built exactly as before, so the simulated-drive path and
    /// native lockstep parity are unchanged. With it true and a C64 architecture
    /// selected, the session runs a <see cref="CoordinatorMachine"/> (C64 host +
    /// emulated 1541 over IEC).
    /// </summary>
    public EmulatorRuntimeSession Create(
        CreateEmulatorSessionRequest request,
        bool trueDrive,
        int driveDevice = 8,
        string? diskImagePath = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var architectureId = string.IsNullOrWhiteSpace(request.ArchitectureId) ? _defaultArchitectureId : request.ArchitectureId;

        if (!_descriptors.TryGetValue(architectureId, out var descriptor))
            throw new InvalidOperationException($"Architecture '{architectureId}' is not registered.");

        var isTrueDriveRig = trueDrive && descriptor is C64Descriptor;
        var machine = isTrueDriveRig
            ? C64TrueDriveRigBuilder.Build(_architectureBuilder, descriptor, driveDevice, diskImagePath)
            : _architectureBuilder.Build(descriptor);

        var sessionId = $"emulator-{Guid.NewGuid():N}";
        var session = new EmulatorRuntimeSession(sessionId, descriptor, machine, CreateIecBusActivityMonitor(machine));

        // The true-drive rig boots with the disk inserted at build time, so the
        // session's media list must reflect it (otherwise the host reports the
        // drive empty even though the 1541 has the disk).
        if (isTrueDriveRig && !string.IsNullOrWhiteSpace(diskImagePath) && File.Exists(diskImagePath))
        {
            var slot = driveDevice == 9 ? MediaSlot.Drive9 : MediaSlot.Drive8;
            session.MediaAttachments[slot] = new MediaAttachmentDto(
                slot,
                diskImagePath,
                Path.GetFileName(diskImagePath),
                IsAttached: true,
                IsReadOnly: false,
                AppliedToRuntime: true);
        }

        return session;
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
        // Attach the platform real-time audio backend so the SID streams live
        // (null on non-Windows / headless, which leaves emulation silent).
        var audioBackend = AudioBackendFactory.CreateDefault();
        return TryFindC64RomBasePath(out var romBasePath)
            ? new ArchitectureBuilder(CreateC64RomProvider(romBasePath), audioBackend)
            : new ArchitectureBuilder(audioBackend);
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
        // On the true-drive rig the live IEC traffic flows on the coordinator's
        // own bus (the host's always-on bus is unused there), so watch that one.
        if (machine is CoordinatorMachine { IecBus: { } rigBus })
            return new IecBusActivityMonitor(rigBus);

        // Otherwise watch the machine's always-on IEC bus (the single instance
        // the drives are already wired to by the architecture builder), so the
        // activity indicator reflects the real bus rather than a throwaway one.
        var busDevice = machine.Devices.All.OfType<IecBusDevice>().FirstOrDefault();
        if (busDevice is null)
            return null;

        return new IecBusActivityMonitor(busDevice.Bus);
    }
}
