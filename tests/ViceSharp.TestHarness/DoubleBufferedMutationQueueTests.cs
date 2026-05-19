namespace ViceSharp.TestHarness;

using System.Reflection;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="DoubleBufferedMutationQueue"/>, the
/// bounded double-buffered lock-free primitive that records every
/// state mutation in the emulator. The queue is documented as
/// safe for single-producer / single-consumer usage and provides
/// zero-allocation enqueue once the underlying ArrayPool buffers are
/// rented. These tests exercise the public contract of
/// <see cref="IMutationQueue"/> (Enqueue / Commit / Clear) and use
/// targeted reflection to inspect internal counts and the active-buffer
/// flip on Commit. They also cover the capacity boundary (silent drop
/// once a buffer is full) and confirm the queue tolerates concurrent
/// enqueue/commit pressure without throwing or corrupting its internal
/// invariants (active-buffer always 0 or 1, counts never negative or
/// past capacity).
/// </summary>
public sealed class DoubleBufferedMutationQueueTests
{
    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: A producer records a handful of mutations, then commits.
    /// Acceptance: Each Enqueue advances the active buffer's count by
    /// one; Commit swaps to the empty back buffer (active flips from
    /// 0 -> 1) and clears the producer side count back to zero so the
    /// consumed cycle's data does not bleed into the next cycle.
    /// </summary>
    [Fact]
    public void Enqueue_ThenCommit_AdvancesCountThenSwapsAndClears()
    {
        var queue = new DoubleBufferedMutationQueue();

        queue.Enqueue(new DeviceId(1), 0xD000, 0x00, 0xFF, 0);
        queue.Enqueue(new DeviceId(2), 0xD001, 0x01, 0x02, 1);
        queue.Enqueue(new DeviceId(3), 0xD002, 0x02, 0x03, 2);

        Assert.Equal(3, GetActiveCount(queue));
        Assert.Equal(0, GetActiveBuffer(queue));

        queue.Commit();

        Assert.Equal(1, GetActiveBuffer(queue));
        // Producer side (now buffer 1) starts empty; consumer side
        // (buffer 0) still holds the 3 entries from the prior cycle
        // until the NEXT Commit clears them.
        Assert.Equal(0, GetCount(queue, bufferIndex: 1));
        Assert.Equal(3, GetCount(queue, bufferIndex: 0));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: A producer commits without enqueueing anything (idle
    /// CPU cycle that issued no memory writes).
    /// Acceptance: Commit still flips the active buffer and the
    /// internal commit counter is incremented (no-op cycles are still
    /// observable by the trim heuristic).
    /// </summary>
    [Fact]
    public void Commit_OnEmptyQueue_StillFlipsActiveBuffer()
    {
        var queue = new DoubleBufferedMutationQueue();

        Assert.Equal(0, GetActiveBuffer(queue));

        queue.Commit();
        Assert.Equal(1, GetActiveBuffer(queue));

        queue.Commit();
        Assert.Equal(0, GetActiveBuffer(queue));

        Assert.Equal(2L, GetCommitCount(queue));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: Two consecutive commits, each preceded by enqueues,
    /// model the steady-state CPU-tick / VIC-tick handoff. Commit
    /// clears the BACK buffer (consumer's side) then flips, so on
    /// every cycle the producer writes into a clean buffer and the
    /// consumer side retains the previous cycle's data until the
    /// next Commit overwrites it.
    /// Acceptance: After two cycles the active buffer is 0 again, the
    /// new producer side (buffer 0) is empty, and the back buffer
    /// (buffer 1) holds exactly the single entry enqueued during the
    /// second cycle.
    /// </summary>
    [Fact]
    public void DoubleCommit_KeepsLatestCycleOnBackBuffer()
    {
        var queue = new DoubleBufferedMutationQueue();

        queue.Enqueue(new DeviceId(1), 0x0001, 0, 1, 0);
        queue.Enqueue(new DeviceId(2), 0x0002, 0, 2, 1);
        queue.Commit();

        queue.Enqueue(new DeviceId(3), 0x0003, 0, 3, 2);
        queue.Commit();

        // After two commits we are back on buffer 0 as the active
        // (producer) side. The back buffer (1) still holds the single
        // entry from the second cycle until the next Commit clears it.
        Assert.Equal(0, GetActiveBuffer(queue));
        Assert.Equal(0, GetCount(queue, bufferIndex: 0));
        Assert.Equal(1, GetCount(queue, bufferIndex: 1));

        // One more Commit (with no Enqueue this cycle) drains everything.
        queue.Commit();
        Assert.Equal(1, GetActiveBuffer(queue));
        Assert.Equal(0, GetCount(queue, bufferIndex: 0));
        Assert.Equal(0, GetCount(queue, bufferIndex: 1));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: A misbehaving producer floods the queue past the
    /// configured capacity in a single cycle (no intervening Commit).
    /// Acceptance: The queue silently caps at capacity, dropping the
    /// overflow rather than throwing or reallocating. This is the
    /// documented back-pressure contract: emulator timing must not be
    /// disrupted by a misbehaving device.
    /// </summary>
    [Fact]
    public void Enqueue_BeyondCapacity_IsCappedAndDropsSilently()
    {
        const int capacity = 8;
        var queue = new DoubleBufferedMutationQueue(capacity);

        // Enqueue capacity + 5 entries.
        for (var i = 0; i < capacity + 5; i++)
            queue.Enqueue(new DeviceId(1), (ushort)i, 0, (byte)i, (ulong)i);

        // ArrayPool may rent a buffer larger than the requested
        // capacity; the queue silently caps at whatever the rented
        // buffer's actual length is, never past it. Verify only that
        // the active buffer's count is at most its physical length and
        // is at least the requested capacity (so requested enqueues
        // up to that point definitely succeeded).
        var activeCount = GetActiveCount(queue);
        var activeLength = GetActiveBufferLength(queue);

        Assert.True(activeCount <= activeLength);
        Assert.True(activeCount >= capacity);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: An operator hard-resets the emulator mid-frame: the
    /// mutation queue must drop everything it holds without committing.
    /// Acceptance: Clear zeroes both internal counts AND resets the
    /// active-buffer flag to 0 regardless of which buffer was active.
    /// </summary>
    [Fact]
    public void Clear_ResetsAllInternalState()
    {
        var queue = new DoubleBufferedMutationQueue();

        queue.Enqueue(new DeviceId(1), 0, 0, 1, 0);
        queue.Enqueue(new DeviceId(2), 0, 0, 2, 1);
        queue.Commit();
        queue.Enqueue(new DeviceId(3), 0, 0, 3, 2);

        // Now both buffers have data: buffer0 has the 2 pre-commit
        // entries, buffer1 has the post-commit one. Active is 1.
        Assert.Equal(1, GetActiveBuffer(queue));
        Assert.Equal(2, GetCount(queue, bufferIndex: 0));
        Assert.Equal(1, GetCount(queue, bufferIndex: 1));

        queue.Clear();

        Assert.Equal(0, GetActiveBuffer(queue));
        Assert.Equal(0, GetCount(queue, bufferIndex: 0));
        Assert.Equal(0, GetCount(queue, bufferIndex: 1));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: An operator runs Clear on a freshly-constructed queue
    /// (no Enqueue, no Commit) - this happens during emulator-session
    /// setup before any cycle has run.
    /// Acceptance: Clear is a safe no-op - it must not throw and
    /// internal state remains the documented initial state.
    /// </summary>
    [Fact]
    public void Clear_OnEmptyQueue_IsNoOp()
    {
        var queue = new DoubleBufferedMutationQueue();

        queue.Clear();

        Assert.Equal(0, GetActiveBuffer(queue));
        Assert.Equal(0, GetCount(queue, bufferIndex: 0));
        Assert.Equal(0, GetCount(queue, bufferIndex: 1));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: The trim heuristic fires every 128 commits to release
    /// over-rented ArrayPool memory back. Drive 128 empty commits and
    /// confirm the commit counter reaches exactly 128 without crashing
    /// (the trim path is private but must not throw on the default
    /// capacity where no trim actually happens).
    /// Acceptance: 128 commits complete; commit counter == 128; queue
    /// is still usable afterwards.
    /// </summary>
    [Fact]
    public void Commit_HundredAndTwentyEightTimes_FiresTrimWithoutCrash()
    {
        var queue = new DoubleBufferedMutationQueue();

        for (var i = 0; i < 128; i++)
            queue.Commit();

        Assert.Equal(128L, GetCommitCount(queue));

        // Queue is still usable after the trim path ran.
        queue.Enqueue(new DeviceId(42), 0xC000, 0x00, 0x42, 0);
        Assert.Equal(1, GetActiveCount(queue));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: Producer thread enqueues while a consumer thread
    /// repeatedly commits (SPSC pattern documented on the class). The
    /// queue must not throw or corrupt invariants under this load.
    /// Acceptance: All tasks complete without exception; active buffer
    /// flag is always 0 or 1; neither count exceeds the underlying
    /// buffer's physical length.
    /// </summary>
    [Fact]
    public async Task SingleProducerSingleConsumer_DoesNotCorruptState()
    {
        var queue = new DoubleBufferedMutationQueue();
        const int enqueueIterations = 5_000;
        const int commitIterations = 200;

        var producer = Task.Run(() =>
        {
            for (var i = 0; i < enqueueIterations; i++)
                queue.Enqueue(new DeviceId(1), (ushort)(i & 0xFFFF), 0, (byte)(i & 0xFF), (ulong)i);
        }, TestContext.Current.CancellationToken);

        var consumer = Task.Run(() =>
        {
            for (var i = 0; i < commitIterations; i++)
            {
                queue.Commit();
                Thread.Yield();
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(producer, consumer);

        // Final invariant checks.
        var active = GetActiveBuffer(queue);
        Assert.InRange(active, 0, 1);

        var count0 = GetCount(queue, bufferIndex: 0);
        var count1 = GetCount(queue, bufferIndex: 1);
        Assert.True(count0 >= 0);
        Assert.True(count1 >= 0);
        Assert.True(count0 <= GetBufferLength(queue, bufferIndex: 0));
        Assert.True(count1 <= GetBufferLength(queue, bufferIndex: 1));
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / DoubleBufferedMutationQueue).
    /// Use case: Smoke test that Enqueue does not allocate after the
    /// underlying buffers are warm. Run the same workload twice and
    /// observe that the second pass triggers no Gen0 allocations
    /// (capacity guard hit before any new entry constructor allocates).
    /// Acceptance: Zero Gen0 collections happen during the second
    /// (steady state) pass. This is a soft check - we only require
    /// the delta to be zero, not the absolute count.
    /// </summary>
    [Fact]
    public void Enqueue_SteadyState_DoesNotAllocate()
    {
        var queue = new DoubleBufferedMutationQueue();

        // Warm-up pass: rent the ArrayPool buffers and JIT the hot path.
        for (var i = 0; i < 1024; i++)
            queue.Enqueue(new DeviceId(1), (ushort)i, 0, (byte)i, (ulong)i);
        queue.Commit();
        for (var i = 0; i < 1024; i++)
            queue.Enqueue(new DeviceId(1), (ushort)i, 0, (byte)i, (ulong)i);
        queue.Commit();

        // Force GC to a known clean baseline.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var gen0Before = GC.CollectionCount(0);

        for (var i = 0; i < 1024; i++)
            queue.Enqueue(new DeviceId(1), (ushort)i, 0, (byte)i, (ulong)i);
        queue.Commit();

        var gen0After = GC.CollectionCount(0);

        Assert.Equal(gen0Before, gen0After);
    }

    private static int GetActiveBuffer(DoubleBufferedMutationQueue queue)
        => (int)GetField(queue, "_activeBuffer")!;

    private static int GetActiveCount(DoubleBufferedMutationQueue queue)
        => GetActiveBuffer(queue) == 0
            ? (int)GetField(queue, "_count0")!
            : (int)GetField(queue, "_count1")!;

    private static int GetCount(DoubleBufferedMutationQueue queue, int bufferIndex)
        => bufferIndex == 0
            ? (int)GetField(queue, "_count0")!
            : (int)GetField(queue, "_count1")!;

    private static long GetCommitCount(DoubleBufferedMutationQueue queue)
        => (long)GetField(queue, "_commitCount")!;

    private static int GetBufferLength(DoubleBufferedMutationQueue queue, int bufferIndex)
    {
        var buffer = (Array)GetField(queue, bufferIndex == 0 ? "_buffer0" : "_buffer1")!;
        return buffer.Length;
    }

    private static int GetActiveBufferLength(DoubleBufferedMutationQueue queue)
        => GetBufferLength(queue, GetActiveBuffer(queue));

    private static object? GetField(DoubleBufferedMutationQueue queue, string name)
    {
        var field = typeof(DoubleBufferedMutationQueue)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {name} not found on DoubleBufferedMutationQueue");
        return field.GetValue(queue);
    }
}
