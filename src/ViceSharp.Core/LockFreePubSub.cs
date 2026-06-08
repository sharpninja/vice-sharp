// This file is part of ViceSharp.
// Copyright (C) 2026 ViceSharp Contributors
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Lock-free publish/subscribe implementation for the emulation hot path.
/// Publish uses a fixed route table, preallocated subscriber slots, and preallocated message storage.
/// Subscribe and unsubscribe are cold-path slot updates.
/// </summary>
public sealed class LockFreePubSub : IPubSub
{
    private const int DefaultMessageCapacity = 8192;
    private const int DefaultInlinePayloadSize = 64;
    private const int DefaultArenaCapacity = 64 * 1024;
    private const int DefaultTopicCapacity = 256;
    private const int DefaultSubscribersPerTopic = 16;
    private const int DefaultSubscriptionCapacity = 256;

    private readonly object _subscriptionLock = new();
    private readonly LockFreeMessagePool _messagePool;
    private readonly PayloadArena _payloadArena;
    private readonly int _inlinePayloadSize;
    private readonly int _subscribersPerTopic;

    private TopicRoute?[] _routes;
    private SubscriptionSlot?[] _subscriptionSlots;
    private int[] _freeSubscriptionSlots;
    private int _freeSubscriptionCount;
    private int _nextSubscriptionSlot;
    private int _routeCount;
    private int _subscriptionCount;
    private long _publishedMessageCount;
    private long _droppedMessageCount;

    /// <summary>
    /// Creates a pub/sub bus with fixed-capacity message and subscription storage.
    /// </summary>
    public LockFreePubSub(
        int messageCapacity = DefaultMessageCapacity,
        int inlinePayloadSize = DefaultInlinePayloadSize,
        int arenaCapacity = DefaultArenaCapacity,
        int maxArenaCapacity = DefaultArenaCapacity,
        int topicCapacity = DefaultTopicCapacity,
        int subscribersPerTopic = DefaultSubscribersPerTopic,
        int subscriptionCapacity = DefaultSubscriptionCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inlinePayloadSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(arenaCapacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxArenaCapacity, arenaCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topicCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscribersPerTopic);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionCapacity);

