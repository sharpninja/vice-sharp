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
#include "cia.h"
#include "drive.h"
#include "gfxoutput.h"
#include "init.h"
#include "interrupt.h"
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
#include "c64/c64model.h"
#include "cartridge.h"
#include "vicii.h"
#include "vice-shim-runtime.h"
#include "viciisc/viciitypes.h"

int archdep_init(int *argc, char **argv);

typedef struct vice_machine_s {
    uint32_t magic;
    int c64_model;
    int cartridge_attached;
    int cartridge_mapping_mode;
    int cartridge_length;
    uint8_t cartridge_image[0x80000];
} vice_machine_t;

enum {
    VICE_MACHINE_MAGIC = 0x56494345u,
    VICE_SHIM_CART_MAPPING_AUTO = 0,
    VICE_SHIM_CART_MAPPING_STANDARD_8K = 1,
    VICE_SHIM_CART_MAPPING_STANDARD_16K = 2,
    VICE_SHIM_CART_MAPPING_ULTIMAX = 3,
    VICE_SHIM_CART_MAPPING_GAME_SYSTEM = 4,
    VICE_SHIM_CART_BANK_SIZE = 0x2000,
    VICE_SHIM_CART_GENERIC_IMAGE_LIMIT = 0x4000,
    VICE_SHIM_CART_GAME_SYSTEM_IMAGE_SIZE = 0x80000,
    VICE_SHIM_CART_IMAGE_LIMIT = 0x80000
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
extern int check_ba_low;
extern int maincpu_ba_low_flags;
extern vicii_t vicii;

static int vice_shim_model_from_selector(const char *selector);
static int vice_shim_selector_equals(const char *left, const char *right);
static int vice_shim_apply_cartridge_locked(const vice_machine_t *instance);
static int vice_shim_cartridge_type_for(const vice_machine_t *instance, int length, int mapping_mode);
static void vice_shim_detach_cartridge_locked(void);
static int vice_shim_write_temp_file(const uint8_t *data, int length, char *path, size_t path_size);

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

