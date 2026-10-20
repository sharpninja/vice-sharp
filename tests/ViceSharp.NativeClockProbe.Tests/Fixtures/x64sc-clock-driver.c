/* Test fixture only. Drives the wrapped vicii_cycle the way c64cpusc.c CLK_INC does
 * (the returned flags are what the core ORs into maincpu_ba_low_flags) and records the
 * observer's view: the clock at the hook and the BA flags the hook published. Not VICE. */
#include <errno.h>
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

uint64_t maincpu_clk;
uint64_t fake_after_real;
int fake_ba_flags;
extern int vicii_cycle(void);
extern int vice_sharp_vicii_ba_flags;

void vice_sharp_observe_clock(uint64_t clock)
{
    printf("observe,%" PRIu64 ",%d\n", clock, vice_sharp_vicii_ba_flags);
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

static int parse_int(const char *text, int *value)
{
    char *end;
    long parsed;
    errno = 0;
    parsed = strtol(text, &end, 10);
    if (errno || text == end || *end != '\0' || parsed < 0 || parsed > 2147483647L) {
        return 0;
    }
    *value = (int)parsed;
    return 1;
}

int main(int argc, char **argv)
{
    int i;
    if ((argc % 3) != 1) {
        return 2;
    }
    for (i = 1; i < argc; i += 3) {
        int returned;
        if (!parse_u64(argv[i], &maincpu_clk) || !parse_u64(argv[i + 1], &fake_after_real) ||
            !parse_int(argv[i + 2], &fake_ba_flags)) {
            return 2;
        }
        returned = vicii_cycle();
        printf("end,%" PRIu64 ",%d\n", maincpu_clk, returned);
    }
    return ferror(stdout) ? 3 : 0;
}
