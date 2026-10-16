# ViceSharp

A C# port of [VICE](https://vice-emu.sourceforge.io/) (Versatile Commodore Emulator) targeting .NET 10.

> **Parity is under active re-audit.** Native VICE is the immutable behavioral oracle. Historical C64 and VIC-20 lockstep receipts prove bounded slices only; they do not establish whole-emulator parity. Canonical native VICE `.vsf` save/load remains a required, open product gate. See [docs/Iteration-Roadmap.md](docs/Iteration-Roadmap.md), [PLAN-VICE-IMMUTABLE-001](docs/plans/PLAN-VICE-IMMUTABLE-001.md), and [PLAN-VSFLOCKSTEP-001](docs/plans/PLAN-VSFLOCKSTEP-001.md).

## Quick Start

```pwsh
# 1. Clone and build
git clone https://github.com/sharpninja/vice-sharp.git vice-sharp
cd vice-sharp
dotnet build ViceSharp.slnx

# 2. Point at your VICE data root, or put x64sc.exe on PATH (see docs/ROMs.md)
$env:VICESHARP_ROM_PATH = "C:\path\to\GTK3VICE-3.8-win64"

# 3. Boot a C64 with a true-drive 1541 attached
dotnet run --project src/ViceSharp.Console -- `
    --roms $env:VICESHARP_ROM_PATH `
    --machine-yaml docs/samples/c64-plus-1541.multisystem.yaml `
    --cycles 1000000
```

Coming from classic VICE? The `ViceSharp.Launcher` library provides VICE-compatible argument parsing and topology generation for `x64`, `x64sc`, `xvic`, and `c1541`. `ViceSharp.Console` still parses its process arguments as `x64sc`, and standalone VICE-named binaries are not shipped. The generated `xvic` topology is not yet an end-to-end console path because the generic multi-system kind dispatcher recognizes only `C64` and `C1541`. See [docs/USER-GUIDE.md](docs/USER-GUIDE.md) and [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md).

## Install

The current line is **v1.3.3** (GitVersion `next-version`; NuGet + MSI / winget packaging). Published nuget.org / winget packages may lag until the next release tag.

```pwsh
# Desktop UI as a dotnet global tool (command: vicesharp)
dotnet tool install --global ViceSharp.Avalonia

# Console reference shell as a dotnet global tool (command: vicesharp-console)
dotnet tool install --global ViceSharp.Console

