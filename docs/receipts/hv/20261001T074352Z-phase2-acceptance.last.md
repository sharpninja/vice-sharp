2026-10-01T08:07:22Z - **OverallVerdict: DISAGREE. Accuracy: 96. Completeness: 94.**

The saved 127-test runs are green, but two fresh probes disprove Phase 2 acceptance. Both violate iteration-2 **FR-SNP-003 AC3**, which requires identical memory and output throughout playback.

I read all 21 required profile files completely before validation, excluding `add-profile.grok.md`, and reloaded all 21 after automatic compaction. Earlier ledger, timing, and hostile verdicts were excluded as evidence of current acceptance.

**F1: Replay loses a held CIA serial input pin.**

Feed SP bits `1,0,1`, capture with `RecordKey(0,0,"A",true,recorded)`, and play into a fresh machine. Leave SP unchanged and issue five more CNT edges. Run `LDA $DC0C; STA $C100` for eight cycles on playback and an independent reference, with A pressed on both.

Observed:

- After restore: reference SP=`true`; playback SP=`false`.
- At cycle 8: reference SDR and RAM `$C100` are `$BF`; playback values are `$A0`.
- Both clocks are 8. Probe exit: **1**.

`CaptureSerialInput` preserves only the shifter and edge count. The existing partial-byte test supplies every subsequent SP bit, concealing the missing pin state. VICE explicitly snapshots `sp_in_state`.

Preserve the held input state in the in-memory replay image and add this continuation test without growing `CiaSnapshotState` or the serialized snapshot.

Sources: [Mos6526.cs:302](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Cia/Mos6526.cs:302), [replay capture:68](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Snapshots/C64ChipExecutionImage.cs:68), [existing test:310](F:/GitHub/vice-sharp/tests/ViceSharp.TestHarness/FrSnp003AcceptanceTests.cs:310), [VICE snapshot:2327](F:/GitHub/vice-sharp/native/vice/vice/src/core/ciacore.c:2327).

**F2: Replay loses accumulated interrupt timing.**

On independent fresh PAL machines:

1. Write 64 NOP bytes at `$C000`; set PC=`$C000`, Flags=`$20`.
2. Write `$DC04=$02`, `$DC05=$00`, `$DC0D=$81`, `$DC0E=$11`.
3. Step six cycles, record A-down at `(0,6)`, and play into a fresh machine.
4. Press A on the independent reference and step both four more cycles.

Observed at cycle 10:

- Reference: PC=`$C004`, S=`$FF`, RAM `$0100=$C0`.
- Playback: PC=`$C005`, S=`$00`, RAM `$0100=$00`.
- IRQ assertion cycle: reference **6**, playback **7**.
- IRQ delay count: reference **4**, playback **3**.

`AdoptElapsedCycles` restores only the elapsed counter. The replay image omits the clock’s interrupt timing state, so restored CPU and CIA fields cannot reproduce the execution. VICE interrupt snapshots save `irq_clk` and `irq_delay_cycles`.

Preserve the relevant interrupt execution state and add this continuation regression. The passing idle lockstep receipt does not exercise this defect.

Sources: [SystemClock.cs:40](F:/GitHub/vice-sharp/src/ViceSharp.Core/SystemClock.cs:40), [AdoptElapsedCycles:69](F:/GitHub/vice-sharp/src/ViceSharp.Core/SystemClock.cs:69), [IRQ sampling:237](F:/GitHub/vice-sharp/src/ViceSharp.Core/SystemClock.cs:237), [replay image:54](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Snapshots/C64ChipExecutionImage.cs:54), [VICE interrupt snapshot:385](F:/GitHub/vice-sharp/native/vice/vice/src/interrupt.c:385), [VICE delay snapshot:411](F:/GitHub/vice-sharp/native/vice/vice/src/interrupt.c:411).

Both defects violate [FR-SNP-003 AC3](F:/GitHub/vice-sharp/docs/requirements/functional/FR-Snapshot.md:99). Both were reproduced twice on the current binaries. No fixes were made.

**Assertions 1 through 6:**

1. **PASS.** Reran `pwsh.exe -NoProfile -NonInteractive -File .\tools\check_phase_acceptance_coverage.ps1 -ThroughPhase 2`: exit **0**, **94** in scope, **0** missing. The script uses markdown Iteration and searches `tests/`; Priority and `src/` mentions do not substitute. The coverage test invokes the real check.
2. **FAIL.** Both saved Release summaries report **Failed 0, Passed 127, Skipped 0**, ending `EXIT:0`. The junction and target were verified. Existing tests exercise the specified behaviors, but F1 and F2 disprove complete replay acceptance.
3. **PASS.** SID test-bit and resonance behavior follows the inspected VICE/reSID sources. Resonance uses the gain stored by `$D417`, not raw band-pass magnitude. Requirement text, including FR-CPU-002 ac-05 and FR-PRF-005, has no working-tree edit.
4. **PASS.** The idle receipt reports **Failed 0, Passed 2, Skipped 0, IDLE_EXIT=0**. Its `2026-09-30T22:58:33Z` timestamp is later than the three named CPU/bus sources. `AdoptElapsedCycles` only assigns the counter; the CPU execution image does not change Tick or Step. The additional CIA state remains in memory; serialized size remains **16+65536**. No new idle run was demanded.
5. **PASS under your specified evidence rule.** The goal is active, all seven plan checkboxes remain open, and the inspected diff contains no requirement, TODO, or provenance edit. The saved PLAN-C64SET-001 query has `done=false`. Live lookup was blocked by approval policy `never`; no live `done=true` result was obtained. Phase 3/4 were not started.
6. **PASS.** The ledger closes the narrower items 2, 5, and 8 and explicitly says the recovered set is not accepted. Its close and older AGREE do not establish this gate.

