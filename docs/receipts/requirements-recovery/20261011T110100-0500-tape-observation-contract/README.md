# Tape observation and hidden-state contract

Prepared 2026-10-11T11:05:01-05:00. R0 planning evidence only. This supplements the unapproved tape amendment and its motor, recording and wiring contracts. It does not change requirements, implement an observer, run tests, close an HV finding or advance a gate.

## What this changes in the remediation plan

The current external clock probe cannot supply the required tape event trace. A native snapshot is also an operation with side effects, not automatically a neutral state getter. Before real tape assertions can bind to an immutable oracle, the observer must prove both coverage and noninterference for the particular fixture.

Native VICE remains the immutable behavior target at `cc67418cba58e71d8e70fd353e9de47199ad2075`. Native VICE VSF remains the actual, mandatory snapshot format. The supplemental event trace described here is test evidence, not a replacement snapshot format. No private-state writes, instruction patches, native source edits, new native exports or relaxed parity tolerances are authorized by this document.

[observation-contract.json](observation-contract.json) retains 14 source files verified against upstream Git blobs, 14 local input hashes, three binary/tool hashes, actual object-inspection output, the bounded fixture searches, four current effective FR readbacks, ten capability assessments and ten proposed observer-conformance fixture groups with 44 negative controls. These are specifications, not 44 executed or passing tests. [verification.json](verification.json) records the document and source checks actually performed.

## Evidence and consequences

### 1. Clock records do not expose tape behavior

`tools/immutable-vice/clock-observer.c:14-18` invokes the real `vic_cycle` and then publishes `maincpu_clk`. The retained diagnostic `gate-runtime/gate-observer.c:12-38` publishes only `VICE_GATE,<clock>` and `VICE_GATE_STOP,<status>`. Its permission gate does not expose tape motor, mode, alarms, recording bytes, IRQ events, sound or RTC state. Existing clock/stdio mocks cannot pass tape-specific ACs.

`IViceNative` offers existing VSF load/save, memory and chip surfaces, but no explicit tape observation contract. Existing P/Invoke/shim declarations are not proof of an accepted immutable-native tape oracle or of a particular binary's provenance.

### 2. A proposed alarm wrapper would miss inline scheduling

`vice/src/alarm.h:131-185` defines dispatch and `alarm_set` inline static. The retained Windows `datasette.o` has external symbols and relocations for `alarm_new`, `alarm_unset`, tapeport/UI callbacks, sound enqueue and stdio operations, but none for `alarm_set`. Both inspection commands exited 0. Source identity and this object's hash are pinned separately; this is not a rebuilt-object or final-link proof.

A candidate external observer may forward `alarm_new` unchanged and retain its returned pointer for read-only inspection. `datasette.c:863-864` uses the same name for both ports, so identity must include context and original port data. `alarm.h:38-99` exposes pointer lifetime and queue/index fields. The observer must not replace callback/data, modify the queue or infer events from final state.

This candidate remains unimplemented and unvalidated. In particular, samples can miss a set/change/unset sequence occurring between sample points. Complete transient scheduling coverage remains a named blocker until a permissible observer proves it. Wrapping only `alarm_unset` is not sufficient.

### 3. VSF fields are useful but not the whole process state

The `DATASETTE` 1.5 writer (`datasette.c:1381-1399`) stores 18 fields, including running motor, no-image mode, last write clock, stop clock, alarm pending/clock, long-gap state, counter offset, resources and full-wave state. The `TAPE` 1.0 writer (`tape-snapshot.c:228-251`) stores 13 fields including image type/read-only, TAP size/version/system, logical cursor, counters, mode and changed flag. The exact field lists are in the contract JSON.

Those writers do not serialize `last_write_bit`, TAP buffer contents/indices, the physical FILE cursor and stream state, wobble phase/remainder, azimuth remainder or datasette sound ring/phase. An absent module is not a zero-valued module. Multiple module occurrences must retain their native ordering and port context. Independently validate decoding against native-authored bytes; do not silently impose a different emulator load policy when checking observation fixtures.

Use separate fresh-process, same-process reset, detach/reattach and VSF-reload fixtures. Replay their entire native prelude, including attach scans, resource changes and dependency calls. A matching snapshot file does not establish matching unrepresented history. Neutral timing fixtures remain separate from mandatory non-neutral wobble, azimuth and audio fixtures.

### 4. Snapshot observation can change the tested operation sequence

`tape-snapshot.c:69-129` embeds TAP data by obtaining the FILE position, seeking to the end and beginning, reading the contents, and seeking back. Restoring the offset does not remove those intervening stream operations. A snapshot between playback read and recording write therefore cannot be assumed neutral to the recording-stream contract.

