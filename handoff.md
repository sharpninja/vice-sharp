# ViceSharp Reboot Handoff (2026-05-21)

## Current Baseline

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- Last synchronized project commit before this MCP backfill/planning slice: `8c1b2fc99670597b8a9eb86f67bd90c644dc457b` (`docs(handoff): record matrix wiki sync`)
- `origin/main` and `github/main` were both verified at `8c1b2fc99670597b8a9eb86f67bd90c644dc457b` before this slice.
- Matrix/idle implementation commit: `243c651805a04d89fcfb1a26073b5de210037519` (`feat(vic): add matrix idle fetch coverage`).
- No intentionally uncommitted local docs are expected after the prior reddit follow-up draft deletion.
- Active plan: `docs/plan.md`, "ViceSharp Phase 1 Completion Plan", updated 2026-05-21.
- This is a reboot continuity handoff, not a Phase 1 completion claim.

## MCP And Agent Rules

- Read `AGENTS-README-FIRST.yaml` first in every resumed session.
- Use `F:\GitHub\mcpserver-codex-plugin` for MCP session-log, TODO, requirements, import/export, and traceability work.
- Bootstrap through `workflow.sessionlog.bootstrap` before `workflow.sessionlog.*`, `workflow.todo.*`, or `workflow.requirements.*`.
- Do not use raw REST or direct `docs/todo.yaml` edits for normal MCP work.
- If marker signature, health nonce, plugin availability, or MCP auth fails, stop MCP mutation and use the plugin failsafe path.
- During the 2026-05-21 matrix/idle slice, a subagent reported an MCP health
  nonce failure after the main agent had opened the turn and queried TODO
  state. MCP mutation was paused after that report, and local fallback docs
  were updated instead.
- MCP STDIO frames on physical blank lines. For multiline markdown payloads, pass compressed JSON or another single-line serialized payload so blank lines do not truncate the request.
- All subagents must read `AGENTS-README-FIRST.yaml` before work and must report progress to the main agent at least every five minutes.
- 2026-05-21/22 MCP backfill verified the Codex plugin trust path, imported
  pending and failsafe recovery data through plugin calls, normalized legacy
  invalid request IDs with `powershell-yaml`, replayed valid edge-case
  requirement updates, and skipped only empty or superseded failsafe payloads.
  `ARCH-TESTBENCH-001` is now marked in MCP TODO state as the next-wave
  high-priority Phase 1 focus.

## Important Local Environment Notes

- The repo is not expected to contain ROMs.
- Resolve VICE data from the local WinVICE install/root, not from repo-local ROM folders.
- Last verified local WinVICE root: `C:\Users\kingd\.choco\lib\winvice-nightly\tools\GTK3VICE-3.8-win64`.
- Its VICE data folders are directly under that root, including `C64` and `DRIVES`.
- If a tool says it cannot locate repo ROMs, that is not a valid Phase 1 blocker.

## Requirements And Wiki State

- MCP requirements were refreshed for:
  - `FR-VIC-001`
  - `FR-VIC-007`
  - `FR-VIC-010`
  - `TEST-VIC-001`
- Mappings were verified for `FR-VIC-001`, `FR-VIC-007`, and `FR-VIC-010`; each maps to `TR-CYCLE-001` and `TEST-VIC-001`.
- Server-generated wiki export was committed in `857a69c`.
- Generated wiki files updated under:
  - `docs/Project/wiki/azure/`
  - `docs/Project/wiki/github/`
  - `docs/requirements/requirements-wiki-documents.zip`
- The GitHub wiki was also published to the actual wiki repository, not just zipped:
  - URL: `https://github.com/sharpninja/vice-sharp/wiki`
  - Wiki remote: `https://github.com/sharpninja/vice-sharp.wiki.git`
  - Wiki commit: `28b82e9175757b93be350a10d62eaeb7ae8b40b8` (`docs(wiki): refresh from 243c651`)
