/* External link wrapper only. Never copy into the immutable VICE source tree.
 * FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001 AC2/AC3/AC5; TEST-VICE-001 AC2/AC4.
 * Link with --wrap=vic_cycle. Pinned upstream cc67418cba58e71d8e70fd353e9de47199ad2075:
 * vice/src/types.h defines CLOCK as uint64_t; vic20cpu.c calls vic_cycle per CLK_INC.
 * Forward unchanged native behavior before reporting the actual clock. This does
 * not manufacture bus observations, CPU registers, dummy flags or half-cycle phases.
 */
#include <stdint.h>

extern uint64_t maincpu_clk;
extern void __real_vic_cycle(void);
extern void vice_sharp_observe_clock(uint64_t clock);

void __wrap_vic_cycle(void)
{
    __real_vic_cycle();
    vice_sharp_observe_clock(maincpu_clk);
}
