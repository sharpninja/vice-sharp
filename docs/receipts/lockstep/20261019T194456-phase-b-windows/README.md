# Phase B on PAYTON-LEGION2, 2026-10-19

Goal GOAL-LOCKSTEP-REMEDIATION-001, task "Step 8", first phase. Brief: OMAR-VICE# 2026-10-19T19:43 (Phase B of HANDOFF.lockstep-plan.md section 4 as amended by HANDOFF.lockstep-plan.v2.md section 4 and the section 11 decisions). D3 defers the review that v1 Phase B names to step 12, so this phase exits on measured evidence plus the self-assessment below. No reviewer was launched.

Head: b660bee9ffa5f80a2c45caa29cde8b62b5751529 on develop. dotnet 10.0.402. native/vice gitlink cc67418cba58e71d8e70fd353e9de47199ad2075.

## 1. Repin of the 826-input packet

Packet: `docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/final-old-input-revalidation.json` (826 original pins). Script: `Get-PacketPinDiff.ps1` (read-only, analyzer 0). Full per-input results: `pin-diff.json` (old hash, Oct 8 revalidation hash, current hash, status, changing commits) and `pin-diff.txt`.

- 826 inputs: 813 unchanged, 13 changed, 0 missing. 825 are inside the repository.
- 11 inputs had already changed at the Oct 8 revalidation (the disclosed Oct 8 evidence corrections). 8 of them are unchanged since and reached develop in c01e6d32 (2026-10-16T09:41, checkpoint commit): the reviewer progress jsonl, the binary/clock/gate runtime READMEs, gate-observer.c, GATE-SLICE.md, OracleContractTests.cs and BINARY-MONITOR-SLICE.md.
- The other 3 of those 11 changed again after Oct 8:
  - `HANDOFF.md`: ed7c2e60, 6530196f, c01e6d32, fa259f22, 8a5ca856, 76bacc3d, 92214889 (2026-10-15 to 2026-10-16).
  - `tests/ViceSharp.OracleContract.Tests/ContractAssertions.cs` and `README.md`: c01e6d32.
- 2 inputs changed for the first time after Oct 8:
  - `AGENTS-README-FIRST.yaml`: the gitignored MCP marker, rewritten on every server restart. Hash only; never read into receipts or committed.
  - `docs/requirements/test/TEST-Requirements.md`: ed7c2e60 (2026-10-15), c01e6d32, fa259f22 (2026-10-16).
- No changed input differs from HEAD in the working tree.

## 2. Native-safe suites unfiltered, Release

Receipt folder: `docs/receipts/lockstep/20261019T194456-phase-b-native-safe-windows/` (run.log, summary.json, three TRX files, console logs, native inventories). Script: `Invoke-PhaseBNativeSafe.ps1` here, the hash-verified copy of OMAR-VICE#'s `Invoke-PhaseAStep1NativeSafe.ps1` (original kept alongside as `Invoke-PhaseAStep1NativeSafe.original.ps1`, SHA256 233A6712...) with one added `-Suffix` parameter for the receipt name.

