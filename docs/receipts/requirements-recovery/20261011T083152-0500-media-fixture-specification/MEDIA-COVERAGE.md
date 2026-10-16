# Media coverage supplement

R0 proposal only, prepared by Codex on PAYTON-LEGION2. No record is applied, accepted or satisfied. This replaces five label-only media coverage entries for review purposes, not their requirement text. The base draft SHA256 is `320887C5F2F09154F1EC7970B041A638632741A7750E838FE05F77883BB1DA83`.

## ac-01: image-sector behavior

Use every named group in [D64-CONTRACT.md](D64-CONTRACT.md), including its safety and observation prerequisites. This is filesystem-backed image I/O with no GCR, not true-drive execution. `coverage-supplement.json` lists the groups explicitly. The proposed D64 TR and TEST IDs still do not exist in the effective store; this supplement does not reserve, create or map them. No passing evidence is implied.

## ac-02: true-drive IEC, motor and DOS behavior

The separate five-record drive proposal at `../20261008T153521-0500-legacy-drive-ac-proposal/proposed-field-delta.json` still requires its own accepted HV and exact approval. Current FR-ARCH-TRUEDRIVE-1541-002 has zero structured AC and its fixed 985-call/300000-cycle wording is not a valid replacement for the pinned source. The following cases expose what the media criterion depends on; they do not approve that separate delta.

### Media_Vic20IecDigitalMatrix

At the IEC routine boundary, use 1541 drive type, host bits A=ATN, C=CLK, D=DATA in `{0,1}`, and drive contribution bits DA, DC, DD in `{0,1}`. Exercise all 64 combinations with drive 8 enabled and disabled, with drives 9..11 disabled. Drive-write argument encodes the complement of DD in bit 1, DC in bit 3 and DA in bit 4; unrelated bits remain fixed. Host PA bit 7 is A; effective PCR bit 1 is C and bit 5 is D.

For enabled 1541 drive 8, expected physical bus levels are:

```text
atn = 1 XOR A
clk = (1 XOR C) AND (1 XOR DC)
data = (1 XOR D) AND (1 XOR DD) AND (1 XOR (A XOR DA))
driveRead = data OR (clk << 2) OR (atn << 7)
hostPaRead = (data << 1) OR clk OR (A << 7)
```

For a disabled drive, omit its CLK, DATA and modifier contributions. Extend with both drives 8 and 9 enabled, each independently holding CLK or DATA low while the other releases, then disable the holder. Preserve independent line levels at every step. Do not infer a direction register from an effective output byte: VIA register-to-output masking is an additional adapter-boundary fixture prerequisite.

Controls: invert one polarity, force CLK from ATN, omit the ATNA modifier, let a disabled drive pull low, or release a shared line while another enabled participant still holds it. Each control has a distinct expected rejection. Source: `vic20/vic20iec.c:49-73,95-127,143-149,206-234`.

### Media_C64IecDigitalMatrix

Use drive type 1541. At the C64 IEC boundary enumerate host bits 3,4,5 and drive-write bits 1,3,4, giving 64 combinations. Keep all other bits fixed to zero, except inactive `drv_bus` entries initialized to 0xFF. Use the exact integer formula, retaining the byte mask:

```text
cpuBus = ((H << 2) & 0x80) | ((H << 2) & 0x40) | ((H << 1) & 0x10)
driveBus = ((R << 3) & 0x40) | ((R << 6) & ((~R XOR cpuBus) << 3) & 0x80)
cpuPort = cpuBus AND all drv_bus[4..11]
drivePort = ((cpuPort >> 4) & 4) | (cpuPort >> 7) | ((cpuBus << 3) & 0x80)
```

Run drive-8-only and drive-9-only configurations separately. Include two-drive independent contention with the correct configuration dispatch; do not just call the single-drive helper twice. Include no-drive mode: writes update `iec_fast_1541`, reads return `(iec_fast_1541 & 0x30) << 2`, rather than running the drive CPU. The matrix is not proof of CIA2 DDR wiring, which must be pinned at the machine-adapter boundary before the real suite is complete.

