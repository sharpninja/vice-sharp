/* External wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 S4b: the pure VICE_OBS emitter. The machine glue fills one
 * vice_sharp_obs_input per CPU cycle from VICE globals; the emitter writes the two
 * step 1 lines (PHI1, PHI2) through the line sink. Rules and the mock gate:
 * tests/ViceSharp.NativeClockProbe.Tests/ObsEmitterContractTests.cs. */
#ifndef VICE_SHARP_OBS_EMITTER_H
#define VICE_SHARP_OBS_EMITTER_H

#include <stdint.h>

typedef struct {
    int aec_phi1_low;   /* the VIC owns PHI1 (x64sc: 1); VIC-20 under D15: 0 */
    int aec_from_ba;    /* PHI2 AEC follows BA with the three-cycle rule (x64sc: 1); VIC-20: 0 */
} vice_sharp_obs_rules;

typedef struct {
    uint64_t clock;          /* maincpu_clk at the hook */
    const char *kind;        /* "read", "write" or "dummy"; NULL when the CPU made no access */
    uint16_t address;
    uint8_t data;
    unsigned interrupt_mask; /* maincpu_int_status->global_pending_int, 0 to 127 */
    int ba_high;             /* BA line level this cycle (1 high); VIC-20 passes 0 under D15 */
    int stolen;              /* 1 inside the VIC-II steal loop */
    int phi1_data_known;     /* 0 when VICE exposes no PHI1 bus value; the column is then empty */
    uint8_t phi1_data;
} vice_sharp_obs_input;

typedef void (*vice_sharp_obs_line_sink)(const char *line);

/* Resets the half-cycle counter and the BA history. */
void vice_sharp_obs_init(const vice_sharp_obs_rules *rules, vice_sharp_obs_line_sink sink);

/* Emits the PHI1 and PHI2 lines for one CPU cycle. */
void vice_sharp_obs_cycle(const vice_sharp_obs_input *in);

#endif
