using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed class LockFreePubSub : IPubSub
{
    private readonly Dictionary<TopicId, List<Action<ReadOnlySpan<byte>>>> _subscribers = new();

    public void Publish(TopicId topic, ReadOnlySpan<byte> payload)
    {
        if (_subscribers.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler(payload);
            }
        }
    }

    public void Subscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler)
    {
        if (!_subscribers.TryGetValue(topic, out var handlers))
        {
            handlers = new List<Action<ReadOnlySpan<byte>>>();
            _subscribers[topic] = handlers;
        }
        handlers.Add(handler);
    }

    public void Unsubscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler)
    {
        if (_subscribers.TryGetValue(topic, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
}