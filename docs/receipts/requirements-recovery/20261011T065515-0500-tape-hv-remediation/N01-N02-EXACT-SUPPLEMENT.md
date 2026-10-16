# Tape amendment: exact N-01/N-02 correction supplement

Prepared locally by Codex on PAYTON-LEGION2. Evidence measured through 2026-10-11T06:55:15-05:00.

Status: draft, unapplied, not independently accepted. This is an additive supplement to the retained eleven-record proposal, not permission to apply either artifact. No requirement, acceptance criterion, mapping, implementation or native source was changed in MCP or production. The full original parity plan remains the objective.

## Inputs and authority

- Original proposal: `../20261009T154620-0500-tape-amendment-proposal/proposed-field-delta.json`, SHA256 `1D8A442588ADD7883693694352954DF2077D166FB47ED4E2924AFE738A481507`.
- Review: `../../hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/verdict.json`, SHA256 `E87F607E6F533823A59E7FF7EAFDB7A8A88B3FE0548D82163797383B3EB9FC0B`.
- Live effective requirements were retrieved through `workflow.requirements.effective` using the Codex plugin: layer `layer-1`, 235 functional, 139 technical, 142 testing records, 233 mapping rows. This is a current effective projection, not a complete hidden-metadata preimage.
- Parent MCP session: `Codex-20261007T135626Z-plugin-session`; current request: `req-20261011T114744Z-tape-proposal-remediation`.
- Native target: immutable commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. All source references below are relative to its `vice/` directory. Inspection used the separate materialization at `F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice`.

The prior review remains DISAGREE, accuracy 93 and completeness 84, with 35 PASS, 6 FAIL, 0 UNKNOWN and 1 N/A. This supplement does not close that verdict or claim that the other findings are repaired.

## N-01: do not submit nonexistent mapping targets

All 40 target references in the original seven proposed mapping rows were checked against the live effective records. Exactly two targets were absent: `TR-MEDIA-D64-001` and `TEST-D64IO-001`, both in the proposed `FR-BACKFILL-MEDIA-001` mapping. The other 38 references resolved to existing records.

The current live mapping and original proposal's before-value agree:

- `trIds`: `TR-DRV-EDGE-001`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001`.
- `testIds`: `TEST-DRV-MOTOR-001`, `TEST-TAPE-RAMP-001`, `TEST-TAPE-SENSE-001`.

Replace only this mapping row's proposed after-value:

- Old proposed `trIds`: `TR-DRV-EDGE-001`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001`, `TR-IEC-EDGE-001`, `TR-MEDIA-D64-001`.
- New proposed `trIds`: `TR-DRV-EDGE-001`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001`, `TR-IEC-EDGE-001`.
- Old proposed `testIds`: `TEST-DRV-MOTOR-001`, `TEST-TAPE-RAMP-001`, `TEST-TAPE-SENSE-001`, `TEST-IEC-TIMING-001`, `TEST-D64IO-001`.
- New proposed `testIds`: `TEST-DRV-MOTOR-001`, `TEST-TAPE-RAMP-001`, `TEST-TAPE-SENSE-001`, `TEST-IEC-TIMING-001`.

Keep `frId`, `workspaceId`, every before-value, and all six other mapping rows unchanged. This changes the proposed addition count from 15 to 13, with zero removals from live mappings. After composition, 38 target references must resolve; retain a separate existence result for every reference, not only a total count.

The two omitted additions remain required work in the separate, unapproved media proposal. Do not delete or retire their proposed records, waive D64 coverage, or claim that holding these edges outside this tape amendment resolves the media backlog. Applying their creation records would be a different approval scope. If either target appears before the final proposal is reviewed, report that drift and rebuild the exact delta rather than silently applying this draft.

## N-02: expand the correction to two existing FR records

The original eleven-record proposal leaves two live records contradictory. Add updates for existing `FR-CPU-004` and `FR-PRF-003`, making thirteen candidate record updates when composed. Do not create replacement IDs. Preserve all their AC IDs, other AC text, titles, priorities, scope, workspace, pending status and unsatisfied flags. Preserve existing mappings.

For each of the four pairs below, change the structured AC text and the identical literal sentence in the record's embedded `## Acceptance Criteria` body. Require exactly one matching structured AC ID and one matching body occurrence. If either old value differs, stop and report drift. Do not regenerate unrelated prose, reorder criteria or clear hidden fields.

### FR-CPU-004 / ac-03

Before:

> Bit 3 controls the cassette motor (active low).

After:

> On C64, the cassette motor output is active low from DDR-masked bit 5, !(DDR & DATA & 0x20), and the cassette write output is bit 3, (~DDR | DATA) & 0x08. Match pinned VICE's changed-effective-output detection for data and DDR writes, including input/output transitions and repeated writes. Do not exchange the motor and write bits.

