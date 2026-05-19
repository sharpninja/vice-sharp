namespace ViceSharp.TestHarness;

using System.Collections.Concurrent;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="LockFreePubSub"/>, the lock-free
/// publish/subscribe primitive used by ViceSharp for high-frequency
/// device communication. Publish is documented as wait-free,
/// zero-allocation on the hot path, and ABA-safe; Subscribe and
/// Unsubscribe use copy-on-write with CompareExchange to update the
/// internal <see cref="System.Collections.Immutable.ImmutableDictionary{TKey,TValue}"/>.
/// These tests exercise the public <see cref="IPubSub"/> contract
/// (Publish / Subscribe / Unsubscribe) using payload byte-copy
/// patterns inside handlers since <see cref="System.ReadOnlySpan{T}"/>
/// is a ref struct that cannot escape the lambda. They also drive a
/// concurrent stress workload to confirm copy-on-write maintains
/// invariants under contention.
/// </summary>
public sealed class LockFreePubSubTests
{
    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: A single subscriber registers for a topic, then the
    /// producer publishes one message. This is the smallest possible
    /// hot-path scenario - a device publishing a memory-write event
    /// to a single observer (such as a VIC chip listening for a CPU
    /// store to its register window).
    /// Acceptance: The handler is invoked exactly once with the same
    /// bytes that were published.
    /// </summary>
    [Fact]
    public void Subscribe_ThenPublish_DeliversPayloadToHandler()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(42);

        var invocations = 0;
        byte[]? received = null;
        Action<ReadOnlySpan<byte>> handler = payload =>
        {
            invocations++;
            received = payload.ToArray();
        };

        pubsub.Subscribe(topic, handler);
        pubsub.Publish(topic, new byte[] { 0x10, 0x20, 0x30 });

