/* External wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 S4b: x64sc machine glue for the VICE_OBS fields, every value read from
 * a VICE global the pinned source exposes (cc67418cba58e71d8e70fd353e9de47199ad2075):
 *  - interrupt mask: maincpu_int_status->global_pending_int (interrupt.h 55 to 128, read
 *    through the real header, so the layout is the compiler's, not a mirror);
 *  - BA: the flags vicii_cycle returned for this cycle, published by clock-observer-x64sc.c
 *    (vice_sharp_vicii_ba_flags; bit MAINCPU_BA_LOW_VICII, mainc64cpu.h 31, set when BA is low);
 *  - stolen: vice_sharp_stolen_cycle, set inside the wrapped vicii_steal_cycles (S4a);
 *  - PHI1 data: vicii_read_phi1() (viciisc/vicii-phi1.c 34, vicii.last_read_phi1).
 * Rules: the VIC-II owns PHI1 (AEC low there) and PHI2 AEC follows BA with the three-cycle
 * rule (obs-emitter.c). Compile with -I<work>/src -I<build>/src. */
#include "vice.h"
#include "interrupt.h"
#include "mainc64cpu.h"

#include "obs-fields.h"

extern int vice_sharp_vicii_ba_flags;
extern int vice_sharp_stolen_cycle;
extern uint8_t vicii_read_phi1(void);

static const vice_sharp_obs_rules rules = { 1, 1 };

const vice_sharp_obs_rules *vice_sharp_obs_rules_get(void)
{
    return &rules;
}

void vice_sharp_obs_fill(vice_sharp_obs_input *in)
{
    in->interrupt_mask = maincpu_int_status != NULL ? (maincpu_int_status->global_pending_int & 0x7fu) : 0u;
    in->ba_high = (vice_sharp_vicii_ba_flags & MAINCPU_BA_LOW_VICII) ? 0 : 1;
    in->stolen = vice_sharp_stolen_cycle ? 1 : 0;
    in->phi1_data_known = 1;
    in->phi1_data = vicii_read_phi1();
}
