# ViceSharp state review

Reviewed 2026-09-06, America/Chicago. Reviewer: Codex (gpt-6). Scope: current checkout and the latest Grok stopping point. No product source, plans, requirements, or TODO status changed by this review.

## Assessment

The current checkout builds and the executed focused tests pass. It is not a clean, fully validated project closeout. Grok's next C64 settings implementation is awaiting operator approval. The latest Grok handoff turn is incomplete.

## Findings

1. **P2: RemoteControl CLI depends on a sibling checkout and is excluded from normal solution validation.** `tools/ViceSharp.RemoteControlCli/ViceSharp.RemoteControlCli.csproj:20` references `../../../Avalonia.RemoteControl/src/Avalonia.RemoteControl.Client/Avalonia.RemoteControl.Client.csproj`. A standalone ViceSharp checkout cannot resolve this dependency without obtaining that sibling repository. The CLI and `tests/ViceSharp.RemoteControlCli.Tests` are absent from `ViceSharp.slnx`; a solution build/test therefore omits them. The separate CLI test run succeeded here, with output proving it built the sibling project's Client and Protocol assemblies. Use a reproducible dependency and integrate the intended build/test gate before treating the CLI as portable.

2. **P2: Canonical handoff does not record the actual stopping point.** `HANDOFF.md:1` still advertises August 17; its last write is September 4 at 03:45:20 UTC. `HANDOFF.md:107` resumes from pixel parity, not the later Settings/RemoteControl work and pending C64 plan. Live MCP session `GrokCode-20260904T020555Z-continue-plan` ends with `req-20260906T143000Z-015-write-handoff-exit`, title `Write HANDOFF.md and exit`, status `in_progress`, response null. The immediately preceding turn says the C64 plan was written and implementation stopped for approval. This makes root handoff alone insufficient for a reliable continuation.

3. **P2: C64 planning records are not yet a complete repository handoff.** Live `PLAN-C64SET-001` is open, but its functionalRequirements and technicalRequirements arrays are empty and its reference is unset. The effective layer does contain pending `FR-MACHINEPICKER-001` (3 AC), `FR-UISETVIS-001` (2 AC), `TR-UIAXAML-PICKER-001` (1 AC), and `TEST-UISET-003` (3 AC). None of these four IDs occurs in the current canonical `docs/requirements/*.md` tree. The detailed plan is outside the repo at `C:/Users/kingd/.grok/sessions/F%3A%5CGitHub%5Cvice-sharp/01a06a23-81e8-7b41-8fa0-4773aa19757c/goal/plan-c64-settings.md`; line 7 says approval is required. Reconcile the plan reference, requirement links, and canonical docs before the implementation phase.

4. **Validation weakness: native pixel tests can pass without exercising native VICE.** The new BGRA cases return normally when `ViceNativeXvic.IsAvailable` is false (`tests/ViceSharp.TestHarness/Vic20/Vic20PixelLockstep.cs`, including lines 147 and 203 cases). This produces a passing test without a comparison on machines lacking xvic. That did not occur in this review: all three TRX files contain actual xvic create/reset/step output. A required native parity gate should explicitly prove native availability.

## Current state

- Branch `main`; HEAD and live `origin/main` both `df6da6bb4e19e339da2e6076c185dba24cc860ce`, verified with `git rev-parse HEAD` and `git ls-remote origin refs/heads/main`.
- 37 tracked modified files and 360 untracked `git status --porcelain=v1` entries at review time. Entries include grouped directories, so this is not an untracked file count. Much of the untracked material is older probe/review evidence; no cleanup or commit was performed.
- Main uncommitted work: VIC-20 native BGRA palette/opaque frame initialization; dynamic video canvas and stride handling; VIC-20 settings propagation over gRPC; Settings RemoteControl inventory/action gates; Warp speed/status sampling; CLI tooling and tests; docs and requirement changes.
- MCP: 33 open TODOs. `PLAN-REMOTECTRL-001` remains open with TEST-REMOTECTRL-001 and app-launch verification tasks false. `PLAN-C64SET-001` remains open.
- Latest completed implementation-session turn `req-20260906T141930Z-014-c64-settings-type-variant` records planning only. The C64 plan calls for Computer/Model dropdowns, family-specific visibility, c64/c64c/ntsc RC validation, red/green tests, and hostile validation. It expressly does not complete PLAN-REMOTECTRL-001.
- Prior September 4 hostile reviews had failures, followed by `docs/receipts/hostile-validator-20260904T215905Z.md` AGREE for the narrow warp/preset requirements and inventory fixes. That is historical scoped evidence, not a fresh verdict on this review.

