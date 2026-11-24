#Requires -Version 7.4
<#
.SYNOPSIS
    Runs every Pester suite under tests/ of the named plugin worktrees (or of their default-branch base, -Base) and
    writes one NUnit XML and one failed-tests list per repo, plus a summary line per repo.
#>
[CmdletBinding()]
param(
    [string[]]$Repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin'),
    [string]$Root = 'F:\GitHub',
    [string]$WorktreeName = 'params-file-20261019',
    [string]$Label = 'fix',
    [string]$OutDir = (Join-Path $PSScriptRoot 'plugin-tests')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module Pester -MinimumVersion 5.7.0
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
foreach ($r in $Repos) {
    $w = Join-Path $Root "$r\.worktrees\$WorktreeName"
    $tests = Join-Path $w 'tests'
    $files = @(Get-ChildItem -LiteralPath $tests -Filter '*.Tests.ps1' -File -ErrorAction SilentlyContinue)
    if (-not $files.Count) { Write-Information ('{0}: no Pester suites' -f $r) -InformationAction Continue; continue }
    $started = Get-Date
    Push-Location $w
    try {
        $cfg = New-PesterConfiguration
        $cfg.Run.Path = @($files.FullName)
        $cfg.Run.PassThru = $true
        $cfg.Output.Verbosity = 'None'
        $cfg.TestResult.Enabled = $true
        $cfg.TestResult.OutputPath = Join-Path $OutDir "$r.$Label.xml"
        $res = Invoke-Pester -Configuration $cfg
    }
    finally { Pop-Location }
    @($res.Failed | ForEach-Object { $_.ExpandedPath }) | Out-File -LiteralPath (Join-Path $OutDir "$r.$Label.failed.txt") -Encoding utf8NoBOM
    $line = '{0} [{1}] suites={2} Result={3} Total={4} Passed={5} Failed={6} Skipped={7} NotRun={8} seconds={9}' -f $r, $Label, $files.Count, $res.Result, $res.TotalCount, $res.PassedCount, $res.FailedCount, $res.SkippedCount, $res.NotRunCount, [int]((Get-Date) - $started).TotalSeconds
    Write-Information $line -InformationAction Continue
    Add-Content -LiteralPath (Join-Path $OutDir 'summary.txt') -Value ((Get-Date -Format s) + ' ' + $line) -Encoding utf8NoBOM
}
