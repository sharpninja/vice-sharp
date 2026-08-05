# PLAN-ROMM-001: Integrate RomM game library into the Xbox and Avalonia clients

Byrd Development Process v4. Approved plan of record. The canonical planning copy lived at
`C:\Users\kingd\.claude\plans\majestic-dazzling-sparrow.md`; this is the repo-committed mirror.

## Context

ViceSharp today can only attach a single media file the user picks from disk; there is no game-library
browsing. `f:\github\romm` is the operator's CBM-centric RomM server (a thin fork of upstream
`rommapp/romm` v5.0.0) plus the pre-built `RomM.Client` 1.0.0 and `RomM.Client.Csdb` 1.0.0 NuGet
packages (both live on nuget.org) and a `csdb-bridge` sidecar. The goal is to let both ViceSharp
front-ends browse, search, list, and launch thousands of titles from RomM, plus discover/ingest scene
content from CSDb:

- **Xbox (UWP) head**: an Xbox-Game-Pass-style 10-foot tile grid; single RomM source, platform bound to
  the active machine (Settings), with search, A-Z jump, server-backed lists, gamepad launch.
- **Avalonia desktop head**: a Steam-style library grid with the same features, mouse/keyboard driven.

Outcome: pick a game in either client, it downloads, attaches to the right slot, and boots. Lists persist
server-side and sync across both clients. CSDb releases can be searched, ingested, and launched as normal
RomM games.

This plan follows Byrd Development Process v4 (`F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`):
requirements first (FR/TR/TEST with acceptance criteria), TDD per increment (Fowler Red -> Green ->
Refactor, Byrd mocks-first then real), and a 100% green full-suite regression gate before exiting any
phase. Every acceptance criterion below is covered by at least one named test written red-first; UI-only
ACs that cannot be unit-tested are covered by the integration/dev-PC validation tier and are marked `[V]`.

## Locked decisions (from the operator)

1. **Consume `RomM.Client` 1.0.0 and `RomM.Client.Csdb` 1.0.0 from nuget.org** (already published,
   verified live). vice-sharp `NuGet.config` stays `<clear/>` + nuget.org-only. No upstream RomM.Client
   source changes: reach not-yet-typed endpoints (Collections, Search, device-pairing, cover fields,
   char_index) via RomM.Client's generic `Transport.SendAsync`, deserialized with source-gen DTOs in
   `ViceSharp.RomM`.
2. **CSDb in scope now**, via `RomM.Client.Csdb` (`CsdbClient` search + `CsdbRomMWorkflow` ingest) where
   the RomM roms root is locally writable, with the `csdb-bridge` sidecar (`:8090`) as the
   remote/sandboxed fallback (Xbox).
3. **Lists = RomM server-side collections** (`/api/collections`).
4. **Build shared core first, then both heads together** per feature.
5. **All new vice-sharp RomM code ships as standalone NuGet packages** (`ViceSharp.Library.ViewModels`,
   `ViceSharp.RomM`) to nuget.org with the existing ViceSharp package set.
6. **No source or platform pickers in the library UI.** The only library source is RomM; CSDb is a
   separate discovery/ingest surface that feeds RomM, not a browse source. The browse platform is always
   the currently-selected machine from Settings, resolved to its RomM slug
   (`c64/c128/c-plus-4/vic-20/cpet`) and applied automatically; changing the machine re-scopes the
   library. No Local/RomM/CSDb tab, no platform chip row: a read-only machine indicator only.

## Architecture (public interfaces documented before implementation, per BDP)

Two new packable vice-sharp libraries mirror the proven seam split (`IRomAcquirer` in
`src/ViceSharp.Xbox.ViewModels/IRomAcquirer.cs` implemented by
`src/ViceSharp.Xbox/RomProvisioning/RomFetchRomAcquirer.cs`):

- **`ViceSharp.Library.ViewModels`** (portable, packable): source-agnostic browser VMs + seams + DTOs.
  References ONLY `ViceSharp.Protocol`. No HTTP/RomM.Client/engine/Host/Avalonia/WinRT. `IsPackable`,
  GPL-2.0-or-later.
- **`ViceSharp.RomM`** (portable, packable): the RomM+CSDb adapter. References
  `ViceSharp.Library.ViewModels`, `ViceSharp.Protocol`, `RomM.Client`, `RomM.Client.Csdb`. Holds the
  HTTP/IO, the source-gen `RomMJsonContext` for untyped-endpoint DTOs, and the CSDb gateways. `IsPackable`;
  nuspec deps = the two romm packages + the two vice-sharp packages.
