# ViceSharp.Monitor

The ViceSharp machine-language monitor and debugger surface for the C#/.NET 10 port of VICE.

## What it is

ViceSharp.Monitor is a class library that provides the debugging surface for a running ViceSharp machine. It exposes a `Monitor` type (implementing `IMonitor`) with a text command interpreter covering CPU registers, single-step and step-over, memory dumps, breakpoints, reset, and cycle counts. It also ships `Mos6502Disassembler`, a length-accurate 6502/6510 disassembler that covers all 256 opcodes (including the undocumented set, using VICE mnemonics), a `DeterministicTraceLogger` that emits x64sc-compatible traces for cycle-accurate validation, and view helpers (`CpuStatusView`, `MemoryInspectorView`, `BreakpointController`) under `ViceSharp.Monitor.Views`.

## Install

```
dotnet add package ViceSharp.Monitor
```

## Notes

This is a library, not a standalone tool. It builds on `ViceSharp.Abstractions`, `ViceSharp.Chips`, and `ViceSharp.Core`: construct a `Monitor` around an `IMachine` and drive it with `ExecuteCommand`, or attach a `DeterministicTraceLogger` to a machine to capture VICE-format traces. ViceSharp ships no Commodore ROMs; the machine you attach the monitor to needs ROMs supplied via `VICESHARP_ROM_PATH` (see `docs/ROMs.md`).

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