        _inlinePayloadSize = inlinePayloadSize;
        _subscribersPerTopic = subscribersPerTopic;
        _messagePool = new LockFreeMessagePool(messageCapacity, inlinePayloadSize);
        _payloadArena = new PayloadArena(arenaCapacity, maxArenaCapacity);
        _routes = new TopicRoute[RoundUpToPowerOfTwo(topicCapacity)];
        _subscriptionSlots = new SubscriptionSlot?[subscriptionCapacity];
        _freeSubscriptionSlots = new int[subscriptionCapacity];
    }

    /// <summary>
    /// Message pool used by this bus.
    /// </summary>
    public IMessagePool MessagePool => _messagePool;

    /// <summary>
    /// Payload arena used for raw payloads larger than the inline slot size.
    /// </summary>
    public PayloadArena PayloadArena => _payloadArena;

    /// <inheritdoc />
    public int SubscriptionCount => Volatile.Read(ref _subscriptionCount);

    /// <summary>
    /// Number of messages successfully published.
    /// </summary>
    public long PublishedMessageCount => Interlocked.Read(ref _publishedMessageCount);

    /// <summary>
    /// Number of messages dropped because the pool or arena was exhausted.
    /// </summary>
    public long DroppedMessageCount => Interlocked.Read(ref _droppedMessageCount);

    /// <inheritdoc />
    public void Publish<T>(Topic topic, T payload)
        where T : unmanaged
    {
        var payloadSize = Unsafe.SizeOf<T>();
        if (payloadSize > _inlinePayloadSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payload),
                payloadSize,
                $"Typed pub/sub payloads must be {_inlinePayloadSize} bytes or smaller.");
        }

        var payloadCopy = payload;
        var payloadBytes = MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<T, byte>(ref payloadCopy),
            payloadSize);

        PublishCore(topic, payloadBytes, MessageKind.Typed, SubscriberPayloadKind.Typed, typeof(T).TypeHandle.Value);
    }

    /// <inheritdoc />
    public void Publish(Topic topic, MessageKind kind, PubSubPayload payload)
    {
        var payloadCopy = payload;
        var payloadBytes = MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<PubSubPayload, byte>(ref payloadCopy),
            PubSubPayload.Size);

        PublishCore(topic, payloadBytes, kind, SubscriberPayloadKind.Packed, typeof(PubSubMessage).TypeHandle.Value);
    }

    /// <inheritdoc />
    public void Publish(TopicId topic, ReadOnlySpan<byte> payload) =>
        PublishCore(topic, payload, MessageKind.Raw, SubscriberPayloadKind.Raw, payloadTypeHandle: 0);

    /// <inheritdoc />
    public SubscriptionHandle Subscribe<T>(Topic topic, Action<T> handler)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(handler);

        var payloadSize = Unsafe.SizeOf<T>();
        if (payloadSize > _inlinePayloadSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(T),
                payloadSize,
                $"Typed pub/sub payloads must be {_inlinePayloadSize} bytes or smaller.");
        }

        MessageHandler invoker = (payload, _, _) => handler(MemoryMarshal.Read<T>(payload));
        return AddSubscription(topic, SubscriberPayloadKind.Typed, typeof(T).TypeHandle.Value, payloadSize, handler, invoker);
    }

    /// <inheritdoc />
    public SubscriptionHandle Subscribe(Topic topic, Action<PubSubMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        MessageHandler invoker = (payload, kind, messageTopic) => handler(new PubSubMessage(
            messageTopic,
            kind,
            MemoryMarshal.Read<PubSubPayload>(payload)));
        return AddSubscription(topic, SubscriberPayloadKind.Packed, typeof(PubSubMessage).TypeHandle.Value, PubSubPayload.Size, handler, invoker);
    }

    /// <inheritdoc />
    public SubscriptionHandle Subscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        MessageHandler invoker = (payload, _, _) => handler(payload);
        return AddSubscription(topic, SubscriberPayloadKind.Raw, payloadTypeHandle: 0, payloadSize: -1, handler, invoker);
    }

    /// <inheritdoc />
    public void Unsubscribe(SubscriptionHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        lock (_subscriptionLock)
        {
            RemoveSubscriptionLocked(handle);
        }
    }

    /// <inheritdoc />
    public void Unsubscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_subscriptionLock)
        {
            var route = TryFindRoute(topic);
            if (route is null)
            {
                return;
            }

            var entries = route.Subscribers;
            var highWater = Volatile.Read(ref route.HighWaterMark);
            foreach (var entry in entries)
            {
                if (entry.Active == 1 && entry.PayloadKind == SubscriberPayloadKind.Raw && Equals(entry.OriginalHandler, handler))
                {
                    RemoveSubscriptionLocked(new SubscriptionHandle(entry.SlotIndex, entry.Generation, topic));
                    return;
                }

                if (--highWater == 0)
                {
                    return;
                }
            }
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        // Delivery is synchronous for Iteration 1. This method remains in the
        // public contract for callers that coordinate with future queued modes.
    }

    /// <inheritdoc />
    public void FrameReset()
    {
        _messagePool.Reset();
        _payloadArena.Reset();
    }

    private void PublishCore(
        Topic topic,
        ReadOnlySpan<byte> payload,
        MessageKind kind,
        SubscriberPayloadKind payloadKind,
        nint payloadTypeHandle)
    {
        var route = TryFindRoute(topic);
        if (route is null || Volatile.Read(ref route.ActiveCount) == 0)
        {
            return;
        }

        if (!_messagePool.TryRentForPublish(topic, kind, payload.Length, out var handle))
        {
            Interlocked.Increment(ref _droppedMessageCount);
            return;
        }

        try
        {
            if (!CopyPayload(handle, payload))
            {
                Interlocked.Increment(ref _droppedMessageCount);
                return;
            }

            var deliveredPayload = _messagePool.GetPayloadSpan(handle, _payloadArena);
            var entries = route.Subscribers;
            var highWater = Volatile.Read(ref route.HighWaterMark);
            for (var i = 0; i < highWater; i++)
            {
                var entry = entries[i];
                if (Volatile.Read(ref entry.Active) == 1 && entry.CanAccept(payloadKind, payloadTypeHandle, payload.Length))
                {
                    entry.Handler(deliveredPayload, kind, topic);
                }
            }

            Interlocked.Increment(ref _publishedMessageCount);
        }
        finally
        {
            _messagePool.ReturnPublished(handle);
        }
    }

    private bool CopyPayload(MessageHandle handle, ReadOnlySpan<byte> payload)
    {
        if (payload.Length <= _inlinePayloadSize)
        {
            payload.CopyTo(_messagePool.GetInlinePayloadSpan(handle, payload.Length));
            return true;
        }

        if (!_payloadArena.TryAllocate(payload.Length, out var offset))
        {
            return false;
        }

        _messagePool.SetArenaPayload(handle, offset, payload.Length);
        payload.CopyTo(_payloadArena.GetSpan(offset, payload.Length));
        return true;
    }

    private SubscriptionHandle AddSubscription(
        Topic topic,
        SubscriberPayloadKind payloadKind,
        nint payloadTypeHandle,
        int payloadSize,
        Delegate originalHandler,
        MessageHandler invoker)
    {
        lock (_subscriptionLock)
        {
            var slotIndex = RentSubscriptionSlotLocked();
            var slot = _subscriptionSlots[slotIndex] ??= new SubscriptionSlot();
            var generation = unchecked(slot.Generation + 1);
            if (generation <= 0)
            {
                generation = 1;
            }

            slot.Generation = generation;
            slot.Topic = topic;
            var route = GetOrAddRouteLocked(topic);
            var routeSlotIndex = route.RentSlot();
            slot.RouteSubscriberIndex = routeSlotIndex;
            slot.Active = true;

            var entry = new SubscriberEntry(slotIndex, generation, topic, payloadKind, payloadTypeHandle, payloadSize, originalHandler, invoker);
            route.Subscribers[routeSlotIndex] = entry;
            route.ActivateSlot(routeSlotIndex);

            Interlocked.Increment(ref _subscriptionCount);

            return new SubscriptionHandle(slotIndex, generation, topic);
        }
    }

    private bool RemoveSubscriptionLocked(SubscriptionHandle handle)
    {
        if (!TryGetActiveSlot(handle, out var slot))
        {
            return false;
        }

        slot.Active = false;

        var route = TryFindRoute(handle.Topic);
        if (route is not null)
        {
            var routeSlotIndex = slot.RouteSubscriberIndex;
            if ((uint)routeSlotIndex < (uint)route.Subscribers.Length)
            {
                ref var entry = ref route.Subscribers[routeSlotIndex];
                if (entry.SlotIndex == handle.SlotIndex && entry.Generation == handle.Generation)
                {
                    Volatile.Write(ref entry.Active, 0);
                    entry = default;
                    route.ReturnSlot(routeSlotIndex);
                }
            }
        }

        ReturnSubscriptionSlotLocked(handle.SlotIndex);
        Interlocked.Decrement(ref _subscriptionCount);
        return true;
    }

    private int RentSubscriptionSlotLocked()
    {
        if (_freeSubscriptionCount > 0)
        {
            return _freeSubscriptionSlots[--_freeSubscriptionCount];
        }

        if (_nextSubscriptionSlot == _subscriptionSlots.Length)
        {
            GrowSubscriptionStorageLocked();
        }

        return _nextSubscriptionSlot++;
    }

    private void ReturnSubscriptionSlotLocked(int slotIndex)
    {
        if (_freeSubscriptionCount == _freeSubscriptionSlots.Length)
        {
            Array.Resize(ref _freeSubscriptionSlots, _freeSubscriptionSlots.Length * 2);
        }

        _freeSubscriptionSlots[_freeSubscriptionCount++] = slotIndex;
    }

    private void GrowSubscriptionStorageLocked()
    {
        var newLength = _subscriptionSlots.Length * 2;
        Array.Resize(ref _subscriptionSlots, newLength);
        Array.Resize(ref _freeSubscriptionSlots, newLength);
    }

    private bool TryGetActiveSlot(SubscriptionHandle handle, out SubscriptionSlot slot)
    {
        slot = null!;
        if ((uint)handle.SlotIndex >= (uint)_subscriptionSlots.Length)
        {
            return false;
        }

        var candidate = _subscriptionSlots[handle.SlotIndex];
        if (candidate is null || !candidate.Active || candidate.Generation != handle.Generation || candidate.Topic != handle.Topic)
        {
            return false;
        }

        slot = candidate;
        return true;
    }

    private TopicRoute? TryFindRoute(Topic topic)
    {
        var routes = Volatile.Read(ref _routes);
        var mask = routes.Length - 1;
        var start = topic.GetHashCode() & mask;

        for (var probe = 0; probe < routes.Length; probe++)
        {
            var route = routes[(start + probe) & mask];
            if (route is null || !Volatile.Read(ref route.HasTopic))
            {
                return null;
            }

            if (route.Topic == topic)
            {
                return route;
            }
        }

        return null;
    }

    private TopicRoute GetOrAddRouteLocked(Topic topic)
    {
        while (true)
        {
            var route = TryFindRoute(topic);
            if (route is not null)
            {
                return route;
            }

            if (_routeCount * 4 >= _routes.Length * 3)
            {
                GrowRouteTableLocked();
                continue;
            }

            return AddRouteLocked(topic);
        }
    }

    private TopicRoute AddRouteLocked(Topic topic)
    {
        var routes = _routes;
        var mask = routes.Length - 1;
        var start = topic.GetHashCode() & mask;

        for (var probe = 0; probe < routes.Length; probe++)
        {
            var index = (start + probe) & mask;
            if (routes[index] is null || !routes[index]!.HasTopic)
            {
                var route = new TopicRoute(topic, _subscribersPerTopic);
                routes[index] = route;
                Volatile.Write(ref route.HasTopic, true);
                _routeCount++;
                return route;
            }
        }

        GrowRouteTableLocked();
        return AddRouteLocked(topic);
    }

    private void GrowRouteTableLocked()
    {
        var original = _routes;
        var grown = new TopicRoute?[original.Length * 2];

        for (var i = 0; i < original.Length; i++)
        {
            var route = original[i];
            if (route is null || !route.HasTopic)
            {
                continue;
            }

            AddRouteToTable(grown, route);
        }

        Volatile.Write(ref _routes, grown);
    }

    private static void AddRouteToTable(TopicRoute?[] table, TopicRoute route)
    {
        var mask = table.Length - 1;
        var start = route.Topic.GetHashCode() & mask;

        for (var probe = 0; probe < table.Length; probe++)
        {
            var index = (start + probe) & mask;
            if (table[index] is null || !table[index]!.HasTopic)
            {
                table[index] = route;
                return;
            }
        }

        throw new InvalidOperationException("The pub/sub route table is full.");
    }

    private static int RoundUpToPowerOfTwo(int value)
    {
        var result = 1;
        while (result < value)
        {
            result <<= 1;
        }

        return result;
    }

    private sealed class SubscriptionSlot
    {
        public int Generation;
        public Topic Topic;
        public int RouteSubscriberIndex;
        public bool Active;
    }

    private delegate void MessageHandler(ReadOnlySpan<byte> payload, MessageKind kind, Topic topic);

    private enum SubscriberPayloadKind : byte
    {
        Raw,
        Typed,
        Packed,
    }

    private sealed class TopicRoute
    {
        private int[] _freeSlots;
        private int _freeSlotCount;
        private int _nextSlot;

        public TopicRoute(Topic topic, int subscriberCapacity)
        {
            Topic = topic;
            Subscribers = new SubscriberEntry[subscriberCapacity];
            _freeSlots = new int[subscriberCapacity];
        }

        public bool HasTopic;
        public Topic Topic { get; }
        public SubscriberEntry[] Subscribers;
        public int ActiveCount;
        public int HighWaterMark;

        public int RentSlot()
        {
            if (_freeSlotCount > 0)
            {
                return _freeSlots[--_freeSlotCount];
            }

            if (_nextSlot == Subscribers.Length)
            {
                Grow();
            }

            return _nextSlot++;
        }

        public void ActivateSlot(int slotIndex)
        {
            if (slotIndex >= HighWaterMark)
            {
                Volatile.Write(ref HighWaterMark, slotIndex + 1);
            }

            Volatile.Write(ref Subscribers[slotIndex].Active, 1);
            Interlocked.Increment(ref ActiveCount);
        }

        public void ReturnSlot(int slotIndex)
        {
            if (_freeSlotCount == _freeSlots.Length)
            {
                Array.Resize(ref _freeSlots, _freeSlots.Length * 2);
            }

            _freeSlots[_freeSlotCount++] = slotIndex;
            Interlocked.Decrement(ref ActiveCount);
        }

        private void Grow()
        {
            var oldLength = Subscribers.Length;
            Array.Resize(ref _freeSlots, _freeSlots.Length * 2);
            Array.Resize(ref Subscribers, oldLength * 2);
        }
    }

    private struct SubscriberEntry
    {
        public SubscriberEntry(
            int slotIndex,
            int generation,
            Topic topic,
            SubscriberPayloadKind payloadKind,
            nint payloadTypeHandle,
            int payloadSize,
            Delegate originalHandler,
            MessageHandler handler)
        {
            SlotIndex = slotIndex;
            Generation = generation;
            Topic = topic;
            PayloadKind = payloadKind;
            PayloadTypeHandle = payloadTypeHandle;
            PayloadSize = payloadSize;
            OriginalHandler = originalHandler;
            Handler = handler;
            Active = 0;
        }

        public int SlotIndex { get; }
        public int Generation { get; }
        public Topic Topic { get; }
        public SubscriberPayloadKind PayloadKind { get; }
        public nint PayloadTypeHandle { get; }
        public int PayloadSize { get; }
        public Delegate OriginalHandler { get; }
        public MessageHandler Handler { get; }
        public int Active;

        public bool CanAccept(SubscriberPayloadKind payloadKind, nint payloadTypeHandle, int payloadSize) =>
            PayloadKind switch
            {
                SubscriberPayloadKind.Raw => true,
                SubscriberPayloadKind.Packed => payloadKind == SubscriberPayloadKind.Packed && payloadSize == PubSubPayload.Size,
                _ => payloadKind == SubscriberPayloadKind.Typed && PayloadTypeHandle == payloadTypeHandle && PayloadSize == payloadSize,
            };
    }
}

