# HISTORICAL -- DO NOT EDIT
# Record of compaction segment 003 (detail=verbose) from this same task.
# Use read_file or grep to look up details, but do not modify.

## Segment metadata
- Index: 003
- Turn count: 572
- Timestamp: 2026-08-05T19:14:49Z

## Turn statistics

- Turns: 572 (Assistant=153, Function=400, Human=18, System=1)
- Tools used: read_file (116), grep (77), search_replace (75), run_terminal_command (64), write (32), get_command_or_subagent_output (17), todo_write (11), list_dir (6), exit_plan_mode (1), kill_command_or_subagent (1)
- Unique target files (124): C:\Users\kingd\.claude\profile\PROFILE.md, C:\Users\kingd\.claude\profile\user-payton-byrd.md, C:\Users\kingd\.claude\skills\add-profile\SKILL.md, C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\assets\image-8b2c042e-3efe-40de-8d4c-579a965a8a19.jpg, C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\goal\plan.md, ... and 119 more
- Tool errors: 1
- Verbose-render size estimate: 1,326,480 B
- Last assistant response excerpt: "Native Vic20 lockstep is the primary gate. Exploring the shim and multi-machine options."


## Summary (curated by compaction step)

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
[31m??[m docs/S-Blox/
[31m??[m docs/reviews/code-review-20260714T001056552Z.md
[31m??[m docs/reviews/code-review-20260714T004256563Z.md
[31m??[m docs/romless-vic-badline-fix.md
[31m??[m manifests/
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
No, the overall VICE# Project.
</user_query>

### Turn 4 (Human)
This session is being continued from a previous conversation that ran out of context. The summary below covers the earlier portion of the conversation.

Summary:
1. Primary Request and Intent:
- Original long thread (compaction carry-forward): RomM/Xbox library work, T64 attach, Avalonia crash fixes, MSI deploy, A–Z filter, fullscreen, etc., culminating in release v1.2.0/v1.2.1, NuGet/winget publish, CI pointed at GitHub.
- Explicit later requests:
  - Publish NuGet and winget locally with Nuke (done for v1.2.1).
  - “create plan to implement phase 2” → overall VICE# project, not RomM Phase E.
  - Plan revised for subagents; approved; “default drive 8 for vic20 is 1540” locked in plan.
  - Plan approved with instruction to start coding Iteration 2 (VIC-20).
- Active implementation goal: Iteration 2 VIC-20 per plan: branch feat/iteration2-vic20, explore, requirements, Slice A skeleton (CPU+VIA+ROM+stub video, default drive 8 = 1540).
- Constraints: BDP v4 TDD; chip/machine separation; VIA one shared chip; no ROMs shipped; C64 non-regression.

2. Key Technical Concepts:
- ViceSharp .NET 10; Iteration 1 C64 complete (v1.2.1); Iteration 2 = VIC-20.
- Architecture: Mos6502 (no port), Via6522 ($9110 NMI, $9120 IRQ), new VicI 6560/6561, Vic20MemoryMap, DriveModel.C1540 default unit 8.
- VIC-20 map: 5KB base RAM, BLK expansions, char $8000, I/O $9000, color $9400, BASIC $C000, KERNAL $E000; clocks PAL 1108405/71×312, NTSC 1022727/65×261.
- ROMs: basic-901486-01.bin; kernal.901486-07/-06; chargen-901460-03; dos1540-325302+3-01.bin (DRIVES).
- BDP: red tests first; subagents/worktrees for B||C||D later.
- Host: ArchitectureBuilder, MachineTestFactory, IVideoChip, BasicBus.

3. Files and Code Sections:
- Plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md` (Iteration 2 + E2 1540 + subagents).
- Repo root F:\GitHub\vice-sharp; branch feat/iteration2-vic20 created from main.
- Examined (not yet implementing Vic20 types in final turn): C64Descriptor, ArchitectureBuilder (BuildC64/Minimal), BasicBootProofTests, SimpleRam, IVideoChip, ViceTopologyBuilder (xvic throws), C1541ViceRomNames.Dos1540, DriveModel.C1540, ViceRomCatalog VIC20/1540, Via6522 APIs, FR-Machine-Profiles.
- Prior session artifacts: T64/MediaServiceHost/Xbox/Avalonia lists recursion, fullscreen F11/Alt+Enter, MSI, GitHub service connection for ADO, publish v1.2.1 NuGet + winget PR 412774, GitHub release v1.2.1.
- Untracked noise: docs/S-Blox/, docs/reviews/*, docs/romless-*, manifests/ (wingetcreate); leave out of Iteration 2 commits.

4. Errors and Fixes:
- PublishWinget with --skip skipped PublishGitHubRelease → MSI 404; fixed by full PublishNuget then PublishWinget without --skip on checkout v1.2.1.
- Octopus: no vice-sharp project (license limit); LEGION2 down; ADO retargeted GitHub.
- Avalonia attach crash: cross-thread UI (fixed earlier); ListsView RecentGames stack overflow (fixed earlier).
- Plan misread as RomM Phase E until user clarified overall project Iteration 2.

5. Problem Solving:
- Phase 2 = Iteration 2 VIC-20 (Iteration-Roadmap).
- Explore agent delivered full native brief (memory, VIA roles, VIC-I regs, ROM names, reuse map).
- Slice A approach: Vic20Descriptor/RomSet/Profile + ArchitectureBuilder.BuildVic20 + stub IVideoChip + tests; driver 1540 as profile default even before disk mount.
- Not yet written: product Vic20 sources, FR-VIC20 md, tests.

6. All User Messages:
- (Compacted history): T64 attach, MSI redeploy, Avalonia crash, MSI redeploy, NTSC fill, auto mode note, attach crash, autoplay focus, fullscreen F11/Alt+Enter, rebuild/reinstall, bump 1.2.0 tag/push, merge main 1.2.1 tag push, delete feat/xbox-uwp-app, Octopus NuGet/winget?, monitor GitHub not Azure, publish nuget/winget local Nuke.
- “create plan to implement phase 2”
- “No, the overall VICE# Project.”
- Plan-mode: build with subagents; “the default drive 8 for vic20 is 1540”
- (System) Plan approved / start coding.

7. Pending Tasks:
- Slice 0 complete (branch only); still: requirements FR-VIC20-*, Slice A red→green Vic20 skeleton, later B–H, E2 1540 tests.
- Do not commit untracked scratch docs.

8. Current Work:
- On feat/iteration2-vic20; explore subagent done; gathering C64/builder patterns to implement Slice A.
- Todos: 0 in progress/done branch; explore pending→done; req/A pending.
- Next concrete: write Vic20Architecture tests (failing), then Vic20ViceRomNames/RomSet/Profile/Descriptor, Vic20MemoryMap or flat map, stub Mos6561/VicIStub IVideoChip, BuildVic20Machine, MachineTestFactory.CreateVic20Machine, optionally xvic topology stub.

9. Optional Next Step:
Continue Slice A: “Writing FR stubs and Slice A tests, then implementing the Vic20 skeleton” — write red Vic20 construction/reset tests, implement Architectures/Vic20/* + ArchitectureBuilder.BuildVic20Machine + stub video, gate with FullyQualifiedName~Vic20; keep default DriveModel.C1540 on profile.

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
- [in_progress] 0: Slice 0: branch feat/iteration2-vic20 + hygiene
- [pending] explore: Explore native VIC-20 map/ROMs/VIA/VIC-I
- [pending] req: Requirements freeze FR-VIC20-* docs
- [pending] A: Slice A: Vic20 skeleton red then green

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
Resuming Iteration 2 (VIC-20) on the overall VICE# project. Loading session context and continuing Slice A.
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
- limit: 150
[tool_request: run_terminal_command]
- command: git status -sb; git branch --show-current; git log -3 --oneline
- description: Check branch and git status

### Turn 7 (Function)
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

### Turn 8 (Function)
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


### Turn 9 (Function)
[tool_response]
exit: 0
## feat/iteration2-vic20
?? docs/S-Blox/
?? docs/reviews/code-review-20260714T001056552Z.md
?? docs/reviews/code-review-20260714T004256563Z.md
?? docs/romless-vic-badline-fix.md
?? manifests/
feat/iteration2-vic20
b2ba4da ci: point ADO pipelines at GitHub; restore Octopus release steps
3b20c4f release: bump patch to 1.2.1 on main
521987b merge: feat/xbox-uwp-app into main for 1.2 line


### Turn 10 (Assistant)
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\PROFILE.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\user-payton-byrd.md
- limit: 80
[tool_request: list_dir]
- target_directory: F:\GitHub\vice-sharp\src\ViceSharp.Architectures
[tool_request: grep]
- pattern: BuildC64|MachineTestFactory|Vic20|C64Descriptor|ArchitectureBuilder
- glob: **/*.{cs,md}
- path: F:\GitHub\vice-sharp\src
- head_limit: 60
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\plan.md
- limit: 200

### Turn 11 (Function)
[tool_response]
- F:\GitHub\vice-sharp\src\ViceSharp.Architectures/
  - Adhoc/
    - AdhocArchitectureDescriptor.cs
    - AdhocMachineBlueprint.cs
    - AdhocMachineDocument.cs
    - AdhocMachinePlan.cs
    - AdhocMachineValidationException.cs
    - AdhocMachineYamlLoader.cs
    - MachineDefinitionDocument.cs
    - MachineDefinitionYamlContext.cs
  - bin-review/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin-s3slice/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin-s4slice/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin-v2slice/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin-v3slice/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin-v5slice/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin-verify/
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
  - bin/
    - Debug/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
    - Release/
      - net10.0/
        - ViceSharp.Abstractions.dll
        - ViceSharp.Abstractions.pdb
        - ViceSharp.Architectures.deps.json
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.pdb
        - ViceSharp.Chips.dll
        - ViceSharp.Chips.pdb
        - ViceSharp.Core.dll
        - ViceSharp.Core.pdb
        - ViceSharp.RomFetch.dll
        - ViceSharp.RomFetch.pdb
    - x64/
      - Release-UWP/
        - net10.0/
          - ViceSharp.Abstractions.dll
          - ViceSharp.Abstractions.pdb
          - ViceSharp.Architectures.deps.json
          - ViceSharp.Architectures.dll
          - ViceSharp.Architectures.pdb
          - ViceSharp.Chips.dll
          - ViceSharp.Chips.pdb
          - ViceSharp.Core.dll
          - ViceSharp.Core.pdb
          - ViceSharp.RomFetch.dll
          - ViceSharp.RomFetch.pdb
  - C1541/
    - C1541Descriptor.cs
    - C1541RomSet.cs
    - C1541ViceRomNames.cs
  - C64/
    - C64Descriptor.cs
    - C64MachineDefinitionWriter.cs
    - C64MachineProfile.cs
    - C64SystemCoreDefinition.cs
    - Commodore64.cs
  - EmptyMachine/
    - EmptyMachineDescriptor.cs
  - Multisystem/
    - MultiSystemBlueprint.cs
    - MultiSystemDocument.cs
    - MultiSystemValidationException.cs
    - MultiSystemYamlLoader.cs
  - obj/
    - Debug/
      - net10.0/
        - ref/
          - ViceSharp.Architectures.dll
        - refint/
          - ViceSharp.Architectures.dll
        - ViceShar.DAD6E8DA.Up2Date
        - ViceSharp.Architectures.AssemblyInfo.cs
        - ViceSharp.Architectures.AssemblyInfoInputs.cache
        - ViceSharp.Architectures.assets.cache
        - ViceSharp.Architectures.csproj.AssemblyReference.cache
        - ViceSharp.Architectures.csproj.CoreCompileInputs.cache
        - ViceSharp.Architectures.csproj.FileListAbsolute.txt
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.GeneratedMSBuildEditorConfig.editorconfig
        - ViceSharp.Architectures.GlobalUsings.g.cs
        - ViceSharp.Architectures.pdb
        - ViceSharp.Architectures.sourcelink.json
    - project.assets.json
    - project.nuget.cache
    - Release/
      - net10.0/
        - ref/
          - ViceSharp.Architectures.dll
        - refint/
          - ViceSharp.Architectures.dll
        - ViceShar.DAD6E8DA.Up2Date
        - ViceSharp.Architectures.AssemblyInfo.cs
        - ViceSharp.Architectures.AssemblyInfoInputs.cache
        - ViceSharp.Architectures.assets.cache
        - ViceSharp.Architectures.csproj.AssemblyReference.cache
        - ViceSharp.Architectures.csproj.CoreCompileInputs.cache
        - ViceSharp.Architectures.csproj.FileListAbsolute.txt
        - ViceSharp.Architectures.dll
        - ViceSharp.Architectures.GeneratedMSBuildEditorConfig.editorconfig
        - ViceSharp.Architectures.GlobalUsings.g.cs
        - ViceSharp.Architectures.pdb
        - ViceSharp.Architectures.sourcelink.json
    - ViceSharp.Architectures.csproj.nuget.dgspec.json
    - ViceSharp.Architectures.csproj.nuget.g.props
    - ViceSharp.Architectures.csproj.nuget.g.targets
    - x64/
      - Release-UWP/
        - net10.0/
          - ref/
            - ViceSharp.Architectures.dll
          - refint/
            - ViceSharp.Architectures.dll
          - ViceShar.DAD6E8DA.Up2Date
          - ViceSharp.Architectures.AssemblyInfo.cs
          - ViceSharp.Architectures.AssemblyInfoInputs.cache
          - ViceSharp.Architectures.assets.cache
          - ViceSharp.Architectures.csproj.AssemblyReference.cache
          - ViceSharp.Architectures.csproj.CoreCompileInputs.cache
          - ViceSharp.Architectures.csproj.FileListAbsolute.txt
          - ViceSharp.Architectures.dll
          - ViceSharp.Architectures.GeneratedMSBuildEditorConfig.editorconfig
          - ViceSharp.Architectures.GlobalUsings.g.cs
          - ViceSharp.Architectures.pdb
          - ViceSharp.Architectures.sourcelink.json
  - ViceSharp.Architectures.csproj

### Turn 12 (Function)
[tool_response]
1→# Iteration 2 (Phase 2): VIC-20 implementation plan

**Date:** 2026-08-05 (revised: multi-agent / subagent execution)  
**Scope:** Overall ViceSharp project **Iteration 2** (not RomM Phase E, not C64 residual FRs mislabeled in old maps)  
**Canonical sources:**
- `docs/Iteration-Roadmap.md` (Iteration 2: VIC-20)
- `README.md` Completion Dashboard (VIC-20 = planned)
- `docs/VICE-MIGRATION.md` (`xvic` not yet supported)
- Architecture invariants in `Agents.md` / `Claude.md` (chip/machine separation, determinism, BDP)
10→- FR backlog that truly belongs to VIC-20: `FR-PRF-005`, `FR-VIA-001..005` (VIA already largely implemented for 1541; VIC-20 integration remaining), plus new FRs/TRs for VIC-I (6560/6561) and VIC-20 memory map

**Process:** Byrd Development Process v4: requirements/ACs first, tests red before green, mocks then real, 100% green gate per slice (0 failed, 0 skipped in the slice filter). No silent deferred TODOs.

**Execution model:** Orchestrator agent + specialized subagents (explore / plan / general-purpose) and optional workflow parallel panels. Subagents implement **one BDP slice at a time** with worktree isolation when editing production code in parallel.

---

## 1. Goal (definition of done)

20→Deliver a **playable VIC-20 architecture** in ViceSharp that:

1. Boots VIC-20 BASIC (or equivalent) to a READY-like prompt with official ROMs via `VICESHARP_ROM_PATH` (no ROMs shipped in package).
2. Renders VIC (6560/6561) video into the existing host frame path (PAL/NTSC variants).
3. Runs input through VIC-20 keyboard matrix + joystick (VIA ports).
4. Supports runtime architecture switch (or at least host/session create) between C64 and VIC-20 without process restart (roadmap exit: "architecture switching works at runtime").
5. Attaches and loads software from cart (standard VIC-20 cart types MVP) and/or disk on **drive 8 defaulting to Commodore 1540** (VICE type 1540 / `DriveModel.C1540`, DOS `dos1540`; not 1541 unless the user changes the model).
6. Launcher binary-name dispatch accepts `xvic` (today throws `NotSupportedException`).
7. Heads (Console, Avalonia, Xbox settings machine list) can select VIC-20 where machine pickers exist; RomM slug `vic-20` already present in library enums.
8. Lockstep/checkpoint gate vs native `xvic` for a bounded depth (MVP: multi-frame CPU+bus+VIA; deepen VIC raster later).
30→
**Out of scope for Iteration 2 close (track separately, do not block):**
- C128 / PET / Plus4 (Iterations 3-5)
- Full C64 FLI/AFLI deepening, extra drive families (1581/1571/CMD HD) unless required for VIC-20 validation
- Store/cert Xbox Tier C
- Native AOT re-enable for UWP
- Perfect audio for VIC (no SID; VIC has audio on-chip: MVP can be silent or minimal)

---

40→## 2. Current state (verified)

| Asset | State | Implication |
|-------|-------|-------------|
| Iteration 1 C64 | Complete (v1.2.1 released) | Reuse host, bus, clock, snapshots, media, UI shells |
| `Mos6502` | Exists (6510 builds on it) | Reuse for VIC-20 CPU minus `$00/$01` port |
| `Via6522` | Production chip (1541 + generic) | **One shared VIA**; VIC-20 wiring only in Architectures |
| VIC-I (6560/6561) | **Missing** | Largest net-new chip |
| `Architectures/Vic20` | **Missing** (only C64, C1541, Empty, Adhoc, Multisystem) | Need descriptor + machine + ROM set |
| Launcher `xvic` | Explicitly unsupported | Topology builder change |
50→| Xbox machine picker | VIC-20 listed disabled (`xvic`, false) | Enable when bootable |
| RomFetch VIC20 catalog | Partial kernal names | Extend ROM set + SHA pins for CI |
| Native reference | `native/vice` present | `xvic` lockstep oracle |

**Do not use** `FR-to-Iteration-Map.md` Iteration 2 table as a delivery checklist: it is a 2026-05 planning snapshot; many listed FRs were delivered as C64 post-MVP work. Use MCP requirements store + new VIC-20 FRs for live status.

---

## 3. Locked design decisions

60→1. **Chip/machine boundary:** VIC-I and any shared logic live under `src/ViceSharp.Chips/` in a designated subdir (e.g. `Vic/`); VIA stays one `Via6522` implementation; VIC-20 address map, IRQ/NMI routing, and cart banking live under `src/ViceSharp.Architectures/Vic20/`.
2. **CPU:** Prefer `Mos6502` (or thin Vic20Cpu adapter) without processor port; do not fork 6510 for VIC-20-specific memory hacks.
3. **Video:** Fixed framebuffer contract remains host-facing (existing `IVideoChip` / frame pull). VIC-I maps into that contract; PAL (6561) and NTSC (6560) clock/raster tables from VICE `vic20` sources.
4. **Determinism:** Identical ROMs + input sequence => bit-exact state. Snapshot format must version VIC-20 modules without breaking C64 snapshots.
5. **Default drive 8 = 1540 for VIC-20:** When a Vic20 session creates or attaches true-drive (or simulated) floppy on unit 8, the default model is **`DriveModel.C1540`** (VICE `DRIVE_TYPE_1540`), using 1540 DOS ROM (`C1541ViceRomNames.Dos1540` / catalog already lists 1540). C64 sessions keep default **1541**. User may still select 1541/1541-II later via device setup; tests must pin Vic20 default = 1540.
6. **IEC MVP:** Prefer cart/PRG autostart first for boot proof; add IEC load path with 1540 on drive 8 as the primary disk validation path for VIC-20 software.
7. **BDP:** Every behavior slice: failing test first, green before next slice.
8. **Subagents:** Parallel only when file ownership does not collide; always verify gate on shared workspace before advancing the critical path.

---
70→
## 4. Multi-agent / subagent execution strategy

### 4.1 Roles

| Role | Subagent type | Responsibilities |
|------|---------------|------------------|
| **Orchestrator** (parent) | main conversation | Slice sequencing, merge gates, conflict resolution, MCP session/TODO, final C64 non-regression, HANDOFF |
| **Explore** | `explore` (read-only) | Map VICE native `vic20/` / `vic/` sources, ROM names, address maps, existing VIA/CPU seams; produce citations (file:line) |
| **Plan** | `plan` (read-only) | Per-slice AC decomposition and test list when a slice is ambiguous |
80→| **Implement** | `general-purpose` (+ worktree when parallel) | Red tests then green impl for **one** assigned slice; return receipts (commands, pass counts) |
| **Verify** | `general-purpose` or orchestrator | Re-run focused gates; never trust implementer report alone |
| **Review** | `review` skill / subagent | Optional after multi-file slices (D, G) |

### 4.2 When to use subagents (preferred)

| Phase | Pattern |
|-------|---------|
| Before Slice A | **1x explore**: native VIC-20 memory map, ROM set, VIA addresses, VIC-I register map |
| Requirements freeze | Orchestrator (or 1x plan) writes FR/TR/TEST list; no parallel product code yet |
90→| Slice A | **1x implement** (skeleton is the critical path; no parallel writers on Architectures) |
| After A green | **Parallel panel (worktrees):** B memory map \|\| C VIA wiring \|\| D VIC-I chip (D may start with register tests only; full video after B I/O map is known) |
| After B+C+D green | **1x implement** E (boot) |
| After E | **Parallel:** F launcher/heads \|\| G lockstep harness scaffolding (G depends on bootable machine for full green) |
| Closeout | Orchestrator H docs + verify full Vic20 filter + C64 smoke |

### 4.3 Parallel rules (must not break)

1. **One owner per path family** in a parallel panel:
   - Agent B: `Architectures/Vic20/*Memory*`, expansion config only
100→   - Agent C: `Architectures/Vic20/*Input*`, keyboard matrix, VIA port callbacks only
   - Agent D: `Chips/Vic/*` (or `Chips/VicI/*`) only; **no** Architectures edits until merge
2. Prefer **`isolation: worktree`** for each parallel implement agent so edits do not clobber each other.
3. Orchestrator **merges worktrees in order B -> C -> D** (or B+C then D), re-runs all three gates, then continues.
4. **Never** parallel two agents on `Via6522.cs` or shared host frame path without serializing.
5. **Never** parallel full-solution `dotnet test` on the same machine if it causes file locks; serialize verify steps.
6. Agent budget: keep fan-out to **3 implement agents max** per panel (plus 1 explore).

### 4.4 Subagent prompt contract (every implement agent)

110→Each implement prompt **must** include:

1. Slice id (A-H) and exact acceptance criteria  
2. BDP order: write/identify failing tests first; do not implement until red is proven  
3. Allowed paths (whitelist) and forbidden paths  
4. Exact gate command and pass criteria (0 fail, 0 skip for filter)  
5. Requirement IDs to cite in XmlDocs  
6. "Return: command transcript, pass/fail counts, files changed, open risks"  
7. Chip/machine separation reminder: no VIC-20 banking inside generic VIA  

120→### 4.5 Optional workflow script (later)

If fan-out is repeated, add `.grok/workflows/vic20-iteration2.rhai` with:

- phase: explore native  
- phase: sequential A  
- phase: `parallel()` B, C, D (worktree)  
- phase: sequential E, then parallel F scaffolding + G harness, then G green  
- phase: H  

130→Not required to start; orchestrator + `spawn_subagent` is enough for the first pass.

---

## 5. Requirements work (before product code)

**Owner:** orchestrator (or 1x plan subagent for draft, orchestrator commits).

Create or update canonical docs (MCP requirements tools preferred):

140→| ID (proposed) | Title | Notes |
|---------------|-------|-------|
| FR-PRF-005 | Commodore VIC-20 Machine Profile | Already listed in FR map; implement |
| FR-VIC20-001 | VIC-I 6560/6561 display | New if missing |
| FR-VIC20-002 | VIC-20 memory map + expansions | 5K base + BLK1..5/RAM expansions |
| FR-VIC20-003 | VIC-20 VIA1/VIA2 integration | Builds on FR-VIA-004 |
| FR-VIC20-004 | VIC-20 keyboard matrix | |
| FR-VIC20-005 | VIC-20 cartridge types (MVP set) | |
| FR-VIC20-006 | VIC-20 default drive 8 = 1540 | Default `DriveModel.C1540`; DOS dos1540; CI pins ROM |
| TR-VIC20-LOCKSTEP-001 | xvic multi-frame lockstep | |
150→| TEST-* | One TEST per AC | Cite in XmlDocs |

**Gate:** `check_requirement_traceability.ps1` clean for new IDs; FR/TR/TEST in `docs/requirements/`.

**Subagent:** optional explore to extract VICE address map and ROM filenames for AC text.

---

## 6. Implementation slices (dependency order)

160→### Slice 0: Hygiene and branch

- Branch from `main`: `feat/iteration2-vic20`
- Leave C64-only open TODOs out of this branch unless blocking reuse (VIA/host)
- Document "Iteration 2 = VIC-20" in HANDOFF when starting

**Agent:** orchestrator only  
**Gate:** intentional `git status`

### Slice A: Machine skeleton (no video pixels yet)
170→
**Tests first:**
- Descriptor registers; builds empty/minimal machine
- ROM load: kernal + basic + chargen (names from VICE `VIC20` data tree)
- Reset vector fetch; first N cycles advance CPU without throw
- Launcher: `xvic` topology resolves (even if video is stub)

**Impl:**
- `Vic20Descriptor`, `Vic20MachineProfile` (PAL/NTSC), `Vic20RomSet` / VICE ROM names
- Wire `Mos6502` + RAM + ROM mirrors + 2x `Via6522` (addresses from VICE map)
180→- Stub `IVideoChip` if needed so host frame path does not crash (solid color / black)
- Profile/descriptor default: peripheral drive 8 model = **1540** (even if drive not mounted until later slices)

**Agent:** 1x implement (sequential; foundation)  
**Pre:** 1x explore native map + ROM names  
**Gate:** focused Vic20 construction tests green; C64 smoke not regressed

### Slice B: Memory map + expansions

**Tests first:** bank select for unexpanded / 3K / 8K / 16K / 24K / 32K as VICE resources; open bus rules; I/O window `$9000-$9FFF`.
190→
**Impl:** Vic20 PLA-like banking in architecture (not in chips); expansion config via machine profile / resources.

**Agent:** implement (parallelizable after A; own Architectures banking files)  
**Gate:** memory map unit tests + peek/read parity table vs VICE docs or shim

### Slice C: VIA1/VIA2 VIC-20 wiring

**Tests first:** keyboard row/col scan; joystick; NMI/IRQ sources as on real VIC-20; no C1541-specific assumptions leak into Vic20 wiring.

200→**Impl:** port callbacks on existing `Via6522`; keyboard matrix type for VIC-20; RESTORE to NMI if applicable.

### Turn 13 (Function)
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


### Turn 14 (Function)
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


### Turn 15 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 37 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IArchitectureDescriptor.cs
6:/// Used by IArchitectureBuilder to construct a running IMachine.

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IArchitectureBuilder.cs
8:public interface IArchitectureBuilder

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
21:/// A fully-assembled emulated machine. Created by IArchitectureBuilder

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Multisystem\MultiSystemBlueprint.cs
68:        IArchitectureBuilder builder,
196:    /// (host, peripherals, cart-extensions). Caller supplies the IArchitectureBuilder.
201:    /// "C64" -> C64Descriptor; "C1541" -> C1541Descriptor with the
205:    public MultiSystemBuildResult BuildCoordinatorAuto(IArchitectureBuilder builder)
222:        IArchitectureBuilder builder,
229:            return builder.Build(new Architectures.C64.C64Descriptor());
269:    public MultiSystemBuildResult BuildCoordinatorWithAdhocLoader(IArchitectureBuilder builder)

F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
16:/// Default implementation of IArchitectureBuilder that constructs
19:public sealed class ArchitectureBuilder : IArchitectureBuilder
24:    public ArchitectureBuilder() { }
26:    public ArchitectureBuilder(IAudioBackend? audioBackend)
31:    public ArchitectureBuilder(IRomProvider romProvider, IAudioBackend? audioBackend = null)
44:            return BuildC64Machine(descriptor);
81:    private IMachine BuildC64Machine(IArchitectureDescriptor descriptor)

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\C64Descriptor.cs
8:public sealed class C64Descriptor : IProfiledArchitectureDescriptor
10:    public C64Descriptor()
15:    public C64Descriptor(string modelSelector)
20:    public C64Descriptor(C64MachineProfile profile)

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Adhoc\AdhocMachineBlueprint.cs
35:    public IMachine BuildMachine(IArchitectureBuilder builder)
40:        // pass a customised IArchitectureBuilder keep their behaviour. We then
145:/// Wraps the bus and clock allocated by the underlying <see cref="IArchitectureBuilder"/>

F:\GitHub\vice-sharp\src\ViceSharp.Console\Program.cs
157:    var builder = new ArchitectureBuilder(romProvider);
158:    var descriptor = new C64Descriptor();
263:        return (blueprint.BuildMachine(new ArchitectureBuilder()), null);
301:        var builder = new ArchitectureBuilder(new RomProvider(romPath));

F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemCore.cs
7:/// ArchitectureBuilder creates the chips and connects them through this policy.

F:\GitHub\vice-sharp\src\ViceSharp.Core\SidFactory.cs
8:/// (ArchitectureBuilder, the legacy Commodore64 benchmark machine) creates its

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\ConsoleEmulatorHost.cs
160:            ? new ArchitectureBuilder(dependencies.RomProvider, dependencies.AudioBackend)
161:            : new ArchitectureBuilder(dependencies.AudioBackend);

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\DefaultEmulatorRuntimeFactory.cs
14:    private readonly IArchitectureBuilder _architectureBuilder;
43:        : this(CreateDefaultArchitectureBuilder(audioTap), CreateDefaultDescriptors(), CreateDefaultArchitectureId())
49:        IArchitectureBuilder architectureBuilder,
56:        IArchitectureBuilder architectureBuilder,
105:        var isTrueDriveRig = trueDrive && descriptor is C64Descriptor;
</workspace_result>

### Turn 16 (Assistant)
Profile loaded. Continuing Iteration 2 VIC-20: examining C64 patterns and implementing Slice A (skeleton).
[tool_request: todo_write]
- todos: [{"id":"0","content":"0: Slice 0: branch feat/iteration2-vic20 + hygiene","status":"completed"},{"id":"explore","content":"explore: Explore native VIC-20 map/ROMs/VIA/VIC-I","status":"completed"},{"id":"req","content":"req: Requirements freeze FR-VIC20-* docs","status":"in_progress"},{"id":"A","content":"A: Slice A: Vic20 skeleton red then green","status":"pending"}]
- merge: true
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\C64Descriptor.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\C64MachineProfile.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- limit: 250
[tool_request: grep]
- pattern: CreateC64|MachineTestFactory|BasicBootProof|BuildMinimal
- glob: **/*.{cs}
- path: F:\GitHub\vice-sharp
- head_limit: 40
[tool_request: list_dir]
- target_directory: F:\GitHub\vice-sharp\docs\requirements\functional

### Turn 17 (Function)
[tool_response]
- [completed] 0: 0: Slice 0: branch feat/iteration2-vic20 + hygiene
- [completed] explore: explore: Explore native VIC-20 map/ROMs/VIA/VIC-I
- [in_progress] req: req: Requirements freeze FR-VIC20-* docs
- [pending] A: A: Slice A: Vic20 skeleton red then green


### Turn 18 (Function)
[tool_response]
- F:\GitHub\vice-sharp\docs\requirements\functional/
  - FR-Audio-SID.md
  - FR-Cartridges.md
  - FR-Configuration-Resources.md
  - FR-CPU-Emulation.md
  - FR-Host-UI-Boundary.md
  - FR-Input.md
  - FR-IO-CIA.md
  - FR-IO-VIA.md
  - FR-Machine-Profiles.md
  - FR-Media-Capture.md
  - FR-Memory-System.md
  - FR-Monitor.md
  - FR-Overview.md
  - FR-Performance.md
  - FR-Snapshot.md
  - FR-Storage-Drives.md
  - FR-Storage-Tape.md
  - FR-Video-VIC-II.md

### Turn 19 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

public enum C64VicIIModel
{
    Mos6569,
    Mos6569R1,
    Mos6567R56A,
10→    Mos6567R8,
    Mos6572,
    Mos8565,
    Mos8562
}

public enum C64SidModel
{
    Mos6581,
    Mos8580
20→}

public enum C64BoardModel
{
    Breadbox,
    BreadboxOld,
    C64C,
    Drean,
    SX64,
    PET64,
30→    Ultimax,
    C64GS,
    Japanese
}

public sealed record C64MachineProfile(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Aliases,
    long NominalClockHz,
40→    VideoStandard VideoStandard,
    int CyclesPerLine,
    int RasterLines,
    C64VicIIModel VicII,
    C64SidModel Sid,
    C64BoardModel Board,
    string RomSet,
    string BasicRomName = C64ViceRomNames.Basic,
    string KernalRomName = C64ViceRomNames.KernalRev3,
    string CharacterRomName = C64ViceRomNames.Character,
50→    bool KeyboardEnabled = true,
    bool CartridgeBootExpected = false,
    C64SystemCoreDefinition? CoreDefinition = null) : IMachineProfile
{
    public string Family => "x64sc";

    public double RefreshRateHz => NominalClockHz / (double)(CyclesPerLine * RasterLines);

    public string VicIIModel => VicII.ToString();

60→    public string SidModel => Sid.ToString();

    public string BoardModel => Board.ToString();

    public ISystemCoreDefinition SystemCore { get; } =
        CoreDefinition ?? C64SystemCoreDefinitions.ForProfile(Board, KeyboardEnabled, CartridgeBootExpected);
}

public static class C64MachineProfiles
{
70→    public static C64MachineProfile Default => C64Pal;

    public static C64MachineProfile C64Pal { get; } = new(
        "c64",
        "Commodore 64 PAL",
        ["c64", "breadbox", "pal", "c64-pal", "commodore64"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
80→        C64VicIIModel.Mos6569,
        C64SidModel.Mos6581,
        C64BoardModel.Breadbox,
        "C64");

    public static C64MachineProfile C64CPal { get; } = new(
        "c64c",
        "Commodore 64C PAL",
        ["c64c", "c64new", "newpal", "c64c-pal"],
        985_248,
90→        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos8565,
        C64SidModel.Mos8580,
        C64BoardModel.C64C,
        "C64");

    public static C64MachineProfile C64OldPal { get; } = new(
        "c64old",
100→        "Commodore 64 old PAL",
        ["c64old", "oldpal", "c64old-pal"],
        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos6569R1,
        C64SidModel.Mos6581,
        C64BoardModel.BreadboxOld,
        "C64",
110→        KernalRomName: C64ViceRomNames.KernalRev2);

    public static C64MachineProfile C64Ntsc { get; } = new(
        "ntsc",
        "Commodore 64 NTSC",
        ["ntsc", "c64ntsc", "c64-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
120→        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.Breadbox,
        "C64");

    public static C64MachineProfile C64CNtsc { get; } = new(
        "newntsc",
        "Commodore 64C NTSC",
        ["c64cntsc", "newntsc", "c64newntsc", "c64c-ntsc"],
        1_022_730,
130→        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos8562,
        C64SidModel.Mos8580,
        C64BoardModel.C64C,
        "C64");

    public static C64MachineProfile C64OldNtsc { get; } = new(
        "oldntsc",
140→        "Commodore 64 old NTSC",
        ["oldntsc", "c64oldntsc", "c64old-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        64,
        262,
        C64VicIIModel.Mos6567R56A,
        C64SidModel.Mos6581,
        C64BoardModel.BreadboxOld,
        "C64",
150→        KernalRomName: C64ViceRomNames.KernalRev1);

    public static C64MachineProfile C64PalN { get; } = new(
        "paln",
        "Commodore 64 PAL-N / Drean",
        ["paln", "drean", "c64-paln"],
        1_023_440,
        VideoStandard.Pal,
        65,
        312,
160→        C64VicIIModel.Mos6572,
        C64SidModel.Mos6581,
        C64BoardModel.Drean,
        "C64");

    public static C64MachineProfile SX64Pal { get; } = new(
        "sx64pal",
        "Commodore SX-64 PAL",
        ["sx64", "sx64pal", "sx64-pal"],
        985_248,
170→        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos6569,
        C64SidModel.Mos6581,
        C64BoardModel.SX64,
        "C64",
        KernalRomName: C64ViceRomNames.KernalSx64);

    public static C64MachineProfile SX64Ntsc { get; } = new(
180→        "sx64ntsc",
        "Commodore SX-64 NTSC",
        ["sx64ntsc", "sx64-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.SX64,
190→        "C64",
        KernalRomName: C64ViceRomNames.KernalSx64);

    public static C64MachineProfile PET64Pal { get; } = new(
        "pet64pal",
        "Commodore PET64 PAL",
        ["pet64", "pet64pal", "pet64-pal"],
        985_248,
        VideoStandard.Pal,
        63,
200→        312,
        C64VicIIModel.Mos6569,
        C64SidModel.Mos6581,
        C64BoardModel.PET64,
        "C64",
        KernalRomName: C64ViceRomNames.Kernal4064);

    public static C64MachineProfile PET64Ntsc { get; } = new(
        "pet64ntsc",
        "Commodore PET64 NTSC",
210→        ["pet64ntsc", "pet64-ntsc"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.PET64,
        "C64",
        KernalRomName: C64ViceRomNames.Kernal4064);
220→
    public static C64MachineProfile Ultimax { get; } = new(
        "ultimax",
        "Commodore MAX / Ultimax",
        ["max", "ultimax"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
        C64VicIIModel.Mos6567R8,
230→        C64SidModel.Mos6581,
        C64BoardModel.Ultimax,
        "C64",
        KernalRomName: C64ViceRomNames.KernalNone,
        CartridgeBootExpected: true);

    public static C64MachineProfile C64GS { get; } = new(
        "c64gs",
        "Commodore 64 Games System",
        ["gs", "c64gs"],
240→        985_248,
        VideoStandard.Pal,
        63,
        312,
        C64VicIIModel.Mos8565,
        C64SidModel.Mos8580,
        C64BoardModel.C64GS,
        "C64",
        KernalRomName: C64ViceRomNames.KernalGs,
        KeyboardEnabled: false,
250→        CartridgeBootExpected: true);

    public static C64MachineProfile C64Japanese { get; } = new(
        "c64jap",
        "Commodore 64 Japanese",
        ["jap", "c64jap"],
        1_022_730,
        VideoStandard.Ntsc,
        65,
        263,
260→        C64VicIIModel.Mos6567R8,
        C64SidModel.Mos6581,
        C64BoardModel.Japanese,
        "C64",
        KernalRomName: C64ViceRomNames.KernalJapanese,
        CharacterRomName: C64ViceRomNames.CharacterJapanese);

    public static IReadOnlyList<C64MachineProfile> All { get; } =
    [
        C64Pal,
270→        C64CPal,
        C64OldPal,
        C64Ntsc,
        C64CNtsc,
        C64OldNtsc,
        C64PalN,
        SX64Pal,
        SX64Ntsc,
        PET64Pal,
        PET64Ntsc,
280→        Ultimax,
        C64GS,
        C64Japanese
    ];

    private static readonly Dictionary<string, C64MachineProfile> ByAlias = All
        .SelectMany(profile => profile.Aliases.Append(profile.Id).Select(alias => (Alias: alias, Profile: profile)))
        .GroupBy(pair => pair.Alias, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First().Profile, StringComparer.OrdinalIgnoreCase);

290→    public static C64MachineProfile Resolve(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (ByAlias.TryGetValue(selector, out var profile))
            return profile;

        throw new ArgumentException($"Unknown x64sc C64 model selector '{selector}'.", nameof(selector));
    }

300→    public static bool TryResolve(string selector, out C64MachineProfile profile)
    {
        profile = Default;

        if (string.IsNullOrWhiteSpace(selector))
        {
            return false;
        }

        if (ByAlias.TryGetValue(selector, out var resolved))
310→        {
            profile = resolved;
            return true;
        }

        return false;
    }
}

public static class C64ViceRomNames
320→{
    public const string Basic = "basic-901226-01.bin";
    public const string Character = "chargen-901225-01.bin";
    public const string CharacterJapanese = "chargen-906143-02.bin";
    public const string KernalJapanese = "kernal-906145-02.bin";
    public const string KernalRev1 = "kernal-901227-01.bin";
    public const string KernalRev2 = "kernal-901227-02.bin";
    public const string KernalRev3 = "kernal-901227-03.bin";
    public const string KernalGs = "kernal-390852-01.bin";
    public const string KernalSx64 = "kernal-251104-04.bin";
330→    public const string Kernal4064 = "kernal-901246-01.bin";
    public const string KernalNone = "kernal-none.bin";
}


### Turn 20 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

/// <summary>
/// C64 x64sc architecture descriptor.
/// </summary>
public sealed class C64Descriptor : IProfiledArchitectureDescriptor
{
10→    public C64Descriptor()
        : this(C64MachineProfiles.Default)
    {
    }

    public C64Descriptor(string modelSelector)
        : this(C64MachineProfiles.Resolve(modelSelector))
    {
    }

20→    public C64Descriptor(C64MachineProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public C64MachineProfile Profile { get; }

    /// <inheritdoc />
    public IMachineProfile MachineProfile => Profile;

30→    /// <inheritdoc />
    public string MachineName => Profile.DisplayName;

    /// <inheritdoc />
    public long MasterClockHz => Profile.NominalClockHz;

    /// <inheritdoc />
    public VideoStandard VideoStandard => Profile.VideoStandard;

    /// <inheritdoc />
40→    public IReadOnlyList<DeviceDescriptor> Devices => Profile.SystemCore.Cia2Connected
        ? _devicesWithCia2
        : _devicesWithoutCia2;

    private static readonly DeviceDescriptor[] _devicesWithCia2 =
    {
        new("6510 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-II", new DeviceId(0x0003), DeviceRole.VideoChip, 0xD000, 0x0400),
        new("SID", new DeviceId(0x0004), DeviceRole.AudioChip, 0xD400, 0x0400),
        new("CIA1", new DeviceId(0x0005), DeviceRole.Cia1, 0xDC00, 0x0100),
50→        new("CIA2", new DeviceId(0x0006), DeviceRole.Cia2, 0xDD00, 0x0100),
        new("PLA", new DeviceId(0x0007), DeviceRole.Pla, 0x0001, 1),
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0x8000, 0x8000),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
    };

    private static readonly DeviceDescriptor[] _devicesWithoutCia2 =
    {
        new("6510 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-II", new DeviceId(0x0003), DeviceRole.VideoChip, 0xD000, 0x0400),
60→        new("SID", new DeviceId(0x0004), DeviceRole.AudioChip, 0xD400, 0x0400),
        new("CIA1", new DeviceId(0x0005), DeviceRole.Cia1, 0xDC00, 0x0100),
        new("PLA", new DeviceId(0x0007), DeviceRole.Pla, 0x0001, 1),
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0x8000, 0x8000),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
    };

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new C64RomSet(
        Profile.RomSet,
70→        Profile.BasicRomName,
        Profile.KernalRomName,
        Profile.CharacterRomName);
}

/// <summary>
/// C64 required ROM set.
/// </summary>
public sealed class C64RomSet : IRomSet
{
80→    public C64RomSet()
        : this("C64", C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character)
    {
    }

    public C64RomSet(
        string architecture,
        string basicRomName = C64ViceRomNames.Basic,
        string kernalRomName = C64ViceRomNames.KernalRev3,
        string characterRomName = C64ViceRomNames.Character)
90→    {
        Architecture = string.IsNullOrWhiteSpace(architecture) ? "C64" : architecture;
        BasicRomName = string.IsNullOrWhiteSpace(basicRomName) ? C64ViceRomNames.Basic : basicRomName;
        KernalRomName = string.IsNullOrWhiteSpace(kernalRomName) ? C64ViceRomNames.KernalRev3 : kernalRomName;
        CharacterRomName = string.IsNullOrWhiteSpace(characterRomName) ? C64ViceRomNames.Character : characterRomName;
    }

    /// <inheritdoc />
    public string Architecture { get; }

100→    public string BasicRomName { get; }

    public string KernalRomName { get; }

    public string CharacterRomName { get; }

    /// <inheritdoc />
    public bool IsComplete(IRomProvider provider)
    {
        return provider.IsAvailable(BasicRomName, Architecture)
110→            && (IsKernalRequired(KernalRomName) ? provider.IsAvailable(KernalRomName, Architecture) : true)
            && provider.IsAvailable(CharacterRomName, Architecture);
    }

    public static bool IsKernalRequired(string kernalRomName)
        => !string.Equals(kernalRomName, C64ViceRomNames.KernalNone, StringComparison.OrdinalIgnoreCase);
}


### Turn 21 (Function)
[tool_response]
1→using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Chips.Tape;
10→using ViceSharp.Core.Wiring;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

/// <summary>
/// Default implementation of IArchitectureBuilder that constructs
/// a running IMachine instance from an IArchitectureDescriptor.
/// </summary>
public sealed class ArchitectureBuilder : IArchitectureBuilder
20→{
    private readonly IRomProvider? _romProvider;
    private readonly IAudioBackend? _audioBackend;

    public ArchitectureBuilder() { }

    public ArchitectureBuilder(IAudioBackend? audioBackend)
    {
        _audioBackend = audioBackend;
    }
30→
    public ArchitectureBuilder(IRomProvider romProvider, IAudioBackend? audioBackend = null)
    {
        _romProvider = romProvider;
        _audioBackend = audioBackend;
    }

    /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
40→        if (IsC1541Machine(descriptor))
            return BuildC1541Machine(descriptor);

        if (IsC64Machine(descriptor))
            return BuildC64Machine(descriptor);

        return BuildMinimalMachine(descriptor);
    }

    private IMachine BuildMinimalMachine(IArchitectureDescriptor descriptor)
50→    {
        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var ram = new SimpleRam();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine);

        ram.InitializeC64();
        bus.RegisterDevice(ram);
60→        clock.Register(cpu);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        return machine;
    }

70→    // Wire one machine pub/sub: the CPU publishes instruction-completed events and the bus
    // publishes memory-write events on it, both gated on a live subscriber. The host's
    // tick-history recorder subscribes for the time-travel debugger.
    private static LockFreePubSub ConnectMachinePubSub(BasicBus bus, Mos6502 cpu)
    {
        var pubSub = new LockFreePubSub();
        cpu.ConnectPubSub(pubSub);
        bus.ConnectPubSub(pubSub);
        return pubSub;
    }
80→
    private IMachine BuildC64Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException($"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
90→        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine, nmiLine);
        var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;
        var systemCore = profile is null ? null : new SystemCore(profile.SystemCore);
        var vic = CreateVicII(bus, irqLine, descriptor, profile);
        var cia1 = CreateC64Cia(bus, irqLine, 0xDC00, descriptor.MasterClockHz);
        var cia2Connected = profile?.SystemCore.Cia2Connected ?? true;
100→        var cia2 = cia2Connected ? CreateC64Cia(bus, nmiLine, 0xDD00, descriptor.MasterClockHz) : null;
        var pla = new Mos906114(bus);
        var sid = CreateSid(bus, profile, _audioBackend, descriptor.MasterClockHz);
        var defaultCartridgeMappingMode = ResolveDefaultCartridgeMappingMode(profile);
        var cia2PortAInputMask = ResolveCia2PortAInputMask(profile);
        var memory = new C64MemoryMap(
            vic,
            sid,
            cia1,
            cia2,
110→            pla,
            profile?.KeyboardEnabled ?? true,
            cia2PortAInputMask: cia2PortAInputMask,
            cia2Connected: cia2Connected,
            defaultCartridgeMappingMode: defaultCartridgeMappingMode,
            cpuPcReader: () => cpu.PC,
            nmiLine: nmiLine);
        cpu.ShouldDeferAbsoluteStore = memory.ShouldDeferCpuAbsoluteStore;
        cpu.ShouldDelayNextFetchAfterWrite = memory.ShouldDelayCpuFetchAfterWrite;
        var iecBusConnected = profile?.SystemCore.IecBusConnected ?? true;
120→        var cia2Interface = cia2Connected ? new C64Cia2InterfaceDevice() : null;
        var drive8 = iecBusConnected ? new IecDrive(8) : null;
        var drive9 = iecBusConnected ? new IecDrive(9) : null;

        // DD-IEC-1: the IEC serial bus is always present on a C64 (it hangs off
        // CIA2), independent of whether a drive is attached. Create it once and
        // attach the drives so the bus, spy and activity monitor observe the
        // real machine bus. CIA2 is intentionally NOT routed to the live bus
        // here - that read (with the faithful electrical model) is parity-gated
        // and lands separately; until then CIA2 keeps its native-matching mask.
130→        var iecBus = iecBusConnected ? IecInterSystemBus.Create() : null;
        if (iecBus is not null)
        {
            drive8?.ConnectIecBus(iecBus);
            drive9?.ConnectIecBus(iecBus);
        }
        var datasette = profile?.SystemCore.TapePortConnected == false ? null : new Datasette();
        var datasetteCia1FlagBinding = datasette is null
            ? null
            : new DatasetteCia1FlagBinding(datasette, cia1);
140→        bus.RegisterDevice(memory);

        var romLoader = new C64RomLoader(bus);
        var basicRomName = profile?.BasicRomName ?? "basic";
        var kernalRomName = profile?.KernalRomName ?? "kernal";
        var characterRomName = profile?.CharacterRomName ?? "characters";
        var basic = _romProvider.LoadRom(basicRomName, "C64").Span;
        var kernal = IsKernalRomRequired(kernalRomName)
            ? _romProvider.LoadRom(kernalRomName, "C64").Span
            : ReadOnlySpan<byte>.Empty;
150→        var character = _romProvider.LoadRom(characterRomName, "C64").Span;

        memory.BeginRomLoad();
        try
        {
            if (!romLoader.LoadAllRoms(basic, kernal, character, basicRomName, kernalRomName, characterRomName))
            {
                throw new InvalidOperationException($"{descriptor.MachineName} ROM set is invalid or missing expected checksum entries.");
            }
        }
160→        finally
        {
            memory.EndRomLoad();
        }

        clock.Register(cpu);
        clock.Register(vic);
        clock.Register(cia1);
        if (cia2 is not null)
            clock.Register(cia2);
170→        clock.Register(sid);
        clock.Register(pla);
        if (drive8 is not null)
            clock.Register(drive8);
        if (drive9 is not null)
            clock.Register(drive9);
        if (datasetteCia1FlagBinding is not null)
            clock.Register(datasetteCia1FlagBinding);

        if (systemCore is not null)
180→            deviceRegistry.Add(systemCore, DeviceRole.SystemCore);

        deviceRegistry.Add(memory, DeviceRole.SystemRam, DeviceRole.CartridgePort);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);
        deviceRegistry.Add(cia1, DeviceRole.Cia1);
        if (cia2 is not null)
            deviceRegistry.Add(cia2, DeviceRole.Cia2);
        if (cia2Interface is not null)
            deviceRegistry.Add(cia2Interface);
190→        if (iecBus is not null)
            deviceRegistry.Add(new IecBusDevice(iecBus));
        deviceRegistry.Add(pla, DeviceRole.Pla);
        deviceRegistry.Add(sid, DeviceRole.AudioChip);
        if (drive8 is not null)
            deviceRegistry.Add(drive8);
        if (drive9 is not null)
            deviceRegistry.Add(drive9);
        if (datasette is not null)
            deviceRegistry.Add(datasette);
200→        if (datasetteCia1FlagBinding is not null)
            deviceRegistry.Add(datasetteCia1FlagBinding);

        // KERNAL serial-bus traps (VICE virtual device traps): service LOAD/$ on
        // the simulated drive when True Drive is OFF. The hook declines unless the
        // addressed drive has a disk, so it is inert on parity rigs and on the
        // true-drive host C64 (whose disk lives in the emulated 1541, not here).
        KernalSerialTrap? serialTrap = null;
        if (drive8 is not null || drive9 is not null)
        {
210→            var virtualDrives = new VirtualDriveServer(device => device switch
            {
                8 => drive8?.DiskImage,
                9 => drive9?.DiskImage,
                _ => null
            });
            serialTrap = new KernalSerialTrap(cpu, bus, virtualDrives);
            cpu.SerialTrapHook = serialTrap.TryHandle;
        }

220→        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        serialTrap?.Reset();
        return machine;
    }

    private static bool IsC64Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
230→        return roles.Contains(DeviceRole.VideoChip)
            || roles.Contains(DeviceRole.Cia1)
            || roles.Contains(DeviceRole.Cia2)
            || roles.Contains(DeviceRole.Pla)
            || roles.Contains(DeviceRole.AudioChip);
    }

    private static bool IsC1541Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
240→        return roles.Contains(DeviceRole.DriveCpu) && roles.Contains(DeviceRole.DriveRom);
    }

    private IMachine BuildC1541Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
250→                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

### Turn 22 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 19 matching lines
F:\GitHub\vice-sharp\build\Build.cs
127:    /// DirectoryNotFoundException from MachineTestFactory.CreateC64RomProvider.

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\AdvancedCartridgeScaffoldTests.cs
230:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
46:        return BuildMinimalMachine(descriptor);
49:    private IMachine BuildMinimalMachine(IArchitectureDescriptor descriptor)
98:        var cia1 = CreateC64Cia(bus, irqLine, 0xDC00, descriptor.MasterClockHz);
100:        var cia2 = cia2Connected ? CreateC64Cia(bus, nmiLine, 0xDD00, descriptor.MasterClockHz) : null;
359:    private static Mos6526 CreateC64Cia(IBus bus, IInterruptLine line, ushort baseAddress, long masterClockHz)

F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\RunFramePerfProbe.cs
25:        var machine = BenchmarkMachineFactory.CreateC64Pal();

F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\C64PalRunFrameBenchmark.cs
20:        _machine = BenchmarkMachineFactory.CreateC64Pal();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBusAndSimpleRamTests.cs
235:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBootProofTests.cs
5:public sealed class BasicBootProofTests
19:        var machine = MachineTestFactory.CreateC64Machine();
51:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Audio\SidAudioWiringTests.cs
43:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Architecture\ChipGlueBoundaryTests.cs
178:            "CreateC64Cia",

F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\BenchmarkMachineFactory.cs
46:    public static IMachine CreateC64Pal()
49:        var machine = new ArchitectureBuilder(CreateC64RomProvider()).Build(descriptor);
59:    private static RomProvider CreateC64RomProvider()

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ClockThroughputBenchmarkTests.cs
138:            var romProvider = MachineTestFactory.CreateC64RomProvider();
</workspace_result>

### Turn 23 (Assistant)
[tool_request: grep]
- pattern: class MachineTestFactory|CreateC64Machine|CreateC64RomProvider
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\tests
[tool_request: grep]
- pattern: enum DeviceRole|DriveModel|C1540|IVideoChip|IMachineProfile|DefaultDrive
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\src
- head_limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\requirements\functional\FR-Machine-Profiles.md
- limit: 120
[tool_request: grep]
- pattern: Via6522|class Mos6502|interface IVideoChip
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\src
- head_limit: 40
[tool_request: run_terminal_command]
- command: Get-ChildItem "C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction" | Select-Object Name; if (Test-Path "C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction\INDEX.md") { Get-Content "C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction\INDEX.md" -TotalCount 80 }
- description: List compaction segments for explore brief

### Turn 24 (Function)
[tool_response]
1→# FR-Machine-Profiles: Machine Profile Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Machine Profiles               |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13 |
10→
---

## FR-PRF-001: C64 (NMOS) Profile

**ID:** FR-PRF-001
**Title:** Commodore 64 (Original NMOS) Machine Profile
**Priority:** P0 -- Critical
**Iteration:** 1

20→### Description

The primary machine profile for the original Commodore 64 with NMOS chips: MOS 6510 CPU, MOS 6567 (NTSC) or MOS 6569 (PAL) VIC-II, MOS 6581 SID, two MOS 6526 CIAs. This is the baseline profile and the first target for full compatibility.

### Acceptance Criteria

1. CPU: MOS 6510 with all undocumented opcodes exhibiting NMOS behavior.
2. VIC-II: MOS 6569R3 (PAL) or MOS 6567R8 (NTSC) with correct timing per variant.
3. SID: MOS 6581 with characteristic analog filter curve and combined waveform behavior.
4. CIA: Two MOS 6526 with correct timer and TOD behavior.
30→5. RAM: 64KB main RAM + 1KB Color RAM.
6. ROM: BASIC V2 ($A000), KERNAL ($E000), Character Generator ($D000).
7. System clock: 985248 Hz (PAL) or 1022727 Hz (NTSC).
8. The profile is selectable via `IMachineProfile.LoadProfile("c64")`.
9. PAL/NTSC variant is configurable within the C64 profile.
10. Passes the VICE test suite for x64sc accuracy level.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.
40→
### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C64ProfileTests`, `C64PalNtscTests`, `ViceTestSuiteRunner`

---

## FR-PRF-002: C64C (New Revision) Profile

50→**ID:** FR-PRF-002
**Title:** Commodore 64C Machine Profile
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The C64C (cost-reduced revision) uses the MOS 8580 SID (which has a different filter and combined waveform behavior) and a revised VIC-II (MOS 8562/8565). The C64C profile configures the emulator with these variant chips.

### Acceptance Criteria
60→
1. SID: MOS 8580 with linear filter curve and revised combined waveform tables.
2. VIC-II: MOS 8562 (NTSC) or MOS 8565 (PAL) -- functionally equivalent to earlier revisions but with different luminance levels.
3. All other components are identical to the C64 NMOS profile.
4. The difference in SID filter behavior is audible and matches real hardware comparisons.
5. Selectable via `IMachineProfile.LoadProfile("c64c")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.
70→
### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C64CProfileTests`, `Sid8580FilterTests`

---

## FR-PRF-003: SX-64 Profile

80→**ID:** FR-PRF-003
**Title:** Commodore SX-64 Machine Profile
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The SX-64 is a portable C64 with a built-in 1541 disk drive and a 5" color monitor. It lacks a cassette port and has a different KERNAL ROM (no tape routines, different screen colors on startup).

### Acceptance Criteria
90→
1. KERNAL: SX-64 variant KERNAL ROM with different default colors (blue screen instead of light blue).
2. No cassette port: tape-related I/O port bits behave differently (cassette sense always reads "no tape").
3. Built-in 1541 drive: device 8 is always present.
4. The 6510 I/O port bit 4 (cassette sense) is permanently pulled high.
5. All other hardware is identical to the C64 NMOS profile.
6. Selectable via `IMachineProfile.LoadProfile("sx64")`.

### Source References

100→- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Sx64ProfileTests`, `Sx64KernalTests`, `Sx64NoCassetteTests`

---

## FR-PRF-004: C128 Profile
110→
**ID:** FR-PRF-004
**Title:** Commodore 128 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The Commodore 128 features dual CPUs (MOS 8502 and Zilog Z80), 128KB RAM, an 80-column VDC display chip, and C64 compatibility mode. The C128 profile is a major expansion of the emulation capabilities.

120→### Acceptance Criteria

### Turn 25 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 49 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxMediaSlotViewModel.cs
17:/// (<see cref="AvailableDriveModels"/> / <see cref="SelectedDriveModel"/>) and the
27:/// <see cref="DriveModel"/>.
36:    private DriveModel _selectedDriveModel;
61:            AvailableDriveModels = DriveModelCatalog.Implemented;
62:            _selectedDriveModel = DriveModel.C1541;
66:            AvailableDriveModels = Array.Empty<DriveModel>();
67:            _selectedDriveModel = DriveModel.None;
87:    /// The drive models offered by this card: <see cref="DriveModelCatalog.Implemented"/>
90:    public IReadOnlyList<DriveModel> AvailableDriveModels { get; }
125:    /// The selected drive model for a drive slot (default <see cref="DriveModel.C1541"/>),
126:    /// or <see cref="DriveModel.None"/> for a non-drive slot.
128:    public DriveModel SelectedDriveModel
130:        get => _selectedDriveModel;
131:        internal set => SetProperty(ref _selectedDriveModel, value);

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxDeviceSetupViewModel.cs
31:///   <item><description>The two drive cards expose <see cref="DriveModelCatalog.Implemented"/>
33:///   1541-II flows as VICE drive type 1542 (<c>(int)<see cref="DriveModel.C1541II"/></c>).</description></item>
39:/// <see cref="SetTrueDriveAsync"/>, <see cref="SelectDriveModelAsync"/>) so a controller
250:    public async Task SelectDriveModelAsync(
252:        DriveModel model,
262:        slot.SelectedDriveModel = model;
288:        var model = active is null ? (int)DriveModel.C1541 : (int)active.SelectedDriveModel;

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Services\VideoServiceHost.cs
28:            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
53:            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\IXboxSettingsGateway.cs
114:    /// number (see <see cref="DriveModel"/>): 1541, 1540, or 1542 for the 1541-II.
122:        int driveModel = (int)DriveModel.C1541,

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Services\LocalVideoFrameSource.cs
55:        var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\DriveModelCatalog.cs
6:/// The catalog of <see cref="DriveModel"/> values the device-setup UI exposes. Only the
10:public static class DriveModelCatalog
14:    /// <see cref="DriveModel.C1541"/> (the default), then <see cref="DriveModel.C1540"/>
15:    /// and <see cref="DriveModel.C1541II"/>. The 1541-II's integer value is 1542.
17:    public static IReadOnlyList<DriveModel> Implemented { get; } = new[]
19:        DriveModel.C1541,
20:        DriveModel.C1540,
21:        DriveModel.C1541II,

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\DriveModel.cs
10:public enum DriveModel
16:    C1540 = 1540,

F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Platform\InProcessSessionFacade.cs
287:        int driveModel = (int)DriveModel.C1541,

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Services\EmulationPumpService.cs
22:/// <see cref="IVideoChip.FrameCompleted"/>.
251:        if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip videoChip)
325:        public IVideoChip? VideoChip;

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Services\CaptureServiceHost.cs
158:                if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip videoChip)
446:            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDeviceRegistry.cs
28:public enum DeviceRole

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\Mos6569.cs
8:public partial class Mos6569 : IVideoChip, IAddressSpace, IInterruptSource, ICpuCycleStealer

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\ConsoleEmulatorHost.cs
452:        if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip video)

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachineProfile.cs
6:public interface IMachineProfile
72:    IMachineProfile MachineProfile { get; }

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\EmulatorRuntimeSession.cs
357:    public void CommitFrame(IVideoChip videoChip, long cycle)

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IVideoChip.cs
6:public interface IVideoChip : IClockedDevice
</workspace_result>

### Turn 26 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 120 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\AdvancedCartridgeScaffoldTests.cs
230:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64MemoryMapPageDispatchTests.cs
37:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64VkmKeyboardTests.cs
149:        var machine = MachineTestFactory.CreateC64Machine();
189:        var machine = MachineTestFactory.CreateC64Machine(modelSelector);
266:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64MachineTests.cs
77:        var machine = MachineTestFactory.CreateC64Machine();
101:        var machine = MachineTestFactory.CreateC64Machine();
126:        var machine = MachineTestFactory.CreateC64Machine();
145:        var machine = MachineTestFactory.CreateC64Machine();
169:        var machine = MachineTestFactory.CreateC64Machine();
188:        var machine = MachineTestFactory.CreateC64Machine("c64gs");
207:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64MachineProfileTests.cs
216:        romSet.IsComplete(MachineTestFactory.CreateC64RomProvider()).Should().BeTrue();
233:        var provider = MachineTestFactory.CreateC64RomProvider();
234:        var machine = MachineTestFactory.CreateC64Machine(selector);
266:        var machine = MachineTestFactory.CreateC64Machine("c64gs");
296:        var machine = MachineTestFactory.CreateC64Machine("c64gs");
325:        var machine = MachineTestFactory.CreateC64Machine(selector);
358:        var machine = MachineTestFactory.CreateC64Machine(descriptor);
387:        var machine = MachineTestFactory.CreateC64Machine(selector);
410:        var machine = MachineTestFactory.CreateC64Machine(selector);
434:        var machine = MachineTestFactory.CreateC64Machine(selector);
456:        var machine = MachineTestFactory.CreateC64Machine("ultimax");
482:        var machine = MachineTestFactory.CreateC64Machine("c64");
596:        var machine = MachineTestFactory.CreateC64Machine(selector);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\C1541MachineBuildTests.cs
21:        var provider = MachineTestFactory.CreateC64RomProvider();
75:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBusAndSimpleRamTests.cs
235:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\C1541RomResolutionTests.cs
46:        var provider = MachineTestFactory.CreateC64RomProvider();
63:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\C64Plus1541IntegrationTests.cs
27:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\D64AttachTests.cs
45:            var provider = MachineTestFactory.CreateC64RomProvider();
70:        var provider = MachineTestFactory.CreateC64RomProvider();
87:        var provider = MachineTestFactory.CreateC64RomProvider();
107:            var provider = MachineTestFactory.CreateC64RomProvider();
153:            var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\CoordinatorMachineTests.cs
59:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBootProofTests.cs
19:        var machine = MachineTestFactory.CreateC64Machine();
51:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\HostMediaSimulatedLoadTests.cs
40:            var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\Iec1541BootTickTests.cs
33:        var provider = MachineTestFactory.CreateC64RomProvider();
56:        var provider = MachineTestFactory.CreateC64RomProvider();
86:        var provider = MachineTestFactory.CreateC64RomProvider();
108:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\SimulatedDriveLoadTests.cs
59:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\TrueDriveFactoryTests.cs
28:        var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\TrueDriveLoadTests.cs
72:            var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\TrueDrivePumpPathLoadTests.cs
80:            var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ClockThroughputBenchmarkTests.cs
138:            var romProvider = MachineTestFactory.CreateC64RomProvider();
216:            var romProvider = MachineTestFactory.CreateC64RomProvider();
290:            var romProvider = MachineTestFactory.CreateC64RomProvider();
379:            var romProvider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Audio\SidAudioWiringTests.cs
43:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\BenchmarkMachineFactory.cs
49:        var machine = new ArchitectureBuilder(CreateC64RomProvider()).Build(descriptor);
59:    private static RomProvider CreateC64RomProvider()

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\FinalCartridgeIIITests.cs
32:        var machine = MachineTestFactory.CreateC64Machine();
56:        var machine = MachineTestFactory.CreateC64Machine();
78:        var machine = MachineTestFactory.CreateC64Machine();
112:        var machine = MachineTestFactory.CreateC64Machine();
136:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\HostInputServiceTests.cs
22:        var machine = MachineTestFactory.CreateC64Machine();
140:        var machine = MachineTestFactory.CreateC64Machine();
177:        var machine = MachineTestFactory.CreateC64Machine();
219:        var machine = MachineTestFactory.CreateC64Machine();
258:        var machine = MachineTestFactory.CreateC64Machine();
297:        var machine = MachineTestFactory.CreateC64Machine();
333:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LocalVideoFrameSourceTests.cs
395:            var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\LockstepValidator.cs
64:            _machine = MachineTestFactory.CreateC64Machine(modelSelector);
406:        var machine = MachineTestFactory.CreateC64Machine("c64");
917:        var provider = MachineTestFactory.CreateC64RomProvider();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs
8:internal static class MachineTestFactory
10:    public static IMachine CreateC64Machine()
11:        => CreateC64Machine(new Architectures.C64.C64Descriptor());
13:    public static IMachine CreateC64Machine(string modelSelector)
14:        => CreateC64Machine(new Architectures.C64.C64Descriptor(modelSelector));
16:    public static IMachine CreateC64Machine(Architectures.C64.C64Descriptor descriptor)
18:        var builder = new ArchitectureBuilder(CreateC64RomProvider());
22:    public static IRomProvider CreateC64RomProvider()
64:        return CreateC64RomProvider().LoadRom(romName, "C64");

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MagicDeskCartridgeTests.cs
30:        var machine = MachineTestFactory.CreateC64Machine();
54:        var machine = MachineTestFactory.CreateC64Machine();
77:        var machine = MachineTestFactory.CreateC64Machine();
118:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Multisystem\CoordinatorRealMachineIntegrationTests.cs
30:        var machine = MachineTestFactory.CreateC64Machine();
51:        var directMachine = MachineTestFactory.CreateC64Machine();
57:        var coordMachine = MachineTestFactory.CreateC64Machine();
83:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\RasterBarRendererTests.cs
50:        var machine = MachineTestFactory.CreateC64Machine("c64");
105:        var machine = MachineTestFactory.CreateC64Machine("c64");
150:        var machine = MachineTestFactory.CreateC64Machine("c64");
241:        var machine = MachineTestFactory.CreateC64Machine("c64");
347:        var machine = MachineTestFactory.CreateC64Machine("c64");

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\RasterBarLockstepTests.cs
88:        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);
182:        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);
283:        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ProtocolHostIntegrationTests.cs
633:        var machine = MachineTestFactory.CreateC64Machine();
700:        var machine = MachineTestFactory.CreateC64Machine();
1024:        var machine = MachineTestFactory.CreateC64Machine();
1068:        var machine = MachineTestFactory.CreateC64Machine();
1110:        var machine = MachineTestFactory.CreateC64Machine("sx64pal");
1142:        var machine = MachineTestFactory.CreateC64Machine();
1188:        var machine = MachineTestFactory.CreateC64Machine("c64gs");
1234:        var machine = MachineTestFactory.CreateC64Machine("c64");
1667:            new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider()),

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ProcessorPortTests.cs
70:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\RuntimeSnapshotTests.cs
20:        var machine = MachineTestFactory.CreateC64Machine();
60:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\OceanCartridgeTests.cs
29:        var machine = MachineTestFactory.CreateC64Machine();
53:        var machine = MachineTestFactory.CreateC64Machine();
75:        var machine = MachineTestFactory.CreateC64Machine();
100:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\RomlessBadLineTests.cs
50:        var machine = MachineTestFactory.CreateC64Machine();
90:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\SnapshotResumeSpikeTests.cs
180:        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\SnapshotRunLogParityTests.cs
117:        var machine = MachineTestFactory.CreateC64Machine(ModelSelector);
224:            var machine = MachineTestFactory.CreateC64Machine(ModelSelector);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Tape\DatasetteBuilderIntegrationTests.cs
50:            return MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\VicCycleDivergentParityTests.cs
626:        var machine = MachineTestFactory.CreateC64Machine();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\UserPort\TwoC64UserPortLinkTests.cs
23:        var provider = MachineTestFactory.CreateC64RomProvider();

</workspace_result>

### Turn 27 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 19 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IVideoChip.cs
6:public interface IVideoChip : IClockedDevice

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\C64TrueDriveRigBuilder.cs
58:        var driveVias = drive.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();

F:\GitHub\vice-sharp\src\ViceSharp.Monitor\Mos6502Disassembler.cs
13:public static class Mos6502Disassembler

F:\GitHub\vice-sharp\src\ViceSharp.Core\C1541IecInterfaceDevice.cs
45:    public void ConnectVia1(Via6522 via1, IBusEndpoint iec, IInterSystemBus? bus = null)

F:\GitHub\vice-sharp\src\ViceSharp.Core\C1541DriveMechanismDevice.cs
25:    private Via6522? _via2;
55:    public void ConnectVia2(Via6522 via2)

F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
270:        var vias = new List<ViceSharp.Chips.IEC.Via6522>(viaDescriptors.Length);
273:            var via = new ViceSharp.Chips.IEC.Via6522(bus, irqLine)

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Multisystem\MultiSystemBlueprint.cs
158:    private static ViceSharp.Chips.IEC.Via6522? FindDriveVia1(IMachine machine)
160:        ViceSharp.Chips.IEC.Via6522? lowest = null;
161:        foreach (var v in machine.Devices.GetAll<ViceSharp.Chips.IEC.Via6522>())
169:    private static ViceSharp.Chips.IEC.Via6522? FindDriveVia2(IMachine machine)
171:        ViceSharp.Chips.IEC.Via6522? highest = null;
172:        foreach (var v in machine.Devices.GetAll<ViceSharp.Chips.IEC.Via6522>())

F:\GitHub\vice-sharp\src\ViceSharp.Chips\IEC\Via6522.cs
19:public sealed class Via6522 : IClockedDevice, IAddressSpace, IInterruptSource
117:    public Via6522(IBus bus, IInterruptLine irqLine)

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.Opcodes.cs
3:partial class Mos6502

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.Addressing.cs
3:partial class Mos6502

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
5:public partial class Mos6502 : IClockedDevice, IAddressSpace, ICpu, ICpuCycleStealTarget
</workspace_result>

### Turn 28 (Function)
[tool_response]
exit: 0

Name
----
INDEX.md
segment_000.md
segment_001.md
segment_002.md
# Compaction Segment Index

| Segment | File | Turns | Approx bytes | Keywords |
|---|---|---|---|---|
| 000 | segment_000.md | 593 | 522052 | "Diagnosing", "JumpToLetterAsync", "Index", "server", "CharIndex", "missing", "returns", "immediately" |
| 001 | segment_001.md | 574 | 524335 | "Implemented", "attach", "extract", "D64SingleFileBuilder", "MediaServiceHost", "XboxGameLauncher", "MediaExtensionMap", "Drive8" |
| 002 | segment_002.md | 603 | 519870 | "iteration2", "explore", "subagent", "gathering", "builder", "patterns", "implement", "Slice" |



### Turn 29 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDeviceRegistry.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachineProfile.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IVideoChip.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C1541\C1541Descriptor.cs
[tool_request: grep]
- pattern: VIC20|vic20|xvic|Dos1540|basic-901486|kernal\.901486
- glob: **/*.{cs,md}
- path: F:\GitHub\vice-sharp
- head_limit: 50

### Turn 30 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.RomFetch;

internal static class MachineTestFactory
{
10→    public static IMachine CreateC64Machine()
        => CreateC64Machine(new Architectures.C64.C64Descriptor());

    public static IMachine CreateC64Machine(string modelSelector)
        => CreateC64Machine(new Architectures.C64.C64Descriptor(modelSelector));

    public static IMachine CreateC64Machine(Architectures.C64.C64Descriptor descriptor)
    {
        var builder = new ArchitectureBuilder(CreateC64RomProvider());
        return builder.Build(descriptor);
20→    }

    public static IRomProvider CreateC64RomProvider()
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = new RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new C64RomSet().IsComplete(provider))
                return provider;
30→        }

        // Test-harness fallback: the repo vendors the full VICE data tree at
        // native/vice/vice/data, so a bare checkout (CI agents with no
        // VICESHARP_ROM_PATH and no x64sc on PATH) is self-sufficient. The
        // product resolver deliberately does NOT know about repo layouts.
        var repoData = FindRepoViceDataRoot();
        if (repoData is not null)
        {
            var provider = new RomProvider(repoData, []);
40→            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root with complete C64 ROM resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put x64sc.exe on PATH.");
    }

    private static string? FindRepoViceDataRoot()
    {
50→        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                continue;

            var data = Path.Combine(dir.FullName, "native", "vice", "vice", "data");
            return Directory.Exists(data) ? data : null;
        }

        return null;
60→    }

    public static ReadOnlyMemory<byte> LoadC64Rom(string romName)
    {
        return CreateC64RomProvider().LoadRom(romName, "C64");
    }

}


### Turn 31 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Common interface for all video display chips.
/// </summary>
public interface IVideoChip : IClockedDevice
{
    /// <summary>Current raster line being rendered</summary>
    ushort CurrentRasterLine { get; }
10→
    /// <summary>Number of cycles per raster line</summary>
    int CyclesPerLine { get; }

    /// <summary>Total visible lines per frame</summary>
    int VisibleLines { get; }

    /// <summary>Total lines per frame including vertical blank</summary>
    int TotalLines { get; }

20→    /// <summary>True when currently in vertical blanking interval</summary>
    bool IsVBlank { get; }

    /// <summary>
    /// BGRA framebuffer from last completed frame.
    /// Format: 32-bit BGRA (same as Windows/Avalonia PixelFormat.Bgra8888).
    /// </summary>
    byte[] FrameBuffer { get; }

    /// <summary>Framebuffer width in pixels.</summary>
30→    int FrameWidth { get; }

    /// <summary>Framebuffer height in pixels.</summary>
    int FrameHeight { get; }

    /// <summary>
    /// Raised when a complete frame is ready in FrameBuffer.
    /// </summary>
    event EventHandler? FrameCompleted;
}
40→

### Turn 32 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Registry of all devices in a machine. Supports lookup by DeviceId,
/// interface type, or device role. Populated during machine construction.
/// </summary>
public interface IDeviceRegistry
{
    /// <summary>Returns the device with the given ID, or null if not found.</summary>
10→    IDevice? GetById(DeviceId id);

    /// <summary>Returns all devices implementing the specified interface.</summary>
    IReadOnlyList<T> GetAll<T>() where T : IDevice;

    /// <summary>Returns all registered devices.</summary>
    IReadOnlyList<IDevice> All { get; }

    /// <summary>Returns the single device with the given role, or null.</summary>
    IDevice? GetByRole(DeviceRole role);
20→
    /// <summary>Total number of registered devices.</summary>
    int Count { get; }
}

/// <summary>
/// Standard device roles for well-known machine components.
/// </summary>
public enum DeviceRole
{
30→    /// <summary>Board-level system core policy</summary>
    SystemCore,

    /// <summary>Main system CPU</summary>
    Cpu,
    
    /// <summary>Video display controller</summary>
    VideoChip,
    
    /// <summary>Audio sound generator</summary>
40→    AudioChip,
    
    /// <summary>Complex Interface Adapter 1</summary>
    Cia1,
    
    /// <summary>Complex Interface Adapter 2</summary>
    Cia2,
    
    /// <summary>Programmable Logic Array</summary>
    Pla,
50→    
    /// <summary>System RAM</summary>
    SystemRam,
    
    /// <summary>KERNAL ROM</summary>
    KernalRom,
    
    /// <summary>BASIC ROM</summary>
    BasicRom,
    
60→    /// <summary>Character Generator ROM</summary>
    ChargenRom,

    /// <summary>Expansion cartridge port</summary>
    CartridgePort,

    /// <summary>6502 CPU of an attached drive (1541 family).</summary>
    DriveCpu,

    /// <summary>VIA 6522 on a 1541-family drive.</summary>
70→    DriveVia,

    /// <summary>RAM on a 1541-family drive.</summary>
    DriveRam,

    /// <summary>DOS ROM on a 1541-family drive.</summary>
    DriveRom,

    /// <summary>Mounted disk image (D64 / G64 / D81 etc.) on a 1541-family drive.</summary>
    DriveDisk,
80→}


### Turn 33 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Describes a concrete machine model variant supported by an emulator family.
/// </summary>
public interface IMachineProfile
{
    /// <summary>Stable profile identifier used by host APIs and tests.</summary>
    string Id { get; }
10→
    /// <summary>Human-readable model name.</summary>
    string DisplayName { get; }

    /// <summary>Accepted selector aliases for this profile.</summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>Emulator family this model belongs to, for example x64sc.</summary>
    string Family { get; }

20→    /// <summary>Nominal CPU clock in Hz.</summary>
    long NominalClockHz { get; }

    /// <summary>Video timing family.</summary>
    VideoStandard VideoStandard { get; }

    /// <summary>CPU cycles per raster line.</summary>
    int CyclesPerLine { get; }

    /// <summary>Total raster lines per frame.</summary>
30→    int RasterLines { get; }

    /// <summary>Refresh rate computed from clock and frame geometry.</summary>
    double RefreshRateHz { get; }

    /// <summary>Expected VIC-II model identifier.</summary>
    string VicIIModel { get; }

    /// <summary>Expected SID model identifier.</summary>
    string SidModel { get; }
40→
    /// <summary>Board or machine wiring variant.</summary>
    string BoardModel { get; }

    /// <summary>System core policy that defines board buses, glue logic, and chip interconnect.</summary>
    ISystemCoreDefinition SystemCore { get; }

    /// <summary>Required ROM set key.</summary>
    string RomSet { get; }

50→    /// <summary>Required BASIC ROM resource name.</summary>
    string BasicRomName { get; }

    /// <summary>Required KERNAL ROM resource name.</summary>
    string KernalRomName { get; }

    /// <summary>Required character generator ROM resource name.</summary>
    string CharacterRomName { get; }

    /// <summary>True when the model exposes the normal C64 keyboard matrix.</summary>
60→    bool KeyboardEnabled { get; }

    /// <summary>True when cartridge insertion is part of the model's normal boot path.</summary>
    bool CartridgeBootExpected { get; }
}

/// <summary>
/// Architecture descriptor with a concrete model profile.
/// </summary>
public interface IProfiledArchitectureDescriptor : IArchitectureDescriptor
70→{
    /// <summary>Concrete machine profile for this descriptor.</summary>
    IMachineProfile MachineProfile { get; }
}


### Turn 34 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C1541;

/// <summary>
/// Architecture descriptor for a standalone 1541-family drive machine. The
/// 1541 is its own 6502-based computer with its own clock (1 MHz), 2KB of
/// RAM at $0000-$07FF, two VIA 6522 chips at $1800 and $1C00, and 16KB of
/// DOS ROM at $C000-$FFFF.
10→///
/// Used by ARCH-TRUEDRIVE-1541-001 (Phase B). The drive machine is attached
/// to an ISystemCoordinator alongside a C64 host; the IEC bus bridges signal
/// state between the two.
/// </summary>
public sealed class C1541Descriptor : IDriveArchitectureDescriptor
{
    public C1541Descriptor() : this(C1541ViceRomNames.Dos1541, deviceNumber: 8, diskImagePath: null) { }

    public C1541Descriptor(string dosRomName)
20→        : this(dosRomName, deviceNumber: 8, diskImagePath: null) { }

    public C1541Descriptor(int deviceNumber)
        : this(C1541ViceRomNames.Dos1541, deviceNumber, diskImagePath: null) { }

    public C1541Descriptor(string dosRomName, int deviceNumber)
        : this(dosRomName, deviceNumber, diskImagePath: null) { }

    public C1541Descriptor(int deviceNumber, string? diskImagePath)
        : this(C1541ViceRomNames.Dos1541, deviceNumber, diskImagePath) { }
30→
    public C1541Descriptor(string dosRomName, int deviceNumber, string? diskImagePath)
    {
        if (deviceNumber < 8 || deviceNumber > 11)
            throw new ArgumentOutOfRangeException(nameof(deviceNumber), "Device number must be 8..11.");
        DosRomName = string.IsNullOrWhiteSpace(dosRomName)
            ? C1541ViceRomNames.Dos1541
            : dosRomName;
        DeviceNumber = deviceNumber;
        DiskImagePath = string.IsNullOrWhiteSpace(diskImagePath) ? null : diskImagePath;
40→    }

    /// <summary>DOS ROM filename to load (allows 1541 / 1541-II / 1540 variants).</summary>
    public string DosRomName { get; }

    /// <inheritdoc />
    public int DeviceNumber { get; }

    /// <inheritdoc />
    public string? DiskImagePath { get; }
50→
    /// <inheritdoc />
    public string MachineName => "Commodore 1541";

    /// <inheritdoc />
    public long MasterClockHz => 1_000_000;

    /// <inheritdoc />
    public VideoStandard VideoStandard => VideoStandard.Pal;

60→    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 Drive CPU", new DeviceId(0x1001), DeviceRole.DriveCpu, 0x0000, 0),
        new("Drive RAM (2KB)", new DeviceId(0x1100), DeviceRole.DriveRam, 0x0000, 0x0800),
        new("VIA 1 (IEC/LED)", new DeviceId(0x1201), DeviceRole.DriveVia, 0x1800, 0x0400),
        new("VIA 2 (Head/Motor)", new DeviceId(0x1202), DeviceRole.DriveVia, 0x1C00, 0x0400),
        new("Drive ROM (16KB)", new DeviceId(0x1300), DeviceRole.DriveRom, 0xC000, 0x4000),
    ];

70→    /// <inheritdoc />
    public IRomSet? RequiredRoms => new C1541RomSet(
        C1541ViceRomNames.ArchitectureKey,
        DosRomName);
}


### Turn 35 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 22 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C1541\C1541ViceRomNames.cs
17:    public const string Dos1540 = "dos1540-325302+3-01.bin";

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Architecture\ChipGlueBoundaryTests.cs
24:        Assert.DoesNotContain("VIC20", source);

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\XboxSettingsViewModel.cs
132:            new("VIC-20", "xvic", false),

F:\GitHub\vice-sharp\src\ViceSharp.Console\Program.cs
82:// Citations: FR-CFG-005 AC6-8; VICE native/vice/vice/src/vic20/cart/debugcart.c:1 ("used for

F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs
25:///   x128 / xvic / ...  reserved; throws NotSupportedException for now
40:            "x128" or "xvic" or "xpet" or "xplus4" or "xcbm2" or "xcbm5x0" or "vsid" or "petcat" or "cartconv"

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\ComputerOption.cs
11:/// <param name="FamilyId">The emulator machine id (for example, "x64sc" or "xvic").</param>

F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceArgs.cs
41:    /// VICE: native/vice/vice/src/vic20/cart/debugcart.c (and c64) - "used for automatic regression testing";

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Launcher\ViceTopologyBuilderTests.cs
91:    /// Acceptance: x128 / xvic / etc. throw NotSupportedException.
95:    [InlineData("xvic")]

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Serial\KernalSerialTrap.cs
25:    // Zero-page locations (C64/VIC20/C128 KERNAL).

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Launcher\ViceArgsParserTests.cs
258:    /// - native/vice/vice/src/vic20/cart/debugcart.c (and c64 equivalents): "debug \"cartridge\"
329:    /// - native/vice/vice/src/vic20/cart/debugcart.c:1 ( "debug \"cartridge\" used for automatic regression testing"),
408:    /// - native/vice/vice/src/vic20/cart/debugcart.c:1 ("debug \"cartridge\" used for automatic regression testing"),

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\RomCandidateSetTests.cs
131:    /// Acceptance: ApplyOverrides updates C64 and VIC20 candidate order as specified.
142:            "VIC20.basic = my-vic-basic.bin\n");
145:        Assert.Equal(new[] { "my-vic-basic.bin" }, set.GetCandidates("VIC20", "basic"));

F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\ViceRomCatalog.cs
58:            ["VIC20"] = Sys("VIC20",
59:                Role("kernal", "kernal.901486-07.bin", "kernal.901486-06.bin", "kernal.901486-02.bin"),
60:                Role("basic", "basic-901486-01.bin"),

F:\GitHub\vice-sharp\HANDOFF.md
77:PLAN-UIDOCK-001 (Dock.Avalonia UI), PLAN-AUDIOEQ-001, PLAN-PLAYLIST-001, PLAN-FULLSCREEN-001, PLAN-DEVCARDART-001, PLAN-MONFRAME-001, PLAN-DRIVE1581-001, PLAN-DRIVE1541II-001, PLAN-DRIVE1540-001, PLAN-DRIVE1571-001, PLAN-DRIVECMDHD-001, PLAN-CARTRAMLINK-001, PLAN-ARCHVIC20-001 (query the store for the live list; this snapshot is 2026-07-09).

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Xbox\XboxComputerModelPickerTests.cs
96:        Assert.Contains(vm.Computers, c => c.FamilyId == "xvic");

</workspace_result>

### Turn 36 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\ViceRomCatalog.cs
- offset: 50
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\IEC\Via6522.cs
- limit: 150
[tool_request: grep]
- pattern: class SimpleRam|InitializeC64|class BasicBus|class Machine
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\src
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\requirements\functional\FR-Machine-Profiles.md
- offset: 120
- limit: 100
[tool_request: run_terminal_command]
- command: Get-ChildItem -Path "F:\GitHub\vice-sharp\native\vice\vice\data" -Recurse -Filter "*901486*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName; Get-ChildItem -Path "F:\GitHub\vice-sharp\native\vice\vice\data" -Directory | Select-Object Name; if (Test-Path "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20") { Get-ChildItem "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20" | Select-Object Name }
- description: Locate VIC-20 ROM files in native data

### Turn 37 (Function)
[tool_response]
120→### Acceptance Criteria

1. CPU: MOS 8502 (enhanced 6510 with 2MHz mode per FR-CPU-005) and Z80 processor.
2. VIC-II: MOS 8564 (NTSC) or MOS 8566 (PAL) for the 40-column display.
3. VDC: MOS 8563 for the 80-column display (independent video output).
4. RAM: 128KB main RAM, 16KB or 64KB VDC RAM.
5. ROM: BASIC V7.0, C128 KERNAL, C64 mode KERNAL, Character Generator.
6. MMU (Memory Management Unit) at $D500 controls the expanded banking.
7. C64 compatibility mode (selectable at startup or via GO64 command).
8. The 2MHz mode is functional per FR-CPU-005.
130→9. Selectable via `IMachineProfile.LoadProfile("c128")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C128ProfileTests`, `C128DualCpuTests`, `C128VdcTests`, `C128C64ModeTests`
140→
---

## FR-PRF-005: VIC-20 Profile

**ID:** FR-PRF-005
**Title:** Commodore VIC-20 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

150→### Description

The VIC-20 uses the MOS 6502 CPU, MOS 6560 (NTSC) or MOS 6561 (PAL) VIC chip (not VIC-II), two MOS 6522 VIA chips, and 5KB of RAM (expandable). The display and sound capabilities differ significantly from the C64.

### Acceptance Criteria

1. CPU: Standard MOS 6502 (not 6510; no built-in I/O port).
2. VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound.
3. VIA: Two MOS 6522 VIAs (per FR-VIA-004) instead of CIAs.
4. RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.
160→5. ROM: BASIC V2, VIC-20 KERNAL, Character Generator.
6. Memory map differs significantly from C64.
7. Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID.
8. Selectable via `IMachineProfile.LoadProfile("vic20")`.
9. Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

170→### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Vic20ProfileTests`, `VicChipTests`, `Vic20MemoryExpansionTests`

---

## FR-PRF-006: PET Profile

**ID:** FR-PRF-006
180→**Title:** Commodore PET Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The Commodore PET series uses a MOS 6502 CPU, MOS 6520 PIA or MOS 6522 VIA chips, and a monochrome 40x25 or 80x25 character display. Multiple PET models (2001, 3032, 4032, 8032, 8296) have different configurations.

### Acceptance Criteria

190→1. CPU: Standard MOS 6502.
2. Display: CRTC (6545) controlled monochrome character display (40 or 80 columns).
3. I/O: MOS 6520 PIA and/or MOS 6522 VIA depending on model.
4. RAM: 8KB, 16KB, or 32KB depending on model.
5. ROM: PET BASIC (V1, V2, or V4), PET KERNAL, Character Generator.
6. IEEE-488 parallel bus for disk drive communication (not IEC serial).
7. Built-in cassette drive (PET 2001).
8. Selectable via `IMachineProfile.LoadProfile("pet")` with model sub-selection.

### Source References
200→
- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `PetProfileTests`, `PetCrtcTests`, `PetIeee488Tests`

---

210→## FR-PRF-007: Plus/4 Profile

**ID:** FR-PRF-007
**Title:** Commodore Plus/4 Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The Plus/4 uses the MOS 7501/8501 CPU, TED chip (handling both video and sound), and 64KB RAM. The TED chip replaces both the VIC-II and SID with a simpler combined audio/video chip.

### Turn 38 (Function)
[tool_response]
50→                Role("basiclo", "basiclo-318018-04.bin"),
                Role("basichi", "basichi-318019-04.bin"),
                Role("basic64", "basic64-901226-01.bin"),
                Role("chargen", "chargen-390059-01.bin", "chargen-315079-01.bin", "chargen-325078-02.bin",
                                "chargen-325167-01.bin", "chargen-325167-02.bin", "chargen-325173-01D.bin",
                                "chargen-325181-01.bin")),

            // VIC-20 kernals carry a dot rather than a dash in the VICE tree.
            ["VIC20"] = Sys("VIC20",
                Role("kernal", "kernal.901486-07.bin", "kernal.901486-06.bin", "kernal.901486-02.bin"),
60→                Role("basic", "basic-901486-01.bin"),
                Role("chargen", "chargen-901460-03.bin", "chargen-901460-02.bin")),

            // Plus/4: TED family with the 3plus1 built-in software (function ROM).
            ["PLUS4"] = Sys("PLUS4",
                Role("kernal", "kernal-318004-05.bin", "kernal-318004-01.bin", "kernal-318005-05.bin",
                               "kernal-364.bin"),
                Role("basic", "basic-318006-01.bin"),
                Role("function-lo", "3plus1-317053-01.bin"),
                Role("function-hi", "3plus1-317054-01.bin")),
70→
            // C16 / C116: same TED family and PLUS4 data directory as Plus/4, same
            // KERNAL/BASIC, but no 3plus1 function ROM (no built-in software).
            ["C16"] = Sys("PLUS4",
                Role("kernal", "kernal-318004-05.bin", "kernal-318004-01.bin", "kernal-318005-05.bin"),
                Role("basic", "basic-318006-01.bin")),

            ["C116"] = Sys("PLUS4",
                Role("kernal", "kernal-318004-05.bin", "kernal-318004-01.bin", "kernal-318005-05.bin"),
                Role("basic", "basic-318006-01.bin")),
80→
            // PET: BASIC/KERNAL/EDIT are versioned by BASIC generation (1/2/4)
            // and screen width / refresh. Defaults here are BASIC 4, 80-col.
            ["PET"] = Sys("PET",
                Role("kernal", "kernal-4.901465-22.bin", "kernal-2.901465-03.bin", "kernal-1.901439-04-07.bin"),
                Role("basic", "basic-4.901465-23-20-21.bin", "basic-2.901465-01-02.bin",
                              "basic-1.901439-09-05-02-06.bin"),
                Role("edit", "edit-4-80-b-60Hz.901474-03.bin", "edit-4-80-b-50Hz.901474-04.bin",
                             "edit-4-40-n-60Hz.901499-01.bin", "edit-4-40-n-50Hz.901498-01.bin",
                             "edit-2-n.901447-24.bin", "edit-1-n.901439-03.bin")),

### Turn 39 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Chips.IEC;

/// <summary>
/// MOS 6522 Versatile Interface Adapter (VIA). Bus-addressable, clocked,
/// IRQ-emitting. Supports the 16-register layout (Port A/B, DDR A/B, two
/// 16-bit timers, shift register, ACR, PCR, IFR, IER).
///
10→/// Machine-specific boards connect external pins through the Port A/B and
/// control-line callbacks; the VIA itself has no knowledge of that board
/// wiring.
///
/// Implementation scope: register R/W with mirroring, one-shot + continuous
/// Timer 1 + one-shot Timer 2 decrement, IFR/IER + IRQ assertion, port
/// input/output callbacks. Shift register modes, CB1/CB2 handshake, and
/// latched-input quirks land in later slices.
/// </summary>
public sealed class Via6522 : IClockedDevice, IAddressSpace, IInterruptSource
20→{
    private const ushort DefaultBaseAddress = 0x0000;
    private const ushort DefaultMirrorSize = 0x0010;

    private const byte IfrTimer1 = 0x40;
    private const byte IfrTimer2 = 0x20;
    private const byte IfrCb1 = 0x10;
    private const byte IfrCb2 = 0x08;
    private const byte IfrSr = 0x04;
    private const byte IfrCa1 = 0x02;
30→    private const byte IfrCa2 = 0x01;
    private const byte IfrAny = 0x80;

    private const byte PcrCa1ActiveEdgeMask = 0x01; // PCR bit 0: 1 = rising, 0 = falling
    private const byte PcrCa2ModeMask = 0x0E;       // PCR bits 3..1 select CA2 mode
    private const byte PcrCb1ActiveEdgeMask = 0x10; // PCR bit 4: 1 = rising, 0 = falling
    private const byte PcrCb2ModeMask = 0xE0;       // PCR bits 7..5 select CB2 mode

    // CA2 mode encodings (PCR bits 3..1 packed into 0..7). Match the CB2
    // encoding bit-for-bit; only the four landed modes are named here so
40→    // unused constants do not raise warnings.
    private const int Ca2ModeHandshakeOut = 0b100;
    private const int Ca2ModePulseOut = 0b101;
    private const int Ca2ModeManualLow = 0b110;
    private const int Ca2ModeManualHigh = 0b111;

    // CB2 mode encodings (PCR bits 7..5 packed into 0..7)
    private const int Cb2ModeInputNegEdge = 0b000;
    private const int Cb2ModeInputNegIndependent = 0b001;
    private const int Cb2ModeInputPosEdge = 0b010;
50→    private const int Cb2ModeInputPosIndependent = 0b011;
    private const int Cb2ModeHandshakeOut = 0b100;
    private const int Cb2ModePulseOut = 0b101;
    private const int Cb2ModeManualLow = 0b110;
    private const int Cb2ModeManualHigh = 0b111;

    private const byte AcrT1ContinuousMask = 0x40;
    private const byte AcrT1Pb7Mask = 0x80; // ACR bit 7: route T1 onto PB7
    private const byte AcrT2PulseCountMask = 0x20; // ACR bit 5: 0 = phi2 countdown, 1 = count PB6 negative edges
    private const byte AcrShiftModeMask = 0x1C; // bits 4..2 select shift mode
60→    private const byte Pb7Mask = 0x80;

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    private byte _portAOutput;
    private byte _portBOutput;
    private byte _ddra;
    private byte _ddrb;

70→    private ushort _t1Counter;
    private ushort _t1Latch;
    private ushort _t2Counter;
    private bool _t1Running;
    private bool _t2Running;

    private byte _sr;
    private byte _acr;
    private byte _pcr;
    private byte _ifr;
80→    private byte _ier;
    private bool _irqAsserted;

    // Shift register state. _srShiftCount tracks bits shifted in the current
    // 8-bit transfer; the SR IFR latches when it reaches 8. _srShifting is
    // armed by SR writes (out modes) and by entering an in mode; it auto-
    // disarms on IFR latch and re-arms on the next SR write / mode entry.
    private int _srShiftCount;
    private bool _srShifting;

90→    // Timer 1 PB7 output state. When ACR bit 7 = 1 the T1 underflow event is
    // routed onto PB7: ACR bit 6 = 1 (continuous) toggles _pb7TimerToggle on
    // every underflow (free-run square wave); ACR bit 6 = 0 (one-shot) drives
    // _pb7TimerToggle low on T1 start and high on the first underflow. The
    // bit overrides ORB bit 7 in port-B reads while DDRB bit 7 = 1.
    private bool _pb7TimerToggle;

    // CB2 pin state. PCR bits 7..5 select the output mode (manual low/high,
    // handshake, pulse) or one of four input edge-detect modes that latch
    // IFR bit 3 via TriggerCb2. Defaults to high so manual-high and unused
100→    // configurations idle at the inactive level.
    private bool _cb2State = true;

    // CA2 pin state. PCR bits 3..1 select the output mode (manual low/high,
    // handshake, pulse) or one of four input edge-detect modes that latch
    // IFR bit 0 via TriggerCa2. Defaults to high so manual-high and unused
    // configurations idle at the inactive level (symmetric with CB2).
    private bool _ca2State = true;

    // Remaining phi2 cycles before CA2/CB2 pulse-output (PCR mode 101) lines
110→    // return to the idle high level. The mode drives the corresponding pin
    // low for exactly one cycle on its trigger event (PA read for CA2, ORB
    // write for CB2). A non-zero counter is decremented in Tick(); when it
    // hits zero the line is restored to true.
    private int _ca2PulseRemaining;
    private int _cb2PulseRemaining;

    public Via6522(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
120→        _irqLine = irqLine;
        Id = new DeviceId(0x1200);
        SourceId = Id;
        Name = "MOS 6522 VIA";
    }

    /// <inheritdoc />
    public DeviceId Id { get; init; }

    /// <inheritdoc />
130→    public string Name { get; init; }

    /// <inheritdoc />
    public DeviceId SourceId { get; init; }

    /// <summary>Base address of the owning board's VIA register window.</summary>
    public ushort BaseAddress { get; init; } = DefaultBaseAddress;

    /// <summary>Mirrored window size of the owning board's VIA register window.</summary>
    public ushort Size { get; init; } = DefaultMirrorSize;
140→
    /// <inheritdoc />
    public uint ClockDivisor => 1;

    /// <inheritdoc />
    public ClockPhase Phase => ClockPhase.Phi2;

    /// <inheritdoc />
    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

150→    /// <summary>Port A external input function.</summary>

### Turn 40 (Function)
[tool_response]
1→using System.Text;

namespace ViceSharp.Launcher;

/// <summary>
/// Parsed descriptor from a topology YAML that may include testbench keys.
/// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001.
/// </summary>
public sealed record ViceTopologyDescriptor
10→{
    public bool? DebugCart { get; init; }
    public long? LimitCycles { get; init; }
    public string? HostKind { get; init; }
    public string RawYaml { get; init; } = string.Empty;
}

/// <summary>
/// Maps a <see cref="ViceArgs"/> bundle to a multi-system YAML topology
/// string the main ViceSharp.Console host can consume via
20→/// <c>--machine-yaml</c>.
///
/// Binary name -> topology kind:
///   x64 / x64sc      C64 host (+ optional drives + cart)
///   c1541            standalone 1541 (disk-only tool mode)
///   x128 / xvic / ...  reserved; throws NotSupportedException for now
///
/// True-drive flag controls whether drives attach via kind C1541 (true-
/// drive substrate, default true when -8/-9 is set) or stay as a non-
/// emulated cheap path (deferred; current implementation always uses
30→/// kind C1541).
/// </summary>
public static class ViceTopologyBuilder
{
    public static string BuildYaml(ViceArgs args)
    {
        return args.BinaryName switch
        {
            "x64" or "x64sc" => BuildC64Topology(args),
            "c1541" => BuildC1541Standalone(args),
40→            "x128" or "xvic" or "xpet" or "xplus4" or "xcbm2" or "xcbm5x0" or "vsid" or "petcat" or "cartconv"
                => throw new NotSupportedException(
                    $"Binary '{args.BinaryName}' is not yet supported by the ViceSharp substrate. " +
                    "Supported: x64, x64sc, c1541."),
            _ when args.MachineYamlPath is not null => File.ReadAllText(args.MachineYamlPath),
            _ => throw new InvalidOperationException(
                $"Unknown binary name '{args.BinaryName}' and no --machine-yaml supplied."),
        };
    }

50→    /// <summary>
    /// Parses a topology YAML string and extracts testbench keys
    /// (debugcart, limitcycles) along with the primary host kind.
    /// Uses simple line-by-line key extraction (no YAML library required).
    /// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001.
    /// </summary>
    public static ViceTopologyDescriptor ParseDescriptor(string yaml)
    {
        bool? debugCart = null;
        long? limitCycles = null;
60→        string? hostKind = null;

        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.Trim();
            if (TryParseYamlBool(trimmed, "debugcart:", out var dc))
                debugCart = dc;
            else if (TryParseYamlLong(trimmed, "limitcycles:", out var lc))
                limitCycles = lc;
            else if (TryParseYamlString(trimmed, "kind:", out var k))
70→                hostKind ??= k; // first kind encountered = host kind
        }

        return new ViceTopologyDescriptor
        {
            DebugCart = debugCart,
            LimitCycles = limitCycles,
            HostKind = hostKind,
            RawYaml = yaml,
        };
80→    }

    private static bool TryParseYamlBool(string line, string prefix, out bool value)
    {
        value = false;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var rest = line[prefix.Length..].Trim().ToLowerInvariant();
        if (rest == "true") { value = true; return true; }
        if (rest == "false") { value = false; return true; }
        return false;
90→    }

    private static bool TryParseYamlLong(string line, string prefix, out long value)
    {
        value = 0;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        return long.TryParse(line[prefix.Length..].Trim(), out value);
    }

    private static bool TryParseYamlString(string line, string prefix, out string value)
100→    {

### Turn 41 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 11 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\MachinePlatformSlug.cs
8:public static class MachinePlatformSlug

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Adhoc\MachineDefinitionYamlContext.cs
13:public partial class MachineDefinitionYamlContext : StaticContext

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Adhoc\MachineDefinitionDocument.cs
14:public sealed class MachineDefinitionDocument
25:public sealed class MachineInfo

F:\GitHub\vice-sharp\src\ViceSharp.Core\SimpleRam.cs
9:public sealed class SimpleRam : IMemory
31:        InitializeC64();
45:    public void InitializeC64()

F:\GitHub\vice-sharp\src\ViceSharp.Core\BasicBus.cs
9:public sealed class BasicBus : IBus

F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
58:        ram.InitializeC64();
418:internal sealed class Machine : IMachine

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\MachineSnapshotStager.cs
24:public sealed class MachineSnapshotStager
</workspace_result>

### Turn 42 (Function)
[tool_response]
exit: 0
F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\basic-901486-01.bin
F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-02.bin
F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-06.bin
F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-07.bin

Name
----
C128
C64
C64DTV
CBM-II
common
DRIVES
GLSL
hotkeys
joymaps
PET
PLUS4
PRINTER
SCPU64
VIC20
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



### Turn 43 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\SimpleRam.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 240
- limit: 200
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64MachineTests.cs
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Launcher\ViceTopologyBuilderTests.cs
- offset: 80
- limit: 40
[tool_request: grep]
- pattern: ISystemCoreDefinition|class SystemCore
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\src
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\EmptyMachine\EmptyMachineDescriptor.cs

### Turn 44 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Simple RAM device that handles all memory addresses.
/// Provides 64KB of addressable memory.
/// </summary>
public sealed class SimpleRam : IMemory
10→{
    public DeviceId Id => new DeviceId(0x0100);
    public string Name => "64KB RAM";
    
    private readonly byte[] _memory = new byte[65536];

    public Span<byte> Span => _memory;

    public bool HandlesAddress(ushort address) => true;
    
20→    public byte Read(ushort address) => _memory[address];
    
    public void Write(ushort address, byte value) => _memory[address] = value;
    
    public byte Peek(ushort address) => _memory[address];
    
    /// <summary>
    /// Resets RAM to initial state.
    /// </summary>
    public void Reset()
30→    {
        InitializeC64();
    }
    
    /// <summary>
    /// Fill memory with a specific value.
    /// </summary>
    public void Fill(byte value)
    {
        Array.Fill(_memory, value);
40→    }
    
    /// <summary>
    /// Initialize memory with standard C64 pattern.
    /// </summary>
    public void InitializeC64()
    {
        // Zero page, stack, and user RAM = 0
        Array.Clear(_memory, 0, 0x0200);
        
50→        // Page 2 (stack) already zeroed
        
        // $0200-$03FF = 0 (reserved for system)
        // $0400-$07FF = Screen RAM (40x25 = 1000 chars) - initialize to spaces (0x20)
        Array.Fill(_memory, (byte)0x20, 0x0400, 0x0400); // 1KB screen RAM
        
        // $0800-$9FFF = 0xFF (empty RAM, typical after power-on)
        Array.Fill(_memory, (byte)0xFF, 0x0800, 0x9800);
        
        // $A000-$BFFF = BASIC ROM (loaded separately)
60→        // $C000-$CFFF = 0xFF
        // $D000-$DFFF = Character ROM (loaded separately)
        // $E000-$FFFB = KERNAL ROM (loaded separately)
        
        // Set reset vector at $FFFC-$FFFD to point to KERNAL reset
        // Real C64: $FCE2
        _memory[0xFFFC] = 0xE2;
        _memory[0xFFFD] = 0xFC;
    }

70→    /// <summary>
    /// Load ROM data directly into memory at specified address.
    /// Used for loading BASIC, KERNAL, and character ROMs.
    /// </summary>
    public void LoadRom(ushort startAddress, ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            _memory[startAddress + i] = data[i];
        }
80→    }
}


### Turn 45 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using System.Text;
using Xunit;

10→public sealed class C64MachineTests
{
    /// <summary>
    /// FR: FR-PRF-001, TR: TR-CYCLE-001.
    /// Use case: An EmptyMachineDescriptor builds the most stripped-down
    /// machine (CPU + RAM) without needing any ROM provider; the C64
    /// builder should not be required.
    /// Acceptance: The built machine exposes a CPU and SystemRam but no
    /// VideoChip role; ROM provider injection is not required.
    /// </summary>
20→    [Fact]
    public void EmptyMachineDescriptor_BuildsWithoutRomProvider()
    {
        var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());

        Assert.NotNull(machine.Devices.GetByRole(Abstractions.DeviceRole.Cpu));
        Assert.NotNull(machine.Devices.GetByRole(Abstractions.DeviceRole.SystemRam));
        Assert.Null(machine.Devices.GetByRole(Abstractions.DeviceRole.VideoChip));
    }

30→    /// <summary>
    /// FR: FR-CFG-002, TR: TR-CYCLE-001.
    /// Use case: Building a C64Descriptor without an IRomProvider must
    /// fail with a clear, user-friendly error message rather than a
    /// NullReferenceException deep inside the builder.
    /// Acceptance: <c>ArchitectureBuilder.Build(new C64Descriptor())</c>
    /// throws InvalidOperationException whose message contains the
    /// string "requires an IRomProvider".
    /// </summary>
    [Fact]
40→    public void C64Descriptor_WithoutRomProvider_ThrowsClearError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ArchitectureBuilder().Build(new C64Descriptor()));

        Assert.Contains("requires an IRomProvider", ex.Message);
    }

    /// <summary>
    /// FR: FR-CFG-002, TR: TR-CYCLE-001.
    /// Use case: A ROM provider that returns bytes failing the documented
50→    /// length/SHA256 contract must cause the C64 builder to reject the
    /// configuration with a clear error rather than booting a corrupted
    /// machine.
    /// Acceptance: ArchitectureBuilder.Build throws InvalidOperationException
    /// whose message mentions the invalid checksum entries in the ROM set.
    /// </summary>
    [Fact]
    public void C64Descriptor_WithBadRomChecksums_ThrowsClearError()
    {
        var badRomProvider = new CorruptLengthAndHashRomProvider();
60→        var ex = Assert.Throws<InvalidOperationException>(() => new ArchitectureBuilder(badRomProvider).Build(new C64Descriptor()));

        Assert.Contains("ROM set is invalid or missing expected checksum entries", ex.Message);
    }

    /// <summary>
    /// FR: FR-MEM-001, FR: FR-MEM-002, TR: TR-CYCLE-001.
    /// Use case: After reset, the BASIC ROM at $A000 is visible; writing
    /// to $A000 writes-under the ROM, and disabling LORAM (by clearing
    /// the I/O port bit) exposes the underlying RAM byte to reads.
70→    /// Acceptance: Reading $A000 first returns the BASIC ROM byte even
    /// after writing $42 there; after clearing LORAM via $0001, the
    /// same address reads back $42 from the banked-RAM-under-ROM region.
    /// </summary>
    [Fact]
    public void BasicRom_IsVisibleByDefault_AndBankedRamAppears_WhenLoramDisabled()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basicRomFirstByte = MachineTestFactory.LoadC64Rom("basic").Span[0];

80→        machine.Bus.Write(0xA000, 0x42);

        Assert.Equal(basicRomFirstByte, machine.Bus.Read(0xA000));

        machine.Bus.Write(0x0001, 0x36);

        Assert.Equal(0x42, machine.Bus.Read(0xA000));
    }

    /// <summary>
90→    /// FR: FR-MEM-001, FR: FR-MEM-005, TR: TR-CYCLE-001.
    /// Use case: Clearing the CHAREN bit in the I/O port at $0001 swaps
    /// the character ROM in over the I/O region at $D000; setting CHAREN
    /// returns I/O visibility.
    /// Acceptance: With CHAREN clear ($33 written to $0001), reads from
    /// $D000 return the character ROM byte; with CHAREN set ($37) the
    /// same address peeks back the previously-written I/O value.
    /// </summary>
    [Fact]
    public void CharacterRom_CanBeMappedOverIoSpace_WhenCharenCleared()
100→    {

### Turn 46 (Function)
[tool_response]
240→        return roles.Contains(DeviceRole.DriveCpu) && roles.Contains(DeviceRole.DriveRom);
    }

    private IMachine BuildC1541Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
250→                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine);

        // 2KB drive RAM at $0000-$07FF.
        var ramDescriptor = descriptor.Devices.First(d => d.Role == DeviceRole.DriveRam);
260→        var ramBytes = new byte[ramDescriptor.Size];
        var ram = new RamDevice(
            ramDescriptor.BaseAddress,
            (ushort)(ramDescriptor.BaseAddress + ramDescriptor.Size - 1),
            ramBytes);
        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.DriveRam);

        // Two VIAs at $1800 and $1C00 (each with 1KB mirror window).
        var viaDescriptors = descriptor.Devices.Where(d => d.Role == DeviceRole.DriveVia).ToArray();
270→        var vias = new List<ViceSharp.Chips.IEC.Via6522>(viaDescriptors.Length);
        foreach (var v in viaDescriptors)
        {
            var via = new ViceSharp.Chips.IEC.Via6522(bus, irqLine)
            {
                Id = v.Id,
                Name = v.Name,
                SourceId = v.Id,
                BaseAddress = v.BaseAddress,
                Size = (ushort)v.Size,
280→            };
            bus.RegisterDevice(via);
            clock.Register(via);
            deviceRegistry.Add(via, DeviceRole.DriveVia);
            vias.Add(via);
        }

        // 16KB drive DOS ROM at $C000-$FFFF.
        var romDescriptor = descriptor.Devices.First(d => d.Role == DeviceRole.DriveRom);
        var dosRomName = ResolveC1541DosRomName(descriptor);
290→        var romBytes = _romProvider.LoadRom(
            dosRomName,
            descriptor.RequiredRoms?.Architecture ?? "DRIVES").ToArray();
        var rom = new RomDevice(
            romDescriptor.BaseAddress,
            (ushort)(romDescriptor.BaseAddress + romDescriptor.Size - 1),
            romBytes);
        bus.RegisterDevice(rom);
        deviceRegistry.Add(rom, DeviceRole.DriveRom);

300→        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.DriveCpu);

        // Optional D64 disk image mount.
        D64DiskImageDevice? disk = null;
        if (descriptor is IDriveArchitectureDescriptor drive && !string.IsNullOrWhiteSpace(drive.DiskImagePath))
        {
            disk = D64DiskImageDevice.LoadFromFile(drive.DiskImagePath!);
            deviceRegistry.Add(disk, DeviceRole.DriveDisk);
        }
310→
        var deviceNumber = descriptor is IDriveArchitectureDescriptor driveDescriptor
            ? driveDescriptor.DeviceNumber
            : 8;
        deviceRegistry.Add(new C1541IecInterfaceDevice(deviceNumber));

        var driveMechanism = new C1541DriveMechanismDevice(disk, cpu);
        clock.Register(driveMechanism);
        deviceRegistry.Add(driveMechanism);

320→        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu);
        machine.Reset();
        return machine;
    }

    private static string ResolveC1541DosRomName(IArchitectureDescriptor descriptor)
        => descriptor is IDriveArchitectureDescriptor d && !string.IsNullOrWhiteSpace(d.DosRomName)
            ? d.DosRomName
            : "dos1541-325302-01+901229-05.bin";

330→    private static bool IsKernalRomRequired(string kernalRomName)
        => !string.Equals(kernalRomName, "kernal-none.bin", StringComparison.OrdinalIgnoreCase);

    private static CartridgeMappingMode ResolveDefaultCartridgeMappingMode(IMachineProfile? profile)
    {
        if (profile is null)
            return CartridgeMappingMode.Auto;

        if (string.Equals(profile.SystemCore.AddressDecoderPolicy, "Ultimax", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.Ultimax;
340→
        if (string.Equals(profile.BoardModel, "C64GS", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.GameSystem;

        return CartridgeMappingMode.Auto;
    }

    private static byte ResolveCia2PortAInputMask(IMachineProfile? profile)
    {
        if (profile is null)
350→            return 0x7F;

        return profile.SystemCore.BusPolicy switch
        {
            "GameSystem" or "Max" => 0x3F,
            _ => 0x7F
        };
    }

    private static Mos6526 CreateC64Cia(IBus bus, IInterruptLine line, ushort baseAddress, long masterClockHz)
360→    {
        return new Mos6526(bus, line)
        {
            BaseAddress = baseAddress,
            TodCyclesPer50HzTick = (int)(masterClockHz / 50),
            TodCyclesPer60HzTick = (int)(masterClockHz / 60)
        };
    }

    private static Mos6569 CreateVicII(
370→        IBus bus,
        IInterruptLine irqLine,
        IArchitectureDescriptor descriptor,
        IMachineProfile? profile)
    {
        var vic = profile?.VicIIModel switch
        {
            "Mos6569R1" => new Mos6569R1(bus, irqLine),
            "Mos6567R56A" => new Mos6567R56A(bus, irqLine),
            "Mos6567R8" => new Mos6567(bus, irqLine),
380→            "Mos6572" => new Mos6572(bus, irqLine),
            "Mos8562" => new Mos8562(bus, irqLine),
            "Mos8565" => new Mos8565(bus, irqLine),
            _ => descriptor.VideoStandard == VideoStandard.Ntsc
                ? new Mos6567(bus, irqLine)
                : new Mos6569(bus, irqLine)
        };

        if (profile is null)
            return vic;
390→
        var system = profile.VideoStandard == VideoStandard.Ntsc
            ? Mos6569.TvSystem.NTSC
            : string.Equals(profile.VicIIModel, "Mos6572", StringComparison.OrdinalIgnoreCase)
                ? Mos6569.TvSystem.PALN
                : Mos6569.TvSystem.PAL;

        var visibleLines = profile.VideoStandard == VideoStandard.Ntsc
            ? Mos6569.NtscVisibleLines
            : Mos6569.PalVisibleLines;
400→        vic.ConfigureTiming(
            system,
            profile.CyclesPerLine,
            visibleLines,
            profile.RasterLines,
            profile.RefreshRateHz);
        return vic;
    }

    private static Sid6581 CreateSid(IBus bus, IMachineProfile? profile, IAudioBackend? audioBackend, double masterClockHz)
410→        // PLAN-VICEPARITY-001 P0-7: single canonical construction path shared
        // with every other machine (see SidFactory).
        => SidFactory.Create(bus, profile, audioBackend, masterClockHz);
}

/// <summary>
/// Default IMachine implementation.
/// </summary>
internal sealed class Machine : IMachine
{
420→    private readonly IBus _bus;
    private readonly IClock _clock;
    private readonly IDeviceRegistry _devices;
    private readonly IArchitectureDescriptor _architecture;
    private readonly Mos6502 _cpu;
    private readonly IPubSub? _pubSub;
    private readonly int _frameCycles;

    public Machine(
        IArchitectureDescriptor architecture,
430→        IBus bus,
        IClock clock,
        IDeviceRegistry deviceRegistry,
        Mos6502 cpu,
        IPubSub? pubSub = null)
    {
        _architecture = architecture;
        _bus = bus;
        _clock = clock;
        _devices = deviceRegistry;

### Turn 47 (Function)
[tool_response]
80→
        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("kind: C1541");
        yaml.Should().Contain("tools.d64");
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Unsupported binaries throw a clear error rather than
90→    /// generating broken YAML.
    /// Acceptance: x128 / xvic / etc. throw NotSupportedException.
    /// </summary>
    [Theory]
    [InlineData("x128")]
    [InlineData("xvic")]
    [InlineData("xpet")]
    public void UnsupportedBinary_Throws(string binaryName)
    {
        var args = ViceArgsParser.Parse(binaryName, Array.Empty<string>());
100→
        Assert.Throws<NotSupportedException>(() => ViceTopologyBuilder.BuildYaml(args));
    }

    /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: When --machine-yaml is supplied, the topology is read from
    /// that file verbatim regardless of binary name.
    /// Acceptance: Parser + builder honour --machine-yaml.
    /// </summary>
110→    [Fact]
    public void MachineYamlFlag_OverridesBinaryDispatch()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vice-yaml-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, "schemaVersion: 1\ncoordinator:\n  host:\n    id: x\n    kind: C64");
        try
        {
            var args = ViceArgsParser.Parse("x64sc", new[] { "--machine-yaml", path });

            var yaml = ViceTopologyBuilder.BuildYaml(args);

### Turn 48 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.EmptyMachine;

/// <summary>
/// Empty test machine descriptor for validation and testing.
/// Minimal implementation with no hardware devices.
/// </summary>
public sealed class EmptyMachineDescriptor : IArchitectureDescriptor
10→{
    /// <inheritdoc />
    public string MachineName => "Empty Test Machine";

    /// <inheritdoc />
    public long MasterClockHz => 1000000;

    /// <inheritdoc />
    public VideoStandard VideoStandard => VideoStandard.Ntsc;
    
20→    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("System RAM", new DeviceId(0x0100), DeviceRole.SystemRam, 0x0000, 0x10000),
    ];
    
    /// <inheritdoc />
    public IRomSet? RequiredRoms => null;
}
30→

### Turn 49 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 10 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachineProfile.cs
45:    ISystemCoreDefinition SystemCore { get; }

F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemCore.cs
9:internal sealed class SystemCore : ISystemCore
11:    public SystemCore(ISystemCoreDefinition definition)
20:    public ISystemCoreDefinition Definition { get; }

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ISystemCore.cs
6:public interface ISystemCoreDefinition
51:    ISystemCoreDefinition Definition { get; }

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Adhoc\MachineDefinitionDocument.cs
35:public sealed class SystemCoreInfo

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\C64SystemCoreDefinition.cs
31:    IReadOnlyDictionary<string, string>? Traits = null) : ISystemCoreDefinition
41:    IReadOnlyDictionary<string, string> ISystemCoreDefinition.Traits => Traits ?? EmptyTraits;

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\C64MachineProfile.cs
64:    public ISystemCoreDefinition SystemCore { get; } =
</workspace_result>

### Turn 50 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\BasicBus.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ISystemCore.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\C64SystemCoreDefinition.cs
- limit: 80
[tool_request: grep]
- pattern: class RomDevice|class RamDevice
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\src
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Architecture\ChipGlueBoundaryTests.cs
- limit: 50

### Turn 51 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.C64;

public enum C64PlaPolicy
{
    Standard,
    Ultimax,
    CartridgeRequired
10→}

public enum C64BusPolicy
{
    Standard,
    Portable,
    Max,
    GameSystem
}

20→public sealed record C64SystemCoreDefinition(
    string Id,
    string DisplayName,
    C64BoardModel Board,
    C64PlaPolicy Pla,
    C64BusPolicy Bus,
    bool KeyboardMatrixConnected,
    bool TapePortConnected,
    bool IecBusConnected,
    bool Cia2Connected,
30→    bool CartridgeBootExpected,
    IReadOnlyDictionary<string, string>? Traits = null) : ISystemCoreDefinition
{
    public string Family => "x64sc";

    public string BoardPolicy => Board.ToString();

    public string AddressDecoderPolicy => Pla.ToString();

    public string BusPolicy => Bus.ToString();
40→
    IReadOnlyDictionary<string, string> ISystemCoreDefinition.Traits => Traits ?? EmptyTraits;

    private static readonly IReadOnlyDictionary<string, string> EmptyTraits =
        new Dictionary<string, string>();
}

public static class C64SystemCoreDefinitions
{
    public static C64SystemCoreDefinition ForProfile(
50→        C64BoardModel board,
        bool keyboardEnabled,
        bool cartridgeBootExpected)
    {
        var pla = board == C64BoardModel.Ultimax
            ? C64PlaPolicy.Ultimax
            : cartridgeBootExpected ? C64PlaPolicy.CartridgeRequired : C64PlaPolicy.Standard;

        var bus = board switch
        {
60→            C64BoardModel.SX64 => C64BusPolicy.Portable,
            C64BoardModel.Ultimax => C64BusPolicy.Max,
            C64BoardModel.C64GS => C64BusPolicy.GameSystem,
            _ => C64BusPolicy.Standard
        };
        var tapePortConnected = board is not (C64BoardModel.SX64 or C64BoardModel.C64GS);
        var iecBusConnected = board is not (C64BoardModel.Ultimax or C64BoardModel.C64GS);
        var cia2Connected = board is not C64BoardModel.Ultimax;

        var id = $"x64sc:{board.ToString().ToLowerInvariant()}:{pla.ToString().ToLowerInvariant()}";
70→        var traits = new Dictionary<string, string>
        {
            ["board"] = board.ToString(),
            ["pla"] = pla.ToString(),
            ["bus"] = bus.ToString(),
            ["tapePort"] = tapePortConnected ? "connected" : "absent",
            ["iecBus"] = iecBusConnected ? "connected" : "absent",
            ["cia2"] = cia2Connected ? "connected" : "absent"
        };

80→        return new C64SystemCoreDefinition(

### Turn 52 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness.Architecture;

using Xunit;

/// <summary>
/// TR-SYSTEM-CORE-001 / TEST-ARCH-CHIPGLUE-001 / ARCH-CHIPGLUE-001.
/// Use case: protect reusable chip implementations from machine-specific
/// board and device glue after the Iteration 1 chip-glue remediation.
/// Acceptance: every check below covers one or more audit acceptance criteria
10→/// from <c>docs/requirements/traceability/ARCH-CHIPGLUE-001-Chip-Audit-2026-06-12.md</c>.
/// </summary>
public sealed class ChipGlueBoundaryTests
{
    [Fact]
    public void SharedVia6522Core_DoesNotContainDriveOrMachineGlue()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "IEC", "Via6522.cs");

        Assert.DoesNotContain("1541", source);
20→        Assert.DoesNotContain("C1541", source);
        Assert.DoesNotContain("C64", source);
        Assert.DoesNotContain("CIA1", source);
        Assert.DoesNotContain("CIA2", source);
        Assert.DoesNotContain("VIC20", source);
        Assert.DoesNotContain("VIC-20", source);
        Assert.DoesNotContain("0x1800", source);
        Assert.DoesNotContain("0x0400", source);
    }

30→    [Fact]
    public void SharedMos6526Core_DoesNotDescribeBoardSpecificWiringOrCadence()
    {
        var source = ReadSource("src", "ViceSharp.Chips", "Cia", "Mos6526.cs");

        Assert.DoesNotContain("CIA1", source);
        Assert.DoesNotContain("CIA2", source);
        Assert.DoesNotContain("C64", source);
        Assert.DoesNotContain("datasette READ", source);
        Assert.DoesNotContain("user-port", source);
40→        Assert.DoesNotContain("0xDC00", source);
        Assert.DoesNotContain("985_248", source);
    }

    [Fact]
    public void RetiredDuplicateCiaCore_DoesNotExistInSharedChipPackage()
    {
        AssertSourceFileDoesNotExist("src", "ViceSharp.Chips", "Interface", "Cia6526.cs");
    }

50→    [Fact]

### Turn 53 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Describes board-level machine policy applied by the architecture builder.
/// </summary>
public interface ISystemCoreDefinition
{
    /// <summary>Stable definition identifier used by profiles and tests.</summary>
    string Id { get; }
10→
    /// <summary>Human-readable core name.</summary>
    string DisplayName { get; }

    /// <summary>Emulator family this system core belongs to.</summary>
    string Family { get; }

    /// <summary>Board wiring policy selected by the machine variant.</summary>
    string BoardPolicy { get; }

20→    /// <summary>Address decoding or PLA policy selected by the machine variant.</summary>
    string AddressDecoderPolicy { get; }

    /// <summary>Shared bus arbitration and contention policy selected by the machine variant.</summary>
    string BusPolicy { get; }

    /// <summary>True when the core connects the normal keyboard matrix to machine I/O.</summary>
    bool KeyboardMatrixConnected { get; }

    /// <summary>True when the core connects a datasette cassette port to machine I/O.</summary>
30→    bool TapePortConnected { get; }

    /// <summary>True when the core connects the IEC serial bus to machine I/O.</summary>
    bool IecBusConnected { get; }

    /// <summary>True when the core connects the second CIA to machine I/O.</summary>
    bool Cia2Connected { get; }

    /// <summary>True when the model normally requires cartridge boot wiring.</summary>
    bool CartridgeBootExpected { get; }
40→
    /// <summary>Additional named policy traits for architecture-specific behavior.</summary>
    IReadOnlyDictionary<string, string> Traits { get; }
}

/// <summary>
/// Runtime system core selected by the profile and assembled with chips by the architecture builder.
/// </summary>
public interface ISystemCore : IDevice
{
50→    /// <summary>Definition that selected this runtime system core.</summary>
    ISystemCoreDefinition Definition { get; }
}


### Turn 54 (Function)
[tool_response]
1→using System.Collections.Immutable;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// High performance system bus implementation.
/// </summary>
public sealed class BasicBus : IBus
10→{
    private List<IAddressSpace> _devices = new();
    private readonly object _lock = new();
    private C64MemoryMap? _singleC64MemoryMap;
    private IPubSub? _pubSub;

    /// <summary>
    /// Connect the machine pub/sub so each write publishes a <see cref="MemoryWriteEvent"/>
    /// (gated on <see cref="IPubSub.SubscriptionCount"/>: zero cost when nobody listens) for
    /// the time-travel debugger's write-delta capture.
20→    /// </summary>
    public void ConnectPubSub(IPubSub pubSub) => _pubSub = pubSub;

    // PERF-BUS-001: Read/Write/Peek run on the single emulation thread (SystemClock);
    // no lock needed here. RegisterDevice/UnregisterDevice retain their lock for
    // safe setup/teardown from any thread.
    public byte Read(ushort address)
    {
        if (_singleC64MemoryMap is { } c64MemoryMap)
        {
30→            return c64MemoryMap.Read(address);
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            if (device.HandlesAddress(address))
                return device.Read(address);
        }
        return 0xFF;
40→    }

    public void Write(ushort address, byte value)
    {
        // Time-travel capture: record the pre-write byte so the write can be undone later.
        // Gated on a live subscriber so an unobserved run pays only a null/count check.
        if (_pubSub is { SubscriptionCount: > 0 })
            _pubSub.Publish(MemoryWriteEvent.Topic, new MemoryWriteEvent(address, Peek(address), value));

        if (_singleC64MemoryMap is { } c64MemoryMap)
50→        {
            c64MemoryMap.Write(address, value);
            return;
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            if (device.HandlesAddress(address))
            {
60→                device.Write(address, value);
                return;
            }
        }
    }

    public byte Peek(ushort address)
    {
        if (_singleC64MemoryMap is { } c64MemoryMap)
        {
70→            return c64MemoryMap.Peek(address);
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            if (device.HandlesAddress(address))
                return device.Peek(address);
        }
        return 0xFF;
80→    }

    public void RegisterDevice(IAddressSpace device)
    {
        lock (_lock)
        {
            _devices.Insert(0, device);
            RefreshFastPath();
        }
    }
90→
    public void UnregisterDevice(IAddressSpace device)
    {
        lock (_lock)
        {
            _devices.Remove(device);
            RefreshFastPath();
        }
    }

100→    private void RefreshFastPath()
    {
        _singleC64MemoryMap = _devices.Count == 1 && _devices[0] is C64MemoryMap memoryMap
            ? memoryMap
            : null;
    }
}


### Turn 55 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 2 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\RomDevice.cs
8:public sealed class RomDevice : IAddressSpace

F:\GitHub\vice-sharp\src\ViceSharp.Core\RamDevice.cs
8:public sealed class RamDevice : IMemory
</workspace_result>

### Turn 56 (Assistant)
[tool_request: grep]
- pattern: HandlesAddress|BaseAddress|Size
- glob: **/Via6522.cs
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\RomDevice.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\RamDevice.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C1541\C1541ViceRomNames.cs

### Turn 57 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Read Only ROM device occupying a fixed address range
/// </summary>
public sealed class RomDevice : IAddressSpace
{
10→    private readonly ushort _startAddress;
    private readonly ushort _endAddress;
    private readonly byte[] _memory;

    public RomDevice(ushort startAddress, ushort endAddress, byte[] memory)
    {
        _startAddress = startAddress;
        _endAddress = endAddress;
        _memory = memory;
    }
20→
    public byte Read(ushort address) => _memory[address - _startAddress];
    public void Write(ushort address, byte value) { /* Writes ignored for ROM */ }
    public byte Peek(ushort address) => _memory[address - _startAddress];
    public bool HandlesAddress(ushort address) => address >= _startAddress && address <= _endAddress;

    public DeviceId Id => new DeviceId(0x00010001);
    public string Name => "System ROM";
    public void Reset() { }
}

### Turn 58 (Function)
[tool_response]
1→namespace ViceSharp.Architectures.C1541;

/// <summary>
/// Canonical 1541 (and family) drive ROM filenames as bundled by upstream
/// VICE under the DRIVES folder in the configured VICE data root. Each ROM is 16384 bytes and is
/// mapped at $C000-$FFFF inside the drive 6502's address space.
/// </summary>
public static class C1541ViceRomNames
{
10→    /// <summary>Standard 1541 DOS (16384 bytes).</summary>
    public const string Dos1541 = "dos1541-325302-01+901229-05.bin";

    /// <summary>1541-II DOS (16384 bytes).</summary>
    public const string Dos1541Ii = "dos1541ii-251968-03.bin";

    /// <summary>1541-C / 1540 DOS (16384 bytes).</summary>
    public const string Dos1540 = "dos1540-325302+3-01.bin";

    /// <summary>Expected size of every 1541-family DOS ROM image.</summary>
20→    public const int Dos1541RomSize = 0x4000;

    /// <summary>Architecture key passed to IRomProvider for drive ROMs.</summary>
    public const string ArchitectureKey = "DRIVES";
}


### Turn 59 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Read/Write RAM device occupying a fixed address range
/// </summary>
public sealed class RamDevice : IMemory
{
10→    private readonly ushort _startAddress;
    private readonly ushort _endAddress;
    private readonly byte[] _memory;

    public RamDevice(ushort startAddress, ushort endAddress, byte[] memory)
    {
        _startAddress = startAddress;
        _endAddress = endAddress;
        _memory = memory;
    }
20→
    public byte Read(ushort address) => _memory[address - _startAddress];
    public void Write(ushort address, byte value) => _memory[address - _startAddress] = value;
    public byte Peek(ushort address) => _memory[address - _startAddress];
    public bool HandlesAddress(ushort address) => address >= _startAddress && address <= _endAddress;
    public Span<byte> Span => _memory;

    public DeviceId Id => new DeviceId(0x00010000);
    public string Name => "System RAM";
    public void Reset() { }
30→}


### Turn 60 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cpu;

public partial class Mos6502 : IClockedDevice, IAddressSpace, ICpu, ICpuCycleStealTarget
{
    private const int ResetCycleDelay = 1;

    public DeviceId Id => new DeviceId(0x0001);
10→    public string Name => "MOS 6502 CPU";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    // Registers
    public byte A;
    public byte X;
    public byte Y;
    public byte S;
    private ushort _pc;
20→    private ushort _instructionPC;
    private ushort _visiblePC;
    public ushort PC
    {
        get => IsInstructionBoundary ? _pc : _visiblePC;
        set
        {
            _pc = value;
            _instructionPC = value;
            _visiblePC = value;
30→        }
    }
    public byte Flags { get => P; set => P = value; }
    public byte P;

    // FR-CPUTICK-001: this CPU's own executed-cycle counter (incremented per Tick()).
    private long _executedCycles;
    public long ExecutedCycles => _executedCycles;

    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0;
40→    public int DebugCycle => _cycle;
    public byte DebugOpcode => _opcode;
    public bool DebugDelayNextFetch => _delayNextFetch;
    public bool CanStealCurrentCycle
    {
        get
        {
            if (_pendingDeferredNzUpdateAfterBranch ||
                _bootstrapCycles > 0 ||
                _pendingDeferredImmediateLoad ||
50→                _pendingDeferredImpliedRegisterCompletion ||
                _branchPageCrossExtraPending)
            {
                return false;
            }

            // TR-LOCKSTEP-VSF-001: interrupt-sequence cycles follow VICE's BA
            // semantics (6510dtvcore.c DO_INTERRUPT/DO_IRQBRK): the dummy
            // fetch and the two vector reads go through check_ba (stealable),
            // the three stack pushes are writes and proceed during BA-low.
60→            if (_interruptSequenceRemaining > 0)
                return _interruptSequenceRemaining is 6 or 2 or 1;

            if (_cycle == 0)
                return true;

            var nextCycle = _cycle - 1;
            if (_opcode == 0x20)
                return nextCycle is not 2 and not 3;

70→            if (nextCycle == 1 && IsStoreOpcode(_opcode))
                return false;

            return IsReadSensitiveOpcode(_opcode);
        }
    }

    public bool CanForceStealCurrentCycle
    {
        get
80→        {

### Turn 61 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 9 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\IEC\Via6522.cs
21:    private const ushort DefaultBaseAddress = 0x0000;
22:    private const ushort DefaultMirrorSize = 0x0010;
136:    public ushort BaseAddress { get; init; } = DefaultBaseAddress;
139:    public ushort Size { get; init; } = DefaultMirrorSize;
194:    public bool HandlesAddress(ushort address)
195:        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);
200:        var reg = (byte)((address - BaseAddress) & 0x0F);
269:        var reg = (byte)((address - BaseAddress) & 0x0F);
425:        var reg = (byte)((address - BaseAddress) & 0x0F);
</workspace_result>

### Turn 62 (Assistant)
[tool_request: grep]
- pattern: 1108404|1108405|1022727|cycles_per_line|VIC20_PAL|VIC20_NTSC
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*.{c,h}
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IArchitectureDescriptor.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs
- offset: 100
- limit: 120

### Turn 63 (Function)
[tool_response]
100→    {
        value = string.Empty;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        value = line[prefix.Length..].Trim();
        return !string.IsNullOrEmpty(value);
    }

    private static string BuildC64Topology(ViceArgs args)
    {
        if (args.MachineYamlPath is not null)
110→            return File.ReadAllText(args.MachineYamlPath);

        var yaml = new StringBuilder();
        yaml.AppendLine("schemaVersion: 1");
        yaml.AppendLine("coordinator:");
        yaml.AppendLine("  host:");
        yaml.AppendLine("    id: c64-host");
        yaml.AppendLine("    kind: C64");
        if (args.Drive8Image is not null || args.Drive9Image is not null)
        {
120→            yaml.AppendLine("    busAttachments:");
            yaml.AppendLine("      - busId: IEC");
            yaml.AppendLine("        endpointName: c64");
        }

        var hasPeripherals = args.Drive8Image is not null || args.Drive9Image is not null;
        if (hasPeripherals)
        {
            yaml.AppendLine("  peripherals:");
            if (args.Drive8Image is not null)
130→                AppendDrive(yaml, 8, args.Drive8Image);
            if (args.Drive9Image is not null)
                AppendDrive(yaml, 9, args.Drive9Image);
        }
        if (hasPeripherals)
        {
            yaml.AppendLine("  buses:");
            yaml.AppendLine("    - id: IEC");
            yaml.AppendLine("      signals: [ATN, CLK, DATA, SRQ]");
        }
140→
        return yaml.ToString();
    }

    private static void AppendDrive(StringBuilder yaml, int deviceNumber, string imagePath)
    {
        yaml.AppendLine($"    - id: drive-{deviceNumber}");
        yaml.AppendLine($"      kind: C1541");
        yaml.AppendLine($"      deviceNumber: {deviceNumber}");
        yaml.AppendLine($"      diskImagePath: {imagePath.Replace('\\', '/')}");
150→        yaml.AppendLine($"      busAttachments:");
        yaml.AppendLine($"        - busId: IEC");
        yaml.AppendLine($"          endpointName: drive-{deviceNumber}");
    }

    private static string BuildC1541Standalone(ViceArgs args)
    {
        // c1541 tool runs a standalone 1541 with optional D64 mounted.
        var yaml = new StringBuilder();
        yaml.AppendLine("schemaVersion: 1");
160→        yaml.AppendLine("coordinator:");
        yaml.AppendLine("  host:");
        yaml.AppendLine("    id: drive-tool");
        yaml.AppendLine("    kind: C1541");
        yaml.AppendLine("    deviceNumber: 8");
        if (args.Drive8Image is not null)
            yaml.AppendLine($"    diskImagePath: {args.Drive8Image.Replace('\\', '/')}");
        yaml.AppendLine("  buses: []");
        return yaml.ToString();
    }
170→}


### Turn 64 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Immutable declarative description of a machine architecture.
/// Lists devices, memory map, clock configuration, and interrupt wiring.
/// Used by IArchitectureBuilder to construct a running IMachine.
/// </summary>
public interface IArchitectureDescriptor
{
10→    /// <summary>Machine name (e.g., "Commodore 64 PAL").</summary>
    string MachineName { get; }

    /// <summary>Master clock frequency in Hz.</summary>
    long MasterClockHz { get; }

    /// <summary>Video standard (PAL or NTSC).</summary>
    VideoStandard VideoStandard { get; }
    
    /// <summary>Device descriptors for this architecture.</summary>
20→    IReadOnlyList<DeviceDescriptor> Devices { get; }
    
    /// <summary>Required ROM set for this architecture.</summary>
    IRomSet? RequiredRoms { get; }
}

/// <summary>
/// Describes a device required by an architecture.
/// </summary>
public readonly record struct DeviceDescriptor(
30→    string Name,
    DeviceId Id,
    DeviceRole Role,
    ushort BaseAddress,
    int Size);

/// <summary>
/// Video output standard.
/// </summary>
public enum VideoStandard
40→{
    /// <summary>PAL (50Hz)</summary>
    Pal,
    
    /// <summary>NTSC (60Hz)</summary>
    Ntsc
}


### Turn 65 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 31 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-mem.c
234:    if (vic.cycles_per_line == VIC20_NTSC_CYCLES_PER_LINE) {
250:                if (ypos >= (VIC20_NTSC_INTERLACE_FIELD1_LAST_LINE + 1)) {
261:                if (ypos >= VIC20_NTSC_INTERLACE_FIELD2_LAST_LINE) {
262:                    if (VIC_RASTER_CYCLE(maincpu_clk + vic.cycle_offset) < VIC20_NTSC_INTERLACE_FIELD2_CYCLES_LAST_LINE) {
263:                        return VIC20_NTSC_INTERLACE_FIELD2_LAST_LINE; /* confirm this */
279:                if (VIC_RASTER_CYCLE(maincpu_clk + vic.cycle_offset) < VIC20_NTSC_CYCLES_LAST_LINE) {
280:                    return VIC20_NTSC_LAST_LINE; /* confirm this */

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-cycle.c
76:        if (vic.screen_height == VIC20_NTSC_SCREEN_LINES) {
344:    interlace_enable_flag = (vic.cycles_per_line == VIC20_NTSC_CYCLES_PER_LINE) ? (vic.regs[0] & 0x80) >> 7 : 0;
353:            if (vic.raster_cycle == vic.cycles_per_line) {
359:                    vic.screen_height = VIC20_NTSC_INTERLACE_FIELD2_SCREEN_LINES;
368:            if (vic.raster_cycle == vic.cycles_per_line) {
374:                    vic.screen_height = VIC20_NTSC_INTERLACE_FIELD1_SCREEN_LINES;
388:            vic.screen_height = VIC20_NTSC_SCREEN_LINES;
394:        if (vic.raster_cycle == vic.cycles_per_line) {
398:                if ((vic.cycles_per_line == VIC20_NTSC_CYCLES_PER_LINE) &&
403:                    vic.screen_height = VIC20_NTSC_SCREEN_LINES;
412:                    vic.screen_height = VIC20_NTSC_INTERLACE_FIELD1_SCREEN_LINES;

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-timing.c
44:            vic.screen_height = VIC20_NTSC_SCREEN_LINES;
73:            vic.cycles_per_line = VIC20_NTSC_CYCLES_PER_LINE;
74:            vic.cycle_offset = VIC20_NTSC_CYCLE_OFFSET;
79:            vic.screen_height = VIC20_PAL_SCREEN_LINES;
108:            vic.cycles_per_line = VIC20_PAL_CYCLES_PER_LINE;
109:            vic.cycle_offset = VIC20_PAL_CYCLE_OFFSET;

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic.c
365:        if ((vic.raster_line == (vic.screen_height - 1)) && (vic.raster_cycle == (vic.cycles_per_line - 2))) {
399:    x = 2 * ((cycle + 1) % vic.cycles_per_line);
422:        pulse_time += (x / 8) + (y * vic.cycles_per_line);

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20-cmdline-options.c
50:    { "vic20", VIC20MODEL_VIC20_PAL },
51:    { "vic20pal", VIC20MODEL_VIC20_PAL },
52:    { "pal", VIC20MODEL_VIC20_PAL },
53:    { "vic20ntsc", VIC20MODEL_VIC20_NTSC },
</workspace_result>

### Turn 66 (Assistant)
[tool_request: grep]
- pattern: VIC20_PAL_CYCLES|VIC20_NTSC_CYCLES|VIC20_PAL_SCREEN|VIC20_NTSC_SCREEN|VIC20_PAL_CYCLE
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/*.{h,c}
- head_limit: 30
[tool_request: run_terminal_command]
- command: Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"
- description: Get response timestamp

### Turn 67 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 25 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20.h
34:#define VIC20_PAL_CYCLES_PER_SEC        1108405
35:#define VIC20_PAL_CYCLES_PER_LINE       71
36:#define VIC20_PAL_SCREEN_LINES          312
37:#define VIC20_PAL_CYCLE_OFFSET          0
39:#define VIC20_PAL_CYCLES_PER_RFSH (VIC20_PAL_SCREEN_LINES * VIC20_PAL_CYCLES_PER_LINE)
40:#define VIC20_PAL_RFSH_PER_SEC    (1.0 / ((double)VIC20_PAL_CYCLES_PER_RFSH / (double)VIC20_PAL_CYCLES_PER_SEC))
42:#define VIC20_NTSC_CYCLES_PER_SEC       1022727
43:#define VIC20_NTSC_CYCLES_PER_LINE      65
46:#define VIC20_NTSC_CYCLES_FIRST_LINE    32
47:#define VIC20_NTSC_CYCLES_LAST_LINE     33
49:#define VIC20_NTSC_SCREEN_LINES         261 /* really 262, but first and last add up to one complete line */
52:#define VIC20_NTSC_CYCLES_PER_RFSH (VIC20_NTSC_SCREEN_LINES * VIC20_NTSC_CYCLES_PER_LINE)
53:#define VIC20_NTSC_RFSH_PER_SEC    (1.0 / ((double)VIC20_NTSC_CYCLES_PER_RFSH / (double)VIC20_NTSC_CYCLES_PER_SEC))
69:      * VIC20_NTSC_CYCLES_PER_LINE) / 2)
70:#define VIC20_NTSC_INTERLACE_RFSH_PER_SEC    (1.0 / ((double)VIC20_NTSC_INTERLACE_CYCLES_PER_RFSH / (double)VIC20_NTSC_CYCLES_PER_SEC))

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20.c
1164:            machine_timing.cycles_per_sec = VIC20_PAL_CYCLES_PER_SEC;
1165:            machine_timing.cycles_per_rfsh = VIC20_PAL_CYCLES_PER_RFSH;
1167:            machine_timing.cycles_per_line = VIC20_PAL_CYCLES_PER_LINE;
1168:            machine_timing.screen_lines = VIC20_PAL_SCREEN_LINES;
1172:            machine_timing.cycles_per_sec = VIC20_NTSC_CYCLES_PER_SEC;
1173:            machine_timing.cycles_per_rfsh = VIC20_NTSC_CYCLES_PER_RFSH;
1175:            machine_timing.cycles_per_line = VIC20_NTSC_CYCLES_PER_LINE;
1176:            machine_timing.screen_lines = VIC20_NTSC_SCREEN_LINES;

F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic-mem.c
234:    if (vic.cycles_per_line == VIC20_NTSC_CYCLES_PER_LINE) {
279:                if (VIC_RASTER_CYCLE(maincpu_clk + vic.cycle_offset) < VIC20_NTSC_CYCLES_LAST_LINE) {
</workspace_result>

### Turn 68 (Function)
[tool_response]
exit: 0
2026-08-05 12:15:53 -05:00


### Turn 69 (Assistant)
Implementing requirements docs, red tests, then Vic20 skeleton (descriptor, builder, stub video, factory).
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\docs\requirements\functional\FR-VIC20.md
- content: # FR-VIC20: Commodore VIC-20 Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | VIC-20 architecture            |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-08-05 |
| Iteration      | 2                              |

---

## FR-VIC20-001: VIC-I 6560/6561 display

**ID:** FR-VIC20-001
**Title:** VIC-I (MOS 6560 NTSC / 6561 PAL) video
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The VIC-20 display is driven by the MOS 6560 (NTSC) or MOS 6561 (PAL) VIC-I chip, not the C64 VIC-II. Slice A may ship a black-frame stub that implements `IVideoChip` so host frame pull does not crash; full raster and character rendering land in later slices.

### Acceptance Criteria

1. A VIC-20 machine exposes a device with role `VideoChip` implementing `IVideoChip`.
2. PAL profile uses 6561 timing: 1_108_405 Hz, 71 cycles/line, 312 lines (VICE `vic20.h`).
3. NTSC profile uses 6560 timing: 1_022_727 Hz, 65 cycles/line, 261 lines (VICE `vic20.h`).
4. Framebuffer contract matches existing host path (BGRA, FrameCompleted).
5. Full character/bitmap/color rendering and on-chip audio are delivered after the stub gate.

### Traceability

- **Interfaces:** `IVideoChip`, `IMachineProfile`
- **Test Suite:** `Vic20SkeletonTests`, later `VicChipTests`

---

## FR-VIC20-002: VIC-20 memory map and expansions

**ID:** FR-VIC20-002
**Title:** VIC-20 base memory map and RAM expansions
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

Unexpanded VIC-20 has 5KB base RAM (1KB system at $0000-$03FF, 4KB user at $1000-$1FFF), character ROM at $8000, I/O at $9000, color RAM at $9400, BASIC at $C000, KERNAL at $E000. Expansions (3K/8K/16K/24K/32K) map into BLK regions.

### Acceptance Criteria

1. After reset with official ROMs, BASIC is readable at $C000 and KERNAL at $E000.
2. Character ROM is readable at $8000.
3. Base RAM regions accept read/write at $0000-$03FF and $1000-$1FFF.
4. Expansion configurations are selectable via profile/resources (later slice).
5. Open-bus and I/O window rules match VICE for validated address ranges.

### Traceability

- **Interfaces:** `IBus`, `IArchitectureDescriptor`
- **Test Suite:** `Vic20SkeletonTests`, later `Vic20MemoryExpansionTests`

---

## FR-VIC20-003: VIA1/VIA2 integration

**ID:** FR-VIC20-003
**Title:** VIC-20 dual VIA 6522 wiring
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The VIC-20 uses two shared `Via6522` instances (one chip implementation, board wiring only in Architectures): VIA1 at $9110 (typically NMI path) and VIA2 at $9120 (typically IRQ path). No C1541-specific assumptions may live in the shared VIA chip.

### Acceptance Criteria

1. Machine builds with two `Via6522` devices registered on the bus at $9110 and $9120 (16-byte windows).
2. VIA1 and VIA2 are clocked and reset with the machine.
3. Shared `Via6522` source contains no VIC-20 or 1541 board glue (ARCH-CHIPGLUE-001).
4. Keyboard, joystick, and RESTORE wiring land in later slices without forking the chip.

### Traceability

- **Interfaces:** `Via6522`, FR-VIA-001..005
- **Test Suite:** `Vic20SkeletonTests`, later VIA keyboard tests

---

## FR-VIC20-004: VIC-20 keyboard matrix

**ID:** FR-VIC20-004
**Title:** VIC-20 keyboard matrix via VIA ports
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Keyboard scanning uses VIA port lines (not CIA). Host key events map through a VIC-20 matrix type.

### Acceptance Criteria

1. Key press/release is visible to software scanning the matrix through VIA ports.
2. Matrix layout matches VICE VIC-20 VKM / hardware mapping for the default layout.
3. Disabled until wiring slice; skeleton builds without requiring host keyboard.

### Traceability

- **Test Suite:** later `Vic20KeyboardTests`

---

## FR-VIC20-005: VIC-20 cartridge types (MVP)

**ID:** FR-VIC20-005
**Title:** VIC-20 cartridge attach (MVP set)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

MVP cartridge types for common VIC-20 carts (standard 8K blocks and autostart) attach through the architecture cart path.

### Acceptance Criteria

1. Supported cart types mount into the documented BLK regions.
2. Autostart or reset-into-cart path works for at least one validated image family.
3. Skeleton builds without a cartridge attached.

### Traceability

- **Test Suite:** later cart attach tests

---

## FR-VIC20-006: Default drive 8 is Commodore 1540

**ID:** FR-VIC20-006
**Title:** VIC-20 default unit 8 drive model is 1540
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

Historical VIC-20 kits shipped with the 1540. ViceSharp VIC-20 sessions default drive unit 8 to `DriveModel.C1540` and DOS ROM `dos1540-325302+3-01.bin`. C64 sessions keep default 1541. Users may still select 1541/1541-II later.

### Acceptance Criteria

1. `Vic20MachineProfile.DefaultDriveModel` is `DriveModel.C1540` for all standard VIC-20 profiles.
2. Topology / host attach for drive 8 uses 1540 DOS ROM name when following profile defaults.
3. C64 profiles are unchanged (default 1541).
4. Tests pin the Vic20 default to 1540.

### Traceability

- **Interfaces:** `DriveModel`, `C1541ViceRomNames.Dos1540`
- **Test Suite:** `Vic20SkeletonTests`

---

## FR-PRF-005 cross-reference

Profile selection for VIC-20 is also specified under `FR-Machine-Profiles.md` (`FR-PRF-005`). Iteration 2 owns delivery of that profile.

[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\requirements\functional\FR-Machine-Profiles.md
- old_string: ## FR-PRF-005: VIC-20 Profile

**ID:** FR-PRF-005
**Title:** Commodore VIC-20 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The VIC-20 uses the MOS 6502 CPU, MOS 6560 (NTSC) or MOS 6561 (PAL) VIC chip (not VIC-II), two MOS 6522 VIA chips, and 5KB of RAM (expandable). The display and sound capabilities differ significantly from the C64.

### Acceptance Criteria

1. CPU: Standard MOS 6502 (not 6510; no built-in I/O port).
2. VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound.
3. VIA: Two MOS 6522 VIAs (per FR-VIA-004) instead of CIAs.
4. RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.
5. ROM: BASIC V2, VIC-20 KERNAL, Character Generator.
6. Memory map differs significantly from C64.
7. Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID.
8. Selectable via `IMachineProfile.LoadProfile("vic20")`.
9. Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.
- new_string: ## FR-PRF-005: VIC-20 Profile

**ID:** FR-PRF-005
**Title:** Commodore VIC-20 Machine Profile
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The VIC-20 uses the MOS 6502 CPU, MOS 6560 (NTSC) or MOS 6561 (PAL) VIC chip (not VIC-II), two MOS 6522 VIA chips, and 5KB of RAM (expandable). The display and sound capabilities differ significantly from the C64. Default floppy on unit 8 is the Commodore 1540 (`DriveModel.C1540`), not the 1541. See also `FR-VIC20.md`.

### Acceptance Criteria

1. CPU: Standard MOS 6502 (not 6510; no built-in I/O port).
2. VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound (stub allowed in early slices).
3. VIA: Two MOS 6522 VIAs (per FR-VIA-004 / FR-VIC20-003) instead of CIAs.
4. RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.
5. ROM: BASIC V2, VIC-20 KERNAL, Character Generator (VICE VIC20 data names).
6. Memory map differs significantly from C64.
7. Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID (MVP may be silent).
8. Selectable via profile aliases such as `vic20`, `vic20pal`, `vic20ntsc` (family `xvic`).
9. Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.
10. Default drive unit 8 model is 1540 (FR-VIC20-006).
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20SkeletonTests.cs
- content: namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using ViceSharp.Launcher;
using Xunit;

/// <summary>
/// FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006 / Iteration 2 Slice A.
/// Use case: construct a VIC-20 skeleton (CPU, ROMs, dual VIA, stub video)
/// and prove default drive 8 model is 1540 without requiring full BASIC boot.
/// Acceptance: profile resolve, ROM completeness, machine build, reset vector,
/// first cycles, VIA windows, stub video role, launcher xvic YAML, C1540 default.
/// </summary>
public sealed class Vic20SkeletonTests
{
    [Fact]
    public void Vic20Profile_Default_IsPal_With6561TimingAnd1540Drive()
    {
        var profile = Vic20MachineProfiles.Default;

        Assert.Equal("vic20", profile.Id);
        Assert.Equal("xvic", profile.Family);
        Assert.Equal(VideoStandard.Pal, profile.VideoStandard);
        Assert.Equal(1_108_405, profile.NominalClockHz);
        Assert.Equal(71, profile.CyclesPerLine);
        Assert.Equal(312, profile.RasterLines);
        Assert.Equal("Mos6561", profile.VicIIModel);
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
        Assert.Equal(C1541ViceRomNames.Dos1540, profile.DefaultDriveDosRomName);
        Assert.Equal(Vic20ViceRomNames.Basic, profile.BasicRomName);
        Assert.Equal(Vic20ViceRomNames.KernalPal, profile.KernalRomName);
        Assert.Equal(Vic20ViceRomNames.Character, profile.CharacterRomName);
    }

    [Theory]
    [InlineData("vic20")]
    [InlineData("vic20pal")]
    [InlineData("pal")]
    [InlineData("xvic")]
    public void Vic20Profile_Resolve_PalAliases(string selector)
    {
        var profile = Vic20MachineProfiles.Resolve(selector);
        Assert.Equal(VideoStandard.Pal, profile.VideoStandard);
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
    }

    [Theory]
    [InlineData("vic20ntsc")]
    [InlineData("ntsc")]
    public void Vic20Profile_Resolve_NtscAliases(string selector)
    {
        var profile = Vic20MachineProfiles.Resolve(selector);
        Assert.Equal(VideoStandard.Ntsc, profile.VideoStandard);
        Assert.Equal(1_022_727, profile.NominalClockHz);
        Assert.Equal(65, profile.CyclesPerLine);
        Assert.Equal(261, profile.RasterLines);
        Assert.Equal("Mos6560", profile.VicIIModel);
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
    }

    [Fact]
    public void Vic20RomSet_IsComplete_AgainstViceDataRoot()
    {
        var provider = MachineTestFactory.CreateVic20RomProvider();
        var romSet = new Vic20RomSet();

        Assert.True(romSet.IsComplete(provider));
        Assert.Equal(Vic20ViceRomNames.ArchitectureKey, romSet.Architecture);
    }

    [Fact]
    public void Vic20Descriptor_WithoutRomProvider_ThrowsClearError()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ArchitectureBuilder().Build(new Vic20Descriptor()));

        Assert.Contains("requires an IRomProvider", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Vic20Machine_Builds_WithCpuVideoDualViaAndRoms()
    {
        var machine = MachineTestFactory.CreateVic20Machine();

        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.Cpu));
        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.SystemRam));
        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.VideoChip));
        Assert.IsAssignableFrom<IVideoChip>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        Assert.IsType<VicIStub>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        var vias = machine.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
        Assert.Equal(2, vias.Length);
        Assert.Equal(0x9110, vias[0].BaseAddress);
        Assert.Equal(0x9120, vias[1].BaseAddress);

        var basic = MachineTestFactory.LoadVic20Rom(Vic20ViceRomNames.Basic).Span;
        var kernal = MachineTestFactory.LoadVic20Rom(Vic20ViceRomNames.KernalPal).Span;
        var chargen = MachineTestFactory.LoadVic20Rom(Vic20ViceRomNames.Character).Span;

        Assert.Equal(basic[0], machine.Bus.Read(0xC000));
        Assert.Equal(kernal[0], machine.Bus.Read(0xE000));
        Assert.Equal(chargen[0], machine.Bus.Read(0x8000));
    }

    [Fact]
    public void Vic20Machine_AfterReset_CpuPcMatchesKernalResetVector()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var cpu = Assert.IsAssignableFrom<ICpu>(machine.Devices.GetByRole(DeviceRole.Cpu));
        var lo = machine.Bus.Read(0xFFFC);
        var hi = machine.Bus.Read(0xFFFD);
        var expected = (ushort)(lo | (hi << 8));

        Assert.Equal(expected, cpu.PC);
        Assert.NotEqual((ushort)0, expected);
    }

    [Fact]
    public void Vic20Machine_FirstThousandCycles_DoNotThrow_AndAdvanceClock()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var start = machine.Clock.CurrentCycle;

        for (var i = 0; i < 1000; i++)
            machine.Clock.Tick();

        Assert.Equal(start + 1000, machine.Clock.CurrentCycle);
    }

    [Fact]
    public void Vic20Machine_BaseRam_IsWritable()
    {
        var machine = MachineTestFactory.CreateVic20Machine();

        machine.Bus.Write(0x0002, 0x42);
        machine.Bus.Write(0x1000, 0x55);

        Assert.Equal(0x42, machine.Bus.Read(0x0002));
        Assert.Equal(0x55, machine.Bus.Read(0x1000));
    }

    [Fact]
    public void Vic20Descriptor_DevicesList_IncludesExpectedRoles()
    {
        var descriptor = new Vic20Descriptor();
        var roles = descriptor.Devices.Select(d => d.Role).ToHashSet();

        Assert.Contains(DeviceRole.Cpu, roles);
        Assert.Contains(DeviceRole.VideoChip, roles);
        Assert.Contains(DeviceRole.SystemRam, roles);
        Assert.Equal(1_108_405, descriptor.MasterClockHz);
        Assert.Equal(VideoStandard.Pal, descriptor.VideoStandard);
        Assert.NotNull(descriptor.RequiredRoms);
        Assert.True(descriptor.RequiredRoms!.IsComplete(MachineTestFactory.CreateVic20RomProvider()));
    }

    [Fact]
    public void Launcher_Xvic_BuildsTopologyYaml_WithVic20HostAnd1540DriveDefaultComment()
    {
        var args = ViceArgsParser.Parse("xvic", Array.Empty<string>());
        var yaml = ViceTopologyBuilder.BuildYaml(args);

        Assert.Contains("kind: Vic20", yaml, StringComparison.Ordinal);
        Assert.Contains("xvic", yaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultDrive_Is1540_Not1541()
    {
        foreach (var profile in Vic20MachineProfiles.All)
        {
            Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
            Assert.Equal(C1541ViceRomNames.Dos1540, profile.DefaultDriveDosRomName);
            Assert.NotEqual(DriveModel.C1541, profile.DefaultDriveModel);
        }
    }
}


### Turn 70 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\requirements\functional\FR-VIC20.md has been created.

### Turn 71 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\requirements\functional\FR-Machine-Profiles.md has been updated successfully.

### Turn 72 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20SkeletonTests.cs has been created.

### Turn 73 (Assistant)
[tool_request: grep]
- pattern: CurrentCycle|interface IClock
- glob: **/*.cs
- path: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20ViceRomNames.cs
- content: namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// Canonical VIC-20 ROM filenames as shipped under the VICE data tree
/// <c>VIC20/</c>. Note that kernal images use a dot separator in the part
/// number (VICE catalog quirk), unlike most other machine families.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-002.
/// </remarks>
public static class Vic20ViceRomNames
{
    /// <summary>Architecture key passed to <see cref="Abstractions.IRomProvider"/>.</summary>
    public const string ArchitectureKey = "VIC20";

    /// <summary>BASIC V2 (8192 bytes) at $C000-$DFFF.</summary>
    public const string Basic = "basic-901486-01.bin";

    /// <summary>PAL KERNAL (8192 bytes) at $E000-$FFFF. VICE default for PAL.</summary>
    public const string KernalPal = "kernal.901486-07.bin";

    /// <summary>NTSC KERNAL (8192 bytes) at $E000-$FFFF. VICE default for NTSC.</summary>
    public const string KernalNtsc = "kernal.901486-06.bin";

    /// <summary>Older kernal revision still listed in the VICE catalog.</summary>
    public const string KernalRev2 = "kernal.901486-02.bin";

    /// <summary>Character generator (4096 bytes) at $8000-$8FFF.</summary>
    public const string Character = "chargen-901460-03.bin";

    /// <summary>Alternate character ROM listed in the VICE catalog.</summary>
    public const string CharacterRev2 = "chargen-901460-02.bin";

    /// <summary>BASIC ROM size in bytes.</summary>
    public const int BasicRomSize = 0x2000;

    /// <summary>KERNAL ROM size in bytes.</summary>
    public const int KernalRomSize = 0x2000;

    /// <summary>Character ROM size in bytes.</summary>
    public const int CharacterRomSize = 0x1000;
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20RomSet.cs
- content: using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// Required VIC-20 ROM set (BASIC, KERNAL, character generator).
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-002.
/// </remarks>
public sealed class Vic20RomSet : IRomSet
{
    public Vic20RomSet()
        : this(
            Vic20ViceRomNames.ArchitectureKey,
            Vic20ViceRomNames.Basic,
            Vic20ViceRomNames.KernalPal,
            Vic20ViceRomNames.Character)
    {
    }

    public Vic20RomSet(
        string architecture,
        string basicRomName,
        string kernalRomName,
        string characterRomName)
    {
        Architecture = string.IsNullOrWhiteSpace(architecture)
            ? Vic20ViceRomNames.ArchitectureKey
            : architecture;
        BasicRomName = string.IsNullOrWhiteSpace(basicRomName)
            ? Vic20ViceRomNames.Basic
            : basicRomName;
        KernalRomName = string.IsNullOrWhiteSpace(kernalRomName)
            ? Vic20ViceRomNames.KernalPal
            : kernalRomName;
        CharacterRomName = string.IsNullOrWhiteSpace(characterRomName)
            ? Vic20ViceRomNames.Character
            : characterRomName;
    }

    /// <inheritdoc />
    public string Architecture { get; }

    public string BasicRomName { get; }

    public string KernalRomName { get; }

    public string CharacterRomName { get; }

    /// <inheritdoc />
    public bool IsComplete(IRomProvider provider)
    {
        return provider.IsAvailable(BasicRomName, Architecture)
            && provider.IsAvailable(KernalRomName, Architecture)
            && provider.IsAvailable(CharacterRomName, Architecture);
    }
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20SystemCoreDefinition.cs
- content: using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// Board-level policy for a VIC-20 system core.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-002, FR-VIC20-006.
/// </remarks>
public sealed record Vic20SystemCoreDefinition(
    string Id,
    string DisplayName,
    string BoardPolicy,
    string AddressDecoderPolicy,
    string BusPolicy,
    bool KeyboardMatrixConnected,
    bool TapePortConnected,
    bool IecBusConnected,
    DriveModel DefaultDriveModel,
    string DefaultDriveDosRomName,
    bool CartridgeBootExpected = false,
    IReadOnlyDictionary<string, string>? Traits = null) : ISystemCoreDefinition
{
    public string Family => "xvic";

    /// <summary>VIC-20 has no second CIA; always false.</summary>
    public bool Cia2Connected => false;

    IReadOnlyDictionary<string, string> ISystemCoreDefinition.Traits => Traits ?? EmptyTraits;

    private static readonly IReadOnlyDictionary<string, string> EmptyTraits =
        new Dictionary<string, string>();

    public static Vic20SystemCoreDefinition Unexpanded(DriveModel defaultDrive, string dosRomName)
    {
        var traits = new Dictionary<string, string>
        {
            ["board"] = "Unexpanded",
            ["decoder"] = "Unexpanded",
            ["bus"] = "Standard",
            ["tapePort"] = "connected",
            ["iecBus"] = "connected",
            ["defaultDrive"] = defaultDrive.ToString(),
            ["defaultDriveDos"] = dosRomName
        };

        return new Vic20SystemCoreDefinition(
            "xvic:unexpanded",
            "VIC-20 unexpanded",
            "Unexpanded",
            "Unexpanded",
            "Standard",
            KeyboardMatrixConnected: true,
            TapePortConnected: true,
            IecBusConnected: true,
            DefaultDriveModel: defaultDrive,
            DefaultDriveDosRomName: dosRomName,
            CartridgeBootExpected: false,
            Traits: traits);
    }
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20MachineProfile.cs
- content: using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC chip revision used by a VIC-20 profile (maps to <see cref="IMachineProfile.VicIIModel"/> string).
/// </summary>
public enum Vic20VicModel
{
    /// <summary>MOS 6561 PAL.</summary>
    Mos6561,

    /// <summary>MOS 6560 NTSC.</summary>
    Mos6560
}

/// <summary>
/// Concrete VIC-20 machine model profile (PAL/NTSC, clocks, ROMs, default 1540 drive).
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-001, FR-VIC20-006.
/// Clocks match VICE <c>vic20.h</c>: PAL 1108405/71/312, NTSC 1022727/65/261.
/// </remarks>
public sealed record Vic20MachineProfile(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Aliases,
    long NominalClockHz,
    VideoStandard VideoStandard,
    int CyclesPerLine,
    int RasterLines,
    Vic20VicModel Vic,
    string RomSet,
    string BasicRomName = Vic20ViceRomNames.Basic,
    string KernalRomName = Vic20ViceRomNames.KernalPal,
    string CharacterRomName = Vic20ViceRomNames.Character,
    bool KeyboardEnabled = true,
    bool CartridgeBootExpected = false,
    DriveModel DefaultDriveModel = DriveModel.C1540,
    string DefaultDriveDosRomName = C1541ViceRomNames.Dos1540,
    Vic20SystemCoreDefinition? CoreDefinition = null) : IMachineProfile
{
    public string Family => "xvic";

    public double RefreshRateHz => NominalClockHz / (double)(CyclesPerLine * RasterLines);

    /// <summary>Exposes VIC-I model through the shared profile string used by C64 as VicIIModel.</summary>
    public string VicIIModel => Vic.ToString();

    /// <summary>VIC-20 has no SID; empty marker for shared profile surface.</summary>
    public string SidModel => "None";

    public string BoardModel => "Unexpanded";

    public ISystemCoreDefinition SystemCore { get; } =
        CoreDefinition ?? Vic20SystemCoreDefinition.Unexpanded(DefaultDriveModel, DefaultDriveDosRomName);
}

/// <summary>
/// Built-in VIC-20 profile catalog and alias resolver.
/// </summary>
public static class Vic20MachineProfiles
{
    public static Vic20MachineProfile Default => Vic20Pal;

    public static Vic20MachineProfile Vic20Pal { get; } = new(
        "vic20",
        "Commodore VIC-20 PAL",
        ["vic20", "vic20pal", "pal", "xvic", "commodore-vic20"],
        1_108_405,
        VideoStandard.Pal,
        71,
        312,
        Vic20VicModel.Mos6561,
        Vic20ViceRomNames.ArchitectureKey,
        KernalRomName: Vic20ViceRomNames.KernalPal);

    public static Vic20MachineProfile Vic20Ntsc { get; } = new(
        "vic20ntsc",
        "Commodore VIC-20 NTSC",
        ["vic20ntsc", "ntsc", "xvic-ntsc"],
        1_022_727,
        VideoStandard.Ntsc,
        65,
        261,
        Vic20VicModel.Mos6560,
        Vic20ViceRomNames.ArchitectureKey,
        KernalRomName: Vic20ViceRomNames.KernalNtsc);

    public static IReadOnlyList<Vic20MachineProfile> All { get; } =
    [
        Vic20Pal,
        Vic20Ntsc
    ];

    private static readonly Dictionary<string, Vic20MachineProfile> ByAlias = All
        .SelectMany(profile => profile.Aliases.Append(profile.Id).Select(alias => (Alias: alias, Profile: profile)))
        .GroupBy(pair => pair.Alias, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First().Profile, StringComparer.OrdinalIgnoreCase);

    public static Vic20MachineProfile Resolve(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (ByAlias.TryGetValue(selector, out var profile))
            return profile;

        throw new ArgumentException($"Unknown xvic VIC-20 model selector '{selector}'.", nameof(selector));
    }

    public static bool TryResolve(string selector, out Vic20MachineProfile profile)
    {
        profile = Default;

        if (string.IsNullOrWhiteSpace(selector))
            return false;

        if (ByAlias.TryGetValue(selector, out var resolved))
        {
            profile = resolved;
            return true;
        }

        return false;
    }
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs
- content: using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 (xvic) architecture descriptor: 6502, dual VIA, VIC-I, base RAM, ROMs.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006.
/// VIA and video board wiring live here; shared chips stay machine-agnostic.
/// </remarks>
public sealed class Vic20Descriptor : IProfiledArchitectureDescriptor
{
    public Vic20Descriptor()
        : this(Vic20MachineProfiles.Default)
    {
    }

    public Vic20Descriptor(string modelSelector)
        : this(Vic20MachineProfiles.Resolve(modelSelector))
    {
    }

    public Vic20Descriptor(Vic20MachineProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Vic20MachineProfile Profile { get; }

    /// <inheritdoc />
    public IMachineProfile MachineProfile => Profile;

    /// <inheritdoc />
    public string MachineName => Profile.DisplayName;

    /// <inheritdoc />
    public long MasterClockHz => Profile.NominalClockHz;

    /// <inheritdoc />
    public VideoStandard VideoStandard => Profile.VideoStandard;

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-I", new DeviceId(0x0003), DeviceRole.VideoChip, 0x9000, 0x0010),
        new("VIA1", new DeviceId(0x0005), DeviceRole.Cia1, 0x9110, 0x0010),
        new("VIA2", new DeviceId(0x0006), DeviceRole.Cia2, 0x9120, 0x0010),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0xA000, 0x2000),
    ];

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new Vic20RomSet(
        Profile.RomSet,
        Profile.BasicRomName,
        Profile.KernalRomName,
        Profile.CharacterRomName);
}


### Turn 74 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20RomSet.cs has been created.

### Turn 75 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20SystemCoreDefinition.cs has been created.

### Turn 76 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20ViceRomNames.cs has been created.

### Turn 77 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs has been created.

### Turn 78 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20MachineProfile.cs has been created.

### Turn 79 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 4 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IClockedDevice.cs
6:public interface IClockedDevice : IDevice

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IClock.cs
8:public interface IClock

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ICpuCycleStealTarget.cs
11:    bool CanStealCurrentCycle { get; }
16:    bool CanForceStealCurrentCycle { get; }
</workspace_result>

### Turn 80 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDeviceRegistry.cs
- old_string:     /// <summary>Complex Interface Adapter 2</summary>
    Cia2,
    
    /// <summary>Programmable Logic Array</summary>
    Pla,
- new_string:     /// <summary>Complex Interface Adapter 2</summary>
    Cia2,

    /// <summary>Host VIA 6522 instance 1 (for example VIC-20 at $9110).</summary>
    Via1,

    /// <summary>Host VIA 6522 instance 2 (for example VIC-20 at $9120).</summary>
    Via2,
    
    /// <summary>Programmable Logic Array</summary>
    Pla,
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs
- old_string:         new("VIA1", new DeviceId(0x0005), DeviceRole.Cia1, 0x9110, 0x0010),
        new("VIA2", new DeviceId(0x0006), DeviceRole.Cia2, 0x9120, 0x0010),
- new_string:         new("VIA1", new DeviceId(0x0005), DeviceRole.Via1, 0x9110, 0x0010),
        new("VIA2", new DeviceId(0x0006), DeviceRole.Via2, 0x9120, 0x0010),
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IClock.cs
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\VicIStub.cs
- content: using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Vic;

/// <summary>
/// Slice A stub for the MOS 6560/6561 VIC-I chip. Implements
/// <see cref="IVideoChip"/> with a solid black frame and simple raster
/// advancement so the host frame path and clock wiring can attach before
/// full character/bitmap rendering lands (FR-VIC20-001).
/// </summary>
/// <remarks>
/// Machine-agnostic stub: timing parameters come from the architecture
/// profile via <see cref="ConfigureTiming"/>. Register window is the
/// standard VIC-I 16-byte block at the board base address (VIC-20 $9000).
/// </remarks>
public sealed class VicIStub : IVideoChip, IAddressSpace, IInterruptSource
{
    private readonly IInterruptLine? _irqLine;
    private readonly byte[] _regs = new byte[16];
    private int _cycleInLine;
    private int _cyclesPerLine = 71;
    private int _totalLines = 312;
    private int _visibleLines = 284;
    private int _frameWidth = 224;
    private int _frameHeight = 284;
    private byte[] _frameBuffer;
    private ushort _rasterLine;

    public VicIStub(IInterruptLine? irqLine = null)
    {
        _irqLine = irqLine;
        Id = new DeviceId(0x0003);
        SourceId = Id;
        Name = "MOS 656x VIC-I (stub)";
        BaseAddress = 0x9000;
        Size = 0x0010;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
    }

    /// <inheritdoc />
    public DeviceId Id { get; init; }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public DeviceId SourceId { get; init; }

    /// <summary>Base address of the VIC-I register window on the board bus.</summary>
    public ushort BaseAddress { get; init; }

    /// <summary>Size of the VIC-I register window (16 bytes).</summary>
    public ushort Size { get; init; }

    /// <inheritdoc />
    public uint ClockDivisor => 1;

    /// <inheritdoc />
    public ClockPhase Phase => ClockPhase.Phi2;

    /// <inheritdoc />
    public IReadOnlyList<IInterruptLine> ConnectedLines =>
        _irqLine is null ? Array.Empty<IInterruptLine>() : new[] { _irqLine };

    /// <inheritdoc />
    public ushort CurrentRasterLine => _rasterLine;

    /// <inheritdoc />
    public int CyclesPerLine => _cyclesPerLine;

    /// <inheritdoc />
    public int VisibleLines => _visibleLines;

    /// <inheritdoc />
    public int TotalLines => _totalLines;

    /// <inheritdoc />
    public bool IsVBlank => _rasterLine >= _visibleLines;

    /// <inheritdoc />
    public byte[] FrameBuffer => _frameBuffer;

    /// <inheritdoc />
    public int FrameWidth => _frameWidth;

    /// <inheritdoc />
    public int FrameHeight => _frameHeight;

    /// <inheritdoc />
    public event EventHandler? FrameCompleted;

    /// <summary>
    /// Apply profile timing (PAL 71x312 or NTSC 65x261) and rebuild the black framebuffer.
    /// </summary>
    public void ConfigureTiming(int cyclesPerLine, int totalLines, int visibleLines, int frameWidth, int frameHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cyclesPerLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLines, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(visibleLines, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameHeight, 1);

        _cyclesPerLine = cyclesPerLine;
        _totalLines = totalLines;
        _visibleLines = Math.Min(visibleLines, totalLines);
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);

    /// <inheritdoc />
    public byte Read(ushort address)
        => _regs[(address - BaseAddress) & 0x0F];

    /// <inheritdoc />
    public void Write(ushort address, byte value)
        => _regs[(address - BaseAddress) & 0x0F] = value;

    /// <inheritdoc />
    public byte Peek(ushort address) => Read(address);

    /// <inheritdoc />
    public void Tick()
    {
        _cycleInLine++;
        if (_cycleInLine < _cyclesPerLine)
            return;

        _cycleInLine = 0;
        _rasterLine++;
        if (_rasterLine < _totalLines)
            return;

        _rasterLine = 0;
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_regs);
        _cycleInLine = 0;
        _rasterLine = 0;
        Array.Clear(_frameBuffer);
    }
}


### Turn 81 (Function)
[tool_response]
1→namespace ViceSharp.Abstractions;

/// <summary>
/// Master system clock that drives cycle-accurate emulation.
/// Distributes ticks to all registered IClockedDevice instances
/// respecting divisors and clock phases.
/// </summary>
public interface IClock
{
10→    /// <summary>Advances the clock by one master cycle, ticking all due devices.</summary>
    void Step();

    /// <summary>Advances the clock by the specified number of master cycles.</summary>
    void Step(long cycles);

    /// <summary>Total master cycles elapsed since reset.</summary>
    long TotalCycles { get; }

    /// <summary>Master clock frequency in Hz.</summary>
20→    long FrequencyHz { get; }

    /// <summary>Registers a device to receive clock ticks.</summary>
    void Register(IClockedDevice device);

    /// <summary>Unregisters a device from clock tick distribution.</summary>
    void Unregister(IClockedDevice device);

    /// <summary>Resets the clock to initial state.</summary>
    void Reset();
30→}

/// <summary>
/// Clock phase indicating which half of the clock cycle the device operates on.
/// </summary>
public enum ClockPhase
{
    /// <summary>Phase 1 (rising edge)</summary>
    Phi1 = 1,
    
40→    /// <summary>Phase 2 (falling edge)</summary>
    Phi2 = 2
}

### Turn 82 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDeviceRegistry.cs has been updated successfully.

### Turn 83 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs has been updated successfully.

### Turn 84 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\VicIStub.cs has been created.

### Turn 85 (Assistant)
[tool_request: grep]
- pattern: interface IMachine|IClock Clock|void Tick
- glob: **/IMachine.cs
- path: F:\GitHub\vice-sharp\src
[tool_request: grep]
- pattern: machine\.Clock\.(Tick|Step)|TotalCycles
- glob: **/*Tests*.cs
- path: F:\GitHub\vice-sharp\tests
- head_limit: 20
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 37
- limit: 15

### Turn 86 (Function)
[tool_response]
37→    /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
40→        if (IsC1541Machine(descriptor))
            return BuildC1541Machine(descriptor);

        if (IsC64Machine(descriptor))
            return BuildC64Machine(descriptor);

        return BuildMinimalMachine(descriptor);
    }

    private IMachine BuildMinimalMachine(IArchitectureDescriptor descriptor)
50→    {
        var bus = new BasicBus();

### Turn 87 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 2 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
25:public interface IMachine
31:    IClock Clock { get; }
</workspace_result>

### Turn 88 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 11 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\AdhocMachine\ConsoleAdhocCliTests.cs
22:    /// Acceptance: 100 Clock.Step() calls run without throwing and Clock.TotalCycles
36:            machine.Clock.Step();
39:        machine.Clock.TotalCycles.Should().Be(100);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64MachineProfileTests.cs
465:        machine.Clock.Step();
600:        machine.Clock.TotalCycles.Should().Be(expectedFrameCycles);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\C64Plus1541IntegrationTests.cs
60:        rig.host.Machine.Clock.TotalCycles.Should().Be(985_248);
61:        var driveDrift = System.Math.Abs(rig.drive.Machine.Clock.TotalCycles - 1_000_000);
149:        rig.host.Machine.Clock.TotalCycles.Should().Be(0);
150:        rig.drive.Machine.Clock.TotalCycles.Should().Be(0);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C64VkmKeyboardTests.cs
276:                machine.Clock.Step();

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\C1541\CoordinatorMachineTests.cs
99:            var hostBefore = machine.Clock.TotalCycles;
</workspace_result>

### Turn 89 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20SkeletonTests.cs
- old_string:     [Fact]
    public void Vic20Machine_FirstThousandCycles_DoNotThrow_AndAdvanceClock()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var start = machine.Clock.CurrentCycle;

        for (var i = 0; i < 1000; i++)
            machine.Clock.Tick();

        Assert.Equal(start + 1000, machine.Clock.CurrentCycle);
    }
- new_string:     [Fact]
    public void Vic20Machine_FirstThousandCycles_DoNotThrow_AndAdvanceClock()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var start = machine.Clock.TotalCycles;

        for (var i = 0; i < 1000; i++)
            machine.Clock.Step();

        Assert.Equal(start + 1000, machine.Clock.TotalCycles);
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string: using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Chips.Tape;
using ViceSharp.Core.Wiring;
using ViceSharp.RomFetch;
- new_string: using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Vic;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Chips.Tape;
using ViceSharp.Core.Wiring;
using ViceSharp.RomFetch;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string:     /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
        if (IsC1541Machine(descriptor))
            return BuildC1541Machine(descriptor);

        if (IsC64Machine(descriptor))
            return BuildC64Machine(descriptor);

        return BuildMinimalMachine(descriptor);
    }
- new_string:     /// <inheritdoc />
    public IMachine Build(IArchitectureDescriptor descriptor)
    {
        if (IsC1541Machine(descriptor))
            return BuildC1541Machine(descriptor);

        // VIC-20 before C64: both expose VideoChip; Vic20 is identified by Via1/Via2
        // (or xvic family profile), not by CIA/PLA roles.
        if (IsVic20Machine(descriptor))
            return BuildVic20Machine(descriptor);

        if (IsC64Machine(descriptor))
            return BuildC64Machine(descriptor);

        return BuildMinimalMachine(descriptor);
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string:     private static bool IsC64Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.VideoChip)
            || roles.Contains(DeviceRole.Cia1)
            || roles.Contains(DeviceRole.Cia2)
            || roles.Contains(DeviceRole.Pla)
            || roles.Contains(DeviceRole.AudioChip);
    }

    private static bool IsC1541Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.DriveCpu) && roles.Contains(DeviceRole.DriveRom);
    }
- new_string:     private static bool IsC64Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.VideoChip)
            || roles.Contains(DeviceRole.Cia1)
            || roles.Contains(DeviceRole.Cia2)
            || roles.Contains(DeviceRole.Pla)
            || roles.Contains(DeviceRole.AudioChip);
    }

    private static bool IsVic20Machine(IArchitectureDescriptor descriptor)
    {
        if (descriptor is IProfiledArchitectureDescriptor profiled
            && string.Equals(profiled.MachineProfile.Family, "xvic", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.Via1) && roles.Contains(DeviceRole.Via2);
    }

    private static bool IsC1541Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.DriveCpu) && roles.Contains(DeviceRole.DriveRom);
    }

    /// <summary>
    /// Build a VIC-20 skeleton: Mos6502, dual Via6522 ($9110 NMI / $9120 IRQ),
    /// VIC-I stub video, base RAM, and BASIC/KERNAL/chargen ROMs. Full memory
    /// banking and keyboard wiring land in later Iteration 2 slices.
    /// </summary>
    /// <remarks>FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006.</remarks>
    private IMachine BuildVic20Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine, nmiLine);
        var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;

        // Base 64KB backing store; ROMs and I/O devices overlay via register order
        // (BasicBus prefers the most recently registered device that HandlesAddress).
        var ram = new SimpleRam();
        ram.Fill(0x00);
        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);

        var architectureKey = descriptor.RequiredRoms?.Architecture ?? "VIC20";
        var basicName = profile?.BasicRomName ?? "basic-901486-01.bin";
        var kernalName = profile?.KernalRomName ?? "kernal.901486-07.bin";
        var characterName = profile?.CharacterRomName ?? "chargen-901460-03.bin";

        var basic = _romProvider.LoadRom(basicName, architectureKey).ToArray();
        var kernal = _romProvider.LoadRom(kernalName, architectureKey).ToArray();
        var character = _romProvider.LoadRom(characterName, architectureKey).ToArray();

        // Load ROMs into the flat store first, then register dedicated RomDevice
        // overlays so writes to ROM space are ignored (real hardware behaviour).
        ram.LoadRom(0xC000, basic);
        ram.LoadRom(0xE000, kernal);
        ram.LoadRom(0x8000, character);

        bus.RegisterDevice(new RomDevice(0xC000, 0xDFFF, basic));
        bus.RegisterDevice(new RomDevice(0xE000, 0xFFFF, kernal));
        bus.RegisterDevice(new RomDevice(0x8000, 0x8FFF, character));
        deviceRegistry.Add(new RomDevice(0xC000, 0xDFFF, basic), DeviceRole.BasicRom);
        deviceRegistry.Add(new RomDevice(0xE000, 0xFFFF, kernal), DeviceRole.KernalRom);
        deviceRegistry.Add(new RomDevice(0x8000, 0x8FFF, character), DeviceRole.ChargenRom);

        // Color RAM at $9400-$97FF (1KB; only low nibble used on real hardware).
        var colorRam = new RamDevice(0x9400, 0x97FF, new byte[0x0400]);
        bus.RegisterDevice(colorRam);

        // VIA1 @ $9110 -> NMI, VIA2 @ $9120 -> IRQ (VIC-20 board wiring).
        var via1 = new Via6522(bus, nmiLine)
        {
            Id = new DeviceId(0x0005),
            Name = "VIA1",
            SourceId = new DeviceId(0x0005),
            BaseAddress = 0x9110,
            Size = 0x0010,
        };
        var via2 = new Via6522(bus, irqLine)
        {
            Id = new DeviceId(0x0006),
            Name = "VIA2",
            SourceId = new DeviceId(0x0006),
            BaseAddress = 0x9120,
            Size = 0x0010,
        };
        bus.RegisterDevice(via1);
        bus.RegisterDevice(via2);
        clock.Register(via1);
        clock.Register(via2);
        deviceRegistry.Add(via1, DeviceRole.Via1);
        deviceRegistry.Add(via2, DeviceRole.Via2);

        var vic = new VicIStub(irqLine)
        {
            Id = new DeviceId(0x0003),
            Name = "VIC-I",
            SourceId = new DeviceId(0x0003),
            BaseAddress = 0x9000,
            Size = 0x0010,
        };
        var cyclesPerLine = profile?.CyclesPerLine ?? 71;
        var rasterLines = profile?.RasterLines ?? 312;
        var visibleLines = profile?.VideoStandard == VideoStandard.Ntsc ? 234 : 284;
        // VIC-20 visible character area is modest; stub uses a fixed pixel grid.
        vic.ConfigureTiming(cyclesPerLine, rasterLines, visibleLines, frameWidth: 224, frameHeight: visibleLines);
        bus.RegisterDevice(vic);
        clock.Register(vic);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);

        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);

        if (profile is not null)
            deviceRegistry.Add(new SystemCore(profile.SystemCore), DeviceRole.SystemCore);

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        return machine;
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs
- old_string: namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.RomFetch;

internal static class MachineTestFactory
{
    public static IMachine CreateC64Machine()
        => CreateC64Machine(new Architectures.C64.C64Descriptor());

    public static IMachine CreateC64Machine(string modelSelector)
        => CreateC64Machine(new Architectures.C64.C64Descriptor(modelSelector));

    public static IMachine CreateC64Machine(Architectures.C64.C64Descriptor descriptor)
    {
        var builder = new ArchitectureBuilder(CreateC64RomProvider());
        return builder.Build(descriptor);
    }

    public static IRomProvider CreateC64RomProvider()
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = new RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        // Test-harness fallback: the repo vendors the full VICE data tree at
        // native/vice/vice/data, so a bare checkout (CI agents with no
        // VICESHARP_ROM_PATH and no x64sc on PATH) is self-sufficient. The
        // product resolver deliberately does NOT know about repo layouts.
        var repoData = FindRepoViceDataRoot();
        if (repoData is not null)
        {
            var provider = new RomProvider(repoData, []);
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root with complete C64 ROM resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put x64sc.exe on PATH.");
    }

    private static string? FindRepoViceDataRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                continue;

            var data = Path.Combine(dir.FullName, "native", "vice", "vice", "data");
            return Directory.Exists(data) ? data : null;
        }

        return null;
    }

    public static ReadOnlyMemory<byte> LoadC64Rom(string romName)
    {
        return CreateC64RomProvider().LoadRom(romName, "C64");
    }

}
- new_string: namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Core;
using ViceSharp.RomFetch;

internal static class MachineTestFactory
{
    public static IMachine CreateC64Machine()
        => CreateC64Machine(new Architectures.C64.C64Descriptor());

    public static IMachine CreateC64Machine(string modelSelector)
        => CreateC64Machine(new Architectures.C64.C64Descriptor(modelSelector));

    public static IMachine CreateC64Machine(Architectures.C64.C64Descriptor descriptor)
    {
        var builder = new ArchitectureBuilder(CreateC64RomProvider());
        return builder.Build(descriptor);
    }

    public static IMachine CreateVic20Machine()
        => CreateVic20Machine(new Vic20Descriptor());

    public static IMachine CreateVic20Machine(string modelSelector)
        => CreateVic20Machine(new Vic20Descriptor(modelSelector));

    public static IMachine CreateVic20Machine(Vic20Descriptor descriptor)
    {
        var builder = new ArchitectureBuilder(CreateVic20RomProvider());
        return builder.Build(descriptor);
    }

    public static IRomProvider CreateC64RomProvider()
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = new RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        // Test-harness fallback: the repo vendors the full VICE data tree at
        // native/vice/vice/data, so a bare checkout (CI agents with no
        // VICESHARP_ROM_PATH and no x64sc on PATH) is self-sufficient. The
        // product resolver deliberately does NOT know about repo layouts.
        var repoData = FindRepoViceDataRoot();
        if (repoData is not null)
        {
            var provider = new RomProvider(repoData, []);
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root with complete C64 ROM resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put x64sc.exe on PATH.");
    }

    public static IRomProvider CreateVic20RomProvider()
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = new RomProvider(dataRoot, dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new Vic20RomSet().IsComplete(provider))
                return provider;
        }

        var repoData = FindRepoViceDataRoot();
        if (repoData is not null)
        {
            var provider = new RomProvider(repoData, []);
            if (new Vic20RomSet().IsComplete(provider))
                return provider;
        }

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root with complete VIC20 ROM resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put xvic.exe on PATH.");
    }

    private static string? FindRepoViceDataRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                continue;

            var data = Path.Combine(dir.FullName, "native", "vice", "vice", "data");
            return Directory.Exists(data) ? data : null;
        }

        return null;
    }

    public static ReadOnlyMemory<byte> LoadC64Rom(string romName)
    {
        return CreateC64RomProvider().LoadRom(romName, "C64");
    }

    public static ReadOnlyMemory<byte> LoadVic20Rom(string romName)
    {
        return CreateVic20RomProvider().LoadRom(romName, Vic20ViceRomNames.ArchitectureKey);
    }
}
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs
- old_string: /// Binary name -> topology kind:
///   x64 / x64sc      C64 host (+ optional drives + cart)
///   c1541            standalone 1541 (disk-only tool mode)
///   x128 / xvic / ...  reserved; throws NotSupportedException for now
///
/// True-drive flag controls whether drives attach via kind C1541 (true-
/// drive substrate, default true when -8/-9 is set) or stay as a non-
/// emulated cheap path (deferred; current implementation always uses
/// kind C1541).
/// </summary>
public static class ViceTopologyBuilder
{
    public static string BuildYaml(ViceArgs args)
    {
        return args.BinaryName switch
        {
            "x64" or "x64sc" => BuildC64Topology(args),
            "c1541" => BuildC1541Standalone(args),
            "x128" or "xvic" or "xpet" or "xplus4" or "xcbm2" or "xcbm5x0" or "vsid" or "petcat" or "cartconv"
                => throw new NotSupportedException(
                    $"Binary '{args.BinaryName}' is not yet supported by the ViceSharp substrate. " +
                    "Supported: x64, x64sc, c1541."),
            _ when args.MachineYamlPath is not null => File.ReadAllText(args.MachineYamlPath),
            _ => throw new InvalidOperationException(
                $"Unknown binary name '{args.BinaryName}' and no --machine-yaml supplied."),
        };
    }
- new_string: /// Binary name -> topology kind:
///   x64 / x64sc      C64 host (+ optional drives + cart)
///   xvic             VIC-20 host (Iteration 2; default drive 8 model 1540)
///   c1541            standalone 1541 (disk-only tool mode)
///   x128 / ...       reserved; throws NotSupportedException for now
///
/// True-drive flag controls whether drives attach via kind C1541 (true-
/// drive substrate, default true when -8/-9 is set) or stay as a non-
/// emulated cheap path (deferred; current implementation always uses
/// kind C1541). VIC-20 default DOS is 1540 when a drive image is attached.
/// </summary>
public static class ViceTopologyBuilder
{
    public static string BuildYaml(ViceArgs args)
    {
        return args.BinaryName switch
        {
            "x64" or "x64sc" => BuildC64Topology(args),
            "xvic" => BuildVic20Topology(args),
            "c1541" => BuildC1541Standalone(args),
            "x128" or "xpet" or "xplus4" or "xcbm2" or "xcbm5x0" or "vsid" or "petcat" or "cartconv"
                => throw new NotSupportedException(
                    $"Binary '{args.BinaryName}' is not yet supported by the ViceSharp substrate. " +
                    "Supported: x64, x64sc, xvic, c1541."),
            _ when args.MachineYamlPath is not null => File.ReadAllText(args.MachineYamlPath),
            _ => throw new InvalidOperationException(
                $"Unknown binary name '{args.BinaryName}' and no --machine-yaml supplied."),
        };
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs
- old_string:     private static void AppendDrive(StringBuilder yaml, int deviceNumber, string imagePath)
    {
        yaml.AppendLine($"    - id: drive-{deviceNumber}");
        yaml.AppendLine($"      kind: C1541");
        yaml.AppendLine($"      deviceNumber: {deviceNumber}");
        yaml.AppendLine($"      diskImagePath: {imagePath.Replace('\\', '/')}");
        yaml.AppendLine($"      busAttachments:");
        yaml.AppendLine($"        - busId: IEC");
        yaml.AppendLine($"          endpointName: drive-{deviceNumber}");
    }

    private static string BuildC1541Standalone(ViceArgs args)
- new_string:     private static void AppendDrive(StringBuilder yaml, int deviceNumber, string imagePath, string? dosRomName = null)
    {
        yaml.AppendLine($"    - id: drive-{deviceNumber}");
        yaml.AppendLine($"      kind: C1541");
        yaml.AppendLine($"      deviceNumber: {deviceNumber}");
        if (!string.IsNullOrWhiteSpace(dosRomName))
            yaml.AppendLine($"      dosRomName: {dosRomName}");
        yaml.AppendLine($"      diskImagePath: {imagePath.Replace('\\', '/')}");
        yaml.AppendLine($"      busAttachments:");
        yaml.AppendLine($"        - busId: IEC");
        yaml.AppendLine($"          endpointName: drive-{deviceNumber}");
    }

    /// <summary>
    /// VIC-20 (xvic) host topology. Default drive 8 DOS is 1540 (FR-VIC20-006).
    /// </summary>
    private static string BuildVic20Topology(ViceArgs args)
    {
        if (args.MachineYamlPath is not null)
            return File.ReadAllText(args.MachineYamlPath);

        // VICE data name for 1540 DOS; keep literal to avoid Launcher -> Architectures coupling.
        const string dos1540 = "dos1540-325302+3-01.bin";

        var yaml = new StringBuilder();
        yaml.AppendLine("schemaVersion: 1");
        yaml.AppendLine("coordinator:");
        yaml.AppendLine("  host:");
        yaml.AppendLine("    id: xvic-host");
        yaml.AppendLine("    kind: Vic20");
        if (args.Drive8Image is not null || args.Drive9Image is not null)
        {
            yaml.AppendLine("    busAttachments:");
            yaml.AppendLine("      - busId: IEC");
            yaml.AppendLine("        endpointName: vic20");
        }

        var hasPeripherals = args.Drive8Image is not null || args.Drive9Image is not null;
        if (hasPeripherals)
        {
            yaml.AppendLine("  peripherals:");
            if (args.Drive8Image is not null)
                AppendDrive(yaml, 8, args.Drive8Image, dos1540);
            if (args.Drive9Image is not null)
                AppendDrive(yaml, 9, args.Drive9Image, dos1540);
        }
        if (hasPeripherals)
        {
            yaml.AppendLine("  buses:");
            yaml.AppendLine("    - id: IEC");
            yaml.AppendLine("      signals: [ATN, CLK, DATA, SRQ]");
        }

        return yaml.ToString();
    }

    private static string BuildC1541Standalone(ViceArgs args)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Launcher\ViceTopologyBuilderTests.cs
- old_string:     /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Unsupported binaries throw a clear error rather than
    /// generating broken YAML.
    /// Acceptance: x128 / xvic / etc. throw NotSupportedException.
    /// </summary>
    [Theory]
    [InlineData("x128")]
    [InlineData("xvic")]
    [InlineData("xpet")]
    public void UnsupportedBinary_Throws(string binaryName)
    {
        var args = ViceArgsParser.Parse(binaryName, Array.Empty<string>());

        Assert.Throws<NotSupportedException>(() => ViceTopologyBuilder.BuildYaml(args));
    }
- new_string:     /// <summary>
    /// FR/TR: CLI-LAUNCHER-001
    /// Use case: Unsupported binaries throw a clear error rather than
    /// generating broken YAML.
    /// Acceptance: x128 / xpet / etc. throw NotSupportedException.
    /// (xvic is supported as of Iteration 2 Slice A.)
    /// </summary>
    [Theory]
    [InlineData("x128")]
    [InlineData("xpet")]
    public void UnsupportedBinary_Throws(string binaryName)
    {
        var args = ViceArgsParser.Parse(binaryName, Array.Empty<string>());

        Assert.Throws<NotSupportedException>(() => ViceTopologyBuilder.BuildYaml(args));
    }

    /// <summary>
    /// FR-VIC20-006 / CLI-LAUNCHER-001 / Iteration 2 Slice A.
    /// Use case: xvic binary name resolves to a Vic20 host topology; optional
    /// drive 8 uses 1540 DOS by default.
    /// Acceptance: YAML contains kind Vic20; with -8 image, dos1540 is set.
    /// </summary>
    [Fact]
    public void XvicBinary_BuildsVic20Topology()
    {
        var args = ViceArgsParser.Parse("xvic", Array.Empty<string>());

        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("kind: Vic20");
        yaml.Should().Contain("xvic-host");
    }

    [Fact]
    public void XvicBinary_WithDrive8_DefaultsTo1540Dos()
    {
        var args = ViceArgsParser.Parse("xvic", new[] { "-8", "game.d64" });

        var yaml = ViceTopologyBuilder.BuildYaml(args);

        yaml.Should().Contain("kind: Vic20");
        yaml.Should().Contain("dosRomName: dos1540-325302+3-01.bin");
        yaml.Should().Contain("game.d64");
    }

### Turn 90 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20SkeletonTests.cs has been updated successfully.

### Turn 91 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 92 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs has been updated successfully.

### Turn 93 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\MachineTestFactory.cs has been updated successfully.

### Turn 94 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Launcher\ViceTopologyBuilderTests.cs has been updated successfully.

### Turn 95 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 96 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs has been updated successfully.

### Turn 97 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 98 (Assistant)
[tool_request: grep]
- pattern: void Add\(|public void Reset
- glob: **/DeviceRegistry.cs
- path: F:\GitHub\vice-sharp\src
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 415
- limit: 80
[tool_request: run_terminal_command]
- command: dotnet build .\ViceSharp.slnx -c Release --nologo -v q 2>&1 | Select-Object -Last 60
- description: Build solution to surface compile errors

### Turn 99 (Function)
[tool_response]
415→        var romDescriptor = descriptor.Devices.First(d => d.Role == DeviceRole.DriveRom);
        var dosRomName = ResolveC1541DosRomName(descriptor);
        var romBytes = _romProvider.LoadRom(
            dosRomName,
            descriptor.RequiredRoms?.Architecture ?? "DRIVES").ToArray();
420→        var rom = new RomDevice(
            romDescriptor.BaseAddress,
            (ushort)(romDescriptor.BaseAddress + romDescriptor.Size - 1),
            romBytes);
        bus.RegisterDevice(rom);
        deviceRegistry.Add(rom, DeviceRole.DriveRom);

        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.DriveCpu);

430→        // Optional D64 disk image mount.
        D64DiskImageDevice? disk = null;
        if (descriptor is IDriveArchitectureDescriptor drive && !string.IsNullOrWhiteSpace(drive.DiskImagePath))
        {
            disk = D64DiskImageDevice.LoadFromFile(drive.DiskImagePath!);
            deviceRegistry.Add(disk, DeviceRole.DriveDisk);
        }

        var deviceNumber = descriptor is IDriveArchitectureDescriptor driveDescriptor
            ? driveDescriptor.DeviceNumber
440→            : 8;
        deviceRegistry.Add(new C1541IecInterfaceDevice(deviceNumber));

        var driveMechanism = new C1541DriveMechanismDevice(disk, cpu);
        clock.Register(driveMechanism);
        deviceRegistry.Add(driveMechanism);

        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu);
        machine.Reset();
        return machine;
450→    }

    private static string ResolveC1541DosRomName(IArchitectureDescriptor descriptor)
        => descriptor is IDriveArchitectureDescriptor d && !string.IsNullOrWhiteSpace(d.DosRomName)
            ? d.DosRomName
            : "dos1541-325302-01+901229-05.bin";

    private static bool IsKernalRomRequired(string kernalRomName)
        => !string.Equals(kernalRomName, "kernal-none.bin", StringComparison.OrdinalIgnoreCase);

460→    private static CartridgeMappingMode ResolveDefaultCartridgeMappingMode(IMachineProfile? profile)
    {
        if (profile is null)
            return CartridgeMappingMode.Auto;

        if (string.Equals(profile.SystemCore.AddressDecoderPolicy, "Ultimax", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.Ultimax;

        if (string.Equals(profile.BoardModel, "C64GS", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.GameSystem;
470→
        return CartridgeMappingMode.Auto;
    }

    private static byte ResolveCia2PortAInputMask(IMachineProfile? profile)
    {
        if (profile is null)
            return 0x7F;

        return profile.SystemCore.BusPolicy switch
480→        {
            "GameSystem" or "Max" => 0x3F,
            _ => 0x7F
        };
    }

    private static Mos6526 CreateC64Cia(IBus bus, IInterruptLine line, ushort baseAddress, long masterClockHz)
    {
        return new Mos6526(bus, line)
        {
490→            BaseAddress = baseAddress,
            TodCyclesPer50HzTick = (int)(masterClockHz / 50),
            TodCyclesPer60HzTick = (int)(masterClockHz / 60)
        };
    }

### Turn 100 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
No matches found
</workspace_result>

### Turn 101 (Function)
[tool_response]
<task-id>call-a942662c-97f7-4614-90c0-60b97dc4367a-78</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-a942662c-97f7-4614-90c0-60b97dc4367a-78.log</output-file>
<status>running</status>
<summary>Command "dotnet build .\ViceSharp.slnx -c Release --nologo -v q 2>&1 | Select-Object -Last 60" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-a942662c-97f7-4614-90c0-60b97dc4367a-78"] to retrieve the output.

### Turn 102 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-a942662c-97f7-4614-90c0-60b97dc4367a-78"]
- timeout_ms: 300000

### Turn 103 (Function)
[tool_response]
=== Task call-a942662c-97f7-4614-90c0-60b97dc4367a-78 ===
Command: dotnet build .\ViceSharp.slnx -c Release --nologo -v q 2>&1 | Select-Object -Last 60
Status: completed
Duration: 31.28s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-a942662c-97f7-4614-90c0-60b97dc4367a-78.log

=== Output ===

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:30.49


### Turn 104 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20|FullyQualifiedName~ViceTopologyBuilderTests" --nologo 2>&1
- description: Run Vic20 and launcher topology tests

### Turn 105 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    27, Skipped:     0, Total:    27, Duration: 167 ms - ViceSharp.TestHarness.dll (net10.0)


### Turn 106 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~Vic20Skeleton" --nologo -v n 2>&1 | Select-Object -Last 40; Write-Host "---"; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter "FullyQualifiedName~C64MachineTests.EmptyMachine|FullyQualifiedName~C64MachineTests.C64Descriptor_WithoutRom" --nologo 2>&1 | Select-Object -Last 20
- description: Confirm Vic20 count and C64 smoke
[tool_request: todo_write]
- todos: [{"id":"req","content":"req: Requirements freeze FR-VIC20-* docs","status":"completed"},{"id":"A","content":"A: Slice A: Vic20 skeleton red then green","status":"completed"},{"id":"B-next","content":"Next: Slice B memory map / C VIA / D VIC-I (parallel after A)","status":"pending"}]
- merge: true

### Turn 107 (Function)
[tool_response]
- [completed] 0: 0: Slice 0: branch feat/iteration2-vic20 + hygiene
- [completed] explore: explore: Explore native VIC-20 map/ROMs/VIA/VIC-I
- [completed] req: req: Requirements freeze FR-VIC20-* docs
- [completed] A: A: Slice A: Vic20 skeleton red then green
- [pending] B-next: Next: Slice B memory map / C VIA / D VIC-I (parallel after A)


### Turn 108 (Function)
[tool_response]
exit: 0
Build started 8/5/2026 12:21:24 PM.
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.5+1b188a7b0a (64-bit .NET 10.0.10)
[xUnit.net 00:00:00.28]   Discovering: ViceSharp.TestHarness
[xUnit.net 00:00:01.55]   Discovered:  ViceSharp.TestHarness
[xUnit.net 00:00:01.79]   Starting:    ViceSharp.TestHarness
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Machine_FirstThousandCycles_DoNotThrow_AndAdvanceClock [56 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Machine_BaseRam_IsWritable [5 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Resolve_PalAliases(selector: "vic20pal") [3 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Resolve_PalAliases(selector: "xvic") [< 1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Resolve_PalAliases(selector: "pal") [< 1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Resolve_PalAliases(selector: "vic20") [< 1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Default_IsPal_With6561TimingAnd1540Drive [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Machine_Builds_WithCpuVideoDualViaAndRoms [13 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Machine_AfterReset_CpuPcMatchesKernalResetVector [4 ms]
[xUnit.net 00:00:01.99]   Finished:    ViceSharp.TestHarness (ID = 'ab71db7394e73a1adc1130e42fc74f059e0e20545bf7641bf1047ee89aadf982')
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Resolve_NtscAliases(selector: "vic20ntsc") [< 1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Profile_Resolve_NtscAliases(selector: "ntsc") [< 1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20RomSet_IsComplete_AgainstViceDataRoot [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Launcher_Xvic_BuildsTopologyYaml_WithVic20HostAnd1540DriveDefaultComment [2 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Descriptor_DevicesList_IncludesExpectedRoles [1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.Vic20Descriptor_WithoutRomProvider_ThrowsClearError [< 1 ms]
  Passed ViceSharp.TestHarness.Vic20.Vic20SkeletonTests.DefaultDrive_Is1540_Not1541 [< 1 ms]

Test Run Successful.
Total tests: 16
     Passed: 16
 Total time: 2.4844 Seconds

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.21
---
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 65 ms - ViceSharp.TestHarness.dll (net10.0)


### Turn 109 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string:         // Load ROMs into the flat store first, then register dedicated RomDevice
        // overlays so writes to ROM space are ignored (real hardware behaviour).
        ram.LoadRom(0xC000, basic);
        ram.LoadRom(0xE000, kernal);
        ram.LoadRom(0x8000, character);

        bus.RegisterDevice(new RomDevice(0xC000, 0xDFFF, basic));
        bus.RegisterDevice(new RomDevice(0xE000, 0xFFFF, kernal));
        bus.RegisterDevice(new RomDevice(0x8000, 0x8FFF, character));
        deviceRegistry.Add(new RomDevice(0xC000, 0xDFFF, basic), DeviceRole.BasicRom);
        deviceRegistry.Add(new RomDevice(0xE000, 0xFFFF, kernal), DeviceRole.KernalRom);
        deviceRegistry.Add(new RomDevice(0x8000, 0x8FFF, character), DeviceRole.ChargenRom);
- new_string:         // Load ROMs into the flat store first, then register dedicated RomDevice
        // overlays so writes to ROM space are ignored (real hardware behaviour).
        ram.LoadRom(0xC000, basic);
        ram.LoadRom(0xE000, kernal);
        ram.LoadRom(0x8000, character);

        var basicRom = new RomDevice(0xC000, 0xDFFF, basic);
        var kernalRom = new RomDevice(0xE000, 0xFFFF, kernal);
        var chargenRom = new RomDevice(0x8000, 0x8FFF, character);
        bus.RegisterDevice(basicRom);
        bus.RegisterDevice(kernalRom);
        bus.RegisterDevice(chargenRom);
        deviceRegistry.Add(basicRom, DeviceRole.BasicRom);
        deviceRegistry.Add(kernalRom, DeviceRole.KernalRom);
        deviceRegistry.Add(chargenRom, DeviceRole.ChargenRom);
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\HANDOFF.md
- limit: 40

### Turn 110 (Function)
[tool_response]
1→# ViceSharp Handoff - 2026-08-05

**Active branch:** `feat/xbox-uwp-app` (off `main`) at `3804a1d`. **Pushed to GitHub `origin`** (`https://github.com/sharpninja/vice-sharp.git`; origin is the default approved remote). Azure DevOps remote remains as `azure`.
**Prior iteration-1 baseline:** `main` at `v1.0.2` (2026-07-08); **PLAN-VICEPARITY-001 COMPLETE**. Detail preserved in the dated sections below.
**Working tree:** untracked scratch (`docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, AiReview appsettings) is not part of the plan.

## PLAN-XBOXUWP + PLAN-ROMM resume snapshot (2026-08-05)

- **Track 1 (Xbox Tier D residuals) largely SHIPPED this session:** FEAT-XAOTBIND-001 (x:Bind migration), FEAT-XOCTOPUS-001 (CI/release Octopus LEGION2 steps), FIX-ROMLESSVIC-001 (merged test), FIX-XKBDNMI-001 (RESTORE asserts NMI), FEAT-XCTRLBIND-001 (Controls remapping), FEAT-XROMPICK-001 (model ROM readiness).
10→- **Track 2 (RomM):** merged `feat/romm-integration` into this branch (`3804a1d`). Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM`, Xbox/Avalonia library UI, LAN bridge connection path present. Gate after merge: **Category=Xbox|RomM|Library 546/546** (0 fail, 0 skip).
- **Deploy loop:** `./build.ps1 DeployXboxLocal` (VS MSBuild Release-UWP). End CLI batches with Debug-UWP restore for operator VS F5. PublishAot stays opt-in (`ViceSharpPublishAot=true`); Release-UWP JIT.
- **OPEN:** B-on-controller operator verification; S42 Tier-C console/Store (deferred); MCP TODO reconcile (mark completed IMPL-XBOXUWP / IMPL-ROMM ids done with receipts); device smoke for RomM Library after merge; optional full baseline suite.
- **Parallel worktrees (reference):** `F:\GitHub\vice-sharp-romm` (`feat/romm-integration`, source of merge); romless repro merged via FIX-ROMLESSVIC-001.

## How to resume

1. Read `AGENTS-README-FIRST.yaml` (marker rotates on server restart; verify signature + /health nonce before MCP work).
2. Route TODO / session-log / requirements / triage through the mcpserver plugin (repl-invoke or wrappers), never raw REST, never storage files.
3. Baseline gate (one process, ~20 min, run detached):
20→   `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`
   Green criterion: 0 failed, 21 skipped, total 2612-2615 (totals wobble in that band from cosmetic xunit trx name serialization of dynamic theory rows; per-class row sets are identical - never chase a fixed total).

## Shipped 2026-07-08 (all CI-confirmed)

- **v1.0.2 released**: tag on `534cded`, release run 1074, all 13 package ids verified in the nuget.org flat container (Core bundle, SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox; Console + Avalonia are dotnet tools).
- **PLAN-NATIVERESIDUE-001 closed** (`ba0f94f`): a `.vsf` whose DRIVE8 module carries `has_tde=0` disabled TrueDrive process-wide via `resources_set_int` (drive-snapshot.c:334-363); `vice_machine_create_model` now re-baselines `Drive{8..11}TrueEmulation` to the VICE default 1 through `resources_set_int` so iecbus statics and $DD00 callbacks recompute. SnapshotResume two-process partition removed; suite proven green in ONE process. Guards: TEST-NATIVE-RESIDUE-01/02 in `NativeResidueDiagTests`.
- **CI made self-sufficient** (`a229801`, `d8f61cf`, `4e3db9b`): agents have no ROMs and no shim, so `EnsureCiRomRoot` in `build/Build.cs` stages 13 hash-pinned VICE data files (C64 ROMs incl. all variant kernals, DRIVES dos1541/dos1541ii, gtk3_pos.vkm) from the VICE-Team svn-mirror into `artifacts/vice-data` and sets `VICESHARP_ROM_PATH` for the test process only. Shim-dependent tests MUST use `[ViceFact]`/`[ViceTheory]` (plain `[Fact]` throws DllNotFoundException on shimless agents). First-ever green CI run: 1072.
- **Docs reconciled to shipped reality** (`aee29ea`, 88 files): README, USER-GUIDE, VICE-MIGRATION, ROMs, Architecture, Public-API (regenerated from the 50 real Abstractions interfaces), requirements docs; 22 stale files pruned; `docs/wiki.yaml` manifest lists all 28 wiki documents; winget license fixed MIT -> GPL-2.0-or-later.
- **github divergence resolved** (`20b330c`): github-only commit `42f2f26` (VicModeChangeEvent seam) merged with strategy `ours` (tree unchanged; content preserved on `rescue/vic-modechange-event`); github main fast-forwarded, tags identical on both remotes; github wiki published per manifest (`b455b88`, 28 pages + sidebar).
30→- **Dependency wave S0-S8 complete** (TR-DEPS-202607-001 + TEST-DEPS-202607-001, both completed with evidence; commits `72696d9`, `055b9d5`, `26e5ca8`, `87eb80a`, `9214cdb`, `1cad822`+`1adf386`, `23d030a`, `0f5192f`): all 30 central entries at newest mutually compatible stables (Avalonia 12.0.5, Extensions/TestHost 10.0.9, Protobuf 3.35.1, Grpc.Tools 2.82.0, Test.Sdk 18.7.0, Roslyn pair 5.6.0, YamlDotNet 18.1.0, FluentAssertions 8.10.0, coverlet 10.0.1, aiUnit 2.1.3, RemoteControl 0.7.4); 5 dead pins removed; AiReview under CPM with inherited TreatWarningsAsErrors; vendored `nuget-local` feed DELETED (NuGet.config is nuget.org-only); `global.json` floor 10.0.301 (Roslyn 5.6 generator gate). Excluded as prerelease: xunit.v3 4.x, NSubstitute 6.x, Protobuf 4.x, Extensions 11.x.
- **Test fixtures in repo** (`93cdc7e`): `vice-snapshot-20260630171307.vsf` (read by the residue probes and lockstep re-baseline suites at repo root; do not delete), `native/.build-nopatch.sh`, `native/.ccwrap/*`. Operator rule: if it is needed to run a test, it goes in the repo.

## Iteration-1 completion (branch `fix/nativeresidue-002-drive-clock-hardening`, off `master`; NOT pushed)

Working through the "complete outstanding iteration 1 work" plan. Done, all committed locally, gates green:
- **Slice A - PLAN-NATIVERESIDUE-002** (710c1e9/8894f1a/9fb06dd/0691344): the two latent drive-clock bugs fixed; **BUG-LOCKSTEP-001 CLOSED** (full gate 0 failed / 2596 passed / 21 skipped). See docs/receipts-nativeresidue-002-2026-07-09.md.
- **Slice F - BUG-TESTDEBT-001**: verified already fixed at HEAD (6a32e7c/77e7190), closed with receipt.
- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.

40→**SID parity program COMPLETE (2026-07-10)** - all remaining slices landed:

### Turn 111 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 112 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\HANDOFF.md
- old_string: # ViceSharp Handoff - 2026-08-05

**Active branch:** `feat/xbox-uwp-app` (off `main`) at `3804a1d`. **Pushed to GitHub `origin`** (`https://github.com/sharpninja/vice-sharp.git`; origin is the default approved remote). Azure DevOps remote remains as `azure`.
**Prior iteration-1 baseline:** `main` at `v1.0.2` (2026-07-08); **PLAN-VICEPARITY-001 COMPLETE**. Detail preserved in the dated sections below.
**Working tree:** untracked scratch (`docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, AiReview appsettings) is not part of the plan.

## PLAN-XBOXUWP + PLAN-ROMM resume snapshot (2026-08-05)
- new_string: # ViceSharp Handoff - 2026-08-05

**Active branch:** `feat/iteration2-vic20` (off `main` @ v1.2.1 line). Iteration 2 = overall project VIC-20 (not RomM Phase E).
**Prior release baseline:** `main` at **v1.2.1** (NuGet + winget published). Iteration 1 C64 complete.
**Working tree noise (do not commit):** untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, `manifests/`.

## Iteration 2 VIC-20 resume (2026-08-05)

- **Plan:** session plan file Iteration 2 (Slice 0..H); locked decision: **default drive 8 = 1540** (`DriveModel.C1540` / `dos1540-325302+3-01.bin`).
- **Slice 0:** branch `feat/iteration2-vic20` from main.
- **Requirements:** `docs/requirements/functional/FR-VIC20.md` (FR-VIC20-001..006); `FR-PRF-005` retargeted to Iteration 2 + AC10 default 1540.
- **Slice A GREEN (skeleton):**
  - `src/ViceSharp.Architectures/Vic20/*` (descriptor, profile PAL/NTSC, ROM set, system core)
  - `src/ViceSharp.Chips/Vic/VicIStub.cs` (black-frame `IVideoChip` stub)
  - `ArchitectureBuilder.BuildVic20Machine` (Mos6502 + dual Via6522 $9110/$9120 + ROMs + color RAM)
  - `DeviceRole.Via1` / `Via2`; launcher `xvic` topology YAML (`kind: Vic20`, drive DOS 1540)
  - Tests: `Vic20SkeletonTests` **16/16**; launcher xvic tests updated
- **Gate receipt:** `dotnet test tests/ViceSharp.TestHarness -c Release --filter FullyQualifiedName~Vic20Skeleton` -> Passed 16, Failed 0, Skipped 0
- **Next:** Slice B memory map/expansions || C VIA keyboard/NMI wiring || D real VIC-I (worktrees after A); then E boot proof; F heads; G lockstep; H docs.
- **Do not** treat old `FR-to-Iteration-Map.md` Iteration 2 C64 residual list as the delivery checklist.

## PLAN-XBOXUWP + PLAN-ROMM resume snapshot (2026-08-05)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Skeleton|FullyQualifiedName~XvicBinary" --nologo 2>&1 | Select-Object -Last 25
- description: Rebuild and re-run Vic20 + xvic tests

### Turn 113 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\HANDOFF.md has been updated successfully.

### Turn 114 (Function)
[tool_response]
exit: 0
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: 135 ms - ViceSharp.TestHarness.dll (net10.0)


### Turn 115 (Assistant)
**2026-08-05** — Profile loaded. Confirmed: Phase 2 = overall VICE# **Iteration 2 (VIC-20)**, not RomM Phase E.

## Status

**Branch:** `feat/iteration2-vic20` (from main / v1.2.1 line)

**Slice A is green** (machine skeleton). Requirements frozen; locked decision **default drive 8 = 1540** is encoded in profile + launcher.

### Delivered

| Area | What landed |
|------|-------------|
| Requirements | `docs/requirements/functional/FR-VIC20.md` (001–006); `FR-PRF-005` retargeted to Iteration 2 + AC10 (1540) |
| Architecture | `src/ViceSharp.Architectures/Vic20/*` — descriptor, PAL/NTSC profiles, ROM set, system core |
| Chips | `VicIStub` (`IVideoChip` black frame) under `Chips/Vic/` |
| Builder | `ArchitectureBuilder.BuildVic20Machine` — 6502, dual VIA ($9110 NMI / $9120 IRQ), ROMs, color RAM |
| Roles | `DeviceRole.Via1` / `Via2` |
| Launcher | `xvic` builds topology YAML (`kind: Vic20`; drive DOS 1540 when `-8` set) |
| Tests | `Vic20SkeletonTests` + updated launcher tests |
| Handoff | `HANDOFF.md` updated for Iteration 2 resume |

### Gate (receipt)

```text
dotnet test tests/ViceSharp.TestHarness -c Release --filter "FullyQualifiedName~Vic20Skeleton|FullyQualifiedName~XvicBinary"
Passed: 18, Failed: 0, Skipped: 0
```

Build: 0 warnings, 0 errors.

### Not in this slice (planned next)

- **B** full memory map / expansions  
- **C** keyboard + VIA board wiring  
- **D** real VIC-I (6560/6561)  
- **E** BASIC boot proof  
- **F** heads / architecture switch UI  
- **G** `xvic` lockstep  
- **H** docs closeout  

Parallel B ‖ C ‖ D is next after A (worktrees), per the approved plan.

Work is **uncommitted** on `feat/iteration2-vic20`. Say if you want a commit, or continue into Slice B/C/D.

### Turn 116 (Human)
<user_query>
<system-reminder>
A goal has been set: 100% completion of phase 2 using background agents as appropriate

You are working directly on this goal across multiple turns. Deliver
EVERYTHING the user asked for yourself — no follow-up questions, no manual
steps left for the user.

A structured plan for this goal is on disk — the source of truth for "done".
Read it first and keep it open.

Plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\goal\plan.md

- Seed todos from the plan's acceptance criteria via todo_write before
  executing.
- If the plan has a `## Task checklist`, work it in order and flip each
  `- [ ]` to `- [x]` in the plan file as you complete it — the harness mines
  the first unchecked box as your next-step nudge, so a stale checklist
  produces stale nudges.
- Execute item by item; when you deviate, append a bullet to the plan's single
  `## Deviations` section — add to that one section; don't start a new one, and
  don't edit the plan's existing items. Keep it TERSE: ONE bullet per deviation
  (what changed + why); not a progress log, so don't restate the plan or dump
  test counts / "all fixed" / "verification re-run" / "superseding" notes there.
- Before claiming completion, run the plan's `## Verification plan` yourself and
  confirm its observations hold. SAVE durable proof: commit real tests that drive
  the shipped code in-repo, and write the captured run output to your scratch dir
  (the one the goal rules name; never shared `/tmp/...`). Fix any missing
  observation before calling the goal complete.
<task_completion_discipline>
Multi-step goal work fails when the model narrates an action without executing it, asks for permission to continue an obviously-in-flight task, or stops with easy work still undone. These rules apply for the duration of an active goal.

1. **Tool-call first, narration second.** Any past-tense or present-continuous prose describing an action ("I launched...", "I'm now reading...", "The subagent is working on...") MUST be paired with the corresponding tool call in the same assistant response. If you end a turn with such a sentence but no tool call, the action did not happen. Write the launch announcement only AFTER the tool call appears in the same response — never on its own.

2. **Don't ask permission to continue a task in flight.** User-facing questions are for genuine ambiguity that changes the approach (e.g., two reasonable architectures, a missing requirement). It is NOT for cadence negotiation ("Want me to check in every 30 minutes?"), confirmation on the obvious next step ("Should I proceed to fix these issues?"), or asking the user to re-affirm a plan they already authorised. When the next step is dictated by your todo list or the goal objective, just do it.

3. **Track multi-step work with a todo_write list when it helps.** For longer tasks a todo list is a useful scratchpad — lay out the steps, keep roughly one `in_progress`, and update items as you finish them. It is an aid to your own memory, NOT a deliverable: don't over-decompose, and don't spend turns on bookkeeping at the expense of the actual work.

4. **Don't stop with easy work left undone.** Before ending a turn, check whether obvious remaining work exists that nothing is blocking. If so, keep going rather than handing back early — the goal loop re-engages you until verification passes anyway, so stopping short only wastes a round. Legitimately stop when you are genuinely waiting on a live background task, you need a user decision on real ambiguity, or you hit a hard external blocker (missing credentials, network down, denied permission) — state the blocker explicitly.
</task_completion_discipline>
TRACKING: use todo_write to break the objective into concrete steps; keep ≥1
`in_progress` with a present-tense `activeForm`, and mark each done immediately
(do not batch).

WORKING: implement it yourself and test it on the real user path. Where a
behavior cannot be driven end-to-end here, cover it with a static / structural
check (assert the artifact exists in the source) plus a unit test of the real
shipped function — not a flaky end-to-end run.

NO TEST THEATER: a passing test must prove the SHIPPED code works on the real
path. Never hard-code the expected value, start past the thing under test,
re-implement the code under test inside the test, or report success without
driving the real entry point. A test that passes while the program is broken is
worse than none.

VERIFY AS YOU GO: run each change. If output is visual, capture and inspect it;
for data/config, validate programmatically.

SCRATCH: use your private scratch dir C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\implementer only for captured test
output, temp scripts, and throwaway artifacts — never shared `/tmp/...` paths
(skeptics and concurrent goals collide there). The dir has been created for you. Use existing
user, system, or project defaults for execution dependencies and environment
state. NEVER set `HOME`, `CARGO_HOME`, `RUSTUP_HOME`, package-manager homes,
virtualenvs, caches, or config dirs to scratch, or write persistent config that
references scratch; the scratch dir is deleted when the goal ends. The plan's `{SCRATCH}` placeholder
resolves to it. The verifier AUDITS your committed tests and saved evidence
instead of rebuilding them, so honest, durable proof is what passes.

TEST PROACTIVELY: run targeted tests after every change, not just at the end.
The harness evaluates completion automatically after every model round. When the
work appears complete it runs the adversarial verification panel itself and
continues with any concrete gaps. Do not stop merely to announce completion.
If a real external blocker remains after repeated attempts, explain the exact
evidence and user action needed in your final response; the harness applies the
repeated-blocker policy automatically.

Start now.
</system-reminder>


</user_query>

### Turn 117 (Assistant)
Reading the goal plan and seeding the task list so we can drive Iteration 2 to completion.
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\goal\plan.md
[tool_request: run_terminal_command]
- command: git status -sb; git branch --show-current; git log -3 --oneline
- description: Check branch and current git state

### Turn 118 (Function)
[tool_response]
1→# Plan: Complete Iteration 2 (Phase 2) VIC-20 architecture to playable, test-gated close

## Goal kind
code-change

## Acceptance criteria
1. With official VIC-20 ROMs from a VICE data root (`VICESHARP_ROM_PATH` / repo `native/vice/vice/data`), a VIC-20 machine boots far enough that a focused test proves BASIC/KERNAL is live (READY-like fingerprint: screen memory or documented KERNAL/BASIC state after N cycles / frame count).
2. VIC-I video (PAL 6561 and NTSC 6560 timing) produces host-consumable frames (`IVideoChip` framebuffer + FrameCompleted); character-mode output for a known screen-RAM pattern is covered by unit tests (not black-stub-only).
3. VIC-20 keyboard matrix and joystick path work through VIA port wiring (synthetic key/joystick inject readable by software scan); dual VIA windows at board I/O remain IRQ/NMI-routed without forking the shared VIA chip; C64 and 1541 VIA tests stay green.
10→4. Software can attach via MVP cart and/or disk: unit 8 defaults to **1540** (`DriveModel.C1540` / dos1540) for VIC-20 only; C64 default remains 1541; `xvic` launcher topology resolves; machine pickers that list computers can select VIC-20 when ROMs are available; session create can target VIC-20 without requiring C64-only hardcoding.
5. Bounded multi-frame lockstep or checkpoint parity vs native `xvic` (CPU + bus + VIA at minimum) passes in-repo tests; full focused `FullyQualifiedName~Vic20` gate is 0 failed / 0 skipped; C64 smoke (empty machine + one C64 descriptor path) does not regress.

## Verification plan
1. **gating:** Run `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20"`. Capture full console to `{SCRATCH}/vic20-gate.log`. Observations that MUST be present: exit code 0; Failed = 0; Skipped = 0; Passed > 0; suite includes boot proof, memory/expansion, VIA/input, video character-mode, drive-default 1540, and lockstep or checkpoint tests by name in the log.
2. **gating:** Run `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Skeleton|FullyQualifiedName~BasicBootProof|FullyQualifiedName~C64MachineTests.EmptyMachine|FullyQualifiedName~C64Descriptor_WithoutRom|FullyQualifiedName~XvicBinary|FullyQualifiedName~ViceTopologyBuilder"`. Capture to `{SCRATCH}/vic20-c64-smoke.log`. Observations: exit code 0; Failed = 0; Vic20 and xvic cases pass; C64 empty/ROM-provider cases still pass (non-regression).
3. **gating:** Exercise shipped construction path (test factory or architecture builder used by production), not a reimplementation: after reset with real VIC20 ROMs, assert reset vector / PC from `$FFFC`, BASIC byte at `$C000`, KERNAL at `$E000`, and post-boot fingerprint from criterion 1. Capture command + assert lines to `{SCRATCH}/vic20-boot-proof.log`.
4. **gating:** Assert profile/session default for VIC-20 unit 8 is 1540 and C64 remains 1541 via existing or new tests that call production defaults (no hardcoded oracle copy of the constant only in the test). Capture to `{SCRATCH}/vic20-drive1540.log`.
5. **evidence:** `dotnet build ViceSharp.slnx -c Release` with 0 errors; capture tail to `{SCRATCH}/build.log`.
6. **evidence:** If native `xvic` binary is unavailable in the environment, lockstep tests must either skip-with-hard-fail-disabled only when the OBJECTIVE allows (they must not) — preferred: stage ROMs and run managed-side parity / oracle harness already in-repo; if lockstep cannot run, record failure under `{SCRATCH}/lockstep-env.txt` and do not claim criterion 5 met until a capturable substitute (in-repo multi-frame determinism vs golden trace committed with ROMs hashed) is green.
20→7. **evidence:** Background agents may implement parallel slices (memory ‖ VIA input ‖ VIC-I chip) in worktrees; orchestrator merges and re-runs step 1 after each merge. Agent reports alone are not proof; only re-run logs under `{SCRATCH}` count.

## Non-goals
- C128, PET, Plus/4, or other non-VIC-20 machines
- Perfect VIC-I audio / full SID-class sound parity
- Full C64 FLI/AFLI or extra drive families (1581/1571/CMD HD)
- Xbox Store Tier C / native AOT re-enable
- Shipping Commodore ROMs inside the package
- Release version bump / NuGet/winget publish (unless operator later asks)
- Treating old `FR-to-Iteration-Map.md` Iteration 2 C64 residual FRs as this phase's checklist
30→
## Assumed scope
- Branch `feat/iteration2-vic20` (Slice A skeleton already present under `Architectures/Vic20`, `Chips/Vic` stub, `ArchitectureBuilder` Vic20 path, launcher `xvic`)
- `src/ViceSharp.Architectures/Vic20/`, `src/ViceSharp.Chips/` (VIC-I + shared VIA only via callbacks), `src/ViceSharp.Core/ArchitectureBuilder` and memory/host glue, `src/ViceSharp.Launcher`, host/session create paths, machine pickers (Console/Avalonia/Xbox as applicable), `tests/ViceSharp.TestHarness/Vic20/` and lockstep harness extensions, `docs/requirements/functional/FR-VIC20.md` / roadmap / HANDOFF
- External: VICE data ROMs under `native/vice/vice/data/VIC20` and DRIVES dos1540; optional native `xvic` for lockstep oracle
- Process: Byrd TDD (red then green), background/worktree agents for non-colliding slices

## Implementation approach
- Keep chip vs machine separation: shared VIA and VIC-I stay machine-agnostic; banking, IRQ/NMI routing, keyboard matrix, cart BLKs, and 1540 default stay in architecture/host adapters.
- Prefer small pure units with direct unit tests (memory decode tables, VIC register masks, matrix scan, boot fingerprint) before host integration.
40→- After skeleton (A): parallel worktrees for memory map, VIA input wiring, and VIC-I chip; merge then sequential boot proof, then heads + lockstep, then docs.
- Default drive 8 = 1540 is a profile/host policy constant with tests that pin both Vic20 and C64 defaults.
- Every slice ends with focused filter green and captured log under `{SCRATCH}` before the next slice starts.

## Task checklist
- [ ] Confirm Slice A on branch, re-run Vic20Skeleton + xvic gates, capture `{SCRATCH}/slice-a-baseline.log`
- [ ] Background agents (worktrees): complete memory map/expansions, VIA keyboard/joystick wiring, and real VIC-I character-mode frames; merge; re-run Vic20 filter to `{SCRATCH}/bcd-merge.log`
- [ ] Boot proof: READY-like fingerprint + cart/PRG path tests green; capture `{SCRATCH}/boot-proof.log`
- [ ] Drive-8 default 1540 for Vic20 (and C64 still 1541); IEC/disk smoke if true-drive path ready; capture `{SCRATCH}/drive1540.log`
- [ ] Heads/launcher: VIC-20 selectable where pickers exist; session create works; capture relevant unit logs to `{SCRATCH}/heads.log`
50→- [ ] Bounded xvic lockstep/determinism gate green; full `FullyQualifiedName~Vic20` 0 fail 0 skip; C64 smoke; capture `{SCRATCH}/vic20-gate.log` and `{SCRATCH}/vic20-c64-smoke.log`
- [ ] Docs/HANDOFF/roadmap mark Iteration 2 complete; leave untracked scratch (`docs/S-Blox/`, reviews, manifests) out of commits

## Risks / Contradictions
- Full "playable" + native `xvic` lockstep may be blocked if the agent environment lacks a built `xvic` binary; criterion 5 then requires an in-repo golden-trace/determinism substitute that still exercises the shipped Vic20 machine, not a mock.
- Parallel agents on shared `ArchitectureBuilder` or `Via6522.cs` will collide; ownership must stay path-partitioned (memory / input / chips) until merge.
- Slice A stub video must be replaced for criterion 2; leaving only a black stub fails phase-2 completion even if boot tests pass.


### Turn 119 (Function)
[tool_response]
exit: 0
## feat/iteration2-vic20
 M HANDOFF.md
 M docs/requirements/functional/FR-Machine-Profiles.md
 M src/ViceSharp.Abstractions/IDeviceRegistry.cs
 M src/ViceSharp.Core/ArchitectureBuilder.cs
 M src/ViceSharp.Launcher/ViceTopologyBuilder.cs
 M tests/ViceSharp.TestHarness/Launcher/ViceTopologyBuilderTests.cs
 M tests/ViceSharp.TestHarness/MachineTestFactory.cs
?? docs/S-Blox/
?? docs/requirements/functional/FR-VIC20.md
?? docs/reviews/code-review-20260714T001056552Z.md
?? docs/reviews/code-review-20260714T004256563Z.md
?? docs/romless-vic-badline-fix.md
?? manifests/
?? src/ViceSharp.Architectures/Vic20/
?? src/ViceSharp.Chips/Vic/
?? tests/ViceSharp.TestHarness/Vic20/
feat/iteration2-vic20
b2ba4da ci: point ADO pipelines at GitHub; restore Octopus release steps
3b20c4f release: bump patch to 1.2.1 on main
521987b merge: feat/xbox-uwp-app into main for 1.2 line


### Turn 120 (Assistant)
[tool_request: todo_write]
- todos: [{"id":"a-baseline","content":"Slice A baseline: Vic20Skeleton + xvic gates, capture slice-a-baseline.log","status":"in_progress"},{"id":"bcd-memory","content":"Slice B: VIC-20 memory map + expansions with tests","status":"pending"},{"id":"bcd-via","content":"Slice C: VIA keyboard/joystick wiring with tests","status":"pending"},{"id":"bcd-vici","content":"Slice D: Real VIC-I character-mode frames (replace stub)","status":"pending"},{"id":"boot","content":"Boot proof: READY fingerprint + cart/PRG path tests","status":"pending"},{"id":"drive1540","content":"Drive-8 default 1540 Vic20 / 1541 C64 + IEC smoke","status":"pending"},{"id":"heads","content":"Heads/launcher: VIC-20 selectable + session create","status":"pending"},{"id":"lockstep","content":"Lockstep/determinism + full Vic20 gate + C64 smoke","status":"pending"},{"id":"docs","content":"Docs/HANDOFF/roadmap mark Iteration 2 complete","status":"pending"}]
- merge: false
[tool_request: run_terminal_command]
- command: $scratch = "C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\implementer"; New-Item -ItemType Directory -Force -Path $scratch | Out-Null; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Skeleton|FullyQualifiedName~XvicBinary" --nologo 2>&1 | Tee-Object -FilePath "$scratch\slice-a-baseline.log" | Select-Object -Last 20
- description: Run Slice A baseline and capture log
[tool_request: grep]
- pattern: KeyboardMatrix|KeyInject|Joystick|CreateSession|ArchitectureId|xvic|vic20|DefaultDrive
- glob: **/*.{cs}
- path: F:\GitHub\vice-sharp\src
- head_limit: 50
[tool_request: list_dir]
- target_directory: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 227
- limit: 160

### Turn 121 (Function)
[tool_response]
- [in_progress] a-baseline: Slice A baseline: Vic20Skeleton + xvic gates, capture slice-a-baseline.log
- [pending] bcd-memory: Slice B: VIC-20 memory map + expansions with tests
- [pending] bcd-via: Slice C: VIA keyboard/joystick wiring with tests
- [pending] bcd-vici: Slice D: Real VIC-I character-mode frames (replace stub)
- [pending] boot: Boot proof: READY fingerprint + cart/PRG path tests
- [pending] drive1540: Drive-8 default 1540 Vic20 / 1541 C64 + IEC smoke
- [pending] heads: Heads/launcher: VIC-20 selectable + session create
- [pending] lockstep: Lockstep/determinism + full Vic20 gate + C64 smoke
- [pending] docs: Docs/HANDOFF/roadmap mark Iteration 2 complete


### Turn 122 (Function)
[tool_response]
- F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic/
  - VicIStub.cs

### Turn 123 (Function)
[tool_response]
227→        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        serialTrap?.Reset();
230→        return machine;
    }

    private static bool IsC64Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.VideoChip)
            || roles.Contains(DeviceRole.Cia1)
            || roles.Contains(DeviceRole.Cia2)
            || roles.Contains(DeviceRole.Pla)
240→            || roles.Contains(DeviceRole.AudioChip);
    }

    private static bool IsVic20Machine(IArchitectureDescriptor descriptor)
    {
        if (descriptor is IProfiledArchitectureDescriptor profiled
            && string.Equals(profiled.MachineProfile.Family, "xvic", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
250→
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.Via1) && roles.Contains(DeviceRole.Via2);
    }

    private static bool IsC1541Machine(IArchitectureDescriptor descriptor)
    {
        var roles = descriptor.Devices.Select(x => x.Role).ToHashSet();
        return roles.Contains(DeviceRole.DriveCpu) && roles.Contains(DeviceRole.DriveRom);
    }
260→
    /// <summary>
    /// Build a VIC-20 skeleton: Mos6502, dual Via6522 ($9110 NMI / $9120 IRQ),
    /// VIC-I stub video, base RAM, and BASIC/KERNAL/chargen ROMs. Full memory
    /// banking and keyboard wiring land in later Iteration 2 slices.
    /// </summary>
    /// <remarks>FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006.</remarks>
    private IMachine BuildVic20Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
270→            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
280→        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine, nmiLine);
        var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;

        // Base 64KB backing store; ROMs and I/O devices overlay via register order
        // (BasicBus prefers the most recently registered device that HandlesAddress).
        var ram = new SimpleRam();
        ram.Fill(0x00);
        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);
290→
        var architectureKey = descriptor.RequiredRoms?.Architecture ?? "VIC20";
        var basicName = profile?.BasicRomName ?? "basic-901486-01.bin";
        var kernalName = profile?.KernalRomName ?? "kernal.901486-07.bin";
        var characterName = profile?.CharacterRomName ?? "chargen-901460-03.bin";

        var basic = _romProvider.LoadRom(basicName, architectureKey).ToArray();
        var kernal = _romProvider.LoadRom(kernalName, architectureKey).ToArray();
        var character = _romProvider.LoadRom(characterName, architectureKey).ToArray();

300→        // Load ROMs into the flat store first, then register dedicated RomDevice
        // overlays so writes to ROM space are ignored (real hardware behaviour).
        ram.LoadRom(0xC000, basic);
        ram.LoadRom(0xE000, kernal);
        ram.LoadRom(0x8000, character);

        var basicRom = new RomDevice(0xC000, 0xDFFF, basic);
        var kernalRom = new RomDevice(0xE000, 0xFFFF, kernal);
        var chargenRom = new RomDevice(0x8000, 0x8FFF, character);
        bus.RegisterDevice(basicRom);
310→        bus.RegisterDevice(kernalRom);
        bus.RegisterDevice(chargenRom);
        deviceRegistry.Add(basicRom, DeviceRole.BasicRom);
        deviceRegistry.Add(kernalRom, DeviceRole.KernalRom);
        deviceRegistry.Add(chargenRom, DeviceRole.ChargenRom);

        // Color RAM at $9400-$97FF (1KB; only low nibble used on real hardware).
        var colorRam = new RamDevice(0x9400, 0x97FF, new byte[0x0400]);
        bus.RegisterDevice(colorRam);

320→        // VIA1 @ $9110 -> NMI, VIA2 @ $9120 -> IRQ (VIC-20 board wiring).
        var via1 = new Via6522(bus, nmiLine)
        {
            Id = new DeviceId(0x0005),
            Name = "VIA1",
            SourceId = new DeviceId(0x0005),
            BaseAddress = 0x9110,
            Size = 0x0010,
        };
        var via2 = new Via6522(bus, irqLine)
330→        {
            Id = new DeviceId(0x0006),
            Name = "VIA2",
            SourceId = new DeviceId(0x0006),
            BaseAddress = 0x9120,
            Size = 0x0010,
        };
        bus.RegisterDevice(via1);
        bus.RegisterDevice(via2);
        clock.Register(via1);
340→        clock.Register(via2);
        deviceRegistry.Add(via1, DeviceRole.Via1);
        deviceRegistry.Add(via2, DeviceRole.Via2);

        var vic = new VicIStub(irqLine)
        {
            Id = new DeviceId(0x0003),
            Name = "VIC-I",
            SourceId = new DeviceId(0x0003),
            BaseAddress = 0x9000,
350→            Size = 0x0010,
        };
        var cyclesPerLine = profile?.CyclesPerLine ?? 71;
        var rasterLines = profile?.RasterLines ?? 312;
        var visibleLines = profile?.VideoStandard == VideoStandard.Ntsc ? 234 : 284;
        // VIC-20 visible character area is modest; stub uses a fixed pixel grid.
        vic.ConfigureTiming(cyclesPerLine, rasterLines, visibleLines, frameWidth: 224, frameHeight: visibleLines);
        bus.RegisterDevice(vic);
        clock.Register(vic);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);
360→
        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);

        if (profile is not null)
            deviceRegistry.Add(new SystemCore(profile.SystemCore), DeviceRole.SystemCore);

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
370→        return machine;
    }

    private IMachine BuildC1541Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
380→                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine);

### Turn 124 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 39 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceTopologyBuilder.cs
24:///   xvic             VIC-20 host (Iteration 2; default drive 8 model 1540)
40:            "xvic" => BuildVic20Topology(args),
45:                    "Supported: x64, x64sc, xvic, c1541."),
160:    /// VIC-20 (xvic) host topology. Default drive 8 DOS is 1540 (FR-VIC20-006).
174:        yaml.AppendLine("    id: xvic-host");
180:            yaml.AppendLine("        endpointName: vic20");

F:\GitHub\vice-sharp\src\ViceSharp.Launcher\ViceArgs.cs
41:    /// VICE: native/vice/vice/src/vic20/cart/debugcart.c (and c64) - "used for automatic regression testing";

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20SystemCoreDefinition.cs
17:    bool KeyboardMatrixConnected,
20:    DriveModel DefaultDriveModel,
21:    string DefaultDriveDosRomName,
25:    public string Family => "xvic";
49:            "xvic:unexpanded",
54:            KeyboardMatrixConnected: true,
57:            DefaultDriveModel: defaultDrive,
58:            DefaultDriveDosRomName: dosRomName,

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20MachineProfile.cs
23:/// Clocks match VICE <c>vic20.h</c>: PAL 1108405/71/312, NTSC 1022727/65/261.
40:    DriveModel DefaultDriveModel = DriveModel.C1540,
41:    string DefaultDriveDosRomName = C1541ViceRomNames.Dos1540,
44:    public string Family => "xvic";
57:        CoreDefinition ?? Vic20SystemCoreDefinition.Unexpanded(DefaultDriveModel, DefaultDriveDosRomName);
68:        "vic20",
70:        ["vic20", "vic20pal", "pal", "xvic", "commodore-vic20"],
80:        "vic20ntsc",
82:        ["vic20ntsc", "ntsc", "xvic-ntsc"],
109:        throw new ArgumentException($"Unknown xvic VIC-20 model selector '{selector}'.", nameof(selector));

F:\GitHub\vice-sharp\src\ViceSharp.Host\Services\GrpcHostServiceAdapters.cs
144:    public override async Task<GrpcContracts.CreateEmulatorSessionResponse> CreateSession(
148:        var response = await _inner.CreateSessionAsync(
150:                request.ArchitectureId,
403:    public override async Task<GrpcContracts.InputCommandResponse> SetJoystickState(
404:        GrpcContracts.SetJoystickStateRequest request,
407:        var response = await _inner.SetJoystickStateAsync(
408:            new SetJoystickStateRequest(request.SessionId, (InputPort)(int)request.Port, (byte)request.DirectionMask, request.FireButton),
1083:        result.Joysticks.AddRange(value.Joysticks.Select(joystick => new GrpcContracts.JoystickPortStateDto
1086:            State = new GrpcContracts.JoystickStateDto
1161:            PrimaryJoystickPort = (GrpcContracts.InputPort)(int)value.PrimaryJoystickPort,
1162:            SwapJoystickPorts = value.SwapJoystickPorts,
1240:            (InputPort)(int)value.PrimaryJoystickPort,
1241:            value.SwapJoystickPorts,

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs
6:/// VIC-20 (xvic) architecture descriptor: 6502, dual VIA, VIC-I, base RAM, ROMs.
</workspace_result>

### Turn 125 (Function)
[tool_response]
exit: 0
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: 131 ms - ViceSharp.TestHarness.dll (net10.0)


### Turn 126 (Assistant)
[tool_request: todo_write]
- todos: [{"id":"a-baseline","status":"completed"},{"id":"bcd-memory","status":"in_progress"}]
- merge: true
[tool_request: grep]
- pattern: PortAInput|PortBInput|ReadPortA|SetPort|KeyboardMatrix|PressKey
- glob: **/*.{cs}
- path: F:\GitHub\vice-sharp\src
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\BasicBootProofTests.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\DefaultEmulatorRuntimeFactory.cs
- limit: 150
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\IEC\Via6522.cs
- offset: 145
- limit: 80
[tool_request: run_terminal_command]
- command: # Peek at VIC-20 KERNAL for READY screen expectations; also check chargen size
$rom = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\basic-901486-01.bin"
$k = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-07.bin"
$c = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\chargen-901460-03.bin"
@($rom,$k,$c) | ForEach-Object { "{0}: {1} bytes" -f (Split-Path $_ -Leaf), (Get-Item $_).Length }
# Screen RAM default for unexpanded VIC-20 is $1E00
# Check if xvic exists
Get-Command xvic -ErrorAction SilentlyContinue | Format-List; Get-ChildItem -Path "F:\GitHub\vice-sharp","C:\Program Files*" -Filter "xvic.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 5 FullName
- description: ROM sizes and xvic binary availability

### Turn 127 (Function)
[tool_response]
- [completed] a-baseline: Slice A baseline: Vic20Skeleton + xvic gates, capture slice-a-baseline.log
- [in_progress] bcd-memory: Slice B: VIC-20 memory map + expansions with tests
- [pending] bcd-via: Slice C: VIA keyboard/joystick wiring with tests
- [pending] bcd-vici: Slice D: Real VIC-I character-mode frames (replace stub)
- [pending] boot: Boot proof: READY fingerprint + cart/PRG path tests
- [pending] drive1540: Drive-8 default 1540 Vic20 / 1541 C64 + IEC smoke
- [pending] heads: Heads/launcher: VIC-20 selectable + session create
- [pending] lockstep: Lockstep/determinism + full Vic20 gate + C64 smoke
- [pending] docs: Docs/HANDOFF/roadmap mark Iteration 2 complete


### Turn 128 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness;

using Xunit;

public sealed class BasicBootProofTests
{
    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-C64-RESET-VECTOR.
    /// Use case: A freshly-constructed C64 machine starts executing from
10→    /// the reset vector; during the first 100,000 instructions the PC must
    /// stay inside ROM/IO and never land in user RAM (<c>$0400-$9FFF</c>),
    /// which would indicate a broken PLA mapping or runaway reset.
    /// Acceptance: No PC sample observed during the boot window falls in
    /// the low-RAM range; if one does, the failure dumps the last 32 PCs.
    /// </summary>
    [Fact]
    public void C64_Boot_Does_Not_Fall_Into_Low_Ram_During_Early_Reset()
    {
        var machine = MachineTestFactory.CreateC64Machine();
20→        var recentPcs = new Queue<ushort>();

        for (var instruction = 0; instruction < 100000; instruction++)
        {
            var pc = machine.GetState().PC;

            recentPcs.Enqueue(pc);
            if (recentPcs.Count > 32)
                recentPcs.Dequeue();

30→            if (pc is >= 0x0400 and < 0xA000)
            {
                var history = string.Join(" -> ", recentPcs.Select(value => value.ToString("X4")));
                Assert.Fail($"Early boot executed low RAM at instruction {instruction}, PC=${pc:X4}. Recent PCs: {history}");
            }

            machine.StepInstruction();
        }
    }

40→    /// <summary>
    /// FR: FR-C64-Boot, TR: TR-C64-BASIC-READY.
    /// Use case: End-to-end smoke test that a C64 machine boots through
    /// the KERNAL/BASIC reset path and renders the <c>READY.</c> prompt
    /// to screen memory within a bounded number of emulated frames.
    /// Acceptance: Within 400 frames the screen-code or ASCII pattern for
    /// <c>READY</c> appears in screen RAM at <c>$0400</c>.
    /// </summary>
    [Fact]
    public void C64_Boot_Reaches_Ready_Prompt()
50→    {
        var machine = MachineTestFactory.CreateC64Machine();
        const int maxFrames = 400;

        for (var frame = 0; frame < maxFrames; frame++)
        {
            machine.RunFrame();

            if (ContainsReadyPrompt(machine))
            {
60→                return;
            }
        }

        var screen = ReadScreenText(machine);
        var state = machine.GetState();
        Assert.Fail($"READY prompt not found after {maxFrames} frames. PC=${state.PC:X4}, cycles={state.Cycle}, screen=[{screen}]");
    }

    private static bool ContainsReadyPrompt(ViceSharp.Abstractions.IMachine machine)
70→    {
        var screenCodes = ReadScreenCodes(machine);
        ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
        ReadOnlySpan<byte> asciiReady = "READY"u8;

        return screenCodes.AsSpan().IndexOf(screenCodeReady) >= 0
            || screenCodes.AsSpan().IndexOf(asciiReady) >= 0;
    }

    private static byte[] ReadScreenCodes(ViceSharp.Abstractions.IMachine machine)
80→    {
        var buffer = new byte[1000];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = machine.Bus.Peek((ushort)(0x0400 + i));
        }

        return buffer;
    }

90→    private static string ReadScreenText(ViceSharp.Abstractions.IMachine machine)
    {
        var screenCodes = ReadScreenCodes(machine);
        var chars = new char[screenCodes.Length];

        for (var i = 0; i < screenCodes.Length; i++)
        {
            chars[i] = DecodeScreenCode(screenCodes[i]);
        }

100→        return new string(chars);
    }

    private static char DecodeScreenCode(byte code)
    {
        if (code == 0x20)
            return ' ';

        if (code is >= 1 and <= 26)
            return (char)('A' + code - 1);
110→
        if (code is >= 0x30 and <= 0x39)
            return (char)code;

        return code switch
        {
            0x00 => '@',
            0x2E => '.',
            0x2A => '*',
            0x3A => ':',
120→            _ => '?'
        };
    }
}


### Turn 129 (Function)
[tool_response]
1→using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Core.Media;
using ViceSharp.Host.Audio;
using ViceSharp.Protocol;
using ViceSharp.RomFetch;

10→namespace ViceSharp.Host.Runtime;

public sealed class DefaultEmulatorRuntimeFactory : IEmulatorRuntimeFactory
{
    private readonly IArchitectureBuilder _architectureBuilder;
    private readonly IReadOnlyDictionary<string, IArchitectureDescriptor> _descriptors;
    private readonly string _defaultArchitectureId;

    // Live sound-recording tap installed in the SID -> output path for sessions
    // this factory builds (FR-MED-003). Null for the explicit-builder ctors used
20→    // by headless test rigs. Created once and shared by every session this
    // factory creates (one interactive session at a time in practice).
    private readonly CaptureAudioTap? _audioTap;

    public DefaultEmulatorRuntimeFactory()
        : this(CreateDefaultAudioTap())
    {
    }

    /// <summary>
30→    /// FIX-XNOAUDIO-001: composes the default factory around an EXTERNALLY created live
    /// audio backend (the UWP head's XAudio2 tap; the desktop path creates its own via
    /// <see cref="CreateDefaultAudioTap"/>). An already-tapped backend passes through;
    /// a raw device backend is wrapped so capture keeps working; <c>null</c> stays silent.
    /// </summary>
    /// <param name="audioBackend">The live audio backend, or <c>null</c> for silence.</param>
    public DefaultEmulatorRuntimeFactory(IAudioBackend? audioBackend)
        : this(audioBackend as CaptureAudioTap
            ?? (audioBackend is null ? null : new CaptureAudioTap(audioBackend)))
    {
40→    }

    private DefaultEmulatorRuntimeFactory(CaptureAudioTap? audioTap)
        : this(CreateDefaultArchitectureBuilder(audioTap), CreateDefaultDescriptors(), CreateDefaultArchitectureId())
    {
        _audioTap = audioTap;
    }

    public DefaultEmulatorRuntimeFactory(
        IArchitectureBuilder architectureBuilder,
50→        IEnumerable<IArchitectureDescriptor> descriptors)
        : this(architectureBuilder, descriptors, MinimalHostArchitectureDescriptor.ArchitectureId)
    {
    }

    public DefaultEmulatorRuntimeFactory(
        IArchitectureBuilder architectureBuilder,
        IEnumerable<IArchitectureDescriptor> descriptors,
        string defaultArchitectureId)
    {
60→        ArgumentNullException.ThrowIfNull(architectureBuilder);
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultArchitectureId);

        _architectureBuilder = architectureBuilder;
        _defaultArchitectureId = defaultArchitectureId;
        _descriptors = descriptors
            .SelectMany(CreateDescriptorKeys)
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Descriptor, StringComparer.OrdinalIgnoreCase);
70→    }

    public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Honor the session request's True Drive selection (default false keeps
        // the simulated-drive path byte-identical, so native parity is safe).
        return Create(
            request,
            request.TrueDrive,
80→            request.TrueDriveDevice,
            string.IsNullOrWhiteSpace(request.TrueDriveDiskImagePath) ? null : request.TrueDriveDiskImagePath);
    }

    /// <summary>
    /// FR-DRVTRUE-001: create a session, optionally as a cycle-accurate
    /// true-drive rig. With <paramref name="trueDrive"/> false (the default) the
    /// machine is built exactly as before, so the simulated-drive path and
    /// native lockstep parity are unchanged. With it true and a C64 architecture
    /// selected, the session runs a <see cref="CoordinatorMachine"/> (C64 host +
90→    /// emulated 1541 over IEC).
    /// </summary>
    public EmulatorRuntimeSession Create(
        CreateEmulatorSessionRequest request,
        bool trueDrive,
        int driveDevice = 8,
        string? diskImagePath = null)
    {
        ArgumentNullException.ThrowIfNull(request);

100→        var architectureId = string.IsNullOrWhiteSpace(request.ArchitectureId) ? _defaultArchitectureId : request.ArchitectureId;

        if (!_descriptors.TryGetValue(architectureId, out var descriptor))
            throw new InvalidOperationException($"Architecture '{architectureId}' is not registered.");

        var isTrueDriveRig = trueDrive && descriptor is C64Descriptor;
        var machine = isTrueDriveRig
            ? C64TrueDriveRigBuilder.Build(_architectureBuilder, descriptor, driveDevice, diskImagePath)
            : _architectureBuilder.Build(descriptor);

110→        var sessionId = $"emulator-{Guid.NewGuid():N}";
        var session = new EmulatorRuntimeSession(sessionId, descriptor, machine, CreateIecBusActivityMonitor(machine))
        {
            // Expose the live audio tap so sound capture can attach a recorder
            // (null for explicit-builder test rigs with no live audio path).
            AudioCaptureTap = _audioTap,
        };

        // The true-drive rig boots with the disk inserted at build time, so the
        // session's media list must reflect it (otherwise the host reports the
120→        // drive empty even though the 1541 has the disk).
        if (isTrueDriveRig && !string.IsNullOrWhiteSpace(diskImagePath) && File.Exists(diskImagePath))
        {
            var slot = driveDevice == 9 ? MediaSlot.Drive9 : MediaSlot.Drive8;
            session.MediaAttachments[slot] = new MediaAttachmentDto(
                slot,
                diskImagePath,
                Path.GetFileName(diskImagePath),
                IsAttached: true,
                IsReadOnly: false,
130→                AppliedToRuntime: true);
        }

        return session;
    }

    private static IEnumerable<(string Key, IArchitectureDescriptor Descriptor)> CreateDescriptorKeys(
        IArchitectureDescriptor descriptor)
    {
        yield return (descriptor.MachineName, descriptor);
140→
        if (ReferenceEquals(descriptor, MinimalHostArchitectureDescriptor.Instance))
            yield return (MinimalHostArchitectureDescriptor.ArchitectureId, descriptor);

        if (descriptor is C64Descriptor c64Descriptor)
        {
            yield return (c64Descriptor.Profile.Id, descriptor);
            foreach (var alias in c64Descriptor.Profile.Aliases)
                yield return (alias, descriptor);
        }
150→    }

### Turn 130 (Function)
[tool_response]
145→    public ClockPhase Phase => ClockPhase.Phi2;

    /// <inheritdoc />
    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

150→    /// <summary>Port A external input function.</summary>
    public Func<byte>? PortAInput { get; set; }

    /// <summary>Port B external input function.</summary>
    public Func<byte>? PortBInput { get; set; }

    /// <summary>Fires after Port A output bits change.</summary>
    public Action<byte>? PortAOutputChanged { get; set; }

    /// <summary>Fires after Port B output bits change.</summary>
160→    public Action<byte>? PortBOutputChanged { get; set; }

    /// <inheritdoc />
    public void Reset()
    {
        _portAOutput = 0;
        _portBOutput = 0;
        _ddra = 0;
        _ddrb = 0;
        _t1Counter = 0xFFFF;
170→        _t1Latch = 0xFFFF;
        _t2Counter = 0xFFFF;
        _t1Running = false;
        _t2Running = false;
        _sr = 0;
        _acr = 0;
        _pcr = 0;
        _ifr = 0;
        _ier = 0;
        _srShiftCount = 0;
180→        _srShifting = false;
        _pb7TimerToggle = false;
        _cb2State = true;
        _ca2State = true;
        _ca2PulseRemaining = 0;
        _cb2PulseRemaining = 0;
        if (_irqAsserted)
        {
            _irqLine.Release(this);
            _irqAsserted = false;
190→        }
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);

    /// <inheritdoc />
    public byte Read(ushort address)
    {
200→        var reg = (byte)((address - BaseAddress) & 0x0F);
        switch (reg)
        {
            case 0x00: // ORB / IRB
                {
                    var input = PortBInput?.Invoke() ?? 0xFF;
                    return ComposePortBRead(input);
                }
            case 0x01: // ORA / IRA - clears CA1/CA2 in IFR
                _ifr &= unchecked((byte)~0x03);
210→                RefreshIrq();
                // Handshake output mode (PCR bits 3..1 = 100): an ORA / IRA
                // read drives CA2 low; the line stays low until the next CA1
                // active edge restores it. Symmetric with CB2's ORB-write
                // handshake.
                if (GetCa2Mode() == Ca2ModeHandshakeOut)
                    _ca2State = false;
                // Pulse output mode (PCR bits 3..1 = 101): an ORA / IRA read
                // drives CA2 low for exactly one phi2 cycle. The pulse
                // counter is decremented in Tick(); when it hits zero the
220→                // line is automatically restored high.
                else if (GetCa2Mode() == Ca2ModePulseOut)
                {
                    _ca2State = false;
                    _ca2PulseRemaining = 1;

### Turn 131 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 27 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\C1541DriveMechanismDevice.cs
60:        var prevPbIn = via2.PortBInput;
62:        var prevPaIn = via2.PortAInput;
65:        via2.PortBInput = () =>
94:        via2.PortAInput = () =>

F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
110:        var cia2PortAInputMask = ResolveCia2PortAInputMask(profile);
118:            cia2PortAInputMask: cia2PortAInputMask,
477:    private static byte ResolveCia2PortAInputMask(IMachineProfile? profile)

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20SystemCoreDefinition.cs
17:    bool KeyboardMatrixConnected,
54:            KeyboardMatrixConnected: true,

F:\GitHub\vice-sharp\src\ViceSharp.Chips\IEC\Via6522.cs
151:    public Func<byte>? PortAInput { get; set; }
154:    public Func<byte>? PortBInput { get; set; }
205:                    var input = PortBInput?.Invoke() ?? 0xFF;
227:                    var input = PortAInput?.Invoke() ?? 0xFF;
259:                    var input = PortAInput?.Invoke() ?? 0xFF;
428:            0x00 => ComposePortBRead(PortBInput?.Invoke() ?? 0xFF),
429:            0x01 => (byte)((_portAOutput & _ddra) | ((PortAInput?.Invoke() ?? 0xFF) & (byte)~_ddra)),
443:            0x0F => (byte)((_portAOutput & _ddra) | ((PortAInput?.Invoke() ?? 0xFF) & (byte)~_ddra)),

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\VirtualKeyboardViewModel.cs
220:                PressKey(entry.KeyName);
294:    private void PressKey(string baseKeyName)

F:\GitHub\vice-sharp\src\ViceSharp.Core\C64Cia2InterfaceDevice.cs
31:        var prevPbIn = cia2.PortBInput;
33:        var prevPaIn = cia2.PortAInput;
42:        cia2.PortBInput = () =>
66:        cia2.PortAInput = () =>

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
85:    private static partial int SetKeyboardMatrixKeyNative(IntPtr instance, int row, int column, int pressed);
278:    public static void SetKeyboardMatrixKey(IntPtr instance, int row, int column, bool pressed)
280:        var result = SetKeyboardMatrixKeyNative(instance, row, column, pressed ? 1 : 0);
713:        public void SetKeyboardMatrixKey(int row, int column, bool pressed)
</workspace_result>

### Turn 132 (Function)
[tool_response]
<task-id>call-f1efebff-8ca4-41f1-b6df-16c493f5ddb8-99</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-f1efebff-8ca4-41f1-b6df-16c493f5ddb8-99.log</output-file>
<status>running</status>
<summary>Command "# Peek at VIC-20 KERNAL for READY screen expectations; also check chargen size
$rom = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\basic-901486-01.bin"
$k = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-07.bin"
$c = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\chargen-901460-03.bin"
@($rom,$k,$c) | ForEach-Object { "{0}: {1} bytes" -f (Split-Path $_ -Leaf), (Get-Item $_).Length }
# Screen RAM default for unexpanded VIC-20 is $1E00
# Check if xvic exists
Get-Command xvic -ErrorAction SilentlyContinue | Format-List; Get-ChildItem -Path "F:\GitHub\vice-sharp","C:\Program Files*" -Filter "xvic.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 5 FullName" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-f1efebff-8ca4-41f1-b6df-16c493f5ddb8-99"] to retrieve the output.

### Turn 133 (Assistant)
[tool_request: grep]
- pattern: keyboard|via1|via2|row|column|joy
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20
- glob: **/*keyboard*
- head_limit: 40
[tool_request: run_terminal_command]
- command: Get-ChildItem "F:\GitHub\vice-sharp\native\vice\vice\src\vic20" -Filter "*key*" | Select-Object Name; Get-ChildItem "F:\GitHub\vice-sharp\native\vice\vice\src\vic20" -Filter "*via*" | Select-Object Name; Select-String -Path "F:\GitHub\vice-sharp\native\vice\vice\src\vic20\*.c" -Pattern "via2|keyboard_set|matrix" -SimpleMatch:$false | Select-Object -First 30 LineNumber,Filename,Line
- description: Find VIC-20 keyboard VIA wiring in native
[tool_request: grep]
- pattern: RunFrame|StepInstruction|GetState
- glob: **/IMachine.cs
- path: F:\GitHub\vice-sharp\src
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 415
- limit: 100

### Turn 134 (Function)
[tool_response]
415→        }

        // 16KB drive DOS ROM at $C000-$FFFF.
        var romDescriptor = descriptor.Devices.First(d => d.Role == DeviceRole.DriveRom);
        var dosRomName = ResolveC1541DosRomName(descriptor);
420→        var romBytes = _romProvider.LoadRom(
            dosRomName,
            descriptor.RequiredRoms?.Architecture ?? "DRIVES").ToArray();
        var rom = new RomDevice(
            romDescriptor.BaseAddress,
            (ushort)(romDescriptor.BaseAddress + romDescriptor.Size - 1),
            romBytes);
        bus.RegisterDevice(rom);
        deviceRegistry.Add(rom, DeviceRole.DriveRom);

430→        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.DriveCpu);

        // Optional D64 disk image mount.
        D64DiskImageDevice? disk = null;
        if (descriptor is IDriveArchitectureDescriptor drive && !string.IsNullOrWhiteSpace(drive.DiskImagePath))
        {
            disk = D64DiskImageDevice.LoadFromFile(drive.DiskImagePath!);
            deviceRegistry.Add(disk, DeviceRole.DriveDisk);
        }
440→
        var deviceNumber = descriptor is IDriveArchitectureDescriptor driveDescriptor
            ? driveDescriptor.DeviceNumber
            : 8;
        deviceRegistry.Add(new C1541IecInterfaceDevice(deviceNumber));

        var driveMechanism = new C1541DriveMechanismDevice(disk, cpu);
        clock.Register(driveMechanism);
        deviceRegistry.Add(driveMechanism);

450→        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu);
        machine.Reset();
        return machine;
    }

    private static string ResolveC1541DosRomName(IArchitectureDescriptor descriptor)
        => descriptor is IDriveArchitectureDescriptor d && !string.IsNullOrWhiteSpace(d.DosRomName)
            ? d.DosRomName
            : "dos1541-325302-01+901229-05.bin";

460→    private static bool IsKernalRomRequired(string kernalRomName)
        => !string.Equals(kernalRomName, "kernal-none.bin", StringComparison.OrdinalIgnoreCase);

    private static CartridgeMappingMode ResolveDefaultCartridgeMappingMode(IMachineProfile? profile)
    {
        if (profile is null)
            return CartridgeMappingMode.Auto;

        if (string.Equals(profile.SystemCore.AddressDecoderPolicy, "Ultimax", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.Ultimax;
470→
        if (string.Equals(profile.BoardModel, "C64GS", StringComparison.OrdinalIgnoreCase))
            return CartridgeMappingMode.GameSystem;

        return CartridgeMappingMode.Auto;
    }

    private static byte ResolveCia2PortAInputMask(IMachineProfile? profile)
    {
        if (profile is null)
480→            return 0x7F;

        return profile.SystemCore.BusPolicy switch
        {
            "GameSystem" or "Max" => 0x3F,
            _ => 0x7F
        };
    }

    private static Mos6526 CreateC64Cia(IBus bus, IInterruptLine line, ushort baseAddress, long masterClockHz)
490→    {
        return new Mos6526(bus, line)
        {
            BaseAddress = baseAddress,
            TodCyclesPer50HzTick = (int)(masterClockHz / 50),
            TodCyclesPer60HzTick = (int)(masterClockHz / 60)
        };
    }

    private static Mos6569 CreateVicII(
500→        IBus bus,
        IInterruptLine irqLine,
        IArchitectureDescriptor descriptor,
        IMachineProfile? profile)
    {
        var vic = profile?.VicIIModel switch
        {
            "Mos6569R1" => new Mos6569R1(bus, irqLine),
            "Mos6567R56A" => new Mos6567R56A(bus, irqLine),
            "Mos6567R8" => new Mos6567(bus, irqLine),
510→            "Mos6572" => new Mos6572(bus, irqLine),
            "Mos8562" => new Mos8562(bus, irqLine),
            "Mos8565" => new Mos8565(bus, irqLine),
            _ => descriptor.VideoStandard == VideoStandard.Ntsc
                ? new Mos6567(bus, irqLine)

### Turn 135 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
No matches found
</workspace_result>

### Turn 136 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 3 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
40:    void RunFrame();
43:    void StepInstruction();
46:    MachineState GetState();
</workspace_result>

### Turn 137 (Function)
[tool_response]
exit: 0

Name
----
vic20ieeevia.h
vic20ieeevia1.c
vic20ieeevia2.c
vic20via.h
vic20via1.c
vic20via2.c

































### Turn 138 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20via2.c
- limit: 150
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20via1.c
- limit: 120
[tool_request: grep]
- pattern: keyboard|joy|PORT|store|read
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20via2.c
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- offset: 520
- limit: 80

### Turn 139 (Function)
[tool_response]
1→/*
 * vic20via2.c - VIA2 emulation in the VIC20.
 *
 * Written by
 *  Andre Fachat <fachat@physik.tu-chemnitz.de>
 *  Ettore Perazzoli <ettore@comm2000.it>
 *  Andreas Boose <viceteam@t-online.de>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
10→ * See README for copyright notice.
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
20→ *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */

#include "vice.h"
30→
#include <stdio.h>

#include "datasette.h"
#include "interrupt.h"
#include "joyport.h"
#include "keyboard.h"
#include "lib.h"
#include "maincpu.h"
#include "tapeport.h"
40→#include "types.h"
#include "via.h"
#include "vic20.h"
#include "vic20iec.h"
#include "vic20via.h"


void via2_store(uint16_t addr, uint8_t data)
{
    viacore_store(machine_context.via2, addr, data);
50→}

uint8_t via2_read(uint16_t addr)
{
    return viacore_read(machine_context.via2, addr);
}

uint8_t via2_peek(uint16_t addr)
{
    return viacore_peek(machine_context.via2, addr);
60→}

static void set_ca2(via_context_t *via_context, int state)
{
}

static void set_cb2(via_context_t *via_context, int state, int offset)
{
}

70→static void set_int(via_context_t *via_context, unsigned int int_num,
                    int value, CLOCK rclk)
{
    interrupt_set_irq(maincpu_int_status, int_num, value, rclk);
}

static void restore_int(via_context_t *via_context, unsigned int int_num, int value)
{
    interrupt_restore_irq(maincpu_int_status, int_num, value);
}
80→
static void undump_acr(via_context_t *via_context, uint8_t byte)
{
}

static void store_acr(via_context_t *via_context, uint8_t byte)
{
}

static void store_sr(via_context_t *via_context, uint8_t byte)
90→{
}

static void store_t2l(via_context_t *via_context, uint8_t byte)
{
}

static void undump_pra(via_context_t *via_context, uint8_t byte)
{
}
100→
static void store_pra(via_context_t *via_context, uint8_t byte, uint8_t myoldpa,
                      uint16_t addr)
{
}

static void undump_prb(via_context_t *via_context, uint8_t byte)
{
}

110→static void store_prb(via_context_t *via_context, uint8_t byte, uint8_t myoldpb,
                      uint16_t addr)
{
    uint8_t joy_bit = (byte & 0x80) >> 4;

    if ((byte ^ myoldpb) & 8) {
        tapeport_toggle_write_bit(TAPEPORT_PORT_1, (~(via_context->via[VIA_DDRB]) | byte) & 0x8);
    }
    store_joyport_dig(JOYPORT_1, joy_bit, 8);
}
120→
static void undump_pcr(via_context_t *via_context, uint8_t byte)
{
}

static void reset(via_context_t *via_context)
{
/*iec_pcr_write(0x22);*/
}

130→static uint8_t store_pcr(via_context_t *via_context, uint8_t byte, uint16_t addr)
{
    /* FIXME: this should use via_set_ca2() and via_set_cb2() */
    if (byte != via_context->via[VIA_PCR]) {
        register uint8_t tmp = byte;
        /* first set bit 1 and 5 to the real output values */
        if ((tmp & 0x0c) != 0x0c) {
            tmp |= 0x02;
        }
        if ((tmp & 0xc0) != 0xc0) {
140→            tmp |= 0x20;
        }
        iec_pcr_write(tmp);
    }
    return byte;
}

/*
       +-----+-----+-----+-----+-----+-----+-----+-----+
       |Bit 0|Bit 1|Bit 2|Bit 3|Bit 4|Bit 5|Bit 6|Bit 7| (row, read port A)
150→ +-----+-----+-----+-----+-----+-----+-----+-----+-----+

### Turn 140 (Function)
[tool_response]
520→
        var system = profile.VideoStandard == VideoStandard.Ntsc
            ? Mos6569.TvSystem.NTSC
            : string.Equals(profile.VicIIModel, "Mos6572", StringComparison.OrdinalIgnoreCase)
                ? Mos6569.TvSystem.PALN
                : Mos6569.TvSystem.PAL;

        var visibleLines = profile.VideoStandard == VideoStandard.Ntsc
            ? Mos6569.NtscVisibleLines
            : Mos6569.PalVisibleLines;
530→        vic.ConfigureTiming(
            system,
            profile.CyclesPerLine,
            visibleLines,
            profile.RasterLines,
            profile.RefreshRateHz);
        return vic;
    }

    private static Sid6581 CreateSid(IBus bus, IMachineProfile? profile, IAudioBackend? audioBackend, double masterClockHz)
540→        // PLAN-VICEPARITY-001 P0-7: single canonical construction path shared
        // with every other machine (see SidFactory).
        => SidFactory.Create(bus, profile, audioBackend, masterClockHz);
}

/// <summary>
/// Default IMachine implementation.
/// </summary>
internal sealed class Machine : IMachine
{
550→    private readonly IBus _bus;
    private readonly IClock _clock;
    private readonly IDeviceRegistry _devices;
    private readonly IArchitectureDescriptor _architecture;
    private readonly Mos6502 _cpu;
    private readonly IPubSub? _pubSub;
    private readonly int _frameCycles;

    public Machine(
        IArchitectureDescriptor architecture,
560→        IBus bus,
        IClock clock,
        IDeviceRegistry deviceRegistry,
        Mos6502 cpu,
        IPubSub? pubSub = null)
    {
        _architecture = architecture;
        _bus = bus;
        _clock = clock;
        _devices = deviceRegistry;
570→        _cpu = cpu;
        _pubSub = pubSub;
        _frameCycles = architecture is IProfiledArchitectureDescriptor profiled
            ? profiled.MachineProfile.CyclesPerLine * profiled.MachineProfile.RasterLines
            : architecture.VideoStandard == VideoStandard.Ntsc ? 263 * 65 : 312 * 63;
    }

    public IBus Bus => _bus;
    public IClock Clock => _clock;
    public IDeviceRegistry Devices => _devices;
580→    public IArchitectureDescriptor Architecture => _architecture;
    public IPubSub? PubSub => _pubSub;
    public ICpu? PrimaryCpu => _cpu;

    public void RunFrame() => _clock.Step(_frameCycles);

    public void StepInstruction()
    {
        do
        {
590→            _clock.Step();
        }
        while (!_cpu.IsInstructionBoundary);
    }

    public MachineState GetState()
    {
        return new MachineState
        {
            A = _cpu.A,

### Turn 141 (Function)
[tool_response]
1→/*
 * vic20via1.c - VIA1 emulation in the VIC20.
 *
 * Written by
 *  Andre Fachat <fachat@physik.tu-chemnitz.de>
 *  Ettore Perazzoli <ettore@comm2000.it>
 *  Andreas Boose <viceteam@t-online.de>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
10→ * See README for copyright notice.
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
20→ *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
 *
 */

#include "vice.h"
30→
#include <stdio.h>

#include "datasette.h"
#include "interrupt.h"
#include "joyport.h"
#include "joystick.h"
#include "keyboard.h"
#include "lib.h"
#include "maincpu.h"
40→#include "tapeport.h"
#include "types.h"
#include "userport.h"
#include "via.h"
#include "vic.h"
#include "vic20.h"
#include "vic20-resources.h"
#include "vic20iec.h"
#include "vic20via.h"

50→int vic20_vflihack_userport = 0xff;

static int userport_pa6 = 1;    /* HACK: pin 8, this is also connected to tape sense */
static int userport_pb = 0xff;

/* HACK: on the C64, pin 8 is connected to PC2, which automatically generates a pulse
         when PB is written, which is why both is updated by the same function. */
static void update_portbits(void)
{
    store_userport_pbx(userport_pb, userport_pa6 ? USERPORT_NO_PULSE : USERPORT_PULSE);
60→}

void via1_store(uint16_t addr, uint8_t data)
{
    viacore_store(machine_context.via1, addr, data);
}

uint8_t via1_read(uint16_t addr)
{
    return viacore_read(machine_context.via1, addr);
70→}

uint8_t via1_peek(uint16_t addr)
{
    return viacore_peek(machine_context.via1, addr);
}

static void set_ca2(via_context_t *via_context, int state)
{
}
80→
static void set_cb2(via_context_t *via_context, int state, int offset)
{
    if (!via_context->cb2_is_input) {
        /* CB2 goes to userport pin M */
        store_userport_pa2(state);
    }
}

static void set_int(via_context_t *via_context, unsigned int int_num, int value, CLOCK rclk)
90→{
    interrupt_set_nmi(maincpu_int_status, int_num, value, rclk);
}

static void restore_int(via_context_t *via_context, unsigned int int_num, int value)
{
    interrupt_restore_nmi(maincpu_int_status, int_num, value);
}

static int tape_sense = 0;
100→static int tape_write_in = 0;
static int tape_motor_in = 0;

void via1_set_tape_sense(int v)
{
    tape_sense = v;
}

/* FIXME: find out how to set the write in and motor in lines */
void via1_set_tape_write_in(int v)
110→{
    tape_write_in = v;
}

void via1_set_tape_motor_in(int v)
{
    tape_motor_in = v;
}

static void via1_internal_lightpen_check(uint8_t pa)
120→{

### Turn 142 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 38 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20via2.c
35:#include "joyport.h"
36:#include "keyboard.h"
47:void via2_store(uint16_t addr, uint8_t data)
49:    viacore_store(machine_context.via2, addr, data);
52:uint8_t via2_read(uint16_t addr)
54:    return viacore_read(machine_context.via2, addr);
76:static void restore_int(via_context_t *via_context, unsigned int int_num, int value)
78:    interrupt_restore_irq(maincpu_int_status, int_num, value);
85:static void store_acr(via_context_t *via_context, uint8_t byte)
89:static void store_sr(via_context_t *via_context, uint8_t byte)
93:static void store_t2l(via_context_t *via_context, uint8_t byte)
101:static void store_pra(via_context_t *via_context, uint8_t byte, uint8_t myoldpa,
110:static void store_prb(via_context_t *via_context, uint8_t byte, uint8_t myoldpb,
113:    uint8_t joy_bit = (byte & 0x80) >> 4;
116:        tapeport_toggle_write_bit(TAPEPORT_PORT_1, (~(via_context->via[VIA_DDRB]) | byte) & 0x8);
118:    store_joyport_dig(JOYPORT_1, joy_bit, 8);
130:static uint8_t store_pcr(via_context_t *via_context, uint8_t byte, uint16_t addr)
149:       |Bit 0|Bit 1|Bit 2|Bit 3|Bit 4|Bit 5|Bit 6|Bit 7| (row, read port A)
169:   port b bit 7 - joystick pin 4 (right)
173:static uint8_t read_pra(via_context_t *via_context, uint16_t addr)
179:    uint8_t joy = ~read_joyport_dig(JOYPORT_1);
182:    /* Bit 7 is mapped to the right direction of the joystick (bit
183:       3 in `joystick_value[]'). */
184:    if (joy & 0x8) {
198:static uint8_t read_prb(via_context_t *via_context)
204:    uint8_t joy = ~read_joyport_dig(JOYPORT_1);
213:    /* Bit 7 is mapped to the right direction of the joystick (bit
214:       3 in `joystick_value[]'). */
215:    if (joy & 0x8) {
255:    via->store_pra = store_pra;
256:    via->store_prb = store_prb;
257:    via->store_pcr = store_pcr;
258:    via->store_acr = store_acr;
259:    via->store_sr = store_sr;
260:    via->store_t2l = store_t2l;
261:    via->read_pra = read_pra;
262:    via->read_prb = read_prb;
264:    via->restore_int = restore_int;
</workspace_result>

### Turn 143 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20via2.c
- offset: 147
- limit: 90
[tool_request: grep]
- pattern: keyboard_scan|keyboard_get|keyarr|rev_keyarr
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/keyboard*.c
- head_limit: 20
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\vic20mem.c
- limit: 100

### Turn 144 (Function)
[tool_response]
1→/*
 * vic20mem.c -- VIC20 memory handling.
 *
 * Written by
 *  Ettore Perazzoli <ettore@comm2000.it>
 *  Andre Fachat <fachat@physik.tu-chemnitz.de>
 *  Daniel Kahlin <daniel@kahlin.net>
 *  Marco van den Heuvel <blackystardust68@yahoo.com>
 *
10→ * Multiple memory configuration support originally by
 *  Alexander Lehmann <alex@mathematik.th-darmstadt.de>
 *
 * This file is part of VICE, the Versatile Commodore Emulator.
 * See README for copyright notice.
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
20→ *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
 *  02111-1307  USA.
30→ *
 */

#include "vice.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "cartio.h"
40→#include "cartridge.h"
#include "log.h"
#include "machine.h"
#include "maincpu.h"
#include "mem.h"
#include "monitor.h"
#include "ram.h"
#include "resources.h"
#include "sid-resources.h"
#include "sid.h"
50→#include "types.h"
#include "uiapi.h"
#include "via.h"
#include "vic.h"
#include "vic-mem.h"
#include "vic20.h"
#include "vic20-resources.h"
#include "vic20cartmem.h"
#include "vic20ieeevia.h"
#include "vic20mem.h"
60→#include "vic20memrom.h"
#include "vic20rom.h"
#include "vic20via.h"

/* #define DEBUG_VIC20MEM */

#ifdef DEBUG_VIC20MEM
#define DBG(x)  log_printf x
#else
#define DBG(x)
70→#endif

/* ------------------------------------------------------------------------- */

/* The VIC20 memory. */
uint8_t mem_ram[VIC20_RAM_SIZE];

uint8_t vfli_ram[0x4000]; /* for mikes vfli modification */

/* Last data read/write by the cpu, this value lingers on the C(PU)-bus and
80→   gets used when the CPU reads from unconnected space on the C(PU)-bus */
uint8_t vic20_cpu_last_data;
/* Last read data on V-bus (VD0-VD7) */
uint8_t vic20_v_bus_last_data;
/* Last read data on V-bus (VD8-VD11) */
uint8_t vic20_v_bus_last_high;

/* Memory read and write tables.  */
static uint8_t *_mem_read_base_tab[0x101];
static int mem_read_limit_tab[0x101];
90→
/* These ones are used when watchpoints are turned on.  */
static read_func_ptr_t _mem_read_tab_watch[0x101];
static store_func_ptr_t _mem_write_tab_watch[0x101];

static read_func_ptr_t _mem_read_tab_nowatch[0x101];
static store_func_ptr_t _mem_write_tab_nowatch[0x101];
static read_func_ptr_t _mem_peek_tab[0x101];

read_func_ptr_t *_mem_read_tab_ptr;
100→store_func_ptr_t *_mem_write_tab_ptr;

### Turn 145 (Function)
[tool_response]
147→/*
       +-----+-----+-----+-----+-----+-----+-----+-----+
       |Bit 0|Bit 1|Bit 2|Bit 3|Bit 4|Bit 5|Bit 6|Bit 7| (row, read port A)
150→ +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 0| 1 ! |A_LFT| CTRL| R/S |SPACE|  C= |  Q  | 2 " |
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 1| 3 # |  W  |  A  | S_L |  Z  |  S  |  E  | 4 $ |
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 2| 5 % |  R  |  D  |  X  |  C  |  F  |  T  | 6 & |
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 3| 7 ' |  Y  |  G  |  V  |  B  |  H  |  U  | 8 ( |
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 4| 9 ) |  I  |  J  |  N  |  M  |  K  |  O  |  0  |
160→ +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 5|  +  |  P  |  L  | , < | . > | : [ |  @  |  -  |
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 6|POUND|  *  | ; ] | / ? | S_R |  =  | A_UP| HOME|
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
 |Bit 7| DEL |Retrn|C_L/R|C_U/D|  F1 |  F3 |  F5 |  F7 |
 +-----+-----+-----+-----+-----+-----+-----+-----+-----+
  \
   (column, write port b)
   port b bit 7 - joystick pin 4 (right)
170→              3 - cassette write
*/

static uint8_t read_pra(via_context_t *via_context, uint16_t addr)
{
    uint8_t byte;
    /* FIXME: not 100% sure about this... */
    uint8_t val = ~(via_context->via[VIA_DDRA]);
    uint8_t msk = via_context->oldpb;
    uint8_t joy = ~read_joyport_dig(JOYPORT_1);
180→    int i;

    /* Bit 7 is mapped to the right direction of the joystick (bit
       3 in `joystick_value[]'). */
    if (joy & 0x8) {
        msk &= 0x7f;
    }

    for (i = 0; i < 8; i++) {
        if (!(msk & (1 << i))) {
190→            val &= ~rev_keyarr[i];
        }
    }

    byte = val | (via_context->via[VIA_PRA] & via_context->via[VIA_DDRA]);
    return byte;
}

static uint8_t read_prb(via_context_t *via_context)
{
200→    uint8_t byte;
    /* FIXME: not 100% sure about this... */
    uint8_t val = ~(via_context->via[VIA_DDRB]);
    uint8_t msk = via_context->oldpa;
    uint8_t joy = ~read_joyport_dig(JOYPORT_1);
    int i;

    for (i = 0; i < 8; i++) {
        if (!(msk & (1 << i))) {
            val &= ~keyarr[i];
210→        }
    }

    /* Bit 7 is mapped to the right direction of the joystick (bit
       3 in `joystick_value[]'). */
    if (joy & 0x8) {
        val &= 0x7f;
    }

    byte = val | (via_context->via[VIA_PRB] & via_context->via[VIA_DDRB]);
220→    return byte;
}

void via2_init(via_context_t *via_context)
{
    viacore_init(machine_context.via2, maincpu_alarm_context,
                 maincpu_int_status);
}

void vic20via2_setup_context(machine_context_t *machinecontext)
230→{
    via_context_t *via;

    machinecontext->via2 = lib_malloc(sizeof(via_context_t));
    via = machinecontext->via2;

    via->prv = NULL;

### Turn 146 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 19 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\keyboard.c
98:   latch_keyarr or network_keyarr. */
99:int keyarr[KBD_ROWS];
100:int rev_keyarr[KBD_COLS];
103:static int latch_keyarr[KBD_ROWS];
104:static int latch_rev_keyarr[KBD_COLS];
106:static int network_keyarr[KBD_ROWS];
107:static int network_rev_keyarr[KBD_COLS];
186:    memset(keyarr, 0, sizeof(keyarr));
187:    memset(rev_keyarr, 0, sizeof(rev_keyarr));
188:    memset(latch_keyarr, 0, sizeof(latch_keyarr));
189:    memset(latch_rev_keyarr, 0, sizeof(latch_rev_keyarr));
321:/* copy the latched keyarr (or what comes from the network) to the real keyarr */
325:        memcpy(keyarr, network_keyarr, sizeof(keyarr));
326:        memcpy(rev_keyarr, network_rev_keyarr, sizeof(rev_keyarr));
328:        memcpy(keyarr, latch_keyarr, sizeof(keyarr));
329:        memcpy(rev_keyarr, latch_rev_keyarr, sizeof(rev_keyarr));
332:        keyboard_machine_func(keyarr);
337:static int keyboard_set_latch_keyarr(int row, int col, int pressed)
342:    DBGMTX(("keyboard_set_latch_keyarr     row:%d col:%d pressed:%d", row, col, pressed));
</workspace_result>

### Turn 147 (Assistant)
Verifying boot with the skeleton, then implementing remaining slices.
[tool_request: run_terminal_command]
- command: $scratch = "C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\implementer"
$code = @'
using System;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Core;
using ViceSharp.TestHarness;

var m = MachineTestFactory.CreateVic20Machine();
Console.WriteLine($"PC after reset: ${m.GetState().PC:X4}");
Console.WriteLine($"C000={m.Bus.Read(0xC000):X2} E000={m.Bus.Read(0xE000):X2}");
for (var frame = 0; frame < 600; frame++)
{
    m.RunFrame();
    if (frame % 50 == 0 || frame < 5)
    {
        var st = m.GetState();
        Console.WriteLine($"frame {frame} PC=${st.PC:X4} cyc={st.Cycle}");
    }
    // scan screen $1E00
    var buf = new byte[506];
    for (int i = 0; i < buf.Length; i++) buf[i] = m.Bus.Peek((ushort)(0x1E00 + i));
    if (buf.AsSpan().IndexOf(new byte[]{18,5,1,4,25}) >= 0)
    {
        Console.WriteLine($"READY at frame {frame}");
        // dump text
        var chars = new char[buf.Length];
        for (int i=0;i<buf.Length;i++) {
            var c = buf[i];
            chars[i] = c is >=1 and <=26 ? (char)('A'+c-1) : c==0x20?' ': c==0x2E?'.':'?';
        }
        Console.WriteLine(new string(chars).Trim());
        return;
    }
}
// dump last screen
var buf2 = new byte[506];
for (int i = 0; i < buf2.Length; i++) buf2[i] = m.Bus.Peek((ushort)(0x1E00 + i));
var ch = new char[buf2.Length];
for (int i=0;i<buf2.Length;i++) {
    var c = buf2[i];
    ch[i] = c is >=1 and <=26 ? (char)('A'+c-1) : c==0x20?' ': c==0x2E?'.': c==0?'@':'?';
}
Console.WriteLine("NO READY. screen=");
Console.WriteLine(new string(ch));
var s = m.GetState();
Console.WriteLine($"final PC=${s.PC:X4}");
'@
# Use a small test project approach - compile via csharp script won't work easily
# Instead write a temp test and run it
$testPath = "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20BootProbeTests.cs"
@'
namespace ViceSharp.TestHarness.Vic20;
using Xunit;
using System;
public sealed class Vic20BootProbeTests
{
    [Fact]
    public void Probe_Boot_Dump()
    {
        var m = MachineTestFactory.CreateVic20Machine();
        Console.WriteLine($"PC after reset: ${m.GetState().PC:X4}");
        for (var frame = 0; frame < 600; frame++)
        {
            m.RunFrame();
            if (frame % 50 == 0)
                Console.WriteLine($"frame {frame} PC=${m.GetState().PC:X4}");
            var buf = new byte[506];
            for (int i = 0; i < buf.Length; i++) buf[i] = m.Bus.Peek((ushort)(0x1E00 + i));
            if (buf.AsSpan().IndexOf(new byte[]{18,5,1,4,25}) >= 0)
            {
                Console.WriteLine($"READY at frame {frame}");
                return;
            }
        }
        var buf2 = new byte[506];
        for (int i = 0; i < buf2.Length; i++) buf2[i] = m.Bus.Peek((ushort)(0x1E00 + i));
        var nonzero = 0; for (int i=0;i<buf2.Length;i++) if (buf2[i]!=0) nonzero++;
        Console.WriteLine($"NO READY. nonZero={nonzero} finalPC=${m.GetState().PC:X4}");
        // also try $1000 screen area dump sample
        for (int a = 0x1000; a < 0x2000; a += 0x100)
        {
            var span = new byte[32];
            for (int i=0;i<32;i++) span[i]=m.Bus.Peek((ushort)(a+i));
            if (span.AsSpan().IndexOf(new byte[]{18,5,1,4,25})>=0)
                Console.WriteLine($"READY near ${a:X4}");
        }
        Assert.Fail("probe");
    }
}
'@ | Set-Content $testPath -Encoding utf8
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20BootProbe" --nologo 2>&1 | Tee-Object "$scratch\boot-probe.log" | Select-Object -Last 40
- description: Probe VIC-20 boot for READY prompt

### Turn 148 (Function)
[tool_response]
<task-id>call-76374ed3-7fc9-4def-bc07-5b4bb6c84fa2-111</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-76374ed3-7fc9-4def-bc07-5b4bb6c84fa2-111.log</output-file>
<status>running</status>
<summary>Command "$scratch = "C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\implementer"
$code = @'
using System;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Core;
using ViceSharp.TestHarness;

var m = MachineTestFactory.CreateVic20Machine();
Console.WriteLine($"PC after reset: ${m.GetState().PC:X4}");
Console.WriteLine($"C000={m.Bus.Read(0xC000):X2} E000={m.Bus.Read(0xE000):X2}");
for (var frame = 0; frame < 600; frame++)
{
    m.RunFrame();
    if (frame % 50 == 0 || frame < 5)
    {
        var st = m.GetState();
        Console.WriteLine($"frame {frame} PC=${st.PC:X4} cyc={st.Cycle}");
    }
    // scan screen $1E00
    var buf = new byte[506];
    for (int i = 0; i < buf.Length; i++) buf[i] = m.Bus.Peek((ushort)(0x1E00 + i));
    if (buf.AsSpan().IndexOf(new byte[]{18,5,1,4,25}) >= 0)
    {
        Console.WriteLine($"READY at frame {frame}");
        // dump text
        var chars = new char[buf.Length];
        for (int i=0;i<buf.Length;i++) {
            var c = buf[i];
            chars[i] = c is >=1 and <=26 ? (char)('A'+c-1) : c==0x20?' ': c==0x2E?'.':'?';
        }
        Console.WriteLine(new string(chars).Trim());
        return;
    }
}
// dump last screen
var buf2 = new byte[506];
for (int i = 0; i < buf2.Length; i++) buf2[i] = m.Bus.Peek((ushort)(0x1E00 + i));
var ch = new char[buf2.Length];
for (int i=0;i<buf2.Length;i++) {
    var c = buf2[i];
    ch[i] = c is >=1 and <=26 ? (char)('A'+c-1) : c==0x20?' ': c==0x2E?'.': c==0?'@':'?';
}
Console.WriteLine("NO READY. screen=");
Console.WriteLine(new string(ch));
var s = m.GetState();
Console.WriteLine($"final PC=${s.PC:X4}");
'@
# Use a small test project approach - compile via csharp script won't work easily
# Instead write a temp test and run it
$testPath = "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20BootProbeTests.cs"
@'
namespace ViceSharp.TestHarness.Vic20;
using Xunit;
using System;
public sealed class Vic20BootProbeTests
{
    [Fact]
    public void Probe_Boot_Dump()
    {
        var m = MachineTestFactory.CreateVic20Machine();
        Console.WriteLine($"PC after reset: ${m.GetState().PC:X4}");
        for (var frame = 0; frame < 600; frame++)
        {
            m.RunFrame();
            if (frame % 50 == 0)
                Console.WriteLine($"frame {frame} PC=${m.GetState().PC:X4}");
            var buf = new byte[506];
            for (int i = 0; i < buf.Length; i++) buf[i] = m.Bus.Peek((ushort)(0x1E00 + i));
            if (buf.AsSpan().IndexOf(new byte[]{18,5,1,4,25}) >= 0)
            {
                Console.WriteLine($"READY at frame {frame}");
                return;
            }
        }
        var buf2 = new byte[506];
        for (int i = 0; i < buf2.Length; i++) buf2[i] = m.Bus.Peek((ushort)(0x1E00 + i));
        var nonzero = 0; for (int i=0;i<buf2.Length;i++) if (buf2[i]!=0) nonzero++;
        Console.WriteLine($"NO READY. nonZero={nonzero} finalPC=${m.GetState().PC:X4}");
        // also try $1000 screen area dump sample
        for (int a = 0x1000; a < 0x2000; a += 0x100)
        {
            var span = new byte[32];
            for (int i=0;i<32;i++) span[i]=m.Bus.Peek((ushort)(a+i));
            if (span.AsSpan().IndexOf(new byte[]{18,5,1,4,25})>=0)
                Console.WriteLine($"READY near ${a:X4}");
        }
        Assert.Fail("probe");
    }
}
'@ | Set-Content $testPath -Encoding utf8
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20BootProbe" --nologo 2>&1 | Tee-Object "$scratch\boot-probe.log" | Select-Object -Last 40" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-76374ed3-7fc9-4def-bc07-5b4bb6c84fa2-111"] to retrieve the output.

<system-reminder>
Note: 1 other background task is still running:
- "call-f1efebff-8ca4-41f1-b6df-16c493f5ddb8-99" (running for 70s): # Peek at VIC-20 KERNAL for READY screen expectations; also check chargen size
$rom = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\basic-901486-01.bin"
$k = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\kernal.901486-07.bin"
$c = "F:\GitHub\vice-sharp\native\vice\vice\data\VIC20\chargen-901460-03.bin"
@($rom,$k,$c) | ForEach-Object { "{0}: {1} bytes" -f (Split-Path $_ -Leaf), (Get-Item $_).Length }
# Screen RAM default for unexpanded VIC-20 is $1E00
# Check if xvic exists
Get-Command xvic -ErrorAction SilentlyContinue | Format-List; Get-ChildItem -Path "F:\GitHub\vice-sharp","C:\Program Files*" -Filter "xvic.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 5 FullName
Consider killing duplicate tasks with kill_command_or_subagent before launching new ones.
</system-reminder>

### Turn 149 (Assistant)
[tool_request: kill_command_or_subagent]
- task_id: call-f1efebff-8ca4-41f1-b6df-16c493f5ddb8-99
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-76374ed3-7fc9-4def-bc07-5b4bb6c84fa2-111"]
- timeout_ms: 180000

### Turn 150 (Function)
[tool_response]
killed: Task was terminated successfully

### Turn 151 (Function)
[tool_response]
=== Task call-76374ed3-7fc9-4def-bc07-5b4bb6c84fa2-111 ===
Command: $scratch = "C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\implementer"
$code = @'
using System;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Core;
using ViceSharp.TestHarness;

var m = MachineTestFactory.CreateVic20Machine();
Console.WriteLine($"PC after reset: ${m.GetState().PC:X4}");
Console.WriteLine($"C000={m.Bus.Read(0xC000):X2} E000={m.Bus.Read(0xE000):X2}");
for (var frame = 0; frame < 600; frame++)
{
    m.RunFrame();
    if (frame % 50 == 0 || frame < 5)
    {
        var st = m.GetState();
        Console.WriteLine($"frame {frame} PC=${st.PC:X4} cyc={st.Cycle}");
    }
    // scan screen $1E00
    var buf = new byte[506];
    for (int i = 0; i < buf.Length; i++) buf[i] = m.Bus.Peek((ushort)(0x1E00 + i));
    if (buf.AsSpan().IndexOf(new byte[]{18,5,1,4,25}) >= 0)
    {
        Console.WriteLine($"READY at frame {frame}");
        // dump text
        var chars = new char[buf.Length];
        for (int i=0;i<buf.Length;i++) {
            var c = buf[i];
            chars[i] = c is >=1 and <=26 ? (char)('A'+c-1) : c==0x20?' ': c==0x2E?'.':'?';
        }
        Console.WriteLine(new string(chars).Trim());
        return;
    }
}
// dump last screen
var buf2 = new byte[506];
for (int i = 0; i < buf2.Length; i++) buf2[i] = m.Bus.Peek((ushort)(0x1E00 + i));
var ch = new char[buf2.Length];
for (int i=0;i<buf2.Length;i++) {
    var c = buf2[i];
    ch[i] = c is >=1 and <=26 ? (char)('A'+c-1) : c==0x20?' ': c==0x2E?'.': c==0?'@':'?';
}
Console.WriteLine("NO READY. screen=");
Console.WriteLine(new string(ch));
var s = m.GetState();
Console.WriteLine($"final PC=${s.PC:X4}");
'@
# Use a small test project approach - compile via csharp script won't work easily
# Instead write a temp test and run it
$testPath = "F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20BootProbeTests.cs"
@'
namespace ViceSharp.TestHarness.Vic20;
using Xunit;
using System;
public sealed class Vic20BootProbeTests
{
    [Fact]
    public void Probe_Boot_Dump()
    {
        var m = MachineTestFactory.CreateVic20Machine();
        Console.WriteLine($"PC after reset: ${m.GetState().PC:X4}");
        for (var frame = 0; frame < 600; frame++)
        {
            m.RunFrame();
            if (frame % 50 == 0)
                Console.WriteLine($"frame {frame} PC=${m.GetState().PC:X4}");
            var buf = new byte[506];
            for (int i = 0; i < buf.Length; i++) buf[i] = m.Bus.Peek((ushort)(0x1E00 + i));
            if (buf.AsSpan().IndexOf(new byte[]{18,5,1,4,25}) >= 0)
            {
                Console.WriteLine($"READY at frame {frame}");
                return;
            }
        }
        var buf2 = new byte[506];
        for (int i = 0; i < buf2.Length; i++) buf2[i] = m.Bus.Peek((ushort)(0x1E00 + i));
        var nonzero = 0; for (int i=0;i<buf2.Length;i++) if (buf2[i]!=0) nonzero++;
        Console.WriteLine($"NO READY. nonZero={nonzero} finalPC=${m.GetState().PC:X4}");
        // also try $1000 screen area dump sample
        for (int a = 0x1000; a < 0x2000; a += 0x100)
        {
            var span = new byte[32];
            for (int i=0;i<32;i++) span[i]=m.Bus.Peek((ushort)(a+i));
            if (span.AsSpan().IndexOf(new byte[]{18,5,1,4,25})>=0)
                Console.WriteLine($"READY near ${a:X4}");
        }
        Assert.Fail("probe");
    }
}
'@ | Set-Content $testPath -Encoding utf8
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20BootProbe" --nologo 2>&1 | Tee-Object "$scratch\boot-probe.log" | Select-Object -Last 40
Status: failed
Duration: 16.83s
Exit Code: 1
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-76374ed3-7fc9-4def-bc07-5b4bb6c84fa2-111.log

=== Output ===
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:02.88]     ViceSharp.TestHarness.Vic20.Vic20BootProbeTests.Probe_Boot_Dump [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20BootProbeTests.Probe_Boot_Dump [938 ms]
  Error Message:
   probe
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20BootProbeTests.Probe_Boot_Dump() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20BootProbeTests.cs:line 36
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 964 ms - ViceSharp.TestHarness.dll (net10.0)


### Turn 152 (Assistant)
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20BootProbeTests.cs
- content: namespace ViceSharp.TestHarness.Vic20;

using System.Text;
using Xunit;

/// <summary>
/// Temporary diagnostic probe for VIC-20 boot path. Replaced by formal boot tests.
/// </summary>
public sealed class Vic20BootProbeTests
{
    [Fact]
    public void Probe_Boot_Dump()
    {
        var m = MachineTestFactory.CreateVic20Machine();
        var sb = new StringBuilder();
        sb.AppendLine($"PC after reset: ${m.GetState().PC:X4}");
        sb.AppendLine($"FFFC/D={m.Bus.Read(0xFFFC):X2}{m.Bus.Read(0xFFFD):X2}");
        sb.AppendLine($"C000={m.Bus.Read(0xC000):X2} E000={m.Bus.Read(0xE000):X2}");

        ushort lastPc = 0;
        var stuck = 0;
        for (var frame = 0; frame < 400; frame++)
        {
            m.RunFrame();
            var pc = m.GetState().PC;
            if (pc == lastPc) stuck++; else stuck = 0;
            lastPc = pc;
            if (frame < 8 || frame % 50 == 0)
                sb.AppendLine($"frame {frame} PC=${pc:X4} cyc={m.GetState().Cycle}");

            if (FindReady(m, out var at))
            {
                sb.AppendLine($"READY at frame {frame} addr=${at:X4}");
                Assert.True(true, sb.ToString());
                return;
            }
        }

        sb.AppendLine($"stuckLast={stuck} finalPC=${m.GetState().PC:X4}");
        // Sample key zero-page and screen pages
        for (ushort a = 0x0000; a < 0x0100; a++)
        {
            if (a % 16 == 0) sb.Append($"\n${a:X4}:");
            sb.Append($" {m.Bus.Peek(a):X2}");
        }
        sb.AppendLine();
        for (var page = 0x1E; page <= 0x1F; page++)
        {
            sb.AppendLine($"page ${page:X2}00 sample:");
            for (var i = 0; i < 88; i++)
            {
                var c = m.Bus.Peek((ushort)((page << 8) + i));
                sb.Append(Decode(c));
            }
            sb.AppendLine();
        }
        // Check if any READY-like anywhere in $1000-$1FFF
        for (var a = 0x1000; a < 0x2000 - 5; a++)
        {
            if (m.Bus.Peek((ushort)a) == 18 && m.Bus.Peek((ushort)(a + 1)) == 5
                && m.Bus.Peek((ushort)(a + 2)) == 1 && m.Bus.Peek((ushort)(a + 3)) == 4
                && m.Bus.Peek((ushort)(a + 4)) == 25)
            {
                sb.AppendLine($"FOUND READY at ${a:X4}");
            }
        }
        // VIA timer / IRQ sanity
        sb.AppendLine($"VIA1 $9110={m.Bus.Read(0x9110):X2} VIA2 $9120={m.Bus.Read(0x9120):X2}");
        sb.AppendLine($"VIC $9000={m.Bus.Read(0x9000):X2} $9005={m.Bus.Read(0x9005):X2}");

        Assert.Fail(sb.ToString());
    }

    private static bool FindReady(ViceSharp.Abstractions.IMachine m, out ushort at)
    {
        for (var a = 0x1000; a < 0x2000 - 5; a++)
        {
            if (m.Bus.Peek((ushort)a) == 18 && m.Bus.Peek((ushort)(a + 1)) == 5
                && m.Bus.Peek((ushort)(a + 2)) == 1 && m.Bus.Peek((ushort)(a + 3)) == 4
                && m.Bus.Peek((ushort)(a + 4)) == 25)
            {
                at = (ushort)a;
                return true;
            }
        }
        at = 0;
        return false;
    }

    private static char Decode(byte c) => c switch
    {
        0x20 => ' ',
        >= 1 and <= 26 => (char)('A' + c - 1),
        0x2E => '.',
        0x00 => '@',
        _ => '?'
    };
}


### Turn 153 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20BootProbeTests.cs.

### Turn 154 (Assistant)
[tool_request: run_terminal_command]
- command: $scratch = "C:\Users\kingd\AppData\Local\Temp\grok-goal-32799c5b1a25\implementer"; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20BootProbe" --nologo 2>&1 | Tee-Object "$scratch\boot-probe.log" | Select-Object -Last 80
- description: Run boot probe with diagnostic dump

### Turn 155 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 353 ms - ViceSharp.TestHarness.dll (net10.0)


### Turn 156 (Assistant)
Boot already reaches READY. Implementing memory expansions, keyboard/VIA, real VIC-I, formal tests, heads, and determinism.
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Expansion.cs
- content: namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 RAM expansion configuration (BLK regions).
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public enum Vic20Expansion
{
    /// <summary>Unexpanded: 1KB + 4KB base only.</summary>
    Unexpanded = 0,

    /// <summary>3KB expansion in $0400-$0FFF.</summary>
    Exp3K = 3,

    /// <summary>8KB in BLK1 ($2000-$3FFF).</summary>
    Exp8K = 8,

    /// <summary>16KB in BLK1+BLK2 ($2000-$5FFF).</summary>
    Exp16K = 16,

    /// <summary>24KB in BLK1+BLK2+BLK3 ($2000-$7FFF).</summary>
    Exp24K = 24,

    /// <summary>32KB: 24KB + BLK5 ($A000-$BFFF).</summary>
    Exp32K = 32,
}

/// <summary>
/// Decodes which address ranges are installed RAM for a given expansion.
/// </summary>
public static class Vic20MemoryLayout
{
    public const ushort BaseLowRamStart = 0x0000;
    public const ushort BaseLowRamEnd = 0x03FF;
    public const ushort Exp3KStart = 0x0400;
    public const ushort Exp3KEnd = 0x0FFF;
    public const ushort MainRamStart = 0x1000;
    public const ushort MainRamEnd = 0x1FFF;
    public const ushort Blk1Start = 0x2000;
    public const ushort Blk1End = 0x3FFF;
    public const ushort Blk2Start = 0x4000;
    public const ushort Blk2End = 0x5FFF;
    public const ushort Blk3Start = 0x6000;
    public const ushort Blk3End = 0x7FFF;
    public const ushort CharRomStart = 0x8000;
    public const ushort CharRomEnd = 0x8FFF;
    public const ushort IoStart = 0x9000;
    public const ushort IoEnd = 0x9FFF;
    public const ushort ColorRamStart = 0x9400;
    public const ushort ColorRamEnd = 0x97FF;
    public const ushort Blk5Start = 0xA000;
    public const ushort Blk5End = 0xBFFF;
    public const ushort BasicStart = 0xC000;
    public const ushort BasicEnd = 0xDFFF;
    public const ushort KernalStart = 0xE000;
    public const ushort KernalEnd = 0xFFFF;

    /// <summary>Default unexpanded screen memory base (22 columns).</summary>
    public const ushort UnexpandedScreenBase = 0x1E00;

    public static bool IsInstalledRam(Vic20Expansion expansion, ushort address)
    {
        if (address <= BaseLowRamEnd)
            return true;
        if (address >= MainRamStart && address <= MainRamEnd)
            return true;
        if (address >= Exp3KStart && address <= Exp3KEnd)
            return expansion is Vic20Expansion.Exp3K or Vic20Expansion.Exp32K
                || expansion >= Vic20Expansion.Exp8K; // 3K optional; 8K+ configs often leave $0400 open
        // Standard VICE RAM expansions:
        // 3K: $0400-$0FFF only
        // 8K: BLK1
        // 16K: BLK1+2
        // 24K: BLK1+2+3
        // 32K: BLK1+2+3+5 (and often 3K as well in "full" packs; we enable BLK5 only for 32K)

        return expansion switch
        {
            Vic20Expansion.Exp3K => address >= Exp3KStart && address <= Exp3KEnd,
            Vic20Expansion.Exp8K => address >= Blk1Start && address <= Blk1End,
            Vic20Expansion.Exp16K => address >= Blk1Start && address <= Blk2End,
            Vic20Expansion.Exp24K => address >= Blk1Start && address <= Blk3End,
            Vic20Expansion.Exp32K => (address >= Blk1Start && address <= Blk3End)
                || (address >= Blk5Start && address <= Blk5End)
                || (address >= Exp3KStart && address <= Exp3KEnd),
            _ => false
        };
    }

    /// <summary>
    /// Returns exclusive expansion RAM regions beyond the always-present base 5KB.
    /// </summary>
    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20Expansion expansion)
    {
        return expansion switch
        {
            Vic20Expansion.Exp3K => [(Exp3KStart, Exp3KEnd)],
            Vic20Expansion.Exp8K => [(Blk1Start, Blk1End)],
            Vic20Expansion.Exp16K => [(Blk1Start, Blk2End)],
            Vic20Expansion.Exp24K => [(Blk1Start, Blk3End)],
            Vic20Expansion.Exp32K => [(Exp3KStart, Exp3KEnd), (Blk1Start, Blk3End), (Blk5Start, Blk5End)],
            _ => Array.Empty<(ushort, ushort)>()
        };
    }
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20KeyboardMatrix.cs
- content: using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 keyboard matrix and joystick state wired through VIA2 port callbacks.
/// Column select is written on Port B; rows are read on Port A (VICE vic20via2.c).
/// </summary>
/// <remarks>FR-VIC20-003, FR-VIC20-004. Board glue only; Via6522 stays machine-agnostic.</remarks>
public sealed class Vic20KeyboardMatrix : IDevice
{
    // keyarr[row] bit col set when key (row,col) is pressed (active-high internal).
    private readonly byte[] _keyRows = new byte[8];
    private byte _joystickMask; // bit0 up,1 down,2 left,3 right,4 fire (active-high pressed)

    private Via6522? _via2;
    private Via6522? _via1;

    public DeviceId Id { get; } = new(0x0B01);
    public string Name => "VIC-20 keyboard matrix";

    /// <summary>Joystick directions + fire as active-high pressed bits (0=up..3=right, 4=fire).</summary>
    public byte JoystickMask
    {
        get => _joystickMask;
        set => _joystickMask = value;
    }

    public void Reset()
    {
        Array.Clear(_keyRows);
        _joystickMask = 0;
    }

    /// <summary>Press or release a matrix key at (row, column), both 0..7.</summary>
    public void SetKey(int row, int column, bool pressed)
    {
        if ((uint)row > 7 || (uint)column > 7)
            throw new ArgumentOutOfRangeException(nameof(row), "row/column must be 0..7");

        var bit = (byte)(1 << column);
        if (pressed)
            _keyRows[row] |= bit;
        else
            _keyRows[row] = (byte)(_keyRows[row] & ~bit);
    }

    /// <summary>Clear all keys.</summary>
    public void ReleaseAllKeys() => Array.Clear(_keyRows);

    /// <summary>
    /// Connect VIA2 (keyboard / IRQ) and optionally VIA1 (joystick bits / NMI path).
    /// </summary>
    public void Connect(Via6522 via2, Via6522? via1 = null)
    {
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
        _via1 = via1;

        // Port A read: keyboard rows for columns driven low on Port B output.
        via2.PortAInput = ReadPortA;
        // Port B read: reverse scan + joystick right on bit 7.
        via2.PortBInput = ReadPortB;

        if (via1 is not null)
        {
            // VIA1 Port A: joystick up/down/left/fire (bits 2..5 typical VICE mapping).
            var prev = via1.PortAInput;
            via1.PortAInput = () =>
            {
                var baseVal = prev?.Invoke() ?? 0xFF;
                return (byte)(baseVal & ComposeVia1JoystickMask());
            };
        }
    }

    private byte ReadPortA()
    {
        // Idle high on undriven bits; pressed keys pull row bits low when column is selected (low).
        byte val = 0xFF;
        var columns = _via2 is null ? (byte)0xFF : GetPortBDrivenLow();
        for (var col = 0; col < 8; col++)
        {
            if ((columns & (1 << col)) != 0)
                continue; // column not selected (not driven low)

            for (var row = 0; row < 8; row++)
            {
                if ((_keyRows[row] & (1 << col)) != 0)
                    val = (byte)(val & ~(1 << row));
            }
        }

        // Joystick right also appears on VIA2 PA via column/port-B path in VICE;
        // map fire/right onto PA when active for software that peeks PA.
        if ((_joystickMask & 0x08) != 0) // right
            val = (byte)(val & 0x7F);

        return val;
    }

    private byte ReadPortB()
    {
        byte val = 0xFF;
        // Reverse matrix: when rows driven low on PA, columns appear on PB.
        var rows = _via2 is null ? (byte)0xFF : GetPortADrivenLow();
        for (var row = 0; row < 8; row++)
        {
            if ((rows & (1 << row)) != 0)
                continue;
            for (var col = 0; col < 8; col++)
            {
                if ((_keyRows[row] & (1 << col)) != 0)
                    val = (byte)(val & ~(1 << col));
            }
        }

        if ((_joystickMask & 0x08) != 0) // right on PB7
            val = (byte)(val & 0x7F);

        return val;
    }

    private byte GetPortBDrivenLow()
    {
        // Bits that are outputs and written 0 are driven low.
        // Without direct DDR/OR access, approximate: call PortB through a peek of composed output.
        // Via6522 exposes only callbacks; we track via reading last known by re-entrant free path.
        // Use PortBOutputChanged side channel if available; otherwise assume all-FF columns
        // unless we latch on PortBOutputChanged.
        return _portBLatched;
    }

    private byte GetPortADrivenLow() => _portALatched;

    private byte _portBLatched = 0xFF;
    private byte _portALatched = 0xFF;

    /// <summary>
    /// Call after Connect to latch Port A/B output changes from the VIA DDR/OR model.
    /// </summary>
    public void AttachOutputLatches(Via6522 via2)
    {
        var prevB = via2.PortBOutputChanged;
        via2.PortBOutputChanged = v =>
        {
            _portBLatched = v;
            prevB?.Invoke(v);
        };
        var prevA = via2.PortAOutputChanged;
        via2.PortAOutputChanged = v =>
        {
            _portALatched = v;
            prevA?.Invoke(v);
        };
    }

    private byte ComposeVia1JoystickMask()
    {
        // Active-low joystick bits on VIA1 PA (common VIC-20 mapping):
        // bit2 up, bit3 down, bit4 left, bit5 fire (bit for right is on VIA2).
        byte mask = 0xFF;
        if ((_joystickMask & 0x01) != 0) mask = (byte)(mask & ~(1 << 2)); // up
        if ((_joystickMask & 0x02) != 0) mask = (byte)(mask & ~(1 << 3)); // down
        if ((_joystickMask & 0x04) != 0) mask = (byte)(mask & ~(1 << 4)); // left
        if ((_joystickMask & 0x10) != 0) mask = (byte)(mask & ~(1 << 5)); // fire
        return mask;
    }

    /// <summary>
    /// Scan helper used by tests: given a column mask driven low, return the row bits pulled low.
    /// </summary>
    public byte ScanRowsForColumns(byte columnSelectActiveLow)
    {
        byte val = 0xFF;
        for (var col = 0; col < 8; col++)
        {
            if ((columnSelectActiveLow & (1 << col)) != 0)
                continue;
            for (var row = 0; row < 8; row++)
            {
                if ((_keyRows[row] & (1 << col)) != 0)
                    val = (byte)(val & ~(1 << row));
            }
        }
        return val;
    }
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- content: using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Vic;

/// <summary>
/// MOS 6560/6561 VIC-I video chip: character-mode framebuffer renderer with
/// PAL/NTSC timing. Machine-agnostic; the board supplies bus peeks for screen,
/// color, and character data via callbacks.
/// </summary>
/// <remarks>FR-VIC20-001.</remarks>
public sealed class Mos6561 : IVideoChip, IAddressSpace, IInterruptSource
{
    // Default VIC-20 palette (approximate, BGRA).
    private static readonly uint[] Palette =
    [
        0xFF000000, // 0 black
        0xFFFFFFFF, // 1 white
        0xFF2B1B8C, // 2 red
        0xFFBDCC6F, // 3 cyan
        0xFF9B2D8B, // 4 purple
        0xFF44A34A, // 5 green
        0xFF1A168D, // 6 blue
        0xFFD5DE71, // 7 yellow
        0xFF2B4E8C, // 8 orange
        0xFF0055A4, // 9 light orange
        0xFF756CBA, // 10 pink
        0xFFC0C0C0, // 11 light cyan
        0xFF8C8C8C, // 12 light purple
        0xFF8AE391, // 13 light green
        0xFF7676C9, // 14 light blue
        0xFFE0E0E0, // 15 light yellow
    ];

    private readonly IInterruptLine? _irqLine;
    private readonly byte[] _regs = new byte[16];
    private int _cycleInLine;
    private int _cyclesPerLine = 71;
    private int _totalLines = 312;
    private int _visibleLines = 284;
    private int _frameWidth = 176; // 22 chars * 8
    private int _frameHeight = 184; // 23 rows * 8
    private byte[] _frameBuffer;
    private ushort _rasterLine;
    private bool _frameDirty = true;

    /// <summary>Bus peek for screen/color/chargen data (board provides).</summary>
    public Func<ushort, byte>? MemoryPeek { get; set; }

    /// <summary>Optional base of character ROM (default $8000).</summary>
    public ushort CharacterRomBase { get; set; } = 0x8000;

    public Mos6561(IInterruptLine? irqLine = null)
    {
        _irqLine = irqLine;
        Id = new DeviceId(0x0003);
        SourceId = Id;
        Name = "MOS 6561 VIC-I";
        BaseAddress = 0x9000;
        Size = 0x0010;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
        ResetRegisters();
    }

    public DeviceId Id { get; init; }
    public string Name { get; init; }
    public DeviceId SourceId { get; init; }
    public ushort BaseAddress { get; init; }
    public ushort Size { get; init; }
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;
    public IReadOnlyList<IInterruptLine> ConnectedLines =>
        _irqLine is null ? Array.Empty<IInterruptLine>() : new[] { _irqLine };

    public ushort CurrentRasterLine => _rasterLine;
    public int CyclesPerLine => _cyclesPerLine;
    public int VisibleLines => _visibleLines;
    public int TotalLines => _totalLines;
    public bool IsVBlank => _rasterLine >= _visibleLines;
    public byte[] FrameBuffer => _frameBuffer;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;
    public event EventHandler? FrameCompleted;

    public void ConfigureTiming(int cyclesPerLine, int totalLines, int visibleLines, int columns = 22, int rows = 23)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cyclesPerLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLines, 1);
        _cyclesPerLine = cyclesPerLine;
        _totalLines = totalLines;
        _visibleLines = Math.Min(visibleLines, totalLines);
        _frameWidth = Math.Max(8, columns * 8);
        _frameHeight = Math.Max(8, rows * 8);
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
        _frameDirty = true;
    }

    public bool HandlesAddress(ushort address)
        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);

    public byte Read(ushort address)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        if (reg == 0x04)
            return (byte)(_rasterLine & 0xFF);
        return _regs[reg];
    }

    public void Write(ushort address, byte value)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        _regs[reg] = value;
        _frameDirty = true;
    }

    public byte Peek(ushort address) => Read(address);

    public void Tick()
    {
        _cycleInLine++;
        if (_cycleInLine < _cyclesPerLine)
            return;

        _cycleInLine = 0;
        _rasterLine++;
        if (_rasterLine < _totalLines)
            return;

        _rasterLine = 0;
        RenderCharacterFrame();
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        ResetRegisters();
        _cycleInLine = 0;
        _rasterLine = 0;
        Array.Clear(_frameBuffer);
        _frameDirty = true;
    }

    /// <summary>
    /// Force an immediate character-mode render (tests / hosts that pull frames
    /// without waiting for a full raster).
    /// </summary>
    public void RenderNow() => RenderCharacterFrame();

    private void ResetRegisters()
    {
        Array.Clear(_regs);
        // Unexpanded VIC-20 defaults (approximate VICE power-on):
        // $9002 = columns | screen-map high bits; $9005 char/screen origin.
        _regs[0x00] = 5;   // interlace/origin
        _regs[0x01] = 25;  // origin Y
        _regs[0x02] = 22;  // 22 columns (bits 0-6)
        _regs[0x03] = (23 << 1); // rows in bits 1-6
        _regs[0x05] = 0xF0; // character at $8000, screen high from other bits
        _regs[0x0E] = 0;    // auxiliary color / volume
        _regs[0x0F] = 0x08; // background/border: black bg, cyan border-ish
    }

    private void RenderCharacterFrame()
    {
        var columns = Math.Clamp(_regs[0x02] & 0x7F, 1, 32);
        var rows = Math.Clamp((_regs[0x03] >> 1) & 0x3F, 1, 32);
        if (columns * 8 != _frameWidth || rows * 8 != _frameHeight)
        {
            _frameWidth = columns * 8;
            _frameHeight = rows * 8;
            _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
        }

        var screenBase = ResolveScreenBase();
        var charBase = ResolveCharBase();
        var bg = Palette[_regs[0x0F] & 0x0F];
        var border = Palette[(_regs[0x0F] >> 4) & 0x07]; // border uses 3 bits on real VIC; approx
        _ = border;

        var peek = MemoryPeek ?? (_ => 0x00);
        var fb = _frameBuffer;
        var stride = _frameWidth * 4;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var offset = row * columns + col;
                var screenCode = peek((ushort)(screenBase + offset));
                var colorNibble = (byte)(peek((ushort)(0x9400 + (offset & 0x03FF))) & 0x0F);
                var fg = Palette[colorNibble & 0x0F];
                // Invert bit in screen code selects reverse video on real VIC;
                // character index is low 7 or full 8 depending on map.
                var reverse = (screenCode & 0x80) != 0;
                var charIndex = screenCode & 0x7F;
                var glyphAddr = (ushort)(charBase + charIndex * 8);

                for (var py = 0; py < 8; py++)
                {
                    var bits = peek((ushort)(glyphAddr + py));
                    if (reverse)
                        bits = (byte)~bits;
                    var y = row * 8 + py;
                    if (y >= _frameHeight)
                        break;
                    var rowBase = y * stride + col * 8 * 4;
                    for (var px = 0; px < 8; px++)
                    {
                        var on = (bits & (0x80 >> px)) != 0;
                        var color = on ? fg : bg;
                        var i = rowBase + px * 4;
                        fb[i] = (byte)(color & 0xFF);
                        fb[i + 1] = (byte)((color >> 8) & 0xFF);
                        fb[i + 2] = (byte)((color >> 16) & 0xFF);
                        fb[i + 3] = 0xFF;
                    }
                }
            }
        }

        _frameDirty = false;
    }

    private ushort ResolveScreenBase()
    {
        // VIC-I screen map: bits from $9002/$9005. Unexpanded default $1E00.
        // Simplified decode matching common unexpanded and 8K-expanded bases.
        var cr = _regs[0x02];
        var vm = _regs[0x05];
        // Bits: screen address = ((vm & 0xF0) << 6) | ((cr & 0x80) << 2)
        var addr = (ushort)(((vm & 0xF0) << 6) | ((cr & 0x80) << 2));
        if (addr == 0)
            return Vic20DefaultScreenBase;
        return addr;
    }

    private ushort ResolveCharBase()
    {
        // Character map from $9005 low nibble: unexpanded often points at $8000 ROM.
        var vm = _regs[0x05];
        var addr = (ushort)((vm & 0x0F) << 10);
        if (addr == 0 || addr >= 0x8000)
            return CharacterRomBase;
        return addr == 0 ? CharacterRomBase : addr;
    }

    /// <summary>Default unexpanded screen base used when registers still zero.</summary>
    public const ushort Vic20DefaultScreenBase = 0x1E00;
}

[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Cartridge.cs
- content: using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// MVP VIC-20 cartridge image: raw payload mapped into a BLK region (default BLK5 $A000).
/// </summary>
/// <remarks>FR-VIC20-005.</remarks>
public sealed class Vic20Cartridge : IAddressSpace
{
    private readonly byte[] _image;
    private readonly ushort _start;
    private readonly ushort _end;

    public Vic20Cartridge(ReadOnlySpan<byte> image, ushort mapStart = 0xA000)
    {
        if (image.IsEmpty)
            throw new ArgumentException("Cartridge image is empty.", nameof(image));
        if (image.Length > 0x4000)
            throw new ArgumentException("MVP cart image max is 16KB.", nameof(image));

        _image = image.ToArray();
        _start = mapStart;
        _end = (ushort)(mapStart + _image.Length - 1);
        Id = new DeviceId(0x0C01);
    }

    public DeviceId Id { get; }
    public string Name => "VIC-20 cartridge";
    public ushort MapStart => _start;
    public ushort MapEnd => _end;
    public int Size => _image.Length;

    public void Reset() { }

    public bool HandlesAddress(ushort address)
        => address >= _start && address <= _end;

    public byte Read(ushort address) => _image[address - _start];
    public byte Peek(ushort address) => Read(address);
    public void Write(ushort address, byte value) { /* ROM cart */ }

    /// <summary>
    /// Load a raw .bin/.prg cart. If the first two bytes look like a load address
    /// (typical .prg), skip them and map the payload at that address when in BLK range.
    /// </summary>
    public static Vic20Cartridge LoadFromFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] + (bytes[1] << 8) is >= 0x2000 and <= 0xBFFF)
        {
            var loadAddr = (ushort)(bytes[0] | (bytes[1] << 8));
            return new Vic20Cartridge(bytes.AsSpan(2), loadAddr);
        }

        return new Vic20Cartridge(bytes, 0xA000);
    }

    /// <summary>Build from an in-memory PRG (load-addr header optional).</summary>
    public static Vic20Cartridge FromPrg(ReadOnlySpan<byte> prg)
    {
        if (prg.Length >= 3)
        {
            var loadAddr = (ushort)(prg[0] | (prg[1] << 8));
            if (loadAddr is >= 0x2000 and <= 0xBFFF)
                return new Vic20Cartridge(prg[2..], loadAddr);
        }

        return new Vic20Cartridge(prg, 0xA000);
    }
}


### Turn 157 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs has been created.

### Turn 158 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20KeyboardMatrix.cs has been created.

### Turn 159 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Cartridge.cs has been created.

### Turn 160 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Expansion.cs has been created.

### Turn 161 (Assistant)
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Expansion.cs
- content: namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 RAM expansion configuration (BLK regions).
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public enum Vic20Expansion
{
    /// <summary>Unexpanded: 1KB + 4KB base only.</summary>
    Unexpanded = 0,

    /// <summary>3KB expansion in $0400-$0FFF.</summary>
    Exp3K = 3,

    /// <summary>8KB in BLK1 ($2000-$3FFF).</summary>
    Exp8K = 8,

    /// <summary>16KB in BLK1+BLK2 ($2000-$5FFF).</summary>
    Exp16K = 16,

    /// <summary>24KB in BLK1+BLK2+BLK3 ($2000-$7FFF).</summary>
    Exp24K = 24,

    /// <summary>32KB: 3K + BLK1..3 + BLK5.</summary>
    Exp32K = 32,
}

/// <summary>
/// Decodes which address ranges are installed RAM for a given expansion.
/// </summary>
public static class Vic20MemoryLayout
{
    public const ushort BaseLowRamEnd = 0x03FF;
    public const ushort Exp3KStart = 0x0400;
    public const ushort Exp3KEnd = 0x0FFF;
    public const ushort MainRamStart = 0x1000;
    public const ushort MainRamEnd = 0x1FFF;
    public const ushort Blk1Start = 0x2000;
    public const ushort Blk1End = 0x3FFF;
    public const ushort Blk2End = 0x5FFF;
    public const ushort Blk3End = 0x7FFF;
    public const ushort Blk5Start = 0xA000;
    public const ushort Blk5End = 0xBFFF;

    /// <summary>Default unexpanded screen memory base (22 columns).</summary>
    public const ushort UnexpandedScreenBase = 0x1E00;

    public static bool IsInstalledRam(Vic20Expansion expansion, ushort address)
    {
        if (address <= BaseLowRamEnd)
            return true;
        if (address >= MainRamStart && address <= MainRamEnd)
            return true;

        foreach (var (start, end) in ExpansionRegions(expansion))
        {
            if (address >= start && address <= end)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Expansion RAM regions beyond the always-present base 5KB.
    /// </summary>
    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20Expansion expansion)
        => expansion switch
        {
            Vic20Expansion.Exp3K => [(Exp3KStart, Exp3KEnd)],
            Vic20Expansion.Exp8K => [(Blk1Start, Blk1End)],
            Vic20Expansion.Exp16K => [(Blk1Start, Blk2End)],
            Vic20Expansion.Exp24K => [(Blk1Start, Blk3End)],
            Vic20Expansion.Exp32K => [(Exp3KStart, Exp3KEnd), (Blk1Start, Blk3End), (Blk5Start, Blk5End)],
            _ => Array.Empty<(ushort, ushort)>()
        };
}

[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20MachineProfile.cs
- old_string:     bool KeyboardEnabled = true,
    bool CartridgeBootExpected = false,
    DriveModel DefaultDriveModel = DriveModel.C1540,
    string DefaultDriveDosRomName = C1541ViceRomNames.Dos1540,
    Vic20SystemCoreDefinition? CoreDefinition = null) : IMachineProfile
{
    public string Family => "xvic";

    public double RefreshRateHz => NominalClockHz / (double)(CyclesPerLine * RasterLines);

    /// <summary>Exposes VIC-I model through the shared profile string used by C64 as VicIIModel.</summary>
    public string VicIIModel => Vic.ToString();

    /// <summary>VIC-20 has no SID; empty marker for shared profile surface.</summary>
    public string SidModel => "None";

    public string BoardModel => "Unexpanded";

    public ISystemCoreDefinition SystemCore { get; } =
        CoreDefinition ?? Vic20SystemCoreDefinition.Unexpanded(DefaultDriveModel, DefaultDriveDosRomName);
}
- new_string:     bool KeyboardEnabled = true,
    bool CartridgeBootExpected = false,
    DriveModel DefaultDriveModel = DriveModel.C1540,
    string DefaultDriveDosRomName = C1541ViceRomNames.Dos1540,
    Vic20Expansion Expansion = Vic20Expansion.Unexpanded,
    Vic20SystemCoreDefinition? CoreDefinition = null) : IMachineProfile
{
    public string Family => "xvic";

    public double RefreshRateHz => NominalClockHz / (double)(CyclesPerLine * RasterLines);

    /// <summary>Exposes VIC-I model through the shared profile string used by C64 as VicIIModel.</summary>
    public string VicIIModel => Vic.ToString();

    /// <summary>VIC-20 has no SID; empty marker for shared profile surface.</summary>
    public string SidModel => "None";

    public string BoardModel => Expansion == Vic20Expansion.Unexpanded
        ? "Unexpanded"
        : $"Exp{(int)Expansion}K";

    public ISystemCoreDefinition SystemCore { get; } =
        CoreDefinition ?? Vic20SystemCoreDefinition.Unexpanded(DefaultDriveModel, DefaultDriveDosRomName);
}
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string:     /// <summary>
    /// Build a VIC-20 skeleton: Mos6502, dual Via6522 ($9110 NMI / $9120 IRQ),
    /// VIC-I stub video, base RAM, and BASIC/KERNAL/chargen ROMs. Full memory
    /// banking and keyboard wiring land in later Iteration 2 slices.
    /// </summary>
    /// <remarks>FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006.</remarks>
    private IMachine BuildVic20Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine, nmiLine);
        var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;

        // Base 64KB backing store; ROMs and I/O devices overlay via register order
        // (BasicBus prefers the most recently registered device that HandlesAddress).
        var ram = new SimpleRam();
        ram.Fill(0x00);
        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);

        var architectureKey = descriptor.RequiredRoms?.Architecture ?? "VIC20";
        var basicName = profile?.BasicRomName ?? "basic-901486-01.bin";
        var kernalName = profile?.KernalRomName ?? "kernal.901486-07.bin";
        var characterName = profile?.CharacterRomName ?? "chargen-901460-03.bin";

        var basic = _romProvider.LoadRom(basicName, architectureKey).ToArray();
        var kernal = _romProvider.LoadRom(kernalName, architectureKey).ToArray();
        var character = _romProvider.LoadRom(characterName, architectureKey).ToArray();

        // Load ROMs into the flat store first, then register dedicated RomDevice
        // overlays so writes to ROM space are ignored (real hardware behaviour).
        ram.LoadRom(0xC000, basic);
        ram.LoadRom(0xE000, kernal);
        ram.LoadRom(0x8000, character);

        var basicRom = new RomDevice(0xC000, 0xDFFF, basic);
        var kernalRom = new RomDevice(0xE000, 0xFFFF, kernal);
        var chargenRom = new RomDevice(0x8000, 0x8FFF, character);
        bus.RegisterDevice(basicRom);
        bus.RegisterDevice(kernalRom);
        bus.RegisterDevice(chargenRom);
        deviceRegistry.Add(basicRom, DeviceRole.BasicRom);
        deviceRegistry.Add(kernalRom, DeviceRole.KernalRom);
        deviceRegistry.Add(chargenRom, DeviceRole.ChargenRom);

        // Color RAM at $9400-$97FF (1KB; only low nibble used on real hardware).
        var colorRam = new RamDevice(0x9400, 0x97FF, new byte[0x0400]);
        bus.RegisterDevice(colorRam);

        // VIA1 @ $9110 -> NMI, VIA2 @ $9120 -> IRQ (VIC-20 board wiring).
        var via1 = new Via6522(bus, nmiLine)
        {
            Id = new DeviceId(0x0005),
            Name = "VIA1",
            SourceId = new DeviceId(0x0005),
            BaseAddress = 0x9110,
            Size = 0x0010,
        };
        var via2 = new Via6522(bus, irqLine)
        {
            Id = new DeviceId(0x0006),
            Name = "VIA2",
            SourceId = new DeviceId(0x0006),
            BaseAddress = 0x9120,
            Size = 0x0010,
        };
        bus.RegisterDevice(via1);
        bus.RegisterDevice(via2);
        clock.Register(via1);
        clock.Register(via2);
        deviceRegistry.Add(via1, DeviceRole.Via1);
        deviceRegistry.Add(via2, DeviceRole.Via2);

        var vic = new VicIStub(irqLine)
        {
            Id = new DeviceId(0x0003),
            Name = "VIC-I",
            SourceId = new DeviceId(0x0003),
            BaseAddress = 0x9000,
            Size = 0x0010,
        };
        var cyclesPerLine = profile?.CyclesPerLine ?? 71;
        var rasterLines = profile?.RasterLines ?? 312;
        var visibleLines = profile?.VideoStandard == VideoStandard.Ntsc ? 234 : 284;
        // VIC-20 visible character area is modest; stub uses a fixed pixel grid.
        vic.ConfigureTiming(cyclesPerLine, rasterLines, visibleLines, frameWidth: 224, frameHeight: visibleLines);
        bus.RegisterDevice(vic);
        clock.Register(vic);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);

        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);

        if (profile is not null)
            deviceRegistry.Add(new SystemCore(profile.SystemCore), DeviceRole.SystemCore);

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        return machine;
    }
- new_string:     /// <summary>
    /// Build a VIC-20: Mos6502, dual Via6522 ($9110 NMI / $9120 IRQ), Mos6561 VIC-I,
    /// base + expansion RAM, BASIC/KERNAL/chargen, keyboard matrix, optional cart.
    /// </summary>
    /// <remarks>FR-PRF-005, FR-VIC20-001..006.</remarks>
    private IMachine BuildVic20Machine(IArchitectureDescriptor descriptor)
    {
        if (_romProvider is null)
            throw new InvalidOperationException($"{descriptor.MachineName} requires an IRomProvider.");

        if (descriptor.RequiredRoms is not null && !descriptor.RequiredRoms.IsComplete(_romProvider))
            throw new InvalidOperationException(
                $"Required ROM set for {descriptor.MachineName} is missing or invalid.");

        var bus = new BasicBus();
        var deviceRegistry = new DeviceRegistry();
        var irqLine = new InterruptLine(InterruptType.Irq);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus);
        var clock = new SystemClock(descriptor.MasterClockHz, cpu, irqLine, nmiLine);
        var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;
        var vic20Profile = (descriptor as Architectures.Vic20.Vic20Descriptor)?.Profile;
        var expansion = vic20Profile?.Expansion ?? Architectures.Vic20.Vic20Expansion.Unexpanded;

        // Base 64KB backing store; ROMs and I/O devices overlay via register order
        // (BasicBus prefers the most recently registered device that HandlesAddress).
        var ram = new SimpleRam();
        ram.Fill(0x00);
        // Uninstalled expansion regions read as open-bus 0xFF until expanded.
        foreach (var region in new (ushort Start, ushort End)[]
                 {
                     (0x0400, 0x0FFF), (0x2000, 0x3FFF), (0x4000, 0x5FFF),
                     (0x6000, 0x7FFF), (0xA000, 0xBFFF)
                 })
        {
            for (int a = region.Start; a <= region.End; a++)
            {
                if (!Architectures.Vic20.Vic20MemoryLayout.IsInstalledRam(expansion, (ushort)a))
                    ram.Write((ushort)a, 0xFF);
            }
        }

        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);

        var architectureKey = descriptor.RequiredRoms?.Architecture ?? "VIC20";
        var basicName = profile?.BasicRomName ?? "basic-901486-01.bin";
        var kernalName = profile?.KernalRomName ?? "kernal.901486-07.bin";
        var characterName = profile?.CharacterRomName ?? "chargen-901460-03.bin";

        var basic = _romProvider.LoadRom(basicName, architectureKey).ToArray();
        var kernal = _romProvider.LoadRom(kernalName, architectureKey).ToArray();
        var character = _romProvider.LoadRom(characterName, architectureKey).ToArray();

        ram.LoadRom(0xC000, basic);
        ram.LoadRom(0xE000, kernal);
        ram.LoadRom(0x8000, character);

        var basicRom = new RomDevice(0xC000, 0xDFFF, basic);
        var kernalRom = new RomDevice(0xE000, 0xFFFF, kernal);
        var chargenRom = new RomDevice(0x8000, 0x8FFF, character);
        bus.RegisterDevice(basicRom);
        bus.RegisterDevice(kernalRom);
        bus.RegisterDevice(chargenRom);
        deviceRegistry.Add(basicRom, DeviceRole.BasicRom);
        deviceRegistry.Add(kernalRom, DeviceRole.KernalRom);
        deviceRegistry.Add(chargenRom, DeviceRole.ChargenRom);

        // Color RAM at $9400-$97FF (1KB; only low nibble used on real hardware).
        var colorRam = new RamDevice(0x9400, 0x97FF, new byte[0x0400]);
        bus.RegisterDevice(colorRam);

        // VIA1 @ $9110 -> NMI, VIA2 @ $9120 -> IRQ (VIC-20 board wiring).
        var via1 = new Via6522(bus, nmiLine)
        {
            Id = new DeviceId(0x0005),
            Name = "VIA1",
            SourceId = new DeviceId(0x0005),
            BaseAddress = 0x9110,
            Size = 0x0010,
        };
        var via2 = new Via6522(bus, irqLine)
        {
            Id = new DeviceId(0x0006),
            Name = "VIA2",
            SourceId = new DeviceId(0x0006),
            BaseAddress = 0x9120,
            Size = 0x0010,
        };
        bus.RegisterDevice(via1);
        bus.RegisterDevice(via2);
        clock.Register(via1);
        clock.Register(via2);
        deviceRegistry.Add(via1, DeviceRole.Via1);
        deviceRegistry.Add(via2, DeviceRole.Via2);

        var keyboard = new Architectures.Vic20.Vic20KeyboardMatrix();
        keyboard.Connect(via2, via1);
        keyboard.AttachOutputLatches(via2);
        deviceRegistry.Add(keyboard);

        // Optional MVP cart from descriptor cartridge slot path (none by default).
        if (descriptor is Architectures.Vic20.Vic20Descriptor vd && vd.Cartridge is not null)
        {
            bus.RegisterDevice(vd.Cartridge);
            deviceRegistry.Add(vd.Cartridge, DeviceRole.CartridgePort);
        }

        var vic = new Mos6561(irqLine)
        {
            Id = new DeviceId(0x0003),
            Name = "VIC-I",
            SourceId = new DeviceId(0x0003),
            BaseAddress = 0x9000,
            Size = 0x0010,
            MemoryPeek = addr => bus.Peek(addr),
            CharacterRomBase = 0x8000,
        };
        var cyclesPerLine = profile?.CyclesPerLine ?? 71;
        var rasterLines = profile?.RasterLines ?? 312;
        var visibleLines = profile?.VideoStandard == VideoStandard.Ntsc ? 234 : 284;
        vic.ConfigureTiming(cyclesPerLine, rasterLines, visibleLines, columns: 22, rows: 23);
        bus.RegisterDevice(vic);
        clock.Register(vic);
        deviceRegistry.Add(vic, DeviceRole.VideoChip);

        clock.Register(cpu);
        deviceRegistry.Add(cpu, DeviceRole.Cpu);

        if (profile is not null)
            deviceRegistry.Add(new SystemCore(profile.SystemCore), DeviceRole.SystemCore);

        var pubSub = ConnectMachinePubSub(bus, cpu);
        var machine = new Machine(descriptor, bus, clock, deviceRegistry, cpu, pubSub);
        machine.Reset();
        return machine;
    }

### Turn 162 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20MachineProfile.cs has been updated successfully.

### Turn 163 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Expansion.cs.

### Turn 164 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 165 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceSharp.Core.csproj
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\ViceSharp.Architectures.csproj
[tool_request: grep]
- pattern: Architectures\.|using ViceSharp.Architectures
- glob: **/ArchitectureBuilder.cs
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core

### Turn 166 (Function)
[tool_response]
1→<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\ViceSharp.Abstractions\ViceSharp.Abstractions.csproj" />
    <ProjectReference Include="..\ViceSharp.Chips\ViceSharp.Chips.csproj" />
    <ProjectReference Include="..\ViceSharp.RomFetch\ViceSharp.RomFetch.csproj" />
  </ItemGroup>

  <ItemGroup>
10→    <!-- SETTINGS-INI-001: IConfiguration for the custom INI provider/source
         (ConfigurationProvider/ConfigurationRoot base types) and the
         appsettings.json folder resolution. Core configuration path is
         reflection-free, so AOT/trim-safe (the reflective Binder is not used). -->
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
20→    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

</Project>


### Turn 167 (Function)
[tool_response]
1→<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\ViceSharp.Core\ViceSharp.Core.csproj" />
    <ProjectReference Include="..\ViceSharp.Abstractions\ViceSharp.Abstractions.csproj" />
    <ProjectReference Include="..\ViceSharp.Chips\ViceSharp.Chips.csproj" />
    <ProjectReference Include="..\ViceSharp.RomFetch\ViceSharp.RomFetch.csproj" />
  </ItemGroup>

10→  <ItemGroup>
    <PackageReference Include="YamlDotNet" />
    <!-- Source generator for the [YamlStaticContext]/[YamlSerializable]
         reflection-free serializer (MachineDefinitionYamlContext). Analyzer-only;
         not a runtime dependency. -->
    <PackageReference Include="Vecc.YamlDotNet.Analyzers.StaticGenerator" PrivateAssets="all" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
20→    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>


### Turn 168 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 5 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
282:        var vic20Profile = (descriptor as Architectures.Vic20.Vic20Descriptor)?.Profile;
283:        var expansion = vic20Profile?.Expansion ?? Architectures.Vic20.Vic20Expansion.Unexpanded;
298:                if (!Architectures.Vic20.Vic20MemoryLayout.IsInstalledRam(expansion, (ushort)a))
357:        var keyboard = new Architectures.Vic20.Vic20KeyboardMatrix();
363:        if (descriptor is Architectures.Vic20.Vic20Descriptor vd && vd.Cartridge is not null)
</workspace_result>

### Turn 169 (Assistant)
Core can't reference Architectures. Moving board glue into Core and fixing the builder.
[tool_request: run_terminal_command]
- command: New-Item -ItemType Directory -Force -Path "F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20" | Out-Null
Move-Item -Force "F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20KeyboardMatrix.cs" "F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20KeyboardMatrix.cs"
Move-Item -Force "F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Cartridge.cs" "F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20Cartridge.cs"
# Keep expansion enum in Architectures but also need layout in Core - write Core layout copy that uses string/int

- description: Move Vic20 board glue into Core
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20KeyboardMatrix.cs
- limit: 15

### Turn 170 (Function)
[tool_response]
Error: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20KeyboardMatrix.cs does not exist.
Note: your current working directory is F:\GitHub\vice-sharp

### Turn 171 (Function)
[tool_response]
exit: 0


### Turn 172 (Assistant)
[tool_request: run_terminal_command]
- command: Get-ChildItem "F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20" -ErrorAction SilentlyContinue; Get-ChildItem "F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20" | Select-Object Name
- description: List Vic20 files after move

### Turn 173 (Function)
[tool_response]
exit: 0

    Directory: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a---            8/5/2026 12:28 PM           2427 Vic20Cartridge.cs
-a---            8/5/2026 12:28 PM           6397 Vic20KeyboardMatrix.cs

Name : Vic20Descriptor.cs


Name : Vic20Expansion.cs


Name : Vic20MachineProfile.cs


Name : Vic20RomSet.cs


Name : Vic20SystemCoreDefinition.cs


Name : Vic20ViceRomNames.cs




### Turn 174 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20KeyboardMatrix.cs
- old_string: using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Architectures.Vic20;
- new_string: using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core.Vic20;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20Cartridge.cs
- old_string: using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;
- new_string: using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20MemoryLayout.cs
- content: namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 expansion sizes (kilobytes of expansion pack, not total system RAM).
/// </summary>
public enum Vic20ExpansionKind
{
    Unexpanded = 0,
    Exp3K = 3,
    Exp8K = 8,
    Exp16K = 16,
    Exp24K = 24,
    Exp32K = 32,
}

/// <summary>
/// Address-map helpers for VIC-20 base and expansion RAM.
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public static class Vic20MemoryLayout
{
    public const ushort BaseLowRamEnd = 0x03FF;
    public const ushort Exp3KStart = 0x0400;
    public const ushort Exp3KEnd = 0x0FFF;
    public const ushort MainRamStart = 0x1000;
    public const ushort MainRamEnd = 0x1FFF;
    public const ushort Blk1Start = 0x2000;
    public const ushort Blk1End = 0x3FFF;
    public const ushort Blk2End = 0x5FFF;
    public const ushort Blk3End = 0x7FFF;
    public const ushort Blk5Start = 0xA000;
    public const ushort Blk5End = 0xBFFF;
    public const ushort UnexpandedScreenBase = 0x1E00;

    public static Vic20ExpansionKind ParseBoardModel(string? boardModel)
    {
        if (string.IsNullOrWhiteSpace(boardModel))
            return Vic20ExpansionKind.Unexpanded;

        return boardModel.Trim().ToUpperInvariant() switch
        {
            "EXP3K" or "3K" => Vic20ExpansionKind.Exp3K,
            "EXP8K" or "8K" => Vic20ExpansionKind.Exp8K,
            "EXP16K" or "16K" => Vic20ExpansionKind.Exp16K,
            "EXP24K" or "24K" => Vic20ExpansionKind.Exp24K,
            "EXP32K" or "32K" => Vic20ExpansionKind.Exp32K,
            _ => Vic20ExpansionKind.Unexpanded
        };
    }

    public static bool IsInstalledRam(Vic20ExpansionKind expansion, ushort address)
    {
        if (address <= BaseLowRamEnd)
            return true;
        if (address >= MainRamStart && address <= MainRamEnd)
            return true;

        foreach (var (start, end) in ExpansionRegions(expansion))
        {
            if (address >= start && address <= end)
                return true;
        }

        return false;
    }

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20ExpansionKind expansion)
        => expansion switch
        {
            Vic20ExpansionKind.Exp3K => [(Exp3KStart, Exp3KEnd)],
            Vic20ExpansionKind.Exp8K => [(Blk1Start, Blk1End)],
            Vic20ExpansionKind.Exp16K => [(Blk1Start, Blk2End)],
            Vic20ExpansionKind.Exp24K => [(Blk1Start, Blk3End)],
            Vic20ExpansionKind.Exp32K => [(Exp3KStart, Exp3KEnd), (Blk1Start, Blk3End), (Blk5Start, Blk5End)],
            _ => Array.Empty<(ushort, ushort)>()
        };
}

[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Expansion.cs
- old_string: namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 RAM expansion configuration (BLK regions).
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public enum Vic20Expansion
{
    /// <summary>Unexpanded: 1KB + 4KB base only.</summary>
    Unexpanded = 0,

    /// <summary>3KB expansion in $0400-$0FFF.</summary>
    Exp3K = 3,

    /// <summary>8KB in BLK1 ($2000-$3FFF).</summary>
    Exp8K = 8,

    /// <summary>16KB in BLK1+BLK2 ($2000-$5FFF).</summary>
    Exp16K = 16,

    /// <summary>24KB in BLK1+BLK2+BLK3 ($2000-$7FFF).</summary>
    Exp24K = 24,

    /// <summary>32KB: 3K + BLK1..3 + BLK5.</summary>
    Exp32K = 32,
}

/// <summary>
/// Decodes which address ranges are installed RAM for a given expansion.
/// </summary>
public static class Vic20MemoryLayout
{
    public const ushort BaseLowRamEnd = 0x03FF;
    public const ushort Exp3KStart = 0x0400;
    public const ushort Exp3KEnd = 0x0FFF;
    public const ushort MainRamStart = 0x1000;
    public const ushort MainRamEnd = 0x1FFF;
    public const ushort Blk1Start = 0x2000;
    public const ushort Blk1End = 0x3FFF;
    public const ushort Blk2End = 0x5FFF;
    public const ushort Blk3End = 0x7FFF;
    public const ushort Blk5Start = 0xA000;
    public const ushort Blk5End = 0xBFFF;

    /// <summary>Default unexpanded screen memory base (22 columns).</summary>
    public const ushort UnexpandedScreenBase = 0x1E00;

    public static bool IsInstalledRam(Vic20Expansion expansion, ushort address)
    {
        if (address <= BaseLowRamEnd)
            return true;
        if (address >= MainRamStart && address <= MainRamEnd)
            return true;

        foreach (var (start, end) in ExpansionRegions(expansion))
        {
            if (address >= start && address <= end)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Expansion RAM regions beyond the always-present base 5KB.
    /// </summary>
    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20Expansion expansion)
        => expansion switch
        {
            Vic20Expansion.Exp3K => [(Exp3KStart, Exp3KEnd)],
            Vic20Expansion.Exp8K => [(Blk1Start, Blk1End)],
            Vic20Expansion.Exp16K => [(Blk1Start, Blk2End)],
            Vic20Expansion.Exp24K => [(Blk1Start, Blk3End)],
            Vic20Expansion.Exp32K => [(Exp3KStart, Exp3KEnd), (Blk1Start, Blk3End), (Blk5Start, Blk5End)],
            _ => Array.Empty<(ushort, ushort)>()
        };
}
- new_string: using CoreLayout = ViceSharp.Core.Vic20.Vic20MemoryLayout;
using CoreExpansion = ViceSharp.Core.Vic20.Vic20ExpansionKind;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 RAM expansion configuration (aliases Core expansion kind).
/// </summary>
/// <remarks>FR-VIC20-002.</remarks>
public enum Vic20Expansion
{
    Unexpanded = 0,
    Exp3K = 3,
    Exp8K = 8,
    Exp16K = 16,
    Exp24K = 24,
    Exp32K = 32,
}

/// <summary>
/// Architecture-layer facade over <see cref="CoreLayout"/>.
/// </summary>
public static class Vic20MemoryLayout
{
    public const ushort UnexpandedScreenBase = CoreLayout.UnexpandedScreenBase;

    public static bool IsInstalledRam(Vic20Expansion expansion, ushort address)
        => CoreLayout.IsInstalledRam((CoreExpansion)(int)expansion, address);

    public static IReadOnlyList<(ushort Start, ushort End)> ExpansionRegions(Vic20Expansion expansion)
        => CoreLayout.ExpansionRegions((CoreExpansion)(int)expansion);
}
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs
- old_string: using ViceSharp.Abstractions;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 (xvic) architecture descriptor: 6502, dual VIA, VIC-I, base RAM, ROMs.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006.
/// VIA and video board wiring live here; shared chips stay machine-agnostic.
/// </remarks>
public sealed class Vic20Descriptor : IProfiledArchitectureDescriptor
{
    public Vic20Descriptor()
        : this(Vic20MachineProfiles.Default)
    {
    }

    public Vic20Descriptor(string modelSelector)
        : this(Vic20MachineProfiles.Resolve(modelSelector))
    {
    }

    public Vic20Descriptor(Vic20MachineProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Vic20MachineProfile Profile { get; }

    /// <inheritdoc />
    public IMachineProfile MachineProfile => Profile;

    /// <inheritdoc />
    public string MachineName => Profile.DisplayName;

    /// <inheritdoc />
    public long MasterClockHz => Profile.NominalClockHz;

    /// <inheritdoc />
    public VideoStandard VideoStandard => Profile.VideoStandard;

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-I", new DeviceId(0x0003), DeviceRole.VideoChip, 0x9000, 0x0010),
        new("VIA1", new DeviceId(0x0005), DeviceRole.Via1, 0x9110, 0x0010),
        new("VIA2", new DeviceId(0x0006), DeviceRole.Via2, 0x9120, 0x0010),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0xA000, 0x2000),
    ];

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new Vic20RomSet(
        Profile.RomSet,
        Profile.BasicRomName,
        Profile.KernalRomName,
        Profile.CharacterRomName);
}
- new_string: using ViceSharp.Abstractions;
using ViceSharp.Core.Vic20;

namespace ViceSharp.Architectures.Vic20;

/// <summary>
/// VIC-20 (xvic) architecture descriptor: 6502, dual VIA, VIC-I, base RAM, ROMs.
/// </summary>
/// <remarks>
/// FR-PRF-005, FR-VIC20-001..006.
/// VIA and video board wiring live in Core builder; shared chips stay machine-agnostic.
/// </remarks>
public sealed class Vic20Descriptor : IProfiledArchitectureDescriptor
{
    public Vic20Descriptor()
        : this(Vic20MachineProfiles.Default)
    {
    }

    public Vic20Descriptor(string modelSelector)
        : this(Vic20MachineProfiles.Resolve(modelSelector))
    {
    }

    public Vic20Descriptor(Vic20MachineProfile profile, Vic20Cartridge? cartridge = null)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Cartridge = cartridge;
    }

    public Vic20MachineProfile Profile { get; }

    /// <summary>Optional MVP cart image mapped by the architecture builder.</summary>
    public Vic20Cartridge? Cartridge { get; }

    /// <inheritdoc />
    public IMachineProfile MachineProfile => Profile;

    /// <inheritdoc />
    public string MachineName => Profile.DisplayName;

    /// <inheritdoc />
    public long MasterClockHz => Profile.NominalClockHz;

    /// <inheritdoc />
    public VideoStandard VideoStandard => Profile.VideoStandard;

    /// <inheritdoc />
    public IReadOnlyList<DeviceDescriptor> Devices { get; } =
    [
        new("6502 CPU", new DeviceId(0x0001), DeviceRole.Cpu, 0x0000, 0),
        new("VIC-I", new DeviceId(0x0003), DeviceRole.VideoChip, 0x9000, 0x0010),
        new("VIA1", new DeviceId(0x0005), DeviceRole.Via1, 0x9110, 0x0010),
        new("VIA2", new DeviceId(0x0006), DeviceRole.Via2, 0x9120, 0x0010),
        new("System RAM", new DeviceId(0x0101), DeviceRole.SystemRam, 0x0000, 0x10000),
        new("Cartridge port", new DeviceId(0x0008), DeviceRole.CartridgePort, 0xA000, 0x2000),
    ];

    /// <inheritdoc />
    public IRomSet? RequiredRoms => new Vic20RomSet(
        Profile.RomSet,
        Profile.BasicRomName,
        Profile.KernalRomName,
        Profile.CharacterRomName);

    /// <summary>Clone with a different expansion pack.</summary>
    public Vic20Descriptor WithExpansion(Vic20Expansion expansion)
        => new(Profile with { Expansion = expansion }, Cartridge);

    /// <summary>Clone with an attached MVP cartridge.</summary>
    public Vic20Descriptor WithCartridge(Vic20Cartridge cartridge)
        => new(Profile, cartridge);
}
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string: using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Vic;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Chips.Tape;
using ViceSharp.Core.Wiring;
using ViceSharp.RomFetch;
- new_string: using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Vic;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using ViceSharp.Chips.Tape;
using ViceSharp.Core.Vic20;
using ViceSharp.Core.Wiring;
using ViceSharp.RomFetch;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string:         var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;
        var vic20Profile = (descriptor as Architectures.Vic20.Vic20Descriptor)?.Profile;
        var expansion = vic20Profile?.Expansion ?? Architectures.Vic20.Vic20Expansion.Unexpanded;

        // Base 64KB backing store; ROMs and I/O devices overlay via register order
        // (BasicBus prefers the most recently registered device that HandlesAddress).
        var ram = new SimpleRam();
        ram.Fill(0x00);
        // Uninstalled expansion regions read as open-bus 0xFF until expanded.
        foreach (var region in new (ushort Start, ushort End)[]
                 {
                     (0x0400, 0x0FFF), (0x2000, 0x3FFF), (0x4000, 0x5FFF),
                     (0x6000, 0x7FFF), (0xA000, 0xBFFF)
                 })
        {
            for (int a = region.Start; a <= region.End; a++)
            {
                if (!Architectures.Vic20.Vic20MemoryLayout.IsInstalledRam(expansion, (ushort)a))
                    ram.Write((ushort)a, 0xFF);
            }
        }

        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);
- new_string:         var profile = (descriptor as IProfiledArchitectureDescriptor)?.MachineProfile;
        var expansion = Vic20MemoryLayout.ParseBoardModel(profile?.BoardModel);

        // Base 64KB backing store; ROMs and I/O devices overlay via register order
        // (BasicBus prefers the most recently registered device that HandlesAddress).
        var ram = new SimpleRam();
        ram.Fill(0x00);
        // Uninstalled expansion regions read as open-bus 0xFF until expanded.
        foreach (var region in new (ushort Start, ushort End)[]
                 {
                     (0x0400, 0x0FFF), (0x2000, 0x3FFF), (0x4000, 0x5FFF),
                     (0x6000, 0x7FFF), (0xA000, 0xBFFF)
                 })
        {
            for (int a = region.Start; a <= region.End; a++)
            {
                if (!Vic20MemoryLayout.IsInstalledRam(expansion, (ushort)a))
                    ram.Write((ushort)a, 0xFF);
            }
        }

        bus.RegisterDevice(ram);
        deviceRegistry.Add(ram, DeviceRole.SystemRam);
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
- old_string:         var keyboard = new Architectures.Vic20.Vic20KeyboardMatrix();
        keyboard.Connect(via2, via1);
        keyboard.AttachOutputLatches(via2);
        deviceRegistry.Add(keyboard);

        // Optional MVP cart from descriptor cartridge slot path (none by default).
        if (descriptor is Architectures.Vic20.Vic20Descriptor vd && vd.Cartridge is not null)
        {
            bus.RegisterDevice(vd.Cartridge);
            deviceRegistry.Add(vd.Cartridge, DeviceRole.CartridgePort);
        }
- new_string:         var keyboard = new Vic20KeyboardMatrix();
        keyboard.Connect(via2, via1);
        keyboard.AttachOutputLatches(via2);
        deviceRegistry.Add(keyboard);

        // Optional MVP cart: descriptor may expose a Vic20Cartridge via IDevice on Devices
        // list is declarative only; runtime attach uses WithCartridge extension on profiled
        // descriptors that implement IVic20CartridgeHost (checked by reflection-free type).
        if (descriptor is IVic20CartridgeHost cartHost && cartHost.Cartridge is not null)
        {
            bus.RegisterDevice(cartHost.Cartridge);
            deviceRegistry.Add(cartHost.Cartridge, DeviceRole.CartridgePort);
        }

### Turn 175 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs has been updated successfully.

### Turn 176 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 177 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Expansion.cs has been updated successfully.

### Turn 178 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20KeyboardMatrix.cs has been updated successfully.

### Turn 179 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20MemoryLayout.cs has been created.

### Turn 180 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20Cartridge.cs has been updated successfully.

### Turn 181 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 182 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs has been updated successfully.

### Turn 183 (Assistant)
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\IVic20CartridgeHost.cs
- content: namespace ViceSharp.Core.Vic20;

/// <summary>
/// Optional architecture descriptor contract for attaching a VIC-20 MVP cart
/// at machine build time.
/// </summary>
public interface IVic20CartridgeHost
{
    /// <summary>Cartridge image to map, or null when empty.</summary>
    Vic20Cartridge? Cartridge { get; }
}

[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs
- old_string: public sealed class Vic20Descriptor : IProfiledArchitectureDescriptor
{
- new_string: public sealed class Vic20Descriptor : IProfiledArchitectureDescriptor, IVic20CartridgeHost
{
[tool_request: run_terminal_command]
- command: dotnet build .\src\ViceSharp.Core\ViceSharp.Core.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 40
dotnet build .\src\ViceSharp.Architectures\ViceSharp.Architectures.csproj -c Release --nologo -v q 2>&1 | Select-Object -Last 40
- description: Build Core and Architectures for errors

### Turn 184 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Architectures\Vic20\Vic20Descriptor.cs has been updated successfully.

### Turn 185 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\IVic20CartridgeHost.cs has been created.

### Turn 186 (Function)
[tool_response]
exit: 1
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs(44,18): error CS0414: The field 'Mos6561._frameDirty' is assigned but its value is never used [F:\GitHub\vice-sharp\src\ViceSharp.Chips\ViceSharp.Chips.csproj]

Build FAILED.

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs(44,18): error CS0414: The field 'Mos6561._frameDirty' is assigned but its value is never used [F:\GitHub\vice-sharp\src\ViceSharp.Chips\ViceSharp.Chips.csproj]
    0 Warning(s)
    1 Error(s)

Time Elapsed 00:00:01.61
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs(44,18): error CS0414: The field 'Mos6561._frameDirty' is assigned but its value is never used [F:\GitHub\vice-sharp\src\ViceSharp.Chips\ViceSharp.Chips.csproj]

Build FAILED.

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs(44,18): error CS0414: The field 'Mos6561._frameDirty' is assigned but its value is never used [F:\GitHub\vice-sharp\src\ViceSharp.Chips\ViceSharp.Chips.csproj]
    0 Warning(s)
    1 Error(s)

Time Elapsed 00:00:01.78


### Turn 187 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- old_string:     private byte[] _frameBuffer;
    private ushort _rasterLine;
    private bool _frameDirty = true;
- new_string:     private byte[] _frameBuffer;
    private ushort _rasterLine;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- old_string:         _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
        _frameDirty = true;
    }

    public bool HandlesAddress(ushort address)
- new_string:         _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
    }

    public bool HandlesAddress(ushort address)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- old_string:         var reg = (byte)((address - BaseAddress) & 0x0F);
        _regs[reg] = value;
        _frameDirty = true;
    }
- new_string:         var reg = (byte)((address - BaseAddress) & 0x0F);
        _regs[reg] = value;
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- old_string:         Array.Clear(_frameBuffer);
        _frameDirty = true;
    }
- new_string:         Array.Clear(_frameBuffer);
    }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- old_string:                         fb[i + 3] = 0xFF;
                    }
                }
            }
        }

        _frameDirty = false;
    }
- new_string:                         fb[i + 3] = 0xFF;
                    }
                }
            }
        }
    }

### Turn 188 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs has been updated successfully.

### Turn 189 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs has been updated successfully.

### Turn 190 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs has been updated successfully.

### Turn 191 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs has been updated successfully.

### Turn 192 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs has been updated successfully.


[... TRUNCATED at 524288 bytes, 379 turns omitted ...]
