# Consolidated R0 requirement-amendment review packet

Prepared by Codex on PAYTON-LEGION2, 2026-10-11T12:26:24-05:00. Review candidate only. No requirement application, implementation acceptance or whole-plan completion is claimed.

## Decision requested from the reviewer

Determine whether the corrected **fourteen-record requirement-text amendment and its ordered implementation plan** are sufficiently accurate and complete to present for the operator's exact-delta approval. Review all applicable A/B/C/D surfaces, the complete original plan and all 35 prior finding dispositions. The reviewer owns the verdict; this packet supplies no prewritten acceptance.

The midnight eleven-record review is terminal: DISAGREE, accuracy 93, completeness 84, 35 PASS, 6 FAIL, 0 UNKNOWN and 1 N/A. This is a new remediation review, not another retry of that completed review. Its [complete verdict](../../hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/verdict.md) and JSON twin remain controlling evidence of what failed.

R0 is review of an **unapplied requirement amendment**, not the executable mock, application-coordinator, native-observer or emulator gate. The prior reviewer explicitly treated executable AC tests as a later gate for this unapplied amendment. Missing future implementation evidence is disclosed and remains mandatory at its actual gate. That distinction does not excuse incomplete or untestable requirement text, an undecided application design, or an unsupported claim in this packet.

## Exact delta and denominator

The sole candidate payload remains [proposed-field-delta.json](../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json), SHA256 `320887C5F2F09154F1EC7970B041A638632741A7750E838FE05F77883BB1DA83`.

Fourteen existing records, not new record creation: FR-RUNTIME-TAPE-002, TR-TAP-EDGE-001, TR-TAPE-EDGE-001, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001, FR-TAP-001, FR-TAP-002, FR-TAP-003, FR-TAP-004, TEST-TAP-001, FR-BACKFILL-MEDIA-001, FR-CPU-004, FR-PRF-003 and FR-TAP-005. Its seven complete mapping rows are reviewed separately from record updates.

All 66 original AC IDs are retained: 56 texts rewritten, ten unchanged, 24 added, 90 resulting criteria. ID continuity is not semantic continuity. `consolidated-coverage.json` contains 82 amendment coverage rows plus eight unchanged baseline criteria, with 117 distinct proposed group names after replacing the five inherited media placeholder rows. These are specification bindings, not 117 passing tests. The unchanged eight remain required for whole-plan acceptance.

`live-baseline.json` retains complete current typed records and value-bearing comparisons. At 2026-10-11T12:26:24-05:00: 112 projected-field checks, 121 complete typed-field checks, seven mapping-row comparisons and 38 mapping-target references had zero differences or missing targets. These reads establish a point-in-time baseline, not a lock or proof against concurrent writers. Read timestamps fabricated by adapters are not historical persistence evidence.

## Packet reading order

1. This index and [all finding dispositions](FINDING-DISPOSITIONS.md).
2. The complete immutable [original goal-plan reference](../../handoff/20261011T112946-0500-canonical-reconciliation/goal-plan.reference.md), hash `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`, and current [HANDOFF.md](../../../../HANDOFF.md). Tape is not the full objective.
3. Exact delta, [capacitor cases](../20261011T080155-0500-capacitor-guard-remediation/CAPACITOR-SOURCE-AND-CASES.md), [application contract](../20261011T080155-0500-capacitor-guard-remediation/R0-R1-GUARD-CONTRACT.md), consolidated coverage and live baseline.
4. [Native hidden-state/version/turbo audit](../20261011T072531-0500-tape-state-composite/N07-N12-SOURCE-AUDIT.md), [D64/media specifications](../20261011T083152-0500-media-fixture-specification/README.md), [motor matrix](../20261011T085402-0500-tape-motor-matrix/README.md), and [recording/wiring contract](../20261011T092059-0500-tape-recording-wiring-contract/README.md).
5. [Direct test dispositions](../20261011T095710-0500-tape-test-disposition/README.md), [adjacent test dispositions](../20261011T115707-0500-adjacent-tape-test-disposition/README.md), [typed API/guard audit](../20261011T102600-0500-requirements-api-guard/README.md), and [native observation contract](../20261011T110100-0500-tape-observation-contract/README.md).
6. Original failed verdict, preserved predecessor drafts, current input-pin manifest and explicit remaining gates below. Older supplements' open-dependency lists are historical preparation checkpoints, not declarations that later supplements already passed review.

## Ordering and approval boundaries

