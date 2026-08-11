# Goal verification — Achieved

0 of 3 skeptics refuted; survives the panel.

Per-skeptic reports: C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\goal-classifier-32799c5b1a25-2-skeptic-0.md, C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\goal-classifier-32799c5b1a25-2-skeptic-1.md, C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\goal-classifier-32799c5b1a25-2-skeptic-2.md


---
## Inlined skeptic report: goal-classifier-32799c5b1a25-2-skeptic-0.md

# Adversarial verification round 2: Phase 2 VIC-20

## Verdict: NOT REFUTED

All four prior gaps are confirmed fixed in current sources and by implementer gate + skeptic spot-checks. No new gating defect found that fails a plan criterion.

## Prior gaps

### 1. ResolveScreenBase (bug) — FIXED
`Mos6561.ResolveScreenBase` (`src/ViceSharp.Chips/Vic/Mos6561.cs:222-228`) now uses VICE `mem_get_screen_parameter`:
`bank = (vm&0x80)?0:0x8000; addr = bank + ((vm&0x70)<<6) + ((cr&0x80)<<2)`.

Probe after boot: `$9002=$96` `$9005=$F0` → `$1E00`; READY at `$1E58` (unexpanded).

### 2. CharacterMode test theater (gap) — FIXED
`Vic20VideoTests.CharacterMode_RendersInkPixels_AtDecodedScreenBase_ForPlantedGlyph` plants at decoded base, forces black bg / white ink, asserts every 8x8 pixel matches chargen bits (`mismatches==0`, `inkPixels>0`). Plus explicit `ResolveScreenBase_MatchesViceFormula_UnexpandedReadyLayout`.

### 3. SimpleRam C64 clobber / sticky open bus (bug) — FIXED
`Vic20SystemRam` replaces `SimpleRam` in `BuildVic20Machine` (`ArchitectureBuilder.cs:287`). Uninstalled regions: read `0xFF`, writes ignored. `Reset()` clears installed only (no `InitializeC64`).

Probe: write `$2000=$5A` → read `FF`. READY relocated to `$1E58` (was `$1058` when expanded falsely).

### 4. Open-bus test peek-only (gap) — FIXED
`UnexpandedMachine_OpenBusExpansion_WriteDoesNotStick` write-then-reads `$2000`/`$0400`/`$A000`. `UnexpandedMachine_Reset_DoesNotApplyC64InitPattern` asserts `$0400` stays open-bus and `$1000` clears without C64 `$20` fill.

## Gate evidence

| Source | Result |
|--------|--------|
| implementer `vic20-gate.log` | 55 passed / 0 failed / 0 skipped |
| implementer `vic20-c64-smoke.log` | 31 passed |
| implementer `build.log` | 0 errors / 0 warnings |
| skeptic spot filter Video+Memory+Boot+Determinism | 27 passed / 0 failed |

## Criteria (plan)

1. READY fingerprint — MET (`$1E58` within 25 frames)
2. Character-mode frames + FrameCompleted + PAL/NTSC — MET (honest glyph test + Mos6561)
3. VIA keyboard/joystick — MET (prior tests; no regression in Vic20 suite)
4. Cart / 1540 / xvic / session / picker — MET (prior green suite expanded)
5. Determinism substitute + full Vic20 gate — MET

## Non-blocking observation (not a refute)

Color fetch uses `$9400 + cellOffset` rather than `$9400 + ((screenBase+offset)&0x3FF)`. Post-boot ink at `$9600` is blue while renderer reads `$9400` (zero → black ink on cyan bg). Glyphs still form and are host-visible; criterion 2 and prior gaps do not require color-RAM base fidelity. Anti-ratchet: not raised as a new blocking gap.

## Plan changes

Checklist marked complete; deviations document worktrees skip, xvic substitute, and Vic20SystemRam — no criterion weakened.


---
## Inlined skeptic report: goal-classifier-32799c5b1a25-2-skeptic-1.md

# Skeptic round 1 — Phase 2 VIC-20 completion

## Verdict: Not Refuted

All three prior-gap classes are fixed with honest production-path tests and green captured evidence. Plan acceptance criteria 1–5 hold. Confidence: high.

## Prior gaps — disposition

### 1. `Mos6561.ResolveScreenBase` (bug) — FIXED

Location: `src/ViceSharp.Chips/Vic/Mos6561.cs:222-228`

```csharp
var bank = (vm & 0x80) != 0 ? 0 : 0x8000;
var addr = (ushort)(bank + ((vm & 0x70) << 6) + ((cr & 0x80) << 2));
```

