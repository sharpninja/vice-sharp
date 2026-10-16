# ViceSharp Handoff

Generated local: 2026-10-04T12:41:21-05:00

## Diagnostic stdio mocks: 2026-10-08T14:28:11-05:00

F-12 now has 19 reusable mock-contract cases in `tests/ViceSharp.NativeClockProbe.Tests/StdioAdapterContractTests.cs`, with a supplied-provider assertion for identical later C binding. Correct mocks pass; independent wrong return/exit state, null/missing/extra/duplicated events, operation/stream/value changes and adjacent-order reversals are asserted to fail inside each passing test. `STDIO-ADAPTER-SLICE.md` was written first and explicitly separates this diagnostic protocol from native stdout retention and production integration.

Unfiltered Release runs passed clock 69/69 at 14:25:18, VSF 199/199 at 14:25:57 and oracle 87/87 at 14:26:16 (all 2026-10-08 -05:00), total 355/355; exit 0 and failed/notExecuted/aborted/error/timeout 0. Prior 50 clock cases are preserved. Every build/test retained unchanged native pre/post inventories; final eight baseline fields, 3847 stock source files, 99 link inputs and original gate executable match. Both current C compiler logs and source/preimage evidence are retained in `docs/receipts/native-vice/20261008T142142-0500-stdio-mock-contract/`.

The actual stdio adapter and production C code were not edited or newly bound. Real adapter binding, stdout retention and broken-pipe behavior remain mandatory and unrun; this is not full F-12 resolution or a phase advance. Repin the new test/plan and changed documentation before the same Opus max review resumes. Parent turn: `Codex-20261007T135626Z-plugin-session` / `req-20261008T191836Z-prompt-faf5`. Pending F-02 operator disposition, quota-blocked HV, and the entire original goal remain unchanged.

## Secret-exposure assessment: 2026-10-08T14:16:11-05:00

Draft F-02 remains open pending operator disposition and independent review. A read-only comparison found that the active credential label in Global `MEMORY-LAB-002` version 5 equals the local Tailscale credential's `keyId`, not its full `authKey`. Three selected retained tool outputs contain those identifiers. None of the 22 selected tool outputs contains the current full local Tailscale auth key or the current workspace API key. This is bounded evidence, not an audit of every output or a claim that credentials were never exposed elsewhere. No key or identifier values are included in the new assessment receipt.

The retained 07:15:14 marker output contains a redaction placeholder, so the historical key cannot be compared with the current key. The current marker records server startup at 09:13:49 and documents rotation on restart; historical-key invalidation remains an inference, not direct verification. No provider validity query, credential change, service restart, cloud operation, native-source edit or acceptance-state change was performed. The operator has been asked whether to leave credentials unchanged with this uncertainty recorded or prepare a rotation plan for approval. Do not interpret the unanswered question as authorization.

Sanitized evidence, exact output call IDs, selection limits and pending disposition: `docs/receipts/native-vice/20261008T141611-0500-secret-exposure-assessment/assessment.json` and sibling `README.md`. Parent turn: `Codex-20261007T135626Z-plugin-session` / `req-20261008T190255Z-prompt-b8db`. Preserve the original review finding and supply this additive evidence when the same Opus max review resumes. Upstream-required build generation remains approved in the separate pinned materialization; existing `native/vice` stays immutable.

## Routed triage evidence: 2026-10-08T13:58:02-05:00

The direct evidence missing from draft A21 is now available. The supported read-only `mcp__mcpserver__triage_status` tool, scoped to the documented routed store `F:\GitHub\McpServer`, returned both exact report IDs: `triage-report-70a8021a03c34270bacfde4f1c7e98c6` in group `triage-group-67df6e92fea64c1d`, and `triage-report-e85d4371f54c4751959e723de7b46b02` in group `triage-group-0420cc09f47afcbd`. Both responses have `status: grouped`, `originalWorkspacePath: F:\GitHub\vice-sharp`, and `workspacePath: F:\GitHub\McpServer`. This proves report existence/routing directly, not through parent session descriptions; grouped does not mean either underlying bug is resolved.

Exact primary response bodies and hashes: `docs/receipts/native-vice/20261008T135800-0500-routed-triage-readback/primary-report-readback.json`. Parent turn: `Codex-20261007T135626Z-plugin-session` / `req-20261008T185607Z-prompt-9c0d`. No triage report, server configuration/code, native source, historical review or acceptance state was modified. Supply this additive evidence to the same Opus max review and let the reviewer reassess A21; do not rewrite its original UNKNOWN or claim hostile acceptance. All original goal and real-emulator gates remain open.

## Current evidence corrections: 2026-10-08T13:50:54-05:00

Draft F-04/F-05/F-06 descriptions were corrected from direct receipt readback. All seven original gate stdout files are empty; neither the README nor the diagnostic adapter's header now claims retained native messages. Only the header comment changed, with bytes from the first `#include` onward identical. A separate object compiled successfully using the passing test runner's child environment convention. The first direct compile exited 1 with no object and an empty log; both attempts are retained, and the first cause is unproven. The existing gate executable and runtime behavior were not changed or rerun. Native stdout retention and mock-first stdio adapter coverage remain open, not waived.

Clock documentation now cites the native RESET message between the first and second `7,8` pairs in all three wrapped stdout logs, explicitly as correlation rather than causal/reset-event proof. Binary-monitor chronology now includes `binary-mock-01` at 09:28:37 (125/125: 77 prior plus 48 protocol mocks), the exact 13 additional boundary/error cases, `binary-mock-02` at 09:30:20 (137 passed, 1 fixture-parser failure), and corrected `binary-mock-03` at 09:31:04 (138/138). Original TRX/logs were preserved.

F-11's unretained pre-refactor clock-helper bytes and historical clock compile log remain missing evidence. Later passing runs/current copies do not reconstruct them. For F-14, live MCP confirmed 11 decision dialogs and zero matching actions across four completed historical turns. The supported session workflow treats completed turns as immutable. Eleven explicitly current corrective action/dialog pairs now carry the exact original decisions and source linkage on `req-20261008T184031Z-prompt-336a`; primary readback matched all 11 pairs. Full primary turn JSON comparisons confirm all four source turns remain unchanged. This is forward audit repair, not retroactive compliance or independent acceptance of the disposition.

Receipt index: `docs/receipts/native-vice/20261008T134300-0500-hv-evidence-corrections/README.md`. It retains pre-edit copies, exact original log/TRX hashes and excerpts, compile attempts, corrective primary readbacks and native integrity. All eight native baseline fields, 3847 original stock sources, 99 stock link inputs and the original gate probe executable remain unchanged. No test assertions, production logic, native source, goal/TODO/requirement acceptance, commit or push changed. The same quota-interrupted Opus review and all original real-emulator gates still require completion. Repin these changed documentation/comment inputs for that review; do not call the draft findings accepted or resolved by HV.

## Current fixture extraction: 2026-10-08T13:33:57-05:00

F-10's test-only extraction is now locally verified within the existing unaccepted Phase 1 gate. `tests/ViceSharp.OracleContract.Tests/OracleContractFixtures.cs` holds shared rows, input builders and independent expectations. `ContractAssertions` exposes supplied-provider assertions for decisions, boundary resolution, comparator results and audit call history. Boundary requests no longer contain a pre-resolved path; the returned path is asserted independently, with a new wrong-path control in each of the ten boundary cases. Real filesystem resolution is still unimplemented/unproven and remains a mandatory real-binding gate, not a satisfied requirement.

All three native-safe suites ran unfiltered in Release: oracle 87/87 at 13:29:50, VSF 199/199 at 13:29:58, clock 50/50 at 13:30:06 (all 2026-10-08 -05:00), total 336, exit 0 and failed/notExecuted/aborted/error/timeout 0. Every build/run retained matching native before/after inventories. All 87 TRX names and 85 ordered theory rows match the pre-refactor cases exactly. Seventeen static token comparisons preserve moved inputs/expectations/assertions and lifecycle controls. Original sources and ledgers are retained; current source copies and both current C compiler logs are copied and hash verified. Receipt index: `docs/receipts/native-vice/20261008T131300-0500-phase1-fixture-reuse/README.md`.

Current MCP turn is `req-20261008T181223Z-prompt-3ff5` in `Codex-20261007T135626Z-plugin-session`. These source and documentation changes occurred after the interrupted review, so repin and disclose them before resuming the same Opus max logical review. They do not remediate every finding or authorize phase advancement. No production/native edit, acceptance-state change, commit or push. The quota-blocked review state and whole original goal below remain unchanged. Upstream-required build generation remains confined to the separate pinned materialization, never the existing `native/vice` tree.