    if (WaitForSingleObject(worker, VICE_SHIM_STOP_TIMEOUT_MS) != WAIT_OBJECT_0) {
        // If shutdown takes too long, treat it as non-fatal and continue.
        // The worker should eventually terminate on its own.
        g_worker_running = 0;
        g_cycle_paused = 1;
        g_stop_requested = 0;
        g_granted_cycles = 0;
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
    return vice_machine_create_model(NULL);
}

VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)
{
    if (g_debug_create_calls < 8) {
        fprintf(stderr, "vice_machine_create call=%u model=%s\\n", ++g_debug_create_calls, model_selector == NULL ? "default" : model_selector);
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
    if (model == C64MODEL_UNKNOWN) {
        fprintf(stderr, "vice_machine_create unknown model=%s\\n", model_selector == NULL ? "(null)" : model_selector);
        fflush(stderr);
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    c64model_set(model);
    vice_shim_detach_cartridge_locked();

    machine = (vice_machine_t *)calloc(1, sizeof(*machine));
    if (machine == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    machine->magic = VICE_MACHINE_MAGIC;
    machine->c64_model = model;
    if (g_debug_create_calls < 8) {
        fprintf(stderr, "vice_machine_create returns=%p\\n", (void *)machine);
        fflush(stderr);
    }
    g_active_machine = machine;
    LeaveCriticalSection(&g_state_lock);

    return machine;
}

static int vice_shim_model_from_selector(const char *selector)
{
    if (selector == NULL || selector[0] == '\0' || vice_shim_selector_equals(selector, "c64")) {
        return C64MODEL_C64_PAL;
    }

    if (vice_shim_selector_equals(selector, "c64c")
        || vice_shim_selector_equals(selector, "newpal")
        || vice_shim_selector_equals(selector, "c64new")) {
        return C64MODEL_C64C_PAL;
    }

    if (vice_shim_selector_equals(selector, "c64old") || vice_shim_selector_equals(selector, "oldpal")) {
        return C64MODEL_C64_OLD_PAL;
    }

    if (vice_shim_selector_equals(selector, "ntsc") || vice_shim_selector_equals(selector, "c64ntsc")) {
        return C64MODEL_C64_NTSC;
    }

    if (vice_shim_selector_equals(selector, "newntsc")
        || vice_shim_selector_equals(selector, "c64cntsc")
        || vice_shim_selector_equals(selector, "c64newntsc")) {
        return C64MODEL_C64C_NTSC;
    }

    if (vice_shim_selector_equals(selector, "oldntsc") || vice_shim_selector_equals(selector, "c64oldntsc")) {
        return C64MODEL_C64_OLD_NTSC;
    }

    if (vice_shim_selector_equals(selector, "paln") || vice_shim_selector_equals(selector, "drean")) {
        return C64MODEL_C64_PAL_N;
    }

    if (vice_shim_selector_equals(selector, "sx64pal") || vice_shim_selector_equals(selector, "sx64")) {
        return C64MODEL_C64SX_PAL;
    }

    if (vice_shim_selector_equals(selector, "sx64ntsc")) {
        return C64MODEL_C64SX_NTSC;
    }

    if (vice_shim_selector_equals(selector, "pet64pal") || vice_shim_selector_equals(selector, "pet64")) {
        return C64MODEL_PET64_PAL;
    }

    if (vice_shim_selector_equals(selector, "pet64ntsc")) {
        return C64MODEL_PET64_NTSC;
    }

    if (vice_shim_selector_equals(selector, "ultimax") || vice_shim_selector_equals(selector, "max")) {
        return C64MODEL_ULTIMAX;
    }

    if (vice_shim_selector_equals(selector, "c64gs") || vice_shim_selector_equals(selector, "gs")) {
        return C64MODEL_C64_GS;
    }

    if (vice_shim_selector_equals(selector, "c64jap") || vice_shim_selector_equals(selector, "jap")) {
        return C64MODEL_C64_JAP;
    }

    return C64MODEL_UNKNOWN;
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
    if (g_debug_reset_calls < 8) {
        fprintf(stderr, "vice_machine_destroy machine=%p\\n", machine);
        fflush(stderr);
    }

    vice_machine_t *instance = (vice_machine_t *)machine;

    if (instance == NULL) {
        return;
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_active_machine == instance) {
        vice_shim_detach_cartridge_locked();
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
        fprintf(stderr, "vice_machine_reset call=%u machine=%p\\n", ++g_debug_reset_calls, machine);
        fflush(stderr);
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    vice_machine_t *instance = (vice_machine_t *)machine;
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    c64model_set(instance->c64_model);
    machine_powerup();
    mem_powerup();
    if (instance->cartridge_attached) {
        vice_shim_apply_cartridge_locked(instance);
    } else {
        vice_shim_detach_cartridge_locked();
    }
    vicii_reset_registers();
    maincpu_reset();
    vice_shim_reset_cpu_state_locked();
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_attach_cartridge(void *machine, const uint8_t *image, int length, int mapping_mode)
{
    int result;

    if (image == NULL || length <= 0 || length > VICE_SHIM_CART_IMAGE_LIMIT) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    vice_machine_t *instance = (vice_machine_t *)machine;
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    if (vice_shim_cartridge_type_for(instance, length, mapping_mode) == CARTRIDGE_NONE) {
        LeaveCriticalSection(&g_state_lock);
        return -3;
    }

    memcpy(instance->cartridge_image, image, (size_t)length);
    instance->cartridge_length = length;
    instance->cartridge_mapping_mode = mapping_mode;
    instance->cartridge_attached = 1;

    result = vice_shim_apply_cartridge_locked(instance);
    LeaveCriticalSection(&g_state_lock);
    return result;
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

static int vice_shim_apply_cartridge_locked(const vice_machine_t *instance)
{
    char temp_path[MAX_PATH];
    int cartridge_type;
    int result;

    if (instance == NULL || !instance->cartridge_attached) {
        return 0;
    }

    cartridge_type = vice_shim_cartridge_type_for(
        instance,
        instance->cartridge_length,
        instance->cartridge_mapping_mode);
    if (cartridge_type == CARTRIDGE_NONE) {
        return -1;
    }

    if (!vice_shim_write_temp_file(
            instance->cartridge_image,
            instance->cartridge_length,
            temp_path,
            sizeof(temp_path))) {
        return -2;
    }

    result = cartridge_attach_image(cartridge_type, temp_path);
    DeleteFileA(temp_path);
    return result == 0 ? 0 : -3;
}

static int vice_shim_cartridge_type_for(const vice_machine_t *instance, int length, int mapping_mode)
{
    int resolved_mapping_mode = mapping_mode;

    if (length != VICE_SHIM_CART_BANK_SIZE
            && length != VICE_SHIM_CART_GENERIC_IMAGE_LIMIT
            && length != VICE_SHIM_CART_GAME_SYSTEM_IMAGE_SIZE) {
        return CARTRIDGE_NONE;
    }

    if (resolved_mapping_mode == VICE_SHIM_CART_MAPPING_AUTO) {
        if (instance != NULL
                && instance->c64_model == C64MODEL_C64_GS
                && length == VICE_SHIM_CART_GAME_SYSTEM_IMAGE_SIZE) {
            resolved_mapping_mode = VICE_SHIM_CART_MAPPING_GAME_SYSTEM;
        } else if (instance != NULL && instance->c64_model == C64MODEL_ULTIMAX) {
            resolved_mapping_mode = VICE_SHIM_CART_MAPPING_ULTIMAX;
        } else {
            resolved_mapping_mode = length == VICE_SHIM_CART_BANK_SIZE
                ? VICE_SHIM_CART_MAPPING_STANDARD_8K
                : VICE_SHIM_CART_MAPPING_STANDARD_16K;
        }
    }

    switch (resolved_mapping_mode) {
        case VICE_SHIM_CART_MAPPING_STANDARD_8K:
            return length == VICE_SHIM_CART_BANK_SIZE ? CARTRIDGE_GENERIC_8KB : CARTRIDGE_NONE;
        case VICE_SHIM_CART_MAPPING_STANDARD_16K:
            return length == VICE_SHIM_CART_GENERIC_IMAGE_LIMIT ? CARTRIDGE_GENERIC_16KB : CARTRIDGE_NONE;
        case VICE_SHIM_CART_MAPPING_ULTIMAX:
            return CARTRIDGE_ULTIMAX;
        case VICE_SHIM_CART_MAPPING_GAME_SYSTEM:
            return length == VICE_SHIM_CART_GAME_SYSTEM_IMAGE_SIZE ? CARTRIDGE_GS : CARTRIDGE_NONE;
        default:
            return CARTRIDGE_NONE;
    }
}

static void vice_shim_detach_cartridge_locked(void)
{
    cartridge_detach_image(CARTRIDGE_ULTIMAX);
    cartridge_detach_image(CARTRIDGE_GENERIC_8KB);
    cartridge_detach_image(CARTRIDGE_GENERIC_16KB);
    cartridge_detach_image(CARTRIDGE_GS);
}

static int vice_shim_write_temp_file(const uint8_t *data, int length, char *path, size_t path_size)
{
    char temp_directory[MAX_PATH];
    DWORD temp_directory_length;
    HANDLE file;
    DWORD written;
    BOOL write_ok;

    if (data == NULL || path == NULL || path_size < MAX_PATH || length <= 0) {
        return 0;
    }

    temp_directory_length = GetTempPathA((DWORD)sizeof(temp_directory), temp_directory);
    if (temp_directory_length == 0 || temp_directory_length >= sizeof(temp_directory)) {
        return 0;
    }

    if (GetTempFileNameA(temp_directory, "vsc", 0, path) == 0) {
        return 0;
    }

    file = CreateFileA(path, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (file == INVALID_HANDLE_VALUE) {
        DeleteFileA(path);
        return 0;
    }

    write_ok = WriteFile(file, data, (DWORD)length, &written, NULL);
    CloseHandle(file);

    if (!write_ok || written != (DWORD)length) {
        DeleteFileA(path);
        return 0;
    }

    return 1;
}

VICE_SHIM_API void vice_machine_step_cycle(void *machine)
{
    if (g_debug_step_calls < 16) {
        fprintf(stderr, "vice_machine_step_cycle call=%u machine=%p\\n", ++g_debug_step_calls, machine);
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
        fprintf(stderr, "cycle_checkpoint: granted=%u running=%d paused=%d stop=%d\\n",
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

VICE_SHIM_API void vice_interrupt_get_state(void *machine, struct vice_interrupt_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && maincpu_int_status != NULL) {
        state->global_pending = (uint8_t)(maincpu_int_status->global_pending_int & 0xff);
        state->irq_source_count = (uint8_t)(maincpu_int_status->nirq & 0xff);
        state->nmi_source_count = (uint8_t)(maincpu_int_status->nnmi & 0xff);
        state->irq_asserted = (uint8_t)(maincpu_int_status->nirq > 0);
        state->nmi_asserted = (uint8_t)(maincpu_int_status->nnmi > 0);
    }
    LeaveCriticalSection(&g_state_lock);
}
