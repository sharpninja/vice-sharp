2026-10-01 07:33:27 UTC

**DISAGREE: Phase 2 acceptance gate is not green.**

Accuracy: **96**. Completeness: **94**. Across assertions 1–6: **5 PASS, 1 FAIL, 0 UNKNOWN**. Assertion 2 contains two independently reproduced product failures.

Validator: **Codex / gpt-6-astra / xhigh**. All **21** specified profile files were read in full before validation. The ledger-match and timing-only AGREE reviews were excluded from acceptance evidence.

**FAIL F1: Replay loses CIA time-of-day values when character ROM covers I/O at capture.**

On independent fresh PAL machines:

1. Write `$DC0B=$01`, `$DC0A=$23`, `$DC09=$45`, `$DC08=$06`, then `$0001=$33`.
2. Call `RecordKey(0, 0, "A", true, recorded)` and `Play` into a fresh machine. Independently press A on the reference.
3. Restore `$0001=$37` on both.

Reference `$DC08` through `$DC0B`: `06,45,23,01`. Playback: `00,00,00,00`.

Executing `AD 08 DC / 8D 00 C1` at `$C000` for eight cycles proves a RAM divergence:

```text
referenceC100=06 playbackC100=00 referenceCycles=8 playbackCycles=8
```

The [CIA replay image](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Snapshots/C64ChipExecutionImage.cs:61) omits TOD state. [Restore](F:/GitHub/vice-sharp/src/ViceSharp.Core/Snapshots/RuntimeSnapshotStore.cs:87) correctly avoids writing the banked character-ROM window through I/O, but the separate chip image does not restore the missing clock. Local [VICE snapshot code](F:/GitHub/vice-sharp/native/vice/vice/src/core/ciacore.c:2259) preserves TOD registers, alarms, latch, and clock state.

**FAIL F2: Replay loses an active CIA serial output transfer.**

On independent fresh PAL machines:

1. Write `$DC04=$02`, `$DC05=$00`, `$DC0C=$A5`, `$DC0E=$41`.
2. Step four cycles, record A at `(0,4)`, and play into a fresh machine.
3. Independently press A on the reference.

Immediately after playback:

```text
referenceSP=True playbackSP=False referenceCNT=True playbackCNT=False
```

After 21 more cycles, an actual CPU-bus `Read($DC0D)` gives:

```text
referenceICR=09 playbackICR=01 referenceCycles=25 playbackCycles=25
```

Playback misses the serial-complete interrupt flag at the recorded point. The [replay image](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Snapshots/C64ChipExecutionImage.cs:119) restores serial **input** state but omits the output shifter, count, active-transfer flag, and pins used by [ShiftSerialDataOutputBit](F:/GitHub/vice-sharp/src/ViceSharp.Chips/Cia/Mos6526.cs:675). [VICE](F:/GitHub/vice-sharp/native/vice/vice/src/core/ciacore.c:2279) saves serial bit count and shifter state.

Both failures violate [FR-SNP-003 acceptance criterion 3](F:/GitHub/vice-sharp/docs/requirements/functional/FR-Snapshot.md:99). The missing CIA execution state needs preservation on the in-memory replay image and independent-reference regression tests. This does not require growing `CiaSnapshotState` or changing the serialized `16 + 65536` contract. I implemented no changes.

**Assertion dispositions**

1. **PASS.** Reran:
   `pwsh.exe -NoProfile -NonInteractive -File .\tools\check_phase_acceptance_coverage.ps1 -ThroughPhase 2`.
   Result: **94 in scope, 0 missing, exit 0**. Source uses markdown Iteration integers and references under `tests/`. The coverage checks retain the required behavior.
2. **FAIL.** Both saved Release summaries contain **Failed 0, Passed 125, Skipped 0**, ending `EXIT:0`. The named facts exercise product behavior, and the specified output junction exists. Those tests omit F1 and F2, so their passing count does not establish behavioral acceptance.
3. **PASS.** SID test-bit behavior follows inspected local reSID source. Resonance tests exercise the gain stored by `$D417` on running chips rather than the rejected raw-band-pass inequality. Requirement text remains unchanged.
4. **PASS.** The idle receipt contains **Passed 2, Failed 0, Skipped 0, IDLE_EXIT=0**. Its `2026-09-30 22:58:33 UTC` timestamp is newer than all three named CPU/bus sources. `AdoptElapsedCycles` only changes the elapsed counter; the CPU execution image does not alter `Tick` or `Step`. No additional idle run was required.
5. **PASS under the explicitly authorized fallback.** Git status/diff show no requirement, TODO, or provenance edit. The goal remains active; all seven plan checkboxes remain open. The [saved TODO query](F:/GitHub/vice-sharp/docs/receipts/hv/plan-c64set-001-done-false.json) records `done=false`. Live TODO access was rejected by approval policy `never`; this is not live-store confirmation. No Phase 3/4 start was found.
6. **PASS.** The ledger closes items 2, 5, and 8 while explicitly leaving the recovered set unaccepted. That closure and the older ledger AGREE were not used to accept this gate.

**Disposition of all 36 prior findings**

These passes cover the exact reported cases, not every possible replay state. I inspected product/test source and saved results for every case. I also directly invoked **61 existing test methods**, with **61 passes and 0 failures**, against the saved Release assemblies. This was not a new VSTest run. File-writing media and palette tests were assessed from source, implementation, and saved results.

