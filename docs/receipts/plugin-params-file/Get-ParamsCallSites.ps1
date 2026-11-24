#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only survey for the plugin params-on-command-line defect (triage-report-821fe2da58284b4ea21604cb2da2fccb):
    in each of the nine mcpserver plugin repos, on origin's default branch, list every non-test line that
    passes params text to a child process (-ParamsYaml / -Params arguments), every declaration of
    $ParamsYaml, whether repl-invoke.ps1 has a file parameter, and whether the repo carries a shared-core
    manifest. Writes survey.json beside this script.
#>
[CmdletBinding()]
param(
    [string]$Root = 'F:\GitHub',
    [string]$OutPath = (Join-Path $PSScriptRoot 'survey.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
$pattern = '-ParamsYaml|''-Params''|"-Params"'
$report = foreach ($name in $repos) {
    $path = Join-Path $Root $name
    $default = (git -C $path symbolic-ref --short refs/remotes/origin/HEAD).Trim()
    $sha = (git -C $path rev-parse --short=8 $default).Trim()
    $lines = @(git -C $path grep -n -E -e $pattern $default -- '*.ps1' '*.psm1')
    $sites = @($lines | ForEach-Object { $_ -replace "^$([regex]::Escape($default)):", '' } | Where-Object { $_ -notmatch '^tests/' -and $_ -notmatch '\[string\]\$ParamsYaml' })
    $replDecl = @(git -C $path grep -n -E -e 'param\(|\[string\]\$Params(Yaml|Path)' $default -- 'lib/repl-invoke.ps1' | Select-Object -First 6)
    $manifest = $false
    git -C $path cat-file -e "${default}:CORE-MANIFEST.yaml" 2>$null
    if ($LASTEXITCODE -eq 0) { $manifest = $true }
    [pscustomobject]@{ repo = $name; defaultBranch = $default; head = $sha; siteCount = $sites.Count; sites = $sites; replInvokeDecl = $replDecl; coreManifest = $manifest }
}
$report | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM
foreach ($r in $report) {
    Write-Information ('== {0} {1}@{2} sites={3} coreManifest={4}' -f $r.repo, $r.defaultBranch, $r.head, $r.siteCount, $r.coreManifest) -InformationAction Continue
    foreach ($s in $r.sites) { Write-Information ('   {0}' -f $s.Substring(0, [Math]::Min(190, $s.Length))) -InformationAction Continue }
}
