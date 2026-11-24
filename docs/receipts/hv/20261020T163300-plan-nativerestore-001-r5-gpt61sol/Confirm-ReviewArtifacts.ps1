[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Round 5 saved verdict and primary persistence receipts', 'Verify identities, counts and exact content')) { return }
$verdict = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') -Raw | ConvertFrom-Json
$proof = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Raw | ConvertFrom-Json
$prior4 = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../20261020T153600-plan-nativerestore-001-r4-gpt61sol/verdict.json') -Raw | ConvertFrom-Json
$prior3 = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../20261020T141600-plan-nativerestore-001-r3b-gpt61sol/verdict.json') -Raw | ConvertFrom-Json
$integrity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'review-integrity-final.json') -Raw | ConvertFrom-Json
$scripts = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'script-validation.json') -Raw | ConvertFrom-Json
$sources = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'pinned-sources.verified-final.json') -Raw | ConvertFrom-Json)
$earlierSources = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'pinned-sources.verified.json') -Raw | ConvertFrom-Json)
$missing4 = @($prior4.findings.id | Where-Object { @($verdict.findings | Where-Object id -EQ $_).Count -ne 1 })
$missing3 = foreach ($finding in $prior3.findings) {
    $current = @($verdict.findings | Where-Object id -EQ $finding.id)
    $old4 = @($prior4.findings | Where-Object id -EQ $finding.id)
    if ($current.Count -ne 1 -or $old4.Count -ne 1 -or $current[0].round3bDispositionInRevision4 -ne $old4[0].status) { $finding.id }
}
$sourceChanges = foreach ($source in $earlierSources) {
    $current = @($sources | Where-Object path -CEQ $source.path)
    if ($current.Count -ne 1 -or $current[0].sha256 -ne $source.sha256) { $source.path }
}
$content = foreach ($file in $proof.content) {
    $localHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $file.name)).Hash
    $readbackHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot ($file.name + '.readback'))).Hash
    [pscustomobject]@{ name = $file.name; localSha256 = $localHash; readbackSha256 = $readbackHash; exact = ($localHash -eq $file.sourceSha256 -and $readbackHash -eq $file.sourceSha256 -and $file.rawBytesMatch) }
}
$harnessPatternCandidates = @(rg -l 'ViceNative.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' tests/ViceSharp.TestHarness -g '*.cs')
$stopText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'persistence-stop-gate.txt')).Trim()
$stop = $stopText | ConvertFrom-Json
$checks = [ordered]@{
    all41Round4IdsExactlyOnce = ($missing4.Count -eq 0 -and @($verdict.findings | Where-Object round4Status).Count -eq 41)
    all35Round3bRevision4Dispositions = (@($missing3).Count -eq 0)
    findings47Unique = ($verdict.findings.Count -eq 47 -and @($verdict.findings | Group-Object id | Where-Object Count -GT 1).Count -eq 0)
    pass40Fail3Unknown0AddAc4 = (@($verdict.findings | Where-Object status -EQ PASS).Count -eq 40 -and @($verdict.findings | Where-Object status -EQ FAIL).Count -eq 3 -and @($verdict.findings | Where-Object status -EQ UNKNOWN).Count -eq 0 -and @($verdict.findings | Where-Object status -EQ ADD-AC).Count -eq 4)
    exactCompleteContent = (@($content | Where-Object { -not $_.exact }).Count -eq 0 -and @($content).Count -eq 2)
    primaryCompletedExactTurn = ($proof.persisted -and -not $proof.queued -and -not $proof.degraded -and $proof.turnStatus -eq 'completed' -and $proof.reviewerSessionId -eq $verdict.reviewerSessionId -and $proof.reviewerRequestId -eq $verdict.reviewerRequestId)
    pins37StillMatch = ($integrity.pinCount -eq 37 -and $integrity.pinMatches -eq 37)
    pinnedSources23Match = ($sources.Count -eq 23 -and @($sources | Where-Object { -not $_.matches }).Count -eq 0 -and @($sourceChanges).Count -eq 0)
    native163ContentUnchanged = ($integrity.nativeComparedEntries -eq 163 -and @($integrity.changedNativeEntries).Count -eq 0 -and @($integrity.deletedNativeEntries).Count -eq 0 -and $integrity.nativeStatusUnchanged)
    outsideReceiptStatusUnchanged = $integrity.statusOutsideReceiptUnchanged
    diffWhitespaceClean = ($integrity.gitDiffCheckExitCode -eq 0)
    scriptsParseAndAnalyzeClean = ($scripts.parserErrorCount -eq 0 -and $scripts.analyzerFindingCount -eq 0)
    stopGateNoBlock = ($stop.decision -ne 'block' -and $stopText -eq '{}')
    disagreeAndScoresPreserved = ($verdict.OverallVerdict -eq 'DISAGREE' -and $verdict.accuracyPercent -eq 97 -and $verdict.completenessPercent -eq 96)
}
$failed = @($checks.Keys | Where-Object { -not $checks[$_] })
$report = [ordered]@{
    timestampLocal = (Get-Date -Format s)
    artifactVerification = $(if ($failed.Count) { 'FAIL' } else { 'PASS: saved review artifacts only; subject plan remains DISAGREE' })
    checks = $checks
    failedChecks = $failed
    exactContent = @($content)
    stopGateOutput = $stopText
    stopGateInterpretation = 'Supported plugin-hook.ps1 Close-PluginTurnIfNeeded line 1262 emits the empty object on its nonblocking path. No fictional status:passed field is claimed.'
    semanticHarnessCandidatesUsingPlanUnescapedDotPattern = $harnessPatternCandidates.Count
    reviewerSessionId = $verdict.reviewerSessionId
    reviewerRequestId = $verdict.reviewerRequestId
    fullFailList = @($verdict.findings | Where-Object status -EQ FAIL | Select-Object id, severity, section)
}
$report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.artifact-check.json')
$report | ConvertTo-Json -Depth 15
if ($failed.Count) { throw 'Saved review artifact verification failed' }
