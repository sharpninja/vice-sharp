# BUG-6 verification report: PowerShell fix does not reach the Claude path

Date: 2026-06-23
Reporter: ClaudeCode (vice-sharp workspace)
Server: http://PAYTON-LEGION2:7147, version `1.0.0+3378b59154554a820e636f405c83fb6f5ecb7422`, Healthy, nonce echo verified.
REPL global tool: `mcpserver-repl 6.1.3-local.20260622.4`.
Wrapper under test: `F:\GitHub\mcpserver-claude-code-plugin\lib\` (repl-invoke.sh, repl-invoke.ps1, Invoke-ClaudeMcpPlugin.ps1), modified 2026-06-23 08:29-08:34.

## TL;DR

The BUG-6 fix was applied to the PowerShell wrapper `repl-invoke.ps1` (functions `Invoke-WorkflowAppendActions`, `Invoke-ReplRaw`). That file is real and the fix looks correct, but the **Claude Code plugin never executes it on its main invoke path**. `Invoke-ClaudeMcpPlugin.ps1 -Command Invoke` shells out to the **bash** wrapper `repl-invoke.sh`, which still carries the original list-parsing bug. So from Claude's actual runtime, structured/inline-JSON action lists are still dropped, and server-side turn persistence is failing for the multi-line + non-filePath case.

The hard, proven findings are 1, 2, 3, and 4: the routing gap, the bash normalizer dropping JSON lists, the bash counter keying on filePath lines, and the bash persist path still emitting "Malformed YAML envelope" (server `invalid_envelope`) for any turn that carries an actions block. BUG-6 cause 2 is therefore not resolved end to end on the bash path, which is the only path Claude uses.

## Finding 1 (PROVEN, primary): the Claude invoke path uses bash, not the fixed PS file

`Invoke-ClaudeMcpPlugin.ps1` dispatches every command to bash scripts:

`lib/Invoke-ClaudeMcpPlugin.ps1` (dispatch switch):
- `Status`  -> `lib\mcp.claude.status.sh`
- `Invoke`  -> `lib\repl-invoke.sh`   (line 195)
- `CompleteTurn` -> `lib\final-response.sh`

`repl-invoke.ps1` (the file that received the BUG-6 fix) is only referenced by `lib/cache-manager.ps1:65`, never by the main invoke path. Its own header documents it as "PowerShell parallel of lib/repl-invoke.sh".

Consequence: the `Invoke-WorkflowAppendActions` and `Invoke-ReplRaw` fixes do not execute when Claude logs actions or completes turns. They only help callers that dot-source `repl-invoke.ps1` directly.

Fix options (pick one):
- Port the same two fixes into the bash `repl-invoke.sh` (see Finding 2 and 3), or
- Repoint `Invoke-ClaudeMcpPlugin.ps1` `Invoke`/`CompleteTurn` to `repl-invoke.ps1` instead of the bash scripts, or
- Have the bash `repl-invoke.sh` delegate list-bearing methods to the PS implementation.

Any plugin whose `Invoke` entry shells to `repl-invoke.sh` (claude-code confirmed; likely the other bash-shim plugins) has the same gap. Grep each plugin's invoke entry for `repl-invoke.sh` vs `repl-invoke.ps1`.

## Finding 2 (PROVEN): bash normalizer is YAML-only, drops inline-JSON action/dialog lists

`lib/repl-invoke.sh`:

```bash
# line 3223
_repl_normalized_actions_block()      { _repl_list_block_get "$1" "actions"; }
_repl_normalized_dialog_items_block() { _repl_list_block_get "$1" "dialogItems"; }
```

`_repl_list_block_get` (line 123) is a YAML-only awk parser. For inline JSON input `{"actions":[...]}` it finds no `actions:` block and returns empty, so nothing is persisted and `codeEdits` stays flat.

The JSON-capable parser `_repl_json_array_block_get` (line 160) already exists and is wired for `acceptanceCriteria` (lines 1479, 1492) but not for actions/dialogItems.

Proof (direct calls, same shell):
- `_repl_normalized_actions_block '{"actions":[3 items]}'` -> empty
- `_repl_json_array_block_get '{"actions":[3 items]}' actions` -> correct 3-item normalized YAML block
- End-to-end: `appendActions` with `{"actions":[...]}` returns `ok: true, codeEdits: 0` (dropped); the same payload as a YAML list returns `codeEdits: 2` (persisted).

Fix (mirror the acceptanceCriteria pattern):

```bash
_repl_normalized_actions_block() {
    local block
    block="$(_repl_json_array_block_get "$1" "actions" 2>/dev/null || true)"
    [ -n "$block" ] && { printf '%s\n' "$block"; return 0; }
    _repl_list_block_get "$1" "actions"
}

