# Hostile red-gate review: Handoff completion plan Phase 4

You are the independent hostile validator for the Phase 4 VIC-20 settings visibility red gate in the local ViceSharp workspace F:\GitHub\vice-sharp.

Execution constraints:
- This is a local Grok CLI hostile review. Do not use web search, cloud runners, remote agents, or subagents.
- Read/search the local repository and named evidence only.
- Do not modify product code, tests, requirements, TODOs, plans, HANDOFF.md, or receipts.
- There is no turn limit or artificial time limit. Continue until you can issue a terminal verdict.
- Do not call MCP. The parent Codex turn owns MCP logging.
- UWP and Xbox implementation are out of scope. Do not require or propose changes to those projects.

Mandatory context:
1. Read C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md in full and apply its adversarial A-D surfaces.
2. Read the complete plan C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md, focusing on lines 257-285.
3. Read F:\GitHub\vice-sharp\AGENTS.md and the relevant Phase 4 code/tests.

Evidence to verify:
- New red test file:
  F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Ui\Vic20SettingsVisibilityNotificationTests.cs
- Existing structural UI tests:
  F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Ui\Vic20SettingsUiSurfaceTests.cs
- Production view model:
  F:\GitHub\vice-sharp\src\ViceSharp.Avalonia\ViewModels\AttachPanelViewModel.cs
- Retained red TRX:
  F:\GitHub\vice-sharp\TestResults\handoff-completion\phase4-red\phase4-red.trx

Exact red-run claim:
- Command:
  dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20SettingsVisibilityNotificationTests|FullyQualifiedName~Vic20SettingsUiSurfaceTests" --results-directory .\TestResults\handoff-completion\phase4-red --logger "trx;LogFileName=phase4-red.trx"
- Expected observed result: total 8, passed 5, failed 3, skipped 0.
- The three expected failures must be:
  1. direct SelectedMachineProfile family change omits IsVic20Selected notification;
  2. RefreshSettingsAsync host profile assignment omits the notification even when the following settings GET fails;
  3. RestoreSettings/RevertSettings family change omits the notification.
- The draft-preservation test must pass and prove the VIC-20 memory preset and expansion-cart kind/preset survive C64/VIC-20 switches.
- Existing UI surface tests must pass.
- The red tests must fail for the intended production gaps, not because of compile errors, bad mocks, missing prerequisites, skips, or unrelated behavior.
- No UWP/Xbox source change is required or authorized.

Required review:
- Surface A: validate every exact test/evidence claim above.
- Surface B: validate local-only, PowerShell/no-Python, BDPv4 red-before-product order, zero skips, and scoped files.
- Surface C: validate that the tests trace to FR-UISETVIS-001 and TEST-UISET-003 without claiming either requirement complete.
- Surface D: validate that this is the correct Phase 4 red gate and that the smallest planned production fix is still confined to notification emission in AttachPanelViewModel.
- Explicitly state whether any test is overfit, vacuous, or missing a required edge case.
- Explicitly state that this UI notification slice neither proves nor changes emulator behavioral parity with VICE.

Output contract:
- OverallVerdict AGREE or DISAGREE.
- AccuracyScore and CompletenessScore as integer percentages.
- PASS, FAIL, UNKNOWN counts.
- Every FAIL and UNKNOWN finding with exact file/line or TRX evidence.
- AGREE only when every applicable claim passes, failCount=0, unknownCount=0, and both scores are at least 98.
- End with one single-line JSON object containing OverallVerdict, AccuracyScore, CompletenessScore, PassCount, FailCount, UnknownCount.
