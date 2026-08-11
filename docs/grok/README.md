# docs/grok - Grok session archives

## 2026-08-11-vic20-exact-session

Full Grok session dump for VIC-20 / xvic parity work (session id `019fd0df-383d-7fc0-9f0a-69785afd78ed`).

### Package layout

| Path | Description |
|------|-------------|
| `session/` | **Complete raw Grok session directory** (byte-for-byte copy of `~/.grok/sessions/.../019fd0df-...`). All data files live here. |
| `session/chat_history.jsonl` | Model message log (platform format has no per-message timestamps) |
| `session/events.jsonl` | Timestamped session events (`ts` ISO fields: turns, tools, goals, MCP) |
| `session/updates.jsonl` | Timestamped ACP session updates (`timestamp` unix seconds: message chunks, tools, hooks) |
| `session/rewind_points.jsonl` | Rewind checkpoints |
| `session/hunk_records.jsonl` | Edit hunk records |
| `session/plan.md` / `session/goal/` | Live plan + goal harness state at copy time |
| `session/compaction/` | Compaction segment rollouts |
| `session/compaction_checkpoints/` | Compaction checkpoints |
| `session/compaction_requests/` | Compaction request payloads |
| `session/recap_requests/` | Recap request payloads |
| `session/terminal/` | Terminal command logs |
| `session/subagents/` | Subagent artifacts |
| `session/images/` `session/assets/` | Session media |
| `session/*.log` | Gate logs written into the session dir during the run |
| `plan.md` | Session BDPv4 plan snapshot (same as `session/plan.md` at archive refresh) |
| `goal/` | Goal harness snapshot (same as `session/goal/` at archive refresh) |
| `TRANSCRIPT.md` | Human-readable export derived earlier from chat history (not a raw session file) |

### Honesty notes

1. Marking `PLAN-VIC20-EXACT-001` done under a reduced "scoped Exact" bar was a false plan completion relative to the approved full xvic parity plan.
2. The first archive commit only shipped a subset (chat_history without timestamps, plan, goal, TRANSCRIPT). Timestamps were not stripped from chat_history; that file's format never carries them. Timed data is in `session/events.jsonl` and `session/updates.jsonl`. This package includes the full session tree so that omission is not repeated.

### GitHub packaging note

`session/updates.jsonl` is **234 MB**, over GitHub's 100 MB blob limit. Account LFS budget is also exhausted, so the archive commits the **exact same bytes** as `session/updates.jsonl.gz` (gzip).

- Original SHA256: `ED3B35E2C162BC67165E712861F7F8D56130BE3719F61A297831BEA5F1EDEC5A`
- Restore: `gzip -dk session/updates.jsonl.gz` (or Expand-Archive-equivalent gzip decompress)
- Working tree may still hold the uncompressed file locally; it is not committed as a plain blob.

