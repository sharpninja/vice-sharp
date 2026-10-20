/* External wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 S4b: the machine glue interface the bus adapter uses to fill the
 * fields the VICE_OBS emitter needs from VICE globals (obs-fields-x64sc.c,
 * obs-fields-xvic.c) or from a test double (Fixtures/obs-fields-double.c). */
#ifndef VICE_SHARP_OBS_FIELDS_H
#define VICE_SHARP_OBS_FIELDS_H

#include "obs-emitter.h"

/* The machine's AEC rules (see obs-emitter.h). */
const vice_sharp_obs_rules *vice_sharp_obs_rules_get(void);

/* Fills interrupt_mask, ba_high, stolen, phi1_data_known and phi1_data for the current cycle;
 * clock, kind, address and data are the adapter's. */
void vice_sharp_obs_fill(vice_sharp_obs_input *in);

#endif
