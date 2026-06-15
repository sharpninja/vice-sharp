using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Records a cycle-stamped trace of an <see cref="IInterSystemBus"/> for the
/// IEC bus monitor / scope. Captures an entry on every line edge plus an entry
/// at each step boundary (so idle stretches and the "now" cursor render), into
/// a bounded ring buffer. Pure observation - recording never drives or reads
/// the bus in a way that changes resolved state, so it is parity-safe.
///
/// The recorder is inactive until <see cref="Start"/> is called and unsubscribes
/// on <see cref="Stop"/>/<see cref="Dispose"/>, so it costs nothing when the
/// monitor is closed. Cycle stamps come from the supplied provider (the master
/// clock's cycle count).
/// </summary>
public sealed class IecBusTraceRecorder : IDisposable
{
    /// <summary>Sample recorded because a bus line transitioned.</summary>
    public const string EdgeKind = "edge";

    /// <summary>Sample recorded at an emulator step boundary.</summary>
    public const string StepKind = "step";

    private readonly IInterSystemBus _bus;
    private readonly Func<long> _cycleProvider;
    private readonly int _capacity;
    private readonly Queue<BusTraceSample> _samples;
    private bool _recording;

    public IecBusTraceRecorder(IInterSystemBus bus, Func<long> cycleProvider, int capacity = 8192)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(cycleProvider);
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

        _bus = bus;
        _cycleProvider = cycleProvider;
        _capacity = capacity;
        _samples = new Queue<BusTraceSample>(capacity);
    }

    /// <summary>True while subscribed to bus edges.</summary>
    public bool IsRecording => _recording;

    /// <summary>Number of samples currently held in the ring.</summary>
    public int Count => _samples.Count;

    /// <summary>Maximum samples retained before the oldest are evicted.</summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Begin recording: subscribe to bus edges and capture a baseline step
    /// sample of the current bus state. Idempotent.
    /// </summary>
    public void Start()
    {
        if (_recording)
            return;

        _recording = true;
        _bus.LineChanged += OnLineChanged;
        Append(StepKind, signal: null);
    }

    /// <summary>Stop recording and unsubscribe. Idempotent. Retains captured samples.</summary>
    public void Stop()
    {
        if (!_recording)
            return;

        _bus.LineChanged -= OnLineChanged;
        _recording = false;
    }

    /// <summary>
    /// Record a step-boundary sample of the current bus state, regardless of
    /// whether any line changed. Used after each emulator step so the trace has
    /// a sample at the cursor even across idle cycles.
    /// </summary>
    public void MarkStep()
    {
        if (_recording)
            Append(StepKind, signal: null);
    }

    /// <summary>Captured samples in chronological order (oldest first).</summary>
    public IReadOnlyList<BusTraceSample> GetSamples() => _samples.ToArray();

    /// <summary>Captured samples at or after <paramref name="cycle"/>, chronological.</summary>
    public IReadOnlyList<BusTraceSample> Since(long cycle)
    {
        var result = new List<BusTraceSample>();
        foreach (var sample in _samples)
        {
            if (sample.Cycle >= cycle)
                result.Add(sample);
        }

        return result;
    }

    /// <summary>Drop all captured samples.</summary>
    public void Clear() => _samples.Clear();

    public void Dispose() => Stop();

    private void OnLineChanged(object? sender, BusEdgeEventArgs e) => Append(EdgeKind, e.Signal);

    private void Append(string kind, string? signal)
    {
        while (_samples.Count >= _capacity)
            _samples.Dequeue();

        _samples.Enqueue(new BusTraceSample(_cycleProvider(), kind, signal, _bus.Snapshot()));
    }
}

/// <summary>
/// One cycle-stamped entry in an <see cref="IecBusTraceRecorder"/> trace: the
/// master-clock cycle, why it was captured (<see cref="IecBusTraceRecorder.EdgeKind"/>
/// or <see cref="IecBusTraceRecorder.StepKind"/>), the line that changed (for
/// edges), and the full bus state at that instant.
/// </summary>
public readonly record struct BusTraceSample(long Cycle, string Kind, string? Signal, BusSnapshot Bus);
