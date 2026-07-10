# Receipts: PLAN-NATIVERESIDUE-002 (drive-clock + shim residue hardening)

Branch `fix/nativeresidue-002-drive-clock-hardening` off `main`. Closes the two
latent VICE drive-clock bugs parked in HANDOFF.md and targets BUG-LOCKSTEP-001.

## Mechanism (verified against current source)

Two drive-side residue channels survive the single-process oracle's machine
destroy/create because `drive_t` and `drivecpu_context_t` live in process-global
`diskunit_context[]` and the create allow-list (vice-shim.c) never touched them.

- Channel A (drive clocks): `drive_snapshot_read_module` declares
  `attach_clk/detach_clk/attach_detach_clk[NUM_DISK_UNITS]` uninitialized
  (drive-snapshot.c:317-319), writes them only under `if (has_tde[unr])`, then
  copies them unconditionally into `drive_t` for every `type != DRIVE_TYPE_NONE`
  unit (540-552). A `has_tde=0` restore copies uninitialized stack. Disk
  attach/detach and `has_tde=1` restores also leave nonzero clocks that persist
  into the next machine.
- Channel B (cycle_accum): `drivecpu_reset_clk` (drivecpu.c:186-191) re-anchors
  last_clk/last_exc_cycles/stop_clk but omits `cycle_accum` (the 16.16 fractional
  carry). `drivecpu_reset` zeroes the drive clock (`*clk_ptr = 0`) right before
  calling reset_clk, so the fraction must reset with it. The create-time TDE
  re-baseline invokes reset_clk (`resources_set_int` -> `set_drive_true_emulation`
  -> reset_clk; `resources_set_internal_int` calls the setter unconditionally),
  so the stale fraction leaks to every machine created after a resume.

Both feed the IEC serial timeline that produces the `$DD00` bit-7 read, the
`$47/$C7` lockstep fingerprint at cycle 167.

## Fix

- `drive-snapshot.c:317-319`: zero-init the three stack arrays (`= {0}`).
- `drivecpu.c` + `drivecpu65c02.c` `*_reset_clk`: add `cpu->cycle_accum = 0;`.
- `vice-shim.c vice_machine_create_model`: after the TDE loop, re-baseline
  `attach_clk/detach_clk/attach_detach_clk = 0` for units 8-11 (closes the
  has_tde=1 / disk-detach persistence hole the vendored patch cannot).
- `vice-shim.c vice_machine_destroy`: close `g_shim_sid_psid` (SID oracle
  renderer), symmetric with `vice_machine_reset`.
- New test-only exports: `vice_drive_get_clock_residue`,
  `vice_drivecpu_get_cycle_accum`, `vice_drivecpu_set_cycle_accum`.

Vendored edits are in `native/patches/vice-shim-runtime.patch` (5 pre-existing
hunks byte-identical; 3 drive files added) with a reapplication section in
`vice-shim-runtime.prompt.md`.

## dll SHA256 ledger

- Baseline (pre-slice): `8a60b527045d7a68d33a13f870ea8ce8970abb25d1cb011674474efe41e5cf14`
- Commit 1 (exports only): `af036cedd438bf4819747325e9306f24192de75422c8bfc1610283cbd27a8705`
- Red verify (fix disabled): `13913efe2a23282493bd0e6b854d6f309fbaa98d3cc52537982d801c968ee28c`
- Green (fix + setter): `7a81ef098cf40784219a586632748d74fdde55e3fba00e16f7d714d3a94d9726`

## Red -> green (focused, NativeResidueDiagTests)

- Commit 1 dll (exports, no fix): TEST-NATIVE-RESIDUE-03 got
  `attach=99746 detach=99746 attachDetach=99746`; TEST-NATIVE-RESIDUE-04 got
  `cycle_accum 30820` leaking scratch->check. 2 failed / 2 passed.
- Red-verify dll (fix disabled, setter present): TEST-NATIVE-RESIDUE-04 planted
  `0x1234` leaked to a fresh machine (`got 4660`). Deterministic red.
- Green dll (fix + setter): all 4 NativeResidueDiag tests pass (0 failed).
  - 03 Phase 1 (attach/detach leak) green; Phase 2 (has_tde=0 stack) green.
  - 04 (setter poison) green.

Note: cycle_accum cannot be driven nonzero from managed single-stepping and no
shipped `.vsf` carries a nonzero value (measured postResume/maxDuringStep/
preDestroy all 0), so the commit-1 `30820` was pure inter-machine residue. The
setter injects a deterministic poison for a same-session red/green guard.

## Full baseline gate (BUG-LOCKSTEP-001 closure)

Command:
`dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`

Pre-fix red baseline (docs/receipts-test-baseline-2026-07-07.md:13-14): 136
`X64ScVariantLockstepTests` + 2 `LockstepValidationTests` failing in the combined
run (cycle 167, `A=$47 vs $C7`), green standalone.

Post-fix result (dll 7a81ef09, single-process full suite, 2026-07-09):
**0 failed, 2596 passed, 21 skipped, total 2617.** Zero `X64ScVariantLockstepTests`
or `LockstepValidationTests` failures. The order-dependent lockstep cluster that
was red only in combined runs is now green in one process. BUG-LOCKSTEP-001
closed. Total 2617 = prior 2612-2615 band + 2 new residue tests; skips unchanged
at 21 (ROM/native auto-skips).

## Build gotcha

`native/.build-nopatch.sh` (`make x64sc-program`) does NOT rebuild changed
VICE-core objects; their `.o` stay stale (June 1) and the dll relinks stale code.
Delete the changed `.o` and `libdrive.a` before rebuilding, and run the build
under the MSYS2 MINGW64 login shell (`MSYSTEM=MINGW64 bash -lc`) so the compiler
gets a writable `/tmp` (a plain shell leaves `TMP` pointing at `C:\WINDOWS`).

## Out-of-scope residue candidates (recorded, not fixed)

- Live `unit->type` restored from a snapshot is never re-baselined at create
  (a snapshot enabling unit 9 would leave a phantom drive in later machines).
- `drivecpu65c02.c` snapshot write/read width asymmetry for `cycle_accum`
  (`SMW_DW` 32-bit at 457 vs `SMR_CLOCK` 64-bit at 524), an upstream format defect.
