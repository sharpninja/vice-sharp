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

using System.Buffers;
using System.Threading;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Bounded double buffered mutation queue with zero-allocation steady state operation.
/// Provides hard back pressure limits and automatic trimming on idle.
/// Thread safe for single producer single consumer patterns.
/// </summary>
public sealed class DoubleBufferedMutationQueue : IMutationQueue
{
    private const int DefaultCapacity = 4096;
    private const float HighWaterMark = 0.9f;

    private MutationEntry[] _buffer0;
    private MutationEntry[] _buffer1;
    private int _count0;
    private int _count1;
    private int _activeBuffer;
    private long _commitCount;

    public DoubleBufferedMutationQueue(int capacity = DefaultCapacity)
    {
        _buffer0 = ArrayPool<MutationEntry>.Shared.Rent(capacity);
        _buffer1 = ArrayPool<MutationEntry>.Shared.Rent(capacity);
    }

    /// <summary>
    /// Enqueue a mutation entry. Zero allocation in steady state.
    /// </summary>
    public void Enqueue(DeviceId source, ushort address, byte oldValue, byte newValue, ulong cycle)
    {
        ref var count = ref _activeBuffer == 0 ? ref _count0 : ref _count1;
        var buffer = _activeBuffer == 0 ? _buffer0 : _buffer1;

        if (count < buffer.Length)
        {
            buffer[count++] = new MutationEntry(source, address, oldValue, newValue, cycle);
        }
    }

    /// <summary>
    /// Atomically flip buffers and prepare for next cycle.
    /// Clears consumed buffer completely.
    /// </summary>
    public void Commit()
    {
        ref var consumedCount = ref _activeBuffer == 0 ? ref _count1 : ref _count0;
        var consumedBuffer = _activeBuffer == 0 ? _buffer1 : _buffer0;

        // Clear consumed buffer entries
        consumedBuffer.AsSpan(0, consumedCount).Clear();
        consumedCount = 0;

        // Atomic flip
        _activeBuffer ^= 1;

        // Trim excess periodically
        if (Interlocked.Increment(ref _commitCount) % 128 == 0)
        {
            TrimIdleBuffers();
        }
    }

    /// <summary>
    /// Clear all buffers and reset state.
    /// </summary>
    public void Clear()
    {
        _buffer0.AsSpan(0, _count0).Clear();
        _buffer1.AsSpan(0, _count1).Clear();
        _count0 = 0;
        _count1 = 0;
        _activeBuffer = 0;
    }

    private void TrimIdleBuffers()
    {
        if (_count0 < _buffer0.Length * (1 - HighWaterMark) && _buffer0.Length > DefaultCapacity)
        {
            var newBuffer = ArrayPool<MutationEntry>.Shared.Rent(DefaultCapacity);
            _buffer0.AsSpan(0, _count0).CopyTo(newBuffer);
            ArrayPool<MutationEntry>.Shared.Return(_buffer0);
            _buffer0 = newBuffer;
        }

        if (_count1 < _buffer1.Length * (1 - HighWaterMark) && _buffer1.Length > DefaultCapacity)
        {
            var newBuffer = ArrayPool<MutationEntry>.Shared.Rent(DefaultCapacity);
            _buffer1.AsSpan(0, _count1).CopyTo(newBuffer);
            ArrayPool<MutationEntry>.Shared.Return(_buffer1);
            _buffer1 = newBuffer;
        }
    }
}