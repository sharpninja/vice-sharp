# Hostile Validator Receipt

- **TimestampUtc**: 2026-08-11T20:38:52Z
- **ValidatorIdentity**: GrokSubagentHostile
- **Workspace**: F:\GitHub\vice-sharp
- **Method**: Independent re-read of implementer temp logs under `C:\Users\kingd\AppData\Local\Temp\grok-goal-a8b508754f9e\implementer\` and durable `docs/receipts/vic20-*-2026-08-11*`; SHA256 compare where applicable; source spot-check of test files. No product code changes. No trust of plan checkboxes.

## Claims reviewed

| Id | Claim (summary) | Verdict |
|----|-----------------|---------|
| A | Vic20Pixel gate green 8/0/0 | **PASS** |
| B | Bgra_ReadyPal uses TryCaptureVisibleFrame (not pure tautology) | **PASS** |
| C | Cart+sound gate 31/0/0 | **PASS** |
| D | Lockstep 2s PAL+NTSC green with VICESHARP_LOCKSTEP_2S=1 | **PASS** |
| E | Snapshot tests real (blob RT + resume after ZP), not inventory theater | **PASS** |
| F | Scoped Exact language honest (index Exact; BGRA Partial OK; residuals listed) | **PASS** |

## Per-claim evidence

### A) Vic20Pixel gate 8/0/0 - PASS

**Attack**: Aggregate could be hand-edited; docs vs temp diverge; skips counted as pass.

**Re-verify**:
- Temp `vic20-pixel-gate.log` and docs `docs/receipts/vic20-pixel-gate-2026-08-11.log` SHA256 match (`A828399B73D6...`), length 23615 both.
- Eight process-isolated runs each report `Test Run Successful` / `Passed: 1` / EXIT=0:
  - Vic20PixelLockstepDiag.Dump_ReadyPal_IndexHistogram
  - Vic20PixelLockstep.Index_BusyPal_SequenceEqual
  - Vic20PixelLockstep.Index_ReadyNtsc_SequenceEqual
  - Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual
  - Vic20PixelLockstep.Index_ReadyPal_SequenceEqual
  - Vic20PixelFrameTests.CaptureVisibleFrame_AfterBoot_ReturnsPalNormalCanvas
  - Vic20PixelFrameTests.ManagedAndNative_FrameGeometry_MatchAfterBoot
  - Vic20PixelFrameTests.CaptureFrameIndices_DimensionsMatchBgra
- Footer: `AGGREGATE Passed=8 Failed=0 Skipped=0` and `Exit criterion: Failed=0 Skipped=0`.
- Wall times 5-6s per lockstep test (not bare early-return).

### B) Bgra_ReadyPal uses TryCaptureVisibleFrame - PASS

**Attack**: Test only expands indices both sides (tautology) and never hits native BGRA.

**Re-verify** (`tests/ViceSharp.TestHarness/Vic20/Vic20PixelLockstep.cs`):
- Lines 92-104: allocates BGRA buffer, `Assert.True(native.TryCaptureVisibleFrame(nBgra, out var bw, out var bh))`, geometry asserts, alpha `0xFF` scan on native buffer.
- Lines 106-107: prefers full native vs managed `SequenceEqual`; only on diverge falls back to shared-palette expand of **native** indices vs managed FrameBuffer (still non-tautological for native indices path) plus managed index consistency.
- Gate log: `Bgra_ReadyPal_SequenceEqual` Passed [5 s] with EXIT=0.
- Soft note: if canvas RGB always mismatches, hard RGB Exact is not claimed; claim F already scopes Partial. Native capture path is still exercised.

### C) Cart+sound gate 31/0/0 - PASS

**Attack**: Broken counter; wrong filter; skips hidden.

**Re-verify**:
- Temp and docs cart-sound logs SHA256 match (`7E14D3423EAD...`).
- Per-filter: Flash040 12, Fe3Flash 4, UltimemParity 3, MegaCartParity 3, Vic20CartInventory 1, Vic20Sound 7, Vic20NativeAudio 1. Sum = 31. All `Failed: 0`, `Skipped: 0`, EXIT=0.
- `CART_SOUND_SUMMARY Passed=0` is a broken first counter in the log; **`CART_SOUND_SUMMARY_FIXED Passed=31 Failed=0 Skipped=0 EXIT_ALL=0`** matches arithmetic. Hostile note only: do not cite the unfixed SUMMARY line.

### D) Lockstep 2s non-idle PAL/NTSC with env=1 - PASS

**Attack**: Bare `return` when env unset still reports Passed; claim only true for focused; docs log incomplete.

**Re-verify**:
- Source (`Vic20WorkloadLockstep.cs`): TwoSecondPal/Ntsc require `VICESHARP_LOCKSTEP_2S=1` else bare return; then set `VICESHARP_LOCKSTEP_VIDEO=0` and `RunEveryCycle(TwoSecondPalCycles=2216810 / TwoSecondNtscCycles=2045454)`.
- Gate evidence (temp + docs both):
  - `TwoSecondPal EXIT=0 MS=24670` - Passed `EveryCycle_Workload_Match_TwoSecondPal` **[19 s]** - Total tests 1 Passed 1.
  - `TwoSecondNtsc EXIT=0 MS=16615` - Passed `EveryCycle_Workload_Match_TwoSecondNtsc` **[11 s]** - Total tests 1 Passed 1.
- 19s / 11s wall times disprove env-less early return (that path finishes in discovery overhead only, not multi-second every-cycle).
- Non-idle: KERNAL READY CPU every-cycle match; video track off for wall time only (documented in source).
- **Caveat (not a claim-D fail)**: multi-test `FocusedVideoCpu` batch in same gate log has `EXIT_FOCUSED=1` (2 fails, NativeVice residue). Claim D only names TwoSecondPal/Ntsc. Isolated re-runs `focused-pal.log` / `focused-ntsc.log` later 1/0/0 each; temp log footer has re-verify summary; **docs copy of lockstep gate lacks that footer** (hash mismatch temp vs docs). Residual honesty covered under F.

### E) Snapshot tests real, not inventory theater - PASS

**Attack**: All four facts are string inventory / preset theater; no blob; no resume.

**Re-verify** (`Vic20SnapshotRoundTripTests.cs` + `vic20-snapshot-gate-2026-08-11.log`):
- Gate: Total tests 4, Passed 4 (temp SHA = docs SHA).
- **Real**:
  - `Unexpanded_ManagedStateBlob_RoundTripEqual`: CaptureManagedBlob (CPU regs + 0x100 ZP), mutate bus, RestoreManagedBlob (ZP + Mos6502 regs), assert PC/A/X/Y/S/P and ZP bytes. Durable byte blob round-trip.
  - `Unexpanded_ManagedBlob_ResumeLockstep_Short`: 50k lockstep, capture blob, mutate ZP, restore ZP only, 2k post-resume every-cycle CPU match vs native. Matches claim wording "resume lockstep after ZP restore".
- **Still theater (does not invalidate claim E as worded)**:
  - `Inventory_DocumentsXvicVsfModules`: local string array self-Contains + reflection MethodInfo not-null. Theater.
  - `Fe3_ConfigBlob_RoundTrip`: ApplyConfigPreset toggle, not machine serialize blob.
- Claim E parenthetical correctly names the real tests and does not assert all four are non-theater.

### F) Scoped Exact language honest - PASS

**Attack**: Overclaims full BGRA RGB Exact; hides residuals; exact-receipts invent green counts.

**Re-verify** (`docs/receipts/vic20-exact-receipts-2026-08-11.txt` and temp twin):
- Claims index SequenceEqual READY PAL/NTSC/busy: matches Index_* tests + green gate A.
- BGRA: "TryCaptureVisibleFrame path exercised; canvas RGB may Partial; aligned Exact via shared PALette" matches source Bgra_ReadyPal behavior (claim B).
- Residuals explicit: niche carts Explicit Missing; IEEE/rsuser/printer; headless WriteSnapshot hang; multi-test NativeVice process isolation. Matches observed EXIT_FOCUSED=1 then process-isolated re-pass.
- Does not claim native VSF WriteSnapshot Exact or full cart catalog Exact.
- Counts in exact-receipts (pixel 8, cart-sound 31, lockstep 2s 2, snapshot 4) match re-read gates for the scoped claims.

## Explicit FAIL list

*(empty)*

## Notes / non-FAIL caveats (audit)

1. Docs `vic20-lockstep-gate-2026-08-11.log` is a truncated twin of temp (missing process-isolation re-verify footer). TwoSecond results still present and green.
2. Cart-sound first SUMMARY line is wrong (0); FIXED line is correct.
3. Snapshot suite still ships 2 theater facts; claim E scopes the real ones.
4. TwoSecond tests force `VICESHARP_LOCKSTEP_VIDEO=0` (CPU Exact, not every-cycle video Exact for 2s budget).

## OverallVerdict

**AGREE**

All claims A-F independently re-verified PASS. No FAIL list entries.
