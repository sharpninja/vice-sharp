# Tape HV source audit and amendment boundary

Prepared: 2026-10-09T15:40:18-05:00. Agent: Codex. Workspace: F:\GitHub\vice-sharp, PAYTON-LEGION2.

Status: source inspection evidence, not a passing test, accepted proposal, requirement mutation, or phase exit. The full original handoff-completion goal remains open. No native source, product code, tests, requirements, historical evidence, or goal state was changed by this audit.

## Result

Independent inspection confirms the core behavioral objections in the failed tape HV. It also narrows one reviewer statement: STOP mode alone does not guarantee motor-stop execution exactly 32000 cycles after motor-off. An already-pending later alarm can survive a transition to STOP.

Thirteen materialized native files were compared to Git objects at immutable commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. All thirteen matched byte-for-byte by Git blob identity. SHA-256 pins and command details are in `verification.json`. This proves source identity for those files, not runtime parity or cleanliness of the entire native checkout.

The preceding review's 94 input paths were rehashed: 93 unchanged, zero existing files changed, one missing. The missing input is `AGENTS-README-FIRST.yaml`. MCP was not queried after the hook's health failure. Requirement values discussed here are retained primary readbacks from the approved application, not a fresh server query. Revalidation is required after trust recovery.

Controlling plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`, SHA-256 `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`. All 48261 characters were read this turn. Native immutability, approval gates, full cumulative suites, real emulator gates, and the later Phase 3/4 start approval remain intact.

## Evidence locations

Paths below are relative to the separate pinned materialization, not permission to edit the submodule:

`F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice`

The prior review remains unchanged at `docs/receipts/hv/20261009T141130-0500-approved-tape-ac-opus/verdict.json`: DISAGREE, accuracy 95, completeness 88, 34 PASS, 5 FAIL, 1 UNKNOWN, 1 N/A. This parent audit does not alter that verdict or assign independent review scores.

The approved five-record snapshot remains `docs/receipts/requirements-recovery/20261009T140421-0500-approved-tape-ac-application/effective-requirements.after.json`. The approved canonical projection remains `docs/requirements/legacy-tape-acceptance-correction.md`.

## Native facts and discriminating cases

These cases are proposed test inputs and source-derived expected outcomes, not executed native observations. Future tests must first prove correct and incorrect mock traces inside a green harness, then reuse the same assertions against real providers. Pin machine, tape resources, media, initial internal state, clock domain, and order of control/CPU/alarm events.

### 1. Stop deadline differs from stop execution (HV F-01)

- `src/datasette/datasette.c:1194-1201`: motor-off sets `motor_stop_clk = maincpu_clk + MOTOR_DELAY`. It creates an alarm only if none is already pending.
- `src/datasette/datasette.c:729-749`: the alarm callback unsets its alarm, checks whether the current clock has reached the stop deadline, and only then clears the running-motor flag. Before the deadline it rearms the deadline.
- `src/datasette/datasette.c:806-851`: active reading can replace that same alarm with the next gap deadline.
- `src/alarm.h:176-183`: setting an already-pending alarm modifies its time, rather than adding an independent second alarm.

Source-derived cases:

1. `MotorOff_NoPendingAlarm_StopsAtDeadline`: running motor, no pending alarm, motor-off at clock 1000. The new alarm is 33000. With dispatch at its scheduled clock and no intervening control, the running flag clears at 33000.
2. `MotorOff_PendingReadBeyondDeadline_DoesNotInsertSecondAlarm`: running START with pending alarm at 50000, motor-off at 1000. The deadline becomes 33000 but the pending alarm remains 50000. No independent 33000 callback is inserted. At the callback at 50000 the motor clears before the read-side pulse path.
3. `MotorOff_ReadBeforeDeadline_ReschedulesPastDeadline`: callback at 20000 before a stop deadline of 33000, valid next effective gap of 40000, offset 0, normal playback speed. The callback briefly rearms 33000, then replaces that alarm with 60000. Stop executes on that later callback, absent intervening changes.
4. `MotorOff_StopModeWithExistingLateAlarm`: begin in the pending-read state from case 2, switch transport to STOP before motor-off. STOP sets the mode and resets the write gap but does not unset the pending alarm (`1052-1058`). Stop execution can still be 50000. The review's shorthand that STOP means exact-deadline stopping is therefore too broad.
5. `MotorOn_CancelsStopAfterDeadlineBeforeCallback`: under case 2, re-enable motor at 40000, before the 50000 callback. `1182-1192` clears the stop deadline while retaining the already-running flag. Cancellation is possible after the nominal deadline if the stop callback has not executed.

Reject mocks that schedule a separate guaranteed 33000 stop, stop immediately on motor-off, extend the deadline on repeated off, or force a new startup delay on cancellation. Retain ordinary exact-deadline cases; do not replace them with an equally unconditional late-stop rule. Actual dispatch can carry an offset and occur after the scheduled clock, so both scheduled and observed clocks belong in the trace.

### 2. Fast-wind command alarm is not the ordinary startup alarm (HV F-02)

`src/datasette/datasette.c:925-949,1069-1090,1127-1148` sets FORWARD/REWIND mode before calling its helper. The helper schedules `maincpu_clk + MOTOR_DELAY_FAST` (1000), whether or not the motor is running. The START/other-direction condition inside that helper sees the newly assigned mode. Do not copy its apparent intent instead of its executed behavior.

`datasette_start_motor` (`1000-1008`) preserves a pending alarm. Therefore:

- `FastWind_MotorStopped_CallbackBeforeMotorOn`: the 1000-cycle callback runs with motor off, unsets itself and returns. A later motor-on with no pending alarm schedules the ordinary 32000-cycle startup alarm.
- `FastWind_MotorOnBeforePendingCallback`: motor-on before that pending fast-wind callback preserves it. It does not replace it with a 32000-cycle alarm.
- `FastWind_ExistingReadAlarm_Replaced`: a new FORWARD/REWIND command sets its 1000-cycle alarm even if another alarm was pending.

Each case needs absent/read-only/writable image coverage, both winding directions, command/dispatch tie ordering, and rejection of unconditional 32000 scheduling.

### 3. Recording uses native running state, not a completed startup timer (HV F-03)

`src/datasette/datasette.c:1182-1190` sets `datasette_motor` immediately after the start helper. `1303-1321` gates writes on attached image, accepted RECORD mode, nonzero native motor flag, and a zero-to-nonzero write-line transition. It does not require the startup alarm to have fired. Motor-off leaves that flag set until the stop callback executes.

Named cases must include `Recording_DuringStartupWindow`, `Recording_DuringDelayedStop`, `Recording_AfterStopDeadlineBeforeCallback`, and `Recording_AfterExecutedStop`. The first three may write under the native eligibility conditions; the last may not. Repeated/nonqualifying edges still update the retained line value. Reject mocks that equate motor-control input with motor-running state or globally inhibit writes during startup.

### 4. RECORD/motor order changes first-write state (HV F-04)

`src/datasette/datasette.c:1091-1099,1035-1038`: accepted RECORD resets `last_write_clk` to zero but does not call the start helper. The helper (`1010-1020`) seeds the clock and forces the retained write line low only when it runs with zero write clock.

- `RecordThenMotorOn_FirstEdge`: writable RECORD at 900, previously stopped motor-on at 1000, qualifying edge at 1100. Source-derived gap is 100, not 1100, subject to the exact initial state.
- `MotorOnThenRecord_FirstEdgeFromZero`: motor-on at 1000, accepted RECORD at 1100, known retained write line zero, qualifying edge at 1200. Source-derived gap is 1200, because RECORD reset the clock and did not call the helper.
- Repeat with retained write line nonzero, a same-clock edge, a future retained clock, rejected RECORD, and repeated RECORD. The branches at `1311-1318` must discriminate those cases.

`1215-1291` establishes byte encoding, remainder, partial-write position, STOP side effects and low-24-bit truncation. Add `Recording_LongGap_24BitBoundary` with `0xffffff`, `0x1000000`, and `0x1000001`, plus zero/one/two/three-byte long-payload writes. Never repair a native quirk in the oracle.

### 5. Machine output wiring is a separate contract (HV F-05/F-14)

- C64 motor: `src/c64/c64pla.c:77-81`, `!((DDR & DATA) & 0x20)`, bit 5, active low. DDR input yields motor-on independently of PLAY/sense. A wrong bit-3 implementation must fail.
- C64 write: `src/c64/c64pla.c:84-89`, `(~DDR | DATA) & 0x08`, bit 3, input pulled high. DDR changes can generate transitions.
- C64 sense: `src/c64/c64pla.c:65-67`, input bit 4, separate from CIA1 FLAG read-pulse routing.
- SX-64: output forwarding is excluded by `board_type != BOARD_SX64`; `src/c64/c64mem.c:218-222` supplies the unconnected-port read configuration.
- VIC-20 motor: `src/vic20/vic20via1.c:185-203`, changed PCR writes forward `!(byte & 0x02)`. The local normalized `tmp` is not the value passed to the motor call. Test actual raw-byte behavior, not the comment's intended CA2 model.
- VIC-20 write: `src/vic20/vic20via2.c:110-118` forwards PB3 effective-level changes. `src/core/viacore.c:717-724` proves DDRB writes also call `store_prb`, with effective `PRB | ~DDRB` and prior effective output. Do not test register-data writes only.

Required cases: `MachineMotor_C64Bit5AndDdr`, `MachineWrite_C64Bit3AndDdrEdges`, `MachineMotor_Vic20PcrRawByte`, `MachineWrite_Vic20Pb3AndDdrEdges`, `MachineTape_Sx64Disconnected`, retaining separate input-sense and pulse-interrupt cases.

### 6. Device enable, lifecycle and other sense sources (HV F-14)

- `src/datasette/datasette.c:169-174,775-778,1052-1153`: device-enabled gates apply to sense/flux forwarding at their actual call sites.
- `src/tapeport/tapeport.c:245-267,299-312`: motor/write dispatch selects the registered device; sense and flux forwarding do not contain a universal `tapeport_active` guard. Power-up dispatch does (`281-296`). Do not invent one global enable check.
- `src/datasette/datasette.c:879-922,952-998`: attach/detach/reset affect image pointers, mode, buffering, sense, pending alarms and clocks. Internal reset displays motor-off but does not assign `datasette_motor = 0` in that function. UI state is not proof of internal state. CPU-reset behavior also depends on `DatasetteResetWithCPU` and `autostart_ignore_reset`.
- `src/tapeport/sense-dongle.c:68-94`: enabling asserts sense; disabling only changes its enabled flag; power-up asserts sense. Test device-switch lifecycle rather than assuming disable actively deasserts the line.
- `src/tapeport/cp-clockf83.c:161-190`: RTC sense is asserted only with high RTC data and motor-state false. Motor input drives inverted SDA; write input drives normalized SCL; both recompute sense.

Required cases: `Datasette_EnableAndFluxGating`, `Tapeport_DispatchAndPowerupGating`, `Datasette_AttachDetachResetState`, `SenseDongle_EnableDisablePowerup`, and `CpClockF83_MotorWriteSenseTruthTable`. Independent mocks must reject synthesized motor resets and unconditional sense clearing.

### 7. TAP v0 contradiction and distinct decoding paths (additional source finding)

The retained `FR-TAP-002` body and ac-02 say a zero byte in v0 consumes three following bytes. Both inspected native paths contradict that:

- `src/tape/tap.c:389-444`: the file/block decoder returns 256 pulse units for v0 zero and consumes no extension; v1/v2 zero consumes three little-endian bytes then shifts right by three. Its v2 path adds two decoded halfwaves.
- `src/datasette/datasette.c:504-535`: live datasette v0 zero uses `DatasetteZeroGapDelay`, not three extra bytes. Nonzero bytes become byte times eight, then wobble/misalignment apply. Later-version zero consumes the three-byte extension; an all-zero extension uses the zero-gap resource.
- `src/datasette/datasette.c:607-691`: live C16 v1/v2 behavior is selected by machine behavior, not by blindly applying the file decoder's v2 summation to C64/VIC-20 pulse events.
- `src/tape/tap.c:121-196`: header magic/short-read failure, header mismatch warnings and clock-selection fallback are distinct outcomes. "Validated" must not mean rejecting every mismatch that VICE merely warns about.

Required cases: `TapV0_ZeroDoesNotConsumeExtension`, `TapV1V2_ExtensionAndTruncation`, `Tap_BlockDecoderVersusDatasettePath`, `Tap_HeaderWarningVersusRejection`, and `Tap_ClockHeaderVersusSelectedMachine`. Pin zero-gap, speed, wobble and azimuth resources. Reject a single shared expected trace that conflates metadata/block decoding with clocked machine events.

The retained FR-TAP-003 ac-03 also pairs 352/512/672 cycles with TAP values 0x30/0x42/0x56. Direct multiplication by eight yields 384/528/688. This is an additional internal inconsistency to include in the exact amendment, not authorization to substitute those new numbers as universally correct pulse thresholds. `tap_init` configuration and decoder threshold boundaries still need independent inspection before locking replacement text.

## Requirement reconciliation decisions to prepare, not apply

1. Correct the approved five records' stop, fast-wind, recording and wiring criteria with the cases above. Preserve valid existing cases and AC identities; append omitted cases rather than silently dropping them.
2. Resolve two duplicated semantic identities. `docs/requirements/backfill/Classic-VICE-Edge-Case-TR-Backfill.md:391-434` assigns TR-TAP-EDGE-001 to TAP decoding/TurboTape and TR-TAPE-EDGE-001 to tapeport peripherals, while MCP uses those IDs for datasette recording/timing. Recommended proposal: one explicit union contract for each existing ID, preserving both legacy obligations and mappings. Do not retire, delete, silently rename or overwrite either meaning. Alternative, requiring an explicit operator choice: assign distinct new IDs with complete preserved provenance and mappings.
3. Expand the exact amendment to FR-TAP-001 and FR-TAP-002. Motor bit 3, mandatory PLAY before motor-on, a universal hardware half-second startup, v0 overflow and version scope conflict with the native target. Preserve unrelated obligations until independently audited; do not remove audio or public-interface requirements as a shortcut.
4. Include TEST-TAP-001's missing structured AC and explicit legacy decoder/peripheral coverage. Its retained record has zero AC. Preserve existing FR-TAP-001 through FR-TAP-005 links, TR-CYCLE-001 and TEST-X64SC-LOCKSTEP-001 when adding missing links.
5. FR-BACKFILL-MEDIA-001 is already covered by an unapplied five-AC delta in `docs/receipts/requirements-recovery/20261009T023102-0500-legacy-media-video-ac-proposal/proposed-field-delta.json`. Reuse and reconcile that exact proposed row. Do not create a second competing parent-FR proposal or treat it as approved. Its current retained closed/zero-AC state still conflicts with the reopened tape children.
6. Reconcile FR-TAP-003's pulse-value inconsistency and FR-TAP-004's machine-write/interlock wording before declaring the combined tape contract ready. Source inspection above identifies the issue, not a completed FR rewrite.

The exact old/new eight-or-more-record amendment, AC-level traceability, full-field preimages, proposed mock case matrix and independent HV remain to be completed. This document is deliberately not presented for implementation approval as a decision-complete replacement plan.

## All previous findings remain accounted for

- F-01 through F-05: independently inspected; concrete source-derived cases above. No runtime validation or requirement mutation.
- F-06: both ID meanings preserved as an unresolved reconciliation boundary.
- F-07: existing media proposal located; still unapproved.
- F-08: generated em-dash export defect remains triaged, not fixed; no hand-edit of generated output.
- F-09: missing historical full-field TR/TEST preimages remain UNKNOWN. Future full `getFr/listTr/listTest` reads cannot retroactively prove preservation. No fabricated reconstruction.
- F-10: missing export request/raw failure receipts and uncertain rejecting-validator attribution remain open. No export rerun or new server repair.
- F-11: per-AC executable traceability remains open. Source evidence and ID presence are not test coverage.
- F-12: supported Testing export still omits titles in retained evidence; no exporter change.
- F-13: relevant MCP TODO update and handoff reconciliation remain pending. Missing marker prevents fresh TODO readback; no direct TODO edits.
- F-14: expanded source findings above, with remaining scope decisions explicit.
- F-15: original credential exposure remains a recorded failure. This audit did not print, copy or republish raw reviewer stream or credentials. Original raw stream remains retained under the prior containment policy.
- F-16: both post-launch receipts must be pinned in the next review packet; no claim the original reviewer saw their launch-time hashes.
- F-17: next local reviewer still needs explicit isolated environment and real source identity. No reviewer launch this turn.
- Additional parent finding: ongoing reviewer progress/ETA entries were missing. Future prompt must require numeric progress/ETA during the run, never fabricated backfill.

## Next gate and limitations

First finish the exact corrective delta and its case/mapping ledger. Before application, restore trusted MCP access, compare fresh effective and full-field records against the retained before-values, disclose drift, obtain the operator's exact-delta approval, and retain fresh full-field preimages. No bulk ingest, unsafe queue drain, historical session cancellation, or native modification.

Then perform requirements-gate HV with local Opus 5.5 max, no artificial caps, complete request/response JSONL, periodic numeric progress/ETA, and full verdict through supported MCP/failsafe. No green mock gate or production work may be inferred from this source audit. All required real xvic/x64sc, VSF, cumulative and whole-goal gates remain mandatory.

Tests executed this turn: none. Build executed: none. Runtime trace executed: none. No commit or push. Project gates accepted: none.
