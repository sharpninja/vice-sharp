# Interrupted HV evidence corrections

Recorded 2026-10-08T13:50:54-05:00. This slice corrects evidence descriptions and adds traceable current audit records. It does not implement emulator behavior, recreate missing historical evidence, advance a phase or establish independent acceptance.

## Corrections and remaining obligations

- **F-04:** all seven gate `stdout.log` files are zero bytes. Their paths, sizes and hashes are in `historical-evidence-readback.json`. The gate README and adapter header now say native diagnostic retention was not established. Clock/stop stderr and host-permission records are separate evidence. Buffered stdout plus `_Exit` is a candidate explanation only. No behavior change or runtime rerun occurred. Retention and adapter-contract coverage remain mandatory before relying on these diagnostics for that purpose or integrating the adapter into the harness.
- **F-05:** all three wrapped clock logs contain `VICE_CLOCK,7` and `VICE_CLOCK,8` at lines 26/27, `Main CPU:  RESET.` at line 28, then the next `7,8` pair at lines 29/30. Exact excerpts/hashes are retained. This is correlated evidence, not a directly observed causal reset event or permission to relabel epochs.
- **F-06:** the binary chronology includes the omitted 125/125 mock run at 09:28:37, then 13 added cases. TRX name comparison has exactly 13 additions and no removals. The first expanded run at 09:30:20 had one `FormatException` in fixture-name parsing; the corrected 138/138 run at 09:31:04 preceded production binding. Failed receipts remain failures.
- **F-11:** the original untracked pre-refactor `ClockHookImplementationTests.cs` bytes and its historical clock compiler log were not retained. This slice discloses that gap. [Later compiler-log copies](../20261008T131300-0500-phase1-fixture-reuse/compile-log-copy-proof.json) prove only their later runs. No preimage was reconstructed or presented as original evidence.
- **F-14:** four completed source turns contain 11 decision dialogs and no matching decision actions. The supported session skill declares completed turns immutable. Eleven newly dated corrective action/dialog pairs were added to the current turn, each retaining the exact original decision and source session/request/timestamp. Primary readback verifies every pair. The four original primary turn JSON objects remain identical. This repairs the current audit trail, not past compliance; independent acceptance of this disposition is still required.

## Preserved evidence

- `preimage-copy-proof.json`: seven pre-edit document/comment-source copies with matching SHA256; originals were not deleted.
- `historical-evidence-readback.json`: seven empty stdout records, reset excerpts and hashes, all three binary mock run counters/TRX hashes, and all 13 added case names. No runtime log or TRX was rewritten.
- `historical-decision-readback-before.json`: original decision records and terminal status from primary MCP.
- `current-corrective-decision-records.json` and `current-corrective-decision-readback.json`: exact current corrective records and primary confirmation.
- `historical-turn-preservation-proof.json`: exact full primary-turn JSON comparisons before/after the new records, all four unchanged.
- `integrity-result.json` and native inventories: all eight native fields unchanged, 3847 original stock source files unchanged, 99 link inputs unchanged, original gate probe SHA256 `A15ED9740135D4E95A2594D8155E2FC807B45453FFCF525E731DEDF02733FF60` unchanged.

## Comment-only source check

Only the opening `gate-observer.c` comment changed. Source bytes beginning with the first `#include` are exactly equal to the retained preimage. The original probe executable was not relinked or replaced.

`comment-compile-result.json` retains the first direct compiler failure: exit 1, no object, empty compiler output. Its cause is not established. `comment-compile-02-result.json` retains the successful separate-object check: exact compiler hash/arguments, child working directory, compiler-directory PATH prefix used by the existing passing `CompiledCFixture`, exit 0 and object hash. The child environment convention did not alter the parent/global environment. Separate stdout/stderr and native before/after inventories are retained. Neither compile is a runtime-log retention test.

The initial documentation patch was rejected because its expected context omitted part of a line; no change landed from that attempt. The subsequent exact-context patch landed. `git diff --check`, per-file whitespace checks and post-edit source/doc hashes are recorded in `correction-result.json`.

## Review and continuation

Current parent: `Codex-20261007T135626Z-plugin-session` / `req-20261008T184031Z-prompt-336a`. Primary completion readback is retained separately. All corrected docs/comment inputs changed after the interrupted review and must be repinned/disclosed on resumption.

The same local Opus 5.5 max logical review remains incomplete on quota. Its recovered draft was DISAGREE, accuracy 96 / completeness 94, five FAIL and one UNKNOWN, not a finalized verdict. No reviewer was substituted or restarted here. Secret-exposure disposition, the prior local-only deviation, historical hook interference, routed triage readback and all remaining findings/gates remain open. See [the complete interrupted-review summary](../../hv/20261008-noon-catchup-opus.resume-02.terminal-summary.md).
