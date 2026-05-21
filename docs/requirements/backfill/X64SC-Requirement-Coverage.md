# X64SC Requirement Coverage

## Document Information

| Field | Value |
|-------|-------|
| Backfill Plan | Backfill Imported Requirements To x64sc Parity |
| Target Emulator | x64sc |
| Last Updated | 2026-05-21 |
| Owning TODO | BACKFILL-COVERAGE-001 |

## Classification Rules

Each imported FR is classified for the x64sc backfill with one of these values:

| Classification | Meaning |
|----------------|---------|
| x64sc Required | Must be implemented and validated for x64sc parity. |
| Shared Infrastructure | Needed by x64sc indirectly, usually through drive, resource, build, or host infrastructure. |
| Out Of x64sc Scope | Imported behavior belongs to a non-x64sc machine family and is not part of this backfill. |
| Deferred After x64sc | Useful behavior that can wait until after the x64sc parity gate. |

## Coverage Matrix

| Requirement Area | FR IDs | Classification | Owning TODO | Notes |
|------------------|--------|----------------|-------------|-------|
| CPU 6510 execution | `FR-CPU-001` to `FR-CPU-004` | x64sc Required | BACKFILL-CORE-001 | Includes undocumented NMOS behavior, exact cycle timing, interrupts, and the 6510 I/O port. |
| C128 8502 mode | `FR-CPU-005` | Out Of x64sc Scope | none | C128 is imported but outside the x64sc target. |
| Memory and PLA banking | `FR-MEM-001` to `FR-MEM-006` | x64sc Required | BACKFILL-CORE-001 | Includes Ultimax memory mode, VIC bank switching, color RAM, zero page, stack behavior, and definable system-core PLA/address-decoder policy assembled by `ArchitectureBuilder`. |
| VIC-II rendering and timing | `FR-VIC-001` to `FR-VIC-010` | x64sc Required | BACKFILL-VIDEO-001 | Must cover PAL, NTSC, old NTSC, PAL-N, 6567, 6569, 6572, 8562, and 8565 timing. Border behavior includes closed-border sprite masking and opened-border sprite visibility from `FR-VIC-007`; sprite DMA timing must use model-specific VICE tables from `FR-VIC-010`. |
| SID audio | `FR-SID-001` to `FR-SID-012` | x64sc Required | BACKFILL-SID-001 | Includes 6581, 8580, filter differences, digi playback, and multi-SID configuration where x64sc exposes it. |
| CIA I/O | `FR-CIA-001` to `FR-CIA-007` | x64sc Required | BACKFILL-CORE-001 | Includes timers, TOD, keyboard matrix scanning, joystick reads, shift register, IRQ, and NMI behavior. |
| VIA chip core | `FR-VIA-001` to `FR-VIA-003` | Shared Infrastructure | BACKFILL-MEDIA-001 | Required for drive-side behavior, not the C64 motherboard. |
| VIC-20 VIA integration | `FR-VIA-004` | Out Of x64sc Scope | none | Belongs to xvic, not x64sc. |
| Disk drive VIA integration | `FR-VIA-005` | Shared Infrastructure | BACKFILL-MEDIA-001 | Required by 1541/IEC parity for x64sc. |
| Disk and IEC | `FR-DRV-001` to `FR-DRV-006` | x64sc Required | BACKFILL-MEDIA-001 | Covers 1541 baseline plus x64sc-supported drive attachment and fast loader behavior. |
| Tape and datasette | `FR-TAP-001` to `FR-TAP-005` | x64sc Required | BACKFILL-MEDIA-001 | SX-64 and C64GS profile quirks must be handled by model-specific resource/peripheral policy. |
| Cartridge | `FR-CRT-001` to `FR-CRT-005` | x64sc Required | BACKFILL-MEDIA-001 | Includes Ultimax/MAX and C64GS boot requirements. |
| Input devices and VKM | `FR-INP-001` to `FR-INP-006` | x64sc Required | BACKFILL-INPUT-001 | Keyboard matrix, control ports, paddles, mouse, lightpen, and VICE keymaps are x64sc parity inputs. |
| Monitor | `FR-MON-001` to `FR-MON-006` | x64sc Required | BACKFILL-HOSTUI-001 | Monitor commands must route through the host control surface. |
| Snapshot and replay | `FR-SNP-001` to `FR-SNP-004` | x64sc Required | BACKFILL-MEDIA-001 | Final validation relies on deterministic state capture and comparison. |
| Media capture | `FR-MED-001` to `FR-MED-005` | x64sc Required | BACKFILL-MEDIA-001 | Host/UI capture entrypoints stay behind the control boundary. |
| Host and gRPC boundary | `FR-HOST-001` to `FR-HOST-006` | x64sc Required | BACKFILL-HOSTUI-001 | gRPC remains the control, configuration, media, input, and monitor boundary. |
| Avalonia control UI | `FR-UI-001` to `FR-UI-004` | x64sc Required | BACKFILL-HOSTUI-001 | The renderer may stay direct in-process; controls route through host abstractions. |
| Configuration and resources | `FR-CFG-001` to `FR-CFG-008` | x64sc Required | BACKFILL-CORE-001, BACKFILL-MEDIA-001, BACKFILL-HOSTUI-001 | Required for model selection, ROM selection, palettes, autostart, hotkeys, peripherals, debug behavior, and limiter settings. |
| Existing C64-family profiles | `FR-PRF-001` to `FR-PRF-003` | x64sc Required | BACKFILL-MODEL-001 | Existing imported profile FRs cover C64, C64C, and SX-64. |
| Non-x64sc machine profiles | `FR-PRF-004` to `FR-PRF-008` | Out Of x64sc Scope | none | C128, VIC-20, PET, Plus/4, and C16 are imported but deferred until after x64sc. |

