/* Diagnostic output sink only. Native CLI owns the run/cycle limit and exit.
 * This does not supply a synthetic clock, alter native state or claim bus data.
 */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>

void vice_sharp_observe_clock(uint64_t clock)
{
    printf("VICE_CLOCK,%" PRIu64 "\n", clock);
    fflush(stdout);
}
