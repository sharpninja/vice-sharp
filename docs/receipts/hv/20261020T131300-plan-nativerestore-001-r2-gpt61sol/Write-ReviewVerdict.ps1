[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$identity = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') | ConvertFrom-Json
$request = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') | ConvertFrom-Json
$data = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'review-findings.psd1')
$oldVerdictPath = Join-Path (Split-Path $PSScriptRoot -Parent) '20261020T122200-plan-nativerestore-001-gpt61sol/verdict.json'
$old = Get-Content -Raw -LiteralPath $oldVerdictPath | ConvertFrom-Json
$findings = @($data.findings | ForEach-Object { [pscustomobject]$_ })
$missingIds = @($old.findings.id | Where-Object { $_ -notin $findings.id })
$duplicateIds = @($findings | Group-Object id | Where-Object Count -gt 1)
if ($missingIds.Count -or $duplicateIds.Count) { throw 'Finding ID coverage failed.' }
foreach ($finding in $findings) {
    foreach ($field in @('id','status','severity','file','section','evidence','rationale','correction')) {
        if ([string]::IsNullOrWhiteSpace($finding.$field)) { throw ('Missing finding field: ' + $finding.id + '/' + $field) }
    }
    if ($finding.status -notin @('PASS','FAIL','UNKNOWN','ADD-AC')) { throw 'Invalid finding status.' }
}
$timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
$boundary = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'boundary-verification.json') | ConvertFrom-Json
$scoreBasis = 'Accuracy 92: the corrected native baseline, preservation rules, caller retirement and approved gate amendments are well supported, but consumer completeness, exhaustive hunk claims and documentation validation remain inaccurate. Completeness 84: unsafe H5 dependency, incomplete real VSF obligations, missing observable outcomes and unavailable primary audit/store evidence prevent execution readiness. These are holistic judgments, not test pass rates; neither score reaches 98.'
$verdict = [ordered]@{
    OverallVerdict='DISAGREE'
    accuracyPercent=92
    completenessPercent=84
    scoreBasis=$scoreBasis
    passCount=@($findings | Where-Object status -eq 'PASS').Count
    failCount=@($findings | Where-Object status -eq 'FAIL').Count
    unknownCount=@($findings | Where-Object status -eq 'UNKNOWN').Count
    addAcCount=@($findings | Where-Object status -eq 'ADD-AC').Count
    findings=$findings
    reviewerAgent=$identity.reviewerAgent
    reviewerModel=$identity.reviewerModel
    reviewerEffort=$identity.reviewerEffort
    reviewerSessionId=$identity.reviewerSessionId
    reviewerRequestId=$identity.reviewerRequestId
    reviewerIdentityDisposition='IDs supplied to the supported open/begin wrappers; no primary persistence or completed state is established.'
    parentSessionId=$identity.parentSessionId
    parentRequestId=$identity.parentRequestId
    requestPath=$request.requestPath
    responsePath=$request.responsePath
    timestampLocal=$timestamp
    classification='Class 1: independent project implementation plan review before execution'
    requestedCommit='4c5cb166'
    observedInitialCommit=$boundary.initialHead
    observedFinalCommit=$boundary.finalHead
    pinCount=$boundary.pinCount
    pinDrift=@($boundary.pinMismatches)
    profileRead=@{skill='/home/sharpninja/.codex/skills/add-profile/SKILL.md';count=1;readInFull=$true;files=@('/home/sharpninja/.claude/profile/PROFILE-MERGED.md')}
    markerReadByReviewer=$false
    execution=$boundary
    persistenceDisposition='QUEUED ONLY under the explicit full-content failsafe authorization after primary and supported triage failure. PrimaryReadback=false; completed-state proof unavailable.'
    requirementCoverage=@(
        'FR-VICE-001 -> TR-VICE-ORACLE-001 -> TEST-VICE-001: D0/D4 identity and dirty-state checks; D1/D2/D5 outside-tree build and unchanged-source evidence. TEST-VICE-001 AC1/AC2 remain acceptance obligations; status stays pending until step 12.'
        'TR-VSFLOCKSTEP-RESUME-001 AC1: F05/N01 prevent claiming complete real resume and v1.4 round-trip coverage. AC2: D1 owns the builder reference with clean out-of-tree and external-wrapper semantics preserved; F11 passes at plan level.'
        'TR-CYCLE-001 AC4: D5 restores a real native PHI2/AEC reference test under approved A2; it is supporting evidence. Managed bus gating and the full-field comparator remain Phase E work; F10 requires honest docs and valid verification.'
        'PLAN-VICEORACLE-001 tasks 4/5: D0 owns supported current-state readback and final status changes remain subject to step 12. U02: effective mappings and current task states are not attested by this untrusted reviewer plugin.'
    )
    hunkAudit=@(
        '20 patch hunks / 12 tracked files: c64cpusc include+CLK_INC -> H1; mainc64cpu include+stolen-cycle loop -> H2; mainc64cpu bootstrap -> H3; mainc64cpu legacy read -> H4.'
        'drive-snapshot initialization -> H5; drivecpu and drivecpu65c02 reset residue -> H6; mainviccpu include plus vic20cpu include/CLK_INC -> H7. mainviccpu bootstrap is the uncovered H3-like behavior under F04.'
        'filter8580new.h, sid.h and resid.cc observation/control APIs -> H8; two VIC sound guard/clamp hunks -> H9; sound exports and header -> H10. Injected header/config are H11 outside the tracked patch. All are retained as evidence; R7 still rejects H5/H6/H9 from the oracle.'
    )
    whatIfCoverage=@(
        'Interruption mid-revert: F07/A03 PASS at plan level; completed versus untouched paths are classified and unexpected bytes stop; A04 adds pin/root and Windows checkout-byte controls.'
        'Staged drift: F07 PASS; clean native index is required before every overwrite and at exit.'
        'Different LEGION2 delta: A01 PASS; preserve/map every hunk, accept explained context differences only with disposition, block unclassified or required-uncovered changes. F04 remains an accuracy gap in the reference classification.'
        'TDE-off DRIVE module: N01 FAIL; the required supplied fixture already has DRIVE8 TDE=0. A later Phase E rejection cannot supply earlier D5 safe resume. Missing modules also require configured-drive analysis.'
        'Consumer missed by grep: F03 FAIL; plain Fact direct/native-instance users and xvic escape the 43-file list; string-only managed tests are false positives.'
        'Build overwrites existing untracked bytes: F08 PASS; path/hash/length comparison and the planned overwrite negative control reject it.'
        'Submodule own dirty status under R1 B: P03 PASS; exact retained untracked inventory is intentional, tracked diffs are empty and the superproject reports clean.'
        'Known-red set changes D2 to D5: F06 PASS; equality by case identity rejects substitutions and improvements as well as extra failures. A05 adds complete execution/discovery proof.'
        'Resume, reset or malformed snapshot boundary: N02 ADD-AC; prove a unique successful load and constrained first-fetch behavior rather than equating every clock jump with successful complete resume.'
        'D5 failure after one or both hosts: A03 PASS; stop before Phase E with receipts and originals retained and restored source pinned; no automatic old-patch reapply.'
        'Repeat All in an existing build tree: A06 ADD-AC; successful up-to-date make output can lack the link line the current stage requires.'
        'Rules chained: R9 resolves the old green-gate phase cycle; explicit plan-review authority resolves D3 timing. H5 versus D5 resume is still a substantive dependency failure; primary audit versus AGREE remains blocked under U01.'
    )
    reviewerDeviations=@(
        'R02: the first progress timestamp was an unmeasured placeholder, immediately disclosed; subsequent timestamps were measured. Some progress intervals exceeded the working cadence.'
        'Initial skill/profile reads and analyzer bootstrap used inline PowerShell before file-based helpers were established. Supported MCP calls thereafter used the analyzed file helper. No Bash, Python or Node command ran.'
        'The supported begin wrapper reported a recovery path under .mcpServer despite the receipt-local cache override. That wrapper side effect is disclosed; no marker, hooks, settings, environment or historical queue was repaired or replayed.'
    )
}
if (-not $verdict.requestPath) { $verdict.requestPath = Join-Path $PSScriptRoot 'request.jsonl' }
$lines = [Collections.Generic.List[string]]::new()
$lines.Add("$timestamp - DISAGREE: PLAN-NATIVERESTORE-001 revision 2 before execution.")
$lines.Add('')
$lines.Add(('Accuracy {0}%; completeness {1}%. PASS {2}, FAIL {3}, UNKNOWN {4}, ADD-AC {5}. Phase D is not approved by this review.' -f $verdict.accuracyPercent,$verdict.completenessPercent,$verdict.passCount,$verdict.failCount,$verdict.unknownCount,$verdict.addAcCount))
$lines.Add('')
$lines.Add($scoreBasis)
$lines.Add('')
$lines.Add(('Reviewer: Codex, gpt-6.1-sol, max. Reviewer IDs: {0} / {1}. Parent: {2} / {3}. The reviewer IDs are not proof of a primary turn.' -f $identity.reviewerSessionId,$identity.reviewerRequestId,$identity.parentSessionId,$identity.parentRequestId))
$lines.Add('')
$lines.Add(('Request: {0}. Response: {1}. Structured verdict: verdict.json. Persistence receipt: verdict.persistence.json.' -f $verdict.requestPath,$verdict.responsePath))
$lines.Add('')
$lines.Add(('Class 1, plan review only. All 26 input hashes/lengths match. Requested commit 4c5cb166; initial observed develop HEAD {0}; final observed HEAD {1}. Additional committed material is review packaging, handoff and Windows trace receipts, with no subject-pin drift. The profile was read in full. No marker or API-key file was read. No native/full-solution build, managed test run, nested reviewer, native mutation, commit or push was performed. Historical test results remain attributed to their receipts.' -f $boundary.initialHead,$boundary.finalHead))
$lines.Add('')
foreach ($finding in $findings) {
    $anchor = if ($finding.PSObject.Properties.Name -contains 'line') { $finding.file + ':' + $finding.line } else { $finding.file + ', ' + $finding.section }
    $lines.Add(('{0} {1} [{2}]: {3}. Source: {4}.' -f $finding.id,$finding.status,$finding.severity,$finding.section,$anchor))
    $lines.Add('Evidence: ' + $finding.evidence)
    $lines.Add('Assessment: ' + $finding.rationale)
    $lines.Add('Required action / acceptance outcome: ' + $finding.correction)
    $lines.Add('')
}
foreach ($field in @('requirementCoverage','hunkAudit','whatIfCoverage','reviewerDeviations')) {
    foreach ($item in $verdict[$field]) { $lines.Add($item) }
    $lines.Add('')
}
$lines.Add('Persistence: QUEUED ONLY after genuine primary and supported triage failures. The complete prose and JSON are retained and hash-verified in this receipt through the authorized local failsafe. No primary full-content readback or completed-state history proof exists; verdict.persistence.json records the attempted wrapper completion/history and the local readback hashes. No repair, historical replay or cancellation was performed. Both scores are below 98, independently requiring DISAGREE.')
$jsonText = $verdict | ConvertTo-Json -Depth 20
$proseText = ($lines -join "`n") + "`n"
if ($PSCmdlet.ShouldProcess('verdict.json and verdict.md','Write complete hostile-review verdict')) {
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verdict.json'),$jsonText + "`n",[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verdict.md'),$proseText,[Text.UTF8Encoding]::new($false))
}
[pscustomobject]@{timestampLocal=$timestamp;OverallVerdict=$verdict.OverallVerdict;accuracyPercent=92;completenessPercent=84;passCount=$verdict.passCount;failCount=$verdict.failCount;unknownCount=$verdict.unknownCount;addAcCount=$verdict.addAcCount;oldFindingCount=$old.findings.Count;coveredOldFindingCount=@($findings | Where-Object id -in $old.findings.id).Count;totalFindings=$findings.Count} | ConvertTo-Json
