# Phase C step 3, PLAN-OBSERVER-001 slice S1b: x64sc 32-row layout, mock first, then the row-aware wrapper (LAB-OMARCHY), 2026-10-20T07:46 to 07:52

Follows S1 (`docs/receipts/lockstep/20261020T074300-phase-c-step3-s1-bus-observer-linux/`, `7dbfd29`) and LEGION2's Windows run of S1 (22 and 175, equal to Linux, gcc 15.2.0 MSYS2, receipt pending Payton's clearance on LEGION2) and its review at 07:49. No VICE source, object, build target or emulator binary is used.

## BDP order

1. Mock gate first, commit `0125915` (07:46): `BusObserverRowsContractTests`, the ten single-row fixtures adapted to the 32-row layout (`layout,32`, `repatch,16448,1`) plus `bank-switch`, `rewrite-noncurrent-row`, `bank-then-rebuild`; five lenient variants rejected. Gate: `--filter "FullyQualifiedName~BusObserverRowsContractTests"`: 15 passed, 0 failed, 0 skipped.
2. Fixtures and the failing implementation test (07:49): `native-bus-double.c` gains the `VICE_SHARP_ROWS` shape (static `mem_read_tab[32][0x101]`, global `mem_get_current_bank_config`, global setters, static `mem_update_tab_ptrs` from the toggle and from a banking change, watch rows delegating to the current row), `bus-driver.c` gains `bank` and `rewriterow`, `CompiledBusProbe` is shared by both implementation classes, `BusObserverRowsImplementationTests` compiles `bus-probe-rows` with `-DVICE_SHARP_ROWS=32`. Run before the wrapper change: the 13 row cases failed (no `layout` event, `repatch,514,1`, and `foreign` after a banking change, which is the S1 wrapper failing closed as designed); the 10 single-row cases still passed.
3. Wrapper (07:50): `bus-observer.c` with `VICE_SHARP_ROWS`, per-row saved originals selected by `mem_get_current_bank_config()` in every trampoline (closes LEGION2 hazard 2: dummy cycles after a `$01` switch now use the new configuration's handlers), `locate_rows` with the two-stage layout self-test through the global `mem_read_tab_set` (live row is the current configuration's row; stride 0x101), `layout,<rows>` with the fail-closed fallback, per-cycle rescan of the current row, `foreign` when the current row is unknown. Gate: `--filter BusObserver`: 50 passed, 0 failed, 0 skipped.
4. Review item 4 (07:51): translate event extended to `<limit>,<baseNulled>,<realLimit>,<realBaseNonNull>`; mocks changed first (two implementation cases then failed on `translate`), then the wrapper. Final gates (07:52): `--filter BusObserver` 50 passed; full `ViceSharp.NativeClockProbe.Tests` 203 passed, 0 failed, 0 skipped, 846 ms; `git diff --check` clean; gcc `-std=c11 -Wall -Wextra -Werror` clean for both fixtures.

## Review disposition (LEGION2, 07:49)

- 1 confirmed. 2 fixed in this slice (above). 4 fixed in this slice. 3 (foreign reported every cycle while the monitor's watch flag is 1): not changed, recorded as a known limitation in `BUS-OBSERVER-SLICE.md`; the static watch rows cannot be identified without toggling the monitor's own `watchpoints_active`, and gated runs keep watchpoints off.

## Open

- Windows run of the S1b gates (LEGION2 after pull). S2 build script. S3 proof items (a) no bus access through the rows during `mem_initialize_memory`, (b) single emulation thread, both to be cited from source.

## Self-assessment

Accuracy 97: every rule has a fixture, a failing run and a passing run, and a citation. Completeness 93: Windows run and S2 open and named.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 96%, accuracy 97%, completeness 93%
