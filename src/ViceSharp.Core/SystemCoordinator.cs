using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Default ISystemCoordinator implementation. Owns N machines, advances each
/// at its own clock rate using a fractional accumulator anchored to the host
/// (first attached) machine's FrequencyHz. Cart-port extension machines share
/// the host clock and are stepped exactly once per host cycle.
/// </summary>
public sealed class SystemCoordinator : ISystemCoordinator
{
    private readonly List<IMachine> _systems = new();
    private readonly List<IInterSystemBus> _buses = new();
    private readonly HashSet<IMachine> _cartExtensions = new();
    private long _hostCycles;

    public long TotalHostCycles => _hostCycles;

    public IReadOnlyList<IMachine> Systems => _systems;

    public IReadOnlyList<IInterSystemBus> Buses => _buses;

    public void AttachSystem(IMachine machine)
    {
        if (_systems.Contains(machine))
            throw new InvalidOperationException("Machine already attached.");
        _systems.Add(machine);
    }

    public void AttachCartExtension(IMachine extension, IMachine host)
    {
        if (!_systems.Contains(host))
            throw new InvalidOperationException("Host machine is not attached.");
        if (_systems.Contains(extension))
            throw new InvalidOperationException("Extension already attached.");
        _systems.Add(extension);
        _cartExtensions.Add(extension);
    }

    public void DetachSystem(IMachine machine)
    {
        if (!_systems.Remove(machine))
            throw new InvalidOperationException("Machine is not attached.");
        _cartExtensions.Remove(machine);
    }

    public void AttachBus(IInterSystemBus bus)
    {
        if (_buses.Contains(bus))
            throw new InvalidOperationException("Bus already attached.");
        _buses.Add(bus);
    }

    public void Step()
    {
        if (_systems.Count == 0)
        {
            _hostCycles++;
            return;
        }

        var host = _systems[0];
        host.Clock.Step();
        foreach (var machine in _cartExtensions)
        {
            machine.Clock.Step();
        }

        _hostCycles = host.Clock.TotalCycles;
        SynchronizePeripheralSystemsToHost();
    }

    public void Step(long hostCycles)
    {
        for (long i = 0; i < hostCycles; i++)
            Step();
    }

    /// <summary>
    /// Advances non-host, non-cart systems to the host clock's current cycle.
    /// VICE calls drive_cpu_execute_* from IEC callbacks before reading or
    /// mutating serial bus state; this method gives board-level IEC glue the
    /// same catch-up point without moving that policy into chip cores.
    /// </summary>
    public void SynchronizePeripheralSystemsToHost()
    {
        if (_systems.Count == 0)
            return;

        var host = _systems[0];
        var hostRate = host.Clock.FrequencyHz;
        var hostCycles = host.Clock.TotalCycles;

        for (var i = 1; i < _systems.Count; i++)
        {
            var machine = _systems[i];
            if (_cartExtensions.Contains(machine))
                continue;

            var targetCycles = hostCycles * machine.Clock.FrequencyHz / hostRate;
            while (machine.Clock.TotalCycles < targetCycles)
                machine.Clock.Step();
        }
    }

    public void Reset()
    {
        _hostCycles = 0;
        foreach (var machine in _systems)
        {
            machine.Reset();
        }
    }
}
