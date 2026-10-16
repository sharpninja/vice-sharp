# Approved legacy tape correction: application receipt

Recorded 2026-10-09T14:28:23-05:00 by Codex. Requirements application is verified; independent acceptance is pending.

## Approved and applied

The operator approved the pasted proposal, matching the retained `20261008T150905-0500-legacy-tape-ac-proposal/README.md`. The exact approved delta has SHA-256 `5E4AF883A53EE1AE9ED5105B51F9AEE3A175D13E47464B2355BAF9B64DB9BC0A`.

- FR-RUNTIME-TAPE-002: six acceptance criteria.
- TR-TAP-EDGE-001: four acceptance criteria.
- TR-TAPE-EDGE-001: four acceptance criteria.
- TEST-TAPE-RAMP-001: four acceptance criteria and corrected title.
- TEST-TAPE-SENSE-001: five acceptance criteria and corrected title.

All five records are pending and all 23 criteria remain unsatisfied. Nothing was retired. No product code, test implementation, or native VICE source was changed by this application.

## Evidence

- `approval.json`: approval source and drift checks.
- `batch-request.json` and `batch-response.json`: exact supported atomic five-record update and response.
- `five-records.primary-readback.json` and `requirements-verification.json`: independent MCP readback, zero comparison errors, unchanged mappings and unrelated records.
- `effective-requirements.before.json` and `effective-requirements.after.json`: retained complete effective snapshots.
- `projection-backup-pins.json` and `projections-before/`: 108 copied and SHA-verified original projection files, retained before export.
- `export-response.json` and `wiki-export-response.json`: supported export results. The latter contains a ZIP payload, not JSON metadata.
- `projection-file-changes.json`: actual projection changes after export. The generated plan wiki picked up pre-existing canonical-plan addenda; no new plan acceptance was asserted.
- `projection-validation.json`: full traceability output and diff check.
- `source-pins.rechecked.json`: six immutable native and five managed input hashes checked unchanged.
- `parent-turn-primary-readback.in-progress.json`: primary session readback after application and profile recovery.

Canonical tape projection: `docs/requirements/legacy-tape-acceptance-correction.md`.

## Validation boundaries

`tools/check_requirement_traceability.ps1` returned exit 0, but reported 238 canonical IDs, 152 referenced, 86 missing references, and 1,011 noncanonical references. That is not a clean coverage gate. `git diff --check` returned exit 0. No test suite ran in this requirements-only slice.

The documented Markdown export option failed schema validation. The supported YAML/all request successfully generated the Markdown projections. The defect was recorded through failsafe and accepted triage as `triage-report-52b871ce6e7a4f2c8b157cde7c6f7da9`. The failsafe remains retained, with draining disabled.

After context compaction, Codex's marker-read redaction covered only the top-level key and missed its repeated authentication fields. Those fields appeared in tool output. This was disclosed and recorded in the parent MCP turn without repeating the credential. No secret-safe or whole-turn process-compliance claim is made. Credential rotation was not performed.

## Independent review and later gates

Local Claude Opus 5.5 max review: `docs/receipts/hv/20261009T141130-0500-approved-tape-ac-opus/`. At this receipt's timestamp, the review was still running and had no verdict. Its 94 pinned inputs had been rechecked unchanged.

The parent turn is `Codex-20261007T135626Z-plugin-session / req-20261009T190241Z-prompt-6646`.

This approval does not satisfy AC, accept implemented parity, approve the separate drive/media/video proposals, or complete the full goal. The native-safe harness, discriminating green mock gate, unchanged real-provider bindings, cumulative suites, native VSF, and real-emulator parity gates remain required.

## Final disposition, 2026-10-09T15:04:50-05:00

Opus finished with **DISAGREE**, accuracy **95**, completeness **88**: 34 PASS, 5 FAIL, 1 UNKNOWN, 1 N/A. Review process exit code 0 and CLI result `success` mean the review ran to completion, not that its acceptance gate passed.

The five failed assertions concern the generated em-dash separators (B6), inaccurate active-playback stop timing (C2), missing native scenarios (C3), negative controls that do not cover those scenarios (C4), and conflicting requirement meanings/statuses (C5). The unknown (A1.4) is preservation of TR/TEST fields omitted from the effective API, for which complete before-images were not retained. See every assertion and all 17 findings in `docs/receipts/hv/20261009T141130-0500-approved-tape-ac-opus/verdict.md` and its JSON twin.

The full verdict is present in both primary MCP turns. The parent readback exactly matches all 25,464 characters after CRLF-to-LF normalization (SHA-256 `B602CD3705CD238F8EA3A5D98B7BB3F005A433585ADCB1340519C3282BE8EBFB`). The reviewer's six parts preserve the same content plus one terminal LF; its turn is completed. Reviewer linkage: `ClaudeCode-20261009T191359Z-plugin-session / req-20261009T191402Z-prompt-e6c7`.

Post-review actions and limitations:

- F-08 was submitted through the supported failsafe and triage flow as `triage-report-ca7a4811fb4c421a860aae9a31e4f10e`. The report was accepted; the generator is not fixed, generated documents were not hand-edited, and the retained failsafe was not drained.
- Opus also disclosed its own marker redaction failure (F-15). The original `response.jsonl` contains two occurrences of the key. An exact-path `.gitignore` entry now excludes that original. It remains on disk, unchanged. `response.redacted.jsonl` is an exact copy except those two replacements; all 2,676 JSONL records parse. Proof is `parent-completion-and-redaction-proof.json` in the review directory. Neither agent's earlier tool output was retracted, and the key has not been rotated. Do not publish the raw stream.
- Parent rechecked all 94 original input pins after review: 94 unchanged. A scan of the other 137 receipt files found no occurrence of the exposed key. `git diff --check` returned 0.
- Parent found only one reviewer text entry containing both numeric completion percentage and ETA: the initial entry. The required ongoing percentage/ETA updates were not supplied. This additional review-process failure is not included in the reviewer's original assertion counts; no retrospective entries were fabricated.

No criteria were silently rewritten after the review, and nothing was marked satisfied or done. Native VICE remains the immutable target. A separately approved corrective delta is needed before advancing the requirements gate; some findings also affect records outside the five approved here.
