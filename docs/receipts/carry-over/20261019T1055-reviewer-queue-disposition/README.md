# Disposition of the four historical reviewer-queue records (D7 carry-over)

Written 2026-10-19T10:52:49 local (measured file write time; the folder stamp `T1055` was estimated by the author before writing and is a label, not a measurement) on PAYTON-LEGION2 by Claude Code (claude-opus-5-5), session `ClaudeCode-20261019T152439Z-plugin-session`, request `req-20261019T152609Z-prompt-b96d`. Authority: Payton's direct instruction in this session, "Refer to BDPv4", given in reply to the statement that these records' disposition was his call. Applied reading (inference, stated to Payton): BDPv4 "Team Utilization and Planning" says the team that uncovers problems resolves them on its own; profile section 4.6 forbids moving or deleting files without his clearance.

## Decision

All four records are preserved in place and classified **superseded**. None is replayed, moved, quarantined or deleted. Drain stays disabled (`MCP_FAILSAFE_DRAIN_DISABLED=1`, verified in this session). Replaying `6e35` would create a phantom duplicate turn, so it must never be replayed.

## Records

Folder: `.mcpServer/claude/reviews/20261008T073800-0500-phase1-oracle-contracts/failsafe/pending/` (workspace-relative, not tracked by git).

- `20261008T170046Z-session_submit-2c6e.yaml` (11,690 bytes, SHA256 prefix `5701B287CF8D61DA`). SubmitAsync for session `ClaudeCode-20261008T124507Z-phase1-oracle-contracts-hv`, turn `req-20261008T124548Z-001-independent-hostile-validation-phase1-mock` status `canceled`. Server state: the same turn exists, status `canceled`, response "Superseded by req-20261008T170045Z-prompt-d17e before it was completed." Already reflected.
- `20261008T170052Z-session_submit-6e35.yaml` (11,693 bytes, `325671E94C825A71`). SubmitAsync for the same HV session with turn `req-20261008T170045Z-prompt-b84d` status `in_progress`, title "# Noon catch-up: same logical local Opus hostile review". Server state: the HV session holds only turn `req-20261008T124548Z-001`; `b84d` does not exist. Its prompt is the one persisted as `req-20261008T170045Z-prompt-d17e` in the plugin session below. Hook duplicate under the wrong session.
- `20261008T170053Z-session_submit-f583.yaml` (16,981 bytes, `AE80AAE5975F5FED`). SubmitAsync for session `ClaudeCode-20261008T170040Z-plugin-session`, turn `req-20261008T170045Z-prompt-d17e` `in_progress`. Server state: that turn exists with 7 ordered actions, model `claude-opus-5-5`, later than the queued snapshot. Superseded.
- `20261008T171005Z-session_setTurnTitle-a8c8.yaml` (409 bytes, `2C2D53C58A79273F`). SetTurnTitleAsync for `d17e` with title "HV Opus max noon catch-up: Phase1 mocks + Phase2 feasibility backlog (A-D)". Server state: the turn's `queryTitle` equals that title; its action 5 records "server applied; plugin reported false-negative and left failsafe a8c8". Already applied.

## Evidence

- Record fields were parsed with `ConvertFrom-Yaml` (powershell-yaml 0.4.12) and hashed with `Get-FileHash` on 2026-10-19 about 10:52 local.
- Server readback: `sessionlog_query` (mcpserver plugin MCP surface), workspace `F:\GitHub\vice-sharp`, agent `ClaudeCode`, from `2026-10-08T12:00:00Z` to `2026-10-08T23:59:00Z`: totalCount 2, the two sessions above with the turn states quoted.
- The 2026-10-08 reviewer had already disclosed these records as "not replayed, not deleted" (turn d17e, action 6) and filed triage `triage-report-1773cfcbe2bc4b41999aedeef90ba863`.

## Not done here

Deletion or quarantine of the files needs Payton's explicit clearance (profile 4.6). F-02 (credential exposure) and the plugin pending queues are separate D7 items and are not disposed of by this record.
