/* Test fixture only. Drives the VICE-shaped double through the wrapped seams
 * and prints every observer event as "kind,detail". Not VICE. */
#if !defined(_WIN32)
#define _POSIX_C_SOURCE 200112L
#endif
#include <errno.h>
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef uint8_t (*read_func_ptr_t)(uint16_t addr);
typedef void (*store_func_ptr_t)(uint16_t addr, uint8_t value);

uint64_t maincpu_clk;

extern read_func_ptr_t *_mem_read_tab_ptr;
extern store_func_ptr_t *_mem_write_tab_ptr;
extern read_func_ptr_t *_mem_read_tab_ptr_dummy;
extern store_func_ptr_t *_mem_write_tab_ptr_dummy;

extern void mem_initialize_memory(void);
extern void mem_mmu_translate(unsigned int addr, uint8_t **base, int *start, int *limit);
extern void mem_toggle_watchpoints(int flag, void *context);
extern void vic_cycle(void);
extern void double_rewrite_read_page(int page);
extern int double_rewrite_row_read_page(int config, int page);
extern int double_bank(int config);
extern void double_alias_dummy(void);
extern void double_foreign(void);
extern void vice_sharp_bus_cycle(uint64_t clock);

void vice_sharp_observe_clock(uint64_t clock)
{
    vice_sharp_bus_cycle(clock);
}

void vice_sharp_bus_sink(const char *kind, const char *detail)
{
    printf("%s,%s\n", kind, detail);
}

void vice_sharp_bus_on_access(const char *kind, uint16_t addr, uint8_t data)
{
    (void)kind;
    (void)addr;
    (void)data;
}

static int parse_number(const char *text, uint64_t *value)
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

static int split(char *token, char **op, uint64_t *a, uint64_t *b, int *arity)
{
    char *first = strchr(token, ':');
    char *second;
    *op = token;
    *arity = 0;
    if (first == NULL) {
        return 1;
    }
    *first = '\0';
    second = strchr(first + 1, ':');
    if (second != NULL) {
        *second = '\0';
        if (!parse_number(second + 1, b)) {
            return 0;
        }
        *arity = 2;
    } else {
        *arity = 1;
    }
    return parse_number(first + 1, a);
}

int main(int argc, char **argv)
{
    int i;
    for (i = 1; i < argc; i++) {
        char *op;
        uint64_t a = 0, b = 0;
        int arity;
        uint8_t *base;
        int start, limit;
        if (!split(argv[i], &op, &a, &b, &arity)) {
            return 2;
        }
        if (strcmp(op, "init") == 0 && arity == 0) {
            mem_initialize_memory();
        } else if (strcmp(op, "cycle") == 0 && arity == 0) {
            vic_cycle();
        } else if (strcmp(op, "alias") == 0 && arity == 0) {
            double_alias_dummy();
        } else if (strcmp(op, "foreign") == 0 && arity == 0) {
            double_foreign();
        } else if (strcmp(op, "keep") == 0 && arity == 0) {
#if defined(_WIN32)
            if (_putenv("VICESHARP_FAST_PATH=keep") != 0) {
                return 2;
            }
#else
            if (setenv("VICESHARP_FAST_PATH", "keep", 1) != 0) {
                return 2;
            }
#endif
        } else if (strcmp(op, "clock") == 0 && arity == 1) {
            maincpu_clk = a;
        } else if (strcmp(op, "read") == 0 && arity == 1 && a <= 0xffff) {
            (void)_mem_read_tab_ptr[a >> 8]((uint16_t)a);
        } else if (strcmp(op, "dread") == 0 && arity == 1 && a <= 0xffff) {
            (void)_mem_read_tab_ptr_dummy[a >> 8]((uint16_t)a);
        } else if (strcmp(op, "write") == 0 && arity == 2 && a <= 0xffff && b <= 0xff) {
            _mem_write_tab_ptr[a >> 8]((uint16_t)a, (uint8_t)b);
        } else if (strcmp(op, "dwrite") == 0 && arity == 2 && a <= 0xffff && b <= 0xff) {
            _mem_write_tab_ptr_dummy[a >> 8]((uint16_t)a, (uint8_t)b);
        } else if (strcmp(op, "translate") == 0 && arity == 1 && a <= 0xffff) {
            mem_mmu_translate((unsigned int)a, &base, &start, &limit);
        } else if (strcmp(op, "rewrite") == 0 && arity == 1 && a <= 0x100) {
            double_rewrite_read_page((int)a);
        } else if (strcmp(op, "toggle") == 0 && arity == 1 && a <= 2) {
            mem_toggle_watchpoints((int)a, NULL);
        } else if (strcmp(op, "bank") == 0 && arity == 1 && a <= 0xff) {
            if (double_bank((int)a) != 0) {
                return 2;
            }
        } else if (strcmp(op, "rewriterow") == 0 && arity == 2 && a <= 0xff && b <= 0x100) {
            if (double_rewrite_row_read_page((int)a, (int)b) != 0) {
                return 2;
            }
        } else {
            return 2;
        }
    }
    return ferror(stdout) ? 3 : 0;
}
