using System.Runtime.InteropServices;

namespace ViceSharp.Abstractions;

/// <summary>
/// Lock-free topic-based publish/subscribe for high-frequency device communication.
/// </summary>
public interface IPubSub
{
    /// <summary>
    /// Publishes an unmanaged payload to all subscribers of the given topic.
    /// </summary>
    void Publish<T>(Topic topic, T payload)
        where T : unmanaged;

    /// <summary>
    /// Publishes an already-packed 64-byte message payload to all subscribers of the topic.
    /// </summary>
    void Publish(Topic topic, MessageKind kind, PubSubPayload payload);

    /// <summary>
    /// Subscribes a strongly typed handler to the given topic.
    /// </summary>
    SubscriptionHandle Subscribe<T>(Topic topic, Action<T> handler)
        where T : unmanaged;

    /// <summary>
    /// Subscribes a handler that receives the message kind and packed payload.
    /// </summary>
    SubscriptionHandle Subscribe(Topic topic, Action<PubSubMessage> handler);

    /// <summary>
    /// Publishes raw bytes to all span subscribers of the given topic.
    /// </summary>
    void Publish(TopicId topic, ReadOnlySpan<byte> payload);

    /// <summary>
    /// Subscribes a raw byte handler to the given topic.
    /// </summary>
    SubscriptionHandle Subscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler);

    /// <summary>
    /// Removes a subscription by its opaque handle.
    /// </summary>
    void Unsubscribe(SubscriptionHandle handle);

    /// <summary>
    /// Removes the first matching raw byte subscription for compatibility with the early API.
    /// </summary>
    void Unsubscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler);

    /// <summary>
    /// Delivers pending messages. Synchronous implementations may complete this as a no-op.
    /// </summary>
    void Flush();

    /// <summary>
    /// Resets frame-scoped message state.
    /// </summary>
    void FrameReset();

    /// <summary>
    /// Number of active subscriptions.
    /// </summary>
    int SubscriptionCount { get; }
}

/// <summary>
/// Discriminator for fixed-size pub/sub payload slots.
/// </summary>
public enum MessageKind : byte
{
    /// <summary>
    /// Unspecified message kind.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Raw byte payload published through the compatibility span API.
    /// </summary>
    Raw = 1,

    /// <summary>
    /// Unmanaged payload published through the generic typed API.
    /// </summary>
    Typed = 2,

    /// <summary>
    /// IRQ line state message.
    /// </summary>
    Irq = 3,

    /// <summary>
    /// NMI line state message.
    /// </summary>
    Nmi = 4,

    /// <summary>
    /// VIC-II BA line message.
    /// </summary>
    BusAvailable = 5,

    /// <summary>
    /// Address-enable control line message.
    /// </summary>
    AddressEnableControl = 6,

    /// <summary>
    /// DMA event message.
    /// </summary>
    Dma = 7,

    /// <summary>
    /// Clock phase or cycle message.
    /// </summary>
    Clock = 8,

    /// <summary>
    /// State mutation notification.
    /// </summary>
    State = 9,
}

/// <summary>
/// Fixed 64-byte pub/sub payload union.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct PubSubPayload(
    ulong Word0,
    ulong Word1,
    ulong Word2,
    ulong Word3,
    ulong Word4,
    ulong Word5,
    ulong Word6,
    ulong Word7)
{
    /// <summary>
    /// Payload size in bytes.
    /// </summary>
    public const int Size = 64;

    /// <summary>
    /// Empty payload.
    /// </summary>
    public static readonly PubSubPayload Empty = default;
}

/// <summary>
/// Discriminated pub/sub message with a fixed 64-byte inline payload.
/// </summary>
public readonly record struct PubSubMessage(Topic Topic, MessageKind Kind, PubSubPayload Payload);

/// <summary>
/// Pre-allocated pool of message slots for zero-allocation pub/sub delivery.
/// </summary>
public interface IMessagePool
{
    /// <summary>
    /// Rents a message slot from the pool.
    /// </summary>
    MessageHandle Rent();

    /// <summary>
    /// Returns a message slot to the pool.
    /// </summary>
    void Return(MessageHandle handle);

    /// <summary>
    /// Total number of slots in the pool.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Number of slots currently rented.
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    /// Returns all slots to the pool.
    /// </summary>
    void Reset();
}

/// <summary>
/// Strongly typed topic identifier.
/// </summary>
public readonly struct Topic : IEquatable<Topic>
{
    /// <summary>
    /// Numeric topic value used on the hot path.
    /// </summary>
    public uint Value { get; }

    /// <summary>
    /// Optional human-readable topic name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Creates a topic from a precomputed numeric value.
    /// </summary>
    public Topic(uint value)
    {
        Value = value;
        Name = null;
    }

    /// <summary>
    /// Creates a topic from a name using a deterministic FNV-1a hash.
    /// </summary>
    public Topic(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = string.Intern(name);
        Value = ComputeStableHash(Name);
    }

    /// <summary>
    /// Creates a topic from a name using a deterministic FNV-1a hash.
    /// </summary>
    public static Topic FromName(string name) => new(name);

    /// <inheritdoc />
    public bool Equals(Topic other)
    {
        if (Value != other.Value)
        {
            return false;
        }

        return Name is null || other.Name is null || ReferenceEquals(Name, other.Name) || Name == other.Name;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Topic other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Name is null ? $"Topic#{Value}" : $"{Name}({Value})";

    /// <summary>
    /// Returns whether two topics identify the same route.
    /// </summary>
    public static bool operator ==(Topic left, Topic right) => left.Equals(right);

    /// <summary>
    /// Returns whether two topics identify different routes.
    /// </summary>
    public static bool operator !=(Topic left, Topic right) => !left.Equals(right);

    private static uint ComputeStableHash(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;

        var hash = offset;
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= prime;
        }

        return hash;
    }
}

