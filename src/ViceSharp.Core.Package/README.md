# ViceSharp.Core

The managed C64 emulation core for ViceSharp, a C#/.NET 10 port of VICE (the Versatile Commodore Emulator).

## What it is

ViceSharp.Core is a bundle package: a single NuGet that carries the five emulation-core assemblies (ViceSharp.Abstractions, ViceSharp.Chips, ViceSharp.RomFetch, ViceSharp.Core, and ViceSharp.Architectures). Abstractions is the emulator contract (33+ interfaces and value types); Core provides the bus, system clock, mutation queue, lock-free pub/sub, snapshots, and media recorders; Chips implements the CPU (6502/6510/8502), VIC-II, SID, CIA, VIA, and PLA; Architectures defines the C64 machine. The design is library-first with a zero-allocation per-cycle loop, POCO state, and deterministic (bit-exact) replay: the managed C64 runs in cycle-exact lockstep with VICE x64sc, and the SID is validated bit-exact against reSID.

## Install

```
dotnet add package ViceSharp.Core
```

## Notes

This package ships no Commodore ROMs. Point `VICESHARP_ROM_PATH` (or `VICE_DATA_PATH`) at a VICE data root that contains the `C64/` and `DRIVES/` resources, or put `x64sc.exe` on your `PATH`; ViceSharp.RomFetch resolves the ROMs from there. Runtime dependencies are Microsoft.Extensions.Configuration(.Abstractions) for the settings provider and YamlDotNet for machine definitions.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
