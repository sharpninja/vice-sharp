# Switching from Classic VICE to ViceSharp

A side-by-side reference for users who already drive classic VICE (`x64sc`, `xvic`, `c1541`, etc.) and want to evaluate ViceSharp against the same behavior. Native VICE is the immutable reference; ViceSharp is not yet a complete drop-in replacement. For install and first run, start at [USER-GUIDE.md](USER-GUIDE.md).

## 1. Binary mapping

| Classic VICE | ViceSharp launcher | Status |
|--------------|--------------------|--------|
| `x64` | `x64` (binary-name dispatch) | Supported by the launcher library and maps to the same managed C64 topology as `x64sc`; ViceSharp does not maintain a separate fast variant. No `x64.exe` ships yet. Full x64sc parity remains pending. |
| `x64sc` | `x64sc` (console shell) | `ViceSharp.Console` parses its arguments as binary name `x64sc`. It shares the managed C64 path with `x64`; no `x64sc.exe` ships yet, and historical bounded lockstep is not whole-machine parity proof. |
| `c1541` | `c1541` (binary-name dispatch) | Supported as a standalone 1541 disk-tool topology (single drive, optional D64 mount) in the launcher library. No `c1541.exe` ships yet. |
| `x128` | `x128.exe` | Not yet. Throws `NotSupportedException`. C128 is iteration 3. |
| `xvic` | `xvic` (library topology generation) | Partial. `ViceTopologyBuilder` recognizes `xvic` and emits a `Vic20` topology, but no `xvic.exe` ships, `ViceSharp.Console` hardcodes `x64sc`, and the generic multi-system kind dispatcher does not yet recognize `Vic20`. |
| `xpet` | `xpet.exe` | Not yet. PET is iteration 4. |
| `xplus4` | `xplus4.exe` | Not yet. Plus/4 / C16 is iteration 5. |
| `xcbm2` / `xcbm5x0` | same names | Not yet. CBM-II is post-MVP. |
| `vsid` | `vsid.exe` | Not yet. SID-only player is post-MVP. |
| `petcat` | `petcat.exe` | Not yet. BASIC tokeniser is post-MVP. |
| `cartconv` | `cartconv.exe` | Not yet. Cart converter is post-MVP. |

`ViceSharp.Launcher` is a class library: no VICE-named executables are built today. Binary-name dispatch is implemented in [ViceTopologyBuilder.cs](../src/ViceSharp.Launcher/ViceTopologyBuilder.cs). `ViceSharp.Console` hardcodes the binary name `x64sc`, so its process path does not expose `xvic` selection. Unsupported names throw; `xvic` is recognized by the topology builder but remains incomplete at the generic multi-system dispatcher.

## 2. Flag mapping

The launcher's flag parser is [ViceArgsParser.cs](../src/ViceSharp.Launcher/ViceArgsParser.cs). The table below is exhaustive for the parser; unlisted classic VICE flags are collected into `ViceArgs.Unknown` and currently have no effect. That describes ViceSharp behavior and does not assert that every classic VICE binary handles unknown options the same way.

### Supported

| Classic VICE flag | ViceSharp launcher | Notes |
|-------------------|--------------------|-------|
| `-8 <path>` | `-8 <path>` | Attach D64 to drive 8. |
| `-9 <path>` | `-9 <path>` | Attach D64 to drive 9. |
| `-cart <path>` | `-cart <path>` | Attach standard 8K / 16K raw or CRT cartridge image. Live memory-map wiring is implemented for standard cartridges; see section 3 for remaining mapper limits. |
| `+truedrive` | `+truedrive` | Enable true-drive emulation; drive YAML peripheral gets `fidelity: TrueDevice`. |
| `-truedrive` | `-truedrive` | Disable true-drive emulation. |
| `-config <path>` (closest analogue) | `--machine-yaml <path>` / `-m <path>` | Explicit machine topology YAML. ViceSharp uses YAML topologies instead of a flat `vicerc`. |
| (n/a in classic) | `--cycles <N>` | Host-cycle budget. Classic VICE runs until you quit; ViceSharp's console host needs a budget for deterministic batch runs. |
| `-autostart <path>` | `-autostart <path>` | Autostart a PRG. Parsed and consumed by the console entry point (FR-CFG-005 AC8). |
| `x64sc ... testcase.prg` (positional) | `program.prg` (positional) | Any bare `*.prg` argument is treated as an autostart PRG, matching the classic testbench invocation style. |
| `-debugcart` | `-debugcart` / `+debugcart` | Debug cartridge ($D7FF exit signaling for regression harnesses, per VICE `debugcart.c`). `-debugcart` enables, `+debugcart` disables; consumed by the console entry point. |
| `-limitcycles <N>` | `--limitcycles <N>` / `-limitcycles <N>` | Bounded execution cycle limit (testbench); overrides the run's cycle budget in the console entry point. |
| `-verbose` / `-v` | `-v` / `--verbose` | Same intent. |
| `-help` / `-?` | `--help` / `-h` / `-?` | Same intent. |

