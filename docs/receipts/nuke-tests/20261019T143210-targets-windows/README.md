# PLAN-NUKETESTS-001 steps 5 and 6 on PAYTON-LEGION2

Machine PAYTON-LEGION2, branch `develop` at `fa303b4448791489c8f1e7e920a1638d906a4a1b` (fast-forwarded from `6fb84af` at 2026-10-19T14:31:21; the local PaletteId edit was dropped for the identical committed `f3c227f`, which differs only in its doc-comment wording). dotnet 10.0.402. Claude Code claude-opus-5-5, session `ClaudeCode-20261019T152439Z-plugin-session`, request `req-20261019T152609Z-prompt-b96d`. Remediation phase: no hostile review (D3).

## Step 5: Windows gates (2026-10-19T14:32:10 to 14:32:59)

Script `../Invoke-NukeTestsStep5Windows.ps1` (parser 0 errors, Invoke-ScriptAnalyzer Warning/Error 0, `-WhatIf` before the real run). Logs, copied TRX files, `run.log` and `summary.json` are in this folder.

- `./build.ps1 BuildTest`: exit 0. `ViceSharp.Build.Tests.trx` total 96, executed 96, passed 96, failed 0, not executed 0. Nuke log: "BuildTest: selected 96, passed 96, host filter Host!=Linux". The TRX holds 0 `UserLocalDeployment` cases (excluded by filter, not skipped) and 1 `LinuxHostTrait_MarksOnlyTheDeploymentContract` case, the guard that pins the excluded count at 13.
- `dotnet test tests/ViceSharp.Library.Tests -c Release`: exit 0, 124 total, 124 passed, 0 failed, 0 not executed.
- `./build.ps1 RommDesktopTest`: exit 0. `ViceSharp.Heads.Tests` 61/61 passed; `ViceSharp.Library.Tests` 124/124 passed. No host filter.
- Native inventory before and after: 2,539 entries, SHA256 `C7848A24867CD60C49FE6088692A6E4565240868C89D811F806B429CDB3CBFC5`, unchanged.
- Same as Linux: each Nuke start (BuildTest, RommDesktopTest) logs one `Microsoft.Build.Exceptions.InvalidProjectFileException` "... Could not load file or assembly 'NuGet.Frameworks, Version=7.9.0.0' ...". Both targets still exit 0 with full counts.

## Step 6: requirement capture (2026-10-19T14:35:25)

Script `../Invoke-NukeTestsStep6Capture.ps1` (parser 0, analyzer 0, `-WhatIf` diff reviewed first). Journal and plan in `../20261019T143442-requirement-capture-legion2/`. The first WhatIf stopped on an ambiguous anchor (the target-list text also appears in section 3); the anchor was narrowed to the section 1 line before any write.

- TR-BUILD-001: criteria 11 to 12; new `ac-12` is the plan section 3 text verbatim, `isSatisfied: false`. Description, 96 to 99 lines: `BuildTest` added to the Technical Specification section 1 target list; a `BuildTest` bullet added after the `CiTest` bullet in section 4 (**wording composed by the agent** from ac-12 and the target's own description; the plan states intent only); item 12 added to the description's Acceptance Criteria list so prose and structured criteria agree; the plan's Verification Method sentence added verbatim. Readback: description, criteria and every stable field (title, status, priority, area, subarea, notes, scopeStartLayerKey) match.
- TEST-BUILD-001 created after a probe showed no TEST-BUILD record exists: title "Nuke build tests", priority high, area BUILD, status pending, criteria ac-1 to ac-4 = plan section 3 items (1) to (4), all unsatisfied; the description carries the plan's traceability (TR-BUILD-001 ac-10, ac-11, ac-12; TR-PLAT-001 ac-8). Readback matches.
- Not done: no mapping row was added. Mappings are keyed by FR, and section 3 says "No FR changes" and names no mapping. TEST-BUILD-001 therefore does not appear in FR-keyed traceability exports. This is a gap for the plan owner to decide.
- Mirror to LAB-OMARCHY: vice-sharp-b1's step.

## Self-assessment

Accuracy 97: every count is from a TRX or journal named here, and the one composed text (the section 4 bullet) is labeled. Completeness 93: the mapping question is open; the NuGet.Frameworks exception is observed, not diagnosed; the LAB-OMARCHY mirror is pending.
