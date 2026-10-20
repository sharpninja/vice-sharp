@{
    findings = @(
        @{
            id='P01'; status='PASS'; severity='info'; file='input-pins.json'; section='All 26 input pins'
            evidence='All 26 SHA256 and length pairs matched before analysis; the subject hash is 9AE793C5DE8C20FD8C5CC5B567F5C6E1D07FC9AC8EEB59C379E79CDF612038D2. Six materialized source files also match their pinned Git blobs. See pin-verification.json and source-pin-verification.json.'
            rationale='The revised artifact and source being reviewed are identified independently of round 1.'
            correction='Retain the pins; disclose the separate checkout-commit drift recorded in this verdict.'
        }
        @{
            id='P02'; status='PASS'; severity='info'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=7; section='Native starting state'
            evidence='Live git records gitlink and native HEAD cc67418cba58e71d8e70fd353e9de47199ad2075, 8 modified tracked files, 222 insertions, 8 deletions, an empty native staged diff, and 155 untracked paths. state-before.json retains hashes and lengths for those 163 paths.'
            rationale='The native SHA, tracked paths and diff totals agree with live state.'
            correction='Repeat the same measurements independently on both hosts at D0.'
        }
        @{
            id='P03'; status='PASS'; severity='info'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=38; section='R1 B and R4'
            evidence='R1 B, D4 and Exit explicitly retain the classified untracked inventory and change only .gitmodules ignore=dirty to ignore=untracked. The current .gitmodules still has ignore=dirty; implementation has not occurred.'
            rationale='The authorized exit permits the submodule itself to list untracked paths while the superproject reports no submodule dirt. It does not require deletion or info/exclude.'
            correction='Keep the amended tracked-clean plus inventory-equality gate.'
        }
        @{
            id='P04'; status='PASS'; severity='info'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=3; section='Plan review, D3 and the implementation hold'
            evidence='The 12:21 operator ruling authorizes plan review. D-pre requires an HV AGREE receipt and an explicit implementation release; section 5 keeps acceptance/status changes pending until step 12.'
            rationale='Plan HV and postponed implementation HV are distinct. This review grants no execution release.'
            correction='Keep the explicit release and final done-state boundary.'
        }
        @{
            id='P05'; status='PASS'; severity='info'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=62; section='D-pre installation receipts'
            evidence='Linux install README lines 13-24 and Windows install README lines 7-25 both identify Nuke installs of 1.3.111 from 2c8d60284229b9fedc2d50f840153b9e8d8cbcbf, with binary hashes. Windows used InstallMsi, which ran PublishMsi. Both original receipts were read in this round.'
            rationale='The recorded installs satisfy the documented Nuke route. This is receipt verification, not a new inspection or GUI launch on either host.'
            correction='At entry, record the accepted installed commit/version and operator release as A02 requires.'
        }
        @{
            id='P06'; status='PASS'; severity='info'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=13; section='Bounded Phase C evidence'
            evidence='The builder confines work/build/bin output to artifacts/immutable-vice. Linux S2b/S4 receipts and the now available Windows S3/S4c README record both observer builds and 100k/1M/4M equivalence evidence. Windows 4M is newly recorded as completed at 13:26, whereas the pinned handoff still records its earlier deferral.'
            rationale='These receipts support external build, bus observation and bounded reset-workload equivalence. They do not establish a replacement DLL ABI, native VSF resume or managed parity.'
            correction='Preserve these limits and refresh the D-pre trace matrix from the actual receipts.'
        }
        @{
            id='P07'; status='PASS'; severity='info'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=63; section='Preservation and narrow restore authority'
            evidence='D0 copies tracked modifications and injected source; D4 requires a committed preservation receipt and explicit per-path checkout, prohibits reset/clean, and retains every untracked file. The Oct 4 header hashes to 5450FDA8CFEDB8A22BADA8CD04EE75FF830C48796592FF9EC0141E899D846259; its inventory has 2,527 paths.'
            rationale='Copy/hash/retain and operator-approved legacy-builder retirement remain compatible. Restoring a tracked path is authorized only inside the approved D4 execution.'
            correction='Retain originals and both host receipts; do not infer deletion or automatic patch-reapply authority.'
        }
        @{
            id='F01'; status='PASS'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=8; section='Corrected untracked count'
            evidence='The plan now states 155 untracked and 163 total porcelain entries, matching git -C native/vice status --porcelain=v1 -uall. D0 explicitly inventories hidden and ordinary paths.'
            rationale='The round 1 count defect is corrected.'
            correction='Use complete per-host -uall enumeration during execution.'
        }
        @{
            id='F02'; status='PASS'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=65; section='Windows harness caller retirement'
            evidence='The live harness csproj still defines ViceNativeProject at line 10 and invokes BuildNative at line 72. D2 now explicitly removes both, then retires Vice.Native.proj and the three legacy builders, and requires both-host build-log evidence that the retired native path did not run.'
            rationale='The previously omitted executable caller now has a concrete disposition.'
            correction='Limit the no-reference check to active executable/build references; historical receipts may retain names.'
        }
        @{
            id='F03'; status='FAIL'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=45; section='R6 consumer inventory and gate denominator'
            evidence='The stated token grep does yield 43 files, 5 with Category and 38 without, but it is neither complete nor semantic. SnapshotResumeSpikeTests.cs:47-108 uses plain Fact plus ViceNative and is outside the 43. Vic20NativeLockstepTests.cs:18-46 is also outside it and uses ViceNative.CreateInstance plus XvicOraclePrerequisites. Conversely XAudio2AudioMathTests.cs:26-29 matches only documentation saying no ViceFact, and XboxTestConventionTests.cs:159-173 contains sample strings. The proposed check covers zero ViceFact/ViceTheory methods and zero vice_x64 loads, not all direct/indirect users or vice_xvic.'
            rationale='Literal tagging of the 43 files can exclude managed tests while leaving real native consumers selected. Linux skips/failures and Windows dirty-DLL execution can therefore survive the planned inventory pass.'
            correction='D0/D2 must enumerate actual test methods and transitive fixtures/providers using ViceNative, ViceNativeXvic, bridge APIs and native instances; exclude only authorized shim-bound cases, record every Phase E/F owner, and prove selection and absence of both DLL loads with missed-plain-Fact and string-only negative controls.'
        }
        @{
            id='F04'; status='FAIL'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=24; section='H1-H11 classification accuracy'
            evidence='The Oct 4 patch has 20 hunks in 12 files (patch-hunks.json). Its mainviccpu.c hunk at patch line 230 installs host-register bootstrap, but H7 describes hosted CLK_INC as H1/H2 and does not classify that H3-like resume behavior. Pinned drive-snapshot.c:135-162 creates every DRIVE module before testing TDE; H5 incorrectly says a no-drive unit is omitted. has_tde is payload byte 0, not a module-header field. Section 2 also says only H5/H9 are behavior corrections despite H6 being one, and H9 says no Phase D-G workload compares VIC sound despite handoff Phase F item 6 and existing Vic20SoundLockstep native audio cases. The 1,432 historical dependency files are .Po, not .po.'
            rationale='The added classification correctly identifies several old omissions, but its exhaustive and reachability claims are still inaccurate.'
            correction='Map all 20 hunks explicitly, including xvic bootstrap; correct writer/reader and payload facts, the three-correction count, extension spelling and Phase F audio ownership. Keep R7 rejection; preserve existing obligations and block required-uncovered behavior.'
        }
        @{
            id='F05'; status='FAIL'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=68; section='R8 and complete VSF Slice 1 semantics'
            evidence='PLAN-VSFLOCKSTEP-001:325-350 requires both-machine prerequisites, fixture provisioning and deterministic regeneration, the deterministic-module manifest, v1.2/v1.3 native resume, v1.4 native round-trip and the cumulative NativeSnapshotCompatibilityTest target. TR-VSFLOCKSTEP-RESUME-001 AC1:77 requires those real resumes/round-trip; the adapter at X64ScSnapshotCompatibility.cs:28-30 explicitly disclaims native proof. D1 names test families and a future amendment, D2 replaces commands, and D5 concretely specifies only an x64sc autostart clock/PC test. It does not allocate the native writer/round-trip, xvic fixture generation, manifest/cumulative-target execution or the deferred S5 register comparison to concrete observer-capable steps. The new test also does not state how normalized v1.2/v1.3 bytes reach the unmodified reader.'
            rationale='Changing libraries to observer executables changes the interface, not the existing acceptance obligations. Clock/PC agreement alone cannot replace real complete resume and round-trip evidence or the promised register cross-check.'
            correction='Before execution, finish the approved Slice 1 amendment with an obligation-to-step/test/immutable-command/evidence map for both machines. Specify normalization input, native snapshot writing, real v1.2/v1.3/v1.4 evidence, fixtures, manifest, cumulative gate and the retained register obligation, or obtain an explicit scoped requirement/scheduling amendment.'
        }
        @{
            id='F06'; status='PASS'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=48; section='R9 known-red exception'
            evidence='The diagnostic README:14-35 and its focused command name 20 failures (4 CPU/NMI and 16 visible-PC). Build.cs:97-112 selects these uncategorized classes. R9, D2, D5 and Exit now require exactly that named failure set, no additional failures and no skips; they do not filter the 20 out.'
            rationale='The explicit operator-approved exception removes the Phase D/Phase E green-gate cycle. It is a baseline exception, not a passing full-suite claim.'
            correction='Keep identity-based set comparison at both runs; any improvement, replacement failure or extra failure changes the authorized set and requires disposition.'
        }
        @{
            id='F07'; status='PASS'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=67; section='Pre-overwrite and resume rules'
            evidence='D4 now checks a clean index and each live hash against the preserved or already-restored version before overwrite; unexpected state stops. It checks cached and working diffs, rehashes copies, distinguishes restored/untouched/unexpected paths, and names changed-live, staged, partial, missing and corrupt receipt Pester cases.'
            rationale='This resolves the unsafe overwrite and undefined partial-run outcomes identified in round 1 at plan level.'
            correction='Implement the same contract against the native repository on both hosts; A04 strengthens the pinned-identity and worktree-byte controls.'
        }
        @{
            id='F08'; status='PASS'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=63; section='Hash/length inventory bracket'
            evidence='D0 now hashes and records lengths for every untracked path and modified tracked file. D2, D4, D5 and Exit require path/content equality, with a mock negative control for overwriting an existing untracked path. D4 separately accounts for the authorized tracked restore.'
            rationale='An existing-path content change can no longer hide behind an unchanged porcelain path list.'
            correction='Retain both-host inventories and compare the appropriate pre-build or post-restore baseline, including additions and removals.'
        }
        @{
            id='F09'; status='PASS'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=60; section='Pester, analyzer and WhatIf ownership'
            evidence='The live builder:26 and equivalence runner:20 still lack SupportsShouldProcess and create directories at lines 52 and 41 respectively. D1 now explicitly owns the change, mock-first Pester dry-run tests proving no files/directories, analyzer zero findings and dry runs before real execution. Installed review tools report Pester 5.9.1 and PSScriptAnalyzer 1.25.0.'
            rationale='The defect is now an implementation prerequisite rather than an assumed existing capability. An approved native toolchain shell does not itself violate PowerShell-only orchestration.'
            correction='Apply the stated mock gate to all observable side effects, then bind the same cases to both host implementations.'
        }
        @{
            id='F10'; status='FAIL'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=68; section='Comparator XMLDoc enforcement and AC4 evidence scope'
            evidence='D5 restores REAL-C64-PHI2-AEC-001 and the XMLDoc item, but says the latter is enforced by XmlDocsConventionTests. That test scans attributed test methods only (XmlDocsConventionTests.cs:38-40,56-74); LockstepValidator is an ordinary class with a class summary at lines 15-18, so it is not checked. Its implementation remains IViceNative/register/CIA based; the observer-field comparator is explicitly Phase E item 1. TR-CYCLE-001:25,44 concerns the managed core; a native-only trace assertion is supporting reference evidence, not full managed AC4 satisfaction.'
            rationale='The missing exit items are restored, but the proposed documentation validator cannot enforce the claim and the documentation must not describe future behavior as implemented.'
            correction='Use verification that actually covers the comparator documentation, or remove the false enforcement claim. Describe current behavior and the future half-cycle contract accurately. Keep the real trace gate and leave managed AC4 pending until its managed evidence exists.'
        }
        @{
            id='F11'; status='PASS'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=74; section='Blast radius and reference-update ownership'
            evidence='The edit inventory now lists the actual csproj caller, Vice.Native.proj, one PowerShell builder and two shell launchers for retirement, the observer scripts and new preservation/restore scripts, and no exclude file/script. D1 owns the TR-VSFLOCKSTEP-RESUME-001 AC2 builder-reference update through the wrapper; D1/D2 own the Slice 1 amendment and validation-command replacement.'
            rationale='The contradictory R1 A inventory is gone. An AC2 builder-name substitution can preserve its clean-materialization and external-wrapper semantics; it does not resolve AC1 coverage under F05.'
            correction='Complete canonical/generated reference propagation and the repository traceability audit through the supported paths when implementing the reference update; report existing traceability debt separately.'
        }
        @{
            id='R01'; status='PASS'; severity='high'; file='response.jsonl'; section='Round 2 credential handling'
            evidence='No marker, API-key file or raw round 1 response was read, copied or printed in this round. The supported wrapper alone accessed its marker internally; wrapper output was captured and checked before display. Review inputs were the redacted verdict documents, not the credential-bearing round 1 stream.'
            rationale='The round 1 credential-read failure was not repeated. This does not retroactively erase the prior exposure or claim credential rotation.'
            correction='Preserve the no-marker-read rule and keep the old raw stream private.'
        }
        @{
            id='U01'; status='UNKNOWN'; severity='high'; file='logging-attempts.jsonl'; section='Primary reviewer persistence and completion'
            evidence='Supported Status succeeded, then bootstrap/beginTurn failed: MCP_UNTRUSTED: marker refresh failed before REPL request: MCP_UNTRUSTED: Health check failed - Permission denied. One supported workflow.triage.report attempt failed identically. A supported queryHistory also failed. openSession explicitly returned persisted:false; beginTurn returned rejected/persisted:false. Local identity and full-content fallback records are not server receipts.'
            rationale='The complete primary turn, full-content primary readback and completed-state history proof remain unavailable. The operator authorized a fallback, not an assertion that primary persistence succeeded.'
            correction='Keep this verdict queued only and acceptance blocked unless the operator explicitly waives primary persistence. A later authorized recovery must persist/read back the entire verdict and prove completed state; do not repair trust, replay historical queues or cancel old turns here.'
        }
        @{
            id='U02'; status='UNKNOWN'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=63; section='Effective requirements and PLAN-VICEORACLE-001 tasks'
            evidence='Canonical FR-VICE-001 -> TR-VICE-ORACLE-001 -> TEST-VICE-001 documents, TR-VSFLOCKSTEP-RESUME-001 AC1/AC2 and TR-CYCLE-001 AC4 were read directly. D0 now explicitly requires effective mappings and TODO task 4/5 readback. The primary trust failure prevents attesting current store layers, mappings and task statuses.'
            rationale='The plan now owns the missing readback, but a future instruction is not current store evidence.'
            correction='Before implementation depends on these records, use the supported wrappers to retain exact effective mappings and both task states. Do not infer store completion from projections.'
        }
        @{
            id='A01'; status='PASS'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=63; section='DeltaComparison_ClassifiesEveryCurrentHunk'
            evidence='D0 now preserves and maps every current hunk, accepts explained host/context differences as listed dispositions, blocks unclassified or required-uncovered behavior, and names missing-hunk/wrong-host negative controls.'
            rationale='A different LEGION2 delta no longer implicitly means failure or permission to overwrite an unclassified change. The classification accuracy defect remains F04.'
            correction='Apply this outcome to the fresh host delta, not a presumed byte-identical Oct 4 patch.'
        }
        @{
            id='A02'; status='PASS'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=62; section='EntryEvidence_BindsBothHostsAndExplicitRelease'
            evidence='D-pre/D0 require both installed commit/version pairs, the Windows trace matrix including any explicit deferral and owner, this plan AGREE receipt, and the operator instruction releasing implementation. D0 does not run until all are present.'
            rationale='Receipt arrival or plan approval alone cannot lift the explicit implementation hold.'
            correction='Use the new Windows 4M result in the entry matrix; retain the explicit release requirement.'
        }
        @{
            id='A03'; status='PASS'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=67; section='PostRestoreFailure_StopsWithPreservedEvidence'
            evidence='D4 now states that failure after the first host or both hosts stops before Phase E, retains originals and receipts, reports each host/path state and leaves restored source pinned. Automatic patch reapply and untracked deletion are forbidden; the two failure positions are named test cases.'
            rationale='Rollback evidence and permission to modify the oracle are correctly separated.'
            correction='Keep this safe stopped state; any later recovery mutation needs scoped operator authority.'
        }
        @{
            id='N01'; status='FAIL'; severity='high'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=24; section='Reachable H5 fixture and Phase D/Phase E dependency'
            evidence='Independent byte inspection of ready-c64sc-truedrive.vsf (SHA256 5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19) finds MAINCPU 1.2, clock 109163946, PC 22377, and DRIVE8 2.0 at offset 66204, inclusive length 24, payload has_tde=0/has_drives=1. Pinned drive-snapshot.c:317-319 leaves clock arrays uninitialized, reads them only at 368-419, then copies them for every non-NONE configured unit at 540-550. Even missing modules take the has_tde=0 continue path at 333-339. H5 schedules fail-closed adapter rejection only in Phase E, while D5 and AC1 require real resume of the supplied fixture before Phase E starts.'
            rationale='The conditional branch is already reachable in an approved input. Merely naming a later rejection neither makes the earlier load safe nor preserves the mandatory resume AC. Module-header scanning alone cannot establish the TDE or configured-drive preconditions.'
            correction='D0 must report this concrete fixture and stop required-uncovered execution before D4/D5. Specify a safe, approved external disposition and prerequisite order before any affected native load; cover TDE-off and missing modules plus configured unit types. If the disposition rejects a required fixture, resolve the existing resume requirement explicitly rather than silently dropping it. Keep the native hunk rejected under R7.'
        }
        @{
            id='N02'; status='ADD-AC'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=68; section='D5: SnapshotResume_SelectsUniqueSuccessfulLoadBoundary'
            evidence='Pinned autostart.c:1552-1575 opens the snapshot then reboots and waits for the deferred load trap (1266-1274); initial/random autostart delay is at 1528-1537. mainc64cpu.c:947-981 restores clock/registers; 6510dtvcore.c:408-428 imports them after the trap and handles reset. c64cpusc.c:139-146 loads PC then increments clock before the hook, and LOAD can first steal cycles through mainc64cpu.c:359-362. c64-snapshot.c:146-153 resets on read failure. VICE_OBS exposes read/write/dummy, not an opcode-fetch tag.'
            rationale='clk+1 is a sound narrow expectation after a successful, suitably constrained load; it is not a universal unique load-success locator or complete register proof. The plan does not define outcomes for no discontinuity, a colliding clock, an unrelated reset, a later-module load failure, or an idle/stolen first gate.'
            correction='Add the named D5 AC: bind the unique observed resume boundary and load outcome to the exact normalized fixture; state the valid fixture/state preconditions and first observable fetch rule. Accept the constrained positive case and reject ambiguous/absent boundaries, wrong clk/PC, unrelated reset, later-module failure and missing prerequisites using independent mocks before the same real observer cases. Do not infer successful complete resume from two matching fields.'
        }
        @{
            id='A04'; status='ADD-AC'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=67; section='D4: Restore_RejectsWrongPinnedIdentityAndResumesCheckoutBytes'
            evidence='D4 now protects hashes and staged state but describes already-restored bytes as the index version and checks gitlink/checked-out equality at the end. Execution spans two repositories and Windows/Linux working-tree representations.'
            rationale='An explicit pre-overwrite pin/root outcome and checkout-byte comparison make the existing safety contract unambiguous without changing its restoration mechanism.'
            correction='Add this D4 AC: a wrong repository, changed recorded/native/gitlink identity or wrong-host receipt stops before the first overwrite; an interrupted run recognizes the exact working-tree bytes produced by checkout on each host, including the configured line-ending conversion. Prove both-host positive resume and these negative controls.'
        }
        @{
            id='A05'; status='ADD-AC'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=65; section='D2/D5: KnownRedGate_RequiresCompleteExecutionAndStableDiscovery'
            evidence='R9 defines exact failure-set identity at D2 and D5. The plan does not explicitly bind that comparison to discovered versus executed selected cases, terminal execution state and a normal Nuke nonzero exit caused only by those failures.'
            rationale='An exact 20-name failure set alone must not mask a crashed, partially executed or changed test denominator. This adds an observable gate outcome; it does not revoke the approved known-red exception.'
            correction='Add this D2/D5 AC: retain selected case IDs and terminal TRX/results for every project; all selected cases execute, skipped/aborted/not-executed are zero, and the only nonzero result is the exact R9 set. Reject an equal-sized swapped set, missing project, discovery drift and partial/aborted run; report an intentional R6 denominator change separately.'
        }
        @{
            id='A06'; status='ADD-AC'; severity='medium'; file='docs/plans/PLAN-NATIVERESTORE-001.md'; line=68; section='D1/D5: ObserverBuild_RepeatAllRetainsUsableLinkEvidence'
            evidence='D5 requires all six builder stages on existing build trees. The builder advertises idempotent stages at line 7, but Make:220-231 extracts link lines only from the current make stdout, writes that mapping to stock-link-lines.json, then throws if no link line was emitted. A successful up-to-date make can emit none.'
            rationale='The required rerun has no stated successful outcome for an already-built tree. This is a static inference from the existing code; no prohibited build was run.'
            correction='Add a D1-owned mock-first AC and real D5 rerun: repeating All after a successful build either produces verified usable link evidence and completes, or stops with an explicit safe prerequisite before replacing required prior evidence. Cover up-to-date stdout and interrupted stage resume; retain original outputs until an authorized disposition.'
        }
        @{
            id='R02'; status='FAIL'; severity='medium'; file='response.jsonl'; section='Reviewer progress timestamp contract'
            evidence='My first commentary emitted timestampLocal 2026-10-20T00:00:00 without measuring it. I disclosed that it was a placeholder, then measured local time through PowerShell and used measured sortable timestamps in subsequent progress reports. Some progress intervals also exceeded the requested working communication cadence.'
            rationale='The first progress object violated the explicit never-estimated timestamp criterion. Later correction cannot make that earlier timestamp measured. This is my review-process defect, not an implementer plan defect.'
            correction='Retain the disclosed deviation. Future review launch/bootstrap must measure the first timestamp before emitting the first progress object and maintain live progress during drafting as well as source inspection.'
        }
    )
}
