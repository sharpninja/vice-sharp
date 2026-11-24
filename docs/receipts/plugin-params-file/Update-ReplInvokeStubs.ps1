#Requires -Version 7.4
<#
.SYNOPSIS
    Test-double maintenance for the -ParamsPath contract: the core test suites replace lib/repl-invoke.ps1 with stubs
    (here-strings in PluginPowerShellRuntime.Tests.ps1, and SessionLogWrapperHarness.ps1) that model its old command
    line, param(Method, ParamsYaml) only. Launchers now pass -ParamsPath, so each stub gains the same contract as
    production repl-invoke.ps1: accept -ParamsPath and read the file into $ParamsYaml. Assertions are untouched.
    Reports the number of stubs changed per file; -WhatIf only counts.
#>
[CmdletBinding(SupportsShouldProcess)]
param([string]$CoreWorktree = 'F:\GitHub\McpServer\.worktrees\plugin-params-file-20261019')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$fixtures = Join-Path $CoreWorktree 'plugins\core\test-fixtures'

$edits = @(
    [pscustomobject]@{
        File = Join-Path $fixtures 'pester\PluginPowerShellRuntime.Tests.ps1'
        Old  = "param(`n    [Parameter(Mandatory)][string]`$Method,`n    [string]`$ParamsYaml = ''`n)"
        New  = "param(`n    [Parameter(Mandatory)][string]`$Method,`n    [string]`$ParamsYaml = '',`n    [string]`$ParamsPath = ''`n)`nif (`$ParamsPath) { `$ParamsYaml = [System.IO.File]::ReadAllText(`$ParamsPath) }"
        Min  = 1
    }
    [pscustomobject]@{
        File = Join-Path $fixtures 'pester\PluginPowerShellRuntime.Tests.ps1'
        Old  = "param(`n    [string]`$Method,`n    [string]`$ParamsYaml = ''`n)"
        New  = "param(`n    [string]`$Method,`n    [string]`$ParamsYaml = '',`n    [string]`$ParamsPath = ''`n)`nif (`$ParamsPath) { `$ParamsYaml = [System.IO.File]::ReadAllText(`$ParamsPath) }"
        Min  = 1
    }
    [pscustomobject]@{
        File = Join-Path $fixtures 'SessionLogWrapperHarness.ps1'
        Old  = "param([string]`$Method, [string]`$ParamsYaml)"
        New  = "param([string]`$Method, [string]`$ParamsYaml, [string]`$ParamsPath)`nif (`$ParamsPath) { `$ParamsYaml = [System.IO.File]::ReadAllText(`$ParamsPath) }"
        Min  = 1
    }
)

foreach ($e in $edits) {
    $text = [System.IO.File]::ReadAllText($e.File)
    $eol = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $old = $e.Old -replace "`n", $eol
    $hits = ([regex]::Matches($text, [regex]::Escape($old))).Count
    if ($hits -lt $e.Min) { throw "no stub anchor in $($e.File)" }
    if ($PSCmdlet.ShouldProcess($e.File, "update $hits repl-invoke stub parameter blocks")) {
        $text = $text.Replace($old, ($e.New -replace "`n", $eol))
        [System.IO.File]::WriteAllText($e.File, $text, [System.Text.UTF8Encoding]::new($false))
    }
    Write-Information ('{0}: {1} stub(s)' -f [IO.Path]::GetFileName($e.File), $hits) -InformationAction Continue
}
