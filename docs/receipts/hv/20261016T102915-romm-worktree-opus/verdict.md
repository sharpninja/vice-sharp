# Hostile review verdict: non-Xbox RomM desktop integration

Reviewed 2026-10-16T11:10:10 (America/Chicago local). Reviewer claude-opus-5-5 effort max (ClaudeCode CLI session 0811d877-c1c8-483f-95f3-89d066d5594b).
Runtime receipt: Win32_Process: claude.exe PID 22016 (parent of PowerShell.MCP.Proxy 23624) command line includes --session-id 0811d877-c1c8-483f-95f3-89d066d5594b --model claude-opus-5-5 --effort max; launch-result.json pid 22016.
Profile: add-profile executed first: C:\Users\kingd\.claude\skills\add-profile\SKILL.md and C:\Users\kingd\.claude\profile\PROFILE-MERGED.md read in full (1 profile file). Console: PowerShell.MCP isolated console agent_id sa-47738b43 (PID 35232).
MCP session ClaudeCode-20261016T153328Z-plugin-session, turn req-20261016T153331Z-prompt-bc1e; parent Codex-20261007T135626Z-plugin-session / req-20261016T150502Z-turn-2379.
Request: F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\request.jsonl
Response: F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\response.jsonl

## Verdict

- OverallVerdict: DISAGREE
- accuracyPercent: 79. Of 26 implementer claims checked, 21 are accurate (counts, mock-gate chronology, preservation, native immutability, exclusions, dependency, requirement state), 4 are inaccurate or overstated (off-screen retention fixed; 8/8 contracts production-bound; main HEAD fa259f2; and the safe-to-merge headline, which F-01 to F-03 contradict), and 1 is unverifiable (U-01). The score is weighted down for the headline claim.
- completenessPercent: 75. Required scope is a correct merge of every remaining non-Xbox change, tests for each behavior change, and complete receipts. About 3 of the 7 claimed corrections are test-evidenced. Three runtime regressions are unaddressed, four non-Xbox files have no disposition, and two receipts are missing. The preservation and operations scope is complete.
- passCount: 20; failCount: 6; unknownCount: 1
- Code integration: Class 1 (project implementation): selective port of the desktop LibraryView, new LibraryCoverImage and AvaloniaCoverImageLoader, RomMLibraryViewModel changes, RomM desktop contract tests, Nuke RommDesktopTest target and the approved build dependency bump. Result: FAIL (F-01 to F-04 and F-06; F-05 is a receipt-accuracy item).
- Operations: Class 2 (user-directed operations): worktree snapshots, manifests, bundles, and the planned commit, push and removal. Result: preservation and recovery PASS (P-05, P-19). BDPv4 and requirement surfaces are not applied to the operation itself.

## Proceeding to the authorized operations

- Commit: Not justified yet. Fix F-01 to F-04 tests-first (or obtain explicit operator acceptance of each), correct F-05 and F-06 in the record, re-run RommDesktopTest including the new contracts, then run a new hostile review.
- Push: Not justified until the commit above has an accepted review. origin/main = local main = d36c133, so the push would fast-forward.
- Worktree removal: Preservation is verified (P-05, P-19), so removal of the two exact worktrees is safe for data. Defer it, per the operator order (Afterwards) and the record contract, until after an accepted review and a verified push. Revalidate source against manifest immediately before removal; removing the dirty romm worktree requires git worktree remove --force.

## FAIL findings

### F-01 FAIL (High): Lists tab Delete acts on the Library-rail collection, not the collection highlighted in the Lists rail

