# External cycle-permission gate slice

Decision locked 2026-10-08T10:58:01-05:00 before gate implementation.
Approved immutable Phase 2 control feasibility, FR-VICE-001 AC2/AC4,
TR-VICE-ORACLE-001 AC2/AC3/AC5, TEST-VICE-001 AC2/AC4. Current effective
Layer 1 readback leaves all three requirements pending and every AC false.

At each existing post-`vic_cycle` callback, an external observer publishes the
actual uint64 clock, flushes it, then receives exactly one host command byte.
Only lowercase ASCII `c` permits returning to native execution. Lowercase `q`
requests probe termination. EOF, invalid input and I/O errors fail closed.
This is a private local diagnostic protocol, not a general network API.

Core interface: `vice_sharp_clock_gate(uint64_t clock, const gate_io *io)` with
context plus publish(clock), flush(), receive() function pointers. Zero means
publish/flush success; any other value is failure. Receive returns an unsigned
byte, -1 for EOF or -2 for input error. Results are 0 continue, 64 publish
failure, 65 flush failure, 66 EOF, 67 explicit quit, 68 invalid byte, 69 missing
dependency, 70 input error. Validate every required function before side effects.
Do not flush/read after publication failure or read after flush failure.

Mock fixtures cover zero/max/intermediate clocks; positive/negative output
failure; flush failure; EOF/input failure; quit; uppercase/newline/NUL/out-of-byte
commands; null interface and each missing function. Independent literal event
orders and result codes must pass correct mocks and reject wrong results,
missing/extra/reordered events, wrong phases and changed clock values. Only
after the complete project plus all prior suites pass may gate C code begin.
Then the identical cases/assertions bind to a compiled C fixture with scripted
I/O, followed by full cumulative suites again.

A later diagnostic stdio adapter may block on the host input pipe. It must
terminate the probe on nonzero gate status without another native callback.
Runtime proof requires host readback before permission, no unsolicited next
record while paused, one permission per next callback, repeated identical
permitted prefixes, explicit quit/EOF/error outcomes, and unchanged native,
stock-source, stock-object and original-binary hashes. This does not prove
all devices freeze or all bus data exists. Never label mere clock output as
complete parity, exact half-cycle classification or the full Phase 2 exit.

Retain command output, TRX, native integrity, failed attempts and all raw
runtime messages. Include the entire slice in noon Opus catch-up; do not mark
any phase/requirement accepted before the complete gates and HV pass.

## Recorded results: 2026-10-08T11:10:31-05:00

Mock gate passed 31/31 (19 new gate cases plus 12 prior clock cases), with
199/199 VSF and 82/82 prior oracle cases before gate C implementation.
After implementation, the same 19 cases bound to compiled C and the full
project passed 50/50; prior suites again passed 199/199 and 82/82. Zero
failed/skipped/aborted. Real stdio-bound stock probes then tested 16 individual
permissions twice, explicit quit, EOF and four invalid command forms.
See `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/gate-runtime/README.md`
for exact chronology, all receipt links, native/source integrity and limits.
No acceptance or complete bus observation is claimed.

## Evidence limitations recorded 2026-10-08T13:47:42-05:00

The seven original gate-runtime stdout files are empty. Those diagnostic runs
did not establish the raw native stdout retention required above. Clock/stop
records and host-permission observations remain separately retained; do not
infer native startup/reset diagnostics from them. The stdio adapter still
needs its own mock-first buffering/termination contract coverage before a
behavior change or production harness integration. This does not waive the
retention requirement or make the existing incomplete evidence acceptable.

The pre-refactor `ClockHookImplementationTests.cs` bytes and that refactor's
clock-probe compiler log were not retained. Current sources, later passing
reruns and copies of both current compiler logs are current evidence only,
not reconstructed historical proof. Source preimages and every compiler log
must be retained for subsequent changes. See the Phase 2 `gate-runtime/README.md`
and `docs/receipts/native-vice/20261008T134300-0500-hv-evidence-corrections/`.
