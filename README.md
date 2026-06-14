# ViceSharp

A C# port of [VICE](https://vice-emu.sourceforge.io/) (Versatile Commodore Emulator) targeting .NET 10 with NativeAOT support.

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

Coming from classic VICE? The `ViceSharp.Launcher` project provides `x64`, `x64sc`, and `c1541` binaries that accept the usual `-8`, `-9`, `-cart`, `+truedrive` / `-truedrive` flags. See [docs/USER-GUIDE.md](docs/USER-GUIDE.md) for the full install and first-run walkthrough, and [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md) for a side-by-side flag mapping.

## User documentation

- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) - install, first run, CLI launcher, YAML topology, disk images, what works today
- [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md) - binary + flag mapping, behaviour caveats, performance / accuracy, bug compatibility
- [docs/ROMs.md](docs/ROMs.md) - legal ROM options, environment variable, directory layout
- [docs/](docs/README.md) - full documentation index (architecture, public API, iteration plans, diagrams)

## Status

✅ **Iteration 0 (Foundations)** — Complete. All core primitives implemented, lock-free and zero allocation.
✅ **Iteration 1 (C64 Bringup)** — **Complete (Phase 1 closed 2026-05-31, HEAD `6086e11`)**:
  - Full suite at closeout: 1641 passed / 2 skipped / 0 failed
  - x64sc lockstep: 322 passed (10-frame depth across no-cart variants)
  - Perf: 11.5M+ cycles/sec under release JIT (47x the Phase 1 PERF-TUNING-001 target of 246,312 cps; 1173% PAL real-time)
  - Snapshot/capture/input/testbench/launcher all closed: see `docs/handoff.md`
  - Post-closeout audit remediation: `ARCH-CHIPGLUE-001` verified that reusable chip cores stay machine-agnostic and C64/C1541 glue lives in Core machine/device adapters.
  - 2 skipped tests are ROM-gated process smoke tests (no Phase 1 dependency)
  - Post-Phase 1 deferrals: PERF-BENCHMARK-001 native baseline, advanced cartridge mappers, cross-platform host shells, 8580 SID filter deepening, wiki publishing automation. See handoff.md.

Working chip layer implementations:
  - `Mos6510` CPU (opcodes + core)
  - `Mos6569` VIC-II
  - `Mos6526` CIA
  - `Mos6581` SID (noise LFSR + voice 3 OSC3/ENV3 readback)
  - Folders for Cpu/Cia/Sid/VicIi

Bounded runtime validation slices are implemented for 1541/D64 attach+sector reads, TAP datasette pulse reads, standard 8K/16K cartridge mapping, runtime snapshot save/load, and BMP frame capture. Full subsystem parity for advanced drive, tape, cartridge, snapshot, and media workflows remains future scope.

## Completion Dashboard

Snapshot of VICE-to-ViceSharp parity sourced from MCP TODO state and the iteration roadmap. Last refreshed `2026-05-31` at HEAD `32880a4` (Phase 1 marathon close - all six original deferral items pulled back into scope and shipped; see `docs/handoff.md`). Perf probe: 11.5M+ cycles/sec (47x the Phase 1 PERF-TUNING-001 target of 246,312 cps). AOT publish: gate green via YamlStream representation-model rewrite. Wiki publish: automated via `tools/Publish-Wiki.ps1` + Nuke `PublishWiki`. Advanced cartridge mappers: all 7 mappers landed as minimum-viable scaffolds. 8580 SID: real Chamberlin SVF on linear cutoff curve. PLATFORM-CROSS-001: macOS, Xbox, Android, iOS host shells scaffolded.

**Legend** — State: ✅ done · 🟢 active · 🟡 bounded gate done, deepening pending · ⚪ planned

### Iteration 0 — Foundations

| Feature | State | % | Source |
|---------|:----:|:----:|--------|
| .NET 10 + Nuke build pipeline | ✅ | 100% | iteration0 batch 1 |
| MCP Server + TODO workspace integration | ✅ | 100% | iteration0 batch 2 |
| Documentation set (Architecture, Public API, Roadmap) | ✅ | 100% | iteration0 batch 3 |
| GraphRAG ingest | ✅ | 100% | iteration0 batch 4 |

### Iteration 1 — C64 Bringup