- **Heads** reference both libraries (project ref in-solution; packages for external reuse) and supply the
  head impls.

Public seam interfaces (in `ViceSharp.Library.ViewModels`):
- `IRomMLibraryGateway`: `ResolvePlatformIdAsync(slug)` (internal `/api/platforms` slug->id lookup,
  cached); `BrowseAsync(LibraryQuery{searchTerm,platformId,limit,offset,order,jumpLetter?})` ->
  `LibraryPage{items[RomTile],total,offset,charIndex}` (`platformId` always the active machine's; no
  `source` field); `GetRomAsync(romId)` -> `RomDetail`;
  `DownloadAsync(romId,fileName,cacheDir,IProgress<double>?)` -> `AcquiredGame`; `TriggerScanAsync`;
  collections `GetCollectionsAsync(includeSmartVirtual)`, `CreateCollectionAsync(name)`,
  `RenameCollectionAsync`, `DeleteCollectionAsync`, `AddRomsAsync(id,romIds)`,
  `RemoveRomsAsync(id,romIds)`.
- `IGameLauncher`: `LaunchAsync(AcquiredGame, MediaSlot, autostart)` -> `LaunchOutcome`.
- `ILibraryImageLoader`: `OpenCoverAsync(CoverRef)` -> `Stream`.
- `ICsdbGateway`: `SearchAsync(q, kinds, limit)` -> `CsdbHit[]`; `IngestAndScanAsync(selections(<=20),
  force)` -> `CsdbIngestResult`.
- `IRomMConnectionStore`: `LoadAsync`/`SaveAsync(RomMConnection{baseUrl,authMode,token})`/`ClearAsync`.
- `ICurrentMachineProvider`: `GetActivePlatformSlug()` -> the active machine's RomM slug
  (`c64/c128/c-plus-4/vic-20/cpet`) + a `PlatformChanged` event; the browse VM scopes every query to it
  and reloads when it changes. There is no in-library platform picker; the head binds this to the existing
  Settings machine selection.
- `AcquiredGame(LocalPath, FileName, MediaKind)` neutral launch handle; `MediaExtensionMap` (extension ->
  `MediaSlot` + `IsLaunchable`); `LibraryObservableObject` (captured-`SynchronizationContext`
  PropertyChanged dispatch, copied from `XboxRomProvisioningViewModel`).

**Package consumption (nuget.org only)**: RomM.Client 1.0.0 + RomM.Client.Csdb 1.0.0 resolve from
nuget.org; pin in `Directory.Packages.props`; no `NuGet.config` change. **Source-gen JSON**: RomM.Client's
internal reflection JSON is runtime-safe (both heads JIT/R2R, not native AOT) and, as a prebuilt package,
never trips vice-sharp's trim analyzer; all NEW vice-sharp JSON uses source-gen contexts (satisfies
`IsAotCompatible`/`EnableTrimAnalyzer`/`TreatWarningsAsErrors`).

Launch primitives (exist): Xbox `App.AttachBootCartridgeAsync` (`App.xaml.cs:725`) +
`InProcessSessionFacade.AttachMediaAsync` payload overload (`:226`) + `MediaSelectionChanged` (`:573`) +
Frame auto-expand (`:344`); Avalonia `ShellViewModel.DropAndStartFileAsync` (`ShellViewModel.cs:150`) +
`AttachPanelViewModel.AttachAsync`. Per-head UI (net-new, no grid/cover-art exists): Xbox
`Views/LibraryPage.xaml` `GridView` to `docs/wireframes/xbox.md`; Avalonia `Views/LibraryView.axaml`
`ItemsRepeater` (pattern `Views/TickHistoryView.axaml` + `ViewModels/TickHistoryViewModel.cs`).

## UI wireframes (SVG)

SVG per repo convention. Seven screens, committed alongside this plan:

- [romm-xbox-library.svg](../wireframes/romm-xbox-library.svg) - **Xbox Game Pass library** (10-foot,
  gamepad): a search box + a read-only machine indicator (platform follows Settings; no source or platform
  picker), a virtualized cover-tile grid with a focused tile, an A-Z jump strip, a selected-game bar with
  slot picker + download progress, and the (A) Attach / (Y) Attach+autostart / (X) Add to list / (B) Back
  gamepad bar. Drives `LibraryBrowseViewModel` (FR-ROMM-BROWSE/LAUNCH, AC-XUI-*).
