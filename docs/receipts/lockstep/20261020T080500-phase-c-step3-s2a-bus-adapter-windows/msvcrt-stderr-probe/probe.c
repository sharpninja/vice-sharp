/* Scratch probe: how does this CRT report a write to stderr when stderr is a pipe whose read end is closed? */
#include <fcntl.h>
#include <io.h>
#include <stdio.h>

int main(void)
{
    int fds[2];
    int r1, e1, r2, e2, f;
    FILE *log = fopen("probe-result.txt", "w");
    if (log == NULL || _pipe(fds, 4096, _O_BINARY) != 0) {
        return 90;
    }
    _close(fds[0]);                  /* no reader: the pipe is broken */
    if (_dup2(fds[1], 2) != 0) {
        return 91;
    }
    r1 = fprintf(stderr, "VICE_BUS,repatch,514,1\n");
    e1 = ferror(stderr);
    r2 = fprintf(stderr, "VICE_BUS,cycle,7\n");
    e2 = ferror(stderr);
    f = fflush(stderr);
    fprintf(log, "first fprintf=%d ferror=%d; second fprintf=%d ferror=%d; fflush=%d\n", r1, e1, r2, e2, f);
    fclose(log);
    return 0;
}
