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

// CPU resume/pipeline state (TR-LOCKSTEP-VSF-001): the .vsf-restored in-flight
// context of the x64sc main CPU right after vice_machine_read_snapshot, i.e.
// everything the MAINCPU module (mainc64cpu.c maincpu_snapshot_read_module)
// and the C64MEM module (c64memsnapshot.c pport block) carry beyond the plain
// register file: the last-opcode interrupt context, the pending BA-low stall
// flags, the 6510 processor port (written data/dir plus the effective read
// values that select ROM/IO banking), and the interrupt-status clocks. Field
// order and 1-byte packing mirror ViceSharp.Core.ViceNative.ViceCpuPipelineState
// byte for byte.
#pragma pack(push, 1)
struct vice_cpu_pipeline_state {
    uint64_t clk;                /* maincpu_clk at export */
    uint32_t last_opcode_info;   /* vsf MAINCPU: last opcode + IRQ delay/enable flags */
    uint32_t ba_low_flags;       /* vsf MAINCPU: maincpu_ba_low_flags (pending BA stall) */
    uint8_t  pport_data;         /* vsf C64MEM: pport.data ($01 written value) */
    uint8_t  pport_dir;          /* vsf C64MEM: pport.dir ($00 written value) */
    uint8_t  pport_data_read;    /* effective $01 read value (pull-ups applied) */
    uint8_t  pport_dir_read;     /* effective $00 read value */
    uint32_t global_pending_int; /* vsf MAINCPU interrupt module: pending IK_* mask */
    uint64_t irq_clk;            /* interrupt-status clocks (CLOCK) */
    uint64_t nmi_clk;
    uint64_t irq_delay_cycles;
    uint64_t nmi_delay_cycles;
};
#pragma pack(pop)

VICE_SHIM_API void vice_cpu_get_pipeline_state(void* machine, struct vice_cpu_pipeline_state* state);

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
    /* TR-LOCKSTEP-VSF-001: .vsf VIC-II module resume context beyond the register
       file (viciisc/vicii-snapshot.c saves both). allow_bad_lines is the DEN
       seen-at-line-$30 latch that gates every badline (and therefore every
       BA-low CPU stall) for the rest of the frame; idle_state is the
       display/idle g-access state. Both are required to stage a managed VIC
       from a mid-frame snapshot. */
    uint8_t allow_bad_lines;
    uint8_t idle_state;
};

VICE_SHIM_API void vice_vic_get_state(void* machine, struct vice_vic_state* state);

// Visible frame capture (for native visible raster/pixel checkpoints under display-mode effects, e.g. invalid ECM COL_NONE black per vicii-draw-cycle.c:133-141 etc.).
// Returns non-zero on success; fills buffer with BGRA (320x200 recommended). Authentic from native VICE vicii state.
VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height);

// Per-pixel VIC oracle (PLAN-VICEPARITY-001 Phase 0 / TR-VIC-ORACLE-001): copy
// the visible frame window from the viciisc raster draw buffer as raw VICE
// palette indices, one byte per pixel (0x00-0x0F). vicii-draw-cycle.c writes
// these indices 8 per cycle into vicii.dbuf; each line is flushed into the
// canvas draw buffer by vicii_raster_draw_handler. Index-exact comparison is
// palette-independent, which is what the VIC parity ACs assert.
VICE_SHIM_API int vice_vic_capture_frame_indices(void* machine, uint8_t* buffer, int length, int* width, int* height);

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
    /* TR-LOCKSTEP-VSF-001: .vsf CIA resume context beyond the live counters -
       the timer latches (ciat_read_latch; reload values on underflow) and the
       ICR interrupt-enable mask (cia_context->irq_enabled, saved by the CIA
       snapshot module). Required to stage a managed CIA from a snapshot. */
    uint16_t timer_a_latch;
    uint16_t timer_b_latch;
    uint8_t irq_mask;
    uint8_t reserved;
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

// Read a register straight from the shim's private, clocked reSID instance.
// Returns engine-computed values such as OSC3 ($1b) and ENV3 ($1c) that only
// exist once reSID has been clocked via vice_sid_render_samples (the headless
// main SID #0 read by vice_sid_get_state is never clocked). addr is 0x00-0x1f.
VICE_SHIM_API uint8_t vice_sid_engine_read(void* machine, uint16_t addr);

// Clock the shim's private reSID instance by exactly `cycles` CPU cycles using a
// 1:1 (sample_rate == cpu_clock) renderer, so its internal ADSR/accumulator state
// advances cycle-exactly - unlike vice_sid_render_samples, whose 44100 Hz
// resampling advances ~22.34 cycles per sample and cannot be matched to an exact
// cycle count. Syncs the current SID registers first. Use this for cycle-exact
// SID lockstep against the managed Sid6581.
VICE_SHIM_API void vice_sid_clock(void* machine, int cycles);

// Single-cycle reSID oracle (PLAN-VICEPARITY-001 Phase 0 / TR-SID-ORACLE-001).
// vice_sid_clock batches through reSID's clock(delta_t), which drops the
// single-cycle envelope/waveform pipelines; the exact API drives
// reSID::SID::clock() one cycle at a time so managed parity tests can assert
// bit-exact equality. vice_sid_exact_open force-recreates the shim's private
// engine (no batched history) and syncs the machine's SID register file once;
// afterwards drive writes ONLY through vice_sid_exact_write (a re-sync would
// clobber pipeline state).

// Full reSID internal state export. Field order and packing (1-byte) mirror
// ViceSharp.Core.ViceNative.ViceSidExactState byte for byte.
#pragma pack(push, 1)
struct vice_sid_exact_state {
    uint8_t  registers[32];
    uint32_t accumulator[3];
    uint32_t shift_register[3];
    uint32_t shift_register_reset[3];
    uint32_t shift_pipeline[3];
    uint32_t floating_output_ttl[3];
    uint16_t pulse_output[3];
    uint16_t rate_counter[3];
    uint16_t rate_counter_period[3];
    uint16_t exponential_counter[3];
    uint16_t exponential_counter_period[3];
    uint8_t  envelope_counter[3];
    uint8_t  envelope_state[3];
    uint8_t  hold_zero[3];
    uint8_t  envelope_pipeline[3];
    uint8_t  bus_value;
    uint32_t bus_value_ttl;
    uint8_t  write_pipeline;
    uint8_t  write_address;
    uint8_t  voice_mask;
    // VICE-Sharp S9 parity probe: reSID internal filter state.
    // [0]=Vlp [1]=Vbp [2]=Vhp [3]=v1 [4]=v2 [5]=v3 [6]=sum [7]=mix
    // [8]=filter.output() [9]=Vlp_x [10]=Vlp_vc [11]=Vbp_x [12]=Vbp_vc
    int32_t  filter_probe[13];
};
#pragma pack(pop)

VICE_SHIM_API int vice_sid_exact_open(void* machine);
VICE_SHIM_API void vice_sid_exact_reset(void* machine);
VICE_SHIM_API int vice_sid_exact_clock(void* machine, int cycles);
VICE_SHIM_API void vice_sid_exact_write(void* machine, uint16_t addr, uint8_t value);
VICE_SHIM_API uint8_t vice_sid_exact_read(void* machine, uint16_t addr);
VICE_SHIM_API int16_t vice_sid_exact_output(void* machine);
VICE_SHIM_API void vice_sid_exact_get_state(void* machine, struct vice_sid_exact_state* state);

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
