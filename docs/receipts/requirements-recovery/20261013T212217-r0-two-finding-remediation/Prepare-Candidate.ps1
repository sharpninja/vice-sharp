[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PluginPath,
    [Parameter(Mandatory)][string]$CacheRoot,
    [string]$WorkspacePath = 'F:\GitHub\vice-sharp'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Read-only preparation. This script never submits a requirement or mapping mutation.
# Native objects are returned to the calling console for serialization and editor writes.
function Get-CanonicalValue {
    param([AllowNull()][object]$Value)
    if ($null -eq $Value) { return $null }
    if ($Value -is [System.Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($key in @($Value.Keys | Sort-Object -CaseSensitive)) {
            $result[$key] = Get-CanonicalValue $Value[$key]
        }
        return ,$result
    }
    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string]) {
        return ,@($Value | ForEach-Object { Get-CanonicalValue $_ })
    }
    return $Value
}
function Test-ExactValue {
    param([AllowNull()][object]$Left, [AllowNull()][object]$Right)
    return (ConvertTo-Json -InputObject (Get-CanonicalValue $Left) -Depth 100 -Compress) -ceq
        (ConvertTo-Json -InputObject (Get-CanonicalValue $Right) -Depth 100 -Compress)
}
function Read-Requirement {
    param([string]$Kind, [string]$Id)
    $method = @{fr='client.Requirements.GetFrAsync';tr='client.Requirements.GetTrAsync';test='client.Requirements.GetTestAsync'}[$Kind]
    $raw = & $PluginPath -Command Invoke -Method $method -ParamsObject @{id=$Id} -WorkspacePath $WorkspacePath -CacheRoot $CacheRoot
    $reply = $raw | ConvertFrom-Yaml
    if ($reply.type -ne 'result' -or -not $reply.payload.result.id) { throw "Read failed: $Kind/$Id" }
    return [ordered]@{kind=$Kind;id=$Id;method=$method;readAtLocal=(Get-Date -Format s);result=$reply.payload.result}
}
function New-Criterion {
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions','',Justification='Pure in-memory object factory; no file, server or process mutation.')]
    [CmdletBinding()]
    param([int]$Number,[string]$Text)
    return [ordered]@{id=('ac-{0:d2}' -f $Number);text=$Text;isSatisfied=$false}
}

