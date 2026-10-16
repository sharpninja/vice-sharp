# Decision acceptance single-fault correction

2026-10-08, America/Chicago. This corrects test infrastructure inside the existing unaccepted Phase 1 mock gate. It changes neither requirements nor production behavior and does not advance a gate.

## Defect and approved contract

ContractAssertions.ProveDecision changes Accepted together with Error and HalfCycle in its acceptance negative control. Removing the acceptance assertion could therefore leave that control green because another field still rejects the response. The README incorrectly describes these mutations as independent.

Existing requirements govern the correction: FR-VICE-001 AC1/AC3, TR-VICE-ORACLE-001 AC1 and TEST-VICE-001 AC1 require correct oracle acceptance and dirty-target rejection. BDPv4 requires discriminating wrong mocks. The same generic helper also serves existing receipt, observation and PHI2 cases; their fixture inputs and expectations must remain unchanged.

## Sequence and validation

1. Copy and hash-verify all existing oracle source files and README into this receipt directory before edits. Preserve originals.
2. Add DecisionAcceptanceControlTests.AcceptanceOnlyFaultIsRejected with two independent existing fixture expectations: clean -> accepted and tracked-delta -> rejected/DirtyBefore. First exercise the correct NSubstitute response through AssertProbe. Then change only Accepted and require the same assertion to reject it. Error and HalfCycle must remain identical. These are passing negative-control tests, not deliberately red tests.
3. Invoke the supported code-verification hook immediately after the edit. Run all three approved native-safe projects unfiltered Release to green before changing the generic helper. Preserve logs, TRX, diagnostics and native before/after inventories.
4. Replace only the compound acceptance mutation in ProveDecision with expected-with-Accepted-inverted. Keep Error and HalfCycle unchanged. Do not change DecisionIs, the real-binding seam, fixture rows or expected results.
5. Repeat edit verification and the full native-safe scope: OracleContract, VsfCompatibility and NativeClockProbe. Confirm the prior 87 oracle cases remain present plus two new cases; VSF 199 and clock 69 remain mandatory. No failed, skipped, aborted, error, timeout or unexecuted cases are accepted.
6. Retain the exact diff and post-edit hashes, source/fixture preservation proof, complete validation receipts and primary MCP readback. Add the correction to the pending Opus catch-up evidence. Independent acceptance remains pending; do not modify the frozen earlier packet or claim an HV pass.

Every build/test is surrounded by the existing eight-field native inventory check. Existing native dirt must be preserved exactly and is not considered a clean oracle. Run only the standalone projects, never the solution, normal TestHarness or native bridge build. No live emulator parity or real adapter behavior is proved by this slice.

The retained validation result JSON records each exact dotnet command, configuration, result directory and counts. git diff --check must return zero. No commit, push, reviewer launch, credential change or canonical requirement write is part of this correction.