### Partial / bounded

| Classic VICE flag | ViceSharp behaviour |
|-------------------|---------------------|
| `-cart <path>` | Standard raw/CRT images load, normalise to 8K / 16K ROML+ROMH banks, and drive the C64 memory map through `GAME` / `EXROM`. Broader mapper families and cart-converter workflows are post-MVP. |
| `-autostart <path>` | Console: PRG autostart is parsed and dispatched by the console entry point; disk-image autostart (`LOAD"*",8,1: RUN` injection from a D64) is not wired, so for disks use `-8 <disk.d64>` plus a topology where the drive image is mounted at boot. Avalonia desktop: drop a `*.prg` on the video surface to load it into the current session (RUN when the load address equals live TXTTAB); drop a disk image to attach drive 8 and autostart. |

### Not yet (collected as unknown)

Each of these is currently in the launcher's `Unknown` bucket; the run still proceeds but the flag has no effect. They are all candidate work items for the launcher.

| Classic VICE flag | Status |
|-------------------|--------|
| `-warp` | Launcher flag not yet wired. The capability exists: the Avalonia desktop UI has a Warp toggle (Alt+W), a speed-cycle button, and a live limiter slider; warp runs uncapped and discards live sound, same semantics as VICE `-warp`. |
| `-sound` / `-soundoutput` | Launcher flag not yet wired. A default WinMM audio backend is connected on Windows desktop (the Avalonia app enables it at startup; `VICESHARP_AUDIO=0` disables it); library consumers pass an `IAudioBackend` to `Sid6581(IBus, IAudioBackend?)`. |
| `-fullscreen` | Not yet. The Avalonia/host-control core exists, but the launcher path does not start an always-on display shell. |
| `-model <name>` (`c64c`, `c64pal`, etc.) | Launcher flag not yet wired. The model profiles exist: `C64MachineProfiles` defines C64, C64C, SX-64, C64GS, and Japanese boards in PAL and NTSC variants, exercised by the 322-case `X64ScVariantLockstepTests` gate. Only the flag wiring is missing. |
| `-ntsc` / `-pal` | Launcher flag not yet wired. NTSC and PAL profiles exist; see `-model`. |
| `-tape <path>` | Not yet via the launcher. TAP support exists at the device layer, but launcher attach plus spin-up/record timing remain under `RUNTIME-TAPE-002`. |
| `-monitor` (built-in machine-language monitor) | Not yet via the launcher. The gRPC monitor/control surface is built under `BACKFILL-HOSTUI-001`; wiring this flag belongs with `CLI-LAUNCHER-001`. |
| `-keymap`, `-joydev`, `-userportdevice`, `-cartrev`, etc. | Not yet. |

