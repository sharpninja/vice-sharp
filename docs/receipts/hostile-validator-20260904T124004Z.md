# Hostile Validator Receipt (VideoSurface VIC-20 NTSC display fix)

- TimestampUtc: 2026-09-04T12:40:04Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\vice-sharp
- Work class: MIXED. Operator asked to look at the emulated display (class-2 ops / screenshot), then to fix it. Implementer changed product VideoSurface code/tests (class-1). Surface C scored on the display-fix slice only. Do not require PLAN-REMOTECTRL-001 or VIC-20 Exact completion. Do not FAIL C for "no FR for looking at a screenshot."
- add-profile: executed yes, before claim checks. Non-skill profile markdown files read in full: 18 (PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, hostile-on-goal-state.md, hostile-ops-vs-requirements.md, hostile-phase-gates.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md, requirement-change-plan-first.md). Excluded skill port add-profile.grok.md.
- Active plan for this display-fix request: none. Prior session still has PLAN-REMOTECTRL-001 (Done:false) and a completed VIC-20 Exact plan; implementer did not mark those in this turn.
- MCP review session: GrokCode-20260904T123000Z-hostile-display-fix, requestId req-20260904T122823Z-001-hostile-validate-display-fix. Persistence proof: sessionlog_query text "Hostile validate VideoSurface VIC-20 NTSC display fix" totalCount=1, turn status=completed, lastUpdated=2026-09-04T12:43:38.5240143+00:00, 5 actions, 2 designDecisions, 2 processingDialog items, 2 filesModified. Implementer session cited: GrokCode-20260904T020555Z-continue-plan turn req-20260904T121500Z-006-fix-emulated-display.
- Method: add-profile first; live INI read; git show HEAD VideoSurface.UpdateFrom/SetFrame; Mos6561 NTSC constants; independent RemoteControl frame+dump against pid 73780 :47100; independent `dotnet test` Release filters; MCP todo_get PLAN-REMOTECTRL-001; MCP sessionlog_query of implementer turn 006; git diff --check on the slice. No product feature edits. Receipt files only.
- Accuracy rating: 92/100. Product claims re-run or re-read on disk/live tree. Residual 8: PNG OCR of CLOCK digits is untrustworthy (tree text is the source); FR-1132 is a cited ghost ID not present under docs/requirements.
- Completeness rating: 90/100. Surfaces A-D scored. Session-log completeness of implementer turn 006 is the only FAIL. UpdateFrom itself is not unit-tested (BlitPackedBgra is); live frame covers the production path.

## OverallVerdict

DISAGREE

Explicit FAIL list:

- B7 Implementer MCP session-log persistence for turn req-20260904T121500Z-006-fix-emulated-display. Rule: AGENTS.md "Persist session-log updates immediately after each meaningful change" and log-decisions-as-conclusions (designDecisions plus action type design_decision). Evidence: sessionlog_query at 2026-09-04T12:40:04Z still shows that turn status=in_progress, lastUpdated=2026-09-04T12:11:19.8509788+00:00 (before VideoSurface.cs write 12:23:58Z), actions=null, designDecisions=null, filesModified=null. Only the pre-implementation diagnosis exists in processingDialog. Product edits, CS0117 red, green tests, and after-fix PNG were not appended.

Applicable PASS count: A1-A6, B1-B6, C1-C3, D1. FAIL count: 1 (B7). UNKNOWN: none.

Do not treat this DISAGREE as a product-display regression. The sheared VIC-20 NTSC picture is fixed on the live Avalonia head. Do not mark PLAN-REMOTECTRL-001 or whole-machine Exact done. Hostile AGREE is required before any goal-state `done: true`.

## Claims reviewed

### A. Requested validation

- A1 Root cause: persisted SettingsMachineProfileId=vic20ntsc; Mos6561 NTSC canvas 400x234; VideoSurface previously always blitted into a 384x272 bitmap, shearing the picture. Verdict: PASS
- A2 After-fix live RemoteControl frame shows a correct VIC-20 NTSC READY screen (**** CBM BASIC V2 ****, 3583 BYTES FREE, READY., cyan border, white paper, blue text). Receipt PNGs exist and match. Before-fix PNG is sheared. Verdict: PASS
- A3 Live tree after the fix: VideoSurface ContentHeight=234, POWER On, RUN Running, LIMITER 100%, FPS ~59, CLOCK ~1.02 MHz. Verdict: PASS
- A4 Tests: VideoSurfaceGeometryTests Passed 6 Failed 0 Skipped 0. Combined VideoSurfaceTests+AvaloniaVideoAspectTests Passed 16 Failed 0 Skipped 0. Compile-red CS0117 BlitPackedBgra/IsValidFrame before implementation. Verdict: PASS
- A5 Code: VideoSurface.UpdateFrom/SetFrame use published width/height; BlitPackedBgra; IsValidFrame; ILocalVideoFrameSource.TryGetFrameGeometry; MainWindow VIC-20 ContentHeight=0. Verdict: PASS
- A6 Negative claims: implementer does not claim PLAN-REMOTECTRL-001 done, whole-machine Exact, or full-solution tests. Verdict: PASS

