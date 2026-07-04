# S9 reSID 6581 filter - bit-exact remediation (CHECKPOINT, pre-reboot)

Branch: `feat/vsf-lockstep-resid-rebaseline`. Date: 2026-07-04.
Status: **IN PROGRESS - S9 NOT committable (filter not bit-exact).** All work is
uncommitted in the working tree (survives reboot). Do NOT commit S9 until the
filter-tap path is bit-exact and the 5 broken tests are green.

## What happened

The S9 background worker reported "clean" but its own gates never oracle-compared
the filter OUTPUT and never ran the broad SID regression sweep. Independent verify:

- PASS: S9 focused 57 pass / 4 skip (correctly-deferred pending) / 0 fail.
- PASS: ParityCoverageManifest + XmlDocs + SidDeterminism = 10/0/0.
- PASS: distinct ParityAc = ratchet `ExpectedMinCovered` = 405.
- The worker's "13 VIC failures" were a miscount: `VicCycleDivergentParity` in
  isolation is 16 pass + 1 known-`pending` FETCH-06 (excluded from gate). No VIC regression.
- **FAIL: broad SID regression sweep = 5 failures** the worker never ran:
  - FAITHFUL locks: `PULSE-07` (SidWaveformFaithfulParity), `RING-04`.
  - DIVERGENT: `ENV-50`, `CLOCK-06`, `CLOCK-07` (SidEnvDivergentParity).
  - Cause: S9 replaced the bypass filter with the real reSID op-amp model, changing
    `GenerateSample`/`CycleFilterOutput` scale. The 5 tests' hand-mirrored expected
    values encode the pre-S9 bypass model.

## The real problem (oracle probe results)

None of the 57 new S9 tests compare filter output to the reSID oracle (`SidExactOutput`);
they are all structural (constants + `Assert.InRange`). Bit-exactness was never proven.
A throwaway oracle probe (managed vs `SidExactOutput`, cycle-locked, since deleted) showed:

- DC / plateau (no filter routing): **bit-exact (max Δ0 over 2000 cyc)**.
- Direct voice path (no filter routing): near-exact, **max Δ7, mean 2.15** (NOT 0).
- **LP filter tap: broken - mean Δ1031, max 2707, 2/20000 exact.**
- BP filter tap: broken - mean Δ63, max 807.

Signature = correct integrator EQUILIBRIUM (DC) but wrong RATE/transient (dynamic),
LP worst (double-integrated), BP less, direct/DC none. Points to a table-build
discrepancy (spline / `SolveGainD` Newton-Raphson / vcr tables) or a per-cycle
integrator-rate issue that only manifests when a voice routes THROUGH the filter.
NOTE: "DC exact" used scenario 1 which had NO filter routing, so it does NOT validate
the integrator tables - the integrator is effectively unverified.

Inspection so far (all read as faithful ports, bug NOT yet found by reading):
- `solve_integrate_6581`, `ExternalFilter::clock`/`output`, integrator ORDER
  (Vlp-from-old-Vbp then Vbp-from-old-Vhp) all match reSID filter8580new.h /extfilt.h.
- `SetW0_6581` IS called on $D415/$D416; `_filterCutoff` is proper 11-bit `(hi<<3)|(lo&7)`.
- `output()` single-tap mixer-offset structure matches reSID (multi-tap counts all taps
  as ONE mixer input - managed handles single-tap correctly; verify multi-tap later).
- vcr_kVg / vcr_n_Ids_term table-build formulas match filter8580new.cc.

Decision (user, AskUserQuestion 2026-07-04): **Fix to bit-exact now.** Extend shim to
export oracle filter integrators, oracle-TDD the tap path, fix direct-path residual too.

## Native shim probe ADDED (uncommitted) - to pinpoint the stage

Exports reSID internal filter state through the existing `vice_sid_exact_get_state`:
- `native/vice/vice/src/resid/filter8580new.h`: public `Filter::vsharp_probe(int* out)`
  -> [0]Vlp [1]Vbp [2]Vhp [3]v1 [4]v2 [5]v3 [6]sum [7]mix [8]output() [9]Vlp_x
  [10]Vlp_vc [11]Vbp_x [12]Vbp_vc.
