# ViceSharp

A C# port of [VICE](https://vice-emu.sourceforge.io/) (Versatile Commodore Emulator) targeting .NET 10 with NativeAOT support.

## Status

✅ **Iteration 0 (Foundations)** — Complete. All core primitives implemented, lock-free and zero allocation.
⏳ **Iteration 1 (C64 Bringup)** — In progress with the current boot and lockstep validation baseline green:
  - BASIC `READY.` boot proof is covered by the test harness
  - VICE-backed lockstep validation reaches the 100,000-cycle regression gate
  - `dotnet test .\ViceSharp.slnx --nologo` currently passes `664/664`

Working chip layer implementations:
  - `Mos6510` CPU (opcodes + core)
  - `Mos6569` VIC-II
  - `Mos6526` CIA
  - `Mos6581` SID (noise LFSR + voice 3 OSC3/ENV3 readback)
  - Folders for Cpu/Cia/Sid/VicIi

Bounded runtime validation slices are implemented for 1541/D64 attach+sector reads, TAP datasette pulse reads, standard 8K/16K cartridge mapping, runtime snapshot save/load, and BMP frame capture. Full subsystem parity for advanced drive, tape, cartridge, snapshot, and media workflows remains future scope.

## Completion Dashboard

Snapshot of VICE-to-ViceSharp parity sourced from MCP TODO state and the iteration roadmap. Last refreshed `2026-05-16`.

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
| MOS 6569 VIC-II raster core + IRQ + sprites (first-scanline parity) | 🟢 | 70% | `BACKFILL-VIDEO-001` |
| MOS 6526 CIA1/CIA2 (timers + TOD + ports) | ✅ | 100% | full solution test |
| MOS 6581 SID (3-voice + ADSR + filter + noise LFSR + OSC3/ENV3) | 🟢 | 55% | `BACKFILL-SID-001` |
| PLA + Memory map ($0000-$FFFF) | ✅ | 100% | boot proof |
| Reset sequencing (7-cycle + port init) | ✅ | 100% | reset tests |
| ROM loader (KERNAL/BASIC/CHARGEN + SHA1) | ✅ | 100% | `BasicBootProofTests` |
| 1541 / IEC / D64 (attach + deterministic sector reads) | 🟡 | 30% | `RUNTIME-1541-001` done · `RUNTIME-1541-002` open |
| Datasette / TAP (pulse reads, motor gating bounded) | 🟡 | 25% | `RUNTIME-TAPE-001` done · `RUNTIME-TAPE-002` open |
| Standard cartridge mapping (8K/16K raw, not yet wired to memory map) | 🟡 | 25% | `RUNTIME-CART-001` done · `RUNTIME-CART-002` open |
| Runtime snapshot (64K + public CPU state) | 🟡 | 30% | `RUNTIME-SNAPSHOT-001` done · `RUNTIME-SNAPSHOT-002` open |
| Frame capture (BGRA → BMP artifact) | 🟡 | 25% | `RUNTIME-CAPTURE-001` done · `RUNTIME-CAPTURE-002` open |
| Keyboard matrix + control-port parity (12 variants, held keyboard) | 🟢 | 60% | `BACKFILL-INPUT-001` |
| Host UI + Monitor control surface (gRPC, typed RPCs, disassembly trace) | 🟢 | 60% | `BACKFILL-HOSTUI-001` |
| x64sc variant lockstep gate (293/293 variants, raster checkpoints) | 🟢 | 50% | `BACKFILL-LOCKSTEP-001` |
| Upstream VICE testbench integration | ⚪ | 0% | `ARCH-TESTBENCH-001` |

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
| XMLDOCS test contract (cite FR/TR, use case, acceptance) | ⚪ | 0% | `QA-XMLDOCS-001` |
| BenchmarkDotNet harness vs native VICE | ⚪ | 0% | `PERF-BENCHMARK-001` |
| Repository maintenance + github wiki | ⚪ | 0% | `REPO-MAINT-001` |
| Ad-hoc machine YAML schema + Console loader + Avalonia 12 helper | ⚪ | 0% | `ARCH-ADHOCMACHINE-001` |
| Cross-platform hosts (UWP Xbox + Avalonia 12 mobile + MacOS) | ⚪ | 0% | `PLATFORM-CROSS-001` |
| Completion Dashboard (this section) | 🟢 | 50% | `DOC-DASHBOARD-001` |

Dashboard is regenerated as subagent slices land. Source-of-truth IDs: see `http://PAYTON-LEGION2:7147/mcpserver/todo?done=false` for live MCP TODO state.

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
