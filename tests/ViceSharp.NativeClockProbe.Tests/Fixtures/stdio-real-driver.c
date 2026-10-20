/* Phase C step 2 driver for the real-stdio binding of the unmodified gate adapter. Not VICE source.
 * Stands in for native code: writes NATIVE_BEFORE to stdout (buffered, never flushed by this driver before a
 * gate stop), then calls vice_sharp_observe_clock for clocks 7 and 8, writing NATIVE_AFTER,<clock> after each
 * return. Real stdin, stdout and stderr; no seam. With argv[1] == "wait" it first reads one stdin byte, so a
 * test can close the stderr pipe before the adapter publishes (broken-pipe case).
 */
#include <stdint.h>
#include <stdio.h>
#include <string.h>

void vice_sharp_observe_clock(uint64_t clock);

int main(int argc, char **argv)
{
    uint64_t clock;
    if (argc > 1 && strcmp(argv[1], "wait") == 0) {
        (void)fgetc(stdin);
    }
    fputs("NATIVE_BEFORE\n", stdout);
    for (clock = 7; clock <= 8; clock++) {
        vice_sharp_observe_clock(clock);
        printf("NATIVE_AFTER,%u\n", (unsigned)clock);
    }
    fflush(stdout);
    return 0;
}
