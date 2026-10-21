/* Test fixture only. Not VICE. Stands in for the pinned core's trap registration and snapshot writer: the fake
 * interrupt_maincpu_trigger_trap records the callback (the driver dispatches it at the scripted boundary clock,
 * as the core would at the next instruction boundary whose copied pending mask includes the trap), and the fake
 * machine_write_snapshot records the path and returns the scripted result. The sink prints to stdout. */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>

extern uint64_t maincpu_clk;
extern int fake_write_result;
extern int trap_registrations;
extern void (*registered_trap)(uint16_t, void *);
extern void *registered_data;

void interrupt_maincpu_trigger_trap(void (*trap_func)(uint16_t, void *data), void *data)
{
    trap_registrations++;
    registered_trap = trap_func;
    registered_data = data;
    printf("trap-registered,%" PRIu64 "\n", maincpu_clk);
}

int machine_write_snapshot(const char *name, int save_roms, int save_disks, int event_mode)
{
    (void)save_roms;
    (void)save_disks;
    (void)event_mode;
    printf("write,%s\n", name);
    return fake_write_result;
}

void vice_sharp_snapshot_sink(const char *line)
{
    puts(line);
}
