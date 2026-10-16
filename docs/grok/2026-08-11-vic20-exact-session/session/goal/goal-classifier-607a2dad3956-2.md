# Goal verification — Achieved

0 of 3 skeptics refuted; survives the panel.

Per-skeptic reports: C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\goal-classifier-607a2dad3956-2-skeptic-0.md, C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\goal-classifier-607a2dad3956-2-skeptic-1.md, C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\goal-classifier-607a2dad3956-2-skeptic-2.md


---
## Inlined skeptic report: goal-classifier-607a2dad3956-2-skeptic-0.md

# Adversarial re-verification (round 2): Phase 2 VIC-20 lockstep

## Decision: **Not Refuted**

Every prior gap is confirmed fixed on the real create/step/peek path. No plan weakening. Gating criteria 1-4 hold with captured and spot-checked evidence.

---

## Prior gaps (round 1) vs current code

### 1. Dual-VIA samples — FIXED

- Test: `NativeXvic_Managed_DualVia_ControlRegs_Lockstep` at `tests/ViceSharp.TestHarness/Vic20/Vic20NativeLockstepTests.cs:101-123`
- After equal cycles (64/256/1024): peeks offsets 2,3,11,12,14 (DDRA/DDRB/ACR/PCR/IER) at `$9110` and `$9120` via `native.PeekBus` vs `managed.Bus.Peek` (`AssertViaControlRegsAgree` lines 224-235).
- Asserts dual `Via6522` devices still mapped.
- Spot-check: **3/3 passed**.

### 2. VIC-I register samples managed vs native — FIXED

- Tests: `NativeXvic_Managed_VicI_Regs_Lockstep` (64..5000) and `NativeXvic_VicRasterAdvances_AndMatchesManaged` (`Vic20NativeLockstepTests.cs:135-209`).
- Compares static peeks and VICE raster encoding on `$9003`/`$9004`.
- `Mos6561.Read` implements VICE encoding (`Mos6561.cs:113-118`); power-on zeros (`ResetRegisters` 168-174) so both sides start unprogrammed like xvic.
- Intermediate `vic20-via-vic-lockstep.log` showed honest reds (managed `$9000=$05` vs native `$00`) before the power-on fix; final logs green.
- Spot-check: **5/5 VicI/raster theories passed**.

### 3. No VIA state export (GetCiaState zeros) — FIXED via PeekBus

- Native: `vice_machine_peek_bus` → `via1_peek` / `via2_peek` / `vic_peek` (`native/vice-shim-vic20.c:649-669`).
- Managed: `IViceNative.PeekBus` (`IViceNative.cs:48`); xvic: `ViceNative.Xvic.cs:66-67, 325`.
- Lockstep tests use PeekBus, not CIA stubs.

---

## Full criteria (plan unchanged; PLAN_CHANGES none)

| Criterion | Status | Evidence |
|-----------|--------|----------|
| 1 READY / borders / $900F | MET | Boot proof 19/0; video tests in Vic20 filter |
| 2 Profiles / 1540 / open-bus / shared VIA | MET | Skeleton/memory/keyboard + open-bus bus fix |
| 3 Native lockstep CPU + dual-VIA + VIC-I samples | MET | CPU 5000 + DualVia + VicI peeks; real xvic path |
| 4 Vic20 0 fail/skip + C64 smoke | MET | gate 86/0/0; C64 smoke 31/0 |

Verification plan artifacts under implementer SCRATCH: `vic20-gate.log`, `vic20-xvic-lockstep.log` (24 passed), `vic20-boot-proof.log`, `vic20-c64-smoke.log`, `build.log`, `docs/receipts-vic20-phase2-verify-2026-08-05.txt`.

---

## Honesty notes (not refutes)

- VIA control samples in the first 1k cycles may still be mostly power-on defaults; the path is real (`via1_peek`/`via2_peek`) and would fail on wrong peeks (default native inactive path is `0xFF`).
- VIC-I static peeks stay zero until kernal video init (~500k); dynamic `$9003`/`$9004` agreement is the strong signal and is asserted through 5000 cycles.
- Residual mid-instruction PC lag after ~5005 and Tier A polish are documented non-gating residuals, not unmet plan criteria.

---

## Spot-checks this round

- `FullyQualifiedName~Vic20NativeLockstep` → **18 passed / 0 failed**
- DualVia + VicI + raster filter → **8 passed / 0 failed**
- Boot / C64 smoke / build receipts already green in SCRATCH


---
## Inlined skeptic report: goal-classifier-607a2dad3956-2-skeptic-1.md

# Skeptic 1 verdict: Not Refuted

## Prior gaps (must re-verify)

