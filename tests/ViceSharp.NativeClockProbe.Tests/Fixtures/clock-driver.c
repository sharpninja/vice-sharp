/* Test fixture only. Independent call/observer spies, not VICE. */
#include <errno.h>
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

uint64_t maincpu_clk;
uint64_t fake_after_real;
extern void vic_cycle(void);

void vice_sharp_observe_clock(uint64_t clock)
{
    printf("observe,%" PRIu64 "\n", clock);
}

static int parse_clock(const char *text, uint64_t *value)
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
    if ((argc % 2) != 1) {
        return 2;
    }
    for (i = 1; i < argc; i += 2) {
        if (!parse_clock(argv[i], &maincpu_clk) ||
            !parse_clock(argv[i + 1], &fake_after_real)) {
            return 2;
        }
        vic_cycle();
        printf("end,%" PRIu64 "\n", maincpu_clk);
    }
    return ferror(stdout) ? 3 : 0;
}