After compaction, reload the complete current `add-profile` skill and all fresh Global MCP memories before resuming. Latest reload finished 2026-10-08T13:25:44-05:00: 22 verified profile parts, 15 other Globals and one manifest, 38 total. Preserve local-only execution, mock-first TDD, native immutability, sortable local timestamps, and authorized full-content failsafe for failed saves/triage.

## Current mock remediation: 2026-10-08T13:05:32-05:00

Within the unaccepted Phase 1 mock gate, addressed draft F-07/F-08 with four independent lifecycle controls (wrong receipt and wrong postflight argument, each for unchanged/changed inventory) and an all-zero test-receipt rejection. Every new lifecycle case first accepts the correct mock transcript and then rejects exactly one corrupted argument. Original tests/assertions remain. Full native-safe runs: oracle 87/87, VSF 199/199, clock 50/50, total 336 passed, failed/notExecuted/aborted 0, all exit 0; native inventories unchanged across every build/run. This is local mock validation, not independent acceptance or real-emulator proof.

New F-09 scenario ledger maps 16 missing/unknown/gap PHI2/AEC cases to missing-observation ACs, and the other 18 only to TR-CYCLE-001 AC4. It contains all original 82 test names and five new cases. The original reviewed ledger is preserved byte-identical. Pre-edit files, live requirement readback, exact commands, TRX, logs and inventory receipts are indexed by `docs/receipts/native-vice/20261008T130200-0500-phase1-mock-remediation/README.md`. Current turn: `req-20261008T175952Z-prompt-f7fb` in the existing Codex session. This turn changed test/docs inputs after review; pin and disclose them on the next review.

F-10 reusable fixtures/provider binding remains open before real adapter work. The interrupted Opus review and all other findings below remain unaccepted. No production/native edits, phase advancement, satisfaction changes, commit or push. Continue remediation within the existing gate; do not infer another implementation waiver from the quota failure. The whole original goal and all real gates are unchanged.

## Latest review state: 2026-10-08T12:55:41-05:00

The noon local Opus 5.5 max catch-up stopped with exit 1 and HTTP 429 `usage_limit_reached`: provider message `You've hit your session limit`, reset `4:50pm (America/Chicago)`. PID 117120 has exited. Do not treat the old monitor or its 72% progress estimate as a running or accepted review. The monitor `collect-vicesharp-opus-catch-up-verdict` was deleted after terminal capture; no automatic retry was started.

Receipt prefix: `docs/receipts/hv/20261008-noon-catchup-opus.resume-02`. The full response has 1438 valid JSONL events, zero parse errors, 3276225 bytes, SHA256 `5CF0CC2C842576EF145CB23522D0C7FFE37ED321AA3C67608C6A3D4470CC607A`. Its last three tool calls contain 43 claims (37 PASS, 5 FAIL, 1 UNKNOWN), 16 findings, and a proposed DISAGREE with accuracy 96 and completeness 94. The claims/findings calls succeeded, but the verdict-construction call was rejected by PowerShell.MCP before execution. No finalized verdict or completed reviewer-turn readback was obtained. Preserve this distinction. `interrupted-draft-recovery.json` retains the full literal tool inputs/results and structured claims/findings, extracted with the PowerShell parser without executing the recorded commands. `terminal-summary.md` indexes the findings and next actions.

The entire 73039-character recovery artifact is saved in nine primary MCP dialog chunks on `Codex-20261007T135626Z-plugin-session` / `req-20261008T172748Z-prompt-d3fd`. Readback matched every chunk, allowing only CRLF-to-LF normalization; see `.partial-draft-primary-proof.json`. This proves parent preservation, not reviewer completion or acceptance. The first single large wrapper call failed before starting its child because serialized parameters exceeded the Windows command-line limit. Smaller supported wrapper calls succeeded. Triage accepted `triage-report-ad5cb20268a041dfb8d5fefa6f231228`; the supported failsafe report `20261008T175401Z-wrapper_argv_length-a2a6.yaml` is retained. Do not blindly drain existing queues.

Reviewer rerun TRX files show 82 + 199 + 50 = 331 native-safe unit tests passed, zero failed/notExecuted/aborted. Post-terminal rehash at 12:51:08 confirms all eight native baseline fields, all 3847 original materialized source files, 99 stock link inputs, and both stock executable hashes unchanged. Of 826 launch inputs, 825 are unchanged. The sole change is the original reviewer-progress JSONL: Opus appended its timestamp correction; hashing its original 1593-byte prefix reproduces the launch hash exactly. This handoff update occurs after that post-terminal check and is an additional disclosed pin change for any future review.

Unaccepted read-only source/binary findings are in sibling `.post-launch-source-addendum.json`, `.post-launch-interrupt-addendum.json`, and `.post-launch-reset-clones-addendum.json`. They correct optimized read-helper coverage and constrain actual interrupt, reset and stolen-cycle observation. They are not a completed bus decoder or proof of parity. No product code, test code, native source, acceptance state, commit or push changed during this recovery. The whole original goal and all real-emulator gates remain open. Do not advance implementation gates on the draft. Preserve the explicit noon waiver's end, local-only execution, same Opus max logical review, and all original evidence for an authorized follow-up.

After compaction, execute the full current `add-profile` skill and freshly read all Global MCP bodies again. The 12:48 reload verified 22 profile parts, 15 other Global records and one manifest, 38 total. Keep the user's latest mock-first, sortable-local-timestamp, immutable-native, and failsafe-for-triage corrections over older embedded defaults. Never print credentials or blindly replay hook-generated historical cancellations.

## Latest observation progress: 2026-10-08T11:49:07-05:00

Post-startup external CDB hardware attachment now works. The unchanged stock xvic produced actual packed fast-fetch before/after register results, 128 consecutive clock-write observations, and 20 actual normal-store argument observations. All 39 fast-fetch pairs have host RIP +3 and the same clock. This is partial diagnostic evidence, not a complete bus decoder or parity gate. Regular/dummy reads, interrupt stages, stolen cycles, deterministic start and full cycle alignment remain unproven. Zero dummy-store hits in this sample does not establish coverage or absence. CDB exited 0 through `q`; native target exit -1073740972 is debugger termination, not normal shutdown. All original source/link/stock-binary/native baseline checks remain unchanged.

Full receipts and exact limits: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/bus-runtime/README.md`. The fixture entry-watchpoint and recursive-trace failures are also retained and indexed in sibling `debugger-runtime/README.md`. Current MCP turn: `req-20261008T161608Z-prompt-c06c`; the prior control turn completed with primary readback. Include all diagnostics in noon local Opus catch-up, along with the complete earlier backlog. No native edits, completion-state changes, commit or push. Full original goal and all real-emulator gates remain active.

## Latest control progress: 2026-10-08T11:10:31-05:00

External callback-permission gate `tools/immutable-vice/clock-gate.c/.h` now publishes the actual clock, flushes and requires one lowercase `c` to return to native execution. EOF, quit, invalid input and I/O failures return distinct nonzero statuses. It was written after 19 new discriminating mock fixtures passed within a 31/31 clock suite and prior 199/199 plus 82/82 suites. The same fixtures bound to compiled C then passed within 50/50, with prior suites again 199/199 and 82/82. Cumulative 331 native-safe tests, zero failed/skipped/aborted; no full real-emulator acceptance claim.

Seven real separately linked stock-xvic probes completed without forced termination. Two runs granted 16 permissions and got 17 records each, with matching prefixes also matching the ungated callback prefix. Quit returned 67, EOF 66, invalid/uppercase/newline/NUL 68; every terminal case stopped without another callback. Across 39 withheld-permission intervals there was no next record, minimum measured 249.6824 ms. These are narrow post-callback controls, not all-device freeze or complete half-cycle/bus proof. All 3847 original source files, 99 stock link inputs, original xvic and dirty native baseline remained unchanged. Full evidence: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/gate-runtime/README.md`.

Remaining focus is actual native bus observation and unchanged semantics beyond these short prefixes. Fast fetch bypasses memory tables; do not fabricate addresses/data from PC or history. All previous snapshot, native-safe harness, full real-emulator and parity gates remain. Noon local Opus catch-up includes all clock/gate source, tests, helper refactor, docs, compiler and runtime receipts, and failed diagnostics. No acceptance/done-state/commit/push. Current MCP turn remains `req-20261008T151514Z-prompt-9817`. After compaction fully reload the current add-profile skill and all Global MCP memories before substantive work, preserving latest user corrections and the whole original goal.

