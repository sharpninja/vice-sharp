/* Test fixture only: the machine glue double for the bus adapter's VICE_OBS composition.
 * Reports x64sc rules, BA high, interrupt mask 0, not stolen and no exposed PHI1 value. Not VICE. */
#include "obs-fields.h"

static const vice_sharp_obs_rules rules = { 1, 1 };

const vice_sharp_obs_rules *vice_sharp_obs_rules_get(void)
{
    return &rules;
}

void vice_sharp_obs_fill(vice_sharp_obs_input *in)
{
    in->interrupt_mask = 0;
    in->ba_high = 1;
    in->stolen = 0;
    in->phi1_data_known = 0;
    in->phi1_data = 0;
}
