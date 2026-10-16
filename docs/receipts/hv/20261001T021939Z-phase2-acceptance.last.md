2026-10-01T02:37:00Z - **DISAGREE. Phase 2 acceptance is not green.** Accuracy **94**, completeness **90**. Numbered assertions: **5 PASS, 1 FAIL, 0 UNKNOWN**. These scores reflect accurate green-run receipts but incomplete product behavior and test coverage.

Validator: Codex, gpt-6-astra, xhigh. I read all **21** specified profile files in full, excluding `add-profile.grok.md`, before validation. The older ledger-match and timing-only AGREE reviews are excluded as evidence.

Three product failures were reproduced against the current `tests/ViceSharp.TestHarness/bin/phase2-accept` assemblies in clean `pwsh.exe -NoProfile -NonInteractive` processes. These probes performed no product edits or file-based test runs.

1. **F1: Replay from a running baseline is not bit-exact.** Step a fresh PAL machine 1000 cycles, call `RecordKey(0, 1000, "A", true, recorded)`, then `Play(freshMachine)`. Both clocks report 1000, but reference Y is **40**, playback Y is **90**, and **75 RAM bytes differ**. At `$002B`, reference is `$FF`, playback is `$00`. `Play` restores the already-advanced CPU/RAM snapshot, sets its replay position to frame start, then executes those 1000 cycles again. The power-on-only acceptance stimuli miss this. This violates FR-SNP-003 acceptance criteria 2 and 3. See [RuntimeSnapshotStore.cs:190](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:190) and [AdvanceTo:437](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:437).

2. **F2: Snapshot serialization loses the processor-port state.** Capture a fresh machine, serialize and deserialize the snapshot entirely in memory, then restore it into another fresh machine. `$0000/$0001` changes from **`$2F/$37` to `$00/$17`**; `$E000` changes from KERNAL **`$85` to `$FF`**. `HasProcessorPort` changes from true to false. `VisibleIo`, direction, and data fields are explicitly not serialized, so restoration falls through to writing raw RAM through the CPU bus, including `$0000/$0001`. The earlier hidden-BASIC fix works for the in-memory object but does not survive the snapshot persistence path. VICE saves `mem_ram` and processor-port state separately. See [RuntimeSnapshot.cs:32](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshot.cs:32), [RuntimeSnapshotStore.cs:66](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:66), and [c64memsnapshot.c:217](F:/GitHub/vice-sharp/native/vice/vice/src/c64/c64memsnapshot.c:217).

3. **F3: CIA reset preserves a partial serial-input count.** Supply three CNT input edges, call `Reset()`, then supply bits **1,0,1,0,0,1,0,1**. SDR becomes **`$14`**, not **`$A5`**, and ICR bit 3 is set. `Reset` clears the input shifter but omits `_sdrInputEdges`; the fifth post-reset edge prematurely latches a byte. VICE resets `sr_bits` to zero. The existing fresh-CIA test passes but does not cover this reachable reset case. See [Mos6526.cs:225](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Cia/Mos6526.cs:225), [TriggerCntEdge:652](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Cia/Mos6526.cs:652), and [ciacore.c:635](F:/GitHub/vice-sharp/native/vice/vice/src/core/ciacore.c:635).

The six assertions resolve as follows:

- **1 PASS.** Independently executed `pwsh.exe -NoProfile -NonInteractive -File .\tools\check_phase_acceptance_coverage.ps1 -ThroughPhase 2`: **94 in scope, 0 missing, exit 0**. The script uses markdown `Iteration`, searches only eligible files under `tests/`, excludes bin/obj, and retains the exit-on-missing rule. The coverage fact still asserts exit 0. Both files predate this goal; no loosening was found.
- **2 FAIL.** Both saved Release summaries genuinely report **106 passed, 0 failed, 0 skipped, exit 0**. The saved runner includes every named class and uses the repository-contained `bin/phase2-accept` output. Source contains 106 facts including coverage. I additionally invoked **69 existing acceptance methods directly: 69 passed, 0 failed**. This was direct assertion execution, not a new `dotnet test` run. F1-F3 demonstrate that the green filter does not establish the claimed acceptance behavior.
- **3 PASS for the specified VICE conflicts.** The test-bit test checks deferred reSID reset rather than immediate all-ones. Resonance is checked after `$D417` writes and Tick on both models using the stored live gain; 8580 setting 15 remains above 6581 zero. The local VICE formulas agree. FR-CPU-002 and FR-PRF-005 text is unchanged by this working-tree diff.
- **4 PASS.** `idle-lockstep.txt` reports **2 passed, 0 failed, 0 skipped, exit 0**, timestamp **2026-09-30T22:58:33Z**. It is newer than Mos6502.cs (**21:40:49Z**), C64MemoryMap.cs (**22:51:26Z**), and ArchitectureBuilder.cs (**22:49:19Z**). The inspected First10000/First100000 facts require actual cycle counts and successful lockstep reports. Snapshot resume retains `ResetCycleDelay = 1`; power-on reset clears its stagger. Wolf64 mismatch was not used as this gate.
- **5 PASS under the explicitly authorized fallback.** Goal state is active/Executing, every goal-plan checkbox remains open, and the saved PLAN-C64SET-001 query says `done: false`. Git status/diff shows no requirement, TODO, or provenance edit and no Phase 3/4 start. `git diff --check` exits 0. Missing current HV paths in HANDOFF is not treated as a failure.
- **6 PASS.** The ledger diff closes items 2, 5, and 8 while explicitly stating that the recovered set is not accepted and goal 2 is not complete. This review does not promote that ledger close into Phase 2 acceptance.

