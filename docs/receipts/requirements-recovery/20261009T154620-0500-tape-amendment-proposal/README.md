# Exact tape requirement amendment proposal

Prepared locally by Codex on PAYTON-LEGION2, 2026-10-09. Workspace: F:\GitHub\vice-sharp.

**Unapplied. Not independently accepted. No implementation permission or completed phase.**

The exact proposed changes are in [FIELD-DELTA.md](FIELD-DELTA.md) and [proposed-field-delta.json](proposed-field-delta.json). The JSON includes retained before-values, exact after-values, seven before/after mapping rows, and one proposed case/source mapping for each of the 71 after-criteria. The generator performs no file writes or MCP calls.

## Decision requested after review

Approve only the eleven listed record updates and additive mapping differences, followed by supported readback and canonical projection reconciliation. This approval would not authorize product implementation, retire a requirement, satisfy any AC, approve the other media/video or drive proposal records, waive HV, or open Phase 3/4.

This is an explicit expansion beyond the previously approved five-record correction. It is necessary because the source audit found contradictory FR wording and duplicate meanings for the same TR IDs. Previous approval does not cover it.

The proposal changes these records:

- FR-RUNTIME-TAPE-002: eight AC. Retains transport/sense/recording; adds exact alarm, output wiring and lifecycle coverage.
- TR-TAP-EDGE-001: six AC. Preserves both historical meanings under the same ID: TAP decoder/version/TurboTape and datasette sense/recording.
- TR-TAPE-EDGE-001: seven AC. Preserves both historical meanings under the same ID: motor timing and tapeport peripheral/RTC/dongle semantics.
- TEST-TAPE-RAMP-001: five AC. Adds late-alarm and fast-wind cases, with actual callback time separate from stop deadline.
- TEST-TAPE-SENSE-001: eight AC. Adds output wiring, recording windows, lifecycle and non-datasette sense devices.
- FR-TAP-001: six AC. Corrects motor bit, PLAY coupling and timing assumptions; retains public motor-state and audio obligations.
- FR-TAP-002: eight AC. Corrects v0 overflow, explicitly preserves v2 and distinct native decode paths, retains public mount/eject and machine-tag obligations.
- FR-TAP-003: six AC. Corrects inconsistent pulse-value claims and separates decoder thresholds from actual machine timing.
- FR-TAP-004: five AC. Corrects machine write-line and RECORD interlock semantics while retaining SAVE, existing-image and append-position behavior.
- TEST-TAP-001: seven AC replacing its empty AC list, including all retained named turbo-loader formats, public API, audio, peripheral and real-provider gates.
- FR-BACKFILL-MEDIA-001: five AC, reusing the exact existing unapproved row from `20261009T023102-0500-legacy-media-video-ac-proposal/proposed-field-delta.json`. No other row from that proposal is included.

There are 47 existing structured AC identities and 71 proposed AC. Every existing AC ID is retained. After-values are pending and unsatisfied. Existing priorities, scope and workspace values in the effective projection are preserved. Full hidden metadata preservation remains unproven until fresh complete records can be captured.

## Why these changes return to VICE

The preceding [source audit](../20261009T154018-0500-tape-hv-source-audit/README.md) provides detailed code branches and 30 named source-derived cases. This proposal adds TAP threshold, native audio and retained public-interface/loader coverage. Sixteen selected native files match Git objects at immutable commit `cc67418cba58e71d8e70fd353e9de47199ad2075`.

Important corrections:

1. A stop deadline is not necessarily the actual stop callback time. Existing late alarms can survive STOP mode; re-enable can cancel stopping after the deadline but before dispatch.
2. FORWARD/REWIND schedules a 1000-cycle alarm. The ordinary start helper adds 32000 only when no alarm is pending.
3. Native motor-running state is asserted immediately and retained during delayed stop. Recording can occur in those intervals. RECORD/motor command ordering changes first-gap state.
4. C64 motor uses DDR-masked bit 5; write uses bit 3. VIC-20 raw PCR motor and VIA2 PB3/DDRB write behavior must match the executed source, including quirks. SX-64 is different.
5. v0 zero does not consume a three-byte extension. The block decoder and the live datasette path use different units/resource behavior. A parsed duration is not a machine-visible pulse.
6. C64/VIC-20 `tapeinit` specifies inclusive block-decoder pulse-unit bands 36-54, 55-73 and 74-100. The old cycle/byte pairs were inconsistent. Native timer/interrupt and loader evidence is still separately required.
7. Native tape audio uses the registered datasette sound chip. The proposal keeps audio required, but explicitly replaces the unverified SID EXT IN routing assertion with the pinned native audio path and sample/buffer checks.

Native source remains immutable. Source inspection proves the basis for a proposed contract, not runtime equivalence. No native binary, mock contract, production implementation, or media workload was run for this proposal.

## ID ownership and legacy preservation

Recommended decision is union, not retirement or renumbering. Each collided ID gets a single contract containing both legitimate historical obligations. Mappings add the missing links from FR-TAP-001 through FR-TAP-005 while retaining all old TR-CYCLE-001 and TEST-X64SC-LOCKSTEP-001 links.

The parent media mapping reuses its existing proposed additions. Its D64/drive/IEC obligations remain open; including that row does not approve or satisfy separate drive requirements.

Alternative rejected for this proposal: moving one meaning to new IDs now. That would add migration choices and risk severing historical evidence. It remains an operator option, but requires a different exact delta and review. Nothing is silently renamed or deleted here.

After approval and primary readback, reconcile the two sections at `docs/requirements/backfill/Classic-VICE-Edge-Case-TR-Backfill.md:391-434` with the union contracts and both sets of trace links. Preserve a byte-identical copy and verify its hash first. Update `docs/requirements/legacy-tape-acceptance-correction.md` from the verified complete after-values, including all relevant parent mappings. Regenerate supported `docs/Project` and wiki projections; do not hand-edit generated exports or import an incomplete projection.

## Public API and workload obligations stay visible

The retained `ITapeUnit.MotorState`, `ITapeUnit.Mount()` and `ITapeUnit.Eject()` AC are deliberately unchanged. A scoped `rg` over current `src` and `tests` C# files found no `ITapeUnit` or `MotorState` definitions/references; inspected `Datasette` implements `ITapeDevice`. This is an implementation/API gap, not permission to silently replace or delete those requirements. The later interface/design slice must explicitly handle it before claiming real-provider acceptance.

All five pre-existing turbo-loader names remain mandatory in TEST-TAP-001: Novaload, Biturbo, Turbotape, Cyberload and Pavloda. This proposal does not invent workload assets or claim they exist. Missing fixture/firmware/hash/observability blocks the applicable real gate. Do not replace loader success with a synthetic parser test or reduce the denominator.

## BDPv4 sequence and mutation boundaries

### R0. Freeze the exact requirement delta

1. Restore supported trusted MCP access. The workspace marker was absent during preparation, following the supported hook's `health-failed` result. No server calls were made after that failure.
2. Read the new marker without exposing credentials, verify signature/nonce through the Codex plugin, retain drain-disabled protection and query current session state. Do not restart services or replay historical cancellation records.
3. Capture full getFr/listTr/listTest records plus effective requirements and mappings. Compare every listed before-field with this retained baseline. Stop and disclose any drift. Do not overwrite unrelated changes. Capture non-projected area/subarea/testType/notes and timestamps before mutation; historical missing preimages remain UNKNOWN, not retrospectively reconstructed facts.
4. Pin this packet, the preceding audit, all cited native sources, retained primary inputs, prior full HV verdict and its previously unpinned post-launch receipts. Review the exact delta using local Opus 5.5 max, no fallback model or artificial limits. The reviewer must load its complete profile and preserve full JSONL and verdict records. Require progress/ETA entries during the run.
5. Present this exact delta and independent findings for explicit operator approval. Approval is not inferred from continuation or from the prior five-record approval. No product or test implementation starts here.