- Future wiki exports should push `docs/Project/wiki/github` to the GitHub wiki remote after generating the ZIP.

## Current Classic VICE Edge-Case Backfill

- The requirements-only backfill slice is complete and committed in the main
  repo.
- The slice scanned `native/vice/vice/src` with
  `tools/audit_vice_edge_cases.ps1` and wrote the review inventory to
  `docs/requirements/backfill/Classic-VICE-Edge-Case-Inventory.md`.
- The curated promotion record is
  `docs/requirements/backfill/Classic-VICE-Edge-Case-TR-Backfill.md`.
- The VICE source provenance policy in
  `docs/requirements/sources/VICE-Source-Manifest.md` now allows the narrow
  exception that classic VICE source may create or clarify TRs only for
  observable compatibility behavior ViceSharp must match.
- MCP requirements now include 19 new canonical edge TRs:
  `TR-VIC-EDGE-001` through `TR-VIC-EDGE-007`, `TR-CPU-EDGE-001`,
  `TR-CPU-EDGE-002`, `TR-RAM-EDGE-001`, `TR-SID-EDGE-001` through
  `TR-SID-EDGE-003`, `TR-IEC-EDGE-001`, `TR-DRV-EDGE-001`,
  `TR-MEDIA-EDGE-001`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001`, and
  `TR-VDC-EDGE-001`.
- Generated requirements markdown and wiki markdown were refreshed under
  `docs/Project/` and `docs/Project/wiki/`.
- Current traceability audit output after the matrix/idle implementation slice: 163 canonical IDs,
  82 referenced canonical IDs, 81 unreferenced canonical IDs, and 53
  noncanonical IDs in `src`/`tests`. The new edge TRs are requirements-only
  anchors and are expected to remain unreferenced until their implementation
  slices add executable coverage.
- `docs/plan.md` now integrates the edge TRs into the Phase 1 slice order:
  VIC-II edge TRs drive `BACKFILL-VIDEO-001`, media/tape edge TRs drive
  `BACKFILL-MEDIA-001`, `ARCH-TRUEDRIVE-1541-002`, and `RUNTIME-TAPE-002`,
  and final lockstep must explicitly triage every Phase-1-relevant edge TR.
- `BACKFILL-VIDEO-001` continued with `TR-VIC-EDGE-002` continuous side-border
  implementation:
  - `Mos6569` now snapshots per-line horizontal-display, left-border-open, and
    right-border-open state.
  - `VideoRenderer` renders background fill in opened side borders and admits
    sprites through carried-open side borders.
  - Managed coverage now includes right-open, left-carry, continuous carry,
    cycle-17 blank-line behavior, and PAL/NTSC/PAL-N/old-NTSC/HMOS border
    cycle invariance.
- `BACKFILL-VIDEO-001` continued with `TR-VIC-EDGE-006` register readback and
  collision latch behavior:
  - `Mos6569.Read` now matches VICE/x64sc fixed readback bits for `$D019`,
    `$D01A`, and unused `$D02F-$D03F`.
  - Writes to unused VIC-II registers and collision registers do not fabricate
    register or latch state.
  - Managed coverage now includes focused register-readback/IRQ/collision validation.
- `BACKFILL-VIDEO-001` continued with `TR-VIC-EDGE-005` matrix idle/fill
  behavior:
  - `Mos6569` now tracks the 40-column matrix/color latches and exposes the
    VICE idle graphics address rule (`$39ff` in ECM, `$3fff` otherwise).
  - `C64MemoryMap` latches matrix prefetch `$ff` and color nibbles from raw
    CPU-program RAM at the visible PC, matching VICE `ram_base_phi2[reg_pc]`.
  - Managed coverage now includes focused matrix prefetch, real matrix fetch,
    standard-text graphics latch consumption, and ECM idle graphics address
    validation.
- MCP TODOs updated with edge TR links:
  `BACKFILL-VIDEO-001`, `BACKFILL-MEDIA-001`, `ARCH-TRUEDRIVE-1541-002`,
  `RUNTIME-TAPE-002`, and `BACKFILL-LOCKSTEP-001`.
- GitHub wiki was published from `docs/Project/wiki/github` to
  `https://github.com/sharpninja/vice-sharp/wiki` at wiki commit
  `28b82e9175757b93be350a10d62eaeb7ae8b40b8`.

