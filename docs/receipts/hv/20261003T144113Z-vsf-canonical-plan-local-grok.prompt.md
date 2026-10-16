# Local Grok hostile validation: PLAN-VSFLOCKSTEP-001 Gate 0 remediation

You are the independent hostile reviewer. Work read-only in the local checkout `F:\GitHub\vice-sharp`. Do not edit files, requirements, TODOs, session logs, git state, or application state. Do not use web search, remote repositories, subagents, or cloud CI. Inspect the current working tree, including uncommitted files, and ground every conclusion in local file/source evidence.

The operator's governing requirement is not negotiable:

- Native VICE `.vsf` has been the actual ViceSharp snapshot format requirement since day one.
- `.vsf` must be the canonical default for all supported C64/x64sc and VIC-20/xvic public and desktop snapshot paths. It is not optional, secondary, or a peer beside proprietary formats.
- Managed VSF resume support has existed for months. The plan must preserve and productionize it rather than pretend it is new.
- Native VICE must remain the actual version-specific VSF reader and writer and the behavioral/file oracle.
- UWP and Xbox implementation are out of scope, but their historical requirements are not retired.
- No product work, requirement, TODO, or plan step is complete. Gate 0 requires operator approval after this review.

Primary review packet:

- `docs/plans/PLAN-VSFLOCKSTEP-001.md`
- `docs/requirements/functional/FR-Snapshot.md` (`FR-SNP-005` and recovered `FR-VSFLOCKSTEP-001`)
- `docs/requirements/technical/TR-Native-Vice-Snapshot.md` (`TR-LOCKSTEP-VSF-001` and recovered `TR-VSFLOCKSTEP-RESUME-001`)
- `docs/requirements/test/TEST-Requirements.md` (`TEST-SNP-002` and recovered `TEST-VSFLOCKSTEP-001`)
- `HANDOFF.md`, especially the 2026-10-03 snapshot correction and operator contract
- Prior completed review: `docs/receipts/hv/20261003T133142Z-vsf-canonical-plan.response.jsonl`

Independently inspect the relevant source instead of trusting the plan's claims:

- `tests/ViceSharp.TestHarness/LockstepValidator.cs`
- `tests/ViceSharp.TestHarness/SnapshotResumeSpikeTests.cs`
- `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`
- `src/ViceSharp.Abstractions/IViceNative.cs`
- `src/ViceSharp.Chips/Cpu/Mos6502.cs` and `Mos6502.ExecutionImage.cs`
- `src/ViceSharp.Chips/Snapshots/C64ChipExecutionImage.cs`
- `src/ViceSharp.Core/ViceNative.cs` and `ViceNative.Xvic.cs`
- `src/ViceSharp.Host.InProcess/Services/SnapshotServiceHost.cs`
- `src/ViceSharp.Host.InProcess/Runtime/EmulatorRuntimeSession.cs`
- `src/ViceSharp.Protocol/ProtocolContracts.cs`
- `native/vice-shim.c`, `native/vice-shim-vic20.c`, `native/build-vice-shim.ps1`, `native/build-vice-shim.sh`, and `native/build-vice-shim-xvic.sh`
- pinned native VICE snapshot sources under `native/vice/vice/src/`
- `build/Build.cs` `PublishMsi` and `InstallMsi`

Re-test every previous failure; do not close one just because the plan says it is addressed:

1. F01 current CPU baseline accuracy: distinguish architectural-register restart from actual native pipeline import.
2. F02 decision completeness: exact interfaces/signatures, projects, ownership/lifetime, state shape, model/config support, errors, restore/save order, and protocol boundary.
3. F03 executable prerequisites: exact fixture matrix/provenance/hash/generation, manifest schema, pinned commit/ROM/media hashes, and a real MSYS2 xvic toolchain path.
4. F04 BDPv4 phase stops: requirement, expected-red, mock, and product-green stops with fresh review and cumulative earlier-slice validation.
5. F05 native/session lifecycle: bounded acquisition, native singleton behavior, cancellation during non-interruptible calls, save locking, session-change revalidation, rollback, rollback failure, artifact cleanup.
6. F06 corruption contract: honest VSF integrity limits, generic envelope validation without replacing VICE module parsing, duplicate/missing mandatory modules, optional unknown modules, and exact native output preservation.
7. F07 packaging: supported RID, exact native dependency/license/ROM policy, unsupported-target behavior, publish/MSI content checks, InstallMsi path, and installed C64/VIC-20 default snapshot smoke.

Also attack these plan-wide assertions:

- All six current/recovered requirements remain active, mapped, pending, and have acceptance criteria appropriate to validate the requirement.
- `FR-VSFLOCKSTEP-001` accurately describes the committed fixture, including module count/list and SHA-256.
- Native VSF is canonical across normal capture, restore, file, quick-slot, and named-slot paths with no opt-in selector and no proprietary fallback.
- The two proprietary formats are explicit import-only migration inputs; replay and internal rollback bundles are not peer snapshot formats.
- The plan requires native load, native write, both round-trip directions, complete C64/VIC-20 state, and at least 100,000 cycles of x64sc/xvic deterministic continuation with zero skipped prerequisites.
- No language implies product completion, requirement satisfaction, or implementation authorization.

Return one complete Markdown verdict with:

- `OverallVerdict: AGREE` or `OverallVerdict: DISAGREE`.
- Integer `AccuracyScore` and `CompletenessScore` from 0 to 100.
- `PassCount`, `FailCount`, and `UnknownCount`.
- A numbered finding for every failure or unknown with exact file/line/source evidence and the minimum required remediation.
- A direct PASS/FAIL/UNKNOWN answer for each of F01-F07 and each plan-wide assertion above.
- A final machine-readable one-line JSON object containing the same verdict, scores, and counts.

AGREE is permitted only if every applicable item passes, no blocking failure or unknown remains, and both scores are at least 98. Otherwise return DISAGREE and enumerate every problem; do not stop after the first one.
