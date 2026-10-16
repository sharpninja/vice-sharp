# Exact proposed tape requirement delta

UNAPPLIED. Generated from proposed-field-delta.json object. Before-values are retained primary evidence, not a fresh MCP read. Approval applies only to these exact records and mapping additions; all AC remain false and statuses pending.

## FR-RUNTIME-TAPE-002

Fields changed: body, acceptanceCriteria

### before

Title: Datasette motor ramp + sense line + record mode
Status: pending

Datasette transport, tape-sense, motor timing, and recording must reproduce pinned immutable VICE behavior for the same selected machine, tape media state, control sequence, and emulated clock sequence. Machine-specific tape-sense and pulse wiring belong in machine adapters. Internal pulse-buffer success or compatibility with managed-only tests does not establish VICE parity.

- [ ] ac-01: Drive STOP, START, FORWARD, REWIND, and RECORD from a known STOP state with no image, read-only image, and writable image. Each accepted mode and tape-sense transition matches pinned VICE. No-image RECORD and read-only-image RECORD leave mode and sense unchanged; accepted RECORD with writable media asserts sense.
- [ ] ac-02: For identical accepted tape-sense transitions, C64 CPU port $0001 input bit 4 and VIC-20 VIA1 port A input bit 6 match pinned VICE, including input/output direction masking. C64 CIA1 FLAG and VIC-20 VIA2 CA1 pulse events are tested separately and are not substituted for tape-sense input.
- [ ] ac-03: With an initially stopped motor and no pending datasette alarm, a motor-on transition schedules the startup read alarm at the current emulated clock plus 32000. The ordered read-side event trace immediately before, at, and after that deadline matches pinned VICE. A pre-existing pending alarm is preserved as VICE preserves it; metadata retrieval alone is not counted as a pulse event.
- [ ] ac-04: For a running motor, motor-off schedules stopping at current clock plus 32000 without stopping immediately. Motor-on before that stop executes cancels the pending stop without imposing a new startup ramp. Repeated motor-off does not extend an existing stop deadline. Ordered boundary events and restart after an executed stop match pinned VICE.
- [ ] ac-05: Recording writes occur only under the native combination of an attached image, accepted RECORD mode, running motor, and qualifying write-line transition. No image, read-only media that rejected RECORD, wrong mode, motor stopped, repeated levels, and nonqualifying transitions produce the same absence of tape writes as pinned VICE.
- [ ] ac-06: For identical accepted recording signal traces and media, emitted TAP bytes, elapsed-gap/remainder handling, first-write state, image-position updates, and failure/STOP side effects match pinned VICE. Cover short and long gap boundaries and partial or failed writes; an incremented managed pulse-buffer count alone does not satisfy this criterion.

### after

Title: Datasette motor ramp + sense line + record mode
Status: pending

Datasette transport, tape sense, motor scheduling, machine wiring and recording shall reproduce pinned immutable VICE for identical machine, board, resource, media, control and clock state. Preserve separate motor input, native running state, scheduled alarm time and actual callback time. Machine wiring belongs in machine adapters. Native quirks are target behavior; managed pulse-buffer success is not parity.

- [ ] ac-01: From known STOP, cover STOP, START, FORWARD, REWIND and RECORD with absent, read-only and writable images. Match accepted mode and sense. Absent-image and read-only-image RECORD do not change mode or sense; accepted writable RECORD asserts sense when the datasette is enabled.
- [ ] ac-02: Match C64 CPU port $0001 input bit 4 and VIC-20 VIA1 port A input bit 6, including DDR/output masking. Separately match C64 CIA1 FLAG and VIC-20 VIA2 CA1 pulse routing; sense is not an interrupt pulse.
- [ ] ac-03: A stopped motor with no pending alarm schedules the ordinary startup callback at current clock plus 32000; retain an existing alarm as VICE does. FORWARD/REWIND commands schedule their 1000-cycle alarm independently of motor-running state, including motor-on before and after that alarm dispatch. Compare ordered scheduled and actual events, not a pulse-duration getter.
- [ ] ac-04: Motor-off on a running motor sets a stop deadline of current clock plus 32000 without immediate stopping or replacing an already pending alarm. Actual stopping occurs when a datasette callback observes the deadline reached. Cover callbacks before, at and beyond the deadline, active read rescheduling, STOP with a retained later alarm, repeated off preserving the deadline, and motor-on cancellation even after the deadline but before stop execution. A stopped motor starts again through the native pending-alarm rule.
- [ ] ac-05: Recording eligibility follows attached image, accepted RECORD mode, the native running-motor flag and qualifying zero-to-nonzero write-line transition. The running flag is set immediately on motor-on and retained until the delayed-stop callback. Cover writes during startup and pending stop, no writes after executed stop, rejected RECORD, repeated/opposite edges, and retained write-line updates even while ineligible.
- [ ] ac-06: Match emitted TAP bytes, last-write clock/line, remainder, image position/size/dirty state, counters and failure/STOP effects for RECORD-before-motor and motor-before-RECORD, same/future-clock first edges, short gaps 0/6/7/8/2046/2047, long gaps at and above 24-bit boundaries, TAP v0/v1/v2 output paths, and first/partial/failed writes. Preserve the native first-gap and truncation behavior.
- [ ] ac-07: Match machine motor/write outputs: C64 DDR-masked bit-5 active-low motor and bit-3 write; VIC-20 changed PCR raw bit-1 motor and effective VIA2 PB3 write including DDRB-induced edges. Cover SX-64 disconnection, repeated writes and input/output direction changes without substituting intended hardware behavior for pinned code.
- [ ] ac-08: Match enabled/disabled datasette sense/flux forwarding, tapeport device dispatch, attach/detach, resource-controlled CPU reset and retained state. Assert internal state separately from UI motor display. Preserve pending alarms, gap state and sense exactly as pinned VICE, including no universal tapeport-active suppression of every signal.