## Latest progress: 2026-10-08T10:52:40-05:00

The external `vic_cycle` link wrapper now forwards the real native clock after calling the real callback exactly once. Mocks passed 6/6 with prior 199/199 and 82/82 suites before C implementation. Same-assertion compiled-C bindings then passed with 12/12 plus the same prior suites, 293 cumulative native-safe tests, zero failed/skipped/aborted. These are not full real-emulator acceptance tests. `tests/ViceSharp.NativeClockProbe.Tests/README.md` indexes the chronology, commands and retained failures.

The separate wrapped stock-xvic executable links 99 unchanged stock inputs plus external objects. Two `-limitcycles 100000` runs emitted 99998 actual callback-clock observations each, byte-identical stdout, including initial `7,8,7,8` reversal. All four short/long baseline/probe runs exited 1 and explicitly logged `cycle limit reached` in stdout; none timed out. The cycle limit is instruction-boundary termination, not deterministic per-cycle pause/resume. All 3847 original stock source files, the stock executable, link inputs and native dirty baseline remained unchanged. Exact link/run/hash receipts and limitations: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/clock-runtime/README.md`.

Next: continue actual native bus/control feasibility, with no inferred bus values, no filtered clock reversal, no native patch and no comparator exclusions. Address/data/RW, dummy/real, interrupt-stage, stolen-cycle and cycle-boundary pause/resume remain unproven. The later manual debugger interruption exposed a running CPU and clock but did not prove a hardware breakpoint hit or explain the prior failure. Noon Opus catch-up must review this entire wrapper slice and all earlier deferred work. No phase acceptance, done-state, commit or push.

Current MCP turn is `req-20261008T151514Z-prompt-9817` on `Codex-20261007T135626Z-plugin-session`. After recovery at 10:50, all 38 Global MCP records were read, including 22 hash-verified profile parts; marker hash remained unchanged. PowerShell.MCP console #16116 retains the correct workspace/cache/queue routing. Keep reloading the complete add-profile skill after compaction, not a cached summary. Earlier dated entries below remain historical evidence and are superseded only where stated here.

## Work in progress: 2026-10-08T09:56:27-05:00

The explicit binary-monitor acknowledgement gap is now covered by stock xvic and x64sc runtime receipts. Both acknowledged snapshot save/load and instruction steps; both returned native error `0x8f` for missing files, rejected by the production guard. Stock x64sc explicitly acknowledged loading the normalized legacy 1.2 fixture with PC `$5769`. Both probe processes exited 0. Latest full native-safe suites are 199/199 plus 82/82, with zero failed/skipped/aborted; the new 61 mock cases and prior suites passed before the codec implementation. See `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/binary-runtime/README.md` for exact receipts, retained failures, source findings and remaining gates.

Full native snapshot state still differs. VIA differences match the pinned writer/reader CABSTATE mask asymmetry; SID and drive field mappings are identified but their exact runtime causes remain unproven. Do not patch VICE, mask comparator differences, or confuse native-to-native differences with managed divergence. Whole-snapshot identity is diagnostic, not a newly added requirement. Deterministic cycle control, complete executed bus observations, reset-boundary xvic behavior and full real-emulator gates remain open. Continue the approved public-interface, external-process-control, then external interposition feasibility sequence. The noon catch-up HV includes this protocol slice and all prior deferred work. No phase acceptance is claimed.

2026-10-08T10:09:08-05:00: two local CDB hardware-breakpoint probes produced zero stops/hits, so neither is passing cycle-control evidence. The clock-write watchpoint and follow-up execute watchpoint were shown enabled, and runtime entry disassembly matched the pinned symbol, but the cause remains unknown. Both verified owned debugger/emulator pairs were stopped; no probe remains running. `debugger-runtime/README.md` under the Phase 2 receipt root retains exact launch commands, failure logs, source limitations and integrity readback. All 3847 original stock source files, stock xvic hash and the dirty native baseline remained unchanged. Do not conclude every external-control mechanism is impossible from these two failures. Include them in noon HV.

Current MCP turn: `Codex-20261007T135626Z-plugin-session` / `req-20261008T142219Z-prompt-bc0b`. After compaction reload the complete current add-profile skill and all Global MCP bodies again. PowerShell.MCP console #16116 must use the vice-sharp Codex cache and failsafe queue explicitly: inherited environment values had pointed to other workspaces. Only process-local routing was corrected, no global configuration. Preserve the historical quarantine and do not blindly drain stale snapshots. During marker reload an incomplete redaction exposed the repeated API key to a tool transcript; subsequent rendering replaces the exact key everywhere. Never copy that value into receipts or handoffs.

The 09:17 state below is retained as chronological evidence, superseded above for current counts and the acknowledgement next step.

### Earlier progress: 2026-10-08T09:17:17-05:00

Phase 2 is not complete. External adapter `src/ViceSharp.ViceCompatibility/X64ScSnapshotCompatibility.cs` now normalizes validated C64SC VSF 2.0 MAINCPU 1.2/1.3 bytes without native edits or file I/O. The mock suite passed 38/38 before implementation; the combined mock/implementation byte suite subsequently passed 77/77 and the prior oracle-contract suite passed 82/82, all full unfiltered Release runs with zero failed/skipped/aborted. These 159 tests are native-safe unit/byte evidence, not native resume or parity acceptance. The adapter is not yet wired into production snapshot paths. Preserve the failed first mock run and the corrected C64-to-C64SC fixture history. See `tests/ViceSharp.VsfCompatibility.Tests/README.md`.

Evidence root: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`. It includes mock/test TRX and logs, per-build/test native-integrity snapshots, source pins, copied-fixture provenance, requirement before/after readbacks, traceability output and stock build receipts. Traceability exit 0 still reports 83 unreferenced canonical IDs and 1011 noncanonical references; this is not a clean traceability gate.

Stock upstream build completed with exit 0, PID `36708`; see `stock-build-02-result.json` and retained stdout/stderr. The entry script is `build-stock-vice.sh`, which invokes the installed MSYS2 MINGW64 toolchain via explicit Git Bash. It runs upstream `autogen.sh`, `configure --enable-option-checking=fatal --enable-headlessui --disable-pdf-docs --disable-html-docs`, then `make -j4`, only in the separate pinned checkout below. The first configure failed for missing libcurl; the exact upstream-documented `mingw-w64-x86_64-curl` prerequisite was installed, exit 0, `pkg-config` reports 8.17.0. No VICE patch was applied. All 3847 original tracked files matched their pre-build hashes after build and runtime probes. The 2815 generated files have their own inventory. `native/vice` also remained unchanged. Compiler warnings were retained, not suppressed or claimed absent.

Stock runtime receipts are under `stock-runtime/`; read its `README.md` before repeating probes. Both xvic and x64sc report VICE 3.10 and execute native monitor save/load/step at BASIC-ready checkpoints, exit 0. Registers and clocks matched the uninterrupted one-instruction runs, but complete VSF bytes did not: xvic VIA/drive modules and x64sc SID/drive modules differ. The reset-boundary xvic load stalled and was terminated; preserve that failed edge case. The real external adapter normalized the retained MAINCPU 1.2 fixture, and stock x64sc recovered all seven checked CPU fields and executed another instruction, exit 0. This is runtime evidence for that fixture only, not full device, v1.3, managed, or lockstep parity. Text-monitor `undump` ignores its return status; use the public binary-monitor acknowledgement for fail-closed automation. Do not turn exit 0 alone into a snapshot acceptance gate.

Latest cumulative native-safe suites remain 159/159 passing, failed/skipped/aborted 0: `runtime-cumulative-vsf-02/result.json` (77) and `runtime-cumulative-oracle-03/result.json` (82). No production code changed in this runtime-probe increment. `InvokeNormalizer.cs.txt` is only a diagnostic byte-array invocation bridge to the already-tested production method, because PowerShell cannot box `ReadOnlySpan<byte>`; it contains no second normalizer. No probe process remains running.

Next: obtain explicit public binary-monitor snapshot result acknowledgements, diagnose the retained native save/resume differences and reset-boundary stall without changing VICE, then continue wrapper-only bus/timing observation feasibility. Source inspection shows instruction stepping in `monitor_binary.c` and a fast opcode-fetch path in `vic20cpu.c`; do not mislabel instruction history or memory peeks as complete per-cycle bus evidence. Required dummy/real and timing observations are still unproven. Noon Opus HV must include all code, test, docs, build-procedure approval, prerequisite work, runtime probes and unresolved failures recorded here. No phase acceptance or native-safe TestHarness integration is claimed.

