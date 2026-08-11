# Goal verification — Achieved

0 of 3 skeptics refuted; survives the panel.

Per-skeptic reports: C:\Users\kingd\AppData\Local\Temp\grok-goal-f7270d7293dd\goal-classifier-f7270d7293dd-1-skeptic-0.md, C:\Users\kingd\AppData\Local\Temp\grok-goal-f7270d7293dd\goal-classifier-f7270d7293dd-1-skeptic-1.md, C:\Users\kingd\AppData\Local\Temp\grok-goal-f7270d7293dd\goal-classifier-f7270d7293dd-1-skeptic-2.md


---
## Inlined skeptic report: goal-classifier-f7270d7293dd-1-skeptic-0.md

# Adversarial verification: Fix VIC20 implementation to be VICE-faithful

## Verdict: Not Refuted

Confidence: **high**. Blocking: **none**.

## Contract

- **OBJECTIVE:** Fix VIC20 implementation to be VICE-faithful.
- **PLAN:** Gated machine identity (VIC-I present rules, memory/color bus, dual VIA, host VIC-I PAR). Full xvic is explicitly non-goal.
- **PLAN_CHANGES:** Task checklist flipped `[ ]` to `[x]` only; acceptance criteria and verification plan were not weakened.

## Criterion results

### AC1 - VIC-I presentation and color rules - MET

Shipped `src/ViceSharp.Chips/Vic/Mos6561.cs`:

- `PixelWidth = 2` (VICE `VIC_PIXEL_WIDTH`).
- Normal-border geometry: PAL 224*2 x (311-28+1)=448x284; NTSC 200*2 x 234 (`vic-timing.h`).
- Color base `$9600`/`$9400` from `$9002` bit 7.
- Palette from VICE `PALette.vpl` (cyan index 3 = 0x64,0xE3,0xDE).
- `Tick` mirrors `vic_cycle` order; fetch fills `cbuf`/`gbuf`; `EndOfLine` -> `DrawRasterLine` applies `drawing_table` std/MC + reverse gate. `RenderNow` drives that path (no EOF invent grid as display truth).

Honest evidence:

- `CharacterMode_RendersInkPixels_AtDecodedScreenBase_ForPlantedGlyph`: planted glyph vs chargen bits, double-width, 0 mismatches.
- `BorderCyan_UsesVicePalettesVplRgb` + `vic20-palette-vice.log`.
- Geometry tests + `vic20-pal-frame-geometry.log` / `vic20-ntsc-frame-geometry.log`.

### AC2 - VIC-I PAR + host present path - MET (plan-named hosts)

- Chip: `GetPixelAspectRatio` = PAL `1.66574035/2`, NTSC `1.50411479/2` (VICE `vic_get_pixel_aspect`).
- Host API: `ConsoleEmulatorHost.TryGetVicIPixelAspect` returns session `Mos6561` PAR; tested in `ConsoleHost_TryGetVicIPixelAspect_UsesViceVicIFormulas`.
- Xbox: `App.xaml.cs` prefers TryGetVicI over VIC-II table (`vic20-par-wiring.txt`).
- Plan assumed scope names ConsoleHost / Xbox App. Avalonia still uses literal `1.0` for VIC-20 (`MainWindow.axaml.cs:200`) as a near-square approximation (not C64 0.936/0.75). Residual on that head only; not a miss of the plan-named wiring.

### AC3 - RAM BLKs + color open-bus - MET

- `Vic20SystemRam.HandlesAddress` only when BLK installed; open-bus fall-through.
- `Vic20ColorRam` nibble store; read = nibble | (`VBusLastData` & 0xF0) (VICE `colorram_read`).
- Tests: uninstalled BLK, color high-nibble combine, ram-block matrix / xvic `-memory` tokens.

### AC4 - Dual VIA board - MET

- Builder: VIA1 `$9110` -> NMI, VIA2 `$9120` -> IRQ; keyboard on VIA2; idle IEC VIA1 PA `$7E`.
- Tests: bases/roles, idle `$7E`.

### AC5 - In-repo tests + session frame path - MET

Implementer capture `vic20-vice-faithful-tests.log`:

```
Test Run Successful.
Total tests: 106
     Passed: 106
```

PAL/NTSC geometry logs: 2/2 each, exit 0. Pump smoke drives real `ConsoleHostComposition` session create and asserts committed 448x284 / 400x234.

## Verification plan

