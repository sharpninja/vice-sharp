# Phase C step 3, PLAN-OBSERVER-001 slice S2a: bus adapter on real pipes, mock first (LAB-OMARCHY), 2026-10-20T08:00 to 08:03

Follows S1c (`c711365`). LEGION2 ran S1b and S1c on Windows with equal counts (50 and 203; 58 and 211; receipts pending Payton's clearance on LEGION2). No VICE source, object, build target or emulator binary is used; the retained gate adapter `gate-observer.c` is linked unmodified.

## Decision (plan item 6, operator-confirm item 3 still open)

The observer's events reach the host on stderr as `VICE_BUS,<kind>,<detail>` lines, the stream the retained gate adapter already publishes `VICE_GATE,<clock>` on and flushes before reading the permission byte. The gate adapter is not modified: `tools/immutable-vice/bus-adapter.c` wraps `vice_sharp_observe_clock` (a cross-object call from `clock-observer.c`), runs the observer's per-cycle hook first and then the real gate, so every event of a cycle precedes that cycle's gate record (plan item 7). A sink write failure reports `VICE_BUS_STOP,72` and terminates with 72, never returning to native unobserved (the gate owns 64 to 70). Composition of the `VICE_OBS` half-cycle line from these events and the machine globals is S4.

## BDP order

1. Mocks first (08:01): `BusAdapterFixtures` and `BusAdapterContractTests` (four cases: continue-both, continue-then-quit, eof-first, broken-stderr; wrong observations rejected: wrong or zero exit, extra bytes, bus events after their gate record, dropped access, cycle or repatch line, doubled line ends, forbidden native output after a stop). Run with the real-process class before the adapter existed: mocks 4 passed, real 4 failed (`cc1: fatal error: .../Fixtures/bus-adapter.c: No such file or directory`).
2. Adapter (08:02): `bus-adapter.c` (sink and wrapped observe). Driver `Fixtures/bus-real-driver.c` rebuilds the double's tables, then per clock performs one read through the live row and calls the wrapped `vic_cycle`; with `wait` it reads one stdin byte first so the test can close stderr before the first sink write.
3. Gates (08:02): `--filter BusAdapter` 8 passed; full `ViceSharp.NativeClockProbe.Tests` 219 passed, 0 failed, 0 skipped; `git diff --check` clean; gcc `-std=c11 -Wall -Wextra -Werror` clean with the five wraps (`vic_cycle`, `mem_initialize_memory`, `mem_mmu_translate`, `mem_toggle_watchpoints`, `vice_sharp_observe_clock`). Observed on Linux: broken-stderr exits 72 (the first sink write, `repatch`, fails on the closed pipe) with no `NATIVE_AFTER` on stdout.

## Open

- Windows run (expect `--filter BusAdapter` 8, full 219; the broken-stderr case may show the same 72 or, if the MSVCRT buffers stderr, a later failure; the fixture accepts only 72, so a Windows difference is a finding, not a tolerance).
- S2b: the per-host PowerShell build script linking the stock objects with the five wraps (`-DVICE_SHARP_ROWS=32` for x64sc), launch and result JSON, object hashes, inventories.
- S3 proof items (a) and (b), then the byte-identical trace proof.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 96%, accuracy 97%, completeness 94%
