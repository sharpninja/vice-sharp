# Local Grok hostile revalidation: remediated PLAN-VSFLOCKSTEP-001 Gate 0

Act as the independent hostile reviewer. Work read-only in the local checkout `F:\GitHub\vice-sharp`. Do not edit files, requirements, TODOs, session logs, git state, or application state. Do not use web search, remote repositories, subagents, cloud CI, or any cloud execution. Use local built-in read/search facilities; do not require an MCP or PowerShell integration merely to inspect files.

This is a Gate 0 **plan review**, not an implementation review. Planned APIs, UI, tests, targets, and product behavior are intentionally absent from current product code. Do not fail merely because future Slice 1-7 code does not yet exist. Fail when the plan or requirements are inaccurate, contradictory, untestable, incomplete, or not decision-ready. No implementation is authorized by this review.

Governing contract:

- Native VICE `.vsf` is the day-one canonical ViceSharp snapshot format for supported x64sc C64 and xvic VIC-20 paths. It is not optional, secondary, or a peer beside proprietary formats.
- Managed VSF resume support has existed for months and must be preserved and productionized.
- Native VICE remains the version-specific reader, writer, and behavioral/file oracle.
- UWP/Xbox implementation is out of active scope. Its legacy requirements remain active and are not retired.
- Every FR, including recovered legacy FRs, has testable AC. No AC, requirement, TODO, plan gate, or product slice is complete.

Review packet:

- `docs/plans/PLAN-VSFLOCKSTEP-001.md`
- `docs/requirements/functional/FR-Snapshot.md`: `FR-SNP-005`, `FR-VSFLOCKSTEP-001`
- `docs/requirements/technical/TR-Native-Vice-Snapshot.md`: `TR-LOCKSTEP-VSF-001`, `TR-VSFLOCKSTEP-RESUME-001`
- `docs/requirements/test/TEST-Requirements.md`: `TEST-SNP-002`, `TEST-VSFLOCKSTEP-001`
- `HANDOFF.md`
- prior verdict: `docs/receipts/hv/20261003T144113Z-vsf-canonical-plan-local-grok.verdict.md`
- local source-evidence receipt: `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`
- live MCP readback receipt: `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json`
- generated mappings: `docs/Project/wiki/github/TR-per-FR-Mapping.md`
- generated FR/TR/TEST pages and `.mcp-requirements-manifest.json` in the same directory

Independently inspect relevant local source, especially `snapshot.c`, both native shims, `LockstepValidator.cs`, `Mos6502.cs`, `IViceNative.cs`, `SnapshotServiceHost.cs`, `ProtocolContracts.cs`, `emulator_host.proto`, `MainWindow.axaml`, and `build/Build.cs`. Treat receipts as evidence to verify, not authority.

Re-test every prior finding:

1. F01 current CPU baseline distinguishes architectural restart from actual native pipeline import.
2. F02 locks exact interfaces/types/projects, closed model mapping, protobuf import RPC, status mapping, Avalonia file/quick/ten-slot behavior, persistence, and unsupported-host boundary.
3. F03 locks fixture provenance/hash/modules, exact ROM allowlist, manifest schema, pinned commit, deterministic generation, MSYS2 paths, and selector vocabulary.
4. F04 gives every slice requirement/red/mock/product-green stops, one cumulative target owner, fresh reviews, and no unrelated quarantined parity suite as VSF acceptance.
5. F05 defines the exact non-blocking ABI in both DLLs, independent per-DLL singleton leases, timeout/cancellation, lock boundaries, revalidation, rollback/fault, and cleanup.
6. F06 matches pinned VICE framing: current 58-byte and legal legacy 37-byte headers, 22-byte module header, little-endian inclusive length, exact EOF, no invented compatibility caps, honest checksum limits, mandatory duplicate/missing rules, and unknown-optional pass-through.
7. F07 locks supported RID, post-publish/pre-WiX loose staging, recursive PE closure, system allowlist, VICE licensing/source offer, ROM exclusion, unsupported-host behavior, MSI inspection/install, and installed C64/VIC-20 smoke.
8. The supplied fixture's length, hash, header, exact 24-module order, and exact EOF evidence are reproducible from the local file.
9. The native submodule commit, selector arms, MSYS2 paths, and PE imports agree with the retained local evidence.
10. Both FRs map to both TRs and both TESTs in the regenerated wiki, all six records are pending with 30 unsatisfied criteria, and no legacy record is retired.

Also attack the plan for new gaps, internal contradictions, false current-state claims, hidden proprietary fallback, format-selector loopholes, impossible gates, destructive rollback behavior, unsafe parsing/arithmetic, package-order mistakes, or accidental UWP/Xbox implementation scope.

Return one complete Markdown verdict with:

- `OverallVerdict: AGREE` or `OverallVerdict: DISAGREE`.
- Integer `AccuracyScore` and `CompletenessScore` from 0 to 100.
- `PassCount`, `FailCount`, and `UnknownCount`.
- Direct PASS/FAIL/UNKNOWN for F01-F07 and items 8-10 above.
- A numbered finding for every failure or unknown with exact local file/line/source evidence and the minimum remediation.
- A final one-line JSON object containing the same verdict, scores, and counts.

AGREE is permitted only if every applicable item passes, no blocking failure or unknown remains, and both scores are at least 98. Otherwise return DISAGREE and enumerate every problem. Do not end with progress commentary.
