# ViceSharp handoff

Updated by Codex on PAYTON-LEGION2, 2026-10-16T09:45:45. Operational handoff and Git checkpoint requested and approved by the operator. Development remains paused after accepted R0 remediation HV. The full goal remains incomplete. No requirement application or implementation is authorized by the handoff, review result or commit.

## Current checkpoint and authority

Latest order: "write detailed handoff (clean up any existing) and then commit and sync origin." The operator answered "Proceed with that scope" to committing all dirty parent-repository files and pushing `main` to `origin`, while preserving `native/vice` unchanged and excluding credentials, dumps and protected raw review streams. That approval is for this checkpoint, not a new review, implementation, requirement apply or completion claim. No further HV is requested for this operational action. Remain paused after sync.

This root file is the single canonical handoff. The exact previous version is retained as [HANDOFF.before.md](docs/receipts/handoff/20261016T092300-handoff-sync/HANDOFF.before.md), SHA256 `EA2737B0084B411D9E927227E183596A5779345D809E46B2C968F53A327A9579`. Historical receipt copies are evidence, not competing current instructions. The [checkpoint receipt](docs/receipts/handoff/20261016T092300-handoff-sync/README.md) records checks and limitations; commit/push results are recorded in the current MCP turn and final response after the Git actions, not predicted here.

Publication screening found historical workspace credentials in 37 files, including the previously tracked `docs/grok/2026-08-11-vic20-exact-session/session/updates.jsonl.gz`. Five additional raw transcript files have unresolved credential-shaped fields and are conservatively kept local. Protected raw HV requests, responses, stderr and CLIXML are also kept local, together with three unrelated private phone screenshots. Exact paths, original hashes, reasons and prior tracking are in [exclusions.json](docs/receipts/handoff/20261016T092300-handoff-sync/exclusions.json). There are 465 inventoried exclusions, of which 113 were tracked. Only index tracking is removed; original bytes are retained and ignored. Existing Git history is not rewritten, credentials are not rotated by this task, and older published exposure is not claimed resolved. Verified redacted copies and complete verdict files remain publishable. A fresh clone will not include protected raw evidence; use retained local paths or MCP full-verdict readback instead of fabricating missing receipts.

The local checkpoint is commit `c01e6d32e8927ae5e3a8de5e28a1efa32c383a5d`, containing 2,499 changed files. Sync also requires integrating remote `6530196ffa98e2fda691423f7d29d0f48b2d43df`. Five merge conflicts were resolved: HANDOFF.md, README.md, docs/USER-GUIDE.md, docs/wiki.yaml and src/ViceSharp.Core/InterruptLine.cs. The code resolution retains local `AcknowledgeRisingEdge` and all remote snapshot methods unchanged. Documentation retains Linux deployment/runtime additions and the stricter immutable-reference and zero-failure gate caveats. The YAML manifest was deserialized, merged by document ID and serialized, retaining both plan entries and RomM documentation. No native/vice content or gitlink is part of the merge. Commit/push completion is verified separately in the MCP turn.

## Parallel origin work retained during sync

The exact incoming handoff is preserved as [HANDOFF.origin-main-6530196.md](docs/receipts/handoff/20261016T092300-handoff-sync/HANDOFF.origin-main-6530196.md), Git blob `07657a6cf61d3c2d3622fc1dbc2f00848cb25ae9`. It contains the other local workspace's October 15 Linux ROM/audio/user-local deployment work and October 16 PR7 regression work. The remote handoff's claims are retained as attributed history, not re-run or accepted by this chat. In particular, it reports 487 focused PR7 regression passes, five preexisting CPU/NMI failures, an earlier audio-throughput failure and unconfirmed restored-game continuity. `DIAG-SNAPSHOT-001`, `DIAG-AUDIO-001` and `DIAG-DRIVERO-001` remain its stated follow-ups. Its receipt paths are on that other workspace and are not assumed present here.

The remote snapshot-clock fixes describe versioned runtime snapshots. Integrating them does not waive the day-one canonical VICE VSF requirement or prove end-to-end snapshot parity. Its historical RED/GREEN wording does not supersede this operator's mocks-first BDPv4 directive. Its separate PR7 merge/develop-branch instruction does not authorize changing this chat from the expressly approved main-to-origin checkpoint. No PR action, deployment, app restart or additional machine work is performed here. The merge is not a new HV or full implementation gate. Before future implementation, refresh evidence pins and reconcile relevant incoming changes against the accepted frozen R0 packet; do not claim all 324 pins still match the merged tree.

## Resume here

