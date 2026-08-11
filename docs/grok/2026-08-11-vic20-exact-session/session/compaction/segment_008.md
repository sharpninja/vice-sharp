# HISTORICAL -- DO NOT EDIT
# Record of compaction segment 008 (detail=verbose) from this same task.
# Use read_file or grep to look up details, but do not modify.

## Segment metadata
- Index: 008
- Turn count: 591
- Timestamp: 2026-08-06T08:22:43Z

## Turn statistics

- Turns: 591 (Assistant=215, Function=369, Human=6, System=1)
- Tools used: run_terminal_command (120), search_replace (93), read_file (85), grep (35), get_command_or_subagent_output (23), write (6), todo_write (5), kill_command_or_subagent (1), list_dir (1)
- Unique target files (28): C:\Users\kingd\.claude\profile, C:\Users\kingd\.claude\profile\PROFILE.md, C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md, C:\Users\kingd\.claude\profile\approve-before-execute.md, C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md, ... and 23 more
- Tool errors: 0
- Verbose-render size estimate: 1,098,640 B
- Last assistant response excerpt: "Regression: early LDA zp after any implied (including DEX) is too broad. Narrowing to flag ops only (SEC/CLC/...)."


## Summary (curated by compaction step)

Summary:
1. Primary Request and Intent: User standing rule: always ask (1) where diverged from VICE and (2) how to realign with VICE (no lag hacks). Goal is Vic20 every-cycle A/X/Y/S/P/PC lockstep vs native xvic (not playable-only). Skeptic/completion gates: NotTakenBne_CycleTrace must be Explicit; residual must not be claimed multi-second/1.1e6 without evidence; continue autonomously. Work continued post-compaction: mid-ROR S drop at c=540740, then long chain of VICE export/phase realignments. Evaluator latest (before compaction): implement soft-defer for CMP abs,X after taken-branch trail==1; re-run focused+2s; update receipt. Host still shows STALE skeptic items (NotTaken permanent Fact; residual 517828) which on disk are false (both Explicit; current residual much higher).

2. Key Technical Concepts: ViceSharp .NET 10; PAL 1108405 Hz; every-cycle lockstep native.Step + managed.Clock.Step; VICE 6510(c/dtvcore) BRANCH/PLA/RTI/JSR/ASL/DO_INTERRUPT; soft-defer vs cycle-stage; host-visible PC via _visiblePC / IsInstructionBoundary; SystemClock arms IRQ after CPU tick when IsInstructionBoundary; dual VIA VIC-20; fuse/soft phase coupling; Byrd/receipts.

3. Files and Code Sections: Mos6502.cs (main): CompleteJsrTargetFetch + _interruptSampleDespiteSuppress; PC getter decoupled from IRQ sample; soft-imm one-insn latch at fetch (_applySkipSoftImmThisInsn); _fuseImpliedAfterIndyLoad / _fuseNonLoadImmAfterLoadBranch / _afterShortTakenBranchLag; shift RMW commit cycle rules; store hold rules; soft-defer/fuse for CMP abs,X, loads, JMP, JSR, RTS, STA IRQ suppress, etc. BranchCycleCountTests.cs: both TakenBne and NotTakenBne [Fact(Explicit=true)] lines 24,55. Vic20DivergeProbe.cs every-cycle 2s/focused. docs/receipts-lockstep-vice-realign-2026-08-06.txt + many docs/2s-*.log / focused logs. Latest residual evidence docs/2s-sta-bne-2026-08-06.log: matchedCycles=553376 (regression from 559295).

4. Errors and Fixes: Mid-ROR S drop: post-JSR suppress blocked IRQ; fixed with interrupt sample despite suppress. Sticky soft-imm skip over-fused CMP# at 518756: latch/clear at fetch. Store hold and branch-lag shift/RTS rules refined repeatedly. LDA zp early commit after all implied regressed DEX path; narrowed to IsImpliedFlagOpcode. Store hold skip after all branch trail=1 regressed STX after taken BCS (c=553377 nPC=opcode mPC=next): need hold after taken-branch target, advance after not-taken/load. Many incremental residual climbs from 540739→559295 then regression to 553376 on over-broad store hold.

5. Problem Solving: Skeptic NotTaken/517828 STALE on disk. Current work refining FinishStagedMemoryWrite nonOvl plain-store PC hold: trail==0 hold; trail==1 after taken branch hold; trail==1 after load/not-taken advance; trail>=2 advance. Focused store-hold rebuild was in progress. Peak honest 2s residual before store-hold regression: 559295.

6. All User Messages: "remember, always ask, where did I diverge from VICE, and how do I get back in line with VICE?" plus system continue/skeptic/evaluator residual and CMP abs,X soft-defer task.

7. Pending Tasks: Finish store-hold refinement so both 553377 (STX after taken BCS) and 559296 (STA after not-taken BNE) match; re-run focused+2s; update receipt; leave NotTaken Explicit; only claim multi-s/done with green 2s or hard VICE-block evidence.

8. Current Work: Edited FinishStagedMemoryWrite hold logic in Mos6502.cs to use afterTakenBranch (_afterFullLengthTakenBranch || _afterShortTakenBranchLag). Started focused gate run focused-store-hold.log (status at summary: running/timeout background). Previous 2s: docs/2s-sta-bne-2026-08-06.log DIV first=553377 matchedCycles=553376 STX $86 after taken BCS nPC=$F403 mPC=$F405.

9. Optional Next Step: Await/confirm focused-store-hold green, then re-run VICESHARP_LOCKSTEP_2S=1 TwoSecondPal, update docs/receipts-lockstep-vice-realign-2026-08-06.txt with matchedCycles/first-diverge. If STX still wrong or STA regresses, further split taken vs not-taken store hold. Quote: "Implement soft-defer for CMP abs,X... then re-run focused + TwoSecondPal and update the docs receipt" / continue autonomous residual climb without claiming multi-second done.

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
Today's date: 2026-08-06
Note: Prefer using relative paths over absolute paths as tool call args when possible.
</user_info>