## Executed validation

All commands ran from `F:/GitHub/vice-sharp` through PowerShell.MCP. Logs and TRX are retained in this directory.

- `dotnet build .\ViceSharp.slnx -c Release --no-restore -v minimal -maxcpucount:1 /p:UseSharedCompilation=false`: exit 0; 0 errors, 1 AVLN5001 warning for obsolete TextBox.Watermark at SettingsView.axaml:76. See `solution-build.log`.
- `dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~VideoSurface|FullyQualifiedName~Vic20Settings|FullyQualifiedName~WarpModeTests|FullyQualifiedName~AttachPanelViewModelTests|FullyQualifiedName~GrpcHostServiceAdaptersTests' --results-directory '.\validation-output\codex-grok-state-review-20260906\tests' --logger 'trx;LogFileName=focused.trx' --diag '.\validation-output\codex-grok-state-review-20260906\focused-vstest.log' --blame-hang --blame-hang-timeout 90s`: exit 0; 132 passed, 0 failed, 0 skipped.
- `dotnet test .\tests\ViceSharp.RemoteControlCli.Tests\ViceSharp.RemoteControlCli.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=cli.trx' --results-directory '.\validation-output\codex-grok-state-review-20260906\tests'`: exit 0; 6 passed, 0 failed, 0 skipped. These are parser tests, not live RPC tests.
- Three separate `dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~Vic20PixelLockstep.<case> --results-directory .\validation-output\codex-grok-state-review-20260906\tests --logger trx;LogFileName=<case>.trx --blame-hang --blame-hang-timeout 60s` invocations, with logger argument quoted at execution: Bgra_ReadyPal_SequenceEqual, Bgra_BusyPal_SequenceEqual, Bgra_ReadyNtsc_SequenceEqual. Each exit 0, 1 passed, 0 failed, 0 skipped. Native xvic execution confirmed in TRX. Each used a fresh process as required by the known native lifecycle problem.
- Total executed tests: 141 passed, 0 failed, 0 skipped across these five runs. This is focused validation, not the full suite.
- `git diff --check`: exit 0.
- `tools/check_requirement_traceability.ps1`: exit 0, but audit reports 195 canonical IDs, 125 referenced, 70 unreferenced; 1011 noncanonical IDs in source/tests. This informational exit is not evidence of complete coverage. See `traceability.log`.

## Limits and unresolved validation

- Full suite not rerun. `HANDOFF.md:29` records an older broad run at 1155 passed, 10 skipped, 6 failed, followed by a native hang; it also records a later XMLDocs fix. The remaining performance/boundary/snapshot/native lifecycle issues require current verification before project-wide readiness can be claimed.
- No fresh live desktop interaction, deployment, packaging, commit, or push was performed.
- Independent Grok hostile audit failed before reviewing anything: HTTP 402 Payment Required, Grok Build usage balance exhausted. Exact output: `hostile-stderr.log`; prompt: `hostile-brief.txt`. No reviewer-produced OverallVerdict, PASS/FAIL/UNKNOWN counts, receipt, or session-persistence proof was obtained. Do not represent the old AGREE receipts as validation of this report.
- Codex self-assessment: accuracy 95/100 for the bounded claims above, backed by live source/store/test evidence; completeness 80/100 because independent review, full-suite health, and live UI validation remain unverified.

## Audit linkage

Primary review persisted in MCP session `Codex-20260906T165605Z-plugin-session`, turn `req-20260906T165615Z-prompt-73e9`. Server readback before closeout confirmed the review title, five actions, one decision and one dialog entry. Subsequent turns record the local timestamp correction and final report delivery.