Operator: "Resume. Remediate both HV failures, rerun HV with Opus 5.5 max. Iterate to HV agree then pause." That bounded request is fulfilled. Latest [Opus 5.5 max verdict](docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.md): **AGREE, accuracy 99, completeness 98; 49 PASS, 0 FAIL, 0 UNKNOWN, 1 N/A**. Both original blocking findings and the subsequent anchor-sweep finding are resolved for the [sole current 16-record R0 candidate](docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/README.md). Seven nonblocking information/optional findings remain preserved in the verdict, not silently fixed. Do not launch another reviewer, apply requirements or implement the application coordinator without a new operator instruction.

The reviewer completed its own MCP turn `ClaudeCode-20261014T042517Z-plugin-session` / `req-20261014T042520Z-prompt-6807`. [Full persistence receipt](docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.persistence.json): authoritative Markdown V2 (6 chunks) and JSON (3 chunks) read back exactly. Parent independently confirmed this and all 324 pinned inputs at 2026-10-13T23:49:18. [Terminal receipt](docs/receipts/hv/20261013T232143-r0-anchor-opus/terminal-result.json): process exit 0, 1,134 valid JSONL events, zero malformed lines, empty stderr; streams flushed and closed at 2026-10-13T23:50:26. The pre-completion handoff is copied and hash-verified as [HANDOFF.reviewed.md](docs/receipts/hv/20261013T232143-r0-anchor-opus/HANDOFF.reviewed.md), SHA256 `AFB26B6BDDDDA97A9C951102A668C0D8A7AC14F84731150AD20045884D70FD86`. On October 16 the candidate and verdict hashes still matched and the reviewer turn was independently queried as completed with 23 dialog items. This does not revalidate all 324 current inputs: the live marker has regenerated and this handoff is being updated. The exact delta remains UNAPPROVED and UNAPPLIED; all mock, implementation and real-emulator gates remain mandatory.

Preparation update, 2026-10-13T23:21:05: round3 preserves the16record payloads from round2 (including unchanged original14) and all9mapping rows. Only coverage anchors and receipt metadata change. It has104unsatisfied AC/117named groups,6changed+3unchanged mapping rows,13added/0removed references.178document checks pass, plus10independent expected-rejection controls, including missing CIA and VIA anchors. These are not emulator or coordinator mock gates. The exact anchor set is FR-RUNTIME-TAPE-002/ac-02; TR-TAP-EDGE-001/ac-02; TEST-TAPE-SENSE-001/ac-02; FR-TAP-003/ac-01 and ac-02; TEST-TAP-001/ac-01 and ac-03; FR-TAP-005/ac-01, ac-03, ac-04, ac-05 and ac-06. FR-TAP-005/ac-02 loader-specific evidence remains required at R2. Candidate SHA256 `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`. All32 AC-text-only groups remain mandatory R2 work. [Round2 handoff copy](docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/HANDOFF.round2.md), SHA256 `59B479B66BF781B2B67AB2C691C283C67B5AB23C7A5DBA4E783C5F6F477BE3FA`, is retained.

The objective remains 100% completion of `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md` with HV agreement. Not tape-only, documentation-only or mock-only completion. Original plan SHA256: `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`.

Read the current `AGENTS-README-FIRST.yaml` and bootstrap the supported Codex plugin. After compaction execute the complete `C:\Users\kingd\.codex\skills\add-profile\SKILL.md` and every profile source it currently requires; as inspected2026-10-13 it reads `C:\Users\kingd\.claude\profile\PROFILE-MERGED.md`. Consult current MCP workspace guidance. A summary is not a substitute. Do not voluntarily compact early; automatic platform compaction is outside that guarantee.

This file replaces contradictory current-state sections. The exact prior handoff is preserved in [HANDOFF.before.md](docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/HANDOFF.before.md), SHA256 `B7A13C695B04D2A2560AEEA3C174189594D8F0F776402662E688C86352E909E8`. It retains every historical finding and receipt. Treat it as history, not instructions to repeat applies, restart completed reviews or drain queues. A byte-identical [complete goal-plan reference](docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/goal-plan.reference.md) is beside it. The original external plan remains unchanged.

## Operating contract

Operator trust directive, preserved verbatim: "If you cannot or will not follow my instructions, then I cannot trust you.  If I cannot trust you, I will not use you.  Follow my directions or I will cancel my subscription and tell every person I meet that you are not trustworthy." Follow explicit scope, sequence, environment and approval boundaries. If an instruction cannot be followed, state the exact limitation before any conflicting action. Do not substitute a different result or claim guaranteed permanent context.

