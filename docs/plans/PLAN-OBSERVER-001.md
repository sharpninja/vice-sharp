# PLAN-OBSERVER-001: Phase C step 3 under option B (runtime table interposition with the fast path withheld)

Prepared 2026-10-19T20:45 on LAB-OMARCHY at `develop` `3ef73d6` while Payton's ruling on the Phase C step 3 blocker is open, so that a choice of B starts work at once. Scope of record: `HANDOFF.lockstep-plan.md` section 4 Phase C steps 3 to 5, as amended by `HANDOFF.lockstep-plan.v2.md` section 4 and the section 11 decisions (D3 no hostile validation until the scope is code complete, D4 approved, D6 develop only, D8 accuracy first). Evidence base: `docs/receipts/lockstep/20261019T201000-phase-c-step3-windows/` (LEGION2 nm probe), `20261019T203900-phase-c-step3-option-analysis-linux/` (source analysis, cross-checked by LEGION2), the step 1 contracts (`OBSERVATION-RECORD-SLICE.md`, `OBSERVATION-LINE-SLICE.md`) and the step 2 binding (`b2ddf745`). This plan is not approved until Payton rules B; it is a plan, not code.

## 1. Mechanism (decision-complete)

1. Build target: the separate materialization `artifacts/immutable-vice/cc67418c.../source` on each host (byte-identical on both, `docs/receipts/native-vice/20261019T202542-linux-materialization/`), stock objects linked with external translation units only; `native/vice` untouched; every generated file inventoried and outside both trees.
2. Fast path withheld: `ld --wrap=mem_mmu_translate`. `__wrap_mem_mmu_translate` calls `__real_mem_mmu_translate` and then sets `*limit = 0` (and `*base = NULL`), so `JUMP` (`maincpu.c` 83 to 89) and `maincpu_resync_limits` (519 to 524) leave `bank_limit` 0 and every `FETCH_OPCODE` and `FETCH_PARAM` takes the `LOAD` branch (`vic20cpu.c` 125 to 134; `6510core.c` 2081 to 2107, 537 to 541). Emulated semantics unchanged by design; proven, not assumed (section 3).
3. Access capture: after `mem_initialize_memory` the wrapper (hooked through the existing `__wrap_vic_cycle` entry on first call, or a wrapped `machine_specific_init`, decided in slice S1 by nm evidence) reads the four global table pointers (`vic20mem.c` 99 to 102; `c64memsc.c` 102 to 105), copies the 0x101-entry tables, and points the pointers at wrapper-owned tables of trampolines. Each trampoline records `kind` (read, write, dummy read, dummy write, by which table it serves), `address`, the data byte (the original read's return value, or the stored value), and `maincpu_clk` before calling the original. The dummy tables are interposed the same way; `mem_toggle_watchpoints` is itself wrapped so a later monitor toggle re-interposes instead of restoring the stock tables.
4. Half-cycle and phase: the per-cycle hook (`__wrap_vic_cycle` for xvic; the `vicii_cycle` equivalent for x64sc, named by the nm probe) advances the half-cycle index and emits the `VICE_OBS` line of the step 1 contract once per CPU cycle with the accesses captured since the previous hook, PHI1 for the dummy-read-only or idle half and PHI2 for the access half as VICE's single-cycle model exposes them; where VICE performs no access in a cycle the kind is `idle` and `data` is what the contract requires on idle half-cycles (the step 1 self-assessment's open item, resolved by evidence in S3: either the last bus value VICE exposes or a fail-closed `Missing:Data`, never an invented value).
5. Interrupt mask and stolen cycles: `maincpu_int_status->global_pending_int` (reachable global per the nm probe) for `interruptMask`; `stolen` from `maincpu_ba_low_flags` and `vicii_steal_cycles` on x64sc; on the VIC-20 `ba` and `stolen` follow Payton's Ba/Aec applicability ruling (constant 0 only if he rules "not applicable", with the contract amended to say so; otherwise `Missing:Ba` fail closed).
6. Output: `VICE_OBS` lines on stdout in binary mode on Windows (CRLF is a framing fault), through the retained stdio adapter path only after its retention contract is fixed (F-04), otherwise through a dedicated file descriptor opened by the wrapper so native stdout is not involved; decided in S2 by the F-04 state.
7. Pause and resume: the existing clock gate (`tools/immutable-vice/clock-gate.c`) stays the permission mechanism; the observation line precedes the gate's `VICE_GATE` record for the same cycle, so the host reads the record before permitting the next cycle (deterministic per-cycle pause and resume replacing `-limitcycles`).

## 2. Slices and gates (BDPv4, mocks first, no hostile validation until step 12)

- S0 Registration audit (both hosts, read only): enumerate every caller of the page registration that sets a non-null `read_base` (`vic20mem.c` 314 to 320 and the x64sc equivalent in `c64memsc.c`) and classify each mapped page as side-effect-free or not; receipt with file and line per caller. A side-effecting page on the fast path is a named blocker for B.
- S1 Wrapper contract, mock first (NativeClockProbe.Tests): trampoline capture record and ordering fixtures (read, write, dummy, address, data, clock), the `limit 0` translate rule, the toggle re-interposition rule, with negative controls (wrong kind, dropped access, wrong clock, restored stock table); then the compiled C fixture binding through `CompiledCFixture` on both hosts.
- S2 Build: PowerShell build script per host (`native/build-vice-observer.ps1`, canonical; Linux gcc, Windows MSYS2 gcc by full path) linking the stock objects with `clock-observer.c`, `clock-gate.c`, the new `bus-observer.c` and `--wrap=vic_cycle,--wrap=mem_mmu_translate,--wrap=mem_toggle_watchpoints` (x64sc adds its cycle hook); link launch and result JSON, object hashes, inventory before and after.
- S3 Equivalence proof: ROM-only workloads from `docs/requirements/oracles/vic20-xvic-manifest.json` and the C64 reset workload run twice, with and without the wrapped translate, with the gate permitting every cycle: register, clock and `VICE_GATE` traces byte-identical, total cycles equal, and the S0 classification cited; any difference is a named blocker.
- S4 Real observations (step 4): the same workloads with capture on, raw `VICE_OBS` traces and hashes retained, validated by the step 1 validator (zero `Missing` or `Malformed` on the fields the ruling requires), stock binaries untouched.
- S5 Legacy adapter (step 5): the external `MAINCPU` adapter remains the only compatibility path, proven against the captured traces.
- Each slice: focused filter green with zero skipped, full applicable suite green on both hosts, inventories unchanged, receipt with self-assessment; scores below 98 are listed gaps.

## 3. Risks and named blockers

- A side-effecting read on a `read_base` page (S0) or a trace difference in S3: blocker, reported, no workaround by inference.
- Performance: every access through a trampoline slows VICE; acceptable for gated lockstep runs, measured in S4.
- The dummy tables' coverage of every dummy access depends on VICE routing all dummy reads through `_mem_read_tab_ptr_dummy`; S1 fixtures include `FETCH_PARAM_DUMMY` with the fast path withheld (`6510core.c` 538 to 541) and the `STORE_DUMMY` path, cited by line in the receipt.
- F-04 (stdout retention) decides the output channel (section 1, item 6).

## 4. Operator-confirm items

1. Rule B (this plan), C, or D for step 3.
2. Rule Ba/Aec applicability for the VIC-20.
3. Approve the output-channel choice in section 1 item 6 once F-04's state is known, or choose the dedicated descriptor now.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 93%, accuracy 97%, completeness 90%
