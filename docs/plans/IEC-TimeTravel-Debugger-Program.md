# IEC Time-Travel Debugger + True-Drive Program - BDP Plan

Status: IN PROGRESS (per-slice status updated 2026-07-08). Branch base: `1cfbcb1`.

Slice status summary: S1, S4, S7, and S10 have shipped; S9 is device-side only
(no UI binding); S3, S5, and S6 remain open (rewind RPCs still return
NotImplemented; no decoder or scope panel code exists). S2 and S8 were not
re-verified in the 2026-07-08 status pass.

## Goal

Deliver a deterministic, step-through IEC bus debugger (logic-analyzer "scope"
with protocol decode and true reverse-execution) and the true-drive 1541
integration it is used to validate, so `LOAD"*",8,1` works on a single-system
C64 GUI and the operator can watch and step the IEC protocol forward and
backward.

## Non-negotiable constraints

- **Native x64sc parity stays green** after every slice. Any CIA2-serial change
  is gated on `ConnectedCia2IoWritesAndReadsMatchNative`, `C64MachineTests`
  idle-serial reads (`$47` / `$07`), and the lockstep/checkpoint suite.
- **Determinism is the foundation.** Reverse-execution and trace re-derivation
  rely on the core being deterministic (the lockstep guarantee). Any state that
  affects execution but is not captured by a snapshot breaks exact rewind.
- BDP: each slice is tests-first, with a named red state, green criteria, and a
  validation scope. A slice exits only when its scope is 0 failed / 0 skipped.

## Locked design decisions (from design session)

- DD-IEC-1: The IEC bus is always present on every C64 (single-system too);
  drives attach/detach against it at runtime. (User directive.)
- DD-IEC-2: Enabling/disabling a drive literally starts/stops the drive
  instance (clock register/unregister + bus connect/disconnect). (User.)
- DD-IEC-3: The drive activity LED is the drive's VIA2 `$1C00` PB3 output, set
  by the 1541 DOS ROM (VICE `led_status` model) - NOT IEC bus traffic. (User.)
- DD-IEC-4: CIA2 reads the live IEC bus through a faithful electrical model;
  native idle DATA-low is supplied by the true-drive 1541 holding DATA, so the
  electrical-model switch and true-drive integration land together. C64GS
  (no IEC) keeps disconnected→low. (Spike finding.)
- DD-IEC-5: Reverse-execution = frame-granular snapshot ring + restore +
  deterministic re-run to the target cycle. (User chose frame snapshots.)
- DD-IEC-6: The scope captures edge-driven samples + a sample per step boundary,
  cycle-stamped; on rewind the trace is re-derived deterministically. (User.)
- DD-IEC-7: Lines + IEC protocol decode ship together; dedicated IEC Scope
  panel (reusable for user-port/tape buses later). (User.)

## Requirements (source of truth = MCP)

Functional:
- FR-IECSPY-001 - point-in-time IEC bus snapshot/spy (line levels + pullers + talkers). **DONE.**
- FR-IECMON-001 - IEC bus monitor / scope view: timing diagram (ATN/CLK/DATA/SRQ), who-drives, decoded protocol, step + rewind sync, dedicated panel.
- FR-REVEXEC-001 - reverse execution (backward step by cycle/frame).
- FR-IECLOAD-001 - true-drive 1541 LOAD/SAVE/directory over IEC on single-system C64.
- FR-DRVLED-001 - per-drive activity LED from drive VIA2 PB3.

Technical:
- TR-IECSPY-001 - `IInterSystemBus.Snapshot()` API. **DONE.**
- TR-SNAPFULL-001 - complete machine snapshot/restore capturing all execution-affecting state; deterministic round-trip (restore → re-run N == continuous run N).
- TR-REVEXEC-001 - frame-snapshot ring + restore + re-run-to-cycle; replaces RewindCycle/RewindFrame NotImplemented stubs.
- TR-IECTRACE-001 - edge-driven + step-mark trace recorder, cycle-stamped, bounded ring, deterministic re-derivation on rewind.
- TR-IECDECODE-001 - IEC protocol decoder (ATN frame, LISTEN/TALK + device, OPEN/secondary/channel, data bytes + EOI, turnaround, timeout).
- TR-IECELEC-001 - faithful IEC serial electrical model + native idle parity; C64GS disconnected→low.
- TR-DRVLIFE-001 - drive attach/detach starts/stops the instance on the always-on bus.

Test: one TEST record per slice (TEST-IECSPY-001 done; TEST-SNAPFULL-001, TEST-REVEXEC-001, TEST-IECTRACE-001, TEST-IECDECODE-001, TEST-IECMON-001, TEST-IECELEC-001, TEST-DRVLIFE-001, TEST-IECLOAD-001, TEST-DRVLED-001).

TODO: PLAN-IECDEBUG-001 (program tracker).

## Slices (dependency-ordered, tests-first)

### S1 - IEC spy (DONE)
`IInterSystemBus.Snapshot()` → `BusSnapshot` (line levels, per-line pullers,
talking endpoints). 5/5 tests green. Parity-safe.

