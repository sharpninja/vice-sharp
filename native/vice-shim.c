#include "vice-shim.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "archdep.h"
#include "archdep_set_openmp_wait_policy.h"
#include "archdep_startup_log_error.h"
#include "archdep_tick.h"
#include "cia.h"
#include "drive.h"
#include "gfxoutput.h"
#include "init.h"
#include "lib.h"
#include "log.h"
#include "machine.h"
#include "maincpu.h"
#include "main.h"
#include "mem.h"
#include "mos6510.h"
#include "resources.h"
#include "screenshot.h"
#include "sid.h"
#include "sid/sid-snapshot.h"
#include "sysfile.h"
#include "uiapi.h"
#include "video.h"
#include "c64/c64.h"
#include "vice-shim-runtime.h"
#include "viciisc/viciitypes.h"

int archdep_init(int *argc, char **argv);

typedef struct vice_machine_s {
    uint32_t magic;
} vice_machine_t;

enum {
    VICE_MACHINE_MAGIC = 0x56494345u
};

static INIT_ONCE g_sync_once = INIT_ONCE_STATIC_INIT;
static CRITICAL_SECTION g_state_lock;
static CONDITION_VARIABLE g_state_cv;

static vice_machine_t *g_active_machine;
static HANDLE g_worker_thread;
static int g_worker_running;
static int g_runtime_initialized;
static int g_runtime_failed;
static int g_bootstrap_pending;
static int g_stop_requested;
static unsigned int g_granted_cycles;
static int g_cycle_paused;

extern CLOCK stolen_cycles;
extern int check_ba_low;
extern int maincpu_ba_low_flags;
extern vicii_t vicii;

static BOOL CALLBACK vice_shim_initialize_sync(PINIT_ONCE init_once, PVOID parameter, PVOID *context)
{
    (void)init_once;
    (void)parameter;
    (void)context;

    InitializeCriticalSection(&g_state_lock);
    InitializeConditionVariable(&g_state_cv);
    return TRUE;
}

static void vice_shim_ensure_sync_primitives(void)
{
    InitOnceExecuteOnce(&g_sync_once, vice_shim_initialize_sync, NULL, NULL);
}

static int vice_shim_is_active_machine(const void *machine)
{
    const vice_machine_t *instance = (const vice_machine_t *)machine;
    return instance != NULL
        && instance->magic == VICE_MACHINE_MAGIC
        && instance == g_active_machine;
}

static int vice_shim_get_module_directory(char *buffer, size_t buffer_size)
{
    HMODULE module = NULL;
    DWORD length;
    char *separator;

    if (!GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                            (LPCSTR)&vice_machine_create,
                            &module)) {
        return 0;
    }

    length = GetModuleFileNameA(module, buffer, (DWORD)buffer_size);
    if (length == 0 || length >= buffer_size) {
        return 0;
    }

    separator = strrchr(buffer, '\\');
    if (separator == NULL) {
        separator = strrchr(buffer, '/');
    }

    if (separator == NULL) {
        return 0;
    }

    *separator = '\0';
    return 1;
}

static int vice_shim_compose_path(char *buffer,
                                  size_t buffer_size,
                                  const char *base,
                                  const char *relative_path)
{
    int written = snprintf(buffer, buffer_size, "%s\\%s", base, relative_path);
    return written > 0 && (size_t)written < buffer_size;
}

