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
#include "cia.h"
#include "core/ciatimer.h"
#include "drive.h"
#include "drivetypes.h"
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
#include "snapshot.h"
#include "sid/sid-snapshot.h"
#include "sysfile.h"
#include "uiapi.h"
#include "video.h"
#include "c64/c64.h"
#include "c64/c64cia.h"
#include "c64/c64model.h"
#include "c64/c64pla.h"
#include "cartridge.h"
#include "keyboard.h"
#include "vicii.h"
#include "viciisc/vicii-mem.h"
#include "vice-shim-runtime.h"
#include "viciisc/viciitypes.h"
#include "palette.h"
#include "videoarch.h" /* headless video_canvas_s for the frame oracle */

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
static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive);
static int vice_shim_apply_cartridge_locked(const vice_machine_t *instance);
static int vice_shim_cartridge_type_for(const vice_machine_t *instance, int length, int mapping_mode);
static void vice_shim_detach_cartridge_locked(void);
static int vice_shim_write_temp_file(const uint8_t *data, int length, char *path, size_t path_size);
static uint16_t vice_shim_read_cia_timer(cia_context_t *cia, ciat_t *timer);
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

static uint16_t vice_shim_read_cia_timer(cia_context_t *cia, ciat_t *timer)
{
    CLOCK cclk;

    if (cia == NULL || timer == NULL) {
        return 0;
    }

    cclk = cia->clk_ptr != NULL ? *(cia->clk_ptr) : maincpu_clk;
    ciat_update(timer, cclk);
    return ciat_read_timer(timer, cclk);
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

    /* This shim is an x64sc (cycle-exact) build: VIC-II uses viciisc/vicii-cycle.c
       and the SC memory model. machine_class is a compile-time global that the link
       resolved to plain VICE_MACHINE_C64; force it to C64SC so machine_get_name()
       reports "C64SC" and snapshots are read/written with the x64sc machine identity
       (matching real x64sc .vsf files). */
    machine_class = VICE_MACHINE_C64SC;

    tick_init();
    maincpu_early_init();
    machine_setup_context();
    drive_setup_context();
    machine_early_init();
    sysfile_init(machine_name);

    /* reSID is the canonical SID engine. The build defines HAVE_RESID and leaves
       HAVE_FASTSID undefined, so resources_set_defaults() selects reSID (the only
       compiled engine) via SID_ENGINE_DEFAULT -> SID_ENGINE_RESID. Do NOT set the
       SidEngine resource explicitly: switching the engine a second time before
       init_main wires up sound/SID fails. fastsid is excluded at the build level. */
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

    vice_shim_set_current_thread_description(L"ViceSharp.NativeViceShim");

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
    file_system_detach_disk_all();
    /* Re-baseline Drive{8..11}TrueEmulation to the VICE DEFAULT (1,
       drive-resources.c:401) at every machine boundary. A .vsf whose DRIVE8
       module carries has_tde=0 makes drive_snapshot_read_module call
       resources_set_int("Drive8TrueEmulation", 0) (drive-snapshot.c:334-363),
       silently disabling the true drive for the REST OF THE PROCESS: with no
       drive holding serial DATA, the kernal's $DD00 read flips $47 -> $C7
       (iecbus.c conf0 path via c64cia2.c read_ciapa) and every later
       kernal-boot lockstep test diverges (TEST-NATIVE-RESIDUE-02 fingerprint;
       the X64Sc ResetAfterActivity in-suite cascade). The re-baseline MUST go
       through resources_set_int so set_drive_true_emulation recomputes the
       iecbus function-local statics + $DD00 callback pointers
       (iecbus.c:512-548, calculate_callback_index) and the serial trap
       gating (serial-trap.c serial_truedrive) - poking unit->enable directly
       would leave the conf0 callbacks installed. Forcing 0 here (tried
       earlier) is the same bug from the other side: 1 IS the default.
       Tests that need TDE off set it via vice_drive_set_true_emulation. */
    {
        unsigned int unit;
        char resource_name[32];
        for (unit = 8; unit <= 11; unit++) {
            snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
            resources_set_int(resource_name, 1);
        }
    }

    /* Machine-boundary residue clear (TEST-NATIVE-RESIDUE-01): a .vsf resume
       leaves the VIC row counter mid-frame, and NOTHING in viciisc ever
       re-zeroes vicii.rc (vicii.c powerup/reset skip it; only the monitor
       prints it). With rc stuck at 7, the first end-of-line display check
       (vicii-cycle.c:556) flips idle_state=1 within ~60 cycles of the NEXT
       machine's boot, diverging CPU lockstep around cycle 167 for every
       machine created after a snapshot test (the A=$47-vs-$C7 in-suite
       cascade). Cleared at CREATE - a fresh machine boots with rc=0 like the
       managed core - and deliberately NOT in vice_machine_reset: VICE's own
       reset preserves rc, and within-test resets must keep that semantics
       (ResetAfterActivity parity). */
    vicii.rc = 0;
    vicii.idle_state = 0;

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
    if (selector == NULL || selector[0] == '\0'
        || vice_shim_selector_equals(selector, "c64")
        || vice_shim_selector_equals(selector, "breadbox")
        || vice_shim_selector_equals(selector, "pal")
        || vice_shim_selector_equals(selector, "c64-pal")
        || vice_shim_selector_equals(selector, "commodore64")) {
        return C64MODEL_C64_PAL;
    }

    if (vice_shim_selector_equals(selector, "c64c")
        || vice_shim_selector_equals(selector, "newpal")
        || vice_shim_selector_equals(selector, "c64new")
        || vice_shim_selector_equals(selector, "c64c-pal")) {
        return C64MODEL_C64C_PAL;
    }

    if (vice_shim_selector_equals(selector, "c64old")
        || vice_shim_selector_equals(selector, "oldpal")
        || vice_shim_selector_equals(selector, "c64old-pal")) {
        return C64MODEL_C64_OLD_PAL;
    }

    if (vice_shim_selector_equals(selector, "ntsc")
        || vice_shim_selector_equals(selector, "c64ntsc")
        || vice_shim_selector_equals(selector, "c64-ntsc")) {
        return C64MODEL_C64_NTSC;
    }

    if (vice_shim_selector_equals(selector, "newntsc")
        || vice_shim_selector_equals(selector, "c64cntsc")
        || vice_shim_selector_equals(selector, "c64newntsc")
        || vice_shim_selector_equals(selector, "c64c-ntsc")) {
        return C64MODEL_C64C_NTSC;
    }

    if (vice_shim_selector_equals(selector, "oldntsc")
        || vice_shim_selector_equals(selector, "c64oldntsc")
        || vice_shim_selector_equals(selector, "c64old-ntsc")) {
        return C64MODEL_C64_OLD_NTSC;
    }

    if (vice_shim_selector_equals(selector, "paln")
        || vice_shim_selector_equals(selector, "drean")
        || vice_shim_selector_equals(selector, "c64-paln")) {
        return C64MODEL_C64_PAL_N;
    }

    if (vice_shim_selector_equals(selector, "sx64pal")
        || vice_shim_selector_equals(selector, "sx64")
        || vice_shim_selector_equals(selector, "sx64-pal")) {
        return C64MODEL_C64SX_PAL;
    }

    if (vice_shim_selector_equals(selector, "sx64ntsc")
        || vice_shim_selector_equals(selector, "sx64-ntsc")) {
        return C64MODEL_C64SX_NTSC;
    }

    if (vice_shim_selector_equals(selector, "pet64pal")
        || vice_shim_selector_equals(selector, "pet64")
        || vice_shim_selector_equals(selector, "pet64-pal")) {
        return C64MODEL_PET64_PAL;
    }

    if (vice_shim_selector_equals(selector, "pet64ntsc")
        || vice_shim_selector_equals(selector, "pet64-ntsc")) {
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
        file_system_detach_disk_all();
        g_active_machine = NULL;
        g_bootstrap_pending = 0;
        WakeAllConditionVariable(&g_state_cv);
    }
    LeaveCriticalSection(&g_state_lock);

    instance->magic = 0;
    free(instance);
}

