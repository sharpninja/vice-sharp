/* External link wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 slice S1 (Phase C step 3, option B, handoff D14).
 * FR-VICE-001 AC4; TR-VICE-ORACLE-001 AC3/AC5; TEST-VICE-001 AC4.
 * Link with --wrap=mem_initialize_memory and --wrap=mem_mmu_translate beside the
 * existing --wrap=vic_cycle hook (clock-observer.c), whose vice_sharp_observe_clock
 * adapter must call vice_sharp_bus_cycle ahead of the gate record.
 * Pinned upstream cc67418cba58e71d8e70fd353e9de47199ad2075:
 *  - The four table pointers are global (vic20mem.c 99 to 102; c64memsc.c 102 to 105)
 *    but reassigned intra-object (vic20mem.c 539 to 554; c64memsc.c 169 to 184), so
 *    this wrapper patches the contents of VICE's own rows and never the pointers.
 *  - Every content rewrite happens inside mem_initialize_memory, which has only
 *    cross-object callers (receipt 20261019T205400-phase-c-step3-registration-audit).
 *  - c64memsc.c 613 to 736 compares row entries by identity during the rebuild, so
 *    nothing is patched until the real rebuild has returned.
 *  - Dummy accesses reach the same rows through _mem_read_tab_ptr_dummy and
 *    _mem_write_tab_ptr_dummy (maincpu.c 171 to 187); the dummy pointers are re-pointed
 *    at wrapper-owned rows so the kind is known; while they alias the real rows the
 *    kind is reported as "missing", never guessed.
 *  - mem_mmu_translate feeds bank_base and bank_limit (maincpu.c 83 to 89, 519 to 524);
 *    limit 0 and base NULL force every fetch through the rows.
 * Events ("kind,detail" through vice_sharp_bus_sink), rules and the mock gate:
 * tests/ViceSharp.NativeClockProbe.Tests/BUS-OBSERVER-SLICE.md.
 * The rescan in vice_sharp_bus_cycle is the coverage guarantee: an unpatched entry or
 * a foreign live row is reported every cycle it is seen; nothing is silently skipped.
 */
#include <inttypes.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>

typedef uint8_t (*read_func_ptr_t)(uint16_t addr);
typedef void (*store_func_ptr_t)(uint16_t addr, uint8_t value);

#define VICE_SHARP_PAGES 0x101

extern uint64_t maincpu_clk;
extern read_func_ptr_t *_mem_read_tab_ptr;
extern store_func_ptr_t *_mem_write_tab_ptr;
extern read_func_ptr_t *_mem_read_tab_ptr_dummy;
extern store_func_ptr_t *_mem_write_tab_ptr_dummy;
extern void __real_mem_initialize_memory(void);
extern void __real_mem_mmu_translate(unsigned int addr, uint8_t **base, int *start, int *limit);
extern void vice_sharp_bus_sink(const char *kind, const char *detail);

static read_func_ptr_t saved_read[VICE_SHARP_PAGES];
static store_func_ptr_t saved_write[VICE_SHARP_PAGES];
static read_func_ptr_t *known_read_row;
static store_func_ptr_t *known_write_row;
static read_func_ptr_t dummy_read_row[VICE_SHARP_PAGES];
static store_func_ptr_t dummy_write_row[VICE_SHARP_PAGES];

static void emit_access(const char *kind, uint16_t addr, uint8_t data)
{
    char detail[64];
    snprintf(detail, sizeof detail, "%s,%u,%u,%" PRIu64, kind, (unsigned)addr, (unsigned)data, maincpu_clk);
    vice_sharp_bus_sink("access", detail);
}

static uint8_t read_real(int page, uint16_t addr)
{
    uint8_t value = saved_read[page](addr);
    emit_access(_mem_read_tab_ptr_dummy == dummy_read_row ? "read" : "missing", addr, value);
    return value;
}

static void write_real(int page, uint16_t addr, uint8_t value)
{
    saved_write[page](addr, value);
    emit_access(_mem_write_tab_ptr_dummy == dummy_write_row ? "write" : "missing", addr, value);
}

static uint8_t read_dummy(int page, uint16_t addr)
{
    uint8_t value = saved_read[page](addr);
    emit_access("dummy-read", addr, value);
    return value;
}

static void write_dummy(int page, uint16_t addr, uint8_t value)
{
    saved_write[page](addr, value);
    emit_access("dummy-write", addr, value);
}

/* One trampoline per page and kind; the page index is baked into each function. */
#define VICE_SHARP_DEFINE(h, l) \
    static uint8_t rr_##h##_##l(uint16_t a) { return read_real((h) * 16 + (l), a); } \
    static void wr_##h##_##l(uint16_t a, uint8_t v) { write_real((h) * 16 + (l), a, v); } \
    static uint8_t rd_##h##_##l(uint16_t a) { return read_dummy((h) * 16 + (l), a); } \
    static void wd_##h##_##l(uint16_t a, uint8_t v) { write_dummy((h) * 16 + (l), a, v); }
#define VICE_SHARP_ROW(X, h) \
    X(h, 0) X(h, 1) X(h, 2) X(h, 3) X(h, 4) X(h, 5) X(h, 6) X(h, 7) \
    X(h, 8) X(h, 9) X(h, 10) X(h, 11) X(h, 12) X(h, 13) X(h, 14) X(h, 15)
