# PLAN-VSFLOCKSTEP-001: Complete and productionize native VICE snapshot support

Status: Awaiting operator approval. No native snapshot product implementation is authorized by this plan draft.

## Goal

Complete the existing managed VSF support and restore native VICE `.vsf` as the canonical ViceSharp snapshot format for x64sc C64 and xvic VIC-20. Every normal public and desktop snapshot path must load and save `.vsf` through the production bridge, and the managed emulator must resume deterministically against the matching VICE engine for at least 100,000 cycles.

The operator correction is binding: native VICE snapshot capability is a must and is non-negotiable. Proprietary ViceSharp snapshot and replay formats do not satisfy this goal.

## Requirements and traceability

Current full-capability requirements:

- `FR-SNP-005`: canonical native VICE snapshot interoperability, nine acceptance criteria, pending.
- `TR-LOCKSTEP-VSF-001`: production bridge, canonical default routing, complete state transfer, transactionality, lockstep, and installed packaging, seven acceptance criteria, pending.
- `TEST-SNP-002`: default-path, bidirectional x64sc/xvic compatibility, corruption, and installed-package gates, eight acceptance criteria, pending.

Recovered legacy requirements remain active and are not retired:

- `FR-VSFLOCKSTEP-001`: external x64sc `.vsf` resume in the native oracle, three acceptance criteria, pending.
- `TR-VSFLOCKSTEP-RESUME-001`: backward-compatible native module resume and VIC-II model alignment, two acceptance criteria, pending.
- `TEST-VSFLOCKSTEP-001`: external x64sc full-resume acceptance, one acceptance criterion, pending.

Related requirements: `FR-SNP-001`, `FR-SNP-002`, `FR-SNP-003`, and `TR-DET-001`.

`PLAN-VSFLOCKSTEP-001` remains open until every gate has executable proof and hostile AGREE with accuracy and completeness each at least 98.

## Scope

In scope:

- Existing native VICE snapshot bridge and managed state-injection path.
- Native `.vsf` load and save for x64sc C64 and xvic VIC-20.
- Complete state transfer required for deterministic continuation: CPU, memory, video, CIA/VIA, SID/VIC audio, timing, interrupts, banking, cartridges, tape, and drives represented by the supported snapshot.
- Production `SnapshotServiceHost`, desktop file command, quick-slot, and named-slot integration with `vice.native.vsf` as the canonical default.
- Bidirectional VICE compatibility and at least 100,000 post-restore lockstep cycles.
- Mandatory native libraries and fixtures with zero skipped acceptance tests.

Out of scope:

- UWP and Xbox implementation. Their historical requirements are not deleted or retired, but they are outside this effort.
- VICE machines other than x64sc C64 and xvic VIC-20.
- A replacement managed parser for VICE's version-specific snapshot modules.
- Starting Phase 3 or Phase 4 from the current parity plan without the separately required operator direction.

## Corrected existing baseline

Managed VSF support has existed for months. The evidence is in the current tree:

- `SnapshotResumeSpikeTests.ParseVsf` is a managed container walker for the `MAINCPU` and `C64MEM` modules used by the original feasibility spike.
- `LockstepValidator.RunFromSnapshot` loads a `.vsf` into a native VICE instance, resets the managed machine, calls `StageManagedFromNative`, and immediately compares managed and native execution.
- `LockstepValidator.StageManagedFromNative` transfers all 64 KB RAM, the written 6510 processor-port direction/data values, CPU architectural registers, VIC-II registers/raster/badline/display state, and CIA1/CIA2 ports, timers, control, flags, and masks into the managed machine. It calls `GetCpuPipelineState`, but consumes only `PportDir` and `PportData`; `InjectSnapshotResumeState` writes A/X/Y/S/P/PC and then calls `ResetInFlightState`. The current bridge therefore reconstructs a first-cycle restart with a measured stagger; it does not import the native instruction pipeline, BA-low flags, pending-interrupt clocks, or delay counters.
- `Mos6502.InjectSnapshotResumeState`, `Mos6569.InjectSnapshotState`, and `Mos6526.InjectSnapshotState` are production chip hooks created for this resume path.
- `SaveManagedRunLog` deliberately uses a short-lived native instance to decode version-specific VSF module blobs before staging the managed machine.
- `IViceNative`, `ViceNative`, and `ViceNative.Xvic` already expose native `ReadSnapshot` and `WriteSnapshot` operations.
- The dirty worktree also contains internal C64 execution-image support used by FR-SNP-003 replay/rollback work. That internal image is useful managed-state infrastructure, but it is neither the native VSF format nor proof that VICE pipeline fields can be imported.

The earlier draft incorrectly described this as absent and proposed a new managed container codec. That approach is withdrawn.

## Remaining divergence

Observation:

- The working managed resume bridge is located in `tests/ViceSharp.TestHarness/LockstepValidator.cs`, not in a production service.
- The C64 bridge transfers RAM, processor port, CPU, VIC-II, and CIA state. It does not yet prove complete SID, cartridge, tape, drive, and every cycle-visible peripheral state required by FR-SNP-005.
- `StageManagedFromNative` is C64-specific. It expects `Mos6569` and two `Mos6526` devices and does not stage an xvic VIC-20 machine with VIC-I and dual VIA.
- Native write exists, but there is no proven production transfer of complete managed state into a native VICE instance before `WriteSnapshot`.
- `SnapshotServiceHost` exposes only `vice-sharp.machine-snapshot.v2` and `vice-sharp.runtime-snapshot.v1`; normal capture and restore paths do not route through the existing native VSF bridge.
- Several VSF tests skip when the native library or fixture is absent. A skip cannot prove the non-negotiable capability.

How ViceSharp diverged from VICE:

- The VSF bridge became a test-harness staging mechanism instead of the production snapshot boundary.
- Later proprietary snapshot formats displaced the required VICE-native snapshot path, became the normal product formats, and captured less state than VICE.
- xvic support and reverse managed-to-native state transfer were not completed under the same acceptance gate.

How ViceSharp gets back to VICE:

- Move the already proven state-transfer mechanism into production without changing its behavioral oracle.
- Let native VICE continue to read and write its version-specific file format.
- Complete state export and import for C64 and VIC-20, then prove field equality and long-run lockstep in both directions.
- Make that native VSF bridge the default for every normal snapshot API, desktop file command, quick slot, and named slot, and make missing prerequisites fail.
- Isolate any retained proprietary reader behind an explicit import-only migration boundary rather than exposing it as a peer snapshot format.

The narrow FR-SNP-003 cartridge replay remediation received hostile AGREE at 99 accuracy and 99 completeness. That verdict proves only cartridge replay value preservation. It does not prove native VICE snapshot compatibility and cannot close Phase 2 after `FR-SNP-005` was added.