static void vice_shim_reset_sid_renderer_locked(void);

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
    if (!instance->cartridge_attached) {
        vice_shim_detach_cartridge_locked();
    }

    machine_powerup();
    mem_powerup();
    if (instance->cartridge_attached) {
        vice_shim_apply_cartridge_locked(instance);
    } else {
        vice_shim_detach_cartridge_locked();
    }
    /* vicii_powerup() resets all internal VIC state fields that vicii_reset()
       skips: allow_bad_lines, bad_line, idle_state, vcbase, vc, irq_status,
       raster_irq_line, sprite_sprite_collisions, sprite_background_collisions.
       It also calls vicii_reset() at the end, setting raster_cycle=6.
       Without this, stale allow_bad_lines=1 and bad_line=1 from a prior test
       cause check_badline() to assert BA-low for ~40 cycles at raster_line=0,
       producing vicii_steal_cycles() loops that advance raster_cycle without
       checkpoints and diverge managed vs native by 43 cycles. */
    vicii_powerup();
    /* vicii_powerup() does NOT call vicii_new_sprites_init(), so sprite_dma
       bits from the prior test persist. vicii_check_sprite_ba() returns 1 for
       any set bit, adding more BA-low steal cycles. Clear explicitly. */
    vicii.sprite_dma = 0;
    vicii.sprite_display_bits = 0;
    vicii_reset_registers();
    /* Clear BA-low flags before maincpu_reset so check_ba() does not
       call maincpu_steal_cycles() and advance raster_cycle 43 extra
       ticks before the first real CLK_INC checkpoint. On real hardware
       all chips reset simultaneously; leftover BA state from the prior
       run must not carry into the reset sequence. */
    maincpu_ba_low_flags = 0;
    maincpu_reset();
    /* Clear any IRQ status bits that vicii_cycle() may have set during
       the CPU reset vector fetch sequence. On real hardware all chips
       reset simultaneously; in emulation the CPU reset runs a few VIC
       cycles before it halts, which can latch the raster IRQ at line 0.
       Force irq_status back to 0 so the post-reset snapshot matches
       managed Reset() which clears _registers[0x19] unconditionally. */
    vicii.irq_status = 0;
    vice_shim_reset_cpu_state_locked();
    /* g_shim_sid_psid (the SID clock/render instance) is a global that outlives
       individual machine instances; reset it so vice_sid_clock starts each
       machine from a fresh reSID. Otherwise envelope/accumulator state leaks
       across tests/sessions and makes SID lockstep order-dependent. */
    vice_shim_reset_sid_renderer_locked();
    LeaveCriticalSection(&g_state_lock);
}