- VICE is the immutable behavioral target, including quirks. On each mismatch answer how managed behavior diverged and how it returns to VICE. Never modify the reference to make managed code pass.
- Do not edit, patch, restore, clean, delete or generate into `native/vice`. Preserve its existing dirty state. Upstream-required in-source generation was approved in a separate pinned materialization outside the submodule, with source-byte provenance. That is not permission to use the dirty patch as an oracle.
- Native VICE `.vsf` is the day-one canonical snapshot format, not secondary export. Managed VSF resume already exists. Preserve and productionize it, do not propose it as new functionality or replace it with proprietary snapshots.
- Every FR, including legacy FRs, needs appropriate AC. UWP/Xbox implementation is out of scope; its historical requirements are not retired or deleted.
- BDPv4: requirement-derived assertions and independent discriminating mocks first, all green before production implementation. Negative controls assert expected rejection within a green harness. Then bind the same cases/assertions to production. Red production runs are not a pre-implementation gate.
- Every executable exit requires the full applicable current-plus-prior suite with zero failed, skipped and aborted tests and all required real runtime evidence. Focused filters, name-counting scripts, source inspection and mock success cannot replace that gate.
- Work locally in this chat on PAYTON-LEGION2, `F:\GitHub\vice-sharp`. All shell work uses PowerShell.MCP. No Python on Windows, raw shell fallback, cloud execution, remote chat or web-review substitution.
- The operator explicitly directed Opus5.5max for these remediation rounds, superseding older alternation wording. Grok is unavailable. Use locally launched `claude-opus-5-5`, effort `max`, per invocation. No model substitution, nested reviewer or artificial turn/runtime limit. Require full profile reload, isolated reviewer PowerShell/MCP state, live JSONL progress with numeric completion/ETA, complete request/response and full verdict persistence.
- Acceptance requires AGREE, accuracy and completeness both at least 98, zero FAIL/UNKNOWN, and full primary verdict readback. No goal/TODO/requirement/satisfaction or phase done-state advance on incomplete or rejected review.
- Attempt supported primary MCP saves first. Genuine failed saves and failed triage may use authorized full-content failsafe. Label queued versus persisted. Keep `MCP_FAILSAFE_DRAIN_DISABLED=1`; do not replay historical records, cancel old turns or discard quarantine evidence.
- New requirement deltas require explicit exact-delta approval after HV. Approval of a smaller tape proposal does not authorize the expanded16-record candidate. Its idempotency departure and writer-window prerequisites also require the explicit authority described in its README before R1.
- This checkpoint's explicit approval covers all dirty parent-repository files subject to the stated publication exclusions, not `native/vice`. Outside this checkpoint, preserve the original plan's separate commit authorization boundary. GitHub `origin` only. No force-push, destructive checkout or broad cleanup. Copy, verify and retain originals.
- F: relocation is complete. Normal development on F: is authorized; no further migration or partition work.
- Reports use measured sortable local timestamps `yyyy-MM-ddTHH:mm:ss`, no invented times, em dashes or tables. Existing MCP identifiers retain the protocol-required UTC naming; new receipt names use sortable local time. Every completion claim needs command output, an on-disk check or MCP readback.

## Live state at October 16 checkpoint

Measured through local PowerShell.MCP and supported Codex MCP wrapper before editing and staging. Refresh before later work. The prior October 11 state and its failed-save evidence remain in the linked historical handoff and receipts, not current-service claims.

