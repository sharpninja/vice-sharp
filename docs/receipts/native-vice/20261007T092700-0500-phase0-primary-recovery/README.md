# Phase 0 primary MCP recovery evidence

Recorded local: 2026-10-07T09:30:11.003-05:00.

Scope: recovery prerequisites for the approved immutable-VICE plan. Not product implementation, not xvic parity, not a complete VSF gate, and not a Phase 0 done claim before independent review.

## Primary state

- New session `Codex-20261007T135626Z-plugin-session`, turn `req-20261007T135720Z-turn-866a` exists in primary MCP. Supported `workflow.sessionlog.queryHistory` returned one turn. `appendDialog` and `appendActions` returned `persisted: true`, `degraded: false`, `queued: false`.
- Full bootstrap returned `True` with the refreshed marker (`markerWrittenAtUtc=2026-10-07T14:04:33.1253985+00:00`). Installed plugin manifest is 1.118.0. Earlier 1.119.0 cache path disappeared during this session; no claim is made about the cause.
- `workflow.failsafe.drain` returned scanned 4, replayed 4, failed 0, quarantined 0, skipped 0, aborted false. Current Codex and GrokCode pending folders are empty; GrokCode quarantine is empty.
- `requirement-before.json` preserves the obsolete patch mandate read through `workflow.requirements.getTr`.
- `requirement-primary-readback.json` is the independent post-update `getTr` response. Both existing AC IDs, status `pending`, layer `layer-1`, and unsatisfied values are preserved. Description and both AC texts were compared case-sensitively to canonical lines 73, 77, and 78 of `docs/requirements/technical/TR-Native-Vice-Snapshot.md`.
- `workflow.requirements.generateDocument` with `docType=all, format=wiki` returned success and regenerated `docs/Project/wiki/github/Technical-Requirements.md`. Its TR section now contains the corrected body and both ACs. The GitHub manifest records `2026-10-07T09:26:05.2365241-05:00`. The wrapper returned a ZIP payload and an empty files array; filesystem content and timestamps, not that empty array, establish the write.

## Historical HV limitation

`historical-hv-primary-readback.json` is a fresh native MCP `sessionlog_query` readback. Session `GrokCode-20261004T145125Z-immutable-vice-remediation-hv`, request `req-20261004T145125Z-001-immutable-vice-plan-rerun`, is completed and contains 10,019 characters of response, including all claim prose and scores. The final structured verdict JSON present in the local `.verdict.md` is absent from that primary response. Do not use it as a complete HV persistence receipt. A fresh independent recovery-gate review must include its full result and structured verdict in its own primary session turn.

## Native preservation

`native-state.json` records unchanged commit `cc67418cba58e71d8e70fd353e9de47199ad2075`, 12 tracked modifications, and 2,527 untracked paths. This is the preexisting dirty target, not an acceptable oracle.

Read-only `git -C native/vice apply --reverse --check docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch` used the absolute patch path and returned exit 0. It did not apply the patch.

- Preserved patch SHA-256: `90604E39B827F850A382DC13DD921141290EF2C34E1483CDC6C612A07803E9E2`.
- Preserved and live `native/vice/vice/src/vice-shim-runtime.h` SHA-256: `5450FDA8CFEDB8A22BADA8CD04EE75FF830C48796592FF9EC0141E899D846259`.

## Validation boundaries and disclosed errors

- `pwsh.exe -NoProfile -NonInteractive -File tools/check_requirement_traceability.ps1`: exit 0, 235 canonical IDs, 149 referenced, 86 unreferenced, 1,011 noncanonical references. Full diagnostic output is `traceability.log`. The script's default exit does not fail on findings and is not a clean traceability gate.
- `git diff --check`: exit 0, no output, before the authored recovery documentation update. Rerun after final edits.
- No product build or tests were run; no native files were changed; no cleanup, commit, push, or disk operation was performed.
- A PowerShell wildcard anchor check mishandled literal backticks and refused the first requirement update before mutation. It was replaced with literal `StartsWith` checks; the subsequent update and readback succeeded.
- An initial live-header hash check used `native/vice/src` instead of the observed `native/vice/vice/src`; it failed to find the file. The corrected read-only check returned the matching hash above.
- A marker-redaction pattern missed the embedded `X-Api-Key` example in tool output. Subsequent reads redact the actual secret value everywhere. The key was not rotated, and no credential value is included in this receipt.

## Recovery review and follow-up

Recorded after the 2026-10-07T10:24:27.491-05:00 status check. These later observations supersede the earlier queue snapshot above, not the historical drain result.

- Local Grok review `docs/receipts/hv/20261007T093300-0500-immutable-vice-phase0-recovery.verdict.md` returned DISAGREE, accuracy 96, completeness 97, PASS 18, FAIL 1, UNKNOWN 0. A12 found that the canonical handoff and the whole-goal plan omitted the operator's discriminating-mocks-first and full-applicable-suite directive. Both files now contain that directive; their text was read back. Independent acceptance is still pending.
- The original CLI launch used aggregate `json` output, not a complete NDJSON event stream. The original output is preserved as `.raw-result.json`. The matching `.response.jsonl` was recovered by copying the reviewer's full 488-line `updates.jsonl` stream. Source and copy have SHA-256 `FFE4449413612456C32F41BC0295FDDF375FD41FBA2FC8D1330BB54454188709`. The resumed review uses `streaming-json` directly.
- That reviewer's completed primary MCP turn contains its full verdict prose but not a parseable final structured verdict object. It is not a complete persistence receipt. The resumed reviewer must use a new review turn, persist prose plus the complete structured verdict before completion, then parse and compare the primary readback.
- `failsafe-drain-raw-tool-receipt.json` and `authentication-raw-tool-receipts.json` preserve the actual earlier tool outputs, independently checkable against the source rollout and call IDs embedded in those files. These are not reconstructed success summaries.
- `requirement-read-timestamp-drift.json` records two pure `getTr` reads returning changing creation/update timestamps. Those timestamps cannot prove historical persistence timing. The report initially failed with `The storage backend is currently unreachable. Retry the operation once connectivity is restored.` The work stopped at that reporting boundary.
- A subsequent supported session operation drained the retained report with `replayed=1, failed=0`. Current server triage readback confirms report `triage-report-9015f95eff514dc396825e180ce514f4`, group `triage-group-1506db9162904d87`, status `grouped`, routed to `F:\GitHub\McpServer`. Intake is accepted; the timestamp defect is not claimed fixed. A first group lookup incorrectly used the ViceSharp context and returned not found; the supported native triage status lookup with the actual McpServer workspace returned the record.
- Session logging was independently confirmed functional: `Codex-20261007T135626Z-plugin-session` exists in server history and new turn `req-20261007T151956Z-turn-d4df` completed with `persisted=true, degraded=false, queued=false`. This was a new turn in the earlier session, not another newly created session.
- `git diff --check` returned exit 0 with no output after the A12 edits. The next review will recheck after this receipt update. No implementation, native source change, build, cleanup, commit, or push is claimed.
