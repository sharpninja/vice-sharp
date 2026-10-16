# Tape startup/stop matrix: unexecuted R0 draft

Prepared locally by Codex on 2026-10-11, continuing the full ViceSharp plan. No requirement, test, code, native source or acceptance gate was changed by this supplement.

- [MOTOR-CONTRACT.md](MOTOR-CONTRACT.md) explains the source-derived states, operation ordering, numerical boundaries and limitations.
- [motor-matrix.json](motor-matrix.json) contains 64 explicit motor/alarm state vectors covering all 15 existing ac-03/ac-04 groups. Declared machine/media axes expand to 366 state-projection cases, not 366 executed or passing tests.
- [verification.json](verification.json) records source and artifact hashes, exact serialization readback, structural checks and the untouched approval boundary.
- [The preceding media specification](../20261011T083152-0500-media-fixture-specification/README.md) retains all eight tape criteria and all 41 named tape groups. This supplement does not replace it or its base fourteen-record proposal.

The added straddling-lateness vector distinguishes actual callback time from the alarm's scheduled time. Other rows distinguish START from an already-running motor-on, retain existing alarms, cancel stops before/at/after the deadline, preserve write state, retain repeated-off deadlines and compare both same-clock dispatch orders. An early active read explicitly replaces the stop alarm with its next read alarm; an early no-media STOP callback retains its duplicate alarm-set call.

Native public-operation preludes, snapshot/observer feasibility, full file/container fixtures and attached-media fast-wind callback traces remain open. The file does not authorize private-state injection or edits to native VICE. A source-derived expectation is not a native execution result.

The remaining 26 groups for the other six tape criteria are still required: transport/sense, machine routing, recording eligibility and bytes, cursor/buffer boundaries, wiring and lifecycle. Guard schema verification, cross-packet dependency approval and canonical handoff/TODO reconciliation also remain unfinished. All existing tests and real-emulator gates are retained.

Next: finish those concrete matrices and combine the corrected R0 review packet. Then obtain whole-packet Opus max HV and explicit approval of the expanded exact delta before implementation. The last independent verdict remains DISAGREE (93 accuracy, 84 completeness); these new documents have not been re-reviewed.
