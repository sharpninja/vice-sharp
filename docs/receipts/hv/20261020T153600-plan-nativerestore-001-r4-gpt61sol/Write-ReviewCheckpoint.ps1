[CmdletBinding(SupportsShouldProcess)]
param([string]$Label,[string]$Text,[switch]$Decision)
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
if (-not $PSCmdlet.ShouldProcess($Label,'Append review checkpoint to own supported-plugin turn')) { return }
$identity=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') -Raw | ConvertFrom-Json
$category=if ($Decision) { 'decision' } else { 'observation' }
$payload=[ordered]@{requestId=$identity.reviewerRequestId;dialogItems=@([ordered]@{timestamp=[DateTime]::Now.ToString('s');role='model';category=$category;content=$Text})}
$paramsPath=Join-Path $PSScriptRoot ($Label+'.params.json')
$payload | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $paramsPath -Encoding utf8
& (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method workflow.sessionlog.appendDialog -PayloadPath $paramsPath -Label $Label
if ($Decision) {
    $payload=[ordered]@{requestId=$identity.reviewerRequestId;actions=@([ordered]@{order=[int][DateTimeOffset]::Now.ToUnixTimeSeconds();type='design_decision';status='completed';filePath='docs/plans/PLAN-NATIVERESTORE-001.md';description=$Text})}
    $payload | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $paramsPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method workflow.sessionlog.appendActions -PayloadPath $paramsPath -Label ($Label+'-action')
}
