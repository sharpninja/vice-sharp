# ViceSharp Iteration Roadmap

## Iteration 0: Foundations (historical milestone)

**Goal:** Runnable scaffolding with zero emulation.

**Status:** Historical foundation milestone. Its artifacts remain in the repository, but this documentation refresh did not rerun the original full acceptance gate and does not use the old completion label as proof of current parity.

- Solution structure and build system (Nuke)
- 33+ public interfaces in ViceSharp.Abstractions
- Roslyn source generator for device registration
- ROM fetch tool
- CI/CD target definitions (the historical Azure pipelines are retired; GitHub `origin` is the source of truth)
- Comprehensive documentation and GraphRAG knowledge base
- Determinism test harness (empty machine, bit-exact snapshots)

**Historical exit criteria:** `dotnet test` passes, the managed console app starts and exits cleanly, all docs are written. Current work must use the active zero-failed, zero-skipped gates rather than inherit this status.

## Iteration 1: C64 (MVP)

**Goal:** Playable Commodore 64 emulation with behavior derived from and proven against immutable VICE `x64sc`.

**Status (reopened 2026-10-04):** The repository contains broad C64 implementations and historical bounded lockstep evidence. Earlier “complete” labels and the v1.0.2 2594-passed/21-skipped baseline are not current acceptance evidence. Skipped tests are not passing tests, and named CPU/checkpoint windows do not prove whole-machine parity.

Historical evidence includes a 100,000-cycle BASIC READY CPU path, multi-frame no-cartridge model variants, and focused chip and device gates. Each receipt applies only to its named model, state fields, workload, and cycle budget.

Current acceptance work must:

- reconcile every active FR, TR, TEST, and acceptance criterion with classic VICE behavior;
- restore and prove the immutable `native/vice` boundary;
- close CPU, bus, VIC-II, CIA, SID, IEC, drive, tape, cartridge, input, timing, and host gaps with source-pinned tests;
- route supported C64 snapshots through native VICE `.vsf`, including complete state transfer and deterministic resume;
- finish with zero failed, zero skipped, and no aborted prerequisites.

**Exit criteria:** Pending. Commercial software, Klaus Dormann coverage, audio, media, and deterministic save/load must be proven by the current acceptance suite against unmodified VICE, not inferred from historical percentage dashboards.

## Iteration 2: VIC-20

**Goal:** Add VIC-20 as a second architecture with behavior measured against immutable native `xvic`.

**Status (reopened 2026-10-04):** Historical receipts prove bounded PAL and NTSC CPU-register lockstep plus named video, cartridge, flash, persistence, and audio slices. They do not prove complete xvic parity. The current acceptance state remains pending under [PLAN-VICE-IMMUTABLE-001](plans/PLAN-VICE-IMMUTABLE-001.md) and [PLAN-VSFLOCKSTEP-001](plans/PLAN-VSFLOCKSTEP-001.md).

Implemented surfaces include the MOS 6502-based host, VIC-I, shared VIA implementation with machine-specific wiring, color RAM and open-bus behavior, RAM expansion selection, 1540 defaults, VIC-20 cartridge families, desktop settings, and scoped present/audio comparisons. The current Avalonia settings view hides VIC-20 memory and expansion controls unless a VIC-20 profile is selected.

Historical bounded evidence:

- PAL 10-second A/X/Y/S/P/PC receipt: 11,084,050 cycles.
- NTSC 10-second A/X/Y/S/P/PC receipt: 10,227,270 cycles.
- Scoped READY and busy-frame capture comparisons, timed FE3 erase behavior, cartridge write-back, and deterministic batched VIC-I sound tests.
- Receipt index: [docs/README.md](README.md#lockstep-receipts-durable).

Open acceptance gates include immutable-oracle cleanup, requirement and acceptance-criteria reconciliation, complete xvic state checkpoints, canonical VICE `.vsf` save/load, deterministic resume, launcher/runtime completion, input end-to-end coverage, and a zero-failed, zero-skipped final suite.

**Exit criteria:** Pending. The iteration is complete only when the current requirements matrix and approved plans pass against unmodified native xvic with zero failed and zero skipped tests.

## Iteration 3: C128

**Goal:** Add Commodore 128 with dual-CPU support.

- MOS 8502 CPU (2 MHz mode)
- Z80 coprocessor (CP/M mode)
- VIC-IIe (6569 superset with 2 MHz support)
- VDC (8563) - 80-column display
- MMU - extended banking, shared RAM
- 1571/1581 drive support
- C128/C64 mode switching

**Exit criteria:** Boots in C128 mode, switches to C64 mode, CP/M mode starts.

## Iteration 4: PET

**Goal:** Add Commodore PET family.

- MOS 6502 CPU
- PIA (6520) and VIA (6522) I/O
- CRTC (6545) video controller
- IEEE-488 bus
- PET keyboard (business/graphics)
- PET 2001, 3032, 4032, 8032 variants

## Iteration 5: Plus/4 and C16

**Goal:** Add TED-based machines.

- MOS 7501/8501 CPU
- TED (7360/8360) - combined video+audio+timer
- Plus/4 built-in software ROM
- C16 reduced memory model

## Future Iterations

- **Performance:** SIMD rendering, JIT CPU core, GPU-accelerated video
- **Networking:** RS-232 emulation, TCP/IP stack (RR-Net/TFE)
- **Peripherals:** REU, GeoRAM, IDE64, SD2IEC
- **Platform:** WebAssembly target, mobile (MAUI)
- **Community:** Plugin system, Lua scripting, ROM repository integration
