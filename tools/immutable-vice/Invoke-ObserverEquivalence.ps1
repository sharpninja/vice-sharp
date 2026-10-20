<#
.SYNOPSIS
PLAN-OBSERVER-001 slice S3: equivalence runs of the observer binaries against the stock binaries.

.DESCRIPTION
For each machine (xvic, x64sc) and each cycle count, runs three headless, deterministic processes on the
ROM-only reset workload (no media, VICE data directory from the hash-verified work tree, log colors off
because the stock headless logger dereferences a null text when stdout is not a terminal and colors are on,
dummy sound device, cycle limit, exit screenshot):
  stock     the stock binary from the Make stage (no wrappers);
  withheld  the observer binary with the bank-base fast path withheld (default);
  keep      the observer binary with VICESHARP_FAST_PATH=keep (translate forwarded unchanged).
The observer runs are gated: a permit stream of 'c' bytes feeds stdin, one per cycle. Evidence per run:
exit code, SHA256 of the exit screenshot, counts of VICE_GATE, VICE_BUS access, access with kind missing,
missing and foreign reports, translate events, STOP lines, and SHA256 of stdout and stderr.
Equivalence claims checked here: exit screenshots byte-identical across the three runs; VICE_GATE counts
equal between withheld and keep; no STOP, no missing or foreign report, no access with kind missing in the
withheld run. Any difference is reported, never tolerated. Nothing under the materialization is touched.
#>
[CmdletBinding()]
param(
    [ValidateSet('x64sc', 'xvic', 'both')]
    [string] $Machine = 'both',
    [int[]] $Cycles = @(100000, 1000000),
    [string] $Sha = 'cc67418cba58e71d8e70fd353e9de47199ad2075',
    [string] $RepoRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string] $OutDir = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$hostTag = if ($IsWindows) { 'windows' } else { 'linux' }
$exeExt = if ($IsWindows) { '.exe' } else { '' }
$root = Join-Path $RepoRoot "artifacts/immutable-vice/$Sha"
$data = Join-Path $root "work-$hostTag/vice/data"
$buildSrc = Join-Path $root "build-$hostTag/src"
$bin = Join-Path $root "bin-$hostTag"
if (-not $OutDir) { $OutDir = Join-Path $root ("equivalence-$hostTag/" + (Get-Date -Format 'yyyyMMddTHHmmss')) }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$machines = if ($Machine -eq 'both') { @('xvic', 'x64sc') } else { @($Machine) }

function Get-Sha256 { param([string] $Path) if (Test-Path -LiteralPath $Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } else { $null } }

