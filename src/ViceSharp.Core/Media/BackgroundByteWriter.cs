namespace ViceSharp.Core.Media;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// FR-MED-004 (review finding: blocking socket/file I/O on the emulation worker).
/// Decouples the producer (the emulation worker enqueuing a copy of each frame or
/// audio batch) from the slow consumer (the actual socket/file write) by running
/// the writes on a dedicated background thread. Payload buffers are rented from
/// <see cref="ArrayPool{T}"/> so steady-state recording adds no per-write GC churn
/// (also addressing the per-WriteSamples allocation finding).
///
/// The queue is bounded: when the consumer cannot keep up, <see cref="Enqueue"/>
/// applies back-pressure (blocks the producer) instead of growing unboundedly or
/// dropping data. A consumer write that throws latches <see cref="Faulted"/> and
/// is never surfaced to the producer.
/// </summary>
public sealed class BackgroundByteWriter : IDisposable
{
    private readonly record struct Chunk(byte[] Buffer, int Length);

    private readonly BlockingCollection<Chunk> _queue;
    private readonly Action<byte[], int> _write;
    private readonly Thread _worker;
    private readonly bool _dropWhenFull;
    private volatile bool _faulted;

    /// <param name="write">Consumer write: receives a rented buffer and the valid length.</param>
    /// <param name="capacity">Bounded queue depth.</param>
    /// <param name="name">Background-thread name (diagnostics).</param>
    /// <param name="dropWhenFull">
    /// When <see langword="true"/> a full queue drops the overflow payload (counted in
    /// <see cref="DroppedCount"/>) instead of blocking the producer. This is required
    /// for any writer fed from the real-time emulation worker: a stalled consumer
    /// (e.g. an ffmpeg socket that stops draining) must degrade the recording, never
    /// freeze the emulator. When <see langword="false"/> (the default) a full queue
    /// blocks the producer (back-pressure), preserving every payload.
    /// </param>
    public BackgroundByteWriter(Action<byte[], int> write, int capacity, string name, bool dropWhenFull = false)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _write = write;
        _dropWhenFull = dropWhenFull;
        _queue = new BlockingCollection<Chunk>(capacity);
        _worker = new Thread(Run)
        {
            IsBackground = true,
            Name = name,
        };
        _worker.Start();
    }

    /// <summary>True once a consumer write has thrown; further writes are skipped.</summary>
    public bool Faulted => _faulted;

    /// <summary>The first exception thrown by the consumer write, if any (diagnostics).</summary>
    public Exception? FaultException { get; private set; }

    private long _enqueuedCount;
    private long _writtenCount;
    private long _droppedCount;

    /// <summary>Diagnostics: payloads accepted into the queue.</summary>
    public long EnqueuedCount => Interlocked.Read(ref _enqueuedCount);

    /// <summary>Diagnostics: payloads written by the consumer.</summary>
    public long WrittenCount => Interlocked.Read(ref _writtenCount);

    /// <summary>Diagnostics: payloads dropped because the queue was full (drop-on-full only).</summary>
    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    /// <summary>
    /// Copies <paramref name="data"/> into a pooled buffer and enqueues it for the
    /// background writer. With the default policy this blocks when the queue is full
    /// (back-pressure); with <c>dropWhenFull</c> it drops the overflow (counted in
    /// <see cref="DroppedCount"/>) and returns immediately, so the producer never
    /// blocks. A no-op once faulted or completed.
    /// </summary>
    public void Enqueue(ReadOnlySpan<byte> data)
    {
        if (_faulted || _queue.IsAddingCompleted || data.IsEmpty)
            return;

        var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        data.CopyTo(buffer);
        try
        {
            if (_dropWhenFull)
            {
                // Non-blocking add: a full queue means the consumer is not keeping up
                // (e.g. a stalled ffmpeg socket). Drop the payload rather than stall
                // the emulation worker - a frozen emulator is worse than a gap in the
                // recording.
                if (_queue.TryAdd(new Chunk(buffer, data.Length)))
                {
                    Interlocked.Increment(ref _enqueuedCount);
                }
                else
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    Interlocked.Increment(ref _droppedCount);
                }
            }
            else
            {
                _queue.Add(new Chunk(buffer, data.Length));
                Interlocked.Increment(ref _enqueuedCount);
            }
        }
        catch (InvalidOperationException)
        {
            // Adding completed between the check and the Add/TryAdd; return the buffer.
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void Run()
    {
        try
        {
            foreach (var chunk in _queue.GetConsumingEnumerable())
            {
                try
                {
                    if (!_faulted)
                    {
                        _write(chunk.Buffer, chunk.Length);
                        System.Threading.Interlocked.Increment(ref _writtenCount);
                    }
                }
                catch (Exception ex)
                {
                    FaultException ??= ex;
                    _faulted = true;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(chunk.Buffer);
                }
            }
        }
        catch
        {
            _faulted = true;
        }
    }

    /// <summary>
    /// Signals end-of-input and waits (up to <paramref name="timeout"/>) for the
    /// background writer to flush every queued payload and exit. Idempotent.
    /// </summary>
    public void CompleteAndJoin(TimeSpan timeout)
    {
        try { _queue.CompleteAdding(); } catch (ObjectDisposedException) { }
        try { _worker.Join(timeout); } catch { /* best effort */ }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        CompleteAndJoin(TimeSpan.FromSeconds(5));
        // Return any buffers the consumer never reached.
        while (_queue.TryTake(out var chunk))
            ArrayPool<byte>.Shared.Return(chunk.Buffer);
        _queue.Dispose();
    }
}