Counts use the six numbered assertions: **5 PASS, 1 FAIL, 0 UNKNOWN**. F1 and F2 are separate defects beneath assertion 2.

**Disposition of all 38 prior findings:**

- **1, 2, 5, 7–10: PASS** for SID bus replacement, CIA shifting/CNT pulse, hotkeys, 1351 movement, ROM-bank reads, live/reordered watches, and resonance gain.
- **3, 13, 18, 19, 21–24: PASS** for the specified replay timing, hidden BASIC RAM, running CPU/RAM/VIC/SID state, port serialization, noise/envelope/audio, and mid-instruction continuation cases.
- **20, 25, 26, 28–34: PASS** for serial reset/partial-byte state, seek boundaries, port data latch, held keys/rewind, CIA timer pipelines/one-shot, hidden character-ROM RAM, and rejecting late periodic slots.
- **35–38: PASS** for completed SDR, color RAM, TOD behind banked-out I/O, and serial output transfer.
- **4, 6, 11, 14–16, 27: PASS** on inspected product code, behavioral tests, and saved runs for palette staging/validation/rendering, actual media formats and JPEG detail/color, standard BLK mounting, and ffmpeg advertisements.
- **12: PASS** using current open goal/plan state, unchanged storage-related diff, and the authorized saved TODO evidence.
- **17: PASS** for recorded frame/cycle values, advancing FastForward, captured palette metadata, and real 1541 monitor memory.

Those passes cover the specified scenarios. They do not cover held-SP or pending-interrupt continuation.

**Verification and scope:**

I directly invoked **99 existing acceptance facts** from the current Release assembly in fresh PowerShell processes: **28 replay facts and 71 core facts, all passed**. This was not a new full `dotnet test` run. File-writing cases were assessed from source and the two saved 127-test runs, as authorized.

The additional probes executed real product behavior in memory and returned `CONFIRMED_PRODUCT_FAILURES:2`, `PROBE_EXIT:1`. `git diff --check` returned **0**. I made no product, requirement, plan, HANDOFF, provenance, or TODO-storage edits.

The loaded binaries are newer than the relevant replay sources. SHA-256:

- `ViceSharp.Chips.dll`: `9F06D68FB6686E4B3B089DD2FCD3BBCD051164381E2B7309E0773EFEF2D517F1`
- `ViceSharp.Core.dll`: `0ED210026EED1651F2A244267DB8738D9C07D6E80E0654229384121A099558DC`

All four supplied receipts were reread under `C:\Users\kingd\AppData\Local\Temp\grok-goal-0af1da0c5558\implementer\`.

Surface A fails assertion 2; surface C fails FR-SNP-003 AC3; surface D fails the plan’s behavioral acceptance and new ≥98 AGREE gate. Workspace constraints were respected, with the logging gap below. **Accuracy 96 and completeness 94 remain below acceptance because replay state and its coverage are incomplete.**

**MCP persistence gap:**

I attempted session opening, beginTurn, full-verdict updateTurn, completeTurn, and readback through the supported routes. **A durable reviewer turn was not confirmed.**

The full-verdict update and completion reported persistence failures, naming:

- `pending/20261001T080658Z-session_submit-3996.yaml`
- `pending/20261001T080720Z-session_submit-3eae.yaml`

These are under `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/`. Live readback returned: `MCP tool call requires approval, but approval policy is never`.

The parent must record this process gap and store the entire verdict. It is not an additional product failure on assertions 1–5. The session/request IDs below identify the attempted turn, not proven persistence.

The parent-created durable files exist:

- [Request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T074352Z-phase2-acceptance.request.jsonl)
- [Response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T074352Z-phase2-acceptance.response.jsonl)

The response stream was still being written during review; its final completion and the parent’s MCP write are not yet verified.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":96,"completeness":94,"passCount":5,"failCount":1,"unknownCount":0,"failList":[{"id":"F1","assertion":2,"requirement":"FR-SNP-003 AC3","finding":"Replay omits held CIA SerialDataIn: reference SDR and RAM C100 BF, playback A0, both at cycle 8."},{"id":"F2","assertion":2,"requirement":"FR-SNP-003 AC3","finding":"Replay omits accumulated interrupt timing: at cycle 10 reference PC C004, S FF, RAM 0100 C0; playback PC C005, S 00, RAM 0100 00."}],"sessionId":"Codex-20261001T074617Z-plugin-session","requestId":"req-20261001T074700Z-phase2-hostile"}
