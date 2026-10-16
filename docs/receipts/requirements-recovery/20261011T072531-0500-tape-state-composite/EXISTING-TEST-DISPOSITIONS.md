# Existing tape-test disposition ledger

Status: proposed, no tests edited or executed. This ledger covers the fourteen named tests inspected in four files, not the whole cumulative suite. Apply changes only after exact requirement approval, then prove the corrected assertions using discriminating mocks before production changes. Preserve existing inputs and prior assertions in retained history; do not skip/delete tests to obtain green results.

Source paths are relative to `F:\GitHub\vice-sharp`. Every disposition below requires the same assertions at mock and real-provider gates, plus the full cumulative suites. Method renaming alone is not remediation.

## StorageRuntimeTests.cs

Path: `tests/ViceSharp.TestHarness/StorageRuntimeTests.cs`.

1. `Tap_V0_OverflowPulse_Reads_ThreeByteCycleCount`, lines 181-199. Current fixture is version 0 with bytes `[0, 0x40, 0x1F, 0]`; current assertions expect one 8000-cycle pulse then EOF. That consumes an extension which native v0 does not consume. Preserve those exact bytes as a regression fixture, but after approval split expected observations by layer: native block pulse units are 256, 64, 31, 256; neutral regular live gaps use `DatasetteZeroGapDelay`, 512, 248, `DatasetteZeroGapDelay`. Do not substitute one layer's units or configurable zero-gap default for the other. Add independent negative controls for extension consumption, wrong alignment and premature EOF. Requirements: `FR-TAP-002/ac-02`, `TR-TAP-EDGE-001/ac-05`, `TEST-TAP-001/ac-02`. The public managed adapter's units must be explicit before binding the corrected assertions.
2. `Tap_Attach_Rejects_Truncated_Pulse_Data`, lines 202-217. **Keep the rejection for its exact two-byte payload.** The old explanation attributes rejection to a declared length of three; native `tap_open` rejects any physical payload shorter than three. Rename/document this as a minimum-physical-size case after approval. Add valid physical payloads of at least three bytes with declared length less/equal/greater than actual, including the same mismatch independently of the minimum size. Add 0/1/2/3-byte boundary cases. Do not simply invert the old assertion to acceptance. Requirements: `FR-TAP-002/ac-04`, `TEST-TAP-001/ac-02`.

This qualifies N-15 in the retained HV: the test's attribution is defective, but its current rejection result alone does not contradict native behavior. A new discriminating fixture is necessary.

## FrCpu004AcceptanceTests.cs

Path: `tests/ViceSharp.TestHarness/FrCpu004AcceptanceTests.cs`.

3. `PortBit3_ControlsTheCassetteMotorActiveLow`, lines 67-90. The name says bit 3, while its writes/assertions exercise DDR-masked bit 5. Preserve the existing write sequence as one C64 output-direction case and correct the name/FR commentary after approval. Expand to data/DDR truth tables, input/output transitions, repeated effective outputs and separate write-bit behavior. Clarify whether `MotorEnabled` denotes the control input or native retained running state; do not treat immediate input-off as proof that the delayed running motor already stopped. Requirements: corrected `FR-CPU-004/ac-03`, `FR-RUNTIME-TAPE-002/ac-04/ac-07`, `TR-TAP-EDGE-001/ac-02`.

## Tape/DatasetteSenseLineTests.cs

The class comments incorrectly place sense on CIA1 port B bit 4. Machine wiring must instead be asserted at the C64 CPU port and VIC-20 VIA1 sense input, separately from CIA1 FLAG/VIA2 CA1 pulse routing. A local `SenseLine` property test is not machine-wiring evidence.

