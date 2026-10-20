# Linux materialization of the immutable VICE commit, LAB-OMARCHY, 2026-10-19T20:25

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 4, Phase C amendment ("step 2 built and proved on both hosts, because the Linux shim build path must consume the same external translation units later in Phase D") and Phase D amendment ("the Linux materialization is created under `artifacts/immutable-vice/` on LAB-OMARCHY by the same script"). This receipt is the materialization only: no build, no wrapper, no change under `native/vice`. Class 2 operator-directed preparation on the lockstep path, done while Phase C step 3 waits on an operator ruling.

Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, `develop` at `feb1fd4`, 2026-10-19T20:25:42 to 20:25:45, by `~/codex-dispatch/scripts/New-LinuxMaterialization.ps1` (PowerShell, parser and analyzer clean, `-WhatIf` first).

## What was done

- Source of truth: the submodule's own object store. `git -C native/vice cat-file -t cc67418cba58e71d8e70fd353e9de47199ad2075` is `commit`; the gitlink `HEAD:native/vice` and the submodule checkout HEAD are both that commit.
- Export: `git -C native/vice archive --format=tar -o <receipt>/vice-cc67418c.tar cc67418c...` (SHA256 `AA418D5BA0F4BD20...`, full value in `summary.json`), extracted with `tar -xf` into `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`. `artifacts/` is gitignored (`.gitignore` line 4; `git check-ignore` confirms).
- Result: 3,842 files, 48,919,088 bytes. Every file hashed; the sorted hash list (`hash|bytes|relative path`, 3,842 lines) is retained at `~/codex-dispatch/materialization-20261019/apply/materialization-hashes.txt`, SHA256 `D22FDBF8A015C6F7...` (full value in `summary.json`).
- Native tree untouched: `git -C native/vice status --porcelain=v1 -uall` before and after is 163 entries, byte-identical (`native-inventory-before.txt`, `native-inventory-after.txt`, same SHA256).

## Comparison with PAYTON-LEGION2

LEGION2's materialization (`artifacts/immutable-vice/cc67418c...`, reported 2026-10-19T19:51 as 6,689 files matching the gitlink) includes the stock upstream build outputs made on 2026-10-08. This Linux materialization is source only, so the counts are not comparable until LEGION2 reports its `source` subtree count and hash-list hash computed the same way; that comparison is requested and will be appended here. Line-ending normalization may differ between the two exports (git archive honors `.gitattributes`); a difference confined to CRLF versus LF is expected to be reported as such, not as drift.

## What this does not claim

No Linux build of VICE, no shim, no wrapper, no observation. The build tools present on LAB-OMARCHY (autoconf, automake, libtool, pkg-config, make, gcc 16.2.1, bison, flex; `xa` absent) were only listed. Phase D step 1 (the canonical PowerShell materialize-and-build script) is not started; this receipt gives it its input.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 99%, accuracy 99%, completeness 97%
