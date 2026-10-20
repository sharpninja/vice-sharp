[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Before','After')][string]$Stage = 'Before')
$ErrorActionPreference = 'Stop'
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$nativeRoot = Join-Path $workspaceRoot 'native/vice'
$nativeStatus = @(& git -C $nativeRoot status --porcelain=v1 -uall)
$nativeDiffStat = @(& git -C $nativeRoot diff --stat)
$nativeNumStat = @(& git -C $nativeRoot diff --numstat)
$nativeFiles = foreach ($entry in $nativeStatus) {
    $relativePath = $entry.Substring(3)
    $fullPath = Join-Path $nativeRoot $relativePath
    [pscustomobject]@{ status = $entry.Substring(0,2); path = $relativePath; sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash; bytes = (Get-Item -LiteralPath $fullPath -Force).Length }
}
$snapshot = [ordered]@{
    timestampLocal = (Get-Date).ToString('s')
    branch = [string](& git -C $workspaceRoot branch --show-current)
    commit = [string](& git -C $workspaceRoot rev-parse HEAD)
    gitlink = @(& git -C $workspaceRoot ls-tree HEAD native/vice)
    submoduleStatus = @(& git -C $workspaceRoot submodule status)
    nativeHead = [string](& git -C $nativeRoot rev-parse HEAD)
    nativeStatus = $nativeStatus
    nativeDiffStat = $nativeDiffStat
    nativeNumStat = $nativeNumStat
    nativeFiles = @($nativeFiles)
    superprojectStatus = @(& git -C $workspaceRoot status --porcelain=v1 -uall)
}
if ($PSCmdlet.ShouldProcess($PSScriptRoot, ('Save ' + $Stage + ' read-only state receipt'))) {
    $snapshot | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('state-' + $Stage.ToLowerInvariant() + '.json')) -Encoding utf8
}
[pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); branch = $snapshot.branch; commit = $snapshot.commit; submoduleStatus = $snapshot.submoduleStatus; nativeEntries = $nativeStatus.Count; trackedModified = @($nativeStatus | Where-Object { -not $_.StartsWith('??') }).Count; untracked = @($nativeStatus | Where-Object { $_.StartsWith('??') }).Count; diffStat = $nativeDiffStat } | ConvertTo-Json -Depth 8
