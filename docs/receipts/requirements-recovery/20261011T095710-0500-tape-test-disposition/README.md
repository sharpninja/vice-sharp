# Existing tape-test disposition: draft, not implementation

Prepared from local source and effective MCP requirements on 2026-10-11. This addresses midnight HV finding N-15 without applying the unapproved requirement amendment, editing production/tests, or claiming native parity.

## What this adds

[disposition-ledger.json](disposition-ledger.json) records 63 test methods across 13 files: all 28 methods in the Tape directory, eight CPU-port methods, 17 tape replay methods, and ten host/profile/storage/memory/architecture methods. Counts are method counts, not executed cases; theory rows are not expanded here.

Each row retains its exact source span and SHA-256, declaration line, original observations, assertions to preserve, proposed correction, discriminating negative controls, requirement/AC references, native-source anchors, and explicit unapproved/not-run state. Source spans end before the next method declaration and can include intervening comments or attributes. Whole-file pins remove any ambiguity about their context.

The direct inventory uses all 14 draft requirement IDs, legacy tape aliases, and concrete Datasette/TAP symbols. [search-disposition.json](search-disposition.json) adds a broader word search: 31 matching files, 19 beyond the direct inventory, 32 files in the union. Those 19 contain eight adjacent regressions, nine lexical false positives such as audio taps, and two retained legacy Xbox surfaces. Their matching lines and hashes are retained. They have not received the same method/body audit. No file is removed from an applicable full regression gate.

## Decisions that prevent renewed divergence

- Correct invalid one- and two-byte TAP fixtures. Native `tap_open` rejects fewer than three actual payload bytes. A negative result on an invalid fixture cannot prove a motor or recording predicate. Declared length mismatches need independent fixtures with at least three actual bytes.
- Keep file-decoder pulse units separate from live gap clocks. The existing v0 overflow test incorrectly expects a 24-bit extension. Preserve its exact input as a regression: block decoding yields 256, 64, 31, 256 pulse units; the neutral-resource live path yields configured zero-gap delay, 512, 248, configured zero-gap delay. The header default is 2500, but the test must pin the active resource.
- Replace the pre-Tick immediate-read assumption with native alarm scheduling in runtime tests. Preserve pure reader helper coverage separately. Native START emits flux at its callback before fetching/scheduling the following gap; a managed countdown alone is not the oracle.
- Split pending-stop cancellation from restarting after the stop callback. Turning the requested motor line back on before the delayed stop executes retains the native running flag and clears the stop deadline; it does not universally restart a fresh ramp.
- Split rejected no-image/read-only RECORD from accepted writable-media RECORD. Preserve event recording even when a native public command is ignored. Positive recording must check qualifying write edges, bytes, clocks, file positions and failure effects, not just a managed list count.
- Retain instantaneous host cursor helpers without calling them native REWIND. Add the distinct timed native transport and its replay, including direction changes and command alarms.
- Correct names and meanings, not merely expected numbers. The test named bit-3 motor writes bit 5. The bit-5 low-to-low test is not charge-decay evidence. The test named bits 6 and 7 covers only bit 6 at a fixed deadline; native behavior uses randomized deadlines and strictly-later read-side discharge.
- Keep all 17 tape replay regressions. Three assume accepted RECORD with no media; several assume flux before native startup. Retain input counts/order/timestamps, media bytes, clock, CPU, RAM, baseline and periodic-seek assertions. Bind corrected values to independent native observations, not a second instance of the same managed implementation.
- Native VSF remains the actual snapshot format. Require native DATASETTE state and media-module round trips, not a substitute managed-only format. Native serialization does not automatically preserve every process-static variable; the prior hidden-state contract still applies.
- Preserve host RPC semantics: the inspected SX-64 no-device test expects RPC `Ok` with `AppliedToRuntime=false` and its error text, despite an inaccurate comment suggesting a separate status code. Do not introduce an unapproved API change while correcting tape behavior.
- Retain real builder integration. Missing ROMs cannot be hidden by its current `SkipWhen` paths at mandatory gates. Device registration is structural evidence, not runtime timing proof.

## Requirement and source authority

The live `workflow.requirements.effective` read returned layer-1 with 235 FR, 139 TR, 142 TEST and 233 mappings. The complete current FR-SNP-003 record is retained in the ledger: all eight criteria remain pending. Its exact-replay obligation does not need to be weakened to repair these tests.

Other row references point explicitly to the **after-values of the unapplied** [14-record proposal](../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json), not to already-approved live changes. Retained AC IDs do not make changed meanings interchangeable. This supplement neither expands the requirement write set nor approves it.

Six native files are pinned by SHA-256 and Git blob to commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. They are read from the immutable materialization outside `native/vice`. The dirty native checkout is retained untouched and is not accepted as a clean parity oracle.

Use the existing [motor matrix](../20261011T085402-0500-tape-motor-matrix/README.md), [recording and wiring contracts](../20261011T092059-0500-tape-recording-wiring-contract/README.md), and [media fixture specification](../20261011T083152-0500-media-fixture-specification/README.md) for their detailed state vectors. These are specifications, not executed tests.

## Required next gates

1. Finish remaining dependency, observer, hidden-state, loader, guard and handoff remediation; incorporate this disposition into the exact approval packet. The broad search's adjacent regression bodies and transitive test dependencies still need review before calling the migration exhaustive.
2. Obtain local Opus-max HV on the complete corrected packet. The midnight DISAGREE (93 accuracy, 84 completeness) has not been superseded by this draft. No new review was launched here.
3. Obtain explicit operator approval of the exact requirement delta. Then use supported MCP writes with preservation and readback guards. Do not infer approval from this documentation.
4. Write the mapped contract tests with independent mocks. Prove correct responses and expected rejection of every wrong response inside a green harness before production work. No deliberately red production run substitutes for the mock gate.
5. Bind the same scenarios/assertions to managed and immutable-native providers. Retain native VSF, every real-emulator gate, and the full applicable current-plus-prior suite with zero failures, skips or aborts. Named filters remain diagnostic only.

[verification.json](verification.json) records document consistency and source-integrity checks only. This slice runs **zero mock, managed or native tests** and changes no product/test code, requirements, done-state, native source, commits or remotes. N-15 is not claimed closed or accepted.