# Embed the emulation core (Abstractions + Chips + RomFetch + Core + Architectures) in your own app
dotnet add package ViceSharp.Core
```

Individual packages (`ViceSharp.Protocol`, `ViceSharp.Monitor`, `ViceSharp.Launcher`, `ViceSharp.AdhocHelper`, `ViceSharp.Host`, `ViceSharp.SourceGen`, and the `ViceSharp.Host.MacOS` / `Android` / `iOS` shells) are published alongside the bundle. Xbox/UWP is outside the supported product scope; its source remains frozen legacy. The Windows desktop app is also packaged as a self-contained MSI (Nuke `PublishMsi`) with winget metadata (`PublishWinget`, package id `sharpninja.ViceSharp`).

## User documentation

- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) - install, first run, CLI launcher, YAML topology, disk images, desktop drag-drop (including `*.prg` load/RUN), capture, diagnostics attach, what works today
- [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md) - binary + flag mapping, behaviour caveats, performance / accuracy, bug compatibility
- [docs/ROMs.md](docs/ROMs.md) - legal ROM options, environment variable, directory layout
- [docs/](docs/README.md) - full documentation index (architecture, public API, iteration plans, diagrams)

## Status

> Status audited 2026-10-04. Native VICE is the immutable behavioral oracle. Historical lockstep receipts remain valid only for the exact models, checkpoints, cycle budgets, and fields they name. They do not establish whole-machine or release-wide parity.

- **C64:** Bounded x64sc lockstep and checkpoint suites exist, including historical 100,000-cycle and multi-frame receipts. Full classic VICE behavior and edge-case parity are being re-audited against active FR/TR/TEST acceptance criteria.
- **VIC-20:** Historical PAL and NTSC 10-second A/X/Y/S/P/PC receipts exist, plus scoped video, cartridge, flash, and audio tests. Full xvic parity is not yet accepted. Follow [PLAN-VICE-IMMUTABLE-001](docs/plans/PLAN-VICE-IMMUTABLE-001.md) and [PLAN-VSFLOCKSTEP-001](docs/plans/PLAN-VSFLOCKSTEP-001.md).
- **Snapshots:** The current host routes C64 snapshots through `vice-sharp.machine-snapshot.v2` and non-C64 snapshots through `vice-sharp.runtime-snapshot.v1`. Managed VSF staging already exists in the test harness, but normal public and desktop save/load paths do not yet use canonical native VICE `.vsf`. Proprietary formats are not a substitute for the required VICE format.
- **Native boundary:** The checked-in `native/vice` revision is the immutable reference. Remediation moves build, observation, stepping, compatibility, and snapshot integration into external wrappers. No parity result may depend on modifying native VICE.
- **VIC-20 settings:** The current Avalonia working tree binds VIC-20 memory and expansion sections to `IsVic20Selected`, and the ViewModel raises visibility changes when the selected profile changes or settings load.
- **Product scope:** Avalonia desktop and Console are supported product shells. Microsoft Store and Xbox/UWP are cancelled legacy source, not current targets.

## Verification evidence

Use exact receipts and acceptance tests, not percentage dashboards, to judge parity. Durable historical VIC-20 receipts are indexed in [docs/README.md](docs/README.md#lockstep-receipts-durable). Current remediation gates and their required zero-failed, zero-skipped evidence are defined in the two plans above.

The 2026-10-04 traceability audit found 235 canonical IDs, 149 referenced in source/tests, 86 canonical IDs without source/test references, and 1,011 noncanonical IDs in source/tests. This is an open remediation baseline, not a green gate.

## Machine roadmap

| Machine | Architecture | Status |
|---------|-------------|--------|
| C64 / C64C | MOS 6510 + VIC-II + SID + CIA x2 | Implemented surfaces; full x64sc parity pending |
| SX-64 | Same family as C64 with integrated drive/display differences | Bounded model coverage; full parity pending |
| VIC-20 | MOS 6502 + VIC-I + VIA x2 | Implemented surfaces and bounded 10-second receipts; full xvic parity pending |
| C128 | MOS 8502 + VIC-IIe + SID + CIA x2 + Z80 | Iteration 3 |
| PET | MOS 6502 + PIA/VIA + CRTC | Iteration 4 |
| Plus/4 / C16 | MOS 7501 + TED | Iteration 5 |

## Building

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.301 or later)

```bash
# Restore and build
dotnet build ViceSharp.slnx

# Run tests
dotnet test ViceSharp.slnx

# Using Nuke build system
./build.sh Compile    # Linux/macOS
build.cmd Compile     # Windows
```

### Nuke Targets

| Target | Description |
|--------|-------------|
| `Clean` | Remove bin/obj/artifacts |
| `Restore` | Restore NuGet packages |
| `Compile` | Build with TreatWarningsAsErrors |
| `Test` | Run unit tests with filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy` (excludes determinism, the on-demand aiUnit AI reviews, and the quarantined parity categories) |
| `DeterminismTest` | Run determinism verification tests |
| `RunConsole` | Run the console reference shell |
| `RunAvalonia` | Run the Avalonia desktop UI |
| `PublishWiki` | Legacy wiki target. Do not use it for requirements regeneration until it is migrated from direct REST to the supported MCP plugin `workflow.requirements.generateDocument` path. |
| `PublishMsi` | Publish the self-contained desktop app and package `artifacts/installer/ViceSharp.msi` |
| `InstallMsi` | Install the locally built MSI |
| `PublishWinget` | Generate winget package metadata for the MSI |
| `CiTest` | CI-ready variant of `Test`: restores and builds in-job, then stages hash-pinned ROMs via `EnsureCiRomRoot` when the agent has no VICE data root |
| `ParityTest` | Run the whole VICE-parity suite (`Category=Parity`), including quarantined `ParityPending` tests (remediation burn-down) |
| `PackNuget` | Pack the `ViceSharp.Core` bundle and the individual NuGet packages into `artifacts/packages`, verifying package contents |
| `PublishNuget` | Tag-gated release publish: pack from the tagged checkout and push to nuget.org; requires `NUGET_API_KEY` |

