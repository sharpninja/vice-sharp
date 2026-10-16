# HISTORICAL -- DO NOT EDIT
# Record of compaction segment 015 (detail=verbose) from this same task.
# Use read_file or grep to look up details, but do not modify.

## Segment metadata
- Index: 015
- Turn count: 491
- Timestamp: 2026-08-11T17:45:08Z

## Turn statistics

- Turns: 491 (Assistant=138, Function=333, Human=19, System=1)
- Tools used: read_file (97), run_terminal_command (85), search_replace (73), grep (43), get_command_or_subagent_output (17), write (8), todo_write (6), exit_plan_mode (2), list_dir (2)
- Unique target files (75): C:\Users\kingd\.agents\skills\commit-sync\SKILL.md, C:\Users\kingd\.agents\skills\wrap-up\SKILL.md, C:\Users\kingd\.claude\profile\PROFILE.md, C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md, C:\Users\kingd\.claude\profile\approve-before-execute.md, ... and 70 more
- Tool errors: 1
- Verbose-render size estimate: 1,283,840 B
- Last assistant response excerpt: "Cleaning up the botched capture loop."


## Summary (curated by compaction step)

Summary:
1. Primary Request and Intent: Operator Payton wants VIC-20 managed code faithful to VICE xvic (no invent); hostile-validator AGREE for Exact; full bit-exact whole-machine / pixel FB / cart+sound parity plan approved (BDPv4 with FR/TR/TEST + 100% AC coverage); finish that plan without stopping (goal harness). Also: refresh-docs + wrap-up with detailed handoff; MCP trust; no bulk probe-log commits; retire Azure remote; cancel Microsoft Store (emulators not allowed); then cancel Xbox UWP/Dev-Mode sideload too; high TODO PLAN-ZIPMEDIA-001 for zip virtual media; report xvic status. Labs go-by-default on PAYTON machines; receipts; no em-dashes; PowerShell; MCP not raw YAML.

2. Key Technical Concepts: xvic oracle (`vice_xvic.dll`); every-cycle CPU + VIC-I pipeline lockstep; READY first_x=48 crop; Mos6561 IndexFrameBuffer; capture first_x recompute from geometry; FE3 Flash040Core TYPE_B; BDPv4 FR/TR/AC/TEST; hostile validator; plan phases A–G; index Exact primary, BGRA via shared palette after alignment; origin=GitHub only after azure retired; Xbox/Store cancelled.

3. Files and Code Sections: Plan files at session `plan.md` and goal `goal/plan.md` (BDPv4 A–G). Key code: `Mos6561` IndexFrameBuffer + paint path; `IViceNative.TryCaptureFrameIndices`/`WriteBus`; `ViceNative.Xvic` P/Invokes; `native/vice-shim-vic20.c` capture rewrite (compute first_x, window_params, row_src clamp for NTSC); `FinalExpansion3Cartridge`+`Flash040Core`; tests `Vic20PixelLockstep*`, Flash040/Fe3, Vic20Video*; docs HANDOFF/README/USER-GUIDE/audit/wiki cancel banners; MCP PLAN-VIC20-EXACT-001, FR-VIC20-001, TR-VIC20-PIXEL-001, TEST-VIC20-001. Commits: 0cd61f8, e1cf592, 41d0eb9, 16bfbf1, d20d340, 85396c8 (scaffold) + later uncommitted capture fixes.

4. Errors and Fixes: invent crop/right-border → first_x; BGRA mismatch first_x/viewport → recompute first_x; NTSC last_line>=height → clamp row reads, keep timing out_h; BGRA RGB vs Tobias → index Exact + expand via shared PALette; MCP createFr AC as objects; FR IDs FR-VIC20-001 form; Index_ReadyPal red n=0 m=3 at x=296 → first_x fix then green; parallel Nuke/locks, WrapPanel→VariableSizedWrapGrid; no azure push.

5. Problem Solving: Closed invent crop; READY geometry Exact-scoped + AGREE; flash builder+FE3 FSM (erase latency Partial); pixel capture wired; Phase A largely green after first_x; plan A–G approved; goal executing autonomously. In-progress: finish NTSC height/capture cleanup, then B–G.

6. All User Messages: xvic/screenshot; right-border challenges; hostile validator until AGREE; redeploy; wrap BLKs; FE3/Mega/Ultimem builder plan all FE3 in scope; /refresh-docs + wrap-up handoff; resume checklist; Both (FE3+pixel) and stop using azure; high TODO zip virtual media; cancel Microsoft Store; cancel local Xbox UWP/Dev-Mode too; /refresh-docs + wrap-up; xvic conversion status?; close existing plan and plan full bit-exact whole VIC-20/pixel FB/cart+sound; revise plan for BDPv4 FR/TR/AC 100% TDD; plan approved / goal: finish plan without stopping.

7. Pending Tasks: Complete goal plan Phases A–G (pixel full green + NTSC/busy/BGRA; VIC residual; cart erase latency+Ultimem/Mega; sound export+PCM; workload CPU lockstep; snapshots; umbrella gate 0 fail/0 skip; audit/HANDOFF/MCP/receipts; hostile AGREE); verification logs under goal scratch; PLAN-ZIPMEDIA-001 open not in current goal.

8. Current Work: Autonomously executing goal plan. Phase A: PAL index SequenceEqual green after first_x recompute; added NTSC/busy/BGRA tests; WriteBus on IViceNative; cleaned/rewriting capture loop (window_params + row_src). Last edit: replaced messy capture with clean `vice_shim_vic20_window_params` / `vice_shim_vic20_row_src` / rewrite of `vice_machine_capture_visible_frame` start; index-capture function rewrite incomplete mid-file. Pixel gate last run: 7 pass / 1 fail (NTSC height 234 vs 233) then capture cleanup in progress; not yet re-verified after clean rewrite.

9. Optional Next Step: Finish index capture rewrite in `native/vice-shim-vic20.c` (mirror BGRA row clamp), rebuild xvic under MINGW64, re-run `FullyQualifiedName~Vic20Pixel` → 0 fail/0 skip, save `{SCRATCH}/vic20-pixel-gate.log`, check off Phase A checklist, then Phase B onward without stopping. Quote: “finish current plan without stopping” / Phase A task “Green Phase A… 0 fail 0 skip.”

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
Today's date: 2026-08-10
Note: Prefer using relative paths over absolute paths as tool call args when possible.
</user_info>

<git_status>
This is the git status at the start of the conversation. Note that this status is a snapshot in time, and will not update during the conversation.
## [32mmain[m...[31morigin/main[m
 [31mM[m build/Build.cs
 [31mM[m native/vice-shim-vic20.c
 [31mM[m native/vice-shim.h
 [31mM[m src/ViceSharp.Abstractions/IViceNative.cs
 [31mM[m src/ViceSharp.Architectures/Vic20/Vic20Descriptor.cs
 [31mM[m src/ViceSharp.Architectures/Vic20/Vic20Expansion.cs
 [31mM[m src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs
 [31mM[m src/ViceSharp.Avalonia/Views/SettingsView.axaml
 [31mM[m src/ViceSharp.Avalonia/Views/SettingsView.axaml.cs
 [31mM[m src/ViceSharp.Chips/Vic/Mos6561.cs
 [31mM[m src/ViceSharp.Core/ArchitectureBuilder.cs
 [31mM[m src/ViceSharp.Core/Vic20/Vic20MemoryLayout.cs
 [31mM[m src/ViceSharp.Core/Vic20/Vic20SystemRam.cs
 [31mM[m src/ViceSharp.Core/ViceNative.Xvic.cs
 [31mM[m src/ViceSharp.Core/ViceNative.cs
 [31mM[m src/ViceSharp.Host.InProcess/Runtime/ConsoleEmulatorHost.cs
 [31mM[m src/ViceSharp.Host.InProcess/Runtime/DefaultEmulatorRuntimeFactory.cs
 [31mM[m src/ViceSharp.Host.InProcess/Runtime/EmulatorRuntimeSession.cs
 [31mM[m src/ViceSharp.Host.InProcess/Services/HostProtocolMapper.cs
 [31mM[m src/ViceSharp.Host.InProcess/Services/MediaServiceHost.cs
 [31mM[m src/ViceSharp.Host.InProcess/Services/SettingsServiceHost.cs
 [31mM[m src/ViceSharp.Protocol/ProtocolContracts.cs
 [31mM[m src/ViceSharp.Protocol/SettingsOptionCatalog.cs
 [31mM[m src/ViceSharp.Xbox.ViewModels/AboutInfo.cs
 [31mM[m src/ViceSharp.Xbox.ViewModels/AboutViewModel.cs
 [31mM[m src/ViceSharp.Xbox.ViewModels/NavigationDestination.cs
 [31mM[m src/ViceSharp.Xbox.ViewModels/VideoFramePullViewModel.cs
 [31mM[m src/ViceSharp.Xbox.ViewModels/XboxSettingsViewModel.cs
 [31mM[m src/ViceSharp.Xbox/App.xaml.cs
 [31mM[m src/ViceSharp.Xbox/Views/SettingsPage.xaml
 [31mM[m src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 [31mM[m tests/ViceSharp.TestHarness/Vic20/Vic20DivergeProbe.cs
 [31mM[m tests/ViceSharp.TestHarness/Vic20/Vic20MemoryMapTests.cs
 [31mM[m tests/ViceSharp.TestHarness/Vic20/Vic20VideoTests.cs
 [31mM[m tests/ViceSharp.TestHarness/Xbox/AboutViewModelTests.cs
 [31mM[m tests/ViceSharp.TestHarness/Xbox/Fakes/FakeXboxSettingsGateway.cs
 [31mM[m tests/ViceSharp.TestHarness/Xbox/VideoFramePullViewModelTests.cs
 [31mM[m tests/ViceSharp.TestHarness/Xbox/XboxComputerModelPickerTests.cs
 [31mM[m tests/ViceSharp.TestHarness/Xbox/XboxSettingsViewModelTests.cs
[31m??[m docs/10s-lockstep-2026-08-07.log
[31m??[m docs/10s-lockstep-blankfix-2026-08-06.log
[31m??[m docs/10s-lockstep-idle-2026-08-06.log
[31m??[m docs/10s-lockstep-memptr2-2026-08-06.log
[31m??[m docs/10s-lockstep-ntsc-2026-08-07.log
[31m??[m docs/10s-lockstep-rst6-2026-08-06.log
[31m??[m docs/2s-adc-2026-08-06.log
[31m??[m docs/2s-adc2-2026-08-06.log
[31m??[m docs/2s-and-imm-2026-08-06.log
[31m??[m docs/2s-arm-bcc-2026-08-06.log
[31m??[m docs/2s-asl-2026-08-06.log
[31m??[m docs/2s-asl-bcs-2026-08-06.log
[31m??[m docs/2s-b1-beq-2026-08-06.log
[31m??[m docs/2s-b1stage-2026-08-06.log
[31m??[m docs/2s-bcc-hold-2026-08-06.log
[31m??[m docs/2s-bccsta-2026-08-06.log
[31m??[m docs/2s-bcs-stage-2026-08-06.log
[31m??[m docs/2s-beq-2026-08-06.log
[31m??[m docs/2s-beq-hold-2026-08-06.log
[31m??[m docs/2s-beqpla-2026-08-06.log
[31m??[m docs/2s-bit-2026-08-06.log
[31m??[m docs/2s-bit2-2026-08-06.log
[31m??[m docs/2s-bit3-2026-08-06.log
[31m??[m docs/2s-bit4-2026-08-06.log
[31m??[m docs/2s-bit5-2026-08-06.log
[31m??[m docs/2s-bitf-2026-08-06.log
[31m??[m docs/2s-bitf2-2026-08-06.log
[31m??[m docs/2s-bmi-chain-2026-08-06.log
[31m??[m docs/2s-bne-cmp-2026-08-06.log
[31m??[m docs/2s-bne-stage-2026-08-06.log
[31m??[m docs/2s-bnehold-2026-08-06.log
[31m??[m docs/2s-bpl3-2026-08-06.log
[31m??[m docs/2s-bpl4-2026-08-06.log
[31m??[m docs/2s-bpl6-2026-08-06.log
[31m??[m docs/2s-bpl7-2026-08-06.log
[31m??[m docs/2s-br-irq-2026-08-06.log
[31m??[m docs/2s-cmp-absx-2026-08-06.log
[31m??[m docs/2s-cmpzp-2026-08-06.log
[31m??[m docs/2s-cmpzp2-2026-08-06.log
[31m??[m docs/2s-colorram-2026-08-06.log
[31m??[m docs/2s-cpx-soft-2026-08-06.log
[31m??[m docs/2s-delay1-2026-08-06.log
[31m??[m docs/2s-delays-block-2026-08-06.log
[31m??[m docs/2s-delays-final-2026-08-06.log
[31m??[m docs/2s-delays-irq-2026-08-06.log
[31m??[m docs/2s-eor-fuse-2026-08-06.log
[31m??[m docs/2s-eor-imm-2026-08-06.log
[31m??[m docs/2s-final-2026-08-06.log
[31m??[m docs/2s-flag-only-2026-08-06.log
[31m??[m docs/2s-fuse-narrow-2026-08-06.log
[31m??[m docs/2s-hold-narrow-2026-08-06.log
[31m??[m docs/2s-holdindy-2026-08-06.log
[31m??[m docs/2s-incsticky-2026-08-06.log
[31m??[m docs/2s-indy-diag-2026-08-06.log
[31m??[m docs/2s-indy-nz-2026-08-06.log
[31m??[m docs/2s-inx-2026-08-06.log
[31m??[m docs/2s-iny-fuse-2026-08-06.log
[31m??[m docs/2s-irq-fuse-lda-2026-08-06.log
[31m??[m docs/2s-irq2-2026-08-06.log
[31m??[m docs/2s-irqb-2026-08-06.log
[31m??[m docs/2s-irqdelay0-2026-08-06.log
[31m??[m docs/2s-irqdiag-2026-08-06.log
[31m??[m docs/2s-irqfall-2026-08-06.log
[31m??[m docs/2s-irqrmw-2026-08-06.log
[31m??[m docs/2s-irqsuppress-2026-08-06.log
[31m??[m docs/2s-jmp-2026-08-06.log
[31m??[m docs/2s-jmp-after-jsr-2026-08-06.log
[31m??[m docs/2s-jmp-fuse-2026-08-06.log
[31m??[m docs/2s-jmp2-2026-08-06.log
[31m??[m docs/2s-jmp3-2026-08-06.log
[31m??[m docs/2s-jsr-irq-2026-08-06.log
[31m??[m docs/2s-jsr-jmpind-2026-08-06.log
[31m??[m docs/2s-jsr-jump-2026-08-06.log
[31m??[m docs/2s-jsr-ldy-2026-08-06.log
[31m??[m docs/2s-lda-2026-08-06.log
[31m??[m docs/2s-lda-abs-bcc-2026-08-06.log
[31m??[m docs/2s-lda-after-sec-2026-08-06.log
[31m??[m docs/2s-lda-imm-chain-2026-08-06.log
[31m??[m docs/2s-lda-indy-2026-08-06.log
[31m??[m docs/2s-lda-sbc-2026-08-06.log
[31m??[m docs/2s-lda-zp-branch-2026-08-06.log
[31m??[m docs/2s-ldax-tsx-2026-08-06.log
[31m??[m docs/2s-ldx-jmp-2026-08-06.log
[31m??[m docs/2s-ldy-fuse-2026-08-06.log
[31m??[m docs/2s-ldy-plain-2026-08-06.log
[31m??[m docs/2s-lsrx-2026-08-06.log
[31m??[m docs/2s-lsrzp-2026-08-06.log
[31m??[m docs/2s-lsrzp2-2026-08-06.log
[31m??[m docs/2s-memdiag-2026-08-06.log
[31m??[m docs/2s-nib-2026-08-06.log
[31m??[m docs/2s-ora-2026-08-06.log
[31m??[m docs/2s-pla-2026-08-06.log
[31m??[m docs/2s-pla-late-2026-08-06.log
[31m??[m docs/2s-pla2-2026-08-06.log
[31m??[m docs/2s-post-store-rts-2026-08-06.log
[31m??[m docs/2s-rola-fuse-2026-08-06.log
[31m??[m docs/2s-ror-chain-2026-08-06.log
[31m??[m docs/2s-ror-chain2-2026-08-06.log
[31m??[m docs/2s-ror-rti-2026-08-06.log
[31m??[m docs/2s-ror-zp-beq-2026-08-06.log
[31m??[m docs/2s-rora-soft-2026-08-06.log
[31m??[m docs/2s-rora-trail2-2026-08-06.log
[31m??[m docs/2s-rti-2026-08-06.log
[31m??[m docs/2s-rti2-2026-08-06.log
[31m??[m docs/2s-rts-load-2026-08-06.log
[31m??[m docs/2s-rts-store-br-2026-08-06.log
[31m??[m docs/2s-rts-trail-2026-08-06.log
[31m??[m docs/2s-rts-trail2-2026-08-06.log
[31m??[m docs/2s-rtsand-2026-08-06.log
[31m??[m docs/2s-rtsfl-2026-08-06.log
[31m??[m docs/2s-sec-2026-08-06.log
[31m??[m docs/2s-sec-bne-2026-08-06.log
[31m??[m docs/2s-shift-phase-2026-08-06.log
[31m??[m docs/2s-shift-taken-2026-08-06.log
[31m??[m docs/2s-skip-latch-2026-08-06.log
[31m??[m docs/2s-sta-bne-2026-08-06.log
[31m??[m docs/2s-sta-irq-2026-08-06.log
[31m??[m docs/2s-sta-load-2026-08-06.log
[31m??[m docs/2s-sta-trail-2026-08-06.log
[31m??[m docs/2s-stazp-2026-08-06.log
[31m??[m docs/2s-sticky-clear-2026-08-06.log
[31m??[m docs/2s-sticky2-2026-08-06.log
[31m??[m docs/2s-store-2026-08-06.log
[31m??[m docs/2s-store-hold-2026-08-06.log
[31m??[m docs/2s-store-hold2-2026-08-06.log
[31m??[m docs/2s-store-irq-2026-08-06.log
[31m??[m docs/2s-sty-trail-2026-08-06.log
[31m??[m docs/2s-tax-chain-2026-08-06.log
[31m??[m docs/2s-tay-2026-08-06.log
[31m??[m docs/2s-txa-fuse-2026-08-06.log
[31m??[m docs/2s-vbus-2026-08-06.log
[31m??[m docs/2s-vbus-2s-green-2026-08-06.log
[31m??[m docs/2s-vbus-best-2026-08-06.log
[31m??[m docs/2s-vbus-delay3-2026-08-06.log
[31m??[m docs/2s-vbus-diag-2026-08-06.log
[31m??[m docs/2s-vbus-dump-green-2026-08-06.txt
[31m??[m docs/2s-vbus-final-2026-08-06.log
[31m??[m docs/2s-vbus-final2-2026-08-06.log
[31m??[m docs/2s-vbus-noread-2026-08-06.log
[31m??[m docs/2s-vbus-openh-2026-08-06.log
[31m??[m docs/2s-vbus-order-2026-08-06.log
[31m??[m docs/2s-vbus-order2-2026-08-06.log
[31m??[m docs/2s-vbus-phase-2026-08-06.log
[31m??[m docs/2s-vbus-phase1-2026-08-06.log
[31m??[m docs/2s-vbus-vert-2026-08-06.log
[31m??[m docs/2s-vbus2-2026-08-06.log
[31m??[m docs/2s-via-cont-2026-08-06.log
[31m??[m docs/2s-via-t1-2026-08-06.log
[31m??[m docs/2s-via1iec-2026-08-06.log
[31m??[m docs/S-Blox/
[31m??[m docs/_deploy-desktop-2026-08-06.log
[31m??[m docs/_deploy-desktop-2026-08-06b.log
[31m??[m docs/_deploy-desktop-2026-08-08_015554.log
[31m??[m docs/_deploy-desktop-2026-08-08_025358.log
[31m??[m docs/_deploy-desktop-2026-08-08_032741.log
[31m??[m docs/_deploy-desktop-2026-08-08_045348.log
[31m??[m docs/_deploy-desktop-2026-08-08_045348.log.err
[31m??[m docs/_deploy-desktop-2026-08-08_045445.log
[31m??[m docs/_deploy-desktop-2026-08-08_045501.log
[31m??[m docs/_deploy-last-pair.txt
[31m??[m docs/_deploy-xbox-2026-08-06.log
[31m??[m docs/_deploy-xbox-2026-08-06b.log
[31m??[m docs/_deploy-xbox-2026-08-08_015554.log
[31m??[m docs/_deploy-xbox-2026-08-08_025358.log
[31m??[m docs/_deploy-xbox-2026-08-08_032741.log
[31m??[m docs/_deploy-xbox-2026-08-08_045348.log
[31m??[m docs/_deploy-xbox-2026-08-08_045348.log.err
[31m??[m docs/_vs-ready-2026-08-08-crop.png
[31m??[m docs/_vs-ready-2026-08-08-latest.png
[31m??[m docs/_vs-ready-2026-08-08-prev-crop.png
[31m??[m docs/_vs-ready-2026-08-08-prev.png
[31m??[m docs/adc-focused-2026-08-06.log
[31m??[m docs/adc2-focused-2026-08-06.log
[31m??[m docs/asl-focused-2026-08-06.log

... (git status truncated)
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
/refresh-docs and /mcpserver:wrap-up with detailed handoff
</user_query>

### Turn 4 (Human)
This session is being continued from a previous conversation that ran out of context. The summary below covers the earlier portion of the conversation.

Summary:
1. Primary Request and Intent: Operator (Payton) wants VIC-20 managed code faithful to VICE xvic (no invent); hostile Grok sub-agent validation for Exact claims; video not just CPU lockstep; redeploy desktop+Xbox; screenshots vs xvic READY. Fixed present crop (first_x, L+R borders); wrap BLK toggles; UWP expansion-cart attach/eject; plan+impl flash cart image builder (FE3/MegaCart/Ultimem UI + C64 EasyFlash layout reuse; all FE3 features in scope except residual flash040 FSM). Latest explicit ask: /refresh-docs and /mcpserver:wrap-up with detailed handoff (MCP trust, wiki.yaml, requirements export, validation, session-log closeout, commit via commit-sync contract). Lab go-by-default on PAYTON-DESKTOP/LEGION2; accuracy/receipts; no em-dashes; PowerShell; MCP not raw YAML.

2. Key Technical Concepts: VICE full line + border paint + viewport first_x (READY PAL L48+paper352+R48); src=0 was invent; CRT domain reason for borders; C64 flip-flop borders contrast; Byrd TDD; FlashCarts (IFlashCartProfile, FlashImageBuilder, FE3 512K / Ultimem 1MB / MegaCart+NVRAM / EasyFlash layout); Portable FlashCartImageBuilderViewModel; MediaSlot.Cartridge size detect attach; UWP VariableSizedWrapGrid (no WrapPanel); Hostile-validator AGREE receipts; MCP workflow.requirements.* / sessionlog / commit-sync; AGENTS-README-FIRST: port 7147, baseUrl PAYTON-LEGION2, workspace F:\GitHub\vice-sharp, Grok plugin.

3. Files and Code Sections: Mos6561 present first_x crop, BorderX=48, display_xstop border blank, no paper strip; Vic20VideoTests geometry/latch/space; audit-vic20-vs-vice rebaselined; Xbox SettingsPage VariableSizedWrapGrid BLKs, expansion attach/eject, Build cart image → FlashCartBuilderPage; Avalonia Settings WrapPanel BLKs + FlashCartBuilderView dialog; Core/FlashCarts/* + tests FlashCart/* Fe3Preset*; FinalExpansion3Cartridge presets flash/super-rom/rom-ram/ram2; ExpansionCartManageState Fe3Presets; NavigationDestination.FlashCartBuilder; XboxSettingsViewModel expansion image APIs; docs/receipts/hostile-validator-20260808T094911Z (borders AGREE), 20260808T103211Z (flash AGREE); deploy logs InstallMsi 1.2.7 + DeployXboxLocal; plan.md FE3 full scope amendment. Refresh/wrap-up interrupted after reading skills + marker prefix (apiKey present in marker).

4. Errors and Fixes: Claimed no right border from clipped capture → first_x crop; parallel Nuke lock / InstallMsi Debug rejected → sequential Release; WrapPanel unknown UWP → VariableSizedWrapGrid; xUnit1051 CancellationToken; Xbox restore net10.0 vs UWP TFM thrashing; StatusText private set; self-validator rejected → spawn hostile skill AGREE only.

5. Problem Solving: Closed invent crop/strip/eager latch; READY geometry Exact scoped; flash builder core+UI+tests+AGREE; FE3 presets expanded, flash040 residual Partial; refresh-docs+wrap-up not finished (MCP auth failed for mcpserver).

6. All User Messages (substance): Start xvic/screenshot; challenged no right border / on drugs / white surrounded; CRT border domain lecture; C64 correct / audit; skipped audit / hostile request; Do not stop until hostile agrees; redeploy both; wrap BLKs; UWP lack MegaCart/Ultimem/FE3 content UI; plan builder FE3/Mega/Ultimem+C64 reuse Byrd; approve plan All FE3 features in scope; /refresh-docs and /mcpserver:wrap-up with detailed handoff.

7. Pending Tasks: Complete refresh-docs (docs inventory, README, wiki.yaml, prune, MCP requirements export if trusted); wrap-up (health/nonce/signature, requirements reconcile+export, validation evidence, commit-sync if required, session-log append/completeTurn, detailed handoff); residual FE3 flash040; optional VicIStub cleanup; optional desktop InstallMsi for Avalonia builder.

8. Current Work: User requested refresh-docs + wrap-up with detailed handoff. Read refresh-docs SKILL, wrap-up SKILL, start of AGENTS-README-FIRST.yaml (endpoints/keys). MCP servers note: mcpserver auth required/failed. No docs edits or exports executed yet.

9. Optional Next Step: Finish the latest ask: verify MCP marker signature/nonce via Grok plugin repl-invoke; if trusted run requirements generateDocument wiki zip, refresh docs/wiki/README for FlashCart/VIC-20, validation, session-log completeTurn, detailed handoff; if untrusted MCP_UNTRUSTED then non-MCP docs refresh + report wrap-up blocked. Quote: “/refresh-docs and /mcpserver:wrap-up with detailed handoff” after skill load; marker read only started.

Full verbatim rollouts of previous segments are available at C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction/segment_*.md.  See C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\compaction/INDEX.md for a table of contents.  Use read_file or grep to recover specific details (exact code, file paths, tool outputs) if this summary is insufficient.  Do NOT modify these files.

### Turn 5 (Human)
<system-reminder>
## Available Skills
The following skills are available for use:

- refresh-docs: Refresh a repo's project documentation and its README.md, prune out-of-date docs, update docs/wiki.yaml with the project docs to publish to the wiki, then ex…
  Use when: the user says "refresh docs", "/refresh-docs", "refresh the documentation", "update the README", "prune stale docs", "update the wiki manifest / docs/wiki.yaml", or asks to reconcile docs with the current code and re-export requirements …
  Absolute path: C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md
- check-work: Check your work with a verification subagent that reviews diffs, runs builds and tests, and evaluates correctness. Read this file for instructions
  Use when: asked to "check work", "verify changes", "self-verify", "/check-work", "/check", "/verify", or "/self-verify".
  Absolute path: C:\Users\kingd\.grok\skills\check-work\SKILL.md
- create-skill: Interactively create a new Grok skill (SKILL.md + optional scripts/references)
  Use when: the user wants to create a skill, scaffold a skill, or runs /create-skill.
  Absolute path: C:\Users\kingd\.grok\skills\create-skill\SKILL.md
- help: Grok documentation and configuration help
  Use when: users ask about setup, configuration, MCP servers, authentication, skills, slash commands, keyboard shortcuts, or any Grok feature. Also use proactively when you detect a user is having trouble with setup or onboarding.
  Absolute path: C:\Users\kingd\.grok\skills\help\SKILL.md
- hostile-validator: Adversarial Grok sub-agent that independently validates implementer status claims. Use on every Use Case plan step and every status report that asserts pass/done/green/complete. Not a PowerShell script. Not Codex. Not the implementer's self-check.
  Absolute path: C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md
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
- use-case-management: Use when the user asks to "list use cases", "create use case", "link use case to FR", "use case diagram", "use case coverage", "approve use case", or manage use case product keys.
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-usecase\SKILL.md
- workspace-initialization: This skill should be used when the user asks to "initialize workspace", "register workspace", "add workspace", "create workspace marker", or "bootstrap MCP workspace"
  Absolute path: C:\Users\kingd\.grok\skills\mcpserver-workspace\SKILL.md
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
- skill-design-principles: Concise, high-signal principles for writing and editing skills well
  Use when: Use whenever authoring or editing a skill.
  Absolute path: C:\Users\kingd\.grok\bundled\skills\skill-design-principles\SKILL.md
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
- usecase: Use when the user asks to "list use cases", "create use case", "link use case to FR", "use case diagram", "use case coverage", "approve use case", or manage use case product keys.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/usecase\SKILL.md
- workspace: Use when the user asks to "initialize workspace", "register workspace", "add workspace", "create workspace marker", or "bootstrap MCP workspace"
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/workspace\SKILL.md
- wrap-up: Use when asked to "wrap up", "export requirements", or "close out" MCP-backed work: reconcile MCP requirements, export wiki documents, run validation, commit and push via the commit-sync contract, reconcile the session log, and complete the turn.
  Absolute path: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/wrap-up\SKILL.md

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
I'll run `/refresh-docs` and `/mcpserver:wrap-up` with a detailed handoff. Loading the skills, marker, and profile first.
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.agents\skills\wrap-up\SKILL.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
- limit: 200
[tool_request: read_file]
- target_file: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills\wrap-up\SKILL.md

### Turn 7 (Function)
[tool_response]
1→---
name: refresh-docs
description: Refresh a repo's project documentation and its README.md, prune out-of-date docs, update docs/wiki.yaml with the project docs to publish to the wiki, then export MCP requirement documents. Use when the user says "refresh docs", "/refresh-docs", "refresh the documentation", "update the README", "prune stale docs", "update the wiki manifest / docs/wiki.yaml", or asks to reconcile docs with the current code and re-export requirements after a feature or refactor lands.
---

# Refresh Docs

Reconcile `docs/` with the current codebase, remove documentation that no longer applies, make `docs/wiki.yaml` list every project doc that should be published to the wiki, and export the MCP-backed requirement documents.

10→## Preconditions

- Work from the repository root.
- Read `AGENTS-README-FIRST.yaml` and follow its marker, plugin, session, and trust rules. Verify the marker signature and the `/health` nonce before any MCP mutation.
- Use the required MCP Server plugin for your agent (Claude/Codex/Grok) for requirements and export (`workflow.requirements.*`). Do not hand-author exported requirement docs or substitute raw REST.
- Use the operator's approved shell for all shell invocation (e.g. PowerShell.Mcp on Windows). Build YAML/JSON from native objects and serialize; never handwrite fragile payloads.
- Read facts from the code, not from memory. This skill refreshes and exports only; it does not commit or push (leave that to a wrap-up flow or an explicit request).

## Step 1 - Refresh project documentation

20→1. Inventory the docs surface: `docs/user/*`, `docs/architecture/*`, `docs/requirements/*.md`, `docs/testing/*`, `docs/*.md`, and `README.md` (adapt to the repo's actual layout).
2. Establish what changed since the docs were last touched: `git log --oneline` for the changed code areas, the current project/solution structure, public commands, and settings. Prefer the shipped behavior over any doc's prior claims.
3. Update each doc that drifted: renamed or removed components, changed UI/behavior, new or removed features, install commands and version pins, and cross-references. Match the existing doc voice; no em-dashes.
4. Keep the project `README.md` current as a first-class refresh target, not an afterthought: the project description and overview, the feature list, install and quickstart/usage commands, current version references, badges, and links into `docs/` must all match the shipped state. Reconcile it against the code and the refreshed docs on every run.
5. If the repo ships a documentation screenshot/capture script (for example `scripts/Capture-ClientToolScreenshots.ps1`) and the UI changed, regenerate the embedded screenshots into the docs image folder from the current build, and verify a raw capture reflects the current UI before keeping it.

## Step 2 - Prune out-of-date docs

1. Find docs describing removed or superseded features: grep the docs for names of deleted components, APIs, or flows and confirm against the code that they no longer exist.
2. Delete the obsolete `.md` files, and remove their references from index pages (for example `docs/user/index.md`) and from `docs/wiki.yaml`.
30→3. Do not delete a doc that still has accurate content. When a doc is partly stale, update it rather than delete. When unsure whether to remove a doc, flag it in the report instead of deleting.

## Step 3 - Update docs/wiki.yaml

`docs/wiki.yaml` uses schema `mcp-wiki-export/v1` with `home.document`, a `documents` list, and a `navigation` tree. Each `documents` entry is `{ id, title, source, target, platforms }`.

1. Keep the server-generated requirement docs (their `source` is `generated:<key>`, for example `generated:home|functional|technical|testing|mapping|matrix`). Do not point these at files.
2. Add or refresh a `documents` entry for every CURRENT project doc that should appear in the wiki (the user guides under `docs/user/` and the architecture docs under `docs/architecture/`, plus `docs/Development-Process.md` / `README.md` if desired):
   - `source`: the repo-relative path to the markdown file, for example `docs/user/client-tool.md`.
   - `target`: the wiki page filename in PascalCase-with-hyphens, for example `Client-Tool.md`.
40→   - `title`: the human page title.
   - `platforms`: `[github, azure]` unless a doc is platform-specific.
   Use stable `id` slugs (for example `user-client-tool`, `arch-overview`).
3. Remove `documents` and `navigation` entries for any doc pruned in Step 2 or no longer present on disk.
4. Add each new document to `navigation` (as a child of the appropriate parent, or top-level). Keep the generated requirement docs grouped as they are.
5. If the repo has a wiki sync script (for example `scripts/Sync-GitHubWikiDocs.ps1`) with its own project-doc list, cross-check it against `docs/wiki.yaml` so the manifest and the sync script agree on which project docs publish and their page names.
6. Confirm the file still parses as YAML after editing.

## Step 4 - Export requirements

50→1. Export via the MCP requirements workflow: `workflow.requirements.generateDocument` with `format: wiki` and `docType: all`.
2. Write the returned `contentBase64` ZIP exactly to `docs/requirements/requirements-wiki-documents.zip`. The MCP database is the source of truth; do not hand-author the exported docs.
3. If the server also regenerates in-place markdown under `docs/Project/wiki/`, review the diff. Keep only the intended requirement changes. Do NOT accept destructive deletions of project-doc wiki pages that are listed in `docs/wiki.yaml`; that means the manifest and the export disagree, so fix `docs/wiki.yaml` in Step 3 rather than accept the deletions.
4. If the server rejects `format: wiki`, record it as a blocker and stop; do not fall back to another format unless the user asks.

## Validation and report

- Run `git diff --check`; confirm `docs/wiki.yaml` parses; if code samples in docs changed, sanity-build the relevant snippet.
- Report only verified facts: `README.md` updated, docs updated, docs pruned (with reasons), `docs/wiki.yaml` entries added/removed, screenshots regenerated, the requirements export artifact path, and any docs you flagged instead of deleting or any blockers.
- Do not commit or push; summarize the changes and let the operator run a wrap-up or commit explicitly.
60→

### Turn 8 (Function)
[tool_response]
1→---
name: wrap-up
description: Finish and synchronize MCP-backed work using mcpserver-codex-plugin. Use when the user asks to wrap up, close out, export requirements, update MCP requirements, reconcile session logs, commit and sync, or push completed work to origin.
---

# Wrap Up

Close out an MCP-backed task with traceability, generated requirement docs, a focused commit, and a clean session-log record.

10→## Preconditions

- Work from the active repository root.
- Read the repo's `AGENTS-README-FIRST.yaml` and follow its active marker, plugin, session, TODO, and trust rules.
- Use `mcpserver-codex-plugin` for MCP operations. Do not substitute raw REST, generic helper modules, or another agent plugin for normal requirements, exports, TODO, or session-log mutation.
- Bootstrap with `workflow.sessionlog.bootstrap` through the plugin wrapper before using `workflow.sessionlog.*` or `workflow.requirements.*`.
- If the required plugin is unavailable, marker verification fails, health nonce verification fails, or MCP returns authentication failures, stop MCP mutations, record the blocker if a trusted session-log path is still available, and report the blocker.

## Workflow

20→1. **Open or continue a session-log turn** before wrap-up work:
   - Use `workflow.sessionlog.beginTurn` when the current user request is not already represented by an active turn.
   - Use `workflow.sessionlog.queryHistory` first when reconciling older work, so missing turns, decisions, commits, or actions are appended once instead of duplicated.

2. **Reconstruct the actual work state**:
   - Inspect `git status --short`, `git diff`, recent commits, branch/upstream state, generated artifacts, test output, and repo handoff/session artifacts.
   - Distinguish intended changes from unrelated dirty or untracked files.
   - Do not stage or revert unrelated files unless the user explicitly asks.

3. **Update newly captured MCP requirements**:
30→   - Refresh live inventory with `workflow.requirements.listFr`, `workflow.requirements.listTr`, `workflow.requirements.listTest`, and `workflow.requirements.listMappings`.
   - Create or update only requirements that were newly captured or materially changed during the task.
   - Prefer update over duplicate when an ID or title already exists.
   - Use `workflow.requirements.createFr` or `updateFr` for functional requirements.
   - Use `workflow.requirements.createTr` or `updateTr` for technical requirements.
   - Use `workflow.requirements.createTest` or `updateTest` for test requirements.
   - Use `workflow.requirements.createMapping` and `deleteMapping` to keep traceability current.
   - Every new FR must map to at least one TR and at least one TEST before export.

4. **Export requirements documents in Wiki format**:
40→   - Call `workflow.requirements.generateDocument` with:
     - `format: wiki`
     - `docType: all`
   - Treat the MCP database as source of truth. Do not hand-author exported requirement docs.
   - Preserve the server-generated artifact exactly. For wiki export, write the returned `contentBase64` ZIP to `docs/requirements/requirements-wiki-documents.zip` unless the repo has a more specific existing convention.
   - If the MCP server rejects `format: wiki`, log it as a blocker. Export another format only when the user explicitly accepts a fallback or the repo's current instructions already define that fallback.

5. **Validate before commit**:
   - Run focused tests or builds that match the changed surface.
   - For docs-only or skill-only closeout, run at least `git diff --check` and any local skill/document validation available.
50→   - Do not claim validation passed unless the command completed successfully.

6. **Commit and sync to origin**:
   - Recheck `git status --short`.
   - Stage only the intended files for this wrap-up.
   - Commit with a concise conventional message that matches the actual changes.
   - Push the current branch to `origin`. If the branch has no upstream, push the current branch to `origin` and set upstream only when that is appropriate for the repo workflow.
   - Capture the commit SHA, branch, remote, and push result.

7. **Reconcile the MCP session log**:
60→   - Append missing actions for requirements updates, wiki export artifact path, validation commands, commit SHA, push result, and intentionally excluded dirty files.
   - Add design decisions with both dialog context and an action when the decision changed implementation or traceability.
   - Add missing commits or important actions discovered from `git log` or `queryHistory`, but do not duplicate existing records.
   - Complete the active turn with `workflow.sessionlog.completeTurn` after sync succeeds.
   - Use `workflow.sessionlog.failTurn` only when wrap-up cannot complete due to a real blocker.

## Final Response

Report only verified facts:

70→- Requirement IDs created or updated.
- Wiki export artifact path.
- Validation commands and outcomes.
- Commit SHA.
- Pushed branch and remote.
- Remaining dirty/untracked files intentionally left out.
- Any blockers or fallback decisions.

When a commit or push was actually performed in the current turn, emit the app's git directives in the final response after the human-readable summary.


### Turn 9 (Function)
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


### Turn 10 (Function)
[tool_response]
1→port: 7147
baseUrl: http://PAYTON-LEGION2:7147
apiKey: ulj_Nt7WaXp24dEQ87mNkCUrbE0l8N-ms85Vk4nDxIE
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
pid: 5456
startedAt: 2026-08-09T02:03:48.7373830+00:00
markerWrittenAtUtc: 2026-08-09T02:03:48.7373830+00:00
serverStartedAtUtc: 2026-08-09T02:03:35.0179222+00:00
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
  value: 3D1004A2C7A0FCFA837454F743EA60290BD8BB54574E23C54AC51DD73DE5F3E7
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
  contract_digest: 624DF9D3F2DE21D6BD027E0724101F4AD6D765AAC58A00ADFE5CEF3B5FB74308
  agents:
    Codex:
      source_type: Codex
80→      plugin_name: mcpserver-codex-plugin
      plugin_version: 1.85.0
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
      plugin_version: 1.85.0
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
      plugin_version: 1.85.0
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
150→      plugin_version: 1.85.0
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
170→      plugin_version: 1.85.0
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

### Turn 11 (Function)
[tool_response]
1→---
name: Wrap-Up
description: 'Use when asked to "wrap up", "export requirements", or "close out" MCP-backed work: reconcile MCP requirements, export wiki documents, run validation, commit and push via the commit-sync contract, reconcile the session log, and complete the turn.'
version: 0.1.0
---

Finish and synchronize MCP-backed work: reconcile requirements, export wiki documents, run validation, commit/push, and close out the session-log turn.

## Trust and transport
10→
Trust marker details only after the plugin's local status/bootstrap check confirms marker trust and workspace health (including signature and nonce health). Use the plugin's supported wrapper surface for MCP mutations (for example `lib/repl-invoke.ps1`, or the plugin's session, todo, and requirements tools); do not use raw REST for normal MCP mutations.

## Method-name disambiguation

`workflow.*` names in this skill (`workflow.sessionlog.*`, `workflow.todo.*`, `workflow.requirements.*`) are plugin workflow/REPL method names, not literal native MCP tool names. Native `/mcp-transport` tools use names such as `sessionlog_*`, `todo_*`, and `requirements_*` (for example `sessionlog_submit`, `todo_list`, `requirements_generate`); hosted-agent adapters may expose `mcp_*` aliases. Do not declare the plugin unavailable solely because generic MCP discovery does not list literal `workflow.*` names.

If your agent exposes only a `pwsh` (or shell) tool, invoke the plugin shim directly instead of declaring it unavailable:

```
20→pwsh.exe -NoProfile -NonInteractive -File "<plugin-root>\lib\repl-invoke.ps1" -Method <workflow.method> -ParamsYaml <yaml>
```

## Wrap-up steps

1. Reconcile requirements with `workflow.requirements.*`, including requirement reconciliation for new FR/TR/TEST evidence.
2. Export wiki requirements with `workflow.requirements.generateDocument` using `format: wiki` and `docType: all`.
3. Run validation commands and keep zero-failure zero-skip evidence.
4. If commit/push is required, use the `commit-sync` pause and acknowledgement contract first.
5. Perform session-log reconciliation with `workflow.sessionlog.appendDialog` and `workflow.sessionlog.appendActions`.
30→6. Finish with `workflow.sessionlog.completeTurn`; use `workflow.sessionlog.failTurn` when validation, export, or commit/push cannot be completed (validation failure, export failure, or blocked commit/push).


### Turn 12 (Human)
<system-reminder>
The following skills are available for use:

- refresh-docs: Refresh a repo's project documentation and its README.md, prune out-of-date docs, update docs/wiki.yaml with the project docs to publish to the wiki, then ex…
  Use when: the user says "refresh docs", "/refresh-docs", "refresh the documentation", "update the README", "prune stale docs", "update the wiki manifest / docs/wiki.yaml", or asks to reconcile docs with the current code and re-export requirements …
  Absolute path: C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md
- wrap-up: Finish and synchronize MCP-backed work using mcpserver-codex-plugin
  Use when: the user asks to wrap up, close out, export requirements, update MCP requirements, reconcile session logs, commit and sync, or push completed work to origin.
  Absolute path: C:\Users\kingd\.agents\skills\wrap-up\SKILL.md
- add-profile: Load Payton's operator profile (identity, standing preferences, active-project context) into the current context
  Use when: the user says "add profile", "/add-profile", "load my profile", "remember who I am", or after context loss/compaction when the profile detail is gone.
  Absolute path: C:\Users\kingd\.claude\skills\add-profile\SKILL.md
</system-reminder>

### Turn 13 (Assistant)
Profile skill next, then MCP trust checks and a docs/git inventory in parallel.
[tool_request: run_terminal_command]
- command: Get-ChildItem "$env:USERPROFILE\.claude\profile\*.md" | Select-Object Name, FullName; Get-ChildItem "C:\Users\kingd\.grok\installed-plugins" -ErrorAction SilentlyContinue | Select-Object Name; Test-Path "F:\GitHub\mcpserver-grok-plugin"; Test-Path "C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f"
- description: List profile files and Grok plugin roots
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\PROFILE.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.claude\profile\user-payton-byrd.md
[tool_request: read_file]
- target_file: C:\Users\kingd\.agents\skills\commit-sync\SKILL.md
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
- offset: 200
- limit: 150

### Turn 14 (Function)
[tool_response]
1→---
name: commit-sync
description: >
  Pause all in-progress work, commit every dirty file in the current repo, push to origin,
  then resume. Use when the user says "commit-sync", "/commit-sync", "save my work",
  "sync to origin", "commit and push everything", or "checkpoint".
---

# commit-sync
10→
Interrupt current work, snapshot the repo to origin, then hand control back.

## Step 1 — Pause and get acknowledgement

Stop whatever you were doing mid-task. Do not stage, commit, or push yet.

Use `AskUserQuestion` with a single question:

- Header: `commit-sync`
20→- Question: "Ready to commit all dirty files and push to origin. Proceed?"
- Options:
  - `Proceed` — commit and push, then resume work
  - `Cancel` — skip the commit-sync and resume work unchanged

If the user selects **Cancel** (or chooses Other and expresses cancellation), skip all remaining steps and resume from where you paused.

## Step 2 — Inspect repo state

Run these in parallel:
30→
```
git status --short
git diff --stat
git log --oneline -5
git remote -v
```

Identify:
- All modified, added, deleted, and untracked files that belong to this repo
40→- The current branch name
- The configured `origin` remote URL
- Whether the branch already has an upstream tracking ref

Do not stage anything yet.

## Step 3 — Commit

Stage all dirty and untracked repo files:

50→```
git add -A
```

Generate a conventional commit message that summarizes the staged changes. Follow these rules:
- Format: `<type>(<scope>): <imperative summary>`
- Types: `feat`, `fix`, `refactor`, `docs`, `chore`, `build`, `style`, `test`, `ci`
- Subject line 72 chars max, no trailing period
- Add a body only when the staged diff contains non-obvious decisions or breaking changes
- Append the Co-Authored-By trailer: `Co-Authored-By: Codex Sonnet 4.6 <noreply@anthropic.com>`
60→
Commit using a HEREDOC to preserve formatting:

```bash
git commit -m "$(cat <<'EOF'
<generated message here>
EOF
)"
```

70→Capture the resulting commit SHA.

## Step 4 — Push

Push the current branch to `origin`:

```bash
git push origin HEAD
```

80→If the branch has no upstream tracking ref, set it:

```bash
git push -u origin HEAD
```

Capture the remote, branch, and push result (success or error output).

## Step 5 — Resume

90→Report what was done (compact, one block):

- Commit SHA and subject line
- Branch pushed and remote URL
- File count staged

Then immediately resume the task that was interrupted before Step 1, picking up exactly where you left off. State in one sentence what you are resuming.

## Error handling

100→- If `git add -A` fails: report the error, skip commit and push, resume work.
- If `git commit` fails (nothing to commit, hook failure, etc.): report the reason, skip push, resume work.
- If `git push` fails: report the error (do not force-push), resume work. Leave the local commit intact.
- Never use `--force`, `--no-verify`, or `--force-with-lease` unless the user explicitly requests it in their acknowledgement message.


### Turn 15 (Function)
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


### Turn 16 (Function)
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


### Turn 17 (Function)
[tool_response]
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
  - Header: X-Api-Key: ulj_Nt7WaXp24dEQ87mNkCUrbE0l8N-ms85Vk4nDxIE
  - Or query param: ?api_key=ulj_Nt7WaXp24dEQ87mNkCUrbE0l8N-ms85Vk4nDxIE
  If you receive a 401, re-read this marker file — the token rotates on each server restart.

  ## Where Things Live
340→
  - AGENTS.md — agent conduct, requirements tracking, session continuity, glossary
  - .github/copilot-instructions.md — build/test commands, architecture, coding conventions
  - docs/context/ — on-demand reference (schemas, module docs, compliance rules, action types)
  - docs/Project/ — requirements docs, TODO.yaml, mapping matrices
  - templates/ — prompt templates

  ## Context Loading by Task Type

  - Session logging → docs/context/session-log-schema.md + docs/context/module-bootstrap.md (pull module-bootstrap via MCP context)

### Turn 18 (Function)
[tool_response]
exit: 0

Name                                       FullName
----                                       --------
accuracy-first-verify-sources.md           C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md
add-profile.grok.md                        C:\Users\kingd\.claude\profile\add-profile.grok.md
approve-before-execute.md                  C:\Users\kingd\.claude\profile\approve-before-execute.md
bring-the-receipts.md                      C:\Users\kingd\.claude\profile\bring-the-receipts.md
lab-authorization.md                       C:\Users\kingd\.claude\profile\lab-authorization.md
log-decisions-as-conclusions.md            C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md
never-skip-explicit-actions.md             C:\Users\kingd\.claude\profile\never-skip-explicit-actions.md
no-attitude-honesty-tell.md                C:\Users\kingd\.claude\profile\no-attitude-honesty-tell.md
no-python-lab.md                           C:\Users\kingd\.claude\profile\no-python-lab.md
no-shortcuts-precision-over-convenience.md C:\Users\kingd\.claude\profile\no-shortcuts-precision-over-convenience.md
philosophical-dialogue-mode.md             C:\Users\kingd\.claude\profile\philosophical-dialogue-mode.md
PROFILE.md                                 C:\Users\kingd\.claude\profile\PROFILE.md
session-turn-title-summary.md              C:\Users\kingd\.claude\profile\session-turn-title-summary.md
user-payton-byrd.md                        C:\Users\kingd\.claude\profile\user-payton-byrd.md
f--github-mcpserver-grok-plugin-67f1f31f   
registry.json                              
True
True



### Turn 19 (Human)
<system-reminder>
The following skills are available for use:

- commit-sync: Pause all in-progress work, commit every dirty file in the current repo, push to origin, then resume
  Use when: the user says "commit-sync", "/commit-sync", "save my work", "sync to origin", "commit and push everything", or "checkpoint".
  Absolute path: C:\Users\kingd\.agents\skills\commit-sync\SKILL.md
</system-reminder>

### Turn 20 (Assistant)
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
[tool_request: list_dir]
- target_directory: C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f
[tool_request: list_dir]
- target_directory: F:\GitHub\mcpserver-grok-plugin

### Turn 21 (Function)
[tool_response]
- C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f/
  - cache/
    - current-turn.yaml
    - failsafe/
    - session-state.yaml
    - workspaces/
      - cbmengine-15f0f94c667f/
        - bootstrap/
      - mcpserver-1d57323c60e4/
        - bootstrap/
      - mcpserver-grok-plugin-3ea3b8e46fb7/
        - bootstrap/
  - CORE-MANIFEST.yaml
  - ENFORCEMENT.md
  - GROK-USAGE.md
  - hooks/
    - hooks.json
    - scripts/
      - cache-flush.ps1
      - code-verify.ps1
      - health-check.ps1
      - plan-approved.ps1
      - plan-modified.ps1
      - post-compact.ps1
      - pre-compact.ps1
      - session-end.ps1
      - session-start.ps1
      - stop-gate.ps1
      - subagent-import.ps1
      - user-prompt-submit.ps1
  - lib/
    - agent-runtime-header.ps1
    - cache-manager.ps1
    - ensure-repl.ps1
    - final-response.ps1
    - Invoke-McpPlugin.ps1
    - marker-resolver.ps1
    - mcp-status.ps1
    - McpPluginShim.psm1
    - plugin-env.ps1
    - plugin-hook.ps1
    - PLUGIN-RESIDUAL.txt
    - repl-invoke.ps1
    - resolve-cache-dir.ps1
    - yaml-object-mutation.ps1
  - LICENSE
  - Plugin-Validation-Testing-Plan.md
  - README.md
  - schemas/
    - repl-yaml-message.schema.json
  - skills/
    - clear-session/
      - SKILL.md
    - commit-sync/
      - SKILL.md
    - graphrag/
      - SKILL.md
    - reddit-draft/
      - SKILL.md
    - reddit-publish/
      - SKILL.md
    - requirements/
      - SKILL.md
    - session/
      - SKILL.md
    - sync-logs/
      - SKILL.md
    - todo/
      - SKILL.md
    - triage/
      - SKILL.md
    - usecase/
      - SKILL.md
    - workspace/
      - SKILL.md
    - wrap-up/
      - SKILL.md

### Turn 22 (Function)
[tool_response]
- F:\GitHub\mcpserver-grok-plugin/
  - cache/
    - current-turn.yaml
    - failsafe/
    - session-state.yaml
    - workspaces/
      - cbmengine-15f0f94c667f/
        - bootstrap/
      - mcpserver-1d57323c60e4/
        - bootstrap/
      - mcpserver-grok-plugin-3ea3b8e46fb7/
        - bootstrap/
  - CORE-MANIFEST.yaml
  - ENFORCEMENT.md
  - GROK-USAGE.md
  - hooks/
    - hooks.json
    - scripts/
      - cache-flush.ps1
      - code-verify.ps1
      - health-check.ps1
      - plan-approved.ps1
      - plan-modified.ps1
      - post-compact.ps1
      - pre-compact.ps1
      - session-end.ps1
      - session-start.ps1
      - stop-gate.ps1
      - subagent-import.ps1
      - user-prompt-submit.ps1
  - lib/
    - agent-runtime-header.ps1
    - cache-manager.ps1
    - ensure-repl.ps1
    - final-response.ps1
    - Invoke-McpPlugin.ps1
    - marker-resolver.ps1
    - mcp-status.ps1
    - McpPluginShim.psm1
    - plugin-env.ps1
    - plugin-hook.ps1
    - PLUGIN-RESIDUAL.txt
    - repl-invoke.ps1
    - resolve-cache-dir.ps1
    - yaml-object-mutation.ps1
  - LICENSE
  - Plugin-Validation-Testing-Plan.md
  - README.md
  - schemas/
    - repl-yaml-message.schema.json
  - skills/
    - clear-session/
      - SKILL.md
    - commit-sync/
      - SKILL.md
    - graphrag/
      - SKILL.md
    - reddit-draft/
      - SKILL.md
    - reddit-publish/
      - SKILL.md
    - requirements/
      - SKILL.md
    - session/
      - SKILL.md
    - sync-logs/
      - SKILL.md
    - todo/
      - SKILL.md
    - triage/
      - SKILL.md
    - usecase/
      - SKILL.md
    - workspace/
      - SKILL.md
    - wrap-up/
      - SKILL.md
  - tests/
    - skills.bats
    - smoke.bats

### Turn 23 (Function)
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


### Turn 24 (Function)
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


### Turn 27 (Function)
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


### Turn 29 (Assistant)
Profile loaded (14 profile markdown files read). Bootstrapping MCP trust and inventorying docs/git state.
[tool_request: read_file]
- target_file: F:\GitHub\mcpserver-grok-plugin\GROK-USAGE.md
- limit: 120
[tool_request: run_terminal_command]
- command: $env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'; $env:PLUGIN_AGENT_NAME = 'GrokCode'; Set-Location 'F:\GitHub\vice-sharp'; pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\mcp-status.ps1' 2>&1
- description: Verify MCP marker trust and plugin status
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'; git status --short 2>&1 | Select-Object -First 80; Write-Host '---'; git branch -vv 2>&1; Write-Host '---'; git log --oneline -15 2>&1; Write-Host '---'; git remote -v 2>&1
- description: Git status, branch, recent commits
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'; Get-ChildItem docs -Directory | Select-Object Name; Write-Host '---TOP---'; Get-ChildItem docs -File -Filter *.md | Select-Object Name, LastWriteTime | Sort-Object LastWriteTime -Descending | Select-Object -First 40; Write-Host '---WIKI---'; Test-Path docs/wiki.yaml; if (Test-Path docs/wiki.yaml) { (Get-Item docs/wiki.yaml).Length; (Get-Item docs/wiki.yaml).LastWriteTime }
- description: Inventory docs surface and wiki.yaml

### Turn 30 (Function)
[tool_response]
1→# McpServer Grok Plugin — Usage for Grok 4.3 Coding Agent CLI / TUI

This directory is a fork/adaptation of `mcpserver-claude-code-plugin` for the **Grok 4.3** interactive CLI / TUI (and compatible Grok agents).

It provides:

- Native Grok `SKILL.md` files under `skills/` (todo, session, requirements, usecase, graphrag, workspace).
- Discoverable MCP tools through the Streamable HTTP endpoint in `.mcp.json`, plus full contract parity with the McpServer workflow shim (`workflow.*` namespaces) via plugin helpers.
- Hook scripts and lib/ (bash reference implementation + pwsh emphasis for this workspace).
10→- Offline cache support and marker bootstrap logic (signature + nonce) exactly as required by `AGENTS-README-FIRST.yaml`.

## Recommended Loading for Grok

1. Copy or symlink the contents of `skills/` into your personal Grok skills location (`~/.grok/skills` or a marketplace plugin directory).
2. Grok will discover the skills on next start / relevant prompt (e.g. "start session", "create a todo", "list requirements").
3. Use `grok inspect`, `grok mcp doctor mcpserver`, or `/mcps` to confirm the enabled plugin exposes the `mcpserver` MCP entry. The MCP entry should connect to `http://localhost:7147/mcp-transport`; workflow shim helpers still require `mcpserver-repl` (the dotnet global tool) on PATH.

Preferred runtime in this workspace: **PowerShell 7+ (`pwsh.exe`)** + the existing `McpSession.psm1`, `McpTodo.psm1`, `McpContext.psm1` modules. The bash hooks are retained for compatibility with Claude/Codex/Copilot agents that use the original plugin.

20→## Bootstrap (Marker + Signature + Nonce)

The skills and hooks implement (or guide) the exact flow required by the workspace marker:

- Read `AGENTS-README-FIRST.yaml`
- Verify HMAC-SHA256 signature using the workspace API key
- Call `/health?nonce=...` and confirm echo
- Only then open sessions, create TODOs, etc.

See `hooks/scripts/session-start.sh` (and the Grok skills) for the reference implementation. On Grok, the skills perform equivalent checks where possible and fall back gracefully to `MCP_UNTRUSTED` / local-only mode when the server is unavailable (as required by policy).
30→
## Session / Turn Lifecycle (GrokCode)

Use the **session** skill (or the plugin shim for the underlying `workflow.sessionlog.*` methods) with the canonical naming:

- SessionId: `GrokCode-YYYYMMDDTHHMMSSZ-slug`
- RequestId: `req-YYYYMMDDTHHMMSSZ-slug`
- `sourceType` / agent prefix: `GrokCode` (Pascal-Case)

All design decisions must be logged as `appendDialog` (category: decision) **and** `appendActions` (type: design_decision).
40→
The `workflow.sessionlog.*`, `workflow.todo.*`, and `workflow.requirements.*` names are plugin shim/REPL method names. They are not expected to appear as literal Grok `search_tool` results. If the TUI exposes `pwsh` but not a dedicated workflow tool, call `lib\repl-invoke.ps1` from this plugin root with `-Method <workflow.method>` and YAML params.

## TODO / Requirements

The **todo** and **requirements** skills implement the full contract (create, query, streaming plan/implement/status, FR/TR/TEST mapping, canonical ID rules `^[A-Z]+-[A-Z0-9]+-\d{3}$` or `ISSUE-\d+`).

Internal checklist state stays local by default. Enable `workflow.todo.internal.enable` only when you want MCP TODOs as the backing store.

## GraphRAG & Workspace
50→
Ad-hoc ingestion and workspace policy helpers are provided via the respective skills.

## Differences from the Original Claude Code Plugin

- Primary interface is Grok `SKILL.md` files (not Claude hooks/skills system).
- Strong emphasis on `pwsh.exe` and the workspace PowerShell modules.
- Bash hooks and `mcpserver-repl` helper usage retained for contract fidelity and multi-agent workflow shim use.
- Cache/ is shipped clean; runtime state lives in the user's environment.
- No assumption of `CLAUDE_PLUGIN_ROOT` or Claude-specific env vars (Grok equivalents are `PLUGIN_ROOT` / `MCP_GROK_*`).
60→
## Development / Validation

- Original `tests/` (bats) are retained from the source for contract regression coverage.
- Grok-specific validation: load the SKILL.md files, confirm `grok mcp doctor mcpserver` succeeds against `/mcp-transport`, exercise the workflow shim methods via `mcpserver-repl` helpers or the PowerShell modules, and confirm marker bootstrap + session/turn lifecycle.
- See `Plugin-Validation-Testing-Plan.md` (source document, with Grok notes added).

## License

MIT (same as source).
70→
---

Maintained as part of the McpServer workspace agent plugin ecosystem. The Grok contract has been registered in `MarkerFileService.BuildDefaultAgentPlugins`. After the next McpServer restart, `AGENTS-README-FIRST.yaml` will include the `Grok` entry and instruct GrokCode agents to bootstrap `mcpserver-grok-plugin` automatically.


### Turn 31 (Function)
[tool_response]
exit: 0

---TOP---
---WIKI---
Name
----
announcements
maintenance
plans
Project
receipts
requirements
reviews
S-Blox
samples
schemas
session-logs
wireframes
xbox
audit-vic20-vs-vice-2026-08-07.md
receipts-vic20-ntsc-apply-stackoverflow-2026-08-07.md
receipts-vic20-video-40x40-2026-08-07.md
receipts-vic20-ram-all-stackoverflow-2026-08-06.md
Iteration-Roadmap.md
README.md
PRIVACY.md
xbox-store-publishing.md
romless-vic-badline-fix.md
receipts-viceparity-closure-2026-07-10.md
receipts-viceparity-s13-2026-07-10.md
receipts-viceparity-s12-2026-07-10.md
receipts-viceparity-s11b-2026-07-10.md
receipts-nativeresidue-002-2026-07-09.md
plan.md
ROL.md
Iteration-00-Foundations.md
StateWindow.md
PubSub.md
Decoupling.md
audit-vicii-vs-vice-2026-07-06.md
Public-API.md
Architecture.md
VICE-MIGRATION.md
USER-GUIDE.md
AI-Review.md
ROMs.md
receipts-test-baseline-2026-07-07.md
receipts-s9-filter-bitexact.md
True
7219




### Turn 32 (Function)
[tool_response]
exit: 0
 M build/Build.cs
 M native/vice-shim-vic20.c
 M native/vice-shim.h
 M src/ViceSharp.Abstractions/IViceNative.cs
 M src/ViceSharp.Architectures/Vic20/Vic20Descriptor.cs
 M src/ViceSharp.Architectures/Vic20/Vic20Expansion.cs
 M src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs
 M src/ViceSharp.Avalonia/Views/SettingsView.axaml
 M src/ViceSharp.Avalonia/Views/SettingsView.axaml.cs
 M src/ViceSharp.Chips/Vic/Mos6561.cs
 M src/ViceSharp.Core/ArchitectureBuilder.cs
 M src/ViceSharp.Core/Vic20/Vic20MemoryLayout.cs
 M src/ViceSharp.Core/Vic20/Vic20SystemRam.cs
 M src/ViceSharp.Core/ViceNative.Xvic.cs
 M src/ViceSharp.Core/ViceNative.cs
 M src/ViceSharp.Host.InProcess/Runtime/ConsoleEmulatorHost.cs
 M src/ViceSharp.Host.InProcess/Runtime/DefaultEmulatorRuntimeFactory.cs
 M src/ViceSharp.Host.InProcess/Runtime/EmulatorRuntimeSession.cs
 M src/ViceSharp.Host.InProcess/Services/HostProtocolMapper.cs
 M src/ViceSharp.Host.InProcess/Services/MediaServiceHost.cs
 M src/ViceSharp.Host.InProcess/Services/SettingsServiceHost.cs
 M src/ViceSharp.Protocol/ProtocolContracts.cs
 M src/ViceSharp.Protocol/SettingsOptionCatalog.cs
 M src/ViceSharp.Xbox.ViewModels/AboutInfo.cs
 M src/ViceSharp.Xbox.ViewModels/AboutViewModel.cs
 M src/ViceSharp.Xbox.ViewModels/NavigationDestination.cs
 M src/ViceSharp.Xbox.ViewModels/VideoFramePullViewModel.cs
 M src/ViceSharp.Xbox.ViewModels/XboxSettingsViewModel.cs
 M src/ViceSharp.Xbox/App.xaml.cs
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 M tests/ViceSharp.TestHarness/Vic20/Vic20DivergeProbe.cs
 M tests/ViceSharp.TestHarness/Vic20/Vic20MemoryMapTests.cs
 M tests/ViceSharp.TestHarness/Vic20/Vic20VideoTests.cs
 M tests/ViceSharp.TestHarness/Xbox/AboutViewModelTests.cs
 M tests/ViceSharp.TestHarness/Xbox/Fakes/FakeXboxSettingsGateway.cs
 M tests/ViceSharp.TestHarness/Xbox/VideoFramePullViewModelTests.cs
 M tests/ViceSharp.TestHarness/Xbox/XboxComputerModelPickerTests.cs
 M tests/ViceSharp.TestHarness/Xbox/XboxSettingsViewModelTests.cs
?? docs/10s-lockstep-2026-08-07.log
?? docs/10s-lockstep-blankfix-2026-08-06.log
?? docs/10s-lockstep-idle-2026-08-06.log
?? docs/10s-lockstep-memptr2-2026-08-06.log
?? docs/10s-lockstep-ntsc-2026-08-07.log
?? docs/10s-lockstep-rst6-2026-08-06.log
?? docs/2s-adc-2026-08-06.log
?? docs/2s-adc2-2026-08-06.log
?? docs/2s-and-imm-2026-08-06.log
?? docs/2s-arm-bcc-2026-08-06.log
?? docs/2s-asl-2026-08-06.log
?? docs/2s-asl-bcs-2026-08-06.log
?? docs/2s-b1-beq-2026-08-06.log
?? docs/2s-b1stage-2026-08-06.log
?? docs/2s-bcc-hold-2026-08-06.log
?? docs/2s-bccsta-2026-08-06.log
?? docs/2s-bcs-stage-2026-08-06.log
?? docs/2s-beq-2026-08-06.log
?? docs/2s-beq-hold-2026-08-06.log
?? docs/2s-beqpla-2026-08-06.log
?? docs/2s-bit-2026-08-06.log
?? docs/2s-bit2-2026-08-06.log
?? docs/2s-bit3-2026-08-06.log
?? docs/2s-bit4-2026-08-06.log
?? docs/2s-bit5-2026-08-06.log
?? docs/2s-bitf-2026-08-06.log
?? docs/2s-bitf2-2026-08-06.log
?? docs/2s-bmi-chain-2026-08-06.log
?? docs/2s-bne-cmp-2026-08-06.log
?? docs/2s-bne-stage-2026-08-06.log
?? docs/2s-bnehold-2026-08-06.log
?? docs/2s-bpl3-2026-08-06.log
?? docs/2s-bpl4-2026-08-06.log
?? docs/2s-bpl6-2026-08-06.log
?? docs/2s-bpl7-2026-08-06.log
?? docs/2s-br-irq-2026-08-06.log
?? docs/2s-cmp-absx-2026-08-06.log
?? docs/2s-cmpzp-2026-08-06.log
?? docs/2s-cmpzp2-2026-08-06.log
?? docs/2s-colorram-2026-08-06.log
?? docs/2s-cpx-soft-2026-08-06.log
---
  develop                          aaa53e3 test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]
  feat/drive-family-and-device-art b6b7753 docs(requirements): regenerate wiki requirement exports (drive-family FR/TR/TEST + BUG-DRV1540-001)
  feat/iteration2-vic20            b793b23 feat: Vic20 iteration2 core + Microsoft Store submission docs
+ feat/romm-integration            978b5a6 (F:/GitHub/vice-sharp-romm) fix(xbox): OnLaunched composed the shell twice, leaking an unarmed gamepad pump
+ fix/romless-vic-content          7f57f92 (F:/GitHub/vice-sharp-romless) [azure/fix/romless-vic-content] test(vic): ROM-less bad-line repro asserts VICE-faithful observables; VIC unchanged [FIX-ROMLESSVIC-001]
* main                             e5203f9 [origin/main] docs(requirements): capture C= logo source/attribution as FR-XBOXGPL-007
---
e5203f9 docs(requirements): capture C= logo source/attribution as FR-XBOXGPL-007
e77ec58 docs: refresh wiki export for store docs and v1.2.2 line
f46404d release: bump to 1.2.2 with Store art and C= logo attribution
3d3b6e4 fix(vic20): 10s PAL+NTSC every-cycle lockstep vs xvic
8794a41 merge: feat/iteration2-vic20 into main
b793b23 feat: Vic20 iteration2 core + Microsoft Store submission docs
b2ba4da ci: point ADO pipelines at GitHub; restore Octopus release steps
3b20c4f release: bump patch to 1.2.1 on main
521987b merge: feat/xbox-uwp-app into main for 1.2 line
7442c1d release: bump minor to 1.2.0 (RomM, T64, Avalonia UX)
cbb72be feat(xbox): Game-Pass style RomM Library page [PLAN-ROMM-001 AC-XUI-02]
0c83627 fix(xbox): restore RomM Library/Lists/CSDb on home menu [PLAN-ROMM-001]
3ba3068 feat(xbox): ESC closes virtual keyboard when open [FEAT-XKBD-ESC-001]
f67eeb9 fix: restore Octopus CI steps; drop Avalonia Architectures/Chips refs
6b875b6 fix(build): bump Cryptography.Xml 10.0.10; nullable RomProvisioning ViewModel
---
azurehttps://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp (fetch)
azurehttps://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp (push)
originhttps://github.com/sharpninja/vice-sharp.git (fetch)
originhttps://github.com/sharpninja/vice-sharp.git (push)


### Turn 33 (Function)
[tool_response]
exit: 0
{"status":"available","agent":"ClaudeCode","namespaces":["workflow.sessionlog","workflow.todo","workflow.requirements","workflow.triage","workflow.graphrag","workflow.memory","workflow.failsafe"],"requirementMethods":["workflow.requirements.listLayers","workflow.requirements.createLayer","workflow.requirements.updateLayer","workflow.requirements.effective"],"requirementClientMethods":["client.Requirements.ListRequirementLayersAsync","client.Requirements.CreateRequirementLayerAsync","client.Requirements.UpdateRequirementLayerAsync","client.Requirements.GetEffectiveRequirementsAsync"],"cacheDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\claude","hasSession":true,"hasCurrentTurn":true,"pendingCount":0,"pendingTurnCount":0,"failsafeDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\claude\\failsafe","failsafeCount":0,"failsafeQuarantineCount":4}


### Turn 34 (Assistant)
[tool_request: run_terminal_command]
- command: $env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
Remove-Item Env:CLAUDE_PLUGIN_ROOT -ErrorAction SilentlyContinue
Remove-Item Env:CODEX_PLUGIN_ROOT -ErrorAction SilentlyContinue
Set-Location 'F:\GitHub\vice-sharp'
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\mcp-status.ps1' 2>&1
Write-Host '---SESSION---'
Get-Content 'F:\GitHub\mcpserver-grok-plugin\cache\session-state.yaml' -ErrorAction SilentlyContinue
Write-Host '---TURN---'
Get-Content 'F:\GitHub\mcpserver-grok-plugin\cache\current-turn.yaml' -ErrorAction SilentlyContinue
Write-Host '---INVOKE-STATUS---'
# Try Invoke-McpPlugin Status if exists
Get-ChildItem 'F:\GitHub\mcpserver-grok-plugin\lib\*.ps1' | Select-Object Name

- description: Force GrokCode MCP status and session state
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# Untracked source/docs of interest (exclude bulk 2s- logs)
git status --short 2>&1 | Where-Object { $_ -notmatch 'docs/2s-|docs/10s-|docs/adc|docs/asl|docs/_deploy|docs/_vs-ready|docs/S-Blox' } | Select-Object -First 120
Write-Host '---UNTRACKED SRC---'
git status --short --untracked-files=all 2>&1 | Where-Object { $_ -match '^\?\?.*(src/|tests/|docs/(plan|README|wiki|USER|Architecture|receipts|Project|requirements)|HANDOFF)' } | Select-Object -First 100

- description: List meaningful dirty/untracked code and docs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\wiki.yaml
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\README.md
- limit: 150
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\HANDOFF.md
- limit: 120

### Turn 35 (Function)
[tool_response]
1→# ViceSharp Handoff - 2026-08-06

**Active branch:** `feat/iteration2-vic20` (off `main` @ v1.2.1 line). Iteration 2 = overall project VIC-20 (not RomM Phase E).
**Prior release baseline:** `main` at **v1.2.1** (NuGet + winget published). Iteration 1 C64 complete.
**Working tree noise (do not commit):** untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, `manifests/`, many `docs/*-2026-08-06.log` lockstep artifacts.

## VIC-20 every-cycle lockstep - PAL + NTSC 10s GREEN (2026-08-06)

- **PAL TenSecondPal:** matchedCycles = 11_084_050 / budget 11_084_050 (~35 s). Log: `docs/10s-lockstep-2026-08-06.log`. Receipt: `docs/receipts-lockstep-10s-2026-08-06.txt`.
10→- **NTSC TenSecondNtsc:** matchedCycles = 10_227_270 / budget 10_227_270 (~37 s). Log: `docs/10s-lockstep-ntsc-2026-08-06.log`. Receipt: `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`.
- **PAL TwoSecondPal** (VICESHARP_LOCKSTEP_2S=1): Passed ~8 s. FocusedWindow: Passed.
- **Via6522 unit:** 48/0/0 after bus-visible T1/T2 change.
- **Where diverged (NTSC residual c=521027):** soft-BIT $9124 (VIA2 T1) V flag only (nP=$E5 mP=$A5).
- **How diverged:** (1) soft-apply re-LOADed T1; VICE BIT(GET_ABS) loads once. (2) VIA Tick before CPU made CPU Read see post-Tick T1; VICE LOAD is at maincpu_clk before CLK_INC (saw $C0 V=1 vs managed $BF V=0).
- **How realigned:** SoftDeferBitBody latches one GET; soft-apply N/V/Z from latch. Via6522 captures pre-Tick `_t1BusVisible`/`_t2BusVisible` for Read; Peek stays post-Tick (export match). Rejected CPU-before-VIA Phi2 reorder (broke free-run phase).
- Env: `VICESHARP_LOCKSTEP_10S=1` for 10s gates; `VICESHARP_LOCKSTEP_2S=1` for 2s. Run NTSC and PAL in **isolated processes** (failed NTSC can poison later PAL short runs at c=5513).

## Iteration 2 VIC-20 - native lockstep gate GREEN (2026-08-05)

20→- **Branch:** `feat/iteration2-vic20` (off main / v1.2.1). Locked: **default drive 8 = 1540**.
- **Native xvic oracle:** `native/vice_xvic.dll` via `native/build-vice-shim-xvic.sh` + `vice-shim-vic20.c`; hosted `vic20cpu.c` / `mainviccpu.c` (`VICE_SHIM_HOSTED`). Managed: `ViceNative.CreateInstance("vic20"|"vic20ntsc"|"xvic")` -> `ViceNativeXvic`.
- **Open-bus (FR-VIC20-002):** `BasicBus` last-data latch; BASIC/KERNAL ROM reads do **not** refresh last-data (VICE `vic20memrom_*`); chargen does; uninstalled BLK falls through (not sticky 0xFF). Fixes kernal `CMP $A003,X` Carry/N vs xvic.
- **Lockstep receipts (criterion 3):** full **A/X/Y/S/P/PC** through **5000** cycles; **dual VIA** control peeks ($9110/$9120 DDRA/DDRB/ACR/PCR/IER) at 64/256/1024; **VIC-I** static peeks + VICE raster encoding ($9003/$9004) through 5000. Vic20 filter **87/0/0**. API: `IViceNative.PeekBus` / `vice_machine_peek_bus`. Mos6561 power-on zeros + raster encoding match VICE. Receipts: `docs/receipts-vic20-phase2-verify-2026-08-05.txt`. CI: VIC20 + dos1540 in `EnsureCiRomRoot`.
- **Residual:** intermittent mid-instruction PC lag after cycle ~5005 (control-flow diverge later; deeper CPU staging for multi-frame bit-exact).
- **Still open (Tier A polish):** J expansion UX, K input E2E, L cart/disk, M snapshots.
- **Do not** commit untracked `docs/S-Blox/`, reviews, manifests. Rebuild oracle: `MSYSTEM=MINGW64 bash -lc 'cd /f/GitHub/vice-sharp/native && bash ./build-vice-shim-xvic.sh'`.

## PLAN-XBOXUWP + PLAN-ROMM resume snapshot (2026-08-05)

30→- **Track 1 (Xbox Tier D residuals) largely SHIPPED this session:** FEAT-XAOTBIND-001 (x:Bind migration), FEAT-XOCTOPUS-001 (CI/release Octopus LEGION2 steps), FIX-ROMLESSVIC-001 (merged test), FIX-XKBDNMI-001 (RESTORE asserts NMI), FEAT-XCTRLBIND-001 (Controls remapping), FEAT-XROMPICK-001 (model ROM readiness).
- **Track 2 (RomM):** merged `feat/romm-integration` into this branch (`3804a1d`). Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM`, Xbox/Avalonia library UI, LAN bridge connection path present. Gate after merge: **Category=Xbox|RomM|Library 546/546** (0 fail, 0 skip).
- **Deploy loop:** `./build.ps1 DeployXboxLocal` (VS MSBuild Release-UWP). End CLI batches with Debug-UWP restore for operator VS F5. PublishAot stays opt-in (`ViceSharpPublishAot=true`); Release-UWP JIT.
- **Track 3 (Microsoft Store / S42) IN PROGRESS 2026-08-05:**
  - Phase 0 **GO with mitigations**: `docs/xbox/gpl-store-section6-review.md`
  - Privacy: `docs/PRIVACY.md` (listing URL source)
  - Listing copy + screenshot runbook: `docs/xbox/store-listing-copy.md`
  - GPL gate receipt: `XboxGplComplianceTests` **5 passed / 0 failed / 0 skipped** (Release)
  - ADO gaps: variable group `xbox-store-publish` **missing**; pipeline `azure-pipelines-xbox-store.yml` **not registered** (project has VICE-Sharp-CI id 15, VICE-Sharp-Release id 16 only); environment `xbox-store` not confirmed
  - **Operator guide (step-by-step + links):** `docs/xbox/store-next-steps-guide.md`
40→  - **Next operator:** Partner Center reserve name + fill 8 vars; create ADO var group + environment approval; register Store pipeline; capture screenshots; S42 console matrix
- **OPEN:** B-on-controller operator verification; S42 Tier-C console deploy + cert; Partner Center + ADO wiring; MCP TODO reconcile; device smoke for RomM Library after merge.
- **Parallel worktrees (reference):** `F:\GitHub\vice-sharp-romm` (`feat/romm-integration`, source of merge); romless repro merged via FIX-ROMLESSVIC-001.

## How to resume

1. Read `AGENTS-README-FIRST.yaml` (marker rotates on server restart; verify signature + /health nonce before MCP work).
2. Route TODO / session-log / requirements / triage through the mcpserver plugin (repl-invoke or wrappers), never raw REST, never storage files.
3. Baseline gate (one process, ~20 min, run detached):
   `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`
50→   Green criterion: 0 failed, 21 skipped, total 2612-2615 (totals wobble in that band from cosmetic xunit trx name serialization of dynamic theory rows; per-class row sets are identical - never chase a fixed total).

## Shipped 2026-07-08 (all CI-confirmed)

- **v1.0.2 released**: tag on `534cded`, release run 1074, all 13 package ids verified in the nuget.org flat container (Core bundle, SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox; Console + Avalonia are dotnet tools).
- **PLAN-NATIVERESIDUE-001 closed** (`ba0f94f`): a `.vsf` whose DRIVE8 module carries `has_tde=0` disabled TrueDrive process-wide via `resources_set_int` (drive-snapshot.c:334-363); `vice_machine_create_model` now re-baselines `Drive{8..11}TrueEmulation` to the VICE default 1 through `resources_set_int` so iecbus statics and $DD00 callbacks recompute. SnapshotResume two-process partition removed; suite proven green in ONE process. Guards: TEST-NATIVE-RESIDUE-01/02 in `NativeResidueDiagTests`.
- **CI made self-sufficient** (`a229801`, `d8f61cf`, `4e3db9b`): agents have no ROMs and no shim, so `EnsureCiRomRoot` in `build/Build.cs` stages 13 hash-pinned VICE data files (C64 ROMs incl. all variant kernals, DRIVES dos1541/dos1541ii, gtk3_pos.vkm) from the VICE-Team svn-mirror into `artifacts/vice-data` and sets `VICESHARP_ROM_PATH` for the test process only. Shim-dependent tests MUST use `[ViceFact]`/`[ViceTheory]` (plain `[Fact]` throws DllNotFoundException on shimless agents). First-ever green CI run: 1072.
- **Docs reconciled to shipped reality** (`aee29ea`, 88 files): README, USER-GUIDE, VICE-MIGRATION, ROMs, Architecture, Public-API (regenerated from the 50 real Abstractions interfaces), requirements docs; 22 stale files pruned; `docs/wiki.yaml` manifest lists all 28 wiki documents; winget license fixed MIT -> GPL-2.0-or-later.
- **github divergence resolved** (`20b330c`): github-only commit `42f2f26` (VicModeChangeEvent seam) merged with strategy `ours` (tree unchanged; content preserved on `rescue/vic-modechange-event`); github main fast-forwarded, tags identical on both remotes; github wiki published per manifest (`b455b88`, 28 pages + sidebar).
- **Dependency wave S0-S8 complete** (TR-DEPS-202607-001 + TEST-DEPS-202607-001, both completed with evidence; commits `72696d9`, `055b9d5`, `26e5ca8`, `87eb80a`, `9214cdb`, `1cad822`+`1adf386`, `23d030a`, `0f5192f`): all 30 central entries at newest mutually compatible stables (Avalonia 12.0.5, Extensions/TestHost 10.0.9, Protobuf 3.35.1, Grpc.Tools 2.82.0, Test.Sdk 18.7.0, Roslyn pair 5.6.0, YamlDotNet 18.1.0, FluentAssertions 8.10.0, coverlet 10.0.1, aiUnit 2.1.3, RemoteControl 0.7.4); 5 dead pins removed; AiReview under CPM with inherited TreatWarningsAsErrors; vendored `nuget-local` feed DELETED (NuGet.config is nuget.org-only); `global.json` floor 10.0.301 (Roslyn 5.6 generator gate). Excluded as prerelease: xunit.v3 4.x, NSubstitute 6.x, Protobuf 4.x, Extensions 11.x.
60→- **Test fixtures in repo** (`93cdc7e`): `vice-snapshot-20260630171307.vsf` (read by the residue probes and lockstep re-baseline suites at repo root; do not delete), `native/.build-nopatch.sh`, `native/.ccwrap/*`. Operator rule: if it is needed to run a test, it goes in the repo.

## Iteration-1 completion (branch `fix/nativeresidue-002-drive-clock-hardening`, off `master`; NOT pushed)

Working through the "complete outstanding iteration 1 work" plan. Done, all committed locally, gates green:
- **Slice A - PLAN-NATIVERESIDUE-002** (710c1e9/8894f1a/9fb06dd/0691344): the two latent drive-clock bugs fixed; **BUG-LOCKSTEP-001 CLOSED** (full gate 0 failed / 2596 passed / 21 skipped). See docs/receipts-nativeresidue-002-2026-07-09.md.
- **Slice F - BUG-TESTDEBT-001**: verified already fixed at HEAD (6a32e7c/77e7190), closed with receipt.
- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.

**SID parity program COMPLETE (2026-07-10)** - all remaining slices landed:
70→- **S11 / S11b**: 8580 reSID filter port (filter8580new m==1) + write pipeline + per-model ttl(0xA2000)/scaleFactor(5); shim exports vice_sid_exact_set_sampling + vice_sid_exact_clock_buffered; DATABUS-07 flipped; oracle via "c64c". Ratchet ->444.
- **S12**: amplify(scaleFactor)/clip PCM16 seam + extfilt enable branch; OUTPUT-01..07. Ratchet ->451.
- **S13**: fixed-point Kaiser FIR resampler (fast/interpolate/resample); OUTPUT-08..13. Ratchet ->457.
- **CH**: retired the dead Chamberlin SVF stack (10 members + 7 guards + SidFilter6581Tests.cs). PLAN-SIDCHAMBERLIN-001 closed.
- **BC**: ported reSID batched SID::clock(delta_t) (Sid6581.BatchedClock.cs); SAMPLE_FAST now value-bit-exact; un-pended the last 4 SID quarantines + authored the last 9 (EXTFILT-01..07, CLOCK-05/08). Ratchet ->466 with a strict completion pin (Assert.Equal(466, covered)).
- **LW** (4924b94, pushed): live audio wired through the reSID SAMPLE_RESAMPLE engine (Resample always, VICE x64sc parity); push tail bit-exact vs the buffered pull; warp = cadence-only; zero-alloc; benchmark in SidSamplingBenchmarks.
- **CL** (a38964b/e100433, local): NativeCollectionConventionTests + [Collection("NativeVice")] on the 6 native-bridge test classes.
- **G**: DROPPED per operator (ADO wiki push).

Closure gates: parity 466/0/0 (completion pin holds), determinism 5/0, XmlDocs green, full baseline 0 NEW failures (7 pre-existing VideoRendererTests only) on the clean re-run. One run-1 full-suite native ACCESS_VIOLATION did NOT reproduce (flaky native-shim instability; candidate PLAN-NATIVERESIDUE-002 follow-up, not a SID blocker) - see the closure receipts.
80→
**Native build recipe (learned this session, load-bearing):** `make x64sc-program` does NOT rebuild changed VICE-core `.o` (they stay stale). Before rebuilding, delete the changed `.o` (and `libdrive.a` for drive files), then run under the MSYS2 MINGW64 login shell so the compiler gets a writable `/tmp`:
`MSYSTEM=MINGW64 /c/msys64/usr/bin/bash.exe -lc 'rm -f <changed>.o; bash /f/GitHub/vice-sharp/native/.build-nopatch.sh'`. The dll is gitignored (built locally). Vendored edits go in native/patches/vice-shim-runtime.patch (regenerate via `git -C native/vice diff`; keep the 5 pre-existing hunks byte-identical).

**Oracle note:** SidExactRead (read path) is the reliable bus observable; SidExactGetState().BusValue export is NOT a dependable live-latch snapshot (returned stale/garbage) - compare via the read path + managed spec constants.

## In flight (interrupted mid-task)

Plugin reload + Agent Help (mcpserver core synced to 1.36.0, `mcpserver-repl` 1.4.5 confirmed on PATH):
1. Validate cache: `%USERPROFILE%\.claude\plugins\cache` has `mcpserver-local` (active family; confirm 1.36.0 from the plugin's own `.claude-plugin/plugin.json`), `mcpserver-cowork` (stale candidate - verify before deleting), `caveman` (unrelated, keep). The version inspection was interrupted by a transient permission-classifier outage; re-run it.
90→2. Run the claude-hook-validation skill; refresh MCP hooks if missing; restart Claude Code if hooks were installed.
3. Open a `workflow.agenthelp.createSession` (new in repl 1.4.5) and submit the outstanding MCP issues:
   - repl-invoke `Invoke-WorkflowAppendActions` audit counters never increment: regex `(?m)^\s*type:` misses `- type:` list items (repl-invoke.ps1:891-897; filed as triage-report-8a6539340a174c00a57dee53ec8f42ea).
   - No per-action readback: `workflow.sessionlog.queryHistory` returns only session headers, so appends cannot be content-verified; session header `lastUpdated`/`filesModifiedCount` never advance (anchored to `started`).
   - `Invoke-McpPlugin.ps1` defaults `-Command Status`: calling it with only `-Method` silently prints the status blob instead of invoking (footgun; cost one silent append failure).
   - `workflow.requirements.createTr` requires `subarea` (schema) but the skill docs do not mention it; the server also derived `subarea: 202607` from the id rather than honoring the supplied value.
4. Log the reload + help-session outcomes; triage any confirmed server defects.

## Parked items

100→- Azure DevOps wiki push needs `ADO_PAT` set, then `tools/Publish-Wiki.ps1 -Target azure` (github wiki already published).
- ~~Latent VICE bugs documented, not fixed~~ FIXED 2026-07-09 (PLAN-NATIVERESIDUE-002, branch `fix/nativeresidue-002-drive-clock-hardening`): drive `attach_clk`/`detach_clk`/`attach_detach_clk` uninitialized-stack read on `has_tde=0` restore (drive-snapshot.c zero-init) and `cycle_accum` omitted from `drivecpu_reset_clk` (drivecpu.c + drivecpu65c02.c) both fixed via the vendored runtime patch, plus a shim create-time drive-clock re-baseline. **BUG-LOCKSTEP-001 CLOSED**: full baseline gate 0 failed / 2596 passed / 21 skipped / 2617 total (was 136+2 lockstep failures). Receipts: docs/receipts-nativeresidue-002-2026-07-09.md. New residue candidates recorded there (live unit->type not re-baselined; drivecpu65c02 cycle_accum SMW/SMR width asymmetry). Build note: `make x64sc-program` does NOT rebuild changed VICE-core `.o`; delete the stale `.o`+`libdrive.a` and build under `MSYSTEM=MINGW64 bash -lc`.
- ~~VICE parity program (PLAN-VICEPARITY-001) remaining slices~~ **COMPLETE 2026-07-10** (SID side fully bit-exact vs reSID; VIC per-cycle work landed through V7 + audit phases). Only manifest-wide pending AC left is TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock conflict, out of SID scope, needs the parity owner). Candidate follow-up: PLAN-NATIVERESIDUE-002 native-shim lifecycle hardening (a sustained-consecutive-native-load ACCESS_VIOLATION observed once, non-reproducing).

## Open TODO backlog (MCP store)

PLAN-UIDOCK-001 (Dock.Avalonia UI), PLAN-AUDIOEQ-001, PLAN-PLAYLIST-001, PLAN-FULLSCREEN-001, PLAN-DEVCARDART-001, PLAN-MONFRAME-001, PLAN-DRIVE1581-001, PLAN-DRIVE1541II-001, PLAN-DRIVE1540-001, PLAN-DRIVE1571-001, PLAN-DRIVECMDHD-001, PLAN-CARTRAMLINK-001, PLAN-ARCHVIC20-001 (query the store for the live list; this snapshot is 2026-07-09).

## Operational gotchas (hard-won this session)

110→- Long test runs: OS-detach with `Start-Process dotnet ... -RedirectStandardOutput` and tail the log with a Monitor; never run two test/build invocations concurrently (native lock + obj/bin contention); verify the log grows before trusting a launch.
- `git add a b c bad-path` aborts the WHOLE add on one bad pathspec and a following commit ships whatever was staged earlier; always check the commit stat against intent.
- Diagnostic probes: `LiveLimiterBandProbeTests` reports via a by-design `Assert.Fail`; `Demo_SilentWarp` headroom is host-load sensitive (re-run in isolation before believing a failure). Live-app probes attach via `%LOCALAPPDATA%\ViceSharp\debug-attach.json`; NEVER dispose the probe client (it kills the session).
- github force-push is classifier-blocked regardless of instruction phrasing; the sanctioned divergence pattern is merge `-s ours` + fast-forward.


### Turn 36 (Function)
[tool_response]
1→# ViceSharp

A C# port of [VICE](https://vice-emu.sourceforge.io/) (Versatile Commodore Emulator) targeting .NET 10.

> **Iteration 1 (C64) is complete.** The managed C64 core runs in cycle-exact lockstep with VICE's `x64sc`. **Iteration 2 (VIC-20)** every-cycle A/X/Y/S/P/PC lockstep vs native `xvic` is green for **10 s PAL** (11_084_050 cycles) and **10 s NTSC** (10_227_270 cycles). See [docs/Iteration-Roadmap.md](docs/Iteration-Roadmap.md) and [HANDOFF.md](HANDOFF.md).

## Quick Start

```pwsh
10→# 1. Clone and build
git clone https://github.com/sharpninja/vice-sharp.git vice-sharp
cd vice-sharp
dotnet build ViceSharp.slnx

# 2. Point at your VICE data root, or put x64sc.exe on PATH (see docs/ROMs.md)
$env:VICESHARP_ROM_PATH = "C:\path\to\GTK3VICE-3.8-win64"

# 3. Boot a C64 with a true-drive 1541 attached
dotnet run --project src/ViceSharp.Console -- `
20→    --roms $env:VICESHARP_ROM_PATH `
    --machine-yaml docs/samples/c64-plus-1541.multisystem.yaml `
    --cycles 1000000
```

Coming from classic VICE? The `ViceSharp.Launcher` library provides VICE-compatible argument parsing and binary-name topology dispatch (`x64`, `x64sc`, `c1541`), consumed by `ViceSharp.Console`, which accepts the usual `-8`, `-9`, `-cart`, `+truedrive` / `-truedrive` flags. Standalone VICE-named binaries are not yet shipped. See [docs/USER-GUIDE.md](docs/USER-GUIDE.md) for the full install and first-run walkthrough, and [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md) for a side-by-side flag mapping.

## Install

The current line is **v1.2.2** (GitVersion `next-version`; NuGet + MSI / winget packaging). Published nuget.org / winget packages may lag until the next release tag.
30→
```pwsh
# Desktop UI as a dotnet global tool (command: vicesharp)
dotnet tool install --global ViceSharp.Avalonia

# Console reference shell as a dotnet global tool (command: vicesharp-console)
dotnet tool install --global ViceSharp.Console

# Embed the emulation core (Abstractions + Chips + RomFetch + Core + Architectures) in your own app
dotnet add package ViceSharp.Core
40→```

Individual packages (`ViceSharp.Protocol`, `ViceSharp.Monitor`, `ViceSharp.Launcher`, `ViceSharp.AdhocHelper`, `ViceSharp.Host`, `ViceSharp.SourceGen`, and the `ViceSharp.Host.MacOS` / `Android` / `iOS` / `Xbox` shells) are published alongside the bundle. The Windows desktop app is also packaged as a self-contained MSI (Nuke `PublishMsi`) with winget metadata (`PublishWinget`, package id `sharpninja.ViceSharp`).

## User documentation

- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) - install, first run, CLI launcher, YAML topology, disk images, capture, diagnostics attach, what works today
- [docs/VICE-MIGRATION.md](docs/VICE-MIGRATION.md) - binary + flag mapping, behaviour caveats, performance / accuracy, bug compatibility
- [docs/ROMs.md](docs/ROMs.md) - legal ROM options, environment variable, directory layout
- [docs/](docs/README.md) - full documentation index (architecture, public API, iteration plans, diagrams)
50→
## Status

✅ **Iteration 0 (Foundations)**: Complete. All core primitives implemented, lock-free and zero allocation.
✅ **Iteration 1 (C64 Bringup)**: **Complete (Phase 1 closed 2026-05-31; diagnostics/attach surface updated 2026-06-25)**:
  - x64sc lockstep and D64 attach paths are covered across deterministic no-cartridge variants
  - Perf: 11.5M+ cycles/sec under release JIT (47x the Phase 1 PERF-TUNING-001 target of 246,312 cps; 1173% PAL real-time)
  - Snapshot/capture/input/testbench/launcher surfaces are in place, including gRPC capture and diagnostics services
  - Desktop packaging is self-contained JIT + ReadyToRun through Nuke `PublishMsi`; native ahead-of-time publishing is no longer a project requirement
  - External debuggers can attach deterministically through `%LOCALAPPDATA%\ViceSharp\debug-attach.json` and `DiagnosticsService`
60→
✅ **Iteration 2 (VIC-20)**: **Core + every-cycle native lockstep (2026-08-06)**:
  - Managed VIC-20 (PAL/NTSC), dual VIA, VIC-I, color RAM open-bus, default drive 8 = 1540
  - Native oracle: `vice_xvic.dll` / `ViceNative.CreateInstance("vic20"|"vic20ntsc")`
  - Every-cycle A/X/Y/S/P/PC vs xvic: **TenSecondPal** 11_084_050 and **TenSecondNtsc** 10_227_270 green
  - Receipts: `docs/receipts-lockstep-10s-2026-08-06.txt`, `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`
  - Gates (env): `VICESHARP_LOCKSTEP_10S=1`, `VICESHARP_LOCKSTEP_2S=1`; filter `FullyQualifiedName~Vic20DivergeProbe`
  - Tier-A polish still open: expansion UX, input E2E, cart/disk depth, snapshots (see [HANDOFF.md](HANDOFF.md))

Working chip layer implementations:
70→  - `Mos6510` CPU (opcodes + core)
  - `Mos6569` VIC-II
  - `Mos6526` CIA
  - `Mos6581` SID (noise LFSR + voice 3 OSC3/ENV3 readback)
  - Folders for Cpu/Cia/Sid/VicIi

Bounded runtime validation slices are implemented for 1541/D64 attach+sector reads, TAP datasette pulse reads, standard 8K/16K cartridge mapping, runtime snapshot save/load, and BMP frame capture. Full subsystem parity for advanced drive, tape, cartridge, snapshot, and media workflows remains future scope.

## Completion Dashboard

80→Snapshot of VICE-to-ViceSharp parity sourced from MCP TODO state and the iteration roadmap. Last refreshed `2026-08-06` on `main` (line **v1.2.2**; VIC-20 every-cycle 10s PAL+NTSC lockstep green; Store art + CC BY-SA C= logo attribution; see [HANDOFF.md](HANDOFF.md)). Perf probe: 11.5M+ cycles/sec (47x the Phase 1 PERF-TUNING-001 target of 246,312 cps). Wiki publish: automated via `tools/Publish-Wiki.ps1` + Nuke `PublishWiki`. Advanced cartridge mappers: all 7 mappers landed as minimum-viable scaffolds. PLATFORM-CROSS-001: macOS, Xbox, Android, iOS host shells scaffolded.

**Legend**: State: ✅ done · 🟢 active · 🟡 bounded gate done, deepening pending · ⚪ planned

### Iteration 0: Foundations

| Feature | State | % | Source |
|---------|:----:|:----:|--------|
| .NET 10 + Nuke build pipeline | ✅ | 100% | iteration0 batch 1 |
| MCP Server + TODO workspace integration | ✅ | 100% | iteration0 batch 2 |
90→| Documentation set (Architecture, Public API, Roadmap) | ✅ | 100% | iteration0 batch 3 |
| GraphRAG ingest | ✅ | 100% | iteration0 batch 4 |

### Iteration 1: C64 Bringup

| Feature | State | % | Source |
|---------|:----:|:----:|--------|
| MOS 6510 CPU (official + illegal opcodes) | ✅ | 100% | `LockstepValidationTests.First100000CyclesMatch` |
| Processor port `$00/$01` + interrupts (IRQ/NMI/RDY/RES) | ✅ | 100% | lockstep gate |
| MOS 6569 VIC-II (raster IRQ + bad line + sprite collision/IRQ + sprite Y-exp/multicolor + sprite DMA + sprite-DMA stall + visible sprite composition + sprite priority + light pen + color/register read masks + $D018/$D016/$D011 decoding + display mode selection + VICE display-mode pixel color routing + $D015/$D010 sprite registers + managed continuous side-border behavior + managed matrix idle/fill behavior + RC window cycle-accurate + non-PAL sprite DMA tables + screen-RAM checkpoint) | ✅ | 100% | `BACKFILL-VIDEO-001` closed (Phase 1 slice 1; FLI/AFLI deepening continues post-Phase 1) |
100→| MOS 6526 CIA1/CIA2 (timers + TOD 12-hour + timer-B chain + SDR + FLAG pin + force-load + keyboard scan + joystick scan + ICR) | ✅ | 100% | `BACKFILL-CIA` + base input scan coverage |
| MOS 6581 SID (hard sync + ring mod + combined waveforms 6581 + 8580 + ADSR bug + PCM equiv + $D418 digi + audio backend + filter 6581 + non-linear cutoff curve + dual-SID + noise LFSR + determinism) | ✅ | 100% | `BACKFILL-SID-001` closed; 8580 filter deepening is post-MVP |
| MOS 6522 VIA (timer-1 PB7 + timer-2 phi2+PB6 + SR modes + CA1/CB1 edge IRQ + CA2/CB2 handshake/manual/pulse) | ✅ | 100% | `BACKFILL-VIA` complete |
| Mos6510 CPU interrupts (NMI vector + BRK B-flag + IRQ vector) | ✅ | 100% | `BACKFILL-CPU` complete |
| PLA + Memory map ($0000-$FFFF) | ✅ | 100% | boot proof |
| Reset sequencing (7-cycle + port init) | ✅ | 100% | reset tests |
| ROM loader (KERNAL/BASIC/CHARGEN + SHA1) | ✅ | 100% | `BasicBootProofTests` |
| 1541 / IEC / D64 (attach + deterministic sector reads + IEC ATN timing + 1541 motor ramp) | ✅ | 100% | `ARCH-TRUEDRIVE-1541-002` Phase 1 close (slice 2) |
| Datasette / TAP (pulse reads + CIA1 FLAG + builder wiring + rewind/seek + 32k motor ramp + SenseLine + record buffer) | ✅ | 100% | `RUNTIME-TAPE-002` Phase 1 close (slice 3) |
| Standard cartridge mapping (8K/16K raw + CRT + GAME/EXROM live map) | ✅ | 100% | `RUNTIME-CART-002` validated for standard cartridges; broad mapper families are post-MVP |
110→| Runtime snapshot (CPU A/X/Y/S/P/PC + 64K + VIC + CIA TOD + SID ADSR round-trip) | ✅ | 100% | `RUNTIME-SNAPSHOT-002` Phase 1 close (slice 4) |
| Media export (PNG/BMP screenshot + WAV sound + BMP sequence all/unique + muxed MP4/MKV/AVI video via ffmpeg, over the gRPC capture surface) | ✅ | 100% | `RUNTIME-CAPTURE-002` + `FR-MED-002/003/004`; ffmpeg-backed muxed video mirrors VICE `ffmpegexedrv` |
| Keyboard matrix + control-port parity (LoadFromFile + EnumerateDevices + 30-frame key-repeat hold) | ✅ | 100% | `BACKFILL-INPUT-001` Phase 1 close (slice 5) |
| Host UI + Monitor control surface (10 services + 8 adapters + view model + registry + mapper + frame source + InProcessGrpcHost + 2 clients; ~230 tests) | ✅ | 100% | `BACKFILL-HOSTUI-001` closeable; launcher/UI shell work is tracked separately |
| Core primitives (SystemClock + DoubleBufferedMutationQueue + LockFreePubSub + BasicBus + SimpleRam) | ✅ | 100% | TR-PUBSUB-PERFORMANCE + TR-Cycle-Accuracy + TR-System-Core |
| Chip/package boundary audit (shared chips vs machine/device glue) | ✅ | 100% | `ARCH-CHIPGLUE-001` closed with `TEST-ARCH-CHIPGLUE-001`; focused gate 579/579 and lockstep/checkpoint 335/335 |
| x64sc variant lockstep gate (10-frame depth across no-cart variants, 322 lockstep tests green) | ✅ | 100% | `BACKFILL-LOCKSTEP-001` Phase 1 close (slice 7) |
| Upstream VICE testbench integration (debugcart + limitcycles + PRG autostart + help text + ROM-less smoke) | ✅ | 100% | `ARCH-TESTBENCH-001` + `CLI-LAUNCHER-001` Phase 1 close (slice 6) |

### Iterations 2-5: Other Machines
120→
| Machine | Target Iteration | State | % |
|---------|:----------------:|:----:|:----:|
| SX-64 | 1 | ⚪ | 0% |
| VIC-20 (MOS 6502 + VIC-I + VIA x2) | 2 | ✅ | 95% |
| C128 (MOS 8502 + VIC-IIe + Z80) | 3 | ⚪ | 0% |
| PET (MOS 6502 + PIA/VIA + CRTC) | 4 | ⚪ | 0% |
| Plus/4 / C16 (MOS 7501 + TED) | 5 | ⚪ | 0% |

### Tooling and Ecosystem
130→
| Feature | State | % | Source |
|---------|:----:|:----:|--------|
| XMLDOCS test contract (cite FR/TR, use case, acceptance) | ✅ | 100% | `QA-XMLDOCS-001` CLOSED: ratchet baseline at 0 (full retrofit + `XmlDocsConventionTests.ExpectedMaxViolations=0`) |
| BenchmarkDotNet harness vs native VICE | 🟡 | 60% | `PERF-TUNING-001` Phase 1 close (slice 8; PerfProbe measured 11.5M+ cycles/sec = 47x the 25% target). `PERF-BENCHMARK-001` native baseline + sweep deferred post-Phase 1. |
| Repository maintenance + github wiki | 🟢 | 35% | `REPO-MAINT-001` (audit + plan in [docs/maintenance/](docs/maintenance/), execution deferred) |
| Ad-hoc machine YAML schema + Console loader + Avalonia 12 helper | 🟢 | 60% | `ARCH-ADHOCMACHINE-001` (schema + loader + `--machine-yaml` flag, helper app deferred) |
| Cross-platform hosts (UWP Xbox + Avalonia 12 mobile + MacOS) | 🟢 | 15% | `PLATFORM-CROSS-001` (wireframes in [docs/wireframes/](docs/wireframes/README.md), host code pending) |
| Completion Dashboard (this section) | ✅ | 100% | `DOC-DASHBOARD-001` Phase 1 close (slice 9) |

140→Dashboard is regenerated as subagent slices land. VIC-20 every-cycle lockstep (2026-08-06): PAL 10 s and NTSC 10 s green (`Vic20DivergeProbe`); see durable receipts under `docs/receipts-lockstep-10s-*.txt`. Full-suite baseline totals still wobble with theory-row serialization; use 0 failed as the green criterion (see [HANDOFF.md](HANDOFF.md)).

## Supported Machines (planned)

| Machine | Architecture | Status |
|---------|-------------|--------|
| C64 / C64C | MOS 6510 + VIC-II + SID + CIA x2 | Iteration 1 |
| SX-64 | Same as C64 (built-in monitor + 1541) | Iteration 1 |
| VIC-20 | MOS 6502 + VIC-I + VIA x2 | Iteration 2 (core + 10 s lockstep on `main`) |
| C128 | MOS 8502 + VIC-IIe + SID + CIA x2 + Z80 | Iteration 3 |
150→| PET | MOS 6502 + PIA/VIA + CRTC | Iteration 4 |

### Turn 37 (Function)
[tool_response]
1→schema: mcp-wiki-export/v1
home:
  document: home
documents:
- id: home
  title: Home
  source: generated:home
  target: Home.md
  platforms:
10→  - github
  - azure
- id: functional
  title: Functional Requirements
  source: generated:functional
  target: Functional-Requirements.md
  platforms:
  - github
  - azure
- id: technical
20→  title: Technical Requirements
  source: generated:technical
  target: Technical-Requirements.md
  platforms:
  - github
  - azure
- id: testing
  title: Testing Requirements
  source: generated:testing
  target: Testing-Requirements.md
30→  platforms:
  - github
  - azure
- id: mapping
  title: TR per FR Mapping
  source: generated:mapping
  target: TR-per-FR-Mapping.md
  platforms:
  - github
  - azure
40→- id: matrix
  title: Requirements Matrix
  source: generated:matrix
  target: Requirements-Matrix.md
  platforms:
  - github
  - azure
- id: project-overview
  title: Project Overview
  source: README.md
50→  target: Project-Overview.md
  platforms:
  - github
  - azure
- id: user-guide
  title: User Guide
  source: docs/USER-GUIDE.md
  target: User-Guide.md
  platforms:
  - github
60→  - azure
- id: vice-migration
  title: Migrating from Classic VICE
  source: docs/VICE-MIGRATION.md
  target: VICE-Migration.md
  platforms:
  - github
  - azure
- id: rom-setup
  title: ROM Setup and Legal Sources
70→  source: docs/ROMs.md
  target: ROM-Setup.md
  platforms:
  - github
  - azure
- id: privacy
  title: Privacy Policy
  source: docs/PRIVACY.md
  target: Privacy-Policy.md
  platforms:
80→  - github
  - azure
- id: xbox-store-publishing
  title: Xbox Microsoft Store Publishing
  source: docs/xbox-store-publishing.md
  target: Xbox-Store-Publishing.md
  platforms:
  - github
  - azure
- id: store-listing-copy
90→  title: Store Listing Copy
  source: docs/xbox/store-listing-copy.md
  target: Store-Listing-Copy.md
  platforms:
  - github
  - azure
- id: gpl-store-section6
  title: GPL vs Microsoft Store Review
  source: docs/xbox/gpl-store-section6-review.md
  target: GPL-Store-Section6-Review.md
100→  platforms:
  - github
  - azure
- id: store-next-steps
  title: Store Next Steps Guide
  source: docs/xbox/store-next-steps-guide.md
  target: Store-Next-Steps-Guide.md
  platforms:
  - github
  - azure
110→- id: store-product-identity
  title: Store Product Identity
  source: docs/xbox/store-product-identity.md
  target: Store-Product-Identity.md
  platforms:
  - github
  - azure
- id: ado-store-setup
  title: ADO Store Pipeline Setup
  source: docs/xbox/ado-store-setup.md
120→  target: ADO-Store-Setup.md
  platforms:
  - github
  - azure
- id: architecture
  title: Architecture
  source: docs/Architecture.md
  target: Architecture.md
  platforms:
  - github
130→  - azure
- id: public-api
  title: Public API Reference
  source: docs/Public-API.md
  target: Public-API.md
  platforms:
  - github
  - azure
- id: pubsub
  title: Pub-Sub and Mutation Queue
140→  source: docs/PubSub.md
  target: PubSub.md
  platforms:
  - github
  - azure
- id: decoupling
  title: Video and Audio Decoupling
  source: docs/Decoupling.md
  target: Video-Audio-Decoupling.md
  platforms:
150→  - github
  - azure
- id: state-window
  title: State Window (Design Proposal)
  source: docs/StateWindow.md
  target: State-Window.md
  platforms:
  - github
  - azure
- id: iteration-roadmap
160→  title: Iteration Roadmap
  source: docs/Iteration-Roadmap.md
  target: Iteration-Roadmap.md
  platforms:
  - github
  - azure
- id: ai-review
  title: AI Code and Project Review (aiUnit)
  source: docs/AI-Review.md
  target: AI-Review.md
170→  platforms:
  - github
  - azure
- id: registry-of-lore
  title: Registry of Lore
  source: docs/ROL.md
  target: Registry-of-Lore.md
  platforms:
  - github
  - azure
180→- id: fr-overview
  title: Functional Requirements Overview
  source: docs/requirements/functional/FR-Overview.md
  target: Functional-Requirements-Overview.md
  platforms:
  - github
  - azure
- id: fr-xbox-branding
  title: FR Xbox Branding (C= logo attribution)
  source: docs/requirements/functional/FR-Xbox-Branding.md
190→  target: FR-Xbox-Branding.md
  platforms:
  - github
  - azure
- id: tr-overview
  title: Technical Requirements Overview
  source: docs/requirements/technical/TR-Overview.md
  target: Technical-Requirements-Overview.md
  platforms:
  - github
200→  - azure
- id: test-requirements
  title: Test Requirements
  source: docs/requirements/test/TEST-Requirements.md
  target: Test-Requirements.md
  platforms:
  - github
  - azure
- id: x64sc-model-matrix
  title: x64sc Model Matrix
210→  source: docs/requirements/backfill/X64SC-Model-Matrix.md
  target: X64sc-Model-Matrix.md
  platforms:
  - github
  - azure
- id: x64sc-requirement-coverage
  title: x64sc Requirement Coverage
  source: docs/requirements/backfill/X64SC-Requirement-Coverage.md
  target: X64sc-Requirement-Coverage.md
  platforms:
220→  - github
  - azure
- id: vice-edge-case-backfill
  title: Classic VICE Edge-Case TR Backfill
  source: docs/requirements/backfill/Classic-VICE-Edge-Case-TR-Backfill.md
  target: Classic-VICE-Edge-Case-TR-Backfill.md
  platforms:
  - github
  - azure
- id: vice-source-manifest
230→  title: VICE Source Manifest
  source: docs/requirements/sources/VICE-Source-Manifest.md
  target: VICE-Source-Manifest.md
  platforms:
  - github
  - azure
- id: machine-architecture-schema
  title: Ad-Hoc Machine Architecture YAML Schema (v1)
  source: docs/schemas/machine-architecture.schema.md
  target: Machine-Architecture-Schema.md
240→  platforms:
  - github
  - azure
- id: cross-platform-wireframes
  title: Cross-Platform UI Wireframes
  source: docs/wireframes/README.md
  target: Cross-Platform-Wireframes.md
  platforms:
  - github
  - azure
250→- id: desktop-windows-ui
  title: Desktop UI Reference (Windows, Avalonia)
  source: docs/wireframes/desktop-windows.md
  target: Desktop-Windows-UI.md
  platforms:
  - github
  - azure
navigation:
- title: Home
  path: Home
260→  children:
  - document: home
  - document: functional
  - document: technical
  - document: testing
  - document: mapping
  - document: matrix
- title: Getting Started
  path: Getting-Started
  children:
270→  - document: project-overview
  - document: user-guide
  - document: vice-migration
  - document: rom-setup
  - document: privacy
- title: Architecture
  path: Architecture
  children:
  - document: architecture
  - document: public-api
280→  - document: pubsub
  - document: decoupling
  - document: state-window
  - document: iteration-roadmap
  - document: ai-review
  - document: registry-of-lore
- title: Requirements
  path: Requirements
  children:
  - document: fr-overview
290→  - document: fr-xbox-branding
  - document: tr-overview
  - document: test-requirements
  - document: x64sc-model-matrix
  - document: x64sc-requirement-coverage
  - document: vice-edge-case-backfill
  - document: vice-source-manifest
- title: Store and Xbox
  path: Store-and-Xbox
  children:
300→  - document: xbox-store-publishing
  - document: store-listing-copy
  - document: gpl-store-section6
  - document: store-next-steps
  - document: store-product-identity
  - document: ado-store-setup
- title: Reference
  path: Reference
  children:
  - document: machine-architecture-schema
310→  - document: cross-platform-wireframes
  - document: desktop-windows-ui


### Turn 38 (Function)
[tool_response]
exit: 0
 M build/Build.cs
 M native/vice-shim-vic20.c
 M native/vice-shim.h
 M src/ViceSharp.Abstractions/IViceNative.cs
 M src/ViceSharp.Architectures/Vic20/Vic20Descriptor.cs
 M src/ViceSharp.Architectures/Vic20/Vic20Expansion.cs
 M src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs
 M src/ViceSharp.Avalonia/Views/SettingsView.axaml
 M src/ViceSharp.Avalonia/Views/SettingsView.axaml.cs
 M src/ViceSharp.Chips/Vic/Mos6561.cs
 M src/ViceSharp.Core/ArchitectureBuilder.cs
 M src/ViceSharp.Core/Vic20/Vic20MemoryLayout.cs
 M src/ViceSharp.Core/Vic20/Vic20SystemRam.cs
 M src/ViceSharp.Core/ViceNative.Xvic.cs
 M src/ViceSharp.Core/ViceNative.cs
 M src/ViceSharp.Host.InProcess/Runtime/ConsoleEmulatorHost.cs
 M src/ViceSharp.Host.InProcess/Runtime/DefaultEmulatorRuntimeFactory.cs
 M src/ViceSharp.Host.InProcess/Runtime/EmulatorRuntimeSession.cs
 M src/ViceSharp.Host.InProcess/Services/HostProtocolMapper.cs
 M src/ViceSharp.Host.InProcess/Services/MediaServiceHost.cs
 M src/ViceSharp.Host.InProcess/Services/SettingsServiceHost.cs
 M src/ViceSharp.Protocol/ProtocolContracts.cs
 M src/ViceSharp.Protocol/SettingsOptionCatalog.cs
 M src/ViceSharp.Xbox.ViewModels/AboutInfo.cs
 M src/ViceSharp.Xbox.ViewModels/AboutViewModel.cs
 M src/ViceSharp.Xbox.ViewModels/NavigationDestination.cs
 M src/ViceSharp.Xbox.ViewModels/VideoFramePullViewModel.cs
 M src/ViceSharp.Xbox.ViewModels/XboxSettingsViewModel.cs
 M src/ViceSharp.Xbox/App.xaml.cs
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml
 M src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs
 M tests/ViceSharp.TestHarness/Vic20/Vic20DivergeProbe.cs
 M tests/ViceSharp.TestHarness/Vic20/Vic20MemoryMapTests.cs
 M tests/ViceSharp.TestHarness/Vic20/Vic20VideoTests.cs
 M tests/ViceSharp.TestHarness/Xbox/AboutViewModelTests.cs
 M tests/ViceSharp.TestHarness/Xbox/Fakes/FakeXboxSettingsGateway.cs
 M tests/ViceSharp.TestHarness/Xbox/VideoFramePullViewModelTests.cs
 M tests/ViceSharp.TestHarness/Xbox/XboxComputerModelPickerTests.cs
 M tests/ViceSharp.TestHarness/Xbox/XboxSettingsViewModelTests.cs
?? docs/audit-vic20-vs-vice-2026-08-07.md
?? docs/bccsta-focused-2026-08-06.log
?? docs/beq-focused-2026-08-06.log
?? docs/beqpla-focused-2026-08-06.log
?? docs/bit-focused-2026-08-06.log
?? docs/bit2-focused-2026-08-06.log
?? docs/bit3-focused-2026-08-06.log
?? docs/bit4-focused-2026-08-06.log
?? docs/bit5-focused-2026-08-06.log
?? docs/bitf-focused-2026-08-06.log
?? docs/bitf2-focused-2026-08-06.log
?? docs/bnehold-focused-2026-08-06.log
?? docs/bpl-focused-2026-08-06.log
?? docs/bpl2-focused-2026-08-06.log
?? docs/bpl3-focused-2026-08-06.log
?? docs/bpl4-focused-2026-08-06.log
?? docs/bpl5-focused-2026-08-06.log
?? docs/bpl6-focused-2026-08-06.log
?? docs/bpl7-focused-2026-08-06.log
?? docs/cmpzp-focused-2026-08-06.log
?? docs/cmpzp2-focused-2026-08-06.log
?? docs/focused-b1-beq-2026-08-06.log
?? docs/focused-beq-hold-2026-08-06.log
?? docs/focused-cpx-soft-2026-08-06.log
?? docs/focused-final-2026-08-06.log
?? docs/focused-gates-final-2026-08-06.log
?? docs/focused-holdindy-2026-08-06.log
?? docs/focused-incsticky-2026-08-06.log
?? docs/focused-indy-nz-2026-08-06.log
?? docs/focused-iny-fuse-2026-08-06.log
?? docs/focused-irq-fuse-lda-2026-08-06.log
?? docs/focused-irqb-2026-08-06.log
?? docs/focused-irqfall-2026-08-06.log
?? docs/focused-irqsuppress-2026-08-06.log
?? docs/focused-jsr-irq-2026-08-06.log
?? docs/focused-jsr-irq2-2026-08-06.log
?? docs/focused-jsr-jump-2026-08-06.log
?? docs/focused-jsr-ldy-2026-08-06.log
?? docs/focused-lda-indy-2026-08-06.log
?? docs/focused-ldy-fuse-2026-08-06.log
?? docs/focused-ldy-plain-2026-08-06.log
?? docs/focused-ora-2026-08-06.log
?? docs/focused-revert-irq-2026-08-06.log
?? docs/focused-ror-chain-2026-08-06.log
?? docs/focused-ror-chain2-2026-08-06.log
?? docs/focused-ror-rti-2026-08-06.log
?? docs/focused-ror-zp-beq-2026-08-06.log
?? docs/focused-rts-nonovl-2026-08-06.log
?? docs/focused-rts-store-br-2026-08-06.log
?? docs/focused-rts-trail-eq2-2026-08-06.log
?? docs/focused-rts-trail2-2026-08-06.log
?? docs/focused-shift-phase-2026-08-06.log
?? docs/focused-sta-trail-2026-08-06.log
?? docs/focused-stazp-2026-08-06.log
?? docs/focused-store-2026-08-06.log
?? docs/focused-store-hold-2026-08-06.log
?? docs/focused-store-hold2-2026-08-06.log
?? docs/focused-sty-trail-2026-08-06.log
?? docs/focused-tax-chain-2026-08-06.log
?? docs/focused-vbus-order-2026-08-06.log
?? docs/inx-focused-2026-08-06.log
?? docs/irqrmw-focused-2026-08-06.log
?? docs/jmp-focused-2026-08-06.log
?? docs/jmp2-focused-2026-08-06.log
?? docs/jmp3-focused-2026-08-06.log
?? docs/lda-focused-2026-08-06.log
?? docs/lsrx-focused-2026-08-06.log
?? docs/lsrzp-focused-2026-08-06.log
?? docs/lsrzp2-focused-2026-08-06.log
?? docs/ntsc-t1-phase.txt
?? docs/pla-focused-2026-08-06.log
?? docs/pla-late-focused-2026-08-06.log
?? docs/pla2-focused-2026-08-06.log
?? docs/receipts-lockstep-2s-bcc-2026-08-06.log
?? docs/receipts-lockstep-2s-cmp-2026-08-06.log
?? docs/receipts-lockstep-2s-imm-2026-08-06.log
?? docs/receipts-lockstep-2s-jsr-2026-08-06.log
?? docs/receipts-lockstep-2s-ldaabs-2026-08-06.log
?? docs/receipts-lockstep-2s-ldax-2026-08-06.log
?? docs/receipts-lockstep-2s-ldx-2026-08-06.log
?? docs/receipts-lockstep-2s-ldx2-2026-08-06.log
---UNTRACKED SRC---


### Turn 39 (Function)
[tool_response]
exit: 0
{"status":"available","agent":"ClaudeCode","namespaces":["workflow.sessionlog","workflow.todo","workflow.requirements","workflow.triage","workflow.graphrag","workflow.memory","workflow.failsafe"],"requirementMethods":["workflow.requirements.listLayers","workflow.requirements.createLayer","workflow.requirements.updateLayer","workflow.requirements.effective"],"requirementClientMethods":["client.Requirements.ListRequirementLayersAsync","client.Requirements.CreateRequirementLayerAsync","client.Requirements.UpdateRequirementLayerAsync","client.Requirements.GetEffectiveRequirementsAsync"],"cacheDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\claude","hasSession":true,"hasCurrentTurn":true,"pendingCount":0,"pendingTurnCount":0,"failsafeDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\claude\\failsafe","failsafeCount":0,"failsafeQuarantineCount":4}
---SESSION---
status: verified
sessionId: GrokCode-20260703T131501Z-mousekeyproxy-sync
agent: GrokCode
started: 2026-07-03T13:16:16Z
lastUpdated: 2026-07-09T05:53:46Z
markerFilePath: F:\GitHub\MouseKeyProxy\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-08T20:54:31.3402936Z
---TURN---
turnRequestId: req-20260703T131620Z-001-sync-logs-goal-resume
queryTitle: /sync-logs and /goal resume for MouseKeyProxy impl
openedAt: 2026-07-03T13:16:20Z
status: completed
codeEdits: 28
lastBuildStatus: unknown
auditActions: 25
auditFiles: 28
auditDialog: 0
auditDecisions: 6
queryText: |
  /sync-logs and /goal resume. Reconcile the session log with work on FR-MKP-007, bidi Commands+transport, mock-only tests, commit ee767c7, then resume verification.
auditCommits: 7
sessionId: GrokCode-20260703T131501Z-mousekeyproxy-sync
markerFilePath: F:\GitHub\MouseKeyProxy\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-07-08T20:54:31.3402936Z
---INVOKE-STATUS---

Name
----
agent-runtime-header.ps1
cache-manager.ps1
ensure-repl.ps1
final-response.ps1
Invoke-McpPlugin.ps1
marker-resolver.ps1
mcp-status.ps1
plugin-env.ps1
plugin-hook.ps1
repl-invoke.ps1
resolve-cache-dir.ps1
yaml-object-mutation.ps1



### Turn 40 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# Full untracked that are not probe logs
git ls-files --others --exclude-standard 2>&1 | Where-Object {
  $_ -notmatch 'docs/(2s-|10s-|focused-|bcc|beq|bit|bne|bpl|cmp|inx|irq|jmp|lda|lsr|pla|adc|asl|_deploy|_vs-ready|S-Blox|receipts-lockstep-2s|ntsc-t1)'
} | Select-Object -First 150
Write-Host '---COUNT ALL UNTRACKED---'
(git ls-files --others --exclude-standard 2>&1 | Measure-Object).Count
Write-Host '---SRC UNTRACKED---'
git ls-files --others --exclude-standard 2>&1 | Where-Object { $_ -match '^(src|tests)/' }

- description: List untracked source and non-log files
[tool_request: read_file]
- target_file: F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\mcpserver-grok-plugin\skills\session\SKILL.md
- limit: 100
[tool_request: grep]
- pattern: FlashCart|first_x|BLK|Ultimem|MegaCart|FE3|expansion
- path: F:\GitHub\vice-sharp\docs
- glob: *.md
- head_limit: 40
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# GitVersion / package version
if (Test-Path GitVersion.yml) { Select-String -Path GitVersion.yml -Pattern 'next-version' }
if (Test-Path Directory.Build.props) { Select-String -Path Directory.Build.props -Pattern 'Version|PackageVersion' | Select-Object -First 10 }
# Flash cart sources exist?
Get-ChildItem -Recurse -Path src -Filter '*Flash*' -ErrorAction SilentlyContinue | Select-Object FullName
Get-ChildItem -Recurse -Path tests -Filter '*Flash*' -ErrorAction SilentlyContinue | Select-Object FullName
Get-ChildItem -Recurse -Path docs/receipts -ErrorAction SilentlyContinue | Select-Object Name, LastWriteTime

- description: Version pins and FlashCart file inventory

### Turn 41 (Function)
[tool_response]
1→---
name: Session Log Management
description: Use when the user asks to "start session", "log session", "begin turn", "update turn", "complete turn", or "query session history"
version: 0.1.0
---

# Session Log Management

## Overview
10→
To manage agent session logs, use the `workflow.sessionlog.*` REPL command namespace through the plugin wrapper (`lib/repl-invoke.ps1` / `Invoke-McpPlugin.ps1`). Session logging captures agent activity, reasoning dialog, file operations, and design decisions as a structured audit trail.

`workflow.sessionlog.*` is a plugin workflow/REPL namespace, not a literal native MCP tool namespace. Native McpServer `/mcp-transport` discovery uses names such as `sessionlog_*`, `todo_*`, and `requirements_*`; hosted-agent adapters may expose `mcp_session_*` aliases, and your agent's tool discovery may show configured MCP tools with native names such as `sessionlog_submit`, `todo_list`, and `requirements_generate`. Do not call this plugin unavailable solely because `workflow.*` names are absent from generic MCP discovery.

`workflow.*` result envelopes may include `deprecated: true`. This is success metadata that tells callers the workflow namespace is legacy-compatible and should migrate toward the canonical `client.*` surface where available; it is not a failure signal and is not a reason to use raw REST. Treat an empty `workflow.sessionlog.queryHistory` result as a valid no-match result, not as an inert wrapper. Re-check the workspace current directory, the explicit `agent` or `sourceType`, and local plugin cache/session state through the wrapper before reporting history as unavailable.

This skill covers manual operations, history queries, and the full lifecycle for agents that need direct control.

## Preferred Workflow
20→
For most work sessions:

1. Bootstrap session logging once.
2. Open or resume the session.
3. On each user request, begin a turn.
4. Consult current session/task state before asking the user for context.
5. Update the turn with relevant files, decisions, and actions.
6. Complete the turn after verification, or record failure if blocked.

30→## Running the Workflow Methods

When the plugin's hooks (in `hooks/`) are active, most session management is automated and you do not need to drive it by hand. When hooks are not active for your agent, drive the lifecycle yourself through the plugin wrapper, never by hand-piping envelopes to REPL stdio:

```pwsh
pwsh -NoProfile -File "<plugin-root>/lib/repl-invoke.ps1" -Method <method> -ParamsYaml @'
<yaml params>
'@
```

40→The equivalent function form is `Invoke-McpPlugin.ps1 "<method>" "<yaml params>"`. For a one-shot bootstrap plus session open, prefer `<plugin-root>/lib/session-start.ps1 <workspace-path>`.

The YAML envelope blocks in this skill document the wire contract each method maps to. Pass the `params:` body to the wrapper; it wraps it in the request envelope, validates documented params, and emits single-line JSON to REPL stdio for you. If you bypass the wrapper for diagnostics and write to REPL stdio directly, send one single-line JSON request envelope per message, never formatted YAML.

Why the wrapper and not raw stdio:

- `workflow.sessionlog.beginTurn` and `openSession` are not server routes. Calling them raw returns `method_invocation_error` / `method_not_found`. The wrapper treats them as local no-ops and tracks turn state in `cache/current-turn.yaml`.
- Persisting a turn goes through `client.SessionLog.SubmitAsync`, which is strict: `actions[].order` must be an unquoted integer (`order: 1`, never `order: "1"`) or you get `JSON value could not be converted to System.Int32`. The wrapper builds the envelope with correct types.
- There is no `session.init` method. Bootstrap with `workflow.sessionlog.bootstrap` only.

50→## Identifier Naming Conventions

### Session IDs

Format: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`

Regex: `^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`

- Agent name must be PascalCase (e.g. `YourAgent`)
- Timestamp must be ISO 8601 compact UTC: `yyyyMMddTHHmmssZ`
60→- Suffix must be lowercase kebab-case: `feature-auth`, `bugfix-timeout`

Valid examples: `YourAgent-20260409T120000Z-implement-auth`, `YourAgent-20260304T113901Z-refactor-session`

Invalid: `youragent-20260409T120000Z-task` (lowercase agent), `YourAgent-20260304-feature` (missing time component)

### Request IDs

Format: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`

70→Regex: `^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`

Valid examples: `req-20260409T120001Z-add-jwt-001`, `req-20260409T120002Z-query-todos`

Invalid: `request-20260409T120001Z-task` (wrong prefix), `req-20260409-task` (missing time)

## Session Lifecycle

### Step 1 - Bootstrap (once per process lifetime)

80→Bootstrap initializes the session log subsystem. This operation is idempotent:

```yaml
type: request
payload:
  requestId: req-20260409T120000Z-bootstrap-001
  method: workflow.sessionlog.bootstrap
  params: {}
```

90→```yaml
type: result
payload:
  requestId: req-20260409T120000Z-bootstrap-001
  result:
    initialized: true
```

### Step 2 - Open Session

100→To create a new session record at the start of a work session:

```yaml
type: request
payload:
  requestId: req-20260409T120001Z-open-001
  method: workflow.sessionlog.openSession
  params:
    agent: YourAgent
    sessionId: YourAgent-20260409T120001Z-implement-auth
110→    title: Implement JWT authentication
    model: <model-id>
```

```yaml
type: result
payload:
  requestId: req-20260409T120001Z-open-001
  result:
    sessionId: YourAgent-20260409T120001Z-implement-auth
120→    started: 2026-04-09T12:00:01Z
```

### Step 3 - Begin Turn (once per user message)

To start a new turn before working on a user request:

```yaml
type: request
payload:
130→  requestId: req-20260409T120002Z-begin-001
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260409T120002Z-add-jwt-001
    queryTitle: Add JWT authentication
    queryText: Implement JWT token generation and validation for the API
```

```yaml
type: result
140→payload:
  requestId: req-20260409T120002Z-begin-001
  result:
    turnRequestId: req-20260409T120002Z-add-jwt-001
    status: in_progress
    timestamp: 2026-04-09T12:00:02Z
```

### Step 4 - Update Turn

150→To record interpretation, response summary, tags, and referenced files during work:

```yaml
type: request
payload:
  requestId: req-20260409T120003Z-update-001
  method: workflow.sessionlog.updateTurn
  params:
    response: Created TokenService and JwtValidator classes
    interpretation: User wants JWT authentication with token generation and validation
160→    tokenCount: 1250
    tags:
      - feature
      - security
      - FR-AUTH-001
    contextList:
      - src/Services/TokenService.cs
      - src/Services/JwtValidator.cs
```

170→### Step 5 - Append Dialog

To record reasoning steps, tool calls, observations, and decisions as the work progresses:

```yaml
type: request
payload:
  requestId: req-20260409T120004Z-dialog-001
  method: workflow.sessionlog.appendDialog
  params:
180→    dialogItems:
      - timestamp: 2026-04-09T12:00:04Z
        role: model
        content: Analyzing authentication requirements and existing patterns...
        category: reasoning
      - timestamp: 2026-04-09T12:00:05Z
        role: model
        content: |
          Decision: Use HS256 for JWT signing.
          Rationale: Symmetric key simplifies key management for this internal service.
190→          Alternatives: RS256 adds key distribution complexity without benefit here.
        category: decision
```

Valid `category` values: `reasoning`, `tool_call`, `tool_result`, `observation`, `decision`.

Valid `role` values: `model`, `tool`, `system`, `user`.

### Step 6 - Append Actions

200→To record file operations and other work artifacts:

```yaml
type: request
payload:
  requestId: req-20260409T120005Z-actions-001
  method: workflow.sessionlog.appendActions
  params:
    actions:
      - order: 1
210→        description: Created TokenService with JWT generation
        type: create
        status: completed
        filePath: src/Services/TokenService.cs
      - order: 2
        description: Created JwtValidator for token validation
        type: create
        status: completed
        filePath: src/Services/JwtValidator.cs
      - order: 3
220→        description: Chose HS256 for JWT signing (internal service, symmetric key)
        type: design_decision
        status: completed
        filePath: ""
```

Standard action `type` values: `edit`, `create`, `delete`, `design_decision`, `commit`, `pr_comment`, `issue_comment`, `web_reference`, `dependency_add`.

`actions[].order` must be an unquoted integer (`order: 1`, never `order: "1"`).

230→### Step 7 - Complete Turn

To finalize a turn as successfully completed (immutable after this call):

```yaml
type: request
payload:
  requestId: req-20260409T120006Z-complete-001
  method: workflow.sessionlog.completeTurn
  params:
240→    response: |
      JWT authentication implemented:
      - TokenService generates HS256-signed tokens
      - JwtValidator validates and extracts claims
      - Services registered in Startup.cs
      - All unit tests passing
```

### Failing a Turn

250→To mark a turn as failed with an error description:

```yaml
type: request
payload:
  requestId: req-20260409T120007Z-fail-001
  method: workflow.sessionlog.failTurn
  params:
    errorMessage: Unable to complete - missing System.IdentityModel.Tokens.Jwt package
    errorCode: dependency_missing
260→```

Both `completeTurn` and `failTurn` produce an immutable terminal state. No further `updateTurn`, `appendDialog`, or `appendActions` calls are allowed on a completed or failed turn.

## Querying Session History

To browse previous sessions for context continuity:

```yaml
type: request
270→payload:
  requestId: req-20260409T120008Z-history-001
  method: workflow.sessionlog.queryHistory
  params:
    agent: YourAgent
    limit: 10
    offset: 0
```

```yaml
280→type: result
payload:
  requestId: req-20260409T120008Z-history-001
  result:
    sessions:
      - agent: YourAgent
        sessionId: YourAgent-20260409T120001Z-implement-auth
        title: Implement JWT authentication
        model: <model-id>
        started: 2026-04-09T12:00:01Z
290→        lastUpdated: 2026-04-09T12:30:00Z
        status: completed
        turnCount: 3
        filesModifiedCount: 5
        tags: [auth, jwt, security]
    totalCount: 1
    offset: 0
    limit: 10
```

300→Omit `agent` to query across all agents. Use `offset` and `limit` for pagination.

## Turn Lifecycle State Machine

A turn transitions through these states only in forward order:

1. `in_progress` - created via `beginTurn`, open for updates
2. `completed` - finalized via `completeTurn` (immutable)
3. `failed` - finalized via `failTurn` (immutable)

310→Any attempt to call `updateTurn`, `appendDialog`, or `appendActions` on a `completed` or `failed` turn returns a `turn_immutable` error.

## Error Handling

```yaml
type: error
payload:
  requestId: req-20260409T120003Z-update-001
  code: turn_immutable
  message: Turn is immutable (status: completed)
320→  details:
    turnRequestId: req-20260409T120002Z-add-jwt-001
    currentStatus: completed
    hint: Begin a new turn instead
```

Common error codes:

- `session_not_found`: no active session; call `openSession` first
- `session_already_exists`: session ID already in use
330→- `invalid_session_id`: session ID format violation
- `invalid_request_id`: request ID format violation
- `turn_not_found`: no active turn; call `beginTurn` first
- `turn_already_exists`: turn with same request ID already exists
- `turn_immutable`: cannot modify completed or failed turn

## Session Continuity After Restart

After a server restart or agent reconnect:

340→1. Call `workflow.sessionlog.queryHistory` to review past sessions
2. Decide whether to continue in a new session referencing the previous one in the title, or to re-open fresh
3. Re-read `AGENTS-README-FIRST.yaml` for the rotated API key before making any calls
4. Call `bootstrap` again (idempotent) then `openSession` for the new session

## Model-Authored Session Logging

Plugins do not import local chat transcripts for normal session logging. The active model must write its own session log through `workflow.sessionlog.beginTurn`, `workflow.sessionlog.appendDialog`, `workflow.sessionlog.appendActions`, and `workflow.sessionlog.completeTurn` or `workflow.sessionlog.failTurn`.

### Turn Completion
350→
Plugin stop/final-response helpers may complete an active turn through `lib/repl-invoke.ps1`, but the turn payload must already be model-authored through the session-log workflow. The runtime builds request envelopes with PowerShell objects and serializers. Secret values (API keys, Bearer tokens) are redacted before logging.

### Subagent Logging

Subagents are responsible for writing their own turns through the same session-log workflow. Parent or child models may include parent request IDs, agent names, and source metadata as tags or actions when those values are available, but plugin packages must not parse transcript files to populate session logs.

### Manual Recovery

Manual recovery uses the normal `workflow.sessionlog.*` commands or non-plugin server-side transcript ingestion when explicitly invoked outside a plugin package. Plugins must not expose transcript ingestion helpers, skills, endpoint shortcuts, or parser forks.
360→
### Non-Destructive Merge

Server-side rich fields are never overwritten by sparse incoming values. The PowerShell session payload builder uses field-level merge: an empty incoming array never replaces a non-empty server-side array. This allows model-authored `completeTurn` calls to close turns safely after rich fields have already been captured through the session-log workflow.

### Secret Redaction

The following patterns are redacted from model-authored session-log text before logging:

- `X-Api-Key:` header values
370→- `apiKey:` parameter values (20+ character strings)
- `Bearer <token>` values
- `Authorization:` header values

## Implementation Notes

- Use `Invoke-McpPlugin.ps1` from `lib/repl-invoke.ps1` to build and dispatch envelopes.
- Generate request IDs with the current UTC timestamp to guarantee uniqueness: `req-$(date -u +%Y%m%dT%H%M%SZ)-<slug>`.
- Post `beginTurn` before starting any work on a user request; post `completeTurn` or `failTurn` after work ends. Never defer these calls.
- Log all design decisions in `appendDialog` with `category: decision` AND in `appendActions` with `type: design_decision`.
380→- Record all web sources consulted as actions with `type: web_reference` and include the URL in the description.
- At approximately every 10 interactions, verify all turns are persisted and all design decisions are captured before continuing.


### Turn 42 (Function)
[tool_response]
1→#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Status', 'Invoke', 'CompleteTurn')]
    [string]$Command = 'Status',

    [string]$Method,

    [string]$Params,
10→
    [string]$ParamsPath,

    [object]$ParamsObject,

    [string]$Response,

    [string]$ResponsePath,

    [string]$WorkspacePath,
20→
    [string]$PluginRoot = $(if ($env:MCP_PLUGIN_ROOT) { $env:MCP_PLUGIN_ROOT } elseif ($env:CLAUDE_PLUGIN_ROOT) { $env:CLAUDE_PLUGIN_ROOT } else { Split-Path -Parent $PSScriptRoot }),

    [string]$CacheRoot,

    [int]$TimeoutSeconds = $(if ($env:MCP_PLUGIN_TIMEOUT_SECONDS) { [int]$env:MCP_PLUGIN_TIMEOUT_SECONDS } else { 90 })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
30→
$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -ErrorAction Stop
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')

$script:McpPluginParamsObjectBound = $PSBoundParameters.ContainsKey('ParamsObject')
if ($script:McpPluginParamsObjectBound -and ($PSBoundParameters.ContainsKey('Params') -or $PSBoundParameters.ContainsKey('ParamsPath'))) {
    throw '-ParamsObject cannot be combined with -Params or -ParamsPath.'
}

40→$script:McpPluginParamsFromObject = ''
if ($script:McpPluginParamsObjectBound) {
    Import-McpYamlSerializer
    $script:McpPluginParamsFromObject = ConvertTo-Yaml -Data $ParamsObject -Options WithIndentedSequences
}

function Resolve-McpPluginDefaultWorkspacePath {
    $current = (Get-Location).ProviderPath
    if ($current) {
        $dir = $current
50→        for ($i = 0; $i -lt 20 -and $dir; $i++) {
            $marker = Join-Path $dir 'AGENTS-README-FIRST.yaml'
            if (Test-Path -LiteralPath $marker -PathType Leaf) {
                return (Resolve-Path -LiteralPath $dir).ProviderPath
            }

            $parent = Split-Path -Parent $dir
            if ($parent -eq $dir) { break }
            $dir = $parent
        }
60→    }

    $configured = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:CODEX_WORKSPACE_PATH,
        $env:CODEX_PROJECT_DIR,
        $env:CLAUDE_PROJECT_DIR
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1

70→    if ($configured) { return (Resolve-Path -LiteralPath $configured).ProviderPath }
    return $current
}

$script:McpPluginWorkspacePathExplicit = $PSBoundParameters.ContainsKey('WorkspacePath')
if (-not $script:McpPluginWorkspacePathExplicit -or [string]::IsNullOrWhiteSpace($WorkspacePath)) {
    $WorkspacePath = Resolve-McpPluginDefaultWorkspacePath
}

$script:McpPluginInvocation = New-McpPluginInvocationOptions `
80→    -Command $Command `
    -Method ($Method ?? '') `
    -Params $(if ($script:McpPluginParamsObjectBound) { $script:McpPluginParamsFromObject } else { $Params ?? '' }) `
    -ParamsPath ($ParamsPath ?? '') `
    -ParamsObject $(if ($script:McpPluginParamsObjectBound) { $ParamsObject } else { $null }) `
    -Response ($Response ?? '') `
    -ResponsePath ($ResponsePath ?? '') `
    -WorkspacePath $WorkspacePath `
    -PluginRoot $PluginRoot `
    -CacheRoot ($CacheRoot ?? '') `
90→    -TimeoutSeconds $TimeoutSeconds

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    return $resolved.ProviderPath
}

function Resolve-OptionalDirectory {
100→    param([Parameter(Mandatory)][string]$Path)

### Turn 43 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 29 matching lines
F:\GitHub\vice-sharp\docs\audit-vic20-vs-vice-2026-08-07.md
63:- **VICE:** `vic-draw.c` + full line buffer; `raster_line_draw_borders`; host `first_x` via `video_viewport_resize` / `video_canvas_refresh`  
66:  - **Exact** for normal READY present window (PAL first_x=48, L+R borders, paper origin 48): tests `ReadyPal_NormalBorderWindow_*`, `ViewportFirstX_ReadyPal_*`, `SpaceGlyph_PaintsBackgroundPaper_*`  
125:- **Status:** **Stub** (MVP FE3/Ultimem/MegaCart); full VICE cart suite **Missing**. No Exact claim.
159:| READY first_x window | `video-viewport.c` | `ComputeViewportFirstX` + crop |

F:\GitHub\vice-sharp\docs\audit-vicii-vs-vice-2026-07-06.md
194:### 2.C Cycle state machine, border flip-flops, sprite DMA/expansion (NTSC timing + mid-line writes)
206:- **Impact:** On 6567R8/8562/6572 sprite DMA turns on one cycle late every line; because `check_exp` already ran at RasterX 55, the Y-expansion cadence on the turn-on line desyncs (mistimed double-height sprites) and BA stealing shifts. PAL unaffected.
245:- **Impact:** Any sprite whose DMA/display window straddles the raster wrap (low-Y sprites re-enabled on lines 256-311, common in lower-border sprite work and multiplexers) is force-stopped at line 0 instead of continuing until mcbase==63; the first line(s) drop or expansion cadence resets. Boot display (no sprites) unaffected.
300:- **Root cause:** The $D017 write was reduced to a pure expansion-flag decode; the store-time crunch/exp_flop interaction with running fetch state was not ported.
301:- **Impact:** The vertical sprite-crunch / sprite-stretch trick renders incorrectly, and turning off Y-expansion does not immediately arm `exp_flop`, so the following line's toggle can be one line off. Affects demos/games using sprite crunch; not boot.

F:\GitHub\vice-sharp\docs\Iteration-Roadmap.md
57:- 5KB base RAM + expansion packs (3K/8K/16K/24K/32K)
58:- MVP cartridge map (BLK PRG/raw) and READY boot proof with official ROMs
68:**Exit criteria:** Runs VIC-20 software, architecture switching works at runtime, and multi-second every-cycle register lockstep vs native xvic. Met for READY fingerprint, character frames, session factory, focused `FullyQualifiedName~Vic20` gates, and the 10 s PAL + NTSC diverge probes. Tier-A polish (expansion UX, input E2E, cart/disk depth, snapshots) remains open; see `HANDOFF.md`.

F:\GitHub\vice-sharp\docs\plans\perf-sprite-dma-optimization-plan-001.md
35:- vicii-sprite.c: (BA mask composition with badlines, expansion, DMA height multi-line after latch).
90:- Risk: Cache divergence on rare FLI + multi-sprite + expansion + oldNTSC edge. Mitigation: the new test explicitly covers FLI compose + all models + the exact scenarios from existing VicIISpriteDmaStallTests.

F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20260808T094911Z.md
10:1. Mos6561 normal READY present geometry matches VICE `video_viewport` `first_x` (READY PAL: first_x=48, left border [0..47], paper origin 48, right border [400..447]); proven by tests driving shipped `FrameBuffer` after `RenderNow`.
26:  `first_x = gfx_x - (canvas_w - gfx_w) / 2` when `gfx_w + small_x_border*2 > width`.
27:- READY PAL inputs: gfx_x=96, gfx_w=352, canvas=448, screen=568 → right=120, small=96, 352+192=544>448 → first_x=96-48=**48**.
29:- Crop present path uses that first_x on the shipped line buffer (lines 663-671).
36:**Caveat (does not fail claim as stated):** pixel framebuffer lockstep vs xvic remains Missing per audit; claim is geometry bands / first_x, not full FB parity.
57:- VICE host uses `viewport->first_x` as draw-buffer X origin in `video-canvas.c` (`video_canvas_refresh_all`).
91:- Exact labels scoped to named rules (color RAM store/read/peek rules, READY first_x window, normal geometry constants, etc.).

F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20260808T103211Z.md
13:Independent re-check of implementer claims for FlashImageBuilder / FlashCartProfiles / portable VM / Avalonia+UWP entry points / FE3 presets / attach path. Default each claim FAIL/UNKNOWN until tool evidence.
17:### Claim 1: FlashImageBuilder + FlashCartProfiles are UI-free Core types; FE3 size Exact FinalExpansion3Cartridge.FlashSize (0x80000); Ultimem default Exact UltimemCartridge.DefaultImageSize; Mega-Cart ROM Exact MegaCartCartridge.LowRomSize and NVRAM SecondarySizeBytes Exact NvramSize
23:- Types live under `src/ViceSharp.Core/FlashCarts/` (`FlashImageBuilder.cs`, `FlashCartProfiles.cs`).
24:- `FlashCartProfiles.Fe3` uses `FinalExpansion3Cartridge.FlashSize`; Ultimem uses `UltimemCartridge.DefaultImageSize`; MegaCart uses `MegaCartCartridge.LowRomSize` and `secondarySizeBytes: MegaCartCartridge.NvramSize` (`FlashCartProfiles.cs` lines 11-35).
25:- Constants: `FinalExpansion3Cartridge.FlashSize = 0x80000`; `UltimemCartridge.DefaultImageSize = 0x100000`; `MegaCartCartridge.LowRomSize = 0x100000`; `MegaCartCartridge.NvramSize = 0x2000`.
35:- `FlashCartProfiles.EasyFlash` id `easyflash`, display "EasyFlash (C64 layout, 8K banks)", size `Bank8K * 128`, bank size `Bank8K` (`0x2000`), secondary 0; comment says structure for reuse / UI out of band (`FlashCartProfiles.cs` lines 37-47).
36:- Included in `FlashCartProfiles.All`.
38:- Primary VM list hides EasyFlash (`Profiles` filters fe3/ultimem/megacart only); `AllProfiles` still exposes it (`FlashCartImageBuilderViewModel.cs`).
</workspace_result>

### Turn 44 (Function)
[tool_response]
exit: 0
docs/audit-vic20-vs-vice-2026-08-07.md
docs/receipts-lockstep-everycycle-2s-after-rts-2026-08-06.log
docs/receipts-lockstep-everycycle-2s-after-zpinc-2026-08-06.log
docs/receipts-lockstep-everycycle-2s-attempt-2026-08-06.log
docs/receipts-lockstep-everycycle-after-rts-2026-08-06.log
docs/receipts-lockstep-everycycle-baseline-2026-08-06.log
docs/receipts-lockstep-everycycle-baseline-2026-08-06.txt
docs/receipts-lockstep-everycycle-gates-2026-08-06.log
docs/receipts-lockstep-everycycle-zpinc-fix-2026-08-06.log
docs/receipts-lockstep-focused-after-rts-phase-2026-08-06.log
docs/receipts-lockstep-focused-bcc-2026-08-06.log
docs/receipts-lockstep-focused-cmp-2026-08-06.log
docs/receipts-lockstep-focused-imm-2026-08-06.log
docs/receipts-lockstep-focused-jsr-2026-08-06.log
docs/receipts-lockstep-focused-ldaabs-2026-08-06.log
docs/receipts-lockstep-focused-ldax-2026-08-06.log
docs/receipts-lockstep-focused-ldx-2026-08-06.log
docs/receipts-lockstep-focused-ldx2-2026-08-06.log
docs/receipts-lockstep-focused-nop-2026-08-06.log
docs/receipts-lockstep-focused-nopchain-2026-08-06.log
docs/receipts-lockstep-focused-plp-2026-08-06.log
docs/receipts-lockstep-focused-plp-b-2026-08-06.log
docs/receipts-lockstep-focused-rts-overlap-2026-08-06.log
docs/receipts-lockstep-focused-rts-v2-2026-08-06.log
docs/receipts-lockstep-focused-rts-v3-2026-08-06.log
docs/receipts-lockstep-focused-rts-v4-2026-08-06.log
docs/receipts-lockstep-focused-sty-2026-08-06.log
docs/receipts-lockstep-focused-zpinc-only-2026-08-06.log
docs/receipts-vic20-ntsc-apply-stackoverflow-2026-08-07.md
docs/receipts-vic20-ram-all-stackoverflow-2026-08-06.md
docs/receipts-vic20-video-40x40-2026-08-07.md
docs/receipts/hostile-validator-20260808T094911Z.json
docs/receipts/hostile-validator-20260808T094911Z.md
docs/receipts/hostile-validator-20260808T103211Z.json
docs/receipts/hostile-validator-20260808T103211Z.md
docs/reviews/code-review-20260714T001056552Z.md
docs/reviews/code-review-20260714T004256563Z.md
docs/romless-vic-badline-fix.md
docs/rti-focused-2026-08-06.log
docs/rti2-focused-2026-08-06.log
docs/rtsand-focused-2026-08-06.log
docs/rtsfl-focused-2026-08-06.log
docs/sec-focused-2026-08-06.log
docs/tay-focused-2026-08-06.log
docs/via1-iec-focused-2026-08-06.log
docs/vic20-cpu-plus-video-lockstep-focused-2026-08-08.log
docs/vic20-present-path-fix-2026-08-08.log
docs/vic20-ready-paper-fix-2026-08-08.log
docs/vic20-video-lockstep-2026-08-08.log
docs/vic20-video-lockstep-2k-2026-08-08.log
docs/xvic-err.txt
docs/xvic-err2.txt
docs/xvic-out.txt
docs/xvic-out2.txt
docs/xvic-ready-nofilter-2026-08-08.png
docs/xvic-ready-pal-2026-08-08.png
docs/xvic-ready-pal-2026-08-08b.png
docs/xvic-ready-user-2026-08-08.png
docs/xvic-shot.ini
manifests/s/sharpninja/ViceSharp/1.2.1/sharpninja.ViceSharp.installer.yaml
manifests/s/sharpninja/ViceSharp/1.2.1/sharpninja.ViceSharp.locale.en-US.yaml
manifests/s/sharpninja/ViceSharp/1.2.1/sharpninja.ViceSharp.yaml
src/ViceSharp.Avalonia/Views/FlashCartBuilderView.axaml
src/ViceSharp.Avalonia/Views/FlashCartBuilderView.axaml.cs
src/ViceSharp.Core/FlashCarts/FlashBankContentKind.cs
src/ViceSharp.Core/FlashCarts/FlashBankSlot.cs
src/ViceSharp.Core/FlashCarts/FlashCartImageBuilderViewModel.cs
src/ViceSharp.Core/FlashCarts/FlashCartProfiles.cs
src/ViceSharp.Core/FlashCarts/FlashImageBuilder.cs
src/ViceSharp.Core/FlashCarts/IFlashBuilderFileIo.cs
src/ViceSharp.Core/FlashCarts/IFlashCartProfile.cs
src/ViceSharp.Core/Iec/FileSystemIecDevice.cs
src/ViceSharp.Core/Vic20/FinalExpansion3Cartridge.cs
src/ViceSharp.Core/Vic20/IVic20ExpansionCartPort.cs
src/ViceSharp.Core/Vic20/IVic20RamConfiguration.cs
src/ViceSharp.Core/Vic20/MegaCartCartridge.cs
src/ViceSharp.Core/Vic20/UltimemCartridge.cs
src/ViceSharp.Core/Vic20/Vic20ExpansionCartKind.cs
src/ViceSharp.Core/Vic20/Vic20ExpansionCartPort.cs
src/ViceSharp.Protocol/ExpansionCartManageState.cs
src/ViceSharp.Protocol/Vic20MemoryUiState.cs
src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml
src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml.cs
tests/ViceSharp.TestHarness/FlashCart/Fe3PresetTests.cs
tests/ViceSharp.TestHarness/FlashCart/FlashCartImageBuilderViewModelTests.cs
tests/ViceSharp.TestHarness/FlashCart/FlashImageBuilderTests.cs
tests/ViceSharp.TestHarness/IEC/FileSystemIecDeviceTests.cs
tests/ViceSharp.TestHarness/IEC/FileSystemIecUnitSettingsTests.cs
tests/ViceSharp.TestHarness/Ui/Vic20SettingsUiSurfaceTests.cs
tests/ViceSharp.TestHarness/Vic20/MediaServiceHostVic20ExpansionTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20BoardWiringTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20ExpansionCartTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20MemorySettingsRestartTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20RamBlocksTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20SettingsRestartPreservesExpansionTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20VideoLockstep.cs
tests/ViceSharp.TestHarness/Vic20/Vic20VideoPumpSmokeTests.cs
---COUNT ALL UNTRACKED---
363
---SRC UNTRACKED---
src/ViceSharp.Avalonia/Views/FlashCartBuilderView.axaml
src/ViceSharp.Avalonia/Views/FlashCartBuilderView.axaml.cs
src/ViceSharp.Core/FlashCarts/FlashBankContentKind.cs
src/ViceSharp.Core/FlashCarts/FlashBankSlot.cs
src/ViceSharp.Core/FlashCarts/FlashCartImageBuilderViewModel.cs
src/ViceSharp.Core/FlashCarts/FlashCartProfiles.cs
src/ViceSharp.Core/FlashCarts/FlashImageBuilder.cs
src/ViceSharp.Core/FlashCarts/IFlashBuilderFileIo.cs
src/ViceSharp.Core/FlashCarts/IFlashCartProfile.cs
src/ViceSharp.Core/Iec/FileSystemIecDevice.cs
src/ViceSharp.Core/Vic20/FinalExpansion3Cartridge.cs
src/ViceSharp.Core/Vic20/IVic20ExpansionCartPort.cs
src/ViceSharp.Core/Vic20/IVic20RamConfiguration.cs
src/ViceSharp.Core/Vic20/MegaCartCartridge.cs
src/ViceSharp.Core/Vic20/UltimemCartridge.cs
src/ViceSharp.Core/Vic20/Vic20ExpansionCartKind.cs
src/ViceSharp.Core/Vic20/Vic20ExpansionCartPort.cs
src/ViceSharp.Protocol/ExpansionCartManageState.cs
src/ViceSharp.Protocol/Vic20MemoryUiState.cs
src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml
src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml.cs
tests/ViceSharp.TestHarness/FlashCart/Fe3PresetTests.cs
tests/ViceSharp.TestHarness/FlashCart/FlashCartImageBuilderViewModelTests.cs
tests/ViceSharp.TestHarness/FlashCart/FlashImageBuilderTests.cs
tests/ViceSharp.TestHarness/IEC/FileSystemIecDeviceTests.cs
tests/ViceSharp.TestHarness/IEC/FileSystemIecUnitSettingsTests.cs
tests/ViceSharp.TestHarness/Ui/Vic20SettingsUiSurfaceTests.cs
tests/ViceSharp.TestHarness/Vic20/MediaServiceHostVic20ExpansionTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20BoardWiringTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20ExpansionCartTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20MemorySettingsRestartTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20RamBlocksTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20SettingsRestartPreservesExpansionTests.cs
tests/ViceSharp.TestHarness/Vic20/Vic20VideoLockstep.cs
tests/ViceSharp.TestHarness/Vic20/Vic20VideoPumpSmokeTests.cs


### Turn 45 (Function)
[tool_response]
exit: 0

GitVersion.yml:3:# next-version pins the major.minor base so the build field auto-increments with
GitVersion.yml:6:next-version: 1.2.2
Directory.Build.props:4:    <LangVersion>13</LangVersion>
Directory.Build.props:10:    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>

FullName : F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\Views\FlashCartBuilderView.axaml


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\Views\FlashCartBuilderView.axaml.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\FlashBankContentKind.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\FlashBankSlot.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\FlashCartImageBuilderViewModel.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\FlashImageBuilder.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\IFlashBuilderFileIo.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Core\FlashCarts\IFlashCartProfile.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\x64\Release-UWP\net10.0-windows10.0.26100.0\win-x64\Views\FlashC
           artBuilderPage.xbf


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\obj\x64\Release-UWP\net10.0-windows10.0.26100.0\win-x64\embed\Views\
           FlashCartBuilderPage.xbf


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\obj\x64\Release-UWP\net10.0-windows10.0.26100.0\win-x64\Views\FlashC
           artBuilderPage.g.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\obj\x64\Release-UWP\net10.0-windows10.0.26100.0\win-x64\Views\FlashC
           artBuilderPage.g.i.cs


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\obj\x64\Release-UWP\net10.0-windows10.0.26100.0\win-x64\Views\FlashC
           artBuilderPage.xaml


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\obj\x64\Release-UWP\net10.0-windows10.0.26100.0\win-x64\Views\FlashC
           artBuilderPage.xbf


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\FlashCartBuilderPage.xaml


FullName : F:\GitHub\vice-sharp\src\ViceSharp.Xbox\Views\FlashCartBuilderPage.xaml.cs


FullName : F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\FlashCart


FullName : F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\FlashCart\FlashCartImageBuilderViewModelTests.cs


FullName : F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\FlashCart\FlashImageBuilderTests.cs


Name          : hostile-validator-20260808T094911Z.json
LastWriteTime : 8/8/2026 4:51:48 AM


Name          : hostile-validator-20260808T094911Z.md
LastWriteTime : 8/8/2026 4:51:48 AM


Name          : hostile-validator-20260808T103211Z.json
LastWriteTime : 8/8/2026 5:32:53 AM


Name          : hostile-validator-20260808T103211Z.md
LastWriteTime : 8/8/2026 5:32:53 AM


Name          : store-phase0-20260805.md
LastWriteTime : 8/5/2026 2:45:35 PM




### Turn 46 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Status -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' 2>&1

- description: Invoke Grok MCP plugin Status for trust
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\USER-GUIDE.md
- limit: 80
[tool_request: grep]
- pattern: expansion|cartridge|VIC-20|flash|BLK|Settings
- path: F:\GitHub\vice-sharp\docs\USER-GUIDE.md
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\README.md
- limit: 100
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# MSI version from last deploy log if present
Select-String -Path 'docs/_deploy-desktop*.log','docs/_deploy-last-pair.txt' -Pattern '1\.2\.|version|InstallMsi' -ErrorAction SilentlyContinue | Select-Object -Last 15
# GitVersion
Get-Content GitVersion.yml | Select-Object -First 20

- description: Check deployed version references

### Turn 47 (Function)
[tool_response]
1→# ViceSharp Documentation

## User guides

- [USER-GUIDE.md](USER-GUIDE.md) - install, first run, CLI launcher, machine YAML, disk images, capture, diagnostics attach, what works today
- [VICE-MIGRATION.md](VICE-MIGRATION.md) - swap classic VICE for ViceSharp: binary + flag mapping, behaviour caveats, bug compatibility
- [ROMs.md](ROMs.md) - legal ROM options, environment variable, expected directory layout
- [PRIVACY.md](PRIVACY.md) - privacy policy (store listing source)

10→## Architecture and Design

- [Architecture.md](Architecture.md) - POCO model, MVVM, mutation queue, device model, architectures
- [Public-API.md](Public-API.md) - 33+ public interfaces with XML doc summaries
- [StateWindow.md](StateWindow.md) - Configuration surface, presets, memory math
- [Decoupling.md](Decoupling.md) - Video and audio decoupling, refresh strategies
- [PubSub.md](PubSub.md) - Pool, arena, handle lifecycle, refcount, hot-path cost

## Reference

20→- [ROMs.md](ROMs.md) - Legal ROM options, environment variable setup
- [ROL.md](ROL.md) - Registry of Lore (122+ entries)
- [AI-Review.md](AI-Review.md) - aiUnit AI Code Review + Project Review tests, and running them with the Grok Build CLI
- [wiki.yaml](wiki.yaml) - wiki export manifest (`mcp-wiki-export/v1`)

## Iteration Plans

- [Iteration-00-Foundations.md](Iteration-00-Foundations.md) - Scaffolding, interfaces, source generator, CI/CD
- [Iteration-Roadmap.md](Iteration-Roadmap.md) - Full roadmap through MVP (Iteration 2 VIC-20 includes 10 s PAL/NTSC every-cycle lockstep)

30→## Lockstep receipts (durable)

- [receipts-lockstep-10s-2026-08-06.txt](receipts-lockstep-10s-2026-08-06.txt) - VIC-20 PAL 10 s every-cycle A/X/Y/S/P/PC vs xvic
- [receipts-lockstep-10s-ntsc-2026-08-06.txt](receipts-lockstep-10s-ntsc-2026-08-06.txt) - VIC-20 NTSC 10 s (soft-BIT / VIA bus-visible realign)
- [receipts-lockstep-vice-realign-2026-08-06.txt](receipts-lockstep-vice-realign-2026-08-06.txt) - VICE realign notes (V-bus, color RAM, order)

Ephemeral intermediate probe logs (`docs/*-focused-*.log`, `docs/2s-*.log` from debug slices) are not wiki content; prefer durable receipts above.

## Continuity

40→- [HANDOFF.md](../HANDOFF.md) (repo root) - canonical session continuity; do not use retired `docs/handoff.md`

## Session Logs

MCP session logs live in the MCP Server store (not on-disk under this tree for active work). Historical files under `session-logs/` if present are archival only.


### Turn 48 (Function)
[tool_response]
1→# ViceSharp User Guide

This guide walks a new user from a fresh clone to a running C64 with a true-drive 1541 attached. If you are coming from classic VICE and want a quick translation of your usual `x64sc` / `c1541` invocations, see [VICE-MIGRATION.md](VICE-MIGRATION.md).

## 1. Install

### Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (10.0.201 or later). Verify with `dotnet --version`.
10→- **Git**.
- **Native VICE shim for Windows x64**. Download `vice-sharp-native-win-x64.zip` from [GitHub Releases](https://github.com/sharpninja/vice-sharp/releases), extract it at the repository root, and confirm `native/vice_x64.dll`, `native/libiconv-2.dll`, and `native/zlib1.dll` exist. Alternatively install **MSYS2 + mingw-w64 gcc** and let the build recreate the shim from source.

### Clone and build

```pwsh
git clone https://github.com/sharpninja/vice-sharp.git vice-sharp
cd vice-sharp
dotnet build ViceSharp.slnx
```
20→
The build is `TreatWarningsAsErrors`; a clean checkout on a supported SDK builds with zero warnings. If you see SDK-version errors, re-check `dotnet --version`. If you do not install the release bundle first, Windows builds that touch native VICE will invoke `native/build-vice-shim.ps1` and require MSYS2 at `C:\msys64`.

### First-run verification

Run the supported test gate (the same filter the Nuke `Test` target uses):

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"
```
30→
`./build.ps1 Test` runs the same gate. At the v1.0.2 release the baseline is Failed 0 / Passed 2594 / Skipped 21 / Total 2615 (single process). A failure here means your local environment is off (typically a missing native VICE bundle or a wrong .NET SDK) rather than a real regression.

Avoid an unfiltered `dotnet test ViceSharp.slnx`: it pulls in opt-in categories (determinism replays, AI reviews, quarantined parity suites), can exceed 5 minutes, and has hung historically.

## 2. Get ROMs

ViceSharp does not ship Commodore ROM images. Use the data root from an installed VICE package or another legal source. The full sourcing rules are in [ROMs.md](ROMs.md); the short version:

1. Set `VICESHARP_ROM_PATH` to your VICE data root, or put `x64sc.exe` on `PATH`:
40→   ```pwsh
   $env:VICESHARP_ROM_PATH = "C:\path\to\GTK3VICE-3.8-win64"
   ```
2. Use the native VICE layout, for example `C64/basic-901226-01.bin`, `C64/kernal-901227-03.bin`, `C64/chargen-901225-01.bin`, and `DRIVES/dos1541-325302-01+901229-05.bin`. See [ROMs.md](ROMs.md#rom-directory-structure) for the full tree.
3. The layout is checked at machine build time: known ROMs are validated by MD5 and a mismatch fails the load. See [ROMs.md](ROMs.md#rom-validation) for the known checksums.

If you have classic VICE installed, the preferred value is the VICE data root that contains `C64/` and `DRIVES/`; pointing directly at `C64/` is normalized to the parent root. If not, the `ViceSharp.RomFetch` library can resolve and download ROMs from a user-supplied source (a standalone CLI is planned but does not ship yet); see [ROMs.md](ROMs.md#vicesharpromfetch-tool) for what it provides today.

## 3. Run your first machine

50→ViceSharp's reference shell is the `ViceSharp.Console` project. Run the canonical C64 + 1541 topology shipped in `docs/samples/`:

```pwsh
dotnet run --project src/ViceSharp.Console -- `
    --roms $env:VICESHARP_ROM_PATH `
    --machine-yaml docs/samples/c64-plus-1541.multisystem.yaml `
    --cycles 1000000
```

Flag-by-flag:
60→
| Flag | Meaning |
|------|---------|
| `--roms <dir>` | VICE data root. Defaults to automatic resolver behavior when omitted by runtime paths that use `ViceDataPathResolver`. |
| `--machine-yaml <path>` | Multi-system YAML topology to load (host + peripherals + buses). |
| `-m <path>` | Short form of `--machine-yaml`. |
| `--cycles <N>` | Host-cycle budget for this run. Defaults to 100,000. |
| `--trace <path>` | Append a deterministic instruction trace. |

You can also boot a default C64 host (no peripherals, no YAML) by omitting `--machine-yaml`:
70→
```pwsh
dotnet run --project src/ViceSharp.Console -- --roms $env:VICESHARP_ROM_PATH --cycles 100000
```

## 4. CLI launcher (VICE-compatible flags)

`ViceSharp.Launcher` is a class library, not a set of executables: it supplies VICE-compatible argument parsing ([ViceArgsParser.cs](../src/ViceSharp.Launcher/ViceArgsParser.cs)) and binary-name topology dispatch ([ViceTopologyBuilder.cs](../src/ViceSharp.Launcher/ViceTopologyBuilder.cs)). VICE-named binaries (`x64.exe`, `x64sc.exe`, `c1541.exe`) are not yet shipped. Today the consumer is `ViceSharp.Console`, which parses its arguments as binary name `x64sc`, so you invoke the launcher flags through the console shell:

```pwsh
80→dotnet run --project src/ViceSharp.Console -- <flags>

### Turn 49 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 8 matching lines
F:\GitHub\vice-sharp\docs\USER-GUIDE.md
89:| `-cart <path>` | Attach a cartridge image. |
94:| `-debugcart` / `+debugcart` | Enable / disable the debug cartridge ($D7FF exit signaling for regression harnesses). Note the VICE polarity: `-debugcart` enables, `+debugcart` disables. |
101:The console entry point currently consumes `--help`, `-debugcart` / `+debugcart`, `--limitcycles`, and `-autostart` / positional `.prg` from the parsed set (plus its own `--roms`, `--machine-yaml`, `--cycles`, and `--trace` flags from section 3). Disk, cartridge, and true-drive dispatch from parsed flags lives in the launcher library (`ViceTopologyBuilder`) and is not yet wired into the console entry point; use `--machine-yaml` for drive topologies today.
112:Example: VICE testbench style "run a PRG with the debug cartridge and a bounded cycle budget":
185:| Standard 8K / 16K cartridge mapping (raw + CRT) | Working | Image normalises to ROML/ROMH and drives the live C64 memory map with GAME/EXROM behaviour; broader mapper families are post-MVP. |
192:| C128 / VIC-20 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException`. |
199:- **Warp mode** runs the emulator uncapped, same semantics as VICE `-warp`; live sound is discarded while warp is on. Toggle it with **Alt+W**, the System menu's **Warp mode** item, or the Warp toggle in the Settings **Limiter** section.
200:- **Limiter slider** (Settings > Limiter) sets a live target speed percentage; the emulator paces to the slider target. Past 200% live sound is suspended.
</workspace_result>

### Turn 50 (Function)
[tool_response]
exit: 0

docs\_deploy-desktop-2026-08-08_032741.log:27:03:27:54 [INF] > "C:\Program Files\dotnet\dotnet.exe" publish 
F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\ViceSharp.Avalonia.csproj --configuration Release --runtime win-x64 
--property:Version=1.2.7 --property:ILLinkTreatWarningsAsErrors=false --property:DebugType=none 
--property:DebugSymbols=false --property:GenerateDocumentationFile=false 
--property:CopyDocumentationFilesFromPackages=false --property:PublishAot=false --property:PublishTrimmed=true 
--property:TrimMode=partial --property:PublishReadyToRun=true --property:PublishSingleFile=true 
--property:IncludeNativeLibrariesForSelfExtract=true --self-contained true
docs\_deploy-desktop-2026-08-08_032741.log:165:03:29:01 [INF] > "C:\Program Files\dotnet\dotnet.exe" build 
F:\GitHub\vice-sharp\installer\ViceSharp.Installer.wixproj --configuration Release --property:ProductVersion=1.2.7 
--property:PublishedRoot=F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\win-x64\publish 
--property:EnableDebugGrpc=false
docs\_deploy-desktop-2026-08-08_032741.log:178:║ InstallMsi
docs\_deploy-desktop-2026-08-08_032741.log:182:03:29:52 [INF] InstallMsi complete (exit 0)
docs\_deploy-desktop-2026-08-08_032741.log:210:InstallMsi         Succeeded       0:03
docs\_deploy-desktop-2026-08-08_045445.log:9:NUKE Execution Engine version 10.1.0 (Windows,.NETCoreApp,Version=v10.0)
docs\_deploy-desktop-2026-08-08_045445.log:55:InstallMsi         NotRun              
docs\_deploy-desktop-2026-08-08_045501.log:9:NUKE Execution Engine version 10.1.0 (Windows,.NETCoreApp,Version=v10.0)
docs\_deploy-desktop-2026-08-08_045501.log:25:04:55:12 [INF] > C:\Users\kingd\.dotnet\tools\dotnet-gitversion.exe 
/targetpath "F:\GitHub\vice-sharp" /output json
docs\_deploy-desktop-2026-08-08_045501.log:26:04:55:13 [INF] MSI version from GitVersion: 1.2.7
docs\_deploy-desktop-2026-08-08_045501.log:27:04:55:13 [INF] > "C:\Program Files\dotnet\dotnet.exe" publish 
F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\ViceSharp.Avalonia.csproj --configuration Release --runtime win-x64 
--property:Version=1.2.7 --property:ILLinkTreatWarningsAsErrors=false --property:DebugType=none 
--property:DebugSymbols=false --property:GenerateDocumentationFile=false 
--property:CopyDocumentationFilesFromPackages=false --property:PublishAot=false --property:PublishTrimmed=true 
--property:TrimMode=partial --property:PublishReadyToRun=true --property:PublishSingleFile=true 
--property:IncludeNativeLibrariesForSelfExtract=true --self-contained true
docs\_deploy-desktop-2026-08-08_045501.log:165:04:56:42 [INF] > "C:\Program Files\dotnet\dotnet.exe" build 
F:\GitHub\vice-sharp\installer\ViceSharp.Installer.wixproj --configuration Release --property:ProductVersion=1.2.7 
--property:PublishedRoot=F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\win-x64\publish 
--property:EnableDebugGrpc=false
docs\_deploy-desktop-2026-08-08_045501.log:178:║ InstallMsi
docs\_deploy-desktop-2026-08-08_045501.log:182:04:58:36 [INF] InstallMsi complete (exit 0)
docs\_deploy-desktop-2026-08-08_045501.log:210:InstallMsi         Succeeded       0:03
# GitVersion configuration. The MSI/winget ProductVersion is derived in
# build/Build.cs as {Major}.{Minor}.{CommitsSinceVersionSource}, e.g. 0.1.356.
# next-version pins the major.minor base so the build field auto-increments with
# history. Windows Installer compares major.minor.build for upgrades; the WiX
# MajorUpgrade (AllowSameVersionUpgrades) makes every redeploy reinstall.
next-version: 1.2.2



### Turn 51 (Function)
[tool_response]
exit: 0
{"status":"available","agent":"GrokCode","namespaces":["workflow.sessionlog","workflow.todo","workflow.requirements","workflow.triage","workflow.graphrag","workflow.memory","workflow.failsafe"],"requirementMethods":["workflow.requirements.listLayers","workflow.requirements.createLayer","workflow.requirements.updateLayer","workflow.requirements.effective"],"requirementClientMethods":["client.Requirements.ListRequirementLayersAsync","client.Requirements.CreateRequirementLayerAsync","client.Requirements.UpdateRequirementLayerAsync","client.Requirements.GetEffectiveRequirementsAsync"],"cacheDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok","hasSession":true,"hasCurrentTurn":true,"pendingCount":0,"pendingTurnCount":0,"failsafeDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\failsafe","failsafeCount":0,"failsafeQuarantineCount":0}


### Turn 52 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
# Show full status with trust fields if available via verbose
Get-Content 'F:\GitHub\vice-sharp\.mcpServer\grok\session-state.yaml' -ErrorAction SilentlyContinue
Write-Host '---TURN---'
Get-Content 'F:\GitHub\vice-sharp\.mcpServer\grok\current-turn.yaml' -ErrorAction SilentlyContinue
Write-Host '---BOOTSTRAP---'
$ts = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1' -Method 'workflow.sessionlog.bootstrap' -ParamsYaml '{}' -WorkspacePath 'F:\GitHub\vice-sharp' 2>&1

- description: Read Grok session cache and bootstrap
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\USER-GUIDE.md
- offset: 170
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\Iteration-Roadmap.md
- offset: 40
- limit: 50
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\audit-vic20-vs-vice-2026-08-07.md
- limit: 80

### Turn 53 (Function)
[tool_response]
40→- Monitor/debugger - disassembly, breakpoints, memory view, watch
- Save/load state snapshots
- Avalonia desktop UI

**Exit criteria:** Loads and runs commercial C64 software, passes Klaus Dormann test suite, SID audio plays correctly, save/load state round-trips deterministically.

## Iteration 2: VIC-20

**Goal:** Add VIC-20 as a second architecture.

50→**Status (2026-08-06):** Core complete on `main` (merged from `feat/iteration2-vic20`). Every-cycle native lockstep green for multi-second PAL and NTSC.

- MOS 6502 CPU (reuse 6510 core minus I/O port)
- VIC-I (6560/6561) character-mode video (`Mos6561`) with PAL/NTSC timing
- VIA x2 (shared `Via6522`) at $9110 NMI / $9120 IRQ; keyboard matrix + joystick glue
- VIA timer CPU `Read` uses pre-Tick bus-visible counters (VICE LOAD at `maincpu_clk` before `CLK_INC`); peeks stay post-Tick
- Color RAM 4-bit + V-bus open-bus high nibble (`Vic20ColorRam` / `BasicBus`)
- 5KB base RAM + expansion packs (3K/8K/16K/24K/32K)
- MVP cartridge map (BLK PRG/raw) and READY boot proof with official ROMs
- Default drive unit 8 = **1540** (`DriveModel.C1540`); C64 remains 1541
60→- Launcher `xvic` topology; host session create; Xbox computer picker enables VIC-20
- Native oracle: `native/vice_xvic.dll` via `ViceNative.CreateInstance("vic20"|"vic20ntsc")`
- Every-cycle A/X/Y/S/P/PC lockstep vs xvic:
  - **PAL 10 s:** 11_084_050 cycles (`EveryCycle_CpuRegs_Match_TenSecondPal`)
  - **NTSC 10 s:** 10_227_270 cycles (`EveryCycle_CpuRegs_Match_TenSecondNtsc`)
  - Env: `VICESHARP_LOCKSTEP_10S=1` (and `VICESHARP_LOCKSTEP_2S=1` for 2 s PAL)
  - Receipts: `docs/receipts-lockstep-10s-2026-08-06.txt`, `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`

**Exit criteria:** Runs VIC-20 software, architecture switching works at runtime, and multi-second every-cycle register lockstep vs native xvic. Met for READY fingerprint, character frames, session factory, focused `FullyQualifiedName~Vic20` gates, and the 10 s PAL + NTSC diverge probes. Tier-A polish (expansion UX, input E2E, cart/disk depth, snapshots) remains open; see `HANDOFF.md`.

70→## Iteration 3: C128

**Goal:** Add Commodore 128 with dual-CPU support.

- MOS 8502 CPU (2 MHz mode)
- Z80 coprocessor (CP/M mode)
- VIC-IIe (6569 superset with 2 MHz support)
- VDC (8563) - 80-column display
- MMU - extended banking, shared RAM
- 1571/1581 drive support
80→- C128/C64 mode switching

**Exit criteria:** Boots in C128 mode, switches to C64 mode, CP/M mode starts.

## Iteration 4: PET

**Goal:** Add Commodore PET family.

- MOS 6502 CPU
- PIA (6520) and VIA (6522) I/O

### Turn 54 (Function)
[tool_response]
1→# VIC-20 vs VICE (xvic) Full Implementation Audit

**Date:** 2026-08-08 (re-baselined)  
**Canonical tree:** `native/vice/vice/src/vic20/` (+ `core/viacore.c`, `data/VIC20/`)  
**Managed tree:** `src/ViceSharp.Chips/{Vic,Cpu,IEC}`, `src/ViceSharp.Core/Vic20`, `ArchitectureBuilder.BuildVic20Machine`  

**Standard of truth:** VICE source logic only. Screenshots and "looks close" are not acceptance.  
**Statuses:** Exact | Partial | Stub | Missing | Invented  

10→**Process note:** All VIC-20 / VICE-faithfulness work uses a **hostile validator** (independent read-only agent; assumes overclaim; Exact requires VICE file+function + matching managed control flow).

**Video lockstep:** Native `vice_vic20_get_video_state` + managed `CaptureVideoLockstepState`. Tests: `Vic20VideoLockstep` (2k + 500k). CPU probe also compares video unless `VICESHARP_LOCKSTEP_VIDEO=0`. Pixel framebuffer compare vs xvic still **Missing** (`capture_visible_frame` stub).

---

## Hostile claim scope (what may be Exact)

The tree does **not** claim whole-machine Exact-same-as-VICE. Exact is allowed only for the rules/subsystems listed in the matrix below as **Exact**. Everything else is Partial, Stub, or Explicit Missing.

20→---

## Chip-by-chip matrix

### CPU

- **VICE:** `vic20cpu.c` `CLK_INC` + maincpu  
- **Managed:** `Mos6502` + `SystemClock` Phi2 (CPU before VIC)  
- **Status:** **Partial**  
- **Why not Exact:** Shared 6502; lockstep-hardened slices only.
30→
### Bus / open-bus

- **VICE:** `vic20bus.c`, `vic20memrom.c`  
- **Managed:** `BasicBus` last-data + V-bus latch  
- **Status:** **Partial** (selected rules Exact below)  
- **Exact rules:** BASIC/KERNAL ROM reads do not refresh last-data; chargen does.  
- **Not Exact:** Full C-bus / V-bus matrix for all unconnected regions.

### Memory map
40→
- **VICE:** `vic20mem.c`, `vic20io.c`  
- **Managed:** `Vic20SystemRam` + `RomDevice` + overlays  
- **Status:** **Partial**  
- **Missing:** Full `mem_conf`, VFLI, complete cart banking matrix.

### Color RAM

- **VICE:** `colorram_read` / `colorram_store` / `colorram_peek` in `vic20mem.c`  
- **Managed:** `Vic20ColorRam.cs`  
50→- **Status:** **Exact** for those three rules (4-bit store; read OR `VBusLastData & 0xF0`; peek no side effect)  
- **Missing:** VFLI color banking path.

### VIC-I cycle engine

- **VICE:** `vic-cycle.c`  
- **Managed:** `Mos6561.Tick`  
- **Status:** **Partial**  
- **Exact-ish / present:** open_v/h, START delay, matrix/chargen into cbuf/gbuf, memptr/row, `LatchColumns` pending-only (live `text_cols` only on open_h/start_fetch; test `LatchColumns_DoesNotEagerlySetLiveTextCols`), `display_xstop` from start_fetch.  
- **Missing / diverge:** Interlace, light pen, full `vic_cycle_do_fetch` map (VFLI/unconnected), idle `no_fetch` bus refresh.
60→
### VIC-I draw + present

- **VICE:** `vic-draw.c` + full line buffer; `raster_line_draw_borders`; host `first_x` via `video_viewport_resize` / `video_canvas_refresh`  
- **Managed:** full-line buffer; glyph paint all slots (no invent paper strip); border blank left of `display_xstart` / right of `display_xstop`; crop `ComputeViewportFirstX`  
- **Status:**  
  - **Exact** for normal READY present window (PAL first_x=48, L+R borders, paper origin 48): tests `ReadyPal_NormalBorderWindow_*`, `ViewportFirstX_ReadyPal_*`, `SpaceGlyph_PaintsBackgroundPaper_*`  
  - **Partial** overall draw (no half_char / old_mc mid-line path, no raster cache)  
- **Not Invented:** crop is not `src=0`; paper is not a continuous invent strip.

70→### VIC-I mem / regs

- **VICE:** `vic-mem.c`  
- **Managed:** reg array; height from reg3 bit0; raster encode on read 3/4  
- **Status:** **Partial**  
- **Missing:** Sound store side effects (Explicit Missing), lightpen/pot, mid-line `raster_changes_*`.

### VIC-I sound

- **VICE:** `vic20sound.c`  
80→- **Managed:** `$900A-$900E` register store only  

### Turn 55 (Function)
[tool_response]
170→- D64 stream load, sector writes, and commit-to-stream are covered; launcher-level
  save-as workflows and non-D64 formats are still future work.

## 7. What works today / what doesn't

This matches the [README dashboard](../README.md#completion-dashboard); the matrix below is the practical "what should I expect" view.

| Area | State | Notes |
|------|-------|-------|
| C64 host (MOS 6510 + VIC-II + SID + CIA x2 + PLA + KERNAL boot) | Working | Cycle-exact lockstep parity with native VICE `x64sc`: 322 x64sc variant cases (C64 / C64C / SX-64, PAL + NTSC) at multi-frame depth, plus the 335-case lockstep/checkpoint gate and 100k-cycle parity. |
180→| Multi-system substrate (YAML topology + auto-binds) | Working | Canonical sample builds C64 + true-drive 1541 with one CLI invocation; ROM-backed boot still requires local ROM assets. |
| 1541 drive (true-drive: 6502 + VIA1 + VIA2 + IEC) | Bounded | IEC, head step, motor, D64 sector reads/writes, and stream commit are wired; drive-CPU lockstep and full KERNAL load validation remain Phase 1 work. |
| CIA1 / CIA2 timers, TOD, ports | Working | Full solution test passes. |
| SID hard sync + ring modulation | Working | Voice i syncs from voice ((i+2) % 3); triangle XOR with sync-source MSB. |
| SID audio backend wiring | Working (default on desktop) | The Avalonia desktop app enables a WinMM audio backend by default on Windows (it sets `VICESHARP_AUDIO=1` at startup; set `VICESHARP_AUDIO=0` to force silence). Headless and test hosts stay silent. Library consumers pass an `IAudioBackend` to `Sid6581(IBus, IAudioBackend?)` to receive 256-sample batches. |
| Standard 8K / 16K cartridge mapping (raw + CRT) | Working | Image normalises to ROML/ROMH and drives the live C64 memory map with GAME/EXROM behaviour; broader mapper families are post-MVP. |
| Tape (TAP pulse reads) | Bounded | TAP pulse stream, CIA1 FLAG integration, builder wiring, rewind, and seek are present; spin-up/spin-down timing and record state remain Phase 1 work. |
| Snapshot save/load | Bounded | 64K + public CPU state round-trips; full chip, timing, and resume state remain Phase 1 work. |
| Media capture / recording | Working | Screenshot (PNG/BMP), WAV sound, BMP frame sequence (all or unique frames), and muxed video with sound (MP4 / MKV / AVI via ffmpeg) export through the gRPC capture surface and the Avalonia Snapshot menu. See "Media capture and recording" below. |
| VIC-II pixel sequencer / sprite collisions | Partial | Visible sprite composition, sprite priority/collision coverage, display-mode pixel routing including invalid ECM priority/collision, managed continuous side-border behavior, VIC-II register readback masks/collision latch writes, and managed matrix idle/fill behavior are covered; native display-mode/register/matrix checkpoints, sprite fetch depth, and FLI/AFLI timing remain under `BACKFILL-VIDEO-001`. |
190→| SID combined waveforms + ADSR-bug accuracy | Working | Combined waveform, ADSR, digi, filter, PCM-equivalence, and dual-SID coverage are in the focused suite; further analog 8580/filter deepening is post-MVP unless final lockstep exposes a concrete regression. |
| Cartridge ports / user port as live CPU attachment | Substrate ready | `IInterSystemBus` supports `UserPort` and `CartPort` bus kinds; chip-level bindings are in for CIA2 / VIA1 / VIA2 / GAME / EXROM. Cartridge-as-running-CPU sample topology is future work. |
| C128 / VIC-20 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException`. |
| Host UI (Avalonia, monitor, gRPC control) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Launcher-integrated always-on UI remains separate work. |

### Warp, speed limiter, and speed controls

The Avalonia desktop UI exposes VICE-style speed controls:

- **Warp mode** runs the emulator uncapped, same semantics as VICE `-warp`; live sound is discarded while warp is on. Toggle it with **Alt+W**, the System menu's **Warp mode** item, or the Warp toggle in the Settings **Limiter** section.
200→- **Limiter slider** (Settings > Limiter) sets a live target speed percentage; the emulator paces to the slider target. Past 200% live sound is suspended.
- **Speed-cycle button** jumps between 100% (sound on) and 200%.
- The **status bar** shows the current state as `Limiter 100%` (or the active percentage) and `WARP` while warp is on.

### Media capture and recording

From the Avalonia desktop UI, the **Snapshot** menu exports emulator output:

- **Save screenshot...** writes the current frame as PNG or BMP (chosen by the file extension).
- **Record sound...** toggles a WAV recording of the SID output (start, then choose a file; pick the menu item again to stop).
210→- **Record video** offers three modes (toggle to start, pick again to stop):
  - **MP4 + sound** muxes H.264 video and AAC audio into an `.mp4` (also `.mkv` / `.avi` by extension). Requires `ffmpeg` on `PATH` (or set `VICESHARP_FFMPEG` to its full path).
  - **BMP sequence (all frames)** writes one numbered `frame_NNNNNN.bmp` per emulated frame into the chosen folder.
  - **BMP sequence (unique frames)** writes only frames that differ from the previous one, collapsing static screens.

Everything is also driveable over the gRPC `CaptureService` (capability discovery, screenshot, start/stop, and listing active captures). Muxed video and WAV sound need a live audio device; headless hosts export screenshots and the BMP sequence only.

## 8. Diagnostics and debug attach

When the Avalonia app starts its in-process gRPC host, it publishes a local attach file:
220→
```text
%LOCALAPPDATA%\ViceSharp\debug-attach.json
```

The file records the process id, gRPC endpoint, protocol package, app version, current UI session id, auth mode, and timestamps. It is rewritten when the current UI session changes and is removed on clean shutdown.

For human debugging, use the Debug menu's **Copy Debug Attach Info** item. It copies the same attach information plus a current status summary.

For tool debugging, read `debug-attach.json` and call the diagnostics service directly. Development builds or `VICESHARP_GRPC_REFLECTION=1` enable gRPC reflection, so `grpcurl` can discover the service surface:
230→
```pwsh
$attach = Get-Content "$env:LOCALAPPDATA\ViceSharp\debug-attach.json" | ConvertFrom-Json
grpcurl -plaintext $attach.endpoint list
grpcurl -plaintext -d "{}" $attach.endpoint vice_sharp.v1.DiagnosticsService/GetPerformanceSnapshot
```

`GetPerformanceSnapshot` defaults to the current UI session when `session_id` is omitted. Explicit unknown session ids return `NotFound`.

## 9. Troubleshooting
240→
**`Failed to load multi-system YAML: ...`** - the loader is strict about schema. Re-check `kind:` (case-sensitive: `C64`, `C1541`), `busAttachments:` references resolve to a `buses:` entry, and `deviceNumber:` is 8..11.

**Missing ROMs / `FileNotFoundException` on kernal/basic/chargen** - `VICESHARP_ROM_PATH` is unset or pointing at the wrong directory. Verify the layout in [ROMs.md](ROMs.md#rom-directory-structure).

**Drive sits idle / `PC` never moves** - true-drive emulation may be off. In YAML, set `fidelity: TrueDevice` (the launcher library's `+truedrive` flag maps to the same setting). The CPU on a buffered drive doesn't run by design.

**`Native VICE failed to create a machine`** - the native shim couldn't allocate a VICE instance. On Windows this is usually disk pressure (the shim writes scratch state) or a missing native bundle. Free space and confirm `native/vice_x64.dll`, `native/libiconv-2.dll`, and `native/zlib1.dll` exist, or rebuild the shim with `pwsh native/build-vice-shim.ps1` (needs MSYS2 + gcc). Maintainers can package a rebuilt bundle with `pwsh tools/Package-NativeViceBinary.ps1`.

**`SetDriveTrueEmulation` returns non-zero** - VICE rejected the resource set. The most common cause is an out-of-range unit number; valid units are 8..11.

### Turn 56 (Function)
[tool_response]
exit: 1
status: verified
sessionId: GrokCode-20260810T130431Z-plugin-session
agent: GrokCode
started: 2026-08-10T13:04:31Z
lastUpdated: 2026-08-10T13:04:31Z
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-08-09T02:03:48.7967449Z
agentSessionId: 019fd0df-383d-7fc0-9f0a-69785afd78ed
agentSessionTranscriptFile: ""
agentExecutablePath: C:\Users\kingd\.grok\bin\grok.exe
agentExecutableVersion: grok 1.0.0 (3cd0d0cbce) [stable]
---TURN---
turnRequestId: req-20260810T130257Z-prompt-72b6
queryTitle: <user_query>
openedAt: 2026-08-10T13:02:57Z
status: in_progress
sessionId: GrokCode-20260810T130242Z-plugin-session
codeEdits: 0
lastBuildStatus: unknown
auditActions: 0
auditDialog: 0
auditDecisions: 0
auditFiles: 0
auditCommits: 0
queryText: |-
  <user_query>
  /refresh-docs and /mcpserver:wrap-up with detailed handoff
  </user_query>
  <skill_information>
  <skills_referenced>
  <skill name="refresh-docs" path="C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md"/>
  <skill name="mcpserver:wrap-up" path="C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/wrap-up\SKILL.md"/>
  </skills_referenced>
  <skill name="refresh-docs" args="and">
  # Refresh Docs

  Reconcile `docs/` with the current codebase, remove documentation that no longer applies, make `docs/wiki.yaml` list every project doc that should be published to the wiki, and export the MCP-backed requirement documents.

  ## Preconditions

  - Work from the repository root.
  - Read `AGENTS-README-FIRST.yaml` and follow its marker, plugin, session, and trust rules. Verify the marker signature and the `/health` nonce before any MCP mutation.
  - Use the required MCP Server plugin for your agent (Claude/Codex/Grok) for requirements and export (`workflow.requirements.*`). Do not hand-author exported requirement docs or substitute raw REST.
  - Use the operator's approved shell for all shell invocation (e.g. PowerShell.Mcp on Windows). Build YAML/JSON from native objects and serialize; never handwrite fragile payloads.
  - Read facts from the code, not from memory. This skill refreshes and exports only; it does not commit or push (leave that to a wrap-up flow or an explicit request).

  ## Step 1 - Refresh project documentation

  1. Inventory the docs surface: `docs/user/*`, `docs/architecture/*`, `docs/requirements/*.md`, `docs/testing/*`, `docs/*.md`, and `README.md` (adapt to the repo's actual layout).
  2. Establish what changed since the docs were last touched: `git log --oneline` for the changed code areas, the current project/solution structure, public commands, and settings. Prefer the shipped behavior over any doc's prior claims.
  3. Update each doc that drifted: renamed or removed components, changed UI/behavior, new or removed features, install commands and version pins, and cross-references. Match the existing doc voice; no em-dashes.
  4. Keep the project `README.md` current as a first-class refresh target, not an afterthought: the project description and overview, the feature list, install and quickstart/usage commands, current version references, badges, and links into `docs/` must all match the shipped state. Reconcile it against the code and the refreshed docs on every run.
  5. If the repo ships a documentation screenshot/capture script (for example `scripts/Capture-ClientToolScreenshots.ps1`) and the UI changed, regenerate the embedded screenshots into the docs image folder from the current build, and verify a raw capture reflects the current UI before keeping it.

  ## Step 2 - Prune out-of-date docs

  1. Find docs describing removed or superseded features: grep the docs for names of deleted components, APIs, or flows and confirm against the code that they no longer exist.
  2. Delete the obsolete `.md` files, and remove their references from index pages (for example `docs/user/index.md`) and from `docs/wiki.yaml`.
  3. Do not delete a doc that still has accurate content. When a doc is partly stale, update it rather than delete. When unsure whether to remove a doc, flag it in the report instead of deleting.

  ## Step 3 - Update docs/wiki.yaml

  `docs/wiki.yaml` uses schema `mcp-wiki-export/v1` with `home.document`, a `documents` list, and a `navigation` tree. Each `documents` entry is `{ id, title, source, target, platforms }`.

  1. Keep the server-generated requirement docs (their `source` is `generated:<key>`, for example `generated:home|functional|technical|testing|mapping|matrix`). Do not point these at files.
  2. Add or refresh a `documents` entry for every CURRENT project doc that should appear in the wiki (the user guides under `docs/user/` and the architecture docs under `docs/architecture/`, plus `docs/Development-Process.md` / `README.md` if desired):
     - `source`: the repo-relative path to the markdown file, for example `docs/user/client-tool.md`.
     - `target`: the wiki page filename in PascalCase-with-hyphens, for example `Client-Tool.md`.
     - `title`: the human page title.
     - `platforms`: `[github, azure]` unless a doc is platform-specific.
     Use stable `id` slugs (for example `user-client-tool`, `arch-overview`).
  3. Remove `documents` and `navigation` entries for any doc pruned in Step 2 or no longer present on disk.
  4. Add each new document to `navigation` (as a child of the appropriate parent, or top-level). Keep the generated requirement docs grouped as they are.
  5. If the repo has a wiki sync script (for example `scripts/Sync-GitHubWikiDocs.ps1`) with its own project-doc list, cross-check it against `docs/wiki.yaml` so the manifest and the sync script agree on which project docs publish and their page names.
  6. Confirm the file still parses as YAML after editing.

  ## Step 4 - Export requirements

  1. Export via the MCP requirements workflow: `workflow.requirements.generateDocument` with `format: wiki` and `docType: all`.
  2. Write the returned `contentBase64` ZIP exactly to `docs/requirements/requirements-wiki-documents.zip`. The MCP database is the source of truth; do not hand-author the exported docs.
  3. If the server also regenerates in-place markdown under `docs/Project/wiki/`, review the diff. Keep only the intended requirement changes. Do NOT accept destructive deletions of project-doc wiki pages that are listed in `docs/wiki.yaml`; that means the manifest and the export disagree, so fix `docs/wiki.yaml` in Step 3 rather than accept the deletions.
  4. If the server rejects `format: wiki`, record it as a blocker and stop; do not fall back to another format unless the user asks.

  ## Validation and report

  - Run `git diff --check`; confirm `docs/wiki.yaml` parses; if code samples in docs changed, sanity-build the relevant snippet.
  - Report only verified facts: `README.md` updated, docs updated, docs pruned (with reasons), `docs/wiki.yaml` entries added/removed, screenshots regenerated, the requirements export artifact path, and any docs you flagged instead of deleting or any blockers.
  - Do not commit or push; summarize the changes and let the operator run a wrap-up or commit explicitly.


  **ARGUMENTS:** and
  </skill>
  <skill name="mcpserver:wrap-up" args="with detailed handoff">
  Finish and synchronize MCP-backed work: reconcile requirements, export wiki documents, run validation, commit/push, and close out the session-log turn.

  ## Trust and transport

  Trust marker details only after the plugin's local status/bootstrap check confirms marker trust and workspace health (including signature and nonce health). Use the plugin's supported wrapper surface for MCP mutations (for example `lib/repl-invoke.ps1`, or the plugin's session, todo, and requirements tools); do not use raw REST for normal MCP mutations.

  ## Method-name disambiguation

  `workflow.*` names in this skill (`workflow.sessionlog.*`, `workflow.todo.*`, `workflow.requirements.*`) are plugin workflow/REPL method names, not literal native MCP tool names. Native `/mcp-transport` tools use names such as `sessionlog_*`, `todo_*`, and `requirements_*` (for example `sessionlog_submit`, `todo_list`, `requirements_generate`); hosted-agent adapters may expose `mcp_*` aliases. Do not declare the plugin unavailable solely because generic MCP discovery does not list literal `workflow.*` names.

  If your agent exposes only a `pwsh` (or shell) tool, invoke the plugin shim directly instead of declaring it unavailable:

  ```
  pwsh.exe -NoProfile -NonInteractive -File "<plugin-root>\lib\repl-invoke.ps1" -Method <workflow.method> -ParamsYaml <yaml>
  ```

  ## Wrap-up steps

  1. Reconcile requirements with `workflow.requirements.*`, including requirement reconciliation for new FR/TR/TEST evidence.
  2. Export wiki requirements with `workflow.requirements.generateDocument` using `format: wiki` and `docType: all`.
  3. Run validation commands and keep zero-failure zero-skip evidence.
  4. If commit/push is required, use the `commit-sync` pause and acknowledgement contract first.
  5. Perform session-log reconciliation with `workflow.sessionlog.appendDialog` and `workflow.sessionlog.appendActions`.
  6. Finish with `workflow.sessionlog.completeTurn`; use `workflow.sessionlog.failTurn` when validation, export, or commit/push cannot be completed (validation failure, export failure, or blocked commit/push).


  **ARGUMENTS:** with detailed handoff
  </skill>
  </skill_information>
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-08-09T02:03:48.7967449Z
---BOOTSTRAP---
repl-invoke.ps1: A parameter cannot be found that matches parameter name 'WorkspacePath'.


### Turn 57 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1
- limit: 80
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
# Update turn title + bootstrap via Invoke-McpPlugin
$params = @{
  requestId = 'req-20260810T130257Z-prompt-72b6'
  queryTitle = 'Refresh docs and wrap-up with detailed handoff'
  queryText = '/refresh-docs and /mcpserver:wrap-up with detailed handoff'
}
# beginTurn may already exist - try updateTurn
$yaml = @"
requestId: req-20260810T130257Z-prompt-72b6
queryTitle: Refresh docs and wrap-up with detailed handoff
interpretation: Operator requested docs refresh plus MCP wrap-up and a detailed handoff after VIC-20 present-path, expansion UX, and flash cart builder work.
response: In progress - MCP trust OK (GrokCode), refreshing docs then export/validate/commit.
tags:
  - refresh-docs
  - wrap-up
  - vic20
  - flashcart
"@
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.updateTurn' -Params $yaml -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' 2>&1

- description: Update active session turn title and status
[tool_request: todo_write]
- todos: [{"id":"1","content":"Refresh project docs (README, USER-GUIDE, HANDOFF, roadmap, flash cart doc)","status":"in_progress"},{"id":"2","content":"Update docs/wiki.yaml and docs/README index","status":"pending"},{"id":"3","content":"MCP requirements export (wiki zip)","status":"pending"},{"id":"4","content":"Validate (focused tests + git diff --check)","status":"pending"},{"id":"5","content":"Commit/push intended work + session log completeTurn","status":"pending"},{"id":"6","content":"Write detailed handoff report","status":"pending"}]
- merge: false

### Turn 58 (Function)
[tool_response]
- [in_progress] 1: Refresh project docs (README, USER-GUIDE, HANDOFF, roadmap, flash cart doc)
- [pending] 2: Update docs/wiki.yaml and docs/README index
- [pending] 3: MCP requirements export (wiki zip)
- [pending] 4: Validate (focused tests + git diff --check)
- [pending] 5: Commit/push intended work + session log completeTurn
- [pending] 6: Write detailed handoff report


### Turn 59 (Function)
[tool_response]
1→<#
.SYNOPSIS
    Sends a YAML request envelope through the PowerShell MCP runtime.
.DESCRIPTION
    Constructs a YAML envelope and routes it through the configured
    PowerShell MCP invocation path.

    Translation shim: workflow.sessionlog.* methods are not server routes
    — the dispatcher rejects them as method_not_found. They are plugin-
10→    local verbs that update cache/current-turn.yaml so the Stop hook can
    verify completion, and persist a session-log turn via the real
    client.SessionLog.SubmitAsync route.

    Two usage modes:
      1. Script entry: pwsh -File repl-invoke.ps1 -Method <m> [-ParamsYaml <y>]
      2. Dot-source for Invoke-ReplMethod cmdlet:
             . .\repl-invoke.ps1
             Invoke-ReplMethod -Method workflow.sessionlog.completeTurn ...
#>
20→[CmdletBinding()]
param(
    [string]$Method,
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'

$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -Force -ErrorAction Stop
30→$shimCommand = Get-Command New-McpPluginTurnUpsertRequest -ErrorAction Stop
if (-not $shimCommand.Parameters.ContainsKey('ProcessingDialog')) {
    Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
    Import-Module $shimModule -Force -ErrorAction Stop
    $shimCommand = Get-Command New-McpPluginTurnUpsertRequest -ErrorAction Stop
    if (-not $shimCommand.Parameters.ContainsKey('ProcessingDialog')) {
        throw 'McpPluginShim.psm1 is stale or invalid because New-McpPluginTurnUpsertRequest lacks ProcessingDialog.'
    }
}
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')
40→. (Join-Path $PSScriptRoot 'agent-runtime-header.ps1')
Import-McpYamlSerializer
. (Join-Path $PSScriptRoot 'marker-resolver.ps1')

$script:ReplInvokePluginRoot = if ($env:MCP_PLUGIN_ROOT) {
    $env:MCP_PLUGIN_ROOT
} else {
    Split-Path -Parent $PSScriptRoot
}

50→# Agent for per-agent REPL cache and isolation. Must be passed to every mcpserver-repl call.
# Keep this precedence aligned with the shell resolve-cache-dir counterpart and MarkerFileClientOptionsResolver.ResolveAgentKey.
$script:AgentName = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_NAME) { $env:PLUGIN_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT }
                   elseif ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST }
                   else { 'default' }

function Get-ReplCanonicalAgentName {
    # TR-MCP-REPL-011: map the resolved agent (which can fall back to lowercase 'default' or a
60→    # lowercase host key like 'claude-code') to a PascalCase source type so composed session ids
    # satisfy the server regex ^[A-Z][A-Za-z0-9]*-... (BUG-TRIAGE-085).
    param([string]$AgentName)

    if ([string]::IsNullOrWhiteSpace($AgentName)) { return 'ClaudeCode' }

    switch (($AgentName.Trim().ToLowerInvariant() -replace '[^a-z0-9]', '')) {
        'claude'       { return 'ClaudeCode' }
        'claudecode'   { return 'ClaudeCode' }
        'claudecowork' { return 'ClaudeCowork' }
70→        'codex'        { return 'Codex' }
        'copilot'      { return 'Copilot' }
        'grok'         { return 'GrokCode' }
        'grokcode'     { return 'GrokCode' }
        'cline'        { return 'Cline' }
        'clinev2'      { return 'Cline' }
        'opencode'     { return 'OpenCode' }
    }

    $parts = [regex]::Split($AgentName.Trim(), '[^A-Za-z0-9]+') | Where-Object { $_ }
80→    if (-not $parts) { return 'ClaudeCode' }

### Turn 60 (Function)
[tool_response]
exit: 1
workflow.sessionlog.updateTurn re-binding current-turn cache 'F:\GitHub\vice-sharp\.mcpServer\grok\current-turn.yaml' from rotated sessionId 'GrokCode-20260810T130242Z-plugin-session' to active sessionId 'GrokCode-20260810T130431Z-plugin-session'.
workflow.sessionlog.updateTurn failed for 'req-20260810T130257Z-prompt-72b6': The property 'Count' cannot be found on this object. Verify that the property exists.
Exception: F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1:306
Line |
 306 |          throw $msg
     |          ~~~~~~~~~~
     | Plugin command failed with exit code 1.


### Turn 61 (Assistant)
Session turn hit a plugin rebind glitch; continuing docs refresh and retrying MCP after.
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\USER-GUIDE.md
- offset: 180
- limit: 20
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# Spot-check key shipped behaviors from code for docs accuracy
Select-String -Path 'src/ViceSharp.Chips/Vic/Mos6561.cs' -Pattern 'ComputeViewportFirstX|BorderX|first_x|48' | Select-Object -First 15
Select-String -Path 'src/ViceSharp.Core/FlashCarts/FlashCartProfiles.cs' -Pattern 'Fe3|Ultimem|MegaCart|EasyFlash' | Select-Object -First 20
Select-String -Path 'src/ViceSharp.Xbox/Views/SettingsPage.xaml' -Pattern 'VariableSizedWrapGrid|FlashCart|Expansion|BLK' | Select-Object -First 20
Select-String -Path 'src/ViceSharp.Avalonia/Views/SettingsView.axaml' -Pattern 'WrapPanel|FlashCart|Expansion|BLK' | Select-Object -First 20
# About version string
Select-String -Path 'src/ViceSharp.Xbox.ViewModels/AboutInfo.cs' -Pattern 'Version|1\.2' | Select-Object -First 10

- description: Verify code facts for docs accuracy

### Turn 62 (Function)
[tool_response]
180→| Multi-system substrate (YAML topology + auto-binds) | Working | Canonical sample builds C64 + true-drive 1541 with one CLI invocation; ROM-backed boot still requires local ROM assets. |
| 1541 drive (true-drive: 6502 + VIA1 + VIA2 + IEC) | Bounded | IEC, head step, motor, D64 sector reads/writes, and stream commit are wired; drive-CPU lockstep and full KERNAL load validation remain Phase 1 work. |
| CIA1 / CIA2 timers, TOD, ports | Working | Full solution test passes. |
| SID hard sync + ring modulation | Working | Voice i syncs from voice ((i+2) % 3); triangle XOR with sync-source MSB. |
| SID audio backend wiring | Working (default on desktop) | The Avalonia desktop app enables a WinMM audio backend by default on Windows (it sets `VICESHARP_AUDIO=1` at startup; set `VICESHARP_AUDIO=0` to force silence). Headless and test hosts stay silent. Library consumers pass an `IAudioBackend` to `Sid6581(IBus, IAudioBackend?)` to receive 256-sample batches. |
| Standard 8K / 16K cartridge mapping (raw + CRT) | Working | Image normalises to ROML/ROMH and drives the live C64 memory map with GAME/EXROM behaviour; broader mapper families are post-MVP. |
| Tape (TAP pulse reads) | Bounded | TAP pulse stream, CIA1 FLAG integration, builder wiring, rewind, and seek are present; spin-up/spin-down timing and record state remain Phase 1 work. |
| Snapshot save/load | Bounded | 64K + public CPU state round-trips; full chip, timing, and resume state remain Phase 1 work. |
| Media capture / recording | Working | Screenshot (PNG/BMP), WAV sound, BMP frame sequence (all or unique frames), and muxed video with sound (MP4 / MKV / AVI via ffmpeg) export through the gRPC capture surface and the Avalonia Snapshot menu. See "Media capture and recording" below. |
| VIC-II pixel sequencer / sprite collisions | Partial | Visible sprite composition, sprite priority/collision coverage, display-mode pixel routing including invalid ECM priority/collision, managed continuous side-border behavior, VIC-II register readback masks/collision latch writes, and managed matrix idle/fill behavior are covered; native display-mode/register/matrix checkpoints, sprite fetch depth, and FLI/AFLI timing remain under `BACKFILL-VIDEO-001`. |
190→| SID combined waveforms + ADSR-bug accuracy | Working | Combined waveform, ADSR, digi, filter, PCM-equivalence, and dual-SID coverage are in the focused suite; further analog 8580/filter deepening is post-MVP unless final lockstep exposes a concrete regression. |
| Cartridge ports / user port as live CPU attachment | Substrate ready | `IInterSystemBus` supports `UserPort` and `CartPort` bus kinds; chip-level bindings are in for CIA2 / VIA1 / VIA2 / GAME / EXROM. Cartridge-as-running-CPU sample topology is future work. |
| C128 / VIC-20 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException`. |
| Host UI (Avalonia, monitor, gRPC control) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Launcher-integrated always-on UI remains separate work. |

### Warp, speed limiter, and speed controls

The Avalonia desktop UI exposes VICE-style speed controls:

- **Warp mode** runs the emulator uncapped, same semantics as VICE `-warp`; live sound is discarded while warp is on. Toggle it with **Alt+W**, the System menu's **Warp mode** item, or the Warp toggle in the Settings **Limiter** section.

### Turn 63 (Function)
[tool_response]
exit: 0

src\ViceSharp.Chips\Vic\Mos6561.cs:73:    /// sets <c>first_x = 48</c>; paper at full-line 96 maps to canvas x 48.
src\ViceSharp.Chips\Vic\Mos6561.cs:75:    public const int BorderX = 48;
src\ViceSharp.Chips\Vic\Mos6561.cs:91:    public static int ComputeViewportFirstX(
src\ViceSharp.Chips\Vic\Mos6561.cs:652:        // Horizontal start is VICE viewport first_x (video_viewport_resize + 
video_canvas_refresh),
src\ViceSharp.Chips\Vic\Mos6561.cs:665:        var firstX = ComputeViewportFirstX(gfxX, gfxW, _frameWidth, lineW, 
gfxAreaMoves: true);
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:11:    public static IFlashCartProfile Fe3 { get; } = new 
FixedProfile(
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:12:        "fe3",
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:19:    /// <summary>VIC-20 Ultimem default image: 1 MiB, 8K 
banks.</summary>
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:20:    public static IFlashCartProfile Ultimem { get; } = new 
FixedProfile(
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:21:        "ultimem",
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:22:        "Ultimem (1MB image)",
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:23:        UltimemCartridge.DefaultImageSize,
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:29:    public static IFlashCartProfile MegaCart { get; } = new 
FixedProfile(
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:30:        "megacart",
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:32:        MegaCartCartridge.LowRomSize,
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:35:        secondarySizeBytes: MegaCartCartridge.NvramSize);
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:38:    /// C64 EasyFlash-style banked flash layout (structure for 
reuse).
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:39:    /// Default 1 MiB = 128 x 8K banks (UI for EasyFlash is out 
of band).
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:41:    public static IFlashCartProfile EasyFlash { get; } = new 
FixedProfile(
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:42:        "easyflash",
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:43:        "EasyFlash (C64 layout, 8K banks)",
src\ViceSharp.Core\FlashCarts\FlashCartProfiles.cs:50:        [Fe3, Ultimem, MegaCart, EasyFlash];
src\ViceSharp.Xbox\Views\SettingsPage.xaml:28:                <!-- VIC-20 RAM (xvic) + expansion cart + uIEC; 
IsVic20Selected gates visibility in code-behind if needed. -->
src\ViceSharp.Xbox\Views\SettingsPage.xaml:31:                <!-- UWP has no WrapPanel; VariableSizedWrapGrid is the 
wrap layout (left-to-right, then next row). -->
src\ViceSharp.Xbox\Views\SettingsPage.xaml:32:                <VariableSizedWrapGrid Orientation="Horizontal"
src\ViceSharp.Xbox\Views\SettingsPage.xaml:37:                    <ToggleSwitch Header="BLK0 $0400" IsOn="{x:Bind 
ViewModel.Vic20Blk0, Mode=TwoWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:38:                    <ToggleSwitch Header="BLK1 $2000" IsOn="{x:Bind 
ViewModel.Vic20Blk1, Mode=TwoWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:39:                    <ToggleSwitch Header="BLK2 $4000" IsOn="{x:Bind 
ViewModel.Vic20Blk2, Mode=TwoWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:40:                    <ToggleSwitch Header="BLK3 $6000" IsOn="{x:Bind 
ViewModel.Vic20Blk3, Mode=TwoWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:41:                    <ToggleSwitch Header="BLK5 $A000" IsOn="{x:Bind 
ViewModel.Vic20Blk5, Mode=TwoWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:42:                </VariableSizedWrapGrid>
src\ViceSharp.Xbox\Views\SettingsPage.xaml:43:                <TextBlock Text="Expansion cart (FE3 / Ultimem / 
Mega-Cart)" FontSize="22" Foreground="White" Visibility="{x:Bind ViewModel.IsVic20Selected, Mode=OneWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:44:                <ComboBox Header="Cart kind" ItemsSource="{x:Bind 
ViewModel.ExpansionCartKinds, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedExpansionCartKind, Mode=TwoWay}" 
Visibility="{x:Bind ViewModel.IsVic20Selected, Mode=OneWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:45:                <ComboBox Header="Cart preset" ItemsSource="{x:Bind 
ViewModel.ExpansionCartPresets, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedExpansionCartPreset, 
Mode=TwoWay}" Visibility="{x:Bind ViewModel.IsVic20Selected, Mode=OneWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:46:                <ToggleSwitch Header="Write image on detach" 
IsOn="{x:Bind ViewModel.ExpansionCartWriteBack, Mode=TwoWay}" Visibility="{x:Bind ViewModel.IsVic20Selected, 
Mode=OneWay}" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:48:                <TextBlock Text="{x:Bind 
ViewModel.ExpansionCartImageStatus, Mode=OneWay}"
src\ViceSharp.Xbox\Views\SettingsPage.xaml:61:                            Click="OnAttachExpansionCartImage" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:64:                            IsEnabled="{x:Bind 
ViewModel.HasExpansionCartImage, Mode=OneWay}"
src\ViceSharp.Xbox\Views\SettingsPage.xaml:65:                            Click="OnEjectExpansionCartImage" />
src\ViceSharp.Xbox\Views\SettingsPage.xaml:68:                            Click="OnOpenFlashCartBuilder" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:42:            <!-- VIC-20 system RAM (xvic -memory): presets + 
BLK0/1/2/3/5 -->
src\ViceSharp.Avalonia\Views\SettingsView.axaml:47:                <WrapPanel Orientation="Horizontal">
src\ViceSharp.Avalonia\Views\SettingsView.axaml:48:                    <CheckBox Content="BLK0 $0400" 
IsChecked="{Binding Vic20Blk0, Mode=TwoWay}" Margin="0,0,12,6" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:49:                    <CheckBox Content="BLK1 $2000" 
IsChecked="{Binding Vic20Blk1, Mode=TwoWay}" Margin="0,0,12,6" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:50:                    <CheckBox Content="BLK2 $4000" 
IsChecked="{Binding Vic20Blk2, Mode=TwoWay}" Margin="0,0,12,6" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:51:                    <CheckBox Content="BLK3 $6000" 
IsChecked="{Binding Vic20Blk3, Mode=TwoWay}" Margin="0,0,12,6" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:52:                    <CheckBox Content="BLK5 $A000" 
IsChecked="{Binding Vic20Blk5, Mode=TwoWay}" Margin="0,0,12,6" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:53:                </WrapPanel>
src\ViceSharp.Avalonia\Views\SettingsView.axaml:58:                <TextBlock Classes="section" Text="VIC-20 expansion 
cart" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:59:                <ComboBox Classes="row" ItemsSource="{Binding 
ExpansionCartKinds}"
src\ViceSharp.Avalonia\Views\SettingsView.axaml:60:                          SelectedItem="{Binding 
SelectedExpansionCartKind, Mode=TwoWay}" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:61:                <ComboBox Classes="row" ItemsSource="{Binding 
ExpansionCartPresets}"
src\ViceSharp.Avalonia\Views\SettingsView.axaml:62:                          SelectedItem="{Binding 
SelectedExpansionCartPreset, Mode=TwoWay}" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:63:                <CheckBox Content="Write image on detach/exit" 
IsChecked="{Binding ExpansionCartWriteBack, Mode=TwoWay}" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:64:                <Button Content="Build cart image." 
Click="OnOpenFlashCartBuilder" HorizontalAlignment="Left" />
src\ViceSharp.Avalonia\Views\SettingsView.axaml:164:            <WrapPanel Orientation="Horizontal">
src\ViceSharp.Avalonia\Views\SettingsView.axaml:171:            </WrapPanel>
src\ViceSharp.Xbox.ViewModels\AboutInfo.cs:88:    /// The fallback version string used when the running assembly does 
not carry a
src\ViceSharp.Xbox.ViewModels\AboutInfo.cs:89:    /// resolvable product version (see <see 
cref="AboutViewModel.Version"/>).
src\ViceSharp.Xbox.ViewModels\AboutInfo.cs:90:    /// Matches <c>GitVersion.yml</c> <c>next-version</c> 
major.minor.patch base;
src\ViceSharp.Xbox.ViewModels\AboutInfo.cs:91:    /// stamped builds prefer <see 
cref="System.Reflection.AssemblyInformationalVersionAttribute"/>.
src\ViceSharp.Xbox.ViewModels\AboutInfo.cs:93:    public const string Version = "1.2.2";



### Turn 64 (Assistant)
Updating the drifted docs now: README, USER-GUIDE, Iteration-Roadmap, HANDOFF, FlashCart guide, and wiki index.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\FlashCart-Builder.md
- new_string: # Flash Cart Image Builder

Portable builder for VIC-20 expansion flash images (and a C64 EasyFlash layout profile for reuse). UI-free Core types; Avalonia and Xbox host the same view model.

## Profiles

| Profile id | Display | Primary size | Bank size | Secondary |
|------------|---------|--------------|-----------|-----------|
| `fe3` | Final Expansion 3 | 512 KiB (`FinalExpansion3Cartridge.FlashSize`) | 8 KiB | none |
| `ultimem` | Ultimem (1MB image) | 1 MiB (`UltimemCartridge.DefaultImageSize`) | 8 KiB | none |
| `megacart` | Mega-Cart | 1 MiB low ROM (`MegaCartCartridge.LowRomSize`) | 8 KiB | 8 KiB NVRAM (`NvramSize`) |
| `easyflash` | EasyFlash (C64 layout) | 1 MiB (128 x 8K banks) | 8 KiB | none |

Primary UI lists FE3 / Ultimem / Mega-Cart. EasyFlash remains on `FlashCartProfiles.All` for structure reuse; C64 EasyFlash attach UI is out of band.

## Core types

- `src/ViceSharp.Core/FlashCarts/IFlashCartProfile.cs` - profile contract
- `FlashCartProfiles.cs` - shipped profile instances
- `FlashImageBuilder.cs` - pack primary (+ optional secondary) streams into a raw image
- `FlashCartImageBuilderViewModel.cs` - portable VM (bank slots, load file, build, status text)
- `IFlashBuilderFileIo.cs` - host file I/O seam for tests and UI

## UI entry points

### Avalonia desktop

Settings (VIC-20 machine selected) → **VIC-20 expansion cart** → **Build cart image.** opens `FlashCartBuilderView` dialog bound to `FlashCartImageBuilderViewModel`.

BLK0/1/2/3/5 RAM toggles use a horizontal `WrapPanel` so they wrap on narrow Settings panes.

### Xbox UWP

Settings (VIC-20 selected) → expansion cart section:

- Cart kind / preset combo boxes (FE3 presets include flash / super-rom / rom-ram / ram2 style options)
- Attach image / Eject image
- **Build cart image** navigates to `FlashCartBuilderPage` (`NavigationDestination.FlashCartBuilder`)

BLK toggles use `VariableSizedWrapGrid` (UWP has no `WrapPanel`).

## Attach path

Built or external images attach through the media/cartridge slot with size-based detection for FE3 / Ultimem / Mega-Cart. Expansion cart state is exposed via protocol (`ExpansionCartManageState`) and settings host APIs on Xbox (`XboxSettingsViewModel`) and Avalonia settings surfaces.

## Accuracy notes

- Image sizes and bank geometry for the three VIC-20 profiles are Exact against the matching cartridge type constants (hostile validator receipt `docs/receipts/hostile-validator-20260808T103211Z.md`).
- FE3 runtime: presets and banked flash image attach are in scope; residual **Partial** flash040 program/erase FSM vs full VICE FE3 flash model.
- Mega-Cart NVRAM secondary blob size is Exact; full Mega-Cart mapper runtime depth remains Partial / Stub per `docs/audit-vic20-vs-vice-2026-08-07.md`.

## Tests

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart"
```

Coverage: profile sizes, builder packing, portable VM bank load/build, FE3 preset enum surface.

- old_string: 
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\README.md
- old_string: ✅ **Iteration 2 (VIC-20)**: **Core + every-cycle native lockstep (2026-08-06)**:
  - Managed VIC-20 (PAL/NTSC), dual VIA, VIC-I, color RAM open-bus, default drive 8 = 1540
  - Native oracle: `vice_xvic.dll` / `ViceNative.CreateInstance("vic20"|"vic20ntsc")`
  - Every-cycle A/X/Y/S/P/PC vs xvic: **TenSecondPal** 11_084_050 and **TenSecondNtsc** 10_227_270 green
  - Receipts: `docs/receipts-lockstep-10s-2026-08-06.txt`, `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`
  - Gates (env): `VICESHARP_LOCKSTEP_10S=1`, `VICESHARP_LOCKSTEP_2S=1`; filter `FullyQualifiedName~Vic20DivergeProbe`
  - Tier-A polish still open: expansion UX, input E2E, cart/disk depth, snapshots (see [HANDOFF.md](HANDOFF.md))
- new_string: ✅ **Iteration 2 (VIC-20)**: **Core + every-cycle native lockstep + present-path + expansion UX (2026-08-08)**:
  - Managed VIC-20 (PAL/NTSC), dual VIA, VIC-I, color RAM open-bus, default drive 8 = 1540
  - Native oracle: `vice_xvic.dll` / `ViceNative.CreateInstance("vic20"|"vic20ntsc")`
  - Every-cycle A/X/Y/S/P/PC vs xvic: **TenSecondPal** 11_084_050 and **TenSecondNtsc** 10_227_270 green
  - READY present geometry Exact-scoped: VICE `first_x` crop (PAL first_x=48), L+R border bands, paper origin 48 (not invent `src=0` or paper strip)
  - Settings: wrap BLK0/1/2/3/5 toggles; FE3 / Ultimem / Mega-Cart attach/eject; portable [Flash Cart Builder](docs/FlashCart-Builder.md) (Avalonia dialog + Xbox page)
  - Receipts: `docs/receipts-lockstep-10s-2026-08-06.txt`, `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`; hostile AGREE `docs/receipts/hostile-validator-20260808T094911Z.md` (borders), `docs/receipts/hostile-validator-20260808T103211Z.md` (flash builder)
  - Gates (env): `VICESHARP_LOCKSTEP_10S=1`, `VICESHARP_LOCKSTEP_2S=1`; filter `FullyQualifiedName~Vic20DivergeProbe`
  - Still open: full FE3 flash040 FSM, pixel framebuffer lockstep vs xvic, input E2E, snapshots (see [HANDOFF.md](HANDOFF.md))
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\README.md
- old_string: Snapshot of VICE-to-ViceSharp parity sourced from MCP TODO state and the iteration roadmap. Last refreshed `2026-08-06` on `main` (line **v1.2.2**; VIC-20 every-cycle 10s PAL+NTSC lockstep green; Store art + CC BY-SA C= logo attribution; see [HANDOFF.md](HANDOFF.md)). Perf probe: 11.5M+ cycles/sec (47x the Phase 1 PERF-TUNING-001 target of 246,312 cps). Wiki publish: automated via `tools/Publish-Wiki.ps1` + Nuke `PublishWiki`. Advanced cartridge mappers: all 7 mappers landed as minimum-viable scaffolds. PLATFORM-CROSS-001: macOS, Xbox, Android, iOS host shells scaffolded.
- new_string: Snapshot of VICE-to-ViceSharp parity sourced from MCP TODO state and the iteration roadmap. Last refreshed `2026-08-10` on `main` (GitVersion base **v1.2.2**; local MSI/deploy line **1.2.7** from commits-since-version; VIC-20 every-cycle 10s lockstep + READY first_x borders + flash cart builder; Store art + CC BY-SA C= logo attribution; see [HANDOFF.md](HANDOFF.md)). Perf probe: 11.5M+ cycles/sec (47x the Phase 1 PERF-TUNING-001 target of 246,312 cps). Wiki publish: automated via `tools/Publish-Wiki.ps1` + Nuke `PublishWiki`. Advanced cartridge mappers: all 7 mappers landed as minimum-viable scaffolds. PLATFORM-CROSS-001: macOS, Xbox, Android, iOS host shells scaffolded.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\README.md
- old_string: | VIC-20 (MOS 6502 + VIC-I + VIA x2) | 2 | ✅ | 95% |
- new_string: | VIC-20 (MOS 6502 + VIC-I + VIA x2) | 2 | ✅ | 97% |
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\Iteration-Roadmap.md
- old_string: **Status (2026-08-06):** Core complete on `main` (merged from `feat/iteration2-vic20`). Every-cycle native lockstep green for multi-second PAL and NTSC.

- MOS 6502 CPU (reuse 6510 core minus I/O port)
- VIC-I (6560/6561) character-mode video (`Mos6561`) with PAL/NTSC timing
- VIA x2 (shared `Via6522`) at $9110 NMI / $9120 IRQ; keyboard matrix + joystick glue
- VIA timer CPU `Read` uses pre-Tick bus-visible counters (VICE LOAD at `maincpu_clk` before `CLK_INC`); peeks stay post-Tick
- Color RAM 4-bit + V-bus open-bus high nibble (`Vic20ColorRam` / `BasicBus`)
- 5KB base RAM + expansion packs (3K/8K/16K/24K/32K)
- MVP cartridge map (BLK PRG/raw) and READY boot proof with official ROMs
- Default drive unit 8 = **1540** (`DriveModel.C1540`); C64 remains 1541
- Launcher `xvic` topology; host session create; Xbox computer picker enables VIC-20
- Native oracle: `native/vice_xvic.dll` via `ViceNative.CreateInstance("vic20"|"vic20ntsc")`
- Every-cycle A/X/Y/S/P/PC lockstep vs xvic:
  - **PAL 10 s:** 11_084_050 cycles (`EveryCycle_CpuRegs_Match_TenSecondPal`)
  - **NTSC 10 s:** 10_227_270 cycles (`EveryCycle_CpuRegs_Match_TenSecondNtsc`)
  - Env: `VICESHARP_LOCKSTEP_10S=1` (and `VICESHARP_LOCKSTEP_2S=1` for 2 s PAL)
  - Receipts: `docs/receipts-lockstep-10s-2026-08-06.txt`, `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`

**Exit criteria:** Runs VIC-20 software, architecture switching works at runtime, and multi-second every-cycle register lockstep vs native xvic. Met for READY fingerprint, character frames, session factory, focused `FullyQualifiedName~Vic20` gates, and the 10 s PAL + NTSC diverge probes. Tier-A polish (expansion UX, input E2E, cart/disk depth, snapshots) remains open; see `HANDOFF.md`.
- new_string: **Status (2026-08-10):** Core complete on `main` (merged from `feat/iteration2-vic20`). Every-cycle native lockstep green for multi-second PAL and NTSC. READY present-path and expansion flash cart UX landed 2026-08-08.

- MOS 6502 CPU (reuse 6510 core minus I/O port)
- VIC-I (6560/6561) character-mode video (`Mos6561`) with PAL/NTSC timing
- VIA x2 (shared `Via6522`) at $9110 NMI / $9120 IRQ; keyboard matrix + joystick glue
- VIA timer CPU `Read` uses pre-Tick bus-visible counters (VICE LOAD at `maincpu_clk` before `CLK_INC`); peeks stay post-Tick
- Color RAM 4-bit + V-bus open-bus high nibble (`Vic20ColorRam` / `BasicBus`)
- 5KB base RAM + expansion packs (3K/8K/16K/24K/32K); Settings wrap BLK0/1/2/3/5 toggles (Avalonia `WrapPanel`, Xbox `VariableSizedWrapGrid`)
- Cartridge map: BLK PRG/raw + FE3 / Ultimem / Mega-Cart attach/eject; portable Flash Cart Image Builder (see `docs/FlashCart-Builder.md`)
- READY present geometry Exact-scoped: VICE viewport `first_x` crop (PAL 48), L+R borders, paper origin (hostile AGREE borders receipt)
- Default drive unit 8 = **1540** (`DriveModel.C1540`); C64 remains 1541
- Launcher `xvic` topology; host session create; Xbox computer picker enables VIC-20
- Native oracle: `native/vice_xvic.dll` via `ViceNative.CreateInstance("vic20"|"vic20ntsc")`
- Every-cycle A/X/Y/S/P/PC lockstep vs xvic:
  - **PAL 10 s:** 11_084_050 cycles (`EveryCycle_CpuRegs_Match_TenSecondPal`)
  - **NTSC 10 s:** 10_227_270 cycles (`EveryCycle_CpuRegs_Match_TenSecondNtsc`)
  - Env: `VICESHARP_LOCKSTEP_10S=1` (and `VICESHARP_LOCKSTEP_2S=1` for 2 s PAL)
  - Receipts: `docs/receipts-lockstep-10s-2026-08-06.txt`, `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`
- Audit matrix: `docs/audit-vic20-vs-vice-2026-08-07.md` (Exact only for named rules; pixel FB lockstep Missing)

**Exit criteria:** Runs VIC-20 software, architecture switching works at runtime, and multi-second every-cycle register lockstep vs native xvic. Met for READY fingerprint, character frames, session factory, focused `FullyQualifiedName~Vic20` gates, and the 10 s PAL + NTSC diverge probes. Remaining polish: FE3 flash040 residual, pixel framebuffer lockstep vs xvic, input E2E, snapshots; see `HANDOFF.md`.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\USER-GUIDE.md
- old_string: | Standard 8K / 16K cartridge mapping (raw + CRT) | Working | Image normalises to ROML/ROMH and drives the live C64 memory map with GAME/EXROM behaviour; broader mapper families are post-MVP. |
| Tape (TAP pulse reads) | Bounded | TAP pulse stream, CIA1 FLAG integration, builder wiring, rewind, and seek are present; spin-up/spin-down timing and record state remain Phase 1 work. |
| Snapshot save/load | Bounded | 64K + public CPU state round-trips; full chip, timing, and resume state remain Phase 1 work. |
| Media capture / recording | Working | Screenshot (PNG/BMP), WAV sound, BMP frame sequence (all or unique frames), and muxed video with sound (MP4 / MKV / AVI via ffmpeg) export through the gRPC capture surface and the Avalonia Snapshot menu. See "Media capture and recording" below. |
| VIC-II pixel sequencer / sprite collisions | Partial | Visible sprite composition, sprite priority/collision coverage, display-mode pixel routing including invalid ECM priority/collision, managed continuous side-border behavior, VIC-II register readback masks/collision latch writes, and managed matrix idle/fill behavior are covered; native display-mode/register/matrix checkpoints, sprite fetch depth, and FLI/AFLI timing remain under `BACKFILL-VIDEO-001`. |
| SID combined waveforms + ADSR-bug accuracy | Working | Combined waveform, ADSR, digi, filter, PCM-equivalence, and dual-SID coverage are in the focused suite; further analog 8580/filter deepening is post-MVP unless final lockstep exposes a concrete regression. |
| Cartridge ports / user port as live CPU attachment | Substrate ready | `IInterSystemBus` supports `UserPort` and `CartPort` bus kinds; chip-level bindings are in for CIA2 / VIA1 / VIA2 / GAME / EXROM. Cartridge-as-running-CPU sample topology is future work. |
| C128 / VIC-20 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException`. |
| Host UI (Avalonia, monitor, gRPC control) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Launcher-integrated always-on UI remains separate work. |
- new_string: | Standard 8K / 16K cartridge mapping (raw + CRT) | Working | Image normalises to ROML/ROMH and drives the live C64 memory map with GAME/EXROM behaviour; broader mapper families are post-MVP. |
| Tape (TAP pulse reads) | Bounded | TAP pulse stream, CIA1 FLAG integration, builder wiring, rewind, and seek are present; spin-up/spin-down timing and record state remain Phase 1 work. |
| Snapshot save/load | Bounded | 64K + public CPU state round-trips; full chip, timing, and resume state remain Phase 1 work. |
| Media capture / recording | Working | Screenshot (PNG/BMP), WAV sound, BMP frame sequence (all or unique frames), and muxed video with sound (MP4 / MKV / AVI via ffmpeg) export through the gRPC capture surface and the Avalonia Snapshot menu. See "Media capture and recording" below. |
| VIC-II pixel sequencer / sprite collisions | Partial | Visible sprite composition, sprite priority/collision coverage, display-mode pixel routing including invalid ECM priority/collision, managed continuous side-border behavior, VIC-II register readback masks/collision latch writes, and managed matrix idle/fill behavior are covered; native display-mode/register/matrix checkpoints, sprite fetch depth, and FLI/AFLI timing remain under `BACKFILL-VIDEO-001`. |
| SID combined waveforms + ADSR-bug accuracy | Working | Combined waveform, ADSR, digi, filter, PCM-equivalence, and dual-SID coverage are in the focused suite; further analog 8580/filter deepening is post-MVP unless final lockstep exposes a concrete regression. |
| Cartridge ports / user port as live CPU attachment | Substrate ready | `IInterSystemBus` supports `UserPort` and `CartPort` bus kinds; chip-level bindings are in for CIA2 / VIA1 / VIA2 / GAME / EXROM. Cartridge-as-running-CPU sample topology is future work. |
| VIC-20 host (MOS 6502 + VIC-I + VIA x2 + 1540 default) | Working core | Managed core + 10 s every-cycle A/X/Y/S/P/PC lockstep vs native `xvic` (PAL and NTSC). READY present uses VICE `first_x` crop with L+R borders. Settings: BLK wrap toggles, FE3/Ultimem/Mega-Cart attach, Flash Cart Builder ([FlashCart-Builder.md](FlashCart-Builder.md)). Pixel FB lockstep vs xvic still open. |
| C128 / PET / Plus/4 / CBM-II | Not yet | Launcher binaries throw `NotSupportedException` for those machines. |
| Host UI (Avalonia, monitor, gRPC control, Xbox UWP) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Xbox Settings includes VIC-20 expansion + flash builder page. |

### Turn 65 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\README.md has been updated successfully.

### Turn 66 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\Iteration-Roadmap.md has been updated successfully.

### Turn 67 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\USER-GUIDE.md has been updated successfully.

### Turn 68 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\README.md has been updated successfully.

### Turn 69 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\FlashCart-Builder.md has been created successfully.

### Turn 70 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\README.md has been updated successfully.

### Turn 71 (Assistant)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\HANDOFF.md
- old_string: # ViceSharp Handoff - 2026-08-06

**Active branch:** `feat/iteration2-vic20` (off `main` @ v1.2.1 line). Iteration 2 = overall project VIC-20 (not RomM Phase E).
**Prior release baseline:** `main` at **v1.2.1** (NuGet + winget published). Iteration 1 C64 complete.
**Working tree noise (do not commit):** untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, `manifests/`, many `docs/*-2026-08-06.log` lockstep artifacts.

## VIC-20 every-cycle lockstep - PAL + NTSC 10s GREEN (2026-08-06)
- new_string: # ViceSharp Handoff - 2026-08-10

**Active branch:** `main` (GitVersion base **v1.2.2**; local MSI/deploy line **1.2.7** from commits-since-version). Iteration 1 C64 complete; Iteration 2 VIC-20 core + lockstep + present-path + expansion UX on tree.
**Remotes:** `origin` = GitHub `sharpninja/vice-sharp` (downstream); Azure DevOps `azure` is source of truth for package/CI when both are used. Default push target per AGENTS: `origin` unless operator says otherwise.
**Working tree noise (do not commit unless operator asks):** untracked `docs/S-Blox/`, `docs/reviews/*`, bulk `docs/2s-*`, `docs/10s-*`, `docs/*-focused-*.log`, `docs/_deploy-*`, `docs/xvic-*` probe captures, `manifests/` winget copies. Prefer durable receipts under `docs/receipts*` and `docs/receipts/`.

## Session closeout 2026-08-10 (refresh-docs + wrap-up)

### What landed on the working tree (pre-commit / commit this wrap-up)

1. **VIC-I READY present path (Exact-scoped geometry)**  
   - `Mos6561`: full-line paint + border blank outside paper; crop via VICE `ComputeViewportFirstX` (READY PAL first_x=48, L+R 48, paper origin 48). Rejected invent crop `src=0` and continuous paper strip.  
   - Tests: `Vic20VideoTests` geometry/latch/space; audit rebaselined `docs/audit-vic20-vs-vice-2026-08-07.md`.  
   - Hostile AGREE: `docs/receipts/hostile-validator-20260808T094911Z.md`.

2. **VIC-20 expansion Settings UX**  
   - Avalonia: `WrapPanel` BLK0/1/2/3/5; expansion cart kind/preset/write-back; Build cart image dialog.  
   - Xbox UWP: `VariableSizedWrapGrid` for BLKs (no WrapPanel); attach/eject expansion image; navigate to FlashCartBuilder page.  
   - Protocol/host: expansion cart manage state, media size detect, settings option catalog.

3. **Flash Cart Image Builder (Byrd slice)**  
   - Core: `src/ViceSharp.Core/FlashCarts/*` (profiles FE3 512K, Ultimem 1MB, MegaCart+NVRAM, EasyFlash layout).  
   - Portable `FlashCartImageBuilderViewModel`; FE3 presets on expansion cart surface.  
   - UI: Avalonia `FlashCartBuilderView`; Xbox `FlashCartBuilderPage`.  
   - Tests: `tests/ViceSharp.TestHarness/FlashCart/*`.  
   - Hostile AGREE: `docs/receipts/hostile-validator-20260808T103211Z.md`.  
   - Residual **Partial**: FE3 flash040 program/erase FSM vs full VICE.

4. **Deploy evidence (local lab)**  
   - Desktop: Nuke `InstallMsi` ProductVersion **1.2.7** succeeded (`docs/_deploy-desktop-2026-08-08_045501.log`).  
   - Xbox: `DeployXboxLocal` used in-session (see deploy logs under `docs/_deploy-xbox-*`).

5. **Docs refresh this turn**  
   - README Iteration 2 + dashboard date; USER-GUIDE VIC-20 row; Iteration-Roadmap; new `docs/FlashCart-Builder.md`; wiki.yaml + docs/README index; this handoff.

### Exact vs Partial discipline

- Exact only with VICE file+function + matching managed control + hostile AGREE when claiming Exact.  
- Do **not** claim whole-machine Exact or pixel framebuffer lockstep vs xvic (still Missing / Partial per audit).  
- CPU every-cycle 10s lockstep remains green (separate from video FB).

### Resume next

1. Optional: FE3 flash040 residual (TDD first).  
2. Optional: pixel framebuffer compare vs xvic (`capture_visible_frame` / present path).  
3. Optional: re-run desktop InstallMsi if Avalonia builder only was built after last MSI.  
4. Store/Partner Center ADO wiring still open (see Store section below).  
5. Query MCP TODO store for live backlog; snapshot list at bottom is stale.

### Validation commands (focused)

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart"
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Video"
git diff --check
```

Full baseline (long): filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy` (expect 0 failed; skips allowed only if pre-existing category policy).

---

## VIC-20 every-cycle lockstep - PAL + NTSC 10s GREEN (2026-08-06)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\README.md
- old_string: ## User guides

- [USER-GUIDE.md](USER-GUIDE.md) - install, first run, CLI launcher, machine YAML, disk images, capture, diagnostics attach, what works today
- [VICE-MIGRATION.md](VICE-MIGRATION.md) - swap classic VICE for ViceSharp: binary + flag mapping, behaviour caveats, bug compatibility
- [ROMs.md](ROMs.md) - legal ROM options, environment variable, expected directory layout
- [PRIVACY.md](PRIVACY.md) - privacy policy (store listing source)
- new_string: ## User guides

- [USER-GUIDE.md](USER-GUIDE.md) - install, first run, CLI launcher, machine YAML, disk images, capture, diagnostics attach, what works today
- [FlashCart-Builder.md](FlashCart-Builder.md) - VIC-20 FE3 / Ultimem / Mega-Cart flash image builder (Avalonia + Xbox)
- [VICE-MIGRATION.md](VICE-MIGRATION.md) - swap classic VICE for ViceSharp: binary + flag mapping, behaviour caveats, bug compatibility
- [ROMs.md](ROMs.md) - legal ROM options, environment variable, expected directory layout
- [PRIVACY.md](PRIVACY.md) - privacy policy (store listing source)
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\README.md
- old_string: ## Lockstep receipts (durable)

- [receipts-lockstep-10s-2026-08-06.txt](receipts-lockstep-10s-2026-08-06.txt) - VIC-20 PAL 10 s every-cycle A/X/Y/S/P/PC vs xvic
- [receipts-lockstep-10s-ntsc-2026-08-06.txt](receipts-lockstep-10s-ntsc-2026-08-06.txt) - VIC-20 NTSC 10 s (soft-BIT / VIA bus-visible realign)
- [receipts-lockstep-vice-realign-2026-08-06.txt](receipts-lockstep-vice-realign-2026-08-06.txt) - VICE realign notes (V-bus, color RAM, order)

Ephemeral intermediate probe logs (`docs/*-focused-*.log`, `docs/2s-*.log` from debug slices) are not wiki content; prefer durable receipts above.
- new_string: ## Lockstep receipts (durable)

- [receipts-lockstep-10s-2026-08-06.txt](receipts-lockstep-10s-2026-08-06.txt) - VIC-20 PAL 10 s every-cycle A/X/Y/S/P/PC vs xvic
- [receipts-lockstep-10s-ntsc-2026-08-06.txt](receipts-lockstep-10s-ntsc-2026-08-06.txt) - VIC-20 NTSC 10 s (soft-BIT / VIA bus-visible realign)
- [receipts-lockstep-vice-realign-2026-08-06.txt](receipts-lockstep-vice-realign-2026-08-06.txt) - VICE realign notes (V-bus, color RAM, order)
- [audit-vic20-vs-vice-2026-08-07.md](audit-vic20-vs-vice-2026-08-07.md) - VIC-20 vs xvic Exact/Partial/Missing matrix (rebaselined 2026-08-08)
- [receipts/hostile-validator-20260808T094911Z.md](receipts/hostile-validator-20260808T094911Z.md) - hostile AGREE: READY first_x borders
- [receipts/hostile-validator-20260808T103211Z.md](receipts/hostile-validator-20260808T103211Z.md) - hostile AGREE: flash cart builder

Ephemeral intermediate probe logs (`docs/*-focused-*.log`, `docs/2s-*.log`, `docs/10s-*.log`, `docs/xvic-*` captures from debug slices) are not wiki content; prefer durable receipts above.
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\wiki.yaml
- old_string: - id: rom-setup
  title: ROM Setup and Legal Sources
  source: docs/ROMs.md
  target: ROM-Setup.md
  platforms:
  - github
  - azure
- id: privacy
  title: Privacy Policy
  source: docs/PRIVACY.md
  target: Privacy-Policy.md
  platforms:
  - github
  - azure
- new_string: - id: rom-setup
  title: ROM Setup and Legal Sources
  source: docs/ROMs.md
  target: ROM-Setup.md
  platforms:
  - github
  - azure
- id: flash-cart-builder
  title: Flash Cart Image Builder
  source: docs/FlashCart-Builder.md
  target: Flash-Cart-Builder.md
  platforms:
  - github
  - azure
- id: privacy
  title: Privacy Policy
  source: docs/PRIVACY.md
  target: Privacy-Policy.md
  platforms:
  - github
  - azure
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\wiki.yaml
- old_string: - id: iteration-roadmap
  title: Iteration Roadmap
  source: docs/Iteration-Roadmap.md
  target: Iteration-Roadmap.md
  platforms:
  - github
  - azure
- id: ai-review
  title: AI Code and Project Review (aiUnit)
  source: docs/AI-Review.md
  target: AI-Review.md
  platforms:
  - github
  - azure
- new_string: - id: iteration-roadmap
  title: Iteration Roadmap
  source: docs/Iteration-Roadmap.md
  target: Iteration-Roadmap.md
  platforms:
  - github
  - azure
- id: audit-vic20-vs-vice
  title: VIC-20 vs VICE Implementation Audit
  source: docs/audit-vic20-vs-vice-2026-08-07.md
  target: VIC20-vs-VICE-Audit.md
  platforms:
  - github
  - azure
- id: ai-review
  title: AI Code and Project Review (aiUnit)
  source: docs/AI-Review.md
  target: AI-Review.md
  platforms:
  - github
  - azure
[tool_request: search_replace]
- file_path: F:\GitHub\vice-sharp\docs\wiki.yaml
- old_string: - title: Getting Started
  path: Getting-Started
  children:
  - document: project-overview
  - document: user-guide
  - document: vice-migration
  - document: rom-setup
  - document: privacy
- title: Architecture
  path: Architecture
  children:
  - document: architecture
  - document: public-api
  - document: pubsub
  - document: decoupling
  - document: state-window
  - document: iteration-roadmap
  - document: ai-review
  - document: registry-of-lore
- new_string: - title: Getting Started
  path: Getting-Started
  children:
  - document: project-overview
  - document: user-guide
  - document: flash-cart-builder
  - document: vice-migration
  - document: rom-setup
  - document: privacy
- title: Architecture
  path: Architecture
  children:
  - document: architecture
  - document: public-api
  - document: pubsub
  - document: decoupling
  - document: state-window
  - document: iteration-roadmap
  - document: audit-vic20-vs-vice
  - document: ai-review
  - document: registry-of-lore

### Turn 72 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\HANDOFF.md has been updated successfully.

### Turn 73 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\wiki.yaml has been updated successfully.

### Turn 74 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\README.md has been updated successfully.

### Turn 75 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\README.md has been updated successfully.

### Turn 76 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\wiki.yaml has been updated successfully.

### Turn 77 (Function)
[tool_response]
The file F:\GitHub\vice-sharp\docs\wiki.yaml has been updated successfully.

### Turn 78 (Assistant)
[tool_request: todo_write]
- todos: [{"id":"1","status":"completed"},{"id":"2","status":"completed"},{"id":"3","status":"in_progress"}]
- merge: true
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
$env:MCP_AGENT_NAME = 'GrokCode'

# Parse wiki.yaml
try {
  Import-Module powershell-yaml -ErrorAction Stop
  $null = ConvertFrom-Yaml (Get-Content -Raw docs/wiki.yaml)
  'wiki.yaml: parse OK (powershell-yaml)'
} catch {
  # fallback: yq or just document that we checked structure
  if (Get-Command yq -ErrorAction SilentlyContinue) {
    yq e '.' docs/wiki.yaml | Out-Null
    'wiki.yaml: parse OK (yq)'
  } else {
    # minimal: check required keys present
    $t = Get-Content -Raw docs/wiki.yaml
    if ($t -match 'schema:\s*mcp-wiki-export/v1' -and $t -match 'flash-cart-builder' -and $t -match 'audit-vic20-vs-vice') {
      'wiki.yaml: structural keys OK (no yaml module)'
    } else {
      throw "wiki.yaml structural check failed: $_"
    }
  }
}

# Export requirements wiki
$exportParams = @'
format: wiki
docType: all
'@
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.requirements.generateDocument' -Params $exportParams -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' -TimeoutSeconds 180 2>&1

- description: Parse wiki.yaml and export requirements wiki
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$log = 'docs/receipts/wrapup-flashcart-vic20video-20260810.txt'
New-Item -ItemType Directory -Force -Path docs/receipts | Out-Null
$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart|FullyQualifiedName~Vic20VideoTests" --no-restore 2>&1 | Tee-Object -FilePath $log
$exit = $LASTEXITCODE
$sw.Stop()
"EXIT=$exit ELAPSED_MS=$($sw.ElapsedMilliseconds)" | Tee-Object -FilePath $log -Append
exit $exit

- description: Run FlashCart and Vic20Video focused tests
- timeout: 300000

### Turn 79 (Function)
[tool_response]
- [completed] 1: Refresh project docs (README, USER-GUIDE, HANDOFF, roadmap, flash cart doc)
- [completed] 2: Update docs/wiki.yaml and docs/README index
- [in_progress] 3: MCP requirements export (wiki zip)
- [pending] 4: Validate (focused tests + git diff --check)
- [pending] 5: Commit/push intended work + session log completeTurn
- [pending] 6: Write detailed handoff report


### Turn 80 (Function)
[tool_response]
exit: 0 [truncated: showing first/last 20.0KB of 673.9KB - full output at: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-2027b0a4-de12-476f-8807-bcae473af8b1-59.log]
wiki.yaml: parse OK (powershell-yaml)
type: result
payload:
  requestId: req-20260810T130929Z-acf1
  result:
    success: true
    content: ''
    contentBase64: UEsDBBQABgAIAC9BCl3KpZQopAIAABYFAAAlAAAAYXp1cmUvLm1jcC1yZXF1aXJlbWVudHMtbWFuaWZlc3QuanNvbqrm5VJQUCpOzkjNTVSyUlDKTS7QLUotLM0sSs1NzSsp1i3PzM7ULzNU0gErLMhJLEnLL8oFKU2sKi1KhYqnp+alFiWWpKY4loSWJINkjQyMzHQNLHQNDUIMja0MLK2MDfQsLUyMLC2MtA0MrAwMoDpT8pNLwTYpWSlEg+xQUFDyyM9N1ctNgahQUFByK81LLsnMz0vM0Q1CchuykpDU5Iy8zGS8KopLMvPScZsQpFuQWqTrFqTrm1hQkJmXjmw8srW6voklRZkVyNIBRflZqckluv5lqUVlmanlyHKhxalFuu6lmSkoXgrzdHbV9c1ML0oEeQxZfZC/r25waklpAbKgW05icYauc2JRia5TaWZOSmoRsmxAUWZZYnKlbkB+TmZyJbJMRFJ+hW5wSX5Rqm5AaVJOZnEGmscgcj6ZkLBxzi9A0e4e4APVHZwKjgEz3aBUdA9CjPBLrSjRDS5JLSjG9CxERUBRfkppcomuZ0pqXklmCYpFji7+cIvQvO5YlJyRWZKaXFJahBKCYP8k6zoGeCJ7OKA0Kbg0CVkkLDMlNV/XsTQlM1/XJTU5v7QgByMMEktSdcMz81LyUWLOswSUpjPz83SD8hNTchNRYiTM09nIQLesWBcckyDjS5BtdfTEElJBqemZxSVFlbr5abo++ajewZHGsSYptyBdcMQ6FSXmpaB5BntGwGpMSGpxCc7sAFiEmUlxsq5vfkpqDpYED5FFyhW6zvllqUWJ6ShR5JyTWFycmQwJIteU9FRd58TiVN2QIF2nxOTstMycHOQgAwdkcH5pUXKqrm9iXmZaajFKkPomJmdk5qXqIicI3WBwyYVsjHNRfnGxbgC0oNINzyxKTStKzE0tRlbkklqcXZJfAI30Yt1QcCICFT+xvFy1XABQSwMEFAAGAAgAL0EKXe6vbGhaBAAAMAgAAA0AAABhenVyZS9Ib21lLm1kUlYISi0szSxKzU3NKynm4tJV8MjPTeXlUlDQVYgGMWM1QKQmVMStNC+5JDM/LzEHRV+sBkJCF9lAmL6Q1OSMvMxkDG1wcRy6iksy89LRrApJBYti1xGkUJBapOAWpOCbWFCQmZceqxESpFuQWqTrFqQLFYK5CdmdCr6JJUWZFbEayIK6EEFNXi5dBffUErBTgksSi0pSU6DBEVCUn5WaXKLgX5ZaVJaZWh6rARXRhYnALAstTi1ScC/NTEmN1QCxdcFsmKxbTmJxhoJzYlGJgmduYnqqglNpZk5KalGsBlhGFySjCxWD6fHNTC9KBLsprSg/V8E5J7G4ODNZIczT2TVWA0TqQlXk58G0BPn7KgSnlpQWKCTmpSj4pKYn5igE55cWJacWx2oE+fvqgiVhqgOKMssSkysVAvJzMpMrQV4D83UhfHCoOBYlZ2SWpCaXlBbB0gyyUKwGMg9ubmlSTmaygmOAp0JQalpqUWpecmqsRgBYVNcxwBNJnW5waRLYrb6lJYmgdKcQWJpaClEdXJoEUxmWmZKaD1bnWJqSma/gkpqcX1qQA45+sJwuWFwXIQ7TGVySWJKqEJ6Zl5JfrqDhklqcmZ6nEFCUX5BfnJijGasBlteFyMP0eJakggI+P08hKD8xJTexIFYDLqQLFYKpDfN01jUyUCgrBseLgmduQQ44p0E8A3JVCTiyjAx0y4p1wbEGFoTpd/RUcM5PSQX7DZq0FIJSQUlNQSMxMzQvs0QzVsPRUxciBtMVlJqeWVxSVKmQn6bgk1+UCkrWEBHd/DRdkAg49pDTOjRBI7IxSqZDSuAIFSgZECPBuwUpRCTlVyg4FSXmpYAysYazrUJOfnq+QmJJSVFmUikoOjVjNdyCdEHqdGHqYH6Alwy4HAJXgN8doNICxYhYDZAQiiaYnRVmJsXJCr75K
ak58AIhAiSmCxZDlAfg0hGiGikQFZzzy1KLEtNTYzUgupDkdGFyMLuQ86uCa0p6qq5zYnGqQkiQglNicnZaZk5OrAZUDSRdwNXohgTpwtTATAOlHGhWVvBNzMtMSy2GJCxXXUgG14WJgmM+uCS/CJKoQGEPjXsQU8E3M7kovzg/rUQBogacLYszwFkJpEAXLKyLEIa5ACyu4JMJLpsVnPMLKkG5J78oVRcqpgsSg6l2D/ABZQp02yDJOFbDPcAHalFwKri+MUNL4RDb/FIrQO5MLSiGFa1gcV2QuG5wSWpBMWoxC5YFZe+U0uQSBc+U1LySzBK4OwOK8kHiujBxmFsdXfxhgQFYZkFqTmZeKqQQjdVwdPGHO7OktAActPAiDRqqjim6HvnJCr6JyRkgncgFokKko6+PQnByRmpuooJGmaFmrAZUmS6yMl2ICphznIvyi4t1A3ISS9Lyi3IVQj0VwjOLUtOKEnNBpTiqrC5CCqbdJbU4uyS/AKQP7lQFDUgJV6yj4FiWmJOfl5moGQsqDUEqoaVfsW4oqGQGAFBLAwQUAAYACAAvQQpdOAyT2adlAACexgAAIAAAAGF6dXJlL0Z1bmN0aW9uYWwtUmVxdWlyZW1lbnRzLm1kUlZwK81LLsnMz0vMUQhKLSzNLErNTc0rKVbQ8HUOUAhOLSpLLdLk5eLlUlZWcAxy9tANCQp1dQnyDHPVNTQ1MdQ1MDBSCCkqTdV1KcosS1UAiSl4ujorlGTmZualKyTmpSjk5pfkFykUJeYWgMzxTE12Ki3WC8lMztbQVMjMLciBWugY4qdblFpckJ9XnKpQXJJYkqqQm5ickZmXqqDh7OOt7+IY4qhQnlmSkZmnYGlhqpBcmZyTWqyQn6bgGOKnkFhcnFpUoqmn4JmaDHYKFhuMDQx0DAwMdME6kZylkJSall+UqlCUX5IICgokQ4JSE1OCU5NL8os0DC10DDQVilJLSovyihWcHH0VkipLUosV0orycxVczEz0FMI8nV0VMlOTk0qL9ZKtjEzMdY3MzHQUUkAhow8m9ZL1eLmCk/MLUq0UchIrU4t0DbVBgaKsrODk6Ozt5unjo+vr6uLpqGtgYKjgm5qSmaiQklqWmZxarKDhYmaio1CSWJCqqeCp76+Qhoi4gsSizJJKkEEuZiYumcXZnrmJ6akuYI0KxWDnKwTph+sg+VkH7IOi1MQUhcScHEQ8pKboKbgkliQWp5aAIgAedToKxamgiMnJzEvVAUdrUWpyfhEodlNSFZLzQfFYkqqn4JyTX5xaDE4CoGiBJoP0xAKFtPwihYCMxOJUBUPCgRDm6eLqDw6EME9nXU9PhbLM4syknFTdtKLE3FQFiIcVNIKcFcoz81Lyy3UUXHwdFZIzUpOzC/Iz80qKdRSKk4tSU/N0gxx9wekXak6Qs36YM1ryyk0sSc5ILYbEHzhx6CYmJ5cWgZJgUmpGYllmfhGSXZp6Cn6JJaDUjmSfQlliTmYKSEdxQVFmSSrYPSAvg0LXVCEvP083wNFHITc/JTWnWE8hGOw4hSBHX4Xi0qKyzDJQSs5LVQCpgXixNC85IzEvHRQf4HRVlpmcmVmcrA+mIUlYL9kKlAVNzYx1FCDCaaklyRl6yVaGxqa6hmZmOMPZLUjX2c0dHL4IJij9BOQkJqdm5OekpBYpFCGKA4WkxOTstMycnNQUcDyG+Lv4K+Rk5mUrILQTtswUodqUMstM8Vrm4RkQHOIY4gr2X0BqkW5JZnK2QlppTo5CckZmATT2kxMLSkqLUkEOcU1MzlCA8lMUwKqL8xILijPyS4oVSjJSIXoz80pSi0DlJKRwyk9TSC1LLaqEGJdWCjVcI8zTWUch2NNFR8HZ01FHIcDHURMcZimZxQU5iZUKmXlgI1NSk0rToYkUm2+0tByTk1MLShLzklMVnEFJqigz0UpLi5dLVyG6IhaUWBHWgDNkgI+jQirIJ/BCVcEzGFSOppXmQMoCmF6IfzMyC
9SLIa5XyCxG+L8gtQgSBqDCVqEqtShfISO/RLcgsSQDVFbkJ4MLSZhZzpBgTIEalJKanJ+SWqyQmVeSr5CXmJuaolCUmp5ZXJJapJ+WmZqTAsoopanFMO0hGakKyEGhUJyRX14M8QcostSLFSBGwixILAEHX3FqTmpySSoktkBxqKwMTl0BoSGezt7wmHcOCFXIzEtJLUjNS0nNK4H4Kzm/FBSV4EArSC3SBSkqLkhNTVHITS0pykwGGQcOIpAENLpSc0tzEkH1WHZqakGxQmZJsUJ+eR6K2akVqcmlJakpkMypgGwLKA1B4z81RQFiVyYoaSWWKDgHhKoXK6BpBpc9ZalFxaUIu0oSi9JTSxSSc/KTs0lOMQqxCnAvuUId6gypQTPzkmGVfj4osYHiH+YcSC0LDqm8/BKF/DyFRIXikvyc1DyF/CKF4uzMgoLUFIgiPXCUKsQqgHIcKOQg3ixILUoGhXxqYWliDigyc0oSdeHG55elFimkpILEykElJZgP8ahHlTU4pp3NTBQKijJzE4sqFUD1VbFCYlJ+aYmCpamuoYGBAsz8xBKQbA6o4ZEKdi84zEFVL1Q9suLyjNQ8haLSvLzMvHS4u0FJEZS1S4tBZXJaYnKqQk5mcUkxyApwGgFHSmpeSVEmqNrPLwI7LyO/uARsHzjrFaQWZRZkpBYl5oDiVSEls7gkMy+5JKcSbosvtE1TlFqcWgLOXanF0IIEa0qAJiM9UKKEpvFgFydwAncOdkkCWZEMCrVKHYXMvPTU4hJQzZeYp6NQlJpWlFqcAdIX4OPopxvk7+sL0qanEJyaWJScoQDWXpycmpeqUJSak5pYnFoMMwOSfYPyc31hpoEaclATwd7OyUwqSiyq1ONydNZ1BjvI0ApqsGNxZV4yvKGUnJNYXJyZlpmaopAByjUaKam5+frJRYnJ2frFmSmaCrp2Cs7FKUkumcUQf4Rlppb7gqrJkNTikmI9iGPjgyDtLo/MkmIkO42soJ5WgJQHmfl5oHIMlCYTSxSMDAgZ7gkOsXjnxIJixxIjAySTja0UfPKTE3NATnNPLEktB5fcoPAtVijLTAQ7GRQ+4flF2Wk5+eWgQMlTKCnKTE9PLSpWKE5OzANZjm4GxE9Qa8NBpXpxSEZqXnByYh6yv0ysFJyKMlPSU5GtD/APDilW0E8uTknSLzPUh8Q2xN7kxJycYgWQe5xzMlPzSvRCEouzi/VAxvpAIgoSKRpJYFMVUvJTixVAGRrkTnAUYFgHcWlAfnFJcTw2J5pawVp9oEAvSswENfuglgVBkh6oP5FaDCqkCcQxNDyCwIZANSPFhZmVQjo0ChDRnFSpkJyfl5aZrqBRlJ9brFCUn1+iUF6UWZKYlJMKD3odhdSc4lRoYII9ihSiweA6JDM/D+rXzOTsYqdKZ7Cp2ApYcCcI1OAJiQBlJAXn/LyS1IoSeDcFVHikFitoK5QUJeYVZ4K6EsWg7AcqWJLR1GYkQqveYgX3xNxUUMtA3zcxM883Na9UPyyzqKQ0Mcc7tTIpP7EoRR/spKJcl8zEnPx0cB9IISU1LTMPVP0hrNJTQLQYrBQSkVyhCwmUktTiEkhvBFbiaICdrJOZV1BaoqmQmpKeaq2Ql69QmodpvAKkOVoMCnWQrwkHkJGCX36eLsx3oDYQqBtTkF9UUqyQl1paUpSYAwodV3AjCtRAhiuFBVZafhFIS1J+SYYCij5rBUdEGk7LLEoFtaoSFXJT80pRQyEotTg/pwwinVcJboZj2AJu5jgqZIAaJ9oKxaDmH6ieSoM2MHTtFLzyKw1tvfIrjWz9IO7WAYUSyF7CoWCMGgpF+aWgTmOop0JeYllmOrQpxcvlmYfiOB0FF92CxBT9nNS0El2wk0BJGqJLR8HRNjTTMbkksyyxJFVHwck2NNMpMTkb1ecuAYkpLqAmiq6dQmimH8SyVJAIKPDAgjAjrBWcwKaDTbGGhUBqbmYJyJtZ+ZUQB6SmpaUmlxD2s
YmCf16qLqjxDIvmzCrQWEBBaXEGqAUBi11QTQrutLqCWmMgFYm4ogjklmKFRIV83AaX5Csg0glqUMBiXNcOlsPASR3kZ3jMwpyqAHalRmKeAshOUNkFTaqgKC8BtX5KCwqKUouLU1MU8vJ18ws08QWIizlohMRQwcXcEFRRZytkgjrmoKyQm1gCSvwgCY2U/FJQ17Y4MyU1RQE0gqKbll+Um1ii4O4cBBoiSUxPLbZSSK1IBDUkFIxNLHXMLM0gow86CuYGoIyenF2sAE7HoFaPi5mJQlV+XqoCJNsnFhTkgKpfULsOZIe1QmJJSWJyBrigzM/LqVQoyVdIVDA0BTkS1GCyRgwHgNqoqQrFGYlFqSkKnu7JRaDRBXjzqDgfVP1AihSF3FRQCZFZnAvqTID9CfNGYnpePigJ4Q0pC2hIWeAKKQtDBQ1jPdPMPAVfN18FCwMDb8ywsTC01DEyMICFjQU8bCoUjBRAfi9WqFAwNFAoyKgszkxOzFEwNTTSBQ3jgM2EjJQUK2iYGCjk5KeDFRiZmkEUwCRBoVgCar1o4gxHkBcg4ZhaUZAPSiklkIAKdzE0NzfQB7k/LSe/oKASMh4AblB4+qblggIXPHSDN6Rcw5wdg1wcg0LAlRCkZ6ebnFiUolAGGepJLCoBd9jTi/JL81LAVRCkkEVqnYLVQ/s2jmWJOfl5mYngEEpKBPX881JArRjwaIVuYkFBUX5BUSZocANqQwpkaAlkUSIoX6Zk5oJ6ehr5BYnJmSWVCgZ6RgaaOgpFmekZJbqJeckZ+UWpKToKGZkluqBKSDczDzqeg+ROyICYQlJqRmZeCjhdgd0ILi3ySvQUwKN6xdCGngLI6qRKWNIDNRcVNJJA3YOkzDwF0LiLoamJgT4oM+mX5mXn5Zfn6UCylqcniGFuCCItDHUUgt1cQP0IQx0FZ18XXQ9Qt93XRdfNRdNaISSxAJT288shuSAFNixmreCcWFQCaUlBuquJCskwET2QZAqklswr0VHwcXXRUUjKzEvJzEsvhoycJZaW5OeCS36FzJRihcSiVAXEaA+u8UG3IF2XoDDweKtzfm5ufgpoxBKcZ0uKSlMhAQEqUUCNDZCwlYKZqYERpFAoKs0rycxN1S0uzyyB5ntDfSMFX48qSG9SQSPM09FQIcBRISmzRMFUU0ehpDxfIczTsVghsURBxdDCwEBfxdDZwEBHAZKEFdxcnMFSRqDxVBUjNzc3BY2i1PRihdzMoiJQZCuoKRhrgodAFMxMjczAik3Ais0RitUUDCoM3DR1FIyNvJ0UXPyDFYL8fcFKLQxAloFEQUNkcENBrjGwMDCwVkApM92dgyAeBWVxWApB8ZORpg5ojBbcUQOVuZAS0BrUtVPITc3NTE0GjZqZW+gaWlqiVh6QYSNQT1kX1PEuLkktUCgrVqgwMylOhlgKTpgWIZUFqbaggFfQSAI3RTNL8lKLQa1BWPpX8PF3dAG3ZItB1SC4tgIV1aCulYu5If6KBBz1xgrIUW9hqIAt6i0MoVGfWKKQllmRmqIAjmkdBQtvJ/CAIzgQwVFh6ObmBokjkBgkekzB0VOQk5iZB4m54tTcxLySzORiBfA4KaieBlUyeYngIdD8osTknFRNeMIAGWQGNhwUzzhi1ho8PpyaV5JZlAqqfjKK8kvTM8B53tnTUUEDNHKclpiTA7IL1ChF9KtANoOUuPk4uoMGIFM1rcFFN6g6UHCxMFTIBY+ZlyCZCE2xJZUFqbpGoDGCFAXQUBZa3BuZWOoamZqBE0hyZqKhqYVhil4yRUkBVGHhSgogp8KTAxExbwoOseDUoszEHAWn0mKFgKL8kvzkfHALGpTl4eNTxRmgcRRIxaOQmKeQCGonpuqCuqigeZliiBFJpdD2gmOIn46Cs4+3jgJofgVSPgUHBYLDFlQWQwa/SzIS4T0HcJkKSrf5BalF4FKsGBxq+UnFqUVl4EYFyHSwvZkloIEPjAkPvGObCrFgv4LMS
M1LgYzmKxRB2/G4nAuqhRAeLc8sSk3R9Q8C+6IYMfQCHvdLARcEKZlF4MqyEuz2tMycVAUk/6Sn5oH8lqqA5ClQ6IFmPyAhCqrlwDM/oBQJcmtxZjp4bDi/tCg5lQwrYVMokDIlOb8I5D4FUJ2jgJyaQa4AJV+k8SCXoDBQZQduDSCXDyag1g9m1WBiACrunUHTdwaakHpDoSyxKDMxr8RKIRM0Sgpu+ICiWQHc/VQoyS/Iz8lPr1TQAFcp2gqwclkbXE+YmRoZQSoLbQVDM7SC3NnAwEAT4idQSKXkF4PdamxkamxgpG2sa2CoB6qxoWU/UfkNWvKAZ0VwFcMgT6LkPfD0G2QUDzocrQAvzhNLFMCjk9DAAA+wFiuABtUUjHwVwAN84FmhYh2FgpxSSGPA2zXIz9FH19nHP9hVAV7Cg7IF0dkaFMKenpjRZgia78JWspuAZYiMK3hvABLkhpmZukamhpZmFroGxshBDk4Mrgqg4lEhE9z9MNKkXjQY4a4NSQorH1cXcECBxpbBUYnIhT6uLqB2D3h0GyIFbjdCG2bQAhCUXX1cXRSKwdkzBTJpiyjV1EGTf45GChqgRo4meKxBwQncGDIGTXyWKCRVgmsnUJTB2ymgmR5XhZzUlHjQKEppMaQxrwkaQ0VMOOSnwUuzkqLEtLRM3DPAkBaer7+Lqw/Yr+B2BcRQaJsGNNJWkFOamwSuGPNSdEvydVPzUkDed4S0j8EDpwqpeaW5ChqOScWgjgp4GMpaAZRddJMT8/LzwNm7LDGnNLUY1CQ2ALWETcDtYRMjW5APdVHbyJoKoAFO6EQ3KBMXQOseBQ3notTEklRX6NxIcGpxcWZ+HnTYTw+UhOPBMRIP7kjoQCa0HcGdT9AiA5eSfD0keU14QeeSmpZYmlMCMzcI0nx1SwTNw0MKbGczE9CqA3AQBWWmO5VmgqcswR1ZsKBLanFyUWZBSX4RTBt06DAvHRyToHkfsKugmT4Frl4PNIFRnAket0yEZHa0sANbAG7wgcbJU9NTi0DDT6BZer3MvExwby4sMzk1OLWkBNTo11OA9L5SIL4CdY9BoWwNrp2TikGTI/lF4JkAyNyYQh6o95+TWZVaDOqZg9QqFOcjKuPMYkR3QQEUNDkKidBkkgnrH6Wm4O1CBoXBkpkRJN1A9UPDKD8P3sXXCPUE5wCwT3QUchJBPZVUyJIQULsD3ncERySoG5SiAGl8gPuFoG6wLiScIWaDZ00S4Y2J4NQSeDxChqjh5RZEF6hxnJiSVQqah4E02hXAlXVaZk4JqIsK6o6DVUImi2DuU0gET54WK+iC3WUBKt70IWxLEBuiOrc0pySzuLK4JDUXUclFOvr6wLUn5inkF4ByUGIONIggk5j5edB0g+hSF0PMhHgTnFNTi0DJCByJ8MSBlHZAXitKLS7JL0oF97OKSxKLSkoLCEQcaP0NuHhAFIWgEIRGY0l+enoOYk4b1FoAB4B6sUKoJ1LEYGjRU3DNA7XdUhRsIdN5iCUQkKYldGgK0gbQDvN01AbV2eBK0tPVWdMaNL4FMUADmtA1FWwVckAjAOWpIFKhOBNcTqSm6CeVpqWlgkaUwG7TU/AFdxpBZbCzK6hLplual1kC8RHIoZByABSk+gpumSmpOaDSHCQBGfsAdcecoCbqKYASJbS/q5CRn5dfBJpAVdBITwSN5YGKJEgqUEjOzy9KycwDz+mCGlSaOqAxdVCxo5CflgbKcKkV4Nm7dOw5D180uTv6ugY4Qmosn9S0EsiQqoI2ZFAXmnK8/CONFDT8g3RzU4vSU1MUCopSdYP9nV3Aq1WQdIGSCXgwGDK+CQ4yBZBmHQWEZug

... (output truncated) ...

JLSAoXEvBQFn9T0xByF4PzSouTU4liNIH9fXbAkTHVAUWZZYnKlQkB+TmZyJchrYL4uhA8OFcei5IzMktTkktIiWJpBForVQObBzS1NyslMVnAM8FQISk1LLUrNS06N1QgAi+o6BngiqdMNLk0Cu9W3tCQRlO4UAktTSyGqg0uTYCrDMlNS88HqHEtTMvMVXFKT80sLcsDRD5bTBYvrIsRhOoNLEktSFcIz81LyyxU0XFKLM9PzFAKK8gvyixNzNGM1wPK6EHmYHs+SVFDA5+cpBOUnpuQmFsRqwIV0oUIwtWGezrpGBgplxeB4UfDMLcgB5xiIZ0CuKgFHlpGBblmxLjjWwIIw/Y6eCs75Kalgv0GTlkJQKiipKWgkZobmZZZoxmo4eupCxGC6glLTM4tLiioV8tMUfPKLUkHJGiKim5+mCxIBxx5yWocmaEQ2Rsl0SAkcoQIlA2IkeLcghYik/AoFp6LEvBRQJtZwtlXIyU/PV0gsKSnKTCoFRadmrIZbkC5InS5MHcwP8JIBl0PgCvC7A1RaoBgRqwESQtEEs7PCzKQ4WcE3PyU1B14gRIDEdMFiiPIAXDpCVCMFooJzfllqUWJ6aqwGRBeSnC5MDmYXcn5VcE1JT9V1TixOVQgJUnBKTM5Oy8zJidWAqoGkC7ga3ZAgXZgamGmglAPNygq+iXmZaanFkITlqgvJ4LowUXDMB5fkF0ESFSjsoXEPYir4ZiYX5Rfnp5UoQNSAs2VxBjgrgRTogoV1EcIwF4DFFXwywWWzgnN+QSUo9+QXpepCxXRBYjDV7gE+oEyBbhskGcdquAf4QC0KTgXXN2ZoKRxim19qBcidqQXFsKIVLK4LEtcNLkktKEYtZsGyoOydUppcouCZkppXklkCd2dAUT5IXBcmDnOro4s/LDAAyyxIzcnMS4UUorEaji7+cGeWlBaAgxZepEFD1TFF1yM/WcE3MTkDpBO5QFSIdPT1UQhOzkjNTVTQKDPUjNWAKtNFVqYLUQFzjnNRfnGxbkBOYklaflGuQqinQnhmUWpaUWIuqBRHldVFSMG0u6QWZ5fkF4D0wZ2qoAEp4Yp1FBzLEnPy8zITNWNBpSFIJbT0K9YNBZXMAFBLAwQUAAYACAAvQQpduuM3oC8AAAAtAAAAEQAAAGdpdGh1Yi9fRm9vdGVyLm1kc0/NSy1KLElNUUgrys9V8HUOUChKLSzNLErNTc0rKVYoz8zOVEitKMgvKtHjAgBQSwECFAAUAAYACAAvQQpdyqWUKKQCAAAWBQAAJQAAAAAAAAAAAAAAAAAAAAAAYXp1cmUvLm1jcC1yZXF1aXJlbWVudHMtbWFuaWZlc3QuanNvblBLAQIUABQABgAIAC9BCl3ur2xoWgQAADAIAAANAAAAAAAAAAAAAAAAAOcCAABhenVyZS9Ib21lLm1kUEsBAhQAFAAGAAgAL0EKXTgMk9mnZQAAnsYAACAAAAAAAAAAAAAAAAAAbAcAAGF6dXJlL0Z1bmN0aW9uYWwtUmVxdWlyZW1lbnRzLm1kUEsBAhQAFAAGAAgAL0EKXaXz4J+vWQAA3NIAAB8AAAAAAAAAAAAAAAAAUW0AAGF6dXJlL1RlY2huaWNhbC1SZXF1aXJlbWVudHMubWRQSwECFAAUAAYACAAvQQpd4RZWatM3AACdZwAAHQAAAAAAAAAAAAAAAAA9xwAAYXp1cmUvVGVzdGluZy1SZXF1aXJlbWVudHMubWRQSwECFAAUAAYACAAvQQpdjBECv2YHAADNFwAAGgAAAAAAAAAAAAAAAABL/wAAYXp1cmUvVFItcGVyLUZSLU1hcHBpbmcubWRQSwECFAAUAAYACAAvQQpdF94P3OAMAAC9aQAAHAAAAAAAAAAAAAAAAADpBgEAYXp1cmUvUmVxdWlyZW1lbnRzLU1hdHJpeC5tZFBLAQIUABQABgAIAC9BCl23IxXcRSgAAGdFAAAZAAAAAAAAAAAAAAAAAAMUAQBhe
nVyZS9Qcm9qZWN0LU92ZXJ2aWV3Lm1kUEsBAhQAFAAGAAgAL0EKXRwHoUlnLAAAeUwAABMAAAAAAAAAAAAAAAAAfzwBAGF6dXJlL1VzZXItR3VpZGUubWRQSwECFAAUAAYACAAvQQpdfYgS510aAADeLgAAFwAAAAAAAAAAAAAAAAAXaQEAYXp1cmUvVklDRS1NaWdyYXRpb24ubWRQSwECFAAUAAYACAAvQQpdh5nfh6MOAACzFwAAEgAAAAAAAAAAAAAAAACpgwEAYXp1cmUvUk9NLVNldHVwLm1kUEsBAhQAFAAGAAgAL0EKXSSux/N0BwAAuQsAABsAAAAAAAAAAAAAAAAAfJIBAGF6dXJlL0ZsYXNoLUNhcnQtQnVpbGRlci5tZFBLAQIUABQABgAIAC9BCl0hx556RwsAAHkRAAAXAAAAAAAAAAAAAAAAACmaAQBhenVyZS9Qcml2YWN5LVBvbGljeS5tZFBLAQIUABQABgAIAC9BCl2ou7Zf1A0AAMMVAAAeAAAAAAAAAAAAAAAAAKWlAQBhenVyZS9YYm94LVN0b3JlLVB1Ymxpc2hpbmcubWRQSwECFAAUAAYACAAvQQpd2jmdLSwSAAAfHwAAGwAAAAAAAAAAAAAAAAC1swEAYXp1cmUvU3RvcmUtTGlzdGluZy1Db3B5Lm1kUEsBAhQAFAAGAAgAL0EKXSoikjHXDgAAyxcAACIAAAAAAAAAAAAAAAAAGsYBAGF6dXJlL0dQTC1TdG9yZS1TZWN0aW9uNi1SZXZpZXcubWRQSwECFAAUAAYACAAvQQpdE043OPYiAAAWRgAAHwAAAAAAAAAAAAAAAAAx1QEAYXp1cmUvU3RvcmUtTmV4dC1TdGVwcy1HdWlkZS5tZFBLAQIUABQABgAIAC9BCl2m7J8uGgYAAJIKAAAfAAAAAAAAAAAAAAAAAGT4AQBhenVyZS9TdG9yZS1Qcm9kdWN0LUlkZW50aXR5Lm1kUEsBAhQAFAAGAAgAL0EKXWCeAGclBwAAlAwAABgAAAAAAAAAAAAAAAAAu/4BAGF6dXJlL0FETy1TdG9yZS1TZXR1cC5tZFBLAQIUABQABgAIAC9BCl0yduUY6hkAAIYrAAAVAAAAAAAAAAAAAAAAABYGAgBhenVyZS9BcmNoaXRlY3R1cmUubWRQSwECFAAUAAYACAAvQQpdU6FVZ0UuAABZbAAAEwAAAAAAAAAAAAAAAAAzIAIAYXp1cmUvUHVibGljLUFQSS5tZFBLAQIUABQABgAIAC9BCl33KD7ieQ0AABcVAAAPAAAAAAAAAAAAAAAAAKlOAgBhenVyZS9QdWJTdWIubWRQSwECFAAUAAYACAAvQQpdIjg757QcAADIMgAAHwAAAAAAAAAAAAAAAABPXAIAYXp1cmUvVmlkZW8tQXVkaW8tRGVjb3VwbGluZy5tZFBLAQIUABQABgAIAC9BCl17cR7mFRIAAOIhAAAVAAAAAAAAAAAAAAAAAEB5AgBhenVyZS9TdGF0ZS1XaW5kb3cubWRQSwECFAAUAAYACAAvQQpdN2mWtWQPAACiFQAAGgAAAAAAAAAAAAAAAACIiwIAYXp1cmUvSXRlcmF0aW9uLVJvYWRtYXAubWRQSwECFAAUAAYACAAvQQpdoiCxImwRAAByHQAAHAAAAAAAAAAAAAAAAAAkmwIAYXp1cmUvVklDMjAtdnMtVklDRS1BdWRpdC5tZFBLAQIUABQABgAIAC9BCl1QzmSTVgsAAC0RAAASAAAAAAAAAAAAAAAAAMqsAgBhenVyZS9BSS1SZXZpZXcubWRQSwECFAAUAAYACAAvQQpdJVQ0YrtPAAAKmwAAGQAAAAAAAAAAAAAAAABQuAIAYXp1cmUvUmVnaXN0cnktb2YtTG9yZS5tZFBLAQIUABQABgAIAC9BCl31Pm2aWRcAAME0AAApAAAAAAAAAAAAAAAAAEIIAwBhenVyZS9GdW5jdGlvbmFsLVJlc
XVpcmVtZW50cy1PdmVydmlldy5tZFBLAQIUABQABgAIAC9BCl044SGAFwYAAIUJAAAZAAAAAAAAAAAAAAAAAOIfAwBhenVyZS9GUi1YYm94LUJyYW5kaW5nLm1kUEsBAhQAFAAGAAgAL0EKXW5lPfvKCgAAEBMAACgAAAAAAAAAAAAAAAAAMCYDAGF6dXJlL1RlY2huaWNhbC1SZXF1aXJlbWVudHMtT3ZlcnZpZXcubWRQSwECFAAUAAYACAAvQQpdVL3wEt8VAABRMwAAGgAAAAAAAAAAAAAAAABAMQMAYXp1cmUvVGVzdC1SZXF1aXJlbWVudHMubWRQSwECFAAUAAYACAAvQQpdc2cfDH0IAACaEwAAGwAAAAAAAAAAAAAAAABXRwMAYXp1cmUvWDY0c2MtTW9kZWwtTWF0cml4Lm1kUEsBAhQAFAAGAAgAL0EKXWW5vGPPEwAAACcAACMAAAAAAAAAAAAAAAAADVADAGF6dXJlL1g2NHNjLVJlcXVpcmVtZW50LUNvdmVyYWdlLm1kUEsBAhQAFAAGAAgAL0EKXWl9Anc3LwAAt3AAACsAAAAAAAAAAAAAAAAAHWQDAGF6dXJlL0NsYXNzaWMtVklDRS1FZGdlLUNhc2UtVFItQmFja2ZpbGwubWRQSwECFAAUAAYACAAvQQpdSXw0JWUHAAC5FAAAHQAAAAAAAAAAAAAAAACdkwMAYXp1cmUvVklDRS1Tb3VyY2UtTWFuaWZlc3QubWRQSwECFAAUAAYACAAvQQpd/Y+Sek4TAACzKQAAJAAAAAAAAAAAAAAAAAA9mwMAYXp1cmUvTWFjaGluZS1BcmNoaXRlY3R1cmUtU2NoZW1hLm1kUEsBAhQAFAAGAAgAL0EKXYU85R4aDgAA7BUAACIAAAAAAAAAAAAAAAAAza4DAGF6dXJlL0Nyb3NzLVBsYXRmb3JtLVdpcmVmcmFtZXMubWRQSwECFAAUAAYACAAvQQpdrWAtVBIMAACSHQAAGwAAAAAAAAAAAAAAAAAnvQMAYXp1cmUvRGVza3RvcC1XaW5kb3dzLVVJLm1kUEsBAhQAFAAGAAgAL0EKXYMtvI5KAAAAbwAAABEAAAAAAAAAAAAAAAAAcskDAGF6dXJlL0hvbWUvLm9yZGVyUEsBAhQAFAAGAAgAL0EKXTDODoFZAAAAVwAAABwAAAAAAAAAAAAAAAAA68kDAGF6dXJlL0dldHRpbmctU3RhcnRlZC8ub3JkZXJQSwECFAAUAAYACAAvQQpdWkihsoAAAACEAAAAGQAAAAAAAAAAAAAAAAB+ygMAYXp1cmUvQXJjaGl0ZWN0dXJlLy5vcmRlclBLAQIUABQABgAIAC9BCl0k8uKLmQAAAMoAAAAZAAAAAAAAAAAAAAAAADXLAwBhenVyZS9SZXF1aXJlbWVudHMvLm9yZGVyUEsBAhQAFAAGAAgAL0EKXSI/SzlpAAAAgQAAABsAAAAAAAAAAAAAAAAABcwDAGF6dXJlL1N0b3JlLWFuZC1YYm94Ly5vcmRlclBLAQIUABQABgAIAC9BCl0T94EISwAAAEkAAAAWAAAAAAAAAAAAAAAAAKfMAwBhenVyZS9SZWZlcmVuY2UvLm9yZGVyUEsBAhQAFAAGAAgAL0EKXai3+wVKAAAASAAAAAwAAAAAAAAAAAAAAAAAJs0DAGF6dXJlLy5vcmRlclBLAQIUABQABgAIAC9BCl0YDfHlpQIAABcFAAAmAAAAAAAAAAAAAAAAAJrNAwBnaXRodWIvLm1jcC1yZXF1aXJlbWVudHMtbWFuaWZlc3QuanNvblBLAQIUABQABgAIAC9BCl3ur2xoWgQAADAIAAAOAAAAAAAAAAAAAAAAAIPQAwBnaXRodWIvSG9tZS5tZFBLAQIUABQABgAIAC9BCl04DJPZp2UAAJ7GAAAhAAAAAAAAAAAAAAAAAAnVAwBnaXRodWIvRnVuY3Rpb25hbC1SZXF1aXJlbWVud
HMubWRQSwECFAAUAAYACAAvQQpdpfPgn69ZAADc0gAAIAAAAAAAAAAAAAAAAADvOgQAZ2l0aHViL1RlY2huaWNhbC1SZXF1aXJlbWVudHMubWRQSwECFAAUAAYACAAvQQpd4RZWatM3AACdZwAAHgAAAAAAAAAAAAAAAADclAQAZ2l0aHViL1Rlc3RpbmctUmVxdWlyZW1lbnRzLm1kUEsBAhQAFAAGAAgAL0EKXYwRAr9mBwAAzRcAABsAAAAAAAAAAAAAAAAA68wEAGdpdGh1Yi9UUi1wZXItRlItTWFwcGluZy5tZFBLAQIUABQABgAIAC9BCl0X3g/c4AwAAL1pAAAdAAAAAAAAAAAAAAAAAIrUBABnaXRodWIvUmVxdWlyZW1lbnRzLU1hdHJpeC5tZFBLAQIUABQABgAIAC9BCl23IxXcRSgAAGdFAAAaAAAAAAAAAAAAAAAAAKXhBABnaXRodWIvUHJvamVjdC1PdmVydmlldy5tZFBLAQIUABQABgAIAC9BCl0cB6FJZywAAHlMAAAUAAAAAAAAAAAAAAAAACIKBQBnaXRodWIvVXNlci1HdWlkZS5tZFBLAQIUABQABgAIAC9BCl19iBLnXRoAAN4uAAAYAAAAAAAAAAAAAAAAALs2BQBnaXRodWIvVklDRS1NaWdyYXRpb24ubWRQSwECFAAUAAYACAAvQQpdh5nfh6MOAACzFwAAEwAAAAAAAAAAAAAAAABOUQUAZ2l0aHViL1JPTS1TZXR1cC5tZFBLAQIUABQABgAIAC9BCl0krsfzdAcAALkLAAAcAAAAAAAAAAAAAAAAACJgBQBnaXRodWIvRmxhc2gtQ2FydC1CdWlsZGVyLm1kUEsBAhQAFAAGAAgAL0EKXSHHnnpHCwAAeREAABgAAAAAAAAAAAAAAAAA0GcFAGdpdGh1Yi9Qcml2YWN5LVBvbGljeS5tZFBLAQIUABQABgAIAC9BCl2ou7Zf1A0AAMMVAAAfAAAAAAAAAAAAAAAAAE1zBQBnaXRodWIvWGJveC1TdG9yZS1QdWJsaXNoaW5nLm1kUEsBAhQAFAAGAAgAL0EKXdo5nS0sEgAAHx8AABwAAAAAAAAAAAAAAAAAXoEFAGdpdGh1Yi9TdG9yZS1MaXN0aW5nLUNvcHkubWRQSwECFAAUAAYACAAvQQpdKiKSMdcOAADLFwAAIwAAAAAAAAAAAAAAAADEkwUAZ2l0aHViL0dQTC1TdG9yZS1TZWN0aW9uNi1SZXZpZXcubWRQSwECFAAUAAYACAAvQQpdE043OPYiAAAWRgAAIAAAAAAAAAAAAAAAAADcogUAZ2l0aHViL1N0b3JlLU5leHQtU3RlcHMtR3VpZGUubWRQSwECFAAUAAYACAAvQQpdpuyfLhoGAACSCgAAIAAAAAAAAAAAAAAAAAAQxgUAZ2l0aHViL1N0b3JlLVByb2R1Y3QtSWRlbnRpdHkubWRQSwECFAAUAAYACAAvQQpdYJ4AZyUHAACUDAAAGQAAAAAAAAAAAAAAAABozAUAZ2l0aHViL0FETy1TdG9yZS1TZXR1cC5tZFBLAQIUABQABgAIAC9BCl0yduUY6hkAAIYrAAAWAAAAAAAAAAAAAAAAAMTTBQBnaXRodWIvQXJjaGl0ZWN0dXJlLm1kUEsBAhQAFAAGAAgAL0EKXVOhVWdFLgAAWWwAABQAAAAAAAAAAAAAAAAA4u0FAGdpdGh1Yi9QdWJsaWMtQVBJLm1kUEsBAhQAFAAGAAgAL0EKXfcoPuJ5DQAAFxUAABAAAAAAAAAAAAAAAAAAWRwGAGdpdGh1Yi9QdWJTdWIubWRQSwECFAAUAAYACAAvQQpdIjg757QcAADIMgAAIAAAAAAAAAAAAAAAAAAAKgYAZ2l0aHViL1ZpZGVvLUF1ZGlvLURlY291cGxpbmcubWRQSwECFAAUAAYACAAvQQpde3Ee5hUSAADiIQAAFgAAAAAAAAAAAAAAAADyR
gYAZ2l0aHViL1N0YXRlLVdpbmRvdy5tZFBLAQIUABQABgAIAC9BCl03aZa1ZA8AAKIVAAAbAAAAAAAAAAAAAAAAADtZBgBnaXRodWIvSXRlcmF0aW9uLVJvYWRtYXAubWRQSwECFAAUAAYACAAvQQpdoiCxImwRAAByHQAAHQAAAAAAAAAAAAAAAADYaAYAZ2l0aHViL1ZJQzIwLXZzLVZJQ0UtQXVkaXQubWRQSwECFAAUAAYACAAvQQpdUM5kk1YLAAAtEQAAEwAAAAAAAAAAAAAAAAB/egYAZ2l0aHViL0FJLVJldmlldy5tZFBLAQIUABQABgAIAC9BCl0lVDRiu08AAAqbAAAaAAAAAAAAAAAAAAAAAAaGBgBnaXRodWIvUmVnaXN0cnktb2YtTG9yZS5tZFBLAQIUABQABgAIAC9BCl31Pm2aWRcAAME0AAAqAAAAAAAAAAAAAAAAAPnVBgBnaXRodWIvRnVuY3Rpb25hbC1SZXF1aXJlbWVudHMtT3ZlcnZpZXcubWRQSwECFAAUAAYACAAvQQpdOOEhgBcGAACFCQAAGgAAAAAAAAAAAAAAAACa7QYAZ2l0aHViL0ZSLVhib3gtQnJhbmRpbmcubWRQSwECFAAUAAYACAAvQQpdbmU9+8oKAAAQEwAAKQAAAAAAAAAAAAAAAADp8wYAZ2l0aHViL1RlY2huaWNhbC1SZXF1aXJlbWVudHMtT3ZlcnZpZXcubWRQSwECFAAUAAYACAAvQQpdVL3wEt8VAABRMwAAGwAAAAAAAAAAAAAAAAD6/gYAZ2l0aHViL1Rlc3QtUmVxdWlyZW1lbnRzLm1kUEsBAhQAFAAGAAgAL0EKXXNnHwx9CAAAmhMAABwAAAAAAAAAAAAAAAAAEhUHAGdpdGh1Yi9YNjRzYy1Nb2RlbC1NYXRyaXgubWRQSwECFAAUAAYACAAvQQpdZbm8Y88TAAAAJwAAJAAAAAAAAAAAAAAAAADJHQcAZ2l0aHViL1g2NHNjLVJlcXVpcmVtZW50LUNvdmVyYWdlLm1kUEsBAhQAFAAGAAgAL0EKXWl9Anc3LwAAt3AAACwAAAAAAAAAAAAAAAAA2jEHAGdpdGh1Yi9DbGFzc2ljLVZJQ0UtRWRnZS1DYXNlLVRSLUJhY2tmaWxsLm1kUEsBAhQAFAAGAAgAL0EKXUl8NCVlBwAAuRQAAB4AAAAAAAAAAAAAAAAAW2EHAGdpdGh1Yi9WSUNFLVNvdXJjZS1NYW5pZmVzdC5tZFBLAQIUABQABgAIAC9BCl39j5J6ThMAALMpAAAlAAAAAAAAAAAAAAAAAPxoBwBnaXRodWIvTWFjaGluZS1BcmNoaXRlY3R1cmUtU2NoZW1hLm1kUEsBAhQAFAAGAAgAL0EKXYU85R4aDgAA7BUAACMAAAAAAAAAAAAAAAAAjXwHAGdpdGh1Yi9Dcm9zcy1QbGF0Zm9ybS1XaXJlZnJhbWVzLm1kUEsBAhQAFAAGAAgAL0EKXa1gLVQSDAAAkh0AABwAAAAAAAAAAAAAAAAA6IoHAGdpdGh1Yi9EZXNrdG9wLVdpbmRvd3MtVUkubWRQSwECFAAUAAYACAAvQQpdQe4x7FQEAAAgCAAAEgAAAAAAAAAAAAAAAAA0lwcAZ2l0aHViL19TaWRlYmFyLm1kUEsBAhQAFAAGAAgAL0EKXbrjN6AvAAAALQAAABEAAAAAAAAAAAAAAAAAuJsHAGdpdGh1Yi9fRm9vdGVyLm1kUEsFBgAAAABXAFcA7hgAABacBwAAAA==
    contentType: application/zip
    fileName: requirements-wiki-documents.zip
    files: []
    format: wiki
    docType: all
    generatedAt: 2026-08-10T13:09:31.4067522+00:00
  deprecated: true

---


### Turn 81 (Function)
[tool_response]
<task-id>call-2027b0a4-de12-476f-8807-bcae473af8b1-60</task-id>
<task-type>bash</task-type>
<output-file>C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-2027b0a4-de12-476f-8807-bcae473af8b1-60.log</output-file>
<status>running</status>
<summary>Command "Set-Location 'F:\GitHub\vice-sharp'
$log = 'docs/receipts/wrapup-flashcart-vic20video-20260810.txt'
New-Item -ItemType Directory -Force -Path docs/receipts | Out-Null
$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart|FullyQualifiedName~Vic20VideoTests" --no-restore 2>&1 | Tee-Object -FilePath $log
$exit = $LASTEXITCODE
$sw.Stop()
"EXIT=$exit ELAPSED_MS=$($sw.ElapsedMilliseconds)" | Tee-Object -FilePath $log -Append
exit $exit
" exceeded the default timeout and was automatically moved to background. Process is still running.</summary>
Use get_command_or_subagent_output tool with task_ids=["call-2027b0a4-de12-476f-8807-bcae473af8b1-60"] to retrieve the output.

### Turn 82 (Assistant)
Writing the requirements wiki zip from the export, then waiting on the focused tests.
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
# Re-export to a file we can decode cleanly
$out = pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.requirements.generateDocument' -Params "format: wiki`ndocType: all" -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' -TimeoutSeconds 180 2>&1 | Out-String
# Parse contentBase64 from YAML-ish output
if ($out -match 'contentBase64:\s*(\S+)') {
  $b64 = $Matches[1]
  $bytes = [Convert]::FromBase64String($b64)
  $dest = 'docs/requirements/requirements-wiki-documents.zip'
  [IO.Directory]::CreateDirectory((Split-Path $dest)) | Out-Null
  [IO.File]::WriteAllBytes((Join-Path (Get-Location) $dest), $bytes)
  $fi = Get-Item $dest
  "WROTE $dest size=$($fi.Length) generatedOk"
  # spot-check zip has Flash-Cart-Builder
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $z = [IO.Compression.ZipFile]::OpenRead($fi.FullName)
  try {
    $names = $z.Entries | Select-Object -ExpandProperty FullName
    $names | Where-Object { $_ -match 'Flash-Cart|VIC20-vs-VICE|Home.md' } | Select-Object -First 20
    "entryCount=$($names.Count)"
  } finally { $z.Dispose() }
} else {
  'FAILED to find contentBase64'
  $out.Substring(0, [Math]::Min(500, $out.Length))
}

- description: Decode wiki zip to requirements path
[tool_request: get_command_or_subagent_output]
- task_ids: ["call-2027b0a4-de12-476f-8807-bcae473af8b1-60"]
- timeout_ms: 300000

### Turn 83 (Function)
[tool_response]
=== Task call-2027b0a4-de12-476f-8807-bcae473af8b1-60 ===
Command: Set-Location 'F:\GitHub\vice-sharp'
$log = 'docs/receipts/wrapup-flashcart-vic20video-20260810.txt'
New-Item -ItemType Directory -Force -Path docs/receipts | Out-Null
$sw = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart|FullyQualifiedName~Vic20VideoTests" --no-restore 2>&1 | Tee-Object -FilePath $log
$exit = $LASTEXITCODE
$sw.Stop()
"EXIT=$exit ELAPSED_MS=$($sw.ElapsedMilliseconds)" | Tee-Object -FilePath $log -Append
exit $exit

Status: completed
Duration: 22.93s
Exit Code: 0
Output File: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\019fd0df-383d-7fc0-9f0a-69785afd78ed\terminal\call-2027b0a4-de12-476f-8807-bcae473af8b1-60.log

=== Output ===
  ViceSharp.Abstractions -> F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\bin\Release\net10.0\ViceSharp.Abstractions.dll
  ViceSharp.Protocol -> F:\GitHub\vice-sharp\src\ViceSharp.Protocol\bin\Release\net10.0\ViceSharp.Protocol.dll
  ViceSharp.RomFetch -> F:\GitHub\vice-sharp\src\ViceSharp.RomFetch\bin\Release\net10.0\ViceSharp.RomFetch.dll
  ViceSharp.Chips -> F:\GitHub\vice-sharp\src\ViceSharp.Chips\bin\Release\net10.0\ViceSharp.Chips.dll
  ViceSharp.Library.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Library.ViewModels\bin\Release\net10.0\ViceSharp.Library.ViewModels.dll
  ViceSharp.Xbox.Input -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.Input\bin\Release\net10.0\ViceSharp.Xbox.Input.dll
  ViceSharp.RomM -> F:\GitHub\vice-sharp\src\ViceSharp.RomM\bin\Release\net10.0\ViceSharp.RomM.dll
  ViceSharp.Xbox.ViewModels -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\bin\Release\net10.0\ViceSharp.Xbox.ViewModels.dll
  ViceSharp.Core -> F:\GitHub\vice-sharp\src\ViceSharp.Core\bin\Release\net10.0\ViceSharp.Core.dll
  ViceSharp.Monitor -> F:\GitHub\vice-sharp\src\ViceSharp.Monitor\bin\Release\net10.0\ViceSharp.Monitor.dll
  ViceSharp.Architectures -> F:\GitHub\vice-sharp\src\ViceSharp.Architectures\bin\Release\net10.0\ViceSharp.Architectures.dll
  ViceSharp.Benchmarks -> F:\GitHub\vice-sharp\tests\ViceSharp.Benchmarks\bin\Release\net10.0\ViceSharp.Benchmarks.dll
  ViceSharp.Launcher -> F:\GitHub\vice-sharp\src\ViceSharp.Launcher\bin\Release\net10.0\ViceSharp.Launcher.dll
  ViceSharp.Host.InProcess -> F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\bin\Release\net10.0\ViceSharp.Host.InProcess.dll
  ViceSharp.AdhocHelper -> F:\GitHub\vice-sharp\src\ViceSharp.AdhocHelper\bin\Release\net10.0\ViceSharp.AdhocHelper.dll
  ViceSharp.Xbox -> F:\GitHub\vice-sharp\src\ViceSharp.Xbox\bin\Release\net10.0\ViceSharp.Xbox.dll
  ViceSharp.Console -> F:\GitHub\vice-sharp\src\ViceSharp.Console\bin\Release\net10.0\ViceSharp.Console.dll
  ViceSharp.Host -> F:\GitHub\vice-sharp\src\ViceSharp.Host\bin\Release\net10.0\ViceSharp.Host.dll
  ViceSharp.Avalonia -> F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\bin\Release\net10.0\ViceSharp.Avalonia.dll
  ViceSharp.Host.MacOS -> F:\GitHub\vice-sharp\src\ViceSharp.Host.MacOS\bin\Release\net10.0\ViceSharp.Host.MacOS.dll
  ViceSharp.TestHarness -> F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll
  Building VICE native library
  make: Entering directory '/f/GitHub/vice-sharp/native/vice/vice/src/lib/linenoise-ng'
  make: Nothing to be done for 'all'.
  make: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src/lib/linenoise-ng'
  make: Entering directory '/f/GitHub/vice-sharp/native/vice/vice/src'
  make: Leaving directory '/f/GitHub/vice-sharp/native/vice/vice/src'
Test run for F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 572 ms - ViceSharp.TestHarness.dll (net10.0)
EXIT=0 ELAPSED_MS=22352


### Turn 84 (Function)
[tool_response]
exit: 0
WROTE docs/requirements/requirements-wiki-documents.zip size=505114 generatedOk
azure/Home.md
azure/Flash-Cart-Builder.md
azure/VIC20-vs-VICE-Audit.md
github/Home.md
github/Flash-Cart-Builder.md
github/VIC20-vs-VICE-Audit.md
entryCount=87


### Turn 85 (Assistant)
Focused tests: 42 passed, 0 failed, 0 skipped. Staging intended files, validating, then committing and pushing.
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
git diff --check 2>&1 | Select-Object -First 40
Write-Host '---DIFFCHECK-EXIT---' $LASTEXITCODE

# Stage intended production/test/docs (not probe logs)
$paths = @(
  'build/Build.cs',
  'native/vice-shim-vic20.c',
  'native/vice-shim.h',
  'src/',
  'tests/',
  'README.md',
  'HANDOFF.md',
  'docs/FlashCart-Builder.md',
  'docs/USER-GUIDE.md',
  'docs/Iteration-Roadmap.md',
  'docs/README.md',
  'docs/wiki.yaml',
  'docs/audit-vic20-vs-vice-2026-08-07.md',
  'docs/receipts/',
  'docs/requirements/requirements-wiki-documents.zip',
  'docs/receipts-vic20-ntsc-apply-stackoverflow-2026-08-07.md',
  'docs/receipts-vic20-ram-all-stackoverflow-2026-08-06.md',
  'docs/receipts-vic20-video-40x40-2026-08-07.md'
)
foreach ($p in $paths) {
  if (Test-Path $p) { git add -- $p 2>&1 | Out-Null }
}
# Ensure durable lockstep receipts if present
Get-ChildItem docs -File -Filter 'receipts-lockstep*.txt' -ErrorAction SilentlyContinue | ForEach-Object { git add -- $_.FullName 2>&1 | Out-Null }

Write-Host '---STAGED STAT---'
git diff --cached --stat 2>&1 | Select-Object -Last 40
Write-Host '---STAGED COUNT---'
(git diff --cached --name-only 2>&1 | Measure-Object).Count
Write-Host '---UNSTAGED SAMPLE---'
git status --short 2>&1 | Where-Object { $_ -notmatch 'docs/(2s-|10s-|focused-|_deploy|_vs-ready|S-Blox|bcc|beq|bit|bne|bpl|cmp|inx|irq|jmp|lda|lsr|pla|adc|asl|rti|rts|sec|tay|via|vic20-video|vic20-present|vic20-ready|xvic|receipts-lockstep-|ntsc-t1)' } | Select-Object -First 50

- description: git diff --check and stage intended files

### Turn 86 (Function)
[tool_response]
exit: 0
HANDOFF.md:11: trailing whitespace.
+1. **VIC-I READY present path (Exact-scoped geometry)**  
HANDOFF.md:12: trailing whitespace.
+   - `Mos6561`: full-line paint + border blank outside paper; crop via VICE `ComputeViewportFirstX` (READY PAL first_x=48, L+R 48, paper origin 48). Rejected invent crop `src=0` and continuous paper strip.  
HANDOFF.md:13: trailing whitespace.
+   - Tests: `Vic20VideoTests` geometry/latch/space; audit rebaselined `docs/audit-vic20-vs-vice-2026-08-07.md`.  
HANDOFF.md:16: trailing whitespace.
+2. **VIC-20 expansion Settings UX**  
HANDOFF.md:17: trailing whitespace.
+   - Avalonia: `WrapPanel` BLK0/1/2/3/5; expansion cart kind/preset/write-back; Build cart image dialog.  
HANDOFF.md:18: trailing whitespace.
+   - Xbox UWP: `VariableSizedWrapGrid` for BLKs (no WrapPanel); attach/eject expansion image; navigate to FlashCartBuilder page.  
HANDOFF.md:21: trailing whitespace.
+3. **Flash Cart Image Builder (Byrd slice)**  
HANDOFF.md:22: trailing whitespace.
+   - Core: `src/ViceSharp.Core/FlashCarts/*` (profiles FE3 512K, Ultimem 1MB, MegaCart+NVRAM, EasyFlash layout).  
HANDOFF.md:23: trailing whitespace.
+   - Portable `FlashCartImageBuilderViewModel`; FE3 presets on expansion cart surface.  
HANDOFF.md:24: trailing whitespace.
+   - UI: Avalonia `FlashCartBuilderView`; Xbox `FlashCartBuilderPage`.  
HANDOFF.md:25: trailing whitespace.
+   - Tests: `tests/ViceSharp.TestHarness/FlashCart/*`.  
HANDOFF.md:26: trailing whitespace.
+   - Hostile AGREE: `docs/receipts/hostile-validator-20260808T103211Z.md`.  
HANDOFF.md:29: trailing whitespace.
+4. **Deploy evidence (local lab)**  
HANDOFF.md:30: trailing whitespace.
+   - Desktop: Nuke `InstallMsi` ProductVersion **1.2.7** succeeded (`docs/_deploy-desktop-2026-08-08_045501.log`).  
HANDOFF.md:33: trailing whitespace.
+5. **Docs refresh this turn**  
HANDOFF.md:38: trailing whitespace.
+- Exact only with VICE file+function + matching managed control + hostile AGREE when claiming Exact.  
HANDOFF.md:39: trailing whitespace.
+- Do **not** claim whole-machine Exact or pixel framebuffer lockstep vs xvic (still Missing / Partial per audit).  
HANDOFF.md:44: trailing whitespace.
+1. Optional: FE3 flash040 residual (TDD first).  
HANDOFF.md:45: trailing whitespace.
+2. Optional: pixel framebuffer compare vs xvic (`capture_visible_frame` / present path).  
HANDOFF.md:46: trailing whitespace.
+3. Optional: re-run desktop InstallMsi if Avalonia builder only was built after last MSI.  
---DIFFCHECK-EXIT--- 
---STAGED STAT---
 .../Services/HostProtocolMapper.cs                 |   8 +-
 .../Services/MediaServiceHost.cs                   |  87 +-
 .../Services/SettingsServiceHost.cs                | 224 ++++-
 src/ViceSharp.Protocol/ExpansionCartManageState.cs |  42 +
 src/ViceSharp.Protocol/ProtocolContracts.cs        |  27 +-
 src/ViceSharp.Protocol/SettingsOptionCatalog.cs    |  47 ++
 src/ViceSharp.Protocol/Vic20MemoryUiState.cs       | 160 ++++
 src/ViceSharp.Xbox.ViewModels/AboutInfo.cs         |   6 +-
 src/ViceSharp.Xbox.ViewModels/AboutViewModel.cs    |  59 +-
 .../NavigationDestination.cs                       |   5 +
 .../VideoFramePullViewModel.cs                     |  53 +-
 .../XboxSettingsViewModel.cs                       | 551 ++++++++++++-
 src/ViceSharp.Xbox/App.xaml.cs                     |  20 +-
 src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml |  41 +
 .../Views/FlashCartBuilderPage.xaml.cs             | 166 ++++
 src/ViceSharp.Xbox/Views/SettingsPage.xaml         |  45 +
 src/ViceSharp.Xbox/Views/SettingsPage.xaml.cs      | 108 ++-
 .../FlashCart/Fe3PresetTests.cs                    |  24 +
 .../FlashCartImageBuilderViewModelTests.cs         |  80 ++
 .../FlashCart/FlashImageBuilderTests.cs            | 136 +++
 .../IEC/FileSystemIecDeviceTests.cs                |  68 ++
 .../IEC/FileSystemIecUnitSettingsTests.cs          |  87 ++
 .../Ui/Vic20SettingsUiSurfaceTests.cs              |  71 ++
 .../Vic20/MediaServiceHostVic20ExpansionTests.cs   | 106 +++
 .../Vic20/Vic20BoardWiringTests.cs                 |  84 ++
 .../Vic20/Vic20DivergeProbe.cs                     |  32 +-
 .../Vic20/Vic20ExpansionCartTests.cs               | 110 +++
 .../Vic20/Vic20MemoryMapTests.cs                   |   7 +-
 .../Vic20/Vic20MemorySettingsRestartTests.cs       |  72 ++
 .../Vic20/Vic20RamBlocksTests.cs                   | 128 +++
 .../Vic20SettingsRestartPreservesExpansionTests.cs |  96 +++
 .../Vic20/Vic20VideoLockstep.cs                    | 176 ++++
 .../Vic20/Vic20VideoPumpSmokeTests.cs              |  88 ++
 .../ViceSharp.TestHarness/Vic20/Vic20VideoTests.cs | 375 ++++++++-
 .../Xbox/AboutViewModelTests.cs                    |  56 +-
 .../Xbox/Fakes/FakeXboxSettingsGateway.cs          |  21 +-
 .../Xbox/VideoFramePullViewModelTests.cs           |  32 +
 .../Xbox/XboxComputerModelPickerTests.cs           | 107 +++
 .../Xbox/XboxSettingsViewModelTests.cs             | 221 +++++
 92 files changed, 7787 insertions(+), 428 deletions(-)
---STAGED COUNT---
92
---UNSTAGED SAMPLE---
M  HANDOFF.md
M  README.md
M  build/Build.cs
A  docs/FlashCart-Builder.md
M  docs/Iteration-Roadmap.md
 M docs/Project/wiki/azure/.mcp-requirements-manifest.json
 M docs/Project/wiki/azure/Architecture/.order
 M docs/Project/wiki/azure/Functional-Requirements-Overview.md
 M docs/Project/wiki/azure/Functional-Requirements.md
 M docs/Project/wiki/azure/Getting-Started/.order
 M docs/Project/wiki/azure/Home.md
 M docs/Project/wiki/azure/Iteration-Roadmap.md
 M docs/Project/wiki/azure/Project-Overview.md
 M docs/Project/wiki/azure/Requirements-Matrix.md
 M docs/Project/wiki/azure/Requirements/.order
 M docs/Project/wiki/azure/TR-per-FR-Mapping.md
 M docs/Project/wiki/azure/Technical-Requirements.md
 M docs/Project/wiki/azure/Testing-Requirements.md
 M docs/Project/wiki/azure/User-Guide.md
 M docs/Project/wiki/github/.mcp-requirements-manifest.json
 M docs/Project/wiki/github/Functional-Requirements-Overview.md
 M docs/Project/wiki/github/Functional-Requirements.md
 M docs/Project/wiki/github/Home.md
 M docs/Project/wiki/github/Iteration-Roadmap.md
 M docs/Project/wiki/github/Project-Overview.md
 M docs/Project/wiki/github/Requirements-Matrix.md
 M docs/Project/wiki/github/TR-per-FR-Mapping.md
 M docs/Project/wiki/github/Technical-Requirements.md
 M docs/Project/wiki/github/Testing-Requirements.md
 M docs/Project/wiki/github/User-Guide.md
 M docs/Project/wiki/github/_Sidebar.md
M  docs/README.md
M  docs/USER-GUIDE.md
A  docs/audit-vic20-vs-vice-2026-08-07.md
A  docs/receipts-vic20-ntsc-apply-stackoverflow-2026-08-07.md
A  docs/receipts-vic20-ram-all-stackoverflow-2026-08-06.md
A  docs/receipts-vic20-video-40x40-2026-08-07.md
A  docs/receipts/hostile-validator-20260808T094911Z.json
A  docs/receipts/hostile-validator-20260808T094911Z.md
A  docs/receipts/hostile-validator-20260808T103211Z.json
A  docs/receipts/hostile-validator-20260808T103211Z.md
A  docs/receipts/wrapup-flashcart-vic20video-20260810.txt
M  docs/requirements/requirements-wiki-documents.zip
M  docs/wiki.yaml
M  native/vice-shim-vic20.c
M  native/vice-shim.h
M  src/ViceSharp.Abstractions/IViceNative.cs
M  src/ViceSharp.Architectures/Vic20/Vic20Descriptor.cs
M  src/ViceSharp.Architectures/Vic20/Vic20Expansion.cs
M  src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs


### Turn 87 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# Strip trailing whitespace from HANDOFF.md
$p = 'HANDOFF.md'
$lines = Get-Content -LiteralPath $p
$fixed = $lines | ForEach-Object { $_ -replace '[ \t]+$','' }
Set-Content -LiteralPath $p -Value $fixed -Encoding utf8
git add -- HANDOFF.md
# Stage regenerated Project wiki export pages
git add -- docs/Project/wiki 2>&1
git add -- docs/requirements/requirements-wiki-documents.zip
git diff --check -- HANDOFF.md 2>&1
Write-Host 'CHECK_EXIT' $LASTEXITCODE
(git diff --cached --name-only | Measure-Object).Count

- description: Fix HANDOFF whitespace; stage wiki export pages

### Turn 88 (Function)
[tool_response]
exit: 0
CHECK_EXIT 0
124


### Turn 89 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$msg = @'
feat(vic20): present-path borders, expansion UX, flash cart builder

VICE-faithful Mos6561 first_x crop (READY PAL L+R borders), wrap BLK
settings on Avalonia/Xbox, FE3/Ultimem/Mega-Cart attach plus portable
FlashCart image builder. Docs/wiki refresh and requirements wiki export.
Hostile AGREE receipts for borders and flash builder.

Co-Authored-By: Grok Code <noreply@x.ai>
Signed-off-by: model-effort@agent confidence=high
'@
git commit -m $msg 2>&1
Write-Host 'COMMIT_EXIT' $LASTEXITCODE
git rev-parse HEAD
git log -1 --format='%H%n%s%n%an <%ae>'

- description: Commit staged VIC-20 and docs wrap-up

### Turn 90 (Function)
[tool_response]
exit: 0
[main 0cd61f8] feat(vic20): present-path borders, expansion UX, flash cart builder
 124 files changed, 8531 insertions(+), 452 deletions(-)
 create mode 100644 docs/FlashCart-Builder.md
 create mode 100644 docs/Project/wiki/azure/FR-Xbox-Branding.md
 create mode 100644 docs/Project/wiki/azure/Flash-Cart-Builder.md
 create mode 100644 docs/Project/wiki/azure/VIC20-vs-VICE-Audit.md
 create mode 100644 docs/Project/wiki/github/FR-Xbox-Branding.md
 create mode 100644 docs/Project/wiki/github/Flash-Cart-Builder.md
 create mode 100644 docs/Project/wiki/github/VIC20-vs-VICE-Audit.md
 create mode 100644 docs/audit-vic20-vs-vice-2026-08-07.md
 create mode 100644 docs/receipts-lockstep-everycycle-baseline-2026-08-06.txt
 create mode 100644 docs/receipts-vic20-ntsc-apply-stackoverflow-2026-08-07.md
 create mode 100644 docs/receipts-vic20-ram-all-stackoverflow-2026-08-06.md
 create mode 100644 docs/receipts-vic20-video-40x40-2026-08-07.md
 create mode 100644 docs/receipts/hostile-validator-20260808T094911Z.json
 create mode 100644 docs/receipts/hostile-validator-20260808T094911Z.md
 create mode 100644 docs/receipts/hostile-validator-20260808T103211Z.json
 create mode 100644 docs/receipts/hostile-validator-20260808T103211Z.md
 create mode 100644 docs/receipts/wrapup-flashcart-vic20video-20260810.txt
 create mode 100644 src/ViceSharp.Avalonia/Views/FlashCartBuilderView.axaml
 create mode 100644 src/ViceSharp.Avalonia/Views/FlashCartBuilderView.axaml.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/FlashBankContentKind.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/FlashBankSlot.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/FlashCartImageBuilderViewModel.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/FlashCartProfiles.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/FlashImageBuilder.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/IFlashBuilderFileIo.cs
 create mode 100644 src/ViceSharp.Core/FlashCarts/IFlashCartProfile.cs
 create mode 100644 src/ViceSharp.Core/Iec/FileSystemIecDevice.cs
 create mode 100644 src/ViceSharp.Core/Vic20/FinalExpansion3Cartridge.cs
 create mode 100644 src/ViceSharp.Core/Vic20/IVic20ExpansionCartPort.cs
 create mode 100644 src/ViceSharp.Core/Vic20/IVic20RamConfiguration.cs
 create mode 100644 src/ViceSharp.Core/Vic20/MegaCartCartridge.cs
 create mode 100644 src/ViceSharp.Core/Vic20/UltimemCartridge.cs
 create mode 100644 src/ViceSharp.Core/Vic20/Vic20ExpansionCartKind.cs
 create mode 100644 src/ViceSharp.Core/Vic20/Vic20ExpansionCartPort.cs
 create mode 100644 src/ViceSharp.Protocol/ExpansionCartManageState.cs
 create mode 100644 src/ViceSharp.Protocol/Vic20MemoryUiState.cs
 create mode 100644 src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml
 create mode 100644 src/ViceSharp.Xbox/Views/FlashCartBuilderPage.xaml.cs
 create mode 100644 tests/ViceSharp.TestHarness/FlashCart/Fe3PresetTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/FlashCart/FlashCartImageBuilderViewModelTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/FlashCart/FlashImageBuilderTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/IEC/FileSystemIecDeviceTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/IEC/FileSystemIecUnitSettingsTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Ui/Vic20SettingsUiSurfaceTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/MediaServiceHostVic20ExpansionTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20BoardWiringTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20ExpansionCartTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20MemorySettingsRestartTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20RamBlocksTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20SettingsRestartPreservesExpansionTests.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20VideoLockstep.cs
 create mode 100644 tests/ViceSharp.TestHarness/Vic20/Vic20VideoPumpSmokeTests.cs
COMMIT_EXIT 0
0cd61f86fa99ee1e109c69ff2c494a6879c35def
0cd61f86fa99ee1e109c69ff2c494a6879c35def
feat(vic20): present-path borders, expansion UX, flash cart builder
Sharp Ninja <ninja@thesharp.ninja>


### Turn 91 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
git push origin HEAD 2>&1
Write-Host 'PUSH_EXIT' $LASTEXITCODE
git status -sb 2>&1 | Select-Object -First 5

- description: Push main commit to origin
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
$env:MCP_AGENT_NAME = 'GrokCode'

# Refresh turn state
Get-Content 'F:\GitHub\vice-sharp\.mcpServer\grok\current-turn.yaml' -ErrorAction SilentlyContinue | Select-Object -First 20
Get-Content 'F:\GitHub\vice-sharp\.mcpServer\grok\session-state.yaml' -ErrorAction SilentlyContinue | Select-Object -First 15

$dialog = @'
dialogItems:
  - timestamp: 2026-08-10T13:15:00Z
    role: model
    content: |
      Decision: Commit VIC-20 present-path, expansion UX, flash cart builder, and docs refresh as one wrap-up commit on main; exclude bulk probe logs and S-Blox.
      Consequence: origin/main gains 0cd61f8; ephemeral lockstep logs remain untracked local noise.
      Alternatives rejected: separate docs-only commit (operator asked single wrap-up); committing all 363 untracked probe logs (noise, prior handoff rule).
    category: decision
  - timestamp: 2026-08-10T13:15:10Z
    role: model
    content: |
      Observation: Focused gate FullyQualifiedName~FlashCart|Vic20VideoTests Release = 42 passed, 0 failed, 0 skipped (docs/receipts/wrapup-flashcart-vic20video-20260810.txt).
      Requirements wiki export written to docs/requirements/requirements-wiki-documents.zip (505114 bytes, includes Flash-Cart-Builder and VIC20-vs-VICE-Audit).
    category: observation
'@

pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.appendDialog' -Params $dialog -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' 2>&1 | Select-Object -Last 30

- description: Push done path; append session dialog decisions

### Turn 92 (Function)
[tool_response]
exit: 0
To https://github.com/sharpninja/vice-sharp.git
   e5203f9..0cd61f8  HEAD -> main
PUSH_EXIT 0
## main...origin/main
?? docs/10s-lockstep-2026-08-07.log
?? docs/10s-lockstep-blankfix-2026-08-06.log
?? docs/10s-lockstep-idle-2026-08-06.log
?? docs/10s-lockstep-memptr2-2026-08-06.log


### Turn 93 (Function)
[tool_response]
exit: 0
turnRequestId: req-20260810T130257Z-prompt-72b6
queryTitle: Refresh docs and wrap-up with detailed handoff
openedAt: 2026-08-10T13:02:57Z
status: in_progress
sessionId: GrokCode-20260810T130431Z-plugin-session
codeEdits: 0
lastBuildStatus: unknown
auditActions: 0
auditDialog: 0
auditDecisions: 0
auditFiles: 0
auditCommits: 0
queryText: |-
  <user_query>
  /refresh-docs and /mcpserver:wrap-up with detailed handoff
  </user_query>
  <skill_information>
  <skills_referenced>
  <skill name="refresh-docs" path="C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md"/>
  <skill name="mcpserver:wrap-up" path="C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/wrap-up\SKILL.md"/>
status: verified
sessionId: GrokCode-20260810T130431Z-plugin-session
agent: GrokCode
started: 2026-08-10T13:04:31Z
lastUpdated: 2026-08-10T13:04:31Z
markerFilePath: F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml
markerLastWriteUtc: 2026-08-09T02:03:48.7967449Z
agentSessionId: 019fd0df-383d-7fc0-9f0a-69785afd78ed
agentSessionTranscriptFile: ""
agentExecutablePath: C:\Users\kingd\.grok\bin\grok.exe
agentExecutableVersion: grok 1.0.0 (3cd0d0cbce) [stable]


### Turn 94 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
$env:MCP_AGENT_NAME = 'GrokCode'

$actions = @'
actions:
  - order: 1
    description: "Docs refresh - README, USER-GUIDE, Iteration-Roadmap, FlashCart-Builder, HANDOFF, wiki.yaml"
    type: edit
    status: completed
    filePath: docs/wiki.yaml
  - order: 2
    description: "Exported requirements wiki ZIP via workflow.requirements.generateDocument format=wiki"
    type: edit
    status: completed
    filePath: docs/requirements/requirements-wiki-documents.zip
  - order: 3
    description: "Focused tests FlashCart|Vic20VideoTests Release 42/0/0"
    type: edit
    status: completed
    filePath: docs/receipts/wrapup-flashcart-vic20video-20260810.txt
  - order: 4
    description: "Commit 0cd61f8 feat(vic20) present-path borders expansion flash cart builder docs"
    type: commit
    status: completed
    filePath: ""
  - order: 5
    description: "Pushed main to origin https://github.com/sharpninja/vice-sharp.git e5203f9..0cd61f8"
    type: commit
    status: completed
    filePath: ""
  - order: 6
    description: "Decision - single wrap-up commit on main; exclude bulk probe logs"
    type: design_decision
    status: completed
    filePath: HANDOFF.md
'@

Write-Host '---APPEND ACTIONS---'
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.appendActions' -Params $actions -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' 2>&1 | Select-Object -Last 25

$complete = @'
response: |
  Refresh-docs and wrap-up complete on main 0cd61f8 (pushed origin).
  Docs: FlashCart-Builder, README/USER-GUIDE/Roadmap/HANDOFF, wiki.yaml (+2 nav pages), requirements-wiki-documents.zip.
  Validation: FlashCart|Vic20VideoTests Release 42 passed / 0 failed / 0 skipped.
  No new FR/TR/TEST IDs created this turn (export only).
  Intentionally untracked: bulk docs/* probe logs, S-Blox, reviews, manifests.
  Residual open: FE3 flash040 Partial; pixel FB lockstep Missing.
'@

Write-Host '---COMPLETE TURN---'
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Invoke -Method 'workflow.sessionlog.completeTurn' -Params $complete -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' 2>&1 | Select-Object -Last 40

Write-Host '---TURN STATE---'
Get-Content 'F:\GitHub\vice-sharp\.mcpServer\grok\current-turn.yaml' -ErrorAction SilentlyContinue | Select-Object -First 25

- description: Append actions and complete session turn

### Turn 95 (Function)
[tool_response]
exit: 0
---APPEND ACTIONS---
---COMPLETE TURN---
---TURN STATE---
turnRequestId: req-20260810T130257Z-prompt-72b6
queryTitle: Refresh docs and wrap-up with detailed handoff
openedAt: 2026-08-10T13:02:57Z
status: completed
sessionId: GrokCode-20260810T130431Z-plugin-session
codeEdits: 6
lastBuildStatus: unknown
auditActions: 6
auditDialog: 2
auditDecisions: 1
auditFiles: 6
auditCommits: 2
queryText: |-
  <user_query>
  /refresh-docs and /mcpserver:wrap-up with detailed handoff
  </user_query>
  <skill_information>
  <skills_referenced>
  <skill name="refresh-docs" path="C:\Users\kingd\.grok\skills\refresh-docs\SKILL.md"/>
  <skill name="mcpserver:wrap-up" path="C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\./skills/wrap-up\SKILL.md"/>
  </skills_referenced>
  <skill name="refresh-docs" args="and">
  # Refresh Docs

  Reconcile `docs/` with the current codebase, remove documentation that no longer applies, make `docs/wiki.yaml` list every project doc that should be published to the wiki, and export the MCP-backed requirement documents.


### Turn 96 (Assistant)
[tool_request: todo_write]
- todos: [{"id":"3","status":"completed"},{"id":"4","status":"completed"},{"id":"5","status":"completed"},{"id":"6","status":"completed"}]
- merge: true
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
[DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss UTC')
git log -1 --oneline
git status -sb | Select-Object -First 3
# remaining untracked count
$u = (git ls-files --others --exclude-standard | Measure-Object).Count
"untracked_remaining=$u"

- description: Final timestamp, SHA, dirty summary

### Turn 97 (Function)
[tool_response]
- [completed] 1: Refresh project docs (README, USER-GUIDE, HANDOFF, roadmap, flash cart doc)
- [completed] 2: Update docs/wiki.yaml and docs/README index
- [completed] 3: MCP requirements export (wiki zip)
- [completed] 4: Validate (focused tests + git diff --check)
- [completed] 5: Commit/push intended work + session log completeTurn
- [completed] 6: Write detailed handoff report


### Turn 98 (Function)
[tool_response]
exit: 0
2026-08-10 13:11:07 UTC
0cd61f8 feat(vic20): present-path borders, expansion UX, flash cart builder
## main...origin/main
?? docs/10s-lockstep-2026-08-07.log
?? docs/10s-lockstep-blankfix-2026-08-06.log
untracked_remaining=319


### Turn 99 (Assistant)
**2026-08-10 13:11 UTC** — Profile loaded (14 profile files). `/refresh-docs` + `/mcpserver:wrap-up` complete.

## MCP trust
- Plugin: `mcpserver-grok-plugin` / agent `GrokCode`
- Status: `available`, marker verified for `F:\GitHub\vice-sharp`
- Session: `GrokCode-20260810T130431Z-plugin-session`
- Turn: `req-20260810T130257Z-prompt-72b6` — **completed** (audit: 6 actions, 2 dialog, 1 decision, 2 commits)

## Refresh-docs (verified)
**Updated**
- `README.md` — Iteration 2 present-path, flash builder, dashboard date, MSI 1.2.7 note
- `docs/USER-GUIDE.md` — VIC-20 Working core (was “Not yet”); Xbox expansion note
- `docs/Iteration-Roadmap.md` — BLK wrap, flash cart, first_x, audit pointer
- `HANDOFF.md` — full 2026-08-10 session closeout
- `docs/README.md` — FlashCart + audit + hostile receipts index
- `docs/wiki.yaml` — added `flash-cart-builder`, `audit-vic20-vs-vice` (+ nav)
- **New** `docs/FlashCart-Builder.md`

**Pruned:** none. Flagged as non-wiki noise (left untracked): bulk `docs/2s-*`, `10s-*`, `*-focused-*.log`, `xvic-*`, `_deploy-*`, `S-Blox/`, `reviews/`, `manifests/`.

**Requirements export**
- `workflow.requirements.generateDocument` `format: wiki` `docType: all` OK
- Artifact: `docs/requirements/requirements-wiki-documents.zip` (505 114 bytes, 87 entries)
- Includes `Flash-Cart-Builder.md` and `VIC20-vs-VICE-Audit.md` for azure + github
- Regenerated `docs/Project/wiki/**` kept and committed
- **No new FR/TR/TEST IDs** this turn (export only; no requirement invent)

## Validation
```
dotnet test ... --filter "FullyQualifiedName~FlashCart|FullyQualifiedName~Vic20VideoTests" -c Release
Passed!  Failed: 0, Passed: 42, Skipped: 0, Total: 42
```
Receipt: `docs/receipts/wrapup-flashcart-vic20video-20260810.txt`  
`git diff --check` clean on staged content after HANDOFF whitespace fix.

## Commit / push
- **SHA:** `0cd61f86fa99ee1e109c69ff2c494a6879c35def` (`0cd61f8`)
- **Subject:** `feat(vic20): present-path borders, expansion UX, flash cart builder`
- **Branch:** `main` → **origin** `https://github.com/sharpninja/vice-sharp.git` (`e5203f9..0cd61f8`)
- **124 files**, +8531 / −452
- **Not pushed:** Azure DevOps remote `azure` (GitHub origin only this turn)
- **Left untracked:** ~319 probe logs / noise paths

---

## Detailed handoff (resume here)

### Shipped on `main` @ `0cd61f8`
1. **READY present geometry (Exact-scoped)**  
   `Mos6561` full-line paint + border blank; crop via VICE `ComputeViewportFirstX` (PAL first_x=48, L+R borders, paper origin 48). No invent `src=0` / paper strip.  
   Hostile AGREE: `docs/receipts/hostile-validator-20260808T094911Z.md`

2. **Expansion Settings UX**  
   Avalonia `WrapPanel` BLKs + Build cart dialog; Xbox `VariableSizedWrapGrid` BLKs + attach/eject + FlashCart page.  
   Protocol/host: expansion cart state, size-detect attach, FE3 presets.

3. **Flash Cart Builder**  
   Core `FlashCarts/*` (FE3 512K, Ultimem 1MB, MegaCart+NVRAM, EasyFlash layout). Portable VM + Avalonia/Xbox hosts.  
   Hostile AGREE: `docs/receipts/hostile-validator-20260808T103211Z.md`  
   User doc: `docs/FlashCart-Builder.md`

4. **Prior still green**  
   CPU every-cycle 10s PAL/NTSC lockstep vs xvic (separate from pixel FB).

### Explicit open (do not overclaim)
| Item | Status |
|------|--------|
| FE3 flash040 program/erase FSM | **Partial** residual |
| Pixel framebuffer lockstep vs xvic | **Missing** |
| Whole-machine Exact | **Never claimed** |
| Store Partner Center / ADO var group | Still open (older handoff) |
| Optional re-`InstallMsi` if UI only rebuilt after 1.2.7 | Operator call |

### Deploy baseline (local lab, pre-this-commit packaging)
- Desktop MSI **1.2.7** InstallMsi succeeded (`docs/_deploy-desktop-2026-08-08_045501.log`)
- Xbox DeployXboxLocal used in prior session (`docs/_deploy-xbox-*`)
- GitVersion base still **1.2.2**; product build number = commits-since

### How to resume
1. Read `HANDOFF.md` (repo root) + this turn’s commit.
2. MCP: re-verify marker signature + `/health` nonce after any server restart.
3. Next product slices (TDD): FE3 flash040 residual, or pixel FB compare vs xvic.
4. If ADO package truth is needed: push/sync `main` to `azure` explicitly.
5. Do **not** bulk-commit remaining untracked probe logs.

### Design decision (logged)
Single wrap-up commit on `main` for code + durable docs + wiki export; exclude bulk ephemeral lockstep/probe logs per prior handoff policy.

### Turn 100 (Human)
<system-reminder>
MCP server updated:
- github (89 tools)

MCP servers that failed to connect:
- mcpserver (auth required)

To use MCP tools, you MUST call `search_tool` first to retrieve the tool's input schema before calling `use_tool`. NEVER guess parameter names — always use the exact schema returned by `search_tool`.
</system-reminder>

### Turn 101 (Human)
<user_query>
1. Read HANDOFF.md (repo root) + this turn’s commit.
2. MCP: re-verify marker signature + /health nonce after any server restart.
3. Next product slices (TDD): FE3 flash040 residual, or pixel FB compare vs xvic.
4. If ADO package truth is needed: push/sync main to azure explicitly.
5. Do not bulk-commit remaining untracked probe logs.
</user_query>

### Turn 102 (Assistant)
Resuming from the handoff checklist: reading `HANDOFF.md` and the wrap-up commit, re-verifying MCP trust, then checking azure and the two next-slice options.
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\HANDOFF.md
- limit: 80
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
git log -1 --format='SHA=%H%nSubject=%s%nAuthor=%an <%ae>%nDate=%ci%n'
git show --stat --oneline HEAD | Select-Object -First 40
Write-Host '---BRANCH---'
git status -sb
git rev-parse HEAD origin/main 2>$null
git rev-parse azure/main 2>$null
git log --oneline origin/main..HEAD 2>$null
git log --oneline azure/main..HEAD 2>$null | Select-Object -First 10
git log --oneline HEAD..azure/main 2>$null | Select-Object -First 5

- description: Show wrap-up commit and azure vs origin
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'
$env:MCP_AGENT_NAME = 'GrokCode'

# Plugin status (marker trust path)
Write-Host '=== STATUS ==='
pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' -Command Status -WorkspacePath 'F:\GitHub\vice-sharp' -PluginRoot 'F:\GitHub\mcpserver-grok-plugin' 2>&1

# Explicit health nonce echo (read-only diagnosis after plugin status)
Write-Host '=== HEALTH NONCE ==='
$marker = Get-Content -Raw 'AGENTS-README-FIRST.yaml'
if ($marker -match 'baseUrl:\s*(\S+)') { $base = $Matches[1].Trim() } else { throw 'no baseUrl' }
if ($marker -match 'apiKey:\s*(\S+)') { $key = $Matches[1].Trim() } else { throw 'no apiKey' }
$nonce = [guid]::NewGuid().ToString('N')
$uri = "$base/health?nonce=$nonce"
try {
  $resp = Invoke-RestMethod -Uri $uri -Headers @{ 'X-Api-Key' = $key } -TimeoutSec 15
  $echo = $null
  if ($resp -is [string]) { $echo = $resp }
  elseif ($resp.nonce) { $echo = [string]$resp.nonce }
  elseif ($resp.PSObject.Properties['nonce']) { $echo = [string]$resp.nonce }
  else { $echo = ($resp | ConvertTo-Json -Compress) }
  "nonce_sent=$nonce"
  "nonce_echo=$echo"
  "nonce_match=$($echo -eq $nonce -or ($echo -like "*$nonce*"))"
  "health_raw_type=$($resp.GetType().FullName)"
  if ($resp -isnot [string]) { $resp | ConvertTo-Json -Compress -Depth 4 }
} catch {
  "HEALTH_FAILED: $($_.Exception.Message)"
}

# Marker signature recompute (HMAC-SHA256 marker-v1)
Write-Host '=== SIGNATURE ==='
# Use plugin marker resolver if available via status; also try explicit
$sigScript = @'
$ErrorActionPreference = "Stop"
$yaml = Get-Content -Raw "AGENTS-README-FIRST.yaml"
# crude field extract for signature fields listed in marker
function Get-Field([string]$text, [string]$name) {
  if ($text -match "(?m)^${name}:\s*(.+)$") { return $Matches[1].Trim() }
  return $null
}
$apiKey = Get-Field $yaml "apiKey"
$sigValue = $null
if ($yaml -match "(?ms)signature:.*?value:\s*([A-F0-9]+)") { $sigValue = $Matches[1].Trim() }
$port = Get-Field $yaml "port"
$baseUrl = Get-Field $yaml "baseUrl"
$workspace = Get-Field $yaml "workspace"
$workspacePath = Get-Field $yaml "workspacePath"
$pid = Get-Field $yaml "pid"
$startedAt = Get-Field $yaml "startedAt"
$markerWrittenAtUtc = Get-Field $yaml "markerWrittenAtUtc"
$serverStartedAtUtc = Get-Field $yaml "serverStartedAtUtc"
# endpoints and agentPlugins from nested - read from known structure
$lines = @(
  "canonicalization=marker-v1",
  "port=$port",
  "baseUrl=$baseUrl",
  "apiKey=$apiKey",
  "workspace=$workspace",
  "workspacePath=$workspacePath",
  "pid=$pid",
  "startedAt=$startedAt",
  "markerWrittenAtUtc=$markerWrittenAtUtc",
  "serverStartedAtUtc=$serverStartedAtUtc"
)
# For full signature we need all signature.fields - use marker-resolver from plugin if present
$resolver = "F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1"
if (Test-Path $resolver) {
  . $resolver
  if (Get-Command Test-McpMarkerSignature -ErrorAction SilentlyContinue) {
    $r = Test-McpMarkerSignature -MarkerPath "AGENTS-README-FIRST.yaml"
    "Test-McpMarkerSignature=$r"
  } elseif (Get-Command Get-McpMarkerTrust -ErrorAction SilentlyContinue) {
    Get-McpMarkerTrust -MarkerPath "AGENTS-README-FIRST.yaml" | ConvertTo-Json -Compress
  } else {
    Get-Command -Name *Marker* | Select-Object Name
    "sig_declared=$sigValue"
  }
} else {
  "no marker-resolver; sig_declared=$sigValue"
}
'@
pwsh.exe -NoProfile -NonInteractive -Command $sigScript 2>&1

- description: Re-verify MCP marker signature and health nonce
[tool_request: grep]
- pattern: flash040|Flash040|FE3|FinalExpansion
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20
- glob: *.cs
- head_limit: 40
[tool_request: grep]
- pattern: capture_visible|VideoLockstep|pixel.*lockstep|framebuffer|FrameBuffer
- path: F:\GitHub\vice-sharp
- glob: *.{cs,md,c,h}
- head_limit: 40

### Turn 103 (Function)
[tool_response]
1→# ViceSharp Handoff - 2026-08-10

**Active branch:** `main` (GitVersion base **v1.2.2**; local MSI/deploy line **1.2.7** from commits-since-version). Iteration 1 C64 complete; Iteration 2 VIC-20 core + lockstep + present-path + expansion UX on tree.
**Remotes:** `origin` = GitHub `sharpninja/vice-sharp` (downstream); Azure DevOps `azure` is source of truth for package/CI when both are used. Default push target per AGENTS: `origin` unless operator says otherwise.
**Working tree noise (do not commit unless operator asks):** untracked `docs/S-Blox/`, `docs/reviews/*`, bulk `docs/2s-*`, `docs/10s-*`, `docs/*-focused-*.log`, `docs/_deploy-*`, `docs/xvic-*` probe captures, `manifests/` winget copies. Prefer durable receipts under `docs/receipts*` and `docs/receipts/`.

## Session closeout 2026-08-10 (refresh-docs + wrap-up)

### What landed on the working tree (pre-commit / commit this wrap-up)
10→
1. **VIC-I READY present path (Exact-scoped geometry)**
   - `Mos6561`: full-line paint + border blank outside paper; crop via VICE `ComputeViewportFirstX` (READY PAL first_x=48, L+R 48, paper origin 48). Rejected invent crop `src=0` and continuous paper strip.
   - Tests: `Vic20VideoTests` geometry/latch/space; audit rebaselined `docs/audit-vic20-vs-vice-2026-08-07.md`.
   - Hostile AGREE: `docs/receipts/hostile-validator-20260808T094911Z.md`.

2. **VIC-20 expansion Settings UX**
   - Avalonia: `WrapPanel` BLK0/1/2/3/5; expansion cart kind/preset/write-back; Build cart image dialog.
   - Xbox UWP: `VariableSizedWrapGrid` for BLKs (no WrapPanel); attach/eject expansion image; navigate to FlashCartBuilder page.
   - Protocol/host: expansion cart manage state, media size detect, settings option catalog.
20→
3. **Flash Cart Image Builder (Byrd slice)**
   - Core: `src/ViceSharp.Core/FlashCarts/*` (profiles FE3 512K, Ultimem 1MB, MegaCart+NVRAM, EasyFlash layout).
   - Portable `FlashCartImageBuilderViewModel`; FE3 presets on expansion cart surface.
   - UI: Avalonia `FlashCartBuilderView`; Xbox `FlashCartBuilderPage`.
   - Tests: `tests/ViceSharp.TestHarness/FlashCart/*`.
   - Hostile AGREE: `docs/receipts/hostile-validator-20260808T103211Z.md`.
   - Residual **Partial**: FE3 flash040 program/erase FSM vs full VICE.

4. **Deploy evidence (local lab)**
30→   - Desktop: Nuke `InstallMsi` ProductVersion **1.2.7** succeeded (`docs/_deploy-desktop-2026-08-08_045501.log`).
   - Xbox: `DeployXboxLocal` used in-session (see deploy logs under `docs/_deploy-xbox-*`).

5. **Docs refresh this turn**
   - README Iteration 2 + dashboard date; USER-GUIDE VIC-20 row; Iteration-Roadmap; new `docs/FlashCart-Builder.md`; wiki.yaml + docs/README index; this handoff.

### Exact vs Partial discipline

- Exact only with VICE file+function + matching managed control + hostile AGREE when claiming Exact.
- Do **not** claim whole-machine Exact or pixel framebuffer lockstep vs xvic (still Missing / Partial per audit).
40→- CPU every-cycle 10s lockstep remains green (separate from video FB).

### Resume next

1. Optional: FE3 flash040 residual (TDD first).
2. Optional: pixel framebuffer compare vs xvic (`capture_visible_frame` / present path).
3. Optional: re-run desktop InstallMsi if Avalonia builder only was built after last MSI.
4. Store/Partner Center ADO wiring still open (see Store section below).
5. Query MCP TODO store for live backlog; snapshot list at bottom is stale.

50→### Validation commands (focused)

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart"
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Video"
git diff --check
```

Full baseline (long): filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy` (expect 0 failed; skips allowed only if pre-existing category policy).

60→---

## VIC-20 every-cycle lockstep - PAL + NTSC 10s GREEN (2026-08-06)

- **PAL TenSecondPal:** matchedCycles = 11_084_050 / budget 11_084_050 (~35 s). Log: `docs/10s-lockstep-2026-08-06.log`. Receipt: `docs/receipts-lockstep-10s-2026-08-06.txt`.
- **NTSC TenSecondNtsc:** matchedCycles = 10_227_270 / budget 10_227_270 (~37 s). Log: `docs/10s-lockstep-ntsc-2026-08-06.log`. Receipt: `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`.
- **PAL TwoSecondPal** (VICESHARP_LOCKSTEP_2S=1): Passed ~8 s. FocusedWindow: Passed.
- **Via6522 unit:** 48/0/0 after bus-visible T1/T2 change.
- **Where diverged (NTSC residual c=521027):** soft-BIT $9124 (VIA2 T1) V flag only (nP=$E5 mP=$A5).
- **How diverged:** (1) soft-apply re-LOADed T1; VICE BIT(GET_ABS) loads once. (2) VIA Tick before CPU made CPU Read see post-Tick T1; VICE LOAD is at maincpu_clk before CLK_INC (saw $C0 V=1 vs managed $BF V=0).
70→- **How realigned:** SoftDeferBitBody latches one GET; soft-apply N/V/Z from latch. Via6522 captures pre-Tick `_t1BusVisible`/`_t2BusVisible` for Read; Peek stays post-Tick (export match). Rejected CPU-before-VIA Phi2 reorder (broke free-run phase).
- Env: `VICESHARP_LOCKSTEP_10S=1` for 10s gates; `VICESHARP_LOCKSTEP_2S=1` for 2s. Run NTSC and PAL in **isolated processes** (failed NTSC can poison later PAL short runs at c=5513).

## Iteration 2 VIC-20 - native lockstep gate GREEN (2026-08-05)

- **Branch:** `feat/iteration2-vic20` (off main / v1.2.1). Locked: **default drive 8 = 1540**.
- **Native xvic oracle:** `native/vice_xvic.dll` via `native/build-vice-shim-xvic.sh` + `vice-shim-vic20.c`; hosted `vic20cpu.c` / `mainviccpu.c` (`VICE_SHIM_HOSTED`). Managed: `ViceNative.CreateInstance("vic20"|"vic20ntsc"|"xvic")` -> `ViceNativeXvic`.
- **Open-bus (FR-VIC20-002):** `BasicBus` last-data latch; BASIC/KERNAL ROM reads do **not** refresh last-data (VICE `vic20memrom_*`); chargen does; uninstalled BLK falls through (not sticky 0xFF). Fixes kernal `CMP $A003,X` Carry/N vs xvic.
- **Lockstep receipts (criterion 3):** full **A/X/Y/S/P/PC** through **5000** cycles; **dual VIA** control peeks ($9110/$9120 DDRA/DDRB/ACR/PCR/IER) at 64/256/1024; **VIC-I** static peeks + VICE raster encoding ($9003/$9004) through 5000. Vic20 filter **87/0/0**. API: `IViceNative.PeekBus` / `vice_machine_peek_bus`. Mos6561 power-on zeros + raster encoding match VICE. Receipts: `docs/receipts-vic20-phase2-verify-2026-08-05.txt`. CI: VIC20 + dos1540 in `EnsureCiRomRoot`.
- **Residual:** intermittent mid-instruction PC lag after cycle ~5005 (control-flow diverge later; deeper CPU staging for multi-frame bit-exact).
80→- **Still open (Tier A polish):** J expansion UX, K input E2E, L cart/disk, M snapshots.

### Turn 104 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 16 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\FinalExpansion3Cartridge.cs
10:/// Where diverged: full flash040 program path deferred; START/FLASH menu boot
14:public sealed class FinalExpansion3Cartridge : IAddressSpace
47:    public FinalExpansion3Cartridge(ReadOnlySpan<byte> flashImage)
84:                // VICE FE3 FLASH mode (REGA mode bits): bankable flash window for programming/menu.
135:                throw new ArgumentException($"Unknown FE3 preset '{presetId}'.", nameof(presetId));
319:            // still allow if not RO-only intent; FE3 RegA bit0 = BLK0 RO in some modes

F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\IVic20ExpansionCartPort.cs
6:/// Attach/detach surface for VIC-20 expansion carts (FE3, Ultimem, Mega-Cart).
18:    void AttachFinalExpansion3(ReadOnlySpan<byte> flashImage);

F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20ExpansionCartKind.cs
7:    FinalExpansion3 = 1,

F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\Vic20ExpansionCartPort.cs
7:/// Runtime attach port for FE3 / Ultimem / Mega-Cart on a VIC-20 machine bus.
36:            case FinalExpansion3Cartridge fe3:
51:    public void AttachFinalExpansion3(ReadOnlySpan<byte> flashImage)
54:        var cart = new FinalExpansion3Cartridge(flashImage) { WriteBack = WriteBack };
57:        _kind = Vic20ExpansionCartKind.FinalExpansion3;
95:            case FinalExpansion3Cartridge fe3:
113:            FinalExpansion3Cartridge { WriteBack: true, FlashDirty: true } fe3 => fe3.GetFlashImage(),
</workspace_result>

### Turn 105 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 21 matching lines
F:\GitHub\vice-sharp\HANDOFF.md
39:- Do **not** claim whole-machine Exact or pixel framebuffer lockstep vs xvic (still Missing / Partial per audit).
45:2. Optional: pixel framebuffer compare vs xvic (`capture_visible_frame` / present path).

F:\GitHub\vice-sharp\README.md
69:  - Still open: full FE3 flash040 FSM, pixel framebuffer lockstep vs xvic, input E2E, snapshots (see [HANDOFF.md](HANDOFF.md))

F:\GitHub\vice-sharp\native\vice-shim-vic20.c
1161:VICE_SHIM_API int vice_machine_capture_visible_frame(void *machine, uint8_t *buffer, int length, int *width, int *height)

F:\GitHub\vice-sharp\native\vice-shim.h
155:VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height);

F:\GitHub\vice-sharp\native\vice-shim.c
1754:VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height)

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\VideoRenderer.cs
47:    public readonly byte[] FrameBuffer = new byte[ScreenWidth * ScreenHeight * 4];
53:    // FrameBuffer stores pixels as BGRA bytes: [0]=B, [1]=G, [2]=R, [3]=A,
101:        Span<byte> line = FrameBuffer.AsSpan(y * ScreenWidth * 4, ScreenWidth * 4);
443:    // visibility state before a pixel reaches the framebuffer.
706:        Array.Clear(FrameBuffer);

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\Mos6569.cs
78:    public byte[] FrameBuffer => _renderer.FrameBuffer;

F:\GitHub\vice-sharp\src\ViceSharp.Core\Capture\FrameCapture.cs
33:                videoChip.FrameBuffer,
39:                videoChip.FrameBuffer,
45:                videoChip.FrameBuffer,
63:    /// capture service, which snapshots the framebuffer under its session lock first. Returns

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\VicIStub.cs
81:    public byte[] FrameBuffer => _frameBuffer;
93:    /// Apply profile timing (PAL 71x312 or NTSC 65x261) and rebuild the black framebuffer.

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
3:// Vic20VideoLockstepState lives in Abstractions for native/managed compare.
18:    /// VIC-I 16-color palette as 0xAARRGGBB (B at low byte for framebuffer writers).
71:    /// Visible left border width / paper origin X in the <em>normal-border framebuffer</em>
</workspace_result>

### Turn 106 (Function)
[tool_response]
exit: 0
SHA=0cd61f86fa99ee1e109c69ff2c494a6879c35def
Subject=feat(vic20): present-path borders, expansion UX, flash cart builder
Author=Sharp Ninja <ninja@thesharp.ninja>
Date=2026-08-10 08:10:22 -0500

0cd61f8 feat(vic20): present-path borders, expansion UX, flash cart builder
 HANDOFF.md                                         |  63 +-
 README.md                                          |  12 +-
 build/Build.cs                                     |  12 +-
 docs/FlashCart-Builder.md                          |  58 ++
 docs/Iteration-Roadmap.md                          |  10 +-
 .../wiki/azure/.mcp-requirements-manifest.json     |   5 +-
 docs/Project/wiki/azure/Architecture/.order        |   1 +
 docs/Project/wiki/azure/FR-Xbox-Branding.md        |  57 ++
 docs/Project/wiki/azure/Flash-Cart-Builder.md      |  58 ++
 .../wiki/azure/Functional-Requirements-Overview.md |   2 +
 docs/Project/wiki/azure/Functional-Requirements.md |  19 +
 docs/Project/wiki/azure/Getting-Started/.order     |   1 +
 docs/Project/wiki/azure/Home.md                    |   3 +
 docs/Project/wiki/azure/Iteration-Roadmap.md       |  10 +-
 docs/Project/wiki/azure/Project-Overview.md        |  12 +-
 docs/Project/wiki/azure/Requirements-Matrix.md     |   3 +
 docs/Project/wiki/azure/Requirements/.order        |   1 +
 docs/Project/wiki/azure/TR-per-FR-Mapping.md       |   1 +
 docs/Project/wiki/azure/Technical-Requirements.md  |  11 +
 docs/Project/wiki/azure/Testing-Requirements.md    |   6 +
 docs/Project/wiki/azure/User-Guide.md              |   5 +-
 docs/Project/wiki/azure/VIC20-vs-VICE-Audit.md     | 189 +++++
 .../wiki/github/.mcp-requirements-manifest.json    |   5 +-
 docs/Project/wiki/github/FR-Xbox-Branding.md       |  57 ++
 docs/Project/wiki/github/Flash-Cart-Builder.md     |  58 ++
 .../github/Functional-Requirements-Overview.md     |   2 +
 .../Project/wiki/github/Functional-Requirements.md |  19 +
 docs/Project/wiki/github/Home.md                   |   3 +
 docs/Project/wiki/github/Iteration-Roadmap.md      |  10 +-
 docs/Project/wiki/github/Project-Overview.md       |  12 +-
 docs/Project/wiki/github/Requirements-Matrix.md    |   3 +
 docs/Project/wiki/github/TR-per-FR-Mapping.md      |   1 +
 docs/Project/wiki/github/Technical-Requirements.md |  11 +
 docs/Project/wiki/github/Testing-Requirements.md   |   6 +
 docs/Project/wiki/github/User-Guide.md             |   5 +-
 docs/Project/wiki/github/VIC20-vs-VICE-Audit.md    | 189 +++++
 docs/Project/wiki/github/_Sidebar.md               |   3 +
 docs/README.md                                     |   6 +-
 docs/USER-GUIDE.md                                 |   5 +-
---BRANCH---
## main...origin/main
?? docs/10s-lockstep-2026-08-07.log
?? docs/10s-lockstep-blankfix-2026-08-06.log
?? docs/10s-lockstep-idle-2026-08-06.log
?? docs/10s-lockstep-memptr2-2026-08-06.log
?? docs/10s-lockstep-ntsc-2026-08-07.log
?? docs/10s-lockstep-rst6-2026-08-06.log
?? docs/2s-adc-2026-08-06.log
?? docs/2s-adc2-2026-08-06.log
?? docs/2s-and-imm-2026-08-06.log
?? docs/2s-arm-bcc-2026-08-06.log
?? docs/2s-asl-2026-08-06.log
?? docs/2s-asl-bcs-2026-08-06.log
?? docs/2s-b1-beq-2026-08-06.log
?? docs/2s-b1stage-2026-08-06.log
?? docs/2s-bcc-hold-2026-08-06.log
?? docs/2s-bccsta-2026-08-06.log
?? docs/2s-bcs-stage-2026-08-06.log
?? docs/2s-beq-2026-08-06.log
?? docs/2s-beq-hold-2026-08-06.log
?? docs/2s-beqpla-2026-08-06.log
?? docs/2s-bit-2026-08-06.log
?? docs/2s-bit2-2026-08-06.log
?? docs/2s-bit3-2026-08-06.log
?? docs/2s-bit4-2026-08-06.log
?? docs/2s-bit5-2026-08-06.log
?? docs/2s-bitf-2026-08-06.log
?? docs/2s-bitf2-2026-08-06.log
?? docs/2s-bmi-chain-2026-08-06.log
?? docs/2s-bne-cmp-2026-08-06.log
?? docs/2s-bne-stage-2026-08-06.log
?? docs/2s-bnehold-2026-08-06.log
?? docs/2s-bpl3-2026-08-06.log
?? docs/2s-bpl4-2026-08-06.log
?? docs/2s-bpl6-2026-08-06.log
?? docs/2s-bpl7-2026-08-06.log
?? docs/2s-br-irq-2026-08-06.log
?? docs/2s-cmp-absx-2026-08-06.log
?? docs/2s-cmpzp-2026-08-06.log
?? docs/2s-cmpzp2-2026-08-06.log
?? docs/2s-colorram-2026-08-06.log
?? docs/2s-cpx-soft-2026-08-06.log
?? docs/2s-delay1-2026-08-06.log
?? docs/2s-delays-block-2026-08-06.log
?? docs/2s-delays-final-2026-08-06.log
?? docs/2s-delays-irq-2026-08-06.log
?? docs/2s-eor-fuse-2026-08-06.log
?? docs/2s-eor-imm-2026-08-06.log
?? docs/2s-final-2026-08-06.log
?? docs/2s-flag-only-2026-08-06.log
?? docs/2s-fuse-narrow-2026-08-06.log
?? docs/2s-hold-narrow-2026-08-06.log
?? docs/2s-holdindy-2026-08-06.log
?? docs/2s-incsticky-2026-08-06.log
?? docs/2s-indy-diag-2026-08-06.log
?? docs/2s-indy-nz-2026-08-06.log
?? docs/2s-inx-2026-08-06.log
?? docs/2s-iny-fuse-2026-08-06.log
?? docs/2s-irq-fuse-lda-2026-08-06.log
?? docs/2s-irq2-2026-08-06.log
?? docs/2s-irqb-2026-08-06.log
?? docs/2s-irqdelay0-2026-08-06.log
?? docs/2s-irqdiag-2026-08-06.log
?? docs/2s-irqfall-2026-08-06.log
?? docs/2s-irqrmw-2026-08-06.log
?? docs/2s-irqsuppress-2026-08-06.log
?? docs/2s-jmp-2026-08-06.log
?? docs/2s-jmp-after-jsr-2026-08-06.log
?? docs/2s-jmp-fuse-2026-08-06.log
?? docs/2s-jmp2-2026-08-06.log
?? docs/2s-jmp3-2026-08-06.log
?? docs/2s-jsr-irq-2026-08-06.log
?? docs/2s-jsr-jmpind-2026-08-06.log
?? docs/2s-jsr-jump-2026-08-06.log
?? docs/2s-jsr-ldy-2026-08-06.log
?? docs/2s-lda-2026-08-06.log
?? docs/2s-lda-abs-bcc-2026-08-06.log
?? docs/2s-lda-after-sec-2026-08-06.log
?? docs/2s-lda-imm-chain-2026-08-06.log
?? docs/2s-lda-indy-2026-08-06.log
?? docs/2s-lda-sbc-2026-08-06.log
?? docs/2s-lda-zp-branch-2026-08-06.log
?? docs/2s-ldax-tsx-2026-08-06.log
?? docs/2s-ldx-jmp-2026-08-06.log
?? docs/2s-ldy-fuse-2026-08-06.log
?? docs/2s-ldy-plain-2026-08-06.log
?? docs/2s-lsrx-2026-08-06.log
?? docs/2s-lsrzp-2026-08-06.log
?? docs/2s-lsrzp2-2026-08-06.log
?? docs/2s-memdiag-2026-08-06.log
?? docs/2s-nib-2026-08-06.log
?? docs/2s-ora-2026-08-06.log
?? docs/2s-pla-2026-08-06.log
?? docs/2s-pla-late-2026-08-06.log
?? docs/2s-pla2-2026-08-06.log
?? docs/2s-post-store-rts-2026-08-06.log
?? docs/2s-rola-fuse-2026-08-06.log
?? docs/2s-ror-chain-2026-08-06.log
?? docs/2s-ror-chain2-2026-08-06.log
?? docs/2s-ror-rti-2026-08-06.log
?? docs/2s-ror-zp-beq-2026-08-06.log
?? docs/2s-rora-soft-2026-08-06.log
?? docs/2s-rora-trail2-2026-08-06.log
?? docs/2s-rti-2026-08-06.log
?? docs/2s-rti2-2026-08-06.log
?? docs/2s-rts-load-2026-08-06.log
?? docs/2s-rts-store-br-2026-08-06.log
?? docs/2s-rts-trail-2026-08-06.log
?? docs/2s-rts-trail2-2026-08-06.log
?? docs/2s-rtsand-2026-08-06.log
?? docs/2s-rtsfl-2026-08-06.log
?? docs/2s-sec-2026-08-06.log
?? docs/2s-sec-bne-2026-08-06.log
?? docs/2s-shift-phase-2026-08-06.log
?? docs/2s-shift-taken-2026-08-06.log
?? docs/2s-skip-latch-2026-08-06.log
?? docs/2s-sta-bne-2026-08-06.log
?? docs/2s-sta-irq-2026-08-06.log
?? docs/2s-sta-load-2026-08-06.log
?? docs/2s-sta-trail-2026-08-06.log
?? docs/2s-stazp-2026-08-06.log
?? docs/2s-sticky-clear-2026-08-06.log
?? docs/2s-sticky2-2026-08-06.log
?? docs/2s-store-2026-08-06.log
?? docs/2s-store-hold-2026-08-06.log
?? docs/2s-store-hold2-2026-08-06.log
?? docs/2s-store-irq-2026-08-06.log
?? docs/2s-sty-trail-2026-08-06.log
?? docs/2s-tax-chain-2026-08-06.log
?? docs/2s-tay-2026-08-06.log
?? docs/2s-txa-fuse-2026-08-06.log
?? docs/2s-vbus-2026-08-06.log
?? docs/2s-vbus-2s-green-2026-08-06.log
?? docs/2s-vbus-best-2026-08-06.log
?? docs/2s-vbus-delay3-2026-08-06.log
?? docs/2s-vbus-diag-2026-08-06.log
?? docs/2s-vbus-dump-green-2026-08-06.txt
?? docs/2s-vbus-final-2026-08-06.log
?? docs/2s-vbus-final2-2026-08-06.log
?? docs/2s-vbus-noread-2026-08-06.log
?? docs/2s-vbus-openh-2026-08-06.log
?? docs/2s-vbus-order-2026-08-06.log
?? docs/2s-vbus-order2-2026-08-06.log
?? docs/2s-vbus-phase-2026-08-06.log
?? docs/2s-vbus-phase1-2026-08-06.log
?? docs/2s-vbus-vert-2026-08-06.log
?? docs/2s-vbus2-2026-08-06.log
?? docs/2s-via-cont-2026-08-06.log
?? docs/2s-via-t1-2026-08-06.log
?? docs/2s-via1iec-2026-08-06.log
?? docs/S-Blox/
?? docs/_deploy-desktop-2026-08-06.log
?? docs/_deploy-desktop-2026-08-06b.log
?? docs/_deploy-desktop-2026-08-08_015554.log
?? docs/_deploy-desktop-2026-08-08_025358.log
?? docs/_deploy-desktop-2026-08-08_032741.log
?? docs/_deploy-desktop-2026-08-08_045348.log
?? docs/_deploy-desktop-2026-08-08_045348.log.err
?? docs/_deploy-desktop-2026-08-08_045445.log
?? docs/_deploy-desktop-2026-08-08_045501.log
?? docs/_deploy-last-pair.txt
?? docs/_deploy-xbox-2026-08-06.log
?? docs/_deploy-xbox-2026-08-06b.log
?? docs/_deploy-xbox-2026-08-08_015554.log
?? docs/_deploy-xbox-2026-08-08_025358.log
?? docs/_deploy-xbox-2026-08-08_032741.log
?? docs/_deploy-xbox-2026-08-08_045348.log
?? docs/_deploy-xbox-2026-08-08_045348.log.err
?? docs/_vs-ready-2026-08-08-crop.png
?? docs/_vs-ready-2026-08-08-latest.png
?? docs/_vs-ready-2026-08-08-prev-crop.png
?? docs/_vs-ready-2026-08-08-prev.png
?? docs/adc-focused-2026-08-06.log
?? docs/adc2-focused-2026-08-06.log
?? docs/asl-focused-2026-08-06.log
?? docs/bccsta-focused-2026-08-06.log
?? docs/beq-focused-2026-08-06.log
?? docs/beqpla-focused-2026-08-06.log
?? docs/bit-focused-2026-08-06.log
?? docs/bit2-focused-2026-08-06.log
?? docs/bit3-focused-2026-08-06.log
?? docs/bit4-focused-2026-08-06.log
?? docs/bit5-focused-2026-08-06.log
?? docs/bitf-focused-2026-08-06.log
?? docs/bitf2-focused-2026-08-06.log
?? docs/bnehold-focused-2026-08-06.log
?? docs/bpl-focused-2026-08-06.log
?? docs/bpl2-focused-2026-08-06.log
?? docs/bpl3-focused-2026-08-06.log
?? docs/bpl4-focused-2026-08-06.log
?? docs/bpl5-focused-2026-08-06.log
?? docs/bpl6-focused-2026-08-06.log
?? docs/bpl7-focused-2026-08-06.log
?? docs/cmpzp-focused-2026-08-06.log
?? docs/cmpzp2-focused-2026-08-06.log
?? docs/focused-b1-beq-2026-08-06.log
?? docs/focused-beq-hold-2026-08-06.log
?? docs/focused-cpx-soft-2026-08-06.log
?? docs/focused-final-2026-08-06.log
?? docs/focused-gates-final-2026-08-06.log
?? docs/focused-holdindy-2026-08-06.log
?? docs/focused-incsticky-2026-08-06.log
?? docs/focused-indy-nz-2026-08-06.log
?? docs/focused-iny-fuse-2026-08-06.log
?? docs/focused-irq-fuse-lda-2026-08-06.log
?? docs/focused-irqb-2026-08-06.log
?? docs/focused-irqfall-2026-08-06.log
?? docs/focused-irqsuppress-2026-08-06.log
?? docs/focused-jsr-irq-2026-08-06.log
?? docs/focused-jsr-irq2-2026-08-06.log
?? docs/focused-jsr-jump-2026-08-06.log
?? docs/focused-jsr-ldy-2026-08-06.log
?? docs/focused-lda-indy-2026-08-06.log
?? docs/focused-ldy-fuse-2026-08-06.log
?? docs/focused-ldy-plain-2026-08-06.log
?? docs/focused-ora-2026-08-06.log
?? docs/focused-revert-irq-2026-08-06.log
?? docs/focused-ror-chain-2026-08-06.log
?? docs/focused-ror-chain2-2026-08-06.log
?? docs/focused-ror-rti-2026-08-06.log
?? docs/focused-ror-zp-beq-2026-08-06.log
?? docs/focused-rts-nonovl-2026-08-06.log
?? docs/focused-rts-store-br-2026-08-06.log
?? docs/focused-rts-trail-eq2-2026-08-06.log
?? docs/focused-rts-trail2-2026-08-06.log
?? docs/focused-shift-phase-2026-08-06.log
?? docs/focused-sta-trail-2026-08-06.log
?? docs/focused-stazp-2026-08-06.log
?? docs/focused-store-2026-08-06.log
?? docs/focused-store-hold-2026-08-06.log
?? docs/focused-store-hold2-2026-08-06.log
?? docs/focused-sty-trail-2026-08-06.log
?? docs/focused-tax-chain-2026-08-06.log
?? docs/focused-vbus-order-2026-08-06.log
?? docs/inx-focused-2026-08-06.log
?? docs/irqrmw-focused-2026-08-06.log
?? docs/jmp-focused-2026-08-06.log
?? docs/jmp2-focused-2026-08-06.log
?? docs/jmp3-focused-2026-08-06.log
?? docs/lda-focused-2026-08-06.log
?? docs/lsrx-focused-2026-08-06.log
?? docs/lsrzp-focused-2026-08-06.log
?? docs/lsrzp2-focused-2026-08-06.log
?? docs/ntsc-t1-phase.txt
?? docs/pla-focused-2026-08-06.log
?? docs/pla-late-focused-2026-08-06.log
?? docs/pla2-focused-2026-08-06.log
?? docs/receipts-lockstep-2s-bcc-2026-08-06.log
?? docs/receipts-lockstep-2s-cmp-2026-08-06.log
?? docs/receipts-lockstep-2s-imm-2026-08-06.log
?? docs/receipts-lockstep-2s-jsr-2026-08-06.log
?? docs/receipts-lockstep-2s-ldaabs-2026-08-06.log
?? docs/receipts-lockstep-2s-ldax-2026-08-06.log
?? docs/receipts-lockstep-2s-ldx-2026-08-06.log
?? docs/receipts-lockstep-2s-ldx2-2026-08-06.log
?? docs/receipts-lockstep-2s-nop-2026-08-06.log
?? docs/receipts-lockstep-2s-nopchain-2026-08-06.log
?? docs/receipts-lockstep-2s-plp-2026-08-06.log
?? docs/receipts-lockstep-2s-plp-b-2026-08-06.log
?? docs/receipts-lockstep-2s-rts-diag-2026-08-06.log
?? docs/receipts-lockstep-2s-rts-v2-2026-08-06.log
?? docs/receipts-lockstep-2s-rts-v3-2026-08-06.log
?? docs/receipts-lockstep-2s-rts-v4-2026-08-06.log
?? docs/receipts-lockstep-2s-sty-2026-08-06.log
?? docs/receipts-lockstep-2s-zpinc-only-2026-08-06.log
?? docs/receipts-lockstep-everycycle-2s-after-rts-2026-08-06.log
?? docs/receipts-lockstep-everycycle-2s-after-zpinc-2026-08-06.log
?? docs/receipts-lockstep-everycycle-2s-attempt-2026-08-06.log
?? docs/receipts-lockstep-everycycle-after-rts-2026-08-06.log
?? docs/receipts-lockstep-everycycle-baseline-2026-08-06.log
?? docs/receipts-lockstep-everycycle-gates-2026-08-06.log
?? docs/receipts-lockstep-everycycle-zpinc-fix-2026-08-06.log
?? docs/receipts-lockstep-focused-after-rts-phase-2026-08-06.log
?? docs/receipts-lockstep-focused-bcc-2026-08-06.log
?? docs/receipts-lockstep-focused-cmp-2026-08-06.log
?? docs/receipts-lockstep-focused-imm-2026-08-06.log
?? docs/receipts-lockstep-focused-jsr-2026-08-06.log
?? docs/receipts-lockstep-focused-ldaabs-2026-08-06.log
?? docs/receipts-lockstep-focused-ldax-2026-08-06.log
?? docs/receipts-lockstep-focused-ldx-2026-08-06.log
?? docs/receipts-lockstep-focused-ldx2-2026-08-06.log
?? docs/receipts-lockstep-focused-nop-2026-08-06.log
?? docs/receipts-lockstep-focused-nopchain-2026-08-06.log
?? docs/receipts-lockstep-focused-plp-2026-08-06.log
?? docs/receipts-lockstep-focused-plp-b-2026-08-06.log
?? docs/receipts-lockstep-focused-rts-overlap-2026-08-06.log
?? docs/receipts-lockstep-focused-rts-v2-2026-08-06.log
?? docs/receipts-lockstep-focused-rts-v3-2026-08-06.log
?? docs/receipts-lockstep-focused-rts-v4-2026-08-06.log
?? docs/receipts-lockstep-focused-sty-2026-08-06.log
?? docs/receipts-lockstep-focused-zpinc-only-2026-08-06.log
?? docs/reviews/code-review-20260714T001056552Z.md
?? docs/reviews/code-review-20260714T004256563Z.md
?? docs/romless-vic-badline-fix.md
?? docs/rti-focused-2026-08-06.log
?? docs/rti2-focused-2026-08-06.log
?? docs/rtsand-focused-2026-08-06.log
?? docs/rtsfl-focused-2026-08-06.log
?? docs/sec-focused-2026-08-06.log
?? docs/tay-focused-2026-08-06.log
?? docs/via1-iec-focused-2026-08-06.log
?? docs/vic20-cpu-plus-video-lockstep-focused-2026-08-08.log
?? docs/vic20-present-path-fix-2026-08-08.log
?? docs/vic20-ready-paper-fix-2026-08-08.log
?? docs/vic20-video-lockstep-2026-08-08.log
?? docs/vic20-video-lockstep-2k-2026-08-08.log
?? docs/xvic-err.txt
?? docs/xvic-err2.txt
?? docs/xvic-out.txt
?? docs/xvic-out2.txt
?? docs/xvic-ready-nofilter-2026-08-08.png
?? docs/xvic-ready-pal-2026-08-08.png
?? docs/xvic-ready-pal-2026-08-08b.png
?? docs/xvic-ready-user-2026-08-08.png
?? docs/xvic-shot.ini
?? manifests/
0cd61f86fa99ee1e109c69ff2c494a6879c35def
0cd61f86fa99ee1e109c69ff2c494a6879c35def
e5203f94c69511ce3dbbf69eee7a4f038cd53d7a
0cd61f8 feat(vic20): present-path borders, expansion UX, flash cart builder


### Turn 107 (Function)
[tool_response]
exit: 1
=== STATUS ===
{"status":"available","agent":"GrokCode","namespaces":["workflow.sessionlog","workflow.todo","workflow.requirements","workflow.triage","workflow.graphrag","workflow.memory","workflow.failsafe"],"requirementMethods":["workflow.requirements.listLayers","workflow.requirements.createLayer","workflow.requirements.updateLayer","workflow.requirements.effective"],"requirementClientMethods":["client.Requirements.ListRequirementLayersAsync","client.Requirements.CreateRequirementLayerAsync","client.Requirements.UpdateRequirementLayerAsync","client.Requirements.GetEffectiveRequirementsAsync"],"cacheDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok","hasSession":true,"hasCurrentTurn":true,"pendingCount":0,"pendingTurnCount":0,"failsafeDir":"F:\\GitHub\\vice-sharp\\.mcpServer\\grok\\failsafe","failsafeCount":0,"failsafeQuarantineCount":0}
=== HEALTH NONCE ===
nonce_sent=8a06eee27e3544cd9f1901273b0cfb30
nonce_echo=8a06eee27e3544cd9f1901273b0cfb30
nonce_match=True
health_raw_type=System.Management.Automation.PSCustomObject
{"status":"Healthy","version":"1.4.25+28f56515ca1a806f60a6617d3b742e8e3b58b854","checks":[{"name":"self","status":"Healthy","description":null,"duration":0.0027},{"name":"upstream","status":"Healthy","description":"Federation disabled.","duration":0.0026}],"nonce":"8a06eee27e3544cd9f1901273b0cfb30","storage":"reachable"}
=== SIGNATURE ===
WriteError: 
Line |
  15 |  $pid = Get-Field $yaml "pid"
     |  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | Cannot overwrite variable PID because it is read-only or constant.


### Turn 108 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
# Signature via plugin marker-resolver functions
$script = @'
$ErrorActionPreference = "Stop"
. "F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1"
$cmds = Get-Command -Name "*Marker*","*Signature*","*Trust*" -ErrorAction SilentlyContinue | Select-Object Name, CommandType
$cmds | Format-Table -AutoSize | Out-String
# try common patterns
$markerPath = "F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml"
foreach ($name in @(
  "Test-McpMarkerSignature",
  "Assert-McpMarkerTrust",
  "Get-McpMarkerTrustResult",
  "Resolve-McpMarker",
  "Read-McpMarkerFile",
  "Verify-McpMarkerSignature"
)) {
  if (Get-Command $name -ErrorAction SilentlyContinue) {
    Write-Host "TRY $name"
    & $name -MarkerPath $markerPath 2>&1 | Out-String
  }
}
# inspect exported functions from marker-resolver by grepping file
Select-String -Path "F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1" -Pattern "^function " | ForEach-Object { $_.Line }
'@
pwsh.exe -NoProfile -NonInteractive -Command $script 2>&1

Write-Host '=== AZURE REMOTE ==='
git fetch azure 2>&1 | Select-Object -Last 10
git rev-parse HEAD origin/main azure/main 2>&1
git log --oneline azure/main..HEAD 2>&1
Write-Host '=== CAPTURE VISIBLE FRAME STUB ==='
Select-String -Path 'native/vice-shim-vic20.c' -Pattern 'capture_visible_frame' -Context 0,15 | Select-Object -First 40

- description: Verify signature via resolver; azure lag; capture stub

### Turn 109 (Function)
[tool_response]
exit: 0

Name                                                    CommandType
----                                                    -----------
Add-AdlStoreTrustedIdProvider                                 Alias
Add-AdlStoreTrustedIdProvider                                 Alias
Add-AdlStoreTrustedIdProvider                                 Alias
Add-AdlStoreTrustedIdProvider                                 Alias
Add-TrustedProvisioningCertificate                            Alias
Get-AdlStoreTrustedIdProvider                                 Alias
Get-AdlStoreTrustedIdProvider                                 Alias
Get-AdlStoreTrustedIdProvider                                 Alias
Get-AdlStoreTrustedIdProvider                                 Alias
Remove-AdlStoreTrustedIdProvider                              Alias
Remove-AdlStoreTrustedIdProvider                              Alias
Remove-AdlStoreTrustedIdProvider                              Alias
Remove-AdlStoreTrustedIdProvider                              Alias
Remove-TrustedProvisioningCertificate                         Alias
Set-AdlStoreTrustedIdProvider                                 Alias
Set-AdlStoreTrustedIdProvider                                 Alias
Set-AdlStoreTrustedIdProvider                                 Alias
Set-AdlStoreTrustedIdProvider                                 Alias
Set-AzSqlServerTrustGroup                                     Alias
Set-AzSqlServerTrustGroup                                     Alias
Set-AzSqlServerTrustGroup                                     Alias
Set-AzSqlServerTrustGroup                                     Alias
Add-VpnConnectionTriggerTrustedNetwork                     Function
Find-MarkerFile                                            Function
Get-AzAksTrustedAccessRole                                 Function
Get-AzAksTrustedAccessRoleBinding                          Function
Get-MarkerAgentPluginField                                 Function
Get-MarkerEndpoint                                         Function
Get-MarkerField                                            Function
Get-MarkerFileSnapshot                                     Function
Get-SSHTrustedHost                                         Function
New-AzAksTrustedAccessRoleBinding                          Function
New-SSHTrustedHost                                         Function
Remove-AzAksTrustedAccessRoleBinding                       Function
Remove-SSHTrustedHost                                      Function
Remove-VpnConnectionTriggerTrustedNetwork                  Function
Set-VpnConnectionTriggerTrustedNetwork                     Function
Test-MarkerSignature                                       Function
Test-MarkerSignature                                       Function
Update-AzAksTrustedAccessRoleBinding                       Function
Update-MpSignature                                         Function
Update-MpSignature                                         Function
Add-AzApplicationGatewayTrustedClientCertificate             Cmdlet
Add-AzApplicationGatewayTrustedRootCertificate               Cmdlet
Add-AzDataLakeStoreTrustedIdProvider                         Cmdlet
Get-AuthenticodeSignature                                    Cmdlet
Get-AzApplicationGatewayTrustedClientCertificate             Cmdlet
Get-AzApplicationGatewayTrustedRootCertificate               Cmdlet
Get-AzDataLakeStoreTrustedIdProvider                         Cmdlet
Get-AzSqlInstanceServerTrustCertificate                      Cmdlet
Get-AzSqlServerTrustGroup                                    Cmdlet
Get-TrustedProvisioningCertificate                           Cmdlet
Install-TrustedProvisioningCertificate                       Cmdlet
New-AzApplicationGatewayTrustedClientCertificate             Cmdlet
New-AzApplicationGatewayTrustedRootCertificate               Cmdlet
New-AzFirewallPolicyIntrusionDetectionSignatureOverride      Cmdlet
New-AzSqlInstanceServerTrustCertificate                      Cmdlet
New-AzSqlServerTrustGroup                                    Cmdlet
Remove-AzApplicationGatewayTrustedClientCertificate          Cmdlet
Remove-AzApplicationGatewayTrustedRootCertificate            Cmdlet
Remove-AzDataLakeStoreTrustedIdProvider                      Cmdlet
Remove-AzSqlInstanceServerTrustCertificate                   Cmdlet
Remove-AzSqlServerTrustGroup                                 Cmdlet
Set-AuthenticodeSignature                                    Cmdlet
Set-AzApplicationGatewayTrustedClientCertificate             Cmdlet
Set-AzApplicationGatewayTrustedRootCertificate               Cmdlet
Set-AzDataLakeStoreTrustedIdProvider                         Cmdlet
Uninstall-TrustedProvisioningCertificate                     Cmdlet
ApplyTrustOffline.exe                                   Application


function Find-MarkerFile {
function Get-MarkerField {
function Get-MarkerFileSnapshot {
function Get-MarkerEndpoint {
function Get-MarkerAgentPluginField {
function Test-MarkerSignature {
function Invoke-FullBootstrap {
=== AZURE REMOTE ===
0cd61f86fa99ee1e109c69ff2c494a6879c35def
0cd61f86fa99ee1e109c69ff2c494a6879c35def
e5203f94c69511ce3dbbf69eee7a4f038cd53d7a
0cd61f8 feat(vic20): present-path borders, expansion UX, flash cart builder
=== CAPTURE VISIBLE FRAME STUB ===

> native\vice-shim-vic20.c:1161:VICE_SHIM_API int vice_machine_capture_visible_frame(void *machine, uint8_t *buffer, 
int length, int *width, int *height)
  native\vice-shim-vic20.c:1162:{
  native\vice-shim-vic20.c:1163:    (void)machine;
  native\vice-shim-vic20.c:1164:    (void)buffer;
  native\vice-shim-vic20.c:1165:    (void)length;
  native\vice-shim-vic20.c:1166:    (void)width;
  native\vice-shim-vic20.c:1167:    (void)height;
  native\vice-shim-vic20.c:1168:    return 0;
  native\vice-shim-vic20.c:1169:}
  native\vice-shim-vic20.c:1170:
  native\vice-shim-vic20.c:1171:VICE_SHIM_API int vice_vic_capture_frame_indices(void *machine, uint8_t *buffer, int 
length, int *width, int *height)
  native\vice-shim-vic20.c:1172:{
  native\vice-shim-vic20.c:1173:    (void)machine;
  native\vice-shim-vic20.c:1174:    (void)buffer;
  native\vice-shim-vic20.c:1175:    (void)length;
  native\vice-shim-vic20.c:1176:    (void)width;



### Turn 110 (Assistant)
[tool_request: run_terminal_command]
- command: Set-Location 'F:\GitHub\vice-sharp'
pwsh.exe -NoProfile -NonInteractive -Command @'
$ErrorActionPreference = "Stop"
. "F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1"
$markerPath = "F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml"
$ok = Test-MarkerSignature -MarkerPath $markerPath
"Test-MarkerSignature=$ok"
$snap = Get-MarkerFileSnapshot -MarkerPath $markerPath
"workspace=$($snap.workspacePath)"
"baseUrl=$($snap.baseUrl)"
"markerWrittenAtUtc=$($snap.markerWrittenAtUtc)"
"declaredSig=$($snap.signatureValue)"
'@ 2>&1

# azure fetch with network
git fetch azure main 2>&1
"azure/main after fetch: $(git rev-parse azure/main 2>$null)"
"commits azure behind: $((git rev-list --count azure/main..HEAD 2>$null))"

- description: Test-MarkerSignature and fetch azure main

### Turn 111 (Function)
[tool_response]
exit: 0
Test-MarkerSignature: 
Line |
   4 |  $ok = Test-MarkerSignature -MarkerPath $markerPath
     |                             ~~~~~~~~~~~
     | A parameter cannot be found that matches parameter name 'MarkerPath'.
From https://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp
 * branch            main       -> FETCH_HEAD
azure/main after fetch: e5203f94c69511ce3dbbf69eee7a4f038cd53d7a
commits azure behind: 1


### Turn 112 (Assistant)
[tool_request: run_terminal_command]
- command: pwsh.exe -NoProfile -NonInteractive -Command @'
$ErrorActionPreference = "Stop"
. "F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1"
(Get-Command Test-MarkerSignature).Parameters.Keys
$help = Get-Help Test-MarkerSignature -Full
$help.synopsis
$help.parameters | Out-String
# try positional
$r = Test-MarkerSignature "F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml"
"result=$r type=$($r.GetType().FullName)"
if ($r -is [hashtable] -or $r.PSObject) { $r | ConvertTo-Json -Depth 4 -Compress }
'@ 2>&1

- description: Discover Test-MarkerSignature signature API

### Turn 113 (Function)
[tool_response]
exit: 0
MarkerFile
Verbose
Debug
ErrorAction
WarningAction
InformationAction
ProgressAction
ErrorVariable
WarningVariable
InformationVariable
OutVariable
OutBuffer
PipelineVariable

Test-MarkerSignature [-MarkerFile] <string> [<CommonParameters>]


parameter
---------
{@{name=MarkerFile; required=true; pipelineInput=false; isDynamic=false; globbing=false; parameterSetName=(All); parame.


result=True type=System.Boolean
true


### Turn 114 (Assistant)
**2026-08-10** — Resume checklist executed (no product code changed; no probe logs staged).

## 1. HANDOFF + wrap-up commit

**HANDOFF.md** (repo root, 2026-08-10): `main`, GitVersion base 1.2.2 / local MSI 1.2.7; present-path + expansion UX + flash builder shipped; residual FE3 flash040 Partial; pixel FB lockstep Missing; bulk probe logs stay untracked.

**This turn’s commit (on `main` / `origin/main`):**
- SHA: `0cd61f86fa99ee1e109c69ff2c494a6879c35def` (`0cd61f8`)
- Subject: `feat(vic20): present-path borders, expansion UX, flash cart builder`
- Author: Sharp Ninja; date 2026-08-10 08:10:22 -0500
- Working tree: clean of staged code; only untracked probe/deploy noise

## 2. MCP trust re-verify

| Check | Result |
|-------|--------|
| Plugin Status | `available`, agent `GrokCode`, session present |
| Marker signature | `Test-MarkerSignature -MarkerFile AGENTS-README-FIRST.yaml` → **True** |
| `/health` nonce | sent `8a06eee27e3544cd9f1901273b0cfb30`, echo exact match; status **Healthy**, storage reachable, version `1.4.25+28f56515…` |

**MCP_TRUSTED** for this workspace. (Native Grok MCP tool list still shows `mcpserver` auth-required; plugin REPL path is the working surface.)

## 3. Next product slices (TDD readiness — not started)

**A. FE3 flash040 residual**
- Where: `FinalExpansion3Cartridge.cs` (comment: full flash040 program path deferred)
- Slice shape: red tests for program/erase FSM vs VICE FE3, then implement only what tests require
- Scope: cart runtime accuracy; does not block READY present path

**B. Pixel FB compare vs xvic**
- Where: `vice_machine_capture_visible_frame` in `native/vice-shim-vic20.c` is a **stub** (`return 0`)
- Also stubbed: `vice_vic_capture_frame_indices`
- Slice shape: implement real capture from xvic draw buffer, then managed vs native present-frame test (after first_x crop alignment)
- Scope: larger; needs native shim work + VICE framebuffer path; enables true pixel lockstep

**Recommendation (inference):** if the goal is cart completeness, pick **A**. If the goal is “READY looks Exact including pixels,” pick **B** (geometry Exact is already AGREE’d; FB compare is the gap).

I have **not** started either slice (needs your pick).

## 4. ADO (`azure`) package truth

After `git fetch azure main`:
- `HEAD` / `origin/main`: `0cd61f8`
- `azure/main`: `e5203f9` (**1 commit behind**)
- Behind commit: `0cd61f8 feat(vic20): present-path borders, expansion UX, flash cart builder`

**Not pushed** (conditional step; you did not say “push azure”). Say **push azure** and I will `git push azure main`.

## 5. Probe logs

**Not bulk-committed.** Still untracked: hundreds of `docs/2s-*`, `10s-*`, focused logs, `_deploy-*`, `xvic-*`, `S-Blox/`, `manifests/`, etc. Left alone per handoff.

---

**Waiting on you:**
1. Next slice: **FE3 flash040** or **pixel FB** (or neither yet)
2. **Push azure?** yes / no

### Turn 115 (Human)
<user_query>
1. Both; 2. We are not uzing azure any longer
</user_query>

### Turn 116 (Assistant)
Implementing both TDD slices (FE3 flash040 + pixel FB vs xvic) and retiring Azure as a remote from docs/handoff. Exploring current FE3 and capture paths first.
[tool_request: todo_write]
- todos: [{"id":"1","content":"Explore FE3 flash040 managed + VICE source; write failing tests","status":"in_progress"},{"id":"2","content":"Implement FE3 flash040 residual to green","status":"pending"},{"id":"3","content":"Explore xvic frame capture stub; write failing pixel FB tests","status":"pending"},{"id":"4","content":"Implement capture_visible_frame + managed compare green","status":"pending"},{"id":"5","content":"Retire azure remote from HANDOFF/docs; validate + handoff","status":"pending"}]
- merge: false
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\Vic20\FinalExpansion3Cartridge.cs
[tool_request: grep]
- pattern: flash040|FLASH040|fe3|final.?expansion|vic20-fe3
- path: F:\GitHub\vice-sharp\native\vice
- glob: *.{c,h}
- head_limit: 50
[tool_request: grep]
- pattern: CaptureVisible|capture_visible|GetFrame|FrameBuffer|VideoLockstep
- path: F:\GitHub\vice-sharp\src
- glob: *.{cs}
- head_limit: 50
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20ExpansionCartTests.cs
- limit: 150

### Turn 117 (Function)
[tool_response]
- [in_progress] 1: Explore FE3 flash040 managed + VICE source; write failing tests
- [pending] 2: Implement FE3 flash040 residual to green
- [pending] 3: Explore xvic frame capture stub; write failing pixel FB tests
- [pending] 4: Implement capture_visible_frame + managed compare green
- [pending] 5: Retire azure remote from HANDOFF/docs; validate + handoff


### Turn 118 (Function)
[tool_response]
1→using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 Final Expansion 3 (VICE finalexpansion.c v3.2) MVP.
/// 512K SRAM + 512K flash; REGA $9C02 / REGB $9C03; RAM modes for BLK0/1/2/3/5.
/// </summary>
/// <remarks>
10→/// Where diverged: full flash040 program path deferred; START/FLASH menu boot
/// needs user flash image. Realign: REGA/REGB bit fields and RAM1/SUPER_RAM/ROM_RAM
/// maps match VICE tables for host RAM Manager presets.
/// </remarks>
public sealed class FinalExpansion3Cartridge : IAddressSpace
{
    public const int FlashSize = 0x80000;
    public const int SramSize = 0x80000;

    public const byte RegABankMask = 0x0F;
20→    public const byte RegAModeMask = 0xE0;
    public const byte ModeStart = 0x00;
    public const byte ModeFlash = 0x20;
    public const byte ModeSuperRom = 0x40;
    public const byte ModeRomRam = 0x60;
    public const byte ModeRam1 = 0x80;
    public const byte ModeSuperRam = 0xA0;
    public const byte ModeRam2 = 0xC0;

    public const byte RegABlk0Ro = 0x01;
30→    public const byte RegABlk1Sel = 0x02;
    public const byte RegABlk2Sel = 0x04;
    public const byte RegABlk3Sel = 0x08;
    public const byte RegABlk5Sel = 0x10;

    public const byte RegBBlk0Off = 0x01;
    public const byte RegBBlk1Off = 0x02;
    public const byte RegBBlk2Off = 0x04;
    public const byte RegBBlk3Off = 0x08;
    public const byte RegBBlk5Off = 0x10;
40→
    private readonly byte[] _flash;
    private readonly byte[] _sram;
    private byte _registerA;
    private byte _registerB;
    private bool _flashDirty;

    public FinalExpansion3Cartridge(ReadOnlySpan<byte> flashImage)
    {
        _flash = new byte[FlashSize];
50→        _sram = new byte[SramSize];
        var copy = Math.Min(flashImage.Length, FlashSize);
        if (copy > 0)
            flashImage[..copy].CopyTo(_flash);
        Id = new DeviceId(0x0C10);
        PowerUp();
    }

    public DeviceId Id { get; }
    public string Name => "Final Expansion 3";
60→    public byte RegisterA => _registerA;
    public byte RegisterB => _registerB;
    public bool FlashDirty => _flashDirty;
    public bool WriteBack { get; set; }

    public void PowerUp()
    {
        // VICE powerup: START mode, bank 0, registers enabled.
        _registerA = ModeStart;
        _registerB = 0;
70→    }

    public void Reset() => PowerUp();

    public void ApplyConfigPreset(string presetId)
    {
        switch (presetId.Trim().ToLowerInvariant())
        {
            case "start":
            case "menu":
80→                _registerA = ModeStart;
                _registerB = 0;
                break;
            case "flash":
                // VICE FE3 FLASH mode (REGA mode bits): bankable flash window for programming/menu.
                _registerA = ModeFlash;
                _registerB = 0;
                break;
            case "super-rom":
            case "superrom":
90→                _registerA = ModeSuperRom;
                _registerB = 0;
                break;
            case "rom-ram":
            case "romram":
                _registerA = ModeRomRam;
                _registerB = 0;
                break;
            case "ram2":
                _registerA = ModeRam2;
100→                _registerB = 0;
                break;
            case "none":
            case "unexpanded":
                _registerA = ModeRam1;
                _registerB = (byte)(RegBBlk0Off | RegBBlk1Off | RegBBlk2Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "3k":
                _registerA = (byte)(ModeRam1 | RegABlk0Ro);
                _registerB = (byte)(RegBBlk1Off | RegBBlk2Off | RegBBlk3Off | RegBBlk5Off);
110→                break;
            case "8k":
                _registerA = (byte)(ModeRam1 | RegABlk1Sel);
                _registerB = (byte)(RegBBlk0Off | RegBBlk2Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "16k":
                _registerA = (byte)(ModeRam1 | RegABlk1Sel | RegABlk2Sel);
                _registerB = (byte)(RegBBlk0Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "24k":
120→                _registerA = (byte)(ModeRam1 | RegABlk1Sel | RegABlk2Sel | RegABlk3Sel);
                _registerB = (byte)(RegBBlk0Off | RegBBlk5Off);
                break;
            case "full":
            case "all":
            case "32k":
                _registerA = (byte)(ModeRam1 | RegABlk0Ro | RegABlk1Sel | RegABlk2Sel | RegABlk3Sel | RegABlk5Sel);
                _registerB = 0;
                break;
            case "super-ram":
130→            case "superram":
                _registerA = (byte)(ModeSuperRam | RegABlk1Sel | RegABlk2Sel | RegABlk3Sel | RegABlk5Sel);
                _registerB = 0;
                break;
            default:
                throw new ArgumentException($"Unknown FE3 preset '{presetId}'.", nameof(presetId));
        }
    }

    public bool HandlesAddress(ushort address)
140→    {
        if (address is >= 0x9C00 and <= 0x9FFF)
            return true;
        return IsMappedWindow(address);
    }

    public byte Read(ushort address)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
            return ReadIo3(address);
150→        if (TryMap(address, isWrite: false, out var offset, out var useFlash))
            return useFlash ? _flash[offset] : _sram[offset];
        return 0xFF;
    }

    public byte Peek(ushort address) => Read(address);

    public void Write(ushort address, byte value)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
160→        {
            WriteIo3(address, value);
            return;
        }

        if (!TryMap(address, isWrite: true, out var offset, out var useFlash))
            return;

        if (useFlash)
        {
170→            _flash[offset] = value;
            _flashDirty = true;
        }
        else
        {
            _sram[offset] = value;
        }
    }

    public byte[] GetFlashImage() => _flash.ToArray();
180→
    public void ClearFlashDirty() => _flashDirty = false;

    private byte ReadIo3(ushort address)
    {
        return (address & 0x03) switch
        {
            0x02 => _registerA,
            0x03 => _registerB,
            _ => 0xFF,
190→        };
    }

    private void WriteIo3(ushort address, byte value)
    {
        switch (address & 0x03)
        {
            case 0x02:
                _registerA = value;
                break;
200→            case 0x03:
                _registerB = value;
                break;
        }
    }

    private bool IsMappedWindow(ushort address)
    {
        return address switch
        {
210→            >= 0x0400 and <= 0x0FFF => IsBlockEnabled(0),
            >= 0x2000 and <= 0x3FFF => IsBlockEnabled(1),
            >= 0x4000 and <= 0x5FFF => IsBlockEnabled(2),
            >= 0x6000 and <= 0x7FFF => IsBlockEnabled(3),
            >= 0xA000 and <= 0xBFFF => IsBlockEnabled(5),
            _ => false,
        };
    }

    private bool IsBlockEnabled(int block)
220→    {
        var mode = (byte)(_registerA & RegAModeMask);
        if (mode is ModeStart or ModeFlash or ModeSuperRom)
        {
            // START/FLASH/SUPER_ROM: BLK5 typically ROM; expose BLK5 for menu/rom reads.
            return block == 5 || mode == ModeSuperRom;
        }

        var offMask = block switch
        {
230→            0 => RegBBlk0Off,
            1 => RegBBlk1Off,
            2 => RegBBlk2Off,
            3 => RegBBlk3Off,
            5 => RegBBlk5Off,
            _ => (byte)0,
        };
        if ((_registerB & offMask) != 0)
            return false;

240→        // RAM1/RAM2/ROM_RAM/SUPER_RAM: use select bits (BLK0 uses RO bit as present).
        var selMask = block switch
        {
            0 => RegABlk0Ro,
            1 => RegABlk1Sel,
            2 => RegABlk2Sel,
            3 => RegABlk3Sel,
            5 => RegABlk5Sel,
            _ => (byte)0,
        };
250→        // If no select bits set at all in RAM1, treat as none selected.
        if (mode is ModeRam1 or ModeRam2 or ModeRomRam)
            return (_registerA & selMask) != 0 || (block == 0 && (_registerA & RegABlk0Ro) != 0 && selMask == RegABlk0Ro);

        if (mode == ModeSuperRam)
            return true;

        return false;
    }

260→    private bool TryMap(ushort address, bool isWrite, out int offset, out bool useFlash)
    {
        offset = 0;
        useFlash = false;
        if (!IsMappedWindow(address))
            return false;

        var mode = (byte)(_registerA & RegAModeMask);
        var bank = _registerA & RegABankMask;
        useFlash = mode is ModeStart or ModeFlash or ModeSuperRom or ModeRomRam;
270→
        // VICE RAM map (simplified): BLK windows into 8K slices; SUPER_RAM banks N*64K.
        int windowBase = address switch
        {
            >= 0x0400 and <= 0x0FFF => 0x0400,
            >= 0x2000 and <= 0x3FFF => 0x0000,
            >= 0x4000 and <= 0x5FFF => 0x2000,
            >= 0x6000 and <= 0x7FFF => 0x4000,
            >= 0xA000 and <= 0xBFFF => 0x6000,
            _ => 0,
280→        };

        var local = address switch
        {
            >= 0x0400 and <= 0x0FFF => address - 0x0400,
            >= 0x2000 and <= 0x3FFF => address - 0x2000,
            >= 0x4000 and <= 0x5FFF => address - 0x4000,
            >= 0x6000 and <= 0x7FFF => address - 0x6000,
            >= 0xA000 and <= 0xBFFF => address - 0xA000,
            _ => 0,
290→        };

        if (mode == ModeSuperRam)
        {
            offset = (bank * 0x10000) + windowBase + local;
            useFlash = false;
        }
        else if (mode is ModeRam1 or ModeRam2)
        {
            offset = windowBase + local;
300→            if (address is >= 0x0400 and <= 0x0FFF)
                offset = 0x0400 + local;
            useFlash = false;
        }
        else
        {
            // ROM modes: banked flash
            offset = (bank * 0x8000) + windowBase + local;
            useFlash = true;
        }
310→
        var size = useFlash ? FlashSize : SramSize;
        if (offset < 0 || offset >= size)
            return false;

        // BLK0 RO in RAM1: ignore writes
        if (isWrite && useFlash == false && address is >= 0x0400 and <= 0x0FFF
            && (_registerA & RegABlk0Ro) != 0 && mode is ModeRam1 or ModeRomRam)
        {
            // still allow if not RO-only intent; FE3 RegA bit0 = BLK0 RO in some modes
320→            // For simplicity allow writes unless ModeRomRam on BLK0.
            if (mode == ModeRomRam)
                return false;
        }

        return true;
    }
}


### Turn 119 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FE3 / Ultimem / Mega-Cart attach and map smoke tests driving real cart types.
/// </summary>
public sealed class Vic20ExpansionCartTests
10→{
    [Fact]
    public void Fe3_Ram1_FullPreset_MapsBlk1()
    {
        var flash = new byte[FinalExpansion3Cartridge.FlashSize];
        var cart = new FinalExpansion3Cartridge(flash);
        cart.ApplyConfigPreset("full");
        Assert.Equal(FinalExpansion3Cartridge.ModeRam1 | FinalExpansion3Cartridge.RegABlk0Ro
            | FinalExpansion3Cartridge.RegABlk1Sel | FinalExpansion3Cartridge.RegABlk2Sel
            | FinalExpansion3Cartridge.RegABlk3Sel | FinalExpansion3Cartridge.RegABlk5Sel,
20→            cart.RegisterA & 0xFF);

        cart.Write(0x2000, 0x5A);
        Assert.Equal(0x5A, cart.Read(0x2000));
        cart.Write(0xA000, 0xA5);
        Assert.Equal(0xA5, cart.Read(0xA000));
    }

    [Fact]
    public void Fe3_Io3_RegARegB_RoundTrip()
30→    {
        var cart = new FinalExpansion3Cartridge(new byte[FinalExpansion3Cartridge.FlashSize]);
        cart.Write(0x9C02, FinalExpansion3Cartridge.ModeSuperRam | 0x03);
        cart.Write(0x9C03, FinalExpansion3Cartridge.RegBBlk0Off);
        Assert.Equal((byte)(FinalExpansion3Cartridge.ModeSuperRam | 0x03), cart.Read(0x9C02));
        Assert.Equal(FinalExpansion3Cartridge.RegBBlk0Off, cart.Read(0x9C03));
        // Mirror
        Assert.Equal(cart.Read(0x9C02), cart.Read(0x9C06));
    }

40→    [Fact]
    public void Fe3_WriteBack_FlushReturnsDirtyFlash()
    {
        var cart = new FinalExpansion3Cartridge(new byte[FinalExpansion3Cartridge.FlashSize]);
        cart.WriteBack = true;
        cart.ApplyConfigPreset("start");
        // START maps BLK5 to flash; write dirties flash when mode allows
        cart.Write(0x9C02, FinalExpansion3Cartridge.ModeFlash);
        cart.Write(0xA010, 0x42);
        Assert.True(cart.FlashDirty);
50→        var image = cart.GetFlashImage();
        Assert.Equal(FinalExpansion3Cartridge.FlashSize, image.Length);
    }

    [Fact]
    public void Ultimem_BankRegister_MapsBlk5()
    {
        var image = new byte[0x20000];
        for (var i = 0; i < image.Length; i++)
            image[i] = (byte)(i & 0xFF);
60→        var cart = new UltimemCartridge(image);
        cart.Write(0x9C0E, 0x01); // bank lo for BLK5
        var v = cart.Read(0xA000);
        // bank 1 * 0x2000 + 0
        Assert.Equal(image[0x2000], v);
    }

    [Fact]
    public void MegaCart_BankLow_SelectsRomWindow()
    {
70→        var rom = new byte[0x10000];
        rom[0x0000] = 0x11;
        rom[0x2000] = 0x22;
        var cart = new MegaCartCartridge(rom);
        cart.Write(0x9C00, 0x00);
        Assert.Equal(0x11, cart.Read(0x2000));
        cart.Write(0x9C00, 0x01);
        Assert.Equal(0x22, cart.Read(0x2000));
    }

80→    [Fact]
    public void MegaCart_Nvram_WriteAndFlush()
    {
        var cart = new MegaCartCartridge(new byte[0x10000]);
        cart.ApplyConfigPreset("nvram");
        // Enable nvram via bank bit7
        cart.Write(0x9C00, 0x80);
        cart.Write(0x0500, 0x77);
        Assert.Equal(0x77, cart.Read(0x0500));
        Assert.True(cart.NvramDirty);
90→        var nv = cart.GetNvram();
        Assert.Equal(0x77, nv[0x0500]);
    }

    [Fact]
    public void ExpansionCartPort_AttachFe3_OnBus()
    {
        var bus = new ViceSharp.Core.BasicBus();
        var port = new Vic20ExpansionCartPort(bus);
        var flash = new byte[FinalExpansion3Cartridge.FlashSize];
100→        flash[0] = 0xC3;
        port.AttachFinalExpansion3(flash);
        Assert.Equal(Vic20ExpansionCartKind.FinalExpansion3, port.AttachedKind);
        port.ApplyConfigPreset("8k");
        Assert.True(port.TryWriteMapped(0x2000, 0xAB));
        Assert.True(port.TryReadMapped(0x2000, out var v));
        Assert.Equal(0xAB, v);
        port.Eject();
        Assert.Equal(Vic20ExpansionCartKind.None, port.AttachedKind);
    }
110→}


### Turn 120 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 29 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Host\Services\GrpcHostServiceAdapters.cs
36:        // sessions in real time off the UI thread. GetFrameAsync is a pure pull.
350:    public override async Task<GrpcContracts.GetVideoFrameResponse> GetFrame(
354:        var response = await _inner.GetFrameAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);
363:        var response = await _inner.GetFrameAsync(new SessionRequest(request.SessionId), context.CancellationToken).ConfigureAwait(false);

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\VideoRenderer.cs
47:    public readonly byte[] FrameBuffer = new byte[ScreenWidth * ScreenHeight * 4];
53:    // FrameBuffer stores pixels as BGRA bytes: [0]=B, [1]=G, [2]=R, [3]=A,
101:        Span<byte> line = FrameBuffer.AsSpan(y * ScreenWidth * 4, ScreenWidth * 4);
706:        Array.Clear(FrameBuffer);

F:\GitHub\vice-sharp\src\ViceSharp.Chips\VicIi\Mos6569.cs
78:    public byte[] FrameBuffer => _renderer.FrameBuffer;

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\VicIStub.cs
81:    public byte[] FrameBuffer => _frameBuffer;

F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
3:// Vic20VideoLockstepState lives in Abstractions for native/managed compare.
211:        EnsureFrameBufferSize();
251:    public byte[] FrameBuffer => _frameBuffer;
262:    public Vic20VideoLockstepState CaptureVideoLockstepState(uint cycle = 0)
270:        return new Vic20VideoLockstepState
304:        EnsureFrameBufferSize();
570:        EnsureFrameBufferSize();
743:        EnsureFrameBufferSize();
798:    private void EnsureFrameBufferSize()

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Services\CaptureServiceHost.cs
450:            frame = new byte[videoChip.FrameBuffer.Length];
451:            videoChip.FrameBuffer.CopyTo(frame, 0);

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
77:    Vic20VideoLockstepState GetVic20VideoState();
244:/// (<c>vice_vic20_get_video_state</c> / managed <c>Mos6561.CaptureVideoLockstepState</c>).
248:public readonly struct Vic20VideoLockstepState

F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IVideoChip.cs
27:    byte[] FrameBuffer { get; }
36:    /// Raised when a complete frame is ready in FrameBuffer.

F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\MainWindow.axaml.cs
925:            var frame = await _hostClient.GetFrameAsync().ConfigureAwait(true);

F:\GitHub\vice-sharp\src\ViceSharp.Xbox.ViewModels\ILocalVideoFramePull.cs
46:    /// <c>TryGetFrameGeometry</c> byte-for-byte so the head adapter stays a thin
60:    bool TryGetFrameGeometry(string sessionId, out FrameGeometry geometry);

F:\GitHub\vice-sharp\src\ViceSharp.Host.InProcess\Runtime\ConsoleEmulatorHost.cs
</workspace_result>

### Turn 121 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found at least 45 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\cartridge.h
481:#define CARTRIDGE_VIC20_FINAL_EXPANSION 5   /* finalexpansion.c */

F:\GitHub\vice-sharp\native\vice\vice\src\c128\cart\gmod2c128.c
40:#include "flash040.h"
90:static flash040_context_t *flashrom_state = NULL;
172:    return flash040core_read(flashrom_state, (addr & 0x3fff) + (c128gmod2_bank << 14));
177:    flash040core_store(flashrom_state, (addr & 0x3fff) + (c128gmod2_bank << 14), value);
178:    if (flashrom_state->flash_state != FLASH040_STATE_READ) {
200:    flash040core_reset(flashrom_state);
211:        flash040core_reset(flashrom_state);
217:    flashrom_state = lib_malloc(sizeof(flash040_context_t));
218:    flash040core_init(flashrom_state, maincpu_alarm_context, FLASH040_TYPE_NORMAL, ext_function_rom);
489:        flash040core_shutdown(flashrom_state);
511:static const char flash_snap_module_name[] = "FLASH040GMOD2";
538:    return flash040core_snapshot_write_module(s, flashrom_state, flash_snap_module_name);
576:    flashrom_state = lib_malloc(sizeof(flash040_context_t));
577:    flash040core_init(flashrom_state, maincpu_alarm_context, FLASH040_TYPE_NORMAL, ext_function_rom);
579:    if (flash040core_snapshot_read_module(s, flashrom_state, flash_snap_module_name) < 0) {
580:        flash040core_shutdown(flashrom_state);

F:\GitHub\vice-sharp\native\vice\vice\src\flash040.h
2: * flash040.h - (AM)29F0[14]0(B) Flash emulation.
26:#ifndef VICE_FLASH040_H
27:#define VICE_FLASH040_H
31:enum flash040_type_s {
33:    FLASH040_TYPE_NORMAL,
36:    FLASH040_TYPE_B,
39:    FLASH040_TYPE_010,
42:    FLASH040_TYPE_032B_A0_1_SWAP,
45:    FLASH040_TYPE_064,
48:    FLASH040_TYPE_NUM
50:typedef enum flash040_type_s flash040_type_t;
52:enum flash040_state_s {
53:    FLASH040_STATE_READ,
54:    FLASH040_STATE_MAGIC_1,
55:    FLASH040_STATE_MAGIC_2,
56:    FLASH040_STATE_AUTOSELECT,
57:    FLASH040_STATE_BYTE_PROGRAM,
58:    FLASH040_STATE_BYTE_PROGRAM_ERROR,
59:    FLASH040_STATE_ERASE_MAGIC_1,
60:    FLASH040_STATE_ERASE_MAGIC_2,
61:    FLASH040_STATE_ERASE_SELECT,
62:    FLASH040_STATE_CHIP_ERASE,
63:    FLASH040_STATE_SECTOR_ERASE,
64:    FLASH040_STATE_SECTOR_ERASE_TIMEOUT,
65:    FLASH040_STATE_SECTOR_ERASE_SUSPEND
67:typedef enum flash040_state_s flash040_state_t;
69:#define FLASH040_ERASE_MASK_SIZE 8
71:typedef struct flash040_context_s {
</workspace_result>

### Turn 122 (Assistant)
[tool_request: grep]
- pattern: flash040|flashrom|fe3_
- path: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\cart
- glob: *final*
- head_limit: 40
[tool_request: run_terminal_command]
- command: Get-ChildItem 'F:\GitHub\vice-sharp\native\vice\vice\src\vic20\cart' -Filter '*final*' | Select-Object Name
Get-ChildItem 'F:\GitHub\vice-sharp\native\vice\vice\src' -Filter 'flash040*' | Select-Object FullName

- description: Locate FE3 and flash040 source files
[tool_request: grep]
- pattern: capture_visible_frame|CaptureVisibleFrame
- path: F:\GitHub\vice-sharp
- glob: *.{cs,c,h}
- head_limit: 40
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\Vic20VideoLockstep.cs
- limit: 120

### Turn 123 (Function)
[tool_response]
1→namespace ViceSharp.TestHarness.Vic20;

using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
10→/// Every-cycle VIC-I video pipeline lockstep vs xvic (same discipline as CPU
/// A/X/Y/S/P/PC): after each phi2, compare raster, area, fetch, memptr, cbuf/gbuf,
/// and raw $9000-$900F stores. CPU lockstep is not video proof; this is.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20VideoLockstep
{
    /// <summary>~0.45s PAL; past power-on into KERNAL video programming.</summary>
    public const int FocusedBudgetCycles = 500_000;

20→    [Fact]
    public void EveryCycle_VicI_VideoState_Match_FocusedWindow()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        var matched = RunEveryCycleVideo(FocusedBudgetCycles);
        Assert.True(matched == FocusedBudgetCycles,
            $"expected full video match for {FocusedBudgetCycles} cycles, matched={matched}");
    }
30→
    /// <summary>
    /// Short smoke: video state advances and stays bit-equal for 2k cycles.
    /// </summary>
    [Fact]
    public void EveryCycle_VicI_VideoState_Match_2k()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

40→        var matched = RunEveryCycleVideo(2000);
        Assert.Equal(2000, matched);
    }

    /// <summary>
    /// Steps managed + xvic; returns matched cycles. Throws on first video mismatch.
    /// </summary>
    public static int RunEveryCycleVideo(int budget, string modelSelector = "vic20")
    {
        using var native = ViceNative.CreateInstance(modelSelector);
50→        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine(modelSelector);
        managed.Reset();
        var vic = Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));

        var log = new StringBuilder();
        const int pre = 8;
        var ring = new string[pre];
        var ringAt = 0;
        var ringCount = 0;
60→
        for (var i = 1; i <= budget; i++)
        {
            native.Step();
            managed.Clock.Step();

            var n = native.GetVic20VideoState();
            var m = vic.CaptureVideoLockstepState((uint)i);
            var detail = DescribeMismatch(n, m);
            var line =
70→                $"c={i} nR={n.RasterLine} mR={m.RasterLine} nCyc={n.RasterCycle} mCyc={m.RasterCycle} " +
                $"nA={n.Area} mA={m.Area} nF={n.FetchState} mF={m.FetchState} " +
                $"nMem={n.Memptr} mMem={m.Memptr} nY={n.YCounter} mY={m.YCounter} " +
                $"nRow={n.RowCounter} mRow={m.RowCounter} nCols={n.TextCols} mCols={m.TextCols} " +
                $"nBlank={n.BlankThisLine} mBlank={m.BlankThisLine} mis={detail is not null}";

            ring[ringAt] = line;
            ringAt = (ringAt + 1) % pre;
            if (ringCount < pre)
                ringCount++;
80→
            if (detail is null)
                continue;

            var start = (ringAt - ringCount + pre) % pre;
            for (var k = 0; k < ringCount; k++)
                log.AppendLine(ring[(start + k) % pre]);
            log.AppendLine(detail);
            log.AppendLine(FormatRegs("nRegs", n.Regs));
            log.AppendLine(FormatRegs("mRegs", m.Regs));
90→            // Bus-visible $9003/$9004 (raster encode) — part of video output surface.
            log.AppendLine(
                $"peek $9003 n=${native.PeekBus(0x9003):X2} m=${managed.Bus.Peek(0x9003):X2} " +
                $"$9004 n=${native.PeekBus(0x9004):X2} m=${managed.Bus.Peek(0x9004):X2}");

            throw new Xunit.Sdk.XunitException(
                $"VIDEO DIV first={i} matchedCycles={i - 1} budget={budget}\n{log}");
        }

        return budget;
100→    }

    /// <summary>
    /// Compare native vs managed VIC-I pipeline. Returns null if equal.
    /// </summary>
    public static string? DescribeMismatch(Vic20VideoLockstepState n, Vic20VideoLockstepState m)
    {
        if (n.RasterLine != m.RasterLine)
            return $"RasterLine n={n.RasterLine} m={m.RasterLine}";
        if (n.RasterCycle != m.RasterCycle)
110→            return $"RasterCycle n={n.RasterCycle} m={m.RasterCycle}";
        if (n.Area != m.Area)
            return $"Area n={n.Area} m={m.Area}";
        if (n.FetchState != m.FetchState)
            return $"FetchState n={n.FetchState} m={m.FetchState}";
        if (n.TextCols != m.TextCols)
            return $"TextCols n={n.TextCols} m={m.TextCols}";
        if (n.TextLines != m.TextLines)
            return $"TextLines n={n.TextLines} m={m.TextLines}";
        if (n.YCounter != m.YCounter)
120→            return $"YCounter n={n.YCounter} m={m.YCounter}";

### Turn 124 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 17 matching lines
F:\GitHub\vice-sharp\native\vice\vice\src\vic20\cart\finalexpansion.c
45:#include "flash040.h"
125:static flash040_context_t flash_state;
334:                value = flash040core_read(&flash_state, faddr);
341:            value = flash040core_read(&flash_state, faddr);
345:                value = flash040core_read(&flash_state, faddr);
406:            flash040core_store(&flash_state, faddr, value);
633:    flash040core_reset(&flash_state);
705:    /* flash040core_init() does not clear the flash */
708:    flash040core_init(&flash_state, maincpu_alarm_context, FLASH040_TYPE_B, cart_flash);
762:    /* flash040core_init() does not clear the flash */
765:    flash040core_init(&flash_state, maincpu_alarm_context, FLASH040_TYPE_B, cart_flash);
800:    flash040core_shutdown(&flash_state);
960:    if ((flash040core_snapshot_write_module(s, &flash_state, FLASH_SNAP_MODULE_NAME) < 0)) {
990:    flash040core_init(&flash_state, maincpu_alarm_context, FLASH040_TYPE_B, cart_flash);
999:        flash040core_shutdown(&flash_state);
1009:    if ((flash040core_snapshot_read_module(s, &flash_state, FLASH_SNAP_MODULE_NAME) < 0)) {
1010:        flash040core_shutdown(&flash_state);
</workspace_result>

### Turn 125 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 8 matching lines
F:\GitHub\vice-sharp\native\vice-shim.h
155:VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height);

F:\GitHub\vice-sharp\native\vice-shim.c
1754:VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height)

F:\GitHub\vice-sharp\native\vice-shim-vic20.c
1161:VICE_SHIM_API int vice_machine_capture_visible_frame(void *machine, uint8_t *buffer, int length, int *width, int *height)

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
194:    [LibraryImport(LibraryName, EntryPoint = "vice_machine_capture_visible_frame")]
195:    public static partial int CaptureVisibleFrame(IntPtr instance, [Out] byte[] buffer, int length, out int width, out int height);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\VicExactFrameOracleTests.cs
136:    public void CaptureVisibleFrame_RealPath_MatchesIndexIdentity()

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceNativeBridge.cs
262:            int res = ViceNative.CaptureVisibleFrame(machine, bgraBuffer, required, out width, out height);

F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\VicIIRegisterReadbackNativeTests.cs
735:    /// shim.c (vice_machine_capture_visible_frame + "full vicii.raster.canvas copy follow-on" note).
</workspace_result>

### Turn 126 (Function)
[tool_response]
exit: 0

Name
----
finalexpansion.c
finalexpansion.h
finalexpansion.o




### Turn 127 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\flash040.h
[tool_request: run_terminal_command]
- command: # flash040.c - key functions
Get-ChildItem 'F:\GitHub\vice-sharp\native\vice\vice\src' -Filter 'flash040.c' -Recurse | Select-Object FullName

- description: Find flash040.c path
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\vic20\cart\finalexpansion.c
- offset: 300
- limit: 120
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim.c
- offset: 1750
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\VicExactFrameOracleTests.cs
- offset: 100
- limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\ViceNativeBridge.cs
- offset: 240
- limit: 50

### Turn 128 (Function)
[tool_response]
1→/*
 * flash040.h - (AM)29F0[14]0(B) Flash emulation.
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
 *  Foundation, Inc., 51 Franklin Street, Suite 500, Boston, MA 02110-1335 USA.
 *
 */

#ifndef VICE_FLASH040_H
#define VICE_FLASH040_H

#include "types.h"
30→
enum flash040_type_s {
    /* 29F040 */
    FLASH040_TYPE_NORMAL,

    /* 29F040B */
    FLASH040_TYPE_B,

    /* 29F010 */
    FLASH040_TYPE_010,
40→
    /* 29F032B, A0/1 swapped */
    FLASH040_TYPE_032B_A0_1_SWAP,

    /* Spansion S29GL064N */
    FLASH040_TYPE_064,

    /* This item always needs to be at the end */
    FLASH040_TYPE_NUM
};
50→typedef enum flash040_type_s flash040_type_t;

enum flash040_state_s {
    FLASH040_STATE_READ,
    FLASH040_STATE_MAGIC_1,
    FLASH040_STATE_MAGIC_2,
    FLASH040_STATE_AUTOSELECT,
    FLASH040_STATE_BYTE_PROGRAM,
    FLASH040_STATE_BYTE_PROGRAM_ERROR,
    FLASH040_STATE_ERASE_MAGIC_1,
60→    FLASH040_STATE_ERASE_MAGIC_2,
    FLASH040_STATE_ERASE_SELECT,
    FLASH040_STATE_CHIP_ERASE,
    FLASH040_STATE_SECTOR_ERASE,
    FLASH040_STATE_SECTOR_ERASE_TIMEOUT,
    FLASH040_STATE_SECTOR_ERASE_SUSPEND
};
typedef enum flash040_state_s flash040_state_t;

#define FLASH040_ERASE_MASK_SIZE 8
70→
typedef struct flash040_context_s {
    uint8_t *flash_data;
    flash040_state_t flash_state;
    flash040_state_t flash_base_state;

    uint8_t program_byte;
    uint8_t erase_mask[FLASH040_ERASE_MASK_SIZE];
    int flash_dirty;

80→    flash040_type_t flash_type;

    uint8_t last_read;
    struct alarm_s *erase_alarm;
} flash040_context_t;

struct alarm_context_s;

void flash040core_init(struct flash040_context_s *flash040_context,
                       struct alarm_context_s *alarm_context,
90→                       flash040_type_t type, uint8_t *data);
void flash040core_shutdown(struct flash040_context_s *flash040_context);
void flash040core_reset(struct flash040_context_s *flash040_context);

void flash040core_store(struct flash040_context_s *flash040_context,
                        unsigned int addr, uint8_t data);
uint8_t flash040core_read(struct flash040_context_s *flash040_context,
                          unsigned int addr);
uint8_t flash040core_peek(struct flash040_context_s *flash040_context,
                          unsigned int addr);
100→
struct snapshot_s;

int flash040core_snapshot_write_module(struct snapshot_s *s,
                                       struct flash040_context_s *flash040_context,
                                       const char *name);
int flash040core_snapshot_read_module(struct snapshot_s *s,
                                      struct flash040_context_s *flash040_context,
                                      const char *name);

110→#endif


### Turn 129 (Function)
[tool_response]
300→        case MODE_FLASH:
        case MODE_SUPER_ROM:
        case MODE_SUPER_RAM:
            bank = register_a & REGA_BANK_MASK;
            break;
        case MODE_ROM_RAM:
            if (sel) {
                bank = 0;
            } else {
                bank = 1;
310→            }
            break;
        case MODE_RAM1:
            bank = 1;
            break;
        case MODE_RAM2:
            if (sel) {
                bank = 2;
            } else {
                bank = 1;
320→            }
            break;
        default:
            bank = 0;
            break;
    }

    /* Calculate Address */
    faddr = calc_addr(addr, bank, base);

330→    /* Perform access */
    switch (mode) {
        case MODE_START:
            if (blk == 5) {
                value = flash040core_read(&flash_state, faddr);
            } else {
                value = vic20_cpu_last_data;
            }
            break;
        case MODE_FLASH:
340→        case MODE_SUPER_ROM:
            value = flash040core_read(&flash_state, faddr);
            break;
        case MODE_ROM_RAM:
            if (sel) {
                value = flash040core_read(&flash_state, faddr);
            } else {
                value = cart_ram[faddr];
            }
            break;
350→        case MODE_RAM1:
        case MODE_RAM2:
        case MODE_SUPER_RAM:
            value = cart_ram[faddr];
            break;
        default:
            value = vic20_cpu_last_data;
            break;
    }
    return value;
360→}

static void internal_store(uint16_t addr, uint8_t value, int blk, uint16_t base, int sel)
{
    uint8_t mode;
    int bank;
    unsigned int faddr;

    mode = register_a & REGA_MODE_MASK;

370→    /* Determine which bank to access */
    switch (mode) {
        case MODE_FLASH:
        case MODE_SUPER_RAM:
            bank = register_a & REGA_BANK_MASK;
            break;
        case MODE_SUPER_ROM:
#ifdef FE3_2_SUPER_ROM_BUG
            bank = 1 | (register_a & REGA_BANK_MASK);
            break;
380→#endif
        case MODE_START:
            bank = 1;
            break;
        case MODE_ROM_RAM:
        case MODE_RAM1:
            if (sel) {
                bank = 2;
            } else {
                bank = 1;
390→            }
            break;
        case MODE_RAM2:
            bank = 1;
            break;
        default:
            bank = 0;
            break;
    }

400→    /* Calculate Address */
    faddr = calc_addr(addr, bank, base);

    /* Perform access */
    switch (mode) {
        case MODE_FLASH:
            flash040core_store(&flash_state, faddr, value);
            break;
        case MODE_ROM_RAM:
        case MODE_START:
410→        case MODE_RAM1:
        case MODE_RAM2:
        case MODE_SUPER_ROM:
        case MODE_SUPER_RAM:
            cart_ram[faddr] = value;
            break;
        default:
            break;
    }
}

### Turn 130 (Function)
[tool_response]
1750→// Full visible frame as BGRA (presentation-level capture). The real path maps
// the raster draw buffer's palette indices through the canvas palette; the
// index endpoint above is the parity oracle. 320x200 selects the graphics
// window (gfx_position), 384x272 the full visible canvas including borders.
VICE_SHIM_API int vice_machine_capture_visible_frame(void* machine, uint8_t* buffer, int length, int* width, int* height)
{
    const int CheckpointW = 320;
    const int CheckpointH = 200;
    const int FullW = 384; // matches VideoRenderer + raster full visible canvas
    const int FullH = 272;
1760→
    int fullRequired = FullW * FullH * 4;
    int checkpointRequired = CheckpointW * CheckpointH * 4;

    int w = CheckpointW;
    int h = CheckpointH;
    int required = checkpointRequired;
    int full = 0;
    if (length >= fullRequired)
    {
1770→        w = FullW;
        h = FullH;
        required = fullRequired;
        full = 1;
    }
    else if (length < checkpointRequired)
    {
        if (width) *width = 0;
        if (height) *height = 0;
        return 0;
1780→    }

    if (width) *width = w;
    if (height) *height = h;
    if (buffer == NULL) return 0;

    vice_shim_ensure_sync_primitives();

    EnterCriticalSection(&g_state_lock);
    if (!vice_shim_is_active_machine(machine)) {
1790→        LeaveCriticalSection(&g_state_lock);
        memset(buffer, 0, required);
        buffer[0] = 0xCC; buffer[1] = 0xCC; buffer[2] = 0xCC; buffer[3] = 0xFF;
        return 1;
    }

    {
        const uint8_t *base = NULL;
        int stride = 0;
        int visW = 0;
1800→        int visH = 0;
        const palette_t *pal = vicii.raster.canvas != NULL ? vicii.raster.canvas->palette : NULL;

        if (!vice_shim_vic_visible_window_locked(&base, &stride, &visW, &visH)
            || pal == NULL || pal->entries == NULL) {
            LeaveCriticalSection(&g_state_lock);
            return 0;
        }

        if (!full) {
1810→            /* Graphics window: gfx_position is in screen coordinates; the
             * visible window starts at first_displayed_line and x 0 of the
             * visible canvas maps to screen x extra-left already removed, so
             * offset by the left border width and the gfx start line. */
            int gfxTop = (int)vicii.raster.geometry->gfx_position.y - vicii.first_displayed_line;
            if (gfxTop < 0 || gfxTop + CheckpointH > visH
                || vicii.screen_leftborderwidth + CheckpointW > visW) {
                LeaveCriticalSection(&g_state_lock);
                return 0;
            }
1820→            base += (size_t)gfxTop * stride + vicii.screen_leftborderwidth;
            visW = CheckpointW;
            visH = CheckpointH;
        }
        else if (visW != FullW || visH != FullH) {
            LeaveCriticalSection(&g_state_lock);
            return 0;
        }

        for (int row = 0; row < h; row++) {

### Turn 131 (Function)
[tool_response]
100→    [ViceFact]
    public void CaptureIndices_Deterministic_AcrossInstances()
    {
        static byte[] Capture()
        {
            var machine = CreateReadyMachine();
            try
            {
                var indices = new byte[VisibleWidth * VisibleHeight];
                Assert.True(
110→                    ViceNativeBridge.TryCaptureVicFrameIndices(machine, indices, out _, out _),
                    "index capture failed");
                return indices;
            }
            finally
            {
                ViceNativeBridge.DestroyMachine(machine);
            }
        }

120→        var first = Capture();
        var second = Capture();
        Assert.True(first.AsSpan().SequenceEqual(second), "frame capture diverged between identical runs");
    }

    /// <summary>
    /// FR: FR-VIC-DRAW-COLOR, TR: TR-VIC-ORACLE-001, TEST: TEST-VIC-ORACLE-P0-03.
    /// Use case: the BGRA visible-frame capture backs presentation-level
    /// comparisons and must now carry real rendered pixels instead of the
    /// previous 0xCC sentinel fill.
130→    /// Acceptance: the full 384x272 BGRA capture is not the sentinel pattern,
    /// every alpha byte is 0xFF, and colour identity follows the index
    /// capture: pixels with equal indices have equal BGRA values and pixels
    /// with different indices have different BGRA values.
    /// </summary>
    [ViceFact]
    public void CaptureVisibleFrame_RealPath_MatchesIndexIdentity()
    {
        var machine = CreateReadyMachine();
        try
140→        {
            var indices = new byte[VisibleWidth * VisibleHeight];
            Assert.True(
                ViceNativeBridge.TryCaptureVicFrameIndices(machine, indices, out _, out _),
                "index capture failed");

            var bgra = new byte[VisibleWidth * VisibleHeight * 4];
            Assert.True(
                ViceNativeBridge.TryCaptureVicVisibleFrame(machine, bgra, out var width, out var height),
                "BGRA capture failed");
150→            Assert.Equal(VisibleWidth, width);
            Assert.Equal(VisibleHeight, height);

            Assert.False(
                bgra[0] == 0xCC && bgra[1] == 0xCC && bgra[2] == 0xCC,
                "BGRA capture still returns the sentinel fill");

            var colourByIndex = new Dictionary<byte, (byte B, byte G, byte R)>();
            for (int i = 0; i < indices.Length; i++)
            {
160→                Assert.Equal(0xFF, bgra[(i * 4) + 3]);
                var colour = (bgra[i * 4], bgra[(i * 4) + 1], bgra[(i * 4) + 2]);
                if (colourByIndex.TryGetValue(indices[i], out var seen))
                {
                    Assert.True(seen == colour, $"index {indices[i]:X2} mapped to two BGRA colours at pixel {i}");
                }
                else
                {
                    colourByIndex[indices[i]] = colour;
                }
170→            }

            Assert.Equal(colourByIndex.Count, colourByIndex.Values.Distinct().Count());
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(machine);
        }
    }
}

### Turn 132 (Function)
[tool_response]
240→        if (bgraBuffer != null && bgraBuffer.Length >= fullRequired)
        {
            width = FullW;
            height = FullH;
        }
        else if (bgraBuffer != null && bgraBuffer.Length >= checkpointRequired)
        {
            width = CheckpointW;
            height = CheckpointH;
        }
250→        else
        {
            width = 0;
            height = 0;
            return false;
        }

        int required = width * height * 4;

        if (machine != IntPtr.Zero)
260→        {
            // Real delegation (authentic from native emulation; shim now supports full via vicii raster/line buffers for this slice)
            int res = ViceNative.CaptureVisibleFrame(machine, bgraBuffer, required, out width, out height);
            return res != 0;
        }

        // Mock/stub (validated green prior to real full shim wiring per BDP)
        // Size-aware: for full buffer use simulator-derived expectations (COL_NONE black gfx + sentinel); for checkpoint prior behavior.
        Array.Clear(bgraBuffer, 0, Math.Min(required, bgraBuffer.Length));
        if (bgraBuffer.Length >= 4)
270→        {
            bgraBuffer[0] = 0xCC;
            bgraBuffer[1] = 0xCC;
            bgraBuffer[2] = 0xCC;
            bgraBuffer[3] = 0xFF;
        }

        // For full buffer mock path (tests-first enabling), mark a representative gfx sample black (COL_NONE parity with simulator).
        if (width == FullW && height == FullH && bgraBuffer.Length >= fullRequired)
        {
280→            // Simple full-canvas mock fill for invalid ECM display (gfx region black per VICE :133-141; borders 0 for checkpoint).
            // Real path in shim will use vicii raster buffers for authentic.
            int sampleGfx = (100 * FullW + 160) * 4;
            if (sampleGfx + 3 < fullRequired)
            {
                bgraBuffer[sampleGfx + 0] = 0;
                bgraBuffer[sampleGfx + 1] = 0;
                bgraBuffer[sampleGfx + 2] = 0;
                bgraBuffer[sampleGfx + 3] = 0xFF;
            }

### Turn 133 (Function)
[tool_response]
exit: 0


### Turn 134 (Assistant)
[tool_request: run_terminal_command]
- command: Get-ChildItem 'F:\GitHub\vice-sharp\native\vice\vice\src' -Recurse -Filter 'flash040.c' | Select-Object FullName

- description: Locate flash040.c
[tool_request: grep]
- pattern: flash040core_store|MAGIC|0xAA|0x55|BYTE_PROGRAM|CHIP_ERASE|sector
- path: F:\GitHub\vice-sharp\native\vice\vice\src\flash040core.c
- head_limit: 80
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
- limit: 80
[tool_request: grep]
- pattern: CaptureVisible|GetFrame|FrameBuffer|RenderNow|first_x|FrameWidth|FrameHeight
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- head_limit: 40
[tool_request: grep]
- pattern: CaptureVisibleFrame|GetVic20|Xvic
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core
- glob: *.cs
- head_limit: 40

### Turn 135 (Function)
[tool_response]
Error: F:\GitHub\vice-sharp\native\vice\vice\src\flash040core.c does not exist.
Similar entries in parent directory: core, flash040.h
Note: your current working directory is F:\GitHub\vice-sharp

### Turn 136 (Function)
[tool_response]
1→using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

10→/// <summary>
/// Native VICE xvic (VIC-20) library interop binding (vice_xvic.dll).
/// Separate from <see cref="ViceNative"/> / vice_x64 because VICE machine
/// globals cannot share a process image between C64SC and VIC20.
/// </summary>
public static unsafe partial class ViceNativeXvic
{
    private const string LibraryName = "vice_xvic";
    private static readonly string[] AlternateLibraryNames = ["vice_xvic"];
    private static readonly string[] RelativeSearchDirectories =
20→    [
        "",
        "native",
        Path.Combine("runtimes", "win-x64", "native"),
        Path.Combine("runtimes", "linux-x64", "native"),
        Path.Combine("runtimes", "osx-x64", "native"),
        Path.Combine("runtimes", "osx-arm64", "native")
    ];

    private static readonly Lazy<string?> ResolvedLibraryPath = new(FindLibraryPath);
30→
    // DllImport resolver is installed once on the Core assembly by ViceNative
    // (combined vice_x64 + vice_xvic). Do not call SetDllImportResolver here.

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;

    /// <summary>Absolute path of the resolved vice_xvic library, or null.</summary>
    public static string? ResolvedPath => ResolvedLibraryPath.Value;
    public static string AvailabilityMessage => ResolvedLibraryPath.Value is { } path
        ? $"Native VICE xvic library resolved at '{path}'."
40→        : "Native VICE xvic library not found. Build 'vice_xvic' via native/build-vice-shim-xvic.sh into the test output or a searched native directory.";

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create")]
    public static partial IntPtr Create();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create_model", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr CreateModel(string modelSelector);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_destroy")]
    public static partial void Destroy(IntPtr instance);
50→
    [LibraryImport(LibraryName, EntryPoint = "vice_machine_reset")]
    public static partial void ResetNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_step_cycle")]
    public static partial void StepNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_read_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int ReadSnapshotNative(IntPtr instance, string path);

60→    [LibraryImport(LibraryName, EntryPoint = "vice_machine_write_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WriteSnapshotNative(IntPtr instance, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_ram")]
    private static partial byte PeekRamNative(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_bus")]
    private static partial byte PeekBusNative(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_disk", StringMarshalling = StringMarshalling.Utf8)]
70→    private static partial int AttachDiskNative(IntPtr instance, uint unit, uint drive, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_detach_disk")]
    private static partial int DetachDiskNative(IntPtr instance, uint unit, uint drive);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_set_keyboard_matrix_key")]
    private static partial int SetKeyboardMatrixKeyNative(IntPtr instance, int row, int column, int pressed);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_a")]
    public static partial byte GetA(IntPtr instance);
80→

### Turn 137 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 11 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
73:    /// sets <c>first_x = 48</c>; paper at full-line 96 maps to canvas x 48.
211:        EnsureFrameBufferSize();
251:    public byte[] FrameBuffer => _frameBuffer;
252:    public int FrameWidth => _frameWidth;
253:    public int FrameHeight => _frameHeight;
304:        EnsureFrameBufferSize();
570:        EnsureFrameBufferSize();
652:        // Horizontal start is VICE viewport first_x (video_viewport_resize + video_canvas_refresh),
741:    public void RenderNow()
743:        EnsureFrameBufferSize();
798:    private void EnsureFrameBufferSize()
</workspace_result>

### Turn 138 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 15 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
15:public static unsafe partial class ViceNativeXvic
100:    public static partial void GetVic20VideoState(IntPtr instance, ref ViceNative.ViceVic20VideoState state);
359:            ViceNativeXvic.GetVicState(_instance, ref state);
375:        public Vic20VideoLockstepState GetVic20VideoState()
378:            ViceNativeXvic.GetVic20VideoState(_instance, ref state);
405:            ViceNativeXvic.GetCiaState(_instance, ciaIndex, ref state);
426:            ViceNativeXvic.GetCpuPipelineState(_instance, ref state);
447:            ViceNativeXvic.Destroy(_instance);
453:            ViceNativeXvic.GetVicState(_instance, ref state);

F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.cs
31:        // Combined resolver: vice_x64 (this class) and vice_xvic (ViceNativeXvic).
46:            var path = ViceNativeXvic.ResolvedPath;
195:    public static partial int CaptureVisibleFrame(IntPtr instance, [Out] byte[] buffer, int length, out int width, out int height);
265:        if (ViceNativeXvic.IsVic20ModelSelector(modelSelector))
266:            return ViceNativeXvic.CreateInstance(modelSelector);
844:        public Vic20VideoLockstepState GetVic20VideoState()
</workspace_result>

### Turn 139 (Function)
[tool_response]
exit: 0


### Turn 140 (Assistant)
[tool_request: run_terminal_command]
- command: Get-ChildItem 'F:\GitHub\vice-sharp\native\vice' -Recurse -Filter 'flash040*' -ErrorAction SilentlyContinue | Select-Object FullName

- description: Find all flash040 source files
[tool_request: grep]
- pattern: CaptureVisible|IViceNative
- path: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
- head_limit: 30
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
- offset: 1
- limit: 120
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- offset: 640
- limit: 100
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\docs\audit-vic20-vs-vice-2026-08-07.md
- offset: 110
- limit: 40

### Turn 141 (Function)
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
    /// Side-effect-free peek of the full native address space (RAM/ROM/I/O).
    /// Used for Vic20 lockstep samples of VIA ($9110/$9120) and VIC-I ($9000).
    /// C64 implementations may fall back to <see cref="PeekRam"/>.
    /// </summary>
    byte PeekBus(ushort address);

50→    /// <summary>
    /// Load a VICE snapshot (.vsf) into the native machine, resuming it from the
    /// staged state. The cycle counter is re-baselined so <see cref="GetState"/>
    /// reports cycles elapsed since the load point. Returns 0 on success.
    /// </summary>
    int ReadSnapshot(string path);

    /// <summary>
    /// Write the current native machine state to a VICE snapshot (.vsf).
    /// Returns 0 on success.
60→    /// </summary>
    int WriteSnapshot(string path);

    /// <summary>
    /// Get current full machine state
    /// </summary>
    MachineState GetState();

    /// <summary>
    /// Get current native VIC-II timing state for lockstep diagnostics.
70→    /// </summary>
    NativeVicState GetVicState();

    /// <summary>
    /// VIC-I video pipeline snapshot for every-cycle lockstep (xvic only).
    /// C64 native implementations throw <see cref="NotSupportedException"/>.
    /// </summary>
    Vic20VideoLockstepState GetVic20VideoState();

    /// <summary>
80→    /// Get the main-CPU resume/pipeline state (TR-LOCKSTEP-VSF-001): the
    /// .vsf-restored in-flight context beyond the plain register file (last
    /// opcode info, pending BA-low stall flags, the 6510 processor port that
    /// selects ROM/IO banking, and the interrupt-status clocks). Valid right
    /// after <see cref="ReadSnapshot"/> and at any paused cycle boundary; used
    /// to stage a managed machine so snapshot-resumed lockstep aligns from
    /// cycle 0.
    /// </summary>
    NativeCpuPipelineState GetCpuPipelineState();

90→    /// <summary>
    /// Get a CIA's resume state (TR-LOCKSTEP-VSF-001): ports/DDRs, live timer
    /// counters plus reload latches, control registers, latched interrupt
    /// flags and the ICR enable mask, as restored from the .vsf CIA modules.
    /// Index 0 = CIA1 ($DC00), 1 = CIA2 ($DD00).
    /// </summary>
    NativeCiaState GetCiaState(int ciaIndex);
}

/// <summary>
100→/// CIA resume state exported by the native shim (TR-LOCKSTEP-VSF-001):
/// everything a managed MOS 6526 needs to resume a .vsf mid-run - the port
/// output latches and data directions, the LIVE timer counters (ciat_read_timer)
/// with their reload latches (ciat_read_latch), the control registers, the
/// latched interrupt flags and the ICR interrupt-enable mask (ciacore
/// irq_enabled).
/// </summary>
public readonly struct NativeCiaState
{
    /// <summary>Port A output latch ($DC00 written value).</summary>
110→    public byte PortA { get; init; }

    /// <summary>Port B output latch ($DC01 written value).</summary>
    public byte PortB { get; init; }

    /// <summary>Port A data direction ($DC02).</summary>
    public byte DdrA { get; init; }

    /// <summary>Port B data direction ($DC03).</summary>
    public byte DdrB { get; init; }
120→

### Turn 142 (Function)
[tool_response]
640→                if (_displayXStart > 0)
                    FillRow(line, 0, Math.Min(_displayXStart, lineW), border);
                if (_displayXStop > 0 && _displayXStop < lineW)
                {
                    var rightStart = _displayXStop;
                    // display_xstop is exclusive-ish end of gfx in VICE (blank from xstop to end).
                    FillRow(line, rightStart * 4, lineW - rightStart, border);
                }
            }
        }
650→
        // Crop full line into normal-border output (vic_set_geometry display_width).
        // Horizontal start is VICE viewport first_x (video_viewport_resize + video_canvas_refresh),
        // not full-line x=0 (that invent wiped the right border on READY).
        var first = IsNtsc ? NtscNormalFirstDisplayedLine : PalNormalFirstDisplayedLine;
        var last = IsNtsc ? NtscNormalLastDisplayedLine : PalNormalLastDisplayedLine;
        if (rasterLine < first || rasterLine > last)
            return;

        var fbY = rasterLine - first;
660→        if ((uint)fbY >= (uint)_frameHeight)
            return;

        var gfxX = ResolveGfxXForViewport();
        var gfxW = ResolveGfxWForViewport();
        var firstX = ComputeViewportFirstX(gfxX, gfxW, _frameWidth, lineW, gfxAreaMoves: true);
        var copyW = Math.Min(_frameWidth, Math.Max(0, lineW - firstX));
        if (copyW <= 0)
            return;
        var src = firstX * 4;
670→        var dst = fbY * _frameWidth * 4;
        Buffer.BlockCopy(line, src, _frameBuffer, dst, copyW * 4);
        // If the slice is short of canvas width, pad with border (should not happen for READY).
        if (copyW < _frameWidth)
            FillRow(_frameBuffer, dst + copyW * 4, _frameWidth - copyW, border);
    }

    /// <summary>
    /// Full-line gfx X for viewport: live <c>display_xstart</c> after open_h, else
    /// <c>$9000</c> origin (VICE geometry default before first open).
680→    /// </summary>
    private int ResolveGfxXForViewport()
    {
        if (_displayXStart > 0)
            return _displayXStart;
        var originCycle = _regs[0] & 0x7F;
        var screenW = FullScreenWidthUnits;
        return Math.Min(originCycle * 4, screenW) * PixelWidth;
    }

690→    /// <summary>
    /// Full-line gfx width for viewport: live text cols, else pending / $9002.
    /// </summary>
    private int ResolveGfxWForViewport()
    {
        var cols = _textCols;
        if (cols <= 0)
            cols = _pendingTextCols;
        if (cols <= 0)
            cols = Math.Min(_regs[0x02] & 0x7F, MaxTextCols);
700→        if (cols <= 0)
            cols = 22;
        return cols * 8 * PixelWidth;
    }

    public void Reset()
    {
        ResetRegisters();
        // VICE vic_reset: raster_cycle = 6.
        _cycleInLine = 6;
710→        _rasterLine = 0;
        _fetchState = FetchState.Idle;
        _bufOffset = 0;
        _area = VicArea.Idle;
        _memptr = 0;
        _memptrInc = 0;
        _ycounter = 0;
        _rowCounter = 0;
        // VICE vic_init: pending_text_cols=22, text_lines=23; text_cols assigned on open_h.
        _textLines = 23;
720→        _textCols = 0;
        _pendingTextCols = 22;
        _charHeight = 8;
        // VICE raster_reset: blank_this_line = 0 (raster.c).
        _lineWasBlank = false;
        _blankThisLine = false;
        _displayXStart = 0;
        _displayXStop = 0;
        Array.Clear(_cbuf);
        Array.Clear(_gbuf);
730→        Array.Clear(_frameBuffer);
        if (_lineBuffer.Length > 0)
            Array.Clear(_lineBuffer);
    }

    /// <summary>
    /// Drive one full raster through <see cref="Tick"/> so the cycle/draw path
    /// is the display truth (tests and hosts that do not wait for live frames).
    /// Program $9000/$9001 (origin) and geometry regs before calling when you
    /// need a specific text placement (KERNAL PAL uses $9000=12, $9001=38).

### Turn 143 (Function)
[tool_response]
110→
### Keyboard / joystick

- **Status:** **Partial**

### IEC serial

- **Status:** **Exact** for idle PA levels only; live bus **Missing**

### Datasette / IEEE / rsuser / printer
120→
- **Status:** **Missing** (Explicit)

### Cartridges

- **Status:** **Stub** (MVP FE3/Ultimem/MegaCart); full VICE cart suite **Missing**. No Exact claim.

### Snapshots

- **Status:** **Missing** (Explicit)
130→
### Machine glue

- **Status:** **Partial**

---

## Invent list (closed vs remaining)

| # | Item | Status |
140→|---|------|--------|
| 1 | Pre-cropped FB + canvas X = display_xstart (`src=0`) | **Closed** — full line + `ComputeViewportFirstX` |
| 2 | Continuous invent paper strip | **Closed** — space cells via draw slots (slot 0 bg) |
| 3 | Eager live column latch | **Closed** — pending only until open_h |
| 4 | Idle IEC as full board serial | **Remaining** if overclaimed; matrix says idle Exact only |
| 5 | Cart MVP as VICE ports | **Remaining** Stub — Explicit not Exact |
| 6 | Sound store-only as sound | **Remaining** Missing — Explicit |
| 7 | VicIStub alternate surface | Check tree; non-product if unused |
| 8 | `RenderNow` harness driver | Allowed harness-only; not a VICE API claim |
| 9 | Via6522 incomplete SR modes | **Remaining** Partial |

### Turn 144 (Function)
[tool_response]
<workspace_result workspace_path="F:\GitHub\vice-sharp">
Found 1 matching lines
F:\GitHub\vice-sharp\src\ViceSharp.Abstractions\IViceNative.cs
6:public interface IViceNative : IDisposable
</workspace_result>

### Turn 145 (Function)
[tool_response]
exit: 0

FullName
--------
F:\GitHub\vice-sharp\native\vice\vice\src\flash040.h
F:\GitHub\vice-sharp\native\vice\vice\src\core\flash040core.c
F:\GitHub\vice-sharp\native\vice\vice\src\core\flash040core.o
F:\GitHub\vice-sharp\native\vice\vice\src\core\.deps\flash040core.Po



### Turn 146 (Assistant)
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\core\flash040core.c
- limit: 250
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice\vice\src\core\flash040core.c
- offset: 250
- limit: 250
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- offset: 60
- limit: 50
[tool_request: grep]
- pattern: PalNormal|NtscNormal|FrameWidth|display_width|448|284
- path: F:\GitHub\vice-sharp\src\ViceSharp.Chips\Vic\Mos6561.cs
- head_limit: 40
[tool_request: grep]
- pattern: TryCapture|CaptureVisible|GetFrame
- path: F:\GitHub\vice-sharp\src\ViceSharp.Core\ViceNative.Xvic.cs
[tool_request: read_file]
- target_file: F:\GitHub\vice-sharp\native\vice-shim-vic20.c
- offset: 1100
- limit: 100


[... TRUNCATED at 524288 bytes, 344 turns omitted ...]
