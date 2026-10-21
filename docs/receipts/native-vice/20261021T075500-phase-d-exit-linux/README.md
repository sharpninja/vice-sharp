# PLAN-NATIVERESTORE-001 Phase D exit receipt, LAB-OMARCHY (Linux): state BLOCKED

Session OMAR-VICE#, written 2026-10-21T07:55 after D5 part 2 (receipt `docs/receipts/native-vice/20261021T053000-phase-d-d5-linux/README.md`). Phase D has exactly two end states (plan exit, N14): COMPLETE or BLOCKED. This host's state is BLOCKED. Nothing below is a done-state claim; no acceptance criterion is marked satisfied. The exit items are listed one by one with what was measured on this host; LEGION2 has executed none of D3 to D5 (Payton's hold of 2026-10-21T05:35, relayed by LEG-VICE#), so every two-host item is pending by definition.

## Blockers open

- `BLOCKER-R12-XVIC-CHECKPOINT` (R12 B, literal 100,000): on LAB-OMARCHY both xvic fixtures (`xvic-pal-unexpanded`, `xvic-ntsc-unexpanded`) were written at clock 100,002 at trigger 100,000 in every run of three rounds (two runs each); writer result 0; candidates byte-deterministic per model (PAL `20932DC3E8634F9024EA1E82D57B953CAAB7C796A912F0884E0516934EDF8014`, NTSC `BB4F4F19AB54421F8726C04FCE146843475EE453B5043198506363F91A3A6EB5`). Context: clock 100,000 PHI2 read $FDB8 data $A5 (LDA zero page opcode fetch), 100,001 read $FDB9 data $97 with interrupt mask 8 (IK_TRAP only), 100,002 read $0097 data 0 and the checkpoint, 100,003 read $FDBA; no IRQ or NMI pending, no stolen cycle. The pinned trap runs at the next instruction boundary (6510core.c 482 to 486); the x64sc round trip at trigger 300,000 wrote at 300,002 the same way. LEGION2 clocks: not measured (hold). Blocked obligations and case identifiers: fixture provisioning (`NativeViceSnapshotPrerequisiteTests.RequiredFixtures_ArePresentAndHashPinned`, reported BLOCKED), deterministic regeneration (`NativeViceSnapshotFixtureGeneratorTests.Regeneration_IsByteDeterministic`, reported BLOCKED), the cumulative target (`NativeSnapshotCompatibilityTest --native-snapshot-through-slice 1`: total 10, passed 8, failed 0, BLOCKED 2, exit nonzero), full Slice 1 acceptance, Phase E start. Blockers file: `docs/requirements/oracles/vice-vsf-fixture-blockers.json`.

## Exit items, measured on LAB-OMARCHY

- Gitlink and checked-out SHA equal; `diff --exit-code` and `diff --cached --exit-code` empty; porcelain inventory equal to the D0 classified inventory by path, hash and length; superproject shows the submodule clean: D4 receipt `20261021T052000-phase-d-d4-restore-linux` (every proof true) and the F08 bracket after D5 (Equal, 155 entries, head equal, `submodule-inventory-after-d5.json`). LEGION2: pending.
- No generated output, injected source or wrapper input under `native/vice` participates in the oracle build: the builder reads the materialization copy only; inventory stage "original materialization files unchanged: 3841" after the 06:48 relink.
- `TEST-VICE-001` AC1 and AC2 clean-target preflight: covered by the builder's Copy and Inventory stages (hash-verified copy, unchanged materialization) in the D5 receipt section 1.
- `REAL-C64-PHI2-AEC-001` on real traces: passes over three committed x64sc traces with negative controls (reference evidence; managed `TR-CYCLE-001` AC4 and `PLAN-VICEORACLE-001` task 5 pending Phase E).
- Comparator XML docs corrected and verified: `LockstepValidator` summary corrected; `LockstepValidatorDocumentationTests` passes.
- `Vic20HalfCycleComparatorTests` green (R13 B): green in the D2 and D5 gates (not in the failure lists).
- VSF Slice 1 obligations on this host: libraries from the pinned commit (pass), MSYS2 pass-through on Linux (pass), fixtures (BLOCKED), regeneration (BLOCKED), manifest (pass, schema 1, 34 reader units, supplied fixture), legacy adapter byte cases (199 unchanged, pass), real v1.2 resume (pass), real v1.4 round trip (pass), cumulative target (BLOCKED state, nonzero exit), absence fails instead of skips (every real-bound case ran inside the cumulative target; none skipped). LEGION2: all pending.
- Nuke `Test` under A05: run 2 of D5 (`gate-2/`): seven projects, 4,477 cases, 4,417 passed, 42 failed (the R9 set of 20 plus 22 outside it: the 21 pre-existing at `af62785c` and one host-load pacing diagnostic), 18 not executed by their own opt-in conditions, load log empty, zero ShimLegacy leaks, drift of 9 identifiers all added by Phase D. The evaluator's verdict is Fail under the literal A05 rule; the R9 premise was never a full-suite measurement, and the denominator ruling is Payton's (R16: "rule on the gate at the exit"). Run 1 of D5 lost the NativeClockProbe project's output without any logged cause; run 2 did not reproduce it.
- Receipts with self-assessment: D5 receipt sections 10 and 12.
- `PLAN-VICEORACLE-001` tasks 4 and 5: left pending through the TODO wrapper with a dated evidence note appended to `remaining` (readback verified); no done state.
- Hostile review of the implementation: deferred to step 12 (D3). Not run.

## What Payton has to rule

1. `BLOCKER-R12-XVIC-CHECKPOINT`: the pinned VICE cannot write a snapshot strictly inside an instruction; a trigger that lands on an opcode fetch is honored at the next instruction boundary (here 100,002). Options as the plan states them: keep the literal 100,000 and stay BLOCKED, or lift the blocker by ruling on an accepted write clock (for example "the trigger is armed, the recorded write clock is the fixture's clock", with the manifest carrying both). LEGION2's measurement is still required either way.
2. The A05 denominator (R16): whether the 22 failures outside the R9 set (21 pre-existing before Phase D plus one pacing diagnostic per run) and the 18 self-skipping opt-in cases are inside or outside the Phase D gate.
3. LEGION2's hold: D3 to D5 on LEGION2 cannot start until it is lifted; LEGION2 must pull `bae513bc` or later first (bus adapter and generator changes).

## Self-assessment

Correctness 92 percent, accuracy 93 percent, completeness 85 percent for this host (as the D5 receipt states); the two-host completeness is 50 percent by construction while LEGION2 is held.
