namespace ViceSharp.Abstractions;

/// <summary>
/// Lock free publish/subscribe system for high frequency device communication.
/// </summary>
public interface IPubSub
{
    /// <summary>
    /// Publish a message to all subscribers of the given topic.
    /// </summary>
    void Publish(TopicId topic, ReadOnlySpan<byte> payload);

    /// <summary>
    /// Subscribe to messages on the given topic.
    /// </summary>
    void Subscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler);

    /// <summary>
    /// Unsubscribe from a topic.
    /// </summary>
    void Unsubscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler);
}

/// <summary>
/// Strongly typed topic identifier.
/// </summary>
public readonly struct TopicId : IEquatable<TopicId>
{
    public uint Value { get; }

    public TopicId(uint value)
    {
        Value = value;
    }

    public bool Equals(TopicId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TopicId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"Topic#{Value}";

    public static bool operator ==(TopicId left, TopicId right) => left.Equals(right);
    public static bool operator !=(TopicId left, TopicId right) => !left.Equals(right);
}