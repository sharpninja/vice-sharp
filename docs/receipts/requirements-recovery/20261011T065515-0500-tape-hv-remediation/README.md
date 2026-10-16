# Tape HV remediation: partial exact-delta supplements

Codex, PAYTON-LEGION2, `F:\GitHub\vice-sharp`. Latest artifact/source validation measured at 2026-10-11T07:05:54-05:00.

**Unapplied. Incomplete approval packet. No accepted phase or implementation gate.**

The objective remains 100% completion of `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md` with independent HV agreement. These artifacts address four findings from the midnight tape-amendment review without reducing that objective or changing requirements in MCP.

## Delivered in this continuation

- [N-01/N-02 exact text](N01-N02-EXACT-SUPPLEMENT.md) and [machine-readable supplement](exact-supplement.json): remove two nonexistent targets from the proposed additions, retaining all live links and the separate media obligations; add exact corrections for four AC across existing `FR-CPU-004` and `FR-PRF-003`.
- [N-05/N-06 exact text](N05-N06-EXACT-SUPPLEMENT.md) and [machine-readable supplement](motor-supplement.json): six proposed AC appends and seven additive case-map updates for already-running START and early/same-clock stop cancellation. No existing cases removed.
- [Artifact evidence](artifact-validation.json): actual before-value checks, case lists, mapping existence results, file hashes and selected native pre/post hashes. These are structural/source checks, not emulator tests.

The source audit also qualifies the reviewer's N-02 wording: SX-64 CPU reads do not always return zero on input bits 3-5. Both native memory implementations overlay retained capacitor state; it survives the exact decay deadline and clears only on a later read. The new proposal preserves those edge cases instead of replacing one constant-value error with another. The original verdict is retained unchanged.

## Verified scope and limits

- Four AC before-values and body occurrences match the retrieved live effective records; status and satisfaction flags are unchanged.
- All 38 target references remaining after the proposed mapping correction resolve in that effective projection. The original packet had 40 references with two missing targets.
- Six motor AC selectors match their original proposed texts; seven coverage selectors exist. Existing groups are retained, with three unique new motor groups proposed.
- Five selected materialized native source files match pinned Git blobs and remain unchanged after this work. This is not a whole-native-tree integrity or runtime claim.
- Both JSON supplements read back equal to their native PowerShell serializations after line-ending/final-newline normalization.
- Repository `git diff --check` exited 0. The four new content files were also separately checked for trailing whitespace, with zero violations; untracked files are not covered by ordinary `git diff --check`.
- Original proposal SHA256 remains `1D8A442588ADD7883693694352954DF2077D166FB47ED4E2924AFE738A481507`; retained review SHA256 remains `E87F607E6F533823A59E7FF7EAFDB7A8A88B3FE0548D82163797383B3EB9FC0B`.

No mock suite, managed/native runtime suite or new HV ran. No production code, native source, live requirement/mapping, satisfaction flag or done-state was changed. No commit or push occurred. The prior review remains DISAGREE, accuracy 93, completeness 84, 35 PASS, 6 FAIL, 0 UNKNOWN, 1 N/A.

## MCP distinction

The effective query returned layer-1 counts of 235 FR, 139 TR, 142 TEST and 233 mappings. A later `workflow.requirements.getFr` failed with `Plugin command failed with exit code 1` before capturing a complete record. Triage separately returned `method_invocation_error` / `QBrainAi.Client.QBrainAiException`: "The storage backend is currently unreachable. Retry the operation once connectivity is restored."

The complete triage request is retained through the supported failsafe at `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261011T115419Z-goal_tape_getfr_triage_failure-eace.yaml`, SHA256 `86CFD2FF272DA233FE5BCC06435BD1F402D19690D5D05052794CA7AF24D3CA26`. This report is queued, not accepted server triage. Other session-log saves succeeded. Do not describe the entire MCP server as unavailable or equate successful logging with complete requirement metadata.

Parent session: `Codex-20261007T135626Z-plugin-session`; turn: `req-20261011T114744Z-tape-proposal-remediation`. `MCP_FAILSAFE_DRAIN_DISABLED=1` remains set. No historical queues were replayed or turns canceled. No service/environment repair was attempted.

## Resume point

1. Read these supplements and the unchanged full midnight verdict. Compose a separately retained complete machine-readable delta, starting from the original eleven records plus the two existing FR corrections. The supplements are not themselves an approved batch request.
2. Complete remaining semantic/source work, including N-07 through N-12, the case denominator and changed-obligation disclosure, test-disposition ledger, and current handoff/TODO tracking. Preserve all prior unresolved findings and process disclosures. N-12 may require another scope expansion after its audit; do not silently add it to an approval.
3. Recover complete full-record preimages through the supported interface. Implement and prove target-existence, stable-metadata and value-bearing comparison safeguards. Current effective projection success does not satisfy this prerequisite.
4. Review the complete corrected packet with local Opus 5.5 max. Require AGREE, both scores at least 98 and zero FAIL/UNKNOWN, complete receipts and verified MCP persistence. Then obtain explicit approval for that exact expanded delta before applying it.
5. Continue the original mocks-first, unchanged real-provider assertions, full cumulative suites, immutable xvic/x64sc, native VSF, VIC-20 settings and final handoff gates. None is waived by proposal preparation.
