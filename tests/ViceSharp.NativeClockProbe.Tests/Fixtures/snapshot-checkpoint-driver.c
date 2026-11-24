/* Test fixture only. Not VICE. argv: spec ("-" for unset) start end boundary write_result.
 * Configures the checkpoint unit directly (the environment path is the same code), advances the clock from start
 * to end calling the per-cycle hook as bus-adapter.c does, dispatches a registered trap when the clock equals the
 * boundary (the core's next eligible instruction boundary), and prints the registration count at the end. */
#include <errno.h>
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

uint64_t maincpu_clk;
int fake_write_result;
int trap_registrations;
void (*registered_trap)(uint16_t, void *);
void *registered_data;

extern void vice_sharp_checkpoint_configure(const char *spec);
extern void vice_sharp_checkpoint_on_clock(uint64_t clock);

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
    uint64_t start, end, boundary, clock;
    long result;
    char *tail;
    if (argc != 6) {
        return 2;
    }
    if (!parse_u64(argv[2], &start) || !parse_u64(argv[3], &end) || !parse_u64(argv[4], &boundary) || end < start) {
        return 2;
    }
    errno = 0;
    result = strtol(argv[5], &tail, 10);
    if (errno || *tail != '\0') {
        return 2;
    }
    fake_write_result = (int)result;
    vice_sharp_checkpoint_configure(strcmp(argv[1], "-") == 0 ? NULL : argv[1]);
    for (clock = start; clock <= end; clock++) {
        maincpu_clk = clock;
        vice_sharp_checkpoint_on_clock(clock);
        if (registered_trap != NULL && clock == boundary) {
            void (*trap)(uint16_t, void *) = registered_trap;
            registered_trap = NULL;
            trap(0, registered_data);
        }
    }
    printf("end,%d\n", trap_registrations);
    return ferror(stdout) ? 3 : 0;
}