If a flag you depend on is in this list, please file an issue (see [USER-GUIDE.md, Where to file regressions](USER-GUIDE.md#where-to-file-regressions)) so it gets prioritised against real demand.

## 3. Behaviour caveats

### Topology is YAML, not a flat config

Classic VICE puts everything in one command line (or one `vicerc`). ViceSharp prefers an explicit YAML topology because the substrate is multi-system from the start: each drive, cartridge-CPU, or user-port-CPU is its own clocked machine on a shared `IInterSystemBus`. The launcher synthesises a YAML on the fly for the common `-8` / `-9` / `+truedrive` cases, but for anything beyond that, write the YAML directly.

The canonical sample is [docs/samples/c64-plus-1541.multisystem.yaml](samples/c64-plus-1541.multisystem.yaml); see [USER-GUIDE.md section 5](USER-GUIDE.md#5-machine-yaml-topologies) for the schema.

Recommendation:
- VICE muscle memory: the `x64sc -8 disk.d64 +truedrive` form is what the launcher library parses and dispatches; until named binaries ship, drive topologies go through `--machine-yaml` on the console shell.
- Multi-drive / multi-machine: write a YAML and pass `--machine-yaml`.

### No always-on UI / GUI host

The launcher invokes the console host. It does not start an Avalonia screen, built-in monitor window, or default sound output. The host UI/control core exists behind gRPC and the in-process Avalonia host boundary, but the launcher remains a batch-mode emulator until `CLI-LAUNCHER-001` wires those surfaces into process-level flags. `--cycles N` is the way you bound a run.

### True-drive emulation defaults

In ViceSharp, the canonical sample sets `fidelity: TrueDevice` explicitly. If you omit `fidelity:`, you get `Buffered` (sector-stream fast path), which is much faster but never runs the drive's 6502. Classic VICE's "TrueDrive 8" resource maps to `+truedrive` on the launcher, which in turn sets `fidelity: TrueDevice` on every drive peripheral the launcher emits.

### Snapshot format is native VICE VSF

VICE `.vsf` is the canonical required snapshot format for supported x64sc C64 and xvic VIC-20 paths. The current product host does not yet meet that contract: it emits `vice-sharp.machine-snapshot.v2` for C64 and `vice-sharp.runtime-snapshot.v1` for non-C64. Managed VSF staging exists in the test harness, but normal public and desktop save/load do not yet use the production native bridge.

Migration compatibility is therefore pending. A proprietary ViceSharp snapshot is not interchangeable with a VICE snapshot and does not satisfy the requirement. Track the production bridge and deterministic resume gates in [PLAN-VSFLOCKSTEP-001](plans/PLAN-VSFLOCKSTEP-001.md).

## 4. Performance and accuracy

ViceSharp targets behavioral parity with immutable native VICE. Current evidence is bounded:

- Historical C64 receipts include a 100,000-cycle BASIC READY CPU path and multi-frame no-cartridge model/checkpoint slices.
- Historical VIC-20 receipts include 10-second PAL and NTSC A/X/Y/S/P/PC comparisons plus named frame, flash, persistence, and audio slices.
- BenchmarkDotNet probes exist for CPU, VIC, SID, and CIA hot paths; native comparison sweeps remain open under `PERF-BENCHMARK-001`.

What this means in practice:

- A passing lockstep window proves only the models, workload, state fields, and cycle budget named by that test. It does not prove whole-machine or all-software compatibility.
- VIC-II raster effects, SID analog behavior, drive/tape/cartridge edge cases, input, timing, and host integration still require acceptance coverage against classic VICE.
- Full parity remains pending until active FR/TR/TEST acceptance criteria pass against an unmodified oracle with zero failed and zero skipped tests.
- The immutable-oracle and canonical snapshot gates are defined in [PLAN-VICE-IMMUTABLE-001](plans/PLAN-VICE-IMMUTABLE-001.md) and [PLAN-VSFLOCKSTEP-001](plans/PLAN-VSFLOCKSTEP-001.md).

## 5. Bug compatibility

Classic VICE faithfully reproduces several Commodore-era hardware bugs that real demos and games rely on. ViceSharp's current status:

| Bug | Status |
|-----|--------|
| 6510 illegal opcodes | Covered by focused implementation and bounded lockstep tests; final parity acceptance remains pending. |
| 6510 jump-vector page-cross bug (`JMP ($xxFF)`) | Covered by focused tests; final parity acceptance remains pending. |
| VIC-II "bad line" cycle stealing | Reproduced at the CIA / CPU contention level; managed matrix idle/fill behavior is covered, while native matrix checkpoints and FLI/AFLI effects remain under `BACKFILL-VIDEO-001`. |
| VIC-II sprite-DMA timing | Bounded. Sprite fetch is wired and side-border visibility is managed, but non-PAL per-model fetch tables and native multiplexing checkpoints remain. |
| SID ADSR bug | Reproduced in the focused Phase 1 SID suite; further analog deepening is post-MVP. |
| SID combined waveforms | Reproduced for the Phase 1 SID suite; further analog deepening is post-MVP. |
| 1541 GCR bit-stream timing | Byte-level GCR playback implemented under true drive: `GcrCodec` plus `C1541DriveMechanismDevice` raise byte-ready through VIA2 at per-speed-zone intervals (32/30/28/26 cycles). The sector-stream fast path remains the default fidelity; sub-byte bit-cell effects (weak bits, killer tracks) are not modeled. |

If a specific demo / game depends on a deferred bug, file a regression with the SID dump or D64; that helps prioritise the relevant slice.

## 6. Where to file regressions

File issues at [github.com/sharpninja/vice-sharp/issues](https://github.com/sharpninja/vice-sharp/issues). See [USER-GUIDE.md, Where to file regressions](USER-GUIDE.md#where-to-file-regressions) for what to include.

When opening a parity issue, include both the classic VICE invocation you are coming from and the ViceSharp invocation that should have matched, plus a minimal repro (D64 / CRT / TAP / SID dump).