$parentPath = Join-Path $WorkspacePath 'docs\receipts\requirements-recovery\20261011T080155-0500-capacitor-guard-remediation\proposed-field-delta.json'
$old = Get-Content -LiteralPath $parentPath -Raw | ConvertFrom-Json -AsHashtable
$delta = $old | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
$coverage = Get-Content -LiteralPath (Join-Path $WorkspacePath 'docs\receipts\requirements-recovery\20261011T121006-0500-r0-review-packet\consolidated-coverage.json') -Raw | ConvertFrom-Json -AsHashtable
$typed = [System.Collections.Generic.List[object]]::new()
$comparisons = [System.Collections.Generic.List[object]]::new()
foreach ($record in $old.records) {
    $read = Read-Requirement $record.kind $record.id
    $typed.Add($read)
    foreach ($field in $record.before.Keys) {
        $equal = $read.result.Contains($field) -and (Test-ExactValue $record.before[$field] $read.result[$field])
        $comparisons.Add([ordered]@{kind=$record.kind;id=$record.id;field=$field;expected=$record.before[$field];actual=$read.result[$field];equal=$equal})
        if (-not $equal) { throw "Preimage drift: $($record.id)/$field" }
    }
}
$cpu = Read-Requirement test 'TEST-CPU-001'
$prf = Read-Requirement test 'TEST-PRF-001'
$typed.Add($cpu)
$typed.Add($prf)
if (@($cpu.result.acceptanceCriteria).Count -ne 0 -or @($prf.result.acceptanceCriteria).Count -ne 0) { throw 'New TEST preimages are no longer empty; reconcile before proposing.' }
$cpuAfter = $cpu.result | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
$prfAfter = $prf.result | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
$testPreamble = 'Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. '
$cpuCriteria = [System.Collections.Generic.List[object]]::new()
$cpuCriteria.Add((New-Criterion 1 ($testPreamble + 'For FR-CPU-004/ac-03, C64_PortMotorAndWrite_DdrTruthTable covers all DATA/DDR combinations for bits 5 and 3, changes caused by DATA and DDR, input/output transitions and repeated identical writes. Assert motor = !(DDR & DATA & 0x20), write = (~DDR | DATA) & 0x08, exact changed-effective-output calls and no call when the native effective output is unchanged. Reject swapped bits, wrong polarity, forwarding on every write, and missed DDR-only changes. Preserve unrelated bits and pin prior native output-history state.')))
$cpuCriteria.Add((New-Criterion 2 ($testPreamble + 'For FR-CPU-004/ac-05, C64_PortBit5_ReadDirectionMatrix enumerates DATA bit5 low/high crossed with DDR input/output, both transitions and repeated reads. Assert input reads clear bit5 and output reads match pinned c64pla port calculation, preserving all unrelated read bits. Reject input pull-up, output forced-zero, SX capacitor substitution on C64, stale direction and unintended read-side output changes.')))
$caseText = Get-Content -LiteralPath (Join-Path (Split-Path $parentPath) 'CAPACITOR-SOURCE-AND-CASES.md') -Raw
$caseMatches = [regex]::Matches($caseText,'(?m)^([1-8])\. `([^`]+)`: ([^\r\n]+)')
if ($caseMatches.Count -ne 8) { throw 'Expected exactly eight independent capacitor specifications.' }
foreach ($case in $caseMatches) {
    $cpuCriteria.Add((New-Criterion ([int]$case.Groups[1].Value + 2) ($testPreamble + 'For FR-CPU-004/ac-06, ' + $case.Groups[2].Value + ': ' + $case.Groups[3].Value)))
}
$cpuAfter.acceptanceCriteria = $cpuCriteria.ToArray()
$prfAfter.acceptanceCriteria = @(
    (New-Criterion 1 ($testPreamble + 'For FR-PRF-003/ac-02, Sx64_PortOutputs_AreDisconnected crosses DATA/DDR bits 3,4,5 and direction transitions for SX-64 and C64 control boards. Assert SX-64 never forwards motor, write or sense-out to a cassette device, including repeated and changed effective outputs; its CPU reads still obey DATA, DDR and independently established retained-charge state. Reject C64 forwarding on SX-64, globally disconnecting C64, or hardcoding all SX input bits high or low.')),
    (New-Criterion 2 ($testPreamble + 'For FR-PRF-003/ac-04, Sx64_InputCapacitors_ResetAndIndependentBits exercises each bit 3,4,5 separately and mixed masks with different captured 0/1 values. Assert output reads use normal port data, input reads use the corresponding retained data_set_bit, expiry clears only eligible expired charge/flags, and reset clears charge and flags without inventing deadline clearing. Reject shared state between pins, output charge overlay, resurrection after reset and dropped unrelated read bits.')),
    (New-Criterion 3 ($testPreamble + 'For FR-PRF-003/ac-04, Sx64_InputCapacitors_StrictFalloffBoundary tests each bit with an independently chosen deadline D at clocks D-1, D, D+1; only deadline < currentClock expires on $0001 read. Cover repeated reads, no-read gaps, output-mode reads and $0000-only reads. Assert DATA-read expiry side effects even for output pins but no charge overlay there; DDR reads do not expire charge. Reject <=, eager/host-clock expiry, input-only expiry and DDR-read discharge.')),
    (New-Criterion 4 ($testPreamble + 'For FR-PRF-003/ac-04, Sx64_InputCapacitors_DeadlineSelection uses clock10 with independently scripted inclusive jitter endpoints 0 and300000 to produce deadlines1500010 and1800010 for bits3/4/5. DATA writes while output, including same-value and zero writes, and DDR output-to-input transitions capture data and consume independent draws. Other DDR transitions or DATA writes while input do not. Cross-check C64_Capacitor_RngConsumptionOrder and ResetAndClockWrap assertions for SX bit order, sparse masks, uint64 wrap and prior global RNG history; reject one shared draw, exclusive endpoints, wrong1500000-cycle base, omitted repeated writes and invented native endpoint seeds.'))
)
foreach ($pair in @(@($cpu,$cpuAfter),@($prf,$prfAfter))) {
    $delta.records += [ordered]@{kind='test';id=$pair[0].id;operation='update';approvalStatus='unapplied-awaiting-exact-operator-approval';changedFields=@('acceptanceCriteria');before=$pair[0].result;after=$pair[1];provenance='R0-01 remediation: explicit TEST-level criteria for existing FR mappings. Existing condition and all metadata preserved; no executed-test or satisfaction claim.'}
}
$mappingRead = (& $PluginPath -Command Invoke -Method client.Requirements.ListMappingsAsync -ParamsObject @{} -WorkspacePath $WorkspacePath -CacheRoot $CacheRoot) | ConvertFrom-Yaml
$mappingRows = @($mappingRead.payload.result)
if ($mappingRows.Count -eq 1 -and $mappingRows[0].Contains('items')) { $mappingRows = @($mappingRows[0].items) }
foreach ($id in @('FR-CPU-004','FR-PRF-003')) {
    $row = @($mappingRows | Where-Object frId -CEQ $id)
    if ($row.Count -ne 1) { throw "Missing/duplicate mapping: $id" }
    $delta.mappingsBefore += $row[0]
    $delta.mappingsAfter += $row[0]
}
$trace = @(
    [ordered]@{frId='FR-CPU-004';frAcId='ac-03';testId='TEST-CPU-001';testAcIds=@('ac-01')},
    [ordered]@{frId='FR-CPU-004';frAcId='ac-05';testId='TEST-CPU-001';testAcIds=@('ac-02')},
    [ordered]@{frId='FR-CPU-004';frAcId='ac-06';testId='TEST-CPU-001';testAcIds=@('ac-03','ac-04','ac-05','ac-06','ac-07','ac-08','ac-09','ac-10')},
    [ordered]@{frId='FR-PRF-003';frAcId='ac-02';testId='TEST-PRF-001';testAcIds=@('ac-01')},
    [ordered]@{frId='FR-PRF-003';frAcId='ac-04';testId='TEST-PRF-001';testAcIds=@('ac-02','ac-03','ac-04')}
)
foreach ($binding in $trace) {
    $source = @($coverage.amendmentRows | Where-Object { $_.requirementId -ceq $binding.frId -and $_.acId -ceq $binding.frAcId })[0]
    for ($i=0; $i -lt $binding.testAcIds.Count; $i++) {
        $groups = if ($binding.testAcIds.Count -eq 1) { @($source.namedCaseGroups) } else { @($source.namedCaseGroups[$i]) }
        $coverage.amendmentRows += [ordered]@{requirementId=$binding.testId;acId=$binding.testAcIds[$i];kind='test';namedCaseGroups=$groups;sourceAnchors=@($source.sourceAnchors);evidenceStatus='R2 specification only; mock and real execution both pending';frAcBinding=($binding.frId+'/'+$binding.frAcId)}
    }
}
$delta.status = 'r0-review-candidate-unapplied-awaiting-HV-and-exact-operator-approval'
$delta.createdAtLocal = Get-Date -Format s
$delta.recordCount = 16
$delta.afterCriteriaCount = 104
$delta.applicationAuthorized = $false
$delta.derivedFrom = [ordered]@{path=$parentPath;sha256=(Get-FileHash -LiteralPath $parentPath -Algorithm SHA256).Hash;disposition='Historical superseded candidate; never apply in addition to this candidate.'}
$delta.approvalPrerequisites = @(
    'R0: source-accurate requirement text, explicit TEST AC traceability, consistent scope/stage disclosures and local Opus5.5 max AGREE >=98/98, zero FAIL/UNKNOWN, complete reviewer receipts and MCP readback.',
    'Then explicit operator approval names this complete16-record delta and nine mapping rows, with seven changed rows and two unchanged CPU/profile traceability rows. Earlier smaller approvals do not apply.',
    'The operator must separately accept the disclosed locked-decision2 departure: supported updateBatch/UpsertMapping calls have no server idempotency-key field. The R1 plan requires state reconciliation, never blind retry; without explicit acceptance R1 stays read-only.',
    'R1 prerequisites, not R0 text-review prerequisites: approved writer-window evidence, complete stable preimage checks, G01-G18 mocks green before coordinator implementation, then real guards/application/readback/export/traceability/HV.',
    'R2/R3 prerequisites, not claims of R0 execution: detailed fixtures and negative controls for every group, including all32 AC-text-only groups in R2-BACKLOG.md; independent green mocks before product changes, then identical real assertions, native observability/noninterference, lawful loader assets and full cumulative gates.'
)
$delta.testCriterionTraceability = $trace
$coverage.scope = 'Single current16-record R0 review candidate. TEST AC coverage is specification traceability, not executable acceptance.'
foreach ($row in $coverage.amendmentRows) {
    if ($row.requirementId -eq 'FR-BACKFILL-MEDIA-001' -and $row.acId -eq 'ac-03') { $row.evidenceStatus = 'R0 requirement/specification binding defined; complete independent fixture and negative-control implementation remains mandatory at R2 before production.' }
    if (($row.requirementId -eq 'FR-TAP-003' -and $row.acId -eq 'ac-02') -or
        ($row.requirementId -eq 'FR-TAP-005' -and $row.acId -eq 'ac-04')) {
        $row.sourceAnchors = @('src/core/ciacore.c:592-600,1594-1625','src/core/viacore.c:441-462','src/c64/c64datasette.c:36-49','src/vic20/vic20datasette.c:37-49')
    }
    if ($row.requirementId -eq 'FR-TAP-005' -and $row.acId -eq 'ac-02') {
        $row.sourceAnchorLimitation = 'Generic tape core sources do not establish named-loader success. Pin each lawful loader-specific asset/source, successful native workload and expected observations at R2 before using the real provider; currently unproved.'
    }
}
$delta.coverage = @($coverage.amendmentRows) + @($coverage.unchangedMandatoryRows)
$groups = @($coverage.amendmentRows | ForEach-Object namedCaseGroups | Sort-Object -Unique)
$delta.coverageDisclosure = [ordered]@{totalRows=$delta.coverage.Count;amendmentRows=$coverage.amendmentRows.Count;unchangedMandatoryRows=$coverage.unchangedMandatoryRows.Count;uniqueNamedGroups=$groups.Count;newTestCriteria=14;cpuProfileChangedFrCriteria=5;cpuProfileCriteriaWithExplicitTestAc=5;testExecutionCount=0;explanation='Receipt rows and named groups are specification bindings only, not executable coverage. The five CPU/profile FR criteria now map to real TEST records with proposed constraining AC. All existing broad CPU/profile conditions and unchanged criteria remain mandatory.32 AC-text-only groups require complete independent fixture design at R2. No global executable coverage claim is made.'}
$coverage.counts = [ordered]@{records=16;beforeCriteria=66;afterCriteria=104;amendmentRows=$coverage.amendmentRows.Count;unchangedMandatoryRows=$coverage.unchangedMandatoryRows.Count;uniqueProposedGroups=$groups.Count;executedTests=0}
$coverage.preparedAtLocal = $delta.createdAtLocal
$coverage.proposalPath = Join-Path $PSScriptRoot 'proposed-field-delta.json'
$coverage.proposalSha256 = 'Assigned from editor-written candidate bytes before review freeze'
$coverage.replacementPolicy = 'This ledger replaces the previous consolidated ledger as current candidate coverage. Earlier versions remain historical. No case is counted as executed.'
$coverage.acceptanceBoundary = 'R0 text/specification review only. R1 guards and application, R2 discriminating mocks, R3 real providers and all original gates remain mandatory and unexecuted here.'
$delta.invariants = @($delta.invariants | ForEach-Object { $_.Replace('Typed preimages captured for14 records','Typed preimages captured for16 records') })
$delta.scopeDisclosure = 'One16-record proposal replaces the14-record draft solely for future exact approval. Adds14 unsatisfied AC to existing TEST-CPU-001 and TEST-PRF-001; preserves their original broad conditions and metadata. Original14 after-records are unchanged. Nine full mapping rows include seven previously proposed changes plus two unchanged CPU/profile rows. No live mutation authorized.'
$baseline = [ordered]@{capturedAtLocal=(Get-Date -Format s);typedReads=$typed.ToArray();originalProjectedFieldComparisons=$comparisons.ToArray();mappingRead=@($mappingRows | Where-Object frId -In $delta.mappingsBefore.frId);limitation='Typed shapes are not every database field. R1 must additionally compare Notes, ScopeEndLayerKey and criterion evidence through the supported full requirements surface; all unclassified fields fail closed. This is not a writer lock.'}

[ordered]@{Delta=$delta;Coverage=$coverage;Baseline=$baseline;Trace=$trace}
