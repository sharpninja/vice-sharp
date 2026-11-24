#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: state of the remaining PAYTON-LEGION2 plugin installs (Codex, Grok, Cline v2) and of the worktrees this
    session created, for the reinstall and cleanup plan. Prints CLI availability, marketplace sources, cache dirs, the
    installed copies' lib hashes, and each worktree's tracked, untracked and ignored file counts.
#>
[CmdletBinding()]
param([string]$Root = 'F:\GitHub')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$u = $env:USERPROFILE
function Get-LibHash([string]$Dir) {
    foreach ($f in 'lib\Invoke-McpPlugin.ps1', 'lib\repl-invoke.ps1') {
        $p = Join-Path $Dir $f
        if (Test-Path -LiteralPath $p) { '    {0} {1}' -f $f, (Get-FileHash -LiteralPath $p).Hash.Substring(0, 8) } else { "    $f missing" }
    }
}

'== CLIs'
foreach ($c in 'codex', 'grok', 'cline') { $cmd = Get-Command $c -ErrorAction SilentlyContinue; '{0}: {1}' -f $c, $(if ($cmd) { $cmd.Source } else { 'not found' }) }

'== Codex'
$cfg = Join-Path $u '.codex\config.toml'
if (Test-Path -LiteralPath $cfg) { Select-String -LiteralPath $cfg -Pattern 'mcpserver|marketplace|plugin' | ForEach-Object { '  config L{0}: {1}' -f $_.LineNumber, $_.Line.Trim() } }
foreach ($d in @(Get-ChildItem -LiteralPath (Join-Path $u '.codex\plugins\cache') -Directory -ErrorAction SilentlyContinue)) {
    foreach ($p in @(Get-ChildItem -LiteralPath $d.FullName -Directory)) {
        $vers = @(Get-ChildItem -LiteralPath $p.FullName -Directory | Sort-Object { try { [version]$_.Name } catch { [version]'0.0' } })
        '  cache {0}/{1}: {2} versions: {3}' -f $d.Name, $p.Name, $vers.Count, (($vers | Select-Object -ExpandProperty Name) -join ' ')
    }
}
$cx = Join-Path $Root 'mcpserver-codex-plugin'
'  checkout {0}: branch {1} head {2} origin/main {3} dirty {4}' -f $cx, (git -C $cx rev-parse --abbrev-ref HEAD), (git -C $cx rev-parse --short=8 HEAD), (git -C $cx rev-parse --short=8 origin/main), @(git -C $cx status --porcelain --untracked-files=no).Count

'== Grok'
$gk = Join-Path $Root 'mcpserver-grok-plugin'
'  checkout {0}: branch {1} head {2} main {3} origin/main {4} dirty {5}' -f $gk, (git -C $gk rev-parse --abbrev-ref HEAD), (git -C $gk rev-parse --short=8 HEAD), (git -C $gk rev-parse --short=8 main), (git -C $gk rev-parse --short=8 origin/main), @(git -C $gk status --porcelain --untracked-files=no).Count
foreach ($i in @(Get-ChildItem -LiteralPath (Join-Path $u '.grok\installed-plugins') -Directory -ErrorAction SilentlyContinue)) {
    '  installed {0}: branch {1} head {2} remote {3} dirty {4} untracked {5}' -f $i.Name, (git -C $i.FullName rev-parse --abbrev-ref HEAD), (git -C $i.FullName rev-parse --short=8 HEAD), (git -C $i.FullName config --get remote.origin.url), @(git -C $i.FullName status --porcelain --untracked-files=no).Count, @(git -C $i.FullName ls-files --others --exclude-standard).Count
    Get-LibHash $i.FullName
}
foreach ($f in @(Get-ChildItem -LiteralPath (Join-Path $u '.grok') -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
    if (Select-String -LiteralPath $f.FullName -Pattern 'mcpserver-grok' -Quiet) { '  grok json mentions plugin: ' + $f.Name }
}

'== Cline v2'
$cl = Join-Path $Root 'mcpserver-cline-v2-plugin'
'  checkout {0}: branch {1} head {2} origin/main {3}' -f $cl, (git -C $cl rev-parse --abbrev-ref HEAD), (git -C $cl rev-parse --short=8 HEAD), (git -C $cl rev-parse --short=8 origin/main)
$hits = @(foreach ($base in @((Join-Path $u '.cline'), (Join-Path $u 'Documents\Cline'), (Join-Path $env:APPDATA 'Code\User\globalStorage\saoudrizwan.claude-dev'))) {
        if (Test-Path -LiteralPath $base) { Get-ChildItem -LiteralPath $base -Recurse -Depth 6 -File -Filter 'Invoke-McpPlugin.ps1' -Force -ErrorAction SilentlyContinue }
    })
foreach ($h in $hits) {
    $dir = Split-Path (Split-Path $h.FullName -Parent) -Parent
    $ver = if (Test-Path -LiteralPath (Join-Path $dir 'package.json')) { (Get-Content -LiteralPath (Join-Path $dir 'package.json') -Raw | ConvertFrom-Json).version } else { '?' }
    '  copy {0}: version {1} git {2}' -f $dir, $ver, (Test-Path -LiteralPath (Join-Path $dir '.git'))
    Get-LibHash $dir
}
foreach ($base in @((Join-Path $u '.cline'))) {
    if (Test-Path -LiteralPath $base) { Get-ChildItem -LiteralPath $base -Recurse -Depth 3 -File -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '\.json$' -and (Select-String -LiteralPath $_.FullName -Pattern 'mcpserver' -Quiet) } | ForEach-Object { '  cline config mentions mcpserver: ' + $_.FullName } }
}

'== Worktrees created by this session'
$repos = @('McpServer', 'mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
foreach ($r in $repos) {
    $p = Join-Path $Root $r
    $lines = @(git -c color.ui=never -C $p worktree list --porcelain | Where-Object { $_ -like 'worktree *' } | ForEach-Object { $_.Substring(9) })
    foreach ($w in @($lines | Where-Object { $_ -match '20261019|core-baseline' })) {
        $wp = $w -replace '/', '\'
        $br = (git -C $wp rev-parse --abbrev-ref HEAD).Trim()
        $unpushed = if ($br -ne 'HEAD') { @(git -C $wp log --oneline "$br" --not --remotes).Count } else { 'detached' }
        '  {0}: branch {1} head {2} tracked-dirty {3} untracked {4} ignored {5} unpushed {6}' -f $wp, $br, (git -C $wp rev-parse --short=8 HEAD), @(git -C $wp status --porcelain --untracked-files=no).Count, @(git -C $wp ls-files --others --exclude-standard).Count, @(git -C $wp ls-files --others --ignored --exclude-standard).Count, $unpushed
    }
}
$probe = Join-Path $env:TEMP 'mcpcore-probe-f66c39f9'
'  temp probe {0}: exists {1} files {2}' -f $probe, (Test-Path -LiteralPath $probe), @(Get-ChildItem -LiteralPath $probe -Recurse -File -Force -ErrorAction SilentlyContinue).Count
