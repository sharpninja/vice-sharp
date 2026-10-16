# R0 requirements API and guard specification

Prepared locally on PAYTON-LEGION2 on 2026-10-11. Draft only. No requirement write, executable guard test, coordinator implementation, emulator test or new hostile review was performed in this slice.

This supplement resolves API ambiguities in the [earlier guard contract](../20261011T080155-0500-capacitor-guard-remediation/R0-R1-GUARD-CONTRACT.md). It does not apply or alter the [14-record proposal](../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json). Where the earlier contract overstates array ordering or field-name restrictions, the explicit distinctions below are the proposed correction for the next complete approval packet. Older receipts remain historical evidence.

## Authority and receipts

- Server marker identifies `1.4.40+bcae36fdce7ce31e792db9e06a17bff51019bdf9`. Source inspection uses that exact Git commit, not the dirty McpServer worktree. The historical source paths begin with `QBrainAi`.
- Plugin 1.118.0 selects `qbrain-ai-repl.exe` before `mcpserver-repl.exe`; both executable overrides were empty. Installed QBrainAi client/core assemblies report `1.4.40+5010795fe67abe3b98d294fce15794b6d2bff8e5`. Their relevant client/model/workflow blobs equal the server commit's blobs. Binary metadata is provenance, not proof of every runtime behavior.
- [api-evidence.json](api-evidence.json) retains source excerpts and blobs, installed assembly hashes, fresh typed reads, value-bearing comparisons and the unsubmitted request payloads. All 112 proposal preimage fields match fresh typed reads. Additional typed fields remain preservation obligations, not silently discarded projection noise.
- [guard-fixtures.json](guard-fixtures.json) defines scripted provider scenarios for G01-G18, with exact baseline/request references, injected deviations, expected state and forbidden calls. These are specification data, not executed tests or a mock-green receipt.
- [verification.json](verification.json) records this document slice's checks. No acceptance gate is satisfied by these checks alone.

## Exact supported call shapes

Use the required Codex plugin with native PowerShell objects and `-ParamsObject`. Do not submit any payload in this packet before R0 HV, exact operator approval, and the R1 guard mock gate.

1. `workflow.requirements.updateBatch` takes `records`, containing `kind`, `id`, and only the approved changed fields. This packet uses `description` for both FR/TR body and TEST condition. The API also supports `body` and `condition`; those names are not intrinsically invalid. The guard rejects aliases only when they differ from this exact reviewed payload, conflict, or hide unapproved fields. It must not invent a server limitation.
2. Typed reads use `client.Requirements.GetFrAsync`, `GetTrAsync`, and `GetTestAsync`, each with `id`. Full mapping reads use `client.Requirements.ListMappingsAsync` with no parameters, or `GetMappingAsync` with `frId`. Workflow mapping queries flatten rows into individual links and are insufficient as the only full-row baseline.
3. Mapping writes use `client.Requirements.UpsertMappingAsync` with **two named parameters**: `frId` and a nested `request` containing complete `trIds` and `testIds` arrays. The generic client binder matches these method parameter names. An additions-only request would replace the row and remove old links. The seven draft requests therefore include the full desired union, not just additions. Skip an already-equal row only after a fresh verified read.
4. `workspaceId`, `linkedUseCases`, timestamps, area/subarea/testType and unapproved fields are not sent in the batch patch. Their exposed preservation values are independently checked. Null patch fields preserve existing values; null cannot be used to clear notes or scope end. Empty AC arrays are materially different from omitted/null criteria and are forbidden by this amendment.

## Mapping safety and comparison semantics

The installed workflow's `CreateMappingAsync` catches every failure while reading an existing mapping, treats it as absence, and then upserts. With a timeout or backend failure, its request can omit existing links. This is source-demonstrated control flow and an inferred data-loss risk, not a destructive live reproduction. Triage accepted `triage-report-5deae93eb0764736ae97184817d14eb6`, group `triage-group-320f2143bb2556d0`. No server repair is part of this slice.

