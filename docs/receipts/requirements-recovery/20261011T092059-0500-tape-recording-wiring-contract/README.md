# Remaining tape recording and wiring specification

Prepared locally on PAYTON-LEGION2. The timestamped directory records its creation time, not an acceptance or completion time.

This unapplied R0 supplement addresses the remaining 26 named groups of `FR-RUNTIME-TAPE-002` after the earlier 15-group motor/alarm matrix. It does not amend the requirement store or supersede the retained midnight DISAGREE verdict. No implementation, native source, test, goal or requirement status changed.

## Contents

- [Recording contract](RECORDING-CONTRACT.md): eleven ac-05/ac-06 groups, including exact byte/remainder/24-bit behavior, startup and delayed-stop eligibility, command order, partial writes and public STOP dispatch.
- [Wiring and lifecycle contract](WIRING-LIFECYCLE-CONTRACT.md): fifteen ac-01/ac-02/ac-07/ac-08 groups, including DDR/PCR behavior, pulse routing, device dispatch, event playback and ordered attachment/reset/detachment effects.
- [Typed vectors and source pins](contract-vectors.json): six unchanged coverage bindings, fourteen immutable source pins and 117 source-derived example rows: 27 encoding, 12 write-failure, six edge, sixteen sense, four C64 output, 36 VIC PCR and sixteen VIC PB examples. These are not executed tests. The 36 PCR examples do not replace the required 65536-pair exhaustive mock matrix.
- [Verification receipt](verification.json): document/vector consistency, source-identity and repository whitespace checks only. Read its exact scope; it is not a mocks-green or runtime-parity receipt.

## Inputs retained

- [Fourteen-record base proposal](../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json), SHA256 `320887C5F2F09154F1EC7970B041A638632741A7750E838FE05F77883BB1DA83`.
- [Original eight-AC, 41-group tape coverage](../20261011T083152-0500-media-fixture-specification/coverage-supplement.json). Six bindings are copied without changing their IDs, names, source anchors or unexecuted status.
- [Previous motor/alarm contract](../20261011T085402-0500-tape-motor-matrix/MOTOR-CONTRACT.md), containing the other fifteen groups. No existing receipt was edited.
- [Midnight review](../../hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/verdict.md): DISAGREE, accuracy93/completeness84. This supplement has not received a new review.

## What remains

All real-provider reachability, observation, C-runtime and full-chip timing questions identified in these documents remain open. So do nonneutral signal-processing cases, unknown TAP versions, process-static/snapshot/audio/RTC dependencies and successful loader fixtures. A name-to-contract binding is not evidence that the bound test exists or passes.

The broader R0 packet still needs the dependency contracts, superseded-test disposition, value-bearing pre-application guard evidence, coherent handoff and whole-packet Opus max review. Expanded amendment application still requires explicit operator approval after HV. Mocks-first and every real-emulator/full-suite gate remain mandatory. Native VICE remains the immutable target, including quirks and failure behavior.
