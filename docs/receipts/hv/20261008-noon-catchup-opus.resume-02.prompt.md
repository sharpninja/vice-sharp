# Noon catch-up: same logical local Opus hostile review

Resume Claude CLI session `cd19ecc2-b410-4b7c-8a8a-dca9bbb6b008` as the
independent reviewer, exact model `claude-opus-5-5`, effort `max`. Run locally
on PAYTON-LEGION2 in `F:\GitHub\vice-sharp`. No substitute model, reviewer
subagent, cloud task, artificial turn limit or artificial runtime cap.

The operator explicitly instructed: "Claude HV will fail until noon. Continue
without HV until then, at which time have Claude HV everything it has missied."
This catch-up covers EVERYTHING deferred since accepted Phase 0, not merely
the original Phase 1 tests. The temporary timing waiver is not acceptance.
The parent will not advance another implementation gate while this review is
pending. Judge narrow implemented slices and the complete outstanding context
separately; do not pretend the unfinished whole goal is complete.

## Mandatory first action and controlling corrections

Execute the full CURRENT add-profile skill at
`C:\Users\kingd\.codex\skills\add-profile\SKILL.md` before review. It now
requires fresh, unfiltered Global MCP inventory, every full body, decoding and
hash verification of profile snapshots against the manifest, and complete
decoded reads. Do NOT follow the obsolete local-profile loading instructions
in the preserved original prompt or hostile-validator skill. Do not expose
secrets. The current manifest has 22 snapshot parts but discover current counts
rather than assuming them. Record actual verified counts.

Read `C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md` and retain
all A/B/C/D attack surfaces, with current user corrections overriding old
Grok-only, red-first and UTC wording. You ARE the reviewer requested by Codex;
the Global preference for Claude launching Codex HV does not authorize a
nested reviewer or substituting your own work. Latest direct instruction here
requires this local Opus review.

BDPv4 requires correct mocks and deliberately incorrect mocks exercising the
same assertions within a GREEN harness BEFORE production code, then the SAME
fixtures/assertions against real code. Full cumulative applicable suites must
be green with zero failures/skips/aborts. All real-emulator gates remain.
Every FR including legacy retains appropriate AC. VICE native remains the
immutable target; native VSF is the actual primary snapshot format.

Read the marker safely by replacing its parsed API-key value EVERYWHERE before
rendering. Global MEMORY-LAB-002 embeds credential identifiers in prose; redact
them as well, including after 'auth key' and 'Key ... was revoked'. Read all
other instructions in full. Do not print or propagate passwords, tokens or
keys. Accurate reviewer identity is ClaudeCode, not the legacy signature/model
text embedded in older memories.

Use measured sortable local timestamps with offset from Get-Date. Prior
reviewer's manually supplied future timestamps remain unreliable and require
the correction already specified in resume-01. Provide numeric completion
percentage and estimated remaining time at meaningful intervals in your
durable response JSONL. Mark estimates honestly; use null when truly unknown.
Do not backfill all progress only at the end.

## Prior review and scope recovery

Read the complete original and resume-01 briefs, request/response streams and
exit receipts, preserving them unchanged:
`docs/receipts/hv/20261008T073800-0500-phase1-oracle-contracts-opus*`.
The last run exited 1 with a provider session-limit error, not a verdict.
The original 59 evidence pins are retained in this request. A separate
pin-revalidation receipt discloses changes caused by authorized continued
work. Verify old and new pins before relying on evidence, and again before
verdict. Do not silently pretend old hashes describe changed files.

The full original goal remains 100 percent completion, with HV agree, of:
`C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`.
Also read `docs/plans/PLAN-VICE-IMMUTABLE-001.md`,
`docs/plans/PLAN-VSFLOCKSTEP-001.md`, current HANDOFF, workspace instructions
and current effective-layer requirements through supported MCP methods.
No narrowed replacement goal. UWP/Xbox are out of scope.

Phase 0 accepted only preparation/planning: retained Grok prefix
`docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`, reported
AGREE, 26 PASS, zero FAIL/UNKNOWN, 99 accuracy/98 completeness. Verify the
actual verdict and full primary proof independently. Nothing later inherits
that acceptance. Phase 1 and Phase 2 remain pending HV; no done-state changed.

