# D64 fixture and assertion specification

Prepared by Codex on PAYTON-LEGION2, starting 2026-10-11T08:31:52-05:00.

Status: R0 draft only. No requirement application, test implementation, native execution or acceptance. This supplements FR-BACKFILL-MEDIA-001/ac-01 in the exact fourteen-record draft at `../20261011T080155-0500-capacitor-guard-remediation/proposed-field-delta.json`. It does not create the still-missing TR-MEDIA-D64-001 or TEST-D64IO-001.

## Authority and layer

The immutable reference is VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. The twelve materialized source files in `preparation-evidence.json`, plus the `fsimage.c` dispatch pin in `verification.json`, were checked against that commit's Git blobs. Use the materialization only as read-only evidence. The dirty native checkout is not a clean build or acceptance claim.

This contract covers `disk_image_read_sector` / `disk_image_write_sector` on filesystem-backed D64 with `image->gcr == NULL`, not DOS/IEC commands. GCR-present image access, firmware execution and rotation are separate true-drive obligations. A public managed exception is not assumed identical to a native numeric return. The eventual adapter must retain raw native status, managed outcome, operation stage and byte effects before applying an explicitly reviewed semantic mapping.

Current live FR-DRV-001/ac-05 explicitly requires standard 35-track D64. Native D64 recognition accepts 35 through 42 tracks, with or without an error map. Include those variants in the native behavior inventory; do not silently treat their omission as full VICE parity. Their product/application scope must be reconciled in the exact reviewed proposal before implementation. G64, D71 and other formats are not silently added to this D64 contract or removed from the full plan.

## Deterministic fixture recipe

For track count N in 35..42, sector count is `683 + 17*(N-35)`. Tracks 1..17 have 21 sectors; 18..24 have 19; 25..30 have 18; 31..42 have 17. Sectors are zero-based within each track. Linear sector L is the sum of all preceding tracks' sector counts plus the sector index S.

Every sector has exactly 256 bytes. Bytes 0 and 1 hold L in little-endian order; byte 2 is track T; byte 3 is S. Byte J for J=4..255 is `(17*T + 31*S + J) & 255`. This makes the complete sector payload address-distinct, including sectors above 255. The write payload is that sector's original 256 bytes XOR 255, byte by byte. The pre-read caller buffer is 256 bytes of 0xA5.

`NOMAP` is exactly `256*sectorCount` bytes. `MAP01` appends one byte of value 1 per sector. The sixteen recipe hashes in `preparation-evidence.json` were calculated from independently constructed in-memory byte arrays. No fixture file was materialized and no native/managed test ran. In the approved fixture phase, save disposable fixture copies and verify these hashes before use. Preserve original evidence inputs.

These patterns are image-layer fixtures, not valid DOS directory/BAM structures. In particular, track 18 sector 0 contains the address pattern, not a formatted filesystem. Never use them to claim a successful DOS-mediated BAM read or program load.

## Named cases and exact observations

### D64_AllStandardSectors

Read all 683 sectors of D64-PATTERN-35-NOMAP, resetting the caller buffer to 0xA5 before each read. Expected raw native status is 0 and all 256 bytes equal the independent recipe. Check operation address and byte offset `L*256`, not just returned bytes. No image byte changes. Repeat on MAP01. Negative controls independently corrupt one byte, substitute a different sector, shift an offset by 256, return a nonzero status, or mutate the image during a read. Each control must be rejected by the corresponding assertion inside a passing mock harness.

### D64_RecognitionAndZoneBoundaries

Inventory all sixteen exact-size recipe variants. The D64 recognizer returns 1, sets tracks to N and maximum half-tracks to 84; only MAP01 allocates an error map of sectorCount bytes. Read first/last sectors around 17/18, 24/25, 30/31 and N. Do not use last-track-only tests as a substitute for the all-683 standard matrix. Wrong track count, wrong error-map length, shifted zone boundary and treating MAP01 as extra data sectors are separate negative controls.

Source: `diskimage/fsimage-probe.c:83-149`, `diskimage/diskimage.c:82-136`, `diskconstants.h:42-51`.

### D64_InvalidGeometry

Use NOMAP and MAP01 with in-allocation valid addresses, then requests `(0,0)`, `(43,0)`, `(1,21)`, `(17,21)`, `(18,19)`, `(24,19)`, `(25,18)`, `(30,18)`, `(31,17)`, `(42,17)`. Read and write return -1 before buffer/file/error-map changes. Distinguish the internal geometry reason from the public -1 result. Negative managed indices are adapter-domain tests, not negative C unsigned arguments. Assert no read/write attempt occurs after a failed geometry check. Controls: clamp/wrap an invalid address, return success, change the caller buffer, or perform an I/O call anyway.

Source: `diskimage/fsimage-check.c:50-74`, `fsimage-dxx.c:344-354,439-446`.

### D64_ErrorMapAllByteValues

For each byte value E=0..255, start from a fresh 35-track MAP01 fixture and change only BAM sector L=357's error-map byte at `174848+357`. Read `(18,0)` with the 0xA5 buffer. Leave `gcr == NULL`; no out-of-allocation index is used.

Expected status mapping is: E=1 -> 0; 2 -> 20; 3 -> 21; 4 -> 22; 5 -> 23; 7 -> 25; 8 -> 26; 9 -> 27; 10 -> 28; 11 -> 29; 15 -> 74; 16 -> 24; all other values -> 0. The buffer receives the full sector only for E in `{0,1,5,7,8}`. All other values leave all 256 sentinel bytes unchanged. In particular E=6,12,255 return 0 without reading sector data. Do not turn that unusual behavior into a more intuitive error or success-with-data contract.