## Architecture

ViceSharp is designed as a **library-first emulator**:

- **ViceSharp.Abstractions** - 55 public interface declarations defining the emulator contract
- **ViceSharp.Core** - bus, clock, devices, mutation queue, pub/sub
- **ViceSharp.Chips** - CPU (6502/6510/8502), VIC-II, SID, CIA, VIA, PLA
- **ViceSharp.Architectures** - machine definitions: C64, VIC-20 (Iteration 2), and the C1541 true drive, plus ad-hoc and multisystem topologies (C128, PET, Plus/4 planned for iterations 3-5)
- **ViceSharp.SourceGen** - Roslyn source generator for device registration boilerplate
- **ViceSharp.Host** - composition boundary: emulator sessions, media, snapshots, diagnostics, and the gRPC host surface
- **ViceSharp.Protocol** - gRPC/protobuf contracts and generated client/server types
- **ViceSharp.Monitor** - machine-language monitor/debugger surface
- **ViceSharp.Launcher** - VICE-compatible argument parsing and binary-name topology dispatch (library, consumed by the Console shell)
- **ViceSharp.RomFetch** - ROM descriptors, load-time validation, and pinned download helpers
- **ViceSharp.Console** - command-line reference shell
- **ViceSharp.Library.ViewModels** - reusable library/catalog ViewModels shared by UI shells
- **ViceSharp.RomM** - RomM integration surface
- **ViceSharp.Avalonia** - Avalonia 12.x desktop UI
- **ViceSharp.Xbox**, **ViceSharp.Xbox.ViewModels**, **ViceSharp.Xbox.Input**, and **ViceSharp.Host.Xbox** - frozen legacy projects retained in the solution; Xbox/UWP is outside supported product scope

Key design principles:
- **Zero allocation hot path** - per-cycle emulation allocates nothing
- **POCO model** - all state is plain C# structs/records, no base classes
- **Mutation queue** - all state changes flow through an auditable queue
- **Deterministic** - bit-exact replay given identical inputs
- **Reflection-light hot path** - no runtime reflection in per-cycle emulation

See [docs/Architecture.md](docs/Architecture.md) for the full design.

## ROMs

ViceSharp does not include Commodore ROMs. See [docs/ROMs.md](docs/ROMs.md) for legal ROM options and setup instructions.

## License

Copyright (c) 2026 ViceSharp Contributors.

Licensed under the **GNU General Public License v2.0 or later** (GPL-2.0-or-later). See [COPYING](COPYING) for the full license text.

ViceSharp is a derivative work of VICE, which is also licensed under GPL-2.0-or-later. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for attribution details.

### Commodore C= logo (CC BY-SA 4.0)

Store and branding assets may include the **Commodore C= logo** from Wikimedia Commons:

- **File:** [Commodore C= logo.svg](https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg)
- **Author:** Alien426
- **License:** [Creative Commons Attribution-ShareAlike 4.0 International](https://creativecommons.org/licenses/by-sa/4.0/)

Local copies and ShareAlike notes live under `docs/xbox/store-screenshots/` (`Commodore_C_logo.svg`, `LOGO-ATTRIBUTION-CC-BY-SA-4.0.txt`).

## Contributing

1. Fork the GitHub repository (`github.com/sharpninja/vice-sharp`)
2. Follow the Byrd Development Process: tests first, then implementation
3. All tests must pass before submitting a PR
4. Optional: run the aiUnit AI Code Review / Project Review before a PR (see [docs/AI-Review.md](docs/AI-Review.md))
