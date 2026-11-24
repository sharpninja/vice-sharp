#Requires -Version 7.4
<#
.SYNOPSIS
    MSYS2 rerun of the PLAN-OBSERVER-001 Windows fixture gates on PAYTON-LEGION2 at develop 8c426015 (forward-slash paths; rerun of 131b9394), after
    CompiledCFixture began compiling inside the MSYS2 MINGW64 login shell (Payton 2026-10-20 08:07 "You should always
    use msys to compile native VICE code", 08:14 "Fixtures too"). Request from OMAR-VICE# 08:20: "rerun every Windows
    gate (expect --filter BusAdapter 8 with broken-stderr 72, full project 232) and confirm compile.log shows
    /mingw64/bin/gcc and MSYSTEM=MINGW64".
      Gate 1: --filter BusObserver   Gate 2: --filter BusAdapter   Gate 3: the full ViceSharp.NativeClockProbe.Tests
    Every compile.log is checked for "resolved compiler: /mingw64/bin/gcc", "MSYSTEM=MINGW64", ExitCode 0, no warning
    or error lines, and whether its Executable and Arguments use backslashes (lab rule, Payton 08:19: forward slashes
    on Windows). Paths in this script use forward slashes. native/vice git status is recorded before and after.
    Writes TRX files, logs, compile logs (executables excluded) and summary.txt into this folder.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'F:/GitHub/vice-sharp',
    [string]$ExpectedHead = '8c426015'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = $PSScriptRoot -replace '\\', '/'
$proj = "$RepoRoot/tests/ViceSharp.NativeClockProbe.Tests/ViceSharp.NativeClockProbe.Tests.csproj"
$bin = "$RepoRoot/tests/ViceSharp.NativeClockProbe.Tests/bin/Release/net10.0"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$summary = [System.Collections.Generic.List[string]]::new()
function Add-Line([string]$Text) { $summary.Add($Text); Write-Information $Text -InformationAction Continue }

$head = (git -C $RepoRoot rev-parse --short=8 HEAD).Trim()
if ($head -ne $ExpectedHead) { throw "HEAD is $head, not $ExpectedHead" }
Add-Line ("MSYS2 fixture rerun, host {0}, develop {1}, started {2}; dotnet {3}" -f $env:COMPUTERNAME, $head, (Get-Date -Format s), (dotnet --version))

function Save-NativeInventory([string]$Name) {
    $nv = "$RepoRoot/native/vice"
    $lines = if (Test-Path -LiteralPath "$nv/.git") { @(git -c color.ui=never -c core.quotepath=off -C $nv status --porcelain=v1 --untracked-files=all) } else { @("native/vice is not a git checkout: $(Test-Path -LiteralPath $nv)") }
    [System.IO.File]::WriteAllLines("$out/$Name", [string[]]$lines, $utf8)
    (Get-FileHash -LiteralPath "$out/$Name").Hash
}
$before = Save-NativeInventory 'native-inventory-before.txt'

# Fresh evidence: stale compile logs from the pre-MSYS2 runs must not be mistaken for this run's.
if (Test-Path -LiteralPath $bin) {
    foreach ($e in @(Get-ChildItem -LiteralPath $bin -Directory -Filter '*-evidence')) { Remove-Item -LiteralPath $e.FullName -Recurse -Force }
}
dotnet build $proj -c Release -nologo 2>&1 | Out-File -LiteralPath "$out/build.log" -Encoding utf8NoBOM
$buildExit = $LASTEXITCODE
Add-Line "build exit=$buildExit"
if ($buildExit -ne 0) { throw 'build failed; see build.log' }

function Invoke-Gate([string]$Name, [string[]]$Extra) {
    $trx = "$Name.trx"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet test $proj -c Release --no-build --logger "trx;LogFileName=$trx" --results-directory $out @Extra 2>&1 | Out-File -LiteralPath "$out/$Name.log" -Encoding utf8NoBOM
    $exit = $LASTEXITCODE
    $sw.Stop()
    [xml]$x = Get-Content -LiteralPath "$out/$trx" -Raw
    $c = $x.TestRun.ResultSummary.Counters
    Add-Line ('{0}: exit={1} total={2} executed={3} passed={4} failed={5} notExecuted={6} outcome={7} seconds={8:N1}' -f $Name, $exit, $c.total, $c.executed, $c.passed, $c.failed, $c.notExecuted, $x.TestRun.ResultSummary.outcome, $sw.Elapsed.TotalSeconds)
    $ns = @{ t = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }
    foreach ($f in @(Select-Xml -Xml $x -XPath '//t:UnitTestResult[@outcome="Failed"]' -Namespace $ns)) {
        Add-Line ('  failed: {0} :: {1}' -f $f.Node.testName, ($f.Node.Output.ErrorInfo.Message -replace '\s+', ' '))
    }
    $exit
}
$g1 = Invoke-Gate 'gate1-busobserver' @('--filter', 'BusObserver')
$g2 = Invoke-Gate 'gate2-busadapter' @('--filter', 'BusAdapter')
$g3 = Invoke-Gate 'gate3-full' @()

foreach ($e in @(Get-ChildItem -LiteralPath $bin -Directory -Filter '*-evidence')) {
    $dest = "$out/$($e.Name)"
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Get-ChildItem -LiteralPath $e.FullName -File | Where-Object Extension -NE '.exe' | Copy-Item -Destination $dest
    $cl = "$dest/compile.log"
    if (-not (Test-Path -LiteralPath $cl)) { Add-Line "$($e.Name): compile.log missing"; continue }
    $lines = [System.IO.File]::ReadAllLines($cl)
    $exe = (@($lines | Where-Object { $_ -like 'Executable:*' }) -join '')
    $argsLine = (@($lines | Where-Object { $_ -like 'Arguments:*' }) -join '')
    # Arguments are logged as "a | b | c". The element after -lc is the bash script, whose printf \n escapes are
    # backslashes by design; every other element (the gcc arguments) must be backslash-free.
    $argItems = @(($argsLine -replace '^Arguments:\s*', '') -split ' \| ')
    $scriptIndex = [array]::IndexOf($argItems, '-lc') + 1
    $pathItems = @(for ($i = 0; $i -lt $argItems.Count; $i++) { if ($i -ne $scriptIndex) { $argItems[$i] } })
    $argsLine = $pathItems -join ' | '
    Add-Line ('{0}: {1}; resolved /mingw64/bin/gcc={2}; MSYSTEM=MINGW64={3}; warning/error lines={4}; -Werror={5}; backslash in Executable={6}; backslash in Arguments={7}' -f $e.Name,
        (@($lines | Where-Object { $_ -like 'ExitCode:*' }) -join '').Trim(),
        [bool]@($lines | Where-Object { $_ -eq 'resolved compiler: /mingw64/bin/gcc' }).Count,
        [bool]@($lines | Where-Object { $_ -eq 'MSYSTEM=MINGW64' }).Count,
        @($lines | Where-Object { $_ -match '(?i)\b(warning|error)\b' }).Count,
        ($argsLine -match '-Werror'), ($exe -match '\\'), ($argsLine -match '\\'))
}
$after = Save-NativeInventory 'native-inventory-after.txt'
Add-Line ('native/vice inventory before={0} after={1} unchanged={2}' -f $before.Substring(0, 8), $after.Substring(0, 8), ($before -eq $after))
Add-Line ("finished {0}" -f (Get-Date -Format s))
[System.IO.File]::WriteAllLines("$out/summary.txt", $summary, $utf8)
if ($g1 -ne 0 -or $g2 -ne 0 -or $g3 -ne 0) { exit 1 }
exit 0
