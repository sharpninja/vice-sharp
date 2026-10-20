[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$helperNames = @('Read-ReviewInput.ps1','Review-Probe.ps1','Invoke-ReviewLogging.ps1','Search-ReviewInput.ps1','Inspect-ReviewState.ps1','Save-ReviewAudit.ps1','Inspect-VsfFixtures.ps1','Inspect-PlanClaims.ps1','Verify-ReviewBoundary.ps1','Write-ReviewVerdict.ps1','Save-CompleteReview.ps1','Test-ReviewArtifacts.ps1')
$results = foreach ($name in $helperNames) {
    $filePath = Join-Path $PSScriptRoot $name
    $tokens = $null
    $parseErrors = $null
    [Management.Automation.Language.Parser]::ParseFile($filePath,[ref]$tokens,[ref]$parseErrors) | Out-Null
    $diagnostics = @(Invoke-ScriptAnalyzer -Path $filePath -Severity Warning,Error)
    [pscustomobject]@{path=$name;parserErrorCount=@($parseErrors).Count;analyzerFindingCount=$diagnostics.Count;diagnostics=@($diagnostics | Select-Object RuleName,Severity,Line,Message)}
}
$verdict = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') | ConvertFrom-Json
$original = Get-Content -Raw -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) '20261020T122200-plan-nativerestore-001-gpt61sol/verdict.json') | ConvertFrom-Json
$counts = @{}
foreach ($status in @('PASS','FAIL','UNKNOWN','ADD-AC')) { $counts[$status] = @($verdict.findings | Where-Object status -eq $status).Count }
$missing = @($original.findings.id | Where-Object { $_ -notin $verdict.findings.id })
$duplicates = @($verdict.findings | Group-Object id | Where-Object Count -gt 1)
$requiredFields = @('OverallVerdict','accuracyPercent','completenessPercent','passCount','failCount','unknownCount','findings','reviewerAgent','reviewerModel','reviewerEffort','reviewerSessionId','reviewerRequestId','requestPath','responsePath')
$missingFields = @($requiredFields | Where-Object { $_ -notin $verdict.PSObject.Properties.Name })
$receipt = [ordered]@{timestampLocal=(Get-Date -Format 's');helpers=$results;analyzerVersion=(Get-Command Invoke-ScriptAnalyzer).Version.ToString();pesterVersion=(Get-Command Invoke-Pester).Version.ToString();findingCounts=$counts;round1Ids=$original.findings.Count;round1IdsCovered=$original.findings.Count-$missing.Count;missingIds=$missing;duplicateIds=$duplicates.Name;missingRequiredFields=$missingFields;jsonCountsMatch=($counts.PASS -eq $verdict.passCount -and $counts.FAIL -eq $verdict.failCount -and $counts.UNKNOWN -eq $verdict.unknownCount -and $counts.'ADD-AC' -eq $verdict.addAcCount);overallThresholdConsistent=($verdict.OverallVerdict -eq 'DISAGREE' -and ($verdict.accuracyPercent -lt 98 -or $verdict.completenessPercent -lt 98));scope='Review artifact checks only. No product tests or native compilation.'}
if ($PSCmdlet.ShouldProcess('helper-validation.json','Write parser/analyzer and verdict schema checks')) { $receipt | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'helper-validation.json') -Encoding utf8 }
$receipt | ConvertTo-Json -Depth 16
if (@($results | Where-Object { $_.parserErrorCount -or $_.analyzerFindingCount }).Count -or $missing.Count -or $duplicates.Count -or $missingFields.Count -or -not $receipt.jsonCountsMatch -or -not $receipt.overallThresholdConsistent) { throw 'Review artifact validation failed.' }
