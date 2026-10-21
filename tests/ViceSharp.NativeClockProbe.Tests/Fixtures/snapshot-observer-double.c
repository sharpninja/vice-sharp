/* Test fixture only. Not VICE. Separate translation unit so the wrapper's calls cross an object boundary:
 * the real CPU-module reader and the real whole-snapshot reader return scripted results, the machine glue
 * copies scripted registers and a scripted drives string, and the sink prints to stdout for the test. */
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#include "snapshot-observer.h"

extern int fake_read_result;
extern int fake_load_result;
extern vice_sharp_cpu_regs fake_regs;
extern const char *fake_drives;
extern int real_read_calls;
extern int real_load_calls;

/* Defined under the real names: with --wrap the wrapper's __real_ references resolve to these, and the driver's
 * plain calls are redirected to the wrapper, exactly as in the observer link. */
int maincpu_snapshot_read_module(void *s)
{
    (void)s;
    real_read_calls++;
    return fake_read_result;
}

int machine_read_snapshot(const char *name, int event_mode)
{
    (void)name;
    (void)event_mode;
    real_load_calls++;
    return fake_load_result;
}

void vice_sharp_snapshot_registers(vice_sharp_cpu_regs *out)
{
    *out = fake_regs;
}

int vice_sharp_snapshot_drives(char *buffer, size_t size)
{
    if (strlen(fake_drives) >= size) {
        return -1;
    }
    strcpy(buffer, fake_drives);
    return 0;
}

void vice_sharp_snapshot_sink(const char *line)
{
    puts(line);
}