<git_status>
This is the git status at the start of the conversation. Note that this status is a snapshot in time, and will not update during the conversation.
## [32mmain[m...[31morigin/main[m
 [31mM[m HANDOFF.md
 [31mM[m docs/xbox/ado-store-setup.md
 [31mM[m docs/xbox/store-listing-copy.md
 [31mM[m docs/xbox/store-next-steps-guide.md
 [31mM[m native/vice-shim-vic20.c
 [31mM[m native/vice-shim.h
 [31mM[m src/ViceSharp.Abstractions/IViceNative.cs
 [31mM[m src/ViceSharp.Chips/Cpu/Mos6502.Opcodes.cs
 [31mM[m src/ViceSharp.Chips/Cpu/Mos6502.cs
 [31mM[m src/ViceSharp.Chips/Vic/Mos6561.cs
 [31mM[m src/ViceSharp.Core/ArchitectureBuilder.cs
 [31mM[m src/ViceSharp.Core/Vic20/Vic20KeyboardMatrix.cs
 [31mM[m src/ViceSharp.Core/Vic20/Vic20SystemRam.cs
 [31mM[m src/ViceSharp.Core/ViceNative.Xvic.cs
 [31mM[m src/ViceSharp.Core/ViceNative.cs
 [31mM[m src/ViceSharp.Xbox/Package.appxmanifest
 [31mM[m tests/ViceSharp.TestHarness/Vic20/Vic20NativeLockstepTests.cs
[31m??[m docs/2s-adc-2026-08-06.log
[31m??[m docs/2s-adc2-2026-08-06.log
[31m??[m docs/2s-asl-2026-08-06.log
[31m??[m docs/2s-bccsta-2026-08-06.log
[31m??[m docs/2s-beq-2026-08-06.log
[31m??[m docs/2s-beqpla-2026-08-06.log
[31m??[m docs/2s-bit-2026-08-06.log
[31m??[m docs/2s-bit2-2026-08-06.log
[31m??[m docs/2s-bit3-2026-08-06.log
[31m??[m docs/2s-bit4-2026-08-06.log
[31m??[m docs/2s-bit5-2026-08-06.log
[31m??[m docs/2s-bitf-2026-08-06.log
[31m??[m docs/2s-bitf2-2026-08-06.log
[31m??[m docs/2s-bnehold-2026-08-06.log
[31m??[m docs/2s-bpl3-2026-08-06.log
[31m??[m docs/2s-bpl4-2026-08-06.log
[31m??[m docs/2s-bpl6-2026-08-06.log
[31m??[m docs/2s-bpl7-2026-08-06.log
[31m??[m docs/2s-cmpzp-2026-08-06.log
[31m??[m docs/2s-cmpzp2-2026-08-06.log
[31m??[m docs/2s-final-2026-08-06.log
[31m??[m docs/2s-holdindy-2026-08-06.log
[31m??[m docs/2s-incsticky-2026-08-06.log
[31m??[m docs/2s-inx-2026-08-06.log
[31m??[m docs/2s-irqb-2026-08-06.log
[31m??[m docs/2s-irqdiag-2026-08-06.log
[31m??[m docs/2s-irqfall-2026-08-06.log
[31m??[m docs/2s-irqrmw-2026-08-06.log
[31m??[m docs/2s-irqsuppress-2026-08-06.log
[31m??[m docs/2s-jmp-2026-08-06.log
[31m??[m docs/2s-jmp2-2026-08-06.log
[31m??[m docs/2s-jmp3-2026-08-06.log
[31m??[m docs/2s-lda-2026-08-06.log
[31m??[m docs/2s-lsrx-2026-08-06.log
[31m??[m docs/2s-lsrzp-2026-08-06.log
[31m??[m docs/2s-lsrzp2-2026-08-06.log
[31m??[m docs/2s-memdiag-2026-08-06.log
[31m??[m docs/2s-ora-2026-08-06.log
[31m??[m docs/2s-pla-2026-08-06.log
[31m??[m docs/2s-pla-late-2026-08-06.log
[31m??[m docs/2s-pla2-2026-08-06.log
[31m??[m docs/2s-rti-2026-08-06.log
[31m??[m docs/2s-rti2-2026-08-06.log
[31m??[m docs/2s-rtsand-2026-08-06.log
[31m??[m docs/2s-rtsfl-2026-08-06.log
[31m??[m docs/2s-sec-2026-08-06.log
[31m??[m docs/2s-stazp-2026-08-06.log
[31m??[m docs/2s-store-2026-08-06.log
[31m??[m docs/2s-tay-2026-08-06.log
[31m??[m docs/2s-via1iec-2026-08-06.log
[31m??[m docs/S-Blox/
[31m??[m docs/adc-focused-2026-08-06.log
[31m??[m docs/adc2-focused-2026-08-06.log
[31m??[m docs/asl-focused-2026-08-06.log
[31m??[m docs/bccsta-focused-2026-08-06.log
[31m??[m docs/beq-focused-2026-08-06.log
[31m??[m docs/beqpla-focused-2026-08-06.log
[31m??[m docs/bit-focused-2026-08-06.log
[31m??[m docs/bit2-focused-2026-08-06.log
[31m??[m docs/bit3-focused-2026-08-06.log
[31m??[m docs/bit4-focused-2026-08-06.log
[31m??[m docs/bit5-focused-2026-08-06.log
[31m??[m docs/bitf-focused-2026-08-06.log
[31m??[m docs/bitf2-focused-2026-08-06.log
[31m??[m docs/bnehold-focused-2026-08-06.log
[31m??[m docs/bpl-focused-2026-08-06.log
[31m??[m docs/bpl2-focused-2026-08-06.log
[31m??[m docs/bpl3-focused-2026-08-06.log
[31m??[m docs/bpl4-focused-2026-08-06.log
[31m??[m docs/bpl5-focused-2026-08-06.log
[31m??[m docs/bpl6-focused-2026-08-06.log
[31m??[m docs/bpl7-focused-2026-08-06.log
[31m??[m docs/cmpzp-focused-2026-08-06.log
[31m??[m docs/cmpzp2-focused-2026-08-06.log
[31m??[m docs/focused-final-2026-08-06.log
[31m??[m docs/focused-gates-final-2026-08-06.log
[31m??[m docs/focused-holdindy-2026-08-06.log
[31m??[m docs/focused-incsticky-2026-08-06.log
[31m??[m docs/focused-irqb-2026-08-06.log
[31m??[m docs/focused-irqfall-2026-08-06.log
[31m??[m docs/focused-irqsuppress-2026-08-06.log
[31m??[m docs/focused-ora-2026-08-06.log
[31m??[m docs/focused-stazp-2026-08-06.log
[31m??[m docs/focused-store-2026-08-06.log
[31m??[m docs/inx-focused-2026-08-06.log
[31m??[m docs/irqrmw-focused-2026-08-06.log
[31m??[m docs/jmp-focused-2026-08-06.log
[31m??[m docs/jmp2-focused-2026-08-06.log
[31m??[m docs/jmp3-focused-2026-08-06.log
[31m??[m docs/lda-focused-2026-08-06.log
[31m??[m docs/lsrx-focused-2026-08-06.log
[31m??[m docs/lsrzp-focused-2026-08-06.log
[31m??[m docs/lsrzp2-focused-2026-08-06.log
[31m??[m docs/pla-focused-2026-08-06.log
[31m??[m docs/pla-late-focused-2026-08-06.log
[31m??[m docs/pla2-focused-2026-08-06.log
[31m??[m docs/receipts-lockstep-10s-2026-08-05.txt
[31m??[m docs/receipts-lockstep-2s-bcc-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-cmp-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-imm-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-jsr-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-ldaabs-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-ldax-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-ldx-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-ldx2-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-nop-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-nopchain-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-plp-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-plp-b-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-rts-diag-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-rts-v2-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-rts-v3-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-rts-v4-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-sty-2026-08-06.log
[31m??[m docs/receipts-lockstep-2s-zpinc-only-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-2026-08-06.txt
[31m??[m docs/receipts-lockstep-everycycle-2s-after-rts-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-2s-after-zpinc-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-2s-attempt-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-after-rts-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-baseline-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-baseline-2026-08-06.txt
[31m??[m docs/receipts-lockstep-everycycle-gates-2026-08-06.log
[31m??[m docs/receipts-lockstep-everycycle-zpinc-fix-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-after-rts-phase-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-bcc-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-cmp-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-imm-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-jsr-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-ldaabs-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-ldax-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-ldx-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-ldx2-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-nop-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-nopchain-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-plp-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-plp-b-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-rts-overlap-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-rts-v2-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-rts-v3-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-rts-v4-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-sty-2026-08-06.log
[31m??[m docs/receipts-lockstep-focused-zpinc-only-2026-08-06.log
[31m??[m docs/receipts-lockstep-vice-realign-2026-08-06.txt
[31m??[m docs/receipts-phase2-lockstep-certification-2026-08-05.txt
[31m??[m docs/receipts-vic20-phase2-verify-2026-08-05.txt
[31m??[m docs/reviews/code-review-20260714T001056552Z.md
[31m??[m docs/reviews/code-review-20260714T004256563Z.md
[31m??[m docs/romless-vic-badline-fix.md
[31m??[m docs/rti-focused-2026-08-06.log
[31m??[m docs/rti2-focused-2026-08-06.log
[31m??[m docs/rtsand-focused-2026-08-06.log
[31m??[m docs/rtsfl-focused-2026-08-06.log
[31m??[m docs/sec-focused-2026-08-06.log
[31m??[m docs/tay-focused-2026-08-06.log
[31m??[m docs/via1-iec-focused-2026-08-06.log
[31m??[m docs/xbox/store-product-identity.md
[31m??[m docs/xbox/store-screenshots/
[31m??[m manifests/
[31m??[m src/ViceSharp.Core/Vic20/Vic20IecPort.cs
[31m??[m tests/ViceSharp.TestHarness/Vic20/BranchCycleCountTests.cs
[31m??[m tests/ViceSharp.TestHarness/Vic20/Vic20DivergeProbe.cs
[31m??[m tests/ViceSharp.TestHarness/Vic20/Vic20Via1IecPortTests.cs
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
remember, always ask, where did I diverge from VICE, and how do I get back in line with VICE?
</user_query>

### Turn 4 (Human)
This session is being continued from a previous conversation that ran out of context. The summary below covers the earlier portion of the conversation.

Summary:
1. Primary Request and Intent: User standing rule: always ask (1) where diverged from VICE and (2) how to realign with VICE (no lag hacks). Goal is Vic20 every-cycle A/X/Y/S/P/PC lockstep vs native xvic (not playable-only). Skeptic/completion gates: NotTakenBne_CycleTrace must be Explicit; residual must not be claimed multi-second/1.1e6 without evidence; continue autonomously. Evaluator latest: diagnose native S drop mid-ROR at c=540740 (IRQ/NMI/JSR phase vs VICE), fix Mos6502 export/IRQ timing, re-run focused+2s, update receipts.

2. Key Technical Concepts: ViceSharp .NET 10; PAL 1108405 Hz; every-cycle lockstep native.Step + managed.Clock.Step; VICE 6510dtvcore BRANCH/PLA/RTI/JSR/ASL/DO_INTERRUPT; soft-defer vs cycle-stage; host-visible PC via _visiblePC / IsInstructionBoundary; SystemClock arms IRQ after CPU tick when IsInstructionBoundary; dual VIA VIC-20; BYRD/receipts.

3. Files and Code Sections: Mos6502.cs (main): soft PLA late pull, RTI defer, soft JMP, taken-branch fallthrough staging, not-taken hold trail>=3 skip, RTS overlapped after full-length branch, shift RMW commit cycle 1/2, suppress boundary after shift RMW final, CompleteJsrTargetFetch always _suppressBootstrapBoundary=true (blocks IRQ sample after JSR - likely root of current residual). BranchCycleCountTests.cs: both TakenBne and NotTakenBne [Fact(Explicit=true)] lines 24,55. Vic20DivergeProbe.cs every-cycle 2s/focused. docs/receipts-lockstep-vice-realign-2026-08-06.txt + many docs/2s-*.log / focused logs.

4. Errors and Fixes: PLA early pull after soft BIT; TAY/TAX after soft; RTI first push; LDA# after RTI nonOvl lag; PLA after BEQ; RTS after AND#; CMP zp after branch (narrow); not-taken hold before RTS prefetch; ASL/LSR zp phase; JMP soft (narrow after STY fail); ADC# after staged JUMP; SEC soft after trail>=3 not-taken; BCC after STA; RTS after full-length branch; BIT soft after branch/fuse after LDA#; LSR zp,X after load (cycle 2); IRQ after RMW (suppress boundary after shift final). Many failed staging attempts regressed focused (BCC lag-only, BNE trail=2 after INC, trail=0+nonOvl, trail-only JMP).

5. Problem Solving: Skeptic flags about NotTaken non-Explicit and residual 517828 are STALE on disk: both Explicit; current matchedCycles=540739 (~0.488s, not multi-s, not 1.1e6). Working hypothesis for c=540740 nS=$F2 mS=$F3 with both PC=$D9B0 op=$76: native mid-IRQ (PC frozen at ROR, first push drops S) while managed executes ROR because CompleteJsrTargetFetch sets suppress so IsInstructionBoundary false after JSR, SystemClock never arms IRQ; native does. fetchingCallTarget only consumed when suppress true, so suppress cannot simply be removed without preserving call-target fetch/IRQ arming correctly.

6. All User Messages:
- "remember, always ask, where did I diverge from VICE, and how do I get back in line with VICE?"
- System goal-continue / skeptic / evaluator: complete lockstep; fix NotTaken Explicit; residual honesty; VIA/PLA chain; then BPL restage; then diagnose mid-ROR S drop and re-run tests/receipts.

7. Pending Tasks: Fix IRQ after JSR (suppress vs VICE DO_INTERRUPT boundary); re-run focused 500k + 2s; update receipts; leave NotTaken Explicit; only claim multi-s/done with green 2s or hard VICE-block evidence. Skeptic residual 517828 is historical - real residual is 540739 ROR/IRQ.

8. Current Work: Diagnosing c=540740 after IRQ-after-RMW fix. Reading SystemClock IRQ sample and CompleteJsrTargetFetch: suppress after JSR blocks IRQ; hypothesis native IRQ mid-ROR with frozen PC explains S-only mismatch. Skeptic re-check done: Explicit true; residual 540739 not 517828.

9. Optional Next Step: Realign JSR-end IRQ sample with VICE (allow Irq() after JSR complete when line asserted without breaking call-target fetch phase), then re-run FocusedWindow + VICESHARP_LOCKSTEP_2S=1 TwoSecondPal and update docs/receipts-lockstep-vice-realign-2026-08-06.txt with matchedCycles/first-diverge. Quote: "Diagnose native S drop mid-ROR at c=540740 (IRQ/NMI/JSR stack phase vs VICE), fix Mos6502 export timing, then re-run focused + 2s and update the receipt" / dump nS=$F2 mS=$F3 op=$76 at D9B0.

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
- [in_progress] 2: Diagnose native S drop mid-ROR at c=540740 vs VICE
- [pending] 3: Fix export timing; re-run focused+2s; update receipt
(1 completed)

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
Standing rule noted: every fix starts from “where did we diverge from VICE?” and “how do we realign?” Continuing the mid-ROR S-drop diagnosis (c=540740) against VICE IRQ/JSR boundary timing.
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
- limit: 80
[tool_request: grep]
- pattern: CompleteJsrTargetFetch|_suppressBootstrapBoundary|IsInstructionBoundary|fetchingCallTarget
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu
[tool_request: grep]
- pattern: DO_INTERRUPT|interrupt.*after.*JSR|OP_JSR|JSR
- path: F:\GitHub\vice-sharp\native\vice
- glob: **/6510*.c
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
- limit: 80

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
1→ViceSharp VIC-20 every-cycle lockstep vs xvic - VICE realign session
Date: 2026-08-06
Method: always ask (1) where did we diverge from VICE? (2) how do we get back in line?

Skeptic gaps
------------
1. NotTakenBne / TakenBne: both [Fact(Explicit = true)] BranchCycleCountTests.cs:24,55
2. Focused every-cycle gate: EveryCycle_CpuRegs_Match_FocusedWindow (500_000)
3. Residual honesty: matchedCycles below - NOT multi-second, NOT 1.1e6
10→
Gates (machine-verified)
------------------------
Focused 500k + Via1Iec + Vic20NativeLockstepTests:
  FOCUSED_EXIT=0, Passed 20 / Failed 0
  latest: docs/irqrmw-focused-2026-08-06.log

2s (VICESHARP_LOCKSTEP_2S=1):
  matchedCycles=540739  DIV first=540740  budget=2216810
  ~0.488 s PAL (540739/1108405) - NOT multi-second, NOT 1.1e6
20→  receipt: docs/2s-irqrmw-2026-08-06.log

Progress vs skeptic residual (517828)
-------------------------------------
Skeptic residual:     matchedCycles=517828
Session resume:       matchedCycles=522468
BPL restage start:    matchedCycles=540200
Current residual:     matchedCycles=540739
Net vs skeptic:       +22911 every-cycle matches

30→VICE realignments this continuation
-----------------------------------
1. Taken multi-cycle BPL/BCC fall-through + JUMP staging (narrowed by budget/trail/store)
2. Soft-imm skip after staged taken JUMP; BIT soft after branch / fuse after LDA#
3. Not-taken hold trail>=3 skip + nonOvl clear for fused SEC
4. RTS overlapped after full-length taken branch
5. Shift RMW: zp cycle-2; zp,X cycle-1 after branch, cycle-2 after load
6. Suppress IRQ boundary one CLK after shift RMW final sample

Current residual (VICE-facing)
40→------------------------------
Where:
  c=540740 ROR zp,X $76 at $D9B0 after JSR (trail=4)
  nS=$F2 mS=$F3 (native S one lower mid-ROR; no IRQ flag in dump)

Evidence: docs/2s-irqrmw-2026-08-06.log

How diverged:
  Matched through JSR to ROR start. Mid ROR (dbgCyc=4) native S decremented
  while managed holds. ROR does not push; investigate concurrent stack push
50→  (NMI/IRQ not shown as irq>0) or JSR leftover phase vs xvic.

How to realign:
  Probe native IRQ/NMI lines and stack activity around c=540740; realign
  interrupt or JSR leave with VICE.

Not done / do not claim
-----------------------
- Full 2s every-cycle green
- Match past ~1.1e6 cycles
60→- Multi-second lockstep (still ~0.488 s)


### Turn 9 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 41 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
24:        get => IsInstructionBoundary ? _pc : _visiblePC;
39:    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0;
195:                _suppressBootstrapBoundary = true;
234:    private bool _suppressBootstrapBoundary;
416:        var fetchingCallTarget = false;
417:        if (_suppressBootstrapBoundary)
420:            fetchingCallTarget = _callTargetFetchPending;
423:            _suppressBootstrapBoundary = false;
439:            _suppressBootstrapBoundary = true;
468:            _suppressBootstrapBoundary = true;
812:                    _suppressBootstrapBoundary = true;
828:                _suppressBootstrapBoundary = true;
844:                _suppressBootstrapBoundary = true;
856:                _suppressBootstrapBoundary = true;
870:                _suppressBootstrapBoundary = true;
895:                _suppressBootstrapBoundary = true;
905:                _suppressBootstrapBoundary = true;
916:                _suppressBootstrapBoundary = true;
928:                _suppressBootstrapBoundary = true;
964:        _suppressBootstrapBoundary = true;
995:                    _suppressBootstrapBoundary = true;
1034:                CompleteJsrTargetFetch();
1041:    private void CompleteJsrTargetFetch()
1073:        _suppressBootstrapBoundary = true;
1121:                _suppressBootstrapBoundary = true;
1131:                _suppressBootstrapBoundary = true;
1141:                _suppressBootstrapBoundary = true;
1153:                _suppressBootstrapBoundary = true;
1235:                _suppressBootstrapBoundary = true;
1252:            _suppressBootstrapBoundary = true;
1728:                    _suppressBootstrapBoundary = true;
1734:                _suppressBootstrapBoundary = true;
1823:            _suppressBootstrapBoundary = true;
1841:                _suppressBootstrapBoundary = true;
1885:        _suppressBootstrapBoundary = true;
2287:        // as the following opcode's first FETCH. Suppress IsInstructionBoundary
2293:        _suppressBootstrapBoundary = true;
2313:        _suppressBootstrapBoundary = true;
2471:            _suppressBootstrapBoundary = true;
2497:        _suppressBootstrapBoundary = true;
2560:        _suppressBootstrapBoundary = true;
</workspace_result>

### Turn 10 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 30 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\6510dtvcore.c
264:#define CHECK_PROFILE_JSR(dest_addr)     \
281:#define CHECK_PROFILE_JSR(dest_addr)
354:#define DO_INTERRUPT(int_kind)                                                 \
1194:/* HACK: fix JSR MSB in monitor CPU history */
1196:#define JSR_FIXUP_MSB(x)    monitor_cpuhistory_fix_p2(x)
1198:#define JSR_FIXUP_MSB(x)
1201:#define JSR()                                     \
1215:        JSR_FIXUP_MSB(addr_msb);                  \
1218:        CHECK_PROFILE_JSR(dest_addr);             \
1763:            DO_INTERRUPT(pending_interrupt);
1815:        /* If reg_pc >= bank_limit  then JSR (0x20) hasn't load p2 yet.
1817:           The fixing is now handled in JSR(). */
2022:            case 0x20:          /* JSR $nnnn */
2023:                JSR();

F:\GitHub\vice-sharp\native\vice\vice\src\6510core.c
91:#define CLK_JSR_INT_CYCLE 1
384:#define CHECK_PROFILE_JSR(dest_addr)     \
401:#define CHECK_PROFILE_JSR(dest_addr)
436:#define DO_INTERRUPT(int_kind)                                                                 \
1277:/* HACK: fix JSR MSB in monitor CPU history */
1279:#define JSR_FIXUP_MSB(x)    monitor_cpuhistory_fix_p2(x)
1281:#define JSR_FIXUP_MSB(x)
1284:#define JSR()                                         \
1296:        JSR_FIXUP_MSB(addr_msb);                      \
1298:        CLK_ADD(CLK, CLK_JSR_INT_CYCLE);              \
1299:        CHECK_PROFILE_JSR(tmp_addr);                  \
2336:            DO_INTERRUPT(pending_interrupt);
2406:        /* If reg_pc >= bank_limit  then JSR (0x20) hasn't load p2 yet.
2408:           The fixing is now handled in JSR(). */
2644:            case 0x20:          /* JSR $nnnn */
2645:                JSR();
</workspace_result>

### Turn 11 (Assistant)
[tool_request: list_dir]
- target_directory: C:\Users\kingd\.claude\profile
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 400
- limit: 90
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1000
- limit: 90
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\6510core.c
- offset: 1280
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\6510core.c
- offset: 430
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\6510core.c
- offset: 2320
- limit: 50

### Turn 12 (Function)
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

### Turn 13 (Function)
[tool_response]
400→        {
            TickCore();
        }
        finally
        {
            if (_visiblePC != _opcodeAddress)
            {
                _currentInsnTrailingAtNextPc++;
                _trailingCyclesAtNextPc = _currentInsnTrailingAtNextPc;
            }
410→        }
    }

    private void TickCore()
    {
        var fetchingBranchTarget = false;
        var fetchingCallTarget = false;
        if (_suppressBootstrapBoundary)
        {
            fetchingBranchTarget = _branchTargetFetchPending;
420→            fetchingCallTarget = _callTargetFetchPending;
            _branchTargetFetchPending = false;
            _callTargetFetchPending = false;
            _suppressBootstrapBoundary = false;
        }

        if (_interruptSequenceRemaining > 0)
        {
            ExecuteInterruptSequenceCycle();
            return;
430→        }

        if (_branchPageCrossExtraPending)
        {
            // TR-LOCKSTEP-VSF-001: the taken-branch page-cross fix-up cycle
            // (native BRANCH C4); the fall-through PC stays visible and the
            // target fetch (with its after-branch defer arming) runs next tick.
            _branchPageCrossExtraPending = false;
            _branchTargetFetchPending = true;
            _suppressBootstrapBoundary = true;
440→            return;
        }

        if (_pendingDeferredNzUpdateAfterBranch)
        {
            // Do not steal the final cycle of a staged (zp),Y load/compare; finish
            // that apply first so A/flags land on the native data-read export.
            if (!(_stagedMemoryReadCompleted && (_opcode is 0xB1 or 0xD1)))
            {
                CompleteDeferredNzUpdateAfterBranch();
450→                return;
            }
        }

        if (_delayNextFetch)
        {
            _instructionPC = _pc;
            _visiblePC = _pc;
            _delayNextFetch = false;
            // Following fetch is phase-coupled like a clean first FETCH for BIT
460→            // host-sample count (c=522467 held late after RTS return).
            _fetchAfterRtsDelay = true;
            return;
        }

        if (_bootstrapCycles > 0)
        {
            _bootstrapCycles--;
            _suppressBootstrapBoundary = true;
            return;
470→        }

        if (_pendingDeferredImmediateLoad)
        {
            CompleteDeferredImmediateLoad();
            return;
        }

        if (_pendingDeferredImpliedRegisterCompletion)
        {
480→            CompleteDeferredImpliedRegisterCompletion();
            return;
        }

        if (_cycle == 0)
        {
            // Instruction boundary: the previous instruction has fully executed.
            // Publish it (opcode + post-execution registers) for diagnostic / pacing
            // subscribers. Gated on a live subscriber so an unobserved run pays only
            // a null + count check per instruction; pure notification, so cycle parity

### Turn 14 (Function)
[tool_response]
1000→                {
                    // Clean first FETCH: keep PC at the opcode for the STACK_PEEK
                    // CLK as well (VICE: 2 FETCH + PEEK all export reg_pc at JSR).
                    _visiblePC = _instructionPC;
                    return true;
                }

                // Overlapped first FETCH (common after lag-shaped previous insn):
                // this CLK is already the post-peek INC_PC + PUSH high.
                _pc = (ushort)(_instructionPC + 2);
1010→                _visiblePC = _pc;
                Push((byte)(_pc >> 8));
                return true;
            case 2:
                if (nonOverlappedJsr && _visiblePC == _instructionPC)
                {
                    // Clean path: INC_PC + PUSH high on this CLK.
                    _pc = (ushort)(_instructionPC + 2);
                    _visiblePC = _pc;
                    Push((byte)(_pc >> 8));
1020→                    return true;
                }

                Push((byte)_pc);
                return true;
            case 1:
                if (nonOverlappedJsr)
                {
                    Push((byte)_pc);
                    return true;
1030→                }

                return false;
            case 0:
                CompleteJsrTargetFetch();
                return true;
            default:
                return false;
        }
    }
1040→
    private void CompleteJsrTargetFetch()
    {
        var source = _instructionPC;
        var lo = Read((ushort)(_instructionPC + 1));
        // _pc is already instructionPC+2 after the push phase (VICE loads MSB here).
        var hi = Read(_pc);
        var target = (ushort)(lo | (hi << 8));
        var returnPc = (ushort)(source + 3);
        var nonOverlappedJsr = _afterFullLengthTakenBranch
1050→            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase;
        if (nonOverlappedJsr)
        {
            // VICE JUMP runs after the final CLK_INC export, so this cycle still
            // shows the post-INC_PC address; target is live for the next FETCH.
            var exportPc = _pc;
            _pc = target;
            _instructionPC = exportPc;
            _visiblePC = exportPc;
1060→            // Callee starts without first-FETCH overlap.
            _nonOverlappedFetchPhase = true;
        }
        else
        {
            // Overlapped path: host-visible last CLK already matches target
            // (phase coupling with previous instruction).
            PC = target;
        }

1070→        PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        _suppressBootstrapBoundary = true;
    }

    private void PublishControlTransfer(ushort source, ushort target, ushort returnPc, byte opcode)
    {
        _pubSub?.Publish(CpuControlTransferEvent.Topic, new CpuControlTransferEvent(source, target, returnPc, opcode));
    }
1080→
    private bool TryExecuteCycleStagedMemoryReadOpcode()
    {
        if (TryExecuteCycleStagedBranchOpcode())
        {
            return true;
        }

        if (TryExecuteCycleStagedRtsOpcode())
        {

### Turn 15 (Function)
[tool_response]
1280→#else
#define JSR_FIXUP_MSB(x)
#endif

#define JSR()                                         \
    do {                                              \
        uint8_t addr_msb;                             \
        unsigned int tmp_addr;                        \
                                                      \
        LOAD_DUMMY(0x100 + reg_sp);                   \
1290→        CLK_ADD(CLK, 1);                              \
        INC_PC(2);                                    \
        CLK_ADD(CLK, 2);                              \
        PUSH(((reg_pc) >> 8) & 0xff);                 \
        PUSH((reg_pc) & 0xff);                        \
        addr_msb = LOAD(reg_pc);                      \
        JSR_FIXUP_MSB(addr_msb);                      \
        tmp_addr = (p1 | (addr_msb << 8));            \
        CLK_ADD(CLK, CLK_JSR_INT_CYCLE);              \
        CHECK_PROFILE_JSR(tmp_addr);                  \
1300→        JUMP(tmp_addr);                               \
    } while (0)

#define LAS(value, clk_inc, pc_inc) \
    do {                            \
        reg_sp = reg_sp & (value);  \
        reg_x_write(reg_sp);        \
        reg_a_write(reg_sp);        \
        LOCAL_SET_NZ(reg_sp);       \
        CLK_ADD(CLK, (clk_inc));    \
1310→        INC_PC(pc_inc);             \
    } while (0)

#define LAX(value, clk_inc, pc_inc) \
    do {                            \
        uint8_t tmp = (value);         \
        reg_x_write(tmp);           \
        reg_a_write(tmp);           \
        LOCAL_SET_NZ(tmp);          \
        CLK_ADD(CLK, (clk_inc));    \

### Turn 16 (Function)
[tool_response]
2320→
    {
        enum cpu_int pending_interrupt;

        if (!(CPU_INT_STATUS->global_pending_int & IK_IRQ)
            && (CPU_INT_STATUS->global_pending_int & IK_IRQPEND)
            && CPU_INT_STATUS->irq_pending_clk <= CLK) {
            interrupt_ack_irq(CPU_INT_STATUS);
        }

2330→        pending_interrupt = CPU_INT_STATUS->global_pending_int;
        if (pending_interrupt != IK_NONE) {
#if !defined(DRIVE_CPU)
            profiling_clock_start = CLK;
#endif

            DO_INTERRUPT(pending_interrupt);
            if (!(CPU_INT_STATUS->global_pending_int & IK_IRQ)
                && CPU_INT_STATUS->global_pending_int & IK_IRQPEND) {
                CPU_INT_STATUS->global_pending_int &= ~IK_IRQPEND;
2340→            }
            CPU_DELAY_CLK

            PROCESS_ALARMS
        }
    }

    {
        opcode_t opcode;
#ifdef DEBUG
2350→        CLOCK debug_clk;
#ifdef DRIVE_CPU
        debug_clk = CLK;
#else
        debug_clk = maincpu_clk;
#endif
#endif

#ifdef FEATURE_CPUMEMHISTORY
        CLOCK history_clk;
2360→#ifndef DRIVE_CPU
        history_clk = maincpu_clk;
        memmap_state |= (MEMMAP_STATE_INSTR | MEMMAP_STATE_OPCODE);
#else
        history_clk = CLK;
#endif
#endif

#if !defined(DRIVE_CPU)
        profiling_clock_start = CLK;

### Turn 17 (Function)
[tool_response]
430→#define TRACE_BRK()
#endif

/* Perform the interrupts in `int_kind'.  If we have both NMI and IRQ,
   execute NMI. NMI can take over an in progress IRQ.  */
/* FIXME: LOCAL_STATUS() should check byte ready first.  */
#define DO_INTERRUPT(int_kind)                                                                 \
    do {                                                                                       \
        uint8_t ik = (int_kind);                                                               \
        uint16_t addr, handler_vector;                                                         \
440→                                                                                               \
        if (ik & (IK_IRQ | IK_IRQPEND | IK_NMI)) {                                             \
            if (((ik & IK_NMI)                                                                 \
                 && interrupt_check_nmi_delay(CPU_INT_STATUS, CLK))                            \
                || ((ik & (IK_IRQ | IK_IRQPEND)) && (!LOCAL_INTERRUPT()                        \
                                                     || OPINFO_DISABLES_IRQ(LAST_OPCODE_INFO)) \
                    && interrupt_check_irq_delay(CPU_INT_STATUS, CLK))) {                      \
                if (monitor_mask[CALLER] & (MI_STEP)) {                                        \
                    monitor_check_icount_interrupt();                                          \
                }                                                                              \
450→                if (NMI_CYCLES == 7) {                                                         \
                    FETCH_PARAM_DUMMY(reg_pc);   /* dummy reads */                             \
                    CLK_ADD(CLK, 1);                                                           \
                    FETCH_PARAM_DUMMY(reg_pc);                                                 \
                    CLK_ADD(CLK, 1);                                                           \
                }                                                                              \
                LOCAL_SET_BREAK(0);                                                            \
                PUSH(reg_pc >> 8);                                                             \
                PUSH(reg_pc & 0xff);                                                           \
                CLK_ADD(CLK, 2);                                                               \
460→                PUSH(LOCAL_STATUS());                                                          \
                CLK_ADD(CLK, 1);                                                               \
                LOCAL_SET_INTERRUPT(1);                                                        \
                CPU_DELAY_CLK; /* process alarms for cartridge freeze */                       \
                PROCESS_ALARMS;                                                                \
                if ((CPU_INT_STATUS->global_pending_int & IK_NMI)                              \
                    && (CLK >= (CPU_INT_STATUS->nmi_clk + INTERRUPT_DELAY))) {                 \
                    TRACE_NMI(CLK - NMI_CYCLES + 2);                                           \
                    interrupt_ack_nmi(CPU_INT_STATUS);                                         \
                    handler_vector = 0xfffa;                                                   \
470→                } else {                                                                       \
                    TRACE_IRQ(CLK - IRQ_CYCLES + 2);                                           \
                    interrupt_ack_irq(CPU_INT_STATUS);                                         \
                    handler_vector = 0xfffe;                                                   \
                }                                                                              \
                addr = LOAD_ADDR(handler_vector);                                              \
                CHECK_PROFILE_INTERRUPT(addr, handler_vector);                                 \
                JUMP(addr);                                                                    \
                SET_LAST_OPCODE(0);                                                            \
                CLK_ADD(CLK, 2);                                                               \
480→            }                                                                                  \
        }                                                                                      \
        if (ik & (IK_TRAP | IK_RESET)) {                                                       \
            if (ik & IK_TRAP) {                                                                \
                EXPORT_REGISTERS();                                                            \
                interrupt_do_trap(CPU_INT_STATUS, (uint16_t)reg_pc);                           \
                IMPORT_REGISTERS();                                                            \
                if (CPU_INT_STATUS->global_pending_int & IK_RESET) {                           \
                    ik |= IK_RESET;                                                            \
                }                                                                              \
490→            }                                                                                  \
            if (ik & IK_RESET) {                                                               \
                cpu_reset();                                                                   \
                interrupt_ack_reset(CPU_INT_STATUS);                                           \
                bank_start = bank_limit = 0; /* prevent caching */                             \
                LOCAL_SET_INTERRUPT(1);                                                        \
                CPU_IS_JAMMED = 0;                                                             \
                DMA_ON_RESET;                                                                  \
                addr = LOAD_ADDR(0xfffc);                                                      \
                CHECK_PROFILE_INTERRUPT(addr, 0xfffc);                                         \
500→                JUMP(addr);                                                                    \
            }                                                                                  \
        }                                                                                      \
        if (ik & (IK_MONITOR | IK_DMA)) {                                                      \
            if (ik & IK_MONITOR) {                                                             \
                if (monitor_mask[CALLER] & (MI_STEP)) {                                        \
                    EXPORT_REGISTERS();                                                        \
                    monitor_check_icount((uint16_t)reg_pc);                                    \
                    IMPORT_REGISTERS();                                                        \
                }                                                                              \

### Turn 18 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 485
- limit: 120
[tool_request: grep]
- pattern: fetchingCallTarget|callTargetFetch|_afterFullLengthTakenBranch|Irq\(|IsInstructionBoundary|pendingIrq
- path: F:\GitHub\vice-sharp\src
- glob: **/*.{cs}
- head_limit: 60
[tool_request: run_terminal_command]
- command: Get-ChildItem "C:\Users\kingd\.claude\profile\*.md" | Where-Object { $_.Name -notlike 'add-profile*' } | ForEach-Object { $_.Name }
- description: List profile markdown files to load

### Turn 19 (Function)
[tool_response]
485→        {
            // Instruction boundary: the previous instruction has fully executed.
            // Publish it (opcode + post-execution registers) for diagnostic / pacing
            // subscribers. Gated on a live subscriber so an unobserved run pays only
            // a null + count check per instruction; pure notification, so cycle parity
490→            // is unaffected.
            if (_instructionExecuted && _pubSub is { SubscriptionCount: > 0 })
            {
                _pubSub.Publish(
                    CpuInstructionCompletedEvent.Topic,
                    new CpuInstructionCompletedEvent(_currentInstructionPc, _opcode, A, X, Y, S, P, _pc));
            }

            // Soft-deferred JMP abs: apply JUMP before fetching the target insn.
            // Target's first FETCH is clean (c=539733 INX nX/nPC lag without this).
500→            if (_pendingJmpTarget != 0 && !_softDeferJmpAbs)
            {
                _pc = _pendingJmpTarget;
                _pendingJmpTarget = 0;
                _nonOverlappedFetchPhase = true;
            }

            // Soft-deferred immediate from previous insn: commit A/X/Y while
            // _instructionPC / _opcode still name that previous immediate load.
            if (_softDeferredImmediateLoad)
510→            {
                CompleteDeferredImmediateLoad();
                _softDeferredImmediateLoad = false;
                // Keep _holdIndYStorePcOneCycle for the following STA (zp),Y.
            }

            // Soft-deferred implied (CLC/TAX/TXA/...) or post-NOP-chain
            // 2-byte immediate: commit after last-CLK sample.
            var afterSoftBody = false;
            if (_softDeferredImpliedOp)
520→            {
                var savedOpcode = _opcode;
                var savedInsnPc = _instructionPC;
                var softOp = _softDeferredImpliedOpcode;
                _opcode = softOp;
                _instructionPC = _softDeferredImpliedInstructionPc;
                ExecuteOpcode(softOp);
                _opcode = savedOpcode;
                _instructionPC = savedInsnPc;
                _softDeferredImpliedOp = false;
530→                // Following insn has a clean first FETCH (same as afterSoftCompare).
                afterSoftBody = true;
                // Stack-pull ops after soft-deferred body (PLA after BIT/TAY,
                // RTI after PLA chain): VICE still on STACK_PEEK when managed
                // would pull early. Flags only consumed by PLA/RTI.
                _plaDeferPullOne = true;
                _rtiDeferFirstPullOne = true;
            }

            // Soft-deferred CMP: apply C/NZ after VICE's last-CLK sample.
540→            var afterSoftCompare = false;
            if (_pendingSoftCompareCommit)
            {
                if (_stagedCarryUpdate)
                {
                    if (_stagedCarryValue)
                        P |= 0x01;
                    else
                        P &= 0xFE;
                    _stagedCarryUpdate = false;
550→                }

                if (_stagedNzUpdate)
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }

                _pendingSoftCompareCommit = false;
                // Next instruction's first FETCH is not overlapped (CMP left PC
560→                // on its opcode address through its final CLK).
                afterSoftCompare = true;
            }

            afterSoftCompare = afterSoftCompare || afterSoftBody;

            _instructionPC = _pc;
            _visiblePC = _instructionPC;
            _currentInstructionPc = _pc; // the instruction about to be fetched here
            _opcodeAddress = _pc;
570→
            // KERNAL serial-bus trap (VICE virtual device traps). If a trap fires
            // it has set PC to the routine's resume address; skip the trapped
            // instruction and re-fetch from there on the next cycle. The hook is
            // a no-op (returns false) unless a virtual disk is being addressed,
            // so cycle-accurate behaviour is unchanged in every other case.
            if (SerialTrapHook is not null && SerialTrapHook(_pc))
            {
                return;
            }
580→
            // Capture previous instruction's trailing dwell, then start fresh.
            var priorTrailingAtNextPc = _trailingCyclesAtNextPc;
            DebugPriorTrailingAtNextPc = priorTrailingAtNextPc;
            _trailingCyclesAtNextPc = 0;
            _currentInsnTrailingAtNextPc = 0;

            // Preserve prior opcode before overwrite (RTS overlap phase uses it).
            _previousOpcode = _opcode;
            _opcode = Read(_pc++);
590→            // holdIndY is only for the instruction immediately after a soft-deferred
            // load/implied; clear if this fetch is not STA (zp),Y (c=519327 false hold).
            if (_opcode != 0x91)
                _holdIndYStorePcOneCycle = false;
            // deferZpRmwPcAdvanceOne is only for the zp INC/DEC immediately after a
            // not-taken branch clean FETCH; sticky flag delayed INC NZ/PC by one
            // cycle far from any branch (c=522317 nP=$25 mP=$27 nPC advanced).
            if (!IsZeroPageIncrementDecrementOpcode(_opcode))
                _deferZpRmwPcAdvanceOne = false;
            _instructionExecuted = true; // a real instruction has now been fetched/executed
600→            _cycle = GetCycleCount(_opcode);
            // Match host-visible xvic taken-branch length (VICE BRANCH is always
            // 3 CLK; first FETCH may be overlapped with the previous opcode's
            // trailing export at this same address).
            // Empirically vs xvic GetPC after each step_cycle:

### Turn 20 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 47 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
613:        while (!_cpu.IsInstructionBoundary);

F:\GitHub\vice-sharp\src\ViceSharp.Core\CoordinatorMachine.cs
97:        while (cpu is not null && !cpu.IsInstructionBoundary);

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ICpu.cs
31:    bool IsInstructionBoundary => true;
42:    void Irq();

F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
90:        if (_cpu != null && _nmiPending && _cpu.IsInstructionBoundary)
95:        else if (_cpu != null && _irqLine != null && _irqLine.IsAsserted && _cpu.IsInstructionBoundary)
97:            _cpu.Irq();

F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\Commodore64.cs
110:        while (!_cpu.IsInstructionBoundary);

F:\GitHub\vice-sharp\src\ViceSharp.Chips\IEC\Via6522.cs
210:                RefreshIrq();
234:                RefreshIrq();
241:                RefreshIrq();
249:                RefreshIrq();
293:                RefreshIrq();
319:                RefreshIrq();
327:                RefreshIrq();
336:                RefreshIrq();
346:                RefreshIrq();
405:                    RefreshIrq();
413:                RefreshIrq();
475:                RefreshIrq();
509:                RefreshIrq();
539:            RefreshIrq();
561:            RefreshIrq();
581:            RefreshIrq();
609:            RefreshIrq();
643:            RefreshIrq();
756:            RefreshIrq();
760:    private void RefreshIrq()

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
24:        get => IsInstructionBoundary ? _pc : _visiblePC;
39:    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0;
104:                return _branchTargetFetchPending || _callTargetFetchPending;
125:    public void Irq()
253:    private bool _afterFullLengthTakenBranch;
273:    private bool _callTargetFetchPending;
416:        var fetchingCallTarget = false;
420:            fetchingCallTarget = _callTargetFetchPending;
422:            _callTargetFetchPending = false;
659:            _afterFullLengthTakenBranch = (fetchingBranchTarget && _fullLengthTakenBranchCompleted)
661:            if (_afterFullLengthTakenBranch)
679:                && ((_afterFullLengthTakenBranch)
687:            var armAfterBranchLag = fetchingBranchTarget && !_afterFullLengthTakenBranch;
701:                && (armAfterBranchLag || _afterFullLengthTakenBranch);
734:                ((armAfterBranchLag || _afterFullLengthTakenBranch) && IsIndirectYLoadOpcode(_opcode))
977:        var nonOverlappedJsr = _afterFullLengthTakenBranch
1049:        var nonOverlappedJsr = _afterFullLengthTakenBranch
1072:        _callTargetFetchPending = true;
1937:        _callTargetFetchPending = false;
</workspace_result>

### Turn 21 (Function)
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


### Turn 22 (Assistant)
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\PROFILE.md
- limit: 100
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\user-payton-byrd.md
- limit: 80
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md
- limit: 40
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\approve-before-execute.md
- limit: 40
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\philosophical-dialogue-mode.md
- limit: 40
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md
- limit: 40
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\session-turn-title-summary.md
- limit: 40
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\never-skip-explicit-actions.md
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 650
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 100
- limit: 120
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
- offset: 70
- limit: 50
[tool_request: grep]
- pattern: 54074|nS=|DIV|ROR|irq|S=
- path: F:\GitHub\vice-sharp\docs\2s-irqrmw-2026-08-06.log
- head_limit: 40

### Turn 23 (Function)
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


### Turn 24 (Function)
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

### Turn 25 (Function)
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


### Turn 26 (Function)
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


### Turn 27 (Function)
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


### Turn 28 (Function)
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


### Turn 29 (Function)
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


### Turn 30 (Function)
[tool_response]
650→            if (_opcode != 0x40)
                _rtiDeferFirstPullOne = false;
            var deferIndirectYLoadAfterBranchRmw = _deferNextIndirectYLoadAfterBranchRmw && IsIndirectYLoadOpcode(_opcode);
            _deferNextIndirectYLoadAfterBranchRmw = false;
            // Consume full-length-branch credit for THIS instruction (e.g. JSR after BNE).
            // Also JSR after deferred zp RMW (not-taken BNE -> INC -> JSR at 20761).
            var jsrAfterDeferredRmw = _opcode == 0x20 && _nextJsrNonOverlapped;
            if (jsrAfterDeferredRmw)
                _nextJsrNonOverlapped = false;
            _afterFullLengthTakenBranch = (fetchingBranchTarget && _fullLengthTakenBranchCompleted)
660→                || jsrAfterDeferredRmw;
            if (_afterFullLengthTakenBranch)
            {
                _nonOverlappedFetchPhase = true;
                _nonOverlappedRegion = true;
            }

            // VICE LD #imm (6510dtvcore LD after FETCH): INC_PC with no trailing
            // CLK_INC, so next opcode's first FETCH fuses into the first host
            // sample that already shows post-commit PC/Y. Managed LDY last tick
670→            // exports that PC without consuming RTS F1 (c=518540 nS ahead).
            // Absolute stores (STY abs at c=502759) and taken branches (c=30)
            // do not need early pull: their last CLK / JUMP coupling differs.
            // VICE RTS after a 2-byte immediate (LD#/AND#/CMP#/...): first PULL is
            // visible at dbgCyc=3 when prior trailing dwell is 1 (c=532024 nS ahead
            // of mS after AND # $29). Was limited to LDA/LDX/LDY # only.
            // Also after full-length taken branch (c=540341 nS=$FA mS=$F9 on RTS
            // after BCC): first PULL is early like the overlapped schedule.
            _rtsOverlappedFirstFetch = _opcode == 0x60
                && ((_afterFullLengthTakenBranch)
680→                    || (priorTrailingAtNextPc == 1
                        && IsTwoByteImmediateOpcode(_previousOpcode)
                        && !fetchingBranchTarget
                        && !afterSoftCompare
                        && !_nonOverlappedRegion
                        && !_nonOverlappedFetchPhase));

            var armAfterBranchLag = fetchingBranchTarget && !_afterFullLengthTakenBranch;
            _fullLengthTakenBranchCompleted = fullLengthTakenBranch;
            _deferImmediateLoadAfterBranch = armAfterBranchLag && IsImmediateLoadOpcode(_opcode);
690→            _deferImpliedRegisterCompletionAfterBranch = armAfterBranchLag && IsImpliedRegisterOrFlagOpcode(_opcode);
            // PLA after short taken branch: VICE still on STACK_PEEK when managed
            // would pull at cycle 1 (c=531993 nA=$00 mA=$18 after BEQ).
            if (armAfterBranchLag && _opcode == 0x68)
                _plaDeferPullOne = true;
            if (armAfterBranchLag && _opcode == 0x40)
                _rtiDeferFirstPullOne = true;
            // CMP zp family after taken-branch lag only (not after JSR trail>=2;
            // that regressed CPY at c=517861 where VICE already commits).
            // Short/full taken-branch targets soft-defer flags/PC (c=532264).
700→            _softDeferZpCompare = IsZeroPageCompareOpcode(_opcode)
                && (armAfterBranchLag || _afterFullLengthTakenBranch);
            // JMP abs soft JUMP when first FETCH is clean non-overlapped (after
            // zp shift RMW c=539731, or soft-implied CLC c=539753). Not when
            // trail alone after STY (c=502749 nPC=target already).
            _softDeferJmpAbs = _opcode == 0x4C
                && (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    || _nonOverlappedFetchPhase || _nonOverlappedRegion);
            // Short taken branch -> NOP chain: hold opcode PC on each NOP's final
            // CLK (VICE INC_PC after last FETCH). Sticks across consecutive NOPs
710→            // so the second padding NOP after the branch also matches (c=518741).
            if (_opcode == 0xEA && (armAfterBranchLag || _nopChainHoldAfterBranch))
            {
                _nopHoldOpcodePcOnFinal = true;
                _nopChainHoldAfterBranch = true;
                _softDeferAfterNopChain = false;
            }
            else
            {
                // Leaving the chain: next insn needs soft-defer of its commit so
720→                // host still samples opcode PC on its final FETCH CLK (c=518751).
                _softDeferAfterNopChain = _nopChainHoldAfterBranch && _opcode != 0xEA;
                _nopHoldOpcodePcOnFinal = false;
                _nopChainHoldAfterBranch = false;
            }
            _deferJsrPushAfterBranch = armAfterBranchLag && _opcode == 0x20;
            var fetchingIndexedLoadControlTarget = armAfterBranchLag;
            _deferAbsoluteXLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteXLoadOpcode(_opcode);
            _deferAbsoluteYLoadCompletionAfterBranch = fetchingIndexedLoadControlTarget && IsAbsoluteYLoadOpcode(_opcode);
            // Delay A commit for (zp),Y after a full-length taken branch as well:
730→            // VICE still exports the pre-load A on the data-read CLK when the
            // first FETCH of this load was not overlapped (same host-visible
            // shape as the after-branch lag path without the extra cycle budget).
            _deferIndirectYLoadCompletionAfterBranch =
                ((armAfterBranchLag || _afterFullLengthTakenBranch) && IsIndirectYLoadOpcode(_opcode))
                || deferIndirectYLoadAfterBranchRmw;
            _deferZeroPageRmwPcAdvanceAfterBranch = armAfterBranchLag && IsZeroPageIncrementDecrementOpcode(_opcode);
            _deferIndexedStorePcAdvanceAfterBranch = armAfterBranchLag && IsIndexedAbsoluteStoreOpcode(_opcode);
            _deferZeroPageIndexedStorePcAdvanceAfterBranch = armAfterBranchLag && IsZeroPageIndexedStoreOpcode(_opcode);
            // Short taken branch: lag the following instruction by one CLK.
740→            if (armAfterBranchLag && IsAfterBranchBudgetExtendedOpcode(_opcode))
            {
                _cycle++;
            }

            // Stage fall-through at cycle 1 when:
            //  - budget 4+ (full-length + after-branch lag): c=540201
            //  - full-length with priorTrailing>=3 (c=540231)
            //  - full-length trail=2 after store (c=540337 BCC after STA zp)
            // Not: full-length trail=2 after INC (c=5005 keeps opcode at cyc 1)

### Turn 31 (Function)
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


### Turn 32 (Function)
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

### Turn 33 (Function)
[tool_response]
100→            if (_interruptSequenceRemaining > 0)
                return _interruptSequenceRemaining is 6 or 2 or 1;

            if (_cycle == 0)
                return _branchTargetFetchPending || _callTargetFetchPending;

            var nextCycle = _cycle - 1;
            return _opcode == 0x20 && nextCycle == 3;
        }
    }
110→
    /// <summary>
    /// Arms the IRQ dispatch sequence (TR-LOCKSTEP-VSF-001). Mirrors VICE's
    /// 7-cycle x64sc DO_INTERRUPT IRQ path (6510dtvcore.c:354-407 with
    /// DO_IRQBRK at :314-350): two dummy fetches at PC, PCH/PCL pushes, status
    /// push with B clear, then I is set and the $FFFE/$FFFF vector is read over
    /// two cycles; the JUMP becomes visible on the handler's first fetch cycle
    /// (the hosted per-cycle register export in c64cpusc.c CLK_INC). The system
    /// clock calls this at an instruction boundary, i.e. at the end of the tick
    /// that under this core's one-cycle-lag convention coincides with the
120→    /// native sequence's FIRST dummy cycle, so 6 explicit ticks remain (dummy,
    /// three pushes, two vector reads). A no-op when I is set or a sequence is
    /// already in flight. Tick() consumes the armed sequence one cycle at a
    /// time so BA steals can interleave exactly as on the single-cycle core.
    /// </summary>
    public void Irq()
    {
        if ((P & 0x04) != 0 || _interruptSequenceRemaining > 0)
            return;

130→        _interruptSequenceRemaining = 6;
        _interruptReturnPc = _pc;
        // The interrupted PC stays visible through the whole sequence (VICE
        // keeps exporting reg_pc until the JUMP after the vector fetch).
        _instructionPC = _interruptReturnPc;
        _visiblePC = _interruptReturnPc;
    }

    public void Nmi()
    {
140→        // NMI implementation - push PC and P to stack, clear I flag, jump to NMI vector
        PushWord(PC);
        Push((byte)(P & ~0x10)); // Push P with B flag clear
        P |= 0x04; // Set I flag (IRQs disabled during NMI)
        PC = Read(0xFFFA);
        PC |= (ushort)(Read(0xFFFB) << 8);
    }

    /// <summary>
    /// One cycle of the armed IRQ dispatch sequence (TR-LOCKSTEP-VSF-001),
150→    /// counting <see cref="_interruptSequenceRemaining"/> down 6..1. Micro-op
    /// order and visible register timing mirror VICE's x64sc DO_INTERRUPT +
    /// DO_IRQBRK (6510dtvcore.c:314-407) with the sequence's first dummy cycle
    /// absorbed by the arming boundary tick (this core's one-cycle-lag
    /// convention): cycle 6 dummy-reads the interrupted PC, cycles 5/4/3 push
    /// PCH/PCL/P (B clear; S decrements are visible on those cycles), cycle 2
    /// sets I and reads $FFFE, cycle 1 reads $FFFF and latches the new PC while
    /// the VISIBLE PC stays at the interrupted address until the handler's
    /// first fetch cycle, exactly like the hosted per-cycle register export
    /// (JUMP exported by the next CLK_INC in c64cpusc.c).
160→    /// </summary>
    private void ExecuteInterruptSequenceCycle()
    {
        switch (_interruptSequenceRemaining)
        {
            case 6:
                Read(_interruptReturnPc);
                break;
            case 5:
                // VICE DO_INTERRUPT (6510dtvcore.c): LOCAL_SET_BREAK(0) after the
170→                // dummy reads and before PUSH PCH (c=522269 nP=$21 mP=$31 when B
                // stayed set in managed P).
                P = (byte)(P & ~0x10);
                Push((byte)(_interruptReturnPc >> 8));
                break;
            case 4:
                Push((byte)_interruptReturnPc);
                break;
            case 3:
                Push((byte)(P & ~0x10));
180→                break;
            case 2:
                P |= 0x04;
                _interruptVector = Read(0xFFFE);
                break;
            case 1:
                _interruptVector |= (ushort)(Read(0xFFFF) << 8);
                _pc = _interruptVector;
                // The interrupted PC stays visible through this cycle (VICE
                // exports the JUMP only at the handler's first fetch cycle);
190→                // _delayNextFetch consumes that fetch cycle next tick, flipping
                // the visible PC to the handler and re-establishing the
                // one-cycle lag for the handler's first instruction.
                _instructionPC = _interruptReturnPc;
                _visiblePC = _interruptReturnPc;
                _suppressBootstrapBoundary = true;
                _delayNextFetch = true;
                break;
        }

200→        _interruptSequenceRemaining--;
    }

    private readonly IBus _bus;
    private IPubSub? _pubSub;

    public Func<ushort, bool>? ShouldDeferAbsoluteStore { get; set; }
    public Func<ushort, bool>? ShouldDelayNextFetchAfterWrite { get; set; }

    /// <summary>
210→    /// Optional KERNAL-trap hook (the VICE serial-trap equivalent). Invoked at
    /// each instruction boundary with the address about to be fetched. If it
    /// returns true the trapped instruction is skipped: the handler has already
    /// mutated registers/memory and set <see cref="PC"/> to the routine's resume
    /// address. Used to service virtual (non-true-drive) disk I/O without
    /// bit-banging the IEC bus. Null on true-drive and cycle-parity rigs.
    /// </summary>
    public Func<ushort, bool>? SerialTrapHook { get; set; }

    public Mos6502(IBus bus)

### Turn 34 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 13 matching lines
F:\GitHub\vice-sharp\docs\2s-irqrmw-2026-08-06.log
6:   DIV first=540740 matchedCycles=540739 budget=2216810
7:c=540729 nPC=$D89E mPC=$D89E nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$23 mP=$23 dbgCyc=3 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
8:c=540730 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=2 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
9:c=540731 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=1 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
10:c=540732 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=0 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
11:c=540733 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=5 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
12:c=540734 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=4 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
13:c=540735 nPC=$D8A2 mPC=$D8A2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F4 mS=$F4 nP=$21 mP=$21 dbgCyc=3 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
14:c=540736 nPC=$D8A2 mPC=$D8A2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=2 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
15:c=540737 nPC=$D8A2 mPC=$D8A2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=1 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
16:c=540738 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=0 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
17:c=540739 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=5 op=$76 trail=4 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
18:c=540740 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F2 mS=$F3 nP=$21 mP=$21 dbgCyc=4 op=$76 trail=4 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=True
</workspace_result>

### Turn 35 (Assistant)
[tool_request: grep]
- pattern: irq=|mis=|InterruptSequence|Irq
- path: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2260
- limit: 80
[tool_request: grep]
- pattern: GetCycleCount|0x76|ROR|shift.*Rmw|_suppressBootstrapBoundary = true
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\2s-irqrmw-2026-08-06.log
- offset: 1
- limit: 30

### Turn 36 (Function)
[tool_response]
2260→        _stagedCarryValue = A >= value;
        _stagedNzUpdate = true;
        _stagedNzValue = (byte)(A - value);
    }

    private void CompleteDeferredImmediateLoad()
    {
        var value = Read((ushort)(_instructionPC + 1));
        switch (_opcode)
        {
2270→            case 0xA0:
                Y = value;
                break;
            case 0xA2:
                X = value;
                break;
            case 0xA9:
                A = value;
                break;
        }
2280→
        UpdateNZ(value);
        _pc = (ushort)(_instructionPC + 2);
        _visiblePC = _pc;
        _pendingDeferredImmediateLoad = false;
        _softDeferredImmediateLoad = false;
        // After-branch deferred imm completes on the same host sample VICE uses
        // as the following opcode's first FETCH. Suppress IsInstructionBoundary
        // for the rest of this host step so SystemClock does not arm IRQ here
        // (c=522261 managed irq=6 while nPC continues into the next insn).
2290→        // Cleared at the start of the next Tick before fetch; IRQ then waits
        // until that insn ends, matching VICE. Do not fall through to fetch
        // (that regressed focused 500k at c=4977).
        _suppressBootstrapBoundary = true;
    }

    private void CompleteDeferredImpliedRegisterCompletion()
    {
        ExecuteOpcode(_opcode);
        _instructionPC = _pc;
2300→        _visiblePC = _pc;
        _pendingDeferredImpliedRegisterCompletion = false;
    }


    private bool TryDeferImpliedRegisterCompletionAfterBranch()
    {
        if (!_deferImpliedRegisterCompletionAfterBranch || _cycle != 0)
            return false;

2310→        _deferImpliedRegisterCompletionAfterBranch = false;
        _pendingDeferredImpliedRegisterCompletion = true;
        _visiblePC = _instructionPC;
        _suppressBootstrapBoundary = true;
        return true;
    }

    private void CompleteDeferredNzUpdateAfterBranch()
    {
        if (_stagedCarryUpdate)
2320→        {
            if (_stagedCarryValue)
                P |= 0x01;
            else
                P &= 0xFE;

            _stagedCarryUpdate = false;
        }

        if (_stagedNzUpdate)
2330→        {
            UpdateNZ(_stagedNzValue);
            _stagedNzUpdate = false;
        }

        _pendingDeferredNzUpdateAfterBranch = false;
    }

    private ushort ReadAbsoluteOperand()
    {

### Turn 37 (Function)
[tool_response]
1→Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.09]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=540740 matchedCycles=540739 budget=2216810
c=540729 nPC=$D89E mPC=$D89E nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$23 mP=$23 dbgCyc=3 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540730 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=2 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540731 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=1 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
10→c=540732 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=0 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540733 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=5 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540734 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=4 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540735 nPC=$D8A2 mPC=$D8A2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F4 mS=$F4 nP=$21 mP=$21 dbgCyc=3 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540736 nPC=$D8A2 mPC=$D8A2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=2 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540737 nPC=$D8A2 mPC=$D8A2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=1 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540738 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=0 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540739 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=5 op=$76 trail=4 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540740 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F2 mS=$F3 nP=$21 mP=$21 dbgCyc=4 op=$76 trail=4 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF
20→
  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)


### Turn 38 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 39 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
195:                _suppressBootstrapBoundary = true;
439:            _suppressBootstrapBoundary = true;
468:            _suppressBootstrapBoundary = true;
600:            _cycle = GetCycleCount(_opcode);
812:                    _suppressBootstrapBoundary = true;
828:                _suppressBootstrapBoundary = true;
844:                _suppressBootstrapBoundary = true;
856:                _suppressBootstrapBoundary = true;
870:                _suppressBootstrapBoundary = true;
895:                _suppressBootstrapBoundary = true;
905:                _suppressBootstrapBoundary = true;
916:                _suppressBootstrapBoundary = true;
928:                _suppressBootstrapBoundary = true;
964:        _suppressBootstrapBoundary = true;
995:                    _suppressBootstrapBoundary = true;
1073:        _suppressBootstrapBoundary = true;
1121:                _suppressBootstrapBoundary = true;
1131:                _suppressBootstrapBoundary = true;
1141:                _suppressBootstrapBoundary = true;
1153:                _suppressBootstrapBoundary = true;
1235:                _suppressBootstrapBoundary = true;
1252:            _suppressBootstrapBoundary = true;
1293:        // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
1551:            case 0x76:
1728:                    _suppressBootstrapBoundary = true;
1734:                _suppressBootstrapBoundary = true;
1823:            _suppressBootstrapBoundary = true;
1841:                _suppressBootstrapBoundary = true;
1845:                // Fall-through insn also has a clean first FETCH (ROR A at 195424).
1885:        _suppressBootstrapBoundary = true;
1930:        _cycle = Math.Max(0, GetCycleCount(_opcode) - 1);
2026:            0x6A or // ROR A
2150:        // ASL/ROL/LSR/ROR zp and zp,X (not accumulator forms).
2151:        return opcode is 0x06 or 0x16 or 0x26 or 0x36 or 0x46 or 0x56 or 0x66 or 0x76;
2156:        return opcode is 0x16 or 0x36 or 0x56 or 0x76;
2168:    /// VICE ASL/LSR/ROL/ROR + SET_ZERO(_X)_RMW: SET_C/NZ and INC_PC before the
2173:        var address = _opcode is 0x16 or 0x36 or 0x56 or 0x76
2293:        _suppressBootstrapBoundary = true;
2313:        _suppressBootstrapBoundary = true;
</workspace_result>

### Turn 39 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs
83:                $"irq={cpu.DebugInterruptSequenceRemaining} mis={mismatch}";
</workspace_result>

### Turn 40 (Assistant)
[tool_request: grep]
- pattern: GetCycleCount|case 0x76|zp,X.*6|0x76
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- glob: **/*
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1480
- limit: 120
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1680
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs
- offset: 50
- limit: 100

### Turn 41 (Function)
[tool_response]
50→    }

    /// <summary>
    /// Steps managed Vic20 and native xvic together; returns matched cycle count.
    /// Throws with a ring dump on first A/X/Y/S/P/PC mismatch.
    /// </summary>
    public static int RunEveryCycle(int budget)
    {
        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
60→        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var cpu = managed.Devices.GetAll<Mos6502>().First();

        var log = new StringBuilder();
        const int pre = 12;
        var ring = new string[pre];
        var ringAt = 0;
        var ringCount = 0;

70→        for (var i = 1; i <= budget; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            var mismatch = n.PC != m.PC || n.A != m.A || n.X != m.X || n.Y != m.Y || n.S != m.S || n.P != m.P;
            var line =
                $"c={i} nPC=${n.PC:X4} mPC=${m.PC:X4} nA=${n.A:X2} mA=${m.A:X2} nX=${n.X:X2} mX=${m.X:X2} " +
                $"nY=${n.Y:X2} mY=${m.Y:X2} nS=${n.S:X2} mS=${m.S:X2} nP=${n.P:X2} mP=${m.P:X2} " +
80→                $"dbgCyc={cpu.DebugCycle} op=${cpu.DebugOpcode:X2} trail={cpu.DebugPriorTrailingAtNextPc} " +
                $"nonOvlR={cpu.DebugNonOverlappedRegion} nonOvlF={cpu.DebugNonOverlappedFetchPhase} " +
                $"dly={cpu.DebugDelayNextFetch} stg={cpu.DebugStagedMemoryReadCompleted} " +
                $"irq={cpu.DebugInterruptSequenceRemaining} mis={mismatch}";

            ring[ringAt] = line;
            ringAt = (ringAt + 1) % pre;
            if (ringCount < pre)
                ringCount++;

90→            if (!mismatch)
                continue;

            var start = (ringAt - ringCount + pre) % pre;
            for (var k = 0; k < ringCount; k++)
                log.AppendLine(ring[(start + k) % pre]);

            // Extra bus evidence for load/store diverges.
            var op = cpu.DebugOpcode;
            var opAddr = cpu.DebugOpcodeAddress;
100→            ushort absAddr = 0;
            if (op is 0xAD or 0xAE or 0xAC or 0x8D or 0x8E or 0x8C or 0xEE or 0xCE
                or 0xBD or 0xB9 or 0xBC or 0xBE)
            {
                var lo = managed.Bus.Peek((ushort)(opAddr + 1));
                var hi = managed.Bus.Peek((ushort)(opAddr + 2));
                absAddr = (ushort)(lo | (hi << 8));
                if (op is 0xBD or 0xBC)
                    absAddr = (ushort)(absAddr + m.X);
                else if (op is 0xB9 or 0xBE)
110→                    absAddr = (ushort)(absAddr + m.Y);
            }

            log.AppendLine(
                $"bus: m$C1=${managed.Bus.Peek(0xC1):X2} m$C2=${managed.Bus.Peek(0xC2):X2} " +
                $"n$C1=${native.PeekBus(0xC1):X2} n$C2=${native.PeekBus(0xC2):X2} " +
                $"m$C3=${managed.Bus.Peek(0xC3):X2} m$C4=${managed.Bus.Peek(0xC4):X2} " +
                $"n$C3=${native.PeekBus(0xC3):X2} n$C4=${native.PeekBus(0xC4):X2} " +
                $"m$0314=${managed.Bus.Peek(0x0314):X2} n$0314=${native.PeekBus(0x0314):X2}" +
                (absAddr != 0
120→                    ? $" opAddr=${opAddr:X4} abs=${absAddr:X4} m[${absAddr:X4}]=${managed.Bus.Peek(absAddr):X2} n[${absAddr:X4}]=${native.PeekBus(absAddr):X2}"
                    : string.Empty));

            throw new Xunit.Sdk.XunitException(
                $"DIV first={i} matchedCycles={i - 1} budget={budget}\n{log}");
        }

        return budget;
    }
}
130→

### Turn 42 (Function)
[tool_response]
1480→                return true;
            }
            case 0xAE:
            {
                var xAbs = Read(ReadAbsoluteOperand());
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = xAbs;
                FinishStagedMemoryRead(3, xAbs);
                return true;
            }
1490→            case 0xAC:
            {
                var yAbs = Read(ReadAbsoluteOperand());
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    Y = yAbs;
                FinishStagedMemoryRead(3, yAbs);
                return true;
            }
            case 0x8D:
                _bus.Write(ReadAbsoluteOperand(), A);
1500→                FinishStagedMemoryWrite(3);
                return true;
            case 0x8E:
                var stxAddress = ReadAbsoluteOperand();
                if (ShouldDeferAbsoluteStore?.Invoke(stxAddress) == true)
                {
                    return false;
                }

                _bus.Write(stxAddress, X);
1510→                FinishStagedMemoryWrite(3);
                return true;
            case 0x8C:
                _bus.Write(ReadAbsoluteOperand(), Y);
                FinishStagedMemoryWrite(3);
                return true;
            case 0x9D:
                _bus.Write((ushort)(ReadAbsoluteOperand() + X), A);
                FinishStagedMemoryWrite(3);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
1520→                return true;
            case 0x99:
                _bus.Write((ushort)(ReadAbsoluteOperand() + Y), A);
                FinishStagedMemoryWrite(3);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0xC6:
            case 0xE6:
                // Final STORE CLK after NZ+PC already match VICE INC/DEC macro.
                if (_zpRmwModifyCommitted)
1530→                {
                    _bus.Write(_effectiveAddress, _fetched);
                    _zpRmwModifyCommitted = false;
                    FinishStagedMemoryWrite(2);
                    return true;
                }

                // Fallback (e.g. after-branch defer returned false at cycle 2):
                // full RMW + NZ on this CLK so host still pairs flags with PC.
                if (_opcode == 0xE6)
1540→                    IncrementStagedMemory(ReadZeroPageOperand(), 2);
                else
                    DecrementStagedMemory(ReadZeroPageOperand(), 2);
                return true;
            case 0x06:
            case 0x16:
            case 0x26:
            case 0x36:
            case 0x46:
            case 0x56:
1550→            case 0x66:
            case 0x76:
                // Shift RMW already committed at cycle 2 (zp) or 1 (zp,X).
                // Keep flag set until cycle 0 so ExecuteOpcode is skipped
                // (clearing here caused a second Advance/PC bump: c=539964 mPC=$E8C6).
                if (_zpRmwModifyCommitted)
                    return true;

                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
1560→                return true;
            case 0xBD:
                var absoluteXBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(absoluteXBase, X))
                {
                    return true;
                }

                var absoluteXValue = Read((ushort)(absoluteXBase + X));
                // Non-overlapped: pre-load A on data-read CLK (c=519271 nA=$FF mA=$E4).
1570→                if (!_deferAbsoluteXLoadCompletionAfterBranch
                    && !(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                {
                    A = absoluteXValue;
                }

                FinishStagedMemoryRead(3, absoluteXValue);
                return true;
            case 0xB9:
                var absoluteYBase = ReadAbsoluteOperand();
1580→                if (TryDelayIndexedLoadPageCross(absoluteYBase, Y))
                {
                    return true;
                }

                var absoluteYValue = Read((ushort)(absoluteYBase + Y));
                if (!_deferAbsoluteYLoadCompletionAfterBranch
                    && !(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                {
                    A = absoluteYValue;
1590→                }

                FinishStagedMemoryRead(3, absoluteYValue);
                return true;
            case 0xBC:
                var ldyBase = ReadAbsoluteOperand();
                if (TryDelayIndexedLoadPageCross(ldyBase, X))
                {
                    return true;
                }

### Turn 43 (Function)
[tool_response]
1680→        if (_opcode != 0x60)
        {
            return false;
        }

        // VICE 6510dtvcore.c RTS: STACK_PEEK, PULL, PULL, LOAD, JUMP after the
        // FETCH_OPCODE CLKs. Default pulls at cycle 2/1 match clean-fetch paths
        // (early-boot c=30, non-overlapped c=5048: first PULL not visible at
        // dbgCyc=3). Overlapped first FETCH (priorTrailing==1 after LDY # etc.)
        // means VICE already consumed one FETCH on the previous host sample, so
1690→        // first PULL is visible at our dbgCyc=3 (c=518540 nS ahead of mS).
        switch (_cycle)
        {
            case 3 when _rtsOverlappedFirstFetch:
                _stagedReturnAddress = Pop();
                return true;
            case 2:
                if (_rtsOverlappedFirstFetch)
                {
                    _stagedReturnAddress |= (ushort)(Pop() << 8);
1700→                    return true;
                }

                _stagedReturnAddress = Pop();
                return true;
            case 1:
                if (_rtsOverlappedFirstFetch)
                {
                    _ = Read(_stagedReturnAddress);
                    return true;
1710→                }

                _stagedReturnAddress |= (ushort)(Pop() << 8);
                return true;
            case 0:
                _pc = (ushort)(_stagedReturnAddress + 1);
                // Leaving the non-overlapped callee region on RTS.
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
                if (_rtsOverlappedFirstFetch)
1720→                {
                    // Overlapped first-FETCH path is one host sample ahead of the
                    // clean RTS schedule: VICE JUMP is already visible on this
                    // CLK (c=518543 nPC=return mPC=RTS with delayNextFetch).
                    // Export return PC now and fetch the caller next tick.
                    _rtsOverlappedFirstFetch = false;
                    _visiblePC = _pc;
                    _instructionPC = _pc;
                    _suppressBootstrapBoundary = true;
                    return true;
1730→                }

                _rtsOverlappedFirstFetch = false;
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                if (Peek(_pc) == 0x60)
                {
                    // Same RTS prefetch convention as FinishStagedMemoryWrite:
                    // a following RTS expects an un-lagged entry, so skip the
                    // delayed-fetch tick and fetch it on the next cycle.
1740→                    return true;
                }

                _delayNextFetch = true;
                return true;
            default:
                return false;
        }
    }

1750→    /// <summary>
    /// Cycle-staged RTI (0x40; TR-LOCKSTEP-VSF-001), mirroring VICE's 6-cycle
    /// sequence (6510dtvcore.c RTI: fetch, dummy, stack peek, pull P, pull PCL,
    /// pull PCH): each pull cycle exports the incremented S; the pulled status
    /// becomes visible one cycle after its pull (assignment happens after that
    /// cycle's CLK_INC), and the return-address JUMP becomes visible on the
    /// final tick, exactly like the hosted per-cycle register export.
    /// </summary>
    private bool TryExecuteCycleStagedRtiOpcode()
    {
1760→        if (_opcode != 0x40)
        {
            return false;
        }

        switch (_cycle)
        {
            case 3:
                if (_rtiDeferFirstPullOne)
                {
1770→                    // Extra STACK_PEEK after soft body (c=522485).
                    _rtiDeferFirstPullOne = false;
                    _cycle = 4;
                    return true;
                }

                _fetched = Pop();
                return true;
            case 2:
                P = (byte)((_fetched & ~0x10) | (P & 0x10));

### Turn 44 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 9 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
600:            _cycle = GetCycleCount(_opcode);
1295:        // zp,X (6-cycle): cycle 1 after taken branch (c=539721 ASL after BCC);
1551:            case 0x76:
1930:        _cycle = Math.Max(0, GetCycleCount(_opcode) - 1);
2151:        return opcode is 0x06 or 0x16 or 0x26 or 0x36 or 0x46 or 0x56 or 0x66 or 0x76;
2156:        return opcode is 0x16 or 0x36 or 0x56 or 0x76;
2173:        var address = _opcode is 0x16 or 0x36 or 0x56 or 0x76
2648:            0x66 or 0x76 or 0x6E or 0x7E or
2688:    private partial int GetCycleCount(byte opcode);
</workspace_result>

### Turn 45 (Assistant)
[tool_request: grep]
- pattern: fetchingCallTarget
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 740
- limit: 200
[tool_request: grep]
- pattern: interrupt_check_irq_delay|INTERRUPT_DELAY|irq_clk
- path: F:\GitHub\vice-sharp\native\vice\vice\src
- glob: **/interrupt*.c
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1270
- limit: 80

### Turn 46 (Function)
[tool_response]
740→            if (armAfterBranchLag && IsAfterBranchBudgetExtendedOpcode(_opcode))
            {
                _cycle++;
            }

            // Stage fall-through at cycle 1 when:
            //  - budget 4+ (full-length + after-branch lag): c=540201
            //  - full-length with priorTrailing>=3 (c=540231)
            //  - full-length trail=2 after store (c=540337 BCC after STA zp)
            // Not: full-length trail=2 after INC (c=5005 keeps opcode at cyc 1)
750→            // Not: trail=0+nonOvl (c=5034) or lag alone (c=4997)
            _takenBranchStagedFallthrough = IsBranchOpcode(_opcode)
                && IsBranchTaken(_opcode)
                && fullLengthTakenBranch
                && (_cycle >= 4
                    || priorTrailingAtNextPc >= 3
                    || (priorTrailingAtNextPc == 2 && IsStoreOpcode(_previousOpcode)));

            _pendingDeferredImmediateLoad = false;
            _indexedLoadPageCrossDelayConsumed = false;
760→            _stagedReturnAddress = 0;
            _effectiveAddress = 0;
            _fetched = 0;

        }

        _cycle--;

        if (TryExecuteCycleStagedOpcode())
        {
770→            return;
        }

        if (TryDeferImpliedRegisterCompletionAfterBranch())
        {
            return;
        }

        if (_cycle == 0)
        {
780→            // VICE 2-byte immediates (LD#/CMP#/AND#/...): GET_IMM + body + INC_PC
            // run after the second FETCH CLK is exported. Soft-defer the body so
            // host still samples pre-op regs/PC on that CLK when first-FETCH is
            // not overlapped (c=518756 CMP# in non-overlapped region after
            // not-taken BCC hold).
            // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#); skip soft for non-load imm once.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
790→                if (_skipSoftImmAfterStagedTakenBranch && !IsImmediateLoadOpcode(_opcode))
                {
                    _skipSoftImmAfterStagedTakenBranch = false;
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
800→                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

810→                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }

            // VICE abs ALU (ORA/AND/EOR/ADC/SBC $nnnn): GET_ABS CLK exports
            // pre-op A/PC; body + INC_PC after that sample (c=519285 nA=$03
820→            // mA=$1F nPC=opcode mPC=next). Soft-defer when non-overlapped.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsAbsoluteAluOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
830→                return;
            }

            // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
            // FETCH is not overlapped so host samples the pre-op P/regs.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
840→                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }

            // NOP after short taken branch: VICE still exports opcode PC on the
850→            // final FETCH CLK (INC_PC is after that sample). Suppress boundary
            // so GetState returns visible opcode PC, not the post-fetch next PC.
            if (_nopHoldOpcodePcOnFinal)
            {
                _nopHoldOpcodePcOnFinal = false;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

860→            // After post-branch NOP chain: soft-defer 2-byte immediate body
            // (CMP#/AND#/etc.) so final FETCH sample keeps opcode PC; commit at
            // the next instruction's first tick (VICE INC_PC after last FETCH).
            if (_softDeferAfterNopChain && IsTwoByteImmediateOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _softDeferAfterNopChain = false;
                _visiblePC = _opcodeAddress;
870→                _suppressBootstrapBoundary = true;
                return;
            }

            // BIT abs/zp soft-defer when clean first FETCH, after branch, or
            // priorTrailing>=2. After LDA# trail=1 VICE fuses BIT (c=540355);
            // after taken BNE trail=1 still softs (c=522392 nPC=opcode).
            // _bitSoftDeferEarly handles RTS-delay cycle-1 soft.
            if ((_opcode is 0x2C or 0x24) && !_bitSoftDeferEarly
                && (_nonOverlappedFetchPhase || _nonOverlappedRegion
880→                    || DebugPriorTrailingAtNextPc >= 2
                    || IsBranchOpcode(_previousOpcode)))
            {
                SoftDeferBitBody();
                return;
            }

            // CMP zp/zx after taken-branch clean FETCH: soft-defer Compare body.
            if (_softDeferZpCompare)
            {
890→                _softDeferZpCompare = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            // zp shift RMW already committed mid-instruction. Suppress boundary
900→            // so SystemClock does not sample IRQ one CLK early vs VICE after the
            // RMW final sample (c=540734 nS=$F5 mS=$F4 on first IRQ push).
            if (_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
            {
                _zpRmwModifyCommitted = false;
                _suppressBootstrapBoundary = true;
                return;
            }

            // JMP abs: soft-hold opcode PC; JUMP applies on the next fetch tick.
910→            if (_softDeferJmpAbs && _opcode == 0x4C)
            {
                _softDeferJmpAbs = false;
                _pendingJmpTarget = ReadAbsoluteOperand();
                _pc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

920→            // PLA after soft BIT: late PULL on this CLK (A/S update, PC still
            // opcode). VICE then LOCAL_SET_NZ + INC_PC after the sample.
            if (_plaLatePullPending)
            {
                _plaLatePullPending = false;
                A = Pop();
                _pc = (ushort)(_instructionPC + 1);
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                // NZ after this host sample (VICE LOCAL_SET_NZ post CLK_INC).
930→                _stagedNzUpdate = true;
                _stagedNzValue = A;
                _pendingSoftCompareCommit = true;
                // Following insn (TAY at c=522472) has a clean first FETCH: soft-defer
                // implied body so final CLK keeps pre-op Y/PC (VICE TAY + INC_PC).
                _nonOverlappedFetchPhase = true;
                // RTI after this PLA chain needs the same extra STACK_PEEK
                // (c=522485; flag is cleared if the next fetch is not RTI).
                _rtiDeferFirstPullOne = true;
                return;

### Turn 47 (Function)
[tool_response]
1270→                _visiblePC = _opcodeAddress;
                return true;
            }

            // VICE INC/DEC zp (6510core.c): after load + dummy RMW write,
            // LOCAL_SET_NZ and INC_PC become visible before the final STORE CLK.
            // Absolute RMW already matches this; zp was advancing PC only and
            // lagging NZ until cycle 1 (diverge at c=517829: nP=$21 mP=$23).
            CommitZeroPageRmwModifyAndFlags();
            AdvanceVisiblePc(2);
1280→            return true;
        }

        if (_cycle == 1 && IsZeroPageIncrementDecrementOpcode(_opcode) && _visiblePC == _opcodeAddress
            && !_stagedMemoryReadCompleted)
        {
            // Complete deferred Advance after the hold tick. VICE still pairs
            // LOCAL_SET_NZ with INC_PC on this same host sample.
            CommitZeroPageRmwModifyAndFlags();
            AdvanceVisiblePc(2);
1290→            // do not return - allow cycle==1 final-store switch to run
        }

        // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 after taken branch (c=539721 ASL after BCC);
        // cycle 2 after load (c=540730 LSR after LDA zp nPC advanced at dbgCyc=2).
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
1300→            var commitAtCycle2 = !zpX
                || IsLoadOpcode(_previousOpcode)
                || DebugPriorTrailingAtNextPc == 0;
            var commitAtCycle1 = zpX && !commitAtCycle2;
            if ((_cycle == 2 && commitAtCycle2) || (_cycle == 1 && commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
1310→        }

        if (_cycle == 4 && IsIndirectYStoreOpcode(_opcode))
        {
            // Non-overlapped path after soft LDA #: hold opcode PC one more CLK
            // so host-visible matches VICE (2 FETCH at start before INC_PC).
            if (_holdIndYStorePcOneCycle)
            {
                _holdIndYStorePcOneCycle = false;
                _softDeferCompareCommit = true;
1320→                _visiblePC = _opcodeAddress;
                return true;
            }

            AdvanceVisiblePc(2);
            return true;
        }

        if (_cycle == 3 && IsIndirectYStoreOpcode(_opcode) && _visiblePC == _opcodeAddress)
        {
1330→            // Complete the deferred advance after the hold tick.
            AdvanceVisiblePc(2);
            return true;
        }

        if (IsStagedAbsoluteRmwOpcode(_opcode) && TryExecuteStagedAbsoluteRmwCycle())
        {
            return true;
        }

1340→        if (_cycle == 2 && IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            if (_deferIndexedStorePcAdvanceAfterBranch)
            {
                _deferIndexedStorePcAdvanceAfterBranch = false;
                _indexedStorePcAdvanceWasDeferred = true;
                return false;
            }

            // Non-overlapped clean first FETCH: VICE ST does INC_PC only after

### Turn 48 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 2 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
416:        var fetchingCallTarget = false;
420:            fetchingCallTarget = _callTargetFetchPending;
</workspace_result>

### Turn 49 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 6 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\interrupt.c
141:        cs->irq_clk += warp_amount;
148:        if (cs->irq_clk > warp_amount) {
149:            cs->irq_clk -= warp_amount;
151:            cs->irq_clk = (CLOCK) 0;
388:    if (SMW_CLOCK(m, cs->irq_clk) < 0
435:        || SMR_CLOCK(m, &cs->irq_clk) < 0
</workspace_result>

### Turn 50 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 85
- limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1910
- limit: 40
[tool_request: grep]
- pattern: DebugIsWaiting|IsWaiting|callTarget|branchTarget
- path: F:\GitHub\vice-sharp\src
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\irqrmw-focused-2026-08-06.log
- limit: 30
[tool_request: grep]
- pattern: suppress.*JSR|CompleteJsr|after JSR|IRQ.*JSR|JSR.*IRQ|540740|540734
- path: F:\GitHub\vice-sharp
- glob: **/*.{cs,md,txt,log}
- head_limit: 40

### Turn 51 (Function)
[tool_response]
85→    public bool CanForceStealCurrentCycle
    {
        get
        {
            if (_pendingDeferredNzUpdateAfterBranch ||
90→                _bootstrapCycles > 0 ||
                _pendingDeferredImmediateLoad ||
                _pendingDeferredImpliedRegisterCompletion ||
                _branchPageCrossExtraPending)
            {
                return false;
            }

            // TR-LOCKSTEP-VSF-001: same interrupt-sequence BA semantics as
            // CanStealCurrentCycle (reads stall, stack pushes proceed).
100→            if (_interruptSequenceRemaining > 0)
                return _interruptSequenceRemaining is 6 or 2 or 1;

            if (_cycle == 0)
                return _branchTargetFetchPending || _callTargetFetchPending;

            var nextCycle = _cycle - 1;
            return _opcode == 0x20 && nextCycle == 3;
        }
    }
110→
    /// <summary>
    /// Arms the IRQ dispatch sequence (TR-LOCKSTEP-VSF-001). Mirrors VICE's
    /// 7-cycle x64sc DO_INTERRUPT IRQ path (6510dtvcore.c:354-407 with
    /// DO_IRQBRK at :314-350): two dummy fetches at PC, PCH/PCL pushes, status

### Turn 52 (Function)
[tool_response]
1→  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
10→  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
20→  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)


### Turn 53 (Function)
[tool_response]
1910→        return opcode switch
        {
            0x10 => (P & 0x80) == 0,
            0x30 => (P & 0x80) != 0,
            0x50 => (P & 0x40) == 0,
            0x70 => (P & 0x40) != 0,
            0x90 => (P & 0x01) == 0,
            0xB0 => (P & 0x01) != 0,
            0xD0 => (P & 0x02) == 0,
            0xF0 => (P & 0x02) != 0,
1920→            _ => false
        };
    }

    private void PrefetchOpcodeAt(ushort address)
    {
        _instructionPC = address;
        _visiblePC = address;
        _pc = address;
        _opcode = Read(_pc++);
1930→        _cycle = Math.Max(0, GetCycleCount(_opcode) - 1);
        _stagedMemoryReadCompleted = false;
        _delayNextFetch = false;
        _stagedNzUpdate = false;
        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
        _stagedCarryValue = false;
        _callTargetFetchPending = false;
        _deferImmediateLoadAfterBranch = false;
        _deferImpliedRegisterCompletionAfterBranch = false;
1940→        _deferAbsoluteXLoadCompletionAfterBranch = false;
        _deferAbsoluteYLoadCompletionAfterBranch = false;
        _deferJsrPushAfterBranch = false;
        _deferIndirectYLoadCompletionAfterBranch = false;
        _deferZeroPageRmwPcAdvanceAfterBranch = false;
        _deferNextIndirectYLoadAfterBranchRmw = false;
        _deferIndexedStorePcAdvanceAfterBranch = false;
        _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
        _zpRmwModifyCommitted = false;
        _rtsOverlappedFirstFetch = false;

### Turn 54 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 13 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
104:                return _branchTargetFetchPending || _callTargetFetchPending;
241:    private bool _branchTargetFetchPending;
273:    private bool _callTargetFetchPending;
419:            fetchingBranchTarget = _branchTargetFetchPending;
420:            fetchingCallTarget = _callTargetFetchPending;
421:            _branchTargetFetchPending = false;
422:            _callTargetFetchPending = false;
438:            _branchTargetFetchPending = true;
1072:        _callTargetFetchPending = true;
1897:            _branchTargetFetchPending = true;
1937:        _callTargetFetchPending = false;
2568:        _branchTargetFetchPending = false;
2579:        _callTargetFetchPending = false;
</workspace_result>

### Turn 55 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 13 matching lines
F:\GitHub\vice-sharp\docs\2s-irqrmw-2026-08-06.log
6:   DIV first=540740 matchedCycles=540739 budget=2216810
12:c=540734 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=4 op=$20 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
18:c=540740 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F2 mS=$F3 nP=$21 mP=$21 dbgCyc=4 op=$76 trail=4 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=True

F:\GitHub\vice-sharp\docs\2s-lsrx-2026-08-06.log
6:   DIV first=540734 matchedCycles=540733 budget=2216810
18:c=540734 nPC=$D8A0 mPC=$D8A0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F5 mS=$F4 nP=$21 mP=$21 dbgCyc=0 op=$56 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=4 mis=True

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
697:            // CMP zp family after taken-branch lag only (not after JSR trail>=2;
901:            // RMW final sample (c=540734 nS=$F5 mS=$F4 on first IRQ push).
1034:                CompleteJsrTargetFetch();
1041:    private void CompleteJsrTargetFetch()

F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
18:  matchedCycles=540739  DIV first=540740  budget=2216810
42:  c=540740 ROR zp,X $76 at $D9B0 after JSR (trail=4)
50:  (NMI/IRQ not shown as irq>0) or JSR leftover phase vs xvic.
53:  Probe native IRQ/NMI lines and stack activity around c=540740; realign
</workspace_result>

### Turn 56 (Assistant)
[tool_request: grep]
- pattern: GetState|struct.*State|Irq|Nmi
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
- head_limit: 40
[tool_request: grep]
- pattern: GetState|CpuState|struct
- path: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions
- glob: **/*
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1038
- limit: 40
[tool_request: todo_write]
- todos: [{"id":"2","content":"2: Diagnose native S drop mid-ROR at c=540740 vs VICE","status":"completed"},{"id":"3","content":"3: Fix JSR-end IRQ sample (no suppress); re-run focused+2s; update receipt","status":"in_progress"}]
- merge: true

### Turn 57 (Function)
[tool_response]
- [completed] 1: Verify skeptic: NotTaken Explicit + residual honesty on disk
- [completed] 2: 2: Diagnose native S drop mid-ROR at c=540740 vs VICE
- [in_progress] 3: 3: Fix JSR-end IRQ sample (no suppress); re-run focused+2s; update receipt


### Turn 58 (Function)
[tool_response]
1038→        }
    }
1040→
    private void CompleteJsrTargetFetch()
    {
        var source = _instructionPC;
        var lo = Read((ushort)(_instructionPC + 1));
        // _pc is already instructionPC+2 after the push phase (VICE loads MSB here).
        var hi = Read(_pc);
        var target = (ushort)(lo | (hi << 8));
        var returnPc = (ushort)(source + 3);
        var nonOverlappedJsr = _afterFullLengthTakenBranch
1050→            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase;
        if (nonOverlappedJsr)
        {
            // VICE JUMP runs after the final CLK_INC export, so this cycle still
            // shows the post-INC_PC address; target is live for the next FETCH.
            var exportPc = _pc;
            _pc = target;
            _instructionPC = exportPc;
            _visiblePC = exportPc;
1060→            // Callee starts without first-FETCH overlap.
            _nonOverlappedFetchPhase = true;
        }
        else
        {
            // Overlapped path: host-visible last CLK already matches target
            // (phase coupling with previous instruction).
            PC = target;
        }

1070→        PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        _suppressBootstrapBoundary = true;
    }

    private void PublishControlTransfer(ushort source, ushort target, ushort returnPc, byte opcode)
    {

### Turn 59 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 23 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
220:    public static partial void SidExactGetState(IntPtr instance, ref ViceSidExactState state);
309:    public struct ViceVicState
356:    public struct ViceCiaState
378:        public byte IrqMask;
385:    public struct ViceSidState
424:    public struct ViceSidExactState
517:    public struct ViceInterruptState
519:        public byte IrqAsserted;
520:        public byte NmiAsserted;
522:        public byte IrqSourceCount;
523:        public byte NmiSourceCount;
537:    public struct ViceCpuPipelineState
547:        public ulong IrqClk;
548:        public ulong NmiClk;
549:        public ulong IrqDelayCycles;
550:        public ulong NmiDelayCycles;
708:            // Re-baseline so GetState().Cycle counts forward from the loaded
742:        public MachineState GetState()
793:                IrqMask = state.IrqMask
812:                IrqClk = state.IrqClk,
813:                NmiClk = state.NmiClk,
814:                IrqDelayCycles = state.IrqDelayCycles,
815:                NmiDelayCycles = state.NmiDelayCycles
</workspace_result>

### Turn 60 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 21 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\CpuInstructionCompletedEvent.cs
4:/// Published once per completed CPU instruction (at the instruction boundary, after
6:/// instruction's fetch address, its opcode, and the post-execution register file
10:/// so an unobserved run pays only a null/count check per instruction.
12:public readonly record struct CpuInstructionCompletedEvent(
13:    ushort InstructionAddress,
22:    /// <summary>Pub/Sub topic used for completed-instruction notifications.</summary>
23:    public static readonly Topic Topic = Topic.FromName("cpu.instruction-completed");

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\CpuControlTransferEvent.cs
6:public readonly record struct CpuControlTransferEvent(

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IArchitectureDescriptor.cs
6:/// Used by IArchitectureBuilder to construct a running IMachine.
29:public readonly record struct DeviceDescriptor(

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IAudioRecorder.cs
8:/// container-specific header on construction, append samples on

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IArchitectureBuilder.cs
4:/// Constructs a running IMachine from an IArchitectureDescriptor.

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ICpu.cs
29:    /// True when the CPU is positioned at the boundary before fetching the next instruction.
31:    bool IsInstructionBoundary => true;
34:    /// Execute single instruction at current PC.
37:    int ExecuteInstruction();

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDeviceRegistry.cs
5:/// interface type, or device role. Populated during machine construction.

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IDevice.cs
28:public readonly struct DeviceId : IEquatable<DeviceId>

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IInterSystemBus.cs
66:public readonly record struct BusLineSnapshot(string Signal, bool IsHigh, IReadOnlyList<string> Pullers)

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IMachine.cs
11:public readonly record struct CpuInfo(string Label, long ExecutedCycles, long ClockHz);
18:public readonly record struct CpuRateReading(string Label, double EffectiveClockHz, double EffectiveClockPercent);
</workspace_result>

### Turn 61 (Assistant)
Diagnosis is solid: VICE samples IRQ after JSR JUMP and before the callee FETCH; `CompleteJsrTargetFetch` sets `_suppressBootstrapBoundary`, which closes that window so managed runs ROR while native starts DO_INTERRUPT (S drop with PC frozen at `$D9B0`).
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        _suppressBootstrapBoundary = true;
    }
- new_string:         PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        // VICE 6510core.c main loop: after JSR JUMP, the next iteration runs
        // DO_INTERRUPT before FETCH of the callee. Leaving
        // _suppressBootstrapBoundary set here closed IsInstructionBoundary for
        // that post-JSR sample, so SystemClock never armed IRQ while native
        // entered DO_INTERRUPT with reg_pc frozen at the callee (c=540740
        // nS=$F2 mS=$F3, both PC=$D9B0 op=$76: managed ran ROR, native pushed).
        // Keep the boundary open so an asserted IRQ line can arm on this tick,
        // matching VICE. callTargetFetchPending is only for BA force-steal on
        // the one-tick handoff; clear it when suppress would have been consumed.
        _callTargetFetchPending = true;
        // Do not set _suppressBootstrapBoundary.
    }

### Turn 62 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 63 (Assistant)
Clearing `callTargetFetchPending` so it cannot stick open when suppress is no longer set.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        // VICE 6510core.c main loop: after JSR JUMP, the next iteration runs
        // DO_INTERRUPT before FETCH of the callee. Leaving
        // _suppressBootstrapBoundary set here closed IsInstructionBoundary for
        // that post-JSR sample, so SystemClock never armed IRQ while native
        // entered DO_INTERRUPT with reg_pc frozen at the callee (c=540740
        // nS=$F2 mS=$F3, both PC=$D9B0 op=$76: managed ran ROR, native pushed).
        // Keep the boundary open so an asserted IRQ line can arm on this tick,
        // matching VICE. callTargetFetchPending is only for BA force-steal on
        // the one-tick handoff; clear it when suppress would have been consumed.
        _callTargetFetchPending = true;
        // Do not set _suppressBootstrapBoundary.
    }
- new_string:         PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        // VICE 6510core.c main loop: after JSR JUMP, the next iteration runs
        // DO_INTERRUPT before FETCH of the callee. Leaving
        // _suppressBootstrapBoundary set here closed IsInstructionBoundary for
        // that post-JSR sample, so SystemClock never armed IRQ while native
        // entered DO_INTERRUPT with reg_pc frozen at the callee (c=540740
        // nS=$F2 mS=$F3, both PC=$D9B0 op=$76: managed ran ROR, native pushed).
        // Keep the boundary open so an asserted IRQ line can arm on this tick,
        // matching VICE. Do not set callTargetFetchPending either: that flag
        // was only cleared when suppress was consumed, and would stick open.
    }

### Turn 64 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 65 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" --no-restore 2>&1 | Tee-Object -FilePath docs\focused-jsr-irq-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Run focused lockstep gates after JSR IRQ fix
- timeout: 300000

### Turn 66 (Function)
[tool_response]
exit: 0
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:03.17]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow [25 ms]
  Error Message:
   DIV first=5012 matchedCycles=5011 budget=500000
c=5001 nPC=$FDAF mPC=$FDAF nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$26 mP=$26 dbgCyc=2 op=$E6 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=5002 nPC=$FDB1 mPC=$FDB1 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=1 op=$E6 trail=1 nonOvlR=False nonOvlF=False dly=False stg=True irq=0 mis=False
c=5003 nPC=$FDB1 mPC=$FDB1 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=0 op=$E6 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=5004 nPC=$FDB1 mPC=$FDB1 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=2 op=$D0 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=5005 nPC=$FDB1 mPC=$FDB1 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=1 op=$D0 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=5006 nPC=$FDB3 mPC=$FDB3 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=0 op=$D0 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=5007 nPC=$FDB5 mPC=$FDB5 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=5 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=5008 nPC=$FDB5 mPC=$FDB5 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=4 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=5009 nPC=$FDB5 mPC=$FDB5 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FD mS=$FD nP=$24 mP=$24 dbgCyc=3 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=5010 nPC=$FDB7 mPC=$FDB7 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FC mS=$FC nP=$24 mP=$24 dbgCyc=2 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=5011 nPC=$FDB7 mPC=$FDB7 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FB mS=$FB nP=$24 mP=$24 dbgCyc=1 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=5012 nPC=$FDB7 mPC=$FE91 nA=$00 mA=$00 nX=$04 mX=$04 nY=$00 mY=$00 nS=$FB mS=$FB nP=$24 mP=$24 dbgCyc=0 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$02 m$C2=$04 n$C1=$02 n$C2=$04 m$C3=$00 m$C4=$00 n$C3=$00 n$C4=$00 m$0314=$00 n$0314=$00

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 30
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:    19, Skipped:     0, Total:    20, Duration: 1 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=1


### Turn 67 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1
- limit: 50
[tool_request: grep]
- pattern: public ushort PC|set =>|_visiblePC
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 30
[tool_request: grep]
- pattern: IsInstructionBoundary
- path: F:\GitHub\vice-sharp\src
- glob: **/*.cs

### Turn 68 (Function)
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
    /// <summary>Trailing next-PC dwell captured at last instruction fetch (diagnostics).</summary>
    public int DebugPriorTrailingAtNextPc { get; private set; }
    public bool DebugFullLengthTakenBranch { get; private set; }
    public bool DebugNonOverlappedRegion => _nonOverlappedRegion;
    public bool DebugNonOverlappedFetchPhase => _nonOverlappedFetchPhase;
    public bool DebugStagedMemoryReadCompleted => _stagedMemoryReadCompleted;
    public int DebugInterruptSequenceRemaining => _interruptSequenceRemaining;
50→    public ushort DebugOpcodeAddress => _opcodeAddress;

### Turn 69 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 29 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
21:    private ushort _visiblePC;
22:    public ushort PC
24:        get => IsInstructionBoundary ? _pc : _visiblePC;
29:            _visiblePC = value;
32:    public byte Flags { get => P; set => P = value; }
135:        _visiblePC = _interruptReturnPc;
194:                _visiblePC = _interruptReturnPc;
405:            if (_visiblePC != _opcodeAddress)
457:            _visiblePC = _pc;
567:            _visiblePC = _instructionPC;
811:                    _visiblePC = _opcodeAddress;
827:                _visiblePC = _opcodeAddress;
843:                _visiblePC = _opcodeAddress;
855:                _visiblePC = _opcodeAddress;
869:                _visiblePC = _opcodeAddress;
894:                _visiblePC = _opcodeAddress;
915:                _visiblePC = _opcodeAddress;
927:                _visiblePC = _instructionPC;
960:        _visiblePC = _opcodeAddress;
983:                _visiblePC = _instructionPC;
986:                _visiblePC = _instructionPC;
994:                    _visiblePC = _instructionPC;
1003:                    _visiblePC = _instructionPC;
1010:                _visiblePC = _pc;
1014:                if (nonOverlappedJsr && _visiblePC == _instructionPC)
1018:                    _visiblePC = _pc;
1059:            _visiblePC = exportPc;
1117:                _visiblePC = _pc;
1159:                _visiblePC = _opcodeAddress;
</workspace_result>

### Turn 70 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 10 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Architectures\C64\Commodore64.cs
110:        while (!_cpu.IsInstructionBoundary);

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
24:        get => IsInstructionBoundary ? _pc : _visiblePC;
39:    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0;
1074:        // _suppressBootstrapBoundary set here closed IsInstructionBoundary for
2294:        // as the following opcode's first FETCH. Suppress IsInstructionBoundary

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\ICpu.cs
31:    bool IsInstructionBoundary => true;

F:\GitHub\vice-sharp\src\ViceSharp.Core\ArchitectureBuilder.cs
613:        while (!_cpu.IsInstructionBoundary);

F:\GitHub\vice-sharp\src\ViceSharp.Core\SystemClock.cs
90:        if (_cpu != null && _nmiPending && _cpu.IsInstructionBoundary)
95:        else if (_cpu != null && _irqLine != null && _irqLine.IsAsserted && _cpu.IsInstructionBoundary)

F:\GitHub\vice-sharp\src\ViceSharp.Core\CoordinatorMachine.cs
97:        while (cpu is not null && !cpu.IsInstructionBoundary);
</workspace_result>

### Turn 71 (Assistant)
Decoupling PC export from IRQ sampling: keep suppress for visible PC after JSR, and allow SystemClock to sample IRQ on that same boundary (VICE after JUMP, before callee FETCH).
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:     public ushort PC
    {
        get => IsInstructionBoundary ? _pc : _visiblePC;
        set
        {
            _pc = value;
            _instructionPC = value;
            _visiblePC = value;
        }
    }
    public byte Flags { get => P; set => P = value; }
    public byte P;

    // FR-CPUTICK-001: this CPU's own executed-cycle counter (incremented per Tick()).
    private long _executedCycles;
    public long ExecutedCycles => _executedCycles;

    public bool IsInstructionBoundary => !_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0;
- new_string:     public ushort PC
    {
        // Host-visible PC export is independent of interrupt-sample openings.
        // Suppress still forces _visiblePC so post-JSR non-overlapped last CLK
        // keeps exportPc (c=5012 nPC=$FDB7) while IsInstructionBoundary can
        // open for VICE DO_INTERRUPT before callee FETCH.
        get => (!_suppressBootstrapBoundary && _cycle == 0 && _interruptSequenceRemaining == 0)
            ? _pc
            : _visiblePC;
        set
        {
            _pc = value;
            _instructionPC = value;
            _visiblePC = value;
        }
    }
    public byte Flags { get => P; set => P = value; }
    public byte P;

    // FR-CPUTICK-001: this CPU's own executed-cycle counter (incremented per Tick()).
    private long _executedCycles;
    public long ExecutedCycles => _executedCycles;

    public bool IsInstructionBoundary =>
        _cycle == 0
        && _interruptSequenceRemaining == 0
        && (!_suppressBootstrapBoundary || _interruptSampleDespiteSuppress);
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 220
- limit: 60

### Turn 72 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 73 (Function)
[tool_response]
220→    /// each instruction boundary with the address about to be fetched. If it
    /// returns true the trapped instruction is skipped: the handler has already
    /// mutated registers/memory and set <see cref="PC"/> to the routine's resume
    /// address. Used to service virtual (non-true-drive) disk I/O without
    /// bit-banging the IEC bus. Null on true-drive and cycle-parity rigs.
    /// </summary>
    public Func<ushort, bool>? SerialTrapHook { get; set; }

    public Mos6502(IBus bus)
    {
230→        _bus = bus;
    }

    public void ConnectPubSub(IPubSub pubSub)
    {
        _pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
    }

    private byte _opcode;
    private ushort _currentInstructionPc; // fetch address of the in-flight instruction (for the completed-instruction publish)
240→    private bool _instructionExecuted; // guards the instruction-completed publish against the first (pre-fetch) boundary
    private int _cycle;
    private int _bootstrapCycles;
    private bool _suppressBootstrapBoundary;
    private bool _stagedMemoryReadCompleted;
    private bool _delayNextFetch;
    private bool _stagedNzUpdate;
    private byte _stagedNzValue;
    private bool _stagedCarryUpdate;
    private bool _stagedCarryValue;
250→    private bool _branchTargetFetchPending;
    private bool _branchPageCrossExtraPending;
    /// <summary>
    /// Host-visible cycles the previous instruction spent with PC already at the
    /// next opcode address. Drives host-visible taken-branch length vs xvic.
    /// </summary>
    private int _trailingCyclesAtNextPc;
    private int _currentInsnTrailingAtNextPc;
    /// <summary>Opcode address of the instruction currently in progress (stable for the whole instruction).</summary>
    private ushort _opcodeAddress;
260→    private bool _fullLengthTakenBranchCompleted;
    /// <summary>True while executing the instruction that immediately follows a full-length taken branch.</summary>
    private bool _afterFullLengthTakenBranch;
    /// <summary>
    /// After a full-length taken branch (and its non-overlapped JSR), the next
    /// instruction also lacks first-FETCH overlap until phase re-couples.
    /// </summary>
    private bool _nonOverlappedFetchPhase;
    /// <summary>
    /// Sticky for the whole non-overlapped subroutine region (full-length BNE
270→    /// through RTS), so lag-shaped shortcuts (e.g. STA collapsing into RTS)
    /// stay off until return.
    /// </summary>
    private bool _nonOverlappedRegion;
    /// <summary>Trailing dwell captured at last instruction fetch (for branch resolve).</summary>
    private int _branchFetchPriorTrailing;
    /// <summary>Not-taken branch must hold opcode PC on final CLK (clean first FETCH).</summary>
    private bool _notTakenBranchHoldFinalPc;
    /// <summary>Defer zp RMW PC advance one CLK after not-taken branch with clean FETCH.</summary>
    private bool _deferZpRmwPcAdvanceOne;

### Turn 74 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:     private int _bootstrapCycles;
    private bool _suppressBootstrapBoundary;
    private bool _stagedMemoryReadCompleted;
- new_string:     private int _bootstrapCycles;
    private bool _suppressBootstrapBoundary;
    /// <summary>
    /// Open SystemClock interrupt sampling while suppress still forces host
    /// PC export to _visiblePC (post-JSR JUMP before callee FETCH; VICE
    /// DO_INTERRUPT in the main loop).
    /// </summary>
    private bool _interruptSampleDespiteSuppress;
    private bool _stagedMemoryReadCompleted;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         var fetchingBranchTarget = false;
        var fetchingCallTarget = false;
        if (_suppressBootstrapBoundary)
        {
            fetchingBranchTarget = _branchTargetFetchPending;
            fetchingCallTarget = _callTargetFetchPending;
            _branchTargetFetchPending = false;
            _callTargetFetchPending = false;
            _suppressBootstrapBoundary = false;
        }
- new_string:         var fetchingBranchTarget = false;
        var fetchingCallTarget = false;
        if (_suppressBootstrapBoundary)
        {
            fetchingBranchTarget = _branchTargetFetchPending;
            fetchingCallTarget = _callTargetFetchPending;
            _branchTargetFetchPending = false;
            _callTargetFetchPending = false;
            _suppressBootstrapBoundary = false;
        }

        // One-tick post-JSR IRQ sample window ends before this tick's FETCH.
        _interruptSampleDespiteSuppress = false;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        // VICE 6510core.c main loop: after JSR JUMP, the next iteration runs
        // DO_INTERRUPT before FETCH of the callee. Leaving
        // _suppressBootstrapBoundary set here closed IsInstructionBoundary for
        // that post-JSR sample, so SystemClock never armed IRQ while native
        // entered DO_INTERRUPT with reg_pc frozen at the callee (c=540740
        // nS=$F2 mS=$F3, both PC=$D9B0 op=$76: managed ran ROR, native pushed).
        // Keep the boundary open so an asserted IRQ line can arm on this tick,
        // matching VICE. Do not set callTargetFetchPending either: that flag
        // was only cleared when suppress was consumed, and would stick open.
    }
- new_string:         PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        // Keep suppress so host PC export stays on exportPc for non-overlapped
        // JSR last CLK (c=5012 nPC=$FDB7 mPC would jump to target without it).
        _suppressBootstrapBoundary = true;
        // VICE 6510core.c main loop: after JSR JUMP, DO_INTERRUPT runs before
        // FETCH of the callee. Open interrupt sampling despite suppress so
        // SystemClock can arm IRQ on this tick (c=540740 nS=$F2 mS=$F3 when
        // sample was closed: native pushed, managed ran ROR at $D9B0).
        _interruptSampleDespiteSuppress = true;
    }

### Turn 75 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 76 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 77 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 78 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2555
- limit: 50
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-jsr-irq2-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Re-run focused gates after JSR IRQ decouple
- timeout: 300000

### Turn 79 (Function)
[tool_response]
2555→        S = s;
        P = p;
        PC = pc;
        ResetInFlightState();
    }
2560→
    public void Reset()
    {
        _executedCycles = 0;
        A = 0;
        X = 0;
        Y = 0;
        S = 0x00;
        P = 0x26;
        PC = _bus.Read(0xFFFC);
2570→        PC |= (ushort)(_bus.Read(0xFFFD) << 8);
        ResetInFlightState();
    }

    /// <summary>
    /// Clears the in-flight instruction micro-state and arms the one-cycle
    /// bootstrap stagger shared by <see cref="Reset"/> and
    /// <see cref="InjectSnapshotResumeState"/> (the native hosted bootstrap
    /// clears last_opcode_info/stolen_cycles/check_ba_low the same way before
    /// re-entering the fetch loop; mainc64cpu.c VICE_SHIM_HOSTED block).
2580→    /// </summary>
    private void ResetInFlightState()
    {
        _opcode = 0;
        _cycle = 0;
        _suppressBootstrapBoundary = true;
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;
        _delayNextFetch = false;
        _stagedNzUpdate = false;
2590→        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
        _stagedCarryValue = false;
        _branchTargetFetchPending = false;
        _branchPageCrossExtraPending = false;
        _trailingCyclesAtNextPc = 0;
        _currentInsnTrailingAtNextPc = 0;
        _fullLengthTakenBranchCompleted = false;
        _afterFullLengthTakenBranch = false;
        _nonOverlappedFetchPhase = false;
2600→        _nonOverlappedRegion = false;
        _deferZpRmwPcAdvanceOne = false;
        _nextJsrNonOverlapped = false;
        _notTakenBranchHoldFinalPc = false;
        _callTargetFetchPending = false;

### Turn 80 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 81 (Assistant)
Focused gates green. Running the 2s lockstep probe.
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-jsr-irq-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s every-cycle lockstep after JSR IRQ fix
- timeout: 300000
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         _suppressBootstrapBoundary = true;
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;
- new_string:         _suppressBootstrapBoundary = true;
        _interruptSampleDespiteSuppress = false;
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;

### Turn 82 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 83 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.39]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=540896 matchedCycles=540895 budget=2216810
c=540885 nPC=$EB25 mPC=$EB25 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=1 op=$84 trail=0 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=540886 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=0 op=$84 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540887 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=3 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540888 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=2 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540889 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=1 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=540890 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=0 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540891 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=3 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540892 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=2 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540893 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=1 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=540894 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$FF mX=$FF nY=$40 mY=$40 nS=$EB mS=$EB nP=$25 mP=$25 dbgCyc=0 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540895 nPC=$EB2D mPC=$EB2D nA=$00 mA=$00 nX=$FF mX=$FF nY=$40 mY=$40 nS=$EB mS=$EB nP=$A5 mP=$A5 dbgCyc=1 op=$E0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=540896 nPC=$EB2D mPC=$EB2F nA=$00 mA=$00 nX=$FF mX=$FF nY=$40 mY=$40 nS=$EB mS=$EB nP=$A5 mP=$27 dbgCyc=0 op=$E0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 84 (Assistant)
[tool_request: grep]
- pattern: IsTwoByteImmediateOpcode|0xE0|CPX|IsImmediate
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 50
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 800
- limit: 30
[tool_request: run_terminal_command]
- command: Select-String -Path docs\2s-jsr-irq-2026-08-06.log -Pattern "DIV|irq=|mis=True" | Select-Object -First 20
- description: Confirm 2s residual details from log

### Turn 85 (Function)
[tool_response]
800→            // host still samples pre-op regs/PC on that CLK when first-FETCH is
            // not overlapped (c=518756 CMP# in non-overlapped region after
            // not-taken BCC hold).
            // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#); skip soft for non-load imm once.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                if (_skipSoftImmAfterStagedTakenBranch && !IsImmediateLoadOpcode(_opcode))
                {
810→                    _skipSoftImmAfterStagedTakenBranch = false;
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
820→                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;

### Turn 86 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 14 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
337:    /// CMP zp/zx (and CPX/CPY zp) after clean taken-branch fetch: soft-defer body
699:                        && IsTwoByteImmediateOpcode(_previousOpcode)
707:            _deferImmediateLoadAfterBranch = armAfterBranchLag && IsImmediateLoadOpcode(_opcode);
806:                && IsTwoByteImmediateOpcode(_opcode))
808:                if (_skipSoftImmAfterStagedTakenBranch && !IsImmediateLoadOpcode(_opcode))
815:                    if (IsImmediateLoadOpcode(_opcode))
881:            if (_softDeferAfterNopChain && IsTwoByteImmediateOpcode(_opcode))
2007:    private static bool IsImmediateLoadOpcode(byte opcode)
2016:    private static bool IsTwoByteImmediateOpcode(byte opcode)
2018:        return IsImmediateLoadOpcode(opcode)
2019:            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0;
2141:            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0
2186:        return IsImmediateLoadOpcode(opcode)
2662:            0xE0 or 0xE4 or 0xEC or
</workspace_result>

### Turn 87 (Function)
[tool_response]
exit: 0

docs\2s-jsr-irq-2026-08-06.log:3:[xUnit.net 00:00:04.39]     
ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
docs\2s-jsr-irq-2026-08-06.log:4:  Failed 
ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
docs\2s-jsr-irq-2026-08-06.log:6:   DIV first=540896 matchedCycles=540895 budget=2216810
docs\2s-jsr-irq-2026-08-06.log:7:c=540885 nPC=$EB25 mPC=$EB25 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=1 op=$84 trail=0 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:8:c=540886 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=0 op=$84 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:9:c=540887 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=3 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:10:c=540888 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=2 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:11:c=540889 nPC=$EB27 mPC=$EB27 nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=1 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:12:c=540890 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=0 op=$8D trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:13:c=540891 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=3 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:14:c=540892 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=2 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:15:c=540893 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$00 mX=$00 nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=1 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:16:c=540894 nPC=$EB2A mPC=$EB2A nA=$00 mA=$00 nX=$FF mX=$FF nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$25 mP=$25 dbgCyc=0 op=$AE trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:17:c=540895 nPC=$EB2D mPC=$EB2D nA=$00 mA=$00 nX=$FF mX=$FF nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$A5 mP=$A5 dbgCyc=1 op=$E0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
docs\2s-jsr-irq-2026-08-06.log:18:c=540896 nPC=$EB2D mPC=$EB2F nA=$00 mA=$00 nX=$FF mX=$FF nY=$40 mY=$40 nS=$EB mS=$EB 
nP=$A5 mP=$27 dbgCyc=0 op=$E0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
docs\2s-jsr-irq-2026-08-06.log:22:     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in 
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
docs\2s-jsr-irq-2026-08-06.log:23:   at 
ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in 
F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47



### Turn 88 (Assistant)
[tool_request: grep]
- pattern: _skipSoftImmAfterStagedTakenBranch
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2010
- limit: 40
[tool_request: grep]
- pattern: CPX|0xE0|Compare
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.Opcodes.cs
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1920
- limit: 30

### Turn 89 (Function)
[tool_response]
2010→    }

    /// <summary>
    /// 2-byte immediate ops whose VICE body runs INC_PC with no trailing CLK_INC
    /// after FETCH (LD/CP/ALU #imm). Used for post-NOP-chain soft-defer.
    /// </summary>
    private static bool IsTwoByteImmediateOpcode(byte opcode)
    {
        return IsImmediateLoadOpcode(opcode)
            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0;
2020→    }

    /// <summary>Unstaged zero-page compares (not CD/D1 staged path).</summary>
    private static bool IsZeroPageCompareOpcode(byte opcode)
    {
        return opcode is 0xC5 or 0xD5 or 0xE4 or 0xC4;
    }

    /// <summary>
    /// Absolute ALU ops executed via unstaged ExecuteOpcode (not cycle-staged
2030→    /// CMP abs / RMW). VICE GET_ABS + ORA/AND/... apply A after the data-read
    /// CLK export.
    /// </summary>
    private static bool IsAbsoluteAluOpcode(byte opcode)
    {
        return opcode is 0x0D or 0x2D or 0x4D or 0x6D or 0xED
            or 0x1D or 0x3D or 0x5D or 0x7D or 0xFD
            or 0x19 or 0x39 or 0x59 or 0x79 or 0xF9
            or 0x05 or 0x25 or 0x45 or 0x65 or 0xE5
            or 0x15 or 0x35 or 0x55 or 0x75 or 0xF5;
2040→    }

    private static bool IsImpliedRegisterOrFlagOpcode(byte opcode)
    {
        return opcode is
            0x0A or // ASL A
            0x18 or // CLC
            0x2A or // ROL A
            0x38 or // SEC
            0x4A or // LSR A

### Turn 90 (Function)
[tool_response]
1920→        else
        {
            _branchTargetFetchPending = true;
        }

        return true;
    }

    private static bool IsBranchOpcode(byte opcode)
    {
1930→        return opcode is 0x10 or 0x30 or 0x50 or 0x70 or 0x90 or 0xB0 or 0xD0 or 0xF0;
    }

    private bool IsBranchTaken(byte opcode)
    {
        return opcode switch
        {
            0x10 => (P & 0x80) == 0,
            0x30 => (P & 0x80) != 0,
            0x50 => (P & 0x40) == 0,
1940→            0x70 => (P & 0x40) != 0,
            0x90 => (P & 0x01) == 0,
            0xB0 => (P & 0x01) != 0,
            0xD0 => (P & 0x02) == 0,
            0xF0 => (P & 0x02) != 0,
            _ => false
        };
    }

    private void PrefetchOpcodeAt(ushort address)

### Turn 91 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 6 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
358:    private bool _skipSoftImmAfterStagedTakenBranch;
808:                if (_skipSoftImmAfterStagedTakenBranch && !IsImmediateLoadOpcode(_opcode))
810:                    _skipSoftImmAfterStagedTakenBranch = false;
1903:            _skipSoftImmAfterStagedTakenBranch = true;
1984:        _skipSoftImmAfterStagedTakenBranch = false;
2627:        _skipSoftImmAfterStagedTakenBranch = false;
</workspace_result>

### Turn 92 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 18 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.Opcodes.cs
396:            // CMP - Compare Accumulator
397:            case 0xC9: Compare(A, Immediate()); break;
398:            case 0xC5: Compare(A, ZeroPage()); break;
399:            case 0xD5: Compare(A, ZeroPageX()); break;
400:            case 0xCD: Compare(A, Absolute()); break;
401:            case 0xDD: Compare(A, AbsoluteX()); break;
402:            case 0xD9: Compare(A, AbsoluteY()); break;
403:            case 0xC1: Compare(A, IndirectX()); break;
404:            case 0xD1: Compare(A, IndirectY()); break;
406:            // CPX - Compare X Register
407:            case 0xE0: Compare(X, Immediate()); break;
408:            case 0xE4: Compare(X, ZeroPage()); break;
409:            case 0xEC: Compare(X, Absolute()); break;
411:            // CPY - Compare Y Register
412:            case 0xC0: Compare(Y, Immediate()); break;
413:            case 0xC4: Compare(Y, ZeroPage()); break;
414:            case 0xCC: Compare(Y, Absolute()); break;
617:    private void Compare(byte register, ushort address)
</workspace_result>

### Turn 93 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1860
- limit: 60
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 350
- limit: 20
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.Opcodes.cs
- offset: 610
- limit: 30

### Turn 94 (Function)
[tool_response]
1860→            // INC_PC runs after that sample. Hold MUST win before RTS prefetch
            // (c=532304 nPC=$E906 mPC=$E908 when PrefetchOpcodeAt skipped hold).
            if (_notTakenBranchHoldFinalPc)
            {
                _pc = fallThrough;
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                // Following zp INC needs one extra FETCH-shaped CLK at its opcode
                // before AdvanceVisiblePc (cycle 20756).
                _deferZpRmwPcAdvanceOne = true;
1870→                // Fall-through insn also has a clean first FETCH (ROR A at 195424).
                _nonOverlappedFetchPhase = true;
                _nonOverlappedRegion = true;
                _notTakenBranchHoldFinalPc = false;
                return true;
            }

            if (Peek(fallThrough) == 0x60)
            {
                PrefetchOpcodeAt(fallThrough);
1880→                return true;
            }

            // Not-taken without hold (e.g. priorTrailing>=3): VICE already
            // exported fall-through; clear sticky non-overlapped so following
            // SEC/implied is fused not soft-lagged (c=540323 nP=$A1 mP=$A0).
            if (_branchFetchPriorTrailing >= 3)
            {
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
1890→            }

            return false;
        }

        var target = (ushort)(fallThrough + (sbyte)Read((ushort)(_instructionPC + 1)));
        _pc = target;
        // Multi-cycle staged path already exported fall-through at cycle 1;
        // this CLK is VICE JUMP (c=540202 nPC=$D91D mPC=$D92B when still
        // fall-through). Short taken keeps fall-through visible here.
1900→        if (_takenBranchStagedFallthrough)
        {
            _visiblePC = target;
            _skipSoftImmAfterStagedTakenBranch = true;
        }
        else
        {
            _visiblePC = fallThrough;
        }

1910→        _suppressBootstrapBoundary = true;
        _takenBranchStagedFallthrough = false;
        if ((fallThrough & 0xFF00) != (target & 0xFF00))
        {
            // TR-LOCKSTEP-VSF-001: a taken branch across a page boundary costs
            // 4 native cycles (6510dtvcore.c BRANCH: the PBC fix-up cycle does
            // another dummy fetch and keeps exporting the un-fixed PC); consume
            // one extra tick before the target fetch.
            _branchPageCrossExtraPending = true;
        }

### Turn 95 (Function)
[tool_response]
350→    /// lag): export fall-through PC at cycle 1 to match VICE INC_PC+dummy before
    /// JUMP (c=540201 nPC=$D92B mPC=$D929 on BPL).
    /// </summary>
    private bool _takenBranchStagedFallthrough;
    /// <summary>
    /// After staged multi-cycle taken JUMP, next non-load 2-byte imm is fused
    /// (c=540204 ADC#); skip one soft-defer.
    /// </summary>
    private bool _skipSoftImmAfterStagedTakenBranch;

360→    /// <summary>
    /// After a short taken branch, NOP's final host sample must still export the
    /// opcode PC (VICE INC_PC after the last FETCH CLK). The hold phase sticks
    /// across a run of NOPs (c=518739 first, c=518741 second) until a non-NOP.
    /// </summary>
    private bool _nopHoldOpcodePcOnFinal;
    private bool _nopChainHoldAfterBranch;
    /// <summary>
    /// After a post-branch NOP chain, the next instruction's first FETCH is fused
    /// on VICE with the last NOP's INC_PC; soft-defer 2-byte immediate commit so

### Turn 96 (Function)
[tool_response]
610→    private ushort PopWord()
    {
        byte lo = Pop();
        byte hi = Pop();
        return (ushort)(lo | (hi << 8));
    }

    private void Compare(byte register, ushort address)
    {
        byte value = _bus.Read(address);
620→        ushort result = (ushort)(register - value);

        if (register >= value)
            P |= 0x01;
        else
            P &= 0xFE;

        UpdateNZ((byte)result);
    }

630→    private void BranchRelative(bool shouldBranch)
    {
        sbyte offset = (sbyte)Fetch();
        if (shouldBranch)
            PC = (ushort)(PC + offset);
    }

    private byte ASL(byte value)
    {
        if ((value & 0x80) != 0)

### Turn 97 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 598
- limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 800
- limit: 40

### Turn 98 (Function)
[tool_response]
800→            // host still samples pre-op regs/PC on that CLK when first-FETCH is
            // not overlapped (c=518756 CMP# in non-overlapped region after
            // not-taken BCC hold).
            // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#); skip soft for non-load imm once.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                if (_skipSoftImmAfterStagedTakenBranch && !IsImmediateLoadOpcode(_opcode))
                {
810→                    _skipSoftImmAfterStagedTakenBranch = false;
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
820→                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;
830→                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }

            // VICE abs ALU (ORA/AND/EOR/ADC/SBC $nnnn): GET_ABS CLK exports
            // pre-op A/PC; body + INC_PC after that sample (c=519285 nA=$03
            // mA=$1F nPC=opcode mPC=next). Soft-defer when non-overlapped.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)

### Turn 99 (Function)
[tool_response]
598→
            // Capture previous instruction's trailing dwell, then start fresh.
600→            var priorTrailingAtNextPc = _trailingCyclesAtNextPc;
            DebugPriorTrailingAtNextPc = priorTrailingAtNextPc;
            _trailingCyclesAtNextPc = 0;
            _currentInsnTrailingAtNextPc = 0;

            // Preserve prior opcode before overwrite (RTS overlap phase uses it).
            _previousOpcode = _opcode;
            _opcode = Read(_pc++);
            // holdIndY is only for the instruction immediately after a soft-deferred
            // load/implied; clear if this fetch is not STA (zp),Y (c=519327 false hold).
610→            if (_opcode != 0x91)
                _holdIndYStorePcOneCycle = false;
            // deferZpRmwPcAdvanceOne is only for the zp INC/DEC immediately after a
            // not-taken branch clean FETCH; sticky flag delayed INC NZ/PC by one
            // cycle far from any branch (c=522317 nP=$25 mP=$27 nPC advanced).
            if (!IsZeroPageIncrementDecrementOpcode(_opcode))
                _deferZpRmwPcAdvanceOne = false;
            _instructionExecuted = true; // a real instruction has now been fetched/executed
            _cycle = GetCycleCount(_opcode);
            // Match host-visible xvic taken-branch length (VICE BRANCH is always
620→            // 3 CLK; first FETCH may be overlapped with the previous opcode's
            // trailing export at this same address).
            // Empirically vs xvic GetPC after each step_cycle:
            //   dwell 1 (e.g. CMP last cycle only): 2 host-visible branch steps
            //   dwell 2 (e.g. zp INC after deferred mid-PC advance): 3 host steps
            //   dwell 3 (e.g. zp INC with early AdvanceVisiblePc): 2 host steps
            // Full-length taken budget only for the dwell==2 case (cycle 5005).
            var fullLengthTakenBranch = false;

### Turn 100 (Assistant)
Sticky `_skipSoftImmAfterStagedTakenBranch` was fusing CPX# far after its intended one-instruction window. Narrowing it to a single instruction, then re-running gates.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             // VICE 2-byte immediates (LD#/CMP#/AND#/...): GET_IMM + body + INC_PC
            // run after the second FETCH CLK is exported. Soft-defer the body so
            // host still samples pre-op regs/PC on that CLK when first-FETCH is
            // not overlapped (c=518756 CMP# in non-overlapped region after
            // not-taken BCC hold).
            // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#); skip soft for non-load imm once.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                if (_skipSoftImmAfterStagedTakenBranch && !IsImmediateLoadOpcode(_opcode))
                {
                    _skipSoftImmAfterStagedTakenBranch = false;
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }
- new_string:             // VICE 2-byte immediates (LD#/CMP#/AND#/...): GET_IMM + body + INC_PC
            // run after the second FETCH CLK is exported. Soft-defer the body so
            // host still samples pre-op regs/PC on that CLK when first-FETCH is
            // not overlapped (c=518756 CMP# in non-overlapped region after
            // not-taken BCC hold).
            // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#). That skip is a ONE-instruction window only:
            // sticky skip fused CPX# much later (c=540896 nPC=$EB2D mPC=$EB2F).
            var skipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch;
            _skipSoftImmAfterStagedTakenBranch = false;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                if (skipSoftImmThisInsn && !IsImmediateLoadOpcode(_opcode))
                {
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }

### Turn 101 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 102 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-cpx-soft-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Re-run focused gates after soft-imm window fix
- timeout: 300000

### Turn 103 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 104 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-cpx-soft-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after CPX soft-imm fix
- timeout: 300000

### Turn 105 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.26]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=540964 matchedCycles=540963 budget=2216810
c=540953 nPC=$EB1C mPC=$EB1C nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F0 mS=$F0 nP=$65 mP=$65 dbgCyc=0 op=$68 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540954 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F0 mS=$F0 nP=$67 mP=$67 dbgCyc=5 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540955 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F0 mS=$F0 nP=$67 mP=$67 dbgCyc=4 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540956 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F0 mS=$F0 nP=$67 mP=$67 dbgCyc=4 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540957 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F1 mS=$F1 nP=$67 mP=$67 dbgCyc=3 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540958 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F2 mS=$F2 nP=$21 mP=$21 dbgCyc=2 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540959 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=1 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540960 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=0 op=$40 trail=0 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540961 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=5 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540962 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=4 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540963 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=3 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540964 nPC=$D9B2 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$21 dbgCyc=2 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 106 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1293
- limit: 30
[tool_request: grep]
- pattern: IsLoadOpcode
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- -A: 15

### Turn 107 (Function)
[tool_response]
1293→            {
                // Hold opcode PC one more CLK (extra FETCH after not-taken BNE).
                _deferZpRmwPcAdvanceOne = false;
                _nextJsrNonOverlapped = true;
                _visiblePC = _opcodeAddress;
                return true;
            }
1300→
            // VICE INC/DEC zp (6510core.c): after load + dummy RMW write,
            // LOCAL_SET_NZ and INC_PC become visible before the final STORE CLK.
            // Absolute RMW already matches this; zp was advancing PC only and
            // lagging NZ until cycle 1 (diverge at c=517829: nP=$21 mP=$23).
            CommitZeroPageRmwModifyAndFlags();
            AdvanceVisiblePc(2);
            return true;
        }

1310→        if (_cycle == 1 && IsZeroPageIncrementDecrementOpcode(_opcode) && _visiblePC == _opcodeAddress
            && !_stagedMemoryReadCompleted)
        {
            // Complete deferred Advance after the hold tick. VICE still pairs
            // LOCAL_SET_NZ with INC_PC on this same host sample.
            CommitZeroPageRmwModifyAndFlags();
            AdvanceVisiblePc(2);
            // do not return - allow cycle==1 final-store switch to run
        }

1320→        // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 after taken branch (c=539721 ASL after BCC);

### Turn 108 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 2 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
1328:                || IsLoadOpcode(_previousOpcode)
1329-                || DebugPriorTrailingAtNextPc == 0;
1330-            var commitAtCycle1 = zpX && !commitAtCycle2;
1331-            if ((_cycle == 2 && commitAtCycle2) || (_cycle == 1 && commitAtCycle1))
1332-            {
1333-                CommitZeroPageShiftRmwModifyAndFlags();
1334-                AdvanceVisiblePc(2);
1335-                return true;
1336-            }
1337-        }
1338-
1339-        if (_cycle == 4 && IsIndirectYStoreOpcode(_opcode))
1340-        {
1341-            // Non-overlapped path after soft LDA #: hold opcode PC one more CLK
1342-            // so host-visible matches VICE (2 FETCH at start before INC_PC).
1343-            if (_holdIndYStorePcOneCycle)
--
2186:    private static bool IsLoadOpcode(byte opcode)
2187-    {
2188-        return IsImmediateLoadOpcode(opcode)
2189-            || opcode is 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
2190-            or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE
2191-            or 0xA1 or 0xB1;
2192-    }
2193-
2194-    /// <summary>
2195-    /// VICE ASL/LSR/ROL/ROR + SET_ZERO(_X)_RMW: SET_C/NZ and INC_PC before the
2196-    /// final STORE host sample (6510dtvcore.c ASL macro order).
2197-    /// </summary>
2198-    private void CommitZeroPageShiftRmwModifyAndFlags()
2199-    {
2200-        var address = _opcode is 0x16 or 0x36 or 0x56 or 0x76
2201-            ? (ushort)(byte)(ReadZeroPageOperand() + X)
</workspace_result>

### Turn 109 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 after taken branch (c=539721 ASL after BCC);
        // cycle 2 after load (c=540730 LSR after LDA zp nPC advanced at dbgCyc=2).
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle2 = !zpX
                || IsLoadOpcode(_previousOpcode)
                || DebugPriorTrailingAtNextPc == 0;
            var commitAtCycle1 = zpX && !commitAtCycle2;
            if ((_cycle == 2 && commitAtCycle2) || (_cycle == 1 && commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }
- new_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 after taken branch (c=539721 ASL after BCC);
        // cycle 2 after load (c=540730 LSR after LDA zp), and after RTI/RTS
        // return to callee (c=540964 nPC=$D9B2 mPC=$D9B0 when cycle-1 lag
        // left managed one CLK late after IRQ RTI).
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle2 = !zpX
                || IsLoadOpcode(_previousOpcode)
                || _previousOpcode is 0x40 or 0x60
                || DebugPriorTrailingAtNextPc == 0;
            var commitAtCycle1 = zpX && !commitAtCycle2;
            if ((_cycle == 2 && commitAtCycle2) || (_cycle == 1 && commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }

### Turn 110 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 111 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-ror-rti-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after ROR post-RTI commit fix
- timeout: 300000

### Turn 112 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 113 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-ror-rti-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after ROR post-RTI fix
- timeout: 300000

### Turn 114 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.28]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=540970 matchedCycles=540969 budget=2216810
c=540959 nPC=$EB1D mPC=$EB1D nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=1 op=$40 trail=0 nonOvlR=False nonOvlF=True dly=False stg=False irq=0 mis=False
c=540960 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=0 op=$40 trail=0 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540961 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=5 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540962 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=4 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540963 nPC=$D9B0 mPC=$D9B0 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$21 mP=$21 dbgCyc=3 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540964 nPC=$D9B2 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$A0 dbgCyc=2 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540965 nPC=$D9B2 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$A0 dbgCyc=1 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540966 nPC=$D9B2 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$A0 dbgCyc=0 op=$76 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540967 nPC=$D9B2 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$A0 dbgCyc=5 op=$76 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540968 nPC=$D9B2 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$A0 dbgCyc=4 op=$76 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540969 nPC=$D9B2 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$A0 mP=$A0 dbgCyc=3 op=$76 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=540970 nPC=$D9B4 mPC=$D9B2 nA=$00 mA=$00 nX=$61 mX=$61 nY=$FE mY=$FE nS=$F3 mS=$F3 nP=$22 mP=$A0 dbgCyc=2 op=$76 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 115 (Assistant)
Second ROR after ROR still lagged. Narrowing zp,X cycle-1 commit to only after a branch (VICE lag), defaulting everything else to cycle 2.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 after taken branch (c=539721 ASL after BCC);
        // cycle 2 after load (c=540730 LSR after LDA zp), and after RTI/RTS
        // return to callee (c=540964 nPC=$D9B2 mPC=$D9B0 when cycle-1 lag
        // left managed one CLK late after IRQ RTI).
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle2 = !zpX
                || IsLoadOpcode(_previousOpcode)
                || _previousOpcode is 0x40 or 0x60
                || DebugPriorTrailingAtNextPc == 0;
            var commitAtCycle1 = zpX && !commitAtCycle2;
            if ((_cycle == 2 && commitAtCycle2) || (_cycle == 1 && commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }
- new_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 only after a branch predecessor (c=539721 ASL
        // after BCC; branch lag shortens the host-visible schedule). Otherwise
        // cycle 2: after load (c=540730), after RTI (c=540964), and after a
        // prior shift RMW (c=540970 nPC=$D9B4 mPC=$D9B2 when cycle-1 lag stuck).
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 = zpX && IsBranchOpcode(_previousOpcode);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }

### Turn 116 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 117 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-ror-chain-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after zpX shift commit realign
- timeout: 300000

### Turn 118 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 119 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-ror-chain-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after ROR chain fix
- timeout: 300000

### Turn 120 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.17]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=539726 matchedCycles=539725 budget=2216810
c=539715 nPC=$E705 mPC=$E705 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$A0 mP=$A0 dbgCyc=1 op=$90 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539716 nPC=$E707 mPC=$E707 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$A0 mP=$A0 dbgCyc=0 op=$90 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539717 nPC=$E70E mPC=$E70E nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$A0 mP=$A0 dbgCyc=5 op=$16 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539718 nPC=$E70E mPC=$E70E nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$A0 mP=$A0 dbgCyc=4 op=$16 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539719 nPC=$E70E mPC=$E70E nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$A0 mP=$A0 dbgCyc=3 op=$16 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539720 nPC=$E70E mPC=$E70E nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$A0 mP=$A0 dbgCyc=2 op=$16 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539721 nPC=$E710 mPC=$E710 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$21 mP=$21 dbgCyc=1 op=$16 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539722 nPC=$E710 mPC=$E710 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$21 mP=$21 dbgCyc=0 op=$16 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539723 nPC=$E710 mPC=$E710 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$21 mP=$21 dbgCyc=5 op=$56 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539724 nPC=$E710 mPC=$E710 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$21 mP=$21 dbgCyc=4 op=$56 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539725 nPC=$E710 mPC=$E710 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$21 mP=$21 dbgCyc=3 op=$56 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=539726 nPC=$E710 mPC=$E712 nA=$00 mA=$00 nX=$01 mX=$01 nY=$15 mY=$15 nS=$F0 mS=$F0 nP=$21 mP=$20 dbgCyc=2 op=$56 trail=2 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 121 (Assistant)
Restoring branch-lag cycle-1, and adding cycle-1 only when prior shift RMW has trail==2 (the 539726 case).
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle): cycle 1 only after a branch predecessor (c=539721 ASL
        // after BCC; branch lag shortens the host-visible schedule). Otherwise
        // cycle 2: after load (c=540730), after RTI (c=540964), and after a
        // prior shift RMW (c=540970 nPC=$D9B4 mPC=$D9B2 when cycle-1 lag stuck).
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 = zpX && IsBranchOpcode(_previousOpcode);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }
- new_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle) host-visible modify phase:
        //   cycle 1: after branch (c=539721 ASL after BCC), or after prior
        //            shift RMW with trail==2 (c=539726 LSR after ASL chain)
        //   cycle 2: after load (c=540730), after RTI (c=540964), after prior
        //            shift with trail!=2 (c=540970 ROR after ROR trail=3)
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 = zpX && (
                IsBranchOpcode(_previousOpcode)
                || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 2));
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }

### Turn 122 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 123 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-shift-phase-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after refined shift phase
- timeout: 300000

### Turn 124 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 125 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-shift-phase-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after shift phase refine
- timeout: 300000

### Turn 126 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.17]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541100 matchedCycles=541099 budget=2216810
c=541089 nPC=$D936 mPC=$D936 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=1 op=$90 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541090 nPC=$D936 mPC=$D936 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=0 op=$90 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541091 nPC=$D938 mPC=$D938 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=4 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541092 nPC=$D938 mPC=$D938 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=3 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541093 nPC=$D938 mPC=$D938 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$21 mP=$21 dbgCyc=2 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541094 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=1 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541095 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=0 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541096 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=1 op=$F0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541097 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=0 op=$F0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541098 nPC=$D93C mPC=$D93C nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=4 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541099 nPC=$D93C mPC=$D93C nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=3 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541100 nPC=$D93C mPC=$D93E nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=2 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 127 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // zp (5-cycle): always at cycle 2 (c=539962).
        // zp,X (6-cycle) host-visible modify phase:
        //   cycle 1: after branch (c=539721 ASL after BCC), or after prior
        //            shift RMW with trail==2 (c=539726 LSR after ASL chain)
        //   cycle 2: after load (c=540730), after RTI (c=540964), after prior
        //            shift with trail!=2 (c=540970 ROR after ROR trail=3)
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 = zpX && (
                IsBranchOpcode(_previousOpcode)
                || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 2));
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }
- new_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // Host-visible modify phase (zp 5-cycle / zp,X 6-cycle):
        //   cycle 1: zp,X after branch (c=539721 ASL after BCC); zp,X after
        //            prior shift with trail==2 (c=539726); zp after branch
        //            with trail==0 nonOvl (c=541100 ROR zp after not-taken BEQ
        //            nPC=opcode mPC advanced at dbgCyc=2)
        //   cycle 2: default, including after load (c=540730), RTI (c=540964),
        //            prior shift trail!=2 (c=540970), plain zp (c=539962)
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 = zpX
                ? (IsBranchOpcode(_previousOpcode)
                    || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc == 2))
                : (IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 0);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }

### Turn 128 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 129 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-ror-zp-beq-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after zp ROR post-BEQ fix
- timeout: 300000

### Turn 130 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 131 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-ror-zp-beq-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after zp ROR post-BEQ fix
- timeout: 300000

### Turn 132 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.17]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541105 matchedCycles=541104 budget=2216810
c=541094 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=1 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541095 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=0 op=$E6 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541096 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=1 op=$F0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541097 nPC=$D93A mPC=$D93A nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=0 op=$F0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541098 nPC=$D93C mPC=$D93C nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=4 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541099 nPC=$D93C mPC=$D93C nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=3 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541100 nPC=$D93C mPC=$D93C nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=2 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541101 nPC=$D93E mPC=$D93E nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=1 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541102 nPC=$D93E mPC=$D93E nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=0 op=$66 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541103 nPC=$D93E mPC=$D93E nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=4 op=$66 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541104 nPC=$D93E mPC=$D93E nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A1 dbgCyc=3 op=$66 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541105 nPC=$D93E mPC=$D940 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F5 mS=$F5 nP=$A1 mP=$A0 dbgCyc=2 op=$66 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 133 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // Host-visible modify phase (zp 5-cycle / zp,X 6-cycle):
        //   cycle 1: zp,X after branch (c=539721 ASL after BCC); zp,X after
        //            prior shift with trail==2 (c=539726); zp after branch
        //            with trail==0 nonOvl (c=541100 ROR zp after not-taken BEQ
        //            nPC=opcode mPC advanced at dbgCyc=2)
        //   cycle 2: default, including after load (c=540730), RTI (c=540964),
        //            prior shift trail!=2 (c=540970), plain zp (c=539962)
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 = zpX
                ? (IsBranchOpcode(_previousOpcode)
                    || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                        && DebugPriorTrailingAtNextPc == 2))
                : (IsBranchOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 0);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }
- new_string:         // ASL/LSR/ROL/ROR: VICE SET_C/NZ + INC_PC before final STORE.
        // Host-visible modify phase (zp 5-cycle / zp,X 6-cycle):
        //   cycle 1: zp,X after branch (c=539721); zp after branch trail==0
        //            (c=541100 ROR zp after not-taken BEQ); either mode after
        //            prior shift with trail==2 (c=539726 zp,X; c=541105 zp)
        //   cycle 2: default (load c=540730, RTI c=540964, shift trail!=2
        //            c=540970, plain zp c=539962)
        if (!_zpRmwModifyCommitted && IsZeroPageShiftRmwOpcode(_opcode))
        {
            var zpX = IsZeroPageXShiftRmwOpcode(_opcode);
            var commitAtCycle1 =
                (IsBranchOpcode(_previousOpcode)
                    && (zpX || DebugPriorTrailingAtNextPc == 0))
                || (IsZeroPageShiftRmwOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 2);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
                return true;
            }
        }

### Turn 134 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 135 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-ror-chain2-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after zp shift chain fix
- timeout: 300000

### Turn 136 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 137 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-ror-chain2-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after zp shift chain fix
- timeout: 300000

### Turn 138 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.19]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541157 matchedCycles=541156 budget=2216810
c=541146 nPC=$DE26 mPC=$DE26 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$C6 trail=7 nonOvlR=False nonOvlF=False dly=False stg=True irq=0 mis=False
c=541147 nPC=$DE26 mPC=$DE26 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$C6 trail=7 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541148 nPC=$DE26 mPC=$DE26 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$D0 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541149 nPC=$DE28 mPC=$DE28 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$D0 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541150 nPC=$DE16 mPC=$DE16 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541151 nPC=$DE16 mPC=$DE16 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541152 nPC=$DE18 mPC=$DE18 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541153 nPC=$DE18 mPC=$DE18 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541154 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541155 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=5 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541156 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=4 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541157 nPC=$DE1C mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F8 mS=$F9 nP=$A0 mP=$A0 dbgCyc=3 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 139 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 970
- limit: 90
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 990
- limit: 60

### Turn 140 (Function)
[tool_response]
990→        {
            return TryExecuteCycleStagedMemoryReadOpcode();
        }

        // Clean first FETCH (full-length branch or sticky non-overlapped region
        // after not-taken hold): VICE keeps PC at JSR through STACK_PEEK before
        // INC_PC+PUSH (c=518797 nPC=$E8B5 mPC=$E8B7 nS=$F2 mS=$F1).
        var nonOverlappedJsr = _afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase;
1000→        switch (_cycle)
        {
            case 5:
                _visiblePC = _instructionPC;
                return true;
            case 4:
                _visiblePC = _instructionPC;
                return true;
            case 3:
                if (_deferJsrPushAfterBranch)
1010→                {
                    // Short taken-branch lag: one extra peek-shaped CLK.
                    _deferJsrPushAfterBranch = false;
                    _cycle = 4;
                    _visiblePC = _instructionPC;
                    _suppressBootstrapBoundary = true;
                    return true;
                }

                if (nonOverlappedJsr)
1020→                {
                    // Clean first FETCH: keep PC at the opcode for the STACK_PEEK
                    // CLK as well (VICE: 2 FETCH + PEEK all export reg_pc at JSR).
                    _visiblePC = _instructionPC;
                    return true;
                }

                // Overlapped first FETCH (common after lag-shaped previous insn):
                // this CLK is already the post-peek INC_PC + PUSH high.
                _pc = (ushort)(_instructionPC + 2);
1030→                _visiblePC = _pc;
                Push((byte)(_pc >> 8));
                return true;
            case 2:
                if (nonOverlappedJsr && _visiblePC == _instructionPC)
                {
                    // Clean path: INC_PC + PUSH high on this CLK.
                    _pc = (ushort)(_instructionPC + 2);
                    _visiblePC = _pc;
                    Push((byte)(_pc >> 8));
1040→                    return true;
                }

                Push((byte)_pc);
                return true;
            case 1:
                if (nonOverlappedJsr)
                {
                    Push((byte)_pc);
                    return true;

### Turn 141 (Function)
[tool_response]
970→    /// </summary>
    private void SoftDeferBitBody()
    {
        if (_opcode == 0x2C)
            _ = Read(ReadAbsoluteOperand());
        else
            _ = Read(ReadZeroPageOperand());
        _softDeferredImpliedOp = true;
        _softDeferredImpliedOpcode = _opcode;
        _softDeferredImpliedInstructionPc = _opcodeAddress;
980→        _visiblePC = _opcodeAddress;
        _pc = (ushort)(_opcodeAddress + 1);
        _cycle = 0;
        _bitSoftDeferEarly = false;
        _suppressBootstrapBoundary = true;
    }

    private bool TryExecuteCycleStagedOpcode()
    {
        if (_opcode != 0x20)
990→        {
            return TryExecuteCycleStagedMemoryReadOpcode();
        }

        // Clean first FETCH (full-length branch or sticky non-overlapped region
        // after not-taken hold): VICE keeps PC at JSR through STACK_PEEK before
        // INC_PC+PUSH (c=518797 nPC=$E8B5 mPC=$E8B7 nS=$F2 mS=$F1).
        var nonOverlappedJsr = _afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase;
1000→        switch (_cycle)
        {
            case 5:
                _visiblePC = _instructionPC;
                return true;
            case 4:
                _visiblePC = _instructionPC;
                return true;
            case 3:
                if (_deferJsrPushAfterBranch)
1010→                {
                    // Short taken-branch lag: one extra peek-shaped CLK.
                    _deferJsrPushAfterBranch = false;
                    _cycle = 4;
                    _visiblePC = _instructionPC;
                    _suppressBootstrapBoundary = true;
                    return true;
                }

                if (nonOverlappedJsr)
1020→                {
                    // Clean first FETCH: keep PC at the opcode for the STACK_PEEK
                    // CLK as well (VICE: 2 FETCH + PEEK all export reg_pc at JSR).
                    _visiblePC = _instructionPC;
                    return true;
                }

                // Overlapped first FETCH (common after lag-shaped previous insn):
                // this CLK is already the post-peek INC_PC + PUSH high.
                _pc = (ushort)(_instructionPC + 2);
1030→                _visiblePC = _pc;
                Push((byte)(_pc >> 8));
                return true;
            case 2:
                if (nonOverlappedJsr && _visiblePC == _instructionPC)
                {
                    // Clean path: INC_PC + PUSH high on this CLK.
                    _pc = (ushort)(_instructionPC + 2);
                    _visiblePC = _pc;
                    Push((byte)(_pc >> 8));
1040→                    return true;
                }

                Push((byte)_pc);
                return true;
            case 1:
                if (nonOverlappedJsr)
                {
                    Push((byte)_pc);
                    return true;
1050→                }

                return false;
            case 0:
                CompleteJsrTargetFetch();
                return true;
            default:
                return false;
        }
    }

### Turn 142 (Assistant)
JSR after LDY# with trail=1 is overlapped on VICE even inside a non-overlapped region; realigning that case.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // Clean first FETCH (full-length branch or sticky non-overlapped region
        // after not-taken hold): VICE keeps PC at JSR through STACK_PEEK before
        // INC_PC+PUSH (c=518797 nPC=$E8B5 mPC=$E8B7 nS=$F2 mS=$F1).
        var nonOverlappedJsr = _afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase;
- new_string:         // Clean first FETCH (full-length branch or sticky non-overlapped region
        // after not-taken hold): VICE keeps PC at JSR through STACK_PEEK before
        // INC_PC+PUSH (c=518797 nPC=$E8B5 mPC=$E8B7 nS=$F2 mS=$F1).
        // Exception: after a 2-byte imm with trail==1 (LDY# then JSR), VICE is
        // already on INC_PC+PUSH at dbgCyc=3 even inside a non-overlapped region
        // (c=541157 nPC=$DE1C nS=$F8 mPC=$DE1A mS=$F9).
        var nonOverlappedJsr = (_afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase)
            && !(DebugPriorTrailingAtNextPc == 1
                && IsTwoByteImmediateOpcode(_previousOpcode));
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
- content: ViceSharp VIC-20 every-cycle lockstep vs xvic - VICE realign session
Date: 2026-08-06
Method: always ask (1) where did we diverge from VICE? (2) how do we get back in line?

Skeptic gaps
------------
1. NotTakenBne / TakenBne: both [Fact(Explicit = true)] BranchCycleCountTests.cs:24,55
2. Focused every-cycle gate: EveryCycle_CpuRegs_Match_FocusedWindow (500_000)
3. Residual honesty: matchedCycles below - NOT multi-second, NOT 1.1e6

Gates (machine-verified)
------------------------
Focused 500k + Via1Iec + Vic20NativeLockstepTests:
  FOCUSED_EXIT=0, Passed 20 / Failed 0
  latest: docs/focused-ror-chain2-2026-08-06.log

2s (VICESHARP_LOCKSTEP_2S=1):
  matchedCycles=541156  DIV first=541157  budget=2216810
  ~0.488 s PAL (541156/1108405) - NOT multi-second, NOT 1.1e6
  receipt: docs/2s-ror-chain2-2026-08-06.log

Progress vs skeptic residual (517828)
-------------------------------------
Skeptic residual:     matchedCycles=517828
Session resume:       matchedCycles=522468
BPL restage start:    matchedCycles=540200
Prior IRQ residual:   matchedCycles=540739
Current residual:     matchedCycles=541156
Net vs skeptic:       +23328 every-cycle matches

VICE realignments this continuation
-----------------------------------
1. Post-JSR IRQ sample: suppress still forces host PC export, but
   _interruptSampleDespiteSuppress opens SystemClock IRQ arming so VICE
   DO_INTERRUPT before callee FETCH is matched (c=540740 nS drop mid-ROR
   was native IRQ while managed ran ROR). PC getter decoupled from
   IsInstructionBoundary so exportPc is preserved on non-overlapped JSR last CLK.
2. Soft-imm skip after staged taken JUMP is a one-instruction window only
   (sticky flag fused CPX# at c=540896).
3. zp/zp,X shift RMW modify phase realigned: cycle 1 after branch / prior
   shift trail==2; cycle 2 after load, RTI, other trail (c=540964, 540970,
   541100, 541105).
4. In progress: JSR after 2-byte imm trail==1 inside nonOvl region needs
   overlapped INC_PC+PUSH at dbgCyc=3 (c=541157).

Current residual (VICE-facing)
------------------------------
Where:
  c=541157 JSR $20 at $DE1A after LDY# (trail=1, nonOvl)
  nPC=$DE1C nS=$F8 mPC=$DE1A mS=$F9

Evidence: docs/2s-ror-chain2-2026-08-06.log

How diverged:
  nonOverlappedJsr forced STACK_PEEK hold at cycle 3 while native already
  INC_PC+PUSH high (overlapped schedule after LD# trail=1).

How to realign:
  Treat trail==1 after 2-byte imm as overlapped JSR even in nonOvl region
  (patch staged; re-run focused+2s).

Not done / do not claim
-----------------------
- Full 2s every-cycle green
- Match past ~1.1e6 cycles
- Multi-second lockstep (still ~0.488 s)


### Turn 143 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt.

### Turn 144 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 145 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-jsr-ldy-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after JSR post-LDY overlap fix
- timeout: 300000

### Turn 146 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 147 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-jsr-ldy-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after JSR post-LDY fix
- timeout: 300000

### Turn 148 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.23]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541160 matchedCycles=541159 budget=2216810
c=541149 nPC=$DE28 mPC=$DE28 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$D0 trail=3 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541150 nPC=$DE16 mPC=$DE16 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541151 nPC=$DE16 mPC=$DE16 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541152 nPC=$DE18 mPC=$DE18 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541153 nPC=$DE18 mPC=$DE18 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541154 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541155 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=5 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541156 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=4 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541157 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F8 mS=$F8 nP=$A0 mP=$A0 dbgCyc=3 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541158 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541159 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541160 nPC=$DC5B mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 149 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:     private void CompleteJsrTargetFetch()
    {
        var source = _instructionPC;
        var lo = Read((ushort)(_instructionPC + 1));
        // _pc is already instructionPC+2 after the push phase (VICE loads MSB here).
        var hi = Read(_pc);
        var target = (ushort)(lo | (hi << 8));
        var returnPc = (ushort)(source + 3);
        var nonOverlappedJsr = _afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase;
        if (nonOverlappedJsr)
        {
            // VICE JUMP runs after the final CLK_INC export, so this cycle still
            // shows the post-INC_PC address; target is live for the next FETCH.
            var exportPc = _pc;
            _pc = target;
            _instructionPC = exportPc;
            _visiblePC = exportPc;
            // Callee starts without first-FETCH overlap.
            _nonOverlappedFetchPhase = true;
        }
        else
        {
            // Overlapped path: host-visible last CLK already matches target
            // (phase coupling with previous instruction).
            PC = target;
        }

        PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        // Keep suppress so host PC export stays on exportPc for non-overlapped
        // JSR last CLK (c=5012 nPC=$FDB7 mPC would jump to target without it).
        _suppressBootstrapBoundary = true;
        // VICE 6510core.c main loop: after JSR JUMP, DO_INTERRUPT runs before
        // FETCH of the callee. Open interrupt sampling despite suppress so
        // SystemClock can arm IRQ on this tick (c=540740 nS=$F2 mS=$F3 when
        // sample was closed: native pushed, managed ran ROR at $D9B0).
        _interruptSampleDespiteSuppress = true;
    }
- new_string:     private void CompleteJsrTargetFetch()
    {
        var source = _instructionPC;
        var lo = Read((ushort)(_instructionPC + 1));
        // _pc is already instructionPC+2 after the push phase (VICE loads MSB here).
        var hi = Read(_pc);
        var target = (ushort)(lo | (hi << 8));
        var returnPc = (ushort)(source + 3);
        // Same non-overlapped predicate as the push-phase switch (trail==1 after
        // 2-byte imm is overlapped end-to-end: c=541160 nPC=$DC5B mPC=$DE1C when
        // Complete used the clean export while pushes used the overlapped path).
        var nonOverlappedJsr = (_afterFullLengthTakenBranch
            || _nonOverlappedRegion
            || _nonOverlappedFetchPhase)
            && !(DebugPriorTrailingAtNextPc == 1
                && IsTwoByteImmediateOpcode(_previousOpcode));
        if (nonOverlappedJsr)
        {
            // VICE JUMP runs after the final CLK_INC export, so this cycle still
            // shows the post-INC_PC address; target is live for the next FETCH.
            var exportPc = _pc;
            _pc = target;
            _instructionPC = exportPc;
            _visiblePC = exportPc;
            // Callee starts without first-FETCH overlap.
            _nonOverlappedFetchPhase = true;
        }
        else
        {
            // Overlapped path: host-visible last CLK already matches target
            // (phase coupling with previous instruction).
            PC = target;
        }

        PublishControlTransfer(source, target, returnPc, 0x20);
        _cycle = 0;
        _callTargetFetchPending = true;
        // Keep suppress so host PC export stays on exportPc for non-overlapped
        // JSR last CLK (c=5012 nPC=$FDB7 mPC would jump to target without it).
        // Overlapped path sets PC=target on all views; suppress is harmless.
        _suppressBootstrapBoundary = true;
        // VICE 6510core.c main loop: after JSR JUMP, DO_INTERRUPT runs before
        // FETCH of the callee. Open interrupt sampling despite suppress so
        // SystemClock can arm IRQ on this tick (c=540740 nS=$F2 mS=$F3 when
        // sample was closed: native pushed, managed ran ROR at $D9B0).
        _interruptSampleDespiteSuppress = true;
    }

### Turn 150 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 151 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-jsr-jump-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after JSR JUMP export align
- timeout: 300000

### Turn 152 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 153 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-jsr-jump-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after JSR JUMP export fix
- timeout: 300000

### Turn 154 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.32]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541162 matchedCycles=541161 budget=2216810
c=541151 nPC=$DE16 mPC=$DE16 nA=$17 mA=$17 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541152 nPC=$DE18 mPC=$DE18 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A9 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541153 nPC=$DE18 mPC=$DE18 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541154 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541155 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=5 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541156 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=4 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541157 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F8 mS=$F8 nP=$A0 mP=$A0 dbgCyc=3 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541158 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541159 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541160 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541161 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541162 nPC=$DC5D mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 155 (Assistant)
[tool_request: grep]
- pattern: 0x85|STA zp|IsZeroPageStore|AdvanceVisiblePc|store.*Pc
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1345
- limit: 80

### Turn 156 (Function)
[tool_response]
1345→                    && DebugPriorTrailingAtNextPc == 2);
            if ((_cycle == 1 && commitAtCycle1) || (_cycle == 2 && !commitAtCycle1))
            {
                CommitZeroPageShiftRmwModifyAndFlags();
                AdvanceVisiblePc(2);
1350→                return true;
            }
        }

        if (_cycle == 4 && IsIndirectYStoreOpcode(_opcode))
        {
            // Non-overlapped path after soft LDA #: hold opcode PC one more CLK
            // so host-visible matches VICE (2 FETCH at start before INC_PC).
            if (_holdIndYStorePcOneCycle)
            {
1360→                _holdIndYStorePcOneCycle = false;
                _softDeferCompareCommit = true;
                _visiblePC = _opcodeAddress;
                return true;
            }

            AdvanceVisiblePc(2);
            return true;
        }

1370→        if (_cycle == 3 && IsIndirectYStoreOpcode(_opcode) && _visiblePC == _opcodeAddress)
        {
            // Complete the deferred advance after the hold tick.
            AdvanceVisiblePc(2);
            return true;
        }

        if (IsStagedAbsoluteRmwOpcode(_opcode) && TryExecuteStagedAbsoluteRmwCycle())
        {
            return true;
1380→        }

        if (_cycle == 2 && IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            if (_deferIndexedStorePcAdvanceAfterBranch)
            {
                _deferIndexedStorePcAdvanceAfterBranch = false;
                _indexedStorePcAdvanceWasDeferred = true;
                return false;
            }
1390→
            // Non-overlapped clean first FETCH: VICE ST does INC_PC only after
            // both FETCH CLKs (6510dtvcore ST + SET_ABS_*). Hold opcode PC here;
            // FinishStagedMemoryWrite on cycle 1 makes the advance host-visible.
            if (_nonOverlappedRegion || _nonOverlappedFetchPhase)
            {
                _visiblePC = _opcodeAddress;
                return true;
            }

1400→            AdvanceVisiblePc(3);
            return true;
        }

        if (_cycle == 2 && IsZeroPageIndexedStoreOpcode(_opcode))
        {
            if (_deferZeroPageIndexedStorePcAdvanceAfterBranch)
            {
                _deferZeroPageIndexedStorePcAdvanceAfterBranch = false;
                _indexedStorePcAdvanceWasDeferred = true;
1410→                return false;
            }

            // Non-overlapped STY/STA/STX zp,index (c=518814 nPC=$E568 mPC=$E56A):
            // VICE SET_ZERO_X runs after FETCH; INC_PC is not visible on the
            // second host sample. Hold opcode PC; cycle-1 write advances it.
            if (_nonOverlappedRegion || _nonOverlappedFetchPhase)
            {
                _visiblePC = _opcodeAddress;
                return true;
1420→            }

            AdvanceVisiblePc(2);
            return true;
        }

### Turn 157 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 19 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
384:    /// PC for one extra host CLK before AdvanceVisiblePc (VICE: 2 FETCH at start,
625:            //   dwell 3 (e.g. zp INC with early AdvanceVisiblePc): 2 host steps
766:            //  - full-length trail=2 after store (c=540337 BCC after STA zp)
1317:            AdvanceVisiblePc(2);
1327:            AdvanceVisiblePc(2);
1349:                AdvanceVisiblePc(2);
1366:            AdvanceVisiblePc(2);
1373:            AdvanceVisiblePc(2);
1400:            AdvanceVisiblePc(3);
1422:            AdvanceVisiblePc(2);
1484:            case 0x85:
1601:                AdvanceVisiblePc(2);
1885:                // before AdvanceVisiblePc (cycle 20756).
2159:            || opcode is 0x8D or 0x8C or 0x85 or 0x86 or 0x84
2164:    private void AdvanceVisiblePc(int instructionLength)
2464:                AdvanceVisiblePc(3);
2503:        // F2 with opcode PC (c=519274 STA zp). Indexed stores (zp,X / abs,X)
2695:            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
2707:            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
</workspace_result>

### Turn 158 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1475
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2485
- limit: 50
[tool_request: grep]
- pattern: FinishStagedMemoryWrite|STA zp|0x85|zero.?page.?store
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- glob: **/*

### Turn 159 (Function)
[tool_response]
1475→            }
            case 0xB4:
            {
                var yZpx = Read((byte)(ReadZeroPageOperand() + X));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
1480→                    Y = yZpx;
                FinishStagedMemoryRead(2, yZpx);
                return true;
            }
            case 0x85:
                _bus.Write(ReadZeroPageOperand(), A);
                FinishStagedMemoryWrite(2);
                return true;
            case 0x86:
                _bus.Write(ReadZeroPageOperand(), X);
1490→                FinishStagedMemoryWrite(2);
                return true;
            case 0x96:
                _bus.Write((byte)(ReadZeroPageOperand() + Y), X);
                FinishStagedMemoryWrite(2);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0x84:
                _bus.Write(ReadZeroPageOperand(), Y);
                FinishStagedMemoryWrite(2);
1500→                return true;
            case 0x95:
                _bus.Write((byte)(ReadZeroPageOperand() + X), A);
                FinishStagedMemoryWrite(2);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
                return true;
            case 0x94:
                _bus.Write((byte)(ReadZeroPageOperand() + X), Y);
                FinishStagedMemoryWrite(2);
                DelayNextFetchAfterDeferredIndexedStorePcAdvance();
1510→                return true;
            case 0x2C when _bitSoftDeferEarly:
            case 0x24 when _bitSoftDeferEarly:
                SoftDeferBitBody();
                return true;

### Turn 160 (Function)
[tool_response]
2485→    /// Completes a staged stack push (PHA/PHP; TR-LOCKSTEP-VSF-001): the push
    /// itself just executed on this tick (native exports the decremented S at
    /// the push cycle, 6510dtvcore.c:1354-1366 PUSH + CLK_INC), while the PC
    /// advance (INC_PC after that CLK_INC) only becomes visible on the next
    /// cycle via the staged-completed apply path.
2490→    /// </summary>
    private void FinishStagedStackPush()
    {
        _pc = (ushort)(_instructionPC + 1);
        _visiblePC = _instructionPC;
        _stagedMemoryReadCompleted = true;
    }

    private void FinishStagedMemoryWrite(int instructionLength)
    {
2500→        _pc = (ushort)(_instructionPC + instructionLength);
        // Non-overlapped plain (non-indexed) stores: VICE ST runs INC_PC after
        // both FETCH CLKs, so the managed write sample (2nd of 3) still matches
        // F2 with opcode PC (c=519274 STA zp). Indexed stores (zp,X / abs,X)
        // already held through their extra dummy CLK; on the write sample VICE
        // has advanced (c=518815 STY zp,X nPC=next) so export next-PC here.
        if ((_nonOverlappedRegion || _nonOverlappedFetchPhase)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
2510→            && !IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
            _stagedMemoryReadCompleted = true;
            return;
        }

        _visiblePC = _pc;
        // Lag-shaped shortcut: collapse into the next RTS early. Skip for the
2520→        // whole non-overlapped subroutine region so STA keeps its full VICE
        // cycle budget before RTS (cycle 5048 stack lag).
        if (Peek(_pc) == 0x60 && !_nonOverlappedRegion)
        {
            _cycle = 0;
            return;
        }

        _stagedMemoryReadCompleted = true;
    }
2530→
    private void DelayNextFetchAfterMappedIoWrite(ushort address)
    {
        if (ShouldDelayNextFetchAfterWrite?.Invoke(address) != true)
        {

### Turn 161 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 25 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
766:            //  - full-length trail=2 after store (c=540337 BCC after STA zp)
1393:            // FinishStagedMemoryWrite on cycle 1 makes the advance host-visible.
1484:            case 0x85:
1486:                FinishStagedMemoryWrite(2);
1490:                FinishStagedMemoryWrite(2);
1494:                FinishStagedMemoryWrite(2);
1499:                FinishStagedMemoryWrite(2);
1503:                FinishStagedMemoryWrite(2);
1508:                FinishStagedMemoryWrite(2);
1542:                FinishStagedMemoryWrite(3);
1552:                FinishStagedMemoryWrite(3);
1556:                FinishStagedMemoryWrite(3);
1560:                FinishStagedMemoryWrite(3);
1565:                FinishStagedMemoryWrite(3);
1575:                    FinishStagedMemoryWrite(2);
1713:                FinishStagedMemoryWrite(2);
1779:                    // Same RTS prefetch convention as FinishStagedMemoryWrite:
2159:            || opcode is 0x8D or 0x8C or 0x85 or 0x86 or 0x84
2284:        FinishStagedMemoryWrite(instructionLength);
2292:        FinishStagedMemoryWrite(instructionLength);
2470:                    // Same RTS prefetch convention as FinishStagedMemoryWrite:
2498:    private void FinishStagedMemoryWrite(int instructionLength)
2503:        // F2 with opcode PC (c=519274 STA zp). Indexed stores (zp,X / abs,X)
2695:            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
2707:            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
</workspace_result>

### Turn 162 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:     private void FinishStagedMemoryWrite(int instructionLength)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        // Non-overlapped plain (non-indexed) stores: VICE ST runs INC_PC after
        // both FETCH CLKs, so the managed write sample (2nd of 3) still matches
        // F2 with opcode PC (c=519274 STA zp). Indexed stores (zp,X / abs,X)
        // already held through their extra dummy CLK; on the write sample VICE
        // has advanced (c=518815 STY zp,X nPC=next) so export next-PC here.
        if ((_nonOverlappedRegion || _nonOverlappedFetchPhase)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
            && !IsIndexedAbsoluteStoreOpcode(_opcode))
        {
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
            _stagedMemoryReadCompleted = true;
            return;
        }
- new_string:     private void FinishStagedMemoryWrite(int instructionLength)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        // Non-overlapped plain (non-indexed) stores: VICE ST runs INC_PC after
        // both FETCH CLKs, so the managed write sample (2nd of 3) still matches
        // F2 with opcode PC (c=519274 STA zp). Indexed stores (zp,X / abs,X)
        // already held through their extra dummy CLK; on the write sample VICE
        // has advanced (c=518815 STY zp,X nPC=next) so export next-PC here.
        // After overlapped JSR entry with long prior trail (trail>=3), VICE has
        // already advanced by the STA write sample (c=541162 nPC=$DC5D
        // mPC=$DC5B when hold still forced opcode PC).
        if ((_nonOverlappedRegion || _nonOverlappedFetchPhase)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
            && !IsIndexedAbsoluteStoreOpcode(_opcode)
            && DebugPriorTrailingAtNextPc < 3)
        {
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
            _stagedMemoryReadCompleted = true;
            return;
        }

### Turn 163 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 164 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-sta-trail-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after STA post-JSR trail fix
- timeout: 300000

### Turn 165 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 166 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-sta-trail-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after STA post-JSR fix
- timeout: 300000

### Turn 167 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.25]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541165 matchedCycles=541164 budget=2216810
c=541154 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=0 op=$A0 trail=1 nonOvlR=False nonOvlF=False dly=False stg=False irq=0 mis=False
c=541155 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=5 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541156 nPC=$DE1A mPC=$DE1A nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F9 mS=$F9 nP=$A0 mP=$A0 dbgCyc=4 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541157 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F8 mS=$F8 nP=$A0 mP=$A0 dbgCyc=3 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541158 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541159 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541160 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541161 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541162 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541163 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541164 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541165 nPC=$DC5F mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 168 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         // After overlapped JSR entry with long prior trail (trail>=3), VICE has
        // already advanced by the STA write sample (c=541162 nPC=$DC5D
        // mPC=$DC5B when hold still forced opcode PC).
        if ((_nonOverlappedRegion || _nonOverlappedFetchPhase)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
            && !IsIndexedAbsoluteStoreOpcode(_opcode)
            && DebugPriorTrailingAtNextPc < 3)
        {
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
            _stagedMemoryReadCompleted = true;
            return;
        }
- new_string:         // After overlapped JSR entry / store chain with trail>=2, VICE has
        // already advanced by the write sample (c=541162 STA trail=4;
        // c=541165 STY trail=2 nPC=next mPC=opcode when hold forced opcode).
        // Keep hold only for short-trail clean nonOvl (c=519274 STA zp).
        if ((_nonOverlappedRegion || _nonOverlappedFetchPhase)
            && IsStoreOpcode(_opcode)
            && _visiblePC == _opcodeAddress
            && !IsZeroPageIndexedStoreOpcode(_opcode)
            && !IsIndexedAbsoluteStoreOpcode(_opcode)
            && DebugPriorTrailingAtNextPc <= 1)
        {
            _visiblePC = _opcodeAddress;
            _suppressBootstrapBoundary = true;
            _stagedMemoryReadCompleted = true;
            return;
        }

### Turn 169 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 170 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-sty-trail-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after STY trail store fix
- timeout: 300000

### Turn 171 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 172 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-sty-trail-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after STY trail fix
- timeout: 300000

### Turn 173 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.37]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541168 matchedCycles=541167 budget=2216810
c=541157 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F8 mS=$F8 nP=$A0 mP=$A0 dbgCyc=3 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541158 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541159 nPC=$DE1C mPC=$DE1C nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541160 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$20 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541161 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541162 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541163 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541164 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541165 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541166 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541167 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541168 nPC=$DC61 mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$DD nS=$F7 mS=$F7 nP=$22 mP=$A0 dbgCyc=0 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 174 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#). That skip is a ONE-instruction window only:
            // sticky skip fused CPX# much later (c=540896 nPC=$EB2D mPC=$EB2F).
            var skipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch;
            _skipSoftImmAfterStagedTakenBranch = false;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                if (skipSoftImmThisInsn && !IsImmediateLoadOpcode(_opcode))
                {
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }
- new_string:             // After staged multi-cycle taken branch JUMP, ALU imm is fused on
            // VICE (c=540204 ADC#). That skip is a ONE-instruction window only:
            // sticky skip fused CPX# much later (c=540896 nPC=$EB2D mPC=$EB2F).
            // Immediate loads with priorTrailing>=2 after store chain also fuse
            // (c=541168 LDY# nY=$00 mY still pre-load when soft-deferred).
            var skipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch;
            _skipSoftImmAfterStagedTakenBranch = false;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                var fuseImmLoadAfterTrail =
                    IsImmediateLoadOpcode(_opcode) && DebugPriorTrailingAtNextPc >= 2;
                if ((skipSoftImmThisInsn && !IsImmediateLoadOpcode(_opcode))
                    || fuseImmLoadAfterTrail)
                {
                    // fall through to ExecuteOpcode (fused commit)
                }
                else
                {
                    if (IsImmediateLoadOpcode(_opcode))
                    {
                        _softDeferredImmediateLoad = true;
                        // _pc already past opcode; CompleteDeferredImmediateLoad will
                        // set it to instructionPC+2 when soft-applied at next fetch.
                    }
                    else
                    {
                        _softDeferredImpliedOp = true;
                        _softDeferredImpliedOpcode = _opcode;
                        _softDeferredImpliedInstructionPc = _opcodeAddress;
                    }

                    _holdIndYStorePcOneCycle = true;
                    _visiblePC = _opcodeAddress;
                    _suppressBootstrapBoundary = true;
                    _nonOverlappedFetchPhase = true;
                    return;
                }
            }

### Turn 175 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 176 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-ldy-fuse-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after LDY fuse trail fix
- timeout: 300000

### Turn 177 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 178 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-ldy-fuse-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after LDY fuse fix
- timeout: 300000

### Turn 179 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.14]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=519253 matchedCycles=519252 budget=2216810
c=519242 nPC=$E571 mPC=$E571 nA=$10 mA=$10 nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$31 mP=$31 dbgCyc=1 op=$E0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519243 nPC=$E571 mPC=$E571 nA=$10 mA=$10 nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$31 mP=$31 dbgCyc=0 op=$E0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519244 nPC=$E573 mPC=$E573 nA=$10 mA=$10 nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$33 mP=$33 dbgCyc=1 op=$D0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519245 nPC=$E573 mPC=$E573 nA=$10 mA=$10 nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$33 mP=$33 dbgCyc=0 op=$D0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519246 nPC=$E575 mPC=$E575 nA=$10 mA=$10 nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$33 mP=$33 dbgCyc=1 op=$A9 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519247 nPC=$E575 mPC=$E575 nA=$10 mA=$10 nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$33 mP=$33 dbgCyc=0 op=$A9 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519248 nPC=$E577 mPC=$E577 nA=$FF mA=$FF nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$B1 mP=$B1 dbgCyc=3 op=$95 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519249 nPC=$E577 mPC=$E577 nA=$FF mA=$FF nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$B1 mP=$B1 dbgCyc=2 op=$95 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519250 nPC=$E579 mPC=$E579 nA=$FF mA=$FF nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$B1 mP=$B1 dbgCyc=1 op=$95 trail=0 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=519251 nPC=$E579 mPC=$E579 nA=$FF mA=$FF nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$B1 mP=$B1 dbgCyc=0 op=$95 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519252 nPC=$E579 mPC=$E579 nA=$FF mA=$FF nX=$18 mX=$18 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$B1 mP=$B1 dbgCyc=1 op=$A2 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=519253 nPC=$E579 mPC=$E57B nA=$FF mA=$FF nX=$18 mX=$16 nY=$A0 mY=$A0 nS=$F0 mS=$F0 nP=$B1 mP=$31 dbgCyc=0 op=$A2 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 180 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             // Immediate loads with priorTrailing>=2 after store chain also fuse
            // (c=541168 LDY# nY=$00 mY still pre-load when soft-deferred).
            var skipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch;
            _skipSoftImmAfterStagedTakenBranch = false;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                var fuseImmLoadAfterTrail =
                    IsImmediateLoadOpcode(_opcode) && DebugPriorTrailingAtNextPc >= 2;
                if ((skipSoftImmThisInsn && !IsImmediateLoadOpcode(_opcode))
                    || fuseImmLoadAfterTrail)
                {
                    // fall through to ExecuteOpcode (fused commit)
                }
- new_string:             // Immediate loads with priorTrailing>=2 after plain (non-indexed)
            // store fuse (c=541168 LDY# after STY zp). After indexed store
            // trail=2 still softs (c=519253 LDX# after STA zp,X nX pre-load).
            var skipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch;
            _skipSoftImmAfterStagedTakenBranch = false;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsTwoByteImmediateOpcode(_opcode))
            {
                var fuseImmLoadAfterPlainStore =
                    IsImmediateLoadOpcode(_opcode)
                    && DebugPriorTrailingAtNextPc >= 2
                    && IsStoreOpcode(_previousOpcode)
                    && !IsZeroPageIndexedStoreOpcode(_previousOpcode)
                    && !IsIndexedAbsoluteStoreOpcode(_previousOpcode);
                if ((skipSoftImmThisInsn && !IsImmediateLoadOpcode(_opcode))
                    || fuseImmLoadAfterPlainStore)
                {
                    // fall through to ExecuteOpcode (fused commit)
                }

### Turn 181 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 182 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2150
- limit: 30
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-ldy-plain-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after plain-store LDY fuse narrow
- timeout: 300000

### Turn 183 (Function)
[tool_response]
2150→    /// staged compare and absolute-RMW families, control transfers (JMP abs /
    /// JMP ind and the branch family itself, so chained taken branches keep
    /// the 3-cycle cost), the staged zp/abs loads and stores, the staged stack
    /// pushes, the staged indexed loads LDY abs,X / LDX abs,Y, CMP (zp),Y and
    /// the 2-cycle immediate ALU family. Excluded: classes with a dedicated
    /// after-branch defer path (immediate loads A0/A2/A9, implied register
    /// ops, JSR, LDA abs,X / abs,Y / (zp),Y, zp INC/DEC, indexed stores),
    /// STX abs (0x8E, whose ShouldDeferAbsoluteStore hook already reroutes
    /// I/O stores to the unstaged path with correct after-branch timing), and
    /// the multi-cycle stack ops (RTS/RTI/PLA/PLP/BRK) whose staged offsets
2160→    /// encode their own measured native timing.
    /// </summary>
    private static bool IsAfterBranchBudgetExtendedOpcode(byte opcode)
    {
        return IsStagedCompareOpcode(opcode)
            || IsStagedAbsoluteRmwOpcode(opcode)
            || IsBranchOpcode(opcode)
            || opcode is 0x4C or 0x6C or 0xBC or 0xBE or 0xD1
            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0
            || opcode is 0x8D or 0x8C or 0x85 or 0x86 or 0x84
2170→            || opcode is 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4 or 0xAD or 0xAE or 0xAC
            || opcode is 0x48 or 0x08;
    }

    private void AdvanceVisiblePc(int instructionLength)
    {
        _pc = (ushort)(_instructionPC + instructionLength);
        _visiblePC = _pc;
        // Trailing dwell at next-PC is counted at end of Tick while
        // _visiblePC != _instructionPC (instructionPC stays at the opcode).

### Turn 184 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 185 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-ldy-plain-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s after narrowed LDY plain-store fuse
- timeout: 300000

### Turn 186 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.28]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541172 matchedCycles=541171 budget=2216810
c=541161 nPC=$DC5B mPC=$DC5B nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541162 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541163 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541164 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541165 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541166 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541167 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541168 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=0 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541169 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541170 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541171 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541172 nPC=$DC61 mPC=$DC61 nA=$9B mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 187 (Assistant)
[tool_request: grep]
- pattern: 0xB1|IndirectYLoad|deferIndirectY
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1430
- limit: 50

### Turn 188 (Function)
[tool_response]
1430→            }

            AdvanceVisiblePc(2);
            return true;
        }

        if (_cycle != 1)
        {
            return false;
        }
1440→
        switch (_opcode)
        {
            case 0xA5:
            {
                var aZp = Read(ReadZeroPageOperand());
                // Non-overlapped: VICE exports pre-load A on the data-read CLK
                // (same shape as LDA (zp),Y). Overlapped path commits now.
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    A = aZp;
1450→                FinishStagedMemoryRead(2, aZp);
                return true;
            }
            case 0xA6:
            {
                var xZp = Read(ReadZeroPageOperand());
                // c=518761: non-overlapped LDX zp had mX early vs nX.
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = xZp;
                FinishStagedMemoryRead(2, xZp);
1460→                return true;
            }
            case 0xA4:
            {
                var yZp = Read(ReadZeroPageOperand());
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    Y = yZp;
                FinishStagedMemoryRead(2, yZp);
                return true;
            }
1470→            case 0xB5:
            {
                var aZpx = Read((byte)(ReadZeroPageOperand() + X));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    A = aZpx;
                FinishStagedMemoryRead(2, aZpx);
                return true;
            }
            case 0xB6:
            {

### Turn 189 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 25 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
294:    private bool _deferIndirectYLoadCompletionAfterBranch;
296:    private bool _deferNextIndirectYLoadAfterBranchRmw;
465:            if (!(_stagedMemoryReadCompleted && (_opcode is 0xB1 or 0xD1)))
670:            var deferIndirectYLoadAfterBranchRmw = _deferNextIndirectYLoadAfterBranchRmw && IsIndirectYLoadOpcode(_opcode);
671:            _deferNextIndirectYLoadAfterBranchRmw = false;
751:            _deferIndirectYLoadCompletionAfterBranch =
752:                ((armAfterBranchLag || _afterFullLengthTakenBranch) && IsIndirectYLoadOpcode(_opcode))
753:                || deferIndirectYLoadAfterBranchRmw;
1183:            if (_deferIndirectYLoadCompletionAfterBranch)
1186:                _deferIndirectYLoadCompletionAfterBranch = false;
1222:                if (_opcode is 0xB1)
1259:                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
1271:            else if (_opcode is 0xB1)
1278:                && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
1309:                _deferNextIndirectYLoadAfterBranchRmw = true;
1670:            case 0xB1:
1674:                if (!_deferIndirectYLoadCompletionAfterBranch
1995:        _deferIndirectYLoadCompletionAfterBranch = false;
1997:        _deferNextIndirectYLoadAfterBranchRmw = false;
2125:    private static bool IsIndirectYLoadOpcode(byte opcode)
2127:        return opcode is 0xB1;
2216:            or 0xA1 or 0xB1;
2643:        _deferIndirectYLoadCompletionAfterBranch = false;
2645:        _deferNextIndirectYLoadAfterBranchRmw = false;
2689:            0xA9 or 0xA5 or 0xB5 or 0xAD or 0xBD or 0xB9 or 0xA1 or 0xB1 or
</workspace_result>

### Turn 190 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1170
- limit: 120
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1660
- limit: 30

### Turn 191 (Function)
[tool_response]
1170→                return true;
            }

            if (_deferAbsoluteYLoadCompletionAfterBranch)
            {
                A = _stagedNzValue;
                _deferAbsoluteYLoadCompletionAfterBranch = false;
                _pendingDeferredNzUpdateAfterBranch = true;
                _stagedMemoryReadCompleted = false;
                _suppressBootstrapBoundary = true;
1180→                return true;
            }

            if (_deferIndirectYLoadCompletionAfterBranch)
            {
                A = _stagedNzValue;
                _deferIndirectYLoadCompletionAfterBranch = false;
                _pendingDeferredNzUpdateAfterBranch = true;
                _stagedMemoryReadCompleted = false;
                _suppressBootstrapBoundary = true;
1190→                return true;
            }

            // VICE CP: flags + INC_PC after the final data-read CLK export. On the
            // non-overlapped path after STA (zp),Y, keep old flags and opcode PC
            // this tick; soft-commit at the next instruction's first FETCH.
            if (_softDeferCompareCommit && IsStagedCompareOpcode(_opcode))
            {
                _softDeferCompareCommit = false;
                _pendingSoftCompareCommit = true;
1200→                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _stagedMemoryReadCompleted = false;
                return true;
            }

            if (_stagedCarryUpdate)
            {
                if (_stagedCarryValue)
                    P |= 0x01;
1210→                else
                    P &= 0xFE;

                _stagedCarryUpdate = false;
            }

            if (_stagedNzUpdate)
            {
                // LDA (zp),Y: commit A on this final apply tick. Re-read the
                // effective address so open-bus/last-data matches the native
1220→                // sample on this CLK (staged mid-instr read can disagree when
                // bus latch timing differs).
                if (_opcode is 0xB1)
                {
                    A = Read(ReadIndirectYOperand());
                    _stagedNzValue = A;
                }

                // Non-overlapped zp/abs/indexed loads: commit register on apply
                // tick (data-read CLK still showed the pre-load register).
1230→                if (_nonOverlappedFetchPhase || _nonOverlappedRegion)
                {
                    switch (_opcode)
                    {
                        case 0xA5:
                        case 0xB5:
                        case 0xAD:
                        case 0xBD:
                        case 0xB9:
                            A = _stagedNzValue;
1240→                            break;
                        case 0xA6:
                        case 0xB6:
                        case 0xAE:
                        case 0xBE:
                            X = _stagedNzValue;
                            break;
                        case 0xA4:
                        case 0xB4:
                        case 0xAC:
1250→                        case 0xBC:
                            Y = _stagedNzValue;
                            break;
                    }
                }

                // Non-overlapped loads: host may still sample pre-op P on the
                // data-read / apply CLK. Soft-defer NZ (reg already committed).
                if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
1260→                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                {
                    _pendingSoftCompareCommit = true;
                    _stagedCarryUpdate = false;
                }
                else
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }
1270→            }
            else if (_opcode is 0xB1)
            {
                A = Read(ReadIndirectYOperand());
                UpdateNZ(A);
            }

            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                    or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
1280→            {
                // Keep opcode PC this tick; soft NZ/PC commit on next FETCH.
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
            }
            else
            {
                _instructionPC = _pc;
                _visiblePC = _pc;
            }

### Turn 192 (Function)
[tool_response]
1660→                if (TryDelayIndexedLoadPageCross(ldxBase, Y))
                {
                    return true;
                }

                var ldxAbsY = Read((ushort)(ldxBase + Y));
                if (!(_nonOverlappedFetchPhase || _nonOverlappedRegion))
                    X = ldxAbsY;
                FinishStagedMemoryRead(3, ldxAbsY);
                return true;
1670→            case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                if (!_deferIndirectYLoadCompletionAfterBranch
                    && !_nonOverlappedFetchPhase && !_nonOverlappedRegion)
                    A = indirectYValue;
                FinishStagedMemoryRead(2, indirectYValue);
                return true;
            case 0xCD:
1680→                CompareStagedMemory(ReadAbsoluteOperand(), 3);
                return true;
            case 0xD1:
                CompareStagedMemory(ReadIndirectYOperand(), 2);
                return true;
            case 0x48:
                Push(A);
                FinishStagedStackPush();
                return true;
            case 0x08:

### Turn 193 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                if (!_deferIndirectYLoadCompletionAfterBranch
                    && !_nonOverlappedFetchPhase && !_nonOverlappedRegion)
                    A = indirectYValue;
                FinishStagedMemoryRead(2, indirectYValue);
                return true;
- new_string:             case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                // After fused LDY#/LDA# with trail==1, VICE already shows loaded A
                // on the data-read sample (c=541172 nA=$9B mA=$B3 when deferred).
                var indyCommitAOnDataRead = !_deferIndirectYLoadCompletionAfterBranch
                    && (!_nonOverlappedFetchPhase && !_nonOverlappedRegion
                        || (IsImmediateLoadOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 1));
                if (indyCommitAOnDataRead)
                    A = indirectYValue;
                FinishStagedMemoryRead(2, indirectYValue);
                return true;

### Turn 194 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 195 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-lda-indy-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after LDA (zp),Y commit fix
- timeout: 300000

### Turn 196 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 197 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-lda-indy-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after LDA indy fix
- timeout: 300000

### Turn 198 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.19]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541173 matchedCycles=541172 budget=2216810
c=541162 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541163 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$85 trail=4 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541164 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541165 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541166 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541167 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541168 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=0 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541169 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541170 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541171 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541172 nPC=$DC61 mPC=$DC61 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541173 nPC=$DC63 mPC=$DC61 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$22 dbgCyc=0 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 199 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:                 // Non-overlapped loads: host may still sample pre-op P on the
                // data-read / apply CLK. Soft-defer NZ (reg already committed).
                if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                {
                    _pendingSoftCompareCommit = true;
                    _stagedCarryUpdate = false;
                }
                else
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }
            }
            else if (_opcode is 0xB1)
            {
                A = Read(ReadIndirectYOperand());
                UpdateNZ(A);
            }

            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                    or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
            {
                // Keep opcode PC this tick; soft NZ/PC commit on next FETCH.
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
            }
            else
            {
                _instructionPC = _pc;
                _visiblePC = _pc;
            }
- new_string:                 // Non-overlapped loads: host may still sample pre-op P on the
                // data-read / apply CLK. Soft-defer NZ (reg already committed).
                // Exception: LDA (zp),Y after fused LDY# trail==1 already showed
                // loaded A on data-read; apply CLK must also pair NZ+PC with VICE
                // (c=541173 nPC=$DC63 nP=$A0 mPC=opcode mP pre-load).
                var indyFusedAfterImmLoad = _opcode is 0xB1
                    && IsImmediateLoadOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1;
                if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && !indyFusedAfterImmLoad
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                {
                    _pendingSoftCompareCommit = true;
                    _stagedCarryUpdate = false;
                }
                else
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }
            }
            else if (_opcode is 0xB1)
            {
                A = Read(ReadIndirectYOperand());
                UpdateNZ(A);
            }

            var indyFusedPc = _opcode is 0xB1
                && IsImmediateLoadOpcode(_previousOpcode)
                && DebugPriorTrailingAtNextPc == 1;
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && !indyFusedPc
                && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                    or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
            {
                // Keep opcode PC this tick; soft NZ/PC commit on next FETCH.
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
            }
            else
            {
                _instructionPC = _pc;
                _visiblePC = _pc;
            }

### Turn 200 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 201 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-indy-nz-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after indy NZ/PC fuse fix
- timeout: 300000

### Turn 202 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 203 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-indy-nz-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after indy NZ/PC fix
- timeout: 300000

### Turn 204 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.25]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541175 matchedCycles=541174 budget=2216810
c=541164 nPC=$DC5D mPC=$DC5D nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541165 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541166 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541167 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541168 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=0 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541169 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541170 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541171 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541172 nPC=$DC61 mPC=$DC61 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541173 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541174 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541175 nPC=$DC64 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$00 nS=$F7 mS=$F7 nP=$20 mP=$A0 dbgCyc=0 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 205 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
            // FETCH is not overlapped so host samples the pre-op P/regs.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }
- new_string:             // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
            // FETCH is not overlapped so host samples the pre-op P/regs.
            // After fused LDA (zp),Y (trail==1) VICE already commits INY on the
            // final CLK (c=541175 nY=$01 mY=$00 when soft-deferred).
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode)
                && !(_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
- content: ViceSharp VIC-20 every-cycle lockstep vs xvic - VICE realign session
Date: 2026-08-06
Method: always ask (1) where did we diverge from VICE? (2) how do we get back in line?

Skeptic gaps
------------
1. NotTakenBne / TakenBne: both [Fact(Explicit = true)] BranchCycleCountTests.cs:24,55
2. Focused every-cycle gate: EveryCycle_CpuRegs_Match_FocusedWindow (500_000)
3. Residual honesty: matchedCycles below - NOT multi-second, NOT 1.1e6

Gates (machine-verified)
------------------------
Focused 500k + Via1Iec + Vic20NativeLockstepTests:
  FOCUSED_EXIT=0, Passed 20 / Failed 0
  latest: docs/focused-indy-nz-2026-08-06.log

2s (VICESHARP_LOCKSTEP_2S=1):
  matchedCycles=541174  DIV first=541175  budget=2216810
  ~0.488 s PAL (541174/1108405) - NOT multi-second, NOT 1.1e6
  receipt: docs/2s-indy-nz-2026-08-06.log
  (INY fuse after B1 staged; re-run pending)

Progress vs skeptic residual (517828)
-------------------------------------
Skeptic residual:     matchedCycles=517828
Session resume:       matchedCycles=522468
Prior IRQ residual:   matchedCycles=540739
Current residual:     matchedCycles=541174
Net vs skeptic:       +23346 every-cycle matches
Net vs IRQ residual:  +435 every-cycle matches this continuation

VICE realignments this continuation
-----------------------------------
1. Post-JSR IRQ sample: _interruptSampleDespiteSuppress opens SystemClock
   IRQ arming while suppress still forces host PC export (VICE DO_INTERRUPT
   before callee FETCH; c=540740 nS drop was native IRQ mid-ROR).
2. Soft-imm skip after staged taken JUMP is one-instruction only.
3. zp/zp,X shift RMW modify phase: cycle 1 after branch / prior shift
   trail==2; cycle 2 default (load, RTI, other trail).
4. JSR after 2-byte imm trail==1: overlapped push+JUMP even in nonOvl.
5. Plain store write sample advances PC when trail>=2; plain-store then
   LDY# fuses; indexed-store then LDX# still softs.
6. LDA (zp),Y after fused LDY# trail==1: commit A on data-read and NZ+PC
   on apply (c=541172/541173).
7. In progress: INY after that path must fuse (c=541175).

Current residual (VICE-facing)
------------------------------
Where:
  c=541175 INY $C8 at $DC63 after LDA (zp),Y
  nY=$01 mY=$00 nPC=$DC64 mPC=$DC63 nP=$20 mP=$A0

Evidence: docs/2s-indy-nz-2026-08-06.log

How diverged:
  Soft-deferred implied in nonOvl while VICE fused INY after fused B1.

How to realign:
  Skip soft-implied when previous is B1 with trail==1 (patch staged).

Not done / do not claim
-----------------------
- Full 2s every-cycle green
- Match past ~1.1e6 cycles
- Multi-second lockstep (still ~0.488 s)


### Turn 206 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt.

### Turn 207 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 208 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-iny-fuse-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after INY fuse fix
- timeout: 300000

### Turn 209 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 210 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-iny-fuse-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after INY fuse fix
- timeout: 300000

### Turn 211 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.13]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541177 matchedCycles=541176 budget=2216810
c=541166 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$84 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541167 nPC=$DC5F mPC=$DC5F nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$DD mY=$DD nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541168 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=0 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541169 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541170 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541171 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541172 nPC=$DC61 mPC=$DC61 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541173 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541174 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541175 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=0 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541176 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=1 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541177 nPC=$DC65 mPC=$DC64 nA=$9B mA=$9B nX=$9B mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$20 dbgCyc=0 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 212 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:     /// <summary>
    /// After staged multi-cycle taken JUMP, next non-load 2-byte imm is fused
    /// (c=540204 ADC#); skip one soft-defer.
    /// </summary>
    private bool _skipSoftImmAfterStagedTakenBranch;
- new_string:     /// <summary>
    /// After staged multi-cycle taken JUMP, next non-load 2-byte imm is fused
    /// (c=540204 ADC#); skip one soft-defer.
    /// </summary>
    private bool _skipSoftImmAfterStagedTakenBranch;
    /// <summary>
    /// After fused LDA (zp),Y then fused INY/TAX chain in nonOvl, keep fusing
    /// implied ops until a non-implied is fetched (c=541175 INY, c=541177 TAX).
    /// </summary>
    private bool _fuseImpliedAfterIndyLoad;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
            // FETCH is not overlapped so host samples the pre-op P/regs.
            // After fused LDA (zp),Y (trail==1) VICE already commits INY on the
            // final CLK (c=541175 nY=$01 mY=$00 when soft-deferred).
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode)
                && !(_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }
- new_string:             // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
            // FETCH is not overlapped so host samples the pre-op P/regs.
            // After fused LDA (zp),Y trail==1, VICE fuses INY then TAX (c=541175,
            // c=541177). Sticky chain until a non-implied is fetched.
            if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && ((_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
                    || _fuseImpliedAfterIndyLoad))
            {
                _fuseImpliedAfterIndyLoad = true;
                // fall through to ExecuteOpcode (fused)
            }
            else if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode))
            {
                _fuseImpliedAfterIndyLoad = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }
            else
            {
                _fuseImpliedAfterIndyLoad = false;
            }
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         _skipSoftImmAfterStagedTakenBranch = false;
        _bootstrapCycles = ResetCycleDelay;
- new_string:         _skipSoftImmAfterStagedTakenBranch = false;
        _fuseImpliedAfterIndyLoad = false;
        _bootstrapCycles = ResetCycleDelay;

### Turn 213 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 214 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 215 (Function)
[tool_response]
The string to replace was not found in the file, use the read_file tool to see the correct string. The user may have changed the file since you last read it.

Nearest match: line 358:     private bool _skipSoftImmAfterStagedTakenBranch;

### Turn 216 (Assistant)
[tool_request: grep]
- pattern: _fuseImpliedAfterIndyLoad|_skipSoftImmAfterStagedTakenBranch
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 848
- limit: 45
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2645
- limit: 30

### Turn 217 (Function)
[tool_response]
2645→    /// clears last_opcode_info/stolen_cycles/check_ba_low the same way before
    /// re-entering the fetch loop; mainc64cpu.c VICE_SHIM_HOSTED block).
    /// </summary>
    private void ResetInFlightState()
    {
2650→        _opcode = 0;
        _cycle = 0;
        _suppressBootstrapBoundary = true;
        _interruptSampleDespiteSuppress = false;
        _bootstrapCycles = ResetCycleDelay;
        _stagedMemoryReadCompleted = false;
        _delayNextFetch = false;
        _stagedNzUpdate = false;
        _stagedNzValue = 0;
        _stagedCarryUpdate = false;
2660→        _stagedCarryValue = false;
        _branchTargetFetchPending = false;
        _branchPageCrossExtraPending = false;
        _trailingCyclesAtNextPc = 0;
        _currentInsnTrailingAtNextPc = 0;
        _fullLengthTakenBranchCompleted = false;
        _afterFullLengthTakenBranch = false;
        _nonOverlappedFetchPhase = false;
        _nonOverlappedRegion = false;
        _deferZpRmwPcAdvanceOne = false;
2670→        _nextJsrNonOverlapped = false;
        _notTakenBranchHoldFinalPc = false;
        _callTargetFetchPending = false;
        _deferImmediateLoadAfterBranch = false;
        _deferImpliedRegisterCompletionAfterBranch = false;

### Turn 218 (Function)
[tool_response]
848→                    _nonOverlappedFetchPhase = true;
                    return;
850→                }
            }

            // VICE abs ALU (ORA/AND/EOR/ADC/SBC $nnnn): GET_ABS CLK exports
            // pre-op A/PC; body + INC_PC after that sample (c=519285 nA=$03
            // mA=$1F nPC=opcode mPC=next). Soft-defer when non-overlapped.
            if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsAbsoluteAluOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
860→                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                _nonOverlappedFetchPhase = true;
                return;
            }

            // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
870→            // FETCH is not overlapped so host samples the pre-op P/regs.
            // After fused LDA (zp),Y trail==1, VICE fuses INY then TAX (c=541175,
            // c=541177). Sticky chain until a non-implied is fetched.
            if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && ((_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
                    || _fuseImpliedAfterIndyLoad))
            {
                _fuseImpliedAfterIndyLoad = true;
                // fall through to ExecuteOpcode (fused)
            }
880→            else if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode))
            {
                _fuseImpliedAfterIndyLoad = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
890→                _nonOverlappedFetchPhase = true;
                return;
            }

### Turn 219 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 11 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
358:    private bool _skipSoftImmAfterStagedTakenBranch;
363:    private bool _fuseImpliedAfterIndyLoad;
814:            var skipSoftImmThisInsn = _skipSoftImmAfterStagedTakenBranch;
815:            _skipSoftImmAfterStagedTakenBranch = false;
875:                    || _fuseImpliedAfterIndyLoad))
877:                _fuseImpliedAfterIndyLoad = true;
883:                _fuseImpliedAfterIndyLoad = false;
895:                _fuseImpliedAfterIndyLoad = false;
1965:            _skipSoftImmAfterStagedTakenBranch = true;
2046:        _skipSoftImmAfterStagedTakenBranch = false;
2694:        _skipSoftImmAfterStagedTakenBranch = false;
</workspace_result>

### Turn 220 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2055
- limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 868
- limit: 50
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:         _notTakenBranchHoldFinalPc = false;
        _callTargetFetchPending = false;
        _deferImmediateLoadAfterBranch = false;
- new_string:         _notTakenBranchHoldFinalPc = false;
        _callTargetFetchPending = false;
        _fuseImpliedAfterIndyLoad = false;
        _deferImmediateLoadAfterBranch = false;

### Turn 221 (Function)
[tool_response]
2055→        _softDeferCompareCommit = false;
        _pendingSoftCompareCommit = false;
        _softDeferredImpliedOp = false;
        _pendingDeferredImpliedRegisterCompletion = false;
        _stagedReturnAddress = 0;
2060→        _effectiveAddress = 0;
        _fetched = 0;
    }

    private ushort ReadZeroPageOperand()
    {
        return Read((ushort)(_instructionPC + 1));
    }

    private static bool IsImmediateLoadOpcode(byte opcode)
2070→    {
        return opcode is 0xA0 or 0xA2 or 0xA9;
    }

    /// <summary>
    /// 2-byte immediate ops whose VICE body runs INC_PC with no trailing CLK_INC
    /// after FETCH (LD/CP/ALU #imm). Used for post-NOP-chain soft-defer.
    /// </summary>
    private static bool IsTwoByteImmediateOpcode(byte opcode)
    {
2080→        return IsImmediateLoadOpcode(opcode)
            || opcode is 0x29 or 0x09 or 0x49 or 0x69 or 0xE9 or 0xC9 or 0xE0 or 0xC0;
    }

    /// <summary>Unstaged zero-page compares (not CD/D1 staged path).</summary>
    private static bool IsZeroPageCompareOpcode(byte opcode)
    {
        return opcode is 0xC5 or 0xD5 or 0xE4 or 0xC4;
    }

2090→    /// <summary>
    /// Absolute ALU ops executed via unstaged ExecuteOpcode (not cycle-staged
    /// CMP abs / RMW). VICE GET_ABS + ORA/AND/... apply A after the data-read
    /// CLK export.
    /// </summary>

### Turn 222 (Function)
[tool_response]
868→            // VICE implied ops (CLC/TAX/etc.): flag/register write and INC_PC run
            // after the final CLK is exported. Soft-defer the write when first
870→            // FETCH is not overlapped so host samples the pre-op P/regs.
            // After fused LDA (zp),Y trail==1, VICE fuses INY then TAX (c=541175,
            // c=541177). Sticky chain until a non-implied is fetched.
            if (IsImpliedRegisterOrFlagOpcode(_opcode)
                && ((_previousOpcode == 0xB1 && DebugPriorTrailingAtNextPc == 1)
                    || _fuseImpliedAfterIndyLoad))
            {
                _fuseImpliedAfterIndyLoad = true;
                // fall through to ExecuteOpcode (fused)
            }
880→            else if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                && IsImpliedRegisterOrFlagOpcode(_opcode))
            {
                _fuseImpliedAfterIndyLoad = false;
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _holdIndYStorePcOneCycle = true;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
890→                _nonOverlappedFetchPhase = true;
                return;
            }
            else
            {
                _fuseImpliedAfterIndyLoad = false;
            }

            // NOP after short taken branch: VICE still exports opcode PC on the
            // final FETCH CLK (INC_PC is after that sample). Suppress boundary
900→            // so GetState returns visible opcode PC, not the post-fetch next PC.
            if (_nopHoldOpcodePcOnFinal)
            {
                _nopHoldOpcodePcOnFinal = false;
                _visiblePC = _opcodeAddress;
                _suppressBootstrapBoundary = true;
                return;
            }

            // After post-branch NOP chain: soft-defer 2-byte immediate body
910→            // (CMP#/AND#/etc.) so final FETCH sample keeps opcode PC; commit at
            // the next instruction's first tick (VICE INC_PC after last FETCH).
            if (_softDeferAfterNopChain && IsTwoByteImmediateOpcode(_opcode))
            {
                _softDeferredImpliedOp = true;
                _softDeferredImpliedOpcode = _opcode;
                _softDeferredImpliedInstructionPc = _opcodeAddress;
                _softDeferAfterNopChain = false;

### Turn 223 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 224 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 2105
- limit: 35
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-tax-chain-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after TAX fuse chain
- timeout: 300000

### Turn 225 (Function)
[tool_response]
2105→    {
        return opcode is
            0x0A or // ASL A
            0x18 or // CLC
            0x2A or // ROL A
2110→            0x38 or // SEC
            0x4A or // LSR A
            0x58 or // CLI
            0x6A or // ROR A
            0x78 or // SEI
            0x88 or // DEY
            0x8A or // TXA
            0x98 or // TYA
            0x9A or // TXS
            0xA8 or // TAY
2120→            0xAA or // TAX
            0xB8 or // CLV
            0xBA or // TSX
            0xC8 or // INY
            0xCA or // DEX
            0xD8 or // CLD
            0xE8 or // INX
            0xF8;   // SED
    }

2130→    private static bool IsAbsoluteXLoadOpcode(byte opcode)
    {
        return opcode is 0xBD;
    }

    private static bool IsAbsoluteYLoadOpcode(byte opcode)
    {
        return opcode is 0xB9;
    }


### Turn 226 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
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

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 227 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-tax-chain-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after TAX fuse chain
- timeout: 300000

### Turn 228 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.19]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541179 matchedCycles=541178 budget=2216810
c=541168 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=0 op=$A0 trail=2 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541169 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541170 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541171 nPC=$DC61 mPC=$DC61 nA=$B3 mA=$B3 nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541172 nPC=$DC61 mPC=$DC61 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541173 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541174 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541175 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=0 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541176 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=1 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541177 nPC=$DC65 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541178 nPC=$DC65 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$F0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541179 nPC=$DC67 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$F0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 229 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1820
- limit: 90

### Turn 230 (Function)
[tool_response]
1820→                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                if (Peek(_pc) == 0x60)
                {
                    // Same RTS prefetch convention as FinishStagedMemoryWrite:
                    // a following RTS expects an un-lagged entry, so skip the
                    // delayed-fetch tick and fetch it on the next cycle.
                    return true;
                }

1830→                _delayNextFetch = true;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Cycle-staged RTI (0x40; TR-LOCKSTEP-VSF-001), mirroring VICE's 6-cycle
    /// sequence (6510dtvcore.c RTI: fetch, dummy, stack peek, pull P, pull PCL,
1840→    /// pull PCH): each pull cycle exports the incremented S; the pulled status
    /// becomes visible one cycle after its pull (assignment happens after that
    /// cycle's CLK_INC), and the return-address JUMP becomes visible on the
    /// final tick, exactly like the hosted per-cycle register export.
    /// </summary>
    private bool TryExecuteCycleStagedRtiOpcode()
    {
        if (_opcode != 0x40)
        {
            return false;
1850→        }

        switch (_cycle)
        {
            case 3:
                if (_rtiDeferFirstPullOne)
                {
                    // Extra STACK_PEEK after soft body (c=522485).
                    _rtiDeferFirstPullOne = false;
                    _cycle = 4;
1860→                    return true;
                }

                _fetched = Pop();
                return true;
            case 2:
                P = (byte)((_fetched & ~0x10) | (P & 0x10));
                _stagedReturnAddress = Pop();
                return true;
            case 1:
1870→                _stagedReturnAddress |= (ushort)(Pop() << 8);
                return true;
            case 0:
                _pc = _stagedReturnAddress;
                _instructionPC = _pc;
                _visiblePC = _pc;
                // Leave non-overlapped region on RTI (same as RTS). Soft-deferred
                // LDA # after RTI was lagging VICE (c=522491 nA=$01 mA=$20).
                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
1880→                return true;
            default:
                return false;
        }
    }

    private bool TryExecuteCycleStagedBranchOpcode()
    {
        if (!IsBranchOpcode(_opcode))
        {
1890→            return false;
        }

        var fallThrough = (ushort)(_instructionPC + 2);

        // Taken multi-cycle: VICE BRANCH does INC_PC then dummy LOAD+CLK_INC
        // (exports fall-through) before JUMP. Short taken (2 host steps) keeps
        // opcode PC on cycle 1 and only shows fall-through at cycle 0.
        if (IsBranchTaken(_opcode) && _takenBranchStagedFallthrough && _cycle >= 1)
        {
1900→            if (_cycle >= 2)
            {
                // Early host samples still at opcode (extra FETCH-shaped CLKs).
                _visiblePC = _instructionPC;
                return true;
            }

            // _cycle == 1: fall-through export (c=540201 nPC=$D92B).
            _pc = fallThrough;
            _visiblePC = fallThrough;

### Turn 231 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 1910
- limit: 80

### Turn 232 (Function)
[tool_response]
1910→            _suppressBootstrapBoundary = true;
            return true;
        }

        if (_cycle != 0)
        {
            return false;
        }

        if (!IsBranchTaken(_opcode))
1920→        {
            // VICE not-taken: last FETCH CLK still exports the opcode address;
            // INC_PC runs after that sample. Hold MUST win before RTS prefetch
            // (c=532304 nPC=$E906 mPC=$E908 when PrefetchOpcodeAt skipped hold).
            if (_notTakenBranchHoldFinalPc)
            {
                _pc = fallThrough;
                _visiblePC = _instructionPC;
                _suppressBootstrapBoundary = true;
                // Following zp INC needs one extra FETCH-shaped CLK at its opcode
1930→                // before AdvanceVisiblePc (cycle 20756).
                _deferZpRmwPcAdvanceOne = true;
                // Fall-through insn also has a clean first FETCH (ROR A at 195424).
                _nonOverlappedFetchPhase = true;
                _nonOverlappedRegion = true;
                _notTakenBranchHoldFinalPc = false;
                return true;
            }

            if (Peek(fallThrough) == 0x60)
1940→            {
                PrefetchOpcodeAt(fallThrough);
                return true;
            }

            // Not-taken without hold (e.g. priorTrailing>=3): VICE already
            // exported fall-through; clear sticky non-overlapped so following
            // SEC/implied is fused not soft-lagged (c=540323 nP=$A1 mP=$A0).
            if (_branchFetchPriorTrailing >= 3)
            {
1950→                _nonOverlappedRegion = false;
                _nonOverlappedFetchPhase = false;
            }

            return false;
        }

        var target = (ushort)(fallThrough + (sbyte)Read((ushort)(_instructionPC + 1)));
        _pc = target;
        // Multi-cycle staged path already exported fall-through at cycle 1;
1960→        // this CLK is VICE JUMP (c=540202 nPC=$D91D mPC=$D92B when still
        // fall-through). Short taken keeps fall-through visible here.
        if (_takenBranchStagedFallthrough)
        {
            _visiblePC = target;
            _skipSoftImmAfterStagedTakenBranch = true;
        }
        else
        {
            _visiblePC = fallThrough;
1970→        }

        _suppressBootstrapBoundary = true;
        _takenBranchStagedFallthrough = false;
        if ((fallThrough & 0xFF00) != (target & 0xFF00))
        {
            // TR-LOCKSTEP-VSF-001: a taken branch across a page boundary costs
            // 4 native cycles (6510dtvcore.c BRANCH: the PBC fix-up cycle does
            // another dummy fetch and keeps exporting the un-fixed PC); consume
            // one extra tick before the target fetch.
1980→            _branchPageCrossExtraPending = true;
        }
        else
        {
            _branchTargetFetchPending = true;
        }

        return true;
    }


### Turn 233 (Assistant)
[tool_request: grep]
- pattern: _notTakenBranchHoldFinalPc
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs

### Turn 234 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 5 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
283:    private bool _notTakenBranchHoldFinalPc;
641:            _notTakenBranchHoldFinalPc = IsBranchOpcode(_opcode) && cleanBranchFetch
1924:            if (_notTakenBranchHoldFinalPc)
1935:                _notTakenBranchHoldFinalPc = false;
2671:        _notTakenBranchHoldFinalPc = false;
</workspace_result>

### Turn 235 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 625
- limit: 40

### Turn 236 (Function)
[tool_response]
625→            // 3 CLK; first FETCH may be overlapped with the previous opcode's
            // trailing export at this same address).
            // Empirically vs xvic GetPC after each step_cycle:
            //   dwell 1 (e.g. CMP last cycle only): 2 host-visible branch steps
            //   dwell 2 (e.g. zp INC after deferred mid-PC advance): 3 host steps
630→            //   dwell 3 (e.g. zp INC with early AdvanceVisiblePc): 2 host steps
            // Full-length taken budget only for the dwell==2 case (cycle 5005).
            var fullLengthTakenBranch = false;
            _branchFetchPriorTrailing = priorTrailingAtNextPc;
            // Clean first FETCH (not overlapped with previous last export).
            var cleanBranchFetch = priorTrailingAtNextPc == 2 || afterSoftCompare
                || _nonOverlappedRegion || _nonOverlappedFetchPhase;
            // Not-taken: hold opcode PC on final CLK when first FETCH is clean
            // (incl. sticky non-overlapped region after soft BIT/imm; c=522430).
            // Skip hold when priorTrailing>=3: VICE already exports fall-through
640→            // on that final CLK (c=540321 nPC=$D92B mPC=opcode with hold).
            _notTakenBranchHoldFinalPc = IsBranchOpcode(_opcode) && cleanBranchFetch
                && priorTrailingAtNextPc < 3;
            // Full-length taken branch when previous left 2 trailing dwells at next
            // PC (zp INC deferred), or when soft-deferred CMP just committed so
            // the first FETCH of this branch is not overlapped (cycle 5034).
            if (IsBranchOpcode(_opcode) && IsBranchTaken(_opcode) && cleanBranchFetch)
            {
                _cycle++;
                fullLengthTakenBranch = true;
650→            }

            DebugFullLengthTakenBranch = fullLengthTakenBranch;

            _stagedMemoryReadCompleted = false;
            _delayNextFetch = false;
            _stagedNzUpdate = false;
            _stagedNzValue = 0;
            _stagedCarryUpdate = false;
            _stagedCarryValue = false;
660→            _zpRmwModifyCommitted = false;
            _pendingPlpStatus = false;
            // BIT after RTS delayNextFetch: soft-apply one cycle earlier so the
            // host sample count matches VICE (c=522467 held late with cycle-0
            // soft). After branch / other trail, soft at cycle 0 (c=522392).

### Turn 237 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             // Not-taken: hold opcode PC on final CLK when first FETCH is clean
            // (incl. sticky non-overlapped region after soft BIT/imm; c=522430).
            // Skip hold when priorTrailing>=3: VICE already exports fall-through
            // on that final CLK (c=540321 nPC=$D92B mPC=opcode with hold).
            _notTakenBranchHoldFinalPc = IsBranchOpcode(_opcode) && cleanBranchFetch
                && priorTrailingAtNextPc < 3;
- new_string:             // Not-taken: hold opcode PC on final CLK when first FETCH is clean
            // (incl. sticky non-overlapped region after soft BIT/imm; c=522430).
            // Skip hold when priorTrailing>=3: VICE already exports fall-through
            // on that final CLK (c=540321 nPC=$D92B mPC=opcode with hold).
            // Skip hold after fused INY/TAX chain: trail==0 BEQ already shows
            // fall-through (c=541179 nPC=$DC67 mPC=$DC65 with hold).
            _notTakenBranchHoldFinalPc = IsBranchOpcode(_opcode) && cleanBranchFetch
                && priorTrailingAtNextPc < 3
                && !_fuseImpliedAfterIndyLoad;
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
- content: ViceSharp VIC-20 every-cycle lockstep vs xvic - VICE realign session
Date: 2026-08-06
Method: always ask (1) where did we diverge from VICE? (2) how do we get back in line?

Skeptic gaps
------------
1. NotTakenBne / TakenBne: both [Fact(Explicit = true)] BranchCycleCountTests.cs:24,55
2. Focused every-cycle gate: EveryCycle_CpuRegs_Match_FocusedWindow (500_000)
3. Residual honesty: matchedCycles below - NOT multi-second, NOT 1.1e6

Gates (machine-verified)
------------------------
Focused 500k + Via1Iec + Vic20NativeLockstepTests:
  FOCUSED_EXIT=0, Passed 20 / Failed 0
  latest: docs/focused-tax-chain-2026-08-06.log

2s (VICESHARP_LOCKSTEP_2S=1):
  matchedCycles=541178  DIV first=541179  budget=2216810
  ~0.488 s PAL (541178/1108405) - NOT multi-second, NOT 1.1e6
  receipt: docs/2s-tax-chain-2026-08-06.log

Progress vs skeptic residual (517828)
-------------------------------------
Skeptic residual:     matchedCycles=517828
Prior IRQ residual:   matchedCycles=540739
Current residual:     matchedCycles=541178
Net vs skeptic:       +23350 every-cycle matches
Net vs IRQ residual:  +439 every-cycle matches this continuation

VICE realignments this continuation
-----------------------------------
1. Post-JSR IRQ sample via _interruptSampleDespiteSuppress (c=540740).
2. Soft-imm skip after staged JUMP is one-instruction only.
3. zp/zp,X shift RMW modify phase (branch/trail rules).
4. JSR after 2-byte imm trail==1: overlapped push+JUMP.
5. Plain store trail>=2 advances PC; LDY# after plain store fuses.
6. LDA (zp),Y after fused LDY#: A on data-read, NZ+PC on apply.
7. Fused implied chain after that (INY, TAX) via sticky flag.
8. In progress: not-taken BEQ after fused chain must not hold opcode PC
   (c=541179 nPC=fall-through mPC=opcode).

Current residual (VICE-facing)
------------------------------
Where:
  c=541179 BEQ $F0 not-taken at $DC65 after TAX
  nPC=$DC67 mPC=$DC65

Evidence: docs/2s-tax-chain-2026-08-06.log

How diverged:
  notTakenBranchHoldFinalPc still set from nonOvl clean fetch; VICE already
  exported fall-through after fused TAX trail==0.

How to realign:
  Skip not-taken hold when _fuseImpliedAfterIndyLoad (patch staged).

Not done / do not claim
-----------------------
- Full 2s every-cycle green
- Match past ~1.1e6 cycles
- Multi-second lockstep (still ~0.488 s)


### Turn 238 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt.

### Turn 239 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 240 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-beq-hold-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"
- description: Focused gates after BEQ hold skip
- timeout: 300000

### Turn 241 (Function)
[tool_response]
exit: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0


### Turn 242 (Assistant)
[tool_request: run_terminal_command]
- command: $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-beq-hold-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue
- description: Run 2s lockstep after BEQ hold skip
- timeout: 300000
[tool_request: todo_write]
- todos: [{"id":"3","content":"3: Fix JSR-end IRQ sample (no suppress); re-run focused+2s; update receipt","status":"completed"}]
- merge: true

### Turn 243 (Function)
[tool_response]
- [completed] 1: Verify skeptic: NotTaken Explicit + residual honesty on disk
- [completed] 2: 2: Diagnose native S drop mid-ROR at c=540740 vs VICE
- [completed] 3: 3: Fix JSR-end IRQ sample (no suppress); re-run focused+2s; update receipt


### Turn 244 (Function)
[tool_response]
exit: 0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.25]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541183 matchedCycles=541182 budget=2216810
c=541172 nPC=$DC61 mPC=$DC61 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$22 mP=$22 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541173 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541174 nPC=$DC63 mPC=$DC63 nA=$9B mA=$9B nX=$61 mX=$61 nY=$00 mY=$00 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541175 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=0 op=$C8 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541176 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=1 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541177 nPC=$DC65 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541178 nPC=$DC65 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$F0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541179 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$F0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541180 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541181 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541182 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541183 nPC=$DC67 mPC=$DC67 nA=$3E mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 245 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:             case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                // After fused LDY#/LDA# with trail==1, VICE already shows loaded A
                // on the data-read sample (c=541172 nA=$9B mA=$B3 when deferred).
                var indyCommitAOnDataRead = !_deferIndirectYLoadCompletionAfterBranch
                    && (!_nonOverlappedFetchPhase && !_nonOverlappedRegion
                        || (IsImmediateLoadOpcode(_previousOpcode)
                            && DebugPriorTrailingAtNextPc == 1));
                if (indyCommitAOnDataRead)
                    A = indirectYValue;
                FinishStagedMemoryRead(2, indirectYValue);
                return true;
- new_string:             case 0xB1:
                var indirectYValue = Read(ReadIndirectYOperand());
                // Lag-shaped path: A lands on the data-read tick. Non-overlapped
                // callees defer A to the apply tick so pre-load A matches xvic.
                // After fused LDY# trail==1 or not-taken BEQ trail==1, VICE already
                // shows loaded A on data-read (c=541172 after LDY#; c=541183 after
                // BEQ nA=$3E mA=$9B when deferred).
                var indyCommitAOnDataRead = !_deferIndirectYLoadCompletionAfterBranch
                    && (!_nonOverlappedFetchPhase && !_nonOverlappedRegion
                        || ((IsImmediateLoadOpcode(_previousOpcode)
                                || IsBranchOpcode(_previousOpcode))
                            && DebugPriorTrailingAtNextPc == 1));
                if (indyCommitAOnDataRead)
                    A = indirectYValue;
                FinishStagedMemoryRead(2, indirectYValue);
                return true;
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- old_string:                 // Exception: LDA (zp),Y after fused LDY# trail==1 already showed
                // loaded A on data-read; apply CLK must also pair NZ+PC with VICE
                // (c=541173 nPC=$DC63 nP=$A0 mPC=opcode mP pre-load).
                var indyFusedAfterImmLoad = _opcode is 0xB1
                    && IsImmediateLoadOpcode(_previousOpcode)
                    && DebugPriorTrailingAtNextPc == 1;
                if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && !indyFusedAfterImmLoad
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                {
                    _pendingSoftCompareCommit = true;
                    _stagedCarryUpdate = false;
                }
                else
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }
            }
            else if (_opcode is 0xB1)
            {
                A = Read(ReadIndirectYOperand());
                UpdateNZ(A);
            }

            var indyFusedPc = _opcode is 0xB1
                && IsImmediateLoadOpcode(_previousOpcode)
                && DebugPriorTrailingAtNextPc == 1;
- new_string:                 // Exception: LDA (zp),Y after fused LDY# or not-taken branch
                // trail==1 already showed loaded A on data-read; apply CLK must
                // also pair NZ+PC with VICE (c=541173 after LDY#; c=541183 after BEQ).
                var indyFusedAfterImmLoad = _opcode is 0xB1
                    && (IsImmediateLoadOpcode(_previousOpcode)
                        || IsBranchOpcode(_previousOpcode))
                    && DebugPriorTrailingAtNextPc == 1;
                if ((_nonOverlappedFetchPhase || _nonOverlappedRegion)
                    && !indyFusedAfterImmLoad
                    && _opcode is 0xB1 or 0xA5 or 0xA6 or 0xA4 or 0xB5 or 0xB6 or 0xB4
                        or 0xAD or 0xAE or 0xAC or 0xBD or 0xB9 or 0xBC or 0xBE)
                {
                    _pendingSoftCompareCommit = true;
                    _stagedCarryUpdate = false;
                }
                else
                {
                    UpdateNZ(_stagedNzValue);
                    _stagedNzUpdate = false;
                }
            }
            else if (_opcode is 0xB1)
            {
                A = Read(ReadIndirectYOperand());
                UpdateNZ(A);
            }

            var indyFusedPc = _opcode is 0xB1
                && (IsImmediateLoadOpcode(_previousOpcode)
                    || IsBranchOpcode(_previousOpcode))
                && DebugPriorTrailingAtNextPc == 1;
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
- content: ViceSharp VIC-20 every-cycle lockstep vs xvic - VICE realign session
Date: 2026-08-06
Method: always ask (1) where did we diverge from VICE? (2) how do we get back in line?

Skeptic gaps
------------
1. NotTakenBne / TakenBne: both [Fact(Explicit = true)] BranchCycleCountTests.cs:24,55
2. Focused every-cycle gate: EveryCycle_CpuRegs_Match_FocusedWindow (500_000)
3. Residual honesty: matchedCycles below - NOT multi-second, NOT 1.1e6

Gates (machine-verified)
------------------------
Focused 500k + Via1Iec + Vic20NativeLockstepTests:
  FOCUSED_EXIT=0, Passed 20 / Failed 0
  latest: docs/focused-beq-hold-2026-08-06.log

2s (VICESHARP_LOCKSTEP_2S=1):
  matchedCycles=541182  DIV first=541183  budget=2216810
  ~0.488 s PAL (541182/1108405) - NOT multi-second, NOT 1.1e6
  receipt: docs/2s-beq-hold-2026-08-06.log

Progress vs skeptic residual (517828)
-------------------------------------
Skeptic residual:     matchedCycles=517828
Prior IRQ residual:   matchedCycles=540739
Current residual:     matchedCycles=541182
Net vs skeptic:       +23354 every-cycle matches
Net vs IRQ residual:  +443 every-cycle matches this continuation

VICE realignments this continuation
-----------------------------------
1. Post-JSR IRQ sample: _interruptSampleDespiteSuppress + PC getter decouple
   (c=540740 native S drop was DO_INTERRUPT while managed ran ROR).
2. Soft-imm skip after staged JUMP: one-instruction window only.
3. zp/zp,X shift RMW modify phase (branch / trail==2 / default cycle 2).
4. JSR after 2-byte imm trail==1: overlapped push + JUMP end-to-end.
5. Plain store trail>=2 advances on write; LDY# after plain store fuses.
6. LDA (zp),Y after fused LDY# or not-taken BEQ trail==1: early A + NZ/PC.
7. Fused implied chain (INY/TAX) after that path; skip not-taken BEQ hold.

Current residual (VICE-facing)
------------------------------
Where:
  c=541183 second LDA (zp),Y $B1 at $DC67 after not-taken BEQ
  nA=$3E mA=$9B (native already on data-read commit)

Evidence: docs/2s-beq-hold-2026-08-06.log

How diverged:
  Early A commit was limited to previous=imm load; after BEQ previous is
  branch so managed still deferred A.

How to realign:
  Treat previous=branch trail==1 like previous=imm load for B1 A/NZ/PC
  (patch staged; re-run focused+2s on next turn).

Not done / do not claim
-----------------------
- Full 2s every-cycle green
- Match past ~1.1e6 cycles
- Multi-second lockstep (still ~0.488 s)


### Turn 246 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt.

### Turn 247 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 248 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs has been updated successfully.

### Turn 249 (Assistant)
[tool_request: run_terminal_command]
- command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-b1-beq-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"; if ($LASTEXITCODE -eq 0) { $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-b1-beq-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue }
- description: Focused then 2s after B1 post-BEQ fix
- timeout: 300000

### Turn 250 (Function)
[tool_response]
<task-id>call-614cc956-1c34-4343-8088-a60a3241db33-158</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-614cc956-1c34-4343-8088-a60a3241db33-158.log</output-file>
<status>running</status>
<summary>Command "dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-b1-beq-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"; if ($LASTEXITCODE -eq 0) { $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-b1-beq-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue }" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-614cc956-1c34-4343-8088-a60a3241db33-158"] to retrieve the output.

### Turn 251 (Assistant)
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-614cc956-1c34-4343-8088-a60a3241db33-158"]
- timeout_ms: 300000

### Turn 252 (Function)
[tool_response]
=== Task call-614cc956-1c34-4343-8088-a60a3241db33-158 ===
Command: dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_FocusedWindow|FullyQualifiedName~Vic20NativeLockstepTests|FullyQualifiedName~Vic20Via1IecPortTests" 2>&1 | Tee-Object -FilePath docs\focused-b1-beq-2026-08-06.log; Write-Host "FOCUSED_EXIT=$LASTEXITCODE"; if ($LASTEXITCODE -eq 0) { $env:VICESHARP_LOCKSTEP_2S='1'; dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal" --no-build 2>&1 | Tee-Object -FilePath docs\2s-b1-beq-2026-08-06.log; Write-Host "2S_EXIT=$LASTEXITCODE"; Remove-Item Env:VICESHARP_LOCKSTEP_2S -ErrorAction SilentlyContinue }
Status: completed
Duration: 19.89s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-614cc956-1c34-4343-8088-a60a3241db33-158.log

=== Output ===
  Determining projects to restore...
  All projects are up-to-date for restore.
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
FOCUSED_EXIT=0
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:04.13]     ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [FAIL]
  Failed ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal [2 s]
  Error Message:
   DIV first=541187 matchedCycles=541186 budget=2216810
c=541176 nPC=$DC64 mPC=$DC64 nA=$9B mA=$9B nX=$61 mX=$61 nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=1 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541177 nPC=$DC65 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$AA trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541178 nPC=$DC65 mPC=$DC65 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$F0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541179 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=0 op=$F0 trail=0 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541180 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=4 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541181 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=3 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541182 nPC=$DC67 mPC=$DC67 nA=$9B mA=$9B nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=2 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541183 nPC=$DC67 mPC=$DC67 nA=$3E mA=$3E nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$A0 mP=$A0 dbgCyc=1 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=True irq=0 mis=False
c=541184 nPC=$DC69 mPC=$DC69 nA=$3E mA=$3E nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=0 op=$B1 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541185 nPC=$DC69 mPC=$DC69 nA=$3E mA=$3E nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=2 op=$45 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541186 nPC=$DC69 mPC=$DC69 nA=$3E mA=$3E nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=1 op=$45 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=False
c=541187 nPC=$DC6B mPC=$DC69 nA=$1E mA=$3E nX=$9B mX=$9B nY=$01 mY=$01 nS=$F7 mS=$F7 nP=$20 mP=$20 dbgCyc=0 op=$45 trail=1 nonOvlR=True nonOvlF=True dly=False stg=False irq=0 mis=True
bus: m$C1=$00 m$C2=$20 n$C1=$00 n$C2=$20 m$C3=$6D m$C4=$FD n$C3=$6D n$C4=$FD m$0314=$BF n$0314=$BF

  Stack Trace:
     at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.RunEveryCycle(Int32 budget) in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 123
   at ViceSharp.TestHarness.Vic20.Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TwoSecondPal() in F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20DivergeProbe.cs:line 47
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 2 s - ViceSharp.TestHarness.dll (net10.0)
2S_EXIT=1


### Turn 253 (Assistant)
[tool_request: write]
- file_path: F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt
- content: ViceSharp VIC-20 every-cycle lockstep vs xvic - VICE realign session
Date: 2026-08-06
Method: always ask (1) where did we diverge from VICE? (2) how do we get back in line?

Skeptic gaps
------------
1. NotTakenBne / TakenBne: both [Fact(Explicit = true)] BranchCycleCountTests.cs:24,55
2. Focused every-cycle gate: EveryCycle_CpuRegs_Match_FocusedWindow (500_000)
3. Residual honesty: matchedCycles below - NOT multi-second, NOT 1.1e6

Gates (machine-verified)
------------------------
Focused 500k + Via1Iec + Vic20NativeLockstepTests:
  FOCUSED_EXIT=0, Passed 20 / Failed 0
  latest: docs/focused-b1-beq-2026-08-06.log

2s (VICESHARP_LOCKSTEP_2S=1):
  matchedCycles=541186  DIV first=541187  budget=2216810
  ~0.488 s PAL (541186/1108405) - NOT multi-second, NOT 1.1e6
  receipt: docs/2s-b1-beq-2026-08-06.log

Progress vs skeptic residual (517828)
-------------------------------------
Skeptic residual:     matchedCycles=517828
Prior IRQ residual:   matchedCycles=540739
Current residual:     matchedCycles=541186
Net vs skeptic:       +23358 every-cycle matches
Net vs IRQ residual:  +447 every-cycle matches this continuation

VICE realignments this continuation
-----------------------------------
1. Post-JSR IRQ sample: _interruptSampleDespiteSuppress + PC getter decouple
   (c=540740 native S drop was DO_INTERRUPT while managed ran ROR).
2. Soft-imm skip after staged JUMP: one-instruction window only.
3. zp/zp,X shift RMW modify phase (branch / trail==2 / default cycle 2).
4. JSR after 2-byte imm trail==1: overlapped push + JUMP end-to-end.
5. Plain store trail>=2 advances on write; LDY# after plain store fuses.
6. LDA (zp),Y after fused LDY# or not-taken BEQ trail==1: early A + NZ/PC.
7. Fused implied chain (INY/TAX); skip not-taken BEQ hold after that chain.

Current residual (VICE-facing)
------------------------------
Where:
  c=541187 EOR zp $45 at $DC69 after LDA (zp),Y
  nPC=$DC6B nA=$1E mPC=$DC69 mA=$3E (native fused body; managed soft)

Evidence: docs/2s-b1-beq-2026-08-06.log

How diverged:
  Absolute/zp ALU soft-defer in nonOvl still holds pre-op A/PC on final CLK
  while VICE already committed after fused B1 trail==1.

How to realign:
  Fuse zp/abs ALU after B1 trail==1 (same shape as fused implied chain).

Not done / do not claim
-----------------------
- Full 2s every-cycle green
- Match past ~1.1e6 cycles
- Multi-second lockstep (still ~0.488 s)


### Turn 254 (Function)
[tool_response]
Wrote file successfully to F:\GitHub\vice-sharp\docs\receipts-lockstep-vice-realign-2026-08-06.txt.

### Turn 255 (Assistant)
**Standing rule (locked):** every lockstep fix starts from:
1. **Where did we diverge from VICE?**
2. **How do we get back in line with VICE?** (no lag hacks)

Profile loaded (8 feedback files + identity).

---

### Root residual at session resume (c=540740)

**Where:** After JSR to `$D9B0`, native S dropped mid-“ROR” (`nS=$F2 mS=$F3`) with PC frozen.

**Why (vs VICE):** `CompleteJsrTargetFetch` set `_suppressBootstrapBoundary`, so `IsInstructionBoundary` stayed false and `SystemClock` never armed IRQ. VICE’s main loop runs `DO_INTERRUPT` after JSR `JUMP` and **before** callee FETCH. Native was in IRQ (first push); managed was executing ROR.

**Realign:** Keep suppress for host PC export on clean JSR last CLK; open IRQ sample via `_interruptSampleDespiteSuppress` and decouple the PC getter from interrupt-sample openings.

---

### Progress (receipts)

| Gate | Result |
|------|--------|
| Focused 500k + lockstep suite | **20/20 pass** (`FOCUSED_EXIT=0`) |
| 2s every-cycle | **matchedCycles=541186** (~0.488 s PAL) |
| Prior residual | 540739 |
| Skeptic residual | 517828 |
| Net this continuation | **+447** vs IRQ residual; **+23358** vs skeptic |

Not multi-second. Not 1.1e6. Not 2s green.

---

### Other VICE realignments landed

- Soft-imm skip after staged JUMP: **one-instruction window** only (sticky flag was fusing CPX# hundreds of cycles later)
- zp/zp,X shift RMW modify phase vs branch/trail
- JSR after 2-byte imm trail==1: overlapped push+JUMP
- Store/LDY#/LDA (zp),Y/INY/TAX/BEQ chain phase coupling after overlapped JSR entry

---

### Current residual (next fix)

**Where:** c=541187 `EOR zp $45` at `$DC69` after fused LDA (zp),Y  
`nPC=$DC6B nA=$1E` vs `mPC=$DC69 mA=$3E`

**How diverged:** nonOvl still soft-defers zp ALU body; VICE already committed.

**How to realign:** Fuse zp/abs ALU after B1 with trail==1 (same family as fused implied chain).

Evidence: `docs/receipts-lockstep-vice-realign-2026-08-06.txt`, `docs/2s-b1-beq-2026-08-06.log`.

I will keep using that two-question frame on every residual.

### Turn 256 (Assistant)
Continuing from residual c=541187: fuse zp/abs ALU after B1 trail==1, verify BranchCycleCountTests Explicit status, then re-run focused + 2s and update the receipt.
[tool_request: todo_write]
- todos: [{"id":"1","content":"Verify NotTakenBne Explicit on disk; fuse EOR after B1 trail==1","status":"in_progress"},{"id":"2","content":"Re-run focused + 2s; update receipt with honest residual","status":"pending"}]
- merge: false
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\BranchCycleCountTests.cs
- offset: 1
- limit: 90
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- offset: 850
- limit: 40
[tool_request: grep]
- pattern: IsAbsoluteAluOpcode|soft.*ALU|0x45|EOR
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Cpu\Mos6502.cs
- head_limit: 30

### Turn 257 (Function)
[tool_response]
- [in_progress] 1: Verify NotTakenBne Explicit on disk; fuse EOR after B1 trail==1
- [pending] 2: Re-run focused + 2s; update receipt with honest residual


### Turn 258 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class BranchCycleCountTests
{
10→    private sealed class SimpleRam : IMemory
    {
        private readonly byte[] _m;
        public SimpleRam(byte[] m) { _m = m; Id = new DeviceId(1); }
        public DeviceId Id { get; }
        public string Name => "ram";
        public Span<byte> Span => _m;
        public bool HandlesAddress(ushort a) => true;
        public byte Read(ushort a) => _m[a];
        public byte Peek(ushort a) => _m[a];
20→        public void Write(ushort a, byte v) => _m[a] = v;
        public void Reset() { }
    }

    [Fact(Explicit = true)]
    public void TakenBne_CycleTrace()
    {
        var ram = new byte[65536];
        ram[0x1000] = 0xD0;
        ram[0x1001] = 0x02;
30→        ram[0x1004] = 0xEA;
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam(ram));
        var cpu = new Mos6502(bus);
        cpu.P = 0x20; // Z clear => BNE taken
        cpu.S = 0xFF;
        cpu.PC = 0x1000;
        // drain reset bootstrap
        for (var i = 0; i < 20; i++) cpu.Tick();
        cpu.PC = 0x1000;
40→        cpu.P = 0x20;
        var lines = new System.Text.StringBuilder();
        for (var t = 1; t <= 10; t++)
        {
            cpu.Tick();
            lines.AppendLine($"t{t}: PC=${cpu.PC:X4} cyc={cpu.DebugCycle} op={cpu.DebugOpcode:X2} bound={cpu.IsInstructionBoundary}");
            if (t > 3 && cpu.IsInstructionBoundary && cpu.PC == 0x1004)
            {
                lines.AppendLine($"arrived target at t={t}");
                break;
50→            }
        }
        throw new Xunit.Sdk.XunitException(lines.ToString());
    }

    [Fact(Explicit = true)]
    public void NotTakenBne_CycleTrace()
    {
        var ram = new byte[65536];
        ram[0x1000] = 0xD0;
60→        ram[0x1001] = 0x02;
        ram[0x1002] = 0xEA;
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam(ram));
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        for (var i = 0; i < 20; i++) cpu.Tick();
        cpu.PC = 0x1000;
        cpu.P = 0x22; // Z set => BNE not taken
        var lines = new System.Text.StringBuilder();
70→        for (var t = 1; t <= 10; t++)
        {
            cpu.Tick();
            lines.AppendLine($"t{t}: PC=${cpu.PC:X4} cyc={cpu.DebugCycle} op={cpu.DebugOpcode:X2} bound={cpu.IsInstructionBoundary}");
            if (t > 1 && cpu.IsInstructionBoundary && cpu.PC == 0x1002)
            {
                lines.AppendLine($"fallthrough at t={t}");
                break;
            }
        }
80→        throw new Xunit.Sdk.XunitException(lines.ToString());
    }
}



[... TRUNCATED at 524288 bytes, 332 turns omitted ...]
