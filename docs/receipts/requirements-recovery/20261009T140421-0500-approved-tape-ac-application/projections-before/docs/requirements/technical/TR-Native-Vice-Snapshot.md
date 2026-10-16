# TR-Native-Vice-Snapshot: Native VICE VSF Snapshot Technical Requirement

## Document Information

| Field          | Value                         |
|----------------|-------------------------------|
| Quality Area   | Compatibility / Determinism   |
| Version        | 0.1.0-draft                   |
| Last Updated   | 2026-10-04                    |

---

## TR-LOCKSTEP-VSF-001: Native VICE VSF Snapshot Lockstep

**ID:** TR-LOCKSTEP-VSF-001
**Title:** Native VICE VSF Container and Deterministic Resume Compatibility
**Priority:** P0 -- Critical
**Category:** Compatibility / Correctness
**Status:** Pending

### Description

ViceSharp shall productionize and complete its existing managed VSF resume bridge so native VICE `.vsf` is the canonical snapshot implementation for the supported x64sc C64 and xvic VIC-20 product paths. The existing path asks native VICE to decode its version-specific modules, exports the resumed state through `IViceNative`, and injects that state into the managed machine. The production bridge shall also transfer managed state into the matching native instance so VICE itself writes the output `.vsf`.

### Acceptance Criteria

1. Native `.vsf` load and save use the matching pinned VICE snapshot implementation through the existing `IViceNative.ReadSnapshot` and `WriteSnapshot` bridge. The managed envelope walk accepts the current 58-byte and pinned-reader-compatible legacy 37-byte file headers, reads each 22-byte module header with a little-endian inclusive total length, reaches exact EOF, and preserves VICE machine identity, container/module versions, byte order, framing, and restore-order behavior without parsing version-specific payloads.
2. The existing managed C64 staging path is moved from the test harness into production and completed, and an equivalent VIC-20 adapter is added. Reusable CPU, video, CIA, VIA, SID, memory, cartridge, tape, and drive state contracts remain machine-agnostic while machine wiring stays in the owning machine or device adapter.
3. Snapshot application uses the same non-blocking try-create ABI in both native DLLs, treats C64 and xvic as independent per-DLL singleton leases, bounds acquisition to ten seconds with cancellation, finishes envelope validation plus native read and complete immutable state export before managed mutation, revalidates session generation/profile/media under the session lock, and restores a complete rollback bundle on apply failure. Rollback failure stops and faults the session until it is recreated.
4. Snapshot state preserves every cycle-visible CPU architectural and in-flight pipeline, bus, banking, raster/video pipeline, interrupt, timer, serial, oscillator/envelope/filter, peripheral, media, drive, and cartridge value required for deterministic continuation. Resetting in-flight CPU state is not accepted as full import.
5. Native x64sc and xvic remain the behavioral and file-compatibility implementations and oracles. Every normal supported snapshot API and Avalonia `win-x64` file, quick-slot, and ten-named-slot command invokes the native `.vsf` bridge by default; hosts without that packaged capability return unavailable without a proprietary fallback. UWP/Xbox implementation is out of scope and its historical requirements are not retired.
6. The existing `vice-sharp.machine-snapshot.v2` and `vice-sharp.runtime-snapshot.v1` readers are isolated behind the explicit protobuf `ImportLegacySnapshot(RestoreSnapshotRequest) returns (RestoreSnapshotResponse)` operation and matching `ISnapshotService.ImportLegacySnapshotAsync`. Normal `RestoreSnapshotAsync` rejects those formats with a migration error, no normal capture or save emits them, and replay remains a separate service. Manually injected CPU-plus-RAM state or native-only shim success does not satisfy this requirement.
7. The supported `win-x64` desktop publish and MSI build both shims, recursively verify their PE imports, stage only the proven loose non-system closure after `DotNetPublish` and before WiX harvest, include VICE license/source-offer material, contain no ROM images, and pass an installed default C64 and VIC-20 native-VSF capture/restore smoke test. Unsupported RIDs report native VSF unavailable instead of silently omitting the capability.

### Architecture Constraints

