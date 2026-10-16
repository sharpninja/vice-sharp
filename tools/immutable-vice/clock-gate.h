/* External observation/control seam only. Never place in native VICE sources.
 * FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001 AC2/AC3/AC5; TEST-VICE-001 AC2/AC4.
 */
#ifndef VICE_SHARP_CLOCK_GATE_H
#define VICE_SHARP_CLOCK_GATE_H
#include <stdint.h>

typedef struct vice_sharp_gate_io {
    void *context;
    int (*publish)(void *context, uint64_t clock);
    int (*flush)(void *context);
    int (*receive)(void *context);
} vice_sharp_gate_io;

/* Only zero authorizes the caller to return to native execution.
 * publish/flush: zero success; receive: unsigned byte, -1 EOF, -2 I/O error.
 * No native state is written. No implicit retry or default permission exists.
 */
int vice_sharp_clock_gate(uint64_t clock, const vice_sharp_gate_io *io);
#endif
