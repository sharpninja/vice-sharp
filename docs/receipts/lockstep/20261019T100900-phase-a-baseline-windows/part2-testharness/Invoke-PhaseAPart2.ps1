#Requires -Version 7.4
<#
.SYNOPSIS
    Phase A step 1 part 2 (handoff v2): TestHarness diagnostic filters on PAYTON-LEGION2
    against the existing prebuilt shim, without invoking native/Vice.Native.proj.

.DESCRIPTION
    The TestHarness csproj runs target BuildViceNative before Build when
    Exists('$(ViceNativeProject)'). Passing the global property ViceNativeProject with a
    path that does not exist makes that condition false, so the native project is never
    invoked. The build log is captured at detailed verbosity so the skip is provable.

    Native inventory method matches part 1 (hash-reproduced): `git status --porcelain
    --untracked-files=all` inside native/vice, written with Out-File utf8NoBOM.
    GIT_OPTIONAL_LOCKS=0 keeps git status from refreshing the submodule index.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$OutDir = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$harness = Join-Path $RepoRoot 'tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj'
$nativeRoot = Join-Path $RepoRoot 'native\vice'
$disabledNative = Join-Path $RepoRoot 'native\__vice-native-disabled-phase-a__.proj'
$logPath = Join-Path $OutDir 'run.log'

function Write-RunLog {
    param([Parameter(Mandatory)][string]$Message)
    $line = '{0} {1}' -f (Get-Date -Format s), $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding utf8NoBOM
    Write-Information $line -InformationAction Continue
}

function Save-NativeInventory {
    param([Parameter(Mandatory)][string]$Name)
    $path = Join-Path $OutDir $Name
    $env:GIT_OPTIONAL_LOCKS = '0'
    try {
        $entries = @(git -C $nativeRoot status --porcelain --untracked-files=all)
    }
    finally {
        Remove-Item Env:GIT_OPTIONAL_LOCKS -ErrorAction SilentlyContinue
    }
    $entries | Out-File -LiteralPath $path -Encoding utf8NoBOM
    $tracked = @($entries | Where-Object { -not $_.StartsWith('??') }).Count
    $untracked = @($entries | Where-Object { $_.StartsWith('??') }).Count
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    Write-RunLog ('{0}: {1} entries ({2} tracked changes, {3} untracked), sha256 {4}' -f $Name, $entries.Count, $tracked, $untracked, $hash)
    return $hash
}