- Branch `main`, upstream `origin/main`; pre-checkpoint HEAD `54131ffc868f55a0ec944e99aa9fa67af3613180`; origin `https://github.com/sharpninja/vice-sharp.git`. Commit and remote equality must be proved after the actions, not inferred from this baseline.
- Fetch at `2026-10-16T09:36:10` found divergence: one local commit (`54131ff`) and two remote commits, `ed7c2e6` (Linux ROM launches/audio) and `6530196` (PR7 runtime/desktop regressions). Remote `main` is `6530196ffa98e2fda691423f7d29d0f48b2d43df`. Remote changes overlap source, tests and HANDOFF.md. Preserve both histories; no force push, reset or rebase. The final checkpoint result must disclose whether a clean integration/push succeeded or conflicts blocked it.
- Initial parent status: 2,791 entries with `--untracked-files=all`, comprising 152 modified and 2,639 untracked paths. Source, tests, historical receipts, requirements documentation, tooling and CI files are preexisting dirty work being preserved, not all authored or accepted in this turn.
- Native HEAD and index gitlink: `cc67418cba58e71d8e70fd353e9de47199ad2075`. Native inventory: 12 tracked modifications and 2,527 untracked paths. Preserve the exact inventory and bytes. No native cleanup or gitlink update is included. Patched-native evidence remains diagnostic.
- Marker SHA256 `703828CAB7BFD9B738804CADAD8B64565B6965440B6E4E685924988EBF683324`. Marker reports server `1.4.40+bcae36fdce7ce31e792db9e06a17bff51019bdf9`, PID 5828; marker written `2026-10-15T17:58:52.1876324+00:00`. The quoted UTC field is source evidence, not a local response timestamp. Current plugin status verified trust and availability. Never print credentials.
- Open TODO query at `2026-10-16T09:21:06`: 39 open. `PLAN-GROKCOMPLETION-001`, `PLAN-REQAUDIT-001`, `PLAN-VICEORACLE-001`, `PLAN-VSFLOCKSTEP-001`, `PLAN-C64SET-001`, `PLAN-GRAPHRAG-001` and `PLAN-RASPBIAN-001` remain `done: false`. No TODO, requirement or AC state changes in this checkpoint.
- Historical effective requirement counts at October 11 were layer-1 FR 235, TR 139, TEST 142 and mappings 233. They were not re-queried for this operational checkpoint and are not fresh semantic/coverage proof.
- Parent session `Codex-20261007T135626Z-plugin-session`; current request `req-20261016T141918Z-handoff-commit-sync`. BeginTurn and subsequent dialog writes persisted through the supported plugin. Use this request for checkpoint receipts, not historical reviewer turns.
- Plugin status retains 26 pending, 26 failsafe and two quarantine items. They are not reconciled by this checkpoint. Keep drain disabled. No queue replay, historical cancellation or blind cache cleanup.
- No new product build or tests run for this operational checkpoint. The normal TestHarness build invokes `native/Vice.Native.proj` and its native build scripts; running that path would breach the immutable-native boundary. No undocumented skip flag is invented. This commit is a preservation checkpoint, not a verified release or green implementation gate.
- Operational check at `2026-10-16T09:40:24`: native inventory unchanged; all 465 excluded originals retain their hashes and none remains tracked; handoff links resolve. Default staged diff check has 13 preexisting whitespace diagnostics in four pinned requirement documents, preserved and reported in the checkpoint receipt, not claimed passing.

## Remediation review chronology

These are terminal rounds, not pending retries. Do not resume them merely because older frozen README files describe the next review. The latest round above governs only R0 candidate acceptance.

1. [Original consolidated R0](docs/receipts/hv/20261011T123239-0500-r0-consolidated-opus/resume-20261011T130239-0500/verdict.md): DISAGREE 96/93, 39 PASS, two FAIL, zero UNKNOWN, one N/A. R0-01/C7 required actual TEST AC for five CPU/profile FR criteria, not mappings alone. R0-02/D5 rejected circular readiness text requiring later executable fixtures before R0 text approval. The reviewer hit quota after prose; parent recovery of the full verdict was not reviewer-side completion.
2. [First remediation round](docs/receipts/hv/20261013T213639-r0-remediation-opus/verdict.parent-recovered.md): DISAGREE 97/97, 39 PASS, two FAIL, four UNKNOWN, one N/A. RM-01 corrected the mapping ledger from seven changed/two unchanged to six changed/three unchanged. RM-02 identified Claude plan-mode permissions blocking required PowerShell.MCP. Unknowns covered current hashes, pins, complete primary readback and live MCP verification. The operator said to let this attempt finish; it exited naturally, not by interruption. Parent persistence did not turn it into AGREE.
3. [Round two](docs/receipts/hv/20261013T222855-r0-round2-opus/verdict.md): DISAGREE 98/98, 47 PASS, one FAIL, zero UNKNOWN, one N/A. RN-01 found four FR-TAP-005 coverage rows missing CIA/VIA source anchors or overclaiming their coverage. Full primary verdict readback and process exit were retained.
4. [Round three](docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.md): AGREE 99/98, 49 PASS, zero FAIL/UNKNOWN, one N/A. Corrected anchor coverage was checked across 12 rows; all 24 reviewer anchor-removal probes were detected. Candidate checks comprise 178 document assertions and ten expected-rejection controls. Those are documentation checks, not emulator or coordinator mock acceptance.

The seven retained nonblocking findings are RP-01 (reviewer correction: prior Opus override already documented), RP-02 (an overinclusive VIA source range), RP-03 (optional broader scope-sentence lint), RP-04 (historical TODO/red-green wording), RP-05 (optional recovery API naming), RP-06 (disclosed hook/cache/model/title metadata issues), and RP-07 (triage grouping does not prove resolution). Read the complete verdict for wording and evidence. No optional finding was silently changed after acceptance.

