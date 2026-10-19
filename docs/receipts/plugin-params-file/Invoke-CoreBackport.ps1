#Requires -Version 7.4
<#
.SYNOPSIS
    Step 1 of the plugin params-file fix: back-port vice-sharp-b1's plugin-side commit 7991235 ("build all plugin YAML
    from objects, never from strings", 2026-10-17) into McpServer plugins/core, where the core README says shared
    logic lives ("Fix in this directory and re-sync; never patch a plugin's copy").

    Base check: core lib-ps on this branch equals 88032108, the core the plugins were synced from on 2026-10-15, and
    the plugin copies are 88032108 plus 7991235 only. So the back-port is the plugin copy itself (no three-way
    conflict). Files are copied byte-exact from the claude-code plugin worktree (the eight standard plugins hold
    identical copies). The two plugin tests are ported into plugins/core/test-fixtures/pester with only their
    plugin-root lines changed to a staged plugin (Write-TestStagedPlugin), so the static YAML lint guards every
    future sync. -WhatIf reports the plan.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$CoreWorktree = 'F:\GitHub\McpServer\.worktrees\plugin-params-file-20261019',
    [string]$PluginWorktree = 'F:\GitHub\mcpserver-claude-code-plugin\.worktrees\params-file-20261019'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$copies = [ordered]@{
    'lib/Invoke-McpPlugin.ps1'     = 'plugins/core/lib-ps/Invoke-McpPlugin.ps1'
    'lib/McpPluginShim.psm1'       = 'plugins/core/lib-ps/McpPluginShim.psm1'
    'lib/cache-manager.ps1'        = 'plugins/core/lib-ps/cache-manager.ps1'
    'lib/plugin-hook.ps1'          = 'plugins/core/lib-ps/plugin-hook.ps1'
    'lib/repl-invoke.ps1'          = 'plugins/core/lib-ps/repl-invoke.ps1'
    'lib/yaml-object-mutation.ps1' = 'plugins/core/lib-ps/yaml-object-mutation.ps1'
    'lib/plan-todo-map.ps1'        = 'plugins/core/lib-ps/plan-todo-map.ps1'
    'skills/handoff/invoke.ps1'    = 'plugins/core/skills/handoff/invoke.ps1'
}

# Base check: every core file the back-port replaces is still at 88032108.
foreach ($dest in $copies.Values) {
    $now = git -C $CoreWorktree rev-parse --verify --quiet "HEAD:$dest" 2>$null
    $base = git -C $CoreWorktree rev-parse --verify --quiet "88032108:$dest" 2>$null
    if (-not $now -and -not $base) { continue }
    if ("$now" -ne "$base") { throw "core $dest moved since 88032108 (now '$now', base '$base'); a real three-way merge is needed" }
}

$stagedSetup = @(
    '        $script:CoreRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ''..\..\..\..'')).ProviderPath'
    '        . (Join-Path $PSScriptRoot ''..\PluginTestFixtures.ps1'')'
    '        # Ported from the plugin repos (7991235): run against a staged plugin synced from plugins/core.'
    '        $script:PluginRoot = Write-TestStagedPlugin -RepoRoot $script:CoreRepoRoot -Root (Join-Path ([System.IO.Path]::GetTempPath()) (''mcp-staged-yaml-'' + [guid]::NewGuid().ToString(''N'')))'
) -join "`n"
$ports = @(
    [pscustomobject]@{ Source = 'tests/YamlConstruction.Tests.ps1'; Dest = 'plugins/core/test-fixtures/pester/YamlConstruction.Tests.ps1'; Old = '        $script:PluginRoot = (Resolve-Path (Join-Path $PSScriptRoot ''..'')).Path'; New = $stagedSetup }
    [pscustomobject]@{ Source = 'tests/TimeoutEnvelope.Tests.ps1'; Dest = 'plugins/core/test-fixtures/pester/TimeoutEnvelope.Tests.ps1'; Old = "        Import-Module (Join-Path `$PSScriptRoot '..\lib\McpPluginShim.psm1') -Force`n        . (Join-Path `$PSScriptRoot '..\lib\yaml-object-mutation.ps1')"; New = ($stagedSetup + "`n        Import-Module (Join-Path `$script:PluginRoot 'lib\McpPluginShim.psm1') -Force`n        . (Join-Path `$script:PluginRoot 'lib\yaml-object-mutation.ps1')") }
)

if (-not $PSCmdlet.ShouldProcess($CoreWorktree, 'Back-port 7991235 into plugins/core and port its two tests')) {
    foreach ($k in $copies.Keys) { Write-Information ('copy {0} -> {1}' -f $k, $copies[$k]) -InformationAction Continue }
    foreach ($p in $ports) { Write-Information ('port {0} -> {1}' -f $p.Source, $p.Dest) -InformationAction Continue }
    return
}

foreach ($k in $copies.Keys) {
    $src = Join-Path $PluginWorktree $k
    $dst = Join-Path $CoreWorktree $copies[$k]
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    Copy-Item -LiteralPath $src -Destination $dst -Force
    if ((Get-FileHash -LiteralPath $src).Hash -ne (Get-FileHash -LiteralPath $dst).Hash) { throw "copy hash mismatch: $k" }
}
foreach ($p in $ports) {
    $text = [System.IO.File]::ReadAllText((Join-Path $PluginWorktree $p.Source))
    $eol = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $old = $p.Old -replace "`n", $eol
    $hits = ([regex]::Matches($text, [regex]::Escape($old))).Count
    if ($hits -ne 1) { throw "port anchor found $hits times in $($p.Source)" }
    $text = $text.Replace($old, ($p.New -replace "`n", $eol))
    # Third plugin-root path in TimeoutEnvelope (found by the first core run): read the wrapper from the staged plugin.
    $wrapperOld = "(Join-Path `$PSScriptRoot '..\lib\Invoke-McpPlugin.ps1')"
    if ($text.Contains($wrapperOld)) { $text = $text.Replace($wrapperOld, "(Join-Path `$script:PluginRoot 'lib\Invoke-McpPlugin.ps1')") }
    if ($text.Contains("`$PSScriptRoot '..\lib") -or $text.Contains("`$PSScriptRoot '..')")) { throw "unported plugin-root path remains in $($p.Dest)" }
    [System.IO.File]::WriteAllText((Join-Path $CoreWorktree $p.Dest), $text, [System.Text.UTF8Encoding]::new($false))
}
git -C $CoreWorktree status --short
