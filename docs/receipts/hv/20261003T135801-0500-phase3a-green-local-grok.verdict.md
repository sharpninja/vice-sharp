# Slice 3a green gate: AGREE

TimestampUtc: 2026-10-03T19:06:01.1094287Z
ValidatorIdentity: GrokCode (local hostile review, not a subagent)
add-profile: yes, 22 profile markdown files, add-profile.md first, add-profile.grok.md excluded, plus C:\Users\kingd\.codex\skills\add-profile\SKILL.md.
Work class: project implementation green gate (class 1).
SessionId: GrokCode-20261003T185944Z-phase3a-green
RequestId: req-20261003T185944Z-001-phase3a-green-gate
Prior red verdict: F:\GitHub\vice-sharp\docs\receipts\hv\20261003T132036-0500-phase3a-red-r2-local-grok.verdict.md

OverallVerdict: AGREE
AccuracyScore: 100
CompletenessScore: 100
PassCount: 16
FailCount: 0
UnknownCount: 0
FailList: none

Slice 3a green gate passes. Implementation of XvicOraclePrerequisites matches the corrected red authorization. This AGREE does not complete Phase 3, Phase 3b, FR-SNP-005, TEST-SNP-002, PLAN-C64SET-001, or the whole plan. No TODO was marked done by this review.

## Claims

### 1 PASS. The red authorization and its closed finding are respected.

The red rerun verdict is AGREE 100/100. Its authorization says implementation may start, the green code must call the validator directly, and the lockstep tests that returned must use the same gate. Current tests call XvicOraclePrerequisites.Validate at XvicOraclePrerequisiteTests.cs lines 94-98. git diff removes 35 lines of if (!ViceNativeXvic.IsAvailable). The current Vic20 tree has zero ViceNativeXvic.IsAvailable matches and 35 ValidateCurrent() calls. The workload case still deletes power-on-pal.prg, the fixture program field, and asserts that filename at lines 71-78 and 102.

### 2 PASS. The tests call Validate directly. Reflection is gone.