## Locked architecture

### Preserve VICE as the file-format implementation

- Native VICE remains the VSF reader and writer. ViceSharp will not duplicate VICE's version-specific module parsers in a new managed codec.
- Load path: a short-lived matching native instance calls `ReadSnapshot`, exports a complete immutable state bundle through `IViceNative`, and a production machine adapter validates and applies that bundle to the managed machine.
- Save path: a production machine adapter captures a complete immutable bundle from the managed machine, imports it into a short-lived matching native instance, and native VICE calls `WriteSnapshot`.
- `vice_x64` and `vice_xvic` become explicit desktop runtime components for native snapshot operations. Packaging, availability diagnostics, license notices, and architecture-specific binaries are part of the capability gate.
- The native instance is a format bridge and oracle. Emulation continues in the managed machine after load.

### Production seams

- Put public, serialization-free bridge contracts and immutable state records in `ViceSharp.Abstractions`; put `ViceNativeFactory`, the native lease, envelope validation, and the C64/VIC-20 adapter implementations in `ViceSharp.Core`; put session locking, transaction orchestration, protocol error mapping, and default snapshot routing in `ViceSharp.Host.InProcess`; put the public format constant and legacy-import method in `ViceSharp.Protocol`.
- Keep reusable CPU, VIC-II/VIC-I, CIA/VIA, SID/audio, memory, cartridge, tape, and drive contracts machine-agnostic. C64/x64sc and VIC-20/xvic wiring, module selection, and restore order stay in their owning adapters.
- Keep `LockstepValidator.StageManagedFromNative` as a thin call into the production bridge until all callers migrate, then remove the duplicated staging code.

The contracts are locked before Slice 1:

```csharp
public enum ViceMachineKind { C64Sc, Vic20 }

public enum ViceMachineModel
{
    C64Pal, C64CPal, C64OldPal, C64Ntsc, C64CNtsc, C64OldNtsc, C64PalN,
    Sx64Pal, Sx64Ntsc, Pet64Pal, Pet64Ntsc, Ultimax, C64Gs, C64Jap,
    Vic20Pal, Vic20Ntsc, Vic21, Vic1001
}

public interface IViceNativeFactory
{
    ValueTask<IViceNativeLease> AcquireAsync(
        ViceMachineKind machine,
        ViceMachineModel model,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public interface IViceNativeLease : IAsyncDisposable
{
    IViceNative Native { get; }
}

public interface IViceSnapshotEnvelopeValidator
{
    ViceSnapshotEnvelope Validate(
        ReadOnlySpan<byte> payload,
        ViceMachineKind expectedMachine,
        ViceSnapshotManifest manifest);
}

public interface IViceSnapshotMachineAdapter
{
    ViceMachineKind Machine { get; }
    ViceSnapshotStateBundle CaptureManaged(IMachine machine, ViceSnapshotSessionContext context);
    ViceSnapshotStateBundle ExportNative(IViceNative native, ViceSnapshotEnvelope envelope);
    void ValidateForManaged(ViceSnapshotStateBundle state, IMachine machine, ViceSnapshotSessionContext context);
    void ApplyManaged(ViceSnapshotStateBundle state, IMachine machine);
    void ImportNative(ViceSnapshotStateBundle state, IViceNative native);
}
```

Production code accepts only the closed `ViceMachineModel` enum. `ViceNativeFactory` maps it to these canonical existing shim selectors: `C64Pal -> c64`, `C64CPal -> c64c`, `C64OldPal -> c64old`, `C64Ntsc -> c64ntsc`, `C64CNtsc -> c64cntsc`, `C64OldNtsc -> c64oldntsc`, `C64PalN -> paln`, `Sx64Pal -> sx64pal`, `Sx64Ntsc -> sx64ntsc`, `Pet64Pal -> pet64pal`, `Pet64Ntsc -> pet64ntsc`, `Ultimax -> ultimax`, `C64Gs -> c64gs`, `C64Jap -> c64jap`, `Vic20Pal -> vic20`, `Vic20Ntsc -> vic20ntsc`, `Vic21 -> vic21`, and `Vic1001 -> vic1001`. The aliases accepted by the current C shims are compatibility inputs only; production adapters emit only these canonical selectors. A model not in this enum fails before native acquisition.

The top-level immutable state contract is locked as:

```csharp
public sealed record ViceSnapshotStateBundle(
    ViceMachineKind Machine,
    ViceMachineModel Model,
    string ViceCommit,
    ViceSnapshotContainerDescriptor Container,
    ImmutableArray<ViceSnapshotModuleDescriptor> Modules,
    ulong MasterClock,
    CpuSnapshotState Cpu,
    ImmutableArray<byte> Ram,
    ImmutableArray<byte> ColorRam,
    MemoryBankingSnapshotState Memory,
    VideoSnapshotState Video,
    ImmutableArray<CiaSnapshotState> Cias,
    ImmutableArray<ViaSnapshotState> Vias,
    AudioSnapshotState Audio,
    CartridgeSnapshotState? Cartridge,
    TapeSnapshotState? Tape,
    ImmutableArray<DriveSnapshotState> Drives,
    InterruptSnapshotState Interrupts,
    ImmutableArray<MediaAttachmentFingerprint> Attachments);
```

The named subrecords are serialization-free `ViceSharp.Abstractions` records. `CpuSnapshotState` contains the architectural registers plus opcode, micro-cycle, BA-low flags, IRQ/NMI pending clocks, and delay fields; `MemoryBankingSnapshotState` contains the processor port, written direction/data, decoded banks, and RAM-under-ROM state; `VideoSnapshotState` contains the complete VIC-II or VIC-I registers and raster/fetch/display/sprite pipeline; the ordered CIA/VIA records contain ports, DDRs, counters, latches, control, TOD/serial, IRQ flags, and pipeline state; `AudioSnapshotState` contains SID or VIC oscillator/envelope/filter state; the cartridge, tape, and drive records contain their type/banks/dirty state, motor/position/sense state, and drive CPU/RAM/VIA/image/head/track/bit/write state respectively. Arrays are defensively copied into `ImmutableArray<T>`. Slice 1 freezes every leaf property name and type in the state-transfer manifest before any bridge implementation; a blank, duplicated, or unmapped leaf fails the manifest test.

