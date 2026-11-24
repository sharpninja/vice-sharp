/* Test fixture only. Not VICE. argv: read_result clock a x y sp pc p n z load_result drives.
 * Calls the wrapped CPU-module reader once and the wrapped whole-snapshot reader once, as the pinned
 * c64-snapshot.c 121 and autostart.c 887 do, prints the returned values and proves each real function ran once. */
#include <errno.h>
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#include "snapshot-observer.h"

uint64_t maincpu_clk;
int fake_read_result;
int fake_load_result;
vice_sharp_cpu_regs fake_regs;
const char *fake_drives;
int real_read_calls;
int real_load_calls;

extern int maincpu_snapshot_read_module(void *s);
extern int machine_read_snapshot(const char *name, int event_mode);

static int parse_u64(const char *text, uint64_t *value)
{
    char *end;
    unsigned long long parsed;
    errno = 0;
    parsed = strtoull(text, &end, 10);
    if (errno || text == end || *end != '\0') {
        return 0;
    }
    *value = (uint64_t)parsed;
    return 1;
}

static int parse_int(const char *text, int *value)
{
    char *end;
    long parsed;
    errno = 0;
    parsed = strtol(text, &end, 10);
    if (errno || text == end || *end != '\0') {
        return 0;
    }
    *value = (int)parsed;
    return 1;
}

int main(int argc, char **argv)
{
    uint64_t v;
    int r1, r2, i;
    int fields[8];
    if (argc != 13) {
        return 2;
    }
    if (!parse_int(argv[1], &fake_read_result) || !parse_u64(argv[2], &maincpu_clk)) {
        return 2;
    }
    for (i = 0; i < 8; i++) {
        if (!parse_u64(argv[3 + i], &v) || v > 65535u) {
            return 2;
        }
        fields[i] = (int)v;
    }
    fake_regs.a = (uint8_t)fields[0];
    fake_regs.x = (uint8_t)fields[1];
    fake_regs.y = (uint8_t)fields[2];
    fake_regs.sp = (uint8_t)fields[3];
    fake_regs.pc = (uint16_t)fields[4];
    fake_regs.p = (uint8_t)fields[5];
    fake_regs.n = (uint8_t)fields[6];
    fake_regs.z = (uint8_t)fields[7];
    if (!parse_int(argv[11], &fake_load_result)) {
        return 2;
    }
    fake_drives = argv[12];
    r1 = maincpu_snapshot_read_module(NULL);
    r2 = machine_read_snapshot("fixture.vsf", 0);
    if (real_read_calls != 1 || real_load_calls != 1) {
        return 4;
    }
    printf("ret,%d,%d\n", r1, r2);
    return ferror(stdout) ? 3 : 0;
}
