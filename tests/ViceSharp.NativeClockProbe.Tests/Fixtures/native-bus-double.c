/* Test fixture only: a VICE-shaped memory double, not VICE. Separate translation
 * unit so the driver's calls to mem_initialize_memory, mem_mmu_translate and
 * vic_cycle cross an object boundary and --wrap applies, as in the real link.
 * Shape mirrors the materialized cc67418c: global table pointers (vic20mem.c 99
 * to 102), static rows of 0x101 entries, a global toggle that reassigns the
 * pointers and is also called intra-object at the end of the rebuild
 * (vic20mem.c 523, 536 to 557), watch rows that delegate to the nowatch rows
 * (260 to 284), and an identity check over the rows during the rebuild
 * (c64memsc.c 613 to 736). Stock read returns (addr and 0xff) xor 0xa5.
 */
#include <stdint.h>
#include <stdio.h>

typedef uint8_t (*read_func_ptr_t)(uint16_t addr);
typedef void (*store_func_ptr_t)(uint16_t addr, uint8_t value);

#define PAGES 0x101

read_func_ptr_t *_mem_read_tab_ptr;
store_func_ptr_t *_mem_write_tab_ptr;
read_func_ptr_t *_mem_read_tab_ptr_dummy;
store_func_ptr_t *_mem_write_tab_ptr_dummy;

static read_func_ptr_t nowatch_read[PAGES];
static store_func_ptr_t nowatch_write[PAGES];
static read_func_ptr_t watch_read[PAGES];
static store_func_ptr_t watch_write[PAGES];
static uint8_t fake_rom[0x10000];
static int watchpoints_active;

static uint8_t stock_read(uint16_t addr)
{
    return (uint8_t)((addr & 0xff) ^ 0xa5);
}

static void stock_store(uint16_t addr, uint8_t value)
{
    (void)addr;
    (void)value;
}

static uint8_t watch_read_fn(uint16_t addr)
{
    return nowatch_read[addr >> 8](addr);
}

static void watch_store_fn(uint16_t addr, uint8_t value)
{
    nowatch_write[addr >> 8](addr, value);
}

/* Global setters in the x64sc shape (c64memsc.c 746 and 751). */
void mem_read_tab_set(unsigned int base, unsigned int index, read_func_ptr_t read_func)
{
    (void)base;
    nowatch_read[index] = read_func;
}

void mem_set_write_hook(int config, int page, store_func_ptr_t f)
{
    (void)config;
    nowatch_write[page] = f;
}

void mem_toggle_watchpoints(int flag, void *context)
{
    (void)context;
    if (flag) {
        _mem_read_tab_ptr = watch_read;
        _mem_write_tab_ptr = watch_write;
        if (flag > 1) {
            _mem_read_tab_ptr_dummy = watch_read;
            _mem_write_tab_ptr_dummy = watch_write;
        } else {
            _mem_read_tab_ptr_dummy = nowatch_read;
            _mem_write_tab_ptr_dummy = nowatch_write;
        }
    } else {
        _mem_read_tab_ptr = nowatch_read;
        _mem_write_tab_ptr = nowatch_write;
        _mem_read_tab_ptr_dummy = nowatch_read;
        _mem_write_tab_ptr_dummy = nowatch_write;
    }
    watchpoints_active = flag;
}

void mem_initialize_memory(void)
{
    int page;
    int trampolines = 0;
    for (page = 0; page < PAGES; page++) {
        mem_read_tab_set(0, (unsigned int)page, stock_read);
        mem_set_write_hook(0, page, stock_store);
        watch_read[page] = watch_read_fn;
        watch_write[page] = watch_store_fn;
    }
    /* Identity comparisons as c64memsc.c 656 and 717 to 736 perform them. */
    for (page = 0; page < PAGES; page++) {
        if (nowatch_read[page] != stock_read || nowatch_write[page] != stock_store) {
            trampolines++;
        }
    }
    mem_toggle_watchpoints(watchpoints_active, NULL);
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

/* Driver hooks, not VICE: simulate a rewrite the wrapper cannot see and an
 * intra-object reassignment of the dummy pointers. */
void double_rewrite_read_page(int page)
{
    nowatch_read[page] = stock_read;
}

void double_alias_dummy(void)
{
    _mem_read_tab_ptr_dummy = nowatch_read;
    _mem_write_tab_ptr_dummy = nowatch_write;
}
