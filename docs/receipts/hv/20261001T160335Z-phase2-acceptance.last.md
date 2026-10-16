2026-10-01T16:36:25Z - **DISAGREE. Phase 2 acceptance is not green.**

**New finding F59: replay loses the programmed VIC raster interrupt compare.** This fails assertion 2 and [FR-SNP-003 AC3](F:/GitHub/vice-sharp/docs/requirements/functional/FR-Snapshot.md:99).

I reproduced this twice against the saved production assemblies, without rebuilding or writing test artifacts:

1. On independent fresh PAL machines, install `LDA $D019; STA $C100; JMP $C000` at `$C000`.
2. Set PC=`$C000`, Flags=`$24`; write `$D011=$00`, `$D012=$20`, `$D019=$0F`, `$D01A=$01`.
3. Step 1000 cycles, record A-down at `(0,1000)`, and play into a fresh machine. Press A on the independent reference.
4. Both clocks are 1000 and rasters are 15, but the reference’s interrupt compare is **32**, while playback’s is **15**.
5. Step both another 1208 cycles. Both reach **cycle 2208, PC `$C006`, raster 35**. Reference `$D019` and CPU-written RAM `$C100` are **`$F1`**; playback has **`$70`**.

The source explains the divergence:

- [Mos6569.cs](F:/GitHub/vice-sharp/src/ViceSharp.Chips/VicIi/Mos6569.cs:2949) stores the programmed compare separately from the register array, which mirrors the live raster.
- [CaptureSnapshotState](F:/GitHub/vice-sharp/src/ViceSharp.Chips/VicIi/Mos6569.State.cs:386) omits that independent compare and its edge guard.
- [InjectSnapshotState](F:/GitHub/vice-sharp/src/ViceSharp.Chips/VicIi/Mos6569.State.cs:58) reconstructs them from the live-raster registers.
- [VICE’s snapshot implementation](F:/GitHub/vice-sharp/native/vice/vice/src/viciisc/vicii-snapshot.c:91) explicitly preserves `raster_irq_line` and `raster_irq_triggered`.

The existing pending/held IRQ tests capture on their programmed compare line. They pass while missing this future-line case.

The six assertions resolve as follows:

1. **PASS:** Coverage rerun returned **94 in scope, 0 missing, exit 0**. It uses Iteration and searches tests, not src.
2. **FAIL:** Both saved acceptance runs genuinely report **147 passed, 0 failed, 0 skipped**, with `TEST_EXIT=0` and `TEST2_EXIT=0`. F59 nevertheless disproves the required replay behavior.
3. **PASS:** The stated SID, CIA, processor-port, and GIF corrections are supported. Vice# intentionally omits x64sc’s random discharge jitter; requirement text remains unchanged.
4. **PASS:** Idle receipt reports **2 passed, 0 failed, 0 skipped, `EXIT:0`** and is newer than all three named sources. No additional idle run was required. Replay execution state remains outside the **65552-byte** serialized image.
5. **PASS:** Live MCP returns `PLAN-C64SET-001 done: false`. All 17 named requirements remain pending with zero satisfied criteria. All seven plan checkboxes remain open. No requirement, TODO, or provenance edit or Phase 3/4 start was found. `git diff --check` returned 0.
6. **PASS:** The ledger explicitly withholds recovered-set acceptance. Its earlier AGREE and the timing-only AGREE were excluded from this review’s evidence.

**All 58 specified prior regression scenarios pass their targeted checks.** I read their test bodies and product paths, and directly invoked **118 pure saved test methods: 118 passed, 0 failed**, including all 46 replay methods. This was in-memory invocation, not a fresh dotnet/xUnit gate run. File-writing scenarios were evaluated through source and the saved summaries, as authorized. The complete per-finding dispositions are in the MCP verdict.

Surface A fails assertion 2; surface C fails FR-SNP-003 AC3; surface D therefore fails the plan’s behavioral acceptance gate. These reflect one independently demonstrated defect, not three separate defects.

**Accuracy: 96. Completeness: 94.** The receipts and named corrections are supported, but the acceptance claim overstates replay correctness and coverage. Both scores fail the required 98 threshold.

Add-profile ran before the first git command or validation, following the skill and reading all dynamically discovered eligible files in full:

`2026-10-01T16:05:13Z - Profile loaded: read 21 global profile files.`

It was fully repeated after automatic compaction at `2026-10-01T16:24:41Z`.

The complete **15,609-character verdict**, including all 58 dispositions, F59, scores, JSON, and receipt paths, persisted through the [Codex MCP plugin](C:/Users/kingd/.codex/plugins/cache/mcpserver-codex-plugin/mcpserver/1.118.0/skills/session/SKILL.md). Direct server readback confirmed **status `completed` and exact ordinal equality** despite the wrapper’s unconfirmed-write errors.

Durable streams: [request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T160335Z-phase2-acceptance.request.jsonl), [response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T160335Z-phase2-acceptance.response.jsonl). The scratch response copy was absent during the active run; the parent must finish that copy.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":96,"completeness":94,"passCount":5,"failCount":1,"unknownCount":0,"failList":["Assertion 2 / F59 / FR-SNP-003 AC3: replay loses the programmed VIC raster interrupt compare. After capture at cycle 1000, compare 32 becomes 15; at cycle 2208 reference D019 and CPU-written C100 are F1, while playback has 70. The current Phase 2 behavioral acceptance and plan gate therefore fail."],"sessionId":"Codex-20261001T160705Z-plugin-session","requestId":"req-20261001T160735Z-phase2-hostile"}