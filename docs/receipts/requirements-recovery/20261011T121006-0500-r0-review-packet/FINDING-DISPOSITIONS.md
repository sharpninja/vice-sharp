# Complete finding response for R0 re-review

Prepared 2026-10-11T12:26:24-05:00. All 18 new findings and 17 prior dispositions from the midnight Opus verdict are retained below. These are implementer responses for independent review, not self-issued closure. Read the original full finding text and evidence in the linked verdict. Where a finding concerns later executable proof or irrecoverable history, its continuing gate is explicit.

Paths below are relative to `docs/receipts/requirements-recovery/` unless stated otherwise. The index links every supplement. `080155` means the full `20261011T080155-0500-capacitor-guard-remediation` directory, and analogous time prefixes identify the unique directories listed in the index.

## New findings

### N-01: nonexistent D64 mapping targets

The exact candidate omits the two nonexistent D64 links. It does not create their target records. The full seven-row mapping proposal retains existing links and adds only live targets; fresh baseline checks inspect all 38 references. D64-specific TR/TEST creation remains a separate approval dependency before D64 acceptance. Source: 065515 N01/N02 supplement, 080155 delta, 083152 D64 contract, current live baseline. Request review of this explicit deferral, not acceptance of a nonexistent dependency.

### N-02: contradictory CPU/SX-64 requirements

The candidate expands to include FR-CPU-004 and FR-PRF-003. Bit 5 is the C64 motor output, bit 3 write output; DDR, read direction, board-specific floating input and SX-64 disconnected outputs/capacitors are distinguished. The subsequent capacitor correction is part of the sole 080155 delta. All prior IDs remain; eight unchanged criteria still bind whole-plan tests. Source: exact AC before/after, CAPACITOR-SOURCE-AND-CASES, 092059 wiring. This expansion needs explicit operator approval after HV.

### N-03: synthesized read timestamps

The typed API audit distinguishes real record fields from workflow adapter projections. Preserve raw observations but never use synthesized `createdAt`/`updatedAt` to prove history or write them back. The new complete typed baseline compares 121 actual fields including linked use cases; all are equal. R1 guards test presence/type/value and exclude adapter-only metadata by contract. No historical timestamp recovery is claimed.

### N-04: null/null comparison overclaim

The old receipt is not repaired retrospectively. The new baseline retains actual expected/observed values, field-presence flags and equality results, with source hashes. Its incorrect first comparison against a different representation is also disclosed. Future application must use this value-bearing design and real negative controls, not an `equal:true` field alone.

### N-05: start helper while motor already running

FR-RUNTIME-TAPE-002 ac-03 and all dependent text specify START with absent/read-only/writable media, existing/no alarm, clock zero and retained stop deadline. FORWARD/REWIND preserve the earlier t+1000 schedule rather than inventing a second t+32000 alarm. The 085402 motor matrix includes initial tuples, operations, expected ordered events and wrong-provider controls. Execution remains R2/R3.

### N-06: before-deadline cancellation

FR-RUNTIME-TAPE-002 ac-04 and dependent coverage explicitly cover motor-on before the deadline, exactly at it before callback dispatch and after it before callback, without a new ramp. 085402 vectors retain running/pending/write state while clearing the deadline. Future mocks must reject premature stop or spurious ramp; none are reported as executed here.

### N-07: hidden process/static state

072531 source audit, 080155 delta and 110100 observation contract enumerate wobble/rest/attach-scan/PRNG history, old port/write bits, audio state and RTC state. Fresh process, reset, reattach and VSF reload are not equivalent resets. Native-only prelude reachability, observed rounding/random history and real provider proof remain mandatory before acceptance; a seed manifest alone is not proof.

### N-08: lifecycle quirks and stale alarms

The delta and 092059 lifecycle contract retain reset/attach clearing stop deadline without clearing running motor, stale alarm with new STOP media, remembered no-tape mode, and ordered sense/dispatch behavior. Public-operation preludes must reach those states without modifying VICE. Later executable gates retain these cases rather than normalizing them into a cleaner model.

