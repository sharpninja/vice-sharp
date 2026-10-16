# Legacy tape acceptance correction

Authoritative records: MCP Server, workspace F:\GitHub\vice-sharp. Operator approved the exact five-record correction on 2026-10-09. This is a projection of primary readback, not a separate requirements store. All criteria remain unsatisfied; all five records are pending. Nothing is retired.

Approval and before/after receipts: ../receipts/requirements-recovery/20261009T140421-0500-approved-tape-ac-application/approval.json. Original source-derived proposal: ../receipts/requirements-recovery/20261008T150905-0500-legacy-tape-ac-proposal/README.md. Full supported exports: ../Project/Functional-Requirements.md, ../Project/Technical-Requirements.md and ../Project/Testing-Requirements.md.

Historical statements about CIA1 sense, unconditional off/on ramp reset, no-image recording success and fixed passing counts are superseded by the approved fields below. Original tests and receipts are retained unchanged as historical evidence. This projection does not assert implementation parity, a mock gate, or HV acceptance.

## Datasette motor ramp + sense line + record mode

**ID:** FR-RUNTIME-TAPE-002
**Status:** pending
**Priority:** medium
**Scope:** layer-1+

Datasette transport, tape-sense, motor timing, and recording must reproduce pinned immutable VICE behavior for the same selected machine, tape media state, control sequence, and emulated clock sequence. Machine-specific tape-sense and pulse wiring belong in machine adapters. Internal pulse-buffer success or compatibility with managed-only tests does not establish VICE parity.

### Acceptance criteria

- [ ] **ac-01**: Drive STOP, START, FORWARD, REWIND, and RECORD from a known STOP state with no image, read-only image, and writable image. Each accepted mode and tape-sense transition matches pinned VICE. No-image RECORD and read-only-image RECORD leave mode and sense unchanged; accepted RECORD with writable media asserts sense.
- [ ] **ac-02**: For identical accepted tape-sense transitions, C64 CPU port $0001 input bit 4 and VIC-20 VIA1 port A input bit 6 match pinned VICE, including input/output direction masking. C64 CIA1 FLAG and VIC-20 VIA2 CA1 pulse events are tested separately and are not substituted for tape-sense input.
- [ ] **ac-03**: With an initially stopped motor and no pending datasette alarm, a motor-on transition schedules the startup read alarm at the current emulated clock plus 32000. The ordered read-side event trace immediately before, at, and after that deadline matches pinned VICE. A pre-existing pending alarm is preserved as VICE preserves it; metadata retrieval alone is not counted as a pulse event.
- [ ] **ac-04**: For a running motor, motor-off schedules stopping at current clock plus 32000 without stopping immediately. Motor-on before that stop executes cancels the pending stop without imposing a new startup ramp. Repeated motor-off does not extend an existing stop deadline. Ordered boundary events and restart after an executed stop match pinned VICE.
- [ ] **ac-05**: Recording writes occur only under the native combination of an attached image, accepted RECORD mode, running motor, and qualifying write-line transition. No image, read-only media that rejected RECORD, wrong mode, motor stopped, repeated levels, and nonqualifying transitions produce the same absence of tape writes as pinned VICE.
- [ ] **ac-06**: For identical accepted recording signal traces and media, emitted TAP bytes, elapsed-gap/remainder handling, first-write state, image-position updates, and failure/STOP side effects match pinned VICE. Cover short and long gap boundaries and partial or failed writes; an incremented managed pulse-buffer count alone does not satisfy this criterion.

## Datasette sense line and record mode

**ID:** TR-TAP-EDGE-001
**Status:** pending
**Priority:** medium
**Scope:** layer-1+

Model accepted VICE transport state and image eligibility before deriving tape sense or recording eligibility. Preserve the distinction between device sense state, each machine input port, and tape read-pulse interrupt wiring. Recording consumes ordered clocked write-line transitions and retains the native gap, image-position, and write-failure behavior through the managed adapter.

### Acceptance criteria

- [ ] **ac-01**: Use the FR-RUNTIME-TAPE-002 ac-01 media/control matrix to assert accepted mode, sense state, and unchanged state for ignored RECORD commands; do not treat an unconstrained RecordPressed flag as proof that RECORD was accepted.
- [ ] **ac-02**: Use FR-RUNTIME-TAPE-002 ac-02 observations to assert the C64 CPU-port and VIC-20 VIA1 input bits for both data-direction settings; separately assert pulse routing to C64 CIA1 FLAG and VIC-20 VIA2 CA1.
- [ ] **ac-03**: Use the same recording eligibility traces as FR-RUNTIME-TAPE-002 ac-05. Assert emitted writes and absence of writes as well as updated last-write-line state, including input transitions while recording is ineligible.
- [ ] **ac-04**: Use the same recording byte/error traces as FR-RUNTIME-TAPE-002 ac-06. Preserve the pinned native first-write handling and encoded gap behavior, including known native quirks, without patching VICE or replacing expected values with managed output.

## Datasette motor 32,000-cycle ramp before pulse delivery

**ID:** TR-TAPE-EDGE-001
**Status:** pending
**Priority:** medium
**Scope:** layer-1+

