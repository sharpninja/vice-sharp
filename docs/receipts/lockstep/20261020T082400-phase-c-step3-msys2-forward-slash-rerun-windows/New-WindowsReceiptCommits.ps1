#Requires -Version 7.4
<#
.SYNOPSIS
    Commits today's held PAYTON-LEGION2 Windows lockstep receipts on develop, one commit per receipt, after the MSYS2
    reruns (Payton 2026-10-20 08:18, "After reruns": "hold them, and commit everything together with the MSYS2 rerun
    receipts"). Inputs: the candidate list from Get-CommitCandidates.ps1 -GroupOverride (under 15 minutes old; only the
    reviewed native-inventory false positive allowed as a scan hit). Executables are excluded, gitignored TRX files
    are force-added, and staging uses --pathspec-from-file. The S1 receipt's rerun-pending README note (a tracked
    modification) is its own commit. Pushes develop with a plain push when -Push is given. -WhatIf lists the commits.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot = 'F:/GitHub/vice-sharp',
    [string]$CandidatesPath = "$env:TEMP/windows-receipts-candidates.json",
    [string]$Signature = 'claude-opus-5-5-high@ClaudeCode',
    [switch]$Push
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $RepoRoot
if ((git rev-parse --abbrev-ref HEAD).Trim() -ne 'develop') { throw 'not on develop' }
if (@(git diff --cached --name-only).Count) { throw 'index already has staged changes' }
$c = Get-Content -LiteralPath $CandidatesPath -Raw | ConvertFrom-Json
if (((Get-Date) - [datetime]$c.at).TotalMinutes -gt 15) { throw "candidates are stale ($($c.at))" }
$bad = @($c.files | Where-Object { $_.Hits.Count -and -not ($_.Path -match '/native-inventory-(before|after)\.txt$' -and @($_.Hits | Where-Object { $_ -ne 'literal:creds/lab-omarchy.yaml.password' }).Count -eq 0) })
if ($bad.Count) { throw "unreviewed secret-scan hits: $(@($bad | Select-Object -ExpandProperty Path) -join ', ')" }

