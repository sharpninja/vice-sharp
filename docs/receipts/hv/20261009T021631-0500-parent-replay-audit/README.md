# Unintended Codex queue replay: bounded recovery audit

Evidence captured locally on PAYTON-LEGION2, 2026-10-09T02:18:08-05:00.

## Result and limits

I invoked the parent stop hook without its documented drain-disable flag.
It reported 11 successful replays, then removed those files from the pending
queue. This contradicted my statement that I would preserve the pending records.
This audit does not excuse that error or claim the replay was harmless.

Retained evidence accounts for 11 candidate records: six session submissions,
one dialog write and four triage reports. Nine filenames appear in the earlier
reviewer's actual queue inventory; two later creation receipts account for the
remaining filenames. Each has an original tool-output reference. This is a
recovered operation map, not a byte-complete pre-drain manifest.

The session operations link to five historical turns. Current primary readback
shows three completed and two canceled. Without complete before-images, neither
status changes nor unchanged historical content can be established. All four
associated triage reports remain present in the authoritative McpServer workspace
with their October 8 creation timestamps. This does not establish absence of
duplicate reports or other effects.

The full contents of the eleven original queue files were not recovered.
The installed plugin uses `Remove-Item` after successful replay and does not
archive those payloads in that path. No restoration or historical mutation was
performed. Automatic draining remains disabled in the current parent console.

## Evidence

- `recovered-operation-map.json`: all eleven filenames, linked request/report
  IDs, original rollout lines and source-event fingerprints.
- `retained-queue-inventory-extract.json`: exact earlier reviewer tool result
  listing nine pending records and the separate quarantined files.
- `affected-turns-primary-readback.json`: current primary records for five turns.
- `triage-primary-readback.json`: supported native status readback of four reports
  from `F:\GitHub\McpServer`.
- `audit-result.json`: observations, source anchors and unresolved limitations.
- The preceding `20261009T014822-0500-opus-remediation-resume` receipt directory
  retains the actual stop-hook disclosure and protected-history/native checks.

One read-only triage lookup through the wrapper used the ViceSharp routing
context and returned not found. The supported native triage-status calls with
the explicit McpServer workspace retrieved the existing reports. No raw REST
or storage mutation was used.

## Goal and review state

The original complete ViceSharp plan is unchanged. Its required Opus review
ended with exit 1 and a provider quota error; reset was reported as
2026-10-09T05:40:00-05:00. No reviewer is live, no new retry was launched, and
no implementation, requirement, plan or goal gate is accepted by this audit.
No product/native source, canonical requirements, services or credentials were
changed. This audit itself has not received independent hostile acceptance.