Incidental MCP requirement timestamp anomaly accepted by triage as `triage-report-e85d4371f54c4751959e723de7b46b02`, group `triage-group-0420cc09f47afcbd`. Its supported failsafe record is retained at `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261008T134738Z-requirements_createdat_projection-17ec.yaml`; do not blindly replay stale session records while reconciling it. No MCP repair is in scope.

## Upstream build procedure approved: 2026-10-08T08:31:47-05:00

Operator correction on the rejected in-source-generation path: "Following the VICE Team's reqs is approved." Follow the pinned upstream build procedure in the separate materialization at `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`, including its required generated build files. Do not edit upstream source code, apply the legacy patch, or write anything under `native/vice`. Compare the original tracked-source byte inventory and commit before/after; record generated files separately, including producer commands and hashes. A nonempty generated-file inventory is now allowed in this separate checkout, not a source-code change. This supersedes the stricter zero-generated/untracked-file conditions in older plan text and review packets.

Primary `TR-VSFLOCKSTEP-RESUME-001` AC2 reflects this approved build procedure; readback confirms AC1 and description unchanged, both ACs unsatisfied, status pending. Before/after records: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/resume-tr-before-upstream-build-approval.json` and `resume-tr-after-upstream-build-approval.json`. Include this approval and changed evidence in noon HV.

## Active continuation and noon HV waiver: 2026-10-08T08:19:01-05:00

Operator directive: "Claude HV will fail until noon. Continue without HV until then, at which time have Claude HV everything it has missied." Continue the already-approved implementation through 2026-10-08T12:00:00-05:00 (America/Chicago) without inter-phase HV. Correct mocks, deliberately incorrect mock controls, full cumulative tests, immutable native VICE, and all real-emulator gates remain mandatory. This is a temporary review-timing waiver, not a passing verdict, done-state authorization, or approval of an otherwise unapproved plan.

The single-use current-thread heartbeat `catch-up-vicesharp-hv-at-noon` is ACTIVE for noon. Local Claude Code `claude-opus-5-5`, effort `max`, must review every missed code, test, documentation, decision, and gate change since accepted Phase 0. Include all work performed under this waiver. At noon, catch up HV before further phase advancement. Preserve prior packets and compare their 59 original input hashes with current files, disclose changes, then pin the entire catch-up scope. No artificial reviewer turn/time limits, cloud task, or substitute model.

Phase 1 now has a standalone native-safe mock project at `tests/ViceSharp.OracleContract.Tests`. Its full unfiltered run at 2026-10-08T07:33:34-05:00 executed 82 tests: 82 passed, 0 failed, 0 skipped, 0 aborted. Evidence: `docs/receipts/native-vice/20261008T070900-0500-phase1-mocks/full-tests-02-result.json`, matching TRX/log, `scenario-ac-ledger.json`, and `native-comparison.json`. Earlier compile and eight data-binding failures remain preserved, not erased. This is mock evidence only, not real VICE or VSF parity evidence. Phase 1 HV remains pending; no TODO/requirement/goal completion is claimed. The next approved work is Phase 2 wrapper-only feasibility in `docs/plans/PLAN-VICE-IMMUTABLE-001.md`.

The same logical Claude review session `cd19ecc2-b410-4b7c-8a8a-dca9bbb6b008` can be resumed at noon where supported. Receipt prefix: `docs/receipts/hv/20261008T073800-0500-phase1-oracle-contracts-opus`, including `.resume-01.*`. Resume 01 exited 1 with a provider session-limit error, not a verdict; no live reviewer remained at the last check. Recheck before launching to prevent duplicates. Preserve `.resume-01.metadata-correction.json`, which corrects the inherited `freshSession` flag without rewriting the original request.

The operator also explicitly answered "Yes, use failsafe for triage too." After a genuine primary session/triage failure, preserve the full supported failsafe record, verify it, label queued versus persisted accurately, and continue the active task. This supersedes the marker's stop-on-triage-failure instruction for this work. Do not repair MCP or spend the implementation window repeatedly retrying the same reporting failure. The historical quarantine is retained; do not blindly drain stale session snapshots.

This section supersedes conflicting current-state, review-timing, and next-step statements below. Historical acceptance and failure records remain intact. The original whole-goal plan remains `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`; its full scope is not reduced.

## Temporary HV reviewer override: 2026-10-08

Operator directive: "Grok is unavailable for HV. Use opus-5.5-max for now." Use the locally installed Claude Code CLI with model `claude-opus-5-5` and effort `max` for upcoming hostile validation, including remediation reviews. This temporarily supersedes all alternating-reviewer and Grok-only instructions below and in linked plans. Do not schedule a Grok review while this override applies. Retain the override until the operator changes it; do not infer restored availability.

Local execution, isolated reviewer state, no artificial turn/time limits, all applicable review surfaces, complete request/response JSONL, whole-verdict MCP persistence/readback (authorized full-content failsafe after genuine primary failure), and AGREE with both scores at least 98 and zero FAIL/UNKNOWN remain mandatory. This changes reviewer assignment only, not product requirements, gate scope, prior verdicts, or implementation permission.

## Resume update: 2026-10-07T17:48:03-05:00

Phase 0 of `docs/plans/PLAN-VICE-IMMUTABLE-001.md` is accepted. Local Grok 4.7 xhigh returned AGREE, accuracy 99, completeness 98, PASS 26, FAIL 0, UNKNOWN 0, and exited normally with code 0 at 17:47:27-05:00. Receipt prefix: `docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`. Primary completed turn: `GrokCode-20261007T222539Z-phase0-remediation-hv` / `req-20261007T222539Z-phase0-remediation-hv`. Parent independent readback proves the entire verdict prose and JSON are present in its 22855-character response. See `.parent-primary-proof.json`, `.parent-final-primary-query.json`, `.readback.json`, and `.exit.json`. All 59 pinned inputs were unchanged at review exit. The full response stream is 3988656 bytes, SHA256 `2132F05EBA4A7AB13AECE5112772EE4632E4CA9EF6481F4C28596E689132CA4B`.

The operator-approved `7dc1` quarantine is preserved, with zero active pending and seven quarantined records retained. MCP `PLAN-VICEORACLE-001` records only its Phase 0 task done; the other five tasks and the overall TODO remain open. Readback: `.accepted-todo-readback.json`. Next is the entire native-safe AC-mapped mock-contract project, all six groups, valid and deliberately incorrect mocks, full unfiltered suite green before production implementation. No mock project or production code has begun. Next reviewer is local Opus 5.5 max, then Grok. No real-emulator, native restoration, native VSF, or whole-goal completion is claimed. Do not relaunch the completed Grok review.

This update supersedes conflicting status and rotation in the historical notes below. Original verdicts and reviewed input snapshots are preserved. The review disclosed the parent's earlier raw-pwsh runner violation and mistaken hash-comparison shorthand; independent checks confirmed the corrected current evidence. The hook-created side turn with model `grok` is not the `grok-4.7` verdict turn and was not used as acceptance evidence.

## Historical resume update: 2026-10-07T17:11:10-05:00

Latest gate: local Opus 5.5 max returned DISAGREE (accuracy 94, completeness 85; PASS 22, FAIL 4, UNKNOWN 0), exited normally with code 0 after resuming the same logical review and CLI session `fa2d7384-f174-4f2e-8b3c-4a4cc6477893`. Full receipt prefix: `docs/receipts/hv/20261007T141100-0500-phi2-ac-remediation-opus`. Completed primary turn: `ClaudeCode-20261007T191324Z-plugin-session` / `req-20261007T191327Z-prompt-be46`. The parent's fresh primary query verified the complete 58553-character canonical verdict and structured verdict JSON, allowing only CRLF-to-LF normalization. Proof: `.parent-primary-proof.json`. The separate correction turn `ClaudeCode-20261007T203504Z-plugin-session` / `req-20261007T211928Z-turn-6c6f` is also completed and freshly read back. Do not relaunch the completed retry. Next review is local Grok 4.7 xhigh.

Opus's four findings: missing durable MCP tracking, missing/unknown access-kind and empty/gapped observation cases, unresolved pending-record reconciliation, and focused-filter exit wording. The continuation created and read back open MCP `PLAN-VICEORACLE-001`, including deferred comparator XMLDoc correction and real gate `REAL-C64-PHI2-AEC-001`; amended mock groups 1, 4 and 6; and required full applicable current-plus-prior suites with zero failed/skipped/aborted tests at every executable exit. The whole-goal plan no longer treats a named blocker as 100% completion and explicitly excludes UWP/Xbox implementation while preserving legacy requirements. Before-file copies and current receipts are under `docs/receipts/hv/20261007T163600-0500-phase0-opus-remediation/`. These edits await independent acceptance. No mock project or product implementation has begun.

Earlier Grok findings on incorrect PHI2/AEC mapping, unsupported `TR-CYCLE-001` closure, and intermediate steering hashes were verified by Opus. The TR remains pending with all seven ACs unchanged and unsatisfied. Four reconstructed historical full files remain read-only under `docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok-b7-reconstruction/`. Original review verdicts are unchanged.

Queue recovery: the supported bounded Codex drain replayed `978b` and `7058` (2 replayed, 0 failed/quarantined); the isolated Grok triage queue replayed `40a8` (1 replayed, 0 failed/quarantined). Originals were copied and hash-verified first; primary effects were read back. The operator subsequently answered Yes to preserving/quarantining the additional stale `20261007T185015Z-session_submit-7dc1.yaml`. Supported helper quarantine preserved matching read-only backup and quarantine copies plus a reason file, replayed/deleted zero, and left completed turn `req-20261007T184921Z-prompt-ccb7` deeply equal before/after. No active pending records remain; seven quarantine records are retained. Proof: `docs/receipts/hv/20261007T171002-0500-approved-7dc1-quarantine/verification.json`. Do not replay these preserved records. Reviewer record `2b93` already auto-drained during its completed review; do not recreate it. Phase 0 still awaits local Grok xhigh acceptance.

The operator's complete-content fallback authorization remains active. Read-only native baseline: `docs/receipts/native-vice/20261007T133100-0500-readonly-source-baseline.json`, 6374 files hashed, 12 tracked changes and 2527 untracked paths. Opus independently rehashed it with zero missing/extra/changed files. It is dirty-source preservation evidence, not clean-native acceptance.

Operator approval: "Yes, mocks first; retain all real gates." Phase 1 is the entire new native-safe mock-contract test project defined in `docs/plans/PLAN-VICE-IMMUTABLE-001.md`, with AC-mapped valid and discriminating incorrect mocks. Every existing real-emulator gate remains mandatory and unwaived. Do not run the current TestHarness/solution native-mutating build to validate these mocks. No test project or product code has been created for this phase yet.

The earlier operator-approved quarantine preserved five corrupt hook cancellation records byte-for-byte, with reason files and verified read-only backups, replayed zero, deleted zero, and left both historical primary sessions deeply equal. Receipt: `docs/receipts/hv/20261007T125315-0500-failsafe-quarantine/verification.json`. Seven total quarantined records now remain: those five, older separately retained Codex triage `2b86`, and newly approved stale snapshot `7dc1`. Retained quarantine evidence is not an active pending write and is not claimed replayed. Do not discard it or cancel historical review turns.

This update supersedes conflicting current-state statements in the historical sections below. The operator confirmed the F: relocation is complete and authorized normal development on `F:\GitHub\vice-sharp`; no further migration or partition work is needed. The original completion goal remains the Grok session plan at `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`. Phase 3 was subsequently authorized, and the live `PLAN-C64SET-001` TODO records narrowly reviewed Slices 3a and 3b. Do not ask again for an initial Phase 3 authorization or confuse those slices with complete xvic parity.

Operator correction, 2026-10-07: "RED tests are not acceptable. BDPv4 requires mocking data to prove tests work to green before starting code." Write AC-covering unit tests first and prove them green against valid mocked data before production implementation. Use deliberately incorrect mock data to prove the assertions detect defects, with negative tests asserting the expected rejection so the suite remains green. A failing test or an expected-red production run is not an acceptable gate. Only after the mock-validation gate passes may production implementation begin; then the full applicable suite for the current increment and all prior work must pass with zero failed and zero skipped tests before advancing. Mock evidence is not real VICE parity evidence. This corrects the existing process, not the product requirements, and supersedes conflicting historical instructions below.

The local Grok r2b review reported AGREE at 99/99 with PASS 22, FAIL 0, UNKNOWN 0, but explicitly accepted the now-rejected expected-red production step. Its receipt is historical evidence only for that sequence, not authorization to start implementation. Re-review the corrected active plans before advancing; preserve the original receipt unchanged.

Reviewer rotation directive, 2026-10-07: alternate local Grok `grok-4.7` at `xhigh` and local Claude Code `claude-opus-5-5` at `max` for successive HVs, including remediation re-reviews. The resumed Opus Phase 0 review has terminated. Next is Grok 4.7 xhigh, then Opus 5.5 max. Resuming an interrupted review retains that review's slot. Launch with an isolated session cache/failsafe root and accurate `MCP_SESSION_MODEL` before hooks run, so the new review cannot supersede an old review turn. Do not substitute models, execute in the cloud, spawn reviewer subagents, or impose artificial limits. Thresholds, full JSONL, complete verdict persistence through primary or the authorized fallback, and all A/B/C/D obligations are unchanged. This overrides historical Grok-only restrictions. The full goal plan contains both local launch shapes.

Operator fallback directive, 2026-10-07: "If you genuinely cannot save to MCP, you must use the fallback." Attempt the supported primary save first. On a genuine persistence failure, preserve the complete record, including the entire HV verdict where applicable, through the supported failsafe queue and continue under this authorization. Report the exact failure, queued path, and verification evidence; distinguish local retention from primary persistence and replay/read back when available. Do not bypass the plugin, edit storage directly, discard records, summarize away verdict content, or treat a queued requirement mutation as a confirmed primary update. This supersedes the older stop-only behavior for inability to save to MCP.

The next gate is Phase 0 recovery of `docs/plans/PLAN-VICE-IMMUTABLE-001.md`, before Slice 3c or native VSF implementation. Native VICE is the original immutable target, not a new requirement. The current checkout still contains the preserved preexisting 12 tracked modifications and 2,527 untracked paths; no restoration is authorized by this update. Dirty-native results remain diagnostic only.

Primary parent session: `Codex-20261007T135626Z-plugin-session`; this approval/quarantine/re-review turn is `req-20261007T220948Z-prompt-cca6`. Plan-remediation turn `req-20261007T213226Z-prompt-8b0b` and initial recovery turn `req-20261007T135720Z-turn-866a` are historical, not active turns to reuse. The initial recovery's supported drain replayed 4 records without failures, and primary readback verified the approved `TR-VSFLOCKSTEP-RESUME-001` body and both structured AC corrections, pending and unsatisfied. Evidence: `docs/receipts/native-vice/20261007T092700-0500-phase0-primary-recovery/`.

The historical immutable-plan HV primary turn contains its full prose and scores but omits the final structured verdict JSON. Fresh local Grok review is required; no Phase 0 completion or later phase is claimed. Traceability diagnostic: 235 canonical IDs, 149 referenced, 86 missing references, 1,011 noncanonical references, command exit 0. Do not describe that as a clean traceability gate. No product tests or builds were run during recovery.

Standing continuity: follow explicit scope, environment, sequence, tools, and approval boundaries literally; surface any inability before substituting an action. After compaction execute the complete `C:\Users\kingd\.codex\skills\add-profile\SKILL.md` and all required profile reads before substantive work. Do not voluntarily compact early. Use sortable local timestamps, PowerShell.MCP, local execution, and the alternating local HV reviewers above with no turn cap or artificial timeout. Native VICE must never be edited to make managed code pass.

This file replaces the 2026-09-28 handoff. That older file said recovery had not been applied and that product work was waiting on a plan. Both statements are stale.

## 2026-10-03 Native VICE snapshot correction

The operator made native VICE snapshot capability mandatory and non-negotiable, then clarified that VICE VSF has been the actual ViceSharp snapshot-format requirement since day one. It is the canonical default, not secondary or opt-in functionality. Do not claim Phase 2 complete until every normal public and desktop snapshot path uses native x64sc C64 or xvic VIC-20 `.vsf`, and load, save, bidirectional round trip, and deterministic continuation satisfy `FR-SNP-005`, `TR-LOCKSTEP-VSF-001`, and `TEST-SNP-002`.

Managed VSF resume support already exists and must be preserved:

- `LockstepValidator.RunFromSnapshot` loads native `.vsf` state and starts managed/native comparison.
- `LockstepValidator.StageManagedFromNative` transfers RAM, written 6510 processor-port direction/data, CPU architectural registers, VIC-II state, and both CIAs into the managed C64. It does not import the exported native opcode/micro-cycle, BA-low, pending-interrupt, IRQ/NMI clock, or interrupt-delay fields. `InjectSnapshotResumeState` resets in-flight execution, so this is a measured first-cycle reconstruction, not complete native pipeline transfer.
- `Mos6502.InjectSnapshotResumeState`, `Mos6569.InjectSnapshotState`, and `Mos6526.InjectSnapshotState` are production chip hooks used by that path.
- `IViceNative`, `ViceNative`, and `ViceNative.Xvic` already expose native snapshot read and write operations.

The gap is not the absence of managed VSF support. The gap is that the existing bridge remains in the test harness, is incomplete for all represented machine and peripheral state, lacks complete xvic staging and managed-to-native save transfer, is not exposed by `SnapshotServiceHost`, and was displaced by proprietary formats as the normal capture, file, quick-slot, and named-slot behavior.

The corrected plan is `docs/plans/PLAN-VSFLOCKSTEP-001.md`. It promotes and completes the existing bridge, keeps native VICE as the version-specific VSF reader and writer, and restores `.vsf` as the canonical default across supported public and Avalonia desktop snapshot paths. It does not propose a replacement managed VSF codec. The two existing proprietary readers move behind the explicit protobuf `ImportLegacySnapshot` operation for one-time migration only; normal restore rejects them, no capture path emits them, and replay remains separate. The plan locks closed model selectors, typed immutable state, both native-DLL lease ABIs, exact current/legacy VSF framing, transactional load/save, the Avalonia file/quick/ten-slot surface, fixture/toolchain/ROM inputs, cumulative BDPv4 gates, and post-publish `win-x64` MSI staging/install proof. No native snapshot product implementation has started under it.

The first 2026-10-03 plan hostile review was cancelled after this canonical-format correction. `docs/receipts/hv/20261003T131341Z-vsf-plan-corrected.response.jsonl` has 61 events and no `turn.completed` event. It is an incomplete receipt and cannot authorize Gate 0 or any done-state change.

The next completed review is `docs/receipts/hv/20261003T133142Z-vsf-canonical-plan.response.jsonl`: DISAGREE, accuracy 94, completeness 82, pass 14, fail 7, unknown 0. It found: an overstatement of current CPU pipeline transfer; missing interface/ownership/state/error contracts; non-executable xvic toolchain and fixture decisions; missing red/mock/cumulative phase stops; unresolved native singleton, cancellation, locking, session-change, and rollback-failure behavior; an impossible checksum-corruption promise and duplicate-module gap; and unspecified runtime packaging/install proof.

The fresh installed-local-Grok review is `docs/receipts/hv/20261003T144113Z-vsf-canonical-plan-local-grok.verdict.md`: DISAGREE, accuracy 89, completeness 84, pass 4, fail 8, unknown 7. It independently passed the CPU-baseline correction but found incomplete protobuf/import and desktop-slot decisions, incomplete legacy VSF framing rules, no exact ROM allowlist binding, an unrelated quarantined parity exit, ambiguous per-DLL singleton behavior, incorrect pre-publish native staging order, stale generated mappings, and missing independent evidence for the fixture, toolchain, MCP readback, selectors, and PE closure. The plan and canonical/live requirements now address every numbered item, and the supporting local evidence is retained in `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`. No finding is considered closed until another fresh review agrees.

MCP readback remains authoritative and pending. `FR-VSFLOCKSTEP-001` identifies the supplied fixture's exact 24 modules and SHA-256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, replacing the incorrect 16-module claim. `FR-SNP-005` has 9 acceptance criteria, `FR-VSFLOCKSTEP-001` 3, `TR-LOCKSTEP-VSF-001` 7, `TR-VSFLOCKSTEP-RESUME-001` 2, `TEST-SNP-002` 8, and `TEST-VSFLOCKSTEP-001` 1. All six are pending at `layer-1`, all 30 criteria are unsatisfied, and both FRs map to both TRs and both TESTs. Readback is retained in `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json`.

Recovered legacy records stay active and pending: `FR-VSFLOCKSTEP-001`, `TR-VSFLOCKSTEP-RESUME-001`, and `TEST-VSFLOCKSTEP-001`. They are mapped into the full-capability requirement alongside `TR-LOCKSTEP-VSF-001` and `TEST-SNP-002`. The local MCP wiki projection was regenerated at `2026-10-03T15:16:14.1534405+00:00` and now shows both four-link FR mappings. No legacy requirement was retired.

The FR-SNP-003 cartridge replay remediation received a narrow hostile AGREE at 2026-10-03T12:45:57Z: accuracy 99, completeness 99, pass 18, fail 0, unknown 0. Thread `01a0fdf8-fa4e-74f1-8b53-676b26f748d4`; MCP session `Codex-20261002T175731Z-plugin-session`; request `req-20261003T122054Z-remediation-hv`; request and response streams are `docs/receipts/hv/20261003T121540Z-phase2-fr-snp003-remediation.request.jsonl` and `.response.jsonl`. This proves only cartridge replay value preservation. It does not prove native VSF compatibility and cannot exit Phase 2.

## Checkpoint

Commit `c364e73037f646a7c5d0005c931fb689f665ce9f` is on `origin/main` (`4dfdde5..c364e73`). Message: `docs(receipts): record requirement-set audit checkpoint`. It contains 699 files: the audit receipts, the handoff text at that commit, and `docs/requirements/oracles/vic20-xvic-manifest.json`.

The latest hostile review agreed that the ledger matches the files. It did not accept the recovered requirement set and it did not complete goal 2. Codex `gpt-6-astra` at effort `xhigh`, 2026-09-30T17:02:03Z, thread `01a0f33d-b297-7663-87e5-c83fea36b10c`, accuracy 99, completeness 99, pass 12, fail 0, unknown 0. Receipt: `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`.

Resume here:

- Item 2 is closed. The operator said to replicate VICE behavior. `FR-CPU-002` ac-05 now says each clock phase advances once and the CPU fetch, decode, and execute sub-cycle state at that phase matches VICE x64sc. The requirement status is still pending.
- Item 5 is closed. The operator rule is to do it the same way as VICE. `FR-PRF-005` no longer allows a stub or silent sound. April sentences that disagree with x64sc were not restored.
- Item 8 is closed. `provenance-accepted-20260930.json` is a receipt of what was done. It stays unchanged: 645 empty old hashes and 110 empty source commits.
- Still open: items 1, 3, 6, 7, 10, and 11. Item 7 stays open because the committed manifest still uses KERNAL ROM filenames and existing reset-cycle constants. Committing the file did not create named-program measurements.
- Closed: items 2, 4, 5, 8, and 9, each for the claim recorded in the ledger.
- At this checkpoint, Phase 3 and Phase 4 still required an explicit user sentence to start. Phase 4's VIC-20 settings-visibility slice was implemented later; Phase 3 still requires an explicit start sentence.
- Do not mark `PLAN-C64SET-001`, any requirement, or any TODO done from the 99/99 ledger agreement.
- The phase-exit gate below has not exited. Script exit 0 only means the 17 ids are named under `tests/`.
- This handoff is newer than `c364e73` and is not in that commit.

## Phase-exit gate

BDPv4 phase number is the requirement `Iteration:` field. Priority P0, P1, and P2 is severity and is not a phase. The operator order "Make it so" is this gate. Phase 2 has not exited, and Phase 3 xvic parity has not started. The Phase 4 VIC-20 settings-visibility behavior was implemented later and has only its narrow focused gate; it does not exit Phase 2 or prove xvic parity.

`tools/check_phase_acceptance_coverage.ps1 -ThroughPhase 2` counts a section when its `##` heading is an FR, TR, or TEST id, `Iteration:` is 0, 1, or 2, and `### Acceptance Criteria` has at least one numbered or bullet criterion. A hit counts only under `tests/` in `.cs` and `.md` files. A mention in `src/` does not count.

