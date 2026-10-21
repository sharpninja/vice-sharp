# PLAN-NATIVERESTORE-001 step D4 on LAB-OMARCHY: restore of the native/vice tracked delta (2026-10-21T05:18 to 05:21)

Executed by Claude Code (claude-fable-5-1-high@ClaudeCode, session OMAR-VICE#) after D3 (`20261021T051500-phase-d-d3-classification/`), with D0's receipt committed (`a1b3b6aa`), the plan at HV AGREE (round 8) and Payton's release of 2026-10-20T19:35, under the R16 ruling of 22:58 (D3 to D5 proceed; the ordinary gate is ruled at the exit).

## Order of operations

1. R1 B applied as one superproject commit before the first restore: `.gitmodules` `native/vice` `ignore = dirty` becomes `ignore = untracked` (`dc7b5569`).
2. `native/Restore-ViceSubmoduleDelta.ps1` and its nine Pester controls committed (`ecb69d49`; Pester 9 passed, 0 failed, 0 skipped at 05:18:17 on a mock superproject with a real submodule: happy path, `-WhatIf` writes nothing, resume, changed live delta, staged drift, missing receipt plus corrupt manifest plus corrupt preserved copy, wrong repository plus wrong host, moved gitlink or checkout, untracked inventory drift; analyzer 0). The CRLF-converted mock is LEGION2's control.
3. Dry run (`restore-report.whatif.json`, 05:19): identity verified (receipt host LAB-OMARCHY, receipt submodule path equal to the resolved path, git dir present, gitlink `cc67418c...` equal to the checked-out HEAD and to the receipt), every preserved copy re-hashed equal to the receipt, index clean, untracked inventory equal to the receipt (added 0, deleted 0, changed 0), all 8 listed paths classified untouched (live hash equal to the preserved copy), 0 unexpected, 0 stops; nothing written.
4. Real run (`restore-report.json`, 05:20): the same checks, then `git -C native/vice checkout -- <path>` for each of the 8 paths in order (`vice/src/c64/c64cpusc.c`, `vice/src/drive/drive-snapshot.c`, `vice/src/drive/drivecpu.c`, `vice/src/drive/drivecpu65c02.c`, `vice/src/mainc64cpu.c`, `vice/src/resid/filter8580new.h`, `vice/src/resid/sid.h`, `vice/src/sid/resid.cc`); no `reset --hard`, no `clean`. Outcome Complete.

## Proofs after the restore (from `restore-report.json`)

- `git -C native/vice diff --exit-code`: empty. `git -C native/vice diff --cached --exit-code`: empty. Tracked entries in `status --porcelain=v1 -uall`: 0.
- Untracked inventory equal to the D0 receipt by path, SHA-256 and length: 155 entries, none new, none deleted, none changed (R4, F08).
- Superproject `git status --porcelain=v1 -- native/vice`: no line (the submodule is clean under `ignore = untracked`).
- Gitlink equal to the checked-out SHA: `cc67418cba58e71d8e70fd353e9de47199ad2075`.
- Preserved copies re-hashed after the run: all 8 equal to the receipt.
- `inventory-after-restore.json`: the post-restore inventory baseline (schema `phase-d-inventory/1`, 155 untracked entries, head `cc67418c...`) for the F08 brackets of D5 and the exit (`native/Test-SubmoduleInventory.ps1 -BaselineManifest <this file>`).

## What this does not claim

No native binary was rebuilt yet (D5 rebuilds the observers from the clean materialization and reruns the equivalence matrix). LEGION2's D4 (12 files, its two injected files retained) follows its D0 commit and runs the same script with its own receipt. The ordinary gate stays as measured in D2 until the exit ruling (R16).

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 98%, accuracy 99%, completeness 98%
