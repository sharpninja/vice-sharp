# Local Grok hostile validation: handoff-completion Phase 4 green gate

Act as the independent hostile reviewer for `F:\GitHub\vice-sharp`. This is a project-implementation green gate for the approved Phase 4 VIC-20 settings-visibility slice. Work locally and read-only. Do not edit repository files, requirements, TODOs, plans, HANDOFF, or git state. Do not use web search, remote repositories, cloud execution, cloud runners, or subagents.

Before evaluating any claim, execute the complete add-profile skill. Read `C:\Users\kingd\.grok\skills\add-profile\SKILL.md`, then use PowerShell.MCP and `Show-TextFiles` to read every current markdown file under `C:\Users\kingd\.claude\profile` in full except `add-profile.grok.md`. There are currently 22 included files, counting `add-profile.md`. Do not use `read_file` for the profile load. Record the actual count and the PowerShell.MCP receipt in the verdict.

The review has no turn limit and no artificial time limit. Continue until the evidence is fully inspected and one terminal verdict is ready.

## Governing scope

- Review Phase 4 in `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`, especially lines 257-285.
- UWP/Xbox implementation is out of scope. Legacy requirements remain active and are not retired.
- This slice concerns Avalonia visibility notification and VIC-20 draft preservation. It does not prove xvic parity or emulator behavioral parity with VICE.
- `FR-UISETVIS-001` and `TEST-UISET-003` remain pending. Do not approve a broader done claim.

## Evidence to inspect

- `tests/ViceSharp.TestHarness/Ui/Vic20SettingsVisibilityNotificationTests.cs`
- `tests/ViceSharp.TestHarness/Ui/Vic20SettingsUiSurfaceTests.cs`
- `src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs`
- `src/ViceSharp.Avalonia/Views/SettingsView.axaml`
- `TestResults/handoff-completion/phase4-red/phase4-red.trx`
- `TestResults/handoff-completion/phase4-green/phase4-green.trx`
- `docs/receipts/hv/20261003T170113Z-handoff-phase4-red-local-grok-continuation.request.jsonl`
- `docs/receipts/hv/20261003T170113Z-handoff-phase4-red-local-grok-continuation.response.jsonl`
- `docs/receipts/hv/20261003T170113Z-handoff-phase4-red-local-grok-continuation.verdict.md`
- `docs/requirements/functional/FR-Grok-Completion.md`
- `docs/requirements/test/TEST-Requirements.md`
- Current `git status`, scoped `git diff`, and `git diff --check`.

## Claims to attack

1. The retained red TRX is a real Release run of exactly the four new notification tests plus four existing VIC-20 settings-surface tests: total 8, passed 5, failed 3, skipped 0. The three failures are specifically missing `IsVic20Selected` notifications on direct profile selection, host-refresh assignment when the later settings GET fails, and `RestoreSettings`. The VIC-20 draft-preservation case and all four existing surface tests passed.
2. The production change is minimal and correctly placed: after a successful direct `SelectedMachineProfile` update, after the host-refresh backing-field assignment, and in `RestoreSettings` after `SelectedMachineProfile`. The already-existing `ApplySettingsFromHost` notification remains singular. There is no accidental duplicate notification and no unrelated behavior change from this slice.
3. The retained green TRX is a Release run of the same eight tests: total 8, executed 8, passed 8, failed 0, skipped 0. Its SHA-256 is `9147eaaa4083ac21359524f3b853ecdfd916a75379a39a49c56e8f6da7560550`.
4. The tests are behaviorally meaningful: they observe the property name bound by `SettingsView.axaml`, exercise both C64-to-VIC20 and VIC20-to-C64 direct transitions, cover the refresh early-return path, cover restore/revert, and prove non-default VIC-20 memory/cart draft values survive a family round trip.
5. No Xbox or UWP source was changed for this Phase 4 implementation. Their legacy requirements were not retired.
6. The parent makes no emulator-parity claim and no completion claim for `FR-UISETVIS-001`, `TEST-UISET-003`, Phase 3, or the overall handoff plan.
7. `git diff --check` exits 0 for the current worktree.
8. Request/response JSONL and the full prior red verdict are retained and the full result was persisted to the active MCP Session Log. No TODO or requirement was marked done.
9. The prior red reviewer transcript used Grok `read_file` for profile files after a PowerShell.MCP attempt was cancelled, despite the add-profile rule requiring PowerShell.MCP. Evaluate this process defect explicitly. Do not conceal or waive it. Decide whether the current compliant review can supersede the prior process defect while preserving the already-retained red execution evidence. If not, return DISAGREE and state the minimum honest remediation.

Evaluate the mandatory hostile surfaces:

- A: every requested factual claim above.
- B: workspace rules, PowerShell/no-Python, receipts, Byrd red/green sequencing, and reviewer compliance.
- C: requirement/AC traceability for this slice without inventing a done claim.
- D: Phase 4 and the overall plan holistically, including open Phase 3 work.

Return one complete Markdown verdict containing `OverallVerdict`, integer `AccuracyScore` and `CompletenessScore`, PASS/FAIL/UNKNOWN counts, every finding, and direct dispositions for claims 1-9 and surfaces A-D. `AGREE` is permitted only with zero FAIL, zero UNKNOWN, and both scores at least 98. End with one single-line JSON object with keys `OverallVerdict`, `AccuracyScore`, `CompletenessScore`, `PassCount`, `FailCount`, and `UnknownCount`.
