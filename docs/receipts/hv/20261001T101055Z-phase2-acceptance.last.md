2026-10-01T10:35:09Z - **DISAGREE. Phase 2 acceptance is not green.**

Accuracy: **94/100**. Completeness: **90/100**. Counts across assertions 1-6: **4 PASS, 2 FAIL, 0 UNKNOWN**. The saved test results are credible, but two replay counterexamples violate the acceptance criteria, and one VICE attribution is incorrect.

I read all 21 required profile files in full before validation, excluded add-profile.grok.md, and reloaded all 21 after automatic compaction. The ledger-match AGREE and timing-only 100/100 AGREE are withdrawn as evidence for this claim. Earlier DISAGREE verdicts also supplied no evidence of current success.

**F1: Replay loses the VIC raster interrupt pipeline and asserted interrupt source. High severity; assertion 2 and surfaces C/D FAIL.**

On independent fresh PAL machines, fill $C000-$C07F with NOP, set PC=$C000 and Flags=$20, write $D011=$00, $D012=$01, $D01A=$01, and step 57 cycles. Record A-down at (0,57), Play into a fresh machine, apply A-down to the reference, then step both eight cycles.

At cycle 65, the reference has PC=$C01E, S=$FD, RAM[$0100]=$C0, IRQ assert cycle 58, and IRQ delay 7. Playback has PC=$C020, S=$00, RAM[$0100]=$00, IRQ assert cycle long.MaxValue, and delay 0. Both read $D019=$F1. Capturing at cycle 58 instead also fails after eight more cycles: reference PC=$C01E, playback PC=$C021.

[C64ChipExecutionImage.Restore](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Snapshots/C64ChipExecutionImage.cs:122) reuses the limited snapshot injection. [Mos6569.State.cs](F:/GitHub/vice-sharp/src/ViceSharp.Chips/VicIi/Mos6569.State.cs:65) clears _irqAssertPending and does not restore the live VIC IRQ source. Native [vicii-snapshot.c](F:/GitHub/vice-sharp/native/vice/vice/src/viciisc/vicii-snapshot.c:282) restores that source. The new CIA interrupt facts do not cover VIC interrupts. Preserve the VIC interrupt execution state on the in-memory replay image and add this behavioral regression; keep AdoptElapsedCycles and serialized snapshot size unchanged.

**F2: Replay restores CIA2 registers without restoring VIC bank wiring when I/O was hidden. High severity; assertion 2 and surfaces C/D FAIL.**

On independent fresh machines, write $DD02=$03, $DD00=$03, RAM[$2000]=$42, RAM[$E000]=$24, then $0001=$33. Record A-down at (0,0), Play into a fresh machine, and apply the same key to the reference. Both clocks remain zero and both CIA2 port-A peeks are $7F. The reference selects VIC bank 0 and actual ReadVideoMemory($2000) returns $42; playback selects bank 3 and returns $24.

[RuntimeSnapshotStore](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:99) correctly skips banked-out I/O writes. However, [Mos6526.InjectSnapshotState](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Cia/Mos6526.cs:536) assigns port and DDR fields without updating the [C64MemoryMap bank callback](F:/GitHub/vice-sharp/src/ViceSharp.Core/C64MemoryMap.cs:183). Native [c64cia2.c](F:/GitHub/vice-sharp/native/vice/vice/src/c64/c64cia2.c:167) restores the derived bank through undump_ciapa. Restore the board wiring from the captured CIA output state without executing cycles. This violates FR-MEM-004 and [FR-SNP-003 acceptance criterion 3](F:/GitHub/vice-sharp/docs/requirements/functional/FR-Snapshot.md:99).

**F3: The fixed discharge threshold is an intentional departure from local x64sc. Medium severity; assertion 3 FAIL on accuracy.**

The requested deterministic behavior passes: direct probes of both bits 6 and 7 retained their driven values through 349999 ticks and cleared at tick 350000. However, native [c64memsc.c](F:/GitHub/vice-sharp/native/vice/vice/src/c64/c64memsc.c:422) adds lib_unsigned_rand(0, FALLOFF_RANDOM), where FALLOFF_RANDOM is 350000/5. This is the x64sc source selected by libc64sc_a_SOURCES. [Mos906114.cs](F:/GitHub/vice-sharp/src/ViceSharp.Chips/PLA/Mos906114.cs:27) explicitly documents omitting that jitter for determinism. The blanket assertion that this behavior follows x64sc is false. Correct the attribution; this finding does not request changing the fixed threshold, tests, or requirement text.

The six assertions resolve as follows:

1. **PASS.** Independent execution of .\tools\check_phase_acceptance_coverage.ps1 -ThroughPhase 2 returned exit 0, 94 in-scope requirements, and zero missing from tests/. The source uses markdown Iteration integers and tests-only mentions. No loosened coverage condition was found.
2. **FAIL.** Both saved Release summaries contain Passed 137, Failed 0, Skipped 0, Total 137 and end EXIT:0. The junction points to the stated phase2-f126gate output. I read the acceptance source and directly invoked 36 replay facts plus 72 additional parameterless facts from that assembly; all 108 passed. These results do not cover F1 or F2 and do not establish bit-exact replay.
3. **FAIL.** SID test-bit behavior, live resonance gain, full-byte DDR reads, CIA serial phase/no-retransmit behavior, and GIF audio omission match the inspected implementation and tests. F3 disproves the blanket x64sc attribution for discharge timing.
4. **PASS.** The idle receipt records two passed, zero failed/skipped, exit 0. Its 2026-10-01T10:08:16Z timestamp follows C64MemoryMap.cs at 10:05:25Z, ArchitectureBuilder.cs at 2026-09-30T22:49:19Z, and Mos6502.cs at 21:40:49Z. No additional idle run was demanded. AdoptElapsedCycles only validates and assigns the elapsed counter; IRQ restoration is separate. CPU execution-image restoration does not change Tick/Step. Serialized RuntimeSnapshot remains 16+65536 bytes, and replay-only CIA fields remain outside CiaSnapshotState.
5. **PASS using the expressly permitted fallback.** Live TODO access was blocked by approval policy never. The saved PLAN-C64SET-001 query says done=false; the current goal is active, all seven plan checkboxes remain open, and git status/diff show no requirement, TODO, or provenance edit. No Phase 3/4 start was found. git diff --check returned 0.
6. **PASS.** The ledger closes items 2, 5, and 8 while explicitly saying the recovered set is not accepted and Goal 2 is not complete. It is not evidence for this gate.

The 48 specified prior findings were rechecked against current test bodies and product code, with the saved summaries and the direct executions above. This included bus SID replacement; CIA input/reset/output phases; replay clocks, boundaries, periodic slots, hidden RAM, port latches/charge, CPU microstate, SID envelope/audio, CIA timing/TOD/serial state and held keys; palette validation and renderer propagation; real hotkey reset; encoder signatures and JPEG detail/color; ffmpeg availability; mouse movement; ROM/drive/watch monitor behavior; cartridge routing; and frame/cycle/palette metadata. File-writing and ffmpeg tests were source/receipt audited, not rerun in this read-only review. The two latest key/charge replay regressions passed. Passing those particular stimuli does not resolve the new counterexamples.

Evidence is in phase-coverage.txt, phase-acceptance.txt, phase-acceptance-2.txt, and idle-lockstep.txt under C:\Users\kingd\AppData\Local\Temp\grok-goal-0af1da0c5558\implementer. Direct probes ran through PowerShell.MCP using fresh pwsh.exe -NoProfile -NonInteractive processes and the existing gate assemblies. ViceSharp.TestHarness.dll SHA256: 1517F1D65AE9E6916FCA64112A715CB1782E795B8A443D66EFD801467C9ABCB3. No product, test, requirement, handoff, plan, provenance, or TODO edits were made by this review.

Durable review streams:

- [Request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T101055Z-phase2-acceptance.request.jsonl)
- [Response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T101055Z-phase2-acceptance.response.jsonl)

MCP session: Codex-20261001T074617Z-plugin-session. Turn: req-20261001T101518Z-phase2-48-findings. The wrapper reported durable-write-confirmation errors, including at beginTurn, despite subsequent server queries finding the turn. The complete verdict was persisted through the Codex plugin and verified by server readback, including every finding, scores, VERDICT JSON, and both JSONL paths. The turn was verified completed. The wrapper's confirmation errors remain a process diagnostic, not a missing reviewer turn or product failure.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":94,"completeness":90,"passCount":4,"failCount":2,"unknownCount":0,"failList":["F1 / assertion 2: Replay loses VIC raster IRQ pipeline/source; at cycle 65 reference PC=C01E S=FD RAM0100=C0, playback PC=C020 S=00 RAM0100=00.","F2 / assertion 2: Replay with IO hidden restores CIA2 registers but leaves VIC bank 3 instead of bank 0; VIC ReadVideoMemory(2000) returns 24 instead of 42.","F3 / assertion 3: Fixed 350000-cycle discharge matches the requested oracle but not local x64sc, which adds a random 0..70000-cycle offset."],"sessionId":"Codex-20261001T074617Z-plugin-session","requestId":"req-20261001T101518Z-phase2-48-findings"}
