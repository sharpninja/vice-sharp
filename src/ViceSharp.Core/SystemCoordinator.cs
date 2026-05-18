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
    private readonly Dictionary<IMachine, long> _accumulators = new();
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
        _accumulators[machine] = 0;
    }

    public void AttachCartExtension(IMachine extension, IMachine host)
    {
        if (!_systems.Contains(host))
            throw new InvalidOperationException("Host machine is not attached.");
        if (_systems.Contains(extension))
            throw new InvalidOperationException("Extension already attached.");
        _systems.Add(extension);
        _accumulators[extension] = 0;
        _cartExtensions.Add(extension);
    }

    public void DetachSystem(IMachine machine)
    {
        if (!_systems.Remove(machine))
            throw new InvalidOperationException("Machine is not attached.");
        _accumulators.Remove(machine);
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
        var hostRate = host.Clock.FrequencyHz;

        foreach (var machine in _systems)
        {
            if (ReferenceEquals(machine, host) || _cartExtensions.Contains(machine))
            {
                machine.Clock.Step();
                continue;
            }

            var rate = machine.Clock.FrequencyHz;
            var acc = _accumulators[machine] + rate;
            while (acc >= hostRate)
            {
                machine.Clock.Step();
                acc -= hostRate;
            }
            _accumulators[machine] = acc;
        }

        _hostCycles++;
    }

    public void Step(long hostCycles)
    {
        for (long i = 0; i < hostCycles; i++)
            Step();
    }

    public void Reset()
    {
        _hostCycles = 0;
        foreach (var machine in _systems)
        {
            machine.Reset();
            _accumulators[machine] = 0;
        }
    }
}
