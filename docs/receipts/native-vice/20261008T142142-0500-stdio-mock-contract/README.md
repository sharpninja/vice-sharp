# F-12 diagnostic stdio mock contracts

2026-10-08T14:28:11-05:00, local PAYTON-LEGION2. This is mock-first test
remediation inside the existing unaccepted gate, not a phase completion.

## Result and scope

Nineteen new cases validate the diagnostic adapter's protocol assertions:
publish/flush ordering, zero/maximum clocks, write/flush failures, EOF versus
input error, quit/invalid bytes, and failures while reporting termination.
Every case proves correct mocked observations and rejects independently
changed fields, null results/traces, missing/extra/duplicate events and each
adjacent-order reversal. These are expected rejections inside passing tests.

`tests/ViceSharp.NativeClockProbe.Tests/StdioAdapterContractTests.cs` supplies
one reusable fixture catalog, provider interface and assertion entry point.
It does not execute the C stdio adapter. `STDIO-ADAPTER-SLICE.md` was authored
before the new test source. No adapter, core gate or other production source
was changed. All 50 existing clock cases are preserved unchanged.

The diagnostic adapter still needs identical real-bound tests. Native stdout
retention and actual broken-pipe behavior remain unproven. These mocks do not
endorse the seven empty historical stdout logs or satisfy real-observation
ACs. F-12 and all independent/full-emulator acceptance gates remain open.

## Validation receipts

All paths below are relative to
`docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`:

- `stdio-mock-01-edit-result.json`: automatic nearest-project build succeeded;
  native before/after checks matched. Sibling build log is retained.
- `stdio-mock-clock-01/result.json`: unfiltered Release, 69/69 passed at
  2026-10-08T14:25:18-05:00 (19 new mocks plus 50 prior cases).
- `stdio-mock-vsf-01/result.json`: unfiltered Release, 199/199 at
  2026-10-08T14:25:57-05:00.
- `stdio-mock-oracle-01/result.json`: unfiltered Release, 87/87 at
  2026-10-08T14:26:16-05:00.

Each result contains the exact command, exit code 0, no filter and counts:
failed/notExecuted/aborted/error/timeout all 0. Every run retains TRX,
stdout/stderr, VSTest diagnostics and native pre/post inventories.
Total across the three projects: 355 passed, zero failures or skips.

This directory retains:

- `effective-requirements.before.json`: supported current-layer MCP readback.
  The linked FR/TR/TEST and every AC remain pending/false.
- `stdio-mock-ledger.json`: all 19 actual TRX names/outcomes with supporting
  FR/TR/TEST AC linkage, explicitly not full AC satisfaction.
- `existing-input-hashes.before.json`, `preservation-proof.json` and
  `native-final.json`: original inputs; prior 50 case-name equality; unchanged
  3847 stock sources, 99 stock link inputs, original gate binary and all eight
  native baseline fields.
- `clock-gate-compile.log`, `clock-probe-compile.log` and
  `compiler-log-copies.json`: exact copies and verified source/copy hashes.
- `HANDOFF.before.md` and `GATE-SLICE.before.md`: verified original copies.

The first compiler-log copy command guessed the nonexistent `clock-hook`
directory and stopped before launching the oracle suite. Inspection found
the actual `clock-probe` directory; both logs were preserved, then the oracle
suite ran successfully. The first console projection of the preservation
dictionary showed null fields; the saved JSON was correct, and its direct
readback produced the recorded counts. Neither failed diagnostic is hidden
or counted as validation success.

Parent session: `Codex-20261007T135626Z-plugin-session`.
Parent turn: `req-20261008T191836Z-prompt-faf5`.
Repin these new/changed inputs for the same local Opus max review. No reviewer
was launched, no phase/requirement/TODO was accepted, and no commit or push
was performed.

Co-Authored-By: Codex <noreply@openai.com>
