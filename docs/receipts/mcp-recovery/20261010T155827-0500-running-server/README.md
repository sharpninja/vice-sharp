# ViceSharp MCP recovery observations

Observed locally on PAYTON-LEGION2, 2026-10-10. This records operational checks, not a product acceptance gate or independent hostile verdict.

## Current verified state

- At 2026-10-10T16:06:19-05:00, `Get-CimInstance Win32_Service -Filter "Name='McpServer'"` returned Running, PID 73428.
- ViceSharp marker PID matched 73428. Marker SHA-256: `96ACFE5069A1FC5D8E39A6248CA9CD1FD9328308E576B79FBEB4FFC85516B508`.
- Installed Codex plugin 1.118.0 `Test-MarkerSignature` and `Invoke-FullBootstrap` returned true, including the health nonce check.
- Fresh unfiltered Global inventory returned 38 records. Read all content: 22 decoded profile snapshots, 15 other memories, one manifest. All 22 snapshot hashes matched both snapshot metadata and manifest.
- Supported primary query recovered `Codex-20261007T135626Z-plugin-session`, initially 60 turns. New recovery turn is `req-20261010T205827Z-running-server-missing-vice-workspace`.
- Independent primary readback at 2026-10-10T16:06:37-05:00 returned 61 turns and an exact response match. The older `req-20261009T202906Z-prompt-f886` remained in progress, unchanged by this recovery.
- Current-turn cache: `F:\GitHub\vice-sharp\.mcpServer\codex\recovery-20261010-current-turn`. It is bound to the existing primary session. The prior cache and old queue were preserved. `MCP_FAILSAFE_DRAIN_DISABLED=1`; no replay performed.

Evidence: [recovery.json](recovery.json), SHA-256 `F69733AAA3ED2C21477A776D46846501E0D2B1453A3D025336D313F1E46935EB`.

## Earlier state is historical, not current

Before the external service replacement, PID 72172 exposed a trusted control workspace with five registered workspaces, no ViceSharp entry, and zero Global memories. That control marker then disappeared and a ViceSharp marker appeared. A subsequent replacement produced the current PID 73428. This agent did not restart the service during this turn, change configuration/database selection, initialize a workspace, import data, or infer data loss.

The earlier report `triage-report-a393a20764a2448abfb84024b54343f3` received an intake success response from the old control instance. Its independent readback failed when that marker disappeared. Do not assume that report exists in the current store or replay its stale conclusion as current state.

## Tape amendment baseline refresh

Read the active effective layer through `workflow.requirements.effective`: 235 FR, 139 TR, 142 TEST and 233 mapping rows. Captured six `getFr` responses, complete `listTr` and `listTest` responses, and all eleven selected full-record preimages.

All 88 retained before-fields across eleven proposal records matched current effective values. All 28 retained mapping fields across seven mapping rows matched. No requirement or mapping was changed. The existing eleven-record amendment still requires its independent review and explicit expanded-delta approval.

Evidence: [tape-preimages.json](tape-preimages.json), SHA-256 `470F6E228C95EE45D17082AF2158EBCE2ADB0853AD1CA96FAC5B93CC8D399550`. This contains the full responses and individual comparisons, not merely counts.

## Unresolved metadata observation

Two supported reads of `FR-RUNTIME-TAPE-002` returned different timestamps without an intervening requirement write by this agent:

- First `createdAt`: `2026-10-10T21:07:56.4837141+00:00`; second: `2026-10-10T21:08:20.3862957+00:00`.
- First `updatedAt`: `2026-10-10T21:07:56.4838005+00:00`; second: `2026-10-10T21:08:20.3863873+00:00`.

Cause is unproven. These values are not proof of stable historical metadata. Supported triage intake returned success for `triage-report-25583068f60e48b19d678d556ea2b304`, routed to `F:\GitHub\McpServer`, group `triage-group-2badaaf3d7396c93`. A `getReport` from the ViceSharp context returned report-not-found. Independent triage readback remains unconfirmed; intake success is not a resolution claim. Full-content supported failsafe retained at `.mcpServer\failsafe\Codex\workspaces\RjpcR2l0SHViXHZpY2Utc2hhcnA\pending\20261010T210940Z-getfr_volatile_timestamps-7342.yaml`.

No emulator tests, builds, native changes, requirement writes, commits, pushes, or HV acceptance occurred in this recovery slice. Next product action is to carry this fresh baseline and metadata caveat into the exact tape-amendment review packet. All original real-emulator gates remain mandatory.
