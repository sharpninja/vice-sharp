# Hostile validation: recovered refresh-docs export after MCP restart

You are the independent local Grok hostile reviewer for `F:\GitHub\vice-sharp`.

Run entirely from the local Grok CLI in this workspace. Do not use web search, web fetch, cloud workspaces, remote threads, remote CI, subagents, or Python. Use PowerShell.MCP for all shell and filesystem inspection. Do not commit, push, delete, clean, reset, or edit native VICE. Do not modify product, plan, requirement, or project documentation files. You may write only the exact hostile-review receipt files listed below.

There is no maximum turn limit and no artificial timeout. Continue until the review is complete or a genuine blocker is proven.

## Mandatory first actions

1. Execute the complete add-profile skill from `C:\Users\kingd\.codex\skills\add-profile\SKILL.md` before any validation. Read `C:\Users\kingd\.claude\profile\add-profile.md` first, then every non-Grok Markdown profile file in `C:\Users\kingd\.claude\profile` in full. Record the exact file count.
2. Read `AGENTS-README-FIRST.yaml`, `AGENTS.md`, `HANDOFF.md`, and `C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md` in full.
3. Verify the local Grok plugin source is `F:\GitHub\mcpserver-grok-plugin`, version 1.119.0, and matches the regenerated marker. Use source type `GrokCode`.
4. Open a dedicated Grok MCP session-log turn through the supported local Grok plugin. Never use raw REST or direct storage edits.
5. Use the supported failsafe workflow. At minimum, inspect `workflow.failsafe.status` before and after the review, drain pending Grok records if any, and report exact scanned/replayed/failed/quarantined/skipped counts. If any review session write fails, retain and replay the supported failsafe record and report its absolute path and SHA256.

## Request classification and scope

Classify this as project documentation and requirements-export work. The requested result is the recovered `$refresh-docs` slice only. No plan step, TODO, FR, TR, TEST, parity milestone, or product implementation is being claimed complete.

Native VICE is the immutable behavioral target and must not be modified. VICE `.vsf` is the canonical ViceSharp snapshot requirement and managed VSF staging already existed before this docs slice. UWP/Xbox implementation is out of current scope. Legacy requirements remain active and are not retired.

Active plans to inspect holistically:

- `docs/plans/PLAN-VICE-IMMUTABLE-001.md`
- `docs/plans/PLAN-VSFLOCKSTEP-001.md`

Relevant requirements include `FR-SNP-005`, `FR-VSFLOCKSTEP-001`, `TR-LOCKSTEP-VSF-001`, `TR-VSFLOCKSTEP-RESUME-001`, `TEST-SNP-002`, `TEST-VSFLOCKSTEP-001`, `FR-UISETVIS-001`, and `TEST-UISET-003`. Discover any additional applicable IDs from the MCP requirements surface or canonical docs.

## Claims to attack

### A. Requested result

1. All declared agent plugins in the regenerated marker are synchronized at version 1.119.0.
2. The Codex failsafe queue was drained with scanned 14, replayed 14, failed 0, quarantined 0, skipped 0, then read back pending 0 and quarantine 0.
3. The Grok failsafe queue was drained with scanned 14, replayed 14, failed 0, quarantined 0, skipped 0, then read back pending 0 and quarantine 0.
4. The old blocked Codex turn `req-20261004T164238Z-prompt-3fb2` was read back from `client.SessionLog.QueryAsync` as completed.
5. The previous Grok verdict turn was read back completed with its 9,605-character full verdict body, AGREE, accuracy 99, completeness 99, PASS 30, FAIL 0, but without request/response JSONL paths. That omission is disclosed and is why this fresh HV is required.
6. `workflow.requirements.generateDocument` with `format: wiki` and `docType: all` succeeded and regenerated `docs/Project/wiki`.
7. The exact returned Base64 ZIP was written to `docs/requirements/requirements-wiki-documents.zip`: 384,571 bytes, SHA256 `FA95B17F91200BA6636A0FE0F916FB523E68DC07F94A5AE03FFEACAEF6351068`, generated at `2026-10-04T19:59:26.0500958Z`.
8. `docs/Project/wiki/github/User-Guide.md` no longer says direct dispatch is case-sensitive. It now says direct `C64` and `C1541` dispatch is case-insensitive and other kinds require nested `yaml:`.
9. `docs/wiki.yaml` parses with schema `mcp-wiki-export/v1`, 37 documents, 37 navigation references, and zero duplicate IDs, duplicate targets, missing sources, missing navigation references, or non-GitHub targets.
10. The 11 refreshed Markdown files contain 73 local links and 0 broken links.
11. The ZIP has 42 entries, 924,487 uncompressed bytes, no duplicate entries, no path traversal, and no missing required manifest/navigation/User Guide entries.
12. `git diff --check` is clean.
13. The focused command below reports Total 8, Passed 8, Failed 0, Skipped 0:
    `dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~Vic20SettingsVisibilityNotificationTests|FullyQualifiedName~Vic20SettingsUiSurfaceTests" --logger "console;verbosity=normal"`
