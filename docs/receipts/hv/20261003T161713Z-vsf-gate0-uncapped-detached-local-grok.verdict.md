# Local Grok hostile validation verdict

- Execution: local detached CLI
- Session: `01a1025d-c176-7f31-8d95-b06abbd2715f`
- Request: `7ff8ed54-3fa4-45d5-a5e1-561bd9825d0f`
- Model: `grok-4.7-build`
- Stop reason: `end_turn`
- Turn limit: none
- Request JSONL: `docs/receipts/hv/20261003T161713Z-vsf-gate0-uncapped-detached-local-grok.request.jsonl`
- Response JSONL: `docs/receipts/hv/20261003T161713Z-vsf-gate0-uncapped-detached-local-grok.response.jsonl`

## Verbatim verdict

OverallVerdict: DISAGREE  
Timestamp: 2026-10-03 (UTC clock not re-read; this turn forbids further tool calls)  
AccuracyScore: 74  
CompletenessScore: 62  
PassCount: 9  
FailCount: 2  
UnknownCount: 11  

Gate 0 is not decision-ready. Two required locks are false or unbound on the evidence already gathered. Eleven required questions were not resolved because the plan read was truncated after `docs/plans/PLAN-VSFLOCKSTEP-001.md` line 22, several requirement bodies were truncated, and the fixture, submodule commit, MSYS2 scripts, PE imports, MCP readback JSON, and `.mcp-requirements-manifest.json` were not independently checked. An unresolved question is UNKNOWN. AGREE is not available.

## Direct dispositions

- F01 current CPU baseline: UNKNOWN.
- F02 interfaces, model, RPC, status, Avalonia, persistence, unsupported host: FAIL.
- F03 fixture, ROM allowlist, manifest, commit, generation, MSYS2, selectors: UNKNOWN.
- F04 slice stops, one cumulative owner, fresh reviews, no quarantined parity acceptance: UNKNOWN.
- F05 non-blocking ABI, per-DLL leases, timeout, locks, rollback, cleanup: UNKNOWN.
- F06 pinned VICE framing: UNKNOWN.
- F07 RID, staging order, PE closure, system allowlist, license, ROMs, unsupported host, MSI, installed smoke: FAIL.
- Item 8 fixture length, hash, header, 24-module order, exact EOF from the local file: UNKNOWN.
- Item 9 submodule commit, selector arms, MSYS2 paths, PE imports: UNKNOWN.
- Item 10 both FRs to both TRs and both TESTs, six pending records, 30 unsatisfied criteria, legacy not retired: UNKNOWN.

## PASS

1. PASS, wiki four-link rows. `docs/Project/wiki/github/TR-per-FR-Mapping.md` line 152 maps `FR-SNP-005` to `TR-LOCKSTEP-VSF-001`, `TR-VSFLOCKSTEP-RESUME-001`, `TEST-SNP-002`, and `TEST-VSFLOCKSTEP-001`. Line 204 maps `FR-VSFLOCKSTEP-001` to the same two TRs and two TESTs. This is only the mapping half of item 10.

2. PASS, visible ROM hashes. Plan line 239 names eight hashes that match `build/Build.cs` lines 169-171, 184, and 187-189: `C64/basic-901226-01.bin` `89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D`, `C64/kernal-901227-03.bin` `83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721`, `C64/chargen-901225-01.bin` `FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420`, `DRIVES/dos1541-325302-01+901229-05.bin` `D1D45AFB46FD4E2B48D93CA367B889D75654A3B7ACF73044E51F6D880C09369E`, `VIC20/basic-901486-01.bin` `8BE2D882E0EEB1C6923A43660236EA69D43CA8A3BD0613B1B6C910601584FBED`, `VIC20/kernal.901486-07.bin` `FF6D1416B91A71F5240A5BB8ADC9FDC1B955374C48322CA47B3E436334F168FB`, `VIC20/kernal.901486-06.bin` `8FC94BD895F488AC0AE1A52ABDC32988CD63C777770B89A9D415513098535CC2`. The chargen line begins `BDA280726C`, matching the start of `Build.cs` line 190. The remainder of plan line 239 was truncated, so the ninth hash is not in this PASS.

