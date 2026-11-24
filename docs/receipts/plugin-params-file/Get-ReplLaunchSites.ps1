#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: every non-test line on each plugin repo's origin default branch that passes -ParamsYaml text
    across a process boundary to repl-invoke.ps1 (ProcessStartInfo ArgumentList/Arguments, Start-Process,
    pwsh -File, Invoke-PluginPowerShellScript -Arguments). In-process calls (& script, dot-sourced functions)
    are excluded because no command line is involved. Writes launch-sites.json.
#>
[CmdletBinding()]
param(
    [string]$Root = 'F:\GitHub',
    [string]$OutPath = (Join-Path $PSScriptRoot 'launch-sites.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
$boundary = 'ArgumentList|-Arguments\s|Arguments\s*=|Start-Process|pwsh(\.exe)?[''"]?\s+.*-File|\$psi|ProcessStartInfo'

$all = foreach ($name in $repos) {
    $path = Join-Path $Root $name
    $def = (git -c color.ui=never -C $path symbolic-ref --short refs/remotes/origin/HEAD).Trim()
    $files = @(git -c color.ui=never -C $path grep -l -E -e 'ParamsYaml' $def -- '*.ps1' '*.psm1' | ForEach-Object { $_.Substring($def.Length + 1) } | Where-Object { $_ -notmatch '^tests/' })
    foreach ($f in $files) {
        $text = @(git -c color.ui=never -C $path show "${def}:$f")
        for ($i = 0; $i -lt $text.Count; $i++) {
            if ($text[$i] -notmatch 'ParamsYaml') { continue }
            $lo = [Math]::Max(0, $i - 6); $hi = [Math]::Min($text.Count - 1, $i + 2)
            $window = ($text[$lo..$hi] -join "`n")
            if ($window -match $boundary -and $text[$i] -match '''-ParamsYaml''|"-ParamsYaml"|-ParamsYaml\s*[,)]|ArgumentList\.Add\(\s*\$ParamsYaml') {
                [pscustomobject]@{ repo = $name; file = $f; line = $i + 1; text = $text[$i].Trim() }
            }
        }
    }
}
$all | ConvertTo-Json -Depth 4 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM
Write-Information ("total launch sites: {0}" -f @($all).Count) -InformationAction Continue
foreach ($g in ($all | Group-Object repo)) {
    Write-Information ("== {0}: {1}" -f $g.Name, $g.Count) -InformationAction Continue
    foreach ($s in $g.Group) { Write-Information ("   {0}:{1}  {2}" -f $s.file, $s.line, $s.text.Substring(0, [Math]::Min(150, $s.text.Length))) -InformationAction Continue }
}
