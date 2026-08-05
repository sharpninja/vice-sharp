/*
 * ViceSharp hosted xvic shim (Iteration 2 / TR-VIC20-LOCKSTEP-001).
 * Separate DLL (vice_xvic.dll) from the C64 oracle (vice_x64.dll) because
 * VICE machine_class / maincpu / chip globals are process-global per binary.
 *
 * Surface matches the subset of native/vice-shim.h that Vic20 lockstep needs:
 * create/destroy/reset/step, CPU regs, memory peek/read/write, disk attach,
 * keyboard matrix, VIC-I timing via vice_vic_get_state (cycle + raster + regs).
 * CIA/SID/VIC-II-specific APIs are stubs that zero/no-op so a shared managed
 * binding can load either DLL without missing entry points.
 */

#include "vice-shim.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>

#include <ctype.h>
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
#include "attach.h"
#include "drive.h"
#include "drivetypes.h"
#include "gfxoutput.h"
#include "init.h"
#include "interrupt.h"
#include "keyboard.h"
#include "lib.h"
#include "log.h"
#include "machine.h"
#include "maincpu.h"
#include "main.h"
#include "mem.h"
#include "mos6510.h"
#include "resources.h"
#include "screenshot.h"
#include "snapshot.h"
#include "sysfile.h"
#include "video.h"
#include "vic20/vic20model.h"
#include "vic20/victypes.h"
#include "vic20/vic.h"
#include "vice-shim-runtime.h"

int archdep_init(int *argc, char **argv);

typedef struct vice_machine_s {
    uint32_t magic;
    int vic20_model;
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
static unsigned int g_debug_step_calls;
static unsigned int g_debug_checkpoint_calls;
static unsigned int g_debug_create_calls;
static unsigned int g_debug_reset_calls;
#define VICE_SHIM_CREATE_TIMEOUT_MS 5000u
#define VICE_SHIM_STEP_TIMEOUT_MS 5000u
#define VICE_SHIM_STOP_TIMEOUT_MS 2000u

extern CLOCK stolen_cycles;

static int vice_shim_model_from_selector(const char *selector);
static int vice_shim_selector_equals(const char *left, const char *right);
static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive);
static void vice_shim_set_current_thread_description(const wchar_t *description);

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

static int vice_shim_wait_for_signal_with_timeout(unsigned int timeout_ms)
{
    return SleepConditionVariableCS(&g_state_cv, &g_state_lock, (DWORD)timeout_ms) != 0;
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
        || !vice_shim_compose_path(program_path, sizeof(program_path), module_directory, "vice\\vice\\src\\xvic.exe")
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

    /* xvic binary: machine_class is already VICE_MACHINE_VIC20 via vic20.c. */
    machine_class = VICE_MACHINE_VIC20;

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
        || resources_set_int("RAMInitRandomChance", 0) < 0
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

    vice_shim_set_current_thread_description(L"ViceSharp.NativeViceShim.Xvic");

    maincpu_mainloop();

    EnterCriticalSection(&g_state_lock);
    g_worker_running = 0;
    g_cycle_paused = 1;
    WakeAllConditionVariable(&g_state_cv);
    LeaveCriticalSection(&g_state_lock);

    return 0;
}

typedef HRESULT (WINAPI *vice_shim_set_thread_description_fn)(HANDLE thread, PCWSTR description);

static void vice_shim_set_current_thread_description(const wchar_t *description)
{
    HMODULE kernel32;
    vice_shim_set_thread_description_fn set_thread_description;

    kernel32 = GetModuleHandleW(L"Kernel32.dll");
    if (kernel32 == NULL) {
        return;
    }

    set_thread_description = (vice_shim_set_thread_description_fn)GetProcAddress(kernel32, "SetThreadDescription");
    if (set_thread_description == NULL) {
        return;
    }

    (void)set_thread_description(GetCurrentThread(), description);
}

static int vice_shim_stop_worker(void *machine)
{
    HANDLE worker = NULL;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine) || g_worker_thread == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return 1;
    }

    g_stop_requested = 1;
    WakeAllConditionVariable(&g_state_cv);
    worker = g_worker_thread;
    LeaveCriticalSection(&g_state_lock);

    if (WaitForSingleObject(worker, VICE_SHIM_STOP_TIMEOUT_MS) != WAIT_OBJECT_0) {
        return 0;
    }
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

    return 1;
}

