# PLAN-VICEPARITY-001 S12 receipts (2026-07-10)

SID output amplify/clip + external-filter enable. Branch
`fix/nativeresidue-002-drive-clock-hardening`. Managed-only (no native rebuild;
dll unchanged b743c666).

## What landed

- `Sid6581.cs`: `ClipPcm16(int)` (reSID clip, sid.cc:42-52) + `AmplifyToPcm16(int)`
  = `ClipPcm16((OutputScaleFactor*input)/2)` (reSID amplify, sid.cc:54-57).
  `GenerateSample` reSID branch now returns `AmplifyToPcm16(_cycleExtFilterOutput)
  / 32768f` (was `Clamp(ext/32768, -1, 1)`), so the 6581 is 1.5x louder
  (scaleFactor 3) matching VICE; the 8580 uses scaleFactor 5.
- `Sid6581.Filter.cs`: `_extFilterEnabled` (default true, mirroring VICE
  resid.cc:200) + `EnableExternalFilter(bool)` + the disabled pass-through branch
  in `ClockResidExtFilter6581` (Vlp=Vi<<11, Vhp=0; extfilt.h:100-105).
  `ExternalFilterEnabled` seam.
- 7 new ParityAc in `SidOutputAmplifyParityTests.cs` (FR-SID-OUTPUT AC-01..07).
- Ratchet `ExpectedMinCovered` 444 -> 451.

## Why amplify is on the float host path, not the capture tee

reSID applies `amplify(output(), scaleFactor)` at sample EMISSION (sid.cc:886-888);
`SID::output()` itself (= extfilt.output()) is pre-amplify. The managed
`_cycleExtFilterOutput` == `SID::output()` (bit-exact vs the oracle), so the
lockstep is on the pre-amplify value and the amplify sits on top for the float
host contract. `CaptureAudioTap` is unchanged (capture tees the pre-amplify
path). C# int division truncates toward zero exactly like C++; `/2^15` is exact
(every short is representable in float).

## Gate results (exact)

- Focused `~SidOutputAmplifyParityTests`: **7 passed / 0 failed** (incl. OUTPUT-01
  composite lockstep vs the c64 oracle: SID::output() bit-exact + amplified-float
  identity every cycle, 4000 cycles).
- `~SidDigiPlayback|~ParityCoverageManifestTests|~XmlDocsConventionTests`:
  **17 passed / 0 failed** (digi range/relative assertions survive the 1.5x
  amplify; ratchet 451; XmlDocs tokens present).
- `~LockstepValidation|~SidAudio|~SidClockRate|Category=Determinism`:
  **24 passed / 0 failed** (amplify blast radius clean).
- `Category=Parity&Category!=ParityPending`: **446 passed / 0 failed / 0 skipped**.
- Full baseline `Category!=Determinism&Category!=AiReview`: (see closing note;
  criterion is 0 NEW failures vs the 7 pre-existing VideoRendererTests baseline
  documented in receipts-viceparity-s11b-2026-07-10.md, attributed via
  stash-baseline diff).

## Bookkeeping

- TR-SID-AMPLIFY-001 created (subarea AMPLIFY).
- PLAN-VICEPARITY-001 `remaining` updated (read-back verified): S9-S12 DONE,
  ratchet 451; remaining S13 (resampler ->457).