### 1. Dual-VIA samples after create+step — FIXED
- **Location:** `tests/ViceSharp.TestHarness/Vic20/Vic20NativeLockstepTests.cs:101-123`, helper `AssertViaControlRegsAgree` at 224-235
- **What shipped:** After equal `native.Step()` / `managed.Clock.Step()` budgets (64/256/1024), peeks at VIA1 `$9110` and VIA2 `$9120` offsets 2/3/11/12/14 (DDRA/DDRB/ACR/PCR/IER) compare `native.PeekBus` vs `managed.Bus.Peek`
- **Honesty:** Real production create path (`ViceNative.CreateInstance("vic20")` + `MachineTestFactory.CreateVic20Machine`), not a reimplementation; managed path hits `Via6522.Peek` through `BasicBus.Peek`

### 2. VIC-I managed vs native register samples — FIXED
- **Location:** same file lines 135-209
- **What shipped:** Static peeks plus `$9003`/`$9004` raster encoding lockstep through 5000 cycles; raster-advance test compares native PeekBus to managed after 200 steps
- **Honesty:** Uses real `Mos6561` on managed side; VICE encoding in `Mos6561.Read` (lines 113-118); power-on zeros in `ResetRegisters` (168-174) so lockstep is not pre-seeded READY geometry

### 3. Native VIA/VIC peek path (not CIA zeros) — FIXED
- **Location:** `native/vice-shim-vic20.c:649-662`, `IViceNative.PeekBus`, `ViceNative.Xvic.cs:325`
- **What shipped:** `vice_machine_peek_bus` dispatches `$9110-$911F` → `via1_peek`, `$9120-$912F` → `via2_peek`, `$9000-$900F` → `vic_peek`
- **Note:** `GetCiaState` still returns zeros (interface parity); criterion 3 is satisfied via PeekBus, which the prior gap allowed ("peek IO windows or GetVia")

## Plan acceptance criteria

| # | Result | Evidence |
|---|--------|----------|
| 1 Boot READY + borders | MET | `vic20-boot-proof.log` 19 passed; video/border tests in Vic20 filter |
| 2 Hosts / 1540 / expansion | MET | Vic20 skeleton + memory map tests in green Vic20 filter |
| 3 Native lockstep CPU+VIA+VIC | MET | CPU 5000-cycle A/X/Y/S/P/PC; dual VIA; VIC-I; oracle `native/vice_xvic.dll` present (~10.9MB) |
| 4 Vic20 filter + C64 smoke | MET | gate 86/0/0 (final rebuild 87/0/0); c64 smoke 31; build 0 errors |

## Captured evidence (implementer)

- `vic20-gate.log`: 86 passed / 0 failed / 0 skipped
- `vic20-xvic-lockstep.log`: 24 passed (CPU + dual VIA + VIC-I suite)
- `vic20-via-vic-lockstep-final.log`: full Vic20 filter 87 passed after VIA/VIC work
- `vic20-c64-smoke.log`: 31 passed
- `vic20-boot-proof.log`: 19 passed
- `build.log`: 0 errors
- `docs/receipts-vic20-phase2-verify-2026-08-05.txt` matches summary

Mid-development `vic20-via-vic-lockstep.log` shows earlier VIC-I failures (managed `$9000=$05` vs native `$00`); those are superseded by power-on zeros + final green log and skeptic spot-check.

## Skeptic spot-check

```
dotnet test ... --filter "FullyQualifiedName~NativeXvic_Managed_DualVia|...VicI|...VicRaster"
→ Passed: 8, Failed: 0
```

## Residual (not grounds to refute)

- Intermittent PC lag after ~cycle 5005: plan only requires bounded multi-cycle depth; 5000-cycle gate is green
- Tier A polish (expansion UX, input E2E, cart/disk, snapshots): plan non-goals / only required if Vic20 filter fails
- Soft early-`return` when oracle absent: oracle is present in this environment; tests actually exercised native create/step (not silent pass)

## Conclusion

All three prior skeptic gaps are genuinely fixed with honest create/step/peek tests against the real native oracle. Gating criteria 1–4 hold on current workspace + implementer receipts. **Not Refuted.**


---
## Inlined skeptic report: goal-classifier-607a2dad3956-2-skeptic-2.md

# Skeptic-2 verification: Phase 2 VIC-20 lockstep

## Verdict: Not Refuted

Confidence: **high**. Blocking: **none**.

## Prior gaps (must close first)

### Gap 1: dual-VIA managed vs native samples
**Status: FIXED**

- Test: `NativeXvic_Managed_DualVia_ControlRegs_Lockstep` in
  `tests/ViceSharp.TestHarness/Vic20/Vic20NativeLockstepTests.cs:101-123`
