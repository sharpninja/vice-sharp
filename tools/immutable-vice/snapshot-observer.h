/* External wrapper contract only. Never copy into the immutable VICE source tree.
 * PLAN-NATIVERESTORE-001 D1 (R11 B): the register copy the machine glue hands to
 * snapshot-observer.c. p excludes the zero and sign bits, n carries the sign in bit 7,
 * and z is zero exactly when the zero flag is set, as mos6510.h 116 to 136 stores them. */
#ifndef VICE_SHARP_SNAPSHOT_OBSERVER_H
#define VICE_SHARP_SNAPSHOT_OBSERVER_H

#include <stdint.h>

typedef struct vice_sharp_cpu_regs {
    uint8_t a;
    uint8_t x;
    uint8_t y;
    uint8_t sp;
    uint16_t pc;
    uint8_t p;
    uint8_t n;
    uint8_t z;
} vice_sharp_cpu_regs;

uint8_t vice_sharp_snapshot_status(uint8_t p, uint8_t n, uint8_t z);

#endif