- File/line: src/ViceSharp.Avalonia/Views/LibraryView.cs:245-253; src/ViceSharp.Avalonia/Views/ListsView.cs:48-60, 174-182, 290-320
- Evidence: Runtime probe P3 (synthetic-probe-result-20261016T110051.json): Lists rail highlighted id 2 (Y list); Collections.SelectedCollection id 1 (X list); ListMemberTiles ids [1,2] (X members); clicking Lists Delete issued DeleteCollectionAsync for id 1. Code: the new Library rail writes Collections.SelectedCollection and navigates to the Lists tab (LibraryView.cs:249-251); ListsView syncs only from its own rail SelectionChanged (ListsView.cs:48-60), does not observe SelectedCollection (OnHostChanged ListsView.cs:290-320), and Delete deletes SelectedCollection immediately with no confirmation (ListsView.cs:174-182). SelectedCollection writers in src: LibraryView.cs:249 (new), ListsView.cs:54, Xbox ListsPage.xaml.cs:271.
- Rationale: The port added a second desktop writer of the shared collection selection without updating the Lists tab, so an unconfirmed, irreversible server-side delete can target a collection the Lists tab does not highlight. Main had no such path. The Library rail item also stays selected, so choosing the same collection again does not navigate again.
- Correction: Keep one selection source: have ListsView observe Collections.SelectedCollection and select the matching rail row (or route Library-rail navigation through a VM command that ListsView consumes); make Delete act on the visibly highlighted row (disable it when they differ) and confirm before deleting. Write a headless contract test reproducing P3 first (mock gate, then production binding).

### F-02 FAIL (Medium): Returning to the Library tab drops the selected game, disables Attach and rebuilds the cover loader and cache

- File/line: src/ViceSharp.Avalonia/Views/LibraryView.cs:114-126, 350-354, 420-428, 452-472; src/ViceSharp.Avalonia/Views/AttachPanelView.cs:108-130
- Evidence: Probes P2 and P2b (new view, both construction orders): before the tab switch gridSelectedIndex 3, SelectedGroup Game 0004, CanAttach true; detached while on the other tab; after return gridSelectedIndex -1, SelectedGroup null, CanAttach false, new loader instance. Baseline P5 (main fa259f2 LibraryView in the same harness): selection 3, Game 0004 and CanAttach true preserved after return. Code: OnAttachedToVisualTree runs RebuildCoverLoader and BindList (ItemsSource null, then Items); OnDetachedFromVisualTree disposes the loader and clears _boundBrowse; the cleared ListBox selection now writes SelectedGroup = null. Library shares the sidebar TabControl with Peripherals, Settings, Monitor, History, Lists and CSDb (AttachPanelView.cs:108-130), and a new CoverCache refetches every realized cover.
- Rationale: Functional regression against main in a common flow (select a game, attach, check Peripherals or Lists, return) that the new Library rail itself triggers by navigating to Lists. No test covers it.
- Correction: Rebind only when Browse actually changed; keep the loader for the lifetime of the Browse instance (dispose on Browse change or host disposal rather than every visual detach) or restore selection and detail on re-attach. Add a headless test first: select, switch tab, return, then assert selection, CanAttach and loader identity are preserved.

### F-03 FAIL (Medium): Cover grid is not virtualized: every loaded tile is realized and loads and holds a full-resolution cover, so the claimed off-screen fix is unsupported

- File/line: src/ViceSharp.Avalonia/Views/LibraryView.cs:107-113, 386-394; src/ViceSharp.Avalonia/Views/LibraryCoverImage.cs:7-10; src/ViceSharp.Avalonia/Views/AvaloniaCoverImageLoader.cs:38-42; docs/receipts/worktree-integration/20261016T102915-review.md:24
- Evidence: Avalonia 12.0.5 metadata: Avalonia.Controls.WrapPanel derives from Panel, not VirtualizingPanel, and Avalonia.Controls 12.0.5 has no virtualizing wrap panel. Probe P1: ItemsPanel WrapPanel, itemsPanelIsVirtualizing false; 60 items gave 60 realized tile images with 6 inside the viewport; after two Load more pages 180 items gave 180 realized. Baseline P5 (main grid): VirtualizingStackPanel with 8 realized containers for 60 items. Each realized tile starts a cover load on attach (LibraryView.cs:389-394); covers decode at full resolution (new Bitmap(stream)); CoverCache keeps all bytes; BindList after Load more, A-Z, Recents or tab return recreates all tiles and decodes every cover again.
- Rationale: The record says the old loader could retain off-screen loads and that LibraryCoverImage corrected it. With a non-virtualizing panel, off-screen tiles never detach, so the off-screen cancellation in LibraryCoverImage (its summary cites AC-COVER-03) never runs for scrolled-away tiles. Memory and fetch volume grow with every loaded page (60 per page): a scalability regression against the virtualized list in main. FR-ROMM-COVER-001 describes large virtualized grids.
- Correction: Use a virtualizing layout for tiles (for example rows of tiles in a VirtualizingStackPanel, or a vetted virtualizing wrap panel), or bound realized and decoded covers (Bitmap.DecodeToWidth plus eviction). Correct the LibraryCoverImage summary and the record claim. Add a test that realized tiles stay bounded by the viewport, or get explicit operator acceptance of the limitation.