## TR-TAP-EDGE-001

Fields changed: title, body, acceptanceCriteria

### before

Title: Datasette sense line and record mode
Status: pending

Model accepted VICE transport state and image eligibility before deriving tape sense or recording eligibility. Preserve the distinction between device sense state, each machine input port, and tape read-pulse interrupt wiring. Recording consumes ordered clocked write-line transitions and retains the native gap, image-position, and write-failure behavior through the managed adapter.

- [ ] ac-01: Use the FR-RUNTIME-TAPE-002 ac-01 media/control matrix to assert accepted mode, sense state, and unchanged state for ignored RECORD commands; do not treat an unconstrained RecordPressed flag as proof that RECORD was accepted.
- [ ] ac-02: Use FR-RUNTIME-TAPE-002 ac-02 observations to assert the C64 CPU-port and VIC-20 VIA1 input bits for both data-direction settings; separately assert pulse routing to C64 CIA1 FLAG and VIC-20 VIA2 CA1.
- [ ] ac-03: Use the same recording eligibility traces as FR-RUNTIME-TAPE-002 ac-05. Assert emitted writes and absence of writes as well as updated last-write-line state, including input transitions while recording is ineligible.
- [ ] ac-04: Use the same recording byte/error traces as FR-RUNTIME-TAPE-002 ac-06. Preserve the pinned native first-write handling and encoded gap behavior, including known native quirks, without patching VICE or replacing expected values with managed output.

### after

Title: TAP decoding, datasette sense and recording edge parity
Status: pending

This existing ID retains both historical obligations: TAP version/long-pulse/clock/CBM/TurboTape decoding and datasette transport/sense/recording. Keep the file/block decoder distinct from the clocked datasette read path. Use one reconciled canonical contract; do not retire either meaning or remove its FR/TEST mappings.

- [ ] ac-01: Apply FR-RUNTIME-TAPE-002 ac-01 media/control matrix to accepted transport state and sense. Ignored RECORD is not a RecordPressed success. Include enabled-state gating and unchanged observations.
- [ ] ac-02: Apply FR-RUNTIME-TAPE-002 ac-02 and ac-07 to separate machine sense inputs, read-pulse interrupt inputs and motor/write outputs, including direction registers, raw VIC-20 PCR behavior and SX-64.
- [ ] ac-03: Apply FR-RUNTIME-TAPE-002 ac-05 to ordered recording eligibility and write edges; assert output absence as well as writes and retained line state. Native running state is distinct from control input and completed startup delay.
- [ ] ac-04: Apply FR-RUNTIME-TAPE-002 ac-06 to exact recording bytes and side effects, including both RECORD/motor orderings, first-edge clock handling, 24-bit truncation and failed/partial writes.
- [ ] ac-05: Retain legacy TAP v0/v1/v2 decoding, long-pulse, header and clock-selection coverage. v0 zero consumes no extension. The file/block decoder uses its native pulse units and v2 halfwave combination; the live datasette path uses its own zero-gap resources, direction, machine-specific halfwave handling, wobble and azimuth state. Cover truncated extensions and warning-versus-rejection outcomes without merging these paths.
- [ ] ac-06: Retain legacy CBM and TurboTape pilot/header detection with boundary fixtures against pinned tap.c. C64 and VIC-20 configured CBM pulse-unit ranges are inclusive 36-54, 55-73 and 74-100 after tap_init; TurboTape uses its own ranges and pilot/header control flow. Test each endpoint, adjacent outside value, missing/truncated pilot/header and malformed stream; do not replace loader behavior with a count of parsed pulses.

## TR-TAPE-EDGE-001

Fields changed: title, body, acceptanceCriteria

### before

Title: Datasette motor 32,000-cycle ramp before pulse delivery
Status: pending

Represent datasette startup and delayed motor-stop state in emulated-clock units, preserving pinned VICE alarm scheduling and event order. Preserve pending alarms, cancel a pending stop when motor is re-enabled before the stop executes, and distinguish motor control input from the running-motor state. The observable timing contract must not depend on whether a particular managed Tick method has already been called.

