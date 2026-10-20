/* External link wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-OBSERVER-001 slices S1 and S1b (Phase C step 3, option B, handoff D14).
 * FR-VICE-001 AC4; TR-VICE-ORACLE-001 AC3/AC5; TEST-VICE-001 AC4.
 * Link with --wrap=mem_initialize_memory and --wrap=mem_mmu_translate beside the
 * existing --wrap=vic_cycle hook (clock-observer.c), whose vice_sharp_observe_clock
 * adapter must call vice_sharp_bus_cycle ahead of the gate record. Compile with
 * -DVICE_SHARP_ROWS=32 for x64sc (NUM_CONFIGS, c64memsc.c 83); leave it undefined for xvic.
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
 *  - x64sc rows are mem_read_tab[NUM_CONFIGS][0x101] and mem_write_tab (c64memsc.c 110
 *    to 111), static, with the current row mem_read_tab[mem_config] and the global
 *    mem_get_current_bank_config() (222); the watch functions delegate to the current
 *    row (150 to 162). The wrapper reaches every row by stride 0x101 from the current
 *    row after proving the layout twice through the global mem_read_tab_set (751); on
 *    failure it reports "layout,0" and patches only the live row (fail closed: other
 *    rows then surface as "foreign" every cycle they are live).
 *  - mem_mmu_translate feeds bank_base and bank_limit (maincpu.c 83 to 89, 519 to 524);
 *    limit 0 and base NULL force every fetch through the rows.
 * Events ("kind,detail" through vice_sharp_bus_sink), rules and the mock gates:
 * tests/ViceSharp.NativeClockProbe.Tests/BUS-OBSERVER-SLICE.md.
 * The rescan in vice_sharp_bus_cycle is the coverage guarantee: an unpatched entry of
 * the current row or a foreign live row is reported every cycle it is seen.
 */
#include <inttypes.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>

#ifndef VICE_SHARP_ROWS
#define VICE_SHARP_ROWS 1
#endif

typedef uint8_t (*read_func_ptr_t)(uint16_t addr);
typedef void (*store_func_ptr_t)(uint16_t addr, uint8_t value);

#define VICE_SHARP_PAGES 0x101
#define VICE_SHARP_PROBE_PAGE 0x12

extern uint64_t maincpu_clk;
extern read_func_ptr_t *_mem_read_tab_ptr;
extern store_func_ptr_t *_mem_write_tab_ptr;
extern read_func_ptr_t *_mem_read_tab_ptr_dummy;
extern store_func_ptr_t *_mem_write_tab_ptr_dummy;
extern void __real_mem_initialize_memory(void);
extern void __real_mem_mmu_translate(unsigned int addr, uint8_t **base, int *start, int *limit);
extern void vice_sharp_bus_sink(const char *kind, const char *detail);
#if VICE_SHARP_ROWS > 1
extern int mem_get_current_bank_config(void);
extern void mem_read_tab_set(unsigned int base, unsigned int index, read_func_ptr_t read_func);
#endif

static read_func_ptr_t saved_read[VICE_SHARP_ROWS][VICE_SHARP_PAGES];
static store_func_ptr_t saved_write[VICE_SHARP_ROWS][VICE_SHARP_PAGES];
static read_func_ptr_t *known_read_rows[VICE_SHARP_ROWS];
static store_func_ptr_t *known_write_rows[VICE_SHARP_ROWS];
static read_func_ptr_t dummy_read_row[VICE_SHARP_PAGES];
static store_func_ptr_t dummy_write_row[VICE_SHARP_PAGES];
static int initialized;

static int current_row(void)
{
#if VICE_SHARP_ROWS > 1
    int config = mem_get_current_bank_config();
    return (config >= 0 && config < VICE_SHARP_ROWS) ? config : 0;
#else
    return 0;
#endif
}

static void emit_access(const char *kind, uint16_t addr, uint8_t data)
{
    char detail[64];
    snprintf(detail, sizeof detail, "%s,%u,%u,%" PRIu64, kind, (unsigned)addr, (unsigned)data, maincpu_clk);
    vice_sharp_bus_sink("access", detail);
}

static uint8_t read_real(int page, uint16_t addr)
{
    uint8_t value = saved_read[current_row()][page](addr);
    emit_access(_mem_read_tab_ptr_dummy == dummy_read_row ? "read" : "missing", addr, value);
    return value;
}

static void write_real(int page, uint16_t addr, uint8_t value)
{
    saved_write[current_row()][page](addr, value);
    emit_access(_mem_write_tab_ptr_dummy == dummy_write_row ? "write" : "missing", addr, value);
}

static uint8_t read_dummy(int page, uint16_t addr)
{
    uint8_t value = saved_read[current_row()][page](addr);
    emit_access("dummy-read", addr, value);
    return value;
}

