/* Scratch probe 2: candidate sink fixes on a broken stderr pipe.
 * mode "flush": fprintf then fflush per event; mode "unbuf": setvbuf(stderr, _IONBF) first, then fprintf. */
#include <fcntl.h>
#include <io.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char **argv)
{
    int fds[2];
    int r, e, f = 0;
    const char *mode = argc > 1 ? argv[1] : "flush";
    FILE *log = fopen(argc > 2 ? argv[2] : "probe2-result.txt", "w");
    if (log == NULL || _pipe(fds, 4096, _O_BINARY) != 0) {
        return 90;
    }
    _close(fds[0]);
    if (_dup2(fds[1], 2) != 0) {
        return 91;
    }
    if (strcmp(mode, "unbuf") == 0) {
        setvbuf(stderr, NULL, _IONBF, 0);
    }
    r = fprintf(stderr, "VICE_BUS,repatch,514,1\n");
    e = ferror(stderr);
    if (strcmp(mode, "flush") == 0) {
        f = fflush(stderr);
    }
    fprintf(log, "mode=%s fprintf=%d ferror=%d fflush=%d detected=%d\n", mode, r, e, f, (r < 0 || e != 0 || f != 0));
    fclose(log);
    return 0;
}