### F-04 FAIL (Medium): Test evidence misses most claimed corrections and all three runtime regressions; the detach behavior preceded its test; traceability is inflated

- File/line: tests/ViceSharp.Heads.Tests/RommDesktopContractTests.cs:25-30, 48-61, 123-134, 136-169; src/ViceSharp.Avalonia/Views/LibraryView.cs:236-268, 365-376, 420-428, 452-461; src/ViceSharp.Avalonia/Views/AvaloniaCoverImageLoader.cs:1-55
- Evidence: The 8 contract tests are: layout presence, layout-assertion negative controls (helper only), 4 LibraryCoverImage lifecycle tests, cover-assertion negative controls (helper only), and the VM detail race. No test exercises GameGroup tile and subtitle binding (#2), loader creation from connected credentials, one loader per Browse and disposal on reconnect or detach (#4), auto-connect on attach (#5), rail navigation to Lists and error capture (#6), the view-level mismatched-detail guard (#7 view part), AvaloniaCoverImageLoader at all, or F-01 to F-03. Chronology: LibraryCoverImage.cs, including OnDetachedFromVisualTree, was created 10:23:02; the detach test arrived with the 10:28:17 test-file edit and first ran at 10:28:38 (the record says later). The test header cites TR-ROMM-THREAD-001 and AC-AUI-03, which no test exercises. Only 6 of the 8 tests bind production code, so 8/8 contracts ran against the real controls/VM overstates.
- Rationale: CLAUDE.md requires tests first for every behavior change, and BDPv4 requires mock-gated tests for the claimed behaviors before implementation. Most behavior changes in the record are claimed by inspection only, and the suite stayed green while F-01 to F-03 shipped.
- Correction: Write tests first (mock gate, then production binding) for #2, #4, #5, #6, the #7 view guard, AvaloniaCoverImageLoader (scheme validation, token only on the server-base client, disposal) and F-01 to F-03. Fix the traceability tags. Restate the binding claim as 6 of 8 production-bound tests plus 2 assertion-helper controls.

### F-05 FAIL (Low): Main HEAD recorded as fa259f2; actual local and origin main HEAD is d36c133

- File/line: docs/receipts/worktree-integration/20261016T102915-review.md:13; docs/receipts/hv/20261016T102915-romm-worktree-opus/prompt.md:10
- Evidence: git rev-parse main and origin/main both give d36c133daa73fdb758222bd68b3e6c26fa551608, a merge made 10:26:27 with parents e5aefb0 and fa259f2. fa259f2^{tree} and d36c133^{tree} are both be3bf170531320501f6b6d8a84934475d4d140c6, and git diff fa259f2 d36c133 is empty. The record, written at 10:29:15, does not mention d36c133.
- Rationale: The content baseline is identical, so the reviewed diff is the same. But the receipts misstate the branch head that the commit will be parented on and that the push will fast-forward from.
- Correction: Record main HEAD d36c133 (a tree-identical merge of fa259f2) in the integration record and in the commit and push receipts.

### F-06 FAIL (Low): Four non-Xbox untracked files in vice-sharp-romm have no recorded disposition

- File/line: F:\GitHub\vice-sharp-romm\docs\receipts\hostile-validator-20260915T201002Z.json and .md; F:\GitHub\vice-sharp-romm\docs\receipts\hv\20260915T201002Z-session-start.request.jsonl and .response.jsonl; docs/receipts/worktree-integration/20261016T102915-review.md:11-18
- Evidence: The worktree status lists all four as untracked, and none exists in main (tracked or on disk). SHA256: AF8289F107A3F283CB16E943D5BA575B4A6E7FDD9039A57A378B57D392E3EF13, BFEDDCDEB260CBD8416271A53CEC4D0A549EBA7584442EAADACCC0FD6F6A116B, F008F9BEC4D4F73ED5A9E5C323002F3B3B2BBA03103783841E9D5066EA754D9E, 236106FA18CDE1F4A4F4E65D5C1174291000E6D9F670AC440037388766E9AEF8. All four are in the verified snapshot. The other remaining non-Xbox candidates are LibraryView.cs (ported), AvaloniaCoverImageLoader.cs (ported) and RomMBridgeConnectionSourceTests.cs (equal to main). The reviewer did not read their contents (historical review streams).
- Rationale: The request was to review and merge the remaining non-Xbox changes and preserve excluded work. These items are neither merged nor explicitly excluded. They are preserved, so there is no data risk, but the review record is incomplete.
- Correction: State their disposition in the integration record (for example: excluded historical review artifacts, retained in the snapshot) or port them deliberately.

## UNKNOWN findings

### U-01 UNKNOWN (Low): Unreceipted claims: five NU1902/NU1903 reports for cryptography 10.0.10 and two corrected compile failures

- File/line: docs/receipts/worktree-integration/20261016T102915-review.md:48
- Evidence: No restore or build log for those runs is pinned or present. build/obj/project.assets.json now lists System.Security.Cryptography.Xml 10.0.13 and System.Security.Cryptography.Pkcs 10.0.13, and no NuGetAudit suppression exists. The local NuGet v3 cache held no vulnerability file that could confirm the advisories.
- Rationale: The upgrade itself is operator-approved verbatim (Upgrade affected dependencies and continue), so authority is not in question, but the factual justification (advisory count and IDs) and the failed-run disclosures cannot be verified.
- Correction: Attach the failing restore output (or dotnet list package --vulnerable output for 10.0.10) and the two failed-run logs, or remove the unreceipted specifics.

## PASS findings

### P-01 PASS (Info): Input integrity

- File/line: docs/receipts/hv/20261016T102915-romm-worktree-opus/input-pins.json
- Evidence: Reviewer rehash at 10:40: 28 of 28 pinned SHA256 values match; CLI sha256 EB95BB65955F8B1702E800815F9A2C0388A5DE0F196C354C7EE1DD5CB9A2BA23 matches request.jsonl; prompt.md equals the request prompt (sha256 7894FB0FF55999AD63D6849F17617A7CD187E368CF7D9ED945DCBA79C1544CC0).
- Rationale: Every input was rehashed before review.
- Correction: None required.

### P-02 PASS (Info): RomM and Romless branch history claims

- File/line: feat/romm-integration 3b422de; fix/romless-vic-content 7a85e95
- Evidence: git cherry fa259f2 3b422de: e7c2397 patch-equivalent; 96dc227, 30f6652 and b77f236 net to zero after revert 3b422de (3b422de^{tree} = e7c2397^{tree} = ec22ae927323b05ce6d6e82f66052b33c63b87c9; git diff e7c2397 3b422de is empty). git cherry fa259f2 7a85e95: both commits patch-equivalent; the romless worktree has 0 status entries.
- Rationale: Historical importer commits net to zero; Romless has no additional code to port.
- Correction: None required.

### P-03 PASS (Info): Xbox and shared joystick-detach exclusion

- File/line: src/ViceSharp.Xbox/Platform/WinRtGamepadSource.cs:121-122 (romm worktree)
- Evidence: Outside the interface and its plumbing, the only production callers of SetJoystickPortEnabled are WinRtGamepadSource.cs:121-122 (Xbox). The C64MemoryMap.cs and InputServiceHost.cs changes are tagged FEAT-XKBDJOYDETACH-001. Main has 0 occurrences. All excluded files are in the verified snapshot.
- Rationale: The exclusion follows callers and feature attribution, not file names. The record does not name C64MemoryMap.cs, but it belongs to the same feature set.
- Correction: None required.

### P-04 PASS (Info): Connection-source test and Xbox cover loader already equal main

- File/line: tests/ViceSharp.Library.Tests/Discovery/RomMBridgeConnectionSourceTests.cs; src/ViceSharp.Xbox/RomM/XboxCoverImageLoader.cs
- Evidence: Worktree file blobs equal the fa259f2 blobs: 394bb3283e582198f2c1034ea274b675d5b7b51d and e9a5b0f55f062790024c2e2b335c7492217a8916.
- Rationale: No duplicate port is needed.
- Correction: None required.

### P-05 PASS (Info): Preservation snapshots, manifests and bundles

- File/line: .local-preservation/worktrees-20261016T101333
- Evidence: Source, manifest and snapshot counts: romm 1510/1510/1510 and romless 1284/1284/1284, with 0 path or hash differences and 0 reparse points. Bundle sha256 values match. git bundle verify succeeds; prerequisites 978b5a6 and ed0b67f are ancestors of main. Branch refs exist locally and on origin. No staged entries. All worktree HEAD-reflog commits (12 and 1) are reachable from refs.
- Rationale: Excluded and dirty work is fully recoverable after worktree removal.
- Correction: None required.

### P-06 PASS (Info): Native VICE immutability

- File/line: native/vice; .local-preservation/worktrees-20261016T101333/native-before.json
- Evidence: Tracked plus untracked-nonignored files total 6374 (3847 + 2527): 0 additions, 0 removals and 0 hash differences against native-before.json. Re-verified at 10:48 and again after the reviewer Nuke runs at 10:53.
- Rationale: Neither the integration nor this review changed native VICE.
- Correction: None required.

### P-07 PASS (Info): Scoped validation counts reproduced independently

- File/line: TestResults/romm-desktop/20261016T105139-mocks; 20261016T105215-real; 20261016T105229-real
- Evidence: Reviewer runs of ./build.ps1 RommDesktopTest --configuration Release: mock gate 8/8 passed; real run Heads 23/23 and Library 201/201, 0 failed, 0 skipped. The Windows filter excludes exactly the 13 NotExecuted UserLocalDeploymentTests (SkipUnless IsLinux). The 201 passed-test names are identical between the unrestricted and filtered Library runs.
- Rationale: The claimed 23 + 201 green, with zero failures and skips in the approved Windows scope, is accurate.
- Correction: None required.

### P-08 PASS (Info): Earlier receipts retained and honestly counted

- File/line: TestResults/romm-desktop/real; TestResults/romm-desktop-real-console.log
- Evidence: Unrestricted run: Heads 21/21; Library total 214, passed 201, 13 NotExecuted (all UserLocalDeploymentTests). Final run: Heads 23, Library 201. Skips were not counted as passes.
- Rationale: Matches the record.
- Correction: None required.

### P-09 PASS (Info): Mock-gate chronology for the claimed first slices

- File/line: TestResults/romm-desktop-mock-console.log; romm-desktop-mock-detail-console.log
- Evidence: The 6/6 mock run at 10:20:22 precedes the 10:23:02 creation of LibraryCoverImage.cs and AvaloniaCoverImageLoader.cs, which the view port depends on, so the port could not compile earlier. The 7/7 detail-race mock at 10:26:05 precedes the 10:26:37 last write of RomMLibraryViewModel.cs. The same assertions bind to production through the VICESHARP_ROMM_CONTRACT_MOCKS switch.
- Rationale: Supports the stated order for those slices. The detach slice is covered under F-04.
- Correction: None required.

### P-10 PASS (Info): Whitespace, line-ending and dash hygiene

- File/line: scoped tracked and untracked files
- Evidence: git diff --check fa259f2 exits 0. All new and modified files use LF per .gitattributes, with no trailing whitespace, no BOM, no tab indentation and no en or em dashes.
- Rationale: Clean.
- Correction: None required.

### P-11 PASS (Info): Backup tool static checks

- File/line: tools/Backup-LinkedWorktrees.ps1
- Evidence: Parser reports 0 errors; PSScriptAnalyzer 1.25.0 reports 0 findings at Warning, Error and Information.
- Rationale: The analyzer claim is accurate.
- Correction: None required.

### P-12 PASS (Info): Approved dependency upgrade

- File/line: build/_build.csproj:20
- Evidence: System.Security.Cryptography.Xml moves from 10.0.10 to 10.0.13; project.assets.json lists Xml 10.0.13 and Pkcs 10.0.13; no NuGetAudit disablement anywhere; Directory.Packages.props is unchanged. The operator approval is quoted verbatim.
- Rationale: Authorized and correctly applied; the justification receipt is tracked in U-01.
- Correction: None required.

### P-13 PASS (Info): Requirement scope stays visible and unsatisfied

- File/line: workflow.requirements.effective layer-1
- Evidence: Live query for the 11 scoped IDs: all pending, 0 satisfied ACs, identical to the captured effective-requirements.json. No done state is claimed.
- Rationale: Unimplemented ACs were not silently passed.
- Correction: None required.

### P-14 PASS (Info): Cover path security

- File/line: src/ViceSharp.Avalonia/Views/AvaloniaCoverImageLoader.cs:19-30; src/ViceSharp.RomM/RomMCoverImageSource.cs:26-55
- Evidence: The loader requires an absolute http or https server URL, attaches the token only to the server-base HttpClient, and fetches url_cover through a separate anonymous client. RomMCoverImageSource rejects absolute and backslash authenticated paths. Streams and clients are disposed. Credentials come from the last successful connection (ConnectedServerUrl and ConnectedToken), not from edited form fields.
- Rationale: No token leakage path was found in the new code.
- Correction: None required.

### P-15 PASS (Info): Selective-port fidelity against the original romm files

- File/line: F:\GitHub\vice-sharp-romm\src\ViceSharp.Avalonia\Views\LibraryView.cs; AvaloniaCoverImageLoader.cs
- Evidence: The dirty romm files were compared with their HEAD and main and with the port. Verified corrections: main controls kept (connect row, discovered servers, A-Z strip, Recents, Load more, Attach and Attach + play, Add); GameGroup tiles with subtitle; loader built from successful credentials once per Browse and disposed; remembered-server TryAutoConnectAsync instead of ScanAsync; rail reuses the Lists member path with error capture; stale detail suppressed by generation, gateway identity and a view guard; disposal of streams, clients and bitmaps. The exception is the off-screen retention covered in F-03.
- Rationale: Six of the seven old-view defects are verifiably corrected.
- Correction: None required.

### P-16 PASS (Info): TestHarness scope that cannot run here, checked statically

- File/line: tests/ViceSharp.TestHarness/AvaloniaBoundaryTests.cs:51-73; XmlDocsConventionTests.cs:55-56
- Evidence: None of the AvaloniaBoundaryTests forbidden strings appear in the changed Avalonia files. ViceSharp.Avalonia.csproj is unchanged. XmlDocsConventionTests scans the TestHarness directory only. No TestHarness test references the changed desktop types.
- Rationale: Not running the native-coupled TestHarness does not hide a failure in this diff.
- Correction: None required.

### P-17 PASS (Info): Nuke RommDesktopTest target

- File/line: build/Build.Romm.cs:11-48
- Evidence: The build log lists only managed projects (no native or TestHarness project). The target throws on zero selected tests or any non-passed test, so skips fail the gate. It restores VICESHARP_ROMM_CONTRACT_MOCKS and timestamps TRX output.
- Rationale: Safe, managed-only gate.
- Correction: None required.

### P-18 PASS (Info): VM detail-race correction

- File/line: src/ViceSharp.Avalonia/ViewModels/RomMLibraryViewModel.cs:376-397
- Evidence: ShowDetailAsync increments a generation before the null check and publishes success or error only while the generation and the gateway identity still match. Detail_LastSelectionWins is production-bound and would fail without the generation check.
- Rationale: Correct, and test-evidenced for the generation path.
- Correction: None required.

### P-19 PASS (Info): Operations: recovery and removal readiness (class 2)

- File/line: docs/receipts/worktree-integration/20261016T102915-review.md:50-58
- Evidence: Snapshots, manifests and bundles support recovery: add a worktree on the retained branch ref, then overlay the snapshot excluding the saved .git pointer. /.local-preservation/ is gitignored. The reviewer revalidated source against manifest at 10:47 with 0 differences.
- Rationale: Removing the two exact worktrees is preservation-safe. Per the operator order (Afterwards) and the record contract, removal follows an accepted review and a verified push, with a manifest revalidation immediately before.
- Correction: None required.

### P-20 PASS (Info): Workspace rules for the integration diff

- File/line: git status (scoped)
- Evidence: The diff touches no TODO or requirements storage files. Tooling is PowerShell and Nuke only, with no Python or Node. Native is untouched. This review changed no product files; its writes are limited to receipts and its own MCP session.
- Rationale: Compliant.
- Correction: None required.

## Runtime probe summary

Result file: F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe-result-20261016T110051.json
- P1: New grid: WrapPanel, not virtualizing; 60 items with 60 realized tile images, 6 in viewport; 180 realized after two Load more pages.
- P2: New view tab return: selection 3 -> -1, SelectedGroup Game 0004 -> null, CanAttach true -> false, new loader instance.
- P2b: Same as P2 with production order (view shown before Browse published).
- P3: Lists rail highlights id 2; SelectedCollection id 1; Delete requested id 1.
- P4: Rail All games after search and Recents: final state correct (suspicion withdrawn, O-05).
- P5: Main fa259f2 LibraryView baseline: VirtualizingStackPanel, 8 realized of 60; selection and CanAttach preserved across tab return.

## Observations (not scored)

- O-01: An intermediate real run at 10:26:49 is not mentioned in the record (TestResults/romm-desktop-windows-console.log; 20261016T102649-real Heads 22/22; 20261016T102702-real Library 201/201). The first 6/6 mock TRX was overwritten by the 7/7 run (WARNING: Overwriting results file), so its only receipt is romm-desktop-mock-console.log.
- O-02: Every Nuke log contains [WRN] Exception was suppressed from Nuke Telemetry.CheckAwareness (InvalidProjectFileException: NuGet.Frameworks 7.9.0.0 load mismatch). This is environment-level; the target and tests are unaffected.
- O-03: Pre-existing, not introduced here: TryConnectInternalAsync assigns _gateway and _collectionsGateway before InitializeAsync succeeds (RomMLibraryViewModel.cs:312-322). A failed reconnect therefore leaves details and collections on the failed client while Browse and covers stay on the previous server. Recommend a tracked TODO.
- O-04: tools/Backup-LinkedWorktrees.ps1 hardcodes F:\GitHub\vice-sharp-romm and F:\GitHub\vice-sharp-romless and runs git status without --no-optional-locks. Recommend keeping it as a receipt rather than product tooling, or parameterizing it before commit.
- O-05: Withdrawn suspicion: static reading suggested that rail All games, after a search plus Recents, keeps the stale search. Probe P4 shows the final state is correct (search box empty, VM SearchText empty, last query unfiltered, Recents off); there is only one transient extra query.
- O-06: native/vice has pre-existing dirty state (12 modified C sources, 2527 untracked files) unrelated to this integration and unchanged by it. It is outside this scope but relevant to MEMORY-DECISION-001.
- O-07: Receipts under TestResults/, and the HV request, response and stderr files, are gitignored, so they remain local-only evidence on PAYTON-LEGION2.
- O-08: Reviewer probe artifacts (synthetic-probe/, synthetic-probe-*.log and .json, reviewer-nuke-*.log) are evidence, not product code; do not commit the probe project as product code. synthetic-probe-result-20261016T105946.json holds an unfaithful first P5 baseline harness (Browse published before the main view was constructed, so main never bound its list); it is superseded by synthetic-probe-result-20261016T110051.json.

## Environment and process notes

- MCP: reviewer session ClaudeCode-20261016T153328Z-plugin-session, live turn req-20261016T153331Z-prompt-bc1e. A twin turn req-20261016T153331Z-prompt-1636 (status canceled) was created by duplicate user-level bridge hooks plus plugin hooks; the reviewer did not cancel any historical turn.
- Triage filed and accepted: triage-report-f6bf0bebab4a4f12bbd69b1693745116 (twin turns from duplicate hook registration), triage-report-9ca8b6e2a31e4ed79fda9420f2fccdbf (client.SessionLog.GetAsync unknown method used by plugin-hook.ps1), triage-report-243bc62d02e64be4ac5119103daaa169 (setTurnTitle false rejection).
- Failsafe: 1 entry queued, not drained (MCP_FAILSAFE_DRAIN_DISABLED=1): .mcpServer\claude\reviews\20261016T102915-romm-worktree-opus\failsafe\pending\20261016T153937Z-session_setTurnTitle-e599.yaml. Its intended effect (turn title) is already persisted, confirmed by QueryAsync read-back.
- Startup validation conflict: CLAUDE.md asks to delete stale plugin caches and install hooks; the operator brief prohibits environment repair. Inspection only: no stale mcpserver cache entries (only mcpserver-local\mcpserver\1.119.0); hooks are registered in both user settings and plugin hooks.json; no repair performed.
- Plugin version 1.119.0 comes from the plugin .version and plugin.json; the marker field says 1.118.0 (stale marker field).
- The PowerShell.MCP console does not inherit the claude.exe environment, so the review environment from request.jsonl was applied at process scope before any plugin call.

## Receipts

- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\request.jsonl (sha256 C12B0F007009E7F41810193D29234200F848D75CE147F853803EC3BFEAC4198C)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\response.jsonl
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\verdict.md
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\verdict.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\verdict.persistence.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\reviewer-nuke-mock-20261016T105133.log (sha256 A196B2029F4E20259C4AC3903D5DA7F6B9EEC246708E5F216BCE90F9C10FA3DD)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\reviewer-nuke-real-20261016T105211.log (sha256 ED920B7E54070F6F2DA0AA3D5C0877E46C0E5AB6332F4A9C62028F69FF2F4EE4)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe-build-20261016T105806.log (sha256 486A4E448BC846CEE9496C539B10381DEB1DFF74B1C36AD6857EC7BCE56AF642)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe-build-20261016T105944.log (sha256 429404B239C725D82427311144DEB23016BE630BAA12D3323D30245C5A2DA29A)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe-build-20261016T110050.log (sha256 61961A3353C9A46539AA4F65D85B1ED4893FAB3825D98EB3030368367EDB94A5)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe-result-20261016T105946.json (sha256 3DD25F70A7427295617A6605027D748E87ADDAFB2E72B98007A7E0AB3CEB44E3)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe-result-20261016T110051.json (sha256 DC8303C7BBECA5BDE2B0EC4BFDD6A9FDE2E897EF7D9FA3C55313A5CF82B12E90)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe\BaselineLibraryView.cs (sha256 8131C5E3CBBF7BC1F891A8B527A65CA4CAFD407876392B3635989AF29D502585)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe\Directory.Build.props (sha256 469B0C0CBD688C03D5FD94A45E684CAA5553CC4C6B3206AC4150EF1733624ED1)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe\Directory.Build.targets (sha256 469B0C0CBD688C03D5FD94A45E684CAA5553CC4C6B3206AC4150EF1733624ED1)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe\Directory.Packages.props (sha256 469B0C0CBD688C03D5FD94A45E684CAA5553CC4C6B3206AC4150EF1733624ED1)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe\Program.cs (sha256 77E2D64B4967DF527B3997AC641BDEDBFADBFFD7DFFBAE9A45459312D02BD336)
- F:\GitHub\vice-sharp\docs\receipts\hv\20261016T102915-romm-worktree-opus\synthetic-probe\ReviewProbe.csproj (sha256 B744DB2535AE5DD9CA30EB280BBA24D42389972E314CC457157480862FCB0E6D)
- F:\GitHub\vice-sharp\TestResults\romm-desktop\20261016T105139-mocks\ViceSharp.Heads.Tests.trx (sha256 E21954A50B63A222EA12AF9ACB659DF409886F8E4DCFEF278D6195C8ACA84045)
- F:\GitHub\vice-sharp\TestResults\romm-desktop\20261016T105215-real\ViceSharp.Heads.Tests.trx (sha256 C9A7393B4E3F028AA461CD9196D3F91278600F2AE9DECEFE53BE0D270B16070E)
- F:\GitHub\vice-sharp\TestResults\romm-desktop\20261016T105229-real\ViceSharp.Library.Tests.trx (sha256 667799D3339B329EDD208EF56A94398123CEF16BD8DD7CFB0B3933154881C95D)