- Path: `ViceNative.CreateInstance("vic20")` + `MachineTestFactory.CreateVic20Machine`
  + equal `Step` / `Clock.Step`, then peeks at $9110/$9120 offsets 2,3,11,12,14
  (`AssertViaControlRegsAgree` lines 224-235).
- Native: `IViceNative.PeekBus` -> `vice_machine_peek_bus` -> `via1_peek` / `via2_peek`
  (`native/vice-shim-vic20.c:649-665`).
- Managed: `managed.Bus.Peek` -> `Via6522.Peek`.
- Evidence: implementer `vic20-via-vic-lockstep.log` shows DualVia 64/256/1024 Passed;
  skeptic spotcheck same three cases Passed.

### Gap 2: VIC-I managed vs native (not native-only raster)
**Status: FIXED**

- Tests: `NativeXvic_Managed_VicI_Regs_Lockstep` (64..5000) and
  `NativeXvic_VicRasterAdvances_AndMatchesManaged`
  (`Vic20NativeLockstepTests.cs:135-209`).
- Compares static $9000 peeks and VICE raster encoding ($9004 = line>>1; $9003 bit7 =
  line bit0) on both sides after step.
- Intermediate implementer log (`vic20-via-vic-lockstep.log` 2:54) failed with
  managed $9000=$05 vs native $00 (pre-seeded READY defaults). Current
  `Mos6561.ResetRegisters` clears to power-on zeros (`Mos6561.cs:168-174`).
- Final implementer logs + skeptic re-run: VicI + raster tests Passed.

### Gap 3: VIA export (not CIA zeros only)
**Status: FIXED**

- Prior note on `GetCiaState` zeroing VIA still true for interface parity
  (`ViceNative.Xvic.cs:360-380`), but criterion and prior gap allowed
  "peek IO windows or GetVia".
- Shipped: `PeekBus` / `vice_machine_peek_bus` with real VIA/VIC peeks.
- Lockstep tests use that path exclusively for dual-VIA and VIC-I samples.

## Plan acceptance criteria

### 1. READY + border / $900F
**MET.** `Vic20BootProofTests.Vic20_Boot_Reaches_Ready_Prompt`; video tests for
border strips and $900F split. Capture: `vic20-boot-proof.log` 19 passed.

### 2. Hosts, profiles, 1540, expansion path
**MET.** Heads/skeleton/drive-default tests cover `vic20` / `vic20ntsc`, unit-8
1540, expansion/cart map. Covered under Vic20 filter gate.

### 3. Native lockstep (CPU + dual-VIA + VIC-I)
**MET.**
- CPU A/X/Y/S/P/PC through 5000 cycles PAL and NTSC
  (`NativeXvic_Managed_CpuRegs_Lockstep`).
- Dual VIA control peeks at 64/256/1024.
- VIC-I static + raster peeks through 5000.
- Real create/step/compare; oracle `native/vice_xvic.dll` present (spotchecked).
- Logs show `xvic vice_machine_create` / `step_cycle` (not silent early-return theater
  in this environment).

### 4. Vic20 filter + C64 smoke
**MET.**
- `vic20-gate.log`: Failed 0, Passed 86, Skipped 0
- `vic20-xvic-lockstep.log`: Failed 0, Passed 24
- `vic20-c64-smoke.log`: Failed 0, Passed 31
- `build.log`: 0 errors

## Skeptic spot-check (cheap)

Filter DualVia | VicI | VicRaster | CpuRegs_Lockstep, Release, no-build:

- Total tests: 16, Passed: 16, Failed: 0
- Includes DualVia 64/256/1024, VicI 64/256/1024/5000, VicRasterAdvances, CPU multi-depth
- Log: `C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\skeptic-2\via-vici-spotcheck.log`

## Honesty notes (not refutes)

- Oracle-absent path uses bare `return` (pass) rather than xUnit Skip. When oracle is
  missing this would inflate pass counts; here oracle is present and logs prove native
  create/step ran. Criterion 3/4 satisfied for this environment.
- Dual-VIA samples are control registers (often reset-stable early). Still real peeks
  on both chips; IER managed encoding includes bit 7. Meets prior gap + criterion 3
  wording ("register samples").
- Residual mid-instruction PC lag after ~5005 and Tier A polish items are documented
  residuals / non-goals for this gate; criterion 3 only requires bounded multi-cycle depth.

## Findings

None that refute. Prior gaps closed; gating criteria corroborated by implementer
receipts and skeptic re-run.


---
## Inlined skeptic report: goal-classifier-607a2dad3956-1-skeptic-0.md

# Adversarial verification: Phase 2 VIC-20 / native lockstep

