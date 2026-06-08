using System.Diagnostics;
using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Stopwatch-based PubSub probe for quick TR-PUBSUB-001 validation outside BenchmarkDotNet.
/// </summary>
public static class PubSubPerfProbe
{
    public const int DefaultMessageCount = 1_000_000;

    public static Result Run(int messageCount = DefaultMessageCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageCount);

        var topic = Topic.FromName("irq");
        var signal = new IrqSignal(2, 1, 1234);
        var payload = new PubSubPayload(1, 2, 3, 4, 5, 6, 7, 8);

        var oneSubscriber = new LockFreePubSub(messageCapacity: 8192);
        var threeSubscribers = new LockFreePubSub(messageCapacity: 8192);
        var packedMessages = new LockFreePubSub(messageCapacity: 8192);
        var pool = new LockFreeMessagePool(capacity: 8192);
        var arenaCapacity = Math.Max(64 * 1024, messageCount * 8);
        var arena = new PayloadArena(initialCapacity: arenaCapacity, maxCapacity: arenaCapacity);

        var sink = 0;
        oneSubscriber.Subscribe<IrqSignal>(topic, message => sink += message.Source);
        threeSubscribers.Subscribe<IrqSignal>(topic, message => sink += message.Source);
        threeSubscribers.Subscribe<IrqSignal>(topic, message => sink += message.Source);
        threeSubscribers.Subscribe<IrqSignal>(topic, message => sink += message.Source);
        packedMessages.Subscribe(topic, message => sink += (int)message.Payload.Word0);

        for (var i = 0; i < 1024; i++)
        {
            oneSubscriber.Publish(topic, signal);
            threeSubscribers.Publish(topic, signal);
            packedMessages.Publish(topic, MessageKind.Irq, payload);
            var handle = pool.Rent();
            pool.Return(handle);
            _ = arena.TryAllocate(8, out _);
        }

        _ = MeasurePublishTyped(100_000, oneSubscriber, topic, signal);
        _ = MeasurePublishTyped(100_000, threeSubscribers, topic, signal);
        _ = MeasurePublishPacked(100_000, packedMessages, topic, payload);
        _ = MeasurePool(100_000, pool);
        arena.Reset();
        _ = MeasureArena(100_000, arena);
        arena.Reset();

        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        var oneSubscriberNs = MeasurePublishTyped(messageCount, oneSubscriber, topic, signal);
        var threeSubscriberNs = MeasurePublishTyped(messageCount, threeSubscribers, topic, signal);
        var packedNs = MeasurePublishPacked(messageCount, packedMessages, topic, payload);
        var poolNs = MeasurePool(messageCount, pool);
        var arenaNs = MeasureArena(messageCount, arena);
        arena.Reset();
        oneSubscriberNs = Math.Min(oneSubscriberNs, MeasurePublishTyped(messageCount, oneSubscriber, topic, signal));
        threeSubscriberNs = Math.Min(threeSubscriberNs, MeasurePublishTyped(messageCount, threeSubscribers, topic, signal));
        packedNs = Math.Min(packedNs, MeasurePublishPacked(messageCount, packedMessages, topic, payload));
        poolNs = Math.Min(poolNs, MeasurePool(messageCount, pool));
        arenaNs = Math.Min(arenaNs, MeasureArena(messageCount, arena));
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        GC.KeepAlive(sink);
        return new Result(messageCount, oneSubscriberNs, threeSubscriberNs, packedNs, poolNs, arenaNs, allocatedBytes);
    }

    private static double MeasurePublishTyped(int operationCount, LockFreePubSub pubsub, Topic topic, IrqSignal signal)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < operationCount; i++)
        {
            pubsub.Publish(topic, signal);
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - start;
        return elapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency / operationCount;
    }

    private static double MeasurePublishPacked(int operationCount, LockFreePubSub pubsub, Topic topic, PubSubPayload payload)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < operationCount; i++)
        {
            pubsub.Publish(topic, MessageKind.Irq, payload);
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - start;
        return elapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency / operationCount;
    }

    private static double MeasurePool(int operationCount, LockFreeMessagePool pool)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < operationCount; i++)
        {
            var handle = pool.Rent();
            pool.Return(handle);
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - start;
        return elapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency / operationCount;
    }

    private static double MeasureArena(int operationCount, PayloadArena arena)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < operationCount; i++)
        {
            _ = arena.TryAllocate(8, out _);
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - start;
        return elapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency / operationCount;
    }

    public readonly record struct Result(
        int MessageCount,
        double PublishOneSubscriberNs,
        double PublishThreeSubscribersNs,
        double PublishPackedPayloadNs,
        double MessagePoolRentReturnNs,
        double PayloadArenaAllocateNs,
        long AllocatedBytes);

    private readonly record struct IrqSignal(byte Source, byte Asserted, ulong Cycle);
}
