#include "vice-shim.h"
#include "main.h"
#include "machine.h"
#include "vic.h"
#include "cia.h"
#include "sid.h"
#include "cpu.h"

void* vice_machine_create()
{
    machine_t* machine = machine_create();
    machine_init(machine);
    return machine;
}

void vice_machine_destroy(void* machine)
{
    machine_destroy((machine_t*)machine);
}

void vice_machine_reset(void* machine)
{
    machine_reset((machine_t*)machine);
}

void vice_machine_step_cycle(void* machine)
{
    machine_step_cycle((machine_t*)machine);
}

uint8_t vice_cpu_get_a(void* machine) { return ((machine_t*)machine)->cpu->a; }
uint8_t vice_cpu_get_x(void* machine) { return ((machine_t*)machine)->cpu->x; }
uint8_t vice_cpu_get_y(void* machine) { return ((machine_t*)machine)->cpu->y; }
uint8_t vice_cpu_get_p(void* machine) { return ((machine_t*)machine)->cpu->p; }
uint8_t vice_cpu_get_sp(void* machine) { return ((machine_t*)machine)->cpu->sp; }
uint16_t vice_cpu_get_pc(void* machine) { return ((machine_t*)machine)->cpu->pc; }

void vice_vic_get_state(void* machine, struct vice_vic_state* state)
{
    vic_t* vic = ((machine_t*)machine)->vic;

    state->cycle = vic->cycle;
    state->raster_line = vic->raster_line;
    state->raster_cycle = vic->raster_cycle;
    state->bad_line = vic->bad_line;
    state->display_state = vic->display_state;
    state->sprite_dma = vic->sprite_dma;

    for (int i = 0; i < 64; i++)
    {
        state->registers[i] = vic->registers[i];
    }
}

void vice_cia_get_state(void* machine, int cia_index, struct vice_cia_state* state)
{
    cia_t* cia = cia_index == 0 ? ((machine_t*)machine)->cia1 : ((machine_t*)machine)->cia2;

    state->port_a = cia->port_a;
    state->port_b = cia->port_b;
    state->ddr_a = cia->ddr_a;
    state->ddr_b = cia->ddr_b;
    state->timer_a = cia->timer_a;
    state->timer_b = cia->timer_b;
    state->icr = cia->icr;
    state->cra = cia->cra;
    state->crb = cia->crb;
    state->interrupt_flag = cia->interrupt_flag;
}

void vice_sid_get_state(void* machine, struct vice_sid_state* state)
{
    sid_t* sid = ((machine_t*)machine)->sid;

    for (int i = 0; i < 32; i++)
    {
        state->registers[i] = sid->registers[i];
    }

    for (int i = 0; i < 3; i++)
    {
        state->accumulators[i] = sid->voices[i].accumulator;
        state->envelopes[i] = sid->voices[i].envelope;
    }

    state->filter_state = sid->filter_state;
}