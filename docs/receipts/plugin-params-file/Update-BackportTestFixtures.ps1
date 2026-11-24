#Requires -Version 7.4
<#
.SYNOPSIS
    Test-fixture maintenance for the back-port of 7991235 (YAML built from objects) into plugins/core. Four core
    tests failed only because they model the hand-built era (baseline B-f50cf525 receipt):
      1. "writes pending cache records ..." asserted the literal text timestamp: "<iso>" (hand-built quoting). The
         serializer emits the same value unquoted, and both forms read back as the same String (measured
         2026-10-19T15:31:57). The assertion now checks the parsed value: a String in ISO 8601 UTC form.
         Judgment call by the agent, disclosed to the operator.
      2, 3. The cache flush/retry fixtures copy cache-manager.ps1 and resolve-cache-dir.ps1 into a temporary lib;
         the object-based cache-manager.ps1 now dot-sources yaml-object-mutation.ps1, so the fixture copies it too.
      4. CacheScope.BackgroundOpenSession runs Write-ReplStickySessionState extracted from repl-invoke.ps1, which now
         calls Write-McpYamlObject; the test dot-sources yaml-object-mutation.ps1 first, as its sibling test does.
    No other assertion changes. -WhatIf only counts anchors.
#>
[CmdletBinding(SupportsShouldProcess)]
param([string]$CoreWorktree = 'F:\GitHub\McpServer\.worktrees\plugin-params-file-20261019')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$pester = Join-Path $CoreWorktree 'plugins\core\test-fixtures\pester'

$edits = @(
    [pscustomobject]@{
        File = Join-Path $pester 'PluginPowerShellRuntime.Tests.ps1'; Expect = 1
        Old  = "        `$content | Should -Match 'timestamp: ""\d{4}-\d{2}-\d{2}T'"
        New  = @(
            "        # Built from objects: the serializer decides quoting; assert the parsed value (ISO 8601 UTC string)."
            "        if (-not (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue)) { Import-Module powershell-yaml -ErrorAction Stop }"
            "        `$record = ConvertFrom-Yaml -Yaml `$content -Ordered"
            "        `$record['timestamp'] | Should -BeOfType [string]"
            "        `$record['timestamp'] | Should -Match '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$'"
        ) -join "`n"
    }
    [pscustomobject]@{
        File = Join-Path $pester 'PluginPowerShellRuntime.Tests.ps1'; Expect = -1
        Old  = "        Copy-Item -LiteralPath (Join-Path `$script:LibRoot 'resolve-cache-dir.ps1') -Destination `$libCopy"
        New  = "        Copy-Item -LiteralPath (Join-Path `$script:LibRoot 'resolve-cache-dir.ps1') -Destination `$libCopy`n        Copy-Item -LiteralPath (Join-Path `$script:LibRoot 'yaml-object-mutation.ps1') -Destination `$libCopy"
    }
    [pscustomobject]@{
        File = Join-Path $pester 'TriagePluginIdentity.Tests.ps1'; Expect = 1
        Old  = "    It 'CacheScope.BackgroundOpenSession_DoesNotRebindRootActiveSession' {"
        New  = "    It 'CacheScope.BackgroundOpenSession_DoesNotRebindRootActiveSession' {`n        . (Join-Path `$script:LibRoot 'yaml-object-mutation.ps1')"
    }
)

foreach ($e in $edits) {
    $text = [System.IO.File]::ReadAllText($e.File)
    $eol = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $old = $e.Old -replace "`n", $eol
    $hits = ([regex]::Matches($text, [regex]::Escape($old))).Count
    if (($e.Expect -gt 0 -and $hits -ne $e.Expect) -or ($e.Expect -lt 0 -and $hits -lt 1)) { throw "anchor found $hits times in $([IO.Path]::GetFileName($e.File)): $($e.Old.Split("`n")[0])" }
    if ($PSCmdlet.ShouldProcess($e.File, "apply edit at $hits site(s)")) {
        $text = $text.Replace($old, ($e.New -replace "`n", $eol))
        [System.IO.File]::WriteAllText($e.File, $text, [System.Text.UTF8Encoding]::new($false))
    }
    Write-Information ('{0}: {1} site(s): {2}' -f [IO.Path]::GetFileName($e.File), $hits, $e.Old.Trim().Substring(0, [Math]::Min(70, $e.Old.Trim().Length))) -InformationAction Continue
}