- [romm-avalonia-library.svg](../wireframes/romm-avalonia-library.svg) - **Avalonia Steam library**
  (desktop): a left collections rail (All games / Favorites / user lists / CSDb discovery / New list ->
  FR-ROMM-COLLECT; lists, NOT sources), a search box + sort, a center cover grid, a right detail panel
  (cover, metadata, Attach+play / Attach only / Add to list -> FR-ROMM-DETAIL/LAUNCH), and a connection
  status bar showing the active machine (FR-ROMM-CONN). AC-AUI-*.
- [romm-csdb-pairing.svg](../wireframes/romm-csdb-pairing.svg) - **CSDb discovery + device pairing**: CSDb
  search with Demo/Crack/SID chips, a checkbox result list (title/type/source) with a selection count
  (max 20) and Ingest-and-scan (FR-CSDB, AC-CSDB-*); and the Xbox device-pairing panel (server URL,
  pairing code, waiting-for-approval, keystore note -> FR-ROMM-CONN, AC-CONN-02).
- [romm-xbox-details.svg](../wireframes/romm-xbox-details.svg) - **Xbox game details** (10-foot): enlarged
  cover + screenshot strip, metadata (genre/players/rating/size/added), an About blurb, a files row, a
  slot picker, and the (A) Attach / (Y) Attach+autostart / (X) Add to list / (B) Back bar. Binds
  `RomDetailViewModel` (FR-ROMM-DETAIL/LAUNCH, AC-XUI-05).
- [romm-avalonia-details.svg](../wireframes/romm-avalonia-details.svg) - **Avalonia game details**
  (desktop): breadcrumb, cover, metadata grid, Attach+play / Attach only / Add-to-list, "In:
  <collections>" membership, an About blurb, and a files table. Binds `RomDetailViewModel`
  (FR-ROMM-DETAIL/LAUNCH, AC-AUI-03).
- [romm-xbox-lists.svg](../wireframes/romm-xbox-lists.svg) - **Xbox list management** (10-foot): the user's
  collections rail (counts, focused list, + New list) and the selected list's games, with (A) Open /
  (X) Remove from list / (Y) Rename list / (Menu) Delete list / (B) Back. Binds `CollectionsViewModel`
  (FR-ROMM-COLLECT, AC-XUI-06).
- [romm-avalonia-lists.svg](../wireframes/romm-avalonia-lists.svg) - **Avalonia list management**
  (desktop): collections rail + a selected-list toolbar (Rename / Delete / Add games) over a games table
  with per-row remove; changes sync to RomM. Binds `CollectionsViewModel` (FR-ROMM-COLLECT, AC-AUI-04).

## Requirements + Acceptance Criteria + covering tests (BDP: FR/TR/TEST, 100% AC->test)

Notation: `AC-x-nn: <criterion> -> <test class>.<method>` (unit test, mocks-first) or `[V]`
(validation-tier: build + dev-PC/integration E2E). All AC IDs and TEST records are registered in MCP at
kickoff and mapped FR -> TR -> TEST.

**FR-ROMM-CONN-001 - connection & auth** (TR-ROMM-NUGET-001, TR-ROMM-JSON-001)
- AC-CONN-01: base URL + client token authenticates; `Authorization: Bearer rmm_...` on requests ->
  `RomMGatewayAuthTests.ClientToken_SetsBearer`.
- AC-CONN-02: device-pair exchange returns a token and persists it via the store ->
  `RomMPairingTests.Exchange_ReturnsAndPersists`.
- AC-CONN-03: OAuth password token auto-refreshes on near-expiry through `RomMAuthHandler` ->
  `RomMAuthRefreshTests.NearExpiry_Refreshes`.
- AC-CONN-04: a 401 sets the connection VM to a "sign-in expired" reauth state and raises
  `ConnectionInvalid` -> `LibraryConnectionViewModelTests.Unauthorized_SurfacesReauth`.
- AC-CONN-05: `FileRomMConnectionStore` round-trips baseUrl/authMode/token via source-gen JSON ->
  `FileRomMConnectionStoreTests.SaveLoad_RoundTrips`.
- AC-CONN-06: token never appears in a URL/query string -> `RomMGatewayAuthTests.Token_NeverInUri`.

**FR-ROMM-BROWSE-001 - browse/search/filter/page/jump** (TR-ROMM-THREAD-001)
- AC-BROWSE-01: `BrowseAsync` maps `CustomLimitOffsetPage` -> `LibraryPage` (items, total, offset,
  charIndex) -> `RomMGatewayBrowseTests.Page_Maps`.
