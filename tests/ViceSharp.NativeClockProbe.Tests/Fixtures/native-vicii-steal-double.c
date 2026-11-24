/* Separate translation unit so the wrapper's calls to vicii_cycle cross an object boundary. Not VICE.
 * Stands in for the VIC-II during a steal: each vicii_cycle call prints the clock it sees and returns
 * the next scripted BA value; the real vicii_steal_cycles is defined here too, so the link has a
 * __real_ to satisfy, and it fails loudly if ever reached (the wrapper must never forward to it). */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

extern uint64_t maincpu_clk;
extern int fake_ba_sequence[];
extern int fake_ba_count;
static int next_ba;

int vicii_cycle(void)
{
    int ba = next_ba < fake_ba_count ? fake_ba_sequence[next_ba] : 0;
    next_ba++;
    printf("real,%" PRIu64 ",%d,0\n", maincpu_clk, ba);
    return ba;
}

void vicii_steal_cycles(void)
{
    fprintf(stderr, "real vicii_steal_cycles reached; the wrapper must not forward\n");
    exit(9);
}
