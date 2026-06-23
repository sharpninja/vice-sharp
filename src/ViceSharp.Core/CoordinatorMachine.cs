using System.Collections.Generic;
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

    /// <summary>
    /// The rig's per-CPU roster: the host first (system 0) then each peripheral system's CPU,
    /// each carrying its own ExecutedCycles and clock so the status surface lists every CPU -
    /// host and each drive - distinctly.
    /// </summary>
    public IReadOnlyList<CpuInfo> CpuInfos
    {
        get
        {
            var roster = new List<CpuInfo>(_coordinator.Systems.Count);
            foreach (var system in _coordinator.Systems)
            {
                if (system.PrimaryCpu is { } cpu)
                    roster.Add(new CpuInfo(system.Architecture.MachineName, cpu.ExecutedCycles, system.Clock.FrequencyHz));
            }

            return roster;
        }
    }

    public IDeviceRegistry Devices => _host.Devices;

    public IArchitectureDescriptor Architecture => _host.Architecture;

    public MachineState GetState() => _host.GetState();

    public void RunFrame() => _coordinator.Step(_hostCyclesPerFrame);

    public void StepInstruction()
    {
        // Advance the host one instruction only. Peripheral CPUs (the drive 6502) are NOT
        // stepped in lockstep here: each runs on its own clock and is coupled to the host
        // solely through the async IEC bus - the drive catches up lazily whenever the host's
        // CIA2 reads or writes an IEC line (C64Cia2InterfaceDevice.synchronizeIec), and ATN
        // edges reach the drive's VIA CA1 via the bus LineChanged event. This is VICE's own
        // model (drive_cpu_execute is driven from the IEC callbacks), and removing the rigid
        // per-instruction catch-up is what lets each CPU keep its own independent tick rate.
        _host.StepInstruction();
    }

    public void Reset() => _coordinator.Reset();
}