- AC-BROWSE-02: the active machine's platform slug (`c64/c128/c-plus-4/vic-20/cpet`, from
  `ICurrentMachineProvider`) resolves to a numeric id and always scopes the query; there is no user
  platform picker -> `RomMGatewayBrowseTests.PlatformSlug_ResolvesAndFilters`.
- AC-BROWSE-03: `searchTerm` passes through to `search_term` -> `RomMGatewayBrowseTests.SearchTerm_Passed`.
- AC-BROWSE-04: `LoadMoreAsync` appends the next page; `HasMore` false at `offset>=total` ->
  `LibraryBrowseViewModelTests.Paging_AppendsToEnd`.
- AC-BROWSE-05: A-Z jump maps letter -> offset via `charIndex`; `JumpToLetterAsync` loads that page ->
  `LibraryBrowseViewModelTests.Jump_UsesCharIndex`.
- AC-BROWSE-06: rapid search input debounces (300 ms) to a single query ->
  `LibraryBrowseViewModelTests.Search_Debounced`.
- AC-BROWSE-07: changing the active machine (Settings) re-scopes browse to the new platform slug and
  reloads; there is no in-library source or platform selector ->
  `LibraryBrowseViewModelTests.MachineChange_RescopesAndReloads`.
- AC-BROWSE-08: background load raises PropertyChanged on the captured context (no cross-thread) ->
  `LibraryBrowseViewModelTests.OffContext_Dispatches`.

**FR-ROMM-DETAIL-001 - ROM detail**
- AC-DETAIL-01: `GetRomAsync` maps `DetailedRomSchema` (files/cover/summary/launchable) ->
  `RomMGatewayDetailTests.Detail_Maps`.
- AC-DETAIL-02: `RomDetailViewModel` invokes add-to-collection ->
  `RomDetailViewModelTests.AddToCollection_Invokes`.

**FR-ROMM-COLLECT-001 - lists via RomM collections** (TR-ROMM-JSON-001)
- AC-COLLECT-01: `GetCollectionsAsync` lists user collections; smart/virtual flagged read-only ->
  `RomMGatewayCollectionsTests.List_FlagsReadOnly`.
- AC-COLLECT-02: `CreateCollectionAsync` POSTs and returns the collection ->
  `RomMGatewayCollectionsTests.Create_Posts`.
- AC-COLLECT-03: add/remove send `CollectionRomsPayload{rom_ids[]}` on the right verb ->
  `RomMGatewayCollectionsTests.AddRemove_SendPayload`.
- AC-COLLECT-04: rename (PUT) and delete (DELETE) -> `RomMGatewayCollectionsTests.RenameDelete`.
- AC-COLLECT-05: `CollectionsViewModel` add/remove selected roms then refresh ->
  `CollectionsViewModelTests.AddRemove_Refreshes`.
- AC-COLLECT-06: every collections DTO deserializes reflection-free via `RomMJsonContext` ->
  `RomMJsonContextTests.Collections_SourceGenOnly`.

**FR-ROMM-LAUNCH-001 - download -> attach -> boot**
- AC-LAUNCH-01: `DownloadAsync` streams to `<cacheDir>/{romId}/{fs_name}`, reuses on size match, reports
  progress -> `RomMGatewayDownloadTests.Streams_Reuses_Progress`.
- AC-LAUNCH-02: slot = `SelectedSlot` or `MediaExtensionMap` default from `fs_name` ->
  `MediaExtensionMapTests.Extension_MapsSlot`.
- AC-LAUNCH-03: `.prg` is not launchable and Attach is disabled -> `MediaExtensionMapTests.Prg_NotLaunchable`
  + `LibraryBrowseViewModelTests.Prg_AttachDisabled`.
- AC-LAUNCH-04: attach/attach+start invoke `IGameLauncher` with the resolved slot + autostart flag ->
  `LibraryBrowseViewModelTests.AttachStart_InvokesLauncher`.
- AC-LAUNCH-05: Xbox launcher reads bytes -> `AttachMediaAsync` payload -> `AutostartDrive8Async`(disk)/
  `ColdResetAsync`(cart) -> `XboxGameLauncherTests.PayloadAttachAndBoot`.
- AC-LAUNCH-06: Avalonia launcher delegates to `DropAndStartFileAsync`(autostart)/`AttachAsync`
  (attach-only) -> `AvaloniaGameLauncherTests.DelegatesToShell`.
