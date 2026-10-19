#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: inventories the LEGION2 vice-sharp receipt paths proposed for commit (untracked and gitignored files
    under each group root), with byte length, a flag for files over MaxBytes, and the gitignore state, and runs a secret
    scan over every candidate under MaxBytes. Secret values (the marker API key, MCP_AGENT_API_KEY, and the password,
    key, token and secret values of every ~/.creds/*.yaml file) are loaded into memory and matched literally; only the
    file, line number and the rule name are printed, never a value. Also matches generic credential patterns.
    Writes commit-candidates.json next to this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [long]$MaxBytes = 10MB,
    [string]$OutPath = (Join-Path $PSScriptRoot 'commit-candidates.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module powershell-yaml
$groups = [ordered]@{
    'phase-a-windows'      = @('docs/receipts/lockstep/20261019T100900-phase-a-baseline-windows', 'docs/receipts/lockstep/20261019T101000-phase-a-baseline-windows')
    'requirements-audit'   = @('docs/receipts/requirements-audit')
    'carry-over'           = @('docs/receipts/carry-over')
    'r0-coordinator'       = @('docs/receipts/requirements-recovery/20261019T142329-r1-coordinator')
    'plugin-params-file'   = @('docs/receipts/plugin-params-file')
    'nuke-tests-windows'   = @('docs/receipts/nuke-tests/20261019T143210-targets-windows', 'docs/receipts/nuke-tests/20261019T143442-requirement-capture-legion2', 'docs/receipts/nuke-tests/Invoke-NukeTestsStep5Windows.ps1', 'docs/receipts/nuke-tests/Invoke-NukeTestsStep6Capture.ps1')
}

# Literal secret values, held in memory only.
$secrets = [System.Collections.Generic.List[object]]::new()
# Values of 8+ chars match as substrings; shorter values (4-7 chars) only as whole tokens, to keep false positives down.
function Add-Secret([string]$Rule, $Value) {
    if ($Value -isnot [string] -or $Value.Length -lt 4) { return }
    $rx = if ($Value.Length -lt 8) { [regex]::new('(?<![A-Za-z0-9])' + [regex]::Escape($Value) + '(?![A-Za-z0-9])') } else { $null }
    $secrets.Add([pscustomobject]@{ Rule = $Rule; Value = $Value; Regex = $rx })
}
function Add-YamlSecret([string]$Source, $Node) {
    if ($Node -is [System.Collections.IDictionary]) {
        foreach ($k in $Node.Keys) {
            if ([string]$k -match '(?i)pass|key|token|secret|auth') { Add-Secret "$Source.$k" $Node[$k] }
            Add-YamlSecret $Source $Node[$k]
        }
    }
    elseif ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string]) { foreach ($i in $Node) { Add-YamlSecret $Source $i } }
}
$marker = ConvertFrom-Yaml ([System.IO.File]::ReadAllText((Join-Path $RepoRoot 'AGENTS-README-FIRST.yaml')))
Add-YamlSecret 'marker' $marker
Add-Secret 'env.MCP_AGENT_API_KEY' $env:MCP_AGENT_API_KEY
foreach ($c in @(Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.creds') -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in '.yaml', '.yml' })) {
    Add-YamlSecret "creds/$($c.Name)" (ConvertFrom-Yaml ([System.IO.File]::ReadAllText($c.FullName)))
}
$patterns = [ordered]@{
    'github-token'   = 'gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}'
    'tailscale-key'  = 'tskey-[A-Za-z0-9-]{10,}|\bk[A-Za-z0-9]{10,}CNTRL\b'
    'private-key'    = '-----BEGIN [A-Z ]*PRIVATE KEY-----'
    'aws-key'        = '\bAKIA[0-9A-Z]{16}\b'
    'x-api-key'      = '(?i)x-api-key\s*[:=]\s*["'']?[A-Za-z0-9+/=_-]{16,}'
    'password-value' = '(?i)\bpassword\s*[:=]\s*["'']?[^\s"''$<>{}()]{6,}'
}

Set-Location -LiteralPath $RepoRoot
$rows = foreach ($g in $groups.Keys) {
    foreach ($root in $groups[$g]) {
        $files = @(@(git -c color.ui=never -c core.quotepath=off ls-files --others --exclude-standard -- $root) | ForEach-Object { [pscustomobject]@{ Path = $_; Ignored = $false } })
        $files += @(@(git -c color.ui=never -c core.quotepath=off ls-files --others --ignored --exclude-standard -- $root) | ForEach-Object { [pscustomobject]@{ Path = $_; Ignored = $true } })
        foreach ($f in $files) {
            $full = Join-Path $RepoRoot $f.Path
            $len = (Get-Item -LiteralPath $full -Force).Length
            $hits = @()
            if ($len -le $MaxBytes) {
                $text = [System.IO.File]::ReadAllText($full)
                foreach ($s in $secrets) {
                    $found = if ($s.Regex) { $s.Regex.IsMatch($text) } else { $text.Contains($s.Value) }
                    if ($found) { $hits += "literal:$($s.Rule)" }
                }
                foreach ($p in $patterns.Keys) {
                    foreach ($m in [regex]::Matches($text, $patterns[$p])) { $hits += ('{0}@line{1}' -f $p, ($text.Substring(0, $m.Index).Split("`n").Count)) }
                }
            }
            [pscustomobject]@{ Group = $g; Path = $f.Path; Bytes = $len; Ignored = $f.Ignored; OverMax = ($len -gt $MaxBytes); Marker = ($f.Path -match 'AGENTS-README-FIRST\.yaml$'); Hits = @($hits) }
        }
    }
}
$rows = @($rows)
[ordered]@{ at = (Get-Date -Format s); secretsLoaded = $secrets.Count; maxBytes = $MaxBytes; files = $rows } | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM
"secret values loaded=$($secrets.Count) (values not printed): $(@($secrets | ForEach-Object Rule) -join ', ')"
foreach ($g in $groups.Keys) {
    $r = @($rows | Where-Object Group -EQ $g)
    '{0}: files={1} ignored={2} overMax={3} bytes={4:N0} withHits={5}' -f $g, $r.Count, @($r | Where-Object Ignored).Count, @($r | Where-Object OverMax).Count, ($r | Measure-Object Bytes -Sum).Sum, @($r | Where-Object { $_.Hits.Count }).Count
}
'over max:'; $rows | Where-Object OverMax | ForEach-Object { '  {0} {1:N0}' -f $_.Path, $_.Bytes }
'hits:'; $rows | Where-Object { $_.Hits.Count } | ForEach-Object { '  {0}: {1}' -f $_.Path, ($_.Hits -join ', ') }
'marker files:'; $rows | Where-Object Marker | ForEach-Object { '  ' + $_.Path }
