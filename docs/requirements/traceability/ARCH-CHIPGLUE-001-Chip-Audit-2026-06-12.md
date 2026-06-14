# ARCH-CHIPGLUE-001 Chip Glue Audit - 2026-06-12

Status: verified for ARCH-CHIPGLUE-001. This document records current audit
evidence, moved behavior, and validation results for the chip-glue boundary
cleanup.

Canonical requirement mapping: `TR-SYSTEM-CORE-001` acceptance criteria 6-10
and `TEST-ARCH-CHIPGLUE-001`.

## Acceptance Criteria

| AC | Criterion | Evidence |
| --- | --- | --- |
| AC1 | Every type under `src/ViceSharp.Chips` is inventoried and classified as generic chip behavior, model-specific chip variant, media-format helper, or moved out of the shared chip package. | Inventory snapshot below. The current `src/ViceSharp.Chips` source set contains SID, CIA, CPU, VIA, PLA, VIC-II cores/variants, plus D64/GCR/TAP format helpers. |
| AC2 | Shared chips do not own machine-specific board glue. | `Mos6502` no longer contains C64 VIC-II `$D016` timing policy; `Mos6526` no longer defaults to C64 CIA1 `$DC00` or PAL `985_248` TOD cadence; `Via6522` no longer defaults to 1541 `$1800/$0400`; SID cores no longer default to C64 `$D400`; `Mos906114` no longer resets itself to C64 `$2F/$37`; `Mos6569` no longer owns C64 VIC-bank translation or the unused 320x200 host frame path. |
| AC3 | C64/C1541 board wiring lives in owning machine/device adapters. | `C64MemoryMap` owns C64 CPU `$D016` timing policy, C64 processor-port reset state, and C64 VIC-bank memory translation. `ArchitectureBuilder` assigns the CPU timing policy and supplies C64 CIA/SID address/cadence. `C64Cia2InterfaceDevice`, `C1541IecInterfaceDevice`, and `C1541DriveMechanismDevice` own CIA2/C1541 adapter wiring. |
| AC4 | Drive-specific wiring is encapsulated in drive/device implementation, not attached to the VIA chip. | `C1541DriveMechanismDevice` owns VIA2 motor, stepper, speed-zone, byte-ready, write-protect, sync, and GCR head behavior; `C1541IecInterfaceDevice` owns VIA1 IEC line mapping; `Via6522` exposes generic pins/registers only. |
| AC5 | C64/device helpers that are not shared chips no longer live in `src/ViceSharp.Chips`. | Moved to Core: `IecDrive`, `IecD64Attachment`, `IecBus`, `StandardCartridgeImage`, `StandardCartridgeSize`, `Datasette`, C64 input/VKM types, and media capture/recording helpers. Deleted retired stubs: duplicate `Interface/Cia6526`, legacy `Video/VicII`, fake IEC `DiskController`, and fake IEC `Mos6502DiskCpu`. |
| AC6 | Focused tests cover moved/guarded behavior. | 2026-06-12 focused gates in this run: `ChipGlueBoundaryTests` 11 passed; IEC/drive/boundary 32 passed; cartridge/boundary 261 passed; input/CIA/boundary 58 passed; tape/boundary 55 passed; media/boundary 29 passed; consolidated chip-glue gate 579 passed; lockstep/checkpoint gate 335 passed. Earlier same-day focused gates recorded in session output: CIA/VIA/C1541 225 passed; SID 103 passed; processor-port/C64 memory-map 35 passed; VIC-II/video 260 passed; IEC/C1541/D64 91 passed. All listed gates reported 0 failed, 0 skipped. |

## Current Inventory

