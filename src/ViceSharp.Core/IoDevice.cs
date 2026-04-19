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

using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// C64 I/O address space device.
/// Maps the $D000-$DFFF I/O region to VIC-II, SID, CIA, and expansion ports.
/// </summary>
public sealed class IoDevice : IAddressSpace
{
    // I/O region spans $D000-$DFFF (4096 bytes)
    private const ushort StartAddress = 0xD000;
    private const ushort EndAddress = 0xDFFF;

    // Sub-region sizes
    private const int VicRegionSize = 0x0400;  // $D000-$D3FF (1KB)
    private const int SidRegionSize = 0x0400;   // $D400-$D7FF (1KB)
    private const int ColorRamSize = 0x0400;    // $D800-$DBFF (1KB)
    private const int Cia1RegionSize = 0x0100;  // $DC00-$DCFF (256B)
    private const int Cia2RegionSize = 0x0100;  // $DD00-$DDFF (256B)
    private const int Io1RegionSize = 0x0100;   // $DE00-$DEFF (256B)
    private const int Io2RegionSize = 0x0100;   // $DF00-$DFFF (256B)

    // Read/write buffers for I/O registers (reads often return latched values)
    private readonly byte[] _vicRegisters = new byte[0x40];
    private readonly byte[] _sidRegisters = new byte[0x20];
    private readonly byte[] _cia1Registers = new byte[0x10];
    private readonly byte[] _cia2Registers = new byte[0x10];
    private readonly byte[] _io1Registers = new byte[0x10];
    private readonly byte[] _io2Registers = new byte[0x10];

    // Color RAM (separate 4-bit wide RAM at $D800)
    private readonly byte[] _colorRam = new byte[0x400];

    /// <summary>
    /// Creates a new I/O device with the specified interrupt lines.
    /// </summary>
    public IoDevice()
    {
    }

    /// <inheritdoc />
    public byte Read(ushort address)
    {
        // Strip high bits to get offset within I/O region
        ushort offset = (ushort)((address - StartAddress) & 0x3FF);

        return offset switch
        {
            < VicRegionSize => _vicRegisters[offset],          // $D000-$D3FF
            < VicRegionSize + SidRegionSize => _sidRegisters[offset - VicRegionSize], // $D400-$D7FF
            < VicRegionSize + SidRegionSize + ColorRamSize => _colorRam[offset - VicRegionSize - SidRegionSize], // $D800-$DBFF
            < VicRegionSize + SidRegionSize + ColorRamSize + Cia1RegionSize => _cia1Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize], // $DC00-$DCFF
            < VicRegionSize + SidRegionSize + ColorRamSize + Cia1RegionSize + Cia2RegionSize => _cia2Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize - Cia1RegionSize], // $DD00-$DDFF
            < VicRegionSize + SidRegionSize + ColorRamSize + Cia1RegionSize + Cia2RegionSize + Io1RegionSize => _io1Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize - Cia1RegionSize - Cia2RegionSize], // $DE00-$DEFF
            _ => _io2Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize - Cia1RegionSize - Cia2RegionSize - Io1RegionSize] // $DF00-$DFFF
        };
    }

    /// <inheritdoc />
    public void Write(ushort address, byte value)
    {
        // Strip high bits to get offset within I/O region
        ushort offset = (ushort)((address - StartAddress) & 0x3FF);

        if (offset < VicRegionSize)
        {
            _vicRegisters[offset] = value;
        }
        else if (offset < VicRegionSize + SidRegionSize)
        {
            _sidRegisters[offset - VicRegionSize] = value;
        }
        else if (offset < VicRegionSize + SidRegionSize + ColorRamSize)
        {
            // Color RAM only stores lower 4 bits
            _colorRam[offset - VicRegionSize - SidRegionSize] = (byte)(value & 0x0F);
        }
        else if (offset < VicRegionSize + SidRegionSize + ColorRamSize + Cia1RegionSize)
        {
            _cia1Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize] = value;
        }
        else if (offset < VicRegionSize + SidRegionSize + ColorRamSize + Cia1RegionSize + Cia2RegionSize)
        {
            _cia2Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize - Cia1RegionSize] = value;
        }
        else if (offset < VicRegionSize + SidRegionSize + ColorRamSize + Cia1RegionSize + Cia2RegionSize + Io1RegionSize)
        {
            _io1Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize - Cia1RegionSize - Cia2RegionSize] = value;
        }
        else
        {
            _io2Registers[offset - VicRegionSize - SidRegionSize - ColorRamSize - Cia1RegionSize - Cia2RegionSize - Io1RegionSize] = value;
        }
    }

    /// <inheritdoc />
    public byte Peek(ushort address) => Read(address);

    /// <inheritdoc />
    public bool HandlesAddress(ushort address) => address >= StartAddress && address <= EndAddress;

    /// <inheritdoc />
    public DeviceId Id => new DeviceId(0x00010002);

    /// <inheritdoc />
    public string Name => "I/O Device";

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_vicRegisters, 0, _vicRegisters.Length);
        Array.Clear(_cia1Registers, 0, _cia1Registers.Length);
        Array.Clear(_cia2Registers, 0, _cia2Registers.Length);
        Array.Clear(_io1Registers, 0, _io1Registers.Length);
        Array.Clear(_io2Registers, 0, _io2Registers.Length);
        // SID registers retain values after reset
    }
}