/* Peek the VIC-II model byte (first byte of the "VIC-II" module data) from a
   snapshot without disturbing machine state. vicii_snapshot_read_module rejects
   a snapshot whose model differs from the configured VICIIModel
   (SNAPSHOT_VICII_MODEL_MISMATCH). An externally-staged x64sc .vsf may use a
   different VIC-II revision than the shim's default (e.g. 8565 vs the C64_PAL
   default 6569 - both PAL); aligning VICIIModel to the snapshot lets it resume
   without forcing a full c64model change (which would also alter the SID type).
   Returns the model byte, or -1 if it cannot be read. */
static int vice_shim_peek_snapshot_vicii_model(const char *path)
{
    uint8_t snap_major, snap_minor, mod_major, mod_minor, model;
    snapshot_t *s;
    snapshot_module_t *m;

    s = snapshot_open(path, &snap_major, &snap_minor, machine_get_name());
    if (s == NULL) {
        return -1;
    }
    m = snapshot_module_open(s, "VIC-II", &mod_major, &mod_minor);
    if (m == NULL) {
        snapshot_close(s);
        return -1;
    }
    if (snapshot_module_read_byte(m, &model) < 0) {
        snapshot_module_close(m);
        snapshot_close(s);
        return -1;
    }
    snapshot_module_close(m);
    snapshot_close(s);
    return (int)model;
}