- [ ] ac-01: Assert the native startup schedule at t+32000 from a stopped motor with no pending alarm, and assert that a pending alarm is not overwritten by the start helper.
- [ ] ac-02: Assert motor-off at t on a running motor retains running state before the stop executes at t+32000, and that repeated motor-off preserves the first deadline.
- [ ] ac-03: Assert motor-on before pending stop execution clears the stop deadline and preserves running state without another startup delay; assert motor-on after an executed stop schedules a new native startup alarm.
- [ ] ac-04: Compare ordered pulse and motor-state traces at deadline-1, deadline, and deadline+1 using fixed clock/control/alarm order. Do not use a first-Tick bypass or an unconditional off/on ramp reset as a substitute for native behavior.

### after

Title: Datasette timing and tapeport peripheral line parity
Status: pending

This existing ID retains both historical obligations: datasette startup/delayed-stop/fast-wind timing and tapeport sense/motor/RTC/dongle semantics. Model scheduling in the emulated clock domain and preserve the selected native device behavior. A datasette is not the only possible source of tape sense.

- [ ] ac-01: Assert stopped-motor ordinary startup at t+32000 with no pending alarm and preservation of an existing pending alarm. Observe callback dispatch separately from scheduled time; no first-Tick bypass.
- [ ] ac-02: Assert motor-off sets t+32000 stop deadline, retains a pending alarm and running state until a callback executes at or after that deadline, and repeated motor-off does not extend it. Include no-pending exact-deadline stop, retained late read/STOP alarms and a before-deadline callback rescheduling beyond the deadline.
- [ ] ac-03: Assert motor-on cancels a stop not yet executed, including after the nominal deadline but before a late callback, without imposing a new startup delay on an already-running motor. After an executed stop, obey the ordinary start helper and its pending-alarm preservation.
- [ ] ac-04: Compare complete ordered control, alarm, running-state and pulse traces at deadline-1/deadline/deadline+1 and at actual callback boundaries. Pin same-clock ordering, callback offset, tape resources and effective gap; reject missing, extra, duplicate, reordered or synthesized events.
- [ ] ac-05: FORWARD and REWIND schedule a 1000-cycle alarm after the mode assignment, with absent or attached media and motor on or off. Cover replacement of a pending read alarm and motor-on before/after the fast callback; preserve the executed native behavior rather than the helper comment intent.
- [ ] ac-06: Retain legacy sense-dongle and CP Clock F83 semantics. Dongle enable and power-up assert sense, while its disable callback does not itself clear sense. RTC motor input drives inverted SDA, write input drives normalized SCL, and sense is asserted only when RTC data is high and motor-state is false. Cover repeated calls, all truth-table inputs and selected-device lifecycle without synthesizing datasette PLAY.
- [ ] ac-07: Apply FR-RUNTIME-TAPE-002 ac-08 to per-call-site device enable gating, tapeport dispatch, attachment and reset. Assert internal motor state independently of the UI reset indication and pin reset resources.

## TEST-TAPE-RAMP-001

Fields changed: title, condition, acceptanceCriteria

### before

Title: Datasette motor startup and delayed-stop parity
Status: pending

Validate FR-RUNTIME-TAPE-002 ac-03/ac-04 and every TR-TAPE-EDGE-001 criterion using shared clock/control/alarm fixtures. First prove correct and deliberately incorrect mock traces discriminate in an entirely passing native-safe suite. Then bind unchanged fixtures and assertions to managed behavior and immutable native VICE observations. Preserve historical results but remove fixed historical pass-count claims from the acceptance condition.

- [ ] ac-01: Named cases Startup_NoPendingAlarm_DeadlineBoundaries and Startup_PendingAlarm_NotOverwritten assert complete ordered event traces; incorrect early, late, missing, duplicated, and reordered events are rejected within a passing mock harness.
- [ ] ac-02: Named cases MotorOff_DelayedStop, MotorOff_RepeatedDoesNotExtend, MotorOn_CancelsPendingStop, and MotorOn_AfterExecutedStop_Restarts assert state and event order at deadline-1, deadline, and deadline+1. A mock implementing immediate stop or unconditional off/on restart must be rejected.
- [ ] ac-03: The real-provider run reuses every mock-case input and assertion and compares actual native and managed clocked observations. A pure pulse-duration getter or source inspection is not accepted as a machine-visible pulse trace.
- [ ] ac-04: All cases execute without skips in the full applicable cumulative suite after native-safe harness integration. Retain exact commands, counts, TRX/logs, native input identities, failure history, and hostile validation; no count-only or mock-only result marks these criteria satisfied.

### after

Title: Datasette startup, delayed-stop and fast-wind parity
Status: pending

Validate FR-RUNTIME-TAPE-002 ac-03/ac-04 and TR-TAPE-EDGE-001 ac-01 through ac-05 using shared clock/control/alarm cases. Green correct-mock and expected negative-control evidence precedes production changes. Reuse the same cases and assertions with real managed/native observations; no fixed historical passing total is an acceptance condition.