| Step | Role | Result |
|------|------|--------|
| 1 focused Vic20 suite | gating | 106 pass, exit 0 |
| 2 PAL frame geometry | gating | pass, exit 0 |
| 3 NTSC frame geometry | gating | pass, exit 0 |
| 4 palette cyan vpl | gating | R/G/B match, exit 0 |
| 5 PAR wiring receipt | evidence | present |
| 6 optional deploy | optional | not required |

## Code-correctness floor (spot-check)

- Draw path is real: cycle fetch + line paint; glyph test would fail on stub paint.
- No TODO/`NotImplemented`/skipped tests on the gated surface.
- Dual-VIA interrupt lines wired in `ArchitectureBuilder` (not name-only).
- Residual non-goals (sound, interlace, lightpen, full carts, live IEC, CRT/YUV) are documented; not gating.

## Honesty of FINAL_RESPONSE

Claims of cycle/draw port, 106 green tests, and SCRATCH receipts match disk artifacts and current `Mos6561` source. No fabricated CHANGED_FILES claims against missing sources for the gated criteria.

## Residual notes (not refute)

- Avalonia desktop PAR still `1.0` for VIC-20 rather than exact VICE formulas (ConsoleHost/Xbox are correct).
- Audit markdown still has some stale Partial/Stub wording alongside the refreshed cycle/draw note; docs drift only.
- NTSC last displayed line 261 vs `TotalLines` 261 is the same VICE half-line compromise; height formula still matches plan.
)


---
## Inlined skeptic report: goal-classifier-f7270d7293dd-1-skeptic-1.md

# Skeptic verification: VIC-20 VICE-faithful

## Verdict

**Not Refuted** (`refuted: false`, confidence high, blocking none)

## What was audited

- Plan acceptance criteria 1-5 and gating verification steps
- Shipped `Mos6561` cycle/draw path and board wiring
- Implementer SCRATCH receipts under `C:\Users\kingd\AppData\Local\Temp\grok-goal-f7270d7293dd\implementer`
- Cheap re-run of focused Vic20 video / wiring / pump tests

## Criterion results

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| 1 | VIC-I geometry, pixel width 2, color base, draw rules, PALette.vpl | MET | `Mos6561.cs` constants + `DrawRasterLine`; `Vic20VideoTests` glyph/border/cyan/double-width; palette bytes match VICE `PALette.vpl` |
| 2 | VIC-I PAR formulas + host uses them for VIC-20 | MET (scoped hosts) | Chip formulas; `ConsoleEmulatorHost.TryGetVicIPixelAspect`; Xbox `App.xaml.cs` VIC-I branch; board + pump tests |
| 3 | BLK install + color open-bus | MET | `Vic20SystemRam` / `Vic20ColorRam`; board + memory map tests |
| 4 | Dual VIA roles + idle IEC | MET | `ArchitectureBuilder` VIA1/VIA2; board + Via1 IEC tests |
| 5 | Tests green + session frame geometry | MET | 106 focused pass; pump smoke 448x284 / 400x234 |

## Implementer evidence (primary)

- `vic20-vice-faithful-tests.log`: **106 passed, 0 failed**, exit 0
- `vic20-pal-frame-geometry.log`: 2/2, `PAL_EXIT=0`
- `vic20-ntsc-frame-geometry.log`: 2/2, `NTSC_EXIT=0`
- `vic20-palette-vice.log`: cyan `R=0x64 G=0xE3 B=0xDE`
- `vic20-par-wiring.txt`, `vic20-gap-map.txt` present

## Spot-check

```text
dotnet test ... --filter "Vic20VideoTests|Vic20BoardWiringTests|Vic20VideoPumpSmokeTests"
Passed: 23, Failed: 0
```

## Honesty review

- Tests drive real machines / host sessions, not mocks of the unit under test.
- `RenderNow` loops real `Tick` until `FrameCompleted` (same draw path as live clock).
- Character paint asserts ink pixels against chargen bits with double-width expansion.
- No skip/ignore on the gate suite used for this goal.

## Non-blocking residuals

- Avalonia still sets VIC-20 `PixelAspect = 1.0` in `MainWindow.axaml.cs` (plan PAR wiring named ConsoleHost/Xbox).
- Full xvic modules remain deferred (plan non-goals).
- Audit markdown module row for `vic-draw` is slightly stale vs current `DrawRasterLine` code.

## Findings

None that refute a gating criterion or show a shipped defect against the plan contract.


---
## Inlined skeptic report: goal-classifier-f7270d7293dd-1-skeptic-2.md