Before the acceptance files existed, that command exited 1 on 2026-09-30: 94 requirements in scope, 17 missing from `tests/`, 101 criteria.

- Phase 0: `FR-CPU-004`, `FR-MEM-006` (13 criteria).
- Phase 1: `FR-MEM-004`, `FR-SID-004`, `FR-SID-009`, `FR-UI-004` (21).
- Phase 2: `FR-CFG-003`, `FR-CFG-004`, `FR-CIA-005`, `FR-CRT-002`, `FR-INP-003`, `FR-MED-005`, `FR-MON-005`, `FR-MON-006`, `FR-SID-005`, `FR-SNP-003`, `FR-VIC20-005` (67).

`tests/ViceSharp.TestHarness/PhaseAcceptanceCoverageTests.cs` fact `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases` expects the script to exit 0. Do not loosen that fact.

Re-run during this handoff update, local 2026-09-30 14:58:09: the script exited 0. In-scope requirements: 94. Missing from `tests/`: 0. Each of the 17 ids is named under `tests/`. The acceptance facts are not all green, and the phase has not exited.

The gate files are untracked and are not in `c364e73`:

- `tools/check_phase_acceptance_coverage.ps1`
- `tests/ViceSharp.TestHarness/PhaseAcceptanceCoverageTests.cs`
- `tests/ViceSharp.TestHarness/FrCfg003AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCfg004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCia005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCpu004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCrt002AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrInp003AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMed005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMem004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMem006AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMon005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMon006AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSid004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSid005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSid009AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSnp003AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrUi004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrVic20005AcceptanceTests.cs`

