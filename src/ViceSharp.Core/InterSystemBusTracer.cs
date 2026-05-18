using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Subscribes to an <see cref="IInterSystemBus"/> and records every
/// LineChanged event with a monotonic event index. Diagnostic tool for
/// observing protocol-level traffic - useful for verifying that a real
/// running machine actually drives the bus, and for visualizing bus
/// timing in tests.
///
/// The recorded events list grows unbounded; for long captures use
/// <see cref="MaxEvents"/> or call <see cref="Reset"/> periodically.
/// </summary>
public sealed class InterSystemBusTracer
{
    private readonly List<TraceEvent> _events = new();
    private readonly IInterSystemBus _bus;
    private long _eventIndex;
    private bool _attached;

    public InterSystemBusTracer(IInterSystemBus bus, int maxEvents = 100_000)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        MaxEvents = maxEvents;
    }

    /// <summary>Cap on retained events; older events are dropped beyond this.</summary>
    public int MaxEvents { get; }

    /// <summary>All captured events in chronological order.</summary>
    public IReadOnlyList<TraceEvent> Events => _events;

    /// <summary>Begin recording. Idempotent.</summary>
    public void Attach()
    {
        if (_attached) return;
        _bus.LineChanged += OnLineChanged;
        _attached = true;
    }

    /// <summary>Stop recording. Idempotent.</summary>
    public void Detach()
    {
        if (!_attached) return;
        _bus.LineChanged -= OnLineChanged;
        _attached = false;
    }

    /// <summary>Clear the captured events list (does not affect Attach state).</summary>
    public void Reset()
    {
        _events.Clear();
        _eventIndex = 0;
    }

    /// <summary>Count of events captured for a specific signal.</summary>
    public int CountFor(string signal)
        => _events.Count(e => e.Signal == signal);

    private void OnLineChanged(object? sender, BusEdgeEventArgs e)
    {
        if (_events.Count >= MaxEvents)
            _events.RemoveAt(0);
        _events.Add(new TraceEvent(_eventIndex++, e.Signal, e.NewState));
    }

    /// <summary>A single bus edge capture.</summary>
    public readonly record struct TraceEvent(long Index, string Signal, bool NewState);
}
