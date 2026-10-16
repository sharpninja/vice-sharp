# Hostile review: Phase 0 readiness after PHI2/AEC and evidence remediation

You are the next designated independent local reviewer: Claude Code `claude-opus-5-5`, effort `max`, in `F:\GitHub\vice-sharp`. The preceding review was local Grok 4.7 xhigh and has exited with code 0. Review only. No product/plan edits, native VICE writes, builds, cleanup, commit, push, web/cloud execution, reviewer subagents, fallback model, or artificial turn/time cap. Use PowerShell.MCP for all shell work. You may write your own review receipts and use your supported Claude MCP session/triage/failsafe methods.

## Required first action and current human authority

Run add-profile first: read `C:\Users\kingd\.codex\skills\add-profile\SKILL.md`, then `C:\Users\kingd\.claude\profile\add-profile.md` and every required markdown there in full, excluding only `add-profile.grok.md`. Record actual count. Read `C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md` for the A/B/C/D review contract. Current explicit human corrections override older profile defaults:

- Mocks must prove tests green before production implementation. Deliberately incorrect mocks must be rejected by discriminating assertions while the test suite remains green. No red-production gate.
- Alternate local Grok 4.7 xhigh and local Opus 5.5 max, including remediation reviews. This is the Opus slot.
- "If you genuinely cannot save to MCP, you must use the fallback." Attempt supported primary persistence first. A genuine failure authorizes complete-content supported failsafe retention and continuation, not a false primary-success claim.
- The operator approved preserving/quarantining five unsafe historical cancellation records, with verified copies and historical primary turns unchanged. They must not be replayed.
- The operator approved the exact mock-suite boundary: "Yes, mocks first; retain all real gates." Full pre-implementation suite means every AC-mapped contract test in a new native-safe project. Every existing real-emulator acceptance gate remains mandatory, unwaived and not marked passing from mocks.
- Use sortable America/Chicago timestamps with offset. Include completionPercent and ETA (null when unknown) in JSONL progress/final records. No broad environment audit. Perform only the required workspace/plugin trust bootstrap and checks directly necessary for this review.

## Session isolation and full persistence

The launcher sets a fresh `MCP_CACHE_DIR_OVERRIDE`, dedicated `MCP_FAILSAFE_DIR` and `MCPSERVER_FAILSAFE_DIR`, Claude host/agent/root, actual `MCP_SESSION_MODEL=claude-opus-5-5`, and workspace before hooks start. Your PowerShell.MCP console is a separate persistent process and may not inherit those settings. Verify its location and set those same review-specific values there before plugin use. Do not alter the parent's Codex console or reuse another review's session cache.

Active installed Claude plugin observed by the parent: `C:\Users\kingd\.claude\plugins\cache\mcpserver-local\mcpserver\1.119.0`. The marker still names 1.118.0; Grok already retained version-mismatch triage. Inspect actual identity and supported bootstrap outcome, disclose genuine mismatches, and use triage/failsafe. Do not install, repair, reconfigure, or broadly inspect plugin repositories. Read the marker without printing its API key, including copies embedded in its prose. Replace the actual value everywhere before emitting marker text.

Use the plugin's supported generated session identity or recover your isolated hook-created session; do not handcraft a session ID. Use the real Pascal-case `ClaudeCode` identity and actual model. Plugin environment defaults may replace the model with a generic name, so verify the dedicated review's model through supported readback. Do not directly edit YAML/cache/DB. No raw REST. Do not drain unrelated queues or cancel old turns. Incidental infrastructure failures are not authorization for a repair detour.

Store the entire verdict on your own review turn: full prose, every finding, A/B/C/D claims, scores, and one standalone `=== VERDICT JSON ===` object. Complete your turn, then prove whole-text equality and parsed-JSON equality against primary readback. If primary saving genuinely fails, retain that exact full content through the supported failsafe, record absolute queue paths and SHA-256, label PrimaryReadback false, and finish the review under the operator's fallback authorization. A summary-only record is not acceptable. Parent session queries sometimes return backend_unavailable after a long delay even when writes succeed; retain any successful raw query response in the workspace so exact readback can be independently inspected without treating a later failed query as success.

## Decision requested, without narrowing the whole goal

Evaluate all applicable hostile surfaces: A requested claims, B workspace rules, C effective requirements/AC/mappings, and D the active plan holistically. The five-record quarantine is a separately classified operator action, not a new product FR.