- **1 PASS:** SID replacement receives CPU-bus writes.
- **2 PASS:** Serial input produces `$A5`; output CNT later returns low.
- **3 PASS:** Play/Seek advance clocks; repeated Seek and independent machine comparisons agree; SaveReplay iterates the complete event list.
- **4 PASS:** Real settings-host staging, frame-boundary apply, malformed/incorrect-count rejection, and extra-token acceptance.
- **5 PASS:** Invalid hotkey diagnostics and real session reset.
- **6 PASS:** Actual FLAC/MP3 encoder paths, JPEG/GIF screenshots, GIF video, and default screenshot format.
- **7 PASS:** 1351 encoding produces 74 for `+10` and decreases for `-1`, including `IMousePort`.
- **8 PASS:** Monitor ROM bank exposes KERNAL despite CPU RAM banking.
- **9 PASS:** Watch values refresh; reorder moves entries.
- **10 PASS:** Running-chip resonance gain reflects `$D417`; 8580 setting 15 remains above 6581 zero.
- **11 PASS:** Standard 8K BLK mounts at `$A000` as distinct `StandardBlock`.
- **12 PASS:** Current goal/plan remain open; authorized TODO fallback reports undone.
- **13 PASS:** RAM hidden under BASIC and processor-port state survive replay.
- **14 PASS:** JPEG uses per-block Y/Cb/Cr transforms and AC coefficients; spatial/color tests exceed header checks.
- **15 PASS:** File palette RGB reaches rendering through `PaletteRgb`; named palettes send an empty override.
- **16 PASS:** Missing explicit ffmpeg override restricts advertised audio to WAV.
- **17 PASS:** Recorded timestamps, FastForward cycles, palette metadata, and actual drive memory are exercised.
- **18 PASS:** Running-baseline CPU/RAM replay avoids duplicate cycles.
- **19 PASS:** Serialized processor-port latches survive; legacy loading remains supported.
- **20 PASS:** CIA reset clears partial serial-input count.
- **21 PASS:** Running-baseline VIC-II and SID oscillator state match.
- **22 PASS:** Running-baseline Seek executes only remaining frame cycles.
- **23 PASS:** Noise envelope/audio and subsequent oscillator continuation match.
- **24 PASS:** Replay continues an in-flight CPU instruction.
- **25 PASS:** Unfinished CIA serial **input** survives replay.
- **26 PASS:** Seek leaves input after the target boundary for subsequent Step.
- **27 PASS:** Missing ffmpeg removes muxed advertisements while retaining `bmpseq`; unset override permits PATH discovery.
- **28 PASS:** Processor-port data latch survives independently of pin reads.
- **29 PASS:** Periodic-slot Seek preserves earlier held keys.
- **30 PASS:** CIA timer-start pipeline survives.
- **31 PASS:** RAM hidden under character ROM survives.
- **32 PASS:** Backward Seek removes later keys.
- **33 PASS:** Running Timer B one-shot survives the snapshot heuristic.
- **34 PASS:** Seek rejects periodic slots captured after the requested boundary.
- **35 PASS:** Completed SDR survives banked-out I/O.
- **36 PASS:** Color RAM survives banked-out I/O.

The four named scratch receipts were read in full. Coverage and `git diff --check` both returned exit 0. Both new failures reproduced twice without rebuilding or changing product files. The probes used the saved assemblies:

```text
ViceSharp.Chips.dll SHA256
44286D7E3A70E62FF75CBB40DBBCDB3AA2A5088189EC4A98124607695C5D5154

ViceSharp.Core.dll SHA256
B9EF21ACB1D583E2AB914BBA3A554178DCBE1A7FDECF56DF53BD50C8DC1E1E03
```

Surface A fails assertion 2. Surface C fails FR-SNP-003 AC3. Surface D fails the plan’s behavioral acceptance criterion. No prohibited product/state mutation was observed. **Accuracy 96** reflects accurate receipt claims but an incorrect green conclusion. **Completeness 94** reflects omitted CIA state and regression coverage. Both are below 98.

The complete reproducer commands and outputs are in the parent [response JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T070955Z-phase2-acceptance.response.jsonl), paired with the [request JSONL](F:/GitHub/vice-sharp/docs/receipts/hv/20261001T070955Z-phase2-acceptance.request.jsonl). The parent must retain the finalized stream and required scratch copies.

MCP logging is **verified complete** through the [Codex MCP session workflow](C:/Users/kingd/.codex/plugins/cache/mcpserver-codex-plugin/mcpserver/1.117.0/skills/session/SKILL.md). Although mutation calls reported persistence-confirmation errors, a subsequent live plugin query returned `status=completed`, two actions, and a **12,044-character response exactly matching the submitted full verdict**, including its JSON and receipt paths. The plugin acknowledgement errors remain audit issues; there is no missing reviewer turn.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","accuracy":96,"completeness":94,"passCount":5,"failCount":1,"unknownCount":0,"failList":["Assertion 2 / FR-SNP-003 AC3: replay loses CIA TOD with I/O banked out; CPU stores $00 instead of $06 at $C100, both cycle 8.","Assertion 2 / FR-SNP-003 AC3: replay loses an active CIA serial output transfer; CPU bus ICR is $01 instead of $09, both cycle 25."],"sessionId":"Codex-20261001T071349Z-plugin-session","requestId":"req-20261001T071405Z-phase2-hostile"}