InvokeGate is a direct call. A search of both XvicOraclePrerequisite files for ExceptionDispatchInfo, GetType(, BindingFlags, and System.Reflection returned NO_REFLECTION. The usings are System.Security.Cryptography, System.Text.Json, and Xunit.

### 3 PASS. The six negative cases and the complete fixture behave as claimed.

Validate_FailsWhenXvicBinaryIsMissing deletes xvic.exe and expects xvic executable. Validate_FailsWhenViceXvicLibraryIsMissing deletes vice_xvic.dll and expects vice_xvic native library. Validate_FailsWhenVic20RomSetIsMissingFromConfiguredRoot deletes basic-901486-01.bin and expects ROM. Validate_FailsWhenManifestIsMissing expects manifest. Validate_FailsWhenManifestHashDoesNotMatchFile overwrites the library and expects hash. Validate_FailsWhenNamedWorkloadIsMissing deletes the fixture program and expects that filename. Validate_AcceptsCompleteOracleFixture calls Validate() with no expected exception. The validator throws InvalidOperationException from RequireFile, ValidateHash, and ValidateWorkloads. The isolated Release TRX records all seven Passed.

### 4 PASS. ValidateCurrent uses the checked-in manifest, ResolvedPath, and VICESHARP_ROM_PATH.

XvicOraclePrerequisites.cs lines 34-43 resolve docs/requirements/oracles/vic20-xvic-manifest.json from the repository root, read VICESHARP_ROM_PATH, and pass ViceNativeXvic.ResolvedPath. An empty path fails in NormalizeRomRoot. A search of that file for MachineTestFactory, FindRepoViceDataRoot, and the repo data path returned NO_FACTORY_FALLBACK. FindRepositoryRoot only locates ViceSharp.slnx so the manifest path can be built. It is not a ROM fallback.

### 5 PASS. Executable, library, and every manifest ROM hash are enforced.

Lines 69-78 hash the manifest xvic path against viceBuildHash and the supplied library against viceXvicDllSha256. RequiredVic20RomNames lines 15-23 are basic-901486-01.bin, kernal.901486-02.bin, kernal.901486-06.bin, kernal.901486-07.bin, chargen-901460-02.bin, and chargen-901460-03.bin. Those are the six names in vic20-xvic-manifest.json lines 13-39. ValidateRomAllowlist requires each name in the manifest, requires the file under the configured root VIC20 directory, and hashes every manifest ROM entry. The checked-in manifest hashes are non-empty 64-character hex strings. The current-oracle smoke calls ValidateCurrent and passes, so those checked-in inputs matched on this machine.

### 6 PASS. ValidateCurrent requires every distinct manifest workload program. The explicit overload requires the requested program.

ValidateCurrent calls ValidateCore with workloadProgram null. ValidateWorkloads then requires every distinct program. The manifest programs are only kernal.901486-07.bin and kernal.901486-06.bin. The explicit Validate overload rejects a program the manifest does not name, then RequireFile checks that program under the VIC20 directory. The fixture names power-on-pal.prg and the missing-workload test deletes that file.

### 7 PASS. Traversal, duplicate names, bad hashes, and invalid JSON fail with InvalidOperationException.

Duplicate ROM names throw at lines 122-125. RequireSimpleFileName rejects a name that is not a single file name or that contains a directory separator at lines 265-274. ValidateHash rejects a non-64-hex hash at lines 230-233 and a byte mismatch at lines 238-241. RequireValue rejects a missing hash. ReadManifest wraps JsonException as invalid JSON at lines 107-111. The seven green tests do not execute these branches. The throws are in the source that predates the green TRX.

### 8 PASS. All 35 former IsAvailable branches now call the gate, and none remain.

git diff -U0 across tests/ViceSharp.TestHarness/Vic20 reports 35 removed if (!ViceNativeXvic.IsAvailable) lines. A current-tree search for ViceNativeXvic.IsAvailable returned ZERO_ISAVAILABLE. ValidateCurrent() appears 35 times as a call, plus the method definition. The following lines continue into CreateInstance, lockstep, or an assertion. They do not return because the oracle is missing. DivergeProbe and WorkloadLockstep still return when VICESHARP_LOCKSTEP_2S or VICESHARP_LOCKSTEP_10S is not 1. Those returns are after ValidateCurrent and are the pre-existing opt-in long runs, not the oracle skip.

### 9 PASS. The green TRX and the current-oracle smoke match the claimed counters and hashes.

phase3a-green-r2.trx length 11961, LastWriteTimeUtc 2026-10-03T18:53:13.0096034Z, SHA256 D9A61D7B871F518F35AB4EA562A590F0C86D5B5964FFD078FD3EFDA97F3D8348. Counters total 7, executed 7, passed 7, failed 0, error 0, timeout 0, aborted 0, notExecuted 0. One class, XvicOraclePrerequisiteTests. Seven Passed rows. codeBase is phase3a-green-r2-artifacts\bin\ViceSharp.TestHarness\release\ViceSharp.TestHarness.dll. Start 2026-10-03T13:53:09.9102154-05:00. Finish 2026-10-03T13:53:12.9991794-05:00.

phase3a-current-oracle.trx length 5380, LastWriteTimeUtc 2026-10-03T18:53:47.3968984Z, SHA256 617C0ABCB50752328EC83A3FB87C89DB9CEC3B6AFA8AEDAE7A2703DBF80C0E75. Counters total 1, executed 1, passed 1, failed 0, error 0, timeout 0, aborted 0, notExecuted 0. The only result is Vic20LockstepScaffoldTests.NativeXvic_StepAdvancesCycle_WhenOraclePresent Passed. Its codeBase is the same isolated release DLL. The method at Vic20LockstepScaffoldTests.cs lines 38-49 calls ValidateCurrent, creates vic20, resets, steps 64 times, and asserts the cycle advanced. Every Vic20 cs file LastWriteTimeUtc is before the green TRX. The oracle sources are 2026-10-03T18:48:46Z and 2026-10-03T18:48:50Z.

### 10 PASS. The passing evidence is the isolated run.

TestResults\handoff-completion contains phase3a-red, phase3a-red-r2, phase3a-green-r2, and phase3a-current-oracle. There is no non-isolated phase3a green TRX. Both passing codeBase values point at phase3a-green-r2-artifacts. This review does not treat any other run as the green proof.

### 11 PASS. UWP and Xbox implementation were not changed. Both requirements stay Pending.

git status --short filtered for Xbox, UWP, and uwp returned no paths. FR-SNP-005 Status Pending is FR-Snapshot.md line 156. AC7 is line 170. AC8 leaves UWP/Xbox out of active scope. TEST-SNP-002 Status Pending is TEST-Requirements.md line 251. AC6 is line 264. AC7 says UWP/Xbox is not changed or claimed complete. The requirement diff adds those sections as Pending. It does not mark them completed. Isolated build obj file lists for ViceSharp.Xbox are compile outputs of existing projects, not a source edit in this slice.

### 12 PASS. Phase 3b and later slices were not completed inside Slice 3a.

Plan lines 214-255 still describe Slice 3b and Slice 3c as later work. The tracked Vic20 diff is the 11 named test files. The two oracle files are untracked. ViceNative.Xvic.cs, MachineTestFactory.cs, and LockstepValidator.cs are not in the scoped status. CompletionTestManifestTests.cs line 11 still has the placeholder sha256-vice-xvic-final. Plan exit lines 306-317 are not met. Phase 3, PLAN-C64SET-001, and the whole plan stay incomplete.

### 13 PASS. Workspace rules and plan fidelity hold for this gate.

The red TRX finished at 2026-10-03T18:19:36Z, the red verdict is 2026-10-03T18:26:36Z, the implementation files are 18:48Z-18:51Z, and the green TRX finished at 18:53:12Z. That is tests, then red gate, then implementation, then green evidence. git diff --check exited 0. The two new files have no trailing whitespace. This review used PowerShell.MCP, native objects, and no Python. Receipts are the jsonl pair and this verdict. The session-log readback is required before this AGREE is reported.

## Surface B PASS

Byrd order for this slice is the prior red AGREE followed by implementation and this green gate. This review did not fail the slice by comparing requirement timestamps with file times. No product, requirement, TODO, plan, HANDOFF, or git state was edited by the reviewer except these HV receipt files.

## Surface C PASS

FR-SNP-005 AC7 and TEST-SNP-002 AC6 are cited and still Pending. Full AC6 still includes both shims, MSYS2 paths, Category=NativeViceSnapshot, and the snapshot parity gate. Those are outside Slice 3a and are not claimed done.

## Surface D PASS

Plan lines 198-204 are now covered by passing tests and the validator. The plan command is the XvicOraclePrerequisiteTests filter, which the green TRX runs. Slice 3b, Slice 3c, the idle pin, and the whole-plan exit remain ahead. CPU and clock files were not changed, so the C64 idle pin was not required for this slice.

## Authorization

The Slice 3a green gate passes. Do not mark FR-SNP-005, TEST-SNP-002, Phase 3, Phase 3b, PLAN-C64SET-001, or the plan complete from this result. The next slice still needs its own red test and red gate.