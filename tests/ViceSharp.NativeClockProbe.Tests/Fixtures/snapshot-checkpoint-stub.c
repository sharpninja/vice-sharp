/* Test fixture only. Not VICE. Inert stand-in for snapshot-checkpoint.c's per-cycle hook so the bus adapter can be
 * linked and tested on its own; the checkpoint unit has its own contract (SnapshotCheckpointContractTests). */
#include <stdint.h>

void vice_sharp_checkpoint_on_clock(uint64_t clock)
{
    (void)clock;
}