## Additional x64sc Model Coverage

The imported machine profile FRs do not yet assign standalone IDs for old PAL/NTSC, PAL-N/Drean, PET64, Ultimax/MAX, C64GS, or Japanese C64. They are required by the x64sc parity target and are covered by `BACKFILL-MODEL-001` plus `X64SC-Model-Matrix.md` until a later requirements cleanup splits them into canonical FR IDs.

## Test Mapping Baseline

| Coverage Area | Test Requirement |
|---------------|------------------|
| CPU | `TEST-CPU-001` |
| Memory and banking | `TEST-MEM-001` |
| VIC-II | `TEST-VIC-001` |
| SID | `TEST-SID-001` |
| CIA and keyboard matrix | `TEST-CIA-001` |
| VIA and drive integration | `TEST-VIA-001`, `TEST-DRV-001` |
| Tape | `TEST-TAP-001` |
| Cartridge | `TEST-CRT-001` |
| Input and VKM | `TEST-INPUT-001` |
| Monitor | `TEST-MON-001` |
| Snapshot/replay | `TEST-SNP-001` |
| Media capture | `TEST-MED-001` |
| Machine profiles | `TEST-PRF-001` |
| gRPC and host | `TEST-GRPC-001`, `TEST-HOST-001` |
| UI shell | `TEST-UI-001` |
| Configuration/resources | `TEST-CFG-001` |
| Final x64sc parity gate | `TEST-X64SC-LOCKSTEP-001` |

## Current Evidence

- Baseline solution validation before this slice: `dotnet test .\ViceSharp.slnx --nologo` passed with 93 tests.
- Profile-focused validation for this slice: `dotnet test .\ViceSharp.slnx --nologo --filter C64MachineProfileTests` passed with 47 tests.
- Core-definition scaffold validation: x64sc profiles now expose `ISystemCoreDefinition` policy for board, PLA/address decoder, bus, keyboard matrix, and cartridge boot behavior; `dotnet test .\ViceSharp.slnx --nologo --filter "C64MachineProfileTests|HostInputServiceTests"` passed with 54 tests.
- Architecture-builder boundary validation: `ArchitectureBuilder` now registers the profile-selected `ISystemCore` beside CPU/VIC/SID/CIA/PLA chips, preserving the builder as the glue between system-core policy and chip instances; `dotnet test .\ViceSharp.slnx --nologo` passed with 154 tests.
- Native x64sc selector validation: the shim now creates native machines with profile-specific x64sc model selectors, applies deterministic cartridge state for Ultimax/MAX and C64GS smoke coverage, exposes native physical RAM and interrupt reads, and validates CPU register/cycle parity plus selected physical RAM windows for every required x64sc profile.
- CIA core backfill validation: Timer B can count Timer A underflows instead of Phi2 ticks, TOD writes target clock or alarm according to CRB bit 7, and TOD carry uses BCD-style rollover; `dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --filter "FullyQualifiedName~CiaTimerInterruptTests"` passed with 6 tests.
- VICE ROM-resource validation: every x64sc profile now maps to the exact VICE `c64model.c` ROM resources (`basic-901226-01.bin`, profile-selected KERNAL, profile-selected chargen, and `kernal-none.bin` for MAX/Ultimax), and builder tests verify the selected bytes are mapped into the machine; `dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --filter "FullyQualifiedName~C64MachineProfileTests"` passed with 82 tests.
- x64sc raster/chip checkpoint validation: `X64ScVariantLockstepTests` now compares native and managed CPU/cycle state, selected physical RAM windows, VIC-II raster line/cycle/badline checkpoints, stable CIA register checkpoints, SID register checkpoints, and IRQ/NMI assertion state after one profile-specific scanline for every required x64sc profile. C64GS is validated with a deterministic 512K GS cartridge and profile-selected GS KERNAL resource; `dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --filter "FullyQualifiedName~X64ScVariantLockstepTests"` passed with 74 tests.
- Latest VIC-II edge validation: `TR-VIC-EDGE-002` carries opened side-border state through `Mos6569` and `VideoRenderer`; `TR-VIC-EDGE-006` matches VICE/x64sc readback masks for `$D019`, `$D01A`, unused `$D02F-$D03F`, and collision-register write behavior; and `TR-VIC-EDGE-005` now covers managed matrix/idle fetch behavior from `viciisc/vicii-fetch.c`, including `$ff` prefetch matrix fill, raw CPU-PC RAM color nibbles, standard-text latch consumption, and ECM `$39ff` idle graphics reads. Focused matrix/idle plus adjacent timing validation passed `18/18`, broader VIC/video validation passed `179/179`, and `tools\check_requirement_traceability.ps1` passed with 163 canonical IDs, 82 referenced canonical IDs, 81 unreferenced canonical IDs, and 53 noncanonical references.
- Current full-solution validation: `dotnet test .\ViceSharp.slnx --no-build --nologo` was attempted on 2026-05-21 and timed out after five minutes, then leftover test processes were stopped cleanly. Treat the current green evidence as focused, not solution-wide.
- MCP TODO epics now exist for coverage, model profiles, core timing, video, input, media, SID, host/UI, and final lockstep.

## Closure Rule

This coverage document is not final parity evidence. The backfill closes only when `BACKFILL-LOCKSTEP-001` passes native x64sc lockstep for every required profile without skipped or unsupported variants.
