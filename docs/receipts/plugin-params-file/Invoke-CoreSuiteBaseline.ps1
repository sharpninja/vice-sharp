#Requires -Version 7.4
<#
.SYNOPSIS
    Runs the whole plugins/core/test-fixtures/pester folder of McpServer at a given commit in a detached worktree
    and writes the NUnit XML plus a failures list, so failures can be attributed to the commit that introduced them.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$Label,
    [string]$McpServerRepo = 'F:\GitHub\McpServer',
    [string]$OutDir = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$wt = Join-Path $McpServerRepo ".worktrees\core-baseline-$Label"
if (-not $PSCmdlet.ShouldProcess($wt, "detached worktree at $Commit and full core Pester run")) { return }
if (-not (Test-Path -LiteralPath $wt)) {
    $o = git -C $McpServerRepo worktree add --detach $wt $Commit 2>&1
    if ($LASTEXITCODE -ne 0) { throw "worktree add failed: $o" }
}
Push-Location $wt
try {
    $cfg = New-PesterConfiguration
    $cfg.Run.Path = 'plugins\core\test-fixtures\pester'
    $cfg.Run.PassThru = $true
    $cfg.Output.Verbosity = 'None'
    $cfg.TestResult.Enabled = $true
    $cfg.TestResult.OutputPath = (Join-Path $OutDir "core-pester-$Label.xml")
    $r = Invoke-Pester -Configuration $cfg
}
finally { Pop-Location }
$failed = @($r.Failed | ForEach-Object { $_.ExpandedPath })
$failed | Out-File -LiteralPath (Join-Path $OutDir "core-pester-$Label.failed.txt") -Encoding utf8NoBOM
[pscustomobject]@{ label = $Label; commit = (git -C $wt rev-parse --short=8 HEAD).Trim(); result = $r.Result; total = $r.TotalCount; passed = $r.PassedCount; failed = $r.FailedCount; skipped = $r.SkippedCount; notRun = $r.NotRunCount; finishedAt = (Get-Date -Format s) } | ConvertTo-Json
