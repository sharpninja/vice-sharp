# Phase D step D0 on PAYTON-LEGION2: measure and preserve (final, Outcome Complete)

PLAN-NATIVERESTORE-001 D0, released by Payton 2026-10-20 19:35 ("Release Phase D implementation now", relayed by OMAR-VICE#). Read only on `native/vice`: status, hashes, copies; no checkout, clean, build or restore. `native/vice` git status is byte-identical before and after every run below.

## Run

develop `a1b3b6aa` (D0 script with the LEGION2 generated patterns and the `vice/src/.vice-shim-configure-flags` injected name). In a fresh `pwsh -NoProfile -NonInteractive`:

- `Invoke-Pester native/tests/Save-ViceSubmoduleDelta.Tests.ps1`: 9 total, 9 passed, 0 failed, 0 skipped, 0 not run. `Invoke-ScriptAnalyzer native/Save-ViceSubmoduleDelta.ps1`: 0.
- Gitlink `git ls-tree HEAD native/vice`: `cc67418cba58e71d8e70fd353e9de47199ad2075`; checked-out SHA identical.
- Dry run with `-WhatIf`: Outcome Complete, nothing written. Real run (through `-Command`, so `-LibrarySearchRoots` arrives as the array `native`, `tests/ViceSharp.TestHarness/bin/Release/net10.0`, `tests/ViceSharp.TestHarness/bin/Debug/net10.0`): Outcome Complete.

## Result ([manifest.json](manifest.json))

- 2,539 porcelain entries: 12 tracked modified; 2,527 untracked (1,084 ordinary, 1,443 hidden). `git diff --cached` empty.
- Classes: generated 2,525, injected 2 (`vice/src/.vice-shim-configure-flags`, `vice/src/vice-shim-runtime.h`), modified 12, unclassified 0.
- Patch [native-vice-tracked.patch](native-vice-tracked.patch): 22,404 bytes, SHA256 90604E39B827F850A382DC13DD921141290EF2C34E1483CDC6C612A07803E9E2, 20 hunks, all 20 mapped to plan rows (expected host PAYTON-LEGION2); 0 unclassified hunks, 0 explained differences, 0 missing expected. Byte-identical (same length and SHA256) to the Oct 4 Phase 0 patch in `docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch`.
- Libraries: `native/vice_x64.dll` 14,581,663 bytes, SHA256 E5B076517136813C589258A0550256C7A244EA34431687E4DBC72201A4F4557B; `native/vice_xvic.dll` 10,930,779 bytes, SHA256 F03A7A3545C5964FBAB863D3F3D1AB7A2EB99C2543ED94071F80F07648513886. None under the two TestHarness bin roots.
- Preserved copies: 14 (the 12 modified tracked files and the 2 injected files), each re-verified after the run: copy hash equals source hash equals manifest hash.

The semantic consumer inventory and the fixture scan are source-only and were run once on LAB-OMARCHY (OMAR-VICE#'s D0 receipt).

## Superseded folders (retained, never deleted or moved)

- [../20261020T211907-phase-d-preservation-windows](../20261020T211907-phase-d-preservation-windows): first real run at `743975ea`. Outcome Blocked (16 unclassified entries: 15 generated files the classifier had no pattern for, and `vice/src/.vice-shim-configure-flags`). Also has an empty library list from my invocation error: `-LibrarySearchRoots 'native,tests/...'` passed through `pwsh -File` arrives as one string, not an array, so the search used one nonexistent path.
- [../20261020T212006-phase-d-preservation-windows](../20261020T212006-phase-d-preservation-windows): rerun at `743975ea` through `-Command` with the array; libraries listed correctly; still Blocked on the same 16 entries. Those lists led to the classifier fix in `a1b3b6aa`.

Both are superseded by this folder, produced with the final script version.
