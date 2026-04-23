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
VICE_SHIM_API void vice_machine_destroy(void* machine);
VICE_SHIM_API void vice_machine_reset(void* machine);
VICE_SHIM_API void vice_machine_step_cycle(void* machine);

// CPU State
VICE_SHIM_API uint8_t vice_cpu_get_a(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_x(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_y(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_p(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_sp(void* machine);
VICE_SHIM_API uint16_t vice_cpu_get_pc(void* machine);

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

#ifdef __cplusplus
}
#endif

#endif