- AC-LAUNCH-07: two-phase status ("Downloading N%" then "Starting") ->
  `LibraryBrowseViewModelTests.Launch_TwoPhaseStatus`.

**FR-ROMM-COVER-001 - cover art + cache**
- AC-COVER-01: `url_cover` fetched without auth; `path_cover_*` fetched with bearer under the confirmed
  prefix -> `RomMCoverImageSourceTests.AuthRules`.
- AC-COVER-02: two-tier cache; the second request for the same cover hits cache (no second HTTP) ->
  `CoverCacheTests.SecondRequest_Cached`.
- AC-COVER-03: concurrency gate (<=4) + off-screen cancellation ->
  `CoverCacheTests.Concurrency_Gated_Cancellable`.
- AC-COVER-04: a fetch failure yields the placeholder and never throws ->
  `CoverCacheTests.Failure_Placeholder`.

**FR-CSDB-001 - discovery + ingest + scan + refresh**
- AC-CSDB-01: `SearchAsync` returns classified hits (demo/crack/sid) ->
  `CsdbDiscoveryViewModelTests.Search_ReturnsHits`.
- AC-CSDB-02: ingest selection capped at 20 -> `CsdbDiscoveryViewModelTests.Ingest_CapsAt20`.
- AC-CSDB-03: `LocalCsdbGateway` ingests via `CsdbRomMWorkflow` then triggers scan ->
  `LocalCsdbGatewayTests.Ingest_WritesThenScans`.
- AC-CSDB-04: `BridgeCsdbGateway` POSTs `/csdb/v1/ingest` then calls `RomMClient.Tasks.ScanLibraryAsync`
  (bridge does not scan) -> `BridgeCsdbGatewayTests.Posts_ThenScans`.
- AC-CSDB-05: completion raises `LibraryRefreshRequested` ->
  `CsdbDiscoveryViewModelTests.Ingest_RaisesRefresh`.
- AC-CSDB-06: gateway selection by config (roms root writable -> Local, else Bridge) ->
  `CsdbGatewaySelectionTests.PicksByConfig`.

**FR-ROMM-XBOXUI-001 - Xbox Game Pass UI + navigation**
- AC-XUI-01: `Library` in `NavigationDestination`, a HomePage button, and `Push` route ->
  `XboxNavigationTests.Library_Routable` (structural source/enum test).
- AC-XUI-02: `LibraryPage` builds `Debug-UWP`, binds `LibraryBrowseViewModel` (grid tiles, search,
  read-only machine indicator, slot picker, A/Y actions, A-Z strip; no source/platform picker) `[V]`
  (Debug-UWP build + dev-PC E2E).
- AC-XUI-03: A = Attach, Y = Attach+Autostart via the input command path `[V]` (dev-PC E2E; command
  mapping unit-tested in `XboxInputContext` if routed through `AppCommand`).
- AC-XUI-04: no `RPC_E_WRONG_THREAD` on async tile/cover load `[V]` (dev-PC E2E; guarded by AC-BROWSE-08).
- AC-XUI-05: `GameDetailsPage` builds `Debug-UWP`, binds `RomDetailViewModel` (cover, screenshots,
  metadata, files, slot picker, A/Y/X actions) `[V]` (Debug-UWP build + dev-PC E2E; VM logic in
  `RomDetailViewModelTests`).
- AC-XUI-06: `ListsPage` builds `Debug-UWP`, binds `CollectionsViewModel` (collections rail, selected-list
  games, X=remove / Y=rename / Menu=delete / +New list) `[V]` (Debug-UWP build + dev-PC E2E; VM logic in
  `CollectionsViewModelTests`).

**FR-ROMM-AVUI-001 - Avalonia Steam UI**
- AC-AUI-01: `LibraryView` builds and binds `LibraryBrowseViewModel` `[V]` (build + headless VM tests
  AC-BROWSE-*).
- AC-AUI-02: connection dialog + browse/search/list/launch end to end `[V]` (E2E).
- AC-AUI-03: `GameDetailsView` builds and binds `RomDetailViewModel` (cover, metadata, files table,
  Attach+play/Attach only/Add-to-list, collection membership) `[V]` (build + `RomDetailViewModelTests`).
- AC-AUI-04: `ListsView` builds and binds `CollectionsViewModel` (rail, toolbar Rename/Delete/Add games,
  per-row remove) `[V]` (build + `CollectionsViewModelTests`).