`ViceSnapshotSessionContext` contains the session id, machine kind, exact profile/model selector, video standard, `SnapshotMutationGeneration`, and immutable media attachment fingerprints. `EmulatorRuntimeSession.SnapshotMutationGeneration` increments under `SyncRoot` on reset, profile/model change, media attach/detach, machine replacement, cartridge persistence mutation, and successful restore. Load captures a context before native staging and must match it again after acquiring `SyncRoot`; mismatch returns `SessionChanged` without mutation. Save captures the bundle and context atomically under `SyncRoot`; later live-machine progress does not invalidate that already captured checkpoint.

The current `NativeCpuPipelineState` fields are not treated as imported merely because they are exported. Slice 2 first preserves the current first-cycle restart behavior exactly. Slice 3 adds explicit managed execution-state fields and import hooks for every manifest-mapped native pipeline value, or records a VICE-source-backed equivalence proof for a value that cannot affect continuation. `ResetInFlightState` is not accepted as full pipeline restore.

### Error contract

All bridge failures use `ViceSnapshotCompatibilityException` with a `ViceSnapshotErrorCode`: `NativeRuntimeUnavailable`, `NativeBusy`, `UnsupportedModel`, `WrongMachine`, `UnsupportedContainerVersion`, `UnsupportedModuleVersion`, `MissingMandatoryModule`, `DuplicateMandatoryModule`, `InvalidLength`, `OversizedPayload`, `TrailingData`, `NativeReadFailed`, `StateExportFailed`, `SessionChanged`, `ManagedValidationFailed`, `ManagedApplyFailed`, `RollbackFailed`, `NativeImportFailed`, or `NativeWriteFailed`. Cancellation remains `OperationCanceledException`.

- Invalid snapshot input maps to `RpcStatus.InvalidArgument`.
- Missing native binaries or ROM prerequisites and a busy native singleton map to `RpcStatus.Unavailable`.
- A changed session/profile/attachment token maps to `RpcStatus.FailedPrecondition`.
- Apply, rollback, import, or write failures map to `RpcStatusCode.Internal` with the stable error code and safe diagnostic text. Add `RpcStatus.Internal(string)` rather than constructing ad hoc statuses.
- Save failure returns no `SnapshotDto`; restore failure returns no resumed status. No failure falls back to a proprietary format.
- If rollback fails, set the session to `Stopped`, record a `SnapshotFault` containing both the apply and rollback error codes, and prevent resume/mutation operations with `FailedPrecondition` until the session is destroyed and recreated. The emulation pump already ignores stopped sessions.

### Native ownership, timeout, and cancellation

- Export the same non-blocking ABI from both `vice_x64.dll` and `vice_xvic.dll`: `int vice_machine_try_create_model(const char *model_selector, void **machine)`. The return enum is `VICE_CREATE_OK = 0`, `VICE_CREATE_BUSY = 1`, `VICE_CREATE_INVALID_MODEL = 2`, `VICE_CREATE_INIT_FAILED = 3`, and `VICE_CREATE_OUT_OF_MEMORY = 4`; `*machine` is non-null only for `OK`. Each implementation reuses its existing initialize, model-select, rebaseline, allocation, and ownership path but returns `BUSY` immediately instead of entering `SleepConditionVariableCS(..., INFINITE)`.
- The two DLLs own independent process-local singletons and locks. At most one C64 lease and one VIC-20 lease may exist at a time, but a C64 lease and a VIC-20 lease may coexist. `ViceNativeFactory` selects the DLL by `ViceMachineKind`, polls that DLL's non-blocking export at 25 ms intervals, honors cancellation, and fails with `NativeBusy` after a fixed 10-second acquisition timeout. Tests use a fake factory and a virtual clock; no product snapshot path calls the blocking create export or waits indefinitely.
- The orchestration service owns each lease with `await using` and owns its unique temporary directory. It deletes the directory in `finally` on success, failure, or cancellation.
- Native `ReadSnapshot` and `WriteSnapshot` are non-interruptible once entered. Cancellation is checked before acquisition, before each native call, and before live mutation. If cancellation arrives during a native call, the call is allowed to finish, its staged state/output is discarded, the temporary directory is removed, and `OperationCanceledException` is returned without live mutation. Once transactional managed apply begins under `SyncRoot`, cancellation is deferred until apply or rollback completes; the operation reports the actual committed result rather than claiming cancellation after state changed.

### VSF envelope validation

`ViceVsfEnvelopeValidator` performs read-only, machine-agnostic framing validation before native VICE reads the payload. It validates the 19-byte `VICE Snapshot File\x1a` magic, the two one-byte container-version fields, and the 16-byte NUL-padded machine name. If the 13-byte `VICE Version\x1a` marker is present at offset 37, it consumes the current 21-byte VICE-version block and modules begin at byte 58; if that marker is absent, it follows pinned `snapshot_open` behavior for pre-2.4.30 files and modules begin at byte 37. Each module header is exactly 22 bytes: 16-byte NUL-padded name, one-byte major, one-byte minor, and a four-byte little-endian total module size that includes the 22-byte header. Every total size must be at least 22, checked addition must remain within `payload.Length`, and the final module must end exactly at EOF.

There is no arbitrary 64 MiB or 512-module compatibility cap. The validator operates on the already accepted `ReadOnlySpan<byte>` and bounds iteration by `payload.Length / 22`; the protocol/host's ordinary message and allocation limits apply before this method and map to `OversizedPayload`. Mandatory names and supported version ranges come only from the pinned manifest. Missing or duplicate mandatory names fail. Unknown optional modules are allowed and remain native VICE's responsibility. The validator never parses or rewrites version-specific module payloads. A separate, narrowly scoped legacy `MAINCPU` compatibility adapter handles only validated v1.2 and v1.3 input in a unique temporary copy before immutable VICE reads it: v1.2 gains the absent v1.3 and v1.4 fields, v1.3 gains only the absent v1.4 field, inclusive length is updated, and v1.4 passes through byte-for-byte. Normal current-version capture and restore remain the exact byte stream produced and consumed by native VICE.

VSF has no general checksum. Corruption coverage therefore means malformed envelope/framing/length/module-set data or payload damage that native VICE rejects. The plan does not promise detection of arbitrary payload bit flips that satisfy both structural validation and native VICE.

### Public protocol

