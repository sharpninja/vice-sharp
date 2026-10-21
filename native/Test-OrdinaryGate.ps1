#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D2 AC GateExecution_IsCompleteAndOnlyKnownRedFails (A05, R9, N08, N11): evaluates the
retained TRX files of a Nuke Test run. Every expected project must have a TRX whose run completed; every selected
case executed (zero skipped, aborted or not executed); the failed cases equal exactly the known-red set by fully
qualified name; the native load log exists and is empty; no ShimLegacy inventory method and no real-bound class
executed; every contract class executed; and, when a retained identifier list is given, the executed identifiers
equal it (discovery drift). Writes a report and exits nonzero on any violation.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ResultsDirectory,
    [Parameter(Mandatory)][string[]] $ExpectedProjects,
    [Parameter(Mandatory)][string] $KnownRedPath,
    [Parameter(Mandatory)][string] $LoadLogPath,
    [string] $InventoryPath,
    [string[]] $ContractClasses = @(),
    [string[]] $RealClasses = @(),
    [string] $RetainedIdentifiersPath,
    [string] $ReportPath,
    [string] $WriteIdentifiersPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ExpectedProjects = @($ExpectedProjects | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$ContractClasses = @($ContractClasses | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$RealClasses = @($RealClasses | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$violations = [Collections.Generic.List[string]]::new()
$projects = [Collections.Generic.List[object]]::new()
$cases = [Collections.Generic.List[object]]::new()
$emptyRuns = @()

$trxFiles = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter *.trx -File -ErrorAction SilentlyContinue)
foreach ($trx in $trxFiles) {
    [xml] $doc = Get-Content -LiteralPath $trx.FullName -Raw
    $ns = @{ t = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }
    $methods = @{}
    foreach ($ut in @(Select-Xml -Xml $doc -Namespace $ns -XPath '//t:UnitTest')) {
        $tm = $ut.Node.TestMethod
        $methods[$ut.Node.id] = [pscustomobject]@{ className = $tm.className; method = $tm.name }
    }
    $assembly = $null
    $firstMethod = @(Select-Xml -Xml $doc -Namespace $ns -XPath '//t:TestMethod' | Select-Object -First 1)
    if ($firstMethod.Count -gt 0) { $assembly = [IO.Path]::GetFileNameWithoutExtension($firstMethod[0].Node.codeBase) }
    if (-not $assembly) {
        # A TRX without any test definition comes from a project whose every case the filter excluded (for example
        # Category!=Integration over ViceSharp.Library.IntegrationTests); it names no assembly. Counted, not a violation.
        $emptyRuns += $trx.Name
        continue
    }
    $counters = @(Select-Xml -Xml $doc -Namespace $ns -XPath '//t:Counters' | Select-Object -First 1)
    $summary = @(Select-Xml -Xml $doc -Namespace $ns -XPath '//t:ResultSummary' | Select-Object -First 1)
    $runOutcome = if ($summary.Count -gt 0) { [string]$summary[0].Node.outcome } else { 'missing' }
    $total = 0; $executed = 0; $passed = 0; $failed = 0; $notExecuted = 0
    if ($counters.Count -gt 0) {
        $c = $counters[0].Node
        $total = [int]$c.total; $executed = [int]$c.executed; $passed = [int]$c.passed; $failed = [int]$c.failed
        $notExecuted = if ($c.HasAttribute('notExecuted')) { [int]$c.notExecuted } else { 0 }
    }
    $results = @(Select-Xml -Xml $doc -Namespace $ns -XPath '//t:UnitTestResult')
    foreach ($r in $results) {
        $n = $r.Node
        $m = $methods[$n.testId]
        $className = if ($m) { $m.className } else { '' }
        $method = if ($m) { $m.method } else { $n.testName }
        $cases.Add([pscustomobject]@{ project = $assembly; className = $className; method = $method; fullName = ($className + '.' + $method); displayName = $n.testName; outcome = [string]$n.outcome })
    }
    $projects.Add([pscustomobject]@{ project = $assembly; trx = $trx.Name; runOutcome = $runOutcome; total = $total; executed = $executed; passed = $passed; failed = $failed; notExecuted = $notExecuted; results = $results.Count })
    if ($runOutcome -notin @('Completed', 'Failed')) { $violations.Add("project $assembly run outcome '$runOutcome' (partial or aborted run)") }
    if ($executed -ne $total) { $violations.Add("project $assembly executed $executed of $total selected cases") }
    if ($notExecuted -ne 0) { $violations.Add("project $assembly has $notExecuted not executed (skipped) cases") }
    if ($results.Count -ne $total) { $violations.Add("project $assembly lists $($results.Count) results for $total counted cases") }
}
$trxProjects = @($projects | ForEach-Object { $_.project })
foreach ($p in $ExpectedProjects) { if ($trxProjects -notcontains $p) { $violations.Add("project $p has no TRX in $ResultsDirectory") } }
foreach ($p in $trxProjects) { if ($ExpectedProjects -notcontains $p) { $violations.Add("unexpected TRX project $p") } }

$skippedCases = @($cases | Where-Object { $_.outcome -notin @('Passed', 'Failed') })
foreach ($s in $skippedCases) { $violations.Add("case not executed: " + $s.fullName + " (" + $s.outcome + ")") }

$knownRed = @(Get-Content -LiteralPath $KnownRedPath | ForEach-Object { $_.Trim() } | Where-Object { $_ -and -not $_.StartsWith('#') })
$failedNames = @($cases | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.fullName } | Sort-Object -Unique)
$unexpectedFailures = @($failedNames | Where-Object { $knownRed -notcontains $_ })
$missingKnownRed = @($knownRed | Where-Object { $failedNames -notcontains $_ })
foreach ($u in $unexpectedFailures) { $violations.Add('failure outside the known-red set: ' + $u) }
foreach ($m in $missingKnownRed) { $violations.Add('known-red case did not fail (set changed, report separately): ' + $m) }

if (-not (Test-Path -LiteralPath $LoadLogPath -PathType Leaf)) { $violations.Add('native load log missing: ' + $LoadLogPath) }
else {
    $logLines = @(Get-Content -LiteralPath $LoadLogPath | Where-Object { $_ -ne '' })
    if ($logLines.Count -gt 0) { $violations.Add('native load log not empty (' + $logLines.Count + ' attempts): ' + (($logLines | Select-Object -First 5) -join ' | ')) }
}

$leaks = @()
if ($InventoryPath) {
    $inventory = Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json
    $consumerKeys = @{}
    foreach ($c in @($inventory.consumers)) { $consumerKeys[$c.class + '.' + $c.method] = $true }
    foreach ($case in $cases) {
        $simpleClass = ($case.className -split '\.')[-1]
        if ($consumerKeys.ContainsKey($simpleClass + '.' + $case.method)) { $leaks += $case.fullName }
    }
    foreach ($l in ($leaks | Sort-Object -Unique)) { $violations.Add('ShimLegacy consumer leaked into the ordinary gate: ' + $l) }
}
foreach ($rc in $RealClasses) {
    $hits = @($cases | Where-Object { ($_.className -split '\.')[-1] -eq $rc })
    if ($hits.Count -gt 0) { $violations.Add("real-bound class $rc executed in the ordinary gate ($($hits.Count) cases)") }
}
$contractCounts = @{}
foreach ($cc in $ContractClasses) {
    $hits = @($cases | Where-Object { ($_.className -split '\.')[-1] -eq $cc })
    $contractCounts[$cc] = $hits.Count
    if ($hits.Count -eq 0) { $violations.Add("contract class $cc did not execute in the ordinary gate") }
}

$identifiers = @($cases | ForEach-Object { $_.project + '|' + $_.fullName + '|' + $_.displayName } | Sort-Object -Unique)
if ($WriteIdentifiersPath) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $WriteIdentifiersPath) -Force | Out-Null
    [IO.File]::WriteAllLines($WriteIdentifiersPath, $identifiers)
}
if ($RetainedIdentifiersPath) {
    $retained = @(Get-Content -LiteralPath $RetainedIdentifiersPath | Where-Object { $_ -ne '' } | Sort-Object -Unique)
    $onlyRetained = @($retained | Where-Object { $identifiers -notcontains $_ })
    $onlyCurrent = @($identifiers | Where-Object { $retained -notcontains $_ })
    if ($onlyRetained.Count -gt 0 -or $onlyCurrent.Count -gt 0) {
        $violations.Add('discovery drift against the retained identifier list: ' + $onlyRetained.Count + ' retained only, ' + $onlyCurrent.Count + ' current only')
    }
}