### N-09: recording FILE position/update-stream order

FR-RUNTIME-TAPE-002 ac-06 and recording groups distinguish logical TAP position, buffer indices and physical FILE cursor. RECORD while already running must not invent a seek. 092059 contract specifies command order, boundary and write-failure cases. Platform CRT/update-stream effects require pinned native runtime observations at the real gate, not an undocumented deterministic assertion across platforms.

### N-10: unknown TAP versions/physical length

072531 audit and the exact delta separate block decoding from live datasette paths for all version bytes, zero extension rules, actual payload zero/one/two/three and declared-length independence. Test dispositions retain the malformed/short fixtures that currently assert incompatible rules and identify their intended correction only after approved criteria. Native runtime observations remain required.

### N-11: media row references unapproved/stale criteria

The candidate FR-BACKFILL-MEDIA-001 text now explicitly identifies the live tape record and the separate unapproved drive AC dependency. It does not suggest that the existing zero-AC drive record proves acceptance, or that earlier tape application approved this new text. Its five media criteria receive detailed 083152 specifications. Review the bounded amendment without silently approving the drive proposal.

### N-12: unaudited turbo approximation

FR-TAP-005 is added to the exact delta. Source-derived pulse/interrupt behavior replaces the universal approximate floor/single-clock claim; C64 CIA FLAG and VIC-20 CA1 remain separate. All five named loaders, rate transitions, encodings and successful load outcomes remain mandatory. Materializing and executing lawful native fixtures is a later real gate, not waived because the current bounded corpus search found none.

### N-13: placeholder group counts

The original count is not treated as 58 concrete fixtures. The consolidated coverage replaces the five media placeholder rows with 20 specified media groups and retains a distinct unchanged-AC denominator. The resulting 117 unique names are proposed groups, not executable cases or success counts. Reviewer must assess case quality beyond name/count agreement.

### N-14: retained IDs changed meaning

The exact before/after values remain visible. Across the expanded candidate: 66 old AC IDs, 56 rewritten texts, ten unchanged and 24 new IDs. Existing evidence cannot be transferred automatically to rewritten obligations. Every proposed AC remains unsatisfied until the approved mock/real/cumulative gates prove it.

### N-15: existing incompatible tests

095710 inventories 63 direct methods in 13 files; 115707 inventories 47 adjacent methods in ten files plus nine lexical classifications. Bodies, assertions, fixtures and proposed dispositions are retained, not just names. The CA1 erratum is preserved transparently. These audits neither execute nor approve replacement/removal of tests. Full R2/R3 retains corrected approved assertions, unrelated regressions and legacy evidence; no skip, deletion or reduction by this document.

### N-16: duplicate/historical sessions

Preserve historical quota attempts, open/duplicate hook turns and existing quarantine evidence. Do not cancel or replay them. New remediation review has a new isolated reviewer session/cache and records its real identity. Queue reconciliation is not silently performed under this task. The midnight logical review is already complete and will not be duplicated.

### N-17: reviewer process violations

Carry the original finding unchanged: estimated interim times, temporary-file handling, direct .NET receipt writes and out-of-console triage remain historical deviations. New receipts use measured times and supported local paths. Correct forward behavior cannot make the old behavior compliant or substitute for operator disposition where required.

### N-18: stale handoff/TODO tracking

The canonical handoff was reconciled at 11:30 with original retained, and live plan TODOs remain open. This packet updates the current preparation checkpoint; the old red-first wording does not govern. Green discriminating mocks before implementation is explicit. Whole-plan scope, earlier authorizations and unresolved gates remain visible. New tracking is not a done-state transition.

## Prior finding dispositions

### F-01: deadline callback semantics

Candidate ac-04 and 085402 require stopping when a callback observes deadline reached, not a fictitious standalone stop timer. Text correction is proposed; future executable proof still required.

### F-02: startup scheduling

Candidate ac-03 and N-05 response cover motor-on and running-motor command helper paths with pending-alarm precedence. All mapped tests must exercise those assertions through the approved provider seam.