- Add `SnapshotFormats.NativeViceVsf = "vice.native.vsf"`.
- Change `CaptureSnapshot(SessionRequest)` and its asynchronous transport to return `SnapshotDto.Format == "vice.native.vsf"` and the exact byte stream written by native VICE for supported C64 and VIC-20 sessions.
- `RestoreSnapshot` dispatches `vice.native.vsf` to the production bridge and never falls back to a proprietary payload when native restore fails.
- Add `rpc ImportLegacySnapshot(RestoreSnapshotRequest) returns (RestoreSnapshotResponse);` to `vice_sharp.v1.SnapshotService` in `emulator_host.proto`, regenerate the gRPC server/client through the existing `Grpc.Tools` build, add `SnapshotService.ImportLegacySnapshot`, and add the matching `ISnapshotService.ImportLegacySnapshotAsync` method. Reuse the existing request/response messages; no second transport DTO is introduced. Add `RpcStatus.Internal(string message) => new(RpcStatusCode.Internal, message)` to the existing status factories and use it for the locked internal-error mappings.
- Add an **Emulator Snapshot** group to the existing Avalonia Snapshot menu with `Save Snapshot...`, `Load Snapshot...`, `Quick Save`, `Quick Load`, and a `Named Slots` submenu containing slots 1 through 10, each with Save and Load. Save and Load use `.vsf` as the only default extension and filter, and both call the same snapshot service used by protocol capture/restore. Quick and named slots persist native bytes under `Environment.SpecialFolder.LocalApplicationData/ViceSharp/Snapshots/<machine-kind>/<profile-id>/quick.vsf` and `slot-01.vsf` through `slot-10.vsf`; profile ids are normalized with the existing path-safety convention. Slot saves use write-to-unique-temp plus atomic replace; missing slot loads return `NotFound`; no slot embeds a proprietary envelope or format selector.
- Do not add `CaptureSnapshotAs` as a means of making VSF secondary. An explicit format API is permitted only for diagnostics or legacy import and may not alter the canonical save result.
- Add `SnapshotService.ImportLegacySnapshot` and `ISnapshotService.ImportLegacySnapshotAsync(RestoreSnapshotRequest, CancellationToken)`, returning the existing `RestoreSnapshotResponse`. It alone accepts `vice-sharp.machine-snapshot.v2` and `vice-sharp.runtime-snapshot.v1`, restores that legacy state into the current session once, and leaves every subsequent normal save on native `.vsf`.
- Normal `RestoreSnapshotAsync` rejects either proprietary format with a descriptive migration error directing the caller to `ImportLegacySnapshotAsync`. Replay payloads remain owned by the replay service and are never accepted by either snapshot restore method.
- Internal immutable state bundles used for validation and rollback are implementation details, not a second serialized snapshot format.

This corrects a breaking divergence in the current implementation. Callers that assumed `CaptureSnapshot` returned a proprietary format must migrate to the day-one native VSF contract; they do not retain a competing default.

This plan's product and packaging scope is Avalonia desktop `win-x64`. The shared host resolves an injected `INativeSnapshotRuntimeCapability`; on a host/RID without the packaged native runtime, normal snapshot calls return `RpcStatus.Unavailable` with `NativeRuntimeUnavailable` and never fall back to a proprietary capture. No UWP or Xbox source, command, package, deployment target, or test is modified by this plan. Their historical requirements remain active and are not retired; a future explicitly scoped plan must supply a supported native runtime before those heads can offer snapshot operations. Compile coverage may prove shared interfaces still build, but UWP/Xbox behavior is not a completion claim for this plan.

### State coverage manifest

- Create `docs/requirements/oracles/vice-vsf-state-transfer-manifest.json` from pinned VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`.
- Schema version 1 contains `viceCommit`, `fixtureGeneratorVersion`, `containerFraming`, `romAllowlist`, `fixtures`, and `machines`. `containerFraming` records the 19-byte magic, two one-byte container-version fields, 16-byte machine field, current 58-byte and legacy 37-byte header variants, the optional VICE-version marker/block, the 22-byte module header, little-endian integer encoding, and the inclusive module-length rule. Each fixture records relative path, machine/model/video standard, ROM-set hashes, media profile, generation command, cycle checkpoint, file SHA-256, byte length, header variant, and ordered module name/version/inclusive-length entries. Each machine module records name, supported version range, mandatory/optional classification, VICE read/write source anchors, restore order, represented state categories, exact native export/import functions, exact managed record fields, adapter, and test ids.
- For each x64sc and xvic snapshot module, record machine, module name and version, represented state categories, native export/import functions, managed state record, managed adapter, mandatory or optional classification, and source anchors. The manifest test fails if a state field, mandatory module, native function, adapter field, or required test id is blank or duplicated.
- The manifest drives state-transfer coverage. It does not recreate the VSF parser.
- Any module or field that affects deterministic continuation remains mandatory until proven otherwise by the pinned VICE source and local Grok hostile review.

### Fixture and toolchain contract

- Both native shims build only through MSYS2 MINGW64. Replace the current in-tree/patched build behavior in `native/build-vice-shim.ps1` and add `native/build-vice-shim-xvic.ps1` so each materializes commit `cc67418cba58e71d8e70fd353e9de47199ad2075` into a separate clean source root outside `native/vice`, keeps generated and build output outside both source trees, and links only external wrapper translation units stored outside the submodule. Git Bash is not a supported compiler environment. `native/patches/vice-shim-runtime.patch` remains diagnostic legacy evidence and is never applied.
- `NativeViceSnapshotPrerequisiteTests` executes an MSYS2 MINGW64 preflight against `C:\msys64\mingw64\bin\gcc.exe`, `C:\msys64\usr\bin\make.exe`, `C:\msys64\usr\bin\autoconf`, `C:\msys64\usr\bin\automake`, `C:\msys64\usr\bin\libtool`, and `C:\msys64\mingw64\bin\pkg-config.exe`; proves the recorded gitlink, clean materialized source, and built target are exactly commit `cc67418cba58e71d8e70fd353e9de47199ad2075`; and fails instead of skipping for any source mutation, patch application, generated source, missing tool, ROM hash, shim, fixture, or manifest entry. Pre-build and post-build commit, tracked-status, untracked-status, and source-tree hash receipts must match exactly for both the clean materialization and the untouched dirty checkout.
- Keep the supplied x64sc PAL/C64C true-drive fixture at `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`; its locked SHA-256 is `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, it is 190,854 bytes, it has the current 58-byte VICE 2.0/C64SC header, and its exact 24-module sequence is the one in `FR-VSFLOCKSTEP-001`. Gate 0 read-only proof is retained in `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`.
- Add `tools/ViceSharp.SnapshotFixtureGenerator`, a local .NET console tool referencing `ViceSharp.Core`. Add Nuke `GenerateNativeSnapshotFixtures`, which first builds both shims, then uses the pinned ROM set and native `IViceNative.WriteSnapshot` at fixed checkpoints to create `ready-xvic-pal-unexpanded.vsf` (`vic20`, 100,000 cycles) and `ready-xvic-ntsc-unexpanded.vsf` (`vic20ntsc`, 100,000 cycles). Run generation twice into separate temporary directories and require byte-identical files before committing their hashes to the manifest.
- The base fixture gate is the supplied x64sc file plus both generated xvic files. Media-state fixtures are generated on demand after reverse import exists: C64 PAL with drive/tape/cartridge state in Slice 3/5 and VIC-20 PAL with RAM expansion, standard cartridge, tape, and drive state in Slice 4/5. Their generator arguments, input media hashes, checkpoint cycles, output hashes, and module lists are appended to the same manifest before their slice can pass.
- ROMs and media inputs are never committed or packaged. The generator resolves them through the existing ViceSharp ROM acquisition/configuration path and rejects any base-fixture ROM not in this exact subset of the committed `EnsureCiRomRoot` allowlist: `C64/basic-901226-01.bin` = `89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D`; `C64/kernal-901227-03.bin` = `83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721`; `C64/chargen-901225-01.bin` = `FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420`; `DRIVES/dos1541-325302-01+901229-05.bin` = `D1D45AFB46FD4E2B48D93CA367B889D75654A3B7ACF73044E51F6D880C09369E`; `VIC20/basic-901486-01.bin` = `8BE2D882E0EEB1C6923A43660236EA69D43CA8A3BD0613B1B6C910601584FBED`; `VIC20/kernal.901486-07.bin` = `FF6D1416B91A71F5240A5BB8ADC9FDC1B955374C48322CA47B3E436334F168FB`; `VIC20/kernal.901486-06.bin` = `8FC94BD895F488AC0AE1A52ABDC32988CD63C777770B89A9D415513098535CC2`; `VIC20/chargen-901460-03.bin` = `BDA280726CFE069CA70EF5E2F2D95A29F69D3950C859A12E6979464963FCF502`; and `DRIVES/dos1540-325302+3-01.bin` = `A966E3FF6FD2DB7B8F6D57E4D105C589A92E88A6705201215604AC864CC3C27F`. Media fixture generation may not begin until every input medium has an exact SHA-256 entry committed to the manifest; discovering a hash during the passing generation run is not sufficient.

