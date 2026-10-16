# Tape motor and alarm matrix

R0 planning supplement, prepared by Codex on PAYTON-LEGION2 from 2026-10-11T08:54:02-05:00. No requirement application, test execution or acceptance. The base proposal is the fourteen-record `20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json`, SHA256 `320887C5F2F09154F1EC7970B041A638632741A7750E838FE05F77883BB1DA83`.

This specifies FR-RUNTIME-TAPE-002/ac-03 and ac-04. It retains all 15 named groups for those criteria. The other six criteria and all 26 of their groups remain required. This is not a replacement for the full tape suite.

## Source and observation boundary

Authority: immutable VICE `cc67418cba58e71d8e70fd353e9de47199ad2075`, `vice/src/datasette/datasette.c` and `vice/src/alarm.h`. Their Git blobs and SHA256 values are in `motor-matrix.json`. No native file is edited or built.

These are source-derived contract-state vectors for future discriminating mock tests. They do not authorize injecting private state into native VICE. The real provider must reach the declared state through a retained public-operation prelude or an independently validated native snapshot, and prove it with an accepted external observer. A missing observation or unreachable seed blocks that real case. It does not permit fabricated native values or native edits.

The normalized tuple is `(M,D,A,W,B,Q)`: running motor flag, stop deadline, pending datasette alarm clock or null, last write clock, last write bit, and active image/no-tape mode. A=0 is a pending alarm at clock zero, not absence. D=0 and W=0 have native sentinel meanings. Callback clock C and callback lateness O are separate fields. The callback compares D to C, not C-O.

Each vector names an entry action, its before/after tuple, and the ordered motor/alarm effects. Expand applicable vectors across C64 and VIC-20 and media none/read-only/writable, as declared in JSON. This routine-boundary equivalence does not prove machine-port wiring. Read-only versus writable media must remain distinct rows even where motor outcomes match.

No event playback or network deferral is active. Datasette is enabled; port and alarm are initialized. Within `datasette_start_motor` with media, observe `fseek(fd, logicalSeek + imageOffset, SEEK_SET)` before that helper's conditional alarm installation. A fast command has already set its alarm before calling this helper. Use distinct logical and physical cursor sentinels so an omitted seek is visible. Clock-only advance never implicitly calls the callback in a vector. The eventual scheduler adapter must represent operation and dispatch ordering explicitly, including same-clock events.

## Startup and command vectors

`Startup_NoPendingAlarm_DeadlineBoundaries`: motor-on from M=0 at C=100000 installs A=132000, seeds W=100000 and B=0, reports motor-on, then sets M=1. D becomes zero. Observe at 131999 and 132000 before dispatch: M is already 1. With Q=STOP, dispatch at 132000 unsets A, retains M=1 and does not read tape. Repeat at C=0: A=32000 and W remains the zero sentinel. No test may reinterpret the motor flag as false throughout the startup delay.

`Startup_PendingAlarm_NotOverwritten`: from M=0 with A=100500, motor-on at 100000 preserves A, seeds W/B, and makes M=1. Also retain the boundary seed A=0 in mock discrimination. Its real reachability and same-clock dispatcher order must be proved, not assumed.

`Start_CommandWhileMotorRunning_NoPendingAlarm`: Q=STOP, M=1, W=99000, B=1, no alarm. START at 100000 sets Q=START, emits sense 1, resets W to zero, calls the start helper, seeks attached media, sets A=132000, seeds W=100000/B=0, reports control state and clears both tape-buffer indices. It does not report a motor transition. Repeat at C=0 and with an existing D=132000. START does not clear D.

`Start_CommandWhileMotorRunning_PendingAlarm`: repeat with A=150000 and A=100500. Preserve that exact alarm even when it is earlier than a new startup deadline. Still reset and reseed W/B and seek attached media. A command is not the same operation as setting an already-on motor line: the latter preserves W/B.

`FastWind_ExistingReadAlarm_Replaced`: FORWARD and REWIND at C=100000 install A=101000 regardless of M or prior A. The command sets Q before invoking its fast helper, so the helper's old-mode-looking condition does not add an alarm-unset on this path. `alarm_set` modifies an already-pending alarm in place. For M=1 the subsequent start helper seeks media and seeds W=100000/B=0 but preserves A=101000. For M=0, W becomes zero through gap reset and B retains its old value. Control status and buffer reset still occur. Preserve an existing D when M=1.

`FastWind_MotorOnBeforePendingCallback`: fast command while stopped at 100000, then motor-on at 100100. Keep A=101000, seed W=100100/B=0 and set M=1. The helper must not replace A with 132100. For no media, dispatch at 101000 consumes the alarm, keeps M=1 and generates no flux. Attached-media forward/rewind callback traces additionally require the actual direction, cursor, tape-speed calculation and next alarm; the JSON explicitly leaves those real-provider traces open rather than calling this pre-callback vector sufficient.

`FastWind_MotorStopped_CallbackBeforeMotorOn`: fast command while M=0, dispatch at 101000 before motor-on. Callback unsets A and returns immediately, without reading tape or changing W/B. Motor-on at 101001 then installs A=133001 and seeds W=101001/B=0. Compare this with the preceding operation order; identical final mode is not sufficient.