- [ ] ac-01: Startup_NoPendingAlarm_DeadlineBoundaries and Startup_PendingAlarm_NotOverwritten assert ordered schedules, callback times and events; incorrect early/late/missing/duplicated/reordered events are rejected within a passing mock harness.
- [ ] ac-02: Run all named delayed-stop and cancellation cases in the amendment ledger, retaining deadline and callback boundaries separately. Reject immediate stop, guaranteed-deadline stop despite an existing late alarm, repeated-off deadline extension and unconditional off/on restart.
- [ ] ac-03: Real-provider validation reuses every mock input/assertion and obtains actual native and managed ordered observations. Source inspection, an analytical trace or a duration getter cannot substitute for actual pulse/state evidence. Missing capability fails closed.
- [ ] ac-04: Run every case without skips in full applicable cumulative native-safe and real-emulator suites after native-safe integration. Retain exact commands, counts, TRX/logs, identities, failure history and accepted hostile review before satisfaction.
- [ ] ac-05: Run each named fast-wind case for both FORWARD and REWIND, attached/absent media and stopped/running motor. Assert 1000-cycle command alarms, preservation by motor-on when pending and 32000 startup only when the ordinary helper has no pending alarm; reject unconditional startup timing.

## TEST-TAPE-SENSE-001

Fields changed: title, condition, acceptanceCriteria

### before

Title: Datasette transport, sense wiring and recording parity
Status: pending

Validate FR-RUNTIME-TAPE-002 ac-01/ac-02/ac-05/ac-06 and every TR-TAP-EDGE-001 criterion with shared media, accepted transport-command, machine-port, and write-line fixtures. Establish correct and discriminating incorrect mocks before product changes, then reuse the same cases and assertions against managed and immutable native providers. Historical seven-test success is not an acceptance condition.

- [ ] ac-01: TransportSense_MediaCommandMatrix covers the five transport commands from STOP for absent/read-only/writable media and subsequent STOP transitions. Correct mocks assert accepted mode and sense; wrong polarity, ignored-command state changes, and omitted transitions are rejected.
- [ ] ac-02: MachineSense_C64CpuPortAndVic20Via1 covers both machines and input/output masking. TapeRead_PulseInterruptRouting separately verifies CIA1 FLAG and VIA2 CA1. Wrong port, bit, DDR handling, interrupt target, and extra interrupts are rejected by negative controls.
- [ ] ac-03: Recording_EligibilityAndWriteEdges covers attached versus absent media, RECORD accepted versus rejected, running versus stopped motor, and qualifying versus repeated/opposite write levels. Correct mocks assert writes and no-writes plus state changes; the historical no-tape successful-write behavior must be rejected.
- [ ] ac-04: Recording_GapEncodingAndFailures covers elapsed gaps 0,6,7,8,2046,2047, short-gap remainder carry, first-write state, TAP v0/v1/v2 paths supported by the pinned source, and first/partial/failed output writes. Assert emitted bytes, image position, dirty state and STOP side effects against independently pinned expectations.
- [ ] ac-05: Real-provider validation reuses all fixtures and assertions with actual immutable VICE and managed observations, followed by the full cumulative native-safe and real-emulator gates. Preserve historical tests/evidence, explicitly record superseded wrong expectations, and require zero failed, skipped or aborted cases plus accepted hostile validation before satisfaction.

### after

Title: Datasette transport, machine wiring, recording and lifecycle parity
Status: pending

Validate FR-RUNTIME-TAPE-002 ac-01/ac-02/ac-05 through ac-08 and the corresponding TR-TAP-EDGE-001 and TR-TAPE-EDGE-001 criteria. Establish independent correct and discriminating wrong mocks, then bind unchanged inputs/assertions to real providers. Preserve historical evidence without treating old counts or wrong expectations as acceptance.

- [ ] ac-01: TransportSense_MediaCommandMatrix covers five commands from STOP, absent/read-only/writable images, enabled-state sense gating and later STOP. Reject wrong polarity, ignored-command state changes and missing transitions.
- [ ] ac-02: MachineSense_C64CpuPortAndVic20Via1 covers both machines and DDR/output masking; TapeRead_PulseInterruptRouting separately covers CIA1 FLAG and VIA2 CA1. Reject wrong port/bit/direction/interrupt target and extra or missing pulse events.
- [ ] ac-03: Run all recording eligibility/window/edge cases in the ledger, including startup, pending stop, after nominal deadline before callback and after executed stop. Assert writes/no-writes and retained line state. Reject no-image recording and invented startup inhibits.
- [ ] ac-04: Run both RECORD/motor orderings and gap/error cases including 0/6/7/8/2046/2047, remainder carry, same/future-clock edges, v0/v1/v2, 0xffffff/0x1000000/0x1000001 and zero/one/two/three-byte long-payload writes. Assert bytes, image position/size/dirty state, counters, retained state and STOP side effects against independent native expectations.
- [ ] ac-05: Reuse all fixtures/assertions with actual immutable native and managed providers, then run full cumulative suites. Missing, failed, skipped, aborted or mock-only cases cannot satisfy AC; require independent AGREE, both scores at least 98 and zero FAIL/UNKNOWN.
- [ ] ac-06: Run machine motor/write cases for C64 bit 5/bit 3, input/output DDR and DDR-only changes, VIC-20 PCR raw-byte motor and VIA2 PB3/DDRB transitions, repeated writes and SX-64. Wrong bit, normalized-but-nonnative PCR behavior, omitted DDR edge and connected SX-64 mocks must be rejected.
- [ ] ac-07: Run datasette and tapeport enable/dispatch/attach/detach/reset cases with each reset-resource branch. Reject UI-only internal-state assertions, synthesized motor clears and universal tapeport-active suppression.
- [ ] ac-08: Run sense-dongle lifecycle and CP Clock F83 truth-table cases. Reject datasette-only sense, forced deassertion by the dongle disable callback, wrong SDA inversion, wrong SCL normalization and incorrect RTC sense combinations.

