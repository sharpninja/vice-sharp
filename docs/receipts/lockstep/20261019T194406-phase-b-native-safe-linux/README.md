# Phase B exit evidence, LAB-OMARCHY (Linux), 2026-10-19T19:44

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 4 Phase B, exit amendment "the three native-safe projects are also green unfiltered on LAB-OMARCHY (they are native-safe by construction), recorded in the same receipt", and section 11 sequence step 8 under D3 (no hostile validation until the scope is code complete; the Phase B review is deferred to step 12). Goal TODO `GOAL-LOCKSTEP-REMEDIATION-001`. The Windows half of Phase B (packet repin and disclosure, Windows native-safe run, holistic self-assessment of the Phase 1 scope) is executed on PAYTON-LEGION2 under the brief of 19:43 and lands in its own receipt.

Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, branch `develop`, HEAD `b660bee9ffa5f80a2c45caa29cde8b62b5751529`, dotnet 10.0.402, between 2026-10-19T19:44:06 and 19:44:22 local, by `~/codex-dispatch/scripts/Invoke-NativeSafeSuites.ps1` (PowerShell, parser and analyzer clean). Command per project (`run.log`):

`dotnet test tests/ViceSharp.<Project>.Tests/ViceSharp.<Project>.Tests.csproj -c Release --nologo --logger "trx;LogFileName=<Project>.trx" --results-directory <this folder>`

Results (passed, failed, skipped; TRX beside this file):

- OracleContract: 89, 0, 0 (`OracleContract.trx`).
- VsfCompatibility: 199, 0, 0 (`VsfCompatibility.trx`).
- NativeClockProbe: 69, 0, 0 (`NativeClockProbe.trx`).

Native tree: `native/vice` gitlink `cc67418cba58e71d8e70fd353e9de47199ad2075`; `git -C native/vice status --porcelain=v1 -uall` before and after the three runs is 163 entries (8 tracked changes, 155 untracked), SHA256 `A31D0C086A9A36D1...` both times (`native-inventory-before.txt`, `native-inventory-after.txt`): unchanged. No TestHarness build, no `Vice.Native.proj`, no shim build.

Counts equal the Phase A Linux baseline of 2026-10-18 (89, 199, 69) and the Phase A Windows baseline of 2026-10-19 (89, 199, 69).

Not claimed: Phase B exit as a whole (it needs the LEGION2 receipt), any hostile review, any done state. `PLAN-VICEORACLE-001` task 2 stays open until the step 12 review.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 99%, accuracy 99%, completeness 98%