Release filter of those classes plus `PhaseAcceptanceCoverageTests`, before the last stimulus corrections: Failed 52, Passed 46, Skipped 0, Total 98, exit 1. The coverage fact was not one of the 52 failures.

Stimulus corrections after that filter changed test code only. No product source was edited.

- `FR-SID-009`: OSC3 is voice 3. The noise fact writes `$D412` and reads `$D41B`. It had written `$D404`.
- `FR-VIC20-005`: each cart attaches on its own host, and `AppliedToRuntime` is required. Dirty FE3 and Ultimem come back from `FlushImage`. Mega-Cart NVRAM comes back from `FlushNvram`, and `FlushImage` stays null. Ultimem `$2000` is stored at image offset `$6000` because the power-up BLK1 bank register is 3. An earlier combined session left the FE3 preset `8k` on the port. Ultimem rejects that preset after `Eject`, and the host still returns Ok, so the kind was `None`.

Focused rerun of `FrSid009AcceptanceTests` and `FrVic20005AcceptanceTests`: Failed 0, Passed 9, Skipped 0, exit 0. The 98-test filter was not repeated after that, so the full fail count is not re-measured.

Still red from that run, excluding the two facts fixed above:

- `FR-CPU-004`: the cassette motor stays off when bit 3 is driven active low. Sense bit 4 stays set when play is pressed (expected 0, actual 16). An output-low bit that becomes an input reads the pull-up on the first read (expected 0, actual 32).
- `FR-SID-005`: there is no `SetSidModel` or `SwitchFilterModel`. The resonance fact stays red. The same stimulus produced 8580 band-pass 30320 and 6581 band-pass 26265. A smaller absolute band-pass value is the wrong comparison. Do not flip that inequality.
- `FR-CFG-003`, `FR-CFG-004`, `FR-CIA-005` CNT and SP pins, `FR-CRT-002` ROMH and CRT CHIP packets, `FR-INP-003` mouse methods, `FR-MED-005`, `FR-MON-005` (`Unknown: bank`), `FR-MON-006`, and `FR-SNP-003`.

