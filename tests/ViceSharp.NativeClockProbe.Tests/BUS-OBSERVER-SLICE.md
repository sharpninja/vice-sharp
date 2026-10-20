# Bus observer contract (Phase C step 3, PLAN-OBSERVER-001 slice S1)

Scope: the external wrapper that records every CPU bus access of the immutable VICE oracle without touching VICE source (option B, ruled by Payton 2026-10-20T07:33, handoff D14). Mock first; the C wrapper `tools/immutable-vice/bus-observer.c` binds to the same fixtures through a VICE-shaped double, never VICE itself. Serves FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3 and AC5, TEST-VICE-001 AC4. Source citations are to the materialized commit `cc67418c` (receipt `docs/receipts/lockstep/20261019T205400-phase-c-step3-registration-audit-linux/`).

## Rules under test

1. Capture: after the real table rebuild (`mem_initialize_memory`) returns, every entry of the rows the live read and write pointers resolve to is replaced in place by a trampoline that records `kind, address, data, clock` and calls the saved original. Rows are VICE's own static arrays; the pointers are never replaced (they are reassigned intra-object, `vic20mem.c` 539 to 554, `c64memsc.c` 169 to 184).
2. Timing: nothing is patched while the real rebuild runs, because `c64memsc.c` 613 to 736 compares entries by identity during it. The double counts trampolines present at the end of its real rebuild and reports `init,<count>`; the only correct value is 0.
3. Fast path withheld: the wrapped `mem_mmu_translate` returns limit 0 and base null (`translate,0,0`) so every fetch takes the table route.
4. Dummy accesses: VICE reaches them through `_mem_read_tab_ptr_dummy` and `_mem_write_tab_ptr_dummy` (`maincpu.c` 171 to 187), which alias the real rows. The wrapper re-points the two dummy pointers at its own trampoline rows after each rebuild and in the per-cycle hook; while they alias the real rows the kind is unknowable and the access is reported with kind `missing`, never guessed.
5. Rescan: the per-cycle hook (reached from `vice_sharp_observe_clock`, ahead of the gate record) emits the cycle's accesses, then rescans the known rows: each unpatched entry is reported as `missing,<read|write>,<page>` and re-patched; a live pointer outside the known rows is reported as `foreign,<read|write>` every cycle it is live; `repatch,<entries>,<dummyRepointed>` is emitted only when something changed; then `cycle,<clock>`.

## Event lines

`init,<n>` (double), `repatch,<entries>,<0|1>`, `access,<read|write|dummy-read|dummy-write|missing>,<address>,<data>,<clock>`, `translate,<limit>,<baseNonNull 0|1>`, `missing,<read|write>,<page>`, `foreign,<read|write>`, `cycle,<clock>`. Decimal, positional, one `\n` per line.

## Mock gate (`BusObserverContractTests`)

10 fixtures (empty, init-only, read, fetch, write-and-dummy, translate, rebuild-at-runtime, rewrite-outside-init, dummy-alias, watch-toggle); per fixture the correct mock passes and every single-event corruption is rejected (wrong kind, wrong detail, dropped, duplicated, reordered), plus wrong exit code, null results, an extra event and a mutated input; six lenient variants of the fail-closed rules are rejected explicitly; a coverage guard fixes the event vocabulary.