### R1. Apply approved records and reconcile projections

1. Re-read and compare the approved baseline immediately before writes. Use supported mixed `workflow.requirements.updateBatch` for record fields, limited to the actual `changedFields` shown in the JSON. Map effective `body`/TEST `condition` to the supported request field names using the currently installed schema. Omit unknown/unmodified fields rather than clearing them.
2. Use supported mapping operations for the seven explicit before/after mapping rows. Apply only real additions and retain every old edge. Requirements and mapping operations are not claimed to share one server transaction. If either fails or times out, query actual state before retrying; never replay blindly or claim all-or-nothing across separate APIs.
3. Independently read every record, AC and mapping back, compare exact text/status/flags, and compare full metadata. On mismatch, stop before tests. Any recovery uses a newly approved exact compensating delta; never delete/recreate IDs or reapply the historical 442-operation recovery.
4. Preserve originals, update canonical projections, and run supported exports. Record raw request parameters, stdout/stderr and returned formats, including failures. Generated typography/title omissions remain separate triage defects; do not hide them or mark their findings resolved.
5. Run `tools/check_requirement_traceability.ps1` and retain every reported gap. Exit 0 alone is not proof of complete per-AC executable coverage. Update the existing relevant MCP PLAN TODO and root handoff with this amendment and the following open gates, through supported operations. Do not create a competing TODO or mark it done.
6. Independent requirement-gate HV must agree at >=98 accuracy and >=98 completeness, zero FAIL/UNKNOWN, full retained result and proven MCP persistence or explicitly labeled authorized failsafe. No goal/TODO/AC satisfaction from queued-only evidence.

### R2. Green mock-contract gate before product code

Only after R0/R1 acceptance, add the named AC-derived assertions and independent scripted fakes in the native-safe `tests/ViceSharp.OracleContract.Tests` project. Do not add a native project reference or invoke the legacy native-mutating build.

The ledger in `proposed-field-delta.json` is an AC-to-case-group map, not proof that tests exist. Before writing them, lock the fixture rows and provider contract in a test-slice plan. Required observations include ordered sequence, selected machine/board/resources/media, input command/register/clock, scheduled callback clock, actual dispatch clock/offset, control input, native running flag, accepted mode, sense, pulse/interrupt targets, emitted bytes, image position/size/dirty state, retained write state, return/error/STOP effects and audio output when relevant. Missing native observability cannot be filled with invented values.

Independent mock responses must exercise the actual assertions. The wrong-provider matrix must include wrong values and clocks, omitted/extra/duplicate/reordered events, wrong ports/DDR interpretation, inappropriate writes/no-writes, incorrect return/error/partial-write effects, wrong initial-state transitions and unavailable native evidence. Expected rejection occurs inside a passing harness. No red gate or skipped required case permits implementation.

Use the full unfiltered native-safe cumulative suites, not only new tape tests:

```pwsh
dotnet test .\tests\ViceSharp.OracleContract.Tests\ViceSharp.OracleContract.Tests.csproj -c Release --logger "trx;LogFileName=tape-contracts.trx" --results-directory .\TestResults\tape-amendment-mocks --diag .\TestResults\tape-amendment-mocks\contracts.diag.log --blame-hang --blame-hang-timeout 2m
dotnet test .\tests\ViceSharp.VsfCompatibility.Tests\ViceSharp.VsfCompatibility.Tests.csproj -c Release --logger "trx;LogFileName=tape-vsf-regression.trx" --results-directory .\TestResults\tape-amendment-mocks --diag .\TestResults\tape-amendment-mocks\vsf.diag.log --blame-hang --blame-hang-timeout 2m
dotnet test .\tests\ViceSharp.NativeClockProbe.Tests\ViceSharp.NativeClockProbe.Tests.csproj -c Release --logger "trx;LogFileName=tape-clock-regression.trx" --results-directory .\TestResults\tape-amendment-mocks --diag .\TestResults\tape-amendment-mocks\clock.diag.log --blame-hang --blame-hang-timeout 2m
```

