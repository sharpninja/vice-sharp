/* S2a driver for the real-pipe binding of observer, bus adapter, unmodified gate adapter and gate.
 * Not VICE source. Stands in for native code: rebuilds the double's tables (wrapped), then for
 * clocks 7 and 8 performs one read through the live table row and calls vic_cycle (wrapped by
 * clock-observer.c, whose vice_sharp_observe_clock call is wrapped by bus-adapter.c), writing
 * NATIVE_AFTER,<clock> to stdout after each return. Real stdin, stdout and stderr; no seam. With
 * argv[1] == "wait" it first reads one stdin byte so a test can close the stderr pipe before the
 * first sink write (broken-pipe case). The double prints "init,<n>" to stdout during the rebuild.
 */
#include <stdint.h>
#include <stdio.h>
#include <string.h>

typedef uint8_t (*read_func_ptr_t)(uint16_t addr);

uint64_t maincpu_clk;
extern read_func_ptr_t *_mem_read_tab_ptr;
extern void mem_initialize_memory(void);
extern void vic_cycle(void);

int main(int argc, char **argv)
{
    if (argc > 1 && strcmp(argv[1], "wait") == 0) {
        (void)fgetc(stdin);
    }
    mem_initialize_memory();
    for (maincpu_clk = 7; maincpu_clk <= 8; maincpu_clk++) {
        (void)_mem_read_tab_ptr[0x12]((uint16_t)0x1234);
        vic_cycle();
        printf("NATIVE_AFTER,%u\n", (unsigned)maincpu_clk);
        fflush(stdout);
    }
    return 0;
}
