namespace ViceSharp.Abstractions;

/// <summary>
/// Owns N IMachine instances and drives each at its own clock rate. Host
/// machine is the rate reference. Cart-port extension machines share the
/// host clock (phi2-locked). Independent peripherals (drives, user-port
/// peers, datasette) run on their own clocks; an IInterSystemBus bridges
/// signal-level state across machines.
/// </summary>
public interface ISystemCoordinator
{
    /// <summary>Total host cycles advanced since last Reset.</summary>
    long TotalHostCycles { get; }

    /// <summary>Attached machines in attach order. First attached is the host.</summary>
    IReadOnlyList<IMachine> Systems { get; }

    /// <summary>Buses bridging signals across attached machines.</summary>
    IReadOnlyList<IInterSystemBus> Buses { get; }

    /// <summary>
    /// Attach a machine running on its own independent clock. Frequency is
    /// taken from machine.Clock.FrequencyHz; the coordinator advances this
    /// machine proportionally each host cycle using a fractional accumulator.
    /// First attached machine is the host (rate reference).
    /// </summary>
    void AttachSystem(IMachine machine);

    /// <summary>
    /// Attach a machine as a cart-port extension that shares the host's
    /// clock (phi2-locked). The extension is advanced exactly once per host
    /// cycle, regardless of its own FrequencyHz value.
    /// </summary>
    void AttachCartExtension(IMachine extension, IMachine host);

    /// <summary>Detach an attached machine. Throws if not attached.</summary>
    void DetachSystem(IMachine machine);

    /// <summary>Attach an inter-system bus. Throws if already attached.</summary>
    void AttachBus(IInterSystemBus bus);

    /// <summary>Advance the coordinator by one host cycle.</summary>
    void Step();

    /// <summary>Advance the coordinator by the specified host cycles.</summary>
    void Step(long hostCycles);

    /// <summary>Reset all attached machines and host cycle count.</summary>
    void Reset();
}
