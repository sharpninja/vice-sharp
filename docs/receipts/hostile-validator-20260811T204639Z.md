# Hostile Validator Receipt (Phase G)

- **TimestampUtc**: 2026-08-11T20:46:39Z
- **ValidatorIdentity**: GrokSubagentHostile
- **Workspace**: F:\GitHub\vice-sharp
- **Method**: Independent re-read of implementer temp logs, durable `docs/receipts/*`, live MCP GET of `PLAN-VIC20-EXACT-001`, HANDOFF + audit prose, and TwoSecond source gate. SHA256 compare of umbrella logs. No product code changes. Default FAIL/UNKNOWN until re-verified.

## Claims reviewed

| Id | Claim (summary) | Verdict |
|----|-----------------|---------|
| G1 | Process-isolated umbrella Vic20 gate green: Passed=46 Failed=0 Skipped=0, anyNonZeroExit=0 | **PASS** |
| G2 | Umbrella log green 2s non-idle PAL+NTSC (VICESHARP_LOCKSTEP_2S=1) with real wall time | **PASS** |
| G3 | MCP PLAN-VIC20-EXACT-001 done=true; 7/7 tasks done; doneSummary SCOPED Exact not whole-machine | **PASS** |
| G4 | HANDOFF.md + audit mention Phase G closeout / scoped completion with residuals | **PASS** |
| G5 | Claim language does not overclaim whole-machine Exact | **PASS** |

## Per-claim evidence

### G1) Umbrella process-isolated 46/0/0 - PASS

**Attack**: Hand-edited footer; temp vs docs diverge; skip counted as pass; single process with residue; wrong arithmetic; Fe3 flash dropped from filter.

**Re-verify**:
- Temp `C:\Users\kingd\AppData\Local\Temp\grok-goal-a8b508754f9e\implementer\vic20-umbrella-gate.log` and durable `docs/receipts/vic20-umbrella-gate-2026-08-11.log` both length 6992; SHA256 **match**:
  - `520AE0742CF557B24A4F15E4B80846C7F8F03E0478C0BDAF4AC934545F12C09D`
- Header: `vic20-umbrella-gate PROCESS-ISOLATED 2026-08-11T15:42:04...`
- Thirteen separate parts, each own `Test run for ...` + `EXIT=0` banner + VSTest `Passed!` line:
  - pixel_index_pal P=1
  - pixel_index_ntsc P=1
  - pixel_busy P=1
  - pixel_bgra P=1
  - pixel_frame P=1
  - video_2k P=2
  - sound P=8
  - flash P=12
  - cart P=7
  - workload_pal P=1
  - workload_ntsc P=1
  - snapshot P=4
  - keyboard P=6
- Arithmetic: `1+1+1+1+1+2+8+12+7+1+1+4+6 = 46` (recomputed).
- Footer lines 96-98:
  - `Passed=46 Failed=0 Skipped=0 anyNonZeroExit=0`
  - `Criterion: Failed=0 Skipped=0 and all part EXIT=0`
- Every part reports `Failed: 0` and `Skipped: 0`. No non-zero EXIT banner.
- Per-part `umb-*.log` sidecars exist under implementer temp (pixel through keyboard), consistent with process isolation.
- Flash attack (Fe3 dropped): `Flash040CoreTests` 5 + `Flash040EraseLatencyTests` 3 + `Fe3Flash040Tests` 4 = 12 Facts; class name `Fe3Flash040Tests` matches `FullyQualifiedName~Flash040`. No missing Fe3 erase-latency suite under this filter.

### G2) 2s non-idle PAL+NTSC real wall time - PASS

**Attack**: Bare `return` when `VICESHARP_LOCKSTEP_2S` unset still reports Passed (not Skipped); footer theater; wrong test filter; sub-second wall.

**Re-verify**:
- Source `tests/ViceSharp.TestHarness/Vic20/Vic20WorkloadLockstep.cs`:
  - `EveryCycle_Workload_Match_TwoSecondPal` / `TwoSecondNtsc` bare-return if env != `1`, then `RunEveryCycle(TwoSecondPalCycles=2216810 / TwoSecondNtscCycles=2045454)`.
  - Bare return path cannot produce multi-second VSTest duration.
- Umbrella footer (same SHA-matched log, lines 101-113):
  - `======== 2s workload PAL (VICESHARP_LOCKSTEP_2S=1) ========`
  - Passed 1, Duration **27 s**
  - `======== 2s workload NTSC ========`
  - Passed 1, Duration **11 s**
  - `UMBRELLA_PLUS_2S complete 2026-08-11T15:45:36...` (summary at 15:44:23; ~73s wall for both + overhead)
- Sidecars:
  - `implementer/umb-2s-pal.log`: Passed 1, Duration 27 s
  - `implementer/umb-2s-ntsc.log`: Passed 1, Duration 11 s
