# ViceSharp.Console

The ViceSharp CLI reference shell, packaged as a .NET global tool for driving the managed C64 emulation core from the command line.

## What it is

ViceSharp.Console is the command-line reference shell for ViceSharp, a C#/.NET 10 port of the VICE Commodore 64 emulator. It builds a C64 machine, resets it, runs a bounded number of cycles, and reports clock and CPU state, acting as a headless debug monitor and regression testbench. It can also load ad-hoc or multi-system machine definitions from YAML, emit an optional deterministic instruction trace, and export the built-in C64 machine definitions.

## Install

It ships as a .NET global tool. Install it with:

```
dotnet tool install --global ViceSharp.Console
```

Then run it with the `vicesharp-console` command:

```
vicesharp-console --cycles 100000
```

## Usage

Common options:

- `--cycles <n>`: number of emulation cycles to run.
- `--roms <path>`: point at a VICE data root that holds the Commodore ROMs.
- `--trace <file>`: write a deterministic instruction trace.
- `--machine-yaml <path>` (or `-m`): load an ad-hoc or multi-system machine definition.
- `--export-machines <dir>`: write one YAML definition per built-in C64 variant, then exit.
- `--help` (or `-h`, `-?`): show the full option list.

ViceSharp ships no Commodore ROMs. Set the `VICESHARP_ROM_PATH` environment variable (or pass `--roms`) to a VICE data root so the tool can locate KERNAL, BASIC, and character ROMs.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
