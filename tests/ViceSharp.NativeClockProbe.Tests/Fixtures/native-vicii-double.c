/* Separate translation unit prevents an in-unit call from bypassing --wrap. Not VICE.
 * Stands in for vicii_cycle (viciisc/vicii-cycle.c): prints the clock it sees, changes
 * the clock as the driver scripted, and returns the scripted BA flags. */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>

extern uint64_t maincpu_clk;
extern uint64_t fake_after_real;
extern int fake_ba_flags;

int vicii_cycle(void)
{
    printf("real,%" PRIu64 ",%d\n", maincpu_clk, fake_ba_flags);
    maincpu_clk = fake_after_real;
    return fake_ba_flags;
}