/// <summary>
/// Lock-free fixed-capacity message pool used by <see cref="LockFreePubSub"/>.
/// </summary>
public sealed class LockFreeMessagePool : IMessagePool
{
    private readonly MessageSlot[] _slots;
    private readonly int[] _next;
    private readonly int _slotIndexMask;
    private int _freeHead;
    private int _activeCount;
    private long _sequence;
    private long _exhaustedRentCount;
    private long _forcedReclamationCount;

    /// <summary>
    /// Creates a fixed-capacity message pool.
    /// </summary>
    public LockFreeMessagePool(int capacity = 8192, int inlinePayloadSize = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inlinePayloadSize);

        InlinePayloadSize = inlinePayloadSize;
        _slots = new MessageSlot[capacity];
        _next = new int[capacity];
        _slotIndexMask = (capacity & (capacity - 1)) == 0 ? capacity - 1 : -1;

        for (var i = 0; i < capacity; i++)
        {
            _slots[i] = new MessageSlot(inlinePayloadSize);
            _next[i] = i + 1;
        }

        _next[capacity - 1] = -1;
        _freeHead = 0;
    }

    /// <summary>
    /// Maximum payload bytes stored inline in each slot.
    /// </summary>
    public int InlinePayloadSize { get; }

    /// <inheritdoc />
    public int Capacity => _slots.Length;

    /// <inheritdoc />
    public int ActiveCount => Volatile.Read(ref _activeCount);

    /// <summary>
    /// Number of rent attempts that failed because all slots were active.
    /// </summary>
    public long ExhaustedRentCount => Interlocked.Read(ref _exhaustedRentCount);

    /// <summary>
    /// Number of active slots reclaimed by frame-boundary reset.
    /// </summary>
    public long ForcedReclamationCount => Interlocked.Read(ref _forcedReclamationCount);

    /// <inheritdoc />
    public MessageHandle Rent()
    {
        if (TryRentForPublish(default, MessageKind.Unknown, payloadLength: 0, out var handle))
        {
            return handle;
        }

        throw new InvalidOperationException("The message pool is exhausted.");
    }

    /// <inheritdoc />
    public void Return(MessageHandle handle)
    {
        if (!IsCurrentHandle(handle, out var slot))
        {
            return;
        }

        while (true)
        {
            var refCount = Volatile.Read(ref slot.RefCount);
            if (refCount <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref slot.RefCount, refCount - 1, refCount) != refCount)
            {
                continue;
            }

            if (refCount == 1)
            {
                slot.PayloadLength = 0;
                slot.UsesArena = false;
                slot.ArenaOffset = 0;
                if (slot.RentedFromFreeStack)
                {
                    PushFreeSlot(handle.SlotIndex);
                }

                Interlocked.Decrement(ref _activeCount);
            }

            return;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        var activeCount = Volatile.Read(ref _activeCount);
        if (activeCount > 0)
        {
            Interlocked.Add(ref _forcedReclamationCount, activeCount);
        }

        for (var i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            slot.Generation = NextGeneration(slot.Generation);
            slot.RefCount = 0;
            slot.Topic = default;
            slot.Kind = MessageKind.Unknown;
            slot.PayloadLength = 0;
            slot.UsesArena = false;
            slot.ArenaOffset = 0;
            slot.RentedFromFreeStack = false;
            _next[i] = i + 1;
        }

        _next[_next.Length - 1] = -1;
        Volatile.Write(ref _freeHead, 0);
        Volatile.Write(ref _activeCount, 0);
    }

    internal bool TryRent(Topic topic, MessageKind kind, int payloadLength, out MessageHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadLength);

        while (true)
        {
            var head = Volatile.Read(ref _freeHead);
            if (head < 0)
            {
                Interlocked.Increment(ref _exhaustedRentCount);
                handle = MessageHandle.Invalid;
                return false;
            }

            var next = _next[head];
            if (Interlocked.CompareExchange(ref _freeHead, next, head) != head)
            {
                continue;
            }

            var slot = _slots[head];
            var generation = NextGeneration(slot.Generation);
            var sequence = Interlocked.Increment(ref _sequence);
            slot.Generation = generation;
            slot.Topic = topic;
            slot.Kind = kind;
            slot.PayloadLength = payloadLength;
            slot.UsesArena = false;
            slot.ArenaOffset = 0;
            slot.RentedFromFreeStack = true;
            Volatile.Write(ref slot.RefCount, 1);
            Interlocked.Increment(ref _activeCount);

            handle = new MessageHandle(this, head, generation, topic, kind, payloadLength, sequence);
            return true;
        }
    }

    internal bool TryRentForPublish(Topic topic, MessageKind kind, int payloadLength, out MessageHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadLength);

        var sequence = Interlocked.Increment(ref _sequence);
        var start = GetSlotIndex(sequence - 1);
        for (var probe = 0; probe < _slots.Length; probe++)
        {
            var slotIndex = _slotIndexMask >= 0
                ? (start + probe) & _slotIndexMask
                : (start + probe) % _slots.Length;
            var slot = _slots[slotIndex];
            if (Interlocked.CompareExchange(ref slot.RefCount, 1, 0) != 0)
            {
                continue;
            }

            var generation = NextGeneration(slot.Generation);
            slot.Generation = generation;
            slot.Topic = topic;
            slot.Kind = kind;
            slot.PayloadLength = payloadLength;
            slot.UsesArena = false;
            slot.ArenaOffset = 0;
            slot.RentedFromFreeStack = false;
            Interlocked.Increment(ref _activeCount);

            handle = new MessageHandle(this, slotIndex, generation, topic, kind, payloadLength, sequence);
            return true;
        }

        Interlocked.Increment(ref _exhaustedRentCount);
        handle = MessageHandle.Invalid;
        return false;
    }

    private int GetSlotIndex(long value) =>
        _slotIndexMask >= 0
            ? (int)value & _slotIndexMask
            : (int)(value % _slots.Length);

    internal void ReturnPublished(MessageHandle handle)
    {
        if (!IsCurrentHandle(handle, out var slot))
        {
            return;
        }

        slot.PayloadLength = 0;
        slot.UsesArena = false;
        slot.ArenaOffset = 0;
        Volatile.Write(ref slot.RefCount, 0);
        Interlocked.Decrement(ref _activeCount);
    }

    internal Span<byte> GetInlinePayloadSpan(MessageHandle handle, int payloadLength)
    {
        if (payloadLength > InlinePayloadSize)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLength), payloadLength, "Payload is larger than the inline slot.");
        }

        if (!IsCurrentHandle(handle, out var slot))
        {
            throw new InvalidOperationException("The message handle is not active in this pool.");
        }

        slot.PayloadLength = payloadLength;
        slot.UsesArena = false;
        slot.ArenaOffset = 0;
        return slot.InlinePayload.AsSpan(0, payloadLength);
    }

    internal void SetArenaPayload(MessageHandle handle, int offset, int payloadLength)
    {
        if (!IsCurrentHandle(handle, out var slot))
        {
            throw new InvalidOperationException("The message handle is not active in this pool.");
        }

        slot.PayloadLength = payloadLength;
        slot.UsesArena = true;
        slot.ArenaOffset = offset;
    }

    internal ReadOnlySpan<byte> GetPayloadSpan(MessageHandle handle, PayloadArena arena)
    {
        if (!IsCurrentHandle(handle, out var slot))
        {
            throw new InvalidOperationException("The message handle is not active in this pool.");
        }

        return slot.UsesArena
            ? arena.GetSpan(slot.ArenaOffset, slot.PayloadLength)
            : slot.InlinePayload.AsSpan(0, slot.PayloadLength);
    }

    private void PushFreeSlot(int slotIndex)
    {
        while (true)
        {
            var head = Volatile.Read(ref _freeHead);
            _next[slotIndex] = head;
            if (Interlocked.CompareExchange(ref _freeHead, slotIndex, head) == head)
            {
                return;
            }
        }
    }

    private bool IsCurrentHandle(MessageHandle handle, out MessageSlot slot)
    {
        slot = null!;
        if (!handle.IsValid || !ReferenceEquals(handle.Owner, this) || (uint)handle.SlotIndex >= (uint)_slots.Length)
        {
            return false;
        }

        var candidate = _slots[handle.SlotIndex];
        if (Volatile.Read(ref candidate.RefCount) <= 0 || candidate.Generation != handle.Generation)
        {
            return false;
        }

        slot = candidate;
        return true;
    }

    private static int NextGeneration(int generation)
    {
        var next = unchecked(generation + 1);
        return next <= 0 ? 1 : next;
    }

    private sealed class MessageSlot
    {
        public MessageSlot(int inlinePayloadSize)
        {
            InlinePayload = new byte[inlinePayloadSize];
        }

        public readonly byte[] InlinePayload;
        public int Generation;
        public int RefCount;
        public Topic Topic;
        public MessageKind Kind;
        public int PayloadLength;
        public bool UsesArena;
        public bool RentedFromFreeStack;
        public int ArenaOffset;
    }
}

