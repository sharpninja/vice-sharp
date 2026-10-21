# PLAN-NATIVERESTORE-001 step D0 on LAB-OMARCHY: measure and preserve (2026-10-20T21:14 to 21:28)

Executed by Claude Code (claude-fable-5-1-high@ClaudeCode, session OMAR-VICE#) on LAB-OMARCHY after Payton released Phase D implementation at 19:35 (verbatim option label: "Release Phase D implementation now"), following HV round 8 AGREE 98/98 (`docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/`). Read only on `native/vice`: no checkout, no clean, no build; the submodule's porcelain status and diffstat are identical before and after (163 entries; 8 files changed, 222 insertions, 8 deletions).

## A02 entry evidence (`EntryEvidence_BindsBothHostsAndExplicitRelease`)

- Installed desktop builds (D-pre): LAB-OMARCHY `releases/1.3.111-1beaac12c5d74c038596917d33d04454`, `ViceSharp.Avalonia.dll` ProductVersion `1.3.111+2c8d6028...`, SHA256 `DCDC77B6...` (`docs/receipts/lockstep/20261020T105400-phase-d-pre-desktop-install-linux/`); PAYTON-LEGION2 `InstallMsi` 1.3.111 from `2c8d6028`, MSI SHA256 `C5C90155...` (`docs/receipts/lockstep/20261020T121000-phase-d-pre-desktop-install-windows/`).
- Windows trace matrix: 100k and 1M (both machines) and x64sc 4M on PAYTON-LEGION2, every number equal to the Linux reference (`docs/receipts/lockstep/20261020T102600-phase-c-step3-s3-s4c-equivalence-windows/`, commit `8295696d`).
- Plan approval: HV round 8, GPT-6.1-Sol max, AGREE, accuracy 98, completeness 98, 54 PASS, 0 FAIL, primary-persisted (reviewer session `Codex-20261020T235538Z-plugin-session`, request `req-20261020T235529Z-nativerestore-r8`).
- Release: Payton, 2026-10-20T19:35, through the session question, verbatim option label "Release Phase D implementation now".

## Tooling (tests first, PowerShell only)

- `native/Save-ViceSubmoduleDelta.ps1` (preservation, `SupportsShouldProcess`), contract `native/tests/Save-ViceSubmoduleDelta.Tests.ps1`: 9 passed, 0 failed, 0 skipped (Pester 5.9.1; mock git tree; WhatIf writes nothing; staged change, gitlink mismatch, wrong-host table, missing expected hunk, unclassified live hunk, explained same-file difference, unclassified untracked path, library presence and absence).
- `native/Get-ShimConsumerInventory.ps1` (semantic per-method shim consumer inventory, R6), contract `native/tests/Get-ShimConsumerInventory.Tests.ps1`: 3 passed, 0 failed, 0 skipped (direct plain-Fact user reported; helper-only chain test to helper to provider reported as transitive; managed fake implementing `IViceNative` not reported; comment and string-only mentions not reported; `ViceFact` and `ViceTheory` reported; a same-named type in another project does not leak; `typeof` and `nameof` reflection does not count).
- `native/Get-VsfDriveModules.ps1` (fixture scan, R10 precondition, N05 validation), contract `native/tests/Get-VsfDriveModules.Tests.ps1`: 5 passed, 0 failed, 0 skipped (synthetic containers; truncated DRIVE payload invalid; wrong magic rejected; module past end of file rejected; the approved fixture reproduces the round 2 byte inspection).
- PSScriptAnalyzer 1.25.0 over the six files (Warning, Error, Information): 0 findings.
- Expected-hunk tables: `native/phase-d/expected-hunks-LAB-OMARCHY.json` (11 rows) and `native/phase-d/expected-hunks-PAYTON-LEGION2.json` (20 rows from the Oct 4 patch).

## Preservation (`manifest.json`, `native-vice-tracked.patch`, `preserved/`)

Command (dry run first with `-WhatIf`, which wrote nothing, then the real run; regenerated at 21:26 with the final classification patterns so both hosts use the same script version):

```
pwsh -NoProfile -NonInteractive -File native/Save-ViceSubmoduleDelta.ps1 -SubmodulePath native/vice -ReceiptDirectory docs/receipts/native-vice/20261020T211402-phase-d-preservation-linux -HostName LAB-OMARCHY -GitlinkSha cc67418cba58e71d8e70fd353e9de47199ad2075 -ExpectedHunksPath native/phase-d/expected-hunks-LAB-OMARCHY.json -LibrarySearchRoots native,tests/ViceSharp.TestHarness/bin/Release/net10.0,tests/ViceSharp.TestHarness/bin/Debug/net10.0
```

Result: Outcome Complete. Gitlink and checked-out commit both `cc67418cba58e71d8e70fd353e9de47199ad2075`; staged diff empty; porcelain entries 163 (tracked modified 8; untracked 155: 145 ordinary, 10 hidden); classes modified 8, generated 155, injected 0, unclassified 0; patch SHA256 `D87A63A435C1DEDA2C1E5F00A2F951ED6926225FB2C372AF689F9D1CA1610E1C`, 12,868 bytes, 11 hunks all mapped to section 2 rows (H1 x2, H5, H6 x2, H2, H3, H4, H8 x3), 0 explained differences, 0 unclassified, 0 missing expected; 8 preserved copies under `preserved/` with hashes equal to the live files; libraries found under the three search roots: 0 (no `vice_x64`, `vice-shim` or `vice_xvic` library exists on this host).

Measured host difference (A01): the Linux delta has 11 hunks where the Oct 4 LEGION2 patch has 20; of the 12 hunks in the 8 shared files, the H2 hunk at `mainc64cpu.c` old line 196 (the checkpoint inside the stolen-cycle loop) is absent on LAB-OMARCHY. The LAB-OMARCHY table lists 11 rows and records this; no disposition changes (H2 is rejected either way).

Classification patterns added after LEGION2's first run (21:20, reported by LEG-VICE#; 16 untracked paths were unclassified there and none of them exists on Linux): `debug.h`, `infocontrib.h`, `version.h`, `siddefs.h`, `siddtvdefs.h`, the `wave6581_*.h` and `wave8580_*.h` tables, `.o65`, `vice-version.sh` as generated by configure and make; `vice/src/.vice-shim-configure-flags` as injected by name (the Phase 0 receipt lists it as `.vice-shim-configure-flags`, and the live path is under `vice/src/`). LEGION2 reruns with the committed script so both receipts come from the same version.

## Shim consumer inventory (`shim-consumer-inventory.json`, R6, round 2 F03, round 3b N06, round 7 N24)

`native/Get-ShimConsumerInventory.ps1 -TestRoot tests` over 567 C# files (852 types, 3,324 test methods): 241 consumer methods in 50 files, all in `ViceSharp.TestHarness`: 188 by attribute (`ViceFact` or `ViceTheory`), 52 direct, 1 transitive (`LockstepValidatorKernalCloseTests.ResolveKernalJumpTarget_FollowsC64CloseIndirectVector` via `LockstepValidator`); 59 helper types (57 direct, `Program` of `ViceSharp.Benchmarks` via `NativeViceBaseline`, `LockstepValidatorKernalCloseTests` via `LockstepValidator`). `SnapshotResumeSpikeTests` and `Vic20NativeLockstepTests` (plain `[Fact]` users named in round 2) are included; `XAudio2AudioMathTests` and `XboxTestConventionTests` (string-only mentions) are not; `Vic20HalfCycleComparatorTests` is not a consumer (it reaches `LockstepValidator` only through `typeof`, which does not execute it; R13 B implements the comparator in D2). This list is the D2 `ShimLegacy` tagging input.

## Fixture scan (`vsf-fixture-scan.json`, N01, N05)

- `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf` (SHA256 `5C284F5C...`, 190,854 bytes): `MAINCPU` 1.2, clock 109163946, PC 22377, P `$A0`; `DRIVE8` `has_tde` 0 `has_drives` 1 (offset 66204, length 24), `DRIVE9` to `DRIVE11` `has_tde` 1; valid; R10 precondition `-drive8type 0`.
- `tests/ViceSharp.VsfCompatibility.Tests/Fixtures/native-maincpu-1.2.vsf`: identical fields and the same precondition.
- `tests/ViceSharp.VsfCompatibility.Tests/Fixtures/native-maincpu-1.4.vsf`: `MAINCPU` 1.4, clock 30250590, PC 3937; all four units `has_tde` 1; valid; no precondition.
- No truncated or invalid `DRIVE` payload in any fixture.

## Store readback through the MCP wrappers (`store-readback/`, U02)

Eight calls through `Invoke-McpPlugin.ps1 -Command Invoke` with `MCP_FAILSAFE_DRAIN_DISABLED=1`, all returning `type: result` (`summary.json`): `workflow.requirements.effective`, `workflow.requirements.listLayers`, `workflow.requirements.listMappings` for `FR-VICE-001` (two mappings: `TR-VICE-ORACLE-001`, `TEST-VICE-001`), `workflow.requirements.listTr` and `listTest` (the TR and TEST catalogs, in which `TR-VSFLOCKSTEP-RESUME-001` AC1 still reads "v1.2 and v1.3", to be amended in D1 under R15 through the requirements wrapper), and `workflow.todo.get` for `PLAN-VICEORACLE-001` (done false; tasks listed with their current done flags). No store change was made.

## Self-assessment

Correctness 97 (every number above is read from the artifacts in this directory; the host difference and the 16 LEGION2 paths are measured, not assumed), accuracy 98, completeness 95 (D0 for LAB-OMARCHY is complete; LEGION2's rerun with the final script and its receipt are pending; the semantic inventory is regex-based with the four controls, not a compiler-based analysis, which the D2 gate's load log and TRX evidence will check independently).

Next: D1 (builder `SupportsShouldProcess`, A06 rerun contract, the two snapshot translation units with their doubles and contract tests, the adapter precondition and payload validation, the R15 amendment through the requirements wrapper, the Slice 1 contract tests).