### F-03: motor flag and pulse delivery

Delta, motor and recording/wiring specifications distinguish running flag, startup alarm, delayed-stop deadline, eligibility and delivered pulse events. No numeric duration getter substitutes for ordered native events.

### F-04: recording first edge/order

The recording contract separates motor-on-then-RECORD from RECORD-then-motor-on, clock zero, first write-bit state and partial-write effects. Same assertions must bind mocks and real providers. No runtime pass claimed.

### F-05: machine wiring

N-02's CPU/SX expansion plus 092059 C64 DDR/bit matrix and VIC PCR/PB matrix cover the corpus conflict. VIC-20 read flux is VIA2 CA1, not CB1. Full IRQ timing and all build configurations remain later proof obligations.

### F-06: colliding technical requirements

Both TR-TAP-EDGE-001 and TR-TAPE-EDGE-001 are retained with the intended contract union and mapping links. Do not delete a legacy ID or silently transfer old satisfaction. R1 projection/readback must prove the exact approved values in both records.

### F-07: broader media obligation

N-01/N-11 and 083152 provide explicit D64, true-drive IEC, tape, evidence and gate scope. Missing targets and independent drive approval are disclosed, not fabricated. Broader acceptance remains open.

### F-08: export corruption

The old 139 em-dash/export corruption finding remains a later R1 projection/tooling obligation. This amendment does not repair the export tool or accept corrupted mirrors. Retain raw export bytes and validate actual regenerated content after approved writes; do not normalize source history to hide loss.

### F-09: historic metadata

Unrecoverable old metadata remains unrecovered. Typed current field equality is forward evidence only, not proof of original creation/update timestamps or prior omitted values. N-03 governs future guards.

### F-10: absent old raw export receipt

Do not reconstruct a historical raw response from a later projection. Retain future raw responses and parse/readback receipts at R1. The old gap remains a disclosed limitation, not a waived truth claim.

### F-11: missing/misleading code and test IDs

The per-method audits identify current assertion scope and required mapping corrections. No source-name match is reported as implemented acceptance. Approved R2/R3 must bind actual test assertions to requirements and retain all real gates.

### F-12: retitled TEST export headings

Title preservation and generated headings are an explicit R1 export/readback check. Historical wrong headings do not prove current record title or repair. No export regeneration/application has run in this preparation.

### F-13: stale TODO/handoff

Handled for re-review by the canonical reconciliation and N-18 checkpoint, preserving all open work and prior evidence. Actual acceptance remains the reviewer's decision.

### F-14: native edge coverage

N-05 through N-12 and their source/fixture supplements enumerate the missing state and path distinctions. The candidate's source-derived contracts remain subject to HV. Observer capability, fixture reachability and real execution are later gates, never inferred from a specification.

### F-15: historical credential exposure

The earlier key-invalidation inference is not independently established. Do not print, test or reuse old exposed keys merely to claim closure. Keep raw streams protected; server rotation observation is not a complete credential-incident disposition. Carry unresolved operator/security disposition to whole-plan exit rather than erasing the finding.

### F-16: post-launch receipt hashes

Preserve original launch and terminal hashes; current packet pins are a separate time-stamped manifest. Any drift is disclosed before review. Hashing a changed artifact does not retroactively make it the reviewed artifact.

### F-17: process/session duplicates

Same retention and isolation rules as N-16. No historical turn cancellation, pending queue drain or environment repair is authorized by this review. A new review is new evidence, not erasure of earlier incomplete or rejected attempts.

## Acceptance boundary

The reviewer must decide which text/plan defects remain blockers to exact-delta approval and which findings are truthfully retained for later named gates or historical operator disposition. Do not call all findings closed merely because each has a paragraph. Do not demand a completed real emulator as a substitute for reviewing unapplied requirements. Full-goal acceptance still requires every mandatory later result, with AGREE at least 98/98 and zero FAIL/UNKNOWN at the applicable gates.