| Type(s) | Current location | Classification | Glue finding |
| --- | --- | --- | --- |
| `Sid6581`, `Sid8580`, `Sid8580D`, `Mos6581`, `SidOscillator` | `src/ViceSharp.Chips/Audio`, `src/ViceSharp.Chips/Sid`, root | Generic SID/audio chip models | C64 `$D400` default removed; builders supply board-selected address windows. Remaining comments describe SID behavior, not C64 board wiring. |
| `Mos6526` | `src/ViceSharp.Chips/Cia/Mos6526.cs` | Shared CIA chip core | Board base address and TOD cadence are supplied by machine construction; FLAG/CNT/port wiring stays external. |
| `Mos6502` and opcode helpers | `src/ViceSharp.Chips/Cpu` | Shared CPU core | C64 VIC-II timing behavior is behind generic external predicates; C64 policy lives in `C64MemoryMap` and builder wiring. |
| `D64Image`, `D64ProgramFile`, `GcrCodec` | `src/ViceSharp.Chips/IEC` | Disk media-format helpers | Format-specific D64/GCR helpers only; drive-device ownership lives in Core. |
| `Via6522` | `src/ViceSharp.Chips/IEC/Via6522.cs` | Shared VIA chip core | Generic register and pin behavior only; board/device adapters supply register windows and external pin mapping. |
| `Mos906114` | `src/ViceSharp.Chips/PLA/Mos906114.cs` | PLA/processor-port banking model | C64 `$2F/$37` reset policy moved to `C64MemoryMap`; chip reset is neutral. |
| `TapImage`, `TapPulseReader` | `src/ViceSharp.Chips/Tape` | TAP media-format helpers | File-format parsing and pulse iteration only. `Datasette` device moved to Core. |
| `Mos6569`, `Mos6569R1`, `Mos6567`, `Mos6567R56A`, `Mos6572`, `Mos8562`, `Mos8565`, `VicPalette`, `VideoRenderer` | `src/ViceSharp.Chips/VicIi` | VIC-II chip family, variants, palette, and renderer | Model timing and raster rendering are chip behavior. C64 bank translation moved to `C64MemoryMap`; retired host-frame path removed. |

## Moved Or Retired Items

| Item | Prior location | Current owner |
| --- | --- | --- |
| `IecDrive`, `IecD64Attachment`, `IecBus` | `src/ViceSharp.Chips/IEC` | `src/ViceSharp.Core` |
| `StandardCartridgeImage`, `StandardCartridgeSize` | `src/ViceSharp.Chips/Cartridges` | `src/ViceSharp.Core` |
| C64 input/VKM types | `src/ViceSharp.Chips/Input` | `src/ViceSharp.Core/Input` |
| `Datasette` | `src/ViceSharp.Chips/Tape` | `src/ViceSharp.Core` |
| `FrameSequenceCapture`, `RecordingAudioBackend`, `WavAudioRecorder` | `src/ViceSharp.Chips/Media` | `src/ViceSharp.Core/Media` |
| `Cia6526` duplicate core | `src/ViceSharp.Chips/Interface/Cia6526.cs` | Deleted |
| `VicII` legacy video core | `src/ViceSharp.Chips/Video/VicII.cs` | Deleted |
| `DiskController`, `Mos6502DiskCpu` fake IEC stubs | `src/ViceSharp.Chips/IEC` | Deleted |

## VICE Separation Reference

VICE keeps common VIA behavior in `native/vice/vice/src/core/viacore.c` and
1541 board wiring in `native/vice/vice/src/drive/iecieee/via1d1541.c` /
`via2d.c`. The current ViceSharp split mirrors that boundary: `Via6522`
models the shared chip, while Core-owned device adapters translate VIA pins
to IEC, motor, byte-ready, sync, and GCR behavior.

## Closure Validation

- Source-boundary audit over `src/ViceSharp.Chips`: remaining source files are shared chip cores, chip variants, or disk/tape media-format helpers. Machine/device helpers were moved to Core or deleted.
- Consolidated focused gate:
  `dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~ChipGlueBoundaryTests|FullyQualifiedName~C64MemoryMap|FullyQualifiedName~ProcessorPortTests|FullyQualifiedName~Cia|FullyQualifiedName~Via6522|FullyQualifiedName~Sid|FullyQualifiedName~VicII|FullyQualifiedName~VideoRenderer|FullyQualifiedName~IecDriveMotorRampTests|FullyQualifiedName~IecTimingTests|FullyQualifiedName~StorageRuntimeTests|FullyQualifiedName~StandardCartridgeTests|FullyQualifiedName~C64JoystickPortTests|FullyQualifiedName~C64VkmKeyboardTests|FullyQualifiedName~Datasette|FullyQualifiedName~TapImageTests|FullyQualifiedName~WavAudioRecorderTests|FullyQualifiedName~RecordingAudioBackendTests|FullyQualifiedName~FrameSequenceCaptureTests" --logger "console;verbosity=minimal"`
  - Result: 579 passed, 0 failed, 0 skipped.
- Lockstep/checkpoint gate:
  `dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint" --logger "console;verbosity=minimal"`
  - Result: 335 passed, 0 failed, 0 skipped.