function Invoke-Run {
    param(
        [string] $Label, [string] $Executable, [string] $MachineName, [int] $CycleCount,
        [bool] $Gated, [bool] $Keep
    )
    $prefix = Join-Path $OutDir ("{0}-{1}-{2}" -f $MachineName, $CycleCount, $Label)
    $screenshot = "$prefix.png"
    $stdoutPath = "$prefix.stdout"
    $stderrPath = "$prefix.stderr"
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $Executable
    # -seed 1: VICE seeds its RAM-init random bits (RAMInitRandomChance, ram.c 321, lib_unsigned_rand) from
    # time(NULL) unless -seed is given (lib.c 1045; main.c 298). Without it two runs read different
    # uninitialized bytes on dummy stack reads (observed 2026-10-20T08:56: 9 of 47,815 keep-run accesses on
    # xvic 100k and 99 of 43,503 on x64sc differed in the data byte only). The seed is part of the workload.
    foreach ($a in @('-default', '-seed', '1', '+logcolorize', '-sounddev', 'dummy', '-directory', $data, '-limitcycles', "$CycleCount", '-exitscreenshot', $screenshot)) { [void]$psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $buildSrc
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    if ($Keep) { $psi.Environment['VICESHARP_FAST_PATH'] = 'keep' } else { $psi.Environment.Remove('VICESHARP_FAST_PATH') | Out-Null }
    $p = [System.Diagnostics.Process]::Start($psi)
    $outFile = [IO.File]::Create($stdoutPath)
    $errFile = [IO.File]::Create($stderrPath)
    $outTask = $p.StandardOutput.BaseStream.CopyToAsync($outFile)
    $errTask = $p.StandardError.BaseStream.CopyToAsync($errFile)
    if ($Gated) {
        # One permit byte per cycle plus slack; the process exits at the cycle limit and stops reading.
        $permit = [byte[]]::new($CycleCount * 2 + 4096)
        for ($i = 0; $i -lt $permit.Length; $i++) { $permit[$i] = 99 }
        # The process stops reading at its cycle limit and may exit before the slack is written; that is expected.
        try { $p.StandardInput.BaseStream.Write($permit, 0, $permit.Length); $p.StandardInput.BaseStream.Flush() } catch [IO.IOException] { Write-Verbose ("permit stream closed early by {0}: {1}" -f $Label, $_.Exception.Message) }
    }
    try { $p.StandardInput.Close() } catch [IO.IOException] { Write-Verbose ("stdin close after exit for {0}: {1}" -f $Label, $_.Exception.Message) }
    if (-not $p.WaitForExit(1800000)) { $p.Kill($true); throw "$Label timed out; not a passing run" }
    [void]$outTask.Wait(60000); [void]$errTask.Wait(60000)
    $outTask.Dispose(); $errTask.Dispose()
    $outFile.Dispose(); $errFile.Dispose()
    $stderrLines = [IO.File]::ReadAllLines($stderrPath)
    [pscustomobject]@{
        Label = $Label; Machine = $MachineName; Cycles = $CycleCount; Executable = $Executable; ExitCode = $p.ExitCode
        ScreenshotSha256 = Get-Sha256 $screenshot
        StdoutSha256 = Get-Sha256 $stdoutPath; StderrSha256 = Get-Sha256 $stderrPath
        GateLines = @($stderrLines | Where-Object { $_.StartsWith('VICE_GATE,') }).Count
        AccessLines = @($stderrLines | Where-Object { $_.StartsWith('VICE_BUS,access,') }).Count
        AccessMissingKind = @($stderrLines | Where-Object { $_.StartsWith('VICE_BUS,access,missing,') }).Count
        MissingOrForeign = @($stderrLines | Where-Object { $_.StartsWith('VICE_BUS,missing,') -or $_.StartsWith('VICE_BUS,foreign,') }).Count
        TranslateLines = @($stderrLines | Where-Object { $_.StartsWith('VICE_BUS,translate,') }).Count
        RepatchLines = @($stderrLines | Where-Object { $_.StartsWith('VICE_BUS,repatch,') }).Count
        StopLines = @($stderrLines | Where-Object { $_.Contains('_STOP,') }).Count
    }
}

# Per-cycle bus equivalence between the two gated runs: the keep run observes every access that does not
# go through the bank-base fast path (stores, non-fetch loads, dummies) with address, data and clock, and
# the withheld run observes a superset (the fetches too). Every access line of the keep run must therefore
# occur in the withheld run with at least the same multiplicity, and the VICE_GATE sequences must be
# identical line for line. A screenshot alone can be a weak digest (a machine whose screen has not changed
# yet yields the same image), so this check carries the per-cycle claim.
function Compare-GatedRunPair {
    param([string] $WithheldStderr, [string] $KeepStderr)
    $withheldCounts = @{}
    $withheldGate = [System.Collections.Generic.List[string]]::new()
    foreach ($line in [IO.File]::ReadLines($WithheldStderr)) {
        if ($line.StartsWith('VICE_BUS,access,')) { if ($withheldCounts.ContainsKey($line)) { $withheldCounts[$line]++ } else { $withheldCounts[$line] = 1 } }
        elseif ($line.StartsWith('VICE_GATE,')) { $withheldGate.Add($line) }
    }
    $missingLines = 0
    $keepAccess = 0
    $keepGate = [System.Collections.Generic.List[string]]::new()
    $firstMissing = $null
    foreach ($line in [IO.File]::ReadLines($KeepStderr)) {
        if ($line.StartsWith('VICE_BUS,access,')) {
            $keepAccess++
            if ($withheldCounts.ContainsKey($line) -and $withheldCounts[$line] -gt 0) { $withheldCounts[$line]-- }
            else { $missingLines++; if (-not $firstMissing) { $firstMissing = $line } }
        } elseif ($line.StartsWith('VICE_GATE,')) { $keepGate.Add($line) }
    }
    $gateIdentical = ($withheldGate.Count -eq $keepGate.Count)
    if ($gateIdentical) { for ($i = 0; $i -lt $withheldGate.Count; $i++) { if ($withheldGate[$i] -ne $keepGate[$i]) { $gateIdentical = $false; break } } }
    # A forward gap in the gated clock sequence is a cycle the hook never saw (for example a VIC-II stolen
    # cycle before S4a); a decrease is a reset and is reported separately.
    $gaps = 0; $resets = 0; $previous = -1L
    foreach ($g in $withheldGate) {
        $clock = [long]($g.Substring(10))
        if ($previous -ge 0) { if ($clock -gt $previous + 1) { $gaps++ } elseif ($clock -le $previous) { $resets++ } }
        $previous = $clock
    }
    [pscustomobject]@{ KeepAccessLines = $keepAccess; KeepLinesAbsentFromWithheld = $missingLines; FirstAbsentLine = $firstMissing; GateSequencesIdentical = $gateIdentical; WithheldClockGaps = $gaps; WithheldClockResets = $resets }
}

$results = @()
$comparisons = @()
$findings = @()
foreach ($m in $machines) {
    $stock = Join-Path $buildSrc ($m + $exeExt)
    $observer = Join-Path $bin ("{0}-observer{1}" -f $m, $exeExt)
    foreach ($n in $Cycles) {
        Write-Output ("=== {0} {1} cycles ({2}) ===" -f $m, $n, (Get-Date -Format 'HH:mm:ss'))
        $s = Invoke-Run -Label 'stock' -Executable $stock -MachineName $m -CycleCount $n -Gated $false -Keep $false
        $w = Invoke-Run -Label 'withheld' -Executable $observer -MachineName $m -CycleCount $n -Gated $true -Keep $false
        $k = Invoke-Run -Label 'keep' -Executable $observer -MachineName $m -CycleCount $n -Gated $true -Keep $true
        $results += @($s, $w, $k)
        foreach ($r in @($s, $w, $k)) { Write-Output ("{0,-9} exit {1} gate {2} access {3} missingKind {4} missing/foreign {5} translate {6} stop {7} png {8}" -f $r.Label, $r.ExitCode, $r.GateLines, $r.AccessLines, $r.AccessMissingKind, $r.MissingOrForeign, $r.TranslateLines, $r.StopLines, $r.ScreenshotSha256) }
        if (-not $s.ScreenshotSha256 -or $s.ScreenshotSha256 -ne $w.ScreenshotSha256 -or $s.ScreenshotSha256 -ne $k.ScreenshotSha256) { $findings += "$m/$n screenshot differs across stock, withheld, keep" }
        if ($w.GateLines -ne $k.GateLines) { $findings += "$m/$n gate line counts differ: withheld $($w.GateLines) keep $($k.GateLines)" }
        $c = Compare-GatedRunPair -WithheldStderr (Join-Path $OutDir ("{0}-{1}-withheld.stderr" -f $m, $n)) -KeepStderr (Join-Path $OutDir ("{0}-{1}-keep.stderr" -f $m, $n))
        $comparisons += [pscustomobject]@{ Machine = $m; Cycles = $n; Comparison = $c }
        Write-Output ("compare   keep access {0} absent-from-withheld {1} gate-identical {2} clock-gaps {3} resets {4}" -f $c.KeepAccessLines, $c.KeepLinesAbsentFromWithheld, $c.GateSequencesIdentical, $c.WithheldClockGaps, $c.WithheldClockResets)
        if ($c.KeepLinesAbsentFromWithheld -ne 0) { $findings += "$m/$n $($c.KeepLinesAbsentFromWithheld) keep-run access lines absent from the withheld run (first: $($c.FirstAbsentLine))" }
        if ($c.WithheldClockGaps -ne 0) { $findings += "$m/$n $($c.WithheldClockGaps) forward gaps in the gated clock sequence (cycles the hook never saw)" }
        if (-not $c.GateSequencesIdentical) { $findings += "$m/$n VICE_GATE sequences differ between withheld and keep" }
        if ($w.StopLines -or $k.StopLines) { $findings += "$m/$n STOP line present" }
        if ($w.MissingOrForeign -or $w.AccessMissingKind) { $findings += "$m/$n withheld run has missing or foreign reports ($($w.MissingOrForeign)) or accesses with kind missing ($($w.AccessMissingKind))" }
        if ($s.ExitCode -ne 1 -or $w.ExitCode -ne 1 -or $k.ExitCode -ne 1) { $findings += "$m/$n exit codes not the cycle-limit exit (1): stock $($s.ExitCode) withheld $($w.ExitCode) keep $($k.ExitCode)" }
    }
}
$summary = [pscustomobject]@{
    Host = $hostTag; Sha = $Sha; DataDirectory = $data; Machines = $machines; Cycles = $Cycles
    StockBinaries = @($machines | ForEach-Object { [pscustomobject]@{ Machine = $_; Sha256 = Get-Sha256 (Join-Path $buildSrc ($_ + $exeExt)) } })
    ObserverBinaries = @($machines | ForEach-Object { [pscustomobject]@{ Machine = $_; Sha256 = Get-Sha256 (Join-Path $bin ("{0}-observer{1}" -f $_, $exeExt)) } })
    Runs = $results; Comparisons = $comparisons; Findings = $findings; Equivalent = ($findings.Count -eq 0)
}
$summaryPath = Join-Path $OutDir 'equivalence.json'
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding utf8
Write-Output "summary: $summaryPath"
if ($findings.Count) { Write-Output 'FINDINGS:'; $findings | ForEach-Object { Write-Output "  $_" }; exit 2 }
Write-Output 'equivalent: every check passed'
