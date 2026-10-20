#Requires -Version 7.0
<#
.SYNOPSIS
Runs on PAYTON-LEGION2: Phase A step 1 (part 1), the three native-safe test projects unfiltered in Release with TRX receipts,
native inventory captured before and after, into docs/receipts/lockstep/<stamp>-phase-a-baseline-windows/. No TestHarness build,
no Vice.Native.proj, no shim build.
#>
[CmdletBinding()]
param(
    [string]$WorkspacePath = 'F:\GitHub\vice-sharp',
    [string]$Stamp = '',
    # Receipt folder suffix; Phase B uses phase-b-native-safe-windows (OMAR-VICE# brief 2026-10-19T19:43).
    [string]$Suffix = 'phase-a-baseline-windows'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $Stamp) { $Stamp = Get-Date -Format 'yyyyMMddTHHmmss' }
$receipt = Join-Path $WorkspacePath ("docs\receipts\lockstep\{0}-{1}" -f $Stamp, $Suffix)
[void][IO.Directory]::CreateDirectory($receipt)
$log = Join-Path $receipt 'run.log'
function Note { param([string]$Text) $line = ("{0} {1}" -f (Get-Date -Format 's'), $Text); Add-Content -LiteralPath $log -Value $line; Write-Output $line }

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    $candidate = 'C:\Program Files\dotnet\dotnet.exe'
    if (-not (Test-Path -LiteralPath $candidate)) { throw 'dotnet not found on PATH or at C:\Program Files\dotnet\dotnet.exe' }
    $dotnetExe = $candidate
} else { $dotnetExe = $dotnet.Source }
Note ("dotnet: {0}" -f $dotnetExe)
Note ("dotnet --version: {0}" -f (& $dotnetExe --version))
$safe = ($WorkspacePath -replace '\\', '/')
$nativeSafe = "$safe/native/vice"
Note ("HEAD: {0}" -f (& git -c "safe.directory=$safe" -C $WorkspacePath rev-parse HEAD))
Note ("branch: {0}" -f (& git -c "safe.directory=$safe" -C $WorkspacePath rev-parse --abbrev-ref HEAD))
Note ("gitlink: {0}" -f (& git -c "safe.directory=$safe" -C $WorkspacePath rev-parse 'HEAD:native/vice'))

function Save-NativeInventory {
    param([string]$Name)
    $path = Join-Path $receipt $Name
    $lines = @(& git -c "safe.directory=$nativeSafe" -C (Join-Path $WorkspacePath 'native\vice') status --porcelain=v1 -uall)
    [IO.File]::WriteAllLines($path, $lines)
    $modified = @($lines | Where-Object { $_ -notmatch '^\?\?' }).Count
    $untracked = @($lines | Where-Object { $_ -match '^\?\?' }).Count
    Note ("{0}: {1} entries ({2} tracked changes, {3} untracked), sha256 {4}" -f $Name, $lines.Count, $modified, $untracked, (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.Substring(0, 16))
}
Save-NativeInventory -Name 'native-inventory-before.txt'

$projects = @('ViceSharp.OracleContract.Tests', 'ViceSharp.VsfCompatibility.Tests', 'ViceSharp.NativeClockProbe.Tests')
$summary = [ordered]@{ stamp = $Stamp; machine = $env:COMPUTERNAME; user = $env:USERNAME; results = @() }
foreach ($p in $projects) {
    $csproj = Join-Path $WorkspacePath ("tests\{0}\{0}.csproj" -f $p)
    $short = $p -replace '^ViceSharp\.', '' -replace '\.Tests$', ''
    $trx = "$short.trx"
    $buildLog = Join-Path $receipt "$short.console.log"
    Note ("dotnet test {0} -c Release --nologo --logger trx;LogFileName={1} --results-directory {2}" -f $csproj, $trx, $receipt)
    & $dotnetExe test $csproj -c Release --nologo --logger "trx;LogFileName=$trx" --results-directory $receipt *> $buildLog
    $exit = $LASTEXITCODE
    $trxPath = Join-Path $receipt $trx
    $counts = [ordered]@{ project = $p; exitCode = $exit; total = -1; passed = -1; failed = -1; skipped = -1 }
    if (Test-Path -LiteralPath $trxPath) {
        [xml]$x = Get-Content -LiteralPath $trxPath -Raw
        $c = $x.TestRun.ResultSummary.Counters
        $counts.total = [int]$c.total; $counts.passed = [int]$c.passed; $counts.failed = [int]$c.failed
        $counts.skipped = [int]$c.total - [int]$c.executed
    }
    $summary.results += $counts
    Note ("{0}: exit {1}, total {2}, passed {3}, failed {4}, skipped {5}" -f $p, $exit, $counts.total, $counts.passed, $counts.failed, $counts.skipped)
}
Save-NativeInventory -Name 'native-inventory-after.txt'
$before = Get-FileHash -LiteralPath (Join-Path $receipt 'native-inventory-before.txt') -Algorithm SHA256
$after = Get-FileHash -LiteralPath (Join-Path $receipt 'native-inventory-after.txt') -Algorithm SHA256
$summary['nativeInventoryUnchanged'] = ($before.Hash -eq $after.Hash)
Note ("native inventory unchanged: {0}" -f $summary.nativeInventoryUnchanged)
$summary['finishedAt'] = Get-Date -Format 's'
[IO.File]::WriteAllText((Join-Path $receipt 'summary.json'), ($summary | ConvertTo-Json -Depth 5))
Note ("receipt: {0}" -f $receipt)