Assert the map and image do not change. Controls independently change status, overwrite a hard-error buffer, retain a soft-error buffer, coerce an unknown map byte, alter the map, or invoke file I/O on the hard-error branch.

Source: `fsimage-dxx.c:362-427`, `cbmdos.h:33-48,73,105-117`.

### D64_WriteReadReopen

For every standard sector, start with a fresh NOMAP fixture, write the XOR payload, reread, close and reopen, then read again. Expected status is 0, exact payload on both reads, unchanged length and a whole-file difference confined to that sector. The payload differs at every byte, so precisely 256 data bytes change. Preserve neighboring sectors and the source fixture.

For each E=0..255, repeat the BAM write on a fresh error-map fixture from the preceding case. On successful data and metadata I/O, the in-memory map byte becomes 1 and the persisted map byte is 1; E=1 needs no metadata write. All other map bytes remain unchanged. Assert the ordered data write, conditional one-byte map write and final flush. Controls: in-memory-only success, wrong-sector write, missing map clearing, extra map clearing, lost fresh-open bytes or absent flush.

Source: `fsimage-dxx.c:431-481`.

### D64_ReadOnlyPrecedence

Set the native image read-only flag before calling the public write function. Test valid `(18,0)` and invalid `(0,0)` addresses. Both return -1 without entering filesystem write dispatch; file, map and caller payload remain unchanged. Distinguish read-only rejection from geometry failure in the trace even though both raw statuses are -1. Controls: bypass the flag, dispatch before rejecting, or mutate then report failure.

Source: `diskimage/diskimage.c:673-696`.

Also test a non-null filesystem-image structure with a null backing stream. Public read returns 74 (not ready); public write with read_only=0 returns -1, with no sector access. With read_only=1, public write rejects before dispatch. Read also handles a null filesystem-image pointer, but write dereferences that pointer: do not call the write routine with fabricated invalid internal ownership and pretend it has a portable return value. Controls conflate read 74 with write -1, dispatch on read-only, or access the absent stream. Source: `diskimage/fsimage.c:201-237,241-287`.

### D64_UnrecognizedLengths

For every exact recipe size, test size minus 1 and plus 1, plus empty input. Deduplicate lengths in the case manifest. Expected D64 recognition is false, not an invented universal error code for the entire image-format dispatcher. Also test an exact declared size whose backing read returns EOF during the recognizer's full-file scan; expected D64 recognition is false. Keep this separate from a file shortened after a successful open. Controls: accept a nonmatching D64 size, infer a map from approximate length, or accept an incomplete scan.

Source: `fsimage-probe.c:98-143`. Exact backing-read fault injection remains an adapter prerequisite, not existing test evidence.

### D64_GeometryVersusExtent

Use 35-track NOMAP, no error map and no GCR. Geometry permits `(36,0)` because the upper bound is MAX_TRACKS_1541=42, not `image->tracks`. A read at that fresh file's EOF returns -1 and transfers no bytes. A successful write of 256 bytes at offset 174848 extends the file to 175104 bytes and returns 0. A fresh D64 recognizer does not accept that intermediate length. Assert geometry, operation status, length, bytes and recognition separately. A write to `(42,16)` is a separate sparse-extension case requiring an explicit platform/file gap-byte observation, not an assumed zero-fill oracle.

Do not execute `(36,0)` on a 683-byte error map: the inspected sector routine indexes the map without checking its length. Such a fabricated internal state risks undefined native memory access and has no deterministic golden value. Represent it as an adapter-precondition/safety case, disclose any reachable public-path evidence, and stop instead of patching VICE or inventing a portable native result. It is not a skipped required successful case.

Source: `fsimage-check.c:60-74`, `fsimage-dxx.c:365-367,376-382,439-481`.

### D64_IoFailuresAndPartialEffects

Script and record failures at distinct operation stages: seek before data read, read returning no complete item, seek/data write failure, error-map write failure after successful data write, and final fflush failure. `util_fpread` and `util_fpwrite` treat `fseek < 0` or item count <1 as failure. The sector operation returns -1 for a failed data read/write. It does not promise that a partial fread/fwrite left the buffer/file unchanged. The C library's partial-item effects require a pinned executable/runtime and an observed byte mask; do not assert an invented untouched buffer.

A failed metadata write is logged, but the map was already set to 1 in memory, final fflush is attempted, and the function still returns 0. The final fflush return itself is ignored. Therefore status 0 does not by itself prove persistence. Capture actual persisted bytes, log/error state and reopen outcome separately. Correct mocks encode these branch results; real error/partial-byte fixtures stay blocked until a non-mutating external observation/fault mechanism and exact platform outcomes are pinned. No file-system damage or real-disk fault injection is authorized by this document.

Controls: all-or-nothing rollback fabricated after a partial write, failure converted to success, metadata failure converted to -1, hidden flush failure, or buffer effects discarded by the semantic adapter.

Source: `util.c:593-625`, `fsimage-dxx.c:374-398,453-481`.

## Remaining implementation prerequisites

- Pin the actual immutable native executable, C runtime, external observer, public adapter seam and status mapping. No native source edits, link-time symbol replacement or inferred bus values.
- Materialize and verify recipe fixtures only after R0 approval. Add independently derived expected tuples, correct mocks and every negative control first; all must be green before production changes.
- Resolve the 36..42-track product scope and missing TR/TEST approval dependencies without dropping them from the whole-plan inventory.
- Bind identical cases and assertions to real providers; record every unavailable fault scenario and GCR/true-drive obligation as open. These preparation documents are not mock-green or real parity evidence.