All 17 prior findings were revisited:

1. SID replacement updates the bus target; the `$D418` test checks both replacement and old chip.
2. Fresh CIA input produces `$A5`, and output CNT goes high then low. F3 exposes the remaining reset defect.
3. Power-on seek now returns **19656 twice**. Raster, frame, oscillator, independent-reference playback, cycle stepping, and untruncated event serialization are present. F1 exposes the running-baseline failure.
4. The real settings host stages palette changes; malformed, one-row, and 17-row files are rejected. Sixteen four-token color rows are accepted.
5. Invalid hotkey lines produce diagnostics; session F12 dispatch calls machine reset.
6. Host tests check FLAC/MP3 bytes, JPEG/GIF signatures, GIF video, and the empty-format JPEG default. Product routes reach the corresponding encoders.
7. Both mouse paths use `(position & 0x7f) + 0x40`; the +10/-1 and IMousePort tests exercise resulting POT values.
8. ROM-bank monitor reads fixed KERNAL despite CPU RAM banking.
9. Watch rendering rereads memory and reorder moves the entry.
10. Resonance checks live post-write gains, not raw band-pass magnitudes.
11. An 8K image mounts as StandardBlock at `$A000`, separate from the other cartridge types.
12. Current goal/plan/MCP receipt remain undone under the specified fallback.
13. The direct hidden-BASIC playback test passes, and PlaybackMatches compares reset-replayed RAM/CPU images. F2 exposes loss of port state after serialization.
14. JPEG now converts each block to Y/Cb/Cr, performs DCT, and emits nonzero AC data for the split-block stimulus; spatial-detail and color tests compare output bytes.
15. PaletteRgb carries the custom colors through staging into the renderer; the color-0 test asserts BGRA **0,0,255,255**. Named selection sends an empty payload and restores the model palette.
16. Missing ffmpeg produces WAV-only catalog audio support and lists FLAC/MP3 plus muxed video as unsupported; catalog recognition remains separate.
17. Recording timestamps, FastForward clock advancement, custom 8x8 capture metadata, and real 1541 drive-memory dump all have behavioral stimuli and assertions.

Surfaces A, C, and D fail on assertion 2 and the unmet replay/serial behavior. The reviewed scope controls on surface B pass. No source, requirement, HANDOFF, plan, provenance, or TODO changes were made by this reviewer. No done-state change is authorized.

Durable review streams: [request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T021939Z-phase2-acceptance.request.jsonl) and [response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T021939Z-phase2-acceptance.response.jsonl). The parent must retain the completed response and its scratch copy after this review exits.

MCP receipt: session `Codex-20261001T010547Z-plugin-session`, request `req-20261001T022415Z-phase2-hostile`. The wrapper initially reported unconfirmed writes; canonical plugin readback subsequently confirmed this review turn. Canonical client.SessionLog.QueryAsync confirmed completed status and an exact full-verdict readback. The local stop-gate still reports block because its completion confirmation fails. This is a plugin process discrepancy the parent must record, not a product failure.

The live TODO query was rejected with `MCP tool call requires approval, but approval policy is never`. Assertion 5 uses the user-authorized disk evidence; that denial is not counted as a product failure or UNKNOWN.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":94,"completeness":90,"passCount":5,"failCount":1,"unknownCount":0,"failList":["Assertion 2 / F1: Running-baseline replay produces Y=90 instead of40 and75 RAM differences at the same reported cycle1000.","Assertion 2 / F2: Snapshot serialization drops processor-port state; restore changes $2F/$37 to $00/$17 and KERNAL $E000 from $85 to $FF.","Assertion 2 / F3: CIA reset retains partial serial-input count; eight A5 bits after reset produce SDR $14."],"sessionId":"Codex-20261001T010547Z-plugin-session","requestId":"req-20261001T022415Z-phase2-hostile"}