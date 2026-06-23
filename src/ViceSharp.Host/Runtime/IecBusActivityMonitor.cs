using ViceSharp.Abstractions;

namespace ViceSharp.Host.Runtime;

public sealed class IecBusActivityMonitor
{
    private readonly IInterSystemBus _bus;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private DateTimeOffset _lastActivityUtc;
    private long _transitionCount;

    public IecBusActivityMonitor(
        IInterSystemBus bus,
        TimeSpan? activeHold = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(bus);

        _bus = bus;
        ActiveHold = activeHold ?? TimeSpan.FromMilliseconds(500);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _bus.LineChanged += OnLineChanged;
    }

    public TimeSpan ActiveHold { get; }

    public long TransitionCount
    {
        get
        {
            lock (_gate)
                return _transitionCount;
        }
    }

    public DateTimeOffset? LastActivityUtc
    {
        get
        {
            lock (_gate)
                return _transitionCount == 0 ? null : _lastActivityUtc;
        }
    }

    public bool IsActive
    {
        get
        {
            if (HasLowLine())
                return true;

            lock (_gate)
            {
                if (_transitionCount == 0)
                    return false;

                return _timeProvider.GetUtcNow() - _lastActivityUtc <= ActiveHold;
            }
        }
    }

    public string ActivityState => IsActive ? "Active" : "Idle";

    /// <summary>
    /// Capture the bus's current per-line resolved state and pullers for the IEC monitor panel.
    /// Pure observation; callers that race the emulation worker should hold the session lock.
    /// </summary>
    public BusSnapshot Snapshot() => _bus.Snapshot();

    private bool HasLowLine()
    {
        foreach (var signal in _bus.Signals)
        {
            if (!_bus.ReadLine(signal))
                return true;
        }

        return false;
    }

    private void OnLineChanged(object? sender, BusEdgeEventArgs e)
    {
        lock (_gate)
        {
            _transitionCount++;
            _lastActivityUtc = _timeProvider.GetUtcNow();
        }
    }
}
