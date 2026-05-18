using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Default IInterSystemBus implementation. Wired-OR (open-collector) model:
/// each line is high when no endpoint pulls it low. LineChanged fires only on
/// resolved-state transitions.
/// </summary>
public sealed class InterSystemBus : IInterSystemBus
{
    private readonly List<Endpoint> _endpoints = new();
    private readonly Dictionary<string, bool> _resolved = new();

    public InterSystemBus(string name, IReadOnlyList<string> signals)
    {
        Name = name;
        Signals = signals;
        foreach (var s in signals)
            _resolved[s] = true;
    }

    public string Name { get; }

    public IReadOnlyList<string> Signals { get; }

    public event EventHandler<BusEdgeEventArgs>? LineChanged;

    public IBusEndpoint AttachEndpoint(string endpointName)
    {
        var ep = new Endpoint(this, endpointName);
        _endpoints.Add(ep);
        return ep;
    }

    public void DetachEndpoint(IBusEndpoint endpoint)
    {
        if (endpoint is not Endpoint ep || !_endpoints.Remove(ep))
            throw new InvalidOperationException("Endpoint is not attached to this bus.");
        Recompute();
    }

    public bool ReadLine(string signal)
    {
        if (!_resolved.TryGetValue(signal, out var value))
            throw new ArgumentException($"Unknown signal '{signal}'.", nameof(signal));
        return value;
    }

    internal void Recompute()
    {
        foreach (var signal in Signals)
        {
            var resolved = true;
            foreach (var ep in _endpoints)
            {
                if (ep.IsPullingLow(signal))
                {
                    resolved = false;
                    break;
                }
            }

            var previous = _resolved[signal];
            if (previous != resolved)
            {
                _resolved[signal] = resolved;
                LineChanged?.Invoke(this, new BusEdgeEventArgs(signal, resolved));
            }
        }
    }

    private sealed class Endpoint : IBusEndpoint
    {
        private readonly InterSystemBus _bus;
        private readonly HashSet<string> _pulled = new();

        public Endpoint(InterSystemBus bus, string name)
        {
            _bus = bus;
            Name = name;
        }

        public string Name { get; }

        public bool IsPullingLow(string signal) => _pulled.Contains(signal);

        public void Pull(string signal, bool low)
        {
            if (!_bus.Signals.Contains(signal))
                throw new ArgumentException($"Unknown signal '{signal}'.", nameof(signal));
            var changed = low ? _pulled.Add(signal) : _pulled.Remove(signal);
            if (changed)
                _bus.Recompute();
        }

        public bool ReadLine(string signal) => _bus.ReadLine(signal);
    }
}