**FR-ROMM-PKG-001 - NuGet packaging** (TR-ROMM-BOUNDARY-001, TR-ROMM-NUGET-001)
- AC-PKG-01: both libraries `IsPackable` with id + GPL-2.0-or-later metadata ->
  `PackageMetadataTests.Ids_License`.
- AC-PKG-02: `PublishNuget` packs both nupkgs with correct deps (RomM.Client, RomM.Client.Csdb,
  ViceSharp.*) -> `PackageMetadataTests.Dependencies` + `./build.ps1 PublishNuget --skip` `[V]`.
- AC-PKG-03: `ViceSharp.Library.ViewModels` references only Protocol; assembly/source name no `RomM.`/
  `Http`/`Grpc.`/`Avalonia.`/`Windows.`/engine -> `LibraryViewModelsBoundaryTests`.
- AC-PKG-04: restore resolves RomM.Client/Csdb from nuget.org with the nuget.org-only config ->
  `NuGetConfigTests.NugetOrgOnly` + restore `[V]`.

**Technical requirements (cross-cutting ACs):**
- TR-ROMM-BOUNDARY-001: adapter isolates all HTTP; the VM library has no `System.Net.Http` reference ->
  `LibraryViewModelsBoundaryTests.NoHttp` (AC-PKG-03).
- TR-ROMM-JSON-001: 100% of adapter DTOs (Collections/Search/cover/char_index/pairing)
  serialize/deserialize reflection-free -> `RomMJsonContextTests.AllTypes_SourceGen`.
- TR-ROMM-THREAD-001: `LibraryObservableObject` posts off-context, raises inline on-context ->
  `LibraryObservableObjectTests.Dispatch` (AC-BROWSE-08).
- TR-ROMM-NUGET-001: nuget.org-only; RomM packages pinned in CPM -> `NuGetConfigTests` +
  `DirectoryPackagesTests.RommPinned`.

Coverage invariant (checked in review before each merge): every AC has >=1 passing test; every `[V]` AC
has a build/E2E step in Verification. A plan-level `RommAcCoverageTests` asserts the AC-ID list is
non-decreasing and each unit AC-ID appears in a test's `[Trait("AC", "<id>")]`.

## BDP slice plan (tests-first, mocks-first, Red -> Green -> Refactor; 100% green per gate)

Every new test: `[Trait("Category","Library")]` + `[Trait("AC","<id>")]`. Per-slice loop: (1) write the
named tests for the slice's ACs -> confirm RED; (2) implement against mocks/fakes -> GREEN; (3) swap in
real wiring, re-run -> GREEN; (4) refactor tests+code; (5) run the full `Category=Library` + boundary
suites GREEN before exiting the slice. Filter: `--filter "FullyQualifiedName~Library.<Area>"` (or
`Trait=AC` subsets).

**Phase A - package wiring + live confirmation**
- P0: RomM.Client 1.0.0 + RomM.Client.Csdb 1.0.0 already on nuget.org. Pin them in
  `Directory.Packages.props`; `dotnet restore` (AC-PKG-04, `NuGetConfigTests`, `DirectoryPackagesTests`).
  Stand up romm (`docker compose up -d --build`), mint a token, confirm the cover static-asset prefix live
  (records the value for AC-COVER-01). Gate: restore green + config tests green.

**Phase B - shared core (packable libraries)**
- L1: `ViceSharp.Library.ViewModels` + slnx entry/remap; includes `ICurrentMachineProvider` + the
  machine->slug map (`c64/c128/c-plus-4/vic-20/cpet`). ACs: AC-PKG-03, AC-LAUNCH-02, AC-LAUNCH-03(map
  half), AC-BROWSE-08/TR-THREAD, AC-BROWSE-02(slug-map half). Tests: `LibraryViewModelsBoundaryTests`,
  `MediaExtensionMapTests`, `LibraryObservableObjectTests`, `CurrentMachineSlugTests`.
- L2: `ViceSharp.RomM` adapter browse/platforms/detail/download + `RomMJsonContext`. ACs:
  AC-BROWSE-01/02/03, AC-DETAIL-01, AC-LAUNCH-01, AC-CONN-01/06, TR-JSON(browse/detail). Tests:
  `RomMGatewayBrowseTests`, `RomMGatewayDetailTests`, `RomMGatewayDownloadTests`, `RomMGatewayAuthTests`,
  `RomMJsonContextTests`.
