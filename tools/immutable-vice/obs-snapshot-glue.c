/* External wrapper only. Never copy into the immutable VICE source tree.
 * PLAN-NATIVERESTORE-001 D1 (R11 B, N05): machine glue for snapshot-observer.c, every value
 * read through the real headers of the pinned source (cc67418cba58e71d8e70fd353e9de47199ad2075):
 *  - the register file: maincpu_regs (maincpu.h 55, struct mos6510_regs_s of mos6510.h 37 to 48:
 *    a, x, y, sp, pc, p, n, z), written by maincpu_snapshot_read_module (mainc64cpu.c 934 onward,
 *    mainviccpu.c 756 onward) before this glue copies it; p excludes the zero and sign bits,
 *    n carries the sign in bit 7, z is zero exactly when the zero flag is set (mos6510.h 116 to 136);
 *  - the effective drive resources after a load: Drive<n>Type and Drive<n>TrueEmulation
 *    (resources.h 162, resources_get_int) and the attached image name of each unit's first drive
 *    (attach.h 52, file_system_get_disk_name; "none" when nothing is attached), so an image
 *    detached by the R10 launch precondition is visible as an explicit no-media context.
 * Same for x64sc and xvic. Compile with -I<work>/src -I<build>/src.
 * maincpu.h is not included: it pulls mainlock.h and archdep.h (an arch header outside the
 * wrapper include path, found by the D5 Link stage on Linux 2026-10-21); the one declaration this
 * unit needs from it, "extern mos6510_regs_t maincpu_regs;" (maincpu.h 55), is repeated here
 * against the pinned type of mos6510.h, so the glue still binds the real global. */
#include <stddef.h>
#include <stdio.h>

#include "vice.h"
#include "attach.h"
#include "mos6510.h"
#include "resources.h"

#include "snapshot-observer.h"

extern mos6510_regs_t maincpu_regs;   /* maincpu.h 55 */

void vice_sharp_snapshot_registers(vice_sharp_cpu_regs *out)
{
    out->a = maincpu_regs.a;
    out->x = maincpu_regs.x;
    out->y = maincpu_regs.y;
    out->sp = maincpu_regs.sp;
    out->pc = (uint16_t)maincpu_regs.pc;   /* unsigned int in the pinned struct (mos6510.h 40) */
    out->p = maincpu_regs.p;
    out->n = maincpu_regs.n;
    out->z = maincpu_regs.z;
}

int vice_sharp_snapshot_drives(char *buffer, size_t size)
{
    unsigned int unit;
    size_t used = 0;
    for (unit = 8; unit <= 11; unit++) {
        char type_name[24];
        char tde_name[32];
        int type = -1;
        int tde = -1;
        const char *image;
        int written;
        snprintf(type_name, sizeof type_name, "Drive%uType", unit);
        snprintf(tde_name, sizeof tde_name, "Drive%uTrueEmulation", unit);
        (void)resources_get_int(type_name, &type);
        (void)resources_get_int(tde_name, &tde);
        image = file_system_get_disk_name(unit, 0);
        written = snprintf(buffer + used, size - used, "%s%d,%d,%s", unit == 8 ? "" : ",", type, tde,
                           (image != NULL && image[0] != '\0') ? image : "none");
        if (written < 0 || (size_t)written >= size - used) {
            return -1;
        }
        used += (size_t)written;
    }
    return 0;
}
