# ViceSharp Claude Handoff - 2026-06-14

## Start Here

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `codex/x64sc-d64-30s-lockstep`
- Current HEAD at handoff write: `8eb4c51`
- GitHub remote: `github=https://github.com/sharpninja/vice-sharp.git`
- Azure remote: `origin=https://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp`
- Read `AGENTS-README-FIRST.yaml` first. It says Claude must use `mcpserver-claude-code-plugin`, with fallback root hint `F:\GitHub\mcpserver-claude-code-plugin`.
- There is no root `AGENTS.md` file in this checkout at this handoff. Use the marker file and the user/developer instructions from the active session.
- Treat the worktree as intentionally dirty. Do not revert broad changes unless the user explicitly asks.

## Current User Request Context

The user asked to remediate audit results as a requirement for Iteration 1 completion using BDPv4, then asked Codex to complete the plan. The completed scope is `ARCH-CHIPGLUE-001`: audit all chip implementations for machine-specific glue and move that glue to the owning machine/device definition.

The user was explicit about the architecture rule:

- Shared chip implementations must not contain machine-specific glue.
- The VIA chip must be one common implementation.
- C1541/VIC-20/etc. wiring belongs in the owning machine or device adapter, not in the chip.
- Drive-specific glue belongs in drive machine/device implementation, not attached to VIA.

## What Is Done

The code remediation and requirement closure are implemented in the dirty worktree.

Core boundary changes in the worktree include:

- New Core-owned device/runtime helpers:
  - `src/ViceSharp.Core/C1541DriveMechanismDevice.cs`
  - `src/ViceSharp.Core/C1541IecInterfaceDevice.cs`
  - `src/ViceSharp.Core/C64Cia2InterfaceDevice.cs`
  - `src/ViceSharp.Core/IecBus.cs`
  - `src/ViceSharp.Core/IecDrive.cs`
  - `src/ViceSharp.Core/IecD64Attachment.cs`
  - `src/ViceSharp.Core/Datasette.cs`
  - `src/ViceSharp.Core/StandardCartridgeImage.cs`
  - `src/ViceSharp.Core/StandardCartridgeSize.cs`
  - `src/ViceSharp.Core/Input/*`
  - `src/ViceSharp.Core/Media/*`
- Removed or moved out of `src/ViceSharp.Chips`:
  - fake/legacy IEC stubs: `DiskController`, `Mos6502DiskCpu`
  - duplicate CIA stub: `Interface/Cia6526`
  - legacy video stub: `Video/VicII`
  - IEC drive/runtime helpers
  - C64 input/VKM helpers
  - Datasette runtime helper
  - media capture helpers
  - standard cartridge mapping helpers
- Shared chip code now keeps generic chip behavior only:
  - `Via6522` exposes generic registers/pins, no 1541 address/window defaults.
  - `Mos6526` no longer owns C64 CIA1/CIA2 defaults or PAL TOD cadence.
  - `Mos6502` no longer owns C64 VIC `$D016` timing policy.
  - SID defaults are supplied by machine construction rather than chip core.
  - PLA processor-port reset policy lives in C64 assembly.
  - VIC-bank translation lives in `C64MemoryMap`.

Requirement and handoff docs updated:

- `docs/requirements/technical/TR-System-Core.md`
  - Added AC 6-10 for chip-glue ownership, inventory, common VIA, moved helpers, and validation.
- `docs/requirements/test/TEST-Requirements.md`
  - Added canonical `TEST-ARCH-CHIPGLUE-001`.
- `docs/requirements/traceability/Phase1-Open-Todo-AC-Matrix-2026-06-08.md`
  - Added `ARCH-CHIPGLUE-001`, `TR-SYSTEM-CORE-001`, and `TEST-ARCH-CHIPGLUE-001`.
- `docs/requirements/traceability/TR-to-Design-Decision-Map.md`
  - Added `DD-SYSCORE-003`: shared chips model reusable chip behavior only; machine/device glue lives in Core.
- `docs/requirements/traceability/ARCH-CHIPGLUE-001-Chip-Audit-2026-06-12.md`
  - New audit artifact with inventory, VICE separation reference, moved/retired items, and validation evidence.
- `README.md`
  - Iteration 1 dashboard includes the chip/package boundary audit.
- `docs/handoff.md`
  - Added a 2026-06-12 Iteration 1 remediation addendum.
- `tests/ViceSharp.TestHarness/Architecture/ChipGlueBoundaryTests.cs`
  - New source-boundary tests with direct `TR-SYSTEM-CORE-001 / TEST-ARCH-CHIPGLUE-001 / ARCH-CHIPGLUE-001` citation.

## Validation Already Run

All commands below were run by Codex in this workspace before writing this handoff.

Traceability:

```pwsh
.\tools\check_requirement_traceability.ps1
```

Result: exit 0. Output reported 167 canonical IDs, 97 referenced canonical IDs, 70 missing from `src`/`tests`, and 75 noncanonical references. This historical backlog is reported but not a failure without `-FailOnMissing` or `-FailOnNonCanonical`.

XMLDOC gate:

```pwsh
dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~XmlDocsConventionTests" --logger "console;verbosity=minimal"
```

Result: 1 passed, 0 failed, 0 skipped.

Consolidated chip-glue focused gate:

```pwsh
dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~ChipGlueBoundaryTests|FullyQualifiedName~C64MemoryMap|FullyQualifiedName~ProcessorPortTests|FullyQualifiedName~Cia|FullyQualifiedName~Via6522|FullyQualifiedName~Sid|FullyQualifiedName~VicII|FullyQualifiedName~VideoRenderer|FullyQualifiedName~IecDriveMotorRampTests|FullyQualifiedName~IecTimingTests|FullyQualifiedName~StorageRuntimeTests|FullyQualifiedName~StandardCartridgeTests|FullyQualifiedName~C64JoystickPortTests|FullyQualifiedName~C64VkmKeyboardTests|FullyQualifiedName~Datasette|FullyQualifiedName~TapImageTests|FullyQualifiedName~WavAudioRecorderTests|FullyQualifiedName~RecordingAudioBackendTests|FullyQualifiedName~FrameSequenceCaptureTests" --logger "console;verbosity=minimal"
```