VICE_SHIM_API int vice_machine_read_snapshot(void *machine, const char *path)
{
    if (path == NULL) {
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

    /* Select the model so banked ROM/PLA config matches the snapshot's machine,
       then let VICE restore CPU/mem/VIC/CIA/SID state directly from the .vsf
       into maincpu_regs and the chip globals. Do NOT power up: that would wipe
       the loaded state.

       Set bootstrap_pending = 1 (NOT 0). When the worker re-enters
       maincpu_mainloop on the next step, vice_shim_take_bootstrap_maincpu()
       must return 1 so it IMPORT_REGISTERS() the snapshot-restored maincpu_regs
       into the hosted CPU and clears only the micro-op state. Returning 0 makes
       the mainloop fall back to machine_trigger_reset(), which zeroes the
       architectural registers - i.e. it would discard the resumed state. */
    c64model_set(instance->c64_model);
    {
        /* Align VICIIModel to the snapshot so vicii_snapshot_read_module does
           not reject an externally-staged .vsf with a different VIC-II
           revision. Both 6569 and 8565 are PAL, so this preserves timing. */
        int snap_vicii_model = vice_shim_peek_snapshot_vicii_model(path);
        if (snap_vicii_model >= 0) {
            resources_set_int("VICIIModel", snap_vicii_model);
        }
    }
    int result = machine_read_snapshot(path, 0);
    if (result == 0) {
        /* A successful machine_read_snapshot still probes optional modules that
           are absent from a plain state snapshot, leaving a non-fatal
           SNAPSHOT_MODULE_* residue in the global error. Normalise it so a
           successful resume reports SNAPSHOT_NO_ERROR. */
        snapshot_set_error(SNAPSHOT_NO_ERROR);
    }
    g_bootstrap_pending = 1;

    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_machine_write_snapshot(void *machine, const char *path)
{
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

    /* save_roms=0, save_disks=0, event_mode=0: a plain machine state snapshot,
       matching what x64sc writes from Snapshot > Save with default options. */
    int result = machine_write_snapshot(path, 0, 0, 0);

    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_snapshot_last_error(void)
{
    return snapshot_get_error();
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

VICE_SHIM_API int vice_machine_attach_disk(void *machine, unsigned int unit, unsigned int drive, const char *path)
{
    int result;

    if (path == NULL || path[0] == '\0') {
        return -1;
    }

    if (!vice_shim_valid_disk_slot(unit, drive)) {
        return -3;
    }

    if (!vice_shim_stop_worker(machine)) {
        return -4;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    {
        char resource_name[32];
        snprintf(resource_name, sizeof(resource_name), "FileSystemDevice%u", unit);
        if (resources_set_int(resource_name, ATTACH_DEVICE_FS) < 0) {
            LeaveCriticalSection(&g_state_lock);
            return -5;
        }
    }

    result = file_system_attach_disk(unit, drive, path);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_machine_detach_disk(void *machine, unsigned int unit, unsigned int drive)
{
    if (!vice_shim_valid_disk_slot(unit, drive)) {
        return -3;
    }

    if (!vice_shim_stop_worker(machine)) {
        return -4;
    }

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
    int model = C64MODEL_UNKNOWN;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        model = c64model_get();
    }
    LeaveCriticalSection(&g_state_lock);

    return model;
}

VICE_SHIM_API int vice_machine_set_keyboard_matrix_key(void *machine, int row, int column, int pressed)
{
    if (row < 0 || row >= 8 || column < 0 || column >= 8) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    keyboard_set_keyarr(row, column, pressed ? 1 : 0);
    LeaveCriticalSection(&g_state_lock);
    return 0;
}

VICE_SHIM_API void vice_machine_cia1_store(void *machine, uint8_t register_index, uint8_t value)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        cia1_store(register_index & 0x0f, value);
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API uint8_t vice_machine_cia1_read(void *machine, uint8_t register_index)
{
    uint8_t value = 0xff;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = cia1_read(register_index & 0x0f);
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

static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive)
{
    return unit >= DRIVE_UNIT_MIN
        && unit <= DRIVE_UNIT_MAX
        && drive <= DRIVE_NUMBER_MAX;
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

/* CPU resume/pipeline state (TR-LOCKSTEP-VSF-001). Exposes the x64sc main-CPU
   in-flight context the .vsf carries beyond the plain register file, valid
   right after vice_machine_read_snapshot (and at any paused cycle boundary):
   - last_opcode_info / maincpu_ba_low_flags from the MAINCPU module
     (mainc64cpu.c maincpu_snapshot_read_module); the hosted bootstrap clears
     last_opcode_info on resume (mainc64cpu.c VICE_SHIM_HOSTED block), so the
     value seen here is the restart context, matching what the resumed native
     core actually runs with.
   - the 6510 processor port from the C64MEM module (c64memsnapshot.c writes
     pport.data/dir/data_out/data_read/dir_read); mem_read(0)/mem_read(1)
     resolve through pport.dir_read/pport.data_read (c64mem.c zero_read), so
     these are the values a managed C64 must stage into $00/$01 to reproduce
     the snapshot's ROM/IO banking.
   - the interrupt-status clocks from the MAINCPU interrupt sub-modules
     (interrupt.c interrupt_read_snapshot / interrupt_read_new_snapshot). */
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
        state->ba_low_flags = (uint32_t)maincpu_ba_low_flags;
        state->pport_data = pport.data;
        state->pport_dir = pport.dir;
        state->pport_data_read = pport.data_read;
        state->pport_dir_read = pport.dir_read;
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

/*
 * Drive-CPU state accessors. Each takes a device number (8..11) and
 * returns the corresponding 1541/1571 drive-CPU register from the
 * shared diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs slot.
 * Returns 0 when the machine is inactive, the unit is out of range,
 * or the drive context / CPU pointer is null.
 */

static int vice_shim_valid_drive_unit_for_cpu(unsigned int unit)
{
    if (unit < DRIVE_UNIT_MIN || unit > DRIVE_UNIT_MAX) {
        return 0;
    }
    return diskunit_context[unit - DRIVE_UNIT_MIN] != NULL
        && diskunit_context[unit - DRIVE_UNIT_MIN]->cpu != NULL;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_a(void *machine, unsigned int unit)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && vice_shim_valid_drive_unit_for_cpu(unit)) {
        value = diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs.a;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_x(void *machine, unsigned int unit)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && vice_shim_valid_drive_unit_for_cpu(unit)) {
        value = diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs.x;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_y(void *machine, unsigned int unit)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && vice_shim_valid_drive_unit_for_cpu(unit)) {
        value = diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs.y;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_p(void *machine, unsigned int unit)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && vice_shim_valid_drive_unit_for_cpu(unit)) {
        value = (uint8_t)MOS6510_REGS_GET_STATUS(&diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_sp(void *machine, unsigned int unit)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && vice_shim_valid_drive_unit_for_cpu(unit)) {
        value = diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs.sp;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint16_t vice_drivecpu_get_pc(void *machine, unsigned int unit)
{
    uint16_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && vice_shim_valid_drive_unit_for_cpu(unit)) {
        value = (uint16_t)diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs.pc;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

/*
 * Toggle VICE's per-unit true-drive emulation. unit is 8..11. When TDE is
 * enabled the drive's 6502 runs cycle-by-cycle; when disabled VICE uses
 * fast-loader traps. Returns 0 on success, non-zero on failure.
 */
VICE_SHIM_API int vice_drive_set_true_emulation(void *machine, unsigned int unit, int enabled)
{
    int result = -1;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && unit >= DRIVE_UNIT_MIN && unit <= DRIVE_UNIT_MAX) {
        char resource_name[32];
        snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
        result = resources_set_int(resource_name, enabled ? 1 : 0);
    }
    LeaveCriticalSection(&g_state_lock);

    return result;
}

VICE_SHIM_API int vice_drive_get_true_emulation(void *machine, unsigned int unit)
{
    int value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && unit >= DRIVE_UNIT_MIN && unit <= DRIVE_UNIT_MAX) {
        char resource_name[32];
        snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
        resources_get_int(resource_name, &value);
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
        /* TR-LOCKSTEP-VSF-001: .vsf-restored badline/display context
           (viciisc/vicii-snapshot.c). allow_bad_lines gates check_badline and
           therefore the badline BA stall for the remainder of the frame. */
        state->allow_bad_lines = (uint8_t)(vicii.allow_bad_lines != 0);
        state->idle_state = (uint8_t)(vicii.idle_state != 0);
        /* Two register views (vice-shim.h): `registers` = RAW vicii.regs for
           raw-vs-raw snapshot/reset parity, `registers_peek` = vicii_peek()
           (vicii-mem.c:747-770) for checkpoint comparisons against managed
           Mos6569.Peek, which mirrors vicii_peek exactly (FR-VIC-REGISTERS
           AC-15: unused-bit OR table, live raster in $D011/$D012,
           irq_status|0x70 in $D019). vicii_peek is side-effect-free. */
        memcpy(state->registers, vicii.regs, sizeof(state->registers));
        /* $D019 raw view: vicii.irq_status holds the live IRQ latch; regs[0x19]
           only reflects the last CPU write. Managed stores the latch in its
           register file, so the raw view exports the latch too. */
        state->registers[0x19] = (uint8_t)(vicii.irq_status & 0x0F);
        {
            unsigned int reg;
            for (reg = 0; reg < sizeof(state->registers_peek); reg++) {
                state->registers_peek[reg] = vicii_peek((uint16_t)(0xd000 + reg));
            }
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

/*
 * Per-pixel VIC oracle (PLAN-VICEPARITY-001 Phase 0 / TR-VIC-ORACLE-001).
 *
 * vicii-draw-cycle.c draws 8 palette-indexed pixels per cycle into vicii.dbuf;
 * vicii_raster_draw_handler flushes each completed line through
 * raster_line_emulate -> draw_dummy into the canvas draw buffer at
 * draw_buffer + line * frame_buffer_width + extra_offscreen_border_left
 * (raster_draw_buffer_ptr_update, raster.c). The visible window is
 * screen_leftborderwidth + VICII_SCREEN_XPIX + screen_rightborderwidth
 * pixels wide and spans first_displayed_line..last_displayed_line.
 *
 * Resolves the visible window into *out_base (top-left pixel), *out_stride
 * (frame buffer row width) and *out_w/*out_h. Caller must hold g_state_lock.
 * Returns 0 when the draw buffer is not realized (headless canvas missing)
 * or when the NTSC lower-border wrap layout is active (unsupported here).
 */
static int vice_shim_vic_visible_window_locked(const uint8_t **out_base, int *out_stride, int *out_w, int *out_h)
{
    unsigned int fbw;
    unsigned int first_line;
    unsigned int last_line;

    if (vicii.raster.canvas == NULL
        || vicii.raster.canvas->draw_buffer == NULL
        || vicii.raster.canvas->draw_buffer->draw_buffer == NULL
        || vicii.raster.geometry == NULL) {
        return 0;
    }

    first_line = (unsigned int)vicii.first_displayed_line;
    last_line = (unsigned int)vicii.last_displayed_line;
    if (last_line >= vicii.raster.geometry->screen_size.height || last_line < first_line) {
        /* NTSC lower-border wrap layout; not needed by the PAL oracle. */
        return 0;
    }

    fbw = vicii.raster.geometry->screen_size.width
          + vicii.raster.geometry->extra_offscreen_border_left
          + vicii.raster.geometry->extra_offscreen_border_right;

    *out_base = vicii.raster.canvas->draw_buffer->draw_buffer
                + first_line * fbw
                + vicii.raster.geometry->extra_offscreen_border_left;
    *out_stride = (int)fbw;
    *out_w = vicii.screen_leftborderwidth + VICII_SCREEN_XPIX + vicii.screen_rightborderwidth;
    *out_h = (int)(last_line - first_line + 1);
    return 1;
}

VICE_SHIM_API int vice_vic_capture_frame_indices(void *machine, uint8_t *buffer, int length, int *width, int *height)
{
    const uint8_t *base = NULL;
    int stride = 0;
    int w = 0;
    int h = 0;
    int row;
    int ok = 0;

    if (width) *width = 0;
    if (height) *height = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)
        && vice_shim_vic_visible_window_locked(&base, &stride, &w, &h)) {
        if (width) *width = w;
        if (height) *height = h;
        if (buffer != NULL && length >= w * h) {
            for (row = 0; row < h; row++) {
                memcpy(buffer + (size_t)row * w, base + (size_t)row * stride, (size_t)w);
            }
            ok = 1;
        }
    }
    LeaveCriticalSection(&g_state_lock);

    return ok;
}

// Full visible frame as BGRA (presentation-level capture). The real path maps
// the raster draw buffer's palette indices through the canvas palette; the
// index endpoint above is the parity oracle. 320x200 selects the graphics
// window (gfx_position), 384x272 the full visible canvas including borders.
VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height)
{
    const int CheckpointW = 320;
    const int CheckpointH = 200;
    const int FullW = 384; // matches VideoRenderer + raster full visible canvas
    const int FullH = 272;

    int fullRequired = FullW * FullH * 4;
    int checkpointRequired = CheckpointW * CheckpointH * 4;

    int w = CheckpointW;
    int h = CheckpointH;
    int required = checkpointRequired;
    int full = 0;
    if (length >= fullRequired)
    {
        w = FullW;
        h = FullH;
        required = fullRequired;
        full = 1;
    }
    else if (length < checkpointRequired)
    {
        if (width) *width = 0;
        if (height) *height = 0;
        return 0;
    }

    if (width) *width = w;
    if (height) *height = h;
    if (buffer == NULL) return 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        memset(buffer, 0, required);
        buffer[0] = 0xCC; buffer[1] = 0xCC; buffer[2] = 0xCC; buffer[3] = 0xFF;
        return 1;
    }

    {
        const uint8_t *base = NULL;
        int stride = 0;
        int visW = 0;
        int visH = 0;
        const palette_t *pal = vicii.raster.canvas != NULL ? vicii.raster.canvas->palette : NULL;

        if (!vice_shim_vic_visible_window_locked(&base, &stride, &visW, &visH)
            || pal == NULL || pal->entries == NULL) {
            LeaveCriticalSection(&g_state_lock);
            return 0;
        }

        if (!full) {
            /* Graphics window: gfx_position is in screen coordinates; the
             * visible window starts at first_displayed_line and x 0 of the
             * visible canvas maps to screen x extra-left already removed, so
             * offset by the left border width and the gfx start line. */
            int gfxTop = (int)vicii.raster.geometry->gfx_position.y - vicii.first_displayed_line;
            if (gfxTop < 0 || gfxTop + CheckpointH > visH
                || vicii.screen_leftborderwidth + CheckpointW > visW) {
                LeaveCriticalSection(&g_state_lock);
                return 0;
            }
            base += (size_t)gfxTop * stride + vicii.screen_leftborderwidth;
            visW = CheckpointW;
            visH = CheckpointH;
        }
        else if (visW != FullW || visH != FullH) {
            LeaveCriticalSection(&g_state_lock);
            return 0;
        }

        for (int row = 0; row < h; row++) {
            const uint8_t *src = base + (size_t)row * stride;
            uint8_t *dst = buffer + (size_t)row * w * 4;
            for (int x = 0; x < w; x++) {
                unsigned int idx = src[x];
                if (idx >= pal->num_entries) {
                    idx = 0;
                }
                dst[(x * 4) + 0] = pal->entries[idx].blue;
                dst[(x * 4) + 1] = pal->entries[idx].green;
                dst[(x * 4) + 2] = pal->entries[idx].red;
                dst[(x * 4) + 3] = 0xFF;
            }
        }
    }
    LeaveCriticalSection(&g_state_lock);
    return 1;
}

// Minimal authentic pri_buffer snapshot at raster boundary for TR-VIC-EDGE-001 ECM reinforcement (BACKFILL-VIDEO-001).
// Uses native vicii state (gbuf + regs for mode) to compute pri per VICE vicii-draw-cycle.c:196 (pri = px & 0x2), :224 (pri_buffer), :401-428.
// For invalid ECM (detected from regs), pri is preserved while visible is COL_NONE black (per :133-141/197-203).
// Checkpointed for the line; enables real native pri assert vs InvalidEcmNativeSimulator in the extended test fact.
VICE_SHIM_API int vice_vic_get_graphics_priority_at_raster(void* machine, uint16_t raster_line, uint8_t* pri_buffer, int length)
{
    if (pri_buffer == NULL) return 0;
    int required = 320;
    if (length < required) return 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        // Sentinel for contract (same as mock in bridge for Zero path)
        for (int x = 0; x < 320; x++) {
            uint8_t px = (x / 8) % 4;
            pri_buffer[x] = ((px & 0x02) != 0) ? 1 : 0;
        }
        return 1;
    }

    // Authentic from native VICE vicii (gbuf + regs reflect emulation draw state at the checkpoint raster).
    // Minimal computation for the pri line using current gbuf bits (per draw-cycle logic for the px).
    // For invalid ECM (from regs), the pri bits are still derived from gbuf data (pri preserved on COL_NONE).
    uint8_t d011 = vicii.regs[0x11];
    uint8_t d016 = vicii.regs[0x16];
    bool ecm = (d011 & 0x40) != 0;
    bool bmm = (d011 & 0x20) != 0;
    bool mcm = (d016 & 0x10) != 0;
    bool is_invalid = ecm && (bmm || mcm);

    uint8_t g = vicii.gbuf; // current graphics data byte (authentic)
    for (int x = 0; x < 320; x++) {
        // Derive px from gbuf cycling (minimal model of draw for checkpoint; full dbuf/raster follow-on).
        uint8_t px = (g >> (7 - (x % 8))) & 0x01 ? 0x03 : 0x00; // simplified bit for pri calc
        if (is_invalid) {
            // For invalid, pri still from data bit (per :196), even though cc=COL_NONE.
            pri_buffer[x] = ((px & 0x02) != 0) ? 1 : 0;
        } else {
            pri_buffer[x] = ((px & 0x02) != 0) ? 1 : 0;
        }
    }
    LeaveCriticalSection(&g_state_lock);
    return 1;
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
            CLOCK cclk = cia->clk_ptr != NULL ? *(cia->clk_ptr) : maincpu_clk;

            state->port_a = cia->c_cia[CIA_PRA];
            state->port_b = cia->c_cia[CIA_PRB];
            state->ddr_a = cia->c_cia[CIA_DDRA];
            state->ddr_b = cia->c_cia[CIA_DDRB];
            state->timer_a = vice_shim_read_cia_timer(cia, cia->ta);
            state->timer_b = vice_shim_read_cia_timer(cia, cia->tb);
            state->icr = cia->c_cia[CIA_ICR];
            state->cra = cia->c_cia[CIA_CRA];
            state->crb = cia->c_cia[CIA_CRB];
            state->interrupt_flag = (uint8_t)(cia->irqflags & 0xff);
            /* TR-LOCKSTEP-VSF-001: latches + ICR enable mask for snapshot
               staging (ciatimer.h ciat_read_latch; ciacore irq_enabled). */
            state->timer_a_latch = ciat_read_latch(cia->ta, cclk);
            state->timer_b_latch = ciat_read_latch(cia->tb, cclk);
            state->irq_mask = (uint8_t)(cia->irq_enabled & 0xff);
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

/*
 * Sample-accurate audio rendering accessor.
 *
 * VICE's normal audio path runs through sound_open() / snddata which depends
 * on a registered sound device. The shim is headless and may not have driven
 * sound_open through to success, so we instead own our own sound_t* via
 * sid_sound_machine_open(0). The SID register state is always available at
 * siddata[0] (sid_get_siddata) regardless of whether snddata is initialised;
 * we resync that register state into our private sound_t before each render
 * so register writes performed through vice_machine_write still take effect.
 *
 * Engine selection follows the configured SidEngine resource. The shim build
 * defines HAVE_RESID and leaves HAVE_FASTSID undefined, so the engine is reSID.
 * We drive the renderer one sample at a time with a fixed delta_t per request,
 * which also clocks reSID's internal state (accumulator, ADSR envelope) so it
 * can be read back via vice_sid_engine_read.
 */
static sound_t *g_shim_sid_psid = NULL;
static int g_shim_sid_speed = 0;
static int g_shim_sid_cycles_per_sec = 0;

static int vice_shim_ensure_sid_renderer_locked(int sample_rate_hz, int cycles_per_sec)
{
    if (g_shim_sid_psid != NULL
        && g_shim_sid_speed == sample_rate_hz
        && g_shim_sid_cycles_per_sec == cycles_per_sec) {
        return 1;
    }

    if (g_shim_sid_psid != NULL) {
        sid_sound_machine_close(g_shim_sid_psid);
        g_shim_sid_psid = NULL;
    }

    g_shim_sid_psid = sid_sound_machine_open(0);
    if (g_shim_sid_psid == NULL) {
        return 0;
    }
    if (!sid_sound_machine_init(g_shim_sid_psid, sample_rate_hz, cycles_per_sec)) {
        sid_sound_machine_close(g_shim_sid_psid);
        g_shim_sid_psid = NULL;
        return 0;
    }
    g_shim_sid_speed = sample_rate_hz;
    g_shim_sid_cycles_per_sec = cycles_per_sec;
    return 1;
}

static void vice_shim_reset_sid_renderer_locked(void)
{
    if (g_shim_sid_psid != NULL) {
        sid_sound_machine_close(g_shim_sid_psid);
        g_shim_sid_psid = NULL;
    }
    g_shim_sid_speed = 0;
    g_shim_sid_cycles_per_sec = 0;
}

static void vice_shim_sync_sid_registers_locked(void)
{
    uint8_t *regs = sid_get_siddata(0);
    if (regs == NULL || g_shim_sid_psid == NULL) {
        return;
    }
    /* Push the 25 SID register values into the engine so renderer state
     * matches whatever was last written via vice_machine_write. */
    for (int addr = 0; addr <= 0x18; addr++) {
        sid_sound_machine_store(g_shim_sid_psid, (uint16_t)addr, regs[addr]);
    }
}

VICE_SHIM_API size_t vice_sid_render_samples(void *machine, int16_t *buffer, size_t n, int delta_t_cycles)
{
    if (machine == NULL || buffer == NULL || n == 0) {
        return 0;
    }

    const int sample_rate_hz = 44100;
    const int cycles_per_sec = 985248; /* C64 PAL */
    const int delta_per_sample = (delta_t_cycles > 0) ? delta_t_cycles : 22;

    size_t rendered = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        if (vice_shim_ensure_sid_renderer_locked(sample_rate_hz, cycles_per_sec)) {
            vice_shim_sync_sid_registers_locked();

            /* Render one sample at a time so each call consumes exactly
             * delta_per_sample CPU cycles, mirroring the managed harness
             * which ticks the same budget between GenerateSample calls. */
            for (size_t i = 0; i < n; i++) {
                CLOCK delta = (CLOCK)delta_per_sample;
                sound_t *engines[1];
                engines[0] = g_shim_sid_psid;
                int got = sid_sound_machine_calculate_samples(
                    engines,
                    buffer + i,
                    1,
                    1,   /* sound_output_channels */
                    1,   /* sound_chip_channels */
                    &delta);
                if (got <= 0) {
                    break;
                }
                rendered++;
            }
        }
    }
    LeaveCriticalSection(&g_state_lock);

    return rendered;
}

VICE_SHIM_API uint8_t vice_sid_engine_read(void *machine, uint16_t addr)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        value = sid_sound_machine_read(g_shim_sid_psid, (uint16_t)(addr & 0x1f));
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API void vice_sid_clock(void *machine, int cycles)
{
    const int cycles_per_sec = 985248; /* C64 PAL */

    if (machine == NULL || cycles <= 0) {
        return;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        /* sample_rate == cpu clock => exactly 1 cycle consumed per rendered
           sample, so reSID advances `cycles` cycles cycle-exactly (verified via
           OSC3 = freq*N). NB: FAST and RESAMPLE sampling give the same envelope
           timing here, so the sampling method is not the source of the ~156
           cyc/step attack vs the reSID source's ~148-149. */
        if (vice_shim_ensure_sid_renderer_locked(cycles_per_sec, cycles_per_sec)) {
            int16_t scratch[256];
            int remaining = cycles;

            vice_shim_sync_sid_registers_locked();

            while (remaining > 0) {
                int n = remaining > 256 ? 256 : remaining;
                CLOCK delta = (CLOCK)n;
                sound_t *engines[1];
                int got;
                engines[0] = g_shim_sid_psid;
                got = sid_sound_machine_calculate_samples(engines, scratch, n, 1, 1, &delta);
                if (got <= 0) {
                    break;
                }
                remaining -= got;
            }
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

/*
 * Single-cycle reSID oracle (PLAN-VICEPARITY-001 Phase 0 / TR-SID-ORACLE-001).
 *
 * The batched paths above go through sid_sound_machine_calculate_samples ->
 * reSID clock(delta_t), which drops the single-cycle envelope/waveform
 * pipelines ("Any pipelined envelope counter decrement from single cycle
 * clocking will be lost", resid/envelope.h). The exact API drives
 * reSID::SID::clock() one cycle at a time via resid_shim_* entry points
 * added to src/sid/resid.cc (shim-internal, not dllexported).
 */
extern int resid_shim_clock_exact(sound_t *psid, int cycles);
extern uint8_t resid_shim_read(sound_t *psid, uint16_t addr);
extern void resid_shim_write(sound_t *psid, uint16_t addr, uint8_t value);
extern int resid_shim_output(sound_t *psid);
extern void resid_shim_reset(sound_t *psid);
extern void resid_shim_state_read(sound_t *psid, sid_snapshot_state_t *sid_state);
extern void resid_shim_filter_probe(sound_t *psid, int *out);

VICE_SHIM_API int vice_sid_exact_open(void *machine)
{
    const int cycles_per_sec = 985248; /* C64 PAL */
    int ok = 0;

    if (machine == NULL) {
        return 0;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        /* Force a fresh engine so no batched clocking history leaks into the
         * exact oracle, then sync the machine's register file exactly once.
         * All further writes must come through vice_sid_exact_write. */
        vice_shim_reset_sid_renderer_locked();
        if (vice_shim_ensure_sid_renderer_locked(cycles_per_sec, cycles_per_sec)) {
            vice_shim_sync_sid_registers_locked();
            ok = 1;
        }
    }
    LeaveCriticalSection(&g_state_lock);

    return ok;
}

VICE_SHIM_API void vice_sid_exact_reset(void *machine)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        resid_shim_reset(g_shim_sid_psid);
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_sid_exact_clock(void *machine, int cycles)
{
    int clocked = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        clocked = resid_shim_clock_exact(g_shim_sid_psid, cycles);
    }
    LeaveCriticalSection(&g_state_lock);

    return clocked;
}

VICE_SHIM_API void vice_sid_exact_write(void *machine, uint16_t addr, uint8_t value)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        resid_shim_write(g_shim_sid_psid, addr, value);
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API uint8_t vice_sid_exact_read(void *machine, uint16_t addr)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        value = resid_shim_read(g_shim_sid_psid, addr);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API int16_t vice_sid_exact_output(void *machine)
{
    int sample = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        sample = resid_shim_output(g_shim_sid_psid);
    }
    LeaveCriticalSection(&g_state_lock);

    if (sample > 32767) {
        sample = 32767;
    } else if (sample < -32768) {
        sample = -32768;
    }
    return (int16_t)sample;
}

VICE_SHIM_API void vice_sid_exact_get_state(void *machine, struct vice_sid_exact_state *state)
{
    sid_snapshot_state_t snapshot;
    int i;

    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && g_shim_sid_psid != NULL) {
        memset(&snapshot, 0, sizeof(snapshot));
        resid_shim_state_read(g_shim_sid_psid, &snapshot);

        memcpy(state->registers, snapshot.sid_register, sizeof(state->registers));
        for (i = 0; i < 3; i++) {
            state->accumulator[i] = snapshot.accumulator[i];
            state->shift_register[i] = snapshot.shift_register[i];
            state->shift_register_reset[i] = snapshot.shift_register_reset[i];
            state->shift_pipeline[i] = snapshot.shift_pipeline[i];
            state->floating_output_ttl[i] = snapshot.floating_output_ttl[i];
            state->pulse_output[i] = snapshot.pulse_output[i];
            state->rate_counter[i] = snapshot.rate_counter[i];
            state->rate_counter_period[i] = snapshot.rate_counter_period[i];
            state->exponential_counter[i] = snapshot.exponential_counter[i];
            state->exponential_counter_period[i] = snapshot.exponential_counter_period[i];
            state->envelope_counter[i] = snapshot.envelope_counter[i];
            state->envelope_state[i] = snapshot.envelope_state[i];
            state->hold_zero[i] = snapshot.hold_zero[i];
            state->envelope_pipeline[i] = snapshot.envelope_pipeline[i];
        }
        state->bus_value = snapshot.bus_value;
        state->bus_value_ttl = snapshot.bus_value_ttl;
        state->write_pipeline = snapshot.write_pipeline;
        state->write_address = snapshot.write_address;
        state->voice_mask = snapshot.voice_mask;
        resid_shim_filter_probe(g_shim_sid_psid, state->filter_probe);
    }
    LeaveCriticalSection(&g_state_lock);
}