3. PASS, publish insertion point that was visible. Plan line 454 places `StageNativeSnapshotRuntime(AvaloniaPublishDir)` immediately after `DotNetPublish` and before pruning or WiX harvest, and names the six loose files `vice_x64.dll`, `vice_xvic.dll`, `libstdc++-6.dll`, `libgcc_s_seh-1.dll`, `libwinpthread-1.dll`, and `zlib1.dll`. `build/Build.cs` line 1243 calls `DotNetPublish`, line 1270 sets `IncludeNativeLibrariesForSelfExtract` on that publish, lines 1291-1299 prune debug/doc files, and lines 1306-1313 harvest WiX from `AvaloniaPublishDir`. Post-publish loose files are outside that publish property.

4. PASS, pinned VICE framing constants in source. `native/vice/vice/src/snapshot.c` lines 64-68 define a 19-byte `VICE Snapshot File` magic and a 13-byte `VICE Version` magic. `snapshot.h` lines 33-34 define 16-byte machine and module names. Magic plus two version bytes plus the machine name is 37 bytes. Lines 835-848 write the 13-byte marker, four version bytes, and one little-endian dword, which is the 21-byte block that places modules at byte 58. Lines 114-130 and 265-274 write and read little-endian words and dwords. Lines 693-700 and 390-410 make the module size include the 22-byte header. `c64-snapshot.c` lines 59-60 and 111 require container 2.0 exactly. `vic20-snapshot.c` lines 60-61 and 120 require container 3.1 exactly.

5. PASS, current proprietary baseline. `src/ViceSharp.Avalonia/MainWindow.axaml` lines 41-51 contain screenshot and recording commands only. `emulator_host.proto` lines 68-70 expose `CaptureSnapshot` and `RestoreSnapshot` only. `ProtocolContracts.cs` lines 18-28 have no `RpcStatus.Internal(string)` factory, while line 38 defines `RpcStatusCode.Internal = 5`. `SnapshotServiceHost.cs` lines 19-20 and `MachineSnapshotStager.cs` line 27 use `vice-sharp.runtime-snapshot.v1` and `vice-sharp.machine-snapshot.v2`.

6. PASS, xvic selector arms present in source. `native/vice-shim-vic20.c` lines 416-441 map empty/`vic20`/`vic20pal`/`pal`/`xvic` to PAL, `vic20ntsc`/`ntsc` to NTSC, `vic21`/`supervic` to VIC21, and `vic1001`/`jap` to VIC1001. Any other string returns unknown.

7. PASS, current per-DLL blocking singletons. `native/vice-shim.c` line 85 and `native/vice-shim-vic20.c` line 78 each own a static `g_active_machine`. Lines 390-391 and 381-382 wait with `SleepConditionVariableCS(..., INFINITE)`. A search found `vice_machine_create` and no `vice_machine_try_create`.

8. PASS, recovered legacy record not superseded in the canonical FR. `docs/requirements/functional/FR-Snapshot.md` lines 211-215 say `Superseded By: None`, keep `FR-VSFLOCKSTEP-001` active, and link both TRs and both TESTs.

9. PASS, visible approval boundary. The plan ending states that production implementation stops until the operator approves `PLAN-VSFLOCKSTEP-001`, and that approval authorizes Slice 1 only. The visible tail also says the `Category=NativeViceSnapshot` target must report zero failed, zero skipped, and zero aborted tests, and that unrelated skips in the broad `Test` target are not VSF acceptance.

## FAIL