Authoritative latest verdict hashes rechecked October 16: Markdown `B94E8FA19985F13FED9476CC686D2FDE8F39EBA20DF439EAC24E7ACAA780A1D9`; JSON `36FD48D2B7D0F7441CFC929CBE3CDB06E60B8E151534D4879FA8EB7C6414F4E8`. Reviewer CLI session `bab22fd4-f213-4011-9616-074b14def01b`; runtime model `claude-opus-5-5`; max effort is proved by launch arguments, not an absent runtime field. No reviewer remains pending for this bounded request.

## Historical midnight review: terminal, superseded candidate

[Review receipt](docs/receipts/hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/README.md), [complete verdict](docs/receipts/hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/verdict.md), [structured verdict](docs/receipts/hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/verdict.json).

Single retry resumed CLI session `09cb215d-0e89-4272-aa11-a66514b50ea0`, launched `2026-10-11T00:04:40.3850986-05:00`, PID 33052. Terminal receipt `2026-10-11T00:54:29.1068954-05:00`: exit 0, `claude-opus-5-5`, requested effort `max`. Successful process execution is not approval.

DISAGREE 93/84; 35 PASS, 6 FAIL, 0 UNKNOWN, 1 N/A. Failed assertions:

1. A8: two D64 mapping targets did not exist.
2. C3: tape AC omitted behavior and hidden state.
3. C4: named mocks did not discriminate running-motor start and before-deadline cancellation.
4. C5: contradictory CPU/tape/performance and dependent media requirements.
5. C9: incomplete target checks, stable-field handling and value-bearing application guards.
6. D4: stale handoff and TODO tracking.

All 18 new findings and 17 prior dispositions remain in that historical verdict. The later R0 review chain assesses the changed candidate; it does not erase historical process disclosures or prove subsequent implementation gates. This handoff edit itself closes no finding by self-assessment.

Fresh primary readback at 2026-10-11T11:21:17-05:00 verified completed reviewer turn `ClaudeCode-20261011T050449Z-plugin-session` / `req-20261011T050452Z-prompt-ba39`: eight prose chunks reproduce 42,236 LF-normalized characters; eleven JSON chunks reproduce the file except one terminal LF. Parent `req-20261011T050131Z-midnight-opus-retry` holds the exact 50,824-character final response in nine chunks. Stream: 2,707 events, zero parse errors, SHA256 `BA6DFC9643C571306D09070DF0F80297A30B4B319B40B398B1DF10EE828D14C2`; empty stderr. Original captures were closed before terminal parsing.

Original pre/post input checks: 146/146 unchanged. At 11:21, 145 remained unchanged; only the regenerated marker differed. This is not a review of later drafts. Midnight automation is PAUSED. No quota loop or duplicate retry. Preserve N-16/N-17 disclosures: historical duplicate/unfinished hook turns, estimated interim reviewer timestamps and direct .NET receipt writes. Do not claim blanket workflow compliance.

## Current R0 candidate and supplements

Candidate: [sixteen-record round3 anchor remediation](docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/README.md). `proposed-field-delta.json` SHA256 `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`. UNAPPROVED and UNAPPLIED. It replaces round1 and the prior14-record candidate, whose hash `320887C5F2F09154F1EC7970B041A638632741A7750E838FE05F77883BB1DA83` is historical. Apply one eventual reviewed version, never overlapping drafts successively.

