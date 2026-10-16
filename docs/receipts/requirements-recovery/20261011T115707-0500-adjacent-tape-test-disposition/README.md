# Adjacent tape-test disposition supplement

Prepared 2026-10-11. Draft only: no requirement amendment, code change, test execution or HV acceptance.

Correction recorded 2026-10-11T12:10:06-05:00: this supplement originally misnamed VIC-20 tape flux as CB1 in item 8 and the ADJ-VIA gap. Pinned `vic20datasette.c:37-48` uses VIA2 CA1. Both references are corrected. The original three receipt files are retained byte-identically under [preserved-adjacent-receipt](../20261011T121006-0500-r0-review-packet/preserved-adjacent-receipt/). The prior verification describes the pre-correction bytes; current hashes and correction checks belong to the review packet.

## Scope and receipts

[adjacent-disposition.json](adjacent-disposition.json) supplements the [63-method direct inventory](../20261011T095710-0500-tape-test-disposition/README.md). It retains 47 complete selected method bodies across ten additional files:

- Eight active adjacent files: 32 methods. One method has seven literal InlineData rows.
- Two retained legacy Xbox files: 15 methods. Xbox implementation remains out of scope; its requirements and tests are not retired.
- Nine further files: bounded lexical classification of audio, oscillator, filter or controller "tap" matches. These are not Datasette coverage. This is not a full correctness review of those files.

All 19 additional file hashes were rechecked against the earlier search disposition. Source-body spans, whole-file hashes, literal theory rows and both xUnit and FluentAssertions assertion lines are retained. Method counts are not executed-case counts. No file is excluded from an applicable full regression gate.

The group records separate existing assertions, gaps, proposed corrections and 27 negative-control recipes. These recipes are not implemented or executed cases. The native anchors are independently checked against Git blobs at immutable VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. Reading those source branches is not proof of native runtime behavior.

## Findings and required disposition

1. **UI state is not tape timing.** Preserve all four selected AttachPanel methods: ordered slots/default state, drive IEC telemetry, per-slot true-drive capability and drive-only LEDs, including existing notifications. Those checks do not exercise tape sense, motor, recording or flux. A drive-only LED does not forbid a separately specified tape transport indicator.

2. **T64 and TAP must stay distinct.** The extension map routes TAP to Tape/Tape and T64 to Drive8/Tape. Keep both kinds and slots, PRG non-launchability, unknown-file rejection and the bare-extension case. The production map also supports d71/rom and case-insensitive lookup; the selected theory does not cover those branches. Extension routing does not prove valid bytes, attachment or boot.

3. **Direct CIA FLAG tests are not complete edge/timing tests.** Retain all five stimuli: latch, read-clear, enabled IRQ, disabled IRQ and repeated trigger. The enabled case calls one Tick; the disabled case does not. Immutable `ciacore.c:1594-1625` sets flags through catchup and schedules asynchronous processing. Exact assertion timing still depends on the remaining IFR pipeline and CIA model. Add time-indexed trigger/read/mask-order cases and separately test actual Datasette wiring before declaring timing parity.

4. **gRPC list checks omit Tape metadata.** The list method asserts Ok, three entries and ordered slot enums, but not Tape path, display name, IsAttached, IsReadOnly or AppliedToRuntime. The attach method proves Drive9 slot/payload, not every supplied field or token. The fake records only LastAttachRequest. Future contract tests need field-by-field mutations and a call/token ledger, while preserving the existing order and exact-payload checks.

5. **T64 round-trip evidence is managed-only.** The two tests use one synthetic 104-byte archive. First extraction checks load prefix, length and endpoint data bytes, not every interior byte. The D64 test does compare the complete returned file stream with the extracted PRG, but both producer and consumer are managed. Preserve both tests and fixture bytes. Add independent full-payload expectations and the immutable-native observations already specified in the [media fixture supplement](../20261011T083152-0500-media-fixture-specification/README.md). Do not derive expected extraction bytes from T64Image itself.

6. **Keep all thirteen host negative/guard tests.** They cover missing sessions, absent input, missing file, bad disk bytes, unsupported slot, empty Tape detach, empty listing, cancellation and null registry. None is a successful real TAP/T64 attach. Cancellation exceptions alone do not prove that filesystem, registry and runtime calls never occurred. A recording seam must reject "perform forbidden work, then throw" while preserving current RPC status/null/message expectations.

7. **The shell no-op check needs interaction evidence.** Drive8 toggles and Tape stays false, but the selected method does not prove absence of a Tape host mutation. Add call-ledger controls when implementing the approved contract. Do not count the NSubstitute success defaults as runtime media evidence.

8. **VIC-20 idle PA is only a baseline.** The existing method checks 7E through Read/Peek at no-handshake PA 911F and bus Peek. It does not cover sense transitions, DDRA, joystick/IEC combinations, handshake effects, VIA2 CA1 flux, motor or recording. Immutable `vic20via1.c:222-252` combines inverse sense bit 6 with joystick/IEC and PRA/DDRA masking. Use the existing [wiring contract](../20261011T092059-0500-tape-recording-wiring-contract/README.md) for those vectors. The real MachineTestFactory/ROM dependency remains mandatory.

9. **Preserve legacy without overstating it.** Xbox device tests cover Tape attach/eject via a fake that reports AppliedToRuntime=true without decoding bytes or running a machine. The default-cartridge policy comment says disk/tape, but its user-selection body exercises Drive8, not Tape. The boot/facade wiring guard searches source tokens; it does not run the wiring. Retain all fifteen tests. No Xbox implementation or retirement is authorized here.

## Requirement boundaries

Fourteen related current effective MCP records are retained verbatim as data. These associations do not establish full AC mappings or satisfied criteria. TEST-CIA-001 and TEST-UIMENUBAR-001 have empty AC arrays in this readback. Several FR-ROMM-LAUNCH-001 AC texts contain partial phrases; those exact records are retained for provenance reconciliation. This observation does not authorize a new requirement edit or enlarge the existing [14-record tape delta](../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json).

The prior direct inventory remains intact. This supplement advances its explicitly outstanding adjacent-body audit; it does not certify the full transitive runtime graph, callback seams, fixture migration or complete N-15 closure.

## Gates still open

The midnight review remains DISAGREE: accuracy 93, completeness 84, six failed claims. This document does not supersede it.

Complete the whole R0 finding-closure packet, including observer feasibility and non-interference, hidden-state determinism, loader corpus and API write guards. Obtain local Opus-5.5 max HV on that complete packet and explicit approval of the exact amendment before applying it. Then prove every required assertion against correct and deliberately incorrect mocks inside a green harness before production work. Bind the same cases to managed and immutable-native providers; retain native VSF, full current-plus-prior regression, and all real-emulator/lockstep gates.

[verification.json](verification.json) contains document/source-integrity checks only. This slice runs zero mock, managed or native tests and launches zero reviewers. No requirement, done-state, test/product/native source, commit or remote is changed.

