/* Independent scripted I/O spies, not VICE. Expected results live in C# fixtures. */
#include "clock-gate.h"
#include <errno.h>
#include <inttypes.h>
#include <limits.h>
#include <stdio.h>
#include <stdlib.h>

struct inputs { int publish_result; int flush_result; int receive_result; };

static int publish(void *context, uint64_t clock)
{
    printf("publish,%" PRIu64 "\n", clock);
    return ((struct inputs *)context)->publish_result;
}
static int flush(void *context)
{
    puts("flush,0");
    return ((struct inputs *)context)->flush_result;
}
static int receive(void *context)
{
    puts("receive,0");
    return ((struct inputs *)context)->receive_result;
}
static int parse_int(const char *text, int *result)
{
    char *end;
    long value;
    errno = 0;
    value = strtol(text, &end, 10);
    if (errno || text == end || *end || value < INT_MIN || value > INT_MAX) return 0;
    *result = (int)value;
    return 1;
}
int main(int argc, char **argv)
{
    struct inputs input;
    vice_sharp_gate_io io = { &input, publish, flush, receive };
    unsigned long long clock;
    char *end;
    int missing;
    int status;
    if (argc != 6) return 2;
    errno = 0;
    clock = strtoull(argv[1], &end, 10);
    if (errno || argv[1] == end || *end || argv[1][0] == '-') return 2;
    if (!parse_int(argv[2], &input.publish_result) ||
        !parse_int(argv[3], &input.flush_result) ||
        !parse_int(argv[4], &input.receive_result) ||
        !parse_int(argv[5], &missing) || missing < 0 || missing > 4) return 2;
    if (missing == 2) io.publish = NULL;
    if (missing == 3) io.flush = NULL;
    if (missing == 4) io.receive = NULL;
    status = vice_sharp_clock_gate((uint64_t)clock, missing == 1 ? NULL : &io);
    printf("status,%d\n", status);
    return ferror(stdout) ? 3 : 0;
}
