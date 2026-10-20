# Linux materialization of the immutable VICE commit, LAB-OMARCHY, 2026-10-19T20:25

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 4, Phase C amendment ("step 2 built and proved on both hosts, because the Linux shim build path must consume the same external translation units later in Phase D") and Phase D amendment ("the Linux materialization is created under `artifacts/immutable-vice/` on LAB-OMARCHY by the same script"). This receipt is the materialization only: no build, no wrapper, no change under `native/vice`. Class 2 operator-directed preparation on the lockstep path, done while Phase C step 3 waits on an operator ruling.

Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, `develop` at `feb1fd4`, 2026-10-19T20:25:42 to 20:25:45, by `~/codex-dispatch/scripts/New-LinuxMaterialization.ps1` (PowerShell, parser and analyzer clean, `-WhatIf` first).

## What was done

- Source of truth: the submodule's own object store. `git -C native/vice cat-file -t cc67418cba58e71d8e70fd353e9de47199ad2075` is `commit`; the gitlink `HEAD:native/vice` and the submodule checkout HEAD are both that commit.
- Export: `git -C native/vice archive --format=tar -o <receipt>/vice-cc67418c.tar cc67418c...` (SHA256 `AA418D5BA0F4BD20...`, full value in `summary.json`), extracted with `tar -xf` into `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`. `artifacts/` is gitignored (`.gitignore` line 4; `git check-ignore` confirms).
- Result: 3,842 files, 48,919,088 bytes. Every file hashed; the sorted hash list (`hash|bytes|relative path`, 3,842 lines) is retained at `~/codex-dispatch/materialization-20261019/apply/materialization-hashes.txt`, SHA256 `D22FDBF8A015C6F7...` (full value in `summary.json`).
- Native tree untouched: `git -C native/vice status --porcelain=v1 -uall` before and after is 163 entries, byte-identical (`native-inventory-before.txt`, `native-inventory-after.txt`, same SHA256).

## Comparison with PAYTON-LEGION2 (appended 2026-10-19T20:32)

LEGION2's tree was produced on 2026-10-08 by `git -c core.autocrlf=false clone --no-hardlinks --no-checkout` then `checkout --detach cc67418c` (tree `f1e80918`), a checkout with no CRLF conversion and no `.gitattributes` eol rules; it holds 3,847 tracked files (48,968,247 bytes; `git ls-tree -r` at the commit lists 3,847 blobs) plus 2,815 generated files from its in-place stock build, 6,662 on disk (receipt `docs/receipts/native-vice/20261019T202700-legion2-materialization-hashlist/`, commit `4ba077fa`).

Correction to the first count above: the 3,842-file list of 20:25 was produced by a listing without `-Force`, which on Linux PowerShell drops dot-named entries; the tar and the extracted tree always held all 3,847 files (`tar -tf` counts 3,847 file entries). LEGION2 found this by set difference (the five missing paths were `vice/doc/.indent.pro` and four `.github/workflows/*.yml`, 49,159 bytes). The regenerated list with hidden files (`compare/omarchy-tracked-files.txt`, LEGION2's line format, ordinal sort, LF): 3,847 files, 48,968,247 bytes, SHA256 `F2833CA052F80490BBACC99D29F8E030C781845F882B8984B2B9EC7931B75289`.

Set comparison (`~/codex-dispatch/scripts/Compare-MaterializationLists.ps1`, `compare/compare.json`): 3,847 lines equal, 0 only on LAB-OMARCHY, 0 only on LEGION2. Every tracked file of the immutable commit is byte-identical on both hosts (same SHA256, same size, same path). The two list-file hashes differ only in line order (LEGION2's sort versus ordinal), which the set comparison makes irrelevant. No CRLF difference exists. Observation, not inference: the Linux and Windows materializations of `cc67418c` are the same source tree.

## What this does not claim

No Linux build of VICE, no shim, no wrapper, no observation. The build tools present on LAB-OMARCHY (autoconf, automake, libtool, pkg-config, make, gcc 16.2.1, bison, flex; `xa` absent) were only listed. Phase D step 1 (the canonical PowerShell materialize-and-build script) is not started; this receipt gives it its input.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 99%, accuracy 99%, completeness 97%
