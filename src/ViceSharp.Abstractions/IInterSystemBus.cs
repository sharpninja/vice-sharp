namespace ViceSharp.Abstractions;

/// <summary>
/// A protocol-quantized signal bus bridging multiple IMachine instances.
/// Wired-OR (open-collector) model: a line is high only when no endpoint
/// pulls it low. IEC, user-port, and tape-port buses all conform.
/// </summary>
public interface IInterSystemBus
{
    /// <summary>Human-readable bus name (e.g. "IEC", "UserPort").</summary>
    string Name { get; }

    /// <summary>The set of named signal lines this bus carries.</summary>
    IReadOnlyList<string> Signals { get; }

    /// <summary>Register an endpoint on this bus. Returns an endpoint handle.</summary>
    IBusEndpoint AttachEndpoint(string endpointName);

    /// <summary>Detach a previously registered endpoint.</summary>
    void DetachEndpoint(IBusEndpoint endpoint);

    /// <summary>Resolved line state - true = high, false = low (any endpoint pulling low).</summary>
    bool ReadLine(string signal);

    /// <summary>Fired when a line transitions (any endpoint pull or release changing the resolved state).</summary>
    event EventHandler<BusEdgeEventArgs>? LineChanged;

    /// <summary>
    /// Capture a read-only snapshot of the whole bus at this instant: every
    /// line's resolved level and which attached endpoints are pulling it low
    /// (i.e. who is "talking"), plus the list of all attached endpoints. Pure
    /// observation - taking a snapshot never changes bus state.
    /// </summary>
    BusSnapshot Snapshot();
}

/// <summary>
/// Point-in-time view of an <see cref="IInterSystemBus"/>: the state of every
/// line and which endpoints are asserting it. Used to spy on bus traffic
/// (e.g. an IEC transfer) without perturbing it.
/// </summary>
public sealed class BusSnapshot
{
    public BusSnapshot(string busName, IReadOnlyList<BusLineSnapshot> lines, IReadOnlyList<string> endpoints)
    {
        BusName = busName;
        Lines = lines;
        Endpoints = endpoints;
    }

    /// <summary>The bus this snapshot was taken from (e.g. "IEC").</summary>
    public string BusName { get; }

    /// <summary>Per-line resolved level and the endpoints pulling each line low.</summary>
    public IReadOnlyList<BusLineSnapshot> Lines { get; }

    /// <summary>Names of every endpoint currently attached to the bus.</summary>
    public IReadOnlyList<string> Endpoints { get; }

    /// <summary>Endpoints pulling at least one line low - i.e. devices actively driving the bus.</summary>
    public IReadOnlyList<string> TalkingEndpoints =>
        Lines.SelectMany(line => line.Pullers).Distinct().ToList();
}

/// <summary>One line within a <see cref="BusSnapshot"/>.</summary>
public readonly record struct BusLineSnapshot(string Signal, bool IsHigh, IReadOnlyList<string> Pullers)
{
    /// <summary>True when at least one endpoint is pulling this line low.</summary>
    public bool IsAsserted => !IsHigh;
}

/// <summary>An endpoint handle for an attached system on an IInterSystemBus.</summary>
public interface IBusEndpoint
{
    /// <summary>Endpoint name as supplied at attach time.</summary>
    string Name { get; }

    /// <summary>
    /// Pull a signal line. <paramref name="low"/> = true pulls the line low (asserts).
    /// <paramref name="low"/> = false releases this endpoint's pull. Other endpoints
    /// pulling the same line keep it low (wired-OR).
    /// </summary>
    void Pull(string signal, bool low);

    /// <summary>Resolved line state as seen by this endpoint.</summary>
    bool ReadLine(string signal);
}

/// <summary>Edge transition event payload for IInterSystemBus.LineChanged.</summary>
public sealed class BusEdgeEventArgs : EventArgs
{
    public BusEdgeEventArgs(string signal, bool newState)
    {
        Signal = signal;
        NewState = newState;
    }

    /// <summary>Signal name that transitioned.</summary>
    public string Signal { get; }

    /// <summary>New resolved state - true = high, false = low.</summary>
    public bool NewState { get; }
}
