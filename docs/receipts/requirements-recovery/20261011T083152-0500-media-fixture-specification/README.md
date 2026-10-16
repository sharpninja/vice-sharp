# Media fixture specification: R0 draft, not acceptance

Prepared locally by Codex on PAYTON-LEGION2. Verification started 2026-10-11T08:49:48-05:00.

This supplements the exact fourteen-record proposal in [the capacitor/guard draft](../20261011T080155-0500-capacitor-guard-remediation/README.md). It replaces five label-only media coverage entries with 20 explicitly named planned groups. It changes no requirement text, applies no records and claims no mock-green or native-parity gate.

## Read in order

1. [D64-CONTRACT.md](D64-CONTRACT.md): deterministic image recipes, geometry, all 256 error-map byte values, read-only precedence, write/reopen effects, and fault boundaries.
2. [MEDIA-COVERAGE.md](MEDIA-COVERAGE.md): IEC digital matrices, ordered ATN/motor effects, DOS versus buffered-mode separation, evidence comparison and acceptance boundaries.
3. [coverage-supplement.json](coverage-supplement.json): the five original entries, their replacement bindings, and the exact eight tape criteria and 41 inherited tape groups.
4. [preparation-evidence.json](preparation-evidence.json): source pins, live-requirement preparation snapshot, and 16 deterministic in-memory D64 recipe hashes.
5. [verification.json](verification.json): readback, source identity, recipe recomputation, whitespace and traceability results. Its file hashes cover the five preceding artifacts, not itself.

## Evidence and limits

All 13 selected materialized native files match Git blobs from VICE `cc67418cba58e71d8e70fd353e9de47199ad2075`. This is selected-file identity, not a clean native checkout or executable proof. The native checkout remains dirty and was not changed here.

All 16 recipe hashes were reproduced from the documented byte formula. No fixture was written to disk. No mock tests, real-emulator tests, native oracle, build or new hostile review ran. The proposed groups are not executable coverage yet.

`git diff --check` returned 0 for tracked changes. Separate checks cover the new untracked artifacts. The traceability diagnostic completed but reports 86 canonical IDs absent from source/test references and 1,011 noncanonical references. Those are unresolved audit findings, not a passing traceability gate; this slice does not establish their origin or remediate them.

## Still required before acceptance

- Complete concrete tape matrices and exact observer/provider interfaces, without dropping any of the 41 inherited groups or dependent tape requirements.
- Supply pinned true-drive formatted media, firmware, host workloads and native traces; resolve runtime fault-observation mechanisms.
- Reconcile D64 extended-track scope and the separately proposed drive requirements. `TR-MEDIA-D64-001` and `TEST-D64IO-001` remain absent; this supplement does not create or map them.
- Finish guard schemas and reconcile the canonical handoff/TODO state into the whole corrected R0 packet.
- Obtain whole-packet Opus max HV, then explicit approval for the expanded exact requirement delta. Only then follow the approved mocks-first and real-provider gates.

The midnight review remains DISAGREE, accuracy 93 and completeness 84. These later draft documents have not been re-reviewed. The original full-plan objective remains open; this packet is neither a replacement objective nor approval to begin xvic or settings implementation.
