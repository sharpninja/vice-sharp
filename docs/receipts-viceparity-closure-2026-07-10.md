# PLAN-VICEPARITY-001 closure receipts (2026-07-10)

Machine-verifiable evidence for the final four slices of the VIC-II + SID
VICE-fidelity program (CH, BC, LW, CL) and the program-closure gates. Branch
`fix/nativeresidue-002-drive-clock-hardening`, pushed to `origin` (Azure DevOps).

All test runs: `dotnet test ./tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release`.

## Slice CH - retire the dead Chamberlin SVF stack

- Commits: c887e3d (red absence locks), 26cd9f8 (deletion). Pushed 6d92f3c..26cd9f8.
- Deleted 10 dead members + 7 conditional guards made unconditional; deleted
  SidFilter6581Tests.cs (all `[Fact(Skip)]`, subject = retired Chamberlin).
- Gates: SidChamberlinRetirement 3/3 absence locks; parity 452/0; determinism 5/0.
- MCP: PLAN-SIDCHAMBERLIN-001 closed.

## Slice BC - batched clock(delta_t) engine + last 9 ACs + 466 pin

- Ported reSID `SID::clock(delta_t)` (Sid6581.BatchedClock.cs): write-pipeline
  prologue, batched bus aging, batched envelope/wave/set_waveform_output, the
  oscillator MSB-toggle sub-step loop, filter dt=3 and extfilt dt=8 sub-stepping.
- `ClockFast` now calls `ClockBatched`, making SAMPLE_FAST value-bit-exact.
- OUTPUT-08 upgraded to full value lockstep at SAMPLE_FAST on both 6581 (c64)
  and 8580 (c64c) vs the buffered oracle - bit-exact on the first try.
- Un-pended the 4 SID quarantines (FILTER-6581-27, FILTER-CLOCK-03/04,
  CUTOFFDAC-06); authored the last 9 (EXTFILT-01..07, CLOCK-05/08).
- Ratchet 457 -> 466 with the strict completion pin `Assert.Equal(466, covered)`.
- Only pending AC manifest-wide is TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock, out
  of SID scope).

## Slice LW - live-audio wiring through the resampler (Resample always)

- Commit 4924b94. Pushed a7415ea..4924b94.
- Replaced the double-accumulator nearest-sample live emission with a per-Tick
  decomposition of the reSID fixed-point resampler (SAMPLE_RESAMPLE, VICE x64sc
  default). The live "push" tail shares the buffered "pull" engine state (ring,
  Kaiser FIR, 16.16 cadence).
- `ConfigureAudioClock` forces Resample and arms the tail only when a backend is
  present; `SetRelativeSpeed` scales the live 16.16 cadence only (pitch-shift
  warp, VICE sound_set_relative_speed) - synthesis state untouched.

### TR-SID-RESAMPLE-001 (recorded here; no separate tracked item exists)

- Policy: live audio always resamples (SAMPLE_RESAMPLE), matching VICE x64sc's
  default. Accepted the CPU cost; benchmark below is the evidence.
- Warp semantics: relative speed scales only the live decimation ratio; the FIR
  table and synthesis state are untouched, so parity and determinism are
  unaffected (the parity surface calls SetSamplingParameters directly).
- Equivalence proven bit-exact: the live push tail emits the identical short
  stream AND identical sample count as the buffered ClockBuffered pull.

Gates (SidLiveSampling 4/4; audio-consumer suites 19/19; SidResample 6/6):

```
LiveResampleTail_EquivalentToBufferedResample   PASS  (bit-exact push == pull)
LiveEmission_GatedByBackendAndConfigure         PASS
Warp_ChangesCadenceNotSynthesis                 PASS
LiveResampleTail_IsZeroAllocationOnHotPath      PASS  (0 bytes over 985248 ticks)
```

Benchmark (SidSamplingBenchmarks, 100k SID cycles, ShortRun, MemoryDiagnoser):

```
Method                                     Mean       Ratio   Allocated
Tick() x N (no audio)                      12.09 ms   1.00      -
Tick() x N + live SAMPLE_RESAMPLE tail     30.20 ms   2.50      -
ClockBuffered SAMPLE_RESAMPLE x N cycles   28.86 ms   2.39      -
```

100k SID cycles = ~101.5 ms of PAL audio; the live resample tail runs it in
30.2 ms = ~0.30x real-time (about 30% of one core), so Resample-always holds
100% speed with headroom. No SIMD follow-up needed. Zero allocation on all paths.

Post-review hardening (commit 9b00899): the closure adversarial review found a
latent underflow - at deltaTSample==0 (reachable only via SetRelativeSpeed below
~4.5%) the tail decremented its window countdown to -1 and went permanently
silent. Fixed by closing every window ending at the current cycle (a zero-cycle
window emits immediately and opens the next, mirroring ClockResample), with a
red-before-green lock ExtremeSlowWarp_KeepsEmitting_NoUnderflowStall. The normal
path stays bit-exact (equivalence + zero-alloc invariants re-verified).

