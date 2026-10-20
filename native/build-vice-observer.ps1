<#
.SYNOPSIS
PLAN-OBSERVER-001 slice S2b: build the stock VICE objects from the immutable materialization and relink
x64sc and xvic with the external observer wrappers. Canonical script for both hosts; per-host paths only.

.DESCRIPTION
Stages (run in order, each idempotent, each leaving a receipt under the receipts tree):
  Copy      Copy the pristine materialization source to a work tree and prove the copy byte-identical by
            SHA256 over every file, so autogen never writes into the materialization.
  Autogen   Run VICE's autogen.sh in the work tree (autoreconf; generates configure and Makefile.in there).
  Configure Out-of-tree configure in the build tree: headless UI, no sound or media libraries, no docs.
  Make      Build the x64sc and xvic programs with V=1 and capture their stock link lines from the log.
  Link      Compile the wrapper translation units (tools/immutable-vice) and rerun each captured link line
            with the wrapper objects and the ld --wrap flags appended, writing <machine>-observer binaries
            to the bin tree; record SHA256, nm of the wrap symbols, and the exact link line.
  Inventory Hash the materialization again and compare with the Copy-stage list: it must be unchanged.
Every native tool (sh, autoreconf, configure, make, gcc, nm) runs through the platform shell: /bin/sh on
Linux, and on Windows the MSYS2 MINGW64 login shell (Payton directive 2026-10-20T08:07: "always use msys
to compile native VICE code"), never gcc.exe called directly. Nothing under native/vice or the
materialization source is modified; generated files live only in the work, build, bin and receipts trees
under artifacts/immutable-vice/<sha>/ (gitignored).

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
    [string] $MsysRoot = 'C:\msys64'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:machineChoice = $Machine
$script:jobCount = $Jobs
$script:msysRootPath = $MsysRoot
$hostTag = if ($IsWindows) { 'windows' } else { 'linux' }
$exeExt = if ($IsWindows) { '.exe' } else { '' }
$root = Join-Path $RepoRoot "artifacts/immutable-vice/$Sha"
$source = Join-Path $root 'source/vice'
$work = Join-Path $root "work-$hostTag/vice"
$build = Join-Path $root "build-$hostTag"
$bin = Join-Path $root "bin-$hostTag"
$receipts = Join-Path $root "receipts-$hostTag"
$wrappers = Join-Path $RepoRoot 'tools/immutable-vice'
foreach ($dir in @($build, $bin, $receipts)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
if (-not (Test-Path -LiteralPath $source)) { throw "Materialization source missing: $source" }

# Lab rule (Payton 2026-10-20T08:19): forward slashes on Windows for every path handed to the shell (C:/...).
function ConvertTo-ShellPath {
    param([Parameter(Mandatory)][string] $Path)
    if (-not $IsWindows) { return $Path }
    return ([IO.Path]::GetFullPath($Path) -replace '\\', '/')
}

# Paths handed to the autotools scripts must be MSYS POSIX form (/f/GitHub/...): the generated configure splits
# its aux-dir candidates on ":" (configure 3490 and 3500 to 3502 under IFS=$PATH_SEPARATOR) and the generated
# Makefiles carry srcdir and VPATH, so a drive colon breaks both (LEGION2 finding 2026-10-20T10:13). Forward
# slashes throughout, per the lab rule; gcc arguments keep the C:/ form, which native gcc understands.
function ConvertTo-PosixPath {
    param([Parameter(Mandatory)][string] $Path)
    $forward = ConvertTo-ShellPath $Path
    if ($IsWindows -and $forward -match '^([A-Za-z]):/(.*)$') { return ('/{0}/{1}' -f $Matches[1].ToLowerInvariant(), $Matches[2]) }
    return $forward
}

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

# Runs one shell command line through the platform shell and logs command, cwd, exit and both streams.
function Invoke-Native {
    param(
        [Parameter(Mandatory)][string] $Command,
        [Parameter(Mandatory)][string] $WorkingDirectory,
        [Parameter(Mandatory)][string] $LogPath
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    if ($IsWindows) {
        # Lab rule (Payton 2026-10-20T08:19): forward slashes on Windows.
        $psi.FileName = ($script:msysRootPath -replace '\\', '/').TrimEnd('/') + '/usr/bin/bash.exe'
        [void]$psi.ArgumentList.Add('-lc')
        [void]$psi.ArgumentList.Add($Command)
        $psi.Environment['MSYSTEM'] = 'MINGW64'
        $psi.Environment['CHERE_INVOKING'] = '1'
        $psi.Environment['MSYS2_PATH_TYPE'] = 'minimal'
    } else {
        $psi.FileName = '/bin/sh'
        [void]$psi.ArgumentList.Add('-c')
        [void]$psi.ArgumentList.Add($Command)
    }
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $stdout = $p.StandardOutput.ReadToEndAsync()
    $stderr = $p.StandardError.ReadToEndAsync()
    $p.WaitForExit()
    $text = "SHELL: {0}`nCOMMAND: {1}`nCWD: {2}`nEXIT: {3}`n--- STDOUT`n{4}`n--- STDERR`n{5}" -f $psi.FileName, $Command, $WorkingDirectory, $p.ExitCode, $stdout.Result, $stderr.Result
    Set-Content -LiteralPath $LogPath -Value $text -Encoding utf8
    return [pscustomobject]@{ ExitCode = $p.ExitCode; Stdout = $stdout.Result; Stderr = $stderr.Result; Log = $LogPath }
}

# The copy set is the committed tracked-file list of the materialization (3,847 entries, SHA256|size|path,
# proven byte-identical on both hosts, docs/receipts/native-vice/20261019T202542-linux-materialization/compare/),
# restricted to the vice/ subtree: on PAYTON-LEGION2 the materialization is configured and built in place, so a
# whole-folder copy would carry config.status into the work tree and the generated configure refuses an
# out-of-tree build when $srcdir/config.status exists (LEGION2 finding 2026-10-20T10:08). Every copied file is
# verified against the list's SHA256; a work tree that already holds config.status is refused, never cleaned.
$script:trackedListPath = Join-Path $RepoRoot 'docs/receipts/native-vice/20261019T202542-linux-materialization/compare/omarchy-tracked-files.txt'

function Get-TrackedViceList {
    if (-not (Test-Path -LiteralPath $script:trackedListPath)) { throw "Tracked-file list missing: $script:trackedListPath" }
    $entries = foreach ($line in Get-Content -LiteralPath $script:trackedListPath) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $sha, $size, $path = $line -split '\|', 3
        if ($path.StartsWith('vice/')) { [pscustomobject]@{ Sha256 = $sha.ToUpperInvariant(); Size = [long]$size; Path = $path.Substring(5) } }
    }
    return @($entries | Sort-Object -Property Path -Culture ([cultureinfo]::InvariantCulture))
}

function Step-Copy {
    $tracked = Get-TrackedViceList
    if (Test-Path -LiteralPath (Join-Path $work 'config.status')) {
        throw "Work tree $work holds config.status (an in-tree configure); the out-of-tree configure would refuse it. Remove or rename the work tree (operator decision), then rerun Copy."
    }
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    $copied = 0
    $missingSource = @()
    $different = @()
    foreach ($e in $tracked) {
        $src = Join-Path $source $e.Path
        $dst = Join-Path $work $e.Path
        if (-not (Test-Path -LiteralPath $src)) { $missingSource += $e.Path; continue }
        if (-not (Test-Path -LiteralPath $dst)) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
            Copy-Item -LiteralPath $src -Destination $dst -Force
            $copied++
        }
        $hash = (Get-FileHash -LiteralPath $dst -Algorithm SHA256).Hash
        if ($hash -ne $e.Sha256) { $different += $e.Path }
    }
    $listText = ($tracked | ForEach-Object { "{0} {1}" -f $_.Sha256, $_.Path }) -join "`n"
    $listSha = (Get-FileHash -InputStream ([IO.MemoryStream]::new([Text.Encoding]::UTF8.GetBytes($listText))) -Algorithm SHA256).Hash
    $sourceListPath = Join-Path $receipts 'materialization-hashes.txt'
    Set-Content -LiteralPath $sourceListPath -Value $listText -Encoding utf8
    $result = [pscustomobject]@{
        Stage = 'Copy'; TrackedViceFiles = $tracked.Count; CopiedNow = $copied; TrackedList = $script:trackedListPath
        MissingInSource = $missingSource; DifferentInWork = $different; SourceListSha256 = $listSha; SourceListPath = $sourceListPath
    }
    Write-Receipt -Name 'copy' -Object $result | Out-Null
    if ($missingSource.Count -or $different.Count) { throw "Work tree does not match the tracked list ($($missingSource.Count) missing in the materialization, $($different.Count) different in the work tree)." }
    Write-Output ("copy verified: {0} tracked vice/ files ({1} copied now), list SHA256 {2}" -f $tracked.Count, $copied, $listSha)
}

function Step-Autogen {
    $log = Join-Path $receipts 'autogen.log'
    $r = Invoke-Native -Command './autogen.sh' -WorkingDirectory $work -LogPath $log
    Write-Receipt -Name 'autogen' -Object ([pscustomobject]@{ Stage = 'Autogen'; ExitCode = $r.ExitCode; Log = $log; ConfigureExists = (Test-Path (Join-Path $work 'configure')) }) | Out-Null
    if ($r.ExitCode -ne 0 -or -not (Test-Path (Join-Path $work 'configure'))) { throw "autogen failed (exit $($r.ExitCode)); see $log" }
    Write-Output 'autogen ok'
}

function Step-Configure {
    $log = Join-Path $receipts 'configure.log'
    $flags = @(
        '--enable-headlessui', '--disable-pdf-docs', '--disable-html-docs',
        '--without-pulse', '--without-alsa', '--without-oss', '--without-portaudio',
        '--without-flac', '--without-vorbis', '--without-mpg123', '--without-gif', '--without-lame',
        '--disable-ethernet', '--disable-realdevice', '--disable-x64', '--disable-x64-image'
    )
    $configurePath = ConvertTo-PosixPath (Join-Path $work 'configure')
    if ($configurePath.Contains(':')) { throw "configure path must not contain a drive colon (autotools split on it): $configurePath" }
    $command = ('"{0}" {1}' -f $configurePath, ($flags -join ' '))
    $r = Invoke-Native -Command $command -WorkingDirectory $build -LogPath $log
    Write-Receipt -Name 'configure' -Object ([pscustomobject]@{ Stage = 'Configure'; ExitCode = $r.ExitCode; Command = $command; Log = $log }) | Out-Null
    if ($r.ExitCode -ne 0) { throw "configure failed (exit $($r.ExitCode)); see $log" }
    Write-Output 'configure ok'
}

function Get-TargetList {
    switch ($script:machineChoice) { 'both' { @('x64sc', 'xvic') } default { @($script:machineChoice) } }
}

function Step-Make {
    $log = Join-Path $receipts 'make.log'
    $targets = Get-TargetList
    $programs = @($targets | ForEach-Object { $_ + $exeExt })
    # The per-program targets do not depend on the bundled libraries under src/lib (liblinenoiseng.a is
    # only built by the lib subdirectory recursion), so build src/lib first, then the programs.
    $command = ('make -C src/lib -j{0} V=1 && make -C src -j{0} V=1 {1}' -f $script:jobCount, ($programs -join ' '))
    $r = Invoke-Native -Command $command -WorkingDirectory $build -LogPath $log
    $lines = ($r.Stdout -split "`n")
    $linkLines = @{}
    foreach ($t in $targets) {
        $program = [regex]::Escape($t + $exeExt)
        $hit = @($lines | Where-Object { $_ -match "\s-o\s+$program(\s|$)" -and $_ -notmatch '^\s*(libtool|ar\s)' }) | Select-Object -Last 1
        if ($hit) { $linkLines[$t] = $hit.Trim() }
    }
    $linkPath = Join-Path $receipts 'stock-link-lines.json'
    $linkLines | ConvertTo-Json | Set-Content -LiteralPath $linkPath -Encoding utf8
    Write-Receipt -Name 'make' -Object ([pscustomobject]@{ Stage = 'Make'; ExitCode = $r.ExitCode; Command = $command; LinkLinesFound = @($linkLines.Keys); Log = $log; LinkLines = $linkPath }) | Out-Null
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
        $defines = if ($t -eq 'x64sc') { '-DVICE_SHARP_ROWS=32' } else { '' }
        $objDir = Join-Path $build "observer-obj/$t"
        New-Item -ItemType Directory -Force -Path $objDir | Out-Null
        $objects = @()
        # The machine glue (obs-fields-*.c) reads VICE globals through the real headers of the work tree and
        # the configured config.h of the build tree; every other unit is header-free.
        $includes = @((Join-Path $work 'src'), (Join-Path $build 'src'), (Join-Path $work 'src/vic20'), (Join-Path $work 'src/raster'), (Join-Path $work 'src/viciisc')) |
            ForEach-Object { '-I"' + (ConvertTo-ShellPath $_) + '"' }
        $units = @($cycleHook, 'clock-gate.c', 'gate-observer.c', 'bus-observer.c', 'bus-adapter.c', 'obs-emitter.c', "obs-fields-$t.c")
        foreach ($unit in $units) {
            $obj = Join-Path $objDir ([IO.Path]::ChangeExtension($unit, '.o'))
            $unitIncludes = if ($unit -like 'obs-fields-*') { ($includes -join ' ') } else { '' }
            $command = ('gcc -std=c11 -Wall -Wextra -Werror -O2 -g {0} {1} -c "{2}" -o "{3}"' -f $defines, $unitIncludes, (ConvertTo-ShellPath (Join-Path $wrappers $unit)), (ConvertTo-ShellPath $obj))
            $cl = Invoke-Native -Command $command -WorkingDirectory $objDir -LogPath (Join-Path $receipts "compile-$t-$([IO.Path]::GetFileNameWithoutExtension($unit)).log")
            if ($cl.ExitCode -ne 0) { throw "wrapper compile failed for $unit ($t): $($cl.Stderr)" }
            $objects += (ConvertTo-ShellPath $obj)
        }
        $wrapSymbols = @($cycleSymbol, 'mem_initialize_memory', 'mem_mmu_translate', 'mem_toggle_watchpoints', 'vice_sharp_observe_clock')
        if ($t -eq 'x64sc') { $wrapSymbols += 'vicii_steal_cycles' }
        $wraps = $wrapSymbols | ForEach-Object { "-Wl,--wrap=$_" }
        $output = Join-Path $bin ("{0}-observer{1}" -f $t, $exeExt)
        $stock = [string]$linkLines.$t
        $program = [regex]::Escape($t + $exeExt)
        # The wrapper objects go directly after "-o <output>", ahead of every stock object and archive: ld pulls
        # archive members only for references already seen, so __real_<symbol> (resolved to the stock symbol
        # inside an archive) must be referenced before that archive is scanned. The --wrap flags can go last.
        $quotedObjects = ($objects | ForEach-Object { '"' + $_ + '"' }) -join ' '
        $relink = $stock -replace "\s-o\s+$program(\s|$)", (' -o "' + (ConvertTo-ShellPath $output) + '" ' + $quotedObjects + '$1')
        $relink = $relink + ' ' + ($wraps -join ' ')
        $log = Join-Path $receipts "link-$t.log"
        $r = Invoke-Native -Command $relink -WorkingDirectory (Join-Path $build 'src') -LogPath $log
        if ($r.ExitCode -ne 0) { throw "relink failed for $t (exit $($r.ExitCode)); see $log" }
        $nm = Invoke-Native -Command ('nm "{0}"' -f (ConvertTo-ShellPath $output)) -WorkingDirectory $bin -LogPath (Join-Path $receipts "nm-$t.log")
        $wrapSymbols = @(($nm.Stdout -split "`n") | Where-Object { $_ -match '__wrap_|__real_|vice_sharp_' } | ForEach-Object { $_.Trim() })
        $results += [pscustomobject]@{
            Machine = $t; Output = $output; Sha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
            StockLinkLine = $stock; RelinkLine = $relink; WrapperObjects = $objects; Wraps = $wraps; WrapSymbols = $wrapSymbols
        }
        Write-Output ("linked {0}: {1}" -f $t, $output)
    }
    Write-Receipt -Name 'link' -Object ([pscustomobject]@{ Stage = 'Link'; Host = $hostTag; Results = $results }) | Out-Null
}

