# X64SC Model Matrix

## Document Information

| Field | Value |
|-------|-------|
| Backfill Plan | Backfill Imported Requirements To x64sc Parity |
| Target Emulator | x64sc |
| Last Updated | 2026-05-14 |
| Owning TODO | BACKFILL-MODEL-001 |

## Scope

Vice-Sharp x64sc parity means the C64-family model surface must cover every x64sc variant, not only the default breadbox PAL machine. This matrix is the required selector/profile baseline for subsequent core, video, media, input, host, UI, and lockstep work.

Non-x64sc machines imported from the VICE documentation remain outside this backfill unless they are shared infrastructure used by the x64sc path.

## Required Profiles

| Profile ID | Required Selectors | Display Standard | Clock Hz | Raster Geometry | VIC-II | SID | Board | Keyboard | Cartridge Boot | ROM Resources |
|------------|--------------------|------------------|----------|-----------------|--------|-----|-------|----------|----------------|---------------|
| `c64` | `c64`, `breadbox`, `pal`, `c64-pal`, `commodore64` | PAL | 985248 | 63 cycles x 312 lines | MOS6569 | MOS6581 | Breadbox | enabled | no | `basic-901226-01.bin`, `kernal-901227-03.bin`, `chargen-901225-01.bin` |
| `c64c` | `c64c`, `c64new`, `newpal`, `c64c-pal` | PAL | 985248 | 63 cycles x 312 lines | MOS8565 | MOS8580 | C64C | enabled | no | `basic-901226-01.bin`, `kernal-901227-03.bin`, `chargen-901225-01.bin` |
| `c64old` | `c64old`, `oldpal`, `c64old-pal` | PAL | 985248 | 63 cycles x 312 lines | MOS6569 | MOS6581 | BreadboxOld | enabled | no | `basic-901226-01.bin`, `kernal-901227-02.bin`, `chargen-901225-01.bin` |
| `ntsc` | `ntsc`, `c64ntsc`, `c64-ntsc` | NTSC | 1022730 | 65 cycles x 263 lines | MOS6567R8 | MOS6581 | Breadbox | enabled | no | `basic-901226-01.bin`, `kernal-901227-03.bin`, `chargen-901225-01.bin` |
| `newntsc` | `newntsc`, `c64cntsc`, `c64newntsc`, `c64c-ntsc` | NTSC | 1022730 | 65 cycles x 263 lines | MOS8562 | MOS8580 | C64C | enabled | no | `basic-901226-01.bin`, `kernal-901227-03.bin`, `chargen-901225-01.bin` |
| `oldntsc` | `oldntsc`, `c64oldntsc`, `c64old-ntsc` | NTSC | 1022730 | 64 cycles x 262 lines | MOS6567R56A | MOS6581 | BreadboxOld | enabled | no | `basic-901226-01.bin`, `kernal-901227-01.bin`, `chargen-901225-01.bin` |
| `paln` | `paln`, `drean`, `c64-paln` | PAL-N | 1023440 | 65 cycles x 312 lines | MOS6572 | MOS6581 | Drean | enabled | no | `basic-901226-01.bin`, `kernal-901227-03.bin`, `chargen-901225-01.bin` |
| `sx64pal` | `sx64`, `sx64pal`, `sx64-pal` | PAL | 985248 | 63 cycles x 312 lines | MOS6569 | MOS6581 | SX64 | enabled | no | `basic-901226-01.bin`, `kernal-251104-04.bin`, `chargen-901225-01.bin` |
| `sx64ntsc` | `sx64ntsc`, `sx64-ntsc` | NTSC | 1022730 | 65 cycles x 263 lines | MOS6567R8 | MOS6581 | SX64 | enabled | no | `basic-901226-01.bin`, `kernal-251104-04.bin`, `chargen-901225-01.bin` |
| `pet64pal` | `pet64`, `pet64pal`, `pet64-pal` | PAL | 985248 | 63 cycles x 312 lines | MOS6569 | MOS6581 | PET64 | enabled | no | `basic-901226-01.bin`, `kernal-901246-01.bin`, `chargen-901225-01.bin` |
| `pet64ntsc` | `pet64ntsc`, `pet64-ntsc` | NTSC | 1022730 | 65 cycles x 263 lines | MOS6567R8 | MOS6581 | PET64 | enabled | no | `basic-901226-01.bin`, `kernal-901246-01.bin`, `chargen-901225-01.bin` |
| `ultimax` | `ultimax`, `max` | NTSC | 1022730 | 65 cycles x 263 lines | MOS6567R8 | MOS6581 | Ultimax | enabled | yes | `basic-901226-01.bin`, `kernal-none.bin`, `chargen-901225-01.bin`; cartridge ROMH reset vector |
| `c64gs` | `c64gs`, `gs` | PAL | 985248 | 63 cycles x 312 lines | MOS8565 | MOS8580 | C64GS | disabled | yes | `basic-901226-01.bin`, `kernal-390852-01.bin`, `chargen-901225-01.bin`; GS cartridge |
| `c64jap` | `c64jap`, `jap` | NTSC | 1022730 | 65 cycles x 263 lines | MOS6567R8 | MOS6581 | Japanese | enabled | no | `basic-901226-01.bin`, `kernal-906145-02.bin`, `chargen-906143-02.bin` |

## Implementation Baseline

The current baseline provides a concrete model-profile catalog in `C64MachineProfiles`, exposes selected profile metadata through `IProfiledArchitectureDescriptor`, routes host session creation aliases through `DefaultEmulatorRuntimeFactory`, and applies profile timing, SID, keyboard, cartridge, and ROM-resource defaults in `ArchitectureBuilder`.

`C64RomSet` now resolves exact VICE ROM resource names for every x64sc profile, with the repo-local provider falling back to `native/vice/vice/data` when the workspace `roms` folder only carries compatibility aliases. Ultimax/MAX uses VICE's `kernal-none.bin` policy and takes the reset vector from cartridge ROMH in the deterministic lockstep path.

## Lockstep Requirement

The final x64sc gate must run every profile above against native x64sc and compare CPU registers, flags, cycle count, selected memory windows, CIA/VIC/SID observable register state, IRQ/NMI state, and frame/raster checkpoints. A skipped, stubbed, unsupported, or missing-ROM profile is a blocker, not a pass.
