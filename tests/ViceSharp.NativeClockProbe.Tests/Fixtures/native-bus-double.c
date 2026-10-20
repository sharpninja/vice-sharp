/* Test fixture only: a VICE-shaped memory double, not VICE. Separate translation
 * unit so the driver's calls to mem_initialize_memory, mem_mmu_translate and
 * vic_cycle cross an object boundary and --wrap applies, as in the real link.
 * Shape mirrors the materialized cc67418c. With VICE_SHARP_ROWS undefined or 1 it
 * is the xvic shape: global table pointers (vic20mem.c 99 to 102), one static
 * row of 0x101 entries, a global toggle that reassigns the pointers and is also
 * called intra-object at the end of the rebuild (523, 536 to 557), watch rows
 * delegating to the nowatch row (260 to 284). With VICE_SHARP_ROWS=32 it is the
 * x64sc shape: static mem_read_tab[32][0x101] and mem_write_tab (c64memsc.c 110
 * to 111), a static mem_config with the global mem_get_current_bank_config (222),
 * global setters mem_read_tab_set and mem_set_write_hook (746, 751), the static
 * mem_update_tab_ptrs re-pointing all four pointers (166 to 186) from the toggle
 * and from a banking change, watch rows delegating to the current row (150 to
 * 162). Both shapes run the identity check over the rows during the rebuild
 * (c64memsc.c 613 to 736). Stock read returns (addr and 0xff) xor 0xa5.
 */
#include <stdint.h>
#include <stdio.h>

#ifndef VICE_SHARP_ROWS
#define VICE_SHARP_ROWS 1
#endif

typedef uint8_t (*read_func_ptr_t)(uint16_t addr);
typedef void (*store_func_ptr_t)(uint16_t addr, uint8_t value);

#define PAGES 0x101

read_func_ptr_t *_mem_read_tab_ptr;
store_func_ptr_t *_mem_write_tab_ptr;
read_func_ptr_t *_mem_read_tab_ptr_dummy;
store_func_ptr_t *_mem_write_tab_ptr_dummy;

static read_func_ptr_t mem_read_tab[VICE_SHARP_ROWS][PAGES];
static store_func_ptr_t mem_write_tab[VICE_SHARP_ROWS][PAGES];
static read_func_ptr_t mem_read_tab_watch[PAGES];
static store_func_ptr_t mem_write_tab_watch[PAGES];
static uint8_t fake_rom[0x10000];
static int watchpoints_active;
static int mem_config;

static uint8_t stock_read(uint16_t addr)
{
    return (uint8_t)((addr & 0xff) ^ 0xa5);
}

static void stock_store(uint16_t addr, uint8_t value)
{
    (void)addr;
    (void)value;
}

static uint8_t read_watch(uint16_t addr)
{
    return mem_read_tab[mem_config][addr >> 8](addr);
}

static void store_watch(uint16_t addr, uint8_t value)
{
    mem_write_tab[mem_config][addr >> 8](addr, value);
}

int mem_get_current_bank_config(void)
{
    return mem_config;
}

void mem_read_tab_set(unsigned int base, unsigned int index, read_func_ptr_t read_func)
{
    mem_read_tab[base][index] = read_func;
}

void mem_set_write_hook(int config, int page, store_func_ptr_t f)
{
    mem_write_tab[config][page] = f;
}

static void mem_update_tab_ptrs(int flag)
{
    if (flag) {
        _mem_read_tab_ptr = mem_read_tab_watch;
        _mem_write_tab_ptr = mem_write_tab_watch;
        if (flag > 1) {
            _mem_read_tab_ptr_dummy = mem_read_tab_watch;
            _mem_write_tab_ptr_dummy = mem_write_tab_watch;
        } else {
            _mem_read_tab_ptr_dummy = mem_read_tab[mem_config];
            _mem_write_tab_ptr_dummy = mem_write_tab[mem_config];
        }
    } else {
        _mem_read_tab_ptr = mem_read_tab[mem_config];
        _mem_write_tab_ptr = mem_write_tab[mem_config];
        _mem_read_tab_ptr_dummy = mem_read_tab[mem_config];
        _mem_write_tab_ptr_dummy = mem_write_tab[mem_config];
    }
}

void mem_toggle_watchpoints(int flag, void *context)
{
    (void)context;
    mem_update_tab_ptrs(flag);
    watchpoints_active = flag;
}

void mem_initialize_memory(void)
{
    int config, page;
    int trampolines = 0;
    for (config = 0; config < VICE_SHARP_ROWS; config++) {
        for (page = 0; page < PAGES; page++) {
            mem_read_tab_set((unsigned int)config, (unsigned int)page, stock_read);
            mem_set_write_hook(config, page, stock_store);
        }
    }
    for (page = 0; page < PAGES; page++) {
        mem_read_tab_watch[page] = read_watch;
        mem_write_tab_watch[page] = store_watch;
    }
    /* Identity comparisons as c64memsc.c 656 and 717 to 736 perform them. */
    for (config = 0; config < VICE_SHARP_ROWS; config++) {
        for (page = 0; page < PAGES; page++) {
            if (mem_read_tab[config][page] != stock_read || mem_write_tab[config][page] != stock_store) {
                trampolines++;
            }
        }
    }
    mem_update_tab_ptrs(watchpoints_active);
    printf("init,%d\n", trampolines);
}

void mem_mmu_translate(unsigned int addr, uint8_t **base, int *start, int *limit)
{
    *base = fake_rom;
    *start = (int)(addr & 0xff00u);
    *limit = 0x1ffd;
}

void vic_cycle(void)
{
}

/* Driver hooks, not VICE: a banking change (intra-object, like
 * mem_pla_config_changed), rewrites the wrapper cannot see, and an
 * intra-object reassignment of the dummy pointers. */
int double_bank(int config)
{
    if (config < 0 || config >= VICE_SHARP_ROWS) {
        return -1;
    }
    mem_config = config;
    mem_update_tab_ptrs(watchpoints_active);
    return 0;
}

int double_rewrite_row_read_page(int config, int page)
{
    if (config < 0 || config >= VICE_SHARP_ROWS || page < 0 || page >= PAGES) {
        return -1;
    }
    mem_read_tab[config][page] = stock_read;
    return 0;
}

void double_rewrite_read_page(int page)
{
    mem_read_tab[mem_config][page] = stock_read;
}

void double_alias_dummy(void)
{
    _mem_read_tab_ptr_dummy = mem_read_tab[mem_config];
    _mem_write_tab_ptr_dummy = mem_write_tab[mem_config];
}
