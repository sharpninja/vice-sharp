# Acceptance-control correction evidence

2026-10-08, America/Chicago. Agent: Codex. The two new acceptance-only mock cases and the corrected generic control pass locally. No independent HV acceptance, phase advancement or runtime emulator parity is claimed.

## What changed

- Added DecisionAcceptanceControlTests.AcceptanceOnlyFaultIsRejected for the existing clean/accepted and tracked-delta/rejected-DirtyBefore expectations. Each correct NSubstitute response passes AssertProbe; changing only Accepted must make that same assertion reject the response. Error and HalfCycle stay equal to the independent expectation.
- Changed ProveDecision's compound acceptance mutation to `expected with { Accepted = !expected.Accepted }`. Previously other changed fields could reject the response even if the acceptance assertion were missing.
- The supplied-provider assertion, DecisionIs, all existing fixture rows and expected results remain unchanged. The two additional cases supplement, not replace, the prior 87 oracle cases. No production adapter or algorithm was added.
- Updated the oracle README with the new evidence and limitations. Existing historical paragraphs, sources and receipts remain retained.

The written scope and sequence preceded edits in SLICE.md. Existing requirement mapping is FR-VICE-001 AC1/AC3, TR-VICE-ORACLE-001 AC1 and TEST-VICE-001 AC1. The updated scenario-ac-ledger.json preserves every previous row's mapping and marks the new rows as assertion-discrimination evidence only. No AC is satisfied by this change.

## Chronology and commands

Source originals were copied and hash-verified before edits. The new tests were written before the generic helper correction. Automatic supported code-verification builds succeeded after each C# edit. Build receipts and logs are retained at this directory's root.

Before the helper edit, full unfiltered Release runs passed:

- 15:59:32: OracleContract 89/89.
- 15:59:48: VsfCompatibility 199/199.
- 15:59:56: NativeClockProbe 69/69.

After the helper edit and its successful build, the same full scope passed:

- 16:00:45: OracleContract 89/89.
- 16:00:53: VsfCompatibility 199/199.
- 16:01:10: NativeClockProbe 69/69.

Each stage totals 357 passed, exit 0, with failed, notExecuted, aborted, error and timeout all zero. Each run's result.json records its exact command. The command shape is `dotnet test <standalone-project> -c Release --logger trx;LogFileName=<name>.trx --results-directory <unique-directory> --diag <unique-log> --blame-hang --blame-hang-timeout 2m`; no filter was supplied. PowerShell.MCP invoked the established receipt helper. The solution, normal TestHarness and native bridge build were not run.

Each build and test had an eight-field native before/after inventory comparison. The existing native/vice baseline was preserved, including its existing dirt. Preservation does not make that checkout a clean acceptance oracle. These suites include mock contracts and previously existing native-free C fixture tests, not real VICE execution.

## Receipt omission, explicitly unresolved

I failed to archive the before-helper-clock run's two C compiler logs before the after-helper-clock run. CompiledCFixture writes fixed compile.log paths, so the second run overwrote those first-run compiler logs. I did not reconstruct or relabel later logs as earlier evidence.

The first run's TRX, dotnet stdout/stderr log, VSTest diagnostic log and native inventories are retained. The second run's two C compiler logs are now copied and hash-verified, along with all available unique run logs and executables. c-fixture-evidence-pins.json indexes 192 retained files, including historical runs; it is not a claim that all 192 were produced by this slice. This omission remains part of the pending HV scope.

## Receipts and limits

- preimage-pins.json and *.before: unchanged original source/document bytes.
- postimage-pins.json and *.after: verified source snapshots; README.after predates the final documentation addition, separately preserved as README.final.md.
- before-helper-* and after-helper-*: full test outputs, TRX, diagnostics and result JSON.
- *-native-before.json and *-native-after.json: complete inventories for every build/run.
- scenario-ac-ledger.json: 89 source-to-AC rows, prior 87 mappings preserved, all real-gate satisfaction false.
- verification.json: final case/source preservation, requirement readback, native checks and whitespace result.
- helper-change.diff: exact helper delta against the retained preimage. Its git diff --no-index exit 1 means the files differ, not a build failure.
- clock-gate-evidence and clock-probe-evidence: copied available native-free C fixture evidence.
- session-turn.completed-primary-readback.json and primary-proof.json: written after completion through the supported MCP workflow and primary readback.

The earlier prepared Opus packet was not overwritten or launched. Any future authorized review must include and pin this correction, the legacy audits/proposals and all later evidence. Pending operator decisions and the provider retry boundary remain in force. No commit, push, native edit, canonical requirement change or done-state update occurred in this slice.
