# Switching from Classic VICE to ViceSharp

A side-by-side reference for users who already drive classic VICE (`x64sc`, `c1541`, etc.) and want to try ViceSharp as a drop-in alternative. For the broader install / first-run story, start at [USER-GUIDE.md](USER-GUIDE.md).

## 1. Binary mapping

| Classic VICE | ViceSharp launcher | Status |
|--------------|--------------------|--------|
| `x64` | `x64.exe` | Supported. Maps to the cycle-exact C64 host (ViceSharp does not maintain a separate "fast" variant). |
| `x64sc` | `x64sc.exe` | Supported. Identical topology to `x64` in ViceSharp; both go through the same cycle-exact path. |
| `c1541` | `c1541.exe` | Supported as a standalone 1541 disk-tool topology (single drive, optional D64 mount). |
| `x128` | `x128.exe` | Not yet. Throws `NotSupportedException`. C128 is iteration 3. |
| `xvic` | `xvic.exe` | Not yet. VIC-20 is iteration 2. |
| `xpet` | `xpet.exe` | Not yet. PET is iteration 4. |
| `xplus4` | `xplus4.exe` | Not yet. Plus/4 / C16 is iteration 5. |
| `xcbm2` / `xcbm5x0` | same names | Not yet. CBM-II is post-MVP. |
| `vsid` | `vsid.exe` | Not yet. SID-only player is post-MVP. |
| `petcat` | `petcat.exe` | Not yet. BASIC tokeniser is post-MVP. |
| `cartconv` | `cartconv.exe` | Not yet. Cart converter is post-MVP. |

Binary-name dispatch is implemented in [ViceTopologyBuilder.cs](../src/ViceSharp.Launcher/ViceTopologyBuilder.cs); unsupported binaries deliberately throw with a message listing the supported set so you find out fast rather than silently doing the wrong thing.

## 2. Flag mapping

The launcher's flag parser is [ViceArgsParser.cs](../src/ViceSharp.Launcher/ViceArgsParser.cs). The table below is exhaustive for the parser; unlisted classic VICE flags are collected into `ViceArgs.Unknown` and silently ignored (matching VICE's own lenient handling).

### Supported

| Classic VICE flag | ViceSharp launcher | Notes |
|-------------------|--------------------|-------|
| `-8 <path>` | `-8 <path>` | Attach D64 to drive 8. |
| `-9 <path>` | `-9 <path>` | Attach D64 to drive 9. |
| `-cart <path>` | `-cart <path>` | Attach cartridge image. (Image is loaded; live memory-map wiring is bounded; see section 3.) |
| `+truedrive` | `+truedrive` | Enable true-drive emulation; drive YAML peripheral gets `fidelity: TrueDevice`. |
| `-truedrive` | `-truedrive` | Disable true-drive emulation. |
| `-config <path>` (closest analogue) | `--machine-yaml <path>` / `-m <path>` | Explicit machine topology YAML. ViceSharp uses YAML topologies instead of a flat `vicerc`. |
| (n/a in classic) | `--cycles <N>` | Host-cycle budget. Classic VICE runs until you quit; ViceSharp's console host needs a budget for deterministic batch runs. |
| `-verbose` / `-v` | `-v` / `--verbose` | Same intent. |
| `-help` / `-?` | `--help` / `-h` / `-?` | Same intent. |

### Partial / bounded

| Classic VICE flag | ViceSharp behaviour |
|-------------------|---------------------|
| `-cart <path>` | The image loads and normalises to 8K / 16K ROML+ROMH banks. Live cartridge-port pin wiring (`GAME` / `EXROM` driving the memory map) is staged but not exercised end-to-end yet; expect the cart to load but boot-from-cart behaviour to be incomplete. |
| `-autostart <path>` (classic) | No direct equivalent. Use `-8 <disk.d64>` + a BASIC `LOAD"*",8,1: RUN` from the running console, or build a topology where the drive image is mounted at boot. |

### Not yet (collected as unknown)

Each of these is currently in the launcher's `Unknown` bucket; the run still proceeds but the flag has no effect. They are all candidate work items for the launcher.

| Classic VICE flag | Status |
|-------------------|--------|
| `-warp` | Not yet. The console host always runs at full speed within its `--cycles` budget; a warp toggle is meaningful only for an interactive UI host. |
| `-sound` / `-soundoutput` | Not yet. SID audio backend wiring exists (`Sid6581(IBus, IAudioBackend?)`) but no default backend is connected. |
| `-fullscreen` | Not yet. There is no Avalonia host with display output wired in the launcher path. |
| `-model <name>` (`c64c`, `c64pal`, etc.) | Not yet. ViceSharp currently builds a single C64 model; PAL / NTSC variants are deferred. |
| `-ntsc` / `-pal` | Not yet. See `-model`. |
| `-autostart <path>` | Not yet. |
| `-tape <path>` | Not yet. TAP support exists at the device layer (`RUNTIME-TAPE-001`) but the launcher does not surface it. |
| `-monitor` (built-in machine-language monitor) | Not yet via the launcher. The monitor surface is being built under `BACKFILL-HOSTUI-001` (gRPC + typed RPCs). |
| `-keymap`, `-joydev`, `-userportdevice`, `-cartrev`, etc. | Not yet. |