## Decision: **Refuted**

CPU multi-cycle native lockstep is real and green. Plan **acceptance criterion 3** still requires dual-VIA and VIC-I register sample agreement; that part is missing from shipped tests and native export.

---

## Plan criteria

| # | Status | Notes |
|---|--------|--------|
| 1 Boot / READY / border / $900F | MET | `Vic20BootProofTests`, `Vic20VideoTests`; boot-proof log 19/0 |
| 2 Profiles / 1540 / expansion open-bus / shared VIA | MET | Vic20 filter includes skeleton, memory, keyboard dual-VIA; open-bus in `BasicBus` / `Vic20SystemRam` |
| 3 **Native lockstep CPU + dual-VIA + VIC-I samples** | **UNMET** | CPU only |
| 4 Vic20 filter 0 fail/skip + C64 smoke | MET | 80/0/0 Vic20; 31/0 C64 smoke (captured + re-run) |

---

## Criterion 3 gap (blocking this goal)

Plan text (verbatim intent):

> agree on CPU A/X/Y/S/P/PC **and on dual-VIA plus basic VIC-I register samples**; tests drive the real create/step/compare path

### What is proven

- `ViceNative.CreateInstance("vic20"|"vic20ntsc")` + managed `MachineTestFactory.CreateVic20Machine` + per-cycle step.
- After 64 / 256 / 1024 / 4096 / **5000** cycles: A/X/Y/S/P/PC match (PAL + NTSC theories).
- Captured: `{implementer}/vic20-xvic-lockstep.log` → **17 passed / 0 failed**.
- Spot-check re-run: same 17/0 and full `~Vic20` **80/0/0**.
- Open-bus fix is real in `BasicBus.cs` / `Vic20SystemRam.cs` (not theater).

### What is not proven

1. **Dual VIA samples**  
   - `Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep` only asserts CPU regs (`tests/.../Vic20NativeLockstepTests.cs:54-56`).  
   - No managed-vs-native compare of VIA at `$9110` / `$9120` (ports, DDR, timers, IFR/IER, etc.).  
   - `ViceNativeXvic.NativeMachine.GetCiaState` **zeros** and documents that Vic20 has VIA not CIA (`ViceNative.Xvic.cs:354-357`). No alternate VIA export used by tests.

2. **Basic VIC-I register samples**  
   - `NativeXvic_VicRasterAdvances` only checks that **native** `GetVicState` cycle/raster move (`Vic20NativeLockstepTests.cs:89-105`).  
   - Does **not** compare managed `Mos6561` registers or raster phase to native `registers` / raster fields.  
   - Shim **can** export VIC-I regs (`vice-shim-vic20.c:1056-1078`); tests never lockstep them.

3. Scaffold path `NativeXvic_ManagedLockstep_CpuRegs_WhenOraclePresent` also omits P and all chip samples (CPU-only partial).

### Not used as refute (intentionally)

- End-state vs every-cycle for the 5k theories: plan allows bounded multi-cycle depth; 64-cycle per-step P test covers the open-bus CMP window.
- Residual PC lag after ~5005: outside the claimed 5k bound; plan non-goals allow non-perfect multi-frame.
- Tier A polish (expansion UX, input E2E, cart/disk, snapshots): plan residual / non-blocking for criterion 3, not invented as new gates.
- Stale XML on the lockstep class still mentioning P.C diverge: docs debt, not a failed assertion (tests assert full P).
- Silent `return` when oracle absent: oracle was present for the green capture; criterion 3 caveat about missing oracle does not apply here.

---

## Actionable fix for implementer

1. After equal create/reset/step budgets, sample dual VIA (e.g. peek `$9110`–`$911F` and `$9120`–`$912F`, or a real `GetViaState` export) on native and managed; assert agreement on a small fixed set of regs.
2. Sample basic VIC-I: native `GetVicState().Registers` (and optionally raster line/cycle) vs managed VIC-I register file / raster phase.
3. Keep driving real `ViceNative` + managed factory (no reimplementation).
4. Re-run verification plan steps 1–2; leave named logs under implementer SCRATCH proving VIA+VIC sample match, not only CPU.

Until those assertions are honest and green, **do not claim Phase 2 / criterion 3 lockstep complete**.

---

## Evidence citations

- Plan: acceptance criterion 3 (dual-VIA + VIC-I samples).
- Tests: `F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs`.
- Zero VIA export: `F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs:354-374`.
- VIC-I export exists but unused for lockstep compare: `native/vice-shim-vic20.c:1056-1078`.
- Green CPU-only capture: `C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-xvic-lockstep.log` (17 passed).
- Vic20 filter: `implementer\vic20-gate.log` + re-run 80/0/0.