function Step-Inventory {
    $listPath = Join-Path $receipts 'materialization-hashes.txt'
    if (-not (Test-Path -LiteralPath $listPath)) { throw "Run the Copy stage first: $listPath missing" }
    $beforeMap = @{}
    foreach ($line in Get-Content -LiteralPath $listPath) { $sha, $path = $line -split ' ', 2; $beforeMap[$path] = $sha }
    $afterList = Get-TreeHashList -Root $source
    $afterMap = @{}
    foreach ($e in $afterList) { $afterMap[$e.Path] = $e.Sha256 }
    $changed = @($beforeMap.Keys | Where-Object { $afterMap.ContainsKey($_) -and $afterMap[$_] -ne $beforeMap[$_] } | Sort-Object)
    $removed = @($beforeMap.Keys | Where-Object { -not $afterMap.ContainsKey($_) } | Sort-Object)
    $added = @($afterMap.Keys | Where-Object { -not $beforeMap.ContainsKey($_) } | Sort-Object)
    # Changed or removed original files are a failure. Added files are reported by path and left in place for
    # the operator's disposition (a plugin hook writing session state into a drifted working directory is the
    # known cause, not the build); they are a finding, not a pass, and are never deleted here.
    $unchanged = ($changed.Count -eq 0 -and $removed.Count -eq 0)
    Write-Receipt -Name 'inventory' -Object ([pscustomobject]@{ Stage = 'Inventory'; Before = $beforeMap.Count; After = $afterList.Count; OriginalFilesUnchanged = $unchanged; Changed = $changed; Removed = $removed; Added = $added }) | Out-Null
    if (-not $unchanged) { throw ('Materialization source changed during the build ({0} changed, {1} removed); this is a finding, not a pass.' -f $changed.Count, $removed.Count) }
    if ($added.Count) { Write-Output ('FINDING: {0} file(s) added to the materialization, left for disposition: {1}' -f $added.Count, ($added -join ', ')) }
    Write-Output ("original materialization files unchanged: {0}" -f $beforeMap.Count)
}

$stages = if ($Stage -eq 'All') { @('Copy', 'Autogen', 'Configure', 'Make', 'Link', 'Inventory') } else { @($Stage) }
foreach ($s in $stages) {
    Write-Output ("=== {0} ({1}) ===" -f $s, (Get-Date -Format 'yyyy-MM-ddTHH:mm:ss'))
    & "Step-$s"
}
