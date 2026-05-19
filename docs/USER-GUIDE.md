# ViceSharp User Guide

This guide walks a new user from a fresh clone to a running C64 with a true-drive 1541 attached. If you are coming from classic VICE and want a quick translation of your usual `x64sc` / `c1541` invocations, see [VICE-MIGRATION.md](VICE-MIGRATION.md).

## 1. Install

### Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (10.0.201 or later). Verify with `dotnet --version`.
- **Git**.
- *(Optional)* **MSYS2 + mingw-w64 gcc**, only required if you intend to rebuild the native VICE shim DLL (`native/vice_x64.dll`). The DLL is checked in pre-built for Windows x64; you do not need MSYS2 just to run ViceSharp.

### Clone and build

```pwsh
git clone https://github.com/sharpninja/vice-sharp.git vice-sharp
cd vice-sharp
dotnet build ViceSharp.slnx
```

The build is `TreatWarningsAsErrors`; a clean checkout on a supported SDK builds with zero warnings. If you see SDK-version errors, re-check `dotnet --version`.

### First-run verification

```pwsh
dotnet test ViceSharp.slnx --nologo
```

Expect the entire suite to pass. A regression here means your local environment is off (typically a missing native VICE DLL or a wrong .NET SDK) rather than a real failure.

## 2. Get ROMs

ViceSharp does not ship Commodore ROM images. You must supply your own KERNAL, BASIC, CHARGEN, and 1541 DOS ROMs. The full sourcing rules are in [ROMs.md](ROMs.md); the short version:

1. Set `VICESHARP_ROM_PATH` to your ROM directory:
   ```pwsh
   $env:VICESHARP_ROM_PATH = "$env:USERPROFILE\.vicesharp\roms"
   ```