## Slice CL - native-collection convention + program closure

- Commit a38964b (red convention test), e100433 (green: 6 collection attributes).
- `NativeCollectionConventionTests`: any xUnit test class that drives the native
  VICE bridge must declare `[Collection("NativeVice")]`. Exemptions are
  structural (the "is a test class" gate skips the LockstepValidator helper and
  the ViceMachineValidationFixture; the line-start attribute probe ignores
  XmlDocs' doc/regex-literal marker mentions) - no filename allow-list.
- RED listed exactly the 6 gap classes; GREEN after adding the attribute:
  RasterBarGroundTruthTests, RasterBarLockstepTests, RasterBarRendererTests,
  SidEngineClockingProbeTests, SidEngineParityTests, SnapshotResumeSpikeTests.
- Gate: NativeCollectionConventionTests 1/1 green.

## Adversarial closure verification

Before flipping the MCP program state, a 6-way adversarial verification fan-out
(independent skeptic agents over the working tree, not a re-run of the gates)
returned safeToClose=true with zero blockers:

- Zero pending SID ACs: CONFIRMED. The sole method-level pending ParityAc is
  TEST-VIC-FETCH-06 (VIC-II, out of SID scope). 284 SID ParityAc attributes, all
  pending:false.
- Manifest 466 pin: CONFIRMED. ExpectedMinCovered=466, ExpectedAcCount=466, with
  strict Assert.Equal(466, covered) and Assert.Equal(466, acs.Count); the yaml
  has exactly 466 AC entries (verified three ways).
- No masked SID skips: CONFIRMED. No SID ParityAc body is an unconditional skip;
  the only SID Assert.Skip sites are native-availability guards on plain [Fact]s.
- Live push/pull resample equivalence: CONFIRMED on source review; the flagged
  deltaTSample==0 underflow is now FIXED (commit 9b00899, above).
- Chamberlin removal: CONFIRMED, zero dangling references; SidFilter6581Tests.cs
  deleted.
- Crash attribution: CONFIRMED flaky. 0xC0000005 is an unmanaged-only fault; the
  CL diff is managed-only (6 attribute lines + one pure-managed convention test);
  the same binary re-ran clean. Not a CL regression, not a closure blocker.

## Program-closure gates

- Parity gate (`Category=Parity&Category!=ParityPending`): **466 passed, 0 failed,
  0 skipped**; completion pin `Assert.Equal(466, covered)` holds. 0 pending SID ACs.
- Determinism (`Category=Determinism`): 5 passed, 0 failed.
- XmlDocs convention: green (all new test docs carry FR/TR + Use case:/Acceptance:).

### Full baseline (`Category!=Determinism&Category!=AiReview`)

Criterion (per the deps-wave baseline convention): 0 NEW failures vs the 7
pre-existing VideoRendererTests, with stable skips. Total wobble of +-1 test is
the documented cosmetic trx-serialization noise, not a real test-count change.

- CL branch (e100433), run 1: crashed at 6m31s with a native ACCESS_VIOLATION
  (exit -1073741819) after 740 of ~2713 tests, at the RasterBar native-snapshot
  cluster. trx `baseline-cl.trx`.
- CL branch (e100433), run 2 (re-run): **2713 total, 7 failed, 11 skipped, no
  crash**. trx `baseline-cl2.trx`. The 7 failures are exactly the pre-existing
  VideoRendererTests (0 NEW).
- Pre-CL commit (4924b94): **2714 total, 7 failed, 11 skipped, no crash**. trx
  `baseline-precl.trx`. Same 7 known failures.

Attribution of the run-1 crash: FLAKY native-shim instability, not a regression.
Evidence: (a) the identical CL state re-ran clean; (b) the pre-CL state ran
clean; (c) the 3 RasterBar classes pass in isolation; (d) the CL change is
attribute-only ([Collection("NativeVice")] on 6 classes) plus one pure-managed
source-scan test, and the assembly already disables parallelization, so the
change reorders tests only - it executes no native code that could fault. The
crash is a candidate for a PLAN-NATIVERESIDUE-002 native-lifecycle follow-up
(sustained consecutive native create/resume/dispose under load), not a SID
parity blocker.

The 7 pre-existing VideoRendererTests failures are pure-managed VIC-II renderer
golden-image failures owned by the VIC audit on branch
`feat/vsf-lockstep-resid-rebaseline`; proven pre-existing on clean HEAD by the
stash-baseline method in earlier slices. They are out of SID scope.

## Net result

The SID parity program is complete: 466/466 ACs authored and passing, 0 pending
SID ACs, all SID subsystems (envelope, waveform, noise, sync/ring, filter 6581 +
8580, external filter, combined DAC, databus/POT, batched clock, sampling
Fast/Interpolate/Resample, live-audio wiring) lockstep-verified bit-exact vs the
native reSID oracle. Determinism preserved. The only manifest-wide pending AC is
TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock, out of SID scope, needs the parity
owner).
