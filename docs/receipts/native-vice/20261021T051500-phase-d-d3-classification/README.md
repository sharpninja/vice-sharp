# PLAN-NATIVERESTORE-001 step D3: classification receipt (2026-10-21T05:15, LAB-OMARCHY)

Plan section 2 (revision 8, HV AGREE 98/98 on 2026-10-20) confirmed against the two D0 hunk maps, with the file-and-hunk list, Payton's answers R1 to R16, and the fixture scan result. Written by Claude Code (claude-fable-5-1-high@ClaudeCode, session OMAR-VICE#) after Payton's R16 ruling (2026-10-20T22:58, "C: run D3 to D5 now, rule on the gate at the exit"). Nothing under `native/vice` was read by a tool other than `git status` and `git diff` in D0; nothing changed.

## Inputs

- LAB-OMARCHY D0: `docs/receipts/native-vice/20261020T211402-phase-d-preservation-linux/manifest.json` (host LAB-OMARCHY, gitlink and checked-out SHA `cc67418cba58e71d8e70fd353e9de47199ad2075`, cached diff empty, patch `native-vice-tracked.patch` 12,868 bytes SHA256 `D87A63A435C1DEDA2C1E5F00A2F951ED6926225FB2C372AF689F9D1CA1610E1C`, 163 entries, 8 modified tracked, 155 untracked, 11 hunks, hunk map expected 11 rows, 0 unclassified, 0 explained differences, 0 missing expected, Outcome Complete).
- PAYTON-LEGION2 D0: `docs/receipts/native-vice/20261020T213032-phase-d-preservation-windows/manifest.json` (read over the read-only mount; host PAYTON-LEGION2, 2026-10-20T21:30:39, same gitlink and checked-out SHA, patch 22,404 bytes SHA256 `90604E39B827F850A382DC13DD921141290EF2C34E1483CDC6C612A07803E9E2`, byte-identical to the Oct 4 Phase 0 patch, 2,539 entries, 12 modified tracked, 2,527 untracked, 20 hunks, hunk map expected 20 rows, 0 unclassified, 0 explained differences, 0 missing expected, Outcome Complete; libraries `native/vice_x64.dll` `E5B07651...` and `native/vice_xvic.dll` `F03A7A35...` retained per R4).
- Fixture scan: `20261020T211402-phase-d-preservation-linux/vsf-fixture-scan.json`.
- The approved plan: `docs/plans/PLAN-NATIVERESTORE-001.md` section 2 (the dispositions below quote it) and section 3 (the rulings).

## File-and-hunk list with the confirmed row of each hunk

LEGION2 carries all 20 hunks of the Oct 4 patch (12 files); LAB-OMARCHY carries the 11-hunk, 8-file subset (its checkout never had the xvic, mainviccpu, vic20sound and mainc64cpu `@@ -196` hunks). The D0 maps assign each live hunk to a plan row; the two maps agree on every shared hunk.

| Patch hunk (file, header) | LAB-OMARCHY | LEGION2 | Row | Disposition (plan section 2) |
|---|---|---|---|---|
| `vice/src/c64/c64cpusc.c` `@@ -32,6 +32,10 @@` | 1 | 1 | H1 | Reject: served by the clock gate and bus observer |
| `vice/src/c64/c64cpusc.c` `@@ -44,11 +48,32 @@` | 2 | 2 | H1 | Reject |
| `vice/src/drive/drive-snapshot.c` `@@ -314,9 +314,13 @@` | 3 | 3 | H5 | Reject (R7 A) with the R10 A launch precondition; R14 dead read accepted |
| `vice/src/drive/drivecpu.c` `@@ -188,6 +188,10 @@` | 4 | 4 | H6 | Reject (R7 A); unserved behavior, no non-snapshot drive reset in oracle workloads |
| `vice/src/drive/drivecpu65c02.c` `@@ -170,6 +170,10 @@` | 5 | 5 | H6 | Reject (R7 A) |
| `vice/src/mainc64cpu.c` `@@ -56,6 +56,10 @@` | 6 | 6 | H2 | Reject: served by `__wrap_vicii_steal_cycles` |
| `vice/src/mainc64cpu.c` `@@ -196` (checkpoint in the stolen-cycle loop) | absent | 7 | H2 | Reject |
| `vice/src/mainc64cpu.c` `@@ -752,7 +756,37 @@` | 7 | 8 | H3 | Reject: served by VSF resume through the pinned binaries (R8) |
| `vice/src/mainc64cpu.c` `@@ -951,11 +985,31 @@` | 8 | 9 | H4 | Reject: served by the external adapter (AC1 as amended by R15) |
| `vice/src/mainviccpu.c` `@@ -47` (include) | absent | 10 | H7 | Reject: served by `--wrap=vic_cycle` |
| `vice/src/mainviccpu.c` `@@ -579` (xvic bootstrap) | absent | 11 | H12 | Reject: served by VSF resume through the pinned xvic (R8; fixtures are the D5 Slice 1 obligation under R12) |
| `vice/src/resid/filter8580new.h` `@@ -551,6 +551,16 @@` | 9 | 12 | H8 | Reject; `s9-filter-probe.patch` stays as evidence, never applied |
| `vice/src/resid/sid.h` `@@ -61,6 +61,10 @@` | 10 | 13 | H8 | Reject |
| `vice/src/sid/resid.cc` `@@ -449,4 +449,113 @@` | 11 | 14 | H8 | Reject |
| `vice/src/vic20/vic20cpu.c` `@@ -31` (include) | absent | 15 | H7 | Reject |
| `vice/src/vic20/vic20cpu.c` `@@ -40` (hosted `CLK_INC`) | absent | 16 | H7 | Reject |
| `vice/src/vic20/vic20sound.c` `@@ -306` | absent | 17 | H9 | Reject (R7 A); Phase F item 6 owns the pinned behavior |
| `vice/src/vic20/vic20sound.c` `@@ -338` | absent | 18 | H9 | Reject (R7 A) |
| `vice/src/vic20/vic20sound.c` `@@ -578` (oracle API) | absent | 19 | H10 | Reject (as H8) |
| `vice/src/vic20/vic20sound.h` `@@ -36` (declarations) | absent | 20 | H10 | Reject |
| injected `vice/src/vice-shim-runtime.h`, `vice/src/.vice-shim-configure-flags` | absent | untracked, preserved | H11 | Retained untracked (R4), listed in the inventory, never deleted |

