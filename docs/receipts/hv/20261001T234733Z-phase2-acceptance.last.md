2026-10-02T00:20:45Z - **DISAGREE. Phase 2 acceptance is not green.** Accuracy: **96**. Completeness: **94**. Assertion counts: **5 PASS, 1 FAIL, 0 UNKNOWN**. Both scores fail the required 98 threshold because an ordinary external input still disappears from replay.

**Finding 72: keyboard changes during an open recording are lost.** This violates FR-SNP-003 AC1, AC3, and AC4, and the plan's behavioral acceptance criterion.

I reproduced this independently through the real host service:

1. On a fresh PAL machine, record A-down at frame 0, cycle 0.
2. Step 30 cycles and call `InputServiceHost.SetKeyStateAsync(B, true)`. The host returns Ok. Apply the same input to an independent reference.
3. Step 30 more cycles and record A-up at cycle 60. Play into a fresh machine.
4. `SaveReplay` contains **inputCount=2**, with only A-down and A-up. The required B-down event at cycle 30 is absent.
5. Select all keyboard columns with `$DC02=$FF`, `$DC03=$00`, `$DC00=$00`. At cycle 60, reference `$DC01=$EF`; playback `$DC01=$FF`.
6. Execute `LDA $DC01; STA $C100` (`AD 01 DC 8D 00 C1`) for eight cycles. Both clocks reach 68. Reference `$C100=$EF`; playback `$C100=$FF`.

The source explains the failure: [InputServiceHost.cs](F:/GitHub/vice-sharp/src/ViceSharp.Host.InProcess/Services/InputServiceHost.cs:167) routes normal keys to [C64MemoryMap.SetKeyState](F:/GitHub/vice-sharp/src/ViceSharp.Core/C64MemoryMap.cs:589), which updates the matrix without notifying a recorder. [RuntimeSnapshotStore.cs](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:842) attaches recording callbacks for joystick, mouse, pots, RESTORE, and light pen, but not keyboard changes. Existing keyboard tests explicitly call `RecordKey`, bypassing this gap. A direct-map probe and the separate host-service probe produced the same divergence.

The six assertions resolve as follows:

1. **PASS:** Live coverage returned **94 in scope, 0 missing, exit 0**. The script uses markdown Iteration integers and searches `tests/`; source-only mentions do not satisfy it. The current script and coverage test retain the required gate.
2. **FAIL:** The saved Release summaries really contain **160 passed, 0 failed, 0 skipped**, with `TEST_EXIT=0` and `TEST2_EXIT=0` inside their respective files. I also executed the full 18-class filter against `phase2-f141link`: **160 passed, exit 0**. Those results miss finding 72 and therefore do not prove the behavioral acceptance claim.
3. **PASS:** Inspected product and local VICE source support the corrected SID test-bit/resonance and CIA serial behavior. The full-byte direction register and deterministic 350000-cycle discharge remain. VICE's additional random delay is acknowledged as intentionally omitted. GIF omits audio. No requirement wording change was found in this diff.
4. **PASS:** The idle receipt contains **2 passed, 0 failed, 0 skipped, EXIT:0** and is newer than the three named CPU/bus sources. I did not demand another idle run. `AdoptElapsedCycles` only assigns elapsed time; replay execution state remains in memory, and serialized snapshots remain **16 + 65536 bytes**.
5. **PASS:** Live MCP reports `PLAN-C64SET-001.done=false`; all 17 named requirement rows remain pending with zero satisfied criteria. The goal is active and every plan checkbox is open. Git shows no requirement/TODO/provenance edit or Phase 3/4 start. `git diff --check` passed. I made no product, test, requirement, HANDOFF, plan, provenance, or TODO edits.
6. **PASS:** Ledger items 2, 5, and 8 are closed only within their narrower ledger claim. The diff explicitly says the recovered set is not accepted. I withdraw the older ledger-match and timing-only AGREE reviews as evidence for this Phase 2 claim.

The prescribed prior cases **1-71 were rechecked** against their test/product sources and the live acceptance run; their individual dispositions are recorded in full in the MCP turn. Additional probes retained **129/129 replay events** and verified **both** port bits 6 and 7 held `$C0` through cycle 349999 and cleared at 350000. These passes do not cure finding 72. This review used the existing Release assembly; it does not claim a fresh build or a full-solution test run.

Process receipt: add-profile ran first through **PowerShell.MCP**, using **Show-TextFiles**, with all **22 files** read in full (21 profile documents plus the skill-required `add-profile.md`). Required confirmation: `2026-10-01T23:49:00Z - Profile loaded: read 22 global profile files.` After platform compaction it was fully repeated, with confirmation `2026-10-02T00:07:26Z - Profile loaded: read 22 global profile files.` No Python or non-MCP profile reader was used.

The complete verdict, VERDICT JSON, prior-case dispositions, decisions, and receipt paths are stored through the Codex MCP plugin in session `Codex-20261001T235133Z-plugin-session`, request `req-20261001T235153Z-hostile-phase2`. Server readback is the persistence check.

Durable launch receipts: [request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T234733Z-phase2-acceptance.request.jsonl) and [response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T234733Z-phase2-acceptance.response.jsonl). Fresh test output: [160-test run](F:/GitHub/vice-sharp/.mcpServer/tmp/PowerShell.MCP.Output/pwsh_output_20261001_185528_647_bonfwnx4.q2q.txt).

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":96,"completeness":94,"passCount":5,"failCount":1,"unknownCount":0,"failList":["Assertion 2, finding 72: InputServiceHost.SetKeyStateAsync(B,true) at cycle 30 during an open recording returns Ok but creates no replay event. SaveReplay has 2 inputs instead of 3. At cycle 60 reference DC01=EF, playback FF; at cycle 68 reference C100=EF, playback FF. Violates FR-SNP-003 AC1, AC3, AC4."],"sessionId":"Codex-20261001T235133Z-plugin-session","requestId":"req-20261001T235153Z-hostile-phase2"}