The existing C64 shim (`native/vice-shim.c:792-812`) calls `machine_write_snapshot(path,0,0,0)`; the VIC-20 shim (`native/vice-shim-vic20.c:566-585`) calls it with `(path,1,1,0)`. Both upstream snapshot writers pass `save_disks` into `tapeport_snapshot_write_module`. Those two shim call profiles are not interchangeable for media observation.

Furthermore, both native snapshot writers call `sound_snapshot_prepare`; C64 additionally runs `drive_cpu_execute_all(maincpu_clk)`. `sound.c:1830-1838` shows sound preparation invokes `sound_run_sound`. Disabling image embedding does not establish a side-effect-free full-machine save.

For recording fixtures, preserve a pristine run without snapshot calls at sensitive points. Obtain supplemental checkpoints in independent executions of the exact prelude, and prove reproducibility before comparing them. A pass-through stdio observer must preserve arguments, call count/order, returns and native error state; it must not insert `fseek`, `fflush` or reads to make observation convenient. Final-link interception and observer/no-observer equivalence are still mandatory proofs.

### 5. Audio and RTC require dependency-aware observations

`datasette-sound.c:36-123` retains a 200-entry ring, indices, start clock, half-wave mode, square sign and split state. Flush disables the chip and sets end equal to start; it does not reset every static. The float/int sample paths at `125-225` are distinct. Capturing gap enqueue calls does not prove PCM output. Preserve sample budgets/backend, ring wrap, partial consumption, disable/re-enable and snapshot synchronization.

`pcf8583.c:136-161,213-250` loads persistent context and reads host centiseconds independently of the seconds halt latch. `rtc.c:58-61,192-195` obtains centiseconds from the platform and seconds from host time plus offset. Pin persistent context and timezone and capture the actual dependency call sequence. Do not change the host clock to force a test result.

There is an additional resume edge: `pcf8583.c:181-198` holds the shifted serial `register_train` in process-static storage, and `478-485` reads it during `READ_REGS_TRAIN`. The inspected snapshot writer/reader (`533-675`) neither serializes that array nor rebuilds it. `cp-clockf83.c:95` initializes the tape RTC with shift 2. Test native fresh-process and same-process mid-train restore before a new I2C start, then after a new start. Do not assume serial read registers in VSF imply restoration of the separate train. This is an immutable-native behavior observation, not authorization to repair VICE.

### 6. Named-loader success is still missing evidence

The retained bounded filename search found 15 `.tap/.t64/.prg/.p00/.d64` paths and no `.tap` or `.t64` paths. The named-loader search over `src` and `tests` C# found no Novaload, Biturbo, Turbotape, Cyberload or Pavloda references (`rg` exit 1, no matches). Exact commands, exclusions and results are retained in the JSON. Archives, excluded build/native/output trees, generated temporary fixtures and external corpora were not inspected; this is not a claim that no such media exists anywhere.

Fresh effective Layer 1 readback leaves all six `FR-TAP-005` ACs pending/false. Retain every named successful-loader obligation and the custom encoding, precise interrupt timing, half/full-wave and changing-speed cases. For each real workload, the next corpus step must retain bytes/hash, provenance, machine/resources, exact launch prelude, native success signal and resulting payload/entry state. Two matching failures never prove successful loading.

## Sequence from here

1. Integrate this evidence with the [motor matrix](../20261011T085402-0500-tape-motor-matrix/MOTOR-CONTRACT.md), [recording/wiring contract](../20261011T092059-0500-tape-recording-wiring-contract/RECORDING-CONTRACT.md), [test-disposition ledger](../20261011T095710-0500-tape-test-disposition/README.md), and [API guard contract](../20261011T102600-0500-requirements-api-guard/README.md). Preserve the broader media, CPU, snapshot, xvic and settings obligations of the original plan.
2. Finish fixture provenance/discovery and whole-packet readiness, and reconcile root handoff and MCP TODO tracking. This supplement alone does not close C3, C4 or D4.
3. Obtain required whole-packet Opus max HV and explicit operator approval of the expanded requirement delta before applying it. No acceptance is inferred from source inspection or documentation checks.
4. At the authorized test stage, prove all required assertions against discriminating mocks with expected negative controls inside green suites. Then bind the same cases to validated external native observations and managed production behavior. Do not begin production changes with an unvalidated/red mock gate.
5. Run every applicable real-emulator, native VSF, cumulative suite and full-plan HV gate. Missing native observation or missing workload remains explicit incomplete work, not a skip, exclusion, guessed value or weakened assertion.

## Scope retained

No change to the original plan, product, tests, native source, requirements or mappings. No deployment, commit, push or goal/TODO done-state transition. Phase 3 xvic and Phase 4 settings remain behind the original Phase 2 exit and explicit start approval. The existing midnight verdict remains DISAGREE 93/84; this packet has not received a new review.