## FR-TAP-001

Fields changed: body, acceptanceCriteria

### before

Title: Datasette Motor Control
Status: pending

The Commodore Datasette tape drive motor is controlled by bit 3 of the 6510 I/O port ($0001). The motor is active-low (writing 0 turns the motor on). The motor has a spin-up delay before tape movement begins and a mechanical inertia effect.

## Acceptance Criteria
- Bit 3 of $0001 controls the motor: 0 = motor on, 1 = motor off.
- The motor spin-up delay (approximately 0.5 seconds on real hardware) is modeled.
- The tape position advances only when the motor is running and spin-up is complete.
- The PLAY button state (bit 4 of $0001, cassette sense) must be active (low) for the motor to engage.
- Motor state changes are reflected in the `ITapeUnit.MotorState` property.
- Audio output from the tape (when connected to SID EXT IN) is modeled.

- [ ] ac-01: Bit 3 of $0001 controls the motor: 0 = motor on, 1 = motor off.
- [ ] ac-02: The motor spin-up delay (approximately 0.5 seconds on real hardware) is modeled.
- [ ] ac-03: The tape position advances only when the motor is running and spin-up is complete.
- [ ] ac-04: The PLAY button state (bit 4 of $0001, cassette sense) must be active (low) for the motor to engage.
- [ ] ac-05: Motor state changes are reflected in the `ITapeUnit.MotorState` property.
- [ ] ac-06: Audio output from the tape (when connected to SID EXT IN) is modeled.

### after

Title: Datasette Motor Control
Status: pending

Motor input, native running state, transport motion, public state exposure and tape audio shall follow pinned immutable VICE for the selected machine and resources. C64 motor control is DDR-masked CPU-port bit 5, not bit 3. Ordinary startup scheduling and actual delayed-stop callback execution are distinct from physical-drive approximations. Preserve the existing public MotorState obligation and tape-audio functionality.

- [ ] ac-01: C64 CPU-port bit 5 controls active-low motor output as !((DDR & DATA) & 0x20); input direction drives motor-on. Bit 3 is tape write, not motor. VIC-20 and SX-64 behavior follows FR-RUNTIME-TAPE-002 ac-07.
- [ ] ac-02: Use the native emulated-clock schedule: ordinary start adds 32000 only if no alarm is pending; FORWARD/REWIND uses its 1000-cycle command alarm. Do not substitute an approximate real-hardware half-second timer.
- [ ] ac-03: Tape motion, read-pulse delivery and stopping match the ordered native transport/alarm trace, including late stop execution and pending-alarm cases. Do not conflate immediate native running-state assertion with first movement or apply a universal spin-up-complete gate to every operation.
- [ ] ac-04: PLAY/sense is not a prerequisite for asserting the native motor-running flag. Cover motor-on in STOP and RECORD, and distinguish transport mode/sense from motor control and running state.
- [ ] ac-05: Motor state changes are reflected in the `ITapeUnit.MotorState` property.
- [ ] ac-06: Tape audio remains required. With identical sound-enabled/volume/sample-format/rate and tape halfwave/gap inputs, match pinned VICE datasette sound buffer and output samples, including disabled flush, buffer wrap, empty output and gap boundaries. Native datasette sound-chip output, not an assumed SID EXT IN route, is the acceptance target.

## FR-TAP-002

Fields changed: title, body, acceptanceCriteria

### before

Title: TAP File Format Support (v0 and v1)
Status: pending

The TAP file format stores raw tape pulse timing data. TAP v0 stores pulse lengths as single bytes (with 0 indicating an overflow requiring 3 additional bytes). TAP v1 extends the overflow encoding. The emulator shall read and write both TAP format versions.

