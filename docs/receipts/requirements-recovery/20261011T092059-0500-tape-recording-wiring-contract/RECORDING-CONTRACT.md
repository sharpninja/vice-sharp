# Draft tape recording contract

Status: R0 proposal supplement. No requirement has been applied. No mock, native provider, managed provider or acceptance test has run for these vectors. Source-derived expectations are not runtime receipts.

## Scope and source

This document specifies the eleven ac-05/ac-06 groups of `FR-RUNTIME-TAPE-002` in the [base proposal](../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json). It supplements, rather than replaces, the [motor matrix](../20261011T085402-0500-tape-motor-matrix/MOTOR-CONTRACT.md). `contract-vectors.json` holds typed examples and source hashes.

Oracle: immutable VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. Anchors are relative to its `vice` directory: `src/datasette/datasette.c:1000-1021,1035-1038,1091-1099,1159-1169,1172-1321`; buffer path `391-409`; `src/tape/tap.c:1558-1568`. Source pins must match Git blobs without modifying native sources. C64 and VIC-20 PAL/NTSC are required; PLUS4's extra divide/multiply is not substituted into these machines.

## Provider and evidence contract

Observations include current clock C, last-write clock W, raw last-write line B, internal motor M, delayed-stop clock D, mode, pending alarm, physical stream position, logical TAP position P, image size, dirty flag, cycle counter, total counter, buffer indices, exact file bytes and ordered calls to seek/read/write, event record, sense, UI and alarms.

Use the same assertions with independent scripted mocks, then the managed adapter and immutable native observer. Negative controls must change one asserted field or omit/reorder one required interaction and be rejected inside a passing harness. Do not derive expected bytes from the managed encoder. A mock proves assertion discrimination, not native observability or runtime parity.

Native fixtures must use a documented public command/register sequence or supported snapshot state. Function-static fields not represented in VSF require a controlled process/prelude; they must not be silently injected or assumed reset. A real provider lacking an observable needed field is an open gate, not a skip or fabricated value. No new native exports, source edits or private-memory patching are authorized by this supplement.

## Recording_EligibilityAndWriteEdges

Evaluate all combinations of attached image, accepted mode and internal motor flag. Attached RECORD plus M nonzero is necessary. Absent image, STOP/START/FORWARD/REWIND, and M=0 never call the byte writer. A read-only image rejects the RECORD command; it does not supply a reachable RECORD fixture by fiat. There is no extra read-only check in `datasette_toggle_write_bit` itself.

For each eligible and ineligible state, exercise previous B in {-1,0,8} and new raw line in {0,8}. Only 0 to 8 qualifies. Repeated values, 8 to 0, and initial -1 to 8 do not write. B becomes the exact new raw value on every call, including ineligible calls and failed writes. Retain 8, not normalized Boolean 1.

For a qualifying eligible edge: W >= C sets W=C and emits nothing. Test equality and future W separately. W < C enters `bit_write`. A no-image write still changes B; attach/start and later command ordering determine whether the next edge qualifies. Test the sequence, not just the last edge.

Negative controls: write while stopped; reject a startup write; emit on repeated high or falling raw edge; leave B unchanged while ineligible; normalize raw B; write bytes when W>=C.

## Recording_DuringStartupWindow

At C=100000, issue RECORD while stopped, then motor-on. Native state is M=1, W=100000, B=0, startup alarm=132000. At C=100008, raw line 8 writes byte `01`, advances P by one and cycle counter by one. The future startup alarm does not prevent recording. The UI motor indicator is not the eligibility predicate.

## Recording_DuringDelayedStop

With accepted RECORD and M=1, motor-off at C=100000 sets D=132000 but retains M. Prepare B=0 and W=100000 through the recorded prelude. A qualifying edge at C=100008 writes `01`; D remains pending. Pair with an otherwise identical M=0 control that emits nothing.

## Recording_AfterStopDeadlineBeforeCallback

An existing alarm beyond D can leave M=1 after C reaches D. Pin that prelude from the motor matrix. With D=132000, a later existing alarm at 135996, W=132000 and B=0, an edge at C=132008 writes `01` before the callback. Reaching D alone is not a stop. This synthetic unit tuple requires a native-reachable prelude before it counts as real evidence.

## Recording_AfterExecutedStop

Execute the due callback with C>=D first. It unsets its alarm, clears D, emits UI motor-off and clears M before any subsequent write edge. Raw line changes then update B but emit no bytes, regardless of previously accepted RECORD mode. Pair callback-before-edge and edge-before-callback at the same clock and preserve scheduler order; do not sort observations afterward.

## RecordThenMotorOn_FirstEdge

RECORD resets W to zero; motor-on from M=0 calls the start helper, seeks to offset+P, sets an alarm only if absent, and seeds W=C and B=0. At C=100000 then C=100008, first qualifying edge writes `01`. Include start at clock zero: zero remains the W sentinel, not a distinct initialized Boolean. Repeat first edge at the same clock, then drop to 0 and rise eight cycles later.

## MotorOnThenRecord_FirstEdgeFromZero

Motor-on at C=100000 seeds W=100000 and B=0. RECORD then resets W=0 and does not call the start helper or seek. First edge at C=100008 therefore encodes gap 100008, not gap 8: v1/v2 bytes `00 A8 86 01`, v0 byte `00`; cycle increment 12501. A test normalizing both command orders to the same first gap is wrong. Preserve VICE's documented first-pulse quirk.

## Recording_GapEncodingAndFailures