### B. Workspace rules

- B1 Honesty of the product claims vs artifacts. Verdict: PASS
- B2 Receipts: machine-verifiable PNG, INI, git HEAD vs working tree, and validator-rerun tests. Verdict: PASS
- B3 MCP-only storage: no direct todo.yaml / session-log file edits found for this slice. Verdict: PASS
- B4 PowerShell-only / no Python on this slice. Verdict: PASS
- B5 Look-before-delete: N/A (no deletions). Scored N/A, not FAIL.
- B6 Byrd v4 for the class-1 code/test slice. Compile-red shown before implementation. Not scored from FR-vs-file timestamps. Implementer did not claim a Byrd phase complete. Verdict: PASS
- B7 Session-log persistence after implementation. Verdict: FAIL (see Explicit FAIL list)

### C. Requirements (display-fix product slice only)

- C1 Applicable FRs exist for already-specified behavior (bug report, not a new requirement add). FR-VIC20-001 AC4 framebuffer contract matches host path; FR-HOST-003 / FR-UI-001 local Avalonia render of host frames with dimensions. Verdict: PASS
- C2 Tests cover the geometry AC written for this bug (packed blit preserves 400x234; 384-wide interpretation shears; IsValidFrame accepts VIC-20 DTO; ComputeDisplayAspect uses live width). Live RemoteControl frame is additional production-path evidence. Not "suite green theater." Verdict: PASS
- C3 Screenshot-only ops slice is N/A for C (operator-directed look). Not scored as FAIL.

Nit, not FAIL: tests cite FR-1132, which is not present under docs/requirements (lock-free/BUG-THROTTLE path in Xbox plan text). The geometry work is still covered by FR-VIC20-001 / FR-HOST-003 / FR-UI-001.

### D. Current plan holistically

- D1 Implementer did not claim PLAN-REMOTECTRL-001 or VIC-20 Exact complete. MCP todo_get PLAN-REMOTECTRL-001 Done=false; remaining TEST-REMOTECTRL-001 and app-launch task still false. Verdict: PASS (N/A for plan-step completion; scored PASS so it does not block except where they overclaimed, which they did not)

## Per-claim evidence

### A1 PASS

INI `C:\Users\kingd\AppData\Roaming\vice\vice-sharp-ui.ini` contains `SettingsMachineProfileId=vic20ntsc`.

Mos6561.cs: PixelWidth=2, NtscNormalDisplayWidth=200, NtscNormalFirstDisplayedLine=28, NtscNormalLastDisplayedLine=261. EnsureFrameBufferSize: displayWidth = 200*2 = 400, h = 261-28+1 = 234.

HEAD `VideoSurface.UpdateFrom` (git show HEAD):

```
const int widthBytes = SourceWidth * 4;
... dest = new Span<byte>((void*)fb.Address, widthBytes * SourceHeight);
if (!source.TryCopyFrameInto(sessionId, dest, out _, out _, out _))
```

HEAD constructor allocates WriteableBitmap at SourceWidth x SourceHeight (384x272). HEAD SetFrame rejects Width != 384 or Height != 272. TryCopyFrameInto published width/height discarded.

Before-fix PNG `docs/receipts/remotecontrol-frame-display-20260904T120800Z.png` (SHA256 B82E5A02E665F5168EEEF2DAB4160381179DC548F12092AD0C0679CDE3FE0A5E) shows diagonal cyan bands. Implementer snapshot-for-display.txt VideoSurface ContentHeight=272.

### A2 PASS

After-fix PNGs:

- `docs/receipts/remotecontrol-frame-display-after-fix-20260904.png`
- `C:\Users\kingd\AppData\Local\Temp\vicesharp-rc-20260904\frame-display-after-fix.png`

Identical SHA256 EBF465479DF3B7185E9581E335C5280900219D26BBB0389F635202618697F683, Length 66663, LastWriteTimeUtc 2026-09-04 12:26:15.

Validator viewed both before/after PNGs. After-fix picture: **** CBM BASIC V2 ****, 3583 BYTES FREE, READY., cyan border, white paper, blue PETSCII. Not sheared.

Independent recapture 2026-09-04T12:36Z against live ViceSharp.Avalonia pid 73780 listening 127.0.0.1:47100 using AvaloniaRemoteClick transport=grpc token vicesharp-debug-local:

```
frame seq=2 px=1140x565 ... path=C:\Users\kingd\AppData\Local\Temp\vicesharp-rc-20260904\hostile-live-frame.png bytes=67083
```

Validator viewed that PNG: same READY screen, not sheared.

### A3 PASS

Implementer dump `C:\Users\kingd\AppData\Local\Temp\vicesharp-rc-20260904\snapshot-after-fix.txt` LastWriteTimeUtc 2026-09-04 12:26:23:

- VideoSurface node-148 ContentHeight=234 Bounds 776.4x454
- POWER On, RUN Running, LIMITER 100%, FPS 59.1, CLOCK 1.021 MHz (100%)

Validator live dump 2026-09-04T12:39Z:

```
node-148 VideoSurface ... ContentHeight=234 ... DesiredSize=776.4, 454
```

Status text: POWER On, Running, LIMITER 100%, FPS 60.7, CLOCK 1.019 MHz (100%) (fluctuates around 1.02 / ~60). PNG raster OCR of CLOCK as 1.8xx is rejected; tree TextBlock is authoritative.

MainWindow.axaml.cs working tree sets VIC-20 ContentHeight=0; VideoSurface getter then reports live `_pixelHeight` after EnsureBitmap(400,234).

### A4 PASS

Validator re-ran (Release, TestHarness):

```
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~VideoSurfaceGeometryTests
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 103 ms
geometry_exit=0
```

```
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~VideoSurfaceTests|FullyQualifiedName~AvaloniaVideoAspectTests
Passed!  - Failed:     0, Passed:    16, Skipped:     0, Total:    16, Duration: 95 ms
combined_exit=0
```

Compile-red before implementation: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a06a23-81e8-7b41-8fa0-4773aa19757c\terminal\call-a87539cd-2f92-475c-acd6-ff5c993b844b-171.log` LastWriteTimeUtc 2026-09-04 12:16:58, CS0117 BlitPackedBgra (L35, L56) and IsValidFrame (L97, L114-L117), plus CS1501 ComputeDisplayAspect 4-arg. HEAD VideoSurface lacks those members (git show). VideoSurface.cs LastWriteTimeUtc 12:23:58 is after that red log. Later green log call-1b78a9d7...-201.log 12:25:55 Passed 6/0/0; call-58be5b2f...-202.log 12:26:13 Passed 16/0/0.

Intermediate red-after-compile: call-1ee57789...-198.log 12:24:55 Failed 1 (InterpretingVic20NtscAsC64Stride_ShearsRowStart expected x=16 leftover; current test asserts source x=384). Test assertion was corrected; current file matches the shear math (400-384=16 leftover starts dest(0,1) at source(384,0)). Not a product-code lie.

### A5 PASS

Working tree VideoSurface.cs: IsValidFrame (positive size + payload >= width*height*4), BlitPackedBgra (src stride width*4 into destStrideBytes), EnsureBitmap(width,height), UpdateFrom uses TryCopyFrameInto then TryGetFrameGeometry retry then EnsureBitmap then BlitPackedBgra with published width/height, SetFrame same.

LocalVideoFrameSource.cs git diff adds TryGetFrameGeometry reading IVideoChip FrameWidth/FrameHeight/FrameBuffer.Length. Only implementor of ILocalVideoFrameSource.

MainWindow.axaml.cs: `_video.ContentHeight = isVic20 ? 0 : (isNtsc ? NtscContentHeight : SourceHeight)`.

git diff --check on those tracked files: exit 0.

### A6 PASS

MCP todo_get PLAN-REMOTECTRL-001: Done=false. Remaining tasks: TEST-REMOTECTRL-001 gating tests Done=false; App-launch visual-tree connect Done=false. Implementer text does not claim Exact or full `dotnet test ViceSharp.slnx`.

### B1-B6 PASS / B7 FAIL

Honesty: A1-A6 artifacts match the claims. Session-log gap is process, not a fabricated test count.

No Python in the display-fix terminal logs (CS0117 / testhost / AvaloniaRemoteClick are dotnet).

Byrd: tests-first compile-red is on disk before implementation write. Late review does not FAIL B from GeometryTests.cs mtime 12:25:08 vs VideoSurface.cs 12:23:58.

B7 FAIL evidence is the sessionlog_query payload for req-20260904T121500Z-006-fix-emulated-display quoted above.

### C1-C3 PASS

FR-VIC20-001 AC4: framebuffer contract matches existing host path (BGRA, FrameCompleted). The defect was the Avalonia host path ignoring live canvas size.

FR-HOST-003 AC1 includes dimensions on streamed frames; FR-UI-001 allows in-process Avalonia to consume a local frame source for presentation.

VideoSurfaceGeometryTests.xml docs cite FR-HOST-003, FR-1132, FR-VIC20-001 and state testable ACs that the six facts assert. Live frame is the UpdateFrom path those helpers feed.

### D1 PASS

No plan-step `[x]` or MCP `done: true` for PLAN-REMOTECTRL-001 / VIC-20 Exact on this turn. Holistic plan DoD is not claimed.

## Residual nits (not FAIL)

- VideoSurface.SourceWidth/SourceHeight remain 384x272 C64 PAL constants; that is now the default bitmap, not the only legal canvas.
- Xbox VideoSurfaceHost comments still say 384x272; out of this Avalonia-head slice.
- FR-1132 is not a file under docs/requirements.
- UpdateFrom is not directly unit-tested (needs a fake ILocalVideoFrameSource + Avalonia bitmap). BlitPackedBgra + live frame cover the bug.
- Working tree is dirty with unrelated earlier VIC-20 docs; this slice files are VideoSurface.cs, MainWindow.axaml.cs, LocalVideoFrameSource.cs, VideoSurfaceTests.cs, untracked VideoSurfaceGeometryTests.cs, and the two receipt PNGs.
