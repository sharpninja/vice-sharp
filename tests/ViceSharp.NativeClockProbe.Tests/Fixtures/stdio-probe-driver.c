/* Phase C step 2 driver for the scripted-stdio binding of the real gate adapter. Not VICE source.
 * argv: clock publish flush read errorFlag stopWrite stopFlush (the StdioAdapterFixtures.AdapterInput fields).
 * Calls vice_sharp_observe_clock(clock) once from the unmodified gate-observer.c, whose stdio calls reach the
 * probes below through stdio-probe-io.h. The probes return the scripted results and record each call.
 * Output on the real stdout, one event per line: "EV <operation> <stream> <value as hex, '-' when empty>",
 * then "END returned" or "END exit <status>".
 */
#include <inttypes.h>
#include <setjmp.h>
#include <stdarg.h>
#include <stdint.h>
#include <string.h>
#undef fprintf
#undef fflush
#undef fgetc
#undef ferror
#undef _Exit

void vice_sharp_observe_clock(uint64_t clock);

static int publish_result, flush_result, read_result, error_flag, stop_write_result, stop_flush_result;
static int stop_written;
static jmp_buf exit_point;
static int exit_status;

static const char *stream_name(FILE *stream)
{
    return stream == stderr ? "stderr" : stream == stdin ? "stdin" : stream == stdout ? "stdout" : "other";
}

static void record(const char *operation, const char *stream, const char *value)
{
    size_t i, n = value ? strlen(value) : 0;
    fputs("EV ", stdout);
    fputs(operation, stdout);
    fputc(' ', stdout);
    fputs(stream, stdout);
    fputc(' ', stdout);
    if (n == 0) {
        fputc('-', stdout);
    }
    for (i = 0; i < n; i++) {
        static const char hex[] = "0123456789ABCDEF";
        fputc(hex[((unsigned char)value[i]) >> 4], stdout);
        fputc(hex[((unsigned char)value[i]) & 15], stdout);
    }
    fputc('\n', stdout);
}

int probe_fprintf(FILE *stream, const char *format, ...)
{
    char text[256];
    va_list args;
    va_start(args, format);
    vsnprintf(text, sizeof text, format, args);
    va_end(args);
    record("fprintf", stream_name(stream), text);
    if (strncmp(text, "VICE_GATE_STOP,", 15) == 0) {
        stop_written = 1;
        return stop_write_result;
    }
    return publish_result;
}

int probe_fflush(FILE *stream)
{
    record("fflush", stream_name(stream), "");
    return stop_written ? stop_flush_result : flush_result;
}

int probe_fgetc(FILE *stream)
{
    record("fgetc", stream_name(stream), "");
    return read_result;
}

int probe_ferror(FILE *stream)
{
    record("ferror", stream_name(stream), "");
    return error_flag;
}

_Noreturn void probe_exit(int status)
{
    char text[16];
    snprintf(text, sizeof text, "%d", status);
    record("_Exit", "process", text);
    exit_status = status;
    longjmp(exit_point, 1);
}

int main(int argc, char **argv)
{
    uint64_t clock;
    if (argc != 8) {
        fputs("usage: clock publish flush read errorFlag stopWrite stopFlush\n", stderr);
        return 2;
    }
    clock = strtoull(argv[1], NULL, 10);
    publish_result = atoi(argv[2]);
    flush_result = atoi(argv[3]);
    read_result = atoi(argv[4]);
    error_flag = atoi(argv[5]);
    stop_write_result = atoi(argv[6]);
    stop_flush_result = atoi(argv[7]);
    if (setjmp(exit_point) == 0) {
        vice_sharp_observe_clock(clock);
        record("return", "native", "");
        fputs("END returned\n", stdout);
    } else {
        printf("END exit %d\n", exit_status);
    }
    fflush(stdout);
    return 0;
}
