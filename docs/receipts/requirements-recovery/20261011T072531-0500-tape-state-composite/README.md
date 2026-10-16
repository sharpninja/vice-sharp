# Consolidated tape correction draft

Codex on PAYTON-LEGION2. Prepared 2026-10-11; typed readbacks and artifact checks measured through 2026-10-11T07:35:27-05:00.

**Unapplied, unapproved, not yet HV-ready. No implementation or parity gate is complete.**

The objective remains the entire original handoff completion plan at `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`, with independent HV agreement. Its SHA256 is `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`. This directory advances requirement correction within that goal; it does not replace the goal with tape-only success.

## What is here

- [Exact consolidated projected delta](proposed-field-delta.json): original eleven-record proposal, earlier wiring/motor supplements, and this turn's state, file-format, dependency and turbo-loader corrections. Contains full before/after values, recalculated changed fields, mappings, coverage names, limitations and approval prerequisites.
- [Typed record preimages](typed-record-preimages.json): all fourteen records recovered through supported `getFr`, `listTr` and `listTest`, including metadata omitted by the effective projection. Raw read values and separate stable before/after values are retained. This is not an executable application payload.
- [Value-bearing preparation checks](preparation-checks.json): actual expected and observed values, field-presence checks, native source pins, AC text-change ledger, mapping existence and comparator controls.
- [Native state audit and fixture obligations](N07-N12-SOURCE-AUDIT.md).
- [Existing-test disposition ledger](EXISTING-TEST-DISPOSITIONS.md).
- [TODO before/update/readback receipt](todo-tracking-readback.json): updates to `PLAN-REQAUDIT-001` and `PLAN-VICEORACLE-001`, including the corrected mocks-first wording and current Opus-only review instruction. Done flags and implementation tasks are unchanged. The receipt retains the initial parent-checker error and its correction.

The old proposal, midnight verdict and earlier supplements remain unchanged. The earlier complete review remains **DISAGREE: accuracy 93, completeness 84; 35 PASS, 6 FAIL, 0 UNKNOWN, 1 N/A**. No new reviewer ran in this preparation.

## Exact scope disclosure

This is **fourteen existing-record updates**, not the old eleven-record approval scope. Additional records are `FR-CPU-004`, `FR-PRF-003` and `FR-TAP-005`. Existing media dependency wording also changes from the originally reused media row.

- Before: 66 criteria. After: 90 criteria.
- All 66 existing IDs remain. Text changes on 55; 11 remain unchanged. There are 24 added criteria.
- Within the original eleven records, the earlier 47 retained IDs still include 45 rewritten texts. Keeping an ID does not preserve its old obligation or validate old evidence against its new meaning.
- Coverage has 81 rows and 96 unique names. Five names are inherited media placeholders, two are approval-dependency groups, and the remaining 89 are proposed case-group names, **not implemented tests or proven complete fixture specifications**.
- Nine unchanged criteria in the newly included CPU/SX-64 records do not receive a new tape-amendment coverage row. They remain mandatory under their existing whole-plan gates. Every changed or added criterion has one coverage row; that structural check does not establish executable AC coverage.
- All seven proposed mapping rows retain existing edges. Two nonexistent proposed additions are withheld: `TR-MEDIA-D64-001` and `TEST-D64IO-001`. Those separate media obligations are not retired or waived. The remaining 38 target references exist; proposed additions total 13.
- Only the inherited media record proposes reopening `closed` to `pending`. Nothing proposes a done/satisfied state. No status or AC flag has been changed on the server.

## Evidence, not acceptance

The supported effective read was followed by fresh typed reads for nine FR, two TR and three TEST records. The previous full-FR failure is no longer a current read blocker. Its historical triage failsafe remains retained and was not replayed.

Preparation checks found:

- 112/112 projected fields matched fresh effective values, with field presence checked before comparison.
- 36/36 changed projected fields matched their typed before-values; `body`/`condition` map to typed `description`.
- 28/28 mapping fields matched, and all 38 target references resolved.
- Nine artifact-comparator controls passed, including wrong scalar/metadata, missing or extra null field, nested AC mutation, array omission and order mutation. These are preparation helper controls, not the emulator's BDP mock gate and not application-transaction tests.
- All three generated JSON files parsed and read back equal to native PowerShell serialization after CRLF/LF and terminal-newline normalization.
- Ten selected native materialized files matched the immutable commit's Git blobs before composition. This is selected-source identity, not a whole-native-tree or runtime claim.

Read-synthesized `createdAt` and `updatedAt` values are retained only as observations. They are excluded from stable preservation comparisons and must not be submitted as recovered historical metadata. Stable typed fields, including `area`, `subarea`, `testType`, scope and field presence, are carried forward except for explicit named changes. A later supported update still needs tested preflight, update, rollback/failure and readback safeguards; these files do not prove that transaction.

## Remaining before review and approval

1. Finish decision-complete fixture matrices and executable assertion/provider design. Resolve the five inherited media placeholders without claiming their separate drive proposal is approved.
2. Audit the unchanged `FR-CPU-004/ac-06` approximately-350-microsecond wording across native board/chip variants. The corrected tape bits do not prove all capacitor requirements coherent.
3. Implement and verify the R0/R1 application safeguards against discriminating mocks: drift, missing fields/targets, preservation, volatile timestamps, partial failure, retry and complete server readback. This preparation does not authorize applying anything.
4. Reconcile canonical root `HANDOFF.md` and the completeness of whole-plan TODO coverage, retaining pending gates and all unresolved prior findings, historical/export limitations and reviewer process disclosures. The two existing TODOs above now carry the current draft and pending gates; this does not establish complete backlog coverage.
5. Submit the complete corrected packet to local Opus 5.5 max. Require AGREE, both scores at least 98, zero FAIL/UNKNOWN, complete request/response receipts and whole-verdict MCP readback. Obtain explicit operator approval for the exact expanded delta afterwards.
6. Only then proceed through approved requirements, green discriminating mocks, identical production-bound assertions, cumulative real-emulator suites, immutable xvic/x64sc comparisons, canonical native VSF interoperability, VIC-20 settings visibility/draft behavior, and final handoff gates. Later start-approval boundaries remain intact.

No production/test/native code changed. No emulator tests, new HV, commit, push, deployment, requirement mutation, queue replay or historical-turn cancellation occurred.

MCP parent: `Codex-20261007T135626Z-plugin-session`, request `req-20261011T122210Z-tape-state-exact-delta`.

Tracking readback measured at 2026-10-11T07:42:35-05:00. My first comparison assigned the output of a PowerShell `if` block to an expected value, unrolling empty arrays to null and causing false mismatches. I prematurely described all fields as verified in a session action. The corrected direct-assignment check preserves empty arrays and verifies all twelve fields on each TODO. The original failed checks, correction and two empty-array controls are retained; this was not a server failure. `artifact-validation.json` records the six-file checkpoint before this README update and the later tracking receipt, not a hash of the final directory.
