using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Presents a coordinator-driven rig - a C64 host plus its true-drive 1541
/// peripheral(s), wired together over the IEC bus - as a single
/// <see cref="IMachine"/>, so the host runtime (which only knows IMachine)
/// transparently runs the whole rig in cycle-lockstep.
///
/// Bus / Clock / Devices / Architecture / state are the C64 host's - that is
/// what the runtime reads for video, memory, and status. RunFrame /
/// StepInstruction / Reset drive the <see cref="SystemCoordinator"/> so the
/// C64 and the drive advance together (the drive also catches up lazily on
/// every IEC access via the coordinator's peripheral sync).
/// </summary>
public sealed class CoordinatorMachine : IMachine
{
    private readonly IMachine _host;
    private readonly SystemCoordinator _coordinator;
    private readonly int _hostCyclesPerFrame;

    public CoordinatorMachine(
        IMachine host,
        SystemCoordinator coordinator,
        int hostCyclesPerFrame,
        IInterSystemBus? iecBus = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(coordinator);
        if (hostCyclesPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(hostCyclesPerFrame), hostCyclesPerFrame, "Frame cycle count must be positive.");

        _host = host;
        _coordinator = coordinator;
        _hostCyclesPerFrame = hostCyclesPerFrame;
        IecBus = iecBus;
    }

    /// <summary>
    /// The live inter-system IEC bus the rig's peripherals are wired to (the
    /// bus that actually carries traffic), or null. Distinct from the host's
    /// internal always-on bus; used by the host runtime to monitor real IEC
    /// activity on the true-drive path.
    /// </summary>
    public IInterSystemBus? IecBus { get; }

    /// <summary>The C64 host machine presented to the runtime.</summary>
    public IMachine Host => _host;

    /// <summary>The coordinator driving the host + its peripherals.</summary>
    public SystemCoordinator Coordinator => _coordinator;

    public IBus Bus => _host.Bus;

    public IClock Clock => _host.Clock;

    /// <summary>The rig's primary CPU is the C64 host's CPU (drive CPUs are their own systems).</summary>
    public ICpu? PrimaryCpu => _host.PrimaryCpu;

    public IDeviceRegistry Devices => _host.Devices;

    public IArchitectureDescriptor Architecture => _host.Architecture;

    public MachineState GetState() => _host.GetState();

    public void RunFrame() => _coordinator.Step(_hostCyclesPerFrame);

    public void StepInstruction()
    {
        // Advance the host one instruction, then let the coordinator pull the
        // peripheral machines up to the host's new cycle count so the rig stays
        // in lockstep (IEC accesses already sync lazily during the step).
        _host.StepInstruction();
        _coordinator.SynchronizePeripheralSystemsToHost();
    }

    public void Reset() => _coordinator.Reset();
}
