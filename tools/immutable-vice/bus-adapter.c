/* External link wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 slice S2a (Phase C step 3, option B, handoff D14).
 * FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001 AC2/AC3/AC5; TEST-VICE-001 AC2/AC4.
 * Carries the bus observer's events to the host ahead of the gate record for the same
 * cycle without modifying the retained gate adapter (gate-observer.c bytes preserved):
 * link with --wrap=vice_sharp_observe_clock. clock-observer.c calls vice_sharp_observe_clock
 * cross-object, so the call lands here; this wrapper runs the observer's per-cycle hook
 * (vice_sharp_bus_cycle: rescan, fail-closed reports, "cycle,<clock>") and then the real
 * gate adapter, which publishes VICE_GATE,<clock> on stderr, flushes, and waits for the
 * host's permission byte. The sink writes "VICE_BUS,<kind>,<detail>\n" to stderr, the
 * stream the gate already publishes on, so the host reads every event of a cycle before
 * it reads that cycle's gate record (plan item 7). A sink write failure must not let
 * execution continue unobserved: it reports VICE_BUS_STOP,72 and terminates with 72
 * (the gate owns 64 to 70). Output-channel choice: plan item 6, operator-confirm item 3.
 */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

extern void __real_vice_sharp_observe_clock(uint64_t clock);
extern void vice_sharp_bus_cycle(uint64_t clock);

void vice_sharp_bus_sink(const char *kind, const char *detail)
{
    if (fprintf(stderr, "VICE_BUS,%s,%s\n", kind, detail) < 0) {
        fprintf(stderr, "VICE_BUS_STOP,%d\n", 72);
        fflush(stderr);
        _Exit(72);
    }
}

void __wrap_vice_sharp_observe_clock(uint64_t clock)
{
    vice_sharp_bus_cycle(clock);
    __real_vice_sharp_observe_clock(clock);
}