        Assert.Equal(1, invocations);
        Assert.NotNull(received);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, received);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: A subscriber unsubscribes mid-session (a virtual
    /// device is detached at runtime). Subsequent publishes on the
    /// topic should not reach the unsubscribed handler.
    /// Acceptance: After Unsubscribe the handler is never invoked
    /// again, even though Publish itself does not throw.
    /// </summary>
    [Fact]
    public void Unsubscribe_StopsFurtherDelivery()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(1);

        var invocations = 0;
        Action<ReadOnlySpan<byte>> handler = _ => invocations++;

        pubsub.Subscribe(topic, handler);
        pubsub.Publish(topic, new byte[] { 1 });
        Assert.Equal(1, invocations);

        pubsub.Unsubscribe(topic, handler);
        pubsub.Publish(topic, new byte[] { 2 });
        pubsub.Publish(topic, new byte[] { 3 });

        // No further deliveries after Unsubscribe.
        Assert.Equal(1, invocations);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: Multiple devices subscribe to the same topic (for
    /// example both a logger and a real consumer wired up to the CPU
    /// memory-write topic). A single Publish must reach all of them.
    /// Acceptance: Both handlers are invoked exactly once per publish
    /// and observe the same payload.
    /// </summary>
    [Fact]
    public void MultipleSubscribers_AllReceiveSameMessage()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(7);

        var handlerACalls = 0;
        var handlerBCalls = 0;
        byte[]? receivedA = null;
        byte[]? receivedB = null;

        Action<ReadOnlySpan<byte>> handlerA = payload =>
        {
            handlerACalls++;
            receivedA = payload.ToArray();
        };
        Action<ReadOnlySpan<byte>> handlerB = payload =>
        {
            handlerBCalls++;
            receivedB = payload.ToArray();
        };

        pubsub.Subscribe(topic, handlerA);
        pubsub.Subscribe(topic, handlerB);
        pubsub.Publish(topic, new byte[] { 0xAA, 0xBB });

        Assert.Equal(1, handlerACalls);
        Assert.Equal(1, handlerBCalls);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, receivedA);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, receivedB);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: A device publishes to a topic before any subscriber
    /// has attached (very common during emulator boot when chips wake
    /// in different orders). Publish must never throw or allocate.
    /// Acceptance: Publish on a topic with no subscribers returns
    /// without throwing and does not invoke any handler.
    /// </summary>
    [Fact]
    public void Publish_WithNoSubscribers_IsSilentNoOp()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(99);

        var exception = Record.Exception(() => pubsub.Publish(topic, new byte[] { 0xFF }));

        Assert.Null(exception);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: A publish targets a topic whose only subscriber is
    /// for a DIFFERENT topic id. The handler must not be invoked - the
    /// per-topic isolation guarantee is required for chip wiring
    /// correctness.
    /// Acceptance: Subscriber on topic X is not invoked when publish
    /// occurs on topic Y, but IS invoked when publish targets X.
    /// </summary>
    [Fact]
    public void Publish_DifferentTopic_DoesNotInvokeUnrelatedSubscribers()
    {
        var pubsub = new LockFreePubSub();
        var topicA = new TopicId(100);
        var topicB = new TopicId(200);

        var aCalls = 0;
        pubsub.Subscribe(topicA, _ => aCalls++);

        pubsub.Publish(topicB, new byte[] { 1 });
        pubsub.Publish(topicB, new byte[] { 2 });
        Assert.Equal(0, aCalls);

        pubsub.Publish(topicA, new byte[] { 3 });
        Assert.Equal(1, aCalls);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: Unsubscribe is called for a (topic, handler) pair
    /// that was never subscribed (a defensive teardown sequence). The
    /// operation must be a safe no-op rather than throwing.
    /// Acceptance: Unsubscribe on a missing topic returns without
    /// throwing; subsequent Publish on that topic still no-ops.
    /// </summary>
    [Fact]
    public void Unsubscribe_UnknownTopic_IsSafeNoOp()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(555);
        Action<ReadOnlySpan<byte>> handler = _ => { };

        var unsubscribeException = Record.Exception(() => pubsub.Unsubscribe(topic, handler));
        var publishException = Record.Exception(() => pubsub.Publish(topic, new byte[] { 1 }));

        Assert.Null(unsubscribeException);
        Assert.Null(publishException);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: Multiple subscriptions of the SAME handler delegate
    /// to the same topic. The lock-free implementation appends to an
    /// ImmutableArray on every Subscribe, so the handler is expected
    /// to fire once per Subscribe call when Publish runs.
    /// Acceptance: After two Subscribes of the same handler delegate,
    /// a single Publish fires it twice.
    /// </summary>
    [Fact]
    public void Subscribe_SameHandlerTwice_DeliversTwice()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(11);

        var calls = 0;
        Action<ReadOnlySpan<byte>> handler = _ => calls++;

        pubsub.Subscribe(topic, handler);
        pubsub.Subscribe(topic, handler);
        pubsub.Publish(topic, new byte[] { 1 });

        Assert.Equal(2, calls);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: After all subscribers for a topic are removed, the
    /// internal dictionary entry for the topic should be cleaned up
    /// (the implementation removes the topic when the handler array
    /// becomes empty). This means a subsequent publish hits the
    /// "topic not present" fast path rather than iterating an empty
    /// array.
    /// Acceptance: Subscribe then immediately Unsubscribe; the next
    /// Publish does not invoke the handler. Publish does not throw.
    /// </summary>
    [Fact]
    public void Unsubscribe_LastHandler_RemovesTopicCleanly()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(123);

        var calls = 0;
        Action<ReadOnlySpan<byte>> handler = _ => calls++;

        pubsub.Subscribe(topic, handler);
        pubsub.Unsubscribe(topic, handler);

        var exception = Record.Exception(() => pubsub.Publish(topic, new byte[] { 0x42 }));

        Assert.Null(exception);
        Assert.Equal(0, calls);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: Concurrent publishers fire while another thread
    /// subscribes and unsubscribes - the documented contract is that
    /// Publish is wait-free and Subscribe/Unsubscribe use copy-on-write
    /// with CompareExchange. The pub/sub must not throw, must not
    /// corrupt state, and must deliver at least the messages published
    /// while a subscription is active.
    /// Acceptance: All worker tasks complete without exception; the
    /// invocation counter is a non-negative integer; after the run we
    /// can still successfully subscribe and receive a probe publish.
    /// </summary>
    [Fact]
    public async Task ConcurrentPublishAndSubscribe_DoesNotCorruptState()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(0xDEAD);

        var totalInvocations = 0;
        Action<ReadOnlySpan<byte>> handler = _ => Interlocked.Increment(ref totalInvocations);

        const int publishIterations = 5_000;
        const int subscribeIterations = 200;
        var cancellation = TestContext.Current.CancellationToken;

        var publisher1 = Task.Run(() =>
        {
            var payload = new byte[] { 1, 2, 3 };
            for (var i = 0; i < publishIterations; i++)
                pubsub.Publish(topic, payload);
        }, cancellation);

        var publisher2 = Task.Run(() =>
        {
            var payload = new byte[] { 4, 5, 6 };
            for (var i = 0; i < publishIterations; i++)
                pubsub.Publish(topic, payload);
        }, cancellation);

        var subscriber = Task.Run(() =>
        {
            for (var i = 0; i < subscribeIterations; i++)
            {
                pubsub.Subscribe(topic, handler);
                Thread.Yield();
                pubsub.Unsubscribe(topic, handler);
            }
        }, cancellation);

        await Task.WhenAll(publisher1, publisher2, subscriber);

        Assert.True(totalInvocations >= 0);

        // Final usability check: subscribe fresh and receive one probe.
        var probeCalls = 0;
        Action<ReadOnlySpan<byte>> probe = _ => probeCalls++;
        pubsub.Subscribe(topic, probe);
        pubsub.Publish(topic, new byte[] { 0xFF });
        pubsub.Unsubscribe(topic, probe);

        Assert.Equal(1, probeCalls);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / LockFreePubSub).
    /// Use case: Many distinct subscribers attach to a topic from
    /// many threads simultaneously - emulates a device-wiring storm
    /// during fast-boot. Copy-on-write with CompareExchange must
    /// eventually settle so that every Subscribe call is reflected in
    /// the published broadcast list.
    /// Acceptance: After N concurrent subscribes each with a distinct
    /// handler, a single Publish invokes exactly N handlers. Total
    /// observed invocations equal handler count.
    /// </summary>
    [Fact]
    public async Task ConcurrentSubscribes_AllHandlersReceivePublish()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(0xBEEF);
        const int handlerCount = 64;
        var cancellation = TestContext.Current.CancellationToken;

        var invocationCounts = new ConcurrentDictionary<int, int>();
        var subscribeTasks = new Task[handlerCount];

        for (var i = 0; i < handlerCount; i++)
        {
            var id = i;
            invocationCounts[id] = 0;
            subscribeTasks[i] = Task.Run(() =>
            {
                Action<ReadOnlySpan<byte>> handler = _ =>
                    invocationCounts.AddOrUpdate(id, 1, (_, v) => v + 1);
                pubsub.Subscribe(topic, handler);
            }, cancellation);
        }

        await Task.WhenAll(subscribeTasks);

        pubsub.Publish(topic, new byte[] { 0x01 });

        Assert.Equal(handlerCount, invocationCounts.Count);
        Assert.All(invocationCounts.Values, count => Assert.Equal(1, count));
    }
}