### Transactional load

1. Reject any non-`vice.native.vsf` format at the normal restore boundary; proprietary formats are directed to explicit legacy import.
2. Capture `ViceSnapshotSessionContext`, validate the envelope and manifest against the expected machine, and write the unmodified payload to a unique temporary file.
3. Acquire the matching native lease within 10 seconds and call native `ReadSnapshot`. Native failure occurs before managed mutation.
4. Export the complete native state into an immutable bundle, validate manifest coverage, and release the native lease.
5. Check cancellation, acquire `EmulatorRuntimeSession.SyncRoot`, and recapture the session context. If the generation/profile/model/video/media token changed, return `SessionChanged` without mutation.
6. Capture a complete managed rollback bundle through the same adapter while still under the lock.
7. Apply in the manifest's pinned VICE restore order: memory and banking; CPU architectural and execution state; video pipeline; CIA/VIA; SID/VIC audio; cartridge, tape, and drives; interrupt lines and master clock; then session attachment/status projection.
8. If apply fails, restore the rollback bundle before releasing the lock. If rollback fails, stop and fault the session as defined by the error contract.
9. Increment `SnapshotMutationGeneration` and publish resumed status only after the complete apply succeeds.

### Transactional save

1. Resolve the supported C64/x64sc or VIC-20/xvic adapter and verify native runtime and ROM capability.
2. Acquire `EmulatorRuntimeSession.SyncRoot`, capture the immutable complete managed bundle and session context at one cycle, then release the lock. Do no native work while holding the session lock.
3. Check cancellation, acquire the matching native lease within 10 seconds, reset it to the exact model, and import every mandatory manifest field in the manifest's restore order.
4. Reject incomplete import before `WriteSnapshot`; then call native `WriteSnapshot` to a unique temporary path.
5. Read the exact native-produced bytes, validate their envelope and machine identity without rewriting them, and return `SnapshotDto("vice.native.vsf", capturedClock, payload)` only after complete success.
6. On import, write, validation, or cancellation failure, return no payload and remove the temporary artifact. No path emits a proprietary substitute.

### Deterministic continuation

- Compare CPU and master-clock phase, bus-visible memory, banking, raster and video pipeline, IRQ/NMI state, timers and serial state, audio oscillator, envelope and filter state, media position, cartridge state, and drive state at the restore point and throughout the lockstep window.
- Fail on the first divergence with cycle, subsystem, field, managed value, native value, snapshot hash, and state-transfer manifest revision.

## Current hostile-review state

The fresh local Grok review in `docs/receipts/hv/20261003T144113Z-vsf-canonical-plan-local-grok.verdict.md` returned DISAGREE, accuracy 89, completeness 84, pass 4, fail 8, unknown 7. It did not authorize implementation. This revision responds to every numbered finding:

- F01 remained PASS: the baseline accurately distinguishes architectural-register restart from real native pipeline import.
- F02 now closes the model vocabulary and immutable record types, defines the protobuf RPC and generated transport, locks the status factory, specifies the exact Avalonia file/quick/named-slot surface and persistence, and defines the unsupported-host boundary without adding UWP/Xbox work.
- F03 now binds generator inputs to exact committed ROM hashes and retains local evidence for the supplied fixture, pinned VICE commit, MSYS2 paths, and both shim selector vocabularies.
- F04 now gives the Nuke compatibility target only to Slice 1 and removes the unrelated quarantined `ParityTest` suite from the VSF exit.
- F05 now defines an exact non-blocking ABI in both DLLs and independent C64/xvic singleton/lease behavior.
- F06 now covers current and pre-2.4.30 headers, little-endian inclusive module lengths, exact EOF, and removes invented 64 MiB/512-module compatibility limits.
- F07 now stages loose native files after `DotNetPublish` and before WiX, uses the verified recursive PE import closure, and retains its local import evidence.
- Generated MCP wiki mappings are regenerated through `workflow.requirements.generateDocument`; local traceability and live MCP readback must be retained with the next review.

These are remediation claims about the plan and evidence only. No failure is considered closed by this prose. Gate 0 remains open until a later fresh local Grok review returns AGREE at 98/98 or higher and the operator explicitly approves the corrected plan.

## BDPv4 execution plan

Every slice has four hard stops. Work and review remain local. Operator directive, 2026-10-07: alternate local Grok `grok-4.7` at `xhigh` and local Claude Code `claude-opus-5-5` at `max` for successive reviews, including remediation re-reviews. The current Grok mock-green correction review finishes first; Opus is next. Use the current handoff and whole-goal plan for rotation and local launch receipts. Future "local Grok" gate references below mean the next designated reviewer, not a Grok-only restriction; historical receipts retain their real identity. Do not use fallback models, cloud execution, reviewer subagents, or artificial turn/time limits. Both reviewers retain the same A/B/C/D scope, 98/98 threshold, JSONL receipts, and complete primary MCP verdict readback.

