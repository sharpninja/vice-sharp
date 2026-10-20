# Dense session-log smoke, LAB-OMARCHY server, 2026-10-19T19:50 to 19:55

Sequence step 11 of `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 11 ("dense session-log proof"), goal TODO `GOAL-LOCKSTEP-REMEDIATION-001`. Class: McpServer behavior, measurement only, no code change (the goal plan's Phase 2 rule: "Run only a measurement first"). Executed while PAYTON-LEGION2 owned the Phase B and Phase C critical path.

## Contract measured

From the goal plan (`docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok-b7-reconstruction/goal-plan.after-human-steering.md`, Phase 2): on a dense session, `begin` and `complete` each finish in under 5 seconds or the server returns a classified retryable failure, never a false success; immediate readback equals the written turn. The original 2026-09-29 smoke (`docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`) timed raw HTTP calls against PAYTON-LEGION2:7147 (begin 322 ms, complete 312 ms). Under the workspace rule that session-log operations go through the plugin wrapper and never raw REST, this smoke times each wrapper call instead (`pwsh` launching `Invoke-McpPlugin.ps1 -Command Invoke`, which launches the REPL), so every figure below includes the wrapper's own process cost.

## Method

`~/codex-dispatch/scripts/Invoke-DenseSessionSmoke.ps1` (PowerShell, parser and analyzer clean, `-WhatIf` first). A separate plugin cache root (`-CacheRoot`, the plugin's own background-scope mechanism) seeded with a copy of the live `session-state.yaml` whose `sessionId` is set to the densest `ClaudeCode` session the server lists (`workflow.sessionlog.queryHistory`, limit 200: 7 sessions; densest `ClaudeCode-20261019T142759Z-plugin-session`, 33 turns before the smoke). The live interactive session's `current-turn.yaml` is never touched. Per run: `beginTurn`, `appendDialog` (1 item), `appendActions` (1 item), `updateTurn`, `completeTurn`, `queryHistory` readback, then three candidate turn-level read methods. Receipts: `apply/`, `apply-2/`, `apply-3/` (one `dense-session-smoke.json` each, with every params and response file in the originals under `~/codex-dispatch/dense-smoke-20261019/`).

## Results (wall clock per wrapper call, milliseconds)

Run 1 (`apply/`, 19:50:42): rejected before any write. The request id `req-20261019T195042-dense-smoke` lacked the `Z` the server requires (`req-<yyyyMMddTHHmmssZ>-<slug>`); the wrapper returned a result envelope with `code: rejected`, `persisted: false`, `retryable: false` and a failsafe path (`.mcpServer/failsafe/default/.../20261020T005048Z-session_submit-daeb.yaml`, left in place), and the server's own error (`McpValidationException` from `SessionLog.SubmitAsync`) was carried in `childStderr`. This is the classified-failure half of the contract, observed: no false success. The first version of the script mis-read that envelope as success (it checked only the document type); corrected before run 2.

Run 2 (`apply-2/`, 19:53:41, request `req-20261020T005341Z-dense-smoke`): history-before 7,483; begin 8,338; appendDialog 7,875; appendActions 8,504; updateTurn 4,103; complete 5,523; history-after 3,152. Turn count 33 to 34.

Run 3 (`apply-3/`, 19:55:04, request `req-20261020T005504Z-dense-smoke`): history-before 3,391; begin 4,313; appendDialog 3,599; appendActions 4,415; updateTurn 4,327; complete 6,099; history-after 3,206. Turn count 34 to 35.

Wrapper baseline: the three turn-level read attempts, which the server rejects immediately, cost 2,509 to 2,873 ms each across the runs. That is the wrapper's process cost per call (pwsh plus REPL start), not server time.

## Conclusions (observation versus inference marked)

- Observed: `begin` met the 5-second budget in run 3 (4,313) and missed it in run 2 (8,338); `complete` missed it in both runs (5,523 and 6,099). Session-level readback matched in both runs (turn count plus one). The classified-failure path works (run 1).
- Observed: no turn-level read method is reachable through the wrapper (`client.SessionLog.GetTurnAsync` and `client.SessionLog.GetTurnDetailAsync` return `method_invocation_error`; `workflow.sessionlog.getTurn` fails schema validation), so "readback equals the written turn" could be proved only at session level here.
- Inference: subtracting the per-call wrapper baseline (about 2.7 s), the server-side portion is roughly 1.6 to 5.6 s for `begin` and 2.8 to 3.4 s for `complete`, which is still above the sub-second figures of the 2026-09-29 raw measurement on the LEGION2 server. The run-to-run spread (begin 4.3 s versus 8.3 s, history 3.4 s versus 7.5 s) says the host was not quiet; two samples cannot separate server latency from host load.
- Not claimed: that the LAB-OMARCHY server (1.4.41-recovery) meets the 5-second contract; that it fails it; anything about the LEGION2 server, which the original proof targeted.

## Disposition

Evidence only, no code change, per the plan. Filed through triage as `triage-report-9396485ee3bc46ba8fbc143544ddf955` (group `triage-group-d8ae0ed9628e9009`, 19:57, routed by the server to the McpServer workspace): the wrapper's per-call process cost (about 2.7 s on this host) makes the 5-second contract unmeasurable through the sanctioned route, and the wrapper exposes no turn-level readback. Follow-ups carried in the goal TODO: repeat the smoke on the LEGION2 server when that agent is idle; a measurement that isolates server time needs either a wrapper-reported server elapsed figure or an operator-approved direct timing.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 97%, accuracy 98%, completeness 95%
