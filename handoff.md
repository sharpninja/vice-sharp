# ViceSharp Reboot Handoff (2026-05-21, refreshed 2026-05-27)

## Current Baseline (post 2026-05-27 plan/handoff audit at HEAD 064d3a0)

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- Current HEAD: `064d3a0`
- Prior handoff baseline commit referenced: `8c1b2fc99670597b8a9eb86f67bd90c644dc457b`
- Additional VIC progress since baseline (examples): `243c651` (matrix idle), `06b080d` (register readback), `646b3a1` (carried side borders) and later side-border / border flip-flop tests.
- `AGENTS.md` created during this audit to formalize Grok plugin usage + Byrd process + writing rules.
- Active plan: `docs/plan.md` (ViceSharp Phase 1 Completion Plan) + this handoff.md. Both refreshed during the audit.
- Superseded older handoff (2026-05-17) archived with note in place.
- Audit used `F:\GitHub\mcpserver-grok-plugin` (GrokCode) per explicit directive and AGENTS-README-FIRST.yaml. Session started: GrokCode-20260527T184055Z-start-session.
- This remains a reboot continuity handoff, not a Phase 1 completion claim.

## 2026-05-27 Audit Reconciliation Notes (this session)

- Code evidence (Mos6569.cs + dedicated tests in VicIIBorderFlipFlopTests.cs / VideoRendererTests.cs) confirms substantial progress on BACKFILL-VIDEO-001 sub-slices (continuous/carried side borders per FR-VIC-007, matrix idle per TR-VIC-EDGE-005, register readback/collision per TR-VIC-EDGE-006).
- 1541/D64 substrate + IEC present (D64DiskImageDevice, C1541 architecture files); aligns with dashboard "70%" for RUNTIME-1541-002. True-drive CPU lockstep remains the deep remaining work (ARCH-TRUEDRIVE-1541-002).
- Traceability script executed; many requirements now have test references.
- Live MCP TODO query (via plugin, workflow.todo.query) for major Phase 1 keywords (BACKFILL-VIDEO, RUNTIME-1541, etc.) returned 0 items. This suggests either different ID patterns in the server or server-side closure of items beyond what local markdowns reflect (one of the stated Sources of Truth in docs/plan.md).
- Recommendation: After this audit, run full `dotnet test` focused gates where practical, push the updated markdowns, and reconcile any remaining MCP TODOs through the Grok plugin (not direct yaml). The older superseded handoff-2026-05-17.md is now explicitly archived in place.

## MCP And Agent Rules

- Read `AGENTS-README-FIRST.yaml` first in every resumed session.
- Use the agent-specific plugin declared in AGENTS-README-FIRST.yaml (for GrokCode agents: `F:\GitHub\mcpserver-grok-plugin`; for other agents their matching plugin). See root `AGENTS.md` for full Grok rules.
- Bootstrap through the plugin (workflow.sessionlog.bootstrap or equivalent New-McpSessionLog / Initialize-McpSession) before any workflow.sessionlog.*, workflow.todo.*, or workflow.requirements.* work.
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

## Current Phase 1 Plan (2026-05-27 audit refresh)

`PERF-TUNING-001` is still required for Phase 1 completion. The next wave remains
`ARCH-TESTBENCH-001`.

The active closeout order (per docs/plan.md, lightly refreshed in audit) is largely
unchanged, with the addition of this reconciliation step having been performed.

Real Phase 1 blockers (local markdown view; see 2026-05-27 audit notes above and
MCP TODO query results for server-side truth):

- `BACKFILL-VIDEO-001` (substantial managed + test progress on side borders, matrix idle, register readback/collision since prior snapshot; native checkpoints and deeper FLI/AFLI/non-PAL sprite DMA remain)
- `BACKFILL-MEDIA-001`
- `BACKFILL-INPUT-001`
- `BACKFILL-LOCKSTEP-001`
- `RUNTIME-TAPE-002`
- `RUNTIME-SNAPSHOT-002`
- `RUNTIME-CAPTURE-002`
- `ARCH-TESTBENCH-001`
- `ARCH-TRUEDRIVE-1541-002` (D64/1541 substrate exists and is functional per code + tests)
- `CLI-LAUNCHER-001`
- `PERF-TUNING-001`

(See full audit notes section above for evidence from code, tests, and live MCP query via the Grok plugin.)

## Latest Video Progress (refreshed with 2026-05-27 audit evidence)

Recent committed VIC work (post 05-21 baselines) includes additional side-border carry,
register readback, and matrix idle slices (see commits after 8c1b2fc / 46edda9).

The plan (and Mos6569.cs + border tests) records substantial `BACKFILL-VIDEO-001` progress:

- Managed continuous + carried side borders (FR-VIC-007), matrix idle/fill (TR-VIC-EDGE-005),
  and register readback + collision latch (TR-VIC-EDGE-006) are implemented with focused tests
  (VicIIBorderFlipFlopTests.cs, VideoRendererTests.cs, etc.).
- Native x64sc checkpoints, non-PAL sprite DMA tables, FLI/AFLI depth, and full visible-frame
  validation remain the next sub-slices (as listed in the Continue section below).

Audit note: Code comments in Mos6569.cs explicitly track BACKFILL-VIDEO-001 / specific FR/TR IDs.

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

## Resume Prompt (updated 2026-05-27 after plan audit)

Use this prompt after reboot:

```text
Continue from the ViceSharp reboot handoff in F:\GitHub\vice-sharp (refreshed 2026-05-27).

Read AGENTS-README-FIRST.yaml first and use F:\GitHub\mcpserver-grok-plugin (for GrokCode agents) or the matching plugin for your agent for all MCP TODO/session-log/requirements operations. Bootstrap the plugin before MCP calls. All agents must read AGENTS-README-FIRST.yaml + the root AGENTS.md and report progress.

Current verified state (post 2026-05-27 audit at HEAD 064d3a0):
- handoff.md and docs/plan.md refreshed during audit. Superseded 2026-05-17 handoff archived in place.
- Substantial BACKFILL-VIDEO-001 progress landed (side borders, matrix idle, register readback) with tests; see Mos6569.cs and VicII*Tests.cs. Native checkpoints and deeper FLI/AFLI/non-PAL DMA remain.
- 1541/D64 substrate functional (RUNTIME-1541-002 substrate done; true-drive/CPU lockstep in ARCH-TRUEDRIVE-1541-002).
- Live MCP TODO query during audit (via plugin) returned 0 items for major Phase 1 keywords. Local markdowns updated; treat server MCP state as source of truth going forward.
- AGENTS.md now present (Grok plugin rules, Byrd process, no em-dashes, Azure DevOps primary, etc.).
- Do not treat repo-local ROM absence as a blocker; resolve VICE data from local x64sc.exe/WinVICE root.
- GitHub wiki push: use docs/Project/wiki/github to the .wiki.git remote.

Next work:
Continue Phase 1 under the refreshed docs/plan.md. Prioritize remaining BACKFILL-VIDEO-001 native checkpoints + deeper sprite DMA / FLI, true-drive 1541 lockstep, and ARCH-TESTBENCH-001. Before code changes name canonical FR/TR/TEST IDs + cite VICE source. Run traceability script and focused tests. Update MCP state via the active agent plugin, then commit coherent green slices. Re-run full plan/handoff audit when major slices land.
```