#define VICE_SHARP_ALL(X) \
    VICE_SHARP_ROW(X, 0) VICE_SHARP_ROW(X, 1) VICE_SHARP_ROW(X, 2) VICE_SHARP_ROW(X, 3) \
    VICE_SHARP_ROW(X, 4) VICE_SHARP_ROW(X, 5) VICE_SHARP_ROW(X, 6) VICE_SHARP_ROW(X, 7) \
    VICE_SHARP_ROW(X, 8) VICE_SHARP_ROW(X, 9) VICE_SHARP_ROW(X, 10) VICE_SHARP_ROW(X, 11) \
    VICE_SHARP_ROW(X, 12) VICE_SHARP_ROW(X, 13) VICE_SHARP_ROW(X, 14) VICE_SHARP_ROW(X, 15)

VICE_SHARP_ALL(VICE_SHARP_DEFINE)
static uint8_t rr_256(uint16_t a) { return read_real(0x100, a); }
static void wr_256(uint16_t a, uint8_t v) { write_real(0x100, a, v); }
static uint8_t rd_256(uint16_t a) { return read_dummy(0x100, a); }
static void wd_256(uint16_t a, uint8_t v) { write_dummy(0x100, a, v); }

#define VICE_SHARP_RR(h, l) rr_##h##_##l,
#define VICE_SHARP_WR(h, l) wr_##h##_##l,
#define VICE_SHARP_RD(h, l) rd_##h##_##l,
#define VICE_SHARP_WD(h, l) wd_##h##_##l,
static read_func_ptr_t tramp_read[VICE_SHARP_PAGES] = { VICE_SHARP_ALL(VICE_SHARP_RR) rr_256 };
static store_func_ptr_t tramp_write[VICE_SHARP_PAGES] = { VICE_SHARP_ALL(VICE_SHARP_WR) wr_256 };
static read_func_ptr_t dummy_read_row[VICE_SHARP_PAGES] = { VICE_SHARP_ALL(VICE_SHARP_RD) rd_256 };
static store_func_ptr_t dummy_write_row[VICE_SHARP_PAGES] = { VICE_SHARP_ALL(VICE_SHARP_WD) wd_256 };

static int patch_entry(int page)
{
    int count = 0;
    if (known_read_row[page] != tramp_read[page]) {
        saved_read[page] = known_read_row[page];
        known_read_row[page] = tramp_read[page];
        count++;
    }
    if (known_write_row[page] != tramp_write[page]) {
        saved_write[page] = known_write_row[page];
        known_write_row[page] = tramp_write[page];
        count++;
    }
    return count;
}

static int repoint_dummy(void)
{
    if (_mem_read_tab_ptr_dummy == dummy_read_row && _mem_write_tab_ptr_dummy == dummy_write_row) {
        return 0;
    }
    _mem_read_tab_ptr_dummy = dummy_read_row;
    _mem_write_tab_ptr_dummy = dummy_write_row;
    return 1;
}

static void emit_repatch(int entries, int dummy)
{
    char detail[32];
    snprintf(detail, sizeof detail, "%d,%d", entries, dummy);
    vice_sharp_bus_sink("repatch", detail);
}

void __wrap_mem_initialize_memory(void)
{
    int page;
    int entries = 0;
    __real_mem_initialize_memory();
    /* After the rebuild the dummy pointers resolve to the rows every access ends in
     * (nowatch rows for watch flags 0 and 1, watch rows for flag 2), so those are the
     * rows to patch; the real pointers may legitimately point at delegating watch rows. */
    known_read_row = _mem_read_tab_ptr_dummy;
    known_write_row = _mem_write_tab_ptr_dummy;
    for (page = 0; page < VICE_SHARP_PAGES; page++) {
        entries += patch_entry(page);
    }
    emit_repatch(entries, repoint_dummy());
}

void __wrap_mem_mmu_translate(unsigned int addr, uint8_t **base, int *start, int *limit)
{
    char detail[32];
    __real_mem_mmu_translate(addr, base, start, limit);
    *base = NULL;
    *limit = 0;
    snprintf(detail, sizeof detail, "%d,%d", *limit, *base != NULL);
    vice_sharp_bus_sink("translate", detail);
}

void vice_sharp_bus_cycle(uint64_t clock)
{
    char detail[32];
    if (known_read_row != NULL) {
        int page;
        int entries = 0;
        int dummy;
        if (_mem_read_tab_ptr != known_read_row) {
            vice_sharp_bus_sink("foreign", "read");
        }
        if (_mem_write_tab_ptr != known_write_row) {
            vice_sharp_bus_sink("foreign", "write");
        }
        for (page = 0; page < VICE_SHARP_PAGES; page++) {
            if (known_read_row[page] != tramp_read[page]) {
                snprintf(detail, sizeof detail, "read,%d", page);
                vice_sharp_bus_sink("missing", detail);
            }
            if (known_write_row[page] != tramp_write[page]) {
                snprintf(detail, sizeof detail, "write,%d", page);
                vice_sharp_bus_sink("missing", detail);
            }
            entries += patch_entry(page);
        }
        dummy = repoint_dummy();
        if (entries != 0 || dummy != 0) {
            emit_repatch(entries, dummy);
        }
    }
    snprintf(detail, sizeof detail, "%" PRIu64, clock);
    vice_sharp_bus_sink("cycle", detail);
}
