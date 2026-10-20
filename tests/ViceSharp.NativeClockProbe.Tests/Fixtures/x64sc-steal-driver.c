/* Test fixture only. argv: start clock, then the BA values the VIC double returns per stolen cycle
 * (the last must be 0). Calls the wrapped vicii_steal_cycles once, as mainc64cpu.c 118 does, and
 * prints the observer's view per inner cycle and the state after the return. Not VICE. */
#include <errno.h>
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

uint64_t maincpu_clk;
int fake_ba_sequence[256];
int fake_ba_count;
extern void vicii_steal_cycles(void);
extern int vice_sharp_vicii_ba_flags;
extern int vice_sharp_stolen_cycle;

void vice_sharp_observe_clock(uint64_t clock)
{
    printf("observe,%" PRIu64 ",%d,%d\n", clock, vice_sharp_vicii_ba_flags, vice_sharp_stolen_cycle);
}

static int parse_u64(const char *text, uint64_t *value)
{
    char *end;
    unsigned long long parsed;
    errno = 0;
    parsed = strtoull(text, &end, 10);
    if (errno || text == end || *end != '\0') {
        return 0;
    }
    *value = (uint64_t)parsed;
    return 1;
}

int main(int argc, char **argv)
{
    int i;
    if (argc < 3 || argc > 2 + 256) {
        return 2;
    }
    if (!parse_u64(argv[1], &maincpu_clk)) {
        return 2;
    }
    for (i = 2; i < argc; i++) {
        uint64_t v;
        if (!parse_u64(argv[i], &v) || v > 0x7fffffffu) {
            return 2;
        }
        fake_ba_sequence[fake_ba_count++] = (int)v;
    }
    if (fake_ba_sequence[fake_ba_count - 1] != 0) {
        return 2;
    }
    vicii_steal_cycles();
    printf("end,%" PRIu64 ",0,%d\n", maincpu_clk, vice_sharp_stolen_cycle);
    return ferror(stdout) ? 3 : 0;
}