static int vice_shim_initialize_runtime_locked(void)
{
    char module_directory[MAX_PATH];
    char program_path[MAX_PATH];
    char data_directory[MAX_PATH];
    char *argv[1];
    int argc = 1;

    if (g_runtime_initialized) {
        return 1;
    }

    if (g_runtime_failed) {
        return 0;
    }

    if (!vice_shim_get_module_directory(module_directory, sizeof(module_directory))
        || !vice_shim_compose_path(program_path, sizeof(program_path), module_directory, "vice\\vice\\src\\x64sc.exe")
        || !vice_shim_compose_path(data_directory, sizeof(data_directory), module_directory, "vice\\vice\\data")) {
        g_runtime_failed = 1;
        return 0;
    }

    console_mode = true;
    default_settings_requested = true;
    help_requested = false;
    video_disabled_mode = false;

    archdep_set_openmp_wait_policy();
    lib_init();
    log_set_limit_early(LOG_LIMIT_STANDARD);

    argv[0] = program_path;
    if (archdep_init(&argc, argv) != 0) {
        archdep_startup_log_error("archdep_init failed.\n");
        g_runtime_failed = 1;
        return 0;
    }

    tick_init();
    maincpu_early_init();
    machine_setup_context();
    drive_setup_context();
    machine_early_init();
    sysfile_init(machine_name);

    if (init_resources() < 0
        || init_cmdline_options() < 0
        || gfxoutput_early_init((int)help_requested) < 0
        || gfxoutput_resources_init() < 0
        || gfxoutput_cmdline_options_init() < 0
        || screenshot_cmdline_options_init() < 0
        || resources_set_defaults() < 0
        || resources_set_string("Directory", data_directory) < 0
        || log_init() < 0
        || video_init() < 0
        || init_main() < 0) {
        g_runtime_failed = 1;
        return 0;
    }

    g_runtime_initialized = 1;
    return 1;
}

static unsigned __stdcall vice_shim_worker_main(void *parameter)
{
    (void)parameter;

    maincpu_mainloop();

    EnterCriticalSection(&g_state_lock);
    g_worker_running = 0;
    g_cycle_paused = 1;
    WakeAllConditionVariable(&g_state_cv);
    LeaveCriticalSection(&g_state_lock);

    return 0;
}

static void vice_shim_stop_worker(void *machine)
{
    HANDLE worker = NULL;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine) || g_worker_thread == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    g_stop_requested = 1;
    WakeAllConditionVariable(&g_state_cv);
    worker = g_worker_thread;
    LeaveCriticalSection(&g_state_lock);

    WaitForSingleObject(worker, INFINITE);
    CloseHandle(worker);

    EnterCriticalSection(&g_state_lock);
    if (g_worker_thread == worker) {
        g_worker_thread = NULL;
    }
    g_worker_running = 0;
    g_stop_requested = 0;
    g_granted_cycles = 0;
    g_cycle_paused = 0;
    LeaveCriticalSection(&g_state_lock);
}

static void vice_shim_reset_cpu_state_locked(void)
{
    const uint16_t reset_vector = (uint16_t)(mem_read(0xfffc) | (mem_read(0xfffd) << 8));

    maincpu_clk_limit = 0;
    maincpu_ba_low_flags = 0;
    stolen_cycles = 0;
    check_ba_low = 0;
    maincpu_rmw_flag = 0;
    last_opcode_info = 0;
    last_opcode_addr = reset_vector;

    maincpu_regs.a = 0;
    maincpu_regs.x = 0;
    maincpu_regs.y = 0;
    maincpu_regs.sp = 0;
    maincpu_regs.p = P_INTERRUPT;
    maincpu_regs.n = 0;
    maincpu_regs.z = 0;
    maincpu_regs.pc = reset_vector;

    g_bootstrap_pending = 1;
}

