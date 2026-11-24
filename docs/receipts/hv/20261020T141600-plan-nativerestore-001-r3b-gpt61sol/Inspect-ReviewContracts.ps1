[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('HistoricalInventory','PriorFindings','RequestMetadata')][string]$Operation)
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Operation, 'Read complete pinned data and retain structured review evidence')) { return }
switch ($Operation) {
    'HistoricalInventory' {
        $path = 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt'
        $rows = @(Get-Content -LiteralPath $path | ForEach-Object {
            [pscustomobject][ordered]@{path=$_;extension=[System.IO.Path]::GetExtension($_);injected=($_ -match 'shim|config\.h$')}
        })
        $report = [ordered]@{
            timestampLocal=(Get-Date -Format s);path=$path;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
            count=$rows.Count;rows=$rows;extensions=@($rows | Group-Object -Property extension | Sort-Object -Property Name | ForEach-Object { [ordered]@{extension=$_.Name;count=$_.Count} })
        }
        $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-inventory.json') -Encoding utf8
        [ordered]@{timestampLocal=$report.timestampLocal;count=$report.count;extensions=$report.extensions;injected=@($rows | Where-Object { $_.injected })} | ConvertTo-Json -Depth 10 | Write-Output
    }
    'PriorFindings' {
        $prior = Get-Content -LiteralPath 'docs/receipts/hv/20261020T131300-plan-nativerestore-001-r2-gpt61sol/verdict.json' -Raw | ConvertFrom-Json
        foreach ($finding in $prior.findings) { $finding | Select-Object id,status,severity,file,section,rationale | ConvertTo-Json -Depth 10 | Write-Output }
    }
    'RequestMetadata' {
        $request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json -AsHashtable
        $summary = [ordered]@{timestampLocal=(Get-Date -Format s);keys=@($request.Keys)}
        foreach ($key in $request.Keys) {
            if ($key -match '(?i)reviewer|implementer|executable|agent|model|effort|parent|branch|commit|sha|command|args|arguments|workspace|launch|sandbox') { $summary[$key] = $request[$key] }
        }
        $text = $summary | ConvertTo-Json -Depth 20
        if ($text -match '(?i)(?:X-Api-Key\s*[:=]\s*|apiKey\s*[:=]\s*)[\x22\x27]?[A-Za-z0-9+/=_-]{20,}|Bearer\s+[A-Za-z0-9+/=_-]{20,}') { throw 'Credential-shaped metadata suppressed.' }
        $text | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'request-metadata.json') -Encoding utf8
        Write-Output $text
    }
}
