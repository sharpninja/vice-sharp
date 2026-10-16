# Tape amendment: exact N-05/N-06 correction supplement

Source inspection completed locally through 2026-10-11T07:01:58-05:00. Draft only, unapplied, not an independent acceptance gate. Compose after the sibling N-01/N-02 supplement; preserve the original eleven-record proposal and every existing case.

## Pinned evidence

Original proposal: `../20261009T154620-0500-tape-amendment-proposal/proposed-field-delta.json`, SHA256 `1D8A442588ADD7883693694352954DF2077D166FB47ED4E2924AFE738A481507`.

Native `vice/src/datasette/datasette.c` at commit `cc67418cba58e71d8e70fd353e9de47199ad2075` matches Git blob `06579175a8ae151fc83972487234d417a4743ec4`, SHA256 `FF38524B8CB131DA01BA62275A1F43CA7A813F87C279D3F55A8A7226E42FD368`. Read-only comparison used `git rev-parse` for the pinned object and `git hash-object` for the separate materialized source. No native edit, generation, build or runtime test occurred.

## N-05: two entry paths reach the start helper

`datasette_start_motor` is not restricted to a stopped-to-running transition. START, FORWARD and REWIND also call it when the native running flag is already set, with or without an image. A contract that checks only the transition omits this second entry path.

Source-derived behavior:

- Lines 62-63 define ordinary delay 32000 and fast-wind delay 1000.
- Lines 1000-1021: attached-image seek, alarm installation only if none is pending, and write-clock/write-bit seeding when the write clock is zero.
- Lines 1059-1089 and 1117-1147: commands assign mode, adjust sense, reset the gap and call the helper if the motor is running, in both image/no-image branches.
- Lines 925-949: FORWARD/REWIND schedule their 1000-cycle alarm before the later start-helper call, so that helper preserves the newly pending fast-wind alarm. Do not characterize FORWARD/REWIND as necessarily scheduling 32000 here.
- Lines 1172-1205: a motor-line call has a different state transition from a START command. A nonzero motor-line call clears the stop clock; START does not contain that clearing operation. If the motor is already running, motor-line re-enable does not call the start helper.

An attached-image START while running seeks to `current_file_seek_position + offset`. No-image START does not seek. Both reset the write clock before the helper seeds it with the current clock and sets the retained write bit to zero. At clock zero the seeded clock is still zero; do not replace that native value with a synthetic nonzero sentinel.

## Exact append-only changes to proposed AC text

For each section below, locate the specified AC in the original proposal's `records[].after.acceptanceCriteria`. Preserve its entire existing text, then append one ASCII space and the exact quoted sentence. Preserve IDs, status and satisfaction flags. These are edits to an unapplied proposal, not instructions to append directly to current MCP records.

### FR-RUNTIME-TAPE-002 / ac-03

Append exactly:

> Also cover START while the motor is already running, with absent, read-only and writable media, with no pending alarm and with an existing pending alarm. The command reaches the start helper after resetting the gap: install t+32000 only when no alarm is pending, otherwise preserve the existing alarm; match attached-image seek, current-clock write-clock seeding and write-bit zeroing, including clock zero. Contrast START with running-motor FORWARD/REWIND, whose earlier t+1000 alarm survives the helper, and preserve any stop deadline that the command does not clear.

### TR-TAPE-EDGE-001 / ac-01

Append exactly:

> Exercise the separate command-entry path: START on an already-running motor invokes the start helper with and without media, schedules t+32000 only without a pending alarm, and preserves a pending alarm while still performing the native seek and write-state initialization. Distinguish command START from motor-line re-enable, and preserve the t+1000 alarm already installed by FORWARD/REWIND before their helper call.

### TEST-TAPE-RAMP-001 / ac-01

Append exactly:

> Start_CommandWhileMotorRunning_NoPendingAlarm and Start_CommandWhileMotorRunning_PendingAlarm cover absent/read-only/writable media, clock zero/nonzero, seek/no-seek, retained write state and a pending stop deadline; reject omission of the command helper path, wrong alarm replacement or absence, wrong seek, wrong write-clock/bit seeding, and clearing a stop deadline that START leaves intact.

### FR-RUNTIME-TAPE-002 / ac-04

Append exactly:

> Explicitly include motor-on before the stop deadline and at the deadline before callback dispatch, preserving the already-running motor, pending alarm and write state while clearing the stop deadline; neither case starts a new ramp.

### TR-TAPE-EDGE-001 / ac-03

Append exactly:

> Include cancellation before the deadline and same-clock cancellation before callback dispatch as separate cases from cancellation after the deadline; for an already-running motor, preserve the existing alarm and write state and clear only the stop deadline in this path.

### TEST-TAPE-RAMP-001 / ac-02

Append exactly:

> MotorOn_CancelsPendingStop covers deadline-1 and the exact deadline with motor-on ordered before callback dispatch; assert stop-clock clearing, unchanged running/alarm/write state and no startup ramp, and reject wrong mocks that restart only in these earlier cancellation cases.

## Exact additive case-map changes

Add both `Start_CommandWhileMotorRunning_NoPendingAlarm` and `Start_CommandWhileMotorRunning_PendingAlarm` to each of these existing coverage rows, without removing or renaming any group:

- `FR-RUNTIME-TAPE-002 / ac-03`
- `TR-TAPE-EDGE-001 / ac-01`
- `TEST-TAPE-RAMP-001 / ac-01`

Add `MotorOn_CancelsPendingStop` to each of these existing coverage rows:

- `FR-RUNTIME-TAPE-002 / ac-04`
- `TR-TAPE-EDGE-001 / ac-02`
- `TR-TAPE-EDGE-001 / ac-03`
- `TEST-TAPE-RAMP-001 / ac-02`

Retain the existing `MotorOn_CancelsStopAfterDeadlineBeforeCallback`, `MotorOn_AfterExecutedStop_Restarts` and fast-wind groups. Add source anchors for the command branches and start helper to the START coverage rows; add the motor-line branch to cancellation rows. This adds three unique named groups, not a claim that three executable test cases suffice.

## Fixture and negative-control contract

Before the later mock gate, enumerate fixture rows for all named axes; do not claim full coverage from group names. Retain selected port, machine, media permissions, image seek/offset, native running flag, alarm object and pending state, stop deadline, write clock/bit, command clock, accepted mode, enabled state, sense and ordered effects. Pin event-playback/network routing assumptions for public command entry. An unavailable alarm object is a separate prerequisite/state case, not a reason to silently skip this contract.

For START, expected traces must distinguish a new alarm from preserved alarm while proving that seek/write-state effects still occur. Repeated START must not be collapsed into a no-op. FORWARD and REWIND must preserve their own earlier fast-wind scheduling through the helper. Wrong mocks that give the right final running flag but omit these effects must be rejected inside a green test harness.

For cancellation, include both existing-alarm times at/after the nominal deadline and explicit same-clock ordering. The deadline-1 case must reject a new startup alarm, cleared existing alarm, changed write state or stopped running flag. At the deadline, motor-on-before-callback differs from callback-before-motor-on; the latter belongs to the already-retained executed-stop/restart case. Expected native observations cannot be synthesized from the managed implementation.

## Scope and remaining gates

Six proposed AC texts and seven coverage rows change; no AC or record is added or removed by this supplement. Together with N-01/N-02, the candidate record scope remains thirteen existing records. The complete composite delta, field-level preimage safeguards, remaining findings, full mock and real-provider suites, local Opus HV and explicit expanded-delta approval are still required. This source evidence does not satisfy N-05/N-06 independently or advance any phase.