## Acceptance Criteria
- TAP v0 files are correctly parsed: each byte represents a pulse half-period in units of 8 system clock cycles.
- A byte value of 0 in TAP v0 indicates the next 3 bytes form a 24-bit little-endian overflow value.
- TAP v1 files use an extended header and the same pulse encoding with improved overflow handling.
- The TAP header (magic bytes, version, platform, video standard, data length) is validated on load.
- Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.
- Writing to TAP format captures the pulse timing from emulated writes.
- The platform byte in the header distinguishes C64, VIC-20, and C16/Plus4 tapes.

- [ ] ac-01: TAP v0 files are correctly parsed: each byte represents a pulse half-period in units of 8 system clock cycles.
- [ ] ac-02: A byte value of 0 in TAP v0 indicates the next 3 bytes form a 24-bit little-endian overflow value.
- [ ] ac-03: TAP v1 files use an extended header and the same pulse encoding with improved overflow handling.
- [ ] ac-04: The TAP header (magic bytes, version, platform, video standard, data length) is validated on load.
- [ ] ac-05: Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.
- [ ] ac-06: Writing to TAP format captures the pulse timing from emulated writes.
- [ ] ac-07: The platform byte in the header distinguishes C64, VIC-20, and C16/Plus4 tapes.

### after

Title: TAP File Format Support (v0, v1 and v2 native behavior)
Status: pending

Read/write TAP behavior shall match pinned VICE at the corresponding file/block decoder and live datasette layer. v0 zero does not consume a three-byte extension. v1/v2 extension, halfwave, header, clock and resource behavior must not be merged into a different invented format. Retain public mount/eject and machine-tag obligations.

- [ ] ac-01: For nonzero TAP bytes, preserve native pulse units and layer-specific conversion: file/block decoding uses pulse units while live datasette gaps use byte times eight before resource adjustments. Do not globally label every v0 byte a half-period.
- [ ] ac-02: A v0 zero consumes only that byte. The file/block decoder returns 256 pulse units; the live datasette uses DatasetteZeroGapDelay and its adjustments. Adjacent bytes remain separate pulses, not a 24-bit extension.
- [ ] ac-03: For v1/v2 zero, cover three-byte little-endian extensions, truncated extension and all-zero length at each native layer. File/block v2 combines two halfwaves; live behavior depends on selected machine tape behavior and retains its native direction/resource effects.
- [ ] ac-04: Header handling matches native short-read/magic rejection, stored version/system/video values, mismatch warning versus error behavior, declared data handling and clock fallback. Do not reject a mismatch that the corresponding native path only warns about.
- [ ] ac-05: Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.
- [ ] ac-06: Writing to TAP preserves native recording eligibility, clocks, byte encoding, first-gap and failure effects from FR-RUNTIME-TAPE-002 ac-05/ac-06, including v0/v1/v2 output behavior and 24-bit truncation.
- [ ] ac-07: Header machine tags distinguish C64, VIC-20 and C16/Plus4 and use the native machine/video clock table. Tests pin selected machine separately from the media header and preserve native mismatch/fallback behavior.
- [ ] ac-08: Cover mount/eject, forward/reverse gap access and truncated media without treating file metadata decoding as a live pulse trace. Both layers require independent expected observations; missing native live evidence fails closed.

## FR-TAP-003

Fields changed: body, acceptanceCriteria

### before

Title: Cycle-Accurate Tape Read Timing
Status: pending

Tape reading relies on the CIA1 Timer A or the FLAG pin to measure the time between pulses from the datasette. The Kernal tape routine uses Timer A in one-shot mode to measure pulse widths and classify them as short, medium, or long pulses for data decoding.

## Acceptance Criteria
- Each pulse from the tape triggers the FLAG input on CIA1 (directly connected to the cassette read line).
- The time between pulses is measured by CIA1 Timer A and/or by CIA1 FLAG interrupts.
- Standard Commodore encoding uses three pulse widths: short (352 cycles TAP value $30), medium (512 cycles TAP value $42), and long (672 cycles TAP value $56).
- The standard Commodore tape header (FOUND marker), data blocks, and checksums are readable.
- Tape reading at normal speed produces identical results to real hardware.
- The timing accuracy supports both PAL and NTSC system clock rates.

- [ ] ac-01: Each pulse from the tape triggers the FLAG input on CIA1 (directly connected to the cassette read line).
- [ ] ac-02: The time between pulses is measured by CIA1 Timer A and/or by CIA1 FLAG interrupts.
- [ ] ac-03: Standard Commodore encoding uses three pulse widths: short (352 cycles TAP value $30), medium (512 cycles TAP value $42), and long (672 cycles TAP value $56).
- [ ] ac-04: The standard Commodore tape header (FOUND marker), data blocks, and checksums are readable.
- [ ] ac-05: Tape reading at normal speed produces identical results to real hardware.
- [ ] ac-06: The timing accuracy supports both PAL and NTSC system clock rates.

### after

Title: Cycle-Accurate Tape Read Timing
Status: pending

Tape read pulse delivery, timer/interrupt effects and data decoding shall match immutable VICE for the selected C64 or VIC-20, clock/resources, firmware and workload. File/block decoder thresholds and real machine signal timing are different observable layers; neither can substitute for the other.

