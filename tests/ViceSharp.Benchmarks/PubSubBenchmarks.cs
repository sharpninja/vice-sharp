using BenchmarkDotNet.Attributes;
using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Microbenchmarks for TR-PUBSUB-001 publish, delivery, pool, and arena costs.
/// </summary>
[MemoryDiagnoser]
public class PubSubBenchmarks
{
    private const int Operations = 1024;

    private readonly IrqSignal _signal = new(2, 1, 1234);
    private readonly PubSubPayload _packedPayload = new(1, 2, 3, 4, 5, 6, 7, 8);

    private LockFreePubSub _oneSubscriber = null!;
    private LockFreePubSub _threeSubscribers = null!;
    private LockFreePubSub _packedMessages = null!;
    private LockFreeMessagePool _messagePool = null!;
    private PayloadArena _payloadArena = null!;
    private Topic _topic;
    private int _sink;

    [GlobalSetup]
    public void Setup()
    {
        _topic = Topic.FromName("irq");

        _oneSubscriber = new LockFreePubSub();
        _oneSubscriber.Subscribe<IrqSignal>(_topic, Consume);

        _threeSubscribers = new LockFreePubSub();
        _threeSubscribers.Subscribe<IrqSignal>(_topic, Consume);
        _threeSubscribers.Subscribe<IrqSignal>(_topic, Consume);
        _threeSubscribers.Subscribe<IrqSignal>(_topic, Consume);

        _packedMessages = new LockFreePubSub();
        _packedMessages.Subscribe(_topic, ConsumePacked);

        _messagePool = new LockFreeMessagePool(capacity: Operations);
        _payloadArena = new PayloadArena(initialCapacity: Operations * 16, maxCapacity: Operations * 16);
    }

    [Benchmark(Description = "IPubSub.Publish<T>() one subscriber", OperationsPerInvoke = Operations)]
    public void PublishTypedOneSubscriber()
    {
        var pubsub = _oneSubscriber;
        var topic = _topic;
        var signal = _signal;

        for (var i = 0; i < Operations; i++)
        {
            pubsub.Publish(topic, signal);
        }
    }

    [Benchmark(Description = "IPubSub.Publish<T>() three subscribers", OperationsPerInvoke = Operations)]
    public void PublishTypedThreeSubscribers()
    {
        var pubsub = _threeSubscribers;
        var topic = _topic;
        var signal = _signal;

        for (var i = 0; i < Operations; i++)
        {
            pubsub.Publish(topic, signal);
        }
    }

    [Benchmark(Description = "IPubSub.Publish(MessageKind, PubSubPayload)", OperationsPerInvoke = Operations)]
    public void PublishPackedPayload()
    {
        var pubsub = _packedMessages;
        var topic = _topic;
        var payload = _packedPayload;

        for (var i = 0; i < Operations; i++)
        {
            pubsub.Publish(topic, MessageKind.Irq, payload);
        }
    }

    [Benchmark(Description = "IMessagePool.Rent/Return", OperationsPerInvoke = Operations)]
    public void MessagePoolRentReturn()
    {
        var pool = _messagePool;

        for (var i = 0; i < Operations; i++)
        {
            var handle = pool.Rent();
            pool.Return(handle);
        }
    }

    [Benchmark(Description = "PayloadArena.TryAllocate(16)", OperationsPerInvoke = Operations)]
    public void PayloadArenaAllocate()
    {
        var arena = _payloadArena;
        arena.Reset();

        for (var i = 0; i < Operations; i++)
        {
            _ = arena.TryAllocate(16, out _);
        }
    }

    private void Consume(IrqSignal signal)
    {
        _sink += signal.Source;
    }

    private void ConsumePacked(PubSubMessage message)
    {
        _sink += (int)message.Payload.Word0;
    }

    private readonly record struct IrqSignal(byte Source, byte Asserted, ulong Cycle);
}
