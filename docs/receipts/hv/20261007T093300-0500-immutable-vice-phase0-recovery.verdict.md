# Hostile verdict: PLAN-VICE-IMMUTABLE-001 Phase 0 recovery gate

Work class: project requirements and documentation recovery. This is the gate between the recovered requirements and Phase 1 integrity tests. It is not an implementation claim, a native restoration, an xvic parity claim, a VSF completion claim, a requirement-satisfaction claim, or a Phase 0 done claim.

Sortable local timestamp: 20261007T095341-05:00
TimestampUtc: 2026-10-07T14:53:41.384Z
ValidatorIdentity: GrokSubagentHostile
Agent: GrokCode
Model: grok-4.7
Effort: xhigh
add-profile: yes. 22 non-skill profile markdown files read in full. add-profile.grok.md was excluded.
ProfileFileCount: 22
SessionId: GrokCode-20261007T143841Z-immutable-vice-phase0-hv
RequestId: req-20261007T143841Z-001-immutable-vice-phase0-recovery
TurnId: 107849
Marker signature: true, via plugin Test-MarkerSignature on AGENTS-README-FIRST.yaml. markerWrittenAtUtc 2026-10-07T14:04:33.1253985+00:00. Health nonce echo: true, HTTP 200.
Installed Grok plugin used for wrapper calls: mcpserver-grok 1.119.0 at C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f. The refreshed marker says Grok plugin_version 1.118.0. That is marker drift for this reviewer. It did not stop signature verification.
Request jsonl: F:\GitHub\vice-sharp\docs\receipts\hv\20261007T093300-0500-immutable-vice-phase0-recovery.request.jsonl
Response jsonl: F:\GitHub\vice-sharp\docs\receipts\hv\20261007T093300-0500-immutable-vice-phase0-recovery.response.jsonl
This verdict: F:\GitHub\vice-sharp\docs\receipts\hv\20261007T093300-0500-immutable-vice-phase0-recovery.verdict.md
Structured twin: F:\GitHub\vice-sharp\docs\receipts\hv\20261007T093300-0500-immutable-vice-phase0-recovery.verdict.json

OverallVerdict: DISAGREE
Phase1Permitted: false
Phase0Done: false
AccuracyScore: 96
CompletenessScore: 97
PassCount: 18
FailCount: 1
UnknownCount: 0

No requirement was marked satisfied. No TODO or plan step was marked done. No product test or build was run. No native file was edited. The patching scripts were not executed.

## Why this is not AGREE

Claim 8 is not fully true. PLAN-VICE-IMMUTABLE-001 Phase 1 contains the 2026-10-07 operator instruction: validate unit contracts against discriminating mocks and show them green before production implementation, show that incorrect mocks are detected, keep the expected red result against the current production boundary, and do not advance a gate on focused green results when that gate requires the full applicable suite. HANDOFF.md does not contain that instruction. The whole-goal plan at C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md does not contain it either. Both were in the claim that plans and the handoff incorporate the instruction. The plan exit gate requires this hostile review to return AGREE with accuracy and completeness at least 98 and with zero FAIL or UNKNOWN before Phase 1 begins. This review does not return that. Phase 1 must not start.

Accuracy is 96 because claims 1 through 7 and the immutable-plan half of claim 8 were re-verified, and the handoff plus whole-goal half of claim 8 was not. The score is not 7/8 and it is not 98. Completeness is 97 because the drain counts and the original 401 were taken from the primary turn and the accepted triage record rather than replayed, and because getTr createdAt and updatedAt change to the call time on every read so they are not a durable insert timestamp.

## Surface A

A1 PASS. Native sessionlog_query found sourceType Codex, session Codex-20261007T135626Z-plugin-session, turn req-20261007T135720Z-turn-866a, session status in_progress, turnCount 1. The turn holds seven actions and five dialog items, including the requirement decision, the failsafe drain observation, and the launch of this review. That is persistence of appendDialog and appendActions. I did not call those wrapper methods again.

A2 PASS. Marker agent_plugins.agents.Codex.plugin_version is 1.118.0. The installed marketplace manifest C:\Users\kingd\.codex\plugins\cache\mcpserver-codex-plugin\mcpserver\1.118.0\.codex-plugin\plugin.json has name mcpserver and version 1.118.0. The marketplace path for 1.119.0 is absent. A historical local cache copy remains at C:\Users\kingd\.codex\plugins\cache\mcpserver-local\mcpserver\1.119.0\.codex-plugin\plugin.json. The README sentence that a 1.119.0 cache path disappeared is true of the marketplace path and false if read as "no 1.119.0 copy remains." I do not fail the narrower claim that the installed Codex manifest matching the marker is 1.118.0.

