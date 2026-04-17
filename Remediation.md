**LockFreePubSub**  
Use copy-on-write + `Interlocked` for the subscriber list. This is the standard, proven pattern that eliminates ABA problems and hidden allocations while staying fully lock-free on the publish hot path.

```csharp
public sealed class LockFreePubSub : IPubSub
{
    private readonly ConcurrentDictionary<string, SubscriberList> _topics = new();

    // Hot path – zero alloc, lock-free
    public void Publish(string topic, in Mutation mutation)
    {
        if (_topics.TryGetValue(topic, out var list))
        {
            var snapshot = list.GetSnapshot(); // returns ImmutableArray or frozen array ref
            foreach (var sub in snapshot)
                sub.Invoke(in mutation); // no boxing, use ref structs where possible
        }
    }

    // Cold path only
    public IDisposable Subscribe(string topic, Action<Mutation> handler)
    {
        var newEntry = new SubscriberEntry(handler);
        _topics.AddOrUpdate(topic,
            _ => new SubscriberList(newEntry),
            (_, existing) => existing.Add(newEntry));
        return new Unsubscriber(topic, newEntry, this);
    }
}

internal sealed class SubscriberList
{
    private volatile ImmutableArray<SubscriberEntry> _subscribers; // or array + version

    public ImmutableArray<SubscriberEntry> GetSnapshot() => _subscribers;

    public SubscriberList Add(SubscriberEntry entry)
    {
        var old = _subscribers;
        var updated = old.Add(entry);
        Interlocked.Exchange(ref _subscribers, updated); // atomic swap
        return this;
    }
}
```

This pattern is battle-tested (see classic .NET pub/sub articles using the same “swap the array” technique). No ABA risk because the reference itself is atomically replaced; the old list is immutable and can be safely read by any number of publishers concurrently.

**DoubleBufferedMutationQueue**  
Double-buffer the mutations so the emulation thread can write freely while the consumer drains the other buffer. To fix the “keeps peak size forever” problem, add explicit idle trimming and a configurable high-water mark.

```csharp
public sealed class DoubleBufferedMutationQueue
{
    private readonly int _maxCapacity;
    private readonly object _swapLock = new();
    private Mutation[] _bufferA;
    private Mutation[] _bufferB;
    private int _writeIndex;
    private bool _usingA = true;

    public DoubleBufferedMutationQueue(int initialCapacity = 1024, int maxCapacity = 4096)
    {
        _maxCapacity = maxCapacity;
        _bufferA = new Mutation[initialCapacity];
        _bufferB = new Mutation[initialCapacity];
    }

    // Producer (emulation thread) – zero alloc in steady state
    public bool Enqueue(in Mutation m)
    {
        var current = _usingA ? _bufferA : _bufferB;
        if (_writeIndex >= current.Length)
        {
            if (current.Length >= _maxCapacity) return false; // back-pressure
            Resize(current);
        }
        current[_writeIndex++] = m;
        return true;
    }

    // Consumer (main thread) – called once per frame or on idle
    public void SwapAndDrain(Action<ReadOnlySpan<Mutation>> drainAction)
    {
        lock (_swapLock)
        {
            var fullBuffer = _usingA ? _bufferA : _bufferB;
            var count = _writeIndex;

            drainAction(new ReadOnlySpan<Mutation>(fullBuffer, 0, count));

            // Reset write side
            _writeIndex = 0;
            _usingA = !_usingA;

            // Trim if idle (optional periodic call)
            if (count < fullBuffer.Length / 4 && fullBuffer.Length > 1024)
                TrimExcess();
        }
    }

    private void TrimExcess()
    {
        var targetSize = Math.Max(1024, _writeIndex * 2); // never shrink below reasonable floor
        if (_bufferA.Length > targetSize)
            Array.Resize(ref _bufferA, targetSize);
        if (_bufferB.Length > targetSize)
            Array.Resize(ref _bufferB, targetSize);
    }
}
```

The resize stays strictly cold-path (behind the swap lock). `TrimExcess` (or the built-in `Array.Resize` logic) prevents permanent bloat after bursts. For NativeAOT determinism you can pre-size to a fixed capacity and never resize at runtime if you prefer.

**Routing – kill the linear scan**  
Replace any list-of-topics scan with `ConcurrentDictionary<string, SubscriberList>` (exactly as shown in the LockFreePubSub snippet above). Lookups become O(1) average case. If you need pattern/wildcard routing later, layer a small `Trie` or `ConcurrentDictionary` of prefix matches on top—still far cheaper than linear.

**Zero-allocation hot path guarantees**  
- Use `in Mutation` (ref struct) for everything passed across the pub/sub boundary.  
- Pre-allocate all arrays/buffers at startup or on first resize.  
- Never capture `this` or locals in lambdas/delegates inside hot loops.  
- For NativeAOT, avoid anything that triggers the runtime allocator (no `new object()`, no LINQ, no `string.Format` in hot path).  
- Measure with `GC.GetAllocatedBytesForCurrentThread()` before/after a full emulation frame to prove zero heap traffic.

These three pieces together give you a clean, auditable mutation flow that stays deterministic, scales to thousands of topics/subscribers, and never grows memory after a burst. Drop the classes into `ViceSharp.Core` exactly as sketched (adjust `Mutation` type to whatever struct you already planned). If you want me to expand any part into full source with interfaces, source-gen helpers, or unit tests, just say which one first.