/// <summary>
/// Frame-scoped bump allocator for variable-sized pub/sub payloads.
/// </summary>
public sealed class PayloadArena
{
    private readonly object _growLock = new();
    private readonly int _maxCapacity;
    private byte[] _buffer;
    private int _offset;
    private long _growCount;
    private long _allocationFailureCount;

    /// <summary>
    /// Creates a frame-scoped payload arena.
    /// </summary>
    public PayloadArena(int initialCapacity = 64 * 1024, int maxCapacity = 64 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCapacity, initialCapacity);

        _buffer = new byte[initialCapacity];
        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// Current arena capacity in bytes.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Maximum arena capacity in bytes.
    /// </summary>
    public int MaxCapacity => _maxCapacity;

    /// <summary>
    /// Bytes currently reserved in this frame.
    /// </summary>
    public int Used => Volatile.Read(ref _offset);

    /// <summary>
    /// Number of arena growth operations.
    /// </summary>
    public long GrowCount => Interlocked.Read(ref _growCount);

    /// <summary>
    /// Number of allocation requests that could not fit within <see cref="MaxCapacity"/>.
    /// </summary>
    public long AllocationFailureCount => Interlocked.Read(ref _allocationFailureCount);

    /// <summary>
    /// Attempts to reserve a contiguous payload range.
    /// </summary>
    public bool TryAllocate(int length, out int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (length == 0)
        {
            offset = 0;
            return true;
        }

        while (true)
        {
            var current = Volatile.Read(ref _offset);
            var next = current + length;
            if (next < current)
            {
                Interlocked.Increment(ref _allocationFailureCount);
                offset = -1;
                return false;
            }

            var buffer = _buffer;
            if (next <= buffer.Length)
            {
                if (Interlocked.CompareExchange(ref _offset, next, current) == current)
                {
                    offset = current;
                    return true;
                }

                continue;
            }

            if (!GrowToFit(next))
            {
                Interlocked.Increment(ref _allocationFailureCount);
                offset = -1;
                return false;
            }
        }
    }

    /// <summary>
    /// Gets a mutable span over an allocated arena range.
    /// </summary>
    public Span<byte> GetSpan(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > _buffer.Length - length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "The requested arena range is outside the backing buffer.");
        }

        return _buffer.AsSpan(offset, length);
    }

    /// <summary>
    /// Resets the arena for a new frame.
    /// </summary>
    public void Reset() => Volatile.Write(ref _offset, 0);

    private bool GrowToFit(int requiredCapacity)
    {
        lock (_growLock)
        {
            if (requiredCapacity <= _buffer.Length)
            {
                return true;
            }

            if (requiredCapacity > _maxCapacity)
            {
                return false;
            }

            var nextCapacity = _buffer.Length;
            while (nextCapacity < requiredCapacity)
            {
                var doubled = nextCapacity * 2;
                nextCapacity = doubled <= 0 || doubled > _maxCapacity ? _maxCapacity : doubled;
                if (nextCapacity == _maxCapacity)
                {
                    break;
                }
            }

            var next = new byte[nextCapacity];
            _buffer.AsSpan(0, Volatile.Read(ref _offset)).CopyTo(next);
            _buffer = next;
            Interlocked.Increment(ref _growCount);
            return true;
        }
    }
}