1. FAIL, F02 persistence normalization. Plan line 214 locks quick and named slots at `Environment.SpecialFolder.LocalApplicationData/ViceSharp/Snapshots/<machine-kind>/<profile-id>/quick.vsf` and `slot-01.vsf` through `slot-10.vsf`, then says profile ids are normalized with the existing path-safety convention. A repository search found that phrase only in this plan line. The only nearby sanitizer is `tests/ViceSharp.AiReview.Tests/ReviewLog.cs` line 129, which strips invalid filename characters from review-log names. There is no profile-id path convention to implement. Empty ids, `..`, separators, and invalid filename characters are undecided. Minimum remediation: name the exact normalization function, the rejected characters, the replacement rule, and the result for an empty id, in the plan, before Gate 0 approval.

2. FAIL, F07 system allowlist. Plan line 454 says the PE validator permits only the documented Windows system DLL allowlist and fails on every missing, extra, or unresolved non-system import. A plan search for `KERNEL32`, `DSOUND`, `allowlist`, and `libwinpthread` found no list of allowed system DLLs. The receipt at `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md` lines 92-97 describes current imports (`DSOUND`, `KERNEL32`, `msvcrt`, `ntdll`, `SHELL32`, `USER32`, `WINMM`, `WS2_32`) and is not a normative plan lock. Two implementers can admit different system DLLs. Minimum remediation: put the closed system-DLL names in the plan and make any future import a plan change.

## UNKNOWN

3. UNKNOWN, F01 plan lock. The source baseline is an architectural restart: `tests/ViceSharp.TestHarness/LockstepValidator.cs` lines 511-513 call `GetCpuPipelineState` and write only `PportDir` to `$00` and `PportData` to `$01`. Line 518 calls `InjectSnapshotResumeState` with A, X, Y, S, P, and PC. `src/ViceSharp.Chips/Cpu/Mos6502.cs` lines 4396-4404 assign those registers and call `ResetInFlightState`. Lines 4433-4455 clear opcode, micro-cycle, BA-low holds, and interrupt-delay flags. `ViceNative.cs` lines 622-635 still export `Clk`, `LastOpcodeInfo`, `BaLowFlags`, port readback, and IRQ/NMI clocks. The plan passage that must state this distinction was inside the truncated region after line 22, so the plan lock itself is unresolved.

4. UNKNOWN, F02 type, RPC, and status locks. The plan region that would name projects, immutable records, `ImportLegacySnapshot`, and the `ViceSnapshotErrorCode` to `RpcStatus` map was truncated. Current code confirms the gaps the plan must close: `emulator_host.proto` lines 68-70 and `ProtocolContracts.cs` lines 18-38. Absence of future code is expected. Absence of the plan text from this evidence set leaves those locks unresolved.

5. UNKNOWN, F03. `FR-Snapshot.md` acceptance criterion 1 states fixture `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`, SHA-256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, and 24 module names. The local file was not hashed or walked. Plan line 239 was truncated before the full `VIC20/chargen-901460-03.bin` and `dos1540` entries could be compared with `Build.cs` lines 190-191. Manifest schema, pinned commit, deterministic generation, and MSYS2 executable paths were in the unread plan region. `build-vice-shim.ps1` was not opened.

6. UNKNOWN, F04. The visible tail requires one `Category=NativeViceSnapshot` target with zero failed, zero skipped, and zero aborted tests, and it refuses to treat broad-suite skips as VSF acceptance. The per-slice requirement checkpoint, red test, mock gate, product-green stop, single target owner across slices, fresh hostile review, and explicit exclusion of an unrelated quarantined parity suite were truncated.

7. UNKNOWN, F05 plan ABI. The source still blocks forever inside `vice_machine_create` (`vice-shim.c` lines 390-391 and `vice-shim-vic20.c` lines 381-382). `SnapshotServiceHost.cs` lines 42 and 71 already take `session.SyncRoot`. `EmulationPumpService.cs` lines 217-220 advance a session only while `RunState == Running`, so a Stopped session is not pumped. The planned `try_create` return codes, 25 ms poll, 10-second timeout, cancellation, independent leases, revalidation, rollback fault, and temp cleanup were not in the gathered plan text.