- L3: `LibraryBrowseViewModel`. ACs: AC-BROWSE-04/05/06/07, AC-LAUNCH-03(vm)/04/07. Tests:
  `LibraryBrowseViewModelTests`.
- L4: launch orchestration seam. ACs: AC-LAUNCH-04/07 refinement + fake `IGameLauncher`. Tests:
  `LibraryBrowseViewModelTests` (launch), `MediaExtensionMapTests`.
- L5: collections. ACs: AC-COLLECT-01..06, AC-DETAIL-02. Tests: `RomMGatewayCollectionsTests`,
  `CollectionsViewModelTests`, `RomDetailViewModelTests`, `RomMJsonContextTests.Collections`.
- L6: CSDb. ACs: AC-CSDB-01..06. Tests: `CsdbDiscoveryViewModelTests`, `LocalCsdbGatewayTests`,
  `BridgeCsdbGatewayTests`, `CsdbGatewaySelectionTests`.
- L7: covers + connection + pairing + refresh. ACs: AC-COVER-01..04, AC-CONN-02/03/04/05. Tests:
  `RomMCoverImageSourceTests`, `CoverCacheTests`, `RomMPairingTests`, `RomMAuthRefreshTests`,
  `FileRomMConnectionStoreTests`, `LibraryConnectionViewModelTests`.
- L8: packaging gate. ACs: AC-PKG-01/02. Tests: `PackageMetadataTests`; `./build.ps1 PublishNuget --skip`
  packs both nupkgs.

**Phase C - Xbox head**
- X1: `XboxGameLauncher`. AC-LAUNCH-05. Test: `XboxGameLauncherTests`.
- X2: `XboxCoverImageLoader` + `XboxRomMConnectionStore` (portable fallbacks unit-tested; WinRT compiled
  `Debug-UWP`).
- X3: `LibraryPage` + `GameDetailsPage` + `ListsPage` + nav + Settings pairing page. AC-XUI-01
  (`XboxNavigationTests`), AC-XUI-02/03/04/05/06 `[V]` (Debug-UWP build + dev-PC E2E). Xbox uses
  `BridgeCsdbGateway`.
- X4: CSDb tab (reuses L6 VM).

**Phase D - Avalonia head**
- A1: `AvaloniaGameLauncher`. AC-LAUNCH-06. Test: `AvaloniaGameLauncherTests` (fake `IHostProtocolClient`,
  reuse `FakeHostProtocolClient`).
- A2: `AvaloniaCoverImageLoader` + desktop connection store + gateway selection (AC-CSDB-06 head-side).
- A3: `LibraryView` + `GameDetailsView` + `ListsView` + `MainWindow` wiring + connection dialog.
  AC-AUI-01/02/03/04 `[V]`.
- A4: CSDb tab.

**Phase E - integration + human validation** (BDP validation tier): the Verification E2E flows exercise
the `[V]` ACs against a live RomM/bridge on both heads.

Exit gate for every phase: `dotnet test ViceSharp.slnx -c Debug --filter
"Category=Library|FullyQualifiedName~BoundaryTests"` = 0 failed / 0 skipped, plus the two existing
boundary suites and (for head phases) a `Debug-UWP` Xbox build. Never count a skipped test as covered.

## Risks / edge cases

- Untyped endpoints drift from RomM 5.x: each is guarded by a captured-response contract test
  (`RomMJsonContextTests` + gateway tests) parsing a real 5.0.0 body fixture.
- Thousands of titles: incremental paging + `GridView`/`ItemsRepeater` virtualization + `char_index` jump;
  300 ms debounce; never materialize the full list.
- CSDb co-location: `RomM.Client.Csdb` ingest writes a local `LibraryRomsRoot` (desktop-only); Xbox/remote
  use `BridgeCsdbGateway`; `ICsdbGateway` picks per config (AC-CSDB-06).
- UWP sandbox: launcher reads bytes from the app-writable cache -> payload attach; never hand the host an
  arbitrary path (AC-LAUNCH-05).
- Cover prefix confirmed live at P0; `RomMCoverImageSource` prefix is one-line configurable.
- Offline RomM/bridge: gateways surface connection errors as VM status; local-media attach path stays
  functional.
- Baseline caveat: run `AvaloniaBoundaryTests` first - `MainWindow.axaml.cs` already references
  `ViceSharp.Architectures`/`ViceSharp.Chips` for aspect math; confirm the current suite state before
  adding head code.

## Verification (BDP validation tier)