`contract-vectors.json` supplies 27 successful/no-write boundary rows and 12 short/partial-write rows. Baseline: attached writable RECORD, M=1, B=0, W=1000, P=100, size=100, cycle counter=10, total=12, dirty=0. New line=8. C is W+gap. Image payload must be valid for attachment; this is a scripted unit fixture, not evidence that arbitrary private state is reachable in native VICE.

- Gap 0: same-clock guard; no write, W remains 1000.
- Gap 6: bit writer returns without bytes or metadata changes; W returns to 1000 after subtracting remainder.
- Gap 7: one zero byte is written. W returns to 1000; cycle increment is zero; P and size grow and dirty becomes 1. Do not change the threshold to 8.
- Gap 8: byte `01`, W=1008, cycle increment 1.
- Gap 2046: byte `FF`, W=3040, cycle increment 255. Six cycles remain in the clock difference for the next edge.
- Gap 2047: v0 writes `00`; v1/v2 write `00 FF 07 00`. W=3047, cycle increment 255.

On complete successful output: P increases by bytes reported written; size=max(old size,P); cycles increase by the effective gap divided by 8 with integer truncation; total=max(old total,new cycles); dirty=1; UI counter update occurs. Repeat with size greater than P and total greater than new cycles to prove no shrinking. Repeat with dirty already 1 to prove preservation on failure.

On a short-byte or long-prefix write returning zero: no P increment. On a long payload write returning 0,1,2 of three elements: the prefix has already incremented P; payload increments P by the returned count, then failure exits. Size, counters, total, dirty and final UI counter update are not reached. Do not rollback the prefix, payload or logical position. Record the C-runtime error flag and observable file bytes separately; an injected scripted short write is not a claim that every host failure exposes that exact physical prefix.

Every failure logs the error and calls PUBLIC STOP. With event playback inactive and network disconnected, STOP changes mode to STOP, emits sense=0 if enabled, resets W=0, reports control status and clears buffer indices. It does not instantly clear M or cancel every alarm. B is assigned 8 after STOP returns. With playback active STOP returns without event or internal mutation. With network connected it records a four-byte EVENT_DATASETTE command and defers the internal STOP. These variants retain the pre-STOP W and RECORD mode until an actual internal command executes. Test all three routing modes, including partial writes, without claiming the native writer itself is atomic.

Negative controls: wrong 7/2047 boundaries, rounded rather than truncated cycles, wrong remainder, wrong byte order, absent prefix, unconditional four-byte v0 encoding, unchanged P on partial output, metadata updated on failure, rollback, or forced immediate STOP in playback/network mode.

## Recording_LongGap_24BitBoundary

For v1/v2, bytes after the zero prefix and the cycle increment use the low 24 bits. W remains the full current clock; it is not reduced modulo 24 bits.

- 16777215: `00 FF FF FF`, cycle increment 2097151.
- 16777216: `00 00 00 00`, cycle increment 0.
- 16777217: `00 01 00 00`, cycle increment 0.

For v0 each writes only `00`, with increments 2097151,2097152,2097152 respectively. These examples stay within the signed counter range; they do not assert behavior for overflowing signed C counters. v2 still takes the same recording branch as v1, despite separate playback halfwave handling.

## Recording_FileCursor_CommandOrder

Keep physical stream cursor F and logical payload position P distinct. The writer contains no seek. An ordinary start helper seeks to offset+P; RECORD resets buffer indices and W but does not seek. A repeated motor-on while already running also does not call the start helper. Therefore adding a convenient seek before every write changes the target.

Use a deterministic payload of at least 200003 short nonzero bytes, valid 20-byte TAP header, neutral wobble/tuning/azimuth, and independently pinned attachment-scan history. Observe F with a nonmutating observer, not a corrective seek. Specify and compare these sequences:

1. Attach, RECORD, motor-on, write: helper seeks offset+P before output.
2. Attach, motor-on, RECORD, write before the alarm: motor start performs the seek; RECORD does not.
3. Attach, START, motor-on, execute a read/refill, RECORD, write: the read buffer may leave F at offset+refill-start+bytes-read while P tracks consumed payload; RECORD does not reconcile them.
4. As (3), then START while motor runs, then RECORD and write: START calls the helper and seeks offset+P; alarm preservation and W reset/seeding must also match.

For (3), C update-stream rules and the actual native C runtime are part of the fixture. Do not assert a universally portable write-at-F result for an input-to-output transition lacking a positioning operation. Capture immutable native behavior under the pinned build/runtime, including return count, error flag and file bytes. If no deterministic native observation can be obtained, keep this real-provider case OPEN; do not insert an fseek into VICE or substitute a mock result as proof.

## Recording_FileCursor_BufferBoundary

Refill occurs when next_tap+offset >= last_tap, not only when greater. Successful refill seeks offset+P, requests 100000 bytes, resets next_tap=0 and records the returned last_tap. Exercise next_tap+offset equal to last_tap-1, last_tap and last_tap+1; full and short final refills; failed seek; zero-byte read. Pair positions around payload 99999/100000/100001 using an actual sequence of decoded gaps, not a private cursor assignment. Injecting a seek failure must prevent fread; a zero refill returns failure.

For each boundary, continue through RECORD and one qualifying write, retaining F/P, buffer indices, metadata, command ordering and runtime qualifications above. Wrong providers that equate F=P+offset, refill too late, seek on RECORD, or erase buffered history must fail the same assertions.

## Still-open evidence

This specification does not create the native observation adapter, deterministic CRT failure fixtures, VSF coverage for hidden state, a successful loader fixture, or a real test result. Nonneutral wobble/azimuth, audio and RTC history, unknown TAP versions, live block-decoder differences, and all five required turbo loaders remain separate mandatory dependency cases. They are not waived by enumerating these eleven groups.