Hunk accounting: LEGION2 rows H1 2, H2 2, H3 1, H4 1, H5 1, H6 2, H7 3, H8 3, H9 2, H10 2, H12 1 = 20, equal to the plan's accounting (which counts H12 within its file). LAB-OMARCHY rows H1 2, H5 1, H6 2, H2 1, H3 1, H4 1, H8 3 = 11. Both D0 maps report 0 unclassified hunks, so no hunk is "required and uncovered" (A01): no blocker before D4 arises from classification.

Behavior corrections (H5, H6, H9): each rejected with its external disposition (R7 A): H5 by the adapter's launch precondition `-drive<n>type 0` for every unit whose `DRIVE` module is missing or has `has_tde = 0` (R10 A, proven at the unit level in D1 and against the real binaries in D5); H6 by the workload constraint (no non-snapshot drive reset); H9 by Phase F item 6 ownership (a reached zero divisor or out-of-range index in the pinned xvic is a named Phase F blocker).

## Payton's rulings, verbatim option labels (plan section 3)

- 2026-10-20T10:57: R1 "B: .gitmodules ignore = untracked only"; R2 and R3 "A: retire it now"; R4 "Yes, exclude and keep"; R5 "Yes, add in D2".
- 13:03: R6 "A: ShimLegacy category (Recommended)"; R7 "A: reject all three (Recommended)"; R8 "A: observer -autostart .vsf (Recommended)"; R9 "A: known-red set of 20 (Recommended)".
- 14:06: R10 "A: adapter launch precondition (Recommended)"; R11 "B: require a native register observer".
- 15:30: R12 "B: exact cycle N required"; R13 "B: implement HalfCycleState in Phase D"; R14 "A: accept as benign dead read (Recommended)". 16:29, asked explicitly about a pinned boundary checkpoint: R12 "B: literal 100,000 only".
- 17:23: R15 "change to v1.2 and v1.4".
- 19:35: "Release Phase D implementation now".
- 22:58: R16 "C: run D3 to D5 now, rule on the gate at the exit".

How each ruling lands in D4 and D5: R1 B is the `.gitmodules` change applied in D4 before the first restore; R2 and R3 are done (D2 part 1); R4 means the restore script lists and never deletes untracked entries (the F08 bracket proves none new, deleted or changed); R6 and R9 shape the ordinary gate (measured in D2, ruled at the exit under R16); R7, R10, R14 govern H5, H6 and H9 above; R8, R11, R12, R15 are D5's obligations (resume through `x64sc-observer -autostart`, the register observer records, the literal 100,000 checkpoint on both hosts or `BLOCKER-R12-XVIC-CHECKPOINT`, the v1.2 and v1.4 real inputs); R13 is done (D2 part 2).

## Fixture scan result (D0, LAB-OMARCHY)

Three `.vsf` files, all valid containers with a readable `MAINCPU`, no invalid payloads:

- `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`: SHA256 `5C284F5C...`, 190,854 bytes, C64SC, `MAINCPU` v1.2 (113-byte payload) at clock 109,163,946, A `$C2`, X `$02`, Y `$00`, SP `$FA`, PC `$5769`, P `$A0`; `DRIVE8` present with `has_tde = 0` and `has_drives = 1` (24-byte payload), `DRIVE9` to `DRIVE11` with `has_tde = 1`; precondition `-drive8type 0` (R10 A: this is the H5 path, N01).
- `tests/ViceSharp.VsfCompatibility.Tests/Fixtures/native-maincpu-1.2.vsf`: byte-identical to the file above (same hash and length); same precondition.
- `tests/ViceSharp.VsfCompatibility.Tests/Fixtures/native-maincpu-1.4.vsf`: SHA256 `1D6A178A...`, 193,261 bytes, C64SC, `MAINCPU` v1.4 (125-byte payload) at clock 30,250,590; every `DRIVE` unit has `has_tde = 1`; no precondition.

No xvic fixture exists yet; the two Slice 1 xvic fixtures are generated in D5 under R12.

## What D3 does not do

No source changed, no restore ran, no gate changed. D4 follows on LAB-OMARCHY first (8 files), then LEGION2 (12 files, with the two injected files retained).

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 98%, accuracy 99%, completeness 97%