VICE_SHIM_API void *vice_machine_create(void)
{
    vice_machine_t *machine;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    while (g_active_machine != NULL) {
        SleepConditionVariableCS(&g_state_cv, &g_state_lock, INFINITE);
    }

    if (!vice_shim_initialize_runtime_locked()) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    machine = (vice_machine_t *)calloc(1, sizeof(*machine));
    if (machine == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    machine->magic = VICE_MACHINE_MAGIC;
    g_active_machine = machine;
    LeaveCriticalSection(&g_state_lock);

    return machine;
}

VICE_SHIM_API void vice_machine_destroy(void *machine)
{
    vice_machine_t *instance = (vice_machine_t *)machine;

    if (instance == NULL) {
        return;
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_active_machine == instance) {
        g_active_machine = NULL;
        g_bootstrap_pending = 0;
        WakeAllConditionVariable(&g_state_cv);
    }
    LeaveCriticalSection(&g_state_lock);

    instance->magic = 0;
    free(instance);
}

VICE_SHIM_API void vice_machine_reset(void *machine)
{
    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    maincpu_reset();
    vice_shim_reset_cpu_state_locked();
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API void vice_machine_step_cycle(void *machine)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    if (g_worker_thread == NULL) {
        uintptr_t worker_handle;

        g_stop_requested = 0;
        g_granted_cycles = 1;
        g_cycle_paused = 0;
        g_worker_running = 1;

        worker_handle = _beginthreadex(NULL, 0, vice_shim_worker_main, NULL, 0, NULL);
        if (worker_handle == 0) {
            g_worker_running = 0;
            g_granted_cycles = 0;
            LeaveCriticalSection(&g_state_lock);
            return;
        }

        g_worker_thread = (HANDLE)worker_handle;
    } else {
        g_granted_cycles++;
        g_cycle_paused = 0;
        WakeAllConditionVariable(&g_state_cv);
    }

    while (g_worker_running && !g_cycle_paused) {
        SleepConditionVariableCS(&g_state_cv, &g_state_lock, INFINITE);
    }
    LeaveCriticalSection(&g_state_lock);
}

int vice_shim_cycle_checkpoint(void)
{
    int should_stop;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_granted_cycles > 0) {
        g_granted_cycles--;
    }

    if (g_granted_cycles == 0) {
        g_cycle_paused = 1;
        WakeAllConditionVariable(&g_state_cv);
        while (!g_stop_requested && g_granted_cycles == 0) {
            SleepConditionVariableCS(&g_state_cv, &g_state_lock, INFINITE);
        }
    }

    g_cycle_paused = 0;
    should_stop = g_stop_requested;
    LeaveCriticalSection(&g_state_lock);

    return should_stop;
}

int vice_shim_take_bootstrap_maincpu(void)
{
    int bootstrap_pending;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    bootstrap_pending = g_bootstrap_pending;
    g_bootstrap_pending = 0;
    LeaveCriticalSection(&g_state_lock);

    return bootstrap_pending;
}

VICE_SHIM_API uint8_t vice_cpu_get_a(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.a;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_x(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.x;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_y(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.y;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_p(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = (uint8_t)MOS6510_REGS_GET_STATUS(&maincpu_regs);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_sp(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.sp;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint16_t vice_cpu_get_pc(void *machine)
{
    uint16_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = (uint16_t)maincpu_regs.pc;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API void vice_vic_get_state(void *machine, struct vice_vic_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        state->cycle = (uint32_t)maincpu_clk;
        state->raster_line = (uint16_t)vicii.raster_line;
        state->raster_cycle = (uint8_t)vicii.raster_cycle;
        state->bad_line = (uint8_t)(vicii.bad_line != 0);
        state->display_state = 0;
        state->sprite_dma = vicii.sprite_dma;
        memcpy(state->registers, vicii.regs, sizeof(state->registers));
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API void vice_cia_get_state(void *machine, int cia_index, struct vice_cia_state *state)
{
    cia_context_t *cia = NULL;

    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        cia = cia_index == 0 ? machine_context.cia1 : machine_context.cia2;
        if (cia != NULL) {
            state->port_a = cia->c_cia[CIA_PRA];
            state->port_b = cia->c_cia[CIA_PRB];
            state->ddr_a = cia->c_cia[CIA_DDRA];
            state->ddr_b = cia->c_cia[CIA_DDRB];
            state->timer_a = (uint16_t)(cia->c_cia[CIA_TAL] | (cia->c_cia[CIA_TAH] << 8));
            state->timer_b = (uint16_t)(cia->c_cia[CIA_TBL] | (cia->c_cia[CIA_TBH] << 8));
            state->icr = cia->c_cia[CIA_ICR];
            state->cra = cia->c_cia[CIA_CRA];
            state->crb = cia->c_cia[CIA_CRB];
            state->interrupt_flag = (uint8_t)(cia->irqflags & 0xff);
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API void vice_sid_get_state(void *machine, struct vice_sid_state *state)
{
    sid_snapshot_state_t snapshot;
    uint8_t *sid_registers;

    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        sid_registers = sid_get_siddata(0);
        if (sid_registers != NULL) {
            memcpy(state->registers, sid_registers, sizeof(state->registers));
        }

        memset(&snapshot, 0, sizeof(snapshot));
        sid_state_read(0, &snapshot);
        memcpy(state->accumulators, snapshot.accumulator, sizeof(state->accumulators));
        memcpy(state->envelopes, snapshot.envelope_counter, sizeof(state->envelopes));
        state->filter_state = 0;
    }
    LeaveCriticalSection(&g_state_lock);
}