Sources: `datasette.c:62-67,695-712,729-800,925-949,1000-1020,1035-1156,1172-1205`; `alarm.h:146-184`.

## Delayed stop and cancellation vectors

Use M=1, W=99000, B=1, D=0 at C=100000 unless the row states otherwise. Motor-off never resets W/B or immediately reports motor-off.

`MotorOff_NoPendingAlarm_StopsAtDeadline`: off installs D=A=132000. At clock 131999 without dispatch, state is unchanged. At 132000 before dispatch, M remains 1. The callback first unsets A; then at C>=D clears D, reports motor-off, sets M=0, and returns before flux or image reads. Include dispatch at 132000/O=0 and 132010/O=10 as separate rows. No wall-clock delay or pulse getter establishes this behavior.

`MotorOff_PendingReadBeyondDeadline_DoesNotInsertSecondAlarm`: with A=150000, off sets D=132000 but keeps A=150000. Before that callback M remains 1, even after 132000. At 150000 the callback stops the motor before read/flux effects. Do not create a second independent stop timer.

`MotorOff_StopModeWithExistingLateAlarm`: use Q=STOP, A=150000. Retain the late alarm. STOP transport state is not a motor stop. Dispatch at 150000 follows the deadline check before examining Q.

`MotorOff_RepeatedDoesNotExtend`: issue off at 100000, 100100, 132000 and 132001 without callback dispatch. All later requests preserve the original D and A and retain M=1. A pending deadline is not renewed based on the latest command clock.

`MotorOn_CancelsPendingStop`: with D=A=132000, issue on at 131999 and, in a separate row, at 132000 before dispatch. Clear D only; retain M, A, W and B. No seek, new alarm, write reset or motor UI transition occurs. Dispatch after cancellation with Q=STOP consumes A but leaves M=1.

`MotorOn_CancelsStopAfterDeadlineBeforeCallback`: perform on at 132001 before the due callback. Exactly the same cancellation applies. A due timestamp does not imply that the callback ran. With a retained later alarm A=150000, cancellation also keeps that later alarm.

`MotorOn_AfterExecutedStop_Restarts`: reverse the same-clock order. Dispatch first at 132000, producing M=0,D=0,A=null. Then on at 132000 enters the startup helper, seeks attached media, sets A=164000, seeds W=132000/B=0, reports motor-on and sets M=1. This differs from on-before-dispatch and must be a distinct expanded case.

`MotorOff_ReadBeforeDeadline_ReschedulesPastDeadline`: Q=START, M=1, D=132000, pending read A=131900. At C=131900/O=0 callback unsets A, schedules D temporarily, emits flux when enabled and no long gap is pending, then processes a 4096-cycle gap and replaces A with 135996. D remains 132000. At 132000 without dispatch M stays 1. Dispatch at 135996 first stops the motor and produces no next pulse. In the O=8 variant, the pending read was scheduled at 131892 but dispatch still occurs at C=131900, making the next alarm 135988. Neither variant may retain 132000 as a separate stop alarm.

The straddling-lateness row instead has A=131900, D=132000 and actual dispatch C=132000/O=100. It must stop immediately, with no flux, read, sound or rescheduling. Comparing D to scheduled time C-O would incorrectly read another gap; this row rejects that substitution.

For this active-read case use regular C64/VIC-20 tape behavior, TAP v1 long-gap bytes `00 00 10 00`, speed tuning 0, wobble frequency and amplitude 0, azimuth error 0, last direction +1, pending and elapsed long gap zero, fullwave 0, and enough valid following data. The normalized gap is exactly 4096, below the 100000 split threshold. After the read, logical cursor advances four bytes, elapsed long gap becomes 4096 and cycle counter advances 512. Flux occurs before fetching that gap; sound receives 4096 before lateness subtraction. Real file/header/cursor fixtures remain separately pinned prerequisites, not supplied by these four payload bytes.

Also distinguish an early STOP-mode callback: with attached media it schedules D once and returns. With no media and Q=STOP it schedules the same D a second time before UI-counter update. Final tuples match but ordered calls differ. This is a companion case, not permission to discard duplicate same-clock calls.

Sources: `datasette.c:391-409,449-452,482-535,599-632,722-852,1172-1205`.

## Negative controls and retained limits

For every row, separately mutate each changed tuple field and each required unchanged sentinel. Reject omitted, duplicated, reordered and wrong-clock alarm operations. Explicit mutations include immediate off, renewed off deadline, separate stop alarm, startup overwriting an existing alarm, fast command retaining its old alarm, running motor-on reseeding W/B, START failing to reseed W/B, treating clock zero as a nonzero seed, callback using C-O for stop comparison, and collapsing same-clock action order. Each must be an expected rejection inside a green future mock harness.

The numerical vectors cover the routine-boundary motor/alarm projection only. They do not prove recording bytes, sense/pulse wiring, event/network dispatch, snapshots, complete file cursor behavior, true-drive behavior or native execution. Those obligations remain in the full packet. No tests or requirements are retired and no counted row is a passed test. Whole-packet HV and explicit expanded-delta approval remain required before implementation.
