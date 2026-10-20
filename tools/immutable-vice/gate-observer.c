/* Diagnostic pipe adapter to the mock-proven external gate. Not VICE source.
 * stderr carries clock/stop records. Native output is directed to stdout,
 * but all seven retained gate stdout logs are empty; retention is not proven.
 * A nonzero gate result terminates this diagnostic without another native cycle.
 * _Exit deliberately does not claim a graceful emulator shutdown or snapshot.
 */
#include "clock-gate.h"
#include <inttypes.h>
#include <stdio.h>
#include <stdlib.h>

static int publish(void *context, uint64_t clock)
{
    (void)context;
    return fprintf(stderr, "VICE_GATE,%" PRIu64 "\n", clock) < 0 ? -1 : 0;
}
static int flush(void *context)
{
    (void)context;
    return fflush(stderr);
}
static int receive(void *context)
{
    int value;
    (void)context;
    value = fgetc(stdin);
    return value == EOF ? (ferror(stdin) ? -2 : -1) : value;
}
void vice_sharp_observe_clock(uint64_t clock)
{
    const vice_sharp_gate_io io = { NULL, publish, flush, receive };
    int status = vice_sharp_clock_gate(clock, &io);
    if (status != 0) {
        fprintf(stderr, "VICE_GATE_STOP,%d\n", status);
        fflush(stderr);
        _Exit(status);
    }
}