/// <summary>
/// Compatibility numeric topic identifier used by the early Iteration 0 surface.
/// </summary>
public readonly struct TopicId : IEquatable<TopicId>
{
    /// <summary>
    /// Numeric topic value used on the hot path.
    /// </summary>
    public uint Value { get; }

    /// <summary>
    /// Creates a topic id from a numeric value.
    /// </summary>
    public TopicId(uint value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public bool Equals(TopicId other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TopicId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"Topic#{Value}";

    /// <summary>
    /// Converts a <see cref="Topic"/> to a compatibility <see cref="TopicId"/>.
    /// </summary>
    public static implicit operator TopicId(Topic topic) => new(topic.Value);

    /// <summary>
    /// Converts a compatibility <see cref="TopicId"/> to a <see cref="Topic"/>.
    /// </summary>
    public static implicit operator Topic(TopicId topic) => new(topic.Value);

    /// <summary>
    /// Returns whether two topic ids identify the same route.
    /// </summary>
    public static bool operator ==(TopicId left, TopicId right) => left.Equals(right);

    /// <summary>
    /// Returns whether two topic ids identify different routes.
    /// </summary>
    public static bool operator !=(TopicId left, TopicId right) => !left.Equals(right);
}

/// <summary>
/// Opaque handle for removing a pub/sub subscription.
/// </summary>
public readonly struct SubscriptionHandle : IEquatable<SubscriptionHandle>
{
    /// <summary>
    /// Invalid subscription handle.
    /// </summary>
    public static readonly SubscriptionHandle Invalid = default;

    /// <summary>
    /// Subscription slot index.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// Slot generation used to reject stale handles after slot reuse.
    /// </summary>
    public int Generation { get; }

    /// <summary>
    /// Topic associated with the subscription.
    /// </summary>
    public Topic Topic { get; }

    /// <summary>
    /// Whether the handle identifies a subscription slot.
    /// </summary>
    public bool IsValid => SlotIndex >= 0 && Generation > 0;

    /// <summary>
    /// Creates a subscription handle.
    /// </summary>
    public SubscriptionHandle(int slotIndex, int generation, Topic topic)
    {
        SlotIndex = slotIndex;
        Generation = generation;
        Topic = topic;
    }

    /// <inheritdoc />
    public bool Equals(SubscriptionHandle other) =>
        SlotIndex == other.SlotIndex && Generation == other.Generation && Topic == other.Topic;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SubscriptionHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(SlotIndex, Generation, Topic);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? $"{Topic}/sub#{SlotIndex}:{Generation}" : "InvalidSubscription";

    /// <summary>
    /// Returns whether two handles identify the same subscription generation.
    /// </summary>
    public static bool operator ==(SubscriptionHandle left, SubscriptionHandle right) => left.Equals(right);

    /// <summary>
    /// Returns whether two handles identify different subscription generations.
    /// </summary>
    public static bool operator !=(SubscriptionHandle left, SubscriptionHandle right) => !left.Equals(right);
}

/// <summary>
/// Reference-counted handle into an <see cref="IMessagePool"/>.
/// </summary>
public readonly struct MessageHandle : IEquatable<MessageHandle>, IDisposable
{
    /// <summary>
    /// Invalid message handle.
    /// </summary>
    public static readonly MessageHandle Invalid = default;

    /// <summary>
    /// Pool that owns the slot.
    /// </summary>
    public IMessagePool? Owner { get; }

    /// <summary>
    /// Message slot index.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// Slot generation used to reject stale handles after frame reset or slot reuse.
    /// </summary>
    public int Generation { get; }

    /// <summary>
    /// Topic carried by the message.
    /// </summary>
    public Topic Topic { get; }

    /// <summary>
    /// Discriminator for the payload stored in the slot.
    /// </summary>
    public MessageKind Kind { get; }

    /// <summary>
    /// Payload length in bytes.
    /// </summary>
    public int PayloadLength { get; }

    /// <summary>
    /// Monotonic sequence assigned by the owning pool.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Whether the handle identifies a rented message slot.
    /// </summary>
    public bool IsValid => Owner is not null && SlotIndex >= 0 && Generation > 0;

    /// <summary>
    /// Creates a message handle.
    /// </summary>
    public MessageHandle(
        IMessagePool owner,
        int slotIndex,
        int generation,
        Topic topic,
        MessageKind kind,
        int payloadLength,
        long sequence)
    {
        Owner = owner;
        SlotIndex = slotIndex;
        Generation = generation;
        Topic = topic;
        Kind = kind;
        PayloadLength = payloadLength;
        Sequence = sequence;
    }

    /// <summary>
    /// Returns the message slot to the owning pool.
    /// </summary>
    public void Dispose() => Owner?.Return(this);

    /// <summary>
    /// Returns the message slot to the owning pool.
    /// </summary>
    public void Release() => Dispose();

    /// <inheritdoc />
    public bool Equals(MessageHandle other) =>
        ReferenceEquals(Owner, other.Owner)
        && SlotIndex == other.SlotIndex
        && Generation == other.Generation
        && Sequence == other.Sequence;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MessageHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Owner, SlotIndex, Generation, Sequence);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? $"{Topic}/msg#{SlotIndex}:{Generation}@{Sequence}" : "InvalidMessage";

    /// <summary>
    /// Returns whether two handles identify the same message slot generation.
    /// </summary>
    public static bool operator ==(MessageHandle left, MessageHandle right) => left.Equals(right);

    /// <summary>
    /// Returns whether two handles identify different message slot generations.
    /// </summary>
    public static bool operator !=(MessageHandle left, MessageHandle right) => !left.Equals(right);
}