_repl_normalized_dialog_items_block() {
    local block
    block="$(_repl_json_array_block_get "$1" "dialogItems" 2>/dev/null || true)"
    [ -n "$block" ] && { printf '%s\n' "$block"; return 0; }
    _repl_list_block_get "$1" "dialogItems"
}
```

## Finding 3 (PROVEN): bash codeEdits / auditFiles counter keys on filePath lines, not actions

`_repl_workflow_append_actions` (`lib/repl-invoke.sh` line ~3640):

```bash
added="$(printf '%s\n' "$params" | grep -c '^[[:space:]]*filePath:' || true)"
```

Two problems:
1. It counts the raw `$params`, so inline-JSON input counts 0 (no `filePath:` lines) even when actions exist.
2. `grep '^[[:space:]]*filePath:'` matches empty `filePath: ""` lines too, so a `design_decision` with `filePath: ""` is counted as a file edit. Observed: a 2-action YAML block (one design_decision with `filePath: ""`, one edit with a real path) reported `codeEdits: 2` instead of 1.

Fix: compute the normalized block first, then count only non-empty filePaths from it:

```bash
actions_block="$(_repl_normalized_actions_block "$params")"
added="$(printf '%s\n' "$actions_block" | grep -E '^[[:space:]]*filePath:[[:space:]]*("?)[^"[:space:]]' | grep -c . || true)"
```

(then reuse `$actions_block` at the existing persist call instead of recomputing).

This is the bash analogue of the PS BUG-6 cause 1 ("non-filePath actions dropped / counter wrong"). The PS file was fixed; the bash file was not.

## Finding 4 (PROVEN): "Malformed YAML envelope" still fires on the bash persist path; turns do not reach the server

This is the bash analogue of BUG-6 cause 2 (re-indent mangling nested structures). The PS `Invoke-ReplRaw` re-indent was fixed; the bash `_repl_invoke_raw` / `_repl_persist_turn` path was not.

Every `appendActions` call that carries a non-empty actions block emits TWO documents. Captured verbatim from a single `appendActions` with one `design_decision` action:

```
type: error
payload:
  requestId: req-20260623T140133Z-777e
  code: invalid_envelope
  message: Malformed YAML envelope
  details:

---
type: response
payload:
  ok: true
  codeEdits: 1
```

The first document is the server (mcpserver-repl) rejecting the turn-submit envelope that `_repl_persist_turn` builds (the rich turn doc with the nested `actions:` / `filesModified:` block). The second is the local cache write succeeding. So the caller sees `ok: true` while the actions never reach the server.

Server truth: `GET /mcpserver/sessionlog/ClaudeCode/ClaudeCode-20260623T084500Z-bug6-bash` returns `turnCount: 0`, `turns: []`. The session exists but the turn carrying actions never persisted.

Contrast: earlier same-path sessions whose only non-empty submit was a simple single-line completeTurn (or whose appendActions block was empty because the inline JSON was dropped per Finding 2) did persist (`...-issue1-cc` turnCount 1, `...-issue1-repro` turnCount 2), read through the identical GET route. So the read route is correct and the 0 is real, and the trigger is specifically a turn-submit envelope that contains the nested actions block.

Root cause: the bash envelope builder (`_repl_invoke_raw`, line ~1010, `sed 's/^/    /'` uniform indent) plus the turn-doc construction in `_repl_persist_turn` / `_repl_turns_block` produce YAML that mcpserver-repl parses as `invalid_envelope` once a nested list/map (the actions block) is present. The same uniform-indent-plus-targeted-list-fix approach used for the PS `Invoke-ReplRaw` fix needs porting to bash, and the turn-submit doc must round-trip through `mcpserver-repl` without `invalid_envelope`.

Note this also means BUG-6 cause 2 is not actually resolved end to end for any path that submits actions through bash, which on the Claude plugin is the only path (Finding 1).

## Finding 5 (OBSERVED): PS-native path returns success but does not persist server-side

Test session `ClaudeCode-20260623T084100Z-bug6-ps` run by dot-sourcing the fixed `repl-invoke.ps1` and calling `Invoke-ReplMethod`:
- openSession, beginTurn, appendActions (non-filePath design_decision + verify), completeTurn (multi-line) all returned `True`.
- No "Malformed YAML envelope" on the multi-line completeTurn (PS re-indent fix holds locally).

Server truth: session `...-bug6-ps` is absent from `GET /mcpserver/sessionlog` entirely. `Invoke-ReplMethod` returning `True` reflects the local cache verb succeeding; the best-effort `client.SessionLog.SubmitAsync` server submit did not land. So validating the PS fix by return value alone is misleading; it must be confirmed against server `turnCount`.

## What is confirmed working

- The standalone multi-line completeTurn `response` document no longer throws "Malformed YAML envelope" by itself on either path (PS re-indent fix observed holding for the response-only case). This does NOT extend to turn-submit envelopes that carry an actions block: those still fail with `invalid_envelope` on the bash path (Finding 4).
- `listFr` area filter and the `repairPlaceholders` endpoint (separate from BUG-6) are fixed and verified in earlier rounds.

## Recommended order of fixes

1. Close the routing gap (Finding 1) so the Claude path runs fixed code at all.
2. Port the JSON-list normalizer (Finding 2) and the counter fix (Finding 3) into bash `repl-invoke.sh`.
3. Confirm and fix server-side turn persistence for multi-line / non-filePath turns (Finding 4), verifying against server `turnCount`, not local return values (Finding 5).

## How these were verified

All checks were run from the vice-sharp workspace against the live server with marker trust verified (signature + `/health` nonce echo). Requirements/session reads used REST GET for server truth; wrapper behavior was exercised by sourcing `repl-invoke.sh` / dot-sourcing `repl-invoke.ps1` and by direct internal-function calls. Probe artifacts created during testing were deleted (`FR-MED-090/100/102/110`).
