#ifndef VICE_SHIM_H
#define VICE_SHIM_H

#include <stdint.h>

#ifdef _WIN32
#define VICE_SHIM_API __declspec(dllexport)
#else
#define VICE_SHIM_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Machine lifecycle
VICE_SHIM_API void* vice_machine_create();
VICE_SHIM_API void* vice_machine_create_model(const char* model);
VICE_SHIM_API void vice_machine_destroy(void* machine);
VICE_SHIM_API void vice_machine_reset(void* machine);
VICE_SHIM_API void vice_machine_step_cycle(void* machine);
VICE_SHIM_API int vice_machine_attach_cartridge(void* machine, const uint8_t* image, int length, int mapping_mode);
VICE_SHIM_API int vice_machine_attach_disk(void* machine, unsigned int unit, unsigned int drive, const char* path);
VICE_SHIM_API int vice_machine_detach_disk(void* machine, unsigned int unit, unsigned int drive);
VICE_SHIM_API uint8_t vice_machine_peek_ram(void* machine, uint16_t address);
VICE_SHIM_API uint8_t vice_machine_read(void* machine, uint16_t address);
VICE_SHIM_API void vice_machine_write(void* machine, uint16_t address, uint8_t value);
VICE_SHIM_API int vice_machine_get_model(void* machine);
VICE_SHIM_API int vice_machine_set_keyboard_matrix_key(void* machine, int row, int column, int pressed);
VICE_SHIM_API void vice_machine_cia1_store(void* machine, uint8_t register_index, uint8_t value);
VICE_SHIM_API uint8_t vice_machine_cia1_read(void* machine, uint8_t register_index);

// Snapshot (VICE .vsf) load/save against the active machine. Returns 0 on
// success; negative on shim-level error (-1 bad arg, -2 not active machine);
// otherwise the underlying VICE machine_read/write_snapshot return value.
VICE_SHIM_API int vice_machine_read_snapshot(void* machine, const char* path);
VICE_SHIM_API int vice_machine_write_snapshot(void* machine, const char* path);
// Last VICE snapshot error code (SNAPSHOT_* enum from snapshot.h); 0 = no error.
VICE_SHIM_API int vice_snapshot_last_error(void);

// CPU State
VICE_SHIM_API uint8_t vice_cpu_get_a(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_x(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_y(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_p(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_sp(void* machine);
VICE_SHIM_API uint16_t vice_cpu_get_pc(void* machine);

// Drive CPU State (1541-family, unit = 8..11 device number)
VICE_SHIM_API uint8_t vice_drivecpu_get_a(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_x(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_y(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_p(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_sp(void* machine, unsigned int unit);
VICE_SHIM_API uint16_t vice_drivecpu_get_pc(void* machine, unsigned int unit);

// Per-unit true-drive emulation toggle. Returns 0 on success, non-zero on failure.
VICE_SHIM_API int vice_drive_set_true_emulation(void* machine, unsigned int unit, int enabled);
VICE_SHIM_API int vice_drive_get_true_emulation(void* machine, unsigned int unit);

// VIC-II State
struct vice_vic_state {
    uint32_t cycle;
    uint16_t raster_line;
    uint8_t raster_cycle;
    uint8_t bad_line;
    uint8_t display_state;
    uint8_t sprite_dma;
    uint8_t registers[64];
};

VICE_SHIM_API void vice_vic_get_state(void* machine, struct vice_vic_state* state);

// Visible frame capture (for native visible raster/pixel checkpoints under display-mode effects, e.g. invalid ECM COL_NONE black per vicii-draw-cycle.c:133-141 etc.).
// Returns non-zero on success; fills buffer with BGRA (320x200 recommended). Authentic from native VICE vicii state.
VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height);

// Line pixel+pri snapshot at raster/draw boundary (for TR-VIC-EDGE-001 native ECM reinforcement, BACKFILL-VIDEO-001).
// Returns non-zero on success; fills pri_buffer (0/1 per pixel) for the line using native vicii state (gbuf/pri logic per vicii-draw-cycle.c:196/224).
// Checkpointed authentic pri_buffer for invalid ECM (pri preserved on COL_NONE pixels).
VICE_SHIM_API int vice_vic_get_graphics_priority_at_raster(void* machine, uint16_t raster_line, uint8_t* pri_buffer, int length);

// CIA State
struct vice_cia_state {
    uint8_t port_a;
    uint8_t port_b;
    uint8_t ddr_a;
    uint8_t ddr_b;
    uint16_t timer_a;
    uint16_t timer_b;
    uint8_t icr;
    uint8_t cra;
    uint8_t crb;
    uint8_t interrupt_flag;
};

VICE_SHIM_API void vice_cia_get_state(void* machine, int cia_index, struct vice_cia_state* state);

// SID State
struct vice_sid_state {
    uint8_t registers[32];
    uint32_t accumulators[3];
    uint8_t envelopes[3];
    uint32_t filter_state;
};

VICE_SHIM_API void vice_sid_get_state(void* machine, struct vice_sid_state* state);

// Render N audio samples from the active SID into buffer. Returns the count
// actually rendered as signed 16-bit PCM. delta_t_cycles is the host-cycle
// budget per sample request (e.g. 22 for ~44.1kHz at C64 PAL 985248Hz).
// The renderer is reference-tolerant: it uses the configured SID engine
// (fastsid or reSID) with the current siddata register state. It does not
// require sound_open() to have succeeded.
VICE_SHIM_API size_t vice_sid_render_samples(void* machine, int16_t* buffer, size_t n, int delta_t_cycles);

struct vice_interrupt_state {
    uint8_t irq_asserted;
    uint8_t nmi_asserted;
    uint8_t global_pending;
    uint8_t irq_source_count;
    uint8_t nmi_source_count;
};

VICE_SHIM_API void vice_interrupt_get_state(void* machine, struct vice_interrupt_state* state);

#ifdef __cplusplus
}
#endif

#endif
