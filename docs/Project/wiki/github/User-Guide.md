# ViceSharp User Guide

This guide walks a new user from a fresh clone to a running C64 with a true-drive 1541 attached. If you are coming from classic VICE and want a quick translation of your usual `x64sc` / `c1541` invocations, see [VICE-MIGRATION.md](VICE-MIGRATION.md).

## 1. Install

### Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (10.0.201 or later). Verify with `dotnet --version`.
- **Git**.
- **Native VICE shim for Windows x64**. Download `vice-sharp-native-win-x64.zip` from [GitHub Releases](https://github.com/sharpninja/vice-sharp/releases), extract it at the repository root, and confirm `native/vice_x64.dll`, `native/libiconv-2.dll`, and `native/zlib1.dll` exist. Alternatively install **MSYS2 + mingw-w64 gcc** and let the build recreate the shim from source.

### Clone and build

```pwsh
git clone https://github.com/sharpninja/vice-sharp.git vice-sharp
cd vice-sharp
dotnet build ViceSharp.slnx
```

The build is `TreatWarningsAsErrors`; a clean checkout on a supported SDK builds with zero warnings. If you see SDK-version errors, re-check `dotnet --version`. If you do not install the release bundle first, Windows builds that touch native VICE will invoke `native/build-vice-shim.ps1` and require MSYS2 at `C:\msys64`.

### First-run verification

Run the supported test gate (the same filter the Nuke `Test` target uses):

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"
```

`./build.ps1 Test` runs the same gate. At the v1.0.2 release the baseline is Failed 0 / Passed 2594 / Skipped 21 / Total 2615 (single process). A failure here means your local environment is off (typically a missing native VICE bundle or a wrong .NET SDK) rather than a real regression.

Avoid an unfiltered `dotnet test ViceSharp.slnx`: it pulls in opt-in categories (determinism replays, AI reviews, quarantined parity suites), can exceed 5 minutes, and has hung historically.

## 2. Get ROMs

ViceSharp does not ship Commodore ROM images. Use the data root from an installed VICE package or another legal source. The full sourcing rules are in [ROMs.md](ROMs.md); the short version:

1. Set `VICESHARP_ROM_PATH` to your VICE data root, or put `x64sc.exe` on `PATH`:
   ```pwsh
   $env:VICESHARP_ROM_PATH = "C:\path\to\GTK3VICE-3.8-win64"
   ```
2. Use the native VICE layout, for example `C64/basic-901226-01.bin`, `C64/kernal-901227-03.bin`, `C64/chargen-901225-01.bin`, and `DRIVES/dos1541-325302-01+901229-05.bin`. See [ROMs.md](ROMs.md#rom-directory-structure) for the full tree.
3. The layout is checked at machine build time: known ROMs are validated by MD5 and a mismatch fails the load. See [ROMs.md](ROMs.md#rom-validation) for the known checksums.

If you have classic VICE installed, the preferred value is the VICE data root that contains `C64/` and `DRIVES/`; pointing directly at `C64/` is normalized to the parent root. If not, the `ViceSharp.RomFetch` library can resolve and download ROMs from a user-supplied source (a standalone CLI is planned but does not ship yet); see [ROMs.md](ROMs.md#vicesharpromfetch-tool) for what it provides today.

## 3. Run your first machine

ViceSharp's reference shell is the `ViceSharp.Console` project. Run the canonical C64 + 1541 topology shipped in `docs/samples/`:

```pwsh
dotnet run --project src/ViceSharp.Console -- `
    --roms $env:VICESHARP_ROM_PATH `
    --machine-yaml docs/samples/c64-plus-1541.multisystem.yaml `
    --cycles 1000000
```

Flag-by-flag:

| Flag | Meaning |
|------|---------|
| `--roms <dir>` | VICE data root. Defaults to automatic resolver behavior when omitted by runtime paths that use `ViceDataPathResolver`. |
| `--machine-yaml <path>` | Multi-system YAML topology to load (host + peripherals + buses). |
| `-m <path>` | Short form of `--machine-yaml`. |
| `--cycles <N>` | Host-cycle budget for this run. Defaults to 100,000. |
| `--trace <path>` | Append a deterministic instruction trace. |

You can also boot a default C64 host (no peripherals, no YAML) by omitting `--machine-yaml`:

```pwsh
dotnet run --project src/ViceSharp.Console -- --roms $env:VICESHARP_ROM_PATH --cycles 100000
```

## 4. CLI launcher (VICE-compatible flags)

`ViceSharp.Launcher` is a class library, not a set of executables: it supplies VICE-compatible argument parsing ([ViceArgsParser.cs](../src/ViceSharp.Launcher/ViceArgsParser.cs)) and binary-name topology dispatch ([ViceTopologyBuilder.cs](../src/ViceSharp.Launcher/ViceTopologyBuilder.cs)). VICE-named binaries (`x64.exe`, `x64sc.exe`, `c1541.exe`) are not yet shipped. Today the consumer is `ViceSharp.Console`, which parses its arguments as binary name `x64sc`, so you invoke the launcher flags through the console shell:

```pwsh
dotnet run --project src/ViceSharp.Console -- <flags>
```

The parser recognises this flag set (transcribed from [ViceArgsParser.cs](../src/ViceSharp.Launcher/ViceArgsParser.cs)):

| Flag | Meaning |
|------|---------|
| `-8 <path>` | Attach a D64 disk image to drive 8. |
| `-9 <path>` | Attach a D64 disk image to drive 9. |
| `-cart <path>` | Attach a cartridge image. |
| `+truedrive` | Enable true-drive emulation (`Fidelity: TrueDevice`). |
| `-truedrive` | Disable true-drive emulation. |
| `--machine-yaml <path>` / `-m <path>` | Explicit machine topology YAML. Overrides the binary-name default. |
| `--cycles <N>` | Host-cycle budget. |
| `-debugcart` / `+debugcart` | Enable / disable the debug cartridge ($D7FF exit signaling for regression harnesses). Note the VICE polarity: `-debugcart` enables, `+debugcart` disables. |
| `--limitcycles <N>` / `-limitcycles <N>` | Bounded execution cycle limit (testbench style); overrides the run's cycle budget. |
| `-autostart <path>` | Autostart a PRG file. |
| `program.prg` (positional) | Any bare `*.prg` argument is treated as an autostart PRG (classic VICE testbench style). |
| `-v` / `--verbose` | Verbose output. |
| `--help` / `-h` / `-?` | Show help. |

The console entry point currently consumes `--help`, `-debugcart` / `+debugcart`, `--limitcycles`, and `-autostart` / positional `.prg` from the parsed set (plus its own `--roms`, `--machine-yaml`, `--cycles`, and `--trace` flags from section 3). Disk, cartridge, and true-drive dispatch from parsed flags lives in the launcher library (`ViceTopologyBuilder`) and is not yet wired into the console entry point; use `--machine-yaml` for drive topologies today.

`ViceTopologyBuilder` selects a topology from the binary name (library dispatch, ready for when named binaries ship):

| Binary name | Topology |
|-------------|----------|
| `x64` | C64 host. Adds drives if `-8` / `-9` is given. |
| `x64sc` | Same as `x64` (cycle-exact path is the only path in ViceSharp). |
| `c1541` | Standalone 1541 (disk-only tool mode). |
| `x128`, `xvic`, `xpet`, `xplus4`, `xcbm2`, `xcbm5x0`, `vsid`, `petcat`, `cartconv` | Reserved. Throws `NotSupportedException` for now. |

Example: VICE testbench style "run a PRG with the debug cartridge and a bounded cycle budget":

```pwsh
dotnet run --project src/ViceSharp.Console -- --roms $env:VICESHARP_ROM_PATH -debugcart -limitcycles 5000000 testcase.prg
```

Unknown flags are not fatal: the parser collects them in `ViceArgs.Unknown` and proceeds, matching classic VICE's lenient behaviour. See [VICE-MIGRATION.md](VICE-MIGRATION.md) for which classic flags are currently in this bucket.

## 5. Machine YAML topologies

A multi-system topology describes a host machine, zero or more peripherals (drives, additional CPUs on the user/cart port), and the buses that connect them. The loader is `MultiSystemYamlLoader`; the canonical sample is [docs/samples/c64-plus-1541.multisystem.yaml](samples/c64-plus-1541.multisystem.yaml):

```yaml
schemaVersion: 1

coordinator:
  host:
    id: c64-host
    kind: C64
    busAttachments:
      - busId: IEC
        endpointName: c64

  peripherals:
    - id: drive-8
      kind: C1541
      deviceNumber: 8
      fidelity: TrueDevice
      busAttachments:
        - busId: IEC
          endpointName: drive-8

  buses:
    - id: IEC
      signals: [ATN, CLK, DATA, SRQ]
```

Schema rules in short form:

- `kind:` is dispatched to a real architecture descriptor. Today: `C64` and `C1541`. Other `kind:` values raise a validation error from the loader.
- `busAttachments:` wires a machine endpoint to a named bus. Endpoint names are arbitrary identifiers used by `InterSystemBusTracer` for diagnostics.
- `fidelity:` selects `TrueDevice` (full 6502 CPU + VIA emulation on the drive) or `Buffered` (sector-stream fast path). Default is `Buffered` if omitted.
- `diskImagePath:` on a `C1541` peripheral attaches a D64 at load time.
- `buses:` declares every bus referenced by a `busAttachments:` entry. `IEC` carries the standard four wires; other bus kinds (`UserPort`, `CartPort`) exist for connecting additional CPUs.

Add a second drive by uncommenting the `drive-9` block in the sample. Add a third machine on the user port by declaring a new peripheral and a `UserPort` bus that both ends attach to. The substrate auto-binds the C64 CIA2 and the 1541 VIA1/VIA2 to the right bus endpoints; no manual wiring code is required.

## 6. Disk images

Two ways to attach a D64:

- **YAML**: set `diskImagePath:` on the drive peripheral. See section 5 above. This is the working path today.
- **CLI (library dispatch, binaries pending)**: `x64sc -8 path\to\disk.d64` builds a transient YAML topology with the drive declared as a peripheral via `ViceTopologyBuilder`, but VICE-named binaries are not yet shipped and the console entry point does not consume `-8` yet (see section 4).

D64 mounts go through `D64DiskImageDevice`. Sector reads are deterministic and exercised by the regression suite. Current limitations to be aware of:

- Sector-stream fast path (`Buffered`) is the default when `fidelity:` is omitted. Under `fidelity: TrueDevice`, GCR track encoding and byte-level playback are implemented (`GcrCodec` plus `C1541DriveMechanismDevice`): the drive raises byte-ready through VIA2 at per-speed-zone intervals (32/30/28/26 cycles). Sub-byte bit-cell effects (weak bits, killer tracks used by some copy protections) are not modeled.
- 1541 head step + motor control are wired (4-phase Gray accumulator on VIA2 PB0/PB1, motor on PB2); seek timing matches the substrate's quarter-track resolution.
- D64 stream load, sector writes, and commit-to-stream are covered; launcher-level
  save-as workflows and non-D64 formats are still future work.

## 7. What works today / what doesn't

This matches the [README dashboard](../README.md#completion-dashboard); the matrix below is the practical "what should I expect" view.

| Area | State | Notes |
|------|-------|-------|
| C64 host (MOS 6510 + VIC-II + SID + CIA x2 + PLA + KERNAL boot) | Working | Cycle-exact lockstep parity with native VICE `x64sc`: 322 x64sc variant cases (C64 / C64C / SX-64, PAL + NTSC) at multi-frame depth, plus the 335-case lockstep/checkpoint gate and 100k-cycle parity. |
| Multi-system substrate (YAML topology + auto-binds) | Working | Canonical sample builds C64 + true-drive 1541 with one CLI invocation; ROM-backed boot still requires local ROM assets. |
| 1541 drive (true-drive: 6502 + VIA1 + VIA2 + IEC) | Bounded | IEC, head step, motor, D64 sector reads/writes, and stream commit are wired; drive-CPU lockstep and full KERNAL load validation remain Phase 1 work. |
| CIA1 / CIA2 timers, TOD, ports | Working | Full solution test passes. |
| SID hard sync + ring modulation | Working | Voice i syncs from voice ((i+2) % 3); triangle XOR with sync-source MSB. |
| SID audio backend wiring | Working (default on desktop) | The Avalonia desktop app enables a WinMM audio backend by default on Windows (it sets `VICESHARP_AUDIO=1` at startup; set `VICESHARP_AUDIO=0` to force silence). Headless and test hosts stay silent. Library consumers pass an `IAudioBackend` to `Sid6581(IBus, IAudioBackend?)` to receive 256-sample batches. |
| Standard 8K / 16K cartridge mapping (raw + CRT) | Working | Image normalises to ROML/ROMH and drives the live C64 memory map with GAME/EXROM behaviour; broader mapper families are post-MVP. |
| Tape (TAP pulse reads) | Bounded | TAP pulse stream, CIA1 FLAG integration, builder wiring, rewind, and seek are present; spin-up/spin-down timing and record state remain Phase 1 work. |
| Snapshot save/load | Bounded | 64K + public CPU state round-trips; full chip, timing, and resume state remain Phase 1 work. |
| Media capture / recording | Working | Screenshot (PNG/BMP), WAV sound, BMP frame sequence (all or unique frames), and muxed video with sound (MP4 / MKV / AVI via ffmpeg) export through the gRPC capture surface and the Avalonia Snapshot menu. See "Media capture and recording" below. |
| VIC-II pixel sequencer / sprite collisions | Partial | Visible sprite composition, sprite priority/collision coverage, display-mode pixel routing including invalid ECM priority/collision, managed continuous side-border behavior, VIC-II register readback masks/collision latch writes, and managed matrix idle/fill behavior are covered; native display-mode/register/matrix checkpoints, sprite fetch depth, and FLI/AFLI timing remain under `BACKFILL-VIDEO-001`. |
| SID combined waveforms + ADSR-bug accuracy | Working | Combined waveform, ADSR, digi, filter, PCM-equivalence, and dual-SID coverage are in the focused suite; further analog 8580/filter deepening is post-MVP unless final lockstep exposes a concrete regression. |
| Cartridge ports / user port as live CPU attachment | Substrate ready | `IInterSystemBus` supports `UserPort` and `CartPort` bus kinds; chip-level bindings are in for CIA2 / VIA1 / VIA2 / GAME / EXROM. Cartridge-as-running-CPU sample topology is future work. |
| C128 / VIC-20 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException`. |
| Host UI (Avalonia, monitor, gRPC control) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Launcher-integrated always-on UI remains separate work. |

### Warp, speed limiter, and speed controls

The Avalonia desktop UI exposes VICE-style speed controls:

- **Warp mode** runs the emulator uncapped, same semantics as VICE `-warp`; live sound is discarded while warp is on. Toggle it with **Alt+W**, the System menu's **Warp mode** item, or the Warp toggle in the Settings **Limiter** section.
- **Limiter slider** (Settings > Limiter) sets a live target speed percentage; the emulator paces to the slider target. Past 200% live sound is suspended.
- **Speed-cycle button** jumps between 100% (sound on) and 200%.
- The **status bar** shows the current state as `Limiter 100%` (or the active percentage) and `WARP` while warp is on.

### Media capture and recording

From the Avalonia desktop UI, the **Snapshot** menu exports emulator output:

- **Save screenshot...** writes the current frame as PNG or BMP (chosen by the file extension).
- **Record sound...** toggles a WAV recording of the SID output (start, then choose a file; pick the menu item again to stop).
- **Record video** offers three modes (toggle to start, pick again to stop):
  - **MP4 + sound** muxes H.264 video and AAC audio into an `.mp4` (also `.mkv` / `.avi` by extension). Requires `ffmpeg` on `PATH` (or set `VICESHARP_FFMPEG` to its full path).
  - **BMP sequence (all frames)** writes one numbered `frame_NNNNNN.bmp` per emulated frame into the chosen folder.
  - **BMP sequence (unique frames)** writes only frames that differ from the previous one, collapsing static screens.

Everything is also driveable over the gRPC `CaptureService` (capability discovery, screenshot, start/stop, and listing active captures). Muxed video and WAV sound need a live audio device; headless hosts export screenshots and the BMP sequence only.

## 8. Diagnostics and debug attach

When the Avalonia app starts its in-process gRPC host, it publishes a local attach file:

```text
%LOCALAPPDATA%\ViceSharp\debug-attach.json
```

The file records the process id, gRPC endpoint, protocol package, app version, current UI session id, auth mode, and timestamps. It is rewritten when the current UI session changes and is removed on clean shutdown.

For human debugging, use the Debug menu's **Copy Debug Attach Info** item. It copies the same attach information plus a current status summary.

For tool debugging, read `debug-attach.json` and call the diagnostics service directly. Development builds or `VICESHARP_GRPC_REFLECTION=1` enable gRPC reflection, so `grpcurl` can discover the service surface:

```pwsh
$attach = Get-Content "$env:LOCALAPPDATA\ViceSharp\debug-attach.json" | ConvertFrom-Json
grpcurl -plaintext $attach.endpoint list
grpcurl -plaintext -d "{}" $attach.endpoint vice_sharp.v1.DiagnosticsService/GetPerformanceSnapshot
```

`GetPerformanceSnapshot` defaults to the current UI session when `session_id` is omitted. Explicit unknown session ids return `NotFound`.

## 9. Troubleshooting

**`Failed to load multi-system YAML: ...`** - the loader is strict about schema. Re-check `kind:` (case-sensitive: `C64`, `C1541`), `busAttachments:` references resolve to a `buses:` entry, and `deviceNumber:` is 8..11.

**Missing ROMs / `FileNotFoundException` on kernal/basic/chargen** - `VICESHARP_ROM_PATH` is unset or pointing at the wrong directory. Verify the layout in [ROMs.md](ROMs.md#rom-directory-structure).

**Drive sits idle / `PC` never moves** - true-drive emulation may be off. In YAML, set `fidelity: TrueDevice` (the launcher library's `+truedrive` flag maps to the same setting). The CPU on a buffered drive doesn't run by design.

**`Native VICE failed to create a machine`** - the native shim couldn't allocate a VICE instance. On Windows this is usually disk pressure (the shim writes scratch state) or a missing native bundle. Free space and confirm `native/vice_x64.dll`, `native/libiconv-2.dll`, and `native/zlib1.dll` exist, or rebuild the shim with `pwsh native/build-vice-shim.ps1` (needs MSYS2 + gcc). Maintainers can package a rebuilt bundle with `pwsh tools/Package-NativeViceBinary.ps1`.

**`SetDriveTrueEmulation` returns non-zero** - VICE rejected the resource set. The most common cause is an out-of-range unit number; valid units are 8..11.

**Wrong-sized D64 / load errors** - only the canonical 174,848-byte D64 layout (35 tracks, no error info block) is fully exercised. Larger images may load but sector mapping may be wrong; non-D64 disk formats (G64, D71, D81) are not yet supported.

**Em-dashes in commit messages or doc PRs** - repo policy is to use a colon, hyphen, semicolon, or parentheses instead. CI does not enforce this but reviews will flag it.

## Where to file regressions

File issues at [github.com/sharpninja/vice-sharp/issues](https://github.com/sharpninja/vice-sharp/issues).

When filing, include: ViceSharp git SHA, .NET SDK version, the exact CLI / YAML invocation, expected vs. observed behaviour, and (if applicable) a minimal D64 / CRT / TAP that reproduces the issue.
