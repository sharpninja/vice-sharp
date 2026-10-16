# Pure document validation, not an application coordinator or an emulator gate.
function Get-CandidateIssue {
    [CmdletBinding()]
    param([Parameter(Mandatory)][System.Collections.IDictionary]$Candidate)
    $issues=[System.Collections.Generic.List[string]]::new()
    $changed=0; $unchanged=0; $added=0; $removed=0
    foreach($before in $Candidate.mappingsBefore){
        $after=@($Candidate.mappingsAfter | Where-Object frId -CEQ $before.frId)
        if($after.Count -ne 1){$issues.Add('mapping-identity'); continue}
        $rowAdded=0; $rowRemoved=0
        foreach($kind in @('trIds','testIds')){
            $rowAdded+=@($after[0][$kind] | Where-Object {$before[$kind] -cnotcontains $_}).Count
            $rowRemoved+=@($before[$kind] | Where-Object {$after[0][$kind] -cnotcontains $_}).Count
        }
        $added+=$rowAdded; $removed+=$rowRemoved
        if($rowAdded -or $rowRemoved){$changed++}else{$unchanged++}
    }
    $actual=[ordered]@{rows=$Candidate.mappingsBefore.Count;changed=$changed;unchanged=$unchanged;additions=$added;removals=$removed}
    foreach($field in $actual.Keys){
        if($Candidate.mappingChangeSummary[$field] -isnot [int] -and $Candidate.mappingChangeSummary[$field] -isnot [long]){$issues.Add('mapping-count-type/'+$field)}
        if($actual[$field] -ne $Candidate.mappingChangeSummary[$field]){$issues.Add('mapping-count/'+$field)}
    }
    foreach($row in $Candidate.coverage){
        foreach($field in @('namedCaseGroups','sourceAnchors')){
            if($row.Contains($field) -and $row[$field] -isnot [array]){$issues.Add('coverage-array/'+$field)}
        }
    }
    foreach($binding in $Candidate.testCriterionTraceability){
        $test=@($Candidate.records | Where-Object id -CEQ $binding.testId)
        foreach($acId in $binding.testAcIds){
            $criteria=@($test | ForEach-Object {$_.after.acceptanceCriteria} | Where-Object id -CEQ $acId)
            $prefix='For '+$binding.frId+'/'+$binding.frAcId
            if($criteria.Count -ne 1 -or -not $criteria[0].text.Contains($prefix)){$issues.Add('trace-text/'+$binding.testId+'/'+$acId)}
        }
    }
    if($Candidate.baseline.freshServerReadback -ne $true){$issues.Add('stale-baseline')}
    return ,$issues.ToArray()
}

function Test-CandidateNegativeControl {
    [CmdletBinding()]
    param()
    # Independently authored minimal fixture; not derived from the production proposal.
    $fixture=[ordered]@{
        mappingsBefore=@(@{frId='FR-FIXTURE-001';trIds=@();testIds=@()})
        mappingsAfter=@(@{frId='FR-FIXTURE-001';trIds=@();testIds=@('TEST-FIXTURE-001')})
        mappingChangeSummary=@{rows=1;changed=1;unchanged=0;additions=1;removals=0}
        coverage=@(@{namedCaseGroups=@('Fixture_Case');sourceAnchors=@('fixture-source:1')})
        records=@(@{id='TEST-FIXTURE-001';after=@{acceptanceCriteria=@(@{id='ac-01';text='For FR-FIXTURE-001/ac-01 assert independent expected values.'})}})
        testCriterionTraceability=@(@{frId='FR-FIXTURE-001';frAcId='ac-01';testId='TEST-FIXTURE-001';testAcIds=@('ac-01')})
        baseline=@{freshServerReadback=$true}
    }
    $fixtureIssues=Get-CandidateIssue $fixture
    if($fixtureIssues.Count){throw 'Correct independent fixture rejected'}
    $controls=@(
        @{name='WrongChangedCount';expected='mapping-count/changed';mutate={param($f) $f.mappingChangeSummary.changed=7}},
        @{name='StringCount';expected='mapping-count-type/rows';mutate={param($f) $f.mappingChangeSummary.rows='1'}},
        @{name='ScalarCaseGroups';expected='coverage-array/namedCaseGroups';mutate={param($f) $f.coverage[0].namedCaseGroups='Fixture_Case'}},
        @{name='ScalarSourceAnchors';expected='coverage-array/sourceAnchors';mutate={param($f) $f.coverage[0].sourceAnchors='fixture-source:1'}},
        @{name='MissingTraceText';expected='trace-text/TEST-FIXTURE-001/ac-01';mutate={param($f) $f.records[0].after.acceptanceCriteria[0].text='Unrelated test text'}},
        @{name='WrongTraceCriterion';expected='trace-text/TEST-FIXTURE-001/ac-02';mutate={param($f) $f.testCriterionTraceability[0].testAcIds=@('ac-02')}},
        @{name='StaleBaseline';expected='stale-baseline';mutate={param($f) $f.baseline.freshServerReadback=$false}},
        @{name='MissingMappingRow';expected='mapping-identity';mutate={param($f) $f.mappingsAfter=@()}}
    )
    $results=@(foreach($control in $controls){
        $fault=$fixture | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
        & $control.mutate $fault
        $actual=Get-CandidateIssue $fault
        if($actual -cnotcontains $control.expected){throw ('Negative control not detected: '+$control.name)}
        [ordered]@{name=$control.name;expectedRejection=$control.expected;actualRejections=$actual;passed=$true}
    })
    [ordered]@{correctFixtureAccepted=$true;negativeControls=$results;passed=$results.Count;failed=0;scope='Document-lint assertions exercised with independent data; not an emulator or coordinator mock gate.'}
}
