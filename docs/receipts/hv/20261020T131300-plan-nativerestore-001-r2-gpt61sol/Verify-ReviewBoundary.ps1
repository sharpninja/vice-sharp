[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$before = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'state-before.json') | ConvertFrom-Json
$nativeStatus = @(& git -C native/vice status --porcelain=v1 -uall)
$changes = foreach ($entry in $before.inventory) {
    $filePath = Join-Path 'native/vice' $entry.path
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) { 'missing: ' + $entry.path; continue }
    if ((Get-Item -Force -LiteralPath $filePath).Length -ne $entry.length -or (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash -ne $entry.sha256) { 'bytes: ' + $entry.path }
}
$pinResults = foreach ($pin in (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') | ConvertFrom-Json)) {
    [pscustomobject]@{path=$pin.relativePath;match=((Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash -eq $pin.sha256 -and (Get-Item -Force -LiteralPath $pin.path).Length -eq $pin.bytes)}
}
$sourceChanges = foreach ($sourcePin in (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'source-pin-verification.json') | ConvertFrom-Json)) {
    $filePath = Join-Path 'artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source' $sourcePin.path
    if ((Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash -ne $sourcePin.sha256) { $sourcePin.path }
}
$result = [ordered]@{
    timestampLocal=(Get-Date -Format 's')
    initialHead=$before.head
    finalHead=(& git rev-parse HEAD)
    branch=(& git branch --show-current)
    requestedHead='4c5cb166'
    nativeHead=(& git -C native/vice rev-parse HEAD)
    gitlink=(& git ls-tree HEAD native/vice)
    nativeStatusUnchanged=(($nativeStatus -join "`n") -ceq ($before.nativeStatus -join "`n"))
    nativeInventoriedFiles=$before.inventory.Count
    nativeContentChanges=@($changes)
    pinnedMaterializationChanges=@($sourceChanges)
    pinCount=@($pinResults).Count
    pinMismatches=@($pinResults | Where-Object { -not $_.match })
    trackedSuperprojectChanges=@(& git diff --name-only)
    stagedSuperprojectChanges=@(& git diff --cached --name-only)
    nativeStagedDiff=@(& git -C native/vice diff --cached)
    diffCheck=@(& git diff --check)
    diffCheckExit=$LASTEXITCODE
    sourceReviewOnly=$true
    fullSolutionBuilds=0
    nativeBuilds=0
    managedTestRuns=0
    commits=0
    pushes=0
}
if ($PSCmdlet.ShouldProcess('boundary-verification.json','Write final review boundary evidence')) { $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'boundary-verification.json') -Encoding utf8 }
$result | ConvertTo-Json -Depth 12
if (-not $result.nativeStatusUnchanged -or @($changes).Count -or @($sourceChanges).Count -or $result.pinMismatches.Count -or $result.diffCheckExit -ne 0) { throw 'Review boundary verification failed.' }
