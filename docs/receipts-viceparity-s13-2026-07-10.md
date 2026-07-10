# PLAN-VICEPARITY-001 S13 receipts (2026-07-10)

SID fixed-point sampling engine port (fast / interpolate / Kaiser-FIR resample).
Branch `fix/nativeresidue-002-drive-clock-hardening`. Managed-only (no native
rebuild; dll unchanged b743c666). This completes the SID parity program: ratchet
457 (the 9 remaining ACs to 466 are non-SID chip families).

## What landed

- `src/ViceSharp.Chips/Audio/Sid6581.Sampling.cs` (new partial): statement-for-
  statement port of reSID's sampling front-end (sid.cc:542-1038, sid.h:140-179):
  I0 Bessel kernel, SetSamplingParameters (constraint checks, 16.16
  cycles_per_sample, mirrored ring short[RingSize*2], Kaiser-windowed sinc FIR
  table), and ClockBuffered -> ClockFast / ClockInterpolate / ClockResample.
  ClockBuffered calls the managed single-cycle Tick() where reSID calls clock()
  and reads CycleExternalFilterOutput where reSID calls output(); amplify ->
  AmplifyToPcm16, clip -> ClipPcm16. RESAMPLE_FASTMEM not ported (throws). All
  buffers preallocated in SetSamplingParameters (zero per-sample allocation).
- 6 new ParityAc in `SidResampleDivergentParityTests.cs` (FR-SID-OUTPUT AC-08..13).
- Ratchet `ExpectedMinCovered` 451 -> 457.

## Transcription method (ultracode)

The port was produced by a 13-agent Workflow: 3 transcribers (I0+setup, fast+
interpolate, resample) x 3 adversarial verifier lenses (fixed-point-and-rounding,
off-by-one-and-indexing, signed-shift-and-overflow) + 1 synthesizer. The
verification caught real bit-exactness details before any test ran:
- reSID's `log(2.0f)` binds the FLOAT overload (logf) -> the FIR_RES clamp must
  use MathF.Log(2f), not Math.Log(2.0), or ceil() boundaries can flip fir_RES.
- The FIR fill's `(short)round(val)` is round-half-AWAY-from-zero (C round), not
  C#'s default banker's rounding -> Math.Round(val, MidpointRounding.AwayFromZero).
- The remainder interpolation uses unsigned casts: v1 + (int)((uint)rmd*(uint)(v2-v1) >> 16).
- The mirrored ring double-write + the +1 fir_offset==fir_RES wrap.

## Buffered-oracle smoke (de-risk before the port)

The SidExactClockBuffered export (landed S11a, never exercised) was smoked FIRST:
4096 cycles at SAMPLE_RESAMPLE 44100 produced got=183 samples, remainingCycles=0,
non-trivial output. The marshalling ([Out] short[] + out int remaining) is correct.

## SAMPLE_FAST caveat (documented, not a defect)

reSID's clock_fast advances the chain via the BATCHED SID::clock(delta_t)
(sid.cc:745-832), which holds the voice outputs constant over the window and
sub-steps the filter (dt=3) / external filter (dt=8). The managed fast path
clocks cycle-exact (Tick per cycle). So SAMPLE_FAST per-sample VALUES diverge;
the batched dt=3/dt=8 filter sub-stepping is the deferred TEST-SID-FILTER-CLOCK-
03/04 (plan out-of-scope). OUTPUT-08 therefore locks the fixed-point next-sample
CADENCE (sample count + unconsumed remainder, purely arithmetic) bit-exact vs the
oracle, and the high-fidelity VALUE lockstep is sealed by OUTPUT-09 (interpolate)
and OUTPUT-10 (resample) - both cycle-exact in reSID.

## Gate results (exact)

- Focused `~SidResampleDivergentParityTests`: **6 passed / 0 failed**.
  - OUTPUT-08 fast cadence lockstep (count+remainder), >= 30000 samples.
  - OUTPUT-09 interpolate VALUE lockstep, >= 30000 samples.
  - OUTPUT-10 resample Kaiser-FIR VALUE lockstep, >= 44100 samples, on BOTH the
    6581 (c64) AND the 8580 (c64c) oracle (also seals S11 scaleFactor 5 + S12
    amplify end-to-end).
  - OUTPUT-11 cycles_per_sample 16.16; OUTPUT-12 constants; OUTPUT-13 FIR table
    vs independent in-test sinc*Kaiser recompute (round-away-from-zero).
- XmlDocs + ParityCoverageManifest (ratchet 457): green.
- `Category=Parity&Category!=ParityPending`: **452 passed / 0 failed / 0 skipped**.
- `Category=Determinism`: **5 passed / 0 failed** (Sampling.cs is invoked only via
  ClockBuffered; the existing live-audio/GenerateSample path is untouched).
- Full baseline: 0 NEW failures vs the 7 pre-existing VideoRendererTests (see
  receipts-viceparity-s11b; attributed via stash-baseline).

## Deferred app-wiring

Routing the live-audio path through the resampler (it currently uses the
double-accumulator nearest-sample picker) is a follow-up; S13 exposes the
verified resampler as the parity surface via ClockBuffered. Tracked in
TR-SID-RESAMPLE-001.

## Bookkeeping

- TR-SID-RESAMPLE-001 created (subarea RESAMPLE); TR-SID-ORACLE-002 buffered
  export now consumed.
- PLAN-VICEPARITY-001 remaining updated: all SID parity ACs authored (ratchet 457).