Represent datasette startup and delayed motor-stop state in emulated-clock units, preserving pinned VICE alarm scheduling and event order. Preserve pending alarms, cancel a pending stop when motor is re-enabled before the stop executes, and distinguish motor control input from the running-motor state. The observable timing contract must not depend on whether a particular managed Tick method has already been called.

### Acceptance criteria

- [ ] **ac-01**: Assert the native startup schedule at t+32000 from a stopped motor with no pending alarm, and assert that a pending alarm is not overwritten by the start helper.
- [ ] **ac-02**: Assert motor-off at t on a running motor retains running state before the stop executes at t+32000, and that repeated motor-off preserves the first deadline.
- [ ] **ac-03**: Assert motor-on before pending stop execution clears the stop deadline and preserves running state without another startup delay; assert motor-on after an executed stop schedules a new native startup alarm.
- [ ] **ac-04**: Compare ordered pulse and motor-state traces at deadline-1, deadline, and deadline+1 using fixed clock/control/alarm order. Do not use a first-Tick bypass or an unconditional off/on ramp reset as a substitute for native behavior.

## Datasette motor startup and delayed-stop parity

**ID:** TEST-TAPE-RAMP-001
**Status:** pending
**Priority:** medium
**Scope:** layer-1+

Validate FR-RUNTIME-TAPE-002 ac-03/ac-04 and every TR-TAPE-EDGE-001 criterion using shared clock/control/alarm fixtures. First prove correct and deliberately incorrect mock traces discriminate in an entirely passing native-safe suite. Then bind unchanged fixtures and assertions to managed behavior and immutable native VICE observations. Preserve historical results but remove fixed historical pass-count claims from the acceptance condition.

### Acceptance criteria

- [ ] **ac-01**: Named cases Startup_NoPendingAlarm_DeadlineBoundaries and Startup_PendingAlarm_NotOverwritten assert complete ordered event traces; incorrect early, late, missing, duplicated, and reordered events are rejected within a passing mock harness.
- [ ] **ac-02**: Named cases MotorOff_DelayedStop, MotorOff_RepeatedDoesNotExtend, MotorOn_CancelsPendingStop, and MotorOn_AfterExecutedStop_Restarts assert state and event order at deadline-1, deadline, and deadline+1. A mock implementing immediate stop or unconditional off/on restart must be rejected.
- [ ] **ac-03**: The real-provider run reuses every mock-case input and assertion and compares actual native and managed clocked observations. A pure pulse-duration getter or source inspection is not accepted as a machine-visible pulse trace.
- [ ] **ac-04**: All cases execute without skips in the full applicable cumulative suite after native-safe harness integration. Retain exact commands, counts, TRX/logs, native input identities, failure history, and hostile validation; no count-only or mock-only result marks these criteria satisfied.

## Datasette transport, sense wiring and recording parity

**ID:** TEST-TAPE-SENSE-001
**Status:** pending
**Priority:** medium
**Scope:** layer-1+

Validate FR-RUNTIME-TAPE-002 ac-01/ac-02/ac-05/ac-06 and every TR-TAP-EDGE-001 criterion with shared media, accepted transport-command, machine-port, and write-line fixtures. Establish correct and discriminating incorrect mocks before product changes, then reuse the same cases and assertions against managed and immutable native providers. Historical seven-test success is not an acceptance condition.

### Acceptance criteria

- [ ] **ac-01**: TransportSense_MediaCommandMatrix covers the five transport commands from STOP for absent/read-only/writable media and subsequent STOP transitions. Correct mocks assert accepted mode and sense; wrong polarity, ignored-command state changes, and omitted transitions are rejected.
- [ ] **ac-02**: MachineSense_C64CpuPortAndVic20Via1 covers both machines and input/output masking. TapeRead_PulseInterruptRouting separately verifies CIA1 FLAG and VIA2 CA1. Wrong port, bit, DDR handling, interrupt target, and extra interrupts are rejected by negative controls.
- [ ] **ac-03**: Recording_EligibilityAndWriteEdges covers attached versus absent media, RECORD accepted versus rejected, running versus stopped motor, and qualifying versus repeated/opposite write levels. Correct mocks assert writes and no-writes plus state changes; the historical no-tape successful-write behavior must be rejected.
- [ ] **ac-04**: Recording_GapEncodingAndFailures covers elapsed gaps 0,6,7,8,2046,2047, short-gap remainder carry, first-write state, TAP v0/v1/v2 paths supported by the pinned source, and first/partial/failed output writes. Assert emitted bytes, image position, dirty state and STOP side effects against independently pinned expectations.
- [ ] **ac-05**: Real-provider validation reuses all fixtures and assertions with actual immutable VICE and managed observations, followed by the full cumulative native-safe and real-emulator gates. Preserve historical tests/evidence, explicitly record superseded wrong expectations, and require zero failed, skipped or aborted cases plus accepted hostile validation before satisfaction.

## Unchanged mapping

FR: FR-RUNTIME-TAPE-002
Technical: TR-TAP-EDGE-001, TR-TAPE-EDGE-001
Tests: TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001