- `native/vice/vice/src/sid/resid.cc`: `resid_shim_filter_probe(sound_t*, int*)`.
- `native/vice-shim.c`: extern decl + call in `vice_sid_exact_get_state`.
- `native/vice-shim.h`: `int32_t filter_probe[13];` appended to `vice_sid_exact_state`.
- `src/ViceSharp.Core/ViceNative.cs`: `fixed int FilterProbe[13]` + `GetFilterProbe()`.

Native link FAILED only because `resid.o`/`libsid.a` were stale and never recompiled my
`resid.cc` edit (make said "Nothing to be done"). They were deleted; rebuild needed.

## BUILD RECIPE (hard-won; per-shell, lost on reboot) - use the Bash tool

```bash
# 1. Toolchain + native temp dir wrappers (gcc.exe cannot use POSIX /tmp -> falls to
#    C:\WINDOWS which is unwritable; wrappers force a native TMPDIR).
export PATH="/f/GitHub/vice-sharp/native/.ccwrap:/c/msys64/mingw64/bin:/c/msys64/usr/bin:$PATH"
export TMPDIR="C:/Users/kingd/AppData/Local/Temp" TMP="$TMPDIR" TEMP="$TMPDIR"
#    .ccwrap/{cc,c++,gcc,g++} already exist (untracked) and re-export TMPDIR then exec
#    /c/msys64/mingw64/bin/<tool>.

# 2. Force reSID rebuild so resid_shim_filter_probe links:
cd /f/GitHub/vice-sharp/native/vice/vice/src
rm -f sid/resid.o sid/libsid.a
make -C sid libsid.a          # <-- this was the interrupted step
nm sid/resid.o | grep filter_probe   # must show resid_shim_filter_probe

# 3. Relink the shim DLL (patch gate bypassed: the runtime patch is already applied and
#    my resid.cc edits break its reverse-check; .build-nopatch.sh = build-vice-shim.sh
#    with the patch block (lines 26-36) removed).
cd /f/GitHub/vice-sharp/native
bash .build-nopatch.sh        # relinks vice_x64.dll + copies deps
```

## NEXT STEPS after reboot

1. Run the build recipe above; confirm `vice_x64.dll` relinked with the probe symbol.
2. `dotnet build tests/ViceSharp.TestHarness -c Release` (single-node flags:
   `-m:1 -nodeReuse:false -p:UseSharedCompilation=false`, `VICESHARP_ROM_PATH` set).
3. Write an oracle probe [ViceFact]: same register program on managed Sid6581 and the
   oracle; compare managed `FilterVlp/FilterVbp/FilterVhp`, `_rv1/2/3` (ResidPrescaledV*),
   `CycleFilterOutput` vs `GetFilterProbe()` [0..8]. First divergent index = the bug's stage.
4. Fix the filter until every scenario (direct + LP + BP + notch + resonance) is Δ0 vs
   `SidExactOutput`. Do NOT weaken tests or add tolerance.
5. Rebaseline the 5 broken tests to oracle-derived values (they now diverge because the
   scale/model changed; the new expected must come FROM reSID, verified == oracle).
6. Keep these oracle-comparison tests as PERMANENT locks (fills the worker's gap).
7. Gates green: broad SID sweep, S9 focused, manifest+xmldocs+determinism, ratchet=405,
   `git diff --check`. Then commit S9 + shim probe together.

## Uncommitted files (all intended, keep)
Main repo: src/ViceSharp.Chips/Audio/Sid6581.cs, Sid6581.Filter.cs (new), Sid8580.cs,
src/ViceSharp.Core/ViceNative.cs, native/vice-shim.c, native/vice-shim.h,
tests/.../SidFilter6581DivergentParityTests.cs (new), SidVoiceMixDivergentParityTests.cs,
Sid6581NonLinearCutoffTests.cs, SidFilter6581Tests.cs (retired SVF), ParityCoverageManifestTests.cs.
native/vice repo: filter8580new.h, resid.cc (+ pre-existing patched c64cpusc.c, mainc64cpu.c).
Untracked helpers (keep): native/.ccwrap/, native/.build-nopatch.sh.
NOT mine (leave): CLAUDE.md, vice-snapshot-20260630171307.vsf.