A3 PASS. triage_status on F:\GitHub\vice-sharp returned not_found. The same report id on F:\GitHub\McpServer returned status grouped, title "ViceSharp marker key rejected by REST and Codex plugin while native MCP works", createdUtc 2026-10-07T14:03:25.0969227+00:00. That is acceptance into triage, routed off this workspace. It is a diagnosis record. It is not a hostile verdict. I did not replay the stale-key 401. The current marker signature and health nonce succeed.

A4 PASS. The primary turn dialog records workflow.failsafe.drain scanned 4, replayed 4, failed 0, quarantined 0, skipped 0, aborted false. I did not run drain again. Current v4 queues, counted without creating directories: Codex pending exists with 0 files, GrokCode pending exists with 0 files, both quarantine directories are absent. The empty directories do not prove the historical 4. The primary turn is the source for that count.

A5 PASS. Live workflow.requirements.getTr for TR-VSFLOCKSTEP-RESUME-001 contains the canonical description at docs/requirements/technical/TR-Native-Vice-Snapshot.md line 73 and both acceptance-criterion texts at lines 77 and 78, without the markdown numbers. AC ids are tr-vsflockstep-resume-001-ac-1 and tr-vsflockstep-resume-001-ac-2. status pending. scopeStartLayerKey layer-1. Both isSatisfied values are false. requirement-before.json still has the obsolete patch mandate and the same AC ids, also pending, layer-1, and unsatisfied. The new text is the external adapter and the never-apply rule already written in the approved immutable plan. It is not a new relaxation. Two successive gets returned createdAt and updatedAt equal to each call (2026-10-07T14:47:32Z, then 2026-10-07T14:49:03Z). Those fields move on read. They are not evidence that this review inserted the row, and they are not a durable created time. The saved readback at 14:25:27Z has the same corrected text.

A6 PASS. Live records, all pending, layer-1, and every structured AC unsatisfied: FR-VICE-001 (4), TR-VICE-ORACLE-001 (5), TEST-VICE-001 (4). FR-VICE-001 AC1 text is present. listMappings returns FR-VICE-001 to TR-VICE-ORACLE-001 and TEST-VICE-001. FR-SNP-005 and FR-VSFLOCKSTEP-001 each map to TR-LOCKSTEP-VSF-001, TR-VSFLOCKSTEP-RESUME-001, TEST-SNP-002, and TEST-VSFLOCKSTEP-001. Those five records are also pending with zero satisfied criteria. That set is adequate to author the Phase 1 preflight, wrapper-boundary, and fail-closed tests. It is not implemented. Vic20HalfCycleComparatorTests stays an existing plan fixture, not a new AC.

A7 PASS. docs/Project/wiki/github/Technical-Requirements.md lines 1107-1115 contain the corrected description, both AC texts, status pending, and the sentence that native/patches/vice-shim-runtime.patch is diagnostic legacy evidence and is never applied. docs/Project/wiki/azure/.mcp-requirements-manifest.json has generatedAtUtc 2026-10-07T14:33:55.7003688+00:00 and documents []. The only other Azure entry is .order. I did not regenerate the wiki and I did not see the wrapper files[] payload. The files are the proof.

A8 PASS. git rev-parse HEAD:native/vice and git -C native/vice rev-parse HEAD are both cc67418cba58e71d8e70fd353e9de47199ad2075. porcelain -uall is 12 tracked modifications and 2527 untracked paths. The 12 paths match the preservation receipt. Patch SHA-256 90604E39B827F850A382DC13DD921141290EF2C34E1483CDC6C612A07803E9E2. Preserved vice-shim-runtime.h.preserved and live native/vice/vice/src/vice-shim-runtime.h are both SHA-256 5450FDA8CFEDB8A22BADA8CD04EE75FF830C48796592FF9EC0141E899D846259. git -C native/vice apply --reverse --check of the absolute patch path returned 0. No restore was performed.

A9 PASS. I re-ran tools/check_requirement_traceability.ps1 in this session. Canonical 235, referenced 149, unreferenced 86, noncanonical 1011, FR 132/101/31, TR 59/32/27, TEST 44/16/28, exit 0. The default exit does not fail on findings. This is not a clean traceability gate. The plan does not require a zero-finding audit before Phase 1, and the recovery discloses that. It is not a separate blocker.

A10 PASS. git diff --check at the repository root returned exit 0 with no output. I ran it twice, the second time after the requirement and wiki reads.