- [ ] ac-01: C64 read pulses reach CIA1 FLAG and VIC-20 read pulses reach VIA2 CA1 with the pinned native edge/state semantics. Tape sense remains a distinct input.
- [ ] ac-02: With identical pinned firmware and tape workload, timer/interrupt observations used to measure pulses match native VICE, including boundary and pending-interrupt state. A decoder return value is not timer/interrupt evidence.
- [ ] ac-03: Configured C64/VIC-20 file-decoder CBM pulse-unit ranges are inclusive 36-54 short, 55-73 middle and 74-100 long. Test endpoint and adjacent values after tap_init conversion. Keep live pulse clocks separate; do not pair inconsistent fixed cycle counts with TAP byte values.
- [ ] ac-04: Pinned standard Commodore header, data-block and checksum workloads produce the same decoded bytes/status and machine-visible load outcome as native VICE. Include malformed, truncated and checksum-error fixtures.
- [ ] ac-05: At normal speed, the same named tape workload produces the same ordered native and managed machine-visible pulse/state/output observations. Hardware assumptions or source inspection alone cannot replace the immutable native target.
- [ ] ac-06: Run PAL and NTSC selected-machine/resource profiles with separately pinned tape headers and clock domains. Preserve native differences and any lack of cross-standard rescaling; do not improve the native target.

## FR-TAP-004

Fields changed: body, acceptanceCriteria

### before

Title: Tape Write Support
Status: pending

The emulator shall support writing data to tape images via the datasette write line. The write signal from the VIA/CIA generates pulses that are recorded to the TAP file with correct timing.

## Acceptance Criteria
- The tape write line output generates pulses whose timing is captured.
- Written pulses are stored in TAP format with correct timing values.
- The standard Commodore SAVE routine produces valid tape data.
- Writes can be appended to existing TAP files or written to new files.
- The record interlock (RECORD + PLAY buttons) must be engaged for writes.

- [ ] ac-01: The tape write line output generates pulses whose timing is captured.
- [ ] ac-02: Written pulses are stored in TAP format with correct timing values.
- [ ] ac-03: The standard Commodore SAVE routine produces valid tape data.
- [ ] ac-04: Writes can be appended to existing TAP files or written to new files.
- [ ] ac-05: The record interlock (RECORD + PLAY buttons) must be engaged for writes.

### after

Title: Tape Write Support
Status: pending

Write data to tape images using the selected machine native write-line path and accepted datasette RECORD state. C64 uses CPU-port bit 3; VIC-20 uses effective VIA2 PB3. Preserve native timing, image position, existing content, first-write quirks and write failures, not an in-memory pulse-count substitute.

- [ ] ac-01: Capture qualifying native write-line transitions from C64 CPU-port bit 3 and VIC-20 VIA2 PB3, including DDR-only changes, repeated/opposite edges and SX-64 disconnection.
- [ ] ac-02: Store the same bytes, gap/remainder, last-write state, file position/size/dirty state and counters as pinned VICE across short/long/24-bit boundaries and partial/failed output.
- [ ] ac-03: A standard Commodore SAVE workload with pinned firmware, writable tape, resources and initial state produces the same bytes/status and machine-visible effects as immutable VICE, and its corresponding native/managed load results match. Include failure and write-protected cases.
- [ ] ac-04: New-image, existing-image current-position and explicit end-position recordings preserve the same prefix/suffix and file-position/size effects as native VICE. Existing-file writes must not silently become unconditional append or truncate.
- [ ] ac-05: Writable-image RECORD acceptance is the native interlock; absent/read-only-image RECORD is ignored. Do not require an additional independent PlayPressed boolean once native RECORD mode is accepted. Motor-running and qualifying-edge conditions still apply.

## TEST-TAP-001

Fields changed: title, condition, acceptanceCriteria

### before

Title: Tape and Datasette Tests
Status: pending

Datasette motor, TAP parsing, pulse timing, write behavior, and turbo loader compatibility are verified with unit and timing tests.


### after

Title: Tape format, machine I/O, peripherals, audio and loader contracts
Status: pending

Retain all existing datasette motor, TAP parsing, pulse timing, tape write and turbo-loader coverage, and explicitly map both historical meanings of TR-TAP-EDGE-001/TR-TAPE-EDGE-001. Unit assertions first discriminate independent correct/wrong mocks in a green suite, then remain unchanged for real managed/native providers and named workload gates.

