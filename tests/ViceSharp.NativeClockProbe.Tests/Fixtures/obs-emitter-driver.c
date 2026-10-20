/* Test fixture only. argv: aecPhi1Low, aecFromBa, then one cycle per argument as
 * clock:kind:address:data:mask:baHigh:stolen:phi1 (kind "-" means no CPU access, phi1 "-" means
 * not exposed). Prints every VICE_OBS line the emitter produces. Not VICE. */
#include <errno.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "obs-emitter.h"

static void print_line(const char *line)
{
    fputs(line, stdout);
    fputc('\n', stdout);
}

static int parse_field(char **cursor, uint64_t *value, int *absent)
{
    char *token = *cursor;
    char *colon = strchr(token, ':');
    char *end;
    unsigned long long parsed;
    if (colon) { *colon = '\0'; *cursor = colon + 1; } else { *cursor = token + strlen(token); }
    *absent = (strcmp(token, "-") == 0);
    if (*absent) { *value = 0; return 1; }
    errno = 0;
    parsed = strtoull(token, &end, 10);
    if (errno || token == end || *end != '\0') { return 0; }
    *value = (uint64_t)parsed;
    return 1;
}

int main(int argc, char **argv)
{
    vice_sharp_obs_rules rules;
    int i;
    if (argc < 3) { return 2; }
    rules.aec_phi1_low = argv[1][0] == '1';
    rules.aec_from_ba = argv[2][0] == '1';
    vice_sharp_obs_init(&rules, print_line);
    for (i = 3; i < argc; i++) {
        vice_sharp_obs_input in;
        char *cursor = argv[i];
        char *kind;
        char *colon;
        uint64_t v;
        int absent;
        if (!parse_field(&cursor, &v, &absent) || absent) { return 2; }
        in.clock = v;
        kind = cursor;
        colon = strchr(kind, ':');
        if (!colon) { return 2; }
        *colon = '\0';
        cursor = colon + 1;
        in.kind = strcmp(kind, "-") == 0 ? NULL : kind;
        if (!parse_field(&cursor, &v, &absent) || absent || v > 0xffff) { return 2; }
        in.address = (uint16_t)v;
        if (!parse_field(&cursor, &v, &absent) || absent || v > 0xff) { return 2; }
        in.data = (uint8_t)v;
        if (!parse_field(&cursor, &v, &absent) || absent || v > 0x7f) { return 2; }
        in.interrupt_mask = (unsigned)v;
        if (!parse_field(&cursor, &v, &absent) || absent || v > 1) { return 2; }
        in.ba_high = (int)v;
        if (!parse_field(&cursor, &v, &absent) || absent || v > 1) { return 2; }
        in.stolen = (int)v;
        if (!parse_field(&cursor, &v, &absent) || v > 0xff) { return 2; }
        in.phi1_data_known = !absent;
        in.phi1_data = (uint8_t)v;
        vice_sharp_obs_cycle(&in);
    }
    return ferror(stdout) ? 3 : 0;
}