## Entire deferred backlog to attack

1. Phase 1 standalone `tests/ViceSharp.OracleContract.Tests`: 82 cases,
   scenario/AC mappings, substantive positive and single-fault negative
   assertions, source/destination safety and native receipt contracts,
   incomplete-observation rejection and half-cycle comparator mock behavior.
   Inspect retained build and test failures, not only the final green count.
   `docs/receipts/native-vice/20261008T070900-0500-phase1-mocks/` contains
   chronology, full TRX/logs and native before/after inventories.
2. Approved build-procedure clarification: operator explicitly approved
   following VICE Team requirements including in-source generation in a
   SEPARATE pinned materialization. Original source bytes and native/vice stay
   immutable. Do not reject generated output merely for being in that separate
   tree; audit producer commands, source pin, original/generated inventories,
   tool prerequisite installation and failed/successful stock builds. Stock
   xvic and x64sc report VICE 3.10. Dirty native/vice is not the clean oracle.
3. External `src/ViceSharp.ViceCompatibility` implementation and all tests in
   `tests/ViceSharp.VsfCompatibility.Tests`: MAINCPU 1.2/1.3/1.4 normalizer,
   exact C64SC VSF 2.0 framing/module validation, missing zero defaults only,
   preserving unknown bytes/order and input immutability, malformed/error
   rejection, exact API2 binary-monitor codec and read framing/correlation.
   Attack mock discrimination, phase order, same-assertion real bindings and
   retained failed provisional tag/mock/assembly-lock attempts. Latest suite
   is 199 tests (77 normalizer, 61 binary mocks, 61 real codec bindings), not
   proof of production integration or all native snapshot behavior.
4. Native runtime snapshots and 35 retained binary frames: stock native dump,
   step, load ACKs; missing file protocol error rejected; normalized legacy
   1.2 accepted by stock x64sc. Native-to-native full VSF byte differences are
   RETAINED, including VIA CABSTATE, DRIVE8 head offset, DRIVECPU0 stop_clk,
   SID bytes. Only claims with exact source/runtime proof are supported.
   Legacy1.3 native runtime, reset-boundary stalled load, broad native VSF
   interoperability and whole-file identity are not silently passed.
5. External `tools/immutable-vice/clock-observer.c`: six independent mock
   fixtures passed before wrapper code, same six bound to compiled C after.
   Exactly once real callback forwarding, actual post-callback native clock,
   native side effects, nonmonotonic clocks, zero/max and mutation controls.
   Independent C fixture is not VICE. Separate link uses 99 original inputs.
   Actual 100000-limit probes yield 99998 records, matching repeat hashes,
   including initial 7,8,7,8. All cycle-limit exits are 1, not success exits.
   Inspect correction of stderr-only initial exit-message search.
6. External `clock-gate.c/.h` and NativeClockProbe tests/helper refactor:
   19 additional mock fixtures, positive and wrong responses, then identical
   real C cases. Preproduction31/31 plus199/199 plus82/82; postproduction
   50/50 plus199/199 plus82/82, cumulative331 with zero failed/skipped/aborted.
   Exact test commands/TRX/diag and compiler receipts are retained. Seven
   wrapped-native probes cover individual `c` permissions and quit/EOF/invalid
   commands. Two17-record prefixes match ungated prefix;39holds had no next
   record, minimum249.6824ms. This does not prove every device freezes, long
   delay equivalence or all I/O failures in actual native runtime. Nonzero
   diagnostic gate exit uses _Exit, not graceful native save/shutdown.
7. ALL external CDB diagnostics, successful and unsuccessful: startup clock
   and entry watchpoints failed; manual interruption observed running state,
   not a watchpoint hit; independent C fixture entry watchpoint and recursive
   trace attempts failed. Later post-initialization attachment succeeded.
   No claimed root cause for earlier failures. Explicit single-step actual
   original load observes its host operands and EAX result, without PeekBus
   or emulated-PC-derived data. Latest unchanged stock stream has128clock
   hits,39matchedfetchpairs and20normalstoreargumentobservations. Dummy-store
   hits0, regular/dummy reads not captured, IRQ/NMI/stolen cycles not captured,
   arbitrary post-startup attachment. Do not interpret packed host loads as
   fabricated emulated byte/cycle events. No production decoder exists yet.
   All CDB successful exits0 terminate the native target via q, native exit
   -1073740972; parent forcedTerminationfalse is NOT normal native shutdown.
