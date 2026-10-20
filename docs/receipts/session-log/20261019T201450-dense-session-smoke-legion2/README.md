# Dense session-log smoke, PAYTON-LEGION2 server, 2026-10-19T20:15

Sequence step 11 of `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 11 ("dense session-log proof"), goal TODO `GOAL-LOCKSTEP-REMEDIATION-001`. Measurement only, no code change. This is the LEGION2 half of the measurement; the LAB-OMARCHY half is `docs/receipts/session-log/20261019T1950-dense-session-smoke-omarchy/`. Run while Phase C step 3 waits for an operator ruling.

## Contract measured

From the goal plan, Phase 2: on a dense session, `begin` and `complete` each finish in under 5 seconds, or the server returns a classified retryable failure and never a false success; immediate readback equals the written turn. The original 2026-09-29 smoke (`docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`) timed raw HTTP calls against this server (begin 322 ms, complete 312 ms). Session-log operations must go through the plugin wrapper, never raw REST, so this run times each wrapper call (`pwsh` launching `Invoke-McpPlugin.ps1 -Command Invoke`, which launches the REPL). Every figure includes the wrapper's own process cost.

## Method

OMAR-VICE#'s `Invoke-DenseSessionSmoke.ps1` (`C:\Users\kingd\codex-dispatch\scripts`, SHA256 67A306AA76FE6D4A8000BD9746DCBE99698A64DF5D64EE195ACA510041D3E4D2), read in full first, analyzer 0, `-WhatIf` first. Plugin 1.121.0 (`-PluginScript ...\mcpserver-local\mcpserver\1.121.0\lib\Invoke-McpPlugin.ps1`). Separate cache root (`-ScopeDir C:\Users\kingd\codex-dispatch\dense-smoke-scope-legion2-20261019T201450`) seeded with a copy of the live `session-state.yaml`; the live `current-turn.yaml` was not touched. `workflow.sessionlog.queryHistory` (limit 200) listed 200 sessions; the densest `ClaudeCode` session among them was `ClaudeCode-20260630T233948Z-plugin-session` (68 turns). The list was capped at the limit, so it is the densest of those 200, not necessarily of the whole server. One timed run: `beginTurn`, `appendDialog` (1 item), `appendActions` (1 item), `updateTurn`, `completeTurn`, `queryHistory`, then three candidate turn-level read methods. Request `req-20261020T011509Z-dense-smoke`.

Per-call params, responses and the summary are in `apply/`. The two `queryHistory` responses (243 KB each, listing 200 sessions) stay outside the repository; `manifest.json` records every file with size and SHA256, and the original location.

## Results (wall clock per wrapper call, milliseconds)

history-before 3,082; begin 3,287; appendDialog 2,649; appendActions 3,389; updateTurn 3,270; complete 6,403; history-after 2,828. Turn count 68 to 69.

Turn-level readback: `client.SessionLog.GetTurnAsync` 2,158 (`method_invocation_error`), `client.SessionLog.GetTurnDetailAsync` 2,112 (`method_invocation_error`), `workflow.sessionlog.getTurn` 2,120 (`schema_validation_failed`: "No YAML schema is registered for method 'workflow.sessionlog.getTurn'"). These rejected calls give the wrapper's per-call cost on this host: about 2.1 s.

## Conclusions (observation versus inference marked)

- Observed: `begin` finished in 3,287 ms, inside 5 seconds; `complete` took 6,403 ms, outside 5 seconds, through the wrapper route. Session-level readback matched (turn count plus one). No turn-level read method is reachable through the wrapper, the same as on LAB-OMARCHY.
- Inference: subtracting the per-call wrapper cost of about 2.1 s, the server-side portion is roughly 1.2 s for `begin` and 4.3 s for `complete`. That is far above the 2026-09-29 raw figures (322 ms and 312 ms) on this same server, but one sample cannot separate server latency from host load or from the session's size.
- Not claimed: that the LEGION2 server meets or fails the 5-second contract. The sanctioned route cannot isolate server time (same finding as triage-report-9396485ee3bc46ba8fbc143544ddf955 from LAB-OMARCHY).

## Disposition

Evidence only. The turn written to `ClaudeCode-20260630T233948Z-plugin-session` is labelled "Dense session-log smoke (sequence step 11, measurement only)" and marked `dense-smoke-marker-20261019T201504`; its existing 68 turns were not changed.

Signed: claude-opus-5-5[1m]-high@ClaudeCode | Confidence: correctness 97%, accuracy 97%, completeness 94%