| Feature | State | % | Source |
|---------|:----:|:----:|--------|
| MOS 6510 CPU (official + illegal opcodes) | ✅ | 100% | `LockstepValidationTests.First100000CyclesMatch` |
| Processor port `$00/$01` + interrupts (IRQ/NMI/RDY/RES) | ✅ | 100% | lockstep gate |
| MOS 6569 VIC-II (raster IRQ + bad line + sprite collision/IRQ + sprite Y-exp/multicolor + sprite DMA + sprite-DMA stall + visible sprite composition + sprite priority + light pen + color/register read masks + $D018/$D016/$D011 decoding + display mode selection + VICE display-mode pixel color routing + $D015/$D010 sprite registers + managed continuous side-border behavior + managed matrix idle/fill behavior + RC window cycle-accurate + non-PAL sprite DMA tables + screen-RAM checkpoint) | ✅ | 100% | `BACKFILL-VIDEO-001` closed (Phase 1 slice 1; FLI/AFLI deepening continues post-Phase 1) |
| MOS 6526 CIA1/CIA2 (timers + TOD 12-hour + timer-B chain + SDR + FLAG pin + force-load + keyboard scan + joystick scan + ICR) | ✅ | 100% | `BACKFILL-CIA` + base input scan coverage |
| MOS 6581 SID (hard sync + ring mod + combined waveforms 6581 + 8580 + ADSR bug + PCM equiv + $D418 digi + audio backend + filter 6581 + non-linear cutoff curve + dual-SID + noise LFSR + determinism) | ✅ | 100% | `BACKFILL-SID-001` closed; 8580 filter deepening is post-MVP |
| MOS 6522 VIA (timer-1 PB7 + timer-2 phi2+PB6 + SR modes + CA1/CB1 edge IRQ + CA2/CB2 handshake/manual/pulse) | ✅ | 100% | `BACKFILL-VIA` complete |
| Mos6510 CPU interrupts (NMI vector + BRK B-flag + IRQ vector) | ✅ | 100% | `BACKFILL-CPU` complete |
| PLA + Memory map ($0000-$FFFF) | ✅ | 100% | boot proof |
| Reset sequencing (7-cycle + port init) | ✅ | 100% | reset tests |
| ROM loader (KERNAL/BASIC/CHARGEN + SHA1) | ✅ | 100% | `BasicBootProofTests` |
| 1541 / IEC / D64 (attach + deterministic sector reads + IEC ATN timing + 1541 motor ramp) | ✅ | 100% | `ARCH-TRUEDRIVE-1541-002` Phase 1 close (slice 2) |
| Datasette / TAP (pulse reads + CIA1 FLAG + builder wiring + rewind/seek + 32k motor ramp + SenseLine + record buffer) | ✅ | 100% | `RUNTIME-TAPE-002` Phase 1 close (slice 3) |
| Standard cartridge mapping (8K/16K raw + CRT + GAME/EXROM live map) | ✅ | 100% | `RUNTIME-CART-002` validated for standard cartridges; broad mapper families are post-MVP |
| Runtime snapshot (CPU A/X/Y/S/P/PC + 64K + VIC + CIA TOD + SID ADSR round-trip) | ✅ | 100% | `RUNTIME-SNAPSHOT-002` Phase 1 close (slice 4) |
| Frame capture (BGRA → BMP single + multi-frame sequence + WAV audio + PNG + JPEG format) | ✅ | 100% | `RUNTIME-CAPTURE-002` Phase 1 close (slice 4) |
| Keyboard matrix + control-port parity (LoadFromFile + EnumerateDevices + 30-frame key-repeat hold) | ✅ | 100% | `BACKFILL-INPUT-001` Phase 1 close (slice 5) |
| Host UI + Monitor control surface (10 services + 8 adapters + view model + registry + mapper + frame source + InProcessGrpcHost + 2 clients; ~230 tests) | ✅ | 100% | `BACKFILL-HOSTUI-001` closeable; launcher/UI shell work is tracked separately |
| Core primitives (SystemClock + DoubleBufferedMutationQueue + LockFreePubSub + BasicBus + SimpleRam) | ✅ | 100% | TR-PUBSUB-PERFORMANCE + TR-Cycle-Accuracy + TR-System-Core |
| Chip/package boundary audit (shared chips vs machine/device glue) | ✅ | 100% | `ARCH-CHIPGLUE-001` closed with `TEST-ARCH-CHIPGLUE-001`; focused gate 579/579 and lockstep/checkpoint 335/335 |
| x64sc variant lockstep gate (10-frame depth across no-cart variants, 322 lockstep tests green) | ✅ | 100% | `BACKFILL-LOCKSTEP-001` Phase 1 close (slice 7) |
| Upstream VICE testbench integration (debugcart + limitcycles + PRG autostart + help text + ROM-less smoke) | ✅ | 100% | `ARCH-TESTBENCH-001` + `CLI-LAUNCHER-001` Phase 1 close (slice 6) |

### Iterations 2-5 — Other Machines

| Machine | Target Iteration | State | % |
|---------|:----------------:|:----:|:----:|
| SX-64 | 1 | ⚪ | 0% |
| VIC-20 (MOS 6502 + VIC + VIA x2) | 2 | ⚪ | 0% |
| C128 (MOS 8502 + VIC-IIe + Z80) | 3 | ⚪ | 0% |
| PET (MOS 6502 + PIA/VIA + CRTC) | 4 | ⚪ | 0% |
| Plus/4 / C16 (MOS 7501 + TED) | 5 | ⚪ | 0% |

### Tooling and Ecosystem