Controls: omit drive 9, combine contributors with OR instead of AND, reuse the VIC-20 bit mapping, swap DATA/CLK, or execute a drive in no-drive mode. Source: `c64/c64iec.c:121-154`; `iecbus/iecbus.c:197-286,289-403`.

### Media_IecOrderedAtnEdges

Use fresh initialized machines and a recorded prelude, not an assumed universal zero snapshot. On VIC-20 set PA7=0, then write PA bytes `[0x80,0x80,0x00,0x00]` at successive pinned host clocks. Each write synchronizes drives first. For enabled 1541s, only the first high write sends CA1 RISE; both low writes call CA1 with value 0, including the repeated low write. After signaling, ATN state updates, modifiers recalculate, then the bus resolves. Exercise no enabled drive, drive 8 only, drive 9 only and both enabled.

On C64, seed `iec_old_atn` through the actual initial-state path, then change host bit 3 high-to-low, repeat low, low-to-high, repeat high. For the single-drive path: synchronize the selected drive; update CPU bus; signal CA1 only if ATN changes; recalculate drive contribution; resolve ports. Invert the CA1 direction relative to the effective bus ATN bit exactly as the source does. In the multi-drive path, retain execute-all and serial-device execution before host update and true-drive-only signaling.

Capture function boundary, argument, actor, domain clock and sequence ordinal, including calls that do not change a line level. A trace containing only transitions cannot prove repeated-call behavior. Controls: remove synchronization, move it after host update, duplicate a rising signal, suppress VIC-20 repeated-low signaling, add C64 repeated-write signaling, or shift the event while retaining the final bus level. Source: `vic20iec.c:153-213`, `iecbus.c:237-286,300-350,364-403`.

### Media_MotorZoneAndStopEdges

Pin fresh 1541 state, current half-track, prior VIA2 PRB, drive clock, attach clocks, read/write mode, rotation state, wobble resources and byte-ready state. Keep stepper bits unchanged in this group so a head move cannot masquerade as rotation. Enumerate zones 0..3, motor off->off, off->on, on->on and on->off, with byte-ready edge 0 and 1. Include an additional separately named combined stepper/motor transition when the full drive contract is implemented.

Expected call order: `rotation_rotate_disk` observes old motor state before PRB changes; changed zone bits update the zone; changed motor bit updates BRA_MOTOR_ON; off->on calls `rotation_begins`, setting last clock to current drive clock and cycle_index to 0. On->off with an existing byte-ready edge sets CPU overflow and clears the edge. Motor-off rotation clears req_ref_cycles and returns. Repeated motor-on does not restart rotation through `rotation_begins`.

Exact later head offset, bit transitions and byte-ready timing require pinned raw/GCR media and an accepted native observer. Their numerical traces are not present here. Retain them as mandatory open cases; neither 300000 synthetic ticks nor a status flag satisfies them. Separate media-attach delay from motor-on behavior. Controls: insert a fixed no-rotation ramp, reset rotation on repeated on, process the new motor state before old-state rotation, miss overflow, or use host clock as drive clock. Source: `via2d.c:199-217,321-350`; `rotation.c:139-143,295-305,1106-1164`.

### Media_DosBamAndModeSeparation

Use a freshly formatted, firmware-readable 35-track D64, not the address-pattern image. Pin its full bytes, BAM sector and firmware before tests. Run the same retained host program through the real C64 and VIC-20 IEC path to execute DOS block read of track 18 sector 0; retain command/channel bytes, returned 256 bytes, status-channel bytes and ordered CPU/VIA/IEC observations. Include absent media and native-observed unsuccessful reads. Run the buffered image operation as a separate case with mode=buffered, never as the true-drive result.

The formatted fixture, host program, build/ROM hashes, clock normalization and complete native traces remain to be produced and reviewed. They are explicit blockers to real acceptance, not placeholder hashes or invented expected DOS status strings. Controls reject a direct-image response labeled true-drive, mismatched firmware/media, fabricated success for missing media, or correct BAM bytes with missing CPU/VIA/IEC effects.

## ac-03: bind to the same tape contracts

