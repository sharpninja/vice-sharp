#Requires -Version 7.4
<#
.SYNOPSIS
    Applies the approved R0 delta (16 records, 9 rows) to the PAYTON-LEGION2 requirement store through
    Invoke-R1Coordinator and the production plugin client, after the G01-G18 suite is green
    (gate-freeze-v2.json: 121/121). Writes approval.json, attribution.json, the coordinator journal and
    result.json into OutDir, and backs up the five docs/Project projection files the export overwrites.

    Approval (Payton, 2026-10-19): "Yes" to the exact 16-record, 9-row candidate, to applying without an
    idempotency key (locked decision 2 departure) and to an exclusive-writer window, as relayed by
    vice-sharp-b1 at 10:56 local; confirmed directly to this session by "Stop asking for confirmation unless
    what I said is wrong" and "You have full r/w permissions in my lab to do your assigned work".
    -WhatIf validates the packet hash, the gate freeze and the D8 attribution journal and writes the
    approval and attribution files, without any client call.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$OutDir,
    [Parameter(Mandatory)][string]$D8JournalPath,
    [int]$WindowMinutes = 30,
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    # Plugin used for the writes; 1.121.0 carries the params-file fix (no params on the child command line).
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.121.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$packetPath = Join-Path $here '..\20261013T231703-r0-anchor-remediation\proposed-field-delta.json'
$approvedSha = '63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702'
New-Item -ItemType Directory -Force -Path $OutDir -WhatIf:$false | Out-Null

$sha = (Get-FileHash -LiteralPath $packetPath -Algorithm SHA256).Hash
if ($sha -cne $approvedSha) { throw "packet hash $sha is not the approved $approvedSha" }
$freeze = Get-Content -LiteralPath (Join-Path $here 'gate-freeze-v2.json') -Raw | ConvertFrom-Json
foreach ($p in $freeze.files.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $here $p.Name) -Algorithm SHA256).Hash -cne $p.Value) { throw "frozen file changed since the green gate: $($p.Name)" }
}
if ($freeze.gates.fullSuiteRun1 -notmatch '^121 total, 121 passed, 0 failed, 0 skipped$') { throw "gate freeze does not record a green full suite: $($freeze.gates.fullSuiteRun1)" }

$d8Hit = @(Get-Content -LiteralPath $D8JournalPath | ForEach-Object { $_ | ConvertFrom-Json } | Where-Object { $_.phase -eq 'after' -and $_.id -ceq 'FR-BACKFILL-MEDIA-001' -and $_.record.status -ceq 'pending' })
if ($d8Hit.Count -ne 1) { throw "D8 journal has $($d8Hit.Count) after-entries for FR-BACKFILL-MEDIA-001 status pending (want 1)" }
$attribution = [pscustomobject]@{ Kind = 'fr'; Id = 'FR-BACKFILL-MEDIA-001'; Field = 'status'; JournalPath = (Resolve-Path -LiteralPath $D8JournalPath).Path; JournalSha256 = (Get-FileHash -LiteralPath $D8JournalPath -Algorithm SHA256).Hash; Reason = 'D8 (Payton "Follow the rules") lowered closed to pending at 2026-10-19 14:13 to 14:19; equals the approved R0 after-value.' }

$start = Get-Date
$approval = [pscustomobject]@{
    PacketSha256                      = $sha
    RecordCount                       = 16
    MappingRowCount                   = 9
    LockedDecision2DepartureAccepted  = $true
    ApprovedBy                        = 'Payton Byrd'
    ApprovalQuotes                    = @('Yes (D2: the exact 16-record, 9-row candidate; no idempotency key; exclusive-writer window), relayed by vice-sharp-b1 2026-10-19T10:56', 'Stop asking for confirmation unless what I said is wrong', 'You have full r/w permissions in my lab to do your assigned work')
    WriterWindow                      = [pscustomobject]@{
        StartLocal  = $start.ToString('s')
        ExpiryLocal = $start.AddMinutes($WindowMinutes).ToString('s')
        Scope       = 'LEGION2 requirement store: the 16 packet records and the 9 mapping rows'
        Writers     = @(
            'ClaudeCode-20261019T152439Z-plugin-session (this session): sole writer',
            'vice-sharp-b1 (LAB-OMARCHY): acknowledged no LEGION2 writes at 2026-10-19T14:37 until R0-done report',
            'Codex app-server/exec-server PIDs 24396/24764 (cloud exec-server since 2026-10-17T07:26, no child task; last vice-sharp Codex state 2026-10-16T12:45): inferred idle',
            'Grok Bot and Claude desktop apps: no vice-sharp MCP activity since 2026-10-16: inferred idle')
    }
}
$approval | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $OutDir 'approval.json') -Encoding utf8NoBOM -WhatIf:$false
$attribution | ConvertTo-Json -Depth 4 | Out-File -LiteralPath (Join-Path $OutDir 'attribution.json') -Encoding utf8NoBOM -WhatIf:$false
Write-Information ('approval bound to {0}; window {1}..{2}; attribution journal {3}' -f $sha, $approval.WriterWindow.StartLocal, $approval.WriterWindow.ExpiryLocal, $attribution.JournalSha256) -InformationAction Continue

if (-not $PSCmdlet.ShouldProcess('LEGION2 requirement store (16 records, 9 rows)', 'Apply approved R0 delta')) { return }

$backup = Join-Path $OutDir 'docs-project-before'
New-Item -ItemType Directory -Force -Path $backup | Out-Null
foreach ($f in Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'docs\Project') -Filter '*.md' -File) {
    Copy-Item -LiteralPath $f.FullName -Destination (Join-Path $backup $f.Name)
    if ((Get-FileHash -LiteralPath $f.FullName).Hash -cne (Get-FileHash -LiteralPath (Join-Path $backup $f.Name)).Hash) { throw "backup hash mismatch for $($f.Name)" }
}

Import-Module (Join-Path $here 'R1Coordinator.psm1') -Force
Import-Module (Join-Path $here 'R1PluginClient.psm1') -Force -Global
if (-not (Select-String -LiteralPath (Join-Path $PluginRoot 'lib\repl-invoke.ps1') -Pattern 'ParamsPath' -Quiet)) { throw "plugin at $PluginRoot lacks the -ParamsPath fix" }
$client = New-R1PluginClient -WorkDir (Join-Path $OutDir 'client') -PluginRoot $PluginRoot
$result = Invoke-R1Coordinator -PacketPath $packetPath -Approval $approval -Client $client -Attributions @($attribution) -JournalPath (Join-Path $OutDir 'coordinator-journal.jsonl')
[pscustomobject]@{ State = $result.State; Export = $result.Export; ExportFailed = $result.ExportFailed; Journal = $result.Journal; Calls = $result.Calls; FinishedAt = (Get-Date -Format s) } |
    ConvertTo-Json -Depth 30 | Out-File -LiteralPath (Join-Path $OutDir 'result.json') -Encoding utf8NoBOM
Write-Information ('R1 result: {0}; journal {1} entries; calls {2}' -f $result.State, @($result.Journal).Count, @($result.Calls).Count) -InformationAction Continue
foreach ($j in $result.Journal) { Write-Information ('  {0} {1} {2}' -f $j.Seq, $j.State, $j.Detail) -InformationAction Continue }
