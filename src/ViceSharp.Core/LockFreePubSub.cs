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

using System.Collections.Immutable;
using System.Threading;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Truly lock-free publish/subscribe implementation with zero-allocation hot path.
/// Publish operations are wait-free, ABA safe, and never block.
/// Subscribe/Unsubscribe are cold path operations with full copy-on-write semantics.
/// </summary>
public sealed class LockFreePubSub : IPubSub
{
    private ImmutableDictionary<TopicId, ImmutableArray<Action<ReadOnlySpan<byte>>>> _subscribers =
        ImmutableDictionary<TopicId, ImmutableArray<Action<ReadOnlySpan<byte>>>>.Empty;

    /// <summary>
    /// Publish a message to all subscribers of the given topic.
    /// This method is 100% lock-free, zero-allocation, and thread safe.
    /// </summary>
    public void Publish(TopicId topic, ReadOnlySpan<byte> payload)
    {
        // Volatile read ensures we see the latest consistent snapshot
        var snapshot = Volatile.Read(ref _subscribers);

        if (snapshot.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler(payload);
            }
        }
    }

    /// <summary>
    /// Subscribe to messages on the given topic.
    /// This is a cold path operation that performs copy-on-write.
    /// </summary>
    public void Subscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler)
    {
        while (true)
        {
            var original = Volatile.Read(ref _subscribers);
            var currentHandlers = original.TryGetValue(topic, out var existing)
                ? existing
                : ImmutableArray<Action<ReadOnlySpan<byte>>>.Empty;

            var updatedHandlers = currentHandlers.Add(handler);
            var updated = original.SetItem(topic, updatedHandlers);

            if (Interlocked.CompareExchange(ref _subscribers, updated, original) == original)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Unsubscribe from messages on the given topic.
    /// This is a cold path operation that performs copy-on-write.
    /// </summary>
    public void Unsubscribe(TopicId topic, Action<ReadOnlySpan<byte>> handler)
    {
        while (true)
        {
            var original = Volatile.Read(ref _subscribers);

            if (!original.TryGetValue(topic, out var existing))
            {
                return;
            }

            var updatedHandlers = existing.Remove(handler);
            var updated = updatedHandlers.IsEmpty
                ? original.Remove(topic)
                : original.SetItem(topic, updatedHandlers);

            if (Interlocked.CompareExchange(ref _subscribers, updated, original) == original)
            {
                return;
            }
        }
    }
}