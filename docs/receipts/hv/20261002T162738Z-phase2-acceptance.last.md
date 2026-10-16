2026-10-02 17:00 UTC - **DISAGREE. Phase 2 acceptance gate is not green.**

**Finding 102: seeking to a periodic snapshot applies a mouse delta twice.** This fails assertion 2 and FR-SNP-003 AC3/AC8.

I reproduced this against the current `phase2-f160link` assembly using a recording machine, an independently stepped reference, and a fresh playback machine:

1. Install `LDA $D419; STA $C100; JMP $C000` at `$C000` on recording and reference machines, set PC `$C000`, Flags `$24`, and `$D011=$00`.
2. Record A-down at frame 0, cycle 0; press A on the reference.
3. Step both machines 196560 PAL cycles, then call `ApplyMouseDelta(1, 10, 0)` on both.
4. Call `Seek(playback, 10)`. Both clocks are 196560. Reference POTX is `$4A`; playback POTX is `$54`.
5. Step reference and playback another 22 cycles. Both clocks are 196582 and both PCs are `$C000`. Reference RAM `$C100` is `$4A`; playback RAM `$C100` is `$54`.

`SaveReplay` still contains only two input events and one mouse line:

```text
inputCount=2
input key=A pressed=1 frame=0 cycle=0 codes=0A
input mouse port=1 dx=10 dy=0 frame=10 cycle=0
slot frame=0 snapshot=snapshot:slot:0
slot frame=10 snapshot=snapshot:slot:10
```

The cause is visible in the product code. [C64MemoryMap.cs](F:/GitHub/vice-sharp/src/ViceSharp.Core/C64MemoryMap.cs:761) updates the mouse position before invoking the recorder. [RuntimeSnapshotStore.cs](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:1147) then captures the periodic snapshot before inserting the event. The snapshot therefore already contains the delta, but its event cursor includes that same event. `Seek` restores the snapshot and applies the delta again. The new [ObservedKey_KeepsPeriodicSnapshot test](F:/GitHub/vice-sharp/tests/ViceSharp.TestHarness/FrSnp003AcceptanceTests.cs:3267) checks event/slot text, not playback through this boundary.

The six assertion results are:

- **1 PASS:** Live `pwsh -NoProfile -NonInteractive -File .\tools\check_phase_acceptance_coverage.ps1 -ThroughPhase 2`, invoked through PowerShell.MCP, exits 0: 94 in-scope requirements, zero missing from tests. Source uses markdown Iteration integers and tests-only references. The current coverage checks retain those constraints.
- **2 FAIL:** Both saved Release summaries report Passed 202, Failed 0, Skipped 0 and contain their required `TEST_EXIT=0` / `TEST2_EXIT=0` markers. Those results do not establish the behavioral acceptance claim: finding 102 disproves deterministic seeking.
- **3 PASS:** Current product/test source and local VICE source support the stated SID, CIA, palette and processor-port oracles. The fixed 350000-cycle discharge intentionally omits x64sc's random term; it is not attributed as identical x64sc behavior. GIF capture omits audio. No requirement-text change was found.
- **4 PASS:** The saved idle receipt reports Passed 2, Failed 0, Skipped 0 and ends `EXIT:0`. Its timestamp, `2026-10-02T16:18:19Z`, is newer than Mos6502.cs, C64MemoryMap.cs and ArchitectureBuilder.cs. `AdoptElapsedCycles` only assigns elapsed cycles. No additional idle run was required by the stated rule.
- **5 PASS:** Live MCP TODO query reports `PLAN-C64SET-001 done:false`. The plan checkboxes remain open. Git evidence shows no requirement/TODO/provenance completion edit or Phase 3/4 start. This proves only assertion 5, not the acceptance gate.
- **6 PASS:** The ledger closes items 2, 5 and 8 while explicitly leaving the recovered set unaccepted. Its close is separate from this gate. The older ledger-match and timing-only AGREE reviews are withdrawn as evidence here.

I directly executed **170 existing acceptance methods: 170 passed, zero failed**, including all 98 replay methods and 72 CPU, memory, SID, CIA, cartridge, input, monitor and hotkey methods. The prior replay regressions, including Ocean/Magic Desk banks and observed periodic key capture, pass those existing checks. I also reviewed the remaining palette, capture and UI test bodies and product paths against the saved summaries. I did not run a new full 202-test process or regenerate media artifacts. The retained summaries remain the evidence for those file-producing tests. All **704 workspace source-document hashes** checked in the seven Portable PDBs match current disk files, tying the executed assembly to current source.

Applicable surfaces A, C and D fail because deterministic replay and the plan's behavioral acceptance criterion remain unmet. No outstanding workspace-rule violation was found on surface B. No product, requirement, plan, HANDOFF, provenance or TODO edits were made. `git diff --check` exits 0.

**Accuracy: 96. Completeness: 94.** The receipt counts and most tested behavior are substantiated; the claim of a green gate is contradicted by a current-source memory divergence, and the periodic snapshot tests leave this boundary uncovered. Both scores are below the required 98.

The required add-profile skill ran through PowerShell.MCP using Show-TextFiles before git or validation, reading all 21 enumerated profile files excluding `add-profile*.md`. Required confirmation: `2026-10-02 16:29:00 UTC - Profile loaded: read 21 global profile files.` After automatic compaction, the complete skill and all 21 files were re-read, with confirmation at `2026-10-02 16:51:36 UTC`.

The complete verdict, finding, scores, JSON and receipt paths are stored through the [Codex MCP plugin session workflow](C:/Users/kingd/.codex/plugins/cache/mcpserver-codex-plugin/mcpserver/1.118.0/skills/session/SKILL.md). Server readback verifies the completed turn and full response. Session: `Codex-20261002T163120Z-plugin-session`; request: `req-20261002T163148Z-phase2-hostile`.

Current launch receipts: [request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261002T162738Z-phase2-acceptance.request.jsonl) and [response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261002T162738Z-phase2-acceptance.response.jsonl). The response stream is still being produced by this review; the parent must retain its final verdict and completion events.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":96,"completeness":94,"passCount":5,"failCount":1,"unknownCount":0,"failList":["Assertion 2 / finding 102: a mouse delta that creates the frame-10 periodic snapshot is applied twice by Seek. Reference POTX and RAM $C100 are $4A; playback is $54 at matched clock 196582 and PC $C000. FR-SNP-003 AC3 and AC8 fail."],"sessionId":"Codex-20261002T163120Z-plugin-session","requestId":"req-20261002T163148Z-phase2-hostile"}