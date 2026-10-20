#Requires -Version 7.4
<#
.SYNOPSIS
    PLAN-OBSERVER-001 S2a Windows gates on PAYTON-LEGION2 at develop a7421a17 (S2a bus adapter; request from
    OMAR-VICE#, 2026-10-20 08:04: "--filter BusAdapter 8 passed, full project 219 passed ... Watch the broken-stderr
    case: the fixture accepts only exit 72").
      Gate 1: dotnet test ViceSharp.NativeClockProbe.Tests -c Release --filter BusAdapter
      Gate 2: the full ViceSharp.NativeClockProbe.Tests project
    Copied from ..\20261020T080100-phase-c-step3-s1c-watch-rows-windows\Invoke-S1cWatchRowsWindows.ps1, with the
    bus-adapter-real evidence folder (compile.log, bus-adapter-observations.log) added. The project only copies
    tools/immutable-vice sources and Fixtures/*.c; nothing builds native/vice. native-inventory-before/after.txt record
    git status of native/vice around the run. Writes TRX files, logs, compile logs and summary.txt into this folder.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$ExpectedHead = 'a7421a17'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = $PSScriptRoot
$proj = Join-Path $RepoRoot 'tests\ViceSharp.NativeClockProbe.Tests\ViceSharp.NativeClockProbe.Tests.csproj'
$bin = Join-Path $RepoRoot 'tests\ViceSharp.NativeClockProbe.Tests\bin\Release\net10.0'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$summary = [System.Collections.Generic.List[string]]::new()
function Add-Line([string]$Text) { $summary.Add($Text); Write-Information $Text -InformationAction Continue }

$head = (git -C $RepoRoot rev-parse --short=8 HEAD).Trim()
if ($head -ne $ExpectedHead) { throw "HEAD is $head, not $ExpectedHead" }
Add-Line ("PLAN-OBSERVER-001 S2a Windows gates, host {0}, develop {1}, started {2}" -f $env:COMPUTERNAME, $head, (Get-Date -Format s))
Add-Line ("dotnet {0}; gcc {1}" -f (dotnet --version), ((& 'C:\msys64\mingw64\bin\gcc.exe' --version | Select-Object -First 1)))

function Save-NativeInventory([string]$Name) {
    $nv = Join-Path $RepoRoot 'native\vice'
    $lines = if (Test-Path -LiteralPath (Join-Path $nv '.git')) { @(git -c color.ui=never -c core.quotepath=off -C $nv status --porcelain=v1 --untracked-files=all) } else { @("native/vice is not a git checkout: $(Test-Path -LiteralPath $nv)") }
    [System.IO.File]::WriteAllLines((Join-Path $out $Name), [string[]]$lines, $utf8)
    (Get-FileHash -LiteralPath (Join-Path $out $Name)).Hash
}
$before = Save-NativeInventory 'native-inventory-before.txt'

dotnet build $proj -c Release -nologo 2>&1 | Out-File -LiteralPath (Join-Path $out 'build.log') -Encoding utf8NoBOM
$buildExit = $LASTEXITCODE
Add-Line "build exit=$buildExit"
if ($buildExit -ne 0) { throw 'build failed; see build.log' }

function Invoke-Gate([string]$Name, [string[]]$Extra) {
    $trx = "$Name.trx"
    $log = Join-Path $out "$Name.log"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet test $proj -c Release --no-build --logger "trx;LogFileName=$trx" --results-directory $out @Extra 2>&1 | Out-File -LiteralPath $log -Encoding utf8NoBOM
    $exit = $LASTEXITCODE
    $sw.Stop()
    [xml]$x = Get-Content -LiteralPath (Join-Path $out $trx) -Raw
    $c = $x.TestRun.ResultSummary.Counters
    Add-Line ('{0}: exit={1} total={2} executed={3} passed={4} failed={5} notExecuted={6} outcome={7} seconds={8:N1}' -f $Name, $exit, $c.total, $c.executed, $c.passed, $c.failed, $c.notExecuted, $x.TestRun.ResultSummary.outcome, $sw.Elapsed.TotalSeconds)
    $exit
}
$g1 = Invoke-Gate 'gate1-busadapter' @('--filter', 'BusAdapter')
$g2 = Invoke-Gate 'gate2-full' @()

foreach ($name in 'bus-adapter-real', 'bus-probe', 'bus-probe-rows') {
    $evidence = Join-Path $bin "$name-evidence"
    $ev = Join-Path $out "$name-evidence"
    if (-not (Test-Path -LiteralPath $evidence)) { Add-Line "evidence folder missing: $evidence"; continue }
    New-Item -ItemType Directory -Force -Path $ev | Out-Null
    Get-ChildItem -LiteralPath $evidence -File | Where-Object Extension -NE '.exe' | Copy-Item -Destination $ev
    Get-ChildItem -LiteralPath $evidence -File | ForEach-Object { Add-Line ('evidence {0}/{1} bytes={2} sha256={3}' -f $name, $_.Name, $_.Length, (Get-FileHash -LiteralPath $_.FullName).Hash) }
    $cl = Join-Path $ev 'compile.log'
    if (Test-Path -LiteralPath $cl) {
        $text = [System.IO.File]::ReadAllText($cl)
        $argsLine = @($text -split "`n" | Where-Object { $_ -like 'Arguments:*' })
        $exitLine = (@($text -split "`n" | Where-Object { $_ -like 'ExitCode:*' }) -join '').Trim()
        $warnings = @($text -split "`n" | Where-Object { $_ -match '(?i)\b(warning|error)\b' }).Count
        $rowsDefine = (($argsLine -join '') -match 'VICE_SHARP_ROWS=32')
        $strict = (($argsLine -join '') -match '-Wall \| -Wextra \| -Werror')
        Add-Line ('{0} compile.log: "{1}", {2} lines with warning or error, -Wall -Wextra -Werror={3}, -DVICE_SHARP_ROWS=32={4}' -f $name, $exitLine, $warnings, $strict, $rowsDefine)
    }
    else { Add-Line "$name compile.log missing" }
}
$after = Save-NativeInventory 'native-inventory-after.txt'
Add-Line ('native/vice inventory before={0} after={1} unchanged={2}' -f $before.Substring(0, 8), $after.Substring(0, 8), ($before -eq $after))
Add-Line ("finished {0}" -f (Get-Date -Format s))
[System.IO.File]::WriteAllLines((Join-Path $out 'summary.txt'), $summary, $utf8)
if ($g1 -ne 0 -or $g2 -ne 0) { exit 1 }
exit 0