| Feature | State | % | Source |
|---------|:----:|:----:|--------|
| XMLDOCS test contract (cite FR/TR, use case, acceptance) | ✅ | 100% | `QA-XMLDOCS-001` CLOSED: ratchet baseline at 0 (full retrofit + `XmlDocsConventionTests.ExpectedMaxViolations=0`) |
| BenchmarkDotNet harness vs native VICE | 🟡 | 60% | `PERF-TUNING-001` Phase 1 close (slice 8; PerfProbe measured 11.5M+ cycles/sec = 47x the 25% target). `PERF-BENCHMARK-001` native baseline + sweep deferred post-Phase 1. |
| Repository maintenance + github wiki | 🟢 | 35% | `REPO-MAINT-001` (audit + plan in [docs/maintenance/](docs/maintenance/), execution deferred) |
| Ad-hoc machine YAML schema + Console loader + Avalonia 12 helper | 🟢 | 60% | `ARCH-ADHOCMACHINE-001` (schema + loader + `--machine-yaml` flag, helper app deferred) |
| Cross-platform hosts (UWP Xbox + Avalonia 12 mobile + MacOS) | 🟢 | 15% | `PLATFORM-CROSS-001` (wireframes in [docs/wireframes/](docs/wireframes/README.md), host code pending) |
| Completion Dashboard (this section) | ✅ | 100% | `DOC-DASHBOARD-001` Phase 1 close (slice 9) |

Dashboard is regenerated as subagent slices land. Source-of-truth IDs: see `http://PAYTON-LEGION2:7147/mcpserver/todo?done=false` for live MCP TODO state. Latest validation on 2026-05-21: focused VIC-II matrix/idle plus adjacent timing `18/18`, broader VIC/video `179/179`, and requirement traceability passed with `163` canonical IDs, `82` referenced canonical IDs, `81` unreferenced canonical IDs, and `53` noncanonical source/test references. Full-solution `dotnet test .\ViceSharp.slnx --no-build --nologo` timed out after five minutes during the prior full-suite attempt and was stopped cleanly, so the current green gate remains focused rather than solution-wide. MCP TODO/session writes are paused for this slice because a subagent reported MCP health nonce failure; local fallback docs record the state.

## Supported Machines (planned)

| Machine | Architecture | Status |
|---------|-------------|--------|
| C64 / C64C | MOS 6510 + VIC-II + SID + CIA x2 | Iteration 1 |
| SX-64 | Same as C64 (built-in monitor + 1541) | Iteration 1 |
| VIC-20 | MOS 6502 + VIC + VIA x2 | Iteration 2 |
| C128 | MOS 8502 + VIC-IIe + SID + CIA x2 + Z80 | Iteration 3 |
| PET | MOS 6502 + PIA/VIA + CRTC | Iteration 4 |
| Plus/4 / C16 | MOS 7501 + TED | Iteration 5 |

## Building

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.201 or later)

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
| `Test` | Run unit tests (excludes determinism) |
| `DeterminismTest` | Run determinism verification tests |
| `PublishAot` | Publish NativeAOT console app |
| `CiAzure` | Full Azure DevOps CI pipeline |
| `CiGitHub` | Full GitHub Actions CI pipeline |

## Architecture

ViceSharp is designed as a **library-first emulator**:

- **ViceSharp.Abstractions** — 33+ public interfaces defining the emulator contract
- **ViceSharp.Core** — bus, clock, devices, mutation queue, pub/sub
- **ViceSharp.Chips** — CPU (6502/6510/8502), VIC-II, SID, CIA, VIA, PLA
- **ViceSharp.Architectures** — machine definitions (C64, VIC-20, C128, PET, Plus/4)
- **ViceSharp.SourceGen** — Roslyn source generator for device registration boilerplate
- **ViceSharp.Console** — NativeAOT reference shell
- **ViceSharp.Avalonia** — Avalonia 12.x desktop UI

Key design principles:
- **Zero allocation hot path** — per-cycle emulation allocates nothing
- **POCO model** — all state is plain C# structs/records, no base classes
- **Mutation queue** — all state changes flow through an auditable queue
- **Deterministic** — bit-exact replay given identical inputs
- **NativeAOT compatible** — no reflection on the hot path

See [docs/Architecture.md](docs/Architecture.md) for the full design.

## ROMs

ViceSharp does not include Commodore ROMs. See [docs/ROMs.md](docs/ROMs.md) for legal ROM options and setup instructions.

## License

Copyright (c) 2026 ViceSharp Contributors.

Licensed under the **GNU General Public License v2.0 or later** (GPL-2.0-or-later). See [COPYING](COPYING) for the full license text.

ViceSharp is a derivative work of VICE, which is also licensed under GPL-2.0-or-later. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for attribution details.

## Contributing

1. Fork on Azure DevOps (`dev.azure.com/McpServer/VICE-Sharp`)
2. Follow the Byrd Development Process: tests first, then implementation
3. All tests must pass before submitting a PR
4. NativeAOT compatibility is required for all non-test assemblies