### S2 - Snapshot completeness (TR-SNAPFULL-001) - foundational
- Behavior: `GetState()` + restore capture **all** execution-affecting state:
  CPU (incl. pipeline/pending IRQ-NMI latches), VIC-II sequencer, CIA timer
  pipelines + TOD, SID, PLA/processor port, memory, and (when present) the 1541
  drive CPU/VIA/mechanism and IEC bus line state.
- Tests first (red): `Restore_ThenRunN_EqualsContinuousRunN` for C64 (and
  C64+1541 once S7 lands) at several cut points, asserting full `MachineState`
  + memory equality. Red until missing state is captured.
- Green: round-trip determinism tests pass; existing snapshot tests stay green;
  native lockstep green.
- Validation: snapshot/lockstep/checkpoint filters.

### S3 - Reverse execution (FR-REVEXEC-001, TR-REVEXEC-001) (OPEN)
Status 2026-07-08: still open; `EmulatorHostService` rewind RPCs return
`RpcStatus.NotImplemented`.
- Behavior: frame-granular snapshot ring keyed by cycle; `RewindCycle(n)` /
  `RewindFrame(n)` = restore nearest snapshot ≤ target, re-run to target.
  Replaces the NotImplemented stubs in `EmulatorHostService`.
- Tests first: `RewindCycle_RestoresExactState`, `RewindFrame_...`,
  `StepThenRewind_RoundTrips`, ring eviction bound.
- Green + validation: reverse-step tests + host protocol tests; lockstep green.

### S4 - IEC trace recorder (TR-IECTRACE-001) (DONE)
Status 2026-07-08: shipped as `src/ViceSharp.Core/IecBusTraceRecorder.cs` with
`tests/ViceSharp.TestHarness/IecBusTraceRecorderTests.cs`.
- Behavior: `IecBusTraceRecorder` subscribes to `bus.LineChanged`, cycle-stamps
  each edge (from SystemClock) + records a step-boundary sample; bounded ring;
  re-derives deterministically after rewind. Off unless monitor open.
- Tests first: edge capture ordering + stamps; step marks; ring bound;
  rewind re-derivation equals original.

### S5 - IEC protocol decoder (TR-IECDECODE-001) (OPEN)
- Behavior: pure state machine over samples → decoded events (ATN command
  frame, LISTEN/TALK+dev, OPEN/secondary/channel, data bytes + EOI, turnaround,
  timeout/error).
- Tests first: decode canonical captured sequences (LOAD command frame,
  directory read) → expected event list.

### S6 - IEC Scope panel (FR-IECMON-001) (OPEN)
- Behavior: dedicated Avalonia panel; timing diagram lanes (ATN/CLK/DATA/SRQ),
  who-drives coloring (from pullers), decoded-event bands, cursor/zoom/scroll,
  synced to step + rewind buttons; host-protocol "get IEC trace since cycle N".
- Tests first: view-model trace/decoder binding + host trace-delta contract.

### S7 - True-drive 1541 + faithful electrical model (FR-IECLOAD-001 part, TR-IECELEC-001) (DONE)
Status 2026-07-08: shipped as `src/ViceSharp.Host/Runtime/C64TrueDriveRigBuilder.cs`;
Drive 8 defaults to the true-drive path.
- S7a: assemble true-drive 1541 (6502+VIA1/VIA2+DOS ROM) onto an always-on IEC
  bus in single-system C64, **keeping the static CIA2 mask** → parity stays
  green, drive runs on the bus. Reuse multisystem wiring.
- S7b: switch CIA2 PA6/7 from static mask to the live bus (faithful electrical
  model); the running 1541 supplies idle DATA-low. C64GS stays disconnected→low.
- Tests first: drive-on-bus wiring; `$47`/`$07` idle reproduced via live model;
  native CIA2-IO lockstep green.

### S8 - Drive lifecycle (TR-DRVLIFE-001)
Attach/detach starts/stops the instance (dynamic clock register/unregister +
bus connect/disconnect). Tests: attach creates+clocks+connects; detach reverses.

### S9 - Per-drive LED (FR-DRVLED-001) (PARTIAL: device-side only)
Expose VIA2 PB3 off the drive (`LedOn`), per-drive status DTO, bind card LED.
Tests: ROM sets PB3 → LedOn true → DTO per-drive → VM.
Status 2026-07-08: device-side source shipped
(`C1541DriveMechanismDevice.LedOn`); the DTO/VM/card binding is still the
IEC-activity proxy (tracked under PLAN-DRVTRUE-002).

### S10 - End-to-end LOAD (FR-IECLOAD-001) (DONE)
`LOAD"*",8,1` and `LOAD"$",8` complete over IEC against a real D64; watched on
the scope. Integration test + manual app verify.
Status 2026-07-08: shipped; covered by
`tests/ViceSharp.TestHarness/C1541/TrueDriveLoadTests.cs`.

## Risks

- Snapshot completeness is the long pole; any missed state silently breaks
  rewind determinism. Mitigate with the round-trip equality tests at many cut
  points before building reverse-exec on top.
- Faithful electrical idle parity depends on the 1541 DOS-ROM idle behavior
  reproducing native DATA-low; validate against native lockstep at S7b.
- Reverse-exec memory: frame snapshots × ring depth; measure and bound.
