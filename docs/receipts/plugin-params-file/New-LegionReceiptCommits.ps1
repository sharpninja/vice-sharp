#Requires -Version 7.4
<#
.SYNOPSIS
    Commits today's PAYTON-LEGION2 vice-sharp receipts on develop in six logical commits (order relayed by OMAR-VICE#
    for Payton: "Coordinate with legion2 to commit and sync origin"). Does not push.

    Inputs: commit-candidates.json from Get-CommitCandidates.ps1, which must be under 15 minutes old. Every file under
    the group roots is staged (gitignored TRX receipts force-added) except files over the size cap, commit-candidates.json
    (it names the files a short credential token matched, which would narrow that credential) and AGENTS-README-FIRST.yaml.
    Refuses when any secret-scan hit falls outside the reviewed allowlist (native-inventory-*.txt, lab-omarchy password
    rule only: a whole-token match on an upstream VICE file name, reviewed with the value masked).
    Staging uses git add --pathspec-from-file so long path lists never reach the command line. -WhatIf lists each
    commit's file count and bytes and stages nothing.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$CandidatesPath = (Join-Path $PSScriptRoot 'commit-candidates.json'),
    [string]$Signature = 'claude-opus-5-5-high@ClaudeCode'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $RepoRoot
if ((git rev-parse --abbrev-ref HEAD).Trim() -ne 'develop') { throw 'not on develop' }
$c = Get-Content -LiteralPath $CandidatesPath -Raw | ConvertFrom-Json
if (((Get-Date) - [datetime]$c.at).TotalMinutes -gt 15) { throw "candidates are stale ($($c.at)); rerun Get-CommitCandidates.ps1" }
$badHits = @($c.files | Where-Object { $_.Hits.Count -and -not ($_.Path -match '/native-inventory-(before|after)\.txt$' -and @($_.Hits | Where-Object { $_ -ne 'literal:creds/lab-omarchy.yaml.password' }).Count -eq 0) })
if ($badHits.Count) { throw "unreviewed secret-scan hits: $(@($badHits | ForEach-Object Path) -join ', ')" }

$co = 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'
$messages = [ordered]@{
    'phase-a-windows'    = @('docs(lockstep): Phase A Windows baseline receipts (parts 1 and 2)', 'PAYTON-LEGION2 Phase A receipts: the 20261019T100900 and 20261019T101000 baseline folders, the part2-testharness diagnostic runs (CpuNmiFive, Determinism, Acceptance, LockstepValidation, Vic20 native lockstep abort with the DualVia livelock probe, Vic20HalfCycle, CpuVisiblePcTiming, X64ScVariant) with READMEs, TRX files (force-added; gitignored by default) and native inventories proving native/vice untouched.', 'Excluded: the four hang dumps (two dumps, each stored twice, 228 to 230 MB each) and part2-testharness/build.log (10.9 MB), all over the 10 MB cap; they stay on LEGION2.', 'Confidence: correctness 97%, accuracy 97%, completeness 93%')
    'requirements-audit' = @('docs(requirements): D8 status-accuracy audit and correction receipts', 'Export, audit (v2 evidence classification), correction and compare scripts plus their receipts: 63 records corrected, 109 flags cleared with evidence preserved, 56 records lowered to pending, 0 unexpected differences, 755 mapping rows equal.', 'Confidence: correctness 97%, accuracy 97%, completeness 95%')
    'carry-over'         = @('docs(receipts): reviewer-queue carry-over disposition', 'The 4 reviewer-queue records are superseded and preserved in place.', 'Confidence: correctness 97%, accuracy 97%, completeness 95%')
    'r0-coordinator'     = @('docs(requirements): R0 coordinator, guard gates, apply and mirror verification receipts', 'R1 coordinator modules, G01 to G18 mock gate (88) and binding (33) suites, gate-freeze-v2, the 20261019T161658 apply (verified-data-awaiting-gates: 1 updateBatch, 6 mapping upserts, 3 unchanged), final readback (records 1B7D700A...869B, mappings E15E0AF0...BCC8) and the independent LAB-OMARCHY mirror verification (16/16 records, 9/9 rows, 0 differences; negative control caught 2 planted differences).', 'Confidence: correctness 97%, accuracy 97%, completeness 95%')
    'plugin-params-file' = @('docs(receipts): plugin params-file fix, reinstall and line-ending config receipts', 'Scripts and receipts for the core params-file fix (child command line limit), its back-port, sync and publication to the nine plugin repos, plugin test runs, the Claude 1.121.0 reinstall on LEGION2, core.autocrlf=input on the 11 plugin clones, and the pre-commit secret scan (summary only; the per-file inventory stays local).', 'Confidence: correctness 97%, accuracy 97%, completeness 94%')
    'nuke-tests-windows' = @('docs(nuke-tests): Windows step 5 targets and step 6 requirement capture receipts', 'Step 5 on PAYTON-LEGION2: BuildTest 96/96, Library 124/124, RommDesktopTest Heads 61/61 and Library 124/124 (TRX force-added). Step 6: TR-BUILD-001 ac-12 and TEST-BUILD-001 capture with readbacks.', 'Confidence: correctness 97%, accuracy 97%, completeness 95%')
}

$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('legion-commit-{0}' -f [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp -WhatIf:$false | Out-Null
$utf8 = [System.Text.UTF8Encoding]::new($false)
try {
    foreach ($g in $messages.Keys) {
        $files = @($c.files | Where-Object { $_.Group -eq $g -and -not $_.OverMax -and -not $_.Marker -and $_.Path -notmatch '(^|/)commit-candidates\.json$|AGENTS-README-FIRST\.yaml$' })
        if (-not $files.Count) { throw "${g}: no files" }
        $bytes = ($files | Measure-Object Bytes -Sum).Sum
        Write-Information ('{0}: {1} files ({2} force-added ignored), {3:N0} bytes' -f $g, $files.Count, @($files | Where-Object Ignored).Count, $bytes) -InformationAction Continue
        if (-not $PSCmdlet.ShouldProcess($g, "stage $($files.Count) files and commit")) { continue }
        $spec = Join-Path $tmp "$g.pathspec"
        [System.IO.File]::WriteAllLines($spec, [string[]]@($files | ForEach-Object Path), $utf8)
        git add -f --pathspec-from-file=$spec
        if ($LASTEXITCODE -ne 0) { throw "${g}: git add failed" }
        $staged = @(git -c color.ui=never -c core.quotepath=off diff --cached --name-only)
        if ($staged.Count -ne $files.Count) { throw "${g}: staged $($staged.Count) paths, expected $($files.Count)" }
        $m = $messages[$g]
        $body = @($m[0], '') + @($m[1..($m.Count - 2)] | ForEach-Object { $_; '' }) + @("Signed: $Signature | $($m[-1])", '', $co)
        $dash = @($body | Where-Object { $_ -match '[\u2013\u2014]' })
        if ($dash.Count) { throw "${g}: commit message contains em or en dashes" }
        $msgPath = Join-Path $tmp "$g.msg"
        [System.IO.File]::WriteAllText($msgPath, (($body -join "`n") + "`n"), $utf8)
        git commit -q -F $msgPath
        if ($LASTEXITCODE -ne 0) { throw "${g}: commit failed" }
        Write-Information ('{0}: committed {1} ({2} files)' -f $g, (git rev-parse --short=7 HEAD).Trim(), @(git -c color.ui=never show --name-only --format= HEAD).Count) -InformationAction Continue
    }
}
finally { Remove-Item -LiteralPath $tmp -Recurse -Force -WhatIf:$false -Confirm:$false }
