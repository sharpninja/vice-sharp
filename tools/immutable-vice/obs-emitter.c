/* External wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 S4b: the pure VICE_OBS emitter (step 1 line contract,
 * tests/ViceSharp.NativeClockProbe.Tests/OBSERVATION-LINE-SLICE.md):
 *   VICE_OBS,1,<halfCycle>,<PHI1|PHI2>,<aec>,<ba>,<access>,<address>,<data>,<interruptMask>,<stolen>,<clock>
 * Per CPU cycle n since init: PHI1 at half-cycle 2n (idle, address 0 under contract
 * amendment A1: the CPU is off the bus; data is the PHI1 bus value VICE exposes, or an
 * empty column when it does not), then PHI2 at 2n+1 carrying the cycle's CPU access, or
 * idle with an empty data column when there was none (a stolen cycle, or VICE's own
 * non-cycle reset accesses), which the decoder fails closed as Missing:Data; nothing is
 * guessed. AEC: in PHI1 low where the VIC owns PHI1 (x64sc), else high; in PHI2 derived
 * from BA where the machine rule says so (amendment A2): low once BA has been low for
 * three consecutive cycles including this one (Bauer, "The MOS 6567/6569 video
 * controller", 3.14.3; VICE SC's three-cycle write window, mainc64cpu.c 117 to 154),
 * else high. FR-VICE-001 AC4; TR-VICE-ORACLE-001 AC3/AC5; TEST-VICE-001 AC4. */
#include <inttypes.h>
#include <stddef.h>
#include <stdio.h>

#include "obs-emitter.h"

static vice_sharp_obs_rules current_rules;
static vice_sharp_obs_line_sink current_sink;
static uint64_t cycle_index;
static unsigned ba_low_run;

void vice_sharp_obs_init(const vice_sharp_obs_rules *rules, vice_sharp_obs_line_sink sink)
{
    current_rules = *rules;
    current_sink = sink;
    cycle_index = 0;
    ba_low_run = 0;
}

static void emit(uint64_t half, const char *phase, int aec, int ba, const char *access, unsigned address,
                 int data_known, unsigned data, unsigned mask, int stolen, uint64_t clock)
{
    char line[160];
    char data_text[8] = "";
    if (data_known) {
        snprintf(data_text, sizeof data_text, "%u", data);
    }
    snprintf(line, sizeof line, "VICE_OBS,1,%" PRIu64 ",%s,%d,%d,%s,%u,%s,%u,%d,%" PRIu64,
             half, phase, aec, ba, access, address, data_text, mask, stolen, clock);
    current_sink(line);
}

void vice_sharp_obs_cycle(const vice_sharp_obs_input *in)
{
    int ba = in->ba_high ? 1 : 0;
    int aec_phi1 = current_rules.aec_phi1_low ? 0 : 1;
    int aec_phi2 = 1;
    uint64_t half = cycle_index * 2;
    if (current_rules.aec_from_ba) {
        ba_low_run = in->ba_high ? 0 : ba_low_run + 1;
        aec_phi2 = ba_low_run >= 3 ? 0 : 1;
    }
    emit(half, "PHI1", aec_phi1, ba, "idle", 0, in->phi1_data_known, in->phi1_data, in->interrupt_mask, in->stolen ? 1 : 0, in->clock);
    if (in->kind != NULL) {
        emit(half + 1, "PHI2", aec_phi2, ba, in->kind, in->address, 1, in->data, in->interrupt_mask, in->stolen ? 1 : 0, in->clock);
    } else {
        emit(half + 1, "PHI2", aec_phi2, ba, "idle", 0, 0, 0, in->interrupt_mask, in->stolen ? 1 : 0, in->clock);
    }
    cycle_index++;
}
