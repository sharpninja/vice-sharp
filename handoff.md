# ViceSharp Reboot Handoff (2026-05-21)

## Current Baseline

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- Last verified project commit before this handoff refresh: `857a69cb6cdb1babb023bf1070cb4b2fb9df19af` (`docs(requirements): refresh wiki export`)
- `origin/main` and `github/main` were both verified at `857a69c`.
- The only intentionally uncommitted local item was `docs/reddit-followup-post.md`.
- Active plan: `docs/plan.md`, "ViceSharp Phase 1 Completion Plan", updated 2026-05-21.
- This is a reboot continuity handoff, not a Phase 1 completion claim.

## MCP And Agent Rules

- Read `AGENTS-README-FIRST.yaml` first in every resumed session.
- Use `F:\GitHub\mcpserver-codex-plugin` for MCP session-log, TODO, requirements, import/export, and traceability work.
- Bootstrap through `workflow.sessionlog.bootstrap` before `workflow.sessionlog.*`, `workflow.todo.*`, or `workflow.requirements.*`.
- Do not use raw REST or direct `docs/todo.yaml` edits for normal MCP work.
- If marker signature, health nonce, plugin availability, or MCP auth fails, stop MCP mutation and use the plugin failsafe path.
- MCP STDIO frames on physical blank lines. For multiline markdown payloads, pass compressed JSON or another single-line serialized payload so blank lines do not truncate the request.
- All subagents must read `AGENTS-README-FIRST.yaml` before work and must report progress to the main agent at least every five minutes.

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
  - Wiki commit: `d9175f006ecb39258d8e82df3b655c3908ed025a` (`Publish requirements wiki`)
- Future wiki exports should push `docs/Project/wiki/github` to the GitHub wiki remote after generating the ZIP.

## Current Phase 1 Plan

`PERF-TUNING-001` is now required for Phase 1 completion. The active closeout order in `docs/plan.md` is:

1. Reconcile tracked state.
2. Run the VICE requirements traceability gate.
3. Continue `BACKFILL-VIDEO-001`.
4. Close true-drive 1541 and D64 behavior.
5. Complete datasette MVP.
6. Complete snapshot and capture closeout.
7. Close input parity.
8. Finish testbench and launcher shim.
9. Run final x64sc lockstep.
10. Run the performance first pass for `PERF-TUNING-001`, targeting 25% of classic VICE performance.
11. Complete Phase 1 closeout.

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

The plan records the current `BACKFILL-VIDEO-001` progress:

- `FR-VIC-007` and generated wiki docs cite PAL x64sc border clear/set cycles and the cycle-56 CSEL 1-to-0 right-side-border-open behavior.
- `Mos6569` snapshots skip right-border set checks when the side border remains open.
- Sprite visibility allows side-border pixels when the horizontal flip-flop stayed open.
- Focused renderer/border tests were 28/28.
- The broader VIC/video gate was 155/155.

Continue `BACKFILL-VIDEO-001` from the Slice 0.5 traceability gate. The next useful sub-slices are:

- left-side and continuous side-border cases
- non-PAL border timing
- native x64sc visible-frame checkpoints
- non-PAL per-model sprite DMA tables
- sprite data-fetch side effects
- native lockstep checkpoints for multiplexing edge cases
- visible-frame/checkpoint validation for display-mode pixel effects
- FLI/AFLI timing depth

Before each code slice, name the canonical `FR-*`, `TR-*`, and `TEST-*` IDs and cite VICE source/docs. If the imported requirement is incomplete or wrong, update the requirement first, then implement.

## Validation At Wrap-Up

- `git diff --check` passed before this handoff refresh.
- The requirements ZIP was readable and hashed as:
  - `CCE3EC1DC605ACCBF4ED7938B49EB89D18B9C04A074F26B9BCFB01FB5FB07AFD`
- GitHub wiki remote was reachable at `refs/heads/master`.
- GitHub wiki page returned HTTP 200 after publication earlier in this session.

## Resume Prompt

Use this prompt after reboot:

```text
Continue from the ViceSharp reboot handoff in F:\GitHub\vice-sharp.

Read AGENTS-README-FIRST.yaml first and use F:\GitHub\mcpserver-codex-plugin for MCP TODO/session-log/requirements operations. Bootstrap the plugin before MCP calls. All subagents must read AGENTS-README-FIRST.yaml and report progress to the main agent at least every five minutes.

Current verified state before reboot:
- main had the Phase 1 requirements/wiki work through 857a69cb6cdb1babb023bf1070cb4b2fb9df19af, followed by a reboot handoff refresh commit.
- origin/main and github/main should both be synchronized with local main.
- GitHub wiki was published to https://github.com/sharpninja/vice-sharp/wiki at wiki commit d9175f006ecb39258d8e82df3b655c3908ed025a.
- Only local dirty item intentionally left out: docs/reddit-followup-post.md.
- Requirements refreshed in MCP and generated docs for FR-VIC-001, FR-VIC-007, FR-VIC-010, and TEST-VIC-001; mappings for FR-VIC-001/007/010 include TR-CYCLE-001 and TEST-VIC-001.
- Do not treat repo-local ROM absence as a blocker; resolve VICE data from local x64sc.exe/WinVICE root, especially C:\Users\kingd\.choco\lib\winvice-nightly\tools\GTK3VICE-3.8-win64.
- If publishing wiki docs again, push docs/Project/wiki/github to https://github.com/sharpninja/vice-sharp.wiki.git; do not stop at ZIP-only export.

Next work:
Continue Phase 1 implementation with appropriate subagents under docs/plan.md. Start from Slice 1 remaining BACKFILL-VIDEO-001 work: left-side/continuous side-border cases, non-PAL border timing, native x64sc checkpoints, non-PAL per-model sprite DMA tables, sprite data-fetch side effects, visible-frame/checkpoint validation for display-mode pixel effects, and FLI/AFLI timing depth. Before code changes, name canonical FR/TR/TEST IDs and cite VICE source/docs; update requirements first if source shows the requirement is incomplete or wrong. Run tools/check_requirement_traceability.ps1 and focused tests for the touched slice, update MCP session/TODO state, then commit and push coherent green slices.
```
