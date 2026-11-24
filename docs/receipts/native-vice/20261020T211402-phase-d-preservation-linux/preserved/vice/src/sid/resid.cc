/*
 * resid.cc - reSID interface code.
 *
 * Written by
 *  Teemu Rantanen <tvr@cs.hut.fi>
 *  Dag Lem <resid@nimrod.no>
 *  Andreas Boose <viceteam@t-online.de>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
 * See README for copyright notice.
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */

#include "vice.h"

#ifdef _M_ARM
#undef _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE
#define _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE 1
#endif

extern "C" {

#include <string.h>

#include "sid/sid.h" /* sid_engine_t */
#include "lib.h"
#include "log.h"
#include "resid.h"
#include "resources.h"
#include "sid-snapshot.h"
#include "types.h"

extern log_t sound_log;

} // extern "C"

#include "resid/sid.h"
/* resid-dtv/ is used for DTVSID, but the API is the same */

using namespace reSID;

extern "C" {

struct sound_s
{
    /* speed factor */
    int factor;

    /* resid sid implementation */
    reSID::SID *sid;
};

typedef struct sound_s sound_t;

/* manage temporary buffers. if the requested size is smaller or equal to the
 * size of the already allocated buffer, reuse it.  */
static short *buf = NULL;

static int blen = 0;

static short *getbuf(int len)
{
    if ((buf == NULL) || (blen < len)) {
        if (buf) {
            lib_free(buf);
        }
        blen = len;
        buf = (short *)lib_calloc(len, 1);
    }
    return buf;
}

static sound_t *resid_open(uint8_t *sidstate)
{
    sound_t *psid;
    int i;

    psid = new sound_t;
    psid->sid = new reSID::SID;

    for (i = 0x00; i <= 0x18; i++) {
        psid->sid->write(i, sidstate[i]);
    }

    return psid;
}

static int resid_init(sound_t *psid, int speed, int cycles_per_sec, int factor)
{
    sampling_method method;
    char model_text[100];
    char method_text[100];
    double passband, gain;
    int filters_enabled, model, sampling, passband_percentage, gain_percentage, filter_bias_mV;
    int rawoutput;

    if (resources_get_int("SidFilters", &filters_enabled) < 0) {
        return 0;
    }

    if (resources_get_int("SidModel", &model) < 0) {
        return 0;
    }

    if (resources_get_int("SidResidEnableRawOutput", &rawoutput) < 0) {
        return 0;
    }

    /*
     * Don't even think about changing this to fast during warp :)
     * the resampled result is visible to the emulator.
     */
    if (resources_get_int("SidResidSampling", &sampling) < 0) {
        return 0;
    }

    if ((model == 1) || (model == 2)) {
        /* 8580 */
        if (resources_get_int("SidResid8580Passband", &passband_percentage) < 0) {
            return 0;
        }

        if (resources_get_int("SidResid8580Gain", &gain_percentage) < 0) {
            return 0;
        }

        if (resources_get_int("SidResid8580FilterBias", &filter_bias_mV) < 0) {
            return 0;
        }
    } else {
        /* 6581 */
        if (resources_get_int("SidResidPassband", &passband_percentage) < 0) {
            return 0;
        }

        if (resources_get_int("SidResidGain", &gain_percentage) < 0) {
            return 0;
        }

        if (resources_get_int("SidResidFilterBias", &filter_bias_mV) < 0) {
            return 0;
        }
    }

    passband = speed * passband_percentage / 200.0;
    gain = gain_percentage / 100.0;

    psid->factor = factor;

    switch (model) {
      default:
      case 0:
        psid->sid->set_chip_model(MOS6581);
        psid->sid->set_voice_mask(0x07);
        psid->sid->input(0);
        strcpy(model_text, "MOS6581");
        break;
      case 1:
        psid->sid->set_chip_model(MOS8580);
        psid->sid->set_voice_mask(0x07);
        psid->sid->input(0);
        strcpy(model_text, "MOS8580");
        break;
      case 2:
        psid->sid->set_chip_model(MOS8580);
        psid->sid->set_voice_mask(0x0f);
        psid->sid->input(-32768);
        strcpy(model_text, "MOS8580 + digi boost");
        break;
#if 0
      case 3: /* not yet */
        psid->sid->set_chip_model(MOS6581R4);
        psid->sid->set_voice_mask(0x07);
        psid->sid->input(0);
        strcpy(model_text, "MOS6581R4");
        break;
#endif
      case 4:
        /* resid-dtv has only the DTVSID model and no ext input*/
        strcpy(model_text, "DTVSID");
        break;
    }
    psid->sid->enable_filter(filters_enabled ? true : false);
    psid->sid->adjust_filter_bias(filter_bias_mV / 1000.0);
    psid->sid->enable_external_filter(filters_enabled ? true : false);

    switch (sampling) {
      default:
      case 0:
        method = SAMPLE_FAST;
        strcpy(method_text, "fast");
        break;
      case 1:
        method = SAMPLE_INTERPOLATE;
        strcpy(method_text, "interpolating");
        break;
      case 2:
        method = SAMPLE_RESAMPLE;
        sprintf(method_text, "resampling, pass to %dHz", (int)passband);
        break;
      case 3:
        method = SAMPLE_RESAMPLE_FASTMEM;
        sprintf(method_text, "fast resampling, pass to %dHz", (int)passband);
        break;
    }

    if (!psid->sid->set_sampling_parameters(cycles_per_sec, method,
                                            speed, passband, gain)) {
        log_warning(sound_log,
                    "reSID: Out of spec, increase sampling rate or decrease maximum speed");
        return 0;
    }

    psid->sid->enable_raw_debug_output(rawoutput);

    log_message(sound_log, "reSID: %s, filter %s, sampling rate %dHz - %s%s",
                model_text,
                filters_enabled ? "on" : "off",
                speed, method_text,
                rawoutput ? ", raw debug output enabled": "");

    return 1;
}

static void resid_close(sound_t *psid)
{
    delete psid->sid;
    delete psid;

    if (buf) {
        lib_free(buf);
        buf = NULL;
    }
}

static uint8_t resid_read(sound_t *psid, uint16_t addr)
{
    return psid->sid->read(addr);
}

static void resid_store(sound_t *psid, uint16_t addr, uint8_t byte)
{
    psid->sid->write(addr, byte);
}

static void resid_reset(sound_t *psid, CLOCK cpu_clk)
{
    psid->sid->reset();
}

#ifdef SOUND_SYSTEM_FLOAT
/* FIXME */
static int resid_calculate_samples(sound_t *psid, float *pbuf, int nr, CLOCK *delta_t)
{
    short *tmp_buf;
    int retval;
    int int_delta_t_original = (int)*delta_t;
    int int_delta_t = (int)*delta_t;
    int i;

    /* Tried not to mess with resid during 64-bit conversion. clock(...) wants to modify *delta_t ... */

    if (psid->factor == 1000) {
        tmp_buf = getbuf(2 * nr);
        retval = psid->sid->clock(int_delta_t, tmp_buf, nr, 0);
        (*delta_t) += int_delta_t - int_delta_t_original;
        for (i = 0; i < nr; i++) {
            pbuf[i] = tmp_buf[i] / 32767.0;
        }
        return retval;
    }

    tmp_buf = getbuf(2 * nr * psid->factor / 1000);
    retval = psid->sid->clock(int_delta_t, tmp_buf, nr * psid->factor / 1000, 0) * 1000 / psid->factor;
    (*delta_t) += int_delta_t - int_delta_t_original;
    for (i = 0; i < nr; i++) {
        pbuf[i] = tmp_buf[i] / 32767.0;
    }

    return retval;
}
#else
static int resid_calculate_samples(sound_t *psid, short *pbuf, int nr, int interleave, CLOCK *delta_t)
{
    short *tmp_buf;
    int retval;
    int int_delta_t_original = (int)*delta_t;
    int int_delta_t = (int)*delta_t;

    /* Tried not to mess with resid during 64-bit conversion. clock(...) wants to modify *delta_t ... */

    if (psid->factor == 1000) {
        retval = psid->sid->clock(int_delta_t, pbuf, nr, interleave);
        (*delta_t) += int_delta_t - int_delta_t_original;
        return retval;
    }

    tmp_buf = getbuf(2 * nr * psid->factor / 1000);
    retval = psid->sid->clock(int_delta_t, tmp_buf, nr * psid->factor / 1000, interleave) * 1000 / psid->factor;
    (*delta_t) += int_delta_t - int_delta_t_original;
    memcpy(pbuf, tmp_buf, 2 * nr);

    return retval;
}
#endif

static char *resid_dump_state(sound_t *psid)
{
    reSID::SID::State state;
    char strbuf[0x400];
    /* when sound is disabled *psid is NULL */
    if (psid && psid->sid) {
        state = psid->sid->read_state();
    } else {
        return lib_strdup("no state available when sound is disabled.");
    }
    sprintf(strbuf,
            "FREQ:   %04x %04x %04x\n"
            "PULSE:  %04x %04x %04x\n"
            "CTRL:     %02x   %02x   %02x\n"
            "ADSR:   %04x %04x %04x\n"
            "FILTER: %04x RES: %02x MODE/VOL: %02x\n"
            "ADC: %02x %02x\n"
            "OSC3: %02x ENV3: %02x\n",
            ((state.sid_register[(0 * 7) + 1] << 8) | state.sid_register[(0 * 7) + 0]) & 0xffff,
            ((state.sid_register[(1 * 7) + 1] << 8) | state.sid_register[(1 * 7) + 0]) & 0xffff,
            ((state.sid_register[(2 * 7) + 1] << 8) | state.sid_register[(2 * 7) + 0]) & 0xffff,
            ((state.sid_register[(0 * 7) + 3] << 8) | state.sid_register[(0 * 7) + 2]) & 0xffff,
            ((state.sid_register[(1 * 7) + 3] << 8) | state.sid_register[(1 * 7) + 2]) & 0xffff,
            ((state.sid_register[(2 * 7) + 3] << 8) | state.sid_register[(2 * 7) + 2]) & 0xffff,
            (state.sid_register[(0 * 7) + 4]) & 0xff,
            (state.sid_register[(1 * 7) + 4]) & 0xff,
            (state.sid_register[(2 * 7) + 4]) & 0xff,
            ((state.sid_register[(0 * 7) + 5] << 8) | state.sid_register[(0 * 7) + 6]) & 0xffff,
            ((state.sid_register[(1 * 7) + 5] << 8) | state.sid_register[(1 * 7) + 6]) & 0xffff,
            ((state.sid_register[(2 * 7) + 5] << 8) | state.sid_register[(2 * 7) + 6]) & 0xffff,
            ((state.sid_register[22] << 8) | state.sid_register[21]) & 0xffff,
            (state.sid_register[23]) & 0xff,
            (state.sid_register[24]) & 0xff,
            (state.sid_register[25]) & 0xff,
            (state.sid_register[26]) & 0xff,
            (state.sid_register[27]) & 0xff,
            (state.sid_register[28]) & 0xff
            );
    return lib_strdup(strbuf);
}

static void resid_state_read(sound_t *psid, sid_snapshot_state_t *sid_state)
{
    reSID::SID::State state;
    unsigned int i;

    /* when sound is disabled *psid is NULL */
    if (psid) {
        state = psid->sid->read_state();
    }

    for (i = 0; i < 0x20; i++) {
        sid_state->sid_register[i] = (uint8_t)state.sid_register[i];
    }

    sid_state->bus_value = (uint8_t)state.bus_value;
    sid_state->bus_value_ttl = (uint32_t)state.bus_value_ttl;
    for (i = 0; i < 3; i++) {
        sid_state->accumulator[i] = (uint32_t)state.accumulator[i];
        sid_state->shift_register[i] = (uint32_t)state.shift_register[i];
        sid_state->rate_counter[i] = (uint16_t)state.rate_counter[i];
        sid_state->rate_counter_period[i] = (uint16_t)state.rate_counter_period[i];
        sid_state->exponential_counter[i] = (uint16_t)state.exponential_counter[i];
        sid_state->exponential_counter_period[i] = (uint16_t)state.exponential_counter_period[i];
        sid_state->envelope_counter[i] = (uint8_t)state.envelope_counter[i];
        sid_state->envelope_state[i] = (uint8_t)state.envelope_state[i];
        sid_state->hold_zero[i] = (uint8_t)state.hold_zero[i];
        sid_state->envelope_pipeline[i] = (uint8_t)state.envelope_pipeline[i];
        sid_state->shift_pipeline[i] = (uint8_t)state.shift_pipeline[i];
        sid_state->shift_register_reset[i] = (uint32_t)state.shift_register_reset[i];
        sid_state->floating_output_ttl[i] = (uint32_t)state.floating_output_ttl[i];
        sid_state->pulse_output[i] = (uint16_t)state.pulse_output[i];
    }
    sid_state->write_pipeline = (uint8_t)state.write_pipeline;
    sid_state->write_address = (uint8_t)state.write_address;
    sid_state->voice_mask = (uint8_t)state.voice_mask;
}

static void resid_state_write(sound_t *psid, sid_snapshot_state_t *sid_state)
{
    reSID::SID::State state;
    unsigned int i;

    for (i = 0; i < 0x20; i++) {
        state.sid_register[i] = (char)sid_state->sid_register[i];
    }

    state.bus_value = (reg8)sid_state->bus_value;
    state.bus_value_ttl = (cycle_count)sid_state->bus_value_ttl;
    for (i = 0; i < 3; i++) {
        state.accumulator[i] = (reg24)sid_state->accumulator[i];
        state.shift_register[i] = (reg24)sid_state->shift_register[i];
        state.rate_counter[i] = (reg16)sid_state->rate_counter[i];
        if (sid_state->rate_counter_period[i]) {
            state.rate_counter_period[i] = (reg16)sid_state->rate_counter_period[i];
        }
        state.exponential_counter[i] = (reg16)sid_state->exponential_counter[i];
        if (sid_state->exponential_counter_period[i]) {
            state.exponential_counter_period[i] = (reg16)sid_state->exponential_counter_period[i];
        }
        state.envelope_counter[i] = (reg8)sid_state->envelope_counter[i];
        state.envelope_state[i] = (EnvelopeGenerator::State)sid_state->envelope_state[i];
        state.hold_zero[i] = (sid_state->hold_zero[i] != 0);
        state.envelope_pipeline[i] = (cycle_count)sid_state->envelope_pipeline[i];
        state.shift_pipeline[i] = (cycle_count)sid_state->shift_pipeline[i];
        state.shift_register_reset[i] = (cycle_count)sid_state->shift_register_reset[i];
        state.floating_output_ttl[i] = (cycle_count)sid_state->floating_output_ttl[i];
        state.pulse_output[i] = (reg16)sid_state->pulse_output[i];
    }
    state.write_pipeline = (cycle_count)sid_state->write_pipeline;
    state.write_address = (reg8)sid_state->write_address;
    state.voice_mask = (reg4)sid_state->voice_mask;

    psid->sid->write_state((const reSID::SID::State)state);
}

sid_engine_t resid_hooks =
{
    resid_open,
    resid_init,
    resid_close,
    resid_read,
    resid_store,
    resid_reset,
    resid_calculate_samples,
    resid_dump_state,
    resid_state_read,
    resid_state_write
};

/* ------------------------------------------------------------------------- */
/* VICE-Sharp hosted shim: single-cycle oracle entry points.
 *
 * The shim's managed parity harness needs reSID clocked one cycle at a time
 * (reSID::SID::clock() with the single-cycle envelope/waveform pipelines).
 * The sound-machine layer only exposes the batched clock(delta_t) path, which
 * by reSID's own comment drops pipelined envelope decrements. These helpers
 * are internal to the shim DLL (not dllexported); vice-shim.c wraps them with
 * machine-handle plumbing and locking. */

int resid_shim_clock_exact(sound_t *psid, int cycles)
{
    int i;

    if (psid == NULL || psid->sid == NULL || cycles < 0) {
        return 0;
    }
    for (i = 0; i < cycles; i++) {
        psid->sid->clock();
    }
    return cycles;
}

uint8_t resid_shim_read(sound_t *psid, uint16_t addr)
{
    if (psid == NULL || psid->sid == NULL) {
        return 0;
    }
    return psid->sid->read((reSID::reg8)(addr & 0x1f));
}

void resid_shim_write(sound_t *psid, uint16_t addr, uint8_t value)
{
    if (psid == NULL || psid->sid == NULL) {
        return;
    }
    psid->sid->write((reSID::reg8)(addr & 0x1f), value);
}

int resid_shim_output(sound_t *psid)
{
    if (psid == NULL || psid->sid == NULL) {
        return 0;
    }
    return psid->sid->output();
}

void resid_shim_reset(sound_t *psid)
{
    if (psid == NULL || psid->sid == NULL) {
        return;
    }
    psid->sid->reset();
}

void resid_shim_state_read(sound_t *psid, sid_snapshot_state_t *sid_state)
{
    if (psid == NULL || psid->sid == NULL || sid_state == NULL) {
        return;
    }
    resid_state_read(psid, sid_state);
}

/* VICE-Sharp S9 parity probe: export reSID internal filter state
 * (Vlp/Vbp/Vhp, prescaled v1/v2/v3, sum/mix, filter.output(), integrator
 * x/vc) for managed lockstep. out must have room for 13 ints. */
void resid_shim_filter_probe(sound_t *psid, int *out)
{
    if (psid == NULL || psid->sid == NULL || out == NULL) {
        return;
    }
    psid->sid->vsharp_filter_probe(out);
}

/* VICE-Sharp S11: reconfigure the exact oracle's sampling method. Needed to
 * observe the 8580 SAMPLE_FAST write pipeline (sid.cc:205-220) and to drive the
 * buffered clock/FIR path for S13 resampler parity. clock_freq is passed
 * through from the shim (985248.0 PAL); method is reSID's sampling_method enum
 * order (0=FAST 1=INTERPOLATE 2=RESAMPLE 3=RESAMPLE_FASTMEM). Returns 1 on
 * success, 0 on failure or null SID. */
int resid_shim_set_sampling(sound_t *psid, int method, double clock_freq,
                            double sample_freq, double pass_freq, double filter_scale)
{
    if (psid == NULL || psid->sid == NULL) {
        return 0;
    }
    return psid->sid->set_sampling_parameters(
               clock_freq, (reSID::sampling_method)method, sample_freq,
               pass_freq, filter_scale)
               ? 1 : 0;
}

/* VICE-Sharp S13: buffered clock path (reSID SID::clock(delta_t, buf, n)). On
 * entry *delta_t is the cycle budget; on return it holds the unconsumed
 * remainder. Returns the number of samples written to buf (at most n). */
int resid_shim_clock_buffered(sound_t *psid, int *delta_t, int16_t *buf, int n)
{
    reSID::cycle_count dt;
    int got;

    if (psid == NULL || psid->sid == NULL || delta_t == NULL || buf == NULL || n < 0) {
        return 0;
    }
    dt = (reSID::cycle_count)(*delta_t);
    got = psid->sid->clock(dt, (short *)buf, n, 1);
    *delta_t = (int)dt;
    return got;
}

} // extern "C"
