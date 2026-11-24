/* External link wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-NATIVERESTORE-001 D1, decision R11 B (native register observer at the resume
 * boundary) with N02 and N04. FR-VICE-001 AC2/AC4; TR-VSFLOCKSTEP-RESUME-001 AC1.
 * Link with --wrap=maincpu_snapshot_read_module and --wrap=machine_read_snapshot.
 * Pinned upstream cc67418cba58e71d8e70fd353e9de47199ad2075:
 *  - maincpu_snapshot_read_module (mainc64cpu.c 934, mainviccpu.c 756) writes the loaded
 *    register file into the global maincpu_regs (mainc64cpu.c 513) and is called across
 *    objects from c64-snapshot.c 121 and vic20-snapshot.c 129;
 *  - machine_read_snapshot (machine.h 148) reads every module in order and resets the
 *    machine when a later module fails (c64-snapshot.c 146 to 153); autostart.c 887 calls
 *    it across objects;
 *  - mos6510.h 116 to 136 keeps the zero and sign flags apart from the raw p byte and
 *    recomposes the architectural status as p | (n & 0x80) | P_UNUSED | (zero ? P_ZERO : 0)
 *    where zero is !z; P_UNUSED is 0x20 and P_ZERO is 0x02.
 * The machine glue (obs-fields-<machine>.c, compiled against the real headers) copies the
 * register globals and the effective drive resources; this unit is header-free. Records go
 * through vice_sharp_snapshot_sink (bus-adapter.c: stderr, write and flush checked, stop 72
 * on failure), ahead of the next gate line. Forward unchanged native behavior: the real
 * function is called exactly once and its result is returned untouched. */
#include <inttypes.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>

#include "snapshot-observer.h"

extern uint64_t maincpu_clk;
extern int __real_maincpu_snapshot_read_module(void *s);
extern int __real_machine_read_snapshot(const char *name, int event_mode);
extern void vice_sharp_snapshot_registers(vice_sharp_cpu_regs *out);
extern int vice_sharp_snapshot_drives(char *buffer, size_t size);
extern void vice_sharp_snapshot_sink(const char *line);

uint8_t vice_sharp_snapshot_status(uint8_t p, uint8_t n, uint8_t z)
{
    return (uint8_t)(p | (n & 0x80u) | 0x20u | (z == 0 ? 0x02u : 0u));
}

int __wrap_maincpu_snapshot_read_module(void *s)
{
    char line[96];
    int result = __real_maincpu_snapshot_read_module(s);
    if (result == 0) {
        vice_sharp_cpu_regs regs;
        vice_sharp_snapshot_registers(&regs);
        snprintf(line, sizeof line, "VICE_SNAP,resume,%" PRIu64 ",%u,%u,%u,%u,%u,%u",
                 maincpu_clk, (unsigned)regs.a, (unsigned)regs.x, (unsigned)regs.y, (unsigned)regs.sp,
                 (unsigned)regs.pc, (unsigned)vice_sharp_snapshot_status(regs.p, regs.n, regs.z));
    } else {
        snprintf(line, sizeof line, "VICE_SNAP,resume-fail,%d", result);
    }
    vice_sharp_snapshot_sink(line);
    return result;
}

int __wrap_machine_read_snapshot(const char *name, int event_mode)
{
    char line[160];
    int result = __real_machine_read_snapshot(name, event_mode);
    snprintf(line, sizeof line, "VICE_SNAP,load,%d", result);
    vice_sharp_snapshot_sink(line);
    if (result == 0) {
        char drives[128];
        if (vice_sharp_snapshot_drives(drives, sizeof drives) == 0) {
            snprintf(line, sizeof line, "VICE_SNAP,drives,%s", drives);
            vice_sharp_snapshot_sink(line);
        }
    }
    return result;
}
