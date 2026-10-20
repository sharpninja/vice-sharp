<#
.SYNOPSIS
PLAN-OBSERVER-001 slice S2b: build the stock VICE objects from the immutable materialization and relink
x64sc and xvic with the external observer wrappers. Canonical script for both hosts; per-host paths only.

.DESCRIPTION
Stages (run in order, each idempotent, each leaving a receipt under the work root):
  Copy      Copy the pristine materialization source to a work tree and prove the copy byte-identical by
            SHA256 over every file, so autogen never writes into the materialization.
  Autogen   Run VICE's autogen.sh in the work tree (autoreconf; generates configure and Makefile.in there).
  Configure Out-of-tree configure in the build tree: headless UI, no sound or media libraries, no docs.
  Make      Build with V=1 and capture the stock link lines of x64sc and xvic from the make log.
  Link      Compile the wrapper translation units (tools/immutable-vice) and rerun each captured link line
            with the wrapper objects and the ld --wrap flags appended, writing <machine>-observer binaries
            to the bin tree; record SHA256, nm of the __wrap_ symbols, and the exact link line.
  Inventory Hash the materialization again and compare with the Copy-stage list: it must be unchanged.
Nothing under native/vice or the materialization source is modified. Generated files live only in the
work, build and bin trees under artifacts/immutable-vice/<sha>/ (gitignored).

