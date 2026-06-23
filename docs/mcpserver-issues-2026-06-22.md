# MCP Server / plugin issues encountered (compiled 2026-06-22)

This is a consolidated bug report for the MCP Server + agent-plugin stack, compiled
from work in the **vice-sharp** workspace. It is written for the MCP/plugin
maintainers who are triaging these.

## Provenance and honesty note

For most of the working session I did not maintain the MCP session log at all
(a process failure on my part). After that was called out, I bootstrapped the
session log and reconstructed the work into MCP on **2026-06-22 ~17:22-17:29Z**
(session `ClaudeCode-20260622T121800Z-media-export-aireview`), and in doing so
reproduced two of the bugs below **live**. So the items here come from:

- **Live repro (2026-06-22)** - reproduced this session against the running server
  (`http://PAYTON-LEGION2:7147`, reported `Healthy`, nonce echo verified). Issues 1
  and 4 carry exact evidence; Issue 6 (slow/flaky) was felt as per-call latency.
- **Recorded memory** - notes persisted during prior sessions in this workspace
  (dated by memory age).
- **Prior-session handoff summary** - the carryover summary of the earlier part of
  this same conversation (last couple of days).
- **Repo artifacts** - failure markers committed into the repo (README, handoff).

Each item is labelled with its source and age. Treat the older items (Issue 7,
Issue 8) as possibly-already-fixed; Issues 1-6 are the actionable "recent" set,
with 1 and 4 confirmed live today.

Environment context (from `AGENTS-README-FIRST.yaml`): Claude uses
`mcpserver-claude-code-plugin`; the repl wrapper is `Invoke-ClaudeMcpPlugin.ps1
-Command Invoke` over `mcpserver-repl --agent-stdio`; live server at
`http://PAYTON-LEGION2:7147/mcpserver`.

---

## Recent cluster (last few days)

### Issue 1 - Session-log list params silently drop when passed as inline JSON

- **Status:** REPRODUCED LIVE 2026-06-22 (and not limited to the PowerShell
  wrapper - the bash `repl-invoke.sh` 1.1.2 has it too).
- **Live evidence:** `repl_invoke workflow.sessionlog.appendActions` with a 3-item
  JSON array `{"actions":[...]}` returned `ok: true` / `codeEdits: 0` (dropped).
  The identical list as YAML returned `ok: true` / `codeEdits: 7` (persisted).
- **Component:** repl wrapper list parser (`_repl_list_block_get`); methods
  `workflow.sessionlog.appendActions`, `workflow.sessionlog.appendDialog`.
- **Severity:** High (data loss + cascading turn-close failure).
- **Symptom:** Passing list params as inline JSON (`{"actions":[...]}` /
  `{"dialogItems":[...]}`) returns `ok:true` but persists an **empty** list. The
  wrapper's list-block parser is YAML-only, so it silently ignores the JSON array.
- **Impact / cascade:** Because the turn then has zero action/decision/commit
  items, `workflow.sessionlog.completeTurn` is rejected with **HTTP 400 "Cannot
  close session turn ... contains no decision, action, or commit items."** The
  failed `completeTurn` also flips the local `current-turn.yaml` status to
  `completed`, so every subsequent persist re-triggers the same 400 - the turn is
  effectively stuck until a real action lands.
- **Workaround:** Pass list params via a YAML file and `-ParamsPath <file>`
  (piping via stdin fails parameter binding). Inline `-Params '{json}'` works only
  for SCALAR fields (openSession, beginTurn, updateTurn, query, todo).
- **Suggested fix:** Accept JSON arrays in the list parser (or fail loudly instead
  of returning `ok:true` with an empty block).
- **Source:** Recorded memory `feedback-mcp-sessionlog-yaml-lists` (~7 days old).

### Issue 2 - completeTurn breaks on a multi-line response ("Malformed YAML envelope")