Sixteen existing records: `FR-RUNTIME-TAPE-002`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001`, `TEST-TAPE-RAMP-001`, `TEST-TAPE-SENSE-001`, `FR-TAP-001`, `FR-TAP-002`, `FR-TAP-003`, `FR-TAP-004`, `TEST-TAP-001`, `FR-BACKFILL-MEDIA-001`, `FR-CPU-004`, `FR-PRF-003`, `FR-TAP-005`, `TEST-CPU-001`, `TEST-PRF-001`. Retain all66 original AC IDs;104 proposed AC,56 rewritten texts,10 unchanged,38 added. Eight unchanged CPU/SX-64 criteria and the broad existing TEST conditions remain mandatory. Nine full mapping rows contain six proposed changes and three unchanged rows (FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003); 13 references added, none removed.

Preparation evidence, not accepted executable gates:

- [Initial remediation](docs/receipts/requirements-recovery/20261011T065515-0500-tape-hv-remediation/README.md) and [state-composite predecessor](docs/receipts/requirements-recovery/20261011T072531-0500-tape-state-composite/README.md): retained history and source/finding reasoning.
- [Media fixtures](docs/receipts/requirements-recovery/20261011T083152-0500-media-fixture-specification/README.md): 20 planned groups, 16 in-memory recipes, 13 source pins. Not successful native loader runs. D64/drive dependencies require explicit approved scope.
- [Motor/alarm matrix](docs/receipts/requirements-recovery/20261011T085402-0500-tape-motor-matrix/README.md): 64 vectors, 15 groups, 366 declared projection cases, not executed.
- [Recording/wiring](docs/receipts/requirements-recovery/20261011T092059-0500-tape-recording-wiring-contract/README.md): remaining 26 named runtime groups, 117 source-derived examples, 14 source pins. The combined 41 names do not establish complete tape acceptance.
- [Test disposition](docs/receipts/requirements-recovery/20261011T095710-0500-tape-test-disposition/README.md): 63 methods in 13 files. The [adjacent-body audit](docs/receipts/requirements-recovery/20261011T115707-0500-adjacent-tape-test-disposition/README.md) adds 47 methods in ten files and nine lexical classifications. These are retained assertion/disposition inventories, not accepted full-suite execution. Do not delete/weaken/skip real tests without explicit disposition.
- [Requirements API guard audit](docs/receipts/requirements-recovery/20261011T102600-0500-requirements-api-guard/README.md): typed-field/mapping semantics, 18 specified groups, 69 unexecuted negative controls. Not a proven coordinator or runtime fault matrix.
- [Tape observation contract](docs/receipts/requirements-recovery/20261011T110100-0500-tape-observation-contract/README.md): 14 upstream blob-matched sources, 14 local inputs, three tool/binary pins, ten capability and ten conformance groups, 44 unexecuted controls. SHA256 `2D1692BCBD8E8DD922CE40C483A7DC41CF470E80565C1E357021BFB594D449CD`. Eighteen document/source checks are not mock/native tests.

Critical source findings to carry into the packet:

- The external observer is clock-only, not complete tape/bus/CPU-pipeline observation.
- `alarm_set` is inline, without an external relocation in the inspected datasette object. Observing `alarm_new` returned state is only a candidate. Final queue samples cannot prove every transient schedule/unset or noninterference.
- Native VSF save has sound, drive and optional tape-stream side effects. It is not a neutral getter. Separate pristine execution from capture-prelude runs without weakening canonical VSF support.
- VSF omits some process-static tape/audio/RTC state; the audited RTC serial train is not serialized/rebuilt on read. Specify fresh-process and same-process native histories, including quirks.
- A bounded search found no standalone TAP/T64 or five named loader references in its stated scope, not universal absence across archives/external/generated assets.
- Typed records expose fields omitted by projections, including linked use cases. Read timestamps may be synthesized. Compare stable fields with presence/type awareness; AC lists are ordered and mappings ordinal duplicate-free sets.
- Mapping helper source can catch a failed prior read and then replacement-upsert, risking lost links. Source-only triage `5deae93eb0764736ae97184817d14eb6`, group `320f2143bb2556d0`, was accepted in McpServer. Not a fault-injected reproduction or repair.
- A database batch transaction is not cross-API atomicity or protection for pre-lock read/merge. R1 needs verified conditional writes or an explicitly coordinated exclusive-writer window. Do not stop other agents implicitly. Lost acknowledgements require complete actual-state readback, not blind retry/rollback.

## Next execution sequence

1. R0 candidate review has reached AGREE 99/98 with zero FAIL/UNKNOWN and full primary readback, as linked above. No additional review is pending. Successful loader fixtures, detailed runtime matrices, observation/fault reachability and noninterference proof belong to mandatory R2/R3 gates, not a claim that execution is already complete before R0 text approval.
2. Preserve unresolved historical process/evidence findings and the exact reviewed packet. All 324 pinned inputs matched at final verification. Documents parsing or listing test names is not a mock or implementation gate.
3. Remain paused. The accepted review does not complete the original plan or authorize an apply.
4. On a future operator resume, present the exact expanded delta and explicit idempotency/writer-window decisions for separate approval. No requirement/mapping/status mutation before that approval. Smaller-proposal approval does not cover new AC/records.
5. After R0 approval, write guard/coordinator tests, prove discriminating green mocks, then implement and verify real safeguards. Execute only approved R1 under preimage/writer protection; read back every field, mapping and unchanged item. Compensating writes require prescribed approval.
6. Follow R2/R3 and every original native-safe mock, real-emulator, x64sc/xvic, VSF, UI and cumulative gate. No preparation shortcut waives them.

## Whole-goal obligations

### Original Phase 0/0b: trust and baseline

Historical receipts under `docs/receipts/requirements-recovery/acceptance-20260929/` are not current counts/service proof. Reconcile intervening approved changes at final exit. Preserve applied recovery key `slice5-apply-clean-18028995ae17`, hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`. Do not post another body under that key or recreate the four replaced legacy FR IDs.