- ViceSharp.OracleContract.Tests: 89 total, 89 passed, 0 failed, 0 skipped.
- ViceSharp.VsfCompatibility.Tests: 199 total, 199 passed, 0 failed, 0 skipped.
- ViceSharp.NativeClockProbe.Tests: 69 total, 69 passed, 0 failed, 0 skipped.
- Total 357/357; every TRX result is Passed.
- native/vice inventory before and after: 2,539 entries each (12 tracked changes, 2,527 untracked, all pre-existing), SHA256 C7848A24867CD60C both times: unchanged.
- LAB-OMARCHY ran the same three suites at b660bee: 89/0/0, 199/0/0, 69/0/0 (OMAR-VICE#, 5b13ddd, `20261019T194406-phase-b-native-safe-linux`), so the v2 amendment "also green unfiltered on LAB-OMARCHY" is met in the same step.

## 3. Holistic self-assessment of the Phase 1 scope

Scope: the three mock projects, the 16 draft findings (F-01 to F-16) and their parent dispositions (`finding-dispositions.json`), the shared fixtures, the stdio mock contract and the Oct 8 evidence corrections. Each check below was made against today's tree and today's TRX, not taken from the Oct 8 dispositions. No finding was ever independently accepted; that stays so until step 12.

- F-01 (Medium, resume launch let hooks cancel the historical review turn): disposition "historical record preserved, no replay". Still satisfied: the four queue files 2c6e, 6e35, f583, a8c8 remain in `.mcpServer/claude/reviews/20261008T073800-0500-phase1-oracle-contracts/failsafe/pending`; operator disposition given in Phase A step 7 (superseded, preserved in place). Fresh-cache launch safety is unproven until a launch happens (step 12).
- F-02 (Medium, secret exposure without a rotation decision): operator ruling "Ignore" (2026-10-19). Closed by ruling, not by repair.
- F-03 (Low, web documentation consulted despite the local-only contract): the deviation is retained and excluded from runtime proof. No operator ruling is recorded in the evidence I can see. Open.
- F-04 (Low, gate stdout claim): satisfied. `gate-runtime/README.md` lines 8, 52, 69 to 70 disclose that the seven stdout files are empty; all seven measure 0 bytes. Native stdout retention itself is still unproven (Phase C).
- F-05 (Info, clock reset correlation): satisfied. `clock-runtime/README.md` lines 50 to 52 cite the native `RESET` line as correlation only.
- F-06 (Low, binary-mock-01 missing from the chronology): satisfied. `BINARY-MONITOR-SLICE.md` lines 64 to 75 give mock-01 125/125, mock-02 137/138, mock-03 138/138; the `binary-mock-01` folder exists.
- F-07 (Low, single-fault control for changed postflight): satisfied. 7 postflight tests pass in today's TRX, including `WrongReceiptAfterPostflightIsRejectedIndependently` and `WrongPostflightArgumentIsRejectedIndependently`.
- F-08 (Low, vacuous zero-test receipt accepted): satisfied. `RequiredProvenanceAndCounts(scenario: "zero-tests", accepted: False, error: "NoTestsExecuted")` and related cases pass.
- F-09 (Low, overbroad AC ledger): satisfied. The per-scenario ledgers exist in `20261008T130200-0500-phase1-mock-remediation`, `20261008T131300-0500-phase1-fixture-reuse` and `20261008T155754-0500-decision-single-fault-controls`; the original ledger is preserved.
- F-10 (Low, fixtures not reusable; boundary took ResolvedPath as input): satisfied for the mock scope. 10 boundary and provider cases pass (`ImmutableViceWrapperBoundaryTests.DestinationAndAfterState` with independent requested and resolved paths). The real filesystem and junction resolver is unimplemented (Phase C or later).
- F-11 (Low, helper refactor provenance): permanently partial. The pre-refactor bytes never existed in evidence; `clock-fixture-evidence` (8 files) and `gate-fixture-evidence` (21 files) hold the later preimages and compiler logs.
- F-12 (Low, stdio adapter without mock-first tests): satisfied for mocks. 19 `StdioAdapterContractTests` cases pass, as claimed. Real C adapter binding, stdout retention and broken-pipe behavior are unrun; that is Phase C step 2.
- F-13 (Info, session-log model label is the placeholder codex): still open. The installed Codex plugin 1.121.0 still hardcodes `Model = 'codex'` (`lib/Invoke-McpPlugin.ps1` line 186, `lib/plugin-env.ps1` lines 50 and 80).
- F-14 (Low, decisions logged as dialog only): forward repair recorded on Oct 8. Not re-verified here (it lives in historical MCP session-log turns). Open for step 12.
- F-15 (Info, pending records in the isolated queue): the four records are still present and preserved; disposed by the operator in Phase A step 7.
- F-16 (Info, installed Claude plugin vs marker version): changed. Installed Claude is now 1.121.0; the marker still declares `plugin_version: 1.118.0` for every agent. The marker is server-written, so the mismatch remains and is disclosed.
- Oct 8 evidence corrections: of the 11 inputs changed by Oct 8, 8 are unchanged since and committed; 3 were edited again by later commits, all listed in section 1 (no silent change).
- PLAN-VICEORACLE-001 task 2 stays not done: D3 defers the review that closes it. Recorded here, not in the TODO.

Scores, with justification:

- Accuracy 98. Every count, hash, path and line number above was measured today from the artifact itself (TRX, files, git) and is reproducible from the scripts in this folder. The remaining risk is interpretive: whether each mock case covers its finding's full intent is my judgment, not an independent review.
- Completeness 90. Below 98, so not a pass. Gaps:
  1. F-03: no operator ruling recorded.
  2. F-13: wrapper still hardcodes the Codex model label.
  3. F-14: forward repair not re-verified against the session log.
  4. F-16: marker plugin_version stale against installed 1.121.0.
  5. F-04, F-10 and F-12: real native stdout retention, real path resolver and real C stdio binding are unproven (Phase C entry work, not Phase B).
  6. F-11: historical pre-refactor provenance is permanently missing.
  7. No finding is independently accepted (D3: step 12).

## Phase C entry conditions

No blocker found. The separate materialization exists at `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075` (6,689 files; gitignored), matching the native/vice gitlink, plus `artifacts/immutable-vice/clock-hook` (7 files). MSYS2 is installed (`C:\msys64\usr\bin\bash.exe`, `C:\msys64\mingw64\bin\gcc.exe`); gcc and make are not on PATH, so Phase C scripts must call them by full path. ucrt64 gcc is absent.

## Incidental findings

- `codex plugin add` from a local marketplace copies gitignored files into the plugin cache, including that checkout's MCP marker (with its API key) and 624 `.mcpServer` files. A same-version re-add merges over the cache without deleting stale files. Filed to triage: `triage-report-187219661bd64cbe9f17bb25b842e752`. The stray nested `.worktrees` copy from the earlier reinstall was removed after a verified tar backup (1,903 entries).