Use the typed full-row client method above. A failed baseline read blocks mutation. Only a separately observed authoritative not-found result establishes absence. Even then, it must match the exact approved baseline; these seven rows already exist.

Mappings are mathematical sets of typed target identities. The database read sorts links by target kind and ID; workflow merge also sorts. Retain raw array order in receipts, but compare mapping edges as ordinal, duplicate-free sets. Do not fail semantic preservation merely because order changes. Do reject duplicate/case-only/wrong-kind identities before writing, even though the server may normalize some of them. AC arrays and linked-use-case records retain their ordered, value-sensitive contract; do not generalize set comparison to all arrays.

Typed FR reads expose `linkedUseCases`, absent from the effective projection. Current values, including empty collections, must be retained and independently compared. The typed client model has no `createdAt`, `updatedAt`, area, subarea or testType properties. Workflow adapters may expose synthesized/derived values. Never claim historical metadata preservation from those synthesized values or claim unseen database fields were inspected. Retain both response shapes and classify their provenance. Any newly exposed unclassified field blocks application until its contract is resolved.

## Atomicity is not concurrency protection

The deployed database batch implementation validates identities/scopes, opens a database transaction, updates all records, saves, then commits. This supports the intended all-or-none requirement-record transaction in source. It is not a live failure-injection or durability test. Mapping upserts are separate calls and do not join that transaction.

The controller resolves sparse patches against existing records **before** the repository batch write lock. The transaction decorator passes reads through and wraps mutations individually. The inspected request types expose no revision, expected-preimage, ETag or compare-and-swap parameter. A fresh read followed by a write is therefore not a race-free compare-and-swap. A concurrent change can be overwritten before post-readback detects it.

R1 must remain read-only unless an exclusive requirements-writer window covering the fourteen records and seven mappings is explicitly established and documented, or a separately verified supported conditional-write mechanism is supplied. Do not invent a lock API, silently stop other agents, or equate an advisory local mutex with server-wide exclusion. Include this operational prerequisite in the operator's exact approval request. Revalidate the complete baseline immediately before the first write and before each mapping row even in that window.

The server's transaction decorator can capture a requirements snapshot and register internal rollback compensation. Its live configuration and fault paths have not been proved here. That server behavior is distinct from issuing an unapproved compensating API call from the coordinator. On an ambiguous response, query state and retain uncertainty; never assume that either commit or rollback occurred. No manual compensation is authorized by this supplement.

## Required state machine and gates

The coordinator's observable journal states are `blocked-before-write`, `ready-not-written`, `submitted-outcome-unknown`, `records-confirmed-after`, `records-confirmed-before`, `mixed-or-drift-stop`, `mapping-row-confirmed`, `mapping-partial-stop`, and `verified-data-awaiting-gates`. An API success alone never selects a confirmed state.

For every write, retain the exact request digest, invocation identity, time, response/error, full readback and comparison. The API types inspected here contain no client idempotency-key field. A local journal ID is not server deduplication. Never retry a lost acknowledgement blindly. An exact after-state suppresses replay; an exact before-state requires renewed preflight and explicit valid retry authorization; unavailable or third-state reads stop further writes. Retain all completed mapping rows on partial failure.

After the full packet's HV and exact approval, implement G01-G18 tests first. Correct mocks and all deliberate corruption controls must pass as expected inside a green harness. Only then implement the coordinator against the same assertions. The real full-record and mapping readbacks, exports, traceability and requirement-gate HV remain separate gates before R2. No product/native implementation is authorized by this document.

Remaining R0 work includes tape observer/dependency and hidden-state/loader contracts, adjacent test audit, coherent root handoff/TODO reconciliation, complete packet freeze and local Opus max HV. Midnight DISAGREE 93/84 remains unsuperseded. The full original plan, native VSF, all real-emulator gates and separate Phase 3/4 start approval remain mandatory.
