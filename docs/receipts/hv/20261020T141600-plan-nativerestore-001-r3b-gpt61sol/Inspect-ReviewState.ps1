[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Pins','State')][string]$Operation = 'Pins')
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Operation, 'Inspect read-only review state and retain evidence in this receipt')) { return }
$env:GIT_OPTIONAL_LOCKS = '0'
if ($Operation -eq 'Pins') {
    $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
    $checkedPins = foreach ($pin in $pins) {
        if ($pin.path -match '(?i)AGENTS-README-FIRST|api[-_]?key|credentials|secrets') { throw 'Forbidden pin.' }
        $actualHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
        $actualBytes = (Get-Item -LiteralPath $pin.path -Force).Length
        [ordered]@{path=$pin.relativePath;expectedSha256=$pin.sha256;actualSha256=$actualHash;expectedBytes=$pin.bytes;actualBytes=$actualBytes;matches=($pin.sha256 -eq $actualHash -and $pin.bytes -eq $actualBytes)}
    }
    $report = [ordered]@{timestampLocal=(Get-Date -Format s);pinCount=@($checkedPins).Count;driftCount=@($checkedPins | Where-Object { -not $_.matches }).Count;pins=@($checkedPins)}
    $report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'pin-verification.json') -Encoding utf8
    $report | ConvertTo-Json -Depth 15 | Write-Output
} else {
    $commands = @(
        @('status','--porcelain=v1','-uall'),
        @('branch','--show-current'),
        @('rev-parse','HEAD'),
        @('-C','native/vice','status','--porcelain=v1','-uall'),
        @('-C','native/vice','diff','--stat'),
        @('submodule','status'),
        @('diff','--check')
    )
    $report = foreach ($arguments in $commands) {
        $output = @(& git @arguments 2>&1 | ForEach-Object { [string]$_ })
        [ordered]@{command=('git ' + ($arguments -join ' '));exitCode=$LASTEXITCODE;lineCount=$output.Count;output=$output}
    }
    $report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'git-state.json') -Encoding utf8
    foreach ($item in $report) {
        Write-Output ('COMMAND: ' + $item.command + '; exit=' + $item.exitCode + '; lines=' + $item.lineCount)
        $item.output | Select-Object -First 75 | Write-Output
        if ($item.lineCount -gt 75) { Write-Output ('Full output retained in git-state.json; omitted display lines=' + ($item.lineCount - 75)) }
    }
}