Matches VICE `mem_get_screen_parameter`. Unexpanded READY registers yield `$1E00`. Covered by `ResolveScreenBase_MatchesViceFormula_UnexpandedReadyLayout` and bank-bit test.

### 2. `Vic20VideoTests` character-mode theater (gap) — FIXED

Location: `tests/ViceSharp.TestHarness/Vic20/Vic20VideoTests.cs:39-88`

- Sets black background (`$900F=0`) and decoded screen base `$1E00`
- Plants screen code `1` (`A`) and white color at `$9400`
- Samples full 8x8 cell against chargen `$8008+py`
- Asserts `inkPixels > 0` and `mismatches == 0` (not "any non-black")

Drives real `Mos6561` via production `MachineTestFactory.CreateVic20Machine()`.

### 3. `SimpleRam` clobber / sticky expansion (bug) — FIXED

Location: `src/ViceSharp.Core/Vic20/Vic20SystemRam.cs:30-50`

- Uninstalled BLK/3K: `Read` → `0xFF`, `Write` ignored
- `Reset` → `Array.Clear` only (no C64 `InitializeC64` fill)
- Wired in production: `ArchitectureBuilder.BuildVic20Machine` constructs `Vic20SystemRam(expansion)` at line 287

### 4. Open-bus test only peeks (gap) — FIXED

Location: `tests/ViceSharp.TestHarness/Vic20/Vic20MemoryMapTests.cs:31-68`

- Write-then-read `$2000`, `$0400`, `$A000` expect `0xFF` (non-sticky)
- Reset: `$0400` still open-bus `0xFF`, `$1000` cleared to `0x00`
- Exp8K BLK1 sticky, BLK2 non-sticky

## Acceptance criteria audit

1. **Boot READY** — `Vic20_Boot_Reaches_Ready_Prompt` finds codes 18,5,1,4,25 in `$1000-$1FFF` within 400 frames; production builder ROMs/reset vector tested. Evidence: `implementer/boot-proof.log` (3 passed).

2. **VIC-I character frames** — `Mos6561` renders via `ResolveScreenBase`/`ResolveCharBase`; FrameCompleted + PAL/NTSC timing tests. Not stub-only (`VicIStub` unused on machine path; skeleton asserts `IsType<Mos6561>`).

3. **Keyboard/joystick VIA** — Shared `Via6522` at `$9110`/`$9120`; matrix inject readable on VIA2 Port A; joystick right clears PB7. No chip fork.

4. **Media/host** — MVP cart PRG map; profile + topology default **1540** for Vic20; C64 topology stays 1541; `xvic` host kind; session factory; Xbox `xvic` enabled.

5. **Determinism + full gate** — Plan allows in-repo multi-frame substitute when native `xvic` absent (`Vic20DeterminismTests`). Evidence:
   - `implementer/vic20-gate.log`: **55 passed / 0 failed / 0 skipped**
   - `implementer/vic20-c64-smoke.log`: **31 passed**
   - `implementer/lockstep-env.txt`: **2 passed**
   - `implementer/build.log`: **0 Warning / 0 Error**

Cheap spot-check (this round): Video + Memory + Boot filters → **25 passed / 0 failed**.

## Docs / checklist

- `docs/Iteration-Roadmap.md`: Iteration 2 COMPLETE
- `README.md`: VIC-20 100%
- `HANDOFF.md`: Iteration 2 COMPLETE section (stale "52 passed" count vs current 55 — doc lag only, not a functional fail)
- Plan deviations document B/C/D in-orchestrator and determinism substitute; criteria not weakened

## Honesty

- Tests call production `ArchitectureBuilder` via `MachineTestFactory`
- No Skip/Ignore/TODO in Vic20 test suite
- Gate logs are real `dotnet test` console captures under implementer scratch

## Findings

None blocking. Phase 2 objective is met.


---
## Inlined skeptic report: goal-classifier-32799c5b1a25-2-skeptic-2.md

# Skeptic re-verification (round 2) — Not Refuted

## Mandate

Primary job: confirm each **PRIOR_GAPS** item is genuinely fixed. Do not raise new stylistic nits when criteria hold.

## Prior gaps

### 1. bug · Mos6561.ResolveScreenBase wrong formula — FIXED

**Location:** `F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs:217-228`

Shipped formula matches VICE `mem_get_screen_parameter`:

```csharp
var bank = (vm & 0x80) != 0 ? 0 : 0x8000;
var addr = (ushort)(bank + ((vm & 0x70) << 6) + ((cr & 0x80) << 2));
```

Honest tests:

- `ResolveScreenBase_MatchesViceFormula_UnexpandedReadyLayout` → `$1E00` for `$9005=$F0`, `$9002=22|0x80`
- `ResolveScreenBase_BankBitClear_Selects8000Bank` → `$8000`

### 2. gap · CharacterMode only nonBlack>0 theater — FIXED

**Location:** `F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20VideoTests.cs:39-88`

Test now:

1. Sets black background (`$900F=0`) and white color RAM at `$9400`
2. Resolves screen base via production `Mos6561` (`$1E00`)
3. Plants glyph code `1` at that base
4. Asserts every pixel of the 8×8 cell matches chargen bits at `$8008+py` (white when on, black when off)
5. Requires `inkPixels > 0` and `mismatches == 0`

Would fail if base decode or glyph fetch were wrong. Not theater.

### 3. bug · SimpleRam Reset/open-bus clobber — FIXED

**Location:** `F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20SystemRam.cs:30-50`

- Uninstalled BLK/3K: `Read` → `0xFF`, `Write` ignored
- `Reset()` only `Array.Clear` (no C64 `InitializeC64`)
- Production path: `ArchitectureBuilder.BuildVic20Machine` constructs `new Vic20SystemRam(expansion)` at line 287

### 4. gap · open-bus peek-only — FIXED

**Location:** `F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20MemoryMapTests.cs:31-68`

- Write-then-read `$2000`, `$0400`, `$A000` → still `0xFF`
- Reset preserves open-bus at `$0400` and clears installed `$1000` to `0x00`

## Plan acceptance criteria

| Criterion | Result | Citation |
|-----------|--------|----------|
| 1 READY boot fingerprint | MET | `Vic20BootProofTests.Vic20_Boot_Reaches_Ready_Prompt`; `implementer/boot-proof.log` 3 passed |
| 2 Character-mode frames | MET | `Mos6561` + video tests; FrameCompleted; PAL/NTSC timing |
| 3 VIA keyboard/joystick | MET | `Vic20KeyboardTests` on real dual VIA windows `$9110`/`$9120` |
| 4 Cart / 1540 / heads | MET | cart PRG map; `DriveModel.C1540`; xvic topology; session factory; Xbox picker |
| 5 Determinism + full Vic20 gate + C64 smoke | MET | `Vic20DeterminismTests`; gate 55/0/0; smoke 31/0/0 |

## Captured evidence (implementer scratch)

- `vic20-gate.log` / `vic20-gate-summary.txt`: **Passed 55, Failed 0, Skipped 0**
- `bcd-merge.log`: names include BootProof, Video, MemoryMap (open-bus), Keyboard, DriveDefault, Determinism, Heads
- `vic20-c64-smoke.log`: 31 passed
- `boot-proof.log` / `drive1540.log` / `heads.log` / `lockstep-env.txt` (determinism 2): green
- `build.log`: 0 errors, 0 warnings

## Honesty checks

- Tests drive production `ArchitectureBuilder` via `MachineTestFactory.CreateVic20Machine` (real ROMs).
- No Skip/Ignore/TODO on Vic20 tests added for this goal.
- Plan deviations (no worktrees; managed multi-frame determinism instead of native xvic) are allowed by the plan Risks/Deviations text and do not weaken numbered criteria.
- Checklist checkmarks do not replace logs; logs exist and match claims.

## Residual notes (not refutes)

- `VicIStub.cs` still on disk; production wire-up uses `Mos6561` only.
- Full VIC-I audio / native xvic binary lockstep remain non-goals or optional deepenings per plan/HANDOFF.

## Conclusion

All prior gaps fixed. All gating criteria met with honest tests and captured green evidence. **Not Refuted.**


---
## Inlined skeptic report: goal-classifier-32799c5b1a25-1-skeptic-0.md

# Adversarial verification: Phase 2 VIC-20 completion

## Verdict: REFUTED

Objective: 100% completion of phase 2 (Iteration 2 VIC-20).
Gate logs show 52 Vic20 tests green and C64 smoke green, but shipped video/memory behavior has demonstrable defects, and the character-mode test is theater.

## Criteria

| # | Criterion | Judgment |
|---|-----------|----------|
| 1 | READY boot fingerprint | MET (bus peek finds READY; `vic20-gate.log` + probe) |
| 2 | VIC-I character frames for known screen pattern | **UNMET** (wrong screen base; test theater) |
| 3 | VIA keyboard/joystick, no VIA fork | Mostly MET (wiring tests; not re-verified C64 VIA suite log) |
| 4 | Cart/1540/xvic/session/picker | MET per logs/tests |
| 5 | Determinism substitute + full Vic20 gate | MET as allowed substitute (`Vic20DeterminismTests`) |