Operator correction, 2026-10-07: "RED tests are not acceptable. BDPv4 requires mocking data to prove tests work to green before starting code." The sequence below corrects the existing process, not the product requirements. No expected-red production run is an acceptable prerequisite or phase exit. Mock data validates test contracts only and never counts as native VICE compatibility or parity evidence.

1. **A - requirement checkpoint:** map every named test to current FR/TR/TEST criteria, update MCP and canonical docs, retain readback, and obtain local Grok AGREE at 98/98 or higher before writing tests.
2. **B - valid-mock green:** write the AC-covering unit tests, prove them green against valid mocked data, retain exact commands and passing results, and obtain local Grok AGREE at 98/98 or higher. Do not start product implementation.
3. **C - discriminating-mock green:** cover failure, cancellation, ordering, and rollback with fakes/test doubles. Deliberately incorrect mock data must be caught by the assertions; negative tests assert the expected rejection so the entire mock-validation suite remains green with zero failed and zero skipped tests. Retain the cases and results and obtain local Grok AGREE at 98/98 or higher before product implementation. Do not require any real product assertion to remain red.
4. **D - product green:** only after B and C pass, implement the smallest product change. Run the full applicable suite for the current increment and all prior work, including the entire cumulative `Category=NativeViceSnapshot` suite through the current slice, with zero failed and zero skipped tests. Update MCP, TODO checkpoints, receipts, source anchors, and hashes, then obtain local Grok AGREE at 98/98 or higher before starting the next slice.

Slice 1 adds Nuke target `NativeSnapshotCompatibilityTest` with parameter `--native-snapshot-through-slice N`. Every later slice runs that target, which always includes all tests from Slice 1 through `N`; focused filters are diagnostic only and never replace the cumulative gate. No slice may omit earlier tests from its exit evidence.

### Gate 0: Corrected baseline, requirements, and plan review

- Add no product code.
- Prove the current managed VSF path, its exact transferred fields, its missing fields, and its production wiring gap with source anchors.
- Reconcile the three full-capability records with the recovered FR-VSFLOCKSTEP-001, TR-VSFLOCKSTEP-RESUME-001, and TEST-VSFLOCKSTEP-001 records. No legacy record is retired.

Validation:

```pwsh
.\tools\check_requirement_traceability.ps1
git diff --check
```

Exit:

- MCP readback proves all six requirements and their acceptance criteria remain pending.
- `workflow.requirements.generateDocument` with `format: wiki` and `docType: all` has regenerated both local wiki projections from those records, and the recovered/full-capability mappings agree.
- `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json` retains the six-record counts, zero satisfied criteria, both four-link FR mappings, and generated wiki timestamp.
- `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md` proves the fixture, native commit, selector vocabulary, MSYS2 paths, and current PE dependency closure.
- Local Grok hostile plan review returns AGREE at 98/98 or higher.
- The operator explicitly approves this corrected plan.

### Slice 1: Make VSF prerequisites, immutable builds, and legacy resume non-skippable

Tests first:

- `NativeViceSnapshotPrerequisiteTests.C64AndVic20Libraries_AreBuiltFromPinnedViceCommit`
- `NativeViceSnapshotPrerequisiteTests.RequiredFixtures_ArePresentAndHashPinned`
- `ViceSnapshotStateTransferManifestTests.Manifest_CoversEveryDeterministicModule`
- `NativeViceSnapshotPrerequisiteTests.Msys2Toolchain_HasEveryRequiredCommand`
- `NativeViceSnapshotFixtureGeneratorTests.Regeneration_IsByteDeterministic`
- `NativeViceSnapshotLegacyMainCpuCompatibilityTests.V12_InsertsV13AndV14FieldsBeforeBaLowFlags`
- `NativeViceSnapshotLegacyMainCpuCompatibilityTests.V13_InsertsOnlyV14JammedBeforeBaLowFlags`
- `NativeViceSnapshotLegacyMainCpuCompatibilityTests.V14_PassesThroughByteForByte`
- `NativeViceSnapshotLegacyMainCpuCompatibilityTests.MalformedOrUnsupportedLayout_FailsClosed`
- `ImmutableViceBuildTests.BuildInputsOutputsAndGeneratedFiles_StayOutsideNativeVice`
- `ImmutableViceBuildTests.PinnedSourceTree_IsIdenticalBeforeAndAfterBuild`
- Update existing VSF acceptance tests so missing native libraries or fixtures fail instead of skip.

Planning gap to verify: xvic fixture coverage and the state-transfer manifest are absent, existing tests can skip, the build scripts still patch or generate into `native/vice`, and no external adapter preserves v1.2/v1.3 `MAINCPU` resume against the unmodified v1.4 reader. This is not a requirement to run or retain failing tests.

Mock gate: a fake prerequisite provider supplies a valid baseline, then independently omits or corrupts the library, MSYS2 tool, fixture, commit, ROM hash, media hash, manifest field, output hash, clean source materialization, or legacy normalization capability. Tests assert acceptance of valid data and rejection of each defect. Synthetic v1.2, v1.3, v1.4, malformed, duplicate-module, and overflow cases pin the adapter contract. All mock tests pass before implementation; real xvic fixture and immutable-build evidence is required at the implementation exit, not fabricated by these mocks.

Smallest change: add mandatory fixture provisioning, hashes, the state-transfer manifest, clean out-of-tree native materialization/build, and the external legacy `MAINCPU` compatibility adapter. Do not change code under `native/vice`; do not apply `native/patches/vice-shim-runtime.patch`; do not broaden the adapter beyond the versioned fields required by `TR-VSFLOCKSTEP-RESUME-001`.

Validation:

```pwsh
& .\native\build-vice-shim.ps1
& .\native\build-vice-shim-xvic.ps1
.\build.ps1 GenerateNativeSnapshotFixtures --configuration Release
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 1
```

### Slice 2: Move existing C64 VSF staging into production

Tests first:

- `C64ViceSnapshotStateBridgeTests.ExportsTheExistingStageManagedFromNativeFieldSet`
- `C64ViceSnapshotStateBridgeTests.ValidatesBeforeManagedMutation`
- `C64ViceSnapshotStateBridgeTests.ApplyFailure_RestoresOriginalManagedState`
- `C64ViceSnapshotStateBridgeTests.RollbackFailure_StopsAndFaultsSession`
- `C64ViceSnapshotStateBridgeTests.LockstepValidator_UsesProductionBridge`
- `ViceNativeFactoryTests.BusySingleton_TimesOutInTenSecondsWithoutIndefiniteNativeWait`
- `ViceNativeFactoryTests.CancellationDuringAcquisition_ReleasesAllResources`