.PARAMETER Stage
One of Copy, Autogen, Configure, Make, Link, Inventory, All.
#>
[CmdletBinding()]
param(
    [ValidateSet('Copy', 'Autogen', 'Configure', 'Make', 'Link', 'Inventory', 'All')]
    [string] $Stage = 'All',
    [ValidateSet('x64sc', 'xvic', 'both')]
    [string] $Machine = 'both',
    [string] $Sha = 'cc67418cba58e71d8e70fd353e9de47199ad2075',
    [string] $RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [int] $Jobs = [Environment]::ProcessorCount,
    [string] $CCompiler = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:machineChoice = $Machine
$script:jobCount = $Jobs
$hostTag = if ($IsWindows) { 'windows' } else { 'linux' }
$root = Join-Path $RepoRoot "artifacts/immutable-vice/$Sha"
$source = Join-Path $root 'source/vice'
$work = Join-Path $root "work-$hostTag/vice"
$build = Join-Path $root "build-$hostTag"
$bin = Join-Path $root "bin-$hostTag"
$receipts = Join-Path $root "receipts-$hostTag"
$wrappers = Join-Path $RepoRoot 'tools/immutable-vice'
if (-not $CCompiler) {
    $CCompiler = if ($IsWindows) { 'C:\msys64\mingw64\bin\gcc.exe' } else { '/usr/bin/gcc' }
}
$shell = if ($IsWindows) { 'C:\msys64\usr\bin\bash.exe' } else { '/bin/sh' }
foreach ($dir in @($build, $bin, $receipts)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
if (-not (Test-Path -LiteralPath $source)) { throw "Materialization source missing: $source" }

function Get-TreeHashList {
    param([Parameter(Mandatory)][string] $Root)
    $prefix = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('/', '\') + [IO.Path]::DirectorySeparatorChar
    Get-ChildItem -LiteralPath $Root -Recurse -Force -File |
        ForEach-Object {
            [pscustomobject]@{
                Path = $_.FullName.Substring($prefix.Length).Replace('\', '/')
                Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            }
        } | Sort-Object -Property Path -Culture ([cultureinfo]::InvariantCulture)
}

function Write-Receipt {
    param([Parameter(Mandatory)][string] $Name, [Parameter(Mandatory)] $Object)
    $path = Join-Path $receipts ("{0}-{1}.json" -f (Get-Date -Format 'yyyyMMddTHHmmss'), $Name)
    $Object | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output "receipt: $path"
    return $path
}

function Invoke-Logged {
    param(
        [Parameter(Mandatory)][string] $Executable,
        [string[]] $Arguments = @(),
        [Parameter(Mandatory)][string] $WorkingDirectory,
        [Parameter(Mandatory)][string] $LogPath
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $Executable
    foreach ($a in $Arguments) { [void]$psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $stdout = $p.StandardOutput.ReadToEndAsync()
    $stderr = $p.StandardError.ReadToEndAsync()
    $p.WaitForExit()
    $text = "COMMAND: {0} {1}`nCWD: {2}`nEXIT: {3}`n--- STDOUT`n{4}`n--- STDERR`n{5}" -f $Executable, ($Arguments -join ' '), $WorkingDirectory, $p.ExitCode, $stdout.Result, $stderr.Result
    Set-Content -LiteralPath $LogPath -Value $text -Encoding utf8
    return [pscustomobject]@{ ExitCode = $p.ExitCode; Stdout = $stdout.Result; Stderr = $stderr.Result; Log = $LogPath }
}

function Step-Copy {
    if (-not (Test-Path -LiteralPath $work)) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $work) | Out-Null
        Copy-Item -LiteralPath $source -Destination $work -Recurse -Force
    }
    $sourceList = Get-TreeHashList -Root $source
    $workList = Get-TreeHashList -Root $work
    $workMap = @{}
    foreach ($e in $workList) { $workMap[$e.Path] = $e.Sha256 }
    $missing = @($sourceList | Where-Object { -not $workMap.ContainsKey($_.Path) })
    $different = @($sourceList | Where-Object { $workMap.ContainsKey($_.Path) -and $workMap[$_.Path] -ne $_.Sha256 })
    $listText = ($sourceList | ForEach-Object { "{0} {1}" -f $_.Sha256, $_.Path }) -join "`n"
    $listSha = (Get-FileHash -InputStream ([IO.MemoryStream]::new([Text.Encoding]::UTF8.GetBytes($listText))) -Algorithm SHA256).Hash
    $sourceListPath = Join-Path $receipts 'materialization-hashes.txt'
    Set-Content -LiteralPath $sourceListPath -Value $listText -Encoding utf8
    $result = [pscustomobject]@{
        Stage = 'Copy'; SourceFiles = $sourceList.Count; WorkFilesCoveringSource = $sourceList.Count - $missing.Count
        Missing = @($missing | ForEach-Object { $_.Path }); Different = @($different | ForEach-Object { $_.Path })
        SourceListSha256 = $listSha; SourceListPath = $sourceListPath
    }
    Write-Receipt -Name 'copy' -Object $result | Out-Null
    if ($missing.Count -or $different.Count) { throw "Work tree is not a byte-identical copy of the materialization ($($missing.Count) missing, $($different.Count) different)." }
    Write-Output ("copy verified: {0} files, list SHA256 {1}" -f $sourceList.Count, $listSha)
}

function Step-Autogen {
    $log = Join-Path $receipts 'autogen.log'
    $r = Invoke-Logged -Executable $shell -Arguments @('./autogen.sh') -WorkingDirectory $work -LogPath $log
    Write-Receipt -Name 'autogen' -Object ([pscustomobject]@{ Stage = 'Autogen'; ExitCode = $r.ExitCode; Log = $log; ConfigureExists = (Test-Path (Join-Path $work 'configure')) }) | Out-Null
    if ($r.ExitCode -ne 0 -or -not (Test-Path (Join-Path $work 'configure'))) { throw "autogen failed (exit $($r.ExitCode)); see $log" }
    Write-Output 'autogen ok'
}

function Step-Configure {
    $log = Join-Path $receipts 'configure.log'
    $configureArgs = @(
        (Join-Path $work 'configure'),
        '--enable-headlessui', '--disable-pdf-docs', '--disable-html-docs',
        '--without-pulse', '--without-alsa', '--without-oss', '--without-portaudio',
        '--without-flac', '--without-vorbis', '--without-mpg123', '--without-gif', '--without-lame',
        '--disable-ethernet', '--disable-realdevice', '--disable-x64', '--disable-x64-image'
    )
    $r = Invoke-Logged -Executable $shell -Arguments $configureArgs -WorkingDirectory $build -LogPath $log
    Write-Receipt -Name 'configure' -Object ([pscustomobject]@{ Stage = 'Configure'; ExitCode = $r.ExitCode; Arguments = $configureArgs; Log = $log }) | Out-Null
    if ($r.ExitCode -ne 0) { throw "configure failed (exit $($r.ExitCode)); see $log" }
    Write-Output 'configure ok'
}

function Get-TargetList {
    switch ($script:machineChoice) { 'both' { @('x64sc', 'xvic') } default { @($script:machineChoice) } }
}

function Step-Make {
    $log = Join-Path $receipts 'make.log'
    $targets = Get-TargetList
    $r = Invoke-Logged -Executable 'make' -Arguments (@('-C', 'src', "-j$($script:jobCount)", 'V=1') + $targets) -WorkingDirectory $build -LogPath $log
    $lines = ($r.Stdout -split "`n")
    $linkLines = @{}
    foreach ($t in $targets) {
        $hit = @($lines | Where-Object { $_ -match "\s-o\s+$t(\s|$)" -and $_ -notmatch '^\s*(libtool|ar\s)' }) | Select-Object -Last 1
        if ($hit) { $linkLines[$t] = $hit.Trim() }
    }
    $linkPath = Join-Path $receipts 'stock-link-lines.json'
    $linkLines | ConvertTo-Json | Set-Content -LiteralPath $linkPath -Encoding utf8
    Write-Receipt -Name 'make' -Object ([pscustomobject]@{ Stage = 'Make'; ExitCode = $r.ExitCode; Targets = $targets; LinkLinesFound = @($linkLines.Keys); Log = $log; LinkLines = $linkPath }) | Out-Null
    if ($r.ExitCode -ne 0) { throw "make failed (exit $($r.ExitCode)); see $log" }
    foreach ($t in $targets) { if (-not $linkLines.ContainsKey($t)) { throw "No link line captured for $t; see $log" } }
    Write-Output ("make ok; link lines captured for {0}" -f ($linkLines.Keys -join ', '))
}

function Step-Link {
    $linkPath = Join-Path $receipts 'stock-link-lines.json'
    if (-not (Test-Path -LiteralPath $linkPath)) { throw "Run the Make stage first: $linkPath missing" }
    $linkLines = Get-Content -LiteralPath $linkPath -Raw | ConvertFrom-Json
    $results = @()
    foreach ($t in Get-TargetList) {
        $cycleHook = if ($t -eq 'x64sc') { 'clock-observer-x64sc.c' } else { 'clock-observer.c' }
        $cycleSymbol = if ($t -eq 'x64sc') { 'vicii_cycle' } else { 'vic_cycle' }
        $defines = if ($t -eq 'x64sc') { @('-DVICE_SHARP_ROWS=32') } else { @() }
        $objDir = Join-Path $build "observer-obj/$t"
        New-Item -ItemType Directory -Force -Path $objDir | Out-Null
        $objects = @()
        foreach ($unit in @($cycleHook, 'clock-gate.c', 'gate-observer.c', 'bus-observer.c', 'bus-adapter.c')) {
            $obj = Join-Path $objDir ([IO.Path]::ChangeExtension($unit, '.o'))
            $cl = Invoke-Logged -Executable $CCompiler -Arguments (@('-std=c11', '-Wall', '-Wextra', '-Werror', '-O2', '-g') + $defines + @('-c', (Join-Path $wrappers $unit), '-o', $obj)) -WorkingDirectory $objDir -LogPath (Join-Path $receipts "compile-$t-$([IO.Path]::GetFileNameWithoutExtension($unit)).log")
            if ($cl.ExitCode -ne 0) { throw "wrapper compile failed for $unit ($t): $($cl.Stderr)" }
            $objects += $obj
        }
        $wraps = @($cycleSymbol, 'mem_initialize_memory', 'mem_mmu_translate', 'mem_toggle_watchpoints', 'vice_sharp_observe_clock') | ForEach-Object { "-Wl,--wrap=$_" }
        $output = Join-Path $bin "$t-observer"
        $stock = [string]$linkLines.$t
        $relink = $stock -replace "\s-o\s+$t(\s|$)", (" -o " + $output + '$1')
        $relink = $relink + ' ' + (($objects + $wraps) -join ' ')
        $log = Join-Path $receipts "link-$t.log"
        $r = Invoke-Logged -Executable $shell -Arguments @('-c', $relink) -WorkingDirectory (Join-Path $build 'src') -LogPath $log
        if ($r.ExitCode -ne 0) { throw "relink failed for $t (exit $($r.ExitCode)); see $log" }
        $nm = Invoke-Logged -Executable 'nm' -Arguments @($output) -WorkingDirectory $bin -LogPath (Join-Path $receipts "nm-$t.log")
        $wrapSymbols = @(($nm.Stdout -split "`n") | Where-Object { $_ -match '__wrap_|__real_|vice_sharp_' } | ForEach-Object { $_.Trim() })
        $results += [pscustomobject]@{
            Machine = $t; Output = $output; Sha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
            StockLinkLine = $stock; RelinkLine = $relink; WrapperObjects = $objects; Wraps = $wraps; WrapSymbols = $wrapSymbols
        }
        Write-Output ("linked {0}: {1}" -f $t, $output)
    }
    Write-Receipt -Name 'link' -Object ([pscustomobject]@{ Stage = 'Link'; Compiler = $CCompiler; Results = $results }) | Out-Null
}

function Step-Inventory {
    $listPath = Join-Path $receipts 'materialization-hashes.txt'
    if (-not (Test-Path -LiteralPath $listPath)) { throw "Run the Copy stage first: $listPath missing" }
    $before = Get-Content -LiteralPath $listPath
    $after = Get-TreeHashList -Root $source | ForEach-Object { "{0} {1}" -f $_.Sha256, $_.Path }
    $same = ($before.Count -eq $after.Count) -and (@(Compare-Object -ReferenceObject $before -DifferenceObject $after).Count -eq 0)
    Write-Receipt -Name 'inventory' -Object ([pscustomobject]@{ Stage = 'Inventory'; Before = $before.Count; After = $after.Count; MaterializationUnchanged = $same }) | Out-Null
    if (-not $same) { throw 'Materialization source changed during the build; this is a finding, not a pass.' }
    Write-Output ("materialization unchanged: {0} files" -f $after.Count)
}

$stages = if ($Stage -eq 'All') { @('Copy', 'Autogen', 'Configure', 'Make', 'Link', 'Inventory') } else { @($Stage) }
foreach ($s in $stages) {
    Write-Output ("=== {0} ({1}) ===" -f $s, (Get-Date -Format 'yyyy-MM-ddTHH:mm:ss'))
    & "Step-$s"
}
