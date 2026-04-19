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
using ViceSharp.Core;

namespace ViceSharp.Architectures;

/// <summary>
/// C64 Architecture descriptor providing machine configuration.
/// </summary>
public sealed class C64Descriptor : IArchitectureDescriptor
{
    /// <inheritdoc />
    public string MachineName { get; } = "Commodore 64 PAL";

    /// <inheritdoc />
    public long MasterClockHz { get; } = 985248;

    /// <inheritdoc />
    public VideoStandard VideoStandard { get; } = VideoStandard.Pal;
}

/// <summary>
/// C64 Architecture descriptor for NTSC variant.
/// </summary>
public sealed class C64NtscDescriptor : IArchitectureDescriptor
{
    /// <inheritdoc />
    public string MachineName { get; } = "Commodore 64 NTSC";

    /// <inheritdoc />
    public long MasterClockHz { get; } = 1022727;

    /// <inheritdoc />
    public VideoStandard VideoStandard { get; } = VideoStandard.Ntsc;
}

/// <summary>
/// C64 VIC-II standard palette
/// </summary>
public static class C64Palette
{
    public static readonly uint[] Colors =
    [
        0xFF000000, // 0: Black
        0xFFFFFFFF, // 1: White
        0xFF880000, // 2: Red
        0xFFAAFFEE, // 3: Cyan
        0xFFCC44CC, // 4: Purple
        0xFF00CC55, // 5: Green
        0xFF0000AA, // 6: Blue
        0xFFEEEE77, // 7: Yellow
        0xFFDD8855, // 8: Orange
        0xFF664400, // 9: Brown
        0xFFFF7777, // 10: Light Red
        0xFF333333, // 11: Dark Grey
        0xFF777777, // 12: Grey
        0xFFAAFF66, // 13: Light Green
        0xFF0088FF, // 14: Light Blue
        0xFFBBBBBB  // 15: Light Grey
    ];
}