4. `SenseLine_True_WhenNoButtonPressed`: retain a fresh STOP idle case with explicit device selection, enable state, no-image state and line polarity. Do not generalize it to every reset with retained no-image mode.
5. `SenseLine_False_WhenPlayPressed`: retain accepted no-image START as one case; add writable/read-only image and enabled/disabled forwarding cases, plus actual machine-visible port reads with DDR masks.
6. `SenseLine_False_WhenRecordPressed`: current no-image RECORD success expectation is superseded. Preserve the no-image input as an ignored-RECORD case, retain unchanged mode/sense assertions, and separately add read-only rejection and writable acceptance. Do not add a tape silently to the old test and thereby lose the no-image case.
7. `SenseLine_ReturnsHigh_WhenPlayReleased`: retain accepted START-to-STOP control sequence, with intermediate and final signal observations; map property release explicitly to native STOP rather than assuming all lifecycle paths are equivalent.
8. `TryWritePulse_StorePulse_WhenRecordPressed`: current no-image/in-memory count success is not recording parity. Preserve no-image operations as a rejection case. Add attached writable RECORD with qualifying line edges, actual native running state, encoded bytes, cursor/logical position, size/dirty state and failure effects. Keep count as auxiliary API behavior if independently required; it cannot replace output-byte evidence.
9. `TryWritePulse_ReturnsFalse_WhenNotRecording`: retain the negative case, but add an attached writable image with running motor so rejection discriminates RECORD eligibility rather than merely missing media. Preserve no-image case separately.
10. `TryWritePulse_ReturnsFalse_WhenMotorOff`: retain initially stopped rejection, but add attached writable accepted RECORD with startup, pending delayed stop and executed-stop cases. Control input off is not equivalent to the native running flag off before the deadline callback.

Requirements across items 4-10: `FR-RUNTIME-TAPE-002/ac-01/ac-02/ac-04/ac-05/ac-06/ac-08`, `TR-TAP-EDGE-001/ac-01..ac-04`, `TEST-TAPE-SENSE-001`, and relevant `TEST-TAP-001` rows. Retain the original scenarios as individually identifiable cases rather than replacing this file with one broad success assertion.

## Tape/DatasetteMotorRampTests.cs

`BuildMinimalTap` supplies a one-byte physical payload. Native `tap_open` rejects it. Preserve that fixture as an invalid-image boundary case; create separately named, valid images of at least three bytes for motor behavior. Never attribute a failed mount or absent reader to successful ramp blocking.

11. `NoTape_TryReadNextPulse_ReturnsFalse_Regardless`: retain no-image/no-pulse outcome with explicit accepted control sequence. Add mode, sense and alarm observations where required; no pulse alone does not prove the motor path.
12. `MotorOn_WithTape_PulseBlockedDuringRamp`: retain the 12,808-clock intermediate observation where appropriate, but remove the unrelated IEC timing justification. With a valid mounted image and pinned no-pending-alarm startup, assert the scheduled 32,000-clock boundary and actual callback behavior at boundary-1/boundary/boundary+1. The managed pulse getter is not by itself a native event trace.
13. `MotorOn_WithTape_PulseAvailable_AfterFullRamp`: retain valid-image ordinary startup as one case, with exact initial clock and scheduled-versus-dispatched event observation. Add already-pending-alarm and already-running START cases from the correction instead of imposing universal new-delay behavior.
14. `MotorOff_ThenOn_ResetsRamp`: immediate off/on before an executed stop must not be redefined as a full new ramp. Preserve the original sequence, including REWIND and the intervening tick, as a separate fast-wind/pending-alarm case. Add simple off/on cancellation cases without REWIND, at pre-deadline and same-clock orderings, and a distinct restart after an actually executed stop. Verify original operation ordering in each fixture; do not silently remove REWIND to obtain the expected result.

Requirements: `FR-RUNTIME-TAPE-002/ac-03/ac-04/ac-08`, `TR-TAPE-EDGE-001/ac-01..ac-05/ac-07`, `TEST-TAPE-RAMP-001`, and `TEST-TAP-001/ac-01/ac-02` for valid versus invalid media.

## Required follow-through

Search the complete current test/fixture corpus for each amended ID and semantic pattern before R2. Expand this ledger for additional affected tests. For every actual test edit, record old input/assertion, approved new AC, retained or additional scenario, independently pinned expectation and mock/real receipts. A green focused run does not close cumulative gates. No current failure, skip or unexecuted mandatory case may be reclassified as passing by this ledger.