Result: 579 passed, 0 failed, 0 skipped.

x64sc lockstep/checkpoint gate:

```pwsh
dotnet test tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint" --logger "console;verbosity=minimal"
```

Result: 335 passed, 0 failed, 0 skipped.

Whitespace:

```pwsh
git diff --check
```

Result: clean.

Known validation noise: the test runs print PowerShell profile/native-build helper noise from `SnippetsManager.psm1`, but the test processes exit 0 and report clean pass counts.

## Current Git State

The branch is dirty. The dirty set is broad and is expected for this handoff. Important status categories:

- Requirement/documentation files modified:
  - `README.md`
  - `docs/handoff.md`
  - `docs/requirements/technical/TR-System-Core.md`
  - `docs/requirements/test/TEST-Requirements.md`
  - `docs/requirements/traceability/Phase1-Open-Todo-AC-Matrix-2026-06-08.md`
  - `docs/requirements/traceability/TR-to-Design-Decision-Map.md`
  - `docs/requirements/traceability/ARCH-CHIPGLUE-001-Chip-Audit-2026-06-12.md`
- New boundary tests:
  - `tests/ViceSharp.TestHarness/Architecture/ChipGlueBoundaryTests.cs`
- Broad code/test movement for chip-glue cleanup:
  - many files deleted from `src/ViceSharp.Chips`
  - new Core-owned helpers under `src/ViceSharp.Core`
  - new/renamed C1541/C64 device wiring tests under `tests/ViceSharp.TestHarness/Wiring`
  - multiple existing CIA/VIA/SID/VIC/IEC/input/tape/media tests updated for moved namespaces and ownership boundaries
- Untracked marker backup files exist:
  - `AGENTS-README-FIRST.yaml.deleted-20260613T1853064646959Z`
  - `AGENTS-README-FIRST.yaml.deleted-20260614T0235109221607Z`
  These look like tool-created backups. Do not delete them without user approval.

Use `git status --short --branch` for the exact current list before making any commit.

## MCP / TODO State

Previous Codex verification in this continuation reported live MCP TODO state as `0 open / 8 total`, and `ARCH-CHIPGLUE-001` was marked done. That was before this 2026-06-14 handoff write. Claude should re-bootstrap through `mcpserver-claude-code-plugin` and re-query TODO state before mutating MCP records.

Do not use raw REST or direct DB/YAML writes for MCP state. Follow `AGENTS-README-FIRST.yaml`:

1. Verify marker trust and health nonce.
2. Use the Claude plugin, not Codex/Grok/Cline plugin.
3. Reopen or continue the session log.
4. Query TODOs.
5. Only then perform MCP TODO/session/requirements writes.

## What Claude Should Do Next

1. Bootstrap from `AGENTS-README-FIRST.yaml` with `mcpserver-claude-code-plugin`.
2. Run `git status --short --branch` and inspect the dirty tree before edits.
3. Confirm live MCP TODO state and session log state.
4. Decide with the user whether to commit the current remediation set as one coherent architecture/requirements slice, or split into:
   - code movement/remediation,
   - boundary tests,
   - requirements/docs.
5. If committing, do not include unrelated generated/backup artifacts unless the user explicitly wants them. In particular, review the two `AGENTS-README-FIRST.yaml.deleted-*` files.
6. Re-run the gates before push if any code changes are made after this handoff:
   - traceability script
   - XMLDOC convention
   - consolidated chip-glue focused gate
   - lockstep/checkpoint gate
7. If publishing/wrapping up, update MCP session/TODO/requirements through the Claude plugin, then push to the requested remote.

## Resume Prompt For Claude

```text
Continue in F:\GitHub\vice-sharp from root handoff.md dated 2026-06-14.

Read AGENTS-README-FIRST.yaml first. Use mcpserver-claude-code-plugin for MCP session log, TODO, and requirements operations. Do not substitute raw REST or another agent plugin.

Current branch is codex/x64sc-d64-30s-lockstep at HEAD 8eb4c51 with a broad dirty worktree. Treat the dirty set as intentional unless proven otherwise; do not revert user/Codex work.

The completed slice is ARCH-CHIPGLUE-001 for Iteration 1 completion. Shared chip implementations must remain machine-agnostic; machine/device glue belongs in Core machine/device adapters. VIA must stay one common implementation. C1541 wiring belongs in C1541 device adapters, not Via6522.

Key evidence:
- docs/requirements/traceability/ARCH-CHIPGLUE-001-Chip-Audit-2026-06-12.md
- docs/requirements/test/TEST-Requirements.md has TEST-ARCH-CHIPGLUE-001
- docs/requirements/technical/TR-System-Core.md has AC 6-10
- tests/ViceSharp.TestHarness/Architecture/ChipGlueBoundaryTests.cs cites TR-SYSTEM-CORE-001 / TEST-ARCH-CHIPGLUE-001 / ARCH-CHIPGLUE-001

Last Codex validation:
- traceability script exited 0
- XMLDOC: 1 passed, 0 failed, 0 skipped
- consolidated chip-glue gate: 579 passed, 0 failed, 0 skipped
- lockstep/checkpoint gate: 335 passed, 0 failed, 0 skipped
- git diff --check clean

Next: re-query MCP TODO state, inspect dirty status, then either commit/push the remediation slice or continue only if the user asks for more implementation.
```
