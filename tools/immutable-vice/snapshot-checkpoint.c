/* External link unit only. Never copy into the immutable VICE source tree.
 * PLAN-NATIVERESTORE-001 D1, decision R12 (literal checkpoint cycle) with N15, N21, N22
 * and N28. TR-VSFLOCKSTEP-RESUME-001 AC1; PLAN-VSFLOCKSTEP-001 Slice 1 fixtures.
 * Armed only by VICESHARP_SNAPSHOT_AT=<trigger>:<path>; inert otherwise. When the gate
 * clock first reaches the trigger, registers one trap through the pinned public API
 * interrupt_maincpu_trigger_trap (interrupt.h 305). The pinned core runs it at the next
 * instruction boundary whose copied pending mask includes it (6510dtvcore.c 354 to 412 and
 * 1758 to 1763), after EXPORT_REGISTERS, so the snapshot the callback writes through
 * machine_write_snapshot (machine.h 145) carries the complete register file. The write
 * clock is measured inside the callback and is never equated to the trigger; every nonzero
 * writer result (including the +1 of vic20-snapshot.c 99 to 102) is reported as
 * checkpoint-fail. Nothing re-arms. Records go through vice_sharp_snapshot_sink. */
#include <inttypes.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

extern uint64_t maincpu_clk;
extern void interrupt_maincpu_trigger_trap(void (*trap_func)(uint16_t, void *data), void *data);
extern int machine_write_snapshot(const char *name, int save_roms, int save_disks, int event_mode);
extern void vice_sharp_snapshot_sink(const char *line);

static int configured;      /* 1 once configure ran (from the environment or a driver) */
static int armed;
static int requested;
static uint64_t trigger;
static char path[512];

static void checkpoint_trap(uint16_t address, void *data)
{
    char line[600];
    int result;
    (void)address;
    (void)data;
    result = machine_write_snapshot(path, 0, 0, 0);
    if (result == 0) {
        snprintf(line, sizeof line, "VICE_SNAP,checkpoint,%" PRIu64 ",%" PRIu64, trigger, maincpu_clk);
    } else {
        snprintf(line, sizeof line, "VICE_SNAP,checkpoint-fail,%" PRIu64 ",%" PRIu64 ",%d", trigger, maincpu_clk, result);
    }
    vice_sharp_snapshot_sink(line);
}

/* spec is "<trigger>:<path>" or NULL (unset). A malformed spec reports checkpoint-fail,0,0,-2
 * once and leaves the unit inert. */
void vice_sharp_checkpoint_configure(const char *spec)
{
    const char *colon;
    char *end;
    unsigned long long value;
    configured = 1;
    armed = 0;
    requested = 0;
    if (spec == NULL) {
        return;
    }
    colon = strchr(spec, ':');
    if (colon == NULL || colon == spec || colon[1] == '\0' || (size_t)(strlen(colon + 1)) >= sizeof path) {
        vice_sharp_snapshot_sink("VICE_SNAP,checkpoint-fail,0,0,-2");
        return;
    }
    value = strtoull(spec, &end, 10);
    if (end != colon) {
        vice_sharp_snapshot_sink("VICE_SNAP,checkpoint-fail,0,0,-2");
        return;
    }
    trigger = (uint64_t)value;
    strncpy(path, colon + 1, sizeof path - 1);
    path[sizeof path - 1] = '\0';
    armed = 1;
}

void vice_sharp_checkpoint_on_clock(uint64_t clock)
{
    if (!configured) {
        vice_sharp_checkpoint_configure(getenv("VICESHARP_SNAPSHOT_AT"));
    }
    if (armed && !requested && clock >= trigger) {
        requested = 1;
        interrupt_maincpu_trigger_trap(checkpoint_trap, NULL);
    }
}