14. The traceability audit remains openly incomplete, not green: canonical 235, referenced 149, missing 86, noncanonical 1,011; FR 132/101/31, TR 59/32/27, TEST 44/16/28.
15. This recovery made no native VICE edits, no commit, no push, no done-state change, and no raw REST call.
16. The generated export and refreshed docs do not claim whole-machine x64sc/xvic parity, native VSF completion, or broader plan completion.

### B. Workspace rules

Attack compliance with local-only execution, PowerShell.MCP, no Python, supported plugin-only MCP mutations, native VICE immutability, no destructive cleanup, no commit/push, no handwritten YAML/JSON, exact receipts, and honest disclosure of red/open gates.

Two incidental plugin defects were submitted through supported triage and must not be hidden:

- `triage-report-a792a8aa104f40019984bea76194094d`: plugin restart proof references unavailable `client.SessionLog.GetAsync`; supported `QueryAsync` provided primary readback instead.
- `triage-report-7c80519c4c5d4189b55ee140cdb83de2`: `setTurnTitle` returned a typed-result false negative, wrote a failsafe, and that record then replayed successfully.

### C. Requirements

Verify the docs preserve these non-negotiable truths: native VICE is immutable; VICE behavior is the oracle; canonical ViceSharp snapshots are VICE `.vsf`; managed VSF staging is existing but incomplete and not product-exposed; legacy requirements are active; UWP/Xbox implementation is out of current scope. Confirm no FR/TR/TEST or acceptance criterion was falsely marked satisfied.

### D. Plans

Read both active plans and `HANDOFF.md`. Confirm their statuses remain open and evidence-bounded, no unchecked work is presented as complete, Phase 2 has not exited, Phase 3 xvic parity has not started, and only the narrow VIC-20 settings visibility gate is validated.

## Required independent checks

Re-read the actual files and diff. Re-run `git diff --check`, the focused 8-test command, and `tools\check_requirement_traceability.ps1`. Independently validate the wiki manifest, Markdown links, ZIP structure and hash, generated User Guide wording, plugin versions, failsafe queue depths, and current MCP session-log primary readback. Do not trust the claims above without evidence.

## Receipt and MCP persistence contract

Write only these reviewer artifacts:

- Request JSONL already supplied by parent: `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T200406Z-refresh-docs-export-final-local-grok.request.jsonl`
- Full Grok machine stream written by launcher: `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T200406Z-refresh-docs-export-final-local-grok.response.jsonl`
- Human-readable verdict: `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T200406Z-refresh-docs-export-final-local-grok.verdict.md`
- Structured verdict twin: `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T200406Z-refresh-docs-export-final-local-grok.verdict.json`

The same dedicated Grok MCP review turn must contain the complete verdict prose, every A/B/C/D finding, every PASS/FAIL/UNKNOWN/N/A, numeric accuracy and completeness, final structured JSON, and the absolute request/response JSONL paths. Do not summarize the verdict in MCP. Before completing the turn, query primary session-log state and confirm the full body plus both JSONL paths are visible. Complete the turn, then query it again. Report exact session ID, request ID, terminal status, body/path readback, pending/quarantine counts, and `PrimaryReadback`.

If the turn is immutable before receipt-path correction, do not hide the failure. Open a second proof turn that stores the completed-turn readback and links the first review turn. Both turns must complete.

## Verdict contract

- Default every claim to FAIL or UNKNOWN until re-proven.
- Show every FAIL and UNKNOWN explicitly.
- `OverallVerdict: AGREE` only when all applicable A/B/C/D claims pass, FAIL is 0, UNKNOWN is 0, AccuracyScore is at least 98, CompletenessScore is at least 98, both JSONL files exist, the full verdict and both paths have primary MCP readback, and the review turn is terminal.
- An AGREE validates only this recovered docs/export slice. It does not authorize a plan, TODO, requirement, parity milestone, or goal done-state change.
- End the final response with the verdict receipt path and `OverallVerdict`.