Separate immutable-oracle Phase 0 was accepted on 2026-10-07 by local Grok 4.7 xhigh: AGREE 99/98, 26 PASS, zero FAIL/UNKNOWN, 59 pins unchanged. Prefix `docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`; primary `GrokCode-20261007T222539Z-phase0-remediation-hv` / `req-20261007T222539Z-phase0-remediation-hv`, full 22,855-character proof. Only this task was accepted, not all Phase 0 tests or the whole plan.

### Original Phase 1: recovered requirement acceptance

The nine-defect ledger later included items 10/11. Closed items 2, 4, 5, 8, 9 mean only their specific ledger claims; items 1, 3, 6, 7, 10, 11 remain open absent new acceptance. The 99/99 ledger review did not accept the recovered set. Preserve provenance gaps honestly: 645 empty old hashes and 110 empty source commits in the historical accepted receipt, not invented data.

All current/legacy FRs still need appropriate AC, semantic classification and per-AC executable VICE proof. Mapping count is not coverage. ROM filenames/reset constants and an empty media list are not named workload parity. Preserve the full immutable VICE/build/ROM/media/workload manifest and requirement-audit obligations, not only fourteen tape records.

### Original Phase 2: dense session-log proof

Historical begin 322 ms/complete 312 ms proves only its measured session/server. Not today's service, all plugin rows or provider matrix. The historical 30-second wrapper failures are not under-five-second success or an observed HTTP 503; the successful October 16 checkpoint writes do not replace the dense-session acceptance matrix.

Follow the complete original dense-child/readback and classified-failure contract. If a repair is actually required, use a clean worktree based on verified deployed split-query code, never the dirty McpServer checkout. Mocks first, then real/full suites and all plugin rows (Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, OpenCode), SQLite/SQL Server/PostgreSQL, `Test`, `PluginSessionLogIntegration`, HV, Nuke `UpdateService`, fresh marker and repeated smoke. No manual deploy or unrelated environment repair during HV.

### Immutable prerequisites and original Phase 3 xvic parity

[Immutable boundary plan](docs/plans/PLAN-VICE-IMMUTABLE-001.md) remains prerequisite. The boundary was always required. `FR-VICE-001`, `TR-VICE-ORACLE-001`, `TEST-VICE-001`, and all seven `TR-CYCLE-001` AC remain mandatory. Existing dirty shim and `native/patches/vice-shim-runtime.patch` cannot establish acceptance.

The historical handoff and live `PLAN-C64SET-001` record Phase 3 start authorization and narrow 3a/3b work. Do not automatically ask again for initial authorization or describe these as never started. Reconcile exact approval/gate provenance before further implementation; it does not authorize the expanded requirements or bypass immutable prerequisites. Later R0 statements requiring initial Phase 3/4 approval conflict with this history and cannot erase it.

Historical 3a: seven prerequisite tests plus one smoke, HV 100/100. Historical 3b: one manifest-input and one mismatch test, HV 99/99. Narrow old evidence does not establish clean immutable parity or the later strengthened unfiltered cumulative gate.

Mock work exists. Historical oracle/VSF/clock-stdio runs reported 87/199/69 tests; the old 355 aggregate is mock evidence in its recorded scope, not a fresh run or proof that all current mock and real obligations passed. Shared-fixture, negative-control and stdio remediation receipts are preserved in the former handoff.

External stock builds, binary-monitor save/load/step ACKs, debugger fast-fetch, `vic_cycle` and clock-permission probes prove bounded feasibility only. Optimized/inlined paths, repeated clocks, missing dummy hits and debugger termination cannot be promoted into complete half-cycle observation. Retain failed debugger attempts, empty gate stdout, reset-boundary hang and snapshot-module asymmetries.

Remaining tasks: full native-safe mock acceptance; external capability/provenance; safe migration of preexisting native changes without changing VICE; native-safe TestHarness builds; deferred comparator XMLDoc correction; actual `REAL-C64-PHI2-AEC-001`; all real parity gates. Missing observation fails closed, never supplies inferred native values.

Use the existing single `LockstepValidator` for every half-cycle: A/X/Y/S/P/PC, opcode/micro-op, bus address/data/direction, dummy/real access, IRQ/NMI pipeline and stolen-cycle/BA. Repair foundations first; then one VICE-macro per-cycle CPU engine across 256 opcodes, decimal ADC/SBC, indirect JMP wrap and JAM; then machine glue, VIC-I, VIA/IEC/input/storage/tape, audio/side effects/reset/snapshot/determinism/pacing. No new phase booleans or sample-specific CPU exceptions. Maintain real C64 `First10000`/`First100000` pins on CPU/clock edits. Missing filters/oracle prerequisites fail, not skip. Wolf64 campaigns remain excluded except the stated regression pin.