- **Component:** repl envelope serialization for `workflow.sessionlog.completeTurn`.
- **Severity:** Medium.
- **Symptom:** A multi-line `response` value containing colons, numbered lists, or
  pipe characters breaks the repl envelope with "Malformed YAML envelope", and the
  turn does not close.
- **Workaround:** Pass a single-line `response` via inline `-Params
  '{"response":"one line ..."}'`.
- **Suggested fix:** Properly quote/encode multi-line scalar values in the envelope
  (block scalars or JSON-string encoding).
- **Source:** Recorded memory `feedback-mcp-sessionlog-yaml-lists` (~7 days old).

### Issue 3 - Hook auto-creates orphaned prompt-turns (per user message AND per background task-notification)

- **Component:** Plugin hook layer (turn lifecycle).
- **Severity:** Medium (bookkeeping burden; orphaned open turns).
- **Symptom:** A `*-prompt-XXXX` turn is auto-created for every user message AND for
  every `<task-notification>` background-completion message, separate from any turn
  opened explicitly with `beginTurn`. A long task with several background runs
  therefore leaves several open prompt-turns, each of which needs an action +
  `completeTurn`.
- **Impact:** Orphaned open turns accumulate; closing them requires re-pointing
  `current-turn.yaml` (under `cache/workspaces/<ws>/sessions/<sid>/`) to each
  orphan, completing it, then re-pointing back.
- **Suggested fix:** Do not auto-open a billable turn for background
  task-notifications, or auto-reconcile/close prompt-turns that received no work.
- **Source:** Recorded memory `feedback-mcp-sessionlog-yaml-lists` (~7 days old).

### Issue 4 - Single requirement create drops `acceptanceCriteria` (batch create persists it)

- **Status:** REPRODUCED LIVE 2026-06-22.
- **Live evidence:** `createFr FR-MED-004` with two `acceptanceCriteria` returned
  `success: true`, but `getFr FR-MED-004` showed `acceptanceCriteria: []`. The same
  shape via `createFrBatch` (FR-MED-002) persisted both criteria intact.
- **Component:** `workflow.requirements.createFr` / `createTr` / `createTest`
  (single-record path) vs the `*Batch` path.
- **Severity:** Medium (silent data loss on the single-create path).
- **Symptom:** A single create call does **not** persist the `acceptanceCriteria`
  field, whereas the batch create (`createFrBatch`, etc.) for the same records
  does persist it.
- **Workaround:** Use the batch create methods when acceptance criteria matter.
- **Suggested fix:** Persist `acceptanceCriteria` on the single-create path too (or
  reject the field with an error if unsupported there).
- **Source:** Prior-session handoff summary (this conversation's carryover; last
  couple of days). Mechanism not independently re-verified.

### Issue 5 - `appendActions` effectively must be issued one action at a time

- **Component:** `workflow.sessionlog.appendActions`.
- **Severity:** Low/Medium (throughput + likely the same root cause as Issue 1).
- **Symptom:** Recorded as having to append actions one-at-a-time rather than as a
  multi-item batch. Likely the same YAML-list-parsing root cause as Issue 1 (a
  multi-item JSON list silently collapsing), but listed separately because it was
  noted as its own friction point.
- **Workaround:** Issue one action per call (or YAML list via `-ParamsPath`).
- **Source:** Prior-session handoff summary (last couple of days). Probably a
  duplicate of Issue 1 - worth confirming.

### Issue 6 - `mcpserver-repl --agent-stdio` is slow and flaky

- **Component:** `mcpserver-repl --agent-stdio` transport.
- **Severity:** Medium (reliability/latency; erodes trust in logging).
- **Symptom:** Recorded as "slow / flaky" - intermittent failures and high latency
  on requirements + session-log calls over the stdio repl.
- **Impact:** Encourages skipping/batching session-log writes, which then hits the
  turn-close failures above.
- **Source:** Prior-session handoff summary (last couple of days). No precise repro
  captured; would need request-timing/retry logs to characterise.

---

## Older / historical (include for context; may already be fixed)

