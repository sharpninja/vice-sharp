[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, 'Verify the final read-only boundary and retain integrity evidence')) { return }
$env:GIT_OPTIONAL_LOCKS = '0'
& (Join-Path $PSScriptRoot 'Inspect-ReviewData.ps1') -Operation Inventory -Label final
$initial = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inventory-initial.json') -Raw | ConvertFrom-Json
$final = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inventory-final.json') -Raw | ConvertFrom-Json
$nativeChanges = [System.Collections.Generic.List[object]]::new()
$pathKeys = @(@($initial.paths.path) + @($final.paths.path) | Sort-Object -Unique)
foreach ($key in $pathKeys) {
    $before = @($initial.paths | Where-Object { $_.path -eq $key })
    $after = @($final.paths | Where-Object { $_.path -eq $key })
    if ($before.Count -ne 1 -or $after.Count -ne 1 -or $before[0].sha256 -ne $after[0].sha256 -or $before[0].bytes -ne $after[0].bytes -or $before[0].status -ne $after[0].status) { $nativeChanges.Add([ordered]@{path=$key;before=$before;after=$after}) }
}
$pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
$pinResults = @(foreach ($pin in $pins) {
    if ($pin.path -match '(?i)AGENTS-README-FIRST|api[-_]?key|credentials|secrets') { throw 'Forbidden pin.' }
    $actualHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
    $actualBytes = (Get-Item -LiteralPath $pin.path -Force).Length
    [pscustomobject][ordered]@{path=$pin.relativePath;sha256=$actualHash;bytes=$actualBytes;matches=($pin.sha256 -eq $actualHash -and $pin.bytes -eq $actualBytes)}
})
$sources = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'sources-expanded.json') -Raw | ConvertFrom-Json
$sourceResults = @(foreach ($source in $sources) {
    $path = Join-Path '/home/sharpninja/github/vice-sharp/artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source' $source.path
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    [pscustomobject][ordered]@{path=$source.path;sha256=$actual;unchanged=($actual -eq $source.sha256);pinnedBlobMatches=$source.matches}
})
$rootStatus = @(& git status --porcelain=v1 -uall)
$initialGit = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'git-state.json') -Raw | ConvertFrom-Json
$initialRoot = @($initialGit | Where-Object { $_.command -eq 'git status --porcelain=v1 -uall' })[0].output
$allowedPrefix = 'docs/receipts/hv/20261020T141600-plan-nativerestore-001-r3b-gpt61sol/'
$outsideNew = @($rootStatus | Where-Object { $_ -notin $initialRoot -and -not $_.Substring(3).StartsWith($allowedPrefix,[StringComparison]::Ordinal) })
$diffCheck = @(& git diff --check 2>&1 | ForEach-Object { [string]$_ })
$diffExit = $LASTEXITCODE
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
$cliHash = (Get-FileHash -LiteralPath $request.executable -Algorithm SHA256).Hash
$report = [ordered]@{
    timestampLocal=(Get-Date -Format s);initialHead=$initial.head;finalHead=$final.head;branch=(& git branch --show-current)
    initialNativeHead=$initial.nativeHead;finalNativeHead=$final.nativeHead;gitlink=$final.gitlink
    nativeInventoryPathCount=@($final.paths).Count;nativeInventoryChangedCount=$nativeChanges.Count;nativeChanges=@($nativeChanges.ToArray());nativeIndexDiff=$final.nativeIndexDiff
    pinCount=$pinResults.Count;pinDriftCount=@($pinResults | Where-Object { -not $_.matches }).Count;pins=$pinResults
    materializedSources=$sourceResults;materializedSourceChangedCount=@($sourceResults | Where-Object { -not $_.unchanged -or -not $_.pinnedBlobMatches }).Count
    rootStatus=$rootStatus;newOutsideReceiptChanges=$outsideNew;diffCheckCommand='git diff --check';diffCheckExitCode=$diffExit;diffCheckOutput=$diffCheck
    reviewerCliSha256=$cliHash;reviewerCliMatchesLaunchPin=($cliHash -eq $request.executableSha256)
}
$report | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'final-integrity.json') -Encoding utf8
[ordered]@{timestampLocal=(Get-Date -Format s);nativeInventoryChangedCount=$report.nativeInventoryChangedCount;pinDriftCount=$report.pinDriftCount;materializedSourceChangedCount=$report.materializedSourceChangedCount;newOutsideReceiptChangeCount=$outsideNew.Count;diffCheckExitCode=$diffExit;initialHead=$initial.head;finalHead=$final.head;reviewerCliMatchesLaunchPin=$report.reviewerCliMatchesLaunchPin} | ConvertTo-Json | Write-Output
if ($nativeChanges.Count -ne 0 -or $report.pinDriftCount -ne 0 -or $report.materializedSourceChangedCount -ne 0 -or $outsideNew.Count -ne 0 -or $diffExit -ne 0) { throw 'Review integrity drift requires disclosure before finalization.' }