Source: `src/c64/c64pla.c:77-89`. The static previous-output values at lines 40-46 are part of the fixture state, not automatically reset by `c64pla_pport_reset`.

### FR-CPU-004 / ac-05

Before:

> Bit 5 is unused on C64 but reads as expected based on DDR/pull-up behavior.

After:

> On the C64 board, reading $0001 with DDR bit 5 clear returns bit 5 clear; with DDR bit 5 set, its output readback follows pinned VICE's port data/DDR calculation. Bit 5 is the motor-control bit, not an unused pin. Exercise both data levels, both DDR directions, and direction changes; do not substitute SX-64 floating-pin behavior for C64 behavior.

Source: `src/c64/c64pla.c:53-63`; the additional floating-capacitor read overlay for bits 3-5 in `c64mem.c` and `c64memsc.c` is conditional on `BOARD_SX64`.

### FR-PRF-003 / ac-02

Before:

> No cassette port: tape-related I/O port bits behave differently (cassette sense always reads "no tape").

After:

> The SX-64 board has no connected cassette port. Match pinned VICE's board-specific PLA inputs and suppress forwarding of motor, write and sense-out changes to tapeport devices. CPU-visible reads of bits 3-5 must still follow the native data, DDR and retained floating-capacitor state; neither permanently-high nor permanently-low input values are a valid universal substitute.

Source: `src/c64/c64mem.c:218-222`, `src/c64/c64pla.c:77-95`, and the read overlays cited below. This specifies observable native behavior, not an independent claim about physical hardware.

### FR-PRF-003 / ac-04

Before:

> The 6510 I/O port bit 4 (cassette sense) is permanently pulled high.

After:

> For SX-64 CPU reads of $0001, each input-configured bit 3, 4 and 5 is replaced by its retained native data_set_bit value after native falloff processing. Reset clears those retained values. Output writes and output-to-input transitions seed the corresponding retained data bit, falloff flag and deadline as pinned VICE does. A flagged retained value clears on a read only when its stored deadline is strictly less than maincpu_clk: a charged input therefore remains charged at the exact deadline and clears on the first later read. Output-configured bits retain normal port readback. Match each bit independently, native RNG-dependent deadline selection, repeated writes, direction transitions, reset and read side effects; do not impose an always-high or always-zero sense bit.

Sources:

- `src/c64/c64mem.c:277-324` and `src/c64/c64memsc.c:279-326`: falloff and input overlay.
- `src/c64/c64mem.c:396-417` and `src/c64/c64memsc.c:395-416`: output-to-input capture.
- Both memory implementations, lines 473-488: output-write capture, including the path before the later data-value-change check.
- `src/c64/c64pla.c:106-123`: reset state.
- `src/c64/c64.h:81` and both memory implementations' `FALLOFF_RANDOM_SX`: base delay 1500000 cycles plus the native random draw. This draft does not assert an unverified endpoint policy for `lib_unsigned_rand`.

## Why the reviewer wording needs qualification

N-02 correctly rejects the live always-high SX-64 criteria. Its detail also says that SX-64 reads input bits 3-5 as zero. That is true for reset/uncharged state, not every CPU-visible read. `c64pla_config_changed` clears the base input read bits, but `zero_read` overlays their retained capacitor values afterwards.

For a source-derived boundary fixture with a charged input bit and stored deadline D, the branches require: read at D-1 preserves the charge; read at D preserves it; read at D+1 clears both retained value and falloff flag. These are source-derived expectations for the later mock contract, not a claim that a native or managed runtime test ran. Both `c64mem.c` and `c64memsc.c` were inspected so the correction is not based only on the non-cycle-exact C64 path.

Do not edit the reviewer's retained verdict. Supply this qualification as new evidence in the next complete review.

## Required AC-to-test additions for the later mocks-first gate

These named groups are draft coverage obligations, not existing or passing tests:

- `C64_PortMotorAndWrite_DdrTruthTable`: the four DDR/data combinations for each output, changed/repeated effective values and direction transitions. Negative controls exchange bits 3/5, omit a DDR-induced edge, invert active-low motor, or add duplicate edges.
- `C64_PortBit5_ReadDirectionMatrix`: both data levels with input/output direction. Negative controls return pulled-high input, apply SX-64 retained charge to C64, or ignore the output value.
- `Sx64_PortOutputs_AreDisconnected`: motor, write and sense-out changes update appropriate internal previous-output state without tapeport callbacks. Negative controls forward any disconnected signal or suppress unrelated port behavior.
- `Sx64_InputCapacitors_ResetAndIndependentBits`: bits 3, 4, 5 independently, reset and uncharged input, mixed DDR masks, charged high/low captures and repeated writes. Negative controls force all inputs high/low or leak one bit's state into another.
- `Sx64_InputCapacitors_StrictFalloffBoundary`: D-1, D and D+1, with retained-value and flag side effects, output-mode reads and missing falloff flag. Negative controls use >= instead of >, clear without a flag, skip a required clear, or incorrectly overlay output bits.
- `Sx64_InputCapacitors_DeadlineSelection`: preserve native RNG input and selected deadline; distinguish output-write capture from output-to-input capture and repeat writes. A seed, random draw, stored deadline or observable equivalent must be pinned for the real provider. Missing observability blocks the real case; mocks cannot supply purported native observations.

First bind all relevant assertions to independent correct and wrong mocks and prove the complete applicable mock suite green. Only then bind the same cases and assertions to real managed and immutable-native providers under the original approved phase gates. The remaining public tape API, loader, native VSF, xvic half-cycle, C64 and settings obligations are unchanged.

## R0/R1 safeguards retained and strengthened

- Recheck every proposed mapping target, including targets unchanged in the proposed row, against the fresh effective layer before application. Distinguish an absent target from an unavailable query. Neither permits application.
- Capture complete current records and fresh effective records before applying any field delta. Compare stable metadata explicitly: area, subarea, testType, notes, priority, layer scope and workspace, where present. Preserve field presence separately from null values.
- Record `createdAt`/`updatedAt` as observed transport data, but do not use read-synthesized timestamps as preservation proof. Historical missing metadata remains missing. Do not invent old values or discard stable fields merely because timestamps vary.
- Every preimage check must retain actual before/current values or separate per-field hashes plus presence information. A pair of missing properties comparing null-equal is not a passing preservation check.
- After an approved write, read back exact changed fields and stable metadata, then reconcile projections. A timeout does not authorize blind replay or an assumed rollback. No application is authorized by this supplement.

The current `getFr` attempt failed before any complete record was captured. Triage then returned `method_invocation_error`, `QBrainAi.Client.QBrainAiException`, with message "The storage backend is currently unreachable. Retry the operation once connectivity is restored." The supported full-content failsafe is queued at `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261011T115419Z-goal_tape_getfr_triage_failure-eace.yaml`, SHA256 `86CFD2FF272DA233FE5BCC06435BD1F402D19690D5D05052794CA7AF24D3CA26`. It is not accepted triage or server persistence. Older queues remain drain-disabled and untouched.

## Source identity checks

Each materialized file's `git hash-object` equals `git -C native/vice rev-parse cc67418cba58e71d8e70fd353e9de47199ad2075:vice/<path>`:

- `src/c64/c64pla.c`: Git blob `f3bb97c2af94e5fb330c7469648d465a55dbc6b7`; SHA256 `21574C654D7FEAAF9B860CADA61C74BEE1B3D0EBA1F2CDB3FF814D9F940E3737`.
- `src/c64/c64mem.c`: Git blob `31f4f3e4e5ab66c2c9834e4c2ff9cd35231a69b0`; SHA256 `4EE45952CAC4A572D57F3BBEA7EA0D52F5D303499167228B6EEA24D7EC5EBF95`.
- `src/c64/c64memsc.c`: Git blob `d08f9904d6874753c7d829bc00fc0482d7a54748`; SHA256 `85A08EA194E27735DA0C7A8282772B97E6EFB6E109F3B683C780EE0F2A37F2DB`.
- `src/c64/c64.h`: Git blob `3733dcec1dc2392aa72ab0eedde49e40b2f990a0`; SHA256 `23B26B78EFAC77C43FDF2FE20C399464DC0A9A7C0D103804D7FF65CEA6BF816F`.

These checks prove identity of four selected source files, not whole-tree cleanliness, a successful build, emulator parity or a passed test gate.

## Remaining work before a complete approval packet

Compose these exact changes into a new separately retained machine-readable proposal; preserve the original packet. Reconcile N-05/N-06 motor/start cases, N-07 through N-10 hidden/lifecycle/media state, N-11 dependency wording, N-12 FR-TAP-005 audit, N-13 denominator, N-14 rewritten-obligation disclosure, N-15 test-disposition ledger, and N-18 tracking. N-03/N-04 require executable safeguard evidence and complete metadata readback, not this policy alone. Preserve N-16/N-17 process disclosures and all prior unresolved findings.

The complete corrected packet requires local Opus 5.5 max review, AGREE with both scores at least 98 and zero FAIL/UNKNOWN, then explicit operator approval of the expanded exact delta. No acceptance, production implementation, phase advance, goal completion, commit or push is claimed here.
