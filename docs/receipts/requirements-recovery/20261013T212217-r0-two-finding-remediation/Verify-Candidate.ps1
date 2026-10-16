[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$deltaPath=Join-Path $PSScriptRoot 'proposed-field-delta.json'
$delta=Get-Content -LiteralPath $deltaPath -Raw | ConvertFrom-Json -AsHashtable
$old=Get-Content -LiteralPath $delta.derivedFrom.path -Raw | ConvertFrom-Json -AsHashtable
$baseline=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'live-baseline.json') -Raw | ConvertFrom-Json -AsHashtable
$checks=[System.Collections.Generic.List[object]]::new()
function Confirm-Check {
    param([string]$Name,[bool]$Passed,[object]$Evidence)
    $checks.Add([ordered]@{name=$Name;passed=$Passed;evidence=$Evidence})
    if (-not $Passed) { throw "Candidate verification failed: $Name" }
}
function ConvertTo-CanonicalNode {
    param([AllowNull()][object]$Value)
    if($null -eq $Value){return $null}
    if($Value -is [System.Collections.IDictionary]){
        $node=[ordered]@{}
        foreach($key in @($Value.Keys | Sort-Object -CaseSensitive)){ $node[$key]=ConvertTo-CanonicalNode $Value[$key] }
        return ,$node
    }
    if($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string]){return ,@($Value | ForEach-Object {ConvertTo-CanonicalNode $_})}
    return $Value
}
function Test-NodeEqual {
    param([object]$Left,[object]$Right)
    (ConvertTo-Json -InputObject (ConvertTo-CanonicalNode $Left) -Compress -Depth 100) -ceq (ConvertTo-Json -InputObject (ConvertTo-CanonicalNode $Right) -Compress -Depth 100)
}
Confirm-Check 'Exactly16 unique records' ($delta.records.Count -eq 16 -and @($delta.records.id | Sort-Object -Unique).Count -eq 16) $delta.records.id
foreach($record in $old.records){
    $current=@($delta.records | Where-Object id -CEQ $record.id)[0]
    Confirm-Check ('Original payload preserved: '+$record.id) (Test-NodeEqual $record $current) 'Whole record before/after and change metadata compared structurally, object order ignored; arrays and strings exact.'
}
$beforeCount=0; $afterCount=0; $retained=0; $rewritten=0; $unchanged=0; $added=0
foreach($record in $delta.records){
    $beforeCount+=@($record.before.acceptanceCriteria).Count
    $afterCount+=@($record.after.acceptanceCriteria).Count
    Confirm-Check ('No satisfied criteria: '+$record.id) (@($record.after.acceptanceCriteria | Where-Object isSatisfied -NE $false).Count -eq 0) $record.after.status
    Confirm-Check ('Unique AC IDs: '+$record.id) (@($record.after.acceptanceCriteria.id | Sort-Object -Unique).Count -eq @($record.after.acceptanceCriteria).Count) $record.after.acceptanceCriteria.id
    foreach($prior in $record.before.acceptanceCriteria){
        $now=@($record.after.acceptanceCriteria | Where-Object id -CEQ $prior.id)
        Confirm-Check ('Retain old AC: '+$record.id+'/'+$prior.id) ($now.Count -eq 1) $prior.id
        $retained++
        if($now[0].text -ceq $prior.text){$unchanged++}else{$rewritten++}
    }
    $beforeIds=@($record.before.acceptanceCriteria | ForEach-Object id)
    $added+=@($record.after.acceptanceCriteria | Where-Object id -CNotIn $beforeIds).Count
}
Confirm-Check 'AC denominator' ($beforeCount -eq 66 -and $afterCount -eq 104 -and $retained -eq 66 -and $rewritten -eq 56 -and $unchanged -eq 10 -and $added -eq 38) ([ordered]@{before=$beforeCount;after=$afterCount;retained=$retained;rewritten=$rewritten;unchanged=$unchanged;added=$added})
foreach($id in @('TEST-CPU-001','TEST-PRF-001')){
    $record=@($delta.records | Where-Object id -CEQ $id)[0]
    Confirm-Check ('Only AC changed: '+$id) (Test-NodeEqual $record.changedFields @('acceptanceCriteria')) $record.changedFields
    foreach($key in @($record.before.Keys | Where-Object {$_ -ne 'acceptanceCriteria'})){
        Confirm-Check ('Stable TEST field: '+$id+'/'+$key) (Test-NodeEqual $record.before[$key] $record.after[$key]) $record.after[$key]
    }
}
foreach($binding in $delta.testCriterionTraceability){
    $fr=@($delta.records | Where-Object id -CEQ $binding.frId)[0]
    $test=@($delta.records | Where-Object id -CEQ $binding.testId)[0]
    $mapping=@($delta.mappingsAfter | Where-Object frId -CEQ $binding.frId)[0]
    Confirm-Check ('FR/TEST trace: '+$binding.frId+'/'+$binding.frAcId) ($fr.after.acceptanceCriteria.id -ccontains $binding.frAcId -and $mapping.testIds -ccontains $binding.testId -and @($binding.testAcIds | Where-Object { $test.after.acceptanceCriteria.id -cnotcontains $_ }).Count -eq 0) $binding
}
Confirm-Check 'Nine mapping rows' ($delta.mappingsBefore.Count -eq 9 -and $delta.mappingsAfter.Count -eq 9) $delta.mappingsAfter.frId
foreach($prior in $delta.mappingsBefore){
    $live=@($baseline.mappingRead | Where-Object frId -CEQ $prior.frId)[0]
    foreach($kind in @('trIds','testIds')){
        Confirm-Check ('Mapping preimage: '+$prior.frId+'/'+$kind) (Test-NodeEqual @($prior[$kind] | Sort-Object -CaseSensitive) @($live[$kind] | Sort-Object -CaseSensitive)) $live[$kind]
        $after=@($delta.mappingsAfter | Where-Object frId -CEQ $prior.frId)[0]
        Confirm-Check ('Preserved mapping edges: '+$prior.frId+'/'+$kind) (@($prior[$kind] | Where-Object { $after[$kind] -cnotcontains $_ }).Count -eq 0) $after[$kind]
    }
}
Confirm-Check 'Projected preimages equal' (@($baseline.originalProjectedFieldComparisons | Where-Object equal -NE $true).Count -eq 0) $baseline.originalProjectedFieldComparisons.Count
$allGroups=@($delta.coverage | Where-Object {$_.Contains('namedCaseGroups')} | ForEach-Object namedCaseGroups | Sort-Object -Unique)
$backlog=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'R2-BACKLOG.md') -Raw
$backlogNames=@([regex]::Matches($backlog,'(?m)^\d+\. ([A-Za-z0-9_]+)\r?$') | ForEach-Object {$_.Groups[1].Value})
Confirm-Check 'All32 backlog names resolve' ($backlogNames.Count -eq 32 -and @($backlogNames | Where-Object { $allGroups -cnotcontains $_ }).Count -eq 0) $backlogNames
Confirm-Check 'Coverage denominator' ($delta.coverage.Count -eq 104 -and $allGroups.Count -eq 117) ([ordered]@{rows=$delta.coverage.Count;groups=$allGroups.Count;executedTests=0})
Confirm-Check 'Unapplied review metadata' ($delta.applicationAuthorized -eq $false -and $delta.status -eq 'r0-review-candidate-unapplied-awaiting-HV-and-exact-operator-approval' -and ($delta.approvalPrerequisites -join ' ') -notmatch 'five inherited media placeholders') $delta.status
Confirm-Check 'Parent candidate unchanged' ((Get-FileHash -LiteralPath $delta.derivedFrom.path -Algorithm SHA256).Hash -ceq $delta.derivedFrom.sha256) $delta.derivedFrom
[ordered]@{preparedAtLocal=(Get-Date -Format s);candidatePath=$deltaPath;candidateSha256=(Get-FileHash -LiteralPath $deltaPath -Algorithm SHA256).Hash;classification='Read-only document consistency checks, NOT executable mock/provider/emulator acceptance';checkCount=$checks.Count;failedCount=@($checks | Where-Object passed -EQ $false).Count;checks=$checks.ToArray()}