Decide whether the corrected Phase 0 prerequisites and decision-complete Phase 1 plan permit BEGINNING the mock-contract tests. The planned project does not exist yet; no new tests or production code are claimed implemented. No mock suite, real suite, parity, VSF capability, native restoration, goal or requirement is claimed complete. Do not require execution of the still-gated future tests as evidence that those tests already passed. Conversely, do not grant production implementation: it requires a later complete mock suite and separate hostile test gate.

Full goal remains the external handoff-completion plan. Real emulator regression failures, requirement recovery, all active/legacy ACs, immutable VICE, canonical VSF, real xvic half-cycle lockstep and settings visibility remain mandatory at their applicable gates. A narrow preparation review cannot waive them. All full real suites retain zero failed/skipped/aborted gates; the Phase 1 mocks are not VICE behavior evidence.

## Active artifacts

Use the exact fresh file hashes in this request JSONL; compare them before and after review:

- `F:\GitHub\vice-sharp\HANDOFF.md`
- `F:\GitHub\vice-sharp\docs\plans\PLAN-VICE-IMMUTABLE-001.md`
- `F:\GitHub\vice-sharp\docs\plans\PLAN-VSFLOCKSTEP-001.md`
- `F:\GitHub\vice-sharp\docs\requirements\technical\TR-Cycle-Accuracy.md`
- `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`
- Generated wiki `Plan-Immutable-Native-VICE.md`, `Plan-Native-VICE-VSF-Lockstep.md`, and `docs/wiki.yaml`.

Prior full Grok review base:
`F:\GitHub\vice-sharp\docs\receipts\hv\20261007T131928-0500-phase0-mock-scope-grok`

Read its `.verdict.md`, `.verdict.json`, `.readback.json`, `.parent-primary-proof.json` and `.primary-query-response.json`, and consult request/response JSONL where needed. Verdict: DISAGREE, accuracy 90, completeness 84, PASS 19, FAIL 2, UNKNOWN 1. Completed primary session `GrokCode-20261007T182902Z-hv-phase0-mock-scope`, request `req-20261007T182902Z-001-hv-phase0-mock-scope-grok`, actual model grok-4.7. Parent verified all 19873 verdict characters and embedded JSON against retained raw primary response; its fresh query failed and is not represented as successful.

## Findings and remediation to attack

1. C-cycle-ac4: the old Phase 1 group 5 incorrectly mapped a bus-address mismatch to `TR-CYCLE-001` AC4. The corrected plan explicitly disclaims that association and adds `CpuPhi2AecContractTests` for the actual criterion: CPU accesses only during PHI2 with AEC asserted. Verify every phase/AEC/read/write/idle case, missing observations, and discriminating incorrect-mock controls. Idle remains legal during an eligible phase. These are test-local contracts, not a second production timing algorithm. The bus comparator remains only partial contract coverage for `FR-SNP-005` AC3 and `TEST-SNP-002` AC4; their restored-checkpoint, 100000-cycle and complete-state real gates remain mandatory.

   The existing real fixture's XMLDoc also incorrectly names AC4. Its actual assertions are unchanged and not counted as PHI2/AEC evidence. The plan explicitly records correction of that annotation once harness verification is native-safe. Editing that source now would trigger the old native-mutating build; do not run it or pretend the comment proves coverage. No assertion or real gate is dropped.

2. C-cycle-status: supported `workflow.requirements.updateTr` changed only `TR-CYCLE-001` status from closed to pending. Independent getTr readback confirms seven unsatisfied ACs and exact equality of all other content excluding nonhistorical projection timestamps. Receipt `.cycle-status-remediation.json` contains full before/after. Verify live current-layer requirements and mappings through supported tools where available. Requirement text and all seven ACs are unchanged; no requirement is retired or marked satisfied. The canonical markdown status was corrected and the supported wiki export rerun.

3. B7-replay UNKNOWN: all four full file versions are now retained under `docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok-b7-reconstruction/`. They are explicitly reconstructed after review, not contemporaneous backups. Verify full bytes:
   - HANDOFF.after-human-steering.md: E5777C5847ED42A9E856B7F155AB85C6178432696879F655BDDD6711D30869D6
   - goal-plan.after-human-steering.md: AC8B715D6BA30DDAABEC1F6A53E88667334570347617DFBAEC8A8764C81BFCBF
   - HANDOFF.before-human-steering.md: 24BC7C35F8A9BF565CECBFFDD7422CAFB300B4A6C387E812C90054FB026F197A
   - goal-plan.before-human-steering.md: 8BC3A6818E5B0276870A22ACC559874BFE66FD6322A8C4B5F6E80B3571F373EC
   Independently remove only each exact fallback paragraph plus its following blank line from the retained post-steering bytes in memory and reproduce the corresponding pre-steering bytes/hash. Exact paragraph text and original launch hashes are in `20261007T120522-0500-mock-green-wiki-opus.mid-review-human-steering.json`. No live source or historical verdict was restored or rewritten. Both before and after blobs now exist, so this can be checked without trusting an asserted reconstruction result.

