[CmdletBinding(SupportsShouldProcess)]
param([string]$Label='turn-readback')
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Label, 'Read this reviewer session through the supported client wrapper')) { return }
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') -Raw | ConvertFrom-Json
$paramsPath = Join-Path $PSScriptRoot ($Label + '.params.json')
[ordered]@{agent='Codex';text='PLAN-NATIVERESTORE-001 revision 3';limit=5;offset=0} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $paramsPath -Encoding utf8
& (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method 'client.SessionLog.QueryAsync' -PayloadPath $paramsPath -Label $Label -Quiet
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
Import-McpYamlSerializer
$raw = Get-Content -LiteralPath (Join-Path $PSScriptRoot ($Label + '.txt')) -Raw
$parsed = ConvertFrom-Yaml -Yaml $raw -AllDocuments
$parsed | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label + '.parsed.json')) -Encoding utf8
foreach ($doc in $parsed) {
    if ($doc.payload.result) {
        $own = @($doc.payload.result.items | Where-Object { $_.sessionId -eq $identity.reviewerSessionId })
        foreach ($session in $own) {
            [ordered]@{timestampLocal=(Get-Date -Format s);sessionKeys=@($session.Keys);sessionId=$session.sessionId;status=$session.status;turns=@($session.turns | ForEach-Object { [ordered]@{requestId=$_.requestId;status=$_.status;turnKeys=@($_.Keys);responseCharacters=([string]$_.response).Length;dialogCount=@($_.processingDialog).Count} })} | ConvertTo-Json -Depth 10 | Write-Output
        }
    }
}
