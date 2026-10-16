# R0/R1 pre-application safeguards: proposed contract

Prepared by Codex, 2026-10-11. Unimplemented and unaccepted. This document resolves guard decisions for the exact 14-record draft; it does not authorize a server write. It extends, rather than replaces, R0/R1 in `../20261009T154620-0500-tape-amendment-proposal/README.md`.

## Scope and authority

R0 freezes an exact request for local Opus 5.5 max HV, then explicit operator approval. R1 applies only that approved request and verifies it. The earlier five-record approval is not approval for eleven or fourteen records. A continuation, green comparison or passing mock is not approval. The whole original plan, later Phase 3/4 start approvals, native VSF, xvic/x64sc, UI and full real gates remain unchanged.

After R0 review and explicit approval, but before any R1 mutation, the read-only guard and the mutation coordinator need discriminating scripted-mock tests that pass with zero skipped/failed/aborted cases. No production coordinator is implemented before that mock gate. R0 itself authorizes neither coordinator nor test implementation. Tape/emulator assertion implementation still follows the original R0/R1 acceptance boundary and R2 mocks-first sequence.

## Evidence and comparison contract

Freeze paths and hashes for the complete exact proposal, typed preimages, mappings, source/fixture audit, prior verdict and disclosures, guard test plan/results, full reviewer verdict and receipts, and explicit operator approval. Approval names the accepted exact packet or hashes unambiguously. Any subsequent semantic edit invalidates that binding.

Obtain fresh effective records, all fourteen typed records and seven mapping rows through the required plugin. Use collection/type plus ordinal ID; a missing, duplicate, wrong-kind or differently cased identity fails. Preserve field presence independently from its value. A missing field is not present-null, null is not an empty array, an empty array is not an omitted list, and string/Boolean/number types are not interchangeable. Object-key order is irrelevant; AC/array order and string contents are not silently normalized. Any allowed line-ending transport normalization is named and separately retained, never used to hide a semantic change.

Every comparison receipt carries record kind/ID, field path, expectedPresent, actualPresent, expected type/value, actual type/value, equality method/result and source read time/path/hash. Missing evidence never becomes a `null == null` pass. Count the inspected fields from the actual typed objects and separately count the effective projection. Check metadata omitted by that projection: area, subarea, testType, notes, layer scope and every other returned stable field.

`createdAt` and `updatedAt` are currently read-synthesized and are observations only. Retain their raw values and this limitation. Exclude only these two known fields from stable comparisons; never submit them as historical timestamps or omit all metadata. If a new unclassified field appears, stop and classify its preservation contract before applying. Historical metadata that cannot be recovered stays unknown.

Compare every old AC ID/text/flag/evidence and every new AC, not just counts. Detect duplicate IDs, lost criteria, incorrect rewritten texts, new satisfaction flags and unapproved fields/status changes. The proposed inherited media closed-to-pending transition is the sole status change; all after AC remain false. Classify all three text categories explicitly: retained unchanged, retained rewritten, added. Retaining IDs does not preserve old acceptance meaning.

For mappings, retain every old edge and non-edge field. Resolve every final target in the correct live collection, including old edges; distinguish pre-existing defects from newly proposed ones, but do not call an invalid row ready. The currently withheld nonexistent additions remain obligations for separate resolution, not silently accepted links or retired requirements. Detect an existing edge exactly to avoid duplicate creation; reject case-only or wrong-kind substitutes.

## R1 operation and failure state machine

1. Start read-only. Validate packet hashes, explicit approval, fresh baseline equality, all target identities and supported current parameter shapes. Build a native-object payload from only approved `changedFields`. Map effective body/TEST condition to typed description. Do not copy effective objects directly into an update or send absent metadata as null. Persist exact redacted parameters before any write.
2. Use the supported mixed `workflow.requirements.updateBatch` for the approved record fields. Verify its documented and mock-tested failure semantics. Do not infer server atomicity merely because a method is named batch. If its required behavior is unknown, R1 stays blocked until proved; do not test destructive cases against production requirements.
3. Re-read all fourteen full records before attempting mappings. Require exact approved after-values and unchanged stable metadata. A success response, queued response or returned count alone does not prove application. If readback differs, stop. No emulator tests start.
4. Apply supported mapping operations for the seven exact rows, only actual additions. Keep an ordered operation journal with before/desired-after values, request identity, response, uncertainty and primary readback. Record-level and mapping calls do not share one transaction; never claim otherwise.
5. On error/timeout/lost acknowledgement, do not replay immediately. Query actual state. Exact before means not applied; exact after means applied despite lost acknowledgement; mixed or third-state means stop and retain all evidence. If reads are unavailable, outcome is unknown, not failed or successful. Resume only from verified state under the still-valid approval. A partial multi-call mapping result is documented partial progress, not whole-batch success.
6. Do not issue an automatic compensating write. Any recovery requires a newly approved exact compensating delta after fresh readback, as the original R1 requires. Never overwrite concurrent changes, delete/recreate IDs, restore old timestamp guesses, or reuse the historic recovery idempotency key.
7. After all writes, independently compare every full record, AC, stable metadata field and mapping with desired state. Capture canonical document export requests, stdout/stderr and results through supported paths. Run traceability and retain all gaps. Export typography/title defects are separate unresolved findings; a clean exit code does not prove semantic completeness.
8. Persist all results using the supported primary MCP methods and verify server readback. On genuine primary or triage failure use the authorized full-content failsafe, retaining operation uncertainty. Queued-only results cannot advance done-state. Requirement-gate HV must pass before R2 begins.