### Native VSF is mandatory throughout completion

[Canonical VSF plan](docs/plans/PLAN-VSFLOCKSTEP-001.md) and `PLAN-VSFLOCKSTEP-001` remain open. Existing `RunFromSnapshot`, `StageManagedFromNative`, chip hooks and native read/write interfaces are not missing functionality. Gaps include full in-flight/peripheral state, xvic staging, managed-to-native save and default host/protocol/desktop/slot integration. Register transfer plus pipeline reset is not full resume.

Native VICE remains the version-specific reader/writer. Retain external validated MAINCPU 1.2/1.3 compatibility and byte-preserving 1.4, not an internal patch or replacement codec. File/quick/ten-slot paths use canonical VSF; proprietary readers are explicit import-only. Preserve closed selectors, exact framing, cancellation/lifecycle/transaction/rollback, ROM binding, structural corruption/duplicate/truncation and both-machine round trips. No invented checksum guarantee or arbitrary compatibility caps.

All eight MCP tasks remain open: plan gate; prerequisites/fixtures; production C64 staging; full C64 pipeline/media and 100000-cycle x64sc; VIC-20 and 100000-cycle xvic; managed capture through native writer; canonical UI/protocol plus win-x64 MSI installation; round-trip/corruption/cumulative gates. Packaging uses Nuke and verified post-publish native closure, no ROM redistribution. Tape HV is not VSF plan approval.

### Original Phase 4: system-specific settings

VIC-20-only controls hide on C64 and show on VIC-20. Retain notification cases for selection setter, host-refresh assignment even when later GET fails, and restore; preserve draft preset/cart across family switches and existing bindings. Visibility flags do not prove actual Tab/focus or RemoteControl behavior.

Historical focused run: eight passed, zero failed/skipped; Grok 99/98 in `docs/receipts/hv/20261003T122555-0500-handoff-phase4-green-local-grok-powershell-resume.verdict.md`. Not current unfiltered acceptance. Full native-safe TestHarness and OracleContract plus earlier applicable suites and HV remain mandatory. Do not invoke native-mutating legacy build. `PLAN-C64SET-001` remains open for 14-profile catalog, PAL/NTSC clock, warp, RemoteControl and other approved work.

### Original Phase 5 and final audit

This checkpoint is not Phase 5 completion. Final exit requires reconciled baseline/approved deltas, accepted whole requirement set, dense-session proof, every immutable xvic repair pair, canonical VSF, full settings gates, clean/pinned native provenance where required, cumulative zero-failure/skip/abort suites and complete accepted HV with primary readback. No missing oracle, named blocker or preparation ledger substitutes for completion. Preserve the original approval sequence and separate commit boundary.

## Other unresolved history

The prior handoff retains Oct 8 interrupted-review/remediation evidence. Carry forward unresolved credential-exposure disposition and public-web/local-only deviation questions, four historical reviewer-queue disposition questions, unavailable pre-refactor source/compiler evidence, forward-only logging corrections, snapshot asymmetry/reset hang and incomplete catch-up HV until their actual later disposition is verified. The noon waiver expired; it is not ongoing. Interrupted draft DISAGREE 96/94 is not a terminal verdict. The 826-input packet is retained evidence, not current approval.

Full acceptance failures across CPU port/tape, SID, configuration, CIA pins, cartridge, input, media, monitor and replay remain until reconciled and proved under BDPv4. Historical 98-test run: 52 failed/46 passed; later focused nine-pass correction did not establish a green full exit. Do not weaken old assertions simply to pass: first reconcile to approved VICE-derived criteria and preserve test disposition.

`PLAN-GRAPHRAG-001` and `PLAN-RASPBIAN-001` stay open outside this plan's exit. Other original exclusions stay excluded: unrelated drive/audio/UI/RemoteControl visual-launch/RAMLink/EQ/IEC-debug work, broad cleanup, manual deploy and unapproved commit/push. No duplicate TODOs or hidden dependency scope expansion.

## Checkpoint evidence

[October 16 checkpoint receipts](docs/receipts/handoff/20261016T092300-handoff-sync/README.md) retain the prior canonical handoff, publication exclusions, value-free scan tooling and validation evidence. [October 11 reconciliation receipts](docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/README.md) retain earlier exact copies, state snapshots, hashes, native inventory comparison, link checks and persistence disposition. This operational edit runs no mock/emulator tests or new HV. It reports the already accepted October 13 R0 verdict without claiming original-plan completion. The documentation skill guided separating live state, terminal review history, approval prerequisites and whole-goal obligations.