A standard VIC-20 BLK image, separate from FE3, Ultimem, and Mega-Cart, does not have its own fact.

Do not implement cassette motor, sense, discharge, monitor bank or watch commands, palette files, hotkeys, the media catalog, replay, the mouse port, Ocean ROMH, or a runtime SID switch to turn this suite green. The next code for each still-red criterion is a correct failing test, then mocks, then implementation.

`FR-CPU-002` ac-05 stays the VICE x64sc phase sentence and does not name `IClockedDevice.Tick` or `ICpu.Phase`. `FR-PRF-005` stays the 6560/6561 and the four VIC voices, with no stub and no silent allowance. Ledger items 2, 4, 5, 8, and 9 stay closed for the ledger claims. Items 1, 3, 6, 7, 10, and 11 stay open. `provenance-accepted-20260930.json` was not rewritten. `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json` is modified in the worktree and is not in `c364e73`. This note does not change that file.

## Current State

- Plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`
- Phase-exit gate: see the section above. Script exit 0 at local 2026-09-30 14:58:09 (94 in scope, 0 missing from `tests/`). The acceptance suite is not green. Phase 2 has not exited.
- Phase 0 recorded the live baseline. Phase 2 recorded timings for one dense session. On 2026-09-30 the operator said "Finish this" after "It does not accept the requirement set." Ledger item 2 was later closed when the operator required VICE behavior instead of `IClockedDevice.Tick()` and `ICpu.Phase`. No requirement status was set to completed, and no TODO was marked done. Codex `gpt-6-astra` at effort `xhigh` reviewed that order at 2026-09-30T15:34:43Z and returned DISAGREE, accuracy 95, completeness 90, pass 2, fail 9, unknown 1. The same model and effort reviewed the updated packet at 2026-09-30T16:06:19Z and returned DISAGREE, accuracy 97, completeness 95, pass 2, fail 9, unknown 1. Thread `01a0f308-7f62-72f0-89c7-da43b8460cff`. Receipt: `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.response.jsonl`. The same model and effort reviewed the audit packet at 2026-09-30T16:50:56Z and returned DISAGREE, accuracy 96, completeness 92. Receipt: `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`. The same model and effort reviewed the corrected audit packet at 2026-09-30T17:02:03Z and returned AGREE, accuracy 99, completeness 99, pass 12, fail 0, unknown 0. Thread `01a0f33d-b297-7663-87e5-c83fea36b10c`. Receipt: `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`. That agreement is that the ledger matches the files. It does not accept the recovered requirement set and it does not complete goal 2.
- The recovery guardrail is running. Source commit `a26464e234fa31cae85d433bdec27cbdd654b4c5` merged to `develop` as `fc75387043f8337855e34d92eb3a0e03aa82ce48` in https://github.com/sharpninja/McpServer/pull/73 at 2026-09-30T14:37:17Z. The running service was republished from detached `origin/develop` `fc753870` with Nuke `UpdateService --skip-version-bump --package-version 1.4.41`. Current health is `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`. `GitVersion.yml` was not changed. The dirty `develop` checkout was not used. The Phase 2 smoke was measured earlier on `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`.
- The first Phase 1 and Phase 2 100/100 reviews are withdrawn. The replacement requests are `docs/receipts/hv/phase1-completion.brief.md` and `docs/receipts/hv/phase2-completion.brief.md`.
- The reviewer has no MCP access. The requirement records supplied for review are under `docs/receipts/requirements-recovery/acceptance-20260929/supplied-requirements/`: 510 snapshot files plus 13 live HTTP GETs at 2026-09-30T00:35:23Z.
- The 2026-09-30T00:37:12Z Phase 1 review of the earlier supplied export, `docs/receipts/hv/20260930T003712Z-phase1-supplied.response.jsonl`, is DISAGREE. That export still had empty structured criteria. It is not the current store. The 15:34:43Z acceptance review is `docs/receipts/hv/20260930T152603Z-requirement-set-acceptance.response.jsonl`: DISAGREE, accuracy 95, completeness 90. The 16:06:19Z acceptance review is `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.last.md`: DISAGREE, accuracy 97, completeness 95. The 16:50:56Z review is `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`: DISAGREE, accuracy 96, completeness 92. The latest completed review is `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`: AGREE, accuracy 99, completeness 99, at 2026-09-30T17:02:03Z, on ledger accuracy only. The recovered requirement set is not accepted, and goal 2 is not complete.
- Phase 2 completion review `docs/receipts/hv/20260930T002642Z-phase2-completion.response.jsonl` is AGREE, accuracy 100, completeness 100, on the timing measurement only. It does not accept the requirements.
- Phase 3 (xvic parity) has not started. Phase 4's VIC-20 settings-visibility behavior is implemented and has a focused 8-pass, 0-fail, 0-skip gate; that result does not prove xvic parity.
- Older-plan resume point: an explicit user message approving the start of Phase 3 xvic parity. Approval of the VSF plan is not that approval. Do not open slice 3a before that message.
- `FR-VIC-001`, `TR-CYCLE-001`, and `FR-CPU-002` now have structured acceptance criteria copied from the bullets already in their bodies. Counts are 8, 7, and 6. Body lengths stayed 1105, 995, and 1110. `isSatisfied` is false. The body text was not rewritten. Do not write xvic tests against a body rewrite until the user approves that text and the start.
- Checkpoint `c364e73037f646a7c5d0005c931fb689f665ce9f` is on `origin/main` and includes the acceptance receipts. `PLAN-C64SET-001` is not done. xvic parity is not claimed.

Repository state when this file was replaced:

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- HEAD at the receipt commit: `c364e73037f646a7c5d0005c931fb689f665ce9f`
- Origin: `https://github.com/sharpninja/vice-sharp.git`
- `origin/main` matched that HEAD immediately after `git push origin HEAD:main`.
- The receipt paths from this audit are in that commit. Do not clean, reset, or revert them unless the operator asks.