- [ ] ac-01: Cover FR-TAP-001 motor control/timing and FR-RUNTIME-TAPE-002 command/sense/motor/write behavior, including pending-alarm stop, fast wind, DDR-only transitions, both RECORD/motor orders, failures and lifecycle. Reuse TEST-TAPE-RAMP-001 and TEST-TAPE-SENSE-001 cases without omitting them from cumulative gates.
- [ ] ac-02: Cover all FR-TAP-002 and TR-TAP-EDGE-001 decoder/header/clock/version scenarios, with separate block-decoder and live-datasette assertions. Reject v0 extension consumption, universal halfwave conversion, incorrect truncation, mismatch rejection and clock-domain conflation.
- [ ] ac-03: Cover FR-TAP-003 and FR-TAP-005 pulse boundaries, CBM and TurboTape pilot/header paths, standard load/checksum outcomes and every already-required named turbo format (Novaload, Biturbo, Turbotape, Cyberload, Pavloda), half/full-wave and changing-speed workloads. Pin actual fixtures and firmware; missing fixture, oracle or observation is a failure, never an invented successful loader result.
- [ ] ac-04: Cover retained public motor-state/mount/eject obligations, native tape-audio behavior and machine-visible SAVE/reload/new-existing-append results. Wrong API state, audio samples, persisted bytes, position or failure effects must be rejected by independent negative controls.
- [ ] ac-05: Cover sense dongle enable/disable/power-up and CP Clock F83 motor/SDA/write/SCL/sense cases from TR-TAPE-EDGE-001 ac-06, with device selection and no-datasette cases.
- [ ] ac-06: For every AC-mapped case, valid independent scripted mock responses pass all assertions. Deliberately wrong value, missing/extra/duplicate/reordered observation, wrong clock/side effect and failure-versus-success response must be rejected as an expected negative control within a passing native-safe suite. No failing suite or production-derived expected value qualifies.
- [ ] ac-07: Bind the same inputs/assertions to real managed/native providers, retain exact identities and first divergence, run full applicable current-plus-prior suites with zero failed/skipped/aborted cases, and obtain AGREE at accuracy/completeness at least 98 with zero FAIL/UNKNOWN before satisfaction. Preserve all original xvic/x64sc/VSF and later start-approval gates.

## FR-BACKFILL-MEDIA-001

Fields changed: title, body, status, acceptanceCriteria

### before

Title: Media devices (D64, tape) I/O functional parity
Status: closed

D64DiskImageDevice sector R/W, motor ramp, BAM read all implemented. Datasette motor ramp, sense line, and record mode complete. Closes IEC ATN timing gap for Phase 1.


### after

Title: D64, tape and IEC media behavior matches immutable native VICE
Status: pending

Managed media behavior shall reproduce the pinned native VICE reference at the corresponding layer: D64 image-sector data/status/persistence, true-drive IEC and motor behavior, and machine-specific datasette timing/sense/record behavior. Image-buffer success, synthetic motor counters and historical test totals cannot substitute for real-emulator parity. Preserve the separate buffered and true-drive modes and all legacy evidence.

- [ ] ac-01: For the same pinned D64 fixtures and operation sequence, sector reads/writes, returned semantic status, error-map effects and persisted bytes match native VICE; cover valid sectors, geometry boundaries, malformed/truncated input, read-only input, read errors and failed writes without treating success-only BAM reads as complete coverage.
- [ ] ac-02: True-drive IEC line contributions, both ATN edges, enabled/disabled drive cases, contention, drive motor/rotation progression and DOS-mediated BAM-read bytes/status/effects satisfy the separately proposed FR-ARCH-TRUEDRIVE-1541-002 criteria against the same native initial conditions. Direct image access cannot satisfy true-drive acceptance.
- [ ] ac-03: Datasette motor startup, delayed stop/cancellation, pulse delivery, sense-line wiring and recording on writable media satisfy the separately proposed FR-RUNTIME-TAPE-002 criteria for C64 and VIC-20. A metadata pulse read or in-memory recording count alone is insufficient.
- [ ] ac-04: Each accepted media scenario retains fixture/firmware/resource hashes, mode, initial state, clock domain, operation sequence, ordered observations and first mismatch. Reusing a host-cycle deadline as a drive/tape clock assertion or comparing different media modes is rejected.
- [ ] ac-05: All preceding assertions first pass with independent discriminating mocks and expected negative controls, then the same inputs/assertions pass with real providers and the full applicable suites. Missing, skipped, failing or mock-only evidence leaves the criterion unsatisfied.

## Mapping additions only

- FR-BACKFILL-MEDIA-001: add TR [TR-IEC-EDGE-001, TR-MEDIA-D64-001]; add TEST [TEST-IEC-TIMING-001, TEST-D64IO-001]. Existing links retained.
- FR-RUNTIME-TAPE-002: add TR []; add TEST []. Existing links retained.
- FR-TAP-001: add TR [TR-TAPE-EDGE-001, TR-TAP-EDGE-001]; add TEST [TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001]. Existing links retained.
- FR-TAP-002: add TR [TR-TAP-EDGE-001]; add TEST []. Existing links retained.
- FR-TAP-003: add TR [TR-TAP-EDGE-001]; add TEST []. Existing links retained.
- FR-TAP-004: add TR [TR-TAP-EDGE-001, TR-TAPE-EDGE-001]; add TEST [TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001]. Existing links retained.
- FR-TAP-005: add TR [TR-TAP-EDGE-001]; add TEST []. Existing links retained.
