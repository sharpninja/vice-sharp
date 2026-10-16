[CmdletBinding()]
param([string]$ProposalDirectory = $PSScriptRoot)

# Read-only artifact checks. These are not emulator tests or hostile acceptance.
$ErrorActionPreference = 'Stop'
$checks = [Collections.Generic.List[object]]::new()
function Check([string]$Name, [bool]$Passed, [object]$Detail) {
    $checks.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
}
function Equal-Value($Left, $Right) {
    $leftJson = ConvertTo-Json -InputObject $Left -Depth 50 -Compress
    $rightJson = ConvertTo-Json -InputObject $Right -Depth 50 -Compress
    [System.Text.Json.Nodes.JsonNode]::DeepEquals(
        [System.Text.Json.Nodes.JsonNode]::Parse($leftJson),
        [System.Text.Json.Nodes.JsonNode]::Parse($rightJson))
}

$jsonPath = Join-Path $ProposalDirectory 'proposed-field-delta.json'
$proposal = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json -AsHashtable
$markdown = (Get-Content -LiteralPath (Join-Path $ProposalDirectory 'FIELD-DELTA.md') -Raw).Replace("`r`n", "`n")
$baseline = Get-Content -LiteralPath $proposal.baseline.path -Raw | ConvertFrom-Json -AsHashtable
$media = Get-Content -LiteralPath $proposal.reusedParentProposal.path -Raw | ConvertFrom-Json -AsHashtable
$expectedIds = @('FR-RUNTIME-TAPE-002','TR-TAP-EDGE-001','TR-TAPE-EDGE-001',
    'TEST-TAPE-RAMP-001','TEST-TAPE-SENSE-001','FR-TAP-001','FR-TAP-002',
    'FR-TAP-003','FR-TAP-004','TEST-TAP-001','FR-BACKFILL-MEDIA-001')
Check 'Exact eleven-record scope' (Equal-Value @($proposal.records.id | Sort-Object) @($expectedIds | Sort-Object)) @($proposal.records.id)
Check 'Retained baseline hash' ((Get-FileHash -LiteralPath $proposal.baseline.path -Algorithm SHA256).Hash -ceq $proposal.baseline.sha256) $proposal.baseline.path
Check 'Retained parent proposal hash' ((Get-FileHash -LiteralPath $proposal.reusedParentProposal.path -Algorithm SHA256).Hash -ceq $proposal.reusedParentProposal.sha256) $proposal.reusedParentProposal.path

$beforeCount = 0
$afterCount = 0
$coverageKeys = @($proposal.coverage | ForEach-Object { $_.requirementId + '/' + $_.acId })
$expectedKeys = [Collections.Generic.List[string]]::new()
foreach ($delta in $proposal.records) {
    $collection = switch ($delta.kind) { fr { 'functional' } tr { 'technical' } test { 'testing' } default { throw 'Unknown kind' } }
    $before = $delta.before
    $after = $delta.after
    $bodyField = if ($delta.kind -eq 'test') { 'condition' } else { 'body' }
    $source = @($baseline[$collection] | Where-Object { $_.id -ceq $delta.id })
    Check ($delta.id + ': exact retained preimage') ($source.Count -eq 1 -and (Equal-Value $before $source[0])) $collection
    Check ($delta.id + ': unchanged identity') ($delta.id -ceq $before.id -and $before.id -ceq $after.id) $after.id
    Check ($delta.id + ': pending, unsatisfied') ($after.status -ceq 'pending' -and @($after.acceptanceCriteria | Where-Object { $_.isSatisfied -isnot [bool] -or $_.isSatisfied }).Count -eq 0) $after.status
    $actualChanged = @(@($before.Keys) + @($after.Keys) | Sort-Object -Unique | Where-Object {
        $before.Contains($_) -ne $after.Contains($_) -or -not (Equal-Value $before[$_] $after[$_])
    })
    Check ($delta.id + ': exact changedFields') (Equal-Value @($actualChanged | Sort-Object) @($delta.changedFields | Sort-Object)) $actualChanged
    $allowedFields = @('title', $bodyField, 'status', 'acceptanceCriteria')
    Check ($delta.id + ': no unapproved field changes') (@($actualChanged | Where-Object { $_ -notin $allowedFields }).Count -eq 0) $actualChanged
    $oldAc = @($before.acceptanceCriteria)
    $newAc = @($after.acceptanceCriteria)
    $beforeCount += $oldAc.Count
    $afterCount += $newAc.Count
    Check ($delta.id + ': old AC IDs preserved') (@($oldAc | Where-Object { $_.id -cnotin $newAc.id }).Count -eq 0) @($oldAc.id)
    Check ($delta.id + ': unique nonempty AC') ($newAc.Count -gt 0 -and @($newAc.id | Sort-Object -Unique).Count -eq $newAc.Count -and @($newAc | Where-Object { [string]::IsNullOrWhiteSpace($_.id) -or [string]::IsNullOrWhiteSpace($_.text) }).Count -eq 0) $newAc.Count
    foreach ($criterion in $newAc) {
        $key = $delta.id + '/' + $criterion.id
        $expectedKeys.Add($key)
        $coverage = @($proposal.coverage | Where-Object { $_.requirementId -ceq $delta.id -and $_.acId -ceq $criterion.id })
        Check ($key + ': named cases and sources') ($coverage.Count -eq 1 -and @($coverage[0].namedCaseGroups).Count -gt 0 -and @($coverage[0].sourceAnchors).Count -gt 0) $coverage.Count
    }
    # Retained requirement bodies contain their own "## Acceptance Criteria" headings.
    # Only an actual record/mapping heading ends a record, not a body heading.
    $pattern = '(?ms)^## ' + [regex]::Escape($delta.id) + '\n(?<section>.*?)(?=^## (?:FR-|TR-|TEST-|Mapping additions only)|\z)'
    $sectionMatch = [regex]::Match($markdown, $pattern)
    Check ($delta.id + ': Markdown section exists') $sectionMatch.Success $delta.id
    $section = $sectionMatch.Groups['section'].Value
    $fieldText = 'Fields changed: ' + ($delta.changedFields -join ', ')
    Check ($delta.id + ': Markdown field list') ($section.Contains($fieldText)) $fieldText
    foreach ($side in @('before','after')) {
        $partMatch = [regex]::Match($section, '(?ms)^### ' + $side + '\n(?<part>.*?)(?=^### |\z)')
        $part = $partMatch.Groups['part'].Value
        $value = $delta[$side]
        $matchesText = $partMatch.Success -and $part.Contains('Title: ' + $value.title) -and $part.Contains('Status: ' + $value.status) -and $part.Contains(([string]$value[$bodyField]).Replace("`r`n", "`n"))
        foreach ($criterion in $value.acceptanceCriteria) {
            $matchesText = $matchesText -and $part.Contains($criterion.id + ': ' + ([string]$criterion.text).Replace("`r`n", "`n"))
        }
        $matchesText = $matchesText -and ([regex]::Matches($part, '(?m)^- \[[ x]\] ').Count -eq @($value.acceptanceCriteria).Count)
        Check ($delta.id + ': Markdown exact ' + $side + ' values') $matchesText @($value.acceptanceCriteria).Count
    }
}
Check '47 old and 71 new criteria' ($beforeCount -eq 47 -and $afterCount -eq 71 -and $proposal.beforeCriteriaCount -eq $beforeCount -and $proposal.afterCriteriaCount -eq $afterCount) @($beforeCount,$afterCount)
Check 'Complete unique AC coverage' (Equal-Value @($coverageKeys | Sort-Object) @($expectedKeys | Sort-Object)) $coverageKeys.Count
$parentRow = @($proposal.records | Where-Object { $_.id -ceq 'FR-BACKFILL-MEDIA-001' })[0]
$retainedParent = @($media.records | Where-Object { $_.id -ceq 'FR-BACKFILL-MEDIA-001' })[0]
Check 'Parent row reused exactly' (Equal-Value $parentRow $retainedParent) 'Other media/video proposal rows not imported'

