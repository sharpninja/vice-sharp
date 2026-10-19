#Requires -Version 7.4
<#
.SYNOPSIS
    PLAN-NUKETESTS-001 step 5 on PAYTON-LEGION2: ./build.ps1 BuildTest, dotnet test ViceSharp.Library.Tests
    (Release), ./build.ps1 RommDesktopTest. Captures logs, copies every TRX produced into the receipt folder,
    reads counters from each TRX, and brackets the run with the native/vice inventory (method as in the
    Phase A receipts). No native project is invoked by these targets.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$OutDir = (Join-Path $PSScriptRoot ((Get-Date -Format 'yyyyMMddTHHmmss') + '-targets-windows'))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$steps = @(
    [pscustomobject]@{ Name = 'BuildTest'; Exe = 'pwsh.exe'; Args = @('-NoProfile', '-NonInteractive', '-File', (Join-Path $RepoRoot 'build.ps1'), 'BuildTest'); TrxRoot = 'TestResults\build-tests' }
    [pscustomobject]@{ Name = 'LibraryTests'; Exe = 'dotnet'; Args = @('test', (Join-Path $RepoRoot 'tests\ViceSharp.Library.Tests\ViceSharp.Library.Tests.csproj'), '-c', 'Release', '--nologo', '--logger', 'trx;LogFileName=ViceSharp.Library.Tests.trx', '--results-directory', '__OUT__'); TrxRoot = $null }
    [pscustomobject]@{ Name = 'RommDesktopTest'; Exe = 'pwsh.exe'; Args = @('-NoProfile', '-NonInteractive', '-File', (Join-Path $RepoRoot 'build.ps1'), 'RommDesktopTest'); TrxRoot = 'TestResults\romm-desktop' }
)

if (-not $PSCmdlet.ShouldProcess($RepoRoot, 'Run PLAN-NUKETESTS-001 step 5 targets')) {
    foreach ($s in $steps) { Write-Information ('WhatIf {0}: {1} {2}' -f $s.Name, $s.Exe, ($s.Args -join ' ')) -InformationAction Continue }
    return
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$log = Join-Path $OutDir 'run.log'
function Write-RunLog { param([string]$Message) $l = '{0} {1}' -f (Get-Date -Format s), $Message; Add-Content -LiteralPath $log -Value $l -Encoding utf8NoBOM; Write-Information $l -InformationAction Continue }

function Save-NativeInventory {
    param([string]$Name)
    $p = Join-Path $OutDir $Name
    $env:GIT_OPTIONAL_LOCKS = '0'
    try { $e = @(git -C (Join-Path $RepoRoot 'native\vice') status --porcelain --untracked-files=all) } finally { Remove-Item Env:GIT_OPTIONAL_LOCKS -ErrorAction SilentlyContinue }
    $e | Out-File -LiteralPath $p -Encoding utf8NoBOM
    $h = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
    Write-RunLog ('{0}: {1} entries, sha256 {2}' -f $Name, $e.Count, $h)
    return $h
}

function Read-Trx {
    param([string]$Path)
    [xml]$x = Get-Content -LiteralPath $Path -Raw
    $c = $x.TestRun.ResultSummary.Counters
    $notPassed = @($x.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -ne 'Passed' } | ForEach-Object { '{0} [{1}]' -f $_.testName, $_.outcome })
    return [pscustomobject]@{ trx = (Split-Path -Leaf $Path); total = [int]$c.total; executed = [int]$c.executed; passed = [int]$c.passed; failed = [int]$c.failed; notExecuted = [int]$c.notExecuted; notPassed = $notPassed }
}

Write-RunLog ('HEAD {0} branch {1}' -f (git -C $RepoRoot rev-parse HEAD), (git -C $RepoRoot rev-parse --abbrev-ref HEAD))
Write-RunLog ('dotnet {0}' -f (dotnet --version))
$before = Save-NativeInventory 'native-inventory-before.txt'
$results = [System.Collections.Generic.List[object]]::new()

foreach ($s in $steps) {
    $started = Get-Date
    $stepArgs = @($s.Args | ForEach-Object { $_ -replace '__OUT__', $OutDir })
    Write-RunLog ('{0}: {1} {2}' -f $s.Name, $s.Exe, ($stepArgs -join ' '))
    Push-Location $RepoRoot
    try { & $s.Exe @stepArgs *> (Join-Path $OutDir "$($s.Name).log"); $exit = $LASTEXITCODE } finally { Pop-Location }
    $trx = @()
    if ($s.TrxRoot) {
        $trx = @(Get-ChildItem -Path (Join-Path $RepoRoot $s.TrxRoot) -Recurse -Filter *.trx | Where-Object LastWriteTime -ge $started)
        foreach ($t in $trx) { Copy-Item -LiteralPath $t.FullName -Destination (Join-Path $OutDir ("$($s.Name)-" + $t.Name)) }
        $trx = @(Get-ChildItem -Path $OutDir -Filter "$($s.Name)-*.trx")
    }
    else { $trx = @(Get-ChildItem -Path $OutDir -Filter 'ViceSharp.Library.Tests.trx') }
    $counts = @($trx | ForEach-Object { Read-Trx $_.FullName })
    foreach ($c in $counts) { Write-RunLog ('{0}: exit {1}; {2} total {3} executed {4} passed {5} failed {6} notExecuted {7}' -f $s.Name, $exit, $c.trx, $c.total, $c.executed, $c.passed, $c.failed, $c.notExecuted) }
    if (-not $counts.Count) { Write-RunLog ('{0}: exit {1}; no TRX found' -f $s.Name, $exit) }
    $warn = @(Select-String -LiteralPath (Join-Path $OutDir "$($s.Name).log") -Pattern 'NuGet\.Frameworks|warning' | Select-Object -First 5 | ForEach-Object { $_.Line.Trim() })
    $results.Add([pscustomobject]@{ step = $s.Name; exitCode = $exit; trx = $counts; warningLines = $warn })
}

$after = Save-NativeInventory 'native-inventory-after.txt'
Write-RunLog ('native inventory unchanged: {0}' -f ($before -eq $after))
[pscustomobject]@{ machine = $env:COMPUTERNAME; finishedAt = (Get-Date -Format s); results = $results; nativeInventoryUnchanged = ($before -eq $after) } |
    ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $OutDir 'summary.json') -Encoding utf8NoBOM
Write-RunLog ('receipt {0}' -f $OutDir)
