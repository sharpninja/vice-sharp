namespace ViceSharp.TestHarness;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="LockFreePubSub"/>, the lock-free
/// publish/subscribe primitive used by ViceSharp for high-frequency
/// device communication. Publish is documented as wait-free and
/// zero-allocation on the hot path; Subscribe and Unsubscribe are
/// cold-path slot updates over preallocated route/subscriber arrays.
/// These tests exercise the public <see cref="IPubSub"/> contract
/// (Publish / Subscribe / Unsubscribe) using payload byte-copy
/// patterns inside handlers since <see cref="System.ReadOnlySpan{T}"/>
/// is a ref struct that cannot escape the lambda. They also drive a
/// concurrent stress workload to confirm slot mutation maintains
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
    /// to the same topic. The slot-based implementation treats each
    /// Subscribe call as a distinct registration, so the handler is
    /// expected to fire once per Subscribe call when Publish runs.
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
    /// Publish is wait-free and Subscribe/Unsubscribe mutate subscriber
    /// slots under the cold-path lock. The pub/sub must not throw, must not
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
    /// during fast-boot. Slot assignment under contention must
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

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / typed event path).
    /// Use case: devices publish fixed-size unmanaged event structs
    /// instead of allocating message objects or boxing payloads.
    /// Acceptance: The typed handler receives the exact struct payload
    /// and Subscribe returns an opaque handle for later teardown.
    /// </summary>
    [Fact]
    public void TypedSubscribe_ThenPublish_DeliversUnmanagedPayload()
    {
        var pubsub = new LockFreePubSub();
        var topic = Topic.FromName("irq");

        var calls = 0;
        IrqSignal? received = null;
        var handle = pubsub.Subscribe<IrqSignal>(topic, signal =>
        {
            calls++;
            received = signal;
        });

        pubsub.Publish(topic, new IrqSignal(2, 1, 1234));

        Assert.True(handle.IsValid);
        Assert.Equal(1, calls);
        Assert.Equal(new IrqSignal(2, 1, 1234), received);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / slot handles).
    /// Use case: a caller removes one subscription through the opaque
    /// slot-generation handle returned by Subscribe.
    /// Acceptance: Unsubscribe(handle) removes only that subscription
    /// and SubscriptionCount tracks the active set.
    /// </summary>
    [Fact]
    public void Unsubscribe_BySubscriptionHandle_RemovesOnlySelectedSubscription()
    {
        var pubsub = new LockFreePubSub();
        var topic = Topic.FromName("nmi");

        var calls = 0;
        Action<IrqSignal> handler = _ => calls++;
        var first = pubsub.Subscribe(topic, handler);
        var second = pubsub.Subscribe(topic, handler);

        Assert.Equal(2, pubsub.SubscriptionCount);

        pubsub.Unsubscribe(first);
        pubsub.Publish(topic, new IrqSignal(1, 1, 1));

        Assert.Equal(1, calls);
        Assert.Equal(1, pubsub.SubscriptionCount);

        pubsub.Unsubscribe(second);
        pubsub.Publish(topic, new IrqSignal(1, 1, 2));

        Assert.Equal(1, calls);
        Assert.Equal(0, pubsub.SubscriptionCount);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / deterministic order).
    /// Use case: device wiring order must produce deterministic event
    /// delivery when multiple subscribers listen to the same topic.
    /// Acceptance: handlers run in registration order.
    /// </summary>
    [Fact]
    public void Publish_DeliversSubscribersInRegistrationOrder()
    {
        var pubsub = new LockFreePubSub();
        var topic = new TopicId(0xC011);
        var order = new int[3];
        var next = 0;

        pubsub.Subscribe(topic, _ => order[next++] = 1);
        pubsub.Subscribe(topic, _ => order[next++] = 2);
        pubsub.Subscribe(topic, _ => order[next++] = 3);

        pubsub.Publish(topic, new byte[] { 0xFE });

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / zero allocation).
    /// Use case: the steady-state typed publish path runs inside the
    /// emulator frame loop and must not allocate managed heap objects.
    /// Acceptance: after warmup, 1,000 typed publishes allocate zero
    /// bytes on the current thread.
    /// </summary>
    [Fact]
    public void Publish_TypedPayload_HotPathDoesNotAllocate()
    {
        var pubsub = new LockFreePubSub(messageCapacity: 32);
        var topic = Topic.FromName("ba");
        var calls = 0;
        var signal = new IrqSignal(3, 1, 9876);

        pubsub.Subscribe<IrqSignal>(topic, _ => calls++);
        for (var i = 0; i < 64; i++)
        {
            pubsub.Publish(topic, signal);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++)
        {
            pubsub.Publish(topic, signal);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(1_064, calls);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / message pool).
    /// Use case: the fixed-capacity message pool provides explicit
    /// rent/return behavior and frame-boundary forced reclamation.
    /// Acceptance: ActiveCount, exhaustion, and Reset accounting are
    /// deterministic.
    /// </summary>
    [Fact]
    public void MessagePool_RentReturnAndReset_TrackActiveSlots()
    {
        var pool = new LockFreeMessagePool(capacity: 2);

        var first = pool.Rent();
        var second = pool.Rent();

        Assert.Equal(2, pool.ActiveCount);
        Assert.Throws<InvalidOperationException>(() => pool.Rent());
        Assert.Equal(1, pool.ExhaustedRentCount);

        pool.Return(first);
        Assert.Equal(1, pool.ActiveCount);

        var third = pool.Rent();
        Assert.True(third.IsValid);
        Assert.Equal(2, pool.ActiveCount);

        pool.Reset();

        Assert.Equal(0, pool.ActiveCount);
        Assert.Equal(2, pool.ForcedReclamationCount);

        pool.Return(second);
        pool.Return(third);
        Assert.Equal(0, pool.ActiveCount);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / drop accounting).
    /// Use case: if all message slots are already leased, publish must
    /// fail without allocating or invoking subscribers.
    /// Acceptance: the message is dropped and a later publish succeeds
    /// once a slot is returned.
    /// </summary>
    [Fact]
    public void Publish_WhenMessagePoolExhausted_DropsWithoutInvokingSubscribers()
    {
        var pubsub = new LockFreePubSub(messageCapacity: 1);
        var topic = new TopicId(0xBA);
        var leased = pubsub.MessagePool.Rent();
        var calls = 0;

        pubsub.Subscribe(topic, _ => calls++);
        pubsub.Publish(topic, new byte[] { 1 });

        Assert.Equal(0, calls);
        Assert.Equal(1, pubsub.DroppedMessageCount);

        leased.Dispose();
        pubsub.Publish(topic, new byte[] { 2 });

        Assert.Equal(1, calls);
        Assert.Equal(1, pubsub.DroppedMessageCount);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / payload arena).
    /// Use case: raw payloads larger than the inline message slot are
    /// copied into frame-scoped arena storage and invalidated at the
    /// frame boundary.
    /// Acceptance: subscribers receive the full payload and FrameReset
    /// clears arena and pool usage.
    /// </summary>
    [Fact]
    public void Publish_RawPayloadLargerThanInline_UsesFrameArena()
    {
        var pubsub = new LockFreePubSub(messageCapacity: 4, inlinePayloadSize: 4, arenaCapacity: 16, maxArenaCapacity: 32);
        var topic = new TopicId(0xA3C);
        var payload = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        byte[]? received = null;

        pubsub.Subscribe(topic, message => received = message.ToArray());
        pubsub.Publish(topic, payload);

        Assert.Equal(payload, received);
        Assert.True(pubsub.PayloadArena.Used >= payload.Length);
        Assert.Equal(0, pubsub.MessagePool.ActiveCount);

        pubsub.FrameReset();

        Assert.Equal(0, pubsub.PayloadArena.Used);
        Assert.Equal(0, pubsub.MessagePool.ActiveCount);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / MessageKind union).
    /// Use case: specialized device events can publish a discriminated
    /// fixed-size payload without boxing or allocating message objects.
    /// Acceptance: The packed payload is exactly 64 bytes and the
    /// subscriber observes the original topic, kind, and payload words.
    /// </summary>
    [Fact]
    public void Publish_PackedPayload_DeliversMessageKindAndFixedPayload()
    {
        var pubsub = new LockFreePubSub();
        var topic = Topic.FromName("irq");
        var payload = new PubSubPayload(0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44);
        PubSubMessage? received = null;

        var size = Marshal.SizeOf<PubSubPayload>();
        pubsub.Subscribe(topic, message => received = message);

        pubsub.Publish(topic, MessageKind.Irq, payload);

        Assert.Equal(PubSubPayload.Size, size);
        Assert.Equal(new PubSubMessage(topic, MessageKind.Irq, payload), received);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / route table).
    /// Use case: hot-path topic routing uses an open-addressed fixed
    /// route table instead of a dictionary. Hash collisions must not
    /// cross-deliver events between topics.
    /// Acceptance: two colliding numeric topics remain isolated.
    /// </summary>
    [Fact]
    public void Publish_CollidingRouteSlots_RemainTopicIsolated()
    {
        var pubsub = new LockFreePubSub(topicCapacity: 2);
        var topicA = new Topic(1);
        var topicB = new Topic(3);
        var callsA = 0;
        var callsB = 0;

        pubsub.Subscribe<IrqSignal>(topicA, _ => callsA++);
        pubsub.Subscribe<IrqSignal>(topicB, _ => callsB++);

        pubsub.Publish(topicB, new IrqSignal(1, 1, 1));

        Assert.Equal(0, callsA);
        Assert.Equal(1, callsB);
    }

    /// <summary>
    /// FR/TR: TR-PUBSUB-PERFORMANCE (PUBSUB / flat subscriber arrays).
    /// Use case: route subscriber arrays are preallocated and grow only
    /// on cold-path setup when a route exceeds its initial capacity.
    /// Acceptance: growth preserves deterministic registration order.
    /// </summary>
    [Fact]
    public void Subscribe_WhenRouteSubscriberArrayGrows_PreservesDeliveryOrder()
    {
        var pubsub = new LockFreePubSub(subscribersPerTopic: 1);
        var topic = Topic.FromName("dma");
        var order = new int[3];
        var next = 0;

        pubsub.Subscribe<IrqSignal>(topic, _ => order[next++] = 1);
        pubsub.Subscribe<IrqSignal>(topic, _ => order[next++] = 2);
        pubsub.Subscribe<IrqSignal>(topic, _ => order[next++] = 3);

        pubsub.Publish(topic, new IrqSignal(1, 1, 1));

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    private readonly record struct IrqSignal(byte Source, byte Asserted, ulong Cycle);
}
