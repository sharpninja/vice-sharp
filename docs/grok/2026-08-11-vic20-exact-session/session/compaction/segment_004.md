# HISTORICAL -- DO NOT EDIT
# Record of compaction segment 004 (detail=verbose) from this same task.
# Use read_file or grep to look up details, but do not modify.

## Segment metadata
- Index: 004
- Turn count: 529
- Timestamp: 2026-08-05T22:04:54Z

## Turn statistics

- Turns: 529 (Assistant=152, Function=366, Human=10, System=1)
- Tools used: read_file (112), run_terminal_command (84), grep (77), search_replace (47), get_command_or_subagent_output (27), todo_write (11), write (7), list_dir (1)
- Unique target files (80): C:\Users\kingd\.claude\profile\PROFILE.md, C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md, C:\Users\kingd\.claude\profile\approve-before-execute.md, C:\Users\kingd\.claude\profile\bring-the-receipts.md, C:\Users\kingd\.claude\profile\lab-authorization.md, ... and 75 more
- Tool errors: 2
- Verbose-render size estimate: 1,184,750 B
- Last assistant response excerpt: "Reverting the mid-cycle change and applying only the taken-branch +1 cycle, without double lag compensation."


## Summary (curated by compaction step)

Summary:
1. Primary Request and Intent: Finish Phase 2 VIC-20 with native xvic lockstep as the hard gate; exact VICE behavior is the bar. User rejected incomplete lockstep (CPU-only); dual-VIA and VIC-I samples required. Later: certify Phase 2 lockstep; run to 10s machine time; then “fix all lockstep issues.” Goal is bit-exact managed vs xvic, not playable-only.

2. Key Technical Concepts: ViceSharp .NET 10; separate vice_xvic.dll; PeekBus via via1/via2/vic_peek; open bus = last-data (BASIC/KERNAL do not refresh); Mos6561 power-on zeros and $9003/$9004 VICE raster packing; taken BRANCH = 3 cycles (4 page-cross) per 6510dtvcore; lag-compensation after-branch defers assumed 2-cycle taken branches; cycle 5005 mid-instr PC lag at BNE $FDB1; PAL 1108405 Hz, NTSC 1022727 Hz.

3. Files and Code Sections: native/vice-shim-vic20.c, build-vice-shim-xvic.sh, vice-shim.h (peek_bus); ViceNative.Xvic.cs, IViceNative.PeekBus; Mos6561 reset/raster; BasicBus open-bus latch; Vic20NativeLockstepTests; receipts under docs/ and SCRATCH. Latest Mos6502.cs edits: taken branch _cycle++; retired after-branch lag defers; staged branch only at _cycle==0 for taken path (not-taken still return false → ExecuteOpcode).

4. Errors and Fixes: Open bus 0xFF vs last-data fixed. VIC pre-seed READY defaults vs VICE zeros + wrong $9004 packing fixed. Skeptic VIA/VIC gaps fixed. 10s lockstep FAILED first every-cycle diverge at 5005. Attempted 3-cycle taken branch + retire after-branch lag → diverged earlier at c=27 (managed still at BNE $FD47, native DEX $FD49). Branch micro-test unreliable (bootstrap/reset interference).

5. Problem Solving: Bounded cert pass at 5k end-of-window. Every-cycle fail at 5005 is PC mid-instruction on taken BNE. Making taken branches 3 cycles and removing lag defers overshot; need restore green baseline then fix PC export without desync. Hypothesis: after-branch machinery vs 3-cycle taken is mis-balanced; may need mid-branch visiblePC only while keeping 2+absorb totals, or full cycle model rebuild.

6. All User Messages: (prior session: finish Phase 2 lockstep; goal not complete.) Always remember exact VICE. Certify phase 2 passes lockstep. Run lockstep to 10 seconds. yes, fix all lockstep issues.

7. Pending Tasks: Fix all lockstep issues (cycle 5005 and beyond toward multi-second/10s). Keep Vic20 suite green. Do not leave permanent failing 10s probes in Vic20 filter.

8. Current Work: Fixing Mos6502 taken-branch/PC for xvic exactness. Live fail after taken+1 and no after-branch defers: DIV c=27 nPC=$FD49 mPC=$FD47. BranchCycleCountTests traces polluted by reset/bootstrap. Vic20 lockstep suite failing (~7 fails). Probe Vic20DivergeProbe.cs may still be on disk.

9. Optional Next Step: Restore last-known-good Mos6502 branch/defer behavior (revert taken +1 and re-enable after-branch lag defers so 5k end-of-window green returns), then re-diagnose every-cycle 5005 PC lag with a clean single-instruction BNE vs native tick dump — quote: “yes, fix all lockstep issues” / diverge at c=27 after 3-cycle taken attempt.

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
## [32mfeat/iteration2-vic20[m
 [31mM[m HANDOFF.md
 [31mM[m README.md
 [31mM[m docs/Iteration-Roadmap.md
 [31mM[m docs/requirements/functional/FR-Machine-Profiles.md
 [31mM[m src/ViceSharp.Abstractions/IDeviceRegistry.cs
 [31mM[m src/ViceSharp.Avalonia/MainWindow.axaml.cs
 [31mM[m src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs
 [31mM[m src/ViceSharp.Core/ArchitectureBuilder.cs
 [31mM[m src/ViceSharp.Host.InProcess/Runtime/DefaultEmulatorRuntimeFactory.cs
 [31mM[m src/ViceSharp.Host.InProcess/Services/SettingsServiceHost.cs
 [31mM[m src/ViceSharp.Host.InProcess/Startup/ILocalDataFolder.cs
 [31mM[m src/ViceSharp.Host.InProcess/Startup/LocalDataFolder.cs
 [31mM[m src/ViceSharp.Host.InProcess/Startup/XboxDataPathBridge.cs
 [31mM[m src/ViceSharp.Launcher/ViceTopologyBuilder.cs
 [31mM[m src/ViceSharp.RomFetch/RomProvider.cs
 [31mM[m src/ViceSharp.Xbox.ViewModels/XboxSettingsViewModel.cs
 [31mM[m tests/ViceSharp.TestHarness/Launcher/ViceTopologyBuilderTests.cs
 [31mM[m tests/ViceSharp.TestHarness/MachineTestFactory.cs
 [31mM[m tests/ViceSharp.TestHarness/Xbox/XboxComputerModelPickerTests.cs
[31m??[m docs/S-Blox/
[31m??[m docs/announcements/
[31m??[m docs/requirements/functional/FR-VIC20.md
[31m??[m docs/reviews/code-review-20260714T001056552Z.md
[31m??[m docs/reviews/code-review-20260714T004256563Z.md
[31m??[m docs/romless-vic-badline-fix.md
[31m??[m manifests/
[31m??[m src/ViceSharp.Architectures/Vic20/
[31m??[m src/ViceSharp.Chips/Vic/
[31m??[m src/ViceSharp.Core/Vic20/
[31m??[m src/ViceSharp.Host.InProcess/Startup/Vic20RomBootstrap.cs
[31m??[m tests/ViceSharp.TestHarness/Vic20/
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
<user_query>
The goal is not complete
</user_query>

### Turn 4 (Human)
This session is being continued from a previous conversation that ran out of context. The summary below covers the earlier portion of the conversation.

Summary:
1. Primary Request and Intent:
- Phase 2 = overall VICE# Iteration 2 (VIC-20), not RomM. Plan approved: playable VIC-20, BDP TDD, chip/machine separation, default drive 8 = **1540**, subagents later.
- Later: deploy Avalonia MSI + Xbox UWP; fix missing Vic20 in Avalonia/UWP settings; UWP apply not switching machine; screenshots show READY then broken video then good UWP READY (3583 bytes free). User: long way for borders/colors/RAM; plan refresh + xvic lockstep; “goal not complete.”
- Active goal: **FINISH Phase 2, especially native lockstep** per goal plan at `.../goal/plan.md`; BDP; scratch only under `C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer`; dual-machine managed determinism alone does **not** satisfy native xvic criterion when oracle can exist.

2. Key Technical Concepts:
- .NET 10, ViceSharp; Mos6502; shared Via6522; Mos6561; Vic20SystemRam open-bus; profiles/settings restart; VICESHARP_ROM_PATH; default C1540 for Vic20.
- C64 lockstep: `vice_x64.dll` + `vice-shim.c` + `VICE_SHIM_HOSTED` CLK_INC in `c64cpusc.c`; `ViceNative` / `LockstepValidator`.
- VIC-20 CPU: `vic20cpu.c` has `#define CLK_INC() maincpu_clk++; vic_cycle()` then `#include "../mainviccpu.c"`; needs hosted CLK_INC like C64; `vic20model_set` (PAL=0, NTSC=1); separate link as `vice_xvic.dll` / xvic objects (not c64model in same binary).

3. Files and Code Sections:
- Plans: session `plan.md` (I–O, N0–N4); goal `plan.md` (criteria: READY+borders, host/1540, **native lockstep**, Vic20 filter + C64 smoke).
- Shipped Vic20: Architectures/Vic20/*, Core/Vic20/*, Chips/Vic/Mos6561.cs (borders, $900F bits 0–2 border / 4–7 bg), ArchitectureBuilder, SettingsServiceHost, Vic20RomBootstrap, tests Vic20/*, Avalonia PAR for xvic.
- Native: `native/vice-shim.c` C64-only (`c64model_set`); build `native/build-vice-shim.sh` → `vice_x64.dll`; `xvic.exe` **built** (~10MB, 2026-08-05). Patch does not touch mainvic/vic20cpu. `native/vice/vice/src/vic20/vic20cpu.c` needs VICE_SHIM_HOSTED CLK_INC like c64cpusc.
- Deployed InstallMsi + DeployXboxLocal; LocalState/VIC20 ROMs seeded.

4. Errors and Fixes:
- C64 IsC64Machine caught Vic20 → IsVic20 before C64; Core↔Architectures → glue in Core; SimpleRam open-bus → Vic20SystemRam; settings C64-only → list/resolve Vic20 + Xbox BuildModels xvic; restart not registered → always register Vic20 with data root + bootstrap; Ambiguous Vic20MemoryLayout in tests; CreateEmulatorSessionResponse has no Architecture (use registry).

5. Problem Solving:
- READY proven; I MVP (borders/$900F/PAR); N0 scaffold documents C64-only shim. Goal incomplete: Tier A leftovers (J–M,O) + **native xvic lockstep**. In progress: build/link Vic20-capable oracle (`xvic.exe` done; need hosted `vic20cpu` + `vice-shim-vic20` / `vice_xvic.dll` + managed LockstepValidator path).

6. All User Messages:
- Clarified overall VICE# Phase 2; plan approvals; drive 8=1540; deploy both apps; Avalonia/UWP Vic20 variants; UWP doesn’t switch; images (broken Avalonia, good UWP READY); “long way” borders/colors/RAM; update plan + xvic lockstep; “goal is not complete”; finish Phase 2 especially lockstep (system goal).

7. Pending Tasks:
- Goal checklist: native Vic20 create (shim multi-machine); LockstepValidator Vic20; multi-cycle suite + CI VIC20 ROMs; Tier A gaps; verify 1–5 to SCRATCH; HANDOFF only after native lockstep green when oracle available.
- Native: hosted CLK_INC for vic20cpu/mainvic; vice-shim-vic20 + build-vice-shim-xvic; ViceNative load vice_xvic for vic20/vic20ntsc.

8. Current Work:
- On `feat/iteration2-vic20`, implementing Phase 2 with lockstep priority. Slice I tests green; full Vic20 filter ~66 pass earlier; todos: n0 in_progress. Built `xvic.exe` via MSYS2 `make xvic-program`. Inspected C64 hosted CLK_INC vs unpatched `vic20cpu.c` `CLK_INC`.

9. Optional Next Step:
Continue N0: “extend native so Vic20 model selectors create a real Vic20 machine”—host `vic20cpu.c` CLK_INC (VICE_SHIM_HOSTED), implement link `vice_xvic.dll` (shim-vic20 + xvic objects), then managed create/step/compare tests on real path.

Full verbatim rollouts of previous segments are available at C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction/segment_*.md.  See C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction/INDEX.md for a table of contents.  Use read_file or grep to recover specific details (exact code, file paths, tool outputs) if this summary is insufficient.  Do NOT modify these files.

### Turn 5 (Human)
<system-reminder>
## Available Skills
The following skills are available for use:

- add-profile: Load Payton's operator profile (identity, standing preferences, active-project context) into the current context
  Use when: the user says "add profile", "/add-profile", "load my profile", "remember who I am", or after context loss/compaction when the profile detail is gone.
  Absolute path: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
- session-log-management: This skill should be used when the user asks to "start session", "log session", "begin turn", "update turn", "complete turn", "query session history"
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-session\SKILL.md
- todo-management: This skill should be used when the user asks to "create a todo", "list todos", "update todo", "query tasks", "check todo status", "plan implementation", "mark todo done". Primary skill for the Grok coding agent CLI fork of the McpServer plugin.
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-todo\SKILL.md
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

## TODO List
This is your task list from before the conversation was compacted — it is still active. Keep working through the items below and update their status as you make progress:
- [in_progress] n0: Extend native oracle for Vic20 model create
- [pending] n1: LockstepValidator Vic20 path CPU+VIA+VIC regs
- [pending] n2: Multi-cycle Vic20 lockstep suite + CI ROMs
- [pending] tierA: Close remaining Tier A gaps for Vic20 filter
- [pending] verify: Run verification plan 1-5; capture SCRATCH logs
- [pending] docs: Update HANDOFF/roadmap after gates pass

## Connected MCP Servers
- github (90 tools)
- gmail (16 tools)
- google_calendar (8 tools)
- google_drive (5 tools)
- outlook (18 tools)
- pwsh (4 tools)
- tasks (6 tools)
- voice (1 tool)
To use MCP tools, you MUST call `search_tool` first to retrieve the tool's input schema before calling `use_tool`. NEVER guess parameter names — always use the exact schema returned by `search_tool`.
</system-reminder>

### Turn 6 (Assistant)
Understood — Phase 2 is still open, with native xvic lockstep as the hard criterion. Resuming from N0: hosted Vic20 oracle, then LockstepValidator path.
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
- limit: 120
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\HANDOFF.md
- limit: 150
[tool_request: run_terminal_command]
- command: Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-*" -ErrorAction SilentlyContinue | Select-Object FullName; if (Test-Path "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956") { Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956" -Recurse -Depth 2 | Select-Object FullName }
- description: Locate goal plan and scratch dirs

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
1→# ViceSharp Handoff - 2026-08-05

**Active branch:** `feat/iteration2-vic20` (off `main` @ v1.2.1 line). Iteration 2 = overall project VIC-20 (not RomM Phase E).
**Prior release baseline:** `main` at **v1.2.1** (NuGet + winget published). Iteration 1 C64 complete.
**Working tree noise (do not commit):** untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, `manifests/`.

## Iteration 2 VIC-20 IN PROGRESS (2026-08-05) - GOAL NOT COMPLETE

- **Branch:** `feat/iteration2-vic20` (off main / v1.2.1). Locked: **default drive 8 = 1540**.
10→- **Plan:** session `plan.md` status refresh - Tier A polish + Tier B **native xvic lockstep**.
- **Proven:** READY on Avalonia + UWP (`3583 BYTES FREE`); profile switch; char-mode `Mos6561` with **border strips + $900F decode** (Slice I MVP); open-bus RAM; VIA matrix; 1540 default; managed determinism.
- **Remaining:** J expansion UX, K input E2E, L cart/disk, M snapshots, **N0-N2 native xvic lockstep** (shim is **C64-only** today: `vice_machine_create_model` -> `c64model_set`; Vic20 selectors fail closed - see `Vic20LockstepScaffoldTests`), O docs/CI.
- **Native lockstep blocker:** multi-machine `vice-shim` (or separate xvic host) required before `LockstepValidator` Vic20 path; keep managed dual-machine gate until then.
- **Do not** claim Iteration 2 complete; do not commit untracked `docs/S-Blox/`, reviews, manifests.

## PLAN-XBOXUWP + PLAN-ROMM resume snapshot (2026-08-05)

- **Track 1 (Xbox Tier D residuals) largely SHIPPED this session:** FEAT-XAOTBIND-001 (x:Bind migration), FEAT-XOCTOPUS-001 (CI/release Octopus LEGION2 steps), FIX-ROMLESSVIC-001 (merged test), FIX-XKBDNMI-001 (RESTORE asserts NMI), FEAT-XCTRLBIND-001 (Controls remapping), FEAT-XROMPICK-001 (model ROM readiness).
- **Track 2 (RomM):** merged `feat/romm-integration` into this branch (`3804a1d`). Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM`, Xbox/Avalonia library UI, LAN bridge connection path present. Gate after merge: **Category=Xbox|RomM|Library 546/546** (0 fail, 0 skip).
20→- **Deploy loop:** `./build.ps1 DeployXboxLocal` (VS MSBuild Release-UWP). End CLI batches with Debug-UWP restore for operator VS F5. PublishAot stays opt-in (`ViceSharpPublishAot=true`); Release-UWP JIT.
- **OPEN:** B-on-controller operator verification; S42 Tier-C console/Store (deferred); MCP TODO reconcile (mark completed IMPL-XBOXUWP / IMPL-ROMM ids done with receipts); device smoke for RomM Library after merge; optional full baseline suite.
- **Parallel worktrees (reference):** `F:\GitHub\vice-sharp-romm` (`feat/romm-integration`, source of merge); romless repro merged via FIX-ROMLESSVIC-001.

## How to resume

1. Read `AGENTS-README-FIRST.yaml` (marker rotates on server restart; verify signature + /health nonce before MCP work).
2. Route TODO / session-log / requirements / triage through the mcpserver plugin (repl-invoke or wrappers), never raw REST, never storage files.
3. Baseline gate (one process, ~20 min, run detached):
   `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`
30→   Green criterion: 0 failed, 21 skipped, total 2612-2615 (totals wobble in that band from cosmetic xunit trx name serialization of dynamic theory rows; per-class row sets are identical - never chase a fixed total).

## Shipped 2026-07-08 (all CI-confirmed)

- **v1.0.2 released**: tag on `534cded`, release run 1074, all 13 package ids verified in the nuget.org flat container (Core bundle, SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox; Console + Avalonia are dotnet tools).
- **PLAN-NATIVERESIDUE-001 closed** (`ba0f94f`): a `.vsf` whose DRIVE8 module carries `has_tde=0` disabled TrueDrive process-wide via `resources_set_int` (drive-snapshot.c:334-363); `vice_machine_create_model` now re-baselines `Drive{8..11}TrueEmulation` to the VICE default 1 through `resources_set_int` so iecbus statics and $DD00 callbacks recompute. SnapshotResume two-process partition removed; suite proven green in ONE process. Guards: TEST-NATIVE-RESIDUE-01/02 in `NativeResidueDiagTests`.
- **CI made self-sufficient** (`a229801`, `d8f61cf`, `4e3db9b`): agents have no ROMs and no shim, so `EnsureCiRomRoot` in `build/Build.cs` stages 13 hash-pinned VICE data files (C64 ROMs incl. all variant kernals, DRIVES dos1541/dos1541ii, gtk3_pos.vkm) from the VICE-Team svn-mirror into `artifacts/vice-data` and sets `VICESHARP_ROM_PATH` for the test process only. Shim-dependent tests MUST use `[ViceFact]`/`[ViceTheory]` (plain `[Fact]` throws DllNotFoundException on shimless agents). First-ever green CI run: 1072.
- **Docs reconciled to shipped reality** (`aee29ea`, 88 files): README, USER-GUIDE, VICE-MIGRATION, ROMs, Architecture, Public-API (regenerated from the 50 real Abstractions interfaces), requirements docs; 22 stale files pruned; `docs/wiki.yaml` manifest lists all 28 wiki documents; winget license fixed MIT -> GPL-2.0-or-later.
- **github divergence resolved** (`20b330c`): github-only commit `42f2f26` (VicModeChangeEvent seam) merged with strategy `ours` (tree unchanged; content preserved on `rescue/vic-modechange-event`); github main fast-forwarded, tags identical on both remotes; github wiki published per manifest (`b455b88`, 28 pages + sidebar).
- **Dependency wave S0-S8 complete** (TR-DEPS-202607-001 + TEST-DEPS-202607-001, both completed with evidence; commits `72696d9`, `055b9d5`, `26e5ca8`, `87eb80a`, `9214cdb`, `1cad822`+`1adf386`, `23d030a`, `0f5192f`): all 30 central entries at newest mutually compatible stables (Avalonia 12.0.5, Extensions/TestHost 10.0.9, Protobuf 3.35.1, Grpc.Tools 2.82.0, Test.Sdk 18.7.0, Roslyn pair 5.6.0, YamlDotNet 18.1.0, FluentAssertions 8.10.0, coverlet 10.0.1, aiUnit 2.1.3, RemoteControl 0.7.4); 5 dead pins removed; AiReview under CPM with inherited TreatWarningsAsErrors; vendored `nuget-local` feed DELETED (NuGet.config is nuget.org-only); `global.json` floor 10.0.301 (Roslyn 5.6 generator gate). Excluded as prerelease: xunit.v3 4.x, NSubstitute 6.x, Protobuf 4.x, Extensions 11.x.
40→- **Test fixtures in repo** (`93cdc7e`): `vice-snapshot-20260630171307.vsf` (read by the residue probes and lockstep re-baseline suites at repo root; do not delete), `native/.build-nopatch.sh`, `native/.ccwrap/*`. Operator rule: if it is needed to run a test, it goes in the repo.

## Iteration-1 completion (branch `fix/nativeresidue-002-drive-clock-hardening`, off `master`; NOT pushed)

Working through the "complete outstanding iteration 1 work" plan. Done, all committed locally, gates green:
- **Slice A - PLAN-NATIVERESIDUE-002** (710c1e9/8894f1a/9fb06dd/0691344): the two latent drive-clock bugs fixed; **BUG-LOCKSTEP-001 CLOSED** (full gate 0 failed / 2596 passed / 21 skipped). See docs/receipts-nativeresidue-002-2026-07-09.md.
- **Slice F - BUG-TESTDEBT-001**: verified already fixed at HEAD (6a32e7c/77e7190), closed with receipt.
- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.

**SID parity program COMPLETE (2026-07-10)** - all remaining slices landed:
50→- **S11 / S11b**: 8580 reSID filter port (filter8580new m==1) + write pipeline + per-model ttl(0xA2000)/scaleFactor(5); shim exports vice_sid_exact_set_sampling + vice_sid_exact_clock_buffered; DATABUS-07 flipped; oracle via "c64c". Ratchet ->444.
- **S12**: amplify(scaleFactor)/clip PCM16 seam + extfilt enable branch; OUTPUT-01..07. Ratchet ->451.
- **S13**: fixed-point Kaiser FIR resampler (fast/interpolate/resample); OUTPUT-08..13. Ratchet ->457.
- **CH**: retired the dead Chamberlin SVF stack (10 members + 7 guards + SidFilter6581Tests.cs). PLAN-SIDCHAMBERLIN-001 closed.
- **BC**: ported reSID batched SID::clock(delta_t) (Sid6581.BatchedClock.cs); SAMPLE_FAST now value-bit-exact; un-pended the last 4 SID quarantines + authored the last 9 (EXTFILT-01..07, CLOCK-05/08). Ratchet ->466 with a strict completion pin (Assert.Equal(466, covered)).
- **LW** (4924b94, pushed): live audio wired through the reSID SAMPLE_RESAMPLE engine (Resample always, VICE x64sc parity); push tail bit-exact vs the buffered pull; warp = cadence-only; zero-alloc; benchmark in SidSamplingBenchmarks.
- **CL** (a38964b/e100433, local): NativeCollectionConventionTests + [Collection("NativeVice")] on the 6 native-bridge test classes.
- **G**: DROPPED per operator (ADO wiki push).

Closure gates: parity 466/0/0 (completion pin holds), determinism 5/0, XmlDocs green, full baseline 0 NEW failures (7 pre-existing VideoRendererTests only) on the clean re-run. One run-1 full-suite native ACCESS_VIOLATION did NOT reproduce (flaky native-shim instability; candidate PLAN-NATIVERESIDUE-002 follow-up, not a SID blocker) - see the closure receipts.
60→
**Native build recipe (learned this session, load-bearing):** `make x64sc-program` does NOT rebuild changed VICE-core `.o` (they stay stale). Before rebuilding, delete the changed `.o` (and `libdrive.a` for drive files), then run under the MSYS2 MINGW64 login shell so the compiler gets a writable `/tmp`:
`MSYSTEM=MINGW64 /c/msys64/usr/bin/bash.exe -lc 'rm -f <changed>.o; bash /f/GitHub/vice-sharp/native/.build-nopatch.sh'`. The dll is gitignored (built locally). Vendored edits go in native/patches/vice-shim-runtime.patch (regenerate via `git -C native/vice diff`; keep the 5 pre-existing hunks byte-identical).

**Oracle note:** SidExactRead (read path) is the reliable bus observable; SidExactGetState().BusValue export is NOT a dependable live-latch snapshot (returned stale/garbage) - compare via the read path + managed spec constants.

## In flight (interrupted mid-task)

Plugin reload + Agent Help (mcpserver core synced to 1.36.0, `mcpserver-repl` 1.4.5 confirmed on PATH):
1. Validate cache: `%USERPROFILE%\.claude\plugins\cache` has `mcpserver-local` (active family; confirm 1.36.0 from the plugin's own `.claude-plugin/plugin.json`), `mcpserver-cowork` (stale candidate - verify before deleting), `caveman` (unrelated, keep). The version inspection was interrupted by a transient permission-classifier outage; re-run it.
70→2. Run the claude-hook-validation skill; refresh MCP hooks if missing; restart Claude Code if hooks were installed.
3. Open a `workflow.agenthelp.createSession` (new in repl 1.4.5) and submit the outstanding MCP issues:
   - repl-invoke `Invoke-WorkflowAppendActions` audit counters never increment: regex `(?m)^\s*type:` misses `- type:` list items (repl-invoke.ps1:891-897; filed as triage-report-8a6539340a174c00a57dee53ec8f42ea).
   - No per-action readback: `workflow.sessionlog.queryHistory` returns only session headers, so appends cannot be content-verified; session header `lastUpdated`/`filesModifiedCount` never advance (anchored to `started`).
   - `Invoke-McpPlugin.ps1` defaults `-Command Status`: calling it with only `-Method` silently prints the status blob instead of invoking (footgun; cost one silent append failure).
   - `workflow.requirements.createTr` requires `subarea` (schema) but the skill docs do not mention it; the server also derived `subarea: 202607` from the id rather than honoring the supplied value.
4. Log the reload + help-session outcomes; triage any confirmed server defects.

## Parked items

80→- Azure DevOps wiki push needs `ADO_PAT` set, then `tools/Publish-Wiki.ps1 -Target azure` (github wiki already published).
- ~~Latent VICE bugs documented, not fixed~~ FIXED 2026-07-09 (PLAN-NATIVERESIDUE-002, branch `fix/nativeresidue-002-drive-clock-hardening`): drive `attach_clk`/`detach_clk`/`attach_detach_clk` uninitialized-stack read on `has_tde=0` restore (drive-snapshot.c zero-init) and `cycle_accum` omitted from `drivecpu_reset_clk` (drivecpu.c + drivecpu65c02.c) both fixed via the vendored runtime patch, plus a shim create-time drive-clock re-baseline. **BUG-LOCKSTEP-001 CLOSED**: full baseline gate 0 failed / 2596 passed / 21 skipped / 2617 total (was 136+2 lockstep failures). Receipts: docs/receipts-nativeresidue-002-2026-07-09.md. New residue candidates recorded there (live unit->type not re-baselined; drivecpu65c02 cycle_accum SMW/SMR width asymmetry). Build note: `make x64sc-program` does NOT rebuild changed VICE-core `.o`; delete the stale `.o`+`libdrive.a` and build under `MSYSTEM=MINGW64 bash -lc`.
- ~~VICE parity program (PLAN-VICEPARITY-001) remaining slices~~ **COMPLETE 2026-07-10** (SID side fully bit-exact vs reSID; VIC per-cycle work landed through V7 + audit phases). Only manifest-wide pending AC left is TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock conflict, out of SID scope, needs the parity owner). Candidate follow-up: PLAN-NATIVERESIDUE-002 native-shim lifecycle hardening (a sustained-consecutive-native-load ACCESS_VIOLATION observed once, non-reproducing).

## Open TODO backlog (MCP store)

PLAN-UIDOCK-001 (Dock.Avalonia UI), PLAN-AUDIOEQ-001, PLAN-PLAYLIST-001, PLAN-FULLSCREEN-001, PLAN-DEVCARDART-001, PLAN-MONFRAME-001, PLAN-DRIVE1581-001, PLAN-DRIVE1541II-001, PLAN-DRIVE1540-001, PLAN-DRIVE1571-001, PLAN-DRIVECMDHD-001, PLAN-CARTRAMLINK-001, PLAN-ARCHVIC20-001 (query the store for the live list; this snapshot is 2026-07-09).

## Operational gotchas (hard-won this session)

90→- Long test runs: OS-detach with `Start-Process dotnet ... -RedirectStandardOutput` and tail the log with a Monitor; never run two test/build invocations concurrently (native lock + obj/bin contention); verify the log grows before trusting a launch.
- `git add a b c bad-path` aborts the WHOLE add on one bad pathspec and a following commit ships whatever was staged earlier; always check the commit stat against intent.
- Diagnostic probes: `LiveLimiterBandProbeTests` reports via a by-design `Assert.Fail`; `Demo_SilentWarp` headroom is host-load sensitive (re-run in isolation before believing a failure). Live-app probes attach via `%LOCALAPPDATA%\ViceSharp\debug-attach.json`; NEVER dispose the probe client (it kills the session).
- github force-push is classifier-blocked regardless of instruction phrasing; the sanctioned divergence pattern is merge `-s ours` + fast-forward.


### Turn 9 (Function)
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

### Turn 10 (Function)
[tool_response]
exit: 0

FullName
--------
C:\Users\kingd\AppData\Local\Temp\grok-goal-1c2bb774ee86
C:\Users\kingd\AppData\Local\Temp\grok-goal-517f749f32af
C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956
C:\Users\kingd\AppData\Local\Temp\grok-goal-80009905444d
C:\Users\kingd\AppData\Local\Temp\grok-goal-8042cca8a45e
C:\Users\kingd\AppData\Local\Temp\grok-goal-805fde6d583a
C:\Users\kingd\AppData\Local\Temp\grok-goal-8dcf4780924b
C:\Users\kingd\AppData\Local\Temp\grok-goal-e246966b3b6c
C:\Users\kingd\AppData\Local\Temp\grok-goal-resume-verify
C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer
C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\xvic-build.log



### Turn 11 (Assistant)
[tool_request: run_terminal_command]
- command: Get-ChildItem "$env:USERPROFILE\.claude\profile\*.md" | Select-Object Name; Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956" -Force | Select-Object Name, Mode; Get-Content "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\plan.md" -ErrorAction SilentlyContinue; if (-not $?) { Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956" -Recurse -Filter "plan.md" | ForEach-Object { $_.FullName } }
- description: List profile files and load goal plan
[tool_request: grep]
- pattern: CLK_INC|VICE_SHIM_HOSTED
- path: F:\GitHub\vice-sharp\native
- glob: **/*.{c,h}
[tool_request: list_dir]
- target_directory: F:\GitHub\vice-sharp\native

### Turn 12 (Function)
[tool_response]
- F:\GitHub\vice-sharp\native/
  - build-vice-shim.ps1
  - build-vice-shim.sh
  - CMakeLists.txt
  - libgcc_s_seh-1.dll
  - libiconv-2.dll
  - libstdc++-6.dll
  - libwinpthread-1.dll
  - mainc64cpu-hosted.o
  - patches/
    - s9-filter-probe.patch
    - vice-shim-runtime.patch
    - vice-shim-runtime.prompt.md
  - vice-moncommands.txt
  - vice-shim-hosted.o
  - vice-shim.c
  - vice-shim.h
  - Vice.Native.proj
  - vice/
    - README.md
    - svn-instructions.txt
    - vice/
      - aclocal.m4
      - autogen.sh
      - autom4te.cache/
        - output.0
        - output.1
        - output.2
        - output.3
        - requests
        - traces.0
        - traces.1
        - traces.2
        - traces.3
      - build/
        - beos/
          - intel-build-sdl.sh
          - Makefile
          - Makefile.am
          - Makefile.in
          - ppc-build-sdl.sh
        - debian/
          - control.gtk3
          - control.headless
          - control.sdl1
          - control.sdl2
          - copyright.footer
          - copyright.header
        - git-conversion/
          - make_vice_git.py
          - tag_monster.py
        - github-actions/
          - build-deb.sh
          - build-macos.sh
          - build-msys2.sh
          - build-shared.sh
          - check-lastline.sh
          - check-spaces.sh
          - check-tabs.sh
          - compare-dists.sh
          - contributors.awk
          - irc-message.sh
        - macOS/
          - bindist.sh
          - Makefile
          - Makefile.am
          - Makefile.in
        - Makefile
        - Makefile.am
        - Makefile.in
        - mingw/
          - docker/
            [4 files in subtree: 2 *.sh, 1 *.md, 1 *no-ext]
      - check-activity.sh
      - cmake-bootstrap.sh
      - compile
      - config.guess
      - config.log
      - config.rpath
      - config.status
      - config.sub
      - configure
      - configure.ac
      - COPYING
      - data/
        - C128/
          - basic64-901226-01.bin
          - basichi-318019-04.bin
          - basiclo-318018-04.bin
          - c64hq.vpl
          - c64s.vpl
          - ccs64.vpl
          - chargen-315079-01.bin
          - chargen-325078-02.bin
          - chargen-325167-02.bin
          - chargen-325173-01D.bin
          - chargen-325181-01.bin
          - chargen-390059-01.bin
          - cjam.vpl
          - colodore.vpl
          - community-colors.vpl
          - deekay.vpl
          - default.vrs
          - frodo.vpl
          - godot.vpl
          - gtk3_keyrahv3.vkm
          - gtk3_keyrahv3_de.vkm
          - gtk3_pos.vkm
          - gtk3_pos_de.vkm
          - gtk3_pos_se.vkm
          - gtk3_sym.vkm
          - gtk3_sym_de.vkm
          - gtk3_sym_fr.vkm
          - gtk3_sym_se.vkm
          - kernal-315078-03.bin
          - kernal-318020-05.bin
          - kernal-318034-01.bin
          - kernal-325172-01.bin
          - kernal64-325179-01.bin
          - kernal64-325182-01.bin
          - kernal64-901227-03.bin
          - kernalfi
          - kernalfr
          - kernalit
          - kernalno
          - lemon64.vpl
          - Makefile
          - Makefile.am
          - Makefile.in
          - palette.vpl
          - palette_6569R1_v1r.vpl
          - palette_6569R5_v1r.vpl
          - palette_8565R2_v1r.vpl
          - palette_C64_amber.vpl
          - palette_C64_cyan.vpl
          - palette_C64_green.vpl
          - pc64.vpl
          - pepto-ntsc-sony.vpl
          - pepto-ntsc.vpl
          - pepto-pal.vpl
          - pepto-palold.vpl
          - pixcen.vpl
          - ptoing.vpl
          - rgb.vpl
          - sdl_pos.vkm
          - sdl_pos_de.vkm
          - sdl_sym.vkm
          - sdl_sym_de.vkm
          - sdl_sym_fr.vkm
          - sdl_v128.vkm
          - the64.vpl
          - vdc_comp.vpl
          - vdc_deft.vpl
          - vdc_scart.vpl
          - vice.vpl
        - C64/
          - basic-901226-01.bin
          - c64hq.vpl
          - c64mem.sym
          - c64s.vpl
          - ccs64.vpl
          - chargen-901225-01.bin
          - chargen-906143-02.bin
          - cjam.vpl
          - colodore.vpl
          - community-colors.vpl
          - deekay.vpl
          - default.vrs
          - frodo.vpl
          - godot.vpl
          - gtk3_keyrah.vkm
          - gtk3_keyrah_de.vkm
          - gtk3_pos.vkm
          - gtk3_pos_bmc64.vkm
          - gtk3_pos_de.vkm
          - gtk3_pos_es.vkm
          - gtk3_pos_it.vkm
          - gtk3_pos_se.vkm
          - gtk3_pos_tr.vkm
          - gtk3_pos_uk.vkm
          - gtk3_sym.vkm
          - gtk3_sym_be.vkm
          - gtk3_sym_da.vkm
          - gtk3_sym_de.vkm
          - gtk3_sym_fr.vkm
          - gtk3_sym_it.vkm
          - gtk3_sym_nl.vkm
          - gtk3_sym_se.vkm
          - gtk3_sym_tr.vkm
          - gtk3_sym_uk.vkm
          - gtk3_the64_de.vkm
          - kernal-251104-04.bin
          - kernal-390852-01.bin
          - kernal-901227-01.bin
          - kernal-901227-02.bin
          - kernal-901227-03.bin
          - kernal-901246-01.bin
          - kernal-906145-02.bin
          - lemon64.vpl
          - Makefile
          - Makefile.am
          - Makefile.in
          - palette.vpl
          - palette_6569R1_v1r.vpl
          - palette_6569R5_v1r.vpl
          - palette_8565R2_v1r.vpl
          - palette_C64_amber.vpl
          - palette_C64_cyan.vpl
          - palette_C64_green.vpl
          - pc64.vpl
          - pepto-ntsc-sony.vpl
          - pepto-ntsc.vpl
          - pepto-pal.vpl
          - pepto-palold.vpl
          - pixcen.vpl
          - ptoing.vpl
          - rgb.vpl
          - sdl_c64p.vkm
          - sdl_keyrah.vkm
          - sdl_keyrah_combian.vkm
          - sdl_keyrah_de.vkm
          - sdl_keyrah_retropie.vkm
          - sdl_keyrah_retropie_de.vkm
          - sdl_pos.vkm
          - sdl_pos_ch.vkm
          - sdl_pos_da.vkm
          - sdl_pos_de.vkm
          - sdl_pos_fi.vkm
          - sdl_pos_no.vkm
          - sdl_pos_tr.vkm
          - sdl_pos_uk.vkm
          - sdl_sym.vkm
          - sdl_sym_da.vkm
          - sdl_sym_de.vkm
          - sdl_sym_fr.vkm
          - sdl_sym_it.vkm
          - sdl_sym_nl.vkm
          - sdl_sym_no_pi_4-500_bmc64.vkm
          - sdl_sym_tr.vkm
          - sdl_sym_uk.vkm
          - sdl_sym_uk_pi_4-500_bmc64.vkm
          - sdl_sym_us_pi_4-500_bmc64.vkm
          - the64.vpl
          - vice.vpl
        - C64DTV/
          [57 files in subtree: 46 *.vkm, 4 *.bin, 1 *.am, ...]
        - CBM-II/
          [52 files in subtree: 29 *.vpl, 8 *.bin, 7 *.vkm, ...]
        - common/
          [170 files in subtree: 108 *.png, 20 *.svg, 11 *.desktop, ...]
        - DRIVES/
          - dos1001-901887+8-01.bin
          - dos1540-325302+3-01.bin
          - dos1541-325302-01+901229-05.bin
          - dos1541ii-251968-03.bin
          - dos1551-318008-01.bin
          - dos1570-315090-01.bin
          - dos1571-310654-05.bin
          - dos1571cr-318047-01.bin
          - dos1581-318045-02.bin
          - dos2031-901484-03+05.bin
          - dos2040-901468-06+07.bin
          - dos3040-901468-11-13.bin
          - dos4040-901468-14-16.bin
          - dos9000-300516+7-revC.bin
          - Makefile
          - Makefile.am
          - Makefile.in
        - GLSL/
          - bicubic-interlaced.frag
          - bicubic.frag
          - builtin-interlaced.frag
          - builtin.frag
          - Makefile
          - Makefile.am
          - Makefile.in
          - viewport.vert
        - hotkeys/
          - hotkeys-cartridge.vhk
          - hotkeys-datasette.vhk
          - hotkeys-debug.vhk
          - hotkeys-drive.vhk
          - hotkeys-fliplist.vhk
          - hotkeys-help.vhk
          - hotkeys-settings.vhk
          - hotkeys-snapshot.vhk
          - hotkeys-vsid.vhk
          - hotkeys-winvice-legacy.vhk
          - hotkeys.vhk
          - Makefile
          - Makefile.am
          - Makefile.in
        - joymaps/
          [25 files in subtree: 22 *.vjm, 1 *.am, 1 *.in, ...]
        - Makefile
        - Makefile.am
        - Makefile.in
        - PET/
          [68 files in subtree: 28 *.bin, 24 *.vkm, 8 *.vrs, ...]
        - PLUS4/
          [32 files in subtree: 14 *.vkm, 8 *.bin, 6 *.vpl, ...]
        - PRINTER/
          [15 files in subtree: 9 *.bin, 3 *.vpl, 1 *.am, ...]
        - SCPU64/
          [82 files in subtree: 46 *.vkm, 27 *.vpl, 2 *.bin, ...]
        - VIC20/
          [30 files in subtree: 15 *.vkm, 6 *.bin, 5 *.vpl, ...]
      - depcomp
      - doc/
        - building/
          [15 files in subtree: 11 *.txt, 1 *.am, 1 *.in, ...]
        - checkdoc.c
        - checkdoc.mak
        - CIA-README.txt
        - coding-guidelines.txt
        - Documentation-Howto.txt
        - Doxyfile
        - Doxygen-Howto.txt
        - fixdates.sh
        - gpl.texi
        - html/
          [111 files in subtree: 35 *.png, 33 *.json, 13 *.html, ...]
        - iec-bus.txt
        - indent-test.c
        - joystick.md
        - mainpage.dox
        - Makefile
        - Makefile.am
        - Makefile.in
        - mkdoxy.sh
        - readmes/
          - Makefile
          - Makefile.am
          - Makefile.in
          - Readme-SDL.txt
          - Readme-SDL2.txt
        - Release-Howto.txt
        - src2doxy.sh
        - texinfo.tex
        - txt2doxy.sh
        - uncrustify.cfg
        - vice.texi
        - vim/
          [14 files in subtree: 4 *.vim, 3 *.am, 3 *.in, ...]
      - gdkkeysyms.sh
      - gtk3-resources.py
      - install-sh
      - m4/
        - ax_cxx_compile_stdcxx.m4
        - lib-ld.m4
        - m4_ax_subdirs_configure.m4
        - pkg.m4
        - progtest.m4
        - vice_args.m4
        - vice_compiler_checks.m4
      - Makefile
      - Makefile.am
      - Makefile.in
      - missing
      - mkinstalldirs
      - NEWS
      - README
      - src/
        [3951 files in subtree: 1414 *.c, 1319 *.h, 746 *.o, ...]
      - uiactionlist.py
      - ylwrap
  - vice_x64.dll
  - vicii-cycle-hosted.o
  - zlib1.dll

### Turn 13 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 139 matching lines
F:\GitHub\vice-sharp\native\vice-shim.c
685:       ticks before the first real CLK_INC checkpoint. On real hardware
1342:     last_opcode_info on resume (mainc64cpu.c VICE_SHIM_HOSTED block), so the

F:\GitHub\vice-sharp\native\vice\vice\src\65816core.c
97:#ifndef CLK_INC
98:#define CLK_INC(clock) clock++
405:#define FETCH_PARAM(addr) ((((int)(addr)) < bank_limit) ? (CLK_INC(CLK), bank_base[addr]) : LOAD_PBR(addr))
2350:              CLK_INC(CLK);                                           \
2352:          CLK_INC(CLK);                                               \
2493:              CLK_INC(CLK);                                                               \
2495:          CLK_INC(CLK); /* yes, this is really this complicated with this extra cycle */  \

F:\GitHub\vice-sharp\native\vice\vice\src\6510dtvcore.c
320:        CLK_INC();                                                                                                    \
322:        CLK_INC();                                                                                                    \
324:        CLK_INC();                                                                                                    \
344:        CLK_INC();                                                                                                    \
346:        CLK_INC();                                                                                                    \
369:                    CLK_INC();                                                 \
371:                    CLK_INC();                                                 \
375:                CLK_INC();                                                     \
377:                CLK_INC();                                                     \
379:                CLK_INC();                                                     \
381:                CLK_INC();                                                     \
383:                CLK_INC();                                                     \
399:                    CLK_INC();                                                 \
401:                    CLK_INC();                                                 \
472:    CLK_INC();
477:    CLK_INC();
481:    CLK_INC();
486:        CLK_INC();                        \
489:    CLK_INC();
494:        CLK_INC();                                             \
500:        CLK_INC();                                             \
506:    CLK_INC();
511:    CLK_INC();
516:    CLK_INC();
521:    CLK_INC();
526:    CLK_INC();
531:    CLK_INC();
536:    CLK_INC();
541:        CLK_INC();                                 \
544:    CLK_INC();
552:    CLK_INC();
557:    CLK_INC();
561:    CLK_INC();
566:        CLK_INC();                         \
569:    CLK_INC();
574:        CLK_INC();            \
581:    CLK_INC();
586:    CLK_INC();
591:    CLK_INC();
596:    CLK_INC();
601:    CLK_INC();
606:        CLK_INC();                                  \
609:    CLK_INC();
618:    CLK_INC();                      \
621:    CLK_INC();                      \
624:    CLK_INC();
630:    CLK_INC();
636:        CLK_INC();          \
642:    CLK_INC();                                                 \
644:    CLK_INC();                                                 \
647:        CLK_INC();                                             \
654:    CLK_INC();                                                 \
656:    CLK_INC();                                                 \
659:        CLK_INC();                                             \
666:    CLK_INC();                                                        \
668:    CLK_INC();                                                        \
671:        CLK_INC();                                                    \
678:    CLK_INC();
683:    CLK_INC();
689:        CLK_INC();          \
695:        CLK_INC();                        \
698:    CLK_INC();
719:        CLK_INC();                                              \
972:                CLK_INC();                                        \
975:                    CLK_INC();                                    \
996:            CLK_INC();                                        \
999:                CLK_INC();                                    \
1188:        CLK_INC();                                                   \
1190:        CLK_INC();                                                   \
1207:            CLK_INC();                            \
1211:        CLK_INC();                                \
1213:        CLK_INC();                                \
1217:        CLK_INC();                                \
1357:        CLK_INC();        \
1364:        CLK_INC();                      \
1372:            CLK_INC();            \
1375:        CLK_INC();                \
1385:            CLK_INC();                                        \
1388:        CLK_INC();                                            \
1483:            CLK_INC();                  \
1486:        CLK_INC();                      \
1489:        CLK_INC();                      \
1491:        CLK_INC();                      \
1502:            CLK_INC();        \
1505:        CLK_INC();            \
1507:        CLK_INC();            \
1509:        CLK_INC();            \
1594:            CLK_INC();                                                        \

F:\GitHub\vice-sharp\native\vice\vice\src\c64\c64cpusc.c
35:#ifdef VICE_SHIM_HOSTED
51:#ifdef VICE_SHIM_HOSTED
56:   EXPORT_REGISTERS()/IMPORT_REGISTERS() are in scope here because CLK_INC()
58:#define CLK_INC()                                      \
71:#define CLK_INC()                                  \
156:            CLK_INC();                                         \
158:            CLK_INC();                                         \
161:                CLK_INC();                                     \
166:            CLK_INC();                                         \
168:            CLK_INC();                                         \
171:                CLK_INC();                                     \
184:            CLK_INC();                                           \
187:            CLK_INC();                                           \
191:                CLK_INC();                                       \
196:            CLK_INC();                                           \
198:            CLK_INC();                                           \
201:                CLK_INC();                                       \

F:\GitHub\vice-sharp\native\vice\vice\src\c64\cart\turbomaster.c
323:#define CLK_INCREMENT ++maincpu_clk
343:            CLK_INCREMENT;
347:        CLK_INCREMENT;
353:    CLK_INCREMENT;
355:        CLK_INCREMENT;

F:\GitHub\vice-sharp\native\vice\vice\src\mainc64cpu.c
59:#ifdef VICE_SHIM_HOSTED
759:#ifdef VICE_SHIM_HOSTED
833:                CLK_INC();                                            \

F:\GitHub\vice-sharp\native\vice\vice\src\vice-shim-runtime.h
7: * -DVICE_SHIM_HOSTED (see native/build-vice-shim.sh). See
11:/* Called once per CPU cycle from the hosted CLK_INC(). Returns nonzero when the

F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
626:                CLK_INC();                                            \

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20cpu.c
43:#define CLK_INC() maincpu_clk++; vic_cycle()
120:            CLK_INC();                                          \
121:            CLK_INC();                                          \
123:                CLK_INC();                                      \
127:            CLK_INC();                                          \
129:            CLK_INC();                                          \
132:                CLK_INC();                                      \
143:            CLK_INC();                                            \
145:            CLK_INC();                                            \
148:                CLK_INC();                                        \
152:            CLK_INC();                                            \
154:            CLK_INC();                                            \
157:                CLK_INC();                                        \

F:\GitHub\vice-sharp\native\vice\vice\src\scpu64\scpu64cpu.c
401:#define CLK_INC(clock) scpu64_clock_inc(0)
</workspace_result>

### Turn 14 (Function)
[tool_response]
exit: 0

Name
----
accuracy-first-verify-sources.md
add-profile.grok.md
approve-before-execute.md
bring-the-receipts.md
lab-authorization.md
log-decisions-as-conclusions.md
never-skip-explicit-actions.md
no-attitude-honesty-tell.md
no-python-lab.md
no-shortcuts-precision-over-convenience.md
philosophical-dialogue-mode.md
PROFILE.md
session-turn-title-summary.md
user-payton-byrd.md
implementer



### Turn 15 (Assistant)
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\PROFILE.md
- limit: 80
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\user-payton-byrd.md
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\c64\c64cpusc.c
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20cpu.c
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\build-vice-shim.sh
- limit: 200
[tool_request: grep]
- pattern: vic20|xvic|Vic20|vice_xvic|machine_create
- path: F:\GitHub\vice-sharp\native\vice-shim.c
[tool_request: grep]
- pattern: Vic20Lockstep|LockstepValidator|vice_xvic|vic20
- path: F:\GitHub\vice-sharp\tests
- glob: **/*.{cs}
- head_limit: 40

### Turn 16 (Function)
[tool_response]
1→#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd "$(dirname "$0")" && pwd)
vice_root="$script_dir/vice/vice"
vice_src="$vice_root/src"
vice_shim_root="$script_dir"
shim_dll="$script_dir/vice_x64.dll"
vice_patch="$script_dir/patches/vice-shim-runtime.patch"
10→vice_prompt="$script_dir/patches/vice-shim-runtime.prompt.md"
mingw_bin=$(dirname "$(command -v gcc)")
tmp_makefile=$(mktemp)
vice_shim_include_flags="-I. -I$vice_shim_root -I$vice_src -I$vice_src/sid"
linenoise_dir="$vice_src/lib/linenoise-ng"

cleanup() {
  rm -f "$tmp_makefile"
}

20→trap cleanup EXIT

while IFS= read -r include_dir; do
  vice_shim_include_flags="$vice_shim_include_flags -I$include_dir"
done < <(find "$vice_src" -mindepth 1 -maxdepth 3 -type d | sort)

if [[ -f "$vice_patch" ]]; then
  if git -C "$script_dir/vice" apply --reverse --check "$vice_patch" >/dev/null 2>&1; then
    :
  elif git -C "$script_dir/vice" apply --check "$vice_patch" >/dev/null 2>&1; then
30→    git -C "$script_dir/vice" apply "$vice_patch"
  else
    echo "VICE hosted runtime patch no longer applies cleanly."
    echo "Use the manual prompt at: $vice_prompt"
    exit 1
  fi
fi

if [[ ! -f "$vice_root/configure" ]]; then
  cd "$vice_root"
40→  ./autogen.sh
fi

# Configure flags the shim build requires. reSID is mandatory (the managed
# Sid6581 is modelled on it) and fastsid must stay out of the build entirely.
vice_configure_flags=(
  --enable-option-checking=fatal
  --enable-headlessui
  --disable-arch
  --disable-html-docs
50→  --disable-pdf-docs
  --disable-catweasel
  --disable-hardsid
  --disable-ethernet
  --disable-midi
  --disable-parsid
  --disable-realdevice
  --disable-rs232
  --disable-openmp
  --disable-ipv6
60→  --with-resid
  --without-flac
  --without-gif
  --without-lame
  --without-mpg123
  --without-portaudio
  --without-vorbis
  --without-libcurl
  --without-libieee1284
  --without-unzip-bin
70→  --without-png
)

# Reconfigure when config.h is missing OR the flag set changed since the last
# configure. Without the flag-change check a stale config.h (e.g. an older
# fastsid build) would silently survive and the shim would link the wrong SID
# engine. The marker records the exact flags config.h was last built with.
vice_configure_marker="$vice_src/.vice-shim-configure-flags"
if [[ ! -f "$vice_src/config.h" ]] \
   || [[ ! -f "$vice_configure_marker" ]] \
80→   || [[ "$(cat "$vice_configure_marker" 2>/dev/null)" != "${vice_configure_flags[*]}" ]]; then
  echo "Configuring VICE (config.h missing or shim configure flags changed)."
  cd "$vice_root"
  rm -f "$vice_src/config.h"
  ./configure "${vice_configure_flags[@]}"
  printf '%s' "${vice_configure_flags[*]}" > "$vice_configure_marker"
fi

# Clean potentially malformed dependency maps that can be produced by previous
# Windows-prefixed build artifacts and break dependency includes in this environment.
90→if [[ -d "$linenoise_dir/.deps" ]]; then
  rm -f "$linenoise_dir/.deps"/*.Po
fi

make -C "$linenoise_dir"
# Use the automake-generated phony target; plain "x64sc" is not emitted.
make -C "$vice_src" -j4 x64sc-program

cat > "$tmp_makefile" <<EOF
VICE_SHIM_ROOT := $vice_shim_root
100→VICE_SHIM_INCLUDE_FLAGS := $vice_shim_include_flags
VICE_SHIM_OBJ := \$(VICE_SHIM_ROOT)/vice-shim-hosted.o
VICE_SHIM_MAINCPU_OBJ := \$(VICE_SHIM_ROOT)/mainc64cpu-hosted.o
VICE_SHIM_VICII_CYCLE_OBJ := \$(VICE_SHIM_ROOT)/vicii-cycle-hosted.o
VICE_SHIM_DLL := \$(VICE_SHIM_ROOT)/vice_x64.dll

\$(VICE_SHIM_OBJ): \$(VICE_SHIM_ROOT)/vice-shim.c \$(VICE_SHIM_ROOT)/vice-shim.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -c -o \$@ \$(VICE_SHIM_ROOT)/vice-shim.c

\$(VICE_SHIM_MAINCPU_OBJ): c64/c64cpusc.c mainc64cpu.c vice-shim-runtime.h
110→	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -DVICE_SHIM_HOSTED -c -o \$@ c64/c64cpusc.c

\$(VICE_SHIM_VICII_CYCLE_OBJ): viciisc/vicii-cycle.c viciisc/vicii-cycle.h vice-shim-runtime.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -DVICE_SHIM_HOSTED -c -o \$@ viciisc/vicii-cycle.c

\$(VICE_SHIM_DLL): \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(VICE_SHIM_VICII_CYCLE_OBJ) \$(x64sc_OBJECTS) \$(x64sc_DEPENDENCIES)
	\$(CCLD) \$(AM_CFLAGS) \$(CFLAGS) \$(x64sc_LDFLAGS) \$(LDFLAGS) -shared -static-libgcc -o \$@ \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(VICE_SHIM_VICII_CYCLE_OBJ) \$(x64sc_OBJECTS) \$(x64sc_LDADD) \$(LIBS)
EOF

make -C "$vice_src" -s -f Makefile -f "$tmp_makefile" "$shim_dll"
120→
for dep in libiconv-2.dll zlib1.dll libstdc++-6.dll libwinpthread-1.dll libgcc_s_seh-1.dll; do
  if [[ -f "$mingw_bin/$dep" ]]; then
    cp -f "$mingw_bin/$dep" "$script_dir/$dep"
  fi
done


### Turn 17 (Function)
[tool_response]
1→/*
 * vic20cpu.c - Emulation of the VIC 6502 processor.
 *
 * Written by
 *  Hannu Nuotio <hannu.nuotio@tut.fi>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
 * See README for copyright notice.
 *
10→ *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
20→ *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */

#include "vice.h"

#include <stdio.h>
30→
#include "monitor.h"
#include "vic-cycle.h"

/* ------------------------------------------------------------------------- */

/* Global clock counter.  */
CLOCK maincpu_clk = 0L;
/* if != 0, exit when this many cycles have been executed */
CLOCK maincpu_clk_limit = 0L;
40→
#define REWIND_FETCH_OPCODE(clock) /*clock-=2*/

#define CLK_INC() maincpu_clk++; vic_cycle()

#define CLK_ADD(clock, amount) nosuchfunction(&clock, amount)

/* Skip cycle implementation */

#define SKIP_CYCLE 0
50→
/* opcode_t etc */

#if !defined WORDS_BIGENDIAN && defined ALLOW_UNALIGNED_ACCESS

#define opcode_t uint32_t

#define p0 (opcode & 0xff)
#define p1 ((opcode >> 8) & 0xff)
#define p2 (opcode >> 8)
60→
#define SET_OPCODE(o) (opcode) = o;

#else /* WORDS_BIGENDIAN || !ALLOW_UNALIGNED_ACCESS */

#define opcode_t          \
    struct {              \
        uint8_t ins;         \
        union {           \
            uint8_t op8[2];  \
70→            uint16_t op16;    \
        } op;             \
    }

#define p0 (opcode.ins)
#define p2 (opcode.op.op16)

#ifdef WORDS_BIGENDIAN

#define p1 (opcode.op.op8[1])
80→

### Turn 18 (Function)
[tool_response]
1→/*
 * c64cpusc.c - Emulation of the C64 6510 processor for x64sc.
 *
 * Written by
 *  Hannu Nuotio <hannu.nuotio@tut.fi>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
 * See README for copyright notice.
 *
10→ *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
20→ *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */

#include "vice.h"

#include <stdio.h>
30→
#include "cpmcart.h"
#include "monitor.h"
#include "vicii-cycle.h"

#ifdef VICE_SHIM_HOSTED
#include "../vice-shim-runtime.h"
#endif

/* ------------------------------------------------------------------------- */
40→
/* Global clock counter.  */
CLOCK maincpu_clk = 0L;
/* if != 0, exit when this many cycles have been executed */
CLOCK maincpu_clk_limit = 0L;

#define REWIND_FETCH_OPCODE(clock) /*clock-=2*/

/* Mask: BA low */
int maincpu_ba_low_flags = 0;
50→
#ifdef VICE_SHIM_HOSTED
/* Hosted build (ViceSharp): after the normal per-cycle accounting, publish the
   CPU register file to the shim, let the host pause/stop at this cycle
   boundary, then re-import in case the host adjusted state. A stop request
   unwinds maincpu_mainloop() (return) so the hosting thread regains control.
   EXPORT_REGISTERS()/IMPORT_REGISTERS() are in scope here because CLK_INC()
   only expands inside the 6510dtvcore.c body, after those macros are defined. */
#define CLK_INC()                                      \
    do {                                               \
60→        interrupt_delay();                             \
        maincpu_clk++;                                 \
        maincpu_ba_low_flags &= ~MAINCPU_BA_LOW_VICII; \
        maincpu_ba_low_flags |= vicii_cycle();         \
        EXPORT_REGISTERS();                            \
        if (vice_shim_cycle_checkpoint()) {            \
            return;                                    \
        }                                              \
        IMPORT_REGISTERS();                            \
    } while (0)
70→#else
#define CLK_INC()                                  \
    interrupt_delay();                             \
    maincpu_clk++;                                 \
    maincpu_ba_low_flags &= ~MAINCPU_BA_LOW_VICII; \
    maincpu_ba_low_flags |= vicii_cycle()
#endif


/* Skip cycle implementation */
80→
#define SKIP_CYCLE 0

/* Opcode info updated in FETCH_OPCODE.
   Needed for CLI/SEI detection in vicii_steal_cycles. */
#define OPCODE_UPDATE_IN_FETCH


/* opcode_t etc */

90→#if !defined WORDS_BIGENDIAN && defined ALLOW_UNALIGNED_ACCESS

#define opcode_t uint32_t

#define p0 (opcode & 0xff)
#define p1 ((opcode >> 8) & 0xff)
#define p2 (opcode >> 8)

#define SET_OPCODE(o) (opcode) = o;

100→#else /* WORDS_BIGENDIAN || !ALLOW_UNALIGNED_ACCESS */

### Turn 19 (Function)
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


### Turn 20 (Function)
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

### Turn 21 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 7 matching lines
F:\GitHub\vice-sharp\native\vice-shim.c
164:                            (LPCSTR)&vice_machine_create,
370:VICE_SHIM_API void *vice_machine_create(void)
372:    return vice_machine_create_model(NULL);
375:VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)
378:        fprintf(stderr, "vice_machine_create call=%u model=%s\\n", ++g_debug_create_calls, model_selector == NULL ? "default" : model_selector);
399:        fprintf(stderr, "vice_machine_create unknown model=%s\\n", model_selector == NULL ? "(null)" : model_selector);
485:        fprintf(stderr, "vice_machine_create returns=%p\\n", (void *)machine);
</workspace_result>

### Turn 22 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 23 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\CaptureRunLogUtilityTests.cs
50:        LockstepValidator.SaveNativeRunLog(vsf!, cycles, outPath!);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Launcher\ViceArgsParserTests.cs
258:    /// - native/vice/vice/src/vic20/cart/debugcart.c (and c64 equivalents): "debug \"cartridge\"
329:    /// - native/vice/vice/src/vic20/cart/debugcart.c:1 ( "debug \"cartridge\" used for automatic regression testing"),
408:    /// - native/vice/vice/src/vic20/cart/debugcart.c:1 ("debug \"cartridge\" used for automatic regression testing"),

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidationTests.cs
9:    private readonly LockstepValidator _validator;
13:        _validator = new LockstepValidator();
21:    /// Acceptance: <see cref="LockstepValidator"/> reports zero cycles and

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
18:public sealed class LockstepValidator : IDisposable
42:    public LockstepValidator(

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidatorKernalCloseTests.cs
6:public sealed class LockstepValidatorKernalCloseTests
27:        var target = LockstepValidator.ResolveKernalJumpTarget(bus, 0xFFC3);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\NativeCollectionConventionTests.cs
20:///   - non-test-class native users (shared helpers like LockstepValidator, the

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\SnapshotResumeCpuStateTests.cs
24:    /// Use case: LockstepValidator.StageManagedFromNative resumes the managed C64

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\SnapshotRunLogParityTests.cs
20:/// from a .vsf (<see cref="LockstepValidator.SaveNativeRunLog"/>), and the managed C64
118:        LockstepValidator.StageManagedFromNative(machine, native);
215:            LockstepValidator.SaveNativeRunLog(vsf, Cycles, logPath);
225:            LockstepValidator.StageManagedFromNative(machine, native);
308:    /// Acceptance: <see cref="LockstepValidator.RunFromSnapshot"/> reports ZERO
325:        using var validator = new LockstepValidator(ModelSelector);
355:    /// <see cref="LockstepValidator.RunFromSnapshot"/> with a temp .mp4 path so the
386:            using var validator = new LockstepValidator(ModelSelector);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20LockstepScaffoldTests.cs
9:/// <see cref="LockstepValidator"/> Vic20 path lands.
15:public sealed class Vic20LockstepScaffoldTests
</workspace_result>

### Turn 23 (Assistant)
[tool_request: run_terminal_command]
- command: Get-ChildItem "$env:USERPROFILE\.claude\profile\*.md" | Where-Object { $_.Name -notlike 'add-profile*' } | ForEach-Object { $_.Name }
- description: List all profile markdown files
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 370
- limit: 150
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20LockstepScaffoldTests.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
- offset: 1
- limit: 100
[tool_request: grep]
- pattern: xvic_OBJECTS|xvic_LDADD|xvic-program
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/Makefile*
- head_limit: 40
[tool_request: run_terminal_command]
- command: Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer" -Recurse -ErrorAction SilentlyContinue | Select-Object FullName, Length, LastWriteTime | Format-Table -AutoSize; Get-Content "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\xvic-build.log" -Tail 40 -ErrorAction SilentlyContinue; if (Test-Path "C:\Users\kingd\AppData\Local\Temp\grok-goal-*\*\plan.md") { Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-*\*\plan.md" | Select-Object FullName }
- description: Inspect goal scratch and prior build log

### Turn 24 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-VIC20-LOCKSTEP-001 / Slice N0.
/// Use case: document native <c>xvic</c> lockstep readiness before full
/// <see cref="LockstepValidator"/> Vic20 path lands.
10→/// Acceptance: native shim currently hosts C64 models only
/// (<c>vice_machine_create_model</c> / <c>c64model_set</c>); Vic20 selectors
/// must fail closed until multi-machine shim work lands. Managed dual-machine
/// determinism remains the interim accuracy gate.
/// </summary>
public sealed class Vic20LockstepScaffoldTests
{
    /// <summary>
    /// N0: Vic20 profile selectors are not accepted by the C64-only native shim.
    /// </summary>
20→    [Theory]
    [InlineData("vic20")]
    [InlineData("vic20ntsc")]
    [InlineData("xvic")]
    public void NativeShim_DoesNotCreateVic20Models_Yet(string modelSelector)
    {
        // Production path: CreateInstance throws when create_model returns null.
        var ex = Assert.ThrowsAny<Exception>(() =>
        {
            using var native = ViceNative.CreateInstance(modelSelector);
30→            _ = native;
        });

        Assert.True(
            ex is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException,
            $"unexpected exception type {ex.GetType().Name}: {ex.Message}");
    }

    /// <summary>
    /// Interim accuracy: managed Vic20 machines stay deterministic (N2 precursor).
40→    /// </summary>
    [Fact]
    public void ManagedVic20_TwoMachines_AgreeAfterFrames_UntilNativeXvicExists()
    {
        var a = MachineTestFactory.CreateVic20Machine("vic20");
        var b = MachineTestFactory.CreateVic20Machine("vic20");
        for (var i = 0; i < 16; i++)
        {
            a.RunFrame();
            b.RunFrame();
50→        }

        Assert.Equal(a.GetState().PC, b.GetState().PC);
        Assert.Equal(a.GetState().Cycle, b.GetState().Cycle);
    }
}


### Turn 25 (Function)
[tool_response]
370→VICE_SHIM_API void *vice_machine_create(void)
{
    return vice_machine_create_model(NULL);
}

VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)
{
    if (g_debug_create_calls < 8) {
        fprintf(stderr, "vice_machine_create call=%u model=%s\\n", ++g_debug_create_calls, model_selector == NULL ? "default" : model_selector);
        fflush(stderr);
380→    }

    vice_machine_t *machine;
    int model;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    while (g_active_machine != NULL) {
        SleepConditionVariableCS(&g_state_cv, &g_state_lock, INFINITE);
390→    }

    if (!vice_shim_initialize_runtime_locked()) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    model = vice_shim_model_from_selector(model_selector);
    if (model == C64MODEL_UNKNOWN) {
        fprintf(stderr, "vice_machine_create unknown model=%s\\n", model_selector == NULL ? "(null)" : model_selector);
400→        fflush(stderr);
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    c64model_set(model);
    vice_shim_detach_cartridge_locked();
    file_system_detach_disk_all();
    /* Re-baseline Drive{8..11}TrueEmulation to the VICE DEFAULT (1,
       drive-resources.c:401) at every machine boundary. A .vsf whose DRIVE8
410→       module carries has_tde=0 makes drive_snapshot_read_module call
       resources_set_int("Drive8TrueEmulation", 0) (drive-snapshot.c:334-363),
       silently disabling the true drive for the REST OF THE PROCESS: with no
       drive holding serial DATA, the kernal's $DD00 read flips $47 -> $C7
       (iecbus.c conf0 path via c64cia2.c read_ciapa) and every later
       kernal-boot lockstep test diverges (TEST-NATIVE-RESIDUE-02 fingerprint;
       the X64Sc ResetAfterActivity in-suite cascade). The re-baseline MUST go
       through resources_set_int so set_drive_true_emulation recomputes the
       iecbus function-local statics + $DD00 callback pointers
       (iecbus.c:512-548, calculate_callback_index) and the serial trap
420→       gating (serial-trap.c serial_truedrive) - poking unit->enable directly
       would leave the conf0 callbacks installed. Forcing 0 here (tried
       earlier) is the same bug from the other side: 1 IS the default.
       Tests that need TDE off set it via vice_drive_set_true_emulation. */
    {
        unsigned int unit;
        char resource_name[32];
        for (unit = 8; unit <= 11; unit++) {
            snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
            resources_set_int(resource_name, 1);
430→        }
    }

    /* Machine-boundary drive-clock re-baseline (TEST-NATIVE-RESIDUE-03): a
       fresh drive boots with attach_clk/detach_clk/attach_detach_clk = 0
       (drive_init, drive.c). file_system_detach_disk_all() above sets detach_clk
       (drive_image_detach, driveimage.c), and a has_tde=1 .vsf restore lands the
       snapshot's clocks; both persist into the NEXT machine because
       diskunit_context[] is process-global, forcing the write-protect sense
       (drive-writeprotect.c) and rotation entry (rotation.c) windows on the next
440→       1541 boot and shifting the IEC DATA timeline read at $DD00. No resource
       path exists short of full drive_init, so poke the fields directly like the
       vicii.rc/idle_state clears below. Must run AFTER the TDE re-baseline loop
       (whose drive_enable can re-stamp attach_clk). cycle_accum is handled by
       the patched drivecpu_reset_clk the TDE re-baseline already invokes. */
    {
        unsigned int clk_unit, clk_drive;
        for (clk_unit = 0; clk_unit < NUM_DISK_UNITS; clk_unit++) {
            if (diskunit_context[clk_unit] == NULL) {
                continue;
450→            }
            for (clk_drive = 0; clk_drive < NUM_DRIVES; clk_drive++) {
                drive_t *clk_dptr = diskunit_context[clk_unit]->drives[clk_drive];
                if (clk_dptr != NULL) {
                    clk_dptr->attach_clk = 0;
                    clk_dptr->detach_clk = 0;
                    clk_dptr->attach_detach_clk = 0;
                }
            }
        }
460→    }

    /* Machine-boundary residue clear (TEST-NATIVE-RESIDUE-01): a .vsf resume
       leaves the VIC row counter mid-frame, and NOTHING in viciisc ever
       re-zeroes vicii.rc (vicii.c powerup/reset skip it; only the monitor
       prints it). With rc stuck at 7, the first end-of-line display check
       (vicii-cycle.c:556) flips idle_state=1 within ~60 cycles of the NEXT
       machine's boot, diverging CPU lockstep around cycle 167 for every
       machine created after a snapshot test (the A=$47-vs-$C7 in-suite
       cascade). Cleared at CREATE - a fresh machine boots with rc=0 like the
470→       managed core - and deliberately NOT in vice_machine_reset: VICE's own
       reset preserves rc, and within-test resets must keep that semantics
       (ResetAfterActivity parity). */
    vicii.rc = 0;
    vicii.idle_state = 0;

    machine = (vice_machine_t *)calloc(1, sizeof(*machine));
    if (machine == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
480→    }

    machine->magic = VICE_MACHINE_MAGIC;
    machine->c64_model = model;
    if (g_debug_create_calls < 8) {
        fprintf(stderr, "vice_machine_create returns=%p\\n", (void *)machine);
        fflush(stderr);
    }
    g_active_machine = machine;
    LeaveCriticalSection(&g_state_lock);
490→
    return machine;
}

static int vice_shim_model_from_selector(const char *selector)
{
    if (selector == NULL || selector[0] == '\0'
        || vice_shim_selector_equals(selector, "c64")
        || vice_shim_selector_equals(selector, "breadbox")
        || vice_shim_selector_equals(selector, "pal")
500→        || vice_shim_selector_equals(selector, "c64-pal")
        || vice_shim_selector_equals(selector, "commodore64")) {
        return C64MODEL_C64_PAL;
    }

    if (vice_shim_selector_equals(selector, "c64c")
        || vice_shim_selector_equals(selector, "newpal")
        || vice_shim_selector_equals(selector, "c64new")
        || vice_shim_selector_equals(selector, "c64c-pal")) {
        return C64MODEL_C64C_PAL;
510→    }

    if (vice_shim_selector_equals(selector, "c64old")
        || vice_shim_selector_equals(selector, "oldpal")
        || vice_shim_selector_equals(selector, "c64old-pal")) {
        return C64MODEL_C64_OLD_PAL;
    }

    if (vice_shim_selector_equals(selector, "ntsc")
        || vice_shim_selector_equals(selector, "c64ntsc")

### Turn 26 (Function)
[tool_response]
1→/*
 * mainviccpu.c - Emulation of the VIC 6510 processor.
 *
 * Written by
 *  Ettore Perazzoli <ettore@comm2000.it>
 *  Andreas Boose <viceteam@t-online.de>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
 * See README for copyright notice.
10→ *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
20→ *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */

#include "vice.h"

30→#include <stdio.h>
#include <stdlib.h>

#include "6510core.h"
#include "alarm.h"
#include "archdep.h"
#include "autostart.h"
#include "debug.h"
#include "interrupt.h"
#include "machine.h"
40→#include "maincpu.h"
#include "mem.h"
#include "monitor.h"
#include "mos6510.h"
#include "snapshot.h"
#include "resources.h"
#include "cmdline.h"
#include "traps.h"
#include "types.h"

50→#ifndef EXIT_FAILURE
#define EXIT_FAILURE 1
#endif

log_t maincpu_log = LOG_DEFAULT;

/* MACHINE_STUFF should define/undef

 - NEED_REG_PC

60→*/

/* ------------------------------------------------------------------------- */
#if defined (DEBUG) || defined (FEATURE_CPUMEMHISTORY)
CLOCK debug_clk;
#endif

CLOCK stolen_cycles;

#define NEED_REG_PC
70→
/* ------------------------------------------------------------------------- */

/* Implement the hack to make opcode fetches faster.  */
#define JUMP(addr)                                                                         \
    do {                                                                                   \
        reg_pc = (unsigned int)(addr);                                                     \
        if (reg_pc >= (unsigned int)bank_limit || reg_pc < (unsigned int)bank_start) {     \
            mem_mmu_translate((unsigned int)(addr), &bank_base, &bank_start, &bank_limit); \
        }                                                                                  \
80→    } while (0)

/* ------------------------------------------------------------------------- */

#ifdef FEATURE_CPUMEMHISTORY

/* this is called per memory access, so it should only do whats really needed */
inline static void memmap_mem_update(unsigned int addr, int write, int dummy)
{
    unsigned int type = MEMMAP_RAM_R;
90→    unsigned int a_m = addr >> 8;

    if (((a_m >= 0x90) && (a_m <= 0x93)) ||
        ((a_m >= 0x98) && (a_m <= 0x9f))) {
        /* FIXME: IO2 or IO3 could be RAM */
        type = MEMMAP_I_O_R;
    } else if (((a_m >= 0x60) && (a_m <= 0x7f)) ||
               ((a_m >= 0x80) && (a_m <= 0x8f)) || /* chargen */
                (a_m >= 0xa0)) {
        /* FIXME: blocks 1,2,3 could be ROM also */
100→        type = MEMMAP_ROM_R;

### Turn 27 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 20 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\Makefile
290:am_xvic_OBJECTS = $(am__objects_1) $(am__objects_2)
291:xvic_OBJECTS = $(am_xvic_OBJECTS)
1508:xvic_LDADD = $(xvic_libs) $(emu_extlibs) $(mascuerade_lib)   $(XVIC_RES)
1862:xvic$(EXEEXT): $(xvic_OBJECTS) $(xvic_DEPENDENCIES) $(EXTRA_xvic_DEPENDENCIES) 
1864:	$(AM_V_CCLD)$(xvic_LINK) $(xvic_OBJECTS) $(xvic_LDADD) $(LIBS)
2449:.PHONY: xvic-all xvic-program
2452:	$(MAKE) xvic-program
2454:xvic-program: \

F:\GitHub\vice-sharp\native\vice\vice\src\Makefile.am
1042:xvic_LDADD = $(xvic_libs) $(emu_extlibs) $(mascuerade_lib) @TFE_LIBS@ @NETPLAY_LIBS@ $(XVIC_RES)
1052:.PHONY: xvic-all xvic-program
1055:	$(MAKE) xvic-program
1057:xvic-program: \

F:\GitHub\vice-sharp\native\vice\vice\src\Makefile.in
290:am_xvic_OBJECTS = $(am__objects_1) $(am__objects_2)
291:xvic_OBJECTS = $(am_xvic_OBJECTS)
1508:xvic_LDADD = $(xvic_libs) $(emu_extlibs) $(mascuerade_lib) @TFE_LIBS@ @NETPLAY_LIBS@ $(XVIC_RES)
1862:xvic$(EXEEXT): $(xvic_OBJECTS) $(xvic_DEPENDENCIES) $(EXTRA_xvic_DEPENDENCIES) 
1864:	$(AM_V_CCLD)$(xvic_LINK) $(xvic_OBJECTS) $(xvic_LDADD) $(LIBS)
2449:.PHONY: xvic-all xvic-program
2452:	$(MAKE) xvic-program
2454:xvic-program: \
</workspace_result>

### Turn 28 (Function)
[tool_response]
exit: 0
accuracy-first-verify-sources.md
approve-before-execute.md
bring-the-receipts.md
lab-authorization.md
log-decisions-as-conclusions.md
never-skip-explicit-actions.md
no-attitude-honesty-tell.md
no-python-lab.md
no-shortcuts-precision-over-convenience.md
philosophical-dialogue-mode.md
PROFILE.md
session-turn-title-summary.md
user-payton-byrd.md


### Turn 29 (Function)
[tool_response]
exit: 0

FullName                                                                            Length LastWriteTime
--------                                                                            ------ -------------
C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\xvic-build.log  59173 8/5/2026 2:13:31 PM

rm -f libvic20cart.a
ar cr libvic20cart.a behrbonz.o debugcart.o finalexpansion.o ioramcart.o megacart.o mikroassembler.o minimon.o rabbit.o superexpander.o ultimem.o vic-fp.o vic20-generic.o vic20-ieee488.o vic20-midi.o vic20-sidcart.o vic20cart.o vic20cartmem.o writenow.o 
ranlib libvic20cart.a
make[1]: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src/vic20/cart'
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20ieeevia2.o -MD -MP -MF .deps/vic20ieeevia2.Tpo -c -o vic20ieeevia2.o vic20ieeevia2.c
mv -f .deps/vic20iec.Tpo .deps/vic20iec.Po
mv -f .deps/vic20ieeevia1.Tpo .deps/vic20ieeevia1.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20io.o -MD -MP -MF .deps/vic20io.Tpo -c -o vic20io.o vic20io.c
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20mem.o -MD -MP -MF .deps/vic20mem.Tpo -c -o vic20mem.o vic20mem.c
mv -f .deps/vic20ieeevia2.Tpo .deps/vic20ieeevia2.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20memrom.o -MD -MP -MF .deps/vic20memrom.Tpo -c -o vic20memrom.o vic20memrom.c
mv -f .deps/vic20memrom.Tpo .deps/vic20memrom.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20memsnapshot.o -MD -MP -MF .deps/vic20memsnapshot.Tpo -c -o vic20memsnapshot.o vic20memsnapshot.c
mv -f .deps/vic20io.Tpo .deps/vic20io.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20model.o -MD -MP -MF .deps/vic20model.Tpo -c -o vic20model.o vic20model.c
mv -f .deps/vic20memsnapshot.Tpo .deps/vic20memsnapshot.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20printer.o -MD -MP -MF .deps/vic20printer.Tpo -c -o vic20printer.o vic20printer.c
mv -f .deps/vic20model.Tpo .deps/vic20model.Po
mv -f .deps/vic20printer.Tpo .deps/vic20printer.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20rom.o -MD -MP -MF .deps/vic20rom.Tpo -c -o vic20rom.o vic20rom.c
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20romset.o -MD -MP -MF .deps/vic20romset.Tpo -c -o vic20romset.o vic20romset.c
mv -f .deps/vic20romset.Tpo .deps/vic20romset.Po
mv -f .deps/vic20mem.Tpo .deps/vic20mem.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20rsuser.o -MD -MP -MF .deps/vic20rsuser.Tpo -c -o vic20rsuser.o vic20rsuser.c
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20sound.o -MD -MP -MF .deps/vic20sound.Tpo -c -o vic20sound.o vic20sound.c
mv -f .deps/vic20rom.Tpo .deps/vic20rom.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20via1.o -MD -MP -MF .deps/vic20via1.Tpo -c -o vic20via1.o vic20via1.c
mv -f .deps/vic20rsuser.Tpo .deps/vic20rsuser.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20via2.o -MD -MP -MF .deps/vic20via2.Tpo -c -o vic20via2.o vic20via2.c
mv -f .deps/vic20sound.Tpo .deps/vic20sound.Po
mv -f .deps/vic20via1.Tpo .deps/vic20via1.Po
cc -DHAVE_CONFIG_H -I. -I../../src  -I../../src/arch/systemheaderoverride -DNDEBUG -I../../src/arch/headless -I../../src/arch/shared -I../../src/arch/mingw32-pcap/wpcap -I../../src -I../../src -I../../src/datasette -I../../src/drive -I../../src/printerdrv -I../../src/sid -I../../src/tape -I../../src/raster -I../../src/video -I../../src/userport -I../../src/vic20/cart -I../../src/lib/p64 -I../../src/joyport -I../../src/samplerdrv -I../../src/tapeport -I../../src/rs232drv -I../../src/core -I../../src/core/rtc  -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare  -MT vic20video.o -MD -MP -MF .deps/vic20video.Tpo -c -o vic20video.o vic20video.c
mv -f .deps/vic20via2.Tpo .deps/vic20via2.Po
mv -f .deps/vic20video.Tpo .deps/vic20video.Po
mv -f .deps/vic20cpu.Tpo .deps/vic20cpu.Po
rm -f libvic20.a
ar cr libvic20.a vic-cmdline-options.o vic-color.o vic-cycle.o vic-draw.o vic-mem.o vic-resources.o vic-snapshot.o vic-timing.o vic.o vic20-cmdline-options.o vic20-resources.o vic20-snapshot.o vic20.o vic20bus.o vic20cpu.o vic20datasette.o vic20drive.o vic20export.o vic20iec.o vic20ieeevia1.o vic20ieeevia2.o vic20io.o vic20mem.o vic20memrom.o vic20memsnapshot.o vic20model.o vic20printer.o vic20rom.o vic20romset.o vic20rsuser.o vic20sound.o vic20via1.o vic20via2.o vic20video.o 
ranlib libvic20.a
make[1]: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src/vic20'
c++ -std=c++11   -g -O3 -W -Wall -Wextra -Wformat -Wformat-signedness -Wformat-security -Wshadow -Wpointer-arith -Wmissing-prototypes -Wstrict-prototypes -Wuninitialized -Wmissing-field-initializers -Wunreachable-code -Werror=implicit-function-declaration -Wfatal-errors -Wdiscarded-qualifiers -Wno-unknown-pragmas -Wunused-but-set-variable -Wno-sign-compare -Wno-ignored-qualifiers -Wno-unused-parameter -Wall -Wno-unused-parameter -Wno-sign-compare    -o xvic.exe alarm.o attach.o autostart.o autostart-prg.o cbmdos.o cbmimage.o charset.o clipboard.o cmdline.o color.o crc32.o crt.o debug.o dma.o event.o findpath.o fliplist.o gcr.o info.o init.o initcmdline.o interrupt.o kbdbuf.o keyboard.o keymap.o lib.o log.o machine-bus.o machine.o main.o mainlock.o m3u.o network.o opencbmlib.o palette.o profiler.o ram.o rawfile.o rawnet.o resources.o romset.o screenshot.o sha1.o snapshot.o socket.o sound.o sysfile.o traps.o util.o vicefeatures.o vsync.o zfile.o zipcode.o midi.o ../src/arch/shared/libarchdep.a ../src/tapeport/libtapeport.a ../src/datasette/libdatasette.a ../src/vic20/cart/libvic20cart.a ../src/vic20/libvic20.a ../src/c64/cart/libc64commoncart.a ../src/vic20/cart/libmascuerade.a ../src/drive/iec/libdriveiec.a ../src/drive/iecieee/libdriveiecieee.a ../src/drive/ieee/libdriveieee.a ../src/drive/libdrive.a ../src/drive/tcbm/libdrivetcbm.a ../src/lib/p64/libp64.a ../src/iecbus/libiecbus.a ../src/parallel/libparallel.a ../src/vdrive/libvdrive.a ../src/monitor/libmonitor.a ../src/sid/libsid.a ../src/joyport/libjoyport.a ../src/samplerdrv/libsamplerdrv.a ../src/arch/shared/sounddrv/libsounddrv.a ../src/arch/shared/mididrv/libmididrv.a ../src/arch/shared/socketdrv/libsocketdrv.a ../src/arch/shared/hwsiddrv/libhwsiddrv.a ../src/gfxoutputdrv/libgfxoutputdrv.a ../src/printerdrv/libprinterdrv.a ../src/diskimage/libdiskimage.a ../src/fsdevice/libfsdevice.a ../src/tape/libtape.a ../src/fileio/libfileio.a ../src/serial/libserial.a ../src/core/libcore.a  ../src/rs232drv/librs232drv.a 
../src/raster/libraster.a ../src/userport/libuserport.a ../src/diag/libdiag.a ../src/core/rtc/librtc.a ../src/video/libvideo.a ../src/arch/headless/libarch.a  ../src/imagecontents/libimagecontents.a ../src/vic20/libvic20stubs.a ../src/resid/libresid.a ../src/hvsc/libhvsc.a  ../src/lib/libzmbv/libzmbv.a -ldsound   -lz  ../src/arch/headless/libarch.a ../src/arch/shared/libarchdep.a ../src/lib/linenoise-ng/liblinenoiseng.a ../src/vic20/cart/libmascuerade.a    -lm -liconv  -lwinmm -lntdll -ld2d1 -ld3d11 -lavrt -lws2_32


### Turn 30 (Assistant)
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\approve-before-execute.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\bring-the-receipts.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\lab-authorization.md
- limit: 40
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\never-skip-explicit-actions.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\no-attitude-honesty-tell.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\no-python-lab.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\no-shortcuts-precision-over-convenience.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\philosophical-dialogue-mode.md
- limit: 30
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\session-turn-title-summary.md
- limit: 30
[tool_request: grep]
- pattern: VICE_SHIM_HOSTED|vice_shim
- path: F:\GitHub\vice-sharp\native\vice\vice\src\mainc64cpu.c
- glob: **/*
- -C: 3
[tool_request: grep]
- pattern: ViceNative|vice_x64|DllImport|LoadLibrary|model_selector|vic20
- path: F:\GitHub\vice-sharp\src
- glob: **/*ViceNative*
[tool_request: run_terminal_command]
- command: Get-ChildItem F:\GitHub\vice-sharp -Recurse -Filter "*ViceNative*" -ErrorAction SilentlyContinue | Select-Object FullName; Get-ChildItem F:\GitHub\vice-sharp\native\vice\vice\src -Filter "xvic*" | Select-Object Name, Length, LastWriteTime; Test-Path F:\GitHub\vice-sharp\native\vice\vice\src\xvic.exe; Get-ChildItem F:\GitHub\vice-sharp\native -Filter "vice_x*" | Select-Object Name, Length
- description: Locate ViceNative and xvic artifacts

### Turn 31 (Function)
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


### Turn 32 (Function)
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


### Turn 33 (Function)
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


### Turn 34 (Function)
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


### Turn 35 (Function)
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


### Turn 36 (Function)
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


### Turn 37 (Function)
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


### Turn 38 (Function)
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


### Turn 39 (Function)
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


### Turn 40 (Function)
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


### Turn 41 (Function)
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


### Turn 42 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 3 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\mainc64cpu.c
56-#include "traps.h"
57-#include "types.h"
58-
59:#ifdef VICE_SHIM_HOSTED
60-#include "vice-shim-runtime.h"
61-#endif
62-
--
756-     */
757-    bank_base_ready = true;
758-
759:#ifdef VICE_SHIM_HOSTED
760-    /* Hosted build (ViceSharp): the host bootstraps the CPU explicitly. When a
761-       bootstrap is pending, adopt the host-provided register file and clear the
762-       per-run CPU bookkeeping instead of triggering the normal power-on reset.
--
764-       unit (6510dtvcore.c defines it below, inside the loop), so the import is
765-       inlined here. reg_a_write/reg_a_read are #defined to reg_a in the
766-       non-DTV core, so the locals below are the CPU's real register storage. */
767:    if (vice_shim_take_bootstrap_maincpu()) {
768-        reg_a = maincpu_regs.a;
769-        reg_x = maincpu_regs.x;
770-        reg_y = maincpu_regs.y;
</workspace_result>

### Turn 43 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 20 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
13:public static unsafe partial class ViceNative
15:    private const string LibraryName = "vice_x64";
16:    private static readonly string[] AlternateLibraryNames = ["vice_x64", "vice-shim"];
29:    static ViceNative()
31:        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibrary);
37:        : "Native VICE library not found. Build or copy 'vice_x64'/'vice-shim' into the test output or a searched native directory.";
215:    public static IViceNative CreateInstance(string? modelSelector = null)
227:        return new ViceNativeInstance(handle);
530:    private static IntPtr ResolveLibrary(string libraryName, Assembly _, DllImportSearchPath? __)
669:    private sealed class ViceNativeInstance : IViceNative
674:        public ViceNativeInstance(IntPtr instance)
700:            ViceNative.AttachCartridge(_instance, image, mappingMode);
705:            ViceNative.AttachDisk(_instance, unit, drive, path);
710:            ViceNative.DetachDisk(_instance, unit, drive);
715:            ViceNative.SetKeyboardMatrixKey(_instance, row, column, pressed);
737:            global::ViceSharp.Core.ViceNative.GetVicState(_instance, ref state);
756:            global::ViceSharp.Core.ViceNative.GetCiaState(_instance, ciaIndex, ref state);
778:            global::ViceSharp.Core.ViceNative.GetCpuPipelineState(_instance, ref state);
805:            global::ViceSharp.Core.ViceNative.GetVicState(_instance, ref state);

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
6:public interface IViceNative : IDisposable
</workspace_result>

### Turn 44 (Function)
[tool_response]
exit: 0

FullName
--------
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceNativeBridge.cs

True




### Turn 45 (Assistant)
Profile loaded (14 files). Continuing N0: hosted Vic20 oracle path.
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- limit: 200
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.h
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\mainc64cpu.c
- offset: 750
- limit: 100
[tool_request: grep]
- pattern: vic20model|VIC20MODEL|vic20model_set
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*.{c,h}
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- offset: 1
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- offset: 500
- limit: 80
[tool_request: run_terminal_command]
- command: Get-Content "C:\Users\kingd\AppData\Local\Temp\grok-goal-*\plan.md" -ErrorAction SilentlyContinue | Select-Object -First 5; Get-ChildItem "C:\Users\kingd\AppData\Local\Temp" -Filter "plan.md" -Recurse -Depth 3 -ErrorAction SilentlyContinue | Select-Object FullName; Get-ChildItem "C:\Users\kingd\.grok\sessions" -ErrorAction SilentlyContinue | Select-Object -First 5 Name; if (Test-Path "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956") { Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956" -Force }
- description: Find goal plan.md location

### Turn 46 (Function)
[tool_response]
1→#ifndef VICE_SHIM_H
#define VICE_SHIM_H

#include <stdint.h>

#ifdef _WIN32
#define VICE_SHIM_API __declspec(dllexport)
#else
#define VICE_SHIM_API
10→#endif

#ifdef __cplusplus
extern "C" {
#endif

// Machine lifecycle
VICE_SHIM_API void* vice_machine_create();
VICE_SHIM_API void* vice_machine_create_model(const char* model);
VICE_SHIM_API void vice_machine_destroy(void* machine);
20→VICE_SHIM_API void vice_machine_reset(void* machine);
VICE_SHIM_API void vice_machine_step_cycle(void* machine);
VICE_SHIM_API int vice_machine_attach_cartridge(void* machine, const uint8_t* image, int length, int mapping_mode);
VICE_SHIM_API int vice_machine_attach_disk(void* machine, unsigned int unit, unsigned int drive, const char* path);
VICE_SHIM_API int vice_machine_detach_disk(void* machine, unsigned int unit, unsigned int drive);
VICE_SHIM_API uint8_t vice_machine_peek_ram(void* machine, uint16_t address);
VICE_SHIM_API uint8_t vice_machine_read(void* machine, uint16_t address);
VICE_SHIM_API void vice_machine_write(void* machine, uint16_t address, uint8_t value);
VICE_SHIM_API int vice_machine_get_model(void* machine);
VICE_SHIM_API int vice_machine_set_keyboard_matrix_key(void* machine, int row, int column, int pressed);
30→VICE_SHIM_API void vice_machine_cia1_store(void* machine, uint8_t register_index, uint8_t value);
VICE_SHIM_API uint8_t vice_machine_cia1_read(void* machine, uint8_t register_index);

// Snapshot (VICE .vsf) load/save against the active machine. Returns 0 on
// success; negative on shim-level error (-1 bad arg, -2 not active machine);
// otherwise the underlying VICE machine_read/write_snapshot return value.
VICE_SHIM_API int vice_machine_read_snapshot(void* machine, const char* path);
VICE_SHIM_API int vice_machine_write_snapshot(void* machine, const char* path);
// Last VICE snapshot error code (SNAPSHOT_* enum from snapshot.h); 0 = no error.
VICE_SHIM_API int vice_snapshot_last_error(void);
40→
// CPU State
VICE_SHIM_API uint8_t vice_cpu_get_a(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_x(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_y(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_p(void* machine);
VICE_SHIM_API uint8_t vice_cpu_get_sp(void* machine);
VICE_SHIM_API uint16_t vice_cpu_get_pc(void* machine);

// CPU resume/pipeline state (TR-LOCKSTEP-VSF-001): the .vsf-restored in-flight
50→// context of the x64sc main CPU right after vice_machine_read_snapshot, i.e.
// everything the MAINCPU module (mainc64cpu.c maincpu_snapshot_read_module)
// and the C64MEM module (c64memsnapshot.c pport block) carry beyond the plain
// register file: the last-opcode interrupt context, the pending BA-low stall
// flags, the 6510 processor port (written data/dir plus the effective read
// values that select ROM/IO banking), and the interrupt-status clocks. Field
// order and 1-byte packing mirror ViceSharp.Core.ViceNative.ViceCpuPipelineState
// byte for byte.
#pragma pack(push, 1)
struct vice_cpu_pipeline_state {
60→    uint64_t clk;                /* maincpu_clk at export */
    uint32_t last_opcode_info;   /* vsf MAINCPU: last opcode + IRQ delay/enable flags */
    uint32_t ba_low_flags;       /* vsf MAINCPU: maincpu_ba_low_flags (pending BA stall) */
    uint8_t  pport_data;         /* vsf C64MEM: pport.data ($01 written value) */
    uint8_t  pport_dir;          /* vsf C64MEM: pport.dir ($00 written value) */
    uint8_t  pport_data_read;    /* effective $01 read value (pull-ups applied) */
    uint8_t  pport_dir_read;     /* effective $00 read value */
    uint32_t global_pending_int; /* vsf MAINCPU interrupt module: pending IK_* mask */
    uint64_t irq_clk;            /* interrupt-status clocks (CLOCK) */
    uint64_t nmi_clk;
70→    uint64_t irq_delay_cycles;
    uint64_t nmi_delay_cycles;
};
#pragma pack(pop)

VICE_SHIM_API void vice_cpu_get_pipeline_state(void* machine, struct vice_cpu_pipeline_state* state);

// Drive CPU State (1541-family, unit = 8..11 device number)
VICE_SHIM_API uint8_t vice_drivecpu_get_a(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_x(void* machine, unsigned int unit);
80→VICE_SHIM_API uint8_t vice_drivecpu_get_y(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_p(void* machine, unsigned int unit);
VICE_SHIM_API uint8_t vice_drivecpu_get_sp(void* machine, unsigned int unit);
VICE_SHIM_API uint16_t vice_drivecpu_get_pc(void* machine, unsigned int unit);

// Drive residue observability (TEST-NATIVE-RESIDUE-03/04). Test-only readouts of
// the drive-side clock state that survives a machine boundary. Clock residue
// returns 0 on success (out-params written) or -1 on invalid machine/unit/slot
// (out-params zeroed); cycle_accum returns 0 on invalid machine/unit.
VICE_SHIM_API int vice_drive_get_clock_residue(void* machine, unsigned int unit, uint64_t* attach_clk, uint64_t* detach_clk, uint64_t* attach_detach_clk);
90→VICE_SHIM_API uint64_t vice_drivecpu_get_cycle_accum(void* machine, unsigned int unit);
VICE_SHIM_API int vice_drivecpu_set_cycle_accum(void* machine, unsigned int unit, uint64_t value);

// Per-unit true-drive emulation toggle. Returns 0 on success, non-zero on failure.
VICE_SHIM_API int vice_drive_set_true_emulation(void* machine, unsigned int unit, int enabled);
VICE_SHIM_API int vice_drive_get_true_emulation(void* machine, unsigned int unit);

// VIC-II State
struct vice_vic_state {
    uint32_t cycle;
100→    uint16_t raster_line;
    uint8_t raster_cycle;
    uint8_t bad_line;
    uint8_t display_state;
    uint8_t sprite_dma;
    uint8_t registers[64];
    /* TR-LOCKSTEP-VSF-001: .vsf VIC-II module resume context beyond the register
       file (viciisc/vicii-snapshot.c saves both). allow_bad_lines is the DEN
       seen-at-line-$30 latch that gates every badline (and therefore every
       BA-low CPU stall) for the rest of the frame; idle_state is the
110→       display/idle g-access state. Both are required to stage a managed VIC
       from a mid-frame snapshot. */
    uint8_t allow_bad_lines;
    uint8_t idle_state;
    /* Register file through vicii_peek() (vicii-mem.c:747-770): the CPU-visible
       debug view (unused-bit OR table, live raster in $D011/$D012, irq_status
       in $D019). `registers` above stays the RAW vicii.regs store - snapshot
       and reset-state parity tests compare raw-vs-raw, while the register
       CHECKPOINT tests compare managed Peek against this peek view. Appended
       so existing field offsets are unchanged. */
120→    uint8_t registers_peek[64];
};

VICE_SHIM_API void vice_vic_get_state(void* machine, struct vice_vic_state* state);

// Visible frame capture (for native visible raster/pixel checkpoints under display-mode effects, e.g. invalid ECM COL_NONE black per vicii-draw-cycle.c:133-141 etc.).
// Returns non-zero on success; fills buffer with BGRA (320x200 recommended). Authentic from native VICE vicii state.
VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height);

// Per-pixel VIC oracle (PLAN-VICEPARITY-001 Phase 0 / TR-VIC-ORACLE-001): copy
130→// the visible frame window from the viciisc raster draw buffer as raw VICE
// palette indices, one byte per pixel (0x00-0x0F). vicii-draw-cycle.c writes
// these indices 8 per cycle into vicii.dbuf; each line is flushed into the
// canvas draw buffer by vicii_raster_draw_handler. Index-exact comparison is
// palette-independent, which is what the VIC parity ACs assert.
VICE_SHIM_API int vice_vic_capture_frame_indices(void* machine, uint8_t* buffer, int length, int* width, int* height);

// Line pixel+pri snapshot at raster/draw boundary (for TR-VIC-EDGE-001 native ECM reinforcement, BACKFILL-VIDEO-001).
// Returns non-zero on success; fills pri_buffer (0/1 per pixel) for the line using native vicii state (gbuf/pri logic per vicii-draw-cycle.c:196/224).
// Checkpointed authentic pri_buffer for invalid ECM (pri preserved on COL_NONE pixels).
140→VICE_SHIM_API int vice_vic_get_graphics_priority_at_raster(void* machine, uint16_t raster_line, uint8_t* pri_buffer, int length);

// CIA State
struct vice_cia_state {
    uint8_t port_a;
    uint8_t port_b;
    uint8_t ddr_a;
    uint8_t ddr_b;
    uint16_t timer_a;
    uint16_t timer_b;
150→    uint8_t icr;
    uint8_t cra;
    uint8_t crb;
    uint8_t interrupt_flag;
    /* TR-LOCKSTEP-VSF-001: .vsf CIA resume context beyond the live counters -
       the timer latches (ciat_read_latch; reload values on underflow) and the
       ICR interrupt-enable mask (cia_context->irq_enabled, saved by the CIA
       snapshot module). Required to stage a managed CIA from a snapshot. */
    uint16_t timer_a_latch;
    uint16_t timer_b_latch;
160→    uint8_t irq_mask;
    uint8_t reserved;
};

VICE_SHIM_API void vice_cia_get_state(void* machine, int cia_index, struct vice_cia_state* state);

// SID State
struct vice_sid_state {
    uint8_t registers[32];
    uint32_t accumulators[3];
170→    uint8_t envelopes[3];
    uint32_t filter_state;
};

VICE_SHIM_API void vice_sid_get_state(void* machine, struct vice_sid_state* state);

// Render N audio samples from the active SID into buffer. Returns the count
// actually rendered as signed 16-bit PCM. delta_t_cycles is the host-cycle
// budget per sample request (e.g. 22 for ~44.1kHz at C64 PAL 985248Hz).
// The renderer is reference-tolerant: it uses the configured SID engine
180→// (fastsid or reSID) with the current siddata register state. It does not
// require sound_open() to have succeeded.
VICE_SHIM_API size_t vice_sid_render_samples(void* machine, int16_t* buffer, size_t n, int delta_t_cycles);

// Read a register straight from the shim's private, clocked reSID instance.
// Returns engine-computed values such as OSC3 ($1b) and ENV3 ($1c) that only
// exist once reSID has been clocked via vice_sid_render_samples (the headless
// main SID #0 read by vice_sid_get_state is never clocked). addr is 0x00-0x1f.
VICE_SHIM_API uint8_t vice_sid_engine_read(void* machine, uint16_t addr);

190→// Clock the shim's private reSID instance by exactly `cycles` CPU cycles using a
// 1:1 (sample_rate == cpu_clock) renderer, so its internal ADSR/accumulator state
// advances cycle-exactly - unlike vice_sid_render_samples, whose 44100 Hz
// resampling advances ~22.34 cycles per sample and cannot be matched to an exact
// cycle count. Syncs the current SID registers first. Use this for cycle-exact
// SID lockstep against the managed Sid6581.
VICE_SHIM_API void vice_sid_clock(void* machine, int cycles);

// Single-cycle reSID oracle (PLAN-VICEPARITY-001 Phase 0 / TR-SID-ORACLE-001).
// vice_sid_clock batches through reSID's clock(delta_t), which drops the
200→// single-cycle envelope/waveform pipelines; the exact API drives
// reSID::SID::clock() one cycle at a time so managed parity tests can assert
// bit-exact equality. vice_sid_exact_open force-recreates the shim's private
// engine (no batched history) and syncs the machine's SID register file once;
// afterwards drive writes ONLY through vice_sid_exact_write (a re-sync would
// clobber pipeline state).

// Full reSID internal state export. Field order and packing (1-byte) mirror
// ViceSharp.Core.ViceNative.ViceSidExactState byte for byte.
#pragma pack(push, 1)
210→struct vice_sid_exact_state {
    uint8_t  registers[32];
    uint32_t accumulator[3];
    uint32_t shift_register[3];
    uint32_t shift_register_reset[3];
    uint32_t shift_pipeline[3];
    uint32_t floating_output_ttl[3];
    uint16_t pulse_output[3];
    uint16_t rate_counter[3];
    uint16_t rate_counter_period[3];
220→    uint16_t exponential_counter[3];
    uint16_t exponential_counter_period[3];
    uint8_t  envelope_counter[3];
    uint8_t  envelope_state[3];
    uint8_t  hold_zero[3];
    uint8_t  envelope_pipeline[3];
    uint8_t  bus_value;
    uint32_t bus_value_ttl;
    uint8_t  write_pipeline;
    uint8_t  write_address;
230→    uint8_t  voice_mask;
    // VICE-Sharp S9 parity probe: reSID internal filter state.
    // [0]=Vlp [1]=Vbp [2]=Vhp [3]=v1 [4]=v2 [5]=v3 [6]=sum [7]=mix
    // [8]=filter.output() [9]=Vlp_x [10]=Vlp_vc [11]=Vbp_x [12]=Vbp_vc [13]=Vddt_Vw_2
    int32_t  filter_probe[9];
};
#pragma pack(pop)

VICE_SHIM_API int vice_sid_exact_open(void* machine);
VICE_SHIM_API void vice_sid_exact_reset(void* machine);
240→VICE_SHIM_API int vice_sid_exact_clock(void* machine, int cycles);
VICE_SHIM_API void vice_sid_exact_write(void* machine, uint16_t addr, uint8_t value);
VICE_SHIM_API uint8_t vice_sid_exact_read(void* machine, uint16_t addr);
VICE_SHIM_API int16_t vice_sid_exact_output(void* machine);
VICE_SHIM_API void vice_sid_exact_get_state(void* machine, struct vice_sid_exact_state* state);
// Reconfigure the exact oracle's reSID sampling method (0=FAST 1=INTERPOLATE
// 2=RESAMPLE 3=RESAMPLE_FASTMEM); clock_freq fixed at 985248.0. Returns 1 ok.
VICE_SHIM_API int vice_sid_exact_set_sampling(void* machine, int method, double sample_freq, double pass_freq, double filter_scale);
// Buffered clock path: clock up to `cycles`, write up to buffer_len samples,
// return sample count; *cycles_remaining gets the unconsumed remainder.
250→VICE_SHIM_API int vice_sid_exact_clock_buffered(void* machine, int cycles, int16_t* buffer, int buffer_len, int* cycles_remaining);

struct vice_interrupt_state {
    uint8_t irq_asserted;
    uint8_t nmi_asserted;
    uint8_t global_pending;
    uint8_t irq_source_count;
    uint8_t nmi_source_count;
};

260→VICE_SHIM_API void vice_interrupt_get_state(void* machine, struct vice_interrupt_state* state);

#ifdef __cplusplus
}
#endif

#endif


### Turn 47 (Function)
[tool_response]
750→#endif

    /*
     * Enable maincpu_resync_limits functionality .. in the old code
     * this is where the local stack var had its address copied to
     * the global.
     */
    bank_base_ready = true;

#ifdef VICE_SHIM_HOSTED
760→    /* Hosted build (ViceSharp): the host bootstraps the CPU explicitly. When a
       bootstrap is pending, adopt the host-provided register file and clear the
       per-run CPU bookkeeping instead of triggering the normal power-on reset.
       IMPORT_REGISTERS() is not yet defined at this point in the translation
       unit (6510dtvcore.c defines it below, inside the loop), so the import is
       inlined here. reg_a_write/reg_a_read are #defined to reg_a in the
       non-DTV core, so the locals below are the CPU's real register storage. */
    if (vice_shim_take_bootstrap_maincpu()) {
        reg_a = maincpu_regs.a;
        reg_x = maincpu_regs.x;
770→        reg_y = maincpu_regs.y;
        reg_sp = maincpu_regs.sp;
        reg_p = maincpu_regs.p;
        flag_n = maincpu_regs.n;
        flag_z = maincpu_regs.z;
        bank_start = bank_limit = 0; /* prevent caching */
        JUMP(maincpu_regs.pc);
        maincpu_jammed = 0;
        last_opcode_info = 0;
        last_opcode_addr = reg_pc;
780→        stolen_cycles = 0;
        check_ba_low = 0;
        reu_dma_triggered = 0;
        maincpu_rmw_flag = 0;
    } else {
        machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);
    }
#else
    machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);
#endif
790→
    while (1) {
#define CPU_LOG_ID maincpu_log
#define ANE_LOG_LEVEL ane_log_level
#define LXA_LOG_LEVEL lxa_log_level
#define CPU_IS_JAMMED maincpu_jammed
#define CLK maincpu_clk
#define RMW_FLAG maincpu_rmw_flag
#define LAST_OPCODE_INFO last_opcode_info
#define LAST_OPCODE_ADDR last_opcode_addr
800→#define TRACEFLG debug.maincpu_traceflg

#define CPU_INT_STATUS maincpu_int_status

#define ALARM_CONTEXT maincpu_alarm_context

#define CHECK_PENDING_ALARM() (clk >= next_alarm_clk(maincpu_int_status))

#define CHECK_PENDING_INTERRUPT() check_pending_interrupt(maincpu_int_status)

810→#define TRAP(addr) maincpu_int_status->trap_func(addr);

#define ROM_TRAP_HANDLER() traps_handler()

#define JAM()                                                         \
    do {                                                              \
        unsigned int tmp;                                             \
                                                                      \
        EXPORT_REGISTERS();                                           \
        tmp = machine_jam("   " CPU_STR ": JAM at $%04X   ", reg_pc); \
820→        switch (tmp) {                                                \
            case JAM_RESET_CPU:                                       \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_POWER_CYCLE:                                     \
                machine_powerup();                                    \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_MONITOR:                                         \
                monitor_startup(e_comp_space);                        \
830→                IMPORT_REGISTERS();                                   \
                break;                                                \
            default:                                                  \
                CLK_INC();                                            \
        }                                                             \
    } while (0)

#define CALLER e_comp_space

#define ROM_TRAP_ALLOWED() mem_rom_trap_allowed((uint16_t)reg_pc)
840→
#define GLOBAL_REGS maincpu_regs

#include "6510dtvcore.c"

        maincpu_int_status->num_dma_per_opcode = 0;

        if (maincpu_clk_limit && (maincpu_clk > maincpu_clk_limit)) {
            log_error(maincpu_log, "cycle limit reached.");
            archdep_vice_exit(EXIT_FAILURE);

### Turn 48 (Function)
[tool_response]
1→#include "vice-shim.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>

#include <ctype.h>
#include <stdbool.h>
#include <stddef.h>
10→#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "archdep.h"
#include "archdep_set_openmp_wait_policy.h"
#include "archdep_startup_log_error.h"
#include "archdep_tick.h"
#include "attach.h"
20→#include "cia.h"
#include "core/ciatimer.h"
#include "drive.h"
#include "drivetypes.h"
#include "gfxoutput.h"
#include "init.h"
#include "interrupt.h"
#include "lib.h"
#include "log.h"
#include "machine.h"
30→#include "maincpu.h"
#include "main.h"
#include "mem.h"
#include "mos6510.h"
#include "resources.h"
#include "screenshot.h"
#include "sid.h"
#include "snapshot.h"
#include "sid/sid-snapshot.h"
#include "sysfile.h"
40→#include "uiapi.h"
#include "video.h"
#include "c64/c64.h"
#include "c64/c64cia.h"
#include "c64/c64model.h"
#include "c64/c64pla.h"
#include "cartridge.h"
#include "keyboard.h"
#include "vicii.h"
#include "viciisc/vicii-mem.h"
50→#include "vice-shim-runtime.h"
#include "viciisc/viciitypes.h"
#include "palette.h"
#include "videoarch.h" /* headless video_canvas_s for the frame oracle */

int archdep_init(int *argc, char **argv);

typedef struct vice_machine_s {
    uint32_t magic;
    int c64_model;
60→    int cartridge_attached;
    int cartridge_mapping_mode;
    int cartridge_length;
    uint8_t cartridge_image[0x80000];
} vice_machine_t;

enum {
    VICE_MACHINE_MAGIC = 0x56494345u,
    VICE_SHIM_CART_MAPPING_AUTO = 0,
    VICE_SHIM_CART_MAPPING_STANDARD_8K = 1,
70→    VICE_SHIM_CART_MAPPING_STANDARD_16K = 2,
    VICE_SHIM_CART_MAPPING_ULTIMAX = 3,
    VICE_SHIM_CART_MAPPING_GAME_SYSTEM = 4,
    VICE_SHIM_CART_BANK_SIZE = 0x2000,
    VICE_SHIM_CART_GENERIC_IMAGE_LIMIT = 0x4000,
    VICE_SHIM_CART_GAME_SYSTEM_IMAGE_SIZE = 0x80000,
    VICE_SHIM_CART_IMAGE_LIMIT = 0x80000
};

static INIT_ONCE g_sync_once = INIT_ONCE_STATIC_INIT;
80→static CRITICAL_SECTION g_state_lock;
static CONDITION_VARIABLE g_state_cv;

static vice_machine_t *g_active_machine;
static HANDLE g_worker_thread;
static int g_worker_running;
static int g_runtime_initialized;
static int g_runtime_failed;
static int g_bootstrap_pending;
static int g_stop_requested;
90→static unsigned int g_granted_cycles;
static int g_cycle_paused;
static unsigned int g_debug_step_calls;
static unsigned int g_debug_checkpoint_calls;
static unsigned int g_debug_create_calls;
static unsigned int g_debug_reset_calls;
#define VICE_SHIM_CREATE_TIMEOUT_MS 5000u
#define VICE_SHIM_STEP_TIMEOUT_MS 5000u
#define VICE_SHIM_STOP_TIMEOUT_MS 2000u

100→extern CLOCK stolen_cycles;
extern int check_ba_low;
extern int maincpu_ba_low_flags;
extern vicii_t vicii;

static int vice_shim_model_from_selector(const char *selector);
static int vice_shim_selector_equals(const char *left, const char *right);
static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive);
static int vice_shim_apply_cartridge_locked(const vice_machine_t *instance);
static int vice_shim_cartridge_type_for(const vice_machine_t *instance, int length, int mapping_mode);
110→static void vice_shim_detach_cartridge_locked(void);
static int vice_shim_write_temp_file(const uint8_t *data, int length, char *path, size_t path_size);
static uint16_t vice_shim_read_cia_timer(cia_context_t *cia, ciat_t *timer);
static void vice_shim_set_current_thread_description(const wchar_t *description);

static BOOL CALLBACK vice_shim_initialize_sync(PINIT_ONCE init_once, PVOID parameter, PVOID *context)
{
    (void)init_once;
    (void)parameter;
    (void)context;
120→
    InitializeCriticalSection(&g_state_lock);
    InitializeConditionVariable(&g_state_cv);
    return TRUE;
}

static void vice_shim_ensure_sync_primitives(void)
{
    InitOnceExecuteOnce(&g_sync_once, vice_shim_initialize_sync, NULL, NULL);
}
130→
static int vice_shim_is_active_machine(const void *machine)
{
    const vice_machine_t *instance = (const vice_machine_t *)machine;
    return instance != NULL
        && instance->magic == VICE_MACHINE_MAGIC
        && instance == g_active_machine;
}

static uint16_t vice_shim_read_cia_timer(cia_context_t *cia, ciat_t *timer)
140→{
    CLOCK cclk;

    if (cia == NULL || timer == NULL) {
        return 0;
    }

    cclk = cia->clk_ptr != NULL ? *(cia->clk_ptr) : maincpu_clk;
    ciat_update(timer, cclk);
    return ciat_read_timer(timer, cclk);
150→}

static int vice_shim_wait_for_signal_with_timeout(unsigned int timeout_ms)
{
    return SleepConditionVariableCS(&g_state_cv, &g_state_lock, (DWORD)timeout_ms) != 0;
}

static int vice_shim_get_module_directory(char *buffer, size_t buffer_size)
{
    HMODULE module = NULL;
160→    DWORD length;
    char *separator;

    if (!GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                            (LPCSTR)&vice_machine_create,
                            &module)) {
        return 0;
    }

    length = GetModuleFileNameA(module, buffer, (DWORD)buffer_size);
170→    if (length == 0 || length >= buffer_size) {
        return 0;
    }

    separator = strrchr(buffer, '\\');
    if (separator == NULL) {
        separator = strrchr(buffer, '/');
    }

    if (separator == NULL) {
180→        return 0;
    }

    *separator = '\0';
    return 1;
}

static int vice_shim_compose_path(char *buffer,
                                  size_t buffer_size,
                                  const char *base,
190→                                  const char *relative_path)
{
    int written = snprintf(buffer, buffer_size, "%s\\%s", base, relative_path);
    return written > 0 && (size_t)written < buffer_size;
}

static int vice_shim_initialize_runtime_locked(void)
{
    char module_directory[MAX_PATH];
    char program_path[MAX_PATH];
200→    char data_directory[MAX_PATH];

### Turn 49 (Function)
[tool_response]
1→using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

10→/// <summary>
/// Native VICE library interop binding
/// </summary>
public static unsafe partial class ViceNative
{
    private const string LibraryName = "vice_x64";
    private static readonly string[] AlternateLibraryNames = ["vice_x64", "vice-shim"];
    private static readonly string[] RelativeSearchDirectories =
    [
        "",
20→        "native",
        Path.Combine("runtimes", "win-x64", "native"),
        Path.Combine("runtimes", "linux-x64", "native"),
        Path.Combine("runtimes", "osx-x64", "native"),
        Path.Combine("runtimes", "osx-arm64", "native")
    ];

    private static readonly Lazy<string?> ResolvedLibraryPath = new(FindLibraryPath);

    static ViceNative()
30→    {
        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibrary);
    }

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;
    public static string AvailabilityMessage => ResolvedLibraryPath.Value is { } path
        ? $"Native VICE library resolved at '{path}'."
        : "Native VICE library not found. Build or copy 'vice_x64'/'vice-shim' into the test output or a searched native directory.";

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create")]
40→    public static partial IntPtr Create();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create_model", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr CreateModel(string modelSelector);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_destroy")]
    public static partial void Destroy(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_reset")]
    public static partial void ResetNative(IntPtr instance);
50→
    [LibraryImport(LibraryName, EntryPoint = "vice_machine_step_cycle")]
    public static partial void StepNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_read_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int ReadSnapshotNative(IntPtr instance, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_write_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WriteSnapshotNative(IntPtr instance, string path);

60→    [LibraryImport(LibraryName, EntryPoint = "vice_snapshot_last_error")]
    public static partial int SnapshotLastError();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_cartridge")]
    private static partial int AttachCartridgeNative(IntPtr instance, byte* image, int length, int mappingMode);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_disk", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int AttachDiskNative(IntPtr instance, uint unit, uint drive, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_detach_disk")]
70→    private static partial int DetachDiskNative(IntPtr instance, uint unit, uint drive);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_ram")]
    private static partial byte PeekRamNative(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_read")]
    public static partial byte ReadMemory(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_write")]
    public static partial void WriteMemory(IntPtr instance, ushort address, byte value);
80→
    [LibraryImport(LibraryName, EntryPoint = "vice_machine_get_model")]
    public static partial int GetModel(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_set_keyboard_matrix_key")]
    private static partial int SetKeyboardMatrixKeyNative(IntPtr instance, int row, int column, int pressed);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_cia1_store")]
    public static partial void StoreCia1Register(IntPtr instance, byte registerIndex, byte value);

90→    [LibraryImport(LibraryName, EntryPoint = "vice_machine_cia1_read")]
    public static partial byte ReadCia1Register(IntPtr instance, byte registerIndex);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_a")]
    public static partial byte GetA(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_x")]
    public static partial byte GetX(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_y")]
100→    public static partial byte GetY(IntPtr instance);

### Turn 50 (Function)
[tool_response]
500→        public byte NmiSourceCount;
    }

    /// <summary>
    /// TR-LOCKSTEP-VSF-001: x64sc main-CPU resume/pipeline state, mirroring
    /// <c>struct vice_cpu_pipeline_state</c> in native/vice-shim.h byte for byte
    /// (1-byte packing). Carries the .vsf-restored in-flight context beyond the
    /// plain register file: the MAINCPU module's last_opcode_info and BA-low
    /// stall flags (mainc64cpu.c maincpu_snapshot_read_module), the C64MEM
    /// module's 6510 processor port (c64memsnapshot.c; dir/data written values
510→    /// plus the effective read values that select ROM/IO banking), and the
    /// interrupt-status clocks (interrupt.c snapshot sub-modules).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViceCpuPipelineState
    {
        public ulong Clk;
        public uint LastOpcodeInfo;
        public uint BaLowFlags;
        public byte PportData;
520→        public byte PportDir;
        public byte PportDataRead;
        public byte PportDirRead;
        public uint GlobalPendingInt;
        public ulong IrqClk;
        public ulong NmiClk;
        public ulong IrqDelayCycles;
        public ulong NmiDelayCycles;
    }

530→    private static IntPtr ResolveLibrary(string libraryName, Assembly _, DllImportSearchPath? __)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        var path = ResolvedLibraryPath.Value;
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? FindLibraryPath()
540→    {
        foreach (var candidatePath in EnumerateCandidateLibraryPaths())
            return TryCreateRelocatedRuntime(candidatePath, out var relocatedPath) ? relocatedPath : candidatePath;

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateLibraryPaths()
    {
        foreach (var root in EnumerateSearchRoots(AppContext.BaseDirectory))
550→        {
            foreach (var relativeDirectory in RelativeSearchDirectories)
            {
                var candidateDirectory = Path.Combine(root, relativeDirectory);
                foreach (var libraryName in AlternateLibraryNames)
                {
                    foreach (var fileName in GetCandidateFileNames(libraryName))
                    {
                        var candidatePath = Path.Combine(candidateDirectory, fileName);
                        if (File.Exists(candidatePath))
560→                            yield return candidatePath;
                    }
                }
            }
        }
    }

    private static bool TryCreateRelocatedRuntime(string sourceLibraryPath, out string relocatedLibraryPath)
    {
        relocatedLibraryPath = string.Empty;
570→
        if (!ViceDataPathResolver.TryFindDataRoot(out var dataRoot))
            return false;

        try
        {
            var sourceDirectory = Path.GetDirectoryName(sourceLibraryPath);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
                return false;


### Turn 51 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 25 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20-cmdline-options.c
42:#include "vic20model.h"
50:    { "vic20", VIC20MODEL_VIC20_PAL },
51:    { "vic20pal", VIC20MODEL_VIC20_PAL },
52:    { "pal", VIC20MODEL_VIC20_PAL },
53:    { "vic20ntsc", VIC20MODEL_VIC20_NTSC },
54:    { "ntsc", VIC20MODEL_VIC20_NTSC },
55:    { "vic21", VIC20MODEL_VIC21 },
56:    { "vic1001", VIC20MODEL_VIC1001 },
57:    { NULL, VIC20MODEL_UNKNOWN }
62:    int model = VIC20MODEL_UNKNOWN;
74:    } while ((model == VIC20MODEL_UNKNOWN) && (model_match[i].name != NULL));
76:    if (model == VIC20MODEL_UNKNOWN) {
80:    vic20model_set(model);

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20model.h
2: * vic20model.h - VIC20 model detection and setting.
27:#ifndef VICE_VIC20MODEL_H
28:#define VICE_VIC20MODEL_H
32:#define VIC20MODEL_VIC20_PAL        0
33:#define VIC20MODEL_VIC20_NTSC       1
34:#define VIC20MODEL_VIC21            2 /* SuperVIC (+16K) */
35:#define VIC20MODEL_VIC1001          3 /* vic-1001 (japanese VIC20) */
37:#define VIC20MODEL_NUM 4
39:#define VIC20MODEL_UNKNOWN 99
43:int vic20model_get(void);
44:void vic20model_set(int model);

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20model.c
2: * vic20model.c - Plus4 model detection and setting.
</workspace_result>

### Turn 52 (Function)
[tool_response]
<task-id>call-62c67ae1-74f2-43b4-aec9-5a7eef5de6b6-40</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-62c67ae1-74f2-43b4-aec9-5a7eef5de6b6-40.log</output-file>
<status>running</status>
<summary>Command "Get-Content "C:\Users\kingd\AppData\Local\Temp\grok-goal-*\plan.md" -ErrorAction SilentlyContinue | Select-Object -First 5; Get-ChildItem "C:\Users\kingd\AppData\Local\Temp" -Filter "plan.md" -Recurse -Depth 3 -ErrorAction SilentlyContinue | Select-Object FullName; Get-ChildItem "C:\Users\kingd\.grok\sessions" -ErrorAction SilentlyContinue | Select-Object -First 5 Name; if (Test-Path "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956") { Get-ChildItem "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956" -Force }" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-62c67ae1-74f2-43b4-aec9-5a7eef5de6b6-40"] to retrieve the output.

### Turn 53 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- offset: 200
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
- limit: 150
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 550
- limit: 250
[tool_request: grep]
- pattern: vic_cycle|vic_get|struct vic
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*.{c,h}
- head_limit: 40
[tool_request: run_terminal_command]
- command: (Get-Item F:\GitHub\vice-sharp\native\vice-shim.c).Length; (Get-Content F:\GitHub\vice-sharp\native\vice-shim.c | Measure-Object -Line).Lines; Get-ChildItem F:\GitHub\vice-sharp\native\vice\vice\src\xvic.exe | Select-Object Length, LastWriteTime; Test-Path F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20cpu.o
- description: Measure shim size and xvic readiness

### Turn 54 (Function)
[tool_response]
200→    [LibraryImport(LibraryName, EntryPoint = "vice_sid_exact_get_state")]
    public static partial void SidExactGetState(IntPtr instance, ref ViceSidExactState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_exact_set_sampling")]
    public static partial int SidExactSetSampling(IntPtr instance, int method, double sampleFreq, double passFreq, double filterScale);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_exact_clock_buffered")]
    public static partial int SidExactClockBuffered(IntPtr instance, int cycles, [Out] short[] buffer, int bufferLength, out int cyclesRemaining);

    [LibraryImport(LibraryName, EntryPoint = "vice_interrupt_get_state")]
210→    public static partial void GetInterruptState(IntPtr instance, ref ViceInterruptState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_pipeline_state")]
    public static partial void GetCpuPipelineState(IntPtr instance, ref ViceCpuPipelineState state);

    public static IViceNative CreateInstance(string? modelSelector = null)
    {
        if (!IsAvailable)
            throw new DllNotFoundException(AvailabilityMessage);

220→        var handle = string.IsNullOrWhiteSpace(modelSelector)
            ? Create()
            : CreateModel(modelSelector);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        return new ViceNativeInstance(handle);
    }

230→    public static byte GetCpuRegister(IntPtr instance, int registerId)
    {
        return registerId switch
        {
            0 => GetA(instance),
            1 => GetX(instance),
            2 => GetY(instance),
            3 => GetP(instance),
            4 => GetS(instance),
            5 => (byte)(GetPC(instance) & 0xFF),
240→            6 => (byte)(GetPC(instance) >> 8),
            _ => throw new ArgumentOutOfRangeException(nameof(registerId), registerId, "Expected CPU register id 0-6.")
        };
    }

    public static void AttachCartridge(IntPtr instance, ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
    {
        if (image.IsEmpty)
            throw new ArgumentException("Cartridge image must not be empty.", nameof(image));

250→        var imageBytes = image.ToArray();
        fixed (byte* imagePointer = imageBytes)
        {
            var result = AttachCartridgeNative(instance, imagePointer, imageBytes.Length, (int)mappingMode);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE failed to attach cartridge image. Error code: {result}.");
        }
    }

    public static byte PeekRam(IntPtr instance, ushort address) => PeekRamNative(instance, address);
260→
    public static void AttachDisk(IntPtr instance, uint unit, uint drive, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Disk path is required.", nameof(path));

        var result = AttachDiskNative(instance, unit, drive, path);
        if (result != 0)
            throw new InvalidOperationException($"Native VICE failed to attach disk unit {unit}, drive {drive}, path '{path}'. Error code: {result}.");
    }
270→
    public static void DetachDisk(IntPtr instance, uint unit, uint drive)
    {
        var result = DetachDiskNative(instance, unit, drive);
        if (result != 0)
            throw new InvalidOperationException($"Native VICE failed to detach disk unit {unit}, drive {drive}. Error code: {result}.");
    }

    public static void SetKeyboardMatrixKey(IntPtr instance, int row, int column, bool pressed)
    {

### Turn 55 (Function)
[tool_response]
1→using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core.Input;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
10→using ViceSharp.Core.Media;
using ViceSharp.Core.Wiring;

namespace ViceSharp.TestHarness;

/// <summary>
/// Lockstep cycle accurate validator comparing ViceSharp against original VICE
/// </summary>
public sealed class LockstepValidator : IDisposable
{
20→    private const ushort KernalCloseVector = 0xFFC3;
    private static readonly string[] C64Drive8LoadSequence =
    [
        "L", "O", "A", "D", "\"", "*", "\"", ",", "8", ",", "1", "Return"
    ];

    private readonly IMachine _machine;
    private readonly IViceNative _native;
    private readonly SystemCoordinator? _coordinator;
    private readonly Queue<string> _recentTrace = new();
30→    private readonly LockFreePubSub _pubSub = new();
    private readonly bool _recordRecentTrace;
    private ushort _kernalCloseTarget;
    private readonly SubscriptionHandle _cpuControlTransferSubscription;
    private long _cycleCount;
    private long _lastNativeDelta;
    private bool _hasPendingKernalCloseCall;
    private CpuControlTransferEvent _pendingKernalCloseCall;
    private BasicCommandAutomation? _basicCommandAutomation;
    private bool _nativeSawLoadingPrompt;
40→    private long _nextNativeLoadProbeCycle;

    public LockstepValidator(
        string modelSelector = "c64",
        byte[]? cartridgeImage = null,
        CartridgeMappingMode cartridgeMappingMode = CartridgeMappingMode.Auto,
        byte[]? diskImage = null,
        string? diskPath = null,
        byte diskDriveNumber = 8,
        bool recordRecentTrace = true)
50→    {
        if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
            if (diskImage is null)
                throw new ArgumentException("Managed disk image bytes are required when a native disk path is supplied.", nameof(diskImage));
            if (string.IsNullOrWhiteSpace(diskPath))
                throw new ArgumentException("Native disk path is required when managed disk image bytes are supplied.", nameof(diskPath));

            var rig = CreateC64WithTrueDrive(modelSelector, diskPath, diskDriveNumber);
            _machine = rig.Host;
60→            _coordinator = rig.Coordinator;
        }
        else
        {
            _machine = MachineTestFactory.CreateC64Machine(modelSelector);
        }

        _native = ViceNative.CreateInstance(modelSelector);
        _recordRecentTrace = recordRecentTrace;
        _kernalCloseTarget = KernalCloseVector;
70→
        if (_machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu)
        {
            cpu.ConnectPubSub(_pubSub);
            _cpuControlTransferSubscription = _pubSub.Subscribe<CpuControlTransferEvent>(
                CpuControlTransferEvent.Topic,
                OnCpuControlTransfer);
        }

        if (cartridgeImage is not null)
80→        {
            var cartridgePort = _machine.Devices.GetAll<ICartridgePort>().SingleOrDefault()
                ?? throw new InvalidOperationException($"Machine '{modelSelector}' does not expose a cartridge port.");

            cartridgePort.AttachCartridge(cartridgeImage, cartridgeMappingMode);
            _native.AttachCartridge(cartridgeImage, cartridgeMappingMode);
        }

        if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
90→            if (_coordinator is null)
            {
                var drive = _machine.Devices.GetAll<IFloppyDrive>().SingleOrDefault(drive => drive.DriveNumber == diskDriveNumber)
                    ?? throw new InvalidOperationException($"Machine '{modelSelector}' does not expose drive {diskDriveNumber}.");

                drive.InsertDisk(diskImage);
            }

            _native.AttachDisk(diskDriveNumber, 0, diskPath!);
        }
100→    }

    public void QueueC64Drive8LoadCommand(int cyclesPerFrame = 19656)
    {
        QueueC64BasicCommand(C64Drive8LoadSequence, C64HostKeyboardMapper.DefaultFallbackMap, cyclesPerFrame);
    }

    public void QueueC64Drive8LoadCommand(string vkmPath, int cyclesPerFrame)
    {
        QueueC64BasicCommand(C64Drive8LoadSequence, vkmPath, cyclesPerFrame);
110→    }

    public void QueueC64BasicCommand(IReadOnlyList<string> keySequence, string vkmPath, int cyclesPerFrame)
    {
        if (keySequence.Count == 0)
            throw new ArgumentException("The BASIC command key sequence cannot be empty.", nameof(keySequence));
        if (cyclesPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerFrame), cyclesPerFrame, "Cycles per frame must be positive.");

        var parseResult = C64VkmParser.Load(vkmPath);
120→        if (parseResult.HasErrors)
        {
            var errors = parseResult.Diagnostics
                .Where(diagnostic => diagnostic.Severity == C64VkmDiagnosticSeverity.Error)
                .Select(diagnostic => $"{diagnostic.Path}:{diagnostic.LineNumber}: {diagnostic.Message}");
            throw new InvalidOperationException($"VKM file '{vkmPath}' has parse errors: {string.Join("; ", errors)}");
        }

        QueueC64BasicCommand(keySequence, parseResult.KeyboardMap, cyclesPerFrame);
    }
130→
    public void QueueC64BasicCommand(IReadOnlyList<string> keySequence, IKeyboardInputMap keyboardMap, int cyclesPerFrame)
    {
        if (keySequence.Count == 0)
            throw new ArgumentException("The BASIC command key sequence cannot be empty.", nameof(keySequence));
        if (cyclesPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerFrame), cyclesPerFrame, "Cycles per frame must be positive.");

        var mapSelection = _machine.Devices.GetAll<IKeyboardInputMapSelection>().SingleOrDefault()
            ?? throw new InvalidOperationException("The managed machine does not expose keyboard map selection.");
140→        var keyboardInput = _machine.Devices.GetAll<IMachineKeyboardInput>().SingleOrDefault()
            ?? throw new InvalidOperationException("The managed machine does not expose host keyboard input.");

        mapSelection.SelectKeyboardMap(keyboardMap);
        _basicCommandAutomation = new BasicCommandAutomation(
            keySequence,
            keyboardMap,
            keyboardInput,
            _native,
            cyclesPerFrame);
150→    }

### Turn 56 (Function)
[tool_response]
550→        || vice_shim_selector_equals(selector, "sx64-ntsc")) {
        return C64MODEL_C64SX_NTSC;
    }

    if (vice_shim_selector_equals(selector, "pet64pal")
        || vice_shim_selector_equals(selector, "pet64")
        || vice_shim_selector_equals(selector, "pet64-pal")) {
        return C64MODEL_PET64_PAL;
    }

560→    if (vice_shim_selector_equals(selector, "pet64ntsc")
        || vice_shim_selector_equals(selector, "pet64-ntsc")) {
        return C64MODEL_PET64_NTSC;
    }

    if (vice_shim_selector_equals(selector, "ultimax") || vice_shim_selector_equals(selector, "max")) {
        return C64MODEL_ULTIMAX;
    }

    if (vice_shim_selector_equals(selector, "c64gs") || vice_shim_selector_equals(selector, "gs")) {
570→        return C64MODEL_C64_GS;
    }

    if (vice_shim_selector_equals(selector, "c64jap") || vice_shim_selector_equals(selector, "jap")) {
        return C64MODEL_C64_JAP;
    }

    return C64MODEL_UNKNOWN;
}

580→static int vice_shim_selector_equals(const char *left, const char *right)
{
    if (left == NULL || right == NULL) {
        return 0;
    }

    while (*left != '\0' && *right != '\0') {
        if (tolower((unsigned char)*left) != tolower((unsigned char)*right)) {
            return 0;
        }
590→
        left++;
        right++;
    }

    return *left == '\0' && *right == '\0';
}

/* Forward-declared so destroy can close the private SID oracle renderer; the
   definition and the reset-path caller live further down. */
600→static void vice_shim_reset_sid_renderer_locked(void);

VICE_SHIM_API void vice_machine_destroy(void *machine)
{
    if (g_debug_reset_calls < 8) {
        fprintf(stderr, "vice_machine_destroy machine=%p\\n", machine);
        fflush(stderr);
    }

    vice_machine_t *instance = (vice_machine_t *)machine;
610→
    if (instance == NULL) {
        return;
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
620→    if (g_active_machine == instance) {
        vice_shim_detach_cartridge_locked();
        file_system_detach_disk_all();
        /* Close the private reSID oracle renderer (g_shim_sid_psid) at the
           machine boundary, symmetric with vice_machine_reset. SidExact* tests
           open it without a Reset, so without this it leaks past DestroyMachine
           into the next machine. Hygiene; no observable cross-machine surface. */
        vice_shim_reset_sid_renderer_locked();
        g_active_machine = NULL;
        g_bootstrap_pending = 0;
630→        WakeAllConditionVariable(&g_state_cv);
    }
    LeaveCriticalSection(&g_state_lock);

    instance->magic = 0;
    free(instance);
}

VICE_SHIM_API void vice_machine_reset(void *machine)
{
640→    if (g_debug_reset_calls < 8) {
        fprintf(stderr, "vice_machine_reset call=%u machine=%p\\n", ++g_debug_reset_calls, machine);
        fflush(stderr);
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
650→    vice_machine_t *instance = (vice_machine_t *)machine;
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    c64model_set(instance->c64_model);
    if (!instance->cartridge_attached) {
        vice_shim_detach_cartridge_locked();
    }
660→
    machine_powerup();
    mem_powerup();
    if (instance->cartridge_attached) {
        vice_shim_apply_cartridge_locked(instance);
    } else {
        vice_shim_detach_cartridge_locked();
    }
    /* vicii_powerup() resets all internal VIC state fields that vicii_reset()
       skips: allow_bad_lines, bad_line, idle_state, vcbase, vc, irq_status,
670→       raster_irq_line, sprite_sprite_collisions, sprite_background_collisions.
       It also calls vicii_reset() at the end, setting raster_cycle=6.
       Without this, stale allow_bad_lines=1 and bad_line=1 from a prior test
       cause check_badline() to assert BA-low for ~40 cycles at raster_line=0,
       producing vicii_steal_cycles() loops that advance raster_cycle without
       checkpoints and diverge managed vs native by 43 cycles. */
    vicii_powerup();
    /* vicii_powerup() does NOT call vicii_new_sprites_init(), so sprite_dma
       bits from the prior test persist. vicii_check_sprite_ba() returns 1 for
       any set bit, adding more BA-low steal cycles. Clear explicitly. */
680→    vicii.sprite_dma = 0;
    vicii.sprite_display_bits = 0;
    vicii_reset_registers();
    /* Clear BA-low flags before maincpu_reset so check_ba() does not
       call maincpu_steal_cycles() and advance raster_cycle 43 extra
       ticks before the first real CLK_INC checkpoint. On real hardware
       all chips reset simultaneously; leftover BA state from the prior
       run must not carry into the reset sequence. */
    maincpu_ba_low_flags = 0;
    maincpu_reset();
690→    /* Clear any IRQ status bits that vicii_cycle() may have set during
       the CPU reset vector fetch sequence. On real hardware all chips
       reset simultaneously; in emulation the CPU reset runs a few VIC
       cycles before it halts, which can latch the raster IRQ at line 0.
       Force irq_status back to 0 so the post-reset snapshot matches
       managed Reset() which clears _registers[0x19] unconditionally. */
    vicii.irq_status = 0;
    vice_shim_reset_cpu_state_locked();
    /* g_shim_sid_psid (the SID clock/render instance) is a global that outlives
       individual machine instances; reset it so vice_sid_clock starts each
700→       machine from a fresh reSID. Otherwise envelope/accumulator state leaks
       across tests/sessions and makes SID lockstep order-dependent. */
    vice_shim_reset_sid_renderer_locked();
    LeaveCriticalSection(&g_state_lock);
}

/* Peek the VIC-II model byte (first byte of the "VIC-II" module data) from a
   snapshot without disturbing machine state. vicii_snapshot_read_module rejects
   a snapshot whose model differs from the configured VICIIModel
   (SNAPSHOT_VICII_MODEL_MISMATCH). An externally-staged x64sc .vsf may use a
710→   different VIC-II revision than the shim's default (e.g. 8565 vs the C64_PAL
   default 6569 - both PAL); aligning VICIIModel to the snapshot lets it resume
   without forcing a full c64model change (which would also alter the SID type).
   Returns the model byte, or -1 if it cannot be read. */
static int vice_shim_peek_snapshot_vicii_model(const char *path)
{
    uint8_t snap_major, snap_minor, mod_major, mod_minor, model;
    snapshot_t *s;
    snapshot_module_t *m;

720→    s = snapshot_open(path, &snap_major, &snap_minor, machine_get_name());
    if (s == NULL) {
        return -1;
    }
    m = snapshot_module_open(s, "VIC-II", &mod_major, &mod_minor);
    if (m == NULL) {
        snapshot_close(s);
        return -1;
    }
    if (snapshot_module_read_byte(m, &model) < 0) {
730→        snapshot_module_close(m);
        snapshot_close(s);
        return -1;
    }
    snapshot_module_close(m);
    snapshot_close(s);
    return (int)model;
}

VICE_SHIM_API int vice_machine_read_snapshot(void *machine, const char *path)
740→{
    if (path == NULL) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    vice_machine_t *instance = (vice_machine_t *)machine;
750→    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    /* Select the model so banked ROM/PLA config matches the snapshot's machine,
       then let VICE restore CPU/mem/VIC/CIA/SID state directly from the .vsf
       into maincpu_regs and the chip globals. Do NOT power up: that would wipe
       the loaded state.

760→       Set bootstrap_pending = 1 (NOT 0). When the worker re-enters
       maincpu_mainloop on the next step, vice_shim_take_bootstrap_maincpu()
       must return 1 so it IMPORT_REGISTERS() the snapshot-restored maincpu_regs
       into the hosted CPU and clears only the micro-op state. Returning 0 makes
       the mainloop fall back to machine_trigger_reset(), which zeroes the
       architectural registers - i.e. it would discard the resumed state. */
    c64model_set(instance->c64_model);
    {
        /* Align VICIIModel to the snapshot so vicii_snapshot_read_module does
           not reject an externally-staged .vsf with a different VIC-II
770→           revision. Both 6569 and 8565 are PAL, so this preserves timing. */
        int snap_vicii_model = vice_shim_peek_snapshot_vicii_model(path);
        if (snap_vicii_model >= 0) {
            resources_set_int("VICIIModel", snap_vicii_model);
        }
    }
    int result = machine_read_snapshot(path, 0);
    if (result == 0) {
        /* A successful machine_read_snapshot still probes optional modules that
           are absent from a plain state snapshot, leaving a non-fatal
780→           SNAPSHOT_MODULE_* residue in the global error. Normalise it so a
           successful resume reports SNAPSHOT_NO_ERROR. */
        snapshot_set_error(SNAPSHOT_NO_ERROR);
    }
    g_bootstrap_pending = 1;

    LeaveCriticalSection(&g_state_lock);
    return result;
}

790→VICE_SHIM_API int vice_machine_write_snapshot(void *machine, const char *path)
{
    if (path == NULL) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);

### Turn 57 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 37 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-cycle.h
30:void vic_cycle(void);

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-cycle.c
55:static inline void vic_cycle_close_v(void)
67:static inline void vic_cycle_open_v(void)
74:        vic_cycle_close_v();
85:static inline void vic_cycle_open_h(void)
115:static inline void vic_cycle_close_h(void)
121:static inline void vic_cycle_start_fetch(void)
144:        vic_cycle_close_h();
149:static inline void vic_cycle_end_of_line(void)
166:static inline void vic_cycle_end_of_frame(void)
170:        vic_cycle_close_v();
188:static inline void vic_cycle_handle_memptr(void)
199:            vic_cycle_close_v();
208:static inline void vic_cycle_latch_columns(void)
214:static inline void vic_cycle_latch_rows(void)
226:static inline uint8_t vic_cycle_do_fetch(int addr, uint8_t *color)
261:static inline void vic_cycle_no_fetch(void)
267:static inline int vic_cycle_fix_addr(int addr)
274:static inline void vic_cycle_fetch(void)
285:            vic_cycle_no_fetch();
291:                vic_cycle_start_fetch();
293:            vic_cycle_no_fetch();
300:            vic.vbuf = vic_cycle_do_fetch(vic_cycle_fix_addr(addr), &b);
311:            vic.gbuf[vic.buf_offset] = vic_cycle_do_fetch(vic_cycle_fix_addr(addr), &b);
321:                vic_cycle_close_h();
330:void vic_cycle(void)
337:            vic_cycle_open_v();
354:                vic_cycle_end_of_line();
361:                    vic_cycle_end_of_frame();
369:                vic_cycle_end_of_line();
376:                    vic_cycle_end_of_frame();
395:            vic_cycle_end_of_line();
406:                vic_cycle_end_of_frame();
422:            vic_cycle_open_h();
427:            vic_cycle_handle_memptr();
443:            vic_cycle_latch_rows();
449:        vic_cycle_latch_columns();
</workspace_result>

### Turn 58 (Function)
[tool_response]
exit: 0
80099
1989

  Length LastWriteTime
  ------ -------------
10488833 8/5/2026 2:13:32 PM
True



### Turn 59 (Assistant)
[tool_request: grep]
- pattern: vice_shim_cycle_checkpoint|vice_machine_step_cycle|vice_shim_start_worker|maincpu_mainloop|vice_shim_stop_worker|vice_shim_reset_cpu
- path: F:\GitHub\vice-sharp\native\vice-shim.c
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vice-shim-runtime.h
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 196
- limit: 180
[tool_request: run_terminal_command]
- command: Select-String -Path F:\GitHub\vice-sharp\native\vice-shim.c -Pattern "^VICE_SHIM_API|^static.*vice_shim_|^int vice_shim_cycle|^void vice_shim" | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" }
- description: List shim API and static function decls

### Turn 60 (Function)
[tool_response]
196→static int vice_shim_initialize_runtime_locked(void)
{
    char module_directory[MAX_PATH];
    char program_path[MAX_PATH];
200→    char data_directory[MAX_PATH];
    char *argv[1];
    int argc = 1;

    if (g_runtime_initialized) {
        return 1;
    }

    if (g_runtime_failed) {
        return 0;
210→    }

    if (!vice_shim_get_module_directory(module_directory, sizeof(module_directory))
        || !vice_shim_compose_path(program_path, sizeof(program_path), module_directory, "vice\\vice\\src\\x64sc.exe")
        || !vice_shim_compose_path(data_directory, sizeof(data_directory), module_directory, "vice\\vice\\data")) {
        g_runtime_failed = 1;
        return 0;
    }

    console_mode = true;
220→    default_settings_requested = true;
    help_requested = false;
    video_disabled_mode = false;

    archdep_set_openmp_wait_policy();
    lib_init();
    log_set_limit_early(LOG_LIMIT_STANDARD);

    argv[0] = program_path;
    if (archdep_init(&argc, argv) != 0) {
230→        archdep_startup_log_error("archdep_init failed.\n");
        g_runtime_failed = 1;
        return 0;
    }

    /* This shim is an x64sc (cycle-exact) build: VIC-II uses viciisc/vicii-cycle.c
       and the SC memory model. machine_class is a compile-time global that the link
       resolved to plain VICE_MACHINE_C64; force it to C64SC so machine_get_name()
       reports "C64SC" and snapshots are read/written with the x64sc machine identity
       (matching real x64sc .vsf files). */
240→    machine_class = VICE_MACHINE_C64SC;

    tick_init();
    maincpu_early_init();
    machine_setup_context();
    drive_setup_context();
    machine_early_init();
    sysfile_init(machine_name);

    /* reSID is the canonical SID engine. The build defines HAVE_RESID and leaves
250→       HAVE_FASTSID undefined, so resources_set_defaults() selects reSID (the only
       compiled engine) via SID_ENGINE_DEFAULT -> SID_ENGINE_RESID. Do NOT set the
       SidEngine resource explicitly: switching the engine a second time before
       init_main wires up sound/SID fails. fastsid is excluded at the build level. */
    if (init_resources() < 0
        || init_cmdline_options() < 0
        || gfxoutput_early_init((int)help_requested) < 0
        || gfxoutput_resources_init() < 0
        || gfxoutput_cmdline_options_init() < 0
        || screenshot_cmdline_options_init() < 0
260→        || resources_set_defaults() < 0
        || resources_set_int("RAMInitRandomChance", 0) < 0
        || resources_set_string("Directory", data_directory) < 0
        || log_init() < 0
        || video_init() < 0
        || init_main() < 0) {
        g_runtime_failed = 1;
        return 0;
    }

270→    g_runtime_initialized = 1;
    return 1;
}

static unsigned __stdcall vice_shim_worker_main(void *parameter)
{
    (void)parameter;

    vice_shim_set_current_thread_description(L"ViceSharp.NativeViceShim");

280→    maincpu_mainloop();

    EnterCriticalSection(&g_state_lock);
    g_worker_running = 0;
    g_cycle_paused = 1;
    WakeAllConditionVariable(&g_state_cv);
    LeaveCriticalSection(&g_state_lock);

    return 0;
}
290→
typedef HRESULT (WINAPI *vice_shim_set_thread_description_fn)(HANDLE thread, PCWSTR description);

static void vice_shim_set_current_thread_description(const wchar_t *description)
{
    HMODULE kernel32;
    vice_shim_set_thread_description_fn set_thread_description;

    kernel32 = GetModuleHandleW(L"Kernel32.dll");
    if (kernel32 == NULL) {
300→        return;
    }

    set_thread_description = (vice_shim_set_thread_description_fn)GetProcAddress(kernel32, "SetThreadDescription");
    if (set_thread_description == NULL) {
        return;
    }

    (void)set_thread_description(GetCurrentThread(), description);
}
310→
static int vice_shim_stop_worker(void *machine)
{
    HANDLE worker = NULL;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine) || g_worker_thread == NULL) {
        LeaveCriticalSection(&g_state_lock);
320→        return 1;
    }

    g_stop_requested = 1;
    WakeAllConditionVariable(&g_state_cv);
    worker = g_worker_thread;
    LeaveCriticalSection(&g_state_lock);

    if (WaitForSingleObject(worker, VICE_SHIM_STOP_TIMEOUT_MS) != WAIT_OBJECT_0) {
        return 0;
330→    }
    CloseHandle(worker);

    EnterCriticalSection(&g_state_lock);
    if (g_worker_thread == worker) {
        g_worker_thread = NULL;
    }
    g_worker_running = 0;
    g_stop_requested = 0;
    g_granted_cycles = 0;
340→    g_cycle_paused = 0;
    LeaveCriticalSection(&g_state_lock);

    return 1;
}

static void vice_shim_reset_cpu_state_locked(void)
{
    const uint16_t reset_vector = (uint16_t)(mem_read(0xfffc) | (mem_read(0xfffd) << 8));

350→    maincpu_clk_limit = 0;
    maincpu_ba_low_flags = 0;
    stolen_cycles = 0;
    check_ba_low = 0;
    maincpu_rmw_flag = 0;
    last_opcode_info = 0;
    last_opcode_addr = reset_vector;

    maincpu_regs.a = 0;
    maincpu_regs.x = 0;
360→    maincpu_regs.y = 0;
    maincpu_regs.sp = 0;
    maincpu_regs.p = P_INTERRUPT;
    maincpu_regs.n = 0;
    maincpu_regs.z = 0;
    maincpu_regs.pc = reset_vector;

    g_bootstrap_pending = 1;
}

370→VICE_SHIM_API void *vice_machine_create(void)
{
    return vice_machine_create_model(NULL);
}

VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)

### Turn 61 (Function)
[tool_response]
1→#ifndef VICE_SHIM_RUNTIME_H
#define VICE_SHIM_RUNTIME_H

/*
 * ViceSharp hosted-runtime hooks. Implemented in native/vice-shim.c and linked
 * into vice_x64.dll. Only referenced when the CPU cores are compiled with
 * -DVICE_SHIM_HOSTED (see native/build-vice-shim.sh). See
 * native/patches/vice-shim-runtime.prompt.md for the canonical spec.
 */
10→
/* Called once per CPU cycle from the hosted CLK_INC(). Returns nonzero when the
   host has asked the CPU to stop at this cycle boundary. */
int vice_shim_cycle_checkpoint(void);

/* Returns nonzero when the host has queued a CPU bootstrap (adopt host-provided
   registers instead of performing the normal power-on reset). Consumes the
   pending flag. */
int vice_shim_take_bootstrap_maincpu(void);

20→#endif


### Turn 62 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 15 matching lines
F:\GitHub\vice-sharp\native\vice-shim.c
280:    maincpu_mainloop();
311:static int vice_shim_stop_worker(void *machine)
346:static void vice_shim_reset_cpu_state_locked(void)
615:    vice_shim_stop_worker(machine);
645:    vice_shim_stop_worker(machine);
697:    vice_shim_reset_cpu_state_locked();
745:    vice_shim_stop_worker(machine);
761:       maincpu_mainloop on the next step, vice_shim_take_bootstrap_maincpu()
796:    vice_shim_stop_worker(machine);
826:    vice_shim_stop_worker(machine);
863:    if (!vice_shim_stop_worker(machine)) {
895:    if (!vice_shim_stop_worker(machine)) {
1135:VICE_SHIM_API void vice_machine_step_cycle(void *machine)
1138:        fprintf(stderr, "vice_machine_step_cycle call=%u machine=%p\\n", ++g_debug_step_calls, machine);
1194:int vice_shim_cycle_checkpoint(void)
</workspace_result>

### Turn 63 (Function)
[tool_response]
exit: 0
105:static int vice_shim_model_from_selector(const char *selector);
106:static int vice_shim_selector_equals(const char *left, const char *right);
107:static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive);
108:static int vice_shim_apply_cartridge_locked(const vice_machine_t *instance);
109:static int vice_shim_cartridge_type_for(const vice_machine_t *instance, int length, int mapping_mode);
110:static void vice_shim_detach_cartridge_locked(void);
111:static int vice_shim_write_temp_file(const uint8_t *data, int length, char *path, size_t path_size);
112:static uint16_t vice_shim_read_cia_timer(cia_context_t *cia, ciat_t *timer);
113:static void vice_shim_set_current_thread_description(const wchar_t *description);
115:static BOOL CALLBACK vice_shim_initialize_sync(PINIT_ONCE init_once, PVOID parameter, PVOID *context)
126:static void vice_shim_ensure_sync_primitives(void)
131:static int vice_shim_is_active_machine(const void *machine)
139:static uint16_t vice_shim_read_cia_timer(cia_context_t *cia, ciat_t *timer)
152:static int vice_shim_wait_for_signal_with_timeout(unsigned int timeout_ms)
157:static int vice_shim_get_module_directory(char *buffer, size_t buffer_size)
187:static int vice_shim_compose_path(char *buffer,
196:static int vice_shim_initialize_runtime_locked(void)
274:static unsigned __stdcall vice_shim_worker_main(void *parameter)
293:static void vice_shim_set_current_thread_description(const wchar_t *description)
311:static int vice_shim_stop_worker(void *machine)
346:static void vice_shim_reset_cpu_state_locked(void)
370:VICE_SHIM_API void *vice_machine_create(void)
375:VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)
494:static int vice_shim_model_from_selector(const char *selector)
580:static int vice_shim_selector_equals(const char *left, const char *right)
600:static void vice_shim_reset_sid_renderer_locked(void);
602:VICE_SHIM_API void vice_machine_destroy(void *machine)
638:VICE_SHIM_API void vice_machine_reset(void *machine)
714:static int vice_shim_peek_snapshot_vicii_model(const char *path)
739:VICE_SHIM_API int vice_machine_read_snapshot(void *machine, const char *path)
790:VICE_SHIM_API int vice_machine_write_snapshot(void *machine, const char *path)
813:VICE_SHIM_API int vice_snapshot_last_error(void)
818:VICE_SHIM_API int vice_machine_attach_cartridge(void *machine, const uint8_t *image, int length, int mapping_mode)
851:VICE_SHIM_API int vice_machine_attach_disk(void *machine, unsigned int unit, unsigned int drive, const char *path)
889:VICE_SHIM_API int vice_machine_detach_disk(void *machine, unsigned int unit, unsigned int drive)
912:VICE_SHIM_API uint8_t vice_machine_peek_ram(void *machine, uint16_t address)
927:VICE_SHIM_API uint8_t vice_machine_read(void *machine, uint16_t address)
942:VICE_SHIM_API void vice_machine_write(void *machine, uint16_t address, uint8_t value)
953:VICE_SHIM_API int vice_machine_get_model(void *machine)
968:VICE_SHIM_API int vice_machine_set_keyboard_matrix_key(void *machine, int row, int column, int pressed)
987:VICE_SHIM_API void vice_machine_cia1_store(void *machine, uint8_t register_index, uint8_t value)
998:VICE_SHIM_API uint8_t vice_machine_cia1_read(void *machine, uint8_t register_index)
1013:static int vice_shim_apply_cartridge_locked(const vice_machine_t *instance)
1044:static int vice_shim_cartridge_type_for(const vice_machine_t *instance, int length, int mapping_mode)
1082:static void vice_shim_detach_cartridge_locked(void)
1090:static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive)
1097:static int vice_shim_write_temp_file(const uint8_t *data, int length, char *path, size_t path_size)
1135:VICE_SHIM_API void vice_machine_step_cycle(void *machine)
1194:int vice_shim_cycle_checkpoint(void)
1247:VICE_SHIM_API uint8_t vice_cpu_get_a(void *machine)
1262:VICE_SHIM_API uint8_t vice_cpu_get_x(void *machine)
1277:VICE_SHIM_API uint8_t vice_cpu_get_y(void *machine)
1292:VICE_SHIM_API uint8_t vice_cpu_get_p(void *machine)
1307:VICE_SHIM_API uint8_t vice_cpu_get_sp(void *machine)
1322:VICE_SHIM_API uint16_t vice_cpu_get_pc(void *machine)
1352:VICE_SHIM_API void vice_cpu_get_pipeline_state(void *machine, struct vice_cpu_pipeline_state *state)
1390:static int vice_shim_valid_drive_unit_for_cpu(unsigned int unit)
1399:VICE_SHIM_API uint8_t vice_drivecpu_get_a(void *machine, unsigned int unit)
1414:VICE_SHIM_API uint8_t vice_drivecpu_get_x(void *machine, unsigned int unit)
1429:VICE_SHIM_API uint8_t vice_drivecpu_get_y(void *machine, unsigned int unit)
1444:VICE_SHIM_API uint8_t vice_drivecpu_get_p(void *machine, unsigned int unit)
1459:VICE_SHIM_API uint8_t vice_drivecpu_get_sp(void *machine, unsigned int unit)
1474:VICE_SHIM_API uint16_t vice_drivecpu_get_pc(void *machine, unsigned int unit)
1500:VICE_SHIM_API int vice_drive_get_clock_residue(void *machine, unsigned int unit,
1546:VICE_SHIM_API uint64_t vice_drivecpu_get_cycle_accum(void *machine, unsigned int unit)
1571:VICE_SHIM_API int vice_drivecpu_set_cycle_accum(void *machine, unsigned int unit, uint64_t value)
1592:VICE_SHIM_API int vice_drive_set_true_emulation(void *machine, unsigned int unit, int enabled)
1609:VICE_SHIM_API int vice_drive_get_true_emulation(void *machine, unsigned int unit)
1626:VICE_SHIM_API void vice_vic_get_state(void *machine, struct vice_vic_state *state)
1686:static int vice_shim_vic_visible_window_locked(const uint8_t **out_base, int *out_stride, int *out_w, int *out_h)
1719:VICE_SHIM_API int vice_vic_capture_frame_indices(void *machine, uint8_t *buffer, int length, int *width, int *height)
1754:VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height)
1852:VICE_SHIM_API int vice_vic_get_graphics_priority_at_raster(void* machine, uint16_t raster_line, uint8_t* pri_buffer, int length)
1896:VICE_SHIM_API void vice_cia_get_state(void *machine, int cia_index, struct vice_cia_state *state)
1934:VICE_SHIM_API void vice_sid_get_state(void *machine, struct vice_sid_state *state)
1963:VICE_SHIM_API void vice_interrupt_get_state(void *machine, struct vice_interrupt_state *state)
2005:static int vice_shim_ensure_sid_renderer_locked(int sample_rate_hz, int cycles_per_sec)
2032:static void vice_shim_reset_sid_renderer_locked(void)
2042:static void vice_shim_sync_sid_registers_locked(void)
2055:VICE_SHIM_API size_t vice_sid_render_samples(void *machine, int16_t *buffer, size_t n, int delta_t_cycles)
2100:VICE_SHIM_API uint8_t vice_sid_engine_read(void *machine, uint16_t addr)
2115:VICE_SHIM_API void vice_sid_clock(void *machine, int cycles)
2176:VICE_SHIM_API int vice_sid_exact_open(void *machine)
2203:VICE_SHIM_API void vice_sid_exact_reset(void *machine)
2214:VICE_SHIM_API int vice_sid_exact_clock(void *machine, int cycles)
2229:VICE_SHIM_API void vice_sid_exact_write(void *machine, uint16_t addr, uint8_t value)
2240:VICE_SHIM_API uint8_t vice_sid_exact_read(void *machine, uint16_t addr)
2255:VICE_SHIM_API int16_t vice_sid_exact_output(void *machine)
2275:VICE_SHIM_API void vice_sid_exact_get_state(void *machine, struct vice_sid_exact_state *state)
2326:VICE_SHIM_API int vice_sid_exact_set_sampling(void *machine, int method,
2349:VICE_SHIM_API int vice_sid_exact_clock_buffered(void *machine, int cycles,


### Turn 64 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20cpu.c
- offset: 100
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 1135
- limit: 120
[tool_request: grep]
- pattern: machine_class|VICE_MACHINE_VIC20|machine_name
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*.{c,h}
- head_limit: 25
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
- offset: 580
- limit: 80
[tool_request: run_terminal_command]
- command: Select-String -Path F:\GitHub\vice-sharp\native\vice\vice\src\Makefile.am -Pattern "xvic_LDADD|xvic_libs|libvic20" | Select-Object -First 20; Select-String -Path F:\GitHub\vice-sharp\native\vice\vice\src\Makefile -Pattern "^xvic_LDADD|^xvic_OBJECTS|xvic_libs" | Select-Object -First 15
- description: Inspect xvic link libraries

### Turn 65 (Function)
[tool_response]
100→
#endif /* WORDS_BIGENDIAN || !ALLOW_UNALIGNED_ACCESS */


/* HACK: memmap updates for the reg_pc < bank_limit case */
#ifdef FEATURE_CPUMEMHISTORY
#define MEMMAP_UPDATE(addr) memmap_mem_update(addr, 0, 0)
#else
#define MEMMAP_UPDATE(addr)
#endif
110→

/* FETCH_OPCODE implementation(s) */
/* FIXME: update last_read on "< bank_limit" case */
#if !defined WORDS_BIGENDIAN && defined ALLOW_UNALIGNED_ACCESS
#define FETCH_OPCODE(o) \
    do { \
        if (((int)reg_pc) < bank_limit) {                       \
            o = (*((uint32_t *)(bank_base + reg_pc)) & 0xffffff);  \
            MEMMAP_UPDATE(reg_pc);                              \
120→            CLK_INC();                                          \
            CLK_INC();                                          \
            if (fetch_tab[o & 0xff]) {                          \
                CLK_INC();                                      \
            }                                                   \
        } else {                                                \
            o = LOAD(reg_pc);                                   \
            CLK_INC();                                          \
            o |= LOAD(reg_pc + 1) << 8;                         \
            CLK_INC();                                          \
130→            if (fetch_tab[o & 0xff]) {                          \
                o |= (LOAD(reg_pc + 2) << 16);                  \
                CLK_INC();                                      \
            }                                                   \
        }                                                       \
    } while (0)

#else /* WORDS_BIGENDIAN || !ALLOW_UNALIGNED_ACCESS */
#define FETCH_OPCODE(o) \
    do { \
140→        if (((int)reg_pc) < bank_limit) {                         \
            (o).ins = *(bank_base + reg_pc);                      \
            MEMMAP_UPDATE(reg_pc);                                \
            CLK_INC();                                            \
            (o).op.op16 = *(bank_base + reg_pc + 1);              \
            CLK_INC();                                            \
            if (fetch_tab[(o).ins]) {                             \
                (o).op.op16 |= (*(bank_base + reg_pc + 2) << 8);  \
                CLK_INC();                                        \
            }                                                     \
150→        } else {                                                  \
            (o).ins = LOAD(reg_pc);                               \
            CLK_INC();                                            \
            (o).op.op16 = LOAD(reg_pc + 1);                       \
            CLK_INC();                                            \
            if (fetch_tab[(o).ins]) {                             \
                (o).op.op16 |= (LOAD(reg_pc + 2) << 8);           \
                CLK_INC();                                        \
            }                                                     \
        }                                                         \
160→    } while (0)

#endif /* WORDS_BIGENDIAN || !ALLOW_UNALIGNED_ACCESS */


#include "../mainviccpu.c"


### Turn 66 (Function)
[tool_response]
1135→VICE_SHIM_API void vice_machine_step_cycle(void *machine)
{
    if (g_debug_step_calls < 16) {
        fprintf(stderr, "vice_machine_step_cycle call=%u machine=%p\\n", ++g_debug_step_calls, machine);
        fflush(stderr);
1140→    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

1150→    if (g_worker_thread == NULL || !g_worker_running) {
        if (g_worker_thread != NULL) {
            CloseHandle(g_worker_thread);
            g_worker_thread = NULL;
            g_worker_running = 0;
            g_cycle_paused = 1;
            g_stop_requested = 0;
            g_granted_cycles = 0;
        }

1160→        uintptr_t worker_handle;

        g_stop_requested = 0;
        g_granted_cycles = 1;
        g_cycle_paused = 0;
        g_worker_running = 1;

        worker_handle = _beginthreadex(NULL, 0, vice_shim_worker_main, NULL, 0, NULL);
        if (worker_handle == 0) {
            g_worker_running = 0;
1170→            g_granted_cycles = 0;
            LeaveCriticalSection(&g_state_lock);
            return;
        }

        g_worker_thread = (HANDLE)worker_handle;
    } else {
        g_granted_cycles++;
        g_cycle_paused = 0;
        WakeAllConditionVariable(&g_state_cv);
1180→    }

    while (g_worker_running && !g_cycle_paused) {
        if (!vice_shim_wait_for_signal_with_timeout(VICE_SHIM_STEP_TIMEOUT_MS)) {
            g_stop_requested = 1;
            g_granted_cycles = 0;
            g_cycle_paused = 1;
            WakeAllConditionVariable(&g_state_cv);
            break;
        }
1190→    }
    LeaveCriticalSection(&g_state_lock);
}

int vice_shim_cycle_checkpoint(void)
{
    int should_stop;

    if (g_debug_checkpoint_calls < 16) {
        fprintf(stderr, "cycle_checkpoint: granted=%u running=%d paused=%d stop=%d\\n",
1200→                g_granted_cycles, g_worker_running, g_cycle_paused, g_stop_requested);
        fflush(stderr);
        ++g_debug_checkpoint_calls;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_granted_cycles > 0) {
        g_granted_cycles--;
1210→    }

    if (g_granted_cycles == 0) {
        g_cycle_paused = 1;
        WakeAllConditionVariable(&g_state_cv);
        while (!g_stop_requested && g_granted_cycles == 0) {
            if (!vice_shim_wait_for_signal_with_timeout(VICE_SHIM_STEP_TIMEOUT_MS)) {
                g_stop_requested = 1;
                g_granted_cycles = 0;
                g_cycle_paused = 1;
1220→                WakeAllConditionVariable(&g_state_cv);
                break;
            }
        }
    }

    g_cycle_paused = 0;
    should_stop = g_stop_requested;
    LeaveCriticalSection(&g_state_lock);

1230→    return should_stop;
}

int vice_shim_take_bootstrap_maincpu(void)
{
    int bootstrap_pending;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
1240→    bootstrap_pending = g_bootstrap_pending;
    g_bootstrap_pending = 0;
    LeaveCriticalSection(&g_state_lock);

    return bootstrap_pending;
}

VICE_SHIM_API uint8_t vice_cpu_get_a(void *machine)
{
    uint8_t value = 0;
1250→
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {

### Turn 67 (Function)
[tool_response]
580→    bank_base_ready = true;

    machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);

    while (1) {
#define CPU_LOG_ID maincpu_log
#define ANE_LOG_LEVEL ane_log_level
#define LXA_LOG_LEVEL lxa_log_level
#define CPU_IS_JAMMED maincpu_jammed
#define CLK maincpu_clk
590→#define RMW_FLAG maincpu_rmw_flag
#define LAST_OPCODE_INFO last_opcode_info
#define LAST_OPCODE_ADDR last_opcode_addr
#define TRACEFLG debug.maincpu_traceflg

#define CPU_INT_STATUS maincpu_int_status

#define ALARM_CONTEXT maincpu_alarm_context

#define CHECK_PENDING_ALARM() (clk >= next_alarm_clk(maincpu_int_status))
600→
#define CHECK_PENDING_INTERRUPT() check_pending_interrupt(maincpu_int_status)

#define TRAP(addr) maincpu_int_status->trap_func(addr);

#define ROM_TRAP_HANDLER() traps_handler()

#define JAM()                                                         \
    do {                                                              \
        unsigned int tmp;                                             \
610→                                                                      \
        EXPORT_REGISTERS();                                           \
        tmp = machine_jam("   " CPU_STR ": JAM at $%04X   ", reg_pc); \
        switch (tmp) {                                                \
            case JAM_RESET_CPU:                                       \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_POWER_CYCLE:                                     \
                machine_powerup();                                    \
                DO_INTERRUPT(IK_RESET);                               \
620→                break;                                                \
            case JAM_MONITOR:                                         \
                monitor_startup(e_comp_space);                        \
                IMPORT_REGISTERS();                                   \
                break;                                                \
            default:                                                  \
                CLK_INC();                                            \
        }                                                             \
    } while (0)

630→#define CALLER e_comp_space

#define ROM_TRAP_ALLOWED() mem_rom_trap_allowed((uint16_t)reg_pc)

#define GLOBAL_REGS maincpu_regs

#include "6510dtvcore.c"

        maincpu_int_status->num_dma_per_opcode = 0;

640→        if (maincpu_clk_limit && (maincpu_clk > maincpu_clk_limit)) {
            log_error(LOG_DEFAULT, "cycle limit reached.");
            archdep_vice_exit(EXIT_FAILURE);
        }

        autostart_advance();
#if 0
        if (CLK > 246171754) {
            debug.maincpu_traceflg = 1;
        }
650→#endif
    }
}

/* ------------------------------------------------------------------------- */

void maincpu_set_pc(int pc) {
    MOS6510_REGS_SET_PC(&maincpu_regs, pc);
}


### Turn 68 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 8 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20.c
144:const char machine_name[] = "VIC20";
145:int machine_class = VICE_MACHINE_VIC20;
1287:    return machine_name;

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20-snapshot.c
71:                        machine_name);
115:    s = snapshot_open(name, &major, &minor, machine_name);

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20rom.c
113:                     machine_name,
157:                         machine_name,
176:                         machine_name,
</workspace_result>

### Turn 69 (Function)
[tool_response]
exit: 0

native\vice\vice\src\Makefile.am:612:vic20_lib = $(top_builddir)/src/vic20/libvic20.a
native\vice\vice\src\Makefile.am:613:vic20stubs_lib = $(top_builddir)/src/vic20/libvic20stubs.a
native\vice\vice\src\Makefile.am:614:vic20cart_lib = $(top_builddir)/src/vic20/cart/libvic20cart.a
native\vice\vice\src\Makefile.am:1007:xvic_libs =  \
native\vice\vice\src\Makefile.am:1042:xvic_LDADD = $(xvic_libs) $(emu_extlibs) $(mascuerade_lib) @TFE_LIBS@ 
@NETPLAY_LIBS@ $(XVIC_RES)
native\vice\vice\src\Makefile.am:1043:xvic_DEPENDENCIES = $(xvic_libs)
native\vice\vice\src\Makefile.am:1054:@for i in $(xvic_libs); do rm -fr "$$i"; done
native\vice\vice\src\Makefile.am:1621:@(cd vic20 && $(MAKE) libvic20.a)
native\vice\vice\src\Makefile.am:1623:@echo "making libvic20stubs.a in vic20"
native\vice\vice\src\Makefile.am:1624:@(cd vic20 && $(MAKE) libvic20stubs.a)
native\vice\vice\src\Makefile.am:1626:@echo "making libvic20cart.a in vic20/cart"
native\vice\vice\src\Makefile.am:1627:@(cd vic20/cart && $(MAKE) libvic20cart.a)
native\vice\vice\src\Makefile:291:xvic_OBJECTS = $(am_xvic_OBJECTS)
native\vice\vice\src\Makefile:1473:xvic_libs = \
native\vice\vice\src\Makefile:1508:xvic_LDADD = $(xvic_libs) $(emu_extlibs) $(mascuerade_lib)   $(XVIC_RES)
native\vice\vice\src\Makefile:1509:xvic_DEPENDENCIES = $(xvic_libs)
native\vice\vice\src\Makefile:2451:@for i in $(xvic_libs); do rm -fr "$$i"; done



### Turn 70 (Assistant)
[tool_request: grep]
- pattern: stolen_cycles|check_ba_low|maincpu_ba_low|last_opcode
- path: F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
[tool_request: grep]
- pattern: stolen_cycles|check_ba_low|maincpu_ba_low|last_opcode
- path: F:\GitHub\vice-sharp\native\vice\vice\src\mainc64cpu.c
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic.h
- limit: 80
[tool_request: run_terminal_command]
- command: Select-String -Path F:\GitHub\vice-sharp\native\vice\vice\src\Makefile -Pattern "am__objects_1|xvic_SOURCES|main.o" | Select-Object -First 30; (Select-String -Path F:\GitHub\vice-sharp\native\vice\vice\src\Makefile -Pattern "^am__objects_1" -Context 0,5).Line
- description: Find xvic object composition

### Turn 71 (Function)
[tool_response]
1→/*
 * vic.h - A VIC-I emulation (under construction)
 *
 * Written by
 *  Ettore Perazzoli <ettore@comm2000.it>
 *
 * 16/24bpp support added by
 *  Steven Tieu <stieu@physics.ubc.ca>
 *  Teemu Rantanen <tvr@cs.hut.fi>
10→ *
 * This file is part of VICE, the Versatile Commodore Emulator.
 * See README for copyright notice.
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
20→ *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */
30→
#ifndef VICE_VIC_H
#define VICE_VIC_H

#include "vice.h"
#include "types.h"

/* VIC border mode defines */
#define VIC_NORMAL_BORDERS 0
#define VIC_FULL_BORDERS   1
40→#define VIC_DEBUG_BORDERS  2
#define VIC_NO_BORDERS     3
#define VIC_BORDER_MODE(v) (v << 12)

struct snapshot_s;
struct screenshot_s;
struct palette_s;
struct canvas_refresh_s;
struct video_chip_cap_s;

50→struct raster_s *vic_init(void);
struct video_canvas_s *vic_get_canvas(void);
void vic_reset(void);
void vic_raster_draw_handler(void);

int vic_resources_init(void);
int vic_cmdline_options_init(void);

int vic_snapshot_write_module(struct snapshot_s *s);
int vic_snapshot_read_module(struct snapshot_s *s);
60→
void vic_screenshot(struct screenshot_s *screenshot);
void vic_async_refresh(struct canvas_refresh_s *refresh);
void vic_shutdown(void);

void vic_set_light_pen(CLOCK mclk, int state);
void vic_trigger_light_pen(CLOCK mclk);
CLOCK vic_lightpen_timing(int x, int y);
void vic_trigger_light_pen_internal(int retrigger);

70→struct machine_timing_s;

void vic_change_timing(struct machine_timing_s *machine_timing, int border_mode);

int vic_dump(void);

/* Debugging options.  */
/* #define VIC_RASTER_DEBUG */
/* #define VIC_REGISTERS_DEBUG */
/* #define VIC_CYCLE_DEBUG */
80→

### Turn 72 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 29 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\mainc64cpu.c
80:CLOCK stolen_cycles;
99:int check_ba_low = 0;
121:    if (maincpu_ba_low_flags & MAINCPU_BA_LOW_VICII) {
123:        maincpu_ba_low_flags &= ~MAINCPU_BA_LOW_VICII;
126:    if (maincpu_ba_low_flags & MAINCPU_BA_LOW_REU) {
128:        maincpu_ba_low_flags &= ~MAINCPU_BA_LOW_REU;
136:    opcode = OPINFO_NUMBER(*cs->last_opcode_info_ptr);
140:            if (check_ba_low) {
141:                OPINFO_SET_ENABLES_IRQ(*cs->last_opcode_info_ptr, 1);
158:            if (check_ba_low) {
159:                OPINFO_SET_ENABLES_IRQ(*cs->last_opcode_info_ptr, 1);
179:            OPINFO_SET_ENABLES_IRQ(*cs->last_opcode_info_ptr, 1);
200:    if (maincpu_ba_low_flags) {
210:        stolen_cycles += maincpu_clk - old_maincpu_clk;
323:    check_ba_low = 1;           \
325:    check_ba_low = 0
330:    check_ba_low = 1;           \
332:    check_ba_low = 0
406:    check_ba_low = 1;           \
408:    check_ba_low = 0
413:    check_ba_low = 1;                 \
415:    check_ba_low = 0
487:unsigned int last_opcode_info;
490:unsigned int last_opcode_addr;
624:    interrupt_cpu_status_init(maincpu_int_status, &last_opcode_info);
674:    if (OPINFO_NUMBER(*cs->last_opcode_info_ptr) == 0x00) {
680:    if (OPINFO_DELAYS_INTERRUPT(*cs->last_opcode_info_ptr)) {
701:    if (OPINFO_DELAYS_INTERRUPT(*cs->last_opcode_info_ptr)) {
706:        if (!OPINFO_ENABLES_IRQ(*cs->last_opcode_info_ptr)) {
</workspace_result>

### Turn 73 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 12 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
67:CLOCK stolen_cycles;
310:unsigned int last_opcode_info;
313:unsigned int last_opcode_addr;
446:    interrupt_cpu_status_init(maincpu_int_status, &last_opcode_info);
496:    if (OPINFO_NUMBER(*cs->last_opcode_info_ptr) == 0x00) {
502:    if (OPINFO_DELAYS_INTERRUPT(*cs->last_opcode_info_ptr)) {
523:    if (OPINFO_DELAYS_INTERRUPT(*cs->last_opcode_info_ptr)) {
530:        if (!OPINFO_ENABLES_IRQ(*cs->last_opcode_info_ptr)) {
591:#define LAST_OPCODE_INFO last_opcode_info
592:#define LAST_OPCODE_ADDR last_opcode_addr
732:        || SMW_DW(m, (uint32_t)last_opcode_info) < 0
780:        || SMR_DW_UINT(m, &last_opcode_info) < 0
</workspace_result>

### Turn 74 (Function)
[tool_response]
exit: 0

native\vice\vice\src\Makefile:134:am__objects_1 = alarm.$(OBJEXT) attach.$(OBJEXT) autostart.$(OBJEXT) \
native\vice\vice\src\Makefile:143:main.$(OBJEXT) mainlock.$(OBJEXT) m3u.$(OBJEXT) \
native\vice\vice\src\Makefile:152:am_vsid_OBJECTS = $(am__objects_1) $(am__objects_2)
native\vice\vice\src\Makefile:166:am_x128_OBJECTS = $(am__objects_1) $(am__objects_2)
native\vice\vice\src\Makefile:181:am_x64_OBJECTS = $(am__objects_1) $(am__objects_2)
native\vice\vice\src\Makefile:195:am_x64dtv_OBJECTS = $(am__objects_1)
native\vice\vice\src\Makefile:209:am_x64sc_OBJECTS = $(am__objects_1) $(am__objects_2)
native\vice\vice\src\Makefile:223:am_xcbm2_OBJECTS = $(am__objects_1)
native\vice\vice\src\Makefile:236:am_xcbm5x0_OBJECTS = $(am__objects_1)
native\vice\vice\src\Makefile:249:am_xpet_OBJECTS = $(am__objects_1)
native\vice\vice\src\Makefile:262:am_xplus4_OBJECTS = $(am__objects_1)
native\vice\vice\src\Makefile:276:am_xscpu64_OBJECTS = $(am__objects_1) $(am__objects_2)
native\vice\vice\src\Makefile:290:am_xvic_OBJECTS = $(am__objects_1) $(am__objects_2)
native\vice\vice\src\Makefile:333:./$(DEPDIR)/mainlock.Po ./$(DEPDIR)/midi.Po \
native\vice\vice\src\Makefile:359:$(xplus4_SOURCES) $(xscpu64_SOURCES) $(xvic_SOURCES)
native\vice\vice\src\Makefile:363:$(xplus4_SOURCES) $(xscpu64_SOURCES) $(xvic_SOURCES)
native\vice\vice\src\Makefile:893:mainlock.h \
native\vice\vice\src\Makefile:992:mainlock.c \
native\vice\vice\src\Makefile:1507:xvic_SOURCES = $(base_sources) $(midi_sources)
native\vice\vice\src\Makefile:1905:include ./$(DEPDIR)/mainlock.Po # am--include-marker
native\vice\vice\src\Makefile:2186:-rm -f ./$(DEPDIR)/mainlock.Po
native\vice\vice\src\Makefile:2288:-rm -f ./$(DEPDIR)/mainlock.Po
am__objects_1 = alarm.$(OBJEXT) attach.$(OBJEXT) autostart.$(OBJEXT) \



### Turn 75 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- offset: 669
- limit: 160
[tool_request: todo_write]
- todos: [{"id":"n0","content":"n0: Extend native oracle for Vic20 model create","status":"in_progress"},{"id":"n1","content":"n1: LockstepValidator Vic20 path CPU+VIA+VIC regs","status":"pending"},{"id":"n2","content":"n2: Multi-cycle Vic20 lockstep suite + CI ROMs","status":"pending"},{"id":"tierA","content":"tierA: Close remaining Tier A gaps for Vic20 filter","status":"pending"},{"id":"verify","content":"verify: Run verification plan 1-5; capture SCRATCH logs","status":"pending"},{"id":"docs","content":"docs: Update HANDOFF/roadmap after gates pass","status":"pending"}]
- merge: true

### Turn 76 (Function)
[tool_response]
- [in_progress] n0: n0: Extend native oracle for Vic20 model create
- [pending] n1: n1: LockstepValidator Vic20 path CPU+VIA+VIC regs
- [pending] n2: n2: Multi-cycle Vic20 lockstep suite + CI ROMs
- [pending] tierA: tierA: Close remaining Tier A gaps for Vic20 filter
- [pending] verify: verify: Run verification plan 1-5; capture SCRATCH logs
- [pending] docs: docs: Update HANDOFF/roadmap after gates pass


### Turn 77 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Native VICE emulator binding interface
/// </summary>
public interface IViceNative : IDisposable
{
    /// <summary>
    /// Reset emulator to initial power on state
10→    /// </summary>
    void Reset();

    /// <summary>
    /// Advance emulator by one single master cycle
    /// </summary>
    void Step();

    /// <summary>
    /// Attach a raw cartridge image to the native emulator instance.
20→    /// </summary>
    void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode);

    /// <summary>
    /// Attach a disk image path to the native emulator drive.
    /// </summary>
    void AttachDisk(uint unit, uint drive, string path);

    /// <summary>
    /// Detach the disk image from the native emulator drive.
30→    /// </summary>
    void DetachDisk(uint unit, uint drive);

    /// <summary>
    /// Apply a host keyboard matrix key state to the native emulator.
    /// </summary>
    void SetKeyboardMatrixKey(int row, int column, bool pressed);

    /// <summary>
    /// Read a byte from native physical C64 RAM without bus side effects.
40→    /// </summary>
    byte PeekRam(ushort address);

    /// <summary>
    /// Load a VICE snapshot (.vsf) into the native machine, resuming it from the
    /// staged state. The cycle counter is re-baselined so <see cref="GetState"/>
    /// reports cycles elapsed since the load point. Returns 0 on success.
    /// </summary>
    int ReadSnapshot(string path);

50→    /// <summary>
    /// Write the current native machine state to a VICE snapshot (.vsf).
    /// Returns 0 on success.
    /// </summary>
    int WriteSnapshot(string path);

    /// <summary>
    /// Get current full machine state
    /// </summary>
    MachineState GetState();
60→
    /// <summary>
    /// Get current native VIC-II timing state for lockstep diagnostics.
    /// </summary>
    NativeVicState GetVicState();

    /// <summary>
    /// Get the main-CPU resume/pipeline state (TR-LOCKSTEP-VSF-001): the
    /// .vsf-restored in-flight context beyond the plain register file (last
    /// opcode info, pending BA-low stall flags, the 6510 processor port that
70→    /// selects ROM/IO banking, and the interrupt-status clocks). Valid right
    /// after <see cref="ReadSnapshot"/> and at any paused cycle boundary; used
    /// to stage a managed machine so snapshot-resumed lockstep aligns from
    /// cycle 0.
    /// </summary>
    NativeCpuPipelineState GetCpuPipelineState();

    /// <summary>
    /// Get a CIA's resume state (TR-LOCKSTEP-VSF-001): ports/DDRs, live timer
    /// counters plus reload latches, control registers, latched interrupt
80→    /// flags and the ICR enable mask, as restored from the .vsf CIA modules.
    /// Index 0 = CIA1 ($DC00), 1 = CIA2 ($DD00).
    /// </summary>
    NativeCiaState GetCiaState(int ciaIndex);
}

/// <summary>
/// CIA resume state exported by the native shim (TR-LOCKSTEP-VSF-001):
/// everything a managed MOS 6526 needs to resume a .vsf mid-run - the port
/// output latches and data directions, the LIVE timer counters (ciat_read_timer)
90→/// with their reload latches (ciat_read_latch), the control registers, the
/// latched interrupt flags and the ICR interrupt-enable mask (ciacore
/// irq_enabled).
/// </summary>
public readonly struct NativeCiaState
{
    /// <summary>Port A output latch ($DC00 written value).</summary>
    public byte PortA { get; init; }

    /// <summary>Port B output latch ($DC01 written value).</summary>
100→    public byte PortB { get; init; }

    /// <summary>Port A data direction ($DC02).</summary>
    public byte DdrA { get; init; }

    /// <summary>Port B data direction ($DC03).</summary>
    public byte DdrB { get; init; }

    /// <summary>Timer A live counter.</summary>
    public ushort TimerA { get; init; }
110→
    /// <summary>Timer B live counter.</summary>
    public ushort TimerB { get; init; }

    /// <summary>Timer A reload latch.</summary>
    public ushort TimerALatch { get; init; }

    /// <summary>Timer B reload latch.</summary>
    public ushort TimerBLatch { get; init; }

120→    /// <summary>Control register A ($DC0E).</summary>
    public byte Cra { get; init; }

    /// <summary>Control register B ($DC0F).</summary>
    public byte Crb { get; init; }

    /// <summary>Latched interrupt flags (ICR read side).</summary>
    public byte InterruptFlags { get; init; }

    /// <summary>ICR interrupt-enable mask (write side).</summary>
130→    public byte IrqMask { get; init; }
}

/// <summary>
/// Serializable machine state snapshot
/// </summary>
public readonly struct MachineState
{
    // CPU Registers
    public byte A { get; init; }
140→    public byte X { get; init; }
    public byte Y { get; init; }
    public byte S { get; init; }
    public byte P { get; init; }
    public ushort PC { get; init; }

    // Cycle counter
    public long Cycle { get; init; }
}

150→/// <summary>
/// Main-CPU resume/pipeline state exported by the native shim
/// (TR-LOCKSTEP-VSF-001): the x64sc in-flight context a .vsf carries beyond
/// the plain register file. Mirrors the native MAINCPU snapshot module
/// (last opcode info + BA-low stall flags), the C64MEM module's 6510
/// processor port (written data/dir plus effective read values selecting
/// ROM/IO banking), and the interrupt-status clocks.
/// </summary>
public readonly struct NativeCpuPipelineState
{
160→    /// <summary>Native master clock (maincpu_clk) at export.</summary>
    public ulong Clk { get; init; }

    /// <summary>MAINCPU module last_opcode_info (opcode + IRQ delay/enable flags).</summary>
    public uint LastOpcodeInfo { get; init; }

    /// <summary>MAINCPU module maincpu_ba_low_flags (pending BA-low stall sources).</summary>
    public uint BaLowFlags { get; init; }

    /// <summary>6510 processor port data register ($01 written value).</summary>
170→    public byte PportData { get; init; }

    /// <summary>6510 processor port direction register ($00 written value).</summary>
    public byte PportDir { get; init; }

    /// <summary>Effective $01 read value (pull-ups applied); selects ROM/IO banking.</summary>
    public byte PportDataRead { get; init; }

    /// <summary>Effective $00 read value.</summary>
    public byte PportDirRead { get; init; }
180→
    /// <summary>Pending interrupt mask (IK_* bits) from the interrupt status.</summary>
    public uint GlobalPendingInt { get; init; }

    /// <summary>Clock of the last IRQ line change.</summary>
    public ulong IrqClk { get; init; }

    /// <summary>Clock of the last NMI line change.</summary>
    public ulong NmiClk { get; init; }

190→    /// <summary>IRQ acknowledge-delay cycle counter.</summary>
    public ulong IrqDelayCycles { get; init; }

    /// <summary>NMI acknowledge-delay cycle counter.</summary>
    public ulong NmiDelayCycles { get; init; }
}

public readonly struct NativeVicState
{
    public uint Cycle { get; init; }
200→    public ushort RasterLine { get; init; }
    public byte RasterCycle { get; init; }
    public byte BadLine { get; init; }
    public byte DisplayState { get; init; }
    public byte SpriteDma { get; init; }

    /// <summary>
    /// Snapshot of the 64-byte VIC-II register file ($D000-$D03F) as the native
    /// shim sees it, with $D019 reflecting the live IRQ latch. Used to seed a
    /// managed VIC for snapshot-resume lockstep diagnostics. May be null if the
210→    /// binding did not populate it.
    /// </summary>
    public byte[]? Registers { get; init; }

    /// <summary>
    /// TR-LOCKSTEP-VSF-001: the .vsf VIC-II module's allow_bad_lines latch
    /// (DEN seen at line $30). Gates check_badline and therefore every badline
    /// BA-low CPU stall for the remainder of the frame; must be seeded when
    /// resuming a managed VIC from a mid-frame snapshot.
    /// </summary>
220→    public byte AllowBadLines { get; init; }

    /// <summary>
    /// TR-LOCKSTEP-VSF-001: the .vsf VIC-II module's display/idle g-access
    /// state (vicii.idle_state).
    /// </summary>
    public byte IdleState { get; init; }
}


### Turn 78 (Function)
[tool_response]
669→    private sealed class ViceNativeInstance : IViceNative
670→    {
        private readonly IntPtr _instance;
        private long _cycleBaseline;

        public ViceNativeInstance(IntPtr instance)
        {
            _instance = instance;
        }

        public void Reset()
680→        {
            ResetNative(_instance);
            _cycleBaseline = ReadNativeCycle();
        }

        public void Step() => StepNative(_instance);

        public int ReadSnapshot(string path)
        {
            var result = ReadSnapshotNative(_instance, path);
690→            // Re-baseline so GetState().Cycle counts forward from the loaded
            // state, mirroring Reset().
            _cycleBaseline = ReadNativeCycle();
            return result;
        }

        public int WriteSnapshot(string path) => WriteSnapshotNative(_instance, path);

        public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
        {
700→            ViceNative.AttachCartridge(_instance, image, mappingMode);
        }

        public void AttachDisk(uint unit, uint drive, string path)
        {
            ViceNative.AttachDisk(_instance, unit, drive, path);
        }

        public void DetachDisk(uint unit, uint drive)
        {
710→            ViceNative.DetachDisk(_instance, unit, drive);
        }

        public void SetKeyboardMatrixKey(int row, int column, bool pressed)
        {
            ViceNative.SetKeyboardMatrixKey(_instance, row, column, pressed);
        }

        public byte PeekRam(ushort address) => PeekRamNative(_instance, address);

720→        public MachineState GetState()
        {
            return new MachineState
            {
                A = GetA(_instance),
                X = GetX(_instance),
                Y = GetY(_instance),
                S = GetS(_instance),
                P = GetP(_instance),
                PC = GetPC(_instance),
730→                Cycle = Math.Max(0, ReadNativeCycle() - _cycleBaseline)
            };
        }

        public NativeVicState GetVicState()
        {
            var state = new ViceVicState();
            global::ViceSharp.Core.ViceNative.GetVicState(_instance, ref state);

            return new NativeVicState
740→            {
                Cycle = state.Cycle,
                RasterLine = state.RasterLine,
                RasterCycle = state.RasterCycle,
                BadLine = state.BadLine,
                DisplayState = state.DisplayState,
                SpriteDma = state.SpriteDma,
                Registers = state.GetRegisters(),
                AllowBadLines = state.AllowBadLines,
                IdleState = state.IdleState
750→            };
        }

        public NativeCiaState GetCiaState(int ciaIndex)
        {
            var state = new ViceCiaState();
            global::ViceSharp.Core.ViceNative.GetCiaState(_instance, ciaIndex, ref state);

            return new NativeCiaState
            {
760→                PortA = state.PortA,
                PortB = state.PortB,
                DdrA = state.DdrA,
                DdrB = state.DdrB,
                TimerA = state.TimerA,
                TimerB = state.TimerB,
                TimerALatch = state.TimerALatch,
                TimerBLatch = state.TimerBLatch,
                Cra = state.Cra,
                Crb = state.Crb,
770→                InterruptFlags = state.InterruptFlag,
                IrqMask = state.IrqMask
            };
        }

        public NativeCpuPipelineState GetCpuPipelineState()
        {
            var state = new ViceCpuPipelineState();
            global::ViceSharp.Core.ViceNative.GetCpuPipelineState(_instance, ref state);

780→            return new NativeCpuPipelineState
            {
                Clk = state.Clk,
                LastOpcodeInfo = state.LastOpcodeInfo,
                BaLowFlags = state.BaLowFlags,
                PportData = state.PportData,
                PportDir = state.PportDir,
                PportDataRead = state.PportDataRead,
                PportDirRead = state.PportDirRead,
                GlobalPendingInt = state.GlobalPendingInt,
790→                IrqClk = state.IrqClk,
                NmiClk = state.NmiClk,
                IrqDelayCycles = state.IrqDelayCycles,
                NmiDelayCycles = state.NmiDelayCycles
            };
        }

        public void Dispose()
        {
            Destroy(_instance);
800→        }

        private long ReadNativeCycle()
        {
            var state = new ViceVicState();
            global::ViceSharp.Core.ViceNative.GetVicState(_instance, ref state);
            return state.Cycle;
        }
    }
}
810→

### Turn 79 (Assistant)
[tool_request: grep]
- pattern: extern.*vic\b|typedef.*vic_s|struct vic_s
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*.{c,h}
- head_limit: 30
[tool_request: run_terminal_command]
- command: Get-ChildItem F:\GitHub\vice-sharp\native\vice\vice\src\vic20 -Filter "vic*.h" | Select-Object Name; Select-String -Path F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-types.h -Pattern "regs|raster|cycle" | Select-Object -First 40
- description: Locate Vic-I types for state export

### Turn 80 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 3 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\victypes.h
160:struct vic_s {
252:typedef struct vic_s vic_t;
254:extern vic_t vic;
</workspace_result>

### Turn 81 (Function)
[tool_response]
exit: 1

NameSelect-String: Cannot find path 'F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-types.h' because it does not exist.

----
vic-cmdline-options.h
vic-color.h
vic-cycle.h
vic-draw.h
vic-mem.h
vic-resources.h
vic-snapshot.h
vic-timing.h
vic.h
vic20-cmdline-options.h
vic20-resources.h
vic20-snapshot.h
vic20.h
vic20iec.h
vic20ieeevia.h
vic20mem.h
vic20memrom.h
vic20memsnapshot.h
vic20model.h
vic20rom.h
vic20rsuser.h
vic20sound.h
vic20via.h
victypes.h



### Turn 82 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\victypes.h
- offset: 160
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 1620
- limit: 70
[tool_request: grep]
- pattern: mem_ram_read|mem_read|mem_store|mem_peek
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20mem.c
- head_limit: 20
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 912
- limit: 50

### Turn 83 (Function)
[tool_response]
160→struct vic_s {
    int initialized;

    signed int log;

    raster_t raster;

    struct palette_s *palette;

    uint8_t regs[0x10];
170→
    /* Cycle # within the current line.  */
    unsigned int raster_cycle;

    /* Current line.  */
    unsigned int raster_line;

    int auxiliary_color;
    int mc_border_color;
    int reverse;
180→    int old_auxiliary_color;
    int old_mc_border_color;
    int old_reverse;
    int half_char_flag;

    unsigned int char_height;   /* changes immediately for memory fetch */
    unsigned int row_increase_line; /* may change next line for row count */
    unsigned int text_cols;     /* = 22 */
    unsigned int text_lines;    /* = 23 */
    unsigned int pending_text_cols;
190→    unsigned int line_was_blank;

    unsigned int memptr;

    /* offset for screen memory pointer */
    unsigned int memptr_inc;

    /* counting the text lines in the current frame */
    unsigned int row_counter;

200→    /* area in the frame */
    vic_area_state_t area;

    /* fetch state */
    vic_fetch_state_t fetch_state;

    /* Screen memory buffer (1 char) */
    uint8_t vbuf;

    /* Offset to the cbuf/gbuf buffers */
210→    unsigned int buf_offset;

    /* Color memory buffer */
    uint8_t cbuf[VIC_MAX_TEXT_COLS];

    /* Graphics buffer (chargen/bitmap) */
    uint8_t gbuf[VIC_MAX_TEXT_COLS];

    /* Geometry and timing parameters of the selected VIC emulation.  */
    unsigned int screen_height;
220→    unsigned int first_displayed_line;
    unsigned int last_displayed_line;

    int screen_leftborderwidth;
    int screen_rightborderwidth;

    unsigned int screen_width;  /* max width of video (different for PAL/NTSC) */
    unsigned int display_width; /* width to display (different for border modes) */

    /* parameters (set by vic-chip-model). */
230→    unsigned int cycles_per_line;

    unsigned int cycle_offset;
    unsigned int max_text_cols;

    vic_light_pen_t light_pen;

    /* Video chip capabilities.  */
    struct video_chip_cap_s *video_chip_cap;

240→    struct {
        uint8_t sing[0x100];
        uint16_t doub[0x100];
    } pixel_table;

    /* Interlace flag - 0 if normal/non-interlaced, 1 if interlaced */
    int interlace_enabled;
    /* which of the two interlace fields is shown */
    int interlace_field;
    /* cycle at which the current frame started */
250→    CLOCK framestart_cycle;
};
typedef struct vic_s vic_t;

extern vic_t vic;

#endif


### Turn 84 (Function)
[tool_response]
912→VICE_SHIM_API uint8_t vice_machine_peek_ram(void *machine, uint16_t address)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
920→        value = mem_ram[address];
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_machine_read(void *machine, uint16_t address)
{
    uint8_t value = 0xff;
930→
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = mem_read(address);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
940→}

VICE_SHIM_API void vice_machine_write(void *machine, uint16_t address, uint8_t value)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        mem_store(address, value);
    }
950→    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_get_model(void *machine)
{
    int model = C64MODEL_UNKNOWN;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
960→    if (vice_shim_is_active_machine(machine)) {
        model = c64model_get();

### Turn 85 (Function)
[tool_response]
1620→    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API void vice_vic_get_state(void *machine, struct vice_vic_state *state)
{
    if (state == NULL) {
        return;
1630→    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        state->cycle = (uint32_t)maincpu_clk;
        state->raster_line = (uint16_t)vicii.raster_line;
1640→        state->raster_cycle = (uint8_t)vicii.raster_cycle;
        state->bad_line = (uint8_t)(vicii.bad_line != 0);
        state->display_state = 0;
        state->sprite_dma = vicii.sprite_dma;
        /* TR-LOCKSTEP-VSF-001: .vsf-restored badline/display context
           (viciisc/vicii-snapshot.c). allow_bad_lines gates check_badline and
           therefore the badline BA stall for the remainder of the frame. */
        state->allow_bad_lines = (uint8_t)(vicii.allow_bad_lines != 0);
        state->idle_state = (uint8_t)(vicii.idle_state != 0);
        /* Two register views (vice-shim.h): `registers` = RAW vicii.regs for
1650→           raw-vs-raw snapshot/reset parity, `registers_peek` = vicii_peek()
           (vicii-mem.c:747-770) for checkpoint comparisons against managed
           Mos6569.Peek, which mirrors vicii_peek exactly (FR-VIC-REGISTERS
           AC-15: unused-bit OR table, live raster in $D011/$D012,
           irq_status|0x70 in $D019). vicii_peek is side-effect-free. */
        memcpy(state->registers, vicii.regs, sizeof(state->registers));
        /* $D019 raw view: vicii.irq_status holds the live IRQ latch; regs[0x19]
           only reflects the last CPU write. Managed stores the latch in its
           register file, so the raw view exports the latch too. */
        state->registers[0x19] = (uint8_t)(vicii.irq_status & 0x0F);
1660→        {
            unsigned int reg;
            for (reg = 0; reg < sizeof(state->registers_peek); reg++) {
                state->registers_peek[reg] = vicii_peek((uint16_t)(0xd000 + reg));
            }
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

1670→/*
 * Per-pixel VIC oracle (PLAN-VICEPARITY-001 Phase 0 / TR-VIC-ORACLE-001).
 *
 * vicii-draw-cycle.c draws 8 palette-indexed pixels per cycle into vicii.dbuf;
 * vicii_raster_draw_handler flushes each completed line through
 * raster_line_emulate -> draw_dummy into the canvas draw buffer at
 * draw_buffer + line * frame_buffer_width + extra_offscreen_border_left
 * (raster_draw_buffer_ptr_update, raster.c). The visible window is
 * screen_leftborderwidth + VICII_SCREEN_XPIX + screen_rightborderwidth
 * pixels wide and spans first_displayed_line..last_displayed_line.
1680→ *
 * Resolves the visible window into *out_base (top-left pixel), *out_stride
 * (frame buffer row width) and *out_w/*out_h. Caller must hold g_state_lock.
 * Returns 0 when the draw buffer is not realized (headless canvas missing)
 * or when the NTSC lower-border wrap layout is active (unsupported here).
 */
static int vice_shim_vic_visible_window_locked(const uint8_t **out_base, int *out_stride, int *out_w, int *out_h)
{
    unsigned int fbw;
    unsigned int first_line;

### Turn 86 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 19 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20mem.c
88:static uint8_t *_mem_read_base_tab[0x101];
89:static int mem_read_limit_tab[0x101];
92:static read_func_ptr_t _mem_read_tab_watch[0x101];
95:static read_func_ptr_t _mem_read_tab_nowatch[0x101];
97:static read_func_ptr_t _mem_peek_tab[0x101];
99:read_func_ptr_t *_mem_read_tab_ptr;
101:read_func_ptr_t *_mem_read_tab_ptr_dummy;
103:static uint8_t **_mem_read_base_tab_ptr;
104:static int *mem_read_limit_tab_ptr;
264:    return _mem_read_tab_nowatch[0](addr);
277:    return _mem_read_tab_nowatch[addr >> 8](addr);
289:void mem_store(uint16_t addr, uint8_t value)
294:uint8_t mem_read(uint16_t addr)
296:    return _mem_read_tab_ptr[addr >> 8](addr);
299:static uint8_t mem_peek(uint16_t addr)
301:    return _mem_peek_tab[addr >> 8](addr);
316:            _mem_read_tab_nowatch[i] = read_func;
318:            _mem_peek_tab[i] = peek_func;
319:            _mem_read_base_tab[i] = read_base + ((i << 8) & base_mask);
</workspace_result>

### Turn 87 (Assistant)
[tool_request: grep]
- pattern: mem_ram
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*.{c,h}
- head_limit: 15
[tool_request: grep]
- pattern: mem_ram
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/mem.h
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
- offset: 560
- limit: 30

### Turn 88 (Function)
[tool_response]
560→#define ORIGIN_MEMSPACE (e_comp_space)
    /* Notice that using a struct for these would make it a lot slower (at
       least, on gcc 2.7.2.x).  */
    uint8_t reg_a = 0;
    uint8_t reg_x = 0;
    uint8_t reg_y = 0;
    uint8_t reg_p = 0;
    uint8_t reg_sp = 0;
    uint8_t flag_n = 0;
    uint8_t flag_z = 0;
570→#ifndef NEED_REG_PC
    /* FIXME: this should really be uint16_t, but it breaks things (eg trap17.prg) */
    unsigned int reg_pc;
#endif

    /*
     * Enable maincpu_resync_limits functionality .. in the old code
     * this is where the local stack var had its address copied to
     * the global.
     */
580→    bank_base_ready = true;

    machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);

    while (1) {
#define CPU_LOG_ID maincpu_log
#define ANE_LOG_LEVEL ane_log_level
#define LXA_LOG_LEVEL lxa_log_level
#define CPU_IS_JAMMED maincpu_jammed
#define CLK maincpu_clk

### Turn 89 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 10 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-cycle.c
235:        c = vflimod_enabled ? vfli_ram[color_addr2] : mem_ram[color_addr];
238:        b = mem_ram[addr];
239:        c = vflimod_enabled ? vfli_ram[color_addr2] : mem_ram[color_addr];
243:        b = mem_ram[addr];
247:        b = vflimod_enabled ? vfli_ram[color_addr2] : mem_ram[color_addr];
252:        c = vflimod_enabled ? vfli_ram[color_addr2] : mem_ram[color_addr]; /* FIXME: is this correct? */

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-snapshot.c
89:    if (SMW_BA(m, mem_ram + 0x9400, 0x400) < 0) {
163:    if (SMR_BA(m, mem_ram + 0x9400, 0x400) < 0) {

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic.c
284:    screenshot->screen_ptr = (vic.regs[0x02] & 0x80) ? mem_ram + 0x1e00 : mem_ram + 0x1000;
299:            screenshot->chargen_ptr = mem_ram + 0x9400;
</workspace_result>

### Turn 90 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\mem.h
45:extern uint8_t mem_ram[];
</workspace_result>

### Turn 91 (Assistant)
Implementing the Vic20 hosted oracle: patching CPU cores, adding a slim Vic20 shim + build script, then wiring managed multi-DLL routing.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20cpu.c
- old_string: #include "vice.h"

#include <stdio.h>

#include "monitor.h"
#include "vic-cycle.h"

/* ------------------------------------------------------------------------- */

/* Global clock counter.  */
CLOCK maincpu_clk = 0L;
/* if != 0, exit when this many cycles have been executed */
CLOCK maincpu_clk_limit = 0L;

#define REWIND_FETCH_OPCODE(clock) /*clock-=2*/

#define CLK_INC() maincpu_clk++; vic_cycle()

#define CLK_ADD(clock, amount) nosuchfunction(&clock, amount)
- new_string: #include "vice.h"

#include <stdio.h>

#include "monitor.h"
#include "vic-cycle.h"

#ifdef VICE_SHIM_HOSTED
#include "../vice-shim-runtime.h"
#endif

/* ------------------------------------------------------------------------- */

/* Global clock counter.  */
CLOCK maincpu_clk = 0L;
/* if != 0, exit when this many cycles have been executed */
CLOCK maincpu_clk_limit = 0L;

#define REWIND_FETCH_OPCODE(clock) /*clock-=2*/

#ifdef VICE_SHIM_HOSTED
/* Hosted build (ViceSharp): after the normal per-cycle VIC-I advance, publish
   the CPU register file to the shim, let the host pause/stop at this cycle
   boundary, then re-import in case the host adjusted state. A stop request
   unwinds maincpu_mainloop() (return) so the hosting thread regains control.
   EXPORT_REGISTERS()/IMPORT_REGISTERS() are in scope here because CLK_INC()
   only expands inside the 6510dtvcore.c body, after those macros are defined. */
#define CLK_INC()                                      \
    do {                                               \
        maincpu_clk++;                                 \
        vic_cycle();                                   \
        EXPORT_REGISTERS();                            \
        if (vice_shim_cycle_checkpoint()) {            \
            return;                                    \
        }                                              \
        IMPORT_REGISTERS();                            \
    } while (0)
#else
#define CLK_INC() maincpu_clk++; vic_cycle()
#endif

#define CLK_ADD(clock, amount) nosuchfunction(&clock, amount)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
- old_string: #include "resources.h"
#include "cmdline.h"
#include "traps.h"
#include "types.h"

#ifndef EXIT_FAILURE
#define EXIT_FAILURE 1
#endif
- new_string: #include "resources.h"
#include "cmdline.h"
#include "traps.h"
#include "types.h"

#ifdef VICE_SHIM_HOSTED
#include "vice-shim-runtime.h"
#endif

#ifndef EXIT_FAILURE
#define EXIT_FAILURE 1
#endif
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c
- old_string:     /*
     * Enable maincpu_resync_limits functionality .. in the old code
     * this is where the local stack var had its address copied to
     * the global.
     */
    bank_base_ready = true;

    machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);

    while (1) {
#define CPU_LOG_ID maincpu_log
#define ANE_LOG_LEVEL ane_log_level
#define LXA_LOG_LEVEL lxa_log_level
#define CPU_IS_JAMMED maincpu_jammed
#define CLK maincpu_clk
#define RMW_FLAG maincpu_rmw_flag
#define LAST_OPCODE_INFO last_opcode_info
#define LAST_OPCODE_ADDR last_opcode_addr
#define TRACEFLG debug.maincpu_traceflg

#define CPU_INT_STATUS maincpu_int_status

#define ALARM_CONTEXT maincpu_alarm_context

#define CHECK_PENDING_ALARM() (clk >= next_alarm_clk(maincpu_int_status))

#define CHECK_PENDING_INTERRUPT() check_pending_interrupt(maincpu_int_status)

#define TRAP(addr) maincpu_int_status->trap_func(addr);

#define ROM_TRAP_HANDLER() traps_handler()

#define JAM()                                                         \
    do {                                                              \
        unsigned int tmp;                                             \
                                                                      \
        EXPORT_REGISTERS();                                           \
        tmp = machine_jam("   " CPU_STR ": JAM at $%04X   ", reg_pc); \
        switch (tmp) {                                                \
            case JAM_RESET_CPU:                                       \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_POWER_CYCLE:                                     \
                machine_powerup();                                    \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_MONITOR:                                         \
                monitor_startup(e_comp_space);                        \
                IMPORT_REGISTERS();                                   \
                break;                                                \
            default:                                                  \
                CLK_INC();                                            \
        }                                                             \
    } while (0)

#define CALLER e_comp_space

#define ROM_TRAP_ALLOWED() mem_rom_trap_allowed((uint16_t)reg_pc)

#define GLOBAL_REGS maincpu_regs

#include "6510dtvcore.c"
- new_string:     /*
     * Enable maincpu_resync_limits functionality .. in the old code
     * this is where the local stack var had its address copied to
     * the global.
     */
    bank_base_ready = true;

#ifdef VICE_SHIM_HOSTED
    /* Hosted build (ViceSharp): the host bootstraps the CPU explicitly. When a
       bootstrap is pending, adopt the host-provided register file and clear the
       per-run CPU bookkeeping instead of triggering the normal power-on reset.
       IMPORT_REGISTERS() is not yet defined at this point in the translation
       unit (6510dtvcore.c defines it below, inside the loop), so the import is
       inlined here. reg_a_write/reg_a_read are #defined to reg_a in the
       non-DTV core, so the locals below are the CPU's real register storage. */
    if (vice_shim_take_bootstrap_maincpu()) {
        reg_a = maincpu_regs.a;
        reg_x = maincpu_regs.x;
        reg_y = maincpu_regs.y;
        reg_sp = maincpu_regs.sp;
        reg_p = maincpu_regs.p;
        flag_n = maincpu_regs.n;
        flag_z = maincpu_regs.z;
        bank_start = bank_limit = 0; /* prevent caching */
        JUMP(maincpu_regs.pc);
        maincpu_jammed = 0;
        last_opcode_info = 0;
        last_opcode_addr = reg_pc;
        stolen_cycles = 0;
        maincpu_rmw_flag = 0;
    } else {
        machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);
    }
#else
    machine_trigger_reset(MACHINE_RESET_MODE_RESET_CPU);
#endif

    while (1) {
#define CPU_LOG_ID maincpu_log
#define ANE_LOG_LEVEL ane_log_level
#define LXA_LOG_LEVEL lxa_log_level
#define CPU_IS_JAMMED maincpu_jammed
#define CLK maincpu_clk
#define RMW_FLAG maincpu_rmw_flag
#define LAST_OPCODE_INFO last_opcode_info
#define LAST_OPCODE_ADDR last_opcode_addr
#define TRACEFLG debug.maincpu_traceflg

#define CPU_INT_STATUS maincpu_int_status

#define ALARM_CONTEXT maincpu_alarm_context

#define CHECK_PENDING_ALARM() (clk >= next_alarm_clk(maincpu_int_status))

#define CHECK_PENDING_INTERRUPT() check_pending_interrupt(maincpu_int_status)

#define TRAP(addr) maincpu_int_status->trap_func(addr);

#define ROM_TRAP_HANDLER() traps_handler()

#define JAM()                                                         \
    do {                                                              \
        unsigned int tmp;                                             \
                                                                      \
        EXPORT_REGISTERS();                                           \
        tmp = machine_jam("   " CPU_STR ": JAM at $%04X   ", reg_pc); \
        switch (tmp) {                                                \
            case JAM_RESET_CPU:                                       \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_POWER_CYCLE:                                     \
                machine_powerup();                                    \
                DO_INTERRUPT(IK_RESET);                               \
                break;                                                \
            case JAM_MONITOR:                                         \
                monitor_startup(e_comp_space);                        \
                IMPORT_REGISTERS();                                   \
                break;                                                \
            default:                                                  \
                CLK_INC();                                            \
        }                                                             \
    } while (0)

#define CALLER e_comp_space

#define ROM_TRAP_ALLOWED() mem_rom_trap_allowed((uint16_t)reg_pc)

#define GLOBAL_REGS maincpu_regs

#include "6510dtvcore.c"

### Turn 92 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c has been updated successfully.

### Turn 93 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20cpu.c has been updated successfully.

### Turn 94 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c has been updated successfully.

### Turn 95 (Assistant)
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\native\vice-shim-vic20.c
- content: /*
 * ViceSharp hosted xvic shim (Iteration 2 / TR-VIC20-LOCKSTEP-001).
 * Separate DLL (vice_xvic.dll) from the C64 oracle (vice_x64.dll) because
 * VICE machine_class / maincpu / chip globals are process-global per binary.
 *
 * Surface matches the subset of native/vice-shim.h that Vic20 lockstep needs:
 * create/destroy/reset/step, CPU regs, memory peek/read/write, disk attach,
 * keyboard matrix, VIC-I timing via vice_vic_get_state (cycle + raster + regs).
 * CIA/SID/VIC-II-specific APIs are stubs that zero/no-op so a shared managed
 * binding can load either DLL without missing entry points.
 */

#include "vice-shim.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>

#include <ctype.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "archdep.h"
#include "archdep_set_openmp_wait_policy.h"
#include "archdep_startup_log_error.h"
#include "archdep_tick.h"
#include "attach.h"
#include "drive.h"
#include "drivetypes.h"
#include "gfxoutput.h"
#include "init.h"
#include "interrupt.h"
#include "keyboard.h"
#include "lib.h"
#include "log.h"
#include "machine.h"
#include "maincpu.h"
#include "main.h"
#include "mem.h"
#include "mos6510.h"
#include "resources.h"
#include "screenshot.h"
#include "snapshot.h"
#include "sysfile.h"
#include "video.h"
#include "vic20/vic20model.h"
#include "vic20/victypes.h"
#include "vic20/vic.h"
#include "vice-shim-runtime.h"

int archdep_init(int *argc, char **argv);

typedef struct vice_machine_s {
    uint32_t magic;
    int vic20_model;
} vice_machine_t;

enum {
    VICE_MACHINE_MAGIC = 0x56494345u
};

static INIT_ONCE g_sync_once = INIT_ONCE_STATIC_INIT;
static CRITICAL_SECTION g_state_lock;
static CONDITION_VARIABLE g_state_cv;

static vice_machine_t *g_active_machine;
static HANDLE g_worker_thread;
static int g_worker_running;
static int g_runtime_initialized;
static int g_runtime_failed;
static int g_bootstrap_pending;
static int g_stop_requested;
static unsigned int g_granted_cycles;
static int g_cycle_paused;
static unsigned int g_debug_step_calls;
static unsigned int g_debug_checkpoint_calls;
static unsigned int g_debug_create_calls;
static unsigned int g_debug_reset_calls;
#define VICE_SHIM_CREATE_TIMEOUT_MS 5000u
#define VICE_SHIM_STEP_TIMEOUT_MS 5000u
#define VICE_SHIM_STOP_TIMEOUT_MS 2000u

extern CLOCK stolen_cycles;

static int vice_shim_model_from_selector(const char *selector);
static int vice_shim_selector_equals(const char *left, const char *right);
static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive);
static void vice_shim_set_current_thread_description(const wchar_t *description);

static BOOL CALLBACK vice_shim_initialize_sync(PINIT_ONCE init_once, PVOID parameter, PVOID *context)
{
    (void)init_once;
    (void)parameter;
    (void)context;

    InitializeCriticalSection(&g_state_lock);
    InitializeConditionVariable(&g_state_cv);
    return TRUE;
}

static void vice_shim_ensure_sync_primitives(void)
{
    InitOnceExecuteOnce(&g_sync_once, vice_shim_initialize_sync, NULL, NULL);
}

static int vice_shim_is_active_machine(const void *machine)
{
    const vice_machine_t *instance = (const vice_machine_t *)machine;
    return instance != NULL
        && instance->magic == VICE_MACHINE_MAGIC
        && instance == g_active_machine;
}

static int vice_shim_wait_for_signal_with_timeout(unsigned int timeout_ms)
{
    return SleepConditionVariableCS(&g_state_cv, &g_state_lock, (DWORD)timeout_ms) != 0;
}

static int vice_shim_get_module_directory(char *buffer, size_t buffer_size)
{
    HMODULE module = NULL;
    DWORD length;
    char *separator;

    if (!GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                            (LPCSTR)&vice_machine_create,
                            &module)) {
        return 0;
    }

    length = GetModuleFileNameA(module, buffer, (DWORD)buffer_size);
    if (length == 0 || length >= buffer_size) {
        return 0;
    }

    separator = strrchr(buffer, '\\');
    if (separator == NULL) {
        separator = strrchr(buffer, '/');
    }

    if (separator == NULL) {
        return 0;
    }

    *separator = '\0';
    return 1;
}

static int vice_shim_compose_path(char *buffer,
                                  size_t buffer_size,
                                  const char *base,
                                  const char *relative_path)
{
    int written = snprintf(buffer, buffer_size, "%s\\%s", base, relative_path);
    return written > 0 && (size_t)written < buffer_size;
}

static int vice_shim_initialize_runtime_locked(void)
{
    char module_directory[MAX_PATH];
    char program_path[MAX_PATH];
    char data_directory[MAX_PATH];
    char *argv[1];
    int argc = 1;

    if (g_runtime_initialized) {
        return 1;
    }

    if (g_runtime_failed) {
        return 0;
    }

    if (!vice_shim_get_module_directory(module_directory, sizeof(module_directory))
        || !vice_shim_compose_path(program_path, sizeof(program_path), module_directory, "vice\\vice\\src\\xvic.exe")
        || !vice_shim_compose_path(data_directory, sizeof(data_directory), module_directory, "vice\\vice\\data")) {
        g_runtime_failed = 1;
        return 0;
    }

    console_mode = true;
    default_settings_requested = true;
    help_requested = false;
    video_disabled_mode = false;

    archdep_set_openmp_wait_policy();
    lib_init();
    log_set_limit_early(LOG_LIMIT_STANDARD);

    argv[0] = program_path;
    if (archdep_init(&argc, argv) != 0) {
        archdep_startup_log_error("archdep_init failed.\n");
        g_runtime_failed = 1;
        return 0;
    }

    /* xvic binary: machine_class is already VICE_MACHINE_VIC20 via vic20.c. */
    machine_class = VICE_MACHINE_VIC20;

    tick_init();
    maincpu_early_init();
    machine_setup_context();
    drive_setup_context();
    machine_early_init();
    sysfile_init(machine_name);

    if (init_resources() < 0
        || init_cmdline_options() < 0
        || gfxoutput_early_init((int)help_requested) < 0
        || gfxoutput_resources_init() < 0
        || gfxoutput_cmdline_options_init() < 0
        || screenshot_cmdline_options_init() < 0
        || resources_set_defaults() < 0
        || resources_set_int("RAMInitRandomChance", 0) < 0
        || resources_set_string("Directory", data_directory) < 0
        || log_init() < 0
        || video_init() < 0
        || init_main() < 0) {
        g_runtime_failed = 1;
        return 0;
    }

    g_runtime_initialized = 1;
    return 1;
}

static unsigned __stdcall vice_shim_worker_main(void *parameter)
{
    (void)parameter;

    vice_shim_set_current_thread_description(L"ViceSharp.NativeViceShim.Xvic");

    maincpu_mainloop();

    EnterCriticalSection(&g_state_lock);
    g_worker_running = 0;
    g_cycle_paused = 1;
    WakeAllConditionVariable(&g_state_cv);
    LeaveCriticalSection(&g_state_lock);

    return 0;
}

typedef HRESULT (WINAPI *vice_shim_set_thread_description_fn)(HANDLE thread, PCWSTR description);

static void vice_shim_set_current_thread_description(const wchar_t *description)
{
    HMODULE kernel32;
    vice_shim_set_thread_description_fn set_thread_description;

    kernel32 = GetModuleHandleW(L"Kernel32.dll");
    if (kernel32 == NULL) {
        return;
    }

    set_thread_description = (vice_shim_set_thread_description_fn)GetProcAddress(kernel32, "SetThreadDescription");
    if (set_thread_description == NULL) {
        return;
    }

    (void)set_thread_description(GetCurrentThread(), description);
}

static int vice_shim_stop_worker(void *machine)
{
    HANDLE worker = NULL;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine) || g_worker_thread == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return 1;
    }

    g_stop_requested = 1;
    WakeAllConditionVariable(&g_state_cv);
    worker = g_worker_thread;
    LeaveCriticalSection(&g_state_lock);

    if (WaitForSingleObject(worker, VICE_SHIM_STOP_TIMEOUT_MS) != WAIT_OBJECT_0) {
        return 0;
    }
    CloseHandle(worker);

    EnterCriticalSection(&g_state_lock);
    if (g_worker_thread == worker) {
        g_worker_thread = NULL;
    }
    g_worker_running = 0;
    g_stop_requested = 0;
    g_granted_cycles = 0;
    g_cycle_paused = 0;
    LeaveCriticalSection(&g_state_lock);

    return 1;
}

static void vice_shim_reset_cpu_state_locked(void)
{
    const uint16_t reset_vector = (uint16_t)(mem_read(0xfffc) | (mem_read(0xfffd) << 8));

    maincpu_clk_limit = 0;
    stolen_cycles = 0;
    maincpu_rmw_flag = 0;
    last_opcode_info = 0;
    last_opcode_addr = reset_vector;

    maincpu_regs.a = 0;
    maincpu_regs.x = 0;
    maincpu_regs.y = 0;
    maincpu_regs.sp = 0;
    maincpu_regs.p = P_INTERRUPT;
    maincpu_regs.n = 0;
    maincpu_regs.z = 0;
    maincpu_regs.pc = reset_vector;

    g_bootstrap_pending = 1;
}

static void vice_shim_rebaseline_drives_locked(void)
{
    unsigned int unit;
    char resource_name[32];
    unsigned int clk_unit, clk_drive;

    file_system_detach_disk_all();

    for (unit = 8; unit <= 11; unit++) {
        snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
        resources_set_int(resource_name, 1);
    }

    for (clk_unit = 0; clk_unit < NUM_DISK_UNITS; clk_unit++) {
        if (diskunit_context[clk_unit] == NULL) {
            continue;
        }
        for (clk_drive = 0; clk_drive < NUM_DRIVES; clk_drive++) {
            drive_t *clk_dptr = diskunit_context[clk_unit]->drives[clk_drive];
            if (clk_dptr != NULL) {
                clk_dptr->attach_clk = 0;
                clk_dptr->detach_clk = 0;
                clk_dptr->attach_detach_clk = 0;
            }
        }
    }
}

VICE_SHIM_API void *vice_machine_create(void)
{
    return vice_machine_create_model(NULL);
}

VICE_SHIM_API void *vice_machine_create_model(const char *model_selector)
{
    if (g_debug_create_calls < 8) {
        fprintf(stderr, "xvic vice_machine_create call=%u model=%s\n",
                ++g_debug_create_calls,
                model_selector == NULL ? "default" : model_selector);
        fflush(stderr);
    }

    vice_machine_t *machine;
    int model;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    while (g_active_machine != NULL) {
        SleepConditionVariableCS(&g_state_cv, &g_state_lock, INFINITE);
    }

    if (!vice_shim_initialize_runtime_locked()) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    model = vice_shim_model_from_selector(model_selector);
    if (model == VIC20MODEL_UNKNOWN) {
        fprintf(stderr, "xvic vice_machine_create unknown model=%s\n",
                model_selector == NULL ? "(null)" : model_selector);
        fflush(stderr);
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    vic20model_set(model);
    vice_shim_rebaseline_drives_locked();

    machine = (vice_machine_t *)calloc(1, sizeof(*machine));
    if (machine == NULL) {
        LeaveCriticalSection(&g_state_lock);
        return NULL;
    }

    machine->magic = VICE_MACHINE_MAGIC;
    machine->vic20_model = model;
    g_active_machine = machine;
    LeaveCriticalSection(&g_state_lock);

    return machine;
}

static int vice_shim_model_from_selector(const char *selector)
{
    if (selector == NULL || selector[0] == '\0'
        || vice_shim_selector_equals(selector, "vic20")
        || vice_shim_selector_equals(selector, "vic20pal")
        || vice_shim_selector_equals(selector, "pal")
        || vice_shim_selector_equals(selector, "xvic")) {
        return VIC20MODEL_VIC20_PAL;
    }

    if (vice_shim_selector_equals(selector, "vic20ntsc")
        || vice_shim_selector_equals(selector, "ntsc")) {
        return VIC20MODEL_VIC20_NTSC;
    }

    if (vice_shim_selector_equals(selector, "vic21")
        || vice_shim_selector_equals(selector, "supervic")) {
        return VIC20MODEL_VIC21;
    }

    if (vice_shim_selector_equals(selector, "vic1001")
        || vice_shim_selector_equals(selector, "jap")) {
        return VIC20MODEL_VIC1001;
    }

    return VIC20MODEL_UNKNOWN;
}

static int vice_shim_selector_equals(const char *left, const char *right)
{
    if (left == NULL || right == NULL) {
        return 0;
    }

    while (*left != '\0' && *right != '\0') {
        if (tolower((unsigned char)*left) != tolower((unsigned char)*right)) {
            return 0;
        }

        left++;
        right++;
    }

    return *left == '\0' && *right == '\0';
}

VICE_SHIM_API void vice_machine_destroy(void *machine)
{
    vice_machine_t *instance = (vice_machine_t *)machine;

    if (instance == NULL) {
        return;
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_active_machine == instance) {
        file_system_detach_disk_all();
        g_active_machine = NULL;
        g_bootstrap_pending = 0;
        WakeAllConditionVariable(&g_state_cv);
    }
    LeaveCriticalSection(&g_state_lock);

    instance->magic = 0;
    free(instance);
}

VICE_SHIM_API void vice_machine_reset(void *machine)
{
    if (g_debug_reset_calls < 8) {
        fprintf(stderr, "xvic vice_machine_reset call=%u machine=%p\n", ++g_debug_reset_calls, machine);
        fflush(stderr);
    }

    vice_shim_stop_worker(machine);

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    {
        vice_machine_t *instance = (vice_machine_t *)machine;
        vic20model_set(instance->vic20_model);
    }

    machine_powerup();
    mem_powerup();
    vic_reset();
    maincpu_reset();
    vice_shim_reset_cpu_state_locked();
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_read_snapshot(void *machine, const char *path)
{
    int result;

    if (path == NULL) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    {
        vice_machine_t *instance = (vice_machine_t *)machine;
        vic20model_set(instance->vic20_model);
    }
    result = machine_read_snapshot(path, 0);
    if (result == 0) {
        snapshot_set_error(SNAPSHOT_NO_ERROR);
    }
    g_bootstrap_pending = 1;

    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_machine_write_snapshot(void *machine, const char *path)
{
    int result;

    if (path == NULL) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    result = machine_write_snapshot(path, 1, 1, 0);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_snapshot_last_error(void)
{
    return snapshot_get_error();
}

VICE_SHIM_API int vice_machine_attach_cartridge(void *machine, const uint8_t *image, int length, int mapping_mode)
{
    (void)machine;
    (void)image;
    (void)length;
    (void)mapping_mode;
    /* Vic20 cart attach is a later slice; fail closed for now. */
    return -1;
}

VICE_SHIM_API int vice_machine_attach_disk(void *machine, unsigned int unit, unsigned int drive, const char *path)
{
    int result;

    if (path == NULL || !vice_shim_valid_disk_slot(unit, drive)) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    result = file_system_attach_disk(unit, drive, path);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_machine_detach_disk(void *machine, unsigned int unit, unsigned int drive)
{
    if (!vice_shim_valid_disk_slot(unit, drive)) {
        return -1;
    }

    vice_shim_stop_worker(machine);
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    file_system_detach_disk(unit, drive);
    LeaveCriticalSection(&g_state_lock);
    return 0;
}

static int vice_shim_valid_disk_slot(unsigned int unit, unsigned int drive)
{
    return unit >= 8 && unit <= 11 && drive <= 1;
}

VICE_SHIM_API uint8_t vice_machine_peek_ram(void *machine, uint16_t address)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = mem_ram[address];
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_machine_read(void *machine, uint16_t address)
{
    uint8_t value = 0xff;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = mem_read(address);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API void vice_machine_write(void *machine, uint16_t address, uint8_t value)
{
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        mem_store(address, value);
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_get_model(void *machine)
{
    int model = VIC20MODEL_UNKNOWN;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        model = vic20model_get();
    }
    LeaveCriticalSection(&g_state_lock);

    return model;
}

VICE_SHIM_API int vice_machine_set_keyboard_matrix_key(void *machine, int row, int column, int pressed)
{
    if (row < 0 || row > 7 || column < 0 || column > 7) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    keyboard_set_keyarr(row, column, pressed != 0);
    LeaveCriticalSection(&g_state_lock);
    return 0;
}

VICE_SHIM_API void vice_machine_cia1_store(void *machine, uint8_t register_index, uint8_t value)
{
    (void)machine;
    (void)register_index;
    (void)value;
}

VICE_SHIM_API uint8_t vice_machine_cia1_read(void *machine, uint8_t register_index)
{
    (void)machine;
    (void)register_index;
    return 0xff;
}

VICE_SHIM_API void vice_machine_step_cycle(void *machine)
{
    if (g_debug_step_calls < 16) {
        fprintf(stderr, "xvic vice_machine_step_cycle call=%u machine=%p\n", ++g_debug_step_calls, machine);
        fflush(stderr);
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return;
    }

    if (g_worker_thread == NULL || !g_worker_running) {
        if (g_worker_thread != NULL) {
            CloseHandle(g_worker_thread);
            g_worker_thread = NULL;
            g_worker_running = 0;
            g_cycle_paused = 1;
            g_stop_requested = 0;
            g_granted_cycles = 0;
        }

        {
            uintptr_t worker_handle;

            g_stop_requested = 0;
            g_granted_cycles = 1;
            g_cycle_paused = 0;
            g_worker_running = 1;

            worker_handle = _beginthreadex(NULL, 0, vice_shim_worker_main, NULL, 0, NULL);
            if (worker_handle == 0) {
                g_worker_running = 0;
                g_granted_cycles = 0;
                LeaveCriticalSection(&g_state_lock);
                return;
            }

            g_worker_thread = (HANDLE)worker_handle;
        }
    } else {
        g_granted_cycles++;
        g_cycle_paused = 0;
        WakeAllConditionVariable(&g_state_cv);
    }

    while (g_worker_running && !g_cycle_paused) {
        if (!vice_shim_wait_for_signal_with_timeout(VICE_SHIM_STEP_TIMEOUT_MS)) {
            g_stop_requested = 1;
            g_granted_cycles = 0;
            g_cycle_paused = 1;
            WakeAllConditionVariable(&g_state_cv);
            break;
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

int vice_shim_cycle_checkpoint(void)
{
    int should_stop;

    if (g_debug_checkpoint_calls < 16) {
        fprintf(stderr, "xvic cycle_checkpoint: granted=%u running=%d paused=%d stop=%d\n",
                g_granted_cycles, g_worker_running, g_cycle_paused, g_stop_requested);
        fflush(stderr);
        ++g_debug_checkpoint_calls;
    }

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (g_granted_cycles > 0) {
        g_granted_cycles--;
    }

    if (g_granted_cycles == 0) {
        g_cycle_paused = 1;
        WakeAllConditionVariable(&g_state_cv);
        while (!g_stop_requested && g_granted_cycles == 0) {
            if (!vice_shim_wait_for_signal_with_timeout(VICE_SHIM_STEP_TIMEOUT_MS)) {
                g_stop_requested = 1;
                g_granted_cycles = 0;
                g_cycle_paused = 1;
                WakeAllConditionVariable(&g_state_cv);
                break;
            }
        }
    }

    g_cycle_paused = 0;
    should_stop = g_stop_requested;
    LeaveCriticalSection(&g_state_lock);

    return should_stop;
}

int vice_shim_take_bootstrap_maincpu(void)
{
    int bootstrap_pending;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    bootstrap_pending = g_bootstrap_pending;
    g_bootstrap_pending = 0;
    LeaveCriticalSection(&g_state_lock);

    return bootstrap_pending;
}

VICE_SHIM_API uint8_t vice_cpu_get_a(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.a;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_x(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.x;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_y(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.y;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_p(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.p;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_sp(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.sp;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API uint16_t vice_cpu_get_pc(void *machine)
{
    uint16_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.pc;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}

VICE_SHIM_API void vice_cpu_get_pipeline_state(void *machine, struct vice_cpu_pipeline_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        state->clk = (uint64_t)maincpu_clk;
        state->last_opcode_info = (uint32_t)last_opcode_info;
        /* Vic20 has no BA-low / 6510 pport; leave those zero. */
        if (maincpu_int_status != NULL) {
            state->global_pending_int = (uint32_t)maincpu_int_status->global_pending_int;
            state->irq_clk = (uint64_t)maincpu_int_status->irq_clk;
            state->nmi_clk = (uint64_t)maincpu_int_status->nmi_clk;
            state->irq_delay_cycles = (uint64_t)maincpu_int_status->irq_delay_cycles;
            state->nmi_delay_cycles = (uint64_t)maincpu_int_status->nmi_delay_cycles;
        }
    }
    LeaveCriticalSection(&g_state_lock);
}

/* Drive CPU stubs: present for ABI parity; return zero until true-drive Vic20
   lockstep is wired. */
VICE_SHIM_API uint8_t vice_drivecpu_get_a(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_x(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_y(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_p(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint8_t vice_drivecpu_get_sp(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API uint16_t vice_drivecpu_get_pc(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API int vice_drive_get_clock_residue(void *machine, unsigned int unit, uint64_t *attach_clk, uint64_t *detach_clk, uint64_t *attach_detach_clk)
{
    (void)machine;
    (void)unit;
    if (attach_clk) {
        *attach_clk = 0;
    }
    if (detach_clk) {
        *detach_clk = 0;
    }
    if (attach_detach_clk) {
        *attach_detach_clk = 0;
    }
    return -1;
}

VICE_SHIM_API uint64_t vice_drivecpu_get_cycle_accum(void *machine, unsigned int unit)
{
    (void)machine;
    (void)unit;
    return 0;
}

VICE_SHIM_API int vice_drivecpu_set_cycle_accum(void *machine, unsigned int unit, uint64_t value)
{
    (void)machine;
    (void)unit;
    (void)value;
    return -1;
}

VICE_SHIM_API int vice_drive_set_true_emulation(void *machine, unsigned int unit, int enabled)
{
    char resource_name[32];
    int result;

    if (unit < 8 || unit > 11) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
    result = resources_set_int(resource_name, enabled != 0 ? 1 : 0);
    LeaveCriticalSection(&g_state_lock);
    return result;
}

VICE_SHIM_API int vice_drive_get_true_emulation(void *machine, unsigned int unit)
{
    char resource_name[32];
    int value = 0;

    if (unit < 8 || unit > 11) {
        return -1;
    }

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
        LeaveCriticalSection(&g_state_lock);
        return -2;
    }

    snprintf(resource_name, sizeof(resource_name), "Drive%uTrueEmulation", unit);
    (void)resources_get_int(resource_name, &value);
    LeaveCriticalSection(&g_state_lock);
    return value;
}

/*
 * Reuse the C64-shaped vice_vic_state export for Vic-I timing so managed
 * GetVicState / cycle baselining works without a second ABI. Raster fields
 * map from the VIC-I core; unused VIC-II fields stay zero.
 */
VICE_SHIM_API void vice_vic_get_state(void *machine, struct vice_vic_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        state->cycle = (uint32_t)maincpu_clk;
        state->raster_line = (uint16_t)vic.raster_line;
        state->raster_cycle = (uint8_t)vic.raster_cycle;
        state->bad_line = 0;
        state->display_state = (uint8_t)vic.area;
        state->sprite_dma = 0;
        state->allow_bad_lines = 0;
        state->idle_state = (uint8_t)vic.fetch_state;
        memcpy(state->registers, vic.regs, sizeof(vic.regs));
        memcpy(state->registers_peek, vic.regs, sizeof(vic.regs));
    }
    LeaveCriticalSection(&g_state_lock);
}

VICE_SHIM_API int vice_machine_capture_visible_frame(void *machine, uint8_t *buffer, int length, int *width, int *height)
{
    (void)machine;
    (void)buffer;
    (void)length;
    (void)width;
    (void)height;
    return 0;
}

VICE_SHIM_API int vice_vic_capture_frame_indices(void *machine, uint8_t *buffer, int length, int *width, int *height)
{
    (void)machine;
    (void)buffer;
    (void)length;
    (void)width;
    (void)height;
    return 0;
}

VICE_SHIM_API int vice_vic_get_graphics_priority_at_raster(void *machine, uint16_t raster_line, uint8_t *pri_buffer, int length)
{
    (void)machine;
    (void)raster_line;
    (void)pri_buffer;
    (void)length;
    return 0;
}

VICE_SHIM_API void vice_cia_get_state(void *machine, int cia_index, struct vice_cia_state *state)
{
    (void)machine;
    (void)cia_index;
    if (state != NULL) {
        memset(state, 0, sizeof(*state));
    }
}

VICE_SHIM_API void vice_sid_get_state(void *machine, struct vice_sid_state *state)
{
    (void)machine;
    if (state != NULL) {
        memset(state, 0, sizeof(*state));
    }
}

VICE_SHIM_API size_t vice_sid_render_samples(void *machine, int16_t *buffer, size_t n, int delta_t_cycles)
{
    (void)machine;
    (void)buffer;
    (void)n;
    (void)delta_t_cycles;
    return 0;
}

VICE_SHIM_API uint8_t vice_sid_engine_read(void *machine, uint16_t addr)
{
    (void)machine;
    (void)addr;
    return 0xff;
}

VICE_SHIM_API void vice_sid_clock(void *machine, int cycles)
{
    (void)machine;
    (void)cycles;
}

VICE_SHIM_API int vice_sid_exact_open(void *machine)
{
    (void)machine;
    return 0;
}

VICE_SHIM_API void vice_sid_exact_reset(void *machine)
{
    (void)machine;
}

VICE_SHIM_API int vice_sid_exact_clock(void *machine, int cycles)
{
    (void)machine;
    (void)cycles;
    return 0;
}

VICE_SHIM_API void vice_sid_exact_write(void *machine, uint16_t addr, uint8_t value)
{
    (void)machine;
    (void)addr;
    (void)value;
}

VICE_SHIM_API uint8_t vice_sid_exact_read(void *machine, uint16_t addr)
{
    (void)machine;
    (void)addr;
    return 0xff;
}

VICE_SHIM_API int16_t vice_sid_exact_output(void *machine)
{
    (void)machine;
    return 0;
}

VICE_SHIM_API void vice_sid_exact_get_state(void *machine, struct vice_sid_exact_state *state)
{
    (void)machine;
    if (state != NULL) {
        memset(state, 0, sizeof(*state));
    }
}

VICE_SHIM_API int vice_sid_exact_set_sampling(void *machine, int method, double sample_freq, double pass_freq, double filter_scale)
{
    (void)machine;
    (void)method;
    (void)sample_freq;
    (void)pass_freq;
    (void)filter_scale;
    return 0;
}

VICE_SHIM_API int vice_sid_exact_clock_buffered(void *machine, int cycles, int16_t *buffer, int buffer_len, int *cycles_remaining)
{
    (void)machine;
    (void)cycles;
    (void)buffer;
    (void)buffer_len;
    if (cycles_remaining != NULL) {
        *cycles_remaining = cycles;
    }
    return 0;
}

VICE_SHIM_API void vice_interrupt_get_state(void *machine, struct vice_interrupt_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine) && maincpu_int_status != NULL) {
        state->global_pending = (uint8_t)maincpu_int_status->global_pending_int;
        state->irq_asserted = (uint8_t)((maincpu_int_status->global_pending_int & IK_IRQ) != 0);
        state->nmi_asserted = (uint8_t)((maincpu_int_status->global_pending_int & IK_NMI) != 0);
    }
    LeaveCriticalSection(&g_state_lock);
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\native\build-vice-shim-xvic.sh
- content: #!/usr/bin/env bash
# Build vice_xvic.dll: hosted xvic oracle for ViceSharp Vic20 lockstep.
# Mirrors build-vice-shim.sh (C64 / vice_x64.dll) but links xvic objects and
# compiles vic20cpu.c with -DVICE_SHIM_HOSTED.
set -euo pipefail

script_dir=$(cd "$(dirname "$0")" && pwd)
vice_root="$script_dir/vice/vice"
vice_src="$vice_root/src"
vice_shim_root="$script_dir"
shim_dll="$script_dir/vice_xvic.dll"
vice_patch="$script_dir/patches/vice-shim-runtime.patch"
vice_prompt="$script_dir/patches/vice-shim-runtime.prompt.md"
mingw_bin=$(dirname "$(command -v gcc)")
tmp_makefile=$(mktemp)
vice_shim_include_flags="-I. -I$vice_shim_root -I$vice_src -I$vice_src/sid -I$vice_src/vic20"
linenoise_dir="$vice_src/lib/linenoise-ng"

cleanup() {
  rm -f "$tmp_makefile"
}

trap cleanup EXIT

while IFS= read -r include_dir; do
  vice_shim_include_flags="$vice_shim_include_flags -I$include_dir"
done < <(find "$vice_src" -mindepth 1 -maxdepth 3 -type d | sort)

if [[ -f "$vice_patch" ]]; then
  if git -C "$script_dir/vice" apply --reverse --check "$vice_patch" >/dev/null 2>&1; then
    :
  elif git -C "$script_dir/vice" apply --check "$vice_patch" >/dev/null 2>&1; then
    git -C "$script_dir/vice" apply "$vice_patch"
  else
    echo "VICE hosted runtime patch no longer applies cleanly."
    echo "Use the manual prompt at: $vice_prompt"
    exit 1
  fi
fi

if [[ ! -f "$vice_root/configure" ]]; then
  cd "$vice_root"
  ./autogen.sh
fi

vice_configure_flags=(
  --enable-option-checking=fatal
  --enable-headlessui
  --disable-arch
  --disable-html-docs
  --disable-pdf-docs
  --disable-catweasel
  --disable-hardsid
  --disable-ethernet
  --disable-midi
  --disable-parsid
  --disable-realdevice
  --disable-rs232
  --disable-openmp
  --disable-ipv6
  --with-resid
  --without-flac
  --without-gif
  --without-lame
  --without-mpg123
  --without-portaudio
  --without-vorbis
  --without-libcurl
  --without-libieee1284
  --without-unzip-bin
  --without-png
)

vice_configure_marker="$vice_src/.vice-shim-configure-flags"
if [[ ! -f "$vice_src/config.h" ]] \
   || [[ ! -f "$vice_configure_marker" ]] \
   || [[ "$(cat "$vice_configure_marker" 2>/dev/null)" != "${vice_configure_flags[*]}" ]]; then
  echo "Configuring VICE (config.h missing or shim configure flags changed)."
  cd "$vice_root"
  rm -f "$vice_src/config.h"
  ./configure "${vice_configure_flags[@]}"
  printf '%s' "${vice_configure_flags[*]}" > "$vice_configure_marker"
fi

if [[ -d "$linenoise_dir/.deps" ]]; then
  rm -f "$linenoise_dir/.deps"/*.Po
fi

make -C "$linenoise_dir"
# Ensure xvic objects exist (make xvic-program rebuilds stale targets).
# Hosted CPU overrides libvic20.a:vic20cpu.o at link time (object-before-archive).
rm -f "$vice_src/vic20/vic20cpu.o"
make -C "$vice_src" -j4 xvic-program

cat > "$tmp_makefile" <<EOF
VICE_SHIM_ROOT := $vice_shim_root
VICE_SHIM_INCLUDE_FLAGS := $vice_shim_include_flags
VICE_SHIM_OBJ := \$(VICE_SHIM_ROOT)/vice-shim-vic20-hosted.o
VICE_SHIM_MAINCPU_OBJ := \$(VICE_SHIM_ROOT)/mainviccpu-hosted.o
VICE_SHIM_DLL := \$(VICE_SHIM_ROOT)/vice_xvic.dll

\$(VICE_SHIM_OBJ): \$(VICE_SHIM_ROOT)/vice-shim-vic20.c \$(VICE_SHIM_ROOT)/vice-shim.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -c -o \$@ \$(VICE_SHIM_ROOT)/vice-shim-vic20.c

\$(VICE_SHIM_MAINCPU_OBJ): vic20/vic20cpu.c mainviccpu.c vice-shim-runtime.h
	\$(CC) \$(DEFS) \$(DEFAULT_INCLUDES) \$(INCLUDES) \$(AM_CPPFLAGS) \$(CPPFLAGS) \$(AM_CFLAGS) \$(CFLAGS) \$(VICE_SHIM_INCLUDE_FLAGS) -DVICE_SHIM_HOSTED -c -o \$@ vic20/vic20cpu.c

\$(VICE_SHIM_DLL): \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(xvic_OBJECTS) \$(xvic_DEPENDENCIES)
	\$(CCLD) \$(AM_CFLAGS) \$(CFLAGS) \$(xvic_LDFLAGS) \$(LDFLAGS) -shared -static-libgcc -o \$@ \$(VICE_SHIM_OBJ) \$(VICE_SHIM_MAINCPU_OBJ) \$(xvic_OBJECTS) \$(xvic_LDADD) \$(LIBS)
EOF

make -C "$vice_src" -s -f Makefile -f "$tmp_makefile" "$shim_dll"

for dep in libiconv-2.dll zlib1.dll libstdc++-6.dll libwinpthread-1.dll libgcc_s_seh-1.dll; do
  if [[ -f "$mingw_bin/$dep" ]]; then
    cp -f "$mingw_bin/$dep" "$script_dir/$dep"
  fi
done

echo "Built: $shim_dll"
ls -la "$shim_dll"


### Turn 96 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\native\build-vice-shim-xvic.sh has been created.

### Turn 97 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\native\vice-shim-vic20.c has been created.

### Turn 98 (Assistant)
[tool_request: grep]
- pattern: irq_delay|nmi_delay|irq_clk|nmi_clk|global_pending
- path: F:\GitHub\vice-sharp\native\vice\vice\src\interrupt.h
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 1352
- limit: 40
[tool_request: grep]
- pattern: keyboard_set_keyarr
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/*.{c,h}
- head_limit: 10

### Turn 99 (Function)
[tool_response]
1352→VICE_SHIM_API void vice_cpu_get_pipeline_state(void *machine, struct vice_cpu_pipeline_state *state)
{
    if (state == NULL) {
        return;
    }

    memset(state, 0, sizeof(*state));

1360→    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        state->clk = (uint64_t)maincpu_clk;
        state->last_opcode_info = (uint32_t)last_opcode_info;
        state->ba_low_flags = (uint32_t)maincpu_ba_low_flags;
        state->pport_data = pport.data;
        state->pport_dir = pport.dir;
        state->pport_data_read = pport.data_read;
1370→        state->pport_dir_read = pport.dir_read;
        if (maincpu_int_status != NULL) {
            state->global_pending_int = (uint32_t)maincpu_int_status->global_pending_int;
            state->irq_clk = (uint64_t)maincpu_int_status->irq_clk;
            state->nmi_clk = (uint64_t)maincpu_int_status->nmi_clk;
            state->irq_delay_cycles = (uint64_t)maincpu_int_status->irq_delay_cycles;
            state->nmi_delay_cycles = (uint64_t)maincpu_int_status->nmi_delay_cycles;
        }
    }
    LeaveCriticalSection(&g_state_lock);
1380→}

/*
 * Drive-CPU state accessors. Each takes a device number (8..11) and
 * returns the corresponding 1541/1571 drive-CPU register from the
 * shared diskunit_context[unit - DRIVE_UNIT_MIN]->cpu->cpu_regs slot.
 * Returns 0 when the machine is inactive, the unit is out of range,
 * or the drive context / CPU pointer is null.
 */

1390→static int vice_shim_valid_drive_unit_for_cpu(unsigned int unit)
{

### Turn 100 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 25 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\interrupt.h
70:    CLOCK irq_clk;
76:    CLOCK nmi_clk;
80:       calculate irq_clk).  */
86:    CLOCK irq_delay_cycles;
87:    CLOCK nmi_delay_cycles;
123:    unsigned int global_pending_int;
158:                cs->global_pending_int |= (unsigned int)(IK_IRQ | IK_IRQPEND);
171:                cs->irq_delay_cycles = 0;
174:                    cs->irq_clk = cpu_clk;
176:                    interrupt_fixup_int_clk(cs, cpu_clk, &(cs->irq_clk));
187:                    cs->global_pending_int =
188:                        (cs->global_pending_int & (unsigned int)~IK_IRQ);
209:            if (cs->nnmi == 0 && !(cs->global_pending_int & IK_NMI)) {
210:                cs->global_pending_int = (cs->global_pending_int | IK_NMI);
221:                cs->nmi_delay_cycles = 0;
224:                    cs->nmi_clk = cpu_clk;
226:                    interrupt_fixup_int_clk(cs, cpu_clk, &(cs->nmi_clk));
241:                if (cpu_clk == cs->nmi_clk) {
242:                    cs->global_pending_int = (enum cpu_int)(cs->global_pending_int & ~IK_NMI);
275:    cs->global_pending_int =
276:        (cs->global_pending_int & (unsigned int)~IK_NMI);
286:    cs->global_pending_int =
287:        (cs->global_pending_int & (unsigned int)~IK_IRQPEND);
336:#define maincpu_set_irq_clk(int_num, value, clk) \
342:#define maincpu_set_nmi_clk(int_num, value, clk) \
</workspace_result>

### Turn 101 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 7 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\keyboard.h
108:void keyboard_set_keyarr(int row, int col, int value);
109:void keyboard_set_keyarr_any(int row, int col, int value);

F:\GitHub\vice-sharp\native\vice\vice\src\keyboard.c
367:void keyboard_set_keyarr(int row, int col, int value)
369:    DBGMTX(("keyboard_set_keyarr       val:%d row:%d col:%d \n", value, row, col));
896:void keyboard_set_keyarr_any(int row, int col, int value)
900:    DBGKEY(("keyboard_set_keyarr_any val:%3d row:%d col:%d\n", value, row, col));
925:        keyboard_set_keyarr(row, col, value);
</workspace_result>

### Turn 102 (Assistant)
[tool_request: grep]
- pattern: maincpu_int_status
- path: F:\GitHub\vice-sharp\native\vice\vice\src\maincpu.h
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- offset: 530
- limit: 140

### Turn 103 (Function)
[tool_response]
530→    private static IntPtr ResolveLibrary(string libraryName, Assembly _, DllImportSearchPath? __)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        var path = ResolvedLibraryPath.Value;
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? FindLibraryPath()
540→    {
        foreach (var candidatePath in EnumerateCandidateLibraryPaths())
            return TryCreateRelocatedRuntime(candidatePath, out var relocatedPath) ? relocatedPath : candidatePath;

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateLibraryPaths()
    {
        foreach (var root in EnumerateSearchRoots(AppContext.BaseDirectory))
550→        {
            foreach (var relativeDirectory in RelativeSearchDirectories)
            {
                var candidateDirectory = Path.Combine(root, relativeDirectory);
                foreach (var libraryName in AlternateLibraryNames)
                {
                    foreach (var fileName in GetCandidateFileNames(libraryName))
                    {
                        var candidatePath = Path.Combine(candidateDirectory, fileName);
                        if (File.Exists(candidatePath))
560→                            yield return candidatePath;
                    }
                }
            }
        }
    }

    private static bool TryCreateRelocatedRuntime(string sourceLibraryPath, out string relocatedLibraryPath)
    {
        relocatedLibraryPath = string.Empty;
570→
        if (!ViceDataPathResolver.TryFindDataRoot(out var dataRoot))
            return false;

        try
        {
            var sourceDirectory = Path.GetDirectoryName(sourceLibraryPath);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
                return false;

580→            var runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "ViceSharpNative",
                CreateRuntimeDirectoryName(sourceLibraryPath, dataRoot));
            Directory.CreateDirectory(runtimeDirectory);
            CopyNativeRuntimeFiles(sourceDirectory, runtimeDirectory);

            var expectedDataDirectory = Path.Combine(runtimeDirectory, "vice", "vice", "data");
            if (!Directory.Exists(expectedDataDirectory))
            {
590→                Directory.CreateDirectory(Path.GetDirectoryName(expectedDataDirectory)!);
                Directory.CreateSymbolicLink(expectedDataDirectory, dataRoot);
            }

            if (!Directory.Exists(Path.Combine(expectedDataDirectory, "C64")))
                return false;

            relocatedLibraryPath = Path.Combine(runtimeDirectory, Path.GetFileName(sourceLibraryPath));
            return File.Exists(relocatedLibraryPath);
        }
600→        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
610→            return false;
        }
    }

    private static string CreateRuntimeDirectoryName(string sourceLibraryPath, string dataRoot)
    {
        var sourceInfo = new FileInfo(sourceLibraryPath);
        var input = $"{sourceInfo.FullName}|{sourceInfo.LastWriteTimeUtc.Ticks}|{sourceInfo.Length}|{dataRoot}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16];
    }
620→
    private static void CopyNativeRuntimeFiles(string sourceDirectory, string runtimeDirectory)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var destinationPath = Path.Combine(runtimeDirectory, Path.GetFileName(sourcePath));
            var sourceInfo = new FileInfo(sourcePath);
            var destinationInfo = new FileInfo(destinationPath);
            if (destinationInfo.Exists &&
                destinationInfo.Length == sourceInfo.Length &&
630→                destinationInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc)
            {
                continue;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
            File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);
        }
    }

640→    private static IEnumerable<string> EnumerateSearchRoots(string startingDirectory)
    {
        var current = new DirectoryInfo(startingDirectory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

650→    private static IEnumerable<string> GetCandidateFileNames(string baseName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return $"{baseName}.dll";
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
660→            yield return $"lib{baseName}.dylib";
            yield return $"{baseName}.dylib";
            yield break;
        }

        yield return $"lib{baseName}.so";
        yield return $"{baseName}.so";
    }

    private sealed class ViceNativeInstance : IViceNative

### Turn 104 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
No matches found
</workspace_result>

### Turn 105 (Assistant)
Wiring managed multi-DLL routing and updating tests.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
- old_string: 
- new_string: using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

/// <summary>
/// Native VICE xvic (VIC-20) library interop binding (vice_xvic.dll).
/// Separate from <see cref="ViceNative"/> / vice_x64 because VICE machine
/// globals cannot share a process image between C64SC and VIC20.
/// </summary>
public static unsafe partial class ViceNativeXvic
{
    private const string LibraryName = "vice_xvic";
    private static readonly string[] AlternateLibraryNames = ["vice_xvic"];
    private static readonly string[] RelativeSearchDirectories =
    [
        "",
        "native",
        Path.Combine("runtimes", "win-x64", "native"),
        Path.Combine("runtimes", "linux-x64", "native"),
        Path.Combine("runtimes", "osx-x64", "native"),
        Path.Combine("runtimes", "osx-arm64", "native")
    ];

    private static readonly Lazy<string?> ResolvedLibraryPath = new(FindLibraryPath);

    static ViceNativeXvic()
    {
        NativeLibrary.SetDllImportResolver(typeof(ViceNativeXvic).Assembly, ResolveLibrary);
    }

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;
    public static string AvailabilityMessage => ResolvedLibraryPath.Value is { } path
        ? $"Native VICE xvic library resolved at '{path}'."
        : "Native VICE xvic library not found. Build 'vice_xvic' via native/build-vice-shim-xvic.sh into the test output or a searched native directory.";

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create")]
    public static partial IntPtr Create();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create_model", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr CreateModel(string modelSelector);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_destroy")]
    public static partial void Destroy(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_reset")]
    public static partial void ResetNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_step_cycle")]
    public static partial void StepNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_read_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int ReadSnapshotNative(IntPtr instance, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_write_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WriteSnapshotNative(IntPtr instance, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_ram")]
    private static partial byte PeekRamNative(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_disk", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int AttachDiskNative(IntPtr instance, uint unit, uint drive, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_detach_disk")]
    private static partial int DetachDiskNative(IntPtr instance, uint unit, uint drive);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_set_keyboard_matrix_key")]
    private static partial int SetKeyboardMatrixKeyNative(IntPtr instance, int row, int column, int pressed);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_a")]
    public static partial byte GetA(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_x")]
    public static partial byte GetX(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_y")]
    public static partial byte GetY(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_p")]
    public static partial byte GetP(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_sp")]
    public static partial byte GetS(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_pc")]
    public static partial ushort GetPC(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_vic_get_state")]
    public static partial void GetVicState(IntPtr instance, ref ViceNative.ViceVicState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_pipeline_state")]
    public static partial void GetCpuPipelineState(IntPtr instance, ref ViceNative.ViceCpuPipelineState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_cia_get_state")]
    public static partial void GetCiaState(IntPtr instance, int ciaIndex, ref ViceNative.ViceCiaState state);

    public static bool IsVic20ModelSelector(string? modelSelector)
    {
        if (string.IsNullOrWhiteSpace(modelSelector))
            return false;

        return modelSelector.Equals("vic20", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic20pal", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic20ntsc", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("xvic", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic21", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic1001", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("supervic", StringComparison.OrdinalIgnoreCase);
    }

    public static IViceNative CreateInstance(string? modelSelector = null)
    {
        if (!IsAvailable)
            throw new DllNotFoundException(AvailabilityMessage);

        var handle = string.IsNullOrWhiteSpace(modelSelector)
            ? Create()
            : CreateModel(modelSelector);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE xvic failed to create a machine for model '{modelSelector ?? "default"}'.");

        return new Vic20NativeInstance(handle);
    }

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Only claim vice_xvic; leave vice_x64 to ViceNative's resolver.
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        // Avoid double-registration conflict: when both resolvers are set on the
        // same assembly, the last SetDllImportResolver wins. Chain: if ViceNative
        // registered first, this may be the only active resolver - so also try
        // forwarding is not possible. Registration order is handled in
        // ViceNative static ctor by installing a combined resolver.
        var path = ResolvedLibraryPath.Value;
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? FindLibraryPath()
    {
        foreach (var candidatePath in EnumerateCandidateLibraryPaths())
            return TryCreateRelocatedRuntime(candidatePath, out var relocatedPath) ? relocatedPath : candidatePath;

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateLibraryPaths()
    {
        foreach (var root in EnumerateSearchRoots(AppContext.BaseDirectory))
        {
            foreach (var relativeDirectory in RelativeSearchDirectories)
            {
                var candidateDirectory = Path.Combine(root, relativeDirectory);
                foreach (var libraryName in AlternateLibraryNames)
                {
                    foreach (var fileName in GetCandidateFileNames(libraryName))
                    {
                        var candidatePath = Path.Combine(candidateDirectory, fileName);
                        if (File.Exists(candidatePath))
                            yield return candidatePath;
                    }
                }
            }
        }
    }

    private static bool TryCreateRelocatedRuntime(string sourceLibraryPath, out string relocatedLibraryPath)
    {
        relocatedLibraryPath = string.Empty;

        if (!ViceDataPathResolver.TryFindDataRoot(out var dataRoot))
            return false;

        try
        {
            var sourceDirectory = Path.GetDirectoryName(sourceLibraryPath);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
                return false;

            var runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "ViceSharpNative",
                CreateRuntimeDirectoryName(sourceLibraryPath, dataRoot));
            Directory.CreateDirectory(runtimeDirectory);
            CopyNativeRuntimeFiles(sourceDirectory, runtimeDirectory);

            var expectedDataDirectory = Path.Combine(runtimeDirectory, "vice", "vice", "data");
            if (!Directory.Exists(expectedDataDirectory))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(expectedDataDirectory)!);
                Directory.CreateSymbolicLink(expectedDataDirectory, dataRoot);
            }

            // Vic20 ROMs live under VIC20/; accept either C64 or VIC20 tree.
            if (!Directory.Exists(Path.Combine(expectedDataDirectory, "VIC20"))
                && !Directory.Exists(Path.Combine(expectedDataDirectory, "C64")))
            {
                return false;
            }

            relocatedLibraryPath = Path.Combine(runtimeDirectory, Path.GetFileName(sourceLibraryPath));
            return File.Exists(relocatedLibraryPath);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static string CreateRuntimeDirectoryName(string sourceLibraryPath, string dataRoot)
    {
        var sourceInfo = new FileInfo(sourceLibraryPath);
        var input = $"{sourceInfo.FullName}|{sourceInfo.LastWriteTimeUtc.Ticks}|{sourceInfo.Length}|{dataRoot}|xvic";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16];
    }

    private static void CopyNativeRuntimeFiles(string sourceDirectory, string runtimeDirectory)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var destinationPath = Path.Combine(runtimeDirectory, Path.GetFileName(sourcePath));
            var sourceInfo = new FileInfo(sourcePath);
            var destinationInfo = new FileInfo(destinationPath);
            if (destinationInfo.Exists &&
                destinationInfo.Length == sourceInfo.Length &&
                destinationInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc)
            {
                continue;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
            File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);
        }
    }

    private static IEnumerable<string> EnumerateSearchRoots(string startingDirectory)
    {
        var current = new DirectoryInfo(startingDirectory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static IEnumerable<string> GetCandidateFileNames(string baseName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return $"{baseName}.dll";
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return $"lib{baseName}.dylib";
            yield return $"{baseName}.dylib";
            yield break;
        }

        yield return $"lib{baseName}.so";
        yield return $"{baseName}.so";
    }

    private sealed class Vic20NativeInstance : IViceNative
    {
        private readonly IntPtr _instance;
        private long _cycleBaseline;

        public Vic20NativeInstance(IntPtr instance)
        {
            _instance = instance;
        }

        public void Reset()
        {
            ResetNative(_instance);
            _cycleBaseline = ReadNativeCycle();
        }

        public void Step() => StepNative(_instance);

        public int ReadSnapshot(string path)
        {
            var result = ReadSnapshotNative(_instance, path);
            _cycleBaseline = ReadNativeCycle();
            return result;
        }

        public int WriteSnapshot(string path) => WriteSnapshotNative(_instance, path);

        public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
            => throw new NotSupportedException("Vic20 native cartridge attach is not wired in N0 (use later cart slice).");

        public void AttachDisk(uint unit, uint drive, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Disk path is required.", nameof(path));

            var result = AttachDiskNative(_instance, unit, drive, path);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE xvic failed to attach disk unit {unit}, drive {drive}, path '{path}'. Error code: {result}.");
        }

        public void DetachDisk(uint unit, uint drive)
        {
            var result = DetachDiskNative(_instance, unit, drive);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE xvic failed to detach disk unit {unit}, drive {drive}. Error code: {result}.");
        }

        public void SetKeyboardMatrixKey(int row, int column, bool pressed)
        {
            var result = SetKeyboardMatrixKeyNative(_instance, row, column, pressed ? 1 : 0);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE xvic failed to set keyboard matrix key ({row},{column}). Error code: {result}.");
        }

        public byte PeekRam(ushort address) => PeekRamNative(_instance, address);

        public MachineState GetState()
        {
            return new MachineState
            {
                A = GetA(_instance),
                X = GetX(_instance),
                Y = GetY(_instance),
                S = GetS(_instance),
                P = GetP(_instance),
                PC = GetPC(_instance),
                Cycle = Math.Max(0, ReadNativeCycle() - _cycleBaseline)
            };
        }

        public NativeVicState GetVicState()
        {
            var state = new ViceNative.ViceVicState();
            GetVicState(_instance, ref state);

            return new NativeVicState
            {
                Cycle = state.Cycle,
                RasterLine = state.RasterLine,
                RasterCycle = state.RasterCycle,
                BadLine = state.BadLine,
                DisplayState = state.DisplayState,
                SpriteDma = state.SpriteDma,
                Registers = state.GetRegisters(),
                AllowBadLines = state.AllowBadLines,
                IdleState = state.IdleState
            };
        }

        public NativeCiaState GetCiaState(int ciaIndex)
        {
            // Vic20 has VIA not CIA; export zeros for interface parity.
            var state = new ViceNative.ViceCiaState();
            GetCiaState(_instance, ciaIndex, ref state);
            return new NativeCiaState
            {
                PortA = state.PortA,
                PortB = state.PortB,
                DdrA = state.DdrA,
                DdrB = state.DdrB,
                TimerA = state.TimerA,
                TimerB = state.TimerB,
                TimerALatch = state.TimerALatch,
                TimerBLatch = state.TimerBLatch,
                Cra = state.Cra,
                Crb = state.Crb,
                InterruptFlags = state.InterruptFlag,
                IrqMask = state.IrqMask
            };
        }

        public NativeCpuPipelineState GetCpuPipelineState()
        {
            var state = new ViceNative.ViceCpuPipelineState();
            GetCpuPipelineState(_instance, ref state);

            return new NativeCpuPipelineState
            {
                Clk = state.Clk,
                LastOpcodeInfo = state.LastOpcodeInfo,
                BaLowFlags = state.BaLowFlags,
                PportData = state.PportData,
                PportDir = state.PportDir,
                PportDataRead = state.PportDataRead,
                PportDirRead = state.PportDirRead,
                GlobalPendingInt = state.GlobalPendingInt,
                IrqClk = state.IrqClk,
                NmiClk = state.NmiClk,
                IrqDelayCycles = state.IrqDelayCycles,
                NmiDelayCycles = state.NmiDelayCycles
            };
        }

        public void Dispose()
        {
            Destroy(_instance);
        }

        private long ReadNativeCycle()
        {
            var state = new ViceNative.ViceVicState();
            GetVicState(_instance, ref state);
            return state.Cycle;
        }
    }
}

[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- old_string:     static ViceNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibrary);
    }
- new_string:     static ViceNative()
    {
        // Combined resolver: vice_x64 (this class) and vice_xvic (ViceNativeXvic).
        // Only one SetDllImportResolver is allowed per assembly, so both library
        // names are resolved here. Force ViceNativeXvic type init so its path
        // cache is ready before first xvic import.
        _ = ViceNativeXvic.IsAvailable;
        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibraryCombined);
    }

    private static IntPtr ResolveLibraryCombined(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            var path = ResolvedLibraryPath.Value;
            return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
        }

        if (string.Equals(libraryName, "vice_xvic", StringComparison.Ordinal))
        {
            // Resolved via ViceNativeXvic path finder (IsAvailable force-init above).
            return ResolveXvicLibrary();
        }

        return IntPtr.Zero;
    }

    private static IntPtr ResolveXvicLibrary()
    {
        // Use reflection-free path: ViceNativeXvic public surface only exposes
        // IsAvailable; load via the same search roots under native/vice_xvic.dll.
        foreach (var root in EnumerateSearchRoots(AppContext.BaseDirectory))
        {
            foreach (var relativeDirectory in RelativeSearchDirectories)
            {
                var candidateDirectory = Path.Combine(root, relativeDirectory);
                foreach (var fileName in GetCandidateFileNames("vice_xvic"))
                {
                    var candidatePath = Path.Combine(candidateDirectory, fileName);
                    if (!File.Exists(candidatePath))
                        continue;

                    if (TryCreateRelocatedRuntime(candidatePath, out var relocatedPath))
                        return NativeLibrary.Load(relocatedPath);

                    return NativeLibrary.Load(candidatePath);
                }
            }
        }

        return IntPtr.Zero;
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- old_string:     public static IViceNative CreateInstance(string? modelSelector = null)
    {
        if (!IsAvailable)
            throw new DllNotFoundException(AvailabilityMessage);

        var handle = string.IsNullOrWhiteSpace(modelSelector)
            ? Create()
            : CreateModel(modelSelector);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        return new ViceNativeInstance(handle);
    }
- new_string:     public static IViceNative CreateInstance(string? modelSelector = null)
    {
        // Vic20 models route to the separate xvic oracle (vice_xvic.dll).
        if (ViceNativeXvic.IsVic20ModelSelector(modelSelector))
            return ViceNativeXvic.CreateInstance(modelSelector);

        if (!IsAvailable)
            throw new DllNotFoundException(AvailabilityMessage);

        var handle = string.IsNullOrWhiteSpace(modelSelector)
            ? Create()
            : CreateModel(modelSelector);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        return new ViceNativeInstance(handle);
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- old_string:     private static IntPtr ResolveLibrary(string libraryName, Assembly _, DllImportSearchPath? __)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        var path = ResolvedLibraryPath.Value;
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? FindLibraryPath()
- new_string:     private static string? FindLibraryPath()
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- old_string:             if (!Directory.Exists(Path.Combine(expectedDataDirectory, "C64")))
                return false;
- new_string:             // Accept C64 or VIC20 ROM trees (multi-machine native oracles).
            if (!Directory.Exists(Path.Combine(expectedDataDirectory, "C64"))
                && !Directory.Exists(Path.Combine(expectedDataDirectory, "VIC20")))
            {
                return false;
            }
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20LockstepScaffoldTests.cs
- content: namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-VIC20-LOCKSTEP-001 / Slice N0-N1.
/// Use case: native <c>xvic</c> oracle creates Vic20 machines and steps CPU
/// in lockstep-compatible fashion with the managed Vic20 path.
/// Acceptance: when <c>vice_xvic.dll</c> is present, Vic20 selectors create a
/// real machine, reset lands at the kernal reset vector, and multi-cycle step
/// advances PC/cycle. When the DLL is absent, create fails closed. Managed
/// dual-machine determinism remains the interim accuracy gate.
/// </summary>
public sealed class Vic20LockstepScaffoldTests
{
    /// <summary>
    /// N0: Vic20 profile selectors create via vice_xvic when the oracle is built.
    /// </summary>
    [Theory]
    [InlineData("vic20")]
    [InlineData("vic20ntsc")]
    [InlineData("xvic")]
    public void NativeShim_CreatesVic20Models_WhenXvicOraclePresent(string modelSelector)
    {
        if (!ViceNativeXvic.IsAvailable)
        {
            // Fail closed until N0 build ships vice_xvic.dll (same as pre-N0).
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                using var native = ViceNative.CreateInstance(modelSelector);
                _ = native;
            });

            Assert.True(
                ex is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException,
                $"unexpected exception type {ex.GetType().Name}: {ex.Message}");
            return;
        }

        using var instance = ViceNative.CreateInstance(modelSelector);
        instance.Reset();
        var state = instance.GetState();
        // Kernal reset vector is non-zero; post-reset PC is in kernal space.
        Assert.True(state.PC >= 0xE000, $"expected kernal PC after reset, got ${state.PC:X4}");
    }

    /// <summary>
    /// N1 precursor: native step advances master cycle when oracle is present.
    /// </summary>
    [Fact]
    public void NativeXvic_StepAdvancesCycle_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
        {
            return; // skip-by-return: oracle not built on this agent
        }

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var before = native.GetState();
        for (var i = 0; i < 64; i++)
            native.Step();
        var after = native.GetState();
        Assert.True(after.Cycle > before.Cycle, $"cycle did not advance: before={before.Cycle} after={after.Cycle}");
    }

    /// <summary>
    /// Interim accuracy: managed Vic20 machines stay deterministic (N2 precursor).
    /// </summary>
    [Fact]
    public void ManagedVic20_TwoMachines_AgreeAfterFrames_UntilNativeXvicExists()
    {
        var a = MachineTestFactory.CreateVic20Machine("vic20");
        var b = MachineTestFactory.CreateVic20Machine("vic20");
        for (var i = 0; i < 16; i++)
        {
            a.RunFrame();
            b.RunFrame();
        }

        Assert.Equal(a.GetState().PC, b.GetState().PC);
        Assert.Equal(a.GetState().Cycle, b.GetState().Cycle);
    }

    /// <summary>
    /// N1: short native vs managed CPU lockstep (PC + A) after equal cycle counts.
    /// </summary>
    [Fact]
    public void NativeXvic_ManagedLockstep_CpuRegs_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
        {
            return;
        }

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        // Align both at power-on reset (factory already boots; re-run Reset if exposed).
        managed.Reset();

        const int cycles = 256;
        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.RunCycle();
        }

        var n = native.GetState();
        var m = managed.GetState();
        Assert.Equal(n.PC, m.PC);
        Assert.Equal(n.A, m.A);
        Assert.Equal(n.X, m.X);
        Assert.Equal(n.Y, m.Y);
        Assert.Equal(n.S, m.S);
    }
}


### Turn 106 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs has been updated successfully.

### Turn 107 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs has been created successfully.

### Turn 108 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20LockstepScaffoldTests.cs.

### Turn 109 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs has been updated successfully.

### Turn 110 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs has been updated successfully.

### Turn 111 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs has been updated successfully.

### Turn 112 (Assistant)
[tool_request: grep]
- pattern: void Reset\(|void RunCycle\(|RunFrame
- path: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions
- glob: **/*
[tool_request: grep]
- pattern: public void Reset|public void RunCycle|RunFrame
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core
- glob: **/*Machine*
- head_limit: 30
[tool_request: grep]
- pattern: \.Reset\(|\.RunCycle\(
- path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness
- glob: **/*.cs
- head_limit: 20

### Turn 113 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 3 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\CoordinatorMachine.cs
13:/// what the runtime reads for video, memory, and status. RunFrame /
88:    public void RunFrame() => _coordinator.Step(_hostCyclesPerFrame);
100:    public void Reset() => _coordinator.Reset();
</workspace_result>

### Turn 114 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 9 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBusAndSimpleRamTests.cs
375:        ram.Reset();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBootTest.cs
32:        cpu.Reset();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\C1541MachineBuildTests.cs
98:        machine.Reset();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\C64Plus1541IntegrationTests.cs
120:        rig.host.Machine.Reset();
121:        rig.drive.Machine.Reset();
141:        rig.host.Machine.Reset();
142:        rig.drive.Machine.Reset();
147:        rig.coord.Reset();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\CoordinatorMachineTests.cs
81:        coord.Reset();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Cia1JoystickScanTests.cs
</workspace_result>

### Turn 115 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 8 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
40:    void RunFrame();
49:    void Reset();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDevice.cs
22:    void Reset();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IClock.cs
29:    void Reset();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IPubSub.cs
182:    void Reset();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
11:    void Reset();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ISystem.cs
20:    void Reset();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ISystemCoordinator.cs
49:    void Reset();
</workspace_result>

### Turn 116 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
[tool_request: grep]
- pattern: RunCycle|StepCycle|Step\(
- path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
- head_limit: 30
[tool_request: grep]
- pattern: GetState
- path: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
[tool_request: run_terminal_command]
- command: Select-String -Path F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs -Pattern "CreateVic20|GetState|RunFrame" | Select-Object -First 30; Select-String -Path F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs -Pattern "managed.*Step|_machine\.|RunCycle|StepOne" | Select-Object -First 40
- description: Find managed cycle step API

### Turn 117 (Function)
[tool_response]
1→using System;
using System.Collections.Generic;

namespace ViceSharp.Abstractions;

/// <summary>
/// One CPU's entry in a machine's per-CPU roster: a display label, that CPU's own
/// executed-cycle counter, and its clock rate (so a per-CPU speed = ExecutedCycles delta
/// over wall time ÷ ClockHz can be derived for the status surface).
10→/// </summary>
public readonly record struct CpuInfo(string Label, long ExecutedCycles, long ClockHz);

/// <summary>
/// A measured per-CPU speed reading for the status surface: a CPU's label, its effective rate
/// (its own ExecutedCycles delta over wall time), and that as a percentage of its target clock.
/// One per CPU so the host and each peripheral CPU are listed distinctly.
/// </summary>
public readonly record struct CpuRateReading(string Label, double EffectiveClockHz, double EffectiveClockPercent);

20→/// <summary>
/// A fully-assembled emulated machine. Created by IArchitectureBuilder
/// from an IArchitectureDescriptor. Owns the bus, clock, and all
/// registered devices.
/// </summary>
public interface IMachine
{
    /// <summary>The system bus for this machine.</summary>
    IBus Bus { get; }

30→    /// <summary>The master clock for this machine.</summary>
    IClock Clock { get; }

    /// <summary>Registry of all devices in this machine.</summary>
    IDeviceRegistry Devices { get; }

    /// <summary>The architecture descriptor this machine was built from.</summary>
    IArchitectureDescriptor Architecture { get; }

    /// <summary>Executes one frame (all cycles for one video frame).</summary>
40→    void RunFrame();

    /// <summary>Executes a single CPU instruction (variable cycle count).</summary>
    void StepInstruction();

    /// <summary>Gets the current full machine state snapshot.</summary>
    MachineState GetState();

    /// <summary>Resets the machine to initial power-on state.</summary>
    void Reset();
50→
    /// <summary>
    /// The machine's pub/sub bus for diagnostic / debug events (CPU instruction boundaries,
    /// memory writes). Null on machines that do not wire one; the time-travel debugger's
    /// tick-history recorder subscribes to it when present.
    /// </summary>
    IPubSub? PubSub => null;

    /// <summary>
    /// This machine's primary CPU - the one whose own executed-cycle rate is the headline
60→    /// emulation speed (<see cref="ICpu.ExecutedCycles"/> over wall time ÷ its clock). Null on
    /// machines without a CPU. A rig with more than one CPU (a coordinator's host plus each
    /// drive, or the C128's 8502 + Z80) exposes the others through their own systems; this is
    /// the principal one for the status headline.
    /// </summary>
    ICpu? PrimaryCpu => null;

    /// <summary>
    /// The machine's per-CPU roster - one <see cref="CpuInfo"/> per CPU so the status surface
    /// can list each CPU distinctly. A plain single-CPU machine reports just its primary CPU;
70→    /// a multi-system rig (a coordinator's host plus each drive, or the C128's 8502 + Z80)
    /// overrides this to list the host first then each peripheral CPU. Empty on a machine with
    /// no CPU.
    /// </summary>
    IReadOnlyList<CpuInfo> CpuInfos => PrimaryCpu is { } cpu
        ? new[] { new CpuInfo(Architecture.MachineName, cpu.ExecutedCycles, Clock.FrequencyHz) }
        : Array.Empty<CpuInfo>();
}


### Turn 118 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
46:    MachineState GetState();
</workspace_result>

### Turn 119 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 9 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
172:            _native.Step();
240:            _native.Step();
375:                native.Step();
414:            machine.Clock.Step();
613:            _native.Step();
716:                    _native.Step();
840:            _native.Step();
907:            _machine.Clock.Step();
909:            _coordinator.Step();
</workspace_result>

### Turn 120 (Function)
[tool_response]
exit: 0

tests\ViceSharp.TestHarness\MachineTestFactory.cs:23:    public static IMachine CreateVic20Machine()
tests\ViceSharp.TestHarness\MachineTestFactory.cs:24:        => CreateVic20Machine(new Vic20Descriptor());
tests\ViceSharp.TestHarness\MachineTestFactory.cs:26:    public static IMachine CreateVic20Machine(string 
modelSelector)
tests\ViceSharp.TestHarness\MachineTestFactory.cs:27:        => CreateVic20Machine(new Vic20Descriptor(modelSelector));
tests\ViceSharp.TestHarness\MachineTestFactory.cs:29:    public static IMachine CreateVic20Machine(Vic20Descriptor 
descriptor)
tests\ViceSharp.TestHarness\MachineTestFactory.cs:31:        var builder = new 
ArchitectureBuilder(CreateVic20RomProvider());
tests\ViceSharp.TestHarness\MachineTestFactory.cs:61:    public static IRomProvider CreateVic20RomProvider()
tests\ViceSharp.TestHarness\MachineTestFactory.cs:104:        return CreateVic20RomProvider().LoadRom(romName, 
Vic20ViceRomNames.ArchitectureKey);
tests\ViceSharp.TestHarness\LockstepValidator.cs:71:        if (_machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 
cpu)
tests\ViceSharp.TestHarness\LockstepValidator.cs:81:            var cartridgePort = 
_machine.Devices.GetAll<ICartridgePort>().SingleOrDefault()
tests\ViceSharp.TestHarness\LockstepValidator.cs:92:                var drive = 
_machine.Devices.GetAll<IFloppyDrive>().SingleOrDefault(drive => drive.DriveNumber == diskDriveNumber)
tests\ViceSharp.TestHarness\LockstepValidator.cs:138:        var mapSelection = 
_machine.Devices.GetAll<IKeyboardInputMapSelection>().SingleOrDefault()
tests\ViceSharp.TestHarness\LockstepValidator.cs:140:        var keyboardInput = 
_machine.Devices.GetAll<IMachineKeyboardInput>().SingleOrDefault()
tests\ViceSharp.TestHarness\LockstepValidator.cs:162:        _kernalCloseTarget = 
ResolveKernalJumpTarget(_machine.Bus, KernalCloseVector);
tests\ViceSharp.TestHarness\LockstepValidator.cs:391:    /// each of the <paramref name="cycles"/> managed clock steps 
so both sides can be
tests\ViceSharp.TestHarness\LockstepValidator.cs:603:        _kernalCloseTarget = 
ResolveKernalJumpTarget(_machine.Bus, KernalCloseVector);
tests\ViceSharp.TestHarness\LockstepValidator.cs:622:            
_basicCommandAutomation?.AdvanceTo(_machine.GetState().Cycle, _machine);
tests\ViceSharp.TestHarness\LockstepValidator.cs:640:                var searchStartManagedCycle = 
_machine.GetState().Cycle;
tests\ViceSharp.TestHarness\LockstepValidator.cs:645:                        var managedCyclesAdvanced = 
_machine.GetState().Cycle - searchStartManagedCycle;
tests\ViceSharp.TestHarness\LockstepValidator.cs:647:                            _machine.GetState().Cycle,
tests\ViceSharp.TestHarness\LockstepValidator.cs:673:            if (_hasPendingKernalCloseCall && 
_machine.GetState().PC == _pendingKernalCloseCall.ReturnPc)
tests\ViceSharp.TestHarness\LockstepValidator.cs:676:                var managedCycle = _machine.GetState().Cycle;
tests\ViceSharp.TestHarness\LockstepValidator.cs:735:        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is 
not IMemory memory)
tests\ViceSharp.TestHarness\LockstepValidator.cs:738:        var managedState = _machine.GetState();
tests\ViceSharp.TestHarness\LockstepValidator.cs:773:        var managedState = _machine.GetState();
tests\ViceSharp.TestHarness\LockstepValidator.cs:793:            Actual = _machine.GetState()
tests\ViceSharp.TestHarness\LockstepValidator.cs:849:        var state = _machine.GetState();
tests\ViceSharp.TestHarness\LockstepValidator.cs:884:        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is 
not IMemory memory)
tests\ViceSharp.TestHarness\LockstepValidator.cs:899:            _machine.Reset();
tests\ViceSharp.TestHarness\LockstepValidator.cs:907:            _machine.Clock.Step();
tests\ViceSharp.TestHarness\LockstepValidator.cs:971:        var managedCycle = _machine.GetState().Cycle;
tests\ViceSharp.TestHarness\LockstepValidator.cs:1001:        var target = ResolveKernalJumpTarget(_machine.Bus, 
KernalCloseVector);
tests\ViceSharp.TestHarness\LockstepValidator.cs:1037:        var managedState = _machine.GetState();
tests\ViceSharp.TestHarness\LockstepValidator.cs:1052:        if (_machine.Devices.GetByRole(DeviceRole.VideoChip) is 
not Mos6569 vic)
tests\ViceSharp.TestHarness\LockstepValidator.cs:1063:        return _machine.Devices.GetByRole(DeviceRole.Cpu) is 
Mos6502 cpu
tests\ViceSharp.TestHarness\LockstepValidator.cs:1075:            chars[i] = 
DecodeScreenCode(_machine.Bus.Peek((ushort)(BasicCommandAutomation.ReadyPromptStart + i)));



### Turn 121 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20LockstepScaffoldTests.cs
- old_string:         const int cycles = 256;
        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.RunCycle();
        }
- new_string:         const int cycles = 256;
        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }
[tool_request: run_terminal_command]
- command: # Force type init issue: ViceNativeXvic also calls SetDllImportResolver which throws if already set.
# Remove second resolver from ViceNativeXvic - combined resolver lives only in ViceNative.
Select-String -Path F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs -Pattern "SetDllImportResolver|static ViceNativeXvic" -Context 0,5
- description: Locate dual resolver registration

### Turn 122 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20LockstepScaffoldTests.cs has been updated successfully.

### Turn 123 (Function)
[tool_response]
exit: 0

> src\ViceSharp.Core\ViceNative.Xvic.cs:31:    static ViceNativeXvic()
  src\ViceSharp.Core\ViceNative.Xvic.cs:32:    {
> src\ViceSharp.Core\ViceNative.Xvic.cs:33:        NativeLibrary.SetDllImportResolver(typeof(ViceNativeXvic).Assembly, 
ResolveLibrary);
  src\ViceSharp.Core\ViceNative.Xvic.cs:34:    }
  src\ViceSharp.Core\ViceNative.Xvic.cs:35:
  src\ViceSharp.Core\ViceNative.Xvic.cs:36:    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;
  src\ViceSharp.Core\ViceNative.Xvic.cs:37:    public static string AvailabilityMessage => ResolvedLibraryPath.Value 
is { } path
  src\ViceSharp.Core\ViceNative.Xvic.cs:38:        ? $"Native VICE xvic library resolved at '{path}'."
> src\ViceSharp.Core\ViceNative.Xvic.cs:137:        // same assembly, the last SetDllImportResolver wins. Chain: if 
ViceNative
  src\ViceSharp.Core\ViceNative.Xvic.cs:138:        // registered first, this may be the only active resolver - so 
also try
  src\ViceSharp.Core\ViceNative.Xvic.cs:139:        // forwarding is not possible. Registration order is handled in
  src\ViceSharp.Core\ViceNative.Xvic.cs:140:        // ViceNative static ctor by installing a combined resolver.
  src\ViceSharp.Core\ViceNative.Xvic.cs:141:        var path = ResolvedLibraryPath.Value;
  src\ViceSharp.Core\ViceNative.Xvic.cs:142:        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);



### Turn 124 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
- old_string:     private static readonly Lazy<string?> ResolvedLibraryPath = new(FindLibraryPath);

    static ViceNativeXvic()
    {
        NativeLibrary.SetDllImportResolver(typeof(ViceNativeXvic).Assembly, ResolveLibrary);
    }

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;
- new_string:     private static readonly Lazy<string?> ResolvedLibraryPath = new(FindLibraryPath);

    // DllImport resolver is installed once on the Core assembly by ViceNative
    // (combined vice_x64 + vice_xvic). Do not call SetDllImportResolver here.

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;

    /// <summary>Absolute path of the resolved vice_xvic library, or null.</summary>
    public static string? ResolvedPath => ResolvedLibraryPath.Value;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
- old_string:     private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Only claim vice_xvic; leave vice_x64 to ViceNative's resolver.
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        // Avoid double-registration conflict: when both resolvers are set on the
        // same assembly, the last SetDllImportResolver wins. Chain: if ViceNative
        // registered first, this may be the only active resolver - so also try
        // forwarding is not possible. Registration order is handled in
        // ViceNative static ctor by installing a combined resolver.
        var path = ResolvedLibraryPath.Value;
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? FindLibraryPath()
- new_string:     private static string? FindLibraryPath()
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- old_string:         // Combined resolver: vice_x64 (this class) and vice_xvic (ViceNativeXvic).
        // Only one SetDllImportResolver is allowed per assembly, so both library
        // names are resolved here. Force ViceNativeXvic type init so its path
        // cache is ready before first xvic import.
        _ = ViceNativeXvic.IsAvailable;
        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibraryCombined);
    }

    private static IntPtr ResolveLibraryCombined(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            var path = ResolvedLibraryPath.Value;
            return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
        }

        if (string.Equals(libraryName, "vice_xvic", StringComparison.Ordinal))
        {
            // Resolved via ViceNativeXvic path finder (IsAvailable force-init above).
            return ResolveXvicLibrary();
        }

        return IntPtr.Zero;
    }

    private static IntPtr ResolveXvicLibrary()
    {
        // Use reflection-free path: ViceNativeXvic public surface only exposes
        // IsAvailable; load via the same search roots under native/vice_xvic.dll.
        foreach (var root in EnumerateSearchRoots(AppContext.BaseDirectory))
        {
            foreach (var relativeDirectory in RelativeSearchDirectories)
            {
                var candidateDirectory = Path.Combine(root, relativeDirectory);
                foreach (var fileName in GetCandidateFileNames("vice_xvic"))
                {
                    var candidatePath = Path.Combine(candidateDirectory, fileName);
                    if (!File.Exists(candidatePath))
                        continue;

                    if (TryCreateRelocatedRuntime(candidatePath, out var relocatedPath))
                        return NativeLibrary.Load(relocatedPath);

                    return NativeLibrary.Load(candidatePath);
                }
            }
        }

        return IntPtr.Zero;
    }
- new_string:         // Combined resolver: vice_x64 (this class) and vice_xvic (ViceNativeXvic).
        // Only one SetDllImportResolver is allowed per assembly.
        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibraryCombined);
    }

    private static IntPtr ResolveLibraryCombined(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            var path = ResolvedLibraryPath.Value;
            return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
        }

        if (string.Equals(libraryName, "vice_xvic", StringComparison.Ordinal))
        {
            var path = ViceNativeXvic.ResolvedPath;
            return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
        }

        return IntPtr.Zero;
    }

### Turn 125 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs has been updated successfully.

### Turn 126 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs has been updated successfully.

### Turn 127 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs has been updated successfully.

### Turn 128 (Assistant)
[tool_request: grep]
- pattern: last_opcode_info|maincpu_rmw_flag|stolen_cycles
- path: F:\GitHub\vice-sharp\native\vice-shim.c
- head_limit: 20
[tool_request: grep]
- pattern: last_opcode|maincpu_rmw|stolen_cycles
- path: F:\GitHub\vice-sharp\native\vice\vice\src\maincpu.h
[tool_request: run_terminal_command]
- command: Select-String -Path F:\GitHub\vice-sharp\native\vice\vice\src\maincpu.h,F:\GitHub\vice-sharp\native\vice\vice\src\mainviccpu.c -Pattern "extern.*last_opcode|maincpu_rmw|stolen_cycles" | Select-Object -First 20
- description: Find last_opcode extern decls

### Turn 129 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 7 matching lines
F:\GitHub\vice-sharp\native\vice-shim.c
100:extern CLOCK stolen_cycles;
352:    stolen_cycles = 0;
354:    maincpu_rmw_flag = 0;
355:    last_opcode_info = 0;
1340:   - last_opcode_info / maincpu_ba_low_flags from the MAINCPU module
1342:     last_opcode_info on resume (mainc64cpu.c VICE_SHIM_HOSTED block), so the
1365:        state->last_opcode_info = (uint32_t)last_opcode_info;
</workspace_result>

### Turn 130 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 4 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\maincpu.h
36:extern unsigned int last_opcode_info;
37:extern unsigned int last_opcode_addr;
58:extern int maincpu_rmw_flag;
85:#define maincpu_num_write_cycles() maincpu_opcode_write_cycles[OPINFO_NUMBER(last_opcode_info)]
</workspace_result>

### Turn 131 (Function)
[tool_response]
exit: 0

native\vice\vice\src\maincpu.h:36:extern unsigned int last_opcode_info;
native\vice\vice\src\maincpu.h:37:extern unsigned int last_opcode_addr;
native\vice\vice\src\maincpu.h:58:extern int maincpu_rmw_flag;
native\vice\vice\src\mainviccpu.c:71:CLOCK stolen_cycles;
native\vice\vice\src\mainviccpu.c:308:int maincpu_rmw_flag = 0;
native\vice\vice\src\mainviccpu.c:607:        stolen_cycles = 0;
native\vice\vice\src\mainviccpu.c:608:        maincpu_rmw_flag = 0;
native\vice\vice\src\mainviccpu.c:622:#define RMW_FLAG maincpu_rmw_flag
native\vice\vice\src\mainviccpu.c:802:    maincpu_rmw_flag = 0;



### Turn 132 (Assistant)
[tool_request: grep]
- pattern: machine_write_snapshot
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/machine.h
[tool_request: run_terminal_command]
- command: # Build vice_xvic.dll under MSYS2 MINGW64
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\xvic-shim-build.log"
$env:MSYSTEM = "MINGW64"
$bash = "C:\msys64\usr\bin\bash.exe"
if (-not (Test-Path $bash)) { $bash = "C:\msys64\usr\bin\bash.exe" }
& $bash -lc "export PATH=/mingw64/bin:/usr/bin:`$PATH; cd /f/GitHub/vice-sharp/native && bash ./build-vice-shim-xvic.sh" 2>&1 | Tee-Object -FilePath $log
Write-Host "EXIT=$LASTEXITCODE"
- description: Build vice_xvic.dll hosted oracle
- timeout: 600000

### Turn 133 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\machine.h
145:int machine_write_snapshot(const char *name, int save_roms, int save_disks, int even_mode);
</workspace_result>

### Turn 134 (Function)
[tool_response]
<task-id>call-31f6d8c8-f44e-4484-9abb-598c0f83dfc7-103</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-31f6d8c8-f44e-4484-9abb-598c0f83dfc7-103.log</output-file>
<status>running</status>
<summary>Command "# Build vice_xvic.dll under MSYS2 MINGW64
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\xvic-shim-build.log"
$env:MSYSTEM = "MINGW64"
$bash = "C:\msys64\usr\bin\bash.exe"
if (-not (Test-Path $bash)) { $bash = "C:\msys64\usr\bin\bash.exe" }
& $bash -lc "export PATH=/mingw64/bin:/usr/bin:`$PATH; cd /f/GitHub/vice-sharp/native && bash ./build-vice-shim-xvic.sh" 2>&1 | Tee-Object -FilePath $log
Write-Host "EXIT=$LASTEXITCODE"" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-31f6d8c8-f44e-4484-9abb-598c0f83dfc7-103"] to retrieve the output.

### Turn 135 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet build F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 40
dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 40
- description: Build Core and TestHarness Release
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-31f6d8c8-f44e-4484-9abb-598c0f83dfc7-103"]
- timeout_ms: 120000

### Turn 136 (Function)
[tool_response]
=== Task call-31f6d8c8-f44e-4484-9abb-598c0f83dfc7-103 ===
Command: # Build vice_xvic.dll under MSYS2 MINGW64
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\xvic-shim-build.log"
$env:MSYSTEM = "MINGW64"
$bash = "C:\msys64\usr\bin\bash.exe"
if (-not (Test-Path $bash)) { $bash = "C:\msys64\usr\bin\bash.exe" }
& $bash -lc "export PATH=/mingw64/bin:/usr/bin:`$PATH; cd /f/GitHub/vice-sharp/native && bash ./build-vice-shim-xvic.sh" 2>&1 | Tee-Object -FilePath $log
Write-Host "EXIT=$LASTEXITCODE"
Status: completed
Duration: 16.54s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-31f6d8c8-f44e-4484-9abb-598c0f83dfc7-103.log

=== Output ===
make: Entering directory '/f/GitHub/vice-sharp/native/vice/vice/src/lib/linenoise-ng'
make: Nothing to be done for 'all'.
make: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src/lib/linenoise-ng'
make: Entering directory '/f/GitHub/vice-sharp/native/vice/vice/src'
make: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src'
F:/GitHub/vice-sharp/native/vice-shim-vic20.c: In function 'vice_shim_set_current_thread_description':
F:/GitHub/vice-sharp/native/vice-shim-vic20.c:260:30: warning: cast between incompatible function types from 'FARPROC' {aka 'long long int (*)(void)'} to 'HRESULT (*)(void *, const WCHAR *)' {aka 'long int (*)(void *, const short unsigned int *)'} [-Wcast-function-type]
  260 |     set_thread_description = (vice_shim_set_thread_description_fn)GetProcAddress(kernel32, "SetThreadDescription");
      |                              ^
Built: /f/GitHub/vice-sharp/native/vice_xvic.dll
-rwxr-xr-x 1 kingd kingd 10909818 Aug  5 14:23 /f/GitHub/vice-sharp/native/vice_xvic.dll
EXIT=0


### Turn 137 (Function)
[tool_response]
exit: 1
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(338,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(358,13): error CS1501: No overload for method 'GetCiaState' takes 3 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(379,13): error CS1501: No overload for method 'GetCpuPipelineState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(406,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]

Build FAILED.

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(338,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(358,13): error CS1501: No overload for method 'GetCiaState' takes 3 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(379,13): error CS1501: No overload for method 'GetCpuPipelineState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(406,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
    0 Warning(s)
    4 Error(s)

Time Elapsed 00:00:06.07
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(338,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(358,13): error CS1501: No overload for method 'GetCiaState' takes 3 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(379,13): error CS1501: No overload for method 'GetCpuPipelineState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(406,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]

Build FAILED.

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(338,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(358,13): error CS1501: No overload for method 'GetCiaState' takes 3 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(379,13): error CS1501: No overload for method 'GetCpuPipelineState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs(406,13): error CS1501: No overload for method 'GetVicState' takes 2 arguments [F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj]
    0 Warning(s)
    4 Error(s)

Time Elapsed 00:00:07.72


### Turn 138 (Assistant)
Native DLL built. Fixing method name collisions in the managed binding.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
- old_string:         public NativeVicState GetVicState()
        {
            var state = new ViceNative.ViceVicState();
            GetVicState(_instance, ref state);

            return new NativeVicState
            {
                Cycle = state.Cycle,
                RasterLine = state.RasterLine,
                RasterCycle = state.RasterCycle,
                BadLine = state.BadLine,
                DisplayState = state.DisplayState,
                SpriteDma = state.SpriteDma,
                Registers = state.GetRegisters(),
                AllowBadLines = state.AllowBadLines,
                IdleState = state.IdleState
            };
        }

        public NativeCiaState GetCiaState(int ciaIndex)
        {
            // Vic20 has VIA not CIA; export zeros for interface parity.
            var state = new ViceNative.ViceCiaState();
            GetCiaState(_instance, ciaIndex, ref state);
            return new NativeCiaState
            {
                PortA = state.PortA,
                PortB = state.PortB,
                DdrA = state.DdrA,
                DdrB = state.DdrB,
                TimerA = state.TimerA,
                TimerB = state.TimerB,
                TimerALatch = state.TimerALatch,
                TimerBLatch = state.TimerBLatch,
                Cra = state.Cra,
                Crb = state.Crb,
                InterruptFlags = state.InterruptFlag,
                IrqMask = state.IrqMask
            };
        }

        public NativeCpuPipelineState GetCpuPipelineState()
        {
            var state = new ViceNative.ViceCpuPipelineState();
            GetCpuPipelineState(_instance, ref state);

            return new NativeCpuPipelineState
            {
                Clk = state.Clk,
                LastOpcodeInfo = state.LastOpcodeInfo,
                BaLowFlags = state.BaLowFlags,
                PportData = state.PportData,
                PportDir = state.PportDir,
                PportDataRead = state.PportDataRead,
                PportDirRead = state.PportDirRead,
                GlobalPendingInt = state.GlobalPendingInt,
                IrqClk = state.IrqClk,
                NmiClk = state.NmiClk,
                IrqDelayCycles = state.IrqDelayCycles,
                NmiDelayCycles = state.NmiDelayCycles
            };
        }

        public void Dispose()
        {
            Destroy(_instance);
        }

        private long ReadNativeCycle()
        {
            var state = new ViceNative.ViceVicState();
            GetVicState(_instance, ref state);
            return state.Cycle;
        }
- new_string:         public NativeVicState GetVicState()
        {
            var state = new ViceNative.ViceVicState();
            ViceNativeXvic.GetVicState(_instance, ref state);

            return new NativeVicState
            {
                Cycle = state.Cycle,
                RasterLine = state.RasterLine,
                RasterCycle = state.RasterCycle,
                BadLine = state.BadLine,
                DisplayState = state.DisplayState,
                SpriteDma = state.SpriteDma,
                Registers = state.GetRegisters(),
                AllowBadLines = state.AllowBadLines,
                IdleState = state.IdleState
            };
        }

        public NativeCiaState GetCiaState(int ciaIndex)
        {
            // Vic20 has VIA not CIA; export zeros for interface parity.
            var state = new ViceNative.ViceCiaState();
            ViceNativeXvic.GetCiaState(_instance, ciaIndex, ref state);
            return new NativeCiaState
            {
                PortA = state.PortA,
                PortB = state.PortB,
                DdrA = state.DdrA,
                DdrB = state.DdrB,
                TimerA = state.TimerA,
                TimerB = state.TimerB,
                TimerALatch = state.TimerALatch,
                TimerBLatch = state.TimerBLatch,
                Cra = state.Cra,
                Crb = state.Crb,
                InterruptFlags = state.InterruptFlag,
                IrqMask = state.IrqMask
            };
        }

        public NativeCpuPipelineState GetCpuPipelineState()
        {
            var state = new ViceNative.ViceCpuPipelineState();
            ViceNativeXvic.GetCpuPipelineState(_instance, ref state);

            return new NativeCpuPipelineState
            {
                Clk = state.Clk,
                LastOpcodeInfo = state.LastOpcodeInfo,
                BaLowFlags = state.BaLowFlags,
                PportData = state.PportData,
                PportDir = state.PportDir,
                PportDataRead = state.PportDataRead,
                PportDirRead = state.PportDirRead,
                GlobalPendingInt = state.GlobalPendingInt,
                IrqClk = state.IrqClk,
                NmiClk = state.NmiClk,
                IrqDelayCycles = state.IrqDelayCycles,
                NmiDelayCycles = state.NmiDelayCycles
            };
        }

        public void Dispose()
        {
            ViceNativeXvic.Destroy(_instance);
        }

        private long ReadNativeCycle()
        {
            var state = new ViceNative.ViceVicState();
            ViceNativeXvic.GetVicState(_instance, ref state);
            return state.Cycle;
        }
[tool_request: run_terminal_command]
- command: dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 25
Write-Host "BUILD_EXIT=$LASTEXITCODE"
Test-Path F:\GitHub\vice-sharp\native\vice_xvic.dll
(Get-Item F:\GitHub\vice-sharp\native\vice_xvic.dll).Length
- description: Rebuild tests; confirm vice_xvic.dll

### Turn 139 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs has been updated successfully.

### Turn 140 (Function)
[tool_response]
<task-id>call-ccb4a88e-2c25-471b-bbc7-3e4542eaa2de-107</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-ccb4a88e-2c25-471b-bbc7-3e4542eaa2de-107.log</output-file>
<status>running</status>
<summary>Command "dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 25
Write-Host "BUILD_EXIT=$LASTEXITCODE"
Test-Path F:\GitHub\vice-sharp\native\vice_xvic.dll
(Get-Item F:\GitHub\vice-sharp\native\vice_xvic.dll).Length" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-ccb4a88e-2c25-471b-bbc7-3e4542eaa2de-107"] to retrieve the output.

### Turn 141 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-ccb4a88e-2c25-471b-bbc7-3e4542eaa2de-107"]
- timeout_ms: 180000

### Turn 142 (Function)
[tool_response]
=== Task call-ccb4a88e-2c25-471b-bbc7-3e4542eaa2de-107 ===
Command: dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 25
Write-Host "BUILD_EXIT=$LASTEXITCODE"
Test-Path F:\GitHub\vice-sharp\native\vice_xvic.dll
(Get-Item F:\GitHub\vice-sharp\native\vice_xvic.dll).Length
Status: completed
Duration: 26.45s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-ccb4a88e-2c25-471b-bbc7-3e4542eaa2de-107.log

=== Output ===

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:25.42
BUILD_EXIT=0
True
10909818


### Turn 143 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_ROM_PATH = if ($env:VICESHARP_ROM_PATH) { $env:VICESHARP_ROM_PATH } else { "F:\GitHub\vice-sharp\native\vice\vice\data" }
Write-Host "ROM_PATH=$env:VICESHARP_ROM_PATH"
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-lockstep-n0.log"
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20LockstepScaffold" --logger "console;verbosity=detailed" 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 80
Write-Host "TEST_EXIT=$LASTEXITCODE"
- description: Run Vic20 lockstep scaffold tests
- timeout: 300000

### Turn 144 (Function)
[tool_response]
exit: 0
ROM_PATH=F:\GitHub\vice-sharp\native\vice\vice\data
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.5+1b188a7b0a (64-bit .NET 10.0.10)
[xUnit.net 00:00:00.33]   Discovering: ViceSharp.TestHarness
[xUnit.net 00:00:02.05]   Discovered:  ViceSharp.TestHarness
[xUnit.net 00:00:02.34]   Starting:    ViceSharp.TestHarness
xvic vice_machine_create call=1 model=vic20
xvic vice_machine_reset call=1 machine=00000210cf5e0a00
xvic vice_machine_create call=2 model=vic20ntsc
xvic vice_machine_reset call=2 machine=00000210cf5e0680
xvic vice_machine_create call=3 model=xvic
xvic vice_machine_reset call=3 machine=00000210cf5e0920
xvic vice_machine_create call=4 model=vic20
xvic vice_machine_reset call=4 machine=00000210cf5e08c0
xvic vice_machine_step_cycle call=1 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=2 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=3 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=4 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=5 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=6 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=7 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=8 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=9 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=10 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=11 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=12 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=13 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=14 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=15 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=16 machine=00000210cf5e08c0
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_create call=5 model=vic20
xvic vice_machine_reset call=5 machine=00000210cf5e06c0
[xUnit.net 00:00:03.51]   Finished:    ViceSharp.TestHarness (ID = 'ab71db7394e73a1adc1130e42fc74f059e0e20545bf7641bf1047ee89aadf982')
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.ManagedVic20_TwoMachines_AgreeAfterFrames_UntilNativeXvicExists [424 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeShim_CreatesVic20Models_WhenXvicOraclePresent(modelSelector: "vic20") [102 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeShim_CreatesVic20Models_WhenXvicOraclePresent(modelSelector: "vic20ntsc") [2 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeShim_CreatesVic20Models_WhenXvicOraclePresent(modelSelector: "xvic") [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeXvic_StepAdvancesCycle_WhenOraclePresent [3 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeXvic_ManagedLockstep_CpuRegs_WhenOraclePresent [488 ms]

Test Run Successful.
Total tests: 6
     Passed: 6
 Total time: 4.1578 Seconds
TEST_EXIT=0


### Turn 145 (Assistant)
N0 is green: create, step, and 256-cycle CPU lockstep all passed. Expanding N1/N2 and running the broader Vic20 suite.
[tool_request: todo_write]
- todos: [{"id":"n0","content":"n0: Extend native oracle for Vic20 model create","status":"completed"},{"id":"n1","content":"n1: LockstepValidator Vic20 path CPU+VIA+VIC regs","status":"in_progress"}]
- merge: true
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
- offset: 42
- limit: 30
[tool_request: grep]
- pattern: CreateVic20Machine|class Machine
- path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs
- -A: 15
[tool_request: run_terminal_command]
- command: $env:VICESHARP_ROM_PATH = "F:\GitHub\vice-sharp\native\vice\vice\data"
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-filter.log"
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20" --logger "console;verbosity=minimal" 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 50
Write-Host "TEST_EXIT=$LASTEXITCODE"
- description: Run full Vic20 test filter
- timeout: 600000

### Turn 146 (Function)
[tool_response]
- [completed] n0: n0: Extend native oracle for Vic20 model create
- [in_progress] n1: n1: LockstepValidator Vic20 path CPU+VIA+VIC regs
- [pending] n2: n2: Multi-cycle Vic20 lockstep suite + CI ROMs
- [pending] tierA: tierA: Close remaining Tier A gaps for Vic20 filter
- [pending] verify: verify: Run verification plan 1-5; capture SCRATCH logs
- [pending] docs: docs: Update HANDOFF/roadmap after gates pass


### Turn 147 (Function)
[tool_response]
42→    public LockstepValidator(
        string modelSelector = "c64",
        byte[]? cartridgeImage = null,
        CartridgeMappingMode cartridgeMappingMode = CartridgeMappingMode.Auto,
        byte[]? diskImage = null,
        string? diskPath = null,
        byte diskDriveNumber = 8,
        bool recordRecentTrace = true)
50→    {
        if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
            if (diskImage is null)
                throw new ArgumentException("Managed disk image bytes are required when a native disk path is supplied.", nameof(diskImage));
            if (string.IsNullOrWhiteSpace(diskPath))
                throw new ArgumentException("Native disk path is required when managed disk image bytes are supplied.", nameof(diskPath));

            var rig = CreateC64WithTrueDrive(modelSelector, diskPath, diskDriveNumber);
            _machine = rig.Host;
60→            _coordinator = rig.Coordinator;
        }
        else
        {
            _machine = MachineTestFactory.CreateC64Machine(modelSelector);
        }

        _native = ViceNative.CreateInstance(modelSelector);
        _recordRecentTrace = recordRecentTrace;
        _kernalCloseTarget = KernalCloseVector;
70→
        if (_machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu)

### Turn 148 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 6 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs
9:internal static class MachineTestFactory
10-{
11-    public static IMachine CreateC64Machine()
12-        => CreateC64Machine(new Architectures.C64.C64Descriptor());
13-
14-    public static IMachine CreateC64Machine(string modelSelector)
15-        => CreateC64Machine(new Architectures.C64.C64Descriptor(modelSelector));
16-
17-    public static IMachine CreateC64Machine(Architectures.C64.C64Descriptor descriptor)
18-    {
19-        var builder = new ArchitectureBuilder(CreateC64RomProvider());
20-        return builder.Build(descriptor);
21-    }
22-
23:    public static IMachine CreateVic20Machine()
24:        => CreateVic20Machine(new Vic20Descriptor());
25-
26:    public static IMachine CreateVic20Machine(string modelSelector)
27:        => CreateVic20Machine(new Vic20Descriptor(modelSelector));
28-
29:    public static IMachine CreateVic20Machine(Vic20Descriptor descriptor)
30-    {
31-        var builder = new ArchitectureBuilder(CreateVic20RomProvider());
32-        return builder.Build(descriptor);
33-    }
34-
35-    public static IRomProvider CreateC64RomProvider()
36-    {
37-        var dataRoots = ViceDataPathResolver.FindDataRoots();
38-        foreach (var dataRoot in dataRoots)
39-        {
40-            var provider = new RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
41-            if (new C64RomSet().IsComplete(provider))
42-                return provider;
43-        }
44-
</workspace_result>

### Turn 149 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    68, Skipped:     0, Total:    68, Duration: 1 s - ViceSharp.TestHarness.dll (net10.0)
TEST_EXIT=0


### Turn 150 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
- old_string:         if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
            if (diskImage is null)
                throw new ArgumentException("Managed disk image bytes are required when a native disk path is supplied.", nameof(diskImage));
            if (string.IsNullOrWhiteSpace(diskPath))
                throw new ArgumentException("Native disk path is required when managed disk image bytes are supplied.", nameof(diskPath));

            var rig = CreateC64WithTrueDrive(modelSelector, diskPath, diskDriveNumber);
            _machine = rig.Host;
            _coordinator = rig.Coordinator;
        }
        else
        {
            _machine = MachineTestFactory.CreateC64Machine(modelSelector);
        }

        _native = ViceNative.CreateInstance(modelSelector);
- new_string:         var isVic20 = ViceNativeXvic.IsVic20ModelSelector(modelSelector);

        if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
            if (diskImage is null)
                throw new ArgumentException("Managed disk image bytes are required when a native disk path is supplied.", nameof(diskImage));
            if (string.IsNullOrWhiteSpace(diskPath))
                throw new ArgumentException("Native disk path is required when managed disk image bytes are supplied.", nameof(diskPath));
            if (isVic20)
                throw new NotSupportedException("Vic20 native lockstep with true-drive disk attach is not wired yet (host-only N1 path).");

            var rig = CreateC64WithTrueDrive(modelSelector, diskPath, diskDriveNumber);
            _machine = rig.Host;
            _coordinator = rig.Coordinator;
        }
        else
        {
            _machine = isVic20
                ? MachineTestFactory.CreateVic20Machine(modelSelector)
                : MachineTestFactory.CreateC64Machine(modelSelector);
        }

        _native = ViceNative.CreateInstance(modelSelector);
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs
- content: namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-VIC20-LOCKSTEP-001 / Slice N1-N2.
/// Use case: multi-cycle native xvic vs managed Vic20 CPU register lockstep.
/// Acceptance: when <c>vice_xvic.dll</c> is present, managed and native A/X/Y/S/P/PC
/// match after equal master-cycle counts (reset-aligned). Skips when oracle absent.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20NativeLockstepTests
{
    public static TheoryData<string, int> CycleCounts => new()
    {
        { "vic20", 64 },
        { "vic20", 256 },
        { "vic20", 1024 },
        { "vic20", 4096 },
        { "vic20ntsc", 256 },
        { "vic20ntsc", 1024 },
    };

    /// <summary>
    /// N1/N2: CPU register lockstep for multiple cycle budgets and models.
    /// </summary>
    [Theory]
    [MemberData(nameof(CycleCounts))]
    public void NativeXvic_Managed_CpuRegs_Lockstep(string modelSelector, int cycles)
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance(modelSelector);
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine(modelSelector);
        managed.Reset();

        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var n = native.GetState();
        var m = managed.GetState();
        Assert.True(
            n.PC == m.PC && n.A == m.A && n.X == m.X && n.Y == m.Y && n.S == m.S && n.P == m.P,
            $"CPU mismatch after {cycles} cycles model={modelSelector}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2}; managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
    }

    /// <summary>
    /// N1: VIC-I raster timing fields advance and stay in a plausible range.
    /// </summary>
    [Fact]
    public void NativeXvic_VicRasterAdvances()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var before = native.GetVicState();
        for (var i = 0; i < 200; i++)
            native.Step();
        var after = native.GetVicState();
        Assert.True(after.Cycle > before.Cycle, "VIC cycle counter did not advance");
        // PAL Vic20 is ~71 cycles/line; after 200 cycles raster should have moved or wrapped.
        Assert.True(
            after.RasterLine != before.RasterLine || after.RasterCycle != before.RasterCycle,
            $"raster did not move: line={after.RasterLine} cycle={after.RasterCycle}");
    }

    /// <summary>
    /// N1: LockstepValidator constructs a Vic20 pair without throwing when oracle present.
    /// </summary>
    [Fact]
    public void LockstepValidator_AcceptsVic20Model_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var validator = new LockstepValidator("vic20");
        // Dispose without long run is enough to prove create path.
        Assert.NotNull(validator);
    }
}

[tool_request: run_terminal_command]
- command: dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 15
$env:VICESHARP_ROM_PATH = "F:\GitHub\vice-sharp\native\vice\vice\data"
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-native-lockstep.log"
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20NativeLockstep|FullyQualifiedName~Vic20LockstepScaffold" --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 60
Write-Host "TEST_EXIT=$LASTEXITCODE"
- description: Build and run Vic20 native lockstep suite
- timeout: 300000

### Turn 151 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs has been updated successfully.

### Turn 152 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs has been created.

### Turn 153 (Function)
[tool_response]
<task-id>call-da67291a-b58d-4c3c-9b38-c6b709bc2e55-116</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-da67291a-b58d-4c3c-9b38-c6b709bc2e55-116.log</output-file>
<status>running</status>
<summary>Command "dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 15
$env:VICESHARP_ROM_PATH = "F:\GitHub\vice-sharp\native\vice\vice\data"
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-native-lockstep.log"
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20NativeLockstep|FullyQualifiedName~Vic20LockstepScaffold" --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 60
Write-Host "TEST_EXIT=$LASTEXITCODE"" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-da67291a-b58d-4c3c-9b38-c6b709bc2e55-116"] to retrieve the output.

### Turn 154 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-da67291a-b58d-4c3c-9b38-c6b709bc2e55-116"]
- timeout_ms: 180000

### Turn 155 (Function)
[tool_response]
=== Task call-da67291a-b58d-4c3c-9b38-c6b709bc2e55-116 ===
Command: dotnet build F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 15
$env:VICESHARP_ROM_PATH = "F:\GitHub\vice-sharp\native\vice\vice\data"
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-native-lockstep.log"
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20NativeLockstep|FullyQualifiedName~Vic20LockstepScaffold" --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 60
Write-Host "TEST_EXIT=$LASTEXITCODE"
Status: completed
Duration: 16.76s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-da67291a-b58d-4c3c-9b38-c6b709bc2e55-116.log

=== Output ===

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:11.06
xvic vice_machine_create call=8 model=vic20
xvic vice_machine_reset call=7 machine=000002961e430960
xvic vice_machine_reset call=8 machine=000002961e4306d0
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 64) [104 ms]
  Error Message:
   CPU mismatch after 64 cycles model=vic20: native PC=$FD99 A=$00 X=$01 Y=$00 S=$FD P=$05; managed PC=$FD99 A=$00 X=$01 Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20ntsc", cycles: 1024) [442 ms]
  Error Message:
   CPU mismatch after 1024 cycles model=vic20ntsc: native PC=$FD95 A=$00 X=$33 Y=$00 S=$FD P=$05; managed PC=$FD95 A=$00 X=$33 Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 1024) [11 ms]
  Error Message:
   CPU mismatch after 1024 cycles model=vic20: native PC=$FD95 A=$00 X=$33 Y=$00 S=$FD P=$05; managed PC=$FD95 A=$00 X=$33 Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20ntsc", cycles: 256) [7 ms]
  Error Message:
   CPU mismatch after 256 cycles model=vic20ntsc: native PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$05; managed PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 4096) [17 ms]
  Error Message:
   CPU mismatch after 4096 cycles model=vic20: native PC=$FD92 A=$00 X=$D5 Y=$00 S=$FD P=$05; managed PC=$FD92 A=$00 X=$D5 Y=$00 S=$FD P=$A4
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 256) [4 ms]
  Error Message:
   CPU mismatch after 256 cycles model=vic20: native PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$05; managed PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Passed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.LockstepValidator_AcceptsVic20Model_WhenOraclePresent [11 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_VicRasterAdvances [2 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.ManagedVic20_TwoMachines_AgreeAfterFrames_UntilNativeXvicExists [295 ms]
[xUnit.net 00:00:03.12]   Finished:    ViceSharp.TestHarness (ID = 'ab71db7394e73a1adc1130e42fc74f059e0e20545bf7641bf1047ee89aadf982')
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeShim_CreatesVic20Models_WhenXvicOraclePresent(modelSelector: "vic20") [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeShim_CreatesVic20Models_WhenXvicOraclePresent(modelSelector: "vic20ntsc") [8 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeShim_CreatesVic20Models_WhenXvicOraclePresent(modelSelector: "xvic") [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeXvic_StepAdvancesCycle_WhenOraclePresent [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20LockstepScaffoldTests.NativeXvic_ManagedLockstep_CpuRegs_WhenOraclePresent [8 ms]

Test Run Failed.
Total tests: 14
     Passed: 8
     Failed: 6
 Total time: 3.6977 Seconds
TEST_EXIT=1


### Turn 156 (Assistant)
[tool_request: grep]
- pattern: REGS_GET|REGS_SET|maincpu_regs
- path: F:\GitHub\vice-sharp\native\vice\vice\src\mos6510.h
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 1247
- limit: 80
[tool_request: grep]
- pattern: GetP|Flags|ProcessorStatus|\.P
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu
- glob: **/*
- head_limit: 30
[tool_request: grep]
- pattern: GetState|P =
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core
- glob: **/*Machine*
- head_limit: 30

### Turn 157 (Function)
[tool_response]
1247→VICE_SHIM_API uint8_t vice_cpu_get_a(void *machine)
{
    uint8_t value = 0;
1250→
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.a;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
1260→}

VICE_SHIM_API uint8_t vice_cpu_get_x(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
1270→        value = maincpu_regs.x;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_y(void *machine)
{
    uint8_t value = 0;
1280→
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.y;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
1290→}

VICE_SHIM_API uint8_t vice_cpu_get_p(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
1300→        value = (uint8_t)MOS6510_REGS_GET_STATUS(&maincpu_regs);
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
}

VICE_SHIM_API uint8_t vice_cpu_get_sp(void *machine)
{
    uint8_t value = 0;
1310→
    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.sp;
    }
    LeaveCriticalSection(&g_state_lock);

    return value;
1320→}

VICE_SHIM_API uint16_t vice_cpu_get_pc(void *machine)
{
    uint16_t value = 0;

    vice_shim_ensure_sync_primitives();

### Turn 158 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 29 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\mos6510.h
61:#define MOS6510_REGS_SET_A(regs, val) (regs)->a = (val)
62:#define MOS6510_REGS_SET_X(regs, val) (regs)->x = (val)
63:#define MOS6510_REGS_SET_Y(regs, val) (regs)->y = (val)
64:#define MOS6510_REGS_SET_SP(regs, val) (regs)->sp = (val)
65:#define MOS6510_REGS_SET_PC(regs, val) (regs)->pc = (val)
67:#define MOS6510_REGS_SET_OVERFLOW(regs, val) \
76:#define MOS6510_REGS_SET_BREAK(regs, val) \
85:#define MOS6510_REGS_SET_DECIMAL(regs, val) \
94:#define MOS6510_REGS_SET_INTERRUPT(regs, val) \
103:#define MOS6510_REGS_SET_CARRY(regs, val) \
112:#define MOS6510_REGS_SET_SIGN(regs, val) ((regs)->n = (val) ? 0x80 : 0)
114:#define MOS6510_REGS_SET_ZERO(regs, val) ((regs)->z = !(val))
116:#define MOS6510_REGS_SET_STATUS(regs, val)        \
118:     MOS6510_REGS_SET_ZERO(regs, (val) & P_ZERO), \
121:#define MOS6510_REGS_GET_A(regs) ((regs)->a)
122:#define MOS6510_REGS_GET_X(regs) ((regs)->x)
123:#define MOS6510_REGS_GET_Y(regs) ((regs)->y)
124:#define MOS6510_REGS_GET_SP(regs) ((regs)->sp)
125:#define MOS6510_REGS_GET_PC(regs) ((regs)->pc)
126:#define MOS6510_REGS_GET_FLAGS(regs) ((regs)->p)
127:#define MOS6510_REGS_GET_OVERFLOW(regs) ((regs)->p & P_OVERFLOW)
128:#define MOS6510_REGS_GET_BREAK(regs) ((regs)->p & P_BREAK)
129:#define MOS6510_REGS_GET_DECIMAL(regs) ((regs)->p & P_DECIMAL)
130:#define MOS6510_REGS_GET_INTERRUPT(regs) ((regs)->p & P_INTERRUPT)
131:#define MOS6510_REGS_GET_CARRY(regs) ((regs)->p & P_CARRY)
132:#define MOS6510_REGS_GET_SIGN(regs) ((regs)->n & 0x80)
133:#define MOS6510_REGS_GET_ZERO(regs) (!(regs)->z)
134:#define MOS6510_REGS_GET_STATUS(regs)          \
136:     | (MOS6510_REGS_GET_ZERO(regs) ? P_ZERO : 0))
</workspace_result>

### Turn 159 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 5 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
12:    public ClockPhase Phase => ClockPhase.Phi2;
32:    public byte Flags { get => P; set => P = value; }
332:                _pubSub.Publish(
463:        _pubSub?.Publish(CpuControlTransferEvent.Topic, new CpuControlTransferEvent(source, target, returnPc, opcode));
1398:    public byte Peek(ushort address) => _bus.Peek(address);
</workspace_result>

### Turn 160 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\CoordinatorMachine.cs
86:    public MachineState GetState() => _host.GetState();
</workspace_result>

### Turn 161 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\native\vice-shim-vic20.c
- old_string: VICE_SHIM_API uint8_t vice_cpu_get_p(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        value = maincpu_regs.p;
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}
- new_string: VICE_SHIM_API uint8_t vice_cpu_get_p(void *machine)
{
    uint8_t value = 0;

    vice_shim_ensure_sync_primitives();
    EnterCriticalSection(&g_state_lock);
    if (vice_shim_is_active_machine(machine)) {
        /* Reconstruct architectural P from split n/z + p bits (same as C64
           shim). Reading .p alone drops N/Z and the constant 1 bit. */
        value = (uint8_t)MOS6510_REGS_GET_STATUS(&maincpu_regs);
    }
    LeaveCriticalSection(&g_state_lock);
    return value;
}
[tool_request: run_terminal_command]
- command: $env:MSYSTEM = "MINGW64"
& C:\msys64\usr\bin\bash.exe -lc "export PATH=/mingw64/bin:/usr/bin:`$PATH; cd /f/GitHub/vice-sharp/native && bash ./build-vice-shim-xvic.sh" 2>&1 | Select-Object -Last 20
Write-Host "BUILD_EXIT=$LASTEXITCODE"
- description: Rebuild vice_xvic.dll after P-flag fix
- timeout: 300000

### Turn 162 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\native\vice-shim-vic20.c has been updated successfully.

### Turn 163 (Function)
[tool_response]
exit: 0
make: Entering directory '/f/GitHub/vice-sharp/native/vice/vice/src/lib/linenoise-ng'
make: Nothing to be done for 'all'.
make: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src/lib/linenoise-ng'
make: Entering directory '/f/GitHub/vice-sharp/native/vice/vice/src'
make: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src'
F:/GitHub/vice-sharp/native/vice-shim-vic20.c: In function 'vice_shim_set_current_thread_description':
F:/GitHub/vice-sharp/native/vice-shim-vic20.c:260:30: warning: cast between incompatible function types from 'FARPROC' {aka 'long long int (*)(void)'} to 'HRESULT (*)(void *, const WCHAR *)' {aka 'long int (*)(void *, const short unsigned int *)'} [-Wcast-function-type]
  260 |     set_thread_description = (vice_shim_set_thread_description_fn)GetProcAddress(kernel32, "SetThreadDescription");
      |                              ^
Built: /f/GitHub/vice-sharp/native/vice_xvic.dll
-rwxr-xr-x 1 kingd kingd 10909818 Aug  5 14:26 /f/GitHub/vice-sharp/native/vice_xvic.dll
BUILD_EXIT=0


### Turn 164 (Assistant)
[tool_request: run_terminal_command]
- command: # Clear relocated native runtime cache so new vice_xvic.dll is picked up
$cache = Join-Path $env:TEMP "ViceSharpNative"
if (Test-Path $cache) { Remove-Item $cache -Recurse -Force; Write-Host "cleared $cache" }
$env:VICESHARP_ROM_PATH = "F:\GitHub\vice-sharp\native\vice\vice\data"
$log = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\vic20-native-lockstep2.log"
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20NativeLockstep|FullyQualifiedName~Vic20LockstepScaffold" --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 50
Write-Host "TEST_EXIT=$LASTEXITCODE"
- description: Re-run lockstep tests with fresh native cache
- timeout: 180000

### Turn 165 (Function)
[tool_response]
exit: 0
cleared C:\Users\kingd\AppData\Local\Temp\ViceSharpNative
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
[xUnit.net 00:00:03.05]     ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20ntsc", cycles: 256) [FAIL]
[xUnit.net 00:00:03.05]       CPU mismatch after 256 cycles model=vic20ntsc: native PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$25; managed PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$24
[xUnit.net 00:00:03.05]       Stack Trace:
[xUnit.net 00:00:03.05]         F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs(48,0): at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles)
[xUnit.net 00:00:03.05]            at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
[xUnit.net 00:00:03.05]            at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
[xUnit.net 00:00:03.06]       CPU mismatch after 4096 cycles model=vic20: native PC=$FD92 A=$00 X=$D5 Y=$00 S=$FD P=$A5; managed PC=$FD92 A=$00 X=$D5 Y=$00 S=$FD P=$A4
[xUnit.net 00:00:03.06]       Stack Trace:
[xUnit.net 00:00:03.06]     ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 4096) [FAIL]
[xUnit.net 00:00:03.06]         F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs(48,0): at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles)
[xUnit.net 00:00:03.06]            at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
[xUnit.net 00:00:03.07]            at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
[xUnit.net 00:00:03.07]     ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 256) [FAIL]
[xUnit.net 00:00:03.07]       CPU mismatch after 256 cycles model=vic20: native PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$25; managed PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$24
[xUnit.net 00:00:03.07]       Stack Trace:
[xUnit.net 00:00:03.07]         F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs(48,0): at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles)
[xUnit.net 00:00:03.07]            at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
[xUnit.net 00:00:03.07]            at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
[xUnit.net 00:00:03.09]   Finished:    ViceSharp.TestHarness (ID = 'ab71db7394e73a1adc1130e42fc74f059e0e20545bf7641bf1047ee89aadf982')
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20ntsc", cycles: 256) [8 ms]
  Error Message:
   CPU mismatch after 256 cycles model=vic20ntsc: native PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$25; managed PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 4096) [25 ms]
  Error Message:
   CPU mismatch after 4096 cycles model=vic20: native PC=$FD92 A=$00 X=$D5 Y=$00 S=$FD P=$A5; managed PC=$FD92 A=$00 X=$D5 Y=$00 S=$FD P=$A4
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Failed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(modelSelector: "vic20", cycles: 256) [5 ms]
  Error Message:
   CPU mismatch after 256 cycles model=vic20: native PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$25; managed PC=$FD90 A=$00 X=$0B Y=$00 S=$FD P=$24
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(String modelSelector, Int32 cycles) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20NativeLockstepTests.cs:line 48
   at InvokeStub_Vic20NativeLockstepTests.NativeXvic_Managed_CpuRegs_Lockstep(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
  Passed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.LockstepValidator_AcceptsVic20Model_WhenOraclePresent [7 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20NativeLockstepTests.NativeXvic_VicRasterAdvances [3 ms]

Total tests: 14
Test Run Failed.
     Passed: 8
     Failed: 6
 Total time: 3.7004 Seconds
TEST_EXIT=1


### Turn 166 (Assistant)
[tool_request: grep]
- pattern: MachineState|GetState
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core
- glob: **/*.cs
- head_limit: 40
[tool_request: run_terminal_command]
- command: # Find first cycle of P divergence
$probe = @'
using System;
using ViceSharp.Core;
using ViceSharp.TestHarness;

var model = "vic20";
using var native = ViceNative.CreateInstance(model);
native.Reset();
var managed = MachineTestFactory.CreateVic20Machine(model);
managed.Reset();

for (var i = 0; i < 512; i++)
{
    native.Step();
    managed.Clock.Step();
    var n = native.GetState();
    var m = managed.GetState();
    if (n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P)
    {
        Console.WriteLine($"DIV at cycle {i+1}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2} | managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
        // continue a few more
        for (var j = 0; j < 5; j++)
        {
            native.Step(); managed.Clock.Step();
            n = native.GetState(); m = managed.GetState();
            Console.WriteLine($"  +{j+1}: nP=${n.P:X2} mP=${m.P:X2} nPC=${n.PC:X4} mPC=${m.PC:X4}");
        }
        return;
    }
}
Console.WriteLine("no divergence in 512 cycles");
'@
$probePath = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\PdivProbe.csx"
# Use a small console project instead - csx may not ref harness
# Inline via unit test approach: write a one-shot test and run it
$test = @'
namespace ViceSharp.TestHarness.Vic20;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        for (var i = 0; i < 512; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            if (n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P)
            {
                throw new Xunit.Sdk.XunitException($"DIV at cycle {i+1}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2} | managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
            }
        }
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=detailed" 2>&1 | Select-Object -Last 40
Write-Host "EXIT=$LASTEXITCODE"
- description: Probe first cycle of P register divergence
- timeout: 180000

### Turn 167 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 13 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
610:    public MachineState GetState()
612:        return new MachineState

F:\GitHub\vice-sharp\src\ViceSharp.Core\CoordinatorMachine.cs
86:    public MachineState GetState() => _host.GetState();

F:\GitHub\vice-sharp\src\ViceSharp.Core\Snapshots\RuntimeSnapshotStore.cs
18:        return new RuntimeSnapshot(machine.GetState(), memory);

F:\GitHub\vice-sharp\src\ViceSharp.Core\Snapshots\RuntimeSnapshot.cs
11:    public RuntimeSnapshot(MachineState state, ReadOnlySpan<byte> memory)
26:    public MachineState State { get; private set; }
53:        State = new MachineState

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
220:    public static partial void SidExactGetState(IntPtr instance, ref ViceSidExactState state);
708:            // Re-baseline so GetState().Cycle counts forward from the loaded
738:        public MachineState GetState()
740:            return new MachineState

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
321:        public MachineState GetState()
323:            return new MachineState
</workspace_result>

### Turn 168 (Function)
[tool_response]
<task-id>call-232ae055-7b89-421e-8111-741363743ef2-126</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-232ae055-7b89-421e-8111-741363743ef2-126.log</output-file>
<status>running</status>
<summary>Command "# Find first cycle of P divergence
$probe = @'
using System;
using ViceSharp.Core;
using ViceSharp.TestHarness;

var model = "vic20";
using var native = ViceNative.CreateInstance(model);
native.Reset();
var managed = MachineTestFactory.CreateVic20Machine(model);
managed.Reset();

for (var i = 0; i < 512; i++)
{
    native.Step();
    managed.Clock.Step();
    var n = native.GetState();
    var m = managed.GetState();
    if (n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P)
    {
        Console.WriteLine($"DIV at cycle {i+1}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2} | managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
        // continue a few more
        for (var j = 0; j < 5; j++)
        {
            native.Step(); managed.Clock.Step();
            n = native.GetState(); m = managed.GetState();
            Console.WriteLine($"  +{j+1}: nP=${n.P:X2} mP=${m.P:X2} nPC=${n.PC:X4} mPC=${m.PC:X4}");
        }
        return;
    }
}
Console.WriteLine("no divergence in 512 cycles");
'@
$probePath = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\PdivProbe.csx"
# Use a small console project instead - csx may not ref harness
# Inline via unit test approach: write a one-shot test and run it
$test = @'
namespace ViceSharp.TestHarness.Vic20;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        for (var i = 0; i < 512; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            if (n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P)
            {
                throw new Xunit.Sdk.XunitException($"DIV at cycle {i+1}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2} | managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
            }
        }
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=detailed" 2>&1 | Select-Object -Last 40
Write-Host "EXIT=$LASTEXITCODE"" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-232ae055-7b89-421e-8111-741363743ef2-126"] to retrieve the output.

### Turn 169 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-232ae055-7b89-421e-8111-741363743ef2-126"]
- timeout_ms: 180000
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 600
- limit: 40
[tool_request: grep]
- pattern: public byte P|get => P|set => P|_p
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 40

### Turn 170 (Function)
[tool_response]
600→
    public void StepInstruction()
    {
        do
        {
            _clock.Step();
        }
        while (!_cpu.IsInstructionBoundary);
    }

610→    public MachineState GetState()
    {
        return new MachineState
        {
            A = _cpu.A,
            X = _cpu.X,
            Y = _cpu.Y,
            S = _cpu.S,
            P = _cpu.P,
            PC = _cpu.PC,
620→            Cycle = _clock.TotalCycles
        };
    }

    public void Reset()
    {
        _clock.Reset();
        foreach (var device in _devices.All.Where(device => device is not IClockedDevice))
        {
            device.Reset();
630→        }
        _cpu.Reset();
    }
}

/// <summary>
/// Default IDeviceRegistry implementation.
/// </summary>
internal sealed class DeviceRegistry : IDeviceRegistry
{

### Turn 171 (Function)
[tool_response]
=== Task call-232ae055-7b89-421e-8111-741363743ef2-126 ===
Command: # Find first cycle of P divergence
$probe = @'
using System;
using ViceSharp.Core;
using ViceSharp.TestHarness;

var model = "vic20";
using var native = ViceNative.CreateInstance(model);
native.Reset();
var managed = MachineTestFactory.CreateVic20Machine(model);
managed.Reset();

for (var i = 0; i < 512; i++)
{
    native.Step();
    managed.Clock.Step();
    var n = native.GetState();
    var m = managed.GetState();
    if (n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P)
    {
        Console.WriteLine($"DIV at cycle {i+1}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2} | managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
        // continue a few more
        for (var j = 0; j < 5; j++)
        {
            native.Step(); managed.Clock.Step();
            n = native.GetState(); m = managed.GetState();
            Console.WriteLine($"  +{j+1}: nP=${n.P:X2} mP=${m.P:X2} nPC=${n.PC:X4} mPC=${m.PC:X4}");
        }
        return;
    }
}
Console.WriteLine("no divergence in 512 cycles");
'@
$probePath = "C:\Users\kingd\AppData\Local\Temp\grok-goal-607a2dad3956\implementer\PdivProbe.csx"
# Use a small console project instead - csx may not ref harness
# Inline via unit test approach: write a one-shot test and run it
$test = @'
namespace ViceSharp.TestHarness.Vic20;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        for (var i = 0; i < 512; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            if (n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P)
            {
                throw new Xunit.Sdk.XunitException($"DIV at cycle {i+1}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2} | managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
            }
        }
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=detailed" 2>&1 | Select-Object -Last 40
Write-Host "EXIT=$LASTEXITCODE"
Status: completed
Duration: 19.29s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-232ae055-7b89-421e-8111-741363743ef2-126.log

=== Output ===
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=7 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=8 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=9 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=10 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=11 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=12 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=13 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=14 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=15 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
xvic vice_machine_step_cycle call=16 machine=000002a31e450830
xvic cycle_checkpoint: granted=1 running=1 paused=0 stop=0
[xUnit.net 00:00:02.28]       DIV at cycle 25: native PC=$FD47 A=$CD X=$05 Y=$00 S=$FD P=$A5 | managed PC=$FD47 A=$CD X=$05 Y=$00 S=$FD P=$A4
[xUnit.net 00:00:02.28]     ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence [FAIL]
[xUnit.net 00:00:02.28]       Stack Trace:
[xUnit.net 00:00:02.28]         F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs(22,0): at ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence()
[xUnit.net 00:00:02.28]            at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
[xUnit.net 00:00:02.28]            at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
[xUnit.net 00:00:02.29]   Finished:    ViceSharp.TestHarness (ID = 'ab71db7394e73a1adc1130e42fc74f059e0e20545bf7641bf1047ee89aadf982')
  Failed ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence [134 ms]
  Error Message:
   DIV at cycle 25: native PC=$FD47 A=$CD X=$05 Y=$00 S=$FD P=$A5 | managed PC=$FD47 A=$CD X=$05 Y=$00 S=$FD P=$A4
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs:line 22
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Test Run Failed.
Total tests: 1
     Failed: 1
 Total time: 2.8351 Seconds
EXIT=1


### Turn 172 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 39 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
19:    private ushort _pc;
24:        get => IsInstructionBoundary ? _pc : _visiblePC;
27:            _pc = value;
32:    public byte Flags { get => P; set => P = value; }
33:    public byte P;
47:            if (_pendingDeferredNzUpdateAfterBranch ||
49:                _pendingDeferredImmediateLoad ||
50:                _pendingDeferredImpliedRegisterCompletion ||
81:            if (_pendingDeferredNzUpdateAfterBranch ||
83:                _pendingDeferredImmediateLoad ||
84:                _pendingDeferredImpliedRegisterCompletion ||
123:        _interruptReturnPc = _pc;
125:        // keeps exporting reg_pc until the JUMP after the vector fetch).
175:                _pc = _interruptVector;
192:    private IPubSub? _pubSub;
214:        _pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
244:    private bool _pendingDeferredNzUpdateAfterBranch;
245:    private bool _pendingDeferredImmediateLoad;
246:    private bool _pendingDeferredImpliedRegisterCompletion;
290:        if (_pendingDeferredNzUpdateAfterBranch)
298:            _instructionPC = _pc;
299:            _visiblePC = _pc;
311:        if (_pendingDeferredImmediateLoad)
317:        if (_pendingDeferredImpliedRegisterCompletion)
330:            if (_instructionExecuted && _pubSub is { SubscriptionCount: > 0 })
332:                _pubSub.Publish(
334:                    new CpuInstructionCompletedEvent(_currentInstructionPc, _opcode, A, X, Y, S, P, _pc));
337:            _instructionPC = _pc;
339:            _currentInstructionPc = _pc; // the instruction about to be fetched here
346:            if (SerialTrapHook is not null && SerialTrapHook(_pc))
351:            _opcode = Read(_pc++);
387:            _pendingDeferredImmediateLoad = false;
432:                _pc = (ushort)(_instructionPC + 2);
433:                _visiblePC = _pc;
434:                Push((byte)(_pc >> 8));
437:                Push((byte)_pc);
451:        var hi = Read(_pc);
463:        _pubSub?.Publish(CpuControlTransferEvent.Topic, new CpuControlTransferEvent(source, target, returnPc, opcode));
489:                _pendingDeferredNzUpdateAfterBranch = true;
</workspace_result>

### Turn 173 (Assistant)
[tool_request: run_terminal_command]
- command: $test = @'
namespace ViceSharp.TestHarness.Vic20;
using System.Text;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var n0 = native.GetState();
        var m0 = managed.GetState();
        var sb = new StringBuilder();
        sb.AppendLine($"reset: nP=${n0.P:X2} mP=${m0.P:X2} nPC=${n0.PC:X4} mPC=${m0.PC:X4}");
        for (var i = 0; i < 40; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            var mark = (n.P != m.P || n.PC != m.PC) ? " <<<" : "";
            sb.AppendLine($"c{i+1}: nPC=${n.PC:X4} nP=${n.P:X2} mPC=${m.PC:X4} mP=${m.P:X2}{mark}");
        }
        throw new Xunit.Sdk.XunitException(sb.ToString());
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=detailed" 2>&1 | Select-String -Pattern "reset:|c\d+:|Error Message|Passed|Failed|Total" | Select-Object -First 60
Write-Host "EXIT=$LASTEXITCODE"
- description: Trace P and PC for first 40 cycles
- timeout: 120000

### Turn 174 (Function)
[tool_response]
<task-id>call-de0db21c-7a5c-4c24-b462-8fb9a7d39062-130</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-de0db21c-7a5c-4c24-b462-8fb9a7d39062-130.log</output-file>
<status>running</status>
<summary>Command "$test = @'
namespace ViceSharp.TestHarness.Vic20;
using System.Text;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var n0 = native.GetState();
        var m0 = managed.GetState();
        var sb = new StringBuilder();
        sb.AppendLine($"reset: nP=${n0.P:X2} mP=${m0.P:X2} nPC=${n0.PC:X4} mPC=${m0.PC:X4}");
        for (var i = 0; i < 40; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            var mark = (n.P != m.P || n.PC != m.PC) ? " <<<" : "";
            sb.AppendLine($"c{i+1}: nPC=${n.PC:X4} nP=${n.P:X2} mPC=${m.PC:X4} mP=${m.P:X2}{mark}");
        }
        throw new Xunit.Sdk.XunitException(sb.ToString());
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=detailed" 2>&1 | Select-String -Pattern "reset:|c\d+:|Error Message|Passed|Failed|Total" | Select-Object -First 60
Write-Host "EXIT=$LASTEXITCODE"" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-de0db21c-7a5c-4c24-b462-8fb9a7d39062-130"] to retrieve the output.

### Turn 175 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-de0db21c-7a5c-4c24-b462-8fb9a7d39062-130"]
- timeout_ms: 120000

### Turn 176 (Function)
[tool_response]
=== Task call-de0db21c-7a5c-4c24-b462-8fb9a7d39062-130 ===
Command: $test = @'
namespace ViceSharp.TestHarness.Vic20;
using System.Text;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var n0 = native.GetState();
        var m0 = managed.GetState();
        var sb = new StringBuilder();
        sb.AppendLine($"reset: nP=${n0.P:X2} mP=${m0.P:X2} nPC=${n0.PC:X4} mPC=${m0.PC:X4}");
        for (var i = 0; i < 40; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            var mark = (n.P != m.P || n.PC != m.PC) ? " <<<" : "";
            sb.AppendLine($"c{i+1}: nPC=${n.PC:X4} nP=${n.P:X2} mPC=${m.PC:X4} mP=${m.P:X2}{mark}");
        }
        throw new Xunit.Sdk.XunitException(sb.ToString());
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=detailed" 2>&1 | Select-String -Pattern "reset:|c\d+:|Error Message|Passed|Failed|Total" | Select-Object -First 60
Write-Host "EXIT=$LASTEXITCODE"
Status: completed
Duration: 21.90s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-de0db21c-7a5c-4c24-b462-8fb9a7d39062-130.log

=== Output ===

A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:02.50]       reset: nP=$26 mP=$26 nPC=$FD22 mPC=$FD22
[xUnit.net 00:00:02.50]       c1: nPC=$FD22 nP=$26 mPC=$FD22 mP=$26
[xUnit.net 00:00:02.50]       c2: nPC=$FD22 nP=$26 mPC=$FD22 mP=$26
[xUnit.net 00:00:02.50]       c3: nPC=$FD24 nP=$A4 mPC=$FD24 mP=$A4
[xUnit.net 00:00:02.50]       c4: nPC=$FD24 nP=$A4 mPC=$FD24 mP=$A4
[xUnit.net 00:00:02.50]       c5: nPC=$FD25 nP=$A4 mPC=$FD25 mP=$A4
[xUnit.net 00:00:02.50]       c6: nPC=$FD25 nP=$A4 mPC=$FD25 mP=$A4
[xUnit.net 00:00:02.50]       c7: nPC=$FD26 nP=$A4 mPC=$FD26 mP=$A4
[xUnit.net 00:00:02.50]       c8: nPC=$FD26 nP=$A4 mPC=$FD26 mP=$A4
[xUnit.net 00:00:02.50]       c9: nPC=$FD27 nP=$A4 mPC=$FD27 mP=$A4
[xUnit.net 00:00:02.50]       c10: nPC=$FD27 nP=$A4 mPC=$FD27 mP=$A4
[xUnit.net 00:00:02.50]       c11: nPC=$FD27 nP=$A4 mPC=$FD27 mP=$A4
[xUnit.net 00:00:02.50]       c12: nPC=$FD29 nP=$A4 mPC=$FD29 mP=$A4
[xUnit.net 00:00:02.50]       c13: nPC=$FD29 nP=$A4 mPC=$FD29 mP=$A4
[xUnit.net 00:00:02.50]       c14: nPC=$FD29 nP=$A4 mPC=$FD29 mP=$A4
[xUnit.net 00:00:02.50]       c15: nPC=$FD3F nP=$A4 mPC=$FD3F mP=$A4
[xUnit.net 00:00:02.50]       c16: nPC=$FD3F nP=$A4 mPC=$FD3F mP=$A4
[xUnit.net 00:00:02.50]       c17: nPC=$FD41 nP=$24 mPC=$FD41 mP=$24
[xUnit.net 00:00:02.50]       c18: nPC=$FD41 nP=$24 mPC=$FD41 mP=$24
[xUnit.net 00:00:02.50]       c19: nPC=$FD41 nP=$24 mPC=$FD41 mP=$24
[xUnit.net 00:00:02.50]       c20: nPC=$FD41 nP=$24 mPC=$FD41 mP=$24
[xUnit.net 00:00:02.50]       c21: nPC=$FD44 nP=$A4 mPC=$FD44 mP=$A4
[xUnit.net 00:00:02.50]       c22: nPC=$FD44 nP=$A4 mPC=$FD44 mP=$A4
[xUnit.net 00:00:02.50]       c23: nPC=$FD44 nP=$A4 mPC=$FD44 mP=$A4
[xUnit.net 00:00:02.50]       c24: nPC=$FD44 nP=$A4 mPC=$FD44 mP=$A4
[xUnit.net 00:00:02.50]       c25: nPC=$FD47 nP=$A5 mPC=$FD47 mP=$A4 <<<
[xUnit.net 00:00:02.50]       c26: nPC=$FD47 nP=$A5 mPC=$FD47 mP=$A4 <<<
[xUnit.net 00:00:02.50]       c27: nPC=$FD49 nP=$A5 mPC=$FD49 mP=$A4 <<<
[xUnit.net 00:00:02.50]       c28: nPC=$FD4C nP=$A5 mPC=$FD4C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c29: nPC=$FD4C nP=$A5 mPC=$FD4C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c30: nPC=$FD4C nP=$A5 mPC=$FD4C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c31: nPC=$FD4C nP=$A5 mPC=$FD4C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c32: nPC=$FD4C nP=$A5 mPC=$FD4C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c33: nPC=$FD4C nP=$A5 mPC=$FD4C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c34: nPC=$FD2A nP=$A5 mPC=$FD2A mP=$A4 <<<
[xUnit.net 00:00:02.50]       c35: nPC=$FD2A nP=$A5 mPC=$FD2A mP=$A4 <<<
[xUnit.net 00:00:02.50]       c36: nPC=$FD2C nP=$A5 mPC=$FD2C mP=$A4 <<<
[xUnit.net 00:00:02.50]       c37: nPC=$FD2F nP=$A5 mPC=$FD2F mP=$A4 <<<
[xUnit.net 00:00:02.50]       c38: nPC=$FD2F nP=$A5 mPC=$FD2F mP=$A4 <<<
[xUnit.net 00:00:02.50]       c39: nPC=$FD2F nP=$A5 mPC=$FD2F mP=$A4 <<<
[xUnit.net 00:00:02.50]       c40: nPC=$FD31 nP=$A5 mPC=$FD31 mP=$A4 <<<
  Failed ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence [136 ms]
  Error Message:
   reset: nP=$26 mP=$26 nPC=$FD22 mPC=$FD22
c1: nPC=$FD22 nP=$26 mPC=$FD22 mP=$26
c2: nPC=$FD22 nP=$26 mPC=$FD22 mP=$26
c3: nPC=$FD24 nP=$A4 mPC=$FD24 mP=$A4
c4: nPC=$FD24 nP=$A4 mPC=$FD24 mP=$A4
c5: nPC=$FD25 nP=$A4 mPC=$FD25 mP=$A4
c6: nPC=$FD25 nP=$A4 mPC=$FD25 mP=$A4
c7: nPC=$FD26 nP=$A4 mPC=$FD26 mP=$A4
c8: nPC=$FD26 nP=$A4 mPC=$FD26 mP=$A4
c9: nPC=$FD27 nP=$A4 mPC=$FD27 mP=$A4
c10: nPC=$FD27 nP=$A4 mPC=$FD27 mP=$A4
c11: nPC=$FD27 nP=$A4 mPC=$FD27 mP=$A4
c12: nPC=$FD29 nP=$A4 mPC=$FD29 mP=$A4
c13: nPC=$FD29 nP=$A4 mPC=$FD29 mP=$A4
c14: nPC=$FD29 nP=$A4 mPC=$FD29 mP=$A4
c15: nPC=$FD3F nP=$A4 mPC=$FD3F mP=$A4
EXIT=



### Turn 177 (Assistant)
[tool_request: run_terminal_command]
- command: $test = @'
namespace ViceSharp.TestHarness.Vic20;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        int firstP = -1, firstPc = -1;
        for (var i = 0; i < 20000; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            if (firstP < 0 && n.P != m.P) firstP = i + 1;
            if (firstPc < 0 && n.PC != m.PC)
            {
                firstPc = i + 1;
                throw new Xunit.Sdk.XunitException($"firstP={firstP} firstPc={firstPc} nPC=${n.PC:X4} mPC=${m.PC:X4} nP=${n.P:X2} mP=${m.P:X2} nA=${n.A:X2} mA=${m.A:X2} nX=${n.X:X2} mX=${m.X:X2}");
            }
        }
        throw new Xunit.Sdk.XunitException($"firstP={firstP} firstPc=none-in-20k");
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=normal" 2>&1 | Select-String -Pattern "firstP|Passed|Failed|Total tests|Error Message" | Select-Object -First 20
Write-Host "EXIT=$LASTEXITCODE"
- description: Find first PC divergence within 20k cycles
- timeout: 180000

### Turn 178 (Function)
[tool_response]
<task-id>call-18031726-4088-4e87-b12e-09e69700ac5b-132</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-18031726-4088-4e87-b12e-09e69700ac5b-132.log</output-file>
<status>running</status>
<summary>Command "$test = @'
namespace ViceSharp.TestHarness.Vic20;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        int firstP = -1, firstPc = -1;
        for (var i = 0; i < 20000; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            if (firstP < 0 && n.P != m.P) firstP = i + 1;
            if (firstPc < 0 && n.PC != m.PC)
            {
                firstPc = i + 1;
                throw new Xunit.Sdk.XunitException($"firstP={firstP} firstPc={firstPc} nPC=${n.PC:X4} mPC=${m.PC:X4} nP=${n.P:X2} mP=${m.P:X2} nA=${n.A:X2} mA=${m.A:X2} nX=${n.X:X2} mX=${m.X:X2}");
            }
        }
        throw new Xunit.Sdk.XunitException($"firstP={firstP} firstPc=none-in-20k");
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=normal" 2>&1 | Select-String -Pattern "firstP|Passed|Failed|Total tests|Error Message" | Select-Object -First 20
Write-Host "EXIT=$LASTEXITCODE"" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-18031726-4088-4e87-b12e-09e69700ac5b-132"] to retrieve the output.

### Turn 179 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-18031726-4088-4e87-b12e-09e69700ac5b-132"]
- timeout_ms: 180000

### Turn 180 (Function)
[tool_response]
=== Task call-18031726-4088-4e87-b12e-09e69700ac5b-132 ===
Command: $test = @'
namespace ViceSharp.TestHarness.Vic20;
using ViceSharp.Core;
using Xunit;
public sealed class Vic20PdivProbe
{
    [Fact]
    public void Probe_FirstPDivergence()
    {
        if (!ViceNativeXvic.IsAvailable) return;
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        int firstP = -1, firstPc = -1;
        for (var i = 0; i < 20000; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            if (firstP < 0 && n.P != m.P) firstP = i + 1;
            if (firstPc < 0 && n.PC != m.PC)
            {
                firstPc = i + 1;
                throw new Xunit.Sdk.XunitException($"firstP={firstP} firstPc={firstPc} nPC=${n.PC:X4} mPC=${m.PC:X4} nP=${n.P:X2} mP=${m.P:X2} nA=${n.A:X2} mA=${m.A:X2} nX=${n.X:X2} mX=${m.X:X2}");
            }
        }
        throw new Xunit.Sdk.XunitException($"firstP={firstP} firstPc=none-in-20k");
    }
}
'@
Set-Content -Path "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs" -Value $test
if (Test-Path "$env:TEMP\ViceSharpNative") { Remove-Item "$env:TEMP\ViceSharpNative" -Recurse -Force }
dotnet test F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20PdivProbe" --logger "console;verbosity=normal" 2>&1 | Select-String -Pattern "firstP|Passed|Failed|Total tests|Error Message" | Select-Object -First 20
Write-Host "EXIT=$LASTEXITCODE"
Status: completed
Duration: 29.39s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-18031726-4088-4e87-b12e-09e69700ac5b-132.log

=== Output ===

[xUnit.net 00:00:03.48]       firstP=25 firstPc=5005 nPC=$FDB1 mPC=$FDB3 nP=$24 mP=$24 nA=$00 mA=$00 nX=$04 mX=$FF
[xUnit.net 00:00:03.48]     ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence [FAIL]
[xUnit.net 00:00:03.48]         F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs(25,0): at 
ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence()
  Failed ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence [769 ms]
  Error Message:
   firstP=25 firstPc=5005 nPC=$FDB1 mPC=$FDB3 nP=$24 mP=$24 nA=$00 mA=$00 nX=$04 mX=$FF
     at ViceSharp.TestHarness.Vic20.Vic20PdivProbe.Probe_FirstPDivergence() in 
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20PdivProbe.cs:line 25
Test Run Failed.
Total tests: 1
     Failed: 1
EXIT=1



### Turn 181 (Assistant)
[tool_request: grep]
- pattern: case 0xC9|CMP|SetFlag.*Carry|Carry
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 40
[tool_request: run_terminal_command]
- command: # What opcode is at $FD44 in VIC20 kernal?
$romRoots = @("F:\GitHub\vice-sharp\native\vice\vice\data\VIC20", "$env:VICESHARP_ROM_PATH\VIC20")
foreach ($r in $romRoots) {
  if (Test-Path $r) { Get-ChildItem $r | Select-Object Name; break }
}
$kernal = Get-ChildItem "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20" -Filter "*kernal*" -ErrorAction SilentlyContinue | Select-Object -First 3 FullName, Length
$kernal
# VIC20 kernal maps at $E000, so $FD44 is offset $1D44 from $E000
$kpath = (Get-ChildItem "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20" -Filter "kernal*" | Select-Object -First 1).FullName
if ($kpath) {
  $bytes = [System.IO.File]::ReadAllBytes($kpath)
  $off = 0xFD44 - 0xE000
  $slice = $bytes[$off..($off+15)]
  ($slice | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
  Write-Host "file=$kpath len=$($bytes.Length) off=$off"
}
- description: Disassemble kernal bytes at divergence PC

### Turn 182 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 18 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
227:    private bool _stagedCarryUpdate;
228:    private bool _stagedCarryValue;
358:            _stagedCarryUpdate = false;
359:            _stagedCarryValue = false;
379:            // cycle (CMP abs C4) and its staged apply on the native commit-export
515:            if (_stagedCarryUpdate)
517:                if (_stagedCarryValue)
522:                _stagedCarryUpdate = false;
931:        _stagedCarryUpdate = false;
932:        _stagedCarryValue = false;
1042:    /// pushes, the staged indexed loads LDY abs,X / LDX abs,Y, CMP (zp),Y and
1091:        _stagedCarryUpdate = true;
1092:        _stagedCarryValue = A >= value;
1141:        if (_stagedCarryUpdate)
1143:            if (_stagedCarryValue)
1148:            _stagedCarryUpdate = false;
1368:        _stagedCarryUpdate = false;
1369:        _stagedCarryValue = false;
</workspace_result>

### Turn 183 (Function)
[tool_response]
exit: 0

file=F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-02.bin len=8192 off=7492
Name
----
basic-901486-01.bin
chargen-901460-02.bin
chargen-901460-03.bin
colodore_vic.vpl
default.vrs
gtk3_pos_de.vkm
gtk3_pos_it.vkm
gtk3_pos.vkm
gtk3_sym_de.vkm
gtk3_sym_fr.vkm
gtk3_sym_it.vkm
gtk3_sym.vkm
kernal.901486-02.bin
kernal.901486-06.bin
kernal.901486-07.bin
Makefile
Makefile.am
Makefile.in
mike-ntsc.vpl
mike-pal.vpl
PALette.vpl
sdl_pos_de.vkm
sdl_pos_fi.vkm
sdl_pos_fr.vkm
sdl_pos_uk.vkm
sdl_pos.vkm
sdl_sym_de.vkm
sdl_sym_uk.vkm
sdl_sym.vkm
vice.vpl



DD 03 A0 D0 03 CA D0 F5 60 41 30 C3 C2 CD A2 6D



### Turn 184 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 350
- limit: 50
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 500
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1070
- limit: 90
[tool_request: run_terminal_command]
- command: # Disassemble around $FD40
# DD 03 A0 = CMP $A003? Wait little endian: DD = CMP abs,X; 03 A0 = $A003
# Actually DD = CMP abs,X; operand 03 A0 means $A003,X
# Then D0 03 = BNE +3; CA = DEX; D0 F5 = BNE -11; 60 = RTS
# At c24-c25 PC $FD44 - let's map
$bytes = [System.IO.File]::ReadAllBytes("F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-02.bin")
foreach ($addr in 0xFD20..0xFD50) {
  $off = $addr - 0xE000
  Write-Host ("{0:X4}: {1:X2}" -f $addr, $bytes[$off])
}
- description: Hex dump kernal around divergence

### Turn 185 (Function)
[tool_response]
1070→    {
        var value = (byte)(Read(address) + 1);
        _bus.Write(address, value);
        UpdateNZ(value);
        FinishStagedMemoryWrite(instructionLength);
    }

    private void DecrementStagedMemory(ushort address, int instructionLength)
    {
        var value = (byte)(Read(address) - 1);
1080→        _bus.Write(address, value);
        UpdateNZ(value);
        FinishStagedMemoryWrite(instructionLength);
    }

    private void CompareStagedMemory(ushort address, int instructionLength)
    {
        var value = Read(address);
        _pc = (ushort)(_instructionPC + instructionLength);
        _visiblePC = _instructionPC;
1090→        _stagedMemoryReadCompleted = true;
        _stagedCarryUpdate = true;
        _stagedCarryValue = A >= value;
        _stagedNzUpdate = true;
        _stagedNzValue = (byte)(A - value);
    }

    private void CompleteDeferredImmediateLoad()
    {
        var value = Read((ushort)(_instructionPC + 1));
1100→        switch (_opcode)
        {
            case 0xA0:
                Y = value;
                break;
            case 0xA2:
                X = value;
                break;
            case 0xA9:
                A = value;
1110→                break;
        }

        UpdateNZ(value);
        _pc = (ushort)(_instructionPC + 2);
        _visiblePC = _pc;
        _pendingDeferredImmediateLoad = false;
    }

    private void CompleteDeferredImpliedRegisterCompletion()
1120→    {
        ExecuteOpcode(_opcode);
        _instructionPC = _pc;
        _visiblePC = _pc;
        _pendingDeferredImpliedRegisterCompletion = false;
    }

    private bool TryDeferImpliedRegisterCompletionAfterBranch()
    {
        if (!_deferImpliedRegisterCompletionAfterBranch || _cycle != 0)
1130→            return false;

        _deferImpliedRegisterCompletionAfterBranch = false;
        _pendingDeferredImpliedRegisterCompletion = true;
        _visiblePC = _instructionPC;
        _suppressBootstrapBoundary = true;
        return true;
    }

    private void CompleteDeferredNzUpdateAfterBranch()
1140→    {
        if (_stagedCarryUpdate)
        {
            if (_stagedCarryValue)
                P |= 0x01;
            else
                P &= 0xFE;

            _stagedCarryUpdate = false;
        }
1150→
        if (_stagedNzUpdate)
        {
            UpdateNZ(_stagedNzValue);
            _stagedNzUpdate = false;
        }

        _pendingDeferredNzUpdateAfterBranch = false;
    }


### Turn 186 (Function)
[tool_response]
500→                _stagedMemoryReadCompleted = false;
                _suppressBootstrapBoundary = true;
                return true;
            }

            if (_deferIndirectYLoadCompletionAfterBranch)
            {
                A = _stagedNzValue;
                _deferIndirectYLoadCompletionAfterBranch = false;
                _pendingDeferredNzUpdateAfterBranch = true;
510→                _stagedMemoryReadCompleted = false;
                _suppressBootstrapBoundary = true;
                return true;
            }

            if (_stagedCarryUpdate)
            {
                if (_stagedCarryValue)
                    P |= 0x01;
                else
520→                    P &= 0xFE;

                _stagedCarryUpdate = false;
            }

            if (_stagedNzUpdate)
            {
                UpdateNZ(_stagedNzValue);
                _stagedNzUpdate = false;
            }
530→
            _instructionPC = _pc;
            _visiblePC = _pc;
            _stagedMemoryReadCompleted = false;
            return true;
        }

        if (_cycle == 0 && _deferImmediateLoadAfterBranch)
        {
            _deferImmediateLoadAfterBranch = false;


[... TRUNCATED at 524288 bytes, 342 turns omitted ...]