## Scripted mock/provider test design

The test-facing boundary is a scripted requirements client: effective query, typed query, mapping query, mixed update, mapping operation and full readback. It records exact call order and payloads. Each fixture supplies immutable before/desired-after records and an ordered sequence of responses or exceptions. Assertions inspect complete values, preservation, calls made and forbidden calls, journal states and primary/queued disposition. A fake that returns `true` without exercising those assertions is invalid.

The same fixtures/assertions later bind to the real read-only preflight/coordinator through that boundary. Real production mutations require approval regardless of test results. A test implementation must first pin the actually supported signatures; this document does not invent an MCP route or a hidden transaction.

Required groups and expected rejection controls:

- G01 `ExactPacketAndApproval`: correct binding permits preparation; absent approval, stale hash, changed nested AC or approval for the prior scope rejects before any write. Corrupt fake: reports approved anyway.
- G02 `UniqueOrdinalTypedIdentity`: one exact record per kind/ID succeeds; missing, duplicate, wrong-kind and case-only identities reject. Corrupt fake: selects first or substitutes another collection.
- G03 `PresenceAndType`: present-null, absent, empty array, string, number and Boolean are distinguished. Corrupt fake: null-coalesces missing properties or casts values before comparison.
- G04 `StableMetadata`: every unmodified field survives payload generation and readback, including fields absent from the effective projection. Corrupt fake: clears notes, drops scope, changes area/subarea/testType or introduces an unknown field without classification.
- G05 `VolatileReadTimestamps`: changed synthesized timestamp observations alone do not report stable drift, and they never enter the update payload. Corrupt fake: ignores all metadata or writes observed times as historical values.
- G06 `ExactChangedFields`: only approved fields are emitted with correct API names; body/condition map to description. Corrupt fake: sends body, an empty unmodified field, whole effective projection or an unapproved status.
- G07 `AcIdentityAndMeaning`: every old ID and exact after-text is checked with uniqueness/order/flags/evidence. Corrupt fake: keeps total count but drops/replaces an ID, retains stale text, strips evidence or marks a criterion satisfied.
- G08 `MappingExistenceAndPreservation`: validate all old/final targets in correct collections, exact additions and metadata. Corrupt fake: admits a missing/wrong-kind target, deletes an old edge or duplicates an existing edge.
- G09 `DriftBeforeFirstWrite`: a fresh baseline change rejects without calls to update/mapping. Corrupt fake: compares a cached projection or resumes with a stale approval binding.
- G10 `BatchSuccessNeedsReadback`: successful response plus exact full after-readback permits mapping stage. Corrupt fake: begins mapping before reading all records or accepts count-only/queued success.
- G11 `BatchRejectedNoMutation`: scripted atomic validation rejection leaves all records/mappings at before-state and records the failure. Corrupt fake: changes one record, calls mappings or reports success. This is the expected contract, not evidence that the live batch already satisfies it.
- G12 `LostAcknowledgementApplied`: timeout followed by exact after-state records applied and never repeats the write. Corrupt fake: retries blindly or reports not applied.
- G13 `LostAcknowledgementUnapplied`: timeout followed by exact before-state records not applied; any later retry requires freshly validated inputs/approval and documented retry identity. Corrupt fake: reports success or skips revalidation.
- G14 `UnknownOrMixedOutcome`: unavailable readback, partial batch or concurrent third-state stops before further writes and retains uncertainty. Corrupt fake: treats missing readback as before-state, continues mappings, or performs compensation without approval.
- G15 `PartialMappingSequence`: two verified row updates followed by failure retain exact per-row states and stop/reconcile before further mutation. Corrupt fake: claims all-or-nothing, retries completed additions or drops already-existing edges.
- G16 `FinalWholeScopeReadback`: all fourteen records and seven mappings plus stable metadata agree; omissions/extra fields/wrong text/missing target reject completion. Corrupt fake: verifies only the CPU record or only changed fields.
- G17 `PersistenceAndExportEvidence`: full verdict, parameters, outcomes and required export/traceability receipts are retained with confirmed versus queued state. Corrupt fake: loses findings, reports a queue as primary, omits failed export output or labels traceability exit0 as per-AC coverage.
- G18 `NoUnapprovedRecoveryOrDone`: failure never deletes/recreates records, alters native files, replays historical queues or sets done/satisfied. Corrupt fake: issues any such call; the assertion must detect the call even if final data values look correct.

For each group the correct scripted outcome passes; every listed corruption is rejected inside a green test. Preserve exact fixture, assertion and mock hashes before implementing the real coordinator. Require the full applicable current-plus-prior guard suite, not a selected success case, with no failed/skipped/aborted tests. A future green guard suite proves its declared contract only, not tape emulation, native observability or safe live deployment by itself.

## Current evidence and open gate

The adjacent preparation receipt proves 112 effective-field comparisons and fourteen full stable typed preimage matches. It does not execute G01-G18, prove batch atomicity, exercise a timeout, test compensation, or establish a production update signature. Those executable safeguards, complete tape/media fixtures, canonical handoff reconciliation and new full Opus HV remain open. No guard result is marked accepted here.