- Use `vice.native.vsf` as the canonical public `SnapshotDto.Format` identifier for supported C64 and VIC-20 snapshots.
- Inventory the exact mandatory and optional module names, versions, represented state, and restore order from the pinned VICE source in `docs/requirements/oracles/vice-vsf-state-transfer-manifest.json` before the existing bridge is promoted. This manifest drives state-transfer coverage, not a replacement VSF parser.
- Keep native VSF parsing and writing in native VICE. Put state transfer and wiring in production C64 and VIC-20 adapters built around machine-agnostic state records.
- Load and validate the VSF in a short-lived native instance, export it into immutable staged state, and validate all required state before the live session lock is entered.
- Capture a complete pre-apply rollback state, apply under `EmulatorRuntimeSession.SyncRoot`, and restore the rollback state before releasing the lock if any adapter rejects the staged state.
- Normal snapshot capture and save operations never emit a proprietary format. Retain the two existing proprietary readers only through `ImportLegacySnapshotAsync`; it restores legacy state into the current session once, and the next normal save produces native `.vsf`.

### Source References

- `native/vice/vice/src/snapshot.c`
- `native/vice/vice/src/snapshot.h`
- `native/vice/vice/src/c64/c64-snapshot.c`
- `native/vice/vice/src/vic20/vic20-snapshot.c`
- Pinned VICE commit: `cc67418cba58e71d8e70fd353e9de47199ad2075`

### Traceability

- **Functional Requirement:** FR-SNP-005
- **Test Requirement:** TEST-SNP-002
- **Related Requirements:** FR-SNP-001, FR-SNP-002, TR-DET-001
- **Recovered Legacy Requirements:** FR-VSFLOCKSTEP-001, TR-VSFLOCKSTEP-RESUME-001, TEST-VSFLOCKSTEP-001
- **Plan:** `docs/plans/PLAN-VSFLOCKSTEP-001.md`
- **Gate 0 local evidence:** `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`

---

## TR-VSFLOCKSTEP-RESUME-001: Backward-Compatible Snapshot Resume

**ID:** TR-VSFLOCKSTEP-RESUME-001
**Title:** Backward-compatible snapshot module read and VIC-II model alignment
**Priority:** P1 -- High
**Status:** Pending
**Scope Layer:** `layer-1`

### Description

An external compatibility adapter outside `native/vice` shall normalize legacy x64sc `MAINCPU` v1.2 and v1.3 modules for the pinned, unmodified VICE reader while current v1.4 modules remain byte-identical. The wrapper shall align the VIC-II model before `machine_read_snapshot` and normalize wrapper-reported `snapshot_last_error` after a successful read. No part of this compatibility path may edit, patch, generate into, or behaviorally change `native/vice`.

### Acceptance Criteria

1. An external compatibility adapter outside `native/vice` loads a validated `MAINCPU` v1.2 module by inserting the absent v1.3 `ane_log_level` and `lxa_log_level` fields and absent v1.4 `maincpu_jammed` field with the pinned immutable VICE defaults before `maincpu_ba_low_flags`, updates the inclusive module length, preserves every following byte and module, handles v1.3 by inserting only `maincpu_jammed`, and passes current v1.4 through byte-for-byte. Malformed or unsupported layouts fail closed. External v1.2 and v1.3 fixtures plus a current v1.4 native round-trip prove resume behavior with zero failed, zero skipped, and zero aborted prerequisites.
2. native/build-vice-shim.ps1 materializes the pinned VICE commit into a separate initially clean source root outside native/vice, follows the pinned VICE Team build instructions there (including required in-source build generation, explicitly approved 2026-10-08), and links only external wrapper translation units stored outside the submodule. Pre-build and post-build commit, tracked status and a complete inventory of original upstream source bytes must match exactly. Record all generated/build files separately with paths, hashes and producing commands; those artifacts are permitted only in the separate materialization/output roots and may not replace or modify original upstream source files. Any patch application, generated file or source mutation under native/vice fails the build. native/patches/vice-shim-runtime.patch is diagnostic legacy evidence only and is never applied.

### Traceability

- **Functional Requirements:** FR-VSFLOCKSTEP-001, FR-SNP-005
- **Test Requirements:** TEST-VSFLOCKSTEP-001, TEST-SNP-002
