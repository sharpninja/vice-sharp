/* External link wrapper only. Never copy into the immutable VICE source tree.
 * x64sc counterpart of clock-observer.c. FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001
 * AC2/AC3/AC5; TEST-VICE-001 AC2/AC4. Link with --wrap=vicii_cycle and
 * --wrap=vicii_steal_cycles.
 * Pinned upstream cc67418cba58e71d8e70fd353e9de47199ad2075: c64cpusc.c 47 to 51,
 * CLK_INC() { interrupt_delay(); maincpu_clk++; maincpu_ba_low_flags &= ~MAINCPU_BA_LOW_VICII;
 * maincpu_ba_low_flags |= vicii_cycle(); }. vicii_cycle (viciisc/vicii-cycle.c) returns
 * the BA flags the core ORs in after the call returns, so at the hook they are not yet in
 * maincpu_ba_low_flags; the wrapper publishes the returned value for the adapter and
 * returns it unchanged. Forward unchanged native behavior before reporting the clock.
 */
#include <stdint.h>

extern uint64_t maincpu_clk;
extern int __real_vicii_cycle(void);
extern void vice_sharp_observe_clock(uint64_t clock);

int vice_sharp_vicii_ba_flags;
int vice_sharp_stolen_cycle;

int __wrap_vicii_cycle(void)
{
    int ba_flags = __real_vicii_cycle();
    vice_sharp_vicii_ba_flags = ba_flags;
    vice_sharp_observe_clock(maincpu_clk);
    return ba_flags;
}

/* PLAN-OBSERVER-001 S4a. vicii_steal_cycles (viciisc/vicii-cycle.c 628 to 636) is
 *     do { maincpu_clk++; ba_low = vicii_cycle(); } while (ba_low);
 * with the vicii_cycle call inside its own object, invisible to --wrap, so stolen cycles
 * would reach neither the hook nor the gate. The CPU core calls it cross-object
 * (mainc64cpu.c 118 in c64cpusc.o); this wrapper executes the same three statements and
 * calls vicii_cycle cross-object, so every stolen cycle runs through __wrap_vicii_cycle
 * with the stolen flag set. The real function is never called. This is the only place the
 * wrappers restate VICE statements instead of forwarding them; proven on a display-on
 * workload by the S3 runner (no clock gap, identical traces). */
extern int vicii_cycle(void);

void __wrap_vicii_steal_cycles(void)
{
    int ba_low;
    vice_sharp_stolen_cycle = 1;
    do {
        maincpu_clk++;
        ba_low = vicii_cycle();
    } while (ba_low);
    vice_sharp_stolen_cycle = 0;
}