A11 PASS. sessionlog_query for GrokCode-20261004T145125Z-immutable-vice-remediation-hv shows session status in_progress and turn req-20261004T145125Z-001-immutable-vice-plan-rerun status completed. The response length is 10019 characters. It contains the words OverallVerdict, AccuracyScore, and CompletenessScore in prose. It does not contain the marker "=== VERDICT JSON ===" and it does not contain a quoted "OverallVerdict" JSON field. The local file docs/receipts/hv/20261004T094437-0500-immutable-vice-remediation-plan-rerun-local-grok.verdict.md line 47 does contain that JSON and records PrimaryReadback false. This review does not adopt that AGREE.

A12 FAIL. HANDOFF.md and the whole-goal plan do not contain the 2026-10-07 discriminating-mock and full-applicable-suite instruction. PLAN-VICE-IMMUTABLE-001 line 62 and the generated plan wiki do. The handoff resume discloses the recovery, the dirty native counts, the incomplete traceability audit, the historical HV gap, and the absence of a Phase 0 done claim. It preserves later blockers, including Phase 2 not exited, xvic parity not started, and VSF product work not started. Disclosure and blocker preservation pass. Incorporation of the latest BDP instruction into the handoff and the whole-goal plan does not.

## Surface B

B1 PASS. This is the inter-phase gate after requirements recovery and before tests. FR, TR, TEST, and AC for the oracle boundary exist and were read live. Phase 1 tests are not required to exist yet. No Byrd failure is taken from file timestamps.

B2 PASS. Commands used were PowerShell.MCP only. No Python. No raw REST write. No direct edit of TODO, session-log, or requirements storage. Signature and health were checked before the requirement reads. MCP_FAILSAFE_DRAIN_DISABLED was set in the console process so getTr would not drain or create queues.

B3 PASS. A scan of the Phase 0 recovery receipt directory and the 20261007T093300 hv files other than the locked live response jsonl found zero occurrences of the current workspace API key. The key was not printed. No rotation was attempted. The disclosed redaction miss is not present in the scanned receipts.

B4 PASS. The disclosed backtick and wrong-header-path mistakes did not change native bytes. The live header hash matches the preserved header hash, and the reverse-apply check still matches the preserved patch.

## Surface C

C1 PASS. The requirements that govern the next test phase have structured, testable, unsatisfied acceptance criteria and live mappings. Nothing in this recovery marks them completed or satisfied.

C2 PASS. The 86 unreferenced and 1011 noncanonical traceability findings stay open. They are not treated as coverage and they are not treated as a Phase 1 entry failure under the current plan text.

## Surface D

D1 PASS. PLAN-VICE-IMMUTABLE-001 still says a fresh review is required before Phase 1 or any done-state change, and it does not mark Phase 0 complete. Preservation is the 2026-10-04 receipt, which I re-read and re-hashed. Restoration remains Phase 3. PLAN-VSFLOCKSTEP-001 is not claimed implemented. The whole-goal plan still leaves later phases open and still names the immutable plan as the authoritative boundary plan. Its amendment-time untracked count of 1148 is labeled as the count at amendment time, not as the current 2527.

## Phase 1 permission

Phase1Permitted is false. The single blocker is A12 together with this DISAGREE verdict. The plan will not open Phase 1 until a hostile review returns AGREE at or above 98/98 with zero FAIL and zero UNKNOWN.

These are not additional blockers:

- The native tree is still the preserved dirty target. Restoration is not a Phase 0 recovery prerequisite and was not claimed.
- Traceability is incomplete and is disclosed. The plan does not require a clean audit before the red tests.
- native/build-vice-shim.sh still applies native/patches/vice-shim-runtime.patch when the reverse check fails, and ViceSharp.TestHarness.csproj BuildViceNative calls that script through Vice.Native.proj. I did not run it. The newest file under native/vice/vice/src and the explicit native inputs are older than native/vice_x64.dll (dll 2026-09-25T17:46:33Z, newest source 2026-09-25T17:46:29Z), so the incremental target would skip Exec on a test build that does not touch those inputs. That is not permission to run the script. Phase 1, when it is later allowed, must still simulate dirtiness without modifying native/vice and must not invoke the patching scripts.

## Parent errors

The three disclosed parent errors were checked only for whether they changed the verdict facts. The live API key was not found in the scanned receipts. The native header hash matches the preserved copy. No native restoration occurred. getTr timestamp movement is a separate observation and is not one of those three errors.

## Fail list

- A12: HANDOFF.md and the whole-goal plan omit the 2026-10-07 discriminating-mock and full-applicable-suite instruction that claim 8 says they incorporate. PLAN-VICE-IMMUTABLE-001 contains it.

## Unknown list

None. Drain counts and the 401 are bounded by primary records rather than left unknown. They lower completeness. They are not UNKNOWN verdicts.

## Scores

AccuracyScore 96. CompletenessScore 97. Either score below 98 forces DISAGREE even if the fail count were zero. The fail count is 1.