`Media_TapeFullContractBinding` reuses all eight proposed FR-RUNTIME-TAPE-002 criteria and all 41 named groups from the base packet, preserved verbatim in `coverage-supplement.json`. It does not create a weaker second tape suite. Also retain every dependent FR-TAP/TR/TEST obligation in the fourteen-record draft, including retained turbo-loader workloads and file-cursor/initial-state cases. No metadata pulse-reader test or recording count substitutes for those observations.

The binding validator's inputs are exact approved packet hash, criterion IDs/text, expanded case IDs, assertion/fixture hashes, machine and provider. A correct binding includes all groups and the same assertions for mock, immutable native and managed providers. Controls independently omit a tape criterion, omit a group, substitute the older approved tape body, change a 32000-cycle startup/deferred-stop boundary, collapse sense into pulse routing, switch C64 and VIC-20 wiring, or drop a recording byte/side effect. Expected result is a named binding or comparison rejection, never a green reduced denominator.

These 41 groups are inherited proposed names, not proof that all their concrete fixture matrices exist. The detailed remaining tape matrices still block the combined packet's implementation readiness. This supplement exposes the exact dependency instead of reporting the old `ExistingMediaProposal_ac-03` label as coverage.

## ac-04: evidence identity and comparison

`Media_EvidenceIdentity`: require scenario ID, approved contract hash, expanded case ID, fixture recipe/version/hash and actual before/after image hash, native commit/build hash, observer hash, machine/region, firmware hashes, complete resources, drive types/enabled set, mode, initialized-state hash plus replay prelude, and provider identity. An explicit not-applicable reason is allowed only where the approved case does not use firmware or clocks. Missing is not equal to null or an empty array.

`Media_ClockAndOrder`: each observation carries domain (host, drive or tape), tick, phase when defined, actor, operation and sequence ordinal. Preserve simultaneous-event order. Normalize clocks only with the pinned source-derived conversion. Image-sector calls use operation ordinals and no invented CPU clock. Controls swap domains, rescale without a declared conversion, reorder equal-time events or compare buffered and true-drive modes; all reject before a parity verdict.

`Media_FirstMismatch`: compare complete observation streams with declared field types and byte arrays. Report the earliest unequal ordinal and its domain/time, exact expected and actual values, and both receipt paths. Correct identical nonempty streams pass; wrong first/middle/last value, omitted event, duplicate event and extra trailing event each fail at the exact expected ordinal. Empty streams fail when the case requires observations. A matching final sector or final line level cannot conceal intermediate differences.

## ac-05: mocks-first and real-gate evidence

`Media_GateEvidenceBinding`: inputs are the approved case manifest and chronological mock, negative-control, implementation, native, managed and cumulative receipts. Require all case IDs, fixture hashes, assertion hashes and required fields to match the manifest; require zero failed, skipped, aborted, missing or unexecuted cases in the applicable gate. Negative controls must be individually listed as expected rejections inside a passing harness. Native and managed results must be real provider executions, not relabeled mock output.

Correct complete evidence can establish only its own gate. Controls independently remove one case, mark one skip, replace an assertion hash, replace a fixture, reverse mock/implementation chronology, omit a negative control, substitute a diagnostic filter for the full suite, fabricate a native provider, supply only a test count, or omit the full HV verdict/readback. Each is rejected with the precise missing/mismatched evidence.

`Media_ApprovalAndAcceptanceBoundary`: before execution, require exact R0 packet approval and dependency approvals. Before satisfaction, require the applicable full real-emulator/native-VSF/cumulative gates and designated independent AGREE, accuracy and completeness each >=98, zero FAIL/UNKNOWN, complete JSONL and MCP readback. A score of 97, a missing score, DISAGREE or an unapplied proposed dependency rejects acceptance. This is a future test contract, not a claim those tests run or passed.

## Readiness

The five inherited labels now have explicit planned case bindings and controls, but the whole corrected packet is not yet implementation-ready. Outstanding: detailed tape fixture matrices, exact observer/provider interfaces, runtime fault mechanisms, true-drive media/firmware/trace fixtures, drive/D64 approval dependencies, current guard schemas, root handoff reconciliation and fresh whole-packet HV. No open requirement or test is retired, silently removed or marked done.