### Issue 7 - openSession fails (HTTP 500 DbUpdateException) when the session ID is built via shell variable expansion

- **Component:** `workflow.sessionlog.openSession`.
- **Severity:** High when it bites, but easy to avoid.
- **Symptom:** Building the session ID with shell variable expansion (`$TIMESTAMP`)
  inside a heredoc/JSON payload produced repeated **HTTP 500 DbUpdateException**
  errors. A hardcoded literal session ID
  (`ClaudeCode-20260413T211200Z-vicesharp-phasea`) worked first try.
- **Likely root cause:** bash interpolation corrupting the timestamp format /
  introducing stray encoding before it reaches the server, yielding an ID that
  violates a DB constraint - but the server surfaced it as an opaque 500 rather
  than a validation error.
- **Workaround:** Hardcode the session ID as a literal string in a quoted heredoc.
- **Suggested fix:** Validate the session-ID format server-side and return a 400
  with a clear message instead of a 500 DbUpdateException.
- **Source:** Recorded memory `Session log IDs must be hardcoded strings` (~69 days
  old - quite old; may be stale).

### Issue 8 - MCP health-nonce failure pauses all TODO/session writes (fell back to local docs)

- **Component:** MCP health/nonce + marker-trust verification.
- **Severity:** High (full loss of MCP traceability for the affected slice).
- **Symptom:** A subagent reported an **MCP health nonce failure**. Per
  `AGENTS-README-FIRST.yaml` ("Agents must stop MCP usage after any signature or
  nonce mismatch"), all MCP TODO/session writes were paused and the work was
  recorded only in local fallback docs.
- **Impact:** Requirements/TODO/session-log traceability for that slice never made
  it into the MCP Server; CI traceability checks would see drift between local docs
  and the server.
- **Open question for maintainers:** was this a genuine signature/nonce mismatch
  (server-side rotation / clock skew), or a false positive in the plugin's nonce
  echo check? The `/health` endpoint is documented to "echo a caller nonce exactly
  when one is supplied" - a mismatch there is what trips the stop.
- **Source:** Repo artifact `README.md` (completion-dashboard note, ~2026-05-21
  slice); related marker contract in `AGENTS-README-FIRST.yaml`
  (`health_nonce_endpoint: /health`, `MCP_UNTRUSTED` / `MCP_PLUGIN_UNAVAILABLE`
  fallbacks). NOT first-hand this session.

---

## Summary table

| # | Title | Component | Severity | Source | Age |
|---|-------|-----------|----------|--------|-----|
| 1 | List params silently drop as inline JSON | repl list parser / appendActions+appendDialog | High | memory | ~7d |
| 2 | completeTurn breaks on multi-line response | repl envelope | Medium | memory | ~7d |
| 3 | Orphaned auto-created prompt-turns | hook turn lifecycle | Medium | memory | ~7d |
| 4 | Single create drops acceptanceCriteria | requirements create (single vs batch) | Medium | handoff summary | last few days |
| 5 | appendActions one-at-a-time | appendActions (likely dup of #1) | Low/Med | handoff summary | last few days |
| 6 | repl --agent-stdio slow/flaky | stdio transport | Medium | handoff summary | last few days |
| 7 | openSession 500 on `$VAR` session ID | openSession validation | High-when-hit | memory | ~69d (old) |
| 8 | health-nonce failure pauses MCP writes | health/nonce verification | High | repo README | ~2026-05-21 |

## What would help me give you better repros

If you want hard repros for the recent cluster, point me at a throwaway workspace +
the current plugin build and I will:
- run `appendActions` with inline-JSON vs YAML `-ParamsPath` and capture both the
  `ok:true`-but-empty persist (Issue 1) and the `completeTurn` 400 cascade;
- run `completeTurn` with a multi-line response to capture the "Malformed YAML
  envelope" (Issue 2);
- run single vs batch `createFr` with `acceptanceCriteria` and diff what persists
  (Issue 4);
- capture request timings over the stdio repl (Issue 6).
