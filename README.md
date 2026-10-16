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

Coming from classic VICE? The `ViceSharp.Launcher` library provides VICE-compatible argument parsing and topology generation for `x64`, `x64sc`, `xvic`, and `c1541`, including `-8`, `-9`, `-cart`, `+truedrive` / `-truedrive`. `ViceSharp.Console` still parses its process arguments as `x64sc`; use `--machine-yaml` for drive topologies because parsed disk/cartridge dispatch is not yet wired into the console. Standalone VICE-named binaries are not shipped. The generated `xvic` topology is not yet an end-to-end console path because the generic multi-system kind dispatcher recognizes only `C64` and `C1541`. See [docs/USER-GUIDE.md](docs/USER-GUIDE.md) and [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md).

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

### Linux: install for your user

Run Nuke `DeployUser` as your regular user on Linux with a .NET SDK compatible with [global.json](global.json) (currently 10.0.301 or a later 10.0 feature band). It publishes a self-contained Release build for `x86_64` or `aarch64` and installs the app, launcher, desktop entry, and icon inside your home directory.

```bash
./build.sh DeployUser
~/.local/bin/vicesharp
```

The PowerShell equivalent is `./build.ps1 DeployUser`. `DeployYay` is a compatibility alias for the same user-local deployment; it does not invoke `yay`, `pacman`, or `sudo`.

Application releases live under `~/.local/share/vicesharp/releases/<version>-<unique>/`; the `current` symlink selects the active release. The launcher is `~/.local/bin/vicesharp`, and the desktop entry is `~/.local/share/applications/vicesharp.desktop`. If `XDG_DATA_HOME` is set, it replaces `~/.local/share` and must be an absolute path inside your home. Installation rejects root execution and symlinked or unmanaged destination conflicts. Older releases and existing `ViceSharp` user data are preserved.

An existing system package remains installed. Put `~/.local/bin` before `/usr/bin` in your shell's `PATH` to select the user deployment with `vicesharp`; check with `command -v vicesharp`, or invoke `~/.local/bin/vicesharp` directly. The desktop entry selects the absolute user launcher independently of shell `PATH`.

The app includes C64 GTK3 and SDL keyboard layouts under `data/C64` in its release directory. Input settings discover these `.vkm` files automatically, after configured VICE data directories and per-user keymaps. No system .NET SDK or runtime is required to run the installed app.

Saving RomM credentials on Linux requires the optional `libsecret` package (which provides `secret-tool`) and a running, unlocked Secret Service keyring, such as GNOME Keyring or KWallet with Secret Service support enabled.

Linux audio uses the SDL2 shared library (`libSDL2-2.0.so.0`, also provided by Arch's `sdl2-compat`) and the current user's audio service/device. `DeployUser` does not install system dependencies. The desktop enables sound by default; `VICESHARP_AUDIO=0` disables it. Headless and test hosts remain silent unless explicitly enabled. The C64 BASIC READY screen is normally silent; use a program that produces sound to check playback.

On first local desktop launch, missing default C64 ROMs are downloaded from the VICE GitHub mirror, hash-verified, and cached under `~/.local/share/ViceSharp/roms` (or `$XDG_DATA_HOME/ViceSharp/roms`). Set `VICESHARP_ROM_PATH` or `VICE_DATA_PATH` to prefer an existing ROM set; see [docs/ROMs.md](docs/ROMs.md). ROMs are not bundled. See the [RomM library guide](docs/USER-GUIDE.md#6c-romm-library) for connecting a server and attaching downloaded games, including T64 archives.

### Optional Arch package build

`./build.sh PackYay` (or `./build.ps1 PackYay`) remains a package-only target for explicit system-package workflows. It requires Arch Linux `base-devel`/`makepkg` and the native dependencies listed in the generated `PKGBUILD`; it does not install anything. Each build leaves `PKGBUILD`, `.SRCINFO`, a SHA256-pinned payload, and `vicesharp-<version>-1-<arch>.pkg.tar.zst` under `artifacts/yay/<version>/<arch>/`. Installing that archive is a separate system-administration action. Use `DeployUser` for user-local installation.

Both targets accept `--package-version-override X.Y.Z`; when omitted, they use the release tag on HEAD or the GitVersion-derived default.

## User documentation

- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) - install, first run, CLI launcher, YAML topology, disk images, RomM library, desktop drag-drop (including `*.prg` load/RUN), capture, diagnostics attach, what works today
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

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) compatible with [global.json](global.json), currently 10.0.301 or a later 10.0 feature band.

```bash
# Restore and build
dotnet build ViceSharp.slnx

# Run the filtered Nuke test gate
./build.sh Test --configuration Release

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
| `Test` | Run tests with filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy&Category!=Integration` (separate determinism, paid AI, quarantined parity, and external integration gates) |
| `DeterminismTest` | Run determinism verification tests |
| `RunConsole` | Run the console reference shell |
| `RunAvalonia` | Run the Avalonia desktop UI |
| `DeployUser` | Publish Release and install the Linux desktop app in the current user's home |
| `DeployYay` | Compatibility alias for `DeployUser` |
| `PackYay` | Build an Arch package without installing it |
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

ViceSharp packages do not include Commodore ROMs. Local desktop startup downloads and verifies the default C64 ROMs when no complete local set is available, then reuses its per-user cache offline. Other variants and drive ROMs require separate resources. See [docs/ROMs.md](docs/ROMs.md) for ROM sources, cache locations, and overrides.

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
