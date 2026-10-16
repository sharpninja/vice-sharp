# Capacitor correction and guard contract draft

Codex on PAYTON-LEGION2. Source/readback work measured through 2026-10-11T08:06:45-05:00.

**Unapplied, unapproved, not yet HV-ready. No emulator, mock, requirement-application or whole-plan gate completed.**

## Material change

Pinned VICE disproves the live `FR-CPU-004/ac-06` approximation of 350 microseconds. The corrected draft preserves its ID but specifies executed CPU-cycle deadlines, inclusive jitter, input read behavior, repeated writes and strict expiration boundaries. It also corrects the description's output-pin wording to floating-input pins. See [source audit and eight proposed case groups](CAPACITOR-SOURCE-AND-CASES.md).

The [exact complete 14-record draft](proposed-field-delta.json) is a version of the preceding [consolidated draft](../20261011T072531-0500-tape-state-composite/README.md), not another operation to apply after it. Preserve older receipts as history. A later approved application uses one reviewed exact packet against its preimages, not successive application of overlapping drafts.

Counts: 66 before AC, 90 after AC, all 66 IDs retained, 56 texts rewritten, 10 unchanged, 24 new. There are 82 coverage rows and 104 proposed names: five inherited media placeholders, two approval-dependency groups and 97 other proposed names. None is claimed to be an executable test here. Eight unchanged CPU/SX-64 criteria remain mandatory under the whole plan despite receiving no new amendment-specific coverage row.

[R0/R1 guard contract](R0-R1-GUARD-CONTRACT.md) specifies typed/presence-aware comparisons, exact approval binding, mapping targets, lost acknowledgements, partial mapping outcomes, complete readback and eighteen mock/control groups. It retains the original requirement for new approval before compensating writes and does not claim separate APIs share a transaction.

## Receipts

- [Fresh typed preimages](typed-record-preimages.json): all fourteen records read through the supported Codex plugin from 2026-10-11T08:03:02-05:00 through 2026-10-11T08:03:20-05:00. Raw observations retained. Only known read-synthesized createdAt/updatedAt fields are excluded from stable preservation comparison.
- [Preparation checks](preparation-checks.json): 112 effective fields matched with value/presence comparisons; all fourteen stable typed preimages matched the preceding packet; six native source files matched the immutable commit's blobs. Contains actual expected/observed values, criteria-change ledger and native pins.
- All three JSON files were built from native PowerShell objects, saved using apply_patch, parsed and compared to that serialization after documented CRLF/LF and terminal-newline normalization.
- Original plan SHA256 remains `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`. Goal remains its full completion with HV agreement, not tape-only completion.

These checks are preparation evidence. They are not the G01-G18 executable guard suite, a mock-green emulator gate, a real native comparison or HV acceptance.

## Remaining path

1. Finish all tape/media fixture matrices and public assertion/provider contracts, including five inherited media placeholders and retained turbo-loader workloads.
2. Finish the guard fixture specification and resolve current supported update/mapping schemas and required failure semantics; do not infer them. This remains R0 planning, with no test/coordinator implementation.
3. Reconcile root HANDOFF and full-plan tracking, preserving all unresolved prior findings and approval boundaries. Prior two-TODO updates remain evidence, not complete-backlog proof.
4. Freeze the whole corrected packet, obtain local Opus 5.5 max AGREE at >=98/98 and zero FAIL/UNKNOWN with full MCP-read-back verdict, then obtain explicit exact expanded-delta approval.
5. After R0 approval and before any R1 mutation, implement guard tests first, prove assertions discriminate correct/wrong scripted outcomes in a green suite, then implement and verify the real guard/coordinator. Apply only under R1 and independently verify requirements. Follow R2/R3 and every original full mock/real/native-VSF/xvic/x64sc/UI/handoff gate. Phase 3/4 still require their separate start approval. This ordering clarifies the preceding draft's ambiguous placement of executable guards before review: it does not override the original R0 prohibition on test/product implementation.

The latest independent tape review remains DISAGREE 93/84, six failed assertions. This draft does not close those findings by self-assessment. No new HV ran. No requirement/mapping/status/satisfaction, native/product/test implementation, commit, push, deployment or queue-replay action occurred in this preparation.

Parent MCP session `Codex-20261007T135626Z-plugin-session`, turn `req-20261011T124551Z-capacitor-guard-remediation`.