$report = [pscustomobject]@{
    resultsDirectory = $ResultsDirectory
    projects = @($projects)
    totals = [pscustomobject]@{ projects = $projects.Count; cases = $cases.Count; passed = @($cases | Where-Object { $_.outcome -eq 'Passed' }).Count; failed = $failedNames.Count; notExecuted = $skippedCases.Count }
    knownRed = $knownRed
    failed = $failedNames
    unexpectedFailures = $unexpectedFailures
    missingKnownRed = $missingKnownRed
    loadLog = $LoadLogPath
    shimLegacyLeaks = @($leaks | Sort-Object -Unique)
    contractCounts = $contractCounts
    emptyRuns = @($emptyRuns)
    identifiers = $identifiers.Count
    violations = @($violations)
    outcome = if ($violations.Count -eq 0) { 'Pass' } else { 'Fail' }
}
if ($ReportPath) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}
Write-Output ('gate ' + $report.outcome + ': projects ' + $projects.Count + ', empty runs ' + $emptyRuns.Count + ', cases ' + $cases.Count + ', passed ' + $report.totals.passed + ', failed ' + $failedNames.Count + ', not executed ' + $skippedCases.Count + ', known-red ' + $knownRed.Count + ', violations ' + $violations.Count)
foreach ($p in $projects) { Write-Output ('  ' + $p.project + ': ' + $p.runOutcome + ' total ' + $p.total + ' executed ' + $p.executed + ' passed ' + $p.passed + ' failed ' + $p.failed + ' notExecuted ' + $p.notExecuted) }
foreach ($v in $violations) { Write-Output ('  VIOLATION ' + $v) }
if ($violations.Count -gt 0) { throw 'Ordinary gate evaluation failed.' }
