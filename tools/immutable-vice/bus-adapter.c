/* External link wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 slices S2a and S4b (Phase C step 3, option B, handoff D14; confirm
 * item 3: stderr is the observation channel).
 * FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001 AC2/AC3/AC5; TEST-VICE-001 AC2/AC4.
 * Carries the bus observer's events to the host ahead of the gate record for the same
 * cycle without modifying the retained gate adapter (gate-observer.c bytes preserved):
 * link with --wrap=vice_sharp_observe_clock. clock-observer.c calls vice_sharp_observe_clock
 * cross-object, so the call lands here; this wrapper runs the observer's per-cycle hook
 * (vice_sharp_bus_cycle: rescan, fail-closed reports, "cycle,<clock>"), composes the two
 * VICE_OBS lines of the cycle (obs-emitter.c, fields from the machine glue obs-fields.h),
 * and then runs the real gate adapter, which publishes VICE_GATE,<clock>, flushes and waits
 * for the host's permission byte, so the host reads every line of a cycle before that
 * cycle's gate record (plan item 7).
 * Lines: "VICE_BUS,<kind>,<detail>" for the observer's events (the per-access line only
 * with VICESHARP_BUS_RAW=1 in the environment; repatch, missing, foreign, watch, translate,
 * cycle and extra always), and "VICE_OBS,1,..." for the step 1 records. The cycle's CPU
 * access is the first one the observer reported since the previous hook; any further
 * access before the hook (VICE's own non-cycle reset reads, for example) is reported as
 * "VICE_BUS,extra,<kind>,<address>,<data>" and never dropped silently.
 * A write failure on stderr must not let execution continue unobserved: it reports
 * VICE_BUS_STOP,72 and terminates with 72 (the gate owns 64 to 70). Both the write and the
 * flush are checked: under the Windows MSVCRT, fprintf to a pipe whose reader has gone
 * returns a positive count and only fflush reports the failure (LEGION2 finding
 * 2026-10-20T08:13); on glibc fprintf itself fails. */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "obs-emitter.h"
#include "obs-fields.h"

extern void __real_vice_sharp_observe_clock(uint64_t clock);
extern void vice_sharp_bus_cycle(uint64_t clock);

static int raw_access_lines = -1;   /* -1 until the environment has been read once */
static int emitter_ready;
static int have_access;
static char access_kind[16];
static uint16_t access_address;
static uint8_t access_data;

static void stop_on_write_failure(void)
{
    fprintf(stderr, "VICE_BUS_STOP,%d\n", 72);
    fflush(stderr);
    _Exit(72);
}

static void write_line(const char *line)
{
    if (fputs(line, stderr) < 0 || fputc('\n', stderr) == EOF || fflush(stderr) != 0) {
        stop_on_write_failure();
    }
}

static void read_environment(void)
{
    if (raw_access_lines < 0) {
        const char *raw = getenv("VICESHARP_BUS_RAW");
        raw_access_lines = (raw != NULL && strcmp(raw, "1") == 0);
    }
}

/* PLAN-NATIVERESTORE-001 D1: the snapshot observer and checkpoint units (snapshot-observer.c,
 * snapshot-checkpoint.c) write their VICE_SNAP records through the same checked channel. */
void vice_sharp_snapshot_sink(const char *line)
{
    write_line(line);
}

extern void vice_sharp_checkpoint_on_clock(uint64_t clock);

void vice_sharp_bus_sink(const char *kind, const char *detail)
{
    read_environment();
    if (!raw_access_lines && strcmp(kind, "access") == 0) {
        return;
    }
    if (fprintf(stderr, "VICE_BUS,%s,%s\n", kind, detail) < 0 || fflush(stderr) != 0) {
        stop_on_write_failure();
    }
}

void vice_sharp_bus_on_access(const char *kind, uint16_t addr, uint8_t data)
{
    if (have_access) {
        char detail[48];
        snprintf(detail, sizeof detail, "%s,%u,%u", kind, (unsigned)addr, (unsigned)data);
        vice_sharp_bus_sink("extra", detail);
        return;
    }
    have_access = 1;
    /* "dummy-read" and "dummy-write" are both "dummy" in the step 1 record. */
    strncpy(access_kind, strncmp(kind, "dummy", 5) == 0 ? "dummy" : kind, sizeof access_kind - 1);
    access_kind[sizeof access_kind - 1] = '\0';
    access_address = addr;
    access_data = data;
}

void __wrap_vice_sharp_observe_clock(uint64_t clock)
{
    vice_sharp_obs_input in;
    read_environment();
    vice_sharp_bus_cycle(clock);
    if (!emitter_ready) {
        vice_sharp_obs_init(vice_sharp_obs_rules_get(), write_line);
        emitter_ready = 1;
    }
    memset(&in, 0, sizeof in);
    in.clock = clock;
    in.kind = have_access ? access_kind : NULL;
    in.address = access_address;
    in.data = access_data;
    vice_sharp_obs_fill(&in);
    vice_sharp_obs_cycle(&in);
    have_access = 0;
    /* Inert unless VICESHARP_SNAPSHOT_AT is set (snapshot-checkpoint.c, R12): one branch per cycle. */
    vice_sharp_checkpoint_on_clock(clock);
    __real_vice_sharp_observe_clock(clock);
}
