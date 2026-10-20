/* External wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 S4b: xvic machine glue for the VICE_OBS fields under D15 (2026-10-20T09:07,
 * "Not applicable: constant 0": the VIC-20 has no BA line and its CPU core no BA state, so ba is
 * 0, aec is 1 and stolen is 0 under the per-machine applicability table), every other value read
 * from a VICE global the pinned source exposes (cc67418cba58e71d8e70fd353e9de47199ad2075):
 *  - interrupt mask: maincpu_int_status->global_pending_int (interrupt.h 55 to 128, real header);
 *  - PHI1 data: vic.vbuf, the last byte the VIC fetched (vic20/victypes.h 207, vic-cycle.c 300),
 *    read through the real header.
 * Compile with -I<work>/src -I<build>/src -I<work>/src/vic20 -I<work>/src/raster. */
#include "vice.h"
#include "interrupt.h"
#include "victypes.h"

#include "obs-fields.h"

static const vice_sharp_obs_rules rules = { 0, 0 };

const vice_sharp_obs_rules *vice_sharp_obs_rules_get(void)
{
    return &rules;
}

void vice_sharp_obs_fill(vice_sharp_obs_input *in)
{
    in->interrupt_mask = maincpu_int_status != NULL ? (maincpu_int_status->global_pending_int & 0x7fu) : 0u;
    in->ba_high = 0;
    in->stolen = 0;
    in->phi1_data_known = 1;
    in->phi1_data = vic.vbuf;
}