Recheck prior PASS areas rather than assuming them: all integrity ACs in Phase 1 groups 1-4, `TEST-VICE-001` nonempty condition/four ACs, outside-native build boundary, full mock-suite scope, retained real gates, primary resume TR correction, VSF zero-skip broad Test gate, approved five-record quarantine, full prior verdict persistence and supported retry audit. The earlier review bases and exact evidence paths are in the current handoff and latest Grok receipt. TEST requirement DTO `condition` is not FR/TR `body`; don't manufacture an empty-body defect.

## Verification boundaries and retained evidence

- New planned test project: `tests/ViceSharp.OracleContract.Tests/ViceSharp.OracleContract.Tests.csproj`, no production/TestHarness/native references or native build targets. All project tests unfiltered, valid and deliberately incorrect mocks green. Root/test/ancestor import inspection: prior Grok base `.parent-readonly-preparation.json`. Still inspect actual files; this is a proposal, not a built project.
- No normal TestHarness, solution or Nuke build during this review. `BuildViceNative` currently patches/generates under native/vice and violates the immutable-target boundary.
- Read-only source baseline: `docs/receipts/native-vice/20261007T133100-0500-readonly-source-baseline.json`, 6374 files hashed, HEAD/gitlink cc67418cba58e71d8e70fd353e9de47199ad2075, 12 tracked modifications, 2527 untracked. It is dirty-source preservation evidence, never a clean oracle. Preserve it and native/vice unchanged.
- Current generated-plan hash proof: prior Grok base `.wiki-after-cycle-proof.json`. Canonical immutable plan hash C7E1FE6C3BB5598E79EBD4092FEB5CEE51FF8BAF5648D2CC704E6FD5FA398E06; VSF plan hash C15DA385A6F327191581F7B112F4838AA7644CA51A50F1D81EEDDD57E576D183; both matched projections.
- Traceability command exit 0 is only a diagnostic: 235 canonical IDs, 149 referenced, 86 missing, 1011 noncanonical references. Full output in prior Grok base `.traceability-after-cycle.txt`. These preexisting gaps remain whole-plan work, not declared fixed.
- Five-record quarantine proof: `docs/receipts/hv/20261007T125315-0500-failsafe-quarantine/verification.json`, five byte-identical read-only backups/quarantines, zero replay/deletion, historical primary sessions unchanged. Do not replay them.
- Later valid parent fallback records are not those corrupt cancellations. Retitle/triage retry proof: `docs/receipts/hv/20261007T133800-0500-retitle-failsafe-replay/verification.json`, replay attempted but backend timeout, originals retained. A later turn-update timeout was also retained through supported fallback. Grok's isolated version-mismatch triage remains in its `.mcpServer/grok/reviews/.../failsafe/pending`, disclosed in its readback. Primary storage is intermittent; preserve complete records and distinguish primary from queued rather than demanding unsafe cancellation replay or deleting evidence.

Parent current session `Codex-20261007T135626Z-plugin-session`, turn `req-20261007T184921Z-prompt-ccb7`. Parent has not created new mock/production code, changed native VICE, or marked a phase/goal complete. Query human text instead of request ID when server text search does not index IDs.

## Outputs and threshold

Receipt base:
`F:\GitHub\vice-sharp\docs\receipts\hv\20261007T141100-0500-phi2-ac-remediation-opus`

Launcher already retains full `.request.jsonl`, `.response.jsonl` and stderr. Do not overwrite them. Write `.verdict.md`, native-serialized `.verdict.json`, `.readback.json`, and a retained raw successful primary query response when available. Include every PASS/FAIL/UNKNOWN with evidence, both justified numeric scores, actual identity/model/effort, and explicit Phase0Accepted, MockValidatedTestPhasePermitted, ProductionImplementationPermitted, WholeGoalComplete. The latter two remain false. Only AGREE if all applicable findings pass with both accuracy and completeness at least 98; do not inflate scores. No deadline, no model substitution. Return the final verdict after complete primary persistence or verified full authorized failsafe retention.
