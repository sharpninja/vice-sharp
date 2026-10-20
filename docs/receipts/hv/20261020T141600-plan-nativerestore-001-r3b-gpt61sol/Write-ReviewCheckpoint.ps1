[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$Text,[string]$Label = 'checkpoint',[switch]$Decision)
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Label, 'Persist review observation through the supported wrapper')) { return }
$identityPath = Join-Path $PSScriptRoot 'reviewer-identity.json'
$identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json -AsHashtable
$beginText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'plugin-begin.txt') -Raw
if ($beginText -match '(?m)^\s+sessionId:\s*([^\r\n]+)') { $identity.reviewerSessionId = $Matches[1].Trim().Trim('"') }
$identity | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $identityPath -Encoding utf8
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
$category = if ($Decision) { 'decision' } else { 'observation' }
$payload = [ordered]@{
    requestId=$identity.reviewerRequestId
    dialogItems=@([ordered]@{timestamp=(Get-Date -Format s);role='model';category=$category;content=$Text})
}
$payloadPath = Join-Path $PSScriptRoot ($Label + '.params.json')
$payload | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $payloadPath -Encoding utf8
& (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method 'workflow.sessionlog.appendDialog' -PayloadPath $payloadPath -Label ($Label + '-dialog')
$actionType = if ($Decision) { 'design_decision' } else { 'review_checkpoint' }
$payload = [ordered]@{
    requestId=$identity.reviewerRequestId
    actions=@([ordered]@{order=[int][DateTimeOffset]::Now.ToUnixTimeSeconds();type=$actionType;status='completed';description=$Text;filePath=$PSScriptRoot})
}
$payload | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $payloadPath -Encoding utf8
& (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method 'workflow.sessionlog.appendActions' -PayloadPath $payloadPath -Label ($Label + '-actions')
$payload = [ordered]@{agent='Codex';limit=5;offset=0;sessionId=$identity.reviewerSessionId}
$payload | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $payloadPath -Encoding utf8
& (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method 'workflow.sessionlog.queryHistory' -PayloadPath $payloadPath -Label ($Label + '-history')
Write-Output ('Reviewer sessionId=' + $identity.reviewerSessionId + '; requestId=' + $identity.reviewerRequestId + '; parentSessionId=' + $request.parentSessionId + '; parentRequestId=' + $request.parentRequestId)