1. **R0 now:** review this exact requirement delta, its source-derived specifications, complete finding response and implementation safeguards. No requirement or mapping write.
2. **Operator approval:** after accepted HV, obtain explicit approval of this exact fourteen-record delta and its seven mapping rows. The previously approved smaller proposal does not cover it. Do not silently apply successive overlapping drafts.
3. **R1 coordinator:** write the specified guard tests and independent mocks first. Every planned negative control must reject within a green harness. Only then implement the coordinator. Bind the same assertions to its real supported MCP operations. No production-requirement mutation used merely as a test fixture.
4. **R1 application:** require an explicitly coordinated exclusive-writer window, or a separately verified conditional-write capability. No such server capability is presently proved. The default is an operator-coordinated writer window, not an invented lock API or authority to stop other agents. Refresh complete typed records and mapping rows immediately before each approved action. Any preimage drift stops application and requires renewed review of the changed delta. Use sparse approved record fields, then complete mapping rows with all retained links. Requirements and mappings are not one cross-API transaction. Lost acknowledgement means read actual state and classify before/after/mixed/unavailable; never blind retry, silently repair or compensate. Every resulting field and unaffected field must be read back. Follow the exact API/guard contract, not the projection-only API shape.
5. **R2/R3:** approved criteria drive discriminating green mocks, then the same inputs/assertions bind to real managed/native providers. Materialize legal fixture inputs, establish external native observation and fault reachability, retain failed attempts, and run full applicable current-plus-prior suites with zero failures/skips/aborts. No source check, isolated filter or method inventory substitutes for these gates.
6. **Whole-plan exit:** retain all original requirements recovery, dense-session proof, immutable xvic/x64sc half-cycle parity, canonical native VSF, system-specific UI settings and cumulative/HV obligations. Only independently accepted evidence permits done-state changes.

Historical Phase 3 start authorization and narrow 3a/3b evidence are not revoked by this packet. Old generic wording asking for initial Phase 3/4 approval again is superseded by the reconciled handoff's provenance rule. That history does not approve the expanded fourteen-record amendment or bypass immutable/native prerequisites.

## Later gates explicitly not claimed

- The API guard's 18 groups/69 controls, observer's ten conformance groups/44 controls, motor's 64 vectors/366 declared projections, recording/wiring's 117 examples, capacitor cases and media recipes are unexecuted specifications. The reviewer must judge their adequacy for the proposed work, not count them as passed tests.
- Native clock observation is not full tape/alarm/pipeline observation. The candidate alarm observer lacks demonstrated transient completeness and noninterference. Missing native values fail closed. Native VICE remains immutable; external observation/fault wrappers need their own tests and proof before use.
- Native VSF is canonical and mandatory, not a neutral getter. Save/load can advance other emulated state and does not serialize every process-static tape/audio/RTC field. Compare declared fresh-process/reset/reattach/reload histories and capture preludes without pretending omitted state is zero or recreating native behavior from managed expectations.
- All five named turbo loaders remain required successful workloads. Their actual lawful fixture/firmware/resource manifests and native execution receipts remain to be materialized. Matching two failures is not successful parity; no invented minimum pulse-width guarantee or universal one-cycle machine guarantee.
- D64 36..42-track/product-scope reconciliation and the separate true-drive AC proposal retain their own approval boundaries. Missing TR-MEDIA-D64-001/TEST-D64IO-001 are not silently created or linked by this delta. Broad media acceptance remains unsatisfied until those dependencies are properly specified, approved and proved.
- Existing tests are retained. The 63 direct and 47 adjacent method audits record assertions and proposed dispositions, not a full passing regression run. Legacy Xbox/UWP evidence is retained despite implementation being out of scope. No test is weakened, deleted or skipped by this packet.
- Export corruption/retitled headings, old timestamp/history provenance gaps, credential-exposure disposition and historical hook/process violations remain disclosed. They cannot be erased by fresh documents or relabeled as accepted history.

## Corrections and process disclosure

I incorrectly described two adjacent-audit edges as CB1. The immutable `vic20datasette.c:37-48` uses VIA2 CA1. Only the JSON gap sentence and README sentence were corrected. All three original receipt files are copied and hash-verified in `preserved-adjacent-receipt/`; the old verification remains historical. Fresh correction hashes are in `live-baseline.json`. No requirement, test or native-source change was needed.

I also initially compared older workflow-projection objects with typed-client objects, yielding 70 field-shape differences. That is not server data drift. The valid complete comparison uses the 10:26 typed-client baseline and gives 121 equal field checks. The mistaken output path and the subsequent `.record` versus `.result` lookup error are retained in the baseline receipt.

MCP's primary route is required. Genuine save/triage failures may use the operator-authorized full-content failsafe, accurately labeled queued versus persisted. Drain remains disabled. Current wrapper status before preparation reported 25 pending/failsafe records; nothing was replayed. Reviewer uses an isolated MCP console/session and may not cancel historical turns, repair the environment or invoke a nested reviewer.

No new HV acceptance is implied by writing this index. The entire verdict, scores, findings and durable JSONL paths must be saved through supported MCP and verified by readback. Required acceptance: AGREE, both scores at least 98, zero FAIL and zero UNKNOWN.
