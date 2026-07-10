Use this prompt when `native/patches/vice-shim-runtime.patch` no longer
applies cleanly to the vendored classic VICE source.

Goal: reintroduce the hosted runtime hooks needed by `native/vice-shim.c`
without changing non-hosted behavior.

Apply these changes against the classic VICE source tree rooted at
`native/vice/vice`:

1. Update `vice/src/c64/c64cpusc.c`.
   Add this include immediately after `#include "vicii-cycle.h"`:
   ```c
   #ifdef VICE_SHIM_HOSTED
   #include "../vice-shim-runtime.h"
   #endif
   ```

   Replace the existing `CLK_INC()` macro with a hosted/non-hosted split:
   - In the `VICE_SHIM_HOSTED` branch, keep the existing cycle accounting,
     then call `EXPORT_REGISTERS()`, invoke
     `vice_shim_cycle_checkpoint()`, return immediately when it asks to
     stop, and finally call `IMPORT_REGISTERS()`.
   - In the non-hosted branch, preserve the original `CLK_INC()` behavior
     exactly.

2. Update `vice/src/mainc64cpu.c`.
   Add this include near the other headers, after `#include "types.h"`:
   ```c
   #ifdef VICE_SHIM_HOSTED
   #include "vice-shim-runtime.h"
   #endif
   ```

   In `maincpu_mainloop()`, replace the unconditional
   `machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);` call with:
   - A `VICE_SHIM_HOSTED` branch that checks
     `vice_shim_take_bootstrap_maincpu()`.
   - When bootstrap is pending, call `IMPORT_REGISTERS()` and reset the
     hosted runtime state used by the shim:
     `maincpu_jammed = 0;`
     `last_opcode_info = 0;`
     `last_opcode_addr = reg_pc;`
     `stolen_cycles = 0;`
     `check_ba_low = 0;`
     `reu_dma_triggered = 0;`
     `maincpu_rmw_flag = 0;`
   - Otherwise, fall back to the original
     `machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);`
   - Non-hosted builds must still behave exactly like upstream.

3. Add a new header at `vice/src/vice-shim-runtime.h`:
   ```c
   #ifndef VICE_SHIM_RUNTIME_H
   #define VICE_SHIM_RUNTIME_H

   int vice_shim_cycle_checkpoint(void);
   int vice_shim_take_bootstrap_maincpu(void);

   #endif
   ```

4. Drive-clock residue fixes (PLAN-NATIVERESIDUE-002, TEST-NATIVE-RESIDUE-03/04).
   These make the drive-side clock state deterministic across the single-process
   oracle's machine boundaries; upstream VICE leaves it stale.
   - `vice/src/drive/drive-snapshot.c`, in `drive_snapshot_read_module`:
     zero-initialize the three stack arrays so a `has_tde=0` restore copies 0
     (the fresh-boot baseline) into `drive_t` instead of uninitialized stack:
     ```c
     CLOCK attach_clk[NUM_DISK_UNITS] = {0};
     CLOCK detach_clk[NUM_DISK_UNITS] = {0};
     CLOCK attach_detach_clk[NUM_DISK_UNITS] = {0};
     ```
   - `vice/src/drive/drivecpu.c`, in `drivecpu_reset_clk`: add
     `drv->cpu->cycle_accum = 0;` after the `stop_clk` reset. `drivecpu_reset`
     already zeroes the drive clock (`*(drv->clk_ptr) = 0`), so the fractional
     carry must reset with it; the snapshot-read path re-writes it immediately
     after via SMR_CLOCK, so restores are unaffected.
   - `vice/src/drive/drivecpu65c02.c`, in `drivecpu65c02_reset_clk`: the same
     `drv->cpu->cycle_accum = 0;` addition (1581/2000/4000/CMDHD drives).
   The remaining machine-boundary persistence (a `has_tde=1` restore or the
   shim's own disk-detach leaving nonzero clocks) is re-baselined directly in
   `native/vice-shim.c` `vice_machine_create_model`, not here.

Validation after reapplying the changes:
- `native/build-vice-shim.sh` should build `native/vice_x64.dll`.
- `dotnet build tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj`
  should succeed.
- Native-backed tests should execute instead of skipping.