1. `cd f:\github\romm && docker compose up -d --build` (RomM `:8080`, bridge `:8090`); small c64 library;
   mint a Client API Token.
2. Per slice: `dotnet test ViceSharp.slnx -c Debug --filter "FullyQualifiedName~Library.<Area>"` (unit ACs)
   + `--filter "FullyQualifiedName~BoundaryTests"`.
3. Avalonia E2E (AC-AUI-01/02/03/04, AC-XUI parity): open Library, enter URL + token, browse `c64`, search
   + A-Z jump, open a game's details view, create a collection + add a rom, open the Lists view and
   rename/add/remove, pick a `.d64`, (Y) Attach+Autostart -> C64 boots; CSDb tab: search -> ingest -> scan
   -> refresh -> launch.
4. Xbox E2E (AC-XUI-02..06): deploy `Debug-UWP`/`Release-UWP`, pair device, repeat browse/search, open the
   details page, manage a list (add/remove/rename), launch, and CSDb (bridge); confirm cover tiles render,
   no `RPC_E_WRONG_THREAD`.
5. Package gate (AC-PKG-02): `./build.ps1 PublishNuget --skip` packs `ViceSharp.Library.ViewModels` +
   `ViceSharp.RomM`; full gate `dotnet test ViceSharp.slnx -c Debug --filter
   "Category=Library|FullyQualifiedName~BoundaryTests"` = 0 failed / 0 skipped + a `Debug-UWP` Xbox build.

## Critical files to reuse

- Seam/adapter precedent: `src/ViceSharp.Xbox.ViewModels/IRomAcquirer.cs` +
  `src/ViceSharp.Xbox/RomProvisioning/RomFetchRomAcquirer.cs`; threading/confirm-gate:
  `src/ViceSharp.Xbox.ViewModels/XboxRomProvisioningViewModel.cs`.
- Launch primitives: `src/ViceSharp.Xbox/App.xaml.cs` (@725/@573/@344);
  `src/ViceSharp.Avalonia/ViewModels/ShellViewModel.cs` (@150);
  `src/ViceSharp.Xbox/Platform/InProcessSessionFacade.cs` (@226).
- Boundary/fakes: `tests/ViceSharp.TestHarness/Xbox/XboxViewModelsBoundaryTests.cs`,
  `tests/ViceSharp.TestHarness/AvaloniaBoundaryTests.cs` (`FakeHostProtocolClient`).
- Source-gen JSON: `src/ViceSharp.Xbox.ViewModels/XboxSettingsStore.cs`,
  `src/ViceSharp.Xbox/Platform/SnapshotJsonContext.cs`.
- List/async-collection UI: `src/ViceSharp.Avalonia/Views/TickHistoryView.axaml` +
  `ViewModels/TickHistoryViewModel.cs`; `docs/wireframes/xbox.md`.
- Packaging wiring: `ViceSharp.slnx`, `Directory.Packages.props`, `NuGet.config`, `PublishNuget` in
  `build/Build.cs`.
- romm surfaces (packages from nuget.org; local source for reference):
  `f:\github\romm\src\RomM.Client\Clients\ResourceClients.cs`, `Http\RomMTransport.cs`,
  `Auth\RomMAuthHandler.cs`; `f:\github\romm\src\RomM.Client.Csdb\CsdbRomMWorkflow.cs` + `CsdbClient.cs` +
  `CsdbLibraryOptions.cs`; bridge `f:\github\romm\services\csdb-bridge\src\CsdbBridge\Program.cs`; OpenAPI
  `f:\github\romm\openapi\romm-5.0.0.json`; topology `f:\github\romm\docker-compose.yml`.

## Notes for kickoff (BDP)

Before any code: register FR-ROMM-CONN/BROWSE/DETAIL/COLLECT/LAUNCH/COVER, FR-CSDB, FR-ROMM-XBOXUI/AVUI/PKG
and TR-ROMM-BOUNDARY/JSON/THREAD/NUGET as MCP requirements with the exact AC IDs above; create the TEST
records naming each test class/method; create the PLAN-ROMM-001 slice TODOs (L1..L8, X1..X4, A1..A4) with
FR/TR/TEST references; commit the plan to `docs/plans/PLAN-ROMM-BDPv4.md`. RomM.Client 1.0.0 +
RomM.Client.Csdb 1.0.0 are already on nuget.org and consumed as-is; later typed-endpoint additions are a
non-blocking optimization. Both repos are Azure DevOps `origin`; the two new ViceSharp packages publish to
nuget.org with the existing set.