function Get-TrxCount {
    param([Parameter(Mandatory)][string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [pscustomobject]@{ total = 0; executed = 0; passed = 0; failed = 0; skipped = 0; trxPresent = $false }
    }
    [xml]$trx = Get-Content -LiteralPath $TrxPath -Raw
    $c = $trx.TestRun.ResultSummary.Counters
    return [pscustomobject]@{
        total      = [int]$c.total
        executed   = [int]$c.executed
        passed     = [int]$c.passed
        failed     = [int]$c.failed
        skipped    = [int]$c.notExecuted
        trxPresent = $true
    }
}

$filters = [ordered]@{
    'CpuNmiFive'   = 'FullyQualifiedName~CpuInterruptTests.LdaImm_IrqOnLastClk_DoesNotDispatchBeforeNextFetch|FullyQualifiedName~CpuInterruptTests.RtsThenLdaImm_NextTick_FetchesFollowingOpcode|FullyQualifiedName~CpuInterruptTests.Rts_DelayNextFetchTick_DoesNotTakeIrqBeforeReturnFetch|FullyQualifiedName~Mos6510NmiTests.Nmi_IsEdgeTriggered_HoldingLineDoesNotRefire|FullyQualifiedName~Mos6510NmiTests.Nmi_RtiReturnsCpuToPreNmiState'
    'Determinism'  = 'Category=Determinism'
    'Acceptance'   = 'FullyQualifiedName~AcceptanceTests'
    'LockstepValidationTests'       = 'FullyQualifiedName~LockstepValidationTests'
    'Vic20NativeLockstepTests'      = 'FullyQualifiedName~Vic20NativeLockstepTests'
    'Vic20HalfCycleComparatorTests' = 'FullyQualifiedName~Vic20HalfCycleComparatorTests'
    'CpuVisiblePcTimingTests'       = 'FullyQualifiedName~CpuVisiblePcTimingTests'
    'X64ScVariantLockstepTests'     = 'FullyQualifiedName~X64ScVariantLockstepTests'
}

if (-not $PSCmdlet.ShouldProcess($harness, 'Build TestHarness (native project disabled) and run four diagnostic filters')) {
    foreach ($k in $filters.Keys) { Write-Information ("WhatIf filter {0}: {1}" -f $k, $filters[$k]) -InformationAction Continue }
    Write-Information ("WhatIf build: dotnet build {0} -c Release -p:ViceNativeProject={1} -v:detailed" -f $harness, $disabledNative) -InformationAction Continue
    return
}

if (Test-Path -LiteralPath $disabledNative) { throw "Disabled-native sentinel path unexpectedly exists: $disabledNative" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-RunLog ('dotnet: {0}' -f (Get-Command dotnet).Source)
Write-RunLog ('dotnet --version: {0}' -f (dotnet --version))
Write-RunLog ('HEAD: {0}' -f (git -C $RepoRoot rev-parse HEAD))
Write-RunLog ('branch: {0}' -f (git -C $RepoRoot rev-parse --abbrev-ref HEAD))
Write-RunLog ('gitlink: {0}' -f ((git -C $RepoRoot ls-tree HEAD native/vice) -split '\s+')[2])
$before = Save-NativeInventory -Name 'native-inventory-before.txt'

$buildLog = Join-Path $OutDir 'build.log'
Write-RunLog ('dotnet build {0} -c Release -p:ViceNativeProject={1} -v:detailed' -f $harness, $disabledNative)
dotnet build $harness -c Release --nologo "-p:ViceNativeProject=$disabledNative" -v:detailed *> $buildLog
$buildExit = $LASTEXITCODE
$skipLine = Select-String -LiteralPath $buildLog -Pattern 'Target "BuildViceNative" skipped' -SimpleMatch | Select-Object -First 1
$nativeInvoked = Select-String -LiteralPath $buildLog -Pattern 'Vice.Native.proj' -SimpleMatch | Select-Object -First 1
Write-RunLog ('build exit {0}; BuildViceNative skip line present: {1}; Vice.Native.proj mentioned in build log: {2}' -f $buildExit, [bool]$skipLine, [bool]$nativeInvoked)
$buildSummary = Select-String -LiteralPath $buildLog -Pattern '(Warning\(s\)|Error\(s\)|Build succeeded|Build FAILED)' | ForEach-Object { $_.Line.Trim() }
foreach ($s in $buildSummary) { Write-RunLog ('build: {0}' -f $s) }

$results = [System.Collections.Generic.List[object]]::new()
if ($buildExit -eq 0) {
    foreach ($k in $filters.Keys) {
        $trxName = "$k.trx"
        $console = Join-Path $OutDir "$k.console.log"
        Write-RunLog ('dotnet test {0} -c Release --no-build --blame-hang-timeout 30m --filter "{1}" --logger trx;LogFileName={2}' -f $harness, $filters[$k], $trxName)
        dotnet test $harness -c Release --no-build --nologo --blame-hang-timeout 30m --filter $filters[$k] --logger "trx;LogFileName=$trxName" --results-directory $OutDir *> $console
        $exit = $LASTEXITCODE
        $counts = Get-TrxCount -TrxPath (Join-Path $OutDir $trxName)
        Write-RunLog ('{0}: exit {1}, total {2}, executed {3}, passed {4}, failed {5}, skipped {6}' -f $k, $exit, $counts.total, $counts.executed, $counts.passed, $counts.failed, $counts.skipped)
        $results.Add([pscustomobject]@{ filter = $k; expression = $filters[$k]; exitCode = $exit; counts = $counts })
    }
}

$after = Save-NativeInventory -Name 'native-inventory-after.txt'
Write-RunLog ('native inventory unchanged: {0}' -f ($before -eq $after))

[pscustomobject]@{
    stamp                   = (Split-Path -Leaf (Split-Path -Parent $OutDir))
    part                    = 'phase-a-step1-part2-testharness'
    machine                 = $env:COMPUTERNAME
    user                    = $env:USERNAME
    buildExitCode           = $buildExit
    buildViceNativeSkipped  = [bool]$skipLine
    viceNativeProjInBuildLog = [bool]$nativeInvoked
    results                 = $results
    nativeInventoryUnchanged = ($before -eq $after)
    finishedAt              = (Get-Date -Format s)
} | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $OutDir 'summary.json') -Encoding utf8NoBOM
Write-RunLog ('receipt: {0}' -f $OutDir)