2. Lay the files out under that directory as `C64/kernal`, `C64/basic`, `C64/chargen`, `C64/1541` (or `DRIVES/1541` for a shared drive set). See [ROMs.md](ROMs.md#rom-directory-structure) for the full tree.
3. Validate the layout with `dotnet run --project src/ViceSharp.RomFetch -- --validate --rom-path "$env:VICESHARP_ROM_PATH"`.

If you have classic VICE installed, you can point `VICESHARP_ROM_PATH` straight at its `C64/` directory. If not, `ViceSharp.RomFetch` can import from a VICE install or a user-supplied URL; see [ROMs.md](ROMs.md#vicesharprromfetch-tool) for the exact commands.

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
| `--roms <dir>` | ROM root. Defaults to `roms/` in the working directory. |
| `--machine-yaml <path>` | Multi-system YAML topology to load (host + peripherals + buses). |
| `-m <path>` | Short form of `--machine-yaml`. |
| `--cycles <N>` | Host-cycle budget for this run. Defaults to 100,000. |
| `--trace <path>` | Append a deterministic instruction trace. |

You can also boot a default C64 host (no peripherals, no YAML) by omitting `--machine-yaml`:

```pwsh
dotnet run --project src/ViceSharp.Console -- --roms $env:VICESHARP_ROM_PATH --cycles 100000
```

## 4. CLI launcher (VICE-name binaries)

`ViceSharp.Launcher` ships VICE-named entry points so muscle memory carries over. Build it once and the relevant executables drop into `src/ViceSharp.Launcher/bin/<config>/net10.0/`:

```pwsh
dotnet build src/ViceSharp.Launcher
```

The launcher recognises this flag set (transcribed from [ViceArgsParser.cs](../src/ViceSharp.Launcher/ViceArgsParser.cs)):

| Flag | Meaning |
|------|---------|
| `-8 <path>` | Attach a D64 disk image to drive 8. |
| `-9 <path>` | Attach a D64 disk image to drive 9. |
| `-cart <path>` | Attach a cartridge image. |
| `+truedrive` | Enable true-drive emulation (`Fidelity: TrueDevice`). |
| `-truedrive` | Disable true-drive emulation. |
| `--machine-yaml <path>` / `-m <path>` | Explicit machine topology YAML. Overrides the binary-name default. |
| `--cycles <N>` | Host-cycle budget. |
| `-v` / `--verbose` | Verbose output. |
| `--help` / `-h` / `-?` | Show help. |

The launcher selects a topology from the binary name (see [ViceTopologyBuilder.cs](../src/ViceSharp.Launcher/ViceTopologyBuilder.cs)):

| Binary | Topology |
|--------|----------|
| `x64` | C64 host. Adds drives if `-8` / `-9` is given. |
| `x64sc` | Same as `x64` (cycle-exact path is the only path in ViceSharp). |
| `c1541` | Standalone 1541 (disk-only tool mode). |
| `x128`, `xvic`, `xpet`, `xplus4`, `xcbm2`, `xcbm5x0`, `vsid`, `petcat`, `cartconv` | Reserved. Throws `NotSupportedException` for now. |

Example: VICE-style "boot the C64 with a disk in drive 8 and true-drive emulation on":

```pwsh
.\src\ViceSharp.Launcher\bin\Debug\net10.0\x64sc.exe -8 .\disks\game.d64 +truedrive --cycles 5000000
```

Unknown flags are not fatal: the launcher collects them in `ViceArgs.Unknown` and proceeds, matching classic VICE's lenient behaviour. See [VICE-MIGRATION.md](VICE-MIGRATION.md) for which classic flags are currently in this bucket.

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

- **CLI**: `x64sc.exe -8 path\to\disk.d64`. The launcher builds a transient YAML topology with the drive declared as a peripheral.
- **YAML**: set `diskImagePath:` on the drive peripheral. See section 5 above.

D64 mounts go through `D64DiskImageDevice`. Sector reads are deterministic and exercised by the regression suite. Current limitations to be aware of:

- Sector-stream fast path is the default. Cycle-accurate GCR bit-stream playback (relevant to copy-protected images and fastloader tricks) is deferred and tracked under future drive work.
- 1541 head step + motor control are wired (4-phase Gray accumulator on VIA2 PB0/PB1, motor on PB2); seek timing matches the substrate's quarter-track resolution.
- Writes to the D64 are not persisted to disk in the current build.

## 7. What works today / what doesn't

This matches the [README dashboard](../README.md#completion-dashboard); the matrix below is the practical "what should I expect" view.

| Area | State | Notes |
|------|-------|-------|
| C64 host (MOS 6510 + VIC-II + SID + CIA x2 + PLA + KERNAL boot) | Working | 100k-cycle lockstep parity against native VICE. |
| Multi-system substrate (YAML topology + auto-binds) | Working | Canonical sample boots C64 + true-drive 1541 with one CLI invocation. |
| 1541 drive (true-drive: 6502 + VIA1 + VIA2 + IEC) | Working | Cycle-exact IEC handshake; head step + motor + sector reads wired. |
| CIA1 / CIA2 timers, TOD, ports | Working | Full solution test passes. |
| SID hard sync + ring modulation | Working | Voice i syncs from voice ((i+2) % 3); triangle XOR with sync-source MSB. |
| SID audio backend wiring | Working (off by default) | `Sid6581(IBus, IAudioBackend?)`; pass an `IAudioBackend` to receive 256-sample batches. |
| Standard 8K / 16K cartridge mapping (raw + CRT) | Bounded | Image normalised to ROML/ROMH banks; not yet wired into live memory map. |
| Tape (TAP pulse reads) | Bounded | Pulse stream is deterministic; motor gating is bounded. |
| Snapshot save/load | Bounded | 64K + public CPU state round-trips. |
| Frame capture (BGRA -> BMP) | Bounded | Single-frame artifact works; continuous stream pending. |
| VIC-II pixel sequencer / sprite collisions | Partial | First-scanline parity established; full pixel composition + sprite fetch pending under `BACKFILL-VIDEO-001`. |
| SID combined waveforms + ADSR-bug accuracy | Partial | Hard sync + ring mod landed; combined waveforms + ADSR bug pending. |
| Cartridge ports / user port as live CPU attachment | Substrate ready | `IInterSystemBus` supports `UserPort` and `CartPort` bus kinds; chip-level bindings are in for CIA2 / VIA1 / VIA2 / GAME / EXROM. Cartridge-as-running-CPU sample topology is future work. |
| C128 / VIC-20 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException`. |
| Host UI (Avalonia, monitor, gRPC control) | Partial | Wireframes + gRPC contracts landed; full UI pending. |

## 8. Troubleshooting

**`Failed to load multi-system YAML: ...`** — the loader is strict about schema. Re-check `kind:` (case-sensitive: `C64`, `C1541`), `busAttachments:` references resolve to a `buses:` entry, and `deviceNumber:` is 8..11.

**Missing ROMs / `FileNotFoundException` on kernal/basic/chargen** — `VICESHARP_ROM_PATH` is unset or pointing at the wrong directory. Verify the layout in [ROMs.md](ROMs.md#rom-directory-structure).

**Drive sits idle / `PC` never moves** — true-drive emulation may be off. In YAML, set `fidelity: TrueDevice`. From the launcher, pass `+truedrive`. The CPU on a buffered drive doesn't run by design.

**`Native VICE failed to create a machine`** — the native shim couldn't allocate a VICE instance. On Windows this is usually disk pressure (the shim writes scratch state) or a missing `native/vice_x64.dll`. Free space and confirm the DLL is next to the test binary, or rebuild the shim with `pwsh native/build-vice-shim.ps1` (needs MSYS2 + gcc).

**`SetDriveTrueEmulation` returns non-zero** — VICE rejected the resource set. The most common cause is an out-of-range unit number; valid units are 8..11.

**Wrong-sized D64 / load errors** — only the canonical 174,848-byte D64 layout (35 tracks, no error info block) is fully exercised. Larger images may load but sector mapping may be wrong; non-D64 disk formats (G64, D71, D81) are not yet supported.

**Em-dashes in commit messages or doc PRs** — repo policy is to use a colon, hyphen, semicolon, or parentheses instead. CI does not enforce this but reviews will flag it.

## Where to file regressions

File issues at [github.com/sharpninja/vice-sharp/issues](https://github.com/sharpninja/vice-sharp/issues).

When filing, include: ViceSharp git SHA, .NET SDK version, the exact CLI / YAML invocation, expected vs. observed behaviour, and (if applicable) a minimal D64 / CRT / TAP that reproduces the issue.
