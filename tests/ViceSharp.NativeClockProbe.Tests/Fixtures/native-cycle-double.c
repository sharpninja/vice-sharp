/* Separate translation unit prevents an in-unit call from bypassing --wrap. */
#include <inttypes.h>
#include <stdint.h>
#include <stdio.h>

extern uint64_t maincpu_clk;
extern uint64_t fake_after_real;

void vic_cycle(void)
{
    printf("real,%" PRIu64 "\n", maincpu_clk);
    maincpu_clk = fake_after_real;
}
