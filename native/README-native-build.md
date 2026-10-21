# Native VICE builds for ViceSharp

The only native builder is `native/build-vice-observer.ps1` (PLAN-NATIVERESTORE-001 step D1; decisions R2 A and R3 A retired `build-vice-shim.ps1`, `build-vice-shim.sh` and `build-vice-shim-xvic.sh`). It builds the stock VICE programs from a hash-verified copy of the pinned materialization (`artifacts/immutable-vice/<sha>/source/vice`, commit `cc67418cba58e71d8e70fd353e9de47199ad2075`) and relinks `x64sc-observer` and `xvic-observer` with the external translation units under `tools/immutable-vice/` through `ld --wrap`. Nothing under `native/vice` is read by the build, written by the build, or modified by anything in this repository; the submodule is the immutable oracle source (`PLAN-VICE-IMMUTABLE-001`).

## Invocation

```
pwsh -NoProfile -NonInteractive -File native/build-vice-observer.ps1 -Stage All -Machine both -WhatIf
pwsh -NoProfile -NonInteractive -File native/build-vice-observer.ps1 -Stage All -Machine both
```

Stages: Copy (tracked-list copy of the materialization into `work-<host>`), Autogen, Configure (MSYS POSIX paths on Windows), Make (`src/lib`, `src/arch/shared`, `src/core`, then the programs), Link (wrapper objects right after `-o`, the wrap flags last), Inventory (the materialization re-hashed; added files reported, never deleted). On Windows every native tool runs inside the MSYS2 MINGW64 login shell with forward slashes (lab rule). `-WhatIf` stops before any directory or file is created. A rerun on an up-to-date tree reuses the retained link lines only after every object they name still hashes as recorded (`native/lib/ObserverBuild.psm1`); otherwise the Make stage stops with a named prerequisite.

Receipts go to `artifacts/immutable-vice/<sha>/receipts-<host>/` (gitignored) and are copied into `docs/receipts/` by the step that uses them.

## Wrapped symbols

`vic_cycle` or `vicii_cycle` (clock gate and bus observer), `vicii_steal_cycles` (x64sc stolen cycles), `mem_initialize_memory`, `mem_mmu_translate`, `mem_toggle_watchpoints` (bus observer rows), `vice_sharp_observe_clock` (bus adapter), `maincpu_snapshot_read_module` and `machine_read_snapshot` (snapshot observer, `VICE_SNAP` records). The checkpoint unit (`snapshot-checkpoint.c`) wraps nothing; it is armed only by `VICESHARP_SNAPSHOT_AT=<trigger>:<path>` and registers one trap through the pinned public API.

## Equivalence and traces

`tools/immutable-vice/Invoke-ObserverEquivalence.ps1` runs the stock, withheld and keep triples per machine and cycle count, compares the gate sequences, screenshots and access subsequences, validates the `VICE_OBS` traces, and supports `-WhatIf`.

## Tests

`native/tests/*.Tests.ps1` (Pester 5, run with `Invoke-Pester`) cover the preservation script, the consumer inventory, the fixture scan, the link-line reuse module and both scripts' dry runs. `tests/ViceSharp.NativeClockProbe.Tests` compiles every wrapper unit against doubles through the real linker. PSScriptAnalyzer must report zero findings on every script here.