Planning gap to verify: state transfer exists only as static test-harness code.

Mock gate: fake native factory, lease, clock, native instance, and managed target reproduce the current RAM, written processor-port values, CPU architectural-register restart, VIC-II, and CIA transfer. Negative tests assert detection of missing or incorrect transfer data. Failure injection proves normal rollback, rollback-failure faulting, 10-second timeout, and acquisition cancellation. All these tests pass before implementing the real production bridge.

Smallest change: extract current behavior without semantic change into production seams, then delegate the old test helper to the production bridge.

Validation:

```pwsh
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 2
```

### Slice 3: Complete C64 state transfer and 100,000-cycle x64sc parity

Tests first:

- `C64ViceSnapshotStateBridgeTests.TransfersEveryMandatoryManifestField`
- `C64ViceSnapshotCompatibilityTests.RestoredState_MatchesNativeFieldForField`
- `C64ViceSnapshotLockstepTests.RestoredCheckpoint_MatchesX64ScFor100000Cycles`
- `C64ViceSnapshotPipelineTests.NativePipelineFields_AreImportedOrHavePinnedViceEquivalenceProof`
- `C64ViceSnapshotMediaTests.CartridgeTapeAndDriveState_MatchManifest`

Planning gap to verify: native opcode/micro-cycle and interrupt pipeline state, SID internals, cartridge, tape, drive, or another mandatory manifest field lacks real managed import; resetting in-flight state does not meet the pipeline contract.

Mock gate: fake native and managed adapters cover every C64 manifest field and enforce manifest restore order. Negative tests assert rejection of deliberately omitted pipeline/media fields and incorrect restore order. All mock tests pass before implementation. The real 100,000-cycle gate remains required for the implementation exit and cannot be satisfied by mock results.

Smallest change: add only missing native export/import functions and managed capture/injection hooks exposed by the manifest. Keep wiring in the C64 adapter.

Validation:

```pwsh
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 3
```

### Slice 4: Complete VIC-20 state transfer and 100,000-cycle xvic parity

Tests first:

- `Vic20ViceSnapshotStateBridgeTests.TransfersEveryMandatoryManifestField`
- `Vic20ViceSnapshotCompatibilityTests.RestoredState_MatchesNativeFieldForField`
- `Vic20ViceSnapshotLockstepTests.RestoredCheckpoint_MatchesXvicFor100000Cycles`
- `Vic20ViceSnapshotTransactionTests.ApplyFailure_RestoresOriginalManagedState`
- `Vic20ViceSnapshotMediaTests.ExpansionCartridgeTapeAndDriveState_MatchManifest`

Planning gap to verify: no production VIC-20 bridge stages VIC-I, dual VIA, VIC audio, VIC-20 banking, cartridge, tape, and drive state.

Mock gate: fake xvic and VIC-20 adapters prove full field transfer, validation, VICE restore order, context-token revalidation, normal rollback, and rollback-failure faulting. Negative cases assert rejection of incomplete state, incorrect order, and stale context tokens. All mock tests pass before implementation; real xvic compatibility is required afterward.

Smallest change: add the VIC-20 adapter using shared state records and existing `ViceNative.Xvic` snapshot operations. Keep VIA shared and board wiring in the VIC-20 layer.

Validation:

```pwsh
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 4
```

### Slice 5: Write native VSF from managed C64 and VIC-20 state

Tests first:

- `C64NativeViceSnapshotWriteTests.ManagedState_WritesVsfAcceptedByX64Sc`
- `Vic20NativeViceSnapshotWriteTests.ManagedState_WritesVsfAcceptedByXvic`
- `NativeViceSnapshotWriteTests.ImportFailure_DoesNotEmitArtifact`
- `NativeViceSnapshotWriteTests.CancellationDuringNativeWrite_DiscardsTemporaryArtifact`
- `NativeViceSnapshotWriteTests.CaptureLock_IsReleasedBeforeNativeAcquisition`

Planning gap to verify: native `WriteSnapshot` exists, but complete managed-to-native import does not.

Mock gate: fake native instances record every imported manifest field and prove `WriteSnapshot` is called only after complete import, that the session lock is released before native acquisition, and that cancellation/failure removes temporary artifacts and emits no payload. Negative tests assert detection of omitted fields, early writes, held locks, and leaked artifacts. All mock tests pass before implementation; both real native-load assertions must pass at the implementation exit.

Smallest change: add reverse state transfer through the same adapters and let native VICE serialize `.vsf`. Do not add a second container writer.

Validation:

```pwsh
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 5
```

### Slice 6: Make native VSF the canonical product snapshot format

Tests first:

- `SnapshotServiceNativeViceTests.DefaultCapture_ReturnsViceWrittenVsf`
- `SnapshotServiceNativeViceTests.RestoreNative_UsesProductionBridge`
- `SnapshotServiceNativeViceTests.NativeFailure_NeverFallsBackToLegacyFormat`
- `SnapshotServiceNativeViceTests.LegacyImport_IsExplicitAndCannotBecomeSaveDefault`
- `GrpcSnapshotNativeViceTests.ImportLegacySnapshot_RpcUsesExistingRestoreMessages`
- `DesktopSnapshotNativeViceTests.FileQuickAndNamedSlots_UseNativeVsf`
- `DesktopSnapshotNativeViceTests.QuickAndTenNamedSlots_UseAtomicLocalVsfFiles`
- `SnapshotServiceNativeViceTests.UnsupportedHost_ReturnsUnavailableWithoutLegacyFallback`
- `GrpcSnapshotNativeViceTests.DefaultCapture_PreservesNativeFormatCycleAndPayload`
- `NativeViceSnapshotPackagingTests.WinX64Publish_ContainsNativeRuntimeAndViceLicense`
- `NativeViceSnapshotPackagingTests.WinX64Publish_ContainsNoRomImages`
- `NativeViceSnapshotInstalledSmokeTests.DefaultC64AndVic20CaptureRestore_UsesVsf`

Planning gap to verify: the host, desktop commands, and slots expose or default to proprietary formats.

Mock gate: fake bridge tests canonical default dispatch, file and slot routing, explicit legacy import isolation, cancellation, missing runtime components, wrong machine, unsupported RID, and no fallback. Valid mock data is accepted and deliberately incorrect dispatch or fallback data is rejected. A synthetic publish directory proves the package-content validator accepts the valid package and rejects each missing DLL/license and any bundled ROM. All mock tests pass before implementation; real publish and installed-app evidence is required afterward.

