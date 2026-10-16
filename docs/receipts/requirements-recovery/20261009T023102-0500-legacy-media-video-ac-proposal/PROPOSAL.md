# Legacy media/video acceptance-criteria proposal

Prepared locally by Codex on PAYTON-LEGION2, 2026-10-09T02:31:02-05:00.

Status: UNAPPLIED. This is a source-derived proposal for operator approval, not a recovery of verbatim historical AC. No canonical requirement, production code, test, native source, or acceptance state is changed by this document. Legacy requirements remain active, not retired. Every proposed criterion is unsatisfied. Historical closed states and test-count statements remain in the before images.

## Scope and authority

Continue the approved requirements recovery under PLAN-VICE-IMMUTABLE-001 and the original Grok plan. Native VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075` is immutable behavior authority. Only external wrappers/adapters may be introduced in a later approved implementation. Upstream-required in-source generation is already approved in the separate pinned materialization, not permission to modify native source. This proposal requires no native build.

Update ten existing records: FR-BACKFILL-MEDIA-001, FR-BACKFILL-VIDEO-001, TR-VIC-EDGE-001 through TR-VIC-EDGE-006, TEST-VIC-CHECKPOINT-001, and TEST-VIC-RC-001. Propose two new records, TR-MEDIA-D64-001 and TEST-D64IO-001, because the current media mapping's motor/BAM test does not specify the FR's sector-write/error contract. These IDs are proposals, not reservations; recheck collisions across requirement layers before creation. Preserve all existing IDs, layer fields, priorities and provenance. Change only title, body/condition, acceptanceCriteria and status on existing records. Proposed status is pending, never retired or completed.

Leave all seven TR-CYCLE-001 AC unchanged. Do not edit overlapping drive/tape records: their unapplied proposals are separately retained at `../20261008T153521-0500-legacy-drive-ac-proposal` and `../20261008T150905-0500-legacy-tape-ac-proposal`. Their approval is a dependency, not implied by this proposal. FR-DRV-001 and other broader disk/video requirements remain applicable and unchanged; this slice does not claim exhaustive whole-project acceptance.

## Evidence and limits

The retained source audit is `../20261008T155254-0500-legacy-media-video-source-audit`. Its three native and five managed pins were rechecked unchanged before drafting. Two additional native D64 source files match their Git blobs in the same pinned commit. Machine-readable pin lists and complete current before images accompany this proposal.

- `vice/src/viciisc/vicii-cycle.c:541-563`: VC/VCBASE/VMLI and RC/idle ordered transitions; `:51-60` covers the YSCROLL match, with allow-bad-lines gating outside that helper; `:572-601` separates BA/prefetch from matrix fetch.
- `vice/src/viciisc/vicii-chip-model.c:250-268`, `:405-433`, `:568-575`: MOS6569/MOS8565 are 63 cycles by 312 lines; MOS6567R8/MOS8562 are 65 by 263; MOS6572 is 65 by 312; MOS6567R56A is 64 by 262. MOS8565 is PAL, so "five non-PAL models" is false.
- `vice/src/viciisc/vicii-fetch.c:76-100,192-201`: matrix fetch reads the selected memory and updates internal VIC buffers; it does not establish that every system actor leaves RAM unchanged. Unchanged final bytes alone cannot detect same-value writes.
- `vice/src/diskimage/fsimage-dxx.c:336-481`: bounds, 256-byte sector reads/writes, error-map handling, returned status, successful error-map clearing, and stream flush. Some hard-error reads intentionally retain the caller buffer. Soft errors may still transfer data. Error-map-write failure is logged but does not automatically turn the later return into failure. Match the pinned reference; do not invent stronger transactional semantics.
- `vice/src/diskimage/diskimage.c:652-697`: read/write dispatch and read-only write rejection before dispatch. Public managed exceptions and native numeric status require an explicit lossless semantic adapter, not assumed ABI equality.
- Current `VicIiCheckpointTests.cs` has two managed Facts and two native ViceFacts, not a paired native/managed frame comparison. Its 200-byte range is $0720-$07E7, not $07A0-$07E7. Pattern preservation is not visible-pixel parity or a complete bus-write audit.
- Current `VicIIRcWindowTests.cs` declares eleven cases but asserts RC/idle, not VC/VCBASE/VMLI or native paired state.
- Current `VicIISpriteDmaNativeCheckpointTests.cs` declares five models, finds native sprite DMA within a loose line window, and separately checks managed IsCpuCycleStolen at line $51/x0. Captured native DMA cycle is not compared. Native DMA-active and managed CPU-steal are not interchangeable evidence. These are source observations, not newly executed test results.

## Proposed record fields

The following structured Markdown blocks define proposed text and AC. `Tests` names intended TEST coverage, not existing passing tests. The companion JSON preserves complete before/after records. All `ac-*` entries have isSatisfied=false.

### FR-BACKFILL-MEDIA-001
Kind: fr
Title: D64, tape and IEC media behavior matches immutable native VICE
Field: body
Text: Managed media behavior shall reproduce the pinned native VICE reference at the corresponding layer: D64 image-sector data/status/persistence, true-drive IEC and motor behavior, and machine-specific datasette timing/sense/record behavior. Image-buffer success, synthetic motor counters and historical test totals cannot substitute for real-emulator parity. Preserve the separate buffered and true-drive modes and all legacy evidence.
- ac-01 | For the same pinned D64 fixtures and operation sequence, sector reads/writes, returned semantic status, error-map effects and persisted bytes match native VICE; cover valid sectors, geometry boundaries, malformed/truncated input, read-only input, read errors and failed writes without treating success-only BAM reads as complete coverage. | Tests: TEST-D64IO-001
- ac-02 | True-drive IEC line contributions, both ATN edges, enabled/disabled drive cases, contention, drive motor/rotation progression and DOS-mediated BAM-read bytes/status/effects satisfy the separately proposed FR-ARCH-TRUEDRIVE-1541-002 criteria against the same native initial conditions. Direct image access cannot satisfy true-drive acceptance. | Tests: TEST-DRV-MOTOR-001, TEST-IEC-TIMING-001
- ac-03 | Datasette motor startup, delayed stop/cancellation, pulse delivery, sense-line wiring and recording on writable media satisfy the separately proposed FR-RUNTIME-TAPE-002 criteria for C64 and VIC-20. A metadata pulse read or in-memory recording count alone is insufficient. | Tests: TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001
- ac-04 | Each accepted media scenario retains fixture/firmware/resource hashes, mode, initial state, clock domain, operation sequence, ordered observations and first mismatch. Reusing a host-cycle deadline as a drive/tape clock assertion or comparing different media modes is rejected. | Tests: TEST-D64IO-001, TEST-DRV-MOTOR-001, TEST-IEC-TIMING-001, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001
- ac-05 | All preceding assertions first pass with independent discriminating mocks and expected negative controls, then the same inputs/assertions pass with real providers and the full applicable suites. Missing, skipped, failing or mock-only evidence leaves the criterion unsatisfied. | Tests: TEST-D64IO-001, TEST-DRV-MOTOR-001, TEST-IEC-TIMING-001, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001

### FR-BACKFILL-VIDEO-001
Kind: fr
Title: VIC-II RC, DMA and screen-memory checkpoints match immutable VICE
Field: body
Text: Managed VIC-II shall match the pinned VICE x64sc model at equivalent initial conditions and explicitly aligned cycle/phase checkpoints for RC/VC state, raster progression, sprite DMA, bus ownership and screen-memory access. Include the legacy five named variants without calling MOS8565 non-PAL. These checkpoints support visible-frame parity but do not alone establish full pixel/audio/workload lockstep, which remains mandatory under the wider plan and TR-CYCLE-001.
- ac-01 | On PAL MOS6569, compare RC, VC, VCBASE, VMLI, idle and bad-line state immediately before and after the native VC/RC update events and across bad-line/idle transitions, including RC=7 with bad_line both false and true and timed DEN/YSCROLL changes. | Tests: TEST-VIC-RC-001
- ac-02 | After identical stable PAL initial conditions, both native and managed raster position return to the same position after 19656 full cycles and each cycle counter advances by exactly that amount. Check last-cycle/last-line wrap and samples at frame-minus-one, frame and frame-plus-one. Explicitly document the counter width and phase normalization. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | For MOS6567R8, MOS8562, MOS6567R56A, MOS6572 and PAL MOS8565, compare the same sprite-3 DMA-active and bus-ownership observables at aligned model-specific cycle/phase boundaries. Include disabled/enabled sprite, Y match, next-line continuation, stop/release and bad-line overlap; a loose native line window plus an independent managed boolean does not pass. | Tests: TEST-VIC-CHECKPOINT-001
- ac-04 | With CPU and other memory writers controlled and identified, immediate screen-memory roundtrip and a PAL-frame c-access scenario compare both providers' full $0720-$07E7 fixture, relevant internal fetch effects and read/write direction. No emulation cycles may elapse during immediate roundtrip; matrix fetches must not issue screen-RAM writes, including writes of the existing value. | Tests: TEST-VIC-CHECKPOINT-001
- ac-05 | Keep all wider TR-CYCLE-001 criteria and visible-frame/lockstep gates mandatory. These checkpoint assertions must be proven with discriminating mocks first and identical real bindings later; historical nine/eleven-case claims, native-only checks, skips and mock-only runs cannot close this FR. | Tests: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001

### TR-VIC-EDGE-001
Kind: tr
Title: Paired PAL screen-memory frame checkpoint
Field: body
Text: The managed and pinned native PAL VIC-II shall perform equivalent controlled screen-memory frame scenarios. The 200-byte legacy fixture starts at $0400 + 20*40 = $0720 and ends at $07E7. Attribute memory effects by actor; no pixel-parity claim follows from a RAM-only checkpoint.
- ac-01 | Seed both providers with all 200 address-distinct bytes, record initial register/bank/raster/phase state, and compare every byte before and after exactly 19656 full PAL cycles with CPU and unrelated writers controlled. Native and managed use the same fixture and phase boundary. | Tests: TEST-VIC-CHECKPOINT-001
- ac-02 | Retain c-access activity evidence and actor/direction information sufficient to distinguish VIC reads/internal buffer updates from external RAM writes; detect a same-value write that a final-byte comparison would miss. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | Reject wrong start/end address, one omitted byte, one changed byte, one-cycle drift, absent c-access activity, wrong bank and a fixture corrupted identically on both sides. Compare to independently pinned expectations, not just mutual equality. | Tests: TEST-VIC-CHECKPOINT-001

### TR-VIC-EDGE-002
Kind: tr
Title: PAL raster and cycle-count frame periodicity
Field: body
Text: PAL MOS6569 raster progression shall match pinned native VICE with 63 full cycles per line and 312 lines per frame. Preserve exact cycle-count and raster-position checks while making initialization, phase and wrap semantics explicit.
- ac-01 | From a recorded nonzero raster position and separately from the last cycle of the last line, both providers match the pinned raster sequence at frame-minus-one, frame and frame-plus-one; only the exact 19656-cycle sample repeats the starting phase/position. | Tests: TEST-VIC-CHECKPOINT-001
- ac-02 | Each provider's counter delta is exactly 19656 full cycles for the frame interval, with explicit modular arithmetic if a counter wraps and no double-counting of half-cycle observations. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | Expected negative controls reject 62/64-cycle PAL lines, 311/313-line frames, off-by-one samples, phase swaps and a counter that freezes while raster wraps correctly. | Tests: TEST-VIC-CHECKPOINT-001

### TR-VIC-EDGE-003
Kind: tr
Title: VICE-ordered RC, VC, VCBASE, VMLI and idle transitions
Field: body
Text: Match vicii-cycle.c:541-563 and the selected model cycle flags. At the VC event, assign VC=VCBASE and VMLI=0, and RC=0 on a bad line. At the RC event, RC=7 first sets idle and captures VCBASE=VC; then non-idle or bad-line state increments RC modulo eight and clears idle. Preserve the native operation order and explicitly align managed observation timing.
- ac-01 | Before/after the PAL VC update event, assert VC, VCBASE, VMLI and RC for bad-line and non-bad-line states, using nonzero VC/VMLI fixtures that detect a missing reset or swapped assignment. | Tests: TEST-VIC-RC-001
- ac-02 | At the RC event, exhaust RC 0..7 crossed with idle true/false and bad_line true/false at the contract seam. Assert the entire resulting tuple, including RC=7/bad_line=true yielding captured VCBASE, wrapped RC=0 and idle=false. Classify naturally reachable real traces separately from seam-only transition fixtures. | Tests: TEST-VIC-RC-001
- ac-03 | Run reachable PAL multi-line traces covering bad-line entry, seven row advances, idle entry and exit, and timed DEN/YSCROLL changes. Pin allow-bad-lines and line-range prerequisites so YSCROLL equality alone is never assumed sufficient. | Tests: TEST-VIC-RC-001
- ac-04 | Sample immediately before/after both update events and on neighboring cycles. Document cycle numbering and tick entry/exit alignment; reject a correct value reported one phase/cycle late. | Tests: TEST-VIC-RC-001
- ac-05 | Negative controls independently corrupt each tuple member or operation order and are rejected inside a passing mock suite. The same assertion logic is used for real native/managed observations; absent native observability remains an unsatisfied gate, not inferred parity. | Tests: TEST-VIC-RC-001

### TR-VIC-EDGE-004
Kind: tr
Title: Model-specific sprite DMA and ownership checkpoints
Field: body
Text: Retain all five legacy model cases: MOS6567R8 65x263, MOS8562 65x263, MOS6567R56A 64x262, MOS6572 65x312 and PAL MOS8565 63x312. Compare DMA-active, BA/AEC or equivalent documented CPU-ownership observations separately at corresponding cycle/phase boundaries. Do not equate sprite DMA-active with CPU-cycle stolen.
- ac-01 | Resolve each native selector and managed model to the same pinned model identity, timing geometry and resources. Reject a fallback model, wrong PAL/NTSC classification or timing-table mismatch. | Tests: TEST-VIC-CHECKPOINT-001
- ac-02 | For sprite 3/Y=$50, retain a full aligned trace from before activation through next-line continuation and release, plus disabled-sprite and Y-nonmatch controls. Compare the same DMA-active bit and ownership fields independently, not merely any event near lines $4F-$51. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | Exercise no-bad-line and bad-line-overlap fixtures with explicit DEN, YSCROLL, allow-bad-lines and relevant register-write timing. Validate combined ownership and release against native rather than attributing every stolen cycle to sprite DMA. | Tests: TEST-VIC-CHECKPOINT-001
- ac-04 | Mock controls reject right line/wrong cycle, right cycle/wrong phase, wrong sprite, missing release, native-only event and substituted DMA-active-for-ownership signals. Real-binding receipts retain the formerly unused native cycle and first divergent field. | Tests: TEST-VIC-CHECKPOINT-001

### TR-VIC-EDGE-005
Kind: tr
Title: Matrix c-access has no external screen-RAM write effect
Field: body
Text: Model VICE vicii-fetch.c:76-100,192-201 as reads from selected memory and updates to VIC internal buffers. Do not confuse internal buffer writes with writes to external screen RAM, and do not claim other emulated actors are write-free.
- ac-01 | With pinned bank/address, bad-line and prefetch state, compare fetched byte/color and internal buffer index/effects against native at a defined checkpoint, including prefetched/delayed cases specified by the native source. | Tests: TEST-VIC-CHECKPOINT-001
- ac-02 | Capture attributable external-memory access direction so every matrix c-access is read-only with respect to screen RAM; include a negative control that writes the same value and would evade an unchanged-final-RAM check. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | Separately identify allowed CPU/other-device writes. Reject a fixture with uncontrolled writers or absent c-access execution as insufficient evidence; do not infer VIC write behavior solely from frame-end bytes. | Tests: TEST-VIC-CHECKPOINT-001

### TR-VIC-EDGE-006
Kind: tr
Title: Immediate screen-memory roundtrip with zero elapsed emulation time
Field: body
Text: Native and managed test adapters shall write and read the complete legacy screen-memory fixture at $0720-$07E7 without advancing emulated time. A successful adapter roundtrip proves address/data access only, not visible-frame or DMA parity.
- ac-01 | For all 200 addresses, the address-distinct fixture roundtrips exactly in both providers and matches independent expected bytes; boundary sentinels immediately outside the fixture remain unchanged. | Tests: TEST-VIC-CHECKPOINT-001
- ac-02 | Record cycle/phase and raster state before and after the immediate operation sequence; assert no elapsed emulation time and no hidden step call. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | Mock controls reject wrong address, truncated read, wrong byte, aliased neighboring addresses and a one-cycle hidden advance even when all returned bytes match. Preserve the same checks in real bindings. | Tests: TEST-VIC-CHECKPOINT-001

### TEST-VIC-CHECKPOINT-001
Kind: test
Title: Paired VIC-II checkpoint contract, mock and real-provider gates
Field: condition
Text: Cover every criterion of TR-VIC-EDGE-001,002,004,005,006 using independently expected state/data plus paired native/managed observations. Retain the original four frame/memory cases and five sprite-model cases as provenance, not a fixed completion denominator. Add assertion-level boundary and wrong-response coverage before implementation.
- ac-01 | A scenario ledger maps every linked TR and FR checkpoint AC to concrete case IDs, inputs, expected observables, source pins, mock positive controls, single-fault negative controls and later real bindings; no AC is represented only by a count or test name. | Tests: TEST-VIC-CHECKPOINT-001
- ac-02 | The unfiltered native-safe mock suite passes with zero failed/skipped/unexecuted cases and rejects every planned wrong response as an expected assertion failure inside a passing test, including unchanged-value writes and wrong-phase ownership. | Tests: TEST-VIC-CHECKPOINT-001
- ac-03 | The same cases, inputs, expected values, assertions and interaction checks run against real native and managed providers, with native state/data tied to the pinned immutable build and an explicit model/phase adapter. Missing read-only observability blocks acceptance; native source patches are forbidden. | Tests: TEST-VIC-CHECKPOINT-001
- ac-04 | Full applicable cumulative and real-emulator gates run with retained stdout/stderr, TRX/diagnostics, exact command/filter/configuration, counts and first divergence. Native-unavailable skips, a loose line-window match or native-only RAM tests are not passing parity. | Tests: TEST-VIC-CHECKPOINT-001
- ac-05 | Independent hostile review must evaluate assertions, source pins, phase alignment, requirements and the whole plan; acceptance requires AGREE, accuracy/completeness each at least 98 and zero FAIL/UNKNOWN, with complete durable request/response and MCP verdict readback. | Tests: TEST-VIC-CHECKPOINT-001

### TEST-VIC-RC-001
Kind: test
Title: Full RC/VC transition assertions and native-aligned traces
Field: condition
Text: Preserve the eleven historical RC/idle cases and extend to all TR-VIC-EDGE-003 observables, ordering and boundaries. Unit transition coverage and reachable integration traces must be distinguished. Validate assertions with discriminating mocks before production changes and bind the same assertions to real observations afterward.
- ac-01 | Enumerate every TR-VIC-EDGE-003 AC in a case ledger, including the RC/idle/bad-line cross product, nonzero VC/VCBASE/VMLI, adjacent event samples and reachable DEN/YSCROLL transitions. Tag seam-only states explicitly rather than inventing native reachable traces. | Tests: TEST-VIC-RC-001
- ac-02 | Prove each asserted tuple member, event timing and branch order discriminates correct and single-fault mock results, with all controls accepted/rejected as expected inside a fully green unfiltered native-safe suite. | Tests: TEST-VIC-RC-001
- ac-03 | Bind the same contract to production transition behavior and the reachable trace cases to aligned native/managed providers; record any unavailable field as a blocking observability gap. Real output must not become its own expected fixture. | Tests: TEST-VIC-RC-001
- ac-04 | Preserve full-suite and real trace receipts and independent AGREE with both scores at least 98 and zero FAIL/UNKNOWN before acceptance. Eleven source-declared cases or only RC/idle success cannot close untested VC/VCBASE/VMLI behavior. | Tests: TEST-VIC-RC-001

### TR-MEDIA-D64-001
Kind: tr
Title: D64 image-layer sector, status and persistence parity
Field: body
Text: At the image-sector layer, managed D64 behavior shall match pinned VICE disk_image_read_sector/disk_image_write_sector and fsimage_dxx read/write semantics through an explicit semantic adapter. Keep this separate from DOS/IEC true-drive execution. Define supported geometry and fixture variants from the current D64 product requirements before tests; do not silently add or remove image formats.
- ac-01 | For every valid sector of the standard 35-track 683-sector D64 fixture, compare all 256 bytes to independent address-derived expected bytes and native results. Exercise first/last sector in each zone, BAM 18/0 and legal/illegal neighboring track/sector boundaries. Inventory additional currently required D64 variants before fixing the full test denominator. | Tests: TEST-D64IO-001
- ac-02 | On disposable copies, write address-distinct patterns and compare immediate readback, fresh-open persisted bytes and whole-image differences to native. Only reference-observed data/error-map changes are accepted; validate error-map clearing on successful write and preserve native behavior when metadata persistence reports an error. | Tests: TEST-D64IO-001
- ac-03 | Cover read-only writes, out-of-range access, truncated input, injected read/write failures and error-map values, including hard errors that retain the caller buffer and soft errors that still transfer data. Compare semantic status plus actual buffer/image effects; do not assume every error implies atomic rollback. | Tests: TEST-D64IO-001
- ac-04 | The adapter records operation order, fixture hash, geometry, status mapping, bytes and flush/reopen outcomes without masking native distinctions. Wrong-byte, wrong-sector, false-success, lost-persistence and wrong-error-buffer controls must be rejected before real implementation begins. | Tests: TEST-D64IO-001

### TEST-D64IO-001
Kind: test
Title: D64 sector I/O discriminating contracts and native parity
Field: condition
Text: Add a dedicated D64 image-I/O contract suite for TR-MEDIA-D64-001 and FR-BACKFILL-MEDIA-001. Expected bytes and status/effect tuples derive from pinned native behavior and independently generated fixtures, not the managed implementation. Use temporary disposable copies for all writes, retaining original evidence fixtures.
- ac-01 | Map each D64 AC to data-driven valid/boundary/error cases, including all 683 standard-image sectors and every required image/error-map variant discovered in the approved geometry inventory. Give every case a fixed expected byte/status/effect tuple and source provenance before implementation. | Tests: TEST-D64IO-001
- ac-02 | Correct mocks pass every assertion; independently corrupted status, byte, sector offset, retained error buffer, error map, flush or fresh-open result is rejected by the corresponding assertion inside a green suite. No production code changes until this unfiltered mock gate is green. | Tests: TEST-D64IO-001
- ac-03 | Run the same cases/assertions through native and managed adapters on verified disposable inputs. Retain full-image before/after comparisons and fresh-open results, distinguish image-layer evidence from true-drive IEC evidence, and leave any unrepresented native error effect unsatisfied. | Tests: TEST-D64IO-001
- ac-04 | Retain chronological mock, implementation, real-provider and cumulative-suite evidence; require independent AGREE with accuracy/completeness at least 98 and zero FAIL/UNKNOWN before closing coverage. No suite is executed or claimed passing by this proposal. | Tests: TEST-D64IO-001

## Mapping deltas and gates

Preserve every existing mapping. For FR-BACKFILL-MEDIA-001, add TR-IEC-EDGE-001 and proposed TR-MEDIA-D64-001 to trIds, and TEST-IEC-TIMING-001 and proposed TEST-D64IO-001 to testIds. Preserve the video mapping, including TR-CYCLE-001. AC-to-TEST links above are intentional future obligations, not proof that the existing test files satisfy them.

1. Approval and recovery gate: review exact before/after fields, new IDs, AC and mapping additions. Re-read effective records immediately before any eventual write; stop on concurrent drift. Apply only the approved proposal through supported MCP methods, then verify complete server readback and regenerate canonical projections using supported tooling. No proposal is silently applied.
2. Fixture-definition gate: inventory all currently required D64 variants and native error/status classes, pin exact model/resource/ROM inputs and cycle/phase normalization, and produce the per-AC scenario ledger. This proposal does not claim those not-yet-produced fixtures exist. Resolve observability gaps using read-only external wrappers. If immutable VICE cannot expose the required evidence, stop and disclose that gap; do not weaken the AC or patch VICE.
3. Mock gate: implement or extend native-safe AC contract tests using independent fixtures. Correct mocks pass; each single-fault negative control is rejected inside the passing harness. Run the full applicable native-safe suite unfiltered. Retain existing real-emulator tests as mandatory subsequent gates. Archive any fixed-path compiler log before and immediately after each run.
4. Real implementation gate: only after the mock gate and applicable hostile gate, bind the identical assertions to native/managed implementations and remediate managed behavior. Retain actor, phase, model and first-mismatch evidence. Run full applicable cumulative and real-emulator suites, with zero failures or skips for acceptance.
5. Acceptance gate: independent Opus hostile review, full verdict and both scores at least 98, zero FAIL/UNKNOWN, durable JSONL plus MCP readback, then and only then consider satisfaction/closure. Current Opus retry hit provider quota and returned no verdict; this proposal joins the catch-up backlog. No new reviewer launch occurs here.

This is not a decision-complete implementation plan yet: fixture inventory, native observability, exact scenario ledger, reviewed validation commands and operator approval are still required before product/test implementation. Source inspection identifies coverage gaps; it does not prove runtime failure, runtime success, full visible-frame parity or completion of the original plan.
