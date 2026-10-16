# PLAN-ROMM-001 Phase E: validation runbook

Phase E is the BDP `[V]` validation tier: the flows that cannot be unit-tested are
exercised against a **live RomM + csdb-bridge** on real hardware. It splits into two
halves:

1. **Gateway/VM E2E (automatable)** - the adapter and view-model behaviour against a real
   RomM 5.0.0 server (browse, resolve, detail, download, collections, CSDb). This is
   codified as an **executable, opt-in test suite** you run with one command; it produces
   real receipts. This also de-risks the plan's #1 risk (untyped endpoints drifting from
   RomM 5.x) by parsing real server responses.
2. **GUI + gamepad E2E (manual)** - cover tiles rendering, controller A/Y launch, TV-safe
   layout, no `RPC_E_WRONG_THREAD` - validated by hand on the Xbox console and the desktop.

The unit + boundary tiers are already green (67/67 Library, 5/5 Heads, all Xbox pages
Debug-UWP `exit 0`). This runbook covers the live tier.

---

## A. Bring up a live RomM (+ bridge)

The compose stack lives in the operator's RomM repo (`f:\github\romm`) and builds the
`romm-gb64:local` + `romm-csdb-bridge:local` images from source.

```pwsh
# 1. Start Docker Desktop (Linux engine) and wait for the daemon.
# 2. Build + start RomM (:8080), the CSDb bridge (:8090), and MariaDB:
cd f:\github\romm
docker compose up -d --build
# 3. Wait until the RomM heartbeat answers:
curl http://localhost:8080/api/heartbeat
```

> The heartbeat is unauthenticated - the ViceSharp LAN scan (AC-CONN-07) finds the server
> off this endpoint. Browse/collections/download need a token (next step).

## B. Create an account and mint a Client API Token (operator only)

Creating the RomM admin account and minting the token is a **human step** - account
creation and authentication are not automated by the agent. In the RomM web UI
(`http://localhost:8080`): complete the first-run setup wizard, sign in, then generate a
**Client API Token** (`rmm_...`). Seed a small C64 library so browse/download has
something to return (add a couple of `.d64`/`.crt` titles for the `c64` platform).

> **No pairing code required.** RomM 5.0.0 supports long-lived Client API Tokens created
> directly (`POST /api/client-tokens`, or the web UI's client-tokens page) - the same
> mechanism the csdb-bridge uses via `ROMM_API_TOKEN` (`f:\github\romm/docker-compose.yml`).
> The device-pairing / OAuth device-code flows (`/api/client-tokens/pair/{code}/status`,
> `/api/auth/device/*`) are a convenience for typing-averse 10-foot UIs, NOT a requirement:
> hand the client a pre-generated token and it connects with no code. RomM has **no
> anonymous/no-auth API mode** (endpoints return HTTP 403 without a token), so a token is
> always needed - it just doesn't have to come from a pairing code. ViceSharp accepts the
> token directly (token box on every RomM page + `FileRomMConnectionStore`), so
> `RomMPairingCoordinator` is optional.

## C. Run the automatable gateway/VM E2E suite

Two ways to authenticate. **(1) Explicit token** - set `VICESHARP_ROMM_TOKEN`. **(2) Bridge
self-provision (no token)** - set only `VICESHARP_CSDB_BRIDGE_URL` (and leave the token
unset); the fixture calls the bridge `GET /romm/v1/connection?user_id=...` (default id
`vicesharp-e2e`, override with `VICESHARP_ROMM_USER_ID`), which ensures a RomM user and
returns its credentials, and the suite logs in via the OAuth password grant. Requires the
bridge redeployed with the `/romm/v1/connection` endpoint and an **admin** `ROMM_API_TOKEN`.

```pwsh
cd f:\github\vice-sharp-romm      # (or the worktree: f:\github\vice-sharp-romm)
$env:VICESHARP_ROMM_INTEGRATION = '1'
$env:VICESHARP_ROMM_URL         = 'http://localhost:8080/'
$env:VICESHARP_ROMM_TOKEN       = 'rmm_...'          # from step B
$env:VICESHARP_CSDB_BRIDGE_URL  = 'http://localhost:8090/'   # optional, enables the CSDb test
dotnet test tests\ViceSharp.Library.IntegrationTests\ViceSharp.Library.IntegrationTests.csproj -c Debug
```

The suite (`ViceSharp.Library.IntegrationTests`, `Category=Integration`) covers:

| Test | Exercises | AC |
|------|-----------|----|
| `Heartbeat_Reachable` | connection/auth to the live server | AC-CONN-01 |
| `ResolvePlatform_C64_ReturnsId` | slug -> platform id | AC-BROWSE-02 |
| `Browse_C64_ReturnsPage` | live browse page mapping | AC-BROWSE-01 |
| `Detail_And_Download_FirstLaunchable` | detail + real byte download to the cache | AC-LAUNCH-01 |
| `Collections_RoundTrip` | create -> list -> rename -> delete | AC-COLLECT-02/04 |
| `Csdb_Bridge_Search` | csdb-bridge search (needs the bridge URL) | AC-CSDB-04 |

Behaviour:
- **Opt-in.** Without `VICESHARP_ROMM_INTEGRATION=1` the tests **skip** with a clear reason
  (they never falsely pass). This suite is excluded from the Nuke `Test`/`CiTest` gates.
- **Fail-loud.** With the flag set but the server down/misconfigured, the fixture's
  heartbeat gate fails the whole class (no silent green).
- Green here is the receipt for the gateway/VM half of Phase E.

## D. On-device GUI + gamepad acceptance (manual)

Deploy and drive each head against the same live server.

**Xbox** (`docs/xbox/on-console-setup-runbook.md` for deploy):
- [ ] Home -> **Library**: **Scan LAN** finds the RomM server (or type the URL), Connect,
      C64 tiles list. Confirm on a **retail** console profile whether the LAN scan works
      (the `privateNetworkClientServer` capability may still be sandbox-limited on Xbox; if
      so, use manual URL / device pairing) - AC-CONN-07, AC-XUI-02.
- [ ] (A) Attach and (Y) Attach+autostart boot a `.d64` - AC-XUI-03.
- [ ] **Details** page: cover/metadata/files/add-to-list - AC-XUI-05.
- [ ] **Lists** page: create/rename/delete, membership - AC-XUI-06.
- [ ] **CSDb** page: search -> ingest -> scan -> the new title appears in Library - FR-CSDB.
- [ ] No `RPC_E_WRONG_THREAD` on async tile/cover load; TV-safe layout - AC-XUI-04.

**Avalonia desktop**:
- [ ] Library tab: **Scan LAN** / URL + token -> Connect -> browse/search - AC-AUI-01/02.
- [ ] Select a title -> the right **details pane** shows metadata/files/add-to-list - AC-AUI-03.
- [ ] Lists tab: create/delete; CSDb tab: search -> ingest -> refresh - AC-AUI-04, FR-CSDB.
- [ ] (Attach + play) a `.d64` -> the C64 boots.

## E. Tear down

```pwsh
cd f:\github\romm
docker compose down          # add -v to also drop the volumes
```

---

### Status

- **A-D (unit/boundary/build tiers): complete and green** without a live server.
- **Phase E gateway/VM suite: written + compile-verified**, skips cleanly when not opted in
  (6/6 skipped, 0 failed). Run steps A-C to turn it green against a live RomM.
- **Phase E on-device GUI/gamepad: operator-run** (needs the console + live server).
