#Requires -Version 7.4
<#
.SYNOPSIS
    PLAN-OBSERVER-001 S1 Windows gates on PAYTON-LEGION2 at develop 7dbfd291 (request from OMAR-VICE#, 2026-10-20 07:46:
    "pull and run the same two gates on Windows ... record the counts plus compile.log").
      Gate 1: dotnet test ViceSharp.NativeClockProbe.Tests -c Release --filter BusObserver
      Gate 2: the full ViceSharp.NativeClockProbe.Tests project
    CompiledCFixture compiles the C fixtures with C:\msys64\mingw64\bin\gcc.exe (or VICE_NATIVE_CC). The project only
    copies tools/immutable-vice sources and Fixtures/*.c; nothing builds native/vice. native-inventory-before/after.txt
    record git status of native/vice around the run as proof it is untouched.
    Writes TRX files, logs, compile.log copies and summary.txt into this folder.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$ExpectedHead = '7dbfd291'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = $PSScriptRoot
$proj = Join-Path $RepoRoot 'tests\ViceSharp.NativeClockProbe.Tests\ViceSharp.NativeClockProbe.Tests.csproj'
$evidence = Join-Path $RepoRoot 'tests\ViceSharp.NativeClockProbe.Tests\bin\Release\net10.0\bus-probe-evidence'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$summary = [System.Collections.Generic.List[string]]::new()
function Add-Line([string]$Text) { $summary.Add($Text); Write-Information $Text -InformationAction Continue }

$head = (git -C $RepoRoot rev-parse --short=8 HEAD).Trim()
if ($head -ne $ExpectedHead) { throw "HEAD is $head, not $ExpectedHead" }
Add-Line ("PLAN-OBSERVER-001 S1 Windows gates, host {0}, develop {1}, started {2}" -f $env:COMPUTERNAME, $head, (Get-Date -Format s))
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
$g1 = Invoke-Gate 'gate1-busobserver' @('--filter', 'BusObserver')
if (Test-Path -LiteralPath (Join-Path $evidence 'compile.log')) { Copy-Item -LiteralPath (Join-Path $evidence 'compile.log') -Destination (Join-Path $out 'compile-after-gate1.log') }
$g2 = Invoke-Gate 'gate2-full' @()

$ev = Join-Path $out 'bus-probe-evidence'
if (Test-Path -LiteralPath $evidence) {
    Copy-Item -LiteralPath $evidence -Destination $ev -Recurse
    Get-ChildItem -LiteralPath $ev -Recurse -File | ForEach-Object { Add-Line ('evidence {0} bytes={1} sha256={2}' -f $_.FullName.Substring($ev.Length + 1), $_.Length, (Get-FileHash -LiteralPath $_.FullName).Hash) }
}
else { Add-Line "evidence folder missing: $evidence" }
foreach ($f in @(Get-ChildItem -LiteralPath $out -Filter 'compile*.log' -File -Recurse)) {
    $text = [System.IO.File]::ReadAllText($f.FullName)
    Add-Line ('{0}: {1} bytes, {2} lines with warning or error' -f $f.FullName.Substring($out.Length + 1), $f.Length, @($text -split "`n" | Where-Object { $_ -match '(?i)\b(warning|error)\b' }).Count)
}
$after = Save-NativeInventory 'native-inventory-after.txt'
Add-Line ('native/vice inventory before={0} after={1} unchanged={2}' -f $before.Substring(0, 8), $after.Substring(0, 8), ($before -eq $after))
Add-Line ("finished {0}" -f (Get-Date -Format s))
[System.IO.File]::WriteAllLines((Join-Path $out 'summary.txt'), $summary, $utf8)
if ($g1 -ne 0 -or $g2 -ne 0) { exit 1 }
exit 0