$superseded = 'Compiled outside MSYS2, so superseded by the 20261020T082400 forward-slash MSYS2 rerun; README marks it rerun-pending.'
$plan = [ordered]@{
    's1-note'    = @('docs(lockstep): mark the S1 Windows receipt rerun-pending under the MSYS2 ruling', 'Payton 2026-10-20 08:14 "Fixtures too": the S1 fixtures were compiled by CompiledCFixture calling gcc.exe directly, outside MSYS2. Superseded by the 20261020T082400 MSYS2 forward-slash rerun.')
    's1b'        = @('docs(lockstep): PLAN-OBSERVER-001 S1b Windows gates (LEGION2)', "develop 9a7ab550: --filter BusObserver 50/50, full ViceSharp.NativeClockProbe.Tests 203/203; bus-probe-rows (-DVICE_SHARP_ROWS=32) beside bus-probe. $superseded")
    's1c'        = @('docs(lockstep): PLAN-OBSERVER-001 S1c Windows gates and the x64sc toggle citation (LEGION2)', "develop c711365a: --filter BusObserver 58/58, full 211/211; four wraps including mem_toggle_watchpoints; x64sc citation src/c64/c64cpusc.c 217 includes src/mainc64cpu.c 622. $superseded")
    's2a'        = @('docs(lockstep): PLAN-OBSERVER-001 S2a Windows gates, msvcrt stderr finding, link inputs (LEGION2)', "develop a7421a17: --filter BusAdapter 7/8, full 218/219; broken-stderr exited 65 (msvcrt buffers stderr on a pipe; fprintf succeeds, fflush fails), probe in msvcrt-stderr-probe/ compiled inside MSYS2. Resolve-ViceLinkInputs.ps1 expands the stock x64sc and xvic link inputs (xvic self-check 114/114 against the Oct 8 recorded link). $superseded")
    'x64sc'      = @('docs(lockstep): PLAN-OBSERVER-001 S2a plus x64sc hook Windows gates (LEGION2)', "develop 4a969214: --filter BusAdapter 7/8, full 228/229 (the broken-stderr finding); the 10 x64sc hook tests pass. $superseded")
    'msys2-131b' = @('docs(lockstep): Windows fixture gates rerun with MSYS2 compiles (LEGION2)', 'develop 131b9394: --filter BusObserver 58/58, --filter BusAdapter 8/8 (broken-stderr 72), full 232/232; every compile.log shows resolved /mingw64/bin/gcc and MSYSTEM=MINGW64, but backslash paths (lab rule open at the time).')
    'msys2-8c42' = @('docs(lockstep): Windows fixture gates, MSYS2 compiles with forward-slash paths (LEGION2)', 'develop 8c426015, the compliant run under the 08:07, 08:14 and 08:19 rules: --filter BusObserver 58/58, --filter BusAdapter 8/8 (broken-stderr 72), full 232/232, 0 failed, 0 not executed; all 8 compile.logs resolved /mingw64/bin/gcc, MSYSTEM=MINGW64, Executable C:/msys64/usr/bin/bash.exe, no backslash in any gcc argument; native/vice unchanged.')
}
$s1Readme = 'docs/receipts/lockstep/20261020T074800-phase-c-step3-s1-bus-observer-windows/README.md'
$co = 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'
$conf = 'Confidence: correctness 97%, accuracy 97%, completeness 95%'
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('windows-receipts-{0}' -f [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp -WhatIf:$false | Out-Null
$utf8 = [System.Text.UTF8Encoding]::new($false)
try {
    foreach ($g in $plan.Keys) {
        $paths = @(if ($g -eq 's1-note') { $s1Readme } else { $c.files | Where-Object { $_.Group -eq $g -and -not $_.OverMax -and $_.Path -notmatch '\.exe$' } | Select-Object -ExpandProperty Path })
        if (-not $paths.Count) { throw "${g}: no files" }
        Write-Information ('{0}: {1} files' -f $g, $paths.Count) -InformationAction Continue
        if (-not $PSCmdlet.ShouldProcess($g, "stage $($paths.Count) files and commit")) { continue }
        $spec = Join-Path $tmp "$g.pathspec"
        [System.IO.File]::WriteAllLines($spec, [string[]]$paths, $utf8)
        git add -f --pathspec-from-file=$spec
        if ($LASTEXITCODE -ne 0) { throw "${g}: git add failed" }
        $staged = @(git -c color.ui=never -c core.quotepath=off diff --cached --name-only)
        if ($staged.Count -ne $paths.Count) { throw "${g}: staged $($staged.Count), expected $($paths.Count)" }
        if (@(git -c color.ui=never diff --cached --check).Count) { throw "${g}: git diff --check failed" }
        $body = @($plan[$g][0], '', $plan[$g][1], '', "Signed: $Signature | $conf", '', $co)
        if (@($body | Where-Object { $_ -match '[\u2013\u2014]' }).Count) { throw "${g}: message contains em or en dashes" }
        $msg = Join-Path $tmp "$g.msg"
        [System.IO.File]::WriteAllText($msg, (($body -join "`n") + "`n"), $utf8)
        git commit -q -F $msg
        if ($LASTEXITCODE -ne 0) { throw "${g}: commit failed" }
        Write-Information ('{0}: committed {1} ({2} files)' -f $g, (git rev-parse --short=8 HEAD).Trim(), @(git show --name-only --format= HEAD).Count) -InformationAction Continue
    }
}
finally { Remove-Item -LiteralPath $tmp -Recurse -Force -WhatIf:$false -Confirm:$false }
if ($Push -and -not $WhatIfPreference) {
    git fetch -q origin
    if ((git merge-base HEAD origin/develop).Trim() -ne (git rev-parse origin/develop).Trim()) { throw 'origin/develop moved; not a fast-forward' }
    $o = git push origin develop 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "push failed: $o" }
    git fetch -q origin
    Write-Information ('pushed: HEAD {0} origin/develop {1} ls-remote {2}' -f (git rev-parse --short=8 HEAD), (git rev-parse --short=8 origin/develop), ((git ls-remote origin refs/heads/develop) -split '\s+')[0].Substring(0, 8)) -InformationAction Continue
}
