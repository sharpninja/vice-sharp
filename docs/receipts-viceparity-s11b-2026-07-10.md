# PLAN-VICEPARITY-001 S11b receipts (2026-07-10)

8580 reSID op-amp/DAC filter port + SAMPLE_FAST write pipeline + per-model
constants + Sid8580D collapse. Branch `fix/nativeresidue-002-drive-clock-hardening`.
Managed-only slice: native `vice_x64.dll` unchanged at SHA256 `b743c666...`
(the S11a build); no mingw rebuild.

## What landed

- `Sid6581.Filter.cs`: `ResidFilterModel` (generalized container, +NParam/
  NVgtDefault, nullable 6581-only VCR tables), `BuildResidModel8580`,
  `SolveIntegrate8580`, `SetW0_8580`, `AdjustFilterBias8580`, model-parameterized
  dispatch (FilterModel / IsMos8580Filter / SetW0), `_nVgt`/`_nDac` fields.
- `Sid6581.cs`: `CommitWrite` extraction + 8580 SAMPLE_FAST write pipeline
  (SidSamplingMethod, default Resample), per-model `OutputScaleFactor` (amplify
  3/5), parity seams, and two reSID-fidelity fixes (below). `Voice.WaveformOutput`.
- `Sid8580.cs`: bind Model8580 + solve_integrate_8580 + SetW0_8580 + DataBusTtl
  0xa2000 + OutputScaleFactor 5 + nonlinear EnvelopeDac8580.
- `SidSamplingMethod.cs` (new enum).
- Deleted: `Sid8580D.cs` (fabricated, no reSID counterpart), `Sid8580FilterTests.cs`,
  `Sid6581NonLinearCutoffTests.cs` (subjects removed).
- 25 new ParityAc: `SidFilter8580DivergentParityTests.cs` (14) +
  `Sid8580VariantParityTests.cs` (11). DATABUS-07 un-blocked.
- Ratchet `ExpectedMinCovered` 419 -> 444.

## Two reSID-fidelity fixes (surfaced by the c64c lockstep)

1. Seed `_nVgt` at construction. reSID `nVgt` is a static Filter member set at
   init (filter8580new.cc:603) and `reset()` never clears it; seeding only in
   ResetFilter failed tests that skip Reset (`_nVgt`=0 => `vi<nVgt` always false
   => integrator ignores vi => Vlp==Vbp, runaway). Diagnosed via a probe dump:
   Vlp/Vbp identical + drifting -758/cyc while the oracle held ~30254. Default
   8580 filter bias is 0 (sid.h:160), so NVgtDefault == the adjust_filter_bias(0)
   value (48019).
2. Feed the filter the CURRENT waveform_output, not the delayed Osc3. reSID
   `Voice::output()` uses `model_dac[waveform_output]` (wave.h:588-592); `osc3`
   (readOSC/$1b) is one cycle late on the 8580 tri/saw path (wave.h:475-486).
   The managed model conflated them; pulse (v2) matched but ramping saw/tri
   (v1/v3) were 1-2 LSB off. Added `Voice.WaveformOutput`.

Also: Deliverable 7 swapped the 8580 envelope from a linear-identity stub to the
nonlinear EnvelopeDac8580 (2R/R 2.00 terminated). Both fixes realigned the two
stale S8 voice-mix ACs (VOICE-03/06).

## Gate results (exact)

- Focused `~SidFilter8580DivergentParityTests|~Sid8580VariantParityTests`:
  **25 passed / 0 failed / 0 skipped** (incl. 5 [ViceFact] bit-exact lockstep
  vs the c64c oracle: set_w0 cutoff sweep, solve_integrate_8580 LP program,
  per-cycle ext-filter output, SAMPLE_FAST write pipeline, combined waveforms).
- Smoke lockstep (pre-commit throwaway, deleted): managed Sid8580 filter probe[8]
  AND SidExactOutput both bit-exact 4000 cycles vs c64c.
- `Category=Parity&Category!=ParityPending`: **438 passed / 0 failed / 0 skipped**.
- XmlDocs + ParityCoverageManifest (`~XmlDocsConventionTests|~ParityCoverageManifestTests`):
  green; ratchet 444 met; DIVERGENT<->red-now manifest consistency holds.
- `Category=Determinism` (MANDATORY - 8580 output path changed):
  **5 passed / 0 failed / 0 skipped**.
- Full baseline `Category!=Determinism&Category!=AiReview`:
  **7 failed / 2662 passed / 21 skipped / 2690 total**.

## Baseline failure attribution (stash-baseline diff)

The 7 baseline failures are ALL `VideoRendererTests` (VIC-II sprite/border/raster-
crop rendering) - a subsystem the SID-only S11b changes cannot causally affect.
Proven pre-existing:

- My working tree, `~VideoRendererTests` in isolation: 7 failed / 29 passed / 36 total.
- `git stash push --include-untracked` (clean HEAD), same filter: **identical**
  7 failed / 29 passed / 36 total, same test names.

Therefore S11b introduces **0 new test failures**. The 7 are part of the known
pre-existing VIC-II full-suite failing set (PLAN-VICEPARITY-001 VIC audit Phases
3-6 pending; see memory project_vic_audit_remediation). Failing test ids:
RenderFullFrame_DrawsBackgroundInCarriedOpenLeftSideBorder,
DrawsSpriteInContinuousOpenLeftSideBorder, DrawsSpriteInOpenedRightSideBorder,
CropsPalRasterSoActiveScreenIsVerticallyCentered,
DrawsBackgroundInOpenedRightSideBorder, KeepsSpriteBehindCycle17CselBlankedLine,
DrawsSpriteInCarriedOpenLeftSideBorder.

## Bookkeeping

- TR-SID-ORACLE-002: exists (createTr rejects duplicate); notes updated to record
  S11b consumption of vice_sid_exact_set_sampling (AC-06 SAMPLE_FAST lockstep).
- PLAN-VICEPARITY-001 `remaining` updated (read-back verified): S9/S10/S11 DONE,
  ratchet 444; remaining S12 (amplify/clip ->451) + S13 (resampler ->457).
- PLAN-SIDCHAMBERLIN-001 created: deferred retirement of the now-dead 6581
  Chamberlin SVF stack (entangled with SidFilter6581Tests/SidDigiPlaybackTests,
  not AC-gated).