Smallest change: replace proprietary default capture and restore routing with canonical `vice.native.vsf`, add the locked protocol RPC, route the specified Avalonia file and slot commands through the same service, isolate any retained legacy reader behind explicit import, and package native x64sc/xvic runtime components for the supported desktop target. Do not modify UWP or Xbox source, commands, packages, deployment targets, or tests; unsupported hosts receive the locked unavailable result from the shared service.

Packaging is locked to the repository's current MSI RID, `win-x64`, for this plan. New local Nuke target `BuildNativeSnapshotRuntime` builds both shims and verifies their recursive PE import closure but does not write into the publish directory. Inside `PublishMsi`, immediately after its existing `DotNetPublish` call and before pruning or WiX harvest, `StageNativeSnapshotRuntime(AvaloniaPublishDir)` copies the verified loose files beside `ViceSharp.Avalonia.exe`: `vice_x64.dll`, `vice_xvic.dll`, `libstdc++-6.dll`, `libgcc_s_seh-1.dll`, `libwinpthread-1.dll`, and `zlib1.dll`. Because staging occurs after single-file publish, these six files remain external and cannot be overwritten or absorbed by `IncludeNativeLibrariesForSelfExtract`. `libiconv-2.dll` is not staged unless a future rebuilt PE import closure proves it is required. The validator recursively walks imports, permits only the documented Windows system DLL allowlist outside the staged set, and fails on every missing, extra, or unresolved non-system import. The same post-publish step stages VICE GPL/copyright/source-offer material under `licenses/VICE/` with the pinned commit and upstream source URL.

No copyrighted ROM is bundled. Runtime capability uses the existing ViceSharp ROM acquisition/configuration path; absent or hash-mismatched C64/VIC-20 ROMs return `NativeRuntimeUnavailable` with a direct local remediation message. Any RID other than `win-x64` fails the native-snapshot packaging target with an explicit unsupported-target diagnostic; it is never silently presented as native-VSF capable. The separate open `PLAN-RASPBIAN-001` owns future ARM Linux/Raspbian packaging.

Package proof has two levels: inspect the publish directory and harvested MSI content for the exact runtime/license set and ROM exclusion; then run `PublishMsi`, `InstallMsi`, and a local installed-app smoke that exercises default capture and restore for one C64 and one VIC-20 session, verifies `SnapshotDto.Format == "vice.native.vsf"`, VSF magic/machine identity, and successful restore. Installation is a local gate and may require the normal UAC elevation already implemented by `InstallMsi`.

Validation:

```pwsh
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 6
.\build.ps1 PublishMsi --configuration Release --msi-runtime-identifier win-x64
.\build.ps1 InstallMsi --configuration Release --msi-runtime-identifier win-x64
.\build.ps1 NativeSnapshotInstalledSmoke --configuration Release
```

### Slice 7: Bidirectional round trip, corruption matrix, and final gates

Tests first:

- `NativeViceSnapshotRoundTripTests.X64ScToManagedToX64Sc_PreservesStateAndContinuation`
- `NativeViceSnapshotRoundTripTests.XvicToManagedToXvic_PreservesStateAndContinuation`
- `NativeViceSnapshotRoundTripTests.ManagedC64ToX64ScToManaged_PreservesStateAndContinuation`
- `NativeViceSnapshotRoundTripTests.ManagedVic20ToXvicToManaged_PreservesStateAndContinuation`
- `NativeViceSnapshotNegativeTests.InvalidVsf_NeverMutatesManagedMachine`
- `NativeViceSnapshotNegativeTests.DuplicateMandatoryModule_IsRejectedBeforeNativeRead`
- `NativeViceSnapshotNegativeTests.StructurallyValidPayloadCorruption_IsDelegatedToNativeVice`
- `NativeViceSnapshotTransactionTests.SessionChangedBeforeApply_IsRejectedWithoutMutation`

Required coverage: all four round trips, the envelope-validation matrix, session-generation revalidation, and rollback/native-import failure behavior. Validate the unit-test contracts against mocks before final implementation; do not require failing integration tests as a gate.

Mock gate: a fake envelope validator/native factory/lease/adapter runs valid round-trip data and the complete failure matrix. Tests prove exact error-code mapping, zero mutation before apply, rollback behavior after apply starts, faulted-session behavior after rollback failure, no legacy fallback, no artifact on save failure, and optional-unknown-module pass-through. Negative tests assert detection of deliberately incorrect results or side effects. All mock tests pass before implementation; all four real round trips remain mandatory afterward.

Negative matrix: wrong machine; unsupported container or mandatory module version; manifest-invalid, missing, or duplicate mandatory module; truncated header or module; invalid or oversized length; trailing data; native read failure; state export, context revalidation, managed validation, apply, rollback, managed-to-native import, or native write failure. Arbitrary payload corruption is required to fail only when the envelope validator or native VICE detects it.

Smallest change: close only exposed gaps and add no second owner or duplicate definition for the Slice 1 `NativeSnapshotCompatibilityTest` target.

Validation:

```pwsh
.\build.ps1 Compile
.\build.ps1 Test
.\build.ps1 NativeSnapshotCompatibilityTest --configuration Release --native-snapshot-through-slice 7
.\tools\check_requirement_traceability.ps1
git diff --check
```

Every command must exit 0. Every executed test command in this gate, including the broad repository `Test` regression and the acceptance-bearing `Category=NativeViceSnapshot` compatibility target, must report zero failed, zero skipped, and zero aborted tests. Focused deterministic/lockstep cases are part of the compatibility target, not a substitute for the full regression command. Pre-existing failures or skips remain blockers; reporting them does not waive them or make this gate pass. Retain TRX, diagnostic logs, snapshot hashes, first-divergence reports, native output, and binary hashes.

## Final hostile validation

The next designated local reviewer attacks every AC in the six current and recovered VSF requirements, preservation of existing managed support, native VSF as the canonical default across public and desktop paths, explicit isolation of any legacy importer, complete C64 and VIC-20 transfer, native load and save, 100,000-cycle x64sc/xvic lockstep, transactionality, bounded native acquisition, cancellation, corruption limits, fixture provenance, installed packaging, workspace rules, BDPv4 phase order, and every claimed done state. No cloud runner, hosted agent, web reviewer, or remote CI is used. The launch has no turn cap or artificial time limit.

## Approval boundary

This plan restores the day-one native VSF snapshot contract while preserving recovered legacy requirements. The change is intentionally breaking relative to the current divergent proprietary default. Production implementation stops here until the operator explicitly approves this corrected `PLAN-VSFLOCKSTEP-001`. Approval authorizes Slice 1 only. Each later slice still requires its prior hostile phase gate.