static void write_dummy(int page, uint16_t addr, uint8_t value)
{
    saved_write[current_row()][page](addr, value);
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

static int patch_entry(int row, int page)
{
    int count = 0;
    if (known_read_rows[row][page] != tramp_read[page]) {
        saved_read[row][page] = known_read_rows[row][page];
        known_read_rows[row][page] = tramp_read[page];
        count++;
    }
    if (known_write_rows[row][page] != tramp_write[page]) {
        saved_write[row][page] = known_write_rows[row][page];
        known_write_rows[row][page] = tramp_write[page];
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

/* Locate the rows to patch. After the rebuild the dummy pointers resolve to the rows
 * every access ends in (nowatch rows for watch flags 0 and 1, watch rows for flag 2),
 * so those are the rows to patch; the real pointers may legitimately point at
 * delegating watch rows. Returns the number of rows located. */
static int locate_rows(void)
{
    int row;
    for (row = 0; row < VICE_SHARP_ROWS; row++) {
        known_read_rows[row] = NULL;
        known_write_rows[row] = NULL;
    }
#if VICE_SHARP_ROWS > 1
    {
        int config = current_row();
        int probe = (config + 1) % VICE_SHARP_ROWS;
        read_func_ptr_t *live = _mem_read_tab_ptr_dummy;
        read_func_ptr_t before = live[VICE_SHARP_PROBE_PAGE];
        read_func_ptr_t *row0;
        int ok;
        /* Stage 1: the live row must be mem_read_tab[config], not a watch row. */
        mem_read_tab_set((unsigned int)config, VICE_SHARP_PROBE_PAGE, tramp_read[VICE_SHARP_PROBE_PAGE]);
        ok = live[VICE_SHARP_PROBE_PAGE] == tramp_read[VICE_SHARP_PROBE_PAGE];
        mem_read_tab_set((unsigned int)config, VICE_SHARP_PROBE_PAGE, before);
        if (ok) {
            /* Stage 2: the stride between configuration rows must be 0x101 entries. */
            row0 = live - (ptrdiff_t)config * VICE_SHARP_PAGES;
            before = row0[(ptrdiff_t)probe * VICE_SHARP_PAGES + VICE_SHARP_PROBE_PAGE];
            mem_read_tab_set((unsigned int)probe, VICE_SHARP_PROBE_PAGE, tramp_read[VICE_SHARP_PROBE_PAGE]);
            ok = row0[(ptrdiff_t)probe * VICE_SHARP_PAGES + VICE_SHARP_PROBE_PAGE] == tramp_read[VICE_SHARP_PROBE_PAGE];
            mem_read_tab_set((unsigned int)probe, VICE_SHARP_PROBE_PAGE, before);
        }
        if (ok) {
            store_func_ptr_t *wrow0 = _mem_write_tab_ptr_dummy - (ptrdiff_t)config * VICE_SHARP_PAGES;
            for (row = 0; row < VICE_SHARP_ROWS; row++) {
                known_read_rows[row] = row0 + (ptrdiff_t)row * VICE_SHARP_PAGES;
                known_write_rows[row] = wrow0 + (ptrdiff_t)row * VICE_SHARP_PAGES;
            }
            return VICE_SHARP_ROWS;
        }
        known_read_rows[config] = _mem_read_tab_ptr_dummy;
        known_write_rows[config] = _mem_write_tab_ptr_dummy;
        return 0;
    }
#else
    known_read_rows[0] = _mem_read_tab_ptr_dummy;
    known_write_rows[0] = _mem_write_tab_ptr_dummy;
    return 1;
#endif
}

void __wrap_mem_initialize_memory(void)
{
    int row, page;
    int entries = 0;
    int located;
    __real_mem_initialize_memory();
    located = locate_rows();
#if VICE_SHARP_ROWS > 1
    {
        char detail[16];
        snprintf(detail, sizeof detail, "%d", located);
        vice_sharp_bus_sink("layout", detail);
    }
#else
    (void)located;
#endif
    for (row = 0; row < VICE_SHARP_ROWS; row++) {
        if (known_read_rows[row] == NULL) {
            continue;
        }
        for (page = 0; page < VICE_SHARP_PAGES; page++) {
            entries += patch_entry(row, page);
        }
    }
    initialized = 1;
    emit_repatch(entries, repoint_dummy());
}

void __wrap_mem_mmu_translate(unsigned int addr, uint8_t **base, int *start, int *limit)
{
    char detail[48];
    int real_limit;
    int real_base_nonnull;
    __real_mem_mmu_translate(addr, base, start, limit);
    real_limit = *limit;
    real_base_nonnull = *base != NULL;
    *base = NULL;
    *limit = 0;
    snprintf(detail, sizeof detail, "%d,%d,%d,%d", *limit, *base == NULL, real_limit, real_base_nonnull);
    vice_sharp_bus_sink("translate", detail);
}

void vice_sharp_bus_cycle(uint64_t clock)
{
    char detail[32];
    if (initialized) {
        int row = current_row();
        int page;
        int entries = 0;
        int dummy;
        if (known_read_rows[row] == NULL) {
            vice_sharp_bus_sink("foreign", "read");
            vice_sharp_bus_sink("foreign", "write");
        } else {
            if (_mem_read_tab_ptr != known_read_rows[row]) {
                vice_sharp_bus_sink("foreign", "read");
            }
            if (_mem_write_tab_ptr != known_write_rows[row]) {
                vice_sharp_bus_sink("foreign", "write");
            }
            for (page = 0; page < VICE_SHARP_PAGES; page++) {
                if (known_read_rows[row][page] != tramp_read[page]) {
                    snprintf(detail, sizeof detail, "read,%d", page);
                    vice_sharp_bus_sink("missing", detail);
                }
                if (known_write_rows[row][page] != tramp_write[page]) {
                    snprintf(detail, sizeof detail, "write,%d", page);
                    vice_sharp_bus_sink("missing", detail);
                }
                entries += patch_entry(row, page);
            }
        }
        dummy = repoint_dummy();
        if (entries != 0 || dummy != 0) {
            emit_repatch(entries, dummy);
        }
    }
    snprintf(detail, sizeof detail, "%" PRIu64, clock);
    vice_sharp_bus_sink("cycle", detail);
}