$additions = [Collections.Generic.List[object]]::new()
$expectedMappingIds = @('FR-BACKFILL-MEDIA-001','FR-RUNTIME-TAPE-002','FR-TAP-001','FR-TAP-002','FR-TAP-003','FR-TAP-004','FR-TAP-005')
Check 'Exact seven mapping rows' ((Equal-Value @($proposal.mappingsBefore.frId | Sort-Object) $expectedMappingIds) -and (Equal-Value @($proposal.mappingsAfter.frId | Sort-Object) $expectedMappingIds)) @($proposal.mappingsAfter.frId)
foreach ($old in $proposal.mappingsBefore) {
    $new = @($proposal.mappingsAfter | Where-Object { $_.frId -ceq $old.frId })[0]
    $source = @($baseline.mappings | Where-Object { $_.frId -ceq $old.frId })
    Check ($old.frId + ': exact mapping preimage') ($source.Count -eq 1 -and (Equal-Value $old $source[0])) $old.frId
    foreach ($field in @('trIds','testIds')) {
        Check ($old.frId + ': preserve ' + $field) (@($old[$field] | Where-Object { $_ -cnotin $new[$field] }).Count -eq 0) @($old[$field])
        Check ($old.frId + ': unique ' + $field) (@($new[$field] | Sort-Object -Unique).Count -eq @($new[$field]).Count) @($new[$field]).Count
        $extra = @($new[$field] | Where-Object { $_ -cnotin $old[$field] })
        foreach ($id in $extra) { $additions.Add([ordered]@{frId=$old.frId;field=$field;id=$id}) }
    }
    foreach ($field in @(@($old.Keys) + @($new.Keys) | Sort-Object -Unique | Where-Object { $_ -notin @('trIds','testIds') })) {
        Check ($old.frId + ': preserve mapping field ' + $field) (Equal-Value $old[$field] $new[$field]) $field
    }
}
$parseErrors = @()
foreach ($file in @('New-Proposal.ps1','Verify-Proposal.ps1')) {
    $tokens = $null
    $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $ProposalDirectory $file),[ref]$tokens,[ref]$errors)
    $parseErrors += @($errors)
    Check ($file + ': PowerShell parse') (@($errors).Count -eq 0) @($errors | ForEach-Object { $_.Message })
}
$cases = @($proposal.coverage.namedCaseGroups | Sort-Object -Unique)
[ordered]@{
    schema='vice-sharp/tape-proposal-artifact-checks/v1'
    createdAtLocal=[TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow,'America/Chicago').ToString('yyyy-MM-ddTHH:mm:sszzz')
    passed=(@($checks | Where-Object { -not $_.passed }).Count -eq 0)
    checkCount=$checks.Count
    failedCount=@($checks | Where-Object { -not $_.passed }).Count
    checks=$checks.ToArray()
    uniqueNamedCaseGroupCount=$cases.Count
    uniqueNamedCaseGroups=$cases
    mappingAdditions=$additions.ToArray()
    applicationPerformed=$false
    emulatorTestsRun=0
    hostileAcceptance=$false
    freshPrimaryReadback=$false
    limitations=@('Read-only structural checks do not establish semantic correctness or VICE parity.',
        'No complete fresh MCP records or hidden metadata available; retained baseline only.',
        'Local Opus max review and explicit exact-delta approval remain required.')
}