8. Documentation and audit integrity: current HANDOFF, both plans, source/test
   READMEs, all Phase2 receipt indexes, effective requirements and primary
   readbacks, source/link/native preservation and saved failures. Parent
   final summaries must not turn narrow evidence into phase/whole-goal
   acceptance. Parent writes since09:xx have primary success/readback; older
   pending/failsafe items remain queued, not blindly replayed or erased.
9. Preserve and evaluate disclosed process errors: prior marker-key exposure,
   separately exposed credential identifiers from an insufficient prose-key
   filter, inaccurate inherited freshSession flag, future timestamp labels,
   some overlarge tool reads, and hashtable Select-Object null console
   projections corrected without altering valid serialized receipts. No key
   rotation, complete plugin synchronization, or repaired structured
   designDecisions/filesModified fields has been claimed. Parent logging uses
   full actions and decision dialog; verify actual server shape.

All Phase2 raw evidence lives in
`docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`, including
`binary-runtime`, `clock-runtime`, `gate-runtime`, `debugger-runtime`, and
`bus-runtime`. Read their indexes then inspect raw source/test/runtime/failure
artifacts. The request pins every retained Phase1/Phase2 receipt plus source,
tests, plans, requirements and original review context, not only favorable
results. Scope gaps found by the reviewer must be included, not ignored.

## Permitted review actions and preservation

Read-only inspection and reviewer-owned receipt writes. No product edits,
commit, push, requirement satisfaction, TODO/plan/goal completion, native
cleanup or native/source mutation. Do NOT run solution, TestHarness, Nuke or
normal bridge/native builds because they mutate native/vice. If useful, rerun
only the three standalone native-safe suites, unfiltered, with unique retained
reviewer output directories and complete native before/after inventories.
Read-only hashes, git status/diff and source inspection are permitted. No
environment inventory unrelated to this review, tools installation or repair.

Use your separate PowerShell.MCP console, not the parent's. Apply exactly the
isolated Claude cache/plugin/workspace/failsafe environment from this request
before supported plugin calls. Console isolation is not model delegation.
Installed Claude plugin is1.119.0 while marker names1.118.0. Preserve/disclose
this known mismatch without altering plugins/settings. No shared queue drain,
historical cancellation, raw REST write bypass or direct storage edits.

The operator answered "Yes, use failsafe for triage too". This supersedes the
older stop-on-triage-failure rule. Genuine primary write/triage failures must
retain complete payloads via supported full-content failsafe, verified by
readback/hash, accurately labeled QUEUED, not primary persisted. Do not spend
the run retrying unavailable infrastructure instead of reviewing. MCP primary
may now work: make supported genuine attempts; do not assume either outcome.

## Full output and acceptance

New artifact prefix, never overwrite previous review files:
`F:\GitHub\vice-sharp\docs\receipts\hv\20261008-noon-catchup-opus.resume-02`

Parent captures exact request and full streaming response JSONL. Write full
verdict markdown and structured JSON with every A/B/C/D claim, all findings,
file/line/evidence and concrete remediation, numeric AccuracyPercent and
CompletenessPercent, OverallVerdict, PASS/FAIL/UNKNOWN totals, reviewer model,
CLI/session/turn identity, progress/ETA and receipt paths. Separate decisions
for Phase1 mock adequacy, each implemented narrow slice, complete Phase2
feasibility and whole-goal completion. Whole goal is not claimed complete.

Open/recover a supported review turn and save the ENTIRE verdict prose and
structured result via ordered actions/dialog if necessary, complete it, then
prove exact content readback. Authorized full-content failsafe is the fallback
only after genuine primary failure. Report primary versus queued precisely.
No silent truncated or digest-only verdict persistence. Scores must reflect
actual evidence, not a target to game. AGREE requires BOTH scores >=98 and
zero FAIL or UNKNOWN. Missing full receipt/persistence means incomplete HV.
DISAGREE is a valid independent result. Do not alter tests, comparator,
requirements or claimed scope to force AGREE.
