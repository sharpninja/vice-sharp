# ViceSharp.Launcher

VICE-compatible command-line argument parsing and machine bootstrap for the ViceSharp C64 emulator.

## What it is

ViceSharp.Launcher is a small class library that turns a VICE-style command line (the flags `x64sc`, `c1541`, and friends accept) into a strongly typed `ViceArgs` bundle, then maps that bundle to the multi-system topology YAML the ViceSharp host consumes. `ViceArgsParser.Parse` mirrors VICE 3.x flag conventions for the subset ViceSharp supports (`-8`, `-9`, `-cart`, `+/-truedrive`, `--machine-yaml`, `--cycles`, plus the testbench flags `-debugcart`, `--limitcycles`, and `.prg` autostart), collecting unrecognized flags into `ViceArgs.Unknown` rather than throwing. `ViceTopologyBuilder.BuildYaml` then emits a `--machine-yaml` topology string (C64 host with optional drives, or a standalone c1541 tool), and `ParseDescriptor` reads testbench keys back out. It references ViceSharp.Abstractions, ViceSharp.Architectures, and ViceSharp.Core.

## Install

```
dotnet add package ViceSharp.Launcher
```

## Usage

```csharp
using ViceSharp.Launcher;

var parsed = ViceArgsParser.Parse("x64sc", new[] { "-8", "game.d64", "+truedrive" });
string topologyYaml = ViceTopologyBuilder.BuildYaml(parsed);
```

Feed `topologyYaml` to the ViceSharp host via its `--machine-yaml` option. Note that ViceSharp ships no Commodore ROMs; point `VICESHARP_ROM_PATH` at a VICE data root before running the emulator. Only `x64`, `x64sc`, and `c1541` binary names are currently supported; others throw `NotSupportedException`.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp
