using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class InterruptLine : IInterruptLine
{
    private readonly HashSet<DeviceId> _sources = new();
    private bool _risingEdge;

    public bool IsAsserted => _sources.Count > 0;
    public InterruptType Type { get; }

    public InterruptLine(InterruptType type)
    {
        Type = type;
    }

    public void Assert(IInterruptSource source)
    {
        // VICE interrupt_set_irq: irq_clk updates only when nirq goes 0 to 1.
        if (_sources.Add(source.SourceId) && _sources.Count == 1)
            _risingEdge = true;
    }

    public void Release(IInterruptSource source) => _sources.Remove(source.SourceId);
    public void Clear()
    {
        _sources.Clear();
        _risingEdge = false;
    }

    /// <summary>
    /// Consumes a combined-line 0 to 1 edge (VICE <c>nirq</c> 0 to 1).
    /// Same-cycle Release then Assert still reports the edge even if
    /// <see cref="IsAsserted"/> is true at poll time.
    /// </summary>
    public bool ConsumeRisingEdge()
    {
        if (!_risingEdge)
            return false;
        _risingEdge = false;
        return true;
    }

    /// <summary>
    /// Drops a 0-to-1 edge without releasing the line. Replay already
    /// recorded when that edge happened.
    /// </summary>
    internal void AcknowledgeRisingEdge() => _risingEdge = false;

    // Clock snapshots preserve the physical combined line as well as its
    // unconsumed edge; replaying chip registers cannot reconstruct either.
    internal bool PendingRisingEdge => _risingEdge;
    internal uint[] CaptureSources() => _sources.Select(source => source.Value).Order().ToArray();

    internal void RestoreSources(ReadOnlySpan<uint> sources, bool risingEdge)
    {
        _sources.Clear();
        foreach (var source in sources)
            _sources.Add(new DeviceId(source));
        _risingEdge = risingEdge;
    }
}
