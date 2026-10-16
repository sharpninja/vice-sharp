# Local Grok hostile validation: PLAN-VSFLOCKSTEP-001 Gate 0

OverallVerdict: DISAGREE  
AccuracyScore: 89  
CompletenessScore: 84  
PassCount: 4  
FailCount: 8  
UnknownCount: 7

This Class-1 Gate 0 review was run by the installed local Grok CLI against the local checkout. Web search and subagents were disabled. Both scores are under 98, and unknowns remain, so this verdict does not approve Gate 0 or authorize Slice 1.

## Findings

1. **FAIL, F02, protocol boundary.** `src/ViceSharp.Protocol/Protos/emulator_host.proto` exposes only `CaptureSnapshot` and `RestoreSnapshot`. `ProtocolContracts.cs` has `RpcStatusCode.Internal` but no `RpcStatus.Internal(string)` factory. The plan adds `ImportLegacySnapshot` and asks Slice 6 to prove gRPC transport without specifying the new RPC, request message, or generated client. Minimum remediation: lock the proto RPC and status factory before Slice 1.
2. **FAIL, F02 and canonical-path assertion.** The Avalonia Snapshot menu contains screenshot and recording commands only. No Avalonia source calls `CaptureSnapshot`. Quick save/load exists in the out-of-scope Xbox head and calls the shared snapshot service. Desktop slot count, persistence, menu placement, file dialog, and shared-service unsupported-host behavior are unspecified. Minimum remediation: specify the desktop commands and the behavior of out-of-scope/non-`win-x64` callers without adding Xbox/UWP implementation work.
3. **FAIL, F02 and F06, envelope walk.** The current writer uses a 58-byte file header and 22-byte module headers with little-endian inclusive module sizes. The reader also accepts a pre-2.4.30 37-byte header. The plan mandates only 58 bytes, omits endian and inclusive-size rules, and presents 64 MiB and 512-module caps without identifying them as ViceSharp policy. Minimum remediation: lock both VICE-accepted header forms, little-endian inclusive sizes, and remove or explicitly own additional policy limits and error codes.
4. **FAIL, F03, ROM pin.** `EnsureCiRomRoot` already pins C64, VIC-20, chargen, and drive ROM hashes. The plan refers only to a pinned ROM set. Minimum remediation: bind the generator to that committed allowlist or an equivalent exact allowlist, and require media hashes before the slice that uses the media can pass.
5. **UNKNOWN, F03 and fixture assertion.** The reviewer did not independently hash or walk `ready-c64sc-truedrive.vsf`. Minimum remediation: retain a read-only header walk, module list, length, and SHA-256 next to the requirement evidence.
6. **UNKNOWN, F03, pinned commit and toolchain presence.** The reviewer did not read the submodule HEAD or prove the MSYS2 path and required tools. Minimum remediation: retain local `git rev-parse` and toolchain preflight output.
7. **FAIL, F04.** Slice 7 includes the whole `ParityTest` suite, which contains quarantined red parity work and is non-blocking in CI. The plan also assigns creation of `NativeSnapshotCompatibilityTest` to both Slice 1 and Slice 7. Minimum remediation: use only the zero-skip VSF suite as the VSF acceptance gate and give the Nuke target one owner.
8. **FAIL, F05.** Both native DLLs independently wait forever on their own `g_active_machine`. The plan names one try-create export and one singleton policy. Minimum remediation: specify the same non-blocking ABI in both DLLs and define independent per-DLL lease/busy behavior.
9. **FAIL, F07, packaging order.** `PublishMsi` runs `DotNetPublish` internally and harvests the resulting directory. A dependency that stages native DLLs before publish can be overwritten. Minimum remediation: build first, then stage the verified closure after publish and before WiX harvest, explicitly leaving the DLLs loose beside the single-file executable.
10. **UNKNOWN, F07, dependency closure.** The reviewer did not compare the seven listed DLLs to the PE import tables. Minimum remediation: retain a local import walk and make packaging fail on an unresolved non-system import.
11. **FAIL, requirements mapping.** Generated wiki mappings disagree with the canonical and MCP mappings for the recovered VSF records. Minimum remediation: regenerate the wiki through the supported MCP export and rerun traceability.
12. **UNKNOWN, MCP.** The reviewer did not query the live requirements store. Minimum remediation: retain readback of all six requirements and mappings.
13. **UNKNOWN, xvic model strings.** The reviewer did not inspect the xvic selector arms. Minimum remediation: cite the native selector source and bind the plan to its closed vocabulary.

## Direct answers

- F01 current CPU baseline: PASS.
- F02 decision completeness: FAIL.
- F03 executable prerequisites: FAIL, with unresolved fixture, commit/toolchain, and selector evidence.
- F04 BDPv4 phase stops: FAIL.
- F05 native/session lifecycle: FAIL.
- F06 corruption contract: FAIL.
- F07 packaging: FAIL.
- Six requirements active, mapped, pending, and adequately accepted: FAIL because generated wiki mappings were stale and MCP readback was unavailable to the reviewer.
- `FR-VSFLOCKSTEP-001` fixture description: UNKNOWN.
- Native VSF canonical on all normal supported public/desktop paths: FAIL because the desktop commands and unsupported-host boundary were incomplete.
- Proprietary formats import-only, with replay/rollback not peer formats: PASS.
- Native load/write, both round trips, complete C64/VIC-20 state, 100,000 cycles, and zero skipped prerequisites required: PASS.
- No completion or implementation-authorization claim: PASS.

```json
{"OverallVerdict":"DISAGREE","AccuracyScore":89,"CompletenessScore":84,"PassCount":4,"FailCount":8,"UnknownCount":7}
```