static void vice_shim_reset_cpu_state_locked(void)
{
    const uint16_t reset_vector = (uint16_t)(mem_read(0xfffc) | (mem_read(0xfffd) << 8));

    maincpu_clk_limit = 0;
    stolen_cycles = 0;
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

static void vice_shim_rebaseline_drives_locked(void)
{
    unsigned int unit;
    char resource_name[32];
    unsigned int clk_unit, clk_drive;

    file_system_detach_disk_all();

    for (unit = 8; unit <= 11; unit++) {
        snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
        resources_set_int(resource_name, 1);
    }

    for (clk_unit = 0; clk_unit < NUM_DISK_UNITS; clk_unit++) {
        if (diskunit_context[clk_unit] == NULL) {
            continue;
        }
        for (clk_drive = 0; clk_drive < NUM_DRIVES; clk_drive++) {
            drive_t *clk_dptr = diskunit_context[clk_unit]->drives[clk_drive];
            if (clk_dptr != NULL) {
                clk_dptr->attach_clk = 0;
                clk_dptr->detach_clk = 0;
                clk_dptr->attach_detach_clk = 0;
            }
        }
    }
}

VICE_SHIM_API void *vice_machine_create(void)
{
    return vice_machine_create_model(NULL);
}

VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)
{
    if (g_debug_create_calls < 8) {
        fprintf(stderr, "xvic vice_machine_create call=%u model=%s\n",
                ++g_debug_create_calls,
                model_selector == NULL ? "default" : model_selector);
        fflush(stderr);
    }

    vice_machine_t *machine;
    int model;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    while (g_active_machine != NULL) {
        SleepConditionVariableCS(&g_state_cv, &g_state_lock, INFINITE);
    }

    if (!vice_shim_initialize_runtime_locked()) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    model = vice_shim_model_from_selector(model_selector);
    if (model == VIC20MODEL_UNKNOWN) {
        fprintf(stderr, "xvic vice_machine_create unknown model=%s\n",
                model_selector == NULL ? "(null)" : model_selector);
        fflush(stderr);
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    vic20model_set(model);
    vice_shim_rebaseline_drives_locked();

    machine = (vice_machine_t *)calloc(1, sizeof(*machine));
    if (machine == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    machine->magic = VICE_MACHINE_MAGIC;
    machine->vic20_model = model;
    g_active_machine = machine;
    LeaveCriticalSection(&g_state_lock);

    return machine;
}

static int vice_shim_model_from_selector(const char *selector)
{
    if (selector == NULL || selector[0] == '\0'
        || vice_shim_selector_equals(selector, "vic20")
        || vice_shim_selector_equals(selector, "vic20pal")
        || vice_shim_selector_equals(selector, "pal")
        || vice_shim_selector_equals(selector, "xvic")) {
        return VIC20MODEL_VIC20_PAL;
    }

    if (vice_shim_selector_equals(selector, "vic20ntsc")
        || vice_shim_selector_equals(selector, "ntsc")) {
        return VIC20MODEL_VIC20_NTSC;
    }

    if (vice_shim_selector_equals(selector, "vic21")
        || vice_shim_selector_equals(selector, "supervic")) {
        return VIC20MODEL_VIC21;
    }

    if (vice_shim_selector_equals(selector, "vic1001")
        || vice_shim_selector_equals(selector, "jap")) {
        return VIC20MODEL_VIC1001;
    }

    return VIC20MODEL_UNKNOWN;
}

static int vice_shim_selector_equals(const char *left, const char *right)
{
    if (left == NULL || right == NULL) {
        return 0;
    }

    while (*left != '\0' && *right != '\0') {
        if (tolower((unsigned char)*left) != tolower((unsigned char)*right)) {
            return 0;
        }

        left++;
        right++;
    }

    return *left == '\0' && *right == '\0';
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
        file_system_detach_disk_all();
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
    if (g_debug_reset_calls < 8) {
        fprintf(stderr, "xvic vice_machine_reset call=%u machine=%p\n", ++g_debug_reset_calls, machine);
        fflush(stderr);
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    {
        vice_machine_t *instance = (vice_machine_t *)machine;
        vic20model_set(instance->vic20_model);
    }

    machine_powerup();
    mem_powerup();
    vic_reset();
    maincpu_reset();
    vice_shim_reset_cpu_state_locked();
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_read_snapshot(void *machine, const char *path)
{
    int result;

    if (path == NULL) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    {
        vice_machine_t *instance = (vice_machine_t *)machine;
        vic20model_set(instance->vic20_model);
    }
    result = machine_read_snapshot(path, 0);
    if (result == 0) {
        snapshot_set_error(SNAPSHOT_NO_ERROR);
    }
    g_bootstrap_pending = 1;

    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_machine_write_snapshot(void *machine, const char *path)
{
    int result;

    if (path == NULL) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    result = machine_write_snapshot(path, 1, 1, 0);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_snapshot_last_error(void)
{
    return snapshot_get_error();
}

VICE_SHIM_API int vice_machine_attach_cartridge(void *machine, const uint8_t *image, int length, int mapping_mode)
{
    (void)machine;
    (void)image;
    (void)length;
    (void)mapping_mode;
    /* Vic20 cart attach is a later slice; fail closed for now. */
    return -1;
}

VICE_SHIM_API int vice_machine_attach_disk(void *machine, unsigned int unit, unsigned int drive, const char *path)
{
    int result;

    if (path == NULL || !vice_shim_valid_disk_slot(unit, drive)) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    result = file_system_attach_disk(unit, drive, path);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_machine_detach_disk(void *machine, unsigned int unit, unsigned int drive)
{
    if (!vice_shim_valid_disk_slot(unit, drive)) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    file_system_detach_disk(unit, drive);
    LeaveCriticalSection(&g_state_lock);
    return 0;
}

static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive)
{
    return unit >= 8 && unit <= 11 && drive <= 1;
}

VICE_SHIM_API uint8_t vice_machine_peek_ram(void *machine, uint16_t address)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = mem_ram[address];
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_machine_read(void *machine, uint16_t address)
{
    uint8_t value = 0xff;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = mem_read(address);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API void vice_machine_write(void *machine, uint16_t address, uint8_t value)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        mem_store(address, value);
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_get_model(void *machine)
{
    int model = VIC20MODEL_UNKNOWN;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        model = vic20model_get();
    }
    LeaveCriticalSection(&g_state_lock);

    return model;
}

VICE_SHIM_API int vice_machine_set_keyboard_matrix_key(void *machine, int row, int column, int pressed)
{
    if (row < 0 || row > 7 || column < 0 || column > 7) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    keyboard_set_keyarr(row, column, pressed != 0);
    LeaveCriticalSection(&g_state_lock);
    return 0;
}

VICE_SHIM_API void vice_machine_cia1_store(void *machine, uint8_t register_index, uint8_t value)
{
    (void)machine;
    (void)register_index;
    (void)value;
}

VICE_SHIM_API uint8_t vice_machine_cia1_read(void *machine, uint8_t register_index)
{
    (void)machine;
    (void)register_index;
    return 0xff;
}

VICE_SHIM_API void vice_machine_step_cycle(void *machine)
{
    if (g_debug_step_calls < 16) {
        fprintf(stderr, "xvic vice_machine_step_cycle call=%u machine=%p\n", ++g_debug_step_calls, machine);
        fflush(stderr);
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    if (g_worker_thread == NULL || !g_worker_running) {
        if (g_worker_thread != NULL) {
            CloseHandle(g_worker_thread);
            g_worker_thread = NULL;
            g_worker_running = 0;
            g_cycle_paused = 1;
            g_stop_requested = 0;
            g_granted_cycles = 0;
        }

        {
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
        }
    } else {
        g_granted_cycles++;
        g_cycle_paused = 0;
        WakeAllConditionVariable(&g_state_cv);
    }

    while (g_worker_running && !g_cycle_paused) {
        if (!vice_shim_wait_for_signal_with_timeout(VICE_SHIM_STEP_TIMEOUT_MS)) {
            g_stop_requested = 1;
            g_granted_cycles = 0;
            g_cycle_paused = 1;
            WakeAllConditionVariable(&g_state_cv);
            break;
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

int vice_shim_cycle_checkpoint(void)
{
    int should_stop;

    if (g_debug_checkpoint_calls < 16) {
        fprintf(stderr, "xvic cycle_checkpoint: granted=%u running=%d paused=%d stop=%d\n",
                g_granted_cycles, g_worker_running, g_cycle_paused, g_stop_requested);
        fflush(stderr);
        ++g_debug_checkpoint_calls;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_granted_cycles > 0) {
        g_granted_cycles--;
    }

    if (g_granted_cycles == 0) {
        g_cycle_paused = 1;
        WakeAllConditionVariable(&g_state_cv);
        while (!g_stop_requested && g_granted_cycles == 0) {
            if (!vice_shim_wait_for_signal_with_timeout(VICE_SHIM_STEP_TIMEOUT_MS)) {
                g_stop_requested = 1;
                g_granted_cycles = 0;
                g_cycle_paused = 1;
                WakeAllConditionVariable(&g_state_cv);
                break;
            }
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
        /* Reconstruct architectural P from split n/z + p bits (same as C64
           shim). Reading .p alone drops N/Z and the constant 1 bit. */
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
        value = maincpu_regs.pc;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API void vice_cpu_get_pipeline_state(void *machine, struct vice_cpu_pipeline_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        state->clk = (uint64_t)maincpu_clk;
        state->last_opcode_info = (uint32_t)last_opcode_info;
        /* Vic20 has no BA-low / 6510 pport; leave those zero. */
        if (maincpu_int_status != NULL) {
            state->global_pending_int = (uint32_t)maincpu_int_status->global_pending_int;
            state->irq_clk = (uint64_t)maincpu_int_status->irq_clk;
            state->nmi_clk = (uint64_t)maincpu_int_status->nmi_clk;
            state->irq_delay_cycles = (uint64_t)maincpu_int_status->irq_delay_cycles;
            state->nmi_delay_cycles = (uint64_t)maincpu_int_status->nmi_delay_cycles;
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

/* Drive CPU stubs: present for ABI parity; return zero until true-drive Vic20
   lockstep is wired. */
VICE_SHIM_API uint8_t vice_drivecpu_get_a(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_x(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_y(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_p(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_sp(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint16_t vice_drivecpu_get_pc(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API int vice_drive_get_clock_residue(void *machine, unsigned int unit, uint64_t *attach_clk, uint64_t *detach_clk, uint64_t *attach_detach_clk)
{
    (void)machine;
    (void)unit;
    if (attach_clk) {
        *attach_clk = 0;
    }
    if (detach_clk) {
        *detach_clk = 0;
    }
    if (attach_detach_clk) {
        *attach_detach_clk = 0;
    }
    return -1;
}

VICE_SHIM_API uint64_t vice_drivecpu_get_cycle_accum(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API int vice_drivecpu_set_cycle_accum(void *machine, unsigned int unit, uint64_t value)
{
    (void)machine;
    (void)unit;
    (void)value;
    return -1;
}

VICE_SHIM_API int vice_drive_set_true_emulation(void *machine, unsigned int unit, int enabled)
{
    char resource_name[32];
    int result;

    if (unit < 8 || unit > 11) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
    result = resources_set_int(resource_name, enabled != 0 ? 1 : 0);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_drive_get_true_emulation(void *machine, unsigned int unit)
{
    char resource_name[32];
    int value = 0;

    if (unit < 8 || unit > 11) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
    (void)resources_get_int(resource_name, &value);
    LeaveCriticalSection(&g_state_lock);
    return value;
}

/*
 * Reuse the C64-shaped vice_vic_state export for Vic-I timing so managed
 * GetVicState / cycle baselining works without a second ABI. Raster fields
 * map from the VIC-I core; unused VIC-II fields stay zero.
 */
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
        state->raster_line = (uint16_t)vic.raster_line;
        state->raster_cycle = (uint8_t)vic.raster_cycle;
        state->bad_line = 0;
        state->display_state = (uint8_t)vic.area;
        state->sprite_dma = 0;
        state->allow_bad_lines = 0;
        state->idle_state = (uint8_t)vic.fetch_state;
        memcpy(state->registers, vic.regs, sizeof(vic.regs));
        memcpy(state->registers_peek, vic.regs, sizeof(vic.regs));
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_capture_visible_frame(void *machine, uint8_t *buffer, int length, int *width, int *height)
{
    (void)machine;
    (void)buffer;
    (void)length;
    (void)width;
    (void)height;
    return 0;
}

VICE_SHIM_API int vice_vic_capture_frame_indices(void *machine, uint8_t *buffer, int length, int *width, int *height)
{
    (void)machine;
    (void)buffer;
    (void)length;
    (void)width;
    (void)height;
    return 0;
}

VICE_SHIM_API int vice_vic_get_graphics_priority_at_raster(void *machine, uint16_t raster_line, uint8_t *pri_buffer, int length)
{
    (void)machine;
    (void)raster_line;
    (void)pri_buffer;
    (void)length;
    return 0;
}

VICE_SHIM_API void vice_cia_get_state(void *machine, int cia_index, struct vice_cia_state *state)
{
    (void)machine;
    (void)cia_index;
    if (state != NULL) {
        memset(state, 0, sizeof(*state));
    }
}

VICE_SHIM_API void vice_sid_get_state(void *machine, struct vice_sid_state *state)
{
    (void)machine;
    if (state != NULL) {
        memset(state, 0, sizeof(*state));
    }
}

VICE_SHIM_API size_t vice_sid_render_samples(void *machine, int16_t *buffer, size_t n, int delta_t_cycles)
{
    (void)machine;
    (void)buffer;
    (void)n;
    (void)delta_t_cycles;
    return 0;
}

VICE_SHIM_API uint8_t vice_sid_engine_read(void *machine, uint16_t addr)
{
    (void)machine;
    (void)addr;
    return 0xff;
}

VICE_SHIM_API void vice_sid_clock(void *machine, int cycles)
{
    (void)machine;
    (void)cycles;
}

VICE_SHIM_API int vice_sid_exact_open(void *machine)
{
    (void)machine;
    return 0;
}

VICE_SHIM_API void vice_sid_exact_reset(void *machine)
{
    (void)machine;
}

VICE_SHIM_API int vice_sid_exact_clock(void *machine, int cycles)
{
    (void)machine;
    (void)cycles;
    return 0;
}

VICE_SHIM_API void vice_sid_exact_write(void *machine, uint16_t addr, uint8_t value)
{
    (void)machine;
    (void)addr;
    (void)value;
}

VICE_SHIM_API uint8_t vice_sid_exact_read(void *machine, uint16_t addr)
{
    (void)machine;
    (void)addr;
    return 0xff;
}

VICE_SHIM_API int16_t vice_sid_exact_output(void *machine)
{
    (void)machine;
    return 0;
}

VICE_SHIM_API void vice_sid_exact_get_state(void *machine, struct vice_sid_exact_state *state)
{
    (void)machine;
    if (state != NULL) {
        memset(state, 0, sizeof(*state));
    }
}

VICE_SHIM_API int vice_sid_exact_set_sampling(void *machine, int method, double sample_freq, double pass_freq, double filter_scale)
{
    (void)machine;
    (void)method;
    (void)sample_freq;
    (void)pass_freq;
    (void)filter_scale;
    return 0;
}

VICE_SHIM_API int vice_sid_exact_clock_buffered(void *machine, int cycles, int16_t *buffer, int buffer_len, int *cycles_remaining)
{
    (void)machine;
    (void)cycles;
    (void)buffer;
    (void)buffer_len;
    if (cycles_remaining != NULL) {
        *cycles_remaining = cycles;
    }
    return 0;
}

VICE_SHIM_API void vice_interrupt_get_state(void *machine, struct vice_interrupt_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && maincpu_int_status != NULL) {
        state->global_pending = (uint8_t)maincpu_int_status->global_pending_int;
        state->irq_asserted = (uint8_t)((maincpu_int_status->global_pending_int & IK_IRQ) != 0);
        state->nmi_asserted = (uint8_t)((maincpu_int_status->global_pending_int & IK_NMI) != 0);
    }
    LeaveCriticalSection(&g_state_lock);
}