## Current Phase 1 Plan

`PERF-TUNING-001` is now required for Phase 1 completion. The next wave is
`ARCH-TESTBENCH-001`: inventory the upstream VICE testbench/x64sc hook and
debugcart contract, compare it to the current ViceSharp launcher surface, then
implement the smallest runner path that can execute selected upstream smoke
cases unchanged.

The active closeout order in `docs/plan.md` is:

1. Reconcile tracked state.
2. Run the VICE requirements traceability gate.
3. Start `ARCH-TESTBENCH-001` as the next implementation wave.
4. Continue `BACKFILL-VIDEO-001`.
5. Close true-drive 1541 and D64 behavior.
6. Complete datasette MVP.
7. Complete snapshot and capture closeout.
8. Close input parity.
9. Finish launcher shim follow-through.
10. Run final x64sc lockstep.
11. Run the performance first pass for `PERF-TUNING-001`, targeting 25% of classic VICE performance.
12. Complete Phase 1 closeout.

Real Phase 1 blockers still listed in the plan:

- `BACKFILL-VIDEO-001`
- `BACKFILL-MEDIA-001`
- `BACKFILL-INPUT-001`
- `BACKFILL-LOCKSTEP-001`
- `RUNTIME-TAPE-002`
- `RUNTIME-SNAPSHOT-002`
- `RUNTIME-CAPTURE-002`
- `ARCH-TESTBENCH-001`
- `ARCH-TRUEDRIVE-1541-002`
- `CLI-LAUNCHER-001`
- `PERF-TUNING-001`

## Latest Video Progress

Recent committed VIC work includes:

- `1edaf2f` (`feat(vic): open right side border rendering`)
- `46edda9` (`feat(vic): advance phase 1 parity`)
- `646b3a1` (`feat(vic): carry opened side borders`)
- Current implementation slice: `TR-VIC-EDGE-005` managed matrix idle/fill behavior.

The plan records the current `BACKFILL-VIDEO-001` progress:

- `FR-VIC-007` and generated wiki docs cite PAL x64sc border clear/set cycles and the cycle-56 CSEL 1-to-0 right-side-border-open behavior.
- `Mos6569` snapshots per-line horizontal display, right-open, and carried
  left-open side-border state when the side border remains open.
- Sprite visibility and non-sprite background fill now obey opened side-border
  state instead of static border geometry alone.
- Focused matrix/idle plus adjacent bad-line/core-timing tests were 18/18.
- The broader VIC/video gate was 179/179.

Continue `BACKFILL-VIDEO-001` from the committed continuous side-border slice. The next useful sub-slices are:

- native x64sc side-border checkpoints
- model-aware visible-frame validation beyond the managed PAL/non-PAL cycle-state tests
- non-PAL per-model sprite DMA tables
- sprite data-fetch side effects
- native lockstep checkpoints for multiplexing edge cases
- visible-frame/checkpoint validation for display-mode pixel effects
- `TR-VIC-EDGE-001` invalid ECM priority/collision remediation
- native `TR-VIC-EDGE-006` register checkpoints for the managed readback behavior,
  starting with managed-vs-x64sc `$D000-$D03F` readback/write-ignore coverage
  and then collision read-clear timing
- native `TR-VIC-EDGE-005` matrix/idle checkpoints and the 6569
  RAM-to-character-ROM fetch-address latch path
- FLI/AFLI timing depth

Before each code slice, name the canonical `FR-*`, `TR-*`, and `TEST-*` IDs and cite VICE source/docs. If the imported requirement is incomplete or wrong, update the requirement first, then implement.