If a flag you depend on is in this list, please file an issue (see [USER-GUIDE.md, Where to file regressions](USER-GUIDE.md#where-to-file-regressions)) so it gets prioritised against real demand.

## 3. Behaviour caveats

### Topology is YAML, not a flat config

Classic VICE puts everything in one command line (or one `vicerc`). ViceSharp prefers an explicit YAML topology because the substrate is multi-system from the start: each drive, cartridge-CPU, or user-port-CPU is its own clocked machine on a shared `IInterSystemBus`. The launcher synthesises a YAML on the fly for the common `-8` / `-9` / `+truedrive` cases, but for anything beyond that, write the YAML directly.

The canonical sample is [docs/samples/c64-plus-1541.multisystem.yaml](samples/c64-plus-1541.multisystem.yaml); see [USER-GUIDE.md section 5](USER-GUIDE.md#5-machine-yaml-topologies) for the schema.

Recommendation:
- VICE muscle memory: use `x64sc.exe -8 disk.d64 +truedrive` for the common cases.
- Multi-drive / multi-machine: write a YAML and pass `--machine-yaml`.

### No always-on UI / GUI host

The launcher invokes the console host. There is no Avalonia screen, no built-in monitor window, no sound output by default. ViceSharp's "interactive" runs are intended to come through the host UI work landing under `BACKFILL-HOSTUI-001`; until then, treat the launcher as a batch-mode emulator. `--cycles N` is the way you bound a run.

### True-drive emulation defaults

In ViceSharp, the canonical sample sets `fidelity: TrueDevice` explicitly. If you omit `fidelity:`, you get `Buffered` (sector-stream fast path), which is much faster but never runs the drive's 6502. Classic VICE's "TrueDrive 8" resource maps to `+truedrive` on the launcher, which in turn sets `fidelity: TrueDevice` on every drive peripheral the launcher emits.

## 4. Performance and accuracy

ViceSharp targets cycle-exact parity with native VICE on the C64 host path. The current gate:

- **100,000-cycle lockstep** against native VICE on the BASIC `READY.` boot path (`LockstepValidationTests`). Trace-by-trace identical CPU state.
- **Drive CPU lockstep accessors** (`vice_drivecpu_get_*`) expose VICE's per-unit drive 6502 register file from .NET. The `LockstepDriveValidationTests` gate verifies `Drive%uTrueEmulation` toggles cleanly and the drive CPU advances under TDE.
- **BenchmarkDotNet harness** is checked in under [tests/ViceSharp.Benchmarks/](../tests/ViceSharp.Benchmarks/) for the CPU / VIC / SID / CIA hot paths; native VICE comparison numbers are deferred (`PERF-BENCHMARK-001`).

What this means in practice:
- For boot sequences, KERNAL traps, and any code that lives inside the lockstep gate, ViceSharp's CPU output is identical to VICE cycle-for-cycle.
- For VIC-II pixel-level behaviour, the gate is "first scanline parity"; full pixel sequencer and sprite-collision parity are pending under `BACKFILL-VIDEO-001`. Demo code that depends on raster effects can diverge from VICE.
- For SID, hard sync and ring modulation are wired and exercised, but combined waveforms and the famous ADSR bug are deferred. Demos that depend on those will sound different.

## 5. Bug compatibility

Classic VICE faithfully reproduces several Commodore-era hardware bugs that real demos and games rely on. ViceSharp's current status:

| Bug | Status |
|-----|--------|
| 6510 illegal opcodes | Reproduced. Lockstep gate covers them. |
| 6510 jump-vector page-cross bug (`JMP ($xxFF)`) | Reproduced. |
| VIC-II "bad line" cycle stealing | Reproduced at the CIA / CPU contention level; pixel-level effects pending under `BACKFILL-VIDEO-001`. |
| VIC-II sprite-DMA timing | Bounded. Sprite fetch is wired but full collision parity pending. |
| SID ADSR bug | Not yet. Deferred under `BACKFILL-SID-001` slice 4. |
| SID combined waveforms | Not yet. Deferred under `BACKFILL-SID-001` slice 3. |
| 1541 GCR bit-stream timing | Not yet. Sector-stream fast path is the default; cycle-accurate GCR is future work. |

If a specific demo / game depends on a deferred bug, file a regression with the SID dump or D64; that helps prioritise the relevant slice.

## 6. Where to file regressions

File issues at [github.com/sharpninja/vice-sharp/issues](https://github.com/sharpninja/vice-sharp/issues). See [USER-GUIDE.md, Where to file regressions](USER-GUIDE.md#where-to-file-regressions) for what to include.

When opening a parity issue, include both the classic VICE invocation you are coming from and the ViceSharp invocation that should have matched, plus a minimal repro (D64 / CRT / TAP / SID dump).