8. UNKNOWN, F06 plan lock against the pinned oracle. Source identity is settled and differs by machine. `c64.c` line 179 sets `machine_name` to `C64`, while lines 1481-1484 return `C64SC` when `machine_class == VICE_MACHINE_C64SC`. `vice-shim.c` lines 237-242 force that class, and line 722 opens snapshots with `machine_get_name()`. `c64-snapshot.c` lines 59-66 write container 2.0. `vic20.c` line 144 sets `VIC20`, and `vic20-snapshot.c` lines 60-61 and 115 write and open container 3.1 with `machine_name`. The plan text that must require those exact names, exact container versions, duplicate and missing rules, unknown-optional pass-through, exact EOF, and the checksum limit was truncated. Whether the plan invents a compatibility cap is unresolved.

9. UNKNOWN, item 8. Receipt `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md` lines 21-32 assert length 190854, the SHA-256 above, magic `VICE Snapshot File`, container 2.0, machine `C64SC`, 58 header bytes, VICE 3.8.0.0, 24 modules, and exact EOF. The module table in that read was cut at `VIC-II` 1.3. The receipt was not recomputed from the local file, so it is not reproduction evidence.

10. UNKNOWN, item 9. The same receipt lines 9-13 assert submodule commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. That command was not rerun. x64 selector arms in `vice-shim.c` lines 496-579 were truncated before the NTSC, old-PAL, and PAL-N arms. MSYS2 paths were not read. Receipt lines 92-111 assert the PE closure and six hashes. Those imports were not walked again.

11. UNKNOWN, item 10 store and status completeness. Canonical pending status is visible for `FR-SNP-005` (`FR-Snapshot.md` line 156), `TR-LOCKSTEP-VSF-001` (`TR-Native-Vice-Snapshot.md` line 19), `TR-VSFLOCKSTEP-RESUME-001` (line 68), and `TEST-SNP-002` (`TEST-Requirements.md` line 251). The status lines for `FR-VSFLOCKSTEP-001` and `TEST-VSFLOCKSTEP-001` were truncated. The plan opening says nine, seven, and eight criteria for three of the records. The visible legacy FR has three criteria and the legacy TR has two. The sixth record's criterion count was not visible, so 30 unsatisfied criteria is not established. `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json` and `docs/Project/wiki/github/.mcp-requirements-manifest.json` were not opened.

12. UNKNOWN, F07 tail and Xbox/UWP boundary. Plan line 454 was cut at "fails on every missing, extra". VICE license and source offer, ROM exclusion, unsupported-host behavior, MSI inspection, install, and installed C64/VIC-20 smoke were not in the gathered text. `src/ViceSharp.Xbox.Input/AppCommandDispatcher.cs` line 257 calls the shared `CaptureSnapshotAsync`. A shared-host behavior change reaches that head. The plan text that keeps UWP/Xbox implementation out of scope was truncated.

## Scores

Accuracy is 74. The fully visible numeric anchors, wiki rows, publish order, approval boundary, and current host baseline agree with local source. The path-safety sentence is inaccurate, and the system allowlist is presented as documented when the plan does not document it. Receipt claims for the fixture, commit, PE hashes, and 30 unsatisfied criteria are not counted as accurate.

Completeness is 62. Persistence normalization and the system-DLL allowlist are open. The gathered text does not establish the exact managed types, import RPC, status map, non-blocking ABI, per-slice Byrd stops, machine-name bytes, per-machine container versions, fixture reproduction, toolchain paths, or the six-record unsatisfied-criteria readback.

Workspace rules and requirements coverage are not a separate pass. The visible approval boundary matches plan-first review, and the visible acceptance criteria for `FR-VSFLOCKSTEP-001` and `TR-VSFLOCKSTEP-RESUME-001` are testable. Full AC coverage for all six records remains inside UNKNOWN 11.

{"OverallVerdict":"DISAGREE","AccuracyScore":74,"CompletenessScore":62,"PassCount":9,"FailCount":2,"UnknownCount":11}