Validate paths and cumulative applicability against the current accepted native-safe plan before running. Additional applicable prior suites remain mandatory. Retain stdout/stderr, TRX, diagnostic logs, exit codes and immutable-native pre/post inventories. All required cases pass, none skipped/failed/aborted. Then independent mock-gate HV. These commands are proposed future gates, not commands run for this artifact.

### R3. Production binding and full real gates

Production starts only after green mock acceptance and all original start-approval prerequisites. Bind the same fixture inputs and assertions to managed and immutable native providers. Implement managed behavior or external wrappers only, keeping the native target unchanged. No first-Tick bypass, inferred native events, synthetic success or assertion weakening.

Run full applicable cumulative real-emulator gates, original xvic half-cycle repair pairs, x64sc/C64 regression pins and native VSF obligations. Keep the original separate Phase 3/4 approval and VIC-20 settings scope. Exact product interfaces, fixture assets and accepted observability are a later test/implementation-slice prerequisite, not silently decided by this requirement-only amendment.

## Findings not closed by this proposal

The complete prior 17-finding disposition is retained in the source audit. Generated export typography/title issues, missing historical metadata and raw export receipts, traceability implementation gaps, prior secret exposure, and reviewer receipt/progress defects are not erased by new AC text. Historical original review remains DISAGREE 95/88. No new HV verdict exists for this packet yet.

The full original goal remains active. This packet is movement toward requirements acceptance, not a replacement for 100% xvic parity, native VSF, UI settings visibility, full tests or final hostile agreement.

## Artifact validation and current blocker

Measured 2026-10-09T16:01:11-05:00. `Verify-Proposal.ps1` completed 250 structural checks, all passing. It checked the eleven retained preimages, exact changed fields, preservation of all 47 old AC IDs, 71 unique AC coverage rows, exact Markdown before/after values, seven mapping rows and their 15 additive edges. Those rows name 58 proposed case groups. These are artifact checks, not emulator unit tests, mocked behavior validation or a semantic/HV acceptance gate.

The initial verifier reported eight Markdown mismatches because its section parser treated the retained bodies' `## Acceptance Criteria` headings as new records. The verifier now ends sections only at actual requirement/mapping headings. Requirement data and assertions were not relaxed. Both check runs are retained as `artifact-checks.initial.json` and `artifact-checks.json`. The earlier generator alias collision was corrected by renaming the helper to `New-TapeCriterion`; neither failed attempt made MCP or product changes.

The original JSON draft is preserved byte-for-byte as `proposed-field-delta.draft1.json`, SHA-256 `7020A59D11EEF640919579EC404E3DC237B70C9E7C33272D5195F7B3B3F4845C`. The final JSON records only actual changed fields. Its serialized text read back exactly; the Markdown helper normalized line endings to CRLF, with all text equal after LF normalization. The JSON remains the machine-readable exact delta; neither representation has been applied.

All sixteen cited native files were rehashed and matched their pinned Git objects. Of the prior review's 94 input pins, 93 remained unchanged and the workspace marker was missing; no existing input differed. `verification.json` retains those individual results, selected source hashes, and pins for the prior review's two formerly unpinned post-launch receipts. This is selected-file integrity evidence, not a fresh whole-native inventory or native runtime proof.

The missing `AGENTS-README-FIRST.yaml`, following the supported hook's recorded health failure, prevents trusted fresh MCP/profile loading and the required reviewer bootstrap. No new HV was launched. Session details use the authorized supported failsafe, explicitly queued rather than primary-persisted. Next action after marker restoration: trusted bootstrap, complete profile reload, fresh full-record preimages and drift comparison, then local Opus 5.5 max review of this exact packet. Explicit expanded-delta approval still follows that review. The prior DISAGREE verdict and all real gates remain open.