## Validation At Wrap-Up

- `git diff --check` passed before this handoff refresh.
- Focused matrix/idle plus adjacent bad-line/core-timing validation passed `18/18`.
- Broader VIC/video validation passed `179/179`.
- Requirements traceability passed with 163 canonical IDs, 82 referenced canonical IDs, 81 unreferenced canonical IDs, and 53 noncanonical references.
- `dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --no-build --nologo --filter "FullyQualifiedName~X64ScVariantLockstepTests"` was attempted after this slice and timed out after 240 seconds; the associated test processes were stopped. Do not treat that gate as green for this slice.
- Full-solution `dotnet test .\ViceSharp.slnx --no-build --nologo` timed out after five minutes and was stopped cleanly; it is not a current green full-solution gate.
- The requirements ZIP was readable and hashed as:
  - `CCE3EC1DC605ACCBF4ED7938B49EB89D18B9C04A074F26B9BCFB01FB5FB07AFD`
- GitHub wiki remote was reachable at `refs/heads/master`.
- GitHub wiki remote was updated to `28b82e9175757b93be350a10d62eaeb7ae8b40b8`
  after the matrix/idle docs sync.

## Resume Prompt

Use this prompt after reboot:

```text
Continue from the ViceSharp reboot handoff in F:\GitHub\vice-sharp.

Read AGENTS-README-FIRST.yaml first and use F:\GitHub\mcpserver-codex-plugin for MCP TODO/session-log/requirements operations. Bootstrap the plugin before MCP calls. All subagents must read AGENTS-README-FIRST.yaml and report progress to the main agent at least every five minutes.

Current verified state before reboot:
- main had the register-readback slice through 06b080d3f3717b32e4b2b89ea6149f3ff7dc6319 before the current matrix/idle slice.
- origin/main and github/main were both synchronized to 06b080d3f3717b32e4b2b89ea6149f3ff7dc6319 before the current matrix/idle slice.
- GitHub wiki was published to https://github.com/sharpninja/vice-sharp/wiki at wiki commit 28b82e9175757b93be350a10d62eaeb7ae8b40b8.
- Only local dirty item intentionally left out: docs/reddit-followup-post.md.
- Requirements refreshed in MCP and generated docs for FR-VIC-001, FR-VIC-007, FR-VIC-010, and TEST-VIC-001; mappings for FR-VIC-001/007/010 include TR-CYCLE-001 and TEST-VIC-001.
- Do not treat repo-local ROM absence as a blocker; resolve VICE data from local x64sc.exe/WinVICE root, especially C:\Users\kingd\.choco\lib\winvice-nightly\tools\GTK3VICE-3.8-win64.
- If publishing wiki docs again, push docs/Project/wiki/github to https://github.com/sharpninja/vice-sharp.wiki.git; do not stop at ZIP-only export.

Next work:
Continue Phase 1 implementation with appropriate subagents under docs/plan.md. Start from the remaining BACKFILL-VIDEO-001 work after the managed matrix/idle slice: native x64sc side-border, register, matrix/idle, and display-mode checkpoints; model-aware visible-frame validation; non-PAL per-model sprite DMA tables; sprite data-fetch side effects; native validation for TR-VIC-EDGE-001 invalid ECM priority/collision; TR-VIC-EDGE-005 6569 RAM-to-character-ROM fetch-address latch behavior; and FLI/AFLI timing depth. For TR-VIC-EDGE-006, begin native coverage with managed-vs-x64sc $D000-$D03F readback/write-ignore checkpoints, then collision read-clear timing. Before code changes, name canonical FR/TR/TEST IDs and cite VICE source/docs; update requirements first if source shows the requirement is incomplete or wrong. Run tools/check_requirement_traceability.ps1 and focused tests for the touched slice, update MCP session/TODO state when the plugin trust path is healthy, then commit and push coherent green slices.
```
