# Phase C step 3, PLAN-OBSERVER-001 slice S1c: watch rows as allowed delegates (LAB-OMARCHY), 2026-10-20T07:53 to 07:57

Follows S1b (`6197252`, receipt `20261020T075200-phase-c-step3-s1b-rows-linux/`). Proposed by LEGION2 at 07:53 as a way to learn the monitor's watch rows without touching monitor state; adopted with mocks first. No VICE source, object, build target or emulator binary is used.

## Facts from the materialized source (LEGION2 07:53, re-read here where cited)

- For any nonzero flag the real pointers go to the single static watch row per table (`vic20mem.c` 536 to 556; `c64memsc.c` 166 to 192); flag 1 leaves the dummy pointers on the data rows, flag 2 moves them to the watch rows too. One watch row per table on both machines, so once learned it stays valid.
- The rebuild ends with an intra-object toggle (`vic20mem.c` 523), invisible to `--wrap`.
- The monitor reaches the toggle through `maincpu_monitor_interface->toggle_watchpoints_func`, assigned at `maincpu.c` 395 (read here); LEGION2 also cites `mainc64cpu.c` 622 for x64sc, which I did not find by grep in `c64/mainc64cpu.c` and leave to LEGION2's receipt.

## BDP order

1. Mocks first (07:54): `watch-toggle` re-specified (no `foreign`, kinds stay `read`, `watch,<flag>` emitted), new `watch-flag1-after-rebuild` and `foreign-row`, three lenient variants; the rows contract adapts them (15 fixtures). Implementation run before the wrapper change: link failure `collect2: error: ld returned 1 exit status` on the new `--wrap=mem_toggle_watchpoints` with no `__wrap_mem_toggle_watchpoints` defined (all 25 implementation cases failed), mocks passed.
2. Wrapper (07:56): `learn_watch_rows` (real pointer differing from the dummy pointer after the real rebuild or toggle is the watch row), `__wrap_mem_toggle_watchpoints` (real first, learn on nonzero flag, re-point the dummy pointers at once, emit `watch,<flag>`), the per-cycle foreign check admitting the learned watch row. Double gains a foreign row hook, driver gains `foreign`.
3. Gates (07:57): full `ViceSharp.NativeClockProbe.Tests` 211 passed, 0 failed, 0 skipped (adds 2 single-row mocks, 2 row mocks, 2 single-row and 2 row implementation cases to S1b's 203); `git diff --check` clean; gcc `-std=c11 -Wall -Wextra -Werror` clean for both fixtures.

## Open

- Windows run of S1b and S1c (LEGION2 after pull). S2 build script with the four wraps. S3 proof items (a) and (b).

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 96%, accuracy 97%, completeness 94%