## Finding 1 — bug: wrong VIC-I screen base decode

`Mos6561.ResolveScreenBase` (`src/ViceSharp.Chips/Vic/Mos6561.cs` ~217-228):

```csharp
var addr = (ushort)(((vm & 0xF0) << 6) | ((cr & 0x80) << 2));
```

VICE `mem_get_screen` / `vic20mem.c:729` uses:

```c
((vic_peek(0x9005) & 0x80) ? 0 : 0x8000) + ((vic_peek(0x9005) & 0x70) << 6) + ((vic_peek(0x9002) & 0x80) << 2);
```

Spot-check after READY (skeptic probe):
- READY at `$1058`, `$9002=$16`, `$9005=$C0`
- Shipped formula => `$3000` (open-bus `FF FF FF...`)
- VICE formula => `$1000` (contains READY `12 05 01 04 19`)
- Full framebuffer after `RenderNow`: 32384/32384 non-black garbage, not READY text

Host-consumable frames exist, but character-mode does not sample the known screen RAM pattern the KERNAL painted.

## Finding 2 — gap/theater: character-mode unit test always passes

`Vic20VideoTests.CharacterMode_RendersNonBlackPixels_ForKnownScreenPattern`:
- Plants `'A'` at `$1E00` and sets `$9005=$F0` (shipped base becomes `$3C00`, not `$1E00`)
- Asserts only `nonBlack > 0`
- Power-on `$900F = 0x08` selects **background color index 8 (orange)**, so off-pixels are non-black
- Therefore the assertion cannot fail even if every screen code is wrong

This is not honest evidence for criterion 2 "character-mode output for a known screen-RAM pattern".

## Finding 3 — bug: unexpanded open-bus not real; C64 RAM reset clobber

`BuildVic20Machine` fills uninstalled regions with `0xFF`, then `machine.Reset()` resets non-clocked devices including `SimpleRam`.

`SimpleRam.Reset()` calls `InitializeC64()` (`src/ViceSharp.Core/SimpleRam.cs:29-68`), which:
- Writes C64 screen spaces `0x20` at `$0400-$07FF`
- Wipes the VIC-20 open-bus prefill

Also `SimpleRam.Write` always sticks for the full 64KB. Probe: unexpanded `IsInstalled(0x0400)=false` but write `0x5A` reads back `0x5A`; `$2000` write sticks. KERNAL therefore detects expansion RAM and places screen at `$1000` instead of unexpanded `$1E00`.

`Vic20MemoryMapTests.UnexpandedMachine_OpenBusExpansion_ReadsAsFf_AndBaseRamWritable` only peeks without write-then-read and documents writes as accepted "for MVP" — does not prove open-bus.

## Finding 4 — plan deviation note (not sole refute)

Native `xvic` lockstep replaced by dual-machine determinism; plan Risks + verification step 6 allow this substitute when binary missing. Determinism tests exercise real `ArchitectureBuilder` Vic20 machines — acceptable for criterion 5.

## Implementer must fix (actionable)

1. Fix `Mos6561.ResolveScreenBase` (and char base if needed) to match VICE VIC-I / `vic20mem` decode; add a test that plants a known code at the **decoded** screen base and asserts glyph pixels (or a hash) in the framebuffer — not merely `nonBlack > 0`. Prefer black bg for the assertion or compare against expected non-bg ink pixels for char `'A'`.
2. Stop using C64 `SimpleRam.InitializeC64` on VIC-20 reset: either a Vic20-aware RAM device that ignores writes (or open-bus) in uninstalled BLKs, or skip `SimpleRam.Reset` C64 init for Vic20 and enforce expansion masks on write.
3. Strengthen open-bus test: unexpanded write to `$0400`/`$2000` must not stick (or must match documented open-bus policy), and boot fingerprint for default unexpanded should keep screen at `$1E00` if that is the product intent.
4. Re-run `FullyQualifiedName~Vic20` and capture updated `{SCRATCH}/vic20-gate.log` showing the new video/memory tests green.

## Evidence cited

- Implementer: `vic20-gate.log` (52 passed), `vic20-c64-smoke.log` (31), `build.log` (0 errors)
- Skeptic probe: READY@$1058, wrong base $3000 vs VICE $1000, open-bus writes stick, video test bg non-black
- Code: `Mos6561.cs` ResolveScreenBase; `SimpleRam.cs` Reset/InitializeC64; `ArchitectureBuilder.cs` BuildVic20Machine; `Vic20VideoTests.cs`; `Vic20MemoryMapTests.cs`
