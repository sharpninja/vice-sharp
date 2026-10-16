Resume the same local hostile-review session 01a102e6-097b-7753-aba2-ce6686a3d173 and perform the Phase 3a GREEN hostile gate for F:\GitHub\vice-sharp.

Mandatory:
1. Execute the complete add-profile skill again before review work. Through PowerShell.MCP only, read C:\Users\kingd\.codex\skills\add-profile\SKILL.md, then add-profile.md first and every other included profile markdown file. Read 22 total, excluding only add-profile.grok.md. Use Show-TextFiles for text. Do not use read_file, Get-Content, Python, Node, raw pwsh, or another shell.
2. Local-only, read-only product review. No web, remotes, cloud execution, cloud runners, subagents, repository/product/requirement/TODO/plan/HANDOFF/git edits.
3. Create a new complete GrokCode MCP session-log turn for this green gate through supported MCP tools or wrapper, never raw REST. Persist the full terminal verdict, every finding, scores, JSON, and receipt paths, read it back, and report the IDs.
4. No max-turns option, no turn limit, and no artificial time limit.
5. Treat the plan and requirements as authoritative. Default each claim to FAIL until current local evidence proves it.

Prior gate:
- Corrected red verdict: F:\GitHub\vice-sharp\docs\receipts\hv\20261003T132036-0500-phase3a-red-r2-local-grok.verdict.md
- Red OverallVerdict AGREE, Accuracy 100, Completeness 100, 13 PASS, 0 FAIL, 0 UNKNOWN.
- It authorized implementation only and required a direct validator call plus routing lockstep tests that returned on missing ViceNativeXvic through the same fail-closed gate.

Authoritative scope:
- Plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md lines 188-213, especially Slice 3a.
- FR-SNP-005 AC7.
- TEST-SNP-002 AC6.
- Canonical manifest: F:\GitHub\vice-sharp\docs\requirements\oracles\vic20-xvic-manifest.json.
- UWP/Xbox implementation remains out of scope. Requirements remain active and are not retired.

Implementation evidence:
- New validator: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\XvicOraclePrerequisites.cs
- Direct-call tests: F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\Vic20\XvicOraclePrerequisiteTests.cs
- Native VIC-20 test files changed only to replace missing-oracle returns with XvicOraclePrerequisites.ValidateCurrent():
  Vic20DivergeProbe.cs
  Vic20LockstepScaffoldTests.cs
  Vic20NativeAudioExportTests.cs
  Vic20NativeLockstepTests.cs
  Vic20PixelFrameTests.cs
  Vic20PixelLockstep.cs
  Vic20PixelLockstepDiag.cs
  Vic20SnapshotRoundTripTests.cs
  Vic20SoundLockstep.cs
  Vic20VideoLockstep.cs
  Vic20WorkloadLockstep.cs
- Code-verify build after edits returned status succeeded.
- rg current-state receipt: zero matches for if (!ViceNativeXvic.IsAvailable) under tests/ViceSharp.TestHarness/Vic20.
- git diff --check exit 0.
- The long pre-existing baseline remains live and was not canceled or restarted.

Test receipts:
1. Exact Slice 3a isolated Release filter:
   F:\GitHub\vice-sharp\TestResults\handoff-completion\phase3a-green-r2\phase3a-green-r2.trx
   total 7, executed 7, passed 7, failed 0, skipped/notExecuted/error/timeout/aborted 0.
   SHA256 D9A61D7B871F518F35AB4EA562A590F0C86D5B5964FFD078FD3EFDA97F3D8348.
2. Real pinned-oracle smoke from the same isolated assembly:
   F:\GitHub\vice-sharp\TestResults\handoff-completion\phase3a-current-oracle\phase3a-current-oracle.trx
   Vic20LockstepScaffoldTests.NativeXvic_StepAdvancesCycle_WhenOraclePresent.
   total 1, executed 1, passed 1, failed 0, skipped/notExecuted/error/timeout/aborted 0.
   SHA256 617C0ABCB50752328EC83A3FB87C89DB9CEC3B6AFA8AEDAE7A2703DBF80C0E75.
3. A prior non-isolated green attempt did not execute tests because the preserved baseline testhost held the normal Release output DLL. It is logged as a failed validation attempt, not passing evidence. The isolated run is the proof.

Prove or reject:
1. The red gate authorization and its only prior finding are respected.
2. XvicOraclePrerequisiteTests now call XvicOraclePrerequisites.Validate directly. Reflection and ExceptionDispatchInfo are gone.
3. The validator fails for missing xvic executable, missing vice_xvic native library, missing configured VIC-20 ROM, missing manifest, hash mismatch, and missing manifest-named workload, while accepting the complete fixture.
4. ValidateCurrent uses only the checked-in manifest, ViceNativeXvic.ResolvedPath, and explicit VICESHARP_ROM_PATH. It does not use MachineTestFactory or the repo-ROM fallback.
5. The checked-in manifest xvic executable and vice_xvic library hashes are enforced. All six required VIC-20 ROM names must be present in the manifest and under VICESHARP_ROM_PATH, and every manifest ROM hash is enforced.
6. Every distinct manifest workload program is required on disk for ValidateCurrent. The explicit fixture overload requires its requested program to be named by the manifest and present.
7. Path traversal and duplicate ROM names fail. Invalid/missing hashes and invalid JSON fail with actionable InvalidOperationException messages.
8. Every native VIC-20 test that previously returned on !ViceNativeXvic.IsAvailable now invokes the same gate. Verify the claimed count of 35 removed branches from the diff/current tree and verify zero remain.
9. The exact green TRX is current, contains only the seven prerequisite tests, and has 7 pass, 0 fail, 0 skip. The real current-oracle smoke proves the checked-in inputs and native create/step path pass.
10. No result uses the failed normal-output attempt as green evidence.
11. No UWP/Xbox implementation was changed by this slice. FR-SNP-005 and TEST-SNP-002 remain pending because Slice 3a does not complete full Phase 3 or snapshot parity.
12. Phase 3b and later work has not been smuggled into Slice 3a. Phase 3, PLAN-C64SET-001, and the whole plan remain incomplete.
13. Workspace rules and plan fidelity pass, including tests-first history, local-only execution, PowerShell-only/no-Python, durable JSONL receipts, and full MCP session-log readback.

Review hostile surfaces A-D. Inspect current files, git diff/status, TRX XML, hashes, plan, requirements, manifest, prior verdict, and MCP state. Do not accept summaries without local proof.

Return full verdict prose and final one-line JSON:
{"OverallVerdict":"AGREE|DISAGREE","AccuracyScore":0-100,"CompletenessScore":0-100,"PassCount":0,"FailCount":0,"UnknownCount":0,"FailList":[],"SessionId":"...","RequestId":"..."}

AGREE requires zero FAIL, zero UNKNOWN, AccuracyScore >= 98, CompletenessScore >= 98, and verified full session-log persistence.