# Adversarial verification: VIC-20 VICE-faithful

## Verdict: Not Refuted

## Contract

- **OBJECTIVE:** Fix VIC20 implementation to be VICE-faithful.
- **PLAN:** Gates on defining machine identity (VIC-I present rules, RAM/color open-bus, dual VIA, host VIC-I PAR for ConsoleHost/Xbox), not full xvic. Non-goals: interlace, lightpen, CRT/YUV, sound, full carts, multi-minute every-cycle lockstep.
- **PLAN_CHANGES:** Checklist items marked complete only; acceptance criteria not weakened.

## Criteria

| # | Criterion | Result |
|---|-----------|--------|
| 1 | VIC-I presentation: PixelWidth=2, normal geometry, color base, std/MC draw, PALette.vpl | **MET** |
| 2 | VIC-I PAR formulas + host uses them for VIC-20 (not C64 VIC-II) | **MET** (chip + ConsoleHost/Xbox) |
| 3 | BLK open-bus + colorram_read high nibble | **MET** |
| 4 | Dual VIA roles, keyboard VIA2, idle IEC $7E | **MET** |
| 5 | In-repo tests pass; session frame geometry PAL/NTSC | **MET** |

## Shipped path (audited)

`src/ViceSharp.Chips/Vic/Mos6561.cs`:

- `Tick` mirrors VICE `vic_cycle` order (open_v, cycle++, end_of_line/draw, open_h, memptr, latch, fetch).
- Fetch fills `cbuf`/`gbuf`; `DrawRasterLine` applies `drawing_table` std/MC + reverse; `VIC_PIXEL_WIDTH=2`.
- Geometry constants match `vic-timing.h` (PAL 224x(311-28+1), NTSC 200x(261-28+1)).
- Palette matches `native/vice/vice/data/VIC20/PALette.vpl` (cyan index 3 = 64 E3 DE).
- PAR matches `vic_get_pixel_aspect` (PAL 1.66574035/2, NTSC 1.50411479/2).
- `RenderNow` drives full raster via `Tick` (display truth is cycle/draw, not EOF invent grid).

Board path (`ArchitectureBuilder.BuildVic20Machine`): VIA1 $9110 NMI, VIA2 $9120 IRQ, `Vic20SystemRam` BLK claim, `Vic20ColorRam` open-bus high nibble, VIC `MemoryPeek`/`VBusFetch` wiring.

## Evidence

| Artifact | Observation |
|----------|-------------|
| `implementer/vic20-vice-faithful-tests.log` | 106 passed, 0 failed, exit success |
| `implementer/vic20-pal-frame-geometry.log` | 2/2 passed, PAL_EXIT=0 |
| `implementer/vic20-ntsc-frame-geometry.log` | 2/2 passed, NTSC_EXIT=0 |
| `implementer/vic20-palette-vice.log` | cyan R/G/B 0x64/0xE3/0xDE |
| `implementer/vic20-par-wiring.txt` | Mos6561 + ConsoleHost + Xbox VIC-I path |
| `implementer/vic20-gap-map.txt` | Residuals listed as plan non-goals |
| Spot-check (skeptic) | CharacterMode + PAL geometry + BorderCyan + TryGetVicIPixelAspect: 4/4 pass |

## Honesty of tests

- Vic20 video/board/memory tests construct real machines via `MachineTestFactory` / `ArchitectureBuilder` (no mock of Mos6561/VIA/RAM under test).
- `CharacterMode_RendersInkPixels_*` plants glyph + color and asserts ink pixels after `RenderNow` (cycle fetch + draw).
- Session pump tests create real VIC-20 sessions and assert committed frame W/H.
- No skips/ignores in the focused 106-run.

## Non-blocking notes (not refutes)

1. **Avalonia** `MainWindow.UpdateVideoAspect` still uses hardcoded `1.0` for VIC-20 PAR. Plan host wiring scope is ConsoleHost / Xbox (implemented). Not elevated to refute under plan assumed scope.
2. **`docs/audit-vic20-vs-vice-2026-08-07.md`** still has some stale draw-path wording; gap-map receipt is current for this slice.
3. Multicolor branch is ported 1:1 with VICE `drawing_table` but paint tests focus on standard mode; code review confirms MC pairing matches VICE.

## Decision

All gating acceptance criteria are corroborated by shipped code, honest tests, and implementer captured evidence. Residual gaps are explicit non-goals. **Not Refuted.**
