#include "clock-gate.h"
#include <stddef.h>

int vice_sharp_clock_gate(uint64_t clock, const vice_sharp_gate_io *io)
{
    int command;
    if (io == NULL || io->publish == NULL || io->flush == NULL || io->receive == NULL) {
        return 69;
    }
    if (io->publish(io->context, clock) != 0) {
        return 64;
    }
    if (io->flush(io->context) != 0) {
        return 65;
    }
    command = io->receive(io->context);
    switch (command) {
        case 'c': return 0;
        case 'q': return 67;
        case -1: return 66;
        case -2: return 70;
        default: return 68;
    }
}
