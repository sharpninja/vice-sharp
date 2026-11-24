/* Phase C step 2 test seam, force-included (gcc -include) when compiling the unmodified
 * tools/immutable-vice/gate-observer.c for StdioAdapterRealBindingTests. Not VICE source and not part of
 * the adapter: it routes the adapter's stdio calls to recording probes whose results the fixture scripts,
 * so the identical 19 StdioAdapterFixtures cases exercise the real adapter and clock-gate logic.
 * The real-stdio driver (stdio-real-driver.c) does not use this header.
 */
#ifndef VICE_SHARP_STDIO_PROBE_IO_H
#define VICE_SHARP_STDIO_PROBE_IO_H
#include <stdio.h>
#include <stdlib.h>

int probe_fprintf(FILE *stream, const char *format, ...);
int probe_fflush(FILE *stream);
int probe_fgetc(FILE *stream);
int probe_ferror(FILE *stream);
_Noreturn void probe_exit(int status);

#define fprintf probe_fprintf
#define fflush probe_fflush
#define fgetc probe_fgetc
#define ferror probe_ferror
#define _Exit probe_exit
#endif