Live server at the Phase 2 smoke:

- Version: `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`
- Health: Healthy, storage reachable
- Effective counts: FR 232, TR 138, TEST 140, MAP 231
- Recovery run `slice5-apply-clean-18028995ae17` is applied. Payload hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`.
- Do not POST another body on that idempotency key.

## Receipts

These files exist. The 100/100 reviews are not completion gates.

- Phase 0 baseline: `docs/receipts/requirements-recovery/acceptance-20260929/phase0-baseline.json`
- Phase 0b plan review: `docs/receipts/hv/20260929T230403Z-handoff-completion-plan-ignore-mcp.response.jsonl` (AGREE 99/99 on the plan text)
- Phase 1 ledger: `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json`
- Phase 1 review, withdrawn as a completion gate: `docs/receipts/hv/20260929T233104Z-recovery-acceptance-audit-profile.response.jsonl`. The brief at `docs/receipts/hv/recovery-acceptance-audit.brief.md` told the reviewer that no done claim was made and that open rows were the expected report.
- Phase 2 smoke: `docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`
- Phase 2 review, withdrawn as a completion gate: `docs/receipts/hv/20260929T233645Z-dense-session-smoke.response.jsonl`. The brief told the reviewer to pass the phase when the timing receipt matched and to pass requirements because no requirement was claimed done.

Phase 2 measured session `GrokCode-20260929T215620Z-plugin-session` on that server only. `begin` was HTTP 201 in 322 ms. `complete` was HTTP 200 in 312 ms. Readback matched. No session-log code changed. This does not claim that older 40-second sessions are now fast.

## Recovery Acceptance

The preview gate `docs/receipts/hv/20260928T203110Z-requirements-recovery-preview-gate.response.jsonl` was DISAGREE. Applying that preview did not accept the requirement set.

Ledger status:

- Closed, and only for the narrower claim in the ledger: item 4 (four named TR bodies and two deleted placeholder ids) and item 9 (five live HTTP 400 dry-run rejections, `FR-VIC-001` body still 1105 characters). Item 9 is the plan guardrail row. It is not preview A9.
- Open: item 1 (acceptance criteria were not restored where no heading existed), item 3 (semantic classification is not done), item 6 (mappings are not per-AC coverage), item 7 (manifest is committed and still is not a named-program workload oracle), item 10 (preview D3), and item 11 (preview A9).
- Needs-operator: none. Item 2 is closed: `FR-CPU-002` matches VICE x64sc phase behavior. Item 5 is closed: `FR-PRF-005` requires the 6560/6561 and the four VIC voices, and the x64sc sprite and illegal-mode text stays. Item 8 is closed: the provenance receipt was not rewritten.

Item 9 recheck at 2026-09-30T15:49:00Z, workspace `F:\GitHub\vice-sharp`, method `workflow.requirements.planRecovery`, dry-run. Placeholder body and acceptance-criteria removal returned `validation_error`, `retryable` false, exception `System.ArgumentException`. Empty body, malformed id, and type-prefix mismatch returned `schema_validation_failed`, `retryable` false, and were not planned. `FR-VIC-001` parsed description text was unchanged across the before and after reads (1105 characters, equal, SHA-256 f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e). The earlier 1215 figure was not the parsed description. A direct dry-run POST to `/mcpserver/requirements/recovery/dry-run` at 2026-09-30T16:13:00Z returned HTTP 400 `validation_error`, `retryable` false, for empty body, placeholder body, malformed id, type-prefix mismatch, and acceptance-criteria removal. After those five calls a later GET was saved as `FR-VIC-001-after-get.json` in `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-http-20260930T161259Z`. Its `body` field is 1105 characters, SHA-256 `f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e`, the acceptance heading is present, and the structured list has 8 entries. That SHA matches the 15:15:09Z supplied description. The saved GET is later than the 16:13:00Z probes. The plugin schema still rejects empty body, malformed id, and type prefix before the server sees them. Those plugin results are `schema_validation_failed`, not a second copy of the HTTP 400. Plugin receipt directory: `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-20260930T154900Z`. The running service is health `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`.

The placeholder rows `[TR-REMOTECTRL-SERVER-001]` and `PERF-SPRITE-DMA-OPT-001` were deleted on 2026-09-30. The 2026-09-30T15:15:09Z export records both as `not_found`. The unbracketed `TR-REMOTECTRL-SERVER-001` remains. TR `totalCount` in that export is 136.

Structured acceptance criteria were copied from existing `## Acceptance Criteria` bullets only. The scan covered FR 232, TR 138, and TEST 140. 205 records received criteria, 90 already had criteria and were left unchanged, and 215 had no heading and were not given invented criteria. Status values were not changed. Receipt: `docs/receipts/requirements-recovery/acceptance-20260929/structured-ac-backfill.json`.

Four legacy ids return 404: `ARCH-TRUEDRIVE-1541-002`, `BACKFILL-MEDIA-001`, `BACKFILL-VIDEO-001`, `RUNTIME-TAPE-002`. Their `FR-*` replacements return 200. `FR-MCP-REQRECOVERY-001`, `TR-MCP-REQRECOVERY-001`, and `TEST-MCP-REQRECOVERY-001` are 404 on this workspace. Do not mark them satisfied.

Item 7: `docs/requirements/oracles/vic20-xvic-manifest.json` is in commit `c364e73037f646a7c5d0005c931fb689f665ce9f`. The r4 review saw it untracked. Item 7 stays open. VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. Media is an explicit empty array. Workload `program` values are the ROM names `kernal.901486-07.bin` (machine `vic20`) and `kernal.901486-06.bin` (machine `vic20ntsc`), from `Vic20ViceRomNames`. Cycle counts are the constants already in `Vic20NativeLockstepTests.CycleCounts` and `Vic20DivergeProbe`. The Codex review at 15:34:43Z rejected the earlier `power-on-reset` label. No new cycle numbers were invented.

## Operator Contract

- VICE, including x64sc and xvic quirks, is the behavioral oracle unless an approved requirement says otherwise.
- UWP and Xbox are outside the current implementation scope. Their historical requirements are not deleted or retired unless the operator explicitly directs that change.
- On every ViceSharp versus VICE mismatch, answer how the managed core diverged from VICE and how it gets back to VICE before changing code.
- All implementation, investigation, requirements work, builds, tests, and documentation stay local in this chat. Do not use cloud runners, hosted agents, web tools, remote Codex threads, cloud CI, pushes, or other cloud execution.
- Hostile validation is the sole exception and must run through Grok launched and operated locally. Persist its request/response receipts in `docs/receipts/hv/` and its verdict in the active local MCP session.
- Do not set `--max-turns` or any other artificial turn or time limit on hostile validation. Let the local Grok reviewer run until it returns a terminal verdict or the operator explicitly stops it.
- PowerShell only. Do not use Python.
- Do not mark a requirement, TODO, goal, or plan step done without executable proof and a hostile AGREE at accuracy and completeness both at least 98.

## Open TODOs

36 open. These stay open and are not this handoff's exit: `PLAN-GRAPHRAG-001`, `PLAN-RASPBIAN-001`, `PLAN-ARCHVIC20-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`. The full open id list from the 2026-09-29 query is in the session turn `req-20260929T234800Z-why-are-you-waiting`.

## Next unfinished work

1. Re-run requirement traceability and `git diff --check`, then submit the fully remediated packet and its local evidence to a new local Grok hostile review. Preserve every failure if the verdict is not AGREE at 98/98 or higher.
2. Ask for explicit operator approval of `PLAN-VSFLOCKSTEP-001` only after the hostile gate passes. Approval authorizes Slice 1 only; do not implement native snapshot product code before it.
3. The older Phase-exit gate is still open. The coverage script exits 0 because the 17 ids are named under `tests/`; acceptance facts for missing behavior are still red. Do not loosen `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases` or mark `PLAN-C64SET-001` done.
4. Phase 3 of the older parity plan still requires its own explicit start sentence. Phase 4's VIC-20 settings-visibility behavior is now implemented and covered by `Vic20SettingsVisibilityNotificationTests` and `Vic20SettingsUiSurfaceTests`; that focused gate is not proof of xvic parity. The VSF plan does not silently grant the start of Phase 3.
