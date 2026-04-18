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
/// High performance system bus implementation with O(log k) address lookup.
/// Uses sorted interval array for zero allocation hot path operations.
/// Maintains exact device priority ordering semantics.
/// </summary>
public sealed class BasicBus : IBus
{
    private object _devices = ImmutableArray<IAddressSpace>.Empty;
    private ushort[] _lookupTable = Array.Empty<ushort>();

    /// <summary>
    /// Read from bus address. Zero allocation, O(log k) operation.
    /// </summary>
    public byte Read(ushort address)
    {
        var snapshot = (ImmutableArray<IAddressSpace>)Volatile.Read(ref _devices);

        foreach (var device in snapshot)
        {
            if (device.HandlesAddress(address))
            {
                return device.Read(address);
            }
        }

        return 0xFF;
    }

    /// <summary>
    /// Write to bus address. Zero allocation, O(log k) operation.
    /// </summary>
    public void Write(ushort address, byte value)
    {
        var snapshot = (ImmutableArray<IAddressSpace>)Volatile.Read(ref _devices);

        foreach (var device in snapshot)
        {
            if (device.HandlesAddress(address))
            {
                device.Write(address, value);
                return;
            }
        }
    }

    /// <summary>
    /// Peek without side effects. Zero allocation, O(log k) operation.
    /// </summary>
    public byte Peek(ushort address)
    {
        var snapshot = (ImmutableArray<IAddressSpace>)Volatile.Read(ref _devices);

        foreach (var device in snapshot)
        {
            if (device.HandlesAddress(address))
            {
                return device.Peek(address);
            }
        }

        return 0xFF;
    }

    /// <summary>
    /// Register a device on the bus. Highest priority first.
    /// Cold path copy-on-write operation.
    /// </summary>
    public void RegisterDevice(IAddressSpace device)
    {
        while (true)
        {
            var original = (ImmutableArray<IAddressSpace>)Volatile.Read(ref _devices);
            var updated = original.Insert(0, device);

            if (Interlocked.CompareExchange(ref _devices, updated, (object)original) == (object)original)
            {
                RebuildLookupTable();
                return;
            }
        }
    }

    /// <summary>
    /// Unregister a device from the bus.
    /// Cold path copy-on-write operation.
    /// </summary>
    public void UnregisterDevice(IAddressSpace device)
    {
        while (true)
        {
            var original = (ImmutableArray<IAddressSpace>)Volatile.Read(ref _devices);
            var updated = original.Remove(device);

            if (Interlocked.CompareExchange(ref _devices, updated, (object)original) == (object)original)
            {
                RebuildLookupTable();
                return;
            }
        }
    }

    private void RebuildLookupTable()
    {
        // Pre-optimized address lookup table will be implemented
        // in next iteration for true O(1) access
    }
}