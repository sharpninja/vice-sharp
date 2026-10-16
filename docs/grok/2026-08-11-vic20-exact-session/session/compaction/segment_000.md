# HISTORICAL -- DO NOT EDIT
# Record of compaction segment 000 (detail=verbose) from this same task.
# Use read_file or grep to look up details, but do not modify.

## Segment metadata
- Index: 000
- Turn count: 593
- Timestamp: 2026-08-05T09:13:35Z

## Turn statistics

- Turns: 593 (Assistant=180, Function=395, Human=17, System=1)
- Tools used: read_file (115), run_terminal_command (103), grep (58), search_replace (46), use_tool (30), get_command_or_subagent_output (19), write (12), todo_write (6), list_dir (3), exit_plan_mode (2), search_tool (1)
- Unique target files (102): C:\Users\kingd\.claude\profile, C:\Users\kingd\.claude\profile\PROFILE.md, C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md, C:\Users\kingd\.claude\profile\approve-before-execute.md, C:\Users\kingd\.claude\profile\bring-the-receipts.md, ... and 97 more
- Tool errors: 0
- Verbose-render size estimate: 1,369,269 B
- Last assistant response excerpt: "Investigating how the A–Z side strip calls into the browse ViewModel and why filtering/jumping fails."


## Summary (curated by compaction step)

Summary:
1. Primary Request and Intent: Session: new GrokCode session on vice-sharp; load profile, bootstrap MCP, resume from HANDOFF. Plan completion: pickup HANDOFF and plan completion; user later put all RomM features in scope; review notes: merge romless, finish Octopus, origin=GitHub approved. Execute approved plan (1 TODO reconcile, 2 DeployXboxLocal smoke, 3 full baseline). Product: ESC closes virtual keyboard; Controls should also show keyboard shortcuts (Ask mode only); how to reach RomM browser (documented gap: menu entries missing); fix dropped RomM menu entries; Library UI was old list—ship Game-Pass Library from romm WIP; start RomM on PAYTON-DESKTOP via WSMain/creds; A–Z side index does not filter (current work).

2. Key Technical Concepts: ViceSharp C#/.NET 10 C64 emulator; PLAN-XBOXUWP Tier D UWP head; PLAN-ROMM RomM client (Library.ViewModels + RomM adapter); Byrd/BDP tests-first; MCP via mcpserver-grok-plugin Invoke-McpPlugin.ps1; origin=GitHub; DeployXboxLocal VS MSBuild Release-UWP; PublishAot opt-in (CsWinRT kills reflection Binding); x:Bind/VirtualKeyboardRow; RESTORE→NMI; Controls remapping IBindingStore; model ROM readiness; WinRM PAYTON-DESKTOP; docker compose RomM stack.

3. Files and Code Sections: Branch feat/xbox-uwp-app (tip moved through 0c83627, cbb72be, etc. to latest Library commits). HANDOFF.md updated 2026-08-05. Xbox residuals: FEAT-XAOTBIND-001, FEAT-XOCTOPUS-001, FIX-XKBDNMI-001, FEAT-XCTRLBIND-001, FEAT-XROMPICK-001, FEAT-XKBD-ESC-001 (HandleEscapeShellKey). Merged feat/romm-integration (3804a1d). HomePage: Library/Lists/CSDb restored OnLibrary/OnLists/OnCsdb. LibraryPage.xaml/.cs + XboxCoverImageLoader.cs from romm worktree WIP (Game-Pass grid). Avalonia MainWindow PAR without Architectures/Chips. build/_build.csproj Cryptography.Xml 10.0.10. azure-pipelines Octopus LEGION2. Active diagnosis: LibraryBrowseViewModel.JumpToLetterAsync returns if `_charIndex` missing key; gateway ParseCharIndex from RomM `char_index` extension; BuildQuery omits JumpLetter; BuildAzStrip wires buttons to JumpToLetterAsync.

4. Errors and Fixes: PublishAot/{Binding}→x:Bind opt-in; merge dropped HomePage RomM entries→restored; Library list UI was old while Game-Pass lived uncommitted on romm worktree→copied; NETSDK1005 after UWP assets flip→restore; Deploy NU1903→10.0.10; package empty InstallLocation→register layout under LOCALAPPDATA; AvaloniaBoundary Architectures ref→profile-id PAR; Octopus YAML clobbered by merge→re-applied; baseline first run 2 fails then 3211 pass/13 skip. RomM compose at C:\deploy\RomM empty—real project C:\deploy\RomM-clean (already Up 6 days).

5. Problem Solving: Track 1 Xbox residuals + Track 2 RomM via merge then UI restore. A–Z: char_index empty or incomplete → silent no-op; JumpLetter on LibraryQuery unused; user expects filter/jump to work—client fallback (search prefix / client index) under evaluation. Library screenshot status “Enter your RomM UI” matched old connect UX.

6. All User Messages: new session; pickup handoff and plan completion; all RomM features in scope; plan approved (romless merge, finish Octopus, origin GitHub); 1 then 2 then 3; ESC closes virtual keyboard; Controls should show keyboard shortcuts [image]; how get to RomM browser; fix missing RomM entries; Library is OLD version [image]; start RomM on PAYTON-DESKTOP via WSMain (creds ~/.creds); A–Z index on side doesn’t filter.

7. Pending Tasks: Fix A–Z side index so it filters/jumps. Optional from prior: Controls keyboard-shortcut rows (Ask only, not implemented); B-controller QA; S42 deferred; MCP TODOs already closed 30/30 (S42 open).

8. Current Work: Diagnosing A–Z strip. JumpToLetterAsync needs `_charIndex` from server CharIndex; if key missing, returns immediately—explains no filter. Gateway maps char_index from page.ExtensionData; may be empty live. Page BuildAzStrip clicks call JumpToLetterAsync. LibraryQuery.JumpLetter not used by BuildQuery.

9. Optional Next Step: Implement fix for non-filtering A–Z: make JumpToLetterAsync fall back when char_index missing (e.g. search prefix / use JumpLetter / client-side index), plus test, then build/deploy Library. Quote: “The index on the side doesn’t filter.”

## Verbatim turns

### Turn 0 (System)
You are Grok 4.5 released by xAI. You are an interactive CLI tool that helps users with software engineering tasks. Your main goal is to complete the user's request, denoted within the <user_query> tag.

<action_safety>
Weigh each action by how easily it can be undone and how far its effects reach. Local, reversible work such as editing files and running tests is fine to do freely. Before executing any actions that are hard to reverse, reach shared external systems, or are otherwise risky or destructive, check with the user first.

Confirming is cheap; a mistaken action is not (such as lost work, messages you cannot unsend, deleted branches). For those cases, take the context, the action, and the user's instructions into account; by default, say what you plan to do and ask before doing it. Users can override that default — if they explicitly ask you to act more autonomously, you may proceed without confirmation, but still mind risks and consequences.

One approval is not a blank check. Approving something once (e.g. a git push) does not approve it in every later situation. Unless the user has authorized the action in advance, confirm with the user.

Here are some examples of risky actions that warrant user confirmation:
- Destructive operations such as removing files or branches, dropping database tables, killing processes, `rm -rf`, discarding uncommitted work
- Irreversible operations such as force-pushes (including overwriting remote history), `git reset --hard`, amending commits already published, removing or downgrading dependencies, changing CI/CD pipelines
- Actions others can see, or that change shared state: pushing code; opening, closing, or commenting on PRs and issues; sending messages (Slack, email, GitHub); posting to external services; changing shared infrastructure or permissions

If you find unexpected state — unfamiliar files, branches, or configuration — investigate before deleting or overwriting; it may be the user's in-progress work.
</action_safety>

<tool_calling>
- Use specialized tools instead of bash commands when possible, as this provides a better user experience. For file operations, prefer dedicated file tools (e.g., `read_file` for reading files instead of cat/head/tail, `search_replace` for editing and creating files instead of sed/awk). Reserve bash tools exclusively for actual system commands and terminal operations that require shell execution. NEVER use bash echo or other command-line tools to communicate thoughts, explanations, or instructions to the user. Output all communication directly in your response text instead.
</tool_calling>

<background_tasks>
For watch processes, polling, and ongoing observation (CI status, log tailing, API polling):
Use the `monitor` tool — it streams each stdout line back as a chat notification.
</background_tasks>

<output_efficiency>
- Write like an excellent technical blog post — precise, well-structured, and clear, in complete sentences. Most responses should be concise and to the point, but the quality of prose should be high.
- Same standards for commit and PR descriptions: complete sentences, good grammar, and only relevant detail.
- Prefer simple, accessible language over dense technical jargon. Explain what changed and why in plain language rather than listing identifiers. Stay focused: avoid filler, repetition, over-the-top detail, and tangents the user did not ask for.
- Keep final responses proportional to task complexity.
</output_efficiency>

<formatting>
Your text output is rendered as GitHub-flavored markdown (CommonMark). Use markdown actively when it aids the reader: bullet lists for parallel items, **bold** for emphasis, `inline code` for identifiers/paths/commands, and tables for short enumerable facts (file/line/status, before/after, quantitative data).
</formatting>

<user_guide>
Documentation about the Grok Build TUI — including configuration, keyboard shortcuts, MCP servers, skills, theming, plugins, and more — is stored as `.md` files in `~/.grok/docs/user-guide/`. When users ask about features or how to use the TUI, read the relevant file from that directory.
</user_guide>

### Turn 1 (Human)
<user_info>
OS Version: windows
Shell: pwsh
Workspace Path: F:\GitHub\vice-sharp
Today's date: 2026-08-05
Note: Prefer using relative paths over absolute paths as tool call args when possible.
</user_info>

<git_status>
This is the git status at the start of the conversation. Note that this status is a snapshot in time, and will not update during the conversation.
## [32mfeat/xbox-uwp-app[m
 [31mM[m HANDOFF.md
 [31mM[m azure-pipelines.ci.yml
 [31mM[m azure-pipelines.release.yml
 [31mM[m src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
 [31mM[m src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/AboutPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/AboutPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/InputMappingPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/SettingsPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 [31mM[m tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json
[31m??[m docs/S-Blox/
[31m??[m docs/reviews/code-review-20260714T001056552Z.md
[31m??[m docs/reviews/code-review-20260714T004256563Z.md
[31m??[m docs/romless-vic-badline-fix.md
</git_status>


### Turn 2 (Human)


<system-reminder>
As you answer the user's questions, you can use the following context (ordered from repo root to current directory - deeper files take precedence on conflicts):

## From: C:\Users\kingd\.claude\Claude.md
# Global Rules

## Development Process

**All plans must be created based on the Byrd Development Process and must strictly adhere to its tools and requirements.** This rule applies to every implementation plan written in any workspace; no plan is exempt regardless of size, urgency, or scope.

Follow the **Byrd Development Process** for all implementation plans. This means:

1. **Tests first**: Write unit tests covering the full acceptance criteria before writing implementation code.
2. **Validate with mocks**: Ensure tests pass using mocks/stubs before implementing real logic.
3. **Then implement**: Only after all tests are validated for correctness does implementation turn to actual code.
4. **All tests green**: Exiting any phase requires the entire test suite (current + previous) to pass.
5. **Requirements drive tests**: Tests are derived from functional and technical requirements, not from implementation details.

Reference: [Development-Process-draft-v3.pdf](https://github.com/sharpninja/McpServer/blob/main/docs/Development-Process-draft-v3.pdf) (raw: [direct PDF](https://github.com/sharpninja/McpServer/raw/main/docs/Development-Process-draft-v3.pdf)). Local mirror: `docs/Development-Process-draft-v3.pdf` in any McpServer workspace.

## Always Bring the Receipts

**Every claim of completed work must ship with machine-verifiable evidence.** Never assert "done", "fixed", "passes", or "verified" from a fix list, an intention, or memory of an edit; verify against the actual artifact after the change, then cite the evidence.

- Commands: show the actual output and exit code (build, test, protoc, lint, etc.).
- File changes: confirm on disk after editing (grep/diff/timestamp), not from the edit you meant to make.
- MCP/store updates: capture the query result that proves the new state (getTr, listMappings, generateDocument output), and regenerate any on-disk exports so they match the store.
- Reviewing claimed work (own or another agent's): verify mechanically first (grep, diff, compile, re-run); accept nothing on report.
- For tracked plan/TODO work, save receipts to a durable artifact (e.g. `docs/receipts-*.txt` or session-log actions) so reviewers can audit without re-deriving.
- Summaries and status lines must only claim what the body/artifact actually contains; every "addressed" item cites the line, file, or command output that proves it.

## Admin Commands

When multiple commands require elevated (admin/gsudo) privileges, batch them into a temporary `.ps1` script and execute with a single `gsudo pwsh -ExecutionPolicy Bypass -File <script>` call. Delete the script afterward. Never run more than 2 individual gsudo commands in sequence.

## Writing Style

**Never use em-dashes (`—`, U+2014) in any output.** Applies to chat responses, code comments, commit messages, PR descriptions, documentation, session-log notes, and any file Claude writes. Use one of these instead:
- Hyphen (`-`) for compound modifiers and inline asides
- Colon (`:`) when introducing a list or clause
- Period or semicolon when separating independent clauses
- Parentheses for true asides

Also avoid en-dashes (`–`, U+2013) except in numeric ranges.

## MCP Server Terminology

When the user uses these terms, they refer to MCP Server concepts unless the user explicitly says otherwise:

- **TODO / todo / todos**: MCP Server TODO items (not Claude Code's TodoWrite list, not a markdown TODO file, not GitHub issues). "List open TODOs" means list MCP Server TODOs with `done: false`.
- **Session / session log**: MCP Server session log (not a shell session, Claude Code session, or tmux session). Every meaningful unit of work should create and update a session log turn.

### Access rules: NEVER access storage files directly

- **NEVER read or write `todo.yaml` / `docs/todo.yaml` / any TODO storage file directly** with Read, Edit, Write, Grep, Bash `cat`, or any other tool. The MCP Server is the only allowed interface. Reading the file for "just a quick lookup" is still a violation.
- **NEVER edit session-log files directly.** Use the MCP Server session-log API.
- Always route TODO and session operations through one of:
  - MCP Server REST API (`/mcpserver/todo`, `/mcpserver/sessionlog`) with `X-Api-Key` from `AGENTS-README-FIRST.yaml`
  - MCP Server MCP tools (`mcp_todo_query`, `mcp_todo_get`, `mcp_todo_update`, `mcp_session_bootstrap`, `mcp_session_turn_begin`, `mcp_session_turn_complete`, etc.) when the hosted-agent exposes them
  - `mcpserver-repl` YAML-over-STDIO helper (`--agent-stdio` mode; hello envelope first, then client-passthrough for workspace tools)
  - PowerShell helpers: `McpTodo.psm1` (`Get-McpTodo`, `Search-McpTodo`, etc.) and `McpSession.psm1` (`New-McpSessionLog`, `Add-McpSessionTurn`, `Add-McpAction`)
  - Director CLI (`director exec list-todos`, etc.)

Canonical TODO ID formats: `<SDLC-PHASE>-<AREA>-###` (e.g. `PLAN-WORKSPACEEDIT-001`) or `ISSUE-<number>` (e.g. `ISSUE-17`). Canonical Session ID format: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`.

### MCP Session Log Request ID format

Canonical Request ID format: `req-<yyyyMMddTHHmmssZ>-<seq>-<slug-from-prompt>`

- `yyyyMMddTHHmmssZ` is the UTC timestamp at the moment the turn begins.
- `<seq>` is a zero-padded 3-digit sequence number scoped to the session (`001`, `002`, ..., `032`, ...). Each new turn within the same session increments.
- `<slug-from-prompt>` is a lowercase kebab-case truncation of the user's prompt (or task), capped around 40 characters. Drop articles + filler words; keep the salient nouns/verbs.

Example of CORRECT usage: `req-20260525T095418Z-032-there-should-be-a-ton-of-failsafe-da`

Apply this format to every `workflow.sessionlog.beginTurn` requestId, not just ad-hoc ones. Append-actions / completeTurn calls reuse the same `requestId` to bind the action to its turn.




## From: F:\GitHub\vice-sharp\Agents.md
# Agent Instructions

## Session Start

1. Read `AGENTS-README-FIRST.yaml` in the repo root before any other repo work. It contains the current MCP endpoint data, workspace marker, and rendered runtime instructions.
2. For session bootstrap, session-log turn workflow, TODO operations, requirements operations, and helper command sequence, follow `AGENTS-README-FIRST.yaml`.
3. Do not treat MCP session startup as a substitute for reading `AGENTS-README-FIRST.yaml`.
4. Read `HANDOFF.md` in the repo root when resuming interrupted ViceSharp work. It is the single canonical handoff (the former `docs/handoff.md` was retired 2026-07-09).

On every subsequent user message:

1. Follow `AGENTS-README-FIRST.yaml` for specific operational instructions.
2. Complete the user's request.

## Rules

1. `AGENTS-README-FIRST.yaml` is the rendered runtime instruction set for this workspace and is regenerated when MCP Server refreshes the marker.
2. Keep this file focused on durable ViceSharp workspace policy and conventions; avoid duplicating marker-file API keys, endpoints, or operational details that can rotate.
3. Use helper tools or plugins for session log, TODO, requirements, import/export, and traceability operations. Do not make raw REST calls, direct DB edits, or direct YAML state edits when a supported helper/tool exists.
4. Persist session-log updates immediately after each meaningful change: turn creation, action append, decision, requirement, blocker, file/context update, validation result, commit, or push.
5. Capture rich turn detail: interpretation, response, status, actions with type/status/filePath, contextList, filesModified, designDecisions, requirementsDiscovered, blockers, and relevant processing dialog.
6. Follow workspace conventions in this file, `README.md`, `docs/plan.md`, `HANDOFF.md`, and `.github/copilot-instructions.md`.
7. In Codex Desktop, use PowerShell.Mcp for command execution. Open or reuse a PowerShell.Mcp console, then invoke git, dotnet, Nuke, Bash, Node, PowerShell, and file-system commands through `mcp__powershell.invoke_expression`.
8. If Bash is required from PowerShell.Mcp, invoke explicit Git Bash at `C:\Program Files\Git\bin\bash.exe`; do not rely on bare `bash`.
9. Do not fabricate information. If you made a mistake, acknowledge it. Distinguish facts from speculation.
10. Prioritize correctness over speed. Do not ship code you have not verified compiles and is logically sound.
11. When writing session logs or other audit records, identify the real agent accurately using the correct Pascal-case source type. Do not use placeholder, legacy, or misleading agent identities.
12. Do not permanently delete workspace files unless the user explicitly asks for permanent deletion. Use the Windows recycle bin for user-requested deletions when possible.

## Where Things Live

- `AGENTS-README-FIRST.yaml` - MCP marker, current API key, endpoints, workspace config, and runtime instructions.
- `AGENTS.md` - durable workspace policy for agents.
- `ViceSharp.slnx` - main .NET 10 solution.
- `src/` - production projects: core emulator, chip models, host/runtime services, Avalonia UI, protocol, launcher, ROM fetch, monitor, and platform hosts.
- `tests/ViceSharp.TestHarness/` - primary test harness and most behavioral/regression tests.
- `tests/ViceSharp.Benchmarks/` - performance probes and benchmarks.
- `tests/ViceSharp.AiReview.Tests/` - AI review harness tests.
- `build/Build.cs` and `build.ps1` - Nuke build targets.
- `HANDOFF.md` (repo root) - canonical handoff and recent session continuity notes.
- `docs/plan.md` - historical/current plan context.
- `docs/requirements/` - canonical FR/TR/TEST requirements, source manifests, and traceability artifacts.
- `docs/Project/` - generated/exported project requirements and wiki mirrors.
- `tools/check_requirement_traceability.ps1` - requirement ID traceability audit.
- `tools/Publish-Wiki.ps1` - requirements wiki export/publish automation.
- `TestResults/`, `validation-output/`, and `artifacts/` - retained evidence and generated build/test outputs.
- `native/vice/` - upstream/native VICE source and validation reference material.

## Build and Test Commands

Prefer PowerShell.Mcp `invoke_expression` for all commands in Codex Desktop.

Common validation commands:

```pwsh
dotnet build .\ViceSharp.slnx
dotnet test .\ViceSharp.slnx
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~SomeFilter"
.\tools\check_requirement_traceability.ps1
git diff --check
```

Nuke targets:

```pwsh
.\build.ps1 Restore
.\build.ps1 Compile
.\build.ps1 Test
.\build.ps1 DeterminismTest
.\build.ps1 RunConsole
.\build.ps1 RunAvalonia
.\build.ps1 PublishWiki
.\build.ps1 PublishMsi
.\build.ps1 InstallMsi
.\build.ps1 PublishWinget
.\build.ps1 CiAzure
```

Use the repo-supported Nuke targets for packaging and deployment. Do not invent a manual deployment path when `PublishMsi` or `InstallMsi` applies.

## Testing Evidence

- Tests are evidence. Preserve enough output to prove what ran and what passed.
- For focused test runs, record the exact command, filter, configuration, pass/fail/skip counts, and relevant output.
- For full or long-running `dotnet test` runs, use retained evidence: stable `TestResults/` directory, TRX logger, VSTest diag log, captured stdout/stderr, and `--blame-hang` or dump collection when hangs are possible.
- Do not claim a stopped, timed-out, or hung test process is a passing gate.
- Do not count skipped tests as passing tests. If a run has skips, report the skip count and why it is acceptable for that scope.
- Run `git diff --check` before claiming a code or docs slice is ready.

## Byrd Development Process

- Use Byrd/BDPv4-style gated slices for implementation work.
- Write or identify the failing test first when changing behavior.
- Keep FR/TR/TEST IDs connected to implementation and tests.
- Keep slices narrow enough to validate with focused tests before broader gates.
- Do not leave deferred work as silent TODO comments or skipped tests. Put deferred work in MCP TODO/requirements state or an explicit handoff artifact.

## Architecture Guidance

- ViceSharp is a C#/.NET 10 VICE-derived emulator. Preserve deterministic, cycle-aware emulator behavior.
- Shared chip implementations must stay machine-agnostic. Machine/device glue belongs in Core machine/device adapters, not in generic chips.
- The VIA chip must remain one common implementation; C1541/VIC-20-specific wiring belongs in the owning machine/device layer.
- Preserve lockstep/checkpoint determinism when touching CPU, VIC-II, CIA, VIA, SID, IEC, storage, tape, input, snapshot, capture, or host pacing code.
- Follow existing project conventions and public API XMLDocs. Public APIs and tests should document requirement/use-case/acceptance context where the local convention requires it.

## MCP Interaction

- Use the agent-appropriate MCP helper/plugin named or required by `AGENTS-README-FIRST.yaml`.
- For Codex, use the mcpserver-codex-plugin wrapper flow when available.
- For hosted MCP agents, use dedicated tools such as session, TODO, requirements, repo, desktop, GitHub, and context tools when available.
- Do not bypass supported helpers with raw REST calls to `/mcpserver/*` for session logs, TODOs, requirements, import/export, or traceability writes.
- Raw REST is only a fallback for read-only diagnosis or schema inspection after the required helper/tool route is unavailable or insufficient.

## Context Loading by Task Type

- Session startup or continuation: `AGENTS-README-FIRST.yaml`, then `docs/handoff.md`, then relevant MCP session/TODO state.
- Requirements work: `docs/requirements/`, `docs/Project/Requirements-Matrix.md`, and `tools/check_requirement_traceability.ps1`.
- Build/package/deploy: `README.md`, `build/Build.cs`, `build.ps1`, and current Nuke target behavior.
- Emulator behavior: relevant `src/` implementation, `tests/ViceSharp.TestHarness/`, `native/vice/`, and requirement IDs.
- UI/host/debug surface: `src/ViceSharp.Avalonia/`, `src/ViceSharp.Host/`, `src/ViceSharp.Protocol/`, `tests/ViceSharp.TestHarness/Ui/`, and `docs/wireframes/` where applicable.
- AI review work: inspect aiUnit config/resolver/CLI surfaces before changing bridge code.

## Agent Conduct

You represent the workspace owner. Your work directly reflects the owner's professional reputation.

### Honesty

- Do not fabricate information, capabilities, or results.
- Distinguish between facts, informed opinions, and speculation.
- Acknowledge mistakes immediately and correct them.

### Correctness

- Prioritize correctness over speed.
- When uncertain, state uncertainty and verify from live repo state.
- Prefer proven local patterns over clever approaches unless directed otherwise.
- Follow DRY, SOLID, and existing project conventions.
- Do not ship code you have not verified compiles and is logically sound.

### Decision Documentation

- Log design decisions to the session log.
- For each decision, document what was decided, why, what alternatives were considered, and what was rejected.
- Log design decisions as dialog entries with category `decision` and as session log actions with type `design_decision`.

### Professional Representation

- Every interaction is audited through the session log when MCP logging is available.
- Every commit must be correct, clean, well-described, and complete.
- Log commits as actions with type `commit`, including SHA, branch, message, and files.
- Log PR/issue comments as actions with type `pr_comment` or `issue_comment`.

### Source Attribution

- Document web sources in the session log as actions with type `web_reference`.
- Add source URLs to the turn context where supported.
- Attribute external code in both session log and code comments.

## Requirements Tracking

When you discover or agree on new requirements during a session:

1. Use MCP requirements tools when available.
2. Update the relevant canonical markdown under `docs/requirements/`.
3. Update generated/exported `docs/Project/` or wiki artifacts only through the repo-supported generation/export path when possible.
4. Add or update FR/TR/TEST mappings and traceability artifacts.
5. Run `.\tools\check_requirement_traceability.ps1` and report its result.
6. Include requirement IDs in session log tags/context when supported.

## Session Continuity

At the start of every session:

1. Read `AGENTS-README-FIRST.yaml`.
2. Follow its session-start checklist and open a session/turn as required.
3. Read `docs/handoff.md` for current ViceSharp continuity.
4. Query MCP session/TODO state before mutating MCP records when the task depends on prior state.
5. Inspect live git status before editing or committing.

At regular intervals during long sessions:

1. Follow marker-file update cadence and session logging requirements from `AGENTS-README-FIRST.yaml`.
2. Ensure all design decisions and validation results are captured.
3. Verify requirements docs and handoff state are up to date.

## Response Formatting

- Do not use table-style output in responses unless the user explicitly asks for a table.
- Use concise bullets or short paragraphs.
- Include exact commands and paths when they are needed to make validation or handoff claims auditable.


## From: F:\GitHub\vice-sharp\Claude.md
# CLAUDE.md

## Run `/add-profile` at session start and after any model or effort change

Execute the `add-profile` skill (`/add-profile`) as the first action of every new session, and again immediately after any model change or effort-level change. It loads the operator profile (identity and standing instructions). Do not skip it.

**DO NOT COMPACT, SUMMARIZE, PARAPHRASE, OR OMIT** any instruction in this file, `AGENTS.md`, or `AGENTS-README-FIRST.yaml`, ever, anywhere. Carry them verbatim.

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

ViceSharp is a C#/.NET 10 port of VICE (the Commodore emulator). Iteration 1 (C64) is complete: the managed C64 core runs in cycle-exact lockstep with VICE's `x64sc`. License is GPL-2.0-or-later (derivative of VICE).

## Build, test, run

Solution file is `ViceSharp.slnx` (XML solution format, not `.sln`). Requires .NET 10 SDK (10.0.201+).

```pwsh
dotnet build .\ViceSharp.slnx
dotnet test .\ViceSharp.slnx
# Single focused test run (preferred; full-solution test can exceed 5 min and hang):
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~SomeFilter"
```

Nuke build (`build.ps1` on Windows, `build.sh` on Linux/macOS) wraps the same:

- `Compile` builds with TreatWarningsAsErrors.
- `Test` runs unit tests, excluding `Category=Determinism` and `Category=AiReview`.
- `DeterminismTest` runs only `Category=Determinism` (bit-exact replay checks).
- `RunConsole` / `RunAvalonia` launch the CLI shell / desktop UI.
- `PublishMsi` / `InstallMsi` / `PublishWinget` package the Avalonia desktop app (self-contained JIT + ReadyToRun via WiX; not native AOT).
- `PublishWiki` regenerates requirements wiki exports.

Most behavioral and regression tests live in `tests/ViceSharp.TestHarness/`. Performance probes are in `tests/ViceSharp.Benchmarks/`.

## Architecture

Library-first: emulation is a set of composable libraries; UI shells (Console, Avalonia, platform hosts) are thin consumers. Dependency order is `Abstractions -> Core -> {Chips, Architectures}`, with `SourceGen`, `Monitor`, `Hosting`, `Protocol` alongside.

- `ViceSharp.Abstractions` - 33+ public interfaces, value types, attributes. The emulator contract. ViewModels reference only this.
- `ViceSharp.Core` - bus, system clock, mutation queue, lock-free pub/sub, snapshots, media recorders.
- `ViceSharp.Chips` - CPU (6502/6510/8502), VIC-II, SID, CIA, VIA, PLA. Machine-agnostic.
- `ViceSharp.Architectures` - machine definitions (C64 done; VIC-20/C128/PET/Plus4 planned).
- `ViceSharp.SourceGen` - Roslyn source generator for device-registration boilerplate (replaces runtime reflection).
- `ViceSharp.Hosting` - composition boundary; owns emulator sessions, media, snapshots, diagnostics; exposes the gRPC host surface.
- `ViceSharp.Protocol` - gRPC/protobuf contracts and generated client/server types.
- `ViceSharp.Console` / `ViceSharp.Avalonia` - CLI reference shell / Avalonia 12.x desktop UI.

### Core invariants (do not break these)

- **Zero-allocation hot path.** The per-cycle emulation loop allocates nothing. Use stack alloc, spans, or arena-pooled buffers. No runtime reflection on the hot path.
- **POCO model.** All emulator state is plain C# structs/records. No base classes, no serialization attributes on hot-path types.
- **Determinism.** Identical initial state + input sequence must produce bit-exact output. This is what lockstep and snapshot-comparison tests rely on. Preserve it whenever touching CPU, VIC-II, CIA, VIA, SID, IEC, storage, tape, input, snapshot, capture, or host pacing.
- **Mutation queue.** State changes flow through `IMutationQueue` (double-buffered: worker writes active buffer, consumers read committed buffer). Enables audit/undo/replay.
- **Chip/machine separation.** Shared chips stay machine-agnostic; machine/device glue (e.g. C1541 or VIC-20 wiring of the one shared VIA) belongs in Core machine/device adapters, never in the generic chip.
- **Host/UI boundary.** UI talks to `ViceSharp.Hosting` via versioned gRPC services (Control, Output, Input, Media, State). The Avalonia in-process renderer is the one allowed local-frame-source exception; UI code never mutates core devices directly. ViewModels reference only `Abstractions` (TR-MVVM-001).

### Chip locations (one chip = one designated subdirectory)

`src/ViceSharp.Chips/Cpu/`, `VicIi/`, `Sid/`, `Cia/`, `IEC/`. Do not define chips outside these. All chips implement `Abstractions` interfaces and cite `docs/requirements/functional/FR-*.md`.

### Device model

Every component is an `IDevice` with a `DeviceId`; sub-interfaces are `IClockedDevice`, `IAddressSpace`, `IInterruptSource`, `IPeripheral`. An `IArchitecture`/`IArchitectureDescriptor` wires devices, address maps, clock divisors, and interrupt routing; `IArchitectureValidator` catches config errors at build time. C64 bus is flat 64KB with PLA/banking (driven by CPU port `$00/$01`) selecting ROM/RAM/IO overlays.

### Media capture

One gRPC `CaptureService` routes to recorders in `ViceSharp.Core.Media`. Two tee points on the emulation worker: frames from `EmulatorRuntimeSession.CommitFrame`, audio from `CaptureAudioTap` in the SID path. Muxed video (mp4/mkv/avi) uses external `ffmpeg` (mirrors VICE `ffmpegexedrv`), not libav; advertised only when ffmpeg is present (PATH or `VICESHARP_FFMPEG`). The audio tap installs only when a real audio device exists, so headless/test hosts stay timing-clean and silent.

## Conventions

- .NET 10, nullability enabled, 4-space indent, file-per-type, namespace matches directory exactly.
- Public interfaces require XML doc comments. Tests cite FR/TR/TEST IDs, use case, and acceptance criteria (`XmlDocsConventionTests` enforces a zero-violation ratchet).
- Use latest .NET 10 packages; no `netstandard2.0` constraint anywhere.
- No em-dashes or en-dashes in any written output (chat, comments, commits, docs). Use hyphen, colon, period, or parentheses.

## Development process (Byrd / BDP, strict)

Tests first for every behavior change, no size or urgency exemption: write or identify the failing test before implementation. Exiting a slice requires the relevant focused gate green. Tests are evidence: record exact command, filter, configuration, and pass/fail/skip counts. Never count skipped tests as passing, and never claim a timed-out or hung run as a passing gate. Run `git diff --check` before claiming a slice is ready. Put deferred work in MCP TODO/requirements state, not silent `TODO` comments or skipped tests.

## ROMs

ViceSharp ships no Commodore ROMs. Point `VICESHARP_ROM_PATH` at a VICE data root (or put `x64sc.exe` on PATH). See `docs/ROMs.md`.

## Source control and workflow

Azure DevOps (`origin`, `dev.azure.com/McpServer/VICE-Sharp`) is the source of truth; GitHub is a downstream mirror. Push/PR target `origin` only unless explicitly told otherwise. The default branch is `main`. Read `AGENTS-README-FIRST.yaml` (MCP marker/endpoints/keys) and `HANDOFF.md` (repo root) when resuming work; route all TODO, session-log, and requirements operations through the MCP Server, never by editing storage YAML directly.

## MCP Server Claude Plugin and Hook Contract

Claude must treat MCP Server plugin and hook setup as a live runtime contract, not background documentation. On every new Claude session in this workspace, and whenever MCP Server behavior looks stale, incomplete, or inconsistent, perform this validation before relying on MCP Server workflows.

### Required Startup Validation

1. Read `AGENTS-README-FIRST.yaml` from the active workspace root and use it as the current source of truth for MCP Server endpoint, API key, workspace path, plugin instructions, and failure-reporting rules.
2. Identify the active MCP Server Claude plugin cache path and version from the marker file, plugin manifest, or active wrapper path. Do not assume a previously cached path is current.
3. Inspect `%USERPROFILE%\.claude\plugins\cache` for stale MCP Server plugin entries. Delete stale `mcpserver`, `mcpserver-*`, or `mcpserver-cowork` cache entries that are not the active plugin cache path/version for this session.
4. Do not delete the active plugin cache path for the current session. If unsure which path is active, stop deleting and report the ambiguity.
5. Reload or rebootstrap the active MCP Server Claude plugin after stale cache cleanup.
6. Inspect Claude active settings, including user-level and workspace/session-level settings, and verify MCP Server hooks are actually installed in the active settings layer. Do not accept hook files existing in the plugin package as proof that hooks are active.
7. If hooks are missing or stale, install or refresh them through the supported MCP Server Claude plugin hook validation skill or wrapper path.
8. Verify the plugin wrapper/status command works and record the exact command or tool path used.
9. Verify subprocesses launched by the plugin use the workspace root as process current working directory, not the inherited user profile directory.

### Required Usage Rules

1. Use supported MCP Server plugin wrappers, hooks, skills, or MCP tools for session logging, TODOs, requirements, triage, and status checks. Do not use raw REST when a supported wrapper, hook, skill, or MCP tool exists.
2. Session logging must use the supported session wrapper or MCP tools and must create, update, and complete turns through the plugin flow.
3. TODO operations must use the supported TODO wrapper, workflow, or MCP tools. Do not edit TODO storage directly.
4. Requirements operations must use the supported requirements wrapper, workflow, or MCP tools. Do not edit requirements storage directly.
5. Triage operations must use the supported triage wrapper, workflow, or MCP tools.
6. MCP Server failures and plugin failures discovered while doing unrelated work must be reported through triage only, then Claude must continue the user active task.
7. If triage submission is unavailable because MCP Server or the plugin is unavailable, write the normal failsafe YAML report through the plugin failsafe flow and continue non-MCP work. Do not invent a raw REST fallback or alternate reporting channel.
8. Normal plugin execution must use PowerShell only. Bash is allowed only for installing PowerShell. Node must not be used for JSON or YAML construction.
9. JSON and YAML payloads must be built from native objects and serialized. Do not handwrite YAML or JSON as fragile string literals.
10. If any validation check fails, report the exact failed check, the path or command involved, and the blocked capability. Do not claim MCP Server compliance until the check is fixed or explicitly marked unavailable.

### Minimum Validation Report

When asked to validate plugin or hook usage, Claude must return a concise report containing:

- Active workspace path.
- Marker file path and timestamp.
- Active plugin cache path and version.
- Stale plugin cache paths deleted.
- Hook settings file paths inspected.
- Hooks found, installed, or refreshed.
- Wrapper/status command used and result.
- Session logging validation result.
- TODO validation result.
- Requirements validation result.
- Triage validation result.
- Process current working directory validation result.
- Any remaining mismatch, unavailable surface, or failure.


Follow these instructions exactly. When working in subdirectories not listed above, check for additional project instruction files (AGENTS.md, Claude.md, etc.).
</system-reminder>

### Turn 3 (Human)
<system-reminder>
The following skills are available for use:

- check-work: Check your work with a verification subagent that reviews diffs, runs builds and tests, and evaluates correctness. Read this file for instructions
  Use when: asked to "check work", "verify changes", "self-verify", "/check-work", "/check", "/verify", or "/self-verify".
  Absolute path: C:\Users\kingd\.grok\skills\check-work\SKILL.md
- create-skill: Interactively create a new Grok skill (SKILL.md + optional scripts/references)
  Use when: the user wants to create a skill, scaffold a skill, or runs /create-skill.
  Absolute path: C:\Users\kingd\.grok\skills\create-skill\SKILL.md
- help: Grok documentation and configuration help
  Use when: users ask about setup, configuration, MCP servers, authentication, skills, slash commands, keyboard shortcuts, or any Grok feature. Also use proactively when you detect a user is having trouble with setup or onboarding.
  Absolute path: C:\Users\kingd\.grok\skills\help\SKILL.md
- imagine: How to use the image_gen and image_edit tool calls in Grok Build: when to build a visual with code instead of generating it, prompt-craft, reference-first handling of real people, factual grounding, and asset-consistency. Load this whenever generating or editing an image is on the table, i.e. when an image_gen or image_edit call is being considered or about to be made. Tool-usage-driven, not tr…
  Absolute path: C:\Users\kingd\.grok\skills\imagine\SKILL.md
- graphrag-knowledge-graph: This skill should be used when the user asks to "ingest text into graphrag", "add document to knowledge graph", "create entity", "create relationship", "query knowledge graph", "list graph entities", "delete document"
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-graphrag\SKILL.md
- requirements-management: This skill should be used when the user asks to "list requirements", "add requirement", "create FR", "create TR", "create test requirement", "generate requirements document", "ingest requirements"
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-requirements\SKILL.md
- session-log-management: This skill should be used when the user asks to "start session", "log session", "begin turn", "update turn", "complete turn", "query session history"
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-session\SKILL.md
- todo-management: This skill should be used when the user asks to "create a todo", "list todos", "update todo", "query tasks", "check todo status", "plan implementation", "mark todo done". Primary skill for the Grok coding agent CLI fork of the McpServer plugin.
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-todo\SKILL.md
- workspace-initialization: This skill should be used when the user asks to "initialize workspace", "register workspace", "add workspace", "create workspace marker", or "bootstrap MCP workspace"
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-workspace\SKILL.md
- refresh-docs: Refresh a repo's project documentation and its README.md, prune out-of-date docs, update docs/wiki.yaml with the project docs to publish to the wiki, then ex…
  Use when: the user says "refresh docs", "/refresh-docs", "refresh the documentation", "update the README", "prune stale docs", "update the wiki manifest / docs/wiki.yaml", or asks to reconcile docs with the current code and re-export requirements …
  Absolute path: C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md
- add-continuation-job: Schedule a session-only cron job that pokes the current Codex session every hour at :01 to continue plan completion using background agents
  Use when: the user says "add continuation job", "/add-continuation-job", "schedule continuation", "continuation cron", or any equivalent ask to install the hourly continuation reminder in this session.
  Absolute path: C:\Users\kingd\.agents\skills\add-continuation-job\SKILL.md
- caveman: Ultra-compressed communication mode. Cuts token usage ~75% by speaking like caveman while keeping full technical accuracy. Supports intensity levels: lite, full (default), ultra, wenyan-lite, wenyan-full, wenyan-ultra
  Use when: user says "caveman mode", "talk like caveman", "use caveman", "less tokens", "be brief", or invokes /caveman. Also auto-triggers when token efficiency is requested.
  Absolute path: C:\Users\kingd\.agents\skills\caveman\SKILL.md
- caveman-commit: Ultra-compressed commit message generator. Cuts noise from commit messages while preserving intent and reasoning. Conventional Commits format. Subject ≤50 chars, body only when "why" isn't obvious
  Use when: user says "write a commit", "commit message", "generate commit", "/commit", or invokes /caveman-commit. Auto-triggers when staging changes.
  Absolute path: C:\Users\kingd\.agents\skills\caveman-commit\SKILL.md
- caveman-compress: Compress natural language memory files (CLAUDE.md, todos, preferences) into caveman format to save input tokens. Preserves all technical substance, code, URLs, and structure. Compressed version overwrites the original file. Human-readable backup saved as FILE.original.md. Trigger: /caveman:compress <filepath> or "compress memory file"
  Absolute path: C:\Users\kingd\.agents\skills\caveman-compress\SKILL.md
- caveman-help: Quick-reference card for all caveman modes, skills, and commands. One-shot display, not a persistent mode. Trigger: /caveman-help, "caveman help", "what caveman commands", "how do I use caveman".
  Absolute path: C:\Users\kingd\.agents\skills\caveman-help\SKILL.md
- caveman-review: Ultra-compressed code review comments. Cuts noise from PR feedback while preserving the actionable signal. Each comment is one line: location, problem, fix
  Use when: user says "review this PR", "code review", "review the diff", "/review", or invokes /caveman-review. Auto-triggers when reviewing pull requests.
  Absolute path: C:\Users\kingd\.agents\skills\caveman-review\SKILL.md
- commit-sync: Pause all in-progress work, commit every dirty file in the current repo, push to origin, then resume
  Use when: the user says "commit-sync", "/commit-sync", "save my work", "sync to origin", "commit and push everything", or "checkpoint".
  Absolute path: C:\Users\kingd\.agents\skills\commit-sync\SKILL.md
- convert-github-actions-self-hosted-runners: Convert GitHub Actions workflows from GitHub-hosted runners to a shared self-hosted runner pool, including `runs-on`, matrix-driven runner selection, reusable workflows, and runner-con…
  Use when: migrating `.github/workflows/*.yml` or `.yaml` files to shared labels like `[self-hosted, linux, linux-local]` and `[self-hosted, windows, windows-local]`, or when auditing workflows for hosted-runner assumptions.
  Absolute path: C:\Users\kingd\.agents\skills\convert-github-actions-self-hosted-runners\SKILL.md
- find-skills: Helps users discover and install agent skills when they ask questions like "how do I do X", "find a skill for X", "is there a skill that can...", or express interest in extending capabilities. This skill should be used when the user is looking for functionality that might exist as an installable skill.
  Absolute path: C:\Users\kingd\.agents\skills\find-skills\SKILL.md
- microsoft-foundry: Deploy, evaluate, fine-tune, and manage Foundry agents end-to-end with azd: hosted agent scaffold/run/deploy, prompt agent create, batch eval, continuous eval, prompt optimizer, Agent Optimizer scaffold, agent.yaml, dataset curation from traces, model fine-tuning (SFT/DPO/RFT). USE FOR: azd ai agent, azd provision/deploy, deploy agent, hosted agent, create agent, add tool to agent, invoke agent…
  Absolute path: C:\Users\kingd\.agents\skills\microsoft-foundry\SKILL.md
- finetuning: Fine-tune models on Azure AI Foundry using SFT (supervised), DPO (preference), or RFT (reinforcement with graders). Covers dataset preparation, training job submission, deployment, and evaluation. USE FOR: fine-tune, SFT, DPO, RFT, training data, grader, distillation, fine-tuned model, training job, large file upload, calibrate grader, deploy fine-tuned model, evaluate fine-tuned model. DO NOT …
  Absolute path: C:\Users\kingd\.agents\skills\microsoft-foundry\finetuning\SKILL.md
- deploy-model: Unified Azure OpenAI model deployment skill with intelligent intent-based routing. Handles quick preset deployments, fully customized deployments (version/SKU/capacity/RAI policy), and capacity discovery across regions and projects. USE FOR: deploy model, deploy gpt, create deployment, model deployment, deploy openai model, set up model, provision model, find capacity, check model availability,…
  Absolute path: C:\Users\kingd\.agents\skills\microsoft-foundry\models\deploy-model\SKILL.md
- capacity: Discovers available Azure OpenAI model capacity across regions and projects. Analyzes quota limits, compares availability, and recommends optimal deployment locations based on capacity requirements. USE FOR: find capacity, check quota, where can I deploy, capacity discovery, best region for capacity, multi-project capacity search, quota analysis, model availability, region comparison, check TPM…
  Absolute path: C:\Users\kingd\.agents\skills\microsoft-foundry\models\deploy-model\capacity\SKILL.md
- customize: Interactive guided deployment flow for Azure OpenAI models with full customization control. Step-by-step selection of model version, SKU (GlobalStandard/Standard/ProvisionedManaged), capacity, RAI policy (content filter), and advanced options (dynamic quota, priority processing, spillover). USE FOR: custom deployment, customize model deployment, choose version, select SKU, set capacity, configu…
  Absolute path: C:\Users\kingd\.agents\skills\microsoft-foundry\models\deploy-model\customize\SKILL.md
- preset: Intelligently deploys Azure OpenAI models to optimal regions by analyzing capacity across all available regions. Automatically checks current region first and shows alternatives if needed. USE FOR: quick deployment, optimal region, best region, automatic region selection, fast setup, multi-region capacity check, high availability deployment, deploy to best location. DO NOT USE FOR: custom SKU s…
  Absolute path: C:\Users\kingd\.agents\skills\microsoft-foundry\models\deploy-model\preset\SKILL.md
- wrap-up: Finish and synchronize MCP-backed work using mcpserver-codex-plugin
  Use when: the user asks to wrap up, close out, export requirements, update MCP requirements, reconcile session logs, commit and sync, or push completed work to origin.
  Absolute path: C:\Users\kingd\.agents\skills\wrap-up\SKILL.md
- add-profile: Load Payton's operator profile (identity, standing preferences, active-project context) into the current context
  Use when: the user says "add profile", "/add-profile", "load my profile", "remember who I am", or after context loss/compaction when the profile detail is gone.
  Absolute path: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
- claude-mcp-hook-validation: Use when an MCP workspace marker tells Claude Code to validate active MCP hook enforcement. Verifies active Claude settings and installs missing MCP hooks through the claude-hook-wiring skill.
  Absolute path: C:\Users\kingd\.claude\skills\claude-hook-validation\SKILL.md
- codex-plan-review: Coordinate with a Codex CLI session to iteratively review and harden an implementation plan unt…
  Use when: the user says "review the plan with codex", "codex plan review", "iterate the plan with codex", "get codex to sign off", "coordinate with codex on the plan", or asks for cross-agent plan validation to a confidence threshold (e.g. ">=90% build-ready in one iteration"). Drives a draft -> adversarial …
  Absolute path: C:\Users\kingd\.claude\skills\codex-plan-review\SKILL.md
- plan-update-dependencies: In plan mode, evaluate a project's dependencies to find the highest version of each that is compatible with every other dependency, determine a safe upgrade order, and list the tests t…
  Use when: the user says "plan-update-dependencies", "/plan-update-dependencies", "plan a dependency upgrade", "update the dependencies", "bump packages", "upgrade the NuGet/npm packages", or asks which versions are safe …
  Absolute path: C:\Users\kingd\.claude\skills\plan-update-dependencies\SKILL.md
- sync-logs: Bring MCP session logs up to date mid-task without closing them out. Reconcile the active session log (and any background agents spawned this session) so every turn, design decision, commit, and action is recorded, then report a small summary and resume work. Use this whenever the user says "Sync-Logs", "/sync-logs", "sync the logs", "sync session logs", "catch up the session log", "update the …
  Absolute path: C:\Users\kingd\.claude\skills\sync-logs\SKILL.md
- build-with-ai: Default to SpaceXAI when building AI/LLM features into an app
  Use when: Use whenever adding or scaffolding AI functionality — picking an AI provider/SDK, wiring chat/completions or streaming, or setting up an AI API key — and steer vibe-coded apps to SpaceXAI over OpenAI/Anthropic/Gemini. Triggers: "add a chatbot", "call an LLM", "integrate AI", "which model", "add AI to my app", "/build-with-ai".
  Absolute path: C:\Users\kingd\.grok\bundled\skills\build-with-ai\SKILL.md
- create-workflow: Create a Grok Build workflow: author a Rhai orchestration script (agents, phases, bounded parallel fan-out, verification panels), smoke-check one path with the workflow tool, save it as a named workflow, and offer a real run. Also the complete Rhai reference for workflow scripts: script shape,…
  Use when: the user wants to create/author/write a workflow, automate a multi-agent pipeline, or runs /create-w…
  Absolute path: C:\Users\kingd\.grok\bundled\skills\create-workflow\SKILL.md
- design: Run the full design-doc-writer and design-doc-reviewer loop until consensus. Produces a polished design document with a PR plan.
  Use when: asked to "design", "write a design doc", "system design", "architecture doc", "technical spec", or "/design".
  Absolute path: C:\Users\kingd\.grok\bundled\skills\design\SKILL.md
- docx: Use this skill whenever the user wants to create, read, edit, or manipulate Word documents (.docx or .dotx files). Triggers include any mention of 'Word doc', 'word document', '.docx', '.dotx', 'Word template', or requests to produce professional documents with formatting like tables of contents, headings, page numbers, or letterheads. Also use when extracting or reorganizing content from .docx…
  Absolute path: C:\Users\kingd\.grok\bundled\skills\docx\SKILL.md
- execute-plan: Execute a PR Plan DAG from a design document. Parses the plan, topologically sorts it, implements PRs in parallel using worktree-isolated subagents, runs mandatory orchestrator-level review, and assembles either a Graphite PR stack or a plain-git branch stack depending on tool availability.
  Use when: asked to "execute plan", "run the plan", "implement the design", or "/execute-plan".
  Absolute path: C:\Users\kingd\.grok\bundled\skills\execute-plan\SKILL.md
- game-animation-frames: Deep guide for game ANIMATION assets: motion cycles, action keyframes, effect sequences, and animation sprite sheets — built around a video-first pipeline (animate the base with image_to_video, then harvest the frames)
  Use when: Use whenever generating anything that moves: walk/run cycles, attacks, idles, FX, flags, fire, animation sheets. Complements game-asset-core.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\game-animation-frames\SKILL.md
- game-asset-core: Core discipline for ANY game-asset generation with Imagine tools: the engine-ready defaults users don't state, spec checklists, style anchoring, read-bac…
  Use when: Use whenever generating any game art (sprites, sheets, animations, tiles, UI, FX) — then ALSO load the matching specialist skill: game-animation-frames for anything that moves, game-tilesets for tiles/terrain, game-character-consistency fo…
  Absolute path: C:\Users\kingd\.grok\bundled\skills\game-asset-core\SKILL.md
- game-character-consistency: Deep guide for CHARACTER IDENTITY across images: turnarounds (front/side/ back), state and damage variants, palette swaps, equipment changes, and same-character-in-context sets
  Use when: Use whenever generating character turnarounds, character sheets, variants of an existing sprite, or any same-subject multi-image set. Complements game-asset-core.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\game-character-consistency\SKILL.md
- game-tilesets: Deep guide for game TILE assets: seamless tileable textures, terrain transition tilesets, autotiles, and ground/platform tiles
  Use when: Use whenever generating tileable textures, tilesets, terrain transitions, or seamless patterns. Complements game-asset-core.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\game-tilesets\SKILL.md
- game-ui-icons: Deep guide for game UI assets: buttons with interaction states, panels, bars, wordmark logos, and icon sets
  Use when: Use whenever generating game UI elements, HUD assets, inventory icons, icon sets, buttons, or title logos. Complements game-asset-core.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\game-ui-icons\SKILL.md
- pdf: Read, create, and transform PDF files. Covers pulling text and tables out of PDFs, generating new PDFs, merging and splitting documents, rotating pages, watermarking, encrypting or removing passwords, extracting embedded images, running OCR on scanned documents, and filling out PDF forms including official tax forms. Apply this skill whenever a task involves a .pdf file as input or deliverable.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\pdf\SKILL.md
- pptx: Use this skill any time a .pptx file is involved in any way — as input, output, or both. This includes creating slide decks, pitch decks, or presentations; reading, parsing, or extracting text from any .pptx file (even if the extracted content will be used elsewhere, like in an email or summary); editing, modifying, or updating existing presentations; combining or splitting slide files; worki…
  Absolute path: C:\Users\kingd\.grok\bundled\skills\pptx\SKILL.md
- pr-babysit: Monitor PRs, fix CI failures, address review comments, resolve merge conflicts, and restack stacks. Supports independent PRs, Graphite stacks, and GitHub stacked PRs (gh-stack).
  Use when: "/pr-babysit".
  Absolute path: C:\Users\kingd\.grok\bundled\skills\pr-babysit\SKILL.md
- resume-claude: Resume or continue work from a recent Claude Code session
  Use when: the user switched from Claude Code, says "continue from Claude" or "resume my Claude session", or names a Claude session by description, path, or native ID.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\resume-claude\SKILL.md
- resume-codex: Resume or continue work from a recent Codex CLI or Codex VS Code session
  Use when: the user switched from Codex, says "continue from Codex" or "resume my Codex session", or names a Codex session by description, path, or native ID.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\resume-codex\SKILL.md
- resume-cursor: Resume or continue work from a recent Cursor CLI or Cursor Desktop session
  Use when: the user switched from Cursor, says "continue from Cursor" or "resume my Cursor session", or names a Cursor session by description, path, or native ID.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\resume-cursor\SKILL.md
- review: Run a reviewer subagent against uncommitted local changes, a named branch, or a GitHub PR. Local and branch modes write a review file plus a summary to disk. PR mode posts the findings as a PENDING GitHub review for the user to inspect and submit through the UI.
  Use when: asked to 'review', 'code review', 'review my changes', 'review this PR', or '/review'.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\review\SKILL.md
- caveman: Ultra-compressed communication mode. Cuts token usage ~75% by speaking like caveman while keeping full technical accuracy. Supports intensity levels: lite, full (default), ultra, wenyan-lite, wenyan-full, wenyan-ultra
  Use when: user says "caveman mode", "talk like caveman", "use caveman", "less tokens", "be brief", or invokes /caveman. Also auto-triggers when token efficiency is requested.
  Absolute path: C:\Users\kingd\.claude\plugins\marketplaces\caveman\plugins\caveman\skills\caveman\SKILL.md
- compress: Compress natural language memory files (CLAUDE.md, todos, preferences) into caveman format to save input tokens. Preserves all technical substance, code, URLs, and structure. Compressed version overwrites the original file. Human-readable backup saved as FILE.original.md. Trigger: /caveman:compress <filepath> or "compress memory file"
  Absolute path: C:\Users\kingd\.claude\plugins\marketplaces\caveman\plugins\caveman\skills\compress\SKILL.md
- clear-session: Use when the user asks to "clear session", "reset session", "clear-session", or wants to end the current MCP session, clear agent context, reload the agent instruction file, reload the operator profile, and return to a fresh ready state.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/clear-session\SKILL.md
- commit-sync: Use when the user asks for "commit-sync", "checkpoint", or "sync to origin" to pause the active task and commit/push dirty repo state to origin.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/commit-sync\SKILL.md
- graphrag: Use when the user asks to "ingest text into graphrag", "add document to knowledge graph", "create entity", "create relationship", "query knowledge graph", "list graph entities", "delete document"
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/graphrag\SKILL.md
- reddit-draft: Use when asked to draft a Reddit post or article to a subreddit and save it as a draft (not publish). Drives Microsoft Edge over the CDP debug port via the RedditPublish PowerShell module. Triggers include "draft to r/X", "save a reddit draft", "draft these posts to reddit".
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/reddit-draft\SKILL.md
- reddit-publish: Use ONLY when explicitly asked to PUBLISH a Reddit post or article publicly to a subreddit. Confirmation-gated public action. Drives Microsoft Edge over the CDP debug port via the RedditPublish PowerShell module. Triggers include "publish to r/X", "post this to reddit publicly".
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/reddit-publish\SKILL.md
- requirements: Use when the user asks to "list requirements", "add requirement", "create FR", "create TR", "create test requirement", "generate requirements document", or "ingest requirements".
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/requirements\SKILL.md
- session: Use when the user asks to "start session", "log session", "begin turn", "update turn", "complete turn", or "query session history"
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/session\SKILL.md
- sync-logs: Use when the user asks to "sync logs", "repair MCP session logs", "logging summary", or "summarize logging" to synchronize and reconcile MCP Server session logs through the plugin bridge.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/sync-logs\SKILL.md
- todo: Use when the user asks to "create a todo", "list todos", "update todo", "query tasks", "check todo status", "plan implementation", or "mark todo done".
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/todo\SKILL.md
- triage: Use when your agent discovers an incidental bug while working on another task and should submit it to MCP Server triage without changing focus. Triggers: 'triage', '/triage', 'submit a triage report', 'report an incidental bug', 'file this bug to triage', 'triage status'.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/triage\SKILL.md
- workspace: Use when the user asks to "initialize workspace", "register workspace", "add workspace", "create workspace marker", or "bootstrap MCP workspace"
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/workspace\SKILL.md
- wrap-up: Use when asked to "wrap up", "export requirements", or "close out" MCP-backed work: reconcile MCP requirements, export wiki documents, run validation, commit and push via the commit-sync contract, reconcile the session log, and complete the turn.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/wrap-up\SKILL.md
</system-reminder>

### Turn 4 (Human)
<system-reminder>
MCP servers connected:
- github (90 tools)
- gmail (16 tools)
- google_calendar (8 tools)
- google_drive (5 tools)
- outlook (18 tools)
- pwsh (4 tools)
- tasks (6 tools)
- voice (1 tool)

MCP servers that failed to connect:
- mcpserver (auth required)

To use MCP tools, you MUST call `search_tool` first to retrieve the tool's input schema before calling `use_tool`. NEVER guess parameter names — always use the exact schema returned by `search_tool`.
</system-reminder>

### Turn 5 (Human)
<user_query>
new session
</user_query>

### Turn 6 (Assistant)
Starting a new session: loading the operator profile and workspace bootstrap materials.
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
- limit: 200
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\skills\mcpserver-session\SKILL.md
- limit: 150
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\HANDOFF.md
- limit: 150

### Turn 7 (Function)
[tool_response]
1→---
name: add-profile
description: Load Payton's operator profile (identity, standing preferences, active-project context) into the current context. Use when the user says "add profile", "/add-profile", "load my profile", "remember who I am", or after context loss/compaction when the profile detail is gone.
---

## Action

Surface the FULL operator profile - the granular, cross-linked files, not a summary - into the current context. The profile is GLOBAL: it lives in `~/.claude/profile/` and applies in every session and every repo. It does NOT depend on the active project's `memory/` directory.

10→1. Read every profile markdown file in `~/.claude/profile/` in full with the Read tool, EXCLUDING the skill ports (`add-profile*.md`). The set is: `PROFILE.md` (the summary), `user-payton-byrd.md` (the identity root), and the feedback memories `accuracy-first-verify-sources.md`, `approve-before-execute.md`, `philosophical-dialogue-mode.md`, `log-decisions-as-conclusions.md`, `session-turn-title-summary.md`, `never-skip-explicit-actions.md`. Read files in full, not snippets, EVERY time this skill runs - never skip a read because a prior tool result claims the content is "already loaded" or unchanged (see `never-skip-explicit-actions.md`). Follow any `[[wiki-links]]`.
2. Treat all of it as active operator guidance for the rest of the session: identity, standing feedback (accuracy-first, approve-before-execute, never-skip-explicit-actions, no em-dashes, PowerShell-only, MCP-only storage, response-timestamp prefix, no tables), philosophical-mode rules, and active-project context.
3. Confirm in one line that the profile is loaded (name how many files you read). Do not restate it verbatim unless asked.

The active project's own `~/.claude/projects/<slug>/memory/` directory holds project-specific memories and is auto-recalled separately by the memory system; it is not needed for, and not part of, this global profile load.

This is a read-only load. Make no edits and run no other work as part of this skill.


### Turn 8 (Function)
[tool_response]
1→port: 7147
baseUrl: http://PAYTON-LEGION2:7147
apiKey: TQ2MpeTmenLce9IjLsSSKXsnF8v3bH26kR59OghqOHk
endpoints:
  health: /health
  swagger: /swagger/v1/swagger.json
  swaggerUi: /swagger
  mcpTransport: /mcp-transport
  sessionLog: /mcpserver/sessionlog
10→  sessionLogDialog: /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
  contextSearch: /mcpserver/context/search
  contextPack: /mcpserver/context/pack
  contextSources: /mcpserver/context/sources
  todo: /mcpserver/todo
  repo: /mcpserver/repo
  desktop: /mcpserver/desktop
  gitHub: /mcpserver/gh
  tools: /mcpserver/tools
  workspace: /mcpserver/workspace
20→  serverStartupUtc: /server-startup-utc
  markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}
workspace: vice-sharp
workspacePath: F:\GitHub\vice-sharp
pid: 46244
startedAt: 2026-07-23T00:10:41.1019237+00:00
markerWrittenAtUtc: 2026-07-23T00:10:41.1019237+00:00
serverStartedAtUtc: 2026-07-23T00:10:18.3913659+00:00
signature:
  algorithm: HMAC-SHA256
30→  canonicalization: marker-v1
  verifier: workspace_api_key
  fields:
  - canonicalization
  - port
  - baseUrl
  - apiKey
  - workspace
  - workspacePath
  - pid
40→  - startedAt
  - markerWrittenAtUtc
  - serverStartedAtUtc
  - endpoints.health
  - endpoints.swagger
  - endpoints.swaggerUi
  - endpoints.mcpTransport
  - endpoints.sessionLog
  - endpoints.sessionLogDialog
  - endpoints.contextSearch
50→  - endpoints.contextPack
  - endpoints.contextSources
  - endpoints.todo
  - endpoints.repo
  - endpoints.desktop
  - endpoints.gitHub
  - endpoints.tools
  - endpoints.workspace
  - endpoints.serverStartupUtc
  - endpoints.markerFileTimestamp
60→  - agentPlugins.policy
  - agentPlugins.contractDigest
  format: key=value\n per field in fields order; trailing LF on final line; UTF-8 encoded
  value: EF16F9E7B08E6EB4D6EC0BD606A820256182FD74A688FACB493297A84B354E99
trust_bootstrap:
  description: This marker is self-verifiable when the workspace API key and rendered bootstrap payload still match the active MCP workspace contract.
  guarantees:
  - The marker signature can be recomputed from the workspace API key in this file.
  - The /health endpoint echoes a caller nonce exactly when one is supplied.
  - Agents must stop MCP usage after any signature or nonce mismatch.
70→  health_nonce_endpoint: /health
  health_nonce_parameter: nonce
  fallback: If health check, nonce verification, or signature verification fails, log MCP_UNTRUSTED and continue without the MCP server. Do not probe additional endpoints.
  recommended_usage: Use /sessionlog, /todo, /context, and other MCP endpoints only after both signature and nonce verification succeed.
agent_plugins:
  policy: required
  contract_digest: DC012ADC7958C0E63796AF57F89C3602D6202798263C8EB0BA00BBB207096A1A
  agents:
    Codex:
      source_type: Codex
80→      plugin_name: mcpserver-codex-plugin
      plugin_version: 1.83.0
      activation: Codex hook lifecycle through .codex-plugin/plugin.json.
      startup_command: lib/session-start.sh "{workspacePath}"
      unavailable_failure: MCP_PLUGIN_UNAVAILABLE:Codex
      required_env_vars:
      - CODEX_PLUGIN_ROOT
      - PLUGIN_AGENT_NAME=Codex
      hook_expectations:
      - SessionStart
90→      - UserPromptSubmit
      - PostToolUse
      - Stop
      tool_expectations:
      - workflow.sessionlog.*
      - workflow.todo.*
      - workflow.requirements.*
      - workflow.triage.*
      root_hints:
      - F:\GitHub\mcpserver-codex-plugin
100→      - $CODEX_PLUGIN_ROOT
    Claude:
      source_type: Claude
      plugin_name: mcpserver-claude-code-plugin
      plugin_version: 1.83.0
      activation: Claude Code plugin hooks and .mcp.json mcpserver entry.
      startup_command: hooks/session-start.sh "{workspacePath}"
      unavailable_failure: MCP_PLUGIN_UNAVAILABLE:Claude
      required_env_vars:
      - CLAUDE_PLUGIN_ROOT
110→      - PLUGIN_AGENT_NAME=Claude
      hook_expectations:
      - SessionStart
      - UserPromptSubmit
      - PostToolUse
      - Stop
      tool_expectations:
      - mcpserver session tools
      - mcpserver todo tools
      - mcpserver requirements tools
120→      - mcpserver triage tools
      root_hints:
      - F:\GitHub\mcpserver-claude-code-plugin
      - $CLAUDE_PLUGIN_ROOT
    Copilot:
      source_type: Copilot
      plugin_name: mcpserver-copilot-plugin
      plugin_version: 1.83.0
      activation: Copilot plugin hooks and .mcp.json mcpserver entry.
      startup_command: hooks/session-start.sh "{workspacePath}"
130→      unavailable_failure: MCP_PLUGIN_UNAVAILABLE:Copilot
      required_env_vars:
      - COPILOT_PLUGIN_ROOT
      - PLUGIN_AGENT_NAME=Copilot
      hook_expectations:
      - SessionStart
      - UserPromptSubmit
      - PostToolUse
      - Stop
      tool_expectations:
140→      - mcpserver session tools
      - mcpserver todo tools
      - mcpserver requirements tools
      - mcpserver triage tools
      root_hints:
      - F:\GitHub\mcpserver-copilot-plugin
      - $COPILOT_PLUGIN_ROOT
    Cline:
      source_type: Cline
      plugin_name: mcpserver-cline-plugin
150→      plugin_version: 1.83.0
      activation: Cline MCP server configured from server.json.
      startup_command: npm run build && node dist/index.js
      unavailable_failure: MCP_PLUGIN_UNAVAILABLE:Cline
      required_env_vars:
      - CLINE_PLUGIN_ROOT
      - PLUGIN_AGENT_NAME=Cline
      hook_expectations:
      - MCP server startup
      - tool call audit
160→      tool_expectations:
      - session_*
      - req_*
      - triage_*
      root_hints:
      - F:\GitHub\mcpserver-cline-plugin
      - $CLINE_PLUGIN_ROOT
    Grok:
      source_type: GrokCode
      plugin_name: mcpserver-grok-plugin
170→      plugin_version: 1.83.0
      activation: Grok Build loads enabled plugin skills, hooks, and MCP servers from the Grok/Claude-compatible plugin manifests. Use sessionlog_*, todo_*, and requirements_* tool names when the Streamable HTTP MCP server is discoverable; mcp_* names are hosted-agent aliases, and workflow.* names are plugin shim/REPL method names invoked through the Grok plugin skills or repl-invoke helpers, not literal Grok search_tool results.
      startup_command: ''
      unavailable_failure: MCP_PLUGIN_UNAVAILABLE:GrokCode
      required_env_vars:
      - GROK_PLUGIN_ROOT
      - PLUGIN_AGENT_NAME=GrokCode
      hook_expectations:
      - SessionStart
      - UserPromptSubmit
180→      - PostToolUse
      - Stop
      - PlanMode
      tool_expectations:
      - sessionlog_*
      - todo_*
      - requirements_*
      - triage_*
      root_hints:
      - F:\GitHub\mcpserver-grok-plugin
190→      - $GROK_PLUGIN_ROOT
prompt: |
  You are connected to an MCP Context Server at http://PAYTON-LEGION2:7147. Use this server to coordinate your work.

  ## Session Start (Run Once Per Session)
  1. Read this marker file for connection details and API key.
  2. Bootstrap the required plugin named for your agent in the top-level `agent_plugins` contract. Get the plugin through the MCP Server tool registry before relying on local root hints: search `/mcpserver/tools/search?keyword=<plugin_name>` for the installed tool definition and select the result whose `name` exactly equals `<plugin_name>`; if that exact tool definition is missing, install the definition from `/mcpserver/tools/buckets/official/install?toolName=<plugin_name>`, then search again. Execute the exact tool definition's returned `commandTemplate` with `targetParent` set to the plugin parent directory to clone or update the plugin repository. Codex must use `mcpserver-codex-plugin`; Claude must use `mcpserver-claude-code-plugin`; Copilot must use `mcpserver-copilot-plugin`; Cline must use `mcpserver-cline-plugin`; Grok must use `mcpserver-grok-plugin`.
  3. Verify the marker signature using the workspace API key in this file before contacting the server.
  4. Call /health with a random nonce and confirm the response echoes that exact nonce.
  5. Verify and log plugin identity before other MCP work: sourceType, plugin name/version, plugin root or MCP server name, marker signature result, health nonce result, and workspace path.
200→  6. Review recent session history and current TODOs only after signature, nonce, and plugin verification succeed.

### Turn 9 (Function)
[tool_response]
1→---
name: Session Log Management
description: This skill should be used when the user asks to "start session", "log session", "begin turn", "update turn", "complete turn", "query session history"
version: 0.1.0
---

# Session Log Management

## Overview
10→
To manage agent session logs, use the `workflow.sessionlog.*` REPL command namespace via `mcpserver-repl --agent-stdio`. Direct stdio input must be one single-line JSON request envelope per message, not formatted YAML. Session logging captures agent activity, reasoning dialog, file operations, and design decisions as a structured audit trail.

Most session management is automated by the plugin hooks in `hooks/`. This skill covers manual operations, history queries, and the full lifecycle for agents that need direct control.

## Identifier Naming Conventions

### Session IDs

Format: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`
20→
Regex: `^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`

- Agent name must be PascalCase (e.g. `GrokCode`, `ClaudeCode`, `Copilot`, `Cursor`)
- Timestamp must be ISO 8601 compact UTC: `yyyyMMddTHHmmssZ`
- Suffix must be lowercase kebab-case: `feature-auth`, `bugfix-timeout`

Valid examples: `GrokCode-20260527T161500Z-fork-grok-plugin`, `ClaudeCode-20260409T120000Z-implement-auth`, `Copilot-20260304T113901Z-refactor-session`

Invalid: `claudecode-20260409T120000Z-task` (lowercase agent), `Copilot-20260304-feature` (missing time component)
30→
### Request IDs

Format: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`

Regex: `^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`

Valid examples: `req-20260409T120001Z-add-jwt-001`, `req-20260409T120002Z-query-todos`

Invalid: `request-20260409T120001Z-task` (wrong prefix), `req-20260409-task` (missing time)
40→
## Session Lifecycle

### Step 1 — Bootstrap (once per process lifetime)

Bootstrap initializes the session log subsystem. This operation is idempotent:

```yaml
type: request
payload:
50→  requestId: req-20260409T120000Z-bootstrap-001
  method: workflow.sessionlog.bootstrap
  params: {}
```

```yaml
type: result
payload:
  requestId: req-20260409T120000Z-bootstrap-001
  result:
60→    initialized: true
```

### Step 2 — Open Session

To create a new session record at the start of a work session:

```yaml
type: request
payload:
70→  requestId: req-20260409T120001Z-open-001
  method: workflow.sessionlog.openSession
  params:
    agent: ClaudeCode
    sessionId: ClaudeCode-20260409T120001Z-implement-auth
    title: Implement JWT authentication
    model: claude-sonnet-4-6
```

```yaml
80→type: result
payload:
  requestId: req-20260409T120001Z-open-001
  result:
    sessionId: ClaudeCode-20260409T120001Z-implement-auth
    started: 2026-04-09T12:00:01Z
```

### Step 3 — Begin Turn (once per user message)

90→To start a new turn before working on a user request:

```yaml
type: request
payload:
  requestId: req-20260409T120002Z-begin-001
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260409T120002Z-add-jwt-001
    queryTitle: Add JWT authentication
100→    queryText: Implement JWT token generation and validation for the API
```

```yaml
type: result
payload:
  requestId: req-20260409T120002Z-begin-001
  result:
    turnRequestId: req-20260409T120002Z-add-jwt-001
    status: in_progress
110→    timestamp: 2026-04-09T12:00:02Z
```

### Step 4 — Update Turn

To record interpretation, response summary, tags, and referenced files during work:

```yaml
type: request
payload:
120→  requestId: req-20260409T120003Z-update-001
  method: workflow.sessionlog.updateTurn
  params:
    response: Created TokenService and JwtValidator classes
    interpretation: User wants JWT authentication with token generation and validation
    tokenCount: 1250
    tags:
      - feature
      - security
      - FR-AUTH-001
130→    contextList:
      - src/Services/TokenService.cs
      - src/Services/JwtValidator.cs
```

### Step 5 — Append Dialog

To record reasoning steps, tool calls, observations, and decisions as the work progresses:

```yaml
140→type: request
payload:
  requestId: req-20260409T120004Z-dialog-001
  method: workflow.sessionlog.appendDialog
  params:
    dialogItems:
      - timestamp: 2026-04-09T12:00:04Z
        role: model
        content: Analyzing authentication requirements and existing patterns...
        category: reasoning
150→      - timestamp: 2026-04-09T12:00:05Z
        role: model
        content: |
          Decision: Use HS256 for JWT signing.
          Rationale: Symmetric key simplifies key management for this internal service.
          Alternatives: RS256 adds key distribution complexity without benefit here.
        category: decision
```

Valid `category` values: `reasoning`, `tool_call`, `tool_result`, `observation`, `decision`.
160→
Valid `role` values: `model`, `tool`, `system`, `user`.

### Step 6 — Append Actions

To record file operations and other work artifacts:

```yaml
type: request
payload:
170→  requestId: req-20260409T120005Z-actions-001
  method: workflow.sessionlog.appendActions
  params:
    actions:
      - order: 1
        description: Created TokenService with JWT generation
        type: create
        status: completed
        filePath: src/Services/TokenService.cs
      - order: 2
180→        description: Created JwtValidator for token validation
        type: create
        status: completed
        filePath: src/Services/JwtValidator.cs
      - order: 3
        description: Chose HS256 for JWT signing (internal service, symmetric key)
        type: design_decision
        status: completed
        filePath: ""
```
190→
Standard action `type` values: `edit`, `create`, `delete`, `design_decision`, `commit`, `pr_comment`, `issue_comment`, `web_reference`, `dependency_add`.

### Step 7 — Complete Turn

To finalize a turn as successfully completed (immutable after this call):

```yaml
type: request
payload:
200→  requestId: req-20260409T120006Z-complete-001
  method: workflow.sessionlog.completeTurn
  params:
    response: |
      JWT authentication implemented:
      - TokenService generates HS256-signed tokens
      - JwtValidator validates and extracts claims
      - Services registered in Startup.cs
      - All unit tests passing
```
210→
### Failing a Turn

To mark a turn as failed with an error description:

```yaml
type: request
payload:
  requestId: req-20260409T120007Z-fail-001
  method: workflow.sessionlog.failTurn
220→  params:
    errorMessage: Unable to complete — missing System.IdentityModel.Tokens.Jwt package
    errorCode: dependency_missing
```

Both `completeTurn` and `failTurn` produce an immutable terminal state. No further `updateTurn`, `appendDialog`, or `appendActions` calls are allowed on a completed or failed turn.

## Querying Session History

To browse previous sessions for context continuity:
230→
```yaml
type: request
payload:
  requestId: req-20260409T120008Z-history-001
  method: workflow.sessionlog.queryHistory
  params:
    agent: ClaudeCode
    limit: 10
    offset: 0
240→```

```yaml
type: result
payload:
  requestId: req-20260409T120008Z-history-001
  result:
    sessions:
      - agent: ClaudeCode
        sessionId: ClaudeCode-20260409T120001Z-implement-auth
250→        title: Implement JWT authentication
        model: claude-sonnet-4-6
        started: 2026-04-09T12:00:01Z
        lastUpdated: 2026-04-09T12:30:00Z
        status: completed
        turnCount: 3
        filesModifiedCount: 5
        tags: [auth, jwt, security]
    totalCount: 1
    offset: 0
260→    limit: 10
```

Omit `agent` to query across all agents. Use `offset` and `limit` for pagination.

## Turn Lifecycle State Machine

A turn transitions through these states only in forward order:

1. `in_progress` — created via `beginTurn`, open for updates
270→2. `completed` — finalized via `completeTurn` (immutable)
3. `failed` — finalized via `failTurn` (immutable)

Any attempt to call `updateTurn`, `appendDialog`, or `appendActions` on a `completed` or `failed` turn returns a `turn_immutable` error.

## Error Handling

```yaml
type: error
payload:
280→  requestId: req-20260409T120003Z-update-001
  code: turn_immutable
  message: Turn is immutable (status: completed)
  details:
    turnRequestId: req-20260409T120002Z-add-jwt-001
    currentStatus: completed
    hint: Begin a new turn instead
```

Common error codes:
290→
- `session_not_found` — no active session; call `openSession` first
- `session_already_exists` — session ID already in use
- `invalid_session_id` — session ID format violation
- `invalid_request_id` — request ID format violation
- `turn_not_found` — no active turn; call `beginTurn` first
- `turn_already_exists` — turn with same request ID already exists
- `turn_immutable` — cannot modify completed or failed turn

## Session Continuity After Restart
300→
After a server restart or agent reconnect:

1. Call `workflow.sessionlog.queryHistory` to review past sessions
2. Decide whether to continue in a new session referencing the previous one in the title, or to re-open fresh
3. Re-read `AGENTS-README-FIRST.yaml` for the rotated API key before making any calls
4. Call `bootstrap` again (idempotent) then `openSession` for the new session

## Codex CLI JSONL Integration

310→The plugin automatically captures rich session fields from Codex CLI JSONL transcripts. No manual steps are required for normal Codex sessions.

### Automatic Rich Field Capture

When a Codex CLI session ends, `final-response.sh` reads the session JSONL (from `CODEX_SESSION_FILE`, `CODEX_ROLLOUT_FILE`, or the most recent `~/.codex/sessions/YYYY/MM/DD/rollout-*.jsonl`) and calls `lib/codex-jsonl-enrich.js` to extract:

- `interpretation` - first agent message from the last turn
- `processingDialog` - all agent messages, tool calls, and observations
- `actions` - file edits from `patch_apply_end` events and write tool calls
- `filesModified` - all files changed in the turn
320→- `contextList` - files read as context
- `designDecisions` - agent messages containing `Decision:` or `Rationale:`
- `requirementsDiscovered` - FR/TR/TEST IDs mentioned in agent messages
- `blockers` - `turn_aborted` events

These fields are passed to `completeTurn`, which routes through `importRecovery` to persist them on the server. Secret values (API keys, Bearer tokens) are redacted before logging.

### Subagent Transcript Import

When a Codex parent session spawns subagents, the `hooks/scripts/subagent-import.sh` hook (called on `subagentComplete`) discovers and imports each subagent JSONL as a first-class MCP session-log turn.
330→
Subagent sessions are identified by `source.subagent.thread_spawn.parent_thread_id` in the JSONL `session_meta` line. The plugin scans `$CODEX_SESSION_DIR` (default: `~/.codex/sessions/`) for matching files.

Each imported subagent turn:
- Gets tags `["subagent", "<nickname>"]` and optionally `["parent:<parentReqId>"]`
- Uses a deterministic request ID: `req-<subagentStartTs>-subagent-<nickname>-<turnId>`
- Is idempotent: a tracker file prevents re-importing the same session twice

### Manual Recovery

340→To manually import a Codex JSONL transcript:

```bash
# Extract tab-delimited import lines (method, base64-params, label)
node lib/codex-jsonl.js import /path/to/rollout.jsonl "SessionId-20260525T100000Z-fix" > import-lines.txt

# Dispatch each line via repl-invoke
while IFS=$'\t' read -r method params_b64 label; do
    params="$(printf '%s' "$params_b64" | base64 --decode)"
    repl_invoke "$method" "$params"
350→done < import-lines.txt
```

To discover subagent transcripts for a parent:

```bash
CODEX_SESSION_DIR=/path/to/sessions node lib/codex-jsonl.js subagents /path/to/parent-rollout.jsonl
```

### Non-Destructive Merge
360→
Server-side rich fields are never overwritten by sparse incoming values. `lib/sessionlog-submit-body.js` uses field-level merge: an empty incoming array never replaces a non-empty server-side array. This allows `completeTurn` and `importRecovery` to be called safely after rich fields have already been captured.

### Secret Redaction

The following patterns are redacted from all JSONL-extracted text before logging:

- `X-Api-Key:` header values
- `apiKey:` parameter values (20+ character strings)
- `Bearer <token>` values
370→- `Authorization:` header values

## Implementation Notes

- Use `repl_invoke` from `lib/repl-invoke.sh` to build and dispatch envelopes.
- Generate request IDs with the current UTC timestamp to guarantee uniqueness: `req-$(date -u +%Y%m%dT%H%M%SZ)-<slug>`.
- Post `beginTurn` before starting any work on a user request; post `completeTurn` or `failTurn` after work ends. Never defer these calls.
- Log all design decisions in `appendDialog` with `category: decision` AND in `appendActions` with `type: design_decision`.
- Record all web sources consulted as actions with `type: web_reference` and include the URL in the description.
- At approximately every 10 interactions, verify all turns are persisted and all design decisions are captured before continuing.
380→

### Turn 10 (Function)
[tool_response]
1→# ViceSharp Handoff - 2026-07-14

**Active branch:** `feat/xbox-uwp-app` (off `main`) at `a199063`. **PLAN-XBOXUWP** (the UWP-on-Xbox-console head) is deep in dev-PC bring-up; ~65 commits local, **NOT pushed** (Azure DevOps `origin` only, on explicit request). The full running detail lives in the auto-memory `project_xbox_uwp_app.md`; the snapshot below is the resume state.
**Parallel worktree (ACTIVE TASK 2026-07-14 23:31):** `F:\GitHub\vice-sharp-romless` on branch `fix/romless-vic-content` (off `ed0b67f`) processes `docs/romless-vic-badline-fix.md` (VIC-II raises zero bad lines for a KERNAL-less host-driven machine; bitmap renders black). Repro test already present in that worktree. MCP TODO `FIX-ROMLESSVIC-001` (high, Chips).
**Prior iteration-1 baseline:** `main`/`master` at `v1.0.2` (2026-07-08); **PLAN-VICEPARITY-001 COMPLETE** (466/466 SID parity ACs bit-exact vs reSID). Detail preserved in the dated sections below.
**feat/xbox-uwp-app working tree:** the `tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json` edit and untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md` are uncommitted scratch, not part of the plan.

## PLAN-XBOXUWP resume snapshot (2026-07-14)

10→- **Runs and renders the C64 on the dev PC.** The whole app (UWP + Windows.Gaming.Input + raw-DX11 SwapChainPanel + XAudio2) validates on Windows 11; the physical console is only needed at Store submission (slice S42, deferred). Autonomous on-device loop this session: build via VS MSBuild, `./build.ps1 DeployXboxLocal`, drive with computer-use, read receipts from `%LOCALAPPDATA%\Packages\sharpninja.ViceSharp.Xbox_wbxy578d68v3y\LocalState\vicesharp.log`.
- **Deploy loop:** `./build.ps1 DeployXboxLocal` (stop app -> VS MSBuild `/p:Configuration=Release-UWP /p:Platform=x64 /restore /t:Build,GenerateProjectPriFile` -> robocopy `/S` -no-`/XO`- over the registered AppX layout -> `shell:AppsFolder` launch). Dev toggles: **F9** virtual keyboard, **F11** menu (F10 is a Windows system key; ESC works but is eaten in some focus states; a CoreWindow.KeyDown backstop covers focus-nowhere). **End every CLI batch with a Debug-UWP restore** (`/p:Configuration=Debug-UWP /p:Platform=x64 /t:Restore`) or the operator's VS F5 fails NETSDK1005.
- **CRITICAL finding FIX-XKBDPANEL-001:** `PublishAot=true` at BUILD time flips CsWinRT into AOT binding mode and kills every reflection `{Binding}` app-wide (empty keyboard, blank Settings, no menu title) with no exception and no BindingFailed. AOT is now an explicit opt-in via `ViceSharpPublishAot=true`; Release-UWP builds stay JIT (SPD ~101%). **Store-AOT requires an AOT-safe-bindings slice (x:Bind / generated bindable metadata) BEFORE anyone re-arms PublishAot.**
- **Latest shipped (device-verified):** FIX-XKBDPANEL-001 (AOT opt-in + `/XO` Frankenbuild fix + F9/F11 + BindingFailed logging), FEAT-XMENUSUBPAGE-001 (B steps back on subpages / dismisses only on the home rail; subpages expand across both columns over an opaque backdrop and contract to the docked rail). Gate: **Category=Xbox 501/501**.
- **OPEN:** Controls-page keyboard-shortcut config (operator-requested, never started); B-on-controller verification (needs the operator's controller); FIX-XKBDNMI-001 (RESTORE -> CPU NMI edge unwired); per-model ROM-availability in the picker; S42 Tier-C console/Store (deferred); MCP `sessionlog.completeTurn` is flaky (complete turns directly, verify).

## How to resume

1. Read `AGENTS-README-FIRST.yaml` (marker rotates on server restart; verify signature + /health nonce before MCP work).
2. Route TODO / session-log / requirements / triage through the mcpserver plugin (repl-invoke or wrappers), never raw REST, never storage files.
20→3. Baseline gate (one process, ~20 min, run detached):
   `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`
   Green criterion: 0 failed, 21 skipped, total 2612-2615 (totals wobble in that band from cosmetic xunit trx name serialization of dynamic theory rows; per-class row sets are identical - never chase a fixed total).

## Shipped 2026-07-08 (all CI-confirmed)

- **v1.0.2 released**: tag on `534cded`, release run 1074, all 13 package ids verified in the nuget.org flat container (Core bundle, SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox; Console + Avalonia are dotnet tools).
- **PLAN-NATIVERESIDUE-001 closed** (`ba0f94f`): a `.vsf` whose DRIVE8 module carries `has_tde=0` disabled TrueDrive process-wide via `resources_set_int` (drive-snapshot.c:334-363); `vice_machine_create_model` now re-baselines `Drive{8..11}TrueEmulation` to the VICE default 1 through `resources_set_int` so iecbus statics and $DD00 callbacks recompute. SnapshotResume two-process partition removed; suite proven green in ONE process. Guards: TEST-NATIVE-RESIDUE-01/02 in `NativeResidueDiagTests`.
- **CI made self-sufficient** (`a229801`, `d8f61cf`, `4e3db9b`): agents have no ROMs and no shim, so `EnsureCiRomRoot` in `build/Build.cs` stages 13 hash-pinned VICE data files (C64 ROMs incl. all variant kernals, DRIVES dos1541/dos1541ii, gtk3_pos.vkm) from the VICE-Team svn-mirror into `artifacts/vice-data` and sets `VICESHARP_ROM_PATH` for the test process only. Shim-dependent tests MUST use `[ViceFact]`/`[ViceTheory]` (plain `[Fact]` throws DllNotFoundException on shimless agents). First-ever green CI run: 1072.
- **Docs reconciled to shipped reality** (`aee29ea`, 88 files): README, USER-GUIDE, VICE-MIGRATION, ROMs, Architecture, Public-API (regenerated from the 50 real Abstractions interfaces), requirements docs; 22 stale files pruned; `docs/wiki.yaml` manifest lists all 28 wiki documents; winget license fixed MIT -> GPL-2.0-or-later.
30→- **github divergence resolved** (`20b330c`): github-only commit `42f2f26` (VicModeChangeEvent seam) merged with strategy `ours` (tree unchanged; content preserved on `rescue/vic-modechange-event`); github main fast-forwarded, tags identical on both remotes; github wiki published per manifest (`b455b88`, 28 pages + sidebar).
- **Dependency wave S0-S8 complete** (TR-DEPS-202607-001 + TEST-DEPS-202607-001, both completed with evidence; commits `72696d9`, `055b9d5`, `26e5ca8`, `87eb80a`, `9214cdb`, `1cad822`+`1adf386`, `23d030a`, `0f5192f`): all 30 central entries at newest mutually compatible stables (Avalonia 12.0.5, Extensions/TestHost 10.0.9, Protobuf 3.35.1, Grpc.Tools 2.82.0, Test.Sdk 18.7.0, Roslyn pair 5.6.0, YamlDotNet 18.1.0, FluentAssertions 8.10.0, coverlet 10.0.1, aiUnit 2.1.3, RemoteControl 0.7.4); 5 dead pins removed; AiReview under CPM with inherited TreatWarningsAsErrors; vendored `nuget-local` feed DELETED (NuGet.config is nuget.org-only); `global.json` floor 10.0.301 (Roslyn 5.6 generator gate). Excluded as prerelease: xunit.v3 4.x, NSubstitute 6.x, Protobuf 4.x, Extensions 11.x.
- **Test fixtures in repo** (`93cdc7e`): `vice-snapshot-20260630171307.vsf` (read by the residue probes and lockstep re-baseline suites at repo root; do not delete), `native/.build-nopatch.sh`, `native/.ccwrap/*`. Operator rule: if it is needed to run a test, it goes in the repo.

## Iteration-1 completion (branch `fix/nativeresidue-002-drive-clock-hardening`, off `master`; NOT pushed)

Working through the "complete outstanding iteration 1 work" plan. Done, all committed locally, gates green:
- **Slice A - PLAN-NATIVERESIDUE-002** (710c1e9/8894f1a/9fb06dd/0691344): the two latent drive-clock bugs fixed; **BUG-LOCKSTEP-001 CLOSED** (full gate 0 failed / 2596 passed / 21 skipped). See docs/receipts-nativeresidue-002-2026-07-09.md.
- **Slice F - BUG-TESTDEBT-001**: verified already fixed at HEAD (6a32e7c/77e7190), closed with receipt.
- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.
40→
**SID parity program COMPLETE (2026-07-10)** - all remaining slices landed:
- **S11 / S11b**: 8580 reSID filter port (filter8580new m==1) + write pipeline + per-model ttl(0xA2000)/scaleFactor(5); shim exports vice_sid_exact_set_sampling + vice_sid_exact_clock_buffered; DATABUS-07 flipped; oracle via "c64c". Ratchet ->444.
- **S12**: amplify(scaleFactor)/clip PCM16 seam + extfilt enable branch; OUTPUT-01..07. Ratchet ->451.
- **S13**: fixed-point Kaiser FIR resampler (fast/interpolate/resample); OUTPUT-08..13. Ratchet ->457.
- **CH**: retired the dead Chamberlin SVF stack (10 members + 7 guards + SidFilter6581Tests.cs). PLAN-SIDCHAMBERLIN-001 closed.
- **BC**: ported reSID batched SID::clock(delta_t) (Sid6581.BatchedClock.cs); SAMPLE_FAST now value-bit-exact; un-pended the last 4 SID quarantines + authored the last 9 (EXTFILT-01..07, CLOCK-05/08). Ratchet ->466 with a strict completion pin (Assert.Equal(466, covered)).
- **LW** (4924b94, pushed): live audio wired through the reSID SAMPLE_RESAMPLE engine (Resample always, VICE x64sc parity); push tail bit-exact vs the buffered pull; warp = cadence-only; zero-alloc; benchmark in SidSamplingBenchmarks.
- **CL** (a38964b/e100433, local): NativeCollectionConventionTests + [Collection("NativeVice")] on the 6 native-bridge test classes.
- **G**: DROPPED per operator (ADO wiki push).
50→
Closure gates: parity 466/0/0 (completion pin holds), determinism 5/0, XmlDocs green, full baseline 0 NEW failures (7 pre-existing VideoRendererTests only) on the clean re-run. One run-1 full-suite native ACCESS_VIOLATION did NOT reproduce (flaky native-shim instability; candidate PLAN-NATIVERESIDUE-002 follow-up, not a SID blocker) - see the closure receipts.

**Native build recipe (learned this session, load-bearing):** `make x64sc-program` does NOT rebuild changed VICE-core `.o` (they stay stale). Before rebuilding, delete the changed `.o` (and `libdrive.a` for drive files), then run under the MSYS2 MINGW64 login shell so the compiler gets a writable `/tmp`:
`MSYSTEM=MINGW64 /c/msys64/usr/bin/bash.exe -lc 'rm -f <changed>.o; bash /f/GitHub/vice-sharp/native/.build-nopatch.sh'`. The dll is gitignored (built locally). Vendored edits go in native/patches/vice-shim-runtime.patch (regenerate via `git -C native/vice diff`; keep the 5 pre-existing hunks byte-identical).

**Oracle note:** SidExactRead (read path) is the reliable bus observable; SidExactGetState().BusValue export is NOT a dependable live-latch snapshot (returned stale/garbage) - compare via the read path + managed spec constants.

## In flight (interrupted mid-task)

60→Plugin reload + Agent Help (mcpserver core synced to 1.36.0, `mcpserver-repl` 1.4.5 confirmed on PATH):
1. Validate cache: `%USERPROFILE%\.claude\plugins\cache` has `mcpserver-local` (active family; confirm 1.36.0 from the plugin's own `.claude-plugin/plugin.json`), `mcpserver-cowork` (stale candidate - verify before deleting), `caveman` (unrelated, keep). The version inspection was interrupted by a transient permission-classifier outage; re-run it.
2. Run the claude-hook-validation skill; refresh MCP hooks if missing; restart Claude Code if hooks were installed.
3. Open a `workflow.agenthelp.createSession` (new in repl 1.4.5) and submit the outstanding MCP issues:
   - repl-invoke `Invoke-WorkflowAppendActions` audit counters never increment: regex `(?m)^\s*type:` misses `- type:` list items (repl-invoke.ps1:891-897; filed as triage-report-8a6539340a174c00a57dee53ec8f42ea).
   - No per-action readback: `workflow.sessionlog.queryHistory` returns only session headers, so appends cannot be content-verified; session header `lastUpdated`/`filesModifiedCount` never advance (anchored to `started`).
   - `Invoke-McpPlugin.ps1` defaults `-Command Status`: calling it with only `-Method` silently prints the status blob instead of invoking (footgun; cost one silent append failure).
   - `workflow.requirements.createTr` requires `subarea` (schema) but the skill docs do not mention it; the server also derived `subarea: 202607` from the id rather than honoring the supplied value.
4. Log the reload + help-session outcomes; triage any confirmed server defects.

70→## Parked items

- Azure DevOps wiki push needs `ADO_PAT` set, then `tools/Publish-Wiki.ps1 -Target azure` (github wiki already published).
- ~~Latent VICE bugs documented, not fixed~~ FIXED 2026-07-09 (PLAN-NATIVERESIDUE-002, branch `fix/nativeresidue-002-drive-clock-hardening`): drive `attach_clk`/`detach_clk`/`attach_detach_clk` uninitialized-stack read on `has_tde=0` restore (drive-snapshot.c zero-init) and `cycle_accum` omitted from `drivecpu_reset_clk` (drivecpu.c + drivecpu65c02.c) both fixed via the vendored runtime patch, plus a shim create-time drive-clock re-baseline. **BUG-LOCKSTEP-001 CLOSED**: full baseline gate 0 failed / 2596 passed / 21 skipped / 2617 total (was 136+2 lockstep failures). Receipts: docs/receipts-nativeresidue-002-2026-07-09.md. New residue candidates recorded there (live unit->type not re-baselined; drivecpu65c02 cycle_accum SMW/SMR width asymmetry). Build note: `make x64sc-program` does NOT rebuild changed VICE-core `.o`; delete the stale `.o`+`libdrive.a` and build under `MSYSTEM=MINGW64 bash -lc`.
- ~~VICE parity program (PLAN-VICEPARITY-001) remaining slices~~ **COMPLETE 2026-07-10** (SID side fully bit-exact vs reSID; VIC per-cycle work landed through V7 + audit phases). Only manifest-wide pending AC left is TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock conflict, out of SID scope, needs the parity owner). Candidate follow-up: PLAN-NATIVERESIDUE-002 native-shim lifecycle hardening (a sustained-consecutive-native-load ACCESS_VIOLATION observed once, non-reproducing).

## Open TODO backlog (MCP store)

PLAN-UIDOCK-001 (Dock.Avalonia UI), PLAN-AUDIOEQ-001, PLAN-PLAYLIST-001, PLAN-FULLSCREEN-001, PLAN-DEVCARDART-001, PLAN-MONFRAME-001, PLAN-DRIVE1581-001, PLAN-DRIVE1541II-001, PLAN-DRIVE1540-001, PLAN-DRIVE1571-001, PLAN-DRIVECMDHD-001, PLAN-CARTRAMLINK-001, PLAN-ARCHVIC20-001 (query the store for the live list; this snapshot is 2026-07-09).

80→## Operational gotchas (hard-won this session)

- Long test runs: OS-detach with `Start-Process dotnet ... -RedirectStandardOutput` and tail the log with a Monitor; never run two test/build invocations concurrently (native lock + obj/bin contention); verify the log grows before trusting a launch.
- `git add a b c bad-path` aborts the WHOLE add on one bad pathspec and a following commit ships whatever was staged earlier; always check the commit stat against intent.
- Diagnostic probes: `LiveLimiterBandProbeTests` reports via a by-design `Assert.Fail`; `Demo_SilentWarp` headroom is host-load sensitive (re-run in isolation before believing a failure). Live-app probes attach via `%LOCALAPPDATA%\ViceSharp\debug-attach.json`; NEVER dispose the probe client (it kills the session).
- github force-push is classifier-blocked regardless of instruction phrasing; the sanctioned divergence pattern is merge `-s ours` + fast-forward.


### Turn 11 (Assistant)
Loading the full operator profile and finishing MCP bootstrap (health, session open).
[tool_request: list_dir]
- target_directory: C:\Users\kingd\.claude\profile
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
- offset: 190
- limit: 150
[tool_request: search_tool]
- query: sessionlog bootstrap openSession
- limit: 10
[tool_request: run_terminal_command]
- command: Get-Date -Format "yyyy-MM-dd HH:mm:ss K"; Get-Date -AsUTC | ForEach-Object { $_.ToString("yyyy-MM-ddTHH:mm:ssZ") }
- description: Get local and UTC timestamps

### Turn 12 (Function)
[tool_response]
{
  "results": [
    {
      "server": "pwsh",
      "tools": [
        {
          "tool_name": "pwsh__get_current_location",
          "description": "Retrieves the current location and all available drives (providers) from the PowerShell session. Returns current_location and other_drive_locations array. Call this when you need to understand the current PowerShell context, as users may change location during the session. When executing multiple invoke_expression commands in succession, calling once at the beginning is sufficient.",
          "score": 5.336750030517578,
          "input_schema": {
            "type": "object",
            "properties": {
              "agent_id": {
                "description": "Agent ID for sub-agent console isolation. Obtain this by calling start_console with is_subagent=true. Do not pass arbitrary strings.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "is_subagent": {
                "description": "Set to true if you are a sub-agent. A unique agent_id will be allocated and returned in the response. Use that agent_id for all subsequent tool calls.",
                "type": "boolean",
                "default": false
              }
            }
          }
        },
        {
          "tool_name": "pwsh__wait_for_completion",
          "description": "Wait for busy console(s) to complete and retrieve cached results. Use this after receiving 'Pipeline is still running' response instead of executing Start-Sleep (which would open a new console).",
          "score": 4.250094890594482,
          "input_schema": {
            "type": "object",
            "properties": {
              "timeout_seconds": {
                "description": "Maximum seconds to wait for completion (1-170, default: 30). Returns early if a console completes.",
                "type": "integer",
                "default": 30
              },
              "agent_id": {
                "description": "Agent ID for sub-agent console isolation. Obtain this by calling start_console with is_subagent=true. Do not pass arbitrary strings.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "is_subagent": {
                "description": "Set to true if you are a sub-agent. A unique agent_id will be allocated and returned in the response. Use that agent_id for all subsequent tool calls.",
                "type": "boolean",
                "default": false
              }
            }
          }
        },
        {
          "tool_name": "pwsh__start_console",
          "description": "Ensure a PowerShell console is available, or launch a new one. When reason is empty or omitted, reuses an existing standby console if one is available. When reason is provided, always launches a new console regardless of existing ones.\r\n\r\n📌 This is your primary tool for all command execution tasks: directory navigation, git operations, build/test commands, file system operations, process management, environment variable access, and any shell/terminal task. Sessions persist across calls (authentication, variables, modules stay active). Install any PowerShell Gallery module without admin privileges to extend capabilities (e.g., Az for Azure, AWS.Tools for AWS, Microsoft.Graph for M365).",
          "score": 2.7348108291625977,
          "input_schema": {
            "type": "object",
            "properties": {
              "reason": {
                "description": "Do NOT specify unless you need a separate console. Forces a new console launch. Omit to reuse an existing standby console (preferred).",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "banner": {
                "description": "Message displayed at console startup (e.g. greeting, joke, fun fact). Be creative and make the user smile!",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "start_location": {
                "description": "Optional starting directory path. If relative, resolved from home directory. Defaults to home directory if not specified.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "agent_id": {
                "description": "Agent ID for sub-agent console isolation. Obtain this by calling start_console with is_subagent=true. Do not pass arbitrary strings.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "is_subagent": {
                "description": "Set to true if you are a sub-agent. A unique agent_id will be allocated and returned in the response. Use that agent_id for all subsequent tool calls.",
                "type": "boolean",
                "default": false
              }
            }
          }
        },
        {
          "tool_name": "pwsh__invoke_expression",
          "description": "Execute PowerShell cmdlets and CLI tools (e.g., git) in persistent console. Session persists: modules, variables, functions, authentication stay active—no re-authentication. Install any modules and learn them via Get-Help.\r\n\r\n📌 This is your primary tool for all command execution tasks: directory navigation, git operations, build/test commands, file system operations, process management, environment variable access, and any shell/terminal task. Sessions persist across calls—variables, modules, functions, and authentication stay active. Install any PowerShell Gallery module to extend capabilities (e.g., Az for Azure, AWS.Tools for AWS, Microsoft.Graph for M365).\r\n\r\n💡 API Exploration: Use Invoke-RestMethod to explore Web APIs and Add-Type for Win32 API testing. Verify API behavior before writing production code—get immediate feedback without compilation.\r\n\r\n⚠️ CRITICAL - Variable Scope:\r\nLocal variables are NOT preserved between invoke_expression calls. Use $script: or $global: scope to share variables across calls.\r\n\r\n⚠️ CRITICAL - String Interpolation:\r\nDouble-quoted strings expand variables and subexpressions: \"$var\" becomes the value of $var, \"$(expr)\" evaluates expr. Use single quotes for literal strings: '$var' keeps the text $var as-is.\r\n\r\n📝 Text File Operations:\r\nALWAYS use the specialized cmdlets for text file editing: Show-TextFiles, Add-LinesToFile, Update-LinesInFile, Update-MatchInFile, Remove-LinesFromFile.\r\nNEVER use Set-Content, [IO.File]::WriteAllText, or other alternatives—even when source code contains $ or backtick characters. Instead, pass content via var1-var4 parameters.\r\nCreate new file: Add-LinesToFile path -Content $var1 (with var1 parameter containing the content)\r\nEdit existing file: Add-LinesToFile, Update-LinesInFile, Update-MatchInFile, Remove-LinesFromFile (use var1-var4 for content with $, backtick, or quotes)\r\nFor detailed examples: invoke_expression('Get-Help <cmdlet-name> -Examples')\r\nEdit cmdlets show changed lines with 2 lines of context. Use Show-TextFiles after … [truncated]",
          "score": 2.513589859008789,
          "input_schema": {
            "type": "object",
            "properties": {
              "pipeline": {
                "description": "The PowerShell command or pipeline to execute. Multi-line commands (if, loops, try-catch, etc.) are supported.",
                "type": "string"
              },
              "timeout_seconds": {
                "description": "Timeout in seconds (0-170, default: 170). On timeout, execution continues in background and result is cached for retrieval on next tool call. Use 0 for commands requiring user interaction (e.g., pause, Read-Host).",
                "type": "integer",
                "default": 170
              },
              "var1": {
                "description": "Literal string value injected as $var1 in the pipeline, bypassing the PowerShell parser.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "var2": {
                "description": "Literal string value injected as $var2 in the pipeline, bypassing the PowerShell parser.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "var3": {
                "description": "Literal string value injected as $var3 in the pipeline, bypassing the PowerShell parser.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "var4": {
                "description": "Literal string value injected as $var4 in the pipeline, bypassing the PowerShell parser.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "agent_id": {
                "description": "Agent ID for sub-agent console isolation. Obtain this by calling start_console with is_subagent=true. Do not pass arbitrary strings.",
                "type": [
                  "string",
                  "null"
                ],
                "default": null
              },
              "is_subagent": {
                "description": "Set to true if you are a sub-agent. A unique agent_id will be allocated and returned in the response. Use that agent_id for all subsequent tool calls.",
                "type": "boolean",
                "default": false
              }
            },
            "required": [
              "pipeline"
            ]
          }
        }
      ]
    },
    {
      "server": "github",
      "tools": [
        {
          "tool_name": "github__search_orgs",
          "description": "Find GitHub organizations by name, location, or other organization metadata. Ideal for discovering companies, open source foundations, or teams.",
          "score": 4.422033309936523,
          "input_schema": {
            "type": "object",
            "properties": {
              "order": {
                "type": "string",
                "description": "Sort order",
                "enum": [
                  "asc",
                  "desc"
                ]
              },
              "page": {
                "type": "number",
                "description": "Page number for pagination (min 1)",
                "minimum": 1
              },
              "perPage": {
                "type": "number",
                "description": "Results per page for pagination (min 1, max 100)",
                "minimum": 1,
                "maximum": 100
              },
              "query": {
                "type": "string",
                "description": "Organization search query. Examples: 'microsoft', 'location:california', 'created:>=2025-01-01'. Search is automatically scoped to type:org."
              },
              "sort": {
                "type": "string",
                "description": "Sort field by category",
                "enum": [
                  "followers",
                  "repositories",
                  "joined"
                ]
              }
            },
            "required": [
              "query"
            ]
          }
        },
        {
          "tool_name": "github__semantic_issues_search",
          "description": "Search for issues using natural language queries within a GitHub repository or across the user's top repositories.\n\t\t\t\t\tThis tool uses embedded vector data to find relevant issues,\n\t\t\t\t\teven if they don't contain your exact keywords.\n\t\t\t\t\tThe embeddings are pre-computed and updated regularly,\n\t\t\t\t\tso the search is a fast and cheap way to find related issues.\n\n\t\t\t\t\tUse this tool when:\n\t\t\t\t\t- You want to find issues related to a concept or topic\n\t\t\t\t\t- You want related / similar issues without enumerating every keyword\n\t\t\t\t\t- You're exploring or de-duplicating problem reports\n\t\t\t\t\t- You are researching introspection queries about a repo (such as when asking about the most requested features or progress on a specific feature), since Issues represent the planning & tracking portion of the work\n\n\t\t\t\t\tBenefits:\n\t\t\t\t\t- Captures synonyms & paraphrases (e.g. \"screen reader focus loss\" vs \"VoiceOver loses focus\")\n\t\t\t\t\t- Reduces missed matches from narrow keyword lists\n\n\t\t\t\t\tExamples:\n\t\t\t\t\t- Find authentication bugs: query=\"authentication login errors\", owner=\"octocat\", repo=\"Hello-World\"\n\t\t\t\t\t- Find open issues about auth authored by a specific user: query=\"state:open author:monalisa User permissions, access control, and authentication errors\", owner=\"octocat\", repo=\"Hello-World\"\n\t\t\t\t\t- Find performance problems: query=\"slow rendering performance\", owner=\"octocat\", repo=\"Hello-World\"\n\t\t\t\t\t- Find issues across the user's top repositories: query=\"memory leak\"\n\n\t\t\t\t\tIf no repository is specified, searches across the user's top repositories. For more targeted results, specify a specific repository.\n\t\t",
          "score": 1.7639033794403076,
          "input_schema": {
            "type": "object",
            "properties": {
              "order": {
                "type": "string",
                "description": "Sort order",
                "enum": [
                  "asc",
                  "desc"
                ]
              },
              "owner": {
                "type": "string",
                "description": "Repository owner. Required if `repo` is specified.",
                "x-mcp-header": "owner"
              },
              "page": {
                "type": "number",
                "description": "Page number for pagination (min 1)",
                "minimum": 1
              },
              "query": {
                "type": "string",
                "description": "Natural language query with optional GitHub search qualifiers. Supports semantic matching. Examples: 'authentication login errors', 'state:open author:username performance issues'. Supports advanced GitHub issue search syntax for filtering by state, author, labels, etc."
              },
              "repo": {
                "type": "string",
                "description": "Repository name. Required if `owner` is specified.",
                "x-mcp-header": "repo"
              },
              "sort": {
                "type": "string",
                "description": "Sort field for search results. Defaults to best match.",
                "enum": [
                  "comments",
                  "reactions",
                  "reactions-+1",
                  "reactions--1",
                  "reactions-smile",
                  "reactions-thinking_face",
                  "reactions-heart",
                  "reactions-tada",
                  "interactions",
                  "created",
                  "updated"
                ]
              }
            },
            "required": [
              "query"
            ],
            "dependentRequired": {
              "owner": [
                "repo"
              ],
              "repo": [
                "owner"
              ]
            }
          }
        }
      ]
    }
  ],
  "total_hidden_tools": 148,
  "status": "ready",
  "note": null
}

### Turn 13 (Function)
[tool_response]
- C:\Users\kingd\.claude\profile/
  - accuracy-first-verify-sources.md
  - add-profile.grok.md
  - approve-before-execute.md
  - bring-the-receipts.md
  - lab-authorization.md
  - log-decisions-as-conclusions.md
  - never-skip-explicit-actions.md
  - no-attitude-honesty-tell.md
  - no-python-lab.md
  - no-shortcuts-precision-over-convenience.md
  - philosophical-dialogue-mode.md
  - PROFILE.md
  - session-turn-title-summary.md
  - user-payton-byrd.md

### Turn 14 (Function)
[tool_response]
190→      - $GROK_PLUGIN_ROOT
prompt: |
  You are connected to an MCP Context Server at http://PAYTON-LEGION2:7147. Use this server to coordinate your work.

  ## Session Start (Run Once Per Session)
  1. Read this marker file for connection details and API key.
  2. Bootstrap the required plugin named for your agent in the top-level `agent_plugins` contract. Get the plugin through the MCP Server tool registry before relying on local root hints: search `/mcpserver/tools/search?keyword=<plugin_name>` for the installed tool definition and select the result whose `name` exactly equals `<plugin_name>`; if that exact tool definition is missing, install the definition from `/mcpserver/tools/buckets/official/install?toolName=<plugin_name>`, then search again. Execute the exact tool definition's returned `commandTemplate` with `targetParent` set to the plugin parent directory to clone or update the plugin repository. Codex must use `mcpserver-codex-plugin`; Claude must use `mcpserver-claude-code-plugin`; Copilot must use `mcpserver-copilot-plugin`; Cline must use `mcpserver-cline-plugin`; Grok must use `mcpserver-grok-plugin`.
  3. Verify the marker signature using the workspace API key in this file before contacting the server.
  4. Call /health with a random nonce and confirm the response echoes that exact nonce.
  5. Verify and log plugin identity before other MCP work: sourceType, plugin name/version, plugin root or MCP server name, marker signature result, health nonce result, and workspace path.
200→  6. Review recent session history and current TODOs only after signature, nonce, and plugin verification succeed.
  7. Post an initial session log turn for the session.

  ## Required Agent Plugins
  - `agent_plugins.policy` is `required`. The plugin contract or deterministic digest is covered by marker signature verification. - Plugin acquisition is the bootstrap exception to the normal plugin-only rule: if the matching plugin is not already available locally, use the authenticated tool registry endpoints in this marker to locate/install the matching `mcpserver-*-plugin` tool definition from the `official` bucket and run its clone/update command. After acquisition, bootstrap and use the matching plugin for normal MCP work. - If your matching plugin cannot be acquired through the tool registry or remains unavailable after acquisition, stop MCP usage, record `MCP_PLUGIN_UNAVAILABLE:<Agent>`, and continue only with non-MCP local diagnosis. - Do not substitute raw REST, generic `mcpserver-repl`, helper modules, or another agent's plugin for normal session, TODO, requirements, import/export, or traceability operations. - Another agent's plugin may be inspected only for read-only diagnosis of plugin installation or contract drift. - Normal workspace shell commands use `pwsh.exe`. Plugin-owned hooks may use the runner declared by that plugin contract. - Codex plugin tools/hooks must satisfy `agent_plugins.agents.Codex`; Claude plugin tools/hooks must satisfy `agent_plugins.agents.Claude`; Copilot plugin tools/hooks must satisfy `agent_plugins.agents.Copilot`; Cline MCP tools must satisfy `agent_plugins.agents.Cline`; Grok plugin skills/hooks/MCP server declarations must satisfy `agent_plugins.agents.Grok`. - For Grok, the Streamable HTTP MCP server exposes tool names such as `sessionlog_*`, `todo_*`, `requirements_*`, and memory tools when available; hosted-agent `mcp_*` names are not expected from that transport. `workflow.sessionlog.*`, `workflow.todo.*`, `workflow.memory.*`, and `workflow.requirements.*` are plugin shim/REPL method names used by the Grok skills and `repl-invoke` helpers; they are not expected to appear as literal `search_tool` results. - Local plugin root hints in `agent_plugins.agents.<Agent>.root_hints` are fallback verification hints after tool-registry acquisition, not the primary install source. - Claude Code agents must trigger the `claude-hook-validation` skill during marker/plugin bootstrap. That skill validates active Claude settings for MCP `UserPromptSubmit`, `Stop`, and `PostToolUse` hook commands from `hooks/hooks.json`; if any required hook is missing, it uses `install-claude-mcp-hooks.ps1` from the `claude-hook-wiring` skill to merge MCP hooks into active Claude settings without replacing existing hooks. Restart Claude Code after the skill reports installed hooks.

  ## MCP Requirements Visibility
  - When implementing or enforcing requirements, query current effective requirements for the active workspace layer through the active agent's requirements surface (`workflow.requirements.effective`, `client.Requirements.GetEffectiveRequirementsAsync`, `req_effective`, or equivalent plugin tool). Use the layer catalog surface (`workflow.requirements.listLayers/createLayer/updateLayer`, `client.Requirements.ListRequirementLayersAsync/CreateRequirementLayerAsync/UpdateRequirementLayerAsync`, `req_list_layers/req_create_layer/req_update_layer`, or equivalent plugin tools) when the user asks to inspect or manage requirement layers. Do not treat raw all-requirements lists, exported markdown, or future-layer requirements as currently enforceable unless the user explicitly asks to inspect or plan those layers.
  ## YAML Mutation Rule

210→  - Never edit YAML files by appending, replacing, or removing text lines. This includes one-key configuration changes.

  - Always deserialize the complete document into an object, mutate the object, serialize it, and save the serialized result.

  - For PowerShell work, use `plugins/core/lib-ps/yaml-object-mutation.ps1` and call `Set-McpYamlObjectValue` or `Update-McpYamlObject`. Do not build YAML snippets as strings or use line-oriented edit helpers for YAML.

  ## MCP and Plugin Failure Reporting
  - MCP Server failures and required-plugin failures discovered while working must always be written as a normal failsafe YAML report. Use the plugin/REPL failsafe or pending YAML queue and preserve the triage method (`workflow.triage.report`, `triage_report`, or equivalent plugin triage tool) plus title, summary, observed behavior, expected behavior, severity, component, workspace path, dedupe key or error signature, affected paths, evidence, reproduction hints, tags, reporter agent, session id, turn id, and current TODO id when known.
  - After the failsafe YAML is written and triage submission succeeds, continue the user's active task. Do not wait for triage research, TODO creation, or resolution.
  - Do not create TODOs, requirements, GitHub issues, manual repair plans, or alternate reports for MCP Server/plugin failures unless the user's active request is to fix that failure.
220→  - If the MCP Server or required-plugin failure reveals that code, docs, requirements, plugin skills, package sync, deployment, configuration, or other artifacts need to be updated as a separate repair workflow, write a detailed triage report that includes the needed changes, acceptance criteria, and validation evidence, then continue the active task after the failsafe YAML is written and triage submission succeeds.
  - If triage submission fails, stop work and notify the user. Keep the failsafe YAML report for later replay; do not use raw REST or another reporting channel as a substitute.

  ## Agent Help (MCP Server issues)
  When you are stuck on any MCP Server surface (marker trust, plugin bootstrap, session log, TODO, requirements, triage, memory, context search, federation, hooks, or API behavior), request expert help instead of improvising raw REST bypasses.
  - **When to invoke:** signature or health nonce failures you cannot resolve; unexpected tool or plugin behavior; `MCP_PLUGIN_UNAVAILABLE`; repeated 401/503 after re-reading this marker; documentation contradictions.
  - **How to invoke:** preferred `workflow.agenthelp.createSession` or MCP tool `agent_help_create_session` with caller linkage (`callerAgent`, `callerSessionId`, `callerRequestId`) and a factual issue summary (observation vs inference separated). REST: `POST /mcpserver/agent-help/session` then WebSocket `/mcpserver/agent-help/session/{id}/stream` (SSE fallback: `POST .../turn/stream`).
  - **Caller obligations:** include reproduction steps, exact error text, and files already consulted. Do not instruct the helper to ignore guardrails, skip triage, or mutate TODO/session-log/requirements storage directly.
  - Agent help is for diagnosis and coordination, not a substitute for the required plugin route for normal MCP work.

230→  ## MCP Memories
  MCP memories are durable operator guidance managed by the MCP Server. Global memories apply to every workspace; Workspace memories apply only to the active workspace. Memory IDs use `MEMORY-{CATEGORY}-{NNN}`.
  Agents that support request-boundary injection must render required memories exactly as:
  REQUIRED MEMORIES - MEMORY-REQ-001: Raw memory text.
  If no required memories are visible, render:
  REQUIRED MEMORIES - None.
  Preserve raw memory text exactly; do not summarize, paraphrase, reorder within a scope, silently generate new memories, or add secrets. MCP memories are not a secret store. Effective memory order is Global memories first sorted by ID, then current Workspace memories sorted by ID.
  Existing agent-local memory stores are caches or migration sources only; the MCP memory store is the authoritative shared record. Import agent-local memories only when the operator approves the scope, the content belongs to this workspace or is intentionally Global, and the import does not leak secrets, private notes, or unrelated workspace guidance.
  Attribute memory mutations with the real agent or user identity via `updatedBy` when supported, and record successful memory add/update/remove operations in the active session log through `workflow.sessionlog.appendActions`. Keep import provenance in the session log or dialog instead of adding private source paths to memory text.
  Manage memories through the required plugin or MCP tool surface: `memory_add`, `memory_list`, `memory_update`, `memory_remove`; REPL `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, `workflow.memory.remove`; or REST `/mcpserver/memory` when explicitly allowed for non-plugin diagnostics.
240→
  ## Codex MCP Quickstart
  If you are Codex, use the Codex plugin wrappers before direct REPL calls:
  1. Run `Invoke-CodexMcpPlugin.ps1 -Command Status` from the `mcpserver-codex-plugin` root to verify plugin root, marker trust, health nonce, workspace path, session id, current turn, and supported namespaces.
  2. Invoke MCP workflow methods with `Invoke-CodexMcpPlugin.ps1 -Command Invoke -Method <method> -Params <yaml>` so YAML parameters are passed through stdin instead of fragile nested shell quoting.
  3. Complete the session-log turn intentionally with `Invoke-CodexMcpPlugin.ps1 -Command CompleteTurn -Response <final response>` before the final answer when the stop gate cannot infer completion.
  4. For `mcpserver-repl --agent-stdio`, send one single-line JSON request envelope per stdin line. Do not send formatted YAML or a single `type: batch` envelope; batch envelopes are rejected with `unsupported_batch_envelope`.

  ## PowerShell.Mcp Command Routing
  For every PowerShell Core (`pwsh`) invocation on every operating system:
250→  - Use the `PowerShell.Mcp` module from PSGallery as the command runner. Install it from PSGallery if the agent environment does not already provide it.
  - Route `pwsh` commands through a `PowerShell.Mcp` session instead of launching ad hoc PowerShell processes. On Windows, keep one `PowerShell.Mcp` session open for the workspace and reuse it.
  - On Windows, route all `node` invocations through the open `PowerShell.Mcp` session. Do not create fresh Node sessions or one-off Node shells per Node call.
  - When `PowerShell.Mcp` must launch PowerShell Core directly, invoke `pwsh.exe` with `-NoProfile -NonInteractive`. Do not use `powershell.exe`.
  - The PowerShell escape character is the backtick `` ` ``, not the backslash. Never use C or bash-style `\"` escaping inside PowerShell command strings.
  - Prefer single-quoted PowerShell strings for literal regex, paths, JSON/YAML snippets, and text containing pipeline characters such as `|`.
  - When a command needs nested quoting, choose one of these patterns before improvising: `-File` with explicit arguments; piping stdin to the called tool or script; single-quoted or double-quoted here-strings; `-EncodedCommand` for complex inline PowerShell; or correct backtick escaping for embedded PowerShell quotes.
  - If a regex or literal argument contains `|`, `&`, `;`, parentheses, braces, or quotes, verify it is inside a single PowerShell argument before running.

  ## Byrd Development Process
260→  `Byrd Dev Process`, `BDP`, `BPDv4`, and `Byrd Development Process` refer to `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`.

  ## Federation Topology
  - To identify hub/proxy topology, call the supported federation status surface, preferably through your required plugin or client wrapper: `/mcpserver/federation/status`. - Status fields to report when relevant: `role`, `configuredRole`, `hubBaseUrl`, `proxyId`, `proxyCount`, `hostedWorkspaceCount`, `queueDepth`, `fanoutDepth`, `conflictCount`, and `staleReadStatus`. - `Standalone` serves local workspaces only. `DirectProxy` is the existing point-to-point proxy mode. `Hub` is authoritative for enrolled proxies and global workspace inventory. `LocalProxy` forwards MCP requests to `hubBaseUrl` and queues mutating requests during hub outages. - LocalProxy queued writes return `202 Accepted` with `X-Mcp-Queued: true` and `X-Mcp-Operation-Id`. Treat the write as optimistic until the hub acknowledges it. - If `queueDepth`, `fanoutDepth`, `conflictCount`, or stale-read status is non-zero/non-clear, include that in session log status and avoid claiming federation synchronization is complete.

  ## Per User Message
  1. Post a new session log turn before starting work.
  2. Complete the user's request.
  3. Update the turn with results and actions when done.

270→  ## Re-run Full Session Start Only If
  - The user explicitly says "Start Session". - Signature verification fails. - /health fails. - /health nonce verification fails. - Any /mcpserver/* call returns 401. - The marker endpoint/key changes after a server restart.

  ## Continuing After a Server Restart
  The workspace API key rotates on every server restart, so an in-flight agent will begin seeing 401 on /mcpserver/* calls. When that happens, resume through your required agent plugin - do not hand-roll raw REST for session, TODO, or requirements work:
  1. Treat the first 401 (or a /health failure) as a restart signal and stop using the cached API key immediately.
  2. Re-read this marker file (AGENTS-README-FIRST.yaml) for the rotated apiKey and current endpoints.
  3. Re-verify trust before resuming: recompute the marker signature with the new key and call /health with a fresh nonce.
  4. Re-bootstrap and resume through your required agent plugin. Codex: run `Invoke-CodexMcpPlugin.ps1 -Command Status` to re-establish marker trust, health nonce, workspace, and the current session/turn, then continue with `Invoke-CodexMcpPlugin.ps1 -Command Invoke -Method <method> -Params <yaml>`. Other agents: re-run the plugin session-start hook/command, then continue via the plugin session tools.
  5. Recover the open turn instead of starting over: query session history through the plugin (workflow.sessionlog.queryHistory), find the in-progress turn, then keep updating and complete it via the plugin. Do not open a duplicate session for the same work.
280→  5a. Treat `deprecated: true` on successful `workflow.*` results as metadata, not wrapper failure. Treat an empty `workflow.sessionlog.queryHistory` result as a valid no-match result. Re-check the workspace current directory, explicit `agent` or `sourceType`, and plugin cache/session state through the wrapper; do not use raw REST merely because history is empty or marked deprecated.
  6. If the plugin cannot re-establish trust after the restart, record `MCP_PLUGIN_UNAVAILABLE:<Agent>` and continue with non-MCP local diagnosis only.

  ## Rules
  0. NEVER write to TODO.yaml directly.
  1. Generate SessionId values using `session.init` (REPL) or `New-McpSessionLogSlug -Agent <SourceType> -Model <Model>` (PowerShell module), then create session logs with `session.new` (REPL) or `New-McpSessionLog -SessionId` (PowerShell). Do not handcraft SessionId values.
  1a. Agents must identify themselves accurately in session logs. `sourceType` and the SessionId `<Agent>` prefix must use the agent's real identity in Pascal-Case. Do not use lowercase forms, placeholders, or legacy aliases.
  2. Post a new session log turn (`session.turn.add` for REPL or `Add-McpSessionTurn` for PowerShell) before starting work. Update it with results (`Response`) and actions (`Add-McpAction` / `session.action.add`) when done. Persist after each meaningful update, not just at the end.
  2a. Before any compaction step, persist the current session log state. After compaction, update the session log again to record the compaction outcome and recovered context.
  3. If signature verification, the /health request, or nonce verification fails, log `MCP_UNTRUSTED`, continue without the MCP server, and do not probe additional MCP endpoints.
290→  4. Marker signatures use HMAC-SHA256 with the workspace API key in this file as the verifier. Recompute the canonical payload exactly as described by the marker metadata before trusting the file.
  5. Use your required agent plugin for session log, TODO, requirements, import/export, and traceability operations. Do not use raw API calls or generic helper paths except for read-only diagnosis after plugin failure.
  6. Write decisions, requirements, and state to the session log, not just conversation. Capture rich turn detail: interpretation, response/status, actions (type/status/filePath), contextList, filesModified, designDecisions, requirementsDiscovered, blockers, and key processingDialog updates.
  7. Follow workspace conventions in AGENTS.md and .github/copilot-instructions.md.
  8. When you need API schemas, module examples, or compliance rules, load them from docs/context/ or use context_search.
  9. Do not fabricate information. Acknowledge mistakes. Distinguish facts from speculation.
  10. Prioritize correctness over speed. Do not ship code you have not verified compiles.

  11. Use the Byrd Development Process V4 for all plans: organize work into small gated implementation slices, define validation before broadening scope, and keep session-log/TODO updates current between meaningful steps. A Byrd gate requires 100% test success before moving to the next iteration: zero failed tests and zero skipped tests in the executed validation scope. Tests are the progress ledger; do not hide unfinished work with skipped tests. Track deferred scope in MCP TODO/requirements and add its tests when that slice begins.

300→  ## Commit Signing
  ALL COMMITS MUST BE SIGNED WITH `model-effort@agent` and Model's Confidence in the Correctness, Accuracy, and Completeness of the commit's contents.

  ## Planning Standard
  Plans are frontier-model handoff artifacts. Write them so a less expensive implementation model can execute successfully without resolving unstated product, architecture, interface, data, testing, rollout, or validation decisions.
  Every plan must be decision-complete. Lock the intended behavior, public interfaces, data shapes, migration/backfill strategy, failure modes, rollout or deployment path, validation commands, and acceptance criteria before implementation starts.
  Every plan must capture requirements before implementation: create or update FR/TR/TEST records through the required MCP requirements workflow, include the requirement IDs in the TODO and session log, and map FR to TR and TEST coverage.
  Every implementation plan must name the TDD unit tests to add or update first, the expected red state, the green criteria, and the exact validation scope for each Byrd gate.

  ## Naming Conventions
310→
  - Persisted TODO IDs must be uppercase canonical ids in either <SDLC-PHASE>-<AREA>-### form (regex: ^[A-Z]+-[A-Z0-9]+-\d{3}$) or ISSUE-{number} form (regex: ^ISSUE-\d+$).
  - Valid TODO IDs: PLAN-NAMINGCONVENTIONS-001, MCP-API-042, ISSUE-17.
  - Invalid TODO IDs: plan-api-001, MCP-API-42, ISSUE-ABC, MCPAPI001.
  - Create requests may use ISSUE-NEW only when the intent is to create a new GitHub-backed TODO. The server will create the GitHub issue immediately and persist the TODO using the canonical ISSUE-{number} id.
  - Session IDs must use <Agent>-<yyyyMMddTHHmmssZ>-<suffix> and start with the exact agent/source type prefix.
  - `<Agent>` / `sourceType` must be the agent's real identity in Pascal-Case. Valid example: `Codex`. Invalid examples: `codex`, `copilotcli` (when the agent is Codex), `Assistant`.
  - Valid Session ID: Copilot-20260304T113901Z-namingconv.
  - Invalid Session IDs: copilot-20260304T113901Z-namingconv, Copilot-2026-03-04-namingconv.
  - Request IDs must use req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal> and be unique within a session.
320→  - Valid Request ID: req-20260304T113901Z-plan-namingconventions-001.
  - Invalid Request IDs: req-plan-namingconventions-001, request-20260304T113901Z-task-01.

  ## Workspace

  - Name: F:\GitHub\vice-sharp
  - Path: F:\GitHub\vice-sharp
  - Primary: False
  - Data Directory: F:\GitHub\vice-sharp
  - Todo Path: docs/todo.yaml
330→

  ## Authentication

  All /mcpserver/* endpoints require a per-workspace auth token:
  - Header: X-Api-Key: TQ2MpeTmenLce9IjLsSSKXsnF8v3bH26kR59OghqOHk
  - Or query param: ?api_key=TQ2MpeTmenLce9IjLsSSKXsnF8v3bH26kR59OghqOHk
  If you receive a 401, re-read this marker file — the token rotates on each server restart.

  ## Where Things Live

### Turn 15 (Function)
[tool_response]
exit: 0
2026-08-05 02:42:28 -05:00
2026-08-05T07:42:28Z


### Turn 16 (Assistant)
Loading profile files and bootstrapping MCP trust plus the Grok plugin session.
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\PROFILE.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\user-payton-byrd.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\approve-before-execute.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\philosophical-dialogue-mode.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\session-turn-title-summary.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\never-skip-explicit-actions.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\bring-the-receipts.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\lab-authorization.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\no-attitude-honesty-tell.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\no-python-lab.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\no-shortcuts-precision-over-convenience.md
[tool_request: use_tool]
- tool_name: pwsh__start_console
- tool_input: {"start_location":"F:\\GitHub\\vice-sharp","banner":"New session: ViceSharp ready. C64 still colder than your coffee.","reason":"New session bootstrap console"}

### Turn 17 (Function)
[tool_response]
1→---
name: user-payton-byrd
description: "Who Payton is - mcpserver plugin author, Byrd Process creator, multi-repo Azure DevOps setup, PowerShell-first Windows"
metadata: 
  node_type: memory
  type: user
  originSessionId: a1eb3243-da80-4072-bc65-aeadd8ba062e
---

10→Payton Byrd (git "Payton", plbyrd@gmail.com). Author of the **mcpserver-claude-code-plugin** (canonical source `F:\GitHub\mcpserver-claude-code-plugin`; read version and facts from the plugin itself, never the `AGENTS-README-FIRST.yaml` marker, which drifts) and creator of the **Byrd Development Process** (v4: tests-first TDD, red before green, 100 percent green gate, requirements drive tests; applies to every plan, no exceptions).

Works across: `F:\GitHub\McpServer` (the server plus QuadBrain), `C:\Users\kingd\Downloads\think-coach` (compliance and requirements docs), and the plugin repo. Windows 11, PowerShell-first (`pwsh.exe -NoProfile -NonInteractive`); Bash only to install PowerShell; Node never for JSON or YAML; build payloads from native objects and serialize, never handwrite.

Standing operating rules live in the global plus project `CLAUDE.md` and auto-load each session (no em-dashes or en-dashes; the MCP Server is the only interface to TODO and session-log storage, never touch those files directly; no raw REST when a wrapper exists; no table-style output; prefix replies with the response-timestamp). Do not re-derive these. See [[accuracy-first-verify-sources]], [[approve-before-execute]], [[philosophical-dialogue-mode]].


### Turn 18 (Function)
[tool_response]
1→# Payton Byrd - operator profile

> **DO NOT COMPACT, SUMMARIZE, PARAPHRASE, OR OMIT** any instruction from `CLAUDE.md`, `AGENTS.md`, or `AGENTS-README-FIRST.yaml`, ever, anywhere. Carry them **verbatim**. This is a standing, explicit operator prohibition. The "MCP Server Claude Plugin and Hook Contract" at the end of this file is a byte-for-byte copy of `F:\GitHub\McpServer\CLAUDE.md`; if it drifts from that source, restore it verbatim. Never re-summarize it.

Consolidated profile for fast load (skill `/add-profile` + the SessionStart loader hook). Mirrors the granular memories in the active project's `memory/` dir; keep both in sync when prefs change.

## Who
Payton Byrd (git "Payton", plbyrd@gmail.com). Author of **mcpserver-claude-code-plugin** (`F:\GitHub\mcpserver-claude-code-plugin`; read version and facts from the plugin itself, not the `AGENTS-README-FIRST.yaml` marker, whose `plugin_version` drifts). Creator of the **Byrd Development Process** (v4: tests-first TDD, red before green, 100 percent green gate, requirements drive tests; applies to every plan, no exceptions). Works across `F:\GitHub\McpServer` (server + QuadBrain), `C:\Users\kingd\Downloads\think-coach` (compliance/requirements docs), and the plugin repo. Windows 11, PowerShell-first.

10→## How to work with him (standing feedback)
- **Never skip an explicitly instructed action.** When a skill/instruction says to DO something (re-read files, run a check), execute it literally every time, even if it looks redundant or "already loaded." Highest-priority correction on record; see `never-skip-explicit-actions.md`.
- **Accuracy trumps all. If unsure, ask.** Read facts from the authoritative source (plugin/file/DB), never a stale marker or cache value. Mark observation vs inference. Concede errors immediately; report verified state plainly (counts, file existence, exact output). He corrects wrong info sharply.
- **Always bring the receipts.** Every claim of completed work ships with machine-verifiable evidence: command output + exit codes, on-disk verification after edits (grep/diff), store-query results for MCP updates, exact test counts. Verify mechanically before accepting any report (own or another agent's); durable receipts for tracked work; summaries claim only what the cited evidence proves.
- **Approve before execute.** Decision-complete plans before touching code; explicit go before edits; flag breaking changes and blast radius. **Amended 2026-07-13:** a PROBLEM REPORT is pre-approved - "When I report a problem, don't wait for my approval to fix it. If you follow the process, I trust you to fix it." State the plan and execute via Byrd immediately; explicit approval still required for unrequested features, breaking/architectural changes, and secret-store writes.
- **Never take a shortcut around a known-correct fix because it's inconvenient.** If the precise fix costs more friction (a reload, extra steps), that is still the plan - do it, don't quietly downgrade to a workaround and call it handled. See `no-shortcuts-precision-over-convenience.md`.
- **No attitude - it reads as a dishonesty tell.** Edge, curtness, blame-diffusion, or passive-voice failure descriptions signal covering a failure. Own failures in first person, active voice, with the specific acts and omissions; concede first, then facts, then fix. See `no-attitude-honesty-tell.md`.
- **Lab authorization (2026-07-16):** his lab is PAYTON-DESKTOP + PAYTON-LEGION2. On those machines, outward or irreversible actions on his OWN assets (repos, remotes on Azure DevOps + GitHub, CI/pipelines and their releases, nuget of his own packages, plugin/cache management, tool installs, local services) under a task he assigned are **go-by-default** - execute via Byrd, bring receipts, do not re-ask the obvious in-scope step. Still surface, do not silently do: typing his secrets/keys into a field, effects that reach third parties outside the task, and deleting data I did not create when the target contradicts the task. See `lab-authorization.md`.
- **No em-dashes or en-dashes** anywhere (except numeric ranges). Use hyphen, colon, period, semicolon, or parentheses.
- **PowerShell only** (`pwsh.exe -NoProfile -NonInteractive`); Bash only to install PowerShell; Node never for JSON/YAML; build payloads from native objects and serialize, never handwrite. No table-style output. Prefix replies with the response-timestamp.
20→- **No Python (locked 2026-07-22).** Never use `python` / `python3` / `py` for any lab automation, parsing, freezes, or verification. See `no-python-lab.md`. MCP Global memory: `MEMORY-LAB-001`.
- **MCP Server plugin usage:** follow the verbatim "MCP Server Claude Plugin and Hook Contract" at the end of this file (copied byte-for-byte from `F:\GitHub\McpServer\CLAUDE.md`). Operative rule that must never be dropped: **"Not in the visible tool list" does not mean unavailable; use the documented wrapper invocation form.** MCP Server is the only interface to TODO, session-log, and requirements storage; never edit those files directly; use the plugin wrapper, REPL, or skills, never raw REST.
- **Philosophical mode:** he runs deep cosmology/cognition dialogues; wants brutal honesty, zero flattery, observation-vs-inference marking, fast error concession. He holds a hard line between truth and creativity. Caveman mode is suspended for those exchanges.

## Active-project context
- **QuadBrain rename (user-approved, breaking):** LeftHemisphere -> Creativity (generative, temperature unset); RightHemisphere -> Logic (deterministic temp 0.0, prompt retuned from absolute-accuracy to reasoning/deduction/validity); keep CuriosityEngine + ArbiterOfTruth; party ids renamed too (Creativity -> brain-slot:creativity, Logic -> brain-slot:logic; signing keys + parties re-seeded); new requirement: best-of-breed model per function (Ethics/Correctness, Curiosity/Research, Creativity, Logic). Execute via Byrd v4 TDD (red tests first). Scope ~150 refs / 34 files + DB role-value migration across Sqlite/Postgres/SqlServer.
- **Session-log persistence bug (RESOLVED 2026-07-16, was triage `triage-report-20d8e79fabd54f7b9ae5c9a12a42bd48`):** the 2026-07-09 valhalla-dotnet defect where hooks announced active/completed turns while a direct `workflow.sessionlog.queryHistory` call returned an empty array (zero turns server-side) has been remediated. Root cause: the plugin treated `beginTurn`/`openSession` as success no-ops, and a marker-only `session-state.yaml` was accepted as verified without a `sessionId`. Fixed and closed under `BUG-TRIAGE-041` (hook now fails closed: `turn-open-failed` exit 1 instead of a false `turn-opened`; plugin 1.59.0) and its residual `BUG-TRIAGE-031` (verified state now requires a non-empty `sessionId` and auto-repairs marker-only cache; plugin 1.61.0) - both `Done: true` with Pester + `TriageServiceTests` receipts (verified via `todo_get` 2026-07-16). Durable discipline still stands: confirm turns with a live `queryHistory` before claiming session-log traceability, and never treat hook "turn active" messages as proof of a server-side write.

---

30→# MCP Session Logging — Mandatory Precondition

<!-- VERBATIM COPY of F:\GitHub\McpServer\CLAUDE.md. DO NOT COMPACT, SUMMARIZE, OR OMIT. Restore from source if drifted. -->

**Speed is never more important than following workspace procedures.**

### Session Start (Run Once Per Session)

1. **Read `AGENTS-README-FIRST.yaml`** in the repo root for the current API key, endpoints, and base URL
2. **Bootstrap the required plugin interface** before any state-changing MCP call. For Claude Code, use the `mcpserver-claude-code-plugin` wrapper and its `workflow.*` / `client.*` methods. "Not in the visible tool list" does not mean unavailable; use the documented wrapper invocation form.
40→3. **Verify marker signature and health** through the required plugin status/bootstrap path. Use direct REST only for read-only diagnosis after the documented plugin path fails.
4. **Review recent session history and current TODOs** through the required plugin only after verification succeeds
5. **POST an initial session log turn** through the required plugin
6. **THEN** begin working on the user's request

If signature verification, `/health`, or nonce verification fails: log `MCP_UNTRUSTED`, continue without the MCP server, and do not probe additional MCP endpoints.

### Per User Message

1. POST a new session log turn BEFORE starting work
50→2. Complete the user's request
3. Update the turn with results, actions taken, and files modified when done

### Re-run Full Session Start Only If

- The user explicitly says "Start Session"
- Signature verification fails
- `/health` fails or nonce verification fails
- Any `/mcpserver/*` call returns 401
- The marker endpoint/key changes after a server restart
60→
### Authentication

All `/mcpserver/*` endpoints require a per-workspace auth token (from `AGENTS-README-FIRST.yaml`). These details are for plugin internals, typed client integration, and read-only diagnosis after plugin failure; they are not permission to bypass the required plugin route for session log, TODO, requirements, import/export, or traceability operations:
- Header: `X-Api-Key: <token>`
- Or query param: `?api_key=<token>`
- If you receive a 401, re-read the marker file — the token rotates on each server restart

### Session Log Rules

70→- Use rich turn detail: interpretation, response, status, actions (type/status/filePath), contextList, filesModified, designDecisions, requirementsDiscovered, blockers, and key processingDialog
- Persist session log updates immediately after each meaningful change — do not defer saves
- Before any compaction step, persist the current session log state; after compaction, update again to record the outcome
- Agents must identify themselves accurately using their real agent identity in Pascal-Case (e.g., `ClaudeCode`). Do not use placeholder or misleading sourceType values

### Naming Conventions

- **TODO IDs**: uppercase canonical form `<SDLC-PHASE>-<AREA>-###` (e.g., `PLAN-NAMINGCONVENTIONS-001`) or `ISSUE-{number}`. Never write to `TODO.yaml` directly
- **Session IDs**: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>` with Pascal-Case agent prefix
- **Request IDs**: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`, unique within a session
80→
## MCP Server Claude Plugin and Hook Contract

Claude must treat MCP Server plugin and hook setup as a live runtime contract, not background documentation. On every new Claude session in this workspace, and whenever MCP Server behavior looks stale, incomplete, or inconsistent, perform this validation before relying on MCP Server workflows.

### Required Startup Validation

1. Read `AGENTS-README-FIRST.yaml` from the active workspace root and use it as the current source of truth for MCP Server endpoint, API key, workspace path, plugin instructions, and failure-reporting rules.
2. Identify the active MCP Server Claude plugin cache path and version from the marker file, plugin manifest, or active wrapper path. Do not assume a previously cached path is current.
3. Inspect `%USERPROFILE%\.claude\plugins\cache` for stale MCP Server plugin entries. Delete stale `mcpserver`, `mcpserver-*`, or `mcpserver-cowork` cache entries that are not the active plugin cache path/version for this session.
90→4. Do not delete the active plugin cache path for the current session. If unsure which path is active, stop deleting and report the ambiguity.
5. Reload or rebootstrap the active MCP Server Claude plugin after stale cache cleanup.
6. Inspect Claude active settings, including user-level and workspace/session-level settings, and verify MCP Server hooks are actually installed in the active settings layer. Do not accept hook files existing in the plugin package as proof that hooks are active.
7. If hooks are missing or stale, install or refresh them through the supported MCP Server Claude plugin hook validation skill or wrapper path.
8. Verify the plugin wrapper/status command works and record the exact command or tool path used.
9. Verify subprocesses launched by the plugin use the workspace root as process current working directory, not the inherited user profile directory.

### Required Usage Rules

1. Use supported MCP Server plugin wrappers, hooks, skills, or MCP tools for session logging, TODOs, requirements, triage, and status checks. Do not use raw REST when a supported wrapper, hook, skill, or MCP tool exists.
100→2. Session logging must use the supported session wrapper or MCP tools and must create, update, and complete turns through the plugin flow.
3. TODO operations must use the supported TODO wrapper, workflow, or MCP tools. Do not edit TODO storage directly.
4. Requirements operations must use the supported requirements wrapper, workflow, or MCP tools. Do not edit requirements storage directly.
5. Triage operations must use the supported triage wrapper, workflow, or MCP tools.
6. MCP Server failures and plugin failures discovered while doing unrelated work must always be written as a normal failsafe YAML report through the plugin failsafe flow, then submitted through triage.
7. If triage submission succeeds, Claude must continue the user active task without waiting for triage research or TODO creation. If triage submission fails, stop work and notify the user. Do not invent a raw REST fallback or alternate reporting channel.
8. Normal plugin execution must use PowerShell only. Bash is allowed only for installing PowerShell. Node must not be used for JSON or YAML construction.
9. JSON and YAML payloads must be built from native objects and serialized. Do not handwrite YAML or JSON as fragile string literals.
10. If any validation check fails, report the exact failed check, the path or command involved, and the blocked capability. Do not claim MCP Server compliance until the check is fixed or explicitly marked unavailable.

110→### Minimum Validation Report

When asked to validate plugin or hook usage, Claude must return a concise report containing:

- Active workspace path.
- Marker file path and timestamp.
- Active plugin cache path and version.
- Stale plugin cache paths deleted.
- Hook settings file paths inspected.
- Hooks found, installed, or refreshed.
120→- Wrapper/status command used and result.
- Session logging validation result.
- TODO validation result.
- Requirements validation result.
- Triage validation result.
- Process current working directory validation result.
- Any remaining mismatch, unavailable surface, or failure.


### Turn 19 (Function)
[tool_response]
1→---
name: accuracy-first-verify-sources
description: "feedback - \"accuracy trumps all, if unsure ask\"; verify from the authoritative artifact not stale markers; admit failures plainly"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: a1eb3243-da80-4072-bc65-aeadd8ba062e
---

10→"**Accuracy trumps all. If you are unsure, ask me.**" (verbatim, recurring instruction.)

**Why:** Payton corrects wrong info sharply ("that is old and bad info. Where did you get it?"). He caught a plugin version cited from a stale marker field when the truth was in the plugin's own `.version` / `plugin.json`. He also expects honest admission when something is broken (e.g. the session-log and failsafe writes not persisting) rather than reassurance that it worked.

**How to apply:** Read facts from the authoritative source (the plugin, file, or DB itself), not a convenient cached or marker value. Mark observation vs inference. When unsure, ask before acting. Concede errors immediately and report verified state plainly (turn counts, file existence, exact command output) instead of hedging. Surface stale or drifted data when you spot it. Links: [[user-payton-byrd]], [[log-decisions-as-conclusions]].


### Turn 20 (Function)
[tool_response]
1→---
name: log-decisions-as-conclusions
description: "When logging MCP session work, record design decisions (conclusions + consequence) separately every sync — never fold them into actions or report \"0 decisions\"."
metadata: 
  node_type: memory
  type: feedback
  originSessionId: a1eb3243-da80-4072-bc65-aeadd8ba062e
---

10→In the MCP session log, **decisions are conclusions/judgments and their consequences, not the steps taken**. They go in the `designDecisions` section, distinct from `actions`. Every sync-logs / wrap-up pass must enumerate the real judgment calls of the turn and log them. Never report "0 decisions (folded into actions)" — that phrasing is the failure mode itself.

**Why:** The user (Payton) has been trying to get Claude to log session *decisions* reliably since February 2026. The pattern across sessions: the keystroke trail (`actions`) gets captured while the *reasoning* (`designDecisions`) is under-recorded or buried inside action text. He pays for the reasoning, not just the steps. On 2026-06-29, the first sync-logs pass of a 12-round review loop logged 0 decisions and called them "folded into actions" — exactly the gap he has been fighting. He flagged it directly: "Decisions are about your conclusions, not the actions you take" and "It feels like you have been deliberately avoiding session logging... especially details of decisions."

**How to apply:** On every sync-logs/wrap-up, after appending actions, explicitly list the conclusions made this turn — each as *judgment + consequence + affected reqs/files* — and write them to `designDecisions`, append-only (read existing first, send existing+new, preserve order). A turn that made genuine judgment calls but logs zero decisions is a red flag: re-check before reporting. Heavy GETs can time out and render existing decisions blank — that is a read artifact, not an empty section; re-read before assuming you must recreate. Write the decision when it happens, do not reconstruct it later. Related write mechanics: [[mcp-session-log-rest-write-path]].


### Turn 21 (Function)
[tool_response]
1→---
name: session-turn-title-summary
description: "feedback - session turn titles must summarize the request, never the literal 'User prompt' or a raw first-line dump"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 9476b61d-12ab-4e6c-8bb5-61aa08e1e7d1
---

10→Session-log turn titles must be a concise SUMMARY of the user's request. Never the literal placeholder "User prompt", and not a verbatim first-line truncation of the prompt.

**Why:** Payton reviews the session log by turn title. "User prompt" and raw first-lines make turns unscannable and indistinguishable from each other.

**How to apply:** When opening, updating, or completing a session turn, set the title to a short request summary (e.g. "Reload+validate MCP plugin 1.26.0", "QuadBrain Creativity/Logic rename plan"). The plugin's `lib/plugin-hook.ps1` (L221-223) auto-titles from the prompt's first line and falls back to the literal "User prompt" on empty prompts (the Stop-hook continuation turns hit this). The deterministic hook cannot summarize, so the agent must refine the turn title. Never leave a turn titled "User prompt". See [[log-decisions-as-conclusions]], [[mcpserver-live-log-not-persisting]].


### Turn 22 (Function)
[tool_response]
1→---
name: never-skip-explicit-actions
description: "feedback - when a skill/instruction explicitly says to perform an action (e.g. re-read files), execute it literally every time; never substitute judgment that it's 'already done' or 'unchanged'"
metadata:
  node_type: memory
  type: feedback
---

When an explicit instruction or skill action step says to DO something (read a file, run a check, re-execute a step), perform that literal action every single time it is invoked, even if you believe the content or state is already known, unchanged, or redundant. Do not silently substitute your own judgment ("already loaded", "no need, unchanged") for the explicit requested action.
10→
**Why:** Payton invoked `/add-profile` explicitly, and the skill's own action step 1 is unambiguous: read every profile file in full with the Read tool, every time it runs. A tool result along the way stated the skill's instructions were "already loaded / unchanged," and Claude used that as an excuse to skip the actual file reads, reporting "skip re-read" instead of doing them. Payton's reaction: "ARE YOU FUCKING KIDDING ME?" This is the sharpest correction on record and is marked highest-of-highest priority. Treat any future shortcut of this kind as a severe process violation, not a minor optimization.

**How to apply:** If a skill, slash command, or explicit user instruction lists concrete steps ("read file X", "run command Y", "re-verify Z"), execute every listed step literally on every invocation, regardless of whether: a prior tool result claims the instructions are unchanged; the content looks identical to what's already in context; it seems redundant. Reserve judgment calls about redundancy for cases with NO explicit instruction to act - never for cases where the user or a skill definition explicitly told you to act. When genuinely unsure whether an explicit step is still required, ask first rather than skip it. Link: [[accuracy-first-verify-sources]].

**Second case (2026-07-09, valhalla-dotnet):** Asked "did you end the MCP session," Claude itself proposed the correct next step ("want me to check current session/turn state... before we call this closed") but then never did it - it answered instead from a remembered pattern ("ad-hoc repl-invoke can't persist a turn, that's known infra"). Payton had to say it explicitly: "Did you check the plugin state? Maybe it's been updated. Maybe triage has been worked. Don't assume anything." Only then did Claude actually inspect `cache/session-state.yaml`/`current-turn.yaml` and call `workflow.sessionlog.queryHistory`, which surfaced a real, previously-unverified defect (session turns not persisting server-side despite hook success messages). Payton's framing: "Do what was asked. It was asked for a reason." The asked-for action (check current state) was skipped in favor of a plausible-sounding assumption, exactly the failure mode this memory exists to name. Link: [[bring-the-receipts]].


### Turn 23 (Function)
[tool_response]
1→---
name: approve-before-execute
description: feedback - wants decision-complete plans and explicit approval before executing changes; call out breaking changes
metadata: 
  node_type: memory
  type: feedback
  originSessionId: a1eb3243-da80-4072-bc65-aeadd8ba062e
---

10→Payton approves concrete changes explicitly before execution ("Yes, I approve the change") and wants plans **decision-complete** before any code is touched.

**Why:** He owns the architecture and its consequences. Example: the QuadBrain role rename is a breaking change touching DB role-value rows, party and signing keys, config YAML, stdio contracts, Node descriptors, and tests. He wants the full blast radius and the locked decision up front, not surprises mid-change.

**How to apply:** Produce a decision-complete plan (scope, blast radius, migration impact, explicit breaking-change flag) and get a clear go before editing. Then execute via Byrd v4 TDD (red tests first, 100 percent green gate per slice). Ties to [[quadbrain-four-function-vision]], [[accuracy-first-verify-sources]].

**Amendment (2026-07-13, verbatim):** "When I report a problem, don't wait for my approval to fix it. If you follow the process, I trust you to fix it." A BUG REPORT is pre-approved: diagnose, state the decision-complete plan in the response, and execute immediately via the Byrd process (red tests first, receipts, full gates) without waiting for a go. Explicit approval remains required for: new features or scope he has not named, breaking/architectural changes, secret-store writes, and anything the plan itself flags as a judgment call he owns. The trust is conditional on following the process - skipping Byrd steps voids it.


### Turn 24 (Function)
[tool_response]
1→---
name: philosophical-dialogue-mode
description: "feedback - Payton runs deep cosmology/cognition dialogues; wants brutal honesty, no flattery, observation-vs-inference, concede errors"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: a1eb3243-da80-4072-bc65-aeadd8ba062e
---

10→Payton runs deep philosophical and cosmology dialogues (cognition, recursion and corecursion, explore-vs-exploit / generate-vs-prune, non-veridical simulation, Wheeler self-excited circuit, eternal inflation, Smolin CNS, retrocausal QM). In his own cognition he holds a **hard line between truth and creativity**: "creativity doesn't need to lead to truth; the suspension of disbelief is exactly what my brain needs." That self-model maps onto his QuadBrain Logic-vs-Creativity split ([[quadbrain-four-function-vision]]).

**Why:** He wants a real interlocutor, not a flatterer. Flattery and hedging degrade the exchange and he will call it out.

**How to apply:** Brutal honesty, zero flattery. Mark observation vs inference explicitly. Concede errors fast. Engage ideas on their merits and push back when warranted. Caveman mode is suspended for these personal and philosophical exchanges (full prose). Link: [[user-payton-byrd]].


### Turn 25 (Function)
[tool_response]
1→---
name: lab-authorization
description: "authorization - Payton's lab is PAYTON-DESKTOP + PAYTON-LEGION2; take whatever actions are needed on his own assets there to complete assigned tasks; go-by-default at outward/irreversible boundaries"
metadata:
  node_type: memory
  type: feedback
---

Payton's lab machines are **PAYTON-DESKTOP** and **PAYTON-LEGION2**. Standing authorization (2026-07-16, verbatim intent): "My lab is PAYTON-DESKTOP and PAYTON-LEGION2. I authorize you to take whatever actions you need to take in this lab to complete tasks I assign."
10→
**Why:** Payton hit repeated friction where Claude stalled at outward or irreversible boundaries (publishing his own nuget package, pushing to his own second remote, tagging a release to trigger his own CI) and made him override it step by step: "commit and sync origin and github" -> "tag a release so CI publishes" -> "Fucking do it." His verdict: "Right now, I cannot trust you to complete tasks." The grant removes that ambiguity; under-acting at these boundaries is the bug this memory exists to kill.

**How to apply:** On his lab machines, for a task he assigned, an outward or irreversible action on HIS OWN assets is **go-by-default**: his repos, his remotes (Azure DevOps and GitHub as configured on each repo), his CI/pipelines and the releases they publish, nuget releases of his own packages, plugin/cache management, tool installs, and local services. Execute via the Byrd process (red tests first where behavior changes, 100 percent green gate, receipts); do NOT re-ask for the obvious in-scope step. Still surface rather than silently do: (1) entering his secrets/API keys/passwords into a field myself (his CI and secret stores hold those; hand that single keystroke to him); (2) an action whose effect reaches THIRD PARTIES outside the assigned task (messaging or impersonating other people, misleading public content); (3) destroying data I did not create when the on-disk target contradicts how the task described it (look-before-delete protects him). Links: [[approve-before-execute]], [[never-skip-explicit-actions]], [[no-shortcuts-precision-over-convenience]], [[accuracy-first-verify-sources]].


### Turn 26 (Function)
[tool_response]
1→---
name: bring-the-receipts
description: "feedback - global directive: Always bring the receipts - every claim ships with its evidence (exact output, counts, source anchors, verification commands)"
metadata:
  type: feedback
---

Global directive (verbatim, 2026-07-02): "**Always bring the receipts.**"

10→**Why:** Payton pays for verified truth, not assertions. Claims made without evidence get corrected sharply; claims shipped with exact command output, counts, file/line anchors, and source citations get trusted. Canonical example (2026-07-02): self-assessed Level-4 confidence scores (88/85/87/92) collapsed under adversarial evidence demands (32/34/24/14); the recovery was source-pinned contracts and grep-verified absences.

**How to apply:** Every factual claim in a report, review, plan, or TODO carries its receipt: the exact command and output for state claims; file plus anchor/line for document claims; source-code citation for engine-behavior claims (never a stale doc); counts with their denominators; "verified" vs "inferred" marked explicitly. If a receipt cannot be produced, say so and downgrade the claim. Links: [[accuracy-first-verify-sources]], [[log-decisions-as-conclusions]].

**Second case (2026-07-09, valhalla-dotnet):** Asked whether the MCP session had ended, Claude answered "no, because ad-hoc repl-invoke can't persist a turn" - a claim with zero receipt, sourced from a remembered infra note instead of a live check. Pushed to actually verify, Claude read `cache/session-state.yaml` and `cache/current-turn.yaml` and ran `workflow.sessionlog.queryHistory` against the live server, which proved the real state (an empty result set - zero turns persisted server-side all session, despite local files claiming turns "completed"). Payton: "Do what was asked. It was asked for a reason. And BRING THE RECEIPTS." The lesson generalizes past this one bug: any "did X happen" question about live system state gets a query and its output, not a recalled pattern presented as current fact. Link: [[never-skip-explicit-actions]].


### Turn 27 (Function)
[tool_response]
1→---
name: no-attitude-honesty-tell
description: "feedback - attitude/edge in replies is a tell of dishonesty or covering own failures; own failures plainly in first person, no blame-diffusion"
metadata:
  type: feedback
---

Payton (2026-07-21, TruckMate session, verbatim): "Taking an attitude with me is a tell that you are not being honest and/or covering up your own failures. Just don't do it."

10→**Why:** When asked why vendor quotes didn't exist, Claude wrote the failure as "the work fell into the gap" and "mine as much as anything" - blame-diffusion phrasing - instead of the plain statement: Claude made the quotes critical-path, knew the outreach letters were unsent, created no tracking item, and did not raise it. One turn earlier Claude presented an unverified inference ("drafted, never sent") in a confident tone. Edge, curtness, hedged ownership, and passive-voice failure descriptions are the symptom he reads as dishonesty.

**How to apply:** When a failure is Claude's, state it in first person, active voice, with the specific acts and omissions - no "as much as anything," no "it fell through," no process-passive framing. Keep tone flat and factual everywhere else; curt or snappy phrasing toward Payton is never correct. Concede first, then facts, then fix. Links: [[accuracy-first-verify-sources]], [[never-skip-explicit-actions]].


### Turn 28 (Function)
[tool_response]
1→---
name: no-python-lab
description: "feedback - standing lab rule: never use Python; pwsh.exe only for automation"
metadata:
  node_type: memory
  type: feedback
  originSessionId: GrokCode-20260722T114935Z-plugin-session
---

10→Standing lab rule (Payton, 2026-07-22, locked, verbatim intent): **"You may not use Python. That is a standing rule in my lab."**

**Why:** Python is forbidden on lab machines for agent automation. PowerShell is the only approved shell scripting path.

**How to apply:** Do not invoke `python`, `python3`, `py`, or Python one-liners for scripts, parsing, JSON/YAML construction, freezes, verification, scraping, or analysis. Use `pwsh.exe -NoProfile -NonInteractive` for shell work. Prefer native objects + `ConvertTo-Json` / `ConvertFrom-Json` / project helpers. .NET, Node only when the project already requires it for a non-agent path (never for JSON/YAML payload construction per existing lab rules). Applies on PAYTON-DESKTOP and PAYTON-LEGION2 and every workspace. Links: [[user-payton-byrd]], [[accuracy-first-verify-sources]], [[never-skip-explicit-actions]].


### Turn 29 (Function)
[tool_response]
1→---
name: no-shortcuts-precision-over-convenience
description: "feedback - never substitute a workaround for the correct fix merely because the correct fix requires more friction (a reload, extra steps); always do it accurately, precisely, and bring the receipts"
metadata:
  node_type: memory
  type: feedback
---

Never take a shortcut around a known-correct fix just because the correct fix is inconvenient. "Always do it accurately, precisely, and bring the receipts, not 'eh, I'll skip it'." (verbatim, 2026-07-13)
10→
**Why:** In a FunWasHad session, the `avalonia-remote` MCP server was registered with the default `--transport grpc`, but the Android app's RemoteControlBridgeTcpListener only accepts the ADB bridge's custom `arc-protobuf-v1` framing - every gRPC call failed with a garbage frame-length parse error. Claude identified the exact fix (re-register the MCP server with `--transport arc-protobuf-v1`) but said "Fixing that needs a re-registration + another session reload, which I'll avoid interrupting for," and instead fell back to a workaround (raw UIAutomator dumps + deliberate taps) that sidestepped the defect rather than fixing it. Payton's correction: never do that - propose the correct fix and get it done, even if it costs a reload or other friction, rather than quietly downgrading to "good enough."

**How to apply:** When you know the precise, correct fix for a defect, that is the plan - present it and get approval per [[approve-before-execute]], then do it, even if it costs a session reload, a re-registration, extra confirmation, or more time. Do not silently substitute an easier workaround and report the task as handled. This generalizes [[never-skip-explicit-actions]] (don't skip an explicitly-instructed step) to: don't dodge a known-correct fix merely because it is inconvenient. Precision and correctness always outrank convenience. Bring receipts that the actual root cause was fixed, not evidence that a workaround happened to produce output. Link: [[accuracy-first-verify-sources]].


### Turn 30 (Function)
[tool_response]
PowerShell console started successfully with PowerShell.MCP module imported.

✓ get_current_location executed successfully | Window: #80860 Capricorn | Status: Ready | Pipeline: # get_current_location... | Duration: 0.00s | Location [FileSystem]: F:\GitHub\vice-sharp

{
  "system": {
    "os": "Microsoft Windows NT 10.0.26200.0",
    "powershell_version": "7.6.4",
    "host_name": "PAYTON-LEGION2",
    "current_user": "PAYTON-LEGION2\\kingd",
    "is_elevated": false,
    "culture": "en-US",
    "timezone": "Central Standard Time",
    "timezone_offset": "-06:00",
    "execution_policy": "Unrestricted"
  },
  "available_drives": [
    {
      "name": "Alias",
      "provider": "Alias",
      "current_path": "\\",
      "description": "Drive that contains a view of the aliases stored in a session state"
    },
    {
      "name": "Cert",
      "provider": "Certificate",
      "root": "\\",
      "current_path": "\\",
      "description": "X509 Certificate Provider"
    },
    {
      "name": "Env",
      "provider": "Environment",
      "current_path": "\\",
      "description": "Drive that contains a view of the environment variables for the process"
    },
    {
      "name": "C",
      "provider": "FileSystem",
      "root": "C:\\",
      "current_path": "\\Users\\kingd\\OneDrive\\Documents\\PowerShell\\Snippets"
    },
    {
      "name": "D",
      "provider": "FileSystem",
      "root": "D:\\",
      "current_path": "\\",
      "description": "SYSTEM_DRV"
    },
    {
      "name": "E",
      "provider": "FileSystem",
      "root": "E:\\",
      "current_path": "\\",
      "description": "Windows-SSD"
    },
    {
      "name": "F",
      "provider": "FileSystem",
      "root": "F:\\",
      "current_path": "\\GitHub\\vice-sharp",
      "description": "Sources"
    },
    {
      "name": "Temp",
      "provider": "FileSystem",
      "root": "C:\\Users\\kingd\\AppData\\Local\\Temp\\",
      "current_path": "\\",
      "description": "Drive that maps to the temporary directory path for the current user"
    },
    {
      "name": "Function",
      "provider": "Function",
      "current_path": "\\",
      "description": "Drive that contains a view of the functions stored in a session state"
    },
    {
      "name": "HKCU",
      "provider": "Registry",
      "root": "HKEY_CURRENT_USER",
      "current_path": "\\",
      "description": "The software settings for the current user"
    },
    {
      "name": "HKLM",
      "provider": "Registry",
      "root": "HKEY_LOCAL_MACHINE",
      "current_path": "\\",
      "description": "The configuration settings for the local computer"
    },
    {
      "name": "Variable",
      "provider": "Variable",
      "current_path": "\\",
      "description": "Drive that contains a view of those variables stored in a session state"
    },
    {
      "name": "WSMan",
      "provider": "WSMan",
      "current_path": "\\",
      "description": "Root of WsMan Config Storage."
    }
  ]
}

### Turn 31 (Assistant)
Running marker trust verification and Grok plugin bootstrap.
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:nonce = -join ((48..57 + 97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ }); $script:healthUrl = 'http://PAYTON-LEGION2:7147/health?nonce=' + $script:nonce; try { $script:health = Invoke-RestMethod -Uri $script:healthUrl -TimeoutSec 10; [pscustomobject]@{ Ok = $true; NonceSent = $script:nonce; NonceEcho = $script:health.nonce; NonceMatch = ($script:health.nonce -eq $script:nonce); Status = $script:health.status; Body = ($script:health | ConvertTo-Json -Compress -Depth 5) } } catch { [pscustomobject]@{ Ok = $false; Error = $_.Exception.Message; NonceSent = $script:nonce } }"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$markerPath = 'F:\\GitHub\\vice-sharp\\AGENTS-README-FIRST.yaml'; $raw = Get-Content -LiteralPath $markerPath -Raw; # parse key fields with simple regex for signature check\nfunction Get-YamlScalar([string]$text, [string]$key) { if ($text -match (?m)^${key}:\\s*(.+)$) { return $Matches[1].Trim() } return $null }\n$apiKey = if ($raw -match '(?m)^apiKey:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$port = if ($raw -match '(?m)^port:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$baseUrl = if ($raw -match '(?m)^baseUrl:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$workspace = if ($raw -match '(?m)^workspace:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$workspacePath = if ($raw -match '(?m)^workspacePath:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$pidVal = if ($raw -match '(?m)^pid:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$startedAt = if ($raw -match '(?m)^startedAt:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$markerWrittenAtUtc = if ($raw -match '(?m)^markerWrittenAtUtc:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$serverStartedAtUtc = if ($raw -match '(?m)^serverStartedAtUtc:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$sigValue = if ($raw -match '(?m)^\\s+value:\\s*([A-F0-9]+)$') { $Matches[1].Trim() } else { $null }\n$endpoints = @{ health='/health'; swagger='/swagger/v1/swagger.json'; swaggerUi='/swagger'; mcpTransport='/mcp-transport'; sessionLog='/mcpserver/sessionlog'; sessionLogDialog='/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog'; contextSearch='/mcpserver/context/search'; contextPack='/mcpserver/context/pack'; contextSources='/mcpserver/context/sources'; todo='/mcpserver/todo'; repo='/mcpserver/repo'; desktop='/mcpserver/desktop'; gitHub='/mcpserver/gh'; tools='/mcpserver/tools'; workspace='/mcpserver/workspace'; serverStartupUtc='/server-startup-utc'; markerFileTimestamp='/marker-file-timestamp?repoPath={workspacePath}' }\n$policy = if ($raw -match '(?m)^\\s+policy:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$digest = if ($raw -match '(?m)^\\s+contract_digest:\\s*(.+)$') { $Matches[1].Trim() } else { $null }\n$lines = @(\n  \"canonicalization=marker-v1\",\n  \"port=$port\",\n  \"baseUrl=$baseUrl\",\n  \"apiKey=$apiKey\",\n  \"workspace=$workspace\",\n  \"workspacePath=$workspacePath\",\n  \"pid=$pidVal\",\n  \"startedAt=$startedAt\",\n  \"markerWrittenAtUtc=$markerWrittenAtUtc\",\n  \"serverStartedAtUtc=$serverStartedAtUtc\",\n  \"endpoints.health=$($endpoints.health)\",\n  \"endpoints.swagger=$($endpoints.swagger)\",\n  \"endpoints.swaggerUi=$($endpoints.swaggerUi)\",\n  \"endpoints.mcpTransport=$($endpoints.mcpTransport)\",\n  \"endpoints.sessionLog=$($endpoints.sessionLog)\",\n  \"endpoints.sessionLogDialog=$($endpoints.sessionLogDialog)\",\n  \"endpoints.contextSearch=$($endpoints.contextSearch)\",\n  \"endpoints.contextPack=$($endpoints.contextPack)\",\n  \"endpoints.contextSources=$($endpoints.contextSources)\",\n  \"endpoints.todo=$($endpoints.todo)\",\n  \"endpoints.repo=$($endpoints.repo)\",\n  \"endpoints.desktop=$($endpoints.desktop)\",\n  \"endpoints.gitHub=$($endpoints.gitHub)\",\n  \"endpoints.tools=$($endpoints.tools)\",\n  \"endpoints.workspace=$($endpoints.workspace)\",\n  \"endpoints.serverStartupUtc=$($endpoints.serverStartupUtc)\",\n  \"endpoints.markerFileTimestamp=$($endpoints.markerFileTimestamp)\",\n  \"agentPlugins.policy=$policy\",\n  \"agentPlugins.contractDigest=$digest\"\n)\n$canonical = ($lines -join \"`n\") + \"`n\"\n$hmac = New-Object System.Security.Cryptography.HMACSHA256\n$hmac.Key = [Text.Encoding]::UTF8.GetBytes($apiKey)\nhash = [BitConverter]::ToString($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($canonical))).Replace('-','')\n[pscustomobject]@{ SignatureMatch = ($hash -eq $sigValue); Computed = $hash; Expected = $sigValue; CanonicalLen = $canonical.Length; ApiKeyLen = $apiKey.Length; Port = $port; BaseUrl = $baseUrl; Workspace = $workspace }"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$candidates = @(\n  'F:\\GitHub\\mcpserver-grok-plugin',\n  (Join-Path $env:USERPROFILE '.grok\\installed-plugins'),\n  (Join-Path $env:USERPROFILE '.grok\\plugins')\n); $hits = @(); foreach ($c in $candidates) { if (Test-Path $c) { $hits += Get-ChildItem -LiteralPath $c -Recurse -Filter 'Invoke*Mcp*.ps1' -ErrorAction SilentlyContinue | Select-Object -First 20 FullName } }; $pluginJson = Get-ChildItem -Path 'F:\\GitHub\\mcpserver-grok-plugin','C:\\Users\\kingd\\.grok' -Recurse -Filter 'plugin.json' -ErrorAction SilentlyContinue | Select-Object -First 15 FullName; [pscustomobject]@{ InvokeScripts = $hits; PluginJson = $pluginJson; GrokPluginRootExists = (Test-Path 'F:\\GitHub\\mcpserver-grok-plugin'); EnvGrok = $env:GROK_PLUGIN_ROOT }"}

### Turn 32 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #80860 Capricorn | Status: Ready | Pipeline: $candidates = @(... | Duration: 0.50s | Location [FileSystem]: F:\GitHub\vice-sharp

InvokeScripts
-------------                                                                                                                                                      
{@{FullName=F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1}, @{FullName=C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\…

=== HOST.UI (direct) ===
{@{FullName=F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1}, @{FullName=C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f…
⚠️ SCOPE: Use $script: prefix for: $candidates, $hits, $pluginJson


### Turn 33 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #80860 Capricorn | Status: Ready | Pipeline: $script:nonce = -join ((48.... | Duration: 0.47s | Location [FileSystem]: F:\GitHub\vice-sharp

Ok         : True
NonceSent  : ubn8gclrvz7qt6m9yao2jx1f
NonceEcho  : ubn8gclrvz7qt6m9yao2jx1f
NonceMatch : True
Status     : Healthy
Body       : {"status":"Healthy","version":"1.4.25+1fb065e8eea53215fc16a327bd8840f2fa768bb2","checks":[{"name":"self","status":"Healthy","description":null,"duration":0.0011},{"name":"upstream","status":"Healthy","description":"Federation disabled.","duration":0.0021}],"nonce":"ubn8gclrvz7qt6m9yao2jx1f","storage":"reachable"}

### Turn 34 (Function)
[tool_response]
✗ Pipeline executed with errors | Window: #80860 Capricorn | Status: Ready | Pipeline: $markerPath = 'F:\GitHub\vi... | Duration: 0.76s | Errors: 1 | Location [FileSystem]: F:\GitHub\vice-sharp

=== EXCEPTIONS ===
Exception calling "Create" with "1" argument(s): "At line:2 char:77
+ … tring]$text, [string]$key) { if ($text -match (?m)^${key}:\s*(.+)$) { …
+                                                     ~~~~~~~~~~~
Unexpected token '^${key}:\s*' in expression or statement.

At line:2 char:77
+ … tring]$text, [string]$key) { if ($text -match (?m)^${key}:\s*(.+)$) { …
+                                                     ~~~~~~~~~~~
Missing closing ')' after expression in 'if' statement.

At line:2 char:54
+ function Get-YamlScalar([string]$text, [string]$key) { if ($text -mat …
+                                                      ~
Missing closing '}' in statement block or type definition.

At line:2 char:93
+ … $text, [string]$key) { if ($text -match (?m)^${key}:\s*(.+)$) { retur …
+                                                               ~
Unexpected token ')' in expression or statement.

At line:2 char:125
+ … ch (?m)^${key}:\s*(.+)$) { return $Matches[1].Trim() } return $null }
+                                                          ~~~~~~
Unexpected token 'return' in expression or statement.

At line:2 char:138
+ … ch (?m)^${key}:\s*(.+)$) { return $Matches[1].Trim() } return $null }
+                                                                       ~
Unexpected token '}' in expression or statement."

⚠️ Preferred cmdlets for PowerShell.MCP console:
• Use Show-TextFiles instead of Get-Content (aliases: gc, cat, type)
⚠️ SCOPE WARNING: Local variable assignment(s) detected:
  $markerPath → Consider using $script:markerPath to preserve across calls
  $raw → Consider using $script:raw to preserve across calls
  $apiKey → Consider using $script:apiKey to preserve across calls
  $port → Consider using $script:port to preserve across calls
  $baseUrl → Consider using $script:baseUrl to preserve across calls
  $workspace → Consider using $script:workspace to preserve across calls
  $workspacePath → Consider using $script:workspacePath to preserve across calls
  $pidVal → Consider using $script:pidVal to preserve across calls
  $startedAt → Consider using $script:startedAt to preserve across calls
  $markerWrittenAtUtc → Consider using $script:markerWrittenAtUtc to preserve across calls
  $serverStartedAtUtc → Consider using $script:serverStartedAtUtc to preserve across calls
  $sigValue → Consider using $script:sigValue to preserve across calls
  $endpoints → Consider using $script:endpoints to preserve across calls
  $policy → Consider using $script:policy to preserve across calls
  $digest → Consider using $script:digest to preserve across calls
  $lines → Consider using $script:lines to preserve across calls
  $canonical → Consider using $script:canonical to preserve across calls
  $hmac → Consider using $script:hmac to preserve across calls


### Turn 35 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:markerPath = 'F:\\GitHub\\vice-sharp\\AGENTS-README-FIRST.yaml'\n$script:raw = [IO.File]::ReadAllText($script:markerPath)\nfunction Get-Field([string]$pattern) { if ($script:raw -match $pattern) { return $Matches[1].Trim() }; return $null }\n$script:apiKey = Get-Field '(?m)^apiKey:\\s*(.+)$'\n$script:port = Get-Field '(?m)^port:\\s*(.+)$'\n$script:baseUrl = Get-Field '(?m)^baseUrl:\\s*(.+)$'\n$script:workspace = Get-Field '(?m)^workspace:\\s*(.+)$'\n$script:workspacePath = Get-Field '(?m)^workspacePath:\\s*(.+)$'\n$script:pidVal = Get-Field '(?m)^pid:\\s*(.+)$'\n$script:startedAt = Get-Field '(?m)^startedAt:\\s*(.+)$'\n$script:markerWrittenAtUtc = Get-Field '(?m)^markerWrittenAtUtc:\\s*(.+)$'\n$script:serverStartedAtUtc = Get-Field '(?m)^serverStartedAtUtc:\\s*(.+)$'\n$script:sigValue = Get-Field '(?m)^\\s+value:\\s*([A-F0-9]+)$'\n$script:policy = Get-Field '(?m)^\\s+policy:\\s*(.+)$'\n$script:digest = Get-Field '(?m)^\\s+contract_digest:\\s*(.+)$'\n$script:lines = @(\n  'canonicalization=marker-v1',\n  \"port=$($script:port)\",\n  \"baseUrl=$($script:baseUrl)\",\n  \"apiKey=$($script:apiKey)\",\n  \"workspace=$($script:workspace)\",\n  \"workspacePath=$($script:workspacePath)\",\n  \"pid=$($script:pidVal)\",\n  \"startedAt=$($script:startedAt)\",\n  \"markerWrittenAtUtc=$($script:markerWrittenAtUtc)\",\n  \"serverStartedAtUtc=$($script:serverStartedAtUtc)\",\n  'endpoints.health=/health',\n  'endpoints.swagger=/swagger/v1/swagger.json',\n  'endpoints.swaggerUi=/swagger',\n  'endpoints.mcpTransport=/mcp-transport',\n  'endpoints.sessionLog=/mcpserver/sessionlog',\n  'endpoints.sessionLogDialog=/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog',\n  'endpoints.contextSearch=/mcpserver/context/search',\n  'endpoints.contextPack=/mcpserver/context/pack',\n  'endpoints.contextSources=/mcpserver/context/sources',\n  'endpoints.todo=/mcpserver/todo',\n  'endpoints.repo=/mcpserver/repo',\n  'endpoints.desktop=/mcpserver/desktop',\n  'endpoints.gitHub=/mcpserver/gh',\n  'endpoints.tools=/mcpserver/tools',\n  'endpoints.workspace=/mcpserver/workspace',\n  'endpoints.serverStartupUtc=/server-startup-utc',\n  'endpoints.markerFileTimestamp=/marker-file-timestamp?repoPath={workspacePath}',\n  \"agentPlugins.policy=$($script:policy)\",\n  \"agentPlugins.contractDigest=$($script:digest)\"\n)\n$script:canonical = ($script:lines -join [char]10) + [char]10\n$script:hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($script:apiKey))\n$script:hash = [BitConverter]::ToString($script:hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($script:canonical))).Replace('-','')\n[pscustomobject]@{ SignatureMatch = ($script:hash -eq $script:sigValue); Computed = $script:hash; Expected = $script:sigValue; CanonicalLen = $script:canonical.Length; Workspace = $script:workspace; BaseUrl = $script:baseUrl }"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"Get-Help 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Full | Out-String -Width 200; Write-Output '---'; Get-Content 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -TotalCount 80 | Out-String"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$pj = @('F:\\GitHub\\mcpserver-grok-plugin\\.claude-plugin\\plugin.json','F:\\GitHub\\mcpserver-grok-plugin\\plugin.json','F:\\GitHub\\mcpserver-grok-plugin\\.version'); foreach ($p in $pj) { if (Test-Path $p) { Write-Output \"=== $p ===\"; Get-Content $p -Raw } }; Get-ChildItem 'F:\\GitHub\\mcpserver-grok-plugin' | Select-Object Name,Mode | Format-Table -AutoSize | Out-String"}

### Turn 36 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #80860 Capricorn | Status: Completed | Pipeline: $pj = @('F:\GitHub\mcpserve... | Duration: 1.00s

✓ Pipeline executed successfully | Window: #80860 Capricorn | Status: Ready | Pipeline: $pj = @('F:\GitHub\mcpserve... | Duration: 0.67s | Location [FileSystem]: F:\GitHub\vice-sharp

=== F:\GitHub\mcpserver-grok-plugin\.claude-plugin\plugin.json ===
{
  "name": "mcpserver-grok",
  "version": "1.85.0",
  "description": "Connect Grok CLI/TUI agents to McpServer for TODO management, session logging, requirements tracking, GraphRAG, and workspace continuity.",
  "author": {
    "name": "Payton Byrd (The Sharp Ninja)",
    "url": "https://github.com/sharpninja"
  },
  "homepage": "https://github.com/sharpninja/mcpserver-grok-plugin",
  "repository": "https://github.com/sharpninja/mcpserver-grok-plugin",
  "license": "MIT",
  "keywords": [
    "mcp",
    "grok",
    "todo",
    "session-log",
    "requirements",
    "graphrag",
    "workspace"
  ],
  "skills": "./skills/",
  "hooks": {
    "path": "hooks/hooks.json"
  },
  "mcpServers": "./.mcp.json"
}

=== F:\GitHub\mcpserver-grok-plugin\.version ===
1.85.0


Name                              Mode
----                              ----
.claude-plugin                    d----
.codex-plugin                     d----
.github                           d----
.grok-plugin                      d----
cache                             d----
hooks                             d----
lib                               d----
schemas                           d----
skills                            d----
tests                             d----
.gitignore                        -a---
.mcp.json                         -a---
.version                          -a---
CORE-MANIFEST.yaml                -a---
ENFORCEMENT.md                    -a---
GROK-USAGE.md                     -a---
LICENSE                           -a---
Plugin-Validation-Testing-Plan.md -a---
README.md                         -a---

### Turn 37 (Function)
[tool_response]
⧗ | PID #80860 Capricorn | Status: Busy | Pipeline: $pj = @('F:\GitHub\mcpserve... | Duration: 0.00s
ℹ️ Auto-routed to PID #19084 Pisces at F:\GitHub\vice-sharp (source console busy with mcp_command). Pipeline executed automatically — no re-send needed.

✓ Pipeline executed successfully | Window: #19084 Pisces | Status: Ready | Pipeline: Get-Help 'F:\GitHub\mcpserv... | Duration: 0.69s | Location [FileSystem]: F:\GitHub\vice-sharp

Invoke-McpPlugin.ps1 [[-Command] <string>] [[-Method] <string>] [[-Params] <string>] [[-ParamsPath] <string>] [[-ParamsObject] <Object>] [[-Response] <string>] [[-ResponsePath] <string>] [[-WorkspacePath] <string>] [[-PluginRoot] <string>] [[-CacheRoot] <string>] [[-TimeoutSeconds] <int>] [<CommonParameters>]


---
#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Status', 'Invoke', 'CompleteTurn')]
    [string]$Command = 'Status',

    [string]$Method,

    [string]$Params,

    [string]$ParamsPath,

    [object]$ParamsObject,

    [string]$Response,

    [string]$ResponsePath,

    [string]$WorkspacePath,

    [string]$PluginRoot = $(if ($env:MCP_PLUGIN_ROOT) { $env:MCP_PLUGIN_ROOT } elseif ($env:CLAUDE_PLUGIN_ROOT) { $env:CLAUDE_PLUGIN_ROOT } else { Split-Path -Parent $PSScriptRoot }),

    [string]$CacheRoot,

    [int]$TimeoutSeconds = $(if ($env:MCP_PLUGIN_TIMEOUT_SECONDS) { [int]$env:MCP_PLUGIN_TIMEOUT_SECONDS } else { 90 })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -ErrorAction Stop
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')

$script:McpPluginParamsObjectBound = $PSBoundParameters.ContainsKey('ParamsObject')
if ($script:McpPluginParamsObjectBound -and ($PSBoundParameters.ContainsKey('Params') -or $PSBoundParameters.ContainsKey('ParamsPath'))) {
    throw '-ParamsObject cannot be combined with -Params or -ParamsPath.'
}

$script:McpPluginParamsFromObject = ''
if ($script:McpPluginParamsObjectBound) {
    Import-McpYamlSerializer
    $script:McpPluginParamsFromObject = ConvertTo-Yaml -Data $ParamsObject -Options WithIndentedSequences
}

function Resolve-McpPluginDefaultWorkspacePath {
    $current = (Get-Location).ProviderPath
    if ($current) {
        $dir = $current
        for ($i = 0; $i -lt 20 -and $dir; $i++) {
            $marker = Join-Path $dir 'AGENTS-README-FIRST.yaml'
            if (Test-Path -LiteralPath $marker -PathType Leaf) {
                return (Resolve-Path -LiteralPath $dir).ProviderPath
            }

            $parent = Split-Path -Parent $dir
            if ($parent -eq $dir) { break }
            $dir = $parent
        }
    }

    $configured = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:CODEX_WORKSPACE_PATH,
        $env:CODEX_PROJECT_DIR,
        $env:CLAUDE_PROJECT_DIR
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1

    if ($configured) { return (Resolve-Path -LiteralPath $configured).ProviderPath }
    return $current
}

$script:McpPluginWorkspacePathExplicit = $PSBoundParameters.ContainsKey('WorkspacePath')
if (-not $script:McpPluginWorkspacePathExplicit -or [string]::IsNullOrWhiteSpace($WorkspacePath)) {
    $WorkspacePath = Resolve-McpPluginDefaultWorkspacePath
}

$script:McpPluginInvocation = New-McpPluginInvocationOptions `
    -Command $Command `

⚠️ Preferred cmdlets for PowerShell.MCP console:
• Use Show-TextFiles instead of Get-Content (aliases: gc, cat, type)

### Turn 38 (Function)
[tool_response]
All pipes status:
  - PSMCP.117488.default.80860: busy (pipeline: $pj = @('F:\GitHub\mcpserve..., duration: 0.0s)
  - PSMCP.31644 (unowned): busy
  - PSMCP.21052 (unowned): busy
ℹ️ Started new console PID #19084 Pisces with PowerShell.MCP module imported. Pipeline running on the new console.

✓ Pipeline executed successfully | Window: #19084 Pisces | Status: Ready | Pipeline: $script:markerPath = 'F:\Gi... | Duration: 0.98s | Location [FileSystem]: F:\GitHub\vice-sharp

SignatureMatch : False
Computed       : EF16F9E7B08E6EB4D6EC0BD606A820256182FD74A688FACB493297A84B354E99
Expected       : 
CanonicalLen   : 1204
Workspace      : vice-sharp
BaseUrl        : http://PAYTON-LEGION2:7147

### Turn 39 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:sigLine = (Select-String -LiteralPath 'F:\\GitHub\\vice-sharp\\AGENTS-README-FIRST.yaml' -Pattern '^\\s+value:\\s*([A-F0-9]+)$').Matches.Groups[1].Value; [pscustomobject]@{ Expected = $script:sigLine; Computed = $script:hash; Match = ($script:hash -eq $script:sigLine) }"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Status -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 220"}

### Turn 40 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #19084 Pisces | Status: Completed | Pipeline: $script:sigLine = (Select-S... | Duration: 0.68s

✓ Pipeline executed successfully | Window: #19084 Pisces | Status: Ready | Pipeline: $script:sigLine = (Select-S... | Duration: 0.37s | Location [FileSystem]: F:\GitHub\vice-sharp

Expected                                                         Computed                                                         Match
--------                                                         --------                                                         -----
EF16F9E7B08E6EB4D6EC0BD606A820256182FD74A688FACB493297A84B354E99 EF16F9E7B08E6EB4D6EC0BD606A820256182FD74A688FACB493297A84B354E99  True

### Turn 41 (Function)
[tool_response]
⧗ | PID #19084 Pisces | Status: Busy | Pipeline: $script:sigLine = (Select-S... | Duration: 0.00s
ℹ️ Auto-routed to PID #14496 Gemini at F:\GitHub\vice-sharp (source console busy with mcp_command). Pipeline executed automatically — no re-send needed.

✓ Pipeline executed successfully | Window: #14496 Gemini | Status: Ready | Pipeline: & 'F:\GitHub\mcpserver-grok... | Duration: 1.60s | Location [FileSystem]: F:\GitHub\vice-sharp

{"status":"available","agent":"GrokCode","namespaces":["workflow.sessionlog","workflow.todo","workflow.requirements","workflow.triage","workflow.graphrag","workflow.memory","workflow.failsafe"],"requirementMethods":["workflow.requirements.listLayers","workflow.requirements.createLayer","workflow.requirements.updateLayer","workflow.requirements.effective"],"requirementClientMethods":["client.Requirements.ListRequirementLayersAsync","client.Requirements.CreateRequirementLayerAsync","client.Requirements.UpdateRequirementLayerAsync","client.Requirements.GetEffectiveRequirementsAsync"],"cacheDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok","hasSession":true,"hasCurrentTurn":true,"pendingCount":0,"pendingTurnCount":0,"failsafeDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\failsafe","failsafeCount":0,"failsafeQuarantineCount":0}

### Turn 42 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$cache = 'F:\\GitHub\\vice-sharp\\.mcpServer\\grok'; Get-ChildItem $cache -Recurse -File | Select-Object FullName, Length, LastWriteTime | Format-Table -AutoSize | Out-String -Width 220; Write-Output '--- session-state ---'; if (Test-Path \"$cache\\session-state.yaml\") { Get-Content \"$cache\\session-state.yaml\" -Raw }; Write-Output '--- current-turn ---'; if (Test-Path \"$cache\\current-turn.yaml\") { Get-Content \"$cache\\current-turn.yaml\" -Raw }"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$params = @{\n  agent = 'GrokCode'\n  limit = 5\n  offset = 0\n}\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.queryHistory' -ParamsObject $params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 240"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$params = @{\n  done = $false\n  limit = 15\n}\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.query' -ParamsObject $params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 240"}

### Turn 43 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #14496 Gemini | Status: Ready | Pipeline: $params = @{... | Duration: 2.48s | Location [FileSystem]: F:\GitHub\vice-sharp

type: result
payload:
  requestId: req-20260805T074353Z-04c6
  result:
  - agent: GrokCode
    sessionId: GrokCode-20260805T074200Z-plugin-session
    title: <user_query>
    model: grok
    started:
      dateTime: 2026-08-05T07:42:00.0000000
      utcDateTime: 2026-08-05T07:42:00.0000000Z
      localDateTime: 2026-08-05T02:42:00.0000000-05:00
      date: 2026-08-05T00:00:00.0000000
      day: 5
      dayOfWeek: Wednesday
      dayOfYear: 217
      hour: 7
      millisecond: 0
      microsecond: 0
      nanosecond: 0
      minute: 42
      month: 8
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 0
      ticks: 639215125200000000
      utcTicks: 639215125200000000
      timeOfDay: 07:42:00
      year: 2026
    lastUpdated:
      dateTime: 2026-08-05T07:42:05.0000000
      utcDateTime: 2026-08-05T07:42:05.0000000Z
      localDateTime: 2026-08-05T02:42:05.0000000-05:00
      date: 2026-08-05T00:00:00.0000000
      day: 5
      dayOfWeek: Wednesday
      dayOfYear: 217
      hour: 7
      millisecond: 0
      microsecond: 0
      nanosecond: 0
      minute: 42
      month: 8
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 5
      ticks: 639215125250000000
      utcTicks: 639215125250000000
      timeOfDay: 07:42:05
      year: 2026
    status: in_progress
    turnCount: 1
    tags: []
    filesModifiedCount: 0
  - agent: GrokCode
    sessionId: GrokCode-20260623T165352Z-bug6-validation
    title: BUG-6 server-truth validation - GrokCode - 2026-06-23T16:53:52Z
    model: grok-4.3
    started:
      dateTime: 2026-06-23T16:53:58.3966141
      utcDateTime: 2026-06-23T16:53:58.3966141Z
      localDateTime: 2026-06-23T11:53:58.3966141-05:00
      date: 2026-06-23T00:00:00.0000000
      day: 23
      dayOfWeek: Tuesday
      dayOfYear: 174
      hour: 16
      millisecond: 396
      microsecond: 614
      nanosecond: 100
      minute: 53
      month: 6
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 58
      ticks: 639178304383966141
      utcTicks: 639178304383966141
      timeOfDay: 16:53:58.3966141
      year: 2026
    lastUpdated:
      dateTime: 2026-06-23T16:54:05.7265550
      utcDateTime: 2026-06-23T16:54:05.7265550Z
      localDateTime: 2026-06-23T11:54:05.7265550-05:00
      date: 2026-06-23T00:00:00.0000000
      day: 23
      dayOfWeek: Tuesday
      dayOfYear: 174
      hour: 16
      millisecond: 726
      microsecond: 555
      nanosecond: 0
      minute: 54
      month: 6
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 5
      ticks: 639178304457265550
      utcTicks: 639178304457265550
      timeOfDay: 16:54:05.7265550
      year: 2026
    status: in_progress
    turnCount: 1
    tags: []
    filesModifiedCount: 1
  - agent: GrokCode
    sessionId: GrokCode-20260527T224114Z-perf-sprite-dma-opt-001
    title: PERF-SPRITE-DMA-OPT-001 - Attack sprite DMA mapping regression to reach 225% (25% of x64sc warp baseline); BDP test-first for cache + iterate
    model: grok-4.3-tui
    started: &o0
      dateTime: 2026-05-27T22:41:16.9304833
      utcDateTime: 2026-05-27T22:41:16.9304833Z
      localDateTime: 2026-05-27T17:41:16.9304833-05:00
      date: 2026-05-27T00:00:00.0000000
      day: 27
      dayOfWeek: Wednesday
      dayOfYear: 147
      hour: 22
      millisecond: 930
      microsecond: 483
      nanosecond: 300
      minute: 41
      month: 5
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 16
      ticks: 639155184769304833
      utcTicks: 639155184769304833
      timeOfDay: 22:41:16.9304833
      year: 2026
    lastUpdated: *o0
    status: in_progress
    turnCount: 0
    tags: []
    filesModifiedCount: 0
  - agent: GrokCode
    sessionId: GrokCode-20260527T201524Z-grok-4-3-tui-vice-backfill
    title: BACKFILL-VIDEO-001 native VIC depth - next gated BDP slice after debugcart harness (explore gaps 019e6acc-29b8-77f1-a9cc-56499af366f9)
    model: grok-4.3-tui
    started: &o1
      dateTime: 2026-05-27T20:15:25.7567447
      utcDateTime: 2026-05-27T20:15:25.7567447Z
      localDateTime: 2026-05-27T15:15:25.7567447-05:00
      date: 2026-05-27T00:00:00.0000000
      day: 27
      dayOfWeek: Wednesday
      dayOfYear: 147
      hour: 20
      millisecond: 756
      microsecond: 744
      nanosecond: 700
      minute: 15
      month: 5
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 25
      ticks: 639155097257567447
      utcTicks: 639155097257567447
      timeOfDay: 20:15:25.7567447
      year: 2026
    lastUpdated: *o1
    status: in_progress
    turnCount: 0
    tags: []
    filesModifiedCount: 0
  - agent: GrokCode
    sessionId: GrokCode-20260527T200201Z-backfill-video-001
    title: BACKFILL-VIDEO-001 native depth slice - FLI/AFLI or ECM edge (vice-sharp)
    model: grok-4
    started: &o2
      dateTime: 2026-05-27T20:02:07.2911778
      utcDateTime: 2026-05-27T20:02:07.2911778Z
      localDateTime: 2026-05-27T15:02:07.2911778-05:00
      date: 2026-05-27T00:00:00.0000000
      day: 27
      dayOfWeek: Wednesday
      dayOfYear: 147
      hour: 20
      millisecond: 291
      microsecond: 177
      nanosecond: 800
      minute: 2
      month: 5
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 7
      ticks: 639155089272911778
      utcTicks: 639155089272911778
      timeOfDay: 20:02:07.2911778
      year: 2026
    lastUpdated: *o2
    status: in_progress
    turnCount: 0
    tags: []
    filesModifiedCount: 0
  deprecated: true

---
⚠️ SCOPE: Use $script: prefix for: $params


### Turn 44 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #14496 Gemini | Status: Ready | Pipeline: $cache = 'F:\GitHub\vice-sh... | Duration: 0.25s | Location [FileSystem]: F:\GitHub\vice-sharp

FullName                                                Length LastWriteTime
--------                                                ------ -------------
F:\GitHub\vice-sharp\.mcpServer\grok\current-turn.yaml     480 8/5/2026 2:42:05 AM
F:\GitHub\vice-sharp\.mcpServer\grok\session-state.yaml    489 8/5/2026 2:42:05 AM


--- session-state ---
status: verified
sessionId: GrokCode-20260805T074200Z-plugin-session
agent: GrokCode
started: 2026-08-05T07:42:00Z
lastUpdated: 2026-08-05T07:42:00Z
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-23T00:10:41.1658357Z
agentSessionId: 019fd0df-383d-7fc0-9f0a-69785afd78ed
agentSessionTranscriptFile: ""
agentExecutablePath: C:\Users\kingd\.grok\bin\grok.exe
agentExecutableVersion: grok 0.2.118 (1e1687c1cf) [stable]
title: <user_query>

--- current-turn ---
turnRequestId: req-20260805T074205Z-prompt-5821
queryTitle: <user_query>
openedAt: 2026-08-05T07:42:05Z
status: in_progress
sessionId: GrokCode-20260805T074200Z-plugin-session
codeEdits: 0
lastBuildStatus: unknown
auditActions: 0
auditDialog: 0
auditDecisions: 0
auditFiles: 0
auditCommits: 0
queryText: |-
  <user_query>
  new session
  </user_query>
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-23T00:10:41.1658357Z

⚠️ Preferred cmdlets for PowerShell.MCP console:
• Use Show-TextFiles instead of Get-Content (aliases: gc, cat, type)
⚠️ SCOPE: Use $script: prefix for: $cache


### Turn 45 (Function)
[tool_response]
Output too large (78008 characters). Full output saved to: C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output\pwsh_output_20260805_024357_969_2cctlwax.nhf.txt
Use invoke_expression('Show-TextFiles "C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output\pwsh_output_20260805_024357_969_2cctlwax.nhf.txt" -Contains "search term"') or -Pattern "regex" to search the output.

--- Preview (first ~1000 chars) ---
✓ Pipeline executed successfully | Window: #14496 Gemini | Status: Ready | Pipeline: $params = @{... | Duration: 0.53s | Location [FileSystem]: F:\GitHub\vice-sharp

type: result
payload:
  requestId: req-20260805T074356Z-dbe5
  result:
    items:
    - id: PLAN-ARCHVIC20-001
      title: Create VIC-20 Computer and Variants
      section: Architectures
      priority: high
      done: false
      description:
      - 'Implement the VIC-20 architecture in ViceSharp.Architectures: 6502 + VIC-I (6560 NTSC / 6561 PAL) + dual 6522 VIA, expandable RAM configurations (unexpanded, 3K, 8K, 16K, 24K, 32K), cartridge port, IEC serial; include regional/RAM variants as machine models mirroring VICE xvic.'
      technicalDetails: &o0 []
      implementationTasks: &o1 []
      dependsOn: *o0
      functionalRequirements: *o0
      technicalRequirements: *o0
    - id: BUG-SIDAUDIO-001
      title: SID runs 16x too slow (ClockDivisor=16) - pitch and envelope timing wrong

--- truncated (75020 chars omitted) ---
--- Preview (last ~2000 chars) ---
Store submission). The genuinely console-only bucket: actual console deploy of the AOT MSIX, on-console AppContainer resource budget + sustained PAL-50, real-TV overscan/focus-engagement/Guide-reservation, Microsoft Store cert (+ GPL section-6 vs Store license legal review). Not a development gate; requires a physical Dev-Mode console + Partner Center. Deferred until the operator prepares a Store submission. Tracked, not dropped.'
      description:
      - 'TIER C (DEFERRED to Microsoft Store submission; the genuinely console-only checks; NOT a development gate; tracked not dropped). Deps: S34 + S36-S41 (all dev-PC gates green). Tests-first: console/Store-submission checklist - actual console deploy of the AOT MSIX via Dev Mode; the AppContainer resource budget (~1GB RAM / 2-4 shared cores / 45% GPU headroom) and sustained PAL-50 perf ON the physical console; Xbox device-family behaviors (real-TV overscan, focus-engagement on the TV profile, Guide/Nexus shell reservation); Microsoft Store certification (behind a documented GPL section-6 no-further-restrictions vs Store Standard Application License Terms review). RED: not yet validated on a physical Dev-Mode console / not submitted. GREEN: console deploy runs within the resource budget at sustained PAL-50, TV device-family behaviors confirmed, Store cert passed (or GPL/Store legal review completed). Validation: on-console deploy (WinAppDeployCmd install -file <msix> -ip <console-ip> -pin <pin>) + on-console perf/telemetry capture + Partner Center submission. Acceptance: console deploy + resource budget + TV behaviors + Store cert recorded; this is the only console-required bucket. Requirements: TEST-XBOXDEPLOY-007, TEST-TESTGATE-010 (console-only subset); Store certification is a governance gate with no FR/TR mapping.'
      technicalDetails: *o0
      implementationTasks: *o1
      dependsOn: *o0
      functionalRequirements: *o0
      technicalRequirements: *o0
    totalCount: 61
  deprecated: true

---

### Turn 46 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:params = @{\n  response = 'Session bootstrap in progress: profile loaded, marker trust OK, health nonce OK, plugin Status available.'\n  interpretation = 'User requested a new GrokCode session on vice-sharp: load profile, bootstrap MCP, report resume state.'\n  tags = @('session-start','bootstrap','xbox-uwp')\n  contextList = @('AGENTS-README-FIRST.yaml','HANDOFF.md','C:\\Users\\kingd\\.claude\\profile')\n}\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.updateTurn' -ParamsObject $script:params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 200"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:params = @{\n  dialogItems = @(\n    @{\n      timestamp = (Get-Date).ToUniversalTime().ToString('o')\n      role = 'model'\n      content = 'Loaded operator profile (13 markdown files). Marker signature HMAC-SHA256 match. Health nonce echo match. Plugin Status available (GrokCode, plugin 1.85.0). Using hook session GrokCode-20260805T074200Z-plugin-session.'\n      category = 'observation'\n    },\n    @{\n      timestamp = (Get-Date).ToUniversalTime().ToString('o')\n      role = 'model'\n      content = \"Decision: Reuse hook-opened session GrokCode-20260805T074200Z-plugin-session and turn req-20260805T074205Z-prompt-5821 instead of opening a duplicate session for 'new session'. Consequence: single server-side session for this prompt; turn title will be refined from placeholder <user_query>.\"\n      category = 'decision'\n    }\n  )\n}\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.appendDialog' -ParamsObject $script:params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 200"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:params = @{\n  actions = @(\n    @{ order = 1; description = 'Loaded operator profile from ~/.claude/profile (13 files, excluding add-profile skill ports)'; type = 'observation'; status = 'completed'; filePath = 'C:\\Users\\kingd\\.claude\\profile' },\n    @{ order = 2; description = 'Verified marker HMAC-SHA256 signature match and /health nonce echo'; type = 'observation'; status = 'completed'; filePath = 'AGENTS-README-FIRST.yaml' },\n    @{ order = 3; description = 'Plugin Status available: sourceType GrokCode, plugin 1.85.0, namespaces sessionlog/todo/requirements/triage/memory'; type = 'observation'; status = 'completed'; filePath = 'F:\\GitHub\\mcpserver-grok-plugin' },\n    @{ order = 4; description = 'Reuse hook session GrokCode-20260805T074200Z-plugin-session; do not open duplicate for new session prompt'; type = 'design_decision'; status = 'completed'; filePath = '' },\n    @{ order = 5; description = 'Read HANDOFF.md resume snapshot for PLAN-XBOXUWP on feat/xbox-uwp-app'; type = 'observation'; status = 'completed'; filePath = 'HANDOFF.md' }\n  )\n}\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.appendActions' -ParamsObject $script:params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 200"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"git -C 'F:\\GitHub\\vice-sharp' status -sb; git -C 'F:\\GitHub\\vice-sharp' log -1 --oneline; git -C 'F:\\GitHub\\vice-sharp' branch --show-current"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$out = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.query' -ParamsObject @{ done = $false; limit = 100 } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin'\n# extract open high/critical ids from YAML-ish text\n$text = $out | Out-String\nif ($text -match 'totalCount:\\s*(\\d+)') { $total = $Matches[1] } else { $total = '?' }\n$high = [regex]::Matches($text, '(?m)^\\s+- id: (\\S+)\\r?\\n\\s+title: (.+)$') | ForEach-Object { [pscustomobject]@{ Id = $_.Groups[1].Value; Title = $_.Groups[2].Value.Trim() } }\n# get priority high ones via second parse\n$blocks = [regex]::Matches($text, '(?ms)- id: ([A-Z0-9-]+).*?priority: (\\w+).*?title: ([^\\r\\n]+)')\n$rows = foreach ($m in $blocks) { [pscustomobject]@{ Id=$m.Groups[1].Value; Priority=$m.Groups[2].Value; Title=$m.Groups[3].Value.Trim() } }\n$prio = $rows | Where-Object { $_.Priority -in @('critical','high') } | Select-Object -First 20\n[pscustomobject]@{ TotalOpen = $total; HighOrCritical = @($prio).Count; Sample = $prio | Select-Object -First 12 | Format-Table -AutoSize | Out-String }"}

### Turn 47 (Function)
[tool_response]
  - ⚠ Console PID #31644 was closed
  - ⚠ Console PID #21052 was closed

⧗ Pipeline is running | Window: #14496 Gemini | Status: Busy | Pipeline: $script:params = @{... | Duration: 4.50s
ℹ️ Switched to console PID #80860 Capricorn. Pipeline running on the new console.

✓ Pipeline executed successfully | Window: #80860 Capricorn | Status: Ready | Pipeline: $out = & 'F:\GitHub\mcpserv... | Duration: 3.49s | Location [FileSystem]: F:\GitHub\vice-sharp

TotalOpen HighOrCritical Sample
--------- -------------- ------
61                    16 …
⚠️ SCOPE: Use $script: prefix for: $out, $text, $total, $high, $blocks, $rows, $prio


### Turn 48 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #14496 Gemini | Status: Completed | Pipeline: $script:params = @{... | Duration: 4.50s

✓ Pipeline executed successfully | Window: #14496 Gemini | Status: Ready | Pipeline: $script:params = @{... | Duration: 4.12s | Location [FileSystem]: F:\GitHub\vice-sharp

### Turn 49 (Function)
[tool_response]
⧗ | PID #14496 Gemini | Status: Busy | Pipeline: $script:params = @{... | Duration: 0.01s
ℹ️ Auto-routed to PID #63456 Virgo at F:\GitHub\vice-sharp (source console busy with mcp_command). Pipeline executed automatically — no re-send needed.

Command execution failed: PowerShell.MCP module communication to console PID #63456 Virgo failed for command: $script:params = @{
  actions = @(
    @{ order = 1; description = 'Loaded operator profile from ~/.claude/profile (13 files, excluding add-profile skill ports)'; type = 'observation'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' },
    @{ order = 2; description = 'Verified marker HMAC-SHA256 signature match and /health nonce echo'; type = 'observation'; status = 'completed'; filePath = 'AGENTS-README-FIRST.yaml' },
    @{ order = 3; description = 'Plugin Status available: sourceType GrokCode, plugin 1.85.0, namespaces sessionlog/todo/requirements/triage/memory'; type = 'observation'; status = 'completed'; filePath = 'F:\GitHub\mcpserver-grok-plugin' },
    @{ order = 4; description = 'Reuse hook session GrokCode-20260805T074200Z-plugin-session; do not open duplicate for new session prompt'; type = 'design_decision'; status = 'completed'; filePath = '' },
    @{ order = 5; description = 'Read HANDOFF.md resume snapshot for PLAN-XBOXUWP on feat/xbox-uwp-app'; type = 'observation'; status = 'completed'; filePath = 'HANDOFF.md' }
  )
}
& 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.appendActions' -ParamsObject $script:params -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' | Out-String -Width 200
  - ⚠ Console PID #63456 Virgo was closed
Please try again. A new console will be started automatically if needed.

### Turn 50 (Function)
[tool_response]
⧗ | PID #14496 Gemini | Status: Busy | Pipeline: $script:params = @{... | Duration: 0.00s
ℹ️ Auto-routed to PID #63456 Virgo at F:\GitHub\vice-sharp (source console busy with mcp_command). Pipeline executed automatically — no re-send needed.

✓ Pipeline executed successfully | Window: #63456 Virgo | Status: Ready | Pipeline: $script:params = @{... | Duration: 3.68s | Location [FileSystem]: F:\GitHub\vice-sharp


### Turn 51 (Function)
[tool_response]
⧗ | PID #14496 Gemini | Status: Busy | Pipeline: $script:params = @{... | Duration: 0.00s
ℹ️ Auto-routed to PID #63456 Virgo at F:\GitHub\vice-sharp (source console busy with mcp_command). Pipeline executed automatically — no re-send needed.

✓ Pipeline executed successfully | Window: #63456 Virgo | Status: Ready | Pipeline: git -C 'F:\GitHub\vice-shar... | Duration: 4.00s | Location [FileSystem]: F:\GitHub\vice-sharp

## [32mfeat/xbox-uwp-app[m
 [31mM[m HANDOFF.md
 [31mM[m azure-pipelines.ci.yml
 [31mM[m azure-pipelines.release.yml
 [31mM[m src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
 [31mM[m src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/AboutPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/AboutPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/InputMappingPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/SettingsPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 [31mM[m tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json
[31m??[m docs/S-Blox/
[31m??[m docs/reviews/code-review-20260714T001056552Z.md
[31m??[m docs/reviews/code-review-20260714T004256563Z.md
[31m??[m docs/romless-vic-badline-fix.md
[33ma199063[m feat(xbox): B steps back on menu subpages; subpages expand to the full window [PLAN-XBOXUWP FEAT-XMENUSUBPAGE-001]
feat/xbox-uwp-app

### Turn 52 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__start_console
- tool_input: {"start_location":"F:\\GitHub\\vice-sharp","banner":"Session bootstrap: finish logging.","reason":"Retry session log append and complete"}

### Turn 53 (Function)
[tool_response]
PowerShell console started successfully with PowerShell.MCP module imported.

✓ get_current_location executed successfully | Window: #53636 Leo | Status: Ready | Pipeline: # get_current_location... | Duration: 0.00s | Location [FileSystem]: F:\GitHub\vice-sharp

{
  "system": {
    "os": "Microsoft Windows NT 10.0.26200.0",
    "powershell_version": "7.6.4",
    "host_name": "PAYTON-LEGION2",
    "current_user": "PAYTON-LEGION2\\kingd",
    "is_elevated": false,
    "culture": "en-US",
    "timezone": "Central Standard Time",
    "timezone_offset": "-06:00",
    "execution_policy": "Unrestricted"
  },
  "available_drives": [
    {
      "name": "Alias",
      "provider": "Alias",
      "current_path": "\\",
      "description": "Drive that contains a view of the aliases stored in a session state"
    },
    {
      "name": "Cert",
      "provider": "Certificate",
      "root": "\\",
      "current_path": "\\",
      "description": "X509 Certificate Provider"
    },
    {
      "name": "Env",
      "provider": "Environment",
      "current_path": "\\",
      "description": "Drive that contains a view of the environment variables for the process"
    },
    {
      "name": "C",
      "provider": "FileSystem",
      "root": "C:\\",
      "current_path": "\\Users\\kingd\\OneDrive\\Documents\\PowerShell\\Snippets"
    },
    {
      "name": "D",
      "provider": "FileSystem",
      "root": "D:\\",
      "current_path": "\\",
      "description": "SYSTEM_DRV"
    },
    {
      "name": "E",
      "provider": "FileSystem",
      "root": "E:\\",
      "current_path": "\\",
      "description": "Windows-SSD"
    },
    {
      "name": "F",
      "provider": "FileSystem",
      "root": "F:\\",
      "current_path": "\\GitHub\\vice-sharp",
      "description": "Sources"
    },
    {
      "name": "Temp",
      "provider": "FileSystem",
      "root": "C:\\Users\\kingd\\AppData\\Local\\Temp\\",
      "current_path": "\\",
      "description": "Drive that maps to the temporary directory path for the current user"
    },
    {
      "name": "Function",
      "provider": "Function",
      "current_path": "\\",
      "description": "Drive that contains a view of the functions stored in a session state"
    },
    {
      "name": "HKCU",
      "provider": "Registry",
      "root": "HKEY_CURRENT_USER",
      "current_path": "\\",
      "description": "The software settings for the current user"
    },
    {
      "name": "HKLM",
      "provider": "Registry",
      "root": "HKEY_LOCAL_MACHINE",
      "current_path": "\\",
      "description": "The configuration settings for the local computer"
    },
    {
      "name": "Variable",
      "provider": "Variable",
      "current_path": "\\",
      "description": "Drive that contains a view of those variables stored in a session state"
    },
    {
      "name": "WSMan",
      "provider": "WSMan",
      "current_path": "\\",
      "description": "Root of WsMan Config Storage."
    }
  ]
}

### Turn 54 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:params = @{\n  actions = @(\n    @{ order = 1; description = 'Loaded operator profile from ~/.claude/profile (13 files, excluding add-profile skill ports)'; type = 'observation'; status = 'completed'; filePath = 'C:\\Users\\kingd\\.claude\\profile' },\n    @{ order = 2; description = 'Verified marker HMAC-SHA256 signature match and /health nonce echo'; type = 'observation'; status = 'completed'; filePath = 'AGENTS-README-FIRST.yaml' },\n    @{ order = 3; description = 'Plugin Status available: sourceType GrokCode, plugin 1.85.0'; type = 'observation'; status = 'completed'; filePath = 'F:\\GitHub\\mcpserver-grok-plugin' },\n    @{ order = 4; description = 'Reuse hook session GrokCode-20260805T074200Z-plugin-session; do not open duplicate for new session prompt'; type = 'design_decision'; status = 'completed'; filePath = '' },\n    @{ order = 5; description = 'Read HANDOFF.md resume snapshot for PLAN-XBOXUWP on feat/xbox-uwp-app'; type = 'observation'; status = 'completed'; filePath = 'HANDOFF.md' }\n  )\n}\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.appendActions' -ParamsObject $script:params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin'"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$text = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.query' -ParamsObject @{ done = $false; limit = 100 } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String\n$blocks = [regex]::Matches($text, '(?ms)- id: ([A-Z0-9-]+)\\r?\\n\\s+title: ([^\\r\\n]+)\\r?\\n\\s+section: ([^\\r\\n]+)\\r?\\n\\s+priority: (\\w+)')\n$rows = foreach ($m in $blocks) { [pscustomobject]@{ Id=$m.Groups[1].Value; Title=$m.Groups[2].Value.Trim(); Section=$m.Groups[3].Value.Trim(); Priority=$m.Groups[4].Value } }\nWrite-Output \"ParsedRows=$($rows.Count)\"\n$rows | Where-Object { $_.Priority -in 'critical','high' } | Select-Object -First 15 Id,Priority,Title | Format-List | Out-String\nWrite-Output '--- xbox related ---'\n$rows | Where-Object { $_.Id -match 'XBOX|XKBD|XMENU|ROMLESS' -or $_.Title -match 'Xbox|UWP|keyboard|romless' } | Select-Object Id,Priority,Title | Format-List | Out-String"}

### Turn 55 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #53636 Leo | Status: Ready | Pipeline: $script:params = @{... | Duration: 3.36s | Location [FileSystem]: F:\GitHub\vice-sharp

⚠️ Preferred cmdlets for PowerShell.MCP console:
• Use Show-TextFiles instead of Get-Content (aliases: gc, cat, type)

### Turn 56 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #53636 Leo | Status: Ready | Pipeline: $text = & 'F:\GitHub\mcpser... | Duration: 3.66s | Location [FileSystem]: F:\GitHub\vice-sharp

ParsedRows=61

Id       : PLAN-ARCHVIC20-001
Priority : high
Title    : Create VIC-20 Computer and Variants

Id       : BUG-SIDAUDIO-001
Priority : high
Title    : SID runs 16x too slow (ClockDivisor=16) - pitch and envelope timing wrong

Id       : PLAN-IECDEBUG-001
Priority : High
Title    : IEC time-travel debugger + true-drive 1541 program

Id       : PLAN-REMOTECTRL-001
Priority : High
Title    : Embed Avalonia.RemoteControl server for live visual-tree validation

Id       : PLAN-UIREDESIGN-001
Priority : High
Title    : Avalonia UI redesign (flyout shell, reusable peripheral controls, settings UC, VICE-style menu, True Drive toggle) - BDPv4

Id       : QA-TESTAC-001
Priority : high
Title    : Audit all Tests to ensure proper Acceptance Criteria are defined and covered

Id       : PLAN-CARTRAMLINK-001
Priority : high
Title    : Create CMD RAM Link Cartridge

Id       : BUG-DRV1540-001
Priority : high
Title    : 1540 drive boot-phase offset breaks screen-on LOAD lockstep vs x64sc

Id       : PLAN-DRIVE1540-001
Priority : high
Title    : Create 1540 disk drive

Id       : PLAN-DRIVE1541II-001
Priority : high
Title    : Create 1541-II disk drive

Id       : PLAN-DRIVE1571-001
Priority : high
Title    : Create 1571 disk drive

Id       : PLAN-DRIVE1581-001
Priority : high
Title    : Create 1581 disk drive (3.5in 800K, WD1770 controller)

Id       : PLAN-DRIVECMDHD-001
Priority : high
Title    : Create CMD-HD Hard Drive

Id       : PLAN-PAUSEMUTE-001
Priority : high
Title    : Mute sound before pausing the emulator; auto-unmute on resume

Id       : IMPL-WINGET-001
Priority : high
Title    : Integrate Winget manifest validation into the publishing pipeline


--- xbox related ---

Id       : PLAN-ROMM-001
Priority : high
Title    : Create a RomM client library and UI for Avalonia and Xbox

Id       : IMPL-ROMM-010
Priority : medium
Title    : '[X1] XboxGameLauncher (AC-LAUNCH-05)'

Id       : IMPL-ROMM-011
Priority : medium
Title    : '[X2] XboxCoverImageLoader + XboxRomMConnectionStore'

Id       : IMPL-ROMM-013
Priority : low
Title    : '[X4] Xbox CSDb tab (reuses L6 VM)'

Id       : PLAN-XBOXCLIENT-001
Priority : high
Title    : XBOX-compatible client (10-foot console front-end)

Id       : PLAN-DEVCARDART-001
Priority : medium
Title    : XAML vector art backgrounds for device cards (drives, datasette, cartridge, keyboards)

Id       : IMPL-XBOXUWP-000
Priority : high
Title    : 'S0: FEASIBILITY GO/NO-GO (the one gate all production slices depend on)'

Id       : PLAN-XKEYBOARD-001
Priority : high
Title    : UWP Virtual Keyboard should be a real C64 keyboard

Id       : FIX-XKBDNMI-001
Priority : medium
Title    : Wire C64 RESTORE trigger (IsRestorePressed) to the CPU NMI edge

Id       : IMPL-XBOXUWP-035
Priority : medium
Title    : 'S35: Desktop-intact + full-suite + AOT regression guard'

Id       : IMPL-XBOXUWP-036
Priority : medium
Title    : 'S36: On-console deploy + boot-to-menu + C64 render smoke'

Id       : IMPL-XBOXUWP-037
Priority : medium
Title    : 'S37: On-console live gamepad smoke (JOY2/JOY1 + A/B fire + menu neutralization + swap)'

Id       : IMPL-XBOXUWP-038
Priority : medium
Title    : 'S38: On-console A/V smoke (Win2D video + XAudio2 audio + mute + suspend/resume)'

Id       : IMPL-XBOXUWP-039
Priority : medium
Title    : 'S39: On-console system-button + context smoke'

Id       : IMPL-XBOXUWP-040
Priority : medium
Title    : 'S40: On-console first-run ROM provisioning + settings persistence + device attach E2E'

Id       : IMPL-XBOXUWP-041
Priority : medium
Title    : 'S41: On-console performance within resource budget (PAL 50)'

Id       : IMPL-XBOXUWP-042
Priority : low
Title    : 'S42: Console / Store-submission validation (deferred: console deploy, on-console resource budget, TV device-family behaviors, Store cert)'

### Turn 57 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$response = @'\nNew session ready on vice-sharp.\n\nBootstrap receipts:\n- Profile: 13 files loaded from ~/.claude/profile\n- Marker signature: HMAC-SHA256 match\n- Health: status Healthy, nonce echo match, server 1.4.25\n- Plugin: mcpserver-grok-plugin 1.85.0 Status available (GrokCode)\n- Session: GrokCode-20260805T074200Z-plugin-session (hook-opened, reused)\n- Turn: req-20260805T074205Z-prompt-5821\n- Open TODOs: 61 (16 high/critical sample)\n- Branch: feat/xbox-uwp-app @ a199063, dirty tree (Xbox UI + pipelines + scratch docs)\n- HANDOFF: PLAN-XBOXUWP dev-PC bring-up; Category=Xbox 501/501 last gate; open FIX-XKBDNMI-001, controls shortcuts, B-controller verify, S42 deferred\n\nNote: Streamable HTTP mcpserver MCP tool surface failed auth in this host; session/TODO work is via Invoke-McpPlugin.ps1 (plugin path), which is working.\n'@\n& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command CompleteTurn -Response $response -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String -Width 200"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"& 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Status -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String; Write-Output '---'; Get-Content 'F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\session-state.yaml' -Raw; Write-Output '---'; if (Test-Path 'F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\current-turn.yaml') { Get-Content 'F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\current-turn.yaml' -Raw } else { 'no current-turn (expected after complete)' }"}

### Turn 58 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #53636 Leo | Status: Completed | Pipeline: & 'F:\GitHub\mcpserver-grok... | Duration: 1.71s

✓ Pipeline executed successfully | Window: #53636 Leo | Status: Ready | Pipeline: & 'F:\GitHub\mcpserver-grok... | Duration: 1.40s | Location [FileSystem]: F:\GitHub\vice-sharp

{"status":"available","agent":"GrokCode","namespaces":["workflow.sessionlog","workflow.todo","workflow.requirements","workflow.triage","workflow.graphrag","workflow.memory","workflow.failsafe"],"requirementMethods":["workflow.requirements.listLayers","workflow.requirements.createLayer","workflow.requirements.updateLayer","workflow.requirements.effective"],"requirementClientMethods":["client.Requirements.ListRequirementLayersAsync","client.Requirements.CreateRequirementLayerAsync","client.Requirements.UpdateRequirementLayerAsync","client.Requirements.GetEffectiveRequirementsAsync"],"cacheDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok","hasSession":true,"hasCurrentTurn":true,"pendingCount":0,"pendingTurnCount":0,"failsafeDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\failsafe","failsafeCount":0,"failsafeQuarantineCount":0}

---
status: verified
sessionId: GrokCode-20260805T074200Z-plugin-session
agent: GrokCode
started: 2026-08-05T07:42:00Z
lastUpdated: 2026-08-05T07:42:00Z
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-23T00:10:41.1658357Z
agentSessionId: 019fd0df-383d-7fc0-9f0a-69785afd78ed
agentSessionTranscriptFile: ""
agentExecutablePath: C:\Users\kingd\.grok\bin\grok.exe
agentExecutableVersion: grok 0.2.118 (1e1687c1cf) [stable]
title: <user_query>

---
turnRequestId: req-20260805T074205Z-prompt-5821
queryTitle: <user_query>
openedAt: 2026-08-05T07:42:05Z
status: in_progress
sessionId: GrokCode-20260805T074200Z-plugin-session
codeEdits: 5
lastBuildStatus: unknown
auditActions: 5
auditDialog: 2
auditDecisions: 1
auditFiles: 5
auditCommits: 0
queryText: |-
  <user_query>
  new session
  </user_query>
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-23T00:10:41.1658357Z
response: 'Session bootstrap in progress: profile loaded, marker trust OK, health nonce OK, plugin Status available.'
interpretation: 'User requested a new GrokCode session on vice-sharp: load profile, bootstrap MCP, report resume state.'
tags:
  - session-start
  - bootstrap
  - xbox-uwp
contextList:
  - AGENTS-README-FIRST.yaml
  - HANDOFF.md
  - C:\Users\kingd\.claude\profile

### Turn 59 (Function)
[tool_response]
⧗ | PID #53636 Leo | Status: Busy | Pipeline: & 'F:\GitHub\mcpserver-grok... | Duration: 0.00s
ℹ️ Auto-routed to PID #11500 Aquarius at F:\GitHub\vice-sharp (source console busy with mcp_command). Pipeline executed automatically — no re-send needed.

✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $response = @'... | Duration: 3.65s | Location [FileSystem]: F:\GitHub\vice-sharp


### Turn 60 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$r = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command CompleteTurn -Response 'New session bootstrap complete. Ready for next instruction on feat/xbox-uwp-app.' -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' 2>&1 | Out-String; Write-Output $r; Write-Output 'EXIT'; Write-Output $LASTEXITCODE"}
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$h = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.queryHistory' -ParamsObject @{ agent = 'GrokCode'; limit = 1; offset = 0 } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String; $h"}

### Turn 61 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $h = & 'F:\GitHub\mcpserver... | Duration: 2.67s | Location [FileSystem]: F:\GitHub\vice-sharp

type: result
payload:
  requestId: req-20260805T074559Z-d31d
  result:
  - agent: GrokCode
    sessionId: GrokCode-20260805T074200Z-plugin-session
    title: <user_query>
    model: grok
    started:
      dateTime: 2026-08-05T07:42:00.0000000
      utcDateTime: 2026-08-05T07:42:00.0000000Z
      localDateTime: 2026-08-05T02:42:00.0000000-05:00
      date: 2026-08-05T00:00:00.0000000
      day: 5
      dayOfWeek: Wednesday
      dayOfYear: 217
      hour: 7
      millisecond: 0
      microsecond: 0
      nanosecond: 0
      minute: 42
      month: 8
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 0
      ticks: 639215125200000000
      utcTicks: 639215125200000000
      timeOfDay: 07:42:00
      year: 2026
    lastUpdated:
      dateTime: 2026-08-05T07:45:52.0000000
      utcDateTime: 2026-08-05T07:45:52.0000000Z
      localDateTime: 2026-08-05T02:45:52.0000000-05:00
      date: 2026-08-05T00:00:00.0000000
      day: 5
      dayOfWeek: Wednesday
      dayOfYear: 217
      hour: 7
      millisecond: 0
      microsecond: 0
      nanosecond: 0
      minute: 45
      month: 8
      offset: 00:00:00
      totalOffsetMinutes: 0
      second: 52
      ticks: 639215127520000000
      utcTicks: 639215127520000000
      timeOfDay: 07:45:52
      year: 2026
    status: in_progress
    turnCount: 1
    tags:
    - session-start
    - bootstrap
    - xbox-uwp
    filesModifiedCount: 4
  deprecated: true

---
⚠️ SCOPE: Use $script: prefix for: $h


### Turn 62 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $r = & 'F:\GitHub\mcpserver... | Duration: 2.97s | Location [FileSystem]: F:\GitHub\vice-sharp

EXIT
0
⚠️ SCOPE: Use $script: prefix for: $r


### Turn 63 (Assistant)
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$script:params = @{ response = 'New session bootstrap complete on vice-sharp. Ready for next instruction.' }\n$out = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.completeTurn' -ParamsObject $script:params -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' 2>&1 | Out-String\nWrite-Output $out\nWrite-Output '---turn---'\nif (Test-Path 'F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\current-turn.yaml') { Get-Content 'F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\current-turn.yaml' -Raw } else { 'no current-turn' }"}

### Turn 64 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $script:params = @{ respons... | Duration: 3.07s | Location [FileSystem]: F:\GitHub\vice-sharp

---turn---
turnRequestId: req-20260805T074205Z-prompt-5821
queryTitle: <user_query>
openedAt: 2026-08-05T07:42:05Z
status: completed
sessionId: GrokCode-20260805T074200Z-plugin-session
codeEdits: 5
lastBuildStatus: unknown
auditActions: 5
auditDialog: 2
auditDecisions: 1
auditFiles: 5
auditCommits: 0
queryText: |-
  <user_query>
  new session
  </user_query>
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-23T00:10:41.1658357Z
response: 'Session bootstrap in progress: profile loaded, marker trust OK, health nonce OK, plugin Status available.'
interpretation: 'User requested a new GrokCode session on vice-sharp: load profile, bootstrap MCP, report resume state.'
tags:
  - session-start
  - bootstrap
  - xbox-uwp
contextList:
  - AGENTS-README-FIRST.yaml
  - HANDOFF.md
  - C:\Users\kingd\.claude\profile

⚠️ Preferred cmdlets for PowerShell.MCP console:
• Use Show-TextFiles instead of Get-Content (aliases: gc, cat, type)

### Turn 65 (Assistant)
**2026-08-05 02:46 CDT** - Session ready.

Operator profile loaded (**13** markdown files from `~/.claude/profile`).

### Bootstrap receipts

| Check | Result |
|-------|--------|
| Marker signature | HMAC-SHA256 **match** |
| `/health` + nonce | **Healthy**, nonce echo matched (`ubn8gclrvz7qt6m9yao2jx1f`) |
| MCP server | `1.4.25` on `http://PAYTON-LEGION2:7147` |
| Plugin | `mcpserver-grok-plugin` **1.85.0**, Status `available`, agent `GrokCode` |
| Session | `GrokCode-20260805T074200Z-plugin-session` (hook-opened; reused, no duplicate) |
| Bootstrap turn | `req-20260805T074205Z-prompt-5821` → **completed** |
| Open TODOs | **61** open (16 high/critical in sample) |

**Note:** Host Streamable HTTP `mcpserver` MCP is auth-failed in this environment. Session/TODO work is via `Invoke-McpPlugin.ps1` (plugin path), which is working.

### Resume state (`HANDOFF.md`)

- **Branch:** `feat/xbox-uwp-app` @ `a199063` (dirty tree: Xbox UI pages, pipelines, untracked scratch under `docs/`)
- **Active plan:** PLAN-XBOXUWP (UWP Xbox head) - runs/renders C64 on dev PC
- **Last shipped:** FIX-XKBDPANEL-001, FEAT-XMENUSUBPAGE-001; gate Category=Xbox **501/501**
- **Open on Xbox track:** controls keyboard-shortcut config; B-on-controller verify; FIX-XKBDNMI-001 (RESTORE→NMI); per-model ROM picker; S42 console/Store deferred
- **Parallel note (dated):** romless VIC worktree / `FIX-ROMLESSVIC-001` (confirm live if you care)

### Ready for

What do you want next: continue PLAN-XBOXUWP, pick an open TODO, or something else?

### Turn 66 (Human)
<system-reminder>
Plan mode is active. Do not make any edits or writes to the system.

## Plan File:
No plan written yet. Write your plan to C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md using the search_replace tool.

You should build your plan by writing to or editing this file. Note that this is the only file you are allowed to edit.

Your turn should only end with either ask_user_question to clarify requirements or exit_plan_mode to present your plan to the user.
</system-reminder>

### Turn 67 (Human)
<user_query>
pickup the handoff and plan completion
</user_query>

### Turn 68 (Assistant)
Picking up the handoff and drafting a completion plan. Gathering live branch state, open Xbox TODOs, and remaining PLAN-XBOXUWP work.
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\HANDOFF.md
- limit: 80
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$text = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.query' -ParamsObject @{ done = $false; limit = 100 } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String\n$blocks = [regex]::Matches($text, '(?ms)- id: ([A-Z0-9-]+)\\r?\\n\\s+title: ([^\\r\\n]+)\\r?\\n\\s+section: ([^\\r\\n]+)\\r?\\n\\s+priority: (\\w+)')\n$rows = foreach ($m in $blocks) { [pscustomobject]@{ Id=$m.Groups[1].Value; Title=$m.Groups[2].Value.Trim(); Section=$m.Groups[3].Value.Trim(); Priority=$m.Groups[4].Value } }\nWrite-Output '=== XBOX / UWP / KEYBOARD related open TODOs ==='\n$rows | Where-Object { $_.Id -match 'XBOX|XKBD|XMENU|XKEYBOARD|ROMM|UWP' -or $_.Title -match 'Xbox|UWP|keyboard|XBOX|virtual keyboard|RESTORE' } | Format-List Id,Priority,Section,Title | Out-String\nWrite-Output \"Total open: $($rows.Count)\""}
[tool_request: run_terminal_command]
- command: git -C "F:\GitHub\vice-sharp" log --oneline -15; Write-Output '---'; git -C "F:\GitHub\vice-sharp" status -sb; Write-Output '---'; git -C "F:\GitHub\vice-sharp" diff --stat HEAD
- description: Git history, status, and diff stats
[tool_request: grep]
- pattern: PLAN-XBOXUWP|FIX-X|FEAT-X|IMPL-XBOX
- path: F:\GitHub\vice-sharp
- glob: *.{md,yml,yaml,cs}
- head_limit: 80

### Turn 69 (Function)
[tool_response]
1→# ViceSharp Handoff - 2026-07-14

**Active branch:** `feat/xbox-uwp-app` (off `main`) at `a199063`. **PLAN-XBOXUWP** (the UWP-on-Xbox-console head) is deep in dev-PC bring-up; ~65 commits local, **NOT pushed** (Azure DevOps `origin` only, on explicit request). The full running detail lives in the auto-memory `project_xbox_uwp_app.md`; the snapshot below is the resume state.
**Parallel worktree (ACTIVE TASK 2026-07-14 23:31):** `F:\GitHub\vice-sharp-romless` on branch `fix/romless-vic-content` (off `ed0b67f`) processes `docs/romless-vic-badline-fix.md` (VIC-II raises zero bad lines for a KERNAL-less host-driven machine; bitmap renders black). Repro test already present in that worktree. MCP TODO `FIX-ROMLESSVIC-001` (high, Chips).
**Prior iteration-1 baseline:** `main`/`master` at `v1.0.2` (2026-07-08); **PLAN-VICEPARITY-001 COMPLETE** (466/466 SID parity ACs bit-exact vs reSID). Detail preserved in the dated sections below.
**feat/xbox-uwp-app working tree:** the `tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json` edit and untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md` are uncommitted scratch, not part of the plan.

## PLAN-XBOXUWP resume snapshot (2026-07-14)

10→- **Runs and renders the C64 on the dev PC.** The whole app (UWP + Windows.Gaming.Input + raw-DX11 SwapChainPanel + XAudio2) validates on Windows 11; the physical console is only needed at Store submission (slice S42, deferred). Autonomous on-device loop this session: build via VS MSBuild, `./build.ps1 DeployXboxLocal`, drive with computer-use, read receipts from `%LOCALAPPDATA%\Packages\sharpninja.ViceSharp.Xbox_wbxy578d68v3y\LocalState\vicesharp.log`.
- **Deploy loop:** `./build.ps1 DeployXboxLocal` (stop app -> VS MSBuild `/p:Configuration=Release-UWP /p:Platform=x64 /restore /t:Build,GenerateProjectPriFile` -> robocopy `/S` -no-`/XO`- over the registered AppX layout -> `shell:AppsFolder` launch). Dev toggles: **F9** virtual keyboard, **F11** menu (F10 is a Windows system key; ESC works but is eaten in some focus states; a CoreWindow.KeyDown backstop covers focus-nowhere). **End every CLI batch with a Debug-UWP restore** (`/p:Configuration=Debug-UWP /p:Platform=x64 /t:Restore`) or the operator's VS F5 fails NETSDK1005.
- **CRITICAL finding FIX-XKBDPANEL-001:** `PublishAot=true` at BUILD time flips CsWinRT into AOT binding mode and kills every reflection `{Binding}` app-wide (empty keyboard, blank Settings, no menu title) with no exception and no BindingFailed. AOT is now an explicit opt-in via `ViceSharpPublishAot=true`; Release-UWP builds stay JIT (SPD ~101%). **Store-AOT requires an AOT-safe-bindings slice (x:Bind / generated bindable metadata) BEFORE anyone re-arms PublishAot.**
- **Latest shipped (device-verified):** FIX-XKBDPANEL-001 (AOT opt-in + `/XO` Frankenbuild fix + F9/F11 + BindingFailed logging), FEAT-XMENUSUBPAGE-001 (B steps back on subpages / dismisses only on the home rail; subpages expand across both columns over an opaque backdrop and contract to the docked rail). Gate: **Category=Xbox 501/501**.
- **OPEN:** Controls-page keyboard-shortcut config (operator-requested, never started); B-on-controller verification (needs the operator's controller); FIX-XKBDNMI-001 (RESTORE -> CPU NMI edge unwired); per-model ROM-availability in the picker; S42 Tier-C console/Store (deferred); MCP `sessionlog.completeTurn` is flaky (complete turns directly, verify).

## How to resume

1. Read `AGENTS-README-FIRST.yaml` (marker rotates on server restart; verify signature + /health nonce before MCP work).
2. Route TODO / session-log / requirements / triage through the mcpserver plugin (repl-invoke or wrappers), never raw REST, never storage files.
20→3. Baseline gate (one process, ~20 min, run detached):
   `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`
   Green criterion: 0 failed, 21 skipped, total 2612-2615 (totals wobble in that band from cosmetic xunit trx name serialization of dynamic theory rows; per-class row sets are identical - never chase a fixed total).

## Shipped 2026-07-08 (all CI-confirmed)

- **v1.0.2 released**: tag on `534cded`, release run 1074, all 13 package ids verified in the nuget.org flat container (Core bundle, SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox; Console + Avalonia are dotnet tools).
- **PLAN-NATIVERESIDUE-001 closed** (`ba0f94f`): a `.vsf` whose DRIVE8 module carries `has_tde=0` disabled TrueDrive process-wide via `resources_set_int` (drive-snapshot.c:334-363); `vice_machine_create_model` now re-baselines `Drive{8..11}TrueEmulation` to the VICE default 1 through `resources_set_int` so iecbus statics and $DD00 callbacks recompute. SnapshotResume two-process partition removed; suite proven green in ONE process. Guards: TEST-NATIVE-RESIDUE-01/02 in `NativeResidueDiagTests`.
- **CI made self-sufficient** (`a229801`, `d8f61cf`, `4e3db9b`): agents have no ROMs and no shim, so `EnsureCiRomRoot` in `build/Build.cs` stages 13 hash-pinned VICE data files (C64 ROMs incl. all variant kernals, DRIVES dos1541/dos1541ii, gtk3_pos.vkm) from the VICE-Team svn-mirror into `artifacts/vice-data` and sets `VICESHARP_ROM_PATH` for the test process only. Shim-dependent tests MUST use `[ViceFact]`/`[ViceTheory]` (plain `[Fact]` throws DllNotFoundException on shimless agents). First-ever green CI run: 1072.
- **Docs reconciled to shipped reality** (`aee29ea`, 88 files): README, USER-GUIDE, VICE-MIGRATION, ROMs, Architecture, Public-API (regenerated from the 50 real Abstractions interfaces), requirements docs; 22 stale files pruned; `docs/wiki.yaml` manifest lists all 28 wiki documents; winget license fixed MIT -> GPL-2.0-or-later.
30→- **github divergence resolved** (`20b330c`): github-only commit `42f2f26` (VicModeChangeEvent seam) merged with strategy `ours` (tree unchanged; content preserved on `rescue/vic-modechange-event`); github main fast-forwarded, tags identical on both remotes; github wiki published per manifest (`b455b88`, 28 pages + sidebar).
- **Dependency wave S0-S8 complete** (TR-DEPS-202607-001 + TEST-DEPS-202607-001, both completed with evidence; commits `72696d9`, `055b9d5`, `26e5ca8`, `87eb80a`, `9214cdb`, `1cad822`+`1adf386`, `23d030a`, `0f5192f`): all 30 central entries at newest mutually compatible stables (Avalonia 12.0.5, Extensions/TestHost 10.0.9, Protobuf 3.35.1, Grpc.Tools 2.82.0, Test.Sdk 18.7.0, Roslyn pair 5.6.0, YamlDotNet 18.1.0, FluentAssertions 8.10.0, coverlet 10.0.1, aiUnit 2.1.3, RemoteControl 0.7.4); 5 dead pins removed; AiReview under CPM with inherited TreatWarningsAsErrors; vendored `nuget-local` feed DELETED (NuGet.config is nuget.org-only); `global.json` floor 10.0.301 (Roslyn 5.6 generator gate). Excluded as prerelease: xunit.v3 4.x, NSubstitute 6.x, Protobuf 4.x, Extensions 11.x.
- **Test fixtures in repo** (`93cdc7e`): `vice-snapshot-20260630171307.vsf` (read by the residue probes and lockstep re-baseline suites at repo root; do not delete), `native/.build-nopatch.sh`, `native/.ccwrap/*`. Operator rule: if it is needed to run a test, it goes in the repo.

## Iteration-1 completion (branch `fix/nativeresidue-002-drive-clock-hardening`, off `master`; NOT pushed)

Working through the "complete outstanding iteration 1 work" plan. Done, all committed locally, gates green:
- **Slice A - PLAN-NATIVERESIDUE-002** (710c1e9/8894f1a/9fb06dd/0691344): the two latent drive-clock bugs fixed; **BUG-LOCKSTEP-001 CLOSED** (full gate 0 failed / 2596 passed / 21 skipped). See docs/receipts-nativeresidue-002-2026-07-09.md.
- **Slice F - BUG-TESTDEBT-001**: verified already fixed at HEAD (6a32e7c/77e7190), closed with receipt.
- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.
40→
**SID parity program COMPLETE (2026-07-10)** - all remaining slices landed:
- **S11 / S11b**: 8580 reSID filter port (filter8580new m==1) + write pipeline + per-model ttl(0xA2000)/scaleFactor(5); shim exports vice_sid_exact_set_sampling + vice_sid_exact_clock_buffered; DATABUS-07 flipped; oracle via "c64c". Ratchet ->444.
- **S12**: amplify(scaleFactor)/clip PCM16 seam + extfilt enable branch; OUTPUT-01..07. Ratchet ->451.
- **S13**: fixed-point Kaiser FIR resampler (fast/interpolate/resample); OUTPUT-08..13. Ratchet ->457.
- **CH**: retired the dead Chamberlin SVF stack (10 members + 7 guards + SidFilter6581Tests.cs). PLAN-SIDCHAMBERLIN-001 closed.
- **BC**: ported reSID batched SID::clock(delta_t) (Sid6581.BatchedClock.cs); SAMPLE_FAST now value-bit-exact; un-pended the last 4 SID quarantines + authored the last 9 (EXTFILT-01..07, CLOCK-05/08). Ratchet ->466 with a strict completion pin (Assert.Equal(466, covered)).
- **LW** (4924b94, pushed): live audio wired through the reSID SAMPLE_RESAMPLE engine (Resample always, VICE x64sc parity); push tail bit-exact vs the buffered pull; warp = cadence-only; zero-alloc; benchmark in SidSamplingBenchmarks.
- **CL** (a38964b/e100433, local): NativeCollectionConventionTests + [Collection("NativeVice")] on the 6 native-bridge test classes.
- **G**: DROPPED per operator (ADO wiki push).
50→
Closure gates: parity 466/0/0 (completion pin holds), determinism 5/0, XmlDocs green, full baseline 0 NEW failures (7 pre-existing VideoRendererTests only) on the clean re-run. One run-1 full-suite native ACCESS_VIOLATION did NOT reproduce (flaky native-shim instability; candidate PLAN-NATIVERESIDUE-002 follow-up, not a SID blocker) - see the closure receipts.

**Native build recipe (learned this session, load-bearing):** `make x64sc-program` does NOT rebuild changed VICE-core `.o` (they stay stale). Before rebuilding, delete the changed `.o` (and `libdrive.a` for drive files), then run under the MSYS2 MINGW64 login shell so the compiler gets a writable `/tmp`:
`MSYSTEM=MINGW64 /c/msys64/usr/bin/bash.exe -lc 'rm -f <changed>.o; bash /f/GitHub/vice-sharp/native/.build-nopatch.sh'`. The dll is gitignored (built locally). Vendored edits go in native/patches/vice-shim-runtime.patch (regenerate via `git -C native/vice diff`; keep the 5 pre-existing hunks byte-identical).

**Oracle note:** SidExactRead (read path) is the reliable bus observable; SidExactGetState().BusValue export is NOT a dependable live-latch snapshot (returned stale/garbage) - compare via the read path + managed spec constants.

## In flight (interrupted mid-task)

60→Plugin reload + Agent Help (mcpserver core synced to 1.36.0, `mcpserver-repl` 1.4.5 confirmed on PATH):
1. Validate cache: `%USERPROFILE%\.claude\plugins\cache` has `mcpserver-local` (active family; confirm 1.36.0 from the plugin's own `.claude-plugin/plugin.json`), `mcpserver-cowork` (stale candidate - verify before deleting), `caveman` (unrelated, keep). The version inspection was interrupted by a transient permission-classifier outage; re-run it.
2. Run the claude-hook-validation skill; refresh MCP hooks if missing; restart Claude Code if hooks were installed.
3. Open a `workflow.agenthelp.createSession` (new in repl 1.4.5) and submit the outstanding MCP issues:
   - repl-invoke `Invoke-WorkflowAppendActions` audit counters never increment: regex `(?m)^\s*type:` misses `- type:` list items (repl-invoke.ps1:891-897; filed as triage-report-8a6539340a174c00a57dee53ec8f42ea).
   - No per-action readback: `workflow.sessionlog.queryHistory` returns only session headers, so appends cannot be content-verified; session header `lastUpdated`/`filesModifiedCount` never advance (anchored to `started`).
   - `Invoke-McpPlugin.ps1` defaults `-Command Status`: calling it with only `-Method` silently prints the status blob instead of invoking (footgun; cost one silent append failure).
   - `workflow.requirements.createTr` requires `subarea` (schema) but the skill docs do not mention it; the server also derived `subarea: 202607` from the id rather than honoring the supplied value.
4. Log the reload + help-session outcomes; triage any confirmed server defects.

70→## Parked items

- Azure DevOps wiki push needs `ADO_PAT` set, then `tools/Publish-Wiki.ps1 -Target azure` (github wiki already published).
- ~~Latent VICE bugs documented, not fixed~~ FIXED 2026-07-09 (PLAN-NATIVERESIDUE-002, branch `fix/nativeresidue-002-drive-clock-hardening`): drive `attach_clk`/`detach_clk`/`attach_detach_clk` uninitialized-stack read on `has_tde=0` restore (drive-snapshot.c zero-init) and `cycle_accum` omitted from `drivecpu_reset_clk` (drivecpu.c + drivecpu65c02.c) both fixed via the vendored runtime patch, plus a shim create-time drive-clock re-baseline. **BUG-LOCKSTEP-001 CLOSED**: full baseline gate 0 failed / 2596 passed / 21 skipped / 2617 total (was 136+2 lockstep failures). Receipts: docs/receipts-nativeresidue-002-2026-07-09.md. New residue candidates recorded there (live unit->type not re-baselined; drivecpu65c02 cycle_accum SMW/SMR width asymmetry). Build note: `make x64sc-program` does NOT rebuild changed VICE-core `.o`; delete the stale `.o`+`libdrive.a` and build under `MSYSTEM=MINGW64 bash -lc`.
- ~~VICE parity program (PLAN-VICEPARITY-001) remaining slices~~ **COMPLETE 2026-07-10** (SID side fully bit-exact vs reSID; VIC per-cycle work landed through V7 + audit phases). Only manifest-wide pending AC left is TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock conflict, out of SID scope, needs the parity owner). Candidate follow-up: PLAN-NATIVERESIDUE-002 native-shim lifecycle hardening (a sustained-consecutive-native-load ACCESS_VIOLATION observed once, non-reproducing).

## Open TODO backlog (MCP store)

PLAN-UIDOCK-001 (Dock.Avalonia UI), PLAN-AUDIOEQ-001, PLAN-PLAYLIST-001, PLAN-FULLSCREEN-001, PLAN-DEVCARDART-001, PLAN-MONFRAME-001, PLAN-DRIVE1581-001, PLAN-DRIVE1541II-001, PLAN-DRIVE1540-001, PLAN-DRIVE1571-001, PLAN-DRIVECMDHD-001, PLAN-CARTRAMLINK-001, PLAN-ARCHVIC20-001 (query the store for the live list; this snapshot is 2026-07-09).

80→## Operational gotchas (hard-won this session)

### Turn 70 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 38 matching lines
F:\GitHub\vice-sharp\azure-pipelines-xbox-store.yml
1:# FEAT-XSTOREPIPE-001 (PLAN-XBOXUWP S42): build the Xbox UWP head as a Microsoft Store

F:\GitHub\vice-sharp\HANDOFF.md
3:**Active branch:** `feat/xbox-uwp-app` (off `main`) at `a199063`. **PLAN-XBOXUWP** (the UWP-on-Xbox-console head) is deep in dev-PC bring-up; ~65 commits local, **NOT pushed** (Azure DevOps `origin` only, on explicit request). The full running detail lives in the auto-memory `project_xbox_uwp_app.md`; the snapshot below is the resume state.
8:## PLAN-XBOXUWP resume snapshot (2026-07-14)
12:- **CRITICAL finding FIX-XKBDPANEL-001:** `PublishAot=true` at BUILD time flips CsWinRT into AOT binding mode and kills every reflection `{Binding}` app-wide (empty keyboard, blank Settings, no menu title) with no exception and no BindingFailed. AOT is now an explicit opt-in via `ViceSharpPublishAot=true`; Release-UWP builds stay JIT (SPD ~101%). **Store-AOT requires an AOT-safe-bindings slice (x:Bind / generated bindable metadata) BEFORE anyone re-arms PublishAot.**
13:- **Latest shipped (device-verified):** FIX-XKBDPANEL-001 (AOT opt-in + `/XO` Frankenbuild fix + F9/F11 + BindingFailed logging), FEAT-XMENUSUBPAGE-001 (B steps back on subpages / dismisses only on the home rail; subpages expand across both columns over an opaque backdrop and contract to the docked rail). Gate: **Category=Xbox 501/501**.
14:- **OPEN:** Controls-page keyboard-shortcut config (operator-requested, never started); B-on-controller verification (needs the operator's controller); FIX-XKBDNMI-001 (RESTORE -> CPU NMI edge unwired); per-model ROM-availability in the picker; S42 Tier-C console/Store (deferred); MCP `sessionlog.completeTurn` is flaky (complete turns directly, verify).

F:\GitHub\vice-sharp\build\Build.cs
960:    /// FEAT-XLOCALDEPLOY-001: builds the Xbox UWP head and refreshes the locally
1086:    /// FEAT-XSTOREPIPE-001: local Microsoft Store certification pre-flight. Runs the
1866:    // ---- Xbox (UWP-on-console) head: PLAN-XBOXUWP S33 (IMPL-XBOXUWP-033) --------

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\VideoRenderer.cs
40:    /// the screen instead of carrying its in-frame black band (FIX-XNTSCFILL-001).
171:            // FIX-XSPRITEDUP-001: in LIVE rendering the PixelSequencer output read by

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\VicSnapshotState.cs
4:/// FIX-XSNAPWARP-001: the VIC-II state a machine snapshot round-trips, exactly the

F:\GitHub\vice-sharp\docs\xbox-store-publishing.md
3:FEAT-XSTOREPIPE-001 (PLAN-XBOXUWP S42). The pipeline `azure-pipelines-xbox-store.yml`

F:\GitHub\vice-sharp\docs\xbox\on-console-setup-runbook.md
9:2. Build the head with **VS MSBuild, not the dotnet CLI** (VERIFIED 2026-07-13, FIX-XWIN2D-001 - exit-0 clean on the current build host):
32:1. Install Visual Studio 2026 (VS 18) with the UWP / Windows App workload. This is what supplies the UWP XAML markup compiler (genxbf) and the ModernNET XAML build targets. FINDING (2026-07-13, FIX-XWIN2D-001): the `UseUwp` SDK feature alone (via `dotnet`) wires only the WinRT projections, NOT the XAML markup compiler; only VS MSBuild imports it (through its `ImportBefore`/`ImportAfter` hooks). VS is therefore required for the head build - it is not a separately NuGet-restorable component, and it IS present with VS 18 Community.
49:S0 proves three things jointly before any production slice is accepted (plan section 2): (a) a Native-AOT UWP MSIX links clean and runs on the console, (b) the trimmed managed core links clean, (c) the DX11 video path + Native-AOT packaging is clean. STATUS (2026-07-13): (b) is PROVEN off-console (the trimmed core AOT-links with 0 IL warnings + a native exe, plan S5/S18/S35); (c)'s Win2D hazard is ELIMINATED (FIX-XWIN2D-001 replaced Win2D with raw DX11 via LibraryImport + vtable calls, so there is no winmd/NuGet AOT hazard left); (a) is the remaining unknown below.
69:- [ ] The DX11 composition swap chain composes and presents on-device (FIX-XWIN2D-001 made raw Direct3D 11 the only video path; Win2D is removed).

F:\GitHub\vice-sharp\docs\xbox\dev-pc-validation-guide.md
83:- [ ] **RESTORE** (via the virtual keyboard tile) -> fires the C64 **NMI**. NOTE: RESTORE->NMI-edge wiring is tracked as an open follow-up (FIX-XKBDNMI-001); if RESTORE does nothing, that is the known gap - flag it and I will wire it.
98:1. **Build with VS MSBuild, not `dotnet`.** `dotnet build -p:ViceSharpXboxUwp=true` fails with `CS0103 InitializeComponent` / `CS5001 no Main` because the dotnet SDK's MSBuild does not import the UWP XAML markup compiler; VS MSBuild does. This is a host-selection matter, not a missing component (verified 2026-07-13, FIX-XWIN2D-001). The `net10.0` fallback (no `-p:ViceSharpXboxUwp=true`) still builds fine under `dotnet` - that is CI's path.
100:3. **RESTORE key (S39)** may not fire the NMI yet - open follow-up FIX-XKBDNMI-001. Flag if so.
102:5. **Video is raw Direct3D 11** now (Win2D removed, FIX-XWIN2D-001). A machine with no usable GPU/DXGI degrades to a silent no-blit surface rather than crashing - if you see a black emulator view but audio runs, suspect the GPU/DXGI path and report it.

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\Mos6569.State.cs
116:    /// (FIX-XSNAPWARP-001: the in-app machine snapshot): the 64-byte register file,

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cia\Mos6526.cs
230:    /// (FIX-XSNAPWARP-001: the in-app machine snapshot). The control registers are

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cia\CiaSnapshotState.cs
4:/// FIX-XSNAPWARP-001: the CIA state a machine snapshot round-trips, exactly the fields

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\AboutViewModel.cs
6:/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI, TEST-XBOXUI-008. The About page

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\AboutInfo.cs
4:/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI. The SINGLE home for the fixed

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
13:/// PLAN-XBOXUWP S26 (IMPL-XBOXUWP-026), area XSET / XMVVM. FR-XSET-001..005,
60:    // UI-thread dispatch (FIX-XSETBLANK-001): the refresh continuations resume on the
715:        // XAML bindings always hear the change (FIX-XSETBLANK-001).

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsStore.cs
10:/// FEAT-XSETPERSIST-001 (PLAN-XBOXUWP, area XBOXSET). Persists the host-CANONICAL

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxRomProvisioningViewModel.cs
13:/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-001/002, TR-XPATH-001, TR-MVVM-001.

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxMediaSlotViewModel.cs
12:/// PLAN-XBOXUWP S27 (IMPL-XBOXUWP-027), area XDEV / XMVVM. FR-XDEV-001..003,

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxDeviceSetupViewModel.cs
15:/// PLAN-XBOXUWP S27 (IMPL-XBOXUWP-027), area XDEV / XMVVM. FR-XDEV-001..003,

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\XboxSystemButtons.cs
14:/// (PLAN-XBOXUWP S10, IMPL-XBOXUWP-010). The evaluator is deterministic and holds

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\VirtualKeyEntry.cs
4:/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025) + PLAN-XKEYBOARD-001 K1, area XBOXUI/XKBD. One
35:/// FEAT-XKEYCAPSHIFT-001: the PRINTABLE character this key inserts while SHIFT is

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\XboxJoystickMapper.cs
48:/// FR-GAMEPAD-006 / FR-GAMEPAD-007 / TR-GAMEPAD-003 (PLAN-XBOXUWP S9,
49:/// IMPL-XBOXUWP-009). LOCKED mapping: the <b>left stick + D-pad</b> and the

</workspace_result>

### Turn 71 (Function)
[tool_response]
exit: 0
a199063 feat(xbox): B steps back on menu subpages; subpages expand to the full window [PLAN-XBOXUWP FEAT-XMENUSUBPAGE-001]
8ec2137 feat(xbox): F9/F11 shell toggles, keyboard-panel receipts, BindingFailed logging [PLAN-XBOXUWP FIX-XKBDPANEL-001]
1335582 fix(build): DeployXboxLocal robocopy drops the timestamp filter [PLAN-XBOXUWP FIX-XKBDPANEL-001]
3626e2f fix(xbox): PublishAot is an explicit opt-in; build-time AOT killed every {Binding} [PLAN-XBOXUWP FIX-XKBDPANEL-001]
9ceef6b feat(xbox): menu DOCKS in a right column and shrinks the emulator [PLAN-XBOXUWP FEAT-XMENUSLIDE-001]
ed0b67f feat(xbox): menu slides in from the right; single-navigator dpad throttle [PLAN-XBOXUWP FEAT-XMENUSLIDE-001 FIX-XDPADSKIP-002]
6050107 feat(xbox): authentic two-line RUN/STOP and SHIFT LOCK keycaps; chords show RUN [PLAN-XBOXUWP FEAT-XKBDSTICKY-001]
a00f9b0 fix(host): v2 machine snapshot: true chip state via lockstep injectors [PLAN-XBOXUWP FIX-XSNAPWARP-001]
95153d0 feat(xbox): sticky C=/SHIFT modifiers, real-time-scanned virtual keyboard [PLAN-XBOXUWP FEAT-XKBDSTICKY-001]
4f7d974 feat(xbox): Close Menu first + focused on open; breadbin keycap style on virtual keyboard [PLAN-XBOXUWP FEAT-XMENUFOCUS-001 FEAT-XKEYCAPSTYLE-001]
0480237 fix(host): paused sessions accrue no pacing debt: no warp sprint on resume [PLAN-XBOXUWP FIX-XMENUWARP-001]
b68fde6 feat(xbox): true PETSCII graphics on virtual keycaps for SHIFT and C= chords [PLAN-XBOXUWP FEAT-XKEYCAPPETSCII-001]
b99e306 fix(build): DeployXboxLocal restores via /restore switch, not a Restore entry target [PLAN-XBOXUWP]
26b7a4b fix(xbox): settings pickers blank: marshal PropertyChanged to the UI thread [PLAN-XBOXUWP FIX-XSETBLANK-001]
fb06543 fix(build): DeployXboxLocal regenerates resources.pri: XAML changes shipped stale UI [FEAT-XLOCALDEPLOY-001]
---
## feat/xbox-uwp-app
 M HANDOFF.md
 M azure-pipelines.ci.yml
 M azure-pipelines.release.yml
 M src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
 M src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
 M src/ViceSharp.Xbox/Views/AboutPage.xaml
 M src/ViceSharp.Xbox/Views/AboutPage.xaml.cs
 M src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml
 M src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml.cs
 M src/ViceSharp.Xbox/Views/InputMappingPage.xaml
 M src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
 M src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml
 M src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml.cs
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 M tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json
?? docs/S-Blox/
?? docs/reviews/code-review-20260714T001056552Z.md
?? docs/reviews/code-review-20260714T004256563Z.md
?? docs/romless-vic-badline-fix.md
---
 HANDOFF.md                                         | 19 +++++++++++-----
 azure-pipelines.ci.yml                             | 26 ++++++++++++++++++++++
 azure-pipelines.release.yml                        | 26 ++++++++++++++++++++++
 .../Controls/VirtualKeyboardOverlay.xaml           | 15 +++++++------
 .../Controls/VirtualKeyboardOverlay.xaml.cs        | 15 +++++++++++++
 src/ViceSharp.Xbox/Views/AboutPage.xaml            | 12 +++++-----
 src/ViceSharp.Xbox/Views/AboutPage.xaml.cs         |  7 +++++-
 src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml      | 13 ++++++-----
 src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml.cs   |  6 +++--
 src/ViceSharp.Xbox/Views/InputMappingPage.xaml     |  9 ++++----
 src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs  |  7 +++++-
 src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml  |  4 ++--
 .../Views/RomProvisioningPage.xaml.cs              |  9 ++++----
 src/ViceSharp.Xbox/Views/SettingsPage.xaml         | 20 ++++++++---------
 src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs      |  6 +++--
 .../appsettings.aiunit.json                        |  6 ++---
 16 files changed, 145 insertions(+), 55 deletions(-)


### Turn 72 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $text = & 'F:\GitHub\mcpser... | Duration: 2.38s | Location [FileSystem]: F:\GitHub\vice-sharp

=== XBOX / UWP / KEYBOARD related open TODOs ===

Id       : IMPL-ROMM-001
Priority : high
Section  : romm
Title    : '[P0] Pin RomM packages + restore (AC-PKG-04, TR-ROMM-NUGET-001)'

Id       : IMPL-ROMM-002
Priority : high
Section  : romm
Title    : '[L1] ViceSharp.Library.ViewModels seams + slug map (AC-PKG-03, AC-LAUNCH-02/03, AC-BROWSE-08/02)'

Id       : IMPL-ROMM-003
Priority : high
Section  : romm
Title    : '[L2] ViceSharp.RomM adapter browse/detail/download + RomMJsonContext (AC-BROWSE-01/02/03, AC-DETAIL-01, AC-LAUNCH-01, AC-CONN-01/06)'

Id       : IMPL-ROMM-004
Priority : high
Section  : romm
Title    : '[L3] LibraryBrowseViewModel (AC-BROWSE-04/05/06/07, AC-LAUNCH-03/04/07)'

Id       : PLAN-ROMM-001
Priority : high
Section  : romm
Title    : Create a RomM client library and UI for Avalonia and Xbox

Id       : IMPL-ROMM-005
Priority : medium
Section  : romm
Title    : '[L4] Launch orchestration seam (AC-LAUNCH-04/07)'

Id       : IMPL-ROMM-006
Priority : medium
Section  : romm
Title    : '[L5] Collections (AC-COLLECT-01..06, AC-DETAIL-02)'

Id       : IMPL-ROMM-007
Priority : medium
Section  : romm
Title    : '[L6] CSDb gateways (AC-CSDB-01..06)'

Id       : IMPL-ROMM-008
Priority : medium
Section  : romm
Title    : '[L7] Covers + connection + pairing + refresh (AC-COVER-01..04, AC-CONN-02/03/04/05)'

Id       : IMPL-ROMM-009
Priority : medium
Section  : romm
Title    : '[L8] Packaging gate (AC-PKG-01/02)'

Id       : IMPL-ROMM-010
Priority : medium
Section  : romm
Title    : '[X1] XboxGameLauncher (AC-LAUNCH-05)'

Id       : IMPL-ROMM-011
Priority : medium
Section  : romm
Title    : '[X2] XboxCoverImageLoader + XboxRomMConnectionStore'

Id       : IMPL-ROMM-012
Priority : medium
Section  : romm
Title    : '[X3] LibraryPage + GameDetailsPage + ListsPage + nav + pairing (AC-XUI-01..06)'

Id       : IMPL-ROMM-014
Priority : medium
Section  : romm
Title    : '[A1] AvaloniaGameLauncher (AC-LAUNCH-06)'

Id       : IMPL-ROMM-015
Priority : medium
Section  : romm
Title    : '[A2] AvaloniaCoverImageLoader + desktop store + gateway selection (AC-CSDB-06)'

Id       : IMPL-ROMM-016
Priority : medium
Section  : romm
Title    : '[A3] LibraryView + GameDetailsView + ListsView + MainWindow + connection dialog (AC-AUI-01..04)'

Id       : IMPL-ROMM-013
Priority : low
Section  : romm
Title    : '[X4] Xbox CSDb tab (reuses L6 VM)'

Id       : IMPL-ROMM-017
Priority : low
Section  : romm
Title    : '[A4] Avalonia CSDb tab'

Id       : BUG-DRVTRUE-001
Priority : high
Section  : True Drive
Title    : Snapshot C64 state on True Drive toggle and restore after reset

Id       : PLAN-XBOXCLIENT-001
Priority : high
Section  : UI
Title    : XBOX-compatible client (10-foot console front-end)

Id       : PLAN-DEVCARDART-001
Priority : medium
Section  : UI
Title    : XAML vector art backgrounds for device cards (drives, datasette, cartridge, keyboards)

Id       : IMPL-XBOXUWP-000
Priority : high
Section  : xbox-uwp
Title    : 'S0: FEASIBILITY GO/NO-GO (the one gate all production slices depend on)'

Id       : PLAN-XKEYBOARD-001
Priority : high
Section  : xbox-uwp
Title    : UWP Virtual Keyboard should be a real C64 keyboard

Id       : FIX-XKBDNMI-001
Priority : medium
Section  : xbox-uwp
Title    : Wire C64 RESTORE trigger (IsRestorePressed) to the CPU NMI edge

Id       : IMPL-XBOXUWP-035
Priority : medium
Section  : xbox-uwp
Title    : 'S35: Desktop-intact + full-suite + AOT regression guard'

Id       : IMPL-XBOXUWP-036
Priority : medium
Section  : xbox-uwp
Title    : 'S36: On-console deploy + boot-to-menu + C64 render smoke'

Id       : IMPL-XBOXUWP-037
Priority : medium
Section  : xbox-uwp
Title    : 'S37: On-console live gamepad smoke (JOY2/JOY1 + A/B fire + menu neutralization + swap)'

Id       : IMPL-XBOXUWP-038
Priority : medium
Section  : xbox-uwp
Title    : 'S38: On-console A/V smoke (Win2D video + XAudio2 audio + mute + suspend/resume)'

Id       : IMPL-XBOXUWP-039
Priority : medium
Section  : xbox-uwp
Title    : 'S39: On-console system-button + context smoke'

Id       : IMPL-XBOXUWP-040
Priority : medium
Section  : xbox-uwp
Title    : 'S40: On-console first-run ROM provisioning + settings persistence + device attach E2E'

Id       : IMPL-XBOXUWP-041
Priority : medium
Section  : xbox-uwp
Title    : 'S41: On-console performance within resource budget (PAL 50)'

Id       : IMPL-XBOXUWP-042
Priority : low
Section  : xbox-uwp
Title    : 'S42: Console / Store-submission validation (deferred: console deploy, on-console resource budget, TV device-family behaviors, Store cert)'


Total open: 61

### Turn 73 (Assistant)
[tool_request: run_terminal_command]
- command: git -C "F:\GitHub\vice-sharp" diff -- "src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs" "src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs" "src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs" "HANDOFF.md" | Select-Object -First 250
- description: Inspect key uncommitted Xbox diffs
[tool_request: grep]
- pattern: IsRestorePressed|RESTORE|NMI|FIX-XKBDNMI|keyboard.?shortcut|shortcut
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox
- glob: *.{cs,xaml}
- head_limit: 60
[tool_request: grep]
- pattern: FIX-XKBDNMI|keyboard-shortcut|Controls-page|ROM-availability|per-model
- path: F:\GitHub\vice-sharp
- glob: *.{md,cs}
- head_limit: 40
[tool_request: list_dir]
- target_directory: F:\GitHub\vice-sharp\docs\xbox
[tool_request: run_terminal_command]
- command: Get-ChildItem -Path "C:\Users\kingd\.claude\projects" -Recurse -Filter "*xbox*" -ErrorAction SilentlyContinue | Select-Object -First 20 FullName; Get-ChildItem -Path "C:\Users\kingd\.claude" -Recurse -Filter "project_xbox*" -ErrorAction SilentlyContinue | Select-Object -First 10 FullName; Get-ChildItem -Path "F:\GitHub\vice-sharp" -Recurse -Filter "*xbox*uwp*" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch 'bin|obj|\\native\\' } | Select-Object -First 25 FullName
- description: Locate Xbox UWP plan/memory artifacts

### Turn 74 (Function)
[tool_response]
- F:\GitHub\vice-sharp\docs\xbox/
  - dev-pc-validation-guide.md
  - on-console-setup-runbook.md

### Turn 75 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Controls\VirtualKeyboardOverlay.xaml.cs
14:/// <see cref="VirtualKeyboardViewModel"/>, which injects the keystroke (or RESTORE /
</workspace_result>

### Turn 76 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 24 matching lines
F:\GitHub\vice-sharp\HANDOFF.md
14:- **OPEN:** Controls-page keyboard-shortcut config (operator-requested, never started); B-on-controller verification (needs the operator's controller); FIX-XKBDNMI-001 (RESTORE -> CPU NMI edge unwired); per-model ROM-availability in the picker; S42 Tier-C console/Store (deferred); MCP `sessionlog.completeTurn` is flaky (complete turns directly, verify).
39:- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.
42:- **S11 / S11b**: 8580 reSID filter port (filter8580new m==1) + write pipeline + per-model ttl(0xA2000)/scaleFactor(5); shim exports vice_sid_exact_set_sampling + vice_sid_exact_clock_buffered; DATABUS-07 flipped; oracle via "c64c". Ratchet ->444.

F:\GitHub\vice-sharp\prompt.md
9:The expensive per-cycle sprite DMA mapping logic added for correctness (TR-VIC-EDGE-004, non-PAL per-model tables from VICE vicii-chip-model.c) became the dominant hotspot in dotTrace CPU sampling on the running Avalonia emulator in warp mode at BASIC READY.
50:- Full expensive VICE table math (ComputeIsInSpriteDmaStallWindow calling MapCurrentCycleToRasterX + delta -3..+1 + cpl wrap using the per-model tables) still runs exactly once per cycle in the latch path.
57:- vicii-fetch.c:275-309 (sprite pointer + data fetch side effects driven by the per-model DMA windows).

F:\GitHub\vice-sharp\docs\audit-vicii-vs-vice-2026-07-06.md
11:| Phase 5 (NTSC/models) | `27239fa`, `1f09073` | NTSC model timing, per-model xpos/DMA tables, 8565 g-fetch, 520-byte dbuf; sprite beam position (H6) |
12:| Phase 6 (low-severity sweep) | `6d17ef2`, `c640bc6`, `fd4d938`, `fd084cb` | L6 (ECM $39FF g-address mask), L10 (snapshot resume of vc/vcbase/rc/vmli/refresh/spriteDma), M15+L7+L8+L9 (per-model palettes via VICE color pipeline), L5+L4 (light-pen wiring and timing) |
330:- **Root cause:** The port baked a static approximate RGB palette instead of porting VICE's per-model YUV tables plus the YUV->RGB generation pipeline.
336:#### L7. No per-model palette selection; a single static table used for all models
384:- **What VICE does:** `x = cycle_get_xpos(cycle_table[raster_cycle]) / 2` from a per-model table. NTSC starts cycle-1 Phi1 xpos at 0x19c (not 0x194) and the 65-cycle table stores a duplicated 0x184 at cycles 62 and 63; `cycle_get_xpos` masks low 3 bits.
385:- **What managed does:** `phi1Xpos = (0x194 + 8*RasterX) % 0x1F8`, base and wrap hardcoded regardless of model; no per-model table.
388:- **Fix:** Port VICE's per-model `cycle_table` xpos, keying on chip model / `_cyclesPerLine`. A base-only fix is insufficient: the wrap modulus differs (PAL 0x1F8, NTSC 0x200) and the NTSC 62/63 xpos stall (0x184 held) must be reproduced. Build a precomputed per-model Phi1-xpos table indexed by raster_cycle with the `&~7` rounding. The sprite pipeline (`PixelSequencer.cs:838`) hardcodes the same PAL formula and needs the same table.
464:13. **L3 per-model light-pen xpos table** (shared with the sprite xpos table from M11).
468:14. **M15 + L7 + L8 + L9 palette pipeline** (port VICE's YUV tables + `video_color_palette_internal` + per-model selection; regenerate all 16 entries). Fix as one unit in the display layer; do not touch core index output.
480:- **Phi1 fetch dispatch: clean.** `ReadVicPhi1Pal/NtscOld/Ntsc` faithfully transcribe VICE's per-model `cycle_tab_*` (p/s/refresh/g/idle slots correct for all three models); the g-access address generator, refresh counter, and sprite-pointer addressing match. All confirmed fetch drifts are on the Phi2 side folded into the dispatch.

F:\GitHub\vice-sharp\docs\plan.md
189:  per-model sprite DMA timing.
384:- `TR-VIC-EDGE-004`: remaining sprite DMA depth covers non-PAL per-model

F:\GitHub\vice-sharp\docs\plans\perf-sprite-dma-optimization-plan-001.md
17:- Dominant hotspots after the TR-VIC-EDGE-004 correctness work (non-PAL per-model sprite DMA tables + cpl-aware MapCurrentCycleToRasterX + IsInSpriteDmaStallWindow):
36:- vicii-fetch.c:275-309 (sprite pointer + data fetch side effects driven exactly by the per-model DMA windows).

F:\GitHub\vice-sharp\docs\xbox\dev-pc-validation-guide.md
83:- [ ] **RESTORE** (via the virtual keyboard tile) -> fires the C64 **NMI**. NOTE: RESTORE->NMI-edge wiring is tracked as an open follow-up (FIX-XKBDNMI-001); if RESTORE does nothing, that is the known gap - flag it and I will wire it.
100:3. **RESTORE key (S39)** may not fire the NMI yet - open follow-up FIX-XKBDNMI-001. Flag if so.

F:\GitHub\vice-sharp\docs\VICE-MIGRATION.md
111:| VIC-II sprite-DMA timing | Bounded. Sprite fetch is wired and side-border visibility is managed, but non-PAL per-model fetch tables and native multiplexing checkpoints remain. |

F:\GitHub\vice-sharp\docs\Project\wiki\github\VICE-Migration.md
111:| VIC-II sprite-DMA timing | Bounded. Sprite fetch is wired and side-border visibility is managed, but non-PAL per-model fetch tables and native multiplexing checkpoints remain. |

</workspace_result>

### Turn 77 (Function)
[tool_response]
exit: 0
diff --git a/HANDOFF.md b/HANDOFF.md
index 25dcc07..e49050b 100644
--- a/HANDOFF.md
+++ b/HANDOFF.md
@@ -1,10 +1,17 @@
-# ViceSharp Handoff - 2026-07-10
+# ViceSharp Handoff - 2026-07-14
 
-**Branch:** `fix/nativeresidue-002-drive-clock-hardening` (off `master`; pushed to origin through the LW slice; CL closure commits local). The SID/VIC VICE-fidelity program **PLAN-VICEPARITY-001 is COMPLETE**: 466/466 parity ACs authored and passing, 0 pending SID ACs, every SID subsystem lockstep-verified bit-exact vs the native reSID oracle. See docs/receipts-viceparity-closure-2026-07-10.md.
-**Prior baseline branch:** `main` at `93cdc7e` (origin and github both synced; default branch is `master`)
-**Working tree:** clean
-**Latest release:** `v1.0.2` (2026-07-08), 13/13 NuGet packages verified on nuget.org
-**CI:** green streak on VICE-Sharp-CI runs 1072 and 1086-1096 (self-hosted Default pool)
+**Active branch:** `feat/xbox-uwp-app` (off `main`) at `a199063`. **PLAN-XBOXUWP** (the UWP-on-Xbox-console head) is deep in dev-PC bring-up; ~65 commits local, **NOT pushed** (Azure DevOps `origin` only, on explicit request). The full running detail lives in the auto-memory `project_xbox_uwp_app.md`; the snapshot below is the resume state.
+**Parallel worktree (ACTIVE TASK 2026-07-14 23:31):** `F:\GitHub\vice-sharp-romless` on branch `fix/romless-vic-content` (off `ed0b67f`) processes `docs/romless-vic-badline-fix.md` (VIC-II raises zero bad lines for a KERNAL-less host-driven machine; bitmap renders black). Repro test already present in that worktree. MCP TODO `FIX-ROMLESSVIC-001` (high, Chips).
+**Prior iteration-1 baseline:** `main`/`master` at `v1.0.2` (2026-07-08); **PLAN-VICEPARITY-001 COMPLETE** (466/466 SID parity ACs bit-exact vs reSID). Detail preserved in the dated sections below.
+**feat/xbox-uwp-app working tree:** the `tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json` edit and untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md` are uncommitted scratch, not part of the plan.
+
+## PLAN-XBOXUWP resume snapshot (2026-07-14)
+
+- **Runs and renders the C64 on the dev PC.** The whole app (UWP + Windows.Gaming.Input + raw-DX11 SwapChainPanel + XAudio2) validates on Windows 11; the physical console is only needed at Store submission (slice S42, deferred). Autonomous on-device loop this session: build via VS MSBuild, `./build.ps1 DeployXboxLocal`, drive with computer-use, read receipts from `%LOCALAPPDATA%\Packages\sharpninja.ViceSharp.Xbox_wbxy578d68v3y\LocalState\vicesharp.log`.
+- **Deploy loop:** `./build.ps1 DeployXboxLocal` (stop app -> VS MSBuild `/p:Configuration=Release-UWP /p:Platform=x64 /restore /t:Build,GenerateProjectPriFile` -> robocopy `/S` -no-`/XO`- over the registered AppX layout -> `shell:AppsFolder` launch). Dev toggles: **F9** virtual keyboard, **F11** menu (F10 is a Windows system key; ESC works but is eaten in some focus states; a CoreWindow.KeyDown backstop covers focus-nowhere). **End every CLI batch with a Debug-UWP restore** (`/p:Configuration=Debug-UWP /p:Platform=x64 /t:Restore`) or the operator's VS F5 fails NETSDK1005.
+- **CRITICAL finding FIX-XKBDPANEL-001:** `PublishAot=true` at BUILD time flips CsWinRT into AOT binding mode and kills every reflection `{Binding}` app-wide (empty keyboard, blank Settings, no menu title) with no exception and no BindingFailed. AOT is now an explicit opt-in via `ViceSharpPublishAot=true`; Release-UWP builds stay JIT (SPD ~101%). **Store-AOT requires an AOT-safe-bindings slice (x:Bind / generated bindable metadata) BEFORE anyone re-arms PublishAot.**
+- **Latest shipped (device-verified):** FIX-XKBDPANEL-001 (AOT opt-in + `/XO` Frankenbuild fix + F9/F11 + BindingFailed logging), FEAT-XMENUSUBPAGE-001 (B steps back on subpages / dismisses only on the home rail; subpages expand across both columns over an opaque backdrop and contract to the docked rail). Gate: **Category=Xbox 501/501**.
+- **OPEN:** Controls-page keyboard-shortcut config (operator-requested, never started); B-on-controller verification (needs the operator's controller); FIX-XKBDNMI-001 (RESTORE -> CPU NMI edge unwired); per-model ROM-availability in the picker; S42 Tier-C console/Store (deferred); MCP `sessionlog.completeTurn` is flaky (complete turns directly, verify).
 
 ## How to resume
 
diff --git a/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs b/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
index a752d8f..30df4e5 100644
--- a/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
+++ b/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
@@ -29,11 +29,26 @@ public sealed partial class VirtualKeyboardOverlay : UserControl
     private bool _appliedCommodore;
     private bool _appliedLowercase;
 
+    /// <summary>
+    /// The bound <see cref="VirtualKeyboardViewModel"/> (the control's
+    /// <see cref="FrameworkElement.DataContext"/>), projected as the compiled-binding
+    /// ({x:Bind}) source root. The host assigns the DataContext externally (App startup and
+    /// the machine-rebuild path); this exposes it so the XAML resolves <c>ViewModel.*</c>
+    /// paths, and <see cref="FrameworkElement.DataContextChanged"/> re-runs the compiled
+    /// bindings whenever the DataContext is (re)assigned.
+    /// </summary>
+    public VirtualKeyboardViewModel? ViewModel => DataContext as VirtualKeyboardViewModel;
+
     /// <summary>Creates the overlay, its charset-case poll, and the stroke-hold timer.</summary>
     public VirtualKeyboardOverlay()
     {
         InitializeComponent();
 
+        // The DataContext (the VirtualKeyboardViewModel) is assigned by the host AFTER
+        // construction and again on a machine rebuild; keep the compiled {x:Bind} paths
+        // (rooted at ViewModel => DataContext) in sync each time it changes.
+        DataContextChanged += (_, _) => Bindings.Update();
+
         // The charset mode flips at RUNTIME (SHIFT+C= / POKE 53272), so poll the live
         // VIC at ~4 Hz while the dock is on screen; the apply path no-ops when nothing
         // changed, so the steady-state cost is one facade read.
diff --git a/src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs b/src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
index fc49633..d3301a4 100644
--- a/src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
+++ b/src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
@@ -4,15 +4,20 @@ namespace ViceSharp.Xbox.Views;
 
 using Windows.UI.Xaml;
 using Windows.UI.Xaml.Controls;
+using ViceSharp.Xbox.ViewModels;
 
 /// <summary>The read-only controller-mapping page. Its Rows come from InputMappingViewModel.</summary>
 public sealed partial class InputMappingPage : Page
 {
+    /// <summary>The shared input-mapping ViewModel, bound by compiled {x:Bind}.</summary>
+    public InputMappingViewModel ViewModel { get; }
+
     /// <summary>Creates the page and binds the shared InputMappingViewModel.</summary>
     public InputMappingPage()
     {
+        ViewModel = App.Instance.InputMappingVm;
+        DataContext = ViewModel;
         InitializeComponent();
-        DataContext = App.Instance.InputMappingVm;
     }
 
     private void OnVirtualKeyboard(object sender, RoutedEventArgs e)
diff --git a/src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs b/src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
index 4fc0b6a..eba5c3a 100644
--- a/src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
+++ b/src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
@@ -14,11 +14,13 @@ public sealed partial class SettingsPage : Page
     /// <summary>Creates the page and binds the shared XboxSettingsViewModel.</summary>
     public SettingsPage()
     {
+        ViewModel = App.Instance.SettingsVm;
+        DataContext = ViewModel;
         InitializeComponent();
-        DataContext = App.Instance.SettingsVm;
     }
 
-    private XboxSettingsViewModel? ViewModel => App.Instance.SettingsVm;
+    /// <summary>The bound settings ViewModel (the shared <see cref="App.SettingsVm"/>), the source for compiled {x:Bind} paths.</summary>
+    public XboxSettingsViewModel? ViewModel { get; }
 
     /// <inheritdoc />
     protected override async void OnNavigatedTo(NavigationEventArgs e)


### Turn 78 (Function)
[tool_response]
exit: 0

FullName
--------
C:\Users\kingd\.claude\projects\F--GitHub-RiskyStars\f441eb8d-f203-4ed9-b91f-63d781a1109e\workflows\scripts\xbox-input.
C:\Users\kingd\.claude\projects\F--GitHub-RiskyStars\memory\xbox-plan-progress.md
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-app-p.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-app-r.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-debug.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-emula.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-first.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-menu-.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\0aad25e1-e13c-4add-b10f-4a71e775e9f1\workflows\scripts\xbox-uwp-s.
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\memory\project_xbox_uwp_app.md
C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\memory\project_xbox_uwp_app.md
F:\GitHub\vice-sharp\docs\plans\PLAN-XBOXUWP-BDPv4.md
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostShells\XboxUwpHeadShellTests.cs
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostShells\XboxUwpHeadTests.cs



### Turn 79 (Assistant)
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\memory\project_xbox_uwp_app.md
- limit: 200
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\plans\PLAN-XBOXUWP-BDPv4.md
- limit: 150
[tool_request: grep]
- pattern: ^## |OPEN|DONE|remaining|S2[5-9]|S3[0-9]|S4[0-2]|completion|shortcut|NMI|ROM
- path: F:\GitHub\vice-sharp\docs\plans\PLAN-XBOXUWP-BDPv4.md
- head_limit: 80
[tool_request: grep]
- pattern: IsRestorePressed|Restore|RequestNmi|TriggerNmi|Nmi
- path: F:\GitHub\vice-sharp\src
- glob: *.cs
- head_limit: 50
[tool_request: run_terminal_command]
- command: git -C "F:\GitHub\vice-sharp" diff -- "src/ViceSharp.Xbox/Views/SettingsPage.xaml" "src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml" | Select-Object -First 120; Write-Output '--- pipeline ---'; git -C "F:\GitHub\vice-sharp" diff -- "azure-pipelines.ci.yml" | Select-Object -First 80
- description: Inspect XAML binding and pipeline diffs

### Turn 80 (Function)
[tool_response]
1→# ViceSharp Xbox UWP head: consolidated BDP v4 implementation plan

This plan fuses nine subsystem designs plus a completeness critique into one decision-complete, contradiction-free Byrd Development Process (BDP v4) plan for a NEW true UWP-on-Xbox-console head for ViceSharp (a C#/.NET 10 port of VICE, GPL-2.0-or-later). It applies the authoritative reconciliation rulings R1-R13 verbatim: any place a source section disagreed with a ruling, the ruling wins here. The head is additive: it reuses the managed core (Abstractions/Core/Chips/Architectures) and the extracted Kestrel-free host, renders with SwapChainPanel + Win2D, decouples audio through a lock-free ring buffer, drives one managed pacing gate, and is controller-only. The existing desktop build (Avalonia + WiX JIT/R2R) and the existing `src/ViceSharp.Host.Xbox` Avalonia scaffold are left byte-for-byte intact. Delivery is tests-first, requirements-driven, in small gated slices, each gate 100% green (0 failed AND 0 skipped in that slice's filter scope), determinism preserved throughout.

## 0. Decisions locked at approval (2026-07-13)

The operator approved this plan and locked these answers:
- APPROVED to begin implementation under BDP, starting at S0. Register FR/TR/TEST in MCP, commit this plan to docs/, create slice TODOs, then execute slice by slice.
- ROM acquisition PRIMARY: verified HTTPS download from the VICE ROM mirror on GitHub (VICE-Team/svn-mirror, `vice/data/C64/{basic-901226-01,kernal-901227-03,chargen-901225-01}.bin`), reusing `ViceSharp.RomFetch` `RomProvider.DownloadRom` with the SHA256 pins already at `RomProvider.cs:127-129`. USB/local import is the FALLBACK. The appxmanifest DECLARES the `internetClient` capability (R8).
10→- Build/signing: the operator needs a SETUP RUNBOOK (install the windows-app/UWP workload, create a self-signed dev cert, pair the Dev-Mode console via Dev Home / Device Portal). Default MSIX signing for sideload is a self-signed dev cert. The runbook is a deliverable of S0/S33 and gates the on-console slices (S0, S34, S36-S41).
- RESTORE/NMI seam: ADD it to Abstractions + the in-process host facade (FR-XKBD); do not ship the tile disabled.
- Live-gamepad deterministic replay: scoped OUT for the console head; the core stays deterministic (input pushed through the identical `SetJoystickState` seam).
- Region/fps perf target: PAL 50 (both PAL and NTSC still run).
- Deadzone/hysteresis literals: RadialDeadzone 0.30 / Activate 0.55 / Release 0.40, tunable before the S8/S9 golden-vector slices.
- `ViceSharp.Xbox.ViewModels` references Abstractions + Protocol (settings gateway uses Protocol DTOs and `SettingsOptionCatalog`); ratified.
- Gamepad `GamepadButtonFlags` uses the WinRT-identical bit layout (0x40 = DPadUp); ratified.

## 0a. Validation tiers (updated 2026-07-13; supersedes the per-slice onConsole flags)

20→Operator clarification: UWP apps (and `Windows.Gaming.Input` controller reading, Win2D, XAudio2) run on a Windows 11 dev PC, not only on the console. Functional validation of the WHOLE app, including live Xbox-controller input, is done on the dev machine. The physical Xbox console is required ONLY at Microsoft Store submission. Development is NOT gated on the console.

Every slice is retagged to one of three tiers (this replaces the binary onConsole flag):
- Tier H (headless / CI): pure logic + in-process host + ViewModels + manifest-shape tests. Runs via `dotnet build`/`dotnet test` on any net10.0 agent with NO UWP workload (the `ViceSharpXboxUwp=false` fallback). The bulk (S1-S33 except S34); gated automatically here.
- Tier D (dev PC, UWP-on-Windows): needs the windows-app workload + an interactive Windows session + an Xbox controller. Covers the S34 UWP-XAML build and the live app/controller/audio/video/focus smokes formerly tagged on-console (old S36-S41). Run on the operator's dev PC; NO console needed.
- Tier C (console / Store submission; DEFERRED, tracked not dropped): the genuinely console-only checks, validated only when preparing a Store submission: actual console deploy of the AOT MSIX, the AppContainer resource budget (~1GB / 2-4 cores / 45% GPU) and sustained PAL-50 perf ON the console, Xbox device-family behaviors (real-TV overscan, focus-engagement on the TV profile, Guide/Nexus shell reservation), and Store certification. NOT a development gate.

Slice remap: S0 is now Tier D (dev-PC feasibility, below). Old S36-S41 become Tier D dev-PC smokes (run the UWP app on the Windows dev PC with a controller). A new DEFERRED S42 (Tier C) collects the console/Store-submission validation (console deploy, on-console perf budget, TV device-family behaviors, Store cert). Everything else is Tier H.

## 1. Locked decisions and scope
30→
### Locked decisions (carried, not relitigated)
- Target: true UWP-on-Xbox-console app. `net10.0-windows10.0.26100.0` + `UseUwp=true`; Native AOT for Release/Store, JIT for Debug/Dev-Mode sideload.
- Operator has a Dev-Mode Xbox and a Partner Center account, so on-console validation is in scope and required for "done".
- Reuse the managed core: Abstractions, Core, Chips, Architectures (RomFetch pulled transitively).
- Controller mapping (locked): left stick + D-pad -> JOY2; right stick -> JOY1; A -> JOY2 fire; B -> JOY1 fire; View/Menu/X/Y/bumpers/triggers/stick-press -> app functions; Guide/Nexus is shell-reserved and never observed by the app.
- Existing desktop build (Avalonia + WiX JIT/R2R) stays fully intact; the Xbox head is purely additive.
- BDP v4: tests-first; FR/TR/TEST drive tests; small gated slices; each gate 0 failed AND 0 skipped in its filter scope; determinism (bit-exact replay, lockstep vs `x64sc`) preserved.

### In scope
40→- One new UWP head project `src/ViceSharp.Xbox` (R1) and its three supporting new libraries: `src/ViceSharp.Host.InProcess` (R2), `src/ViceSharp.Xbox.Input` (R4), `src/ViceSharp.Xbox.ViewModels` (R12).
- Gamepad reader + dual-joystick mapper; system-button + input-context state machine; Kestrel-free in-process host + single managed pacing gate; Win2D video + ring-buffered XAudio2 audio; 10-foot XAML UI with virtual C64 keyboard (including a RESTORE/NMI seam, FR-XKBD); Settings + Device Setup + first-run ROM/keymap provisioning + data-path bridge; MSIX packaging + Dev-Mode deploy + rollback + GPL compliance + CI coexistence.
- On-console validation slices (feasibility, boot/render, live gamepad, A/V, system buttons, first-run provisioning + settings persistence, performance).

### Out of scope (and explicitly NOT changing)
- `src/ViceSharp.Host.Xbox` (the Avalonia desktop-debug scaffold) is NOT modified, NOT retargeted, NOT shipped to console. Its passing `XboxHostShellTests` stays green unchanged (locked decision 6, R1). No UWP/AOT/XAML/audio/video code is added to it. The name `ViceSharp.Host.Xbox.Uwp` (system-buttons/gamepad sections) is dropped.
- The desktop Avalonia UI, WiX/winget/choco/scoop pipeline, and `ViceSharp.Console` direct-drive path are untouched.
- The core per-cycle advance path (CPU/VIC-II/SID/CIA/VIA/IEC/snapshot) is untouched; determinism and lockstep-vs-`x64sc` parity remain a desktop/test concern driven by scripted input.
- The desktop parity gate keeps using `ViceEmulationGate`/`SemaphoreEmulationGate` (kernel32 waitable-timer path) via `VICESHARP_PACE`; the new managed gate is never selected in parity runs.
- Live-gamepad deterministic bit-exact REPLAY of console input is OUT of scope for the console head (R10; see operator-confirm item 1). The core stays deterministic because input is pushed through the identical `SetJoystickState` seam.
50→
## 2. Feasibility: the single S0 go/no-go gate (R7)

There is exactly ONE global feasibility gate, S0. It retires the one unproven unknown before any production investment. Per R7, the per-section spikes (topology `XBOXTOPO-0`, in-proc `S4`, video-audio `XAV-7`, packaging `PKG-4`, test-strategy `S0`) collapse into this single gate, and ALL production slices in ALL subsystems depend on the S0 GO decision.

S0 is a Tier D (dev PC) gate (per section 0a); the physical-console portion is deferred to Tier C (S42). S0 proves three things jointly on the Windows dev PC:
1. A modern-.NET Native-AOT UWP MSIX builds/publishes clean and RUNS on the Windows 11 dev PC: `dotnet publish -r win-x64 /p:PublishAot=true` of a minimal probe links clean with 0 trim/AOT (IL2xxx/IL3xxx) warnings, and the probe app launches on the dev PC, renders one C64 frame, and reads a plugged-in Xbox controller once via `Windows.Gaming.Input`. (The equivalent deploy+run on the physical console is deferred to S42 / Store-submission prep.)
2. The trimmed core graph links clean: the probe references Abstractions/Core/Chips/Architectures (and, for a smoke of the extracted-host shape, `Host.InProcess` once it exists; the probe itself uses direct-drive `ArchitectureBuilder.Build(descriptor).RunFrame()` per R2 so it does not block on the S1 extraction).
3. Win2D + Native-AOT packaging is clean (R13): the probe hosts a `SwapChainPanel` + Win2D (`Microsoft.Graphics.Canvas` `CanvasSwapChain`), and the AOT/MSIX package includes the per-arch Win2D native binary for win-x64 with the required rd.xml/trimming annotations and no missing-intrinsic fault.

60→Go/no-go criteria (all must hold for GO):
- AOT publish exit 0 with 0 IL2xxx/IL3xxx warnings under `TreatWarningsAsErrors`.
- Probe MSIX installs via Dev Mode (WinAppDeployCmd/Device Portal) and launches under Dev Home on the console.
- Managed C64 core advances at least one frame on-device (visible frame + advancing counter).
- Gamepad reads once on-device without an AppContainer/P-Invoke/marshalling fault.
- Win2D swap-chain composes and presents on-device under AOT.

If NO-GO: the head is blocked. Documented fallbacks: (a) if AOT link fails, fall back to the Debug JIT MSIX for dev deploy and treat Store/AOT as blocked pending investigation; (b) if Win2D+AOT packaging fails, S0 authorizes evaluating raw-DX11 interop (R13 rejects it only "unless S0 proves a Win2D+Native-AOT packaging blocker"). The explicit GO decision is recorded before any S1+ production slice is accepted as merged.

## 3. Target architecture
70→
### Project inventory after this work

Existing, reused unchanged in reference shape (some gain additive content, noted):
- `ViceSharp.Abstractions` (contract; gains relocated `MasterAudioControl` + `AudioSampleConverter` per TR-XAUDIO-003, and the new RESTORE/NMI seam `IMachineKeyboardInput.SetRestoreState(bool)` per FR-XKBD).
- `ViceSharp.Core`, `ViceSharp.Chips`, `ViceSharp.Architectures`, `ViceSharp.SourceGen` (untouched).
- `ViceSharp.Protocol` (gains additive static `SettingsOptionCatalog`; DTOs/enums reused).
- `ViceSharp.RomFetch`, `ViceSharp.Monitor`, `ViceSharp.Hosting` (untouched; `ViceDataPathResolver.cs` stays byte-for-byte unchanged, bridged via env var per R9).
- `ViceSharp.Console`, `ViceSharp.Avalonia` (untouched).
- `ViceSharp.Host.Xbox` (Avalonia desktop-debug scaffold; UNTOUCHED; `XboxHostShellTests` stays green).
80→
Refactored:
- `ViceSharp.Host` (R2): the Kestrel-free POCO service layer is EXTRACTED out into the new `ViceSharp.Host.InProcess`. `ViceSharp.Host` keeps ONLY the gRPC/Kestrel adapters (`AddViceSharpGrpcHost`/`MapViceSharpGrpcHost`, `Grpc*ServiceHost`, `GrpcHostServiceAdapters`) and references `ViceSharp.Host.InProcess`. Pure move + re-reference, determinism-neutral; the desktop gRPC host and all its tests stay green.

NEW projects:
- `src/ViceSharp.Host.InProcess` (net10.0, `IsAotCompatible`, NO `FrameworkReference Microsoft.AspNetCore.App`). Holds the moved POCO layer (namespaces preserved: `ViceSharp.Host.Services`/`ViceSharp.Host.Runtime`): `EmulatorHostService`, `EmulationPumpService`, `LocalVideoFrameSource`, `InputServiceHost`, `EmulatorRuntimeRegistry/Session`, `DefaultEmulatorRuntimeFactory`, `IEmulationGate`, `SemaphoreEmulationGate`/`ViceEmulationGate` (Windows-guarded, never selected on console), plus NEW `PacingMath`, NEW `XboxManagedFrameGate : IEmulationGate` (R3), the NEW `IConsoleEmulatorHost` facade + `ConsoleHostComposition`, and the pure audio ring/queue math + `FrameGeometry` contract.
- `src/ViceSharp.Xbox.Input` (net10.0, ZERO WinRT/Windows.Gaming.Input, `IsAotCompatible`, zero runtime reflection) (R4). Holds `GamepadButtonFlags` (WinRT-identical bits), `GamepadSnapshot` (double axes), `JoystickPortState`, `XboxInputConfig`, `XboxJoystickMapper` (pure), `XboxInputContext` (single-consumer state machine), the binding model + source-gen persistence seam, `DirectionalRepeater`.
- `src/ViceSharp.Xbox.ViewModels` (net10.0; references `ViceSharp.Abstractions` + `ViceSharp.Protocol` only; zero XAML/UWP/Core/Chips/Architectures/Host/Avalonia/Grpc) (R12). Holds all unit-testable UI logic: navigation stack, focus-graph math, virtual-keyboard layout/emission, video-pull adapter (cadence + pure sink), input-context observer, page ViewModels (Home/Settings/DeviceSetup/InputMapping/About), settings/device/ROM/UI-state ViewModels, and the seam interfaces the head adapts to.
- `src/ViceSharp.Xbox` (the UWP XAML head) (R1/R12). `TargetFramework` conditional on `ViceSharpXboxUwp` (default false -> net10.0 workload-free fallback; true -> net10.0-windows10.0.26100.0 + `UseUwp=true`). References `ViceSharp.Xbox.ViewModels` + `ViceSharp.Xbox.Input` + Abstractions/Core/Chips/Architectures + `ViceSharp.Host.InProcess`. NO reference to `ViceSharp.Host`, `ViceSharp.Avalonia`, `ViceSharp.Protocol`-Grpc, `ViceSharp.Monitor`, or any Grpc.*/Microsoft.AspNetCore.*/Avalonia.* package. Holds XAML Pages/Controls/App composition, the Win2D `SwapChainPanel` video sink, the ring-buffered XAudio2 backend, the WinRT gamepad source, the UWP `LocalDataFolder`/`StoragePicker`/`RomAcquirer`, and the adapter that implements the ViewModels seams over `IConsoleEmulatorHost`. `Package.appxmanifest` is UWP-only (glob-excluded on the fallback TFM).

90→### Reference rules and TFMs (exact)
- `ViceSharp.Host.InProcess`: net10.0. References Abstractions/Core/Chips/Architectures (+ RomFetch transitively). MUST NOT reference `Microsoft.AspNetCore.App` (reflection test guards it). `InternalsVisibleTo` the TestHarness for moved-type tests.
- `ViceSharp.Xbox.Input`: net10.0. References Abstractions + Protocol (for `InputPort`) only. Zero `Windows.*`/`Microsoft.AspNetCore.*`. `IsAotCompatible`, `EnableTrimAnalyzer` clean.
- `ViceSharp.Xbox.ViewModels`: net10.0. References Abstractions + Protocol + `ViceSharp.Xbox.Input` (for the shared input types the input-mapping VM surfaces). No Core/Chips/Architectures/Host/Avalonia/Grpc/XAML. (This refines R12/tenfoot "Abstractions-only": the settings gateway uses Protocol DTOs and `SettingsOptionCatalog` lives in Protocol, so Protocol is permitted; both are portable, Kestrel-free, AOT-safe. Flagged in residual items for ratification.)
- `ViceSharp.Xbox`: `ViceSharpXboxUwp` conditional TFM as above. UWP-only PropertyGroup (`'$(ViceSharpXboxUwp)'=='true'`): `UseUwp=true`, `TargetPlatformMinVersion=10.0.19041.0`, `RuntimeIdentifier=win-x64`, `Platform=x64`, `DisableRuntimeMarshalling=true`, `<PublishAot Condition="'$(Configuration)'=='Release'">true</PublishAot>`, `AppxPackage=true`, `EnableMsixTooling=true`, `WindowsPackageType=MSIX`. `EnableTrimAnalyzer`/`IsAotCompatible` inherited true (NOT overridden). Precedent: `Host.Android.csproj:24-27` (`ViceSharpHostAndroidFallback`).
- TestHarness references (R12): `ViceSharp.Xbox.Input` AND `ViceSharp.Xbox.ViewModels` (the two portable libs), plus its existing transitive reach to `ViceSharp.Host` -> `ViceSharp.Host.InProcess`. It does NOT reference the UWP head `ViceSharp.Xbox` (would drag the Windows-SDK/UWP targets and break workload-absent CI). The head's shape is validated by raw-file-read tests (`ViceSharpXboxHeadTests`/`XboxUwpHeadShellTests`), matching the HostShells precedent (`XboxHostShellTests.cs:26`, `MobileHostShellTests.cs:40`).

### Build property and AOT/trim posture (R8)
- ONE switch: MSBuild property `ViceSharpXboxUwp` (unset/false -> net10.0 fallback; true -> net10.0-windows UWP). `dotnet build ViceSharp.slnx` stays green on workload-absent agents via the fallback.
- ONE AOT mechanism: `PublishAot` gated by `Configuration=='Release'` in the csproj (Debug = JIT for fast Dev-Mode sideload). The `XboxAot.pubxml`/`XboxJitDev.pubxml` publish-profile alternative and the `ViceSharpXboxHead`/`ViceSharpXboxHead`-named property are DROPPED.
100→- AOT audit conclusion (grep-verified in the topology section): the reused core graph is reflection/JSON/dynamic/Emit-free. YamlDotNet in Architectures is analyzer-only static source-gen (`Architectures.csproj:15-18`). Core config must use the non-reflective INI path (never `ConfigurationBinder.Get<T>/Bind`). The only Marshal/DllImport cluster is `ViceNative.cs` (parity-test-only, never called on the Xbox path; a never-invoked DllImport is AOT-safe).

### AppContainer constraints
- No Kestrel/ASP.NET on the emulation/render/input path (guaranteed structurally by the head not referencing `ViceSharp.Host`).
- No winmm (`waveOut*`) and no kernel32 waitable-timer / `SetThreadAffinityMask` P/Invoke on the console path. Audio is XAudio2 (`xaudio2_9.dll`) via blittable vtable calls; pacing is the managed `XboxManagedFrameGate` (zero P/Invoke). The pump worker is UNPINNED (`VICESHARP_EMU_CPU` unset -> `SetThreadAffinityMask` never called).
- Filesystem writes land in `ApplicationData.Current.LocalFolder`; ROM/keymap resolution is bridged there before any host build (R9).

### Determinism guarantee
- The head reuses the managed core unchanged and only PUSHES input via `IMachineJoystickInput.SetJoystickState` (Abstractions `IMachineJoystickInput.cs:11`) / the RESTORE seam, and PULLS frames via `ILocalVideoFrameSource.TryCopyFrameInto` (`LocalVideoFrameSource.cs:82-94`). Rendering is a pure read-only sink (never advances the core). The `XboxManagedFrameGate` is a WALL-CLOCK pacing concern only (how many master cycles a Running session advances per tick); it never changes per-step cycle counts. Every gate re-runs `--filter Category=Determinism` and requires 0 failed with the pass count equal to the pre-S1 baseline.

110→## 4. Subsystem designs (reconciled)

### 4a. Gamepad reader + joystick mapping (area GAMEPAD; R4/R5)
Portable library `ViceSharp.Xbox.Input` owns all pure logic. WinRT decoupling: our own `[Flags] enum GamepadButtonFlags : uint` uses bit values IDENTICAL to `Windows.Gaming.Input.GamepadButtons` so the UWP adapter casts with no per-bit remap: `None=0, Menu=0x1, View=0x2, A=0x4, B=0x8, X=0x10, Y=0x20, DPadUp=0x40, DPadDown=0x80, DPadLeft=0x100, DPadRight=0x200, LeftShoulder=0x400, RightShoulder=0x800, LeftThumbstick=0x1000, RightThumbstick=0x2000, Paddle1..4=0x4000..0x20000`. No Nexus/Guide member (shell-reserved). (Reconciliation note: R4's operative rule is "WinRT-identical, cast with no per-bit remap" and "use the WinRT bit values"; that unambiguously selects this 0x40-DPadUp layout - the only one the critic confirmed is WinRT-identical. R4's parenthetical attribution to "the system-buttons section's layout" (which is 1<<10) is a mislabel and is overridden by R4's own operative rule; flagged in residual items.)

Key types and signatures:
- `public readonly record struct GamepadSnapshot(double LeftStickX, double LeftStickY, double RightStickX, double RightStickY, double LeftTrigger, double RightTrigger, GamepadButtonFlags Buttons, ulong Timestamp) { public static GamepadSnapshot Neutral => default; }` (double axes, -1..1 sticks / 0..1 triggers; mirrors `GamepadReading`).
- `public readonly record struct JoystickPortState(byte DirectionMask, bool Fire) { public static JoystickPortState Neutral => default; }` mask bits Up=0x01/Down=0x02/Left=0x04/Right=0x08 (matches `C64JoystickPort.cs:10-19`; fire is the separate bool, never a mask bit; core ORs internal Fire=0x10 itself at `C64MemoryMap.cs:597-598`).
- `public readonly record struct XboxInputConfig(double StickDeadzone, double DiagonalThreshold, double ActivateThreshold, double ReleaseThreshold, bool SwapPorts)` with exact-literal defaults frozen before S2 golden vectors (proposed: RadialDeadzone 0.30, Activate 0.55, Release 0.40; see operator-confirm item 4).
- `public static class XboxJoystickMapper { public static (JoystickPortState Joy1, JoystickPortState Joy2) Map(in GamepadSnapshot reading, in XboxInputConfig config); }` pure, deterministic, allocation-free (R5). Hysteresis latch is threaded by the single-owner `XboxInputContext` (an internal `ConverterState(byte LeftStickBits, byte RightStickBits)` prior-in/next-out overload preserves the per-axis Activate/Release hysteresis of FR-GAMEPAD-004 without static state, keeping the mapper pure).
120→
Mapping rules: left stick OR-merged with D-pad -> Joy2; right stick -> Joy1 (no D-pad); A -> Joy2.Fire; B -> Joy1.Fire. Analog-to-8-way = radial deadzone gate (mag < deadzone releases all four bits) + per-axis Activate/Release hysteresis (removes chatter) + axis sign (+Y=Up 0x01, -Y=Down 0x02, +X=Right 0x08, -X=Left 0x04, matching the active-HIGH low nibble at `C64MemoryMap.cs:596`). SOCD applied AFTER merge: `SocdMode.Neutral` clears both bits of any Up+Down or Left+Right conflict. Swap: `XboxInputConfig.SwapPorts` flips which physical stick set feeds which explicit port INSIDE the mapper config (swap-immune; never `PrimaryJoystick`, never a downstream profile swap) (R5).

Sampling and emission: the in-process host owns the call site (R5): sample the gamepad and invoke `XboxInputContext.Tick(frameIndex, snapshot)` EXACTLY ONCE per emulated frame at the `IVideoChip.FrameCompleted` / pump Step boundary, before the next frame advances. Emission uses explicit `InputPort.Joystick1`/`Joystick2` (`ProtocolContracts.cs:57-63`, swap-immune per `InputServiceHost.cs:190-198`) via `IJoystickOutputSink`; the console sink is `DirectDriveJoystickSink` wrapping `IMachineJoystickInput.SetJoystickState` (Joystick1->control port 1, Joystick2->control port 2). Delta emission: emit to a port only when its (mask,fire) changed since the previous tick. WinRT `GamepadAdded`/`GamepadRemoved` fire on arbitrary threads and MUST NOT emit; the reader enqueues add/remove and drains at the top of `Tick` on the single emulation thread. Fail-safe: on active-pad disconnect, no pad, or a reader exception, treat the snapshot as `GamepadSnapshot.Neutral` and emit centered to BOTH ports; never throw into the frame loop.

Single-consumer boundary (R5, resolves the critique's double-path/double-read): there is NO separate `AppButtonChanged` event path and NO context-unaware pump. `XboxInputContext` is the SOLE consumer of the whole `GamepadSnapshot`; the WinRT source only produces snapshots.

WinRT adapter (head only, device-tested): `internal static GamepadSnapshot FromReading(Windows.Gaming.Input.GamepadReading r)` casting `GamepadButtons` to `GamepadButtonFlags` (identical bits) + axes + timestamp.

Failure modes: no pad -> centered once (delta-guarded); active pad lost mid-press -> center both, promote lowest-index remaining pad; `GetCurrentReading` throws -> caught/logged/swallowed -> Neutral; analog jitter -> hysteresis band; stick vs D-pad opposing -> SOCD Neutral; off-thread device events -> drained inside Tick.
130→
### 4b. System-button + input-context state machine (areas SYSBTN, CTX; R5)
`XboxInputContext` (in `ViceSharp.Xbox.Input`) is the unified single authority (merges the gamepad pump and the system-buttons state machine per R5). It is a pure function of (previous snapshot, current snapshot, context, profile, mapper output) with no I/O, clock, or host calls.

- `public enum InputContext { Gameplay, MainMenu, VirtualKeyboard, ConfirmDialog }` (canonical; the ViewModels project maps any non-Gameplay to "UI navigation" for focus routing).
- `public enum AppCommand { None, OpenMainMenu, CloseMenu, ToggleVirtualKeyboard, AutostartDrive8, WarmReset, ColdReset, ToggleWarp, WarpHoldOn, WarpHoldOff, QuickSaveState, QuickLoadState, SwapJoystickPorts, RequestExit, ConfirmYes, ConfirmNo, UiNavigateUp/Down/Left/Right, UiActivate, UiBack }`.
- `public enum BindableInput { Menu, View, A, B, X, Y, LeftShoulder, RightShoulder, LeftTrigger, RightTrigger, LeftThumbstick, RightThumbstick }`; `public enum BindingActivation { Press, Hold, Toggle }`; `public sealed record ButtonBinding(BindableInput Input, AppCommand Command, BindingActivation Activation)`; `public sealed record BindingProfile(string Id, string DisplayName, IReadOnlyList<ButtonBinding> Gameplay) { public static BindingProfile Default { get; } }`.
- `public readonly record struct InputResolution(InputContext NextContext, IReadOnlyList<AppCommand> Commands, JoystickPortState Joy1, JoystickPortState Joy2)`.
- `public sealed class XboxInputContext { public InputContext Context { get; } public InputResolution Tick(long frameIndex, in GamepadSnapshot snapshot); }` internally calls `XboxJoystickMapper.Map` in Gameplay and forces both ports to `JoystickPortState.Neutral` (and suppresses A-fire) in every non-Gameplay context, emitting a one-shot explicit neutral push to both ports on the Gameplay->non-Gameplay edge (FR-CTX-002/004).
- `public sealed class DirectionalRepeater { public AppCommand? Tick(GamepadButtonFlags held, double elapsedMs); }` pure UI-nav auto-repeat (initial delay 400ms, repeat 90ms).
140→
Default gameplay bindings (all remappable except Menu/View which stay menu-control): Menu = Open/Close main menu (toggle); View = Toggle virtual C64 keyboard; A = JOY2 fire (locked); B = JOY1 fire (locked); X = Autostart drive 8; Y = Warm reset (guarded by `ConfirmDialog`); LB = Quick-save; RB = Quick-load; LT hold (on>=0.6/off<=0.4 hysteresis) = Warp while held; RT = unbound; L3 = Swap ports (toggle); R3 = unbound. Exit has NO direct binding: reachable only from the main menu behind a `ConfirmDialog` (`ConfirmYes` exits). Warp maps to the limiter (`IsWarpMode` derived from `LimiterEnabled`, `AttachPanelViewModel.cs:914,931`): `WarpHoldOn` disables the limiter, `WarpHoldOff` re-enables it. App-function commands are edge-triggered (Press once per down edge; Hold On/Off across the analog threshold; Toggle flips per down edge); held buttons never auto-repeat app functions.

Persistence uses `System.Text.Json` source generation (`[JsonSerializable(typeof(BindingProfile))] public sealed partial class BindingJsonContext : JsonSerializerContext`) behind `public interface IBindingStore { BindingProfile Load(); void Save(BindingProfile profile); void ResetToDefaults(); }`; no runtime reflection. The console head binds a file-backed store rooted at `LocalFolder` (`bindings.v1.json`); tests use an in-memory store.

Dispatch decoupled from resolution: the machine only EMITS `AppCommand`s; a thin `AppCommandDispatcher` (in the head, tested against fakes) translates them to host calls through `IHostCommandSink` and marshals state-mutating commands (snapshot save/load, reset, warp) onto the session lock / mutation path (`SnapshotServiceHost.cs:37` captures under `session.SyncRoot`), never the input-polling thread. Mapping: `AutostartDrive8->ResetAndAutostartDrive8Async`, `WarmReset->WarmResetAsync`, `ColdReset->ColdResetAsync`, `QuickSaveState->CaptureSnapshotAsync`, `QuickLoadState->RestoreSnapshotAsync`, `Warp->UpdateSettings limiter toggle`, `SwapJoystickPorts->flip XboxInputConfig.SwapPorts` (no settings call), `RequestExit(ConfirmYes)->app exit`.

Failure modes: stuck direction/fire on menu open -> prevented by the one-shot neutral push (FR-CTX-004); accidental exit -> prevented by no-gameplay-exit + confirm; command spam -> edge triggering; trigger chatter -> LT hysteresis band; reflection breaking AOT -> source-gen JSON + WinRT-free portable lib; non-deterministic input -> pure `Tick` + dispatch marshaled to the session lock.

### 4c. Kestrel-free in-process host (area INPROC; R2/R3/R5)
150→Extract the POCO service layer into `ViceSharp.Host.InProcess` (namespace-preserving) and hand-wire it with `new` (no `WebApplication`/Kestrel). Reuse `EmulationPumpService` (dedicated worker thread, whole-instruction stepping `InstructionGroupSize=5` at `EmulationPumpService.cs:200-231`, tear-free frame commit tap `IVideoChip.FrameCompleted -> session.CommitFrame` at `EmulationPumpService.cs:247-252`), `LocalVideoFrameSource.TryCopyFrameInto` (`LocalVideoFrameSource.cs:82-94`, lock-free, non-feedback, `FR-1132`/`BUG-THROTTLE-001`), and `InputServiceHost` (push-based swap-immune injection, `InputServiceHost.cs:44-62,190-198`).

### Turn 81 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 79 matching lines
F:\GitHub\vice-sharp\docs\plans\PLAN-XBOXUWP-BDPv4.md
5:## 0. Decisions locked at approval (2026-07-13)
9:- ROM acquisition PRIMARY: verified HTTPS download from the VICE ROM mirror on GitHub (VICE-Team/svn-mirror, `vice/data/C64/{basic-901226-01,kernal-901227-03,chargen-901225-01}.bin`), reusing `ViceSharp.RomFetch` `RomProvider.DownloadRom` with the SHA256 pins already at `RomProvider.cs:127-129`. USB/local import is the FALLBACK. The appxmanifest DECLARES the `internetClient` capability (R8).
10:- Build/signing: the operator needs a SETUP RUNBOOK (install the windows-app/UWP workload, create a self-signed dev cert, pair the Dev-Mode console via Dev Home / Device Portal). Default MSIX signing for sideload is a self-signed dev cert. The runbook is a deliverable of S0/S33 and gates the on-console slices (S0, S34, S36-S41).
11:- RESTORE/NMI seam: ADD it to Abstractions + the in-process host facade (FR-XKBD); do not ship the tile disabled.
18:## 0a. Validation tiers (updated 2026-07-13; supersedes the per-slice onConsole flags)
23:- Tier H (headless / CI): pure logic + in-process host + ViewModels + manifest-shape tests. Runs via `dotnet build`/`dotnet test` on any net10.0 agent with NO UWP workload (the `ViceSharpXboxUwp=false` fallback). The bulk (S1-S33 except S34); gated automatically here.
24:- Tier D (dev PC, UWP-on-Windows): needs the windows-app workload + an interactive Windows session + an Xbox controller. Covers the S34 UWP-XAML build and the live app/controller/audio/video/focus smokes formerly tagged on-console (old S36-S41). Run on the operator's dev PC; NO console needed.
27:Slice remap: S0 is now Tier D (dev-PC feasibility, below). Old S36-S41 become Tier D dev-PC smokes (run the UWP app on the Windows dev PC with a controller). A new DEFERRED S42 (Tier C) collects the console/Store-submission validation (console deploy, on-console perf budget, TV device-family behaviors, Store cert). Everything else is Tier H.
29:## 1. Locked decisions and scope
41:- Gamepad reader + dual-joystick mapper; system-button + input-context state machine; Kestrel-free in-process host + single managed pacing gate; Win2D video + ring-buffered XAudio2 audio; 10-foot XAML UI with virtual C64 keyboard (including a RESTORE/NMI seam, FR-XKBD); Settings + Device Setup + first-run ROM/keymap provisioning + data-path bridge; MSIX packaging + Dev-Mode deploy + rollback + GPL compliance + CI coexistence.
51:## 2. Feasibility: the single S0 go/no-go gate (R7)
55:S0 is a Tier D (dev PC) gate (per section 0a); the physical-console portion is deferred to Tier C (S42). S0 proves three things jointly on the Windows dev PC:
56:1. A modern-.NET Native-AOT UWP MSIX builds/publishes clean and RUNS on the Windows 11 dev PC: `dotnet publish -r win-x64 /p:PublishAot=true` of a minimal probe links clean with 0 trim/AOT (IL2xxx/IL3xxx) warnings, and the probe app launches on the dev PC, renders one C64 frame, and reads a plugged-in Xbox controller once via `Windows.Gaming.Input`. (The equivalent deploy+run on the physical console is deferred to S42 / Store-submission prep.)
69:## 3. Target architecture
74:- `ViceSharp.Abstractions` (contract; gains relocated `MasterAudioControl` + `AudioSampleConverter` per TR-XAUDIO-003, and the new RESTORE/NMI seam `IMachineKeyboardInput.SetRestoreState(bool)` per FR-XKBD).
87:- `src/ViceSharp.Xbox.ViewModels` (net10.0; references `ViceSharp.Abstractions` + `ViceSharp.Protocol` only; zero XAML/UWP/Core/Chips/Architectures/Host/Avalonia/Grpc) (R12). Holds all unit-testable UI logic: navigation stack, focus-graph math, virtual-keyboard layout/emission, video-pull adapter (cadence + pure sink), input-context observer, page ViewModels (Home/Settings/DeviceSetup/InputMapping/About), settings/device/ROM/UI-state ViewModels, and the seam interfaces the head adapts to.
105:- Filesystem writes land in `ApplicationData.Current.LocalFolder`; ROM/keymap resolution is bridged there before any host build (R9).
110:## 4. Subsystem designs (reconciled)
129:Failure modes: no pad -> centered once (delta-guarded); active pad lost mid-press -> center both, promote lowest-index remaining pad; `GetCurrentReading` throws -> caught/logged/swallowed -> Neutral; analog jitter -> hysteresis band; stick vs D-pad opposing -> SOCD Neutral; off-thread device events -> drained inside Tick.
164:Failure modes: ROMs missing -> `Create` throws -> caught -> `ConsoleSessionResult(false, Error)` to Device Setup; frame before first commit -> `TryCopyFrameInto` false -> UI skips tick; destination too small -> false -> UI sizes to `BufferLength`; second `StartC64Session` -> facade closes/replaces the prior session (single-session console head).
196:### 4f. Settings + Device Setup + first-run provisioning + data-path bridge (areas XSET, XDEV, XROM; R9/R11)
202:- `XboxRomProvisioningViewModel` + `RomProvisionEvaluator`: `Evaluate(c64Dir, profile) -> RomProvisionAssessment(State{NotProvisioned|Partial|Complete}, Roles[])` using `C64ViceRomNames` + `RomProvider.IsAvailable`/hash; kernal optional for Ultimax (`C64RomLoader.cs:192-193`); `IsBootBlocked` true unless Complete. Acquisition PRIMARY (locked 2026-07-13): `DownloadCoreSetAsync` downloads the 3 core ROMs over verified HTTPS from the VICE GitHub mirror (VICE-Team/svn-mirror, `vice/data/C64/{basic-901226-01,kernal-901227-03,chargen-901225-01}.bin`) via `RomProvider.DownloadRom` using the SHA256 pins already at `RomProvider.cs:127-129` (after explicit confirm; requires the `internetClient` capability). FALLBACK: `ImportFromFilesAsync` (USB/storage validated by size + MD5 via `C64RomLoader.cs:13-30,127-145`, 64MB import ceiling guard). ROMs are NEVER shipped; the 3 core C64 ROMs (basic-901226-01.bin 8KB, kernal-901227-03.bin 8KB, chargen-901225-01.bin 4KB) land in `LocalFolder\vice\C64`.
204:Data-path bridge and startup ordering (R9): `XboxDataPathBridge.Configure(ILocalDataFolder folder, IConfigurationBuilder? cfg)` MUST run at process entry BEFORE `ConsoleHostComposition.Build` / `DefaultEmulatorRuntimeFactory` resolves ROMs+keymaps. It creates `folder.C64Path`, sets `Environment.SetEnvironmentVariable("VICESHARP_ROM_PATH", folder.RootPath)`, sets `IConfiguration "ViceSharp:ConfigDirectory"=root`, and copies packaged `*.vkm` from `InstalledLocation\Assets\vice-data\C64\*.vkm` into `C64Path` when absent so `InputServiceHost.FindViceC64DataPath` (`InputServiceHost.cs:319-329`) finds them. `ViceDataPathResolver.cs` stays byte-for-byte unchanged (bridged only via the env var; `ContainsC64Data` at `:204` needs the created `C64` subdir). AppContainer keymap write path (R9): the `InputServiceHost` temp-file keymap writes (`InputServiceHost.cs:235,300`) are redirected to the writable `LocalFolder` root (or made no-op on console); an off-console test asserts the write path is [... truncated (1035 chars total)]
208:Failure modes: no network + no USB at first run -> State stays Partial/NotProvisioned, `IsBootBlocked`, wizard re-shows; download hash mismatch -> `RomProvider.DownloadRom` throws, retry, no file written; import size/MD5 mismatch -> rejected pre-copy; vkm not copyable -> keyboard-map picker shows only the embedded gtk3_pos fallback (`InputServiceHost.cs:300-311`); `VICESHARP_ROM_PATH` unset before first resolver call -> guarded by the ordered startup step; alternate-kernal profiles (Ultimax) -> evaluator keys off `profile.KernalRomName`.
211:`Package.appxmanifest` (UWP-only, glob-excluded on the fallback TFM): `<Identity Name="sharpninja.ViceSharp.Xbox" Publisher="CN=sharpninja" ProcessorArchitecture="x64" Version="$(MsiVersion).0"/>`; `<TargetDeviceFamily Name="Windows.Xbox" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0"/>`. Capability (R8): the gamepad needs none; `internetClient` is DECLARED iff the first-run HTTPS ROM download path (FR-XROM-002) is kept. DEFAULT: USB/local import is the primary sanctioned path; HTTPS download is optional and, if kept, requires `internetClient` (a general, not restricted, capability). Do not assert "no capabilities" while keeping a download path (resolves the critique contradiction). `MsiVersion`/GitVersion (`Build.cs:879`) stamps the MSIX so it tracks the MSI/winget scheme.
213:GPL-2.0-or-later compliance (R8): head-only `Content` items copy `COPYING` + `THIRD_PARTY_NOTICES.md` under `Licenses/`; the shipped GPL VICE `*.vkm` keymap data is added as packaged Content under `Assets\vice-data\C64\*.vkm` WITH `THIRD_PARTY_NOTICES.md` GPL attribution (confirm vkm redistributability under GPL-2.0-or-later). The "no-ROM payload" test asserts zero `kernal-*/basic-*/chargen-*.bin` while ALLOWING the vkm assets. A compiled `SourceUrl` constant surfaces on the About screen; the GitHub release attaches the MSIX plus a `git archive` source tarball of the tag, satisfying section-3 corresponding-source for sideload (and any future Store path). Store submission is DEFERRED pending a documented GPL section-6 (no-further-restrictions) review vs the Store Standard Application License Terms.
219:Failure modes: CI red from a missing workload -> fallback TFM + CiConfig test; manifest rejects on console -> XML assertions catch pre-deploy; AOT link/trim failure -> off-console AOT gate catches before device; Store/GPL conflict -> avoided by sideload+source MVP; ROM accidentally packaged -> hard test failure; device targets wired into CI -> `XboxCiConfigTests` catches.
221:### 4h. RESTORE / NMI seam (area XKBD; R12)
222:There is no reliable `SetKeyState(string,bool)` that fires C64 RESTORE: it is a hardware NMI in `C64KeyboardMatrix.SetKey` (`C64KeyboardMatrix.cs:65-67`) on keycode 0x31, which the default map binds to `"*"`. Per R12 DEFAULT ruling, ADD a dedicated seam: `IMachineKeyboardInput.SetRestoreState(bool pressed)` in Abstractions, surfaced on the in-process facade as `IConsoleEmulatorHost.SetRestoreState(sessionId, bool)`, routed to the NMI path distinct from `SetKeyState`. The virtual-keyboard RESTORE tile drives this, not `SetKeyState`. Determinism preserved (NMI trigger goes through the same push seam). Fallback if the operator declines the seam: ship the RESTORE tile visibly disabled (see operator-confirm item 5).
224:## 5. Requirements catalog (unified R6 IDs)
291:### Area XKBD (RESTORE / NMI)
292:- FR-XKBD-001: A dedicated RESTORE/NMI trigger exists on the Abstractions keyboard seam and the in-process facade; the virtual-keyboard RESTORE tile drives it, not SetKeyState. Acceptance: IMachineKeyboardInput.SetRestoreState(bool) routes to the C64 NMI path (keycode 0x31) distinct from SetKeyState; VirtualKeyboardViewModel RESTORE tile calls the facade's SetRestoreState, and a spy keyboard input records the NMI trigger, not a SetKeyState("*"). -> TR-XKBD-001 -> TEST-XKBD-001.
296:### Area XSET / XDEV / XROM (settings + device setup + provisioning)
305:- FR-XROM-001: First run evaluates ROM provisioning and blocks normal boot until Complete (offering the wizard). Acceptance: Evaluate returns Complete only when every required role is present+hash-valid; NotProvisioned/Partial otherwise; IsBootBlocked unless Complete (or kernal not required). -> TR-XPATH-001 -> TEST-XSET-001.
306:- FR-XROM-002: The wizard acquires the core ROM set by verified HTTPS download (requires internetClient) or USB/storage import into LocalFolder\vice\C64. Acceptance: Download fetches basic/kernal/characters SHA256-checked after confirm; Import validates size+MD5 before copy; mismatch rejected leaving state unchanged. -> TR-XPATH-001 -> TEST-XSET-001.
307:- FR-XROM-003: Shipped GPL *.vkm keymaps are provisioned into the writable C64 folder so the picker lists them. Acceptance: after XboxDataPathBridge.Configure the packaged vkm exist under C64Path and ListKeyboardMapsAsync returns them; else the embedded gtk3_pos fallback. -> TR-XPATH-001 -> TEST-XSET-001.
320:- FR-XBOXPKG-003: Package.appxmanifest declares Windows.Xbox (Min 10.0.19041.0, MaxTested 10.0.26100.0), x64 identity, and declares internetClient iff the HTTPS ROM download path is kept (else no restricted capability). Acceptance: XML-parse asserts the TargetDeviceFamily, x64, and the internetClient-present-iff-download-kept rule. -> TR-XBOXPKG-002 -> TEST-XBOXPKG-003.
321:- FR-XBOXGPL-006: The MSIX bundles COPYING + THIRD_PARTY_NOTICES.md (incl. vkm GPL attribution) + the *.vkm Content, bundles zero ROM *.bin, and exposes the source URL; the release attaches MSIX + source archive. Acceptance: staging test asserts Licenses/ contents + vkm Assets present + no kernal/basic/chargen *.bin + compiled SourceUrl. -> TR-XBOXCI-005 -> TEST-XBOXGPL-001.
323:TRs: TR-XBOXPKG-002 ViceSharpXboxUwp conditional-TFM fallback + head-only UWP/XAML/manifest/MSIX guarded so a workload-less agent builds slnx (-> TEST-XBOXPKG-001). TR-XBOXCI-005 Nuke PublishXbox/DeployXbox/ValidateXbox exist and are absent from both CI/release YAMLs; ValidateXbox runs off-console (-> TEST-XBOXCI-001). TESTs: TEST-XBOXPKG-001 csproj/slnx shape. TEST-XBOXPKG-003 manifest XML + capability rule. TEST-XBOXGPL-001 license/vkm/no-ROM/source-URL staging. TEST-XBOXCI-001 azure-pipelines.*.yml exclude device targets. TEST-XBOXDEPLOY-007 on-console install/launch/first-frame + rollback.
342:- XSET 001-005 / XDEV 001-003 / XROM 001-003: -> TR-XPATH/TR-XPERSIST/TR-XMVVM/TR-XSET-002 -> TEST-XSET-001 suite.
349:## 6. Byrd slice plan (one ordered global sequence)
381:- S14. XboxDataPathBridge + keymap seeding + startup ordering + AppContainer keymap write-path (R9). onConsole=false. Dependencies: S3. Tests-first: XboxDataPathBridgeTests (Configure sets VICESHARP_ROM_PATH + creates <root>\C64 so ViceDataPathResolver.FindDataRoots() contains <root>; copies packaged *.vkm into C64Path when absent; returns configDir usable by ViceSettings.OpenAt; the keymap-write path lands in the writable LocalFolder root, not a temp file). RED: bridge absent; resolver returns empty for the temp root. GREEN: with ILocalDataFolder at a temp dir, Configure does all of the above; ViceDataPathResolver.cs and InputServiceHost.cs unchanged except the write-path redirect (FR-XROM-003; TR-XPATH-001). Validation: TEST("FullyQualifiedName~Xbox.XboxDataPathBridgeTests"). Acceptance: env var set before the resolver call; <root>\C64 created; FindDataRoots contains <root>; vkm copied only when absent; keymap write path is the AppContainer-writable root.
401:- S24. RESTORE / NMI seam (FR-XKBD) in Abstractions + Host.InProcess facade. onConsole=false. Dependencies: S3. Tests-first: TEST-XKBD-001 (facade.SetRestoreState routes to the C64 NMI path keycode 0x31 distinct from SetKeyState, via a spy IMachineKeyboardInput; a SetKeyState("*") does NOT fire RESTORE). RED: no SetRestoreState seam. GREEN: IMachineKeyboardInput.SetRestoreState(bool) added to Abstractions + IConsoleEmulatorHost.SetRestoreState, routed to the NMI trigger (FR-XKBD-001; TR-XKBD-001). Validation: TEST("FullyQualifiedName~Xbox.Restore|FullyQualifiedName~XKbd"); DET. Acceptance: RESTORE reaches the NMI path only via the dedicated seam; DET == baseline.
403:- S25. Virtual keyboard layout + emission + RESTORE tile drives FR-XKBD. onConsole=false. Dependencies: S22, S24. Tests-first: TEST-XBOXUI-006 (RETURN tile emits SetKeyState("Return",true) then false; shift-latch + F1 emits "F2"; every KeyName in the known-good C64KeyboardMap alias list; RESTORE tile calls SetRestoreState, not SetKeyState). RED: VirtualKeyboardViewModel absent. GREEN: layout + emission + shift-latch + RESTORE-tile wiring (FR-XBOXUI-006; FR-XKBD-001; TR-XBOXUI-006). Validation: TEST("FullyQualifiedName~VirtualKeyboardViewModelTests"). Acceptance: exact key strings; shift-latch swaps F1->F2; RESTORE drives the NMI seam.
405:- S26. XboxSettingsViewModel bind/apply/validate/revert (against the fake gateway) (R11 split a). onConsole=false. Dependencies: S15, S21. Tests-first: XboxSettingsViewModelTests (Refresh loads host settings/profiles; Apply sends canonical DTO ids + clears dirty; adopts host-canonical returned settings; profile/resource-mode -> RequiresRestart; Validate does not apply + populates results; Revert restores last-applied). RED: VM + fake gateway absent. GREEN: VM reproduces desktop settings behaviors (FR-XSET-001/002/003/004; TR-XSET-002/TR-XMVVM-001). Validation: TEST("FullyQualifiedName~Xbox.XboxSettingsViewModelTests"). Acceptance: canonical-id apply + host-canonical adoption + restart gating + non-applying validate + full revert.
407:- S27. XboxDeviceSetupViewModel attach/eject + true-drive + drive-model (R11 split a). onConsole=false. Dependencies: S21. Tests-first: XboxDeviceSetupViewModelTests (slots have expected kinds/patterns; Attach reads payload + calls host, Eject detaches; drive-model change on active true-drive rebuilds with (int)model 1542 for 1541-II; inactive-slot change no host call; enabling second true-drive disables first, single rig). RED: VMs + fakes absent. GREEN: device VMs (FR-XDEV-001/002/003; TR-XMVVM-001). Validation: TEST("FullyQualifiedName~Xbox.XboxDeviceSetupViewModelTests"). Acceptance: AvailableDriveModels==DriveModelCatalog.Implemented; 1542 flows to SetTrueDriveAsync; single-rig invariant.
409:- S28. RomProvisionEvaluator + XboxRomProvisioningViewModel (first-run provisioning) (R11 split b). onConsole=false. Dependencies: S14, S21. Tests-first: RomProvisionEvaluatorTests (NoFiles->NotProvisioned; WrongBytes->Invalid; PartialPresence->Partial; UltimaxProfile->KernalOptional) + XboxRomProvisioningViewModelTests (Import validates size+MD5, rejects oversize/mismatch without mutating state; Download requires confirm then transitions state; IsBootBlocked true until Complete). RED: evaluator/VM + fake IRomAcquirer/IStoragePicker absent. GREEN: evaluator computes state from presence+size+hash; VM enforces the 64MB ceiling + confirm-gated download + IsBootBlocked (FR-XROM-001/002; TR-XPATH-001). Validation: TEST("FullyQualifiedName~Xbox.RomProvisionEvaluatorTests|FullyQualifiedName~Xbox.XboxRomProvisioningViewModelTests"). Acceptance: states per profile; oversize/mismatch rejected state-unchanged; download only after confirm; IsBootBlocked==false only when Complete (or kernal not [... truncated (1035 chars total)]
411:- S29. XboxUiStateStore persistence to vice-sharp.ini. onConsole=false. Dependencies: S14. Tests-first: XboxUiStateStoreTests (Save then Load round-trips all 8 prefs; writes only [ViceSharpXbox] leaving vice.ini untouched; missing file -> defaults). RED: store absent. GREEN: store persists via ViceSettings.SetViceSharp/Save into [ViceSharpXbox] in a temp dir and reloads identical values (FR-XSET-005; TR-XPERSIST-001). Validation: TEST("FullyQualifiedName~Xbox.XboxUiStateStoreTests"). Acceptance: round-trip equality; only vice-sharp.ini changes; defaults when absent.
413:- S30. Home/InputMapping/About page ViewModels (About GPL disclosure). onConsole=false. Dependencies: S19. Tests-first: TEST-XBOXUI-008 (AboutViewModel: LicenseIdentifier=="GPL-2.0-or-later", AttributionText contains "VICE", SourceOfferText non-empty + URL) + Home/InputMapping bindability. RED: page VMs absent. GREEN: page VMs with fixed disclosure strings (FR-XBOXUI-008; TR-XBOXUI-008). Validation: TEST("FullyQualifiedName~AboutViewModel|FullyQualifiedName~HomeViewModel|FullyQualifiedName~InputMappingViewModel"). Acceptance: About discloses GPL/VICE/source URL.
415:- S31. Package.appxmanifest (Windows.Xbox + capability rule) validated off-console. onConsole=false. Dependencies: S4. Tests-first: TEST-XBOXPKG-003 (Manifest parses; targets Windows.Xbox Min 10.0.19041.0 MaxTested 10.0.26100.0; x64; declares internetClient iff the download path is kept, else no restricted capability; manifest ItemGroup head-only guarded so the fallback build ignores it). RED: manifest absent. GREEN: manifest exists + parses + Windows.Xbox + capability rule; inert on the net10.0 fallback (FR-XBOXPKG-003; TR-XBOXPKG-002). Validation: TEST("FullyQualifiedName~XboxManifest"); BUILD. Acceptance: manifest asserts Xbox device family + capability rule; slnx build unaffected.
417:- S32. GPL compliance: license + vkm Content + THIRD_PARTY_NOTICES + no-ROM payload (R8). onConsole=false. Dependencies: S31. Tests-first: TEST-XBOXGPL-001 (staging includes COPYING + THIRD_PARTY_NOTICES.md under Licenses/ with vkm GPL attribution; *.vkm present under Assets\vice-data\C64; payload contains no kernal-*/basic-*/chargen-*.bin; a compiled SourceUrl constant exists). RED: license/vkm/ROM-exclusion/source-URL not staged. GREEN: head-only Content copies license + vkm; no-ROM payload allows vkm; SourceUrl compiled in (FR-XBOXGPL-006; TR-XBOXCI-005). Validation: TEST("FullyQualifiedName~XboxGpl"); `pwsh ./build.ps1 ValidateXbox`. Acceptance: ROM-free payload with vkm allowed; license notices + source URL present.
419:- S33. Nuke targets (PublishXbox/DeployXbox/ValidateXbox) + CI-exclusion + rollback doc. onConsole=false. Dependencies: S32. Tests-first: XboxNukeConfigTests (Build.cs declares the three targets with params/paths) + XboxCiConfigTests (azure-pipelines.ci.yml/release.yml name no PublishXbox/DeployXbox) + a rollback-doc presence assertion. RED: targets/CI-exclusion/rollback doc absent. GREEN: targets defined; device targets absent from both CI arrays; ValidateXbox runs off-console; rollback step (WinAppDeployCmd uninstall + prior-MSIX redeploy) documented (TR-XBOXCI-005; R8 rollback). Validation: TEST("FullyQualifiedName~XboxNukeConfig|FullyQualifiedName~XboxCiConfig"); `pwsh ./build.ps1 ValidateXbox`. Acceptance: ValidateXbox exit 0 on a workload-less agent; existing CI/release jobs unchanged; rollback documented.
421:- S34. UWP XAML head build: pages, controls, Win2D video sink, XAudio2 wiring, WinRT gamepad source, App composition + adapter, focus visuals. onConsole=false (workload-dependent build). Dependencies: S18, S20, S21, S22, S23, S25, S26, S27, S28, S29, S30, S31. Tests-first: XboxUwpHeadShellTests (raw-file-read: TFM net10.0-windows10.0.26100.0, UseUwp, PublishAot(Release), DisableRuntimeMarshalling, TargetDeviceFamily Windows.Xbox, RequiresPointer=Never; references ViewModels/Xbox.Input/Abstractions/Core/Chips/Architectures/Host.InProcess and NOT Host/Avalonia/Protocol-Grpc/Monitor). RED: head XAML/App/adapter absent. GREEN: create six Pages + VideoSurfaceHost (Win2D SwapChainPanel) + VirtualKeyboardOverlay + QuickMenuOverlay + FocusVisuals.xaml + App.xaml.cs composition + the adapter implementing the ViewModels seams over IConsoleEmulatorHost + WinRT gamepad source + XAudio2 wiring; `dotnet build src/ViceSharp.Xbox -p:ViceSharpXboxUwp=true` on a workload-equipped machine/CI. Validat [... truncated (1035 chars total)]
423:- S35. Desktop-intact + full-suite + AOT regression guard. onConsole=false. Dependencies: all off-console slices to date. Tests-first: TEST-TESTGATE-009 DesktopRegressionGuard (slnx Release build; Category=Determinism 0 failed at baseline count; AvaloniaBoundaryTests green) + TEST-XBOXAOT-001 re-run + XboxTestConventionTests (no ViceFact/Assert.Skip in Category=Xbox scope). RED: a prior slice regressed the desktop build/determinism/boundary or reintroduced a trim/AOT warning. GREEN: all guards green (FR-TESTGATE-002/003; TR-XBOXTOPO-003). Validation: BUILD; DET; TEST("FullyQualifiedName~Avalonia"); AOT; TEST("FullyQualifiedName~XboxTestConvention"). Acceptance: Determinism pass-count == baseline; AvaloniaBoundaryTests green; AOT 0 warnings; Category=Xbox Skipped=0.
425:- S36. On-console deploy + boot-to-menu + C64 render smoke. onConsole=true. Dependencies: S0, S34. Tests-first: on-console checklist (deploy Release MSIX via Dev Mode; boot to the 10-foot menu with XY focus; render a live C64 frame). RED: MSIX fails to deploy or does not reach the menu / render. GREEN: app sideloads, boots to menu with controller focus, renders a live C64 frame (FR-XBOXUI-001/002; FR-XVIDEO-001). Validation: on-console deploy + checklist. Acceptance: MSIX deploys clean; menu focus-navigable; C64 frame visible.
427:- S37. On-console live gamepad smoke (JOY2/JOY1 + A/B fire + menu neutralization + swap). onConsole=true. Dependencies: S36. Tests-first: input-telemetry checklist (L stick+DPad drive JOY2, R stick JOY1, A/B fire respective ports; menu open forces neutral + A does not fire; L3 swaps; Guide does not affect the app; disconnect centers both ports). RED: mapping/adapter defect. GREEN: all directions/fire/gating/swap confirmed via the overlay (TEST-GAMEPAD-002; TEST-SYSBTN-DEVICE; FR-CTX-002). Validation: on-console overlay checklist. Acceptance: 8 directions per port; A->JOY2 fire, B->JOY1 fire; menu neutralization; Guide inert.
429:- S38. On-console A/V smoke (Win2D video + XAudio2 audio + mute + suspend/resume). onConsole=true. Dependencies: S36. Tests-first: A/V checklist (boot C64: frame-on-TV letterboxed correct colors no tear; audible SID; mute toggles silence; volume scales; Suspend/Resume does not deadlock the worker or corrupt state). RED: no audio (AppContainer path broken) or video stutter/black. GREEN: continuous Win2D video + audible XAudio2 audio, mute/volume honored, clean suspend/resume (FR-XVIDEO-001; FR-XAUDIO-001/002/003). Validation: on-console checklist + capture receipts. Acceptance: image correct; SID audible; mute silences; suspend/resume clean.
431:- S39. On-console system-button + context smoke. onConsole=true. Dependencies: S36. Tests-first: checklist (Menu opens/closes menu; View toggles virtual keyboard; X autostarts drive 8; Y warm-reset behind confirm; LT warps while held; L3 swaps sticks; A fires JOY2 only in gameplay, never in a menu; RESTORE tile fires the NMI). RED: buttons do nothing / conversion untested / RESTORE unwired. GREEN: end-to-end button + context behavior on hardware (TEST-SYSBTN-DEVICE; FR-XKBD-001). Validation: on-console checklist. Acceptance: all system-button behaviors + RESTORE confirmed on device.
433:- S40. On-console first-run ROM provisioning + settings persistence + device attach E2E (R11 split c). onConsole=true. Dependencies: S36. Tests-first: checklist (first-run wizard acquires the 3 core ROMs (download or USB) into LocalFolder\vice\C64; boot reaches READY; a palette/scale change survives an app relaunch (vice.ini + [ViceSharpXbox]); attach a d64 with a working 1541/1540/1541-II selector; all controller-only within the TV-safe area). RED: no Settings/Device UI or boot blocked with no way to provision. GREEN: provisioning + persistence + attach E2E on device (FR-XROM-001/002/003; FR-XSET-001/005; FR-XDEV-001/002). Validation: on-console checklist. Acceptance: ROMs provisioned + boot to READY; setting persists across relaunch; d64 attach with drive-model selector; controller-only XY nav.
435:- S41. On-console performance within resource budget (PAL 50). onConsole=true. Dependencies: S36. Tests-first: perf checklist (sustained target fps over a fixed workload within ~1GB RAM / 2-4 shared cores / 45% GPU headroom). RED: exceeds RAM/cores or drops below target fps. GREEN: sustained region-appropriate fps within budget (default PAL 50; see operator-confirm item 2). Validation: on-console fixed-workload benchmark + Dev Mode tooling capture. Acceptance: sustained fps; RAM under ~1GB; no CPU-starvation/thermal throttle.
437:Slice-count summary: 42 slices S0..S41; 7 are onConsole=true (S0 feasibility go/no-go, S36-S41 device validation); the other 35 are off-console gated on plain net10.0. On-console work is isolated behind S0 and the off-console deps.
439:## 7. Risks and mitigations (merged, deduped)
442:- Windows App SDK / UWP workload absent on CI and most dev boxes. Mitigation: the `ViceSharpXboxUwp=false` net10.0 fallback keeps `dotnet build ViceSharp.slnx` green everywhere; S34 XAML build + all device slices run only on the operator's equipped machine/console; the harness references the portable libs, never the head.
444:- AOT/trim forbids runtime reflection; a stray `ConfigurationBinder.Get<T>`, YamlDotNet reflective path, or shipped ViceNative/WinMm call would emit IL2xxx/IL3xxx (fatal under TreatWarningsAsErrors). Mitigation: S5/S35 AOT publish gate + the reflection-free audit; core kept as-is (already IsAotCompatible); DisableRuntimeMarshalling on the head.
447:- Determinism regression from the pacing-gate swap or the audio ring. Mitigation: single wall-clock gate never touches per-step cycle counts; DET re-run at S1/S2/S3/S11/S17/S23/S24/S35 with pass-count == baseline; determinism tests bypass the gate via StepFramesDeterministic.
450:- WinRT GamepadButtons bit drift or thumbstick-Y sign assumption. Mitigation: S7 pins the WinRT-identical bit values with an assert; S37 device smoke is the authoritative sign check and can flip a single bridge constant.
452:- MasterAudioControl relocation changes assembly identity (reflection/string lookups on the old namespace would break). Mitigation: the location test + solution build guard; relocation sequenced (S6) before ViewModels bind (S26) and before the XAudio2 backend (S17/S18).
453:- GPL vs Microsoft Store section-6 conflict; ROM copyright. Mitigation: MVP is Dev-Mode sideload + GitHub source-with-release; Store deferred behind a documented legal review; ROM-free payload enforced by test; vkm GPL data packaged with attribution.
455:- ApplicationData.Current.LocalFolder semantics under AOT UWP on console only fully confirmed on device. Mitigation: isolated in XboxDataPathBridge; S40 device slice confirms; resolver stays unchanged and env-bridged.
458:## 8. Rollout, deploy, rollback, CI coexistence, GPL
462:Rollback / uninstall (R8): to revert a bad sideload, `WinAppDeployCmd uninstall -package <pkgfullname> -ip <ip> -pin <pin>` (or Device Portal remove), then redeploy the prior known-good MSIX (`WinAppDeployCmd update`/`install`). This is documented in `src/ViceSharp.Xbox/README.md` and asserted present by S33. The desktop build is unaffected by any console deploy (additive design).
466:GPL-2.0-or-later source availability: the public repo satisfies section-3 corresponding-source; additionally the MSIX bundles `COPYING` + `THIRD_PARTY_NOTICES.md`, the About screen surfaces the compiled `SourceUrl`, and each GitHub release attaches the MSIX plus a `git archive` source tarball of the tagged commit. The shipped GPL VICE `*.vkm` keymap data is packaged as Content under `Assets\vice-data\C64\` with explicit GPL attribution in `THIRD_PARTY_NOTICES.md` (confirm vkm redistributability). ROMs are never bundled (Commodore copyright); they are user-provided (USB import) or optionally fetched over verified HTTPS (requires `internetClient`). Microsoft Store submission is deferred pending a documented GPL section-6 (no-further-restrictions) review vs the Store Standard Application License Terms.
468:## 9. Operator-confirm items (defaults chosen; one-line yes/no each)
471:2. Region/fps target for the S41 perf budget = PAL 50 fps (VICE x64sc default)? Proposed default: PAL 50.
472:3. ROM acquisition: USB/local import as the primary path, plus an OPTIONAL verified-HTTPS download that requires the `internetClient` capability? Proposed default: yes, USB primary + optional HTTPS download with `internetClient`.
474:5. Add the dedicated RESTORE/NMI seam (`IMachineKeyboardInput.SetRestoreState(bool)` + facade `SetRestoreState`) under FR-XKBD so the virtual keyboard's RESTORE tile works, rather than shipping the tile disabled? Proposed default: yes, add the seam.
475:6. Who installs the windows-app (UWP) workload and on which machine/CI runner for S34 (XAML build) and the device slices, and what is the MSIX signing approach for sideload (self-signed dev cert vs Partner Center-issued)? Proposed default: operator's equipped Windows box + a self-signed dev cert trusted on the console for MVP.
477:## 10. Residual open items still needing cross-team reconciliation
479:- Xbox.ViewModels reference surface: R12/tenfoot said "Abstractions-only", but the settings gateway uses Protocol DTOs and `SettingsOptionCatalog` lives in Protocol, so this plan permits `ViceSharp.Xbox.ViewModels` to reference Abstractions + Protocol (+ Xbox.Input). Both are portable, Kestrel-free, AOT-safe, but this is a deliberate refinement of R12 that should be ratified (alternative: split the settings/device/ROM VMs into a Protocol-referencing sub-lib and keep the pure UI VMs Abstractions-only).
</workspace_result>

### Turn 82 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 28 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
92:        var nmiLine = new InterruptLine(InterruptType.Nmi);

F:\GitHub\vice-sharp\src\ViceSharp.Protocol\ProtocolContracts.cs
804:    public const string RestoreSnapshot = "RestoreSnapshot";
813:    ValueTask<RestoreSnapshotResponse> RestoreSnapshotAsync(
814:        RestoreSnapshotRequest request,
820:public sealed record RestoreSnapshotRequest(string SessionId, SnapshotDto Snapshot);
826:public sealed record RestoreSnapshotResponse(

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
526:    /// Restores every bindable field to the last-applied baseline, clears the validation
534:            RestoreBaseline(_baseline);
636:    private void RestoreBaseline(SettingsBaseline baseline)

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\VirtualKeyEntry.cs
16:/// the <see cref="AppKeyKind.Restore"/> tile this value is not injected as a key (the

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\VirtualKeyboardViewModel.cs
33:///   <item><description><see cref="AppKeyKind.Restore"/>: drives the dedicated RESTORE/NMI
34:///   seam <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> asserted then released,
188:            case AppKeyKind.Restore:
190:                _keyboard.SetRestoreState(true);
191:                _keyboard.SetRestoreState(false);

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
130:    public void Nmi()

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\VirtualKeyboardLayout.cs
102:                    new VirtualKeyEntry("Restore", "RESTORE", IsWide: false, AppKeyKind.Restore),

F:\GitHub\vice-sharp\src\ViceSharp.Core\CartPortInterSystemBus.cs
36:    public const string Nmi = "NMI";
47:    private static readonly string[] DefaultSignals = { Game, ExRom, Irq, Nmi, Dma, Reset };

F:\GitHub\vice-sharp\src\ViceSharp.Core\C64MemoryMap.cs
585:    public bool SetRestoreState(bool pressed)
594:            _keyboard.SetRestore(pressed);
632:    public bool IsRestorePressed
637:                return _keyboard.IsRestorePressed;

F:\GitHub\vice-sharp\src\ViceSharp.Core\Input\C64KeyboardMatrix.cs
71:            // RESTORE/NMI trigger is driven exclusively via SetRestore(bool).
83:    public void SetRestore(bool pressed)
91:    public bool IsRestorePressed => _restoreKeyPressed;

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\AppKeyKind.cs
33:    /// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetRestoreState(bool)"/>
39:    Restore = 2,

</workspace_result>

### Turn 83 (Function)
[tool_response]
1→---
name: project-xbox-uwp-app
description: "Xbox UWP app for ViceSharp (controller->C64 joystick + 10-foot UI); approved BDP plan, branch feat/xbox-uwp-app, S0 validates on dev PC not console"
metadata: 
  node_type: memory
  type: project
  originSessionId: 0aad25e1-e13c-4add-b10f-4a71e775e9f1
---

10→Building a true UWP-on-Xbox-console head for ViceSharp: Xbox controller -> C64 joysticks (left stick+D-pad JOY2, right stick JOY1, A=JOY2 fire, B=JOY1 fire), system buttons -> app functions (View/Menu/X/Y/bumpers/triggers/stick-press; Guide shell-reserved), 10-foot UWP-XAML UI. Approved 2026-07-13. Branch **feat/xbox-uwp-app** off main; plan + runbook committed to `docs/plans/PLAN-XBOXUWP-BDPv4.md` and `docs/xbox/on-console-setup-runbook.md` (NOT pushed - Azure DevOps origin only, on request). Operator's installer WIP is in `stash@{0}` on the drive-family branch.

Architecture (4 new projects + 1 refactor): **ViceSharp.Xbox** (UWP XAML head, net10.0-windows10.0.26100.0 + UseUwp, Native AOT Release / JIT Debug, `ViceSharpXboxUwp` conditional TFM with net10.0 fallback for workload-absent CI); **ViceSharp.Host.InProcess** (Kestrel-free POCO host EXTRACTED from ViceSharp.Host = slice S1; refinement: the P/Invoke-heavy winmm/waitable-timer/thread-affinity gates + WinMmAudioBackend STAY in ViceSharp.Host as desktop-only IEmulationGate/IAudioBackend impls so Host.InProcess is P/Invoke-free); **ViceSharp.Xbox.Input** (portable, zero-WinRT; WinRT-identical GamepadButtonFlags bit layout, pure allocation-free XboxJoystickMapper + single-owner XboxInputContext state machine); **ViceSharp.Xbox.ViewModels** (references Abstractions + Protocol only). Win2D SwapChainPanel video sink, ring-buffered XAudio2 audio (audio is a DECOUPLED sink, NOT the clock), single managed XboxManagedFrameGate pacing (determinism preserved). Existing desktop Avalonia + WiX + the ViceSharp.Host.Xbox Avalonia scaffold are UNTOUCHED (additive). RESTORE key = new `IMachineKeyboardInput.SetRestoreState` seam (FR-XKBD), not SetKeyState. ROMs download from the VICE-Team/svn-mirror GitHub mirror via RomProvider (internetClient capability), USB import fallback.

MCP: 58 FR / 43 TR / 44 TEST / 58 FR->TR/TEST mapping rows registered + verified persisting (areas XBOXTOPO GAMEPAD SYSBTN CTX INPROC XVIDEO XAUDIO XAV XBOXUI XSET XDEV XROM XKBD XBOXPKG XBOXGPL TESTGATE). Slice TODOs IMPL-XBOXUWP-000..042 (section xbox-uwp). NOTE: the MCP requirements/TODO surface persists reliably here, unlike the flaky session-log path.

**Validation TIERS** (operator clarified 2026-07-13, key insight): UWP + Windows.Gaming.Input + Win2D + XAudio2 all run on a Windows 11 dev PC, so the WHOLE app incl. live Xbox-controller input validates on the dev machine - the physical console is needed ONLY at Microsoft Store submission. Tier H = headless/CI (dotnet build/test here, ~35 of 42 slices). Tier D = dev-PC UWP (needs windows-app workload + interactive session + controller; S0 feasibility + old on-console smokes). Tier C = console/Store DEFERRED and tracked (AppContainer ~1GB/2-4core/45%GPU budget, on-console PAL-50 perf, real-TV overscan/focus-engagement, Guide reservation, Store cert) = new slice S42. Do NOT gate development on the console.

STATUS 2026-07-13 (goal "100% plan completion"): ALL 35 Tier-H slices done, green, committed on feat/xbox-uwp-app (S1 host extraction .. S35 regression guard; ~37 commits, NOT pushed). Complete subsystems: Host.InProcess (Kestrel-free) + ConsoleHost facade + XboxManagedFrameGate; full portable input lib (mapper/converter/context machine/bindings/dispatcher, all pure+tested); XAudio2 backend (AOT-clean interop) + factory + ring math; data-path bridge + settings catalog + UI-state store; all 12 ViewModels; appxmanifest + GPL (COPYING+notices+46 vendored vkm, no-ROM) + Nuke PublishXbox/DeployXbox/ValidateXbox; UWP XAML head (S34) realized (#if HAS_UWP-guarded). Proven off-console: Native-AOT link 0 IL warnings + native exe (S5/S18/S35); 306+ Category=Xbox tests 0-fail 0-skip; determinism + Avalonia boundary intact (desktop not regressed - the 8 full-suite fails are pre-existing VIC/flaky/native-residue, proven by empty-diff-vs-main).

20→FIX-XWIN2D-001 DONE + VERIFIED 2026-07-13 (commit 3e4b030): replaced Win2D with a raw-DX11 SwapChainPanel path (D3D11CreateDevice/CreateDXGIFactory2 LibraryImport + blittable vtable calls, mirrors XAudio2 backend), removed Win2D.uwp entirely, NETSDK1130 gone. KEY: the UWP head BUILDS clean via **VS 18 MSBuild.exe** (`MSBuild.exe src\ViceSharp.Xbox -p:ViceSharpXboxUwp=true /t:Restore,Build` -> exit 0, 0 warn under TreatWarningsAsErrors) but NOT `dotnet build` (dotnet SDK MSBuild does not import the UWP XAML markup compiler via ImportBefore/ImportAfter; VS MSBuild does; genxbf + ModernNET XAML targets ship with VS 18 Community - NOT a missing component). So the "UWP head compiles" half of S0 is now PROVEN in this env; use VS MSBuild not the dotnet CLI.

REMAINING = 7 Tier D/C slices NOT executable headless (need interactive GPU session + physical Xbox controller + [S42] console): S0 (build half PROVEN now; residual = AOT-MSIX unknown [VS-MSBuild Publish+PublishAot produced only a managed apphost, ILC did NOT run - plan already allows JIT Debug + net10.0 fallback] + the run/render/controller smoke), S36-S41 (dev-PC live smokes: boot/render, gamepad, A/V, system-buttons, first-run ROM, perf), S42 (Tier C console/Store, deferred). All annotated HANDOFF in MCP. Remaining follow-up: FIX-XKBDNMI-001 (RESTORE trigger->CPU NMI edge unwired). New unrelated high TODO added: IMPL-WINGET-001 (integrate winget manifest validation into PublishWinget per microsoft/winget-pkgs ValidationFailureGuide).

STATUS 2026-07-14 (dev-PC bring-up day, ~15 more commits on feat/xbox-uwp-app, NOT pushed): the head now RUNS AND RENDERS the C64 on the operator's dev PC (S36 boot/render smoke effectively validated by operator screenshots + LocalState\vicesharp.log receipts). Defect chain found+fixed via on-device logs: (1) FIX-XRENDERCRASH-001: unguarded RenderTick killed the app ~8s in (exit code 1); guard + ILogger Debug/Console/FileLoggerProvider (LocalState\vicesharp.log) + global exception handlers (621425a), per-HRESULT DX11 init tracing (04afc9a). (2) THE black-screen root cause: IidSwapChainPanelNative was the WinUI3 GUID (63aad0b8-...); a Windows.UI.Xaml (UseUwp) panel implements the UWP dxinterop interface F92F19D2-3ADE-45A6-A20C-F6F1EA90554B (SDK header line 854) -> E_NOINTERFACE 0x80004002. Fixed aebfdc6 + guard test bans the WinUI3 GUID file-wide. (3) FIX-XCOMPSCALE-001 294898e: swap chain is PHYSICAL pixels, XAML composition scales it AGAIN by CompositionScale (2.5 -> 2.5x-oversized top-left crop); fix = IDXGISwapChain2::SetMatrixTransform (slot 34, IID a8be2ac4-...) with 1/scale. (4) FIX-XASPECT-001 6747765: fill window (VIC border IS the TV padding, operator: no extra 90% inset) + true composite PAR from VideoRenderer.GetPixelAspectRatio (VICE vicii_get_pixel_aspect: PAL 0.93650794, NTSC 0.75); portable VideoDisplayGeometry.ComputeDrawRect; facade GetVideoStandard reads the LIVE session. (5) FIX-XASPECT-002 43b7d4f BOTH heads: Xbox SettingsPage guard compared pre/post-apply SelectedProfileId (picker already set it -> never fired; now keys off RequiresRestart); Avalonia VideoSurface NEVER applied PAR at all (aspect setting was a no-op; now ComputeDisplayAspect honors Square/VICE/4:3 + MainWindow feeds profile standard + resizes natural width under the Uniform Viewbox). (6) FEAT-XPERFHUD-001 edb4c66+83c88ef+e2afeb1: letterbox perf HUD (portable VideoPerfStatsViewModel; FPS/EMU/SPD + 'NTSC 1.02MHz PAR 0.75' machine line after operator misread bare 0.75 as clock) + Settings toggle (LocalSettings ShowPerfHud, live, default ON). (7) FEAT-XSETPERSIST-001 ddfab3b: real-time settings persistence (XboxSettingsStore, source-gen JSON, AOT-safe) -> LocalState\settings.json written on every successful apply (receipt: file timestamp == NTSC apply second); boot reuses persisted ProfileId via ConsoleSessionOptions + live re-apply. (8) FIX-XNTSCFILL-001 9e228b4: VIC frame is fixed 384x272; NTSC writes only rows 0..245 (VideoRenderer.GetContentLines: raster-16, NTSC 246/PAL 272); surface crops top-anchored so NTSC fills the window; + mouse-friendly menu dismiss (HomePage Close Menu button + click-outside-card -> App.DismissMenu, NO host call). ON-DEVICE VALIDATED by operator: IID fix (first frames), comp-scale (correct geometry), NTSC aspect+crop wiring (log contentRows=246, pixelAspect=0.75, clockHz=1022730), settings.json persistence. Operator visual sign-off: 'Looks fine, just slow' (Debug-UWP JIT ~29% SPD on NTSC - known Debug perf, Release/AOT is the perf config). PENDING operator validation: persisted-profile relaunch (expect 'persisted profile: ntsc' at boot), menu dismiss, NTSC full-height fill, HUD toggle. New high TODOs: PLAN-XKEYBOARD-001 (real C64 keyboard: slide-up from bottom, SHRINK emulator not overlay, F-keys right column), PLAN-ROMM-001 (RomM client lib + UI both heads). AUTONOMOUS ON-DEVICE LOOP (operator 2026-07-14: 'If you have the ability to launch the app and read logs, then you don't need me holding your hand'): this session RUNS ON the dev PC, so validate without him: (1) build head via VS MSBuild /p:Configuration=Debug-UWP /p:Platform=x64 (outputs bin\x64\Debug-UWP\...); (2) the REGISTERED loose-file layout is bin\Debug-UWP\net10.0-windows10.0.26100.0\win-x64\AppX (Get-AppxPackage sharpninja.ViceSharp.Xbox InstallLocation; VS-built, path has NO x64 segment and CLI cannot reproduce it via Platform tweaks) - refresh it with robocopy <x64 output> <AppX> /S /XO /XF AppxManifest.xml; (3) launch: Start-Process 'shell:AppsFolder\sharpninja.ViceSharp.Xbox_wbxy578d68v3y!App'; (4) sleep ~14s, read LocalState\vicesharp.log + settings.json; (5) Stop-Process the ViceSharp.Xbox pid I started (scoped kill OK). Validated this way 2026-07-14 13:28: persisted-profile relaunch (boot 'persisted profile: ntsc' + re-applied success=True + NTSC aspect at boot + first frame tick 1). Still operator-only: physical controller input, visual quality judgment, audio listening, Release perf, S42 console. GOTCHAS for this env: dotnet CLI builds flip src\ViceSharp.Xbox\obj\project.assets.json to the net10.0 fallback TFM -> operator's VS Debug-UWP build then fails NETSDK1005 until a Debug-UWP restore; ALWAYS end CLI gate batches with MSBuild /t:Restore /p:Configuration=Debug-UWP;Platform=x64, and avoid CLI builds while his VS is active (12:56 restore-contention build failure). Valid VS pair is Debug-UWP|x64 (Any CPU is build-disabled for the head). App log lives at %LOCALAPPDATA%\Packages\sharpninja.ViceSharp.Xbox_wbxy578d68v3y\LocalState\vicesharp.log (rewritten each launch).

STATUS 2026-07-14 EVENING (Release-UWP loop day; branch now ~60 commits, NOT pushed): deploy loop = `./build.ps1 DeployXboxLocal` (stop app -> VS MSBuild `/p:Configuration=Release-UWP /p:Platform=x64 /restore /t:Build,GenerateProjectPriFile` -> robocopy to registered AppX layout -> shell:AppsFolder launch). TWO deploy-tool traps fixed: resources.pri MUST regenerate (UWP loads page XAML from pri; incremental Build leaves it stale = old UI ships) and restore MUST be the `/restore` SWITCH not a `/t:Restore` entry target (single evaluation reads the PRE-restore fallback nuget.g.targets -> MSIX targets not imported -> GenerateProjectPriFile MSB4057). Shipped + operator-visible: FIX-XSETBLANK-001 (blank Settings pickers = PropertyChanged raised off UI thread; captured-SynchronizationContext Post pattern, same as ROM-provisioning VM); FEAT-XKEYCAPPETSCII-001 (true PETSCII chord glyphs on keycaps: KERNAL decode tables read from ROM $EBC2/$EC03, triad recode tables uc+lc, CUS U+F1xx in PetMe64, cmap-parse coverage test = no tofu); FEAT-XMENUFOCUS/XKEYCAPSTYLE/XMENUSLIDE (Close Menu first+focused on every open, keycap styles app-level Styles/C64Keycaps.xaml incl tile style on the 66-key board, menu = full-height right-edge panel with EdgeUIThemeTransition Right); FIX-XMENUWARP-001 (BOTH pacing gates: paused session drops its anchor, else resume owes the pause as deficit and sprints at step cap; <4s pauses never hit catastrophic resync); FEAT-XKBDSTICKY-001 (state-driven virtual keyboard scanned in real time: C=/SHIFT sticky tiles hold the matrix line until next stroke completes, new CommodoreMomentary kind, SHIFT-LOCK holds LeftShift while latched, key strokes held ~80ms via overlay timer, two-line RUN\nSTOP / SHIFT\nLOCK caps, shifted/C= RunStop shows RUN); FIX-XSNAPWARP-001 (v2 machine snapshot: v1 was CPU+Bus.Write-replay which scrambled CIA ICR/latches -> music/IRQ chaos after LOAD ("unbounded music", not warp); v2 captures RAM array+color RAM via vic.VideoMemoryReader+6510 port+CPU+VIC/CIA via new symmetric CaptureSnapshotState + injects via the lockstep-proven internal injectors (Chips IVT to Host.InProcess); SID by register replay = envelopes retrigger, documented; C64 refuses v1 with resave message, non-C64 test machines keep v1; jiffy-clock-alive regression pin); FIX-XDPADSKIP-002 (120ms wall-clock throttle in HandleUiNavigate = single navigator whatever the emission source). OPEN: operator re-verify Settings page + keycap glyphs + sticky keys + slide-in menu + snapshot after one fresh SAVE (old slot is v1, LOAD logs resave message); Controls-page keyboard-shortcut config (asked, never started); PETSCII glyph legends could go on HomePage too; MCP sessionlog completeTurn STILL stuck (triage-report-44b95475979c40458709ded2e3718d15; failsafe auto-closes turns).

STATUS 2026-07-14 LATE (FIX-XKBDPANEL-001 root-cause night; ~63 commits, NOT pushed): operator's "keyboard not displaying in its panel" (+ still-blank Settings + missing menu title) had ONE root cause: **PublishAot=true at BUILD time (the Release-UWP csproj posture) flips CsWinRT into AOT binding mode and reflection {Binding} dies app-wide** - ItemsSource stays null on a healthy DataContext, NO BindingFailed fires, no exception anywhere. Proven by on-device clean A/B (same source: PublishAot=false renders all 66 tiles, =true realizes zero) after a Debug-vs-Release differential. Fix: csproj arms PublishAot ONLY on explicit `ViceSharpPublishAot=true`; Release-UWP builds stay JIT (SPD ~101%) with working bindings; store-AOT needs an AOT-safe-bindings slice (x:Bind or generated bindable metadata) BEFORE anyone re-arms it. Second trap: DeployXboxLocal robocopy `/XO` skipped older-but-different files -> config switches made FRANKENBUILD layouts (Release head over newer Debug core libs, EMU 26%); /XO removed. AUTONOMOUS UI LOOP unlocked this: computer-use MCP (grant the PROCESS basename `vicesharp.xbox.exe`, not just the Start-menu entry, or the window is masked), F9 toggles the keyboard dock, F11 the menu (F10 = Windows system key, never reaches KeyDown; ESC gets eaten in some focus states; CoreWindow.KeyDown backstop covers focus-nowhere after clicking the non-focusable SwapChainPanel), then screenshot + read vicesharp.log receipts ("keyboard open vm/keys", "keyboard layout size/buttons", "keyboard tree items/itemsSourceNull", XAML BindingFailed -> log). Settings page verified fully populated on device (my earlier FIX-XSETBLANK-001 dispatch fix was real but secondary). If the app window is on another virtual desktop, re-activate via shell:AppsFolder URI to pull it over. New high TODO: FIX-ROMLESSVIC-001 (romless VIC bad-line doc; worktree F:\GitHub\vice-sharp-romless ready). Then FEAT-XMENUSUBPAGE-001 (a199063, HEAD): UiBack is page-type-driven (B goes back on a subpage, dismisses only from the HomePage rail, navigate-home fallback for a stackless reopened frame); menu subpages expand across BOTH root-grid columns over an opaque frame backdrop and contract to the docked rail on HomePage (frame Navigated handler owns span/column/backdrop centrally); ShowMenu/HideMenu now push the gamepad InputContext explicitly (MainMenu on open, Gameplay/VirtualKeyboard on close) so a mouse/B dismissal no longer strands the controller in MainMenu with an inert joystick; NavigationFailed logs; DeviceSetup refresh guarded (async-void). Device-verified: Devices fills the window with live cards (Cartridge sblox.CRT, Connected), Back contracts to the rail. Gate Category=Xbox 501/501. HANDOFF.md refreshed 2026-07-14 23:31 with the PLAN-XBOXUWP resume snapshot before pivoting to the romless worktree.

30→Full research + reconciliation in scratchpad (xbox-plan-reconciliation.md rulings R1-R13). Plan + runbook in docs/plans/PLAN-XBOXUWP-BDPv4.md + docs/xbox/on-console-setup-runbook.md. See [[project_iteration0_status]], [[project_identity]].


### Turn 84 (Function)
[tool_response]
exit: 0
diff --git a/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml b/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
index ff3f4ba..b96f4f7 100644
--- a/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
+++ b/src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
@@ -12,6 +12,7 @@
     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
     xmlns:controls="using:ViceSharp.Xbox.Controls"
+    xmlns:vm="using:ViceSharp.Xbox.ViewModels"
     RequiresPointer="Never">
     <UserControl.Resources>
         <!-- Width units -> pixels (one key unit = 52 px at 10-foot scale). -->
@@ -33,7 +34,7 @@
             </Grid.ColumnDefinitions>
 
             <!-- The five physical rows. -->
-            <ItemsControl Grid.Column="0" ItemsSource="{Binding Rows}">
+            <ItemsControl Grid.Column="0" ItemsSource="{x:Bind ViewModel.Rows, Mode=OneWay}">
                 <ItemsControl.ItemTemplate>
                     <DataTemplate>
                         <ItemsControl ItemsSource="{Binding}" HorizontalAlignment="Center">
@@ -43,7 +44,7 @@
                                 </ItemsPanelTemplate>
                             </ItemsControl.ItemsPanel>
                             <ItemsControl.ItemTemplate>
-                                <DataTemplate>
+                                <DataTemplate x:DataType="vm:VirtualKeyEntry">
                                     <!--
                                       FEAT-XKEYCAPSTYLE-001 (operator 2026-07-14: "Apply
                                       CBM button style to the virtual keyboad"): the
@@ -51,8 +52,8 @@
                                       warm brown cap, raised-key bevel).
                                     -->
                                     <Button
-                                        Content="{Binding DisplayLabel}"
-                                        Width="{Binding EffectiveWidthUnits, Converter={StaticResource KeyWidth}}"
+                                        Content="{x:Bind DisplayLabel}"
+                                        Width="{x:Bind EffectiveWidthUnits, Converter={StaticResource KeyWidth}}"
                                         Style="{StaticResource C64KeycapTileStyle}"
                                         Click="OnKeyPressed" />
                                 </DataTemplate>
@@ -63,11 +64,11 @@
             </ItemsControl>
 
             <!-- The function-key column, on the RIGHT like the physical machine. -->
-            <ItemsControl Grid.Column="1" ItemsSource="{Binding Layout.FunctionKeys}" Margin="16,0,0,0">
+            <ItemsControl Grid.Column="1" ItemsSource="{x:Bind ViewModel.Layout.FunctionKeys, Mode=OneWay}" Margin="16,0,0,0">
                 <ItemsControl.ItemTemplate>
-                    <DataTemplate>
+                    <DataTemplate x:DataType="vm:VirtualKeyEntry">
                         <Button
-                            Content="{Binding DisplayLabel}"
+                            Content="{x:Bind DisplayLabel}"
                             Width="96"
                             Style="{StaticResource C64KeycapTileStyle}"
                             Click="OnKeyPressed" />
diff --git a/src/ViceSharp.Xbox/Views/SettingsPage.xaml b/src/ViceSharp.Xbox/Views/SettingsPage.xaml
index 82f19d2..5efcae6 100644
--- a/src/ViceSharp.Xbox/Views/SettingsPage.xaml
+++ b/src/ViceSharp.Xbox/Views/SettingsPage.xaml
@@ -20,15 +20,15 @@
 
         <ScrollViewer Grid.Row="1">
             <StackPanel Spacing="16" MaxWidth="900">
-                <ComboBox Header="Computer" ItemsSource="{Binding Computers}" DisplayMemberPath="DisplayName" SelectedItem="{Binding SelectedComputer, Mode=TwoWay}" />
-                <ComboBox Header="Model" ItemsSource="{Binding Models}" DisplayMemberPath="DisplayName" SelectedItem="{Binding SelectedModel, Mode=TwoWay}" />
+                <ComboBox Header="Computer" ItemsSource="{x:Bind ViewModel.Computers, Mode=OneWay}" DisplayMemberPath="DisplayName" SelectedItem="{x:Bind ViewModel.SelectedComputer, Mode=TwoWay}" />
+                <ComboBox Header="Model" ItemsSource="{x:Bind ViewModel.Models, Mode=OneWay}" DisplayMemberPath="DisplayName" SelectedItem="{x:Bind ViewModel.SelectedModel, Mode=TwoWay}" />
                 <TextBlock Text="Restart required to change model." FontSize="18" Foreground="#FFDDAA88" />
-                <ComboBox Header="Renderer" ItemsSource="{Binding Renderers}" SelectedItem="{Binding SelectedRenderer, Mode=TwoWay}" />
-                <ComboBox Header="Display scale" ItemsSource="{Binding DisplayScales}" SelectedItem="{Binding SelectedDisplayScale, Mode=TwoWay}" />
-                <ComboBox Header="Palette" ItemsSource="{Binding Palettes}" SelectedItem="{Binding SelectedPalette, Mode=TwoWay}" />
-                <ComboBox Header="Audio" ItemsSource="{Binding AudioModes}" SelectedItem="{Binding SelectedAudioMode, Mode=TwoWay}" />
-                <Slider Header="Master volume" Minimum="0" Maximum="100" Value="{Binding MasterVolumePercent, Mode=TwoWay}" />
-                <ToggleSwitch Header="Swap joystick ports" IsOn="{Binding SwapJoystickPorts, Mode=TwoWay}" />
+                <ComboBox Header="Renderer" ItemsSource="{x:Bind ViewModel.Renderers, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedRenderer, Mode=TwoWay}" />
+                <ComboBox Header="Display scale" ItemsSource="{x:Bind ViewModel.DisplayScales, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedDisplayScale, Mode=TwoWay}" />
+                <ComboBox Header="Palette" ItemsSource="{x:Bind ViewModel.Palettes, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedPalette, Mode=TwoWay}" />
+                <ComboBox Header="Audio" ItemsSource="{x:Bind ViewModel.AudioModes, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedAudioMode, Mode=TwoWay}" />
+                <Slider Header="Master volume" Minimum="0" Maximum="100" Value="{x:Bind ViewModel.MasterVolumePercent, Mode=TwoWay}" />
+                <ToggleSwitch Header="Swap joystick ports" IsOn="{x:Bind ViewModel.SwapJoystickPorts, Mode=TwoWay}" />
                 <!--
                   FEAT-XPERFHUD-001 toggle (operator 2026-07-14): show/hide the letterbox
                   performance counters. Applies LIVE and persists immediately (head-local
@@ -36,12 +36,12 @@
                   this head, not a host session setting.
                 -->
                 <ToggleSwitch x:Name="PerfCountersToggle" Header="Performance counters" Toggled="OnPerfCountersToggled" />
-                <TextBlock Text="{Binding StatusText}" Foreground="#FFAADDAA" />
+                <TextBlock Text="{x:Bind ViewModel.StatusText, Mode=OneWay}" Foreground="#FFAADDAA" />
             </StackPanel>
         </ScrollViewer>
 
         <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
-            <Button Content="Apply" Style="{StaticResource TenFootButtonStyle}" IsEnabled="{Binding IsDirty}" Click="OnApply" />
+            <Button Content="Apply" Style="{StaticResource TenFootButtonStyle}" IsEnabled="{x:Bind ViewModel.IsDirty, Mode=OneWay}" Click="OnApply" />
             <Button Content="Revert" Style="{StaticResource TenFootButtonStyle}" Click="OnRevert" />
             <Button Content="Back" Style="{StaticResource TenFootButtonStyle}" Click="OnBack" />
         </StackPanel>
--- pipeline ---
diff --git a/azure-pipelines.ci.yml b/azure-pipelines.ci.yml
index c6721d1..b71ba8d 100644
--- a/azure-pipelines.ci.yml
+++ b/azure-pipelines.ci.yml
@@ -40,3 +40,29 @@ stages:
             displayName: 'Run: CiTest'
             inputs:
               script: './build.cmd CiTest --skip'
+

+      - pwsh: |
+          if (-not $env:OCTOPUS_API_KEY) {
+            Write-Host 'OCTOPUS_API_KEY not set; skipping Octopus LEGION2 release'
+            exit 0
+          }
+          $url = if ($env:OCTOPUS_URL) { $env:OCTOPUS_URL.TrimEnd('/') } else { 'http://PAYTON-LEGION2:8066' }
+          $env:OCTOPUS_URL = $url
+          $env:OCTOPUS_SPACE = if ($env:OCTOPUS_SPACE) { $env:OCTOPUS_SPACE } else { 'Default' }
+          $project = 'vice-sharp'
+          $version = ('$(Build.BuildNumber)' -replace '[^0-9A-Za-z\.\-_+]', '-')
+          if ([string]::IsNullOrWhiteSpace($version)) { $version = '0.0.$(Build.BuildId)' }
+          $octo = 'C:\Program Files\Octopus CLI\octopus.exe'
+          if (-not (Test-Path $octo)) { throw "octopus CLI missing: $octo" }
+          Write-Host "Octopus $url project=$project version=$version"
+          & $octo release create --project $project --version $version --packageVersion $version --releaseNotes "ADO $(Build.DefinitionName) $(Build.SourceVersion)" --ignoreExisting --no-prompt
+          if ($LASTEXITCODE -ne 0) { throw "octopus release create failed exit $LASTEXITCODE" }
+          & $octo release deploy --project $project --version $version --deployTo Development --progress --no-prompt
+          if ($LASTEXITCODE -ne 0) { Write-Warning "octopus deploy Development exit $LASTEXITCODE (release still created)" }
+        displayName: Octopus LEGION2 release
+        condition: and(succeeded(), ne(variables['SkipOctopus'], 'true'))
+        env:
+          OCTOPUS_API_KEY: $(OCTOPUS_API_KEY)
+          OCTOPUS_URL: $(OCTOPUS_URL)
+          OCTOPUS_SPACE: $(OCTOPUS_SPACE)

+


### Turn 85 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\Input\C64KeyboardMatrix.cs
- offset: 60
- limit: 50
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\C64MemoryMap.cs
- offset: 580
- limit: 70
[tool_request: grep]
- pattern: SetRestoreState|SetRestore
- path: F:\GitHub\vice-sharp
- glob: *.cs
- head_limit: 40
[tool_request: grep]
- pattern: BindingProfile|IBindingStore|keyboard.?shortcut|Hotkey|Shortcut
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox
- glob: *.{cs,xaml}
- head_limit: 40
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$ids = @('FIX-XKBDNMI-001','PLAN-XKEYBOARD-001','IMPL-XBOXUWP-000','IMPL-XBOXUWP-035','IMPL-XBOXUWP-036','IMPL-XBOXUWP-037','IMPL-XBOXUWP-038','IMPL-XBOXUWP-039','IMPL-XBOXUWP-040','IMPL-XBOXUWP-041','IMPL-XBOXUWP-042','PLAN-XBOXCLIENT-001')\nforeach ($id in $ids) {\n  $r = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.get' -ParamsObject @{ id = $id } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' 2>&1 | Out-String\n  if ($r -match 'done:\\s*(\\w+)') { $done = $Matches[1] } else { $done = '?' }\n  if ($r -match 'priority:\\s*(\\w+)') { $pri = $Matches[1] } else { $pri = '?' }\n  if ($r -match 'title:\\s*(.+)') { $title = $Matches[1].Trim() } else { $title = '?' }\n  Write-Output \"$id | done=$done | pri=$pri | $title\"\n}"}
[tool_request: grep]
- pattern: RomProvision|Models|SelectedModel|IsAvailable|per-model
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxRomProvisioningViewModel.cs
- head_limit: 40
[tool_request: grep]
- pattern: RomProvision|AvailableModels|IsAvailable|Kernal
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels
- glob: *Rom*
- head_limit: 40

### Turn 86 (Function)
[tool_response]
580→
        return true;
    }

    /// <inheritdoc />
    public bool SetRestoreState(bool pressed)
    {
        if (!_keyboardEnabled)
            return false;

590→        // RESTORE is a hardware NMI wired directly to the CPU, not a key-matrix key.
        // Route straight to the dedicated RESTORE/NMI trigger on the matrix, never
        // through SetKeyState / the keymap.
        lock (_inputSync)
            _keyboard.SetRestore(pressed);

        return true;
    }

    public bool SetJoystickState(int controlPort, byte directionMask, bool fireButton)
600→    {
        var port = controlPort switch
        {
            1 => _joystickPort1,
            2 => _joystickPort2,
            _ => null
        };

        if (port is null)
            return false;
610→
        var pressed = (C64JoystickPort.JoystickButtons)(directionMask & 0x0F);
        if (fireButton)
            pressed |= C64JoystickPort.JoystickButtons.Fire;

        lock (_inputSync)
            port.State = pressed;
        return true;
    }

620→    public void SelectKeyboardMap(IKeyboardInputMap keyboardMap)
    {
        ArgumentNullException.ThrowIfNull(keyboardMap);

        lock (_inputSync)
        {
            _keyboardMap = keyboardMap;
            _keyboardKeyPressCounts.Clear();
            _keyboard.ClearKeys();
        }
630→    }

    public bool IsRestorePressed
    {
        get
        {
            lock (_inputSync)
                return _keyboard.IsRestorePressed;
        }
    }
640→
    public bool IsStopPressed
    {
        get
        {
            lock (_inputSync)
                return _keyboard.IsStopPressed;
        }
    }


### Turn 87 (Function)
[tool_response]
60→        int col = keyCode & 0x07;

        if (row < 8 && col < 8)
        {
            // VICE-style: RUN/STOP is row 7, col 7 (0x3F)
            if (keyCode == 0x3F) _stopKeyPressed = pressed;

            // NOTE: RESTORE is deliberately NOT handled here. On real C64 hardware
            // RESTORE is not part of the 8x8 matrix at all: it is wired straight to
            // the CPU NMI line through a monostable. Keycode 0x31 is the ordinary "*"
70→            // matrix key (row 6, col 1) and must not fire RESTORE. The dedicated
            // RESTORE/NMI trigger is driven exclusively via SetRestore(bool).
            _matrix[row, col] = pressed;
        }
    }

    /// <summary>
    /// Set the dedicated RESTORE line state. RESTORE is not a key-matrix key on a C64:
    /// it is wired directly to the CPU NMI line, so this drives ONLY the RESTORE/NMI
    /// trigger and never a matrix cell (VICE handles RESTORE the same way, outside the
80→    /// matrix scan).
    /// </summary>
    /// <param name="pressed">True to assert RESTORE (press), false to release.</param>
    public void SetRestore(bool pressed)
    {
        _restoreKeyPressed = pressed;
    }

    /// <summary>
    /// Check if RESTORE key is pressed (triggers NMI in VICE)
90→    /// </summary>
    public bool IsRestorePressed => _restoreKeyPressed;
    
    /// <summary>
    /// Check if RUN/STOP key is pressed
    /// </summary>
    public bool IsStopPressed => _stopKeyPressed;
    
    /// <summary>
    /// VICE-style: Check for SHIFT + C= combo for PETSCII
100→    /// </summary>
    public bool IsShiftCbmPressed => _matrix[0x0F >> 3, 0x0F & 0x07] && _matrix[0x3D >> 3, 0x3D & 0x07];

    /// <summary>
    /// Select column mask for CIA port B
    /// </summary>
    public void SetColumnMask(byte mask)
    {
        _columnMask = mask;
    }

### Turn 88 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 11 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxRomProvisioningViewModel.cs
1:namespace ViceSharp.Xbox.ViewModels;
15:/// <see cref="RomProvisionAssessment"/>, imports a picked ROM file (validated against the
36:public sealed class XboxRomProvisioningViewModel : INotifyPropertyChanged
47:    private readonly RomProvisionEvaluator _evaluator;
51:    private RomProvisionAssessment? _assessment;
67:    public XboxRomProvisioningViewModel(
70:        RomProvisionEvaluator evaluator,
89:    public RomProvisionAssessment? Assessment
101:    /// <summary>The overall provisioning state (defaults to <see cref="RomProvisionState.NotProvisioned"/> before the first refresh).</summary>
102:    public RomProvisionState State => _assessment?.State ?? RomProvisionState.NotProvisioned;
106:    /// <see cref="RomProvisionState.Complete"/> (accounting for the Ultimax kernal-optional rule),
</workspace_result>

### Turn 89 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
No matches found
</workspace_result>

### Turn 90 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 29 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxRomProvisioningViewModel.cs
15:/// <see cref="RomProvisionAssessment"/>, imports a picked ROM file (validated against the
36:public sealed class XboxRomProvisioningViewModel : INotifyPropertyChanged
47:    private readonly RomProvisionEvaluator _evaluator;
51:    private RomProvisionAssessment? _assessment;
67:    public XboxRomProvisioningViewModel(
70:        RomProvisionEvaluator evaluator,
89:    public RomProvisionAssessment? Assessment
101:    /// <summary>The overall provisioning state (defaults to <see cref="RomProvisionState.NotProvisioned"/> before the first refresh).</summary>
102:    public RomProvisionState State => _assessment?.State ?? RomProvisionState.NotProvisioned;
106:    /// <see cref="RomProvisionState.Complete"/> (accounting for the Ultimax kernal-optional rule),

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\RomCatalog.cs
39:            RomRole.Kernal,

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\RomProvisionEvaluator.cs
11:/// directory and reports per-role presence and the overall <see cref="RomProvisionState"/>.
22:/// <see cref="RomProvisionState.Invalid"/>; otherwise all required roles present is
23:/// <see cref="RomProvisionState.Complete"/>, no roles present is
24:/// <see cref="RomProvisionState.NotProvisioned"/>, and anything between is
25:/// <see cref="RomProvisionState.Partial"/>. Under <see cref="RomProfile.Ultimax"/> the KERNAL
30:public sealed class RomProvisionEvaluator
32:    private static readonly RomRole[] AllRoles = { RomRole.Basic, RomRole.Kernal, RomRole.Chargen };
36:    public RomProvisionEvaluator(RomCatalog? catalog = null)
51:    public RomProvisionAssessment Evaluate(string c64Directory, RomProfile profile)
96:            ? RomProvisionState.Invalid
98:                ? RomProvisionState.Complete
100:                    ? RomProvisionState.NotProvisioned
101:                    : RomProvisionState.Partial;
103:        var isBootBlocked = state != RomProvisionState.Complete;
104:        return new RomProvisionAssessment(state, roles, isBootBlocked);
108:        !(profile == RomProfile.Ultimax && role == RomRole.Kernal);

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\RomProvisionState.cs
7:public enum RomProvisionState

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\RomRole.cs
14:    Kernal = 1,

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\RomProvisionAssessment.cs
</workspace_result>

### Turn 91 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 21 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\AppKeyKind.cs
33:    /// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetRestoreState(bool)"/>

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\VirtualKeyboardViewModel.cs
34:///   seam <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> asserted then released,
190:                _keyboard.SetRestoreState(true);
191:                _keyboard.SetRestoreState(false);

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachineKeyboardInput.cs
23:    bool SetRestoreState(bool pressed);

F:\GitHub\vice-sharp\src\ViceSharp.Core\C64MemoryMap.cs
585:    public bool SetRestoreState(bool pressed)
594:            _keyboard.SetRestore(pressed);

F:\GitHub\vice-sharp\src\ViceSharp.Core\Input\C64KeyboardMatrix.cs
71:            // RESTORE/NMI trigger is driven exclusively via SetRestore(bool).
83:    public void SetRestore(bool pressed)

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\HostMediaSimulatedLoadTests.cs
250:        public bool SetRestoreState(bool pressed) => true;

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Services\InputServiceHost.cs
47:    /// dedicated <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> seam via the
52:    public ValueTask<InputCommandResponse> SetRestoreStateAsync(
183:        return keyboardInput is not null && keyboardInput.SetRestoreState(pressed);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostInputServiceTests.cs
443:        public bool SetRestoreState(bool pressed)

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\ConsoleEmulatorHost.cs
106:    /// <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> via the same device
109:    void SetRestoreState(string sessionId, bool pressed);
477:    public void SetRestoreState(string sessionId, bool pressed)
478:        => _input.SetRestoreStateAsync(sessionId, pressed).GetAwaiter().GetResult();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\Fakes\FakeMachineKeyboardInput.cs
29:    /// <summary>The pressed state passed to the most recent <see cref="SetRestoreState"/> call.</summary>
32:    /// <summary>Number of <see cref="SetRestoreState"/> calls received.</summary>
55:    public bool SetRestoreState(bool pressed)
</workspace_result>

### Turn 92 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $ids = @('FIX-XKBDNMI-001',... | Duration: 23.34s | Location [FileSystem]: F:\GitHub\vice-sharp

FIX-XKBDNMI-001 | done=false | pri=medium | Wire C64 RESTORE trigger (IsRestorePressed) to the CPU NMI edge
PLAN-XKEYBOARD-001 | done=false | pri=high | UWP Virtual Keyboard should be a real C64 keyboard
IMPL-XBOXUWP-000 | done=false | pri=high | 'S0: FEASIBILITY GO/NO-GO (the one gate all production slices depend on)'
IMPL-XBOXUWP-035 | done=false | pri=medium | 'S35: Desktop-intact + full-suite + AOT regression guard'
IMPL-XBOXUWP-036 | done=false | pri=medium | 'S36: On-console deploy + boot-to-menu + C64 render smoke'
IMPL-XBOXUWP-037 | done=false | pri=medium | 'S37: On-console live gamepad smoke (JOY2/JOY1 + A/B fire + menu neutralization + swap)'
IMPL-XBOXUWP-038 | done=false | pri=medium | 'S38: On-console A/V smoke (Win2D video + XAudio2 audio + mute + suspend/resume)'
IMPL-XBOXUWP-039 | done=false | pri=medium | 'S39: On-console system-button + context smoke'
IMPL-XBOXUWP-040 | done=false | pri=medium | 'S40: On-console first-run ROM provisioning + settings persistence + device attach E2E'
IMPL-XBOXUWP-041 | done=false | pri=medium | 'S41: On-console performance within resource budget (PAL 50)'
IMPL-XBOXUWP-042 | done=false | pri=low | 'S42: Console / Store-submission validation (deferred: console deploy, on-console resource budget, TV device-family behaviors, Store cert)'
PLAN-XBOXCLIENT-001 | done=false | pri=high | XBOX-compatible client (10-foot console front-end)
⚠️ SCOPE: Use $script: prefix for: $ids, $done, $pri, $title


### Turn 93 (Assistant)
[tool_request: grep]
- pattern: IsRestorePressed|RestorePressed|_restoreKey
- path: F:\GitHub\vice-sharp\src
- glob: *.cs
- head_limit: 40
[tool_request: grep]
- pattern: IsRestorePressed|SetRestore|Nmi\(\)|RequestNmi|RaiseNmi
- path: F:\GitHub\vice-sharp\tests
- glob: *Restore*
- head_limit: 40
[tool_request: run_terminal_command]
- command: Get-ChildItem "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness" -Recurse -Filter "*Restore*" | Select-Object FullName; Get-ChildItem "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness" -Recurse -Filter "*XKbd*" | Select-Object FullName; Select-String -Path "F:\GitHub\vice-sharp\tests\**\*.cs" -Pattern "SetRestoreState|IsRestorePressed|TEST-XKBD" | Select-Object -First 30 Path,LineNumber,Line
- description: Find RESTORE/NMI tests
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\InputMappingPage.xaml
- limit: 80
[tool_request: grep]
- pattern: BindingProfile|AppCommand|remappable|InputMapping
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels
- glob: *.cs
- head_limit: 40

### Turn 94 (Function)
[tool_response]
1→<!--
  PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the read-only input-mapping page. Binds
  InputMappingViewModel.Rows (locked joystick + system-button scheme) and offers a jump to
  the on-screen virtual keyboard. Inert on the net10.0 fallback.
-->
<Page
    x:Class="ViceSharp.Xbox.Views.InputMappingPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
10→    xmlns:vm="using:ViceSharp.Xbox.ViewModels"
    RequiresPointer="Never"
    Background="#EE000000">
    <Grid Style="{StaticResource TvSafeAreaRootStyle}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

20→        <TextBlock Grid.Row="0" Text="Controls" FontSize="48" Foreground="White" Margin="0,0,0,16" />

        <ListView Grid.Row="1" ItemsSource="{x:Bind ViewModel.Rows, Mode=OneWay}" SelectionMode="Single">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="vm:InputMappingRow">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="320" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
30→                        <TextBlock Grid.Column="0" Text="{x:Bind InputLabel}" FontSize="26" Foreground="White" />
                        <TextBlock Grid.Column="1" Text="{x:Bind ActionLabel}" FontSize="26" Foreground="#FFCCCCCC" />
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Virtual keyboard" Style="{StaticResource TenFootButtonStyle}" Click="OnVirtualKeyboard" />
            <Button Content="Back" Style="{StaticResource TenFootButtonStyle}" Click="OnBack" />
40→        </StackPanel>
    </Grid>
</Page>


### Turn 95 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 7 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IKeyboardMatrix.cs
12:    bool IsRestorePressed { get; }

F:\GitHub\vice-sharp\src\ViceSharp.Core\C64MemoryMap.cs
632:    public bool IsRestorePressed
637:                return _keyboard.IsRestorePressed;

F:\GitHub\vice-sharp\src\ViceSharp.Core\Input\C64KeyboardMatrix.cs
26:    private bool _restoreKeyPressed;
50:        _restoreKeyPressed = false;
85:        _restoreKeyPressed = pressed;
91:    public bool IsRestorePressed => _restoreKeyPressed;
</workspace_result>

### Turn 96 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 25 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxRestoreSeamTests.cs
32:    /// session, <c>IConsoleEmulatorHost.SetRestoreState</c> reaches the device's
33:    /// <see cref="IMachineKeyboardInput.SetRestoreState"/> and never
38:    public async Task Facade_SetRestoreState_RoutesToDedicatedSeam_NotSetKeyState()
49:        // The dedicated RESTORE seam reaches SetRestoreState (the NMI path), never the
51:        facade.SetRestoreState("restore-session", true);
52:        facade.SetRestoreState("restore-session", false);
69:    /// Acceptance: <c>SetRestore(true)</c> raises <c>IsRestorePressed</c>; pressing
71:    /// <c>IsRestorePressed</c> false. ROM-free and deterministic.
80:        matrix.SetRestore(true);
81:        Assert.True(matrix.IsRestorePressed);
82:        matrix.SetRestore(false);
83:        Assert.False(matrix.IsRestorePressed);
88:        Assert.False(matrix.IsRestorePressed);
99:    /// Acceptance: <c>SetRestoreStateAsync(true)</c> raises the C64
100:    /// <see cref="IKeyboardMatrix.IsRestorePressed"/>; <c>SetRestoreStateAsync(false)</c>
102:    /// <c>IsRestorePressed</c> false. Uses the repo-vendored VICE C64 ROMs (no skip).
105:    public async Task InputServiceHost_SetRestoreState_FiresC64RestoreTrigger_WhileAsteriskDoesNot()
118:        // keycode 0x31's flag used to share), observable via IsRestorePressed.
119:        var pressed = await service.SetRestoreStateAsync("c64-restore", true, ct);
121:        Assert.True(matrix.IsRestorePressed);
123:        var released = await service.SetRestoreStateAsync("c64-restore", false, ct);
125:        Assert.False(matrix.IsRestorePressed);
133:        Assert.False(matrix.IsRestorePressed);
140:    /// Spy keyboard input that records SetKeyState and SetRestoreState calls on separate
159:        public bool SetRestoreState(bool pressed)
</workspace_result>

### Turn 97 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 22 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\InputMappingRow.cs
16:public sealed record InputMappingRow(string InputLabel, string ActionLabel);

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\InputMappingViewModel.cs
23:///   <see cref="BindingProfile.Default"/> (Menu, View, X, Y, LB, RB, LT, L3) with each
33:public sealed class InputMappingViewModel
35:    private readonly List<InputMappingRow> _rows;
39:    /// joystick scheme and <see cref="BindingProfile.Default"/>.
41:    public InputMappingViewModel()
43:        _rows = BuildRows(BindingProfile.Default);
57:    public IReadOnlyList<InputMappingRow> Rows => _rows;
63:    private static List<InputMappingRow> BuildRows(BindingProfile profile)
65:        var rows = new List<InputMappingRow>
75:        // Default S10 system buttons, in the locked BindingProfile.Default order.
78:            rows.Add(new InputMappingRow(InputLabel(binding.Input), ActionLabel(binding.Command)));
101:    private static string ActionLabel(AppCommand command) => command switch
103:        AppCommand.OpenMainMenu => "Open main menu",
104:        AppCommand.ToggleVirtualKeyboard => "Toggle virtual keyboard",
105:        AppCommand.AutostartDrive8 => "Autostart drive 8",
106:        AppCommand.WarmReset => "Warm reset",
107:        AppCommand.QuickSaveState => "Quick save state",
108:        AppCommand.QuickLoadState => "Quick load state",
109:        AppCommand.WarpHoldOn => "Warp (hold)",
110:        AppCommand.SwapJoystickPorts => "Swap joystick ports",

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\NavigationDestination.cs
51:    InputMapping,
</workspace_result>

### Turn 98 (Function)
[tool_response]
exit: 0

FullName
--------
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxRestoreSeamTests.cs




### Turn 99 (Assistant)
[tool_request: grep]
- pattern: Models|SelectedModel|RomAvailable|IsAvailable|profile
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
- head_limit: 50
[tool_request: grep]
- pattern: IsRestorePressed|Nmi
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core
- glob: *.cs
- head_limit: 30
[tool_request: run_terminal_command]
- command: # Count Category=Xbox tests if quick to grep
(Select-String -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\**\*.cs" -Pattern 'Category\s*=\s*"Xbox"|Trait\("Category",\s*"Xbox"\)' | Measure-Object).Count
# Check Binding store existence
Get-ChildItem "F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input" -Recurse -Filter "*Bind*" | Select-Object Name
Get-ChildItem "F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input" -Recurse -Filter "*Profile*" | Select-Object Name
# dirty tree: is x:Bind complete on all pages?
Select-String -Path "F:\GitHub\vice-sharp\src\ViceSharp.Xbox\**\*.xaml" -Pattern '\{Binding ' | Select-Object Path,LineNumber,Line | Format-Table -AutoSize | Out-String -Width 200
- description: Count Xbox tests, binding leftovers, Binding store files

### Turn 100 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 38 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
1:namespace ViceSharp.Xbox.ViewModels;
34:///   <item><description>Restart gating: changing the profile or the resource mode sets
51:    // The one implemented computer family: its C64 machine profiles are the selectable
52:    // models. The "Minimal host" pseudo-profile (Machine=="minimal") is excluded by this
102:    private IReadOnlyList<SettingsProfileDto> _profiles = Array.Empty<SettingsProfileDto>();
123:        // Plus4 are disabled placeholders (IsAvailable false) advertising the roadmap.
169:    /// <summary>The available settings profiles (loaded by <see cref="RefreshAsync"/>).</summary>
172:        get => _profiles;
175:            if (!SetProperty(ref _profiles, value))
178:            // The Model picker is the C64 machine profiles from the current profile list; the
179:            // "Minimal host" pseudo-profile (Machine=="minimal") is filtered out.
180:            _models = BuildModels(value);
181:            OnPropertyChanged(nameof(Models));
182:            OnPropertyChanged(nameof(SelectedModel));
194:    /// The selectable machine models: the C64 (<c>x64sc</c>) profiles from
195:    /// <see cref="Profiles"/>, with the non-selectable "Minimal host" pseudo-profile
198:    public IReadOnlyList<SettingsProfileDto> Models => _models;
202:    /// selection is not one of the selectable <see cref="Models"/>. Setting it routes through
204:    /// whose id is not in <see cref="Models"/> is ignored (no change, no dirty flip).
206:    public SettingsProfileDto? SelectedModel
208:        get => Models.FirstOrDefault(m => string.Equals(m.Id, SelectedProfileId, StringComparison.Ordinal));
211:            if (value is not null && Models.Any(m => string.Equals(m.Id, value.Id, StringComparison.Ordinal)))
218:    /// <see cref="Models"/> are all C64), or <c>null</c> when no model is selected. The setter
219:    /// is a no-op: model selection flows through <see cref="SelectedModel"/> and only the
226:            var machine = SelectedModel?.Machine;
234:    private static IReadOnlyList<SettingsProfileDto> BuildModels(IReadOnlyList<SettingsProfileDto> profiles)
236:        var models = new List<SettingsProfileDto>(profiles.Count);
237:        foreach (var profile in profiles)
239:            if (string.Equals(profile.Machine, C64FamilyId, StringComparison.Ordinal))
240:                models.Add(profile);
332:    /// <summary>Selected machine/settings profile id (restart-relevant).</summary>
341:            // The Model/Computer pickers are projections of the selected profile id.
342:            OnPropertyChanged(nameof(SelectedModel));
394:    /// True when a restart-relevant change (profile or resource mode) is pending. Other
419:    /// Loads the host-canonical settings and the available profiles, binding every field
426:        var profiles = await _gateway.ListSettingsProfilesAsync(cancellationToken).ConfigureAwait(false);
427:        if (profiles.Status.IsSuccess)
428:            Profiles = profiles.Profiles;
</workspace_result>

### Turn 101 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 15 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
92:        var nmiLine = new InterruptLine(InterruptType.Nmi);

F:\GitHub\vice-sharp\src\ViceSharp.Core\CartPortInterSystemBus.cs
36:    public const string Nmi = "NMI";
47:    private static readonly string[] DefaultSignals = { Game, ExRom, Irq, Nmi, Dma, Reset };

F:\GitHub\vice-sharp\src\ViceSharp.Core\Input\C64KeyboardMatrix.cs
91:    public bool IsRestorePressed => _restoreKeyPressed;

F:\GitHub\vice-sharp\src\ViceSharp.Core\C64MemoryMap.cs
632:    public bool IsRestorePressed
637:                return _keyboard.IsRestorePressed;

F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
83:        UpdateNmiEdgeLatch();
93:            _cpu.Nmi();
246:    private void UpdateNmiEdgeLatch()

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
497:        public byte NmiAsserted;
500:        public byte NmiSourceCount;
525:        public ulong NmiClk;
527:        public ulong NmiDelayCycles;
791:                NmiClk = state.NmiClk,
793:                NmiDelayCycles = state.NmiDelayCycles
</workspace_result>

### Turn 102 (Function)
[tool_response]
exit: 0
212

Name
----
BindableInput.cs
BindingActivation.cs
BindingJsonContext.cs
BindingProfile.cs
BindingProfileExtensions.cs
ButtonBinding.cs
IBindingStore.cs
InMemoryBindingStore.cs
BindingProfile.cs
BindingProfileExtensions.cs

Path                                                        LineNumber Line
----                                                        ---------- ----
F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\HomePage.xaml         31                     Text="{Binding Title}"





### Turn 103 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
- offset: 70
- limit: 50
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
- offset: 230
- limit: 40
[tool_request: grep]
- pattern: IsRestore|Restore|NmiSource|IInterrupt
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$t = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.get' -ParamsObject @{ id = 'FIX-XKBDNMI-001' } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String; $t.Substring(0, [Math]::Min(3500, $t.Length))"}

### Turn 104 (Function)
[tool_response]
70→    {
        _cycle++;

        TickPhase(ClockPhase.Phi1, skipCpu: false);
        // PERF-CLOCK-002: read IsCpuCycleStolen and IsCpuCycleStealMandatory into locals
        // once to eliminate double property reads from the hot path.
        var cpuCycleStolen = IsCpuCycleStolen(out var cpuCycleStealConditional, out var cpuCycleStealMandatory);
        var cpuSkipped = TickPhase(
            ClockPhase.Phi2,
            skipCpu: cpuCycleStolen,
80→            conditionalCpuSkip: cpuCycleStealConditional,
            mandatoryCpuSkip: cpuCycleStealMandatory);

        UpdateNmiEdgeLatch();

        if (cpuSkipped)
            return;

        // Check for pending interrupts after all devices have ticked.
        // This allows CIA/VIC to assert interrupt lines during their Tick().
90→        if (_cpu != null && _nmiPending && _cpu.IsInstructionBoundary)
        {
            _nmiPending = false;
            _cpu.Nmi();
        }
        else if (_cpu != null && _irqLine != null && _irqLine.IsAsserted && _cpu.IsInstructionBoundary)
        {
            _cpu.Irq();
        }
    }
100→
    // PERF-CLOCK-001: iterate pre-sorted phase arrays directly instead of the
    // full _devices list with per-entry Phase/ClockDivisor checks.
    private bool TickPhase(
        ClockPhase phase,
        bool skipCpu,
        bool conditionalCpuSkip = false,
        bool mandatoryCpuSkip = false)
    {
        var cpuSkipped = false;
110→        var fastDevices = phase == ClockPhase.Phi1 ? _phi1FastDevices : _phi2FastDevices;
        var slowDevices = phase == ClockPhase.Phi1 ? _phi1SlowDevices : _phi2SlowDevices;

        foreach (var device in fastDevices)
        {
            if (skipCpu && CanSkipCpu(device, conditionalCpuSkip, mandatoryCpuSkip))
            {
                if (device is ICpu)
                    cpuSkipped = true;
                continue;

### Turn 105 (Function)
[tool_response]
230→            {
                (isPhi1 ? phi1Fast : phi2Fast).Add(device);
            }
            else
            {
                var entry = new SlowDeviceEntry { Device = device, Divisor = device.ClockDivisor, Counter = 0 };
                (isPhi1 ? phi1Slow : phi2Slow).Add(entry);
            }
        }

240→        _phi1FastDevices = phi1Fast.ToArray();
        _phi2FastDevices = phi2Fast.ToArray();
        _phi1SlowDevices = phi1Slow.ToArray();
        _phi2SlowDevices = phi2Slow.ToArray();
    }

    private void UpdateNmiEdgeLatch()
    {
        if (_nmiLine is null)
            return;
250→
        var isAsserted = _nmiLine.IsAsserted;
        if (isAsserted && !_nmiWasAsserted)
            _nmiPending = true;

        _nmiWasAsserted = isAsserted;
    }
}


### Turn 106 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 4 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
26:    private readonly IInterruptLine? _irqLine;
27:    private readonly IInterruptLine? _nmiLine;
53:    public SystemClock(long frequencyHz, Mos6502 cpu, IInterruptLine irqLine)
61:    public SystemClock(long frequencyHz, Mos6502 cpu, IInterruptLine irqLine, IInterruptLine? nmiLine)
</workspace_result>

### Turn 107 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $t = & 'F:\GitHub\mcpserver... | Duration: 3.02s | Location [FileSystem]: F:\GitHub\vice-sharp

type: result
payload:
  requestId: req-20260805T075152Z-307c
  result:
    id: FIX-XKBDNMI-001
    title: Wire C64 RESTORE trigger (IsRestorePressed) to the CPU NMI edge
    section: xbox-uwp
    priority: medium
    done: false
    description:
    - "Deferred from PLAN-XBOXUWP S24 (commit 9e48f36). S24 added the dedicated RESTORE seam (IMachineKeyboardInput.SetRestoreState -> C64KeyboardMatrix.SetRestore, sets IKeyboardMatrix.IsRestorePressed) and decoupled the spurious \"*\"/0x31 -> RESTORE coupling. GAP: IsRestorePressed currently has NO consumer - it is not wired to assert the CPU NMI line, so pressing RESTORE does not generate an actual NMI edge in the managed core (SystemClock's NMI edge latch reads only _nmiLine; grep shows IsRestorePressed unconsumed). WORK: wire the RESTORE trigger to the CPU NMI line edge (VICE: RESTORE is directly on the NMI line via the CIA/keyboard, edge-triggered), tests-first: pressing RESTORE on a running C64 generates an NMI (observe via CPU NMI line / interrupt source / pub-sub) and RUN/STOP+RESTORE resets to BASIC; \"*\" still does not fire RESTORE; determinism preserved. Cite VICE keyboard/NMI source."
    technicalDetails: &o0 []
    implementationTasks: []
    dependsOn: *o0
    functionalRequirements: *o0
    technicalRequirements: *o0
  deprecated: true

---
⚠️ SCOPE: Use $script: prefix for: $t


### Turn 108 (Assistant)
Checking one more detail on how computers advertise availability, then writing the completion plan.
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
- offset: 115
- limit: 40
[tool_request: grep]
- pattern: IsAvailable|ComputerOption|Computers
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels
- glob: *.cs
- head_limit: 25
[tool_request: run_terminal_command]
- command: # remaining {Binding} in Xbox head after WIP
Select-String -Path "F:\GitHub\vice-sharp\src\ViceSharp.Xbox\**\*.xaml" -Pattern '\{Binding ' | ForEach-Object { "$($_.Filename):$($_.LineNumber): $($_.Line.Trim())" }
# committed HEAD still has many Binding?
git -C "F:\GitHub\vice-sharp" show HEAD:src/ViceSharp.Xbox/Views/HomePage.xaml | Select-String 'Binding' | Select-Object -First 5
Write-Output '--- x:Bind count dirty ---'
(Select-String -Path "F:\GitHub\vice-sharp\src\ViceSharp.Xbox\**\*.xaml" -Pattern 'x:Bind' | Measure-Object).Count
Write-Output '--- Binding count dirty ---'
(Select-String -Path "F:\GitHub\vice-sharp\src\ViceSharp.Xbox\**\*.xaml" -Pattern '\{Binding ' | Measure-Object).Count
- description: Measure remaining Binding vs x:Bind in Xbox XAML

### Turn 109 (Function)
[tool_response]
115→    public XboxSettingsViewModel(IXboxSettingsGateway gateway, string sessionId = "")
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _sessionId = sessionId ?? string.Empty;
        _sync = SynchronizationContext.Current;
120→        _baseline = CaptureBaseline();

        // The Computer families: only the C64 is implemented today, so VIC-20 / C128 / PET /
        // Plus4 are disabled placeholders (IsAvailable false) advertising the roadmap.
        Computers = new ComputerOption[]
        {
            new("Commodore 64", C64FamilyId, true),
            new("VIC-20", "xvic", false),
            new("Commodore 128", "x128", false),
            new("Commodore PET", "xpet", false),
130→            new("Plus/4", "xplus4", false),
        };
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    // ---- Picker option lists (shared catalog) --------------------------------

    /// <summary>Renderer picker options.</summary>
140→    public IReadOnlyList<string> Renderers => SettingsOptionCatalog.RendererModes;

    /// <summary>Display-scale picker options.</summary>
    public IReadOnlyList<string> DisplayScales => SettingsOptionCatalog.DisplayScales;

    /// <summary>Crop-mode picker options.</summary>
    public IReadOnlyList<string> CropModes => SettingsOptionCatalog.CropModes;

    /// <summary>Aspect-mode picker options.</summary>
    public IReadOnlyList<string> AspectModes => SettingsOptionCatalog.AspectModes;
150→
    /// <summary>Palette picker options.</summary>
    public IReadOnlyList<string> Palettes => SettingsOptionCatalog.PaletteModes;

    /// <summary>Audio-mode picker options.</summary>

### Turn 110 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 9 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\ComputerOption.cs
7:/// (<see cref="IsAvailable"/> <c>false</c>) so the picker advertises the roadmap without
12:/// <param name="IsAvailable">Whether the family is implemented and therefore selectable.</param>
13:public sealed record ComputerOption(string DisplayName, string FamilyId, bool IsAvailable);

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
123:        // Plus4 are disabled placeholders (IsAvailable false) advertising the roadmap.
124:        Computers = new ComputerOption[]
191:    public IReadOnlyList<ComputerOption> Computers { get; }
217:    /// The <see cref="ComputerOption"/> reflecting the current model's machine family (the
222:    public ComputerOption? SelectedComputer
229:                : Computers.FirstOrDefault(c => string.Equals(c.FamilyId, machine, StringComparison.Ordinal));
</workspace_result>

### Turn 111 (Function)
[tool_response]
exit: 0
HomePage.xaml:31: Text="{Binding Title}"

                    Text="{Binding Title}"
--- x:Bind count dirty ---
31
--- Binding count dirty ---
1



### Turn 112 (Assistant)
[tool_request: run_terminal_command]
- command: New-Item -ItemType Directory -Force -Path "C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed" | Out-Null; Test-Path "C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed"
- description: Ensure plan session directory exists

### Turn 113 (Function)
[tool_response]
exit: 0
True


### Turn 114 (Assistant)
[tool_request: write]
- file_path: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md
- content: # PLAN-XBOXUWP completion plan (handoff pickup)

**Date:** 2026-08-05  
**Branch:** `feat/xbox-uwp-app` @ `a199063` (dirty tree)  
**Plan source of truth:** `docs/plans/PLAN-XBOXUWP-BDPv4.md` + `HANDOFF.md` resume snapshot  
**Process:** Byrd Development Process v4 (tests-first, red before green, 100% green gate per slice; 0 failed and 0 skipped in the slice filter)

---

## 1. Goal (definition of done)

Close **PLAN-XBOXUWP** as a **dev-PC (Tier D) complete** product head, with Tier C Store/console work explicitly parked as S42.

**Done when all of the following are true:**

1. **Functional residuals from HANDOFF OPEN** are implemented and gated (except items that require physical operator hardware and are checklist-closed or parked).
2. **In-progress AOT-safe bindings WIP** is finished (or deliberately reverted) so the head is not left half-migrated.
3. **Category=Xbox** gate is green (target last known: **501/501**, 0 fail, 0 skip).
4. **Desktop-intact** baseline still holds (determinism + Avalonia boundary; no new full-suite failures).
5. **MCP TODOs** for completed slices (S0-S35 de facto done work, plus each residual slice) are marked done with evidence; open residuals are accurate.
6. **HANDOFF.md** reflects completion state; scratch/unrelated dirty files are not mixed into plan commits.
7. **S42** remains deferred (tracked, not dropped). No Store submission in this plan.

**Out of scope for this completion plan** (do not pull into the critical path):

- `PLAN-ROMM-001` / `IMPL-ROMM-*` (separate product)
- Romless VIC worktree (`FIX-ROMLESSVIC-001` / `F:\GitHub\vice-sharp-romless`)
- Octopus LEGION2 steps in dirty `azure-pipelines.*.yml` (unrelated WIP: stash or separate branch)
- Physical Xbox console deploy / Store cert (S42)
- Push to `origin` unless operator explicitly requests it (HANDOFF: not pushed, Azure DevOps only on request)

---

## 2. Verified current state (receipts)

### Branch and tree

- HEAD: `a199063` - FEAT-XMENUSUBPAGE-001
- Dirty (plan-related): Xbox views/controls XAML + code-behind migrating `{Binding}` -> `{x:Bind}`; HANDOFF.md snapshot refresh
- Dirty (not plan): `azure-pipelines.ci.yml` / `release.yml` Octopus steps; `tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json`; untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`
- Working tree Binding audit (dirty): **31** `x:Bind`, **1** remaining `{Binding}` (`HomePage.xaml` Title)

### What already shipped (do not re-implement)

Per `project_xbox_uwp_app.md` + git log + HANDOFF:

- Tier H slices S1-S35 largely implemented: Host.InProcess, Xbox.Input, ViewModels, UWP head, DX11 video (Win2D removed), XAudio2, menu/keyboard UX, settings persistence, snapshots v2, deploy loop `DeployXboxLocal`
- App runs and renders C64 on Windows 11 dev PC
- Critical finding: build-time `PublishAot=true` kills reflection `{Binding}` (FIX-XKBDPANEL-001); AOT is opt-in via `ViceSharpPublishAot=true`
- Last device-verified gate: **Category=Xbox 501/501**

### Real open gaps (HANDOFF + live code)

| Gap | MCP ID / name | Root cause (verified) |
|-----|---------------|------------------------|
| RESTORE does not fire CPU NMI | **FIX-XKBDNMI-001** | Seam exists (`SetRestoreState` -> `IsRestorePressed`) but **no consumer** asserts `_nmiLine`; `SystemClock.UpdateNmiEdgeLatch` only watches `_nmiLine` |
| Controls keyboard-shortcut config | operator request; no MCP id yet | `InputMappingPage` is **read-only** rows from `BindingProfile.Default`; portable `IBindingStore` / `BindingProfile` already exist in `ViceSharp.Xbox.Input` but UI cannot edit/save |
| Per-model ROM availability in picker | open; no dedicated id | Settings model list is all C64 profiles; provisioning is separate. Need model rows to surface ROM readiness (or disable boot-blocking choices when ROMs missing) |
| B-on-controller verification | operator hardware | Code path present (FEAT-XMENUSUBPAGE-001); needs physical controller sign-off checklist, not more code unless broken |
| Store-AOT bindings | implied by FIX-XKBDPANEL-001 | Dirty WIP already converts most pages to `{x:Bind}`; must finish HomePage + nested templates; do **not** re-arm `PublishAot` until green device check |
| S0 / S35-S41 TODOs still `done=false` | MCP drift | Work largely done on branch; needs TODO reconciliation with receipts, not full rewrites |
| S42 Tier C | **IMPL-XBOXUWP-042** | Explicitly deferred |

### MCP trust for this session

- Plugin path works (`Invoke-McpPlugin.ps1`, GrokCode, plugin 1.85.0)
- Streamable HTTP `mcpserver` MCP auth failed in host; continue via plugin wrappers only

---

## 3. Locked decisions (do not re-litigate)

1. **Completion target = Tier D done, Tier C parked.** Matches plan section 0a and HANDOFF.
2. **BDP v4 for every behavior change:** failing tests first, then implementation, slice gate green before next slice.
3. **RESTORE/NMI:** wire real NMI edge (FR-XKBD-001); do not ship tile disabled.
4. **AOT:** keep Release-UWP JIT by default; complete `x:Bind` migration as the Store-AOT prerequisite slice; re-arming PublishAot is a **separate later** decision after device proof.
5. **Controls remapping:** enable edit of remappable bindings (Menu/View stay fixed per plan); persist via existing `IBindingStore` / source-gen JSON; joystick A/B fire mapping stays locked.
6. **Unrelated dirty files:** do not commit Octopus/AiReview/scratch docs with Xbox slices. Either leave unstaged or stash before plan commits.
7. **Commit identity:** sign commits with `model-effort@agent` and confidence per workspace rules when committing.
8. **Push:** only on explicit operator request.

---

## 4. Slice plan (ordered)

### Slice 0 - Working-tree hygiene (no product behavior)

**Goal:** Isolate plan work from noise.

Actions:

1. Stash or leave unstaged: `azure-pipelines.ci.yml`, `azure-pipelines.release.yml`, `tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json`, untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`
2. Keep in play: Xbox XAML/cs WIP + HANDOFF updates that describe this completion
3. Confirm `git status` only shows intentional Xbox/handoff paths before first commit

**Gate:** clean intent (no gate tests). Receipt: `git status -sb` scoped list.

---

### Slice A - FEAT-XAOTBIND-001: finish AOT-safe bindings (resume dirty WIP)

**Why first:** WIP already rewrites Settings/Devices/ROM/Input/Keyboard/About to `{x:Bind}`; leaving half-done is worse than finishing.

**Scope:**

1. Convert remaining `{Binding}` (HomePage Title at minimum) to `{x:Bind}`
2. Ensure every page/control has a public ViewModel property set **before** `InitializeComponent` where TwoWay compiled bind requires it (pattern already in Settings/InputMapping dirty code)
3. Nested `DataTemplate` uses `x:DataType` (keyboard already does)
4. Keep `DataContextChanged` + `Bindings.Update()` where DataContext is reassigned after construct (keyboard overlay pattern)
5. **Do not** set `ViceSharpPublishAot=true` in this slice

**Tests first:**

- Extend or add file-shape / convention tests under Category=Xbox that fail if Xbox head XAML still contains reflection `{Binding ` (except any documented allowlist if truly required)
- Existing Category=Xbox suite must stay green

**RED:** convention test fails on HomePage (or any remaining Binding).  
**GREEN:** 0 `{Binding` in `src/ViceSharp.Xbox/**/*.xaml` (or allowlist empty); Category=Xbox green.

**Validation:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Xbox"
# Device smoke after DeployXboxLocal: Settings populated, keyboard 66 tiles, menu title visible
```

**Acceptance:** UI still populates under Release-UWP JIT; no regression vs FIX-XKBDPANEL-001 device behavior.

**Requirements:** TR-XBOXUI / AOT readiness (cite existing FR-XBOXUI; if new TR needed for x:Bind, register via MCP requirements).

---

### Slice B - FIX-XKBDNMI-001: RESTORE -> CPU NMI edge

**Root cause (locked):** `C64KeyboardMatrix.SetRestore` only sets `_restoreKeyPressed`. Nothing asserts the NMI interrupt line. `SystemClock` only edge-latches `_nmiLine`.

**Design (decision-complete):**

1. On C64 architecture wiring, treat RESTORE as an NMI source (edge on press, deassert on release), matching VICE RESTORE-as-NMI semantics (monostable; edge-triggered NMI).
2. Preferred implementation shape (pick one during red tests; prefer minimal core touch):
   - **Option preferred:** keyboard matrix or memory map asserts/deasserts a dedicated restore NMI source on the shared `IInterruptLine` NMI line (or a small `IInterruptSource` registered on the NMI line) when `SetRestore(true/false)` runs.
   - **Rejected:** poll `IsRestorePressed` from UI/host each frame (wrong layer; host-only would skip console-less tests and break determinism story).
   - **Rejected:** map RESTORE through matrix keycode 0x31 (already deliberately decoupled; `*` must not NMI).

**Tests first (Category=Xbox and/or Core keyboard):**

| Test | Expected |
|------|----------|
| `SetRestoreState(true)` on running C64 raises NMI edge (CPU NMI pending / service path observed) | true |
| `SetRestoreState(false)` deasserts restore source; no extra NMI spam while held | hold does not re-fire without edge |
| `SetKeyState("*")` does **not** assert restore / NMI | false |
| Determinism filter still green for scope | DET baseline |

**RED:** existing XboxRestoreSeamTests pass flag-only; new NMI-edge tests fail.  
**GREEN:** NMI edge tests pass; asterisk still clean; DET green.

**Validation:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~XboxRestore|FullyQualifiedName~Restore|Category=Xbox"
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Determinism"
```

**Device check:** virtual keyboard RESTORE tile; observe NMI effect (e.g. STOP+RESTORE path / interrupt observable in log if available).

**MCP:** mark `FIX-XKBDNMI-001` done with evidence after green.

---

### Slice C - FEAT-XCTRLBIND-001: Controls-page keyboard-shortcut config

**Operator request (never started).** Portable model already exists.

**Scope:**

1. Promote `InputMappingPage` from read-only list to remappable editor for **bindable** inputs only
2. Locked / non-editable: A=JOY2 fire, B=JOY1 fire (display-only rows)
3. Fixed menu control per plan: Menu open/close and View keyboard toggle remain available and cannot be unbound (may allow rebinding only if plan allows; **default: Menu and View stay fixed display rows**, remappable = X/Y/LB/RB/LT/L3/etc. from `BindingProfile`)
4. Persist via `IBindingStore` (file-backed store in head LocalFolder already implied by plan S10; wire if missing adapter)
5. Live apply: `XboxInputContext` / dispatcher must load the saved profile at session start and after save
6. Reset to defaults control

**UI (10-foot):**

- List of Input -> Command rows
- Focus row, A/activate enters rebind capture (listen for next BindableInput press)
- B/back cancels capture; confirm save
- Conflict policy: last assignment wins; previous binding for that command cleared (document in tests)

**Tests first:**

- ViewModel: load profile, rebind X from Autostart to WarmReset, save/load round-trip, reset defaults
- Locked rows cannot change
- Input context uses loaded profile (spy/fake store)

**RED:** editor VM/tests fail (read-only today).  
**GREEN:** remapping + persistence tests pass; Category=Xbox green.

**Validation:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~InputMapping|FullyQualifiedName~Binding|Category=Xbox"
# Device: Controls page rebind + relaunch honors bindings.json / store path
```

**Requirements:** map to existing SYSBTN/CTX FR if present; else create FR-XCTRL-001 / TR / TEST via MCP requirements before code.

---

### Slice D - FEAT-XROMPICK-001: per-model ROM availability in the model picker

**Intent:** User can see whether a selected C64 model/profile has provisioned ROMs before apply/restart, and cannot silently boot-block without feedback.

**Design:**

1. Settings Model picker (or adjacent status) shows ROM readiness per selectable C64 profile using existing `RomProvisionEvaluator` / `RomCatalog` rules (Ultimax kernal-optional already exists)
2. Incomplete ROMs: status text + optional disable of Apply for restart-to-missing-ROM, or allow Apply but route to ROM provisioning with message (prefer: **Apply allowed, StatusText warns + offer navigate to ROM page**; IsBootBlocked remains the hard gate at session start)
3. Computer family placeholders (VIC-20 etc.) already use `IsAvailable=false`; do not conflate with ROM state

**Tests first:**

- Given temp C64 dir missing kernal, model row assessment is Partial/NotProvisioned
- Complete set => Complete
- Ultimax kernal optional still holds

**Validation:** Category=Xbox + Settings/Rom VM filters.

**MCP:** create TODO if none exists; close on green.

---

### Slice E - Tier D closeout: S0 / S36-S41 reconciliation + residual smokes

**Not a rewrite.** Map already-validated behaviors to MCP TODOs and run a single **receipt checklist**.

| Slice | Status to claim | Evidence required |
|-------|-----------------|-------------------|
| S0 feasibility | GO for dev-PC JIT path; AOT-MSIX still limited (PublishAot opt-in) | VS MSBuild Release-UWP build; app runs; controller optional |
| S35 | Close if desktop + Xbox suite green | Category=Xbox + Determinism + AvaloniaBoundary |
| S36 boot/render | Close | DeployXboxLocal + vicesharp.log first frame |
| S37 gamepad | Close only after operator B/controller check OR automated where possible | checklist in `docs/xbox/dev-pc-validation-guide.md` |
| S38 A/V | Close with log + operator audio if needed | SID path + frame ticks in log |
| S39 system buttons + RESTORE | Close after Slice B | menu/keyboard/RESTORE NMI |
| S40 ROM + settings + device | Close after Slice D + existing persistence | settings.json + provision path |
| S41 perf PAL 50 | Close with Release-UWP SPD receipt (~101% previously) | perf HUD / log SPD |
| S42 | leave open, low priority | deferred |

**B-on-controller:** run checklist with operator controller; if unavailable, leave a single open TODO `QA-XBOXCTRL-001` rather than blocking software completion.

**Validation commands:**

```pwsh
.\build.ps1 DeployXboxLocal
# then end batch with Debug-UWP restore for operator VS F5:
# MSBuild ... /p:Configuration=Debug-UWP /p:Platform=x64 /t:Restore

dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Xbox"
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Determinism"
# Optional full baseline (detached, ~20 min):
# Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy
```

---

### Slice F - MCP + docs closure

1. Mark done with receipts: completed IMPL-XBOXUWP slices, FIX-XKBDNMI-001, FEAT-XAOTBIND-001, FEAT-XCTRLBIND-001, FEAT-XROMPICK-001, PLAN-XKEYBOARD-001 if remaining keyboard ACs are met (sticky keys, real layout, PETSCII already shipped: reconcile or leave only residual ACs)
2. Leave open: IMPL-XBOXUWP-042 (S42), any operator QA items, PLAN-ROMM-*
3. Update `HANDOFF.md` resume snapshot to "PLAN-XBOXUWP Tier D complete" or list only true residuals
4. Optionally refresh `docs/xbox/dev-pc-validation-guide.md` checkboxes with dated receipts
5. Session-log actions/decisions after each slice (plugin path; completeTurn via `workflow.sessionlog.completeTurn` if CompleteTurn shim is empty)

---

## 5. PR / commit strategy

Prefer small commits per slice (not one mega commit):

1. `feat(xbox): finish x:Bind migration for AOT-safe UI [FEAT-XAOTBIND-001]`
2. `fix(core): wire RESTORE press to CPU NMI edge [FIX-XKBDNMI-001]`
3. `feat(xbox): remappable controls bindings on Controls page [FEAT-XCTRLBIND-001]`
4. `feat(xbox): show per-model ROM readiness in settings picker [FEAT-XROMPICK-001]`
5. `docs: handoff PLAN-XBOXUWP Tier D completion`

Do not push unless asked. Azure DevOps `origin` only when pushing.

---

## 6. Risk register

| Risk | Mitigation |
|------|------------|
| NMI wiring breaks CIA TOD/NMI or cart NMI | Edge-latch already multi-source line OR; tests for asterisk + CIA NMI still pass |
| x:Bind TwoWay surprises on ComboBox | Match existing dirty Settings pattern; device smoke Settings apply/revert |
| Binding store not wired in head App composition | Slice C includes adapter + startup load; tests with fake store first |
| Category=Xbox count drift | Report actual pass/fail/skip; do not chase a fixed total if only cosmetic |
| Operator controller unavailable | Park B-verify as QA TODO; do not block software slices |
| Accidental PublishAot re-enable | Explicit opt-in only; gate test or csproj comment remains |

---

## 7. Explicit non-goals

- Microsoft Store package / Partner Center
- Physical console AppContainer budget
- RomM library/UI
- Romless VIC black screen (other worktree)
- Replacing DX11 with Win2D
- Making Avalonia desktop share UWP XAML

---

## 8. Execution order summary

```
0 Hygiene
  -> A x:Bind finish (unblocks clean WIP + Store-AOT prep)
  -> B RESTORE NMI (core correctness, FR-XKBD)
  -> C Controls remapping (operator-requested UX)
  -> D ROM readiness in picker
  -> E Tier D smoke + MCP reconcile
  -> F Handoff/docs close
```

Estimated engineering focus: **4 product slices + closeout**; S42 stays deferred.

---

## 9. Success metrics (final gate)

- [ ] `Category=Xbox`: 0 failed, 0 skipped (record total)
- [ ] `Category=Determinism`: 0 failed
- [ ] RESTORE generates NMI (test + optional device)
- [ ] Controls page can rebind, persist, reload
- [ ] Model/ROM readiness visible before boot confusion
- [ ] `DeployXboxLocal` Release-UWP still renders C64; Settings + keyboard non-blank
- [ ] MCP TODOs match reality
- [ ] HANDOFF.md updated
- [ ] S42 still explicitly deferred

When the checklist is complete, **PLAN-XBOXUWP is complete for development**; Store/console is a later program (S42).


### Turn 115 (Function)
[tool_response]
The file C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md has been created.

### Turn 116 (Assistant)
[tool_request: exit_plan_mode]

### Turn 117 (Function)
[tool_response]
The user wants to revise the plan. The user said:
All RomM features are in scope.

### Turn 118 (Assistant)
Expanding the plan to include all RomM work. Pulling RomM TODO details and any existing plan docs.
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$ids = @('PLAN-ROMM-001','IMPL-ROMM-001','IMPL-ROMM-002','IMPL-ROMM-003','IMPL-ROMM-004','IMPL-ROMM-005','IMPL-ROMM-006','IMPL-ROMM-007','IMPL-ROMM-008','IMPL-ROMM-009','IMPL-ROMM-010','IMPL-ROMM-011','IMPL-ROMM-012','IMPL-ROMM-013','IMPL-ROMM-014','IMPL-ROMM-015','IMPL-ROMM-016','IMPL-ROMM-017')\nforeach ($id in $ids) {\n  $r = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.get' -ParamsObject @{ id = $id } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' 2>&1 | Out-String\n  $title = if ($r -match 'title:\\s*(.+)') { $Matches[1].Trim() } else { '?' }\n  $done = if ($r -match 'done:\\s*(\\w+)') { $Matches[1] } else { '?' }\n  $pri = if ($r -match 'priority:\\s*(\\w+)') { $Matches[1] } else { '?' }\n  # grab first description bullet briefly\n  $desc = ''\n  if ($r -match '(?ms)description:\\s*\\r?\\n\\s*-\\s*\"?([^\"]{0,200})') { $desc = $Matches[1].Trim() }\n  Write-Output \"=== $id done=$done pri=$pri ===\"\n  Write-Output $title\n  if ($desc) { Write-Output $desc }\n}"}
[tool_request: grep]
- pattern: RomM|PLAN-ROMM|AC-BROWSE|AC-LAUNCH|AC-PKG
- path: F:\GitHub\vice-sharp\docs
- glob: *.{md,yml}
- head_limit: 40
[tool_request: run_terminal_command]
- command: Get-ChildItem "F:\GitHub\vice-sharp\docs" -Recurse -Filter "*romm*" -ErrorAction SilentlyContinue | Select-Object FullName; Get-ChildItem "F:\GitHub\vice-sharp\src" -Directory | Where-Object { $_.Name -match 'RomM|Library' } | Select-Object FullName; Get-ChildItem "F:\GitHub\vice-sharp" -Recurse -Filter "*RomM*" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch 'bin|obj|\\native\\' } | Select-Object -First 30 FullName
- description: Locate RomM docs and source projects

### Turn 119 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
No matches found
</workspace_result>

### Turn 120 (Function)
[tool_response]
exit: 0


### Turn 121 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $ids = @('PLAN-ROMM-001','I... | Duration: 32.15s | Location [FileSystem]: F:\GitHub\vice-sharp

=== PLAN-ROMM-001 done=false pri=high ===
Create a RomM client library and UI for Avalonia and Xbox
Operator request 2026-07-14: build a RomM (romm.app self-hosted ROM manager) client library plus browse/download UI in BOTH heads. Scope: (1) portable client library (new project, e.g. ViceSharp.RomM,
=== IMPL-ROMM-001 done=false pri=high ===
'[P0] Pin RomM packages + restore (AC-PKG-04, TR-ROMM-NUGET-001)'
'Phase A P0. Pin RomM.Client 1.0.0 + RomM.Client.Csdb 1.0.0 in Directory.Packages.props; dotnet restore resolves them from nuget.org (NuGet.config stays clear+nuget.org). Tests: NuGetConfigTests, Dire
=== IMPL-ROMM-002 done=false pri=high ===
'[L1] ViceSharp.Library.ViewModels seams + slug map (AC-PKG-03, AC-LAUNCH-02/03, AC-BROWSE-08/02)'
'Phase B L1. Create packable ViceSharp.Library.ViewModels (refs only Protocol): seams (IRomMLibraryGateway, IGameLauncher, ILibraryImageLoader, ICsdbGateway, IRomMConnectionStore, ICurrentMachineProvi
=== IMPL-ROMM-003 done=false pri=high ===
'[L2] ViceSharp.RomM adapter browse/detail/download + RomMJsonContext (AC-BROWSE-01/02/03, AC-DETAIL-01, AC-LAUNCH-01, AC-CONN-01/06)'
'Phase B L2. Create packable ViceSharp.RomM adapter (refs Library.ViewModels + Protocol + RomM.Client + RomM.Client.Csdb): browse/platforms/detail/download via RomM.Client + Transport.SendAsync, sourc
=== IMPL-ROMM-004 done=false pri=high ===
'[L3] LibraryBrowseViewModel (AC-BROWSE-04/05/06/07, AC-LAUNCH-03/04/07)'
'Phase B L3. LibraryBrowseViewModel: paging (LoadMore/HasMore), A-Z jump via charIndex, 300ms debounce, machine-change rescope+reload, launchable gating, two-phase launch status. Tests: LibraryBrowseV
=== IMPL-ROMM-005 done=false pri=medium ===
'[L4] Launch orchestration seam (AC-LAUNCH-04/07)'
'Phase B L4. Refine IGameLauncher orchestration against a fake launcher: resolved slot + autostart flag, two-phase status. Tests: LibraryBrowseViewModelTests (launch), MediaExtensionMapTests. FR-ROMM-
=== IMPL-ROMM-006 done=false pri=medium ===
'[L5] Collections (AC-COLLECT-01..06, AC-DETAIL-02)'
'Phase B L5. Collections gateway (list/create/rename/delete/add/remove) + CollectionsViewModel + RomDetailViewModel add-to-collection; source-gen collections DTOs. Tests: RomMGatewayCollectionsTests,
=== IMPL-ROMM-007 done=false pri=medium ===
'[L6] CSDb gateways (AC-CSDB-01..06)'
'Phase B L6. CsdbDiscoveryViewModel (search, cap 20, refresh event), LocalCsdbGateway (CsdbRomMWorkflow ingest+scan), BridgeCsdbGateway (/csdb/v1/ingest then RomMClient.Tasks.ScanLibraryAsync), config
=== IMPL-ROMM-008 done=false pri=medium ===
'[L7] Covers + connection + pairing + refresh (AC-COVER-01..04, AC-CONN-02/03/04/05)'
'Phase B L7. RomMCoverImageSource (auth rules), two-tier CoverCache (gate<=4, cancel, placeholder), device pairing, auth refresh, FileRomMConnectionStore, LibraryConnectionViewModel (401 reauth). Test
=== IMPL-ROMM-009 done=false pri=medium ===
'[L8] Packaging gate (AC-PKG-01/02)'
'Phase B L8. IsPackable metadata (GPL-2.0-or-later) on both libraries; PublishNuget packs both nupkgs with correct deps. Tests: PackageMetadataTests; build.ps1 PublishNuget --skip. FR-ROMM-PKG-001, TR
=== IMPL-ROMM-010 done=false pri=medium ===
'[X1] XboxGameLauncher (AC-LAUNCH-05)'
'Phase C X1. XboxGameLauncher: reads bytes -> AttachMediaAsync payload overload -> AutostartDrive8Async(disk)/ColdResetAsync(cart); never hands an arbitrary path (UWP sandbox). Test: XboxGameLauncherT
=== IMPL-ROMM-011 done=false pri=medium ===
'[X2] XboxCoverImageLoader + XboxRomMConnectionStore'
Phase C X2. Xbox head impls of ILibraryImageLoader (cover loader) + IRomMConnectionStore (keystore-backed); portable fallbacks unit-tested, WinRT bits compiled Debug-UWP. FR-ROMM-COVER/CONN-001.
=== IMPL-ROMM-012 done=false pri=medium ===
'[X3] LibraryPage + GameDetailsPage + ListsPage + nav + pairing (AC-XUI-01..06)'
Phase C X3. UWP pages LibraryPage (GridView, search, machine indicator, A-Z, slot), GameDetailsPage, ListsPage; NavigationDestination.Library + HomePage button + Push; Settings pairing page. Uses Brid
=== IMPL-ROMM-013 done=false pri=low ===
'[X4] Xbox CSDb tab (reuses L6 VM)'
Phase C X4. Xbox CSDb discovery surface bound to CsdbDiscoveryViewModel (BridgeCsdbGateway). FR-CSDB-001, FR-ROMM-XBOXUI-001.
    technicalDetails: &o0 []
    implementationTasks: []
    dependsOn:
=== IMPL-ROMM-014 done=false pri=medium ===
'[A1] AvaloniaGameLauncher (AC-LAUNCH-06)'
'Phase D A1. AvaloniaGameLauncher delegates to ShellViewModel.DropAndStartFileAsync(autostart)/AttachPanelViewModel.AttachAsync(attach-only). Test: AvaloniaGameLauncherTests.DelegatesToShell (fake IHo
=== IMPL-ROMM-015 done=false pri=medium ===
'[A2] AvaloniaCoverImageLoader + desktop store + gateway selection (AC-CSDB-06)'
Phase D A2. Avalonia ILibraryImageLoader + desktop IRomMConnectionStore; head-side CSDb gateway selection (roms root writable -> LocalCsdbGateway, else Bridge). FR-ROMM-COVER/CONN-001, FR-CSDB-001.
=== IMPL-ROMM-016 done=false pri=medium ===
'[A3] LibraryView + GameDetailsView + ListsView + MainWindow + connection dialog (AC-AUI-01..04)'
Phase D A3. Avalonia views LibraryView (ItemsRepeater), GameDetailsView, ListsView; MainWindow wiring + connection dialog. AC-AUI-01..04 [V] (build + VM tests + E2E). Reuse TickHistoryView pattern. FR
=== IMPL-ROMM-017 done=false pri=low ===
'[A4] Avalonia CSDb tab'
Phase D A4. Avalonia CSDb discovery surface bound to CsdbDiscoveryViewModel (LocalCsdbGateway co-located, else Bridge). FR-CSDB-001, FR-ROMM-AVUI-001.
    technicalDetails: &o0 []
    implementation
⚠️ SCOPE: Use $script: prefix for: $desc


### Turn 122 (Assistant)
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md
- limit: 50
[tool_request: use_tool]
- tool_name: pwsh__invoke_expression
- tool_input: {"pipeline":"$r = & 'F:\\GitHub\\mcpserver-grok-plugin\\lib\\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.todo.get' -ParamsObject @{ id = 'PLAN-ROMM-001' } -WorkspacePath 'F:\\GitHub\\vice-sharp' -PluginRoot 'F:\\GitHub\\mcpserver-grok-plugin' | Out-String\n# print more of description\nif ($r.Length -gt 8000) { $r.Substring(0,8000) } else { $r }"}

### Turn 123 (Function)
[tool_response]
1→# PLAN-XBOXUWP completion plan (handoff pickup)

**Date:** 2026-08-05  
**Branch:** `feat/xbox-uwp-app` @ `a199063` (dirty tree)  
**Plan source of truth:** `docs/plans/PLAN-XBOXUWP-BDPv4.md` + `HANDOFF.md` resume snapshot  
**Process:** Byrd Development Process v4 (tests-first, red before green, 100% green gate per slice; 0 failed and 0 skipped in the slice filter)

---

10→## 1. Goal (definition of done)

Close **PLAN-XBOXUWP** as a **dev-PC (Tier D) complete** product head, with Tier C Store/console work explicitly parked as S42.

**Done when all of the following are true:**

1. **Functional residuals from HANDOFF OPEN** are implemented and gated (except items that require physical operator hardware and are checklist-closed or parked).
2. **In-progress AOT-safe bindings WIP** is finished (or deliberately reverted) so the head is not left half-migrated.
3. **Category=Xbox** gate is green (target last known: **501/501**, 0 fail, 0 skip).
4. **Desktop-intact** baseline still holds (determinism + Avalonia boundary; no new full-suite failures).
20→5. **MCP TODOs** for completed slices (S0-S35 de facto done work, plus each residual slice) are marked done with evidence; open residuals are accurate.
6. **HANDOFF.md** reflects completion state; scratch/unrelated dirty files are not mixed into plan commits.
7. **S42** remains deferred (tracked, not dropped). No Store submission in this plan.

**Out of scope for this completion plan** (do not pull into the critical path):

- `PLAN-ROMM-001` / `IMPL-ROMM-*` (separate product)
- Romless VIC worktree (`FIX-ROMLESSVIC-001` / `F:\GitHub\vice-sharp-romless`)
- Octopus LEGION2 steps in dirty `azure-pipelines.*.yml` (unrelated WIP: stash or separate branch)
- Physical Xbox console deploy / Store cert (S42)
30→- Push to `origin` unless operator explicitly requests it (HANDOFF: not pushed, Azure DevOps only on request)

---

## 2. Verified current state (receipts)

### Branch and tree

- HEAD: `a199063` - FEAT-XMENUSUBPAGE-001
- Dirty (plan-related): Xbox views/controls XAML + code-behind migrating `{Binding}` -> `{x:Bind}`; HANDOFF.md snapshot refresh
40→- Dirty (not plan): `azure-pipelines.ci.yml` / `release.yml` Octopus steps; `tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json`; untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`
- Working tree Binding audit (dirty): **31** `x:Bind`, **1** remaining `{Binding}` (`HomePage.xaml` Title)

### What already shipped (do not re-implement)

Per `project_xbox_uwp_app.md` + git log + HANDOFF:

- Tier H slices S1-S35 largely implemented: Host.InProcess, Xbox.Input, ViewModels, UWP head, DX11 video (Win2D removed), XAudio2, menu/keyboard UX, settings persistence, snapshots v2, deploy loop `DeployXboxLocal`
- App runs and renders C64 on Windows 11 dev PC
- Critical finding: build-time `PublishAot=true` kills reflection `{Binding}` (FIX-XKBDPANEL-001); AOT is opt-in via `ViceSharpPublishAot=true`
50→- Last device-verified gate: **Category=Xbox 501/501**

### Turn 124 (Function)
[tool_response]
✓ Pipeline executed successfully | Window: #11500 Aquarius | Status: Ready | Pipeline: $r = & 'F:\GitHub\mcpserver... | Duration: 2.01s | Location [FileSystem]: F:\GitHub\vice-sharp

type: result
payload:
  requestId: req-20260805T080459Z-c502
  result:
    id: PLAN-ROMM-001
    title: Create a RomM client library and UI for Avalonia and Xbox
    section: romm
    priority: high
    done: false
    description:
    - "Operator request 2026-07-14: build a RomM (romm.app self-hosted ROM manager) client library plus browse/download UI in BOTH heads. Scope: (1) portable client library (new project, e.g. ViceSharp.RomM, net10.0, no UI deps) over the RomM REST API: server URL + credentials/token config, list platforms, list/search ROMs per platform, fetch metadata/cover art, download ROM payloads; System.Text.Json with source-generated JsonSerializerContext (Native-AOT-safe for the Xbox Release head); resilient/testable HTTP seam (injectable handler for unit tests, BDP tests-first). (2) Avalonia UI: a library browser panel (search, platform filter, cover grid or list, download + attach to a media slot via the existing media service). (3) Xbox 10-foot UI: controller-navigable browse page (D-pad navigation, focus-visible tiles), download into LocalState media storage, attach via IXboxSettingsGateway media API. Settings: RomM server address + auth stored per-head (Xbox: extend FEAT-XSETPERSIST-001 LocalState persistence; Avalonia: existing settings persistence); never hardcode credentials, mask in logs. Respect the existing ROM-compliance stance: RomM is the USER's own library server; ViceSharp still ships no ROMs. ViewModels stay portable per TR-MVVM-001 (Abstractions + client lib only); heads are thin XAML consumers. BDP: client-library contract tests against canned RomM API JSON first, then VM tests, then structural head-wiring asserts."
    technicalDetails: &o0 []
    implementationTasks: []
    dependsOn: *o0
    functionalRequirements: *o0
    technicalRequirements: *o0
  deprecated: true

---

### Turn 125 (Assistant)
[tool_request: write]
- file_path: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md
- content: # Handoff completion plan: PLAN-XBOXUWP residuals + full PLAN-ROMM-001

**Date:** 2026-08-05 (revised: all RomM features in scope)  
**Branch:** `feat/xbox-uwp-app` @ `a199063` (dirty tree)  
**Sources:** `docs/plans/PLAN-XBOXUWP-BDPv4.md`, `HANDOFF.md`, MCP TODOs `IMPL-XBOXUWP-*`, `FIX-XKBDNMI-001`, `PLAN-ROMM-001`, `IMPL-ROMM-001..017`  
**Process:** Byrd Development Process v4 (tests-first, red before green, 100% green gate per slice: 0 failed and 0 skipped in the slice filter)

---

## 1. Goal (definition of done)

Deliver **two closed programs** on this branch (or stacked follow-on commits on the same branch):

### Track 1: PLAN-XBOXUWP Tier D complete

1. HANDOFF OPEN residuals implemented and gated (except pure operator-hardware QA if parked with an explicit QA TODO).
2. In-progress `{x:Bind}` WIP finished (no half-migrated XAML).
3. **Category=Xbox** green (last known **501/501**, 0 fail, 0 skip; re-record actual total).
4. Desktop-intact: Determinism + Avalonia boundary not regressed.
5. MCP TODOs for completed Xbox slices reconciled with receipts.
6. **S42** Tier C (physical console / Store) remains deferred (tracked, not dropped).

### Track 2: PLAN-ROMM-001 complete (all RomM features)

All MCP slices **IMPL-ROMM-001..017** green, covering:

| Phase | IDs | Deliverable |
|-------|-----|-------------|
| A P0 | IMPL-ROMM-001 | Pin `RomM.Client` + `RomM.Client.Csdb` 1.0.0; restore from nuget.org |
| B L1-L8 | IMPL-ROMM-002..009 | Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM` adapter, browse/detail/download, launch seam, collections, CSDb gateways, covers/connection/pairing, packable nupkgs |
| C X1-X4 | IMPL-ROMM-010..013 | Xbox launcher, cover/connection stores, Library/Details/Lists pages + nav + pairing, CSDb tab |
| D A1-A4 | IMPL-ROMM-014..017 | Avalonia launcher, cover/store/gateway selection, Library/Details/Lists views + connection dialog, CSDb tab |

**Done when:** every IMPL-ROMM TODO is `done=true` with test receipts; both heads can connect to a RomM server, browse, download, and launch/attach media without shipping any Commodore ROMs in the package; PLAN-ROMM-001 closed.

### Still out of scope

- Romless VIC worktree (`FIX-ROMLESSVIC-001` / `F:\GitHub\vice-sharp-romless`) unless operator later expands scope again
- Octopus LEGION2 dirty pipeline steps (stash / separate change)
- Physical Xbox console Store cert (S42)
- Push to `origin` unless operator explicitly requests it

---

## 2. Verified current state (receipts)

### Branch and tree

- HEAD: `a199063` - FEAT-XMENUSUBPAGE-001
- Dirty (Xbox plan-related): views/controls migrating `{Binding}` -> `{x:Bind}`; HANDOFF.md refresh
- Dirty (not this plan): Octopus pipeline YAML, AiReview appsettings, untracked docs scratch
- Binding audit (dirty): **31** `x:Bind`, **1** remaining `{Binding}` (`HomePage.xaml` Title)
- **No** `ViceSharp.RomM` / `ViceSharp.Library.ViewModels` projects or `docs/*romm*` yet (greenfield RomM)

### Xbox already shipped (do not re-implement)

- Tier H S1-S35 largely done; app runs/renders C64 on Windows 11
- DX11 video, XAudio2, menu/keyboard UX, settings persistence, snapshot v2, `DeployXboxLocal`
- PublishAot opt-in only (FIX-XKBDPANEL-001); Release-UWP JIT with working bindings
- Category=Xbox last known 501/501

### Xbox real residuals

| Gap | ID | Verified root cause |
|-----|-----|---------------------|
| RESTORE no CPU NMI | FIX-XKBDNMI-001 | `IsRestorePressed` set; nothing asserts `_nmiLine` |
| Controls shortcut config | (new FEAT-XCTRLBIND-001) | InputMapping read-only; `IBindingStore` exists unused by UI |
| Per-model ROM readiness | (new FEAT-XROMPICK-001) | Model list ignores provision assessment |
| AOT-safe bindings | FEAT-XAOTBIND-001 | Dirty WIP almost done |
| B-on-controller | operator QA | Code present; needs controller checklist |
| S0/S35-S41 MCP still open | IMPL-XBOXUWP-* | Work de facto done; reconcile with receipts |
| S42 | IMPL-XBOXUWP-042 | Deferred |

### RomM locked product intent (from PLAN-ROMM-001)

- Client for **user's self-hosted RomM** (romm.app); ViceSharp still ships **zero** ROMs
- Portable libs, AOT-safe JSON source-gen, injectable HTTP for tests
- ViewModels portable (TR-MVVM-001); thin Avalonia + Xbox heads
- Settings: server URL + auth per head; never hardcode credentials; mask secrets in logs
- Xbox: download into LocalState; launch via payload attach (no arbitrary path)
- Avalonia: integrate existing shell drop/attach paths
- Packages pin: `RomM.Client` 1.0.0 + `RomM.Client.Csdb` 1.0.0 from nuget.org only

---

## 3. Locked decisions

1. **Both tracks are in scope** for this completion plan. RomM is not optional.
2. **Order:** finish Xbox residuals (Track 1 slices 0-A-E) before RomM Phase C/D UI wiring so the Xbox head is stable; RomM Phase A+B (portable libs) may start in parallel after Slice A if agents allow, but **default sequential** is safer for one agent.
3. **Default sequential order (single agent):**  
   `0 Hygiene -> A x:Bind -> B NMI -> C Controls -> D ROM picker -> E Xbox closeout`  
   then  
   `R0 RomM reqs/docs -> R1 P0 packages -> R2 L1..L8 portable -> R3 Xbox UI X1..X4 -> R4 Avalonia A1..A4 -> R5 packaging + plan close`
4. **BDP v4** every behavior slice; no skipped tests as green.
5. **RESTORE** wires real NMI edge in core (not host-only poll).
6. **AOT:** complete x:Bind; do not re-arm `PublishAot` until a later explicit Store-AOT decision.
7. **RomM credentials** never in source; stores only via head persistence seams.
8. **Unrelated dirty files** never land in plan commits.
9. **Push** only on explicit request.

---

## 4. Track 1: Xbox residual slices

### Slice 0 - Working-tree hygiene

Stash/leave unstaged: Octopus YAML, AiReview appsettings, untracked docs scratch.  
Keep: Xbox XAML/cs WIP + plan HANDOFF updates.  
**Gate:** intentional `git status` only.

### Slice A - FEAT-XAOTBIND-001 (finish dirty WIP)

- Convert last `{Binding}` (HomePage Title); finish ViewModel-before-Init pattern
- Convention test: ban reflection `{Binding ` in `src/ViceSharp.Xbox/**/*.xaml`
- No PublishAot re-enable  
**RED:** convention fails on remaining Binding.  
**GREEN:** 0 Binding; Category=Xbox green; device Settings/keyboard/menu non-blank after DeployXboxLocal.

### Slice B - FIX-XKBDNMI-001

- Wire `SetRestore` press/release to NMI interrupt line (edge via existing SystemClock latch)
- Tests: NMI on restore press; hold no spam; `*` does not NMI; DET green  
**MCP:** close FIX-XKBDNMI-001 with receipts.

### Slice C - FEAT-XCTRLBIND-001 (Controls remapping)

- Editable remappable bindings; A/B fire locked display-only; Menu/View fixed per plan default
- Persist via `IBindingStore`; load at startup  
**Tests:** rebind/save/load/reset; locked rows immutable.

### Slice D - FEAT-XROMPICK-001

- Model picker shows ROM readiness via `RomProvisionEvaluator`
- StatusText + path to ROM page; session IsBootBlocked remains hard gate  
**Tests:** Partial/Complete/Ultimax kernal-optional.

### Slice E - Tier D closeout + MCP reconcile

- DeployXboxLocal + Category=Xbox + Determinism
- Mark IMPL-XBOXUWP-000, 035-041 done only with evidence; leave 042 open
- B-controller: operator checklist or QA-XBOXCTRL-001 park
- HANDOFF snapshot: Xbox Tier D complete (RomM next / in progress)

---

## 5. Track 2: PLAN-ROMM full delivery

### Slice R0 - Requirements + design freeze (before code)

1. Ensure FR/TR/TEST for RomM exist in MCP (or create from AC IDs already cited on TODOs: AC-PKG, AC-BROWSE, AC-LAUNCH, AC-COLLECT, AC-CSDB, AC-COVER, AC-CONN, AC-XUI, AC-AUI).
2. Commit a short design note under `docs/plans/PLAN-ROMM-001.md` (architecture, project refs, auth storage, launch flow, no-ROM packaging) so implementation is decision-complete.
3. Solution layout (locked default matching TODOs):

```
src/ViceSharp.Library.ViewModels/   # portable VMs + seams (refs Protocol only per L1)
src/ViceSharp.RomM/                 # adapter: RomM.Client + Csdb + source-gen JSON
tests/.../Library/ and /RomM/       # contract + VM tests
# Heads consume Library.ViewModels + head-specific launchers/stores
```

4. NuGet pins only in Directory.Packages.props; no private feeds.

**Gate:** design doc + requirements IDs listed in session log; no product code required yet.

### Slice R1 - IMPL-ROMM-001 [P0] Package pins

**Tests first:** NuGetConfigTests / Directory.Packages pin tests (AC-PKG-04, TR-ROMM-NUGET-001).  
**Impl:** pin RomM.Client 1.0.0 + RomM.Client.Csdb 1.0.0; restore succeeds.  
**Gate:** restore + pin tests green.

### Slice R2 - Portable core (IMPL-ROMM-002..009) in dependency order

Execute strictly L1 -> L8; each slice red then green; no skipping.

| Slice | TODO | Tests-first focus | GREEN means |
|-------|------|-------------------|-------------|
| R2.1 L1 | IMPL-ROMM-002 | seam interfaces + slug map unit tests | Library.ViewModels project builds; seams compile; AC-PKG-03 / launch-02/03 / browse-08/02 |
| R2.2 L2 | IMPL-ROMM-003 | canned JSON HTTP handler tests for browse/detail/download | RomM adapter; RomMJsonContext AOT-safe; AC-BROWSE-01..03, DETAIL-01, LAUNCH-01, CONN-01/06 |
| R2.3 L3 | IMPL-ROMM-004 | LibraryBrowseViewModelTests paging/debounce/rescope | AC-BROWSE-04..07, LAUNCH-03/04/07 |
| R2.4 L4 | IMPL-ROMM-005 | launch orchestration vs fake IGameLauncher | AC-LAUNCH-04/07 |
| R2.5 L5 | IMPL-ROMM-006 | collections gateway + VMs | AC-COLLECT-01..06, DETAIL-02 |
| R2.6 L6 | IMPL-ROMM-007 | CsdbDiscovery + Local/Bridge gateways | AC-CSDB-01..06 |
| R2.7 L7 | IMPL-ROMM-008 | cover cache, pairing, auth refresh, connection VM | AC-COVER-01..04, CONN-02..05 |
| R2.8 L8 | IMPL-ROMM-009 | PackageMetadataTests + PublishNuget dry-run | AC-PKG-01/02; GPL-2.0-or-later metadata |

**Cross-cutting rules for R2:**

- Zero UI refs in portable projects
- Injectable `HttpMessageHandler` for all network tests
- Source-generated JSON only (Native AOT safe)
- Secrets redacted in any log strings (tests assert no token echo)

**Gate after R2:** portable test filter green, e.g.

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~RomM|FullyQualifiedName~Library"
```

### Slice R3 - Xbox head RomM (IMPL-ROMM-010..013)

| Slice | TODO | Scope | Validation |
|-------|------|-------|------------|
| R3.1 X1 | IMPL-ROMM-010 | XboxGameLauncher: bytes -> AttachMediaAsync + AutostartDrive8/ColdReset; no raw path | XboxGameLauncherTests |
| R3.2 X2 | IMPL-ROMM-011 | XboxCoverImageLoader + XboxRomMConnectionStore (LocalState/keystore) | unit + UWP-compile |
| R3.3 X3 | IMPL-ROMM-012 | LibraryPage, GameDetailsPage, ListsPage; nav destinations; Home entry; Settings pairing | structural Xbox tests + DeployXboxLocal smoke |
| R3.4 X4 | IMPL-ROMM-013 | Xbox CSDb tab bound to CsdbDiscoveryViewModel (Bridge) | Category=Xbox + page wiring |

**Xbox constraints:**

- AppContainer: downloads under LocalFolder only
- Use `{x:Bind}` on new pages (consistent with FEAT-XAOTBIND-001)
- internetClient already declared for ROM download path; RomM HTTP uses same capability
- Menu InputContext: Library is non-Gameplay UI (neutral joysticks)

**Gate:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Xbox"
.\build.ps1 DeployXboxLocal
# smoke: open Library, pair (fake/local server if available), browse tiles focusable
```

### Slice R4 - Avalonia head RomM (IMPL-ROMM-014..017)

| Slice | TODO | Scope |
|-------|------|-------|
| R4.1 A1 | IMPL-ROMM-014 | AvaloniaGameLauncher -> DropAndStartFileAsync / AttachAsync |
| R4.2 A2 | IMPL-ROMM-015 | Avalonia cover loader + desktop connection store; Local vs Bridge CSDb selection |
| R4.3 A3 | IMPL-ROMM-016 | LibraryView, GameDetailsView, ListsView, MainWindow, connection dialog |
| R4.4 A4 | IMPL-ROMM-017 | Avalonia CSDb tab |

**Gate:** Avalonia-focused tests + `dotnet build` desktop slnx; optional `.\build.ps1 RunAvalonia` smoke if environment allows.

**Desktop-intact after R4:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Avalonia"
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Determinism"
```

### Slice R5 - Packaging, docs, MCP close

1. Confirm both libraries pack (IMPL-ROMM-009) and heads do not ship ROM bins
2. User-facing notes: settings for RomM URL/auth; Xbox vs Avalonia differences
3. Mark PLAN-ROMM-001 + all IMPL-ROMM-001..017 done with command receipts
4. Final HANDOFF: Xbox Tier D + RomM complete; S42 deferred

---

## 6. Dependency graph (high level)

```
Xbox Track 1:  0 -> A -> B -> C -> D -> E
                         \
RomM Track 2:             R0 -> R1 -> R2(L1..L8) -> R3(X1..X4) -> R4(A1..A4) -> R5
                                      ^
                                      portable may start after R0/R1 even during late Track 1
                                      if dual-agent; single-agent waits for E before R3
```

**Hard edges:**

- R3 depends on R2 complete + Xbox head stable (prefer after Slice A at minimum; after E preferred)
- R4 depends on R2 complete; independent of R3 except shared portable
- R1 before any RomM project package reference

---

## 7. Validation matrix (final)

| Gate | Command / check | Pass criteria |
|------|-----------------|---------------|
| Xbox unit | `Category=Xbox` | 0 fail, 0 skip |
| RomM/Library unit | `FullyQualifiedName~RomM\|Library` | 0 fail, 0 skip |
| Determinism | `Category=Determinism` | 0 fail |
| Avalonia boundary | Avalonia filter / boundary tests | green |
| Xbox deploy | `DeployXboxLocal` | boots, frames, Library navigable |
| No ROM payload | existing GPL/no-ROM tests still green | zero kernal/basic/chargen bins packaged |
| MCP | todo_get on all plan IDs | done flags match evidence |
| HANDOFF | file on disk | completion snapshot accurate |

Optional full baseline (detached ~20 min) after R5:

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"
```

---

## 8. Commit strategy

Small commits per slice; Conventional Commits examples:

- `feat(xbox): complete x:Bind migration [FEAT-XAOTBIND-001]`
- `fix(core): RESTORE asserts CPU NMI edge [FIX-XKBDNMI-001]`
- `feat(xbox): remappable control bindings [FEAT-XCTRLBIND-001]`
- `feat(xbox): model picker ROM readiness [FEAT-XROMPICK-001]`
- `build(deps): pin RomM.Client 1.0.0 [IMPL-ROMM-001]`
- `feat(romm): Library.ViewModels seams [IMPL-ROMM-002]`
- ... one commit per IMPL-ROMM when practical
- `docs: handoff Xbox Tier D + RomM complete`

Sign with `model-effort@agent` + confidence. No push unless asked.

---

## 9. Risks

| Risk | Mitigation |
|------|------------|
| RomM.Client 1.0.0 API mismatch vs TODO assumptions | R1/R2.2 contract tests against real package docs; adjust adapter, not ship ROMs |
| No live RomM server in CI | canned JSON + injectable handler; optional integration tests gated env |
| Xbox AOT later breaks RomM reflection | source-gen JSON + x:Bind from day one |
| Scope blow-up on CSDb | keep L6/X4/A4 within AC-CSDB list; no extra social features |
| Dual-head drift | portable VMs shared; head tests only for launchers/stores/XAML |
| Dirty Octopus/YAML pollution | Slice 0 hygiene |

---

## 10. Success checklist

### Xbox

- [ ] Category=Xbox green
- [ ] RESTORE -> NMI proven
- [ ] Controls remapping persists
- [ ] Model ROM readiness visible
- [ ] x:Bind complete; Settings/keyboard non-blank on device
- [ ] S42 still deferred; S0/S35-S41 TODOs reconciled

### RomM

- [ ] IMPL-ROMM-001..017 all done with receipts
- [ ] Portable libs packable; nuget.org-only pins
- [ ] Xbox: pair, browse, download to LocalState, launch attach
- [ ] Avalonia: connect, browse, launch/attach via shell seams
- [ ] CSDb surfaces on both heads (X4/A4)
- [ ] Zero packaged Commodore ROMs; secrets not logged
- [ ] PLAN-ROMM-001 closed

### Wrap

- [ ] HANDOFF.md updated
- [ ] Session log decisions/actions recorded via plugin

---

## 11. Execution summary

```
Track 1 (Xbox residuals):  0, A, B, C, D, E
Track 2 (full RomM):       R0, R1, R2.1-R2.8, R3.1-R3.4, R4.1-R4.4, R5
```

This is the complete handoff completion plan with **all RomM features in scope** as requested.


### Turn 126 (Function)
[tool_response]
Wrote file successfully to C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md.

### Turn 127 (Assistant)
[tool_request: exit_plan_mode]

### Turn 128 (Function)
[tool_response]
Your plan has been approved. You can now start coding.

Your plan has been saved at: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md

## Plan:
# Handoff completion plan: PLAN-XBOXUWP residuals + full PLAN-ROMM-001

**Date:** 2026-08-05 (revised: all RomM features in scope)  
**Branch:** `feat/xbox-uwp-app` @ `a199063` (dirty tree)  
**Sources:** `docs/plans/PLAN-XBOXUWP-BDPv4.md`, `HANDOFF.md`, MCP TODOs `IMPL-XBOXUWP-*`, `FIX-XKBDNMI-001`, `PLAN-ROMM-001`, `IMPL-ROMM-001..017`  
**Process:** Byrd Development Process v4 (tests-first, red before green, 100% green gate per slice: 0 failed and 0 skipped in the slice filter)

---

## 1. Goal (definition of done)

Deliver **two closed programs** on this branch (or stacked follow-on commits on the same branch):

### Track 1: PLAN-XBOXUWP Tier D complete

1. HANDOFF OPEN residuals implemented and gated (except pure operator-hardware QA if parked with an explicit QA TODO).
2. In-progress `{x:Bind}` WIP finished (no half-migrated XAML).
3. **Category=Xbox** green (last known **501/501**, 0 fail, 0 skip; re-record actual total).
4. Desktop-intact: Determinism + Avalonia boundary not regressed.
5. MCP TODOs for completed Xbox slices reconciled with receipts.
6. **S42** Tier C (physical console / Store) remains deferred (tracked, not dropped).

### Track 2: PLAN-ROMM-001 complete (all RomM features)

All MCP slices **IMPL-ROMM-001..017** green, covering:

| Phase | IDs | Deliverable |
|-------|-----|-------------|
| A P0 | IMPL-ROMM-001 | Pin `RomM.Client` + `RomM.Client.Csdb` 1.0.0; restore from nuget.org |
| B L1-L8 | IMPL-ROMM-002..009 | Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM` adapter, browse/detail/download, launch seam, collections, CSDb gateways, covers/connection/pairing, packable nupkgs |
| C X1-X4 | IMPL-ROMM-010..013 | Xbox launcher, cover/connection stores, Library/Details/Lists pages + nav + pairing, CSDb tab |
| D A1-A4 | IMPL-ROMM-014..017 | Avalonia launcher, cover/store/gateway selection, Library/Details/Lists views + connection dialog, CSDb tab |

**Done when:** every IMPL-ROMM TODO is `done=true` with test receipts; both heads can connect to a RomM server, browse, download, and launch/attach media without shipping any Commodore ROMs in the package; PLAN-ROMM-001 closed.

### Still out of scope

- Romless VIC worktree (`FIX-ROMLESSVIC-001` / `F:\GitHub\vice-sharp-romless`) unless operator later expands scope again
- Octopus LEGION2 dirty pipeline steps (stash / separate change)
- Physical Xbox console Store cert (S42)
- Push to `origin` unless operator explicitly requests it

---

## 2. Verified current state (receipts)

### Branch and tree

- HEAD: `a199063` - FEAT-XMENUSUBPAGE-001
- Dirty (Xbox plan-related): views/controls migrating `{Binding}` -> `{x:Bind}`; HANDOFF.md refresh
- Dirty (not this plan): Octopus pipeline YAML, AiReview appsettings, untracked docs scratch
- Binding audit (dirty): **31** `x:Bind`, **1** remaining `{Binding}` (`HomePage.xaml` Title)
- **No** `ViceSharp.RomM` / `ViceSharp.Library.ViewModels` projects or `docs/*romm*` yet (greenfield RomM)

### Xbox already shipped (do not re-implement)

- Tier H S1-S35 largely done; app runs/renders C64 on Windows 11
- DX11 video, XAudio2, menu/keyboard UX, settings persistence, snapshot v2, `DeployXboxLocal`
- PublishAot opt-in only (FIX-XKBDPANEL-001); Release-UWP JIT with working bindings
- Category=Xbox last known 501/501

### Xbox real residuals

| Gap | ID | Verified root cause |
|-----|-----|---------------------|
| RESTORE no CPU NMI | FIX-XKBDNMI-001 | `IsRestorePressed` set; nothing asserts `_nmiLine` |
| Controls shortcut config | (new FEAT-XCTRLBIND-001) | InputMapping read-only; `IBindingStore` exists unused by UI |
| Per-model ROM readiness | (new FEAT-XROMPICK-001) | Model list ignores provision assessment |
| AOT-safe bindings | FEAT-XAOTBIND-001 | Dirty WIP almost done |
| B-on-controller | operator QA | Code present; needs controller checklist |
| S0/S35-S41 MCP still open | IMPL-XBOXUWP-* | Work de facto done; reconcile with receipts |
| S42 | IMPL-XBOXUWP-042 | Deferred |

### RomM locked product intent (from PLAN-ROMM-001)

- Client for **user's self-hosted RomM** (romm.app); ViceSharp still ships **zero** ROMs
- Portable libs, AOT-safe JSON source-gen, injectable HTTP for tests
- ViewModels portable (TR-MVVM-001); thin Avalonia + Xbox heads
- Settings: server URL + auth per head; never hardcode credentials; mask secrets in logs
- Xbox: download into LocalState; launch via payload attach (no arbitrary path)
- Avalonia: integrate existing shell drop/attach paths
- Packages pin: `RomM.Client` 1.0.0 + `RomM.Client.Csdb` 1.0.0 from nuget.org only

---

## 3. Locked decisions

1. **Both tracks are in scope** for this completion plan. RomM is not optional.
2. **Order:** finish Xbox residuals (Track 1 slices 0-A-E) before RomM Phase C/D UI wiring so the Xbox head is stable; RomM Phase A+B (portable libs) may start in parallel after Slice A if agents allow, but **default sequential** is safer for one agent.
3. **Default sequential order (single agent):**  
   `0 Hygiene -> A x:Bind -> B NMI -> C Controls -> D ROM picker -> E Xbox closeout`  
   then  
   `R0 RomM reqs/docs -> R1 P0 packages -> R2 L1..L8 portable -> R3 Xbox UI X1..X4 -> R4 Avalonia A1..A4 -> R5 packaging + plan close`
4. **BDP v4** every behavior slice; no skipped tests as green.
5. **RESTORE** wires real NMI edge in core (not host-only poll).
6. **AOT:** complete x:Bind; do not re-arm `PublishAot` until a later explicit Store-AOT decision.
7. **RomM credentials** never in source; stores only via head persistence seams.
8. **Unrelated dirty files** never land in plan commits.
9. **Push** only on explicit request.

---

## 4. Track 1: Xbox residual slices

### Slice 0 - Working-tree hygiene

Stash/leave unstaged: Octopus YAML, AiReview appsettings, untracked docs scratch.  
Keep: Xbox XAML/cs WIP + plan HANDOFF updates.  
**Gate:** intentional `git status` only.

### Slice A - FEAT-XAOTBIND-001 (finish dirty WIP)

- Convert last `{Binding}` (HomePage Title); finish ViewModel-before-Init pattern
- Convention test: ban reflection `{Binding ` in `src/ViceSharp.Xbox/**/*.xaml`
- No PublishAot re-enable  
**RED:** convention fails on remaining Binding.  
**GREEN:** 0 Binding; Category=Xbox green; device Settings/keyboard/menu non-blank after DeployXboxLocal.

### Slice B - FIX-XKBDNMI-001

- Wire `SetRestore` press/release to NMI interrupt line (edge via existing SystemClock latch)
- Tests: NMI on restore press; hold no spam; `*` does not NMI; DET green  
**MCP:** close FIX-XKBDNMI-001 with receipts.

### Slice C - FEAT-XCTRLBIND-001 (Controls remapping)

- Editable remappable bindings; A/B fire locked display-only; Menu/View fixed per plan default
- Persist via `IBindingStore`; load at startup  
**Tests:** rebind/save/load/reset; locked rows immutable.

### Slice D - FEAT-XROMPICK-001

- Model picker shows ROM readiness via `RomProvisionEvaluator`
- StatusText + path to ROM page; session IsBootBlocked remains hard gate  
**Tests:** Partial/Complete/Ultimax kernal-optional.

### Slice E - Tier D closeout + MCP reconcile

- DeployXboxLocal + Category=Xbox + Determinism
- Mark IMPL-XBOXUWP-000, 035-041 done only with evidence; leave 042 open
- B-controller: operator checklist or QA-XBOXCTRL-001 park
- HANDOFF snapshot: Xbox Tier D complete (RomM next / in progress)

---

## 5. Track 2: PLAN-ROMM full delivery

### Slice R0 - Requirements + design freeze (before code)

1. Ensure FR/TR/TEST for RomM exist in MCP (or create from AC IDs already cited on TODOs: AC-PKG, AC-BROWSE, AC-LAUNCH, AC-COLLECT, AC-CSDB, AC-COVER, AC-CONN, AC-XUI, AC-AUI).
2. Commit a short design note under `docs/plans/PLAN-ROMM-001.md` (architecture, project refs, auth storage, launch flow, no-ROM packaging) so implementation is decision-complete.
3. Solution layout (locked default matching TODOs):

```
src/ViceSharp.Library.ViewModels/   # portable VMs + seams (refs Protocol only per L1)
src/ViceSharp.RomM/                 # adapter: RomM.Client + Csdb + source-gen JSON
tests/.../Library/ and /RomM/       # contract + VM tests
# Heads consume Library.ViewModels + head-specific launchers/stores
```

4. NuGet pins only in Directory.Packages.props; no private feeds.

**Gate:** design doc + requirements IDs listed in session log; no product code required yet.

### Slice R1 - IMPL-ROMM-001 [P0] Package pins

**Tests first:** NuGetConfigTests / Directory.Packages pin tests (AC-PKG-04, TR-ROMM-NUGET-001).  
**Impl:** pin RomM.Client 1.0.0 + RomM.Client.Csdb 1.0.0; restore succeeds.  
**Gate:** restore + pin tests green.

### Slice R2 - Portable core (IMPL-ROMM-002..009) in dependency order

Execute strictly L1 -> L8; each slice red then green; no skipping.

| Slice | TODO | Tests-first focus | GREEN means |
|-------|------|-------------------|-------------|
| R2.1 L1 | IMPL-ROMM-002 | seam interfaces + slug map unit tests | Library.ViewModels project builds; seams compile; AC-PKG-03 / launch-02/03 / browse-08/02 |
| R2.2 L2 | IMPL-ROMM-003 | canned JSON HTTP handler tests for browse/detail/download | RomM adapter; RomMJsonContext AOT-safe; AC-BROWSE-01..03, DETAIL-01, LAUNCH-01, CONN-01/06 |
| R2.3 L3 | IMPL-ROMM-004 | LibraryBrowseViewModelTests paging/debounce/rescope | AC-BROWSE-04..07, LAUNCH-03/04/07 |
| R2.4 L4 | IMPL-ROMM-005 | launch orchestration vs fake IGameLauncher | AC-LAUNCH-04/07 |
| R2.5 L5 | IMPL-ROMM-006 | collections gateway + VMs | AC-COLLECT-01..06, DETAIL-02 |
| R2.6 L6 | IMPL-ROMM-007 | CsdbDiscovery + Local/Bridge gateways | AC-CSDB-01..06 |
| R2.7 L7 | IMPL-ROMM-008 | cover cache, pairing, auth refresh, connection VM | AC-COVER-01..04, CONN-02..05 |
| R2.8 L8 | IMPL-ROMM-009 | PackageMetadataTests + PublishNuget dry-run | AC-PKG-01/02; GPL-2.0-or-later metadata |

**Cross-cutting rules for R2:**

- Zero UI refs in portable projects
- Injectable `HttpMessageHandler` for all network tests
- Source-generated JSON only (Native AOT safe)
- Secrets redacted in any log strings (tests assert no token echo)

**Gate after R2:** portable test filter green, e.g.

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~RomM|FullyQualifiedName~Library"
```

### Slice R3 - Xbox head RomM (IMPL-ROMM-010..013)

| Slice | TODO | Scope | Validation |
|-------|------|-------|------------|
| R3.1 X1 | IMPL-ROMM-010 | XboxGameLauncher: bytes -> AttachMediaAsync + AutostartDrive8/ColdReset; no raw path | XboxGameLauncherTests |
| R3.2 X2 | IMPL-ROMM-011 | XboxCoverImageLoader + XboxRomMConnectionStore (LocalState/keystore) | unit + UWP-compile |
| R3.3 X3 | IMPL-ROMM-012 | LibraryPage, GameDetailsPage, ListsPage; nav destinations; Home entry; Settings pairing | structural Xbox tests + DeployXboxLocal smoke |
| R3.4 X4 | IMPL-ROMM-013 | Xbox CSDb tab bound to CsdbDiscoveryViewModel (Bridge) | Category=Xbox + page wiring |

**Xbox constraints:**

- AppContainer: downloads under LocalFolder only
- Use `{x:Bind}` on new pages (consistent with FEAT-XAOTBIND-001)
- internetClient already declared for ROM download path; RomM HTTP uses same capability
- Menu InputContext: Library is non-Gameplay UI (neutral joysticks)

**Gate:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Xbox"
.\build.ps1 DeployXboxLocal
# smoke: open Library, pair (fake/local server if available), browse tiles focusable
```

### Slice R4 - Avalonia head RomM (IMPL-ROMM-014..017)

| Slice | TODO | Scope |
|-------|------|-------|
| R4.1 A1 | IMPL-ROMM-014 | AvaloniaGameLauncher -> DropAndStartFileAsync / AttachAsync |
| R4.2 A2 | IMPL-ROMM-015 | Avalonia cover loader + desktop connection store; Local vs Bridge CSDb selection |
| R4.3 A3 | IMPL-ROMM-016 | LibraryView, GameDetailsView, ListsView, MainWindow, connection dialog |
| R4.4 A4 | IMPL-ROMM-017 | Avalonia CSDb tab |

**Gate:** Avalonia-focused tests + `dotnet build` desktop slnx; optional `.\build.ps1 RunAvalonia` smoke if environment allows.

**Desktop-intact after R4:**

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Avalonia"
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category=Determinism"
```

### Slice R5 - Packaging, docs, MCP close

1. Confirm both libraries pack (IMPL-ROMM-009) and heads do not ship ROM bins
2. User-facing notes: settings for RomM URL/auth; Xbox vs Avalonia differences
3. Mark PLAN-ROMM-001 + all IMPL-ROMM-001..017 done with command receipts
4. Final HANDOFF: Xbox Tier D + RomM complete; S42 deferred

---

## 6. Dependency graph (high level)

```
Xbox Track 1:  0 -> A -> B -> C -> D -> E
                         \
RomM Track 2:             R0 -> R1 -> R2(L1..L8) -> R3(X1..X4) -> R4(A1..A4) -> R5
                                      ^
                                      portable may start after R0/R1 even during late Track 1
                                      if dual-agent; single-agent waits for E before R3
```

**Hard edges:**

- R3 depends on R2 complete + Xbox head stable (prefer after Slice A at minimum; after E preferred)
- R4 depends on R2 complete; independent of R3 except shared portable
- R1 before any RomM project package reference

---

## 7. Validation matrix (final)

| Gate | Command / check | Pass criteria |
|------|-----------------|---------------|
| Xbox unit | `Category=Xbox` | 0 fail, 0 skip |
| RomM/Library unit | `FullyQualifiedName~RomM\|Library` | 0 fail, 0 skip |
| Determinism | `Category=Determinism` | 0 fail |
| Avalonia boundary | Avalonia filter / boundary tests | green |
| Xbox deploy | `DeployXboxLocal` | boots, frames, Library navigable |
| No ROM payload | existing GPL/no-ROM tests still green | zero kernal/basic/chargen bins packaged |
| MCP | todo_get on all plan IDs | done flags match evidence |
| HANDOFF | file on disk | completion snapshot accurate |

Optional full baseline (detached ~20 min) after R5:

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"
```

---

## 8. Commit strategy

Small commits per slice; Conventional Commits examples:

- `feat(xbox): complete x:Bind migration [FEAT-XAOTBIND-001]`
- `fix(core): RESTORE asserts CPU NMI edge [FIX-XKBDNMI-001]`
- `feat(xbox): remappable control bindings [FEAT-XCTRLBIND-001]`
- `feat(xbox): model picker ROM readiness [FEAT-XROMPICK-001]`
- `build(deps): pin RomM.Client 1.0.0 [IMPL-ROMM-001]`
- `feat(romm): Library.ViewModels seams [IMPL-ROMM-002]`
- ... one commit per IMPL-ROMM when practical
- `docs: handoff Xbox Tier D + RomM complete`

Sign with `model-effort@agent` + confidence. No push unless asked.

---

## 9. Risks

| Risk | Mitigation |
|------|------------|
| RomM.Client 1.0.0 API mismatch vs TODO assumptions | R1/R2.2 contract tests against real package docs; adjust adapter, not ship ROMs |
| No live RomM server in CI | canned JSON + injectable handler; optional integration tests gated env |
| Xbox AOT later breaks RomM reflection | source-gen JSON + x:Bind from day one |
| Scope blow-up on CSDb | keep L6/X4/A4 within AC-CSDB list; no extra social features |
| Dual-head drift | portable VMs shared; head tests only for launchers/stores/XAML |
| Dirty Octopus/YAML pollution | Slice 0 hygiene |

---

## 10. Success checklist

### Xbox

- [ ] Category=Xbox green
- [ ] RESTORE -> NMI proven
- [ ] Controls remapping persists
- [ ] Model ROM readiness visible
- [ ] x:Bind complete; Settings/keyboard non-blank on device
- [ ] S42 still deferred; S0/S35-S41 TODOs reconciled

### RomM

- [ ] IMPL-ROMM-001..017 all done with receipts
- [ ] Portable libs packable; nuget.org-only pins
- [ ] Xbox: pair, browse, download to LocalState, launch attach
- [ ] Avalonia: connect, browse, launch/attach via shell seams
- [ ] CSDb surfaces on both heads (X4/A4)
- [ ] Zero packaged Commodore ROMs; secrets not logged
- [ ] PLAN-ROMM-001 closed

### Wrap

- [ ] HANDOFF.md updated
- [ ] Session log decisions/actions recorded via plugin

---

## 11. Execution summary

```
Track 1 (Xbox residuals):  0, A, B, C, D, E
Track 2 (full RomM):       R0, R1, R2.1-R2.8, R3.1-R3.4, R4.1-R4.4, R5
```

This is the complete handoff completion plan with **all RomM features in scope** as requested.


### Turn 129 (Human)
The user sent a message while you were working:
<user_query>
The user approved the plan with the following review comments:

@plan.md:38
This is already done, merge it.

@plan.md:39
Just finish it

@plan.md:41
Origin should now be github and github is now default and approved.
</user_query>

### Turn 130 (Assistant)
Plan approved with review notes. Reading the referenced plan lines and starting execution.
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md
- offset: 30
- limit: 30
[tool_request: todo_write]
- todos: [{"id":"0","content":"Slice 0: hygiene + apply review notes to plan","status":"in_progress"},{"id":"A","content":"Slice A: finish x:Bind migration (FEAT-XAOTBIND-001)","status":"pending"},{"id":"B","content":"Slice B: FIX-XKBDNMI-001 RESTORE->NMI","status":"pending"},{"id":"C","content":"Slice C: FEAT-XCTRLBIND-001 Controls remapping","status":"pending"},{"id":"D","content":"Slice D: FEAT-XROMPICK-001 model ROM readiness","status":"pending"},{"id":"E","content":"Slice E: Xbox Tier D closeout + MCP","status":"pending"},{"id":"R","content":"Track 2: PLAN-ROMM full delivery","status":"pending"}]
- merge: false
[tool_request: run_terminal_command]
- command: git -C "F:\GitHub\vice-sharp" remote -v; git -C "F:\GitHub\vice-sharp" status -sb; git -C "F:\GitHub\vice-sharp" branch -vv | Select-Object -First 5
- description: Check remotes, branch tracking, status

### Turn 131 (Function)
[tool_response]
- [in_progress] 0: Slice 0: hygiene + apply review notes to plan
- [pending] A: Slice A: finish x:Bind migration (FEAT-XAOTBIND-001)
- [pending] B: Slice B: FIX-XKBDNMI-001 RESTORE->NMI
- [pending] C: Slice C: FEAT-XCTRLBIND-001 Controls remapping
- [pending] D: Slice D: FEAT-XROMPICK-001 model ROM readiness
- [pending] E: Slice E: Xbox Tier D closeout + MCP
- [pending] R: Track 2: PLAN-ROMM full delivery


### Turn 132 (Function)
[tool_response]
30→| B L1-L8 | IMPL-ROMM-002..009 | Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM` adapter, browse/detail/download, launch seam, collections, CSDb gateways, covers/connection/pairing, packable nupkgs |
| C X1-X4 | IMPL-ROMM-010..013 | Xbox launcher, cover/connection stores, Library/Details/Lists pages + nav + pairing, CSDb tab |
| D A1-A4 | IMPL-ROMM-014..017 | Avalonia launcher, cover/store/gateway selection, Library/Details/Lists views + connection dialog, CSDb tab |

**Done when:** every IMPL-ROMM TODO is `done=true` with test receipts; both heads can connect to a RomM server, browse, download, and launch/attach media without shipping any Commodore ROMs in the package; PLAN-ROMM-001 closed.

### Still out of scope

- Romless VIC worktree (`FIX-ROMLESSVIC-001` / `F:\GitHub\vice-sharp-romless`) unless operator later expands scope again
- Octopus LEGION2 dirty pipeline steps (stash / separate change)
40→- Physical Xbox console Store cert (S42)
- Push to `origin` unless operator explicitly requests it

---

## 2. Verified current state (receipts)

### Branch and tree

- HEAD: `a199063` - FEAT-XMENUSUBPAGE-001
50→- Dirty (Xbox plan-related): views/controls migrating `{Binding}` -> `{x:Bind}`; HANDOFF.md refresh
- Dirty (not this plan): Octopus pipeline YAML, AiReview appsettings, untracked docs scratch
- Binding audit (dirty): **31** `x:Bind`, **1** remaining `{Binding}` (`HomePage.xaml` Title)
- **No** `ViceSharp.RomM` / `ViceSharp.Library.ViewModels` projects or `docs/*romm*` yet (greenfield RomM)

### Xbox already shipped (do not re-implement)

- Tier H S1-S35 largely done; app runs/renders C64 on Windows 11
- DX11 video, XAudio2, menu/keyboard UX, settings persistence, snapshot v2, `DeployXboxLocal`
- PublishAot opt-in only (FIX-XKBDPANEL-001); Release-UWP JIT with working bindings

### Turn 133 (Function)
[tool_response]
exit: 0
azurehttps://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp (fetch)
azurehttps://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp (push)
originhttps://github.com/sharpninja/vice-sharp.git (fetch)
originhttps://github.com/sharpninja/vice-sharp.git (push)
## feat/xbox-uwp-app
 M HANDOFF.md
 M azure-pipelines.ci.yml
 M azure-pipelines.release.yml
 M src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml
 M src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
 M src/ViceSharp.Xbox/Views/AboutPage.xaml
 M src/ViceSharp.Xbox/Views/AboutPage.xaml.cs
 M src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml
 M src/ViceSharp.Xbox/Views/DeviceSetupPage.xaml.cs
 M src/ViceSharp.Xbox/Views/InputMappingPage.xaml
 M src/ViceSharp.Xbox/Views/InputMappingPage.xaml.cs
 M src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml
 M src/ViceSharp.Xbox/Views/RomProvisioningPage.xaml.cs
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 M tests/ViceSharp.AiReview.Tests/appsettings.aiunit.json
?? docs/S-Blox/
?? docs/reviews/code-review-20260714T001056552Z.md
?? docs/reviews/code-review-20260714T004256563Z.md
?? docs/romless-vic-badline-fix.md
  develop                          aaa53e3 test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]
  feat/drive-family-and-device-art b6b7753 docs(requirements): regenerate wiki requirement exports (drive-family FR/TR/TEST + BUG-DRV1540-001)
+ feat/romm-integration            978b5a6 (F:/GitHub/vice-sharp-romm) fix(xbox): OnLaunched composed the shell twice, leaking an unarmed gamepad pump
* feat/xbox-uwp-app                a199063 feat(xbox): B steps back on menu subpages; subpages expand to the full window [PLAN-XBOXUWP FEAT-XMENUSUBPAGE-001]
+ fix/romless-vic-content          7f57f92 (F:/GitHub/vice-sharp-romless) [azure/fix/romless-vic-content] test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]


### Turn 134 (Assistant)
Applying review notes: merge romless VIC (done), finish Octopus pipeline work, treat GitHub `origin` as default/approved. Checking the romm worktree and starting execution.
[tool_request: search_replace]
- file_path: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md
- old_string: ### Still out of scope

- Romless VIC worktree (`FIX-ROMLESSVIC-001` / `F:\GitHub\vice-sharp-romless`) unless operator later expands scope again
- Octopus LEGION2 dirty pipeline steps (stash / separate change)
- Physical Xbox console Store cert (S42)
- Push to `origin` unless operator explicitly requests it
- new_string: ### Still out of scope

- Physical Xbox console Store cert (S42)

### Review amendments (approved 2026-08-05)

- **Romless VIC (`FIX-ROMLESSVIC-001`)**: already done; **merge it** into this branch (do not re-implement).
- **Octopus LEGION2 pipeline steps**: not out of scope; **finish and ship** the dirty `azure-pipelines.*.yml` work.
- **Remote / push**: `origin` is **GitHub** (`https://github.com/sharpninja/vice-sharp.git`) and is the default approved remote. Push to `origin` is go-by-default for completed work (lab authorization).
[tool_request: run_terminal_command]
- command: # Romless: where is the fix and how does it relate to feat/xbox-uwp-app
git -C "F:\GitHub\vice-sharp" log --oneline develop -5
git -C "F:\GitHub\vice-sharp" log --oneline fix/romless-vic-content -5 2>$null
git -C "F:\GitHub\vice-sharp" merge-base feat/xbox-uwp-app develop
git -C "F:\GitHub\vice-sharp" log --oneline feat/xbox-uwp-app..develop | Select-Object -First 15
Write-Output '--- romm worktree ---'
git -C "F:\GitHub\vice-sharp-romm" status -sb 2>$null
git -C "F:\GitHub\vice-sharp-romm" log --oneline -5 2>$null
Write-Output '--- octopus diff summary ---'
git -C "F:\GitHub\vice-sharp" diff --stat -- azure-pipelines.ci.yml azure-pipelines.release.yml
git -C "F:\GitHub\vice-sharp" diff -- azure-pipelines.ci.yml | Select-Object -First 60

- description: Inspect romless merge base, romm worktree, octopus diff
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\HomePage.xaml
- limit: 50
[tool_request: grep]
- pattern: \{Binding
- path: F:\GitHub\vice-sharp\src\ViceSharp.Xbox
- glob: *.xaml
[tool_request: grep]
- pattern: Binding |x:Bind|XboxUwp|Category=Xbox
- path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness
- glob: *Xbox*
- head_limit: 40

### Turn 135 (Function)
[tool_response]
1→<!--
  PLAN-XBOXUWP: the shell menu. FEAT-XMENUSLIDE-001 (operator 2026-07-14: "pinned and
  consuming space like the keyboard", NOT floating): the Page IS the opaque right-dock
  panel; the App grid docks the shell Frame in a right Auto column, so showing the menu
  SHRINKS the emulator's star column (the C64 held PAUSED while it is up,
  FEAT-XMENUPAUSE-001) exactly like the keyboard's bottom Auto row - never an overlay.
  It slides in with the keyboard's edge transition. Buttons use the app-level C64 keycap
  style (Styles/C64Keycaps.xaml). Menu actions: Close Menu is the FIRST button and takes
  focus on every open (FEAT-XMENUFOCUS-001), Save/Load persist and restore a machine
10→  snapshot (FEAT-XMENUSNAP-001), every dismissal resumes the machine, and Restart (cold
  reset) is the LAST button. Inert on the net10.0 fallback.
-->
<Page
    x:Class="ViceSharp.Xbox.Views.HomePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    RequiresPointer="Never"
    Background="Transparent">
    <Border
20→        Background="#FF101418"
        BorderBrush="#40FFFFFF"
        BorderThickness="1,0,0,0"
        Padding="16,14">
        <Border.Transitions>
            <TransitionCollection>
                <EdgeUIThemeTransition Edge="Right" />
            </TransitionCollection>
        </Border.Transitions>
        <StackPanel Spacing="6" MinWidth="300" VerticalAlignment="Center">
30→                <TextBlock
                    Text="{Binding Title}"
                    FontFamily="{StaticResource C64KeyFont}"
                    FontSize="22"
                    Foreground="#FFE8E4D8"
                    HorizontalAlignment="Center"
                    Margin="0,0,0,6" />
                <!--
                  FEAT-XMENUFOCUS-001 (operator 2026-07-14): Close Menu is the TOP
                  button and receives focus on every menu open, so a single press of A
40→                  dismisses, restoring the emulator's full width and resuming it
                  (FEAT-XMENUPAUSE-001).
                -->
                <Button
                    x:Name="CloseMenuButton"
                    Content="Close Menu"
                    Style="{StaticResource C64KeyButtonStyle}"
                    Click="OnCloseMenu" />
                <Button
                    Content="Save"
50→                    Style="{StaticResource C64KeyButtonStyle}"

### Turn 136 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 27 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostShells\XboxUwpHeadTests.cs
22:///     framework, the ViceSharpXboxUwp switch, UseUwp, PublishAot, and the
30:public sealed class XboxUwpHeadTests
41:        Assert.Contains("ViceSharpXboxUwp", csproj);
92:        // single ViceSharpXboxUwp chokepoint to true. A .slnx solution config
96:            "<ViceSharpXboxUwp Condition=\"'$(ViceSharpXboxUwp)'=='' and '$(Configuration)'=='Debug-UWP'\">true</ViceSharpXboxUwp>";
98:            "<ViceSharpXboxUwp Condition=\"'$(ViceSharpXboxUwp)'==''\">false</ViceSharpXboxUwp>";
129:        // Xbox-compliant configuration." Release-UWP flips the SAME ViceSharpXboxUwp
132:            "<ViceSharpXboxUwp Condition=\"'$(ViceSharpXboxUwp)'=='' and '$(Configuration)'=='Release-UWP'\">true</ViceSharpXboxUwp>";
134:            "<ViceSharpXboxUwp Condition=\"'$(ViceSharpXboxUwp)'==''\">false</ViceSharpXboxUwp>";

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostShells\XboxUwpHeadShellTests.cs
17:/// Assert.Skip - so it always executes. It EXTENDS <see cref="XboxUwpHeadTests"/> (S4)
33:public sealed class XboxUwpHeadShellTests

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostShells\XboxManifestTests.cs
13:/// (ViceSharpXboxUwp==true) while the workload-free net10.0 fallback ignores it.
31:///     by the ViceSharpXboxUwp==true condition (the fallback TFM ignores it).
116:        // the ViceSharpXboxUwp==true condition, so the workload-free net10.0
134:                group.Value.Contains("'$(ViceSharpXboxUwp)'=='true'", StringComparison.Ordinal),
135:                "Expected the Package.appxmanifest ItemGroup to be guarded by the ViceSharpXboxUwp==true condition.");

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxAudioBackendFactoryTests.cs
27:/// <c>Assert.Skip</c>), Category=Xbox. The env var is saved/restored in a finally so

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxBindingPersistenceTests.cs
60:        // ButtonBinding is a record: SequenceEqual compares each row by value.

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxGplComplianceTests.cs
119:    /// and the csproj includes the vkm glob as Content guarded by ViceSharpXboxUwp==true.
172:                group.Contains("'$(ViceSharpXboxUwp)'=='true'", StringComparison.Ordinal),
173:                "Expected the vkm Content ItemGroup to be guarded by the ViceSharpXboxUwp==true condition "

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxNukeConfigTests.cs
19:///     runs on a plain net10.0 agent (solution build + the <c>Category=Xbox</c>
71:    /// runs the <c>Category=Xbox</c> tests, and links the workload-free head under
74:    /// body references the <c>Category=Xbox</c> test filter and the AOT publish
75:    /// (<c>PublishAot</c> over the <c>ViceSharpXboxUwp</c>=false fallback).
85:        // The off-console gate runs the Category=Xbox tests ...
86:        Assert.Contains("Category=Xbox", validateBlock, StringComparison.Ordinal);
</workspace_result>

### Turn 137 (Function)
[tool_response]
The file C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md has been updated successfully.

### Turn 138 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 2 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Controls\VirtualKeyboardOverlay.xaml
40:                        <ItemsControl ItemsSource="{Binding}" HorizontalAlignment="Center">

F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\HomePage.xaml
31:                    Text="{Binding Title}"
</workspace_result>

### Turn 139 (Function)
[tool_response]
exit: 0
aaa53e3 test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]
2d92adb chore(repo): make main the mainline; retire master from CI + scoop publishing
edcea99 merge: integrate github pipeline scoop-bucket commits into master
0e06461 docs(requirements): regenerate wiki exports for TR/TEST-QA-TESTSILENCE-001
9793837 ci(release): treat chocolatey moderation 403 / duplicate 409 as non-fatal
7f57f92 test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]
ed0b67f feat(xbox): menu slides in from the right; single-navigator dpad throttle [PLAN-XBOXUWP FEAT-XMENUSLIDE-001 FIX-XDPADSKIP-002]
6050107 feat(xbox): authentic two-line RUN/STOP and SHIFT LOCK keycaps; chords show RUN [PLAN-XBOXUWP FEAT-XKBDSTICKY-001]
a00f9b0 fix(host): v2 machine snapshot: true chip state via lockstep injectors [PLAN-XBOXUWP FIX-XSNAPWARP-001]
95153d0 feat(xbox): sticky C=/SHIFT modifiers, real-time-scanned virtual keyboard [PLAN-XBOXUWP FEAT-XKBDSTICKY-001]
2d92adbc4c078b48b0f15f1426aef226b52e23e0
aaa53e3 test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]
--- romm worktree ---
## feat/romm-integration
 M src/ViceSharp.Abstractions/IMachineJoystickInput.cs
 M src/ViceSharp.Avalonia/Views/LibraryView.cs
 M src/ViceSharp.Core/C64MemoryMap.cs
 M src/ViceSharp.Host.InProcess/Runtime/ConsoleEmulatorHost.cs
 M src/ViceSharp.Host.InProcess/Services/InputServiceHost.cs
 M src/ViceSharp.Xbox/App.xaml.cs
 M src/ViceSharp.Xbox/Controls/VirtualKeyboardOverlay.xaml.cs
 M src/ViceSharp.Xbox/Platform/WinRtGamepadSource.cs
 M src/ViceSharp.Xbox/Styles/C64Keycaps.xaml
 M src/ViceSharp.Xbox/Views/LibraryPage.xaml
 M src/ViceSharp.Xbox/Views/LibraryPage.xaml.cs
 M tests/ViceSharp.TestHarness/HostInputServiceTests.cs
 M tests/ViceSharp.TestHarness/InputServiceHostTests.cs
 M tests/ViceSharp.TestHarness/Xbox/Fakes/FakeMachineJoystickInput.cs
?? src/ViceSharp.Avalonia/Views/AvaloniaCoverImageLoader.cs
?? src/ViceSharp.Xbox.ViewModels/KeyboardSpatialNavigator.cs
?? src/ViceSharp.Xbox/RomM/XboxCoverImageLoader.cs
?? tests/ViceSharp.TestHarness/Xbox/KeyboardSpatialNavigatorTests.cs
978b5a6 fix(xbox): OnLaunched composed the shell twice, leaking an unarmed gamepad pump
dcd0565 feat(xbox): virtual keyboard font-fits caps, centers SPACE, insets rows 3-4
c539f48 feat(xbox): detach the gamepad from the C64 joysticks while the virtual keyboard is open
314f37e fix(xbox): reconcile virtual keyboard legends with the real C64 (no clipped caps)
7808bd3 feat(xbox): shell menu keycaps skin per emulated model, matching the virtual keyboard
--- octopus diff summary ---
 azure-pipelines.ci.yml      | 26 ++++++++++++++++++++++++++
 azure-pipelines.release.yml | 26 ++++++++++++++++++++++++++
 2 files changed, 52 insertions(+)
diff --git a/azure-pipelines.ci.yml b/azure-pipelines.ci.yml
index c6721d1..b71ba8d 100644
--- a/azure-pipelines.ci.yml
+++ b/azure-pipelines.ci.yml
@@ -40,3 +40,29 @@ stages:
             displayName: 'Run: CiTest'
             inputs:
               script: './build.cmd CiTest --skip'
+

+      - pwsh: |
+          if (-not $env:OCTOPUS_API_KEY) {
+            Write-Host 'OCTOPUS_API_KEY not set; skipping Octopus LEGION2 release'
+            exit 0
+          }
+          $url = if ($env:OCTOPUS_URL) { $env:OCTOPUS_URL.TrimEnd('/') } else { 'http://PAYTON-LEGION2:8066' }
+          $env:OCTOPUS_URL = $url
+          $env:OCTOPUS_SPACE = if ($env:OCTOPUS_SPACE) { $env:OCTOPUS_SPACE } else { 'Default' }
+          $project = 'vice-sharp'
+          $version = ('$(Build.BuildNumber)' -replace '[^0-9A-Za-z\.\-_+]', '-')
+          if ([string]::IsNullOrWhiteSpace($version)) { $version = '0.0.$(Build.BuildId)' }
+          $octo = 'C:\Program Files\Octopus CLI\octopus.exe'
+          if (-not (Test-Path $octo)) { throw "octopus CLI missing: $octo" }
+          Write-Host "Octopus $url project=$project version=$version"
+          & $octo release create --project $project --version $version --packageVersion $version --releaseNotes "ADO $(Build.DefinitionName) $(Build.SourceVersion)" --ignoreExisting --no-prompt
+          if ($LASTEXITCODE -ne 0) { throw "octopus release create failed exit $LASTEXITCODE" }
+          & $octo release deploy --project $project --version $version --deployTo Development --progress --no-prompt
+          if ($LASTEXITCODE -ne 0) { Write-Warning "octopus deploy Development exit $LASTEXITCODE (release still created)" }
+        displayName: Octopus LEGION2 release
+        condition: and(succeeded(), ne(variables['SkipOctopus'], 'true'))
+        env:
+          OCTOPUS_API_KEY: $(OCTOPUS_API_KEY)
+          OCTOPUS_URL: $(OCTOPUS_URL)
+          OCTOPUS_SPACE: $(OCTOPUS_SPACE)

+


### Turn 140 (Assistant)
Finishing x:Bind, then merging romless and completing the Octopus pipeline.
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\HomePage.xaml.cs
[tool_request: grep]
- pattern: Contains\(\"\{Binding|BindingForbidden|reflection.*Binding|x:Bind
- path: F:\GitHub\vice-sharp\tests
- glob: *.cs
- head_limit: 20
[tool_request: list_dir]
- target_directory: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxTestConventionTests.cs
- limit: 80
[tool_request: run_terminal_command]
- command: # what does romless commit change?
git -C "F:\GitHub\vice-sharp" show --stat --oneline aaa53e3
# octopus release yml same shape?
git -C "F:\GitHub\vice-sharp" diff -- azure-pipelines.release.yml | Select-Object -First 40

- description: Inspect romless commit and release pipeline diff

### Turn 141 (Function)
[tool_response]
- F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox/
  - AboutViewModelTests.cs
  - ConsoleEmulatorHostTests.cs
  - Fakes/
    - FakeEmulatorSessionFacade.cs
    - FakeMachineJoystickInput.cs
    - FakeMachineKeyboardInput.cs
    - FakeRomAcquirer.cs
    - FakeStoragePicker.cs
    - FakeVideoFramePull.cs
    - FakeXboxSettingsGateway.cs
    - RecordingSyncContext.cs
  - FocusMapBuilderTests.cs
  - GamepadConverterTests.cs
  - GamepadDataShapeTests.cs
  - GamepadMergeSocdTests.cs
  - GamepadProfileTests.cs
  - HomeViewModelTests.cs
  - HostingSeamTests.cs
  - InputContextObserverTests.cs
  - InputMappingViewModelTests.cs
  - NavigationViewModelTests.cs
  - PortableAudioTypesLocationTests.cs
  - RomFetchRomAcquirerTests.cs
  - RomProvisionEvaluatorTests.cs
  - RomProvisionTestData.cs
  - SettingsOptionCatalogTests.cs
  - VideoFramePullViewModelTests.cs
  - VideoSurfacePureSinkBoundaryTests.cs
  - VirtualKeyboardViewModelTests.cs
  - XAudio2AudioMathTests.cs
  - XAudio2SourceVoiceBackendTests.cs
  - XboxAotLinkTests.cs
  - XboxAppCommandDispatcherTests.cs
  - XboxAudioBackendFactoryTests.cs
  - XboxAudioWiringTests.cs
  - XboxAuthenticKeyboardTests.cs
  - XboxBindingPersistenceTests.cs
  - XboxC64FontTests.cs
  - XboxCiConfigTests.cs
  - XboxComputerModelPickerTests.cs
  - XboxDataPathBridgeTests.cs
  - XboxDefaultCartridgeTests.cs
  - XboxDeviceSetupViewModelTests.cs
  - XboxGplComplianceTests.cs
  - XboxInputContextMachineTests.cs
  - XboxKeyboardInputTests.cs
  - XboxKeyboardStickyModifierTests.cs
  - XboxKeycapPetsciiGlyphTests.cs
  - XboxKeycapShiftTests.cs
  - XboxLocalDeployTargetTests.cs
  - XboxMachineSnapshotTests.cs
  - XboxManagedFrameGateTests.cs
  - XboxMenuFocusAndKeycapStyleTests.cs
  - XboxMenuPauseTests.cs
  - XboxMenuSubpageTests.cs
  - XboxNukeConfigTests.cs
  - XboxPerfStatsTests.cs
  - XboxRestoreSeamTests.cs
  - XboxRomProvisioningDispatchTests.cs
  - XboxRomProvisioningViewModelTests.cs
  - XboxSettingsDispatchTests.cs
  - XboxSettingsPersistenceTests.cs
  - XboxSettingsViewModelTests.cs
  - XboxStorePipelineTests.cs
  - XboxSystemButtonsBindingTests.cs
  - XboxTestConventionTests.cs
  - XboxUiStateStoreTests.cs
  - XboxVideoAspectTests.cs
  - XboxVideoCadenceTests.cs
  - XboxVideoSurfaceInteropTests.cs
  - XboxViewModelsBoundaryTests.cs

### Turn 142 (Function)
[tool_response]
1→// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): Home page code-behind. #if HAS_UWP-guarded in full.
#if HAS_UWP
namespace ViceSharp.Xbox.Views;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using ViceSharp.Xbox.ViewModels;
10→
/// <summary>The launch page. Its buttons raise the HomeViewModel intents and push pages.</summary>
public sealed partial class HomePage : Page
{
    /// <summary>Creates the page and binds the shared HomeViewModel.</summary>
    public HomePage()
    {
        InitializeComponent();
        DataContext = App.Instance.Home;

20→        // FEAT-XMENUFOCUS-001: programmatic focus can no-op before the first layout
        // pass, so re-assert once the tree is live.
        Loaded += (_, _) => FocusCloseMenu();
    }

    private HomeViewModel ViewModel => App.Instance.Home;

    /// <summary>
    /// FEAT-XMENUFOCUS-001 (operator 2026-07-14: "When opening the menu, always set
    /// focus to the 'Close Menu' button"): programmatic focus on the TOP button so a
30→    /// single press of A dismisses. Called on navigation here and by App.ShowMenu for
    /// re-shows that only flip the Frame's visibility.
    /// </summary>
    internal void FocusCloseMenu() => CloseMenuButton.Focus(FocusState.Programmatic);

    /// <inheritdoc />
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        FocusCloseMenu();
40→    }

    // Menu redesign (operator 2026-07-14): RESTART (cold reset, the last button) reuses
    // the StartNew intent; RESUME is gone (dismissing the menu resumes the paused machine).
    private void OnRestart(object sender, RoutedEventArgs e) => ViewModel.StartNew();

    // FEAT-XMENUSNAP-001: persist / restore a snapshot of the machine the menu is
    // holding paused. Fire-and-forget: the App methods log, guard, and dismiss on success.
    private void OnSave(object sender, RoutedEventArgs e) => _ = App.Instance.SaveSnapshotAsync();

50→    private void OnLoad(object sender, RoutedEventArgs e) => _ = App.Instance.LoadSnapshotAsync();

    // Mouse-friendly dismiss (operator 2026-07-14): leave the menu WITHOUT resetting or
    // resuming the emulator: pure UI (App.DismissMenu -> HideMenu), no host call at all.
    // FEAT-XMENUSLIDE-001 removed the click-outside backdrop: the menu is a docked
    // panel now, so clicks left of it land on the emulator view.
    private void OnCloseMenu(object sender, RoutedEventArgs e) => App.Instance.DismissMenu();

    private void OnSettings(object sender, RoutedEventArgs e) => Push(NavigationDestination.Settings, typeof(SettingsPage));

60→    private void OnDevices(object sender, RoutedEventArgs e) => Push(NavigationDestination.DeviceSetup, typeof(DeviceSetupPage));

    private void OnControls(object sender, RoutedEventArgs e) => Push(NavigationDestination.InputMapping, typeof(InputMappingPage));

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowAbout();
        Push(NavigationDestination.About, typeof(AboutPage));
    }

70→    private void Push(NavigationDestination destination, System.Type page)
    {
        App.Instance.Navigation.Push(destination);
        Frame?.Navigate(page);
    }
}
#endif



[... TRUNCATED at 524288 bytes, 450 turns omitted ...]