- Corroborating named runs (same session, implementer):
  - `lockstep-2s-pal.log`: `EveryCycle_Workload_Match_TwoSecondPal` **[19 s]**, Total time 23.17s
  - `lockstep-2s-ntsc.log`: `EveryCycle_Workload_Match_TwoSecondNtsc` **[11 s]**, Total time 14.95s
- Wall times disprove env-unset bare return. Non-idle workload is KERNAL READY CPU every-cycle (video track off via `VICESHARP_LOCKSTEP_VIDEO=0` in source and umbrella header).

**Non-FAIL caveat**: `umb-2s-*.log` omit FQN lines (summary-only). Method identity rests on umbrella section labels + prior lockstep-2s named logs. Not enough to FAIL G2 as worded.

### G3) MCP PLAN-VIC20-EXACT-001 done + scoped doneSummary - PASS

**Attack**: done=false; tasks partial; doneSummary claims whole-machine Exact; stale store.

**Re-verify** (live GET `http://PAYTON-LEGION2:7147/mcpserver/todo/PLAN-VIC20-EXACT-001` with `X-Api-Key` from `AGENTS-README-FIRST.yaml`; HTTP 200):
- `done`: **true**
- `completedDate`: `2026-08-11`
- `implementationTasks`: **7/7** with `done: true` (Phases A-G listed)
- `doneSummary` (verbatim key phrases):
  - `Scoped bit-exact VIC-20 program complete 2026-08-11`
  - `process-isolated umbrella 46/0/0`
  - `NOT whole-machine Exact: niche carts Explicit Missing; IEEE/rsuser/printer; headless WriteSnapshot hang`
- `description` includes `BDPv4 bit-exact VIC-20 vs xvic (scoped Exact)` and residual exclude list.
- `remaining` lists residuals (niche carts, IEEE/rsuser/printer, WriteSnapshot hang, process isolation).

**Non-FAIL caveat**: TODO **title** still reads `Bit-exact whole VIC-20 vs xvic (...)` and Phase G task string is `Phase G: Whole-machine claim + hostile AGREE`. Hostility notes residual title/task wording; **doneSummary + description explicitly scope and negate whole-machine Exact**, so G3 as worded (doneSummary SCOPED / NOT whole-machine) PASS.

### G4) HANDOFF + audit Phase G closeout with residuals - PASS

**Attack**: Docs silent on Phase G; no residual list; claim done without scope.

**Re-verify**:
- `HANDOFF.md` lines 8-16: section `## Phase G closeout 2026-08-11 (PLAN-VIC20-EXACT-001 DONE scoped)`; marks MCP done with scoped Exact (not whole-machine); umbrella 46/0/0 + 2s; Scoped Exact list; **Residuals** (niche carts, IEEE-488/rsuser/printer, WriteSnapshot hang, NativeVice process isolation).
- `docs/audit-vic20-vs-vice-2026-08-07.md` lines 207-209: `## Phase G closeout 2026-08-11`; PLAN done (scoped Exact); umbrella 46/0/0 + 2s; whole-machine Exact **not** claimed; residuals listed.
- Supporting: `docs/receipts/vic20-phase-g-closeout-2026-08-11.txt` mirrors same scoped closeout + residuals.

### G5) No whole-machine Exact overclaim - PASS

**Attack**: Closeout language equates program complete with whole-machine Exact; residual silence; MCP title only surface.

**Re-verify**:
- HANDOFF Phase G: explicit `(not whole-machine Exact)` and residual block.
- Audit Phase G: `Whole-machine Exact **not** claimed` + residual list.
- Closeout receipt: `Claim: SCOPED Exact only (not whole-machine Exact)`.
- MCP `doneSummary`: `NOT whole-machine Exact` with residual list.
- Standing audit policy (line 186): whole-machine Exact not claimed; matrix remains rule-scoped.

**Non-FAIL caveats** (language hygiene, not claim fails):
1. HANDOFF line 32: `Whole-machine Exact remains scoped (...)` is awkward phrasing; in context it means Exact remains **scoped** (not a whole-machine promotion). Adjacent residual list and line 10 negation support that reading.
2. MCP title + Phase G task string still say "whole" / "Whole-machine claim"; body/doneSummary/docs negate. Recommend title/task cleanup so scanners do not misread.
3. Prior hostile `docs/receipts/hostile-validator-20260811T203852Z.md` AGREE covered claims A-F only (not this G umbrella). Phase G closeout citing that AGREE is partial; **this receipt** is the independent Phase G re-validation.

## Explicit FAIL list

(none)

## Notes / non-FAIL caveats (audit)

- umb-2s logs are summary-only (no FQN); identity via umbrella labels + lockstep-2s named logs.
- MCP title/task "whole" wording residual; doneSummary honest.
- HANDOFF "Whole-machine Exact remains scoped" awkward but not an overclaim in context.
- Prior A-F AGREE is not a substitute for this G re-validation (now completed).

## OverallVerdict

**AGREE**

All claims G1-G5 independently re-verified PASS. No FAIL list entries.
